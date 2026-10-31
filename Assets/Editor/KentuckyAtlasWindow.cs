using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// WithTheDead ▸ Kentucky Atlas: the whole state from above, every building of the world store's current build on it,
    /// to check them one by one. Pick a building and the panel shows what the map data says of it; <b>Build in scene</b>
    /// builds its piece of the map in the open scene the way the game streams it (KentuckyAtlas), furnished, and reports
    /// what the game made of it (archetype, storeys, roof, walls) and whether its doors can be gone through, it has
    /// windows and its inside was furnished; the Scene view goes to it (from the game's angle, its rooms from above, or
    /// at its front door). Mark each check good, wrong or not applying, note what's wrong, correct it (the game builds it
    /// the corrected way: what it is, its use, archetype, house style, storeys, roof, walls, the way its front faces, or
    /// that it isn't there), and save: the records go to the store's world/validation folder, which the browser atlas
    /// (Tools/World/atlas, over aerial photographs) shares. In a running Kentucky raid, <b>Go there</b> sends the
    /// survivor to the building to try it. Map data © OpenStreetMap contributors (ODbL).
    /// </summary>
    public sealed partial class KentuckyAtlasWindow : EditorWindow
    {
        private const float PanelWidth = 390f;
        private const double BuildingsMpp = 6.0;     // metres a point at which the buildings show
        private const double MinMpp = 0.04, MaxMpp = 900.0;
        private const int MaxRegionsInView = 12, KeepRegions = 20, MaxLoading = 3;
        private const string PrefPrefix = "CloneZ.KentuckyAtlas.";
        private const string BrowserAtlas = "http://127.0.0.1:8765/";

        private static readonly Color Background = new Color(0.075f, 0.085f, 0.10f);
        private static readonly Color Unchecked = new Color(0.86f, 0.88f, 0.91f), Partial = new Color(0.96f, 0.70f, 0.26f),
            Good = new Color(0.22f, 0.76f, 0.45f), Wrong = new Color(1f, 0.30f, 0.31f), Hidden = new Color(0.30f, 0.30f, 0.34f),
            Corrected = new Color(0.30f, 0.64f, 1f), Selected = new Color(0f, 0.9f, 1f), Street = new Color(0.55f, 0.58f, 0.62f);

        // --- The store ---
        private MapBuild _build;
        private string _why = "";
        private MapPlace[] _places = new MapPlace[0];
        private readonly Dictionary<long, int[]> _summary = new Dictionary<long, int[]>();   // region -> records, ok, partial, bad, corrected
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _main = new System.Collections.Concurrent.ConcurrentQueue<Action>();
        private int _generation;

        // --- The view: grid metres at the map's middle, metres a GUI point ---
        private double _cx = 1524927, _cz = 1163838, _mpp = 1.2;
        private Rect _mapRect;
        private RenderTexture _rt;
        private Material _mat;
        private Mesh _overview;
        private double _ovX, _ovZ;
        private bool _redraw = true, _streets = true;

        // --- Regions ---
        private sealed class AtlasBuilding
        {
            public MapBuilding Data;
            public Vector2[] Pts;   // region-local metres
            public Rect Bounds;
            public Vector2 Middle;
            public float Area;
            public int Vertex, Count;
        }

        private sealed class AtlasRegion
        {
            public int Rx, Rz;
            public long Key;
            public AtlasBuilding[] Buildings;
            public readonly Dictionary<long, List<int>> ById = new Dictionary<long, List<int>>();
            public readonly Dictionary<long, BuildingCheck> Records = new Dictionary<long, BuildingCheck>();
            public DateTime ChecksStamp;
            public List<int>[] Cells;
            public int CellsPerSide, CellSize;
            public List<Vector3> Vertices, StreetVertices;
            public List<int> Tris, Lines, StreetLines;
            public Mesh Mesh, StreetMesh;
            public Color32[] Colours;
            public double Used;
            public string Name => Rx + "_" + Rz;
        }

        private readonly Dictionary<long, AtlasRegion> _regions = new Dictionary<long, AtlasRegion>();
        private readonly Dictionary<long, Task<AtlasRegion>> _loading = new Dictionary<long, Task<AtlasRegion>>();
        private readonly HashSet<long> _failed = new HashSet<long>();

        // --- The building being checked ---
        private AtlasRegion _selRegion;
        private int _selIndex = -1;
        private BuildingCheck _edit;
        private string _base = "";
        private bool _dirty;
        private AtlasReport _report;
        private BuildingSeen _seen;
        private AtlasRegion _hoverRegion;
        private int _hoverIndex = -1;
        private string _status = "";
        private string _goTo = "";
        private bool _neighbours, _corrections = true, _autoBuild, _showData = true, _showFix;
        private Vector2 _scroll;
        private string[] _uses = { "none", "fuel", "police", "fire_station", "clinic", "pharmacy", "school", "place_of_worship",
            "townhall", "post_office", "bank", "restaurant", "bar", "shop:convenience", "shop:supermarket", "shop:hardware" };

        // --- Dragging ---
        private Vector2 _pressAt;
        private bool _dragged;

        [MenuItem("WithTheDead/Kentucky Atlas")]
        private static void Open() => GetWindow<KentuckyAtlasWindow>("Kentucky Atlas");

        #region Life
        private void OnEnable()
        {
            wantsMouseMove = true;
            minSize = new Vector2(720f, 420f);
            _cx = PrefDouble("X", _cx);
            _cz = PrefDouble("Z", _cz);
            _mpp = PrefDouble("Mpp", _mpp);
            _streets = EditorPrefs.GetBool(PrefPrefix + "Streets", true);
            _neighbours = EditorPrefs.GetBool(PrefPrefix + "Neighbours", false);
            _autoBuild = EditorPrefs.GetBool(PrefPrefix + "AutoBuild", false);
            EditorApplication.update += Tick;
            ReadUses();
            OpenStore();
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            _generation++;
            SaveView();
            foreach (AtlasRegion r in _regions.Values) Free(r);
            _regions.Clear();
            _loading.Clear();
            if (_overview != null) DestroyImmediate(_overview);
            if (_rt != null) DestroyImmediate(_rt);
            if (_mat != null) DestroyImmediate(_mat);
        }

        private void OnFocus()
        {
            // The browser atlas may have saved checks meanwhile.
            RefreshChecks();
        }

        private void OpenStore()
        {
            _build = WorldStore.OpenCurrent(out string why);
            _why = why ?? "";
            if (_build == null) return;
            _build.UseCorrections = false;   // the map shows the data as mapped; the corrections are drawn on it
            int gen = ++_generation;
            MapBuild build = _build;
            Task.Run(() =>
            {
                string path = Path.Combine(build.Folder, "places.json");
                MapPlaces file = JsonUtility.FromJson<MapPlaces>(File.ReadAllText(path));
                return file != null && file.places != null ? file.places : new MapPlace[0];
            }).ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion) _main.Enqueue(() =>
                {
                    if (gen != _generation) return;
                    _places = t.Result;
                    Repaint();
                });
            });
            LoadSummary();
            BuildOverview();
        }

        private void ReadUses()
        {
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "World", "atlas", "web", "vocab.json"));
                if (!File.Exists(path)) return;
                Vocab vocab = JsonUtility.FromJson<Vocab>(File.ReadAllText(path));
                if (vocab != null && vocab.uses != null && vocab.uses.Length > 0) _uses = vocab.uses;
            }
            catch (Exception)
            {
                // the built-in list will do
            }
        }

        [Serializable]
        private sealed class Vocab
        {
            public string[] uses = new string[0];
        }

        private void SaveView()
        {
            EditorPrefs.SetString(PrefPrefix + "X", _cx.ToString("R", CultureInfo.InvariantCulture));
            EditorPrefs.SetString(PrefPrefix + "Z", _cz.ToString("R", CultureInfo.InvariantCulture));
            EditorPrefs.SetString(PrefPrefix + "Mpp", _mpp.ToString("R", CultureInfo.InvariantCulture));
        }

        private static double PrefDouble(string key, double fallback)
            => double.TryParse(EditorPrefs.GetString(PrefPrefix + key, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;

        // Regions read on workers are given their meshes here, on the main thread, and what workers found is handed over.
        private void Tick()
        {
            while (_main.TryDequeue(out Action act)) act();
            if (_loading.Count == 0) return;
            List<long> done = null;
            foreach (KeyValuePair<long, Task<AtlasRegion>> kv in _loading)
                if (kv.Value.IsCompleted) (done ??= new List<long>()).Add(kv.Key);
            if (done == null) return;
            foreach (long key in done)
            {
                Task<AtlasRegion> t = _loading[key];
                _loading.Remove(key);
                if (t.Status != TaskStatus.RanToCompletion || t.Result == null)
                {
                    _failed.Add(key);
                    Debug.LogWarning("[KentuckyAtlas] Couldn't read region " + (key >> 32) + "_" + (int)key + ": "
                        + (t.Exception != null ? t.Exception.GetBaseException().Message : "no data"));
                    continue;
                }
                AtlasRegion r = t.Result;
                MakeMeshes(r);
                r.Used = EditorApplication.timeSinceStartup;
                _regions[key] = r;
            }
            _redraw = true;
            Repaint();
        }
        #endregion

        #region Regions
        private static long Key(int rx, int rz) => ((long)rx << 32) | (uint)rz;

        private void Want(int rx, int rz)
        {
            long key = Key(rx, rz);
            if (_regions.TryGetValue(key, out AtlasRegion have))
            {
                have.Used = EditorApplication.timeSinceStartup;
                return;
            }
            if (_loading.ContainsKey(key) || _failed.Contains(key) || _loading.Count >= MaxLoading || !_build.HasRegion(rx, rz)) return;
            MapBuild build = _build;
            _loading[key] = Task.Run(() => Prepare(build, rx, rz));
        }

        // A region as mapped, its outlines and streets as line and triangle lists, its checks, and a grid to find a
        // building under a point. On a worker.
        private static AtlasRegion Prepare(MapBuild build, int rx, int rz)
        {
            MapRegion data = build.LoadRegion(rx, rz);
            var r = new AtlasRegion { Rx = rx, Rz = rz, Key = Key(rx, rz), CellSize = 64 };
            int size = build.RegionSize;
            r.CellsPerSide = Mathf.CeilToInt(size / (float)r.CellSize);
            r.Cells = new List<int>[r.CellsPerSide * r.CellsPerSide];
            var list = new List<AtlasBuilding>();
            r.Vertices = new List<Vector3>();
            r.Tris = new List<int>();
            r.Lines = new List<int>();
            var poly = new List<Vector2>(32);
            foreach (MapBuilding b in data.buildings ?? new MapBuilding[0])
            {
                if (b == null || b.pts == null || b.pts.Length < 6) continue;
                poly.Clear();
                for (int k = 0; k + 1 < b.pts.Length; k += 2) poly.Add(new Vector2(b.pts[k] * 0.1f, b.pts[k + 1] * 0.1f));
                var a = new AtlasBuilding { Data = b, Pts = poly.ToArray(), Vertex = r.Vertices.Count, Count = poly.Count };
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue, area = 0f;
                Vector2 sum = Vector2.zero;
                for (int i = 0; i < poly.Count; i++)
                {
                    Vector2 p = poly[i], q = poly[(i + 1) % poly.Count];
                    x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.y); x1 = Mathf.Max(x1, p.x); z1 = Mathf.Max(z1, p.y);
                    area += p.x * q.y - q.x * p.y;
                    sum += p;
                    r.Vertices.Add(new Vector3(p.x, p.y, 0f));
                    r.Lines.Add(a.Vertex + i);
                    r.Lines.Add(a.Vertex + (i + 1) % poly.Count);
                }
                a.Bounds = Rect.MinMaxRect(x0, z0, x1, z1);
                a.Middle = sum / poly.Count;
                a.Area = Mathf.Abs(area) * 0.5f;
                foreach (int t in KentuckyAtlas.Triangulate(poly)) r.Tris.Add(a.Vertex + t);
                int index = list.Count;
                list.Add(a);
                if (!r.ById.TryGetValue(b.id, out List<int> same)) r.ById[b.id] = same = new List<int>(1);
                same.Add(index);
                int cx0 = Cell(r, x0), cx1 = Cell(r, x1), cz0 = Cell(r, z0), cz1 = Cell(r, z1);
                for (int cz = cz0; cz <= cz1; cz++)
                    for (int cx = cx0; cx <= cx1; cx++)
                        (r.Cells[cz * r.CellsPerSide + cx] ??= new List<int>(4)).Add(index);
            }
            r.Buildings = list.ToArray();

            r.StreetVertices = new List<Vector3>();
            r.StreetLines = new List<int>();
            foreach (MapStreet s in data.streets ?? new MapStreet[0])
            {
                if (s == null || s.pts == null || s.pts.Length < 4) continue;
                int first = r.StreetVertices.Count;
                for (int k = 0; k + 1 < s.pts.Length; k += 2) r.StreetVertices.Add(new Vector3(s.pts[k] * 0.1f, s.pts[k + 1] * 0.1f, 0f));
                for (int i = first; i + 1 < r.StreetVertices.Count; i++)
                {
                    r.StreetLines.Add(i);
                    r.StreetLines.Add(i + 1);
                }
            }
            ReadChecks(build, r);
            return r;
        }

        private static int Cell(AtlasRegion r, float metres) => Mathf.Clamp(Mathf.FloorToInt(metres / r.CellSize), 0, r.CellsPerSide - 1);

        private static void ReadChecks(MapBuild build, AtlasRegion r)
        {
            string path = MapValidation.PathOf(build.ValidationFolder, r.Rx, r.Rz);
            r.ChecksStamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            BuildingChecksFile file = MapValidation.Load(build.ValidationFolder, r.Rx, r.Rz);
            lock (r.Records)
            {
                r.Records.Clear();
                if (file != null)
                    foreach (BuildingCheck c in file.buildings)
                        if (c != null) r.Records[c.id] = c;
            }
        }

        private void MakeMeshes(AtlasRegion r)
        {
            r.Mesh = new Mesh { name = "Atlas buildings " + r.Name, hideFlags = HideFlags.HideAndDontSave };
            if (r.Vertices.Count > 65000) r.Mesh.indexFormat = IndexFormat.UInt32;
            r.Mesh.SetVertices(r.Vertices);
            r.Mesh.subMeshCount = 2;
            r.Mesh.SetIndices(r.Tris, MeshTopology.Triangles, 0, false);
            r.Mesh.SetIndices(r.Lines, MeshTopology.Lines, 1, false);
            r.Mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f);
            r.Colours = new Color32[r.Vertices.Count];
            for (int i = 0; i < r.Buildings.Length; i++) Paint(r, i, false);
            r.Mesh.SetColors(r.Colours);
            r.StreetMesh = new Mesh { name = "Atlas streets " + r.Name, hideFlags = HideFlags.HideAndDontSave };
            if (r.StreetVertices.Count > 65000) r.StreetMesh.indexFormat = IndexFormat.UInt32;
            r.StreetMesh.SetVertices(r.StreetVertices);
            r.StreetMesh.SetIndices(r.StreetLines, MeshTopology.Lines, 0, false);
            r.StreetMesh.bounds = r.Mesh.bounds;
            r.Vertices = r.StreetVertices = null;
            r.Tris = r.Lines = r.StreetLines = null;
        }

        private static void Free(AtlasRegion r)
        {
            if (r.Mesh != null) DestroyImmediate(r.Mesh);
            if (r.StreetMesh != null) DestroyImmediate(r.StreetMesh);
        }

        // A building's colour: how far its checks have got.
        private static Color ColourOf(BuildingCheck rec)
        {
            if (rec != null && rec.fix != null && rec.fix.hide) return Hidden;
            switch (MapValidation.StatusOf(rec))
            {
                case BuildingStatus.Ok: return Good;
                case BuildingStatus.Bad: return Wrong;
                case BuildingStatus.Partial: return Partial;
                default: return Unchecked;
            }
        }

        private static void Paint(AtlasRegion r, int index, bool apply)
        {
            AtlasBuilding b = r.Buildings[index];
            BuildingCheck rec;
            lock (r.Records) r.Records.TryGetValue(b.Data.id, out rec);
            Color32 c = Linear(ColourOf(rec));
            for (int v = 0; v < b.Count; v++) r.Colours[b.Vertex + v] = c;
            if (apply && r.Mesh != null) r.Mesh.SetColors(r.Colours);
        }

        private static void PaintId(AtlasRegion r, long id)
        {
            if (!r.ById.TryGetValue(id, out List<int> same)) return;
            foreach (int i in same) Paint(r, i, false);
            r.Mesh.SetColors(r.Colours);
        }

        // Vertex colours aren't converted to the project's linear space as material colours are.
        private static Color32 Linear(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear ? (Color32)c.linear : (Color32)c;

        private void Evict(HashSet<long> wanted)
        {
            if (_regions.Count <= KeepRegions) return;
            var old = new List<AtlasRegion>();
            foreach (AtlasRegion r in _regions.Values)
                if (!wanted.Contains(r.Key) && r != _selRegion) old.Add(r);
            old.Sort((a, b) => a.Used.CompareTo(b.Used));
            for (int i = 0; i < old.Count && _regions.Count > KeepRegions; i++)
            {
                Free(old[i]);
                _regions.Remove(old[i].Key);
            }
        }

        // The check files of the regions on show, read again when the browser (or another editor) changed them.
        private void RefreshChecks()
        {
            if (_build == null) return;
            var regions = new List<AtlasRegion>(_regions.Values);
            MapBuild build = _build;
            int gen = _generation;
            Task.Run(() =>
            {
                var changed = new List<AtlasRegion>();
                foreach (AtlasRegion r in regions)
                {
                    string path = MapValidation.PathOf(build.ValidationFolder, r.Rx, r.Rz);
                    DateTime stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
                    if (stamp == r.ChecksStamp) continue;
                    ReadChecks(build, r);
                    changed.Add(r);
                }
                return changed;
            }).ContinueWith(t =>
            {
                if (t.Status != TaskStatus.RanToCompletion || t.Result.Count == 0) return;
                _main.Enqueue(() =>
                {
                    if (gen != _generation) return;
                    foreach (AtlasRegion r in t.Result)
                    {
                        if (r.Mesh == null) continue;
                        for (int i = 0; i < r.Buildings.Length; i++) Paint(r, i, false);
                        r.Mesh.SetColors(r.Colours);
                        if (r == _selRegion && !_dirty) LoadEdit();
                    }
                    LoadSummary();
                    _redraw = true;
                    Repaint();
                });
            });
        }

        // How far the checking has got in every region (for the overview).
        private void LoadSummary()
        {
            if (_build == null) return;
            string dir = Path.Combine(_build.ValidationFolder, "regions");
            int gen = _generation;
            Task.Run(() =>
            {
                var counts = new Dictionary<long, int[]>();
                if (!Directory.Exists(dir)) return counts;
                foreach (string path in Directory.GetFiles(dir, "*.json"))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    int cut = name.IndexOf('_', 1);
                    if (cut < 0 || !int.TryParse(name.Substring(0, cut), out int rx) || !int.TryParse(name.Substring(cut + 1), out int rz)) continue;
                    BuildingChecksFile file = MapValidation.Load(Path.GetDirectoryName(dir), rx, rz);
                    if (file == null) continue;
                    var c = new int[5];
                    foreach (BuildingCheck rec in file.buildings)
                    {
                        if (rec == null) continue;
                        c[0]++;
                        BuildingStatus s = MapValidation.StatusOf(rec);
                        if (s == BuildingStatus.Ok) c[1]++;
                        else if (s == BuildingStatus.Partial) c[2]++;
                        else if (s == BuildingStatus.Bad) c[3]++;
                        if (MapValidation.HasFix(rec.fix)) c[4]++;
                    }
                    counts[Key(rx, rz)] = c;
                }
                return counts;
            }).ContinueWith(t =>
            {
                if (t.Status != TaskStatus.RanToCompletion) return;
                _main.Enqueue(() =>
                {
                    if (gen != _generation) return;
                    _summary.Clear();
                    foreach (KeyValuePair<long, int[]> kv in t.Result) _summary[kv.Key] = kv.Value;
                    BuildOverview();
                    Repaint();
                });
            });
        }
        #endregion

        #region Drawing the map
        private Material Mat()
        {
            if (_mat != null) return _mat;
            _mat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            _mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_Cull", (int)CullMode.Off);
            _mat.SetInt("_ZWrite", 0);
            _mat.SetInt("_ZTest", (int)CompareFunction.Always);
            return _mat;
        }

        // Every region of the build as a square, shaded by its buildings and by how far their checking has got.
        private void BuildOverview()
        {
            if (_build == null || _build.Manifest.regions == null) return;
            if (_overview == null) _overview = new Mesh { name = "Atlas overview", hideFlags = HideFlags.HideAndDontSave };
            _build.TryGetExtent(out _ovX, out _ovZ, out _, out _);
            int size = _build.RegionSize, most = 1;
            foreach (MapRegionEntry e in _build.Manifest.regions) most = Mathf.Max(most, e.buildings);
            var verts = new List<Vector3>();
            var colours = new List<Color32>();
            var quads = new List<int>();
            var lines = new List<int>();
            foreach (MapRegionEntry e in _build.Manifest.regions)
            {
                float x = (float)(e.rx * (double)size - _ovX), z = (float)(e.rz * (double)size - _ovZ);
                int v = verts.Count;
                verts.Add(new Vector3(x, z, 0f));
                verts.Add(new Vector3(x + size, z, 0f));
                verts.Add(new Vector3(x + size, z + size, 0f));
                verts.Add(new Vector3(x, z + size, 0f));
                float density = Mathf.Log10(1f + e.buildings) / Mathf.Log10(1f + most);
                Color c = Color.Lerp(new Color(0.13f, 0.15f, 0.18f), new Color(0.42f, 0.47f, 0.55f), density);
                if (_summary.TryGetValue(Key(e.rx, e.rz), out int[] s) && s[0] > 0)
                {
                    Color done = s[3] > 0 ? Wrong : s[2] > 0 ? Partial : Good;
                    c = Color.Lerp(c, done, 0.35f + 0.6f * Mathf.Clamp01(s[0] / (float)Mathf.Max(1, e.buildings)));
                }
                Color32 lc = Linear(c);
                for (int k = 0; k < 4; k++) colours.Add(lc);
                quads.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
                lines.AddRange(new[] { v, v + 1, v + 1, v + 2, v + 2, v + 3, v + 3, v });
            }
            _overview.Clear();
            _overview.SetVertices(verts);
            _overview.SetColors(colours);
            _overview.subMeshCount = 2;
            _overview.SetIndices(quads, MeshTopology.Triangles, 0, false);
            _overview.SetIndices(lines, MeshTopology.Lines, 1, false);
            _overview.bounds = new Bounds(Vector3.zero, Vector3.one * 1e7f);
            _redraw = true;
        }

        private void RenderMap(float ppp)
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(_mapRect.width * ppp)), h = Mathf.Max(1, Mathf.RoundToInt(_mapRect.height * ppp));
            if (_rt == null || _rt.width != w || _rt.height != h)
            {
                if (_rt != null) DestroyImmediate(_rt);
                _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                    { hideFlags = HideFlags.HideAndDontSave, antiAliasing = 4, name = "Kentucky Atlas" };
                _redraw = true;
            }
            if (!_redraw) return;
            _redraw = false;
            double scale = ppp / _mpp, left = _cx - _mapRect.width * 0.5 * _mpp, bottom = _cz - _mapRect.height * 0.5 * _mpp;
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _rt;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, w, 0, h);
            GL.Clear(true, true, QualitySettings.activeColorSpace == ColorSpace.Linear ? Background.linear : Background);
            Material mat = Mat();
            bool close = _mpp <= BuildingsMpp;
            if (_overview != null)
            {
                Matrix4x4 m = Place(_ovX, _ovZ, left, bottom, scale);
                if (!close) Draw(mat, _overview, 0, m, 1f);
                Draw(mat, _overview, 1, m, close ? 0.25f : 0.5f);
            }
            if (close)
            {
                int size = _build.RegionSize;
                foreach (AtlasRegion r in _regions.Values)
                {
                    if (r.Mesh == null || !InView(r, left, bottom, size)) continue;
                    Matrix4x4 m = Place((double)r.Rx * size, (double)r.Rz * size, left, bottom, scale);
                    if (_streets) Draw(mat, r.StreetMesh, 0, m, 0.45f, Street);
                    Draw(mat, r.Mesh, 0, m, 0.42f);
                    if (_mpp < 2.5) Draw(mat, r.Mesh, 1, m, 0.95f);
                }
            }
            GL.PopMatrix();
            RenderTexture.active = prev;
        }

        private bool InView(AtlasRegion r, double left, double bottom, int size)
        {
            double x0 = (double)r.Rx * size, z0 = (double)r.Rz * size;
            return x0 < left + _mapRect.width * _mpp && x0 + size > left && z0 < bottom + _mapRect.height * _mpp && z0 + size > bottom;
        }

        private static Matrix4x4 Place(double x, double z, double left, double bottom, double scale)
            => Matrix4x4.TRS(new Vector3((float)((x - left) * scale), (float)((z - bottom) * scale), 0f), Quaternion.identity,
                new Vector3((float)scale, (float)scale, 1f));

        private static void Draw(Material mat, Mesh mesh, int sub, Matrix4x4 m, float alpha, Color? tint = null)
        {
            Color c = tint ?? Color.white;
            c.a = alpha;
            mat.SetColor("_Color", c);
            mat.SetPass(0);
            Graphics.DrawMeshNow(mesh, m, sub);
        }

        private Vector2 ToGui(double x, double z)
            => new Vector2((float)(_mapRect.width * 0.5 + (x - _cx) / _mpp), (float)(_mapRect.height * 0.5 - (z - _cz) / _mpp));

        private void ToGrid(Vector2 local, out double x, out double z)
        {
            x = _cx + (local.x - _mapRect.width * 0.5) * _mpp;
            z = _cz - (local.y - _mapRect.height * 0.5) * _mpp;
        }

        // What's drawn over the map: the building picked and the one under the mouse, the corrected ones, the place
        // names, the scale, the credit. In the map's own coordinates (its top-left is 0, 0).
        private void DrawOverlay()
        {
            int size = _build.RegionSize;
            if (_mpp <= BuildingsMpp && _mpp < 3.0)
            {
                Handles.color = Corrected;
                double left = _cx - _mapRect.width * 0.5 * _mpp, bottom = _cz - _mapRect.height * 0.5 * _mpp;
                int marks = 0;
                foreach (AtlasRegion r in _regions.Values)
                {
                    if (r.Mesh == null || !InView(r, left, bottom, size)) continue;
                    lock (r.Records)
                        foreach (KeyValuePair<long, BuildingCheck> kv in r.Records)
                        {
                            if (!MapValidation.HasFix(kv.Value.fix) || !r.ById.TryGetValue(kv.Key, out List<int> same)) continue;
                            foreach (int i in same)
                                if (marks++ < 400) Outline(r, i, Corrected, 2f, true);
                        }
                }
            }
            if (_hoverRegion != null && _hoverIndex >= 0 && (_hoverRegion != _selRegion || _hoverIndex != _selIndex))
                Outline(_hoverRegion, _hoverIndex, Color.white, 2f, false);
            if (_selRegion != null && _selIndex >= 0)
            {
                Outline(_selRegion, _selIndex, Color.black, 6f, false);
                Outline(_selRegion, _selIndex, Selected, 3.5f, false);
            }
            Places();
            ScaleBar();
            if (_hoverRegion != null && _hoverIndex >= 0 && !string.IsNullOrEmpty(_hint))
            {
                var hint = new GUIContent(_hint);
                Vector2 hs = EditorStyles.miniLabel.CalcSize(hint);
                var box = new Rect(Mathf.Min(_hintAt.x + 14f, _mapRect.width - hs.x - 10f), _hintAt.y + 16f, hs.x + 8f, hs.y + 2f);
                EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.75f));
                GUI.Label(new Rect(box.x + 4f, box.y + 1f, hs.x, hs.y), hint, EditorStyles.miniLabel);
            }
            var credit = new GUIContent("Map data © OpenStreetMap contributors (ODbL) · build " + _build.Id);
            Vector2 cs = EditorStyles.miniLabel.CalcSize(credit);
            EditorGUI.DrawRect(new Rect(_mapRect.width - cs.x - 8f, _mapRect.height - cs.y - 2f, cs.x + 8f, cs.y + 2f), new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(new Rect(_mapRect.width - cs.x - 4f, _mapRect.height - cs.y - 1f, cs.x, cs.y), credit, EditorStyles.miniLabel);
            string note = _mpp > BuildingsMpp ? "Zoom in to see the buildings" : _loading.Count > 0 ? "Reading " + _loading.Count + " region(s)…" : null;
            if (note != null) Shadowed(new Rect(10f, 8f, 400f, 20f), note, EditorStyles.boldLabel);
        }

        private void Outline(AtlasRegion r, int index, Color colour, float width, bool dashed)
        {
            AtlasBuilding b = r.Buildings[index];
            double ox = (double)r.Rx * _build.RegionSize, oz = (double)r.Rz * _build.RegionSize;
            var pts = new Vector3[b.Pts.Length + 1];
            for (int i = 0; i <= b.Pts.Length; i++)
            {
                Vector2 p = b.Pts[i % b.Pts.Length];
                pts[i] = ToGui(ox + p.x, oz + p.y);
            }
            Handles.color = colour;
            if (dashed) Handles.DrawDottedLines(Pairs(pts), 3f);
            else Handles.DrawAAPolyLine(width, pts);
        }

        private static Vector3[] Pairs(Vector3[] ring)
        {
            var pairs = new Vector3[(ring.Length - 1) * 2];
            for (int i = 0; i + 1 < ring.Length; i++)
            {
                pairs[2 * i] = ring[i];
                pairs[2 * i + 1] = ring[i + 1];
            }
            return pairs;
        }

        private void Places()
        {
            if (_places.Length == 0) return;
            var taken = new List<Rect>();
            int shown = 0;
            foreach (MapPlace p in _places)
            {
                if (!PlaceShown(p.kind)) continue;
                Vector2 at = ToGui(p.x, p.z);
                if (at.x < -60f || at.y < -20f || at.x > _mapRect.width + 60f || at.y > _mapRect.height + 20f) continue;
                var content = new GUIContent(p.name);
                Vector2 s = EditorStyles.boldLabel.CalcSize(content);
                var rect = new Rect(at.x - s.x * 0.5f, at.y - s.y * 0.5f, s.x, s.y);
                if (taken.Exists(t => t.Overlaps(rect))) continue;
                taken.Add(rect);
                Shadowed(rect, p.name, EditorStyles.boldLabel);
                if (++shown >= 120) break;
            }
        }

        private bool PlaceShown(string kind)
        {
            switch (kind)
            {
                case "city": return true;
                case "town": return _mpp < 220.0;
                case "village": return _mpp < 45.0;
                case "hamlet": case "suburb": case "neighbourhood": case "locality": return _mpp < 8.0;
                default: return false;
            }
        }

        private static void Shadowed(Rect r, string text, GUIStyle style)
        {
            Color was = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, style);
            GUI.color = Color.white;
            var white = new GUIStyle(style) { normal = { textColor = Color.white } };
            GUI.Label(r, text, white);
            GUI.color = was;
        }

        private void ScaleBar()
        {
            double target = 110.0 * _mpp, unit = Math.Pow(10, Math.Floor(Math.Log10(target)));
            double metres = target / unit >= 5 ? 5 * unit : target / unit >= 2 ? 2 * unit : unit;
            float px = (float)(metres / _mpp);
            var r = new Rect(10f, _mapRect.height - 24f, px, 4f);
            EditorGUI.DrawRect(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), Color.black);
            EditorGUI.DrawRect(r, Color.white);
            Shadowed(new Rect(r.x, r.y - 18f, 120f, 16f), metres >= 1000 ? (metres / 1000).ToString("0.#", CultureInfo.InvariantCulture) + " km"
                : metres.ToString("0.#", CultureInfo.InvariantCulture) + " m", EditorStyles.miniBoldLabel);
        }
        #endregion

        #region The window
        private void OnGUI()
        {
            Toolbar();
            if (_build == null)
            {
                EditorGUILayout.HelpBox("The world store can't be read: " + _why + "\n\nThe atlas reads the store at " + WorldStore.Root
                    + " (or wherever CLONEZ_WORLD points).", MessageType.Error);
                if (GUILayout.Button("Try again", GUILayout.Width(120f))) OpenStore();
                return;
            }
            float top = EditorStyles.toolbar.fixedHeight;
            _mapRect = new Rect(0f, top, Mathf.Max(100f, position.width - PanelWidth), position.height - top);
            var panel = new Rect(_mapRect.xMax, top, position.width - _mapRect.xMax, position.height - top);
            MapEvents();
            if (Event.current.type == EventType.Repaint)
            {
                WantView();
                RenderMap(EditorGUIUtility.pixelsPerPoint);
                GUI.DrawTexture(_mapRect, _rt, ScaleMode.StretchToFill, false);
                GUI.BeginClip(_mapRect);
                DrawOverlay();
                GUI.EndClip();
            }
            GUILayout.BeginArea(panel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Panel();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            Keys();
        }

        private void Toolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                bool enter = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    && GUI.GetNameOfFocusedControl() == "AtlasGoTo";
                GUI.SetNextControlName("AtlasGoTo");
                _goTo = EditorGUILayout.TextField(_goTo, EditorStyles.toolbarSearchField, GUILayout.Width(260f));
                if (GUILayout.Button(new GUIContent("Go", "A town, a building's id (in the regions read), \"way 123\", \"rx_rz_id\" or grid metres \"x, z\""),
                        EditorStyles.toolbarButton, GUILayout.Width(32f)) || enter)
                {
                    GoTo(_goTo);
                    if (enter) Event.current.Use();
                }
                bool streets = GUILayout.Toggle(_streets, "Streets", EditorStyles.toolbarButton, GUILayout.Width(56f));
                if (streets != _streets)
                {
                    _streets = streets;
                    EditorPrefs.SetBool(PrefPrefix + "Streets", streets);
                    _redraw = true;
                }
                bool auto = GUILayout.Toggle(_autoBuild, new GUIContent("Build each one", "Build every building picked in the scene (slower)"),
                    EditorStyles.toolbarButton, GUILayout.Width(92f));
                if (auto != _autoBuild)
                {
                    _autoBuild = auto;
                    EditorPrefs.SetBool(PrefPrefix + "AutoBuild", auto);
                }
                if (GUILayout.Button(new GUIContent("Refresh", "Read the checks again (the browser atlas may have saved some)"), EditorStyles.toolbarButton,
                        GUILayout.Width(56f)))
                {
                    _failed.Clear();
                    RefreshChecks();
                    LoadSummary();
                }
                if (GUILayout.Button(new GUIContent("Browser atlas", "The atlas over aerial photographs (start it with Tools\\World\\World.ps1 atlas)"),
                        EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    Application.OpenURL(BrowserAtlas + (Picked() is AtlasBuilding b ? "#b=" + _selRegion.Name + "_" + b.Data.id : ""));
                GUILayout.FlexibleSpace();
                int total = 0;
                foreach (int[] s in _summary.Values) total += s[0];
                GUILayout.Label(total.ToString("N0", CultureInfo.InvariantCulture) + " buildings checked statewide", EditorStyles.miniLabel);
            }
        }

        private void MapEvents()
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive, _mapRect);
            Vector2 local = e.mousePosition - _mapRect.position;
            switch (e.GetTypeForControl(id))
            {
                case EventType.ScrollWheel when _mapRect.Contains(e.mousePosition):
                {
                    ToGrid(local, out double bx, out double bz);
                    _mpp = Math.Max(MinMpp, Math.Min(MaxMpp, _mpp * Math.Pow(1.1, e.delta.y)));
                    ToGrid(local, out double ax, out double az);
                    _cx += bx - ax;
                    _cz += bz - az;
                    ViewChanged();
                    e.Use();
                    break;
                }
                case EventType.MouseDown when _mapRect.Contains(e.mousePosition) && (e.button == 0 || e.button == 2):
                    GUIUtility.hotControl = id;
                    GUIUtility.keyboardControl = 0;
                    _pressAt = e.mousePosition;
                    _dragged = false;
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    _cx -= e.delta.x * _mpp;
                    _cz += e.delta.y * _mpp;
                    _dragged |= (e.mousePosition - _pressAt).sqrMagnitude > 16f;
                    ViewChanged();
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    if (!_dragged && e.button == 0) Pick(local);
                    e.Use();
                    break;
                case EventType.MouseMove when _mapRect.Contains(e.mousePosition):
                {
                    AtlasRegion r = null;
                    int i = -1;
                    if (_mpp <= BuildingsMpp)
                    {
                        ToGrid(local, out double x, out double z);
                        Find(x, z, out r, out i);
                    }
                    if (r != _hoverRegion || i != _hoverIndex)
                    {
                        _hoverRegion = r;
                        _hoverIndex = i;
                        Repaint();
                    }
                    if (r != null && i >= 0) TooltipAt(e.mousePosition, r, i);
                    break;
                }
            }
        }

        private void TooltipAt(Vector2 at, AtlasRegion r, int i)
        {
            MapBuilding d = r.Buildings[i].Data;
            BuildingCheck rec;
            lock (r.Records) r.Records.TryGetValue(d.id, out rec);
            _hintAt = at - _mapRect.position;
            _hint = (string.IsNullOrEmpty(d.name) ? "" : d.name + " · ") + d.kind + (string.IsNullOrEmpty(d.use) ? "" : " (" + d.use + ")")
                + (string.IsNullOrEmpty(d.address) ? "" : " · " + d.address) + " · " + MapValidation.StatusOf(rec).ToString().ToLowerInvariant();
        }

        private string _hint;
        private Vector2 _hintAt;

        private void ViewChanged()
        {
            _redraw = true;
            Repaint();
        }

        // The regions the view needs, read in the background.
        private void WantView()
        {
            if (_build == null || _mpp > BuildingsMpp) return;
            int size = _build.RegionSize;
            double left = _cx - _mapRect.width * 0.5 * _mpp, right = _cx + _mapRect.width * 0.5 * _mpp;
            double bottom = _cz - _mapRect.height * 0.5 * _mpp, topZ = _cz + _mapRect.height * 0.5 * _mpp;
            int rx0 = (int)Math.Floor(left / size), rx1 = (int)Math.Floor(right / size), rz0 = (int)Math.Floor(bottom / size), rz1 = (int)Math.Floor(topZ / size);
            if ((rx1 - rx0 + 1) * (rz1 - rz0 + 1) > MaxRegionsInView) return;
            var wanted = new HashSet<long>();
            // Nearest the middle first.
            int mx = (int)Math.Floor(_cx / size), mz = (int)Math.Floor(_cz / size);
            var order = new List<Vector2Int>();
            for (int rz = rz0; rz <= rz1; rz++)
                for (int rx = rx0; rx <= rx1; rx++)
                    order.Add(new Vector2Int(rx, rz));
            order.Sort((a, b) => (Mathf.Abs(a.x - mx) + Mathf.Abs(a.y - mz)).CompareTo(Mathf.Abs(b.x - mx) + Mathf.Abs(b.y - mz)));
            foreach (Vector2Int v in order)
            {
                wanted.Add(Key(v.x, v.y));
                Want(v.x, v.y);
            }
            Evict(wanted);
        }

        // The building under a grid point: the smallest whose outline holds it.
        private bool Find(double x, double z, out AtlasRegion found, out int index)
        {
            found = null;
            index = -1;
            if (_build == null) return false;
            int size = _build.RegionSize;
            int rx0 = (int)Math.Floor(x / size), rz0 = (int)Math.Floor(z / size);
            float best = float.MaxValue;
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!_regions.TryGetValue(Key(rx0 + dx, rz0 + dz), out AtlasRegion r) || r.Mesh == null) continue;
                    var p = new Vector2((float)(x - (double)r.Rx * size), (float)(z - (double)r.Rz * size));
                    if (p.x < -200f || p.y < -200f || p.x > size + 200f || p.y > size + 200f) continue;
                    List<int> cell = r.Cells[Cell(r, p.y) * r.CellsPerSide + Cell(r, p.x)];
                    if (cell == null) continue;
                    foreach (int i in cell)
                    {
                        AtlasBuilding b = r.Buildings[i];
                        if (!b.Bounds.Contains(p) || b.Area >= best || !Inside(b.Pts, p)) continue;
                        best = b.Area;
                        found = r;
                        index = i;
                    }
                }
            return found != null;
        }

        private static bool Inside(Vector2[] poly, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > q.y) != (poly[j].y > q.y) && q.x < (poly[j].x - poly[i].x) * (q.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        private void Pick(Vector2 local)
        {
            if (_mpp > BuildingsMpp)
            {
                // Zoomed out: go to the spot clicked.
                ToGrid(local, out _cx, out _cz);
                _mpp = 1.5;
                ViewChanged();
                return;
            }
            ToGrid(local, out double x, out double z);
            if (Find(x, z, out AtlasRegion r, out int i)) Select(r, i, false);
        }

        private AtlasBuilding Picked() => _selRegion != null && _selIndex >= 0 ? _selRegion.Buildings[_selIndex] : null;

        private void Select(AtlasRegion r, int index, bool centre)
        {
            bool same = r == _selRegion && index == _selIndex;
            // The building already picked (F, or clicked again): only the view moves; its edit and its build stay.
            if (!same)
            {
                if (_dirty && !Save()) return;
                _selRegion = r;
                _selIndex = index;
                _report = null;
                _seen = null;
                LoadEdit();
                _status = "";
                _scroll = Vector2.zero;
            }
            if (centre)
            {
                AtlasBuilding b = r.Buildings[index];
                _cx = (double)r.Rx * _build.RegionSize + b.Middle.x;
                _cz = (double)r.Rz * _build.RegionSize + b.Middle.y;
                if (_mpp > 1.2) _mpp = 0.6;
            }
            ViewChanged();
            if (_autoBuild && !same) EditorApplication.delayCall += BuildSelected;
        }

        // The selected building's record, to edit (a copy: saved or thrown away).
        private void LoadEdit()
        {
            AtlasBuilding b = Picked();
            if (b == null)
            {
                _edit = null;
                return;
            }
            BuildingCheck rec;
            lock (_selRegion.Records) _selRegion.Records.TryGetValue(b.Data.id, out rec);
            _edit = MapValidation.Copy(rec);
            _edit.id = b.Data.id;
            _base = rec != null ? rec.updated : "";
            _dirty = false;
            _showFix = MapValidation.HasFix(_edit.fix);
        }

        private void Keys()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField) return;
            bool ctrl = e.control || e.command;
            // Plain keys, as the browser atlas has them (Unity's own shortcuts take Ctrl+S before a window sees it).
            switch (e.keyCode)
            {
                case KeyCode.S when !ctrl:
                    Save();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (Save()) Next();
                    break;
                case KeyCode.N when !ctrl:
                    Next();
                    break;
                case KeyCode.B when !ctrl:
                    BuildSelected();
                    break;
                case KeyCode.F when !ctrl && Picked() != null:
                    Select(_selRegion, _selIndex, true);
                    break;
                case KeyCode.Escape:
                    if (_dirty && !Save()) break;
                    _selRegion = null;
                    _selIndex = -1;
                    _edit = null;
                    ViewChanged();
                    break;
                default:
                    return;
            }
            e.Use();
        }
        #endregion

        #region The panel
        private void Panel()
        {
            AtlasBuilding b = Picked();
            if (b == null)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Check the buildings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Scroll to zoom, drag to move. Zoom in to a town and click a building.\n\n"
                    + "Build in scene builds its piece of the map the way the game streams it, furnished, and checks its doors, windows and "
                    + "inside. Mark each check, correct what's wrong (the game builds it the corrected way) and save; Save & next goes on to "
                    + "the nearest building not fully checked.\n\nKeys (outside a text box): N next, B build, F centre on it, S save, Enter save & next, "
                    + "Esc let go.\n\nThe browser atlas (Tools\\World\\World.ps1 atlas) shows the same checks over aerial photographs, for the "
                    + "outlines and kinds.", MessageType.None);
                Legend();
                if (!string.IsNullOrEmpty(_hint) && _hoverRegion != null) EditorGUILayout.LabelField(_hint, EditorStyles.wordWrappedMiniLabel);
                return;
            }            MapBuilding d = b.Data;
            string osm = d.id % 2 == 0 ? "way " + d.id / 2 : "relation " + (d.id - 1) / 2;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(!string.IsNullOrEmpty(d.name) ? d.name : !string.IsNullOrEmpty(d.address) ? d.address : Cap(d.kind), Title);
            EditorGUILayout.LabelField(Cap(d.kind) + (string.IsNullOrEmpty(d.use) ? "" : " (" + d.use + ")") + " · building " + d.id + " (OSM " + osm + ")",
                EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("OpenStreetMap ↗", EditorStyles.miniButtonLeft))
                    Application.OpenURL("https://www.openstreetmap.org/" + osm.Replace(' ', '/'));
                if (GUILayout.Button(new GUIContent("Aerial photo ↗", "In the browser atlas (start it with Tools\\World\\World.ps1 atlas)"), EditorStyles.miniButtonMid))
                    Application.OpenURL(BrowserAtlas + "#b=" + _selRegion.Name + "_" + d.id);
                if (GUILayout.Button(new GUIContent("Copy id", "rx_rz_id: paste it in either atlas's search"), EditorStyles.miniButtonRight))
                    EditorGUIUtility.systemCopyBuffer = _selRegion.Name + "_" + d.id;
            }
            _showData = EditorGUILayout.Foldout(_showData, "The map data", true);
            if (_showData)
            {
                Row("Kind", d.kind);
                Row("Use", d.use);
                Row("Name", d.name);
                Row("Address", d.address);
                Row("Storeys", d.levels + (d.levels == MapValidation.DefaultLevels(d.kind) ? " (none mapped)" : ""));
                Row("Height", d.height > 0f ? d.height.ToString("0.#", CultureInfo.InvariantCulture) + " m" : "");
                Row("Roof", d.roof);
                Row("Walls", d.material);
                Row("Colours", string.Join(" ", new[] { d.colour, d.roofColour }).Trim());
                Row("Built", d.year > 0 ? d.year.ToString(CultureInfo.InvariantCulture) : "");
                Row("Footprint", Mathf.RoundToInt(b.Area).ToString("N0", CultureInfo.InvariantCulture) + " m², " + b.Pts.Length + " corners"
                    + (b.Area < 6f ? " (too small: the game leaves it out)" : ""));
                Row("Where", "region " + _selRegion.Name + ", grid " + Math.Round((double)_selRegion.Rx * _build.RegionSize + b.Middle.x) + " E "
                    + Math.Round((double)_selRegion.Rz * _build.RegionSize + b.Middle.y) + " N");
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("In the game", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent(_dirty && MapValidation.HasFix(_edit.fix) ? "Build in scene (with these corrections)" : "Build in scene",
                            "Builds its piece of the map in the open scene the way the game streams it, furnished, and checks it (B)"), GUILayout.Height(24f)))
                        EditorApplication.delayCall += BuildSelected;
                    bool n = GUILayout.Toggle(_neighbours, new GUIContent("+ neighbours", "Build the eight pieces round it too (slower)"), GUILayout.Width(92f));
                    if (n != _neighbours)
                    {
                        _neighbours = n;
                        EditorPrefs.SetBool(PrefPrefix + "Neighbours", n);
                    }
                }
                _corrections = EditorGUILayout.ToggleLeft(new GUIContent("Build it with the corrections", "Off: as mapped, to compare"), _corrections);
            }
            if (KentuckyAtlas.HasScene)
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Game's angle", EditorStyles.miniButtonLeft)) KentuckyAtlas.Frame(_report, AtlasView.Iso);
                    if (GUILayout.Button("Rooms from above", EditorStyles.miniButtonMid))
                    {
                        KentuckyAtlas.ShowUpper(false);
                        KentuckyAtlas.Frame(_report, AtlasView.Plan);
                    }
                    using (new EditorGUI.DisabledScope(_report == null || !_report.HasDoorSpot))
                        if (GUILayout.Button("At the door", EditorStyles.miniButtonMid)) KentuckyAtlas.Frame(_report, AtlasView.Door);
                    if (GUILayout.Button(KentuckyAtlas.UpperShown ? "Roofs off" : "Roofs on", EditorStyles.miniButtonMid))
                        KentuckyAtlas.ShowUpper(!KentuckyAtlas.UpperShown);
                    if (GUILayout.Button("Clear", EditorStyles.miniButtonRight))
                    {
                        KentuckyAtlas.Clear();
                        _report = null;
                    }
                }
            GameReport();
            using (new EditorGUI.DisabledScope(!KentuckyAtlas.InRaid))
                if (GUILayout.Button(new GUIContent("Go there in the raid", KentuckyAtlas.InRaid ? "Send the survivor to its front door, to try it"
                        : "In Play Mode, in a raid in Kentucky: sends the survivor to its front door")))
                    GoThere(b);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Checks", EditorStyles.boldLabel);
            for (int i = 0; i < MapValidation.CheckNames.Length; i++) CheckRow(i);
            if (_report != null && _report.Id == d.id && _report.Built
                && GUILayout.Button(new GUIContent("Fill in what the build found", "Windows, doors and inside from the atlas's own checks (only those not marked yet)"),
                    EditorStyles.miniButton))
                FillFromReport();
            EditorGUILayout.LabelField("Note", EditorStyles.miniBoldLabel);
            string note = EditorGUILayout.TextArea(_edit.note ?? "", EditorStyles.textArea, GUILayout.MinHeight(34f));
            if (note != _edit.note)
            {
                _edit.note = note;
                Touched();
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Save", "S"), GUILayout.Height(24f))) Save();
                if (GUILayout.Button(new GUIContent("Save & next ▶", "Enter"), GUILayout.Height(24f)) && Save()) Next();
                if (GUILayout.Button(new GUIContent("Next ▶", "N"), GUILayout.Height(24f))) Next();
                using (new EditorGUI.DisabledScope(!_dirty))
                    if (GUILayout.Button("Revert", GUILayout.Height(24f), GUILayout.Width(56f))) LoadEdit();
            }
            if (!string.IsNullOrEmpty(_status) || _dirty)
                EditorGUILayout.LabelField(_dirty ? "Not saved yet" : _status, EditorStyles.wordWrappedMiniLabel);
            else
            {
                BuildingCheck rec;
                lock (_selRegion.Records) _selRegion.Records.TryGetValue(d.id, out rec);
                EditorGUILayout.LabelField(rec == null ? "Not checked yet" : "Saved " + rec.updated + " by " + rec.by
                    + (rec.build != _build.Id && !string.IsNullOrEmpty(rec.build) ? " (against build " + rec.build + ")" : ""), EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space(6f);
            _showFix = EditorGUILayout.Foldout(_showFix, MapValidation.HasFix(_edit.fix) ? "Corrections (the game builds it this way) ●" : "Corrections", true);
            if (_showFix) Corrections();
        }

        private static GUIStyle s_Title;
        private static GUIStyle Title => s_Title ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, wordWrap = true };

        private static void Row(string label, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(70f));
                EditorGUILayout.LabelField(value, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static void Legend()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Swatch(Unchecked, "not checked");
                Swatch(Partial, "partly");
                Swatch(Good, "all good");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                Swatch(Wrong, "something wrong");
                Swatch(Corrected, "corrected");
                Swatch(Hidden, "not there");
            }
        }

        private static void Swatch(Color c, string label)
        {
            Rect r = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f));
            r.y += 3f;
            r.height = 10f;
            EditorGUI.DrawRect(r, c);
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
        }

        private void GameReport()
        {
            AtlasBuilding b = Picked();
            if (_report != null && _report.Id == b.Data.id)
            {
                AtlasReport r = _report;
                string text = r.Summary;
                if (r.Built)
                {
                    text += "\n" + r.Doors + (r.Doors == 1 ? " door" : " doors") + (r.BlockedDoors > 0 ? " (" + r.BlockedDoors + " blocked)" : "") + ", "
                        + r.Windows + " ground-floor windows";
                    if (r.Enterable)
                        text += r.Furnished ? "\nInside: " + r.Use + ", " + r.RoomCount + " rooms (" + r.Rooms + "), " + r.Containers + " containers, "
                            + r.Lights + " lights" : "\nInside: not furnished";
                    text += "\nBuilt in " + Math.Round(r.BuildMs) + " ms, furnished in " + Math.Round(r.FurnishMs) + " ms" + (_corrections ? "" : " (as mapped)");
                }
                EditorGUILayout.HelpBox(text, r.Built ? MessageType.Info : MessageType.Error);
                if (r.Problems.Count > 0) EditorGUILayout.HelpBox(string.Join("\n", r.Problems), MessageType.Warning);
                if (r.Notes.Count > 0) EditorGUILayout.LabelField(string.Join("\n", r.Notes), EditorStyles.wordWrappedMiniLabel);
                return;
            }
            BuildingSeen g = _edit != null ? _edit.game : null;
            if (g != null && !string.IsNullOrEmpty(g.seen))
                EditorGUILayout.HelpBox("When last built (" + g.seen + "): " + g.arch + (string.IsNullOrEmpty(g.style) ? "" : "/" + g.style) + ", " + g.storeys
                    + " storeys, " + g.roof + " roof, " + g.wall + "; " + g.doors + " doors" + (g.blockedDoors > 0 ? " (" + g.blockedDoors + " blocked)" : "")
                    + ", " + g.windows + " windows" + (g.furnished ? "; inside: " + g.use + ", " + g.rooms : "")
                    + (string.IsNullOrEmpty(g.problems) ? "" : "\n" + g.problems), MessageType.None);
            else EditorGUILayout.LabelField("Not built yet: Build in scene to see what the game makes of it.", EditorStyles.wordWrappedMiniLabel);
        }

        private static readonly string[] CheckValues = { MapValidation.Ok, MapValidation.Bad, MapValidation.NotApplicable };
        private static readonly GUIContent[] CheckButtons = { new GUIContent("Good"), new GUIContent("Wrong"), new GUIContent("n/a", "Doesn't apply (a shed's inside)") };

        private void CheckRow(int i)
        {
            string value = MapValidation.GetCheck(_edit.checks, i);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(MapValidation.CheckLabels[i], GUILayout.Width(80f));
                for (int k = 0; k < CheckValues.Length; k++)
                {
                    Color was = GUI.backgroundColor;
                    bool on = value == CheckValues[k];
                    if (on) GUI.backgroundColor = k == 0 ? Good : k == 1 ? Wrong : new Color(0.6f, 0.62f, 0.66f);
                    GUIStyle style = k == 0 ? EditorStyles.miniButtonLeft : k == CheckValues.Length - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
                    if (GUILayout.Toggle(on, CheckButtons[k], style) != on)
                    {
                        MapValidation.SetCheck(_edit.checks, i, on ? "" : CheckValues[k]);
                        Touched();
                    }
                    GUI.backgroundColor = was;
                }
            }
        }

        // Windows, doors and inside as the build's own checks found them, where nothing is marked yet.
        private void FillFromReport()
        {
            AtlasReport r = _report;
            void Fill(int i, string v)
            {
                if (string.IsNullOrEmpty(MapValidation.GetCheck(_edit.checks, i))) MapValidation.SetCheck(_edit.checks, i, v);
            }
            bool shut = !r.Enterable && r.Problems.TrueForAll(p => !p.StartsWith("No door", StringComparison.Ordinal));
            if (shut)
            {
                Fill(3, MapValidation.NotApplicable);
                Fill(4, MapValidation.NotApplicable);
                Fill(5, MapValidation.NotApplicable);
            }
            else
            {
                Fill(3, r.Windows > 0 || !r.Problems.Exists(p => p.StartsWith("No windows", StringComparison.Ordinal)) ? MapValidation.Ok : MapValidation.Bad);
                Fill(4, r.Doors > 0 && r.BlockedDoors == 0 ? MapValidation.Ok : MapValidation.Bad);
                Fill(5, r.Furnished && !r.Problems.Exists(p => p.StartsWith("Inside", StringComparison.Ordinal)) ? MapValidation.Ok : MapValidation.Bad);
            }
            Touched();
        }

        private void Corrections()
        {
            BuildingFix f = _edit.fix;
            EditorGUI.BeginChangeCheck();
            f.hide = EditorGUILayout.ToggleLeft("Not there (pulled down, or never was): the game leaves it out", f.hide);
            f.kind = Choice("Kind", f.kind, MapValidation.Kinds, "(as mapped)");
            using (new EditorGUILayout.HorizontalScope())
            {
                f.use = EditorGUILayout.TextField("Use", f.use ?? "");
                if (GUILayout.Button("▾", EditorStyles.miniButton, GUILayout.Width(22f)))
                {
                    var menu = new GenericMenu();
                    foreach (string u in _uses)
                    {
                        string use = u;
                        menu.AddItem(new GUIContent(use.Replace("/", "\u2215")), f.use == use, () =>
                        {
                            _edit.fix.use = use;
                            Touched();
                        });
                    }
                    menu.ShowAsContext();
                }
            }
            f.name = EditorGUILayout.TextField("Name", f.name ?? "");
            f.arch = Choice("Archetype", f.arch, KentuckyAtlas.Archetypes, "(the game's choice)");
            f.style = Choice("House style", f.style, KentuckyAtlas.HouseStyles, "(the game's choice)");
            f.storeys = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Storeys", "0: the game's choice"), f.storeys), 0, 40);
            f.roof = Choice("Roof", f.roof, MapValidation.Roofs, "(as mapped)");
            f.material = Choice("Walls", f.material, MapValidation.Materials, "(as mapped)");
            f.facing = Choice("Front faces", f.facing, MapValidation.Facings, "(the game's choice)");
            if (EditorGUI.EndChangeCheck()) Touched();
            EditorGUILayout.LabelField("Kind, use, name, storeys, roof and walls correct the map data (the inside and the loot follow the kind and "
                + "use); archetype, house style and the front's way override the game's choices. Build in scene shows them before they're saved.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private static string Choice(string label, string value, string[] options, string none)
        {
            var shown = new string[options.Length + 1];
            shown[0] = none;
            Array.Copy(options, 0, shown, 1, options.Length);
            int at = string.IsNullOrEmpty(value) ? 0 : Array.IndexOf(options, value) + 1;
            if (at == 0 && !string.IsNullOrEmpty(value))
            {
                // Something this list doesn't know (a hand-edited file): keep it, shown as it is.
                Array.Resize(ref shown, shown.Length + 1);
                shown[shown.Length - 1] = value;
                at = shown.Length - 1;
            }
            int picked = EditorGUILayout.Popup(label, at, shown);
            return picked == 0 ? "" : picked == at ? value : options[picked - 1];
        }

        private void Touched()
        {
            _dirty = true;
            _status = "";
            Repaint();
        }
        #endregion

        #region Doing things
        private void BuildSelected()
        {
            AtlasBuilding b = Picked();
            if (b == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                EditorUtility.DisplayProgressBar("Kentucky Atlas", "Building " + (string.IsNullOrEmpty(b.Data.name) ? b.Data.kind + " " + b.Data.id : b.Data.name)
                    + " the way the game streams it, and furnishing it…", 0.4f);
                _report = KentuckyAtlas.Build(_build, _selRegion.Rx, _selRegion.Rz, b.Data, _neighbours, _corrections, _dirty ? _edit.fix : null);
                _seen = _report.Built ? _report.ToSeen() : null;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            if (_report.Built) KentuckyAtlas.Frame(_report, AtlasView.Iso);
            _status = _report.Built ? "" : "Not built: " + _report.Error;
            Repaint();
        }

        private void GoThere(AtlasBuilding b)
        {
            double x, z;
            float yaw;
            if (_report != null && _report.Id == b.Data.id && _report.Built)
            {
                x = _report.SpotGridX;
                z = _report.SpotGridZ;
                yaw = _report.SpotYaw;
            }
            else KentuckyAtlas.SpotOutside(b.Data, _selRegion.Rx, _selRegion.Rz, _build.RegionSize, out x, out z, out yaw);
            string why = KentuckyAtlas.GoThere(x, z, yaw);
            _status = why ?? "Sent the survivor there.";
        }

        // Saves the record being edited (asking first if it was saved meanwhile, by the browser say). False when it wasn't.
        private bool Save()
        {
            AtlasBuilding b = Picked();
            if (b == null || _edit == null) return true;
            AtlasRegion r = _selRegion;
            long id = b.Data.id;
            BuildingCheck now = null;
            BuildingChecksFile file = MapValidation.Load(_build.ValidationFolder, r.Rx, r.Rz);
            if (file != null)
                foreach (BuildingCheck c in file.buildings)
                    if (c != null && c.id == id) now = c;
            string stamp = now != null ? now.updated : "";
            if (stamp != _base)
            {
                int choice = EditorUtility.DisplayDialogComplex("Kentucky Atlas", "This building was saved meanwhile" + (now != null ? " by " + now.by + " (" + now.updated + ")" : "")
                    + ". Save yours over it, or load theirs?", "Save mine", "Cancel", "Load theirs");
                if (choice == 1) return false;
                if (choice == 2)
                {
                    lock (r.Records)
                    {
                        if (now != null) r.Records[id] = now;
                        else r.Records.Remove(id);
                    }
                    PaintId(r, id);
                    LoadEdit();
                    ViewChanged();
                    return false;
                }
            }
            BuildingCheck rec = MapValidation.Copy(_edit);
            rec.id = id;
            rec.game = _seen != null && _report != null && _report.Id == id ? _seen : now != null && now.game != null ? now.game : rec.game;
            rec.build = _build.Id;
            rec.by = "unity@" + Environment.MachineName;
            rec.updated = MapValidation.Now();
            try
            {
                MapValidation.Save(_build.ValidationFolder, r.Rx, r.Rz, rec);
            }
            catch (Exception e)
            {
                _status = "Not saved: " + e.Message;
                Debug.LogWarning("[KentuckyAtlas] Saving building " + id + " failed: " + e);
                Repaint();
                return false;
            }
            bool empty = MapValidation.IsEmpty(rec);
            lock (r.Records)
            {
                if (empty) r.Records.Remove(id);
                else r.Records[id] = rec;
            }
            string path = MapValidation.PathOf(_build.ValidationFolder, r.Rx, r.Rz);
            r.ChecksStamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            PaintId(r, id);
            LoadEdit();
            _status = empty ? "Cleared" : "Saved";
            LoadSummary();
            ViewChanged();
            return true;
        }

        // The nearest building not fully checked (and that the game builds: not hidden, not too small).
        private void Next()
        {
            if (_dirty && !Save()) return;
            AtlasBuilding sel = Picked();
            double fx = sel != null ? (double)_selRegion.Rx * _build.RegionSize + sel.Middle.x : _cx;
            double fz = sel != null ? (double)_selRegion.Rz * _build.RegionSize + sel.Middle.y : _cz;
            AtlasRegion bestRegion = null;
            int bestIndex = -1;
            double best = double.MaxValue;
            foreach (AtlasRegion r in _regions.Values)
            {
                if (r.Mesh == null) continue;
                double ox = (double)r.Rx * _build.RegionSize, oz = (double)r.Rz * _build.RegionSize;
                for (int i = 0; i < r.Buildings.Length; i++)
                {
                    AtlasBuilding b = r.Buildings[i];
                    if (b.Area < 6f || (sel != null && b.Data.id == sel.Data.id)) continue;
                    BuildingCheck rec;
                    lock (r.Records) r.Records.TryGetValue(b.Data.id, out rec);
                    if (!Open(rec)) continue;
                    double dx = ox + b.Middle.x - fx, dz = oz + b.Middle.y - fz, d = dx * dx + dz * dz;
                    if (d >= best) continue;
                    best = d;
                    bestRegion = r;
                    bestIndex = i;
                }
            }
            if (bestRegion == null)
            {
                _status = "Every building in the regions read is checked: move the map on.";
                Repaint();
                return;
            }
            Select(bestRegion, bestIndex, true);
        }

        private static bool Open(BuildingCheck rec)
        {
            if (rec == null) return true;
            if (rec.fix != null && rec.fix.hide) return false;
            for (int i = 0; i < MapValidation.CheckNames.Length; i++)
                if (string.IsNullOrEmpty(MapValidation.GetCheck(rec.checks, i))) return true;
            return false;
        }

        private void GoTo(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0 || _build == null) return;
            string[] parts = text.Split('_');
            if (parts.Length == 3 && int.TryParse(parts[0], out int rx) && int.TryParse(parts[1], out int rz) && long.TryParse(parts[2], out long wanted))
            {
                GoToBuilding(rx, rz, wanted);
                return;
            }
            string[] xz = text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (xz.Length == 2 && double.TryParse(xz[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double gx)
                && double.TryParse(xz[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double gz) && gx > 10000 && gz > 10000)
            {
                _cx = gx;
                _cz = gz;
                _mpp = Math.Min(_mpp, 1.0);
                ViewChanged();
                return;
            }
            string lower = text.ToLowerInvariant();
            long id = 0;
            if (lower.StartsWith("way", StringComparison.Ordinal) && long.TryParse(lower.Substring(3).Trim(' ', '/'), out long way)) id = way * 2;
            else if (lower.StartsWith("relation", StringComparison.Ordinal) && long.TryParse(lower.Substring(8).Trim(' ', '/'), out long rel)) id = rel * 2 + 1;
            else if (long.TryParse(text, out long plain)) id = plain;
            if (id > 0)
            {
                foreach (AtlasRegion r in _regions.Values)
                    if (r.Mesh != null && r.ById.TryGetValue(id, out List<int> same))
                    {
                        Select(r, same[0], true);
                        return;
                    }
                _status = "Building " + id + " isn't in the regions read: go to its town first (or use rx_rz_id).";
                Repaint();
                return;
            }
            MapPlace place = Array.Find(_places, p => string.Equals(p.name, text, StringComparison.OrdinalIgnoreCase))
                ?? Array.Find(_places, p => p.name != null && p.name.StartsWith(text, StringComparison.OrdinalIgnoreCase));
            if (place == null)
            {
                _status = "Nothing called " + text + ".";
                Repaint();
                return;
            }
            _cx = place.x;
            _cz = place.z;
            _mpp = place.kind == "city" ? 3.0 : 1.2;
            ViewChanged();
        }

        // A building in a region that may not be read yet: read it, then pick the building.
        private void GoToBuilding(int rx, int rz, long id)
        {
            if (_regions.TryGetValue(Key(rx, rz), out AtlasRegion have) && have.Mesh != null)
            {
                if (have.ById.TryGetValue(id, out List<int> same)) Select(have, same[0], true);
                else _status = "Region " + rx + "_" + rz + " has no building " + id + ".";
                Repaint();
                return;
            }
            if (!_build.HasRegion(rx, rz))
            {
                _status = "The build has no region " + rx + "_" + rz + ".";
                return;
            }
            _cx = (rx + 0.5) * _build.RegionSize;
            _cz = (rz + 0.5) * _build.RegionSize;
            _mpp = Math.Min(_mpp, 4.0);
            ViewChanged();
            int gen = _generation;
            void Wait()
            {
                if (gen != _generation) return;
                if (_regions.TryGetValue(Key(rx, rz), out AtlasRegion r) && r.Mesh != null) GoToBuilding(rx, rz, id);
                else if (!_failed.Contains(Key(rx, rz))) EditorApplication.delayCall += Wait;
            }
            EditorApplication.delayCall += Wait;
        }

        private static string Cap(string s) => string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s.Substring(1);
        #endregion
    }
}
