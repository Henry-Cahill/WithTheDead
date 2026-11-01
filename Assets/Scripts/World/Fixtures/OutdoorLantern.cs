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
        private const string BulbItemId = "bulb"; // item id for a lantern bulb
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
            into.Add(new InteractAction("Repair lantern", RepairLantern));
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
            ctx.Notify(PowerGrid.PoweredAt(this) ? "The house is powered." : "No power: " + (PowerGrid.NoPowerReasonAt(this) ?? "unknown reason") + ".");
        }

        private void Tap(InteractContext ctx)
        {
            MakeNoise(0.6f);
            if (_lamp == null) ctx.Notify("Cold, dead glass. There's no bulb any more.");
            else if (_lamp.IsOn) ctx.Notify("The glass is warm. A moth bats at it, gives up, and tries again.");
            else if (!_lamp.SwitchedOn) ctx.Notify("Cold glass. It's switched off inside, by the door.");
            else if (!PowerGrid.PoweredAt(this)) ctx.Notify("Cold glass. " + (PowerGrid.NoPowerReasonAt(this) ?? "There's no power") + ".");
            else ctx.Notify("Cold glass. Its little sensor is waiting for dusk.");
        }
        private void RepairLantern(InteractContext ctx)
        {
            MakeNoise(0.3f);
            if (_lamp == null)
            {
                ctx.Notify("There's no lantern fixture to repair.");
                return;
            }
            if (!_lamp.DeadBulb)
            {
                ctx.Notify("The lantern's bulb is fine already.");
                return;
            }
            // check for bulb in inventory
            var inv = ctx.Inventory.Inventory;
            ItemDef bulbDef = ItemDatabase.Get(BulbItemId);
            if (bulbDef == null || !inv.Stacks.Exists(s => s.Def == bulbDef))
            {
                ctx.Notify("You need a replacement bulb to fix the lantern.");
                return;
            }
            // consume one bulb
            var stack = inv.Stacks.Find(s => s.Def == bulbDef);
            inv.Remove(stack, 1);
            // repair the lamp
            _lamp.DeadBulb = false;
            // optionally turn it on so the survivor sees the result
            _lamp.SetSwitchedOn(true);
            ctx.Notify("You replace the broken bulb. The lantern flickers to life.");
        }
    }
}
