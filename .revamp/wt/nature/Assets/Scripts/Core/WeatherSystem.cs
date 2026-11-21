using UnityEngine;

namespace CloneZ
{
    public enum Weather { Clear, Cloudy, Overcast, Fog, Rain, Storm }

    /// <summary>
    /// A lightweight weather simulation. Cycles between clear, overcast, fog, rain
    /// and storm, easing precipitation and fog intensities so transitions are gradual.
    /// Rain darkens the sky (feeding <see cref="TimeOfDay"/>), thickens the fog, spawns a
    /// code-built rain particle effect that trails the camera, and — like Project
    /// Zomboid — cuts down how far the zombies can see.
    ///
    /// On a winter raid (<see cref="Climate.Winter"/>) the sky starts low (overcast or
    /// cloudy, sometimes already snowing), fronts last longer and never build into a
    /// thunderstorm, and precipitation falls as snow whenever the air is cold enough
    /// (<see cref="Climate.SnowFraction"/>): the rain particles stand down (the vision
    /// effects draw the snow) and it soaks far more slowly (<see cref="WettingIntensity"/>).
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        /// <summary>Singleton instance of the WeatherSystem.</summary>
        public static WeatherSystem Instance { get; private set; }

        // Winter fronts linger: a raid's whole night lasts only half an hour of real time.
        private const float WinterDurationScale = 3f;

        /// <summary>Current weather condition.</summary>
        public Weather Current { get; private set; } = Weather.Clear;

        /// <summary>How hard it is precipitating, 0..1 — rain, or on a cold winter night, snow.</summary>
        public float PrecipitationIntensity { get; private set; }

        /// <summary>Precipitation falling as rain, 0..1. Outside winter this is all of it; in winter
        /// whatever falls as snow is left out (see <see cref="SnowfallIntensity"/>).</summary>
        public float RainIntensity => PrecipitationIntensity * (1f - Climate.SnowFraction);

        /// <summary>Precipitation falling as snow, 0..1 (excludes the light night flurry that
        /// <see cref="Climate.SnowIntensity"/> adds for atmosphere).</summary>
        public float SnowfallIntensity => PrecipitationIntensity * Climate.SnowFraction;

        /// <summary>How hard the weather soaks someone out in it: rain in full, snow far less.</summary>
        public float WettingIntensity => RainIntensity + SnowfallIntensity * Climate.SnowWetFactor;

        /// <summary>True while the precipitation falls mostly as snow.</summary>
        public bool IsSnowing => PrecipitationIntensity > 0.01f && Climate.SnowFraction >= 0.5f;

        /// <summary>Current fog intensity, 0..1.</summary>
        public float FogIntensity { get; private set; }    // 0..1
        /// <summary>Current cloudiness level, 0..1.</summary>
        public float Cloudiness { get; private set; }      // 0..1

        private float _targetPrecip, _targetFog, _targetCloud;
        private float _cachedFog = -1f; // cache last fog intensity to avoid redundant RenderSettings updates
        private float _stateTimer;
        private float _flashTimer;
        private bool _restored;   // a saved game set the weather; Start must not clobber it

        private Camera _cam;
        private ParticleSystem _rain;
        private ParticleSystem.EmissionModule _rainEmission;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _cam = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            _cam = Camera.main;
            BuildRain();
            if (!_restored)
            {
                if (Climate.Winter) StartWinter();
                else SetWeather(Weather.Clear);
            }
            _stateTimer = NextDuration();
        }

        // A raid opens under a low winter sky — overcast or cloudy, sometimes already snowing — at full
        // strength rather than easing in from a clear sky.
        private void StartWinter()
        {
            float r = Random.value;
            SetWeather(r < 0.45f ? Weather.Overcast : r < 0.78f ? Weather.Cloudy : Weather.Rain);
            PrecipitationIntensity = _targetPrecip;
            FogIntensity = _targetFog;
            Cloudiness = _targetCloud;
            ApplyFog();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            _stateTimer -= dt;
            if (_stateTimer <= 0f)
            {
                SetWeather(NextWeather(Current));
                _stateTimer = NextDuration();
            }

            PrecipitationIntensity = Mathf.MoveTowards(PrecipitationIntensity, _targetPrecip, CloneZConfig.WeatherBlendSpeed * dt);
            FogIntensity = Mathf.MoveTowards(FogIntensity, _targetFog, CloneZConfig.WeatherBlendSpeed * dt);
            Cloudiness = Mathf.MoveTowards(Cloudiness, _targetCloud, CloneZConfig.WeatherBlendSpeed * dt);

            ApplyFog();
            ApplyRain();
            ApplyStormFlash(dt);
        }

        private void SetWeather(Weather w)
        {
            Current = w;
            switch (w)
            {
                case Weather.Clear:    _targetPrecip = 0f;   _targetFog = 0f;    _targetCloud = 0f;    break;
                case Weather.Cloudy:   _targetPrecip = 0f;   _targetFog = 0.08f; _targetCloud = 0.40f; break;
                case Weather.Overcast: _targetPrecip = 0f;   _targetFog = 0.20f; _targetCloud = 0.70f; break;
                case Weather.Fog:      _targetPrecip = 0f;   _targetFog = 0.85f; _targetCloud = 0.50f; break;
                case Weather.Rain:     _targetPrecip = 0.6f; _targetFog = 0.28f; _targetCloud = 0.75f; break;
                case Weather.Storm:    _targetPrecip = 1.0f; _targetFog = 0.40f; _targetCloud = 0.95f; break;
            }
        }

        /// <summary>Restores the weather from a saved game (holds the saved intensities so its own
        /// Start won't reset it to Clear).</summary>
        public void Restore(Weather weather, float rain, float fog, float cloud)
        {
            _restored = true;
            Current = weather;
            PrecipitationIntensity = _targetPrecip = Mathf.Clamp01(rain);
            FogIntensity = _targetFog = Mathf.Clamp01(fog);
            Cloudiness = _targetCloud = Mathf.Clamp01(cloud);
            _stateTimer = NextDuration();
            ApplyFog();
        }

        // How long the next weather state holds.
        private static float NextDuration()
        {
            float d = Random.Range(CloneZConfig.WeatherMinDuration, CloneZConfig.WeatherMaxDuration);
            return Climate.Winter ? d * WinterDurationScale : d;
        }

        // A winter raid never builds into a thunderstorm (lightning over falling snow looks wrong):
        // a front that would have turned stormy just keeps snowing — or raining, if it's mild.
        private static Weather NextWeather(Weather from)
        {
            Weather next = PickNext(from);
            if (Climate.Winter && next == Weather.Storm) next = Weather.Rain;
            return next;
        }

        // Weather drifts one step at a time so it feels like a front rolling through.
        private static Weather PickNext(Weather from)
        {
            switch (from)
            {
                case Weather.Clear:    return Roll(Weather.Clear, Weather.Cloudy, Weather.Fog);
                case Weather.Cloudy:   return Roll(Weather.Clear, Weather.Overcast, Weather.Cloudy);
                case Weather.Overcast: return Roll(Weather.Cloudy, Weather.Rain, Weather.Fog);
                case Weather.Fog:      return Roll(Weather.Clear, Weather.Cloudy, Weather.Overcast);
                case Weather.Rain:     return Roll(Weather.Overcast, Weather.Storm, Weather.Rain);
                case Weather.Storm:    return Roll(Weather.Rain, Weather.Overcast, Weather.Rain);
                default:               return Weather.Clear;
            }
        }

        private static Weather Roll(Weather a, Weather b, Weather c)
        {
            float r = Random.value;
            return r < 0.5f ? a : (r < 0.8f ? b : c);
        }

        private void ApplyFog()
        {
            float fog = Mathf.Max(FogIntensity, PrecipitationIntensity * 0.35f);   // falling snow thickens the murk like rain
            if (Mathf.Approximately(_cachedFog, fog)) return; // no change, skip updates
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = Mathf.Lerp(35f, 6f, fog);
            RenderSettings.fogEndDistance = Mathf.Lerp(78f, 28f, fog);
            _cachedFog = fog; // update cache
        }

        /// <summary>Extra dimming from cloud cover, fed into the day/night light level.</summary>
        public float LightMultiplier => Mathf.Lerp(1f, 0.5f, Cloudiness);

        /// <summary>Sight radius scale for zombies: heavy rain (or snow) and fog shorten their vision.</summary>
        public float VisionMultiplier =>
            (1f - CloneZConfig.ZombieRainVisionPenalty * PrecipitationIntensity) *
            (1f - CloneZConfig.ZombieFogVisionPenalty * FogIntensity);

        /// <summary>Display label for the current weather state.</summary>
        public string Label
        {
            get
            {
                switch (Current)
                {
                    case Weather.Cloudy: return "Cloudy";
                    case Weather.Overcast: return "Overcast";
                    case Weather.Fog: return "Fog";
                    case Weather.Rain: return Climate.SnowFraction >= 0.5f ? "Snow" : "Rain";
                    case Weather.Storm: return Climate.SnowFraction >= 0.5f ? "Blizzard" : "Storm";
                    default: return "Clear";
                }
            }
        }

        #region Rain effect
        private void BuildRain()
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(transform);
            _rain = go.AddComponent<ParticleSystem>();
            _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _rain.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.72f, 0.80f, 0.92f, 0.35f);
            main.maxParticles = 2500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = _rain.emission;
            emission.rateOverTime = 0f;
            _rainEmission = emission;

            var shape = _rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, 1f, 44f);
            shape.position = new Vector3(0f, 13f, 0f);

            var vel = _rain.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(-28f);
            vel.x = new ParticleSystem.MinMaxCurve(-3.5f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3.2f;
            renderer.velocityScale = 0.06f;
            renderer.material = RainMaterial();
            renderer.alignment = ParticleSystemRenderSpace.View;

            _rain.Play();
        }

        private static Material RainMaterial()
        {
            // Sprites/Default is always available and respects the particle's vertex alpha.
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Unlit/Transparent");
            var m = new Material(s);
            var c = new Color(0.75f, 0.82f, 0.95f, 0.4f);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        private void ApplyRain()
        {
            if (_rain == null) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                // Rain falls round the survivor: the isometric camera hangs far behind them, so there the box
                // centres on the point the view looks at; in the close views, on the camera itself.
                bool iso = ViewState.Current == CameraView.Isometric && IsoCameraFollow.Instance != null;
                _rain.transform.position = iso ? IsoCameraFollow.FocusPoint : _cam.transform.position;
            }
            // Only liquid rain draws streaks — falling snow is drawn by the vision effects instead.
            _rainEmission.rateOverTime = RainIntensity * 1500f;
        }

        // An occasional lightning flash during a storm.
        private void ApplyStormFlash(float dt)
        {
            if (Current == Weather.Storm && RainIntensity > 0.75f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f)
                {
                    _flashTimer = Random.Range(4f, 11f);
                    StartCoroutine(Flash());
                }
            }
        }

        private System.Collections.IEnumerator Flash()
        {
            Color ambient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = new Color(0.7f, 0.72f, 0.8f);
            yield return new WaitForSeconds(0.06f);
            RenderSettings.ambientLight = ambient;
            yield return new WaitForSeconds(0.05f);
            Color a2 = RenderSettings.ambientLight;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.66f);
            yield return new WaitForSeconds(0.05f);
            RenderSettings.ambientLight = a2;
        }
        #endregion
    }
}
