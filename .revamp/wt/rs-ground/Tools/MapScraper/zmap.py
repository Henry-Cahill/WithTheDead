"""zomboidmap.com map model: metadata, the site's isometric projection, regions and tile sets.

World coordinates are Project Zomboid squares (x grows east, y grows south). A cell is 256x256
squares. The site renders every floor ("layer") as its own isometric tile pyramid:

    https://tiles.zomboidmap.com/layer{z}/L{level}/{tx}_{ty}.webp     z = -17 .. 29
    https://tiles.zomboidmap.com/composite/L{level}/{tx}_{ty}.webp    all floors flattened (coarse levels)

L0 is native detail (a square is a 128x64 px diamond and a floor is 192 px tall). Each level up
halves the resolution. manifest.json lists which tiles exist.
"""
from __future__ import annotations

import re
from dataclasses import dataclass

BASE_URL = "https://tiles.zomboidmap.com"

# English text is embedded in the base files; these are the site's other languages.
LANGUAGES = ("de", "fr", "es", "pl", "da", "ru", "pt", "zh")

# Whole-map data files. Together they are about 16 MB, so they are always mirrored in full.
DATA_FILES = (
    "overlays/annotations.json",
    "overlays/spawnpoints.json",
    "overlays/streets.geojson",
    "overlays/buildings.geojson",
    "overlays/building_classes.json",
    "overlays/zones.geojson",
    "overlays/fixtures.json",
    "overlays/search.json",
    "overlays/loot/loot.json",
    "overlays/zpop.png",
    "overlays/danger.png",
    "overlays/water.png",
    *(f"overlays/loot/names.{lang}.json" for lang in LANGUAGES),
    *(f"overlays/i18n/fixtures.{lang}.json" for lang in LANGUAGES),
)

# Named regions as inclusive cell rectangles (cx0, cy0, cx1, cy1).
PRESETS = {
    # Downtown, LSU, the airport and everything down to the Knox Boundary Camp; Valley Station is
    # not included.
    "louisville": (45, 3, 61, 17),
}


@dataclass(frozen=True)
class Region:
    """Axis-aligned area in world squares. x1 and y1 are exclusive."""
    name: str
    x0: int
    y0: int
    x1: int
    y1: int

    def contains(self, x, y):
        return self.x0 <= x < self.x1 and self.y0 <= y < self.y1

    def overlaps(self, ax0, ay0, ax1, ay1):
        return ax0 < self.x1 and ax1 >= self.x0 and ay0 < self.y1 and ay1 >= self.y0

    def padded(self, pad):
        return Region(self.name, self.x0 - pad, self.y0 - pad, self.x1 + pad, self.y1 + pad)

    def cell_range(self, cell_size):
        """Inclusive cell rectangle covering the region."""
        return (self.x0 // cell_size, self.y0 // cell_size,
                (self.x1 - 1) // cell_size, (self.y1 - 1) // cell_size)

    def to_json(self, cell_size):
        return {"name": self.name, "x0": self.x0, "y0": self.y0, "x1": self.x1, "y1": self.y1,
                "width": self.x1 - self.x0, "height": self.y1 - self.y0,
                "cells": list(self.cell_range(cell_size))}


class MapInfo:
    """map_info.json plus the projection the site uses to place tiles and overlays."""

    def __init__(self, data):
        self.data = data
        self.tile_size = int(data["tile_size"])
        self.format = data.get("format", "webp")
        self.levels = int(data["levels"])
        self.grid_w = data["grid_w"]
        self.grid_h = data["grid_h"]
        self.layer_px = data["layer_px"]
        self.gx_origin = data["gx_origin"]
        self.gy_origin = data["gy_origin"]
        self.cell_size = int(data["cell_size"])
        self.cells = data["cells"]
        self.min_layer = int(data["min_layer"])
        self.max_layer = int(data["max_layer"])

    def to_px(self, x, y, z=0, level=0):
        """World position on floor z -> pixel position in the tile space of `level`."""
        s = 1 << level
        return ((x - y - self.gx_origin) * self.grid_w / s,
                ((x + y - self.gy_origin) * self.grid_h - z * self.layer_px) / s)

    def tile_hits(self, tx, ty, level, z, region):
        """Whether tile (tx, ty) of floor z at `level` overlaps the region (separating-axis test)."""
        r = region
        s = self.tile_size << level
        # The projection is linear in u = x - y and v = x + y, so the tile is a box in (u, v).
        u0 = tx * s / self.grid_w + self.gx_origin
        u1 = (tx + 1) * s / self.grid_w + self.gx_origin
        v0 = (ty * s + z * self.layer_px) / self.grid_h + self.gy_origin
        v1 = ((ty + 1) * s + z * self.layer_px) / self.grid_h + self.gy_origin
        if u1 < r.x0 - r.y1 or u0 > r.x1 - r.y0 or v1 < r.x0 + r.y0 or v0 > r.x1 + r.y1:
            return False
        # The region's own axes: x = (u + v) / 2 and y = (v - u) / 2.
        return not ((u0 + v0) / 2 > r.x1 or (u1 + v1) / 2 < r.x0
                    or (v0 - u1) / 2 > r.y1 or (v1 - u0) / 2 < r.y0)

    def whole_map(self):
        c, cs = self.cells, self.cell_size
        return Region("all", c[0] * cs, c[1] * cs, (c[2] + 1) * cs, (c[3] + 1) * cs)


class Manifest:
    """Which tiles exist. Rows are [ty, tx0, tx1, tx0, tx1, ...]: at L0 for floors and at the
    composite base level for composite tiles. Coarser levels shift them down, as the site does."""

    def __init__(self, data):
        self.layers = {int(k): {row[0]: row[1:] for row in rows}
                       for k, rows in (data.get("layers") or {}).items()}
        comp = data.get("composite")
        self.composite_base = int(comp["base"]) if comp else None
        self.composite_rows = {row[0]: row[1:] for row in comp["rows"]} if comp else {}

    def has_composite(self, level):
        return self.composite_base is not None and level >= self.composite_base

    def tiles(self, layer, level):
        """Yield every (tx, ty) the site has for a floor (or "composite") at `level`."""
        if layer == "composite":
            if not self.has_composite(level):
                return
            rows, shift = self.composite_rows, level - self.composite_base
        else:
            rows, shift = self.layers.get(layer), level
            if not rows:
                return
        spans = {}
        for ty, flat in rows.items():
            row = spans.setdefault(ty >> shift, [])
            for i in range(0, len(flat) - 1, 2):
                row.append((flat[i] >> shift, flat[i + 1] >> shift))
        for ty in sorted(spans):
            nxt = None  # first tx in this row not yielded yet
            for a, b in sorted(spans[ty]):
                start = a if nxt is None else max(a, nxt)
                for tx in range(start, b + 1):
                    yield tx, ty
                nxt = b + 1 if nxt is None else max(nxt, b + 1)


@dataclass
class TileGroup:
    layer: object  # floor number or "composite"
    level: int
    count: int

    @property
    def label(self):
        return f"L{self.level} " + ("composite" if self.layer == "composite" else f"floor {self.layer}")


def tile_path(layer, level, tx, ty, fmt="webp"):
    folder = "composite" if layer == "composite" else f"layer{layer}"
    return f"{folder}/L{level}/{tx}_{ty}.{fmt}"


def floor_order(floors):
    """Ground floor first, then upwards, then the basements going down."""
    return sorted(floors, key=lambda z: (z < 0, abs(z)))


def region_tiles(info, manifest, region, layer, level):
    z = 0 if layer == "composite" else layer
    for tx, ty in manifest.tiles(layer, level):
        if info.tile_hits(tx, ty, level, z, region):
            yield tx, ty


def tile_groups(info, manifest, region, levels, floors, composite=True):
    """Tile groups in download order: coarse levels first, so a complete low-detail map exists early."""
    groups = []
    for level in sorted(levels, reverse=True):
        layers = ["composite"] if composite and manifest.has_composite(level) else []
        layers += [z for z in floor_order(floors) if z in manifest.layers]
        for layer in layers:
            count = sum(1 for _ in region_tiles(info, manifest, region, layer, level))
            if count:
                groups.append(TileGroup(layer, level, count))
    return groups


def parse_int_set(spec, lo, hi, keywords=None):
    """'all', '3', '0-5', '-17--1', '0,2,4' or keywords -> sorted ints within [lo, hi]."""
    spec = (spec or "all").strip().lower()
    if spec in ("", "all"):
        return list(range(lo, hi + 1))
    out = set()
    for part in re.split(r"[,\s]+", spec):
        if not part:
            continue
        if keywords and part in keywords:
            out.update(keywords[part])
        elif re.fullmatch(r"-?\d+", part):
            out.add(int(part))
        else:
            m = re.fullmatch(r"(-?\d+)(?:-|\.\.|:)(-?\d+)", part)
            if not m:
                raise ValueError(f"cannot read {part!r} in {spec!r} (try all, 3, 0-5 or -17--1)")
            a, b = sorted((int(m[1]), int(m[2])))
            out.update(range(a, b + 1))
    return sorted(v for v in out if lo <= v <= hi)


def parse_levels(spec, info):
    return parse_int_set(spec, 0, info.levels - 1)


def parse_floors(spec, info):
    keywords = {"ground": [0], "above": range(1, info.max_layer + 1), "basements": range(info.min_layer, 0)}
    return parse_int_set(spec, info.min_layer, info.max_layer, keywords)


def _safe_name(name):
    return re.sub(r"[^A-Za-z0-9_.-]+", "_", name).strip("_") or "region"


def resolve_region(info, name="louisville", bbox=None, cells=None, label=None):
    cs = info.cell_size
    if bbox:
        x0, y0, x1, y1 = bbox
        return Region(_safe_name(label or f"bbox_{x0}_{y0}_{x1}_{y1}"),
                      min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1))
    if cells:
        cx0, cy0, cx1, cy1 = cells
        return Region(_safe_name(label or f"cells_{cx0}_{cy0}_{cx1}_{cy1}"),
                      min(cx0, cx1) * cs, min(cy0, cy1) * cs, (max(cx0, cx1) + 1) * cs, (max(cy0, cy1) + 1) * cs)
    key = (name or "louisville").strip().lower()
    if key == "all":
        whole = info.whole_map()
        return Region(_safe_name(label or "all"), whole.x0, whole.y0, whole.x1, whole.y1)
    if key in PRESETS:
        cx0, cy0, cx1, cy1 = PRESETS[key]
        return Region(_safe_name(label or key), cx0 * cs, cy0 * cs, (cx1 + 1) * cs, (cy1 + 1) * cs)
    raise ValueError(f"unknown region {name!r}: use all, {', '.join(PRESETS)}, --bbox or --cells")
