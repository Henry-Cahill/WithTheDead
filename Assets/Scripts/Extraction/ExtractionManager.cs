using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Runs a raid: the spawn-protection countdown, the exits (beacons in the world, extraction
    /// progress while standing in one), the dawn deadline and the end-of-raid summary. The HUD reads
    /// everything below; created by <see cref="Begin"/> once the map, the survivor and the HUD exist.
    ///
    /// Hold inside an open exit for its <see cref="RaidExit.ExtractSeconds"/> and the kit is carried out
    /// (<see cref="RaidLoadout.SaveExtracted"/>); die, or still be in town at dawn, and it is lost
    /// (<see cref="RaidLoadout.Clear"/>). Either way a <see cref="RaidSummaryUI"/> reports the raid.
    /// </summary>
    public class ExtractionManager : MonoBehaviour
    {
        public enum RaidState { InRaid, Extracting, Extracted, Dead, MissingInAction }

        public static ExtractionManager Instance { get; private set; }

        /// <summary>True while a raid's spawn protection holds (and once the survivor is out): zombies
        /// can't see, chase or hurt the survivor. False outside a raid, so it is safe to call anywhere.</summary>
        public static bool PlayerProtected =>
            Instance != null && (Instance.SpawnProtected || Instance.State == RaidState.Extracted);

        public RaidState State { get; protected set; } = RaidState.InRaid;

        /// <summary>Seconds of spawn protection left (0 once it has worn off). Zombies ignore and
        /// can't hurt a protected survivor.</summary>
        public float SpawnProtectionRemaining { get; protected set; }

        /// <summary>True while spawn protection holds.</summary>
        public bool SpawnProtected => SpawnProtectionRemaining > 0f;

        /// <summary>The spawn protection the raid began with, in seconds (for a countdown bar).</summary>
        public float SpawnProtectionSeconds { get; private set; }

        /// <summary>Every exit on the map.</summary>
        public IReadOnlyList<RaidExit> Exits => _exits;
        protected readonly List<RaidExit> _exits = new List<RaidExit>();

        /// <summary>The open exit the survivor is standing in (extracting), or null.</summary>
        public RaidExit ActiveExit { get; protected set; }

        /// <summary>A closed exit the survivor is standing in (it can't be used yet), or null.</summary>
        public RaidExit ClosedExitHere { get; private set; }

        /// <summary>0..1 progress of the extraction under way (0 when not in a zone).</summary>
        public float ExtractProgress01 { get; protected set; }

        /// <summary>Real seconds until the raid ends at dawn (the survivor is then missing in action).</summary>
        public float RaidSecondsRemaining { get; protected set; }

        /// <summary>Seconds the raid has run so far (game time: pauses don't count).</summary>
        public float RaidElapsedSeconds { get; private set; }

        /// <summary>The exit the survivor got out through (null unless extracted).</summary>
        public RaidExit ExitUsed { get; private set; }

        /// <summary>True once the raid has ended one way or another.</summary>
        public bool RaidOver => State == RaidState.Extracted || State == RaidState.Dead || State == RaidState.MissingInAction;

        /// <summary>The survivor on this raid.</summary>
        public PlayerStats Player => _player;

        /// <summary>Raised once when the raid ends (extracted, killed, missing in action or abandoned).</summary>
        public event System.Action<RaidState> RaidEnded;

        private const float DeathSummaryDelay = 2.2f;   // real seconds: let the fall play out first

        private PlayerStats _player;
        private PlayerInventory _playerInv;
        private PlayerClothing _playerCloth;
        private readonly List<ExtractionBeacon> _beacons = new List<ExtractionBeacon>();
        private Camera _cam;
        private float _dawnTotalHours = -1f;   // the clock's TotalHours at which dawn ends the raid
        private int _killsAtStart;
        private GameManager _gm;
        private string _arrivedClock = "";

        /// <summary>Starts the raid. Called by the bootstrapper once the map, player and HUD exist.</summary>
        public static ExtractionManager Begin(PlayerStats player, IList<RaidExit> exits, float spawnProtectionSeconds)
        {
            var go = new GameObject("ExtractionManager");
            var m = go.AddComponent<ExtractionManager>();
            if (exits != null)
                for (int i = 0; i < exits.Count; i++)
                    if (exits[i] != null) m._exits.Add(exits[i]);
            m.SpawnProtectionSeconds = Mathf.Max(0f, spawnProtectionSeconds);
            m.SpawnProtectionRemaining = m.SpawnProtectionSeconds;
            m.Setup(player);
            return m;
        }

        protected virtual void Awake() { Instance = this; }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_player != null) _player.OnDied -= HandlePlayerDied;
            for (int i = 0; i < _beacons.Count; i++) _beacons[i].Dispose();
            _beacons.Clear();
        }

        /// <summary>Gives the raid up (the pause menu's "Abandon Raid"). Counts as missing in action, so
        /// the kit is forfeited; no summary is shown (the caller heads back to the title screen).</summary>
        public void Abandon()
        {
            if (RaidOver) return;
            State = RaidState.MissingInAction;
            ActiveExit = null;
            ClosedExitHere = null;
            ExtractProgress01 = 0f;
            SpawnProtectionRemaining = 0f;
            RaidLoadout.Clear();
            RaidEnded?.Invoke(State);
        }

        /// <summary>The exit nearest a point (flat distance), optionally open exits only; null if none.</summary>
        public RaidExit NearestExit(Vector3 from, bool openOnly = true)
        {
            RaidExit best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _exits.Count; i++)
            {
                RaidExit e = _exits[i];
                if (openOnly && !e.Open) continue;
                float d = FlatDistance(from, e.Position);
                if (d < bestDist) { bestDist = d; best = e; }
            }
            return best;
        }

        /// <summary>Horizontal distance between two points (exit zones are judged on the flat).</summary>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void Setup(PlayerStats player)
        {
            BindPlayer(player != null ? player : (_gm != null ? _gm.Player : null));
            _killsAtStart = _gm != null ? _gm.ZombieKills : 0;
            _gm = GameManager.Instance;
            // Dawn is fixed on the clock when the raid starts (the sandbox's raid length in in-game hours); the
            // countdown is read off the clock after that, so sleeping (which fast-forwards it) brings dawn closer too.
            TimeOfDay tod = TimeOfDay.Instance;
            float raidHours = Mathf.Clamp(SandboxSettings.Current.RaidHours, 1f, 23f);
            if (tod != null)
            {
                float until = raidHours;
                _dawnTotalHours = tod.TotalHours + until;
                _arrivedClock = ClockLabel(tod);
                RaidSecondsRemaining = until * tod.DayLengthSeconds / 24f;
            }
            else
            {
                RaidSecondsRemaining = raidHours * CloneZConfig.RaidDayLengthSeconds / 24f;
            }

            var root = new GameObject("ExitBeacons").transform;
            root.SetParent(transform, false);
            for (int i = 0; i < _exits.Count; i++)
            {
                ExtractionBeacon beacon = ExtractionBeacon.Build(_exits[i], root);
                if (beacon != null) _beacons.Add(beacon);
            }
        }

        private void BindPlayer(PlayerStats player)
        {
            if (player == _player) return;
            if (_player != null) _player.OnDied -= HandlePlayerDied;
            _player = player;
            // Cache component look‑ups for this player
            _playerInv = _player != null ? _player.GetComponent<PlayerInventory>() : null;
            _playerCloth = _player != null ? _player.GetComponent<PlayerClothing>() : null;
            if (_player != null) _player.OnDied += HandlePlayerDied;
        }

        private void Update()
        {
            if (!RaidOver) TickRaid(Time.deltaTime);
            TickBeacons();
        }

        private void TickRaid(float dt)
        {
            RaidElapsedSeconds += dt;
            if (SpawnProtectionRemaining > 0f) SpawnProtectionRemaining = Mathf.Max(0f, SpawnProtectionRemaining - dt);
            TickDawnClock(dt);
            TickHelicopters(dt);

            if (_player == null && _gm != null) BindPlayer(_gm.Player);
            if (_player != null && _player.IsDead) { EndRaid(RaidState.Dead, null); return; }
            if (!NoDeadline && RaidSecondsRemaining <= 0f) { EndRaid(RaidState.MissingInAction, null); return; }
            TickExtraction(dt);
        }

        /// <summary>True in an open-zone run (Gray Zone Warfare: no raid timer — stay as long as supplies and nerve last).</summary>
        public static bool NoDeadline => GameSession.IsOpenZone || SandboxSettings.Current.OpenZoneNoTimer || GameSession.IsMonsterRun;

        /// <summary>Why the exit the survivor stands in won't let them out yet (null when it will).</summary>
        public string BlockedReason { get; private set; }

        private void TickDawnClock(float dt)
        {
            if (NoDeadline) { RaidSecondsRemaining = 99999f; return; }
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod != null && _dawnTotalHours >= 0f)
                RaidSecondsRemaining = Mathf.Max(0f, (_dawnTotalHours - tod.TotalHours) * tod.DayLengthSeconds / 24f);
            else
                RaidSecondsRemaining = Mathf.Max(0f, RaidSecondsRemaining - dt);
        }

        /// <summary>Calls the helicopter to a flare exit near <paramref name="flarePos"/> (within 30 m). True if one answers.</summary>
        public bool CallHelicopter(Vector3 flarePos)
        {
            for (int i = 0; i < _exits.Count; i++)
            {
                RaidExit e = _exits[i];
                if (e.Condition != ExitCondition.Flare || e.HeliCalled) continue;
                if (FlatDistance(e.Position, flarePos) > 30f) continue;
                e.HeliCalled = true;
                e.HeliLanded = false;
                e.HeliEta = 55f;
                GameEvents.RaiseToast("The helicopter saw your flare. ETA 55 seconds — hold the landing zone.");
                GameEvents.RaiseWorldEvent(e.Position, 160f, "flare");
                HelicopterCalled?.Invoke(e);
                return true;
            }
            return false;
        }

        /// <summary>Raised when a flare calls the helicopter to an exit (the helicopter visuals listen).</summary>
        public static event System.Action<RaidExit> HelicopterCalled;
        /// <summary>Raised when the helicopter at an exit gives up and leaves.</summary>
        public static event System.Action<RaidExit> HelicopterLeft;

        private void TickHelicopters(float dt)
        {
            for (int i = 0; i < _exits.Count; i++)
            {
                RaidExit e = _exits[i];
                if (!e.HeliCalled) continue;
                if (!e.HeliLanded)
                {
                    e.HeliEta -= dt;
                    if (e.HeliEta <= 0f)
                    {
                        e.HeliLanded = true;
                        e.HeliEta = 40f;   // it waits this long on the ground
                        GameEvents.RaiseToast("The helicopter is down at " + e.Name + ". Get aboard!");
                        NoiseSystem.Emit(e.Position, 70f);
                    }
                }
                else
                {
                    e.HeliEta -= dt;
                    if (e.HeliEta <= 0f)
                    {
                        e.HeliCalled = false;
                        e.HeliLanded = false;
                        GameEvents.RaiseToast("The helicopter couldn't wait any longer. It's gone.");
                        HelicopterLeft?.Invoke(e);
                    }
                }
            }
        }

        // Whether an open exit lets this survivor out right now.
        private bool MeetsCondition(RaidExit e, out string reason)
        {
            reason = null;
            if (GameSession.IsMonsterRun) return true;   // a hunter just melts back into the dark
            switch (e.Condition)
            {
                case ExitCondition.Pay:
                    var inv = _playerInv;
                    if (inv == null || Money.Count(inv.Inventory) < e.Cost) { reason = "The smuggler wants " + Money.Format(e.Cost) + " (carried)"; return false; }
                    return true;
                case ExitCondition.NoBackpack:
                    var clothing = _playerCloth;
                    if (clothing != null && clothing.IsWearing(ClothingSlot.Back)) { reason = "Too tight with a backpack on"; return false; }
                    return true;
                case ExitCondition.Switch:
                    if (!e.Powered) { reason = "The gate has no power"; return false; }
                    return true;
                case ExitCondition.CoopScav:
                    if (GameSession.IsScavRun) return true;
                    if (Actor.Nearest(e.Position, 12f, a => a.Faction == Factions.Scavs && a.Kind != ActorKind.Player && Actor.LocalPlayer != null && !a.IsHostileTo(Actor.LocalPlayer)) != null) return true;
                    reason = "The scavs only let their own through";
                    return false;
                case ExitCondition.Flare:
                    if (!e.HeliLanded) { reason = e.HeliCalled ? "Hold out — the helicopter is coming" : "Fire a flare to call the helicopter"; return false; }
                    return true;
                default:
                    return true;
            }
        }

        // Standing inside an open exit fills the extraction; stepping out (or into another) starts over.
        private void TickExtraction(float dt)
        {
            RaidExit open = null, closed = null;
            if (_player != null)
            {
                Vector3 p = _player.transform.position;
                float best = float.MaxValue; float bestClosedDist = float.MaxValue;
                for (int i = 0; i < _exits.Count; i++)
                {
                    RaidExit e = _exits[i];
                    float dx = p.x - e.Position.x, dz = p.z - e.Position.z;
                    float d2 = dx * dx + dz * dz;
                    float r = Mathf.Max(0.5f, e.Radius);
                    if (d2 > r * r) continue;
                    if (e.Open)
                    {
                        if (d2 < best) { best = d2; open = e; }
                    }
                    else
                    {
                        // Track the nearest closed exit the player is standing in
                        if (closed == null || d2 < bestClosedDist)
                        {
                            bestClosedDist = d2;
                            closed = e;
                        }
                    }
                }
            }

            ClosedExitHere = open == null ? closed : null;
            if (open == null)
            {
                ActiveExit = null;
                ExtractProgress01 = 0f;
                BlockedReason = null;
                State = RaidState.InRaid;
                return;
            }

            if (open != ActiveExit)
            {
                ActiveExit = open;
                ExtractProgress01 = 0f;
            }
            if (!MeetsCondition(open, out string reason))
            {
                BlockedReason = reason;
                ExtractProgress01 = 0f;
                State = RaidState.InRaid;
                return;
            }
            BlockedReason = null;
            State = RaidState.Extracting;
            ExtractProgress01 = Mathf.Clamp01(ExtractProgress01 + dt / Mathf.Max(0.1f, open.ExtractSeconds));
            if (ExtractProgress01 >= 1f)
            {
                if (open.Condition == ExitCondition.Pay)
                {
                    var inv = _playerInv;
                    if (inv == null || Money.Take(inv.Inventory, open.Cost) > 0) { BlockedReason = "Couldn't pay"; ExtractProgress01 = 0f; return; }
                }
                EndRaid(RaidState.Extracted, open);
            }
        }

        private void HandlePlayerDied()
        {
            if (!RaidOver) EndRaid(RaidState.Dead, null);
        }

        private void EndRaid(RaidState outcome, RaidExit exit)
        {
            if (RaidOver) return;
            State = outcome;
            ExitUsed = exit;
            ClosedExitHere = null;
            SpawnProtectionRemaining = 0f;
            if (outcome == RaidState.Extracted) ExtractProgress01 = 1f;
            else
            {
                ActiveExit = null;
                ExtractProgress01 = 0f;
            }

            // The kit goes home with the survivor, or stays in the Zone (all but the secure pouch).
            RaidKit kit;
            if (outcome == RaidState.Extracted) kit = RaidLoadout.SaveExtracted(_player);
            else kit = RaidLoadout.Forfeit(_player);
            RecordOutcome(outcome);

            RaidSummary summary = BuildSummary(outcome, exit, kit);
            RaidEnded?.Invoke(outcome);
            GameEvents.RaiseRaidEnded(ToOutcome(outcome), exit != null ? exit.Name : null);

            if (outcome == RaidState.Dead) StartCoroutine(ShowSummaryAfter(summary, DeathSummaryDelay));
            else RaidSummaryUI.Show(summary);
        }

        // The cause of death is only recorded once the death event has run its course, so read it late.
        private IEnumerator ShowSummaryAfter(RaidSummary summary, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            GameManager gm = _gm;
            if (gm != null && !string.IsNullOrEmpty(gm.CauseOfDeath)) summary.Cause = gm.CauseOfDeath;
            RaidSummaryUI.Show(summary);
        }

        private RaidSummary BuildSummary(RaidState outcome, RaidExit exit, RaidKit kit)
        {
            GameManager gm = _gm;
            var summary = new RaidSummary
            {
                Outcome = outcome,
                ExitName = exit != null ? exit.Name : null,
                DurationSeconds = RaidElapsedSeconds,
                Kills = gm != null ? Mathf.Max(0, gm.ZombieKills - _killsAtStart) : 0,
                ArrivedClock = _arrivedClock,
                EndedClock = TimeOfDay.Instance != null ? ClockLabel(TimeOfDay.Instance) : "",
                Streak = outcome == RaidState.Extracted && kit != null ? kit.Streak : 0,
                Cause = gm != null ? gm.CauseOfDeath : "",
            };
            summary.AddKit(kit);
            return summary;
        }

        private void TickBeacons()
        {
            if (_beacons.Count == 0) return;
            if (_cam == null) _cam = Camera.main;
            float t = Time.time;
            bool extracting = State == RaidState.Extracting;
            for (int i = 0; i < _beacons.Count; i++)
            {
                ExtractionBeacon b = _beacons[i];
                bool here = extracting && b.Exit == ActiveExit;
                b.Tick(_cam, t, here, here ? ExtractProgress01 : 0f);
            }
        }

        private static string ClockLabel(TimeOfDay tod) => tod.Clock12Text + " " + tod.AmPm;

        private static RaidOutcome ToOutcome(RaidState s)
        {
            switch (s)
            {
                case RaidState.Extracted: return RaidOutcome.Extracted;
                case RaidState.Dead: return RaidOutcome.Killed;
                default: return RaidOutcome.MissingInAction;
            }
        }

        // The character's record of the raid: statistics, the scav run's standing with the fence, permadeath.
        private void RecordOutcome(RaidState outcome)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null) return;
            if (outcome == RaidState.Extracted) c.Extracts++;
            else if (outcome == RaidState.Dead) c.Deaths++;
            if (GameSession.IsScavRun)
            {
                TraderState fence = c.Trader(TraderCatalog.Fence);
                fence.Rep = Mathf.Clamp(fence.Rep + (outcome == RaidState.Extracted ? 0.02f : -0.01f), -1f, 1f);
                c.ScavReadyUnix = ProfileStore.NowUnix + (long)(SandboxSettings.Current.ScavCooldownMinutes * 60f
                    * (Hideout.LevelOf(c, "intel") > 0 ? 0.75f : 1f));
            }
            else if (GameSession.IsMonsterRun) { }   // a hunter's death costs the runner nothing
            else if (outcome == RaidState.Dead && (c.Ironman || SandboxSettings.Current.PermadeathCharacters))
            {
                c.Dead = true;
                c.CauseOfDeath = GameManager.Instance != null ? GameManager.Instance.CauseOfDeath : "the Zone";
            }
            ProfileStore.Save();
        }

        /// <summary>The raid summary's "continue": back to the hub (or, without a profile, the title screen).</summary>
        public static void ReturnFromRaid()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null && !c.Dead) GameFlow.GoToHub();
            else GameFlow.ToMainMenu();
        }
    }
}
