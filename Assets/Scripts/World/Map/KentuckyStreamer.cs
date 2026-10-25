using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The whole of Kentucky as one raid map. The world store holds the state in 8 km regions (Tools/World); this
    /// builds it around the survivors in 128 m pieces with <see cref="KentuckyMap"/>'s own builders, and lets go of the
    /// pieces left behind, so a survivor can walk or drive from the start town to anywhere in the state. Detail comes in
    /// rings: the pieces next to a survivor in Full (and the ones they're heading into), the ring past them in Lite
    /// (shells, roofs, roads and ground: about a fifth of the work), and a Lite piece is built again in Full (and swapped
    /// in when it's done) as a survivor comes near. The nearest pieces come first, then the ones ahead of the way the
    /// survivor travels or looks; building takes a few milliseconds a frame, each step weighed beforehand by what it took
    /// last time. Regions are read and sorted, and each piece's data cut out, off the main thread, and a piece's meshes
    /// are packed on a worker. The NavMesh is baked around the survivors in the background as they move (agents it
    /// leaves behind wait, and are put back on it when it returns); the arena square that spawns, the A-Life and the
    /// clamps work in follows them (<see cref="CloneZConfig.ArenaCenter"/>), with the build's regions as the edge of the
    /// world (<see cref="CloneZConfig.HardBounds"/>); a piece keeps what was taken from its containers when it's unloaded
    /// and built again. The start town's pieces stay built (in Full) all raid: the exits, the landing zones and the
    /// zone's overlays stand there.
    /// World coordinates stay metres from the start town's centre, as on the fixed square, so the camps, cars and map
    /// marks saved in the zone keep their places. Map data © OpenStreetMap contributors (ODbL).
    /// </summary>
    public sealed class KentuckyStreamer : MonoBehaviour
    {
        /// <summary>The current raid's streamer, or null on a fixed map.</summary>
        public static KentuckyStreamer Active { get; private set; }

        private const int FullRadius = 1;          // pieces either side of a survivor's own built in Full...
        private const int LoadRadius = 2;          // ...and in Lite out to this
        private const int KeepRadius = 3;          // built pieces are let go past this
        private const int PrefetchRadius = 6;      // regions are read this many pieces ahead
        private const int MaxRegionLoads = 2;      // regions being read at once
        private const int MaxRegionsKept = 6;
        private const int MaxAreaReads = 2;        // pieces' data being cut out of their regions at once
        private const int MemoryKept = 1024;       // unloaded pieces whose containers are remembered
        private const float BuildBudget = 0.005f;  // seconds a frame spent building...
        private const float RingBudget = 0.008f;   // ...or this, while a piece next to the local survivor isn't shown in Full...
        private const float CatchUpBudget = 0.04f; // ...or this, while the ground under a survivor isn't built yet
        private const float MaxDebt = 0.01f;       // a frame that ran over its budget is paid back over the next ones, up to this
        private const float AheadSpeed = 3f;       // m/s: faster than this, the pieces ahead are wanted in Full before they're next door
        private const float NavInterval = 0.25f;   // seconds between NavMesh updates at most
        private const float MaxNavSpan = 1600f;    // survivors farther apart than this: the NavMesh stays with the local one
        private const float NavMinY = -20f, NavMaxY = 60f;
        private const int UnloadsPerAssetSweep = 64;
        private const int AgentType = 0;           // humanoid, as the fixed maps' NavMeshSurface bakes

        /// <summary>The raid's world root: the pieces are built under it, and the NavMesh is baked from it.</summary>
        public Transform World { get; private set; }

        /// <summary>What has been built so far, for the logs.</summary>
        public string Summary => _ready + " pieces built (" + _pinned + " kept for the start, " + _readyLite + " in Lite, " + _upgraded
            + " built again in Full), " + _buildings + " buildings, " + _containers + " containers, " + _lights + " lights, " + _trees + " trees";

        /// <summary>How far from a survivor's piece the state is kept built (in some detail), in metres.</summary>
        public float BuiltReach => (LoadRadius + 0.5f) * _chunk;

        /// <summary>How far from a survivor's piece the state is built in Full detail (interiors, props, colliders), in
        /// metres.</summary>
        public float FullReach => (FullRadius + 0.5f) * _chunk;

        private sealed class Chunk
        {
            public int Cx, Cz;
            public long Key;
            public bool Pinned, Ready, Failed, SourcesDirty;
            public KentuckyMap.Detail Detail;
            public Vector3 Middle, UvOrigin;
            public GameObject Root, Overhead;
            public KentuckyMap.ChunkJob Job;
            public IEnumerator<string> Steps;
            public string LastStep;
            public HashSet<Vector3Int> Containers;
            public Bounds SourceBounds;
            /// <summary>The piece built again in Full while this one (in Lite) is shown; swapped in when it's done.</summary>
            public Chunk Upgrade;
            /// <summary>For an upgrade: the piece it replaces.</summary>
            public Chunk Replaces;
            /// <summary>Its upgrade failed: it stays in Lite.</summary>
            public bool NoUpgrade;
            /// <summary>How much there is to build (see WeightOf), once its data is read.</summary>
            public float Weight;
            public readonly List<Mesh> Meshes = new List<Mesh>();
            public readonly List<NavMeshBuildSource> Sources = new List<NavMeshBuildSource>();
            /// <summary>The sign textures the piece's build asked for, and (once it's counted) the distinct ones.</summary>
            public readonly List<string> Signs = new List<string>();
            public HashSet<string> SignKeys;
        }

        private sealed class RegionSlot
        {
            public int Rx, Rz;
            public Task<MapRegionIndex> Load;
            public MapRegionIndex Index;
            public float LastUsed;
        }

        private struct KeptContainer
        {
            public Inventory Contents;
            public bool Searched;
        }

        private sealed class ChunkMemory
        {
            public readonly Dictionary<Vector3Int, KeptContainer> Containers = new Dictionary<Vector3Int, KeptContainer>();
            public readonly HashSet<Vector3Int> Gone = new HashSet<Vector3Int>();
            public int Stamp;
        }

        private struct Want
        {
            public int Cx, Cz, Priority;
            public KentuckyMap.Detail Detail;
        }

        // How long a step of a piece's build took last time, by the step before it (the passes yield in a fixed order), for
        // each detail, and how much the pieces it was learned on had to build (see WeightOf): weighed before the step is
        // run, so a frame doesn't start a long one it has no time left for.
        private sealed class StepCost
        {
            public double Ms, Weight;
            public int Seen;
        }

        private static readonly Comparison<Want> s_ByPriority = (a, b) => a.Priority.CompareTo(b.Priority);

        private MapBuild _build;
        private MapPlace _place;
        private double _originX, _originZ;   // the grid point at the world's origin: the start town's centre
        private int _chunk, _perRegion, _layoutSeed, _raidSeed;
        private float _activityHalf;
        private bool _started;
        private Transform _streamed, _overhead;
        private GameObject _ground, _horizon;
        private Chunk _building;

        private readonly Dictionary<long, Chunk> _chunks = new Dictionary<long, Chunk>();
        private readonly Dictionary<long, RegionSlot> _regions = new Dictionary<long, RegionSlot>();
        private readonly Dictionary<long, ChunkMemory> _memory = new Dictionary<long, ChunkMemory>();
        private readonly Queue<(long key, int stamp)> _memoryOrder = new Queue<(long, int)>();
        private int _memoryStamp;
        private readonly List<Vector3> _focus = new List<Vector3>(4);
        private readonly List<Want> _wanted = new List<Want>(32);
        private readonly Dictionary<long, int> _wantedAt = new Dictionary<long, int>(32);
        private readonly List<Chunk> _scratch = new List<Chunk>(16);
        private readonly List<Mesh> _deadMeshes = new List<Mesh>();
        // How many built pieces show each sign texture; one nobody shows any more is let go of.
        private readonly Dictionary<string, int> _signUsers = new Dictionary<string, int>();
        // Pieces' data being cut out of their regions on workers, ahead of their builds.
        private readonly Dictionary<long, Task<KentuckyMap.Area>> _areaReads = new Dictionary<long, Task<KentuckyMap.Area>>();
        private readonly List<long> _staleReads = new List<long>(8);
        private readonly Dictionary<string, StepCost>[] _stepCosts = { new Dictionary<string, StepCost>(), new Dictionary<string, StepCost>() };
        private float _debt;
        // The local survivor's travel (m/s on the ground, smoothed) and where the camera looks (unit, on the ground).
        private Vector2 _velocity, _look;
        private Vector3 _lastFocus;
        private double _lastFocusAt;
        private bool _haveLastFocus;

        // The NavMesh around the survivors.
        private NavMeshData _navData;
        private NavMeshDataInstance _navInstance;
        private NavMeshBuildSettings _navSettings;
        private AsyncOperation _navOp;
        private Bounds _navOpBounds, _navBuilt, _navWanted;
        private bool _navReady, _navDirty, _staticDirty;
        private float _nextNav, _nextAgents, _nextPrune;
        private readonly List<NavMeshBuildSource> _static = new List<NavMeshBuildSource>();
        private readonly List<Bounds> _staticBounds = new List<Bounds>();
        private readonly List<NavMeshBuildSource> _navSources = new List<NavMeshBuildSource>();
        private readonly List<long> _navKeys = new List<long>(64);

        private int _ready, _readyLite, _pinned, _upgraded, _buildings, _containers, _lights, _trees, _unloadsSinceSweep;
        private bool _prune;

        #region Setting up
        internal static KentuckyStreamer Create(Transform world, ZoneDef zone, MapBuild build, MapPlace place)
        {
            var go = new GameObject("KentuckyStreamer");
            var streamer = go.AddComponent<KentuckyStreamer>();
            streamer.Init(world, zone, build, place);
            return streamer;
        }

        private void Init(Transform world, ZoneDef zone, MapBuild build, MapPlace place)
        {
            Active = this;
            World = world;
            _build = build;
            _place = place;
            _originX = place.x;
            _originZ = place.z;
            _chunk = KentuckyMap.ChunkSize;
            _perRegion = build.RegionSize / _chunk;
            _layoutSeed = zone.LayoutSeed;
            _raidSeed = GameSession.RaidSeed;
            _activityHalf = Mathf.Clamp(zone.RealHalfSize, 96f, BuiltReach - 48f);
            _streamed = new GameObject("Streamed").transform;
            _streamed.SetParent(world, false);
            _overhead = new GameObject("RaidOverhead").transform;
            _overhead.gameObject.AddComponent<CeilingVisibility>();
            if (build.TryGetExtent(out double x0, out double z0, out double x1, out double z1))
                CloneZConfig.HardBounds = Rect.MinMaxRect((float)(x0 - _originX), (float)(z0 - _originZ), (float)(x1 - _originX), (float)(z1 - _originZ));
            MakeGround();
        }

        // Plain snow under every piece not built yet (a collider, so nobody falls out of the world while the pieces
        // under them are on their way) and a wide snowfield to the horizon; both follow the survivor.
        private void MakeGround()
        {
            float side = (2 * KeepRadius + 1) * _chunk;
            _ground = new GameObject("Ground");
            _ground.transform.SetParent(transform, false);
            var col = _ground.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.5f, 0f);
            col.size = new Vector3(side, 1f, side);

            _horizon = new GameObject("Horizon");
            _horizon.transform.SetParent(transform, false);
            var field = new RaidMesh();
            field.Flat(new Vector3(0f, -0.04f, 0f), 2400f, 2400f, 0f, DecorPalette.Matte(new Color(0.84f, 0.87f, 0.92f)));
            List<Mesh> outer = RaidMesh.CreatedMeshes;
            RaidMesh.CreatedMeshes = _deadMeshesOnExit;
            field.ApplyTo(_horizon, Vector3.zero, ShadowCastingMode.Off);
            RaidMesh.CreatedMeshes = outer;
            DecorPalette.Apply();
        }

        private readonly List<Mesh> _deadMeshesOnExit = new List<Mesh>();

        /// <summary>Builds the start square's pieces now (they stay built all raid), then lays out the raid's exits and
        /// the survivor's spawn on <paramref name="info"/> from the square's data.</summary>
        internal void BuildStart(float half, RaidMapInfo info)
        {
            IEnumerator<float> steps = BuildStartSteps(half, info);
            while (steps.MoveNext()) { }
        }

        /// <summary>The start square's build so far, as a stage of the passes (see <see cref="KentuckyMap.StageOf"/>).</summary>
        internal string StartStage { get; private set; } = "";

        /// <summary>As <see cref="BuildStart"/>, a step at a time: yields how far through the start square it is (0 to 1)
        /// after every step of every piece, so a loading screen can show the town going up. The square's data is read
        /// first, and the lot asked for (the hub's bar: <see cref="KentuckyMap.WantLot"/>) is kept clear in every piece.</summary>
        internal IEnumerator<float> BuildStartSteps(float half, RaidMapInfo info)
        {
            var start = new KentuckyMap.Area(_build, _place, half, half, RegionsAround);
            KentuckyMap.PlanLot(start, half);
            int cx0 = ChunkX(-half), cx1 = ChunkX(half), cz0 = ChunkZ(-half), cz1 = ChunkZ(half);
            int total = (cx1 - cx0 + 1) * (cz1 - cz0 + 1), done = 0;
            for (int cz = cz0; cz <= cz1; cz++)
                for (int cx = cx0; cx <= cx1; cx++)
                {
                    Chunk c = Begin(cx, cz, RegionNow(RegionOf(cx), RegionOf(cz)), KentuckyMap.Detail.Full, null);
                    c.Job.Synchronous = true;
                    c.Pinned = true;
                    _chunks[c.Key] = c;
                    _pinned++;
                    // Timed as the streamer times its steps: the start teaches it what a Full piece's steps cost.
                    string before = Starting;
                    int steps = 0;
                    while (true)
                    {
                        double t0 = Now;
                        bool more = Step(c);
                        Learn(c, before, (Now - t0) * 1000.0);
                        if (!more) break;
                        before = c.LastStep;
                        StartStage = c.LastStep;
                        steps++;
                        // A piece's steps aren't counted beforehand: its share creeps towards whole as they come.
                        yield return (done + 0.95f * steps / (steps + 40f)) / total;
                    }
                    Finish(c);
                    done++;
                    yield return (float)done / total;
                }
            KentuckyMap.PlanStart(start, half, info, StartFree);
            foreach (Chunk c in _chunks.Values) c.Job = null;   // the start's sites were kept for the spawn only
            _started = true;
        }

        // A spot (world metres) clear of the start's buildings, props and roads, as the piece that holds it sees it.
        private bool StartFree(Vector2 p)
        {
            if (!_chunks.TryGetValue(Key(ChunkX(p.x), ChunkZ(p.y)), out Chunk c) || c.Job == null || c.Job.Site == null) return false;
            return c.Job.Site.FreeBeside(new Vector2(p.x - c.Middle.x, p.y - c.Middle.z), 0.6f);
        }

        // The regions overlapping a square around a grid point, read now (the start's data).
        private List<MapRegion> RegionsAround(double x, double z, double reach)
        {
            int size = _build.RegionSize;
            int rx0 = (int)Math.Floor((x - reach) / size), rx1 = (int)Math.Floor((x + reach) / size);
            int rz0 = (int)Math.Floor((z - reach) / size), rz1 = (int)Math.Floor((z + reach) / size);
            var found = new List<MapRegion>();
            for (int rx = rx0; rx <= rx1; rx++)
                for (int rz = rz0; rz <= rz1; rz++)
                {
                    MapRegionIndex index = RegionNow(rx, rz);
                    if (index.Data != null) found.Add(index.Data);
                }
            return found;
        }

        /// <summary>Bakes the NavMesh over the start now (the bootstrapper's bake), then keeps it baked around the
        /// survivors in the background.</summary>
        internal void BakeNow()
        {
            if (_navReady) return;   // baked already (once is all the start needs; the rest is kept up in the background)
            _navSettings = NavMesh.GetSettingsByID(AgentType);
            if (_navSettings.agentTypeID == -1) _navSettings.agentTypeID = AgentType;
            _navSettings.minRegionArea = 2f;
            _navData = new NavMeshData(AgentType) { name = "Kentucky" };
            _navInstance = NavMesh.AddNavMeshData(_navData);
            CollectStatic();
            foreach (Chunk c in _chunks.Values) if (c.Ready && c.SourcesDirty) CollectSources(c);
            Bounds start = PinnedBounds();
            Gather(start);
            NavMeshBuilder.UpdateNavMeshData(_navData, _navSettings, _navSources, start);
            _navBuilt = _navWanted = start;
            _navReady = true;
            WorldNavMesh.RegisterStreamed(World, RequestRebuild, b => Invalidate(b, true));
        }

        /// <summary>Undoes everything built (the start failed); the world root is left as it was.</summary>
        internal void Discard()
        {
            foreach (Chunk c in _chunks.Values)
            {
                DropUpgrade(c, true);
                Shelter.Forget(c);
                DestroyMeshes(c.Meshes);
                if (c.Root != null) DestroyImmediate(c.Root);
                if (c.Overhead != null) DestroyImmediate(c.Overhead);
            }
            _chunks.Clear();
            if (_streamed != null) DestroyImmediate(_streamed.gameObject);
            if (_overhead != null) DestroyImmediate(_overhead.gameObject);
            CeilingVisibility.Clear();
            DestroyImmediate(gameObject);
        }

        private void OnDestroy()
        {
            bool current = Active == this;
            if (current) Active = null;
            if (_navData != null)
            {
                if (_navOp != null && !_navOp.isDone) NavMeshBuilder.Cancel(_navData);
                _navInstance.Remove();
                Destroy(_navData);
            }
            foreach (Chunk c in _chunks.Values)
            {
                if (c.Job != null) c.Job.Abandon();
                if (c.Upgrade != null)
                {
                    if (c.Upgrade.Job != null) c.Upgrade.Job.Abandon();
                    DestroyMeshes(c.Upgrade.Meshes);
                }
                DestroyMeshes(c.Meshes);
            }
            DestroyMeshes(_deadMeshes);
            DestroyMeshes(_deadMeshesOnExit);
            CloneZConfig.ArenaCenter = Vector3.zero;
            CloneZConfig.HardBounds = null;
            // Let the raid's map go: what the gameplay layer learned of it, and the regions, pieces and memories held here
            // (the NavMesh's rebuild hooks still point at this object until the next map registers its own).
            if (current || Active == null) MapGameplay.ForgetMap();
            _chunks.Clear();
            _regions.Clear();
            _memory.Clear();
            _memoryOrder.Clear();
            _areaReads.Clear();
        }
        #endregion

        #region Simulation (the map harness drives the streamer in edit mode)
        /// <summary>What one <see cref="Tick"/> did, for the harness's simulated walks and drives.</summary>
        internal sealed class TickStats
        {
            public double Ms, BuildMs;
            public int Steps, Finished, FinishedFull, FinishedLite, Upgraded, Unloaded;
            public string LongestStep;
            public double LongestStepMs;

            public void Clear()
            {
                Ms = BuildMs = LongestStepMs = 0;
                Steps = Finished = FinishedFull = FinishedLite = Upgraded = Unloaded = 0;
                LongestStep = null;
            }
        }

        /// <summary>Driven by <see cref="Simulate"/> instead of Update: no NavMesh, no agents, the survivors where
        /// <see cref="SimulatedFocus"/> says.</summary>
        internal bool Simulated;
        internal readonly List<Vector3> SimulatedFocus = new List<Vector3>(4);
        private TickStats _stats;

        /// <summary>One frame of the streamer as the harness simulates it (edit mode).</summary>
        internal void Simulate(TickStats stats)
        {
            Simulated = true;
            stats.Clear();
            double t0 = Now;
            _stats = stats;
            try { Tick(); }
            finally { _stats = null; }
            stats.Ms = (Now - t0) * 1000.0;
        }

        /// <summary>How many pieces are built, how many meshes they hold (and their vertices), and how many regions are read.</summary>
        internal void Census(out int pieces, out int meshes, out long vertices, out int regions)
            => Census(out pieces, out _, out meshes, out vertices, out regions);

        /// <summary>As <see cref="Census(out int, out int, out long, out int)"/>, with how many of the pieces are in Lite.</summary>
        internal void Census(out int pieces, out int lite, out int meshes, out long vertices, out int regions)
        {
            pieces = 0;
            lite = 0;
            meshes = 0;
            vertices = 0;
            foreach (Chunk c in _chunks.Values)
            {
                if (c.Ready)
                {
                    pieces++;
                    if (c.Detail == KentuckyMap.Detail.Lite) lite++;
                }
                meshes += c.Meshes.Count;
                for (int i = 0; i < c.Meshes.Count; i++) if (c.Meshes[i] != null) vertices += c.Meshes[i].vertexCount;
            }
            regions = _regions.Count;
        }

        /// <summary>The detail the piece at <paramref name="p"/> (world) is shown in, or null when it isn't built.</summary>
        internal KentuckyMap.Detail? DetailAt(Vector3 p)
            => _chunks.TryGetValue(Key(ChunkX(p.x), ChunkZ(p.z)), out Chunk c) && c.Ready ? c.Detail : (KentuckyMap.Detail?)null;

        /// <summary>Everything the streamer made, gone (the harness's teardown; the game's is OnDestroy).</summary>
        internal void Teardown()
        {
            foreach (Chunk c in _chunks.Values)
            {
                DropUpgrade(c, true);
                if (c.Job != null) c.Job.Abandon();
                Shelter.Forget(c);
                DestroyMeshes(c.Meshes);
                if (c.Root != null) DestroyImmediate(c.Root);
                if (c.Overhead != null) DestroyImmediate(c.Overhead);
            }
            _chunks.Clear();
            DestroyMeshes(_deadMeshes);
            DestroyMeshes(_deadMeshesOnExit);
            if (_streamed != null) DestroyImmediate(_streamed.gameObject);
            if (_overhead != null) DestroyImmediate(_overhead.gameObject);
            CeilingVisibility.Clear();
            if (Active == this) Active = null;
            CloneZConfig.ArenaCenter = Vector3.zero;
            CloneZConfig.HardBounds = null;
            DestroyImmediate(gameObject);
        }

        private static double Now => System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;

        // Destroy in play mode; at once in edit mode (the harness), where Destroy isn't allowed.
        private static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
        #endregion

        #region Every frame
        private void Update()
        {
            if (!Simulated) Tick();
        }

        private void Tick()
        {
            if (!_started) return;
            if (!_navReady && !Simulated) BakeNow();   // a world built without the bootstrapper's bake still gets its NavMesh
            FinishRegionLoads();
            GatherFocus();
            if (_focus.Count > 0)
            {
                Vector3 primary = _focus[0];
                Vector3 middle = ChunkMiddle(ChunkX(primary.x), ChunkZ(primary.z));
                CloneZConfig.ArenaCenter = middle;
                CloneZConfig.ArenaHalfSize = _activityHalf;
                if (_ground.transform.position != middle)
                {
                    _ground.transform.position = middle;
                    _horizon.transform.position = middle;
                }
                Travel(primary);
                Plan();
                BuildSome();
            }
            if (Simulated)
            {
                if (_deadMeshes.Count > 0) DestroyMeshes(_deadMeshes);
                EvictRegions();
                return;
            }
            NavTick();
            if (Time.unscaledTime >= _nextAgents)
            {
                _nextAgents = Time.unscaledTime + 1f;
                ReattachAgents();
            }
            Housekeeping();
        }

        // Every survivor the state is built around: the local one first, then the others (co-op, a host's clients).
        private void GatherFocus()
        {
            _focus.Clear();
            if (Simulated)
            {
                _focus.AddRange(SimulatedFocus);
                return;
            }
            Actor local = Actor.LocalPlayer;
            if (local != null) _focus.Add(local.transform.position);
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a != null && a != local && a.IsPlayerControlled) _focus.Add(a.transform.position);
            }
        }

        // The local survivor's travel on the ground (smoothed over about half a second; a jump of 50 m or more in a frame
        // is a teleport, not travel) and the way the camera looks along it.
        private void Travel(Vector3 at)
        {
            double now = Now;
            if (_haveLastFocus)
            {
                double dt = now - _lastFocusAt;
                var moved = new Vector2(at.x - _lastFocus.x, at.z - _lastFocus.z);
                if (moved.sqrMagnitude > 2500f) _velocity = Vector2.zero;
                else if (dt > 1e-3) _velocity = Vector2.Lerp(_velocity, moved / (float)dt, Mathf.Clamp01((float)dt / 0.5f));
            }
            _lastFocus = at;
            _lastFocusAt = now;
            _haveLastFocus = true;
            _look = Vector2.zero;
            if (Simulated) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 f = cam.transform.forward;
            var flat = new Vector2(f.x, f.z);
            if (flat.sqrMagnitude > 1e-4f) _look = flat.normalized;
        }

        // Which pieces should be built and in what detail (the survivor's own and its neighbours in Full, the ring past
        // them in Lite; going faster than a walk, the next ones straight ahead in Full too), in what order (nearest ring
        // first, the Full ones before the Lite ring; within a ring the ones ahead of the local survivor's travel, or of the
        // camera when they stand still), which regions should be read ahead and which pieces' data cut out, and which built
        // pieces are now far enough behind everybody to let go.
        private void Plan()
        {
            _wanted.Clear();
            _wantedAt.Clear();
            for (int i = 0; i < _focus.Count; i++)
            {
                int fx = ChunkX(_focus[i].x), fz = ChunkZ(_focus[i].z);
                Vector2 ahead = Vector2.zero;
                bool moving = false;
                if (i == 0)
                {
                    float speed = _velocity.magnitude;
                    if (speed > 0.5f)
                    {
                        ahead = _velocity / speed;
                        moving = speed > AheadSpeed;
                    }
                    else ahead = _look;
                }
                for (int dz = -LoadRadius; dz <= LoadRadius; dz++)
                    for (int dx = -LoadRadius; dx <= LoadRadius; dx++)
                    {
                        int ring = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
                        float along = ring == 0 ? 1f : (dx * ahead.x + dz * ahead.y) / Mathf.Sqrt(dx * dx + dz * dz);
                        bool full = ring <= FullRadius || (moving && ring == FullRadius + 1 && along > 0.75f);
                        int priority = ring * 100 + (full ? 0 : 1000) + Mathf.RoundToInt((1f - along) * 40f) + dx * dx + dz * dz;
                        var want = new Want
                        {
                            Cx = fx + dx, Cz = fz + dz, Priority = priority, Detail = full ? KentuckyMap.Detail.Full : KentuckyMap.Detail.Lite,
                        };
                        long key = Key(want.Cx, want.Cz);
                        if (_wantedAt.TryGetValue(key, out int at))
                        {
                            Want had = _wanted[at];
                            if (want.Priority < had.Priority) had.Priority = want.Priority;
                            if (full) had.Detail = KentuckyMap.Detail.Full;
                            _wanted[at] = had;
                            continue;
                        }
                        _wantedAt[key] = _wanted.Count;
                        _wanted.Add(want);
                    }
                int rx0 = RegionOf(fx - PrefetchRadius), rx1 = RegionOf(fx + PrefetchRadius);
                int rz0 = RegionOf(fz - PrefetchRadius), rz1 = RegionOf(fz + PrefetchRadius);
                for (int rx = rx0; rx <= rx1; rx++)
                    for (int rz = rz0; rz <= rz1; rz++) Slot(rx, rz, true);
            }
            _wanted.Sort(s_ByPriority);
            for (int i = 0; i < _wanted.Count; i++) _wantedAt[Key(_wanted[i].Cx, _wanted[i].Cz)] = i;

            _scratch.Clear();
            foreach (Chunk c in _chunks.Values)
                if (!c.Pinned && !Near(c.Cx, c.Cz, KeepRadius)) _scratch.Add(c);
            for (int i = 0; i < _scratch.Count; i++) Unload(_scratch[i]);

            ReadAhead();
        }

        private bool Near(int cx, int cz, int radius)
        {
            for (int i = 0; i < _focus.Count; i++)
            {
                int fx = ChunkX(_focus[i].x), fz = ChunkZ(_focus[i].z);
                if (Mathf.Abs(cx - fx) <= radius && Mathf.Abs(cz - fz) <= radius) return true;
            }
            return false;
        }

        // True when the wanted piece still has to be built: it isn't, or it's shown in Lite and now wanted in Full.
        private bool NeedsBuild(Want w, out Chunk have)
        {
            if (!_chunks.TryGetValue(Key(w.Cx, w.Cz), out have)) return true;
            return have.Ready && !have.Failed && !have.Pinned && have.Detail == KentuckyMap.Detail.Lite && w.Detail == KentuckyMap.Detail.Full
                && have.Upgrade == null && !have.NoUpgrade;
        }

        // The data of the next pieces to build, cut out of their regions on workers while the main thread builds others.
        private void ReadAhead()
        {
            _staleReads.Clear();
            foreach (KeyValuePair<long, Task<KentuckyMap.Area>> kv in _areaReads)
                if (!_wantedAt.ContainsKey(kv.Key)) _staleReads.Add(kv.Key);
            for (int i = 0; i < _staleReads.Count; i++) _areaReads.Remove(_staleReads[i]);
            int busy = 0;
            foreach (Task<KentuckyMap.Area> t in _areaReads.Values) if (!t.IsCompleted) busy++;
            int ahead = 0;
            for (int i = 0; i < _wanted.Count && ahead < MaxAreaReads + 1; i++)
            {
                Want w = _wanted[i];
                if (!NeedsBuild(w, out Chunk have) || (_building != null && _building.Key == Key(w.Cx, w.Cz))) continue;
                ahead++;
                long key = Key(w.Cx, w.Cz);
                if (_areaReads.ContainsKey(key) || busy >= MaxAreaReads) continue;
                RegionSlot slot = Slot(RegionOf(w.Cx), RegionOf(w.Cz), true);
                if (slot == null || slot.Index == null) continue;
                _areaReads[key] = StartRead(slot.Index, w.Cx, w.Cz);
                busy++;
            }
        }

        private Task<KentuckyMap.Area> StartRead(MapRegionIndex region, int cx, int cz)
        {
            string place = _place.name, id = _build.Id, attribution = _build.Attribution;
            return Task.Run(() => new KentuckyMap.Area(place, id, attribution, region, cx, cz));
        }

        // A few milliseconds of building a frame (a little more while a piece next to the local survivor isn't shown in Full,
        // much more while a survivor stands on ground that isn't built yet), step by step: a step expected to take longer
        // than what's left of the frame's budget waits for the next frame (unless it's the frame's first), and a frame that
        // ran over pays it back out of the next frames' budgets.
        private void BuildSome()
        {
            Vector3 primary = _focus[0];
            int px = ChunkX(primary.x), pz = ChunkZ(primary.z);
            bool behind = !_chunks.TryGetValue(Key(px, pz), out Chunk under) || !under.Ready;
            float budget = behind ? CatchUpBudget : FullAround(px, pz) ? BuildBudget : RingBudget;
            if (behind) _debt = 0f;
            else if (_debt > 0f)
            {
                float pay = Mathf.Min(_debt, budget);
                _debt -= pay;
                budget -= pay;
                if (budget <= 0f) return;
            }
            double start = Now, budgetS = budget;
            bool ran = false;
            while (true)
            {
                if (_building == null)
                {
                    _building = Next(behind);
                    if (_building == null) break;
                }
                Chunk c = _building;
                if (ran && Now - start + Expected(c) * 0.001 > budgetS) break;
                ran = true;
                double t0 = Now;
                string before = c.Steps == null ? Finishing : c.LastStep ?? Starting;
                bool more = c.Steps != null && Step(c);
                double ms;
                if (more)
                {
                    ms = (Now - t0) * 1000.0;
                    Learn(c, before, ms);
                    Count(c.LastStep, ms);
                    // Its meshes are being packed on a worker: back to it next frame.
                    if (c.LastStep == KentuckyMap.WaitStep) break;
                    continue;
                }
                if (c.Steps != null)
                {
                    // The last step ran; the piece is finished (shown) as a step of its own.
                    ms = (Now - t0) * 1000.0;
                    Learn(c, before, ms);
                    Count(before, ms);
                    c.Steps = null;
                    continue;
                }
                bool swap = c.Replaces != null;
                Finish(c);
                ms = (Now - t0) * 1000.0;
                Learn(c, Finishing, ms);
                Count(swap ? "(swap)" : "(activate)", ms);
                _building = null;
            }
            if (!behind)
            {
                float over = (float)(Now - start - budgetS);
                if (over > 0f) _debt = Mathf.Min(MaxDebt, _debt + over);
            }
        }

        private const string Starting = "(start)", Finishing = "(finish)";

        // True when every piece within FullRadius of piece (cx, cz) is shown in Full.
        private bool FullAround(int cx, int cz)
        {
            for (int dz = -FullRadius; dz <= FullRadius; dz++)
                for (int dx = -FullRadius; dx <= FullRadius; dx++)
                    if (!_chunks.TryGetValue(Key(cx + dx, cz + dz), out Chunk c) || !c.Ready || c.Detail != KentuckyMap.Detail.Full)
                        return false;
            return true;
        }

        // What the step after <paramref name="c"/>'s last one is expected to take: what it took last time, scaled by how much
        // more (or less) this piece has to build than the ones it was learned on (a guess for an unknown one).
        private double Expected(Chunk c)
        {
            string before = c.Steps == null ? Finishing : c.LastStep ?? Starting;
            if (!_stepCosts[(int)c.Detail].TryGetValue(before, out StepCost s)) return 1.0;
            double scale = s.Weight > 0 && c.Weight > 0 ? Math.Min(4.0, Math.Max(0.5, c.Weight / s.Weight)) : 1.0;
            return s.Ms * scale;
        }

        private void Learn(Chunk c, string before, double ms)
        {
            Dictionary<string, StepCost> costs = _stepCosts[(int)c.Detail];
            if (!costs.TryGetValue(before, out StepCost s)) costs[before] = s = new StepCost();
            // Quick to learn, slow to forget a long one: the next piece's step is likelier like the worst than the mean.
            s.Ms = s.Seen == 0 ? ms : ms > s.Ms ? s.Ms * 0.4 + ms * 0.6 : s.Ms * 0.85 + ms * 0.15;
            if (c.Weight > 0) s.Weight = s.Weight <= 0 ? c.Weight : s.Weight * 0.7 + c.Weight * 0.3;
            s.Seen++;
        }

        // How much there is to build on a piece, roughly: its buildings by size, its streets, lines, land-use areas and mapped
        // points. What a step took on one piece is scaled by this to guess what it takes on another (downtown Louisville
        // against a Bardstown street).
        private static float WeightOf(KentuckyMap.Area a)
        {
            if (a == null) return 0f;
            float w = 4f + a.Streets.Count * 0.25f + a.Lines.Count * 0.25f + a.Barriers.Count * 0.25f + a.Areas.Count * 0.5f + a.Nodes.Count * 0.05f;
            for (int i = 0; i < a.Buildings.Count; i++) w += 0.5f + Mathf.Min(a.Buildings[i].Size, 20000f) / 250f;
            return w;
        }

        private void Count(string step, double ms)
        {
            if (_stats == null) return;
            _stats.Steps++;
            _stats.BuildMs += ms;
            if (ms > _stats.LongestStepMs)
            {
                _stats.LongestStepMs = ms;
                _stats.LongestStep = step;
            }
        }

        // The next piece to build: the most wanted one that isn't built in the detail it's wanted in, whose region has been
        // read and whose data has been cut out (that, or read here and now for the ground under a survivor's feet).
        private Chunk Next(bool behind)
        {
            for (int i = 0; i < _wanted.Count; i++)
            {
                Want w = _wanted[i];
                if (!NeedsBuild(w, out Chunk have)) continue;
                RegionSlot slot = Slot(RegionOf(w.Cx), RegionOf(w.Cz), true);
                if (slot == null || slot.Index == null) continue;
                long key = Key(w.Cx, w.Cz);
                bool urgent = behind && w.Priority < 100;   // the ground under a survivor's feet
                KentuckyMap.Area area = null;
                if (_areaReads.TryGetValue(key, out Task<KentuckyMap.Area> read))
                {
                    if (!read.IsCompleted)
                    {
                        if (!urgent) return null;   // ready in a moment: not worth starting a lesser piece for
                        try { read.Wait(); }
                        catch (AggregateException) { }
                    }
                    if (read.Status == TaskStatus.RanToCompletion) area = read.Result;
                    _areaReads.Remove(key);
                }
                else if (!urgent)
                {
                    _areaReads[key] = StartRead(slot.Index, w.Cx, w.Cz);
                    return null;
                }
                Chunk c = Begin(w.Cx, w.Cz, slot.Index, w.Detail, area);
                if (have != null)
                {
                    have.Upgrade = c;
                    c.Replaces = have;
                }
                else _chunks[c.Key] = c;
                return c;
            }
            return null;
        }
        #endregion

        #region Building and letting go of pieces
        // A piece's build set up (its roots made, inactive at the origin); the caller files it (a fresh piece in _chunks, an
        // upgrade on the piece it replaces). <paramref name="area"/>: its data, when it was cut out ahead.
        private Chunk Begin(int cx, int cz, MapRegionIndex region, KentuckyMap.Detail detail, KentuckyMap.Area area)
        {
            var c = new Chunk { Cx = cx, Cz = cz, Key = Key(cx, cz), Middle = ChunkMiddle(cx, cz), Detail = detail };
            c.UvOrigin = new Vector3((float)Wrap((cx + 0.5) * _chunk), 0f, (float)Wrap((cz + 0.5) * _chunk));
            string name = "Piece " + cx + "_" + cz;
            // Built inactive at the origin, where the builders' local and world positions agree; moved into place after.
            c.Root = new GameObject(name);
            c.Root.SetActive(false);
            c.Root.transform.SetParent(_streamed, false);
            c.Root.transform.position = Vector3.zero;
            c.Overhead = new GameObject(name);
            c.Overhead.SetActive(false);
            c.Overhead.transform.SetParent(_overhead, false);
            c.Overhead.transform.position = Vector3.zero;
            c.Job = new KentuckyMap.ChunkJob
            {
                Build = _build, Region = region, Place = _place.name, Cx = cx, Cz = cz,
                Seed = unchecked(_layoutSeed * 486187739 + cx * 73856093 ^ cz * 19349663),
                Root = c.Root.transform, Overhead = c.Overhead.transform, Offset = c.Middle, Detail = detail, Area = area,
            };
            c.Steps = KentuckyMap.ChunkSteps(c.Job).GetEnumerator();
            c.Weight = WeightOf(area);
            return c;
        }

        // One step of a piece's build, with what it makes credited to the piece. False once it's done (or failed).
        private bool Step(Chunk c)
        {
            List<Mesh> meshes = RaidMesh.CreatedMeshes;
            List<string> signs = DecorTextures.SignsUsed;
            Vector3 uv = RaidMesh.PlanarOrigin;
            object owner = Shelter.Owner;
            Func<string, bool> gate = ItemDatabase.LootGate;
            Action<string, int> rolled = ItemDatabase.LootRolled;
            RaidMesh.CreatedMeshes = c.Meshes;
            DecorTextures.SignsUsed = c.Signs;
            RaidMesh.PlanarOrigin = c.UvOrigin;
            Shelter.Owner = c;
            // A piece's loot is its own (see ApplyLoot): the containers' first roll mustn't count against the Central
            // Economy either.
            ItemDatabase.LootGate = null;
            ItemDatabase.LootRolled = null;
            try
            {
                if (c.Job != null && c.Job.Site != null) c.Job.Site.BeginStep();
                bool more = c.Steps.MoveNext();
                if (more) c.LastStep = c.Steps.Current;
                if (c.Weight <= 0f && c.Job != null) c.Weight = WeightOf(c.Job.Area);
                return more;
            }
            catch (Exception e)
            {
                Debug.LogError("[CloneZ] Building Kentucky at piece " + c.Cx + "_" + c.Cz + (c.Detail == KentuckyMap.Detail.Lite ? " (Lite)" : "")
                    + " failed" + (c.Replaces != null ? "; it stays in Lite. " : "; it stays open snow. ") + e);
                c.Failed = true;
                if (c.Job != null) c.Job.Abandon();
                return false;
            }
            finally
            {
                RaidMesh.CreatedMeshes = meshes;
                DecorTextures.SignsUsed = signs;
                RaidMesh.PlanarOrigin = uv;
                Shelter.Owner = owner;
                ItemDatabase.LootGate = gate;
                ItemDatabase.LootRolled = rolled;
            }
        }

        // A piece whose steps have run: moved into place, its loot rolled (or what was left in it given back) and shown,
        // in place of the Lite piece it's an upgrade of. A failed piece is laid as bare snow; a failed upgrade is dropped
        // and the Lite piece stays.
        private void Finish(Chunk c)
        {
            c.Steps = null;
            Chunk old = c.Replaces;
            if (c.Failed && old != null)
            {
                old.NoUpgrade = true;
                DropUpgrade(old, false);
                return;
            }
            if (c.Failed)
            {
                // Its buildings' plans were left with the interiors before the build failed: nothing's to be furnished on
                // bare snow.
                if (InteriorStreamer.Active != null) InteriorStreamer.Active.Forget(c.Root.transform);
                Clear(c);
                List<Mesh> meshes = RaidMesh.CreatedMeshes;
                RaidMesh.CreatedMeshes = c.Meshes;
                KentuckyMap.BareGround(c.Root.transform, _chunk * 0.5f);
                RaidMesh.CreatedMeshes = meshes;
            }
            else if (c.Job != null && c.Job.Site != null)
            {
                _buildings += c.Job.Site.Buildings;
                _containers += c.Job.Site.Containers;
                _trees += c.Job.Site.Trees;
                _lights += c.Job.Town.LightCount;
            }
            c.Root.transform.position = c.Middle;
            c.Overhead.transform.position = c.Middle;
            if (!c.Failed && c.Detail == KentuckyMap.Detail.Full) ApplyLoot(c);
            if (old != null)
            {
                // The Lite piece goes as the Full one comes (its NavMesh sources with it).
                old.Upgrade = null;
                Release(old);
                if (old.Ready)
                {
                    _ready--;
                    if (old.Detail == KentuckyMap.Detail.Lite) _readyLite--;
                }
                _chunks[c.Key] = c;
                c.Replaces = null;
                _upgraded++;
                if (_stats != null) _stats.Upgraded++;
            }
            c.Root.SetActive(true);
            c.Overhead.SetActive(true);
            c.Ready = true;
            _ready++;
            if (c.Detail == KentuckyMap.Detail.Lite) _readyLite++;
            if (_stats != null)
            {
                _stats.Finished++;
                if (c.Detail == KentuckyMap.Detail.Lite) _stats.FinishedLite++;
                else _stats.FinishedFull++;
            }
            c.SignKeys = new HashSet<string>(c.Signs);
            c.Signs.Clear();
            foreach (string key in c.SignKeys) _signUsers[key] = _signUsers.TryGetValue(key, out int n) ? n + 1 : 1;
            if (!c.Pinned) c.Job = null;
            if (_navReady)
            {
                CollectSources(c);
                _navDirty = true;
            }
            else c.SourcesDirty = true;
        }

        // Empties a failed piece so it can be laid as bare snow.
        private void Clear(Chunk c)
        {
            Shelter.Forget(c);
            for (int i = c.Root.transform.childCount - 1; i >= 0; i--) DestroyImmediate(c.Root.transform.GetChild(i).gameObject);
            for (int i = c.Overhead.transform.childCount - 1; i >= 0; i--) DestroyImmediate(c.Overhead.transform.GetChild(i).gameObject);
            DestroyMeshes(c.Meshes);
        }

        private void Unload(Chunk c)
        {
            DropUpgrade(c, false);
            if (c == _building)
            {
                _building = null;
                if (c.Job != null) c.Job.Abandon();
            }
            if (c.Ready) Remember(c);
            Release(c);
            _chunks.Remove(c.Key);
            if (c.Ready)
            {
                _ready--;
                if (c.Detail == KentuckyMap.Detail.Lite) _readyLite--;
            }
            if (_stats != null) _stats.Unloaded++;
        }

        // Everything a piece made, gone: its objects, its meshes (once no NavMesh update is reading them), its shelters and
        // its signs.
        private void Release(Chunk c)
        {
            Shelter.Forget(c);
            DestroyMaterialCopies(c.Root);
            DestroyMaterialCopies(c.Overhead);
            Kill(c.Root);
            Kill(c.Overhead);
            _deadMeshes.AddRange(c.Meshes);
            c.Meshes.Clear();
            ReleaseSigns(c);
            if (c.Ready) _navDirty = true;
            _unloadsSinceSweep++;
            _prune = true;
        }

        // The Full piece being built to replace <paramref name="c"/>, dropped (<paramref name="now"/>: destroyed at once, for
        // a teardown).
        private void DropUpgrade(Chunk c, bool now)
        {
            Chunk u = c.Upgrade;
            if (u == null) return;
            c.Upgrade = null;
            u.Replaces = null;
            if (_building == u) _building = null;
            if (u.Job != null) u.Job.Abandon();
            u.Job = null;
            u.Steps = null;
            if (now)
            {
                Shelter.Forget(u);
                DestroyMeshes(u.Meshes);
                if (u.Root != null) DestroyImmediate(u.Root);
                if (u.Overhead != null) DestroyImmediate(u.Overhead);
                return;
            }
            Release(u);
        }

        // The material copies some props make of their shared materials (Renderer.material) die with them.
        private static void DestroyMaterialCopies(GameObject root)
        {
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                    if (m != null && m.name.EndsWith(" (Instance)", StringComparison.Ordinal)) Kill(m);
        }

        // Signs are textures of their own (a named place's sign is shown by its piece alone): the ones no built piece
        // shows any more are let go of, so a long trip doesn't keep every sign it passed.
        private void ReleaseSigns(Chunk c)
        {
            if (c.SignKeys != null)
                foreach (string key in c.SignKeys)
                    if (_signUsers.TryGetValue(key, out int n)) { if (n > 1) _signUsers[key] = n - 1; else _signUsers.Remove(key); }
            IEnumerable<string> asked = c.SignKeys != null ? (IEnumerable<string>)c.SignKeys : c.Signs;
            foreach (string key in asked)
                // The piece being built may already show it, and only counts its signs once it's done.
                if (!_signUsers.ContainsKey(key) && (_building == null || !_building.Signs.Contains(key))) DecorTextures.Forget(key);
            c.SignKeys = null;
            c.Signs.Clear();
        }

        private static void DestroyMeshes(List<Mesh> meshes)
        {
            for (int i = 0; i < meshes.Count; i++) Kill(meshes[i]);
            meshes.Clear();
        }
        #endregion

        #region What was taken stays taken
        // A container's place in its piece, to the decimetre (a piece builds the same containers in the same places).
        private static Vector3Int SpotOf(Chunk c, Transform t)
        {
            Vector3 p = c.Root.transform.InverseTransformPoint(t.position);
            return new Vector3Int(Mathf.RoundToInt(p.x * 10f), Mathf.RoundToInt(p.y * 10f), Mathf.RoundToInt(p.z * 10f));
        }

        // A fresh piece rolls this raid's loot; one built again gets back what was left in it. The roll is the piece's
        // own, from the raid's seed and the container's place: outside the Central Economy's caps, which are tuned for a
        // single map (gated, the state would run dry as you travel) and would make each machine in a co-op raid roll
        // differently, as each builds its pieces in its own order.
        private void ApplyLoot(Chunk c)
        {
            Func<string, bool> gate = ItemDatabase.LootGate;
            Action<string, int> rolled = ItemDatabase.LootRolled;
            ItemDatabase.LootGate = null;
            ItemDatabase.LootRolled = null;
            try
            {
                RollOrRestore(c);
            }
            finally
            {
                ItemDatabase.LootGate = gate;
                ItemDatabase.LootRolled = rolled;
            }
        }

        // Only a Full piece rolls its containers (a Lite piece's few, if any, stay empty: nobody comes near one before it's
        // built again in Full); what was taken from a piece is the Full piece's to remember and give back.
        private void RollOrRestore(Chunk c)
        {
            _memory.TryGetValue(c.Key, out ChunkMemory kept);
            c.Containers = new HashSet<Vector3Int>();
            foreach (LootContainer box in c.Root.GetComponentsInChildren<LootContainer>(true))
            {
                if (box == null || box.Type == ContainerType.Corpse || box.IsSack) continue;
                Vector3Int spot = SpotOf(c, box.transform);
                if (!c.Containers.Add(spot)) continue;
                if (kept != null)
                {
                    if (kept.Gone.Contains(spot))
                    {
                        DestroyImmediate(box.gameObject);
                        continue;
                    }
                    if (kept.Containers.TryGetValue(spot, out KeptContainer was))
                    {
                        box.Contents = was.Contents;
                        box.Searched = was.Searched;
                        continue;
                    }
                }
                int seed = unchecked(_raidSeed ^ (c.Cx * 73856093) ^ (c.Cz * 19349663) ^ (spot.x * 83492791) ^ (spot.z * 39916801) ^ spot.y);
                RaidWorldRules.RollLoot(box, new System.Random(seed));
            }
            if (kept != null) _memory.Remove(c.Key);
        }

        private void Remember(Chunk c)
        {
            if (c.Containers == null || c.Root == null) return;
            var kept = new ChunkMemory { Stamp = ++_memoryStamp };
            foreach (LootContainer box in c.Root.GetComponentsInChildren<LootContainer>(true))
            {
                if (box == null || box.Type == ContainerType.Corpse || box.IsSack) continue;
                Vector3Int spot = SpotOf(c, box.transform);
                if (!kept.Containers.ContainsKey(spot)) kept.Containers[spot] = new KeptContainer { Contents = box.Contents, Searched = box.Searched };
            }
            foreach (Vector3Int spot in c.Containers)
                if (!kept.Containers.ContainsKey(spot)) kept.Gone.Add(spot);
            _memory[c.Key] = kept;
            _memoryOrder.Enqueue((c.Key, kept.Stamp));
            // Long gone pieces are forgotten (the Zone restocks what you left far behind).
            while (_memory.Count > MemoryKept && _memoryOrder.Count > 0)
            {
                (long key, int stamp) = _memoryOrder.Dequeue();
                if (_memory.TryGetValue(key, out ChunkMemory old) && old.Stamp == stamp) _memory.Remove(key);
            }
        }
        #endregion

        #region Regions
        private RegionSlot Slot(int rx, int rz, bool load)
        {
            long key = Key(rx, rz);
            if (_regions.TryGetValue(key, out RegionSlot slot))
            {
                slot.LastUsed = Time.realtimeSinceStartup;
                return slot;
            }
            if (!load) return null;
            if (!_build.HasRegion(rx, rz))
            {
                slot = new RegionSlot { Rx = rx, Rz = rz, Index = new MapRegionIndex(null, rx, rz, _build.RegionSize, _chunk) };
            }
            else
            {
                int loading = 0;
                foreach (RegionSlot s in _regions.Values) if (s.Load != null) loading++;
                if (loading >= MaxRegionLoads) return null;
                MapBuild build = _build;
                int size = build.RegionSize, chunk = _chunk;
                slot = new RegionSlot { Rx = rx, Rz = rz, Load = Task.Run(() => new MapRegionIndex(build.LoadRegion(rx, rz), rx, rz, size, chunk)) };
            }
            slot.LastUsed = Time.realtimeSinceStartup;
            _regions[key] = slot;
            return slot;
        }

        // A region read now, on this thread if it isn't on its way already (the start's).
        private MapRegionIndex RegionNow(int rx, int rz)
        {
            long key = Key(rx, rz);
            if (!_regions.TryGetValue(key, out RegionSlot slot))
            {
                MapRegion data = _build.HasRegion(rx, rz) ? ReadRegion(rx, rz) : null;
                slot = new RegionSlot { Rx = rx, Rz = rz, Index = new MapRegionIndex(data, rx, rz, _build.RegionSize, _chunk) };
                _regions[key] = slot;
            }
            else if (slot.Index == null)
            {
                try { slot.Load.Wait(); }
                catch (AggregateException) { }
                Complete(slot);
            }
            slot.LastUsed = Time.realtimeSinceStartup;
            return slot.Index;
        }

        private MapRegion ReadRegion(int rx, int rz)
        {
            try
            {
                return _build.LoadRegion(rx, rz);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Region " + rx + "_" + rz + " of build " + _build.Id + " couldn't be read (" + e.GetType().Name + ": "
                    + e.Message + "); it stays open snow.");
                return null;
            }
        }

        private void FinishRegionLoads()
        {
            foreach (RegionSlot slot in _regions.Values)
                if (slot.Load != null && slot.Load.IsCompleted) Complete(slot);
        }

        private void Complete(RegionSlot slot)
        {
            if (slot.Load.Status == TaskStatus.RanToCompletion) slot.Index = slot.Load.Result;
            else
            {
                Exception e = slot.Load.Exception != null ? slot.Load.Exception.GetBaseException() : null;
                Debug.LogWarning("[CloneZ] Region " + slot.Rx + "_" + slot.Rz + " of build " + _build.Id + " couldn't be read ("
                    + (e != null ? e.GetType().Name + ": " + e.Message : "cancelled") + "); it stays open snow.");
                slot.Index = new MapRegionIndex(null, slot.Rx, slot.Rz, _build.RegionSize, _chunk);
            }
            slot.Load = null;
        }

        private readonly List<RegionSlot> _evict = new List<RegionSlot>();

        // Regions nobody has needed for a while go, the longest unused first.
        private void EvictRegions()
        {
            if (_regions.Count <= MaxRegionsKept) return;
            float stale = Time.realtimeSinceStartup - 5f;
            _evict.Clear();
            foreach (RegionSlot slot in _regions.Values)
                if (slot.Load == null && slot.LastUsed < stale && (_building == null || _building.Job == null || _building.Job.Region != slot.Index))
                    _evict.Add(slot);
            _evict.Sort((a, b) => a.LastUsed.CompareTo(b.LastUsed));
            for (int i = 0; i < _evict.Count && _regions.Count > MaxRegionsKept; i++) _regions.Remove(Key(_evict[i].Rx, _evict[i].Rz));
        }
        #endregion

        #region The NavMesh
        /// <summary>Asks for the NavMesh to be baked again from scratch (something baked into it was demolished).</summary>
        private void RequestRebuild()
        {
            _staticDirty = true;
            foreach (Chunk c in _chunks.Values) if (c.Ready) c.SourcesDirty = true;
            _navDirty = true;
        }

        /// <summary>Asks for the NavMesh to be baked again where something baked into it changed (<paramref name="worldBounds"/>,
        /// world space): the built pieces it touches gather their sources again (only theirs: the rest are as they were, so
        /// only the NavMesh tiles over what changed are baked again, in the background). Something outside every piece is
        /// taken for one of the world's own (a camp, the zone's overlays), which are gathered again instead.</summary>
        internal void InvalidateNav(Bounds worldBounds) => Invalidate(worldBounds, false);

        // As InvalidateNav; <paramref name="andWorld"/>: gather the world's own sources again too (what changed may be one).
        private void Invalidate(Bounds worldBounds, bool andWorld)
        {
            bool any = false;
            float h = _chunk * 0.5f;
            foreach (Chunk c in _chunks.Values)
            {
                if (!c.Ready) continue;
                bool square = worldBounds.max.x >= c.Middle.x - h && worldBounds.min.x <= c.Middle.x + h
                    && worldBounds.max.z >= c.Middle.z - h && worldBounds.min.z <= c.Middle.z + h;
                if (!square && !(c.SourceBounds.size != Vector3.zero && c.SourceBounds.Intersects(worldBounds))) continue;
                c.SourcesDirty = true;
                any = true;
            }
            if (andWorld || !any) _staticDirty = true;
            _navDirty = true;
        }

        private void NavTick()
        {
            if (!_navReady) return;
            if (_navOp != null)
            {
                if (!_navOp.isDone) return;
                _navOp = null;
                _navBuilt = _navOpBounds;
            }
            if (_deadMeshes.Count > 0) DestroyMeshes(_deadMeshes);
            if (_focus.Count > 0)
            {
                Bounds want = AroundSurvivors();
                if (want.center != _navWanted.center || want.size != _navWanted.size)
                {
                    _navWanted = want;
                    _navDirty = true;
                }
            }
            if (!_navDirty || Time.realtimeSinceStartup < _nextNav) return;
            _navDirty = false;
            _nextNav = Time.realtimeSinceStartup + NavInterval;
            if (_staticDirty) CollectStatic();
            foreach (Chunk c in _chunks.Values) if (c.Ready && c.SourcesDirty) CollectSources(c);
            Gather(_navWanted);
            _navOpBounds = _navWanted;
            _navOp = NavMeshBuilder.UpdateNavMeshDataAsync(_navData, _navSettings, _navSources, _navWanted);
        }

        // The built pieces around each survivor, together while they're near each other; else the local survivor's.
        private Bounds AroundSurvivors()
        {
            int fx = ChunkX(_focus[0].x), fz = ChunkZ(_focus[0].z);
            int x0 = fx - LoadRadius, x1 = fx + LoadRadius, z0 = fz - LoadRadius, z1 = fz + LoadRadius;
            for (int i = 1; i < _focus.Count; i++)
            {
                int gx = ChunkX(_focus[i].x), gz = ChunkZ(_focus[i].z);
                int nx0 = Mathf.Min(x0, gx - LoadRadius), nx1 = Mathf.Max(x1, gx + LoadRadius);
                int nz0 = Mathf.Min(z0, gz - LoadRadius), nz1 = Mathf.Max(z1, gz + LoadRadius);
                if ((nx1 - nx0 + 1) * _chunk > MaxNavSpan || (nz1 - nz0 + 1) * _chunk > MaxNavSpan) continue;
                x0 = nx0; x1 = nx1; z0 = nz0; z1 = nz1;
            }
            return Square(x0, z0, x1, z1);
        }

        private Bounds PinnedBounds()
        {
            int x0 = int.MaxValue, z0 = int.MaxValue, x1 = int.MinValue, z1 = int.MinValue;
            foreach (Chunk c in _chunks.Values)
            {
                if (!c.Pinned) continue;
                x0 = Mathf.Min(x0, c.Cx); x1 = Mathf.Max(x1, c.Cx);
                z0 = Mathf.Min(z0, c.Cz); z1 = Mathf.Max(z1, c.Cz);
            }
            return x0 > x1 ? Square(0, 0, 0, 0) : Square(x0, z0, x1, z1);
        }

        // The box over pieces (x0, z0) to (x1, z1), from below the ground to above the tallest storey that's baked.
        private Bounds Square(int x0, int z0, int x1, int z1)
        {
            Vector3 a = ChunkMiddle(x0, z0), b = ChunkMiddle(x1, z1);
            float h = _chunk * 0.5f;
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(a.x - h, NavMinY, a.z - h), new Vector3(b.x + h, NavMaxY, b.z + h));
            return bounds;
        }

        // What the next bake reads: the world's own pieces (the zone's overlays...) and every built piece in the box,
        // in a steady order so the pieces that haven't changed aren't baked again. Anything destroyed since it was
        // collected (a dismantled crate) is left out.
        private void Gather(Bounds bounds)
        {
            _navSources.Clear();
            for (int i = 0; i < _static.Count; i++)
                if (_staticBounds[i].Intersects(bounds) && _static[i].component != null) _navSources.Add(_static[i]);
            _navKeys.Clear();
            foreach (Chunk c in _chunks.Values) if (c.Ready && c.SourceBounds.Intersects(bounds)) _navKeys.Add(c.Key);
            _navKeys.Sort();
            for (int i = 0; i < _navKeys.Count; i++)
            {
                List<NavMeshBuildSource> sources = _chunks[_navKeys[i]].Sources;
                for (int k = 0; k < sources.Count; k++) if (sources[k].component != null) _navSources.Add(sources[k]);
            }
        }

        private void CollectStatic()
        {
            _static.Clear();
            _staticBounds.Clear();
            Collect(World, _streamed, _static);
            for (int i = 0; i < _static.Count; i++) _staticBounds.Add(WorldBounds(_static[i]));
            _staticDirty = false;
        }

        private void CollectSources(Chunk c)
        {
            c.Sources.Clear();
            Collect(c.Root.transform, null, c.Sources);
            bool any = false;
            var bounds = new Bounds(c.Middle, Vector3.zero);
            for (int i = 0; i < c.Sources.Count; i++)
            {
                Bounds b = WorldBounds(c.Sources[i]);
                if (!any) { bounds = b; any = true; }
                else bounds.Encapsulate(b);
            }
            c.SourceBounds = bounds;
            c.SourcesDirty = false;
        }

        private static readonly List<NavMeshModifier> s_Modifiers = new List<NavMeshModifier>();
        private static readonly List<NavMeshBuildMarkup> s_Markups = new List<NavMeshBuildMarkup>();
        private static readonly List<NavMeshBuildSource> s_Found = new List<NavMeshBuildSource>();

        // As the fixed maps' NavMeshSurface collects the world's children: their render meshes, as the modifiers on
        // them say, leaving out agents and obstacles (and, here, whatever lies under <paramref name="skip"/>).
        internal static void Collect(Transform root, Transform skip, List<NavMeshBuildSource> into)
        {
            s_Markups.Clear();
            root.GetComponentsInChildren(false, s_Modifiers);
            for (int i = 0; i < s_Modifiers.Count; i++)
            {
                NavMeshModifier m = s_Modifiers[i];
                if (m == null || !m.isActiveAndEnabled || !m.AffectsAgentType(AgentType)) continue;
                if (skip != null && m.transform.IsChildOf(skip)) continue;
                s_Markups.Add(new NavMeshBuildMarkup
                {
                    root = m.transform, overrideArea = m.overrideArea, area = m.area, ignoreFromBuild = m.ignoreFromBuild,
                    applyToChildren = m.applyToChildren, overrideGenerateLinks = m.overrideGenerateLinks, generateLinks = m.generateLinks,
                });
            }
            if (skip != null) s_Markups.Add(new NavMeshBuildMarkup { root = skip, ignoreFromBuild = true, applyToChildren = true });
            s_Found.Clear();
            NavMeshBuilder.CollectSources(root, ~0, NavMeshCollectGeometry.RenderMeshes, 0, false, s_Markups, false, s_Found);
            for (int i = 0; i < s_Found.Count; i++)
            {
                Component c = s_Found[i].component;
                if (c != null && (c.GetComponent<NavMeshAgent>() != null || c.GetComponent<NavMeshObstacle>() != null)) continue;
                into.Add(s_Found[i]);
            }
            s_Modifiers.Clear();
        }

        private static Bounds WorldBounds(NavMeshBuildSource s)
        {
            Bounds local = s.shape == NavMeshBuildSourceShape.Mesh && s.sourceObject is Mesh mesh ? mesh.bounds : new Bounds(Vector3.zero, s.size);
            Matrix4x4 m = s.transform;
            Vector3 x = Abs(m.MultiplyVector(Vector3.right)), y = Abs(m.MultiplyVector(Vector3.up)), z = Abs(m.MultiplyVector(Vector3.forward));
            return new Bounds(m.MultiplyPoint(local.center), x * local.size.x + y * local.size.y + z * local.size.z);
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        // The NavMesh moved off some of the Zone's people and came back: put the ones it's under again back on it.
        private void ReattachAgents()
        {
            if (!_navReady || _navOp != null) return;
            Bounds b = _navBuilt;
            foreach (NavMeshAgent agent in FindObjectsByType<NavMeshAgent>())
            {
                if (agent == null || !agent.isActiveAndEnabled || agent.isOnNavMesh) continue;
                Vector3 p = agent.transform.position;
                if (p.x < b.min.x + 2f || p.x > b.max.x - 2f || p.z < b.min.z + 2f || p.z > b.max.z - 2f) continue;
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 2.5f, agent.areaMask)) agent.Warp(hit.position);
            }
        }
        #endregion

        #region Questions from the rest of the game
        /// <summary>True when the piece under <paramref name="p"/> (world) is built.</summary>
        public bool IsBuiltAt(Vector3 p) => _chunks.TryGetValue(Key(ChunkX(p.x), ChunkZ(p.z)), out Chunk c) && c.Ready;

        /// <summary>True when the NavMesh has been baked over <paramref name="p"/> (world) since its piece was built.</summary>
        public bool IsNavigableAt(Vector3 p)
        {
            if (!_navReady || _navOp != null || _navDirty || !IsBuiltAt(p)) return false;
            return p.x > _navBuilt.min.x && p.x < _navBuilt.max.x && p.z > _navBuilt.min.z && p.z < _navBuilt.max.z;
        }
        #endregion

        #region Housekeeping and coordinates
        private void Housekeeping()
        {
            if (_prune && Time.unscaledTime >= _nextPrune)
            {
                _prune = false;
                _nextPrune = Time.unscaledTime + 5f;
                CeilingVisibility.Prune();
            }
            EvictRegions();
            // The odd runtime material or mesh a prop makes for itself is only reclaimed by a sweep; one every so often.
            if (_unloadsSinceSweep >= UnloadsPerAssetSweep && _building == null && _navOp == null)
            {
                _unloadsSinceSweep = 0;
                Resources.UnloadUnusedAssets();
            }
        }

        private static long Key(int x, int z) => ((long)x << 32) | (uint)z;

        private int ChunkX(float worldX) => (int)Math.Floor((_originX + worldX) / _chunk);

        private int ChunkZ(float worldZ) => (int)Math.Floor((_originZ + worldZ) / _chunk);

        private Vector3 ChunkMiddle(int cx, int cz)
            => new Vector3((float)((cx + 0.5) * _chunk - _originX), 0f, (float)((cz + 0.5) * _chunk - _originZ));

        private int RegionOf(int c) => c >= 0 ? c / _perRegion : -((-c + _perRegion - 1) / _perRegion);

        private static double Wrap(double v)
        {
            double r = v % RaidMesh.PlanarPeriod;
            return r < 0 ? r + RaidMesh.PlanarPeriod : r;
        }
        #endregion
    }
}
