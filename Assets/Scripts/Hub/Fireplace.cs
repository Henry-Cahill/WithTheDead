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
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_isLit)
                into.Add(new InteractAction("Extinguish", c => SetLit(false)));
            else
                into.Add(new InteractAction("Light fireplace", c => SetLit(true)));
        }

        private void SetLit(bool on)
        {
            _isLit = on;
            if (_flameLight != null) _flameLight.enabled = on;
            // simple sound; UI click works even if a dedicated fire sfx isn’t defined yet
            Sfx.PlayUI(SfxIds.UiClick);
            GameEvents.RaiseToast(on ? "The fire crackles warmly." : "You smother the flames.");
        }

        public override void CaptureState(FixtureSave save) => save.State = _isLit ? 1 : 0;

        public override void RestoreState(FixtureSave save)
        {
            _isLit = (save.State & 1) != 0;
            if (_flameLight != null) _flameLight.enabled = _isLit;
        }
    }
}
