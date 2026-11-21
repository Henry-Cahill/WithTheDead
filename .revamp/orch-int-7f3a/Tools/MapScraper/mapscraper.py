#!/usr/bin/env python3
"""CLONE-Z map scraper for zomboidmap.com (Project Zomboid Build 42).

Mirrors what the site serves for a region: every overlay data file, the room interiors of each
cell, and the rendered tiles of every floor at every zoom level. It then builds region exports
(GeoJSON/JSON/CSV, POIs, decoded loot tables, heatmap crops), stitched mosaics and top-down maps.
Runs resume where they stopped, and they stay polite: requests are rate-limited, connections are
kept alive, and failures back off.

  python mapscraper.py plan                          # counts, size and time estimate
  python mapscraper.py run                           # full Louisville rip (resumes)
  python mapscraper.py run --region all --levels 3-11
  python mapscraper.py run --floors=-17--1           # values starting with '-' need the '=' form
  python mapscraper.py status
  python mapscraper.py export | stitch --level 5 | topdown --ppsq 8

MapScraper.ps1 runs `run` in the background and wraps the other commands.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import logging
import os
import random
import shutil
import sys
import threading
import traceback
from concurrent.futures import ThreadPoolExecutor

import exports
import images
from fetch import (DiskIndex, Downloader, FetchError, HttpClient, PathLog, Progress, RunLock, fmt_bytes,
                   fmt_duration, keep_awake, write_atomic)
from zmap import (BASE_URL, DATA_FILES, PRESETS, MapInfo, Manifest, parse_floors, parse_levels, region_tiles,
                  resolve_region, tile_groups, tile_path)

HERE = os.path.dirname(os.path.abspath(__file__))
log = logging.getLogger("mapscraper")


class Paths:
    def __init__(self, out=None):
        self.out = os.path.abspath(out or os.environ.get("CLONEZ_MAP_OUT") or os.path.join(HERE, "out"))
        self.raw = os.path.join(self.out, "raw")
        self.state = os.path.join(self.out, "state")
        self.regions = os.path.join(self.out, "regions")
        self.global_dir = os.path.join(self.out, "global")
        self.log = os.path.join(self.out, "run.log")
        self.progress = os.path.join(self.state, "progress.json")
        self.stop = os.path.join(self.state, "STOP")
        self.pid = os.path.join(self.state, "run.pid")
        self.lock = os.path.join(self.state, "run.lock")
        self.missing = os.path.join(self.state, "missing.txt")
        self.failed = os.path.join(self.state, "failed.txt")
        for folder in (self.out, self.raw, self.state):
            os.makedirs(folder, exist_ok=True)

    def region_dir(self, region):
        return os.path.join(self.regions, region.name)


def setup_logging(paths, to_file=True):
    if sys.stdout is not None:
        try:
            sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    log.handlers.clear()
    log.setLevel(logging.INFO)
    log.propagate = False
    fmt = logging.Formatter("%(asctime)s  %(message)s", "%Y-%m-%d %H:%M:%S")
    handlers = [logging.FileHandler(paths.log, encoding="utf-8")] if to_file else []
    if sys.stdout is not None:  # pythonw (background runs) has no console
        handlers.append(logging.StreamHandler(sys.stdout))
    for handler in handlers:
        handler.setFormatter(fmt)
        log.addHandler(handler)


def load_meta(paths, client=None):
    """map_info.json and manifest.json, refreshed from the site when a client is given."""
    loaded = {}
    for rel in ("map_info.json", "manifest.json"):
        local = os.path.join(paths.raw, rel)
        if client is not None:
            try:
                status, _, body = client.fetch(rel)
                if status != 200:
                    raise FetchError(f"{rel}: HTTP {status}")
                write_atomic(local, body)
            except FetchError as exc:
                if not os.path.exists(local):
                    raise SystemExit(f"Cannot download {rel} ({exc}). Check the internet connection.")
                log.warning("Could not refresh %s (%s); using the copy from the last run.", rel, exc)
        if not os.path.exists(local):
            raise SystemExit(f"{rel} has not been downloaded yet. Run 'plan' or 'run' first (needs internet).")
        with open(local, encoding="utf-8") as f:
            loaded[rel] = json.load(f)
    return MapInfo(loaded["map_info.json"]), Manifest(loaded["manifest.json"])


def note_manifest_change(paths):
    with open(os.path.join(paths.raw, "manifest.json"), "rb") as f:
        digest = hashlib.sha1(f.read()).hexdigest()
    marker = os.path.join(paths.state, "manifest.sha1")
    previous = None
    if os.path.exists(marker):
        with open(marker, encoding="ascii") as f:
            previous = f.read().strip()
    if previous and previous != digest:
        log.warning("The site's map changed since the last run (new game build?). Tiles already on disk are the "
                    "older version; delete out/raw/layer* and out/raw/composite to fetch fresh ones.")
    with open(marker, "w", encoding="ascii") as f:
        f.write(digest)


def region_of(args, info):
    return resolve_region(info, args.region, args.bbox, args.cells, args.name)


def room_paths(info, region):
    cx0, cy0, cx1, cy1 = region.cell_range(info.cell_size)
    return [f"overlays/rooms/{cx}_{cy}.json" for cx in range(cx0, cx1 + 1) for cy in range(cy0, cy1 + 1)]


def tile_items(info, manifest, region, groups):
    for group in groups:
        for tx, ty in region_tiles(info, manifest, region, group.layer, group.level):
            yield group.label, tile_path(group.layer, group.level, tx, ty, info.format)


# ---------------------------------------------------------------- plan

def cmd_plan(args):
    paths = Paths(args.out)
    setup_logging(paths, to_file=False)
    client = HttpClient(BASE_URL, args.rate, threading.Event())
    info, manifest = load_meta(paths, client)
    region = region_of(args, info)
    levels, floors = parse_levels(args.levels, info), parse_floors(args.floors, info)
    tile_region = region.padded(args.pad)
    disk = DiskIndex(paths.raw)

    def size_of(rel):
        local = os.path.join(paths.raw, *rel.split("/"))
        if os.path.exists(local):
            return os.path.getsize(local)
        if args.samples <= 0:
            return None
        try:
            status, headers, _ = client.fetch(rel, method="HEAD")
            return int(headers.get("Content-Length") or 0) if status == 200 else None
        except FetchError:
            return None

    cx0, cy0, cx1, cy1 = region.cell_range(info.cell_size)
    print(f"Region '{region.name}': squares x {region.x0}-{region.x1}, y {region.y0}-{region.y1} "
          f"(cells {cx0}-{cx1} x {cy0}-{cy1})")
    print(f"Map: {info.tile_size} px {info.format} tiles, L0 (native) to L{info.levels - 1}, floors "
          f"{info.min_layer}..{info.max_layer}, composite tiles from L{manifest.composite_base}")
    print(f"Selected: levels {_ranges(levels)}, floors {_ranges(floors)}"
          + ("" if not args.no_composite else ", no composite"))
    print()

    with ThreadPoolExecutor(max_workers=6) as pool:
        data_sizes = list(pool.map(size_of, DATA_FILES))
    data_have = sum(disk.has(rel) for rel in DATA_FILES)
    data_todo = sum(s or 0 for rel, s in zip(DATA_FILES, data_sizes) if not disk.has(rel))
    rooms = room_paths(info, region)
    rooms_have = sum(disk.has(rel) for rel in rooms)
    room_sizes = [os.path.getsize(os.path.join(paths.raw, *r.split("/"))) for r in rooms if disk.has(r)]
    room_avg = sum(room_sizes) / len(room_sizes) if room_sizes else 60 * 1024
    print(f"{'Data files':14} {len(DATA_FILES):>7} files   have {data_have:>7}   to download ~{fmt_bytes(data_todo)}")
    print(f"{'Room cells':14} {len(rooms):>7} cells   have {rooms_have:>7}   to download ~{fmt_bytes(room_avg * (len(rooms) - rooms_have))}"
          "  (empty cells answer 404)")

    groups = tile_groups(info, manifest, tile_region, levels, floors, not args.no_composite)
    rng = random.Random(1)
    stats = {}
    for group in groups:
        st = stats.setdefault(group.level, {"floors": 0, "composite": 0, "have": 0, "seen": 0, "pool": []})
        st["composite" if group.layer == "composite" else "floors"] += group.count
        for tx, ty in region_tiles(info, manifest, tile_region, group.layer, group.level):
            rel = tile_path(group.layer, group.level, tx, ty, info.format)
            st["have"] += disk.has(rel)
            st["seen"] += 1
            if len(st["pool"]) < max(args.samples, 0):
                st["pool"].append(rel)
            elif args.samples > 0:
                j = rng.randrange(st["seen"])
                if j < args.samples:
                    st["pool"][j] = rel

    print()
    print(f"  {'level':6}{'floor tiles':>13}{'composite':>11}{'have':>10}{'avg size':>11}{'to download':>14}")
    total = have = 0
    todo_bytes = 0.0
    with ThreadPoolExecutor(max_workers=6) as pool:
        for level in sorted(stats, reverse=True):
            st = stats[level]
            count = st["floors"] + st["composite"]
            sizes = [s for s in pool.map(size_of, st["pool"]) if s is not None]
            avg = sum(sizes) / len(sizes) if sizes else None
            est = avg * (count - st["have"]) if avg is not None else None
            todo_bytes += est or 0
            total += count
            have += st["have"]
            print(f"  L{level:<5}{st['floors']:>13,}{st['composite']:>11,}{st['have']:>10,}"
                  f"{fmt_bytes(avg) if avg is not None else '?':>11}{fmt_bytes(est) if est is not None else '?':>14}")
    print(f"  {'total':6}{total:>24,}{have:>10,}{'':>11}{fmt_bytes(todo_bytes):>14}")
    requests = (len(DATA_FILES) - data_have) + (len(rooms) - rooms_have) + (total - have)
    print()
    print(f"Requests left: {requests:,}. At {args.rate:g} requests/s that is about {fmt_duration(requests / args.rate)}"
          " (raise -Rate to go faster, lower it to be gentler on the site).")
    print(f"Free space where the output goes ({paths.out}): {fmt_bytes(shutil.disk_usage(paths.out).free)}")
    plan = {"region": region.to_json(info.cell_size), "levels": levels, "floors": floors, "tiles": total,
            "tiles_have": have, "tile_bytes_to_download_estimate": int(todo_bytes), "requests_left": requests,
            "per_level": {f"L{k}": {kk: vv for kk, vv in v.items() if kk != "pool"} for k, v in stats.items()}}
    with open(os.path.join(paths.state, "plan.json"), "w", encoding="utf-8") as f:
        json.dump(plan, f, indent=1)
    return 0


def _ranges(values):
    values = sorted(values)
    if not values:
        return "none"
    parts, start, prev = [], values[0], values[0]
    for v in values[1:] + [None]:
        if v is not None and v == prev + 1:
            prev = v
            continue
        parts.append(str(start) if start == prev else f"{start}..{prev}")
        if v is not None:
            start = prev = v
    return ", ".join(parts)


# ---------------------------------------------------------------- run

def build_images(args, paths, info, manifest, region, levels, floors, progress, stop_event):
    try:
        import PIL  # noqa: F401
    except ImportError:
        log.warning("Pillow is not installed, so mosaics and top-down maps were skipped (python -m pip install pillow).")
        return
    out = paths.region_dir(region)
    level = (images.auto_mosaic_level(info, region, floors, levels) if args.mosaic_level == "auto"
             else int(args.mosaic_level))
    progress.note("mosaic", f"L{level}")
    try:
        images.stitch(paths.raw, info, manifest, region, level, floors, os.path.join(out, "mosaic"), pad=args.pad,
                      composite=not args.no_composite, should_stop=stop_event.is_set)
    except (Exception, SystemExit) as exc:
        log.warning("Mosaic skipped (%s); rebuild it later with 'stitch'.", exc)
    source = images.topdown_level(info, args.topdown_ppsq)
    if 0 in floors and source in levels and not stop_event.is_set():
        progress.note("topdown", f"{args.topdown_ppsq} px per square")
        try:
            images.topdown(paths.raw, info, region, args.topdown_ppsq, [0], os.path.join(out, "topdown"),
                           should_stop=stop_event.is_set)
        except (Exception, SystemExit) as exc:
            log.warning("Top-down maps skipped (%s); rebuild them later with 'topdown'.", exc)


def cmd_run(args):
    paths = Paths(args.out)
    lock = RunLock(paths.lock)
    if not lock.acquire():
        setup_logging(paths)
        log.warning("Another run is already using %s; not starting a second one (see 'status').", paths.out)
        return 3
    setup_logging(paths)
    if os.path.exists(paths.stop):
        os.remove(paths.stop)
    with open(paths.pid, "w", encoding="ascii") as f:
        f.write(str(os.getpid()))
    stop_event = threading.Event()
    meta = {"pid": os.getpid(), "args": {k: v for k, v in vars(args).items() if k != "func"}}
    progress = Progress(paths.state, paths.out, stop_event, args.min_free_gb, meta)
    progress.start()
    keep_awake(True)
    client = HttpClient(BASE_URL, args.rate, stop_event)
    log.info("=== run started (pid %d): region=%s levels=%s floors=%s workers=%d rate=%g/s out=%s ===",
             os.getpid(), args.region if not (args.bbox or args.cells) else (args.bbox or args.cells),
             args.levels, args.floors, args.workers, args.rate, paths.out)
    state, message, code = "done", "", 0
    downloader = None
    try:
        info, manifest = load_meta(paths, client)
        note_manifest_change(paths)
        region = region_of(args, info)
        levels, floors = parse_levels(args.levels, info), parse_floors(args.floors, info)
        meta["region"] = region.to_json(info.cell_size)
        downloader = Downloader(client, paths.raw, args.workers, progress, stop_event, PathLog(paths.missing),
                                args.max_new)
        ok = True
        if not args.no_data:
            ok = downloader.run("data", DATA_FILES, len(DATA_FILES), force=args.refresh, retry_missing=args.refresh)
            if ok:
                rooms = room_paths(info, region)
                ok = downloader.run("rooms", rooms, len(rooms), force=args.refresh,
                                    retry_missing=args.refresh or args.retry_missing)
            if ok:
                progress.note("export", region.name)
                try:
                    exports.export_region(paths.raw, region, paths.region_dir(region), paths.global_dir, info)
                except Exception as exc:  # e.g. a CSV open in Excel; never cost a night of tile downloads
                    log.warning("Region exports failed (%s: %s). The rip carries on; rebuild them later with "
                                "'export' (close exported files that are open in other programs first).",
                                type(exc).__name__, exc)
        if ok and not args.no_tiles:
            tile_region = region.padded(args.pad)
            log.info("Working out which tiles cover '%s' ...", region.name)
            groups = tile_groups(info, manifest, tile_region, levels, floors, not args.no_composite)
            total = sum(g.count for g in groups)
            ok = downloader.run("tiles", tile_items(info, manifest, tile_region, groups), total,
                                retry_missing=args.retry_missing)
            if ok and not args.no_build:
                build_images(args, paths, info, manifest, region, levels, floors, progress, stop_event)
        if stop_event.is_set():
            state, message, code = "stopped", progress.stop_reason or "stopped", 2
    except KeyboardInterrupt:
        state, message, code = "stopped", "Ctrl+C", 2
    except SystemExit as exc:
        state, message, code = "error", str(exc), 1
        log.error("%s", exc)
    except Exception as exc:
        state, message, code = "error", f"{type(exc).__name__}: {exc}", 1
        log.error("Run failed:\n%s", traceback.format_exc())
    finally:
        failures = downloader.failures if downloader else []
        if failures:
            with open(paths.failed, "w", encoding="utf-8") as f:
                f.write("\n".join(failures) + "\n")
            note = f"{len(failures)} downloads failed (listed in state/failed.txt); start again to retry them"
            message = f"{message}; {note}" if message else note
        elif state == "done" and os.path.exists(paths.failed):
            os.remove(paths.failed)
        keep_awake(False)
        progress.finish(state, message)
        for path in (paths.pid, paths.stop):
            try:
                os.remove(path)
            except OSError:
                pass
        lock.release()
    log.info("=== run %s%s ===", state, f": {message}" if message else "")
    return code


# ---------------------------------------------------------------- status / offline builders

def cmd_status(args):
    paths = Paths(args.out)
    running = RunLock(paths.lock).held_elsewhere()
    if not os.path.exists(paths.progress):
        print("A run is starting up..." if running else "No run has been started yet.")
        return 0
    with open(paths.progress, encoding="utf-8") as f:
        snap = json.load(f)
    state = snap.get("state", "?")
    if state == "running" and not running:
        state = "interrupted (the process ended unexpectedly; start again to resume)"
    print(f"State:    {state}" + (f" - {snap['stop_reason']}" if snap.get("stop_reason") else ""))
    if snap.get("message") and snap.get("message") != snap.get("stop_reason"):
        print(f"Note:     {snap['message']}")
    region = snap.get("region") or {}
    print(f"Region:   {region.get('name', '?')}   levels {snap['args'].get('levels')}   floors {snap['args'].get('floors')}")
    print(f"Started:  {snap.get('started_at')}   last update: {snap.get('updated_at')}")
    for name, c in snap.get("phases", {}).items():
        if "total" not in c:
            busy = name == snap.get("phase") and state == "running"
            print(f"  {name:8} {'working: ' + snap.get('detail', '') if busy else 'done'}")
            continue
        pct = 100.0 * c["done"] / c["total"] if c["total"] else 100.0
        print(f"  {name:8} {c['done']:>9,} / {c['total']:<9,} {pct:5.1f}%   new {c['downloaded']:,} "
              f"({fmt_bytes(c['bytes'])})   had {c['skipped']:,}   404 {c['missing']:,}   failed {c['failed']:,}")
    if state == "running":
        print(f"Now:      {snap.get('phase')} {snap.get('detail', '')} | {snap.get('rate_per_s', 0):.1f} files/s, "
              f"{fmt_bytes(snap.get('rate_bytes_s', 0))}/s | ETA for this phase {fmt_duration(snap.get('eta_s'))}")
    print(f"Disk:     {fmt_bytes(shutil.disk_usage(paths.out).free)} free for {paths.out}")
    return 0


def cmd_export(args):
    paths = Paths(args.out)
    setup_logging(paths)
    info, _ = load_meta(paths)
    region = region_of(args, info)
    exports.export_region(paths.raw, region, paths.region_dir(region), paths.global_dir, info)
    return 0


def cmd_stitch(args):
    paths = Paths(args.out)
    setup_logging(paths)
    info, manifest = load_meta(paths)
    region = region_of(args, info)
    floors = parse_floors(args.floors, info)
    level = (images.auto_mosaic_level(info, region, floors, range(info.levels)) if args.level == "auto"
             else int(args.level))
    images.stitch(paths.raw, info, manifest, region, level, floors, os.path.join(paths.region_dir(region), "mosaic"),
                  pad=args.pad, max_mpx=args.max_mpx, composite=not args.no_composite)
    return 0


def cmd_topdown(args):
    paths = Paths(args.out)
    setup_logging(paths)
    info, _ = load_meta(paths)
    region = region_of(args, info)
    images.topdown(paths.raw, info, region, args.ppsq, parse_floors(args.floors, info),
                   os.path.join(paths.region_dir(region), "topdown"), overview_ppsq=args.overview_ppsq)
    return 0


# ---------------------------------------------------------------- CLI

def build_parser():
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument("--out", help="output folder (default: out/ next to this script, or $CLONEZ_MAP_OUT)")

    area = argparse.ArgumentParser(add_help=False)
    area.add_argument("--region", default="louisville", help=f"named region: {', '.join(PRESETS)} or all (default louisville)")
    area.add_argument("--bbox", type=int, nargs=4, metavar=("X0", "Y0", "X1", "Y1"),
                      help="custom region in world squares (X1/Y1 exclusive)")
    area.add_argument("--cells", type=int, nargs=4, metavar=("CX0", "CY0", "CX1", "CY1"),
                      help="custom region in 256-square cells (inclusive)")
    area.add_argument("--name", help="folder name for a custom region")
    area.add_argument("--pad", type=int, default=8, help="extra squares around the region when picking tiles (default 8)")

    net = argparse.ArgumentParser(add_help=False)
    net.add_argument("--rate", type=float, default=8.0, help="maximum requests per second (default 8)")

    tiles = argparse.ArgumentParser(add_help=False)
    tiles.add_argument("--levels", default="all", help="zoom levels, e.g. all, 3-11, 0,1 (L0 = native detail)")
    tiles.add_argument("--floors", default="all", help="floors, e.g. all, 0, 0-5, ground, above, basements, =-17--1")
    tiles.add_argument("--no-composite", action="store_true", help="skip the flattened all-floors tiles")

    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    p = sub.add_parser("plan", parents=[common, area, net, tiles], help="show what a run would download")
    p.add_argument("--samples", type=int, default=6, help="tiles per level measured for the size estimate (0 = none)")
    p.set_defaults(func=cmd_plan)

    p = sub.add_parser("run", parents=[common, area, net, tiles],
                       help="download everything (resumes), then build exports and images")
    p.add_argument("--workers", type=int, default=6, help="parallel downloads (default 6)")
    p.add_argument("--no-data", action="store_true", help="skip data files, rooms and exports")
    p.add_argument("--no-tiles", action="store_true", help="data files, rooms and exports only")
    p.add_argument("--no-build", action="store_true", help="skip the mosaics and top-down maps at the end")
    p.add_argument("--refresh", action="store_true", help="download data and room files again even if present")
    p.add_argument("--retry-missing", action="store_true", help="retry files that answered 404 on earlier runs")
    p.add_argument("--max-new", type=int, help="stop after this many new downloads (for testing)")
    p.add_argument("--min-free-gb", type=float, default=10.0, help="stop when free disk space falls below this (default 10)")
    p.add_argument("--mosaic-level", default="auto", help="level for the stitched mosaics (default auto: the most "
                                                          "detailed one under about 48 megapixels)")
    p.add_argument("--topdown-ppsq", type=int, default=4, help="pixels per square for the top-down map (default 4)")
    p.set_defaults(func=cmd_run)

    p = sub.add_parser("status", parents=[common], help="progress of the current or last run")
    p.set_defaults(func=cmd_status)

    p = sub.add_parser("export", parents=[common, area], help="rebuild the region exports from downloaded data")
    p.set_defaults(func=cmd_export)

    p = sub.add_parser("stitch", parents=[common, area], help="stitch downloaded tiles into mosaics")
    p.add_argument("--level", default="auto", help="zoom level to stitch (default auto)")
    p.add_argument("--floors", default="all", help="floors to stitch (default all)")
    p.add_argument("--no-composite", action="store_true", help="skip composite.png")
    p.add_argument("--max-mpx", type=float, default=300, help="refuse images larger than this many megapixels")
    p.set_defaults(func=cmd_stitch)

    p = sub.add_parser("topdown", parents=[common, area], help="un-skew downloaded tiles into top-down maps")
    p.add_argument("--ppsq", type=int, default=4, help="pixels per square, 1-32 (default 4; uses L4 tiles)")
    p.add_argument("--floors", default="0", help="floors to map (default 0, the ground floor)")
    p.add_argument("--overview-ppsq", type=float, default=1.0, help="pixels per square for overview.png (default 1)")
    p.set_defaults(func=cmd_topdown)
    return parser


def main(argv=None):
    args = build_parser().parse_args(argv)
    try:
        return args.func(args) or 0
    except ValueError as exc:  # unreadable --levels/--floors/--region
        print(f"error: {exc}")
        return 2
    except KeyboardInterrupt:
        return 130


if __name__ == "__main__":
    sys.exit(main())
