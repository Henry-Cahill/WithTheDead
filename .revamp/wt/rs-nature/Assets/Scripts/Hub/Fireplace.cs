using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Simple fireplace that can be lit or extinguished; provides a warm point‑light when lit.
    /// </summary>
    [DisallowMultipleComponent]
    public class Fireplace : Interactable
    {
        private const string STATE_KEY = "Lit"; // not used directly but kept for parity
        private Light _flameLight;
        private bool _isLit;
        private const float DEFAULT_FUEL_SECONDS = 300f; // 5 minutes of fire
        private const int LIT_BIT = 1; // bit 0
        private const int FUEL_SHIFT = 1; // bits 1‑… store fuel seconds (int)
        private float _fuelSeconds; // remaining fuel while lit
        private ParticleSystem _smoke; // visual smoke
        private string _key;

        public override string DisplayName => "Fireplace";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 0.9f;
        public override string SaveKey => _key;

        private void Awake()
        {
            // Generate a stable save key based on position
            _key = KeyFor(nameof(Fireplace), transform.position);
            // Child light that represents the flame
            var lightObj = new GameObject("FlameLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            _flameLight = lightObj.AddComponent<Light>();
            _flameLight.type = LightType.Point;
            _flameLight.color = new Color(1f, 0.6f, 0.2f);
            _flameLight.intensity = 2f;
            _flameLight.range = 4f;
            _flameLight.enabled = false;
            // initialise fuel timer
            _fuelSeconds = DEFAULT_FUEL_SECONDS;
            // create smoke particle system
            var smokeObj = new GameObject("Smoke");
            smokeObj.transform.SetParent(transform, false);
            smokeObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            _smoke = smokeObj.AddComponent<ParticleSystem>();
            var main = _smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            var emission = _smoke.emission;
            emission.rateOverTime = 5f;
            var shape = _smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.3f;
            _smoke.Stop();
        }

        // Subtle flame flicker effect – varies intensity each frame when lit
        private void Update()
        {
            if (!_isLit || _flameLight == null) return;
            // Per-frame flicker and fuel consumption only while lit (the flame light is off otherwise)
            // Perlin noise creates a gentle, non‑repeating variation
            float f = Mathf.PerlinNoise(Time.time * 5f, 0f) * 0.3f + 0.85f;
            _flameLight.intensity = 2f * f;
            // fuel consumption
            _fuelSeconds -= Time.deltaTime;
            if (_fuelSeconds <= 0f)
            {
                SetLit(false);
                GameEvents.RaiseToast("The fire sputters out as it runs out of fuel.");
            }
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_isLit)
                into.Add(new InteractAction("Extinguish", c => SetLit(false)));
            else
                into.Add(new InteractAction("Light fireplace", c => SetLit(true)));

            // Inspect fire action
            into.Add(new InteractAction("Inspect fire", c =>
            {
                int minutes = Mathf.CeilToInt(_fuelSeconds / 60f);
                c.Notify(_isLit ? $"The fire will keep burning for about {minutes} minute{(minutes == 1 ? "" : "s")}." : "The fireplace is dark.");
            }));
        }

        private void SetLit(bool on)
        {
            _isLit = on;
            if (on)
            {
                if (_fuelSeconds <= 0f) _fuelSeconds = DEFAULT_FUEL_SECONDS;
                if (_flameLight != null) _flameLight.enabled = true;
                if (_smoke != null) _smoke.Play();
            }
            else
            {
                if (_flameLight != null) _flameLight.enabled = false;
                if (_smoke != null) _smoke.Stop();
            }
            // simple sound; UI click works even if a dedicated fire sfx isn’t defined yet
            Sfx.PlayUI(SfxIds.UiClick);
            GameEvents.RaiseToast(on ? "The fire crackles warmly." : "You smother the flames.");
        }

        public override void CaptureState(FixtureSave save)
        {
            int fuelInt = Mathf.RoundToInt(_fuelSeconds);
            save.State = (_isLit ? LIT_BIT : 0) | (fuelInt << FUEL_SHIFT);
        }

        public override void RestoreState(FixtureSave save)
        {
            _isLit = (save.State & LIT_BIT) != 0;
            _fuelSeconds = (save.State >> FUEL_SHIFT);
            if (_flameLight != null) _flameLight.enabled = _isLit;
            if (_smoke != null)
            {
                if (_isLit) _smoke.Play(); else _smoke.Stop();
            }
        }
    }
}
