using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The sound of the world around the survivor, all of it synthesised. Beds follow the weather (wind rising into a
    /// howling gale, rain on the leaves or drumming on the roof overhead), the hour (the dawn chorus, birds and summer
    /// cicadas by day, crickets, katydids and frogs on warm nights), the season and the cold (the hush of a December
    /// night), the place (the woods, the town, the roadhouse's murmur and glasses in the hub) and the roof over the ears
    /// (outdoor beds muffled from inside). Wildlife calls far off round the listener — songbirds, woodpeckers, crows,
    /// owls, the whip-poor-will at dusk, coyotes, the abandoned dogs of empty towns, wolves and deer, creaking trees — and
    /// falls silent before an emission. In a storm each lightning strike flashes first (a directional light of our own)
    /// and its thunder follows after the time its sound takes to arrive.
    /// </summary>
    public sealed class AmbienceDirector : MonoBehaviour
    {
        private static AmbienceDirector s_Instance;

        private const float FullBand = 22000f, SpeedOfSound = 343f;
        // How outdoor beds sound from inside: muffled to this, and this much quieter.
        private const float IndoorLowPass = 1100f, IndoorLevel = 0.35f;
        // The whip-poor-will's call runs this long (three phrases); a bird's long run of calls chains them.
        private const float WhipRun = 3.45f;

        // A flat looping bed glided toward its target volume and low-pass. It starts (silent) once its sound is built,
        // so nothing is rendered on the main thread for it, and stops after a while at silence.
        private sealed class Bed
        {
            public readonly string Id;
            public AudioSource Src;
            public float Target, Lp = FullBand, LpTarget = FullBand, Idle;

            public Bed(string id) { Id = id; }
        }

        // One kind of call the wildlife makes: how far off and how high it is heard, how loud, its audible range, how
        // long before it calls again; its weight is recomputed from the hour, season, weather and place.
        private sealed class Caller
        {
            public readonly string Id;
            public readonly float Near, Far, Height, Volume, Range, Rest;
            public float Weight, QuietUntil;

            public Caller(string id, float near, float far, float height, float volume, float range, float rest)
            {
                Id = id;
                Near = near;
                Far = far;
                Height = height;
                Volume = volume;
                Range = range;
                Rest = rest;
            }
        }

        private readonly Bed _windBed = new Bed(SfxIds.WindAmbience), _gale = new Bed(SfxIds.WindHighAmbience),
            _forest = new Bed(SfxIds.ForestAmbience), _winterBed = new Bed(SfxIds.WinterAmbience), _insects = new Bed(SfxIds.NightAmbience),
            _cicadas = new Bed(SfxIds.CicadaAmbience), _frogs = new Bed(SfxIds.FrogAmbience), _rainBed = new Bed(SfxIds.RainAmbience),
            _downpour = new Bed(SfxIds.RainHeavyAmbience), _roof = new Bed(SfxIds.RainRoofAmbience), _bar = new Bed(SfxIds.BarAmbience);
        private Bed[] _beds;

        private readonly Caller _cardinal = new Caller(SfxIds.BirdCardinal, 15f, 70f, 6f, 0.45f, 200f, 6f),
            _wren = new Caller(SfxIds.BirdWren, 15f, 60f, 2.5f, 0.45f, 200f, 6f),
            _chickadee = new Caller(SfxIds.BirdChickadee, 15f, 50f, 5f, 0.45f, 180f, 6f),
            _titmouse = new Caller(SfxIds.BirdTitmouse, 15f, 60f, 6f, 0.45f, 200f, 8f),
            _jay = new Caller(SfxIds.BirdJay, 25f, 90f, 8f, 0.55f, 240f, 20f),
            _woodpecker = new Caller(SfxIds.WoodpeckerDrum, 30f, 120f, 7f, 0.75f, 280f, 20f),
            _crow = new Caller(SfxIds.CrowCall, 40f, 120f, 12f, 0.6f, 300f, 20f),
            _owl = new Caller(SfxIds.OwlCall, 40f, 120f, 8f, 0.7f, 320f, 30f),
            _whip = new Caller(SfxIds.WhippoorwillCall, 40f, 120f, 1.5f, 0.6f, 300f, 50f),
            _coyote = new Caller(SfxIds.CoyoteYip, 70f, 120f, 1f, 0.75f, 350f, 120f),
            _dog = new Caller(SfxIds.DogBarkFar, 60f, 120f, 1f, 0.9f, 320f, 40f),
            _creak = new Caller(SfxIds.BranchCreak, 15f, 45f, 6f, 0.8f, 120f, 12f),
            _crack = new Caller(SfxIds.TreeCrack, 60f, 120f, 5f, 0.75f, 320f, 150f),
            _wolf = new Caller(SfxIds.WolfHowl, 35f, 60f, 2f, 0.45f, 90f, 60f),
            _deer = new Caller(SfxIds.DeerCall, 35f, 60f, 1f, 0.35f, 70f, 60f);
        private Caller[] _callers;
        private Caller _lastCaller;

        private WeatherSystem _weather;
        private TimeOfDay _clock;

        // What surrounds the ears, looked at a couple of times a second (the queries walk lists).
        private float _senseAt;
        private bool _indoors;
        private float _trees, _town = 1f;

        // The conditions this frame.
        private float _day, _night, _dawn, _dusk, _rain, _fog, _air, _warm, _hot, _frogSeason, _birdSeason, _whipSeason;
        private bool _winter;
        // Smoothed: the wind's strength 0..1 and how wet the ground is (frogs come out after rain).
        private float _wind = 0.25f, _wet;

        private float _callAt = 8f, _gustAt = 10f, _rumbleAt = 50f;

        // Lightning: a directional light of our own (TimeOfDay owns the sun), flickered through a few return strokes.
        private Light _flash;
        private float _flashT = -1f, _flashPeak;
        private readonly float[] _strokeAt = new float[4], _strokeAmp = new float[4];
        private int _strokes;

        /// <summary>Ensures a single director exists (it outlives scenes).</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[AmbienceDirector]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<AmbienceDirector>();
        }

        private void Awake()
        {
            _beds = new[] { _windBed, _gale, _forest, _winterBed, _insects, _cicadas, _frogs, _rainBed, _downpour, _roof, _bar };
            _callers = new[] { _cardinal, _wren, _chickadee, _titmouse, _jay, _woodpecker, _crow, _owl, _whip, _coyote, _dog, _creak, _crack, _wolf, _deer };
            WeatherSystem.Lightning += OnLightning;
        }

        private void OnDestroy()
        {
            WeatherSystem.Lightning -= OnLightning;
            if (s_Instance == this) s_Instance = null;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool paused = Time.timeScale <= 0f;
            float gdt = paused ? 0f : Time.deltaTime;
            Actor local = Actor.LocalPlayer;
            bool world = local != null;
            Vector3 ears = ListenerRig.Active ? ListenerRig.Position : (world ? local.transform.position : Vector3.zero);
            if (_weather == null) _weather = WeatherSystem.Instance;
            // The clock is built with each world, after this director (it outlives scenes), so look again until found.
            if (_clock == null) _clock = TimeOfDay.Instance;

            if (Time.unscaledTime >= _senseAt)
            {
                _senseAt = Time.unscaledTime + 0.5f;
                Sense(ears, world);
            }
            Read(gdt);
            Steer(world);
            for (int i = 0; i < _beds.Length; i++) Drive(_beds[i], dt);

            UpdateFlash(paused);
            if (paused || !world || GameSession.IsHub) return;
            WindGusts(gdt);
            DistantStorm(gdt);
            Wildlife(ears, gdt);
        }

        #region Conditions
        private void Sense(Vector3 ears, bool world)
        {
            if (!world)
            {
                _indoors = false;
                _trees = 0f;
                _town = 1f;
                return;
            }
            _indoors = SfxAcoustics.Indoors || (Shelter.Count > 0 && Shelter.IsSheltered(ears));
            _trees = ZoneForest.Density(ears);
            _town = GameSession.IsRaidWorld ? MapGameplay.DensityAt(ears) : 0.6f;
        }

        // The hour, the weather, the season and the air, as the 0..1 factors the beds and the wildlife follow.
        private void Read(float gdt)
        {
            float hour = _clock != null ? _clock.Hour : 12f;
            // The sun's height as TimeOfDay swings it: 0 at 06:00 and 18:00, 1 at noon.
            float sun = Mathf.Sin(hour / 24f * Mathf.PI * 2f - Mathf.PI * 0.5f);
            _day = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.06f, 0.2f, sun));
            _night = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.25f, 0.02f, sun));
            _dawn = Bump(hour, 5f, 6.1f, 7.6f);
            _dusk = Bump(hour, 18f, 19.1f, 20.8f);

            Weather now = _weather != null ? _weather.Current : Weather.Clear;
            _rain = _weather != null ? _weather.RainIntensity : 0f;
            _fog = _weather != null ? _weather.FogIntensity : 0f;
            float gusting = WindFor(now) + 0.12f * (Mathf.PerlinNoise(Time.time * 0.013f, 0.37f) - 0.5f);
            _wind = Mathf.MoveTowards(_wind, Mathf.Clamp01(gusting), gdt * 0.03f);
            _wet = Mathf.MoveTowards(_wet, _rain > 0.05f ? 1f : 0f, gdt * (_rain > 0.05f ? _rain / 90f : 1f / 900f));

            _winter = Climate.Winter;
            _air = Climate.AirTemperatureC;
            int month = _clock != null ? _clock.Date.Month : 7;
            _birdSeason = _winter ? 0.35f : month >= 3 && month <= 7 ? 1f : month >= 8 && month <= 10 ? 0.6f : 0.35f;
            _warm = _winter ? 0f : Smooth(8f, 15f, _air) * Months(month, 5, 10);
            _hot = _winter ? 0f : Smooth(19f, 25f, _air) * Months(month, 6, 9);
            _frogSeason = _winter ? 0f : Smooth(6f, 12f, _air) * (month >= 3 && month <= 6 ? 1f : month >= 7 && month <= 9 ? 0.6f : month == 10 ? 0.2f : 0f);
            _whipSeason = _winter ? 0f : Smooth(6f, 10f, _air) * Months(month, 4, 8);
        }

        private static float WindFor(Weather w)
        {
            switch (w)
            {
                case Weather.Cloudy: return 0.35f;
                case Weather.Overcast: return 0.45f;
                case Weather.Fog: return 0.08f;
                case Weather.Rain: return 0.55f;
                case Weather.Storm: return 0.92f;
                default: return 0.2f;
            }
        }

        // 0 outside (a, b), easing up to 1 at `peak` and back down.
        private static float Bump(float h, float a, float peak, float b)
        {
            if (h <= a || h >= b) return 0f;
            return h < peak ? Mathf.SmoothStep(0f, 1f, (h - a) / (peak - a)) : 1f - Mathf.SmoothStep(0f, 1f, (h - peak) / (b - peak));
        }

        private static float Smooth(float from, float to, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, v));

        // 1 from month `from` to `to`, 0.4 the month either side, else 0.
        private static float Months(int m, int from, int to) => m >= from && m <= to ? 1f : m == from - 1 || m == to + 1 ? 0.4f : 0f;
        #endregion

        #region Beds
        // Where every bed should be: the hub's room with the weather beyond its walls, or the world outside (muffled
        // from inside a building), or — with no survivor about — a faint wind.
        private void Steer(bool world)
        {
            for (int i = 0; i < _beds.Length; i++)
            {
                _beds[i].Target = 0f;
                _beds[i].LpTarget = FullBand;
            }
            if (!world)
            {
                _windBed.Target = 0.12f;
                return;
            }
            float strong = Smooth(0.55f, 0.95f, _wind);
            if (GameSession.IsHub)
            {
                _bar.Target = 0.45f;
                _windBed.Target = 0.05f + 0.1f * _wind;
                _windBed.LpTarget = 700f;
                _gale.Target = 0.3f * strong;
                _gale.LpTarget = 600f;
                _roof.Target = _rain > 0.02f ? 0.12f + 0.4f * _rain : 0f;
                _winterBed.Target = _winter ? 0.08f : 0f;
                _winterBed.LpTarget = 900f;
                return;
            }

            bool alive = !EmissionDirector.Active;   // everything living falls silent before a blowout
            float woods = Mathf.Clamp01(_trees * 1.6f), town = Mathf.Clamp01((_town - 0.5f) / 1.5f);
            float calm = 1f - 0.6f * strong;
            _windBed.Target = (0.15f + 0.35f * _wind) * (1f - 0.4f * _fog);
            _gale.Target = 0.6f * strong;
            _rainBed.Target = 0.5f * Smooth(0f, 0.35f, _rain) * (1f - 0.6f * Smooth(0.5f, 0.95f, _rain));
            _downpour.Target = 0.55f * Smooth(0.45f, 0.95f, _rain);
            if (alive)
            {
                float birds = Mathf.Max(_day, _dawn) * _birdSeason * (0.7f + 0.8f * _dawn);
                _forest.Target = 0.6f * birds * (0.3f + 0.7f * woods) * (1f - 0.6f * town) * (1f - 0.75f * _rain) * calm;
                _cicadas.Target = 0.75f * _day * _hot * (0.4f + 0.6f * woods) * (1f - 0.9f * _rain);
                _insects.Target = 0.55f * _night * _warm * (1f - 0.85f * _rain) * (1f - 0.4f * town);
                _frogs.Target = 0.45f * Mathf.Max(_night, _dusk) * _frogSeason * (0.35f + 0.65f * _wet) * (1f - 0.5f * _rain) * (1f - 0.6f * town);
            }
            bool cold = _winter || _air < 3f;
            _winterBed.Target = cold ? (0.35f + 0.15f * _night) * (1f - 0.6f * _rain) * (1f - 0.5f * strong) : 0f;

            if (_indoors)
            {
                for (int i = 0; i < _beds.Length; i++)
                {
                    _beds[i].Target *= IndoorLevel;
                    _beds[i].LpTarget = IndoorLowPass;
                }
                _roof.Target = _rain > 0.02f ? 0.15f + 0.45f * _rain : 0f;
                _roof.LpTarget = FullBand;
            }
        }

        private static void Drive(Bed b, float dt)
        {
            if (b.Src == null)
            {
                if (b.Target <= 0.001f) return;
                if (!Sfx.Ready(b.Id))
                {
                    Sfx.Request(b.Id);
                    return;
                }
                b.Src = Sfx.LoopFlat(b.Id, 0f);
                if (b.Src == null) return;
                b.Idle = 0f;
                b.Lp = b.LpTarget;
                Sfx.SetLowPass(b.Src, b.Lp);
            }
            // About a second for a big change (stepping indoors), never a jump; the weather's own changes are slower still.
            float v = Sfx.GetVolume(b.Src);
            v = Mathf.MoveTowards(v, b.Target, Mathf.Max(0.04f, Mathf.Abs(b.Target - v) * 1.2f) * dt);
            Sfx.SetVolume(b.Src, v);
            if (!Mathf.Approximately(b.Lp, b.LpTarget))
            {
                b.Lp = Mathf.Exp(Mathf.MoveTowards(Mathf.Log(b.Lp), Mathf.Log(b.LpTarget), dt * 3f));
                Sfx.SetLowPass(b.Src, b.Lp);
            }
            if (b.Target <= 0.001f && v <= 0.001f)
            {
                b.Idle += dt;
                if (b.Idle < 15f) return;
                Sfx.Stop(b.Src);
                b.Src = null;
            }
            else b.Idle = 0f;
        }

        // A gust sweeping past now and then once the wind is strong.
        private void WindGusts(float gdt)
        {
            float strong = Mathf.InverseLerp(0.55f, 0.95f, _wind);
            if (strong <= 0f) return;
            _gustAt -= gdt;
            if (_gustAt > 0f) return;
            _gustAt = Random.Range(7f, 22f) / (0.5f + strong);
            if (!Sfx.Ready(SfxIds.WindGust))
            {
                Sfx.Request(SfxIds.WindGust);
                return;
            }
            AudioSource s = Sfx.PlayFlat(SfxIds.WindGust, (0.25f + 0.35f * strong) * (_indoors ? 0.4f : 1f), Random.Range(0.9f, 1.1f), Random.Range(-0.6f, 0.6f));
            if (s != null && _indoors) Sfx.SetLowPass(s, IndoorLowPass);
        }
        #endregion

        #region Thunder and lightning
        // A storm somewhere else, before or after the one overhead: far thunder rolling in while it rains (never in
        // winter, nor over falling snow), its lightning a dim flicker on the clouds at night.
        private void DistantStorm(float gdt)
        {
            if (_winter || _rain < 0.3f || (_weather != null && _weather.Current == Weather.Storm)) return;
            _rumbleAt -= gdt;
            if (_rumbleAt > 0f) return;
            _rumbleAt = Random.Range(35f, 110f);
            Strike(Random.Range(6000f, 13000f), _night > 0.5f);
        }

        // WeatherSystem's storm strikes: about a third of them close.
        private void OnLightning()
        {
            if (this == null || !isActiveAndEnabled) return;
            Strike(Random.value < 0.3f ? Random.Range(350f, 1800f) : Random.Range(1800f, 9000f), true);
        }

        // A strike `distance` metres off in a random direction: the flash now, the thunder when its sound arrives —
        // heard from a point toward the strike and high up, so walls and roofs muffle it but trees and fences don't.
        private void Strike(float distance, bool flash)
        {
            float a = Random.value * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            float near = Mathf.InverseLerp(9000f, 350f, distance);
            if (flash) Flash(dir, near);
            string id = distance < 2000f ? SfxIds.ThunderNear : SfxIds.ThunderFar;
            Sfx.Request(id);
            float volume = Mathf.Lerp(0.3f, 1f, near * near);
            float pitch = Random.Range(0.94f, 1.04f) * Mathf.Lerp(0.88f, 1f, near);
            Vector3 at = Sfx.ListenerPosition + dir * 50f + Vector3.up * 35f;
            Sfx.PlayDelayed(id, at, distance / SpeedOfSound, volume, pitch, 240f);
        }

        private void Flash(Vector3 dir, float near)
        {
            if (_flash == null)
            {
                var go = new GameObject("Lightning");
                go.transform.SetParent(transform, false);
                _flash = go.AddComponent<Light>();
                _flash.type = LightType.Directional;
                _flash.color = new Color(0.78f, 0.84f, 1f);
                _flash.shadows = LightShadows.Soft;
                _flash.shadowStrength = 0.8f;
                _flash.intensity = 0f;
                // Kept inactive between flashes: nothing that looks for a scene's lights should find it.
                go.SetActive(false);
            }
            float dark = _clock != null ? 1f - _clock.LightLevel : 1f;
            _flashPeak = Mathf.Lerp(0.2f, 2.6f, near * near) * Mathf.Lerp(0.6f, 1f, dark) * (_indoors ? 0.3f : 1f);
            float elevation = Random.Range(30f, 65f) * Mathf.Deg2Rad;
            _flash.transform.rotation = Quaternion.LookRotation((-dir * Mathf.Cos(elevation) + Vector3.down * Mathf.Sin(elevation)).normalized);
            _strokes = Random.Range(2, _strokeAt.Length + 1);
            float t = 0f;
            for (int i = 0; i < _strokes; i++)
            {
                _strokeAt[i] = t;
                _strokeAmp[i] = i == 0 ? 1f : Random.Range(0.35f, 0.9f);
                t += Random.Range(0.04f, 0.13f);
            }
            _flashT = 0f;
            _flash.intensity = 0f;
            _flash.gameObject.SetActive(true);
        }

        private void UpdateFlash(bool paused)
        {
            if (_flashT < 0f || _flash == null) return;
            if (paused)
            {
                EndFlash();
                return;
            }
            _flashT += Time.deltaTime;
            float v = 0f;
            for (int i = 0; i < _strokes; i++)
            {
                float t = _flashT - _strokeAt[i];
                if (t < 0f) continue;
                v = Mathf.Max(v, _strokeAmp[i] * (t < 0.012f ? t / 0.012f : Mathf.Exp(-(t - 0.012f) / 0.045f)));
            }
            _flash.intensity = v * _flashPeak;
            if (_flashT > _strokeAt[_strokes - 1] + 0.35f) EndFlash();
        }

        private void EndFlash()
        {
            _flashT = -1f;
            if (_flash == null) return;
            _flash.intensity = 0f;
            _flash.gameObject.SetActive(false);
        }
        #endregion

        #region Wildlife
        // How likely each call is right now, from the hour, the season, the weather and the place.
        private void Weigh()
        {
            float wet = 1f - 0.85f * _rain, calm = 1f - 0.7f * Mathf.InverseLerp(0.6f, 0.95f, _wind);
            float woods = Mathf.Clamp01(_trees * 1.6f), open = 1f - woods;
            float town = Mathf.Clamp01((_town - 0.5f) / 1.5f), country = 1f - town;
            float song = Mathf.Max(_day, _dawn) * _birdSeason * (1f + 2.5f * _dawn) * wet * calm * (0.35f + 0.65f * woods) * (1f - 0.6f * town);
            _cardinal.Weight = song * (_winter ? 0.25f : 1f);
            _wren.Weight = song * 0.9f;
            _chickadee.Weight = song * 0.8f;
            _titmouse.Weight = song * (_winter ? 0.4f : 0.8f);
            _jay.Weight = song * 0.45f;
            _woodpecker.Weight = _day * (0.2f + 0.8f * woods) * wet * 0.45f * (1f + _dawn);
            _crow.Weight = Mathf.Max(_day, _dawn) * wet * (0.3f + 0.4f * open + 0.3f * town) * 0.6f * (1f + 1.5f * _dawn);
            _owl.Weight = Mathf.Max(_night, _dusk * 0.8f) * (0.3f + 0.7f * woods) * wet * calm * (1f + _dusk + _dawn) * 0.7f;
            _whip.Weight = Mathf.Max(_dusk, Mathf.Max(_night * 0.5f, _dawn * 0.7f)) * _whipSeason * wet * calm * (0.4f + 0.6f * woods) * country;
            _coyote.Weight = _night * country * wet * (_winter ? 0.4f : 0.25f);
            _dog.Weight = (0.35f + 0.65f * _night) * (0.15f + 0.85f * town) * wet * 0.35f * (GameSession.IsExtraction ? 1f : 0.5f);
            _creak.Weight = Mathf.InverseLerp(0.3f, 0.8f, _wind) * (0.15f + 0.85f * woods) * 0.5f + (_winter ? 0.1f * woods : 0f);
            _crack.Weight = (_winter && _air < -1f ? 0.05f : 0f) + Mathf.InverseLerp(0.75f, 1f, _wind) * woods * 0.04f;
            bool raid = GameSession.IsExtraction;
            _wolf.Weight = raid ? _night * 0.12f : 0f;
            _deer.Weight = raid && _trees > 0.25f ? _day * 0.08f : 0f;
            if (!EmissionDirector.Active) return;
            for (int i = 0; i < _callers.Length; i++)
                if (_callers[i] != _creak) _callers[i].Weight = 0f;
        }

        // Every so often (more often the busier the hour), a call from somewhere 15–120 m round the listener: never the
        // same kind twice running, each kind resting a while after it calls.
        private void Wildlife(Vector3 ears, float gdt)
        {
            _callAt -= gdt;
            if (_callAt > 0f) return;
            Weigh();
            float now = Time.time, total = 0f, activity = 0f;
            for (int i = 0; i < _callers.Length; i++)
            {
                Caller c = _callers[i];
                activity += c.Weight;
                if (c == _lastCaller || now < c.QuietUntil) continue;
                total += c.Weight;
            }
            _callAt = Mathf.Min(60f, 16f / (activity + 0.4f) * Random.Range(0.6f, 1.4f)) * (_indoors ? 2f : 1f);
            if (total <= 0.001f) return;
            float pick = Random.value * total;
            Caller chosen = null;
            for (int i = 0; i < _callers.Length; i++)
            {
                Caller c = _callers[i];
                if (c == _lastCaller || now < c.QuietUntil || c.Weight <= 0f) continue;
                chosen = c;
                pick -= c.Weight;
                if (pick <= 0f) break;
            }
            if (chosen == null) return;
            if (!Sfx.Ready(chosen.Id))
            {
                Sfx.Request(chosen.Id);
                _callAt = 1f;
                return;
            }
            _lastCaller = chosen;
            chosen.QuietUntil = now + chosen.Rest * Random.Range(0.8f, 1.5f);
            Vector3 at = Around(ears, chosen);
            Call(chosen, at, 0f);
            if (chosen == _whip)
            {
                // A whip-poor-will goes on and on from the one spot.
                int runs = Random.Range(2, 7);
                for (int k = 1; k < runs; k++) Call(chosen, at, k * WhipRun * Random.Range(0.98f, 1.03f));
            }
            else if (chosen == _cardinal || chosen == _wren || chosen == _chickadee || chosen == _titmouse)
            {
                // A songbird sings again from its perch a few times.
                int again = Random.Range(0, 3);
                float t = 0f;
                for (int k = 0; k < again; k++) Call(chosen, at, t += Random.Range(5f, 12f));
            }
            else if (chosen == _owl && Random.value < 0.35f)
            {
                // Another owl answers from elsewhere.
                Call(chosen, Around(ears, chosen), Random.Range(4f, 9f));
            }
        }

        private static Vector3 Around(Vector3 ears, Caller c)
        {
            float a = Random.value * Mathf.PI * 2f, d = Random.Range(c.Near, c.Far);
            return ears + new Vector3(Mathf.Cos(a) * d, c.Height - 1.6f + Random.Range(-0.5f, 1.5f), Mathf.Sin(a) * d);
        }

        private static void Call(Caller c, Vector3 at, float delay)
        {
            float volume = c.Volume * Random.Range(0.85f, 1.1f), pitch = Random.Range(0.95f, 1.05f);
            if (delay <= 0f) Sfx.Play(c.Id, at, volume, pitch, c.Range);
            else Sfx.PlayDelayed(c.Id, at, delay, volume, pitch, c.Range);
        }
        #endregion
    }
}
