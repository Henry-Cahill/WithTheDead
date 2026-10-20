using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Emissions: the Zone's periodic convulsion. A siren and a radio warning, the sky turning red, then a couple of
    /// minutes to get under a solid roof before the blast — which kills anyone caught in the open (people, not the
    /// dead) and leaves the Zone rearranged: the artifacts nobody took are gone and new ones surface. Some are psi
    /// storms instead: a violet sky, whispers, and the damage goes for the mind. The schedule comes from the raid's
    /// seed and the sandbox interval, so every peer hears the same siren at the same moment.
    /// </summary>
    public sealed class EmissionDirector : MonoBehaviour
    {
        public enum Phase { Calm, Warning, Blast, Fading }
        private static readonly Color _psiStormColor = new Color(0.55f, 0.3f, 0.85f);
        private static readonly Color _standardColor = new Color(0.9f, 0.18f, 0.12f);

        /// <summary>Singleton instance of the EmissionDirector.</summary>
        public static EmissionDirector Instance { get; private set; }
                /// <summary>True when an emission is currently in the warning or blast phase.</summary>
        public static bool Active => Instance != null && (Instance.Current == Phase.Warning || Instance.Current == Phase.Blast);

                /// <summary>Current emission phase; private set because only the director changes it.</summary>
        public Phase Current { get; private set; } = Phase.Calm;
                /// <summary>True when this emission is a psi storm instead of a standard blast.</summary>
        public bool PsiStorm { get; private set; }
        /// <summary>Seconds left in the current phase.</summary>
        public float PhaseLeft => Mathf.Max(0f, _phaseEnds - Time.time);

        private const float WarningSeconds = 100f, BlastSeconds = 24f, FadeSeconds = 18f;

        private ZoneDef _zone;
        private System.Random _rng;
        private float _nextAt, _phaseEnds, _tick, _rumbleAt;
        private AudioSource _siren;

                /// <summary>Start the emission system for the given zone and random seed.</summary>
        public static void Begin(ZoneDef zone, int seed)
        {
            if (Instance != null) Destroy(Instance.gameObject);
            float interval = SandboxSettings.Current.EmissionIntervalMinutes;
            if (interval <= 0f || zone == null) return;
            var go = new GameObject("EmissionDirector");
            Instance = go.AddComponent<EmissionDirector>();
            Instance._zone = zone;
            Instance._rng = new System.Random(seed ^ 0xE1155);
            Instance.Schedule(true);
        }

        /// <summary>A networked client's director: it never schedules on its own; the server says when (<see cref="RemoteStart"/>).</summary>
        public static void BeginFollower(ZoneDef zone, int seed)
        {
            if (Instance != null) Destroy(Instance.gameObject);
            if (zone == null) return;
            var go = new GameObject("EmissionDirector (follower)");
            Instance = go.AddComponent<EmissionDirector>();
            Instance._zone = zone;
            Instance._rng = new System.Random(seed ^ 0xE1155);
            Instance._follower = true;
            Instance._nextAt = float.MaxValue;
        }

        /// <summary>Raised on the server when an emission's warning begins (true = a psi storm), for the netcode.</summary>
        public static event System.Action<bool> Started;

        /// <summary>The server announced an emission: run the same warning, blast and fade here.</summary>
        public static void RemoteStart(bool psi)
        {
            if (Instance == null || Instance.Current != Phase.Calm) return;
            Instance.PsiStorm = psi;
            Instance.StartWarning();
        }

        private bool _follower;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ZoneAtmosphere.SkyOverrideStrength = 0f;
            if (_siren != null) Sfx.Stop(_siren);
        }

        private void Schedule(bool first)
        {
            float minutes = SandboxSettings.Current.EmissionIntervalMinutes / Mathf.Max(0.25f, _zone.EmissionMult);
            float jitter = 0.7f + (float)_rng.NextDouble() * 0.6f;
            _nextAt = Time.time + Mathf.Max(first ? 6f * 60f : 3f * 60f, minutes * 60f * jitter);
            PsiStorm = _rng.NextDouble() < SandboxSettings.Current.PsiStormChance;
        }

        /// <summary>Starts the next emission now (debugging, quests).</summary>
        public void TriggerNow() => _nextAt = Time.time;

        private void Update()
        {
            switch (Current)
            {
                case Phase.Calm:
                    if (Time.time >= _nextAt) StartWarning();
                    break;
                case Phase.Warning:
                    ZoneAtmosphere.SkyOverride = PsiStorm ? _psiStormColor : _standardColor;
                    ZoneAtmosphere.SkyOverrideStrength = Mathf.Lerp(0.05f, 0.45f, 1f - PhaseLeft / WarningSeconds);
                    Rumble(8f, 0.35f);
                    if (Time.time >= _phaseEnds) StartBlast();
                    break;
                case Phase.Blast:
                    ZoneAtmosphere.SkyOverrideStrength = 0.7f + 0.3f * Mathf.PerlinNoise(Time.time * 3f, 0.5f);
                    Rumble(2.5f, 0.9f);
                    _tick -= Time.deltaTime;
                    if (_tick <= 0f) { _tick = 1f; Strike(); }
                    if (Time.time >= _phaseEnds) StartFading();
                    break;
                case Phase.Fading:
                    ZoneAtmosphere.SkyOverrideStrength = Mathf.Lerp(0f, 0.6f, PhaseLeft / FadeSeconds);
                    if (Time.time >= _phaseEnds) Finish();
                    break;
            }
        }

        private void StartWarning()
        {
            Current = Phase.Warning;
            _phaseEnds = Time.time + WarningSeconds;
            if (!_follower) Started?.Invoke(PsiStorm);
            _siren = Sfx.Loop(SfxIds.EmissionSiren, transform, 0.55f, 10f);
            if (_siren != null) _siren.spatialBlend = 0f;
            GameEvents.RaiseEmission(true);
            GameEvents.RaiseSubtitle("Radio", PsiStorm
                ? "Psi storm coming in! Get indoors, get your head down, don't listen to anything you hear!"
                : "Emission! Emission! Everyone under a roof, now! You've got two minutes!");
            GameEvents.RaiseToast((PsiStorm ? "PSI STORM" : "EMISSION") + " — find shelter (a building with a roof)");
        }

        private void StartBlast()
        {
            Current = Phase.Blast;
            _phaseEnds = Time.time + BlastSeconds;
            _tick = 0f;
            Sfx.PlayUI(SfxIds.EmissionRumble, 1f);
        }

        private void StartFading()
        {
            Current = Phase.Fading;
            _phaseEnds = Time.time + FadeSeconds;
            if (_siren != null) { Sfx.Stop(_siren); _siren = null; }
        }

        private void Finish()
        {
            Current = Phase.Calm;
            ZoneAtmosphere.SkyOverrideStrength = 0f;
            GameEvents.RaiseEmission(false);
            ArtifactField.Reshuffle();
            if (Actor.LocalPlayer != null && Actor.LocalPlayer.IsAlive)
            {
                ZoneExposure.Add(0.15f);
                GameEvents.RaiseToast("It has passed. The Zone has moved things around.");
            }
            if (_follower) _nextAt = float.MaxValue;
            else Schedule(false);
        }

        private void Rumble(float every, float volume)
        {
            if (Time.time < _rumbleAt) return;
            _rumbleAt = Time.time + every * Random.Range(0.6f, 1.4f);
            Sfx.PlayUI(SfxIds.EmissionRumble, volume, Random.Range(0.8f, 1.1f));
            if (PsiStorm && Current != Phase.Calm) Sfx.PlayUI(SfxIds.PsiWhisper, volume * 0.6f, Random.Range(0.8f, 1.2f));
        }

        /// <summary>True when a position is under a solid roof (the only safe place during the blast).</summary>
        public static bool Sheltered(Vector3 feet)
        {
            Vector3 head = feet + Vector3.up * 1.7f;
            if (Shelter.Count > 0 && Shelter.IsSheltered(feet + Vector3.up)) return true;
            return Physics.Raycast(head, Vector3.up, 30f, 1, QueryTriggerInteraction.Ignore);
        }

        // One second of the blast: everything alive and exposed suffers; people caught outside die.
        private void Strike()
        {
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                Actor a = Actor.All[i];
                if (a == null || !a.IsAlive || a.Damageable == null) continue;
                if (Sheltered(a.transform.position)) continue;
                switch (a.Kind)
                {
                    case ActorKind.Zombie:
                        continue;                           // the dead don't care
                    case ActorKind.Player:
                    case ActorKind.RemotePlayer:
                    {
                        var info = new DamageInfo(PsiStorm ? 34f : 28f, PsiStorm ? DamageKind.Psi : DamageKind.Radiation, a.Center, Vector3.down, gameObject)
                        { HpScale = true, WeaponId = PsiStorm ? "psi_storm" : "emission" }.WithZone(BodyZone.Head);
                        CombatResolver.Apply(a.Damageable, info, null);
                        if (!PsiStorm)
                        {
                            var stats = a.GetComponent<PlayerStats>();
                            if (stats != null) stats.EnvironmentDrain(3f, "was caught in the open by an emission");
                        }
                        break;
                    }
                    default:
                    {
                        var info = new DamageInfo(160f, PsiStorm ? DamageKind.Psi : DamageKind.Radiation, a.Center, Vector3.down, gameObject)
                        { HpScale = true, WeaponId = "emission" }.WithZone(BodyZone.Head);
                        CombatResolver.Apply(a.Damageable, info, null);
                        break;
                    }
                }
            }
        }
    }
}
