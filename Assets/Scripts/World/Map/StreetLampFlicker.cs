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
        }
        private void Update()
        {
            // slow sinusoidal wobble ±15 %
            float wobble = 0.15f * Mathf.Sin(Time.time * 0.6f + _phase);
            _light.intensity = _baseIntensity * (1f + wobble);
        }
    }
}
