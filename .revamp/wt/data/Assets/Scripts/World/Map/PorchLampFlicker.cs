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
        private TimeOfDay _timeOfDay;
        private float _baseIntensity;
        private float _phaseOffset; // random per‑lamp offset for Perlin noise
        private const float _flickerSpeed = 8f; // how fast the noise cycles
        private bool _wasNight; // remembers previous night state to avoid redundant work
        /// <summary>True while the lamp is currently lit (night‑time).</summary>
        public bool IsActive => _light != null && _light.enabled;

void Awake()
{
    // Cache the Light component on the child lamp object.
    _light = GetComponentInChildren<Light>();
    // Cache the singleton TimeOfDay once.
    _timeOfDay = TimeOfDay.Instance;
    // Preserve the lamp's original intensity for later scaling.
    if (_light != null)
        _baseIntensity = _light.intensity;
    // Give this lamp a unique per‑lamp phase so all lamps do not flicker in lock‑step.
    _phaseOffset = UnityEngine.Random.Range(0f, 1000f);
    // Initialise night‑state tracking.
    _wasNight = _timeOfDay?.IsNight ?? false;
}

        private void ApplyFlicker()
        {
            // Perlin noise varies between 0‑1; map to ± 4 % around base intensity.
            float noise = Mathf.PerlinNoise((Time.time + _phaseOffset) * _flickerSpeed, 0f);
            // Scale to 0.95‑1.05 range for a subtle flicker.
            _light.intensity = _baseIntensity * (0.95f + 0.1f * noise);
        }

        void Update()
        {
            // Guard against missing TimeOfDay reference.
            bool isNight = _timeOfDay?.IsNight ?? false;
            // Turn the lamp on/off only when the night state changes.
            if (isNight != _wasNight)
            {
                if (isNight)
                {
                    if (_light != null) _light.enabled = true;
                }
                else
                {
                    if (_light != null) _light.enabled = false;
                }
                _wasNight = isNight;
            }
            // Apply flicker every frame while it is night and the light is enabled.
            if (isNight && _light != null && _light.enabled)
            {
                ApplyFlicker();
            }
        }
    }
}
