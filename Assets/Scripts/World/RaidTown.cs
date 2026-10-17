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

        public void Warn(string message) => _warnings.Add(message);
        public IReadOnlyList<string> Warnings => _warnings;

        /// <summary>A room / desk / street light (counted).</summary>
        public void Light(Transform parent, Vector3 pos, Color color, float range, float intensity, bool flicker, bool shadows)
        {
            RoomLight.Spawn(parent, pos, color, range, intensity, flicker, shadows);
            LightCount++;
        }

        /// <summary>A solid outdoor collider (cars, trees, dumpsters, barricades) under <c>world</c>.</summary>
        public void PropCollider(Vector3 center, Vector3 size, float yaw)
        {
            if (_propColliders == null)
            {
                _propColliders = new GameObject("PropColliders");
                _propColliders.transform.SetParent(World, false);
            }
            float k = Mathf.Repeat(yaw, 90f);
            if (k < 1f || k > 89f)
            {
                bool swap = Mathf.Abs(Mathf.Repeat(yaw, 180f) - 90f) < 45f;
                var bc = _propColliders.AddComponent<BoxCollider>();
                bc.center = center;
                bc.size = swap ? new Vector3(size.z, size.y, size.x) : size;
            }
            else
            {
                var go = new GameObject("PropCollider");
                go.transform.SetParent(_propColliders.transform, false);
                go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
                go.AddComponent<BoxCollider>().size = size;
            }
            ColliderCount++;
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

        /// <summary>Emits every outdoor chunk mesh (call once everything is drawn).</summary>
        public void EmitOutdoor()
        {
            Out.Emit("Outdoor");
            Far.Emit("Scenery");
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

        public void Emit(string name)
        {
            for (int i = 0; i < _solid.Length; i++)
            {
                Vector3 c = new Vector3(-_half + (i % _n + 0.5f) * _chunk, 0f, -_half + (i / _n + 0.5f) * _chunk);
                if (_solid[i] != null) _solid[i].Emit(name + "Props", _parent, c, ShadowCastingMode.On);
                if (_flat[i] != null) _flat[i].Emit(name + "Ground", _parent, c, ShadowCastingMode.Off);
            }
        }
    }
}
