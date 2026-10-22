using UnityEngine;
using CloneZ;
using System.Collections.Generic;

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
        private static readonly float CullDistance = 64f; // metres from player before flicker is disabled
        internal static readonly HashSet<StreetLampFlicker> s_registered = new();
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
            UpdateCulling(); // initial enable/disable based on player distance
        }

        private void OnEnable()
        {
            s_registered.Add(this);
            UpdateCulling(); // set correct enabled state on spawn
        }

        private void OnDisable()
        {
            s_registered.Remove(this);
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
            // per‑lamp random variation (±10 %)
            float sinVar = UnityEngine.Random.Range(0.9f, 1.1f);
            float dipVar = UnityEngine.Random.Range(0.9f, 1.1f);
            _maxSinusoid *= sinVar;
            _dipStrength *= dipVar;
        }

        private static PlayerController s_player;

        private void UpdateCulling()
        {
            if (s_player == null)
                s_player = UnityEngine.Object.FindObjectOfType<PlayerController>();
            if (s_player == null) return;
            bool shouldEnable = Vector3.Distance(transform.position, s_player.transform.position) <= CullDistance;
            SetFlickerEnabled(shouldEnable);
        }
        private void Update()
        {
            UpdateCulling();
            // sinusoidal wobble
            float sinWobble = _maxSinusoid * Mathf.Sin(Time.time * _wobbleSpeed + _phase);
            // occasional dip using Perlin noise (produces smooth darkening spikes)
            float dip = Mathf.PerlinNoise(Time.time * 0.1f + _dipTimer, 0f);
            dip = Mathf.Clamp01((dip - 0.7f) * 5f); // only act on the top 30 %
            dip *= _dipStrength; // scale strength
            float intensityFactor = 1f + sinWobble - dip;
            _light.intensity = _baseIntensity * intensityFactor;
        }
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
