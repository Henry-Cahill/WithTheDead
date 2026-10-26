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
        private bool _priceTagTaken;
        private bool _repaired;

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

        /// <summary>Populate the list of player actions for this gas pump.</summary>
        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            // Add the core actions that are always available regardless of state.
            into.Add(new InteractAction(_lifted ? "Hang the nozzle back up" : "Lift the nozzle", ToggleNozzle));
            // Present flavour text that gives world‑building hints regardless of interaction state.
            into.Add(new InteractAction("Read the dials", c => c.Notify(
                "Regular, $1.09 9/10 a gallon. The last sale is frozen on the wheels: 11.4 gallons, $12.53. Then nothing.")));
            // More atmospheric description – the hose smell gives a sense of place.
            into.Add(new InteractAction("Sniff the hose", c => c.Notify(
                "Gasoline, sharp and sweet. The tanks under the forecourt aren't dry \u2014 just out of reach without the cashier's switch.")));
            // When the nozzle is lifted we expose additional interactions.
            if (_lifted) {
                // Basic nozzle manipulation actions – tap for sound, check for a leak.
                into.Add(new InteractAction("Tap the nozzle", TapNozzle));
                into.Add(new InteractAction("Check for leak", CheckLeak));
                // Offer the price‑tag only once; after taking it we switch to an inspection action.
                if (!_priceTagTaken)
                {
                    into.Add(new InteractAction("Take the rusted price tag", TakePriceTag));
                }
                else
                {
                    into.Add(new InteractAction("Inspect the rusted price tag", c => c.Notify("The rusted price tag reads $1.09 per gallon, a relic of harder times.")));
                }
                // Repair action is only offered until the nozzle is fixed; once repaired we enable pumping.
                if (!_repaired)
                {
                    into.Add(new InteractAction("Repair the nozzle", RepairNozzle));
                }
                else
                {
                    into.Add(new InteractAction("Pump gasoline", PumpFuel));
                }
                // Atmospheric, non‑state‑changing actions that add world‑building flavour.
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
                // Added flavour‑text actions (local lore, safety tip, maintenance log, family reminiscence, rumor flyer)
                // Theme: local lore – a faded newspaper clipping about the town's founding
                into.Add(new InteractAction("Read the faded newspaper clipping", c => c.Notify("The yellowed sheet reads: ‘In ’84 the old Miller farm survived the first snowstorm, thanks to a hidden coal stash.’")));
                // Theme: survival tip – a warning from a former employee about cold‑fuel interactions
                into.Add(new InteractAction("Read the safety tip note", c => c.Notify("A hand‑written note cautions: ‘Never store gasoline in the pump when temperatures dip below –5 °C – it can freeze the valve.’")));
                // Theme: maintenance note – a rusted maintenance log entry
                into.Add(new InteractAction("Read the rusted maintenance log", c => c.Notify("Scrawled in oily ink: ‘15‑Nov‑1992 – replaced hose clamps, but the sealant cracked after the blizzard.’")));
                // Theme: family reminiscence – a child's drawing taped to the pump
                into.Add(new InteractAction("Read the child's drawing", c => c.Notify("A crayon sketch of a smiling family standing beside a red pickup, captioned ‘Our happy farm.’ The corners are frayed by wind.")));
            // Added flavour‑text actions covering local legend, survival tip, maintenance log, personal reminiscence, and map clue
            into.Add(new InteractAction("Read the town legend plaque", c => c.Notify("A rusted metal plaque tells the tale: ‘On moonlit nights, the ghost of Old Man Harlan haunts the pump, warning travelers to keep their lights on.’")));
            into.Add(new InteractAction("Read the survival tip scribble", c => c.Notify("A hand‑written tip in faded ink advises: ‘Never refuel a vehicle with the engine running after a snowstorm; static can cause sparks.’")));
            into.Add(new InteractAction("Read the maintenance log entry", c => c.Notify("Scrawled in oily ink dated 12‑Mar‑1995: ‘Replaced cracked hose and greased nozzle bearings after the winter freeze.’")));
            into.Add(new InteractAction("Read the veteran's reminiscence", c => c.Notify("A weather‑worn note from a former pump attendant reads: ‘I remember the night the power went out; we kept the lantern burning till sunrise.’")));
            into.Add(new InteractAction("Read the map fragment", c => c.Notify("A torn map corner pinned to the pump shows a red X near the old school gym, marked ‘Safe shelter – bring firewood.’")));
                // Theme: whispered rumor – a torn flyer about a nearby shelter
                into.Add(new InteractAction("Read the torn flyer", c => c.Notify("The torn flyer promises: ‘Safe haven at the old school gym, north‑west of town. Bring firewood.’ The ink is smudged, but the promise feels real.")));


            }
        }

        public override void CaptureState(FixtureSave save)
        {
            base.CaptureState(save);
            save.A = _priceTagTaken ? 1f : 0f;
            save.B = _lifted ? 1f : 0f;
            save.C = _repaired ? 1f : 0f;
        }

        public override void RestoreState(FixtureSave save)
        {
            base.RestoreState(save);
            _priceTagTaken = save.A != 0f;
            _lifted = save.B != 0f;
            _repaired = save.C != 0f;
        }

        /// <summary>Toggle the nozzle between lifted and stowed positions.</summary>
        private void ToggleNozzle(InteractContext ctx)
        {
            _lifted = !_lifted;
            MakeNoise(CloneZConfig.RaidPumpNoise);
            ctx.Notify(_lifted
                ? "You lift the nozzle and squeeze. The pump hums, the wheels roll to zero, and a little sign lights up: SEE CASHIER."
                : "You hang the nozzle back in its cradle with a clunk.");
        }

        /// <summary>Check the nozzle for a leak; only possible when lifted.</summary>
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

        /// <summary>Take the rusted price‑tag item; requires the nozzle to be lifted.</summary>
        private void TakePriceTag(InteractContext ctx)
        {
            if (!_lifted)
            {
                ctx.Notify("You need to lift the nozzle first.");
                return;
            }
            // Give the survivor the price‑tag item.
            var def = ItemDatabase.Get("price_tag");
            if (def != null) ctx.Inventory?.Inventory.Add(def, 1, enforceWeight: false);
            _priceTagTaken = true;
            // Feedback sound and message.
            MakeNoise(CloneZConfig.RaidPumpNoise);
            ctx.Notify("You pry the rusted price tag off the pump and tuck it away.");
        }
        /// <summary>Repair the nozzle using a wrench if the player has one.</summary>
        private void RepairNozzle(InteractContext ctx)
        {
            // Optional: require a wrench; if not present just proceed.
            var wrenchDef = ItemDatabase.Get("wrench");
            if (wrenchDef != null && (ctx.Inventory?.Has(wrenchDef.Id) ?? false) == false)
            {
                ctx.Notify("You need a wrench to repair the nozzle.");
                return;
            }
            _repaired = true;
            MakeNoise(CloneZConfig.RaidPumpNoise);
            ctx.Notify("Using the wrench you tighten the rusted fittings. The pump sighs as the leak seals.");
        }

        /// <summary>Tap the lifted nozzle to produce a brief sound; does nothing else.</summary>
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

        /// <summary>Give the player a gasoline item after confirming the item exists.</summary>
        private void PumpFuel(InteractContext ctx)
        {
            var fuelDef = ItemDatabase.Get("gasoline");
            if (fuelDef == null)
            {
                ctx.Notify("There's no gasoline item defined; cannot give you fuel.");
                return;
            }
            ctx.Inventory?.Inventory.Add(fuelDef, 1, enforceWeight: false);
            MakeNoise(CloneZConfig.RaidPumpNoise);
            ctx.Notify("You twist the handle and a can of gasoline fills up. It might be useful later.");
        }

    }
}
