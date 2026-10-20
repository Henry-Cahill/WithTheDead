#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["osmium>=4.0"]
# ///
"""CLONE-Z world builder: turns OpenStreetMap's extract of Kentucky into the game's map data in the world store.

The world is laid out the way survival games lay theirs out: 256 m cells, grouped in regions of 32 x 32 cells
(8,192 m), on Kentucky's official flat grid (EPSG:3088, Kentucky Single Zone, metres east and north). Each region file
holds the streets, lines (rivers, streams, railways), buildings, land-use areas and points of interest of that region,
in decimetres from its south-west corner. Streets, lines and areas are clipped to the region (plus a small margin, so
neighbours overlap); a building belongs to the region its middle is in.

  uv run --python 3.12 worldbuild.py extract [--id ID] [--part K/N]   write a build's regions (part K of N: several
                                                                       machines share the work, each its own regions)
  uv run --python 3.12 worldbuild.py publish ID                       check a build's parts, write its manifest and make
                                                                       it the build the game and the servers load
  uv run --python 3.12 worldbuild.py status                           the builds in the store and what each machine does

The map data is © OpenStreetMap contributors, available under the Open Database License (ODbL); the game credits it.
"""
from __future__ import annotations

import argparse
import datetime as dt
import gzip
import hashlib
import json
import math
import os
import socket
import sys
import time
from pathlib import Path

STORE = Path(os.environ.get("CLONEZ_WORLD") or (r"\\192.168.1.170\CLONE-Z" if os.name == "nt" else "/mnt/clonez"))
WORLD = STORE / "world"
SOURCE = WORLD / "sources" / "osm" / "kentucky-latest.osm.pbf"
BUILDS = WORLD / "builds"
CURRENT = WORLD / "current.json"
MACHINES = STORE / "machines"

FORMAT = 1
CELL = 256  # metres
REGION_CELLS = 32
REGION = CELL * REGION_CELLS  # 8,192 m
MARGIN = 16.0  # metres that clipped streets, lines and areas reach past their region's edge
ATTRIBUTION = "Map data © OpenStreetMap contributors (ODbL)"


# --- Kentucky's flat grid -------------------------------------------------------------------------------------

class KentuckySingleZone:
    """EPSG:3088 (NAD83 / Kentucky Single Zone): a Lambert conformal conic on GRS80, in metres. OpenStreetMap's
    WGS84 longitudes and latitudes go in as NAD83 (the two differ by about a metre)."""

    A = 6378137.0
    F = 1 / 298.257222101

    def __init__(self) -> None:
        self.e = math.sqrt(2 * self.F - self.F ** 2)
        lat1, lat2, lat0 = (math.radians(d) for d in (37 + 5 / 60, 38 + 40 / 60, 36 + 20 / 60))
        self.lon0 = math.radians(-(85 + 45 / 60))
        self.x0, self.y0 = 1_500_000.0, 1_000_000.0
        m1, m2 = self._m(lat1), self._m(lat2)
        t1, t2, t0 = self._t(lat1), self._t(lat2), self._t(lat0)
        self.n = (math.log(m1) - math.log(m2)) / (math.log(t1) - math.log(t2))
        self.af = self.A * m1 / (self.n * t1 ** self.n)
        self.rho0 = self.af * t0 ** self.n

    def _m(self, lat: float) -> float:
        return math.cos(lat) / math.sqrt(1 - (self.e * math.sin(lat)) ** 2)

    def _t(self, lat: float) -> float:
        es = self.e * math.sin(lat)
        return math.tan(math.pi / 4 - lat / 2) / ((1 - es) / (1 + es)) ** (self.e / 2)

    def forward(self, lon: float, lat: float) -> tuple[float, float]:
        """Metres east and north on the grid for a longitude and latitude in degrees."""
        rho = self.af * self._t(math.radians(lat)) ** self.n
        theta = self.n * (math.radians(lon) - self.lon0)
        return self.x0 + rho * math.sin(theta), self.y0 + self.rho0 - rho * math.cos(theta)


# --- Geometry -------------------------------------------------------------------------------------------------

def clip_segment(ax: float, ay: float, bx: float, by: float, x0: float, y0: float, x1: float, y1: float):
    """The part of segment a-b inside the rectangle as (t0, t1) along it (Liang-Barsky), or None."""
    dx, dy = bx - ax, by - ay
    t0, t1 = 0.0, 1.0
    for p, q in ((-dx, ax - x0), (dx, x1 - ax), (-dy, ay - y0), (dy, y1 - ay)):
        if p == 0:
            if q < 0:
                return None
            continue
        r = q / p
        if p < 0:
            if r > t1:
                return None
            t0 = max(t0, r)
        else:
            if r < t0:
                return None
            t1 = min(t1, r)
    return t0, t1


def clip_polyline(pts: list, x0: float, y0: float, x1: float, y1: float) -> list:
    """The pieces of a polyline inside a rectangle, each a list of at least two points."""
    parts, cur = [], []
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        t = clip_segment(ax, ay, bx, by, x0, y0, x1, y1)
        if t is None:
            if len(cur) > 1:
                parts.append(cur)
            cur = []
            continue
        start = (ax, ay) if t[0] == 0 else (ax + t[0] * (bx - ax), ay + t[0] * (by - ay))
        end = (bx, by) if t[1] == 1 else (ax + t[1] * (bx - ax), ay + t[1] * (by - ay))
        if t[0] > 0 or not cur:  # entered the rectangle on this segment
            if len(cur) > 1:
                parts.append(cur)
            cur = [start]
        cur.append(end)
        if t[1] < 1:  # and left it again
            parts.append(cur)
            cur = []
    if len(cur) > 1:
        parts.append(cur)
    return parts


def clip_polygon(pts: list, x0: float, y0: float, x1: float, y1: float) -> list:
    """A polygon (no repeated last point) cut to a rectangle (Sutherland-Hodgman); may come back empty."""
    def cut(poly, inside, cross):
        out = []
        for i, cur in enumerate(poly):
            prev = poly[i - 1]
            if inside(cur):
                if not inside(prev):
                    out.append(cross(prev, cur))
                out.append(cur)
            elif inside(prev):
                out.append(cross(prev, cur))
        return out

    def at_x(x):
        return lambda a, b: (x, a[1] + (b[1] - a[1]) * (x - a[0]) / (b[0] - a[0]))

    def at_y(y):
        return lambda a, b: (a[0] + (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]), y)

    poly = list(pts)
    for inside, cross in ((lambda p: p[0] >= x0, at_x(x0)), (lambda p: p[0] <= x1, at_x(x1)),
                          (lambda p: p[1] >= y0, at_y(y0)), (lambda p: p[1] <= y1, at_y(y1))):
        if not poly:
            break
        poly = cut(poly, inside, cross)
    return poly


def polygon_area(pts: list) -> float:
    """Signed area (positive when the points run anticlockwise)."""
    return 0.5 * sum(ax * by - bx * ay for (ax, ay), (bx, by) in zip(pts, pts[1:] + pts[:1]))


def regions_touched(pts: list, margin: float = 0.0) -> list:
    """Every region the bounding box of `pts` (grown by `margin`) overlaps."""
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    rx0, rx1 = int((min(xs) - margin) // REGION), int((max(xs) + margin) // REGION)
    rz0, rz1 = int((min(ys) - margin) // REGION), int((max(ys) + margin) // REGION)
    return [(rx, rz) for rx in range(rx0, rx1 + 1) for rz in range(rz0, rz1 + 1)]


def local(pts: list, rx: int, rz: int) -> list:
    """Points as a flat list of decimetres from the region's south-west corner: [x0, z0, x1, z1, ...]."""
    ox, oz = rx * REGION, rz * REGION
    out = []
    for x, y in pts:
        out.append(round((x - ox) * 10))
        out.append(round((y - oz) * 10))
    return out


def drop_repeats(pts: list) -> list:
    """Points with consecutive duplicates (closer than 5 cm) removed."""
    out = []
    for p in pts:
        if not out or abs(p[0] - out[-1][0]) > 0.05 or abs(p[1] - out[-1][1]) > 0.05:
            out.append(p)
    return out


# --- What OpenStreetMap's tags mean in the game -------------------------------------------------------------

STREET_WIDTH = {  # metres of carriageway, for streets without a width or lanes tag
    "motorway": 11, "trunk": 10.5, "primary": 10, "secondary": 9, "tertiary": 8, "unclassified": 6.5,
    "residential": 7, "living_street": 6, "road": 6.5, "service": 4.5, "track": 3, "pedestrian": 5, "busway": 7,
    "motorway_link": 6, "trunk_link": 6, "primary_link": 6, "secondary_link": 6, "tertiary_link": 5.5,
    "footway": 2, "path": 1.5, "cycleway": 2.5, "bridleway": 2, "steps": 2,
}
LINE_WIDTH = {"river": 20, "canal": 10, "stream": 3, "ditch": 1.5, "drain": 1.5, "rail": 3, "abandoned": 3,
              "disused": 3}
PLACE_KINDS = {"city", "town", "village", "hamlet", "suburb", "neighbourhood", "locality"}
BUILDING_KINDS = {
    "house": "house", "detached": "house", "semidetached_house": "house", "terrace": "house", "bungalow": "house",
    "farm": "house", "static_caravan": "trailer", "manufactured": "trailer", "apartments": "apartments",
    "residential": "apartments", "dormitory": "apartments", "hotel": "hotel", "commercial": "commercial",
    "office": "office", "retail": "retail", "supermarket": "retail", "kiosk": "retail", "industrial": "industrial",
    "warehouse": "warehouse", "factory": "industrial", "manufacture": "industrial", "church": "church",
    "chapel": "church", "cathedral": "church", "mosque": "church", "synagogue": "church", "temple": "church",
    "religious": "church", "school": "school", "university": "school", "college": "school", "kindergarten": "school",
    "hospital": "hospital", "public": "civic", "civic": "civic", "government": "civic", "fire_station": "civic",
    "train_station": "civic", "garage": "garage", "garages": "garage", "carport": "garage", "shed": "shed",
    "hut": "shed", "cabin": "house", "barn": "barn", "farm_auxiliary": "barn", "cowshed": "barn", "stable": "barn",
    "silo": "silo", "greenhouse": "greenhouse", "service": "shed", "roof": "canopy", "ruins": "ruin",
}
DEFAULT_LEVELS = {"house": 1, "trailer": 1, "apartments": 3, "hotel": 3, "commercial": 1, "office": 2, "retail": 1,
                  "industrial": 1, "warehouse": 1, "church": 1, "school": 2, "hospital": 3, "civic": 2, "garage": 1,
                  "shed": 1, "barn": 1, "silo": 1, "greenhouse": 1, "canopy": 1, "ruin": 1}
AREA_KINDS = [  # checked in order; the first match wins
    ("natural", "water", "water"), ("water", None, "water"), ("waterway", "riverbank", "water"),
    ("landuse", "reservoir", "water"), ("landuse", "basin", "water"), ("amenity", "parking", "parking"),
    ("amenity", "grave_yard", "cemetery"), ("landuse", "cemetery", "cemetery"), ("landuse", "forest", "forest"),
    ("natural", "wood", "forest"), ("landuse", "farmland", "farmland"), ("landuse", "farmyard", "farmyard"),
    ("landuse", "orchard", "orchard"), ("landuse", "meadow", "meadow"), ("landuse", "grass", "grass"),
    ("natural", "grassland", "grass"), ("natural", "scrub", "scrub"), ("natural", "heath", "scrub"),
    ("natural", "wetland", "wetland"), ("natural", "bare_rock", "rock"), ("natural", "beach", "sand"),
    ("landuse", "quarry", "quarry"), ("landuse", "residential", "residential"), ("landuse", "commercial", "commercial"),
    ("landuse", "retail", "retail"), ("landuse", "industrial", "industrial"), ("landuse", "railway", "industrial"),
    ("landuse", "recreation_ground", "park"), ("leisure", "park", "park"), ("leisure", "garden", "park"),
    ("leisure", "playground", "playground"), ("leisure", "pitch", "pitch"), ("leisure", "sports_centre", "pitch"),
    ("leisure", "stadium", "pitch"), ("leisure", "track", "pitch"), ("leisure", "golf_course", "golf"),
]
POI_AMENITIES = {"fuel", "police", "fire_station", "hospital", "clinic", "doctors", "dentist", "pharmacy", "school",
                 "place_of_worship", "bank", "post_office", "restaurant", "fast_food", "cafe", "bar", "pub", "library",
                 "townhall", "courthouse", "prison", "community_centre", "veterinary", "car_repair", "car_wash"}
KEYS = ("building", "highway", "landuse", "natural", "leisure", "amenity", "shop", "waterway", "railway", "place",
        "water")


def number(value: str | None) -> float:
    """The leading number in a tag value ("2", "2.5", "12 m"), or 0."""
    try:
        return float((value or "").replace(",", ".").split()[0].split(";")[0]) if value else 0.0
    except (ValueError, IndexError):
        return 0.0


def poi_kind(tags) -> str:
    amenity = tags.get("amenity")
    if amenity in POI_AMENITIES:
        return amenity
    shop = tags.get("shop")
    return "shop:" + shop if shop else ""


def area_kind(tags) -> str:
    for key, value, kind in AREA_KINDS:
        got = tags.get(key)
        if got is not None and (value is None or got == value):
            return kind
    return ""


def building_kind(tags, area: float) -> str:
    kind = BUILDING_KINDS.get(tags.get("building", ""))
    if kind:
        return kind
    if tags.get("amenity") in ("school", "college", "university"):
        return "school"
    if tags.get("amenity") == "place_of_worship":
        return "church"
    if tags.get("amenity") or tags.get("shop") or tags.get("office"):
        return "commercial"
    return "house" if area < 250 else "commercial" if area < 2500 else "warehouse"


def street_record(tags, kind: str) -> dict:
    lanes = int(number(tags.get("lanes")))
    width = number(tags.get("width")) or (lanes * 3.3 + 1 if lanes else STREET_WIDTH.get(kind, 6))
    return {"kind": kind, "name": tags.get("name") or tags.get("ref") or "", "width": round(width, 1),
            "lanes": lanes, "oneway": tags.get("oneway") in ("yes", "1", "-1"),
            "bridge": tags.get("bridge", "no") != "no", "tunnel": tags.get("tunnel", "no") != "no",
            "layer": int(number(tags.get("layer")))}


# --- Building a world ---------------------------------------------------------------------------------------

def now() -> str:
    return dt.datetime.now().astimezone().isoformat(timespec="seconds")


def write_json(path: Path, data, compress: bool = False) -> None:
    """Writes under a temporary name, then renames: nobody reading the store ever sees half a file."""
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(f".{path.name}.{socket.gethostname()}.{os.getpid()}.tmp")
    text = json.dumps(data, separators=(",", ":"), ensure_ascii=False) if compress else \
        json.dumps(data, indent=1, ensure_ascii=False)
    if compress:
        with gzip.open(tmp, "wt", encoding="utf-8", compresslevel=6) as f:
            f.write(text)
    else:
        tmp.write_text(text + "\n", encoding="utf-8")
    os.replace(tmp, path)


def check_in(doing: str, **extra) -> None:
    """Tells the other machines what this one is doing (machines/<name>.json in the store)."""
    host = socket.gethostname()
    write_json(MACHINES / f"{host}.json", {"machine": host, "doing": doing, "updated": now(), **extra})


def file_md5(path: Path) -> str:
    h = hashlib.md5()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def parse_part(text: str | None) -> tuple[int, int]:
    if not text:
        return 0, 1
    k, n = (int(v) for v in text.split("/"))
    if not 0 <= k < n:
        raise SystemExit(f"--part {text}: K must be 0 to N-1")
    return k, n


def owns(rx: int, rz: int, k: int, n: int) -> bool:
    """Whether part k of n writes region (rx, rz): neighbours go to different parts, so the work spreads evenly."""
    return (rx * 31 + rz * 17) % n == k


def extract(args) -> int:
    import osmium

    k, n = parse_part(args.part)
    source = Path(args.source)
    build = BUILDS / args.id
    started, host = now(), socket.gethostname()
    check_in(f"extracting build {args.id}, part {k}/{n}", started=started)
    proj = KentuckySingleZone()
    regions: dict = {}
    places: list = []
    stats = {"streets": 0, "lines": 0, "buildings": 0, "areas": 0, "relation_areas": 0, "pois": 0, "places": 0,
             "bad_geometry": 0}

    def region(rx: int, rz: int) -> dict:
        r = regions.get((rx, rz))
        if r is None:
            r = regions[(rx, rz)] = {"format": FORMAT, "rx": rx, "rz": rz, "size": REGION, "streets": [], "lines": [],
                                     "buildings": [], "areas": [], "pois": []}
        return r

    def project(nodes) -> list:
        return drop_repeats([proj.forward(nd.lon, nd.lat) for nd in nodes])

    def add_line(list_name: str, record: dict, pts: list) -> None:
        for rx, rz in regions_touched(pts, MARGIN):
            if owns(rx, rz, k, n):
                box = (rx * REGION - MARGIN, rz * REGION - MARGIN, (rx + 1) * REGION + MARGIN, (rz + 1) * REGION + MARGIN)
                for piece in clip_polyline(pts, *box):
                    region(rx, rz)[list_name].append(dict(record, pts=local(piece, rx, rz)))
                    stats[list_name] += 1

    t0 = time.time()
    processor = osmium.FileProcessor(str(source)).with_locations().with_areas().with_filter(osmium.filter.KeyFilter(*KEYS))
    for obj in processor:
        try:
            tags = obj.tags
            if obj.is_node():
                if tags.get("place") in PLACE_KINDS and tags.get("name"):
                    if k == 0:
                        x, y = proj.forward(obj.location.lon, obj.location.lat)
                        places.append({"id": obj.id, "kind": tags["place"], "name": tags["name"],
                                       "population": int(number(tags.get("population"))), "x": round(x, 1),
                                       "z": round(y, 1)})
                        stats["places"] += 1
                    continue
                kind = poi_kind(tags)
                if kind:
                    x, y = proj.forward(obj.location.lon, obj.location.lat)
                    rx, rz = int(x // REGION), int(y // REGION)
                    if owns(rx, rz, k, n):
                        region(rx, rz)["pois"].append({"id": obj.id, "kind": kind, "name": tags.get("name", ""),
                                                       "x": round((x - rx * REGION) * 10),
                                                       "z": round((y - rz * REGION) * 10)})
                        stats["pois"] += 1
            elif obj.is_way():
                highway, waterway, railway = tags.get("highway"), tags.get("waterway"), tags.get("railway")
                if highway in STREET_WIDTH and tags.get("area") != "yes":
                    pts = project(obj.nodes)
                    if len(pts) > 1:
                        add_line("streets", dict(street_record(tags, highway), id=obj.id), pts)
                elif waterway in LINE_WIDTH or railway in ("rail", "abandoned", "disused"):
                    pts = project(obj.nodes)
                    if len(pts) > 1:
                        kind = waterway if waterway in LINE_WIDTH else railway
                        width = number(tags.get("width")) or LINE_WIDTH[kind]
                        add_line("lines", {"id": obj.id, "kind": kind, "name": tags.get("name", ""),
                                           "width": round(width, 1)}, pts)
            elif obj.is_area():
                is_building = tags.get("building", "no") != "no"
                kind = "" if is_building else area_kind(tags)
                if not is_building and not kind:
                    continue
                for outer in obj.outer_rings():
                    pts = project(outer)
                    if len(pts) > 1 and pts[0] == pts[-1]:
                        pts.pop()
                    if len(pts) < 3:
                        continue
                    area = abs(polygon_area(pts))
                    if is_building:
                        if area < 4:
                            continue  # a sliver, not a building
                        cx = sum(p[0] for p in pts) / len(pts)
                        cy = sum(p[1] for p in pts) / len(pts)
                        rx, rz = int(cx // REGION), int(cy // REGION)
                        if not owns(rx, rz, k, n):
                            continue
                        bkind = building_kind(tags, area)
                        levels = int(number(tags.get("building:levels"))) or DEFAULT_LEVELS.get(bkind, 1)
                        address = " ".join(v for v in (tags.get("addr:housenumber"), tags.get("addr:street")) if v)
                        region(rx, rz)["buildings"].append({
                            "id": obj.id, "kind": bkind, "name": tags.get("name", ""), "address": address,
                            "levels": max(1, min(levels, 40)), "height": round(number(tags.get("height")), 1),
                            "use": tags.get("amenity") or ("shop:" + tags["shop"] if tags.get("shop") else ""),
                            "pts": local(pts, rx, rz)})
                        stats["buildings"] += 1
                    else:
                        if not obj.from_way():
                            stats["relation_areas"] += 1
                        for rx, rz in regions_touched(pts, MARGIN):
                            if not owns(rx, rz, k, n):
                                continue
                            box = (rx * REGION - MARGIN, rz * REGION - MARGIN, (rx + 1) * REGION + MARGIN,
                                   (rz + 1) * REGION + MARGIN)
                            cut = clip_polygon(pts, *box)
                            if len(cut) >= 3 and abs(polygon_area(cut)) >= 1:
                                region(rx, rz)["areas"].append({"id": obj.id, "kind": kind, "name": tags.get("name", ""),
                                                                "pts": local(cut, rx, rz)})
                                stats["areas"] += 1
        except osmium.InvalidLocationError:
            stats["bad_geometry"] += 1
    read_minutes = (time.time() - t0) / 60
    check_in(f"writing build {args.id}, part {k}/{n}: {len(regions)} regions", started=started)

    listing = []
    for (rx, rz), data in sorted(regions.items()):
        write_json(build / "regions" / f"{rx}_{rz}.json.gz", data, compress=True)
        listing.append({"rx": rx, "rz": rz, **{key: len(data[key]) for key in
                                               ("streets", "lines", "buildings", "areas", "pois")}})
    if k == 0:
        write_json(build / "places.json", {"format": FORMAT, "places": sorted(places, key=lambda p: -p["population"])})
    write_json(build / "parts" / f"{k}-of-{n}.json", {
        "part": k, "of": n, "machine": host, "started": started, "finished": now(),
        "read_minutes": round(read_minutes, 1), "source": source.name, "source_md5": file_md5(source),
        "tool_md5": file_md5(Path(__file__)), "stats": stats, "regions": listing})
    check_in("idle", last=f"extracted build {args.id}, part {k}/{n}: {len(regions)} regions", stats=stats)
    print(f"part {k}/{n} of build {args.id}: {len(regions)} regions in {(time.time() - t0) / 60:.1f} min; {stats}")
    return 0


def publish(args) -> int:
    build = BUILDS / args.id
    parts = [json.loads(p.read_text(encoding="utf-8")) for p in sorted((build / "parts").glob("*-of-*.json"))]
    if not parts:
        raise SystemExit(f"build {args.id} has no parts in {build / 'parts'}")
    n = parts[0]["of"]
    have = {p["part"] for p in parts if p["of"] == n}
    if have != set(range(n)):
        raise SystemExit(f"build {args.id} is missing parts {sorted(set(range(n)) - have)} of {n}")
    if len({p["source_md5"] for p in parts}) != 1 or len({p["tool_md5"] for p in parts}) != 1:
        raise SystemExit(f"build {args.id}'s parts were made from different extracts or tool versions")
    regions = sorted((r for p in parts if p["of"] == n for r in p["regions"]), key=lambda r: (r["rx"], r["rz"]))
    missing = [r for r in regions if not (build / "regions" / f"{r['rx']}_{r['rz']}.json.gz").exists()]
    if missing:
        raise SystemExit(f"build {args.id} lists {len(missing)} region files that aren't there")
    totals = {key: sum(r[key] for r in regions) for key in ("streets", "lines", "buildings", "areas", "pois")}
    manifest = {
        "format": FORMAT, "id": args.id, "published": now(), "attribution": ATTRIBUTION,
        "projection": {"epsg": 3088, "name": "NAD83 / Kentucky Single Zone", "units": "m"},
        "cell": CELL, "region": REGION, "units": "decimetres from each region's south-west corner",
        "source": {"file": parts[0]["source"], "md5": parts[0]["source_md5"]}, "tool_md5": parts[0]["tool_md5"],
        "parts": [{"part": p["part"], "machine": p["machine"], "finished": p["finished"]} for p in parts],
        "totals": totals, "regions": regions,
    }
    write_json(build / "manifest.json", manifest)
    write_json(CURRENT, {"build": args.id, "published": manifest["published"], "by": socket.gethostname(),
                         "attribution": ATTRIBUTION})
    print(f"published {args.id}: {len(regions)} regions, {totals}")
    return 0


def status(args) -> int:
    current = json.loads(CURRENT.read_text(encoding="utf-8")).get("build") if CURRENT.exists() else None
    print(f"World store: {STORE}  (current build: {current or 'none'})")
    for build in sorted(BUILDS.glob("*")) if BUILDS.exists() else []:
        parts = sorted(p.stem for p in (build / "parts").glob("*-of-*.json"))
        state = "published" if (build / "manifest.json").exists() else "in progress"
        print(f"  {build.name}: {state}; parts {', '.join(parts) or 'none yet'}")
    for machine in sorted(MACHINES.glob("*.json")) if MACHINES.exists() else []:
        m = json.loads(machine.read_text(encoding="utf-8"))
        print(f"  {m.get('machine')}: {m.get('doing')} (as of {m.get('updated')})")
    return 0


def main(argv: list | None = None) -> int:
    parser = argparse.ArgumentParser(description="CLONE-Z world builder (Kentucky from OpenStreetMap)")
    sub = parser.add_subparsers(dest="cmd", required=True)
    ex = sub.add_parser("extract", help="write a build's regions from the OpenStreetMap extract")
    ex.add_argument("--id", default=dt.datetime.now().strftime("ky-%Y%m%d-%H%M"), help="the build's name")
    ex.add_argument("--part", help="K/N: write only part K (0..N-1) of N, so N machines share the work")
    ex.add_argument("--source", default=str(SOURCE), help="the .osm.pbf extract")
    pub = sub.add_parser("publish", help="check a build's parts, write its manifest and make it current")
    pub.add_argument("id")
    sub.add_parser("status", help="the builds in the store and what each machine is doing")
    args = parser.parse_args(argv)
    return {"extract": extract, "publish": publish, "status": status}[args.cmd](args)


if __name__ == "__main__":
    sys.exit(main())
