"""Tests for the world builder's geometry and tag rules (no OpenStreetMap file needed):
    python -m unittest discover -s Tools/World/tests -v
With pyosmium installed, a tiny made-up map is also extracted and published end to end:
    uv run --python 3.12 --with osmium python -m unittest discover -s Tools/World/tests -v
"""
from __future__ import annotations

import argparse
import contextlib
import gzip
import importlib.util
import io
import json
import math
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

HERE = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location("worldbuild", HERE.parent / "worldbuild.py")
wb = importlib.util.module_from_spec(_spec)
sys.modules["worldbuild"] = wb
_spec.loader.exec_module(wb)


@contextlib.contextmanager
def scratch_store():
    """A throwaway world store beside the tests (removed afterwards), with the builder pointed at it."""
    with tempfile.TemporaryDirectory(prefix="scratch-", dir=HERE) as tmp:
        world = Path(tmp) / "world"
        with mock.patch.object(wb, "BUILDS", world / "builds"), \
                mock.patch.object(wb, "CURRENT", world / "current.json"), \
                mock.patch.object(wb, "MACHINES", Path(tmp) / "machines"):
            yield Path(tmp)


class ProjectionTest(unittest.TestCase):
    def test_matches_epsg_3088(self):
        # Reference values from PROJ (pyproj, EPSG:4326 -> EPSG:3088).
        proj = wb.KentuckySingleZone()
        for lon, lat, x, y in ((-83.1932, 37.2498, 1726801.91, 1104820.33), (-85.7585, 38.2527, 1499256.05, 1213017.99),
                               (-88.6000, 37.0834, 1246633.47, 1087115.50), (-82.6382, 38.4784, 1771470.81, 1242596.48)):
            px, py = proj.forward(lon, lat)
            self.assertAlmostEqual(x, px, delta=0.01)
            self.assertAlmostEqual(y, py, delta=0.01)

    def test_true_north(self):
        proj = wb.KentuckySingleZone()
        for lon, lat in ((-82.0, 37.5), (-85.75, 38.0), (-89.4, 36.6)):
            (x0, y0), (x1, y1) = proj.forward(lon, lat), proj.forward(lon, lat + 0.01)
            self.assertAlmostEqual(math.degrees(math.atan2(x1 - x0, y1 - y0)), proj.true_north(lon), delta=0.01)
        self.assertLess(proj.true_north(-82.0), -2)  # in the east, true north leans west of the grid's north


class GeometryTest(unittest.TestCase):
    def test_clip_polyline(self):
        box = (0, 0, 10, 10)
        self.assertEqual([[(2, 2), (8, 8)]], wb.clip_polyline([(2, 2), (8, 8)], *box))
        self.assertEqual([], wb.clip_polyline([(-5, -5), (-1, -1)], *box))
        self.assertEqual([[(0, 5), (10, 5)]], wb.clip_polyline([(-5, 5), (15, 5)], *box))
        # In, out and back in again: two pieces.
        pieces = wb.clip_polyline([(5, 5), (5, 15), (8, 15), (8, 5)], *box)
        self.assertEqual([[(5, 5), (5, 10)], [(8, 10), (8, 5)]], pieces)
        # A bend inside stays one piece.
        self.assertEqual([[(1, 1), (5, 1), (5, 5)]], wb.clip_polyline([(1, 1), (5, 1), (5, 5)], *box))

    def test_clip_polygon(self):
        square = [(-5, -5), (5, -5), (5, 5), (-5, 5)]
        cut = wb.clip_polygon(square, 0, 0, 10, 10)
        self.assertAlmostEqual(25, abs(wb.polygon_area(cut)))
        self.assertEqual([], wb.clip_polygon([(20, 20), (30, 20), (30, 30)], 0, 0, 10, 10))
        self.assertAlmostEqual(100, abs(wb.polygon_area(wb.clip_polygon([(-1, -1), (11, -1), (11, 11), (-1, 11)],
                                                                         0, 0, 10, 10))))

    def test_regions_and_local_coordinates(self):
        r = wb.REGION
        self.assertEqual([(1, 2)], wb.regions_touched([(r + 10, 2 * r + 10), (r + 20, 2 * r + 30)]))
        self.assertEqual({(0, 2), (1, 2)}, set(wb.regions_touched([(r + 5, 2 * r + 100)], margin=16)))
        self.assertEqual([123, 4567], wb.local([(r + 12.3, 2 * r + 456.7)], 1, 2))
        self.assertEqual([(0, 0), (1, 0)], wb.drop_repeats([(0, 0), (0.01, 0), (1, 0)]))

    def test_parts_share_every_region_exactly_once(self):
        for n in (1, 2, 4, 5):
            for rx in range(140, 240, 7):
                for rz in range(110, 170, 3):
                    self.assertEqual(1, sum(wb.owns(rx, rz, k, n) for k in range(n)))
        self.assertEqual((0, 1), wb.parse_part(None))
        self.assertEqual((2, 4), wb.parse_part("2/4"))
        with self.assertRaises(SystemExit):
            wb.parse_part("4/4")


class TagTest(unittest.TestCase):
    def test_buildings(self):
        self.assertEqual("house", wb.building_kind({"building": "detached"}, 120))
        self.assertEqual("church", wb.building_kind({"building": "yes", "amenity": "place_of_worship"}, 400))
        self.assertEqual("commercial", wb.building_kind({"building": "yes", "shop": "hardware"}, 300))
        self.assertEqual("house", wb.building_kind({"building": "yes"}, 140))
        self.assertEqual("warehouse", wb.building_kind({"building": "yes"}, 6000))

    def test_areas_points_and_streets(self):
        self.assertEqual("water", wb.area_kind({"natural": "water", "water": "pond"}))
        self.assertEqual("forest", wb.area_kind({"natural": "wood"}))
        self.assertEqual("parking", wb.area_kind({"amenity": "parking"}))
        self.assertEqual("", wb.area_kind({"landuse": "unknown_thing"}))
        self.assertEqual("fuel", wb.poi_kind({"amenity": "fuel"}))
        self.assertEqual("shop:hardware", wb.poi_kind({"shop": "hardware"}))
        s = wb.street_record({"name": "Main Street", "lanes": "2", "bridge": "yes"}, "secondary")
        self.assertEqual((7.6, "Main Street", True, False), (s["width"], s["name"], s["bridge"], s["oneway"]))
        self.assertEqual(7, wb.street_record({}, "residential")["width"])
        self.assertEqual(12.0, wb.number("12 m"))
        self.assertEqual(0.0, wb.number("unknown"))
        self.assertEqual((0.0, 0.0), (wb.number("nan"), wb.number("inf")))


class Format2StreetTest(unittest.TestCase):
    def test_surface(self):
        cases = {"asphalt": "asphalt", "paved": "asphalt", "chipseal": "asphalt", "concrete": "concrete",
                 "concrete:plates": "concrete", "concrete:lanes": "concrete", "paving_stones": "brick", "sett": "brick",
                 "cobblestone": "brick", "bricks": "brick", "brick": "brick", "unpaved": "gravel",
                 "compacted": "gravel", "fine_gravel": "gravel", "gravel": "gravel", "pebblestone": "gravel",
                 "dirt": "dirt", "earth": "dirt", "ground": "dirt", "grass": "dirt", "mud": "dirt", "sand": "dirt",
                 "Asphalt": "asphalt", "dirt,_grass": "dirt", "dirt/sand": "dirt", "asphalt;concrete": "asphalt",
                 "paving_stones:30": "brick", "wood": "", "metal": "", "artificial_turf": "", "": "", None: ""}
        for value, kind in cases.items():
            self.assertEqual(kind, wb.surface(value), value)

    def test_sidewalk(self):
        cases = [({"sidewalk": "both"}, "both"), ({"sidewalk": "left"}, "left"), ({"sidewalk": "right"}, "right"),
                 ({"sidewalk": "no"}, "no"), ({"sidewalk": "none"}, "no"), ({"sidewalk": "separate"}, "no"),
                 ({"sidewalk": "yes"}, "both"), ({"sidewalk:both": "yes"}, "both"),
                 ({"sidewalk:both": "separate"}, "no"), ({"sidewalk:both": "no"}, "no"),
                 ({"sidewalk:left": "yes", "sidewalk:right": "separate"}, "left"),
                 ({"sidewalk:left": "no", "sidewalk:right": "yes"}, "right"), ({"sidewalk:left": "yes"}, "left"),
                 ({"sidewalk:left": "no", "sidewalk:right": "no"}, "no"), ({"sidewalk:left": "no"}, ""),
                 ({"sidewalk:both": "no", "sidewalk:right": "yes"}, "right"), ({"sidewalk:left": "lane"}, ""),
                 ({}, "")]
        for tags, side in cases:
            self.assertEqual(side, wb.sidewalk(tags), tags)

    def test_lit(self):
        cases = {"yes": 1, "24/7": 1, "dusk-dawn": 1, "automatic": 1, "Mo-Fr 18:00-23:00": 1, "no": 2, "disused": 2,
                 "": 0, None: 0, "maybe": 0}
        for value, lit in cases.items():
            self.assertEqual(lit, wb.lit(value), value)

    def test_street_record(self):
        s = wb.street_record({"name": "North Third Street", "ref": "US 31E;US 150", "surface": "paved",
                              "sidewalk": "separate", "lit": "yes", "lanes": "2"}, "primary")
        self.assertEqual({"kind": "primary", "name": "North Third Street", "width": 7.6, "lanes": 2, "oneway": False,
                          "bridge": False, "tunnel": False, "layer": 0, "surface": "asphalt", "sidewalk": "no",
                          "lit": 1, "route": "US 31E;US 150"}, s)
        s = wb.street_record({"ref": "KY 245", "surface": "gravel"}, "tertiary")
        self.assertEqual(("KY 245", "KY 245", "gravel", "", 0), (s["name"], s["route"], s["surface"], s["sidewalk"],
                                                                 s["lit"]))
        service = wb.street_record({}, "service")
        self.assertEqual(("", ""), (service["route"], service["surface"]))

    def test_line_record(self):
        culvert = wb.line_record({"waterway": "stream", "tunnel": "culvert", "layer": "-1"}, "stream")
        self.assertEqual({"kind": "stream", "name": "", "width": 3, "bridge": False, "tunnel": True, "layer": -1},
                         culvert)
        rail = wb.line_record({"railway": "rail", "bridge": "viaduct", "layer": "1", "name": "R. J. Corman"}, "rail")
        self.assertEqual((True, False, 1, "R. J. Corman"),
                         (rail["bridge"], rail["tunnel"], rail["layer"], rail["name"]))


class Format2BuildingTest(unittest.TestCase):
    def test_roof_shape(self):
        cases = {"gabled": "gabled", "Hipped": "hipped", "half-hipped": "half-hipped", "half_hipped": "half-hipped",
                 "side_hipped": "hipped", "hipped-and-gabled": "half-hipped", "pyramidal": "pyramidal",
                 "cone": "pyramidal", "skillion": "skillion", "lean_to": "skillion", "gambrel": "gambrel",
                 "mansard": "mansard", "dome": "dome", "onion": "dome", "round": "round", "barrel": "round",
                 "flat": "flat", "saltbox": "gabled", "quadruple_saltbox": "gabled", "crosspitched": "gabled",
                 "many": "", "": "", None: ""}
        for value, shape in cases.items():
            self.assertEqual(shape, wb.roof_shape(value), value)

    def test_material(self):
        cases = {"brick": "brick", "plastic": "vinyl", "vinyl": "vinyl", "timber_framing": "wood", "Wood": "wood",
                 "cement_block": "concrete", "concrete": "concrete", "limestone": "stone", "sandstone": "stone",
                 "stone": "stone", "plaster": "stucco", "stucco": "stucco", "metal": "metal", "steel": "metal",
                 "glass": "glass", "masonry": "brick", "brick_vinyl": "brick", "brick;wood": "brick", "mud_hut": "",
                 "": "", None: ""}
        for value, kind in cases.items():
            self.assertEqual(kind, wb.material(value), value)

    def test_colour(self):
        cases = {"white": "#ffffff", "White": "#ffffff", "#CCC": "#cccccc", "#A17659": "#a17659", "beige": "#f5f5dc",
                 "Light Grey": "#d3d3d3", "light_grey": "#d3d3d3", "dark-red": "#8b0000", "red;white": "#ff0000",
                 "tan": "#d2b48c", "#12345": "", "#ggg": "", "cream": "", "ffffff": "", "": "", None: ""}
        for value, hex_colour in cases.items():
            self.assertEqual(hex_colour, wb.colour(value), value)

    def test_year(self):
        cases = {"1885": 1885, "1885-06-12": 1885, "~1885": 1885, "1880s": 1880, "before 1900": 1900, "c. 1920": 1920,
                 "C19": 1850, "2022": 2022, "late 19th century": 0, "12345": 0, "": 0, None: 0}
        for value, y in cases.items():
            self.assertEqual(y, wb.year(value), value)

    def test_building_record(self):
        b = wb.building_record({"building": "house", "roof:shape": "hip", "building:material": "plastic",
                                "building:colour": "Beige", "roof:colour": "#A00", "roof:levels": "1",
                                "start_date": "~1885", "addr:housenumber": "114", "addr:street": "Flaget Avenue"}, 140)
        self.assertEqual({"kind": "house", "name": "", "address": "114 Flaget Avenue", "levels": 1, "height": 0.0,
                          "use": "", "roof": "hipped", "material": "vinyl", "colour": "#f5f5dc",
                          "roofColour": "#aa0000", "roofLevels": 1, "year": 1885}, b)
        b = wb.building_record({"building": "yes", "construction_date": "1920", "roof:levels": "-2"}, 6000)
        self.assertEqual(("warehouse", 1920, 0, ""), (b["kind"], b["year"], b["roofLevels"], b["roof"]))


class Format2FeatureTest(unittest.TestCase):
    def test_barriers(self):
        cases = [({"barrier": "fence"}, "fence", 0.1), ({"barrier": "wall"}, "wall", 0.4),
                 ({"barrier": "city_wall"}, "wall", 0.4), ({"barrier": "hedge"}, "hedge", 1.2),
                 ({"barrier": "guard_rail"}, "guard_rail", 0.3), ({"barrier": "retaining_wall"}, "retaining_wall", 0.5),
                 ({"natural": "tree_row"}, "tree_row", 6), ({"barrier": "wall", "width": "0.6"}, "wall", 0.6),
                 ({"barrier": "fence", "width": "0.01"}, "fence", 0.1)]
        for tags, kind, width in cases:
            self.assertEqual(kind, wb.barrier_kind(tags), tags)
            self.assertEqual({"kind": kind, "name": "", "width": width}, wb.barrier_record(tags, kind))
        self.assertEqual("", wb.barrier_kind({"barrier": "kerb"}))
        self.assertEqual("", wb.barrier_kind({"natural": "wood"}))

    def test_power(self):
        self.assertEqual(("line", "minor_line", None),
                         tuple(wb.POWER_KINDS.get(v) for v in ("line", "minor_line", "cable")))
        widths = {"69000": 4, "138000;69000": 9, "161000": 9, "345000": 16, "500000": 24, "": 9}
        for voltage, width in widths.items():
            self.assertEqual({"kind": "line", "name": "", "width": width},
                             wb.power_record({"power": "line", "voltage": voltage}, "line"), voltage)
        self.assertEqual(2.4, wb.power_record({"power": "minor_line", "voltage": "7200"}, "minor_line")["width"])

    def test_node_kinds(self):
        cases = [({"natural": "tree"}, "tree"), ({"highway": "street_lamp"}, "street_lamp"),
                 ({"highway": "traffic_signals"}, "traffic_signals"), ({"highway": "stop"}, "stop"),
                 ({"highway": "give_way"}, "give_way"), ({"highway": "crossing"}, "crossing"),
                 ({"railway": "level_crossing"}, "level_crossing"), ({"highway": "bus_stop"}, "bus_stop"),
                 ({"emergency": "fire_hydrant"}, "hydrant"), ({"amenity": "bench"}, "bench"),
                 ({"amenity": "waste_basket"}, "waste_basket"), ({"amenity": "post_box"}, "post_box"),
                 ({"amenity": "telephone"}, "telephone"), ({"amenity": "vending_machine"}, "vending"),
                 ({"man_made": "flagpole"}, "flagpole"), ({"man_made": "water_tower"}, "water_tower"),
                 ({"man_made": "mast"}, "mast"), ({"man_made": "tower", "tower:type": "communication"}, "mast"),
                 ({"historic": "memorial"}, "memorial"), ({"historic": "monument"}, "memorial"),
                 ({"barrier": "gate"}, "gate"), ({"barrier": "swing_gate"}, "gate"),
                 ({"barrier": "bollard"}, "bollard"),
                 ({"man_made": "tower", "tower:type": "lighting"}, ""), ({"highway": "turning_circle"}, ""),
                 ({"power": "pole"}, ""), ({"amenity": "fuel"}, ""), ({}, "")]
        for tags, kind in cases:
            self.assertEqual(kind, wb.node_kind(tags), tags)
        self.assertEqual("fuel", wb.poi_kind({"amenity": "fuel"}))  # a point of interest, not a node

    def test_direction(self):
        cases = {"45": 45, "NE": 45, "nne": 23, "W": 270, "west": 270, "45-135": 90, "315-45": 0, "N-E": 45,
                 "-90": 270, "400": 40, "90;180": 90, "45°": 45, "forward": -1, "backward": -1, "both": -1,
                 "0-360": -1, "nan": -1, "": -1, None: -1}
        for value, deg in cases.items():
            self.assertEqual(deg, wb.direction(value), value)
        self.assertEqual(358, wb.direction("0", north=-2.3))  # true north in eastern Kentucky, on the grid
        self.assertEqual(-1, wb.direction("", north=-2.3))

    def test_signs_face_the_traffic_they_are_for(self):
        self.assertEqual(270, wb.facing(0, 0, 10, 0, True))  # traffic heading east meets a sign facing west
        self.assertEqual(90, wb.facing(0, 0, 10, 0, False))
        self.assertEqual(180, wb.facing(0, 0, 0, 10, True))
        self.assertEqual(225, wb.facing(10, 10, 0, 0, False))  # drawn south-west, for traffic heading north-east
        self.assertIs(True, wb.sign_travel({"highway": "stop", "direction": "forward"}, "stop"))
        self.assertIs(False, wb.sign_travel({"highway": "give_way", "direction": "backward"}, "give_way"))
        self.assertIs(False, wb.sign_travel({"traffic_signals:direction": "backward"}, "traffic_signals"))
        self.assertIsNone(wb.sign_travel({"direction": "both"}, "stop"))
        self.assertIsNone(wb.sign_travel({"direction": "forward"}, "bench"))

    def test_node_record(self):
        tags = {"amenity": "bench", "direction": "NE", "name": "Memorial bench"}
        record = wb.node_record(42, tags, "bench", 123, 456)
        self.assertEqual({"id": 42, "kind": "bench", "name": "Memorial bench", "x": 123, "z": 456, "dir": 45}, record)
        self.assertEqual(["id", "kind", "name", "x", "z", "dir"], list(record))
        self.assertEqual(-1, wb.node_record(7, {"natural": "tree"}, "tree", 0, 0)["dir"])
        self.assertEqual(91, wb.node_record(8, {"direction": "E"}, "bench", 0, 0, north=1.2)["dir"])


class Format2AreaTest(unittest.TestCase):
    def test_new_kinds(self):
        cases = [({"amenity": "school"}, "school"), ({"amenity": "college"}, "school"),
                 ({"amenity": "university"}, "school"), ({"landuse": "education"}, "school"),
                 ({"amenity": "hospital"}, "hospital"), ({"landuse": "religious"}, "churchyard"),
                 ({"amenity": "place_of_worship"}, "churchyard"), ({"landuse": "brownfield"}, "lot"),
                 ({"landuse": "greenfield"}, "lot"), ({"landuse": "construction"}, "lot"),
                 ({"highway": "pedestrian", "area": "yes"}, "plaza"), ({"place": "square"}, "plaza"),
                 ({"amenity": "fuel"}, "fuel"), ({"aeroway": "aerodrome"}, "airfield"),
                 ({"landuse": "military"}, "military"), ({"highway": "pedestrian"}, ""), ({"highway": "service"}, "")]
        for tags, kind in cases:
            self.assertEqual(kind, wb.area_kind(tags), tags)
        self.assertEqual("plaza", wb.area_kind({"highway": "pedestrian"}, from_way=False))  # a multipolygon

    def test_format_1_kinds_still_win(self):
        self.assertEqual("grass", wb.area_kind({"amenity": "school", "landuse": "grass"}))
        self.assertEqual("forest", wb.area_kind({"landuse": "military", "natural": "wood"}))
        self.assertEqual("retail", wb.area_kind({"amenity": "fuel", "landuse": "retail"}))
        self.assertEqual("cemetery", wb.area_kind({"amenity": "grave_yard", "landuse": "religious"}))
        self.assertEqual("airfield", wb.area_kind({"aeroway": "aerodrome", "landuse": "military"}))
        new = {"school", "hospital", "churchyard", "lot", "plaza", "fuel", "airfield", "military"}
        first_new = next(i for i, rule in enumerate(wb.AREA_KINDS) if rule[2] in new)
        self.assertTrue(all(rule[2] in new for rule in wb.AREA_KINDS[first_new:]))
        self.assertTrue(all(rule[2] not in new for rule in wb.AREA_KINDS[:first_new]))


class ManifestTest(unittest.TestCase):
    def test_region_entry(self):
        data = {"rx": 186, "rz": 142, "streets": [{}, {}], "lines": [], "buildings": [{}], "areas": [], "pois": [],
                "barriers": [{}], "power": [], "nodes": [{}, {}, {}]}
        self.assertEqual({"rx": 186, "rz": 142, "streets": 2, "lines": 0, "buildings": 1, "areas": 0, "pois": 0,
                          "barriers": 1, "power": 0, "nodes": 3}, wb.region_entry(data))
        self.assertEqual(0, wb.region_entry({"rx": 0, "rz": 0})["nodes"])

    def _publish(self, store: Path, build_id: str, part: dict, regions: list) -> dict:
        build = wb.BUILDS / build_id
        (build / "regions").mkdir(parents=True)
        for r in regions:
            (build / "regions" / f"{r['rx']}_{r['rz']}.json.gz").write_bytes(b"")
        wb.write_json(build / "parts" / "0-of-1.json", {**part, "regions": regions})
        with contextlib.redirect_stdout(io.StringIO()):
            wb.publish(argparse.Namespace(id=build_id))
        return json.loads((build / "manifest.json").read_text(encoding="utf-8"))

    def test_publish_counts_format_2_arrays(self):
        part = {"part": 0, "of": 1, "format": 2, "machine": "test", "finished": "now", "source": "ky.osm.pbf",
                "source_md5": "a", "tool_md5": "b"}
        regions = [{"rx": 1, "rz": 2, "streets": 3, "lines": 0, "buildings": 4, "areas": 1, "pois": 0, "barriers": 2,
                    "power": 1, "nodes": 5},
                   {"rx": 1, "rz": 3, "streets": 1, "lines": 1, "buildings": 0, "areas": 0, "pois": 2, "barriers": 0,
                    "power": 1, "nodes": 7}]
        with scratch_store() as store:
            manifest = self._publish(store, "f2", part, regions)
            current = json.loads(wb.CURRENT.read_text(encoding="utf-8"))
        self.assertEqual(2, manifest["format"])
        self.assertEqual({"streets": 4, "lines": 1, "buildings": 4, "areas": 1, "pois": 2, "barriers": 2, "power": 2,
                          "nodes": 12}, manifest["totals"])
        self.assertEqual(regions, manifest["regions"])
        self.assertEqual("f2", current["build"])

    def test_publish_still_takes_a_format_1_build(self):
        part = {"part": 0, "of": 1, "machine": "test", "finished": "now", "source": "ky.osm.pbf", "source_md5": "a",
                "tool_md5": "b"}
        regions = [{"rx": 1, "rz": 2, "streets": 3, "lines": 0, "buildings": 4, "areas": 1, "pois": 0}]
        with scratch_store() as store:
            manifest = self._publish(store, "f1", part, regions)
        self.assertEqual(1, manifest["format"])
        self.assertEqual((3, 0, 0, 0), tuple(manifest["totals"][k] for k in ("streets", "barriers", "power", "nodes")))


# A made-up corner of Bardstown: a street with a stop sign facing along it, a house, a fence, a tree row, a power
# line, a culvert, a school, a pedestrian loop and a square, a water tower drawn as its outline and point features.
LON, LAT = -85.4669, 37.8092
TINY_MAP_NODES = {  # id: (east, north) in 1/10,000 degree from (LON, LAT), tags
    1: (0, 0, {}), 2: (5, 0, {"highway": "stop", "direction": "forward"}), 3: (10, 0, {}),
    10: (2, 3, {}), 11: (4, 3, {}), 12: (4, 4, {}), 13: (2, 4, {}),
    20: (0, -3, {}), 21: (8, -3, {}), 22: (0, -5, {}), 23: (8, -5, {}), 24: (0, -8, {}), 25: (6, -8, {}),
    26: (12, -8, {}), 27: (-3, -10, {}), 28: (-3, 10, {}),
    30: (20, 0, {}), 31: (30, 0, {}), 32: (30, 10, {}), 33: (20, 10, {}),
    40: (40, 0, {}), 41: (44, 0, {}), 42: (44, 4, {}), 43: (40, 4, {}),
    44: (50, 0, {}), 45: (54, 0, {}), 46: (54, 4, {}), 47: (50, 4, {}),
    50: (60, 0, {}), 51: (61, 0, {}), 52: (61, 1, {}), 53: (60, 1, {}),
    60: (1, 1, {"natural": "tree"}), 61: (2, 1, {"emergency": "fire_hydrant"}),
    62: (3, 1, {"amenity": "bench", "direction": "NE"}),
    63: (4, 1, {"man_made": "tower", "tower:type": "communication"}), 64: (6, 1, {"highway": "street_lamp"}),
    65: (7, 1, {"amenity": "post_box", "name": "Bardstown Post Office"}), 66: (8, 1, {"amenity": "fuel"}),
    67: (9, 1, {"place": "town", "name": "Bardstown", "population": "13567"}),
    68: (9, 2, {"power": "pole"}),
}
TINY_MAP_WAYS = {  # id: (nodes, tags)
    100: ([1, 2, 3], {"highway": "residential", "name": "Third Street", "ref": "KY 245", "surface": "paved",
                      "sidewalk:left": "yes", "sidewalk:right": "separate", "lit": "yes"}),
    101: ([10, 11, 12, 13, 10], {"building": "house", "roof:shape": "gabled", "building:material": "plastic",
                                 "roof:colour": "red", "start_date": "1885"}),
    102: ([20, 21], {"barrier": "fence"}), 103: ([22, 23], {"natural": "tree_row"}),
    104: ([24, 25, 26], {"power": "minor_line"}),
    105: ([27, 28], {"waterway": "stream", "tunnel": "culvert", "layer": "-1"}),
    106: ([30, 31, 32, 33, 30], {"amenity": "school", "name": "Bardstown Elementary"}),
    107: ([40, 41, 42, 43, 40], {"highway": "pedestrian"}),
    108: ([44, 45, 46, 47, 44], {"highway": "pedestrian", "area": "yes"}),
    109: ([50, 51, 52, 53, 50], {"man_made": "water_tower", "name": "Bardstown Water Tower"}),
}


def tiny_map_xml() -> str:
    def tags(t: dict) -> str:
        return "".join(f'<tag k="{k}" v="{v}"/>' for k, v in t.items())
    lines = ['<?xml version="1.0" encoding="UTF-8"?>', '<osm version="0.6" generator="test">']
    for nid, (east, north, t) in TINY_MAP_NODES.items():
        lines.append(f'<node id="{nid}" version="1" lat="{LAT + north / 10000:.7f}" lon="{LON + east / 10000:.7f}">'
                     f'{tags(t)}</node>')
    for wid, (nodes, t) in TINY_MAP_WAYS.items():
        refs = "".join(f'<nd ref="{n}"/>' for n in nodes)
        lines.append(f'<way id="{wid}" version="1">{refs}{tags(t)}</way>')
    lines.append("</osm>")
    return "\n".join(lines)


@unittest.skipUnless(importlib.util.find_spec("osmium"), "needs pyosmium (uv run --with osmium ...)")
class ExtractTest(unittest.TestCase):
    def test_tiny_map_end_to_end(self):
        with scratch_store() as store:
            source = store / "tiny.osm"
            source.write_text(tiny_map_xml(), encoding="utf-8")
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(0, wb.main(["extract", "--id", "tiny", "--source", str(source)]))
                self.assertEqual(0, wb.main(["publish", "tiny"]))
            build = wb.BUILDS / "tiny"
            manifest = json.loads((build / "manifest.json").read_text(encoding="utf-8"))
            x, y = wb.KentuckySingleZone().forward(LON, LAT)
            name = f"{int(x // wb.REGION)}_{int(y // wb.REGION)}.json.gz"
            with gzip.open(build / "regions" / name, "rt", encoding="utf-8") as f:
                region = json.load(f)
            places = json.loads((build / "places.json").read_text(encoding="utf-8"))

        self.assertEqual(2, manifest["format"])
        self.assertEqual(1, len(manifest["regions"]))
        self.assertEqual({"streets": 2, "lines": 1, "buildings": 1, "areas": 2, "pois": 1, "barriers": 2, "power": 1,
                          "nodes": 8}, manifest["totals"])
        self.assertEqual(2, region["format"])
        self.assertEqual("Bardstown", places["places"][0]["name"])

        streets = {s["id"]: s for s in region["streets"]}
        self.assertEqual({100, 107}, set(streets))  # the closed pedestrian way is a street, the square isn't
        third = streets[100]
        self.assertEqual(("Third Street", "KY 245", "asphalt", "left", 1),
                         (third["name"], third["route"], third["surface"], third["sidewalk"], third["lit"]))

        (stream,) = region["lines"]
        self.assertEqual(("stream", False, True, -1), (stream["kind"], stream["bridge"], stream["tunnel"],
                                                       stream["layer"]))
        (house,) = region["buildings"]
        self.assertEqual(("house", "gabled", "vinyl", "", "#ff0000", 1885),
                         (house["kind"], house["roof"], house["material"], house["colour"], house["roofColour"],
                          house["year"]))
        self.assertEqual({(212, "school"), (216, "plaza")}, {(a["id"], a["kind"]) for a in region["areas"]})
        self.assertEqual({(102, "fence", 0.1), (103, "tree_row", 6)},
                         {(b["id"], b["kind"], b["width"]) for b in region["barriers"]})
        (power,) = region["power"]
        self.assertEqual((104, "minor_line", 2.4, 6), (power["id"], power["kind"], power["width"], len(power["pts"])))
        self.assertEqual([{"id": 66, "kind": "fuel"}], [{"id": p["id"], "kind": p["kind"]} for p in region["pois"]])

        nodes = {nd["id"]: nd for nd in region["nodes"]}
        self.assertEqual({2: "stop", 60: "tree", 61: "hydrant", 62: "bench", 63: "mast", 64: "street_lamp",
                          65: "post_box", 218: "water_tower"}, {i: nd["kind"] for i, nd in nodes.items()})
        for nd in region["nodes"]:
            self.assertEqual(["id", "kind", "name", "x", "z", "dir"], list(nd))
        self.assertAlmostEqual(270, nodes[2]["dir"], delta=1)  # for eastbound traffic, so it faces west
        self.assertEqual(45, nodes[62]["dir"])
        self.assertEqual(-1, nodes[60]["dir"])
        self.assertEqual("Bardstown Water Tower", nodes[218]["name"])
        # Each point sits where its node is, in decimetres from the region's corner.
        tree_x, tree_y = wb.KentuckySingleZone().forward(LON + 1 / 10000, LAT + 1 / 10000)
        rx, rz = region["rx"], region["rz"]
        self.assertEqual((round((tree_x - rx * wb.REGION) * 10), round((tree_y - rz * wb.REGION) * 10)),
                         (nodes[60]["x"], nodes[60]["z"]))


if __name__ == "__main__":
    unittest.main()
