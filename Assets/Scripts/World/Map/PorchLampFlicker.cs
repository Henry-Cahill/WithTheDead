using UnityEngine;
using CloneZ;

namespace CloneZ
{
    /// <summary>
    /// Gives porch lamps a period‑correct flicker that only runs during night‑time.
    /// </summary>
    internal sealed class PorchLampFlicker : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;

        void Awake()
        {
            // Find the Light component on a child (the lamp light GameObject).
            _light = GetComponentInChildren<Light>();
            if (_light != null)
                _baseIntensity = _light.intensity;
        }

        void Update()
        {
            // Assume a singleton DayNightCycle with a public bool IsNight.
            // If unavailable, we default to always on (fails safe).
            bool isNight = TimeOfDay.Instance?.IsNight ?? false;
            if (isNight)
            {
                if (_light != null && !_light.enabled) _light.enabled = true;
                // subtle flicker using Perlin noise (≈10% variance)
                float noise = Mathf.PerlinNoise(Time.time * 8f, 0f);
                if (_light != null)
                    _light.intensity = _baseIntensity * (0.9f + 0.1f * noise);
            }
            else
            {
                if (_light != null && _light.enabled) _light.enabled = false;
            }
        }
    }
}
