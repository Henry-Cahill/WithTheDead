#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["osmium>=4.0"]
# ///
"""CLONE-Z world builder: turns OpenStreetMap's extract of Kentucky into the game's map data in the world store.

The world is laid out the way survival games lay theirs out: 256 m cells, grouped in regions of 32 x 32 cells
(8,192 m), on Kentucky's official flat grid (EPSG:3088, Kentucky Single Zone, metres east and north). Each region file
holds the streets, lines (rivers, streams, railways), buildings, land-use areas, points of interest, barriers (fences,
walls, hedges, guard rails, tree rows), power lines and point features (trees, street lamps, signs, hydrants, ...) of
that region, in decimetres from its south-west corner. Streets, lines, barriers, power lines and areas are clipped to
the region (plus a small margin, so neighbours overlap); a building belongs to the region its middle is in, a point to
the region it's in.

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
import re
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

FORMAT = 2  # 2 only adds fields, arrays and area kinds to 1 (see the README); readers take both
CELL = 256  # metres
REGION_CELLS = 32
REGION = CELL * REGION_CELLS  # 8,192 m
MARGIN = 16.0  # metres that clipped streets, lines, barriers, power lines and areas reach past their region's edge
ARRAYS = ("streets", "lines", "buildings", "areas", "pois", "barriers", "power", "nodes")  # a region's lists
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

    def true_north(self, lon: float) -> float:
        """The grid bearing of true north at a longitude, in degrees clockwise from the grid's north: the meridians
        lean in towards the cone's apex, by up to about 2.3 degrees at Kentucky's east and west ends."""
        return -math.degrees(self.n * (math.radians(lon) - self.lon0))


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


def middle(pts: list) -> tuple[float, float]:
    """The average of a ring's points: where a building or an outlined landmark is said to be."""
    return sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts)


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
    # Format 2's kinds come after all of format 1's, so tags that made a format-1 kind still make the same one.
    ("amenity", "school", "school"), ("amenity", "college", "school"), ("amenity", "university", "school"),
    ("landuse", "education", "school"), ("amenity", "hospital", "hospital"),
    ("amenity", "place_of_worship", "churchyard"), ("landuse", "religious", "churchyard"), ("amenity", "fuel", "fuel"),
    ("highway", "pedestrian", "plaza"), ("place", "square", "plaza"), ("aeroway", "aerodrome", "airfield"),
    ("landuse", "military", "military"), ("landuse", "brownfield", "lot"), ("landuse", "greenfield", "lot"),
    ("landuse", "construction", "lot"),
]
AREA_RULE_KEYS = tuple(dict.fromkeys(key for key, _, _ in AREA_KINDS))
POI_AMENITIES = {"fuel", "police", "fire_station", "hospital", "clinic", "doctors", "dentist", "pharmacy", "school",
                 "place_of_worship", "bank", "post_office", "restaurant", "fast_food", "cafe", "bar", "pub", "library",
                 "townhall", "courthouse", "prison", "community_centre", "veterinary", "car_repair", "car_wash"}
SURFACES = {  # a street's surface, as the game's five
    "asphalt": "asphalt", "paved": "asphalt", "chipseal": "asphalt", "concrete": "concrete", "paving_stones": "brick",
    "sett": "brick", "cobblestone": "brick", "unhewn_cobblestone": "brick", "bricks": "brick", "brick": "brick",
    "unpaved": "gravel", "compacted": "gravel", "fine_gravel": "gravel", "gravel": "gravel", "pebblestone": "gravel",
    "rock": "gravel", "dirt": "dirt", "earth": "dirt", "ground": "dirt", "grass": "dirt", "mud": "dirt", "sand": "dirt",
    "clay": "dirt", "soil": "dirt", "woodchips": "dirt",
}
SIDES = {"yes": True, "no": False, "none": False, "separate": False, "lane": False}  # sidewalk:left/right/both
ROOF_SHAPES = {  # roof:shape, with "-" and " " read as "_"
    "flat": "flat", "gabled": "gabled", "hipped": "hipped", "pyramidal": "pyramidal", "skillion": "skillion",
    "gambrel": "gambrel", "mansard": "mansard", "half_hipped": "half-hipped", "dome": "dome", "round": "round",
    "gable": "gabled", "saltbox": "gabled", "quadruple_saltbox": "gabled", "crosspitched": "gabled",
    "gabled_height_moved": "gabled", "round_gabled": "gabled", "hip": "hipped", "side_hipped": "hipped",
    "side_half_hipped": "half-hipped", "hipped_and_gabled": "half-hipped", "pyramid": "pyramidal",
    "cone": "pyramidal", "lean_to": "skillion", "shed": "skillion", "sawtooth": "skillion", "onion": "dome",
    "barrel": "round", "arched": "round",
}
MATERIALS = {  # building:material, with "-" and " " read as "_"
    "brick": "brick", "bricks": "brick", "masonry": "brick", "wood": "wood", "wooden": "wood", "timber": "wood",
    "timber_framing": "wood", "log": "wood", "logs": "wood", "vinyl": "vinyl", "plastic": "vinyl", "stone": "stone",
    "limestone": "stone", "sandstone": "stone", "granite": "stone", "marble": "stone", "concrete": "concrete",
    "reinforced_concrete": "concrete", "cement_block": "concrete", "concrete_block": "concrete",
    "cinder_block": "concrete", "metal": "metal", "steel": "metal", "aluminium": "metal", "aluminum": "metal",
    "metal_plates": "metal", "metal_sheet": "metal", "tin": "metal", "glass": "glass", "stucco": "stucco",
    "plaster": "stucco", "render": "stucco", "adobe": "stucco",
}
CSS_COLOURS = dict(pair.split("=") for pair in """
aliceblue=f0f8ff antiquewhite=faebd7 aqua=00ffff aquamarine=7fffd4 azure=f0ffff beige=f5f5dc bisque=ffe4c4
black=000000 blanchedalmond=ffebcd blue=0000ff blueviolet=8a2be2 brown=a52a2a burlywood=deb887 cadetblue=5f9ea0
chartreuse=7fff00 chocolate=d2691e coral=ff7f50 cornflowerblue=6495ed cornsilk=fff8dc crimson=dc143c cyan=00ffff
darkblue=00008b darkcyan=008b8b darkgoldenrod=b8860b darkgray=a9a9a9 darkgreen=006400 darkgrey=a9a9a9
darkkhaki=bdb76b darkmagenta=8b008b darkolivegreen=556b2f darkorange=ff8c00 darkorchid=9932cc darkred=8b0000
darksalmon=e9967a darkseagreen=8fbc8f darkslateblue=483d8b darkslategray=2f4f4f darkslategrey=2f4f4f
darkturquoise=00ced1 darkviolet=9400d3 deeppink=ff1493 deepskyblue=00bfff dimgray=696969 dimgrey=696969
dodgerblue=1e90ff firebrick=b22222 floralwhite=fffaf0 forestgreen=228b22 fuchsia=ff00ff gainsboro=dcdcdc
ghostwhite=f8f8ff gold=ffd700 goldenrod=daa520 gray=808080 green=008000 greenyellow=adff2f grey=808080
honeydew=f0fff0 hotpink=ff69b4 indianred=cd5c5c indigo=4b0082 ivory=fffff0 khaki=f0e68c lavender=e6e6fa
lavenderblush=fff0f5 lawngreen=7cfc00 lemonchiffon=fffacd lightblue=add8e6 lightcoral=f08080 lightcyan=e0ffff
lightgoldenrodyellow=fafad2 lightgray=d3d3d3 lightgreen=90ee90 lightgrey=d3d3d3 lightpink=ffb6c1
lightsalmon=ffa07a lightseagreen=20b2aa lightskyblue=87cefa lightslategray=778899 lightslategrey=778899
lightsteelblue=b0c4de lightyellow=ffffe0 lime=00ff00 limegreen=32cd32 linen=faf0e6 magenta=ff00ff maroon=800000
mediumaquamarine=66cdaa mediumblue=0000cd mediumorchid=ba55d3 mediumpurple=9370db mediumseagreen=3cb371
mediumslateblue=7b68ee mediumspringgreen=00fa9a mediumturquoise=48d1cc mediumvioletred=c71585
midnightblue=191970 mintcream=f5fffa mistyrose=ffe4e1 moccasin=ffe4b5 navajowhite=ffdead navy=000080
oldlace=fdf5e6 olive=808000 olivedrab=6b8e23 orange=ffa500 orangered=ff4500 orchid=da70d6 palegoldenrod=eee8aa
palegreen=98fb98 paleturquoise=afeeee palevioletred=db7093 papayawhip=ffefd5 peachpuff=ffdab9 peru=cd853f
pink=ffc0cb plum=dda0dd powderblue=b0e0e6 purple=800080 rebeccapurple=663399 red=ff0000 rosybrown=bc8f8f
royalblue=4169e1 saddlebrown=8b4513 salmon=fa8072 sandybrown=f4a460 seagreen=2e8b57 seashell=fff5ee
sienna=a0522d silver=c0c0c0 skyblue=87ceeb slateblue=6a5acd slategray=708090 slategrey=708090 snow=fffafa
springgreen=00ff7f steelblue=4682b4 tan=d2b48c teal=008080 thistle=d8bfd8 tomato=ff6347 turquoise=40e0d0
violet=ee82ee wheat=f5deb3 white=ffffff whitesmoke=f5f5f5 yellow=ffff00 yellowgreen=9acd32
""".split())
BARRIER_KINDS = {"fence": "fence", "wall": "wall", "city_wall": "wall", "flood_wall": "wall", "hedge": "hedge",
                 "guard_rail": "guard_rail", "retaining_wall": "retaining_wall"}
BARRIER_WIDTH = {"fence": 0.1, "wall": 0.4, "hedge": 1.2, "guard_rail": 0.3, "retaining_wall": 0.5, "tree_row": 6}
POWER_KINDS = {"line": "line", "minor_line": "minor_line"}
NODE_KINDS = {  # (key, value) of a point feature: its kind
    ("highway", "street_lamp"): "street_lamp", ("highway", "traffic_signals"): "traffic_signals",
    ("highway", "stop"): "stop", ("highway", "give_way"): "give_way", ("highway", "crossing"): "crossing",
    ("highway", "bus_stop"): "bus_stop", ("railway", "level_crossing"): "level_crossing",
    ("emergency", "fire_hydrant"): "hydrant", ("amenity", "fire_hydrant"): "hydrant", ("amenity", "bench"): "bench",
    ("amenity", "waste_basket"): "waste_basket", ("amenity", "post_box"): "post_box",
    ("amenity", "telephone"): "telephone", ("amenity", "vending_machine"): "vending",
    ("man_made", "flagpole"): "flagpole", ("man_made", "water_tower"): "water_tower", ("man_made", "mast"): "mast",
    ("historic", "memorial"): "memorial", ("historic", "monument"): "memorial", ("barrier", "gate"): "gate",
    ("barrier", "swing_gate"): "gate", ("barrier", "sliding_gate"): "gate", ("barrier", "wicket_gate"): "gate",
    ("barrier", "hampshire_gate"): "gate", ("barrier", "bollard"): "bollard", ("natural", "tree"): "tree",
}
NODE_KIND_KEYS = ("highway", "railway", "emergency", "amenity", "man_made", "historic", "barrier", "natural")
AREA_FEATURES = {"water_tower", "mast", "flagpole", "memorial"}  # also mapped as outlines: a point at the middle
SIGNS = {"stop", "give_way", "traffic_signals"}  # may be tagged as facing along their street (forward/backward)
COMPASS = {name: i * 22.5 for i, name in enumerate(("N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW",
                                                    "WSW", "W", "WNW", "NW", "NNW"))}
COMPASS.update(NORTH=0, EAST=90, SOUTH=180, WEST=270)
# What the extract reads: ways as streets and lines, areas (closed ways and multipolygons) as buildings, land and
# outlined landmarks, and nodes as places, points of interest and point features. Anything else never reaches Python.
WAY_KEYS = ("highway", "waterway", "railway", "barrier", "natural", "power")
AREA_KEYS = ("building", "man_made", "historic") + AREA_RULE_KEYS
NODE_KEYS = ("place", "shop") + NODE_KIND_KEYS


def number(value: str | None) -> float:
    """The leading number in a tag value ("2", "2.5", "12 m"), or 0."""
    try:
        n = float((value or "").replace(",", ".").split()[0].split(";")[0]) if value else 0.0
    except (ValueError, IndexError):
        return 0.0
    return n if math.isfinite(n) else 0.0


def poi_kind(tags) -> str:
    amenity = tags.get("amenity")
    if amenity in POI_AMENITIES:
        return amenity
    shop = tags.get("shop")
    return "shop:" + shop if shop else ""


def area_kind(tags, from_way: bool = True) -> str:
    """The game's kind of land for an area's tags, or "" (the first rule in AREA_KINDS that matches wins)."""
    got = {key: tags.get(key) for key in AREA_RULE_KEYS}
    for key, value, kind in AREA_KINDS:
        v = got[key]
        if v is not None and (value is None or v == value):
            if key == "highway" and from_way and tags.get("area") != "yes":
                continue  # a closed street, not a square
            return kind
    return ""


def first_value(value: str | None) -> str:
    """The first of a tag's ;-separated values, trimmed ("" for none)."""
    return (value or "").split(";")[0].strip()


def surface(value: str | None) -> str:
    """A street's surface as asphalt, concrete, brick, gravel or dirt, or "" when it's untagged or something else."""
    if not value:
        return ""
    for part in re.split(r"[;,/]", value.lower()):
        part = part.strip(" _")
        kind = SURFACES.get(part) or SURFACES.get(part.split(":")[0])  # concrete:plates, paving_stones:30
        if kind:
            return kind
    return ""


def sidewalk(tags) -> str:
    """Which sides of a street have a sidewalk: both, left, right, no, or "" when unknown. A sidewalk mapped as a
    footway of its own (separate) counts as none, since the footway is built from its own way."""
    value = first_value(tags.get("sidewalk")).lower()
    if value in ("both", "left", "right"):
        return value
    if value == "yes":
        return "both"
    if value in SIDES:
        return "no"
    both = SIDES.get(first_value(tags.get("sidewalk:both")).lower())
    left = SIDES.get(first_value(tags.get("sidewalk:left")).lower(), both)
    right = SIDES.get(first_value(tags.get("sidewalk:right")).lower(), both)
    if left and right:
        return "both"
    if left or right:
        return "left" if left else "right"
    return "no" if left is False and right is False else ""


def lit(value: str | None) -> int:
    """Whether a street is lit at night: 1 yes, 2 no, 0 unknown."""
    v = first_value(value).lower()
    if v in ("no", "disused", "off"):
        return 2
    if v in ("yes", "24/7", "automatic", "dusk-dawn", "sunset-sunrise", "limited", "interval") or ":" in v:
        return 1
    return 0


def roof_shape(value: str | None) -> str:
    return ROOF_SHAPES.get(first_value(value).lower().replace("-", "_").replace(" ", "_"), "")


def material(value: str | None) -> str:
    v = first_value(value).lower().replace("-", "_").replace(" ", "_")
    if v in MATERIALS:
        return MATERIALS[v]
    return next((MATERIALS[w] for w in v.split("_") if w in MATERIALS), "")  # brick_vinyl: brick


def colour(value: str | None) -> str:
    """A colour tag as "#rrggbb" (from "#rrggbb", "#rgb" or a CSS colour name), or "" if it isn't one."""
    v = first_value(value).lower().replace(" ", "").replace("_", "").replace("-", "")
    if v.startswith("#"):
        digits = v[1:] if len(v) != 4 else "".join(c + c for c in v[1:])
        return "#" + digits if len(digits) == 6 and all(c in "0123456789abcdef" for c in digits) else ""
    named = CSS_COLOURS.get(v)
    return "#" + named if named else ""


YEAR = re.compile(r"(?<!\d)(1\d{3}|20\d{2})(?!\d)")
CENTURY = re.compile(r"\bC(\d{1,2})\b", re.IGNORECASE)


def year(value: str | None) -> int:
    """The year in a start_date ("1885", "1885-06-12", "~1885", "1880s", "before 1900"; "C19" is 1850), or 0."""
    m = YEAR.search(value or "")
    if m:
        return int(m.group(1))
    m = CENTURY.search(value or "")
    return int(m.group(1)) * 100 - 50 if m else 0


def compass(deg: float) -> int:
    """Degrees as a whole compass bearing, 0 to 359."""
    return int(math.floor(deg + 0.5)) % 360


def _bearing(text: str) -> float | None:
    text = text.strip().rstrip("°")
    if text in COMPASS:
        return COMPASS[text]
    try:
        deg = float(text)
    except ValueError:
        return None
    return deg if math.isfinite(deg) else None


def direction(value: str | None, north: float = 0.0) -> int:
    """Degrees clockwise from north that a point feature faces, from its direction tag ("45", "NE"; a range like
    "45-135" faces its middle), or -1 when it has none. The tag's degrees are from true north; `north` is the grid
    bearing of true north there (KentuckySingleZone.true_north). Signs tagged forward/backward along their street are
    -1 here; the extract turns those into bearings when it reads the street (see facing)."""
    v = first_value(value).upper()
    if not v:
        return -1
    deg = _bearing(v)
    if deg is None:
        cut = v.find("-", 1)
        a, b = (_bearing(v[:cut]), _bearing(v[cut + 1:])) if cut > 0 else (None, None)
        if a is None or b is None or (b - a) % 360 == 0:
            return -1
        deg = a + (b - a) % 360 / 2
    return compass(deg + north)


def facing(ax: float, ay: float, bx: float, by: float, forward: bool) -> int:
    """The bearing a sign faces when it's for traffic going from a to b along its street (forward) or from b to a:
    signs face the traffic they're for."""
    return compass(math.degrees(math.atan2(bx - ax, by - ay)) + (180 if forward else 0))


def sign_travel(tags, kind: str) -> bool | None:
    """For a sign on a street: True if it's for traffic going the way's way (forward), False if against, else None."""
    if kind not in SIGNS:
        return None
    v = first_value(tags.get("direction") or
                    (tags.get("traffic_signals:direction") if kind == "traffic_signals" else None)).lower()
    return True if v == "forward" else False if v == "backward" else None


def node_kind(tags) -> str:
    for key in NODE_KIND_KEYS:
        value = tags.get(key)
        if value is not None:
            kind = NODE_KINDS.get((key, value))
            if kind:
                return kind
    if tags.get("man_made") == "tower" and tags.get("tower:type") == "communication":
        return "mast"
    return ""


def barrier_kind(tags) -> str:
    kind = BARRIER_KINDS.get(tags.get("barrier"))
    return kind or ("tree_row" if tags.get("natural") == "tree_row" else "")


def structure(tags) -> dict:
    """Whether a way is on a bridge or in a tunnel (a culvert too), and its layer."""
    return {"bridge": tags.get("bridge", "no") != "no", "tunnel": tags.get("tunnel", "no") != "no",
            "layer": int(number(tags.get("layer")))}


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
            "lanes": lanes, "oneway": tags.get("oneway") in ("yes", "1", "-1"), **structure(tags),
            "surface": surface(tags.get("surface")), "sidewalk": sidewalk(tags), "lit": lit(tags.get("lit")),
            "route": tags.get("ref") or ""}


def line_record(tags, kind: str) -> dict:
    width = number(tags.get("width")) or LINE_WIDTH[kind]
    return {"kind": kind, "name": tags.get("name", ""), "width": round(width, 1), **structure(tags)}


def barrier_record(tags, kind: str) -> dict:
    """A fence, wall, hedge, guard rail, retaining wall or tree row; its width is its thickness in metres."""
    width = number(tags.get("width"))
    return {"kind": kind, "name": tags.get("name", ""),
            "width": round(min(width, 20.0) if width >= 0.05 else BARRIER_WIDTH[kind], 2)}


def power_record(tags, kind: str) -> dict:
    """A power line. Its width is the spread of its wires, which grows with the voltage: distribution lines on wooden
    poles, transmission lines on wooden H-frames or steel towers."""
    if kind == "minor_line":
        width = 2.4
    else:
        kv = max((number(v) for v in (tags.get("voltage") or "").split(";")), default=0) / 1000
        width = 9 if kv <= 0 else 4 if kv < 100 else 9 if kv < 200 else 16 if kv < 400 else 24
    return {"kind": kind, "name": tags.get("name", ""), "width": width}


def building_record(tags, area: float) -> dict:
    kind = building_kind(tags, area)
    levels = int(number(tags.get("building:levels"))) or DEFAULT_LEVELS.get(kind, 1)
    address = " ".join(v for v in (tags.get("addr:housenumber"), tags.get("addr:street")) if v)
    return {"kind": kind, "name": tags.get("name", ""), "address": address, "levels": max(1, min(levels, 40)),
            "height": round(number(tags.get("height")), 1),
            "use": tags.get("amenity") or ("shop:" + tags["shop"] if tags.get("shop") else ""),
            "roof": roof_shape(tags.get("roof:shape")), "material": material(tags.get("building:material")),
            "colour": colour(tags.get("building:colour")), "roofColour": colour(tags.get("roof:colour")),
            "roofLevels": max(0, min(int(number(tags.get("roof:levels"))), 10)),
            "year": year(tags.get("start_date") or tags.get("construction_date"))}


def node_record(node_id: int, tags, kind: str, x: int, z: int, north: float = 0.0) -> dict:
    """A point feature at (x, z) decimetres in its region; `north` as for direction."""
    return {"id": node_id, "kind": kind, "name": tags.get("name", ""), "x": x, "z": z,
            "dir": direction(tags.get("direction"), north)}


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


def region_entry(data: dict) -> dict:
    """A region's line in a part's listing and the manifest: where it is and how many of each thing it holds."""
    return {"rx": data["rx"], "rz": data["rz"], **{key: len(data.get(key) or ()) for key in ARRAYS}}


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
    signs: dict = {}  # node id: (record, forward) for signs facing along their street, until the street is read
    stats = {"streets": 0, "lines": 0, "buildings": 0, "areas": 0, "relation_areas": 0, "pois": 0, "places": 0,
             "barriers": 0, "power": 0, "nodes": 0, "signs_faced": 0, "bad_geometry": 0}

    def region(rx: int, rz: int) -> dict:
        r = regions.get((rx, rz))
        if r is None:
            r = regions[(rx, rz)] = {"format": FORMAT, "rx": rx, "rz": rz, "size": REGION,
                                     **{key: [] for key in ARRAYS}}
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

    def add_node(record: dict, x: float, y: float) -> None:
        """A point feature at grid metres (x, y), if this part writes its region."""
        rx, rz = int(x // REGION), int(y // REGION)
        if owns(rx, rz, k, n):
            record["x"], record["z"] = round((x - rx * REGION) * 10), round((y - rz * REGION) * 10)
            region(rx, rz)["nodes"].append(record)
            stats["nodes"] += 1

    def face_signs(nodes) -> None:
        """Signs on this street tagged forward/backward get the bearing they face."""
        last = len(nodes) - 1
        for i, nd in enumerate(nodes):
            sign = signs.pop(nd.ref, None)
            if sign is not None and last > 0:
                a, b = nodes[max(i - 1, 0)], nodes[min(i + 1, last)]
                (ax, ay), (bx, by) = proj.forward(a.lon, a.lat), proj.forward(b.lon, b.lat)
                if ax != bx or ay != by:
                    sign[0]["dir"] = facing(ax, ay, bx, by, sign[1])
                    stats["signs_faced"] += 1

    t0 = time.time()
    # Locations and areas see every object; the filters only decide what reaches the loop, in C++, so the millions of
    # untagged nodes and the ways nothing here reads cost nothing in Python. Multipolygon relations are assembled only
    # if they have one of AREA_KEYS (closed ways always are).
    processor = (osmium.FileProcessor(str(source)).with_locations()
                 .with_areas(osmium.filter.KeyFilter(*AREA_KEYS))
                 .with_filter(osmium.filter.EntityFilter(osmium.osm.NODE | osmium.osm.WAY | osmium.osm.AREA))
                 .with_filter(osmium.filter.KeyFilter(*NODE_KEYS).enable_for(osmium.osm.NODE))
                 .with_filter(osmium.filter.KeyFilter(*WAY_KEYS).enable_for(osmium.osm.WAY))
                 .with_filter(osmium.filter.KeyFilter(*AREA_KEYS).enable_for(osmium.osm.AREA)))
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
                poi, feature = poi_kind(tags), node_kind(tags)
                if not poi and not feature:
                    continue
                x, y = proj.forward(obj.location.lon, obj.location.lat)
                rx, rz = int(x // REGION), int(y // REGION)
                if not owns(rx, rz, k, n):
                    continue
                lx, lz = round((x - rx * REGION) * 10), round((y - rz * REGION) * 10)
                if poi:
                    region(rx, rz)["pois"].append({"id": obj.id, "kind": poi, "name": tags.get("name", ""), "x": lx,
                                                   "z": lz})
                    stats["pois"] += 1
                if feature:
                    record = node_record(obj.id, tags, feature, lx, lz, proj.true_north(obj.location.lon))
                    region(rx, rz)["nodes"].append(record)
                    stats["nodes"] += 1
                    forward = sign_travel(tags, feature) if record["dir"] < 0 else None
                    if forward is not None:
                        signs[obj.id] = (record, forward)
            elif obj.is_way():
                highway, waterway, railway = tags.get("highway"), tags.get("waterway"), tags.get("railway")
                street = highway in STREET_WIDTH and tags.get("area") != "yes"
                line = "" if street else waterway if waterway in LINE_WIDTH else \
                    railway if railway in ("rail", "abandoned", "disused") else ""
                barrier, power = barrier_kind(tags), POWER_KINDS.get(tags.get("power"))
                if not (street or line or barrier or power):
                    continue
                pts = project(obj.nodes)
                if street and signs:
                    face_signs(obj.nodes)
                if len(pts) < 2:
                    continue
                if street:
                    add_line("streets", dict(street_record(tags, highway), id=obj.id), pts)
                elif line:
                    add_line("lines", {"id": obj.id, **line_record(tags, line)}, pts)
                if barrier:
                    add_line("barriers", {"id": obj.id, **barrier_record(tags, barrier)}, pts)
                if power:
                    add_line("power", {"id": obj.id, **power_record(tags, power)}, pts)
            elif obj.is_area():
                is_building = tags.get("building", "no") != "no"
                kind = "" if is_building else area_kind(tags, obj.from_way())
                feature = "" if is_building else node_kind(tags)
                if feature not in AREA_FEATURES:
                    feature = ""
                if not is_building and not kind and not feature:
                    continue
                for outer in obj.outer_rings():
                    pts = project(outer)
                    if len(pts) > 1 and pts[0] == pts[-1]:
                        pts.pop()
                    if len(pts) < 3:
                        continue
                    if feature:  # a landmark mapped as its outline: one point in the middle of its first ring
                        north = proj.true_north(outer[0].lon)
                        add_node(node_record(obj.id, tags, feature, 0, 0, north), *middle(pts))
                        feature = ""
                    area = abs(polygon_area(pts))
                    if is_building:
                        if area < 4:
                            continue  # a sliver, not a building
                        cx, cy = middle(pts)
                        rx, rz = int(cx // REGION), int(cy // REGION)
                        if not owns(rx, rz, k, n):
                            continue
                        region(rx, rz)["buildings"].append({"id": obj.id, **building_record(tags, area),
                                                            "pts": local(pts, rx, rz)})
                        stats["buildings"] += 1
                    elif kind:
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
        listing.append(region_entry(data))
    if k == 0:
        write_json(build / "places.json", {"format": FORMAT, "places": sorted(places, key=lambda p: -p["population"])})
    write_json(build / "parts" / f"{k}-of-{n}.json", {
        "part": k, "of": n, "format": FORMAT, "machine": host, "started": started, "finished": now(),
        "read_minutes": round(read_minutes, 1), "source": source.name, "source_md5": file_md5(source),
        "tool_md5": file_md5(Path(__file__)), "stats": stats, "regions": listing})
    check_in("idle", last=f"extracted build {args.id}, part {k}/{n}: {len(regions)} regions", stats=stats)
    print(f"part {k}/{n} of build {args.id}: {len(regions)} regions in {(time.time() - t0) / 60:.1f} min "
          f"(reading {read_minutes:.1f}); {stats}")
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
    totals = {key: sum(r.get(key, 0) for r in regions) for key in ARRAYS}
    manifest = {
        "format": parts[0].get("format", 1), "id": args.id, "published": now(), "attribution": ATTRIBUTION,
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
