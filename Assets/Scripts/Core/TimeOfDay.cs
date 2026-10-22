using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Drives a continuous day/night cycle: advances a 24-hour clock, swings the
    /// directional "sun", and blends ambient light, fog tint and the camera sky
    /// from dawn through noon, dusk and deep night. Other systems read
    /// <see cref="LightLevel"/> (0 = pitch dark, 1 = full daylight) — the zombies
    /// use it to lose sight of the player after dark, Project-Zomboid style.
    /// </summary>
    public class TimeOfDay : MonoBehaviour
    {
        /// <summary>Singleton instance of the TimeOfDay system.</summary>
        public static TimeOfDay Instance { get; private set; }

        [Header("Assigned by the bootstrapper")]
        /// <summary>Reference to the sun directional light, set by the bootstrapper.</summary>
        public Light Sun;

        /// <summary>Current hour of the day, 0..24.</summary>
        public float Hour { get; private set; }
        /// <summary>Day counter, starting at 1.</summary>
        public int Day { get; private set; } = 1;
        /// <summary>Clock speed multiplier (boosted to fast-forward while sleeping).</summary>
        public float TimeScale { get; set; } = 1f;

        /// <summary>Real seconds per in-game day. The wave arena runs a brisk 12-minute day; a night
        /// raid runs slowly so the hours before dawn last a whole raid (see <see cref="Configure"/>).</summary>
        public float DayLengthSeconds { get; private set; } = CloneZConfig.DayLengthSeconds;

        /// <summary>The calendar year (Knox County, 1993).</summary>
        public const int Year = 1993;

        /// <summary>Start month for the calendar date (defaults to July).</summary>
        public int StartMonth { get; private set; } = 7;
        /// <summary>Day of month for the start date (defaults to 9).</summary>
        public int StartDayOfMonth { get; private set; } = 9;

        /// <summary>Scales the ambient light, fog and sky colour (1 = normal). Static so the vision /
        /// night-vision effects can darken or lift the night; reset whenever a clock is created.</summary>
        public static float AmbientScale = 1f;
        /// <summary>Scales the moonlight (the sun's night-time floor intensity). Reset like <see cref="AmbientScale"/>.</summary>
        public static float MoonlightScale = 1f;

        /// <summary>Total in-game hours elapsed since the world began (Day 1, StartHour = base).
        /// Used to age perishable food — see <see cref="ItemStack"/>.</summary>
        public float TotalHours => (Day - 1) * 24f + Hour;

        /// <summary>Convenience accessor for the current world time in hours (0 before the clock exists).</summary>
        public static float Now => Instance != null ? Instance.TotalHours : 0f;

        /// <summary>Perceived brightness 0..1 (sun elevation blended with the night floor and cloud cover).</summary>
        public float LightLevel { get; private set; } = 1f;
        /// <summary>True when it's considered night (light level below threshold).</summary>
        public bool IsNight => LightLevel < 0.35f;

        // Palette (kept out of the config since these are pure presentation).
        private static readonly Color DayAmbient = new Color(0.40f, 0.42f, 0.47f);
        private static readonly Color NightAmbient = new Color(0.07f, 0.08f, 0.13f);
        private static readonly Color DayFog = new Color(0.55f, 0.60f, 0.66f);
        private static readonly Color NightFog = new Color(0.04f, 0.05f, 0.08f);
        private static readonly Color DaySky = new Color(0.30f, 0.42f, 0.55f);
        private static readonly Color NightSky = new Color(0.02f, 0.03f, 0.06f);
        private static readonly Color NoonSun = new Color(1f, 0.96f, 0.86f);
        private static readonly Color DuskSun = new Color(1f, 0.62f, 0.36f);

        private WeatherSystem _weather;
        private Camera _cam;
        private string _clockTextCached;
        private string _clock12Cached;
        private bool _clockDirty = true;
        // Updates cached formatted time strings.
        private void UpdateClockCache()
        {
            int h = Mathf.FloorToInt(Hour) % 24;
            int m = Mathf.FloorToInt((Hour - Mathf.Floor(Hour)) * 60f);
            _clockTextCached = string.Format("{0:00}:{1:00}", h, m);

            int h12 = h % 12 == 0 ? 12 : h % 12;
            _clock12Cached = string.Format("{0:00}:{1:00}", h12, m);
            _clockDirty = false;
        }



        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Hour = CloneZConfig.StartHour;
            AmbientScale = 1f;
            MoonlightScale = 1f;
            // Cache frequently used references for per‑frame use.
            _cam = Camera.main;
            _weather = WeatherSystem.Instance;
        }

        /// <summary>Sets the clock up for a run: the starting hour, how many real seconds a day lasts
        /// and the calendar date of Day 1. Called by the bootstrapper right after the clock is created.</summary>
        public void Configure(float startHour, float dayLengthSeconds, int startMonth, int startDayOfMonth)
        {
            Day = 1;
            Hour = Mathf.Repeat(startHour, 24f);
            _clockDirty = true;
            DayLengthSeconds = Mathf.Max(1f, dayLengthSeconds);
            StartMonth = Mathf.Clamp(startMonth, 1, 12);
            StartDayOfMonth = Mathf.Clamp(startDayOfMonth, 1, System.DateTime.DaysInMonth(Year, StartMonth));
            Apply();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start() => _cam = Camera.main;

        private void Update()
        {
            Advance(Time.deltaTime);
            Apply();
        }

        private void Advance(float dt)
        {
            float hoursPerSecond = 24f / Mathf.Max(1f, DayLengthSeconds);
            Hour += hoursPerSecond * TimeScale * dt;
            while (Hour >= 24f) { Hour -= 24f; Day++; }
            _clockDirty = true;
        }

        /// <summary>Restores the clock from a saved game and refreshes the sky immediately.</summary>
        public void RestoreClock(int day, float hour)
        {
            Day = Mathf.Max(1, day);
            Hour = Mathf.Repeat(hour, 24f);
            _clockDirty = true;
            Apply();
        }

                private void Apply()
        {
            // Sun elevation: -1 at midnight, 0 at sunrise/sunset, +1 at noon.
            float elevation = Mathf.Sin((Hour / 24f) * Mathf.PI * 2f - Mathf.PI * 0.5f);

            // A soft dawn/dusk ramp centred on the horizon crossing.
            float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.18f, 0.28f, elevation));

            // Overcast/stormy skies dim the world further. (The weather is added after the clock, so look it up late.)
            if (_weather == null) _weather = WeatherSystem.Instance;
            float weatherDim = _weather != null ? _weather.LightMultiplier : 1f;

            LightLevel = Mathf.Clamp01(Mathf.Lerp(CloneZConfig.NightLightFloor, 1f, daylight) * weatherDim);

            // Warm the low sun at dawn/dusk, whiten it at noon.
            float warmth = Mathf.Clamp01(elevation * 1.6f);
            if (Sun != null)
            {
                float pitch = (Hour - 6f) / 12f * 180f;   // 0° at 06:00, 90° overhead at noon, 180° at 18:00
                float yaw = -35f + (Hour / 24f) * 30f;
                Sun.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                Sun.color = Color.Lerp(DuskSun, NoonSun, warmth);
                Sun.intensity = Mathf.Lerp(CloneZConfig.NightSunIntensity * MoonlightScale, CloneZConfig.DaySunIntensity, daylight) * weatherDim;
                Sun.shadowStrength = Mathf.Lerp(0.35f, 0.8f, daylight);
            }

            float scale = Mathf.Max(0f, AmbientScale);
            RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, daylight) * Mathf.Lerp(0.7f, 1f, weatherDim) * scale;
            RenderSettings.fogColor = Color.Lerp(NightFog, DayFog, daylight) * Mathf.Lerp(0.75f, 1f, weatherDim) * scale;

            // Cached _cam is already set in Awake; keep lazy fallback for late creation.
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
                _cam.backgroundColor = Color.Lerp(NightSky, DaySky, daylight) * Mathf.Lerp(0.6f, 1f, weatherDim) * scale;
        }

        /// <summary>Sight radius scale for zombies: full by day, reduced in darkness.</summary>
        public float ZombieVisionMultiplier =>
            Mathf.Lerp(CloneZConfig.ZombieNightVisionMultiplier, 1f, LightLevel);

        /// <summary>Formatted 24-hour clock, e.g. "07:05".</summary>
        public string ClockText
        {
            get
            {
                if (_clockDirty) UpdateClockCache();
                return _clockTextCached;
            }
        }

        /// <summary>12-hour digital-watch clock without the suffix, e.g. "12:40" (see <see cref="AmPm"/>).</summary>
        public string Clock12Text
        {
            get
            {
                if (_clockDirty) UpdateClockCache();
                return _clock12Cached;
            }
        }

        /// <summary>"AM" before noon, "PM" after.</summary>
        public string AmPm => Hour < 12f ? "AM" : "PM";

        /// <summary>Today's calendar date (Day 1 is the configured start date).</summary>
        public System.DateTime Date => new System.DateTime(Year, StartMonth, StartDayOfMonth).AddDays(Mathf.Max(0, Day - 1));

        /// <summary>Month/day as a digital watch shows it, e.g. "12/17".</summary>
        public string DateText
        {
            get
            {
                System.DateTime d = Date;
                return d.Month + "/" + d.Day;
            }
        }
    }
}
