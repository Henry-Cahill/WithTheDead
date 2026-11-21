using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Wooden lamp that can be switched on/off; emits a point light when on.
    /// </summary>
    public sealed class ToggleLamp : Interactable
    {
        private Light _light;
        private bool _isOn;
        private const float MaxFuel = 1f; // full tank (0-1)
        private const float FuelConsumptionPerSecond = 0.02f; // 2 % per second
        private float _fuel = MaxFuel;
        private float _fuelTimer;

        public override string DisplayName => "Wooden lamp";
        public override string SaveKey => KeyFor("togglelamp", transform.position);


        private void Update()
        {
            if (!_isOn || _fuel <= 0f) return;
            _fuelTimer += Time.deltaTime;
            if (_fuelTimer >= 1f)
            {
                _fuelTimer = 0f;
                _fuel = Mathf.Max(0f, _fuel - FuelConsumptionPerSecond);
                if (_fuel <= 0f)
                {
                    _isOn = false;
                    if (_light != null) _light.enabled = false;
                    MakeNoise(1f);
                    // toast to player via context later
                }
            }
        }

        private void Awake()
        {
            // Ensure a child Light component exists
            var child = transform.Find("LampLight");
            if (child == null)
            {
                var go = new GameObject("LampLight");
                go.transform.SetParent(transform, false);
                _light = go.AddComponent<Light>();
                _light.type = LightType.Point;
                _light.range = 5f;
                _light.intensity = 2f;
            }
            else
            {
                _light = child.GetComponent<Light>();
            }
            if (_light != null) {
                _light.enabled = _isOn && _fuel > 0f;
                _light.intensity = 2f * _fuel;
            }
            _fuelTimer = 0f;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            AddInspectAction(ctx, into);
            if (_isOn)
            {
                into.Add(new InteractAction("Turn off", c =>
                {
                    _isOn = false;
                    if (_light != null) _light.enabled = false;
                    c.Notify("You turn off the wooden lamp.");
                    MakeNoise(1f);
                }));
            }
            else
            {
                into.Add(new InteractAction("Turn on", c =>
                {
                    _isOn = true;
                    if (_light != null) _light.enabled = true;
                    c.Notify("You turn on the wooden lamp.");
                    MakeNoise(1f);
                }));
            // Refuel action
            if (ctx.Inventory != null && ctx.Inventory.Has("lamp_oil"))
            {
                into.Add(new InteractAction("Refuel lamp", c =>
                {
                    _fuel = MaxFuel;
                    ctx.Inventory.Consume("lamp_oil", 1);
                    c.Notify("You pour lamp oil into the wooden lamp. It glows brighter.");
                    MakeNoise(1f);
                }));
            }
            }
        }

        public override void CaptureState(FixtureSave save)
        {
            // Store on/off flag in the low bit of the State field
            int fuelByte = (int)(Mathf.Clamp01(_fuel) * 255);
            save.State = (_isOn ? 1 : 0) | (fuelByte << 1);
        }

        public override void RestoreState(FixtureSave save)
        {
            if (save == null) return;
            _isOn = (save.State & 1) != 0;
            int fuelByte = (save.State >> 1) & 0xFF;
            _fuel = fuelByte / 255f;
            if (_light != null) {
                _light.enabled = _isOn && _fuel > 0f;
                _light.intensity = 2f * _fuel;
            }
            _fuelTimer = 0f;
        }
    }
}
