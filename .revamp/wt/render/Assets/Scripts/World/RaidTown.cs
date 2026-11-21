using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Shared state while one raid map is being built: the seeded random stream, the three roots
    /// (the baked <c>world</c>, the overhead pieces that show only from inside, and the scenery past
    /// the fence that never reaches the navmesh), the chunked outdoor meshes, prop colliders, spawn
    /// candidates and the running tallies reported at the end.
    /// </summary>
    internal sealed class RaidTown
    {
        public readonly System.Random Rng;
        public readonly Transform World;
        /// <summary>Ceilings, the tent roof and the canopy: not baked, shown only from inside views.</summary>
        public readonly Transform Overhead;
        /// <summary>Scenery beyond the playable edge: not baked, no colliders.</summary>
        public readonly Transform Scenery;
        public readonly RaidMapInfo Info;

        public readonly List<RaidBuilding> Buildings = new List<RaidBuilding>();
        /// <summary>Outdoor areas props must keep clear of (buildings, doorsteps, exits, lanes).</summary>
        public readonly List<Rect> Keepout = new List<Rect>();
        public readonly OutdoorBatches Out;
        public readonly OutdoorBatches Far;

        public int ContainerCount;
        public int LightCount;
        public int ColliderCount;
        private readonly List<string> _warnings = new List<string>();

        /// <summary>Where the survivor may start: a world position (y = 0) and a yaw.</summary>
        public readonly List<(Vector3 pos, float yaw, RaidBuilding building, RaidRoom room)> SpawnCandidates
            = new List<(Vector3, float, RaidBuilding, RaidRoom)>();

        private GameObject _propColliders;
        private GameObject _ceilingColliders;

        /// <summary>Layer for overhead colliders: hit by the shoulder camera's collision cast (all layers
        /// but Ignore Raycast) yet skipped by Default-only rays (build placement) and the cutaway.</summary>
        public const int OverheadColliderLayer = 1;

        public RaidTown(Transform world, int seed, RaidMapInfo info)
        {
            Rng = new System.Random(seed);
            World = world;
            Info = info;
            Overhead = new GameObject("RaidOverhead").transform;
            Overhead.gameObject.AddComponent<CeilingVisibility>();
            Scenery = new GameObject("RaidScenery").transform;
            Out = new OutdoorBatches(world, 35f, info.HalfSize + 2f);
            Far = new OutdoorBatches(Scenery, 60f, info.HalfSize + 60f);
        }

        /// <summary>One piece of a streamed map: its overhead and scenery roots go under
        /// <paramref name="overheadParent"/> (the map's one overhead root, which carries the
        /// <see cref="CeilingVisibility"/>), so they unload with the piece.</summary>
        public RaidTown(Transform world, int seed, RaidMapInfo info, Transform overheadParent)
        {
            Rng = new System.Random(seed);
            World = world;
            Info = info;
            Overhead = new GameObject("Overhead").transform;
            Overhead.SetParent(overheadParent, false);
            Scenery = new GameObject("Scenery").transform;
            Scenery.SetParent(overheadParent, false);
            Out = new OutdoorBatches(world, 35f, info.HalfSize + 2f);
            Far = new OutdoorBatches(Scenery, 60f, info.HalfSize + 60f);
        }

        /// <summary>Records a layout note, reported (as one warning) once the map is built.</summary>
        public void Warn(string message) => _warnings.Add(message);
        /// <summary>The layout notes recorded so far.</summary>
        public IReadOnlyList<string> Warnings => _warnings;

        /// <summary>A room / desk / street light (counted).</summary>
        public void Light(Transform parent, Vector3 pos, Color color, float range, float intensity, bool flicker, bool shadows)
        {
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            RoomLight.Spawn(parent, pos, color, range, intensity, flicker, shadows);
            LightCount++;
            Lights++;
            LightTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        }

        /// <summary>Running totals over every town built in the process (the map harness reads them around a build):
        /// colliders made, the objects made to hold them, lights, and the time spent making each.</summary>
        internal static long Colliders, ColliderObjects, ColliderTicks, Lights, LightTicks;

        /// <summary>
        /// Keep the prop colliders as plain boxes until <see cref="MakeColliders"/> (or <see cref="EmitOutdoor"/>) makes
        /// them, so a streamed piece makes them in a step of their own (the Kentucky map). Off for the procedural town,
        /// whose colliders are made as they're asked for.
        /// </summary>
        internal bool DeferColliders;

        private readonly List<ColliderBox> _boxes = new List<ColliderBox>();
        private int _boxesMade;

        /// <summary>A prop collider kept for later: a box turned <see cref="Yaw"/> degrees about Y.</summary>
        internal struct ColliderBox
        {
            public Vector3 Center, Size;
            public float Yaw;
        }

        /// <summary>The prop colliders kept for later and not yet made.</summary>
        internal int PendingColliders => _boxes.Count - _boxesMade;

        /// <summary>A solid outdoor collider (cars, trees, dumpsters, barricades) under <c>world</c>.</summary>
        public void PropCollider(Vector3 center, Vector3 size, float yaw)
        {
            ColliderCount++;
            Colliders++;
            if (DeferColliders)
            {
                _boxes.Add(new ColliderBox { Center = center, Size = size, Yaw = yaw });
                return;
            }
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            MakeCollider(center, size, yaw, false);
            ColliderTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        }

        /// <summary>Makes the prop colliders kept so far (see <see cref="DeferColliders"/>) until about
        /// <paramref name="ms"/> milliseconds have gone (0: all of them). True once none are left.</summary>
        internal bool MakeColliders(double ms = 0)
        {
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            long until = ms > 0 ? t0 + (long)(ms * System.Diagnostics.Stopwatch.Frequency / 1000.0) : long.MaxValue;
            while (_boxesMade < _boxes.Count)
            {
                ColliderBox b = _boxes[_boxesMade++];
                MakeCollider(b.Center, b.Size, b.Yaw, true);
                if ((_boxesMade & 7) == 0 && System.Diagnostics.Stopwatch.GetTimestamp() >= until) break;
            }
            if (_boxesMade == _boxes.Count)
            {
                _boxes.Clear();
                _boxesMade = 0;
            }
            ColliderTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            return _boxes.Count == 0;
        }

        // Boxes square to the grid go on the shared PropColliders object; the others each on an object of their own, or
        // (<paramref name="grouped"/>, the Kentucky map) on an object turned their way shared by every box turned the same
        // (a row of parked cars, a fence's posts). A holder takes at most this many (adding components to one object gets
        // dearer the more it has).
        private const int BoxesPerHolder = 32;
        private GameObject _squareHolder;
        private int _squareCount;
        private readonly Dictionary<float, GameObject> _turnedHolders = new Dictionary<float, GameObject>();
        private readonly Dictionary<GameObject, int> _holderCounts = new Dictionary<GameObject, int>();

        private void MakeCollider(Vector3 center, Vector3 size, float yaw, bool grouped)
        {
            if (_propColliders == null)
            {
                _propColliders = new GameObject("PropColliders");
                _propColliders.transform.SetParent(World, false);
                _squareHolder = _propColliders;
                ColliderObjects++;
            }
            float k = Mathf.Repeat(yaw, 90f);
            if (k < 1f || k > 89f)
            {
                if (grouped && _squareCount == BoxesPerHolder)
                {
                    _squareHolder = new GameObject("PropColliders");
                    _squareHolder.transform.SetParent(_propColliders.transform, false);
                    _squareCount = 0;
                    ColliderObjects++;
                }
                bool swap = Mathf.Abs(Mathf.Repeat(yaw, 180f) - 90f) < 45f;
                var bc = (grouped ? _squareHolder : _propColliders).AddComponent<BoxCollider>();
                bc.center = center;
                bc.size = swap ? new Vector3(size.z, size.y, size.x) : size;
                _squareCount++;
                return;
            }
            if (!grouped)
            {
                var go = new GameObject("PropCollider");
                go.transform.SetParent(_propColliders.transform, false);
                go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
                go.AddComponent<BoxCollider>().size = size;
                ColliderObjects++;
                return;
            }
            float turn = Mathf.Repeat(yaw, 360f);
            if (!_turnedHolders.TryGetValue(turn, out GameObject holder) || _holderCounts[holder] == BoxesPerHolder)
            {
                // The holder stands on its first box, so that one is exactly where it was asked for.
                holder = new GameObject("PropCollider");
                holder.transform.SetParent(_propColliders.transform, false);
                holder.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
                _turnedHolders[turn] = holder;
                _holderCounts[holder] = 0;
                ColliderObjects++;
            }
            var box = holder.AddComponent<BoxCollider>();
            if (_holderCounts[holder] > 0) box.center = holder.transform.InverseTransformPoint(center);
            box.size = size;
            _holderCounts[holder]++;
        }

        /// <summary>A thin overhead collider (ceiling, canopy, tent roof) under the overhead root, so the
        /// shoulder camera stops under it instead of rising through.</summary>
        public void OverheadCollider(Vector3 center, Vector3 size, Quaternion rotation)
        {
            if (_ceilingColliders == null)
            {
                _ceilingColliders = new GameObject("OverheadColliders");
                _ceilingColliders.layer = OverheadColliderLayer;
                _ceilingColliders.transform.SetParent(Overhead, false);
            }
            if (rotation == Quaternion.identity)
            {
                var bc = _ceilingColliders.AddComponent<BoxCollider>();
                bc.center = center;
                bc.size = size;
                return;
            }
            var go = new GameObject("OverheadCollider") { layer = OverheadColliderLayer };
            go.transform.SetParent(_ceilingColliders.transform, false);
            go.transform.SetPositionAndRotation(center, rotation);
            go.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>True when a world XZ rect overlaps any keep-out area.</summary>
        public bool Blocked(Rect r)
        {
            for (int i = 0; i < Keepout.Count; i++) if (Keepout[i].Overlaps(r)) return true;
            return false;
        }

        /// <summary>Emits every outdoor chunk mesh (call once everything is drawn), bakes what was queued on
        /// <see cref="Bake"/>, and makes the prop colliders still kept (see <see cref="DeferColliders"/>).</summary>
        public void EmitOutdoor()
        {
            MakeColliders();
            Out.Emit("Outdoor");
            Far.Emit("Scenery");
            if (_bake != null && _bake.Count > 0)
            {
                _bake.Allocate();
                _bake.Pack();
                _bake.Apply();
                _bake = null;
            }
        }

        private MeshBake _bake;

        /// <summary>Meshes waiting to be baked all at once (see <see cref="RaidMesh.EmitLater"/>): a streamed piece packs
        /// them off the main thread; otherwise <see cref="EmitOutdoor"/> bakes them.</summary>
        internal MeshBake Bake => _bake ??= new MeshBake { ReleaseSources = true };

        /// <summary>Takes the meshes waiting to be baked (null when none), leaving a fresh queue.</summary>
        internal MeshBake TakeBake()
        {
            MeshBake b = _bake;
            _bake = null;
            return b != null && b.Count > 0 ? b : null;
        }

        /// <summary>Everything instantiated by the build (for the report).</summary>
        public static int CountObjects(Transform root)
        {
            if (root == null) return 0;
            int n = 1;
            foreach (Transform c in root) n += CountObjects(c);
            return n;
        }
    }

    /// <summary>
    /// Outdoor geometry split into square chunks so the camera culls what it can't see: per chunk one
    /// shadow-casting mesh (props, cars, trees) and one flat mesh (roads, sidewalks, decals, litter).
    /// </summary>
    internal sealed class OutdoorBatches
    {
        private readonly Transform _parent;
        private readonly float _chunk;
        private readonly float _half;
        private readonly int _n;
        private readonly RaidMesh[] _solid;
        private readonly RaidMesh[] _flat;

        /// <summary>Creates a new outdoor batch manager for a given parent transform and chunk size.</summary>
        public OutdoorBatches(Transform parent, float chunk, float halfExtent)
        {
            _parent = parent;
            _chunk = chunk;
            _half = halfExtent;
            _n = Mathf.Max(1, Mathf.CeilToInt(halfExtent * 2f / chunk));
            _solid = new RaidMesh[_n * _n];
            _flat = new RaidMesh[_n * _n];
        }

        private int Index(Vector3 p)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((p.x + _half) / _chunk), 0, _n - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((p.z + _half) / _chunk), 0, _n - 1);
            return z * _n + x;
        }

        /// <summary>The shadow-casting mesh for the chunk containing <paramref name="p"/>.</summary>
        public RaidMesh Solid(Vector3 p)
        {
            int i = Index(p);
            return _solid[i] ??= new RaidMesh();
        }

        /// <summary>The flat (no-shadow) mesh for the chunk containing <paramref name="p"/>.</summary>
        public RaidMesh Flat(Vector3 p)
        {
            int i = Index(p);
            return _flat[i] ??= new RaidMesh();
        }

        /// <summary>Emits all generated meshes for this batch with the given name prefix.</summary>
        public void Emit(string name)
        {
            for (int i = 0; i < _solid.Length; i++)
            {
                Vector3 c = new Vector3(-_half + (i % _n + 0.5f) * _chunk, 0f, -_half + (i / _n + 0.5f) * _chunk);
                if (_solid[i] != null) _solid[i].Emit(name + "Props", _parent, c, ShadowCastingMode.On);
                if (_flat[i] != null) _flat[i].Emit(name + "Ground", _parent, c, ShadowCastingMode.Off);
            }
        }

        /// <summary>As <see cref="Emit"/>, but the meshes are left empty for <paramref name="bake"/> to fill (all of a
        /// piece's at once, off the main thread), and the batches are forgotten (their buffers go back to the pool once
        /// baked).</summary>
        internal void EmitLater(string name, MeshBake bake)
        {
            for (int i = 0; i < _solid.Length; i++)
            {
                Vector3 c = new Vector3(-_half + (i % _n + 0.5f) * _chunk, 0f, -_half + (i / _n + 0.5f) * _chunk);
                if (_solid[i] != null) _solid[i].EmitLater(name + "Props", _parent, c, ShadowCastingMode.On, bake);
                if (_flat[i] != null) _flat[i].EmitLater(name + "Ground", _parent, c, ShadowCastingMode.Off, bake);
                _solid[i] = null;
                _flat[i] = null;
            }
        }
    }
}
