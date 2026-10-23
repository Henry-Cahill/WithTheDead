using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Toggleable heater that emits a warm point‑light when on.</summary>
    public class Heater : Interactable, IWarmthSource
    {
        private const string LightName = "HeaterLight";
        private const float DefaultFuelSeconds = 300f; // 5 min of heat when first turned on
        private const float MaxFuelSeconds = 1800f; // 30 min cap
        private const float FuelPerRefill = 300f; // 5 min added per refuel
        private float _fuelSeconds; // remaining fuel while on
        private Light _light;
        private TextMesh _timerText;
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
            UpdateTimerDisplay();
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
                    _fuelSeconds = DefaultFuelSeconds;
            // create a 3‑D text display above the heater
            var txtGo = new GameObject("HeaterTimer");
            txtGo.transform.SetParent(transform, false);
            txtGo.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var txt = txtGo.AddComponent<TextMesh>();
            _timerText = txt;
            _timerText.fontSize = 32;
            _timerText.alignment = TextAlignment.Center;
            _timerText.anchor = TextAnchor.MiddleCenter;
            _timerText.color = new Color(1f, 0.9f, 0.6f);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            string label = _isOn ? "Turn heater off" : "Turn heater on";
            into.Add(new InteractAction(label, c => Toggle()));
            into.Add(new InteractAction("Add fuel (5 min)", c => AddFuel()));
        }

        private void Toggle()
        {
            _isOn = !_isOn;
            HubRules.HeaterOn = _isOn;
            if (_isOn && _fuelSeconds <= 0f)
            {
                // fresh start – give the default amount of fuel
                _fuelSeconds = DefaultFuelSeconds;
            }
            UpdateLight();
            UpdateTimerDisplay();
            Sfx.PlayUI(SfxIds.UiClick);
            GameEvents.RaiseToast(_isOn ? "The heater glows warm, chasing the cold." : "The heater clicks off, the hub feels chillier.");
        }

        private void Update()
        {
            if (_isOn)
            {
                _fuelSeconds -= Time.deltaTime;
                if (_fuelSeconds <= 0f)
                {
                    _fuelSeconds = 0f;
                    _isOn = false;
                    HubRules.HeaterOn = false;
                    UpdateLight();
                    Sfx.PlayUI(SfxIds.UiClick);
                    GameEvents.RaiseToast("The heater sputters out as its fuel is spent.");
                }
                UpdateTimerDisplay();
            }
        }

        private void UpdateTimerDisplay()
        {
            var txt = _timerText;
            if (txt != null)
            {
                if (_isOn)
                {
                    int secs = Mathf.CeilToInt(_fuelSeconds);
                    txt.text = $"Heat: {secs/60:D2}:{secs%60:D2}";
                }
                else
                {
                    txt.text = "Heater off";
                }
            }
        }

        private void AddFuel()
        {
            float before = _fuelSeconds;
            _fuelSeconds = Mathf.Min(_fuelSeconds + FuelPerRefill, MaxFuelSeconds);
            if (_fuelSeconds > before)
            {
                Sfx.PlayUI(SfxIds.UiClick);
                GameEvents.RaiseToast("You add kindling to the heater, extending its warmth.");
                UpdateTimerDisplay();
            }
        }

        private void UpdateLight()
        {
            if (_light != null) _light.enabled = _isOn;
        }
        public Vector3 WarmthPosition => transform.position + new Vector3(0f, 0.3f, 0f);
        public float WarmthRadius => 2f;
        public float Warmth => _isOn ? 0.3f : 0f;

        protected override void OnEnable() => HomeComfort.Register(this);
        protected override void OnDisable() => HomeComfort.Unregister(this);
    }
}
