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
        private Renderer _poleRenderer;
        private bool _isPrimaryRoad;
        private float _baseIntensity;
        private float _phase;
        private float _dipTimer;
        private Quaternion _originalLocalRot;
        private static readonly float CullDistance = 64f; // metres from player before flicker is disabled
        internal static readonly HashSet<StreetLampFlicker> s_registered = new();
        /// <summary>Maximum intensity reduction caused by ice buildup (0‑0.5 = up to 50 % dim).</summary>
        [SerializeField, Range(0f, 0.5f)] public float _maxIceDim = 0.15f;
        /// <summary>Maximum sinusoidal intensity swing as a fraction of the base intensity (e.g. 0.15 = ±15 %).</summary>
        [SerializeField, Range(0f, 0.5f)] public float _maxSinusoid = 0.15f;
        [SerializeField, Range(0.1f, 2f)] public float _wobbleSpeed = 0.6f;
        /// <summary>Maximum sway angle in degrees (± from upright).</summary>
        [SerializeField, Range(0f, 10f)] public float _swayAmplitudeDeg = 2f;
        /// <summary>Sway speed in Hz.</summary>
        [SerializeField, Range(0.05f, 2f)] public float _swaySpeedHz = 0.3f;
        /// <summary>Strength of occasional “dip” events (0 = none, 1 = full blackout).</summary>
        [SerializeField, Range(0f, 1f)] public float _dipStrength = 0.3f;
        private float _iceDimFactor; // computed from ice cluster count

        private void ApplyRoadVisuals()
        {
            // primary roads get a slightly stronger flicker and a darker pole
            if (_isPrimaryRoad)
            {
                _maxSinusoid += 0.05f; // boost sinusoidal swing
                _swayAmplitudeDeg += 0.5f; // a little more sway
                if (_poleRenderer != null) _poleRenderer.material.color *= 0.75f; // darken pole colour
            }
            else
            {
                // side streets keep the default values, but brighten the pole
                if (_poleRenderer != null) _poleRenderer.material.color *= 1.15f; // lighten pole colour
            }
        }

        private void Awake()
        {
            _light = GetComponent<Light>();
            if (_light == null) { enabled = false; return; }
            else _baseIntensity = _light.intensity;

            // compute ice dim factor based on IceCluster child count
            int icicleCount = 0;
            var iceCluster = transform.Find("IceCluster");
            if (iceCluster != null)
            {
                foreach (Transform ic in iceCluster)
                {
                    if (ic.name.StartsWith("Icicle_")) icicleCount++;
                }
                // scale dim factor proportionally (max at 4 icicles)
                _iceDimFactor = (icicleCount / 4f) * _maxIceDim;
            }

            // retrieve road‑type metadata
            var meta = GetComponent<StreetLampMetadata>();
            _isPrimaryRoad = meta != null && meta.IsPrimaryRoad;
            // find the pole mesh (expected child named "Pole") and cache its renderer
            var pole = transform.Find("Pole");
            if (pole != null) _poleRenderer = pole.GetComponent<Renderer>();
            ApplyRoadVisuals();

            _originalLocalRot = transform.localRotation; // capture original rotation
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
            // jitter sway speed the same way wobble speed is jittered
            float swayJitter = 0.9f + (hash & 0xFF) / 255f * 0.2f;
            _swaySpeedHz *= swayJitter;
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
            // sway side‑to‑side
            float swayAngle = Mathf.Sin(Time.time * _swaySpeedHz + _phase) * _swayAmplitudeDeg;
            transform.localRotation = _originalLocalRot * Quaternion.Euler(0f, 0f, swayAngle);
            // occasional dip using Perlin noise (produces smooth darkening spikes)
            float dip = Mathf.PerlinNoise(Time.time * 0.1f + _dipTimer, 0f);
            dip = Mathf.Clamp01((dip - 0.7f) * 5f); // only act on the top 30 %
            dip *= _dipStrength; // scale strength
            float intensityFactor = 1f + sinWobble - dip - _iceDimFactor; // apply ice dimming
            _light.intensity = _baseIntensity * intensityFactor;
        }

        public void SetFlickerEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled && _light != null)
            {
                _light.intensity = _baseIntensity;
                transform.localRotation = _originalLocalRot; // reset sway
                if (_poleRenderer != null)
                    _poleRenderer.material.color = Color.white;
            }
        }
    }
}