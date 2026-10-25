using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The outdoor weather in real units, for the HUD watch and the body-temperature model.
    ///
    /// Wave survival keeps its mild summer: the old body-ambient formula restated in °C — about 12 °C
    /// before dawn to 26 °C mid-afternoon, a few degrees cooler under rain, cloud and fog — so
    /// <see cref="PlayerTemperature"/> maps it straight back onto the warmth scale it always used.
    ///
    /// A raid is a December night (<see cref="Winter"/>): the seasonal normals for the date (about -2 °C
    /// at the dawn low, +8 °C in the afternoon), cloud cover damping the swing (milder nights, cooler
    /// days), rain / snow and storms chilling the air further, plus a small per-raid offset seeded from
    /// <see cref="GameSession.RaidSeed"/>. In winter precipitation falls as snow when the air is cold
    /// enough, and a light flurry drifts down after dark for atmosphere.
    ///
    /// Everything is derived on demand from <see cref="TimeOfDay"/> and <see cref="WeatherSystem"/>
    /// (cached once per frame), so there is nothing to tick or save.
    /// </summary>
    public static class Climate
    {
        /// <summary>Winter rules (set per mode by the bootstrapper before the world is built).</summary>
        public static bool Winter;

        // ---- The mild (wave-survival) climate: the old body-ambient formula in °C ------------------------
        /// <summary>Mean air temperature of the mild climate (°C) — the old neutral ambient.</summary>
        public const float MildMeanC = 19f;
        /// <summary>Day/night swing either side of the mean (°C): warmest ~14:00, coldest ~02:00.</summary>
        public const float MildSwingC = 7f;
        /// <summary>Body-warmth scale per °C of air: the old day swing spread over <see cref="MildSwingC"/>.</summary>
        public const float BodyScalePerC = CloneZConfig.TempDaySwing / MildSwingC;
        /// <summary>Overcast cooling of the mild air (°C at full cloud) — the old cloud chill in °C.</summary>
        public const float MildCloudCoolC = CloneZConfig.TempCloudChill / BodyScalePerC;
        /// <summary>Fog cooling of the mild air (°C at thick fog) — the old fog chill in °C.</summary>
        public const float MildFogCoolC = CloneZConfig.TempFogChill / BodyScalePerC;
        /// <summary>How much a downpour cools the air itself (°C at full rain).</summary>
        public const float MildRainCoolC = 3f;
        /// <summary>The rest of the old rain chill — rain beating on someone out in it — on the 0..1 warmth
        /// scale per unit of rain. Applied by <see cref="PlayerTemperature"/> only outdoors.</summary>
        public const float RainExposureChill = CloneZConfig.TempRainChill - MildRainCoolC * BodyScalePerC;

        // ---- Winter ------------------------------------------------------------------------------------
        /// <summary>Hour of the daily low (around sunrise).</summary>
        public const float DawnLowHour = 7f;
        /// <summary>Hour of the daily high.</summary>
        public const float AfternoonHighHour = 14.5f;
        /// <summary>How much full cloud cover damps the day/night swing (milder nights, cooler days).</summary>
        public const float WinterCloudDamping = 0.35f;
        /// <summary>°C the air drops under full precipitation.</summary>
        public const float WinterPrecipCoolC = 1.5f;
        /// <summary>°C the air drops in thick fog.</summary>
        public const float WinterFogCoolC = 0.5f;
        /// <summary>Extra °C a storm takes off (on top of its precipitation).</summary>
        public const float WinterStormCoolC = 2f;
        /// <summary>The per-raid random offset spans ± this many °C.</summary>
        public const float RunOffsetRangeC = 2f;
        /// <summary>Precipitation turns to snow below about this air temperature (°C)…</summary>
        public const float SnowBelowC = 1.5f;
        /// <summary>…blending through sleet over ± this many °C around it.</summary>
        public const float SnowBlendC = 0.5f;
        /// <summary>Snow soaks this fraction as fast as the same intensity of rain.</summary>
        public const float SnowWetFactor = 0.1f;
        /// <summary>The light night-time flurry under a clear sky…</summary>
        public const float FlurryClear = 0.15f;
        /// <summary>…and under full cloud.</summary>
        public const float FlurryOvercast = 0.28f;
        /// <summary>Breath shows as vapour below this air temperature (°C, ~45 °F).</summary>
        public const float BreathBelowC = 7f;

        // ---- Shelter -----------------------------------------------------------------------------------
        /// <summary>The temperature an unheated building drifts toward (°C)…</summary>
        public const float IndoorRefC = 12f;
        /// <summary>…and how far from the outdoor air toward it its walls hold the inside (0..1).</summary>
        public const float IndoorBuffer = 0.45f;

        // Monthly normal lows / highs (°C) of a Kentucky town, January..December (mid-month values).
        private static readonly float[] s_NormalLow = { -4f, -3f, 1f, 6f, 11f, 16f, 19f, 18f, 13f, 6f, 1f, -2f };
        private static readonly float[] s_NormalHigh = { 6f, 9f, 14f, 20f, 25f, 29f, 31f, 30f, 27f, 21f, 14f, 8f };

        private static int s_Frame = -1;
        private static bool s_FrameWinter;
        private static float s_Air, s_SnowFraction, s_Snow;

        private static int s_OffsetSeed;
        private static float s_Offset;

        /// <summary>Outdoor air temperature in °C right now.</summary>
        public static float AirTemperatureC { get { Refresh(); return s_Air; } }

        /// <summary>Outdoor air temperature in °F (what the HUD watch shows, e.g. "43.3F").</summary>
        public static float AirTemperatureF => AirTemperatureC * 9f / 5f + 32f;

        /// <summary>How hard it is snowing, 0..1 (0 outside winter): snowfall from the clouds, or at
        /// least a light flurry after dark.</summary>
        public static float SnowIntensity { get { Refresh(); return s_Snow; } }

        /// <summary>Share of the precipitation falling as snow, 0 (rain) .. 1 (snow); 0 outside winter.</summary>
        public static float SnowFraction { get { Refresh(); return s_SnowFraction; } }

        /// <summary>True when it is cold enough to see your breath.</summary>
        public static bool BreathVisible => AirTemperatureC < BreathBelowC;

        /// <summary>The air temperature inside an unheated building when it is <paramref name="outdoorC"/>
        /// outside: the walls keep out the wind and hold it part-way toward <see cref="IndoorRefC"/>.</summary>
        public static float IndoorAirC(float outdoorC) => Mathf.Lerp(outdoorC, IndoorRefC, IndoorBuffer);

        /// <summary>The air temperature (°C) at a world position: indoors inside a registered building
        /// interior (<see cref="Shelter"/>), otherwise the outdoor air.</summary>
        public static float AirTemperatureAt(Vector3 position)
        {
            float air = AirTemperatureC;
            return Shelter.IsSheltered(position) ? IndoorAirC(air) : air;
        }

        private static void Refresh()
        {
            int frame = Time.frameCount;
            if (frame == s_Frame && s_FrameWinter == Winter) return;
            s_Frame = frame;
            s_FrameWinter = Winter;

            TimeOfDay clock = TimeOfDay.Instance;
            WeatherSystem w = WeatherSystem.Instance;
            float hour = clock != null ? clock.Hour : 12f;

            if (!Winter)
            {
                s_Air = MildAirC(hour, w);
                s_SnowFraction = 0f;
                s_Snow = 0f;
                return;
            }

            System.DateTime date = clock != null
                ? clock.Date
                : new System.DateTime(TimeOfDay.Year, CloneZConfig.RaidStartMonth, CloneZConfig.RaidStartDay);
            s_Air = WinterAirC(hour, date, w);
            s_SnowFraction = Mathf.InverseLerp(SnowBelowC + SnowBlendC, SnowBelowC - SnowBlendC, s_Air);

            // Snow from the clouds, or at least a light flurry after dark (which real rain washes out).
            float precip = w != null ? w.PrecipitationIntensity : 0f;
            float cloud = w != null ? w.Cloudiness : 0f;
            float snowfall = precip * s_SnowFraction;
            float liquid = precip - snowfall;
            float night = 1f - Daylight(hour);
            float flurry = Mathf.Lerp(FlurryClear, FlurryOvercast, cloud) * night * (1f - Mathf.Clamp01(liquid * 4f));
            s_Snow = Mathf.Clamp01(Mathf.Max(snowfall, flurry));
        }

        // The old wave-survival ambient (TempNeutral ± TempDaySwing, minus the weather chills) in °C.
        private static float MildAirC(float hour, WeatherSystem w)
        {
            float dayFactor = Mathf.Cos((hour - 14f) / 24f * Mathf.PI * 2f);   // +1 at 14:00, -1 at 02:00
            float air = MildMeanC + MildSwingC * dayFactor;
            if (w != null)
            {
                air -= MildRainCoolC * w.PrecipitationIntensity;
                air -= MildCloudCoolC * w.Cloudiness;
                air -= MildFogCoolC * w.FogIntensity;
            }
            return air;
        }

        private static float WinterAirC(float hour, System.DateTime date, WeatherSystem w)
        {
            NormalsFor(date, out float low, out float high);
            float mean = (low + high) * 0.5f, swing = (high - low) * 0.5f;
            float cloud = w != null ? w.Cloudiness : 0f;
            float air = mean + swing * DiurnalShape(hour) * (1f - WinterCloudDamping * cloud);
            if (w != null)
            {
                air -= WinterPrecipCoolC * w.PrecipitationIntensity;
                air -= WinterFogCoolC * w.FogIntensity;
                if (w.Current == Weather.Storm) air -= WinterStormCoolC * w.PrecipitationIntensity;
            }
            return air + RunOffsetC();
        }

        // -1 at the dawn low, +1 at the afternoon high: a quick morning warm-up, then a long cooling
        // through the evening and the night.
        private static float DiurnalShape(float hour)
        {
            float rise = AfternoonHighHour - DawnLowHour;
            float fall = 24f - rise;
            float since = Mathf.Repeat(hour - DawnLowHour, 24f);
            if (since <= rise) return -Mathf.Cos(since / rise * Mathf.PI);
            return Mathf.Cos((since - rise) / fall * Mathf.PI);
        }

        // Normal low / high for a date, eased between the mid-month values of neighbouring months.
        private static void NormalsFor(System.DateTime date, out float low, out float high)
        {
            int m = Mathf.Clamp(date.Month, 1, 12) - 1;
            float t = (date.Day - 15.5f) / System.DateTime.DaysInMonth(date.Year, date.Month);
            int other = t >= 0f ? (m + 1) % 12 : (m + 11) % 12;
            float k = Mathf.Abs(t);
            low = Mathf.Lerp(s_NormalLow[m], s_NormalLow[other], k);
            high = Mathf.Lerp(s_NormalHigh[m], s_NormalHigh[other], k);
        }

        // Daylight 0..1 for an hour — the same dawn/dusk ramp TimeOfDay lights the world with.
        private static float Daylight(float hour)
        {
            float elevation = Mathf.Sin((hour / 24f) * Mathf.PI * 2f - Mathf.PI * 0.5f);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.18f, 0.28f, elevation));
        }

        // A small per-raid shift so no two raid nights read quite the same on the watch.
        private static float RunOffsetC()
        {
            int seed = GameSession.IsRaidWorld ? GameSession.RaidSeed : 0;
            if (seed == 0) return 0f;
            if (seed != s_OffsetSeed)
            {
                s_OffsetSeed = seed;
                var rng = new System.Random(unchecked(seed * 31 + 17));
                s_Offset = (float)(rng.NextDouble() * 2.0 - 1.0) * RunOffsetRangeC;
            }
            return s_Offset;
        }
    }
}
