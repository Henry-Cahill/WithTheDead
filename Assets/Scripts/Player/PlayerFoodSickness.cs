using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Food poisoning — Project Zomboid's Sickness / Nausea moodle. Eating spoiled
    /// food (stale or rotten) or raw-hazardous ingredients (raw meat, chicken) turns
    /// the stomach: it raises the Sickness stat, which blocks the body from healing,
    /// slows a badly-ill survivor to a queasy trudge and, at its worst, saps health.
    /// It fades on its own over time — faster while resting.
    ///
    /// Sickness lives on <see cref="PlayerStats.Sickness"/> (0..100); this component
    /// owns it, mirroring how <see cref="PlayerWounds"/> owns Pain, <see cref="PlayerPanic"/>
    /// owns Panic and <see cref="PlayerTemperature"/> owns Temperature/Wetness.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerFoodSickness : MonoBehaviour
    {
        private PlayerStats _stats;
        private PlayerSleep _sleepCache;
        // Cached config values to avoid static lookups each frame
        private float _decayPerSec;
        private float _sleepMult;
        private float _drainThreshold;
        private float _maxSickness;
        private float _healthDrainPerSec;

        /// <summary>Nausea moodle level 0..4 (Fine / Queasy / Nauseous / Sick / Ill).</summary>
        public int Level
        {
            get
            {
                float s = _stats != null ? _stats.Sickness : 0f;
                if (s >= CloneZConfig.SicknessLevel4) return 4;
                if (s >= CloneZConfig.SicknessLevel3) return 3;
                if (s >= CloneZConfig.SicknessLevel2) return 2;
                if (s >= CloneZConfig.SicknessLevel1) return 1;
                return 0;
            }
        }

        /// <summary>A short word for the current stomach state (for the HUD / health panel).</summary>
        public string Descriptor
        {
            get
            {
                switch (Level)
                {
                    case 4: return "Violently ill";
                    case 3: return "Sick";
                    case 2: return "Nauseous";
                    case 1: return "Queasy";
                    default: return "Fine";
                }
            }
        }

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _sleepCache = GetComponent<PlayerSleep>();
            // Populate cached static config values once
            _decayPerSec = CloneZConfig.SicknessDecayPerSec;
            _sleepMult = CloneZConfig.SicknessSleepMult;
            _drainThreshold = CloneZConfig.SicknessDrainThreshold;
            _maxSickness = CloneZConfig.SicknessMax;
            _healthDrainPerSec = CloneZConfig.SicknessHealthDrainPerSec;
        }

        private void Update()
        {
            if (_stats == null || _stats.IsDead) return;
            float dt = Time.deltaTime;

            // The illness runs its course over time; sleeping it off is quicker.
            float decay = _decayPerSec;
            if (_sleepCache == null) _sleepCache = GetComponent<PlayerSleep>();   // PlayerSleep is added after this component
            if (_sleepCache != null && _sleepCache.IsSleeping) decay *= _sleepMult;
            _stats.Sickness = Mathf.Max(0f, _stats.Sickness - decay * dt);

            // A stomach in real distress bleeds a little health while it lasts.
            if (_stats.Sickness >= _drainThreshold)
            {
                float severity = Mathf.InverseLerp(_drainThreshold, _maxSickness, _stats.Sickness);
                _stats.EnvironmentDrain(_healthDrainPerSec * severity * dt, "died of food poisoning");
            }
        }

        /// <summary>Called by <see cref="PlayerInventory"/> when a food item is eaten. Spoiled or raw
        /// food may turn the stomach; fresh, cooked food is safe. The gain is rolled so a single bad
        /// bite isn't always a guarantee — but rotten food nearly always is.</summary>
        public void OnAte(ItemDef def, FoodState state)
        {
            if (_stats == null || _stats.IsDead || def == null) return;

            float chance = 0f;
            float gain = 0f;

            if (state == FoodState.Rotten)
            {
                chance = CloneZConfig.FoodRottenSickChance;
                gain = CloneZConfig.SicknessRottenGain;
            }
            else if (def.RawHazard)   // raw meat/chicken sicken you even when perfectly fresh
            {
                chance = CloneZConfig.FoodRawSickChance;
                gain = CloneZConfig.SicknessRawGain;
            }
            else if (state == FoodState.Stale)
            {
                chance = CloneZConfig.FoodStaleSickChance;
                gain = CloneZConfig.SicknessStaleGain;
            }

            if (gain <= 0f) return;
            if (Random.value > chance) return;   // got away with it this time
            _stats.Sickness = Mathf.Clamp(_stats.Sickness + gain, 0f, CloneZConfig.SicknessMax);
        }

        /// <summary>Called by <see cref="PlayerInventory"/> / <see cref="PlayerHydration"/> when the survivor
        /// drinks tainted (rain / barrel) water raw. Boiling it first avoids this entirely.</summary>
        public void OnDrankTainted()
        {
            if (_stats == null || _stats.IsDead) return;
            DrankTainted?.Invoke(this);   // every raw drink risks cholera, whether or not the stomach turns
            if (Random.value > CloneZConfig.WaterTaintedSickChance) return;   // got away with it this time
            _stats.Sickness = Mathf.Clamp(_stats.Sickness + CloneZConfig.SicknessTaintedGain, 0f, CloneZConfig.SicknessMax);
        }

        /// <summary>Raised whenever the survivor drinks tainted water raw (cholera rides in on it).</summary>
        public static event System.Action<PlayerFoodSickness> DrankTainted;
    }
}
