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

        /// <summary>Records a warning message to be reported later.</summary>
    public void Warn(string message) => _warnings.Add(message);
        /// <summary>Read‑only list of warning messages recorded during town build.</summary>
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
        /// Keep the prop colliders as boxes until <see cref="MergeColliders"/>/<see cref="EmitOutdoor"/> makes them one
        /// <see cref="MeshCollider"/> (the Kentucky map: hundreds of turned walls a piece cost an object each, and most of a
        /// build's main-thread time for colliders; one mesh baked on a worker costs a thirtieth, and rays hit it alike).
        /// Off for the procedural town, whose colliders stay boxes.
        /// </summary>
        public bool MergedColliders;

        private readonly List<ColliderBox> _boxes = new List<ColliderBox>();

        /// <summary>A prop collider kept for merging: a box turned <see cref="Yaw"/> degrees about Y.</summary>
        internal struct ColliderBox
        {
            public Vector3 Center, Size;
            public float Yaw;
        }

        /// <summary>The prop colliders kept for merging and not yet made.</summary>
        internal int PendingColliders => _boxes.Count;

        /// <summary>A solid outdoor collider (cars, trees, dumpsters, barricades) under <c>world</c>.</summary>
        public void PropCollider(Vector3 center, Vector3 size, float yaw)
        {
            if (MergedColliders)
            {
                _boxes.Add(new ColliderBox { Center = center, Size = size, Yaw = yaw });
                ColliderCount++;
                Colliders++;
                return;
            }
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            if (_propColliders == null)
            {
                _propColliders = new GameObject("PropColliders");
                _propColliders.transform.SetParent(World, false);
                ColliderObjects++;
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
                ColliderObjects++;
            }
            ColliderCount++;
            Colliders++;
            ColliderTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
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
        /// <see cref="Bake"/>, and makes the prop colliders kept for merging.</summary>
        public void EmitOutdoor()
        {
            ColliderMerge merge = _boxes.Count > 0 ? StartColliders() : null;
            Out.Emit("Outdoor");
            Far.Emit("Scenery");
            if (_bake != null && _bake.Count > 0)
            {
                _bake.Allocate();
                _bake.Pack();
                _bake.Apply();
                _bake = null;
            }
            if (merge != null) FinishColliders(merge);
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

        /// <summary>Starts making the prop colliders kept so far into one collision mesh: the mesh now, its PhysX cooking
        /// on a worker thread (see <see cref="FinishColliders"/>).</summary>
        internal ColliderMerge StartColliders()
        {
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var merge = ColliderMerge.Start(_boxes);
            _boxes.Clear();
            ColliderTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            return merge;
        }

        /// <summary>Puts the merged colliders under <c>world</c>, waiting for their cooking if it isn't done.</summary>
        internal void FinishColliders(ColliderMerge merge)
        {
            if (merge == null || merge.Mesh == null) return;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            merge.Finish(World);
            ColliderObjects++;
            ColliderTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
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

    /// <summary>
    /// Prop colliders made one <see cref="MeshCollider"/>: every box's eight corners and twelve outward triangles in one
    /// mesh (made on the main thread), cooked by PhysX on a worker thread (<see cref="Physics.BakeMesh(EntityId, bool,
    /// MeshColliderCookingOptions)"/>), then given to a collider with the same cooking options, which takes the cooked
    /// data as it is. Boxes keep their exact turn (the per-box colliders squared anything within a degree of the grid).
    /// </summary>
    internal sealed class ColliderMerge
    {
        internal const MeshColliderCookingOptions Cooking = MeshColliderCookingOptions.CookForFasterSimulation
            | MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.WeldColocatedVertices
            | MeshColliderCookingOptions.UseFastMidphase;

        public Mesh Mesh;
        public int Boxes;
        private System.Threading.Tasks.Task _bake;

        // A box's twelve triangles over its corners (bit 1 = +x, 2 = +y, 4 = +z), wound to face out (a turn about Y and
        // positive sizes keep them so).
        private static readonly int[] s_Triangles = MakeTriangles();

        private static int[] MakeTriangles()
        {
            int[] faces = { 1, 3, 7, 5, 0, 4, 6, 2, 2, 6, 7, 3, 0, 1, 5, 4, 4, 5, 7, 6, 0, 2, 3, 1 };
            var corner = new Vector3[8];
            for (int k = 0; k < 8; k++) corner[k] = new Vector3((k & 1) != 0 ? 1f : -1f, (k & 2) != 0 ? 1f : -1f, (k & 4) != 0 ? 1f : -1f);
            var tris = new int[36];
            for (int f = 0; f < 6; f++)
            {
                int a = faces[f * 4], b = faces[f * 4 + 1], c = faces[f * 4 + 2], d = faces[f * 4 + 3];
                Vector3 outward = corner[a] + corner[b] + corner[c] + corner[d];
                if (Vector3.Dot(Vector3.Cross(corner[b] - corner[a], corner[c] - corner[a]), outward) < 0f)
                {
                    int t = b;
                    b = d;
                    d = t;
                }
                tris[f * 6] = a; tris[f * 6 + 1] = b; tris[f * 6 + 2] = c;
                tris[f * 6 + 3] = a; tris[f * 6 + 4] = c; tris[f * 6 + 5] = d;
            }
            return tris;
        }

        public static ColliderMerge Start(List<RaidTown.ColliderBox> boxes)
        {
            var merge = new ColliderMerge { Boxes = boxes.Count };
            if (boxes.Count == 0) return merge;
            var v = new Vector3[boxes.Count * 8];
            var t = new int[boxes.Count * 36];
            for (int i = 0; i < boxes.Count; i++)
            {
                RaidTown.ColliderBox b = boxes[i];
                Quaternion q = Quaternion.Euler(0f, b.Yaw, 0f);
                Vector3 ax = q * new Vector3(b.Size.x * 0.5f, 0f, 0f), ay = new Vector3(0f, b.Size.y * 0.5f, 0f), az = q * new Vector3(0f, 0f, b.Size.z * 0.5f);
                int v0 = i * 8;
                for (int k = 0; k < 8; k++)
                    v[v0 + k] = b.Center + ((k & 1) != 0 ? ax : -ax) + ((k & 2) != 0 ? ay : -ay) + ((k & 4) != 0 ? az : -az);
                int t0 = i * 36;
                for (int k = 0; k < 36; k++) t[t0 + k] = v0 + s_Triangles[k];
            }
            var mesh = new Mesh { name = "PropColliders", indexFormat = v.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            RaidMesh.CreatedMeshes?.Add(mesh);
            mesh.SetVertices(v);
            mesh.SetIndices(t, MeshTopology.Triangles, 0, true);
            merge.Mesh = mesh;
            EntityId id = mesh.GetEntityId();
            merge._bake = System.Threading.Tasks.Task.Run(() => Physics.BakeMesh(id, false, Cooking));
            return merge;
        }

        /// <summary>True once the cooking is done (or failed: the collider then cooks the mesh itself).</summary>
        public bool Ready => _bake == null || _bake.IsCompleted;

        public void Finish(Transform parent)
        {
            if (Mesh == null) return;
            try { _bake?.Wait(); }
            catch (System.AggregateException) { }   // cooked on the main thread below instead
            var go = new GameObject("PropColliders");
            go.transform.SetParent(parent, false);
            var collider = go.AddComponent<MeshCollider>();
            collider.cookingOptions = Cooking;
            collider.sharedMesh = Mesh;
        }
    }
}
