#!/usr/bin/env python3
"""The Kentucky Atlas in a browser: the world store's buildings drawn over Kentucky's own aerial photographs (KyFromAbove,
3 inches a pixel), to check each building's outline and kind against them and record what was found, with the corrections
the game applies, in the store's world/validation folder. The editor's Kentucky Atlas (WithTheDead > Kentucky Atlas) shares
those files and checks what the browser can't: how the game builds each building, its windows, doors and inside.

    python Tools/World/atlas/atlas.py [--port 8765] [--host 127.0.0.1] [--store PATH] [--no-browser]

Standard library only (Python 3.10+). The store is CLONEZ_WORLD, else \\\\192.168.1.170\\CLONE-Z on Windows or /mnt/clonez,
as for the world builder. The page reads the current build's region files as they are (the browser unzips them) and
projects Kentucky's grid (EPSG:3088) itself (web/ky3088.js); this server only serves them and keeps the check files.
Map data (c) OpenStreetMap contributors (ODbL). See Tools/World/README.md, "Checking the buildings".
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import socket
import sys
import threading
import time
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse

HERE = Path(__file__).resolve().parent
WEB = HERE / "web"

FORMAT = 1  # the validation files' format (MapValidation.Format in the game)
CHECKS = ("footprint", "kind", "outside", "windows", "doors", "inside")
VALUES = ("", "ok", "bad", "na")
MAX_BODY = 64 * 1024
REGION_NAME = re.compile(r"(-?\d{1,6})_(-?\d{1,6})")
TYPES = {".html": "text/html; charset=utf-8", ".js": "text/javascript; charset=utf-8", ".css": "text/css; charset=utf-8",
         ".json": "application/json; charset=utf-8", ".svg": "image/svg+xml", ".png": "image/png", ".ico": "image/x-icon"}


def default_store() -> Path:
    return Path(os.environ.get("CLONEZ_WORLD") or (r"\\192.168.1.170\CLONE-Z" if os.name == "nt" else "/mnt/clonez"))


def now() -> str:
    return dt.datetime.now().astimezone().isoformat(timespec="seconds")


def blank_game() -> dict:
    """What the game made of a building, as the editor's atlas records it (MapValidation's BuildingSeen); never set here."""
    return {"seen": "", "arch": "", "style": "", "roof": "", "wall": "", "storeys": 0, "enterable": False, "doors": 0,
            "windows": 0, "blockedDoors": 0, "furnished": False, "use": "", "rooms": "", "containers": 0, "lights": 0,
            "problems": ""}


def is_empty(record: dict) -> bool:
    """A record that says nothing (nothing checked, no note, no correction) isn't kept."""
    fix = record.get("fix") or {}
    return (not any((record.get("checks") or {}).get(c) for c in CHECKS) and not record.get("note")
            and not any(v for v in fix.values()))


def status(record: dict | None) -> str:
    """unchecked, bad (any check failed), ok (every check passed or doesn't apply) or partial: as MapValidation.StatusOf."""
    checks = (record or {}).get("checks") or {}
    values = [checks.get(c, "") for c in CHECKS]
    if "bad" in values:
        return "bad"
    done = sum(v in ("ok", "na") for v in values)
    return "unchecked" if done == 0 else "ok" if done == len(CHECKS) else "partial"


def write_json(path: Path, data) -> None:
    """Writes under a temporary name, then renames, as everything in the store is written (retrying a moment while the
    editor has the file open)."""
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(f".{path.name}.{socket.gethostname()}.{os.getpid()}.tmp")
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


class Conflict(Exception):
    """The record changed since the page read it (the editor, or another page, saved it)."""

    def __init__(self, current: dict | None):
        super().__init__("changed elsewhere")
        self.current = current


class Atlas:
    """The store as the page sees it: the current build's files, and the check files it reads and writes."""

    def __init__(self, store: Path, vocab: dict | None = None, who: str | None = None):
        self.store = Path(store)
        self.world = self.store / "world"
        self.checks_dir = self.world / "validation" / "regions"
        self.vocab = vocab or json.loads((WEB / "vocab.json").read_text(encoding="utf-8"))
        self.who = who or "web@" + socket.gethostname()
        self._lock = threading.Lock()
        self._summary: dict[str, tuple[float, dict]] = {}
        self._current: tuple[float, dict] | None = None

    # --- The build ---------------------------------------------------------------------------------------------------

    def current(self) -> dict:
        """world/current.json, with the build's grid and its regions (as [rx, rz, buildings]) from its manifest; read
        again when current.json changes (a new build published)."""
        path = self.world / "current.json"
        mtime = path.stat().st_mtime
        if self._current and self._current[0] == mtime:
            return self._current[1]
        cur = json.loads(path.read_text(encoding="utf-8-sig"))
        build = cur.get("build") or ""
        if not re.fullmatch(r"[\w.-]+", build):
            raise FileNotFoundError("world/current.json names no build")
        manifest = json.loads((self.world / "builds" / build / "manifest.json").read_text(encoding="utf-8-sig"))
        info = {"build": build, "published": cur.get("published", ""), "format": manifest.get("format", 1),
                "attribution": manifest.get("attribution") or "Map data \u00a9 OpenStreetMap contributors (ODbL)",
                "region": manifest.get("region") or 8192, "cell": manifest.get("cell") or 256,
                "regions": [[r["rx"], r["rz"], r.get("buildings", 0)] for r in manifest.get("regions", [])]}
        self._current = (mtime, info)
        return info

    def build_folder(self) -> Path:
        return self.world / "builds" / self.current()["build"]

    def places_file(self) -> Path:
        return self.build_folder() / "places.json"

    def region_file(self, rx: int, rz: int) -> Path:
        return self.build_folder() / "regions" / f"{rx}_{rz}.json.gz"

    # --- The checks --------------------------------------------------------------------------------------------------

    def checks_file(self, rx: int, rz: int) -> Path:
        return self.checks_dir / f"{rx}_{rz}.json"

    def load_checks(self, rx: int, rz: int) -> dict:
        """A region's check file (an empty one when it has none). Raises when it can't be read, so a save never writes
        over a file it couldn't read."""
        path = self.checks_file(rx, rz)
        if not path.exists():
            return {"format": FORMAT, "rx": rx, "rz": rz, "buildings": []}
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        data["buildings"] = [r for r in data.get("buildings") or [] if isinstance(r, dict) and isinstance(r.get("id"), int)]
        return data

    def summary(self) -> list:
        """Every region with checks: [rx, rz, records, ok, partial, bad, corrected]."""
        out = []
        if not self.checks_dir.is_dir():
            return out
        for path in self.checks_dir.iterdir():
            m = REGION_NAME.fullmatch(path.name[:-5]) if path.name.endswith(".json") else None
            if not m:
                continue
            try:
                mtime = path.stat().st_mtime
                cached = self._summary.get(path.name)
                if not cached or cached[0] != mtime:
                    records = self.load_checks(int(m[1]), int(m[2]))["buildings"]
                    states = [status(r) for r in records]
                    counts = [len(records), states.count("ok"), states.count("partial"), states.count("bad"),
                              sum(1 for r in records if any(v for v in (r.get("fix") or {}).values()))]
                    cached = self._summary[path.name] = (mtime, counts)
                out.append([int(m[1]), int(m[2]), *cached[1]])
            except (OSError, ValueError):
                continue
        return out

    def clean(self, body) -> dict:
        """A record as the page sent it, checked and with nothing else in it."""
        if not isinstance(body, dict):
            raise ValueError("a record is a JSON object")
        bid = body.get("id")
        if not isinstance(bid, int) or isinstance(bid, bool) or bid <= 0:
            raise ValueError("id must be a building's id")
        given = body.get("checks") or {}
        fix = body.get("fix") or {}
        if not isinstance(given, dict) or not isinstance(fix, dict):
            raise ValueError("checks and fix are JSON objects")
        checks = {}
        for name in CHECKS:
            value = given.get(name) or ""
            if value not in VALUES:
                raise ValueError(f"checks.{name} must be one of {', '.join(repr(v) for v in VALUES)}")
            checks[name] = value
        hide = fix.get("hide", False)
        if not isinstance(hide, bool):
            raise ValueError("fix.hide must be true or false")
        storeys = fix.get("storeys", 0) or 0
        if not isinstance(storeys, int) or isinstance(storeys, bool) or not 0 <= storeys <= 40:
            raise ValueError("fix.storeys must be 0 (the game's choice) to 40")
        v = self.vocab
        return {"id": bid, "checks": checks, "note": text(body.get("note"), 2000, "note"),
                "fix": {"hide": hide, "kind": choice(fix.get("kind"), v["kinds"], "fix.kind"),
                        "use": text(fix.get("use"), 64, "fix.use"), "name": text(fix.get("name"), 120, "fix.name"),
                        "arch": choice(fix.get("arch"), v["archetypes"], "fix.arch"),
                        "style": choice(fix.get("style"), v["styles"], "fix.style"),
                        "roof": choice(fix.get("roof"), v["roofs"], "fix.roof"),
                        "material": choice(fix.get("material"), v["materials"], "fix.material"),
                        "facing": choice(fix.get("facing"), v["facings"], "fix.facing"), "storeys": storeys}}

    def save(self, rx: int, rz: int, body) -> dict | None:
        """Saves the checks, note and corrections the page sent for one building (what the game made of it stays as the
        editor recorded it). body["base"], when given, is the record's "updated" as the page read it ("" for none): a
        record saved since raises Conflict. Returns the record as saved, or None when it said nothing and was dropped."""
        record = self.clean(body)
        base = body.get("base")
        build = self.current()["build"]
        with self._lock:
            data = self.load_checks(rx, rz)
            old = next((r for r in data["buildings"] if r["id"] == record["id"]), None)
            if base is not None and (old or {}).get("updated", "") != base:
                raise Conflict(old)
            record["game"] = {**blank_game(), **((old or {}).get("game") or {})}
            record.update(build=build, by=self.who, updated=now())
            keep = [r for r in data["buildings"] if r["id"] != record["id"]]
            if not is_empty(record):
                keep.append(record)
            keep.sort(key=lambda r: r["id"])
            write_json(self.checks_file(rx, rz), {"format": FORMAT, "rx": rx, "rz": rz, "buildings": keep})
        return None if is_empty(record) else record


def text(value, limit: int, what: str) -> str:
    if value is None:
        return ""
    if not isinstance(value, str):
        raise ValueError(f"{what} must be text")
    value = "".join(ch for ch in value if ch in "\n\t" or ch >= " ").strip()
    if len(value) > limit:
        raise ValueError(f"{what} is longer than {limit} characters")
    return value


def choice(value, allowed: list, what: str) -> str:
    value = text(value, 64, what)
    if value and value not in allowed:
        raise ValueError(f"{what} must be one of {', '.join(allowed)}")
    return value


class Handler(BaseHTTPRequestHandler):
    server_version = "KentuckyAtlas/1"
    protocol_version = "HTTP/1.1"
    atlas: Atlas
    loopback: bool

    # --- GET -------------------------------------------------------------------------------------------------------------

    def do_GET(self):
        if not self.host_ok():
            return
        path = urlparse(self.path).path
        try:
            if path == "/":
                return self.send_file(WEB / "index.html", "no-cache")
            if path == "/favicon.ico":
                return self.send_bytes(b"", "image/x-icon", "public, max-age=86400", status=204)
            if path.startswith("/api/"):
                return self.api_get(path[5:])
            name = path[1:]
            if re.fullmatch(r"[\w-]+\.(html|js|css|json|svg|png|ico)", name) and (WEB / name).is_file():
                return self.send_file(WEB / name, "no-cache")
            self.send_json({"error": "not found"}, 404)
        except FileNotFoundError as e:
            self.send_json({"error": f"not in the store: {e}"}, 404)
        except Exception as e:  # noqa: BLE001 - the page shows what went wrong
            self.send_json({"error": f"{type(e).__name__}: {e}"}, 500)

    def api_get(self, what: str):
        atlas = self.atlas
        if what == "current":
            return self.send_json(atlas.current(), cache="no-cache")
        if what == "places":
            return self.send_file(atlas.places_file(), "public, max-age=604800, immutable")
        if what == "checks":
            return self.send_json({"regions": atlas.summary()})
        m = re.fullmatch(r"(region|checks)/" + REGION_NAME.pattern, what)
        if not m:
            return self.send_json({"error": "not found"}, 404)
        rx, rz = int(m[2]), int(m[3])
        if m[1] == "checks":
            return self.send_json(atlas.load_checks(rx, rz))
        path = atlas.region_file(rx, rz)
        if not path.is_file():
            return self.send_json({"error": f"build has no region {rx}_{rz}"}, 404)
        # The region file as it is: the browser unzips it. The page asks with the build's id in the query, so a cached
        # copy is never another build's.
        self.send_bytes(path.read_bytes(), "application/json; charset=utf-8", "public, max-age=604800, immutable", "gzip")

    # --- POST ------------------------------------------------------------------------------------------------------------

    def do_POST(self):
        if not self.host_ok():
            return
        m = re.fullmatch(r"/api/checks/" + REGION_NAME.pattern, urlparse(self.path).path)
        length = int(self.headers.get("Content-Length") or 0)
        if not 0 < length <= MAX_BODY:
            if MAX_BODY < length <= 16 * MAX_BODY:
                self.rfile.read(length)   # read and dropped, so the connection can carry the next request
            else:
                self.close_connection = True
            return self.send_json({"error": "a record is up to 64 KB"}, 413)
        raw = self.rfile.read(length)
        if not m:
            return self.send_json({"error": "not found"}, 404)
        # JSON only (a cross-site form can't send it without the browser asking first, which this server never allows),
        # and only from this page.
        if not (self.headers.get("Content-Type") or "").startswith("application/json"):
            return self.send_json({"error": "send JSON"}, 415)
        origin = self.headers.get("Origin")
        if origin and urlparse(origin).netloc != self.headers.get("Host"):
            return self.send_json({"error": "not from this page"}, 403)
        try:
            body = json.loads(raw.decode("utf-8"))
            record = self.atlas.save(int(m[1]), int(m[2]), body)
            self.log_message("saved %s_%s building %s", m[1], m[2], body.get("id"))
            self.send_json({"record": record})
        except Conflict as c:
            self.send_json({"error": "This building was saved meanwhile (by " + ((c.current or {}).get("by") or "?") + ")",
                            "current": c.current}, 409)
        except (ValueError, UnicodeDecodeError) as e:
            self.send_json({"error": str(e)}, 400)
        except Exception as e:  # noqa: BLE001
            self.send_json({"error": f"{type(e).__name__}: {e}"}, 500)

    # --- Plumbing --------------------------------------------------------------------------------------------------------

    def host_ok(self) -> bool:
        """On the loopback only this machine's names: a page elsewhere can't reach it through a name of its own."""
        if not self.loopback:
            return True
        host = (self.headers.get("Host") or "").rsplit(":", 1)[0].strip("[]").lower()
        if host in ("127.0.0.1", "localhost", "::1"):
            return True
        self.close_connection = True
        self.send_json({"error": "unknown host"}, 403)
        return False

    def send_json(self, data, status: int = 200, cache: str = "no-store"):
        self.send_bytes(json.dumps(data, ensure_ascii=False, separators=(",", ":")).encode("utf-8"),
                        "application/json; charset=utf-8", cache, status=status)

    def send_file(self, path: Path, cache: str):
        self.send_bytes(path.read_bytes(), TYPES.get(path.suffix, "application/octet-stream"), cache)

    def send_bytes(self, data: bytes, kind: str, cache: str, encoding: str | None = None, status: int = 200):
        self.send_response(status)
        self.send_header("Content-Type", kind)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", cache)
        self.send_header("X-Content-Type-Options", "nosniff")
        if encoding:
            self.send_header("Content-Encoding", encoding)
        self.end_headers()
        self.wfile.write(data)

    def log_message(self, format, *args):  # noqa: A002 - BaseHTTPRequestHandler's name
        if format.startswith("saved") or (args and str(args[1] if len(args) > 1 else "").startswith(("4", "5"))):
            sys.stderr.write(f"{self.log_date_time_string()} {format % args}\n")


def make_server(atlas: Atlas, host: str = "127.0.0.1", port: int = 8765) -> ThreadingHTTPServer:
    handler = type("AtlasHandler", (Handler,), {"atlas": atlas, "loopback": host in ("127.0.0.1", "localhost", "::1")})
    server = ThreadingHTTPServer((host, port), handler)
    server.daemon_threads = True
    return server


def main(argv: list | None = None) -> int:
    parser = argparse.ArgumentParser(description="The Kentucky Atlas in a browser: check the world store's buildings "
                                                 "over Kentucky's aerial photographs")
    parser.add_argument("--store", type=Path, default=default_store(), help="the world store (default: CLONEZ_WORLD or the NAS)")
    parser.add_argument("--host", default="127.0.0.1", help="the address to listen on (0.0.0.0 shares it on the LAN)")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--no-browser", action="store_true", help="don't open the page in the browser")
    args = parser.parse_args(argv)
    atlas = Atlas(args.store)
    try:
        cur = atlas.current()
    except (OSError, ValueError) as e:
        print(f"Can't read the world store at {args.store}: {e}", file=sys.stderr)
        return 1
    server = make_server(atlas, args.host, args.port)
    shown = "127.0.0.1" if args.host in ("0.0.0.0", "::") else args.host
    url = f"http://{shown}:{server.server_port}/"
    print(f"Kentucky Atlas: build {cur['build']} from {args.store}, checks in {atlas.checks_dir}\n  {url}  (Ctrl+C stops it)")
    if not args.no_browser:
        threading.Timer(0.6, webbrowser.open, (url,)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
