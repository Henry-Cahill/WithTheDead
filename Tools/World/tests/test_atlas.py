"""Tests for the browser atlas (Tools/World/atlas): its vocabulary against the game's and the builder's, its projection
against PROJ, the check files it keeps, and its server. No world store needed:
    python -m unittest discover -s Tools/World/tests -v
"""
from __future__ import annotations

import gzip
import http.client
import importlib.util
import json
import re
import shutil
import subprocess
import sys
import tempfile
import threading
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
ATLAS_DIR = HERE.parent / "atlas"
MAP_CODE = ROOT / "Assets" / "Scripts" / "World" / "Map"


def _load(name: str, path: Path):
    """The module at path (the one already loaded under that name, if another test file loaded it)."""
    if name in sys.modules:
        return sys.modules[name]
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


atlas = _load("atlas", ATLAS_DIR / "atlas.py")
wb = _load("worldbuild", HERE.parent / "worldbuild.py")
VOCAB = json.loads((ATLAS_DIR / "web" / "vocab.json").read_text(encoding="utf-8"))


def csharp_enum(path: Path, name: str) -> list:
    body = re.search(r"enum " + name + r"\s*\{([^}]*)\}", path.read_text(encoding="utf-8")).group(1)
    return [m for m in re.findall(r"\b([A-Z]\w*)\b", body)]


def csharp_strings(path: Path, field: str) -> list:
    body = re.search(field + r"\s*=\s*\{([^}]*)\}", path.read_text(encoding="utf-8")).group(1)
    return re.findall(r'"([^"]*)"', body)


def make_store(root: Path) -> Path:
    """A tiny store: one build, one region (0_0) with two buildings."""
    build = root / "world" / "builds" / "test-1"
    (build / "regions").mkdir(parents=True)
    (root / "world" / "current.json").write_text(json.dumps({"build": "test-1", "published": "2026-10-31T10:00:00-05:00"}))
    (build / "manifest.json").write_text(json.dumps({"format": 2, "id": "test-1", "region": 8192, "cell": 256,
                                                     "regions": [{"rx": 0, "rz": 0, "buildings": 2}]}))
    (build / "places.json").write_text(json.dumps({"format": 1, "places": [{"id": 1, "kind": "town", "name": "Testville",
                                                                            "population": 10, "x": 100.0, "z": 100.0}]}))
    region = {"format": 2, "rx": 0, "rz": 0, "size": 8192, "buildings": [
        {"id": 20, "kind": "house", "name": "", "address": "1 Main St", "use": "", "levels": 1, "height": 0,
         "pts": [0, 0, 100, 0, 100, 100, 0, 100]},
        {"id": 31, "kind": "church", "name": "First Baptist", "address": "", "use": "place_of_worship", "levels": 1,
         "height": 0, "pts": [200, 200, 400, 200, 400, 500, 200, 500]}]}
    with gzip.open(build / "regions" / "0_0.json.gz", "wt", encoding="utf-8") as f:
        json.dump(region, f)
    return root


def record(bid: int = 20, **checks) -> dict:
    return {"id": bid, "checks": checks, "note": "", "fix": {}}


class VocabularyTest(unittest.TestCase):
    """The page offers what the game and the builder know, and nothing else."""

    def test_kinds_are_the_builders(self):
        self.assertEqual(sorted(set(wb.BUILDING_KINDS.values())), sorted(VOCAB["kinds"]))
        self.assertEqual(VOCAB["kinds"], csharp_strings(MAP_CODE / "MapValidation.cs", "Kinds"))

    def test_archetypes_and_styles_are_the_building_pass(self):
        plan = MAP_CODE / "BuildingPass.Plan.cs"
        self.assertEqual(csharp_enum(plan, "Arch"), VOCAB["archetypes"])
        self.assertEqual([s for s in csharp_enum(plan, "HouseStyle") if s != "None"], VOCAB["styles"])

    def test_lists_match_map_validation(self):
        code = MAP_CODE / "MapValidation.cs"
        for field, key in (("Roofs", "roofs"), ("Materials", "materials"), ("Facings", "facings")):
            self.assertEqual(csharp_strings(code, field), VOCAB[key], field)
        self.assertEqual(csharp_strings(code, "CheckNames"), [c[0] for c in VOCAB["checks"]])
        self.assertEqual(list(atlas.CHECKS), [c[0] for c in VOCAB["checks"]])

    def test_roofs_and_materials_are_the_builders(self):
        self.assertLessEqual(set(VOCAB["roofs"]), set(wb.ROOF_SHAPES.values()))   # its half-hipped and dome are hip and round
        self.assertEqual(set(wb.MATERIALS.values()), set(VOCAB["materials"]))


@unittest.skipUnless(shutil.which("node"), "node isn't installed")
class ProjectionTest(unittest.TestCase):
    """web/ky3088.js projects as PROJ (and the builder) does, both ways."""

    def test_matches_proj(self):
        script = """
const p = require(process.argv[1]);
const out = [];
for (const [lon, lat, x, y] of JSON.parse(process.argv[2])) out.push([p.toGrid(lon, lat), p.toLonLat(x, y)]);
console.log(JSON.stringify(out));
"""
        refs = [(-83.1932, 37.2498, 1726801.91, 1104820.33), (-85.7585, 38.2527, 1499256.05, 1213017.99),
                (-88.6000, 37.0834, 1246633.47, 1087115.50), (-82.6382, 38.4784, 1771470.81, 1242596.48)]
        out = json.loads(subprocess.run(["node", "-e", script, str(ATLAS_DIR / "web" / "ky3088.js"), json.dumps(refs)],
                                        capture_output=True, text=True, check=True).stdout)
        for (lon, lat, x, y), ((gx, gy), (plon, plat)) in zip(refs, out):
            self.assertAlmostEqual(x, gx, delta=0.01)
            self.assertAlmostEqual(y, gy, delta=0.01)
            self.assertAlmostEqual(lon, plon, delta=1e-6)
            self.assertAlmostEqual(lat, plat, delta=1e-6)


class ChecksTest(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="scratch-", dir=HERE)
        self.store = make_store(Path(self.tmp.name))
        self.atlas = atlas.Atlas(self.store, who="web@test")

    def tearDown(self):
        self.tmp.cleanup()

    def saved(self) -> dict:
        return json.loads((self.store / "world" / "validation" / "regions" / "0_0.json").read_text(encoding="utf-8"))

    def test_saves_a_whole_record(self):
        rec = self.atlas.save(0, 0, {**record(20, footprint="ok", kind="bad"), "base": "",
                                     "fix": {"kind": "apartments", "storeys": 2, "facing": "S"}})
        self.assertEqual("web@test", rec["by"])
        self.assertEqual("test-1", rec["build"])
        data = self.saved()
        self.assertEqual(1, data["format"])
        (saved,) = data["buildings"]
        self.assertEqual(["id", "checks", "note", "fix", "game", "build", "by", "updated"], list(saved))
        self.assertEqual({"footprint": "ok", "kind": "bad", "outside": "", "windows": "", "doors": "", "inside": ""}, saved["checks"])
        self.assertEqual({"hide": False, "kind": "apartments", "use": "", "name": "", "arch": "", "style": "", "roof": "",
                          "material": "", "facing": "S", "storeys": 2}, saved["fix"])
        self.assertEqual(atlas.blank_game(), saved["game"])
        self.assertEqual("bad", atlas.status(saved))

    def test_keeps_what_the_editor_saw(self):
        game = {**atlas.blank_game(), "seen": "2026-10-31T09:00:00-05:00", "arch": "House", "doors": 2}
        path = self.store / "world" / "validation" / "regions" / "0_0.json"
        path.parent.mkdir(parents=True)
        path.write_text(json.dumps({"format": 1, "rx": 0, "rz": 0, "buildings": [
            {"id": 20, "checks": {"outside": "ok"}, "note": "", "fix": {}, "game": game, "build": "test-1", "by": "unity@PC",
             "updated": "2026-10-31T09:00:00-05:00"}]}))
        self.atlas.save(0, 0, {**record(20, footprint="ok"), "base": "2026-10-31T09:00:00-05:00"})
        (saved,) = self.saved()["buildings"]
        self.assertEqual(game, saved["game"])
        self.assertEqual("", saved["checks"]["outside"])   # the page sends every check it shows

    def test_conflict_when_saved_meanwhile(self):
        self.atlas.save(0, 0, {**record(20, footprint="ok"), "base": ""})
        with self.assertRaises(atlas.Conflict) as caught:
            self.atlas.save(0, 0, {**record(20, footprint="bad"), "base": ""})
        self.assertEqual("ok", caught.exception.current["checks"]["footprint"])
        self.atlas.save(0, 0, record(20, footprint="bad"))   # no base: last one wins
        self.assertEqual("bad", self.saved()["buildings"][0]["checks"]["footprint"])

    def test_an_empty_record_is_dropped(self):
        self.atlas.save(0, 0, record(20, footprint="ok"))
        self.atlas.save(0, 0, record(31, kind="ok"))
        self.assertIsNone(self.atlas.save(0, 0, record(20)))
        self.assertEqual([31], [r["id"] for r in self.saved()["buildings"]])

    def test_refuses_what_the_game_doesnt_know(self):
        for bad in (record(20, footprint="maybe"), {**record(20), "fix": {"kind": "castle"}},
                    {**record(20), "fix": {"arch": "Igloo"}}, {**record(20), "fix": {"storeys": 41}},
                    {**record(20), "fix": {"hide": "yes"}}, {**record(True)}, {**record(20), "checks": ["ok"]},
                    {**record(20), "note": "x" * 2001}):
            with self.subTest(bad=bad), self.assertRaises(ValueError):
                self.atlas.save(0, 0, bad)
        self.assertFalse((self.store / "world" / "validation").exists())

    def test_never_writes_over_a_file_it_cant_read(self):
        path = self.store / "world" / "validation" / "regions" / "0_0.json"
        path.parent.mkdir(parents=True)
        path.write_text("{ not json")
        with self.assertRaises(ValueError):
            self.atlas.save(0, 0, record(20, footprint="ok"))
        self.assertEqual("{ not json", path.read_text())

    def test_summary(self):
        self.atlas.save(0, 0, record(20, footprint="ok", kind="ok", outside="ok", windows="na", doors="ok", inside="ok"))
        self.atlas.save(0, 0, {**record(31, footprint="ok"), "fix": {"hide": True}})
        self.assertEqual([[0, 0, 2, 1, 1, 0, 1]], self.atlas.summary())


class ServerTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory(prefix="scratch-", dir=HERE)
        cls.store = make_store(Path(cls.tmp.name))
        cls.server = atlas.make_server(atlas.Atlas(cls.store, who="web@test"), "127.0.0.1", 0)
        cls.thread = threading.Thread(target=cls.server.serve_forever, daemon=True)
        cls.thread.start()

    @classmethod
    def tearDownClass(cls):
        cls.server.shutdown()
        cls.server.server_close()
        cls.tmp.cleanup()

    def request(self, method: str, path: str, body=None, headers=None):
        conn = http.client.HTTPConnection("127.0.0.1", self.server.server_port, timeout=10)
        data = json.dumps(body).encode() if body is not None and not isinstance(body, bytes) else body
        conn.request(method, path, body=data, headers=headers or {})
        res = conn.getresponse()
        raw = res.read()
        conn.close()
        return res, raw

    def test_current_and_page(self):
        res, raw = self.request("GET", "/api/current")
        self.assertEqual(200, res.status)
        self.assertEqual({"build": "test-1", "regions": [[0, 0, 2]]}, {k: v for k, v in json.loads(raw).items() if k in ("build", "regions")})
        res, raw = self.request("GET", "/")
        self.assertEqual(200, res.status)
        self.assertIn(b"Kentucky Atlas", raw)
        for path in ("/atlas.js", "/ky3088.js", "/atlas.css", "/vocab.json"):
            self.assertEqual(200, self.request("GET", path)[0].status, path)

    def test_region_goes_out_zipped_as_it_is(self):
        res, raw = self.request("GET", "/api/region/0_0?build=test-1")
        self.assertEqual(200, res.status)
        self.assertEqual("gzip", res.getheader("Content-Encoding"))
        self.assertEqual([20, 31], [b["id"] for b in json.loads(gzip.decompress(raw))["buildings"]])
        self.assertEqual(404, self.request("GET", "/api/region/5_5")[0].status)

    def test_nothing_outside_the_page_and_the_store(self):
        for path in ("/../atlas.py", "/api/region/../../current", "/%2e%2e/atlas.py", "/api/checks/0_0/../../x", "/atlas.py"):
            self.assertEqual(404, self.request("GET", path)[0].status, path)

    def test_save_through_the_server(self):
        json_headers = {"Content-Type": "application/json"}
        res, raw = self.request("POST", "/api/checks/0_0", {**record(31, kind="ok"), "base": ""}, json_headers)
        self.assertEqual(200, res.status, raw)
        self.assertEqual("ok", json.loads(raw)["record"]["checks"]["kind"])
        res, raw = self.request("GET", "/api/checks/0_0")
        self.assertEqual([31], [r["id"] for r in json.loads(raw)["buildings"]])
        res, raw = self.request("POST", "/api/checks/0_0", {**record(31, kind="bad"), "base": ""}, json_headers)
        self.assertEqual(409, res.status)
        self.assertEqual("ok", json.loads(raw)["current"]["checks"]["kind"])
        self.assertEqual(400, self.request("POST", "/api/checks/0_0", record(31, kind="nope"), json_headers)[0].status)

    def test_only_this_page_may_save(self):
        body = record(20, footprint="ok")
        self.assertEqual(415, self.request("POST", "/api/checks/0_0", body, {"Content-Type": "text/plain"})[0].status)
        self.assertEqual(403, self.request("POST", "/api/checks/0_0", body, {"Content-Type": "application/json",
                                                                            "Origin": "http://evil.example"})[0].status)
        self.assertEqual(403, self.request("GET", "/api/current", headers={"Host": "evil.example"})[0].status)
        self.assertEqual(413, self.request("POST", "/api/checks/0_0", b"x" * (atlas.MAX_BODY + 1),
                                           {"Content-Type": "application/json"})[0].status)


if __name__ == "__main__":
    unittest.main()
