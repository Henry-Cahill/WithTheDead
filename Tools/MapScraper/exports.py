"""Region exports built offline from the mirrored whole-map files in out/raw.

For one region this writes streets, buildings (with class, room list, zombie/danger values),
rooms (linked to their building), zones (also split by type), fixtures, points of interest,
labels, spawn points, decoded loot tables and heatmap crops, plus a summary. It can be rebuilt
at any time without touching the network.
"""
from __future__ import annotations

import csv
import json
import logging
import math
import os
import re
from collections import Counter, defaultdict

from fetch import now_iso

log = logging.getLogger("mapscraper")

# Room type -> point-of-interest category (full-match regexes; the first match wins).
POI_RULES = [(name, re.compile(pattern)) for name, pattern in (
    ("Gas station", r"gasstore|gasstorage|gas2go|fossoil"),
    ("Police", r"police.*|evidenceroom|swatlocker"),
    ("Gun store", r"gunstore|gunstorestorage|armysurplusguns"),
    ("Military", r"army.*"),
    ("Fire station", r"fire(garage|storage|station)"),
    ("Pharmacy", r"pharmacy.*"),
    ("Medical", r"(hospital|medical|medclinic|clinic|morgue|dentist|optometrist|laboratory).*|vet"),
    ("Grocery", r"(grocery|gigamart|conveniencestore|cornerstore|generalstore|zippee|candy|liquorstore"
                r"|bakery|butcher|knoxbutcher).*|producestorage|potatostorage"),
    ("Restaurant", r"(restaurant|diner|cafe|cafeteria|burger|pizza|spiffo|jayschicken|chinese|mexican"
                   r"|italian|sushi|seafood|western|donut|icecream|hotdog|deepfry|fishchips|taco|chili).*"
                   r"|foodcourt|dining_.*|kitchen_.*"),
    ("Bar", r"bar|barkitchen|barstorage|beergarden|brewery.*|stripclub.*"),
    ("Hardware", r"toolstore|toolstorage|hardware.*|plumber|paintershop|metalshop|weldingbooth"
                 r"|woodcraftset|workshop|construction|carpentry.*"),
    ("Mechanic", r"mechanic|carsupply|cardealership.*|aircraftgarage|railroadrepair"),
    ("Clothing", r"clothe?s.*|clothing.*|leatherclothesstore|shoe.*|sewing.*|dressingrooms?|department.*"
                 r"|jewelry.*|glassesstore|walletshop"),
    ("Electronics", r"electronics.*|camerastore|radio.*|communications|serverroom|broadcasting"),
    ("Books", r"book.*|library|universitylibrary|comic.*|newsroom|newspaper.*"),
    ("Bank", r"bank.*|vault|depositboxes"),
    ("Church", r"church|lobbychurch|officechurch|chapel"),
    ("School", r".*classroom.*|school.*|elementary.*|secondary.*|university.*|daycare"),
    ("Hotel", r"motel.*|hotel.*|ballroom"),
    ("Sporting goods", r"sportstore|sportstorage|camping|huntingstore"),
    ("Gym & sports", r"gym.*|fitness|sports?|sportroom|boxing|bowlingalley|rollerrink|baseball.*|golf|pool"),
    ("Post office", r"post|poststorage"),
    ("Farm", r"barn|farmstorage|stable|greenhouse|horsebox|kennels"),
    ("Industry", r".*factory.*|.*shipping.*|warehouse|storageunit|whiske?y.*|.*bottling"),
    ("Entertainment", r"theatre.*|movierental|gameroom|lasertag|musicstage|backstage|.*game|arcade.*"
                      r"|recreation(al)?|homecinema"),
    ("Shops", r"pawnshop.*|gift.*|toy.*|petstore|artstore|musicstore|furniture.*|gardenstore"
              r"|housewarestore|kitchenwares|barbequestore|windowsstore|knifestore|.*store"),
)]

# Ground-cover zones painted (in this order) into forage_zones.png.
FORAGE_ZONES = (
    ("TownZone", (150, 150, 150, 255)),
    ("FarmLand", (205, 190, 110, 255)),
    ("Farm", (190, 160, 90, 255)),
    ("Vegitation", (120, 180, 90, 255)),
    ("Forest", (50, 130, 60, 255)),
    ("DeepForest", (20, 80, 35, 255)),
)


# ---------------------------------------------------------------- file helpers

def _raw(raw_dir, rel):
    return os.path.join(raw_dir, *rel.split("/"))


def load_raw(raw_dir, rel, required=True):
    path = _raw(raw_dir, rel)
    if not os.path.exists(path):
        if required:
            log.warning("  missing %s (run the data phase first)", rel)
        return None
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def write_json(path, data, pretty=True):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        if pretty:
            json.dump(data, f, ensure_ascii=False, indent=1)
        else:
            json.dump(data, f, ensure_ascii=False, separators=(",", ":"))


def write_csv(path, header, rows):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(header)
        writer.writerows(rows)


def _collection(features):
    return {"type": "FeatureCollection", "features": features}


# ---------------------------------------------------------------- geometry

def _bbox(points):
    xs = [p[0] for p in points]
    ys = [p[1] for p in points]
    return min(xs), min(ys), max(xs), max(ys)


def _segment_hits(ax, ay, bx, by, r):
    """Liang-Barsky clip: does the segment A-B cross region r?"""
    t0, t1 = 0.0, 1.0
    dx, dy = bx - ax, by - ay
    for p, q in ((-dx, ax - r.x0), (dx, r.x1 - ax), (-dy, ay - r.y0), (dy, r.y1 - ay)):
        if p == 0:
            if q < 0:
                return False
            continue
        t = q / p
        if p < 0:
            if t > t1:
                return False
            t0 = max(t0, t)
        else:
            if t < t0:
                return False
            t1 = min(t1, t)
    return t0 <= t1


def _inside_ring(x, y, ring):
    inside = False
    for a, b in zip(ring, ring[1:] + ring[:1]):
        if (a[1] > y) != (b[1] > y) and x < a[0] + (y - a[1]) * (b[0] - a[0]) / (b[1] - a[1]):
            inside = not inside
    return inside


def geometry_hits(geom, r):
    kind, coords = geom["type"], geom["coordinates"]
    if kind == "Point":
        return r.contains(coords[0], coords[1])
    if kind in ("LineString", "MultiPoint"):
        parts = [coords]
    elif kind in ("MultiLineString", "Polygon"):
        parts = coords
    elif kind == "MultiPolygon":
        parts = [ring for poly in coords for ring in poly]
    else:
        return False
    if not r.overlaps(*_bbox([p for part in parts for p in part])):
        return False
    for part in parts:
        if any(r.contains(p[0], p[1]) for p in part):
            return True
        if any(_segment_hits(a[0], a[1], b[0], b[1], r) for a, b in zip(part, part[1:])):
            return True
    # The region can sit entirely inside a large polygon.
    if kind == "Polygon":
        return _inside_ring(r.x0, r.y0, coords[0])
    if kind == "MultiPolygon":
        return any(_inside_ring(r.x0, r.y0, poly[0]) for poly in coords)
    return False


def _ring_area(ring):
    return abs(sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(ring, ring[1:] + ring[:1]))) / 2


def _line_length(coords):
    return sum(math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(coords, coords[1:]))


class RectIndex:
    """Bucket grid over rectangles for 'smallest rectangle containing this point' lookups."""

    def __init__(self, bucket=64):
        self.bucket = bucket
        self.grid = defaultdict(list)

    def add(self, rect, value):
        b = self.bucket
        for gx in range(int(rect[0]) // b, int(rect[2]) // b + 1):
            for gy in range(int(rect[1]) // b, int(rect[3]) // b + 1):
                self.grid[gx, gy].append((rect, value))

    def smallest_at(self, x, y, accept=None):
        best, best_area = None, math.inf
        for rect, value in self.grid.get((int(x) // self.bucket, int(y) // self.bucket), ()):
            if rect[0] <= x <= rect[2] and rect[1] <= y <= rect[3] and (accept is None or accept(value)):
                area = (rect[2] - rect[0]) * (rect[3] - rect[1])
                if area < best_area:
                    best, best_area = value, area
        return best


class HeatSampler:
    """Zombie population (0-10) and danger (0-255) at a world position, read from the site's PNGs."""

    def __init__(self, raw_dir, info):
        self.maps = {}
        try:
            from PIL import Image
        except ImportError:
            return
        map_w = (info.cells[2] - info.cells[0] + 1) * info.cell_size
        for name in ("zpop", "danger"):
            path = _raw(raw_dir, f"overlays/{name}.png")
            if os.path.exists(path):
                with Image.open(path) as im:
                    gray = im.convert("L")
                self.maps[name] = (gray.load(), gray.size, map_w / gray.size[0])

    def at(self, name, x, y):
        entry = self.maps.get(name)
        if not entry:
            return None
        pixels, (w, h), scale = entry
        px, py = int(x // scale), int(y // scale)
        return pixels[px, py] if 0 <= px < w and 0 <= py < h else None


# ---------------------------------------------------------------- exports

def export_streets(raw, region, out):
    data = load_raw(raw, "overlays/streets.geojson")
    if not data:
        return {}
    features = [f for f in data["features"] if geometry_hits(f["geometry"], region)]
    write_json(os.path.join(out, "streets.geojson"), _collection(features), pretty=False)
    rows, lengths = [], defaultdict(float)
    for f in features:
        coords, props = f["geometry"]["coordinates"], f.get("properties", {})
        length = _line_length(coords)
        lengths[props.get("name", "")] += length
        rows.append([props.get("name", ""), props.get("width", ""), round(length, 1), *_bbox(coords)])
    write_csv(os.path.join(out, "streets.csv"), ["name", "width", "length_squares", "x0", "y0", "x1", "y1"], rows)
    names = sorted(n for n in lengths if n)
    write_json(os.path.join(out, "street_names.json"), names)
    return {"segments": len(features), "named_streets": len(names)}


def _class_lookup(classes):
    if not classes:
        return lambda rect: None
    names, rects = classes.get("classes", []), classes.get("b", [])
    exact = {tuple(b[:4]): names[b[4]] for b in rects if b[4] < len(names)}
    index = RectIndex()
    for b in rects:
        if b[4] < len(names):
            index.add(b[:4], names[b[4]])

    def lookup(rect):
        hit = exact.get(tuple(rect))
        return hit if hit else index.smallest_at((rect[0] + rect[2]) / 2, (rect[1] + rect[3]) / 2)
    return lookup


def export_buildings(raw, region, out, heat):
    data = load_raw(raw, "overlays/buildings.geojson")
    if not data:
        return []
    search = (load_raw(raw, "overlays/search.json") or {}).get("buildings", [])
    by_rect = {(tuple(s["r"]), tuple(s.get("f", ()))): s for s in search}
    class_of = _class_lookup(load_raw(raw, "overlays/building_classes.json"))
    buildings, features, rows = [], [], []
    for i, f in enumerate(data["features"]):
        p = f.get("properties", {})
        x0, y0, x1, y1 = _bbox(f["geometry"]["coordinates"][0])
        if not region.overlaps(x0, y0, x1, y1):
            continue
        floors = (p.get("min_layer", 0), p.get("max_layer", 0))
        entry = search[i] if i < len(search) and search[i]["r"] == [x0, y0, x1, y1] else by_rect.get(((x0, y0, x1, y1), floors))
        rooms = [{"type": n, "x": pt[0], "y": pt[1]} for n, pt in zip(entry["n"], entry.get("p", []))] if entry else []
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        cell = p.get("cell", [0, 0])
        props = {
            "id": f"{cell[0]}_{cell[1]}_{p.get('building', i)}", "cell": cell, "building": p.get("building"),
            "class": class_of([x0, y0, x1, y1]), "rooms": p.get("rooms"),
            "min_layer": floors[0], "max_layer": floors[1], "floor_count": floors[1] - floors[0] + 1,
            "bbox": [x0, y0, x1, y1], "center": [cx, cy], "area": (x1 - x0) * (y1 - y0),
            "zpop": heat.at("zpop", cx, cy), "danger": heat.at("danger", cx, cy), "room_types": rooms,
        }
        buildings.append(props)
        features.append({"type": "Feature", "properties": props, "geometry": f["geometry"]})
        rows.append([props["id"], props["class"] or "", cell[0], cell[1], x0, y0, x1, y1, props["area"],
                     floors[0], floors[1], props["rooms"], props["zpop"], props["danger"],
                     ";".join(r["type"] for r in rooms)])
    write_json(os.path.join(out, "buildings.geojson"), _collection(features), pretty=False)
    write_csv(os.path.join(out, "buildings.csv"),
              ["id", "class", "cell_x", "cell_y", "x0", "y0", "x1", "y1", "area", "min_layer", "max_layer",
               "rooms", "zpop", "danger", "room_types"], rows)
    return buildings


def export_rooms(raw, region, out, info, buildings):
    index = RectIndex()
    for b in buildings:
        index.add(b["bbox"], b)
    cx0, cy0, cx1, cy1 = region.cell_range(info.cell_size)
    rooms, rows, counts, cells = [], [], Counter(), 0
    for cx in range(cx0, cx1 + 1):
        for cy in range(cy0, cy1 + 1):
            data = load_raw(raw, f"overlays/rooms/{cx}_{cy}.json", required=False)
            if not data:
                continue
            cells += 1
            for i, room in enumerate(data):
                rects = room.get("r") or []
                if not rects:
                    continue
                x0, y0 = min(r[0] for r in rects), min(r[1] for r in rects)
                x1, y1 = max(r[0] + r[2] for r in rects), max(r[1] + r[3] for r in rects)
                if not region.overlaps(x0, y0, x1 - 1, y1 - 1):
                    continue
                layer, kind = room.get("l", 0), room.get("n", "")
                owner = index.smallest_at((x0 + x1) / 2, (y0 + y1) / 2,
                                          accept=lambda b, z=layer: b["min_layer"] <= z <= b["max_layer"])
                rid = f"{cx}_{cy}_{i}"
                area = sum(r[2] * r[3] for r in rects)
                rooms.append({"id": rid, "type": kind, "layer": layer, "building": owner["id"] if owner else None,
                              "bbox": [x0, y0, x1, y1], "area": area, "rects": rects})
                rows.append([rid, kind, layer, owner["id"] if owner else "", x0, y0, x1, y1, area, len(rects)])
                counts[kind] += 1
    write_json(os.path.join(out, "rooms.json"), rooms, pretty=False)
    write_csv(os.path.join(out, "rooms.csv"),
              ["id", "type", "layer", "building", "x0", "y0", "x1", "y1", "area", "rect_count"], rows)
    write_json(os.path.join(out, "room_types.json"), dict(counts.most_common()))
    if not cells:
        log.warning("  no room files found for this region (run the rooms phase first)")
    return rooms, counts


def export_zones(raw, region, out):
    data = load_raw(raw, "overlays/zones.geojson")
    if not data:
        return [], Counter()
    features = [f for f in data["features"] if geometry_hits(f["geometry"], region)]
    write_json(os.path.join(out, "zones.geojson"), _collection(features), pretty=False)
    by_type, rows = defaultdict(list), []
    for f in features:
        props, geom = f.get("properties", {}), f["geometry"]
        kind = props.get("type") or "Unknown"
        by_type[kind].append(f)
        points = geom["coordinates"][0] if geom["type"] == "Polygon" else geom["coordinates"]
        area = _ring_area(points) if geom["type"] == "Polygon" else 0
        rows.append([kind, props.get("name", ""), props.get("z", 0), *_bbox(points), round(area, 1), len(points),
                     props.get("lineWidth", "")])
    for kind, fs in by_type.items():
        write_json(os.path.join(out, "zones", re.sub(r"[^A-Za-z0-9_-]+", "_", kind) + ".geojson"),
                   _collection(fs), pretty=False)
    write_csv(os.path.join(out, "zones.csv"),
              ["type", "name", "z", "x0", "y0", "x1", "y1", "area", "vertices", "line_width"], rows)
    return features, Counter({k: len(v) for k, v in by_type.items()})


def export_fixtures(raw, region, out):
    data = load_raw(raw, "overlays/fixtures.json")
    if not data:
        return Counter()
    kept, rows, counts = {}, [], Counter()
    for kind, entry in data.items():
        names = entry.get("names", [])
        points = [p for p in entry.get("pts", []) if region.contains(p[0], p[1])]
        if not points:
            continue
        kept[kind] = {"names": names, "pts": points}
        counts[kind] = len(points)
        for p in points:
            idx = p[3] if len(p) > 3 else 0
            rows.append([kind, names[idx] if idx < len(names) else "", p[0], p[1], p[2] if len(p) > 2 else 0])
    write_json(os.path.join(out, "fixtures.json"), kept, pretty=False)
    write_csv(os.path.join(out, "fixtures.csv"), ["type", "name", "x", "y", "z"], rows)
    return counts


def export_pois(out, buildings):
    category_of = {}

    def category(room_type):
        if room_type not in category_of:
            category_of[room_type] = next((name for name, rx in POI_RULES if rx.fullmatch(room_type)), None)
        return category_of[room_type]

    pois = []
    for b in buildings:
        found = {}
        for room in b["room_types"]:
            cat = category(room["type"])
            if not cat:
                continue
            if cat in found:
                found[cat]["room_types"].append(room["type"])
            else:
                found[cat] = {"category": cat, "x": room["x"], "y": room["y"], "building": b["id"],
                              "building_class": b["class"], "room_types": [room["type"]]}
        pois.extend(found.values())
    write_json(os.path.join(out, "pois.json"), pois)
    write_csv(os.path.join(out, "pois.csv"), ["category", "x", "y", "building", "building_class", "room_types"],
              [[p["category"], p["x"], p["y"], p["building"], p["building_class"] or "", ";".join(p["room_types"])]
               for p in pois])
    return Counter(p["category"] for p in pois)


def export_points(raw, region, out):
    """Map labels (annotations) and spawn points."""
    counts = {}
    labels = [a for a in (load_raw(raw, "overlays/annotations.json") or []) if region.contains(a["x"], a["y"])]
    write_json(os.path.join(out, "labels.json"), labels)
    write_csv(os.path.join(out, "labels.csv"), ["text", "style", "x", "y", "scale", "rotation"],
              [[a.get("t", "").replace("\n", " "), a.get("s", ""), a["x"], a["y"], a.get("sc", ""), a.get("r", "")]
               for a in labels])
    counts["labels"] = len(labels)
    spawns = [s for s in (load_raw(raw, "overlays/spawnpoints.json") or []) if region.contains(s["x"], s["y"])]
    write_json(os.path.join(out, "spawnpoints.json"), spawns)
    counts["spawnpoints"] = len(spawns)
    return counts


def export_raw_subsets(raw, region, out):
    """The site's own search.json and building_classes.json, cut down to the region."""
    search = load_raw(raw, "overlays/search.json")
    if search:
        write_json(os.path.join(out, "site_format", "search.json"),
                   {"buildings": [b for b in search.get("buildings", []) if region.overlaps(*b["r"])]}, pretty=False)
    classes = load_raw(raw, "overlays/building_classes.json")
    if classes:
        write_json(os.path.join(out, "site_format", "building_classes.json"),
                   {"classes": classes.get("classes", []),
                    "b": [b for b in classes.get("b", []) if region.overlaps(*b[:4])]}, pretty=False)


def export_loot(raw, global_dir, out, room_counts):
    """Decode loot.json (indexes -> names) globally, then list the loot of room types in the region."""
    data = load_raw(raw, "overlays/loot/loot.json")
    if not data:
        return {}
    items, rooms, counts = data.get("items", []), data.get("rooms", []), data.get("roomCounts", {})
    containers = {name: sorted(rooms[i] for i in idx if i < len(rooms))
                  for name, idx in data.get("containers", {}).items()}
    room_containers = defaultdict(list)
    for name, room_list in containers.items():
        for room in room_list:
            room_containers[room].append(name)
    by_room = {}
    for ri, entries in data.get("roomItems", {}).items():
        name = rooms[int(ri)]
        by_room[name] = {"rooms_on_map": counts.get(ri, 0), "containers": sorted(room_containers.get(name, [])),
                         "items": [{"item": items[ii], "score": score} for ii, score in entries]}
    by_item = {items[int(ii)]: [{"room": rooms[ri], "score": score, "rooms_on_map": counts.get(str(ri), 0)}
                                for ri, score in entries]
               for ii, entries in data.get("itemRooms", {}).items()}
    folder = os.path.join(global_dir, "loot")
    write_json(os.path.join(folder, "rooms.json"), by_room)
    write_json(os.path.join(folder, "items.json"), by_item)
    write_json(os.path.join(folder, "containers.json"), containers)
    write_csv(os.path.join(folder, "room_items.csv"), ["room", "item", "score"],
              [[room, e["item"], e["score"]] for room, entry in by_room.items() for e in entry["items"]])

    lower = {name.lower(): name for name in by_room}
    here, rows = [], []
    for room_type, n in room_counts.most_common():
        key = lower.get(room_type.lower())
        if not key:
            continue
        entry = by_room[key]
        here.append({"room": key, "rooms_here": n, "rooms_on_map": entry["rooms_on_map"],
                     "containers": entry["containers"], "items": entry["items"]})
        rows.extend([key, n, e["item"], e["score"]] for e in entry["items"])
    write_json(os.path.join(out, "loot_here.json"), here)
    write_csv(os.path.join(out, "loot_here.csv"), ["room", "rooms_here", "item", "score"], rows)
    return {"items": len(items), "room_types": len(by_room), "room_types_here": len(here)}


def export_heatmaps(raw, region, out, info, zones):
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        log.warning("  Pillow is not installed - skipping heatmap crops (python -m pip install pillow)")
        return {}
    Image.MAX_IMAGE_PIXELS = None
    folder = os.path.join(out, "heatmaps")
    os.makedirs(folder, exist_ok=True)
    map_w = (info.cells[2] - info.cells[0] + 1) * info.cell_size
    meta = {}
    for name, meaning in (("zpop", "zombie population, 0 (none) to 10 (highest)"),
                          ("danger", "danger rating, 0 to 255"),
                          ("water", "water view, RGBA as drawn by the site")):
        path = _raw(raw, f"overlays/{name}.png")
        if not os.path.exists(path):
            continue
        with Image.open(path) as im:
            scale = map_w / im.width
            box = (max(0, int(region.x0 // scale)), max(0, int(region.y0 // scale)),
                   min(im.width, math.ceil(region.x1 / scale)), min(im.height, math.ceil(region.y1 / scale)))
            im.crop(box).save(os.path.join(folder, f"{name}.png"))
        meta[name] = {"file": f"{name}.png", "squares_per_pixel": scale, "origin": [box[0] * scale, box[1] * scale],
                      "size": [box[2] - box[0], box[3] - box[1]], "values": meaning}

    width, height = region.x1 - region.x0, region.y1 - region.y0
    scale = max(1, math.ceil(math.sqrt(width * height / 64e6)))
    img = Image.new("RGBA", (math.ceil(width / scale), math.ceil(height / scale)), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    by_type = defaultdict(list)
    for f in zones:
        if f["geometry"]["type"] == "Polygon" and f.get("properties", {}).get("z", 0) == 0:
            by_type[f["properties"].get("type")].append(f["geometry"]["coordinates"][0])
    for kind, color in FORAGE_ZONES:
        for ring in by_type.get(kind, ()):
            draw.polygon([((p[0] - region.x0) / scale, (p[1] - region.y0) / scale) for p in ring], fill=color)
    img.save(os.path.join(folder, "forage_zones.png"))
    meta["forage_zones"] = {"file": "forage_zones.png", "squares_per_pixel": scale, "origin": [region.x0, region.y0],
                            "size": list(img.size), "legend": {k: list(c) for k, c in FORAGE_ZONES}}
    write_json(os.path.join(folder, "heatmaps.json"), meta)
    return {name: entry["size"] for name, entry in meta.items()}


def export_region(raw, region, out, global_dir, info):
    """Write every export for `region` into `out`. Returns the summary dict."""
    log.info("Exporting region '%s' (x %d..%d, y %d..%d) to %s", region.name, region.x0, region.x1,
             region.y0, region.y1, out)
    os.makedirs(out, exist_ok=True)
    write_json(os.path.join(out, "region.json"), region.to_json(info.cell_size))
    heat = HeatSampler(raw, info)
    streets = export_streets(raw, region, out)
    buildings = export_buildings(raw, region, out, heat)
    rooms, room_counts = export_rooms(raw, region, out, info, buildings)
    zones, zone_counts = export_zones(raw, region, out)
    fixture_counts = export_fixtures(raw, region, out)
    poi_counts = export_pois(out, buildings)
    point_counts = export_points(raw, region, out)
    export_raw_subsets(raw, region, out)
    loot = export_loot(raw, global_dir, out, room_counts)
    heatmaps = export_heatmaps(raw, region, out, info, zones)
    summary = {
        "region": region.to_json(info.cell_size), "generated_at": now_iso(), "map_info": info.data,
        "streets": streets, "buildings": len(buildings),
        "buildings_by_class": dict(Counter(b["class"] or "unknown" for b in buildings).most_common()),
        "rooms": len(rooms), "room_types": len(room_counts), "top_room_types": dict(room_counts.most_common(40)),
        "zones": dict(zone_counts.most_common()), "fixtures": dict(fixture_counts.most_common()),
        "pois": dict(poi_counts.most_common()), **point_counts, "loot": loot, "heatmaps": heatmaps,
    }
    write_json(os.path.join(out, "summary.json"), summary)
    log.info("  %d buildings, %d rooms (%d types), %d street segments, %d zones, %d fixtures, %d POIs, %d labels",
             len(buildings), len(rooms), len(room_counts), streets.get("segments", 0), len(zones),
             sum(fixture_counts.values()), sum(poi_counts.values()), point_counts.get("labels", 0))
    return summary
