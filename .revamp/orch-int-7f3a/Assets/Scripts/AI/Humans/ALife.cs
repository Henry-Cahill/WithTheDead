using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>One squad of the Zone's people, alive whether or not anyone is watching.</summary>
    public sealed class ALifeSquad
    {
        public string Id, Faction, LoadoutId, GoalLabel;
        public int Members;
        public float Strength = 1f;
        public Vector3 Position, Goal;
        public float DwellUntil;
        public bool Online;
        public readonly List<HumanNpc> Live = new List<HumanNpc>();

/// <summary>Gets the loadout configuration for this squad, resolving it by ID and faction.</summary>
        public HumanLoadout Loadout => HumanLoadouts.Resolve(LoadoutId, Faction, null, out _);
        /// <summary>Gets the effective combat power of the squad based on member count, loadout, and strength.</summary>
        public float Power => Members * Loadout.Power * Strength;
    }

    /// <summary>
    /// A-Life: the Zone's people travel between camps, anomaly fields, landmarks and outposts, fight each other, loot
    /// and die, whether or not a player is near. Within ~75 m of a player a squad is "online" — real people with guns
    /// and cover AI; further out it is simulated cheaply: it walks its route, and when it meets a hostile squad the
    /// fight is resolved on paper (bodies, loot and distant gunfire left behind). Squads are remembered between raids
    /// and the time you were away is simulated when you return. By day the people move; at night most hole up in
    /// their camps and the mutants own the dark.
    /// </summary>
    public sealed class ALifeDirector : MonoBehaviour
    {
        /// <summary>Singleton instance of the director, set on creation and cleared on destroy.</summary>
        public static ALifeDirector Instance { get; private set; }
        /// <summary>Read‑only list of all squads managed by this director.</summary>
        public IReadOnlyList<ALifeSquad> Squads => _squads;

        private const float OnlineRange = 75f, OfflineRange = 95f;
        private readonly List<ALifeSquad> _squads = new List<ALifeSquad>();
        private ZoneDef _zone;
        private System.Random _rng;
        private float _tick, _spawnCheck;
        private int _nextId;

        private static readonly Dictionary<string, (string faction, int weight)[]> s_Mix = new Dictionary<string, (string, int)[]>
        {
            ["kestrel"] = new[] { (Factions.Freehold, 30), (Factions.Scavs, 35), (Factions.Vanguard, 15), (Factions.Institute, 10), (Factions.Lantern, 10) },
            ["mirelock"] = new[] { (Factions.Freehold, 25), (Factions.Scavs, 20), (Factions.Vanguard, 20), (Factions.Institute, 15), (Factions.Choir, 10), (Factions.Lantern, 10) },
            ["spillway"] = new[] { (Factions.Choir, 35), (Factions.Vanguard, 25), (Factions.Freehold, 15), (Factions.Scavs, 10), (Factions.Institute, 10), (Factions.Lantern, 5) },
        };

        /// <summary>Initialises the director for a zone with a deterministic random seed and spawns its initial state.</summary>
        public static void Begin(ZoneDef zone, int seed)
        {
            if (Instance != null) Destroy(Instance.gameObject);
            var go = new GameObject("ALifeDirector");
            Instance = go.AddComponent<ALifeDirector>();
            Instance._zone = zone;
            Instance._rng = new System.Random(seed ^ 0xA11FE);
            Instance.Load();
            Instance.SpawnWardens();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #region Persistence
        private void Load()
        {
            AccountProfile acct = ProfileStore.Account;
            ZoneWorldState state = acct != null ? acct.Zone(_zone.Id) : null;
            if (state != null)
            {
                foreach (ALifeSquadState s in state.Squads)
                {
                    if (s.Members <= 0) continue;
                    _squads.Add(new ALifeSquad
                    {
                        Id = s.Id, Faction = s.Faction, LoadoutId = string.IsNullOrEmpty(s.LoadoutId) ? HumanLoadouts.DefaultIdFor(s.Faction, _rng) : s.LoadoutId, Members = s.Members,
                        Position = HumanUtil.ClampToMap(s.Position, 8f), GoalLabel = s.Goal, Strength = s.Strength,
                    });
                }
                // The time you were away: a few minutes of the sim for every hour, so returning finds the Zone moved on.
                long away = state.LastVisitUnix > 0 ? System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - state.LastVisitUnix : 0;
                int steps = Mathf.Clamp((int)(away / 600), 0, 30);
                for (int i = 0; i < steps; i++) SimulateOffline(20f, true);
            }
            foreach (ALifeSquad s in _squads) PickGoal(s);
            TopUp(true);
        }

        /// <summary>Writes the squads back to the account (called when the raid ends).</summary>
        public void Save()
        {
            AccountProfile acct = ProfileStore.Account;
            if (acct == null || _zone == null) return;
            ZoneWorldState state = acct.Zone(_zone.Id);
            state.Squads.Clear();
            foreach (ALifeSquad s in _squads)
            {
                if (s.Online) Absorb(s);
                if (s.Members <= 0) continue;
                state.Squads.Add(new ALifeSquadState { Id = s.Id, Faction = s.Faction, LoadoutId = s.LoadoutId, Members = s.Members, Position = s.Position, Goal = s.GoalLabel, Strength = s.Strength });
            }
            state.LastVisitUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        #endregion

        #region Population
        private int Target
        {
            get
            {
                float n = _zone.HumanSquads * 2f;
                if (HumanUtil.IsNight) n *= 0.6f;
                return Mathf.Clamp(Mathf.RoundToInt(n), 1, 8);
            }
        }

        private void TopUp(bool anywhere)
        {
            int live = 0;
            foreach (ALifeSquad s in _squads) if (s.Members > 0) live++;
            for (int i = live; i < Target; i++)
            {
                string faction = RollFaction();
                var s = new ALifeSquad
                {
                    Id = "sq" + (_nextId++) + "_" + _rng.Next(1000),
                    Faction = faction,
                    LoadoutId = HumanLoadouts.DefaultIdFor(faction, _rng),
                    Members = faction == Factions.Choir ? 2 + _rng.Next(3) : 2 + _rng.Next(3),
                };
                s.Position = anywhere ? HumanUtil.RandomMapPoint(_rng, 12f) : HumanUtil.EdgePoint(_rng, 6f);
                if (HumanUtil.NearestPlayerDistance(s.Position) < OfflineRange + 10f) s.Position = HumanUtil.EdgePoint(_rng, 6f);
                PickGoal(s);
                _squads.Add(s);
            }
        }

        private string RollFaction()
        {
            if (!s_Mix.TryGetValue(_zone.Id, out (string faction, int weight)[] mix)) mix = s_Mix["kestrel"];
            int total = 0;
            foreach (var m in mix) total += m.weight;
            int r = _rng.Next(total);
            foreach (var m in mix) { r -= m.weight; if (r < 0) return m.faction; }
            return Factions.Scavs;
        }

        private readonly List<Vector3> _scratch = new List<Vector3>();

        private void PickGoal(ALifeSquad s)
        {
            bool night = HumanUtil.IsNight;
            _scratch.Clear();
            string label = "wander";
            if (night)
            {
                HumanUtil.MarkersMatching(WorldMarkers.HumanCamp, _scratch);
                label = "camp";
            }
            else
            {
                double r = _rng.NextDouble();
                bool hunter = s.Faction == Factions.Institute || s.Faction == Factions.Freehold || s.Faction == Factions.Choir;
                if (hunter && r < 0.55 && Anomaly.All.Count > 0)
                {
                    for (int i = 0; i < Anomaly.All.Count; i++) _scratch.Add(Anomaly.All[i].transform.position);
                    label = "anomalies";
                }
                else if (s.Faction == Factions.Vanguard && r < 0.5)
                {
                    HumanUtil.MarkersMatching(WorldMarkers.Outpost, _scratch);
                    label = "outpost";
                }
                else if (r < 0.8)
                {
                    foreach (var kv in HumanUtil.Landmarks()) _scratch.Add(kv.Value);
                    label = "landmark";
                }
            }
            Vector3 goal = _scratch.Count > 0 ? _scratch[_rng.Next(_scratch.Count)] : HumanUtil.RandomMapPoint(_rng, 12f);
            if (label == "anomalies") goal += (goal - s.Position).normalized * -6f;   // stop at the edge of the field
            s.Goal = HumanUtil.ClampToMap(goal, 6f);
            s.GoalLabel = label;
            s.DwellUntil = 0f;
        }
        #endregion

        #region Ticking
        private void Update()
        {
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = 1f;

            for (int i = _squads.Count - 1; i >= 0; i--)
            {
                ALifeSquad s = _squads[i];
                if (s.Online) TickOnline(s);
                if (s.Members <= 0 && !s.Online) { _squads.RemoveAt(i); continue; }
                float d = HumanUtil.NearestPlayerDistance(s.Online ? LeaderPos(s) : s.Position);
                if (!s.Online && d < OnlineRange) GoOnline(s);
                else if (s.Online && d > OfflineRange) GoOffline(s);
            }
            SimulateOffline(1f, false);

            _spawnCheck -= 1f;
            if (_spawnCheck <= 0f)
            {
                _spawnCheck = 45f;
                TopUp(false);
            }
        }

        private static Vector3 LeaderPos(ALifeSquad s)
        {
            for (int i = 0; i < s.Live.Count; i++) if (s.Live[i] != null && !s.Live[i].IsDead) return s.Live[i].transform.position;
            return s.Position;
        }

        private void TickOnline(ALifeSquad s)
        {
            s.Live.RemoveAll(n => n == null || n.IsDead);
            s.Members = s.Live.Count;
            if (s.Members == 0) { s.Online = false; return; }
            s.Position = LeaderPos(s);
            bool busy = false;
            foreach (HumanNpc n in s.Live) if (n.State == HumanNpc.Mode.Combat || n.State == HumanNpc.Mode.Flee || n.State == HumanNpc.Mode.Investigate) busy = true;
            if (busy) return;
            if (HumanUtil.FlatDistance(s.Position, s.Goal) < 5f)
            {
                if (s.DwellUntil <= 0f) s.DwellUntil = Time.time + Random.Range(40f, 110f);
                else if (Time.time > s.DwellUntil) PickGoal(s);
                return;
            }
            // Walk in a loose file behind the leader.
            for (int i = 0; i < s.Live.Count; i++)
            {
                Vector3 offset = i == 0 ? Vector3.zero : Quaternion.Euler(0f, 140f + i * 40f, 0f) * Vector3.forward * (1.6f * i);
                s.Live[i].TravelTo(s.Goal + offset);
            }
        }

        private void GoOnline(ALifeSquad s)
        {
            s.Online = true;
            s.Live.Clear();
            var rng = new System.Random(_rng.Next());
            for (int i = 0; i < s.Members; i++)
            {
                Vector3 p = s.Position + new Vector3((float)rng.NextDouble() * 4f - 2f, 0f, (float)rng.NextDouble() * 4f - 2f);
                if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 8f, NavMesh.AllAreas)) continue;
                HumanNpc n = HumanNpc.Spawn(hit.position, s.Faction, s.LoadoutId, rng, s.Id);
                n.SquadTag = s;
                n.TravelTo(s.Goal);
                s.Live.Add(n);
            }
            if (s.Live.Count == 0) s.Online = false;
        }

        private void GoOffline(ALifeSquad s)
        {
            Absorb(s);
            foreach (HumanNpc n in s.Live) if (n != null && !n.IsDead) Destroy(n.gameObject);
            s.Live.Clear();
            s.Online = false;
        }

        // Folds the live members back into the squad record.
        private static void Absorb(ALifeSquad s)
        {
            int alive = 0;
            float health = 0f;
            foreach (HumanNpc n in s.Live)
                if (n != null && !n.IsDead && !n.Actor.Restrained && n.Actor.Kind != ActorKind.Companion) { alive++; health += n.Health01; }
            s.Members = alive;
            s.Strength = alive > 0 ? Mathf.Clamp(health / alive, 0.3f, 1f) : 0f;
            s.Position = LeaderPos(s);
        }

        // The cheap simulation of everything out of sight: walking, meeting, fighting, emissions.
        private void SimulateOffline(float dt, bool fastForward)
        {
            for (int i = 0; i < _squads.Count; i++)
            {
                ALifeSquad s = _squads[i];
                if (s.Online || s.Members <= 0) continue;
                Vector3 to = s.Goal - s.Position;
                to.y = 0f;
                if (to.magnitude < 4f)
                {
                    if (fastForward || (s.DwellUntil > 0f && Time.time > s.DwellUntil)) PickGoal(s);
                    else if (s.DwellUntil <= 0f) s.DwellUntil = Time.time + Random.Range(40f, 110f);
                }
                else s.Position += to.normalized * Mathf.Min(to.magnitude, 1.3f * dt * (fastForward ? 30f : 1f));

                if (EmissionDirector.Instance != null && EmissionDirector.Instance.Current == EmissionDirector.Phase.Blast && s.GoalLabel != "camp"
                    && _rng.NextDouble() < 0.02 * dt)
                {
                    LeaveBodies(s, s.Members, fastForward);
                    s.Members = 0;
                }
            }

            for (int i = 0; i < _squads.Count; i++)
            {
                ALifeSquad a = _squads[i];
                if (a.Online || a.Members <= 0) continue;
                for (int j = i + 1; j < _squads.Count; j++)
                {
                    ALifeSquad b = _squads[j];
                    if (b.Online || b.Members <= 0) continue;
                    if (HumanUtil.FlatSqr(a.Position, b.Position) > 22f * 22f) continue;
                    if (!Factions.AreHostile(a.Faction, b.Faction, null, null)) continue;
                    Skirmish(a, b, fastForward);
                }
            }
        }

        private void Skirmish(ALifeSquad a, ALifeSquad b, bool fastForward)
        {
            float pa = a.Power * (0.6f + (float)_rng.NextDouble() * 0.8f);
            float pb = b.Power * (0.6f + (float)_rng.NextDouble() * 0.8f);
            ALifeSquad win = pa >= pb ? a : b, lose = win == a ? b : a;
            float ratio = Mathf.Clamp01(Mathf.Min(pa, pb) / Mathf.Max(0.01f, Mathf.Max(pa, pb)));
            int winLoss = Mathf.Clamp(Mathf.RoundToInt(win.Members * ratio * 0.6f), 0, win.Members - 1);
            int fled = lose.Members > 1 && _rng.NextDouble() < 0.3 ? 1 : 0;
            int loseLoss = lose.Members - fled;
            Vector3 where = Vector3.Lerp(a.Position, b.Position, 0.5f);
            LeaveBodies(lose, loseLoss, fastForward, where);
            LeaveBodies(win, winLoss, fastForward, where);
            lose.Members = fled;
            win.Members -= winLoss;
            win.Strength = Mathf.Max(0.35f, win.Strength - 0.2f);
            if (fled > 0) lose.Position = HumanUtil.ClampToMap(lose.Position + (lose.Position - win.Position).normalized * 25f, 6f);
            win.DwellUntil = Time.time + 60f;   // looting the dead
            if (!fastForward) StartCoroutine(DistantGunfire(where, 3 + _rng.Next(5)));
        }

        // Real bodies with real loot, so a player arriving later finds what's left of the fight.
        private void LeaveBodies(ALifeSquad s, int count, bool fastForward, Vector3? at = null)
        {
            if (count <= 0) return;
            Vector3 c = at ?? s.Position;
            var rng = new System.Random(_rng.Next());
            for (int i = 0; i < count; i++)
            {
                Vector3 p = c + new Vector3((float)rng.NextDouble() * 8f - 4f, 0f, (float)rng.NextDouble() * 8f - 4f);
                if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 6f, NavMesh.AllAreas)) continue;
                HumanNpc n = HumanNpc.Spawn(hit.position, s.Faction, s.LoadoutId, rng, s.Id);
                var kill = new DamageInfo(9999f, DamageKind.Bullet, n.Actor.Center, Random.onUnitSphere, null) { HpScale = true }.WithZone(BodyZone.Thorax);
                n.TakeDamage(kill);
            }
        }

        private IEnumerator DistantGunfire(Vector3 at, int bursts)
        {
            NoiseSystem.Emit(at, 60f);
            GameEvents.RaiseWorldEvent(at, 160f, "gunfire");
            for (int i = 0; i < bursts; i++)
            {
                int shots = 1 + Random.Range(0, 4);
                for (int k = 0; k < shots; k++)
                {
                    Sfx.Play(Random.value < 0.6f ? SfxIds.GunRifle : SfxIds.GunPistol, at + Random.insideUnitSphere * 6f, 0.9f, Random.Range(0.9f, 1.05f), 220f);
                    yield return new WaitForSeconds(Random.Range(0.08f, 0.15f));
                }
                yield return new WaitForSeconds(Random.Range(0.4f, 1.6f));
            }
        }
        #endregion

        #region Static people
        // The cordon's wardens stand their towers and the gate (hostile only to runners in the restricted belt).
        private void SpawnWardens()
        {
            if (_zone == null || !_zone.HasCordon) return;
            var rng = new System.Random(_rng.Next());
            IReadOnlyList<Vector3> towers = WorldMarkers.Get(WorldMarkers.CordonTower);
            for (int i = 0; i < towers.Count; i++)
            {
                HumanNpc w = HumanNpc.Spawn(towers[i], Factions.Wardens, HumanLoadouts.Warden, rng, "wardens");
                w.SetGuard(w.transform.position);
            }
            IReadOnlyList<Vector3> gates = WorldMarkers.Get(WorldMarkers.CordonGate);
            for (int i = 0; i < gates.Count; i++)
                for (int k = 0; k < 2; k++)
                {
                    HumanNpc w = HumanNpc.Spawn(gates[i] + new Vector3(k * 2.5f - 1.25f, 0f, 1.5f), Factions.Wardens, HumanLoadouts.Warden, rng, "wardens");
                    w.SetGuard(w.transform.position);
                }
        }
        #endregion
    }

    /// <summary>Starts A-Life, the outposts and the threat director for every raid; saves the squads when it ends.</summary>
    public sealed class ZonePeopleModule : IGameModule
    {
        public int Order => 50;
        private static int s_Seed;

        public void Init()
        {
            WorldHooks.RaidMapBuilt += (world, map, zone, rng) =>
            {
                s_Seed = rng != null ? rng.Next() : GameSession.RaidSeed;
                OutpostSystem.BuildSites(world, zone ?? ZoneCatalog.Current, new System.Random(s_Seed ^ 0x0B57));
            };
            WorldHooks.RaidStarted += (world, map, zone) =>
            {
                zone = zone ?? ZoneCatalog.Current;
                // The server owns the Zone's people and beasts; clients see them through the netcode.
                if (NetAuthority.IsServer)
                {
                    ALifeDirector.Begin(zone, s_Seed);
                    OutpostSystem.Begin(zone, s_Seed);
                    ThreatDirector.Begin(zone, s_Seed);
                    CompanionService.OnRaidStarted();
                }
                PdaStashes.SpawnForZone(zone);
            };
            GameEvents.RaidEnded += (o, exit) =>
            {
                if (ALifeDirector.Instance != null) ALifeDirector.Instance.Save();
                CompanionService.OnRaidEnded(o);
                ProfileStore.Save();
            };
            PdaStashes.RegisterItems();
            CompanionService.RegisterHubScreen();
        }
    }
}
