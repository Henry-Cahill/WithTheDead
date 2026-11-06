#!/usr/bin/env python3
"""The release console: WithTheDead's release (Tools/Server/Release.ps1) as a page for anyone on the LAN. It builds the
game and its dedicated server from this PC's working copy or from GitHub's main, deploys the server to hts-llm,
publishes the game on its download page and pushes it to itch.io, installs and upgrades butler (itch.io's upload tool),
and shows what each of those places has now.

    python Tools/Server/Console/console.py [--port 8095] [--host 0.0.0.0] [--data DIR]

Standard library only (Python 3.10+, Windows). Each action is a job that runs this working copy's Release.ps1 in Windows
PowerShell, one job at a time. Its log streams to the page and stays, with the job history and the settings, in
%LOCALAPPDATA%\\CloneZ\\console. There's no login, but the console answers only to this PC's own addresses and names (so
no other site can reach it by DNS rebinding) and takes actions only as JSON from its own page (so no other site's page can
make it act). Install.ps1 starts it whenever you sign in to Windows. See Tools/Server/README.md, "Release console".
"""
from __future__ import annotations

import argparse
import base64
import codecs
import ctypes
import datetime as dt
import json
import logging
import logging.handlers
import os
import re
import shutil
import socket
import subprocess
import sys
import threading
import time
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse

HERE = Path(__file__).resolve().parent
WEB = HERE / "web"
PROJECT = HERE.parents[2]
RELEASE_PS1 = PROJECT / "Tools" / "Server" / "Release.ps1"
SELF_TEST = HERE / "SelfTest.ps1"
RELEASES = PROJECT / "Builds" / "Release"
MAIN_CHECKOUT = PROJECT.parent / (PROJECT.name + "-main")  # Release.ps1 -FromMain's clean checkout
LOCAL = Path(os.environ.get("LOCALAPPDATA") or Path.home() / "AppData" / "Local")
DATA = LOCAL / "CloneZ" / "console"
OUR_BUTLER = LOCAL / "CloneZ" / "butler" / "butler.exe"  # where Release.ps1 puts butler when it isn't on the PATH
POWERSHELL = Path(os.environ.get("SystemRoot") or r"C:\Windows") / "System32" / "WindowsPowerShell" / "v1.0" / "powershell.exe"

DEFAULT_PORT = 8095  # not 8090: Dell Repository Manager's service takes that on 127.0.0.1 now and then
_site = os.environ.get("CLONEZ_SITE_URL") or "https://withthedead.howto.software"
SITE = None if _site == "none" else _site.rstrip("/") + "/"
SERVER_HOST = os.environ.get("CLONEZ_SERVER_HOST") or "htsadmin@192.168.1.202"
SERVER_IP = SERVER_HOST.rsplit("@", 1)[-1]
SERVER_DIR = "clonez-server"
SITE_LAN = f"http://{SERVER_IP}:8077/"  # the download page's own server, behind the edge Caddy

NO_WINDOW = 0x08000000  # CREATE_NO_WINDOW: a job's console stays hidden
MAX_BODY = 16 * 1024
KEEP_JOBS = 200
STAMP = re.compile(r"\d{8}\.\d{4}\.[0-9a-f]{7,40}")
JOB_ID = re.compile(r"\d{8}-\d{6}-[a-z]+(?:-\d+)?")
ITCH = re.compile(r"[A-Za-z0-9_-]{1,64}/[A-Za-z0-9_-]{1,128}")
API_KEY = re.compile(r"[A-Za-z0-9_-]{20,128}")
SOURCES = {"working": "this PC's working copy", "main": "GitHub's main"}
TYPES = {".html": "text/html; charset=utf-8", ".js": "text/javascript; charset=utf-8", ".css": "text/css; charset=utf-8",
         ".json": "application/json; charset=utf-8", ".svg": "image/svg+xml", ".png": "image/png", ".ico": "image/x-icon"}
HEADERS = {"X-Content-Type-Options": "nosniff", "X-Frame-Options": "DENY", "Referrer-Policy": "no-referrer",
           "Content-Security-Policy": "default-src 'self'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'none'; "
                                      "form-action 'none'"}

log = logging.getLogger("console")


def _oem_codec() -> str:
    """Windows PowerShell 5 writes into a pipe in its console's code page, the OEM one (437 in the US)."""
    try:
        name = f"cp{ctypes.windll.kernel32.GetOEMCP()}"
        codecs.lookup(name)
        return name
    except (AttributeError, OSError, LookupError):
        return "cp437"


OEM = _oem_codec()
ESCAPES = re.compile(rb"\x1b\[[0-?]*[ -/]*[@-~]|\x1b[@-Z\\-_]")


def decode(raw: bytes) -> str:
    """Output as text: UTF-8 (Release.ps1 under the console, git, ssh, butler), else the OEM code page (Windows
    PowerShell's own default), without terminal colour codes."""
    raw = ESCAPES.sub(b"", raw)
    try:
        return raw.decode("utf-8")
    except UnicodeDecodeError:
        return raw.decode(OEM, errors="replace")


def now() -> str:
    return dt.datetime.now().astimezone().isoformat(timespec="seconds")


def write_json(path: Path, data) -> None:
    """Writes under a temporary name, then renames, so a reader never sees half a file."""
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(f".{path.name}.{os.getpid()}.tmp")
    tmp.write_text(json.dumps(data, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    for attempt in range(10):
        try:
            os.replace(tmp, path)
            return
        except PermissionError:
            if attempt == 9:
                tmp.unlink(missing_ok=True)
                raise
            time.sleep(0.05 * (attempt + 1))


def read_json(path: Path, default):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return default
    except (OSError, ValueError) as e:
        log.warning("can't read %s: %s", path, e)
        return default


def run(argv: list[str], timeout: float = 30) -> subprocess.CompletedProcess:
    return subprocess.run(argv, stdin=subprocess.DEVNULL, capture_output=True, timeout=timeout, creationflags=NO_WINDOW)


def lan_addresses() -> list[str]:
    """This PC's IPv4 addresses, the one it reaches the server from first."""
    found = []
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
            s.connect((SERVER_IP, 9))  # UDP: nothing is sent, it only picks the route
            found.append(s.getsockname()[0])
    except OSError:
        pass
    try:
        for info in socket.getaddrinfo(socket.gethostname(), None, socket.AF_INET):
            if info[4][0] not in found:
                found.append(info[4][0])
    except OSError:
        pass
    return found


class Names:
    """This PC's own names and addresses: the only Host the console answers to (with its port)."""

    def __init__(self, port: int):
        self.port = port
        self._hosts: frozenset[str] = frozenset()
        self._at = 0.0
        self._lock = threading.Lock()
        self._refresh()

    def _refresh(self) -> None:
        hostname = socket.gethostname().lower()
        names = {"localhost", "127.0.0.1", hostname, hostname + ".local", *lan_addresses()}
        try:
            names.add(socket.getfqdn().lower())
        except OSError:
            pass
        hosts = {f"{n}:{self.port}" for n in names}
        if self.port == 80:
            hosts |= names
        self._hosts = frozenset(hosts)
        self._at = time.monotonic()

    def allowed(self, host: str) -> bool:
        host = host.strip().lower()
        if host in self._hosts:
            return True
        # An address the PC has only just got (DHCP) gets in after a fresh look, taken at most every half minute.
        with self._lock:
            if time.monotonic() - self._at > 30:
                self._refresh()
        return host in self._hosts


class Settings:
    """What the page sets, kept outside git: the itch.io project, the source a release starts from, an itch.io API key."""

    def __init__(self, data: Path):
        self.path = data / "settings.json"
        self.key_path = data / "itch-api-key.txt"
        self._lock = threading.Lock()
        saved = read_json(self.path, {})
        saved = saved if isinstance(saved, dict) else {}
        itch = saved.get("itch")
        self.itch = itch if isinstance(itch, str) and ITCH.fullmatch(itch) else ""
        self.source = saved.get("source") if saved.get("source") in SOURCES else "working"

    def public(self) -> dict:
        return {"itch": self.itch, "source": self.source, "apiKey": self.key_path.is_file(), "folder": str(self.path.parent)}

    def update(self, body: dict) -> dict:
        with self._lock:
            if "itch" in body:
                itch = str(body["itch"] or "").strip()
                if itch and not ITCH.fullmatch(itch):
                    raise ValueError("The itch.io project is user/game, as in its address user.itch.io/game.")
                self.itch = itch
            if "source" in body:
                if body["source"] not in SOURCES:
                    raise ValueError("The source is working or main.")
                self.source = body["source"]
            write_json(self.path, {"itch": self.itch, "source": self.source})
        return self.public()

    def api_key(self) -> str:
        try:
            return self.key_path.read_text(encoding="utf-8").strip()
        except OSError:
            return ""

    def set_api_key(self, key) -> dict:
        key = str(key or "").strip()
        with self._lock:
            if not key:
                self.key_path.unlink(missing_ok=True)
            elif not API_KEY.fullmatch(key):
                raise ValueError("That doesn't look like an itch.io API key.")
            else:
                self.key_path.parent.mkdir(parents=True, exist_ok=True)
                self.key_path.write_text(key + "\n", encoding="utf-8")
        return self.public()


class Busy(Exception):
    def __init__(self, job: dict):
        super().__init__(f"\u201c{job['title']}\u201d is running: one job at a time.")
        self.job = job


class _BasicLimits(ctypes.Structure):
    _fields_ = [("PerProcessUserTimeLimit", ctypes.c_int64), ("PerJobUserTimeLimit", ctypes.c_int64),
                ("LimitFlags", ctypes.c_uint32), ("MinimumWorkingSetSize", ctypes.c_size_t),
                ("MaximumWorkingSetSize", ctypes.c_size_t), ("ActiveProcessLimit", ctypes.c_uint32),
                ("Affinity", ctypes.c_size_t), ("PriorityClass", ctypes.c_uint32), ("SchedulingClass", ctypes.c_uint32)]


class _ExtendedLimits(ctypes.Structure):
    _fields_ = [("BasicLimitInformation", _BasicLimits), ("IoInfo", ctypes.c_uint64 * 6),
                ("ProcessMemoryLimit", ctypes.c_size_t), ("JobMemoryLimit", ctypes.c_size_t),
                ("PeakProcessMemoryUsed", ctypes.c_size_t), ("PeakJobMemoryUsed", ctypes.c_size_t)]


class Group:
    """A Windows job object holding every job's processes, which Windows ends when the console's handle to it closes:
    however the console stops (its task ended, a crash, signing out), no job runs on unwatched. Processes that ask to
    break away may. Cancel still ends one job's processes by their id."""

    def __init__(self):
        self.handle = None
        try:
            k32 = ctypes.WinDLL("kernel32", use_last_error=True)
            k32.CreateJobObjectW.restype = ctypes.c_void_p
            k32.CreateJobObjectW.argtypes = [ctypes.c_void_p, ctypes.c_wchar_p]
            k32.SetInformationJobObject.argtypes = [ctypes.c_void_p, ctypes.c_int, ctypes.c_void_p, ctypes.c_uint32]
            k32.AssignProcessToJobObject.argtypes = [ctypes.c_void_p, ctypes.c_void_p]
            handle = k32.CreateJobObjectW(None, None)
            if not handle:
                raise ctypes.WinError(ctypes.get_last_error())
            limits = _ExtendedLimits()
            limits.BasicLimitInformation.LimitFlags = 0x2000 | 0x800  # KILL_ON_JOB_CLOSE, BREAKAWAY_OK
            if not k32.SetInformationJobObject(handle, 9, ctypes.byref(limits), ctypes.sizeof(limits)):
                raise ctypes.WinError(ctypes.get_last_error())
            self._k32, self.handle = k32, handle
        except (AttributeError, OSError) as e:
            log.warning("No job object (%s): a job's processes would outlive the console.", e)

    def add(self, proc: subprocess.Popen) -> None:
        if self.handle and not self._k32.AssignProcessToJobObject(self.handle, int(proc._handle)):
            log.warning("Process %s isn't in the job object (%s).", proc.pid, ctypes.WinError(ctypes.get_last_error()))


class Run:
    """The job that's running, with what only its runner needs."""

    def __init__(self, job: dict):
        self.job = job
        self.cancel = False
        self.done = False  # its log takes no more of its output


class Jobs:
    """The jobs, one at a time: each one a process whose output goes into its log a line at a time."""

    def __init__(self, data: Path):
        self.dir = data / "jobs"
        self.dir.mkdir(parents=True, exist_ok=True)
        self.index = data / "jobs.json"
        self._lock = threading.RLock()
        self._run: Run | None = None
        self._group = Group()
        history = read_json(self.index, [])
        history = history if isinstance(history, list) else []
        self.history = [j for j in history if isinstance(j, dict) and JOB_ID.fullmatch(str(j.get("id", "")))]
        for job in self.history:
            if job.get("status") in ("running", "cancelling"):
                job["status"] = "interrupted"
                job["ended"] = now()
                self.note(job, "The console stopped while this job was running, and its processes stopped with it, so "
                               "it's marked interrupted.")
        self._save()

    @property
    def current(self) -> dict | None:
        run_ = self._run
        return run_.job if run_ else None

    def log_path(self, job_id: str) -> Path:
        if not JOB_ID.fullmatch(job_id):
            raise KeyError(job_id)
        return self.dir / f"{job_id}.log"

    def get(self, job_id: str) -> dict | None:
        return next((j for j in self.history if j["id"] == job_id), None)

    def note(self, job: dict, text: str) -> None:
        """A line of the console's own in a job's log."""
        with open(self.log_path(job["id"]), "a", encoding="utf-8", newline="\n") as f:
            f.write(f"--- {text}\n")

    def _save(self) -> None:
        with self._lock:
            write_json(self.index, self.history)

    def start(self, action: str, title: str, argv: list[str], env: dict) -> dict:
        with self._lock:
            if self._run:
                raise Busy(self._run.job)
            stamp = dt.datetime.now().strftime("%Y%m%d-%H%M%S")
            job_id, n = f"{stamp}-{action}", 1
            while self.get(job_id) or self.log_path(job_id).exists():
                n += 1
                job_id = f"{stamp}-{action}-{n}"
            job = {"id": job_id, "action": action, "title": title, "command": subprocess.list2cmdline(argv),
                   "status": "running", "started": now(), "ended": None, "exit": None, "pid": None}
            with open(self.log_path(job_id), "w", encoding="utf-8", newline="\n") as f:
                f.write(f"--- {title}: started {job['started']}\n--- {job['command']}\n")
            run_ = self._run = Run(job)
            self.history.insert(0, job)
            for old in self.history[KEEP_JOBS:]:
                self.log_path(old["id"]).unlink(missing_ok=True)
            del self.history[KEEP_JOBS:]
            self._save()
        threading.Thread(target=self._go, args=(run_, argv, env), name=f"job {job_id}", daemon=True).start()
        log.info("started %s: %s", job_id, job["command"])
        return job

    def _go(self, run_: Run, argv: list[str], env: dict) -> None:
        job = run_.job
        try:
            proc = subprocess.Popen(argv, cwd=str(PROJECT), env=env, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                                    stderr=subprocess.STDOUT, creationflags=NO_WINDOW)
        except OSError as e:
            self.note(job, f"It couldn't start: {e}")
            return self._finish(run_, None)
        self._group.add(proc)
        with self._lock:
            job["pid"] = proc.pid
            self._save()
            cancelled = run_.cancel
        if cancelled:
            self._kill(run_)
        pump = threading.Thread(target=self._pump, args=(run_, proc.stdout), name=f"log {job['id']}", daemon=True)
        pump.start()
        code = proc.wait()
        # A process it left running (Unity's licensing client, say) can hold its output open: that isn't waited for.
        pump.join(10)
        if pump.is_alive():
            self.note(job, "Something it started is still running; what that prints isn't shown.")
        self._finish(run_, code)

    def _pump(self, run_: Run, stream) -> None:
        """Copies a job's output into its log. A line redrawn in place (a progress bar, \\r) is logged as it stands
        every few seconds rather than at every redraw."""
        shown = 0.0
        pending = b""
        with open(self.log_path(run_.job["id"]), "ab") as out:
            def put(raw: bytes) -> None:
                if not run_.done:
                    out.write(decode(raw).rstrip()[:8000].encode("utf-8") + b"\n")
                    out.flush()

            while True:
                chunk = stream.read1(65536)
                if not chunk:
                    break
                pending += chunk
                *lines, pending = pending.split(b"\n")
                for line in lines:
                    put(line.rstrip(b"\r").rsplit(b"\r", 1)[-1])
                if b"\r" in pending:
                    *redrawn, pending = pending.split(b"\r")
                    latest = next((r for r in reversed(redrawn) if r.strip()), b"")
                    if latest and time.monotonic() - shown > 3:
                        put(latest)
                        shown = time.monotonic()
                if len(pending) > 65536:
                    put(pending)
                    pending = b""
            if pending.strip():
                put(pending.rsplit(b"\r", 1)[-1])

    def _finish(self, run_: Run, code: int | None) -> None:
        job = run_.job
        run_.done = True
        status = "cancelled" if run_.cancel else "succeeded" if code == 0 else "failed"
        took = int((dt.datetime.now().astimezone() - dt.datetime.fromisoformat(job["started"])).total_seconds())
        self.note(job, status.capitalize() + (f" (exit {code})" if code is not None else "") +
                  f" after {took // 60} min {took % 60} s, at {now()}")
        with self._lock:
            job.update(status=status, exit=code, ended=now())
            if self._run is run_:
                self._run = None
            self._save()
        log.info("%s %s (exit %s)", job["id"], status, code)

    def cancel(self, job_id: str) -> dict:
        with self._lock:
            run_ = self._run
            if not run_ or run_.job["id"] != job_id:
                raise KeyError(job_id)
            first = not run_.cancel
            run_.cancel = True
            run_.job["status"] = "cancelling"
            self._save()
        if first:
            self._kill(run_)
        return run_.job

    def _kill(self, run_: Run) -> None:
        """Ends the job's process and every process under it, by its id (never by name)."""
        pid = run_.job.get("pid")
        if not pid:
            return  # it hasn't started yet: _go kills it the moment it has
        self.note(run_.job, f"Cancelled: stopping process {pid} and everything it started (taskkill /PID {pid} /T /F)")
        try:
            r = run(["taskkill", "/PID", str(pid), "/T", "/F"], timeout=60)
            for line in decode(r.stdout + r.stderr).splitlines():
                if line.strip():
                    self.note(run_.job, line.strip())
        except (OSError, subprocess.SubprocessError) as e:
            self.note(run_.job, f"taskkill failed: {e}")
        log.info("cancelled %s (process %s)", run_.job["id"], pid)

    def read_log(self, job_id: str, start: int) -> dict:
        job = self.get(job_id)
        path = self.log_path(job_id)
        if not job or not path.is_file():
            raise KeyError(job_id)
        size = path.stat().st_size
        start = max(0, min(start, size))
        with open(path, "rb") as f:
            f.seek(start)
            data = f.read(256 * 1024)
        end = data.rfind(b"\n") + 1  # whole lines only
        return {"id": job_id, "status": job["status"], "text": data[:end].decode("utf-8", "replace"),
                "next": start + end, "size": size}


# --- What each place has now ----------------------------------------------------------------------------------------------

_sizes: dict[str, tuple[float, int, int]] = {}
_versions: dict[str, tuple[float, str | None]] = {}


def dir_size(path: Path) -> int:
    total, stack = 0, [path]
    while stack:
        try:
            with os.scandir(stack.pop()) as entries:
                for e in entries:
                    try:
                        if e.is_dir(follow_symlinks=False):
                            stack.append(e.path)
                        else:
                            total += e.stat(follow_symlinks=False).st_size
                    except OSError:
                        pass
        except OSError:
            pass
    return total


def first_line(path: Path) -> str | None:
    try:
        with open(path, encoding="utf-8-sig") as f:
            return f.readline().strip() or None
    except OSError:
        return None


def release_version(d: Path) -> str | None:
    """As Release.ps1 reads it: version.txt, else the build log's line about it."""
    version = first_line(d / "version.txt")
    if version:
        return version
    build_log = d / "build.log"
    try:
        mtime = build_log.stat().st_mtime
    except OSError:
        return None
    hit = _versions.get(str(build_log))
    if hit and hit[0] == mtime:
        return hit[1]
    found = None
    with open(build_log, encoding="utf-8", errors="replace") as f:
        for line in f:
            m = re.match(r"\[CloneZBatch\] Release (\S+): built", line)
            if m:
                found = m[1]
                break
    _versions[str(build_log)] = (mtime, found)
    return found


def releases() -> list[dict]:
    """Builds\\Release, newest first. The newest with a server is the one Release.ps1 takes when it's given no stamp."""
    found = []
    if not RELEASES.is_dir():
        return found
    dirs = sorted((d for d in RELEASES.iterdir() if d.is_dir() and STAMP.fullmatch(d.name)), key=lambda d: d.name,
                  reverse=True)
    newest = next((d.name for d in dirs if (d / "server" / "WithTheDead.x86_64").is_file()), None)
    for d in dirs:
        mtime = d.stat().st_mtime_ns
        hit = _sizes.get(d.name)
        if not hit or hit[1] != mtime or time.monotonic() - hit[0] > 600:
            hit = (time.monotonic(), mtime, dir_size(d))
            _sizes[d.name] = hit
        client = d / "client"
        found.append({"stamp": d.name, "version": release_version(d), "newest": d.name == newest,
                      "client": (client / "WithTheDead.exe").is_file(),
                      "server": (d / "server" / "WithTheDead.x86_64").is_file(),
                      "size": hit[2], "joins": first_line(client / "server.txt"),
                      "updates": first_line(client / "updates.txt"), "zip": (d / f"WithTheDead-{d.name}.zip").is_file()})
    return found


_direct = urllib.request.build_opener(urllib.request.ProxyHandler({}))


def latest(base: str | None, direct: bool) -> dict:
    """What a download page hands out: its latest.json."""
    if not base:
        return {"url": None, "error": "no download page (CLONEZ_SITE_URL is none)"}
    url = base + "latest.json"
    req = urllib.request.Request(url, headers={"User-Agent": "WithTheDead release console", "Cache-Control": "no-cache"})
    try:
        with (_direct.open(req, timeout=8) if direct else urllib.request.urlopen(req, timeout=8)) as r:
            data = json.loads(r.read(1 << 16).decode("utf-8-sig"))
        return {"url": url, "version": data.get("version"), "published": data.get("published"), "file": data.get("file"),
                "size": data.get("size")}
    except Exception as e:  # noqa: BLE001 - the page says what went wrong
        return {"url": url, "error": f"{type(e).__name__}: {getattr(e, 'reason', None) or e}"}


def server_status() -> dict:
    """The release the server runs, its container's state and its last status line, over ssh (keys only)."""
    script = (f'echo "release=$(cat ~/{SERVER_DIR}/server/release.txt 2>/dev/null)"\n'
              "echo \"state=$(docker inspect -f '{{.State.Status}} {{.State.StartedAt}} {{.RestartCount}}' clonez-server "
              "2>/dev/null || echo missing)\"\n"
              "echo \"status=$(docker logs --since 30m clonez-server 2>&1 | grep -F '[Server] Status:' | tail -n 1)\"\n")
    # The script travels base64-encoded, as Release.ps1's do, so its quotes arrive as they are.
    b64 = base64.b64encode(script.encode("utf-8")).decode("ascii")
    info = {"host": SERVER_HOST}
    try:
        r = run(["ssh", "-o", "BatchMode=yes", "-o", "ConnectTimeout=8", SERVER_HOST, f"echo {b64} | base64 -d | bash"], 25)
    except (OSError, subprocess.SubprocessError) as e:
        return {**info, "error": f"ssh: {e}"}
    if r.returncode:
        return {**info, "error": decode(r.stderr).strip() or f"ssh failed (exit {r.returncode})"}
    values = dict(line.split("=", 1) for line in decode(r.stdout).splitlines() if "=" in line)
    state = (values.get("state") or "missing").split()
    return {**info, "release": values.get("release") or None, "state": state[0],
            "since": state[1] if len(state) > 1 else None, "restarts": int(state[2]) if len(state) > 2 else None,
            "status": (values.get("status") or "").replace("[Server] Status:", "").strip() or None}


def git_status() -> dict:
    def git(*args, cwd: Path = PROJECT) -> str:
        r = run(["git", "--no-optional-locks", "-C", str(cwd), *args])
        if r.returncode:
            raise RuntimeError((decode(r.stderr).strip().splitlines() or [f"git {args[0]} failed"])[-1])
        return r.stdout.decode("utf-8", "replace")

    def commit(ref: str, cwd: Path = PROJECT) -> dict:
        h, subject, date = git("log", "-1", "--format=%h%x1f%s%x1f%cI", ref, cwd=cwd).strip().split("\x1f")
        return {"commit": h, "subject": subject, "date": date}

    working = commit("HEAD")
    working["branch"] = git("rev-parse", "--abbrev-ref", "HEAD").strip()
    working["changed"] = sum(1 for line in git("status", "--porcelain").splitlines() if line.strip())
    ahead, behind = (int(n) for n in git("rev-list", "--left-right", "--count", "HEAD...origin/main").split())
    working.update(ahead=ahead, behind=behind, path=str(PROJECT))
    main = commit("origin/main")
    fetch_head = PROJECT / ".git" / "FETCH_HEAD"
    if fetch_head.is_file():
        main["fetched"] = dt.datetime.fromtimestamp(fetch_head.stat().st_mtime).astimezone().isoformat(timespec="seconds")
    checkout = {"path": str(MAIN_CHECKOUT), "exists": (MAIN_CHECKOUT / ".git").exists()}
    if checkout["exists"]:
        try:
            checkout.update(commit("HEAD", cwd=MAIN_CHECKOUT))
        except RuntimeError as e:
            checkout["error"] = str(e)
    return {"working": working, "main": main, "checkout": checkout}


_butler_versions: dict[str, tuple[float, str]] = {}


def butler_path() -> Path | None:
    """As Release.ps1 finds it: on the PATH, else its own copy."""
    found = shutil.which("butler")
    if found:
        return Path(found)
    return OUR_BUTLER if OUR_BUTLER.is_file() else None


def butler_creds() -> Path:
    """Where butler keeps its login (its defaultKeyPath)."""
    if os.environ.get("XDG_CONFIG_PATH"):
        return Path(os.environ["XDG_CONFIG_PATH"])
    home = os.environ.get("HOME") or os.environ.get("USERPROFILE") or str(Path.home())
    return Path(home) / ".config" / "itch" / "butler_creds"


def butler_status(settings: Settings) -> dict:
    path = butler_path()
    creds = butler_creds()
    info = {"installed": bool(path), "path": str(path or OUR_BUTLER), "ours": path == OUR_BUTLER, "version": None,
            "creds": str(creds), "credentials": creds.is_file(), "apiKey": bool(settings.api_key()),
            "envKey": bool(os.environ.get("BUTLER_API_KEY"))}
    info["loggedIn"] = info["credentials"] or info["apiKey"] or info["envKey"]
    if path:
        mtime = path.stat().st_mtime
        hit = _butler_versions.get(str(path))
        if not hit or hit[0] != mtime:
            try:
                r = run([str(path), "version"], 15)
                lines = [line.strip() for line in decode(r.stdout + r.stderr).splitlines() if line.strip()]
                hit = (mtime, lines[0] if lines else "version unknown")
            except (OSError, subprocess.SubprocessError) as e:
                hit = (mtime, f"can't run it: {e}")
            _butler_versions[str(path)] = hit
        info["version"] = hit[1]
    return info


# --- The console ----------------------------------------------------------------------------------------------------------

class Console:
    def __init__(self, data: Path, port: int):
        self.data = data
        self.port = port
        self.settings = Settings(data)
        self.jobs = Jobs(data)
        self.names = Names(port)
        self.started = now()
        self._cache: dict[str, tuple[float, object]] = {}
        self._locks = {name: threading.Lock() for name in ("releases", "lan", "public", "server", "git", "butler")}
        self._pool = ThreadPoolExecutor(12, thread_name_prefix="status")

    def urls(self) -> list[str]:
        return [f"http://{a}:{self.port}/" for a in lan_addresses()[:1]] + [f"http://{socket.gethostname().lower()}:{self.port}/"]

    def _compute(self, name: str, fn):
        try:
            value = fn()
        except Exception as e:  # noqa: BLE001 - the page shows what went wrong
            log.warning("status %s: %s", name, e)
            value = {"error": f"{type(e).__name__}: {e}"}
        self._cache[name] = (time.monotonic(), value)
        return value

    def _part(self, name: str, ttl: float, fn, fresh: bool):
        """A part of the status. Once it's been worked out, an old one is answered at once while a new one is worked out
        behind it (at most one at a time), so a slow ssh never holds the page up."""
        lock = self._locks[name]
        hit = self._cache.get(name)
        if hit and not fresh:
            if time.monotonic() - hit[0] >= ttl and lock.acquire(blocking=False):
                def refresh():
                    try:
                        self._compute(name, fn)
                    finally:
                        lock.release()
                self._pool.submit(refresh)
            return hit[1]
        with lock:
            return self._compute(name, fn)

    def status(self, fresh: bool = False) -> dict:
        parts = {"releases": (10, releases), "lan": (20, lambda: latest(SITE_LAN, True)),
                 "public": (20, lambda: latest(SITE, False)), "server": (20, server_status), "git": (8, git_status),
                 "butler": (15, lambda: butler_status(self.settings))}
        futures = {name: self._pool.submit(self._part, name, ttl, fn, fresh) for name, (ttl, fn) in parts.items()}
        got = {name: f.result() for name, f in futures.items()}
        return {"console": {"host": socket.gethostname(), "urls": self.urls(), "project": str(PROJECT),
                            "data": str(self.data), "started": self.started, "site": SITE, "siteLan": SITE_LAN,
                            "time": now()},
                "releases": got["releases"], "site": {"lan": got["lan"], "public": got["public"]},
                "server": got["server"], "git": got["git"], "butler": got["butler"], "settings": self.settings.public(),
                "job": self.jobs.current, "jobs": self.jobs.history[:30]}

    def start(self, body: dict) -> dict:
        """Starts the job an action asks for: Release.ps1 with its arguments as a list, in an environment the console
        controls (a CLONEZ_ITCH from outside can't add an itch.io push to a job that isn't one)."""
        action = body.get("action")
        source = body.get("source") or self.settings.source
        if source not in SOURCES:
            raise ValueError("The source is working or main.")
        stamp = body.get("stamp") or None
        if stamp is not None and not (isinstance(stamp, str) and STAMP.fullmatch(stamp) and (RELEASES / stamp).is_dir()):
            raise ValueError(f"There's no release {stamp} on this PC.")
        on = ["-Stamp", stamp] if stamp else []
        which = f" {stamp}" if stamp else " (the newest)"
        main = ["-FromMain"] if source == "main" else []
        if action in ("release", "deploy") and body.get("confirm") is not True:
            raise ValueError("Confirm it first: the server restarts, and a raid under way on it is lost.")
        itch = self.settings.itch
        push = action == "itch" or (action == "release" and body.get("itch") is True)
        if push:
            if not itch:
                raise ValueError("Set the itch.io project (user/game) first.")
            if not butler_status(self.settings)["loggedIn"]:
                raise ValueError("Log butler in to itch.io first, or give the console an itch.io API key.")
        script = [str(POWERSHELL), "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File"]
        if action == "release":
            title = f"Release from {SOURCES[source]}" + (", and push to itch.io" if push else "")
            args = main + (["-Itch", itch] if push else [])
        elif action == "build":
            title, args = f"Build from {SOURCES[source]}", main + ["-NoDeploy", "-NoPublish"]
        elif action == "deploy":
            title, args = "Deploy" + which, ["-NoBuild", "-NoPublish"] + on
        elif action == "publish":
            title, args = "Publish to the download page" + which, ["-NoBuild", "-NoDeploy"] + on
        elif action == "itch":
            title, args = f"Push to itch.io ({itch})" + which, ["-NoBuild", "-NoDeploy", "-NoSite", "-Itch", itch] + on
        elif action == "check":
            title, args = "Check" + which, ["-NoBuild", "-NoDeploy", "-NoPublish"] + on
        elif action == "butler":
            title, args = "Install or upgrade butler", ["-Butler", "Update"]
        elif action == "login":
            title, args = "Log butler in to itch.io", ["-Butler", "Login"]
        elif action == "fetch":
            return self.jobs.start("fetch", "Fetch from GitHub", ["git", "-C", str(PROJECT), "fetch", "origin"],
                                   self._env(False))
        elif action == "selftest":
            return self.jobs.start("selftest", "Self-test", script + [str(SELF_TEST)], self._env(False))
        else:
            raise ValueError(f"No such action: {action!r}")
        return self.jobs.start(action, title, script + [str(RELEASE_PS1)] + args, self._env(push))

    def _env(self, push: bool) -> dict:
        env = dict(os.environ)
        env.pop("CLONEZ_ITCH", None)
        key = self.settings.api_key() if push else ""
        if key:
            env["BUTLER_API_KEY"] = key
        return env


class Handler(BaseHTTPRequestHandler):
    server_version = "WithTheDeadReleaseConsole/1"
    protocol_version = "HTTP/1.1"
    timeout = 60
    console: Console

    # --- GET -------------------------------------------------------------------------------------------------------------

    def do_GET(self):
        if not self.allowed():
            return
        url = urlparse(self.path)
        path = url.path
        try:
            if path == "/":
                # The page names its script and styles with their file times, so a browser never keeps an old one.
                html = (WEB / "index.html").read_text(encoding="utf-8")
                for name in ("console.css", "console.js"):
                    html = html.replace(f'"{name}"', f'"{name}?v={int((WEB / name).stat().st_mtime)}"')
                return self.send_bytes(html.encode("utf-8"), TYPES[".html"], "no-cache")
            if path == "/favicon.ico":
                return self.send_bytes(b"", "image/x-icon", "public, max-age=86400", status=204)
            if path.startswith("/api/"):
                return self.api_get(path[5:], parse_qs(url.query))
            name = path[1:]
            if re.fullmatch(r"[\w-]+\.(html|js|css|svg|png|ico)", name) and (WEB / name).is_file():
                return self.send_file(WEB / name)
            self.send_json({"error": "not found"}, 404)
        except KeyError:
            self.send_json({"error": "no such job"}, 404)
        except Exception as e:  # noqa: BLE001 - the page shows what went wrong
            log.exception("GET %s", self.path)
            self.send_json({"error": f"{type(e).__name__}: {e}"}, 500)

    def api_get(self, what: str, query: dict):
        c = self.console
        if what == "status":
            return self.send_json(c.status(fresh="fresh" in query))
        if what == "jobs":
            return self.send_json({"current": c.jobs.current, "history": c.jobs.history[:50]})
        m = re.fullmatch(r"jobs/(" + JOB_ID.pattern + r")(/log)?", what)
        if not m:
            return self.send_json({"error": "not found"}, 404)
        if m[2]:
            start = (query.get("from") or ["0"])[0]
            return self.send_json(c.jobs.read_log(m[1], int(start) if start.isdigit() else 0))
        job = c.jobs.get(m[1])
        if not job:
            raise KeyError(m[1])
        self.send_json(job)

    # --- POST ------------------------------------------------------------------------------------------------------------

    def do_POST(self):
        if not self.allowed():
            return
        length = int(self.headers.get("Content-Length") or 0)
        if not 0 < length <= MAX_BODY:
            self.close_connection = True
            return self.send_json({"error": "send a JSON body of up to 16 KB"}, 413 if length else 400)
        raw = self.rfile.read(length)
        # JSON only (a cross-site form can't send it without the browser asking first, which this server never allows),
        # and not from another site's page.
        if not (self.headers.get("Content-Type") or "").startswith("application/json"):
            return self.send_json({"error": "send JSON"}, 415)
        if self.headers.get("Sec-Fetch-Site", "same-origin") not in ("same-origin", "none"):
            return self.refuse("Actions come only from this console's own page.")
        c = self.console
        path = urlparse(self.path).path
        try:
            body = json.loads(raw.decode("utf-8"))
            if not isinstance(body, dict):
                raise ValueError("send a JSON object")
            if path == "/api/jobs":
                return self.send_json({"job": c.start(body)}, 202)
            m = re.fullmatch(r"/api/jobs/(" + JOB_ID.pattern + r")/cancel", path)
            if m:
                return self.send_json({"job": c.jobs.cancel(m[1])})
            if path == "/api/settings":
                return self.send_json({"settings": c.settings.update(body)})
            if path == "/api/settings/key":
                return self.send_json({"settings": c.settings.set_api_key(body.get("key"))})
            self.send_json({"error": "not found"}, 404)
        except Busy as b:
            self.send_json({"error": str(b), "job": b.job}, 409)
        except KeyError:
            self.send_json({"error": "no such job running"}, 404)
        except (ValueError, UnicodeDecodeError) as e:
            self.send_json({"error": str(e)}, 400)
        except Exception as e:  # noqa: BLE001 - the page shows what went wrong
            log.exception("POST %s", self.path)
            self.send_json({"error": f"{type(e).__name__}: {e}"}, 500)

    # --- Plumbing --------------------------------------------------------------------------------------------------------

    def allowed(self) -> bool:
        """Only this PC's own names and addresses (no DNS rebinding), and no other site's page."""
        host = (self.headers.get("Host") or "").strip()
        if not self.console.names.allowed(host):
            return self.refuse("This console answers only to this PC's own addresses and names.")
        origin = self.headers.get("Origin")
        if origin is not None:
            o = urlparse(origin)
            if o.scheme != "http" or o.netloc.lower() != host.lower():
                return self.refuse("Not from this console's own page.")
        return True

    def refuse(self, why: str) -> bool:
        log.warning("refused %s %s from %s (Host %r, Origin %r): %s", self.command, self.path, self.client_address[0],
                    self.headers.get("Host"), self.headers.get("Origin"), why)
        self.close_connection = True
        self.send_json({"error": why}, 403)
        return False

    def send_json(self, data, status: int = 200):
        self.send_bytes(json.dumps(data, ensure_ascii=False, separators=(",", ":")).encode("utf-8"),
                        "application/json; charset=utf-8", "no-store", status=status)

    def send_file(self, path: Path):
        self.send_bytes(path.read_bytes(), TYPES.get(path.suffix, "application/octet-stream"), "no-cache")

    def send_bytes(self, data: bytes, kind: str, cache: str, status: int = 200):
        self.send_response(status)
        self.send_header("Content-Type", kind)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", cache)
        for name, value in HEADERS.items():
            self.send_header(name, value)
        if self.close_connection:
            self.send_header("Connection", "close")
        self.end_headers()
        self.wfile.write(data)

    def log_message(self, format, *args):  # noqa: A002 - BaseHTTPRequestHandler's name
        if len(args) > 1 and str(args[1]).startswith(("4", "5")):
            log.warning("%s %s", self.client_address[0], format % args)


class Server(ThreadingHTTPServer):
    daemon_threads = True
    # One console to a port: Windows would let a second one share it under SO_REUSEADDR.
    allow_reuse_address = False

    def server_bind(self):
        if hasattr(socket, "SO_EXCLUSIVEADDRUSE"):
            self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_EXCLUSIVEADDRUSE, 1)
        super().server_bind()


def make_server(console: Console, host: str = "0.0.0.0", port: int = DEFAULT_PORT) -> Server:
    handler = type("ConsoleHandler", (Handler,), {"console": console})
    return Server((host, port), handler)


def main(argv: list | None = None) -> int:
    parser = argparse.ArgumentParser(description="WithTheDead's release console: the release steps as a page on the LAN")
    parser.add_argument("--host", default="0.0.0.0", help="the address to listen on (default: all of them, for the LAN)")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--data", type=Path, default=DATA, help="where the settings, the job history and the logs go")
    args = parser.parse_args(argv)
    args.data.mkdir(parents=True, exist_ok=True)
    handlers: list[logging.Handler] = [logging.handlers.RotatingFileHandler(args.data / "console.log", maxBytes=1 << 20,
                                                                            backupCount=3, encoding="utf-8")]
    if sys.stderr:  # not under pythonw
        handlers.append(logging.StreamHandler())
    logging.basicConfig(level=logging.INFO, handlers=handlers, format="%(asctime)s %(levelname)s %(message)s")
    threading.excepthook = lambda a: log.error("thread %s died", a.thread.name if a.thread else "?",
                                               exc_info=(a.exc_type, a.exc_value, a.exc_traceback))
    if not RELEASE_PS1.is_file():
        log.error("There's no %s: the console belongs in Tools/Server/Console of the project.", RELEASE_PS1)
        return 1
    console = Console(args.data, args.port)
    try:
        server = make_server(console, args.host, args.port)
    except OSError as e:
        log.error("Can't listen on %s:%s (%s). Is the console running already?", args.host, args.port, e)
        return 1
    log.info("Release console for %s, pid %s: %s (data in %s)", PROJECT, os.getpid(), "  ".join(console.urls()), args.data)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
