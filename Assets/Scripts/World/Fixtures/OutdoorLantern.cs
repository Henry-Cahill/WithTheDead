using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The black carriage lantern beside the front door. It's worked from the porch-light toggle just inside the
    /// door and has a dusk sensor, so it lights itself when it gets dark (see <see cref="HomeLamp"/>); out here
    /// there's only "Tap the lantern", which tells how it's getting on.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutdoorLantern : Interactable
    {
        private HomeLamp _lamp;
        private Vector3 _point, _face, _normal;

        public override string DisplayName => "Porch lantern";
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);

        /// <summary>The lantern's fixture under <paramref name="host"/> (the doorway piece), for <paramref name="lamp"/>.</summary>
        internal static OutdoorLantern Create(Transform host, HomeLamp lamp, Vector3 point, Vector3 face, Vector3 normal)
        {
            var go = new GameObject("PorchLantern");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var l = go.AddComponent<OutdoorLantern>();
            l._lamp = lamp;
            l._point = point;
            l._face = face;
            l._normal = normal;
            return l;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Tap the lantern", Tap));
            into.Add(new InteractAction("Check power", CheckPower));
            into.Add(new InteractAction("Check lantern status", CheckStatus));
        }

        private void CheckStatus(InteractContext ctx)
        {
            if (_lamp == null)
            {
                ctx.Notify("No bulb – the lantern is dark.");
            }
            else
            {
                ctx.Notify(_lamp.IsOn ? "The lantern is lit." : "The lantern is not lit.");
            }
        }

        private void CheckPower(InteractContext ctx)
        {
            ctx.Notify(PowerGrid.HousePowered ? "The house is powered." : "No power: " + (PowerGrid.NoPowerReason ?? "unknown reason") + ".");
        }
        private void Tap(InteractContext ctx)
        {
            MakeNoise(0.6f);
            if (_lamp == null) ctx.Notify("Cold, dead glass. There's no bulb any more.");
            else if (_lamp.IsOn) ctx.Notify("The glass is warm. A moth bats at it, gives up, and tries again.");
            else if (!_lamp.SwitchedOn) ctx.Notify("Cold glass. It's switched off inside, by the door.");
            else if (!PowerGrid.HousePowered) ctx.Notify("Cold glass. " + (PowerGrid.NoPowerReason ?? "There's no power") + ".");
            else ctx.Notify("Cold glass. Its little sensor is waiting for dusk.");
        }
    }
}
