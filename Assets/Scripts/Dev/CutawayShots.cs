#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloneZ
{
    /// <summary>
    /// The isometric cutaway's harness, behind <c>CloneZ.EditorTools.CutawayShots.Run</c>. The map harness builds in edit
    /// mode, where nothing runs, so it never shows the cutaway; this builds the start square the same way (a fixed copy of
    /// the bardstown zone), registers its buildings with <see cref="MapCutaway"/> as their sites would in play mode, stands
    /// a survivor stand-in inside a house and behind (then inside) the tallest building downtown, runs the cutaway's scan
    /// directly and photographs the isometric view each time (and once with no survivor); walks the stand-in into the
    /// house and out again a tenth of a second at a time to log when parts are cut and come back; times the scan and
    /// counts what it allocates; and builds four streamed Louisville pieces to check that their buildings register, cut
    /// and go cleanly with their pieces. Writes the pictures and report.md to the output folder.
    /// </summary>
    public static partial class CutawayShots
    {
        public const int ExitOk = 0, ExitHarness = 1, ExitMap = 2, ExitNoWorld = 3;

        /// <summary>Runs the harness; returns the process exit code.</summary>
        public static int Run(string outDir, string place, VolumeProfile profile) => new Harness(outDir, place, profile).Execute();

        private sealed partial class Harness
        {
            private readonly string _out, _placeName;
            private readonly VolumeProfile _profile;
            private readonly StringBuilder _md = new StringBuilder();
            private readonly StringBuilder _log = new StringBuilder();
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private int _errors, _warnings, _failures;
            private readonly List<string> _firstErrors = new List<string>();

            private ZoneDef _zone;
            private MapBuild _build;
            private MapPlace _place;
            private Transform _world;
            private readonly List<GameObject> _roots = new List<GameObject>();
            private readonly List<Mesh> _meshes = new List<Mesh>();
            private readonly List<MapCutawaySite> _sites = new List<MapCutawaySite>();
            private readonly List<MapCutaway.Building> _buildings = new List<MapCutaway.Building>();
            private float _t = 100f;

            public Harness(string outDir, string place, VolumeProfile profile)
            {
                _out = string.IsNullOrEmpty(outDir) ? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "CutawayShots") : outDir;
                _placeName = place;
                _profile = profile;
            }

            public int Execute()
            {
                int code = ExitOk;
                Application.logMessageReceived += OnLog;
                try
                {
                    Directory.CreateDirectory(_out);
                    _md.Append("# CLONE-Z isometric cutaway harness\n\n");
                    if (!Stage("setup", Setup)) code = ExitNoWorld;
                    else
                    {
                        if (Stage("build the square", BuildSquare) && _world != null)
                        {
                            Stage("count the square", CountSquare);
                            Stage("register the shells", RegisterShells);
                            Stage("pictures", Pictures);
                            Stage("walk in and out", Walk);
                            Stage("scan timing", Timing);
                        }
                        Stage("discard the square", DiscardSquare);
                        Stage("streamed pieces", Pieces);
                    }
                }
                catch (Exception e)
                {
                    _failures++;
                    Debug.LogError("[CutawayShots] the harness failed: " + e);
                }
                finally
                {
                    try { MapCutaway.Clear(); DiscardSquare(); DropRig(); }
                    catch (Exception e) { _failures++; Debug.LogError("[CutawayShots] cleaning up failed: " + e); }
                    Application.logMessageReceived -= OnLog;
                }
                if (code == ExitOk && _failures > 0) code = ExitHarness;
                if (code == ExitOk && _errors > 0) code = ExitMap;
                _md.Append("\n## Log\n\n").Append(_errors).Append(" errors, ").Append(_warnings).Append(" warnings logged during the run")
                    .Append(_firstErrors.Count > 0 ? ":\n\n" : ".\n");
                foreach (string e in _firstErrors) _md.Append("- `").Append(e).Append("`\n");
                _md.Append("\n## Stages\n\n").Append(_log);
                _md.Append("\n**Exit ").Append(code).Append("** after ").Append(F1(_clock.Elapsed.TotalSeconds)).Append(" s.\n");
                try { File.WriteAllText(Path.Combine(_out, "report.md"), _md.ToString()); }
                catch (Exception e) { Debug.LogError("[CutawayShots] couldn't write the report: " + e); }
                return code;
            }

            private bool Stage(string name, Action act)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    act();
                    _log.Append("- ").Append(name).Append(": ok, ").Append(F0(sw.Elapsed.TotalMilliseconds)).Append(" ms\n");
                    return true;
                }
                catch (Exception e)
                {
                    _failures++;
                    _log.Append("- ").Append(name).Append(": **FAILED** ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n');
                    Debug.LogError("[CutawayShots] stage '" + name + "' failed: " + e);
                    return false;
                }
            }

            private void Note(string s)
            {
                Debug.Log("[CutawayShots] " + s);
            }

            private void OnLog(string message, string stack, LogType type)
            {
                if (message != null && message.StartsWith("[CutawayShots]", StringComparison.Ordinal)) return;
                if (type == LogType.Warning) _warnings++;
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                _errors++;
                if (_firstErrors.Count < 12) _firstErrors.Add((message ?? "").Replace('\n', ' ').Replace('`', '\''));
            }

            #region The square
            private void Setup()
            {
                _zone = StartZone(_placeName);
                _build = WorldStore.OpenCurrent(out string why);
                if (_build == null) throw new Exception("the world store at " + WorldStore.Root + " can't be read: " + why);
                _place = _build.FindPlace(_zone.RealPlace);
                if (_place == null) throw new Exception("build " + _build.Id + " has no place called " + _zone.RealPlace);
                Climate.Winter = _zone.Winter;
                Shelter.Clear();
                StarterHome.SetActive(false);
                ViewState.Current = CameraView.Isometric;
                ReadCamera();
                BuildRig();
                _md.Append("Build ").Append(_build.Id).Append(", ").Append(_place.name).Append(" (fixed square, as the map harness builds it). ")
                    .Append("Unity ").Append(Application.unityVersion).Append(", ").Append(SystemInfo.graphicsDeviceType).Append(". ")
                    .Append("Camera: isometric, yaw ").Append(_isoYaw).Append(", pitch ").Append(_isoPitch).Append(". ")
                    .Append("Map data © OpenStreetMap contributors (ODbL).\n\n");
            }

            // A copy of the bardstown zone, fixed rather than statewide (the square and its scenery ring, built at once).
            private static ZoneDef StartZone(string place)
            {
                ZoneDef src = null;
                foreach (ZoneDef z in ZoneCatalog.All)
                    if (z.Id == "bardstown") { src = z; break; }
                var copy = new ZoneDef();
                if (src != null)
                {
                    foreach (FieldInfo f in typeof(ZoneDef).GetFields(BindingFlags.Public | BindingFlags.Instance))
                        if (!f.IsInitOnly) f.SetValue(copy, f.GetValue(src));
                }
                else
                {
                    copy.Id = "bardstown";
                    copy.RealPlace = "Bardstown";
                    copy.LayoutSeed = 1780;
                }
                copy.RealStatewide = false;
                if (!string.IsNullOrEmpty(place)) copy.RealPlace = place;
                return copy;
            }

            private void BuildSquare()
            {
                var before = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
                _world = new GameObject("World").transform;
                List<Mesh> outer = RaidMesh.CreatedMeshes;
                RaidMesh.CreatedMeshes = _meshes;
                RaidMapInfo info;
                string why;
                var sw = Stopwatch.StartNew();
                try { info = KentuckyMap.Build(_world, _zone, out why); }
                finally { RaidMesh.CreatedMeshes = outer; }
                foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!before.Contains(go) && go != _world.gameObject) _roots.Add(go);
                if (info == null)
                {
                    _errors++;
                    _md.Append("**KentuckyMap.Build failed**: ").Append(why).Append("\n\n");
                    Object.DestroyImmediate(_world.gameObject);
                    _world = null;
                    return;
                }
                _md.Append("KentuckyMap.Build: ").Append(F0(sw.Elapsed.TotalMilliseconds)).Append(" ms. ").Append(KentuckyMap.LastStats).Append("\n\n");
                var all = new List<Transform> { _world };
                foreach (GameObject go in _roots) all.Add(go.transform);
                CollectLights(all);
            }

            private void CountSquare()
            {
                _md.Append("## What the square is made of\n\n| root | objects | renderers | vertices | building walls (renderers) | upper parts (renderers) |\n|---|--:|--:|--:|--:|--:|\n");
                var roots = new List<Transform> { _world };
                foreach (GameObject go in _roots) roots.Add(go.transform);
                int objects = 0, renderers = 0;
                long verts = 0;
                foreach (Transform t in roots)
                {
                    Count(t, out int o, out int r, out long v);
                    int shells = 0;
                    foreach (Transform c in t.GetComponentsInChildren<Transform>(true))
                        if (c.name == "Building shells") shells += c.GetComponentsInChildren<MeshRenderer>(true).Length;
                    bool overhead = t != _world;
                    _md.Append("| ").Append(t.name).Append(" | ").Append(F0(o)).Append(" | ").Append(F0(r)).Append(" | ").Append(F0(v)).Append(" | ")
                        .Append(overhead ? "" : F0(shells)).Append(" | ").Append(overhead ? F0(shells) : "").Append(" |\n");
                    objects += o;
                    renderers += r;
                    verts += v;
                }
                _md.Append("| **all** | ").Append(F0(objects)).Append(" | ").Append(F0(renderers)).Append(" | ").Append(F0(verts)).Append(" | | |\n\n");
                _md.Append("Phase A for comparison (everything batched, nothing cut): World 3,126 objects / 1,051 renderers, RaidOverhead 221 / 220, ")
                    .Append("RaidScenery 72 / 71, all 3,419 / 1,342, 731,346 vertices.\n\n");
            }

            private static void Count(Transform root, out int objects, out int renderers, out long vertices)
            {
                objects = root.GetComponentsInChildren<Transform>(true).Length;
                MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
                renderers = root.GetComponentsInChildren<Renderer>(true).Length;
                vertices = 0;
                foreach (MeshFilter f in filters)
                    if (f.sharedMesh != null) vertices += f.sharedMesh.vertexCount;
            }

            private void RegisterShells()
            {
                foreach (MapCutawaySite s in _world.GetComponentsInChildren<MapCutawaySite>(true))
                {
                    _sites.Add(s);
                    s.Register();
                    if (s.Buildings != null) _buildings.AddRange(s.Buildings);
                }
                int walls = 0, uppers = 0, roofs = 0;
                foreach (MapCutaway.Building b in _buildings)
                {
                    if (b.Walls != null) walls++;
                    if (b.Upper != null) uppers++;
                    if (b.LocalRoof != null) roofs++;
                }
                _md.Append(_sites.Count).Append(" site(s), ").Append(MapCutaway.Registered).Append(" buildings with shells of their own registered (")
                    .Append(walls).Append(" with walls, ").Append(uppers).Append(" with an upper part, ").Append(roofs)
                    .Append(" whose roof reaches well past their walls).\n\n");
                if (MapCutaway.Registered != _buildings.Count)
                {
                    _errors++;
                    _md.Append("**Registered count doesn't match the sites' lists** (").Append(_buildings.Count).Append(").\n\n");
                }
            }

            private void DiscardSquare()
            {
                foreach (MapCutawaySite s in _sites)
                    if (s != null) s.Unregister();
                _sites.Clear();
                _buildings.Clear();
                foreach (GameObject go in _roots)
                    if (go != null) Object.DestroyImmediate(go);
                _roots.Clear();
                if (_world != null) Object.DestroyImmediate(_world.gameObject);
                _world = null;
                foreach (Mesh m in _meshes)
                    if (m != null) Object.DestroyImmediate(m);
                _meshes.Clear();
                CeilingVisibility.Clear();
                Shelter.Clear();
            }
            #endregion

            #region Pictures
            private void Pictures()
            {
                MapCutaway.Building house = PickHouse(), tall = PickTall();
                _md.Append("## Pictures\n\nEach picture is the game's isometric camera at its widest zoom (")
                    .Append(Wide).Append(" m half-height) unless noted, in plain overcast daylight; the red capsule stands in for the survivor. ")
                    .Append("\"roofs on\" = what the view showed before the cutaway; \"cut\" = after one scan.\n\n| file | shows | cut away |\n|---|---|---|\n");
                if (tall != null)
                    Shot("01_downtown_no_survivor.png", "Downtown with no survivor: every roof and upper storey shows", Centre(tall), _isoYaw, Wide, false, false, false);
                if (house != null)
                {
                    Vector3 inside = Inside(house);
                    string what = Describe(house);
                    Shot("02_house_roofs_on.png", "Inside " + what + ", roofs on (before)", inside, _isoYaw, Wide, true, false, false);
                    Shot("03_house_cut.png", "Inside " + what + ": its roof lifted, its walls turned to the camera cut to stubs", inside, _isoYaw, Wide, true, true, false);
                    Shot("04_house_cut_close.png", "The same, closest zoom", inside, _isoYaw, Close, true, true, false);
                    Shot("05_house_cut_turned.png", "The same with the camera turned 90 degrees (the other walls go)", inside, _isoYaw + 90f, Wide, true, true, false);
                    Shot("06_house_cut_night.png", "The same by night (the map's lights on round the survivor)", inside, _isoYaw, Wide, true, true, true);
                }
                else _md.Append("| | no house with a shell of its own found | |\n");
                if (tall != null)
                {
                    string what = Describe(tall);
                    Vector3? behind = BehindOf(tall, _isoYaw);
                    if (behind.HasValue)
                    {
                        Shot("07_behind_roofs_on.png", "Behind " + what + " (as the camera sees it), roofs on (before)", behind.Value, _isoYaw, Wide, true, false, false);
                        Shot("08_behind_cut.png", "Behind " + what + ": what stands in the way is cut", behind.Value, _isoYaw, Wide, true, true, false);
                    }
                    else _md.Append("| | no free spot found behind ").Append(what).Append(" | |\n");
                    Shot("09_downtown_inside_cut.png", "Inside " + what, Inside(tall), _isoYaw, Wide, true, true, false);
                }
                MapCutaway.RestoreAll();
                _md.Append('\n');
            }

            private void Shot(string file, string title, Vector3 at, float yaw, float size, bool survivor, bool cut, bool night)
            {
                MapCutaway.RestoreAll();
                string cutList = "";
                if (cut)
                {
                    MapCutaway.Run(at, yaw, _isoPitch, Reach(size), true, _t += 10f, true);
                    cutList = CutList();
                }
                try
                {
                    Picture(file, at, yaw, size, survivor, night);
                    _md.Append("| `").Append(file).Append("` | ").Append(title).Append(" | ").Append(cutList).Append(" |\n");
                }
                catch (Exception e)
                {
                    _failures++;
                    _md.Append("| `").Append(file).Append("` | **failed**: ").Append(e.Message).Append(" | |\n");
                    Debug.LogError("[CutawayShots] picture " + file + " failed: " + e);
                }
            }

            private static string CutList()
            {
                var sb = new StringBuilder();
                IReadOnlyList<MapCutaway.Building> cut = MapCutaway.Cut;
                for (int i = 0; i < cut.Count; i++)
                {
                    if (sb.Length > 0) sb.Append("; ");
                    sb.Append(cut[i].Kind).Append(' ').Append(cut[i].Id).Append(": ").Append(Parts(cut[i].Applied));
                    if (cut[i].Overlay != null) sb.Append(" (stand-in ").Append(cut[i].OverlayMesh.subMeshCount).Append(" parts)");
                }
                return sb.Length > 0 ? sb.ToString() : "nothing";
            }

            private static string Parts(int mask)
            {
                var sb = new StringBuilder();
                if ((mask & 16) != 0) sb.Append("upper");
                if ((mask & 15) == 0) return sb.ToString();
                sb.Append(sb.Length > 0 ? " + walls " : "walls ");
                string names = "NESW";
                for (int g = 0; g < 4; g++)
                    if ((mask & (1 << g)) != 0) sb.Append(names[g]);
                return sb.ToString();
            }

            private static string Describe(MapCutaway.Building b)
                => b.Kind + " " + b.Id + " (" + F0(Area(b.Local)) + " m², walls " + F1(b.WallTop) + " m, top " + F1(b.Top) + " m)";
            #endregion

            #region Picking spots
            // The house nearest the middle of the square that's big enough to read (a shell of its own, walls and roof).
            private MapCutaway.Building PickHouse()
            {
                MapCutaway.Building best = null;
                float bestD = float.MaxValue;
                foreach (MapCutaway.Building b in _buildings)
                {
                    if (b.Walls == null || b.Upper == null || b.Kind != "house") continue;
                    float area = Area(b.Local);
                    if (area < 70f || area > 400f) continue;
                    float d = b.Middle.sqrMagnitude;
                    if (d < bestD) { bestD = d; best = b; }
                }
                return best;
            }

            // The tallest building other than a home within 160 m of the middle (the biggest of equals).
            private MapCutaway.Building PickTall()
            {
                MapCutaway.Building best = null;
                float bestScore = float.MinValue;
                foreach (MapCutaway.Building b in _buildings)
                {
                    if (b.Walls == null || b.Upper == null || b.Kind == "house" || b.Kind == "trailer" || b.Middle.magnitude > 160f) continue;
                    float score = b.Top * 1000f + Mathf.Min(999f, Area(b.Local));
                    if (score > bestScore) { bestScore = score; best = b; }
                }
                return best;
            }

            private static Vector3 Centre(MapCutaway.Building b) => new Vector3(b.Pts != null ? Mid(b.Pts).x : b.Middle.x, 0f, b.Pts != null ? Mid(b.Pts).y : b.Middle.y);

            // A point well inside the footprint: its middle if that's inside, else the inside point farthest from the walls
            // on a half-metre grid.
            private static Vector3 Inside(MapCutaway.Building b)
            {
                Vector2[] p = b.Pts;
                Vector2 mid = Mid(p);
                Vector2 best = mid;
                float bestD = Contains(p, mid) ? EdgeDistance(p, mid) : -1f;
                if (bestD < 1.2f)
                {
                    Rect r = BoundsOf(p);
                    for (float x = r.xMin; x <= r.xMax; x += 0.5f)
                        for (float z = r.yMin; z <= r.yMax; z += 0.5f)
                        {
                            var q = new Vector2(x, z);
                            if (!Contains(p, q)) continue;
                            float d = EdgeDistance(p, q);
                            if (d > bestD) { bestD = d; best = q; }
                        }
                }
                return new Vector3(best.x, 0f, best.y);
            }

            // Just past the building's far side as the camera looks (so it stands between them), on open ground.
            private Vector3? BehindOf(MapCutaway.Building b, float yaw)
            {
                var look = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
                var side = new Vector2(look.y, -look.x);
                Vector2 mid = Mid(b.Pts);
                foreach (float lateral in new[] { 0f, 2f, -2f, 4f, -4f, 6f, -6f })
                {
                    Vector2 q = mid + side * lateral;
                    if (!Contains(b.Pts, q)) continue;
                    float t = 0f;
                    while (t < 80f && Contains(b.Pts, q + look * t)) t += 0.2f;
                    for (float extra = 1.1f; extra < 3.5f; extra += 0.3f)
                    {
                        Vector2 p = q + look * (t + extra);
                        if (!InAnyBuilding(p)) return new Vector3(p.x, 0f, p.y);
                    }
                }
                return null;
            }

            private bool InAnyBuilding(Vector2 p)
            {
                foreach (MapCutaway.Building b in _buildings)
                    if (b.Pts != null && BoundsOf(b.Pts).Contains(p) && Contains(b.Pts, p)) return true;
                return false;
            }
            #endregion

            #region Walking in and out, timing
            // The stand-in walks from 5 m outside the house (on the camera's side) to its middle at a walking pace, waits,
            // and walks back out, a frame every tenth of a second: when is the house cut, and when does it come back?
            private void Walk()
            {
                MapCutaway.Building house = PickHouse();
                if (house == null)
                {
                    _md.Append("## Walking in and out\n\nNo house to walk into.\n\n");
                    return;
                }
                Vector3 inside = Inside(house);
                var mid = new Vector2(inside.x, inside.z);
                var toCamera = new Vector2(-Mathf.Sin(_isoYaw * Mathf.Deg2Rad), -Mathf.Cos(_isoYaw * Mathf.Deg2Rad));
                float t = 0f;
                while (t < 60f && Contains(house.Pts, mid + toCamera * t)) t += 0.1f;
                Vector2 outside = mid + toCamera * (t + 5f);
                MapCutaway.RestoreAll();
                _md.Append("## Walking in and out\n\nThe stand-in walks at 1.4 m/s from 5 m outside ").Append(Describe(house))
                    .Append(" (toward the camera) to its middle, waits 1 s and walks back out; one cutaway frame every 0.1 s. Changes of the house's cut:\n\n")
                    .Append("| t (s) | stand-in | house cut |\n|--:|---|---|\n");
                const float step = 0.1f, speed = 1.4f;
                float now = _t += 100f, start = now;
                int last = -1;
                bool wasInside = false;
                float enteredAt = -1f, cutAt = -1f, leftAt = -1f, restoredAt = -1f;
                var path = new List<Vector2>();
                float len = Vector2.Distance(outside, mid);
                for (float d = 0f; d <= len; d += speed * step) path.Add(Vector2.Lerp(outside, mid, d / len));
                for (int i = 0; i < 10; i++) path.Add(mid);
                for (float d = len; d >= 0f; d -= speed * step) path.Add(Vector2.Lerp(outside, mid, d / len));
                for (int i = 0; i < 10; i++) path.Add(outside);
                foreach (Vector2 p in path)
                {
                    now += step;
                    MapCutaway.Run(new Vector3(p.x, 0f, p.y), _isoYaw, _isoPitch, Reach(Wide), true, now, false);
                    bool inHouse = Contains(house.Pts, p);
                    if (inHouse && !wasInside && enteredAt < 0f) enteredAt = now - start;
                    if (!inHouse && wasInside && leftAt < 0f) leftAt = now - start;
                    wasInside = inHouse;
                    if (house.Applied == last) continue;
                    if (house.Applied != 0 && cutAt < 0f) cutAt = now - start;
                    if (house.Applied == 0 && leftAt >= 0f && restoredAt < 0f) restoredAt = now - start;
                    last = house.Applied;
                    _md.Append("| ").Append(F1(now - start)).Append(" | ").Append(inHouse ? "inside" : F1(EdgeDistance(house.Pts, p)) + " m outside")
                        .Append(" | ").Append(house.Applied == 0 ? "nothing" : Parts(house.Applied)).Append(" |\n");
                }
                _md.Append("\nEntered at ").Append(F1(enteredAt)).Append(" s, cut at ").Append(F1(cutAt)).Append(" s; left at ").Append(F1(leftAt))
                    .Append(" s, all back at ").Append(F1(restoredAt)).Append(" s (restore delay ").Append(F1(MapCutaway.RestoreDelay)).Append(" s).\n\n");
                if (cutAt < 0f || restoredAt < 0f) _errors++;
                MapCutaway.RestoreAll();
            }

            // A scan a frame while the stand-in walks a circle through the middle of town: how long, and what it allocates
            // once every building it passes has its cut walls made.
            private void Timing()
            {
                MapCutaway.RestoreAll();
                const int frames = 3000;
                float now = _t += 100f;
                for (int pass = 0; pass < 2; pass++)
                {
                    long bytes = GC.GetAllocatedBytesForCurrentThread();
                    var sw = Stopwatch.StartNew();
                    int maxCut = 0;
                    for (int i = 0; i < frames; i++)
                    {
                        float a = i * Mathf.PI * 2f / frames;
                        var p = new Vector3(Mathf.Cos(a) * 70f, 0f, Mathf.Sin(a) * 70f);
                        now += MapCutaway.ScanInterval;
                        MapCutaway.Run(p, _isoYaw + (i / 750) * 90f, _isoPitch, Reach(Wide), true, now, false);
                        maxCut = Mathf.Max(maxCut, MapCutaway.CutBuildings);
                    }
                    double us = sw.Elapsed.TotalMilliseconds * 1000.0 / frames;
                    long alloc = GC.GetAllocatedBytesForCurrentThread() - bytes;
                    if (pass == 0) continue;
                    _md.Append("## Scan timing\n\n").Append(frames).Append(" scans (one a frame) while the stand-in walks a 70 m circle round the ")
                        .Append("middle of the square, the camera turning a quarter every 750: **").Append(us.ToString("0.0", CultureInfo.InvariantCulture))
                        .Append(" µs** a scan (cutting and restoring included), ").Append(F0(alloc)).Append(" bytes allocated over the run (after a ")
                        .Append("first lap that made the cut walls), at most ").Append(maxCut).Append(" buildings cut at once.\n\n");
                    if (alloc > 4096) _md.Append("**The scan allocates.**\n\n");
                }
                _md.Append("Cut walls' stand-ins made so far (each the first time its building's walls are cut): ").Append(MapCutaway.OverlaysMade)
                    .Append(", ").Append(F1(MapCutaway.OverlaysMade > 0 ? MapCutaway.OverlayMs / MapCutaway.OverlaysMade : 0)).Append(" ms each on average, ")
                    .Append(F1(MapCutaway.OverlayMaxMs)).Append(" ms the longest.\n\n");
                MapCutaway.RestoreAll();
            }
            #endregion

            #region Streamed pieces
            // Four streamed pieces of Louisville, built as KentuckyStreamer builds them: their buildings register when the
            // pieces are switched on, one inside them is cut, and they all go with their pieces (here unregistered by hand:
            // nothing runs OnDisable in edit mode).
            private void Pieces()
            {
                MapPlace lou = _build.FindPlace("Louisville");
                _md.Append("## Streamed pieces\n\n");
                if (lou == null)
                {
                    _md.Append("No Louisville in this build.\n\n");
                    return;
                }
                MapCutaway.Clear();
                const int size = KentuckyMap.ChunkSize;
                int cx0 = (int)Math.Floor(lou.x / size), cz0 = (int)Math.Floor(lou.z / size);
                var streamed = new GameObject("Streamed").transform;
                var overhead = new GameObject("RaidOverhead").transform;
                var pieces = new List<(GameObject root, GameObject over, List<Mesh> meshes, MapCutawaySite site)>();
                var regions = new Dictionary<long, MapRegionIndex>();
                try
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int cx = cx0 + (i & 1), cz = cz0 + (i >> 1);
                        var middle = new Vector3((float)((cx + 0.5) * size - lou.x), 0f, (float)((cz + 0.5) * size - lou.z));
                        pieces.Add(BuildPiece(lou, cx, cz, middle, streamed, overhead, regions));
                    }
                    int registered = 0;
                    var mine = new List<MapCutaway.Building>();
                    foreach (var p in pieces)
                    {
                        if (p.site == null) continue;
                        p.site.Register();
                        registered += p.site.Buildings.Count;
                        mine.AddRange(p.site.Buildings);
                    }
                    _md.Append("Built pieces ").Append(cx0).Append('_').Append(cz0).Append(" to ").Append(cx0 + 1).Append('_').Append(cz0 + 1)
                        .Append(" (Full detail): ").Append(registered).Append(" buildings registered (").Append(MapCutaway.Registered).Append(" in the cutaway).\n\n");
                    if (registered != MapCutaway.Registered) _errors++;

                    // The biggest building of the first piece: stand inside it, cut, photograph.
                    MapCutaway.Building big = null;
                    if (pieces[0].site != null)
                        foreach (MapCutaway.Building b in pieces[0].site.Buildings)
                            if (b.Walls != null && b.Upper != null && (big == null || Area(b.Local) > Area(big.Local))) big = b;
                    if (big != null)
                    {
                        _buildings.Clear();
                        _buildings.AddRange(mine);
                        Vector3 at = Inside(big);
                        CollectLights(new[] { streamed, overhead });
                        Shot("10_louisville_inside_cut.png", "Inside " + Describe(big) + " in streamed piece " + cx0 + "_" + cz0, at, _isoYaw, Wide, true, true, false);
                        int cutBefore = MapCutaway.CutBuildings;
                        // Let the piece go while it's cut, as the streamer would.
                        var first = pieces[0];
                        first.site.Unregister();
                        UnloadPiece(first);
                        pieces[0] = (null, null, null, null);
                        bool clean = MapCutaway.Registered == registered - first.site.Buildings.Count && !StillCut(first.site.Buildings);
                        MapCutaway.Run(at, _isoYaw, _isoPitch, Reach(Wide), true, _t += 10f, true);
                        _md.Append("Cut inside it: ").Append(cutBefore).Append(" buildings. Its piece let go of while cut: ").Append(MapCutaway.Registered)
                            .Append(" still registered, ").Append(clean ? "nothing of it left cut or registered" : "**something of it was left behind**")
                            .Append("; a scan on the empty spot afterwards cut ").Append(MapCutaway.CutBuildings).Append(" buildings.\n\n");
                        if (!clean) _errors++;
                    }
                }
                finally
                {
                    foreach (var p in pieces)
                    {
                        if (p.site != null) p.site.Unregister();
                        UnloadPiece(p);
                    }
                    if (MapCutaway.Registered != 0)
                    {
                        _errors++;
                        _md.Append("**").Append(MapCutaway.Registered).Append(" buildings still registered after every piece went.**\n\n");
                    }
                    else _md.Append("Every piece let go of: nothing left registered.\n\n");
                    Object.DestroyImmediate(streamed.gameObject);
                    Object.DestroyImmediate(overhead.gameObject);
                    _buildings.Clear();
                }
            }

            private static bool StillCut(List<MapCutaway.Building> list)
            {
                foreach (MapCutaway.Building b in list)
                    if (b.Applied != 0 || b.Active || b.Site != null) return true;
                return false;
            }

            // KentuckyStreamer's Begin, Steps and Finish for one piece (no loot roll, no NavMesh).
            private (GameObject root, GameObject over, List<Mesh> meshes, MapCutawaySite site) BuildPiece(MapPlace place, int cx, int cz,
                Vector3 middle, Transform streamed, Transform overhead, Dictionary<long, MapRegionIndex> regions)
            {
                const int size = KentuckyMap.ChunkSize;
                int per = _build.RegionSize / size;
                int rx = cx >= 0 ? cx / per : -((-cx + per - 1) / per), rz = cz >= 0 ? cz / per : -((-cz + per - 1) / per);
                long key = ((long)rx << 32) | (uint)rz;
                if (!regions.TryGetValue(key, out MapRegionIndex region))
                    regions[key] = region = new MapRegionIndex(_build.HasRegion(rx, rz) ? _build.LoadRegion(rx, rz) : null, rx, rz, _build.RegionSize, size);
                string name = "Piece " + cx + "_" + cz;
                var root = new GameObject(name);
                root.SetActive(false);
                root.transform.SetParent(streamed, false);
                var over = new GameObject(name);
                over.SetActive(false);
                over.transform.SetParent(overhead, false);
                var meshes = new List<Mesh>();
                var job = new KentuckyMap.ChunkJob
                {
                    Build = _build, Region = region, Place = place.name, Cx = cx, Cz = cz, Seed = unchecked(_zone.LayoutSeed * 486187739 + cx * 73856093 ^ cz * 19349663),
                    Root = root.transform, Overhead = over.transform, Offset = middle, Detail = KentuckyMap.Detail.Full,
                };
                List<Mesh> outerMeshes = RaidMesh.CreatedMeshes;
                List<string> outerSigns = DecorTextures.SignsUsed;
                Vector3 outerUv = RaidMesh.PlanarOrigin;
                object owner = Shelter.Owner;
                Func<string, bool> gate = ItemDatabase.LootGate;
                Action<string, int> rolled = ItemDatabase.LootRolled;
                RaidMesh.CreatedMeshes = meshes;
                DecorTextures.SignsUsed = new List<string>();
                RaidMesh.PlanarOrigin = new Vector3((float)Wrap((cx + 0.5) * size), 0f, (float)Wrap((cz + 0.5) * size));
                Shelter.Owner = root;
                ItemDatabase.LootGate = null;
                ItemDatabase.LootRolled = null;
                try
                {
                    foreach (string _ in KentuckyMap.ChunkSteps(job)) { }
                }
                finally
                {
                    RaidMesh.CreatedMeshes = outerMeshes;
                    DecorTextures.SignsUsed = outerSigns;
                    RaidMesh.PlanarOrigin = outerUv;
                    Shelter.Owner = owner;
                    ItemDatabase.LootGate = gate;
                    ItemDatabase.LootRolled = rolled;
                }
                root.transform.position = middle;
                over.transform.position = middle;
                root.SetActive(true);
                over.SetActive(true);
                return (root, over, meshes, root.GetComponentInChildren<MapCutawaySite>(true));
            }

            private static void UnloadPiece((GameObject root, GameObject over, List<Mesh> meshes, MapCutawaySite site) p)
            {
                if (p.root != null)
                {
                    Shelter.Forget(p.root);
                    Object.DestroyImmediate(p.root);
                }
                if (p.over != null) Object.DestroyImmediate(p.over);
                if (p.meshes == null) return;
                foreach (Mesh m in p.meshes)
                    if (m != null) Object.DestroyImmediate(m);
                p.meshes.Clear();
            }

            private static double Wrap(double v)
            {
                double r = v % RaidMesh.PlanarPeriod;
                return r < 0 ? r + RaidMesh.PlanarPeriod : r;
            }
            #endregion

            #region Plane geometry
            private static Vector2 Mid(Vector2[] p)
            {
                Vector2 s = Vector2.zero;
                foreach (Vector2 q in p) s += q;
                return p.Length > 0 ? s / p.Length : s;
            }

            private static float Area(Vector2[] p)
            {
                float a = 0f;
                for (int i = 0, j = p.Length - 1; i < p.Length; j = i++) a += p[j].x * p[i].y - p[i].x * p[j].y;
                return Mathf.Abs(a) * 0.5f;
            }

            private static bool Contains(Vector2[] p, Vector2 q)
            {
                bool inside = false;
                for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                    if ((p[i].y > q.y) != (p[j].y > q.y) && q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                        inside = !inside;
                return inside;
            }

            private static float EdgeDistance(Vector2[] p, Vector2 q)
            {
                float best = float.MaxValue;
                for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                {
                    Vector2 a = p[j], ab = p[i] - a;
                    float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                    best = Mathf.Min(best, Vector2.Distance(q, a + ab * t));
                }
                return best;
            }

            private static Rect BoundsOf(Vector2[] p)
            {
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
                foreach (Vector2 q in p)
                {
                    x0 = Mathf.Min(x0, q.x);
                    z0 = Mathf.Min(z0, q.y);
                    x1 = Mathf.Max(x1, q.x);
                    z1 = Mathf.Max(z1, q.y);
                }
                return Rect.MinMaxRect(x0, z0, x1, z1);
            }
            #endregion

            private static string F0(double v) => v.ToString("N0", CultureInfo.InvariantCulture);

            private static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
#endif
