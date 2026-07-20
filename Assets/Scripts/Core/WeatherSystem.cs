using UnityEngine;

namespace CloneZ
{
    public enum Weather { Clear, Cloudy, Overcast, Fog, Rain, Storm }

    /// <summary>
    /// A lightweight weather simulation. Cycles between clear, overcast, fog, rain
    /// and storm, easing rain and fog intensities so transitions are gradual. Rain
    /// darkens the sky (feeding <see cref="TimeOfDay"/>), thickens the fog, spawns a
    /// code-built rain particle effect that trails the camera, and — like Project
    /// Zomboid — cuts down how far the zombies can see.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        public Weather Current { get; private set; } = Weather.Clear;
        public float RainIntensity { get; private set; }   // 0..1
        public float FogIntensity { get; private set; }    // 0..1
        public float Cloudiness { get; private set; }      // 0..1

        private float _targetRain, _targetFog, _targetCloud;
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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            _cam = Camera.main;
            BuildRain();
            if (!_restored) SetWeather(Weather.Clear);
            _stateTimer = Random.Range(CloneZConfig.WeatherMinDuration, CloneZConfig.WeatherMaxDuration);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            _stateTimer -= dt;
            if (_stateTimer <= 0f)
            {
                SetWeather(PickNext(Current));
                _stateTimer = Random.Range(CloneZConfig.WeatherMinDuration, CloneZConfig.WeatherMaxDuration);
            }

            RainIntensity = Mathf.MoveTowards(RainIntensity, _targetRain, CloneZConfig.WeatherBlendSpeed * dt);
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
                case Weather.Clear:    _targetRain = 0f;   _targetFog = 0f;    _targetCloud = 0f;    break;
                case Weather.Cloudy:   _targetRain = 0f;   _targetFog = 0.08f; _targetCloud = 0.40f; break;
                case Weather.Overcast: _targetRain = 0f;   _targetFog = 0.20f; _targetCloud = 0.70f; break;
                case Weather.Fog:      _targetRain = 0f;   _targetFog = 0.85f; _targetCloud = 0.50f; break;
                case Weather.Rain:     _targetRain = 0.6f; _targetFog = 0.28f; _targetCloud = 0.75f; break;
                case Weather.Storm:    _targetRain = 1.0f; _targetFog = 0.40f; _targetCloud = 0.95f; break;
            }
        }

        /// <summary>Restores the weather from a saved game (holds the saved intensities so its own
        /// Start won't reset it to Clear).</summary>
        public void Restore(Weather weather, float rain, float fog, float cloud)
        {
            _restored = true;
            Current = weather;
            RainIntensity = _targetRain = Mathf.Clamp01(rain);
            FogIntensity = _targetFog = Mathf.Clamp01(fog);
            Cloudiness = _targetCloud = Mathf.Clamp01(cloud);
            _stateTimer = Random.Range(CloneZConfig.WeatherMinDuration, CloneZConfig.WeatherMaxDuration);
            ApplyFog();
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
            float fog = Mathf.Max(FogIntensity, RainIntensity * 0.35f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = Mathf.Lerp(35f, 6f, fog);
            RenderSettings.fogEndDistance = Mathf.Lerp(78f, 28f, fog);
        }

        /// <summary>Extra dimming from cloud cover, fed into the day/night light level.</summary>
        public float LightMultiplier => Mathf.Lerp(1f, 0.5f, Cloudiness);

        /// <summary>Sight radius scale for zombies: heavy rain and fog shorten their vision.</summary>
        public float VisionMultiplier =>
            (1f - CloneZConfig.ZombieRainVisionPenalty * RainIntensity) *
            (1f - CloneZConfig.ZombieFogVisionPenalty * FogIntensity);

        public string Label
        {
            get
            {
                switch (Current)
                {
                    case Weather.Cloudy: return "Cloudy";
                    case Weather.Overcast: return "Overcast";
                    case Weather.Fog: return "Fog";
                    case Weather.Rain: return "Rain";
                    case Weather.Storm: return "Storm";
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
                Vector3 p = _cam.transform.position;
                _rain.transform.position = new Vector3(p.x, p.y, p.z);
            }
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
