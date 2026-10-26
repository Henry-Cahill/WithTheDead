using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// DayZ's illnesses, each with its own cause and its own cure — survival needs that interact rather than a list of
    /// bars. Cholera comes from tainted water (purify or boil it) and drains you through thirst until tetracycline
    /// clears it; a cold comes from being wet and cold for too long, makes you sneeze (the dead hear it) and goes
    /// with vitamins, warmth and rest; food poisoning (the stomach's own system) is settled by charcoal; a wound
    /// infection (the wounds' own system) by antibiotics. Illnesses come out of the raid with you.
    /// </summary>
    public sealed class PlayerDiseases : MonoBehaviour, IStatModSource
    {
        public const string Cholera = "cholera", Cold = "cold";

        private readonly HashSet<string> _active = new HashSet<string>();
        private PlayerStats _stats;
        private PlayerFoodSickness _food;
        private PlayerWounds _wounds;
        private float _coldExposure, _sneezeAt, _vitaminsUntil, _tick;
        private SandboxSettings _sandboxSettings;

        /// <summary>
        /// Returns true if the specified disease is currently active on the player.
        /// </summary>
        public bool Has(string d) => _active.Contains(d);
        /// <summary>
        /// Gets an enumerable of all active disease identifiers on the player.
        /// </summary>
        public IEnumerable<string> Active => _active;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _food = GetComponent<PlayerFoodSickness>();
            _wounds = GetComponent<PlayerWounds>();
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null && c.Health != null && !GameSession.IsBorrowedBody)
                foreach (string d in c.Health.Diseases) _active.Add(d);
            _sandboxSettings = SandboxSettings.Current;
            PlayerFoodSickness.DrankTainted += OnDrankTainted;
            GameEvents.RaidEnded += OnRaidEnded;
        }

        private void OnDestroy()
        {
            PlayerFoodSickness.DrankTainted -= OnDrankTainted;
            GameEvents.RaidEnded -= OnRaidEnded;
        }

        private void OnDrankTainted(PlayerFoodSickness who)
        {
            if (who != _food || !_sandboxSettings.Diseases) return;
            if (Random.value < 0.3f) Catch(Cholera, "Your gut cramps. Something in that water.");
        }

        // Save the player's disease list when a raid ends, persisting it to the profile
        /// <summary>
        /// Called when a raid ends to persist the player's disease list to the profile.
        /// </summary>
        private void OnRaidEnded(RaidOutcome o, string exit)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || GameSession.IsBorrowedBody) return;
            if (c.Health == null) c.Health = new HealthCarry();
            c.Health.Diseases.Clear();
            c.Health.Diseases.AddRange(_active);
        }

        /// <summary>
        /// Registers that the player has contracted the given disease and shows a toast message.
        /// </summary>
        public void Catch(string disease, string message)
        {
            if (!_active.Add(disease)) return;
            GameEvents.RaiseToast(message);
        }

        /// <summary>
        /// Attempts to cure the given disease, removing it from active list and showing a toast message.
        /// </summary>
        public bool Cure(string disease, string message)
        {
            if (!_active.Remove(disease)) return false;
            GameEvents.RaiseToast(message);
            return true;
        }

        private void Update()
        {
            if (_stats == null || _stats.IsDead || HubRules.SafeZone) return;
            float dt = Time.deltaTime;

            // Catching a cold: wet and cold for a few minutes, unless vitamins are keeping it off.
            if (_sandboxSettings.Diseases && !Has(Cold) && Time.time > _vitaminsUntil)
            {
                bool chilled = _stats.ColdStrength > 0.35f && _stats.Wetness > 0.3f;
                _coldExposure = Mathf.Max(0f, _coldExposure + (chilled ? dt : -dt * 0.5f));
                if (_coldExposure > 150f) { _coldExposure = 0f; if (Random.value < 0.6f) Catch(Cold, "You've caught a cold. Sniffles — and sneezes."); }
            }

            if (Has(Cold) && Time.time > _sneezeAt)
            {
                _sneezeAt = Time.time + Random.Range(25f, 70f);
                bool sneeze = Random.value < 0.6f;
                Sfx.Play(sneeze ? SfxIds.Sneeze : SfxIds.Cough, transform.position + Vector3.up * 1.6f, 0.8f, Random.Range(0.9f, 1.1f), 28f);
                NoiseSystem.Emit(transform.position, sneeze ? 11f : 8f);
                // Warm, dry and rested, a cold runs its course.
                if (_stats.ColdStrength < 0.1f && _stats.Wetness < 0.1f && Random.value < 0.15f) Cure(Cold, "Your cold has cleared up.");
            }

            _tick -= dt;
            if (_tick > 0f) return;
            _tick = 1f;
            if (Has(Cholera))
            {
                _stats.Thirst = Mathf.Clamp01(_stats.Thirst + 0.004f);
                _stats.Sickness = Mathf.Min(CloneZConfig.SicknessMax, _stats.Sickness + 0.6f);
                _stats.EnvironmentDrain(0.03f, "died of cholera");
            }
        }

        /// <summary>
        /// Adjusts the given StatMods based on active diseases (e.g., cholera increases thirst rate).
        /// </summary>
        public void ApplyMods(StatMods mods)
        {
            if (Has(Cholera)) { mods.ThirstRate *= 2.5f; mods.StaminaRegen *= 0.7f; }
            if (Has(Cold)) { mods.StaminaRegen *= 0.85f; mods.NoiseMult *= 1.05f; mods.ColdResist -= 0.1f; }
        }

        /// <summary>
        /// Starts a vitamins timer that keeps colds from being caught for 1200 s (twenty minutes of Time.time).
        /// </summary>
        public void TakeVitamins() => _vitaminsUntil = Time.time + 1200f;

        /// <summary>
        /// Registers disease‑related item actions (tetracycline, charcoal, vitamins).
        /// </summary>
        internal static void RegisterItems()
        {
            // Register antibiotic use: cures cholera and optionally treats wound infections.
            ItemActions.RegisterUse("tetracycline", (inv, s) =>
            {
                // Get the disease component from the inventory holder.
                var d = inv.GetComponent<PlayerDiseases>();
                // Attempt to cure cholera; record if any cure succeeded.
                bool did = d != null && d.Cure(Cholera, "The cramps ease. The antibiotics are working.");
                // Also try to treat a wound infection if present.
                var w = inv.GetComponent<PlayerWounds>();
                if (w != null && w.TreatInfection()) did = true;
                // If nothing was cured, inform the player and exit early.
                if (!did) { GameEvents.RaiseToast("You've nothing for antibiotics to treat."); return true; }
                // Remove one antibiotic item from inventory after successful cure.
                inv.Inventory.Remove(s, 1);
                // Play UI sound for taking medicine.
                Sfx.PlayUI(SfxIds.Eat, 0.5f);
                return true;
            });
            // Register charcoal use: reduces stomach sickness severity.
            ItemActions.RegisterUse("charcoal", (inv, s) =>
            {
                // Get player stats to check for sickness level.
                var st = inv.GetComponent<PlayerStats>();
                // If no stats or sickness is low, notify player that charcoal is unnecessary.
                if (st == null || st.Sickness < 5f) { GameEvents.RaiseToast("Your stomach is fine."); return true; }
                // Reduce sickness value, clamped to zero.
                st.Sickness = Mathf.Max(0f, st.Sickness - 50f);
                // Remove one charcoal item.
                inv.Inventory.Remove(s, 1);
                // Notify player that charcoal has settled the stomach.
                GameEvents.RaiseToast("The charcoal settles your stomach.");
                return true;
            });
            // Register vitamins use: starts vitamins timer and can cure a cold.
            ItemActions.RegisterUse("vitamins", (inv, s) =>
            {
                // Get disease component to apply vitamin effects.
                var d = inv.GetComponent<PlayerDiseases>();
                if (d == null) return false;
                // Start vitamins timer to prevent catching colds.
                d.TakeVitamins();
                // Also cure any active cold.
                d.Cure(Cold, "You feel the cold lifting.");
                // Consume one vitamins item.
                inv.Inventory.Remove(s, 1);
                // Play UI sound for taking vitamins.
                Sfx.PlayUI(SfxIds.Eat, 0.4f);
                // Notify player of vitamin effect.
                GameEvents.RaiseToast("Vitamins: colds keep away for a while.");
                return true;
            });
        }
    }

    /// <summary>
    /// The module that registers the disease system and ensures each player gets a PlayerDiseases component.
    /// </summary>
    public sealed class DiseaseModule : IGameModule
    {
        public int Order => 61;

        public void Init()
        {
            PlayerDiseases.RegisterItems();
            WorldHooks.PlayerBuilt += p =>
            {
                if (p != null && p.GetComponent<PlayerDiseases>() == null) p.gameObject.AddComponent<PlayerDiseases>();
            };
        }
    }
}
