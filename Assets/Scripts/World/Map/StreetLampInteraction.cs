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
        private bool _flickerOn;
        private string _displayName = "Street Lamp";

        private void Awake()
        {
            _light = GetComponent<Light>();
            _flicker = GetComponent<StreetLampFlicker>();
            _key = KeyFor("streetlamp", transform.position);
            _flickerOn = _flicker != null && _flicker.enabled;
            if (_flicker != null) _flicker.enabled = true; // always keep component alive
        }

        public override string DisplayName => _displayName;
        public override Vector3 InteractPoint => transform.position;
        public override Transform OcclusionRoot => transform;
        public override string SaveKey => _key;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool on = _light != null && _light.enabled;
            into.Add(new InteractAction(on ? "Turn off street lamp" : "Turn on street lamp", c => Toggle(c, !on)));
            if (on && _flicker != null)
            {
                string flickerLabel = _flickerOn ? "Turn off flicker" : "Turn on flicker";
                into.Add(new InteractAction(flickerLabel, c => ToggleFlicker(c, !_flickerOn)));
            }
        }

        private void Toggle(InteractContext ctx, bool on)
        {
            if (_light != null) _light.enabled = on;
            if (_flicker != null)
            {
                _flicker.enabled = on; // keep component alive only when lamp on
                _flicker.SetFlickerEnabled(on && _flickerOn);
            }
            ctx.Notify(on ? "The street lamp clicks on." : "The street lamp clicks off.");
        }







        public override void CaptureState(FixtureSave save)
        {
            // store on/off as bit 0 and flicker flag as bit 1
            int state = 0;
            if (_light != null && _light.enabled) state |= 1; // bit 0 = lamp on
            if (_flickerOn) state |= 2; // bit 1 = flicker on
            save.State = state;
        }

        public override void RestoreState(FixtureSave save)
        {
            bool on = (save.State & 1) != 0;
            bool flickerOn = (save.State & 2) != 0;
            if (_light != null) _light.enabled = on;
            if (_flicker != null)
            {
                _flicker.enabled = on;
                _flickerOn = flickerOn;
                _flicker.SetFlickerEnabled(on && flickerOn);
            }
        }
        private void ToggleFlicker(InteractContext ctx, bool on)
        {
            _flickerOn = on;
            if (_flicker != null)
            {
                _flicker.SetFlickerEnabled(on);
                // make a brief audible click; radius 2f matches other lamp actions
                MakeNoise(2f);
            }
            ctx.Notify(on ? "The lamp begins to flicker." : "The lamp steadies.");
        }
    }
}
