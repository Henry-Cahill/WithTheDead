using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    public enum WoundType { Scratch, Laceration, Bite }

    /// <summary>
    /// A single injury tracked by <see cref="PlayerWounds"/>. Wounds bleed until
    /// bandaged, hurt (adding pain) until they close, and can fester into a wound
    /// infection (sepsis) if left open or dressed with a dirty rag.
    /// </summary>
    public class Wound
    {
        public WoundType Type;
        public bool Bleeding = true;   // draining health until a dressing is applied
        public bool Bandaged;          // a bandage stops the bleeding and lets it knit
        public bool CleanBandage;      // a sterile bandage vs an improvised rag
        public bool Disinfected;       // disinfectant halts the march toward sepsis
        public bool Infected;          // sepsis has set in — needs antibiotics
        public bool Prone;             // rolled at creation: will this wound fester if neglected?
        public float Sepsis;           // 0..1 progress toward (or, under antibiotics, away from) infection
        public float Healing;          // 0..1 progress toward closing the wound
        public float HealSeconds = CloneZConfig.LacerationHealSeconds;

        public float BleedRate
        {
            get
            {
                switch (Type)
                {
                    case WoundType.Scratch: return CloneZConfig.ScratchBleedPerSec;
                    case WoundType.Bite: return CloneZConfig.BiteBleedPerSec;
                    default: return CloneZConfig.LacerationBleedPerSec;
                }
            }
        }

        public float BasePain
        {
            get
            {
                switch (Type)
                {
                    case WoundType.Scratch: return CloneZConfig.ScratchPain;
                    case WoundType.Bite: return CloneZConfig.BitePain;
                    default: return CloneZConfig.LacerationPain;
                }
            }
        }

        public string Name => Type == WoundType.Bite ? "Bite"
            : Type == WoundType.Laceration ? "Laceration" : "Scratch";
    }

    /// <summary>
    /// The player's body-damage model, inspired by Project Zomboid's BodyDamage /
    /// health panel. Zombie strikes create wounds that bleed (draining health),
    /// build pain (slowing movement and worsening aim) and may fester into an
    /// infection. Treat them with bandages (stop bleeding), disinfectant (prevent
    /// sepsis), antibiotics (cure sepsis) and painkillers (blunt the pain).
    /// This is separate from the fatal zombie virus tracked in <see cref="PlayerStats"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerWounds : MonoBehaviour
    {
        private PlayerStats _stats;
        private PlayerSleep _sleep;
        private readonly List<Wound> _wounds = new List<Wound>();

        private float _antibioticTimer;   // seconds of antibiotic cover remaining
        private float _painkillerTimer;   // seconds of pain relief remaining
        private float _painkillerRelief;  // 0..1 fraction of felt pain suppressed while active

        public IReadOnlyList<Wound> Wounds => _wounds;
        public int WoundCount => _wounds.Count;
        public bool AntibioticsActive => _antibioticTimer > 0f;
        public bool PainkillersActive => _painkillerTimer > 0f;

        // Raw timer readouts, captured/restored by the save system.
        public float AntibioticTimer => _antibioticTimer;
        public float PainkillerTimer => _painkillerTimer;
        public float PainkillerRelief => _painkillerRelief;

        public bool IsBleeding
        {
            get
            {
                for (int i = 0; i < _wounds.Count; i++)
                    if (_wounds[i].Bleeding && !_wounds[i].Bandaged) return true;
                return false;
            }
        }

        public bool NeedsBandage => IsBleeding;

        public bool HasInfectedWound
        {
            get
            {
                for (int i = 0; i < _wounds.Count; i++)
                    if (_wounds[i].Infected) return true;
                return false;
            }
        }

        /// <summary>True when a bandage would help — an undressed wound, or a dirty rag to replace.</summary>
        public bool CanBandage
        {
            get
            {
                for (int i = 0; i < _wounds.Count; i++)
                {
                    Wound w = _wounds[i];
                    if (!w.Bandaged) return true;
                    if (w.Bandaged && !w.CleanBandage && !w.Infected) return true;
                }
                return false;
            }
        }

        /// <summary>True when a wound is open/dirty enough to benefit from disinfectant.</summary>
        public bool CanDisinfect
        {
            get
            {
                for (int i = 0; i < _wounds.Count; i++)
                {
                    Wound w = _wounds[i];
                    if (!w.Infected && !w.Disinfected && !(w.Bandaged && w.CleanBandage)) return true;
                }
                return false;
            }
        }

        private void Awake() => _stats = GetComponent<PlayerStats>();

        /// <summary>Opens a fresh wound of the given kind (called when a zombie lands a hit).</summary>
        public void Inflict(WoundType type)
        {
            var w = new Wound { Type = type };
            switch (type)
            {
                case WoundType.Scratch:
                    w.HealSeconds = CloneZConfig.ScratchHealSeconds;
                    w.Prone = Random.value < CloneZConfig.ScratchSepsisChance;
                    break;
                case WoundType.Bite:
                    w.HealSeconds = CloneZConfig.BiteHealSeconds;
                    w.Prone = Random.value < CloneZConfig.BiteSepsisChance;
                    break;
                default:
                    w.HealSeconds = CloneZConfig.LacerationHealSeconds;
                    w.Prone = Random.value < CloneZConfig.LacerationSepsisChance;
                    break;
            }
            _wounds.Add(w);
            // A fresh wound spikes pain immediately.
            _stats.Pain = Mathf.Min(CloneZConfig.PainMax, _stats.Pain + w.BasePain * 0.5f);
        }

        /// <summary>Rebuilds the wound list and medical timers from a saved game.</summary>
        public void LoadState(List<WoundSave> saved, float antibioticTimer, float painkillerTimer, float relief)
        {
            _wounds.Clear();
            if (saved != null)
            {
                for (int i = 0; i < saved.Count; i++)
                {
                    WoundSave w = saved[i];
                    _wounds.Add(new Wound
                    {
                        Type = (WoundType)w.Type,
                        Bleeding = w.Bleeding,
                        Bandaged = w.Bandaged,
                        CleanBandage = w.CleanBandage,
                        Disinfected = w.Disinfected,
                        Infected = w.Infected,
                        Prone = w.Prone,
                        Sepsis = w.Sepsis,
                        Healing = w.Healing,
                        HealSeconds = w.HealSeconds,
                    });
                }
            }
            _antibioticTimer = Mathf.Max(0f, antibioticTimer);
            _painkillerTimer = Mathf.Max(0f, painkillerTimer);
            _painkillerRelief = Mathf.Clamp01(relief);
        }

        private void Update()
        {
            if (_stats == null || _stats.IsDead) return;
            if (_sleep == null) _sleep = GetComponent<PlayerSleep>();
            // Wounds knit far faster while the survivor sleeps.
            float scale = _sleep != null && _sleep.IsSleeping ? CloneZConfig.WoundHealSleepMultiplier : 1f;
            Tick(Time.deltaTime, scale);
        }

        /// <summary>Advances bleeding, sepsis, healing and pain. <paramref name="healScale"/> &gt; 1 while asleep.</summary>
        public void Tick(float dt, float healScale)
        {
            if (_antibioticTimer > 0f) _antibioticTimer -= dt;
            if (_painkillerTimer > 0f)
            {
                _painkillerTimer -= dt;
                if (_painkillerTimer <= 0f) _painkillerRelief = 0f;
            }

            float healthDrain = 0f;
            float painTarget = 0f;

            for (int i = _wounds.Count - 1; i >= 0; i--)
            {
                Wound w = _wounds[i];
                bool clean = w.Disinfected || (w.Bandaged && w.CleanBandage);

                // Bleeding drains health until the wound is dressed.
                if (w.Bleeding && !w.Bandaged) healthDrain += w.BleedRate;

                if (w.Infected)
                {
                    // An established infection sickens the body; antibiotics slowly clear it.
                    healthDrain += CloneZConfig.SepsisHealthDrainPerSec;
                    if (_antibioticTimer > 0f)
                    {
                        w.Sepsis -= CloneZConfig.AntibioticSepsisCurePerSec * dt;
                        if (w.Sepsis <= 0f)
                        {
                            w.Sepsis = 0f;
                            w.Infected = false;
                            w.Disinfected = true;   // the course of antibiotics leaves it clean
                        }
                    }
                }
                else if (w.Prone && !clean)
                {
                    // A neglected wound festers; a dirty rag only slows the rot.
                    float mult = w.Bandaged ? CloneZConfig.SepsisDirtyBandageMult : 1f;
                    w.Sepsis += CloneZConfig.SepsisRisePerSec * mult * dt;
                    if (w.Sepsis >= 1f) { w.Sepsis = 1f; w.Infected = true; }
                }

                // A dressed, uninfected wound knits shut over time (faster while asleep).
                if (w.Bandaged && !w.Infected)
                {
                    w.Healing += dt * healScale / Mathf.Max(1f, w.HealSeconds);
                    if (w.Healing >= 1f) { _wounds.RemoveAt(i); continue; }
                }

                // Pain fades as the wound closes; an infection keeps it raw.
                float p = w.BasePain * (1f - w.Healing);
                if (w.Infected) p += CloneZConfig.SepsisPainBonus;
                painTarget += p;
            }

            if (healthDrain > 0f) _stats.Bleed(healthDrain * dt);

            // Pain spikes quickly but ebbs slowly; painkillers blunt what you feel.
            painTarget = Mathf.Min(CloneZConfig.PainMax, painTarget) * (1f - _painkillerRelief);
            float rate = painTarget > _stats.Pain ? 40f : CloneZConfig.PainDecayPerSec;
            _stats.Pain = Mathf.MoveTowards(_stats.Pain, painTarget, rate * dt);
        }

        #region Treatment (driven by medical items)
        /// <summary>Dresses the most urgent open wound. Returns false if nothing needed dressing.</summary>
        public bool ApplyBandage(bool sterile)
        {
            Wound w = WorstUndressed();
            // A clean bandage can also replace a grubby rag dressing.
            if (w == null && sterile) w = WorstDirtyBandaged();
            if (w == null) return false;
            w.Bandaged = true;
            w.CleanBandage = sterile;
            w.Bleeding = false;
            if (sterile) w.Sepsis = Mathf.Max(0f, w.Sepsis - CloneZConfig.BandageSepsisReduction);
            return true;
        }

        /// <summary>Disinfects wounds so they won't fester. Returns false if none needed it.</summary>
        public bool Disinfect()
        {
            bool did = false;
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                // Skip anything already infected (needs antibiotics) or already kept clean.
                if (w.Infected || w.Disinfected || (w.Bandaged && w.CleanBandage)) continue;
                w.Disinfected = true;
                w.Sepsis = Mathf.Max(0f, w.Sepsis - CloneZConfig.BandageSepsisReduction);
                did = true;
            }
            return did;
        }

        /// <summary>Starts a course of antibiotics that clears wound infections. Returns false if none present.</summary>
        public bool TreatInfection()
        {
            if (!HasInfectedWound) return false;
            _antibioticTimer = CloneZConfig.AntibioticDurationSec;
            return true;
        }

        /// <summary>Takes painkillers, suppressing felt pain for a while. Returns false if not in pain.</summary>
        public bool RelievePain(float fraction)
        {
            if (_stats == null || _stats.Pain < 2f) return false;
            _painkillerRelief = Mathf.Clamp01(Mathf.Max(_painkillerRelief, fraction));
            _painkillerTimer = CloneZConfig.PainkillerDurationSec;
            return true;
        }

        /// <summary>A first-aid kit: dress, disinfect and speed the closing of the worst wound.</summary>
        public bool FirstAid()
        {
            Wound w = WorstWound();
            if (w == null) return false;
            w.Bandaged = true;
            w.CleanBandage = true;
            w.Bleeding = false;
            w.Disinfected = true;
            w.Sepsis = 0f;
            w.HealSeconds = Mathf.Min(w.HealSeconds, CloneZConfig.FirstAidHealSeconds);
            return true;
        }
        #endregion

        // The wound most in need of a dressing: bleeding first, then by severity.
        private Wound WorstUndressed()
        {
            Wound best = null;
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                if (w.Bandaged) continue;
                if (best == null || Severity(w) > Severity(best)) best = w;
            }
            return best;
        }

        // A wound still wearing a dirty rag dressing (a candidate for a clean re-dressing).
        private Wound WorstDirtyBandaged()
        {
            Wound best = null;
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                if (!w.Bandaged || w.CleanBandage || w.Infected) continue;
                if (best == null || Severity(w) > Severity(best)) best = w;
            }
            return best;
        }

        private Wound WorstWound()
        {
            Wound best = null;
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                if (w.Healing >= 1f) continue;
                if (best == null || Severity(w) > Severity(best)) best = w;
            }
            return best;
        }

        // Higher = more urgent: infected beats bleeding beats deeper wound.
        private static float Severity(Wound w)
        {
            float s = (int)w.Type;
            if (w.Bleeding && !w.Bandaged) s += 3f;
            if (w.Infected) s += 6f;
            return s;
        }
    }
}
