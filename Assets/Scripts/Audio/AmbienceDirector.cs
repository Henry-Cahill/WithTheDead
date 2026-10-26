using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The sound of the world around the survivor: a bed of wind that rises with the weather and drops indoors, birds
    /// in the trees by day, the murmur and glasses of the bar in the hub; wolves far off at night, a deer by day.
    /// </summary>
    public sealed class AmbienceDirector : MonoBehaviour
    {
        private static AmbienceDirector s_Instance;

        private AudioSource _wind, _forest, _bar;
        private WeatherSystem _weather;
        private TimeOfDay _timeOfDay;
        private float _animalTimer = 40f;

        /// <summary>Ensures a single director exists (it outlives scenes).</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[AmbienceDirector]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<AmbienceDirector>();
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Actor local = Actor.LocalPlayer;
            Vector3 ears = ListenerRig.Active ? ListenerRig.Position : (local != null ? local.transform.position : Vector3.zero);
            bool paused = Time.timeScale <= 0f;

            float wind = 0.16f, forest = 0f, bar = 0f;
            if (GameSession.IsHub && local != null)
            {
                wind = 0.04f;
                bar = 0.4f;
            }
            else if (local != null)
            {
                if (_weather == null) _weather = WeatherSystem.Instance;
                if (_weather != null)
                {
                    switch (_weather.Current)
                    {
                        case Weather.Cloudy: wind = 0.22f; break;
                        case Weather.Overcast: wind = 0.28f; break;
                        case Weather.Fog: wind = 0.12f; break;
                        case Weather.Rain: wind = 0.34f; break;
                        case Weather.Storm: wind = 0.55f; break;
                    }
                }
                // The clock is built with each world, after this director (it outlives scenes), so look again until found.
                if (_timeOfDay == null) _timeOfDay = TimeOfDay.Instance;
                bool night = _timeOfDay != null && _timeOfDay.IsNight;
                float trees = ZoneForest.Density(ears);
                forest = night ? 0f : Mathf.Clamp01(trees * 1.4f) * 0.4f + (GameSession.IsExtraction ? 0.05f : 0f);
                bool indoors = Shelter.Count > 0 && Shelter.IsSheltered(ears);
                if (indoors) { wind *= 0.3f; forest *= 0.25f; }
            }

            Bed(ref _wind, SfxIds.WindAmbience, wind, dt);
            Bed(ref _forest, SfxIds.ForestAmbience, forest, dt);
            Bed(ref _bar, SfxIds.BarAmbience, bar, dt);

            if (paused || local == null || GameSession.IsHub) return;
            Animals(ears, dt);
        }

        // A flat looping bed glided toward its target. It starts (silent) once its sound is built, so nothing is
        // rendered on the main thread for it.
        private static void Bed(ref AudioSource src, string id, float target, float dt)
        {
            if (src == null)
            {
                if (target <= 0f) return;
                if (!Sfx.Ready(id)) { Sfx.Request(id); return; }
                src = Sfx.LoopFlat(id, 0f);
                if (src == null) return;
            }
            Sfx.SetVolume(src, Mathf.MoveTowards(Sfx.GetVolume(src), target, dt * 0.25f));
        }

        // Far-off animals: wolves at night, a deer by day in the trees. Placed well away from the survivor so they
        // read as the landscape, not a threat.
        private void Animals(Vector3 ears, float dt)
        {
            if (!GameSession.IsExtraction) return;
            _animalTimer -= dt;
            if (_animalTimer > 0f) return;
            _animalTimer = Random.Range(45f, 120f);
            bool night = _timeOfDay != null && _timeOfDay.IsNight;
            Vector2 o = Random.insideUnitCircle.normalized * Random.Range(35f, 60f);
            Vector3 at = ears + new Vector3(o.x, 2f, o.y);
            if (night) Sfx.Play(SfxIds.WolfHowl, at, 0.45f, Random.Range(0.9f, 1.08f), 90f);
            else if (ZoneForest.Density(ears) > 0.25f) Sfx.Play(SfxIds.DeerCall, at, 0.35f, Random.Range(0.9f, 1.1f), 70f);
        }
    }
}
