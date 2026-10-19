using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The workings of the kitchen's fridge-freezer (<see cref="HomeFurniture"/> of kind Fridge): a small freezer over a
    /// tall fridge, each behind its own door and each a <see cref="StorageFixture"/>. While the house has power
    /// (<see cref="PowerGrid.HousePowered"/>) it keeps food: every perishable stack inside ages at a fraction of the
    /// normal rate (the fridge slows it, the freezer all but stops it) — counted from the world clock, so a night's
    /// sleep counts in full, and once the grid has failed only the hours before it went down. A cool-white lamp comes
    /// on inside while the fridge door is open, if there's power to light it. Feel inside to tell whether it's still
    /// cold; the notes stuck to the door can be read. The doors' state and the contents are saved by the storage itself.
    /// </summary>
    [DisallowMultipleComponent]
    public class KitchenFridge : MonoBehaviour
    {
        private const float DoorAngle = 110f;
        private static readonly Color LampColour = new Color(0.86f, 0.93f, 1f);

        // What's stuck to the fridge door, read one after another.
        private static readonly string[] Notes =
        {
            "A shopping list in round, hurried handwriting: milk, eggs, bread, coffee filters, cat litter (we don't even have a cat??).",
            "A card from the dentist, pinned under a magnet shaped like a strawberry: \"Your next check-up is Thursday at 10:15 AM.\"",
            "\"Gone to the church supper. Casserole's in the oven \u2014 350 for forty minutes. Don't let it burn! \u2014 M\"",
            "A child's crayon drawing of this house: yellow walls, a red door, and a lopsided sun wearing sunglasses.",
            "A phone number with no name beside it, underlined three times.",
                        "A grocery receipt dated 07/03/1993, items: canned beans, bottled water, and a single loaf of bread.",
                "A handwritten note: \"Remember to lock the back door before bed \u2014 Mom\".",
                "A faded Polaroid of the family at the kitchen table, captioned \"Summer BBQ 1993\".",
                "A child's drawing of a zombie with a balloon, scribbled \"Don’t let him eat the pizza! \u2014 Dad\".",
                "A torn page from a diary: \"July 12 \u2014 heard strange noises outside, tried to ignore them.\"",
            };

        private StorageFixture _fridge, _freezer;
        private RoomLight _lamp;
        private MeshRenderer _lens;
        private bool _lampOn;
        private float _lastHours, _nextCool, _nextCheck;
        private int _note;

        private Action<InteractContext> _feelFridge, _feelFreezer, _readNote;

        /// <summary>True while it's keeping its contents cold.</summary>
        public bool Cooling => PowerGrid.HousePowered;

        /// <summary>Fits the fridge-freezer <paramref name="host"/> (its cabinet already drawn by
        /// <see cref="FurnitureBuilder.FridgeCabinet"/> in <paramref name="enamel"/>) with its doors, its two compartments
        /// and its lamp. Fresh: both doors shut.</summary>
        internal static KitchenFridge Build(HomeFurniture host, Color enamel)
        {
            var k = host.gameObject.AddComponent<KitchenFridge>();
            k.Assemble(host, enamel);
            return k;
        }

        private void Awake()
        {
            _feelFridge = c => c.Notify(ColdReport(false));
            _feelFreezer = c => c.Notify(ColdReport(true));
            _readNote = ReadNote;
        }

        private void Assemble(HomeFurniture host, Color enamel)
        {
            Transform root = host.transform;
            Vector3 at = root.position;
            var f = new KitchenFrame(at, host.Yaw);
            var rng = new System.Random(host.Seed * 7 + 11);   // the notes and magnets on the door, the same every time

            // Both doors hang on the cabinet's -X edge and swing out past square.
            const float front = FurnitureBuilder.FridgeDoorFront, hinge = FurnitureBuilder.FridgeHingeX;
            FixturePart freezerDoor = KitchenParts.Door(root, "FreezerDoor", f, d => FurnitureBuilder.FridgeDoor(d, rng, enamel, true),
                new Vector3(hinge, FurnitureBuilder.FreezerDoorY0, front), Vector3.right, Vector3.forward, DoorAngle);
            FixturePart fridgeDoor = KitchenParts.Door(root, "FridgeDoor", f, d => FurnitureBuilder.FridgeDoor(d, rng, enamel, false),
                new Vector3(hinge, FurnitureBuilder.FridgeDoorY0, front), Vector3.right, Vector3.forward, DoorAngle);

            // The lamp: its lens lights up and a little cool light fills the fridge (and spills out of the open door).
            _lens = KitchenParts.PieceAt(root, "FridgeLamp", f, FurnitureBuilder.FridgeLampLens, FurnitureBuilder.FridgeLampGlow);
            _lamp = RoomLight.Spawn(root, f.P(0f, 1.08f, 0f), LampColour, CloneZConfig.FridgeLampRange, CloneZConfig.FridgeLampIntensity, false, false);
            _lamp.ShowBulb = false;
            _lamp.On = false;

            _freezer = StorageFixture.Create(root, "Freezer", Interactable.KeyFor("freezer", at), f.P(0.2f, 1.48f, front + 0.01f),
                new[] { freezerDoor }, root, host.Seed * 13 + 3, new StorageOptions
                {
                    LootType = ContainerType.Fridge,
                    EmptyChance = CloneZConfig.FreezerEmptyChance,
                    MaxStacks = CloneZConfig.FreezerMaxStacks,
                    ExtraActions = (ctx, list) => list.Add(new InteractAction("Feel how cold it is", _feelFreezer)),
                });
            _fridge = StorageFixture.Create(root, "Fridge", Interactable.KeyFor("fridge", at), f.P(0.2f, 0.85f, front + 0.01f),
                new[] { fridgeDoor }, root, host.Seed * 13 + 5, new StorageOptions
                {
                    LootType = ContainerType.Fridge,
                    EmptyChance = CloneZConfig.FridgeEmptyChance,
                    MaxStacks = CloneZConfig.FridgeMaxStacks,
                    ExtraActions = (ctx, list) =>
                    {
                        list.Add(new InteractAction("Feel how cold it is", _feelFridge));
                        list.Add(new InteractAction("Read the notes on the door", _readNote));
                    },
                    OnOpenChanged = open => RefreshLamp(),
                });
            _lastHours = TimeOfDay.Now;
        }

        private void Update()
        {
            float t = Time.time;
            if (t >= _nextCheck)
            {
                _nextCheck = t + CloneZConfig.KitchenStatusInterval;
                RefreshLamp();   // the power can fail (or the breaker be thrown) with the door standing open
            }
            if (t >= _nextCool)
            {
                _nextCool = t + CloneZConfig.FridgeCoolInterval;
                Cool();
            }
        }

        #region Cooling
        // Holds back the ageing of everything perishable inside by the hours it's had power since the last pass.
        private void Cool()
        {
            float now = TimeOfDay.Now, from = _lastHours;
            _lastHours = now;
            if (now <= from) return;   // the clock hasn't moved (or a loaded save set it back)
            float hours = PoweredHours(from, now);
            if (hours <= 0f) return;
            Chill(_fridge, hours * (1f - CloneZConfig.FridgeSpoilFactor), now);
            Chill(_freezer, hours * (1f - CloneZConfig.FreezerSpoilFactor), now);
        }

        // How many of the hours from..to it had power: all of them while the house is powered; once the grid has failed
        // (as its shut-off day begins), only those before it went down. A breaker thrown at the panel is caught on the
        // next pass.
        private static float PoweredHours(float from, float to)
        {
            if (PowerGrid.HousePowered) return to - from;
            PowerGrid grid = PowerGrid.Instance;
            if (grid == null || grid.IsOn) return 0f;
            float failed = (grid.ShutoffDay - 1) * 24f;
            return Mathf.Clamp(failed - from, 0f, to - from);
        }

        // Ages each perishable stack by `advance` hours less than the clock did (never younger than new).
        private static void Chill(StorageFixture s, float advance, float now)
        {
            if (s == null || advance <= 0f) return;
            LootContainer c = s.Container;
            if (c == null || c.Contents == null) return;
            List<ItemStack> stacks = c.Contents.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack st = stacks[i];
                if (st == null || st.Def == null || !st.Def.Perishable) continue;
                st.SpawnHours = Mathf.Min(st.SpawnHours + advance, now);
            }
        }
        #endregion

        #region Lamp and feel
        private void RefreshLamp()
        {
            bool on = _fridge != null && _fridge.IsOpen && PowerGrid.HousePowered;
            if (on == _lampOn) return;
            _lampOn = on;
            if (_lamp != null) _lamp.On = on;
            if (_lens != null) _lens.enabled = on;
        }

        private string ColdReport(bool freezer)
        {
            string reason = PowerGrid.NoPowerReason;
            if (reason == null)
                return freezer
                    ? "Frost nips at your fingertips. Anything in here will keep for a good long while."
                    : "It's cold inside \u2014 the food will keep.";
            reason = char.ToLowerInvariant(reason[0]) + reason.Substring(1);   // "the power's out", "the main breaker is off"
            bool food = HoldsPerishables(freezer ? _freezer : _fridge);
            if (freezer)
                return food
                    ? "The frost has gone to puddles and everything in here has softened \u2014 " + reason + "."
                    : "The frost is melting into a puddle on the shelf \u2014 " + reason + ".";
            return food
                ? "It's warm and starting to smell \u2014 " + reason + "."
                : "It's no colder than the kitchen in here \u2014 " + reason + ".";
        }

        private static bool HoldsPerishables(StorageFixture s)
        {
            if (s == null || s.Container == null || s.Container.Contents == null) return false;
            List<ItemStack> stacks = s.Container.Contents.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i] != null && stacks[i].Def != null && stacks[i].Def.Perishable) return true;
            return false;
        }

        private void ReadNote(InteractContext ctx)
        {
            ctx.Notify(Notes[_note]);
            _note = (_note + 1) % Notes.Length;
        }
        #endregion
    }
}
