using System;
using UnityEngine;

namespace CloneZ
{
    public enum Exertion { Idle, Walking, Running, Sprinting }

    /// <summary>
    /// Survival stats for the player: health, endurance, hunger, thirst and the
    /// zombie infection. Modelled on Project Zomboid's Stats / BodyDamage systems.
    /// </summary>
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Vitals")]
        public float Health = CloneZConfig.HealthMax;      // 0..100
        public float Endurance = CloneZConfig.EnduranceMax; // 0..1
        public float Hunger;                                // 0..1 (1 = starving)
        public float Thirst;                                // 0..1 (1 = dehydrated)
        public float Fatigue;                               // 0..1 (1 = exhausted, needs sleep)
        public float Infection;                             // 0..100
        public bool Infected;
        public float Pain;                                  // 0..100 (open wounds hurt; slows and worsens aim)
        public float Panic;                                 // 0..100 (fear near the dead; shakes aim, robs sleep) — owned by PlayerPanic
        public float Temperature = CloneZConfig.TempStart;  // 0..1 (0 = hypothermic, 0.5 = comfortable, 1 = overheating) — owned by PlayerTemperature
        public float Wetness;                               // 0..1 (1 = soaked) — owned by PlayerTemperature
        public float Sickness;                              // 0..100 (food poisoning / nausea; slows and stops healing) — owned by PlayerFoodSickness

        public Exertion Exertion = Exertion.Idle;
        public bool IsDead { get; private set; }
        public Transform Transform => transform;

        public float HealthNormalized => Mathf.Clamp01(Health / CloneZConfig.HealthMax);
        public float InfectionNormalized => Mathf.Clamp01(Infection / CloneZConfig.InfectionMax);
        public float PainNormalized => Mathf.Clamp01(Pain / CloneZConfig.PainMax);
        public float PanicNormalized => Mathf.Clamp01(Panic / CloneZConfig.PanicMax);
        public float SicknessNormalized => Mathf.Clamp01(Sickness / CloneZConfig.SicknessMax);
        public bool CanSprint => Endurance > 0.05f && Fatigue < CloneZConfig.FatigueExhaustedThreshold && !IsDead;

        /// <summary>How cold the survivor feels, 0 (comfortable) .. 1 (freezing).</summary>
        public float ColdStrength =>
            Mathf.Clamp01((CloneZConfig.TempComfortLow - Temperature) / Mathf.Max(0.01f, CloneZConfig.TempComfortLow));

        /// <summary>How hot the survivor feels, 0 (comfortable) .. 1 (overheating).</summary>
        public float HeatStrength =>
            Mathf.Clamp01((Temperature - CloneZConfig.TempComfortHigh) / Mathf.Max(0.01f, 1f - CloneZConfig.TempComfortHigh));

        /// <summary>Move-speed scale: cold-stiffened muscles and heavy wet clothes drag you down.</summary>
        public float ColdMoveMultiplier =>
            Mathf.Clamp(1f - ColdStrength * CloneZConfig.TempColdMovePenalty
                        - Mathf.Clamp01(Wetness) * CloneZConfig.WetMovePenalty, 0.6f, 1f);

        /// <summary>Firearm aim penalty from shivering in the cold (0 when comfortable).</summary>
        public float ColdAimPenalty => ColdStrength * CloneZConfig.TempColdAimPenalty;

        /// <summary>Move-speed scale: an exhausted survivor drags their feet.</summary>
        public float FatigueMoveMultiplier =>
            Mathf.Lerp(1f, 1f - CloneZConfig.FatigueMovePenalty, Mathf.InverseLerp(0.5f, 1f, Fatigue));

        /// <summary>Move-speed scale from pain: badly hurt survivors move gingerly.</summary>
        public float PainMoveMultiplier =>
            Mathf.Lerp(1f, 1f - CloneZConfig.PainMovePenalty, Mathf.InverseLerp(CloneZConfig.PainMoveStart, CloneZConfig.PainMax, Pain));

        /// <summary>Melee damage scale from pain (a shaking, hurting arm hits softer).</summary>
        public float PainCombatMultiplier =>
            Mathf.Lerp(1f, 1f - CloneZConfig.PainMeleePenalty, PainNormalized);

        /// <summary>Move-speed scale from food poisoning: a queasy survivor slows to a trudge.</summary>
        public float SicknessMoveMultiplier =>
            Mathf.Lerp(1f, 1f - CloneZConfig.SicknessMovePenalty, Mathf.InverseLerp(CloneZConfig.SicknessMoveStart, CloneZConfig.SicknessMax, Sickness));

        public event Action OnDamaged;
        public event Action OnBitten;
        public event Action OnDied;

        private PlayerSkills _skills;
        private PlayerSkills Skills => _skills != null ? _skills : (_skills = GetComponent<PlayerSkills>());

        private PlayerWounds _wounds;
        private PlayerWounds Wounds => _wounds != null ? _wounds : (_wounds = GetComponent<PlayerWounds>());

        private PlayerClothing _clothing;
        private PlayerClothing Clothing => _clothing != null ? _clothing : (_clothing = GetComponent<PlayerClothing>());

        private void Update()
        {
            if (IsDead) return;
            float dt = Time.deltaTime;

            UpdateEndurance(dt);
            UpdateSurvival(dt);
            UpdateFatigue(dt);
            UpdateInfection(dt);
            UpdatePassiveHealth(dt);

            if (Health <= 0f) Die("succumbed to their wounds");
        }

        private void UpdateEndurance(float dt)
        {
            switch (Exertion)
            {
                case Exertion.Sprinting:
                    Endurance -= CloneZConfig.EnduranceSprintDrain * dt;
                    break;
                case Exertion.Running:
                    Endurance -= CloneZConfig.EnduranceRunDrain * dt;
                    break;
                default:
                    // A tired body catches its breath more slowly.
                    float regen = CloneZConfig.EnduranceRegen * (1f - CloneZConfig.FatigueEnduranceRegenPenalty * Fatigue);
                    Endurance += regen * dt;
                    break;
            }
            Endurance = Mathf.Clamp01(Endurance);
        }

        private void UpdateFatigue(float dt)
        {
            float rate = CloneZConfig.FatigueRatePerSec;
            if (Exertion == Exertion.Sprinting) rate += CloneZConfig.FatigueExertionBonus;
            else if (Exertion == Exertion.Running) rate += CloneZConfig.FatigueExertionBonus * 0.5f;
            if (TimeOfDay.Instance != null && TimeOfDay.Instance.IsNight) rate *= CloneZConfig.FatigueNightMultiplier;
            if (Skills != null) rate *= Skills.FatigueRateMult;   // Fitness tires you more slowly (PZ)
            Fatigue = Mathf.Clamp01(Fatigue + rate * dt);
        }

        private void UpdateSurvival(float dt)
        {
            Hunger = Mathf.Clamp01(Hunger + CloneZConfig.HungerRatePerSec * dt);
            Thirst = Mathf.Clamp01(Thirst + CloneZConfig.ThirstRatePerSec * dt);

            if (Hunger >= 1f) Health -= CloneZConfig.StarvingHealthDrainPerSec * dt;
            if (Thirst >= 1f) Health -= CloneZConfig.DehydratedHealthDrainPerSec * dt;
        }

        private void UpdateInfection(float dt)
        {
            if (!Infected) return;
            Infection = Mathf.Clamp(Infection + CloneZConfig.InfectionGrowthPerSec * dt, 0f, CloneZConfig.InfectionMax);
            // The sickness steadily damages the body as it takes hold.
            Health -= CloneZConfig.InfectionHealthDrainPerSec * (Infection / CloneZConfig.InfectionMax) * dt;
            if (Infection >= CloneZConfig.InfectionMax)
                Die("turned into one of them");
        }

        private void UpdatePassiveHealth(float dt)
        {
            // A body fighting food poisoning can't spare anything for healing.
            bool wellFed = Hunger < 0.75f && Thirst < 0.75f && Sickness < CloneZConfig.SicknessRegenBlock;
            if (wellFed && !Infected && Health < CloneZConfig.HealthMax)
                Health = Mathf.Min(CloneZConfig.HealthMax, Health + CloneZConfig.HealthRegenPerSec * dt);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            float dmg = info.Amount * CloneZConfig.PlayerReceivedDamageMultiplier;
            if (Skills != null)
            {
                dmg *= Skills.IncomingDamageMult;   // Fitness + Nimble soak some of the blow (PZ)
                Skills.AddXp(PlayerSkills.Skill.Fitness, CloneZConfig.XpFitnessOnHurt);
            }
            Health -= dmg;
            OnDamaged?.Invoke();
            if (Health <= 0f) Die("was torn apart");
        }

        /// <summary>Called by a zombie that lands an attack.</summary>
        public void ReceiveZombieAttack(bool bite)
        {
            if (IsDead) return;
            float mult = Skills != null ? Skills.IncomingDamageMult : 1f;
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Fitness, CloneZConfig.XpFitnessOnHurt);
            if (bite)
            {
                // Sturdy clothing (leather especially) can stop the teeth reaching skin — the
                // strike still bruises through the fabric, but leaves no wound and no infection.
                if (Clothing != null && Clothing.BlocksBite())
                {
                    Health -= CloneZConfig.ZombieBiteDamage * mult * CloneZConfig.ClothingBlockedDamageMult;
                    OnDamaged?.Invoke();
                    if (Health <= 0f) Die("was torn apart");
                    return;
                }
                Health -= CloneZConfig.ZombieBiteDamage * mult;
                Wounds?.Inflict(WoundType.Bite);
                if (!Infected)
                {
                    Infected = true;      // In PZ a bite is effectively a death sentence.
                    OnBitten?.Invoke();
                }
            }
            else
            {
                // Clothing turns aside many scratches before they break the skin.
                if (Clothing != null && Clothing.BlocksScratch())
                {
                    Health -= CloneZConfig.ZombieScratchDamage * mult * CloneZConfig.ClothingBlockedDamageMult;
                    OnDamaged?.Invoke();
                    if (Health <= 0f) Die("was torn apart");
                    return;
                }
                Health -= CloneZConfig.ZombieScratchDamage * mult;
                // A deeper laceration is the common non-bite wound (PZ baseLacerationChance 65%).
                WoundType wt = UnityEngine.Random.value < CloneZConfig.ZombieScratchChance
                    ? WoundType.Laceration : WoundType.Scratch;
                Wounds?.Inflict(wt);
                // A scratch also carries a small chance of the zombie virus.
                if (!Infected && UnityEngine.Random.value < 0.07f)
                {
                    Infected = true;
                    OnBitten?.Invoke();
                }
            }
            OnDamaged?.Invoke();
            if (Health <= 0f) Die("was torn apart");
        }

        public bool TryConsumeEndurance(float amount)
        {
            if (Endurance <= 0f) return false;
            Endurance = Mathf.Clamp01(Endurance - amount);
            return true;
        }

        /// <summary>Continuous health loss from an open wound (driven by <see cref="PlayerWounds"/>).
        /// Deliberately silent — it must not fire OnDamaged every frame (that would jolt a sleeper awake).</summary>
        public void Bleed(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Health -= amount;
            if (Health <= 0f) Die("bled out");
        }

        /// <summary>Silent continuous health loss from the environment (cold/heat). Like <see cref="Bleed"/>
        /// it must not fire OnDamaged every frame (that would jolt a sleeper awake), but it names its own cause.</summary>
        public void EnvironmentDrain(float amount, string cause)
        {
            if (IsDead || amount <= 0f) return;
            Health -= amount;
            if (Health <= 0f) Die(cause);
        }

        public void Eat(float amount) => Hunger = Mathf.Clamp01(Hunger - amount);
        public void Drink(float amount) => Thirst = Mathf.Clamp01(Thirst - amount);

        /// <summary>Called each frame while asleep: sheds fatigue and knits wounds (PZ sleeping regen).</summary>
        public void Sleep(float dt)
        {
            if (IsDead) return;
            Fatigue = Mathf.Max(0f, Fatigue - CloneZConfig.SleepFatigueRecoveryPerSec * dt);
            Endurance = Mathf.Clamp01(Endurance + CloneZConfig.EnduranceRegen * 2f * dt);
            if (!Infected && Health < CloneZConfig.HealthMax)
                Health = Mathf.Min(CloneZConfig.HealthMax, Health + CloneZConfig.SleepHealthRegenPerSec * dt);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Health = Mathf.Min(CloneZConfig.HealthMax, Health + amount);
        }

        public void ReduceInfection(float amount)
        {
            if (amount <= 0f) return;
            Infection = Mathf.Max(0f, Infection - amount);
            if (Infection <= 0f) Infected = false;   // shook off a minor infection (a bite outpaces this)
        }

        private void Die(string cause)
        {
            if (IsDead) return;
            IsDead = true;
            Health = 0f;
            OnDied?.Invoke();
            GameManager.Instance?.TriggerGameOver(cause);
        }
    }
}
