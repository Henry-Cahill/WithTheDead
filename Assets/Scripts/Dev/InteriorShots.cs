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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloneZ
{
    /// <summary>
    /// The interiors' harness, behind <c>CloneZ.EditorTools.InteriorShots.Run</c>. Builds the fixed square of a few towns
    /// (a copy of the bardstown zone, as the map harness does; every building furnished as it's built), records each
    /// building's interior (what it was used as, its rooms, loot containers, lights, objects, build time, failures), and
    /// photographs one building of each use close up, in its own axes from straight above with roofs and ceilings off, and
    /// from the game's isometric camera with the survivor stand-in inside it and the cutaway run (ceilings hidden as the
    /// game hides them), a few by night too. Then streams a piece of the start town as the statewide map does: checks that
    /// its build furnishes nothing, lets the <see cref="InteriorStreamer"/> furnish it round a survivor frame by frame
    /// (timing every frame), loots a building, walks away and back, and checks what was taken stayed taken; rebuilds the
    /// piece and checks the building comes out the same. Writes the pictures and report.md to the output folder.
    /// </summary>
    public static class InteriorShots
    {
        public const int ExitOk = 0, ExitHarness = 1, ExitMap = 2, ExitNoWorld = 3;

        /// <summary>Runs the harness on <paramref name="places"/> (comma separated); returns the process exit code.</summary>
        public static int Run(string outDir, string places, bool quick, VolumeProfile profile) => new Harness(outDir, places, quick, profile).Execute();

        // One furnished building, as recorded when the square was built.
        private sealed class Rec
        {
            public InteriorBuild Build;
            public InteriorPlan Plan;
            public long Id;
            public string Place, Use, Purpose, Kind, DataUse, Rooms, Error, Troubles;
            public float Area, Angle, Fill;
            public int Verts, RoomCount, Containers, Allotted, Stripped, Lights, Lit, CeilingLamps, Objects, Bodies, Steps;
            /// <summary>Its meshes' vertices (the interior's root and its overhead root).</summary>
            public long Vertices;
            /// <summary>Its renderers and their material slots, and the pieces' renderers merged into rooms' meshes.</summary>
            public int Renderers, Slots, Merged;
            /// <summary>Pieces kept out of the rooms' merged meshes, by the component that kept them.</summary>
            public Dictionary<string, int> Unmerged;
            public double TotalMs, MaxStepMs;
            public string MaxStep;
            /// <summary>Each room's role and how long it took to furnish (ms).</summary>
            public readonly List<KeyValuePair<string, double>> RoomTimes = new List<KeyValuePair<string, double>>();
        }

        private sealed class Harness
        {
            private readonly string _out;
            private readonly List<string> _places = new List<string>();
            private readonly bool _quick;
            private readonly VolumeProfile _profile;
            private readonly StringBuilder _md = new StringBuilder();
            private readonly StringBuilder _stages = new StringBuilder();
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private int _errors, _warnings, _failures;
            private readonly List<string> _firstErrors = new List<string>();

            private MapBuild _build;
            private ZoneDef _baseZone;
            private Transform _world;
            private readonly List<GameObject> _roots = new List<GameObject>();
            private readonly List<Mesh> _meshes = new List<Mesh>();
            private readonly List<MapCutawaySite> _sites = new List<MapCutawaySite>();
            private readonly List<Rec> _all = new List<Rec>();
            private readonly HashSet<string> _pictured = new HashSet<string>();
            private readonly List<string> _pictures = new List<string>();
            private int _shot, _housesPictured, _nights;
            private float _t = 100f;

            public Harness(string outDir, string places, bool quick, VolumeProfile profile)
            {
                _out = string.IsNullOrEmpty(outDir) ? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "InteriorShots") : outDir;
                foreach (string p in (string.IsNullOrEmpty(places) ? (quick ? "Bardstown,Princeton" : "Bardstown,Princeton,Pikeville,Middlesboro") : places).Split(','))
                    if (p.Trim().Length > 0) _places.Add(p.Trim());
                _quick = quick;
                _profile = profile;
            }

            public int Execute()
            {
                int code = ExitOk;
                Application.logMessageReceived += OnLog;
                try
                {
                    Directory.CreateDirectory(_out);
                    _md.Append("# CLONE-Z interiors harness\n\n");
                    if (!Stage("setup", Setup)) code = ExitNoWorld;
                    else
                    {
                        foreach (string place in _places)
                        {
                            string p = place;
                            Stage("square: " + p, () => Square(p));
                            Stage("discard: " + p, DiscardSquare);
                        }
                        Stage("summary by use", Summary);
                        Stage("streamed pieces", Streamed);
                    }
                }
                catch (Exception e)
                {
                    _failures++;
                    Debug.LogError("[InteriorShots] the harness failed: " + e);
                }
                finally
                {
                    try
                    {
                        InteriorPass.Furnished = null;
                        InteriorStreamer.ToolMode = false;
                        MapCutaway.Clear();
                        DiscardSquare();
                        DropRig();
                    }
                    catch (Exception e)
                    {
                        _failures++;
                        Debug.LogError("[InteriorShots] cleaning up failed: " + e);
                    }
                    Application.logMessageReceived -= OnLog;
                }
                if (code == ExitOk && _failures > 0) code = ExitHarness;
                if (code == ExitOk && _errors > 0) code = ExitMap;
                _md.Append("\n## Log\n\n").Append(_errors).Append(" errors, ").Append(_warnings).Append(" warnings logged during the run")
                    .Append(_firstErrors.Count > 0 ? ":\n\n" : ".\n");
                foreach (string e in _firstErrors) _md.Append("- `").Append(e).Append("`\n");
                _md.Append("\n## Stages\n\n").Append(_stages);
                _md.Append("\n**Exit ").Append(code).Append("** after ").Append(F1(_clock.Elapsed.TotalSeconds)).Append(" s.\n");
                try { File.WriteAllText(Path.Combine(_out, "report.md"), _md.ToString()); }
                catch (Exception e) { Debug.LogError("[InteriorShots] couldn't write the report: " + e); }
                return code;
            }

            private bool Stage(string name, Action act)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    act();
                    _stages.Append("- ").Append(name).Append(": ok, ").Append(F0(sw.Elapsed.TotalMilliseconds)).Append(" ms\n");
                    return true;
                }
                catch (Exception e)
                {
                    _failures++;
                    _stages.Append("- ").Append(name).Append(": **FAILED** ").Append(e.GetType().Name).Append(": ").Append(Md(e.Message)).Append('\n');
                    Debug.LogError("[InteriorShots] stage '" + name + "' failed: " + e);
                    return false;
                }
            }

            private void OnLog(string message, string stack, LogType type)
            {
                if (message != null && message.StartsWith("[InteriorShots]", StringComparison.Ordinal)) return;
                if (type == LogType.Warning) _warnings++;
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
                _errors++;
                if (_firstErrors.Count < 16) _firstErrors.Add(Md((message ?? "") + (string.IsNullOrEmpty(stack) ? "" : " @ " + FirstLine(stack))));
            }

            #region Setup
            private void Setup()
            {
                _build = WorldStore.OpenCurrent(out string why);
                if (_build == null) throw new Exception("the world store at " + WorldStore.Root + " can't be read: " + why);
                foreach (ZoneDef z in ZoneCatalog.All)
                    if (z.Id == "bardstown") { _baseZone = z; break; }
                if (_baseZone != null) Climate.Winter = _baseZone.Winter;
                Shelter.Clear();
                StarterHome.SetActive(false);
                ViewState.Current = CameraView.Isometric;
                ReadCamera();
                BuildRig();
                _md.Append("Build ").Append(_build.Id).Append(". Unity ").Append(Application.unityVersion).Append(", ").Append(SystemInfo.graphicsDeviceType)
                    .Append(". Places: ").Append(string.Join(", ", _places)).Append(_quick ? " (quick)" : "").Append(". Edit mode: nothing wakes, so the live ")
                    .Append("kitchen sink is drawn plain and the ceiling fittings show their bare bulbs, as the isometric view has them. ")
                    .Append("Map data © OpenStreetMap contributors (ODbL).\n\n");
            }

            // A copy of the bardstown zone, fixed rather than statewide, at another place.
            private ZoneDef ZoneFor(string place)
            {
                var copy = new ZoneDef();
                if (_baseZone != null)
                {
                    foreach (FieldInfo f in typeof(ZoneDef).GetFields(BindingFlags.Public | BindingFlags.Instance))
                        if (!f.IsInitOnly) f.SetValue(copy, f.GetValue(_baseZone));
                }
                else
                {
                    copy.Id = "bardstown";
                    copy.LayoutSeed = 1780;
                }
                copy.RealStatewide = false;
                copy.RealPlace = place;
                return copy;
            }
            #endregion

            #region The square
            private void Square(string placeName)
            {
                ZoneDef zone = ZoneFor(placeName);
                if (_build.FindPlace(placeName) == null)
                {
                    _md.Append("## ").Append(placeName).Append("\n\nBuild ").Append(_build.Id).Append(" has no place called ").Append(placeName).Append(".\n\n");
                    return;
                }
                var recs = new List<Rec>();
                InteriorPass.Furnished = (b, e) => recs.Add(Record(b, e, placeName));
                var before = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
                _world = new GameObject("World").transform;
                List<Mesh> outer = RaidMesh.CreatedMeshes;
                RaidMesh.CreatedMeshes = _meshes;
                RaidMapInfo info;
                string why;
                var sw = Stopwatch.StartNew();
                try { info = KentuckyMap.Build(_world, zone, out why); }
                finally
                {
                    RaidMesh.CreatedMeshes = outer;
                    InteriorPass.Furnished = null;
                }
                double ms = sw.Elapsed.TotalMilliseconds;
                foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!before.Contains(go) && go != _world.gameObject) _roots.Add(go);
                _md.Append("## ").Append(placeName).Append("\n\n");
                if (info == null)
                {
                    _errors++;
                    _md.Append("**KentuckyMap.Build failed**: ").Append(Md(why)).Append("\n\n");
                    return;
                }
                foreach (MapCutawaySite s in _world.GetComponentsInChildren<MapCutawaySite>(true))
                {
                    _sites.Add(s);
                    s.Register();
                }
                _all.AddRange(recs);

                // What the square is made of, and the interiors' share of it.
                int objects = 0, containers = 0, lights = 0;
                long verts = 0;
                var roots = new List<Transform> { _world };
                foreach (GameObject go in _roots) roots.Add(go.transform);
                foreach (Transform t in roots)
                {
                    objects += t.GetComponentsInChildren<Transform>(true).Length;
                    containers += t.GetComponentsInChildren<LootContainer>(true).Length;
                    lights += t.GetComponentsInChildren<Light>(true).Length;
                    foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(true))
                        if (mf.sharedMesh != null) verts += mf.sharedMesh.vertexCount;
                }
                // Meshes with NaN or infinite vertices (the NavMesh builder skips them, and they draw nothing or everywhere).
                var bad = new List<string>();
                foreach (Transform t in roots)
                    foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(true))
                    {
                        Mesh m = mf.sharedMesh;
                        if (m == null || m.vertexCount == 0) continue;
                        Bounds b = m.bounds;
                        bool finite = Finite(b.center) && Finite(b.extents) && Finite(mf.transform.position);
                        if (finite && m.isReadable)
                        {
                            m.GetVertices(_verts);
                            foreach (Vector3 v in _verts)
                                if (!Finite(v)) { finite = false; break; }
                            _verts.Clear();
                        }
                        if (finite) continue;
                        bad.Add(PathOf(mf.transform) + " (" + m.vertexCount + " vertices)");
                    }
                if (bad.Count > 0)
                {
                    _errors += bad.Count;
                    _md.Append("**").Append(bad.Count).Append(" meshes with NaN or infinite vertices or positions**: ");
                    for (int i = 0; i < bad.Count && i < 12; i++) _md.Append(i > 0 ? "; " : "").Append('`').Append(Md(bad[i])).Append('`');
                    _md.Append("\n\n");
                }
                int iObjects = 0, iContainers = 0, iLights = 0, iLit = 0, failed = 0, stripped = 0, iRenderers = 0, iSlots = 0, iMerged = 0;
                long iVerts = 0;
                double iMs = 0, iMax = 0;
                foreach (Rec r in recs)
                {
                    if (r.Error != null) { failed++; continue; }
                    iObjects += r.Objects;
                    iVerts += r.Vertices;
                    iRenderers += r.Renderers;
                    iSlots += r.Slots;
                    iMerged += r.Merged;
                    iContainers += r.Containers;
                    iLights += r.Lights;
                    iLit += r.Lit;
                    stripped += r.Stripped;
                    iMs += r.TotalMs;
                    iMax = Math.Max(iMax, r.MaxStepMs);
                }
                _md.Append("KentuckyMap.Build ").Append(F0(ms)).Append(" ms. ").Append(Md(KentuckyMap.LastStats)).Append("\n\n").Append(Md(InteriorPass.LastStats)).Append("\n\n");
                _md.Append("| | whole square | interiors |\n|---|--:|--:|\n")
                    .Append("| objects | ").Append(F0(objects)).Append(" | ").Append(F0(iObjects)).Append(" |\n")
                    .Append("| loot containers | ").Append(F0(containers)).Append(" | ").Append(F0(iContainers)).Append(" (").Append(F0(stripped)).Append(" more stripped of their storage) |\n")
                    .Append("| lights | ").Append(F0(lights)).Append(" | ").Append(F0(iLights)).Append(" (").Append(F0(iLit)).Append(" rooms left lit) |\n")
                    .Append("| vertices | ").Append(F0(verts)).Append(" | ").Append(F0(iVerts)).Append(" |\n")
                    .Append("| renderers (material slots) | | ").Append(F0(iRenderers)).Append(" (").Append(F0(iSlots)).Append("; ").Append(F0(iMerged))
                    .Append(" pieces' renderers merged into rooms' furniture meshes) |\n")
                    .Append("| furnishing time | | ").Append(F0(iMs)).Append(" ms in all, slowest step ").Append(F1(iMax)).Append(" ms |\n")
                    .Append("| buildings furnished | | ").Append(recs.Count - failed).Append(" (").Append(failed).Append(" failed) |\n\n");
                if (failed > 0)
                {
                    _errors += failed;
                    _md.Append("### Failures\n\n");
                    foreach (Rec r in recs)
                        if (r.Error != null) _md.Append("- ").Append(r.Use).Append(' ').Append(r.Id).Append(": `").Append(Md(r.Error)).Append("`\n");
                    _md.Append('\n');
                }
                int troubled = 0;
                foreach (Rec r in recs) if (r.Troubles != null) troubled++;
                if (troubled > 0)
                {
                    _errors += troubled;
                    _md.Append("### Rooms or doors left unfinished (").Append(troubled).Append(" buildings)\n\n");
                    int shown = 0;
                    foreach (Rec r in recs)
                    {
                        if (r.Troubles == null || shown++ >= 24) continue;
                        _md.Append("- ").Append(r.Use).Append(' ').Append(r.Id).Append(": `").Append(Md(r.Troubles)).Append("`\n");
                    }
                    _md.Append('\n');
                }
                Buildings(recs);
                Pictures(recs);
                DismantleCheck(recs);
            }

            // Switches off a merged piece that can be dismantled (as dismantling does) and checks its share of its room's
            // merged mesh went with it.
            private void DismantleCheck(List<Rec> recs)
            {
                foreach (Rec r in recs)
                {
                    if (r.Build == null || r.Build.Root == null || r.Build.B == null) continue;
                    MergedPiece piece = r.Build.Root.GetComponentInChildren<MergedPiece>(true);
                    if (piece == null) continue;
                    int before = Collapsed(r.Build);
                    piece.gameObject.SetActive(false);
                    int after = Collapsed(r.Build);
                    _md.Append("Dismantling a merged piece (").Append(Md(piece.name)).Append(" in ").Append(r.Use).Append(' ').Append(r.Id).Append("): ")
                        .Append(after - before).Append(" of its triangles hidden in its room's furniture mesh, ").Append(after > before ? "**OK**" : "**FAIL**").Append(".\n\n");
                    if (after <= before) _errors++;
                    return;
                }
                _md.Append("No merged piece that can be dismantled was found to check.\n\n");
            }

            // The collapsed triangles in a building's merged furniture meshes.
            private static int Collapsed(InteriorBuild b)
            {
                int n = 0;
                foreach (MeshFilter mf in b.B.Root.GetComponentsInChildren<MeshFilter>(true))
                {
                    Mesh m = mf.sharedMesh;
                    if (m == null || !mf.name.StartsWith("Furniture ", StringComparison.Ordinal) || !m.isReadable) continue;
                    for (int s = 0; s < m.subMeshCount; s++)
                    {
                        int[] ix = m.GetIndices(s);
                        for (int i = 0; i + 2 < ix.Length; i += 3)
                            if (ix[i] == ix[i + 1] && ix[i + 1] == ix[i + 2]) n++;
                    }
                }
                return n;
            }

            private static Rec Record(InteriorBuild b, Exception e, string place)
            {
                InteriorPlan p = b.Plan;
                var r = new Rec
                {
                    Build = e == null ? b : null, Plan = p, Id = p.Id, Place = place, Use = p.Use.ToString(), Purpose = p.Purpose, Kind = p.Data.kind,
                    DataUse = p.Data.use, Area = p.Footprint.Size, Verts = p.Footprint.Pts.Count, RoomCount = p.Rooms.Count,
                    Containers = b.Containers.Count, Stripped = b.Stripped, Lights = b.Lights, CeilingLamps = p.CeilingLamps, Objects = b.Objects,
                    Bodies = b.Bodies, TotalMs = b.TotalMs, MaxStepMs = b.MaxStepMs, MaxStep = b.MaxStep, Steps = b.StepCount,
                    Error = e != null ? e.GetType().Name + ": " + e.Message + " @ " + StackHead(e.StackTrace, 5) : null,
                    Troubles = b.Troubles != null ? string.Join(" // ", b.Troubles) : null,
                };
                var rooms = new SortedDictionary<string, int>(StringComparer.Ordinal);
                foreach (PlanRoom room in p.Rooms)
                {
                    r.Allotted += room.Loot;
                    if (room.Lit) r.Lit++;
                    rooms.TryGetValue(room.Role.ToString(), out int n);
                    rooms[room.Role.ToString()] = n + 1;
                }
                var sb = new StringBuilder();
                foreach (KeyValuePair<string, int> kv in rooms)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(kv.Key);
                    if (kv.Value > 1) sb.Append('×').Append(kv.Value);
                }
                r.Rooms = sb.ToString();
                if (b.Root != null) r.Vertices += VerticesUnder(b.Root.transform);
                if (b.Town != null && b.Town.Overhead != null) r.Vertices += VerticesUnder(b.Town.Overhead);
                r.Merged = b.Merged;
                r.Unmerged = b.Unmerged;
                foreach (Transform t in new[] { b.Root != null ? b.Root.transform : null, b.Town != null ? b.Town.Overhead : null })
                {
                    if (t == null) continue;
                    foreach (Renderer rd in t.GetComponentsInChildren<Renderer>(true))
                    {
                        r.Renderers++;
                        r.Slots += rd.sharedMaterials.Length;
                    }
                }
                if (b.RoomMs != null)
                    for (int k = 0; k < b.RoomMs.Length && k < p.Rooms.Count; k++)
                        r.RoomTimes.Add(new KeyValuePair<string, double>(p.Rooms[k].Role.ToString(), b.RoomMs[k]));
                float a = Mathf.Repeat(Mathf.Atan2(p.Ex.y, p.Ex.x) * Mathf.Rad2Deg, 90f);
                r.Angle = Mathf.Min(a, 90f - a);
                if (p.Local != null && p.Local.Count > 2)
                {
                    MapGeometry.Extent(p.Local, out float x0, out float z0, out float x1, out float z1);
                    float box = (x1 - x0) * (z1 - z0);
                    r.Fill = box > 0.01f ? r.Area / box : 1f;
                }
                return r;
            }

            // Every building furnished on the square, by use.
            private void Buildings(List<Rec> recs)
            {
                _md.Append("<details><summary>Every building (").Append(recs.Count).Append(")</summary>\n\n")
                    .Append("| id | use | purpose | kind | m² | turn° | fill | rooms | containers (allotted) | lights | objects | ms (slowest step) |\n")
                    .Append("|---|---|---|---|--:|--:|--:|---|--:|--:|--:|--:|\n");
                var sorted = new List<Rec>(recs);
                sorted.Sort((x, y) => string.CompareOrdinal(x.Use, y.Use) != 0 ? string.CompareOrdinal(x.Use, y.Use) : y.Area.CompareTo(x.Area));
                foreach (Rec r in sorted)
                    _md.Append("| ").Append(r.Id).Append(" | ").Append(r.Use).Append(r.Error != null ? " **failed**" : "").Append(" | ").Append(r.Purpose)
                        .Append(" | ").Append(r.Kind).Append(string.IsNullOrEmpty(r.DataUse) ? "" : " / " + r.DataUse).Append(" | ").Append(F0(r.Area))
                        .Append(" | ").Append(F0(r.Angle)).Append(" | ").Append(r.Fill.ToString("0.00", CultureInfo.InvariantCulture)).Append(" | ").Append(r.Rooms)
                        .Append(" | ").Append(r.Containers).Append(" (").Append(r.Allotted).Append(") | ").Append(r.Lights).Append(r.Lit > 0 ? " lit " + r.Lit : "")
                        .Append(" | ").Append(r.Objects).Append(" | ").Append(F1(r.TotalMs)).Append(" (").Append(F1(r.MaxStepMs)).Append(' ').Append(r.MaxStep).Append(") |\n");
                _md.Append("\n</details>\n\n");
            }

            private void DiscardSquare()
            {
                MapCutaway.RestoreAll();
                foreach (MapCutawaySite s in _sites)
                    if (s != null) s.Unregister();
                _sites.Clear();
                foreach (GameObject go in _roots)
                    if (go != null) Object.DestroyImmediate(go);
                _roots.Clear();
                if (_world != null) Object.DestroyImmediate(_world.gameObject);
                _world = null;
                foreach (Mesh m in _meshes)
                    if (m != null) Object.DestroyImmediate(m);
                _meshes.Clear();
                foreach (Rec r in _all) r.Build = null;
                CeilingVisibility.Clear();
                Shelter.Clear();
            }
            #endregion

            #region Pictures
            // The uses in the order they're looked for, homes first.
            private static readonly string[] Uses =
            {
                "House", "Cottage", "Trailer", "Apartments", "Motel", "Store", "BigStore", "Convenience", "GasStation", "Pharmacy", "Hardware",
                "AutoShop", "Salon", "Restaurant", "Bar", "Office", "Bank", "PostOffice", "Library", "Civic", "Clinic", "Hospital", "Police",
                "FireStation", "Funeral", "Church", "School", "Warehouse", "Workshop", "Barn", "Greenhouse", "Garage", "Shed", "Restrooms",
            };

            // One building of each use not yet photographed (two houses: the most turned and irregular, and the one with the
            // most rooms), the most telling of them: turned off the grid, an irregular outline, a middling size.
            private void Pictures(List<Rec> recs)
            {
                int cap = _quick ? 10 : 16, taken = 0;
                foreach (string use in Uses)
                {
                    if (taken >= cap) break;
                    bool house = use == "House";
                    if (_pictured.Contains(use) && !(house && _housesPictured < 2)) continue;
                    Rec best = null;
                    float bestScore = float.MinValue;
                    foreach (Rec r in recs)
                    {
                        if (r.Build == null || r.Use != use || r.Build.Root == null) continue;
                        float score = r.Angle + (r.Verts > 4 ? 12f : 0f) + (r.Fill < 0.92f ? 8f : 0f) + Mathf.Min(r.RoomCount, 10) * 1.5f
                            - (r.Area > 900f ? 25f : 0f) - (r.Area < 40f ? 10f : 0f);
                        if (house && _housesPictured == 1) score = r.RoomCount * 10f + r.Angle * 0.2f;
                        if (score > bestScore) { bestScore = score; best = r; }
                    }
                    if (best == null) continue;
                    _pictured.Add(use);
                    if (house) _housesPictured++;
                    try
                    {
                        Picture(best);
                        taken++;
                    }
                    catch (Exception e)
                    {
                        _failures++;
                        Debug.LogError("[InteriorShots] picture of " + use + " " + best.Id + " failed: " + e);
                    }
                    if (house && _housesPictured < 2) { _pictured.Remove(use); }
                }
            }

            // A building close up: from straight above in its own axes (its front at the bottom), roofs, upper storeys and
            // ceilings off; and from the isometric camera with the survivor inside, the cutaway run. Side by side in one file.
            private void Picture(Rec r)
            {
                InteriorPlan plan = r.Plan;
                Rect ext = plan.Extent;
                Vector2 c2 = plan.ToSite(ext.center);
                var centre = new Vector3(c2.x, 0f, c2.y);
                float span = Mathf.Max(ext.width, ext.height) + 3f;
                const int side = 800;
                Texture2D top = null, iso = null, night = null;
                try
                {
                    top = TopDown(centre, new Vector3(plan.Ez.x, 0f, plan.Ez.y), span, side);
                    Vector3 inside = Inside(plan);
                    float yaw = IsoYawFacing(plan);
                    float size = Mathf.Clamp(span * 0.42f, 6f, 28f);
                    iso = IsoShot(inside, yaw, size, side, false);
                    string file = (++_shot).ToString("00", CultureInfo.InvariantCulture) + "_" + r.Use.ToLowerInvariant() + "_" + r.Id + ".png";
                    Save(file, top, iso);
                    bool lit = r.Lit > 0 && _nights < (_quick ? 2 : 4);
                    string nightFile = null;
                    if (lit)
                    {
                        night = IsoShot(inside, yaw, size, side, true);
                        nightFile = file.Replace(".png", "_night.png");
                        Save(nightFile, night, null);
                        _nights++;
                    }
                    _pictures.Add("| `" + file + "`" + (nightFile != null ? " (+ `" + nightFile + "`)" : "") + " | " + r.Place + " | " + r.Use + " (" + r.Purpose + "; " + r.Kind
                        + (string.IsNullOrEmpty(r.DataUse) ? "" : " / " + r.DataUse) + ") " + r.Id + " | " + F0(r.Area) + " m², turned " + F0(r.Angle) + "°, " + r.Verts
                        + " corners, fill " + r.Fill.ToString("0.00", CultureInfo.InvariantCulture) + " | " + r.Rooms + " | " + r.Containers + " (" + r.Allotted + ") | "
                        + r.Lights + (r.Lit > 0 ? ", " + r.Lit + " lit" : "") + " | " + r.Objects + " |");
                    if (r.Build != null && r.Build.Root != null) _mixes.Add("- `" + file + "`: " + Mix(r.Build));
                }
                finally
                {
                    if (top != null) Object.DestroyImmediate(top);
                    if (iso != null) Object.DestroyImmediate(iso);
                    if (night != null) Object.DestroyImmediate(night);
                }
            }

            // The middle of the building's biggest room, in the world.
            private static Vector3 Inside(InteriorPlan plan)
            {
                PlanRoom best = null;
                foreach (PlanRoom room in plan.Rooms)
                    if (best == null || room.R.width * room.R.height > best.R.width * best.R.height) best = room;
                Vector2 at = plan.ToSite(best != null ? best.R.center : Vector2.zero);
                return new Vector3(at.x, 0f, at.y);
            }

            // Of the isometric camera's four headings, the one looking in at the building's front.
            private float IsoYawFacing(InteriorPlan plan)
            {
                float best = _isoYaw, bestDot = float.MinValue;
                for (int k = 0; k < 4; k++)
                {
                    float yaw = _isoYaw + 90f * k;
                    var look = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
                    float d = Vector2.Dot(look, plan.Ez);
                    if (d > bestDot) { bestDot = d; best = yaw; }
                }
                return best;
            }

            private void Save(string file, Texture2D left, Texture2D right)
            {
                int w = left.width + (right != null ? right.width : 0), h = Mathf.Max(left.height, right != null ? right.height : 0);
                var sheet = new Texture2D(w, h, TextureFormat.RGB24, false);
                try
                {
                    sheet.SetPixels(0, 0, left.width, left.height, left.GetPixels());
                    if (right != null) sheet.SetPixels(left.width, 0, right.width, right.height, right.GetPixels());
                    sheet.Apply(false);
                    File.WriteAllBytes(Path.Combine(_out, file), sheet.EncodeToPNG());
                }
                finally
                {
                    Object.DestroyImmediate(sheet);
                }
            }
            #endregion

            #region Summary
            private void Summary()
            {
                _md.Append("## By use (every square)\n\n| use | buildings | failed | rooms (mean) | containers mean / max | allotted mean | lights mean / max | lit rooms | objects mean / max | renderers mean (slots) | vertices mean (k) | ms mean / max | slowest step ms |\n")
                    .Append("|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|\n");
                var uses = new SortedDictionary<string, List<Rec>>(StringComparer.Ordinal);
                foreach (Rec r in _all)
                {
                    if (!uses.TryGetValue(r.Use, out List<Rec> list)) uses[r.Use] = list = new List<Rec>();
                    list.Add(r);
                }
                int total = 0, totalContainers = 0;
                foreach (KeyValuePair<string, List<Rec>> kv in uses)
                {
                    int n = 0, failed = 0, rooms = 0, cont = 0, contMax = 0, allot = 0, lights = 0, lightsMax = 0, lit = 0, obj = 0, objMax = 0;
                    double ms = 0, msMax = 0, step = 0, verts = 0, rends = 0, slots = 0;
                    foreach (Rec r in kv.Value)
                    {
                        if (r.Error != null) { failed++; continue; }
                        n++;
                        verts += r.Vertices;
                        rends += r.Renderers;
                        slots += r.Slots;
                        rooms += r.RoomCount;
                        cont += r.Containers;
                        contMax = Math.Max(contMax, r.Containers);
                        allot += r.Allotted;
                        lights += r.Lights;
                        lightsMax = Math.Max(lightsMax, r.Lights);
                        lit += r.Lit;
                        obj += r.Objects;
                        objMax = Math.Max(objMax, r.Objects);
                        ms += r.TotalMs;
                        msMax = Math.Max(msMax, r.TotalMs);
                        step = Math.Max(step, r.MaxStepMs);
                    }
                    total += n;
                    totalContainers += cont;
                    float d = Math.Max(1, n);
                    _md.Append("| ").Append(kv.Key).Append(" | ").Append(n).Append(" | ").Append(failed).Append(" | ").Append(F1(rooms / d)).Append(" | ")
                        .Append(F1(cont / d)).Append(" / ").Append(contMax).Append(" | ").Append(F1(allot / d)).Append(" | ").Append(F1(lights / d)).Append(" / ")
                        .Append(lightsMax).Append(" | ").Append(lit).Append(" | ").Append(F0(obj / d)).Append(" / ").Append(objMax).Append(" | ").Append(F0(rends / d))
                        .Append(" (").Append(F0(slots / d)).Append(") | ").Append(F1(verts / d / 1000.0))
                        .Append(" | ").Append(F1(ms / d)).Append(" / ").Append(F1(msMax)).Append(" | ").Append(F1(step)).Append(" |\n");
                }
                _md.Append("\n").Append(total).Append(" buildings furnished, ").Append(totalContainers).Append(" loot containers in them.\n\n");
                var apart = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (Rec r in _all)
                    if (r.Unmerged != null)
                        foreach (KeyValuePair<string, int> kv in r.Unmerged) Add(apart, kv.Key, kv.Value);
                if (apart.Count > 0) _md.Append("Pieces kept out of the rooms' merged furniture meshes, by what keeps them apart: ").Append(Top(apart, 16)).Append(".\n\n");
                RoomTimes();
                _md.Append("## Pictures\n\nLeft: from straight above in the building's own axes (its front door's wall at the bottom), roofs, upper storeys ")
                    .Append("and ceilings off, plain overcast daylight without shadows. Right: the game's isometric camera (widened to fit the building), the red ")
                    .Append("capsule standing in for the survivor in its biggest room, the cutaway run (its roof and the walls turned to the camera lifted), ")
                    .Append("ceilings hidden as in the game; interior partitions aren't cut here (the game's IsoCutaway does that in play).\n\n")
                    .Append("| file | place | building | footprint | rooms | containers (allotted) | lights | objects |\n|---|---|---|---|---|--:|--:|--:|\n");
                foreach (string p in _pictures) _md.Append(p).Append('\n');
                _md.Append('\n');
                if (_mixes.Count > 0)
                {
                    _md.Append("<details><summary>What the pictured buildings are made of (bodies by name; lights by the body holding them)</summary>\n\n");
                    foreach (string m in _mixes) _md.Append(m).Append('\n');
                    _md.Append("\n</details>\n\n");
                }
            }

            private readonly List<string> _mixes = new List<string>();

            // The pieces of an interior by name (numbers stripped; a piece is what stands under the building's root, its frame
            // or the interior's root), the commonest first; the vertices and renderers they take, the heaviest first; and its
            // lights by the piece holding them.
            private static string Mix(InteriorBuild b)
            {
                Transform root = b.Root.transform, bldg = b.B != null ? b.B.Root : null, frame = b.Frame;
                var bodies = new Dictionary<string, int>(StringComparer.Ordinal);
                var verts = new Dictionary<string, int>(StringComparer.Ordinal);
                var rends = new Dictionary<string, int>(StringComparer.Ordinal);
                var lights = new Dictionary<string, int>(StringComparer.Ordinal);
                int pieces = 0, renderers = 0, slots = 0;
                foreach (Transform holder in new[] { root, bldg, frame })
                {
                    if (holder == null) continue;
                    for (int k = 0; k < holder.childCount; k++)
                    {
                        Transform t = holder.GetChild(k);
                        if (t == bldg || t == frame) continue;
                        pieces++;
                        string name = t.name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', '(', ')', '_');
                        Add(bodies, name, 1);
                        long own = 0;
                        foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(true))
                            if (mf.sharedMesh != null && !Under(mf.transform, t, bldg, frame)) own += mf.sharedMesh.vertexCount;
                        Add(verts, name, (int)own);
                        foreach (Renderer rd in t.GetComponentsInChildren<Renderer>(true))
                        {
                            if (Under(rd.transform, t, bldg, frame)) continue;
                            renderers++;
                            slots += rd.sharedMaterials.Length;
                            Add(rends, name, 1);
                        }
                        int l = t.GetComponentsInChildren<Light>(true).Length;
                        if (l > 0) Add(lights, name, l);
                    }
                }
                return "pieces " + pieces + ": " + Top(bodies, 14) + "; vertices " + F0(VerticesUnder(root)) + ": " + Top(verts, 10) + "; renderers " + renderers
                    + " (" + slots + " material slots): " + Top(rends, 10) + "; lights: " + Top(lights, 10);
            }

            private static void Add(Dictionary<string, int> d, string key, int n)
            {
                d.TryGetValue(key, out int v);
                d[key] = v + n;
            }

            // True when `t` (under `piece`) belongs to one of the other holders below it.
            private static bool Under(Transform t, Transform piece, Transform a, Transform b)
            {
                for (Transform p = t; p != null && p != piece; p = p.parent)
                    if (p == a || p == b) return true;
                return false;
            }

            private static long VerticesUnder(Transform t)
            {
                long n = 0;
                foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) n += mf.sharedMesh.vertexCount;
                return n;
            }

            private static string Top(Dictionary<string, int> counts, int max)
            {
                var list = new List<KeyValuePair<string, int>>(counts);
                list.Sort((a, b) => a.Value != b.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.Key, b.Key));
                var sb = new StringBuilder();
                for (int i = 0; i < list.Count && i < max; i++)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(Md(list[i].Key)).Append('×').Append(list[i].Value);
                }
                if (list.Count > max) sb.Append(", …");
                return sb.Length > 0 ? sb.ToString() : "none";
            }

            // How long rooms of each role take to furnish: the streamer can't split a room, so the slowest is a frame's hitch.
            private void RoomTimes()
            {
                var roles = new SortedDictionary<string, List<double>>(StringComparer.Ordinal);
                foreach (Rec r in _all)
                    foreach (KeyValuePair<string, double> kv in r.RoomTimes)
                    {
                        if (!roles.TryGetValue(kv.Key, out List<double> list)) roles[kv.Key] = list = new List<double>();
                        list.Add(kv.Value);
                    }
                if (roles.Count == 0) return;
                _md.Append("<details><summary>Room furnishing time by role (ms)</summary>\n\n| role | rooms | mean | median | p95 | max |\n|---|--:|--:|--:|--:|--:|\n");
                foreach (KeyValuePair<string, List<double>> kv in roles)
                {
                    List<double> v = kv.Value;
                    v.Sort();
                    double sum = 0;
                    foreach (double x in v) sum += x;
                    _md.Append("| ").Append(kv.Key).Append(" | ").Append(v.Count).Append(" | ").Append(F1(sum / v.Count)).Append(" | ").Append(F1(v[v.Count / 2]))
                        .Append(" | ").Append(F1(v[Math.Min(v.Count - 1, (int)(v.Count * 0.95))])).Append(" | ").Append(F1(v[v.Count - 1])).Append(" |\n");
                }
                _md.Append("\n</details>\n\n");
            }
            #endregion

            #region Streamed pieces
            private Transform _streamed, _overheadRoot;
            private readonly Dictionary<long, MapRegionIndex> _regions = new Dictionary<long, MapRegionIndex>();

            private sealed class Piece
            {
                public GameObject Root, Over;
                public readonly List<Mesh> Meshes = new List<Mesh>();
                public double Ms, PlanMs, PlanMaxMs;
                public int PlanSteps;
            }

            // A piece of the start town streamed as the statewide map streams it, and the streamer furnishing it.
            private void Streamed()
            {
                MapPlace place = _build.FindPlace(_places.Count > 0 ? _places[0] : "Bardstown");
                if (place == null) throw new Exception("no place to stream");
                _md.Append("## Streamed: the statewide map's lazy interiors\n\n");
                const int size = KentuckyMap.ChunkSize;
                int cx = (int)Math.Floor(place.x / size), cz = (int)Math.Floor(place.z / size);
                var middle = new Vector3((float)((cx + 0.5) * size - place.x), 0f, (float)((cz + 0.5) * size - place.z));
                _streamed = new GameObject("Streamed").transform;
                var over = new GameObject("RaidOverhead");
                over.AddComponent<CeilingVisibility>();
                _overheadRoot = over.transform;
                InteriorStreamer.ToolMode = true;
                Piece piece = null;
                try
                {
                    piece = BuildPiece(place, cx, cz, middle);
                    int interiors = 0;
                    foreach (Transform t in piece.Root.GetComponentsInChildren<Transform>(true))
                        if (t.name.StartsWith("Interior KY", StringComparison.Ordinal)) interiors++;
                    InteriorStreamer s = InteriorStreamer.Active;
                    int known = s != null ? s.Known : 0;
                    _md.Append("Piece ").Append(cx).Append('_').Append(cz).Append(" of ").Append(place.name).Append(" (Full) built in ").Append(F0(piece.Ms))
                        .Append(" ms: ").Append(interiors).Append(" interiors in it as built").Append(interiors == 0 ? " (none, as it should be)" : " (**should be none**)")
                        .Append(", ").Append(known).Append(" buildings' plans left with the streamer; the plans took ").Append(F1(piece.PlanMs)).Append(" ms over ")
                        .Append(piece.PlanSteps).Append(" steps (slowest ").Append(F1(piece.PlanMaxMs)).Append(" ms).\n\n");
                    if (interiors != 0) _errors++;
                    if (s == null || known == 0)
                    {
                        _md.Append("**Nothing was registered with the streamer.**\n\n");
                        _errors++;
                        return;
                    }

                    // Furnish round a survivor standing in the piece's middle, a frame (1/60 s) at a time.
                    var focus = new List<Vector3> { middle };
                    int frames = Pump(s, focus, 2400, out int standing);
                    _md.Append("Furnished round a survivor at the piece's middle: ").Append(s.Furnished).Append(" buildings in ").Append(frames)
                        .Append(" frames (").Append(F1(frames / 60.0)).Append(" s at 60 fps), ").Append(standing).Append(" standing, ").Append(s.FailedCount)
                        .Append(" failed; the slowest frame's furnishing took ").Append(F1(s.SlowestFrameMs)).Append(" ms (budget 3 ms, plus the step that ")
                        .Append("crosses it). ").Append(Md(s.Summary)).Append(".\n\n");
                    if (s.FailedCount > 0) _errors += s.FailedCount;

                    LootMemory(s, focus, middle);
                    Rebuild(place, cx, cz, middle, ref piece);
                    HeavyPlans();
                }
                finally
                {
                    if (InteriorStreamer.Active != null) InteriorStreamer.Active.Teardown();
                    InteriorStreamer.ToolMode = false;
                    if (piece != null) Unload(piece);
                    Object.DestroyImmediate(_streamed.gameObject);
                    Object.DestroyImmediate(_overheadRoot.gameObject);
                    _streamed = _overheadRoot = null;
                    CeilingVisibility.Clear();
                    Shelter.Clear();
                }
            }

            // Frames of the streamer until nothing more is furnished for a second (or the limit); returns the frames run.
            private int Pump(InteriorStreamer s, List<Vector3> focus, int limit, out int standing)
            {
                int frames = 0, idle = 0, last = s.Furnished;
                while (frames < limit && idle < 60)
                {
                    _t += 1f / 60f;
                    s.Pump(focus, _t);
                    frames++;
                    if (s.Furnished != last) { last = s.Furnished; idle = 0; }
                    else idle++;
                }
                standing = s.Standing;
                return frames;
            }

            // Loots a furnished building, walks away until it's taken down and back until it stands again, and checks that what
            // was carried off stayed gone, what was emptied stayed empty and the rest is as it was.
            private void LootMemory(InteriorStreamer s, List<Vector3> focus, Vector3 middle)
            {
                var plans = new List<InteriorPlan>();
                s.Plans(plans);
                InteriorBuild target = null;
                foreach (InteriorPlan p in plans)
                {
                    InteriorBuild b = s.BuildOf(p.Id);
                    if (b != null && b.Containers.Count >= 3) { target = b; break; }
                }
                if (target == null)
                {
                    _md.Append("No furnished building with three containers to loot: the memory check is skipped.\n\n");
                    return;
                }
                long id = target.Plan.Id;
                int n = target.Containers.Count;
                var before = new string[n];
                for (int i = 0; i < n; i++) before[i] = Contents(target.Containers[i]);
                _fingerprint = Fingerprint(target);
                _fingerprintId = id;
                // Carry the first off, empty the second.
                Object.DestroyImmediate(target.Containers[0].gameObject);
                target.Containers[1].Contents = new Inventory(999f);
                target.Containers[1].Searched = true;
                focus[0] = middle + new Vector3(2000f, 0f, 0f);
                Pump(s, focus, 120, out _);
                bool down = s.BuildOf(id) == null;
                focus[0] = middle;
                Pump(s, focus, 2400, out _);
                InteriorBuild again = s.BuildOf(id);
                var sb = new StringBuilder();
                sb.Append("Loot memory: building ").Append(id).Append(" (").Append(target.Plan.Use).Append(", ").Append(n).Append(" containers): the first carried off, ")
                    .Append("the second emptied; walked 2 km away (").Append(down ? "taken down" : "**still standing**").Append(") and back (")
                    .Append(again != null ? "furnished again" : "**not furnished again**").Append(")");
                bool ok = down && again != null;
                if (again != null)
                {
                    int same = 0, live = 0;
                    bool gone = again.Containers.Count == n && again.Containers[0] == null;
                    bool empty = again.Containers.Count == n && again.Containers[1] != null && again.Containers[1].IsEmpty && again.Containers[1].Searched;
                    for (int i = 2; i < Mathf.Min(n, again.Containers.Count); i++)
                    {
                        if (again.Containers[i] == null) continue;
                        live++;
                        if (Contents(again.Containers[i]) == before[i]) same++;
                    }
                    sb.Append(": ").Append(again.Containers.Count).Append(" containers, the carried-off one ").Append(gone ? "gone" : "**back**")
                        .Append(", the emptied one ").Append(empty ? "still empty" : "**refilled**").Append(", ").Append(same).Append(" of ").Append(live)
                        .Append(" others as they were");
                    ok &= gone && empty && same == live && again.Containers.Count == n;
                }
                sb.Append(ok ? ". **OK**.\n\n" : ". **FAILED**.\n\n");
                if (!ok) _errors++;
                _md.Append(sb);
            }

            private string _fingerprint;
            private long _fingerprintId;

            // The piece built again from scratch (a new streamer, its memory gone): the same building furnished again comes out
            // the same, object for object.
            private void Rebuild(MapPlace place, int cx, int cz, Vector3 middle, ref Piece piece)
            {
                if (_fingerprint == null) return;
                if (InteriorStreamer.Active != null) InteriorStreamer.Active.Teardown();
                Unload(piece);
                piece = BuildPiece(place, cx, cz, middle);
                InteriorStreamer s = InteriorStreamer.Active;
                if (s == null)
                {
                    _md.Append("**The rebuilt piece registered nothing.**\n\n");
                    _errors++;
                    return;
                }
                Pump(s, new List<Vector3> { middle }, 2400, out _);
                InteriorBuild b = s.BuildOf(_fingerprintId);
                string again = b != null ? Fingerprint(b) : null;
                bool same = again == _fingerprint;
                _md.Append("Determinism: the piece built again and building ").Append(_fingerprintId).Append(" furnished again: ")
                    .Append(b == null ? "**not furnished**" : same ? "identical (objects, names, places, meshes). **OK**" : "**different**").Append(".\n\n");
                if (!same) _errors++;
            }

            private static string Contents(LootContainer c)
            {
                if (c == null || c.Contents == null) return "-";
                var sb = new StringBuilder();
                foreach (ItemStack st in c.Contents.Stacks)
                    if (st != null && st.Def != null) sb.Append(st.Def.Id).Append('x').Append(st.Count).Append(';');
                return sb.ToString();
            }

            // Every object of a building's interior: its path, its place relative to the building and its mesh's size.
            private static string Fingerprint(InteriorBuild b)
            {
                var sb = new StringBuilder();
                Transform root = b.Root.transform;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    Vector3 p = root.InverseTransformPoint(t.position);
                    sb.Append(t.name).Append('@').Append(Mathf.RoundToInt(p.x * 100f)).Append(',').Append(Mathf.RoundToInt(p.y * 100f)).Append(',')
                        .Append(Mathf.RoundToInt(p.z * 100f));
                    MeshFilter mf = t.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) sb.Append('#').Append(mf.sharedMesh.vertexCount);
                    sb.Append('|');
                }
                return sb.Length.ToString(CultureInfo.InvariantCulture) + ":" + Hash(sb.ToString());
            }

            private static ulong Hash(string s)
            {
                ulong h = 14695981039346656037UL;
                foreach (char ch in s)
                {
                    h ^= ch;
                    h *= 1099511628211UL;
                }
                return h;
            }

            // The plan steps of a dense downtown's pieces (Louisville's middle and its neighbours), timed as the streamer runs
            // them: the step a streamed piece's interiors take while it's built.
            private void HeavyPlans()
            {
                MapPlace city = _build.FindPlace("Louisville");
                if (city == null)
                {
                    _md.Append("No Louisville in this build to time the plans in.\n\n");
                    return;
                }
                const int size = KentuckyMap.ChunkSize;
                int cx0 = (int)Math.Floor(city.x / size), cz0 = (int)Math.Floor(city.z / size);
                _md.Append("Plan steps in downtown Louisville's pieces (a step runs until the site's slice is spent, then finishes the plan or ")
                    .Append("the wall under way):\n\n| piece | plans ms in all | steps | slowest step ms | slowest single piece of work |\n|---|--:|--:|--:|---|\n");
                double worst = 0;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int cx = cx0 + dx, cz = cz0 + dz;
                        var middle = new Vector3((float)((cx + 0.5) * size - city.x), 0f, (float)((cz + 0.5) * size - city.z));
                        InteriorPass.ResetPlanTiming();
                        Piece p = null;
                        try
                        {
                            p = BuildPiece(city, cx, cz, middle);
                            _md.Append("| ").Append(cx).Append('_').Append(cz).Append(" | ").Append(F1(p.PlanMs)).Append(" | ").Append(p.PlanSteps).Append(" | ")
                                .Append(F1(p.PlanMaxMs)).Append(" | ").Append(F1(InteriorPass.SlowestPlanMs)).Append(" ms: ").Append(Md(InteriorPass.SlowestPlanWhat)).Append(" |\n");
                            worst = Math.Max(worst, p.PlanMaxMs);
                        }
                        catch (Exception e)
                        {
                            _errors++;
                            _md.Append("| ").Append(cx).Append('_').Append(cz).Append(" | **failed**: `").Append(Md(FirstLine(e.Message))).Append("` | | | |\n");
                        }
                        finally
                        {
                            if (p != null) Unload(p);
                        }
                    }
                _md.Append("\nSlowest plan step in these pieces: ").Append(F1(worst)).Append(" ms.\n\n");
            }

            // KentuckyStreamer's Begin, Steps and Finish for one piece (no loot roll, no NavMesh), its interior plan steps timed.
            private Piece BuildPiece(MapPlace place, int cx, int cz, Vector3 middle)
            {
                const int size = KentuckyMap.ChunkSize;
                int per = _build.RegionSize / size;
                int rx = cx >= 0 ? cx / per : -((-cx + per - 1) / per), rz = cz >= 0 ? cz / per : -((-cz + per - 1) / per);
                long key = ((long)rx << 32) | (uint)rz;
                if (!_regions.TryGetValue(key, out MapRegionIndex region))
                    _regions[key] = region = new MapRegionIndex(_build.HasRegion(rx, rz) ? _build.LoadRegion(rx, rz) : null, rx, rz, _build.RegionSize, size);
                string name = "Piece " + cx + "_" + cz;
                var piece = new Piece { Root = new GameObject(name), Over = new GameObject(name) };
                piece.Root.SetActive(false);
                piece.Root.transform.SetParent(_streamed, false);
                piece.Over.SetActive(false);
                piece.Over.transform.SetParent(_overheadRoot, false);
                int seed = _baseZone != null ? _baseZone.LayoutSeed : 1780;
                var job = new KentuckyMap.ChunkJob
                {
                    Build = _build, Region = region, Place = place.name, Cx = cx, Cz = cz, Seed = unchecked(seed * 486187739 + cx * 73856093 ^ cz * 19349663),
                    Root = piece.Root.transform, Overhead = piece.Over.transform, Offset = middle, Detail = KentuckyMap.Detail.Full,
                };
                List<Mesh> outerMeshes = RaidMesh.CreatedMeshes;
                List<string> outerSigns = DecorTextures.SignsUsed;
                Vector3 outerUv = RaidMesh.PlanarOrigin;
                object owner = Shelter.Owner;
                Func<string, bool> gate = ItemDatabase.LootGate;
                Action<string, int> rolled = ItemDatabase.LootRolled;
                RaidMesh.CreatedMeshes = piece.Meshes;
                DecorTextures.SignsUsed = new List<string>();
                RaidMesh.PlanarOrigin = new Vector3((float)Wrap((cx + 0.5) * size), 0f, (float)Wrap((cz + 0.5) * size));
                Shelter.Owner = piece.Root;
                ItemDatabase.LootGate = null;
                ItemDatabase.LootRolled = null;
                var total = Stopwatch.StartNew();
                var step = new Stopwatch();
                try
                {
                    using (IEnumerator<string> steps = KentuckyMap.ChunkSteps(job).GetEnumerator())
                    {
                        while (true)
                        {
                            step.Restart();
                            if (job.Site != null) job.Site.BeginStep();
                            if (!steps.MoveNext()) break;
                            double ms = step.Elapsed.TotalMilliseconds;
                            if (steps.Current != "interior plans") continue;
                            piece.PlanMs += ms;
                            piece.PlanSteps++;
                            piece.PlanMaxMs = Math.Max(piece.PlanMaxMs, ms);
                        }
                    }
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
                piece.Ms = total.Elapsed.TotalMilliseconds;
                piece.Root.transform.position = middle;
                piece.Over.transform.position = middle;
                piece.Root.SetActive(true);
                piece.Over.SetActive(true);
                return piece;
            }

            private static void Unload(Piece p)
            {
                if (p.Root != null)
                {
                    Shelter.Forget(p.Root);
                    Object.DestroyImmediate(p.Root);
                }
                if (p.Over != null) Object.DestroyImmediate(p.Over);
                foreach (Mesh m in p.Meshes)
                    if (m != null) Object.DestroyImmediate(m);
                p.Meshes.Clear();
            }

            private static double Wrap(double v)
            {
                double r = v % RaidMesh.PlanarPeriod;
                return r < 0 ? r + RaidMesh.PlanarPeriod : r;
            }
            #endregion

            #region The camera, the light, the capture
            // IsoCameraFollow's numbers (read from its defaults).
            private float _isoYaw = 45f, _isoPitch = 38f, _isoClearance = 9f, _isoNear = 0.3f, _lookHeight = 1f;
            private GameObject _rig, _figure;
            private Camera _cam;
            private Light _sun;
            private bool _cameraRender, _pathConfirmed;

            private float IsoDistance(float size) => size / Mathf.Tan(Mathf.Clamp(_isoPitch, 15f, 89f) * Mathf.Deg2Rad) + _isoClearance;

            private float Reach(float size) => IsoDistance(size) * Mathf.Cos(Mathf.Clamp(_isoPitch, 10f, 89f) * Mathf.Deg2Rad) + 1f;

            private void ReadCamera()
            {
                var go = new GameObject("[InteriorShots] camera numbers");
                go.SetActive(false);
                try
                {
                    var f = go.AddComponent<IsoCameraFollow>();
                    _isoYaw = f.IsoYaw;
                    _isoPitch = f.IsoPitch;
                    _isoClearance = f.IsoClearance;
                    _isoNear = f.IsoNearClip;
                    _lookHeight = f.LookHeight;
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }

            private void BuildRig()
            {
                _rig = new GameObject("[InteriorShots]");
                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(_rig.transform, false);
                _cam = camGo.AddComponent<Camera>();
                _cam.enabled = false;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.allowHDR = true;
                _cam.allowMSAA = false;
                UniversalAdditionalCameraData data = _cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.renderShadows = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
                var sunGo = new GameObject("Sun");
                sunGo.transform.SetParent(_rig.transform, false);
                _sun = sunGo.AddComponent<Light>();
                _sun.type = LightType.Directional;
                _sun.useColorTemperature = true;
                _sun.colorTemperature = 5000f;
                RenderSettings.sun = _sun;
                if (_profile != null)
                {
                    var volGo = new GameObject("Game Volume");
                    volGo.transform.SetParent(_rig.transform, false);
                    var volume = volGo.AddComponent<Volume>();
                    volume.isGlobal = true;
                    volume.sharedProfile = _profile;
                }
                _figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _figure.name = "Survivor";
                Object.DestroyImmediate(_figure.GetComponent<Collider>());
                _figure.transform.SetParent(_rig.transform, false);
                _figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = new Material(sh != null ? sh : Shader.Find("Standard")) { name = "[InteriorShots] survivor" };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.75f, 0.12f, 0.10f));
                _figure.GetComponent<Renderer>().sharedMaterial = m;
                _figure.SetActive(false);
            }

            private void DropRig()
            {
                if (_figure != null)
                {
                    Renderer r = _figure.GetComponent<Renderer>();
                    if (r != null && r.sharedMaterial != null) Object.DestroyImmediate(r.sharedMaterial);
                }
                if (_rig != null) Object.DestroyImmediate(_rig);
                _rig = null;
            }

            // The game's overcast day (10:00), or the raid's night at the zone's start hour, by the sums MapShots replicates
            // (no fog: the pictures are about what's inside). Above: no shadows, a brighter sky, so the rooms read.
            private void Light(bool night, bool above)
            {
                float hour = night ? (_baseZone != null ? _baseZone.StartHour : 22f) : 10f;
                const float cloud = 0.7f;
                float elevation = Mathf.Sin(hour / 24f * Mathf.PI * 2f - Mathf.PI * 0.5f);
                float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.18f, 0.28f, elevation));
                float weatherDim = Mathf.Lerp(1f, 0.5f, cloud);
                float level = Mathf.Clamp01(Mathf.Lerp(CloneZConfig.NightLightFloor, 1f, daylight) * weatherDim);
                float dark = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.55f, level));
                float ambientScale = Mathf.Lerp(1f, 0.25f, dark), moonScale = Mathf.Lerp(1f, 0.3f, dark);
                _sun.transform.rotation = above ? Quaternion.Euler(70f, 20f, 0f) : Quaternion.Euler((hour - 6f) / 12f * 180f, -35f + hour / 24f * 30f, 0f);
                _sun.color = Color.Lerp(new Color(1f, 0.62f, 0.36f), new Color(1f, 0.96f, 0.86f), Mathf.Clamp01(elevation * 1.6f));
                _sun.intensity = Mathf.Lerp(CloneZConfig.NightSunIntensity * moonScale, CloneZConfig.DaySunIntensity, daylight) * weatherDim;
                _sun.shadows = above ? LightShadows.None : LightShadows.Soft;
                _sun.shadowStrength = Mathf.Lerp(0.35f, 0.8f, daylight);
                Color ambient = Color.Lerp(new Color(0.07f, 0.08f, 0.13f), new Color(0.40f, 0.42f, 0.47f), daylight) * Mathf.Lerp(0.7f, 1f, weatherDim) * ambientScale;
                if (above) ambient *= 1.6f;
                _cam.backgroundColor = Color.Lerp(new Color(0.02f, 0.03f, 0.06f), new Color(0.30f, 0.42f, 0.55f), daylight) * Mathf.Lerp(0.6f, 1f, weatherDim) * ambientScale;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambient;
                var probe = new SphericalHarmonicsL2();
                probe.AddAmbientLight(ambient);
                RenderSettings.ambientProbe = probe;
                RenderSettings.fog = false;
            }

            // The map's lights as built (their switches) by night near the survivor, off by day; never shadowed here.
            private void MapLights(bool night, Vector3 at, List<Light> lights, List<bool> was)
            {
                lights.Clear();
                was.Clear();
                var roots = new List<Transform>();
                if (_world != null) roots.Add(_world);
                foreach (GameObject go in _roots) if (go != null) roots.Add(go.transform);
                foreach (Transform t in roots)
                    foreach (Light l in t.GetComponentsInChildren<Light>(true))
                    {
                        lights.Add(l);
                        was.Add(l.enabled);
                        l.enabled = night && l.enabled && (l.transform.position - at).sqrMagnitude < RoomLight.CullDistance * RoomLight.CullDistance;
                        l.shadows = LightShadows.None;
                    }
            }

            private static void RestoreLights(List<Light> lights, List<bool> was)
            {
                for (int i = 0; i < lights.Count; i++)
                    if (lights[i] != null) lights[i].enabled = was[i];
            }

            // Everything over the ground storey (roofs, upper storeys, ceilings, awnings) off, or back on.
            private readonly List<Renderer> _hidden = new List<Renderer>();

            private void HideOverhead()
            {
                _hidden.Clear();
                foreach (GameObject go in _roots)
                {
                    if (go == null) continue;
                    foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                        if (!r.forceRenderingOff) { r.forceRenderingOff = true; _hidden.Add(r); }
                }
            }

            private void ShowOverhead()
            {
                foreach (Renderer r in _hidden) if (r != null) r.forceRenderingOff = false;
                _hidden.Clear();
            }

            // The ceilings CeilingVisibility hides in the isometric view (its private list), shadows only, or back.
            private static List<Renderer> Ceilings()
            {
                FieldInfo f = typeof(CeilingVisibility).GetField("s_Renderers", BindingFlags.NonPublic | BindingFlags.Static);
                return f != null ? f.GetValue(null) as List<Renderer> : null;
            }

            private static void SetCeilings(bool show)
            {
                List<Renderer> list = Ceilings();
                if (list == null) return;
                foreach (Renderer r in list)
                    if (r != null) r.shadowCastingMode = show ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            }

            private Texture2D TopDown(Vector3 centre, Vector3 up, float span, int px)
            {
                var lights = new List<Light>();
                var was = new List<bool>();
                HideOverhead();
                MapLights(false, centre, lights, was);
                try
                {
                    Light(false, true);
                    _figure.SetActive(false);
                    _cam.orthographic = true;
                    _cam.orthographicSize = span * 0.5f;
                    _cam.nearClipPlane = 0.3f;
                    _cam.farClipPlane = 200f;
                    _cam.transform.SetPositionAndRotation(centre + Vector3.up * 60f, Quaternion.LookRotation(Vector3.down, up.sqrMagnitude > 1e-4f ? up : Vector3.forward));
                    return Capture(px, px);
                }
                finally
                {
                    RestoreLights(lights, was);
                    ShowOverhead();
                }
            }

            private Texture2D IsoShot(Vector3 at, float yaw, float size, int px, bool night)
            {
                var lights = new List<Light>();
                var was = new List<bool>();
                MapCutaway.RestoreAll();
                MapCutaway.Run(at, yaw, _isoPitch, Reach(size), true, _t += 10f, true);
                SetCeilings(false);
                MapLights(night, at, lights, was);
                try
                {
                    Light(night, false);
                    _figure.SetActive(true);
                    _figure.transform.SetPositionAndRotation(at + Vector3.up * 0.9f, Quaternion.identity);
                    Quaternion rot = Quaternion.Euler(_isoPitch, yaw, 0f);
                    Vector3 focus = at + Vector3.up * _lookHeight;
                    _cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * IsoDistance(size), rot);
                    _cam.orthographic = true;
                    _cam.orthographicSize = size;
                    _cam.nearClipPlane = _isoNear;
                    _cam.farClipPlane = 400f;
                    return Capture(px, px);
                }
                finally
                {
                    _figure.SetActive(false);
                    RestoreLights(lights, was);
                    SetCeilings(true);
                    MapCutaway.RestoreAll();
                }
            }

            private Texture2D Capture(int w, int h)
            {
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "[InteriorShots]", antiAliasing = 1 };
                rt.Create();
                RenderTexture active = RenderTexture.active;
                try
                {
                    // Twice: the first frame of a new view settles anything first-use (shader variants, volume blending).
                    RenderInto(rt);
                    RenderInto(rt);
                    Texture2D tex = Read(rt, w, h);
                    if (!_pathConfirmed)
                    {
                        if (Uniform(tex))
                        {
                            _cameraRender = !_cameraRender;
                            Object.DestroyImmediate(tex);
                            RenderInto(rt);
                            RenderInto(rt);
                            tex = Read(rt, w, h);
                        }
                        _pathConfirmed = true;
                    }
                    return tex;
                }
                finally
                {
                    RenderTexture.active = active;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }

            private static Texture2D Read(RenderTexture rt, int w, int h)
            {
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                tex.Apply(false);
                return tex;
            }

            private static bool Uniform(Texture2D tex)
            {
                Color32 first = tex.GetPixel(0, 0);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        Color32 p = tex.GetPixel(x * tex.width / 16 + 3, y * tex.height / 16 + 3);
                        if (p.r != first.r || p.g != first.g || p.b != first.b) return false;
                    }
                return true;
            }

            private void RenderInto(RenderTexture rt)
            {
                _cam.enabled = true;
                try
                {
                    var request = new RenderPipeline.StandardRequest { destination = rt };
                    if (!_cameraRender && RenderPipeline.SupportsRenderRequest(_cam, request))
                    {
                        RenderPipeline.SubmitRenderRequest(_cam, request);
                        return;
                    }
                    _cam.targetTexture = rt;
                    try { _cam.Render(); }
                    finally { _cam.targetTexture = null; }
                }
                finally
                {
                    _cam.enabled = false;
                }
            }
            #endregion

            private static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

            private readonly List<Vector3> _verts = new List<Vector3>(4096);

            private static string PathOf(Transform t)
            {
                var sb = new StringBuilder(t.name);
                for (Transform p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
                return sb.ToString();
            }

            private static string StackHead(string stack, int lines)
            {
                if (string.IsNullOrEmpty(stack)) return "";
                string[] parts = stack.Split('\n');
                var sb = new StringBuilder();
                for (int i = 0; i < parts.Length && i < lines; i++)
                {
                    if (sb.Length > 0) sb.Append(" < ");
                    string p = parts[i].Trim();
                    int at = p.IndexOf(" [0x", StringComparison.Ordinal);
                    sb.Append(at > 0 ? p.Substring(0, at) : p);
                }
                return sb.ToString();
            }

            private static string FirstLine(string s)
            {
                int i = s.IndexOf('\n');
                return i < 0 ? s : s.Substring(0, i);
            }

            private static string Md(string s) => (s ?? "").Replace('\n', ' ').Replace('\r', ' ').Replace('`', '\'').Replace('|', '/');

            private static string F0(double v) => v.ToString("N0", CultureInfo.InvariantCulture);

            private static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
#endif
