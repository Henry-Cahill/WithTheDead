using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// Furnishes the statewide map's buildings near the survivors and takes them down again behind them. Each streamed piece
    /// leaves its buildings' plans here as it's built (<see cref="InteriorPass.Build"/>); once the piece is in place the
    /// nearest building within <see cref="BuildReach"/> of any survivor is raised (<see cref="InteriorBuild"/>, hidden until
    /// it's whole), a few milliseconds a frame; one everybody has left <see cref="DropReach"/> behind is taken down. What
    /// was in its containers is kept by the building's id (the same building builds the same containers in the same order):
    /// built again, it gets back what was left in it, and what was carried off stays gone. A building built for the first
    /// time rolls this raid's loot, from the raid's seed and the building's id (the same on every machine of a co-op raid,
    /// whatever order they build in). The NavMesh is asked to take in the new walls and furniture now and then.
    /// </summary>
    internal sealed class InteriorStreamer : MonoBehaviour
    {
        private const float BuildReach = 45f, DropReach = 70f;
        private const float StepBudget = 0.003f;   // seconds a frame spent furnishing
        private const float PickInterval = 0.2f;
        private const float NavInterval = 1.5f;
        private const float PruneInterval = 5f;
        private const int MemoryKept = 4096;       // buildings whose containers are remembered

        internal static InteriorStreamer Active { get; private set; }

        private sealed class Job
        {
            public InteriorPlan Plan;
            public Transform Parent, Overhead;
            /// <summary>The footprint's middle and reach, site-local.</summary>
            public Vector2 Centre;
            public float Radius;
            public InteriorBuild Build;
            public IEnumerator<string> Steps;
            public bool Failed;
        }

        private sealed class Memory
        {
            public Inventory[] Contents;
            public bool[] Searched, Gone;
            public int Stamp;
        }

        private readonly List<Job> _jobs = new List<Job>(512);
        private readonly HashSet<long> _registered = new HashSet<long>();
        private readonly Dictionary<long, Memory> _memory = new Dictionary<long, Memory>();
        private readonly Queue<(long id, int stamp)> _memoryOrder = new Queue<(long, int)>();
        private readonly List<Vector3> _focus = new List<Vector3>(4);
        private Job _active;
        private int _stamp, _built, _live;
        private bool _navDirty, _prune, _haveNavBounds;
        private Bounds _navBounds;
        private float _nextPick, _nextNav, _nextPrune;

        /// <summary>Buildings furnished so far, and how many stand furnished now.</summary>
        internal string Summary => _built + " interiors furnished, " + _live + " standing, " + _jobs.Count + " known";

        /// <summary>Leaves a streamed piece's building plans to be furnished when a survivor comes near (made on first use,
        /// under the state's streamer).</summary>
        internal static void Register(Site site, List<InteriorPlan> plans)
        {
            if (Active == null)
            {
                var go = new GameObject("InteriorStreamer");
                KentuckyStreamer streamer = KentuckyStreamer.Active;
                if (streamer != null) go.transform.SetParent(streamer.transform, false);
                Active = go.AddComponent<InteriorStreamer>();
            }
            Active.Add(site, plans);
        }

        private void Add(Site site, List<InteriorPlan> plans)
        {
            foreach (InteriorPlan plan in plans)
            {
                if (!_registered.Add(plan.Id)) continue;
                Rect b = plan.Footprint.Bounds;
                _jobs.Add(new Job
                {
                    Plan = plan, Parent = site.Town.World, Overhead = site.Town.Overhead, Centre = b.center, Radius = b.size.magnitude * 0.5f,
                });
            }
        }

        #region Every frame
        private void Update()
        {
            float now = Time.unscaledTime;
            DropOrphans();
            GatherFocus();
            if (_focus.Count > 0)
            {
                TakeDownBehind();
                if (_active == null && now >= _nextPick)
                {
                    _nextPick = now + PickInterval;
                    _active = Pick();
                    if (_active != null)
                    {
                        _active.Build = new InteriorBuild(_active.Plan, _active.Parent, _active.Overhead, true);
                        _active.Steps = _active.Build.Steps().GetEnumerator();
                    }
                }
                if (_active != null) Advance(_active);
            }
            if (_navDirty && now >= _nextNav)
            {
                _navDirty = false;
                _nextNav = now + NavInterval;
                KentuckyStreamer streamer = KentuckyStreamer.Active;
                if (streamer != null && _haveNavBounds) streamer.InvalidateNav(_navBounds);
                _haveNavBounds = false;
            }
            if (_prune && now >= _nextPrune)
            {
                _prune = false;
                _nextPrune = now + PruneInterval;
                CeilingVisibility.Prune();
            }
        }

        private void GatherFocus()
        {
            _focus.Clear();
            Actor local = Actor.LocalPlayer;
            if (local != null) _focus.Add(local.transform.position);
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a != null && a != local && a.IsPlayerControlled) _focus.Add(a.transform.position);
            }
        }

        // How far the nearest survivor is from a building's outline (roughly: from its middle, less its reach).
        private float Distance(Job j)
        {
            Vector3 p = j.Parent.position;
            float x = p.x + j.Centre.x, z = p.z + j.Centre.y, best = float.MaxValue;
            for (int i = 0; i < _focus.Count; i++)
            {
                float dx = _focus[i].x - x, dz = _focus[i].z - z;
                best = Mathf.Min(best, dx * dx + dz * dz);
            }
            return Mathf.Sqrt(best) - j.Radius;
        }

        // The jobs whose piece has been let go: what their containers held is remembered (the objects went with it).
        private void DropOrphans()
        {
            for (int i = _jobs.Count - 1; i >= 0; i--)
            {
                Job j = _jobs[i];
                if (j.Parent != null) continue;
                if (j.Build != null) TakeDown(j);
                if (_active == j) _active = null;
                _registered.Remove(j.Plan.Id);
                _jobs[i] = _jobs[_jobs.Count - 1];
                _jobs.RemoveAt(_jobs.Count - 1);
            }
        }

        private void TakeDownBehind()
        {
            for (int i = 0; i < _jobs.Count; i++)
            {
                Job j = _jobs[i];
                if (j.Build == null || Distance(j) <= DropReach) continue;
                if (_active == j) _active = null;
                TakeDown(j);
            }
        }

        // The nearest building within reach of a survivor not yet furnished, in a piece that's in place.
        private Job Pick()
        {
            Job best = null;
            float bestD = BuildReach;
            for (int i = 0; i < _jobs.Count; i++)
            {
                Job j = _jobs[i];
                if (j.Build != null || j.Failed || !j.Parent.gameObject.activeInHierarchy) continue;
                float d = Distance(j);
                if (d < bestD) { bestD = d; best = j; }
            }
            return best;
        }

        // A few milliseconds of a building's furnishing, its meshes credited to it and its loot outside the Central Economy.
        private void Advance(Job j)
        {
            List<Mesh> meshes = RaidMesh.CreatedMeshes;
            Vector3 uv = RaidMesh.PlanarOrigin;
            System.Func<string, bool> gate = ItemDatabase.LootGate;
            System.Action<string, int> rolled = ItemDatabase.LootRolled;
            RaidMesh.CreatedMeshes = j.Build.Meshes;
            Vector3 at = j.Parent.position;
            RaidMesh.PlanarOrigin = new Vector3(Wrap(at.x) - at.x, 0f, Wrap(at.z) - at.z);
            ItemDatabase.LootGate = null;
            ItemDatabase.LootRolled = null;
            bool done = false;
            float until = Time.realtimeSinceStartup + StepBudget;
            try
            {
                do
                {
                    if (!j.Steps.MoveNext()) { done = true; break; }
                } while (Time.realtimeSinceStartup < until);
                if (done) Restore(j);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CloneZ] Interiors: furnishing building " + j.Plan.Id + " (" + j.Plan.Use + ") failed: " + e);
                j.Failed = true;
            }
            finally
            {
                RaidMesh.CreatedMeshes = meshes;
                RaidMesh.PlanarOrigin = uv;
                ItemDatabase.LootGate = gate;
                ItemDatabase.LootRolled = rolled;
            }
            if (j.Failed)
            {
                j.Build.Discard();
                j.Build = null;
                j.Steps = null;
                _active = null;
                return;
            }
            if (!done) return;
            j.Steps = null;
            _active = null;
            _built++;
            _live++;
            Touch(j.Build.WorldBounds);
        }

        private static float Wrap(float v)
        {
            double r = v % RaidMesh.PlanarPeriod;
            return (float)(r < 0 ? r + RaidMesh.PlanarPeriod : r);
        }

        private void Touch(Bounds b)
        {
            if (_haveNavBounds) _navBounds.Encapsulate(b);
            else
            {
                _navBounds = b;
                _haveNavBounds = true;
            }
            _navDirty = true;
        }
        #endregion

        #region What was taken stays taken
        // A building just furnished: what was left in it last time, else this raid's loot.
        private void Restore(Job j)
        {
            List<LootContainer> boxes = j.Build.Containers;
            long id = j.Plan.Id;
            _memory.TryGetValue(id, out Memory kept);
            if (kept != null && kept.Contents.Length != boxes.Count) kept = null;
            int raid = GameSession.RaidSeed;
            for (int i = 0; i < boxes.Count; i++)
            {
                LootContainer box = boxes[i];
                if (box == null || box.Type == ContainerType.Corpse || box.IsSack) continue;
                if (kept != null)
                {
                    if (kept.Gone[i])
                    {
                        Destroy(box.gameObject);
                        continue;
                    }
                    if (kept.Contents[i] != null)
                    {
                        box.Contents = kept.Contents[i];
                        box.Searched = kept.Searched[i];
                        continue;
                    }
                }
                int seed = unchecked(raid ^ (int)(id * 0x9E3779B1L) ^ (int)(id >> 29) ^ (i * 83492791) ^ 0x51ED);
                RaidWorldRules.RollLoot(box, new System.Random(seed));
            }
            if (kept != null) _memory.Remove(id);
        }

        // Takes a building's interior down, remembering what its containers hold (and which were carried off or broken up).
        private void TakeDown(Job j)
        {
            InteriorBuild b = j.Build;
            j.Build = null;
            j.Steps = null;
            if (b == null) return;
            if (b.Done)
            {
                bool standing = b.Root != null;
                int n = b.Containers.Count;
                var m = new Memory { Contents = new Inventory[n], Searched = new bool[n], Gone = new bool[n], Stamp = ++_stamp };
                for (int i = 0; i < n; i++)
                {
                    LootContainer box = b.Containers[i];
                    if (ReferenceEquals(box, null)) continue;
                    // Destroyed while the building stood: carried off or broken up. (With the piece gone, every object is.)
                    if (standing && box == null) { m.Gone[i] = true; continue; }
                    m.Contents[i] = box.Contents;
                    m.Searched[i] = box.Searched;
                }
                _memory[j.Plan.Id] = m;
                _memoryOrder.Enqueue((j.Plan.Id, m.Stamp));
                while (_memory.Count > MemoryKept && _memoryOrder.Count > 0)
                {
                    (long id, int stamp) = _memoryOrder.Dequeue();
                    if (_memory.TryGetValue(id, out Memory old) && old.Stamp == stamp) _memory.Remove(id);
                }
                _live--;
                if (standing) Touch(b.WorldBounds);
            }
            b.Discard();
            _prune = true;
        }
        #endregion

        private void OnDestroy()
        {
            foreach (Job j in _jobs)
            {
                if (j.Build == null) continue;
                j.Build.Discard();
                j.Build = null;
            }
            _jobs.Clear();
            if (Active == this) Active = null;
        }
    }
}
