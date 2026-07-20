using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The survivor's thermoregulation — Project Zomboid's body-temperature and
    /// wetness model. Core warmth drifts toward an ambient target set by the time of
    /// day (warm afternoons, cold nights), the weather (rain and cloud chill you) and
    /// what's worn (clothing insulates). Rain soaks you; soaked clothes stop keeping
    /// you warm, so a downpour after dark is genuinely dangerous. Straying too far
    /// from comfortable brings the cold/heat moodles, and the extremes sap health.
    ///
    /// Temperature and Wetness live on <see cref="PlayerStats"/> (0..1); this
    /// component owns them, mirroring how <see cref="PlayerWounds"/> owns Pain and
    /// <see cref="PlayerPanic"/> owns Panic.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerTemperature : MonoBehaviour
    {
        private PlayerStats _stats;
        private PlayerClothing _clothing;
        private PlayerSleep _sleep;

        private PlayerClothing Clothing => _clothing != null ? _clothing : (_clothing = GetComponent<PlayerClothing>());
        private PlayerSleep Sleep => _sleep != null ? _sleep : (_sleep = GetComponent<PlayerSleep>());

        public float Temperature => _stats != null ? _stats.Temperature : CloneZConfig.TempStart;
        public float Wetness => _stats != null ? _stats.Wetness : 0f;

        /// <summary>Thermal comfort band as a level: -2 freezing, -1 cold, 0 comfortable, 1 warm, 2 hot.</summary>
        public int Level
        {
            get
            {
                float t = Temperature;
                if (t < CloneZConfig.TempHypothermiaThreshold + 0.02f) return -2;
                if (t < CloneZConfig.TempComfortLow) return -1;
                if (t > CloneZConfig.TempHyperthermiaThreshold - 0.01f) return 2;
                if (t > CloneZConfig.TempComfortHigh) return 1;
                return 0;
            }
        }

        /// <summary>A short word for the current warmth (for the HUD / health panel).</summary>
        public string Descriptor
        {
            get
            {
                switch (Level)
                {
                    case -2: return "Freezing";
                    case -1: return "Cold";
                    case 1: return "Warm";
                    case 2: return "Hot";
                    default: return "Comfortable";
                }
            }
        }

        /// <summary>A short word for how wet the survivor is.</summary>
        public string WetDescriptor
        {
            get
            {
                float w = Wetness;
                if (w >= 0.8f) return "Soaked";
                if (w >= 0.55f) return "Wet";
                if (w >= CloneZConfig.WetnessDampThreshold) return "Damp";
                return "Dry";
            }
        }

        private void Awake() => _stats = GetComponent<PlayerStats>();

        private void Update()
        {
            if (_stats == null || _stats.IsDead) return;
            float dt = Time.deltaTime;
            UpdateWetness(dt);
            UpdateTemperature(dt);
            ApplyEffects(dt);
        }

        // Rain soaks you (a raincoat keeps most of it off); once out of the rain you dry off,
        // faster when you're warm. Sleeping in a bed counts as being under a roof.
        private void UpdateWetness(float dt)
        {
            float rain = WeatherSystem.Instance != null ? WeatherSystem.Instance.RainIntensity : 0f;
            bool sheltered = Sleep != null && Sleep.IsSleeping;

            if (rain > 0.01f && !sheltered)
            {
                float resist = Clothing != null ? Clothing.WaterResistance() : 0f;
                float gain = CloneZConfig.WetRainPerSec * rain * (1f - CloneZConfig.WetWaterResistFactor * resist);
                _stats.Wetness = Mathf.Clamp01(_stats.Wetness + gain * dt);
            }
            else
            {
                float warmthBoost = 1f + Mathf.Clamp01(_stats.Temperature - 0.5f) * 2f;
                _stats.Wetness = Mathf.Clamp01(_stats.Wetness - CloneZConfig.WetDryPerSec * warmthBoost * dt);
            }
        }

        // Body warmth eases toward the ambient target (day/weather) plus clothing, minus the
        // chill of being wet, plus a shelter bonus while sleeping.
        private void UpdateTemperature(float dt)
        {
            float clothingWarmth = Clothing != null ? Clothing.TotalInsulation(_stats.Wetness) : 0f;
            float shelter = (Sleep != null && Sleep.IsSleeping) ? CloneZConfig.TempShelterWarmth : 0f;

            float target = AmbientTemperature()
                + clothingWarmth
                - _stats.Wetness * CloneZConfig.TempWetChill
                + shelter;
            target = Mathf.Clamp01(target);

            _stats.Temperature = Mathf.MoveTowards(_stats.Temperature, target, CloneZConfig.TempAdjustPerSec * dt);
        }

        // The naked, dry ambient a body would settle at: warm in the early afternoon, cold before
        // dawn, colder still under rain and heavy cloud.
        private float AmbientTemperature()
        {
            float hour = TimeOfDay.Instance != null ? TimeOfDay.Instance.Hour : 12f;
            float dayFactor = Mathf.Cos((hour - 14f) / 24f * Mathf.PI * 2f);   // +1 at 14:00, -1 at 02:00
            float ambient = CloneZConfig.TempNeutral + CloneZConfig.TempDaySwing * dayFactor;

            WeatherSystem w = WeatherSystem.Instance;
            if (w != null)
            {
                ambient -= CloneZConfig.TempRainChill * w.RainIntensity;
                ambient -= CloneZConfig.TempCloudChill * w.Cloudiness;
                ambient -= CloneZConfig.TempFogChill * w.FogIntensity;
            }
            return ambient;
        }

        // The cold tires and eventually freezes you; the heat parches and eventually cooks you.
        private void ApplyEffects(float dt)
        {
            float cold = _stats.ColdStrength;
            float hot = _stats.HeatStrength;

            if (_stats.Temperature < CloneZConfig.TempHypothermiaThreshold)
                _stats.EnvironmentDrain(CloneZConfig.TempHypothermiaHealthDrain * cold * dt, "froze to death");
            else if (_stats.Temperature > CloneZConfig.TempHyperthermiaThreshold)
                _stats.EnvironmentDrain(CloneZConfig.TempHyperthermiaHealthDrain * hot * dt, "succumbed to the heat");

            if (cold > 0f)
                _stats.Fatigue = Mathf.Clamp01(_stats.Fatigue + CloneZConfig.TempColdFatiguePerSec * cold * dt);
            if (hot > 0f)
                _stats.Thirst = Mathf.Clamp01(_stats.Thirst + CloneZConfig.TempHotThirstPerSec * hot * dt);
        }
    }
}
