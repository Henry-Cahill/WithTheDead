using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Kinds of injury. The first three are the original zombie wounds (saved by index); the rest come
    /// from gunfire, blasts, claws and the Zone.</summary>
    public enum WoundType { Scratch, Laceration, Bite, Gunshot, HeavyBleed, Deep, Shard, Burn }

    /// <summary>
    /// A single injury tracked by <see cref="PlayerWounds"/>. Wounds bleed until
    /// bandaged, hurt (adding pain) until they close, and can fester into a wound
    /// infection (sepsis) if left open or dressed with a dirty rag. A heavy bleed needs a tourniquet or
    /// haemostatic gauze (a bandage won't hold it); a deep wound won't close until it's stitched; a lodged
    /// shard keeps it raw until tweezers pull it out.
    /// </summary>
    public class Wound
    {
        public WoundType Type;
        public BodyZone Zone = BodyZone.Thorax;
        public bool Bleeding = true;   // draining health until a dressing is applied
        public bool Bandaged;          // a bandage stops the bleeding and lets it knit
        public bool CleanBandage;      // a sterile bandage vs an improvised rag
        public bool Disinfected;       // disinfectant halts the march toward sepsis
        public bool Infected;          // sepsis has set in — needs antibiotics
        public bool Prone;             // rolled at creation: will this wound fester if neglected?
        public float Sepsis;           // 0..1 progress toward (or, under antibiotics, away from) infection
        public float Healing;          // 0..1 progress toward closing the wound
        public float HealSeconds = CloneZConfig.LacerationHealSeconds;
        public bool NeedsStitches;     // a deep wound: dressing slows it, only a suture lets it close
        public bool Lodged;            // a glass shard or a fragment still in it
        public bool Tourniquet;        // a heavy limb bleed held by a tourniquet (the limb weakens while it's on)

        /// <summary>Gets the bleed rate per second for this wound type.</summary>
        public float BleedRate
        {
            get
            {
                switch (Type)
                {
                    case WoundType.Scratch: return CloneZConfig.ScratchBleedPerSec;
                    case WoundType.Bite: return CloneZConfig.BiteBleedPerSec;
                    case WoundType.Gunshot: return CloneZConfig.LacerationBleedPerSec * 0.9f;
                    case WoundType.HeavyBleed: return CloneZConfig.LacerationBleedPerSec * 2.6f;
                    case WoundType.Deep: return CloneZConfig.LacerationBleedPerSec * 1.3f;
                    case WoundType.Shard: return CloneZConfig.ScratchBleedPerSec;
                    case WoundType.Burn: return 0f;
                    default: return CloneZConfig.LacerationBleedPerSec;
                }
            }
        }

        /// <summary>Gets the base pain value for this wound type.</summary>
        public float BasePain
        {
            get
            {
                switch (Type)
                {
                    case WoundType.Scratch: return CloneZConfig.ScratchPain;
                    case WoundType.Bite: return CloneZConfig.BitePain;
                    case WoundType.Gunshot: return CloneZConfig.LacerationPain * 1.3f;
                    case WoundType.HeavyBleed: return CloneZConfig.LacerationPain * 1.6f;
                    case WoundType.Deep: return CloneZConfig.LacerationPain * 1.4f;
                    case WoundType.Shard: return CloneZConfig.LacerationPain * 1.1f;
                    case WoundType.Burn: return CloneZConfig.LacerationPain * 1.2f;
                    default: return CloneZConfig.LacerationPain;
                }
            }
        }

        /// <summary>Human‑readable name of the wound including body zone.</summary>
        public string Name
        {
            get
            {
                string n;
                switch (Type)
                {
                    case WoundType.Bite: n = "Bite"; break;
                    case WoundType.Laceration: n = "Laceration"; break;
                    case WoundType.Gunshot: n = "Gunshot wound"; break;
                    case WoundType.HeavyBleed: n = "Heavy bleeding"; break;
                    case WoundType.Deep: n = NeedsStitches ? "Deep wound (needs stitches)" : "Stitched wound"; break;
                    case WoundType.Shard: n = Lodged ? "Shard lodged" : "Shard wound"; break;
                    case WoundType.Burn: n = "Burn"; break;
                    default: n = "Scratch"; break;
                }
                return n + " (" + BodyZones.Name(Zone) + ")";
            }
        }

        /// <summary>A bandage alone can't stop this bleed (it needs a tourniquet or haemostatic gauze).</summary>
        public bool IsHeavy => Type == WoundType.HeavyBleed && !Tourniquet;
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

        /// <summary>Read‑only list of all current wounds.</summary>
        public IReadOnlyList<Wound> Wounds => _wounds;
        /// <summary>Number of current wounds.</summary>
        public int WoundCount => _wounds.Count;
        /// <summary>True while antibiotic coverage timer is positive.</summary>
        public bool AntibioticsActive => _antibioticTimer > 0f;
        /// <summary>True while painkiller timer is positive.</summary>
        public bool PainkillersActive => _painkillerTimer > 0f;

        // Raw timer readouts, captured/restored by the save system.
        /// <summary>Current antibiotic timer value (seconds of coverage).</summary>
        public float AntibioticTimer => _antibioticTimer;
        /// <summary>Current painkiller timer value (seconds remaining).</summary>
        public float PainkillerTimer => _painkillerTimer;
        /// <summary>Current painkiller relief fraction (0..1).</summary>
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

        /// <summary>True when any wound is bleeding (used by UI to show bandage hint).</summary>
        public bool NeedsBandage => IsBleeding;

        /// <summary>True when any wound is infected.</summary>
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
        public void Inflict(WoundType type) => Inflict(type, BodyZones.RandomStrike());

        /// <summary>Opens a fresh wound on a body zone. Gunshots and blasts may leave fragments lodged; deep wounds
        /// need stitches before they can close.</summary>
        public Wound Inflict(WoundType type, BodyZone zone)
        {
            var w = new Wound { Type = type, Zone = zone };
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
                case WoundType.Gunshot:
                    w.HealSeconds = CloneZConfig.LacerationHealSeconds * 1.6f;
                    w.Prone = Random.value < CloneZConfig.LacerationSepsisChance * 1.3f;
                    w.Lodged = Random.value < 0.25f;
                    break;
                case WoundType.HeavyBleed:
                    w.HealSeconds = CloneZConfig.LacerationHealSeconds * 2f;
                    w.Prone = Random.value < CloneZConfig.LacerationSepsisChance * 1.5f;
                    break;
                case WoundType.Deep:
                    w.HealSeconds = CloneZConfig.LacerationHealSeconds * 1.8f;
                    w.Prone = Random.value < CloneZConfig.LacerationSepsisChance * 1.4f;
                    w.NeedsStitches = true;
                    break;
                case WoundType.Shard:
                    w.HealSeconds = CloneZConfig.LacerationHealSeconds;
                    w.Prone = Random.value < CloneZConfig.LacerationSepsisChance;
                    w.Lodged = true;
                    break;
                case WoundType.Burn:
                    w.HealSeconds = CloneZConfig.LacerationHealSeconds * 1.5f;
                    w.Prone = Random.value < CloneZConfig.ScratchSepsisChance;
                    w.Bleeding = false;
                    break;
                default:
                    w.HealSeconds = CloneZConfig.LacerationHealSeconds;
                    w.Prone = Random.value < CloneZConfig.LacerationSepsisChance;
                    break;
            }
            w.Prone = w.Prone && Random.value >= PlayerModifiers.Current.InfectionResist;
            _wounds.Add(w);
            // A fresh wound spikes pain immediately.
            _stats.Pain = Mathf.Min(CloneZConfig.PainMax, _stats.Pain + w.BasePain * 0.5f);
            return w;
        }

        /// <summary>True when any wound on <paramref name="zone"/> is still bleeding.</summary>
        public bool IsBleedingOn(BodyZone zone)
        {
            for (int i = 0; i < _wounds.Count; i++)
                if (_wounds[i].Zone == zone && _wounds[i].Bleeding && !_wounds[i].Bandaged) return true;
            return false;
        }

        /// <summary>True when a heavy bleed exists on a limb that can be tourniquetted.</summary>
        public bool HasHeavyBleed
        {
            get
            {
                for (int i = 0; i < _wounds.Count; i++) if (_wounds[i].IsHeavy && _wounds[i].Bleeding) return true;
                return false;
            }
        }

        /// <summary>Holds the worst heavy limb bleed with a tourniquet (limbs only). False when none needs one.</summary>
        public bool ApplyTourniquet()
        {
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                if (!w.IsHeavy || !w.Bleeding || !(BodyZones.IsArm(w.Zone) || BodyZones.IsLeg(w.Zone))) continue;
                w.Tourniquet = true;
                w.Bleeding = false;
                w.Bandaged = true;
                return true;
            }
            return false;
        }

        /// <summary>Packs a heavy bleed anywhere (haemostatic gauze). False when there's none.</summary>
        public bool PackHeavyBleed()
        {
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                if (w.Type != WoundType.HeavyBleed || !w.Bleeding) continue;
                w.Bleeding = false;
                w.Bandaged = true;
                w.CleanBandage = true;
                w.Tourniquet = false;
                return true;
            }
            return false;
        }

        /// <summary>Stitches a deep wound so it can close. False when none needs it.</summary>
        public bool Stitch()
        {
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                if (!w.NeedsStitches) continue;
                w.NeedsStitches = false;
                w.Bleeding = false;
                w.Bandaged = true;
                _stats.Pain = Mathf.Min(CloneZConfig.PainMax, _stats.Pain + 25f);
                return true;
            }
            return false;
        }

        /// <summary>Pulls out a lodged shard or fragment (tweezers). False when nothing is lodged.</summary>
        public bool RemoveShard()
        {
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                if (!w.Lodged) continue;
                w.Lodged = false;
                _stats.Pain = Mathf.Min(CloneZConfig.PainMax, _stats.Pain + 12f);
                return true;
            }
            return false;
        }

        /// <summary>Wounds on zones where a tourniquet is still on (the limb slowly suffers for it).</summary>
        public int TourniquetsOn
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _wounds.Count; i++) if (_wounds[i].Tourniquet) n++;
                return n;
            }
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
                        Zone = (BodyZone)Mathf.Clamp(w.Zone, 0, BodyZones.Count - 1),
                        Bleeding = w.Bleeding,
                        Bandaged = w.Bandaged,
                        CleanBandage = w.CleanBandage,
                        Disinfected = w.Disinfected,
                        Infected = w.Infected,
                        Prone = w.Prone,
                        Sepsis = w.Sepsis,
                        Healing = w.Healing,
                        HealSeconds = w.HealSeconds,
                        NeedsStitches = (w.Flags & 1) != 0,
                        Lodged = (w.Flags & 2) != 0,
                        Tourniquet = (w.Flags & 4) != 0,
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

                // A dressed, uninfected wound knits shut over time (faster while asleep) — unless a deep wound
                // still needs stitches or a shard is still lodged in it.
                if (w.Bandaged && !w.Infected && !w.NeedsStitches && !w.Lodged)
                {
                    w.Healing += dt * healScale * healMult / Mathf.Max(1f, w.HealSeconds);
                    if (w.Healing >= 1f) { _wounds.RemoveAt(i); continue; }
                }

                // Pain fades as the wound closes; an infection keeps it raw.
                float p = w.BasePain * (1f - w.Healing);
                if (w.Infected) p += CloneZConfig.SepsisPainBonus;
                if (w.Lodged) p += 8f;
                painTarget += p;
            }

            if (healthDrain > 0f) _stats.Bleed(healthDrain * bleedMult * dt);

            // Pain spikes quickly but ebbs slowly; painkillers blunt what you feel.
            painTarget = Mathf.Min(CloneZConfig.PainMax, painTarget) * (1f - _painkillerRelief) * (1f - Mathf.Clamp01(PlayerModifiers.Current.PainResist));
            float rate = painTarget > _stats.Pain ? 40f : CloneZConfig.PainDecayPerSec;
            _stats.Pain = Mathf.MoveTowards(_stats.Pain, painTarget, rate * dt);
        }

        // Bleeding and healing bent by traits, artifacts, Zone marks and the Vitality skill.
        private PlayerSkills _skills;
        private float bleedMult
        {
            get
            {
                if (_skills == null) _skills = GetComponent<PlayerSkills>();
                return PlayerModifiers.Current.BleedRate * (_skills != null ? Mathf.Max(0.4f, _skills.VitalityBleedMult) : 1f);
            }
        }

        private float healMult
        {
            get
            {
                if (_skills == null) _skills = GetComponent<PlayerSkills>();
                return PlayerModifiers.Current.HealRate * (_skills != null ? _skills.VitalityHealMult : 1f);
            }
        }

        #region Treatment (driven by medical items)
        /// <summary>Dresses the most urgent open wound. Returns false if nothing needed dressing. A heavy bleed
        /// soaks straight through a bandage — it needs a tourniquet or haemostatic gauze.</summary>
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

        // The wound most in need of a dressing: bleeding first, then by severity (heavy bleeds need more than a bandage).
        private Wound WorstUndressed()
        {
            Wound best = null;
            for (int i = 0; i < _wounds.Count; i++)
            {
                Wound w = _wounds[i];
                if (w.Bandaged || w.IsHeavy) continue;
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
