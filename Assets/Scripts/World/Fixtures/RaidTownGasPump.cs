using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A gas pump on the food mart's forecourt, worked from whichever side the survivor stands. There's no fuel to be had
    /// — the pumps wait on the cashier's say-so, and there's no cashier — but the nozzle can be lifted, the price read off
    /// the dials and the hose sniffed, the way anybody would. Nothing is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidTownGasPump : Interactable
    {
        private Vector3 _origin;
        private float _yaw, _halfWidth;
        private bool _lifted;

        public override string DisplayName => "Gas pump";

        /// <summary>The nozzle on the side the survivor stands at.</summary>
        public override Vector3 InteractPoint
        {
            get
            {
                Quaternion q = Quaternion.Euler(0f, _yaw, 0f);
                float side = 1f;
                PlayerInteractor pi = PlayerInteractor.Current;
                if (pi != null && (Quaternion.Inverse(q) * (pi.transform.position - _origin)).x < 0f) side = -1f;
                return _origin + q * new Vector3(side * (_halfWidth + 0.08f), 1.0f, -0.25f);
            }
        }

        /// <summary>A pump's fixture under <paramref name="host"/>: the pump stands at <paramref name="origin"/> facing
        /// <paramref name="yaw"/>, its island <paramref name="halfWidth"/> either side of its middle.</summary>
        internal static RaidTownGasPump Create(Transform host, Vector3 origin, float yaw, float halfWidth)
        {
            var go = new GameObject("Fixture_GasPump");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(origin, Quaternion.identity);
            var p = go.AddComponent<RaidTownGasPump>();
            p._origin = origin;
            p._yaw = yaw;
            p._halfWidth = halfWidth;
            return p;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction(_lifted ? "Hang the nozzle back up" : "Lift the nozzle", ToggleNozzle));
            into.Add(new InteractAction("Read the dials", c => c.Notify(
                "Regular, $1.09 9/10 a gallon. The last sale is frozen on the wheels: 11.4 gallons, $12.53. Then nothing.")));
            into.Add(new InteractAction("Sniff the hose", c => c.Notify(
                "Gasoline, sharp and sweet. The tanks under the forecourt aren't dry \u2014 just out of reach without the cashier's switch.")));
            into.Add(new InteractAction("Tap the nozzle", TapNozzle));
        }

        private void ToggleNozzle(InteractContext ctx)
        {
            _lifted = !_lifted;
            MakeNoise(CloneZConfig.RaidPumpNoise);
            ctx.Notify(_lifted
                ? "You lift the nozzle and squeeze. The pump hums, the wheels roll to zero, and a little sign lights up: SEE CASHIER."
                : "You hang the nozzle back in its cradle with a clunk.");
        }

        private void TapNozzle(InteractContext ctx)
        {
            if (!_lifted)
            {
                ctx.Notify("The nozzle is still in its cradle.");
                return;
            }
            MakeNoise(CloneZConfig.RaidPumpNoise);
            ctx.Notify("You give the nozzle a quick tap; it squeaks faintly but nothing else happens.");
        }

    }
}
