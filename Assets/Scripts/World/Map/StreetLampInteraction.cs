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
        private bool _dimmed;
        private string _displayName = "Street Lamp";

        private const float BLOCK_TOGGLE_RADIUS = 30f; // metres


        // Used by block‑toggle to set lamp state without duplicate messages.
        internal void ApplyState(bool on, InteractContext ctx = null)
        {
            if (_light != null) _light.enabled = on;
            if (on && _light != null)
            {
                _light.intensity = _dimmed ? 0.2f : 1f;
            }
            if (_flicker != null)
            {
                _flicker.enabled = on;
                _flicker.SetFlickerEnabled(on && _flickerOn);
            }
            // No per‑lamp notification when called from block toggle (ctx may be null)
        }

        private void Toggle(InteractContext ctx, bool on)
        {
            ApplyState(on, ctx);
            MakeNoise(2f);
            ctx.Notify(on ? "The street lamp clicks on." : "The street lamp clicks off.");
        }

    private void Awake()
    {
        _light = GetComponent<Light>();
        _flicker = GetComponent<StreetLampFlicker>();
        _key = KeyFor("streetlamp", transform.position);
        _flickerOn = _flicker != null && _flicker.enabled;
        if (_flicker != null) _flicker.enabled = true; // always keep component alive
        StreetLampRegistry.Register(this); // <-- new line
    }

        private void OnDestroy()
        {
            StreetLampRegistry.Unregister(this);
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
            if (on)
            {
                into.Add(new InteractAction(_dimmed ? "Brighten lamp" : "Dim lamp", c => ToggleDim(c, !_dimmed)));
                // block toggle actions
                into.Add(new InteractAction("Turn off block lamps", c => ToggleBlock(c, false)));
            }
            else
            {
                // block toggle when off
                into.Add(new InteractAction("Turn on block lamps", c => ToggleBlock(c, true)));
            }
        }


    private void ToggleBlock(InteractContext ctx, bool turnOn)
    {
        var nearby = StreetLampRegistry.GetNearby(transform.position, BLOCK_TOGGLE_RADIUS);
        foreach (var lamp in nearby)
        {
            lamp.ApplyState(turnOn, ctx);
        }
        // One audible click for the block action
        MakeNoise(2f);
        ctx.Notify(turnOn ? "All nearby street lamps click on." : "All nearby street lamps click off.");
    }







        public override void CaptureState(FixtureSave save)
        {
            // store on/off as bit 0, flicker flag as bit 1, dim flag as bit 2
            int state = 0;
            if (_light != null && _light.enabled) state |= 1; // bit 0 = lamp on
            if (_flickerOn) state |= 2; // bit 1 = flicker on
            if (_dimmed) state |= 4; // bit 2 = dimmed
            save.State = state;
        }

public override void RestoreState(FixtureSave save)
        {
            bool on = (save.State & 1) != 0;
            bool flickerOn = (save.State & 2) != 0;
            bool dimmed = (save.State & 4) != 0;
            if (_light != null) _light.enabled = on;
            if (_flicker != null)
            {
                _flicker.enabled = on;
                _flickerOn = flickerOn;
                _flicker.SetFlickerEnabled(on && flickerOn);
            }
            _dimmed = dimmed;
            if (_light != null && _light.enabled)
            {
                _light.intensity = dimmed ? 0.2f : 1f;
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
private void ToggleDim(InteractContext ctx, bool dim)
            {
                _dimmed = dim;
                if (_light != null && _light.enabled)
                {
                    _light.intensity = dim ? 0.2f : 1f;
                }
                MakeNoise(2f);
                ctx.Notify(dim ? "The lamp is dimmed." : "The lamp shines brightly.");
            }
    }
}
