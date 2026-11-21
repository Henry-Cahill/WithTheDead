"""Polite, resumable downloading for long unattended runs.

* one keep-alive HTTPS connection per worker thread and one global request-rate limit
* retries with exponential backoff; Retry-After is honoured
* a cool-down when most recent requests fail (site trouble, Wi-Fi drop), then it carries on
* files are written atomically and skipped when already on disk, so every run resumes
* stopping is graceful: requests in flight finish and nothing half-written is left behind
"""
from __future__ import annotations

import collections
import errno
import gzip
import http.client
import json
import logging
import os
import random
import shutil
import ssl
import threading
import time
import zlib
from concurrent.futures import FIRST_COMPLETED, ThreadPoolExecutor, wait
from datetime import datetime
from urllib.parse import urlsplit

USER_AGENT = "CLONE-Z-MapScraper/1.0 (personal offline archive; rate-limited)"

log = logging.getLogger("mapscraper")


class StopRequested(Exception):
    """Raised in workers once a stop has been requested."""


class FetchError(Exception):
    """A file could not be downloaded (it is retried on the next run)."""


def now_iso():
    return datetime.now().astimezone().isoformat(timespec="seconds")


def fmt_bytes(n):
    n = float(n or 0)
    for unit in ("B", "KB", "MB", "GB", "TB"):
        if abs(n) < 1024 or unit == "TB":
            return f"{n:.0f} {unit}" if unit == "B" else f"{n:.1f} {unit}"
        n /= 1024


def fmt_duration(seconds):
    if seconds is None:
        return "?"
    seconds = int(seconds)
    hours, rest = divmod(seconds, 3600)
    minutes, secs = divmod(rest, 60)
    if hours:
        return f"{hours}h{minutes:02d}m"
    return f"{minutes}m{secs:02d}s" if minutes else f"{secs}s"


def write_atomic(path, data, attempts=5):
    """Write through a .part file and rename it into place. Antivirus scanners and the Windows
    indexer briefly lock fresh files, so sharing violations are retried for a moment."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = f"{path}.part"
    for attempt in range(attempts):
        try:
            with open(tmp, "wb") as f:
                f.write(data)
            os.replace(tmp, path)
            return
        except PermissionError:
            if attempt == attempts - 1:
                raise
            time.sleep(0.2 * (attempt + 1))


def check_body(path, body):
    """Reject error pages and damaged files that arrive with status 200."""
    ext = path.rsplit(".", 1)[-1].lower()
    if ext == "webp" and not (body[:4] == b"RIFF" and body[8:12] == b"WEBP"):
        raise ValueError("response is not a WebP image")
    if ext == "png" and not body.startswith(b"\x89PNG\r\n\x1a\n"):
        raise ValueError("response is not a PNG image")
    if ext in ("json", "geojson"):
        json.loads(body)


class RateLimiter:
    """Spaces requests evenly across all threads and can pause everything for a cool-down."""

    def __init__(self, per_second, stop_event):
        self.interval = 1.0 / per_second if per_second and per_second > 0 else 0.0
        self.stop_event = stop_event
        self.lock = threading.Lock()
        self.next_at = time.monotonic()
        self.paused_until = 0.0

    def pause(self, seconds):
        with self.lock:
            self.paused_until = max(self.paused_until, time.monotonic() + seconds)

    def wait(self):
        with self.lock:
            now = time.monotonic()
            at = max(now, self.next_at, self.paused_until)
            self.next_at = at + self.interval
        if at > now and self.stop_event.wait(at - now):
            raise StopRequested()


class HttpClient:
    def __init__(self, base_url, rate, stop_event, timeout=60, retries=6):
        parts = urlsplit(base_url)
        self.host, self.port = parts.hostname, parts.port or 443
        self.prefix = parts.path.rstrip("/")
        self.stop_event = stop_event
        self.limiter = RateLimiter(rate, stop_event)
        self.timeout, self.retries = timeout, retries
        self.ssl = ssl.create_default_context()
        self.local = threading.local()
        self.recent = collections.deque(maxlen=40)
        self.health_lock = threading.Lock()
        self.cooldowns = 0

    def _conn(self):
        conn = getattr(self.local, "conn", None)
        if conn is None:
            conn = http.client.HTTPSConnection(self.host, self.port, timeout=self.timeout, context=self.ssl)
            self.local.conn = conn
        return conn

    def _drop(self):
        conn = getattr(self.local, "conn", None)
        self.local.conn = None
        if conn is not None:
            try:
                conn.close()
            except Exception:
                pass

    def _once(self, method, path):
        self.limiter.wait()
        conn = self._conn()
        try:
            conn.request(method, f"{self.prefix}/{path}", headers={
                "User-Agent": USER_AGENT, "Accept": "*/*", "Accept-Encoding": "gzip"})
            resp = conn.getresponse()
            body = resp.read()
        except (http.client.HTTPException, OSError):
            self._drop()
            raise
        if (resp.getheader("Connection") or "").lower() == "close":
            self._drop()
        if body and (resp.getheader("Content-Encoding") or "").lower() == "gzip":
            body = gzip.decompress(body)
        elif resp.status == 200 and method == "GET":
            expected = resp.getheader("Content-Length") or ""
            if expected.isdigit() and int(expected) != len(body):
                raise ValueError(f"truncated download ({len(body)} of {expected} bytes)")
        return resp.status, resp, body

    def fetch(self, path, method="GET"):
        """Return (status, headers, body) for 200 or 404. Raises FetchError once retries run out."""
        delay, problem = 2.0, ""
        for attempt in range(1, self.retries + 1):
            if self.stop_event.is_set():
                raise StopRequested()
            try:
                status, resp, body = self._once(method, path)
                if status == 200 and method == "GET":
                    check_body(path, body)
                if status in (200, 404):
                    self._health(True)
                    return status, resp.headers, body
                problem = f"HTTP {status}"
                retry_after = resp.getheader("Retry-After") or ""
                if retry_after.isdigit():
                    delay = max(delay, min(float(retry_after), 900.0))
                if status < 500 and status not in (403, 408, 425, 429):
                    self._health(True)
                    raise FetchError(f"{path}: HTTP {status}")
            except (http.client.HTTPException, OSError, ValueError, EOFError, zlib.error) as exc:
                problem = f"{type(exc).__name__}: {exc}"
            self._health(False)
            if attempt < self.retries:
                if self.stop_event.wait(delay * random.uniform(0.75, 1.25)):
                    raise StopRequested()
                delay = min(delay * 2, 120.0)
        raise FetchError(f"{path}: {problem} (gave up after {self.retries} tries)")

    def _health(self, ok):
        """Pause all requests when most of the recent ones failed, backing off up to 15 minutes."""
        with self.health_lock:
            self.recent.append(ok)
            if ok:
                if self.cooldowns and len(self.recent) >= 20 and all(list(self.recent)[-20:]):
                    self.cooldowns = 0
                return
            if len(self.recent) < self.recent.maxlen or self.recent.count(False) < 0.75 * len(self.recent):
                return
            pause = min(60 * 2 ** self.cooldowns, 900)
            self.cooldowns += 1
            self.recent.clear()
        log.warning("Most requests are failing (site or network trouble). Cooling down for %d s, then resuming.", pause)
        self.limiter.pause(pause)


class DiskIndex:
    """Answers "is this file already downloaded?" from cached directory listings."""

    def __init__(self, root):
        self.root = root
        self.lock = threading.Lock()
        self.listing = {}

    def _names(self, folder):
        names = self.listing.get(folder)
        if names is None:
            try:
                names = set(os.listdir(os.path.join(self.root, *folder.split("/")) if folder else self.root))
            except FileNotFoundError:
                names = set()
            self.listing[folder] = names
        return names

    def has(self, rel):
        folder, _, name = rel.rpartition("/")
        with self.lock:
            return name in self._names(folder)

    def add(self, rel):
        folder, _, name = rel.rpartition("/")
        with self.lock:
            self._names(folder).add(name)


class PathLog:
    """Append-only list of relative paths (known 404s), kept in a set."""

    def __init__(self, path):
        self.path = path
        self.lock = threading.Lock()
        self.items = set()
        if os.path.exists(path):
            with open(path, encoding="utf-8") as f:
                self.items = {line.strip() for line in f if line.strip()}

    def __contains__(self, rel):
        return rel in self.items

    def __len__(self):
        return len(self.items)

    def add(self, rel):
        with self.lock:
            if rel in self.items:
                return
            self.items.add(rel)
            with open(self.path, "a", encoding="utf-8") as f:
                f.write(rel + "\n")


class Progress:
    """Counters for the running phase, saved to state/progress.json every couple of seconds.
    The heartbeat thread also logs a progress line every minute, notices the STOP file and stops
    the run when free disk space gets low."""

    def __init__(self, state_dir, out_dir, stop_event, min_free_gb, meta):
        self.path = os.path.join(state_dir, "progress.json")
        self.stop_file = os.path.join(state_dir, "STOP")
        self.out_dir = out_dir
        self.stop_event = stop_event
        self.min_free = min_free_gb * 1024 ** 3
        self.meta = meta
        self.lock = threading.Lock()
        self.started = now_iso()
        self.state, self.stop_reason, self.message = "running", "", ""
        self.phase, self.detail = "starting", ""
        self.phases = {}
        self.samples = collections.deque()
        self.halt = threading.Event()
        self.thread = None

    def begin(self, phase, total):
        with self.lock:
            self.phase, self.detail = phase, ""
            self.phases[phase] = {"total": total, "done": 0, "downloaded": 0, "skipped": 0,
                                  "missing": 0, "failed": 0, "bytes": 0}
            self.samples.clear()
            self.samples.append((time.monotonic(), 0, 0))
        log.info("-- %s: %s items", phase, f"{total:,}")

    def note(self, phase, text=""):
        """Switch to a phase without counters (exports, image building)."""
        with self.lock:
            self.phase, self.detail = phase, text
            self.phases.setdefault(phase, {})

    def add(self, downloaded=0, skipped=0, missing=0, failed=0, nbytes=0):
        with self.lock:
            c = self.phases.get(self.phase)
            if not c or "total" not in c:
                return
            c["downloaded"] += downloaded
            c["skipped"] += skipped
            c["missing"] += missing
            c["failed"] += failed
            c["bytes"] += nbytes
            c["done"] += downloaded + skipped + missing + failed

    def set_detail(self, text):
        if self.detail != text:
            with self.lock:
                self.detail = text

    def request_stop(self, reason):
        with self.lock:
            first = not self.stop_reason
            if first:
                self.stop_reason = reason
        if first:
            log.info("Stopping: %s. Requests in flight will finish first.", reason)
        self.stop_event.set()

    def _rates(self):
        c = self.phases.get(self.phase) or {}
        if "total" not in c or len(self.samples) < 2:
            return 0.0, 0.0, None
        t0, w0, b0 = self.samples[0]
        t1, w1, b1 = self.samples[-1]
        dt = max(t1 - t0, 1e-6)
        per_s, bytes_s = (w1 - w0) / dt, (b1 - b0) / dt
        remaining = c["total"] - c["done"]
        return per_s, bytes_s, (remaining / per_s if per_s > 0 else None)

    def snapshot(self):
        with self.lock:
            per_s, bytes_s, eta = self._rates()
            return {**self.meta, "state": self.state, "stop_reason": self.stop_reason, "message": self.message,
                    "started_at": self.started, "updated_at": now_iso(), "phase": self.phase,
                    "detail": self.detail, "phases": {k: dict(v) for k, v in self.phases.items()},
                    "rate_per_s": round(per_s, 2), "rate_bytes_s": round(bytes_s),
                    "eta_s": None if eta is None else int(eta)}

    def save(self):
        snap = self.snapshot()
        tmp = self.path + ".tmp"
        try:
            with open(tmp, "w", encoding="utf-8") as f:
                json.dump(snap, f, indent=1)
            for _ in range(5):
                try:
                    os.replace(tmp, self.path)
                    break
                except PermissionError:  # a reader has the file open for a moment
                    time.sleep(0.1)
        except OSError:
            pass
        return snap

    def line(self):
        snap = self.snapshot()
        c = snap["phases"].get(snap["phase"]) or {}
        if "total" not in c:
            return f"{snap['phase']} {snap['detail']}".strip()
        total = c["total"]
        pct = 100.0 * c["done"] / total if total else 100.0
        text = (f"{snap['phase']} {c['done']:,}/{total:,} ({pct:.1f}%) | new {c['downloaded']:,} "
                f"({fmt_bytes(c['bytes'])}) | had {c['skipped']:,} | 404 {c['missing']:,} | failed {c['failed']:,} "
                f"| {snap['rate_per_s']:.1f}/s {fmt_bytes(snap['rate_bytes_s'])}/s | ETA {fmt_duration(snap['eta_s'])}")
        return text + (f" | {snap['detail']}" if snap["detail"] else "")

    def start(self):
        self.thread = threading.Thread(target=self._beat, name="heartbeat", daemon=True)
        self.thread.start()

    def _beat(self):
        last_log = time.monotonic()
        while not self.halt.wait(2.0):
            now = time.monotonic()
            with self.lock:
                c = self.phases.get(self.phase) or {}
                if "total" in c:
                    self.samples.append((now, c["downloaded"] + c["missing"] + c["failed"], c["bytes"]))
                    while len(self.samples) > 2 and now - self.samples[0][0] > 300:
                        self.samples.popleft()
            if os.path.exists(self.stop_file):
                self.request_stop("stop requested")
            try:
                free = shutil.disk_usage(self.out_dir).free
                if free < self.min_free:
                    self.request_stop(f"low disk space ({fmt_bytes(free)} free)")
            except OSError:
                pass
            self.save()
            if now - last_log >= 60:
                last_log = now
                log.info(self.line())

    def finish(self, state, message=""):
        with self.lock:
            self.state, self.message = state, message
        self.halt.set()
        if self.thread:
            self.thread.join(timeout=5)
        self.save()


class Downloader:
    def __init__(self, client, raw_dir, workers, progress, stop_event, missing, max_new=None):
        self.client = client
        self.raw_dir = raw_dir
        self.workers = max(1, workers)
        self.progress = progress
        self.stop_event = stop_event
        self.missing = missing
        self.max_new = max_new
        self.new = 0
        self.disk = DiskIndex(raw_dir)
        self.failures = []
        self.write_streak = 0
        self.lock = threading.Lock()

    def run(self, phase, items, total, force=False, retry_missing=False):
        """Download relative paths (str, or (detail, str) tuples) that are not on disk yet.
        Returns False if the run was stopped before the phase finished."""
        self.progress.begin(phase, total)
        inflight = set()
        limit_hit = False
        pool = ThreadPoolExecutor(max_workers=self.workers, thread_name_prefix=phase)
        try:
            for item in items:
                if self.stop_event.is_set():
                    break
                if isinstance(item, tuple):
                    detail, rel = item
                    self.progress.set_detail(detail)
                else:
                    rel = item
                if not force and self.disk.has(rel):
                    self.progress.add(skipped=1)
                    continue
                if not retry_missing and rel in self.missing:
                    self.progress.add(missing=1)
                    continue
                if self.max_new is not None and self.new >= self.max_new:
                    limit_hit = True
                    break
                self.new += 1
                inflight.add(pool.submit(self._get, rel))
                while len(inflight) >= self.workers * 3:
                    _, inflight = wait(inflight, timeout=1.0, return_when=FIRST_COMPLETED)
            while inflight:
                _, inflight = wait(inflight, timeout=1.0)
            if limit_hit:
                self.progress.request_stop(f"download limit of {self.max_new} new files reached")
        except KeyboardInterrupt:
            self.progress.request_stop("Ctrl+C")
            raise
        finally:
            pool.shutdown(wait=True, cancel_futures=True)
        log.info(self.progress.line())
        return not self.stop_event.is_set()

    def _get(self, rel):
        try:
            status, _, body = self.client.fetch(rel)
        except StopRequested:
            return
        except Exception as exc:  # FetchError, or anything unexpected: count it, retry next run
            self.progress.add(failed=1)
            with self.lock:
                self.failures.append(str(exc))
                count = len(self.failures)
            if count <= 20:
                log.warning("failed: %s", exc)
            elif count == 21:
                log.warning("(further failures are counted but not logged; they are retried next run)")
            return
        if status == 404:
            self.missing.add(rel)
            self.progress.add(missing=1)
            return
        try:
            write_atomic(os.path.join(self.raw_dir, *rel.split("/")), body)
        except OSError as exc:
            self.progress.add(failed=1)
            with self.lock:
                self.failures.append(f"{rel}: cannot write ({exc})")
                self.write_streak += 1
                streak = self.write_streak
            # A full disk or repeated failures mean writing is broken; a one-off is retried next run.
            if exc.errno == errno.ENOSPC or streak >= 5:
                self.progress.request_stop(f"cannot write files ({exc})")
            else:
                log.warning("could not write %s (%s); it is retried next run", rel, exc)
            return
        with self.lock:
            self.write_streak = 0
        self.disk.add(rel)
        self.progress.add(downloaded=1, nbytes=len(body))


def keep_awake(on):
    """Keep Windows from sleeping while a run is active (the display can still turn off)."""
    if os.name != "nt":
        return
    try:
        import ctypes
        es_continuous, es_system_required = 0x80000000, 0x00000001
        ctypes.windll.kernel32.SetThreadExecutionState(es_continuous | (es_system_required if on else 0))
    except Exception:
        pass


class RunLock:
    """OS file lock so only one run uses an output folder. The OS releases it if the process dies."""

    def __init__(self, path):
        self.path = path
        self.handle = None

    def acquire(self):
        handle = open(self.path, "a+b")
        try:
            if os.name == "nt":
                import msvcrt
                handle.seek(0)
                msvcrt.locking(handle.fileno(), msvcrt.LK_NBLCK, 1)
            else:
                import fcntl
                fcntl.flock(handle.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError:
            handle.close()
            return False
        self.handle = handle
        return True

    def release(self):
        if self.handle is None:
            return
        try:
            if os.name == "nt":
                import msvcrt
                self.handle.seek(0)
                msvcrt.locking(self.handle.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                import fcntl
                fcntl.flock(self.handle.fileno(), fcntl.LOCK_UN)
        except OSError:
            pass
        self.handle.close()
        self.handle = None

    def held_elsewhere(self):
        if self.acquire():
            self.release()
            return False
        return True
