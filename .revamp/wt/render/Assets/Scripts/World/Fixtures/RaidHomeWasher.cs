using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The controls of a raid cottage's washing machine (its porthole door and the drum behind it are a
    /// <see cref="StorageFixture"/> of their own: somebody's last load, seldom anything worth taking). With the power and
    /// the water on, "Run a wash" sets the machine going by itself — a long cycle of sloshing round, one way and then
    /// the other, ending in a juddering spin — and it can be heard a good way off, the spin farther still. The dead hear
    /// it too: start it and slip away, and the laundry is where they'll go. The door stays locked while it runs (the
    /// drum can't be got at); the wash can be stopped, and it stops by itself if the power or the water goes. Saves
    /// whether it's running and how long it has left.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidHomeWasher : Interactable
    {
        private Transform _body;
        private StorageFixture _drum;
        private Renderer _suds;
        private Transform _sudsT;
        private Vector3 _axis;
        private string _key;
        private Vector3 _point;
        private bool _running;
        private float _left;      // seconds of the cycle still to run
        private float _angle, _nextNoise;

        public override string DisplayName => "Washing machine";
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _body != null ? _body : transform;
        public override string SaveKey => _key;

        /// <summary>True while a wash is going round.</summary>
        public bool IsRunning => _running;

        private bool Spinning => _running && _left <= CloneZConfig.WasherSpinSeconds;

        /// <summary>The controls' fixture on <paramref name="body"/> (the machine's furniture root, its frame <paramref name="f"/>):
        /// <paramref name="drum"/> is the storage behind the porthole and <paramref name="suds"/> the wash going round behind
        /// the glass (a piece whose pivot is the porthole's middle; it's turned about the porthole's axis while it runs).</summary>
        internal static RaidHomeWasher Create(Transform body, in KitchenFrame f, StorageFixture drum, Renderer suds)
        {
            // Determine the world position for the control based on the kitchen frame and a dial offset.
            Vector3 point = f.P(FurnitureBuilder.WasherDial + new Vector3(0f, 0.02f, 0.06f));
            // Create a dedicated GameObject to hold the washer's interactive controls.
            var go = new GameObject("WasherControls");
            // Parent it to the machine's body so it moves with the fixture.
            go.transform.SetParent(body, false);
            // Position and orient the controls at the calculated point with no rotation.
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            // Add the RaidHomeWasher component which implements the interaction logic.
            var w = go.AddComponent<RaidHomeWasher>();
            // Cache the body transform for later use in occlusion and positioning.
            w._body = body;
            // Store reference to the drum storage so we can lock/unlock it during cycles.
            w._drum = drum;
            // Store the renderer that will display suds when the machine runs.
            w._suds = suds;
            // Cache the transform of the suds renderer for fast rotation updates.
            w._sudsT = suds != null ? suds.transform : null;
            // Determine the axis around which the suds rotate based on the kitchen frame orientation.
            w._axis = f.D(Vector3.forward);
            // Remember the interaction point for the player to approach.
            w._point = point;
            // Generate a unique save key for persisting the washer's state.
            w._key = KeyFor("washer", f.Origin);
            // Return the fully-initialized component.
            return w;
        }

        private void Update()
        {
            if (!_running) return;
            if (!PowerGrid.HousePowered || !WaterMains.MainsOn)
            {
                Halt();
                KitchenParts.NotifyNear(_point, 8f, !PowerGrid.HousePowered
                    ? "The washing machine dies mid-cycle with a clunk. The power's gone."
                    : "The washing machine grinds round dry and gives up. The water's been shut off.");
                return;
            }
            float dt = Time.deltaTime;
            _left -= dt;

            // A wash tumbles one way and then the other; the spin just whirls.
            bool spin = Spinning;
            float speed = spin ? 720f : (Mathf.Repeat(_left, 10f) < 5f ? 120f : -120f);
            _angle = Mathf.Repeat(_angle + speed * dt, 360f);
            if (_sudsT != null) _sudsT.rotation = Quaternion.AngleAxis(_angle, _axis);
            if (Time.time >= _nextNoise)
            {
                _nextNoise = Time.time + CloneZConfig.WasherNoiseInterval;
                MakeNoise(spin ? CloneZConfig.WasherSpinNoiseRadius : CloneZConfig.WasherNoiseRadius);
            }
            if (_left > 0f) return;
            Halt();
            KitchenParts.NotifyNear(_point, 10f, "The washing machine shudders to a stop, and the drum creaks round once more. Done.");
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_running)
            {
                into.Add(new InteractAction("Stop the wash", StopWash));
                into.Add(new InteractAction("Check the dial", CheckDial));
                into.Add(new InteractAction("Open the washing machine").DisabledIf(true, "The door's locked until the wash is done"));
                return;
            }
            bool power = PowerGrid.HousePowered, water = WaterMains.MainsOn, open = _drum != null && _drum.IsOpen;
            into.Add(new InteractAction("Run a wash", StartWash)
                .Timed(CloneZConfig.WasherSetSeconds, "Setting the dial", HumanoidActivity.Use)
                .DisabledIf(!power, PowerGrid.NoPowerReason ?? "There's no power")
                .DisabledIf(power && !water, "There's no water to wash with \u2014 the water's been shut off")
                .DisabledIf(power && water && open, "Shut the door first"));
            into.Add(new InteractAction("Read the dial", ReadDial));
            // Listen to the washing machine's state
            into.Add(new InteractAction("Listen to the machine", Listen));
// Open the washing machine door to access the drum when not running
            into.Add(new InteractAction("Open washing machine", OpenDoor)
            .DisabledIf(_drum != null && _drum.IsOpen, "It's already open"));
            // Close the washing machine door after opening it
into.Add(new InteractAction("Close washing machine", CloseDoor)
                .DisabledIf(_drum == null || !_drum.IsOpen, "It's already closed"));
            // When the door is open and the wash isn’t running, let the survivor rummage the drum.
            into.Add(new InteractAction("Search drum", SearchDrum)
                .DisabledIf(_drum == null || !_drum.IsOpen, "The door is closed")
                .DisabledIf(_running, "Can't search while the wash is running"));
        }

        /// <summary>Starts a wash cycle if power, water, and the door are ready.</summary>
        private void StartWash(InteractContext ctx)
        {
            // Verify power and water are available before attempting to start the machine.
            if (!PowerGrid.HousePowered || !WaterMains.MainsOn)
            {
                ctx.Notify("The dial turns, and nothing happens.");
                return;
            }
            // Ensure the drum door is closed so the player cannot access its contents while the wash runs.
            if (_drum != null && _drum.IsOpen)
            {
                ctx.Notify("It won't start with the door open.");
                return;
            }
            // Begin the wash cycle using the configured duration; this locks the door and shows suds.
            Run(CloneZConfig.WasherCycleSeconds);
            // Inform the player that the wash has started and that it will be audible to nearby entities.
            ctx.Notify("You turn the dial to REGULAR and pull the knob. A clunk, the rush of water filling the drum \u2014 and it is not quiet. Anything nearby will hear it.");
        }

        // The machine goes round for <seconds>: the door locked (the drum's storage put out of reach), the suds showing.
        private void Run(float seconds)
        {
            _running = true;
            _left = seconds;
            _nextNoise = Time.time;
            if (_drum != null)
            {
                _drum.SetOpen(false, false);
                // Off altogether, its container too: the inventory's nearby list would otherwise still reach into the drum.
                _drum.gameObject.SetActive(false);
            }
            if (_suds != null) _suds.enabled = true;
        }

        /// <summary>Stops the wash and resets the machine state.</summary>
        private void Halt()
        {
            // Reset running flag and clear remaining time.
            _running = false;
            _left = 0f;
            // Reactivate the drum's container so it can be inspected after the wash stops.
            if (_drum != null) _drum.gameObject.SetActive(true);
            // Hide the suds visual effect.
            if (_suds != null) _suds.enabled = false;
            // Reset any suds rotation to its default orientation.
            if (_sudsT != null) _sudsT.rotation = Quaternion.identity;
            // Emit a door‑close noise to signal the wash has halted.
            MakeNoise(CloneZConfig.WasherDoorNoise);
        }

        private void StopWash(InteractContext ctx)
        {
            Halt();
            ctx.Notify("You turn the dial back to OFF. The drum sloshes to a halt, full of grey water and suds.");
        }

        private void CheckDial(InteractContext ctx)
        {
            float share = _left / CloneZConfig.WasherCycleSeconds;
            ctx.Notify(Spinning ? "The dial's on SPIN. Nearly done \u2014 and loudest now; the whole machine's walking on the concrete."
                : share > 0.6f ? "The dial's pointing at WASH. A good while to go yet."
                : "The dial's round to RINSE. Not long now.");
        }

                private void ReadDial(InteractContext ctx)
        => ctx.Notify("The dial runs from WASH through RINSE to SPIN, with REGULAR, PERMANENT PRESS and DELICATES in faded orange. "
            + "Taped beside it, in marker: \u201CSMALL LOADS ONLY. IT WALKS.\u201D");

        // Listen to the washer's current state
        private void Listen(InteractContext ctx)
        {
            if (_running)
                ctx.Notify(Spinning ? "The wash is spinning loudly, a high‑pitched whine rattles the walls." : "You hear the machine sloshing and churning, a low rumble fills the room.");
            else
                ctx.Notify("The washing machine is silent, only the faint creak of the door remains.");
        }

        /// <summary>Opens the washing machine door to let the player access the drum.</summary>
        private void OpenDoor(InteractContext ctx)
        {
            // If the drum exists, open its door for player access.
            if (_drum != null)
            {
                _drum.SetOpen(true, false);
                ctx.Notify("You open the washing machine door, revealing the drum inside.");
            }
            else
            {
                ctx.Notify("There's nothing inside the machine.");
            }
        }

        // Close the washing machine door after it has been opened
        private void CloseDoor(InteractContext ctx)
        {
            if (_drum != null)
            {
                _drum.SetOpen(false, false);
                ctx.Notify("You close the washing machine door.");
            }
            else
            {
                ctx.Notify("There's nothing inside the machine.");
            }
        }

        // Search the drum for loot when door is open and machine is not running
        private void SearchDrum(InteractContext ctx)
        {
            if (_drum != null && _drum.IsOpen)
            {
                // Attempt to add a cloth rag to the player's inventory
                bool added = ctx.Inventory?.Inventory.Add(ItemDatabase.Get("ClothRag"), 1, enforceWeight: false) > 0;
                if (added)
                {
                    ctx.Notify("You poke around the drum and pull out a rag of dirty cloth.");
                }
                else
                {
                    ctx.Notify("Your pack is full – you can’t take the rag right now.");
                }
            }
            else
            {
                ctx.Notify("There’s nothing to rummage through.");
            }
        }

        public override void CaptureState(FixtureSave save)
        {
            save.State = _running ? 1 : 0;
            save.A = _left;
        }

        public override void RestoreState(FixtureSave save)
        {
            if ((save.State & 1) != 0 && save.A > 0f) Run(save.A);
            else if (_running) Halt();
        }
    }
}
