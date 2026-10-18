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

        /// <summary>Knocked out (shock, blood loss): no movement, no actions, until it wears off.</summary>
        public bool Incapacitated;

        /// <summary>This body is the survivor played on this machine (remote players are proxies, not PlayerStats).</summary>
        public bool IsLocal => true;

        public float HealthNormalized => Mathf.Clamp01(Health / CloneZConfig.HealthMax);
        public float InfectionNormalized => Mathf.Clamp01(Infection / CloneZConfig.InfectionMax);
        public float PainNormalized => Mathf.Clamp01(Pain / CloneZConfig.PainMax);
        public float PanicNormalized => Mathf.Clamp01(Panic / CloneZConfig.PanicMax);
        public float SicknessNormalized => Mathf.Clamp01(Sickness / CloneZConfig.SicknessMax);
        public bool CanSprint => Endurance > 0.05f && Fatigue < CloneZConfig.FatigueExhaustedThreshold && !IsDead && !Incapacitated
                                 && (Limbs == null || Limbs.CanSprint);

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

        private PlayerLimbs _limbs;
        private PlayerLimbs Limbs => _limbs != null ? _limbs : (_limbs = GetComponent<PlayerLimbs>());

        private PlayerBlood _blood;
        private PlayerBlood BloodPool => _blood != null ? _blood : (_blood = GetComponent<PlayerBlood>());

        private void Update()
        {
            if (IsDead) return;
            float dt = Time.deltaTime;

            UpdateEndurance(dt);
            // The hub is a safe, warm room: needs don't tick down while you trade and talk.
            if (!HubRules.SafeZone)
            {
                UpdateSurvival(dt);
                UpdateFatigue(dt);
                UpdateInfection(dt);
            }
            UpdatePassiveHealth(dt);

            if (Health <= 0f) Die("succumbed to their wounds");
        }

        private void UpdateEndurance(float dt)
        {
            StatMods mods = PlayerModifiers.Current;
            float drainMult = mods.StaminaDrain * (Skills != null ? Mathf.Max(0.5f, Skills.EnduranceDrainMult) : 1f);
            switch (Exertion)
            {
                case Exertion.Sprinting:
                    Endurance -= CloneZConfig.EnduranceSprintDrain * drainMult * dt;
                    if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Endurance, 0.6f * dt);
                    break;
                case Exertion.Running:
                    Endurance -= CloneZConfig.EnduranceRunDrain * drainMult * dt;
                    break;
                default:
                    // A tired body catches its breath more slowly.
                    float regen = CloneZConfig.EnduranceRegen * (1f - CloneZConfig.FatigueEnduranceRegenPenalty * Fatigue) * mods.StaminaRegen;
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
            rate *= PlayerModifiers.Current.FatigueRate * SandboxSettings.Current.FatigueRate * SandboxSettings.Current.DrainScale("fatigue");
            Fatigue = Mathf.Clamp01(Fatigue + rate * dt);
        }

        private void UpdateSurvival(float dt)
        {
            SandboxSettings sb = SandboxSettings.Current;
            StatMods mods = PlayerModifiers.Current;
            float metabolism = Skills != null ? Mathf.Max(0.5f, Skills.MetabolismMult) : 1f;
            Hunger = Mathf.Clamp01(Hunger + CloneZConfig.HungerRatePerSec * mods.HungerRate * sb.HungerRate * sb.DrainScale("hunger") * metabolism * dt);
            Thirst = Mathf.Clamp01(Thirst + CloneZConfig.ThirstRatePerSec * mods.ThirstRate * sb.ThirstRate * sb.DrainScale("thirst") * metabolism * dt);
            if (Skills != null && (Hunger < 0.3f && Thirst < 0.3f)) Skills.AddXp(PlayerSkills.Skill.Metabolism, 0.05f * dt);

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
                Health = Mathf.Min(CloneZConfig.HealthMax, Health + CloneZConfig.HealthRegenPerSec * PlayerModifiers.Current.HealRate * dt);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead || HubRules.SafeZone) return;
            // Under a raid's spawn protection the dead can't hurt the survivor at all.
            if (ExtractionManager.PlayerProtected && info.Source != null && info.Source.GetComponentInParent<ZombieHealth>() != null) return;
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Fitness, CloneZConfig.XpFitnessOnHurt);
            if (Limbs != null)
            {
                // The limb model handles armour, resistances, wounds and the health pool.
                Limbs.ApplyHit(info);
                if (!IsDead) OnDamaged?.Invoke();
                return;
            }
            float dmg = info.AmountArena * CloneZConfig.PlayerReceivedDamageMultiplier;
            if (Skills != null) dmg *= Skills.IncomingDamageMult;   // Fitness + Nimble soak some of the blow (PZ)
            Health -= dmg;
            OnDamaged?.Invoke();
            if (Health <= 0f) Die("was torn apart");
        }

        /// <summary>Health lost to hit points taken off a body zone (driven by <see cref="PlayerLimbs"/>).</summary>
        public void LimbDamage(float health, string cause)
        {
            if (IsDead || health <= 0f) return;
            Health -= health;
            if (Health <= 0f) Die(cause);
        }

        /// <summary>Ends the survivor's life outright (a destroyed head or thorax, total blood loss).</summary>
        public void Kill(string cause) => Die(cause);

        /// <summary>Called by a zombie that lands an attack. Ignored while a raid's spawn protection holds.</summary>
        public void ReceiveZombieAttack(bool bite)
        {
            if (IsDead || ExtractionManager.PlayerProtected || HubRules.SafeZone) return;
            float mult = Skills != null ? Skills.IncomingDamageMult : 1f;
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Fitness, CloneZConfig.XpFitnessOnHurt);
            InfectionMode infection = SandboxSettings.Current.Infection;
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
                BodyZone zone = BodyZones.RandomStrike();
                if (Limbs != null && !Limbs.ZombieStrike(true, CloneZConfig.ZombieBiteDamage * mult, out zone))
                {
                    OnDamaged?.Invoke();   // armour turned the teeth
                    return;
                }
                if (Limbs == null) Health -= CloneZConfig.ZombieBiteDamage * mult;
                if (IsDead) return;
                Wounds?.Inflict(WoundType.Bite, zone);
                bool turns = infection == InfectionMode.BiteLethal || (infection == InfectionMode.BiteChance && UnityEngine.Random.value < 0.25f);
                if (turns && !Infected && UnityEngine.Random.value >= PlayerModifiers.Current.InfectionResist * 0.5f)
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
                BodyZone zone = BodyZones.RandomStrike();
                if (Limbs != null && !Limbs.ZombieStrike(false, CloneZConfig.ZombieScratchDamage * mult, out zone))
                {
                    OnDamaged?.Invoke();
                    return;
                }
                if (Limbs == null) Health -= CloneZConfig.ZombieScratchDamage * mult;
                if (IsDead) return;
                // A deeper laceration is the common non-bite wound (PZ baseLacerationChance 65%).
                WoundType wt = UnityEngine.Random.value < CloneZConfig.ZombieScratchChance
                    ? WoundType.Laceration : WoundType.Scratch;
                Wounds?.Inflict(wt, zone);
                // A scratch also carries a small chance of the zombie virus.
                if (infection != InfectionMode.Off && !Infected && UnityEngine.Random.value < 0.07f * (1f - PlayerModifiers.Current.InfectionResist))
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
        /// Deliberately silent — it must not fire OnDamaged every frame (that would jolt a sleeper awake).
        /// The blood that runs out comes out of the blood pool too.</summary>
        public void Bleed(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Health -= amount;
            if (BloodPool != null) BloodPool.LoseBloodForHealth(amount);
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
            Incapacitated = false;
            OnDied?.Invoke();
            Actor me = GetComponent<Actor>();
            GameEvents.RaiseActorKilled(new KillInfo { Victim = me, Position = transform.position, WeaponId = cause });
            GameManager.Instance?.TriggerGameOver(cause);
        }
    }
}
