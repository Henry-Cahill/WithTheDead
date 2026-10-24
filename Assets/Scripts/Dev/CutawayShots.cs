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
    /// the bardstown zone, once with every building batched for the counts before the cutaway, then with their shells),
    /// registers its buildings with <see cref="MapCutaway"/> as their sites would in play mode, and photographs the
    /// isometric view with the cutaway's scan run directly (CutawayShots.Pictures.cs): inside a house and behind, then
    /// inside, a two-storey block downtown at each of the camera's four headings, at a church, walking in through a
    /// house's door and out again, with the game's other cutaway (<see cref="IsoCutaway"/>) as well, and with no survivor.
    /// It times the scan while a survivor walks round town (roofs told by their heights, and by the old footprint test for
    /// comparison), checks the shells' meshes for NaN, times streamed pieces' build steps with the shells baked at once,
    /// queued on the piece's bake, or batched, and loads, cuts, unloads and reloads streamed Louisville pieces to check
    /// nothing is left behind (CutawayShots.Pieces.cs). Writes the pictures and report.md to the output folder.
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
            private readonly List<Interactable> _doors = new List<Interactable>();
            private float _t = 100f;
            private Counts _batched;

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
                        Stage("build the square batched (before)", BuildBatched);
                        if (Stage("build the square", () => BuildSquare(true)) && _world != null)
                        {
                            Stage("count the square", CountSquare);
                            Stage("register the shells", RegisterShells);
                            Stage("pictures", Pictures);
                            Stage("check the meshes", () => CheckMeshes("the square"));
                            Stage("walk in and out", Walk);
                            Stage("scan timing", Timing);
                        }
                        Stage("discard the square", DiscardSquare);
                        Stage("streamed pieces: build steps", ChunkTimings);
                        Stage("streamed pieces: load, cut, unload, reload", Pieces);
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
                    MapCutaway.OwnShells = true;
                    MapCutaway.UseHeights = true;
                    CutawayPass.Deferred = true;
                    Application.logMessageReceived -= OnLog;
                }
                if (code == ExitOk && _failures > 0) code = ExitHarness;
                if (code == ExitOk && _errors > 0) code = ExitMap;
                _md.Append("\n## Log\n\n").Append(_errors).Append(" errors from the cutaway or its checks, ").Append(_warnings).Append(" warnings logged during the run")
                    .Append(_firstErrors.Count > 0 ? ":\n\n" : ".\n");
                foreach (string e in _firstErrors) _md.Append("- `").Append(e).Append("`\n");
                if (_otherErrors > 0)
                {
                    _md.Append("\n").Append(_otherErrors).Append(" errors from other parts of the map (not this harness's to judge; listed for their owners):\n\n");
                    foreach (string e in _firstOther) _md.Append("- `").Append(e).Append("`\n");
                }
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

            /// <summary>A check that failed: counted as an error (exit 2) and said in the report.</summary>
            private void Fail(string what)
            {
                _errors++;
                if (_firstErrors.Count < 12) _firstErrors.Add("check failed: " + what);
                _md.Append("**Check failed: ").Append(what).Append("**\n\n");
            }

            private void OnLog(string message, string stack, LogType type)
            {
                if (message != null && message.StartsWith("[CutawayShots]", StringComparison.Ordinal)) return;
                if (type == LogType.Warning) _warnings++;
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                // The interiors' clean-up calls Destroy, which edit mode refuses: not the map's error, and not this one's.
                if (message != null && message.Contains("Destroy may not be called from edit mode")) return;
                string line = (message ?? "").Replace('\n', ' ').Replace('`', '\'');
                // What the cutaway's own code logs (or what's thrown through it) fails the run; the rest of the map's errors
                // are listed for its owners (the map harness reports them too).
                bool mine = line.Contains("MapCutaway") || line.Contains("CutawayPass") || (stack != null && (stack.Contains("MapCutaway") || stack.Contains("CutawayPass")));
                if (!mine)
                {
                    _otherErrors++;
                    if (_firstOther.Count < 8) _firstOther.Add(line);
                    return;
                }
                _errors++;
                if (_firstErrors.Count < 12) _firstErrors.Add(line);
            }

            private int _otherErrors;
            private readonly List<string> _firstOther = new List<string>();

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

            // The square with every building in the shared batches, as before the cutaway: only counted.
            private void BuildBatched()
            {
                MapCutaway.OwnShells = false;
                try
                {
                    BuildSquare(false);
                    if (_world != null) _batched = CountRoots();
                }
                finally
                {
                    MapCutaway.OwnShells = true;
                    DiscardSquare();
                }
            }

            private void BuildSquare(bool report)
            {
                var before = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
                _world = new GameObject("World").transform;
                List<Mesh> outer = RaidMesh.CreatedMeshes;
                RaidMesh.CreatedMeshes = _meshes;
                RaidMapInfo info;
                string why;
                long emitted = CutawayPass.Emitted, emitTicks = CutawayPass.EmitTicks;
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
                if (!report) return;
                _md.Append("KentuckyMap.Build: ").Append(F0(sw.Elapsed.TotalMilliseconds)).Append(" ms (").Append(CutawayPass.Emitted - emitted)
                    .Append(" shells emitted in ").Append(F1(Ms(CutawayPass.EmitTicks - emitTicks))).Append(" ms). ").Append(KentuckyMap.LastStats).Append("\n\n");
                var all = new List<Transform> { _world };
                foreach (GameObject go in _roots) all.Add(go.transform);
                CollectLights(all);
                // Edit mode never switches components on: the working doors join the live interactables by hand, so the
                // cutaway can find the ones in the walls it cuts.
                foreach (HomeDoor d in _world.GetComponentsInChildren<HomeDoor>(true))
                    if (!Interactable.All.Contains(d))
                    {
                        Interactable.All.Add(d);
                        _doors.Add(d);
                    }
            }

            private struct Counts
            {
                public int Objects, Renderers, Shells, Uppers;
                public long Vertices;
            }

            private Counts CountRoots()
            {
                var c = new Counts();
                var roots = new List<Transform> { _world };
                foreach (GameObject go in _roots) roots.Add(go.transform);
                foreach (Transform t in roots)
                {
                    Count(t, out int o, out int r, out long v);
                    c.Objects += o;
                    c.Renderers += r;
                    c.Vertices += v;
                    foreach (Transform s in t.GetComponentsInChildren<Transform>(true))
                    {
                        if (s.name != "Building shells") continue;
                        int n = s.GetComponentsInChildren<MeshRenderer>(true).Length;
                        if (t == _world) c.Shells += n;
                        else c.Uppers += n;
                    }
                }
                return c;
            }

            private void CountSquare()
            {
                _md.Append("## What the square is made of\n\n| root | objects | renderers | vertices | building walls (renderers) | upper parts (renderers) |\n|---|--:|--:|--:|--:|--:|\n");
                var roots = new List<Transform> { _world };
                foreach (GameObject go in _roots) roots.Add(go.transform);
                foreach (Transform t in roots)
                {
                    Count(t, out int o, out int r, out long v);
                    int shells = 0;
                    foreach (Transform c in t.GetComponentsInChildren<Transform>(true))
                        if (c.name == "Building shells") shells += c.GetComponentsInChildren<MeshRenderer>(true).Length;
                    bool overhead = t != _world;
                    _md.Append("| ").Append(t.name).Append(" | ").Append(F0(o)).Append(" | ").Append(F0(r)).Append(" | ").Append(F0(v)).Append(" | ")
                        .Append(overhead ? "" : F0(shells)).Append(" | ").Append(overhead ? F0(shells) : "").Append(" |\n");
                }
                Counts now = CountRoots();
                _md.Append("| **all** | ").Append(F0(now.Objects)).Append(" | ").Append(F0(now.Renderers)).Append(" | ").Append(F0(now.Vertices))
                    .Append(" | ").Append(F0(now.Shells)).Append(" | ").Append(F0(now.Uppers)).Append(" |\n");
                if (_batched.Objects > 0)
                    _md.Append("| *every building batched (before)* | ").Append(F0(_batched.Objects)).Append(" | ").Append(F0(_batched.Renderers)).Append(" | ")
                        .Append(F0(_batched.Vertices)).Append(" | 0 | 0 |\n");
                _md.Append('\n');
                if (_batched.Objects > 0)
                    _md.Append("Shells of their own cost ").Append(Signed(now.Objects - _batched.Objects)).Append(" objects and ")
                        .Append(Signed(now.Renderers - _batched.Renderers)).Append(" renderers over the same square batched (")
                        .Append(F0(now.Renderers - _batched.Renderers)).Append(" / ").Append(F0(now.Shells + now.Uppers)).Append(" shell renderers: ")
                        .Append("the shared batches they'd have joined mostly exist anyway).\n\n");
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
                int walls = 0, uppers = 0, upright = 0;
                foreach (MapCutaway.Building b in _buildings)
                {
                    if (b.Walls != null) walls++;
                    if (b.Upper != null) uppers++;
                    if (b.Top - b.WallTop > 2.5f) upright++;
                }
                _md.Append(_sites.Count).Append(" site(s), ").Append(MapCutaway.Registered).Append(" buildings with shells of their own registered (")
                    .Append(walls).Append(" with walls, ").Append(uppers).Append(" with an upper part, ").Append(upright)
                    .Append(" reaching 2.5 m or more above their ground storey's walls; ").Append(_doors.Count).Append(" working doors in the square).\n\n");
                if (MapCutaway.Registered != _buildings.Count) Fail("the registered count (" + MapCutaway.Registered + ") doesn't match the sites' lists (" + _buildings.Count + ")");
                // Read every building's meshes now (the game reads them a slice a frame round the survivor), timing each.
                int resolves = MapCutaway.Resolves;
                long bytes = MapCutaway.HeightBytes;
                double worst = 0, sum = 0;
                int clipped = 0;
                foreach (MapCutawaySite s in _sites)
                    foreach (MapCutaway.Building b in s.Buildings)
                    {
                        var sw = Stopwatch.StartNew();
                        MapCutaway.ResolveOne(b);
                        double ms = sw.Elapsed.TotalMilliseconds;
                        sum += ms;
                        worst = Math.Max(worst, ms);
                        if (b.ClipWalls) clipped++;
                    }
                int n = MapCutaway.Resolves - resolves;
                _md.Append("Reading their meshes (the game does it a slice of at most ").Append(F1(MapCutaway.WorkBudgetMs))
                    .Append(" ms a frame for the buildings round the survivor; here all at once): ").Append(n).Append(" roofs read, ")
                    .Append(F1(n > 0 ? sum / n : 0)).Append(" ms each on average, ").Append(F1(worst)).Append(" ms the longest, ")
                    .Append(F0(MapCutaway.HeightBytes - bytes)).Append(" bytes of heights in all. ").Append(clipped)
                    .Append(" buildings' walls reach above their ground storey's top (tall windows, linings): shown clipped while their upper part is lifted.\n\n");
                int unread = 0;
                foreach (MapCutaway.Building b in _buildings)
                    if (!b.Resolved) unread++;
                if (unread > 0) Fail(unread + " buildings' meshes couldn't be read");
            }

            private void DiscardSquare()
            {
                foreach (MapCutawaySite s in _sites)
                    if (s != null) s.Unregister();
                _sites.Clear();
                _buildings.Clear();
                foreach (Interactable d in _doors) Interactable.All.Remove(d);
                _doors.Clear();
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

            #region Checks and timing
            // Every shell mesh and every stand-in made so far, for NaN or infinite vertices, normals and bounds.
            private void CheckMeshes(string where)
            {
                int meshes = 0, bad = 0;
                var sb = new StringBuilder();
                var v = new List<Vector3>(4096);
                foreach (MapCutaway.Building b in _buildings)
                    for (int k = 0; k < 3; k++)
                    {
                        Mesh m = k == 0 ? b.WallsMesh : k == 1 ? b.UpperMesh : b.OverlayMesh;
                        if (m == null) continue;
                        meshes++;
                        string why = Invalid(m, v);
                        if (why == null) continue;
                        bad++;
                        if (bad <= 10) sb.Append("- `").Append(m.name).Append("` (").Append(b.Kind).Append(' ').Append(b.Id).Append("): ").Append(why).Append('\n');
                    }
                _md.Append("Meshes of ").Append(where).Append(": ").Append(meshes).Append(" shell meshes and stand-ins checked for NaN or infinite vertices, normals and bounds: ")
                    .Append(bad == 0 ? "all sound.\n\n" : "**" + bad + " invalid**:\n\n" + sb + "\n");
                if (bad > 0) Fail(bad + " shell meshes of " + where + " have invalid vertex data");
            }

            private static string Invalid(Mesh m, List<Vector3> v)
            {
                Bounds b = m.bounds;
                if (!Finite(b.center) || !Finite(b.extents)) return "bounds " + b;
                v.Clear();
                m.GetVertices(v);
                for (int i = 0; i < v.Count; i++)
                    if (!Finite(v[i])) return "vertex " + i + " is " + v[i];
                v.Clear();
                m.GetNormals(v);
                for (int i = 0; i < v.Count; i++)
                    if (!Finite(v[i])) return "normal " + i + " is " + v[i];
                return null;
            }

            private static bool Finite(Vector3 p) => !(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)
                || float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z));

            // The game's frames while the stand-in walks a circle through the middle of town (a cutaway scan every sixth
            // frame, its work every frame): a first lap reads the roofs and makes the stand-ins round the path as the game
            // would; then two laps time the scans and check them for allocations, with the roofs told by their heights and by
            // their footprint up to their top (the old test), and time the frames with their work.
            private void Timing()
            {
                const int scans = 3000, framesPerScan = 6;
                int keep = MapCutaway.MaxOverlays;
                float evict = MapCutaway.EvictRadius;
                MapCutaway.MaxOverlays = 100000;   // the timed laps find the first lap's stand-ins (eviction is timed in the walk)
                MapCutaway.EvictRadius = 1e6f;
                try
                {
                    MapCutaway.DropCaches();
                    float now = _t += 1000f;
                    LapStats warm = Lap(scans, framesPerScan, ref now);
                    _md.Append("## Scan timing\n\n").Append(scans).Append(" scans while the stand-in walks a 70 m circle round the middle of the square, ")
                        .Append("the camera turning a quarter every 750, ").Append(framesPerScan).Append(" frames of the cutaway's work after each, after a first ")
                        .Append("lap that read the roofs and made the stand-ins round the path as it went (its worst frame, scan and work: ").Append(F2(warm.WorstMs))
                        .Append(" ms):\n\n| roofs told by | µs a scan | bytes the scans allocated | µs a frame (scan and work) | worst frame ms ")
                        .Append("| buildings cut on average | at most | upper parts cut on average |\n|---|--:|--:|--:|--:|--:|--:|--:|\n");
                    for (int mode = 0; mode < 2; mode++)
                    {
                        MapCutaway.UseHeights = mode == 0;
                        MapCutaway.RestoreAll();
                        LapStats s = Lap(scans, framesPerScan, ref now);
                        _md.Append("| ").Append(mode == 0 ? "their heights (now)" : "footprint up to the top (before)").Append(" | ")
                            .Append(s.ScanUs.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ").Append(F0(s.ScanBytes)).Append(" | ")
                            .Append(s.FrameUs.ToString("0.0", CultureInfo.InvariantCulture)).Append(" | ").Append(F2(s.WorstMs)).Append(" | ")
                            .Append(s.AvgCut.ToString("0.00", CultureInfo.InvariantCulture)).Append(" | ").Append(s.MaxCut).Append(" | ")
                            .Append(s.AvgUpper.ToString("0.00", CultureInfo.InvariantCulture)).Append(" |\n");
                        if (s.ScanBytes > 4096) Fail("the scan allocates (" + s.ScanBytes + " bytes over " + scans + " scans)");
                    }
                }
                finally
                {
                    MapCutaway.MaxOverlays = keep;
                    MapCutaway.EvictRadius = evict;
                    MapCutaway.UseHeights = true;
                    MapCutaway.RestoreAll();
                }
                _md.Append("\nStand-ins made so far: ").Append(MapCutaway.OverlaysMade).Append(" (").Append(F1(MapCutaway.OverlaysMade > 0 ? MapCutaway.OverlayMs / MapCutaway.OverlaysMade : 0))
                    .Append(" ms each on average in all, the most one took ").Append(F1(MapCutaway.OverlayWholeMaxMs)).Append(" ms, made in slices of at most ")
                    .Append(F2(MapCutaway.OverlaySliceMaxMs)).Append(" ms); ").Append(MapCutaway.OverlaysEvicted).Append(" let go of. Roofs read in slices of at most ")
                    .Append(F2(MapCutaway.ResolveSliceMaxMs)).Append(" ms. Cuts held back for a stand-in: ").Append(MapCutaway.Holds).Append(", the longest ")
                    .Append(MapCutaway.LongestHold).Append(" frame(s). The longest frame's work: ").Append(F2(MapCutaway.WorkMaxMs)).Append(" ms. Door renderers hidden with their walls: ")
                    .Append(MapCutaway.DoorsHidden).Append(".\n\n");
            }

            private struct LapStats
            {
                public double ScanUs, FrameUs, WorstMs, AvgCut, AvgUpper;
                public long ScanBytes;
                public int MaxCut;
            }

            // One lap of the 70 m circle: a scan per step and `frames` frames of the cutaway's work after it.
            private LapStats Lap(int scans, int frames, ref float now)
            {
                var s = new LapStats();
                var frame = new Stopwatch();
                long scanTicks = 0, frameTicks = 0, sumCut = 0, sumUpper = 0;
                for (int i = 0; i < scans; i++)
                {
                    float a = i * Mathf.PI * 2f / scans;
                    var p = new Vector3(Mathf.Cos(a) * 70f, 0f, Mathf.Sin(a) * 70f);
                    now += MapCutaway.ScanInterval;
                    long bytes = GC.GetAllocatedBytesForCurrentThread();
                    frame.Restart();
                    MapCutaway.Run(p, _isoYaw + (i / 750) * 90f, _isoPitch, Reach(Wide), true, now, false);
                    scanTicks += frame.ElapsedTicks;
                    s.ScanBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
                    for (int f = 0; f < frames; f++)
                    {
                        if (f > 0) frame.Restart();
                        MapCutaway.KeepDoors();
                        MapCutaway.Work(MapCutaway.WorkBudgetMs);
                        frameTicks += frame.ElapsedTicks;
                        s.WorstMs = Math.Max(s.WorstMs, frame.Elapsed.TotalMilliseconds);
                    }
                    int cut = MapCutaway.CutBuildings;
                    s.MaxCut = Mathf.Max(s.MaxCut, cut);
                    sumCut += cut;
                    IReadOnlyList<MapCutaway.Building> list = MapCutaway.Cut;
                    for (int k = 0; k < list.Count; k++)
                        if ((list[k].Applied & 16) != 0) sumUpper++;
                }
                double tickUs = 1e6 / Stopwatch.Frequency;
                s.ScanUs = scanTicks * tickUs / scans;
                s.FrameUs = frames > 0 ? frameTicks * tickUs / (scans * frames) : s.ScanUs;
                s.AvgCut = (double)sumCut / scans;
                s.AvgUpper = (double)sumUpper / scans;
                return s;
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

            private static float EdgeDistance(Vector2[] p, Vector2 q) => EdgeDistance(p, q, out _);

            private static float EdgeDistance(Vector2[] p, Vector2 q, out int edge)
            {
                float best = float.MaxValue;
                edge = 0;
                for (int i = 0; i < p.Length; i++)
                {
                    Vector2 a = p[i], ab = p[(i + 1) % p.Length] - a;
                    float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                    float d = Vector2.Distance(q, a + ab * t);
                    if (d >= best) continue;
                    best = d;
                    edge = i;
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

            private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

            private static string F0(double v) => v.ToString("N0", CultureInfo.InvariantCulture);

            private static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

            private static string F2(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);

            private static string Signed(int v) => (v >= 0 ? "+" : "") + v.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
#endif
