using UnityEngine;
using CloneZ;

namespace CloneZ
{
    /// <summary>
    /// Gives a low‑frequency flicker to street‑lamp lights for a lived‑in winter look.
    /// </summary>
    internal sealed class StreetLampFlicker : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;
        private float _phase;
        private float _dipTimer;
        /// <summary>Maximum sinusoidal intensity swing as a fraction of the base intensity (e.g. 0.15 = ±15 %).</summary>
        [SerializeField, Range(0f, 0.5f)] public float _maxSinusoid = 0.15f;
        /// <summary>Low‑frequency wobble speed in Hz.</summary>
        [SerializeField, Range(0.1f, 2f)] public float _wobbleSpeed = 0.6f;
        /// <summary>Strength of occasional “dip” events (0 = none, 1 = full blackout).</summary>
        [SerializeField, Range(0f, 1f)] public float _dipStrength = 0.3f;
        private void Awake()
        {
            _light = GetComponent<Light>();
            if (_light == null) enabled = false;
            else _baseIntensity = _light.intensity;
        }
        private void Start()
        {
            // deterministic phase from the lamp’s position hash
            int hash = transform.position.GetHashCode();
            _phase = (hash & 0xFFFF) / (float)0xFFFF * Mathf.PI * 2f;
            // small per‑lamp frequency jitter (±10 %)
            float freqJitter = 0.9f + (hash & 0xFF) / 255f * 0.2f;
            _wobbleSpeed *= freqJitter;
            // seed for dip timing (0–1)
            _dipTimer = (hash & 0xFFFF) / (float)0xFFFF;
        }

        private void Update()
        {
            // sinusoidal wobble
            float sinWobble = _maxSinusoid * Mathf.Sin(Time.time * _wobbleSpeed + _phase);
            // occasional dip using Perlin noise (produces smooth darkening spikes)
            float dip = Mathf.PerlinNoise(Time.time * 0.1f + _dipTimer, 0f);
            dip = Mathf.Clamp01((dip - 0.7f) * 5f); // only act on the top 30 %
            dip *= _dipStrength; // scale strength
            float intensityFactor = 1f + sinWobble - dip;
            _light.intensity = _baseIntensity * intensityFactor;
        }
/// <summary>
    /// Enables or disables the flicker effect without destroying the component.
    /// </summary>
    public void SetFlickerEnabled(bool enabled)
    {
        this.enabled = enabled;
        // when disabled we restore the original base intensity
        if (!enabled && _light != null) _light.intensity = _baseIntensity;
    }
}
}
