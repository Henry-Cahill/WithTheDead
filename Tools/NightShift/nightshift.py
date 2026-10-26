#!/usr/bin/env python3
"""CLONE-Z Night Shift: AI coding agents that work through tasks.md overnight.

OpenCode (`opencode run`) is the agent. Its model runs in Ollama on the DGX Spark (no cloud AI tokens spent), or,
named opencode/<id>, in OpenCode's own cloud: Big Pickle (opencode/big-pickle) is free while the network's allowance
lasts, and NIGHTSHIFT_FALLBACK_MODEL takes over for the rest of the night once it runs out. The coordinator runs
in the AI Workbench project container on the Spark. It hands tasks to
workers (the Spark itself plus any helper machines, Linux or Windows), each of which does its task in its own
checkout, compile-checks it against Unity's assemblies and commits. The coordinator then lands each result on the
night's branch (nightshift/<date>), compiles the whole project again and ticks the task off in tasks.md.

  nightshift.py run           coordinator: work through the queue (nightshift.sh start runs it in the background)
  nightshift.py status        progress summary
  nightshift.py map           rebuild memory/PROJECT_MAP.md
  nightshift.py plan [args]   write one task with the planner's model now (what NIGHTSHIFT_AUTOPLAN does when the queue runs low)
  nightshift.py chat [args]   interactive OpenCode with the same model and settings
  nightshift.py work ...      worker: do one task in a checkout and print the result (run by the coordinator)
"""
from __future__ import annotations

import argparse
import base64
import contextlib
import datetime as dt
import difflib
import functools
import json
import math
import os
import random
import re
import secrets
import shlex
import shutil
import signal
import socket
import struct
import subprocess
import sys
import tempfile
import threading
import time
import urllib.request
from collections import Counter
from dataclasses import asdict, dataclass, field
from pathlib import Path

try:
    import fcntl
except ImportError:  # Windows: only a helper's worker runs there, and it never touches tasks.md
    fcntl = None

WINDOWS = os.name == "nt"  # a worker on a Windows helper, run through Git Bash; the coordinator always runs on Linux
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
    "NIGHTSHIFT_FALLBACK_MODEL": "",
    "NIGHTSHIFT_STOP_AT": "",
    "NIGHTSHIFT_TZ": "America/Chicago",
    "NIGHTSHIFT_TASK_MINUTES": "120",
    "NIGHTSHIFT_FIX_ROUNDS": "2",
    "NIGHTSHIFT_CONTEXT": "131072",
    "NIGHTSHIFT_LOCAL_WORKER": "1",
    "NIGHTSHIFT_HELPER_OLLAMA_URL": "http://192.168.1.252:11434",
    "NIGHTSHIFT_HELPER_DIR": "clonez-nightshift",
    "NIGHTSHIFT_AUTOPLAN": "0",
    "NIGHTSHIFT_AUTOPLAN_FOCUS": "graphics,fixes,perf,graphics,features,flavour,docs",
    "NIGHTSHIFT_AUTOPLAN_MODEL": "",
    "NIGHTSHIFT_SMALL_TASK_MODELS": "",
    "NIGHTSHIFT_DOCS_WORKERS": "",
    "NIGHTSHIFT_PUBLISH_EVERY": "120",
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
# A git lock that nothing has written to for this long was left behind by a git command that was killed partway (by a
# container stop, say). Git never removes such a lock by itself, and every later command that needs it fails.
STALE_LOCK_SECONDS = 300
LOCK_EXISTS_RE = re.compile(r"Unable to create '([^']+\.lock)': File exists")


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


def clear_stale_lock(path: Path) -> bool:
    """Deletes the git lock at `path` if nothing has touched it for STALE_LOCK_SECONDS. True once the lock is gone
    (deleted here, or released meanwhile), so the command that ran into it can go again straight away."""
    try:
        touched = path.stat().st_mtime
        if time.time() - touched < STALE_LOCK_SECONDS:
            return False
        path.unlink()
    except FileNotFoundError:
        return True
    except OSError:
        return False
    log(f"removed {path}: a git command that was killed left it behind "
        f"(untouched since {dt.datetime.fromtimestamp(touched):%a %H:%M})")
    return True


def git_run(repo: Path | str, *args: str, env: dict | None = None) -> subprocess.CompletedProcess:
    for attempt in range(8):
        r = subprocess.run(["git", *GIT_SAFE, "-C", str(repo), *args], capture_output=True, text=True, env=env,
                           stdin=subprocess.DEVNULL)
        # AI Workbench (or someone in a terminal) running git in the same repo holds a lock (index.lock, HEAD.lock) for
        # a moment; wait it out. A lock left behind by a killed git would never go away, so that one is deleted.
        if r.returncode == 0 or ".lock" not in r.stderr or "File exists" not in r.stderr or attempt == 7:
            return r
        held = LOCK_EXISTS_RE.search(r.stderr)
        if not (held and clear_stale_lock(Path(repo, held.group(1)))):
            time.sleep(2)


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
    if not uses_ollama(cfg):
        return candidates[-1].rstrip("/")  # every model is one of OpenCode's own, so Ollama goes unused
    raise RuntimeError("Can't reach Ollama (tried " + ", ".join(candidates) + ")")


def uses_ollama(cfg: dict) -> bool:
    """Whether any model the night shift may use runs in Ollama (the others are OpenCode's own, in its cloud)."""
    keys = ("NIGHTSHIFT_MODEL", "NIGHTSHIFT_HELPER_MODEL", "NIGHTSHIFT_AUTOPLAN_MODEL", "NIGHTSHIFT_FALLBACK_MODEL")
    return any(cfg.get(key, "").strip() and not is_cloud(cfg[key].strip()) for key in keys)


def ollama_models(base: str) -> set[str]:
    with urllib.request.urlopen(base + "/api/tags", timeout=15) as r:
        return {m["name"] for m in json.load(r).get("models", [])}


def has_model(base: str, model: str) -> bool:
    if is_cloud(model):
        return model in cloud_models()
    names = ollama_models(base)
    return model in names or (":" not in model and model + ":latest" in names)


def warm_model(base: str, model: str) -> None:
    """Loads an Ollama model into memory for the night. One of OpenCode's own has nothing to load, so it is asked for a
    word instead: one that has stopped answering (ModelOut) shows up now rather than partway through a task."""
    if is_cloud(model):
        ask_opencode(model, "Reply with the single word OK.", timeout=300)
        return
    body = json.dumps({"model": model, "prompt": "", "keep_alive": "8h"}).encode()
    req = urllib.request.Request(base + "/api/generate", body, {"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=900) as r:
        r.read()


# --- OpenCode's own models ------------------------------------------------------------------------------------
# A model named opencode/<id> is one of OpenCode's own, run in its cloud rather than in Ollama; Big Pickle is
# opencode/big-pickle, free for a limited time. OpenCode's free models answer only OpenCode itself (`opencode run`), have
# an allowance OpenCode doesn't publish, and may use what they're sent to improve the model.

CLOUD_PREFIX = "opencode/"
# OpenCode's errors for one of its models that has stopped answering for now. After the first (a free model's usage
# limit) OpenCode keeps retrying without a word, so an agent would otherwise sit out its task's whole time limit.
MODEL_OUT = re.compile(r"Free usage limit|usage limit reached|quota exceeded|insufficient_quota|FreeTierError"
                       r"|free tier can only be used|ProviderModelNotFoundError", re.I)
LOG_ERRORS = ["--print-logs", "--log-level", "ERROR"]  # how those errors reach us: OpenCode's error log, on stderr
ASK_STEPS = 3  # a plain question takes one step; a model that keeps calling tools it hasn't got is cut off after this
ASK_MESSAGE = "Answer the request below. Everything you need is in it, so answer directly, without using any tools."


class ModelOut(RuntimeError):
    """One of OpenCode's own models stopped answering: its free usage limit was reached, say."""

    def __init__(self, model: str, why: str):
        super().__init__(f"{model} stopped answering: {why}")
        self.model, self.why = model, why


def is_cloud(model: str) -> bool:
    return model.startswith(CLOUD_PREFIX)


def opencode_model(model: str) -> str:
    """The provider/model OpenCode knows a model by: one of its own as it is, an Ollama model under ollama/."""
    return model if is_cloud(model) else f"ollama/{model}"


def model_home(model: str) -> str:
    return "OpenCode's cloud" if is_cloud(model) else "Ollama on the DGX Spark"


def why_out(text: str) -> str:
    """The gist of an error MODEL_OUT matched, e.g. "Free usage limit reached"."""
    m = MODEL_OUT.search(text)
    return text[m.start():].split('"')[0].split(". ")[0].strip()[:160] if m else text.strip()[:160]


def cloud_models() -> set[str]:
    """The models OpenCode's own provider offers here; without an account, its free ones (opencode/big-pickle...)."""
    try:
        r = subprocess.run([opencode_bin(), "models", "opencode"], capture_output=True, text=True, timeout=120,
                           stdin=subprocess.DEVNULL, env=dict(os.environ, OPENCODE_DISABLE_AUTOUPDATE="true"))
    except (OSError, RuntimeError, subprocess.TimeoutExpired):
        return set()
    names = (re.sub(r"\x1b\[[0-9;]*m", "", line).strip() for line in r.stdout.splitlines())
    return {name for name in names if name.startswith(CLOUD_PREFIX)}


def ask_config(model: str) -> dict:
    """OpenCode's settings for a plain question (the planner's, or whether a model answers at all). Big Pickle's free
    tier turns away a request that offers no tools, with the same "Free usage limit reached" as when the allowance is
    used up, so the read-only tools stay: in an empty folder, with everything outside it out of bounds, they can't
    read a thing."""
    read_only = {tool: "allow" for tool in ("read", "glob", "grep", "list")}
    return {"$schema": "https://opencode.ai/config.json", "autoupdate": False, "share": "disabled", "lsp": False,
            "model": opencode_model(model), "small_model": opencode_model(model), "instructions": [],
            "agent": {"build": {"steps": ASK_STEPS}},
            "permission": {"*": "deny", **read_only, "external_directory": "deny"}}


def write_and_close(pipe, text: str) -> None:
    try:
        pipe.write(text)
        pipe.close()
    except (OSError, ValueError):  # the process ended without reading it all
        pass


def ask_opencode(model: str, prompt: str, timeout: float = 900) -> str:
    """One answer from one of OpenCode's own models, which answer only OpenCode: `opencode run` in an empty folder (see
    ask_config), the prompt piped in (a script shown whole can be too long for a command line). Any reasoning is left
    out. Raises ModelOut when the model has stopped answering (see MODEL_OUT)."""
    with tempfile.TemporaryDirectory(prefix="nightshift-ask-") as tmp:
        work = Path(tmp) / "work"
        work.mkdir()
        cfg_path = Path(tmp) / "opencode.json"
        cfg_path.write_text(json.dumps(ask_config(model)), encoding="utf-8")
        cmd = [opencode_bin(), "run", "--format", "json", "--model", opencode_model(model), "--dir", str(work),
               "--title", "nightshift: question", *LOG_ERRORS, ASK_MESSAGE]
        env = dict(os.environ, OPENCODE_CONFIG=str(cfg_path), OPENCODE_DISABLE_AUTOUPDATE="true",
                   OPENCODE_DISABLE_LSP_DOWNLOAD="true")
        # No preexec_fn (die_with_parent) here: the coordinator runs other threads, and forking with it isn't safe there.
        # The question ends with the timeout, or when the night shift stops (watch).
        proc = subprocess.Popen(cmd, cwd=work, env=env, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True, encoding="utf-8", errors="replace",
                                start_new_session=True)
        threading.Thread(target=write_and_close, args=(proc.stdin, prompt), daemon=True).start()
        expired, done = threading.Event(), threading.Event()
        timer = threading.Timer(timeout, lambda: (expired.set(), terminate(proc, 10)))
        timer.start()

        def watch() -> None:
            while not done.wait(1):
                if STOPPING.is_set():
                    terminate(proc, 5)
                    return

        threading.Thread(target=watch, daemon=True).start()
        texts: dict[str, list[str]] = {}
        last, error, out = "", "", ""
        try:
            for line in proc.stdout:
                try:
                    ev = json.loads(line)
                except ValueError:
                    if MODEL_OUT.search(line):
                        out = line
                        break
                    continue
                if not isinstance(ev, dict):
                    continue
                kind, part = ev.get("type"), ev.get("part") or {}
                if kind == "text" and (part.get("text") or "").strip():
                    last = part.get("messageID", "")
                    texts.setdefault(last, []).append(part["text"])
                elif kind == "error":
                    error = json.dumps(ev.get("error") or ev)[:400]
                    if MODEL_OUT.search(error):
                        out = error
                        break
        finally:
            done.set()
            timer.cancel()
            terminate(proc, grace=5)
    if out:
        raise ModelOut(model, why_out(out))
    if error:
        raise RuntimeError(f"{model}: {error}")
    if expired.is_set():
        raise RuntimeError(f"{model} gave no answer within {timeout:.0f} s")
    reply = "\n".join(texts.get(last, []))
    if not reply.strip() and proc.returncode:
        raise RuntimeError(f"opencode run exited with code {proc.returncode} without an answer from {model}")
    return re.sub(r"<think>.*?</think>", "", reply, flags=re.S).strip()


# --- OpenCode -----------------------------------------------------------------------------------------------

def opencode_bin() -> str:
    found = shutil.which("opencode")
    if found and not found.lower().endswith((".cmd", ".bat")):  # npm's Windows shim loses a multi-line prompt
        return found
    for candidate in ("/usr/local/bin/opencode", "~/.local/bin/opencode", "~/.opencode/bin/opencode",
                      "~/.local/bin/opencode.exe"):
        path = Path(candidate).expanduser()
        if path.exists():
            return str(path)
    raise RuntimeError("OpenCode is not installed (the Workbench container build installs it; helpers get it from NightShift.ps1 helpers)")


@functools.lru_cache(maxsize=None)
def bash_exe() -> str:
    """The bash that runs check.sh and the agent's shell commands. On Windows that's Git for Windows' bash: a bare "bash"
    there finds WSL's in System32 first, and OpenCode would use PowerShell."""
    if not WINDOWS:
        return "bash"
    roots = []
    try:  # <Git>/ucrt64/libexec/git-core, or mingw64 in older releases
        exec_path = subprocess.run(["git", "--exec-path"], capture_output=True, text=True, stdin=subprocess.DEVNULL).stdout
        if exec_path.strip():
            roots.append(Path(exec_path.strip()).parents[2])
    except OSError:
        pass
    roots.append(Path(os.environ.get("ProgramFiles", r"C:\Program Files")) / "Git")
    for root in roots:
        if (root / "bin" / "bash.exe").is_file():
            return str(root / "bin" / "bash.exe")
    raise RuntimeError("Git for Windows' bash isn't installed (WindowsHelper.ps1 installs Git)")


def opencode_config(base_url: str, model: str, context: int, repo: Path) -> dict:
    models = {}
    for name in dict.fromkeys([model, "qwen3.8", "gpt-oss:120b"]):
        if not is_cloud(name):  # OpenCode knows its own models' limits
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
        "model": opencode_model(model),
        "small_model": opencode_model(model),
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
    model_out: bool = False  # the model stopped answering (see MODEL_OUT), so the agent was stopped


def kill_group(proc: subprocess.Popen, sig: int = signal.SIGTERM) -> None:
    if WINDOWS:  # no process groups: end the process and everything it started (console programs ignore a polite stop)
        subprocess.run(["taskkill", "/PID", str(proc.pid), "/T", "/F"], capture_output=True, stdin=subprocess.DEVNULL)
        return
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
        kill_group(proc, getattr(signal, "SIGKILL", signal.SIGTERM))
        proc.wait()


def die_with_parent() -> None:
    """preexec_fn: have Linux kill the child if this process dies, so an agent never outlives its worker."""
    try:
        import ctypes
        ctypes.CDLL("libc.so.6", use_errno=True).prctl(1, signal.SIGKILL)  # PR_SET_PDEATHSIG
    except Exception:
        pass


WINDOWS_JOB = None  # the worker's job object on Windows; held open for as long as the worker runs


def windows_job() -> None:
    """Windows has no process groups, and no PR_SET_PDEATHSIG for die_with_parent(). Instead the worker puts itself in a
    job object that ends every process it starts (the agent, its shell, dotnet) when the worker ends, however it ends.
    The job also runs them all at below-normal CPU priority, as nice does on the Linux helpers."""
    global WINDOWS_JOB
    import ctypes
    from ctypes import wintypes

    class Basic(ctypes.Structure):
        _fields_ = [("PerProcessUserTimeLimit", ctypes.c_int64), ("PerJobUserTimeLimit", ctypes.c_int64),
                    ("LimitFlags", wintypes.DWORD), ("MinimumWorkingSetSize", ctypes.c_size_t),
                    ("MaximumWorkingSetSize", ctypes.c_size_t), ("ActiveProcessLimit", wintypes.DWORD),
                    ("Affinity", ctypes.c_size_t), ("PriorityClass", wintypes.DWORD), ("SchedulingClass", wintypes.DWORD)]

    class Extended(ctypes.Structure):
        _fields_ = [("BasicLimitInformation", Basic), ("IoInfo", ctypes.c_uint64 * 6), ("ProcessMemoryLimit", ctypes.c_size_t),
                    ("JobMemoryLimit", ctypes.c_size_t), ("PeakProcessMemoryUsed", ctypes.c_size_t),
                    ("PeakJobMemoryUsed", ctypes.c_size_t)]

    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel32.CreateJobObjectW.restype = wintypes.HANDLE
    kernel32.CreateJobObjectW.argtypes = (ctypes.c_void_p, wintypes.LPCWSTR)
    kernel32.SetInformationJobObject.argtypes = (wintypes.HANDLE, ctypes.c_int, ctypes.c_void_p, wintypes.DWORD)
    kernel32.AssignProcessToJobObject.argtypes = (wintypes.HANDLE, wintypes.HANDLE)
    kernel32.GetCurrentProcess.restype = wintypes.HANDLE
    job = kernel32.CreateJobObjectW(None, None)
    info = Extended()
    # KILL_ON_JOB_CLOSE | PRIORITY_CLASS | BREAKAWAY_OK (a program that asks to leave the job may, rather than fail to start)
    info.BasicLimitInformation.LimitFlags = 0x2000 | 0x20 | 0x800
    info.BasicLimitInformation.PriorityClass = 0x4000  # BELOW_NORMAL_PRIORITY_CLASS
    if not job or not kernel32.SetInformationJobObject(job, 9, ctypes.byref(info), ctypes.sizeof(info)) \
            or not kernel32.AssignProcessToJobObject(job, kernel32.GetCurrentProcess()):
        log(f"  couldn't set up the worker's job object (Windows error {ctypes.get_last_error()}); "
            "an agent may outlive a stopped task")
        return
    WINDOWS_JOB = job


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
    text = f"{part.get('tool', '?')} {target}".replace(str(repo) + os.sep, "").replace(str(repo) + "/", "").replace("\n", " ")
    if state.get("status") == "error":
        text += "  (failed)"
    return text[:180]


def run_opencode(repo: Path, prompt: str, cfg_path: Path, model: str, timeout_s: float, log_file: Path,
                 env: dict, session: str | None = None, title: str | None = None) -> AgentRun:
    cloud = is_cloud(model)
    cmd = [opencode_bin(), "run", "--auto", "--format", "json", "--model", opencode_model(model), "--dir", str(repo)]
    if cloud:
        cmd += LOG_ERRORS  # how a model that has stopped answering shows up (see MODEL_OUT)
    if session:
        cmd += ["--session", session]
    elif title:
        cmd += ["--title", title[:80]]
    cmd.append(prompt)
    env = dict(env, OPENCODE_CONFIG=str(cfg_path), OPENCODE_DISABLE_AUTOUPDATE="true", OPENCODE_DISABLE_LSP_DOWNLOAD="true")
    if WINDOWS:  # OpenCode on Windows runs shell commands in PowerShell unless pointed at Git Bash
        env.update(SHELL=bash_exe(), OPENCODE_GIT_BASH_PATH=bash_exe())
    res = AgentRun()
    texts: dict[str, list[str]] = {}
    last_msg = ""
    proc = subprocess.Popen(cmd, cwd=repo, env=env, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, text=True, bufsize=1, start_new_session=True,
                            preexec_fn=None if WINDOWS else die_with_parent)

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
                        if cloud and not res.model_out and MODEL_OUT.search(line):
                            res.model_out, res.error = True, why_out(line)
                            stop_agent(False)
                    continue
                res.session = res.session or ev.get("sessionID")
                kind, part = ev.get("type"), ev.get("part") or {}
                if kind == "tool_use":
                    res.tools += 1
                    log("  " + describe_tool(part, repo))
                elif kind == "text" and (part.get("text") or "").strip():
                    last_msg = part.get("messageID", "")
                    texts.setdefault(last_msg, []).append(part["text"].strip())
                    log("  > " + " ".join(part["text"].split())[:170])
                elif kind == "step_finish":
                    tokens = part.get("tokens") or {}
                    res.tokens_in += tokens.get("input", 0)
                    res.tokens_out += tokens.get("output", 0) + tokens.get("reasoning", 0)
                    res.tokens_cached += (tokens.get("cache") or {}).get("read", 0)
                elif kind == "error":
                    res.error = json.dumps(ev.get("error") or ev)[:400]
                    log("  agent error: " + res.error)
                    if cloud and MODEL_OUT.search(res.error):
                        res.model_out, res.error = True, why_out(res.error)
        proc.wait()
    finally:
        timer.cancel()
        terminate(proc, grace=5)
    res.rc = proc.returncode
    res.text = "\n".join(texts.get(last_msg, []))
    return res


# --- Checks and clean-up in a worker checkout ---------------------------------------------------------------

def compile_check(repo: Path, env: dict) -> tuple[bool, str]:
    rc, out = run_in_group([bash_exe(), str(repo / REL / "check.sh")], timeout=1200, cwd=repo, env=env)
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


def split_lines(text: str) -> list[str]:
    """`text` split after each newline, endings kept: lines as git counts them (str.splitlines also splits at form
    feeds, \\u2028 and the like, which can sit inside a string literal)."""
    return re.findall(r"[^\n]*\n|[^\n]+", text)


@dataclass(frozen=True)
class CsLine:
    """One line of C# source, as scan_cs reads it."""
    code: str  # its code, with what's inside strings and chars left out and comments taken off
    comment_at: int | None  # where the // comment that ends it starts, if one does
    commented: bool  # whether any of it is a comment: //, /// or /* */
    safe: bool  # whether it starts outside strings and /* */ comments, so that a comment line may go just above it


CS_PLAIN = re.compile(r"""[^"'/@$]*\Z""")  # nothing on the line can start a string, a char or a comment
CS_CODE_STOP = re.compile(r"""["'/@${}]""")
CS_STRING_STOP = {"s": re.compile(r'[\\"]'), "v": re.compile('"'), "i": re.compile(r'[\\"{}]'), "iv": re.compile('["{}]')}
CS_OPENERS = (('$@"', "iv"), ('@$"', "iv"), ('@"', "v"), ('$"', "i"), ('"', "s"))


def scan_cs(text: str) -> list[CsLine]:
    """The lines of C# source `text` (as split_lines splits them), each with its code, its comments and whether it
    starts inside a string or /* */ comment that began on an earlier line. It knows C# 9's strings (regular, verbatim
    and interpolated, holes and all), chars, comments and preprocessor lines."""
    out: list[CsLine] = []
    stack: list[str] = []  # what's open: strings "s", "v" (verbatim), "i", "iv" (interpolated); "h", a hole in one
    holes: list[int] = []  # the brace depth inside each open hole
    block = False  # inside a /* */ comment
    for raw in split_lines(text):
        line = raw.rstrip("\r\n")
        safe = not stack and not block
        if safe and line.lstrip().startswith("#"):  # a preprocessor line: a #region's name may hold an apostrophe
            out.append(CsLine("", None, False, True))
            continue
        if safe and CS_PLAIN.match(line):
            out.append(CsLine(line, None, False, True))
            continue
        code: list[str] = []
        comment_at, commented, i, n = None, False, 0, len(line)
        while i < n:
            if block:
                commented = True
                end = line.find("*/", i)
                if end < 0:
                    break
                block, i = False, end + 2
                continue
            top = stack[-1] if stack else ""
            if top in CS_STRING_STOP:  # inside a string only its end, its escapes and its holes matter
                m = CS_STRING_STOP[top].search(line, i)
                if not m:
                    break
                i, c = m.start(), m.group()
                nxt = line[i + 1:i + 2]
                if c == "\\" or (c == '"' and top in ("v", "iv") and nxt == '"'):
                    i += 2
                elif c == '"':
                    stack.pop()
                    i += 1
                    if not stack:
                        code.append('"')
                elif c == nxt:  # {{ or }}: a brace in the text
                    i += 2
                elif c == "{":
                    stack.append("h")
                    holes.append(0)
                    i += 1
                else:
                    i += 1
                continue
            m = CS_CODE_STOP.search(line, i)
            if not m:
                if not top:
                    code.append(line[i:])
                break
            if not top:
                code.append(line[i:m.start()])
            i, c = m.start(), m.group()
            nxt = line[i + 1:i + 2]
            if c == "/" and not top and nxt in ("/", "*"):
                commented = True
                if nxt == "/":
                    comment_at = i
                    break
                block, i = True, i + 2
                continue
            opened = next(((o, kind) for o, kind in CS_OPENERS if line.startswith(o, i)), None)
            if opened:
                stack.append(opened[1])
                if not top:
                    code.append('"')
                i += len(opened[0])
                continue
            if c == "'":
                end = line.find("'", i + 3) if nxt == "\\" else i + 2 if line[i + 2:i + 3] == "'" else -1
                if end > 0:
                    if not top:
                        code.append("' '")
                    i = end + 1
                    continue
            if top == "h" and c in "{}":
                if c == "{":
                    holes[-1] += 1
                elif holes[-1]:
                    holes[-1] -= 1
                else:
                    stack.pop()
                    holes.pop()
                i += 1
                continue
            if not top:
                code.append(c)
            i += 1
        # A regular string can't run on past its line, so what's still open of one is broken code: recover from it.
        while stack and (stack[-1] in ("s", "i") or (stack[-1] == "h" and stack[-2] == "i")):
            if stack.pop() == "h":
                holes.pop()
        out.append(CsLine("".join(code), comment_at, commented, safe))
    return out


def already_documented(lines: list[str], at: int) -> bool:
    """Whether a /// comment is already beside the spot just before lines[at]: above it (blank lines and attributes
    passed over) or below it (blank lines passed over). A summary inserted there would be the member's second."""
    above = at - 1
    while above >= 0 and (not lines[above].strip() or lines[above].strip().startswith("[")):
        above -= 1
    below = at
    while below < len(lines) and not lines[below].strip():
        below += 1
    return ((above >= 0 and lines[above].strip().startswith("///"))
            or (below < len(lines) and lines[below].strip().startswith("///")))


def code_key(line: str, scanned: CsLine) -> str:
    """What new_comments_only matches lines on: the line without whitespace, and without the // comment that ends a
    line of code, so a line the agent only added a comment to still matches."""
    if scanned.comment_at is not None and scanned.code.strip():
        line = line[:scanned.comment_at]
    return "".join(line.split())


def new_comments_only(old: str, new: str) -> str:
    """`old` plus the comment lines `new` adds, and nothing else `new` changes: code, existing comments, blank lines and
    indentation stay as in `old`. Lines are matched on their code, ignoring whitespace, so a declaration the agent
    re-indented keeps its old indentation and gets its new summary, indented like it, and a comment the agent put at the
    end of a line of code goes on a line of its own above that line. Left out: comment lines inside a string or /* */
    comment that runs over several lines (one there would change the string), a /// block for a member that has one
    already (see already_documented), and anything after the last line."""
    bom = "\ufeff" if old.startswith("\ufeff") else ""
    old, new = old[len(bom):], new.removeprefix("\ufeff")
    a, b, sa, sb = split_lines(old), split_lines(new), scan_cs(old), scan_cs(new)
    eol = "\r\n" if "\r\n" in old else "\n"
    out: list[str] = []

    def indent(line: str) -> str:
        return line[:len(line) - len(line.lstrip())]

    matcher = difflib.SequenceMatcher(None, [code_key(l, s) for l, s in zip(a, sa)],
                                      [code_key(l, s) for l, s in zip(b, sb)], autojunk=False)
    for op, i1, i2, j1, j2 in matcher.get_opcodes():
        if op == "equal":
            for i, j in zip(range(i1, i2), range(j1, j2)):
                ending = b[j][sb[j].comment_at:].strip() if sb[j].comment_at is not None and sb[j].code.strip() else ""
                if ending and sa[i].safe and sa[i].comment_at is None \
                        and not (ending.startswith("///") and already_documented(a, i)):
                    out.append(indent(a[i]) + ending + eol)
                out.append(a[i])
            continue
        added = [l.strip() for l in b[j1:j2] if l.strip()]
        if op == "insert" and added and i1 < len(a) and sa[i1].safe and all(l.startswith("//") for l in added) \
                and not (any(l.startswith("///") for l in added) and already_documented(a, i1)):
            below = next((l for l in a[i1:] if l.strip()), "")
            out += [indent(below) + l + eol for l in added]
        out += a[i1:i2]
    return bom + "".join(out)


def keep_only_new_comments(repo: Path, base: str) -> list[str]:
    """For a documentation task: changes back everything the agent changed except the comment lines it added to existing
    scripts (see new_comments_only), so the task lands comments and nothing else. Files it added go, files it deleted
    come back. Returns the files changed back, wholly or in part. Protected files and LESSONS.md are left to
    revert_protected and take_lessons."""
    put_back = []
    for path in changed_files(repo, base):
        if PROTECTED.search(path) or path == LESSONS_REL:
            continue
        full = repo / path
        if not in_commit(repo, base, path):
            full.unlink(missing_ok=True)
        elif path.endswith(".cs") and full.is_file():
            old = subprocess.run(["git", *GIT_SAFE, "-C", str(repo), "show", f"{base}:{path}"], capture_output=True,
                                 stdin=subprocess.DEVNULL, check=True).stdout
            new = full.read_bytes()
            kept = new_comments_only(old.decode("utf-8", "surrogateescape"),
                                     new.decode("utf-8", "surrogateescape")).encode("utf-8", "surrogateescape")
            if kept == new:
                continue
            full.write_bytes(kept)
        else:
            git(repo, "checkout", base, "--", path)
        put_back.append(path)
    return put_back


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
    model: str = ""  # the model the agent ran on
    model_out: bool = False  # the model stopped answering partway (see MODEL_OUT); errors says why


def do_task(repo: Path, base: str, task: str, model: str, ollama: str, minutes: float, fix_rounds: int,
            context: int, unityrefs: str) -> Result:
    started = time.time()
    deadline = started + minutes * 60
    res = Result(model=model)
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
        if run.model_out:
            res.model_out, res.errors = True, run.error
            trouble.append(f"{model} stopped answering: {run.error}")
        elif run.timed_out:
            trouble.append(f"the agent hit the {minutes:.0f}-minute time limit")
        elif run.error:
            trouble.append("the agent reported an error: " + run.error)
        elif run.rc != 0 and not STOPPING.is_set():
            trouble.append(f"the agent exited with code {run.rc}")
        if trouble:
            log("  " + trouble[-1][:200])
        return run

    first = agent(PROMPT.format(task=task))
    if res.model_out:  # not the task's doing: nothing it did is checked or kept, and the task goes back in the queue
        res.status = "error"
        res.minutes = round((time.time() - started) / 60, 1)
        return res
    reverted = revert_protected(repo, base)
    if reverted:
        log("  rolled back protected files: " + ", ".join(reverted[:8]))
    docs = task.startswith(DOC_STARTS)  # a documentation task lands the comments it adds and nothing else
    if docs:
        put_back = keep_only_new_comments(repo, base)
        if put_back:
            log("  kept only the comments it added; changed the rest back in: " + ", ".join(put_back[:8]))
    res.lessons += take_lessons(repo, base)
    ok, out = compile_check(repo, env)
    rounds = 0
    while not ok and not trouble and rounds < fix_rounds and time.time() < deadline - 120 and not STOPPING.is_set():
        rounds += 1
        log(f"  compile check failed; fix round {rounds}/{fix_rounds}")
        agent(FIX_PROMPT.format(errors=out[-6000:]), session=first.session)
        revert_protected(repo, base)
        if docs:
            keep_only_new_comments(repo, base)
        res.lessons += take_lessons(repo, base)
        ok, out = compile_check(repo, env)
    sync_meta(repo, base)
    res.files = changed_files(repo, base)
    res.minutes = round((time.time() - started) / 60, 1)
    git(repo, "add", "-A")
    if STOPPING.is_set():
        res.status, res.errors = "error", "stopped"
    elif res.model_out:  # partway through the fix rounds: the task goes back in the queue, as above
        res.status = "error"
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
    if hasattr(signal, "SIGHUP"):
        signal.signal(signal.SIGHUP, lambda *_: STOPPING.set())
    if WINDOWS:
        windows_job()
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
TASKS_THREAD_LOCK = threading.Lock()
TASKS_LOCK_PATH = STATE / "tasks.lock"


@contextlib.contextmanager
def tasks_lock():
    with TASKS_THREAD_LOCK:
        STATE.mkdir(parents=True, exist_ok=True)
        with TASKS_LOCK_PATH.open("a+b") as lock:
            if fcntl:
                fcntl.flock(lock, fcntl.LOCK_EX)
            try:
                yield
            finally:
                if fcntl:
                    fcntl.flock(lock, fcntl.LOCK_UN)


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
    with tasks_lock():
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


def with_mark(text: str, key: str, mark: str, note: str) -> str | None:
    """`text` (tasks.md) with the unchecked task `key` marked and `note` added under it, or None if it isn't there."""
    lines = text.splitlines()
    for i, line in enumerate(lines):
        m = BOX.match(line)
        if m and m.group(2) == " " and m.group(3) == key:
            lines[i] = f"{m.group(1)}- [{mark}] {m.group(3)}"
            lines.insert(_block_end(lines, i, len(m.group(1))), " " * (len(m.group(1)) + 2) + "- " + note)
            return "\n".join(lines) + "\n"
    return None


def merge_task_edits(base: bytes, current: bytes, edited: bytes) -> bytes | None:
    base_lines = base.splitlines(keepends=True)

    def changes(text: bytes) -> list[tuple[int, int, list[bytes]]]:
        lines = text.splitlines(keepends=True)
        return [(i1, i2, lines[j1:j2]) for tag, i1, i2, j1, j2 in
                difflib.SequenceMatcher(None, base_lines, lines, autojunk=False).get_opcodes() if tag != "equal"]

    left, right = changes(current), changes(edited)
    merged = list(left)
    for start, end, replacement in right:
        duplicate = next((i for i, (other_start, other_end, other_replacement) in enumerate(merged)
                          if (start, end) == (other_start, other_end)), None)
        if duplicate is not None:
            other_start, other_end, other_replacement = merged[duplicate]
            if replacement == other_replacement:
                continue
            if start == end:
                merged[duplicate] = (start, end, other_replacement + [line for line in replacement
                                                                       if line not in other_replacement])
                continue
            return None
        if any(max(start, other_start) < min(end, other_end)
               or (start == end and other_start < start < other_end)
               or (other_start == other_end and start < other_start < end)
               for other_start, other_end, _ in merged):
            return None
        merged.append((start, end, replacement))

    result, cursor = [], 0
    for start, end, replacement in sorted(merged, key=lambda change: (change[0], change[1])):
        if start < cursor:
            return None
        result.extend(base_lines[cursor:start])
        result.extend(replacement)
        cursor = end
    result.extend(base_lines[cursor:])
    return b"".join(result)


def reconcile_task_edits(base: bytes, edited: bytes, mark: tuple[str, str, str] | None = None) -> bool:
    for _ in range(10):
        latest = TASKS.read_bytes()
        merged = merge_task_edits(base, latest, edited)
        if merged is None:
            return False
        if TASKS.read_bytes() != latest:
            continue
        text = merged.decode("utf-8", "surrogateescape")
        redone = with_mark(text, *mark) if mark else None
        TASKS.write_bytes((text if redone is None else redone).encode("utf-8", "surrogateescape"))
        return True
    return False


def mark_task(key: str, mark: str, note: str, path: Path = TASKS) -> None:
    with tasks_lock():
        text = with_mark(path.read_text(encoding="utf-8"), key, mark, note)
        if text is not None:
            write_atomic(path, text)


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
        self.switches: list[str] = []  # when and why the night moved from a cloud model to NIGHTSHIFT_FALLBACK_MODEL
        self.tokens = [0, 0, 0]  # in, out, cached
        # A second run on the same date gets its own report rather than overwriting the first.
        name = date if not (REPORTS / f"{date}.md").exists() else f"{date}-{self.started:%H%M}"
        self.dir = REPORTS / name
        self.file = REPORTS / f"{name}.md"

    def add(self, entry: Entry) -> None:
        self.entries.append(entry)

    def write(self) -> None:
        REPORTS.mkdir(parents=True, exist_ok=True)
        count = lambda o: sum(1 for e in self.entries if e.outcome == o)
        names = [m.strip() for m in self.model.split(",") if m.strip()]
        local = ", ".join(m for m in names if not is_cloud(m))
        cloud = ", ".join(m for m in names if is_cloud(m))
        if cloud:
            where = f"`{cloud}` in OpenCode's cloud" + (f" and `{local}` in Ollama on the DGX Spark" if local else "")
        else:
            where = f"`{self.model}` in Ollama on the DGX Spark (no cloud tokens)"
        lines = [
            f"# Night shift {self.date}",
            "",
            f"Branch `{self.branch}` · model {where} · workers: {', '.join(self.workers) or '-'}",
            f"Started {self.started:%H:%M} · updated {dt.datetime.now():%H:%M} · {count('landed')} landed · "
            f"{count('failed') + count('gave up')} failed · {count('no change')} no change · "
            f"{self.tokens[0] + self.tokens[1]:,} tokens processed{'' if cloud else ' locally'} "
            f"({self.tokens[2]:,} from cache)",
            *(f"Switched at {note}" for note in self.switches),
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
                lines += ["", f"The attempt is saved in [{Path(e.patch_file).name}]({self.dir.name}/{Path(e.patch_file).name})."]
            if e.errors:
                lines += ["", "```", e.errors.strip()[-1500:], "```"]
        if self.unfinished:
            lines += ["", "## Unfinished", "", "Tried tonight but neither landed nor failed for good (stopped, time ran out or the "
                      "worker kept erroring); they are still unchecked in tasks.md:", ""]
            lines += [f"- {t}" for t in self.unfinished]
        self.file.write_text("\n".join(lines) + "\n", encoding="utf-8")


# --- Auto-planning: a queue that never runs dry -------------------------------------------------------------

AUTOPLAN_STATE = STATE / "autoplan.json"  # the focus rotation and when each script was last shown to the planner
PLAN_MAX_LINES = 1200  # longer scripts aren't shown to the planner (it reads them whole), nor is ItemModels.cs
PLAN_PRIMITIVES = re.compile(r"\.(?:Box|BoxOn|Quad|QuadUV|Flat|FlatUV|Upright|Tri|Diamond|Blob|FlatBlob|Cylinder|CylinderX|"
                             r"Ellipsoid|Rod)\s*\(|CreatePrimitive")  # 3D shapes only: colours alone are UI, not objects
PLAN_FRAME = re.compile(r"\bvoid\s+(?:Update|LateUpdate|FixedUpdate)\s*\(")
PLAN_FRAME_WORK = re.compile(r"GetComponent|FindObjectsByType|FindAnyObjectByType|FindObjectOfType|Camera\.main|\.text\s*=|"
                             r"string\.Format|\$\"|\.ToList\(\)|\.Where\(|\.Select\(|new List<")
PLAN_PROSE = re.compile(r"\"[A-Z][^\"\n]{30,}[.!?…]\"")
PLAN_BRANCHES = re.compile(r"\b(?:if|while|for|foreach|switch)\s*\(")
PUBLIC_MEMBER = re.compile(  # a public method or property: a type, then the name (constructors and overrides don't count)
    r"^\s*public\s+(?:(?:static|virtual|async|new|unsafe|extern|sealed)\s+)*"
    r"(?!(?:class|struct|enum|interface|delegate|const|event|readonly|abstract|partial|record|override|operator|implicit|"
    r"explicit)\b)[\w.]+(?:<[^>]*>)?(?:\[[,\s]*\])?\??\s+(\w+)\s*(?:<[^>]*>)?\s*(?:\(|\{|=>)")
INHERITS_DOCS = {"DisplayName", "GetActions", "SaveKey", "CaptureState", "RestoreState", "CanDemolish", "TakeDamage", "Dispose",
                 "Order", "Init", "ToString", "Equals", "GetHashCode"}
TASK_LINE = re.compile(r"^\s*(?:[-*+]|\d+[.)])\s*\[ ?\]\s+(\S.*)$")
PATH_MENTION = re.compile(r"\b(?:Assets|ProjectSettings|Packages|Tools)/[\w./-]*[\w/]")
MENTION_OK = {f"{REL}/check.sh", LESSONS_REL}  # tasks may name these without touching them
# The map scraper's data is Project Zomboid's map, kept on the PC for reference only; it must never reach the game.
OFF_LIMITS = re.compile(r"(?i)mapscraper|zomboidmap|(?<![\w/])ref/")
MAP_DIR = "Assets/Scripts/World/Map/"  # the real-world raid map (OpenStreetMap) and the code that builds it
BIG = "+"  # a focus written with a trailing "+" (map+, fixes+...) asks for one substantial, multi-step task
PLAN_LESSONS_CHARS = 16000  # how much of LESSONS.md the planner reads (all of it, so far)
PLAN_RELATED = 3  # related scripts shown, in outline, beside a big task's script
PLAN_OUTLINE_LINES = 60  # declarations kept per related script
IDENT = re.compile(r"\b[A-Z]\w*")  # a capitalised name: a type, most likely
SIGNATURE = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:public|internal|protected)\s")  # a member other scripts can use
STEP = re.compile(r"^step[ \t]*\d+[ \t]*[:.)-]?[ \t]*", re.I | re.M)  # "Step 1: ", how a big task spells out its work


@dataclass(frozen=True)
class Size:
    """How much a planned task may say and reach: detail lines kept, characters, methods it may call that don't
    exist yet (helpers it asks for), new scripts it may create, and the fewest steps ("Step 1:"...) it needs."""
    details: int
    chars: int
    new_methods: int
    new_scripts: int
    min_steps: int


SMALL_TASK = Size(details=8, chars=3000, new_methods=1, new_scripts=0, min_steps=0)
BIG_TASK = Size(details=16, chars=6000, new_methods=3, new_scripts=1, min_steps=2)


def is_big(task: Task) -> bool:
    """Whether `task` is big: written in steps ("Step 1:", "Step 2:"...), as the planner writes its big tasks, or longer
    than a small task may be. Workers on NIGHTSHIFT_SMALL_TASK_MODELS leave big tasks to the others."""
    return (len(STEP.findall(task.text)) >= BIG_TASK.min_steps or task.text.count("\n") > SMALL_TASK.details
            or len(task.text) > SMALL_TASK.chars)


def undocumented(text: str) -> int:
    """How many public methods and properties in a script have no /// comment above them (attributes skipped)."""
    lines, count = text.splitlines(), 0
    for i, line in enumerate(lines):
        m = PUBLIC_MEMBER.match(line)
        if not m or m.group(1) in INHERITS_DOCS:
            continue
        j = i - 1
        while j >= 0 and lines[j].strip().startswith("["):
            j -= 1
        if j < 0 or not lines[j].strip().startswith("///"):
            count += 1
    return count


EXPLAIN_BODY = 12  # code lines a method needs before its logic is worth explaining inside it
EXPLAIN_STRETCH = 25  # ...or code lines in a row with no comment among them, in a method with comments elsewhere
EXPLAIN_SUMMARY = 5  # code lines a method or property needs before it's worth a summary
EXPLAIN_MAX_LINES = 4000  # an explain task names its methods, so the agent reads only those: long scripts will do
UNITY_MESSAGES = {"Awake", "Start", "Update", "LateUpdate", "FixedUpdate", "OnEnable", "OnDisable", "OnDestroy", "OnValidate",
                  "Reset", "OnGUI", "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay", "OnCollisionEnter", "OnCollisionExit",
                  "OnCollisionStay", "OnApplicationQuit", "OnApplicationPause", "OnApplicationFocus", "OnDrawGizmos",
                  "OnDrawGizmosSelected", "OnBecameVisible", "OnBecameInvisible", "OnRenderObject"}
CS_STRUCT = re.compile(r"[{};]")
CS_TYPE = re.compile(r"\b(class|struct|interface|record|enum)\s+(@?\w+)")
CS_NAME = re.compile(r"((?:@?\w+\s*\.\s*)*@?\w+|operator\s*\S+)\s*(?:<[^()]*>)?\s*$")  # a method's name, before its (
CS_PROPERTY_NAME = re.compile(r"((?:@?\w+\s*\.\s*)*@?\w+|this\s*\[[^\]]*\])\s*$")
CS_MODIFIERS = {"public", "private", "protected", "internal", "static", "virtual", "override", "abstract", "sealed", "async",
                "extern", "unsafe", "new", "readonly", "partial", "volatile", "const", "ref"}
CS_CTOR = re.compile(r"\s*(?:(?:public|private|protected|internal|static|extern|unsafe)\s+)*")  # all before a ctor's name
CS_CODE_CHARS = re.compile(r"[^\s{}()\[\];,]")  # a line with more than brackets and punctuation on it


@dataclass
class CsMember:
    """A type, method, constructor or property a C# script declares, as find_members reads it."""
    kind: str  # "type", "method", "ctor" or "property"
    name: str
    mods: str  # what the declaration says before the name: modifiers, the return type...
    top: int  # 0-based line where the declaration starts, attributes included
    line: int  # 0-based line of the declaration itself
    keyword: str = ""  # a type's: class, struct, interface, record or enum
    body: tuple[int, int] | None = None  # 0-based first and last line of its body: inside its braces, or from its =>


def skip_attributes(text: str) -> int:
    """Where a declaration starts in `text`, past the [attributes] and whitespace in front of it."""
    i = 0
    while True:
        while i < len(text) and text[i].isspace():
            i += 1
        if not text.startswith("[", i):
            return i
        depth = 0
        while i < len(text):
            depth += (text[i] == "[") - (text[i] == "]")
            i += 1
            if not depth:
                break


def assigns(text: str) -> bool:
    """Whether `text` has an = outside brackets (not ==, !=, <=, >= or =>): an initializer."""
    depth = 0
    for k, c in enumerate(text):
        if c in "([":
            depth += 1
        elif c in ")]":
            depth -= 1
        elif c == "=" and not depth and text[k + 1:k + 2] not in ("=", ">") and text[k - 1:k] not in ("=", "!", "<", ">"):
            return True
    return False


def parameters(decl: str) -> re.Match | None:
    """The name in front of a method's or constructor's parameter list (a CS_NAME match ending where the list starts),
    or None for a declaration without one. A tuple type's parentheses don't count, nor does a constraint's new()."""
    depth = 0
    for k, c in enumerate(decl):
        if c == "(":
            if not depth:
                name = CS_NAME.search(decl, 0, k)
                if name and name.group(1) not in CS_MODIFIERS:
                    return name
            depth += 1
        elif c == ")":
            depth -= 1
        elif c == "=" and not depth:
            return None
    return None


def parse_declaration(text: str) -> tuple[str, str, str, str, int] | None:
    """What the code of a declaration up to its body declares: (kind, name, mods, keyword, where it starts past its
    attributes), kind being "namespace", "type", "method", "ctor" or "property". None for anything else: a field with
    an initializer, a delegate, a using."""
    start = skip_attributes(text)
    decl = text[start:]
    if not decl.strip() or re.match(r"\s*(?:using\b|extern\b|=)", decl):
        return None
    name = parameters(decl)
    head = decl[:name.end()] if name else decl
    if re.search(r"\bnamespace\b", head):
        return "namespace", "", "", "", start
    signature = re.split(r"=>|=", head, maxsplit=1)[0]  # a type is named before any initializer or => body
    typed = CS_TYPE.search(signature)
    if typed:
        return "type", typed.group(2), signature[:typed.start()], typed.group(1), start
    if name:
        mods = decl[:name.start()]
        if re.search(r"\bdelegate\b", mods):
            return None
        return ("ctor" if CS_CTOR.fullmatch(mods) else "method"), name.group(1), mods, "", start
    before = decl.split("=>", 1)[0]
    prop = None if assigns(before) else CS_PROPERTY_NAME.search(before.rstrip())
    return ("property", prop.group(1), before[:prop.start()], "", start) if prop else None


def find_members(scanned: list[CsLine]) -> list[CsMember]:
    """The types, methods, constructors and properties a scanned C# script declares, nested types' too, in order."""
    members: list[CsMember] = []
    # Each open brace: "type" (a namespace's or type's, where declarations follow), "member" (a member's body),
    # "expression" (in an initializer or => body, which goes on after it) or "block" (inside one of those).
    scopes: list[str] = []
    depth = 0  # open braces that aren't a namespace's or a type's
    header: list[tuple[int, str]] = []  # the code since the last declaration ended, at type level, by line
    current: CsMember | None = None  # the member whose body is open

    def line_of(offset: int) -> int:
        for idx, text in header:
            if offset < len(text):
                return idx
            offset -= len(text)
        return header[-1][0]

    def declared() -> tuple[CsMember | None, str]:
        text = "".join(t for _, t in header)
        parsed = parse_declaration(text)
        if not parsed:
            return None, text
        kind, name, mods, keyword, start = parsed
        top = line_of(len(text) - len(text.lstrip()))
        line = line_of(start + len(text[start:]) - len(text[start:].lstrip()))
        return CsMember(kind, name, mods, top, line, keyword), text

    for idx, ln in enumerate(scanned):
        pos = 0
        for m in CS_STRUCT.finditer(ln.code):
            c = m.group()
            if depth:
                if c == "{":
                    scopes.append("block")
                    depth += 1
                elif c == "}":
                    depth -= 1
                    if scopes.pop() == "member" and current:
                        current.body = (current.body[0], idx - 1)
                        current, header = None, []
                pos = m.end()
                continue
            header.append((idx, ln.code[pos:m.start()]))
            pos = m.end()
            if c == "}":
                if scopes:
                    scopes.pop()
                header = []
                continue
            member, text = declared()
            if c == ";":
                if member and member.kind in ("method", "ctor", "property") and "=>" in text:
                    member.body = (line_of(text.index("=>")), idx)
                    members.append(member)
                header = []
            elif member and member.kind in ("namespace", "type"):
                if member.kind == "type":
                    members.append(member)
                scopes.append("type")
                header = []
            elif member and "=>" not in text:
                member.body = (idx + 1, idx)
                members.append(member)
                scopes.append("member")
                depth += 1
                current, header = member, []
            else:  # braces in an initializer or => body: the declaration goes on after them
                scopes.append("expression")
                depth += 1
        if not depth:
            header.append((idx, ln.code[pos:] + " "))
    return members


def commented_above(scanned: list[CsLine], member: CsMember) -> bool:
    """Whether a comment sits on the line above a declaration (or above its attributes) or at the end of its line."""
    return any(at > 0 and scanned[at - 1].commented and not scanned[at - 1].code.strip()
               for at in (member.top, member.line)) or scanned[member.line].comment_at is not None


def body_metrics(scanned: list[CsLine], member: CsMember) -> tuple[int, int, int]:
    """A member's body: its lines of code, its lines with a comment, and the most code lines in a row without one."""
    code = comments = run = stretch = 0
    for ln in scanned[member.body[0]:member.body[1] + 1] if member.body else ():
        if ln.commented:
            comments += 1
            run = 0
        if CS_CODE_CHARS.search(ln.code):
            code += 1
            if not ln.commented:
                run += 1
                stretch = max(stretch, run)
    return code, comments, stretch


def inherits_docs(member: CsMember) -> bool:
    """Whether a member needs no summary of its own: a constructor, an override, an interface's member implemented
    explicitly, a Unity message or one of the members INHERITS_DOCS names."""
    return (member.kind == "ctor" or "." in member.name or member.name in INHERITS_DOCS or member.name in UNITY_MESSAGES
            or bool(re.search(r"\boverride\b", member.mods)))


@dataclass(frozen=True)
class Spot:
    """Code that no comment explains, as explain_spots finds it."""
    what: str  # "body": a method whose logic no comment explains; "summary": a member or type with no comment above it
    name: str  # as a task names it: `Spawn`, or class `Horde`
    line: int  # 1-based line of its declaration
    lines: int  # code lines in its body


@functools.lru_cache(maxsize=4096)
def explain_spots(text: str) -> tuple[Spot, ...]:
    """The code in a C# script that no comment explains, in order: methods (constructors and properties too) with
    EXPLAIN_BODY lines of code or more and no comment inside, or EXPLAIN_STRETCH in a row without one; and methods of
    EXPLAIN_SUMMARY lines or more, and types, with no comment above them. Members that inherit their docs need no
    summary (see inherits_docs), nor does a partial type, which may have one on another of its parts."""
    scanned = scan_cs(text)
    spots = []
    for member in find_members(scanned):
        above = commented_above(scanned, member)
        if member.kind == "type":
            if not above and "partial" not in member.mods.split():
                spots.append(Spot("summary", f"{member.keyword} `{member.name}`", member.line + 1, 0))
            continue
        code, comments, stretch = body_metrics(scanned, member)
        if code >= EXPLAIN_BODY and (not comments or stretch >= EXPLAIN_STRETCH):
            spots.append(Spot("body", f"`{member.name}`", member.line + 1, code))
        elif code >= EXPLAIN_SUMMARY and not above and not inherits_docs(member):
            spots.append(Spot("summary", f"`{member.name}`", member.line + 1, code))
    return tuple(spots)


def needs_explaining(text: str) -> bool:
    """Whether a script is worth an explain task: a method whose logic no comment explains, or three members or types
    with no comment above them."""
    spots = explain_spots(text)
    return any(s.what == "body" for s in spots) or sum(s.what == "summary" for s in spots) >= 3


@dataclass(frozen=True)
class Focus:
    label: str  # how the log and the prompt name it
    fits: object  # (script path, script text) -> whether the script is worth showing the planner for this focus
    help: str  # what the planner should look for
    max_lines: int = PLAN_MAX_LINES  # longer scripts aren't planned for


FOCUS = {
    "map": Focus(
        "real-world map", lambda rel, text: rel.startswith(MAP_DIR),
        "This script is part of the raid map built from a real place: OpenStreetMap streets, building footprints and land "
        "use for the state of Kentucky, cut into 256 m cells (see the data classes in "
        f"{MAP_DIR}). Make one kind of thing it builds look or play more like CLONE-Z's hand-built raid town: a building "
        "kind's facade, windows, roof or porch, a road kind's kerbs, sidewalks or markings, props suited to one land-use "
        "kind (trees in forest, cars in parking), or colour variation per building from its id. Reuse the helpers, "
        "materials and Surf the script already uses (or those in FurnitureBuilder and DecorProps), keep colliders in step "
        "with the geometry, and leave the map data format and the importer as they are."),
    "graphics": Focus(
        "object graphics", lambda rel, text: len(PLAN_PRIMITIVES.findall(text)) >= 12,
        "Make one object this script builds look better by adding small visual detail or variation inside its existing "
        "builder method: handles, knobs, hinges, trims, frames, seams, labels, caps, straps, wear or tint variation from the "
        "deterministic random source it already uses, small emissive bits. Give each new part's size in metres and its "
        "position relative to an existing part (quote that call), reuse the materials or colours already used there, keep "
        "colliders, interaction points and pivots as they are, and add no more than about 12 primitives."),
    "perf": Focus(
        "frame-time", lambda rel, text: bool(PLAN_FRAME.search(text) and PLAN_FRAME_WORK.search(text)),
        "Work repeated every frame in Update, LateUpdate or FixedUpdate that could be cached or skipped: GetComponent, Find "
        "or Camera.main lookups, uGUI text rebuilt when nothing changed, LINQ or allocations per frame. Say which field "
        "caches what, where it is filled and when it is refreshed. What the player sees must not change. Never make "
        "something recomputed every frame run once or only on a flag unless every input that can change it marks it "
        "stale (a summary saying it is rebuilt each frame is a requirement), and never cache labels, actions or state "
        "the player's own actions change."),
    "fixes": Focus(
        "bug-fix", lambda rel, text: len(PLAN_BRANCHES.findall(text)) >= 15,
        "A real bug you can prove from the code shown: a wrong condition, an event subscribed but never unsubscribed, a null "
        "reference on a path that really happens, an off-by-one, a value that can never change. Name the method, explain "
        "the wrong behaviour the player gets and give the exact fix. If you can't prove a bug, reply NONE."),
    "features": Focus(
        "small gameplay feature", lambda rel, text: "GetActions(" in text,
        "A small addition that rounds off what this script already does: a new action next to an existing one in "
        "GetActions (say which one to copy), a new use for an item it already handles, a small stat or moodle effect, an "
        "extra reaction or message. Use only item ids, config values and methods that appear in the code shown, and no new "
        "UI screens."),
    "flavour": Focus(
        "flavour-text", lambda rel, text: len(PLAN_PROSE.findall(text)) >= 8,
        "Append 4 to 8 new entries to one existing list of written text in this script: a list whose entries are whole "
        "sentences in quotes that the player reads or hears in the world (notes, letters, broadcasts, barks, graffiti, "
        "book titles, item descriptions). Not menus, help or manual pages that explain how the game works, and never a "
        "list or dictionary of ids, items, skills or numbers. Quote one existing entry as the style reference and name "
        "the themes the new ones should cover. Original wording only: no lyrics, quotes or real brands. Existing "
        "entries, ids, order-dependent logic and code must stay as they are."),
    "docs": Focus("documentation", lambda rel, text: undocumented(text) >= 3, ""),  # a fixed task text; no model needed
    # Comments for code that has none: a fixed task naming the methods and types (see explain_task); no model needed.
    "explain": Focus("code explanation", lambda rel, text: needs_explaining(text), "", EXPLAIN_MAX_LINES),
}

# What a big task (a focus written with a trailing "+") should be, on top of the focus's own help. Documentation has no
# big variant: its task is fixed text.
BIG_HELP = {
    "map": "Take one kind of thing the map builds and finish it end to end: one building kind's whole facade (door "
           "frame, sills, trims, porch, signs), one road kind's kerbs, markings and verges along both edges, or the props "
           "for one land-use kind with their spacing. Give each part its root (ground storey in site.Town.Out with a "
           "PropCollider, upper storeys and roofs in site.Overhead, past the edge in site.Town.Far), check site.Free "
           "before placing and site.Take after, give boxes along a wall the wall's yaw, wrap DecorProps calls in "
           "SetFrame and ResetFrame, and never call a pass over every building from inside another per-building loop.",
    "graphics": "Give one object a full detailing pass: four to six coordinated details (a door's frame, handle, hinges, "
                "kick plate and wear, say) that read together as one improvement, each with its size in metres, its "
                "position relative to an existing part (quote that call) and its material. About 40 primitives at most.",
    "fixes": "A repair pass: two to five real problems you can prove from the code, each its own step with the wrong "
             "behaviour the player gets and the exact fix: wrong conditions, events never unsubscribed, null paths that "
             "really happen, values that never change, caches never refreshed, code inside a loop it should follow, "
             "duplicated blocks that disagree. If you can't prove at least two, reply NONE.",
    "perf": "All the per-frame waste in this script as one pass: each step names the lookup, allocation or rebuild, the "
            "field that will hold the result, where it is filled and exactly what refreshes it. Leave out anything whose "
            "value can change between frames without a signal you can name.",
    "features": "A complete small mechanic that rounds off what this script does, across the script and the related "
                "scripts: a new action with the state it changes, its save (SaveKey, CaptureState and RestoreState), its "
                "sound and its messages; or a new item use registered through ItemActions in a new IGameModule script. "
                "Use only item ids, config values and methods that appear in the code shown, and no new UI screens.",
    "flavour": "One to three lists of written text in this script, 8 to 20 new entries in all, around themes you name, "
               "with one existing entry quoted per list as the style reference.",
}


def split_focus(name: str) -> tuple[str, bool]:
    """A focus as written in NIGHTSHIFT_AUTOPLAN_FOCUS ("graphics" or "graphics+"): its kind, and whether it's big."""
    name = name.strip()
    big = name.endswith(BIG)
    kind = name[:-len(BIG)] if big else name
    return kind, big and kind in BIG_HELP


def focus_names() -> list[str]:
    """Every focus NIGHTSHIFT_AUTOPLAN_FOCUS (and plan --focus) accepts: each kind, and the big variants."""
    return sorted(FOCUS) + sorted(kind + BIG for kind in BIG_HELP)


def focus_label(name: str) -> str:
    kind, big = split_focus(name)
    return FOCUS[kind].label + (" (big)" if big else "")

DOC_TASK = ("Add a one-sentence `/// <summary>` to each public method and property in {path} that has none, saying what it "
            "does and why, in the same voice as the existing summaries in that file. Don't change any code.")
DOC_DETAILS = (
    "Skip fields, constants, constructors, and members that override a base member or implement an interface (for example "
    "DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's "
    "Order and Init): they inherit their docs.",
    "Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that "
    "declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an "
    "edit fails, re-read the file first.")
DOC_TASK_START = DOC_TASK.split("{path}")[0]
DOC_AGAIN = " (Pass {n} over this file: most of it has summaries already.)"  # ends a follow-up pass's task, so keys differ
DOC_PASSES = 3  # documentation passes one script gets at most (see plan_candidates)
EVERY_WORKER = "all"  # NIGHTSHIFT_DOCS_WORKERS=all: every worker takes only documentation tasks

EXPLAIN_TASK = ("Add comments explaining the code in {path}, so a programmer new to CLONE-Z can follow it. Comments only: "
                "don't change any code.")
EXPLAIN_BODIES_LINE = (
    "Explain the logic inside these methods, which no comment explains yet: {listed}. Above each step a reader can't "
    "follow from the code alone, add a short `//` comment saying what it is for or why it is done that way: what a block "
    "or loop is for, why a value, limit or order is what it is, what a condition guards against. Say why, not what: "
    "don't restate the line below, and leave obvious lines alone. Where one of these methods has no comment above it, "
    "add a one-sentence `/// <summary>` there too.")
EXPLAIN_SUMMARIES_LINE = ("Above each of these, which have no comment yet, add a one-sentence `/// <summary>` saying what "
                          "it is for, in the same voice as the file's other summaries: {listed}.")
EXPLAIN_DETAILS = (
    "Never add a second `<summary>` where one exists, and give none to members that override a base member or "
    "implement an interface, or to Unity messages such as Update: they inherit their docs.",
    "Insert only: put each comment on its own line, indented like the line below it, never at the end of a line of "
    "code. Never delete, move or rewrite existing lines or comments; if an edit fails, re-read the file first.")
EXPLAIN_TASK_START = EXPLAIN_TASK.split("{path}")[0]
EXPLAIN_AGAIN = " (Pass {n} over this file: other parts of it have comments already.)"
EXPLAIN_PASSES = 8  # explain passes one script gets at most: each takes a few methods, the next the ones left
EXPLAIN_BODIES = 4  # methods one explain task explains inside, at most
EXPLAIN_BODY_LINES = 250  # ...and their code lines in all, beyond the first method
EXPLAIN_SUMMARIES = 8  # members and types one explain task gives a summary, at most
DOC_KINDS = ("docs", "explain")  # the focuses whose tasks add comments and nothing else: documentation tasks
DOC_STARTS = (DOC_TASK_START, EXPLAIN_TASK_START)  # how their tasks start


def is_docs(task: Task) -> bool:
    """Whether `task` is a documentation task: one of the docs or explain focus's fixed ones (DOC_TASK, EXPLAIN_TASK),
    whoever queued it. It lands only the comments it adds. Workers in NIGHTSHIFT_DOCS_WORKERS take only these, and the
    others leave them to those workers while one is on duty."""
    return task.key.startswith(DOC_STARTS)


def explain_task(path: str, text: str, tasks: list[tuple[str, Task]]) -> Task | None:
    """The explain focus's task for the script at `path`: comments inside the methods with the most code that no
    comment explains (EXPLAIN_BODIES at most), and summaries for the members and types with no comment above them
    (EXPLAIN_SUMMARIES at most), each named with its line. None if nothing in it is left to explain."""
    spots = explain_spots(text)
    bodies, lines = [], 0
    for spot in sorted((s for s in spots if s.what == "body"), key=lambda s: -s.lines):
        if bodies and (len(bodies) == EXPLAIN_BODIES or lines + spot.lines > EXPLAIN_BODY_LINES):
            break
        bodies.append(spot)
        lines += spot.lines
    summaries = [s for s in spots if s.what == "summary"][:EXPLAIN_SUMMARIES]
    if not bodies and not summaries:
        return None
    details = []
    if bodies:
        details.append(EXPLAIN_BODIES_LINE.format(listed=", ".join(
            f"{s.name} (line {s.line}, {s.lines} lines of code)" for s in sorted(bodies, key=lambda s: s.line))))
    if summaries:
        details.append(EXPLAIN_SUMMARIES_LINE.format(listed=", ".join(f"{s.name} (line {s.line})" for s in summaries)))
    first = EXPLAIN_TASK.format(path=path)
    n = 1 + sum(1 for _, task in tasks if task.key.startswith(first))
    key = first + (EXPLAIN_AGAIN.format(n=n) if n > 1 else "")
    return Task(key, "\n".join([key, *details, *EXPLAIN_DETAILS]))

PLAN_PROMPT = """You plan work for the CLONE-Z Night Shift: local AI coding agents that each take one task from a queue
overnight, edit the code of CLONE-Z (a Unity 6 isometric zombie-survival game in C#, everything built from code at
runtime), compile it against Unity's assemblies and land it if it compiles. The agents can't run or see the game and
can't ask questions. They do well with one concrete, precisely located change and fail on vague or sprawling work.

Write the single most useful {label} task for the script below, or reply NONE if there is nothing worth doing.

SETTING
CLONE-Z takes place in rural Kentucky in 1993, in winter, after the outbreak: snowbound, run down, lived in. Whatever
the player sees has to belong there: period-correct, worn and plausible. Nothing modern (no LEDs, flat screens or
smartphones) and nothing that glows without a reason.

WHAT TO LOOK FOR
{help}

RULES
- The task is about {path} (one other file at most, when it's essential). Name the class and the method or field to
  change, say exactly what to change, what the player should see in game and what must stay as it is.
- Ground everything in the code shown: every type, method, field, constant, item id and value you name must appear in
  it exactly as written. Quote the current value of anything you want changed.
- C# 9: no file-scoped namespaces, no global usings. No new asset files (textures, shaders, prefabs, scenes);
  everything is built from code. No save-format or networking changes, no renamed public members. Nothing in Tools/,
  ProjectSettings/ or Packages/, and no .meta or .unity files.
- Don't repeat or overlap these tasks, already queued or done for this script:
{existing}

FORMAT
Reply with exactly one task in this form and nothing else:
- [ ] <the whole request in one or two sentences, naming {path}>
  <detail lines in plain sentences, each indented by two spaces; six at most>
Quote code inline with backticks, as in `m.Box(new Vector3(0f, 0.72f, 0f), new Vector3(0.52f, 0.02f, 0.02f), chrome)`.
No code blocks, no bold text, no headings. Or reply with the single word NONE.

HOW CLONE-Z IS BUILT
{lessons}

SCRIPT {path} ({lines} lines)
{source}
"""

BIG_PLAN_PROMPT = """You plan work for the CLONE-Z Night Shift: local AI coding agents that each take one task from a queue
overnight, edit the code of CLONE-Z (a Unity 6 isometric zombie-survival game in C#, everything built from code at
runtime), compile it against Unity's assemblies and land it if it compiles. The agents can't run or see the game and
can't ask questions. Each has up to two hours per task, and a one-line change leaves most of that unused, so write a
substantial task: one coherent piece of work, three to six related changes that together give the player one visible
result. Agents finish big work when every step is concrete and located, and fail when it is vague or sprawling.

Write the single most useful substantial {label} task for the script below, or reply NONE if nothing there is worth
that much work.

SETTING
CLONE-Z takes place in rural Kentucky in 1993, in winter, after the outbreak: snowbound, run down, lived in. Whatever
the player sees has to belong there: period-correct, worn and plausible. Nothing modern (no LEDs, flat screens or
smartphones) and nothing that glows without a reason.

WHAT TO LOOK FOR
{help}
{big}

RULES
- The task is about {path}. It may also change up to two of the related scripts outlined below, and may add one new
  script in an existing folder under Assets/Scripts/ (give its full path; new classes go there rather than into a long
  file). Name every file it touches, and touch no other script.
- Write the work as steps, one per detail line, each starting "Step 1:", "Step 2:" and so on, naming the file, the
  class and the method or field, and saying exactly what to add or change. Quote the current code of anything changed.
  Finish with a line starting "Done when:" that says what the player sees in game and what must stay as it is.
- Ground everything in the code shown: every type, method, field, constant, item id and value you name must appear in
  the script or the outlines exactly as written, unless a step creates it. Quote the current value of anything you
  want changed.
- What runs every frame stays correct every frame: never make work that is redone each frame run once or only on a flag
  unless every input that can change it marks it stale, and say where each does.
- C# 9: no file-scoped namespaces, no global usings. No new asset files (textures, shaders, prefabs, scenes);
  everything is built from code. No save-format or networking changes, no renamed public members. Nothing in Tools/,
  ProjectSettings/ or Packages/, and no .meta or .unity files.
- Don't repeat or overlap these tasks, already queued or done for this script:
{existing}

FORMAT
Reply with exactly one task in this form and nothing else:
- [ ] <the whole request in one or two sentences, naming {path}>
  Step 1: <the first change, located exactly>
  Step 2: <the next change>
  <more steps as needed; sixteen detail lines at most in all>
  Done when: <what the player sees in game, and what must stay as it is>
Quote code inline with backticks, as in `m.Box(new Vector3(0f, 0.72f, 0f), new Vector3(0.52f, 0.02f, 0.02f), chrome)`.
No code blocks, no bold text, no headings. Or reply with the single word NONE.

HOW CLONE-Z IS BUILT
{lessons}

RELATED SCRIPTS (their types and the members other scripts can use; bodies left out)
{related}

SCRIPT {path} ({lines} lines)
{source}
"""


def plan_candidates(focus: str, repo: Path, tasks: list[tuple[str, Task]]) -> list[str]:
    """Scripts worth showing the planner for `focus`: the right kind of code, short enough to read whole (or, for an
    explain task, to find its way around in), and no unchecked task queued for them yet (two tasks on one script at once
    would collide when they land)."""
    kind, _ = split_focus(focus)
    queued = "\n".join(task.text for mark, task in tasks if mark == " ")
    found = []
    for f in sorted((repo / "Assets" / "Scripts").rglob("*.cs")):
        rel = f.relative_to(repo).as_posix()
        if "/Editor/" in rel or PROTECTED.search(rel) or rel in queued:
            continue
        try:
            text = f.read_text(encoding="utf-8-sig", errors="replace")
        except OSError:
            continue
        if not 30 <= text.count("\n") + 1 <= FOCUS[kind].max_lines or not FOCUS[kind].fits(rel, text):
            continue
        if kind == "docs":
            # Later work adds public members without summaries, so a script gets another pass once every earlier one
            # landed. Not after one that found nothing to document (what's left inherits its docs) or failed.
            passes = [mark for mark, task in tasks if task.key.startswith(DOC_TASK.format(path=rel))]
            if passes and (len(passes) >= DOC_PASSES or any(mark not in "xX" for mark in passes)):
                continue
        elif kind == "explain":
            # Each pass explains a few methods and the next one the rest, as long as every earlier pass landed.
            passes = [mark for mark, task in tasks if task.key.startswith(EXPLAIN_TASK.format(path=rel))]
            if passes and (len(passes) >= EXPLAIN_PASSES or any(mark not in "xX" for mark in passes)):
                continue
        found.append(rel)
    return found


def pick_target(focus: str, candidates: list[str], seen: dict, rng: random.Random | None = None) -> str | None:
    """The candidate shown to the planner least recently for this focus (never-shown ones first, ties at random)."""
    if not candidates:
        return None
    rng = rng or random.Random()
    return min(candidates, key=lambda rel: (seen.get(f"{focus}:{rel}", ""), rng.random()))


def busy_scripts(tasks: list[tuple[str, Task]]) -> set[str]:
    """The paths unchecked tasks name: another task mustn't touch them until those land, or the two would collide."""
    return {m.rstrip("./") for mark, task in tasks if mark == " " for m in PATH_MENTION.findall(task.text)}


def related_scripts(target: str, repo: Path, busy: set[str] | frozenset = frozenset(),
                    limit: int = PLAN_RELATED) -> list[str]:
    """The scripts most tied to `target`, for a big task to reach into: those declaring the types it uses (rarer types
    count for more; a partial class's other files most of all) and those using the types it declares. Editor,
    protected, busy and very long scripts are left out."""
    texts: dict[str, str] = {}
    for f in sorted((repo / "Assets" / "Scripts").rglob("*.cs")):
        rel = f.relative_to(repo).as_posix()
        if "/Editor/" in rel or PROTECTED.search(rel):
            continue
        try:
            texts[rel] = f.read_text(encoding="utf-8-sig", errors="replace")
        except OSError:
            continue
    if target not in texts:
        return []
    words = {rel: set(IDENT.findall(text)) for rel, text in texts.items()}
    used_by = Counter(w for ws in words.values() for w in ws)
    declared: dict[str, list[str]] = {}
    for rel, text in texts.items():
        for name in set(TYPE_RE.findall(text)):
            declared.setdefault(name, []).append(rel)

    def weight(name: str) -> float:  # a name every script uses ties nothing together
        return max(0.0, math.log(len(texts) / (1 + used_by[name])))

    mine = set(TYPE_RE.findall(texts[target]))
    score: Counter = Counter()
    for name, count in Counter(IDENT.findall(texts[target])).items():
        owners = [rel for rel in declared.get(name, ()) if rel != target]
        for rel in owners:
            score[rel] += (3.0 if name in mine else 1.0) * min(count, 5) * weight(name) / len(owners)
    for name in mine:
        for rel, ws in words.items():
            if rel != target and name in ws:
                score[rel] += weight(name)
    ranked = sorted((rel for rel in score if score[rel] > 0), key=lambda rel: (-score[rel], rel))
    return [rel for rel in ranked if rel not in busy and texts[rel].count("\n") + 1 <= 2 * PLAN_MAX_LINES][:limit]


def outline(text: str, max_lines: int = PLAN_OUTLINE_LINES) -> list[str]:
    """A script's types and the members other scripts can use, one signature per line, bodies and initial values left
    out (default parameter values stay)."""
    out = []
    for line in text.splitlines():
        if not (TYPE_RE.match(line) or SIGNATURE.match(line)):
            continue
        sig = re.split(r"\s*(?:\{|=>|//)", line.strip(), maxsplit=1)[0]
        paren, eq = sig.find("("), sig.find(" = ")
        if eq >= 0 and (paren < 0 or eq < paren):
            sig = sig[:eq]
        sig = sig.rstrip(" ;")
        if not sig:
            continue
        if len(out) == max_lines:
            out.append("...")
            break
        out.append(sig[:180])
    return out


def related_block(repo: Path, related: list[str]) -> str:
    parts = []
    for rel in related:
        text = (repo / rel).read_text(encoding="utf-8-sig", errors="replace")
        parts.append(f"{rel} ({text.count(chr(10)) + 1} lines)\n" + "\n".join("  " + s for s in outline(text)))
    return "\n\n".join(parts) or "(none found)"


def plan_prompt(focus: str, path: str, source: str, existing: list[str], lessons: str, related: str = "") -> str:
    name, big = split_focus(focus)
    kind = FOCUS[name]
    return (BIG_PLAN_PROMPT if big else PLAN_PROMPT).format(
        label=kind.label, help=kind.help, big=BIG_HELP.get(name, "") if big else "", path=path,
        existing="\n".join("  " + e for e in existing) or "  (none yet)", lessons=lessons.strip() or "(no notes)",
        related=related or "(none found)", lines=source.count("\n") + 1,
        source="```csharp\n" + source.rstrip() + "\n```")


def ask_model(base: str, model: str, prompt: str, timeout: float = 900) -> str:
    """One answer from Ollama's OpenAI-compatible endpoint, the one OpenCode uses, so the model keeps being served exactly
    as it is for the agents (other settings would make Ollama reload it). Any reasoning is left out. One of OpenCode's
    own models answers only OpenCode, so it is asked through OpenCode (ask_opencode)."""
    if is_cloud(model):
        return ask_opencode(model, prompt, timeout)
    body = json.dumps({"model": model, "messages": [{"role": "user", "content": prompt}], "stream": False}).encode()
    req = urllib.request.Request(base.rstrip("/") + "/v1/chat/completions", body, {"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        reply = json.load(r)
    content = ((reply.get("choices") or [{}])[0].get("message") or {}).get("content") or ""
    return re.sub(r"<think>.*?</think>", "", content, flags=re.S).strip()


LIST_MARK = re.compile(r"^(?:[-*+•]|\d+[.)])\s+")  # a leading bullet or number, not *emphasis*
CALLED = re.compile(r"\b([A-Za-z_]\w{2,})\s*(?:<[^<>`]*>)?\s*\(")  # a method called (or declared) in quoted code
STEP_START = re.compile(r"^(?:step[ \t]*\d+|done when\b)", re.I)  # a big task's step, or its last line


def plain(line: str) -> str:
    """A line of the planner's prose with bold markers, list marks, whole-line italics and runs of spaces taken out."""
    line = LIST_MARK.sub("", line.strip()).replace("**", "")
    if len(line) > 2 and line[0] == line[-1] == "*":
        line = line[1:-1]
    return " ".join(line.split())


def step_start(line: str) -> bool:
    """Whether a line of the planner's reply starts a step ("Step 1:") or the "Done when:" line, however it's marked up."""
    return bool(STEP_START.match(plain(line.strip().lstrip("#"))))


def script_words(repo: Path) -> set[str]:
    """Every word that appears in the game's scripts, to tell real methods from made-up ones."""
    words: set[str] = set()
    for f in (repo / "Assets" / "Scripts").rglob("*.cs"):
        try:
            words.update(re.findall(r"[A-Za-z_]\w*", f.read_text(encoding="utf-8-sig", errors="replace")))
        except OSError:
            continue
    return words


def unknown_calls(text: str, words: set[str]) -> list[str]:
    """Methods the task calls in its quoted code (`...`) that appear nowhere in the game's scripts."""
    found = []
    for span in re.findall(r"`([^`\n]+)`", text):
        for name in CALLED.findall(span):
            if name not in words and name not in found:
                found.append(name)
    return found


def parse_plan(reply: str) -> Task | None:
    """The first task in the planner's reply (a `- [ ]` line and the lines under it, up to a blank line or the next task;
    code in a fenced block is kept line by line), or None, say for NONE. In a task written in steps ("Step 1:" ...
    "Done when:"), blank lines before and between the steps don't end it, and the lines and code under each step are
    joined to that step, so each step is one detail line."""
    lines = re.sub(r"<think>.*?</think>", "", reply or "", flags=re.S).replace("\r", "").splitlines()
    for i, line in enumerate(lines):
        m = TASK_LINE.match(line)
        if not m:
            continue
        # A reply wrapped whole in a fence: the next fence closes it rather than opening a code block.
        wrapped = sum(1 for before in lines[:i] if before.strip().startswith("```")) % 2 == 1
        details, code, stepped = [], False, False

        def add(text: str) -> None:
            if stepped and details and not step_start(text):
                details[-1] += " " + text
            else:
                details.append(text)

        for j in range(i + 1, len(lines)):
            more, s = lines[j], lines[j].strip()
            if s.startswith("```"):
                if wrapped and not code:
                    break
                code = not code
                continue
            if code:
                if s:  # quoted, so it reads as code and the made-up-method check sees it
                    add("`" + " ".join(s.split()).replace("`", "'") + "`")
                continue
            if not s:
                after = next((l.strip() for l in lines[j + 1:] if l.strip()), "")
                if step_start(after) or (stepped and (after.startswith("```") or LIST_MARK.match(after))):
                    continue
                break
            if TASK_LINE.match(more) or (s.startswith("#") and not step_start(s)):
                break
            text = plain(s.lstrip("#"))
            stepped = stepped or step_start(text)
            add(text)
        key = plain(m.group(1))
        return Task(key, "\n".join([key] + [d for d in details if d]))
    return None


def check_plan(task: Task, target: str, repo: Path, tasks: list[tuple[str, Task]],
               words: set[str] | None = None, big: bool = False) -> tuple[Task | None, str]:
    """`task` made fit for the queue, or None and why not. It must name `target` (its file or class name will do; the
    path is put in), mention only paths that exist and aren't protected, leave alone the scripts queued tasks are
    changing, keep to a sensible size, call no method that appears nowhere in the scripts (one new one is fine) and not
    repeat a task in tasks.md. A big task may say more, needs its steps, and may create one new script (in a folder
    that exists) and call a few new helpers. A small task loses any step numbers, so it stays small (see is_big)."""
    size = BIG_TASK if big else SMALL_TASK
    key, *details = task.text.splitlines()
    key = plain(key)
    details = [d for d in (plain(d) if big else STEP.sub("", plain(d)) for d in details) if d][:size.details]
    if target not in key:
        file_name = target.rsplit("/", 1)[-1]
        name = r"(?<![\w./])" + re.escape(file_name)
        if re.search(name, key):
            key = re.sub(name, target, key, count=1)
        elif re.search(r"\b" + re.escape(file_name[:-3]) + r"\b", "\n".join([key] + details)) or \
                any(target in d for d in details):
            key = f"In {target}: {key}"
        else:
            return None, f"it doesn't name {target}"
    text = "\n".join([key] + details)
    if not 40 <= len(key) <= 700 or len(text) > size.chars:
        return None, f"its length ({len(key)} characters on the first line) is out of range"
    steps = len(STEP.findall(text))
    if steps < size.min_steps:
        return None, f"a big task spells out its steps (Step 1:, Step 2:...), and this one has {steps}"
    if OFF_LIMITS.search(text):
        return None, "it refers to the map scraper's data, which must stay out of the game"
    busy, new_scripts = busy_scripts(tasks), []
    for mention in PATH_MENTION.findall(text):
        mention = mention.rstrip("./")
        if mention in MENTION_OK or mention in new_scripts:
            continue
        path = repo / mention
        if PROTECTED.search(mention + ("/" if path.is_dir() else "")):
            return None, f"it mentions {mention}"
        if not path.exists():
            if len(new_scripts) < size.new_scripts and mention.startswith("Assets/Scripts/") and \
                    mention.endswith(".cs") and "/Editor/" not in mention and path.parent.is_dir():
                new_scripts.append(mention)
                continue
            return None, f"it mentions {mention}"
        if mention != target and mention in busy:
            return None, f"it touches {mention}, which a queued task is changing"
    if re.search(r"\.(?:unity|meta|asmdef|asmref)\b", text):
        return None, "it touches scenes or .meta files"
    unknown = unknown_calls(text, words) if words is not None else []
    if len(unknown) > size.new_methods:  # a helper or two the task asks for; more means it's making things up
        return None, "it calls methods that exist nowhere in the scripts: " + ", ".join(unknown[:4])
    for _, other in tasks:
        if other.key == key or (target in other.key and difflib.SequenceMatcher(None, key, other.key).ratio() > 0.85):
            return None, "it repeats a task in tasks.md"
    return Task(key, text), ""


def planned_heading(now: dt.datetime | None = None) -> str:
    return (f"### Auto-planned {(now or dt.datetime.now()):%Y-%m-%d}: written by the night shift when the queue ran low "
            f"(delete any you don't want)")


def planned_keys(path: Path | None = None) -> set[str]:
    """The keys of the tasks under an "### Auto-planned" heading in tasks.md, so tasks people write can go first."""
    path = path or TASKS
    keys, planned = set(), False
    with tasks_lock():
        lines = path.read_text(encoding="utf-8").splitlines() if path.exists() else []
    for line in lines:
        if line.startswith("#"):
            planned = line.startswith("### Auto-planned")
        elif planned:
            m = BOX.match(line)
            if m:
                keys.add(m.group(3))
    return keys


def append_planned(task: Task, heading: str, path: Path | None = None) -> None:
    """Adds `task` at the end of tasks.md, under `heading` (added first unless it's the last heading already)."""
    path = path or TASKS
    with tasks_lock():
        body = path.read_text(encoding="utf-8") if path.exists() else ""
        headings = re.findall(r"^#{1,6} .*$", body, re.M)
        out = body.rstrip("\n")
        if not headings or headings[-1].strip() != heading:
            out += "\n\n" + heading + "\n"
        key, *details = task.text.splitlines()
        write_atomic(path, out + "\n- [ ] " + key + "".join("\n  " + d for d in details) + "\n")


def load_plan_state(path: Path | None = None) -> dict:
    try:
        state = json.loads((path or AUTOPLAN_STATE).read_text(encoding="utf-8"))
        return state if isinstance(state, dict) else {}
    except (OSError, ValueError):
        return {}


def save_plan_state(state: dict, path: Path | None = None) -> None:
    path = path or AUTOPLAN_STATE
    path.parent.mkdir(parents=True, exist_ok=True)
    write_atomic(path, json.dumps(state, indent=1, sort_keys=True))


def plan_model(cfg: dict, ollama: str) -> str:
    """The model that writes tasks: NIGHTSHIFT_AUTOPLAN_MODEL, else the helpers' model (it serves several requests at once,
    so planning doesn't hold up an agent), else NIGHTSHIFT_MODEL."""
    for model in (cfg.get("NIGHTSHIFT_AUTOPLAN_MODEL"), cfg.get("NIGHTSHIFT_HELPER_MODEL")):
        if model and has_model(ollama, model):
            return model
    return cfg["NIGHTSHIFT_MODEL"]


class Planner:
    """Writes the next task into tasks.md when the queue runs low (NIGHTSHIFT_AUTOPLAN). It goes round the focus areas in
    NIGHTSHIFT_AUTOPLAN_FOCUS, shows the model one script at a time (the one that fits and was shown least
    recently) and queues the task it writes once that checks out: it must name that script, mention only scripts that
    exist and nothing protected, and not repeat a task already in tasks.md. Documentation tasks (the docs and explain
    focuses) need no model. A focus written with a trailing "+" asks for a big task: several located steps, with the
    scripts tied to the target shown in outline so the steps can reach into them (see BIG_PLAN_PROMPT). Only the workers
    that take big tasks get those; the coordinator asks for a small one whenever a worker that takes only small tasks
    would have none waiting, and for a documentation task whenever a documentation worker (NIGHTSHIFT_DOCS_WORKERS)
    would have none (see plan_docs)."""

    def __init__(self, ollama: str, model: str, focus: list[str], repo: Path | None = None, ask=None, on_duty=None,
                 dry_run: bool = False, leave_out=None):
        self.ollama, self.model, self.repo = ollama, model, repo or ROOT
        named = [split_focus(f) for f in focus]
        self.focus = [kind + (BIG if big else "") for kind, big in named if kind in FOCUS] or list(FOCUS)
        self.ask = ask or ask_model
        self.on_duty = on_duty or (lambda: not STOPPING.is_set())
        self.leave_out = leave_out or (lambda: ())  # focus kinds the rotation passes over for now
        self.dry_run = dry_run  # plan and show, but queue nothing and remember nothing (nightshift.py plan --dry-run)
        self.state = load_plan_state()
        self.planned = 0

    def next_focus(self) -> str | None:
        """The next focus in turn, passing over the kinds left out (documentation while documentation workers are on duty:
        the coordinator asks for theirs separately); None when every focus is left out."""
        skip = set(self.leave_out())
        for _ in range(len(self.focus)):
            turn = int(self.state.get("turn", 0))
            self.state["turn"] = turn + 1
            name = self.focus[turn % len(self.focus)]
            if split_focus(name)[0] not in skip:
                return name
        return None

    def plan_one(self, tries: int = 3, focus: str | None = None, target: str | None = None,
                 small: bool = False) -> Task | None:
        """Plans and queues one task, trying up to `tries` scripts along the focus rotation (or the given focus and
        script); None if none of them gave one. `small` writes each focus's small variant, for the workers that take
        only small tasks (NIGHTSHIFT_SMALL_TASK_MODELS)."""
        for _ in range(tries):
            if not self.on_duty():
                return None
            kind, tasks = focus or self.next_focus(), read_tasks()
            if kind is None:
                return None  # every focus in the rotation is left out for now
            if small:
                kind = split_focus(kind)[0]
            script = target or pick_target(kind, plan_candidates(kind, self.repo, tasks), self.state.setdefault("seen", {}))
            if not script:
                continue
            self.state.setdefault("seen", {})[f"{kind}:{script}"] = dt.datetime.now().isoformat(timespec="minutes")
            if not self.dry_run:
                save_plan_state(self.state)
            started = time.time()
            task, why = self.write_task(kind, script, tasks)
            if task and self.on_duty():
                if not self.dry_run:
                    append_planned(task, planned_heading())
                self.planned += 1
                log(f"planner: {'would queue' if self.dry_run else 'queued'} for {script} ({focus_label(kind)}, "
                    f"{(time.time() - started) / 60:.1f} min): {first_line(task.key, 100)}")
                return task
            if task:
                return None  # went off duty while the model was thinking: queue nothing more
            log(f"planner: nothing for {script} ({focus_label(kind)}): {why}")
        return None

    def plan_docs(self) -> Task | None:
        """Plans and queues one documentation task, for the documentation workers: the documentation focuses (DOC_KINDS)
        take turns, and when one finds no script to work on, the next goes instead. None if none of them found one."""
        turn = int(self.state.get("docs_turn", 0))
        self.state["docs_turn"] = turn + 1
        for k in range(len(DOC_KINDS)):
            task = self.plan_one(tries=1, focus=DOC_KINDS[(turn + k) % len(DOC_KINDS)])
            if task or not self.on_duty():
                return task
        return None

    def write_task(self, focus: str, target: str, tasks: list[tuple[str, Task]]) -> tuple[Task | None, str]:
        kind, big = split_focus(focus)
        if kind == "docs":
            first = DOC_TASK.format(path=target)
            n = 1 + sum(1 for _, task in tasks if task.key.startswith(first))
            key = first + (DOC_AGAIN.format(n=n) if n > 1 else "")
            return Task(key, "\n".join((key,) + DOC_DETAILS)), ""
        source = (self.repo / target).read_text(encoding="utf-8-sig", errors="replace")
        if kind == "explain":
            task = explain_task(target, source, tasks)
            return (task, "") if task else (None, "nothing in it is left to explain")
        existing = [f"[{mark}] {task.key[:220]}" for mark, task in tasks if target in task.text][-15:]
        lessons_path = self.repo / LESSONS_REL
        lessons = lessons_path.read_text(encoding="utf-8")[:PLAN_LESSONS_CHARS] if lessons_path.exists() else ""
        related = related_block(self.repo, related_scripts(target, self.repo, busy_scripts(tasks))) if big else ""
        reply = self.ask(self.ollama, self.model, plan_prompt(focus, target, source, existing, lessons, related))
        planned = parse_plan(reply)
        if planned is None:
            return None, "the model had nothing to suggest" if reply.strip().upper().startswith("NONE") else "no task in its reply"
        return check_plan(planned, target, self.repo, tasks, script_words(self.repo), big=big)


# --- Coordinator --------------------------------------------------------------------------------------------

def first_line(text: str, width: int = 140) -> str:
    line = next((l.strip() for l in (text or "").splitlines() if l.strip()), "")
    return line[:width]


def stop_time(setting: str, now: dt.datetime | None = None) -> dt.datetime | None:
    """When to stop taking new tasks: the next HH:MM, or None (work until stopped) when the setting is empty or off."""
    setting = (setting or "").strip().lower()
    if setting in ("", "off", "never", "none"):
        return None
    now = now or dt.datetime.now()
    hour, minute = (int(x) for x in setting.split(":"))
    at = now.replace(hour=hour, minute=minute, second=0, microsecond=0)
    return at if at > now else at + dt.timedelta(days=1)


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
        self.big = True  # takes big tasks; False on NIGHTSHIFT_SMALL_TASK_MODELS (see Coordinator.team)
        self.docs = False  # takes only documentation tasks: NIGHTSHIFT_DOCS_WORKERS (see Coordinator.claim)
        self.windows = False  # a Windows helper, where OpenSSH runs commands in Git Bash (found out by prepare)
        self.retired = False  # done for the night: its loop has ended

    def _cmd(self, script: str, wrap: bool = False) -> list[str]:
        if not self.target:
            return ["bash", "-c", script]
        if wrap or self.windows:
            # Base64 leaves nothing for a shell to mangle on the way: Windows' OpenSSH passes the command to Git Bash on a
            # Windows command line, which quotes differently.
            script = f"echo {base64.b64encode(script.encode()).decode()} | base64 -d | bash"
        return ssh_command() + [self.target, script]

    def _git_env(self) -> dict | None:
        return git_env() if self.target else None

    def prepare(self) -> str | None:
        """Returns why this worker can't be used, or None when it is ready. It also finds out whether a helper runs Windows
        (through Git Bash, see WindowsHelper.ps1) and, if so, which command runs Python 3 there."""
        if self.target and not SSH_KEY.exists():
            return "no SSH key for helpers yet (run NightShift.ps1 helpers on the PC)"
        repo = shlex.quote(self.repo)
        # What its models need: Ollama for one there (the fallback included), the internet for one of OpenCode's own.
        fallback = self.cfg.get("NIGHTSHIFT_FALLBACK_MODEL", "").strip()
        reach = []
        if not is_cloud(self.model) or (fallback and not is_cloud(fallback)):
            reach.append(f"curl -s --max-time 5 {shlex.quote(self.ollama)}/api/version >/dev/null || "
                         f"{{ echo \"can't reach Ollama at {self.ollama}\"; exit 1; }}")
        if is_cloud(self.model) or is_cloud(fallback):
            reach.append("curl -s --max-time 10 -o /dev/null https://opencode.ai || "
                         "{ echo \"can't reach opencode.ai, where OpenCode's own models run\"; exit 1; }")
        reach_lines = "\n".join(reach)
        script = f"""set -e
export PATH="$HOME/.local/bin:$HOME/.opencode/bin:$HOME/.dotnet:/opt/dotnet:$PATH"
mkdir -p {repo} && cd {repo} && (git rev-parse --git-dir >/dev/null 2>&1 || git init -q)
command -v opencode >/dev/null || {{ echo "OpenCode is not installed"; exit 1; }}
command -v dotnet >/dev/null || {{ echo ".NET SDK is not installed"; exit 1; }}
[ -f "{self.unityrefs}/Runtime.csproj" ] || {{ echo "Unity reference assemblies are missing"; exit 1; }}
{reach_lines}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*)
  git config core.autocrlf false && git config core.eol lf && git config core.longpaths true  # files as on Linux
  for py in python "py -3" ""; do  # python3 there is often the Microsoft Store's stand-in, not Python
    [ -n "$py" ] || {{ echo "Python 3 is not installed"; exit 1; }}
    if $py -c 'import sys; sys.exit(sys.version_info < (3, 9))' >/dev/null 2>&1; then echo "WINDOWS $py"; break; fi
  done ;;
esac
echo READY"""
        try:
            r = subprocess.run(self._cmd(script, wrap=True), capture_output=True, text=True, timeout=60,
                               stdin=subprocess.DEVNULL)
        except subprocess.TimeoutExpired:
            return "timed out"
        if "READY" not in r.stdout:
            return (r.stdout + r.stderr).strip()[-300:] or f"exit {r.returncode}"
        windows = re.search(r"^WINDOWS (.+?)\s*$", r.stdout, re.M)
        if windows:  # its job object lowers the priority there (see windows_job), so no nice
            self.windows, self.python, self.nice = True, windows.group(1), ""
        return None

    def run(self, task: Task, base: str, minutes: float, model: str, fix_rounds: int, context: int) -> Result:
        self.reap()  # a work process left over from an earlier task must be gone before its checkout is reused
        git(ROOT, "push", "-q", "--no-verify", "--force", self.dest, f"{base}:refs/heads/nightshift-base", env=self._git_env())
        b64 = base64.b64encode(task.text.encode("utf-8")).decode()
        safe_git = "git " + " ".join(shlex.quote(a) for a in GIT_SAFE)
        exports = 'PATH="$HOME/.local/bin:$HOME/.opencode/bin:$HOME/.dotnet:/opt/dotnet:$PATH"'
        if self.windows:  # UTF-8 whatever the code page: OpenCode's events, git's output and the result line
            exports += " PYTHONUTF8=1"
        # On Windows the probed command may be "py -3", so it isn't quoted.
        python = self.python if self.windows else shlex.quote(self.python)
        # exec makes the worker's Python the session leader, so its pid is the process group that reap() kills.
        script = (f"export {exports} && "
                  f"cd {shlex.quote(self.repo)} && {safe_git} checkout -q --detach --force nightshift-base && "
                  f"exec {self.nice}{python} {REL}/nightshift.py work --repo . --base {base} --task-b64 {b64} "
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
    # Windows has no signals to ask first, and reuses PIDs quickly: the process is ended (with everything it started)
    # only while it still is the worker. Its job object ends whatever is left (see windows_job).
    REAP_WINDOWS = ('p=$(cat {pid} 2>/dev/null); if [ -n "$p" ]; then '
                    'MSYS2_ARG_CONV_EXCL="*" powershell.exe -NoProfile -NonInteractive -Command '
                    r""""\$w = Get-CimInstance Win32_Process -Filter 'ProcessId = $p'; """
                    r"""if (\$w.CommandLine -like '*nightshift.py*work*') {{ taskkill /PID $p /T /F }}" >/dev/null 2>&1; """
                    'fi; rm -f {pid}; true')

    def reap(self, grace: int = 15) -> None:
        """Ends this worker's work process, if one is running (TERM, then KILL after `grace` seconds)."""
        script = (self.REAP_WINDOWS if self.windows else self.REAP).format(pid=shlex.quote(self.pid_file), grace=grace)
        try:
            subprocess.run(self._cmd(script), capture_output=True, timeout=grace + 30, stdin=subprocess.DEVNULL)
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
        # The model the night switches to once one of OpenCode's own stops answering (Big Pickle's free usage limit
        # reached, say); empty: the workers on that model stop instead.
        self.fallback = cfg.get("NIGHTSHIFT_FALLBACK_MODEL", "").strip()
        self.models_out: set[str] = set()  # cloud models that stopped answering tonight; their workers are on the fallback
        self.ollama = ""  # Ollama's address (find_ollama), set when the night starts
        self.minutes = float(cfg["NIGHTSHIFT_TASK_MINUTES"])
        self.fix_rounds = int(cfg["NIGHTSHIFT_FIX_ROUNDS"])
        self.context = int(cfg["NIGHTSHIFT_CONTEXT"])
        self.deadline = stop_time(cfg.get("NIGHTSHIFT_STOP_AT", ""))  # None: on duty until stopped
        # Workers on these models take only small tasks (see is_big); the others get the big ones.
        self.small_models = {m.strip().removesuffix(":latest")
                             for m in cfg.get("NIGHTSHIFT_SMALL_TASK_MODELS", "").split(",") if m.strip()}
        # Workers named here (or their host) take only documentation tasks (see is_docs); the others leave those to them.
        # "all" (EVERY_WORKER) names every worker, so the night shift writes documentation and nothing else.
        self.docs_names = {n.strip().lower() for n in cfg.get("NIGHTSHIFT_DOCS_WORKERS", "").split(",") if n.strip()}
        self.queue_lock, self.land_lock = threading.Lock(), threading.Lock()
        self.in_flight: dict[str, tuple[str, str]] = {}  # task key -> (worker, since)
        self.dispatches: dict[str, int] = {}  # task key -> times handed to a worker tonight (never decreases)
        self.completed: set[str] = set()
        self.frozen = False  # the project folder and the night's branch got out of step; commit nothing more there
        self.idle_since: str | None = None  # HH:MM when the last task finished and nothing was left to start
        self.workers: list[Worker] = []
        self.night: Night | None = None
        self.planner: Planner | None = None  # NIGHTSHIFT_AUTOPLAN: writes the next task when the queue runs low
        self.planning = threading.Event()  # set while the planner may still add tasks

    def until(self) -> str:
        return f"no new tasks after {self.deadline:%a %H:%M}" if self.deadline else "on duty until stopped"

    def takes_big(self, worker: Worker) -> bool:
        """Whether `worker` takes big tasks: not when its model is in NIGHTSHIFT_SMALL_TASK_MODELS."""
        return worker.model.removesuffix(":latest") not in self.small_models

    def fall_back(self, model: str, why: str) -> bool:
        """`model`, one of OpenCode's own, stopped answering (`why`: its free usage limit was reached, say). Every worker
        on it, and the planner, switch to NIGHTSHIFT_FALLBACK_MODEL for the rest of the night. Returns False when there's
        nothing to switch to: the workers on `model` then stop for the night."""
        if not self.fallback or model == self.fallback:
            return False
        with self.queue_lock:
            first = model not in self.models_out
            self.models_out.add(model)
            for w in self.workers:
                if w.model == model:
                    w.model = self.fallback
                    w.big = self.takes_big(w)
            if self.planner and getattr(self.planner, "model", None) == model:
                self.planner.model = self.fallback
            if self.model == model:
                self.model = self.fallback
            if first and self.night:
                self.night.switches.append(f"{dt.datetime.now():%H:%M} to `{self.fallback}`: `{model}` stopped answering "
                                           f"({why})")
                self.night.model = ", ".join(sorted({m.strip() for m in self.night.model.split(",") if m.strip()}
                                                    | {self.fallback}))
            if self.night:
                self.write_status()
        if first:
            log(f"{model} stopped answering ({why}); the rest of the night runs on {self.fallback}")
            if not is_cloud(self.fallback):
                try:
                    log(f"loading {self.fallback} into memory...")
                    warm_model(self.ollama, self.fallback)
                except Exception as e:  # the agents' first requests load it anyway
                    log(f"couldn't load {self.fallback} ahead of the agents ({first_line(str(e), 160)})")
        return True

    def start_model(self, model: str) -> str:
        """Gets `model` ready for the night and returns the model to start on. An Ollama model is loaded into memory.
        One of OpenCode's own is asked for a word first, and if it doesn't answer (its free usage limit reached, say),
        the night starts on NIGHTSHIFT_FALLBACK_MODEL instead; without one, it doesn't start."""
        if not is_cloud(model):
            log(f"loading {model} into memory...")
            warm_model(self.ollama, model)
            return model
        log(f"checking {model} answers...")
        try:
            if not has_model(self.ollama, model):
                raise ModelOut(model, "OpenCode doesn't offer it here")
            warm_model(self.ollama, model)
            return model
        except Exception as e:
            why = e.why if isinstance(e, ModelOut) else first_line(str(e), 200)
            if not self.fall_back(model, why):
                raise RuntimeError(f"{model} isn't answering ({why}), and NIGHTSHIFT_FALLBACK_MODEL names no model to "
                                   f"start on instead") from e
            return self.fallback

    def off_duty(self) -> bool:
        """True once no new task may start: stopping, a finish was requested, or the stop time has passed."""
        return STOPPING.is_set() or FINISH_FLAG.exists() or (self.deadline is not None and dt.datetime.now() >= self.deadline)

    def on_branch(self) -> bool:
        """False, and the night shift stops, once the project folder is on another branch than the night's (say after a
        branch switch in AI Workbench), so the agents' work can never land on main or anything else by accident."""
        current = git_run(ROOT, "rev-parse", "--abbrev-ref", "HEAD").stdout.strip()
        if current == self.night.branch:
            return True
        if not STOPPING.is_set():
            log(f"the project folder was switched from {self.night.branch} to {current or 'another branch'}, so the night "
                f"shift is stopping and nothing lands there. Switch back to {self.night.branch} and start it again.")
            STOPPING.set()
        return False

    def sync_landed_checkout(self, branch: str, mark: tuple[str, str, str] | None = None) -> None:
        """Brings the project folder up to the night's branch after a landing moved the branch, like `git checkout`.
        If tasks.md was edited there meanwhile (tasks queued from the PC or JupyterLab), the edit is kept and the
        landing's `mark` (task key, mark, note) is applied to it again; an edit that collides with the mark wins over
        it. Raises if the folder can't be brought up to date: the caller then moves the branch back, because a folder
        whose index lags behind its branch would turn the next commit into a revert of the landing."""
        lock_path = Path(git(ROOT, "rev-parse", "--git-path", "HEAD.lock").strip())
        if not lock_path.is_absolute():
            lock_path = ROOT / lock_path
        for _ in range(30):  # AI Workbench may be committing or switching branch this very moment; wait for it
            try:
                fd = os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o666)
                break
            except FileExistsError:
                if not clear_stale_lock(lock_path):
                    time.sleep(1)
        else:
            raise RuntimeError("the project folder's git stayed busy (HEAD.lock) for 30 s")
        try:
            if git_run(ROOT, "symbolic-ref", "--short", "HEAD").stdout.strip() != branch:
                return  # switched to another branch: none of the night's work is checked out, so there's nothing to refresh
            tasks_rel = f"{REL}/tasks.md"
            with tasks_lock():
                edited = None
                base = None
                if TASKS.exists():
                    edited = TASKS.read_bytes()
                    indexed = subprocess.run(["git", *GIT_SAFE, "-C", str(ROOT), "show", f":{tasks_rel}"],
                                             capture_output=True)
                    if indexed.returncode:
                        raise RuntimeError("couldn't save the task queue before refreshing it: "
                                           + first_line(indexed.stderr.decode("utf-8", "replace"), 300))
                    base = indexed.stdout
                    if git_run(ROOT, "diff", "--quiet", "--", tasks_rel).returncode:
                        git(ROOT, "checkout", "--", tasks_rel)  # the copy the landing started from, so the refresh can't trip
                refreshed = git_run(ROOT, "read-tree", "-m", "-u", "HEAD")
                if refreshed.returncode:
                    if edited is not None and not reconcile_task_edits(base, edited):
                        TASKS.write_bytes(edited)  # nothing else in the folder changed, so the edit as it was wins
                    raise RuntimeError("couldn't bring the project folder up to date with the landing: "
                                       + first_line(refreshed.stderr or refreshed.stdout, 300))
                if edited is not None and not reconcile_task_edits(base, edited, mark):
                    # The edit collides with the landing's own mark (say the landing task itself was reworded meanwhile).
                    # Raising now would move the branch back while the folder already follows the landing, and lose
                    # the edit, so the edit wins and the mark is applied to it again where it still fits.
                    text = edited.decode("utf-8", "surrogateescape")
                    redone = with_mark(text, *mark) if mark else None
                    TASKS.write_bytes((text if redone is None else redone).encode("utf-8", "surrogateescape"))
        finally:
            os.close(fd)
            lock_path.unlink(missing_ok=True)

    def undo_landing(self, ref: str, head: str, landed: str) -> None:
        """Moves the night's branch back to `head` when the project folder couldn't follow it to `landed`, so the folder
        matches its branch again. A failed refresh leaves the folder's consistency uncertain, so its caller freezes
        further commits even if this rollback succeeds. If the rollback fails, the night shift also stops."""
        undone = git_run(ROOT, "update-ref", ref, head, landed)
        if undone.returncode:
            self.frozen = True
            log(f"the night's branch and the project folder no longer match and couldn't be put back "
                f"({first_line(undone.stderr, 200)}), so the night shift is stopping without committing anything more. "
                f"Check 'git status' in the project before starting it again.")
            STOPPING.set()

    def write_status(self) -> None:
        status = {"pid": os.getpid(), "model": self.model, "deadline": f"{self.deadline:%a %H:%M}" if self.deadline else "",
                  "branch": self.night.branch if self.night else "", "workers": [w.name for w in self.workers],
                  "working": {k: {"worker": v[0], "since": v[1]} for k, v in self.in_flight.items()},
                  "dispatches": self.dispatches,
                  "idle_since": self.idle_since,
                  "autoplan": self.planner is not None,
                  "planned": self.planner.planned if self.planner else 0}
        write_atomic(STATUS_FILE, json.dumps(status, indent=2))

    def team(self) -> tuple[list, list]:
        """The workers still on duty that take big tasks, and those that take only small ones. Documentation workers are
        in neither (see docs_team)."""
        active = [w for w in self.workers if not getattr(w, "retired", False) and not getattr(w, "docs", False)]
        return [w for w in active if getattr(w, "big", True)], [w for w in active if not getattr(w, "big", True)]

    def docs_team(self) -> list:
        """The workers still on duty that take only documentation tasks (NIGHTSHIFT_DOCS_WORKERS)."""
        return [w for w in self.workers if not getattr(w, "retired", False) and getattr(w, "docs", False)]

    def left_out(self) -> set[str]:
        """The focuses the planner's rotation passes over for now: the documentation ones while documentation workers are
        on duty, as those get theirs on demand (see wanted and Planner.plan_docs)."""
        return set(DOC_KINDS) if self.docs_team() else set()

    def docs_only(self, worker: Worker) -> bool:
        """Whether NIGHTSHIFT_DOCS_WORKERS names `worker`, by its name, its host or its user@host, or is "all"."""
        target = (worker.target or "").lower()
        names = {worker.name.lower(), target, target.split("@")[-1]}
        return EVERY_WORKER in self.docs_names or bool(self.docs_names & names)

    def claim(self, worker: Worker) -> Task | None:
        with self.queue_lock:
            if self.off_duty() or not self.on_branch():
                return None
            tasks = read_tasks()
            big_takers, small_only = self.team()
            # Big tasks go only to the workers that take them, unless none of those is left on duty.
            takes_big = getattr(worker, "big", True) or not big_takers
            # Documentation tasks go only to the documentation workers while one of them is on duty, and those take
            # nothing else.
            docs_only = getattr(worker, "docs", False)
            leaves_docs = not docs_only and bool(self.docs_team())
            if self.planner:
                # Tasks people wrote go before the planner's. Of the planner's, a worker that takes big tasks takes those
                # first, leaving the small ones to the workers that take only small tasks. sorted() keeps the order
                # otherwise.
                planned, big_first = planned_keys(), getattr(worker, "big", True) and bool(small_only)
                tasks = sorted(tasks, key=lambda mark_task: (mark_task[1].key in planned, big_first and
                                                             mark_task[1].key in planned and not is_big(mark_task[1])))
            for mark, task in tasks:
                if mark == " " and task.key not in self.in_flight and task.key not in self.completed \
                        and self.dispatches.get(task.key, 0) < MAX_DISPATCH and (takes_big or not is_big(task)) \
                        and (is_docs(task) if docs_only else not (leaves_docs and is_docs(task))):
                    self.in_flight[task.key] = (worker.name, dt.datetime.now().strftime("%H:%M"))
                    self.dispatches[task.key] = self.dispatches.get(task.key, 0) + 1
                    self.idle_since = None
                    self.write_status()
                    return task
        return None

    def note_idle(self) -> None:
        """Says once, when the last task in progress has finished, that there's nothing left to start, so a quiet log
        doesn't look like a crash. The status shows it too, and NightShift.ps1 takes it as the cue for a pull request."""
        with self.queue_lock:
            if self.idle_since or self.in_flight:
                return
            self.idle_since = dt.datetime.now().strftime("%H:%M")
            self.write_status()
        landed = sum(1 for e in self.night.entries if e.outcome == "landed")
        if self.planner:
            log(f"idle: waiting for the planner's next task ({landed} of {len(self.night.entries)} landed so far) · "
                f"{self.until()}")
            return
        log(f"idle: nothing left to start in tasks.md ({landed} of {len(self.night.entries)} landed so far); "
            f"new tasks added there start within a minute · {self.until()}")

    def low_water(self) -> int:
        """How many tasks the planner keeps waiting: a couple more than there are workers (documentation workers aside:
        theirs are kept waiting separately, see wanted)."""
        return sum(1 for w in self.workers if not getattr(w, "docs", False)) + 2

    def wanted(self, waiting: list[Task], docs: bool = True) -> str | None:
        """What the planner should write next: "docs" when a documentation worker would find no documentation task
        waiting (one is kept waiting for each, unless `docs` is False: nothing was left to document a moment ago),
        "small" when a worker that takes only small tasks would find none waiting (one is kept waiting for each), "any"
        (the next focus in turn) when fewer than low_water() tasks are waiting, None when enough are. With no worker left
        that takes big tasks, everything it writes is small."""
        big_takers, small_only = self.team()
        docs_team = self.docs_team()
        if docs_team:
            if docs and sum(1 for task in waiting if is_docs(task)) < len(docs_team):
                return "docs"
            waiting = [task for task in waiting if not is_docs(task)]  # those wait for the documentation workers
            if not (big_takers or small_only):
                return None  # only documentation workers are left on duty
        if small_only and sum(1 for task in waiting if not is_big(task)) < len(small_only):
            return "small"
        if len(waiting) < self.low_water():
            return "any" if big_takers else "small"
        return None

    def start_planner(self, ollama: str) -> None:
        """With NIGHTSHIFT_AUTOPLAN on, starts the thread that keeps the queue topped up (see Planner)."""
        if self.cfg.get("NIGHTSHIFT_AUTOPLAN", "0").strip().lower() in ("", "0", "off", "no", "false"):
            return
        model = plan_model(self.cfg, ollama)
        if model in self.models_out:  # stopped answering before the night began
            model = self.fallback
        focus = [f.strip() for f in self.cfg.get("NIGHTSHIFT_AUTOPLAN_FOCUS", "").split(",") if f.strip()]
        # While documentation workers are on duty the rotation skips documentation: they get theirs on demand (wanted).
        self.planner = Planner(ollama, model, focus, on_duty=lambda: self.planning.is_set() and not self.off_duty(),
                               leave_out=self.left_out)
        self.planning.set()
        docs_team = self.docs_team()
        if docs_team and not any(self.team()):
            log(f"auto-planning on, documentation only: a documentation task is kept waiting for each of "
                f"{', '.join(w.name for w in docs_team)}")
        else:
            docs = f"; a documentation task is kept waiting for {', '.join(w.name for w in docs_team)}" if docs_team else ""
            log(f"auto-planning on: {model} writes the next task whenever fewer than {self.low_water()} are waiting "
                f"({', '.join(dict.fromkeys(self.planner.focus))}){docs}")
        threading.Thread(target=self.plan_loop, daemon=True).start()

    def plan_loop(self) -> None:
        misses = 0
        docs_misses, docs_after = 0, 0.0  # when nothing is left to document, when to look again
        while self.planning.is_set() and not self.off_duty() and self.on_branch():
            with self.queue_lock:
                waiting = [task for mark, task in read_tasks() if mark == " " and task.key not in self.in_flight
                           and task.key not in self.completed and self.dispatches.get(task.key, 0) < MAX_DISPATCH]
            want = self.wanted(waiting, docs=time.time() >= docs_after)
            if not want:
                misses = 0
                if STOPPING.wait(30):
                    return
                continue
            try:
                if want == "docs":
                    planned = self.planner.plan_docs()
                else:
                    planned = self.planner.plan_one(small=want == "small")
            except ModelOut as e:  # on to the fallback model, at once; without one, nothing more gets planned
                if self.fall_back(e.model, e.why):
                    continue
                log(f"planner: {e}, and there's no NIGHTSHIFT_FALLBACK_MODEL to ask instead, so nothing more is planned")
                return
            except Exception as e:  # Ollama busy or restarting, a script moved by a landing...
                log(f"planner: {first_line(str(e), 200)}")
                planned = None
            if want == "docs":
                # Without a script left to document, look again later; the other workers' tasks are planned meanwhile.
                docs_misses = 0 if planned else docs_misses + 1
                docs_after = time.time() + min(60 * docs_misses, 900) if docs_misses else 0.0
            if planned:
                misses = 0
                with self.queue_lock:
                    self.write_status()
            elif want != "docs":
                misses += 1  # back off, so a planner with nothing to say doesn't keep the model busy
                if STOPPING.wait(min(60 * misses, 900)):
                    return

    def commit_pending(self, subject: str, paths: list[str] | None = None, repo: Path = ROOT) -> None:
        """Commits changes under `paths` (default: everything) if there are any."""
        if self.frozen and repo == ROOT:
            return
        git(repo, "add", "-A", "--", *(paths or ["."]))
        if git_run(repo, "diff", "--cached", "--quiet").returncode != 0:
            git_commit(repo, subject)

    def add_lessons(self, lessons: list[str], repo: Path = ROOT) -> None:
        path = repo / LESSONS_REL
        body = path.read_text(encoding="utf-8") if path.exists() else "# Lessons\n"
        new = [l if l.startswith("- ") else "- " + l.lstrip("-* ") for l in lessons]
        new = [l for l in dict.fromkeys(new) if l not in body]
        if new:
            path.write_text(body.rstrip("\n") + "\n" + "\n".join(new) + "\n", encoding="utf-8")

    def land(self, worker: Worker, task: Task, res: Result) -> None:
        with self.land_lock:
            if self.frozen or not self.on_branch():
                return  # the task stays unchecked on the night's branch
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
            ref = f"refs/heads/{night.branch}"
            head = git(ROOT, "rev-parse", ref).strip()
            temp_dir = Path(tempfile.mkdtemp(prefix="nightshift-land-"))
            repo = temp_dir / "checkout"
            worktree_added = False
            original_dir, original_file = night.dir, night.file
            entry = Entry(len(night.entries) + 1, task.text, "", worker.name, res.minutes, res.summary, res.files)
            try:
                git(ROOT, "worktree", "add", "--quiet", "--detach", str(repo), head)
                worktree_added = True
                night.dir = repo / original_dir.relative_to(ROOT)
                night.file = repo / original_file.relative_to(ROOT)
                mark = self._land(worker, task, res, entry, head, can_retry, repo)
                landed_head = git(repo, "rev-parse", "HEAD").strip()
                if landed_head != head:
                    git(ROOT, "update-ref", ref, landed_head, head)
                    try:
                        self.sync_landed_checkout(night.branch, mark)
                    except Exception:
                        self.frozen = True
                        STOPPING.set()
                        log("the project folder may be out of sync after the failed refresh; Night Shift is stopping "
                            "without further commits. Check 'git status' before starting it again.")
                        self.undo_landing(ref, head, landed_head)
                        raise
                    self.completed.add(task.key)
            except Exception:
                if worktree_added:
                    git_run(repo, "cherry-pick", "--abort")
                    git_run(repo, "reset", "-q", "--hard", head)
                    git_run(repo, "clean", "-q", "-fd")
                if entry in night.entries:
                    night.entries.remove(entry)
                raise
            finally:
                night.dir, night.file = original_dir, original_file
                if worktree_added:
                    git_run(ROOT, "worktree", "remove", "--force", str(repo))
                shutil.rmtree(temp_dir, ignore_errors=True)

    def _land(self, worker: Worker, task: Task, res: Result, entry: Entry, head: str, can_retry: bool,
              repo: Path = ROOT) -> tuple[str, str, str] | None:
        """Lands the result in `repo` and returns the (task key, mark, note) it put in tasks.md, or None if it didn't."""
        night, stamp = self.night, dt.datetime.now().strftime("%H:%M")
        if res.status == "ok":
            pick = git_run(repo, "cherry-pick", "--no-commit", res.sha)
            # The landing checkout comes fresh from git, without the untracked reference assemblies; use the project's.
            ok, out = (False, "Conflicts with work landed earlier tonight:\n" + pick.stdout + pick.stderr) if pick.returncode \
                else compile_check(repo, dict(os.environ, NIGHTSHIFT_UNITYREFS=str(HERE / ".unityrefs")))
            if ok:
                self.add_lessons(res.lessons, repo)
                entry.outcome = "landed"
                note = f"{stamp} landed by {worker.name} in {res.minutes:g} min: {first_line(res.summary)}"
                mark_task(task.key, "x", note, repo / f"{REL}/tasks.md")
                night.add(entry)
                night.write()
                git(repo, "add", "-A")
                model = res.model or worker.model
                body = (f"{res.summary.strip()}\n\nTask: {task.text}\n\nWorker: {worker.name} · model {model} "
                        f"({model_home(model)}) · {res.minutes:g} min · compile check passed")
                entry.sha = git_commit(repo, "Night shift: " + task.key[:70], body)
                log(f"landed {entry.sha[:9]}: {task.key[:90]}", worker.name)
                return task.key, "x", note
            git_run(repo, "cherry-pick", "--abort")
            git(repo, "reset", "-q", "--hard", head)
            git(repo, "clean", "-q", "-fd")
            if can_retry:
                log("the change doesn't fit on top of tonight's other work; it goes back in the queue", worker.name)
                return None
            res.status, res.errors = "failed", out[-4000:]
            res.patch = git(repo, "format-patch", "--stdout", "-1", res.sha)
        if res.status == "nochange":
            entry.outcome, mark, note = "no change", "~", f"{stamp} {worker.name} made no changes: {first_line(res.summary)}"
        else:
            entry.outcome = "failed" if res.status == "failed" else "gave up"
            entry.errors = res.errors
            mark, note = "!", f"{stamp} {entry.outcome} ({worker.name}, {res.minutes:g} min); see reports/{night.file.name}"
            if res.patch:
                night.dir.mkdir(parents=True, exist_ok=True)
                entry.patch_file = str(night.dir / f"{entry.n:02d}-{slug(task.key)}.patch")
                Path(entry.patch_file).write_text(res.patch, encoding="utf-8")
        self.add_lessons(res.lessons, repo)
        mark_task(task.key, mark, note, repo / f"{REL}/tasks.md")
        night.add(entry)
        night.write()
        self.commit_pending(f"Night shift: {entry.outcome}: {task.key[:60]}", OWN_PATHS, repo)
        log(f"{entry.outcome}: {task.key[:90]}", worker.name)
        return task.key, mark, note

    def worker_loop(self, worker: Worker) -> None:
        try:
            self.take_tasks(worker)
        finally:
            worker.retired = True
            big_takers, small_only = self.team()
            if getattr(worker, "docs", False):
                if not self.docs_team() and any(self.team()) and not self.off_duty():
                    log("no documentation worker is left, so the others take documentation tasks now", worker.name)
            elif getattr(worker, "big", True) and small_only and not big_takers and not self.off_duty():
                log("no worker that takes big tasks is left, so the others take any task now", worker.name)

    def take_tasks(self, worker: Worker) -> None:
        while True:
            task = self.claim(worker)
            if not task:
                if self.off_duty() or self.cfg.get("NIGHTSHIFT_WAIT_FOR_TASKS", "1") == "0":
                    break
                self.note_idle()
                if STOPPING.wait(60):
                    break
                continue  # on duty: tasks added to tasks.md in the meantime get picked up
            log(f"starting: {first_line(task.key, 120)}", worker.name)
            with self.land_lock:
                base = git(ROOT, "rev-parse", "HEAD").strip()
            left = (self.deadline - dt.datetime.now()).total_seconds() / 60 + 30 if self.deadline else self.minutes
            try:
                res = worker.run(task, base, max(20.0, min(self.minutes, left)), worker.model, self.fix_rounds, self.context)
            except Exception as e:
                res = Result(status="error", errors=str(e)[-1500:])
            switched = False
            if res.model_out:  # the model stopped answering, not the task's doing: this try doesn't count against it
                with self.queue_lock:
                    self.dispatches[task.key] = max(0, self.dispatches.get(task.key, 1) - 1)
                switched = self.fall_back(res.model or worker.model, res.errors)
            worker.errors = worker.errors + 1 if res.status == "error" and not res.model_out else 0
            try:
                self.land(worker, task, res)
            except Exception as e:
                log(f"couldn't record the result ({e}); the task stays in the queue", worker.name)
            finally:
                with self.queue_lock:
                    self.in_flight.pop(task.key, None)
                    self.write_status()
            if res.model_out and not switched:
                log(f"{res.model or worker.model} stopped answering and there's no NIGHTSHIFT_FALLBACK_MODEL to switch to; "
                    f"this worker is done for the night", worker.name)
                break
            if worker.errors >= 2:
                log("two errors in a row; this worker is done for the night", worker.name)
                break

    def prepare_branch(self, date: str) -> str:
        current = git(ROOT, "rev-parse", "--abbrev-ref", "HEAD").strip()
        if current.startswith("nightshift/"):
            return current  # starting again (after a stop, or past midnight) carries on with the same branch and PR
        name, n = f"nightshift/{date}", 2
        while git(ROOT, "rev-parse", "--verify", "--quiet", f"refs/heads/{name}", check=False).strip():
            name, n = f"nightshift/{date}-{n}", n + 1
        git(ROOT, "switch", "-q", "-c", name)
        return name

    def run(self) -> None:
        log(f"Night shift starting · model {self.model} · {self.until()}")
        ollama = self.ollama = find_ollama(self.cfg)
        if not is_cloud(self.model) and not has_model(ollama, self.model):
            raise RuntimeError(f"Ollama at {ollama} doesn't have {self.model}; run 'ollama pull {self.model}' on the Spark")
        if self.fallback and not has_model(ollama, self.fallback):
            where = "offered by OpenCode here" if is_cloud(self.fallback) else f"in Ollama at {ollama}"
            log(f"NIGHTSHIFT_FALLBACK_MODEL {self.fallback} isn't {where}, so nothing takes over if a cloud model stops "
                f"answering")
            self.fallback = ""
        opencode_bin()
        if not (HERE / ".unityrefs" / "Runtime.csproj").exists():
            raise RuntimeError("Unity's reference assemblies aren't on the Spark yet; run 'NightShift.ps1 refs' on the PC")
        if not any(mark == " " for mark, _ in read_tasks()) and self.cfg.get("NIGHTSHIFT_WAIT_FOR_TASKS", "1") == "0":
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
        self.model = self.start_model(self.model)
        candidates = [Worker("spark", None, self.cfg, ollama)] if self.cfg["NIGHTSHIFT_LOCAL_WORKER"] != "0" else []
        candidates += [Worker(name, target, self.cfg, ollama) for name, target in helpers(self.cfg)]
        for w in candidates:
            if w.model in self.models_out:
                w.model = self.fallback
            if w.model != self.model and not has_model(ollama, w.model):
                where = "offered by OpenCode here" if is_cloud(w.model) else "in Ollama on the Spark"
                log(f"{w.model} isn't {where}; {w.name} uses {self.model} instead")
                w.model = self.model
            w.big = self.takes_big(w)
            w.docs = self.docs_only(w)
        known = {n for w in candidates for n in (w.name.lower(), (w.target or "").lower(),
                                                  (w.target or "").split("@")[-1].lower())}
        unknown = self.docs_names - known - {EVERY_WORKER}
        if unknown:
            log(f"NIGHTSHIFT_DOCS_WORKERS names {', '.join(sorted(unknown))}, which isn't a worker here "
                f"(set a helper up with NightShift.ps1 helpers)")
        for extra in sorted({w.model for w in candidates} - {self.model}):
            try:
                ready = self.start_model(extra)
            except RuntimeError as e:  # one of OpenCode's own that isn't answering, with no fallback
                log(f"{first_line(str(e), 200)}; its workers use {self.model} instead")
                ready = self.model
            for w in candidates:
                if w.model == extra:
                    w.model = ready
                    w.big = self.takes_big(w)
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
        self.night.workers = [f"{w.name} ({w.model}{', Windows' if w.windows else ''}"
                              f"{', documentation only' if w.docs else '' if w.big else ', small tasks only'})"
                              for w in self.workers]
        self.night.model = ", ".join(sorted({w.model for w in self.workers}))
        self.write_status()
        log("workers: " + ", ".join(self.night.workers))
        big_takers, small_only = self.team()
        if small_only and big_takers:
            log(f"big tasks (written in steps) go to {', '.join(w.name for w in big_takers)}; "
                f"{', '.join(w.name for w in small_only)} take only small ones")
        elif small_only:
            log("every worker's model is in NIGHTSHIFT_SMALL_TASK_MODELS, so they take any task and the planner writes "
                "only small ones")
        if self.docs_team() and not any(self.team()):
            log("every worker takes only documentation tasks (summaries, and comments explaining the code), so the night "
                "shift writes comments and nothing else; other tasks wait in tasks.md")
        elif self.docs_team():
            log(f"documentation tasks (summaries, and comments explaining the code) go to "
                f"{', '.join(w.name for w in self.docs_team())}, "
                f"which take{'s' if len(self.docs_team()) == 1 else ''} nothing else")
        self.start_planner(ollama)
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
        self.planning.clear()  # nothing more gets queued once the night's report is written
        with self.land_lock:
            if self.on_branch():
                self.night.unfinished = [t.key for m, t in read_tasks() if m == " " and self.dispatches.get(t.key)]
                self.night.write()
                self.commit_pending("Night shift: report", OWN_PATHS)
        landed = sum(1 for e in self.night.entries if e.outcome == "landed")
        log(f"done: {landed} of {len(self.night.entries)} tasks landed on {self.night.branch}; report in {REL}/reports/{self.night.file.name}")


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
        until = f"no new tasks after {st['deadline']}" if st.get("deadline") else "on duty until stopped"
        print(f"Branch {st.get('branch') or '-'} · model {st.get('model')} · workers {', '.join(st.get('workers', [])) or '-'}"
              f" · {until}")
        if pid and st.get("idle_since"):
            print(f"  idle since {st['idle_since']}: nothing left to start; tasks added to tasks.md start within a minute")
        if pid and st.get("autoplan"):
            print(f"  auto-planning on: {st.get('planned', 0)} tasks written by the planner since the start")
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


def cmd_state() -> int:
    """One line of JSON for NightShift.ps1: is the coordinator running, is anything left to do, and on which branch."""
    cfg = load_config()
    pid = coordinator_pid()
    st = json.loads(STATUS_FILE.read_text(encoding="utf-8")) if pid and STATUS_FILE.exists() else {}
    tasks = read_tasks()
    marks = [m for m, _ in tasks]
    dispatches = st.get("dispatches") or {}
    try:
        publish_every = float(cfg.get("NIGHTSHIFT_PUBLISH_EVERY") or 0)
    except ValueError:
        publish_every = 0.0
    print(json.dumps({
        "running": bool(pid),
        "branch": st.get("branch") or git(ROOT, "rev-parse", "--abbrev-ref", "HEAD", check=False).strip(),
        "working": len(st.get("working") or {}),
        "queued": marks.count(" "),
        "dispatchable": sum(1 for mark, task in tasks
                            if mark == " " and dispatches.get(task.key, 0) < MAX_DISPATCH),
        "idle_since": st.get("idle_since"),
        # The planner keeps the queue from running dry, so the pull request is published every publish_every minutes.
        "autoplan": bool(st.get("autoplan")),
        "publish_every": publish_every,
    }))
    return 0


def cmd_plan(args: argparse.Namespace) -> int:
    """Writes one task with the planner's model now, the way the planner does when the queue runs low."""
    cfg = load_config()
    ollama = find_ollama(cfg)
    model = args.model or plan_model(cfg, ollama)
    focus = [args.focus] if args.focus else [f.strip() for f in cfg["NIGHTSHIFT_AUTOPLAN_FOCUS"].split(",") if f.strip()]
    planner = Planner(ollama, model, focus, dry_run=args.dry_run)
    tries = 1 if args.target else 3
    try:
        task = planner.plan_one(tries=tries, focus=args.focus, target=args.target)
    except ModelOut as e:  # as the night shift does, ask the fallback model instead (unless a model was asked for)
        fallback = cfg.get("NIGHTSHIFT_FALLBACK_MODEL", "").strip()
        if args.model or not fallback or fallback == model:
            print(f"{e}.")
            return 1
        print(f"{e}; asking {fallback} instead.")
        model = planner.model = fallback
        task = planner.plan_one(tries=tries, focus=args.focus, target=args.target)
    if task is None:
        print("No task planned (see the reason above).")
        return 1
    print(("Would queue" if args.dry_run else "Queued") + f" ({model}):\n- [ ] " + task.text.replace("\n", "\n  "))
    return 0


def cmd_chat(extra: list[str]) -> int:
    cfg = load_config()
    path = write_opencode_config(ROOT, find_ollama(cfg), cfg["NIGHTSHIFT_MODEL"], int(cfg["NIGHTSHIFT_CONTEXT"]))
    env = dict(os.environ, OPENCODE_CONFIG=str(path), OPENCODE_DISABLE_AUTOUPDATE="true", OPENCODE_DISABLE_LSP_DOWNLOAD="true")
    exe = opencode_bin()
    os.execvpe(exe, [exe, str(ROOT), "--model", opencode_model(cfg["NIGHTSHIFT_MODEL"]), *extra], env)
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
    sub.add_parser("state", help="one line of JSON: running, busy or idle, and the branch (for NightShift.ps1)")
    sub.add_parser("map", help="rebuild memory/PROJECT_MAP.md")
    plan = sub.add_parser("plan", help="write one task with the planner's model now, as the planner (NIGHTSHIFT_AUTOPLAN) does")
    plan.add_argument("--focus", choices=focus_names(),
                      help="the kind of task, a trailing + for a big one (default: the next in NIGHTSHIFT_AUTOPLAN_FOCUS)")
    plan.add_argument("--target", help="the script to plan for, e.g. Assets/Scripts/World/DecorProps.cs")
    plan.add_argument("--model", help="the model to ask: in Ollama, or opencode/<id> for one of OpenCode's own "
                                      "(default: as the planner chooses)")
    plan.add_argument("--dry-run", action="store_true", help="show the task without queueing it")
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
    if args.cmd == "state":
        return cmd_state()
    if args.cmd == "map":
        (ROOT / MAP_REL).write_text(build_project_map(ROOT), encoding="utf-8")
        print(f"wrote {MAP_REL}")
        return 0
    if args.cmd == "plan":
        return cmd_plan(args)
    if args.cmd == "chat":
        return cmd_chat(args.extra)
    return cmd_work(args)


if __name__ == "__main__":
    sys.exit(main())
