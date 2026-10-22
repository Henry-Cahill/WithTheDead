using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Interaction component for a street lamp. Allows the player to toggle the lamp on/off with [E].
    /// The lamp is always powered (city lighting), so toggling only affects the Light component and
    /// optionally the attached StreetLampFlicker.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class StreetLampInteraction : Interactable
    {
        private Light _light;
        private StreetLampFlicker _flicker;
        private string _key;
        private string _displayName = "Street Lamp";

        private void Awake()
        {
            _light = GetComponent<Light>();
            _flicker = GetComponent<StreetLampFlicker>();
            _key = KeyFor("streetlamp", transform.position);
        }

        public override string DisplayName => _displayName;
        public override Vector3 InteractPoint => transform.position;
        public override Transform OcclusionRoot => transform;
        public override string SaveKey => _key;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool on = _light != null && _light.enabled;
            into.Add(new InteractAction(on ? "Turn off street lamp" : "Turn on street lamp", c => Toggle(c, !on)));
        }

        private void Toggle(InteractContext ctx, bool on)
        {
            if (_light != null) _light.enabled = on;
            if (_flicker != null) _flicker.enabled = on; // disable flicker when lamp off
            ctx.Notify(on ? "The street lamp clicks on." : "The street lamp clicks off.");
        }

        public override void CaptureState(FixtureSave save)
        {
            // store on/off as bit 0
            int state = 0;
            if (_light != null && _light.enabled) state |= 1;
            save.State = state;
        }

        public override void RestoreState(FixtureSave save)
        {
            bool on = (save.State & 1) != 0;
            if (_light != null) _light.enabled = on;
            if (_flicker != null) _flicker.enabled = on;
        }
    }
}
