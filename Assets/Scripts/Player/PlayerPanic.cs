using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The survivor's fear response — Project Zomboid's Panic moodle. Being near or
    /// in sight of the dead drives panic up (each newly-spotted zombie spikes it, a
    /// looming horde sustains it) and it ebbs once the danger passes. High panic makes
    /// the hands shake — firearms lose accuracy — and, past a point, robs the nerve
    /// needed to sleep. Beta blockers steady the nerves for a while.
    ///
    /// Panic lives on <see cref="PlayerStats.Panic"/> (0..100); this component owns it,
    /// mirroring how <see cref="PlayerWounds"/> owns Pain. Values are grounded in the
    /// reference game's BodyDamage panic model (panicIncreaseValue 7, discrete updates,
    /// beta-blocker relief) and MoodleStat.PANIC thresholds (6 / 30 / 65 / 80).
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerPanic : MonoBehaviour
    {
        private PlayerStats _stats;
        private PlayerWounds _wounds;
        private PlayerSleep _sleep;

        private readonly Collider[] _zombieBuf = new Collider[48];
        private readonly HashSet<ZombieAI> _seen = new HashSet<ZombieAI>();

        private int _lastVisible;
        private float _tickTimer;
        private float _betaTimer;   // seconds of beta-blocker cover remaining

        public bool BetaBlockersActive => _betaTimer > 0f;
        /// <summary>Beta-blocker cover remaining, in seconds (captured/restored by the save system).</summary>
        public float BetaSecondsRemaining => _betaTimer;

        private PlayerWounds Wounds => _wounds != null ? _wounds : (_wounds = GetComponent<PlayerWounds>());
        private PlayerSleep Sleep => _sleep != null ? _sleep : (_sleep = GetComponent<PlayerSleep>());

        /// <summary>Panic moodle level 0..4 (PZ MoodleStat.PANIC thresholds 6 / 30 / 65 / 80).</summary>
        public int Level
        {
            get
            {
                float p = _stats != null ? _stats.Panic : 0f;
                if (p >= CloneZConfig.PanicLevel4) return 4;
                if (p >= CloneZConfig.PanicLevel3) return 3;
                if (p >= CloneZConfig.PanicLevel2) return 2;
                if (p >= CloneZConfig.PanicLevel1) return 1;
                return 0;
            }
        }

        /// <summary>A short word for the current fear level (for the HUD / health panel).</summary>
        public string Descriptor
        {
            get
            {
                switch (Level)
                {
                    case 4: return "Terrified";
                    case 3: return "Panicked";
                    case 2: return "Anxious";
                    case 1: return "Uneasy";
                    default: return "Calm";
                }
            }
        }

        /// <summary>Too rattled to lie down and sleep (PZ blocks sleep while heavily panicked).</summary>
        public bool TooPanickedToSleep => Level >= 3;

        private void Awake() => _stats = GetComponent<PlayerStats>();

        /// <summary>Swallows a dose of beta blockers: steadies the nerves and bleeds off panic for a while.
        /// Returns true if it did anything, so the pill is only spent when it actually helps.</summary>
        public bool TakeBetaBlockers()
        {
            if (_stats == null || _stats.IsDead) return false;
            // Calm and already dosed? Don't waste it.
            if (_stats.Panic < CloneZConfig.PanicLevel1 && _betaTimer > 0f) return false;
            _betaTimer = CloneZConfig.BetaBlockerDurationSec;
            return true;
        }

        /// <summary>Restores remaining beta-blocker cover from a saved game.</summary>
        public void RestoreBeta(float seconds) => _betaTimer = Mathf.Max(0f, seconds);

        private void Update()
        {
            if (_stats == null || _stats.IsDead) return;
            float dt = Time.deltaTime;
            if (_betaTimer > 0f) _betaTimer -= dt;

            _tickTimer += dt;
            if (_tickTimer < CloneZConfig.PanicTickInterval) return;
            float step = _tickTimer;
            _tickTimer = 0f;

            // Asleep the world is frozen and fear drains fast (PZ ReducePanic x2 while asleep).
            if (Sleep != null && Sleep.IsSleeping)
            {
                _stats.Panic = Mathf.Max(0f, _stats.Panic - CloneZConfig.PanicSafeDecayPerSec * 2f * step);
                _lastVisible = 0;
                return;
            }

            int visible = AssessThreat(out float threat);

            // Fear bites harder in the dark; eases toward normal by daylight.
            float darkMult = TimeOfDay.Instance != null
                ? Mathf.Lerp(CloneZConfig.PanicNightMultiplier, 1f, TimeOfDay.Instance.LightLevel)
                : 1f;

            float panic = _stats.Panic;

            // 1) A brand-new zombie coming into view spikes panic (PZ IncreasePanic).
            int newlySeen = Mathf.Max(0, visible - _lastVisible);
            if (newlySeen > 0)
            {
                float spike = newlySeen * CloneZConfig.PanicPerNewZombie * darkMult;
                if (_betaTimer > 0f) spike *= CloneZConfig.BetaBlockerSpikeDampen;
                panic += spike;
            }

            // 2) A looming horde sustains a panic "floor" scaled by how close and aggressive it is;
            //    bleeding keeps you at least uneasy. Beta blockers suppress the response entirely.
            float floor = Mathf.Min(CloneZConfig.PanicMax, threat * CloneZConfig.PanicThreatWeight * darkMult);
            if (Wounds != null && Wounds.IsBleeding) floor = Mathf.Max(floor, CloneZConfig.PanicBleedingFloor);

            if (_betaTimer <= 0f && panic < floor)
            {
                panic = Mathf.MoveTowards(panic, floor, CloneZConfig.PanicSustainRisePerSec * step);
            }
            else
            {
                // 3) Otherwise fear ebbs — fast with nothing in sight, slower while the dead linger,
                //    and faster still (toward zero) while beta blockers hold the nerves steady.
                float target = _betaTimer > 0f ? 0f : floor;
                float decay = visible == 0 ? CloneZConfig.PanicSafeDecayPerSec : CloneZConfig.PanicDecayPerSec;
                if (_betaTimer > 0f) decay += CloneZConfig.BetaBlockerCalmPerSec;
                panic = Mathf.MoveTowards(panic, target, decay * step);
            }

            _stats.Panic = Mathf.Clamp(panic, 0f, CloneZConfig.PanicMax);
            _lastVisible = visible;
        }

        /// <summary>Counts zombies the survivor can actually see nearby and sums a proximity/aggression threat.</summary>
        private int AssessThreat(out float threat)
        {
            threat = 0f;
            int visible = 0;
            Vector3 eye = transform.position + Vector3.up * 1f;

            int n = Physics.OverlapSphereNonAlloc(
                transform.position, CloneZConfig.PanicSightRadius, _zombieBuf,
                1 << CloneZConfig.IgnoreRaycastLayer, QueryTriggerInteraction.Ignore);

            _seen.Clear();
            for (int i = 0; i < n; i++)
            {
                Collider col = _zombieBuf[i];
                if (col == null) continue;
                ZombieAI ai = col.GetComponentInParent<ZombieAI>();
                if (ai == null || ai.IsDeadState || !_seen.Add(ai)) continue;

                Vector3 zp = ai.transform.position;
                float dist = Vector3.Distance(transform.position, zp);
                if (dist < 0.01f || dist > CloneZConfig.PanicSightRadius) continue;

                // A wall between us hides the horror (PZ counts *visible* zombies). Start the ray
                // clear of the player's own body; zombies are on Ignore Raycast so they never block.
                Vector3 dir = (zp - transform.position) / dist;
                Vector3 zeye = zp + Vector3.up * 1f;
                if (Physics.Raycast(eye + dir * 0.6f, (zeye - (eye + dir * 0.6f)).normalized,
                        Vector3.Distance(eye + dir * 0.6f, zeye), 1 << 0, QueryTriggerInteraction.Ignore))
                    continue;

                visible++;
                float prox = 1f - Mathf.Clamp01(dist / CloneZConfig.PanicSightRadius);
                float w = prox;
                if (ai.IsHunting) w *= CloneZConfig.PanicHuntingWeight;
                threat += w;
            }
            return visible;
        }
    }
}
