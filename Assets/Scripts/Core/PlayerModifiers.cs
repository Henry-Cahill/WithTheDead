using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The survivor's combined stat modifiers for this frame — the sum of everything that bends the base
    /// rules: traits and profession, worn artifacts, Zone marks from long exposure, broken limbs, suit
    /// upgrades, diseases. Multipliers start at 1, additive terms at 0. Systems read
    /// <see cref="PlayerModifiers.Current"/> where they apply a rate, so a new source never has to touch them.
    /// </summary>
    public sealed class StatMods
    {
        public float MoveSpeed, SprintSpeed, CarryCapacity, StaminaRegen, StaminaDrain;
        public float HungerRate, ThirstRate, FatigueRate, BleedRate, HealRate, PanicRate;
        public float NoiseMult, VisibilityMult, AimSpread, Recoil, MeleeDamage, ReloadSpeed;
        public float XpMult, LootLuck, InfectionResist, PainResist, ColdResist;
        public float CarryBonusKg, RadiationPerSec, NightVision, MaxHealthBonus, RadiationPurge;
        public Resistances Resist;

        public StatMods() => Reset();

        /// <summary>Resets all stat modifiers to their neutral defaults.</summary>
        public void Reset()
        {
            MoveSpeed = SprintSpeed = CarryCapacity = StaminaRegen = StaminaDrain = 1f;
            HungerRate = ThirstRate = FatigueRate = BleedRate = HealRate = PanicRate = 1f;
            NoiseMult = VisibilityMult = AimSpread = Recoil = MeleeDamage = ReloadSpeed = 1f;
            XpMult = LootLuck = 1f;
            InfectionResist = PainResist = ColdResist = 0f;
            CarryBonusKg = RadiationPerSec = NightVision = MaxHealthBonus = RadiationPurge = 0f;
            Resist = default;
        }

        /// <summary>A one-line summary of the non-neutral modifiers (tooltips, the character sheet).</summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            void M(string n, float v) { if (Mathf.Abs(v - 1f) > 0.005f) sb.Append(n).Append(' ').Append(v > 1f ? "+" : "").Append(Mathf.RoundToInt((v - 1f) * 100f)).Append("%  "); }
            void A(string n, float v, string unit) { if (Mathf.Abs(v) > 0.0005f) sb.Append(n).Append(' ').Append(v > 0f ? "+" : "").Append(v.ToString("0.##")).Append(unit).Append("  "); }
            M("Move", MoveSpeed); M("Carry", CarryCapacity); M("Stamina regen", StaminaRegen); M("Stamina use", StaminaDrain);
            M("Hunger", HungerRate); M("Thirst", ThirstRate); M("Fatigue", FatigueRate); M("Bleeding", BleedRate);
            M("Healing", HealRate); M("Panic", PanicRate); M("Noise", NoiseMult); M("Visibility", VisibilityMult);
            M("Spread", AimSpread); M("Recoil", Recoil); M("Melee", MeleeDamage); M("Reload", ReloadSpeed); M("XP", XpMult);
            A("Carry", CarryBonusKg, " kg"); A("Radiation", RadiationPerSec, "/s"); A("Max HP", MaxHealthBonus, "");
            A("Bullet res", Resist.Bullet, ""); A("Rad res", Resist.Radiation, ""); A("Burn res", Resist.Burn, "");
            A("Shock res", Resist.Shock, ""); A("Chem res", Resist.Chemical, ""); A("Psi res", Resist.Psi, "");
            return sb.Length == 0 ? "No modifiers" : sb.ToString().TrimEnd();
        }
    }

    /// <summary>Something that bends the survivor's stats (a trait, a worn artifact, a broken leg).</summary>
    public interface IStatModSource
    {
                /// <summary>Applies this source's stat modifications to the given StatMods.</summary>
        void ApplyMods(StatMods mods);
    }

    /// <summary>
    /// Gathers every <see cref="IStatModSource"/> on (or registered with) the survivor into one
    /// <see cref="StatMods"/>, rebuilt each frame before the gameplay systems run.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class PlayerModifiers : MonoBehaviour
    {
        /// <summary>The local survivor's modifiers (neutral values when there is no survivor).</summary>
        public static StatMods Current => s_Instance != null ? s_Instance._mods : s_Neutral;
        private static readonly StatMods s_Neutral = new StatMods();
        private static PlayerModifiers s_Instance;

        private readonly StatMods _mods = new StatMods();
        private readonly List<IStatModSource> _external = new List<IStatModSource>();
        private readonly List<IStatModSource> _found = new List<IStatModSource>();
        private bool _dirty;

                /// <summary>Gets the current accumulated stat modifiers for the local survivor.</summary>
        public StatMods Mods => _mods;

        private void Awake() { s_Instance = this; GetComponents(_found); Recompute(); _dirty = true; }
        private void OnDestroy() { if (s_Instance == this) s_Instance = null; }

        /// <summary>Adds a source that doesn't live on the survivor's GameObject (a profile trait set).</summary>
        public void Register(IStatModSource src) { if (src != null && !_external.Contains(src)) _external.Add(src); _dirty = true; }
                /// <summary>Removes a previously registered stat-mod source.</summary>
        public void Unregister(IStatModSource src)
    {
        _external.Remove(src);
        _dirty = true;
    }

        private void Update() { if (_dirty) Recompute(); }

        private void Recompute()
        {
            _mods.Reset();

            for (int i = 0; i < _found.Count; i++) SafeApply(_found[i]);
            for (int i = 0; i < _external.Count; i++) SafeApply(_external[i]);
            _dirty = false;
        }

        private void SafeApply(IStatModSource s)
        {
            try { s.ApplyMods(_mods); }
            catch (System.Exception e) { Debug.LogException(e); }
        }
    }
}
