using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The survivor's thermoregulation — Project Zomboid's body-temperature and
    /// wetness model. Core warmth drifts toward an ambient target set by the outdoor
    /// air (<see cref="Climate.AirTemperatureC"/>: warm afternoons, cold nights, rain
    /// and cloud chill it), what's worn (clothing insulates) and shelter (a building
    /// keeps out the wind and snow and holds some warmth). Rain soaks you, snow far more
    /// slowly; soaked clothes stop keeping you warm, so a downpour after dark is genuinely
    /// dangerous. Straying too far from comfortable brings the cold/heat moodles, and
    /// the extremes sap health.
    ///
    /// The air temperature maps onto the 0..1 warmth scale linearly at the mild climate's
    /// rate (so the wave arena plays exactly as it always did) and more steeply below
    /// <see cref="ColdKneeC"/>, where real cold bites harder. On a winter raid the body
    /// cools on a human timescale (<see cref="WinterResponseSeconds"/>): a survivor in a
    /// beanie, jacket and boots grows cold over minutes but holds out the night, while a
    /// T-shirt alone is freezing within ten.
    ///
    /// Temperature and Wetness live on <see cref="PlayerStats"/> (0..1); this
    /// component owns them, mirroring how <see cref="PlayerWounds"/> owns Pain and
    /// <see cref="PlayerPanic"/> owns Panic.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerTemperature : MonoBehaviour
    {
        /// <summary>Below this air temperature (°C) every further degree chills the body harder. Kept
        /// under the mild climate's coldest air (a storm night, ~6 °C) so the wave arena is unchanged.</summary>
        public const float ColdKneeC = 4f;
        /// <summary>Extra warmth lost per °C below <see cref="ColdKneeC"/>, on top of <see cref="Climate.BodyScalePerC"/>.</summary>
        public const float ColdExtraPerC = 0.0145f;
        /// <summary>Winter: seconds for the body to close ~63% of the gap to its target warmth
        /// (Newton's cooling). The brisk wave arena keeps its fast linear drift.</summary>
        public const float WinterResponseSeconds = 360f;

        private PlayerStats _stats;
        private WeatherSystem _weather;
        private TimeOfDay _timeOfDay;
        private PlayerClothing _clothing;
        private PlayerSleep _sleep;
        // Cached temperature level and descriptor to avoid per-frame computation
        private int _levelCache;
        private string _descriptorCache;

        private PlayerClothing Clothing => _clothing != null ? _clothing : (_clothing = GetComponent<PlayerClothing>());
        private PlayerSleep Sleep => _sleep != null ? _sleep : (_sleep = GetComponent<PlayerSleep>());

        public float Temperature => _stats != null ? _stats.Temperature : CloneZConfig.TempStart;
        public float Wetness => _stats != null ? _stats.Wetness : 0f;

        /// <summary>The warmth the body is drifting toward right now (normally 0..1, but may fall below 0 or rise above 1 when extreme weather applies).</summary>
        public float Target { get; private set; } = CloneZConfig.TempStart;

        /// <summary>True while the survivor is inside a building (<see cref="Shelter"/>): out of the
        /// wind, rain and snow, and warmer.</summary>
        public bool Indoors { get; private set; }

        /// <summary>The naked, dry ambient warmth (0..1 scale, may fall below 0) a body would settle at in
        /// air of <paramref name="airC"/> °C. Linear at the mild climate's rate — the old
        /// TempNeutral ± TempDaySwing over 19 ± 7 °C — and steeper below <see cref="ColdKneeC"/>.</summary>
        public static float AmbientFor(float airC)
        {
            float ambient = CloneZConfig.TempNeutral + (airC - Climate.MildMeanC) * Climate.BodyScalePerC;
            if (airC < ColdKneeC) ambient -= (ColdKneeC - airC) * ColdExtraPerC;
            return ambient;
        }

        /// <summary>Thermal comfort band as a level: -2 freezing, -1 cold, 0 comfortable, 1 warm, 2 hot.</summary>
        public int Level
        {
            get => _levelCache;
        }

        /// <summary>A short word for the current warmth (for the HUD / health panel).</summary>
        public string Descriptor
        {
            get => _descriptorCache;
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

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _weather = WeatherSystem.Instance;
            _timeOfDay = TimeOfDay.Instance;
            // Initialize cached level and descriptor based on initial temperature
            UpdateLevelCache();
        }

        private void Update()
        {
            if (_stats == null || _stats.IsDead) return;
            float dt = Time.deltaTime;
            // Safety: WeatherSystem may have been instantiated after Awake; re‑acquire if still null
            if (_weather == null) _weather = WeatherSystem.Instance;
            UpdateWetness(dt);
            UpdateTemperature(dt);
            ApplyEffects(dt);
        }

        // Rain soaks you (a raincoat keeps most of it off), snow only slowly; under a roof — inside a
        // building, or asleep in a bed — you dry off instead, faster when you're warm.
        private void UpdateWetness(float dt)
        {
            WeatherSystem w = _weather;
            float rain = w != null ? w.RainIntensity : 0f;
            float snow = w != null ? w.SnowfallIntensity : 0f;
            bool sheltered = Indoors || (Sleep != null && Sleep.IsSleeping);

            if ((rain > 0.01f || snow > 0.01f) && !sheltered)
            {
                float resist = Clothing != null ? Clothing.WaterResistance() : 0f;
                // WettingIntensity already weighs rain in full and snow far less (Climate.SnowWetFactor).
                float gain = CloneZConfig.WetRainPerSec * w.WettingIntensity * (1f - CloneZConfig.WetWaterResistFactor * resist);
                _stats.Wetness = Mathf.Clamp01(_stats.Wetness + gain * dt);
            }
            else
            {
                float warmthBoost = 1f + Mathf.Clamp01(_stats.Temperature - 0.5f) * 2f;
                _stats.Wetness = Mathf.Clamp01(_stats.Wetness - CloneZConfig.WetDryPerSec * warmthBoost * dt);
            }
        }

        // Body warmth eases toward the ambient target (air temperature / shelter / rain) plus clothing,
        // minus the chill of being wet, plus a shelter bonus while sleeping.
        private Vector3 _lastWarmthPos;
    private float _cachedWarmth;

    private void UpdateTemperature(float dt)
    {
        // Update cached warmth from HomeComfort only when the player moves significantly.
        Vector3 curPos = transform.position;
        if ((curPos - _lastWarmthPos).sqrMagnitude > 0.0001f) // movement threshold (~0.01 units)
        {
            _cachedWarmth = HomeComfort.WarmthAt(curPos);
            _lastWarmthPos = curPos;
        }

        float clothingWarmth = Clothing != null ? Clothing.TotalInsulation(_stats.Wetness) : 0f;
        float shelter = (Sleep != null && Sleep.IsSleeping) ? CloneZConfig.TempShelterWarmth : 0f;

        float target = AmbientTemperature()
            + clothingWarmth
            - _stats.Wetness * CloneZConfig.TempWetChill
            + shelter
            + _cachedWarmth;   // cached warmth source
        // Do not clamp – the target can be negative (freezing) or >1 (overheating)
        Target = target;

        if (Climate.Winter)
            _stats.Temperature = Mathf.Lerp(_stats.Temperature, target, 1f - Mathf.Exp(-dt / WinterResponseSeconds));
        else
            _stats.Temperature = Mathf.MoveTowards(_stats.Temperature, target, CloneZConfig.TempAdjustPerSec * dt);

        // Update cached level and descriptor after temperature may have changed
        UpdateLevelCache();
    }

        // The naked, dry ambient a body would settle at: the air temperature on the warmth scale (the
        // milder air inside a building), may fall below 0 (extreme cold) or rise above 1 (extreme heat),
        // less the chill of rain beating down on someone out in it.
        private float AmbientTemperature()
        {
            float air = Climate.AirTemperatureC;
            if (Indoors) air = Climate.IndoorAirC(air);
            float ambient = AmbientFor(air);

            WeatherSystem w = _weather;
            if (w != null && !Indoors) ambient -= Climate.RainExposureChill * w.RainIntensity;
            return ambient;
        }

        // Computes and caches the temperature level and descriptor string based on current temperature
        private void UpdateLevelCache()
        {
            float t = Temperature;
            int level;
            if (t < CloneZConfig.TempHypothermiaThreshold + 0.02f) level = -2;
            else if (t < CloneZConfig.TempComfortLow) level = -1;
            else if (t > CloneZConfig.TempHyperthermiaThreshold - 0.01f) level = 2;
            else if (t > CloneZConfig.TempComfortHigh) level = 1;
            else level = 0;

            _levelCache = level;
            switch (level)
            {
                case -2: _descriptorCache = "Freezing"; break;
                case -1: _descriptorCache = "Cold"; break;
                case 1: _descriptorCache = "Warm"; break;
                case 2: _descriptorCache = "Hot"; break;
                default: _descriptorCache = "Comfortable"; break;
            }
        }
        private void ApplyEffects(float dt)
        {
            float cold = _stats.ColdStrength;
            float hot = _stats.HeatStrength;

            if (_stats.Temperature < CloneZConfig.TempHypothermiaThreshold)
                _stats.EnvironmentDrain(CloneZConfig.TempHypothermiaHealthDrain * cold * dt, "froze to death");
            else if (_stats.Temperature > CloneZConfig.TempHyperthermiaThreshold)
                _stats.EnvironmentDrain(CloneZConfig.TempHyperthermiaHealthDrain * hot * dt, "succumbed to the heat");

            TimeOfDay clock = _timeOfDay;
            float pace = clock != null ? Mathf.Min(1f, CloneZConfig.DayLengthSeconds / clock.DayLengthSeconds) : 1f;
            if (cold > 0f)
                _stats.Fatigue = Mathf.Clamp01(_stats.Fatigue + CloneZConfig.TempColdFatiguePerSec * cold * pace * dt);
            if (hot > 0f)
                _stats.Thirst = Mathf.Clamp01(_stats.Thirst + CloneZConfig.TempHotThirstPerSec * hot * pace * dt);
        }
    }
}
