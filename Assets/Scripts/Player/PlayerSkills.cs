using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The player's Project Zomboid–style skills (perks). Each skill accrues XP as
    /// the survivor performs matching actions, levelling 0..10 and unlocking steadily
    /// stronger effects. The per-level modifier tables are transcribed directly from
    /// the reference game (PerkFactory + IsoGameCharacter.get*Mod), so a maxed skill
    /// feels the way it does in PZ.
    /// </summary>
    public class PlayerSkills : MonoBehaviour
    {
        // Appended skills keep the saved indexes of the original thirteen intact.
        public enum Skill
        {
            Blade, Blunt, Aiming, Reloading, Fitness, Strength, Sprinting, Nimble, Sneak, Lightfooted, Carpentry, Cooking, Farming,
            Endurance, Vitality, Metabolism, FirstAid, Mechanics, Foraging, Fishing, Trapping, Perception, Charisma,
        }

        private static readonly int SkillCount = Enum.GetValues(typeof(Skill)).Length;
        private readonly float[] _xp = new float[SkillCount];

        /// <summary>Every skill, for UIs and saves.</summary>
        public static readonly Skill[] AllSkills = (Skill[])Enum.GetValues(typeof(Skill));

        /// <summary>Optional per-skill XP multiplier (skill books, the character's level perks).</summary>
        public static Func<Skill, int, float> XpMultiplier;

        /// <summary>Raised when a skill crosses into a new level: (skill, newLevel).</summary>
        public event Action<Skill, int> OnLevelUp;
        /// <summary>Raised whenever any XP is gained (for UI refresh).</summary>
        public event Action OnXpGained;

        // ---- PZ-grounded per-level effect tables (index = skill level 0..10) ----
        // IsoPlayer.getAimingMod — firearm damage / to-hit multiplier.
        private static readonly float[] AimDamageTable = { 1.00f, 1.10f, 1.14f, 1.18f, 1.22f, 1.26f, 1.30f, 1.34f, 1.36f, 1.40f, 1.50f };
        // IsoPlayer.getInvAimingMod — spread multiplier (lower = tighter cone).
        private static readonly float[] AimSpreadTable = { 1.00f, 0.90f, 0.86f, 0.82f, 0.74f, 0.70f, 0.66f, 0.62f, 0.58f, 0.54f, 0.50f };
        // IsoPlayer.getAimingRangeMod — effective range multiplier.
        private static readonly float[] AimRangeTable = { 1.00f, 1.20f, 1.28f, 1.36f, 1.42f, 1.50f, 1.58f, 1.66f, 1.72f, 1.80f, 2.00f };
        // IsoGameCharacter.getNimbleMod — movement speed while in a combat/aiming stance.
        private static readonly float[] NimbleTable = { 1.00f, 1.10f, 1.14f, 1.18f, 1.22f, 1.26f, 1.30f, 1.34f, 1.38f, 1.42f, 1.50f };
        // IsoGameCharacter.getSneakSpotMod (without PZ's ×1.2) — detection-radius multiplier while crouched.
        private static readonly float[] SneakTable = { 0.90f, 0.90f, 0.80f, 0.75f, 0.70f, 0.65f, 0.60f, 0.55f, 0.50f, 0.45f, 0.40f };
        // IsoGameCharacter.getLightfootMod — footstep-noise multiplier.
        private static readonly float[] LightfootTable = { 1.00f, 0.90f, 0.79f, 0.71f, 0.65f, 0.59f, 0.52f, 0.45f, 0.39f, 0.34f, 0.30f };
        // IsoGameCharacter.getFatigueMod — fatigue-accrual multiplier.
        private static readonly float[] FatigueTable = { 1.00f, 0.95f, 0.92f, 0.89f, 0.87f, 0.85f, 0.83f, 0.81f, 0.79f, 0.77f, 0.75f };

        private static readonly string[] DisplayNames =
        {
            "Blade", "Blunt", "Aiming", "Reloading", "Fitness",
            "Strength", "Sprinting", "Nimble", "Sneaking", "Lightfooted", "Carpentry", "Cooking", "Farming",
            "Endurance", "Vitality", "Metabolism", "First Aid", "Mechanics", "Foraging", "Fishing", "Trapping", "Perception", "Charisma",
        };

        #region XP & levels
        public float GetXp(Skill s) => _xp[(int)s];

        /// <summary>The XP a skill needs to reach <paramref name="level"/> (general curve).</summary>
        public static float XpForLevel(int level)
        {
            int[] curve = CloneZConfig.SkillXpCurve;
            if (level <= 0) return 0f;
            return curve[Mathf.Clamp(level - 1, 0, curve.Length - 1)];
        }

        public int GetLevel(Skill s)
        {
            int[] curve = CurveFor(s);
            float xp = _xp[(int)s];
            int level = 0;
            for (int i = 0; i < curve.Length; i++)
            {
                if (xp >= curve[i]) level = i + 1;
                else break;
            }
            return Mathf.Min(level, CloneZConfig.SkillMaxLevel);
        }

        /// <summary>Fraction (0..1) of the way from the current level to the next.</summary>
        public float LevelProgress(Skill s)
        {
            int level = GetLevel(s);
            if (level >= CloneZConfig.SkillMaxLevel) return 1f;
            int[] curve = CurveFor(s);
            float prev = level == 0 ? 0f : curve[level - 1];
            float next = curve[level];
            return Mathf.Clamp01((_xp[(int)s] - prev) / Mathf.Max(1f, next - prev));
        }

        public void AddXp(Skill s, float amount)
        {
            if (amount <= 0f) return;
            int before = GetLevel(s);
            amount *= PlayerModifiers.Current.XpMult;
            if (XpMultiplier != null)
            {
                try { amount *= Mathf.Max(0f, XpMultiplier(s, before)); }
                catch (Exception e) { Debug.LogException(e); }
            }
            _xp[(int)s] += amount;
            OnXpGained?.Invoke();

            int after = GetLevel(s);
            for (int lvl = before + 1; lvl <= after; lvl++)
                OnLevelUp?.Invoke(s, lvl);
        }

        /// <summary>A copy of every skill's XP, indexed by the Skill enum.</summary>
        public float[] ExportXp() => (float[])_xp.Clone();

        /// <summary>Overwrites all skill XP from a saved game (indexed by the Skill enum order).</summary>
        public void LoadXp(float[] xp)
        {
            if (xp == null) return;
            int n = Mathf.Min(xp.Length, _xp.Length);
            for (int i = 0; i < n; i++) _xp[i] = Mathf.Max(0f, xp[i]);
            OnXpGained?.Invoke();
        }

        private static int[] CurveFor(Skill s)
            => (s == Skill.Fitness || s == Skill.Strength) ? CloneZConfig.PhysicalXpCurve : CloneZConfig.SkillXpCurve;

        public static string NameOf(Skill s) => DisplayNames[(int)s];
        #endregion

        #region Effect accessors (read by the combat / movement / stat systems)
        public float FirearmDamageMult => AimDamageTable[GetLevel(Skill.Aiming)];
        public float FirearmSpreadMult => AimSpreadTable[GetLevel(Skill.Aiming)];
        public float FirearmRangeMult => AimRangeTable[GetLevel(Skill.Aiming)];

        /// <summary>Reload-time multiplier (PZ getReloadingMod = 3.5 − lvl·0.25, normalised).</summary>
        public float ReloadTimeMult
        {
            get
            {
                int lvl = GetLevel(Skill.Reloading);
                return Mathf.Max(0.4f, (3.5f - lvl * 0.25f) / 3.5f);
            }
        }

        public float NimbleMoveMult => NimbleTable[GetLevel(Skill.Nimble)];
        public float FatigueRateMult => FatigueTable[GetLevel(Skill.Fitness)];
        public float FootstepNoiseMult => LightfootTable[GetLevel(Skill.Lightfooted)];
        public float SprintSpeedMult => 1f + GetLevel(Skill.Sprinting) * CloneZConfig.SprintSkillSpeedPerLevel;
        // Skills start at 0 here (PZ defaults to 5), so Strength only ever adds capacity.
        public float CarryCapacityMult => 1f + GetLevel(Skill.Strength) * CloneZConfig.StrengthCarryPerLevel;

        /// <summary>Build-time multiplier for carpentry work (higher Carpentry = faster hammering).</summary>
        public float CarpentryBuildTimeMult
            => Mathf.Max(0.4f, 1f - GetLevel(Skill.Carpentry) * CloneZConfig.CarpentryBuildSpeedPerLevel);

        /// <summary>Barricade HP multiplier (a skilled carpenter nails sturdier walls).</summary>
        public float CarpentryHealthMult
            => 1f + GetLevel(Skill.Carpentry) * CloneZConfig.CarpentryHealthPerLevel;

        /// <summary>Nutrition multiplier a cook wrings from a cooked dish (higher Cooking = more filling meals).</summary>
        public float CookingNutritionMult
            => 1f + GetLevel(Skill.Cooking) * CloneZConfig.CookingHungerPerLevel;

        /// <summary>Harvest-yield multiplier (a skilled farmer pulls more crops from each plot).</summary>
        public float FarmingYieldMult
            => 1f + GetLevel(Skill.Farming) * CloneZConfig.FarmYieldPerLevel;

        /// <summary>Melee damage multiplier from the weapon's own skill plus raw Strength.</summary>
        public float MeleeDamageMult(Skill weaponSkill)
            => (1f + GetLevel(weaponSkill) * CloneZConfig.WeaponLevelDamageIncrement)
             * (1f + GetLevel(Skill.Strength) * CloneZConfig.StrengthMeleePerLevel);

        /// <summary>Incoming-damage multiplier (Fitness + Nimble toughen the body, PZ injury reduction).</summary>
        public float IncomingDamageMult
            => Mathf.Max(0.65f, 1f - 0.03f * GetLevel(Skill.Fitness) - 0.02f * GetLevel(Skill.Nimble));

        /// <summary>How much a zombie's sight radius shrinks toward this player while crouched.</summary>
        public float StealthVisionMult(bool sneaking) => sneaking ? SneakTable[GetLevel(Skill.Sneak)] : 1f;

        // ---- The Zone skills (Tarkov-style physical skills plus the trades) ----
        /// <summary>Stamina drain multiplier (Endurance: -3% per level).</summary>
        public float EnduranceDrainMult => 1f - GetLevel(Skill.Endurance) * 0.03f;
        /// <summary>Bleeding multiplier (Vitality: -4% per level).</summary>
        public float VitalityBleedMult => 1f - GetLevel(Skill.Vitality) * 0.04f;
        /// <summary>Healing multiplier (Vitality: +5% per level).</summary>
        public float VitalityHealMult => 1f + GetLevel(Skill.Vitality) * 0.05f;
        /// <summary>Hunger and thirst multiplier (Metabolism: -3% per level).</summary>
        public float MetabolismMult => 1f - GetLevel(Skill.Metabolism) * 0.03f;
        /// <summary>How much more every medical item does (First Aid: +8% per level).</summary>
        public float FirstAidMult => 1f + GetLevel(Skill.FirstAid) * 0.08f;
        /// <summary>Repair efficiency and vehicle work speed (Mechanics: +6% per level).</summary>
        public float MechanicsMult => 1f + GetLevel(Skill.Mechanics) * 0.06f;
        /// <summary>Foraging finds multiplier.</summary>
        public float ForagingMult => 1f + GetLevel(Skill.Foraging) * 0.12f;
        /// <summary>Fishing bite chance multiplier.</summary>
        public float FishingMult => 1f + GetLevel(Skill.Fishing) * 0.12f;
        /// <summary>Trap catch chance multiplier.</summary>
        public float TrappingMult => 1f + GetLevel(Skill.Trapping) * 0.12f;
        /// <summary>Detector and hearing range multiplier (Perception: +4% per level).</summary>
        public float PerceptionMult => 1f + GetLevel(Skill.Perception) * 0.04f;
        /// <summary>Trader price multiplier (Charisma: -1% per level when buying).</summary>
        public float CharismaPriceMult => 1f - GetLevel(Skill.Charisma) * 0.01f;

        /// <summary>Maps an equipped melee weapon to the skill it trains (bare hands = Blunt).</summary>
        public static Skill WeaponSkillFor(ItemDef weapon) => weapon != null ? weapon.WeaponSkill : Skill.Blunt;
        #endregion
    }
}
