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
            if (_lifted) {
                into.Add(new InteractAction("Tap the nozzle", TapNozzle));
                into.Add(new InteractAction("Check for leak", CheckLeak));
                into.Add(new InteractAction("Inspect the price tag", c => c.Notify("The rusted price tag reads $1.09 per gallon, a relic of harder times.")));
                into.Add(new InteractAction("Listen to the pump's hum", c => c.Notify("The pump emits a low, mournful hum against the winter wind.")));
                into.Add(new InteractAction("Feel the cold metal", c => c.Notify("The metal is bitterly cold, frosting your fingertips.")));
                into.Add(new InteractAction("Observe the frost on the hose", c => c.Notify("A thin layer of frost clings to the hose, glinting in the pale light.")));
                into.Add(new InteractAction("Check the fuel gauge", c => c.Notify("The analog gauge points to zero; the pump is out of fuel.")));
                into.Add(new InteractAction("Read the warning sign", c => c.Notify("The warning sign warns of slippery ice—caution advised.")));
                into.Add(new InteractAction("Read the weathered warning", c => c.Notify("A battered sign cautions: ‘Beware icy patches – the ground is slick after a night’s freeze.’")));
                into.Add(new InteractAction("Read the rust‑ed rumor note", c => c.Notify("Scrawled on a metal strip: ‘Heard the old Miller’s farm got overrun after the blizzard. Stay off the back road.’")));
                into.Add(new InteractAction("Read the safety notice", c => c.Notify("A yellowed placard reads: ‘Do not attempt to pump fuel when temperature drops below –10 °C – risk of valve seize.’")));
                into.Add(new InteractAction("Read the faded ad", c => c.Notify("The peeling poster advertises: ‘Coke‑Zero – the perfect cold companion for a winter night.’")));
                into.Add(new InteractAction("Read the employee’s scribble", c => c.Notify("In shaky handwriting: ‘My dad said the pump’s heart still beats – maybe the cash‑register will open one day.’")));


            }
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

    private void CheckLeak(InteractContext ctx)
    {
        if (!_lifted)
        {
            ctx.Notify("You must lift the nozzle before checking for a leak.");
            return;
        }
        // Play the pump noise for feedback.
        MakeNoise(CloneZConfig.RaidPumpNoise);
        ctx.Notify("You inspect the nozzle for a leak. The pump hums but shows no obvious leak.");
    }

    }
}
