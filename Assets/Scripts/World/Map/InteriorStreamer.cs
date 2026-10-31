using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// Furnishes the statewide map's buildings near the survivors and takes them down again behind them. Each streamed piece
    /// leaves its buildings' plans here as it's built (<see cref="InteriorPass.Build"/>); while the piece is in place the
    /// buildings within <see cref="BuildReach"/> of any survivor are raised nearest first (<see cref="InteriorBuild"/>), a few
    /// milliseconds a frame, at most <see cref="MaxStanding"/> at once; one everybody has left <see cref="DropReach"/> behind
    /// is taken down. A building is raised in its live piece (its fixtures wake as they're made, as the raid town's do; a
    /// piece that's been switched off waits). What was in its containers is kept by the building's id (the same building
    /// builds the same containers in the same order): built again, it gets back what was left in it, and what was carried
    /// off stays gone. A building built for the first time rolls this raid's loot, from the raid's seed and the building's
    /// id (the same on every machine of a co-op raid, whatever order they build in). The NavMesh is asked to take in the
    /// new walls and furniture now and then. One MonoBehaviour for the whole map; nothing per building runs every frame.
    /// </summary>
    internal sealed class InteriorStreamer : MonoBehaviour
    {
        internal const float BuildReach = 45f, DropReach = 70f;
        private const int MaxStanding = 48;
        private const float StepBudget = 0.003f;   // seconds a frame spent furnishing
        private const float ScanInterval = 0.25f;  // how often far buildings and let-go pieces are looked for
        private const float NavInterval = 1.5f;
        private const float PruneInterval = 5f;
        private const int MemoryKept = 4096;       // buildings whose containers are remembered

        internal static InteriorStreamer Active { get; private set; }

        /// <summary>For tools (the interiors harness): streamed pieces register their plans in edit mode too, and the tool
        /// drives the streamer itself (<see cref="Pump"/>).</summary>
        internal static bool ToolMode;

        /// <summary>True when a streamed piece's plans should be left here: in play, or for a tool.</summary>
        internal static bool Wanted => Application.isPlaying || ToolMode;

        private sealed class Job
        {
            public InteriorPlan Plan;
            public Transform Parent, Overhead;
            /// <summary>The footprint's middle and reach, site-local.</summary>
            public Vector2 Centre;
            public float Radius;
            public InteriorBuild Build;
            public bool Failed;
        }

        private sealed class Memory
        {
            public Inventory[] Contents;
            public bool[] Searched, Gone;
            /// <summary>The building's furniture pieces (in the order they were made) broken up while it stood.</summary>
            public bool[] FurnitureGone;
            /// <summary>Its fixtures' states by save key (null when none kept one).</summary>
            public List<FixtureSave> Fixtures;
            public int Stamp;
        }

        private readonly List<Job> _jobs = new List<Job>(512);
        private readonly HashSet<long> _registered = new HashSet<long>();
        private readonly Dictionary<long, Memory> _memory = new Dictionary<long, Memory>();
        private readonly Queue<(long id, int stamp)> _memoryOrder = new Queue<(long, int)>();
        private readonly List<Vector3> _focus = new List<Vector3>(4);
        private readonly Dictionary<Job, float> _distCache = new Dictionary<Job, float>(512);
        private Job _active;
        private int _stamp, _built, _live, _failed, _takenDown;
        private bool _navDirty, _prune, _haveNavBounds;
        private Bounds _navBounds;
        private float _nextScan, _nextNav, _nextPrune;

        /// <summary>The longest a frame's furnishing has run (ms), for the harness.</summary>
        internal double SlowestFrameMs;

        /// <summary>Buildings furnished so far, how many stand furnished now, how many are known.</summary>
        internal string Summary => _built + " interiors furnished (" + _failed + " failed, " + _takenDown + " taken down), " + _live + " standing, "
            + _jobs.Count + " known";

        internal int Standing => _live;
        internal int Known => _jobs.Count;
        internal int Furnished => _built;
        internal int FailedCount => _failed;

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
                if (!_registered.Add(plan.Key)) continue;
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
            if (ToolMode) return;
            GatherFocus();
            Tick(Time.unscaledTime);
        }

        /// <summary>One frame of the streamer with the survivors at <paramref name="focus"/> (world), at time
        /// <paramref name="now"/> (seconds): what the game's Update does, for a tool.</summary>
        internal void Pump(IList<Vector3> focus, float now)
        {
            _focus.Clear();
            for (int i = 0; i < focus.Count; i++) _focus.Add(focus[i]);
            Tick(now);
        }

        /// <summary>Runs per-frame interior streaming logic based on survivor focus and timing intervals.</summary>
        private void Tick(float now)
        {
            _distCache.Clear();
            // Periodic scan: clean up orphaned jobs (pieces that disappeared) and take down interiors left far behind the survivors.
            if (now >= _nextScan)
            {
                _nextScan = now + ScanInterval;
                DropOrphans();
                if (_focus.Count > 0) TakeDownBehind();
            }
            // If any survivor is in range, attempt to furnish the nearest eligible building(s) this frame.
            if (_focus.Count > 0) Furnish();
            // When fixtures have changed, schedule a NavMesh rebuild after a short delay to avoid thrashing.
            if (_navDirty && now >= _nextNav)
            {
                _navDirty = false;
                _nextNav = now + NavInterval;
                KentuckyStreamer streamer = KentuckyStreamer.Active;
                if (streamer != null && _haveNavBounds) streamer.InvalidateNav(_navBounds);
                _haveNavBounds = false;
            }
            // Periodically prune ceiling visibility data to free memory and keep performance stable.
            if (_prune && now >= _nextPrune)
            {
                _prune = false;
                _nextPrune = now + PruneInterval;
                CeilingVisibility.Prune();
            }
        }

        // A few milliseconds of furnishing: the building under way, then the next nearest, until the frame's budget is spent.
        private void Furnish()
        {
            float started = Time.realtimeSinceStartup, until = started + StepBudget;
            for (int guard = 0; guard < 64; guard++)
            {
                if (_active == null)
                {
                    if (_live >= MaxStanding) break;
                    _active = Pick();
                    if (_active == null) break;
                    _active.Build = new InteriorBuild(_active.Plan, _active.Parent, _active.Overhead, false);
                    _live++;
                }
                // A piece switched off (or gone) holds its building where it is: its fixtures must wake as they're made.
                if (_active.Parent == null || !_active.Parent.gameObject.activeInHierarchy) break;
                if (!Advance(_active, until)) break;
                if (Time.realtimeSinceStartup >= until) break;
            }
            double ms = (Time.realtimeSinceStartup - started) * 1000.0;
            if (ms > SlowestFrameMs) SlowestFrameMs = ms;
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

        // How far the nearest survivor is from a building's outline (roughly: from its middle, less its reach), a floor above
        // or below the one they're on counting a couple of metres a metre of height (the floor they're on is furnished first).
        private float Distance(Job j)
        {
            if (_distCache.TryGetValue(j, out float c)) return c;
            Vector3 p = j.Parent.position;
            float x = p.x + j.Centre.x, z = p.z + j.Centre.y, y = p.y + j.Plan.FloorY, best = float.MaxValue;
            for (int i = 0; i < _focus.Count; i++)
            {
                float dx = _focus[i].x - x, dz = _focus[i].z - z;
                best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz) + Mathf.Abs(_focus[i].y - y) * 2f);
            }
            c = best - j.Radius;
            _distCache[j] = c;
            return c;
        }

        // The jobs whose piece has been let go: what their containers held is remembered (the objects went with it).
        private void DropOrphans()
        {
            for (int i = _jobs.Count - 1; i >= 0; i--)
            {
                Job j = _jobs[i];
                if (j.Parent != null) continue;
                if (_active == j) _active = null;
                if (j.Build != null) TakeDown(j);
                _registered.Remove(j.Plan.Key);
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
                if (j.Build != null || j.Failed || j.Parent == null || !j.Parent.gameObject.activeInHierarchy) continue;
                float d = Distance(j);
                if (d < bestD) { bestD = d; best = j; }
            }
            return best;
        }

        // Steps of a building's furnishing until it's done or the frame's time is up, its meshes credited to it and its loot
        // outside the Central Economy. True when it finished (or failed) and the next may start.
        private bool Advance(Job j, float until)
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
            try
            {
                do
                {
                    if (!j.Build.Advance()) { done = true; break; }
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
                // Taken down, and loose loot left in its place (once: a failed building isn't tried again).
                j.Build.Discard();
                j.Build = null;
                _active = null;
                _live--;
                _failed++;
                System.Func<string, bool> g = ItemDatabase.LootGate;
                System.Action<string, int> r = ItemDatabase.LootRolled;
                ItemDatabase.LootGate = null;
                ItemDatabase.LootRolled = null;
                try { InteriorPass.LooseLoot(j.Plan, j.Parent, j.Parent.position); }
                catch (System.Exception e) { Debug.LogWarning("[CloneZ] Interiors: loose loot for building " + j.Plan.Id + " failed: " + e.Message); }
                finally
                {
                    ItemDatabase.LootGate = g;
                    ItemDatabase.LootRolled = r;
                }
                return true;
            }
            if (!done) return false;
            _active = null;
            _built++;
            Touch(j.Build.WorldBounds);
            // A line in the log the first time (and every hundredth), so a play session's log shows the interiors at work.
            if (!ToolMode && (_built == 1 || _built % 100 == 0))
                Debug.Log("[CloneZ] Interiors: " + j.Plan.Use + " " + j.Plan.Id + " furnished near the survivor in " + Mathf.RoundToInt((float)j.Build.TotalMs)
                    + " ms over " + j.Build.StepCount + " steps (" + j.Build.Containers.Count + " containers, " + j.Build.Lights + " lights); " + Summary);
            return true;
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
        // A building just furnished: what was left in it last time (its containers' contents, the furniture broken up, its
        // doors and other fixtures as they were left), else this raid's loot.
        /// <summary>Restores a building's state (containers, furniture, fixtures) after it has been furnished.</summary>
        private void Restore(Job j)
        {
            List<LootContainer> boxes = j.Build.Containers;
            // A building's other floors are kept (and roll their loot) apart from its ground floor.
            long id = j.Plan.Key;
            // Retrieve any previously saved memory for this building (containers, furniture, fixtures).
            _memory.TryGetValue(id, out Memory kept);
            // Furniture broken up while the building last stood comes down again, whole (its drawers with it), switched off
            // first so a merged piece's share of its room's mesh goes with it.
            List<Transform> brokenUp = null;
            List<HomeFurniture> furniture = j.Build.Furniture;
            // Re‑activate any furniture pieces that were broken up in the previous run.
            if (kept != null && kept.FurnitureGone != null && kept.FurnitureGone.Length == furniture.Count)
                for (int i = 0; i < furniture.Count; i++)
                {
                    HomeFurniture piece = furniture[i];
                    if (!kept.FurnitureGone[i] || piece == null) continue;
                    (brokenUp ??= new List<Transform>(4)).Add(piece.transform);
                    piece.gameObject.SetActive(false);
                    InteriorPass.Kill(piece.gameObject);
                }
            // Restore each container's saved contents unless it was removed or belongs to a broken piece.
            bool keptBoxes = kept != null && kept.Contents.Length == boxes.Count;
            int raid = GameSession.RaidSeed;
            for (int i = 0; i < boxes.Count; i++)
            {
                LootContainer box = boxes[i];
                if (box == null || box.Type == ContainerType.Corpse || box.IsSack) continue;
                if (brokenUp != null && Under(box.transform, brokenUp)) continue;
                if (keptBoxes)
                {
                    if (kept.Gone[i])
                    {
                        InteriorPass.Kill(box.gameObject);
                        continue;
                    }
                    if (kept.Contents[i] != null)
                    {
                        box.Contents = kept.Contents[i];
                        box.Searched = kept.Searched[i];
                        continue;
                    }
                }
                RaidWorldRules.RollLoot(box, new System.Random(LootSeed(raid, id, i)));
            }
            // If the building was standing, re‑apply saved fixture states; otherwise just clear memory.
            if (kept != null && kept.Fixtures != null) RestoreFixtures(j.Build, kept.Fixtures);
            // Finally, discard the saved memory entry now that the building state is restored.
            if (kept != null) _memory.Remove(id);
        }

        private static bool Under(Transform t, List<Transform> roots)
        {
            for (int i = 0; i < roots.Count; i++)
                if (t.IsChildOf(roots[i])) return true;
            return false;
        }

        private static readonly List<Interactable> s_Fixtures = new List<Interactable>(64);
        private static readonly List<Interactable> s_FixtureScratch = new List<Interactable>(64);

        // The building's fixtures (doors, switches, cupboards...), in and over it.
        private static void GatherFixtures(InteriorBuild b)
        {
            s_Fixtures.Clear();
            if (b.Root != null)
            {
                b.Root.GetComponentsInChildren(true, s_FixtureScratch);
                s_Fixtures.AddRange(s_FixtureScratch);
            }
            if (b.Town != null && b.Town.Overhead != null)
            {
                b.Town.Overhead.GetComponentsInChildren(true, s_FixtureScratch);
                s_Fixtures.AddRange(s_FixtureScratch);
            }
            s_FixtureScratch.Clear();
        }

        // What each of the building's fixtures that keeps a state holds now (a door locked, battered or off its hinges, a lamp
        // switched off...), by save key; null when none does.
        /// <summary>Captures the current state of a building's fixtures for later restoration.</summary>
        private static List<FixtureSave> CaptureFixtures(InteriorBuild b)
        {
            List<FixtureSave> saves = null;
            HashSet<string> seen = null;
            GatherFixtures(b);
            // Iterate each unique fixture, ensuring we only capture one state per save key.
            foreach (Interactable f in s_Fixtures)
            {
                string key = f != null ? f.SaveKey : null;
                if (key == null || !(seen ??= new HashSet<string>()).Add(key)) continue;
                var s = new FixtureSave { Key = key };
                try { f.CaptureState(s); }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[CloneZ] Interiors: couldn't keep fixture " + key + ": " + e.Message);
                    continue;
                }
                (saves ??= new List<FixtureSave>(16)).Add(s);
            }
            s_Fixtures.Clear();
            return saves;
        }

        // Puts the states kept by CaptureFixtures back on the rebuilt building's fixtures with the same save keys.
        private static void RestoreFixtures(InteriorBuild b, List<FixtureSave> saves)
        {
            var byKey = new Dictionary<string, FixtureSave>(saves.Count);
            foreach (FixtureSave s in saves)
                if (s != null && s.Key != null && !byKey.ContainsKey(s.Key)) byKey[s.Key] = s;
            GatherFixtures(b);
            // Restoring can add or remove fixtures (a lamp group, a part torn away), so it walks the list gathered first.
            var live = new List<Interactable>(s_Fixtures);
            s_Fixtures.Clear();
            foreach (Interactable f in live)
            {
                string key = f != null ? f.SaveKey : null;
                if (key == null || !byKey.TryGetValue(key, out FixtureSave s)) continue;
                try { f.RestoreState(s); }
                catch (System.Exception e) { Debug.LogWarning("[CloneZ] Interiors: couldn't restore fixture " + key + ": " + e.Message); }
            }
        }

        /// <summary>The seed a building's <paramref name="index"/>th container rolls its loot from in raid
        /// <paramref name="raid"/>.</summary>
        internal static int LootSeed(int raid, long id, int index)
            => unchecked(raid ^ (int)(id * 0x9E3779B1L) ^ (int)(id >> 29) ^ (index * 83492791) ^ 0x51ED);

        // Takes a building's interior down, remembering what its containers hold (and which were carried off or broken up).
        private void TakeDown(Job j)
        {
            InteriorBuild b = j.Build;
            j.Build = null;
            if (b == null) return;
            _live--;
            _takenDown++;
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
                if (standing)
                {
                    int pieces = b.Furniture.Count;
                    m.FurnitureGone = new bool[pieces];
                    for (int i = 0; i < pieces; i++)
                        if (!ReferenceEquals(b.Furniture[i], null) && b.Furniture[i] == null) m.FurnitureGone[i] = true;
                    m.Fixtures = CaptureFixtures(b);
                }
                _memory[j.Plan.Key] = m;
                _memoryOrder.Enqueue((j.Plan.Key, m.Stamp));
                while (_memory.Count > MemoryKept && _memoryOrder.Count > 0)
                {
                    (long id, int stamp) = _memoryOrder.Dequeue();
                    if (_memory.TryGetValue(id, out Memory old) && old.Stamp == stamp) _memory.Remove(id);
                }
                if (standing) Touch(b.WorldBounds);
            }
            b.Discard();
            _prune = true;
        }

        /// <summary>Drops the plans a streamed piece left here, its buildings never to be furnished: a piece whose build failed
        /// (laid as bare snow).</summary>
        internal void Forget(Transform parent)
        {
            for (int i = _jobs.Count - 1; i >= 0; i--)
            {
                Job j = _jobs[i];
                if (j.Parent != parent) continue;
                if (_active == j) _active = null;
                if (j.Build != null)
                {
                    j.Build.Discard();
                    j.Build = null;
                    _live--;
                }
                _registered.Remove(j.Plan.Key);
                _jobs[i] = _jobs[_jobs.Count - 1];
                _jobs.RemoveAt(_jobs.Count - 1);
            }
        }

        /// <summary>For tools: the building with id <paramref name="id"/>, as it stands now (null when it isn't furnished):
        /// its ground floor, or its level <paramref name="level"/>.</summary>
        internal InteriorBuild BuildOf(long id, int level = 0)
        {
            foreach (Job j in _jobs)
                if (j.Plan.Id == id && j.Plan.Level == level) return j.Build != null && j.Build.Done ? j.Build : null;
            return null;
        }

        /// <summary>For tools: the plans known, in the order they were registered.</summary>
        internal void Plans(List<InteriorPlan> into)
        {
            foreach (Job j in _jobs) into.Add(j.Plan);
        }
        #endregion

        /// <summary>Takes every interior down and forgets everything (a tool's teardown; the game's is OnDestroy).</summary>
        internal void Teardown()
        {
            foreach (Job j in _jobs)
            {
                if (j.Build == null) continue;
                j.Build.Discard();
                j.Build = null;
            }
            _jobs.Clear();
            _registered.Clear();
            _memory.Clear();
            _memoryOrder.Clear();
            _active = null;
            _live = 0;
            if (Active == this) Active = null;
            InteriorPass.Kill(gameObject);
        }

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
