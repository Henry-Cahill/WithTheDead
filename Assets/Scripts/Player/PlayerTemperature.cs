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
        private PlayerClothing _clothing;
        private PlayerSleep _sleep;

        private PlayerClothing Clothing => _clothing != null ? _clothing : (_clothing = GetComponent<PlayerClothing>());
        private PlayerSleep Sleep => _sleep != null ? _sleep : (_sleep = GetComponent<PlayerSleep>());

        public float Temperature => _stats != null ? _stats.Temperature : CloneZConfig.TempStart;
        public float Wetness => _stats != null ? _stats.Wetness : 0f;

        /// <summary>The warmth the body is drifting toward right now (0..1, same scale as <see cref="Temperature"/>).</summary>
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
            // Probed at chest height so a floor-level interior box still counts the survivor as inside.
            Indoors = Shelter.Count > 0 && Shelter.IsSheltered(transform.position + Vector3.up);
            UpdateWetness(dt);
            UpdateTemperature(dt);
            ApplyEffects(dt);
        }

        // Rain soaks you (a raincoat keeps most of it off), snow only slowly; under a roof — inside a
        // building, or asleep in a bed — you dry off instead, faster when you're warm.
        private void UpdateWetness(float dt)
        {
            WeatherSystem w = WeatherSystem.Instance;
            float rain = w != null ? w.RainIntensity : 0f;
            float snow = w != null ? w.SnowfallIntensity : 0f;
            bool sheltered = Indoors || (Sleep != null && Sleep.IsSleeping);

            if ((rain > 0.01f || snow > 0.01f) && !sheltered)
            {
                float resist = Clothing != null ? Clothing.WaterResistance() : 0f;
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
        private void UpdateTemperature(float dt)
        {
            float clothingWarmth = Clothing != null ? Clothing.TotalInsulation(_stats.Wetness) : 0f;
            float shelter = (Sleep != null && Sleep.IsSleeping) ? CloneZConfig.TempShelterWarmth : 0f;

            float target = AmbientTemperature()
                + clothingWarmth
                - _stats.Wetness * CloneZConfig.TempWetChill
                + shelter
                + HomeComfort.WarmthAt(transform.position);   // a lit burner, a hot shower close by
            target = Mathf.Clamp01(target);
            Target = target;

            if (Climate.Winter)
                _stats.Temperature = Mathf.Lerp(_stats.Temperature, target, 1f - Mathf.Exp(-dt / WinterResponseSeconds));
            else
                _stats.Temperature = Mathf.MoveTowards(_stats.Temperature, target, CloneZConfig.TempAdjustPerSec * dt);
        }

        // The naked, dry ambient a body would settle at: the air temperature on the warmth scale (the
        // milder air inside a building), less the chill of rain beating down on someone out in it.
        private float AmbientTemperature()
        {
            float air = Climate.AirTemperatureC;
            if (Indoors) air = Climate.IndoorAirC(air);
            float ambient = AmbientFor(air);

            WeatherSystem w = WeatherSystem.Instance;
            if (w != null && !Indoors) ambient -= Climate.RainExposureChill * w.RainIntensity;
            return ambient;
        }

        // The cold tires and eventually freezes you; the heat parches and eventually cooks you. Shivering
        // and sweating play out over in-game hours, so they slow with the clock (a raid's night runs 12x
        // slower than the wave arena's day); the killing extremes stay in real time.
        private void ApplyEffects(float dt)
        {
            float cold = _stats.ColdStrength;
            float hot = _stats.HeatStrength;

            if (_stats.Temperature < CloneZConfig.TempHypothermiaThreshold)
                _stats.EnvironmentDrain(CloneZConfig.TempHypothermiaHealthDrain * cold * dt, "froze to death");
            else if (_stats.Temperature > CloneZConfig.TempHyperthermiaThreshold)
                _stats.EnvironmentDrain(CloneZConfig.TempHyperthermiaHealthDrain * hot * dt, "succumbed to the heat");

            TimeOfDay clock = TimeOfDay.Instance;
            float pace = clock != null ? Mathf.Min(1f, CloneZConfig.DayLengthSeconds / clock.DayLengthSeconds) : 1f;
            if (cold > 0f)
                _stats.Fatigue = Mathf.Clamp01(_stats.Fatigue + CloneZConfig.TempColdFatiguePerSec * cold * pace * dt);
            if (hot > 0f)
                _stats.Thirst = Mathf.Clamp01(_stats.Thirst + CloneZConfig.TempHotThirstPerSec * hot * pace * dt);
        }
    }
}
