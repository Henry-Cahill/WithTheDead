"""Tests for the world builder's geometry and tag rules (no OpenStreetMap file needed):
    python -m unittest discover -s Tools/World/tests -v
"""
from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path

_spec = importlib.util.spec_from_file_location("worldbuild", Path(__file__).resolve().parent.parent / "worldbuild.py")
wb = importlib.util.module_from_spec(_spec)
sys.modules["worldbuild"] = wb
_spec.loader.exec_module(wb)


class ProjectionTest(unittest.TestCase):
    def test_matches_epsg_3088(self):
        # Reference values from PROJ (pyproj, EPSG:4326 -> EPSG:3088).
        proj = wb.KentuckySingleZone()
        for lon, lat, x, y in ((-83.1932, 37.2498, 1726801.91, 1104820.33), (-85.7585, 38.2527, 1499256.05, 1213017.99),
                               (-88.6000, 37.0834, 1246633.47, 1087115.50), (-82.6382, 38.4784, 1771470.81, 1242596.48)):
            px, py = proj.forward(lon, lat)
            self.assertAlmostEqual(x, px, delta=0.01)
            self.assertAlmostEqual(y, py, delta=0.01)


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


if __name__ == "__main__":
    unittest.main()
