using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Toggleable heater that emits a warm point‑light when on.</summary>
    public class Heater : Interactable, IWarmthSource
    {
        private const string LightName = "HeaterLight";
        private Light _light;
        private bool _isOn;

        public override string DisplayName => "Heater";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 0.5f;
        public override float Reach => 2.0f;
        public override string SaveKey => Interactable.KeyFor("heater", transform.position);

        // Persistence
        public override void CaptureState(FixtureSave save) => save.State = _isOn ? 1 : 0;
        public override void RestoreState(FixtureSave save)
        {
            _isOn = save.State == 1;
            UpdateLight();
        }

        private void Awake()
        {
            var go = new GameObject(LightName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            _light = go.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = 3f;
            _light.intensity = 2.5f;
            _light.color = new Color(1f, 0.75f, 0.45f);
            _light.enabled = false;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            string label = _isOn ? "Turn heater off" : "Turn heater on";
            into.Add(new InteractAction(label, c => Toggle()));
        }

        private void Toggle()
        {
            _isOn = !_isOn;
            HubRules.HeaterOn = _isOn;
            UpdateLight();
            Sfx.PlayUI(SfxIds.UiClick);
            GameEvents.RaiseToast(_isOn ? "The heater glows warm, chasing the cold." : "The heater clicks off, the hub feels chillier.");
        }

        private void UpdateLight()
        {
            if (_light != null) _light.enabled = _isOn;
        }

        // IWarmthSource implementation – simple constant heat when on
        public Vector3 WarmthPosition => transform.position + new Vector3(0f, 0.3f, 0f);
        public float WarmthRadius => 2f;
        public float Warmth => _isOn ? 0.3f : 0f;

        protected override void OnEnable() => HomeComfort.Register(this);
        protected override void OnDisable() => HomeComfort.Unregister(this);
    }
}
