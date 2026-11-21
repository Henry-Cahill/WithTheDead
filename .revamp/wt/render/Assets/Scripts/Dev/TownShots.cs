#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloneZ
{
    /// <summary>
    /// The Introduction town's harness, behind <c>CloneZ.EditorTools.TownShots.Run</c>. In an empty edit-mode scene it
    /// builds the procedural town (<see cref="RaidMapBuilder.Build"/>) as a raid into the Introduction zone does, bakes
    /// its NavMesh as the bootstrapper would, checks the survivor can walk from the spawn to every exit, every outside
    /// door and every room, counts what the town is made of, and photographs it as the game shows it (the isometric
    /// camera, the overcast day and the raid's night): the plan from above, the whole town, the streets at the widest
    /// zoom, every building, every exit and the spawn. Writes the pictures and report.md to the output folder.
    /// </summary>
    public static class TownShots
    {
        public const int ExitOk = 0, ExitHarness = 1, ExitTown = 2;

        /// <summary>Runs the harness on the town built from <paramref name="seed"/> (null: the zone's layout seed);
        /// <paramref name="quick"/> skips the night pictures but the plan's. Returns the process exit code.</summary>
        public static int Run(string outDir, int? seed, VolumeProfile profile, bool quick) => new Harness(outDir, seed, profile, quick).Execute();

        private sealed class Harness
        {
            private readonly string _out;
            private readonly int? _seedArg;
            private readonly VolumeProfile _profile;
            private readonly bool _quick;
            private readonly StringBuilder _md = new StringBuilder();
            private readonly StringBuilder _stages = new StringBuilder();
            private readonly Stopwatch _clock = Stopwatch.StartNew();
            private readonly List<string> _firstErrors = new List<string>();
            private readonly List<string> _firstWarnings = new List<string>();
            private readonly List<string> _failedChecks = new List<string>();
            private int _errors, _warnings, _failures;

            private ZoneDef _zone;
            private int _seed;
            private Transform _world;
            private RaidMapInfo _info;
            private RaidTown _town;
            private double _buildMs, _bakeMs;
            private readonly List<string> _pictures = new List<string>();

            public Harness(string outDir, int? seed, VolumeProfile profile, bool quick)
            {
                _out = string.IsNullOrEmpty(outDir) ? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "TownShots") : outDir;
                _seedArg = seed;
                _profile = profile;
                _quick = quick;
            }

            public int Execute()
            {
                Application.logMessageReceived += OnLog;
                try
                {
                    Directory.CreateDirectory(_out);
                    _md.Append("# CLONE-Z Introduction town harness\n\n");
                    if (Stage("setup", Setup) && Stage("build the town", Build))
                    {
                        Stage("count", Count);
                        Stage("navmesh and paths", Paths);
                        Stage("pictures", Pictures);
                    }
                }
                catch (Exception e)
                {
                    _failures++;
                    Debug.LogError("[TownShots] the harness failed: " + e);
                }
                finally
                {
                    try { Discard(); }
                    catch (Exception e) { _failures++; Debug.LogError("[TownShots] cleaning up failed: " + e); }
                    Application.logMessageReceived -= OnLog;
                }
                int code = _failures > 0 ? ExitHarness : _errors > 0 || _failedChecks.Count > 0 ? ExitTown : ExitOk;
                WriteReport(code);
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
                    _stages.Append("- ").Append(name).Append(": **FAILED** ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n');
                    Debug.LogError("[TownShots] stage '" + name + "' failed: " + e);
                    return false;
                }
            }

            private void Check(bool ok, string what)
            {
                if (!ok) _failedChecks.Add(what);
            }

            private void OnLog(string message, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    _errors++;
                    if (_firstErrors.Count < 25) _firstErrors.Add(Brief(message) + (type == LogType.Exception ? " @ " + Brief(FirstLine(stack)) : ""));
                }
                else if (type == LogType.Warning)
                {
                    _warnings++;
                    if (_firstWarnings.Count < 25) _firstWarnings.Add(Brief(message));
                }
            }

            #region Setup and build
            private void Setup()
            {
                _zone = ZoneCatalog.Get(ZoneCatalog.DefaultZone);
                _seed = _seedArg ?? _zone.LayoutSeed;
                Climate.Winter = _zone.Winter;
                StarterHome.SetActive(false);
                ReadCamera();
                BuildRig();
            }

            private void Build()
            {
                _world = new GameObject("World").transform;
                var sw = Stopwatch.StartNew();
                _info = RaidMapBuilder.Build(_world, _seed);
                _buildMs = sw.Elapsed.TotalMilliseconds;
                _town = RaidMapBuilder.LastTown;
                if (_town == null) throw new InvalidOperationException("RaidMapBuilder.LastTown was not set");
                Physics.SyncTransforms();
            }

            private void Discard()
            {
                NavMesh.RemoveAllNavMeshData();
                if (_town != null)
                {
                    if (_town.Overhead != null) Object.DestroyImmediate(_town.Overhead.gameObject);
                    if (_town.Scenery != null) Object.DestroyImmediate(_town.Scenery.gameObject);
                }
                if (_world != null) Object.DestroyImmediate(_world.gameObject);
                _world = null;
                CeilingVisibility.Clear();
                if (_rig != null) Object.DestroyImmediate(_rig);
                _rig = null;
            }
            #endregion

            #region Counts
            private struct Tally
            {
                public int Objects, Renderers, Meshes, Vertices, Triangles, Materials, Lights, Colliders;
            }

            private readonly List<(string name, Tally t)> _tallies = new List<(string, Tally)>();

            private void Count()
            {
                var allMeshes = new HashSet<Mesh>();
                var allMaterials = new HashSet<Material>();
                Tally total = default;
                foreach ((string name, Transform root) in Roots())
                {
                    Tally t = TallyOf(root, allMeshes, allMaterials);
                    _tallies.Add((name, t));
                    total.Objects += t.Objects;
                    total.Renderers += t.Renderers;
                    total.Vertices += t.Vertices;
                    total.Triangles += t.Triangles;
                    total.Lights += t.Lights;
                    total.Colliders += t.Colliders;
                }
                total.Meshes = allMeshes.Count;
                total.Materials = allMaterials.Count;
                _tallies.Add(("total", total));
            }

            private IEnumerable<(string, Transform)> Roots()
            {
                yield return ("world", _world);
                if (_town.Overhead != null) yield return ("overhead", _town.Overhead);
                if (_town.Scenery != null) yield return ("scenery", _town.Scenery);
            }

            private static Tally TallyOf(Transform root, HashSet<Mesh> allMeshes, HashSet<Material> allMaterials)
            {
                var t = new Tally();
                var meshes = new HashSet<Mesh>();
                var materials = new HashSet<Material>();
                foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    Mesh m = mf.sharedMesh;
                    if (m == null || !meshes.Add(m)) continue;
                    allMeshes.Add(m);
                    t.Vertices += m.vertexCount;
                    for (int s = 0; s < m.subMeshCount; s++) t.Triangles += (int)(m.GetIndexCount(s) / 3);
                }
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                {
                    t.Renderers++;
                    foreach (Material mat in r.sharedMaterials)
                        if (mat != null && materials.Add(mat)) allMaterials.Add(mat);
                }
                t.Meshes = meshes.Count;
                t.Materials = materials.Count;
                t.Lights = root.GetComponentsInChildren<Light>(true).Length;
                t.Colliders = root.GetComponentsInChildren<Collider>(true).Length;
                t.Objects = root.GetComponentsInChildren<Transform>(true).Length;
                return t;
            }
            #endregion

            #region NavMesh and paths
            private float _navArea;
            private int _navTriangles;
            private readonly List<string> _exitRows = new List<string>();
            private readonly List<string> _buildingRows = new List<string>();
            private readonly List<string> _unreachable = new List<string>();
            private int _roomsChecked, _roomsReached, _doorsChecked, _doorsReached, _carversOpened;
            private Vector3 _from;

            private void Paths()
            {
                // Shut doors and shutters carve the NavMesh closed; the survivor opens them, so the walk is checked with
                // every doorway open.
                foreach (NavMeshObstacle o in _world.GetComponentsInChildren<NavMeshObstacle>(true))
                    if (o.enabled && o.carving) { o.enabled = false; _carversOpened++; }

                var surface = _world.gameObject.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                var sw = Stopwatch.StartNew();
                surface.BuildNavMesh();
                _bakeMs = sw.Elapsed.TotalMilliseconds;

                NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
                _navTriangles = tri.indices.Length / 3;
                for (int i = 0; i + 2 < tri.indices.Length; i += 3)
                {
                    Vector3 a = tri.vertices[tri.indices[i]], b = tri.vertices[tri.indices[i + 1]], c = tri.vertices[tri.indices[i + 2]];
                    _navArea += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                }
                Check(_navTriangles > 0, "the NavMesh bake made nothing");

                Vector3 spawn = _info.PlayerSpawn;
                bool spawnOn = NavMesh.SamplePosition(spawn, out NavMeshHit sh, 1.5f, NavMesh.AllAreas);
                Check(spawnOn, "the spawn " + V(spawn) + " is off the NavMesh");
                if (!spawnOn) return;
                Vector3 from = _from = sh.position;

                foreach (RaidExit e in _info.Exits)
                {
                    float len = PathLength(from, e.Position, 3f, out string why);
                    Check(len >= 0f, "no path from the spawn to the exit " + e.Name + " (" + why + ")");
                    _exitRows.Add("| " + e.Name + " | " + (e.Open ? "open" : "closed") + " | " + V(e.Position) + " | "
                        + (len >= 0f ? F0(len) + " m" : "**" + why + "**") + " |");
                }

                var zones = new List<Rect>();
                for (int i = 0; i < _town.Buildings.Count; i++)
                {
                    RaidBuilding b = _town.Buildings[i];
                    zones.Clear();
                    b.ExteriorDoorZones(zones);
                    int doorsOk = 0;
                    foreach (Rect z in zones)
                    {
                        _doorsChecked++;
                        float len = PathLength(from, new Vector3(z.center.x, 0f, z.center.y), 1.5f, out string why);
                        if (len >= 0f) { doorsOk++; _doorsReached++; }
                        else _unreachable.Add("door outside " + b.Name + " at " + V(new Vector3(z.center.x, 0f, z.center.y)) + ": " + why);
                    }
                    Check(zones.Count > 0, b.Name + " has no outside door");
                    Check(doorsOk == zones.Count, b.Name + ": " + (zones.Count - doorsOk) + " of its " + zones.Count + " outside doors can't be walked to");

                    int roomsOk = 0;
                    foreach (RaidRoom r in b.Rooms)
                    {
                        _roomsChecked++;
                        if (RoomReachable(from, r, out string why)) { roomsOk++; _roomsReached++; }
                        else _unreachable.Add("room " + b.Name + "/" + r.Name + ": " + why);
                    }
                    Rect f = b.Footprint;
                    _buildingRows.Add("| " + (i + 1) + " | " + b.Name + " | " + b.Kind + " | (" + F1(f.xMin) + ", " + F1(f.yMin) + ") – (" + F1(f.xMax) + ", "
                        + F1(f.yMax) + ") | " + F1(f.width) + " × " + F1(f.height) + " | " + b.Rooms.Count + " | " + doorsOk + "/" + zones.Count + " | "
                        + roomsOk + "/" + b.Rooms.Count + " |");
                }
                Check(_roomsReached == _roomsChecked, (_roomsChecked - _roomsReached) + " of " + _roomsChecked + " rooms can't be walked to from the spawn");
                if (_unreachable.Count > 0)
                {
                    InnerDoorways();
                }
            }

            // Every doorway (a "DoorHead" wall piece, centred on its gap) in a building with a room that can't be walked to:
            // whether the NavMesh runs through it, and if not, the clear gap the walls leave and what's baked in it.
            private void InnerDoorways()
            {
                var blocked = new HashSet<string>();
                foreach (string u in _unreachable)
                    if (u.StartsWith("room ", StringComparison.Ordinal)) blocked.Add(u.Substring(5, u.IndexOf('/') - 5));
                foreach (RaidBuilding b in _town.Buildings)
                {
                    if (!blocked.Contains(b.Name)) continue;
                    foreach (Transform t in b.Root.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name != "DoorHead") continue;
                        Renderer head = t.GetComponent<Renderer>();
                        if (head == null) continue;
                        Bounds hb = head.bounds;
                        bool alongX = hb.size.x > hb.size.z;
                        Vector3 gap = new Vector3(t.position.x, 0f, t.position.z);
                        bool through = NavMesh.SamplePosition(gap, out NavMeshHit h, 0.12f, NavMesh.AllAreas);
                        var sb = new StringBuilder(b.Name + " doorway at " + V(gap) + (alongX ? " (along x)" : " (along z)") + ", head "
                            + F1(alongX ? hb.size.x : hb.size.z) + " m wide: " + (through ? "NavMesh runs through" : "no NavMesh in the gap"));
                        if (!through)
                        {
                            var probe = new Bounds(gap + Vector3.up * 1.0f, alongX ? new Vector3(hb.size.x - 0.06f, 1.9f, 0.5f) : new Vector3(0.5f, 1.9f, hb.size.z - 0.06f));
                            int listed = 0;
                            foreach (MeshFilter mf in b.Root.GetComponentsInChildren<MeshFilter>(false))
                            {
                                Renderer r = mf.GetComponent<Renderer>();
                                if (mf.sharedMesh == null || r == null || !r.enabled || !r.bounds.Intersects(probe) || Ignored(mf.transform)) continue;
                                if (!mf.sharedMesh.isReadable) { sb.Append("; ").Append(PathOf(mf.transform)).Append(" (unreadable)"); continue; }
                                int hits = TrianglesIn(mf, probe, out float y0, out float y1);
                                if (hits > 0 && listed++ < 8) sb.Append("; ").Append(PathOf(mf.transform)).Append(' ').Append(hits).Append(" tris y ").Append(F1(y0)).Append("..").Append(F1(y1));
                            }
                            if (listed == 0) sb.Append("; nothing baked in it");
                        }
                        _doorways.Add(sb.ToString());
                    }
                }
            }

            // What the doorway checks found, for the report.
            private readonly List<string> _doorways = new List<string>();

            // The triangles of a mesh whose bounding boxes reach into the probe, and the heights they span there.
            private static int TrianglesIn(MeshFilter mf, Bounds probe, out float y0, out float y1)
            {
                y0 = float.MaxValue;
                y1 = float.MinValue;
                Mesh mesh = mf.sharedMesh;
                Vector3[] v = mesh.vertices;
                int[] t = mesh.triangles;
                Matrix4x4 m = mf.transform.localToWorldMatrix;
                int hits = 0;
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    Vector3 a = m.MultiplyPoint3x4(v[t[i]]), b = m.MultiplyPoint3x4(v[t[i + 1]]), c = m.MultiplyPoint3x4(v[t[i + 2]]);
                    var tb = new Bounds(a, Vector3.zero);
                    tb.Encapsulate(b);
                    tb.Encapsulate(c);
                    if (!tb.Intersects(probe)) continue;
                    hits++;
                    y0 = Mathf.Min(y0, Mathf.Max(tb.min.y, probe.min.y));
                    y1 = Mathf.Max(y1, Mathf.Min(tb.max.y, probe.max.y));
                }
                return hits;
            }

            private static bool Ignored(Transform t)
            {
                for (Transform p = t; p != null; p = p.parent)
                {
                    NavMeshModifier m = p.GetComponent<NavMeshModifier>();
                    if (m != null && m.isActiveAndEnabled && m.ignoreFromBuild && (p == t || m.applyToChildren)) return true;
                }
                return false;
            }

            private static string PathOf(Transform t)
            {
                string s = t.name;
                for (Transform p = t.parent; p != null && p.parent != null; p = p.parent) s = p.name + "/" + s;
                return s;
            }

            // The walked length of the path from a NavMesh point to the NavMesh near a target, or -1 (with why).
            private static float PathLength(Vector3 from, Vector3 to, float reach, out string why)
            {
                why = null;
                if (!NavMesh.SamplePosition(to, out NavMeshHit hit, reach, NavMesh.AllAreas))
                {
                    why = "no NavMesh within " + F1(reach) + " m";
                    return -1f;
                }
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    why = "path " + path.status + (path.corners.Length > 0 ? ", gets to " + V(path.corners[path.corners.Length - 1]) : "");
                    return -1f;
                }
                float len = 0f;
                for (int i = 1; i < path.corners.Length; i++) len += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                return len;
            }

            // A room counts as reached when a complete path ends on NavMesh inside it: its middle, else any of a few spots.
            private static bool RoomReachable(Vector3 from, RaidRoom r, out string why)
            {
                why = "no NavMesh inside";
                Rect inner = r.Inner;
                for (int k = 0; k < 9; k++)
                {
                    float fx = k == 0 ? 0.5f : 0.2f + 0.3f * ((k - 1) % 3);
                    float fz = k == 0 ? 0.5f : 0.2f + 0.3f * ((k - 1) / 3 % 3);
                    var p = new Vector3(Mathf.Lerp(inner.xMin, inner.xMax, fx), 0f, Mathf.Lerp(inner.yMin, inner.yMax, fz));
                    if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 0.8f, NavMesh.AllAreas)) continue;
                    if (!inner.Contains(new Vector2(hit.position.x, hit.position.z))) continue;
                    var path = new NavMeshPath();
                    if (NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete) return true;
                    why = "path " + path.status;
                }
                return false;
            }
            #endregion

            #region Pictures
            private void Pictures()
            {
                CollectLights();
                float half = _info.HalfSize;
                Plan("plan_day.png", false);
                Plan("plan_night.png", true);
                Iso("town_day.png", Vector3.zero, half * 0.93f, false, false, 2400, 1500);
                if (!_quick) Iso("town_night.png", Vector3.zero, half * 0.93f, false, true, 2400, 1500);

                // The streets at the camera's widest zoom, on a 3 × 3 grid over the town.
                float[] g = { -0.55f, 0f, 0.55f };
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                    {
                        var at = new Vector3(g[c] * half, 0f, g[2 - r] * half);
                        string name = "grid_" + "nms"[r] + "wce"[c];
                        Iso(name + "_day.png", at, Wide, false, false);
                        if (!_quick) Iso(name + "_night.png", at, Wide, false, true);
                    }

                for (int i = 0; i < _town.Buildings.Count; i++)
                {
                    RaidBuilding b = _town.Buildings[i];
                    Rect f = b.Footprint;
                    var at = new Vector3(f.center.x, 0f, f.center.y);
                    float size = Mathf.Clamp(Mathf.Max(f.width, f.height) * 0.5f + 2.5f, 8f, 18f);
                    string name = "bldg_" + (i + 1).ToString("00", CultureInfo.InvariantCulture) + "_" + Safe(b.Name);
                    Iso(name + "_day.png", at, size, false, false);
                    if (!_quick) Iso(name + "_night.png", at, size, false, true);
                }

                foreach (RaidExit e in _info.Exits) Iso("exit_" + Safe(e.Name) + "_day.png", e.Position, 9f, false, false);
                Iso("spawn_day.png", _info.PlayerSpawn, _zoom[2], true, false);
                Iso("spawn_night.png", _info.PlayerSpawn, _zoom[2], true, true);
                NavPictures();
            }

            // A building with rooms that can't be walked to, from above with the NavMesh drawn over it in green.
            private void NavPictures()
            {
                var blocked = new HashSet<string>();
                foreach (string u in _unreachable)
                    if (u.StartsWith("room ", StringComparison.Ordinal)) blocked.Add(u.Substring(5, u.IndexOf('/') - 5));
                if (blocked.Count == 0) return;
                NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
                var vertices = tri.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i].y += 0.08f;
                var mesh = new Mesh { name = "[TownShots] navmesh", indexFormat = IndexFormat.UInt32, vertices = vertices, triangles = tri.indices };
                var go = new GameObject("[TownShots] navmesh");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                var mat = new Material(sh != null ? sh : Shader.Find("Unlit/Color")) { name = "[TownShots] navmesh" };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.15f, 0.85f, 0.25f));
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.15f, 0.85f, 0.25f));
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                try
                {
                    foreach (RaidBuilding b in _town.Buildings)
                    {
                        if (!blocked.Contains(b.Name)) continue;
                        Rect f = b.Footprint;
                        Light(false, Vector3.zero, false);
                        _cam.orthographic = true;
                        _cam.orthographicSize = Mathf.Max(f.width, f.height) * 0.6f + 1f;
                        _cam.nearClipPlane = 1f;
                        _cam.farClipPlane = 400f;
                        _cam.transform.SetPositionAndRotation(new Vector3(f.center.x, 150f, f.center.y), Quaternion.Euler(90f, 0f, 0f));
                        Save("nav_" + Safe(b.Name) + ".png", 1400, 1400);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(go);
                    Object.DestroyImmediate(mesh);
                    Object.DestroyImmediate(mat);
                }
            }

            private static string Safe(string s)
            {
                var sb = new StringBuilder(s.Length);
                foreach (char ch in s) sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_');
                return sb.ToString();
            }
            #endregion

            #region Rig
            // IsoCameraFollow's numbers (read from its defaults in Setup).
            private float _isoYaw = 45f, _isoPitch = 38f, _isoClearance = 9f, _isoNear = 0.3f, _lookHeight = 1f;
            private float[] _zoom = { 5.5f, 7f, 9f, 11f, 12.5f };
            private GameObject _rig, _figure;
            private Camera _cam;
            private Light _sun;
            private bool _cameraRender, _pathConfirmed;
            private readonly List<Light> _mapLights = new List<Light>();

            private float Wide => _zoom[_zoom.Length - 1];

            private void ReadCamera()
            {
                var go = new GameObject("[TownShots] camera numbers");
                go.SetActive(false);
                try
                {
                    var f = go.AddComponent<IsoCameraFollow>();
                    _isoYaw = f.IsoYaw;
                    _isoPitch = f.IsoPitch;
                    _isoClearance = f.IsoClearance;
                    _isoNear = f.IsoNearClip;
                    _lookHeight = f.LookHeight;
                    if (f.IsoZoomSizes != null && f.IsoZoomSizes.Length > 0) _zoom = (float[])f.IsoZoomSizes.Clone();
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }

            private void BuildRig()
            {
                _rig = new GameObject("[TownShots]");
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
                _sun.shadows = LightShadows.Soft;
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

                // A survivor-sized stand-in (1.8 m) for the spawn pictures.
                _figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _figure.name = "Survivor";
                Object.DestroyImmediate(_figure.GetComponent<Collider>());
                _figure.transform.SetParent(_rig.transform, false);
                _figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = new Material(sh != null ? sh : Shader.Find("Standard")) { name = "[TownShots] survivor" };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.75f, 0.12f, 0.10f));
                _figure.GetComponent<Renderer>().sharedMaterial = m;
                _figure.SetActive(false);
            }

            private void CollectLights()
            {
                _mapLights.Clear();
                foreach (var (_, root) in Roots()) _mapLights.AddRange(root.GetComponentsInChildren<Light>(true));
            }

            // The game's overcast day (10:00) or the raid's night (the zone's start hour), by the sums the map harnesses
            // replicate from TimeOfDay, WeatherSystem and VisionController (no fog: the pictures are about the town).
            // By night the map's lights near the focus are on, as their budget would have them (all of them for the plan).
            private void Light(bool night, Vector3 focus, bool allLights)
            {
                float hour = night ? _zone.StartHour : 10f;
                const float cloud = 0.7f;
                float elevation = Mathf.Sin(hour / 24f * Mathf.PI * 2f - Mathf.PI * 0.5f);
                float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.18f, 0.28f, elevation));
                float weatherDim = Mathf.Lerp(1f, 0.5f, cloud);
                float level = Mathf.Clamp01(Mathf.Lerp(CloneZConfig.NightLightFloor, 1f, daylight) * weatherDim);
                float dark = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.55f, level));
                float ambientScale = Mathf.Lerp(1f, 0.25f, dark), moonScale = Mathf.Lerp(1f, 0.3f, dark);
                float warmth = Mathf.Clamp01(elevation * 1.6f);
                _sun.transform.rotation = Quaternion.Euler((hour - 6f) / 12f * 180f, -35f + hour / 24f * 30f, 0f);
                _sun.color = Color.Lerp(new Color(1f, 0.62f, 0.36f), new Color(1f, 0.96f, 0.86f), warmth);
                _sun.intensity = Mathf.Lerp(CloneZConfig.NightSunIntensity * moonScale, CloneZConfig.DaySunIntensity, daylight) * weatherDim;
                _sun.shadowStrength = Mathf.Lerp(0.35f, 0.8f, daylight);
                Color ambient = Color.Lerp(new Color(0.07f, 0.08f, 0.13f), new Color(0.40f, 0.42f, 0.47f), daylight)
                    * Mathf.Lerp(0.7f, 1f, weatherDim) * ambientScale;
                _cam.backgroundColor = Color.Lerp(new Color(0.02f, 0.03f, 0.06f), new Color(0.30f, 0.42f, 0.55f), daylight)
                    * Mathf.Lerp(0.6f, 1f, weatherDim) * ambientScale;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambient;
                var probe = new SphericalHarmonicsL2();
                probe.AddAmbientLight(ambient);
                RenderSettings.ambientProbe = probe;
                RenderSettings.fog = false;
                foreach (Light l in _mapLights)
                {
                    if (l == null) continue;
                    l.enabled = night && (allLights || (l.transform.position - focus).sqrMagnitude < RoomLight.CullDistance * RoomLight.CullDistance);
                    l.shadows = LightShadows.None;
                }
            }

            private void Plan(string file, bool night)
            {
                Light(night, Vector3.zero, true);
                _figure.SetActive(false);
                _cam.orthographic = true;
                _cam.orthographicSize = _info.HalfSize + 8f;
                _cam.nearClipPlane = 1f;
                _cam.farClipPlane = 400f;
                _cam.transform.SetPositionAndRotation(new Vector3(0f, 150f, 0f), Quaternion.Euler(90f, 0f, 0f));
                Save(file, 2048, 2048);
            }

            // IsoCameraFollow.PoseIsometric: the fixed heading and pitch, centred on the focus at chest height, backed off with the zoom.
            private void Iso(string file, Vector3 at, float size, bool survivor, bool night, int w = 1600, int h = 1000)
            {
                Light(night, at, false);
                _figure.SetActive(survivor);
                if (survivor) _figure.transform.SetPositionAndRotation(new Vector3(at.x, 0.9f, at.z), Quaternion.identity);
                Quaternion rot = Quaternion.Euler(_isoPitch, _isoYaw, 0f);
                Vector3 focus = new Vector3(at.x, 0f, at.z) + Vector3.up * _lookHeight;
                float distance = size / Mathf.Tan(Mathf.Clamp(_isoPitch, 15f, 89f) * Mathf.Deg2Rad) + _isoClearance;
                if (size > Wide * 1.5f) distance = Mathf.Max(distance, 200f);
                _cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
                _cam.orthographic = true;
                _cam.orthographicSize = size;
                _cam.nearClipPlane = _isoNear;
                _cam.farClipPlane = distance + 250f;
                try { Save(file, w, h); }
                finally { _figure.SetActive(false); }
            }

            private void Save(string file, int w, int h)
            {
                Texture2D tex = null;
                try
                {
                    _cam.aspect = w / (float)h;
                    tex = Capture(w, h);
                    File.WriteAllBytes(Path.Combine(_out, file), tex.EncodeToPNG());
                    _pictures.Add(file);
                }
                finally
                {
                    if (tex != null) Object.DestroyImmediate(tex);
                    _cam.ResetAspect();
                }
            }

            private Texture2D Capture(int w, int h)
            {
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "[TownShots]", antiAliasing = 1 };
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
                            Debug.Log("[TownShots] the first picture came out one flat colour; switching to " + (_cameraRender ? "Camera.Render" : "a render request"));
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

            #region Report
            private void WriteReport(int code)
            {
                _md.Append("Zone `").Append(_zone != null ? _zone.Id : "?").Append("`, layout seed ").Append(_seed).Append(". ");
                _md.Append("`RaidMapBuilder.Build` ").Append(F0(_buildMs)).Append(" ms; NavMesh bake ").Append(F0(_bakeMs)).Append(" ms.\n\n");
                _md.Append("`").Append(RaidMapBuilder.LastStats).Append("`\n\n");

                if (_info != null)
                {
                    _md.Append("Map half-size ").Append(F1(_info.HalfSize)).Append(" m; spawn ").Append(V(_info.PlayerSpawn)).Append(" facing ")
                        .Append(F0(_info.PlayerSpawnYaw)).Append("°.\n\n");
                }

                _md.Append("## Checks\n\n");
                if (_failedChecks.Count == 0) _md.Append("All passed.\n\n");
                foreach (string c in _failedChecks) _md.Append("- **FAILED** ").Append(c).Append('\n');
                _md.Append("\nNavMesh (every door open: ").Append(_carversOpened).Append(" carvers off): ").Append(_navTriangles).Append(" triangles, ")
                    .Append(F0(_navArea)).Append(" m² walkable. Outside doors reached ")
                    .Append(_doorsReached).Append('/').Append(_doorsChecked).Append(", rooms reached ").Append(_roomsReached).Append('/').Append(_roomsChecked).Append(".\n\n");
                if (_unreachable.Count > 0)
                {
                    _md.Append("Unreachable:\n\n");
                    foreach (string u in _unreachable) _md.Append("- ").Append(u).Append('\n');
                    _md.Append('\n');
                }
                if (_doorways.Count > 0)
                {
                    _md.Append("Doorways in buildings with rooms that can't be walked to:\n\n");
                    foreach (string d in _doorways) _md.Append("- ").Append(d).Append('\n');
                    _md.Append('\n');
                }

                _md.Append("## Exits\n\n| exit | this raid | where | walk from the spawn |\n|---|---|---|--:|\n");
                foreach (string row in _exitRows) _md.Append(row).Append('\n');

                _md.Append("\n## Buildings\n\n| # | name | kind | footprint | size (m) | rooms | outside doors reached | rooms reached |\n|--:|---|---|---|---|--:|--:|--:|\n");
                foreach (string row in _buildingRows) _md.Append(row).Append('\n');

                _md.Append("\n## What the town is made of\n\n| root | objects | renderers | meshes | vertices | triangles | materials | lights | colliders |\n|---|--:|--:|--:|--:|--:|--:|--:|--:|\n");
                foreach ((string name, Tally t) in _tallies)
                    _md.Append("| ").Append(name).Append(" | ").Append(t.Objects).Append(" | ").Append(t.Renderers).Append(" | ").Append(t.Meshes).Append(" | ")
                        .Append(t.Vertices).Append(" | ").Append(t.Triangles).Append(" | ").Append(t.Materials).Append(" | ").Append(t.Lights).Append(" | ")
                        .Append(t.Colliders).Append(" |\n");

                _md.Append("\n## Log\n\n").Append(_errors).Append(" errors, ").Append(_warnings).Append(" warnings logged during the run.\n\n");
                foreach (string e in _firstErrors) _md.Append("- error: `").Append(e).Append("`\n");
                foreach (string w in _firstWarnings) _md.Append("- warning: `").Append(w).Append("`\n");

                _md.Append("\n## Pictures\n\n");
                foreach (string p in _pictures) _md.Append("- ").Append(p).Append('\n');

                _md.Append("\n## Stages\n\n").Append(_stages);
                _md.Append("\n**Exit ").Append(code).Append("** after ").Append(F1(_clock.Elapsed.TotalSeconds)).Append(" s.\n");
                try { File.WriteAllText(Path.Combine(_out, "report.md"), _md.ToString()); }
                catch (Exception e) { Debug.LogError("[TownShots] couldn't write the report: " + e); }
            }

            private static string Brief(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                s = s.Replace('\n', ' ').Replace('\r', ' ').Replace('`', '\'');
                return s.Length > 400 ? s.Substring(0, 400) + "…" : s;
            }

            private static string FirstLine(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                int i = s.IndexOf('\n');
                return i < 0 ? s : s.Substring(0, i);
            }

            private static string F0(double v) => v.ToString("0", CultureInfo.InvariantCulture);
            private static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
            private static string V(Vector3 p) => "(" + F1(p.x) + ", " + F1(p.z) + ")";
            #endregion
        }
    }
}
#endif
