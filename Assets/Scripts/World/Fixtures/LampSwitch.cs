using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The switch of a free-standing lamp — the bedside lamp, the standard lamp, a desk lamp: [E] turns it on or off. With
    /// the power out (or the main breaker thrown) the switch still clicks over, but nothing lights until the power's
    /// back — nor ever, once its bulb has gone. Lives on the lamp's own object (see <see cref="HomeLamp.Attach"/>) and is
    /// saved by the lamp's position.
    /// </summary>
    [DisallowMultipleComponent]
    public class LampSwitch : Interactable
    {
        private HomeLamp _lamp;
        private Transform _furniture;
        private string _name;
        private string _key;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => transform.position;
        public override Transform OcclusionRoot => _furniture != null ? _furniture : transform;
        public override string SaveKey => _key;

        /// <summary>Gives <paramref name="lamp"/> its own switch; <paramref name="furniture"/> is the piece it stands on
        /// (its collider mustn't block the reach).</summary>
        internal static LampSwitch Add(HomeLamp lamp, Transform furniture, string displayName)
        {
            var s = lamp.gameObject.AddComponent<LampSwitch>();
            s._lamp = lamp;
            s._furniture = furniture;
            s._name = string.IsNullOrEmpty(displayName) ? "Lamp" : displayName;
            s._key = KeyFor("lamp", lamp.transform.position);
            return s;
        }

        /// <summary>A lamp's name from the furniture it's part of ("Furniture_Nightstand" → "Bedside lamp").</summary>
        internal static string NameFor(Transform furniture)
        {
            string n = furniture != null ? furniture.name : "";
            if (n.Contains("Nightstand")) return "Bedside lamp";
            if (n.Contains("FloorLamp")) return "Standard lamp";
            return "Lamp";
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_lamp == null) return;
            bool on = _lamp.SwitchedOn;
            // Primary toggle action
            into.Add(new InteractAction(on ? "Turn off the lamp" : "Turn on the lamp", c => Flip(c, !on)));
            // Secondary inspect action
            into.Add(new InteractAction("Inspect lamp", Inspect));
            // New secondary action to check bulb status
            into.Add(new InteractAction("Check bulb", CheckBulb));
            // New secondary action to check power status
            into.Add(new InteractAction("Check power", CheckPower));
            // add flicker toggle action
            into.Add(new InteractAction(_lamp.Flicker ? "Disable flicker" : "Enable flicker", ToggleFlicker));
        }

        // Shows the lamp's name in a HUD toast.
        private void Inspect(InteractContext ctx)
        {
            ctx.Notify($"You look at the {DisplayName}.");
        }

        // Checks whether the house currently has electricity.
        private void CheckPower(InteractContext ctx)
        {
            ctx.Notify(PowerGrid.HousePowered ? "The house has power." : "The house is without power.");
        }

        // Toggles the lamp's flicker effect.
        private void ToggleFlicker(InteractContext ctx)
        {
            if (_lamp == null) return;
            _lamp.Flicker = !_lamp.Flicker;
            ctx.Notify(_lamp.Flicker ? "The lamp now flickers." : "The lamp stops flickering.");
            MakeNoise(CloneZConfig.LightSwitchNoise);
        }

        // Notifies whether the lamp's bulb is burnt out.
        private void CheckBulb(InteractContext ctx)
        {
            ctx.Notify(_lamp.DeadBulb ? "The bulb is burnt out." : "The bulb looks fine.");
        }

        private void Flip(InteractContext ctx, bool on)
        {
            if (_lamp == null) return;
            _lamp.SetSwitchedOn(on);
            MakeNoise(CloneZConfig.LightSwitchNoise);
            if (on && !PowerGrid.HousePowered) ctx.Notify(LightingFixtures.NothingHappens());
            else if (on && _lamp.DeadBulb) ctx.Notify("Nothing happens. The bulb must have gone.");
        }

        public override void CaptureState(FixtureSave save)
        {
            int state = 0;
            if (_lamp != null)
            {
                if (_lamp.SwitchedOn) state |= 1; // bit 0 = on/off
                if (_lamp.Flicker) state |= 2; // bit 1 = flicker
            }
            save.State = state;
        }

        public override void RestoreState(FixtureSave save)
        {
            if (_lamp != null)
            {
                _lamp.SetSwitchedOn((save.State & 1) != 0);
                _lamp.Flicker = (save.State & 2) != 0;
            }
        }
    }
}
