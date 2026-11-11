using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Draws every item lying on the ground near the player.
    ///
    /// Ground items have no scene objects of their own — they are records in
    /// <see cref="GroundItemStore"/> — so something has to put them on screen. This gathers the items
    /// in the chunks around the camera, sorts them by item type and level of detail, and issues one
    /// instanced draw per group. The cost therefore scales with how many <em>kinds</em> of thing are
    /// in view rather than how many things: a thousand tin cans on the floor is one draw call, not a
    /// thousand, and certainly not the three hundred thousand the old GameObject visuals would have
    /// needed.
    ///
    /// Nothing about an item is worked out twice while it lies there. Each chunk in range keeps its items
    /// filed by type with everything that holds while they lie still: where each one is, its resting
    /// facing, the phase of its bob and which look it shows. A chunk is filed again only when its contents
    /// change (<see cref="GroundItemStore.Chunk.Version"/>), and when the survivor crosses into a new set
    /// of chunks only the ones arriving are filed. A frame then culls whole chunks and types against the
    /// view before touching anything in them, looks a mesh up only once an item that uses it may be in view
    /// (the lookup can load it from disk), and writes just the moving part of each visible item's matrix.
    /// While nothing that decides what's in view changes (the eye, the view, the chunks in range, the
    /// settings), as when the survivor stands still, a frame skips even that and only moves the items the
    /// last full gather found in view, testing just those whose bob can carry them across the view's edge.
    ///
    /// The gentle spin and bob every dropped item has is folded into the instance matrix here, which
    /// is why the items need no per-object update of their own.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class GroundItemRenderer : MonoBehaviour
    {
        /// <summary>Singleton instance of the renderer for easy access.</summary>
        public static GroundItemRenderer Instance { get; private set; }

        [Tooltip("Items beyond this range are not drawn at all.")]

        /// <summary>Maximum distance at which items are drawn; beyond this they are invisible.</summary>
        public float DrawRadius = 60f;

        [Tooltip("Past this range items drop to the cheap silhouette mesh.")]
        /// <summary>Distance at which items switch to a cheap silhouette mesh.</summary>
        public float NearRadius = 14f;

        [Tooltip("Items spin and bob so they catch the eye, as they always have.")]
        /// <summary>Whether dropped items animate with a spin and bob to catch the player's eye.</summary>
        public bool Animate = true;

        /// <summary>Number of items drawn in the most recent frame.</summary>
        public int DrawnLastFrame { get; private set; }
        /// <summary>Number of draw batches submitted in the most recent frame.</summary>
        public int BatchesLastFrame { get; private set; }
        /// <summary>Chunks whose items were filed afresh in the most recent frame: none while nothing on the ground
        /// near the survivor changes, however long they stand there.</summary>
        public int ChunksRebuiltLastFrame { get; private set; }
        /// <summary>Chunks with items in the draw range, filed and ready to draw.</summary>
        public int ChunksInRange => _inRange.Count;
        /// <summary>Meshes looked up so far, one per look of an item type and level of detail; only ever for items
        /// that might be in view.</summary>
        public int MeshLookups { get; private set; }
        /// <summary>Whether the most recent frame only moved the items it had in view already, nothing that decides what's in
        /// view (the eye, the view, the chunks in range, the settings) having changed: what standing still costs.</summary>
        public bool ViewReusedLastFrame { get; private set; }

        /// <summary>The profiler samples the renderer's frame shows up under: the whole of it, then its three parts.</summary>
        public const string ProfilerName = "GroundItemRenderer", RefreshProfilerName = "GroundItemRenderer.Refresh",
            GatherProfilerName = "GroundItemRenderer.Gather", DrawProfilerName = "GroundItemRenderer.Draw";

        private static readonly ProfilerMarker s_Frame = new ProfilerMarker(ProfilerName);
        private static readonly ProfilerMarker s_Refresh = new ProfilerMarker(RefreshProfilerName);
        private static readonly ProfilerMarker s_Gather = new ProfilerMarker(GatherProfilerName);
        private static readonly ProfilerMarker s_Draw = new ProfilerMarker(DrawProfilerName);

        // Unity's instanced draw path takes at most 1023 matrices in one go.
        private const int BatchLimit = 1023;

        /// <summary>How fast every dropped item turns, and how far its bob lifts and drops it (metres).</summary>
        internal const float SpinDegreesPerSecond = 60f, BobHeight = 0.06f;

        /// <summary>How far an item whose mesh hasn't been looked up yet is taken to reach from where it lies (its mesh's
        /// bounds, the height it rests at and its bob), so it can be culled before the lookup. Every model in the game
        /// reaches less: 1.9 m at most (a car door), the generated meshes 1.4 m. The mesh pipeline refuses to write a mesh
        /// that reaches further (Tools/AssetPipeline/mesh/meshpost.py), and the grounditems smoke run fails on any model or
        /// mesh that does. Should a mesh looked up reach further all the same, the renderer assumes that much from then on.</summary>
        internal const float UnknownReach = 2.5f;

        private const int Outside = 0, Partly = 1, Inside = 2;

        // One item as it lies, everything about it that holds until it's picked up or moved, worked out once.
        private struct Placed
        {
            public float X, Z;
            public float Cos, Sin;         // its resting facing
            public float BobCos, BobSin;   // its bob's phase, scaled by the bob's height
            public uint Look;              // the hash that picks which look it shows (VariantHash)
            public int Variant;            // that look, once the kind's number of looks is known (Run.VariantsFor)
        }

        // One chunk's items of one kind, with the rectangle they lie in.
        private sealed class Run
        {
            public Kind Kind;
            public Placed[] Items = new Placed[8];
            public int Count;
            public int VariantsFor;   // the number of looks Items[].Variant was picked from; 0 before it's known
            public float MinX, MinZ, MaxX, MaxZ;
        }

        // A chunk's items, filed by kind.
        private sealed class ChunkCache
        {
            public GroundItemStore.Chunk Chunk;
            public int Version;       // the chunk's version when it was filed
            public int Stamp;         // the last refresh that found it in range
            public readonly List<Run> Runs = new List<Run>(8);
            public float MinX, MinZ, MaxX, MaxZ;
        }

        // One item type: how many looks it has and, per look and level of detail, its mesh and this frame's matrices.
        private sealed class Kind
        {
            public ItemDef Def;
            public uint IdHash;
            public int Looks;         // 0 until looked up (ItemMeshBaker.VariantCount, which can load meshes)
            public Group[] Groups;    // [variant * 2 + lod], made with Looks
            public int BuildStamp, BuildRun;   // its run in the chunk being filed (Rebuild)
        }

        // What one look of one kind draws at one level of detail: its mesh, looked up the first time an item that uses it
        // may be in view, and this frame's instance matrices.
        private sealed class Group
        {
            public Kind Kind;
            public int Variant;
            public ItemMeshBaker.Lod Lod;
            public bool LookedUp;
            public int LookedUpIn;              // the full gather that looked it up
            public ItemMeshBaker.Baked Baked;   // null when there's nothing to draw
            public float Height, Radius;
            public Matrix4x4[] Matrices = Array.Empty<Matrix4x4>();
            public int Count;
        }

        // An item a full gather found in view, or near enough the view's edge that its bob decides (then tested every frame).
        // Kept while nothing that decides what's in view changes, so standing still only moves these.
        private struct Shown
        {
            public Group Group;
            public float X, Z, Cos, Sin, BobCos, BobSin;
            public bool Edge;
        }

        private readonly Dictionary<long, ChunkCache> _caches = new Dictionary<long, ChunkCache>(64);
        private readonly List<ChunkCache> _inRange = new List<ChunkCache>(64);
        private readonly Stack<ChunkCache> _spareCaches = new Stack<ChunkCache>();
        private readonly Stack<Run> _spareRuns = new Stack<Run>();
        private readonly List<long> _leaving = new List<long>();
        private Kind[] _kinds = new Kind[256];

        // The groups with instances this frame, in the order they were first filled.
        private readonly List<Group> _live = new List<Group>(64);

        // What the chunks in range were last found from: the window of chunk coordinates, the store's version, the meshes'.
        private bool _primed;
        private int _minCX, _maxCX, _minCZ, _maxCZ, _storeVersion, _meshGeneration;
        private int _refreshStamp, _buildStamp, _gatherStamp;

        // Over every mesh looked up so far: how far one reaches from its centre and how high it rests. Whole chunks and
        // runs are culled against these. _unknownReach is what an item not looked up yet is taken to reach: UnknownReach,
        // or more once a mesh has been seen to reach further.
        private float _maxReach, _minHeight, _maxHeight, _unknownReach = UnknownReach;

        // The latest full gather's items in view, and what it was gathered from: valid until any of it changes.
        private Shown[] _shown = new Shown[256];
        private int _shownCount;
        private bool _shownValid, _shownAnimate;
        private Vector3 _shownEye;
        private float _shownDraw, _shownNear;
        private readonly float[] _shownView = new float[24];

        // This frame's view, eye and animation. The planes are kept unrolled too, for the tests made per item.
        private readonly Plane[] _frustum = new Plane[6];
        private readonly float[] _nx = new float[6], _ny = new float[6], _nz = new float[6], _nd = new float[6];
        private float _p0x, _p0y, _p0z, _p0d, _p1x, _p1y, _p1z, _p1d, _p2x, _p2y, _p2z, _p2d,
            _p3x, _p3y, _p3z, _p3d, _p4x, _p4y, _p4z, _p4d, _p5x, _p5y, _p5z, _p5d;
        private Vector3 _eye;
        private double _time;
        private Bounds _worldBounds;
        private bool _animate;
        private float _nearSqr, _band, _spinCos = 1f, _spinSin, _bobCos = 1f, _bobSin;

        private Matrix4x4[] _batch;
        private Camera _cachedMain;

        /// <summary>The point the most recent frame measured draw range and detail from.</summary>
        internal Vector3 Eye => _eye;

        /// <summary>The time the most recent frame's spin and bob were worked out for.</summary>
        internal double AnimatedAt => _time;

        /// <summary>Copies the view planes the most recent frame was culled against into <paramref name="into"/> (six).</summary>
        internal void CopyFrustum(Plane[] into) => Array.Copy(_frustum, into, 6);

        /// <summary>Whether this renderer has looked up <paramref name="def"/>'s looks or meshes, which it does only once one
        /// of its items may be in view.</summary>
        internal bool HasLookedUp(ItemDef def)
        {
            for (int i = 0; i < _kinds.Length; i++)
            {
                if (_kinds[i] != null && _kinds[i].Def == def) return _kinds[i].Looks > 0;
            }
            return false;
        }

        private void OnEnable()
            {
                Instance = this;
                _cachedMain = Camera.main;
            }
        private void OnDisable() { if (Instance == this) Instance = null; }

        private void LateUpdate()
        {
            // The generated meshes' cache lets go of what has gone undrawn longest while it's over budget. That happens
            // here, before anything is looked up or drawn this frame, so Refresh sees the generation it bumps and nothing let
            // go of gets drawn. It runs even with nothing to draw, because the characters' hands load meshes too.
            GeneratedItemMeshes.Trim();

            // A device that can't instance (the dedicated server's, a -nographics run's) has nothing to draw them with.
            if (!SystemInfo.supportsInstancing) return;
            if (_cachedMain == null || _cachedMain != Camera.main) _cachedMain = Camera.main;
            Camera cam = _cachedMain;
            if (cam == null) return;

            using (s_Frame.Auto())
            {
                // The isometric camera hangs well back from the survivor, so detail and draw range are
                // measured from the point it looks at; the shoulder / first-person cameras are close enough.
                _eye = ViewState.Current == CameraView.Isometric && IsoCameraFollow.Instance != null
                    ? IsoCameraFollow.FocusPoint
                    : cam.transform.position;
                GeometryUtility.CalculateFrustumPlanes(cam, _frustum);
                for (int p = 0; p < 6; p++)
                {
                    Vector3 n = _frustum[p].normal;
                    _nx[p] = n.x;
                    _ny[p] = n.y;
                    _nz[p] = n.z;
                    _nd[p] = _frustum[p].distance;
                }
                _p0x = _nx[0]; _p0y = _ny[0]; _p0z = _nz[0]; _p0d = _nd[0];
                _p1x = _nx[1]; _p1y = _ny[1]; _p1z = _nz[1]; _p1d = _nd[1];
                _p2x = _nx[2]; _p2y = _ny[2]; _p2z = _nz[2]; _p2d = _nd[2];
                _p3x = _nx[3]; _p3y = _ny[3]; _p3z = _nz[3]; _p3d = _nd[3];
                _p4x = _nx[4]; _p4y = _ny[4]; _p4z = _nz[4]; _p4d = _nd[4];
                _p5x = _nx[5]; _p5y = _ny[5]; _p5z = _nz[5]; _p5d = _nd[5];
                // Instanced draws are culled against this as a whole, so it has to cover everywhere the batch might put
                // an item, not the mesh's own extents. Made once a frame rather than once a draw.
                _worldBounds = new Bounds(_eye, Vector3.one * (DrawRadius * 2f + 10f));
                _time = Time.timeAsDouble;

                using (s_Refresh.Auto()) Refresh(_eye);
                using (s_Gather.Auto()) Gather(_eye);
                using (s_Draw.Auto()) Draw(cam);
            }
        }

        // ── Keeping the chunks in range filed ─────────────────────────────────────────────────────────

        /// <summary>Brings the filed chunks up to date. Nothing to do unless the survivor has crossed into another set
        /// of chunks or something on the ground has changed; even then a chunk is filed again only if its own contents
        /// changed.</summary>
        private void Refresh(Vector3 eye)
        {
            ChunksRebuiltLastFrame = 0;

            if (ItemMeshBaker.Generation != _meshGeneration)
            {
                _meshGeneration = ItemMeshBaker.Generation;
                ForgetMeshes();
            }

            float size = GroundItemStore.ChunkSize;
            int minCX = Mathf.FloorToInt((eye.x - DrawRadius) / size);
            int maxCX = Mathf.FloorToInt((eye.x + DrawRadius) / size);
            int minCZ = Mathf.FloorToInt((eye.z - DrawRadius) / size);
            int maxCZ = Mathf.FloorToInt((eye.z + DrawRadius) / size);

            if (_primed && GroundItemStore.Version == _storeVersion
                && minCX == _minCX && maxCX == _maxCX && minCZ == _minCZ && maxCZ == _maxCZ) return;

            _primed = true;
            _shownValid = false;
            _storeVersion = GroundItemStore.Version;
            _minCX = minCX;
            _maxCX = maxCX;
            _minCZ = minCZ;
            _maxCZ = maxCZ;
            int stamp = ++_refreshStamp;

            _inRange.Clear();
            for (int cx = minCX; cx <= maxCX; cx++)
            {
                for (int cz = minCZ; cz <= maxCZ; cz++)
                {
                    long key = GroundItemStore.Key(cx, cz);
                    if (!GroundItemStore.TryGetChunk(key, out GroundItemStore.Chunk chunk) || chunk.Items.Count == 0) continue;

                    if (!_caches.TryGetValue(key, out ChunkCache cache))
                    {
                        cache = _spareCaches.Count > 0 ? _spareCaches.Pop() : new ChunkCache();
                        _caches[key] = cache;
                    }
                    if (cache.Chunk != chunk || cache.Version != chunk.Version)
                    {
                        Rebuild(cache, chunk);
                        ChunksRebuiltLastFrame++;
                    }
                    cache.Stamp = stamp;
                    _inRange.Add(cache);
                }
            }

            if (_caches.Count == _inRange.Count) return;

            // A chunk that has left the range stays filed while it's a chunk or less outside it, so walking back and
            // forth across the edge of the range doesn't file it over and over. Further out, or gone from the store, it's
            // let go.
            _leaving.Clear();
            foreach (KeyValuePair<long, ChunkCache> kv in _caches)
            {
                if (kv.Value.Stamp == stamp) continue;
                GroundItemStore.Coords(kv.Key, out int cx, out int cz);
                bool near = cx >= minCX - 1 && cx <= maxCX + 1 && cz >= minCZ - 1 && cz <= maxCZ + 1;
                if (near && GroundItemStore.TryGetChunk(kv.Key, out GroundItemStore.Chunk chunk) && chunk == kv.Value.Chunk) continue;
                _leaving.Add(kv.Key);
            }
            for (int i = 0; i < _leaving.Count; i++)
            {
                ChunkCache cache = _caches[_leaving[i]];
                _caches.Remove(_leaving[i]);
                RecycleRuns(cache);
                cache.Chunk = null;
                _spareCaches.Push(cache);
            }
        }

        /// <summary>Files a chunk's items by kind, each with everything about it that holds while it lies there.</summary>
        private void Rebuild(ChunkCache cache, GroundItemStore.Chunk chunk)
        {
            RecycleRuns(cache);
            cache.Chunk = chunk;
            cache.Version = chunk.Version;
            int stamp = ++_buildStamp;

            List<GroundItemStore.Item> items = chunk.Items;
            for (int i = 0; i < items.Count; i++)
            {
                GroundItemStore.Item item = items[i];
                Kind kind = KindOf(item.TypeIndex);
                if (kind == null) continue;

                Run run;
                if (kind.BuildStamp == stamp) run = cache.Runs[kind.BuildRun];
                else
                {
                    run = _spareRuns.Count > 0 ? _spareRuns.Pop() : new Run();
                    run.Kind = kind;
                    run.Count = 0;
                    run.VariantsFor = 0;
                    run.MinX = run.MinZ = float.MaxValue;
                    run.MaxX = run.MaxZ = float.MinValue;
                    kind.BuildStamp = stamp;
                    kind.BuildRun = cache.Runs.Count;
                    cache.Runs.Add(run);
                }

                if (run.Count == run.Items.Length) Array.Resize(ref run.Items, run.Count * 2);
                ref Placed p = ref run.Items[run.Count++];
                p.X = item.X;
                p.Z = item.Z;
                float yaw = item.YawDegrees * Mathf.Deg2Rad;
                p.Cos = Mathf.Cos(yaw);
                p.Sin = Mathf.Sin(yaw);
                // Spread the bob by id so a pile does not pulse as one: it goes as sin(2 (t + id × 0.61803)).
                double phase = 2.0 * (item.Id * 0.61803f);
                p.BobCos = (float)(Math.Cos(phase) * BobHeight);
                p.BobSin = (float)(Math.Sin(phase) * BobHeight);
                p.Look = VariantHash(item, kind.IdHash);
                p.Variant = 0;

                if (item.X < run.MinX) run.MinX = item.X;
                if (item.X > run.MaxX) run.MaxX = item.X;
                if (item.Z < run.MinZ) run.MinZ = item.Z;
                if (item.Z > run.MaxZ) run.MaxZ = item.Z;
            }

            cache.MinX = cache.MinZ = float.MaxValue;
            cache.MaxX = cache.MaxZ = float.MinValue;
            for (int r = 0; r < cache.Runs.Count; r++)
            {
                Run run = cache.Runs[r];
                cache.MinX = Mathf.Min(cache.MinX, run.MinX);
                cache.MinZ = Mathf.Min(cache.MinZ, run.MinZ);
                cache.MaxX = Mathf.Max(cache.MaxX, run.MaxX);
                cache.MaxZ = Mathf.Max(cache.MaxZ, run.MaxZ);
            }
        }

        private void RecycleRuns(ChunkCache cache)
        {
            for (int r = 0; r < cache.Runs.Count; r++) _spareRuns.Push(cache.Runs[r]);
            cache.Runs.Clear();
        }

        private Kind KindOf(int typeIndex)
        {
            if (typeIndex < 0) return null;
            if (typeIndex >= _kinds.Length) Array.Resize(ref _kinds, Mathf.Max(typeIndex + 1, _kinds.Length * 2));
            Kind kind = _kinds[typeIndex];
            if (kind != null) return kind;

            ItemDef def = GroundItemStore.TypeAt(typeIndex);
            if (def == null) return null;
            kind = new Kind { Def = def, IdHash = IdHash(def) };
            _kinds[typeIndex] = kind;
            return kind;
        }

        /// <summary>Drops every mesh looked up, after <see cref="ItemMeshBaker.Generation"/> says they were destroyed; each is
        /// looked up again when next wanted.</summary>
        private void ForgetMeshes()
        {
            for (int i = 0; i < _live.Count; i++) _live[i].Count = 0;
            _live.Clear();
            _shownValid = false;
            Array.Clear(_shown, 0, _shownCount);
            _shownCount = 0;
            for (int i = 0; i < _kinds.Length; i++)
            {
                Kind kind = _kinds[i];
                if (kind == null) continue;
                kind.Looks = 0;
                kind.Groups = null;
            }
            _maxReach = _minHeight = _maxHeight = 0f;
        }

        /// <summary>Which of an item's looks a ground item shows: a hash of its type and where it lies, so every
        /// client and every reload agree without the choice being stored anywhere.</summary>
        private static uint VariantHash(in GroundItemStore.Item item, uint typeHash)
        {
            uint h = typeHash;
            h ^= (uint)Mathf.RoundToInt(item.X * 4f) * 0x9E3779B1u;
            h ^= (uint)Mathf.RoundToInt(item.Z * 4f) * 0x85EBCA77u;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return h;
        }

        // FNV-1a of an item type's id: its index differs between sessions, its id doesn't.
        private static uint IdHash(ItemDef def)
        {
            uint h = 2166136261u;
            if (def.Id != null) foreach (char c in def.Id) h = (h ^ c) * 16777619u;
            return h == 0 ? 1u : h;
        }

        // ── Each frame ────────────────────────────────────────────────────────────────────────────────

        /// <summary>Fills this frame's instance matrices with every item in range that may be in view. While nothing that
        /// decides what's in view has changed since the last full gather, it only moves the items that one found (standing
        /// still); otherwise it culls the chunks in range afresh.</summary>
        private void Gather(Vector3 eye)
        {
            for (int i = 0; i < _live.Count; i++) _live[i].Count = 0;
            _live.Clear();

            _animate = Animate;
            if (_animate)
            {
                // The turn and the bob every item shares, worked out once; each item adds its own facing and phase.
                double spin = (_time * SpinDegreesPerSecond % 360.0) * (Math.PI / 180.0);
                double bob = (_time * 2.0) % (2.0 * Math.PI);
                _spinCos = (float)Math.Cos(spin);
                _spinSin = (float)Math.Sin(spin);
                _bobCos = (float)Math.Cos(bob);
                _bobSin = (float)Math.Sin(bob);
            }

            ViewReusedLastFrame = _shownValid && _shownAnimate == _animate && _shownDraw == DrawRadius
                && _shownNear == NearRadius && _shownEye.x == eye.x && _shownEye.y == eye.y && _shownEye.z == eye.z && SameView();
            if (ViewReusedLastFrame)
            {
                for (int i = 0; i < _shownCount; i++) Show(ref _shown[i]);
            }
            else GatherAll(eye);

            int drawn = 0;
            for (int i = 0; i < _live.Count; i++) drawn += _live[i].Count;
            DrawnLastFrame = drawn;
        }

        /// <summary>Culls the chunks in range against the view afresh and keeps what may be in view for the frames after, as
        /// long as nothing that decides it changes.</summary>
        private void GatherAll(Vector3 eye)
        {
            Array.Clear(_shown, 0, _shownCount);
            _shownCount = 0;
            _gatherStamp++;
            float unknownReach = _unknownReach;
            float drawSqr = DrawRadius * DrawRadius;
            _nearSqr = NearRadius * NearRadius;
            // How far the bob can carry an item across a plane of the view, and a millimetre for rounding: an item further
            // in than that is in view whatever the bob does, further out it never is, and in between it's tested each frame.
            float bobReach = _animate ? BobHeight : 0f;
            _band = bobReach + 0.001f;

            // How far any item may reach from where it lies, and how high its centre may ride, for culling whole chunks and
            // runs before anything in them is touched: from the meshes looked up so far, and an item whose mesh isn't yet is
            // taken to sit on the ground and reach _unknownReach.
            float reach = Mathf.Max(_unknownReach, _maxReach);
            float yLo = Mathf.Min(0f, _minHeight) - bobReach;
            float yHi = Mathf.Max(0f, _maxHeight) + bobReach;

            for (int c = 0; c < _inRange.Count; c++)
            {
                ChunkCache cache = _inRange[c];
                if (Beyond(cache.MinX, cache.MinZ, cache.MaxX, cache.MaxZ, eye, drawSqr)) continue;
                int chunkView = Classify(cache.MinX, yLo, cache.MinZ, cache.MaxX, yHi, cache.MaxZ, reach);
                if (chunkView == Outside) continue;

                List<Run> runs = cache.Runs;
                for (int r = 0; r < runs.Count; r++)
                {
                    Run run = runs[r];
                    if (Beyond(run.MinX, run.MinZ, run.MaxX, run.MaxZ, eye, drawSqr)) continue;
                    int view = chunkView == Inside ? Inside : Classify(run.MinX, yLo, run.MinZ, run.MaxX, yHi, run.MaxZ, reach);
                    if (view != Outside) GatherRun(run, view == Inside, eye, drawSqr);
                }
            }

            // Kept for the frames after, unless a mesh looked up just now reached further than items not looked up were
            // taken to: some may have been passed over, so the next frame culls afresh.
            _shownValid = _unknownReach == unknownReach;
            _shownAnimate = _animate;
            _shownDraw = DrawRadius;
            _shownNear = NearRadius;
            _shownEye = eye;
            for (int p = 0; p < 6; p++)
            {
                _shownView[p * 4] = _nx[p];
                _shownView[p * 4 + 1] = _ny[p];
                _shownView[p * 4 + 2] = _nz[p];
                _shownView[p * 4 + 3] = _nd[p];
            }
        }

        private bool SameView()
        {
            for (int p = 0; p < 6; p++)
            {
                if (_shownView[p * 4] != _nx[p] || _shownView[p * 4 + 1] != _ny[p] || _shownView[p * 4 + 2] != _nz[p]
                    || _shownView[p * 4 + 3] != _nd[p]) return false;
            }
            return true;
        }

        /// <summary>Finds one run's items that may be in view, keeps them (Shown) and puts them in this frame's matrices.
        /// With <paramref name="inView"/> every centre in the run is known to be in view, so nothing in it is tested.</summary>
        private void GatherRun(Run run, bool inView, Vector3 eye, float drawSqr)
        {
            Kind kind = run.Kind;
            Placed[] items = run.Items;
            int count = run.Count, stamp = _gatherStamp;
            float ex = eye.x, ez = eye.z, nearSqr = _nearSqr, band = _band;

            for (int i = 0; i < count; i++)
            {
                ref Placed p = ref items[i];
                float dx = p.X - ex;
                float dz = p.Z - ez;
                float d2 = dx * dx + dz * dz;
                if (d2 > drawSqr) continue;

                // A kind's looks and meshes are looked up only once one of its items may be in view, since the lookup can
                // load them from disk; until then the item is culled as if it reached _unknownReach from where it lies.
                if (kind.Looks == 0 || run.VariantsFor != kind.Looks)
                {
                    if (kind.Looks == 0)
                    {
                        if (!inView && !InView(p.X, 0f, p.Z, _unknownReach)) continue;
                        LookUpLooks(kind);
                    }
                    if (run.VariantsFor != kind.Looks) PickLooks(run);
                }

                Group g = kind.Groups[p.Variant * 2 + (d2 <= nearSqr ? 0 : 1)];
                if (!g.LookedUp)
                {
                    if (!inView && !InView(p.X, 0f, p.Z, _unknownReach)) continue;
                    LookUp(g);
                }
                if (g.Baked == null) continue;   // no model; nothing to draw

                // Where its bounds reach against the view with the item at rest: in view whatever its bob does, out of view
                // whatever it does, or on the edge. In a run wholly in view every centre is in view, except one whose mesh
                // was looked up just now (the run was culled before its height was known).
                bool edge;
                if (inView) edge = g.LookedUpIn == stamp;
                else
                {
                    float margin = Margin(p.X, g.Height, p.Z, g.Radius, -band);
                    if (margin < -band) continue;
                    edge = margin < band;
                }

                if (_shownCount == _shown.Length) Array.Resize(ref _shown, _shownCount * 2);
                ref Shown s = ref _shown[_shownCount++];
                s.Group = g;
                s.X = p.X;
                s.Z = p.Z;
                s.Cos = p.Cos;
                s.Sin = p.Sin;
                s.BobCos = p.BobCos;
                s.BobSin = p.BobSin;
                s.Edge = edge;
                Show(ref s);
            }
        }

        /// <summary>Puts one item into its group's matrices for this frame: lifted by its bob, tested against the view if the
        /// bob decides whether it's in it, and turned by its spin.</summary>
        private void Show(ref Shown s)
        {
            Group g = s.Group;
            float y = g.Height, cos = s.Cos, sin = s.Sin;
            if (_animate)
            {
                // The same spin and bob every dropped item used to do in its own Update: the shared bob shifted by the
                // item's own phase, the shared turn added to its own facing.
                y += _bobSin * s.BobCos + _bobCos * s.BobSin;
                if (s.Edge && !InView(s.X, y, s.Z, g.Radius)) return;
                float turnedCos = cos * _spinCos - sin * _spinSin;
                sin = sin * _spinCos + cos * _spinSin;
                cos = turnedCos;
            }
            else if (s.Edge && !InView(s.X, y, s.Z, g.Radius)) return;

            if (g.Count == 0) _live.Add(g);
            if (g.Count == g.Matrices.Length) Grow(g);
            // A turn about the vertical and a position: the rest of the matrix never changes (see Grow).
            ref Matrix4x4 m = ref g.Matrices[g.Count++];
            m.m00 = cos;
            m.m02 = sin;
            m.m03 = s.X;
            m.m13 = y;
            m.m20 = -sin;
            m.m22 = cos;
            m.m23 = s.Z;
        }

        private void LookUpLooks(Kind kind)
        {
            kind.Looks = ItemMeshBaker.VariantCount(kind.Def);
            kind.Groups = new Group[kind.Looks * 2];
            for (int v = 0; v < kind.Looks; v++)
            {
                kind.Groups[v * 2] = new Group { Kind = kind, Variant = v, Lod = ItemMeshBaker.Lod.Near };
                kind.Groups[v * 2 + 1] = new Group { Kind = kind, Variant = v, Lod = ItemMeshBaker.Lod.Far };
            }
        }

        private static void PickLooks(Run run)
        {
            int looks = run.Kind.Looks;
            for (int i = 0; i < run.Count; i++) run.Items[i].Variant = looks > 1 ? (int)(run.Items[i].Look % (uint)looks) : 0;
            run.VariantsFor = looks;
        }

        private void LookUp(Group g)
        {
            g.LookedUp = true;
            g.LookedUpIn = _gatherStamp;
            g.Baked = ItemMeshBaker.Get(g.Kind.Def, g.Lod, g.Variant);
            MeshLookups++;
            if (g.Baked == null) return;
            g.Height = g.Baked.BaseHeight;
            g.Radius = g.Baked.Bounds.extents.magnitude;
            _maxReach = Mathf.Max(_maxReach, g.Radius);
            _minHeight = Mathf.Min(_minHeight, g.Height);
            _maxHeight = Mathf.Max(_maxHeight, g.Height);
            // Bigger than items not looked up yet are taken to be: assume this much of them from now on (GatherAll then
            // culls afresh next frame, in case one was passed over this frame).
            float reach = g.Radius + Mathf.Abs(g.Height) + BobHeight;
            if (reach > _unknownReach) _unknownReach = reach;
        }

        // Room for more matrices. Every slot starts as the identity: an item only ever writes its turn and position.
        private static void Grow(Group g)
        {
            var bigger = new Matrix4x4[Mathf.Max(16, g.Matrices.Length * 2)];
            Array.Copy(g.Matrices, bigger, g.Count);
            for (int i = g.Count; i < bigger.Length; i++) bigger[i] = Matrix4x4.identity;
            g.Matrices = bigger;
        }

        // Whether a sphere is in view: in front of, or within its radius behind, each of the view's planes.
        private bool InView(float x, float y, float z, float radius)
        {
            float r = -radius;
            return _p0x * x + _p0y * y + _p0z * z + _p0d >= r
                && _p1x * x + _p1y * y + _p1z * z + _p1d >= r
                && _p2x * x + _p2y * y + _p2z * z + _p2d >= r
                && _p3x * x + _p3y * y + _p3z * z + _p3d >= r
                && _p4x * x + _p4y * y + _p4z * z + _p4d >= r
                && _p5x * x + _p5y * y + _p5z * z + _p5d >= r;
        }

        // How far inside the view a sphere reaches: the least, over the view's planes, of how far it reaches in front of one
        // (below 0 it's out of view). Stops early once that's below `floor`.
        private float Margin(float x, float y, float z, float radius, float floor)
        {
            float m = _p0x * x + _p0y * y + _p0z * z + _p0d + radius;
            if (m < floor) return m;
            float d = _p1x * x + _p1y * y + _p1z * z + _p1d + radius;
            if (d < m && (m = d) < floor) return m;
            d = _p2x * x + _p2y * y + _p2z * z + _p2d + radius;
            if (d < m && (m = d) < floor) return m;
            d = _p3x * x + _p3y * y + _p3z * z + _p3d + radius;
            if (d < m && (m = d) < floor) return m;
            d = _p4x * x + _p4y * y + _p4z * z + _p4d + radius;
            if (d < m && (m = d) < floor) return m;
            d = _p5x * x + _p5y * y + _p5z * z + _p5d + radius;
            return d < m ? d : m;
        }

        /// <summary>Whether items centred anywhere in a box, each reaching up to <paramref name="reach"/> from its centre,
        /// can be in view: Outside when none can (the whole box is more than that behind one of the view's planes), Inside
        /// when every centre in it is in view, otherwise Partly.</summary>
        private int Classify(float x0, float y0, float z0, float x1, float y1, float z1, float reach)
        {
            bool inside = true;
            for (int p = 0; p < 6; p++)
            {
                float nx = _nx[p], ny = _ny[p], nz = _nz[p];
                float front = _nd[p] + nx * (nx >= 0f ? x1 : x0) + ny * (ny >= 0f ? y1 : y0) + nz * (nz >= 0f ? z1 : z0);
                if (front < -reach) return Outside;
                if (inside && _nd[p] + nx * (nx >= 0f ? x0 : x1) + ny * (ny >= 0f ? y0 : y1) + nz * (nz >= 0f ? z0 : z1) < 0f)
                    inside = false;
            }
            return inside ? Inside : Partly;
        }

        // Whether all of a rectangle on the ground is further than the draw range from the eye.
        private static bool Beyond(float x0, float z0, float x1, float z1, Vector3 eye, float rangeSqr)
        {
            float dx = eye.x < x0 ? x0 - eye.x : eye.x > x1 ? eye.x - x1 : 0f;
            float dz = eye.z < z0 ? z0 - eye.z : eye.z > z1 ? eye.z - z1 : 0f;
            return dx * dx + dz * dz > rangeSqr;
        }

        /// <summary>Visits every item drawn last frame — its instance matrix, mesh and kind — for Debug Mode's picker,
        /// which has to draw them too (they have no scene objects of their own).</summary>
        internal void VisitDrawn(Action<Matrix4x4, Mesh, ItemDef> visit)
        {
            for (int i = 0; i < _live.Count; i++)
            {
                Group g = _live[i];
                if (g.Baked == null || g.Baked.Mesh == null) continue;
                for (int k = 0; k < g.Count; k++) visit(g.Matrices[k], g.Baked.Mesh, g.Kind.Def);
            }
        }

        /// <summary>Issues one instanced draw per group, per submesh.</summary>
        private void Draw(Camera cam)
        {
            BatchesLastFrame = 0;
            float now = Time.unscaledTime;
            int frame = Time.frameCount;

            for (int i = 0; i < _live.Count; i++)
            {
                Group g = _live[i];
                ItemMeshBaker.Baked baked = g.Baked;
                if (baked.Mesh == null) continue;   // destroyed without bumping ItemMeshBaker.Generation: nothing to draw with
                baked.MarkUsed(now, frame);   // drawn, so the generated meshes' cache keeps it (GeneratedItemMeshes.Trim)

                for (int sub = 0; sub < baked.Materials.Length; sub++)
                {
                    var rp = new RenderParams(baked.Materials[sub])
                    {
                        camera = cam,
                        receiveShadows = true,
                        worldBounds = _worldBounds,
                        shadowCastingMode = g.Lod == ItemMeshBaker.Lod.Near ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    };

                    if (g.Count <= BatchLimit)
                    {
                        Graphics.RenderMeshInstanced(rp, baked.Mesh, sub, g.Matrices, g.Count);
                        BatchesLastFrame++;
                        continue;
                    }

                    if (_batch == null) _batch = new Matrix4x4[BatchLimit];
                    for (int start = 0; start < g.Count; start += BatchLimit)
                    {
                        int n = Mathf.Min(BatchLimit, g.Count - start);
                        Array.Copy(g.Matrices, start, _batch, 0, n);
                        Graphics.RenderMeshInstanced(rp, baked.Mesh, sub, _batch, n);
                        BatchesLastFrame++;
                    }
                }
            }
        }
    }
}
