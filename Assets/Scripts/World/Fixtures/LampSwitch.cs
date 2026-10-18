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
            into.Add(new InteractAction(on ? "Turn off the lamp" : "Turn on the lamp", c => Flip(c, !on)));
        }

        private void Flip(InteractContext ctx, bool on)
        {
            if (_lamp == null) return;
            _lamp.SetSwitchedOn(on);
            MakeNoise(CloneZConfig.LightSwitchNoise);
            if (on && !PowerGrid.HousePowered) ctx.Notify(LightingFixtures.NothingHappens());
            else if (on && _lamp.DeadBulb) ctx.Notify("Nothing happens. The bulb must have gone.");
        }

        public override void CaptureState(FixtureSave save) => save.State = _lamp != null && _lamp.SwitchedOn ? 1 : 0;

        public override void RestoreState(FixtureSave save)
        {
            if (_lamp != null) _lamp.SetSwitchedOn((save.State & 1) != 0);
        }
    }
}
