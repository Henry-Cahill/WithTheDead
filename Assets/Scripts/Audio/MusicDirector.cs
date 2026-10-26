using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The score: music that follows what the survivor is going through, rendered in code (<see cref="MusicScore"/>,
    /// <see cref="MusicRecords"/>) on background threads and played gaplessly on Music-bus sources.
    /// <para>
    /// What plays where. On the title screen (and through the loading card that follows it) the main theme, a lone
    /// guitar over a drone, told afresh on every pass. In the hub the score is silent: the roadhouse's jukebox plays its
    /// records instead, from the jukebox itself, with a gap between them — and stays quiet while the musician on the
    /// stage is playing. Out in the world the score is four layers on one grid (four bars at 80 bpm a segment), all in
    /// the same harmony, each faded to what the moment calls for:
    /// explore — phrases of drone and fingerpicking that come and go (one to three minutes of music, then one to three
    /// of silence), darker at night; tension — a heartbeat, a ticking string and nervous strings, in within a second of
    /// something hunting the survivor and out slowly after; combat — drums and a galloping low string, in as fast when
    /// shots, blasts or wounds come close, out over ten-odd seconds; dread — a creeping cluster and a choir while an
    /// emission is coming or breaking. Stingers mark the raid starting, death, getting out or not, an emission's
    /// warning and an artifact taken; the layers duck under them, and after death or a raid's end the score falls silent.
    /// </para>
    /// <para>
    /// How. Each layer has two sources taking turns: the next segment is rendered in the background halfway through the
    /// current one and scheduled sample-accurately on the other source at the boundary (its ring-out overlaps the next
    /// segment, which the score is composed to allow). Tension and combat are rendered alongside whenever the score runs
    /// (silent until needed) so they can come in at once; when nothing is playing, a segment of each waits ready. Old
    /// clips are destroyed as soon as they finish (about 3–4 MB a layer-segment; ~60 MB at most with every layer up).
    /// </para>
    /// </summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        private enum Context { Silent, Menu, Hub, World }
        private enum Phrase { Rest, Playing, Ending }

        // One layer: two sources taking turns, the clip waiting for the next boundary, and its gain.
        private sealed class Lane
        {
            public readonly MusicPart Part;
            public readonly float Volume;
            public readonly AudioSource[] Src = new AudioSource[2];
            public readonly AudioClip[] Clip = new AudioClip[2];
            public readonly double[] Ends = new double[2];
            public int Turn, Epoch;
            public AudioClip Ready;
            public long ReadyIndex = -1, RenderingIndex = -1, FailedIndex = -1;
            public bool Rendering, Active, Last, ReadyLast;
            public long PhraseFirst = -1;
            public float Gain, Target;
            public float Up, Down;

            public Lane(MusicPart part, float volume, float up, float down)
            {
                Part = part;
                Volume = volume;
                Up = up;
                Down = down;
            }
        }

        // A grid that lanes are scheduled on: where segment `Base` starts, how long a segment is, the score's seed.
        private sealed class Transport
        {
            public readonly Lane[] Lanes;
            public readonly double Seconds;
            public bool Running;
            public float Watch;
            public double Start;
            public long Base, Scheduled, Next;
            public uint Seed;

            public Transport(double seconds, params Lane[] lanes)
            {
                Seconds = seconds;
                Lanes = lanes;
            }
        }

        private const double StartLead = 0.12, ScheduleAhead = 1.0;
        private const float ScanEvery = 0.25f, MusicianRange = 15f, JukeboxRange = 18f, JukeboxVolume = 0.55f;

        private static MusicDirector s_Instance;
        private static readonly string[] s_Stingers =
        {
            SfxIds.MusRaidStart, SfxIds.MusDeath, SfxIds.MusExtracted, SfxIds.MusRaidFailed, SfxIds.MusEmission, SfxIds.MusDiscovery,
        };

        private readonly Lane _explore = new Lane(MusicPart.Explore, 0.72f, 1.2f, 0.5f);
        private readonly Lane _tension = new Lane(MusicPart.Tension, 0.7f, 1.6f, 0.45f);
        private readonly Lane _combat = new Lane(MusicPart.Combat, 0.7f, 1.8f, 0.45f);
        private readonly Lane _dread = new Lane(MusicPart.Dread, 0.72f, 0.4f, 0.2f);
        private readonly Lane _themeLane = new Lane(MusicPart.Theme, 0.75f, 3f, 0.25f);
        private Transport _score, _theme;

        private Context _ctx = Context.Silent;
        private bool _dead, _over, _paused;
        private float _scanTimer, _quietFor;

        // Exploring: phrases of music and stretches of silence.
        private Phrase _phrase = Phrase.Rest;
        private float _restLeft = 40f, _phraseLeft;
        private double _phraseEndsAt;

        // What the survivor is going through.
        private float _threatRaw, _threat, _threatHold, _heat, _combatLevel, _combatHold, _hurt, _incoming, _dreadLevel;
        private PlayerStats _stats;

        // The night of each segment, fixed when its first part is asked for, so every layer of it agrees.
        private readonly long[] _nightIndex = { -1, -1, -1, -1 };
        private readonly bool[] _nightOf = new bool[4];

        // Stingers duck the layers.
        private float _duck = 1f;
        private double _duckUntil;
        private double _lastDiscovery = -999.0;

        // The jukebox.
        private Transform _jukebox;
        private AudioSource _jukeSrc;
        private AudioClip _jukeClip, _jukeNext;
        private readonly List<HubNpc> _musicians = new List<HubNpc>();
        private readonly List<AudioSource> _scratch = new List<AudioSource>(8);
        private readonly int[] _records = new int[MusicRecords.Count];
        private int _record, _jukeEpoch;
        private bool _jukeRendering, _jukePlaying, _musicianPlaying;
        private float _jukeGap, _jukeGain, _jukeSearch, _musicianCheck, _musicianQuiet, _musicianRefresh, _jukeRetryAt;

        /// <summary>Starts the score (called once by the audio module).</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[MusicDirector]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<MusicDirector>();
        }

        private void Awake()
        {
            _score = new Transport(MusicScore.Segment / (double)MusicScore.Rate, _explore, _tension, _combat, _dread);
            _theme = new Transport(MusicScore.ThemeSegment / (double)MusicScore.Rate, _themeLane);
            _theme.Seed = (uint)Random.Range(1, int.MaxValue);
            _theme.Next = Random.Range(0, 64) * MusicScore.ThemeSections;
            for (int i = 0; i < _records.Length; i++) _records[i] = i;
            Shuffle(_records);
            foreach (string id in s_Stingers) Sfx.Request(id);

            GameEvents.LocalPlayerSpawned += OnPlayerSpawned;
            GameEvents.RaidStarted += OnRaidStarted;
            GameEvents.RaidEnded += OnRaidEnded;
            GameEvents.EmissionStateChanged += OnEmission;
            GameEvents.ItemLooted += OnLooted;
            GameEvents.WorldEvent += OnWorldEvent;
            GameEvents.ActorKilled += OnKilled;
            CombatResolver.ShotResolved += OnShot;
        }

        private void OnDestroy()
        {
            GameEvents.LocalPlayerSpawned -= OnPlayerSpawned;
            GameEvents.RaidStarted -= OnRaidStarted;
            GameEvents.RaidEnded -= OnRaidEnded;
            GameEvents.EmissionStateChanged -= OnEmission;
            GameEvents.ItemLooted -= OnLooted;
            GameEvents.WorldEvent -= OnWorldEvent;
            GameEvents.ActorKilled -= OnKilled;
            CombatResolver.ShotResolved -= OnShot;
            Hook(null);
            StopTransport(_score);
            StopTransport(_theme);
            StopJukebox();
            if (s_Instance == this) s_Instance = null;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            double now = AudioSettings.dspTime;
            _paused = Time.timeScale <= 0f;
            Context ctx = Decide();
            if (ctx != _ctx) Enter(ctx);

            if (_ctx == Context.World) Sense(dt);
            else Calm();
            Explore(dt, now);
            Targets(dt, now);

            RunScore(now, dt);
            RunTheme(now);
            Jukebox(dt);
            Mix(dt);
        }

        #region Where we are
        private Context Decide()
        {
            if (MainMenuController.Showing) return Context.Menu;
            if (LoadingScreen.Showing) return _ctx == Context.Menu ? Context.Menu : Context.Silent;
            bool body = Actor.LocalPlayer != null || PlayerMonster.Local != null;
            if (!body) return Context.Silent;
            if (GameSession.IsHub) return Context.Hub;
            return _dead || _over ? Context.Silent : Context.World;
        }

        private void Enter(Context ctx)
        {
            Context was = _ctx;
            _ctx = ctx;
            if (ctx == Context.World && was != Context.World)
            {
                StopTransport(_score);
                _score.Seed = (uint)Random.Range(1, int.MaxValue);
                _score.Next = 0;
                _phrase = Phrase.Rest;
                _restLeft = Random.Range(25f, 60f);
                Calm();
            }
            if (ctx != Context.World)
            {
                _explore.Active = false;
                _phrase = Phrase.Rest;
            }
            if (ctx != Context.Hub) _jukeSearch = 0f;
        }

        // Forget the danger (a new place, a new life).
        private void Calm()
        {
            _threatRaw = _threat = _threatHold = _heat = _combatLevel = _combatHold = _hurt = _incoming = _dreadLevel = 0f;
        }

        private bool Night()
        {
            TimeOfDay clock = TimeOfDay.Instance;
            return clock != null && clock.IsNight;
        }

        private bool NightFor(long index)
        {
            for (int k = 0; k < _nightIndex.Length; k++)
                if (_nightIndex[k] == index) return _nightOf[k];
            int slot = (int)(index & 3);
            _nightIndex[slot] = index;
            _nightOf[slot] = Night();
            return _nightOf[slot];
        }
        #endregion

        #region Events
        private void OnPlayerSpawned(PlayerStats stats)
        {
            Hook(stats);
            _dead = false;
            _over = false;
        }

        private void Hook(PlayerStats stats)
        {
            if (_stats != null)
            {
                _stats.OnDamaged -= OnDamaged;
                _stats.OnDied -= OnDied;
            }
            _stats = stats;
            if (_stats != null)
            {
                _stats.OnDamaged += OnDamaged;
                _stats.OnDied += OnDied;
            }
        }

        private void OnDamaged()
        {
            if (_ctx != Context.World) return;
            _hurt = 1f;
            _heat = Mathf.Min(1.5f, _heat + (_threatRaw > 0.05f || _incoming > 0f ? 0.4f : 0.15f));
        }

        private void OnDied()
        {
            if (_dead) return;
            bool playing = _ctx == Context.World;
            _dead = true;
            if (playing) Stinger(SfxIds.MusDeath, 0.9f, 0f);
            FadeOutScore();
        }

        private void OnRaidStarted(string zone, RaidKind kind)
        {
            _over = false;
            _dead = false;
            Stinger(SfxIds.MusRaidStart, 0.8f, 6f);
            _phrase = Phrase.Rest;
            _explore.Active = false;
            _restLeft = Random.Range(35f, 75f);
        }

        private void OnRaidEnded(RaidOutcome outcome, string exit)
        {
            if (outcome == RaidOutcome.Extracted) Stinger(SfxIds.MusExtracted, 0.85f, 0f);
            else if (outcome == RaidOutcome.Killed)
            {
                if (!_dead) Stinger(SfxIds.MusDeath, 0.9f, 0f);
                _dead = true;
            }
            else Stinger(SfxIds.MusRaidFailed, 0.85f, 0f);
            _over = true;
            FadeOutScore();
        }

        private void OnEmission(bool active)
        {
            if (active && _ctx == Context.World) Stinger(SfxIds.MusEmission, 0.8f, 5f);
        }

        private void OnLooted(ItemStack stack, string source)
        {
            if (_ctx != Context.World || source != "anomaly" || _combatLevel > 0.3f) return;
            double now = AudioSettings.dspTime;
            if (now - _lastDiscovery < 25.0) return;
            _lastDiscovery = now;
            Stinger(SfxIds.MusDiscovery, 0.7f, 3.5f);
        }

        private void OnWorldEvent(Vector3 pos, float radius, string kind)
        {
            if (_ctx != Context.World || kind == null) return;
            float d = Vector3.Distance(pos, Sfx.ListenerPosition);
            switch (kind)
            {
                case "shot":
                    // The survivor's own shot: a fight if something is after them, otherwise barely anything.
                    _heat = Mathf.Min(1.5f, _heat + (_threatRaw > 0.05f || _threat > 0.2f ? 0.22f : 0.05f));
                    break;
                case "gunfire":
                    if (d < 60f) _heat = Mathf.Min(1.5f, _heat + 0.3f * Near(d, 60f));
                    break;
                case "explosion":
                    if (d < Mathf.Max(60f, radius)) _heat = Mathf.Min(1.5f, _heat + 0.6f * Near(d, Mathf.Max(60f, radius)));
                    break;
            }
        }

        private void OnShot(ShotRequest req, ShotResult res)
        {
            if (_ctx != Context.World) return;
            Actor me = Actor.LocalPlayer;
            if (req.ShooterActor != null && req.ShooterActor == me) return;
            Vector3 ears = Sfx.ListenerPosition;
            bool atMe = (me != null && res.TargetActor == me) || DistanceToSegment(ears, req.Origin, res.End) < 4f;
            if (atMe)
            {
                _incoming = 1f;
                _heat = Mathf.Min(1.5f, _heat + 0.35f);
            }
            else if ((req.Origin - ears).sqrMagnitude < 40f * 40f) _heat = Mathf.Min(1.5f, _heat + 0.12f);
        }

        private void OnKilled(KillInfo info)
        {
            if (_ctx != Context.World || info.Killer == null || info.Killer != Actor.LocalPlayer) return;
            _heat = Mathf.Min(1.5f, _heat + 0.12f);
        }

        private static float Near(float d, float range) => Mathf.Lerp(1f, 0.35f, d / Mathf.Max(1f, range));

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2) : 0f;
            return Vector3.Distance(p, a + ab * t);
        }

        // A stinger over the score; the layers duck under it for `duck` seconds.
        private void Stinger(string id, float volume, float duck)
        {
            if (!Sfx.Ready(id))
            {
                Sfx.Request(id);
                return;
            }
            Sfx.PlayFlat(id, volume);
            if (duck > 0f) _duckUntil = System.Math.Max(_duckUntil, AudioSettings.dspTime + duck);
        }
        #endregion

        #region The danger
        private void Sense(float dt)
        {
            if (_paused) return;
            _scanTimer -= dt;
            if (_scanTimer <= 0f)
            {
                _scanTimer = ScanEvery;
                _threatRaw = Scan();
            }
            _heat = Mathf.Max(0f, _heat - dt / 14f);
            _hurt = Mathf.Max(0f, _hurt - dt / 8f);
            _incoming = Mathf.Max(0f, _incoming - dt / 6f);

            // Hunted: in within a fraction of a second, held a few seconds after it stops, then let go slowly.
            float threat = Mathf.Clamp01(Mathf.Max(_threatRaw, _hurt * 0.7f));
            if (threat >= _threat)
            {
                _threat = Mathf.MoveTowards(_threat, threat, dt * 3f);
                _threatHold = 5f;
            }
            else if ((_threatHold -= dt) <= 0f) _threat = Mathf.MoveTowards(_threat, threat, dt / 12f);

            // A fight: shots, blasts and wounds close by, while something is after the survivor (or shooting at them).
            float fight = Mathf.Clamp01((_heat - 0.15f) / 0.55f) * (_threat > 0.05f || _incoming > 0f || _hurt > 0f ? 1f : 0.5f);
            if (fight >= _combatLevel)
            {
                _combatLevel = Mathf.MoveTowards(_combatLevel, fight, dt * 3f);
                _combatHold = 4f;
            }
            else if ((_combatHold -= dt) <= 0f) _combatLevel = Mathf.MoveTowards(_combatLevel, fight, dt / 10f);

            float dread = 0f;
            EmissionDirector em = EmissionDirector.Instance;
            if (em != null)
            {
                switch (em.Current)
                {
                    case EmissionDirector.Phase.Warning: dread = 0.55f + 0.45f * Mathf.Clamp01(1f - em.PhaseLeft / 100f); break;
                    case EmissionDirector.Phase.Blast: dread = 1f; break;
                    case EmissionDirector.Phase.Fading: dread = 0.6f * Mathf.Clamp01(em.PhaseLeft / 18f); break;
                }
            }
            _dreadLevel = Mathf.MoveTowards(_dreadLevel, dread, dt / (dread > _dreadLevel ? 3f : 8f));
        }

        // How hunted the survivor is right now: the dead chasing them, mutants and animals on the hunt nearby, people
        // fighting them (or a friend at their side).
        private float Scan()
        {
            Actor me = Actor.LocalPlayer;
            Vector3 ears = Sfx.ListenerPosition;
            float sum = 0f;
            List<Actor> all = Actor.All;
            for (int i = 0; i < all.Count; i++)
            {
                Actor a = all[i];
                if (a == null || a == me) continue;
                ActorKind kind = a.Kind;
                if (kind != ActorKind.Zombie && kind != ActorKind.Mutant && kind != ActorKind.Animal && kind != ActorKind.Human) continue;
                float d2 = (a.transform.position - ears).sqrMagnitude;
                if (d2 > 70f * 70f) continue;
                float d = Mathf.Sqrt(d2);
                if (kind == ActorKind.Zombie)
                {
                    if (d < 40f && a.TryGetComponent(out ZombieAI z) && z.IsHunting && Hunts(z.Prey, me, ears))
                        sum += (z.Type == ZombieType.Sprinter ? 0.45f : 0.3f) * Near(d, 40f);
                }
                else if (kind == ActorKind.Human)
                {
                    if (a.TryGetComponent(out HumanNpc h) && h.State == HumanNpc.Mode.Combat && Hunts(h.Target, me, ears))
                        sum += 0.6f * Near(d, 70f);
                }
                else if (d < 50f && a.TryGetComponent(out Creature c) && c.State == Creature.Mode.Hunt)
                    sum += 0.6f * Near(d, 50f);
            }
            return Mathf.Clamp01(sum);
        }

        private static bool Hunts(Actor prey, Actor me, Vector3 ears)
        {
            if (prey == null) return false;
            if (prey == me) return true;
            return prey.IsPlayerControlled && (prey.transform.position - ears).sqrMagnitude < 15f * 15f;
        }
        #endregion

        #region Exploring: phrases and silences
        private void Explore(float dt, double now)
        {
            if (_ctx != Context.World) return;
            bool calm = _threat < 0.05f && _combatLevel < 0.05f && _dreadLevel < 0.05f;
            switch (_phrase)
            {
                case Phrase.Rest:
                    if (_paused) break;
                    if (!calm)
                    {
                        // After a fight, the music comes back sooner: a quiet aftermath.
                        if (_threat > 0.3f || _combatLevel > 0.3f) _restLeft = Mathf.Min(_restLeft, Random.Range(20f, 45f));
                        break;
                    }
                    _restLeft -= dt;
                    if (_restLeft > 0f) break;
                    _phrase = Phrase.Playing;
                    _phraseLeft = Random.Range(60f, 180f);
                    _explore.Active = true;
                    _explore.Last = false;
                    _explore.PhraseFirst = -1;
                    break;
                case Phrase.Playing:
                    if (!_paused) _phraseLeft -= dt;
                    if (_phraseLeft <= 0f) _explore.Last = true;
                    // The closing segment was lost (rendered too late, or the score stopped): end without its cadence.
                    if (!_explore.Active && !_explore.Rendering && _explore.Ready == null) EndPhrase();
                    break;
                case Phrase.Ending:
                    if (now >= _phraseEndsAt) EndPhrase();
                    break;
            }
        }

        private void EndPhrase()
        {
            _phrase = Phrase.Rest;
            _explore.Active = false;
            _explore.Last = false;
            _restLeft = Random.Range(60f, 180f);
        }

        private void FadeOutScore()
        {
            _explore.Active = false;
            _explore.Last = false;
            _phrase = Phrase.Rest;
            _restLeft = Random.Range(60f, 180f);
        }

        private void Targets(float dt, double now)
        {
            bool world = _ctx == Context.World;
            float danger = Mathf.Max(_threat, _combatLevel);
            _tension.Active = _combat.Active = world;
            _tension.Target = world ? Mathf.Clamp01(Mathf.Max(_threat, _combatLevel * 0.85f)) : 0f;
            _combat.Target = world ? _combatLevel : 0f;
            _dread.Active = world && _dreadLevel > 0.01f;
            _dread.Target = world ? _dreadLevel : 0f;
            bool exploring = world && _phrase != Phrase.Rest;
            _explore.Target = exploring ? (1f - 0.6f * danger) * (1f - 0.7f * _dreadLevel) : 0f;
            _themeLane.Active = _ctx == Context.Menu;
            _themeLane.Target = _themeLane.Active ? 1f : 0f;
            float duck = now < _duckUntil ? 0.35f : 1f;
            _duck = Mathf.MoveTowards(_duck, duck, dt * (duck < _duck ? 3f : 0.35f));
        }
        #endregion

        #region The grid
        private void RunScore(double now, float dt)
        {
            Transport t = _score;
            bool world = _ctx == Context.World;
            if (!t.Running)
            {
                if (!world) return;
                // Tension and combat wait ready (so either can come in at once); explore and dread when wanted.
                Want(t, _tension, t.Next);
                Want(t, _combat, t.Next);
                if (_explore.Active) Want(t, _explore, t.Next);
                if (_dread.Active) Want(t, _dread, t.Next);
                bool ready = Has(_tension, t.Next) && Has(_combat, t.Next);
                bool urgent = _tension.Target > 0.01f || _combat.Target > 0.01f;
                if (ready && (urgent || Has(_explore, t.Next) || Has(_dread, t.Next))) StartTransport(t, now);
                return;
            }
            Tick(t, now);
            bool needed = (world && (_phrase != Phrase.Rest || _tension.Target > 0.01f || _combat.Target > 0.01f || _dread.Active))
                || _explore.Gain > 0.002f || _tension.Gain > 0.002f || _combat.Gain > 0.002f || _dread.Gain > 0.002f;
            _quietFor = needed ? 0f : _quietFor + dt;
            if (_quietFor > 1f) StopTransport(t);
            else Watchdog(t, now, dt);
        }

        private void RunTheme(double now)
        {
            Transport t = _theme;
            if (!t.Running)
            {
                if (!_themeLane.Active)
                {
                    if (_themeLane.Ready != null || _themeLane.Rendering) StopLane(_themeLane);
                    return;
                }
                Want(t, _themeLane, t.Next);
                if (Has(_themeLane, t.Next)) StartTransport(t, now);
                return;
            }
            Tick(t, now);
            if (!_themeLane.Active && _themeLane.Gain <= 0.001f)
            {
                StopTransport(t);
                // Next time, a new telling from its intro.
                t.Next = (t.Next / MusicScore.ThemeSections + 1) * MusicScore.ThemeSections;
            }
            else Watchdog(t, now, 0f);
        }

        private void StartTransport(Transport t, double now)
        {
            t.Running = true;
            t.Base = t.Next;
            t.Start = now + StartLead;
            t.Scheduled = t.Base;
            _quietFor = 0f;
            t.Watch = 0f;
            foreach (Lane l in t.Lanes)
                if (Has(l, t.Base)) Schedule(t, l, t.Start);
        }

        private void StopTransport(Transport t)
        {
            if (t == null) return;
            if (t.Running) t.Next = System.Math.Max(t.Next, t.Scheduled + 1);
            t.Running = false;
            foreach (Lane l in t.Lanes) StopLane(l);
        }

        // Every frame while a grid runs: render the next segment halfway through this one, schedule it just before
        // its boundary, drop what came too late, free what has finished.
        private void Tick(Transport t, double now)
        {
            long current = t.Base + (long)System.Math.Floor((now - t.Start) / t.Seconds);
            if (current < t.Base) current = t.Base;
            long next = current + 1;
            double nextAt = t.Start + (next - t.Base) * t.Seconds;
            if (t.Scheduled < next)
            {
                if (now >= nextAt - t.Seconds * 0.5)
                    foreach (Lane l in t.Lanes)
                        if (l.Active) Want(t, l, next);
                if (nextAt - now <= ScheduleAhead)
                {
                    // A boundary already gone by (the game stalled): skip that segment rather than start it late.
                    bool missed = nextAt < now + 0.01;
                    foreach (Lane l in t.Lanes)
                        if (!missed && Has(l, next)) Schedule(t, l, nextAt);
                    t.Scheduled = next;
                }
            }
            foreach (Lane l in t.Lanes)
            {
                if (l.Ready != null && l.ReadyIndex <= t.Scheduled) DropReady(l);
                for (int i = 0; i < 2; i++)
                    if (l.Clip[i] != null && now > l.Ends[i] + 0.25) Release(l, i);
            }
        }

        // A device change or a long stall can leave a running grid silent: start it over.
        private void Watchdog(Transport t, double now, float dt)
        {
            t.Watch += Mathf.Max(dt, 0.02f);
            if (t.Watch < 1f) return;
            t.Watch = 0f;
            if (now < t.Start + 1.5) return;
            bool audible = false, playing = false;
            foreach (Lane l in t.Lanes)
            {
                if (l.Gain > 0.05f) audible = true;
                for (int i = 0; i < 2; i++)
                    if (l.Src[i] != null && l.Src[i].isPlaying) playing = true;
            }
            if (audible && !playing) StopTransport(t);
        }

        private static bool Has(Lane l, long index) => l.Ready != null && l.ReadyIndex == index;

        private void Want(Transport t, Lane l, long index)
        {
            if (Has(l, index) || (l.Rendering && l.RenderingIndex == index) || index == l.FailedIndex) return;
            if (l.Part == MusicPart.Explore && l.PhraseFirst < 0) l.PhraseFirst = index;
            var cue = new MusicCue
            {
                Part = l.Part,
                Seed = t.Seed,
                Index = index,
                Night = l.Part != MusicPart.Theme && NightFor(index),
                PhrasePos = l.Part == MusicPart.Explore ? (int)System.Math.Max(0L, index - l.PhraseFirst) : 0,
                Last = l.Part == MusicPart.Explore && l.Last,
            };
            if (cue.Last) l.Active = false;
            l.Rendering = true;
            l.RenderingIndex = index;
            int epoch = l.Epoch;
            AudioJobs.Run(() => RenderSafely(cue), data => Rendered(t, l, epoch, cue, data), true);
        }

        // A render that fails is logged and comes back empty, so the lane asks again for a later segment.
        private static float[] RenderSafely(MusicCue cue)
        {
            try { return MusicScore.RenderInterleaved(cue); }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CloneZ] Music segment " + cue.Part + " " + cue.Index + " failed: " + e.Message);
                return null;
            }
        }

        private void Rendered(Transport t, Lane l, int epoch, MusicCue cue, float[] data)
        {
            if (this == null || epoch != l.Epoch || cue.Index != l.RenderingIndex) return;
            l.Rendering = false;
            l.RenderingIndex = -1;
            if (data == null)
            {
                // Skip a segment that will not render (it would fail the same way again).
                l.FailedIndex = cue.Index;
                if (!t.Running && cue.Index == t.Next) t.Next++;
                return;
            }
            if (t.Running && cue.Index <= t.Scheduled) return;
            DropReady(l);
            AudioClip clip = AudioClip.Create("Music " + cue.Part + " " + cue.Index, data.Length / 2, 2, MusicScore.Rate, false);
            clip.SetData(data, 0);
            l.Ready = clip;
            l.ReadyIndex = cue.Index;
            l.ReadyLast = cue.Last;
        }

        private void Schedule(Transport t, Lane l, double when)
        {
            AudioClip clip = l.Ready;
            bool last = l.ReadyLast;
            l.Ready = null;
            l.ReadyIndex = -1;
            l.ReadyLast = false;
            if (clip == null) return;
            int i = l.Turn;
            AudioSource src = Source(l, i);
            if (src == null)
            {
                Destroy(clip);
                return;
            }
            Release(l, i);
            src.clip = clip;
            l.Clip[i] = clip;
            l.Ends[i] = when + clip.samples / (double)clip.frequency;
            src.PlayScheduled(when);
            l.Turn = 1 - i;
            if (last)
            {
                _phrase = Phrase.Ending;
                _phraseEndsAt = when + t.Seconds + 1.5;
            }
        }

        private static AudioSource Source(Lane l, int i)
        {
            if (l.Src[i] != null) return l.Src[i];
            AudioSource src = Sfx.CreateSource("Music " + l.Part + (i == 0 ? " A" : " B"), SfxBus.Music);
            if (src != null) Sfx.SetVolume(src, 0f);
            l.Src[i] = src;
            return src;
        }

        private static void Release(Lane l, int i)
        {
            AudioSource src = l.Src[i];
            if (src != null && src.clip != null)
            {
                src.Stop();
                src.clip = null;
            }
            if (l.Clip[i] != null) Destroy(l.Clip[i]);
            l.Clip[i] = null;
        }

        private static void DropReady(Lane l)
        {
            if (l.Ready != null) Destroy(l.Ready);
            l.Ready = null;
            l.ReadyIndex = -1;
            l.ReadyLast = false;
        }

        private static void StopLane(Lane l)
        {
            for (int i = 0; i < 2; i++) Release(l, i);
            DropReady(l);
            l.Epoch++;
            l.Rendering = false;
            l.RenderingIndex = -1;
            l.Gain = 0f;
            l.Turn = 0;
        }
        #endregion

        #region Mixing
        private void Mix(float dt)
        {
            float fast = _dead || _over ? 3f : 1f;
            Fade(_explore, dt, fast, _duck);
            Fade(_tension, dt, fast, _duck);
            Fade(_combat, dt, fast, _duck);
            Fade(_dread, dt, fast, _duck);
            Fade(_themeLane, dt, 1f, 1f);
        }

        private static void Fade(Lane l, float dt, float fast, float duck)
        {
            float rate = l.Target > l.Gain ? l.Up : l.Down * fast;
            l.Gain = Mathf.MoveTowards(l.Gain, l.Target, rate * dt);
            float v = l.Gain * l.Volume * duck;
            for (int i = 0; i < 2; i++)
            {
                AudioSource s = l.Src[i];
                if (s != null && Mathf.Abs(Sfx.GetVolume(s) - v) > 0.0015f) Sfx.SetVolume(s, v);
            }
        }
        #endregion

        #region The jukebox
        private void Jukebox(float dt)
        {
            if (_ctx != Context.Hub)
            {
                if (_jukeSrc == null && _jukeClip == null && _jukeNext == null && !_jukeRendering) return;
                _jukeGain = Mathf.MoveTowards(_jukeGain, 0f, dt / 2.5f);
                if (_jukeSrc != null) Sfx.SetVolume(_jukeSrc, _jukeGain * JukeboxVolume);
                if (_jukeGain <= 0f || _jukeSrc == null) StopJukebox();
                return;
            }
            if (_jukebox == null)
            {
                if (_jukeSrc != null || _jukeClip != null || _jukeNext != null) StopJukebox();
                _jukeSearch -= dt;
                if (_jukeSearch > 0f) return;
                _jukeSearch = 1.5f;
                GameObject box = GameObject.Find("Jukebox");
                if (box == null) return;
                _jukebox = box.transform;
                _jukeSrc = Sfx.CreateSource("Jukebox speaker", SfxBus.Music, _jukebox, true, JukeboxRange);
                if (_jukeSrc != null) Sfx.SetVolume(_jukeSrc, 0f);
                _jukeGap = Random.Range(4f, 7f);
                _musicianCheck = 0f;
                _musicianRefresh = 10f;
                FindMusicians();
            }
            if (_jukeSrc == null)
            {
                // Gone with its scene.
                StopJukebox();
                return;
            }
            if (_jukeNext == null && !_jukeRendering && Time.unscaledTime >= _jukeRetryAt) RenderRecord();

            _musicianCheck -= dt;
            if (_musicianCheck <= 0f)
            {
                _musicianCheck = 0.5f;
                if ((_musicianRefresh -= 0.5f) <= 0f)
                {
                    _musicianRefresh = 10f;
                    FindMusicians();
                }
                _musicianPlaying = MusicianPlaying();
            }
            _musicianQuiet = _musicianPlaying ? 0f : _musicianQuiet + dt;

            float target = 0f;
            if (_jukePlaying)
            {
                bool over = !_jukeSrc.isPlaying;
                if (_musicianPlaying && _jukeGain <= 0.001f) over = true;
                if (over)
                {
                    // The record has run out (or was turned off for the musician's set): a pause before the next.
                    _jukePlaying = false;
                    _jukeSrc.Stop();
                    _jukeSrc.clip = null;
                    if (_jukeClip != null) Destroy(_jukeClip);
                    _jukeClip = null;
                    _jukeGap = _musicianPlaying ? Random.Range(3f, 8f) : Random.Range(8f, 20f);
                }
                else if (!_musicianPlaying) target = 1f;
            }
            else
            {
                if (!_musicianPlaying) _jukeGap -= dt;
                if (_jukeGap <= 0f && _jukeNext != null && _musicianQuiet > 2f)
                {
                    _jukeClip = _jukeNext;
                    _jukeNext = null;
                    _jukeSrc.clip = _jukeClip;
                    _jukeSrc.Play();
                    _jukePlaying = true;
                    _jukeGain = 1f;
                    target = 1f;
                }
            }
            _jukeGain = Mathf.MoveTowards(_jukeGain, target, dt / (target > _jukeGain ? 1.5f : 1.2f));
            float v = _jukeGain * JukeboxVolume;
            if (Mathf.Abs(Sfx.GetVolume(_jukeSrc) - v) > 0.0015f) Sfx.SetVolume(_jukeSrc, v);
        }

        // The next record, pressed in the background (a new take every time it comes round).
        private void RenderRecord()
        {
            int record = _records[_record % _records.Length];
            _record++;
            if (_record % _records.Length == 0)
            {
                Shuffle(_records);
                if (_records[0] == record) (_records[0], _records[_records.Length - 1]) = (_records[_records.Length - 1], _records[0]);
            }
            uint take = (uint)Random.Range(1, int.MaxValue);
            int epoch = _jukeEpoch;
            _jukeRendering = true;
            AudioJobs.Run(() => PressSafely(record, take), data => Pressed(epoch, record, data), _jukeClip == null && !_musicianPlaying);
        }

        private static float[] PressSafely(int record, uint take)
        {
            try { return MusicRecords.Render(record, take); }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CloneZ] Jukebox record " + record + " failed: " + e.Message);
                return null;
            }
        }

        private void Pressed(int epoch, int record, float[] data)
        {
            if (this == null || epoch != _jukeEpoch) return;
            _jukeRendering = false;
            if (data == null) _jukeRetryAt = Time.unscaledTime + 10f;
            if (data == null || _jukeSrc == null) return;
            AudioClip clip = AudioClip.Create("Record " + MusicRecords.Title(record), data.Length, 1, MusicRecords.Rate, false);
            clip.SetData(data, 0);
            if (_jukeNext != null) Destroy(_jukeNext);
            _jukeNext = clip;
        }

        private void StopJukebox()
        {
            if (_jukeSrc != null) Sfx.Stop(_jukeSrc);
            _jukeSrc = null;
            if (_jukeClip != null) Destroy(_jukeClip);
            if (_jukeNext != null) Destroy(_jukeNext);
            _jukeClip = _jukeNext = null;
            _jukebox = null;
            _jukeEpoch++;
            _jukeRendering = _jukePlaying = _musicianPlaying = false;
            _jukeGain = 0f;
            _musicians.Clear();
        }

        private void FindMusicians()
        {
            _musicians.Clear();
            foreach (HubNpc npc in FindObjectsByType<HubNpc>())
                if (npc != null && npc.Kind == HubNpc.Role.Musician) _musicians.Add(npc);
        }

        // A musician within earshot of the jukebox is playing (any sound of theirs going and turned up).
        private bool MusicianPlaying()
        {
            if (_jukebox == null) return false;
            Vector3 at = _jukebox.position;
            for (int i = 0; i < _musicians.Count; i++)
            {
                HubNpc m = _musicians[i];
                if (m == null || !m.isActiveAndEnabled) continue;
                if ((m.transform.position - at).sqrMagnitude > MusicianRange * MusicianRange) continue;
                m.GetComponentsInChildren(false, _scratch);
                for (int k = 0; k < _scratch.Count; k++)
                {
                    AudioSource s = _scratch[k];
                    if (s != null && s.isPlaying && Sfx.GetVolume(s) > 0.05f) return true;
                }
            }
            return false;
        }

        private static void Shuffle(int[] a)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }
        #endregion
    }
}
