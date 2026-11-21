using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The light of a lamp over a front door: lit after dark, out by day, with a faint flicker. The lamps keep no Update
    /// of their own: one driver works them all.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PorchLamp : MonoBehaviour
    {
        private const float FlickerSpeed = 8f;   // how fast the flicker's noise runs
        private const float FlickerDepth = 0.1f;   // the flicker's swing, as a fraction of the lamp's output

        private static readonly List<PorchLamp> s_All = new List<PorchLamp>();
        private static PorchLampDriver s_Driver;

        private Light _light;
        private float _intensity, _phase;

        /// <summary>A porch lamp's light at <paramref name="at"/> (world) under <paramref name="parent"/>;
        /// <paramref name="phase"/> keeps it from flickering in step with its neighbours.</summary>
        internal static PorchLamp Spawn(Transform parent, Vector3 at, float phase)
        {
            var go = new GameObject("PorchLamp");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.intensity = 2f;
            light.range = 6f;
            light.shadows = LightShadows.Soft;
            light.enabled = false;   // until the driver finds it dark out
            var lamp = go.AddComponent<PorchLamp>();
            lamp._light = light;
            lamp._intensity = light.intensity;
            lamp._phase = phase;
            return lamp;
        }

        private void OnEnable()
        {
            s_All.Add(this);
            if (s_Driver == null) s_Driver = new GameObject("PorchLamps").AddComponent<PorchLampDriver>();
        }

        private void OnDisable() => s_All.Remove(this);

        /// <summary>Lights or puts out every porch lamp for the time of day, and flickers the lit ones.</summary>
        internal static void TickAll()
        {
            TimeOfDay day = TimeOfDay.Instance;
            bool night = day != null && day.IsNight;
            float time = Time.time;
            for (int i = 0; i < s_All.Count; i++) s_All[i].Tick(night, time);
        }

        private void Tick(bool night, float time)
        {
            if (_light == null) return;
            if (_light.enabled != night) _light.enabled = night;
            if (night)
                _light.intensity = _intensity * (1f - FlickerDepth * 0.5f + FlickerDepth * Mathf.PerlinNoise((time + _phase) * FlickerSpeed, 0f));
        }
    }

    /// <summary>Works every <see cref="PorchLamp"/>, once a frame; made with the first lamp, gone with the scene.</summary>
    internal sealed class PorchLampDriver : MonoBehaviour
    {
        private void Update() => PorchLamp.TickAll();
    }
}
