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
        private readonly List<ExtractionBeacon> _beacons = new List<ExtractionBeacon>();
        private Camera _cam;
        private float _dawnTotalHours = -1f;   // the clock's TotalHours at which dawn ends the raid
        private int _killsAtStart;
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
            BindPlayer(player != null ? player : (GameManager.Instance != null ? GameManager.Instance.Player : null));
            _killsAtStart = GameManager.Instance != null ? GameManager.Instance.ZombieKills : 0;

            // Dawn is fixed on the clock when the raid starts; the countdown is read off the clock after
            // that, so sleeping (which fast-forwards it) brings dawn closer too.
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod != null)
            {
                float until = Mathf.Repeat(CloneZConfig.RaidDawnHour - tod.Hour, 24f);
                if (until < 0.01f) until = 24f;
                _dawnTotalHours = tod.TotalHours + until;
                _arrivedClock = ClockLabel(tod);
                RaidSecondsRemaining = until * tod.DayLengthSeconds / 24f;
            }
            else
            {
                float until = Mathf.Repeat(CloneZConfig.RaidDawnHour - CloneZConfig.RaidStartHour, 24f);
                RaidSecondsRemaining = until * CloneZConfig.RaidDayLengthSeconds / 24f;
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

            if (_player == null && GameManager.Instance != null) BindPlayer(GameManager.Instance.Player);
            if (_player != null && _player.IsDead) { EndRaid(RaidState.Dead, null); return; }
            if (RaidSecondsRemaining <= 0f) { EndRaid(RaidState.MissingInAction, null); return; }
            TickExtraction(dt);
        }

        private void TickDawnClock(float dt)
        {
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod != null && _dawnTotalHours >= 0f)
                RaidSecondsRemaining = Mathf.Max(0f, (_dawnTotalHours - tod.TotalHours) * tod.DayLengthSeconds / 24f);
            else
                RaidSecondsRemaining = Mathf.Max(0f, RaidSecondsRemaining - dt);
        }

        // Standing inside an open exit fills the extraction; stepping out (or into another) starts over.
        private void TickExtraction(float dt)
        {
            RaidExit open = null, closed = null;
            if (_player != null)
            {
                Vector3 p = _player.transform.position;
                float best = float.MaxValue;
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
                    else if (closed == null) closed = e;
                }
            }

            ClosedExitHere = open == null ? closed : null;
            if (open == null)
            {
                ActiveExit = null;
                ExtractProgress01 = 0f;
                State = RaidState.InRaid;
                return;
            }

            if (open != ActiveExit)
            {
                ActiveExit = open;
                ExtractProgress01 = 0f;
            }
            State = RaidState.Extracting;
            ExtractProgress01 = Mathf.Clamp01(ExtractProgress01 + dt / Mathf.Max(0.1f, open.ExtractSeconds));
            if (ExtractProgress01 >= 1f) EndRaid(RaidState.Extracted, open);
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

            // The kit goes home with the survivor, or stays in town for good.
            RaidKit kit;
            if (outcome == RaidState.Extracted) kit = RaidLoadout.SaveExtracted(_player);
            else
            {
                kit = RaidLoadout.Capture(_player);
                RaidLoadout.Clear();
            }

            RaidSummary summary = BuildSummary(outcome, exit, kit);
            RaidEnded?.Invoke(outcome);

            if (outcome == RaidState.Dead) StartCoroutine(ShowSummaryAfter(summary, DeathSummaryDelay));
            else RaidSummaryUI.Show(summary);
        }

        // The cause of death is only recorded once the death event has run its course, so read it late.
        private IEnumerator ShowSummaryAfter(RaidSummary summary, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            GameManager gm = GameManager.Instance;
            if (gm != null && !string.IsNullOrEmpty(gm.CauseOfDeath)) summary.Cause = gm.CauseOfDeath;
            RaidSummaryUI.Show(summary);
        }

        private RaidSummary BuildSummary(RaidState outcome, RaidExit exit, RaidKit kit)
        {
            GameManager gm = GameManager.Instance;
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
    }
}
