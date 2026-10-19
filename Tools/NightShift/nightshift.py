#!/usr/bin/env python3
"""CLONE-Z Night Shift: local-LLM coding agents that work through tasks.md overnight.

The model runs in Ollama on the DGX Spark and OpenCode (`opencode run`) is the agent, so no cloud AI tokens
are spent. The coordinator runs in the AI Workbench project container on the Spark. It hands tasks to
workers (the Spark itself plus any helper machines), each of which does its task in its own checkout,
compile-checks it against Unity's assemblies and commits. The coordinator then lands each result on the
night's branch (nightshift/<date>), compiles the whole project again and ticks the task off in tasks.md.

  nightshift.py run           coordinator: work through the queue (nightshift.sh start runs it in the background)
  nightshift.py status        progress summary
  nightshift.py map           rebuild memory/PROJECT_MAP.md
  nightshift.py chat [args]   interactive OpenCode with the same model and settings
  nightshift.py work ...      worker: do one task in a checkout and print the result (run by the coordinator)
"""
from __future__ import annotations

import argparse
import base64
import datetime as dt
import json
import os
import re
import secrets
import shlex
import shutil
import signal
import socket
import struct
import subprocess
import sys
import threading
import time
import urllib.request
from dataclasses import asdict, dataclass, field
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
REL = "Tools/NightShift"  # this folder, relative to the repo root
STATE = HERE / ".state"
TASKS = HERE / "tasks.md"
LESSONS_REL = f"{REL}/memory/LESSONS.md"
MAP_REL = f"{REL}/memory/PROJECT_MAP.md"
REPORTS = HERE / "reports"
STATUS_FILE = STATE / "status.json"
FINISH_FLAG = STATE / "finish-requested"
HELPERS_FILE = STATE / "helpers.txt"  # written by NightShift.ps1 helpers: one user@host per line
SSH_KEY = STATE / "ssh" / "id_ed25519"
SSH_KNOWN_HOSTS = STATE / "ssh" / "known_hosts"
RESULT_MARK = "NIGHTSHIFT_RESULT "
AUTHOR = ("CLONE-Z Night Shift", "nightshift@clone-z.local")
MAX_DISPATCH = 3  # a task is handed out at most this many times a night (retries after errors or collisions)
OWN_PATHS = [f"{REL}/tasks.md", f"{REL}/reports", f"{REL}/memory"]  # files the coordinator itself writes

DEFAULTS = {
    "NIGHTSHIFT_MODEL": "qwen3.8",
    "NIGHTSHIFT_HELPER_MODEL": "gpt-oss:120b",
    "NIGHTSHIFT_STOP_AT": "07:00",
    "NIGHTSHIFT_TZ": "America/Chicago",
    "NIGHTSHIFT_TASK_MINUTES": "120",
    "NIGHTSHIFT_FIX_ROUNDS": "2",
    "NIGHTSHIFT_CONTEXT": "131072",
    "NIGHTSHIFT_LOCAL_WORKER": "1",
    "NIGHTSHIFT_HELPER_OLLAMA_URL": "http://192.168.1.252:11434",
    "NIGHTSHIFT_HELPER_DIR": "clonez-nightshift",
    "OLLAMA_URL": "",
}

# Paths the agent must not change; anything it does to them is rolled back before the compile check.
PROTECTED = re.compile(
    r"^(ProjectSettings/|Packages/|\.project/|\.git|Assets/Resources/GeneratedItemModels/|Tools/(AssetPipeline|MapScraper)/"
    r"|Tools/NightShift/(?!memory/LESSONS\.md$))|\.(unity|meta|asmdef|asmref)$|^[^/]+\.(bash|env|txt|yaml|yml|ipynb)$"
)

STOPPING = threading.Event()
LOG_LOCK = threading.Lock()
# The night shift's own git calls skip the repo's hooks (Git LFS ones, and git-lfs isn't in the container or
# on the helpers) and pass LFS pointer files through untouched, so checkouts never try to download LFS objects.
GIT_SAFE = ["-c", "core.hooksPath=/dev/null", "-c", "filter.lfs.required=false", "-c", "filter.lfs.smudge=cat",
            "-c", "filter.lfs.clean=cat", "-c", "filter.lfs.process="]


def load_config() -> dict:
    cfg = dict(DEFAULTS)
    env_file = HERE / "nightshift.env"
    if env_file.exists():
        for line in env_file.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line and not line.startswith("#") and "=" in line:
                key, value = line.split("=", 1)
                cfg[key.strip()] = value.strip().strip('"').strip("'")
    for key in list(cfg):
        if os.environ.get(key):
            cfg[key] = os.environ[key]
    if hasattr(time, "tzset") and cfg.get("NIGHTSHIFT_TZ"):
        os.environ["TZ"] = cfg["NIGHTSHIFT_TZ"]
        time.tzset()
    return cfg


def log(msg: str, who: str = "") -> None:
    stamp = dt.datetime.now().strftime("%H:%M:%S")
    with LOG_LOCK:
        print(f"{stamp} {'[' + who + '] ' if who else ''}{msg}", flush=True)


def git_run(repo: Path | str, *args: str, env: dict | None = None) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *GIT_SAFE, "-C", str(repo), *args], capture_output=True, text=True, env=env,
                          stdin=subprocess.DEVNULL)


def git(repo: Path | str, *args: str, check: bool = True, env: dict | None = None) -> str:
    r = git_run(repo, *args, env=env)
    if check and r.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)}: {(r.stderr or r.stdout).strip()}")
    return r.stdout


def git_commit(repo: Path | str, subject: str, body: str = "") -> str:
    args = ["-c", f"user.name={AUTHOR[0]}", "-c", f"user.email={AUTHOR[1]}", "commit", "-q", "--no-verify", "-m", subject]
    if body:
        args += ["-m", body]
    git(repo, *args)
    return git(repo, "rev-parse", "HEAD").strip()


def ssh_command() -> list[str]:
    return ["ssh", "-i", str(SSH_KEY), "-o", f"UserKnownHostsFile={SSH_KNOWN_HOSTS}", "-o", "StrictHostKeyChecking=yes",
            "-o", "BatchMode=yes", "-o", "ConnectTimeout=10", "-o", "ServerAliveInterval=30", "-o", "ServerAliveCountMax=6"]


def git_env() -> dict:
    return dict(os.environ, GIT_SSH_COMMAND=" ".join(shlex.quote(a) for a in ssh_command()))


# --- Ollama -------------------------------------------------------------------------------------------------

def default_gateway() -> str | None:
    """The Docker host as seen from the container: Ollama on the Spark listens there."""
    try:
        for line in Path("/proc/net/route").read_text().splitlines()[1:]:
            fields = line.split()
            if len(fields) > 2 and fields[1] == "00000000":
                return socket.inet_ntoa(struct.pack("<L", int(fields[2], 16)))
    except OSError:
        pass
    return None


def find_ollama(cfg: dict) -> str:
    candidates = [cfg["OLLAMA_URL"]] if cfg.get("OLLAMA_URL") else []
    gateway = default_gateway()
    if gateway:
        candidates.append(f"http://{gateway}:11434")
    candidates += ["http://127.0.0.1:11434", cfg["NIGHTSHIFT_HELPER_OLLAMA_URL"]]
    for url in candidates:
        url = url.rstrip("/")
        try:
            with urllib.request.urlopen(url + "/api/version", timeout=5) as r:
                json.load(r)
            return url
        except Exception:
            continue
    raise RuntimeError("Can't reach Ollama (tried " + ", ".join(candidates) + ")")


def ollama_models(base: str) -> set[str]:
    with urllib.request.urlopen(base + "/api/tags", timeout=15) as r:
        return {m["name"] for m in json.load(r).get("models", [])}


def has_model(base: str, model: str) -> bool:
    names = ollama_models(base)
    return model in names or (":" not in model and model + ":latest" in names)


def warm_model(base: str, model: str) -> None:
    body = json.dumps({"model": model, "prompt": "", "keep_alive": "8h"}).encode()
    req = urllib.request.Request(base + "/api/generate", body, {"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=900) as r:
        r.read()


# --- OpenCode -----------------------------------------------------------------------------------------------

def opencode_bin() -> str:
    found = shutil.which("opencode")
    if found:
        return found
    for candidate in ("/usr/local/bin/opencode", "~/.local/bin/opencode", "~/.opencode/bin/opencode"):
        path = Path(candidate).expanduser()
        if path.exists():
            return str(path)
    raise RuntimeError("OpenCode is not installed (the Workbench container build installs it; helpers get it from NightShift.ps1 helpers)")


def opencode_config(base_url: str, model: str, context: int, repo: Path) -> dict:
    models = {}
    for name in dict.fromkeys([model, "qwen3.8", "gpt-oss:120b"]):
        models[name] = {"name": f"{name} (DGX Spark)", "limit": {"context": context, "output": 32768}}
    bash = {"*": "allow"}
    for pattern in ("git commit*", "git push*", "git pull*", "git fetch*", "git reset*", "git checkout*", "git switch*",
                    "git restore*", "git rebase*", "git merge*", "git cherry-pick*", "git revert*", "git stash*",
                    "git clean*", "git branch*", "git tag*", "git remote*", "git worktree*", "sudo*", "ssh*", "scp*"):
        bash[pattern] = "deny"
    edit = {"*": "allow"}
    for pattern in ("*.meta", "*.unity", "*ProjectSettings/*", "*Packages/*", f"*{REL}/tasks.md", f"*{REL}/*.py",
                    f"*{REL}/*.sh", f"*{REL}/*.ps1", f"*{REL}/*.env"):
        edit[pattern] = "deny"
    return {
        "$schema": "https://opencode.ai/config.json",
        "autoupdate": False,
        "share": "disabled",
        "lsp": False,
        "model": f"ollama/{model}",
        "small_model": f"ollama/{model}",
        "instructions": [str(repo / LESSONS_REL), str(repo / MAP_REL)],
        "provider": {"ollama": {"npm": "@ai-sdk/openai-compatible", "name": "Ollama (DGX Spark)",
                                "options": {"baseURL": base_url.rstrip("/") + "/v1"}, "models": models}},
        "permission": {"*": "allow", "external_directory": "deny", "doom_loop": "deny", "question": "deny",
                       "websearch": "deny", "bash": bash, "edit": edit},
    }


def write_opencode_config(repo: Path, base_url: str, model: str, context: int) -> Path:
    path = repo / REL / ".state" / "opencode.json"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(opencode_config(base_url, model, context, repo), indent=2), encoding="utf-8")
    return path


@dataclass
class AgentRun:
    rc: int = 0
    session: str | None = None
    text: str = ""
    tools: int = 0
    tokens_in: int = 0
    tokens_out: int = 0
    tokens_cached: int = 0
    timed_out: bool = False
    error: str = ""


def kill_group(proc: subprocess.Popen, sig: int = signal.SIGTERM) -> None:
    try:
        os.killpg(proc.pid, sig)
    except (ProcessLookupError, PermissionError):
        pass


def terminate(proc: subprocess.Popen, grace: float = 20) -> None:
    """TERM the process group, then KILL it if it is still there after `grace` seconds."""
    if proc.poll() is not None:
        return
    kill_group(proc)
    try:
        proc.wait(timeout=grace)
    except subprocess.TimeoutExpired:
        kill_group(proc, signal.SIGKILL)
        proc.wait()


def die_with_parent() -> None:
    """preexec_fn: have Linux kill the child if this process dies, so an agent never outlives its worker."""
    try:
        import ctypes
        ctypes.CDLL("libc.so.6", use_errno=True).prctl(1, signal.SIGKILL)  # PR_SET_PDEATHSIG
    except Exception:
        pass


def run_in_group(cmd: list[str], timeout: float, **kwargs) -> tuple[int, str]:
    """Runs cmd in its own process group and returns (exit code, output); on timeout the whole group goes."""
    proc = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                            start_new_session=True, **kwargs)
    try:
        out, _ = proc.communicate(timeout=timeout)
        return proc.returncode, out or ""
    except subprocess.TimeoutExpired:
        terminate(proc, grace=10)
        out, _ = proc.communicate()
        return 124, (out or "") + f"\n(timed out after {timeout:.0f}s)"


def describe_tool(part: dict, repo: Path) -> str:
    state = part.get("state") or {}
    args = state.get("input") or {}
    target = (args.get("command") or args.get("filePath") or args.get("pattern") or args.get("path")
              or args.get("url") or args.get("description") or "")
    text = f"{part.get('tool', '?')} {target}".replace(str(repo) + "/", "").replace("\n", " ")
    if state.get("status") == "error":
        text += "  (failed)"
    return text[:180]


def run_opencode(repo: Path, prompt: str, cfg_path: Path, model: str, timeout_s: float, log_file: Path,
                 env: dict, session: str | None = None, title: str | None = None) -> AgentRun:
    cmd = [opencode_bin(), "run", "--auto", "--format", "json", "--model", f"ollama/{model}", "--dir", str(repo)]
    if session:
        cmd += ["--session", session]
    elif title:
        cmd += ["--title", title[:80]]
    cmd.append(prompt)
    env = dict(env, OPENCODE_CONFIG=str(cfg_path), OPENCODE_DISABLE_AUTOUPDATE="true", OPENCODE_DISABLE_LSP_DOWNLOAD="true")
    res = AgentRun()
    texts: dict[str, list[str]] = {}
    last_msg = ""
    proc = subprocess.Popen(cmd, cwd=repo, env=env, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, text=True, bufsize=1, start_new_session=True,
                            preexec_fn=die_with_parent)

    def stop_agent(timed_out: bool) -> None:
        if timed_out:
            res.timed_out = True
        threading.Thread(target=terminate, args=(proc, 20), daemon=True).start()

    timer = threading.Timer(max(timeout_s, 60), stop_agent, args=(True,))
    timer.start()
    stop_watch = threading.Thread(target=lambda: (STOPPING.wait(), stop_agent(False)), daemon=True)
    stop_watch.start()
    try:
        with open(log_file, "a", encoding="utf-8") as lf:
            for line in proc.stdout:
                lf.write(line)
                try:
                    ev = json.loads(line)
                except ValueError:
                    if line.strip():
                        log(line.strip()[:200])
                    continue
                res.session = res.session or ev.get("sessionID")
                kind, part = ev.get("type"), ev.get("part") or {}
                if kind == "tool_use":
                    res.tools += 1
                    log("  " + describe_tool(part, repo))
                elif kind == "text" and (part.get("text") or "").strip():
                    last_msg = part.get("messageID", "")
                    texts.setdefault(last_msg, []).append(part["text"].strip())
                elif kind == "step_finish":
                    tokens = part.get("tokens") or {}
                    res.tokens_in += tokens.get("input", 0)
                    res.tokens_out += tokens.get("output", 0) + tokens.get("reasoning", 0)
                    res.tokens_cached += (tokens.get("cache") or {}).get("read", 0)
                elif kind == "error":
                    res.error = json.dumps(ev.get("error") or ev)[:400]
                    log("  agent error: " + res.error)
        proc.wait()
    finally:
        timer.cancel()
        terminate(proc, grace=5)
    res.rc = proc.returncode
    res.text = "\n".join(texts.get(last_msg, []))
    return res


# --- Checks and clean-up in a worker checkout ---------------------------------------------------------------

def compile_check(repo: Path, env: dict) -> tuple[bool, str]:
    rc, out = run_in_group(["bash", str(repo / REL / "check.sh")], timeout=1200, cwd=repo, env=env)
    out = out.strip()
    if rc == 2:
        raise RuntimeError(out)
    return rc == 0, out


def changed_files(repo: Path, base: str) -> list[str]:
    tracked = git(repo, "diff", "--name-only", "-z", base).split("\0")
    untracked = git(repo, "ls-files", "--others", "--exclude-standard", "-z").split("\0")
    return sorted({p for p in tracked + untracked if p})


def in_commit(repo: Path, rev: str, path: str) -> bool:
    return git_run(repo, "cat-file", "-e", f"{rev}:{path}").returncode == 0


def revert_protected(repo: Path, base: str) -> list[str]:
    reverted = []
    for path in changed_files(repo, base):
        if not PROTECTED.search(path):
            continue
        if in_commit(repo, base, path):
            git(repo, "checkout", base, "--", path)
        else:
            (repo / path).unlink(missing_ok=True)
        reverted.append(path)
    return reverted


def take_lessons(repo: Path, base: str) -> list[str]:
    """New lines the agent appended to LESSONS.md. They are taken out of the worker's commit and added by the
    coordinator instead, so lessons from parallel workers never conflict."""
    diff = git(repo, "diff", base, "--", LESSONS_REL)
    added = [l[1:].strip() for l in diff.splitlines() if l.startswith("+") and not l.startswith("+++")]
    if in_commit(repo, base, LESSONS_REL):
        git(repo, "checkout", base, "--", LESSONS_REL)
    return [l for l in added if l and not l.startswith("#")]


CS_META = "fileFormatVersion: 2\nguid: {guid}\n"
FOLDER_META = ("fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
               "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def sync_meta(repo: Path, base: str) -> None:
    """Unity needs a .meta beside every asset. A renamed script keeps its old .meta (and so its GUID, which
    scenes and prefabs reference); new scripts and folders get fresh ones, as Unity would write them; a deleted
    script's .meta goes too. That way the branch imports cleanly on the PC."""
    git(repo, "add", "-A")
    for line in git(repo, "diff", "--cached", "-M", "--name-status", base).splitlines():
        parts = line.split("\t")
        if parts[0].startswith("R") and len(parts) == 3 and parts[2].startswith("Assets/"):
            old_meta, new_meta = parts[1] + ".meta", repo / (parts[2] + ".meta")
            if in_commit(repo, base, old_meta) and not new_meta.exists():
                new_meta.write_bytes(subprocess.run(["git", *GIT_SAFE, "-C", str(repo), "show", f"{base}:{old_meta}"],
                                                    capture_output=True, stdin=subprocess.DEVNULL).stdout)
    for path in changed_files(repo, base):
        if not path.startswith("Assets/") or path.endswith(".meta"):
            continue
        full = repo / path
        if full.exists():
            parent = full.parent
            while parent != repo / "Assets" and not parent.with_name(parent.name + ".meta").exists():
                parent.with_name(parent.name + ".meta").write_text(FOLDER_META.format(guid=secrets.token_hex(16)), encoding="utf-8")
                parent = parent.parent
            meta = full.with_name(full.name + ".meta")
            if path.endswith(".cs") and not meta.exists():
                meta.write_text(CS_META.format(guid=secrets.token_hex(16)), encoding="utf-8")
        else:
            full.with_name(full.name + ".meta").unlink(missing_ok=True)


# --- Worker: one task in its own checkout -------------------------------------------------------------------

PROMPT = """You are the CLONE-Z Night Shift, an autonomous coding agent working alone overnight on CLONE-Z, a Unity 6
isometric zombie-survival game written in C#. Nobody can answer questions until morning, so make sensible
decisions yourself and keep going until the task is done.

TASK
{task}

HOW TO WORK
- Your instructions include memory/PROJECT_MAP.md (one line per script) and memory/LESSONS.md (notes from
  earlier nights). Use them to find the right files, then grep and read only the parts you need. Never read
  Assets/Scripts/World/ItemModels.cs whole (10,000+ lines); grep it instead.
- Match the surrounding code: namespace CloneZ, the same naming and the same comment style. Prefer extending
  the existing systems over adding parallel ones, and keep the change focused on the task.
- Unity is not available here, so you cannot run the game. To compile all scripts against Unity's
  assemblies, run:  bash Tools/NightShift/check.sh
  Fix every error it reports and run it again until it prints COMPILE OK. Never finish while it fails.
- Do not edit Tools/NightShift (except appending to memory/LESSONS.md), ProjectSettings/, Packages/,
  scenes (*.unity), *.meta files or Assets/Resources/GeneratedItemModels/. Do not run git commands; the
  night shift commits your work for you.
- If you learn something a future night would need (a gotcha, where a system lives, a convention), append
  one short bullet to Tools/NightShift/memory/LESSONS.md.

WHEN DONE
Reply with a short summary: what you changed (files and behaviour) and what to try in Unity to see it."""

FIX_PROMPT = """The compile check still fails. Fix these errors, then run  bash Tools/NightShift/check.sh  again
until it prints COMPILE OK:

{errors}"""


@dataclass
class Result:
    status: str = "error"  # ok | nochange | failed | error
    sha: str = ""
    summary: str = ""
    errors: str = ""
    files: list = field(default_factory=list)
    lessons: list = field(default_factory=list)
    patch: str = ""
    minutes: float = 0.0
    tools: int = 0
    tokens_in: int = 0
    tokens_out: int = 0
    tokens_cached: int = 0


def do_task(repo: Path, base: str, task: str, model: str, ollama: str, minutes: float, fix_rounds: int,
            context: int, unityrefs: str) -> Result:
    started = time.time()
    deadline = started + minutes * 60
    res = Result()
    env = dict(os.environ, NIGHTSHIFT_UNITYREFS=unityrefs) if unityrefs else dict(os.environ)
    git(repo, "checkout", "-q", "--detach", "--force", base)
    git(repo, "clean", "-q", "-fdx", "-e", f"{REL}/.state/")
    runs_dir = repo / REL / ".state" / "runs"
    runs_dir.mkdir(parents=True, exist_ok=True)
    log_file = runs_dir / (dt.datetime.now().strftime("%Y%m%d-%H%M%S") + ".jsonl")
    cfg_path = write_opencode_config(repo, ollama, model, context)

    trouble: list[str] = []

    def agent(prompt: str, session: str | None = None) -> AgentRun:
        run = run_opencode(repo, prompt, cfg_path, model, deadline - time.time(), log_file, env,
                           session=session, title=None if session else "nightshift: " + task.splitlines()[0])
        res.tools += run.tools
        res.tokens_in += run.tokens_in
        res.tokens_out += run.tokens_out
        res.tokens_cached += run.tokens_cached
        if run.text:
            res.summary = run.text
        if run.timed_out:
            trouble.append(f"the agent hit the {minutes:.0f}-minute time limit")
        elif run.error:
            trouble.append("the agent reported an error: " + run.error)
        elif run.rc != 0 and not STOPPING.is_set():
            trouble.append(f"the agent exited with code {run.rc}")
        if trouble:
            log("  " + trouble[-1][:200])
        return run

    first = agent(PROMPT.format(task=task))
    reverted = revert_protected(repo, base)
    if reverted:
        log("  rolled back protected files: " + ", ".join(reverted[:8]))
    res.lessons += take_lessons(repo, base)
    ok, out = compile_check(repo, env)
    rounds = 0
    while not ok and not trouble and rounds < fix_rounds and time.time() < deadline - 120 and not STOPPING.is_set():
        rounds += 1
        log(f"  compile check failed; fix round {rounds}/{fix_rounds}")
        agent(FIX_PROMPT.format(errors=out[-6000:]), session=first.session)
        revert_protected(repo, base)
        res.lessons += take_lessons(repo, base)
        ok, out = compile_check(repo, env)
    sync_meta(repo, base)
    res.files = changed_files(repo, base)
    res.minutes = round((time.time() - started) / 60, 1)
    git(repo, "add", "-A")
    if STOPPING.is_set():
        res.status, res.errors = "error", "stopped"
    elif trouble and not res.files:
        res.status, res.errors = "error", "; ".join(trouble)
    elif trouble or not ok:
        # Unfinished or not compiling: keep the attempt for a human, but never land it.
        res.status = "failed"
        res.errors = "\n".join(trouble + ([] if ok else [out[-4000:]]))
        res.patch = git(repo, "diff", "--cached", base)[:400_000]
    elif not res.files:
        res.status = "nochange"
    else:
        res.sha = git_commit(repo, "Night shift: " + task.splitlines()[0][:70], res.summary)
        res.status = "ok"
        git(repo, "branch", "-f", "nightshift-result", res.sha)
    return res


def cmd_work(args: argparse.Namespace) -> int:
    """Worker entry point. Progress goes to stdout; the last line is RESULT_MARK + JSON."""
    repo = Path(args.repo).resolve()
    pid_file = repo.parent / "work.pid"
    pid_file.write_text(str(os.getpid()))
    signal.signal(signal.SIGTERM, lambda *_: STOPPING.set())
    signal.signal(signal.SIGHUP, lambda *_: STOPPING.set())
    task = base64.b64decode(args.task_b64).decode("utf-8")
    try:
        res = do_task(repo, args.base, task, args.model, args.ollama, args.minutes, args.fix_rounds, args.context, args.unityrefs)
    except Exception as e:  # report the failure to the coordinator instead of dying silently
        res = Result(status="error", errors=str(e)[-2000:])
    finally:
        if pid_file.exists() and pid_file.read_text().strip() == str(os.getpid()):
            pid_file.unlink(missing_ok=True)
    print(RESULT_MARK + json.dumps(asdict(res)), flush=True)
    return 0


# --- tasks.md and the night's report -------------------------------------------------------------------------

BOX = re.compile(r"^(\s*)- \[([ xX!~])\] (.+?)\s*$")
TASKS_LOCK = threading.Lock()


@dataclass
class Task:
    key: str   # the text on the checkbox line; identifies the task
    text: str  # the key plus any indented detail lines under it


def _block_end(lines: list[str], i: int, indent: int) -> int:
    j = i + 1
    while j < len(lines) and lines[j].strip() and len(lines[j]) - len(lines[j].lstrip()) > indent and not BOX.match(lines[j]):
        j += 1
    return j


def read_tasks() -> list[tuple[str, Task]]:
    with TASKS_LOCK:
        if not TASKS.exists():
            return []
        lines = TASKS.read_text(encoding="utf-8").splitlines()
    tasks, i = [], 0
    while i < len(lines):
        m = BOX.match(lines[i])
        if not m:
            i += 1
            continue
        end = _block_end(lines, i, len(m.group(1)))
        detail = [l.strip() for l in lines[i + 1:end]]
        tasks.append((m.group(2), Task(m.group(3), "\n".join([m.group(3)] + detail))))
        i = end
    return tasks


def write_atomic(path: Path, text: str) -> None:
    tmp = path.with_name(path.name + ".tmp")
    tmp.write_text(text, encoding="utf-8")
    os.replace(tmp, path)


def mark_task(key: str, mark: str, note: str) -> None:
    with TASKS_LOCK:
        lines = TASKS.read_text(encoding="utf-8").splitlines()
        for i, line in enumerate(lines):
            m = BOX.match(line)
            if m and m.group(2) == " " and m.group(3) == key:
                lines[i] = f"{m.group(1)}- [{mark}] {m.group(3)}"
                lines.insert(_block_end(lines, i, len(m.group(1))), " " * (len(m.group(1)) + 2) + "- " + note)
                write_atomic(TASKS, "\n".join(lines) + "\n")
                return


def slug(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")[:40] or "task"


@dataclass
class Entry:
    n: int
    task: str
    outcome: str  # landed | failed | no change | gave up
    worker: str
    minutes: float
    summary: str = ""
    files: list = field(default_factory=list)
    errors: str = ""
    patch_file: str = ""
    sha: str = ""


class Night:
    def __init__(self, date: str, branch: str, model: str):
        self.date, self.branch, self.model = date, branch, model
        self.started = dt.datetime.now()
        self.workers: list[str] = []
        self.entries: list[Entry] = []
        self.unfinished: list[str] = []  # tasks tried tonight that are still unchecked
        self.tokens = [0, 0, 0]  # in, out, cached
        self.dir = REPORTS / date
        self.file = REPORTS / f"{date}.md"

    def add(self, entry: Entry) -> None:
        self.entries.append(entry)

    def write(self) -> None:
        REPORTS.mkdir(parents=True, exist_ok=True)
        count = lambda o: sum(1 for e in self.entries if e.outcome == o)
        lines = [
            f"# Night shift {self.date}",
            "",
            f"Branch `{self.branch}` · model `{self.model}` in Ollama on the DGX Spark (no cloud tokens) · "
            f"workers: {', '.join(self.workers) or '-'}",
            f"Started {self.started:%H:%M} · updated {dt.datetime.now():%H:%M} · {count('landed')} landed · "
            f"{count('failed') + count('gave up')} failed · {count('no change')} no change · "
            f"{self.tokens[0] + self.tokens[1]:,} tokens processed locally ({self.tokens[2]:,} from cache)",
            "",
            "| # | Task | Result | Worker | Minutes | Commit |",
            "|---|---|---|---|---|---|",
        ]
        for e in self.entries:
            first = e.task.splitlines()[0].replace("|", "/")
            lines.append(f"| {e.n} | {first[:90]} | {e.outcome} | {e.worker} | {e.minutes:g} | {e.sha[:9]} |")
        for e in self.entries:
            lines += ["", f"## {e.n}. {e.task.splitlines()[0]}", "", f"**{e.outcome}** by {e.worker} in {e.minutes:g} min."]
            if e.summary:
                lines += ["", e.summary.strip()]
            if e.files:
                lines += ["", "Files: " + ", ".join(f"`{f}`" for f in e.files[:25])]
            if e.patch_file:
                lines += ["", f"The attempt is saved in [{Path(e.patch_file).name}]({self.date}/{Path(e.patch_file).name})."]
            if e.errors:
                lines += ["", "```", e.errors.strip()[-1500:], "```"]
        if self.unfinished:
            lines += ["", "## Unfinished", "", "Tried tonight but neither landed nor failed for good (stopped, time ran out or the "
                      "worker kept erroring); they are still unchecked in tasks.md:", ""]
            lines += [f"- {t}" for t in self.unfinished]
        self.file.write_text("\n".join(lines) + "\n", encoding="utf-8")


# --- Coordinator --------------------------------------------------------------------------------------------

def first_line(text: str, width: int = 140) -> str:
    line = next((l.strip() for l in (text or "").splitlines() if l.strip()), "")
    return line[:width]


def helpers(cfg: dict) -> list[tuple[str, str]]:
    """(name, user@host) for every helper machine, from .state/helpers.txt and NIGHTSHIFT_HELPERS."""
    lines = HELPERS_FILE.read_text(encoding="utf-8").splitlines() if HELPERS_FILE.exists() else []
    lines += cfg.get("NIGHTSHIFT_HELPERS", "").split(",")
    found: dict[str, str] = {}
    for line in lines:
        parts = line.split("#", 1)[0].split()
        if not parts:
            continue
        target = parts[-1]
        found[target] = parts[0] if len(parts) > 1 else target.split("@")[-1]
    return [(name, target) for target, name in found.items()]


class Worker:
    def __init__(self, name: str, target: str | None, cfg: dict, ollama: str):
        self.name, self.target, self.cfg = name, target, cfg
        # qwen3.8's architecture serves one request at a time in Ollama, so helpers default to a model that runs in
        # parallel; otherwise they would only queue behind the Spark's own agent.
        self.model = (cfg.get("NIGHTSHIFT_HELPER_MODEL") or cfg["NIGHTSHIFT_MODEL"]) if target else cfg["NIGHTSHIFT_MODEL"]
        home_dir = cfg["NIGHTSHIFT_HELPER_DIR"]
        if target:  # a helper machine, reached over SSH; paths are relative to its home
            self.repo, self.dest = f"{home_dir}/repo", f"{target}:{home_dir}/repo"
            self.ollama, self.unityrefs = cfg["NIGHTSHIFT_HELPER_OLLAMA_URL"], f"$HOME/{home_dir}/unityrefs"
            self.pid_file, self.python, self.nice = f"{home_dir}/work.pid", "python3", "nice -n 15 "
        else:  # this container: its own clone in /tmp, so the project checkout is only touched when landing
            self.repo = self.dest = "/tmp/nightshift-work/spark/repo"
            self.ollama, self.unityrefs = ollama, str(HERE / ".unityrefs")
            self.pid_file, self.python, self.nice = "/tmp/nightshift-work/spark/work.pid", sys.executable, ""
        self.proc: subprocess.Popen | None = None
        self.lock = threading.Lock()
        self.errors = 0

    def _cmd(self, script: str) -> list[str]:
        return ssh_command() + [self.target, script] if self.target else ["bash", "-c", script]

    def _git_env(self) -> dict | None:
        return git_env() if self.target else None

    def prepare(self) -> str | None:
        """Returns why this worker can't be used, or None when it is ready."""
        if self.target and not SSH_KEY.exists():
            return "no SSH key for helpers yet (run NightShift.ps1 helpers on the PC)"
        repo = shlex.quote(self.repo)
        script = f"""set -e
export PATH="$HOME/.local/bin:$HOME/.opencode/bin:$HOME/.dotnet:/opt/dotnet:$PATH"
mkdir -p {repo} && cd {repo} && (git rev-parse --git-dir >/dev/null 2>&1 || git init -q)
command -v opencode >/dev/null || {{ echo "OpenCode is not installed"; exit 1; }}
command -v dotnet >/dev/null || {{ echo ".NET SDK is not installed"; exit 1; }}
[ -f "{self.unityrefs}/Runtime.csproj" ] || {{ echo "Unity reference assemblies are missing"; exit 1; }}
curl -s --max-time 5 {shlex.quote(self.ollama)}/api/version >/dev/null || {{ echo "can't reach Ollama at {self.ollama}"; exit 1; }}
echo READY"""
        try:
            r = subprocess.run(self._cmd(script), capture_output=True, text=True, timeout=60, stdin=subprocess.DEVNULL)
        except subprocess.TimeoutExpired:
            return "timed out"
        return None if "READY" in r.stdout else (r.stdout + r.stderr).strip()[-300:] or f"exit {r.returncode}"

    def run(self, task: Task, base: str, minutes: float, model: str, fix_rounds: int, context: int) -> Result:
        self.reap()  # a work process left over from an earlier task must be gone before its checkout is reused
        git(ROOT, "push", "-q", "--no-verify", "--force", self.dest, f"{base}:refs/heads/nightshift-base", env=self._git_env())
        b64 = base64.b64encode(task.text.encode("utf-8")).decode()
        safe_git = "git " + " ".join(shlex.quote(a) for a in GIT_SAFE)
        # exec makes the worker's Python the session leader, so its pid is the process group that reap() kills.
        script = (f'export PATH="$HOME/.local/bin:$HOME/.opencode/bin:$HOME/.dotnet:/opt/dotnet:$PATH" && '
                  f"cd {shlex.quote(self.repo)} && {safe_git} checkout -q --detach --force nightshift-base && "
                  f"exec {self.nice}{shlex.quote(self.python)} {REL}/nightshift.py work --repo . --base {base} --task-b64 {b64} "
                  f"--model {shlex.quote(model)} --ollama {shlex.quote(self.ollama)} --minutes {minutes:.0f} "
                  f'--fix-rounds {fix_rounds} --context {context} --unityrefs "{self.unityrefs}"')
        with self.lock:
            if STOPPING.is_set():
                return Result(status="error", errors="stopped")
            self.proc = proc = subprocess.Popen(self._cmd(script), stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                                stderr=subprocess.STDOUT, text=True, bufsize=1, start_new_session=True)

        def hard_stop() -> None:
            log("over the hard time limit; ending this task", self.name)
            self.reap(grace=10)
            terminate(proc, grace=10)

        timer = threading.Timer((minutes + 15) * 60, hard_stop)
        timer.start()
        result = None
        try:
            for line in proc.stdout:
                line = line.rstrip("\n")
                if line.startswith(RESULT_MARK):
                    result = Result(**json.loads(line[len(RESULT_MARK):]))
                elif line.strip():
                    log(re.sub(r"^\d\d:\d\d:\d\d ", "", line)[:220], self.name)
            proc.wait()
        finally:
            timer.cancel()
            terminate(proc, grace=5)
            with self.lock:
                self.proc = None
        if result is None:
            return Result(status="error", errors=f"the worker exited without a result (exit {proc.returncode})")
        if result.status == "ok":
            ref = f"refs/nightshift/{self.name}"
            git(ROOT, "fetch", "-q", self.dest, f"+refs/heads/nightshift-result:{ref}", env=self._git_env())
            if git(ROOT, "rev-parse", ref).strip() != result.sha:
                return Result(status="error", errors=f"fetched {ref} doesn't match the worker's commit {result.sha[:9]}")
        return result

    REAP = ('p=$(cat {pid} 2>/dev/null); if [ -n "$p" ] && kill -0 "$p" 2>/dev/null; then '
            'kill -TERM -- "-$p" 2>/dev/null || kill -TERM "$p" 2>/dev/null; '
            'for i in $(seq 1 {grace}); do kill -0 "$p" 2>/dev/null || break; sleep 1; done; '
            'kill -KILL -- "-$p" 2>/dev/null; kill -KILL "$p" 2>/dev/null; fi; rm -f {pid}; true')

    def reap(self, grace: int = 15) -> None:
        """Ends this worker's work process, if one is running (TERM, then KILL after `grace` seconds)."""
        try:
            subprocess.run(self._cmd(self.REAP.format(pid=shlex.quote(self.pid_file), grace=grace)), capture_output=True,
                           timeout=grace + 30, stdin=subprocess.DEVNULL)
        except Exception:
            pass

    def stop(self) -> None:
        with self.lock:
            proc = self.proc
        self.reap(grace=10)
        if proc:
            terminate(proc, grace=5)


class Coordinator:
    def __init__(self, cfg: dict):
        self.cfg = cfg
        self.model = cfg["NIGHTSHIFT_MODEL"]
        self.minutes = float(cfg["NIGHTSHIFT_TASK_MINUTES"])
        self.fix_rounds = int(cfg["NIGHTSHIFT_FIX_ROUNDS"])
        self.context = int(cfg["NIGHTSHIFT_CONTEXT"])
        hour, minute = (int(x) for x in cfg["NIGHTSHIFT_STOP_AT"].split(":"))
        now = dt.datetime.now()
        self.deadline = now.replace(hour=hour, minute=minute, second=0, microsecond=0)
        if self.deadline <= now:
            self.deadline += dt.timedelta(days=1)
        self.queue_lock, self.land_lock = threading.Lock(), threading.Lock()
        self.in_flight: dict[str, tuple[str, str]] = {}  # task key -> (worker, since)
        self.dispatches: dict[str, int] = {}  # task key -> times handed to a worker tonight (never decreases)
        self.workers: list[Worker] = []
        self.night: Night | None = None

    def write_status(self) -> None:
        status = {"pid": os.getpid(), "model": self.model, "deadline": f"{self.deadline:%a %H:%M}",
                  "branch": self.night.branch if self.night else "", "workers": [w.name for w in self.workers],
                  "working": {k: {"worker": v[0], "since": v[1]} for k, v in self.in_flight.items()}}
        STATUS_FILE.write_text(json.dumps(status, indent=2), encoding="utf-8")

    def claim(self, worker: Worker) -> Task | None:
        with self.queue_lock:
            if STOPPING.is_set() or FINISH_FLAG.exists() or dt.datetime.now() >= self.deadline:
                return None
            for mark, task in read_tasks():
                if mark == " " and task.key not in self.in_flight and self.dispatches.get(task.key, 0) < MAX_DISPATCH:
                    self.in_flight[task.key] = (worker.name, dt.datetime.now().strftime("%H:%M"))
                    self.dispatches[task.key] = self.dispatches.get(task.key, 0) + 1
                    self.write_status()
                    return task
        return None

    def commit_pending(self, subject: str, paths: list[str] | None = None) -> None:
        """Commits changes under `paths` (default: everything) if there are any."""
        git(ROOT, "add", "-A", "--", *(paths or ["."]))
        if git_run(ROOT, "diff", "--cached", "--quiet").returncode != 0:
            git_commit(ROOT, subject)

    def add_lessons(self, lessons: list[str]) -> None:
        path = ROOT / LESSONS_REL
        body = path.read_text(encoding="utf-8") if path.exists() else "# Lessons\n"
        new = [l if l.startswith("- ") else "- " + l.lstrip("-* ") for l in lessons]
        new = [l for l in dict.fromkeys(new) if l not in body]
        if new:
            path.write_text(body.rstrip("\n") + "\n" + "\n".join(new) + "\n", encoding="utf-8")

    def land(self, worker: Worker, task: Task, res: Result) -> None:
        with self.land_lock:
            night = self.night
            night.tokens = [night.tokens[0] + res.tokens_in, night.tokens[1] + res.tokens_out, night.tokens[2] + res.tokens_cached]
            can_retry = self.dispatches.get(task.key, 0) < MAX_DISPATCH and not STOPPING.is_set()
            if res.status == "error" and (can_retry or STOPPING.is_set()):
                log(f"not finished ({first_line(res.errors, 160)}); it stays in the queue", worker.name)
                return
            # Commit queue edits (and anything else found in the project folder) first, so rolling back a
            # failed landing below can never throw away someone's changes.
            self.commit_pending("Night shift: queue update", OWN_PATHS)
            self.commit_pending("Night shift: changes found in the project folder")
            head = git(ROOT, "rev-parse", "HEAD").strip()
            entry = Entry(len(night.entries) + 1, task.text, "", worker.name, res.minutes, res.summary, res.files)
            try:
                self._land(worker, task, res, entry, head, can_retry)
            except Exception:
                git_run(ROOT, "cherry-pick", "--abort")
                git(ROOT, "reset", "-q", "--hard", head)
                git(ROOT, "clean", "-q", "-fd")
                if entry in night.entries:
                    night.entries.remove(entry)
                raise

    def _land(self, worker: Worker, task: Task, res: Result, entry: Entry, head: str, can_retry: bool) -> None:
        night, stamp = self.night, dt.datetime.now().strftime("%H:%M")
        if res.status == "ok":
            pick = git_run(ROOT, "cherry-pick", "--no-commit", res.sha)
            ok, out = (False, "Conflicts with work landed earlier tonight:\n" + pick.stdout + pick.stderr) if pick.returncode \
                else compile_check(ROOT, dict(os.environ))
            if ok:
                self.add_lessons(res.lessons)
                entry.outcome = "landed"
                mark_task(task.key, "x", f"{stamp} landed by {worker.name} in {res.minutes:g} min: {first_line(res.summary)}")
                night.add(entry)
                night.write()
                git(ROOT, "add", "-A")
                body = (f"{res.summary.strip()}\n\nTask: {task.text}\n\nWorker: {worker.name} · model {worker.model} "
                        f"(Ollama on the DGX Spark) · {res.minutes:g} min · compile check passed")
                entry.sha = git_commit(ROOT, "Night shift: " + task.key[:70], body)
                log(f"landed {entry.sha[:9]}: {task.key[:90]}", worker.name)
                return
            git_run(ROOT, "cherry-pick", "--abort")
            git(ROOT, "reset", "-q", "--hard", head)
            git(ROOT, "clean", "-q", "-fd")
            if can_retry:
                log("the change doesn't fit on top of tonight's other work; it goes back in the queue", worker.name)
                return
            res.status, res.errors = "failed", out[-4000:]
            res.patch = git(ROOT, "format-patch", "--stdout", "-1", res.sha)
        if res.status == "nochange":
            entry.outcome, mark, note = "no change", "~", f"{stamp} {worker.name} made no changes: {first_line(res.summary)}"
        else:
            entry.outcome = "failed" if res.status == "failed" else "gave up"
            entry.errors = res.errors
            mark, note = "!", f"{stamp} {entry.outcome} ({worker.name}, {res.minutes:g} min); see reports/{night.date}.md"
            if res.patch:
                night.dir.mkdir(parents=True, exist_ok=True)
                entry.patch_file = str(night.dir / f"{entry.n:02d}-{slug(task.key)}.patch")
                Path(entry.patch_file).write_text(res.patch, encoding="utf-8")
        self.add_lessons(res.lessons)
        mark_task(task.key, mark, note)
        night.add(entry)
        night.write()
        self.commit_pending(f"Night shift: {entry.outcome}: {task.key[:60]}", OWN_PATHS)
        log(f"{entry.outcome}: {task.key[:90]}", worker.name)

    def worker_loop(self, worker: Worker) -> None:
        while True:
            task = self.claim(worker)
            if not task:
                break
            log(f"starting: {first_line(task.key, 120)}", worker.name)
            with self.land_lock:
                base = git(ROOT, "rev-parse", "HEAD").strip()
            left = (self.deadline - dt.datetime.now()).total_seconds() / 60 + 30
            try:
                res = worker.run(task, base, max(20.0, min(self.minutes, left)), worker.model, self.fix_rounds, self.context)
            except Exception as e:
                res = Result(status="error", errors=str(e)[-1500:])
            worker.errors = worker.errors + 1 if res.status == "error" else 0
            try:
                self.land(worker, task, res)
            except Exception as e:
                log(f"couldn't record the result ({e}); the task stays in the queue", worker.name)
            finally:
                with self.queue_lock:
                    self.in_flight.pop(task.key, None)
                    self.write_status()
            if worker.errors >= 2:
                log("two errors in a row; this worker is done for the night", worker.name)
                break

    def prepare_branch(self, date: str) -> str:
        current = git(ROOT, "rev-parse", "--abbrev-ref", "HEAD").strip()
        if current == f"nightshift/{date}" or current.startswith(f"nightshift/{date}-"):
            return current
        name, n = f"nightshift/{date}", 2
        while git(ROOT, "rev-parse", "--verify", "--quiet", f"refs/heads/{name}", check=False).strip():
            name, n = f"nightshift/{date}-{n}", n + 1
        git(ROOT, "switch", "-q", "-c", name)
        return name

    def run(self) -> None:
        log(f"Night shift starting · model {self.model} · no new tasks after {self.deadline:%a %H:%M}")
        ollama = find_ollama(self.cfg)
        if not has_model(ollama, self.model):
            raise RuntimeError(f"Ollama at {ollama} doesn't have {self.model}; run 'ollama pull {self.model}' on the Spark")
        opencode_bin()
        if not (HERE / ".unityrefs" / "Runtime.csproj").exists():
            raise RuntimeError("Unity's reference assemblies aren't on the Spark yet; run 'NightShift.ps1 refs' on the PC")
        if not any(mark == " " for mark, _ in read_tasks()):
            log("tasks.md has no unchecked tasks, so there's nothing to do")
            return
        date = dt.date.today().isoformat()
        self.night = Night(date, self.prepare_branch(date), self.model)
        (ROOT / MAP_REL).write_text(build_project_map(ROOT), encoding="utf-8")
        self.commit_pending("Night shift: queue and project map")
        log(f"on branch {self.night.branch}; checking the project compiles before starting...")
        ok, out = compile_check(ROOT, dict(os.environ))
        if not ok:
            raise RuntimeError("the project doesn't compile before any changes, so no work could be verified:\n" + out[-2500:])
        log(f"loading {self.model} into memory...")
        warm_model(ollama, self.model)
        candidates = [Worker("spark", None, self.cfg, ollama)] if self.cfg["NIGHTSHIFT_LOCAL_WORKER"] != "0" else []
        candidates += [Worker(name, target, self.cfg, ollama) for name, target in helpers(self.cfg)]
        for w in candidates:
            if w.model != self.model and not has_model(ollama, w.model):
                log(f"{w.model} isn't in Ollama on the Spark; {w.name} uses {self.model} instead")
                w.model = self.model
        for extra in sorted({w.model for w in candidates} - {self.model}):
            log(f"loading {extra} into memory...")
            warm_model(ollama, extra)
        taken: set[str] = set()
        for w in candidates:
            name, n = w.name, 2
            while w.name in taken:  # each worker's result ref is named after it, so names must be unique
                w.name, n = f"{name}-{n}", n + 1
            taken.add(w.name)
            problem = w.prepare()
            if problem:
                log(f"not using {w.name}: {problem}")
            else:
                self.workers.append(w)
        if not self.workers:
            raise RuntimeError("no workers are ready")
        self.night.workers = [f"{w.name} ({w.model})" for w in self.workers]
        self.night.model = ", ".join(sorted({w.model for w in self.workers}))
        self.write_status()
        log("workers: " + ", ".join(self.night.workers))
        threads = [threading.Thread(target=self.worker_loop, args=(w,), daemon=True) for w in self.workers]
        for t in threads:
            t.start()
        while any(t.is_alive() for t in threads):
            if STOPPING.wait(2):
                log("stopping: unfinished tasks are rolled back and stay in the queue")
                stoppers = [threading.Thread(target=w.stop, daemon=True) for w in self.workers]
                for s in stoppers:
                    s.start()
                end = time.time() + 45  # nightshift.sh waits 90 s before it kills everything
                for t in stoppers + threads:
                    t.join(timeout=max(0.0, end - time.time()))
                break
        with self.land_lock:
            self.night.unfinished = [t.key for m, t in read_tasks() if m == " " and self.dispatches.get(t.key)]
            self.night.write()
            self.commit_pending("Night shift: report", OWN_PATHS)
        landed = sum(1 for e in self.night.entries if e.outcome == "landed")
        log(f"done: {landed} of {len(self.night.entries)} tasks landed on {self.night.branch}; report in {REL}/reports/{date}.md")


# --- Memory: the project map --------------------------------------------------------------------------------

TYPE_RE = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly)\s+)*"
                     r"(?:class|struct|interface|enum)\s+(\w+)", re.M)


def build_project_map(repo: Path) -> str:
    """One line per script: size, main types and the first sentence of its first summary comment, so the agent
    can find where things live without reading the code (fewer tokens, faster nights)."""
    out = ["# CLONE-Z project map", "",
           "Generated at the start of every night. Each script with its line count, main types and the first",
           "sentence of its summary. Grep the code for details.", ""]
    by_dir: dict[str, list[Path]] = {}
    for f in sorted((repo / "Assets").rglob("*.cs")):
        if "TutorialInfo" not in f.parts:
            by_dir.setdefault(f.parent.relative_to(repo).as_posix(), []).append(f)
    for folder in sorted(by_dir):
        out.append(f"## {folder}")
        for f in by_dir[folder]:
            text = f.read_text(encoding="utf-8-sig", errors="replace")
            types = list(dict.fromkeys(TYPE_RE.findall(text)))[:3]
            summary = ""
            m = re.search(r"///\s*<summary>(.*?)</summary>", text, re.S)
            if m:
                s = re.sub(r"\s*///\s*", " ", m.group(1))
                s = re.sub(r'<(?:see|seealso|paramref|typeparamref)\s+\w+="([^"]+)"\s*/>', r"\1", s)
                s = " ".join(re.sub(r"<[^>]+>", "", s).split())
                summary = re.split(r"(?<=[.!?])\s", s)[0][:100]
            else:  # partial-class files often carry a plain // header comment above the type instead
                first_type = TYPE_RE.search(text)
                head = text[:first_type.start()] if first_type else ""
                header = re.search(r"^\s*//(?!/)\s*(\S.*)$", head, re.M)
                summary = header.group(1).strip()[:100] if header else ""
            line = f"- {f.name} ({text.count(chr(10)) + 1}) {', '.join(types)}"
            out.append(line + (f": {summary}" if summary else ""))
        out.append("")
    return "\n".join(out)


# --- Status, chat and the command line ----------------------------------------------------------------------

def coordinator_pid() -> int | None:
    try:
        pid = int((STATE / "nightshift.pid").read_text().strip())
        os.kill(pid, 0)
        return pid
    except (OSError, ValueError):
        return None


def cmd_status() -> int:
    load_config()
    pid = coordinator_pid()
    print(f"Night shift: {'RUNNING (pid ' + str(pid) + ')' if pid else 'not running'}")
    if STATUS_FILE.exists():
        st = json.loads(STATUS_FILE.read_text(encoding="utf-8"))
        print(f"Branch {st.get('branch') or '-'} · model {st.get('model')} · workers {', '.join(st.get('workers', [])) or '-'}"
              f" · no new tasks after {st.get('deadline')}")
        for key, w in (st.get("working") or {}).items() if pid else []:
            print(f"  {w['worker']:>12} since {w['since']}: {key[:100]}")
    marks = [m for m, _ in read_tasks()]
    print(f"Tasks: {marks.count(' ')} queued · {marks.count('x') + marks.count('X')} landed · {marks.count('!')} failed"
          f" · {marks.count('~')} no change")
    log_file = STATE / "nightshift.log"
    if log_file.exists():
        print("\nLatest log:")
        for line in log_file.read_text(encoding="utf-8", errors="replace").splitlines()[-15:]:
            print("  " + line)
    print("\nRecent commits:")
    print(git(ROOT, "log", "--oneline", "-6", check=False))
    return 0


def cmd_chat(extra: list[str]) -> int:
    cfg = load_config()
    path = write_opencode_config(ROOT, find_ollama(cfg), cfg["NIGHTSHIFT_MODEL"], int(cfg["NIGHTSHIFT_CONTEXT"]))
    env = dict(os.environ, OPENCODE_CONFIG=str(path), OPENCODE_DISABLE_AUTOUPDATE="true", OPENCODE_DISABLE_LSP_DOWNLOAD="true")
    exe = opencode_bin()
    os.execvpe(exe, [exe, str(ROOT), "--model", f"ollama/{cfg['NIGHTSHIFT_MODEL']}", *extra], env)
    return 0


def cmd_run() -> int:
    cfg = load_config()
    STATE.mkdir(parents=True, exist_ok=True)
    import fcntl
    lock = open(STATE / "coordinator.lock", "w")
    try:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)  # held for the coordinator's lifetime
    except OSError:
        log("another night shift coordinator is already running")
        return 0
    pid_file = STATE / "nightshift.pid"
    write_atomic(pid_file, str(os.getpid()))
    FINISH_FLAG.unlink(missing_ok=True)
    for sig in (signal.SIGTERM, signal.SIGINT):
        signal.signal(sig, lambda *_: STOPPING.set())
    try:
        Coordinator(cfg).run()
        return 0
    except Exception as e:
        log(f"night shift stopped: {e}")
        return 1
    finally:
        STATUS_FILE.unlink(missing_ok=True)
        if pid_file.exists() and pid_file.read_text().strip() == str(os.getpid()):
            pid_file.unlink(missing_ok=True)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="CLONE-Z Night Shift")
    sub = parser.add_subparsers(dest="cmd", required=True)
    sub.add_parser("run", help="work through tasks.md (coordinator)")
    sub.add_parser("status", help="progress summary")
    sub.add_parser("map", help="rebuild memory/PROJECT_MAP.md")
    chat = sub.add_parser("chat", help="interactive OpenCode with the night shift's model and settings")
    chat.add_argument("extra", nargs=argparse.REMAINDER)
    work = sub.add_parser("work", help="do one task in a checkout (run by the coordinator)")
    work.add_argument("--repo", required=True)
    work.add_argument("--base", required=True)
    work.add_argument("--task-b64", required=True)
    work.add_argument("--model", required=True)
    work.add_argument("--ollama", required=True)
    work.add_argument("--minutes", type=float, default=120)
    work.add_argument("--fix-rounds", type=int, default=2)
    work.add_argument("--context", type=int, default=131072)
    work.add_argument("--unityrefs", default="")
    args = parser.parse_args(argv)
    if args.cmd == "run":
        return cmd_run()
    if args.cmd == "status":
        return cmd_status()
    if args.cmd == "map":
        (ROOT / MAP_REL).write_text(build_project_map(ROOT), encoding="utf-8")
        print(f"wrote {MAP_REL}")
        return 0
    if args.cmd == "chat":
        return cmd_chat(args.extra)
    return cmd_work(args)


if __name__ == "__main__":
    sys.exit(main())
