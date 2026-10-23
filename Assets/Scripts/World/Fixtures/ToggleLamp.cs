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

        public override string DisplayName => "Wooden lamp";
        public override string SaveKey => KeyFor("togglelamp", transform.position);

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
            if (_light != null) _light.enabled = _isOn;
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
            }
        }

        public override void CaptureState(FixtureSave save)
        {
            // Store on/off flag in the low bit of the State field
            save.State = _isOn ? 1 : 0;
        }

        public override void RestoreState(FixtureSave save)
        {
            if (save == null) return;
            _isOn = (save.State & 1) != 0;
            if (_light != null) _light.enabled = _isOn;
        }
    }
}
