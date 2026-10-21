using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The raid town's shared workplace furniture, made usable: filing cabinets whose drawers each slide out on their own,
    /// banks of steel lockers with a door to every locker, gun safes behind a heavy door, medicine cabinets, steel shelving,
    /// tool benches with drawers, a pegboard and a vice, and crates with lids to lift — met in the National Guard post, the
    /// clinic, the depot, the county office, the food mart and the quarantine tent alike — and the rest of a workplace's
    /// furniture: desks with working drawers and computers, a steel counter with a working sink, a work fridge, bunks and
    /// hospital beds to lie on, chairs to sit in, a field radio and a bookcase to read from. Each stands as a body of its own
    /// (<see cref="FixtureBody"/>) drawn by <c>FurnitureBuilder.RaidCivic.cs</c>, its moving parts
    /// (<see cref="FixturePart"/>s) hung under it and its storage (<see cref="StorageFixture"/>) stocked the raid's way: the
    /// piece's main place as it always was (<see cref="MainStock"/> or <see cref="SmallStock"/>), the extra drawers and
    /// cupboards it gained lean (<see cref="SpareStock"/>). The sizes are the ones the layouts have always given these pieces.
    /// </summary>
    internal sealed partial class RaidBuilding
    {
        private static readonly string[] CivicOrdinals = { "Top", "Second", "Third", "Fourth", "Fifth", "Sixth" };
        private static readonly ContainerType[] CivicLockerMix = { ContainerType.Desk, ContainerType.Wardrobe, ContainerType.Locker };

        private static readonly string[] CivicDialLines =
        {
            "You spin the dial through a few numbers for luck. Click, click, click. The handle was never locked.",
            "The dial turns smooth and heavy. Somebody left this open in a hurry, and you're not complaining.",
"You try a birthday, then 1-2-3-4. Nothing \u2014 not that it matters: the bolts are already drawn back.",
            "The dial spins with a faint whir; a half‑written note beside it reads “Set to 12‑03‑87 for the generator.”",
            "A scrawl in faded ballpoint ink says “Emergency: keep dial on 4‑7‑1 during storms.”",
            "The metal clicks as you turn it, and a torn Post‑It clings: “Do not twist past 9— lock will jam.”",
            "A cheap laminate sticker peels from the dial: “Backup code 6‑2‑5 for water pump.”",
            "You hear a hollow thump; the dial bears a rust‑stained label: “Keep at 0 for night‑shift power‑down.”",
            "A smear of oil covers the edge; the last scribble reads “Rotate clockwise to reset HVAC.”",
            
        };

        private static readonly string[] CivicRackLines =
        {
            "Every rifle in the rack is chained through its trigger guard, and the padlock won't budge.",
            "Drill rifles, most of them \u2014 the firing pins drawn. Whoever locked up took the real ones.",
            "A steel cable runs through the lot and into a padlock the size of your fist. Not tonight.",
        };

        private static System.Action<InteractContext> s_CivicSpinDial, s_CivicTryRifle;
        private static int s_CivicDialLine, s_CivicRackLine;

        /// <summary>How the kit stocks the extra places a piece gained beyond its main one (a desk's lower drawers, the second
        /// cupboard under a counter, the freezer over the fridge): mostly bare, a thing at most — so a piece split into many
        /// drawers holds about what it always did.</summary>
        public static StorageOptions SpareStock(ContainerType type, string noun = null) => new StorageOptions
        {
            LootType = type,
            Noun = noun,
            EmptyChance = CloneZConfig.CivicSpareEmptyChance,
            MaxStacks = CloneZConfig.CivicSpareMaxStacks,
            RaidBoost = false,
        };

        // A drawer's name by its place in a stack of `count` (0 = the bottom one): "Top drawer", "Second drawer"…
        private static string CivicDrawerName(int fromBottom, int count)
        {
            int k = count - 1 - fromBottom;
            if (k == 0) return "Top drawer";
            if (k == count - 1) return "Bottom drawer";
            if (count == 3) return "Middle drawer";
            return CivicOrdinals[Mathf.Min(k, CivicOrdinals.Length - 1)] + " drawer";
        }

        // Drawer wording for a storage's options.
        private static StorageOptions CivicDrawer(StorageOptions o, string piece, string drawer)
        {
            o.Noun = drawer.ToLowerInvariant();
            o.ContainerName = piece + " \u2014 " + o.Noun;
            o.OpenVerb = "Pull out";
            o.CloseVerb = "Push in";
            return o;
        }

        /// <summary>A steel filing cabinet (0.46 × 1.4 × 0.62 m) of <paramref name="drawers"/> drawers, each sliding out on its
        /// own and each its own storage — files and a few desk odds and ends. Now and then one's been left half out. Returns
        /// its body.</summary>
        public Transform FilingCabinet(RaidRoom room, Place p, int drawers, Color body)
        {
            drawers = Mathf.Clamp(drawers, 1, 5);
            var size = new Vector3(0.46f, 1.4f, 0.62f);
            Color paint = body;
            Transform t = FixtureBody(room, p, size, true, (m, r) => paint = FurnitureBuilder.FilingCabinetBody(m, r, drawers, body), "Filing cabinet");
            KitchenFrame f = FrameOf(p);
            Vector3 at = t.position;
            var travel = new Vector3(0f, 0f, CloneZConfig.CivicDrawerTravel);
            int ajar = Rng.NextDouble() < CloneZConfig.CivicAjarChance ? Rng.Next(drawers) : -1;
            for (int i = 0; i < drawers; i++)
            {
                int row = i;
                var rng = new System.Random(Rng.Next());
                float y = FurnitureBuilder.FilingDrawerY(i);
                FixturePart part = KitchenParts.Drawer(t, "Drawer_" + i, f, d => FurnitureBuilder.FilingDrawer(d, rng, row, paint),
                    new Vector3(0f, y, FurnitureBuilder.FilingFront), travel);
                StorageOptions o = CivicDrawer(SmallStock(ContainerType.Desk), "Filing cabinet", CivicDrawerName(i, drawers));
                o.EmptyChance = CloneZConfig.CivicFilingEmptyChance;
                o.MaxStacks = CloneZConfig.CivicFilingMaxStacks;
                StorageFixture.Create(t, "Filing cabinet", Interactable.KeyFor("filing", at, i), f.P(0f, y, FurnitureBuilder.FilingFront + 0.02f),
                    new[] { part }, t, Rng.Next(), o);
                if (i == ajar) part.SetOpenness(0.3f + 0.3f * (float)Rng.NextDouble(), true);
            }
            return t;
        }

        /// <summary>A bank of <paramref name="count"/> steel lockers filling <paramref name="size"/>, a door to every locker and
        /// every locker its own storage: the first (<paramref name="name"/>) stocked from <paramref name="loot"/> as one of the
        /// raid's main containers, the others ("Locker 2"…) holding a mix of odds and ends. A door may hang ajar. Returns the
        /// first locker's storage (the one a layout may stock up further).</summary>
        public StorageFixture Lockers(RaidRoom room, Place p, Vector3 size, int count, Color body, ContainerType loot, string name)
        {
            count = Mathf.Max(1, count);
            Color paint = body;
            Transform t = FixtureBody(room, p, size, true, (m, r) => paint = FurnitureBuilder.LockerBankBody(m, r, count, body), name);
            KitchenFrame f = FrameOf(p);
            Vector3 at = t.position;
            int ajar = Rng.NextDouble() < CloneZConfig.CivicAjarChance ? Rng.Next(count) : -1;
            StorageFixture first = null;
            for (int i = 0; i < count; i++)
            {
                int n = i;
                var rng = new System.Random(Rng.Next());
                float x = FurnitureBuilder.LockerX(i, count);
                FixturePart door = KitchenParts.Door(t, "LockerDoor_" + i, f, d => FurnitureBuilder.LockerDoor(d, rng, n, count, paint),
                    new Vector3(x - 0.23f, FurnitureBuilder.LockerDoorY, FurnitureBuilder.LockerDoorFront), Vector3.right, Vector3.forward,
                    CloneZConfig.CivicLockerDoorAngle);
                string label = i == 0 ? name : "Locker " + (i + 1);
                StorageOptions o = i == 0 ? MainStock(loot, "locker") : SmallStock(i % 3 == 2 ? loot : CivicLockerMix[i % CivicLockerMix.Length], "locker");
                o.ContainerName = label;
                StorageFixture s = StorageFixture.Create(t, label, Interactable.KeyFor("locker", at, i),
                    f.P(x + 0.1f, 1.1f, FurnitureBuilder.LockerDoorFront + 0.02f), new[] { door }, t, Rng.Next(), o);
                if (i == 0) first = s;
                if (i == ajar) door.SetOpenness(0.3f + 0.25f * (float)Rng.NextDouble(), true);
            }
            return first;
        }

        /// <summary>A steel gun safe (0.8 × 1.9 × 0.6 m) stocked from <paramref name="loot"/>: a heavy door that swings slowly
        /// and clanks, rifles and ammunition behind it, and a dial to spin (it was never locked).</summary>
        public StorageFixture GunSafe(RaidRoom room, Place p, ContainerType loot, string name)
        {
            var size = new Vector3(0.8f, 1.9f, 0.6f);
            Color paint = default;
            Transform t = FixtureBody(room, p, size, true, (m, r) => paint = FurnitureBuilder.GunSafeBody(m, r), name);
            KitchenFrame f = FrameOf(p);
            FixturePart door = KitchenParts.Door(t, "SafeDoor", f, d => FurnitureBuilder.GunSafeDoor(d, paint),
                new Vector3(FurnitureBuilder.SafeHingeX, 0.95f, FurnitureBuilder.SafeDoorFront), Vector3.right, Vector3.forward,
                CloneZConfig.CivicSafeDoorAngle).WithSpeed(CloneZConfig.CivicSafeDoorSeconds);
            StorageOptions o = MainStock(loot, name.ToLowerInvariant());
            o.Noise = CloneZConfig.CivicSafeNoise;
            o.ExtraActions = CivicSafeActions;
            return StorageFixture.Create(t, name, Interactable.KeyFor("gunsafe", t.position), f.P(0.12f, 1.05f, FurnitureBuilder.SafeDoorFront + 0.06f),
                new[] { door }, t, Rng.Next(), o);
        }

        private static void CivicSafeActions(InteractContext ctx, List<InteractAction> into)
        {
            if (s_CivicSpinDial == null) s_CivicSpinDial = c =>
            {
                c.Notify(CivicDialLines[s_CivicDialLine++ % CivicDialLines.Length]);
                NoiseSystem.Emit(c.Position, CloneZConfig.FixtureNoiseSmall * 0.5f);
            };
            into.Add(new InteractAction("Spin the dial", s_CivicSpinDial));
        }

        /// <summary>A wall-standing medicine cabinet (0.9 × 1.8 × 0.42 m) stocked from <paramref name="loot"/>: two glazed doors
        /// over the shelves of stock and two solid ones over the cupboard below, all opening together.</summary>
        public StorageFixture MedicineCabinet(RaidRoom room, Place p, ContainerType loot, string name)
        {
            var size = new Vector3(0.9f, 1.8f, 0.42f);
            Color paint = default;
            Transform t = FixtureBody(room, p, size, true, (m, r) => paint = FurnitureBuilder.MedCabinetBody(m, r), name);
            KitchenFrame f = FrameOf(p);
            var doors = new FixturePart[4];
            for (int k = 0; k < 4; k++)
            {
                bool upper = k < 2, left = (k & 1) == 0;
                doors[k] = KitchenParts.Door(t, (upper ? "UpperDoor" : "LowerDoor") + (left ? "L" : "R"), f,
                    d => FurnitureBuilder.MedCabinetDoor(d, paint, upper, left),
                    new Vector3(left ? -0.45f : 0.45f, 0.9f, FurnitureBuilder.MedDoorFront), left ? Vector3.right : Vector3.left, Vector3.forward,
                    CloneZConfig.CivicCabinetDoorAngle);
            }
            return StorageFixture.Create(t, name, Interactable.KeyFor("medcabinet", t.position), f.P(0f, 1.2f, FurnitureBuilder.MedDoorFront + 0.03f),
                doors, t, Rng.Next(), MainStock(loot, name.ToLowerInvariant()));
        }

        /// <summary>Open steel shelving of <paramref name="size"/> (<paramref name="levels"/> shelves, stock drawn on them
        /// by <paramref name="stockKind"/> as <see cref="FurnitureBuilder.MetalShelf"/> does). <paramref name="stocked"/>
        /// shelving is one of the raid's main containers; otherwise it holds a few odds and ends at most.</summary>
        public StorageFixture Shelving(RaidRoom room, Place p, Vector3 size, int levels, int stockKind, Color frame, ContainerType loot,
            string name, bool stocked)
            => Shelving(room, p, size, levels, stockKind, frame, name,
                stocked ? MainStock(loot, name.ToLowerInvariant()) : SmallStock(loot, name.ToLowerInvariant()));

        /// <summary>Open steel shelving as above, stocked as <paramref name="stock"/> says (e.g. <see cref="SpareStock"/> for
        /// the rows of racking in a warehouse, picked over long ago).</summary>
        public StorageFixture Shelving(RaidRoom room, Place p, Vector3 size, int levels, int stockKind, Color frame, string name, StorageOptions stock)
        {
            Transform t = FixtureBody(room, p, size, true, (m, r) => FurnitureBuilder.MetalShelf(m, r, size.x, size.z, size.y, levels, stockKind, frame), name);
            stock.LookLabel = "Search the shelves";
            return StorageFixture.Create(t, name, Interactable.KeyFor("shelving", t.position), At(p, 0f, 1.05f, size.z * 0.5f + 0.02f),
                null, t, Rng.Next(), stock);
        }

        /// <summary>A workshop tool bench with its pegboard (1.8 × 1.9 × 0.75 m): two drawers under the worktop stocked from
        /// <paramref name="loot"/> (returned), the pegboard to look over for a tool or two, and the vice at the corner to
        /// wind open and shut.</summary>
        public StorageFixture ToolBench(RaidRoom room, Place p, ContainerType loot, string name)
        {
            var size = new Vector3(1.8f, 1.9f, 0.75f);
            Transform t = FixtureBody(room, p, size, true, (m, r) => FurnitureBuilder.ToolBenchBody(m, r), name);
            KitchenFrame f = FrameOf(p);
            Vector3 at = t.position;
            var travel = new Vector3(0f, 0f, CloneZConfig.CivicBenchDrawerTravel);
            var drawers = new FixturePart[2];
            for (int i = 0; i < 2; i++)
            {
                int n = i;
                var rng = new System.Random(Rng.Next());
                drawers[i] = KitchenParts.Drawer(t, "BenchDrawer_" + i, f, d => FurnitureBuilder.BenchDrawer(d, rng, n),
                    new Vector3(FurnitureBuilder.BenchDrawerX[i], FurnitureBuilder.BenchDrawerY, FurnitureBuilder.BenchDrawerFront), travel);
            }
            StorageOptions o = MainStock(loot, "tool drawers");
            o.ContainerName = name;
            o.OpenVerb = "Pull out";
            o.CloseVerb = "Push in";
            StorageFixture main = StorageFixture.Create(t, name, Interactable.KeyFor("toolbench", at),
                f.P(-0.3f, FurnitureBuilder.BenchDrawerY, FurnitureBuilder.BenchDrawerFront + 0.03f), drawers, t, Rng.Next(), o);

            StorageOptions peg = SmallStock(ContainerType.ToolBench, "pegboard");
            peg.ContainerName = "Pegboard";
            peg.LookLabel = "Look over the pegboard";
            StorageFixture.Create(t, "Pegboard", Interactable.KeyFor("pegboard", at), f.P(-0.2f, 1.42f, -0.3f), null, t, Rng.Next(), peg);

            Vector3 vice = FurnitureBuilder.BenchVice;
            FixturePart jaw = KitchenParts.Drawer(t, "ViceJaw", f, FurnitureBuilder.BenchViceJaw, vice, new Vector3(0f, 0f, CloneZConfig.CivicViceTravel))
                .WithSpeed(0.7f);
            CivicToggle.Create(t, "Bench vice", Interactable.KeyFor("vice", at), f.P(vice.x, vice.y + 0.04f, vice.z + 0.13f), t,
                    "Wind the vice open", "Wind the vice shut")
                .Moving(jaw)
                .Notes("The tommy bar squeaks round and the jaw backs off a couple of inches.", "You wind the jaw home until it bites.")
                .Noise(1.5f);
            return main;
        }

        /// <summary>A wooden supply crate of <paramref name="size"/> (stencilled olive when <paramref name="military"/>)
        /// stocked from <paramref name="loot"/> as one of the raid's main containers, its lid hinged along the back.</summary>
        public StorageFixture SupplyCrate(RaidRoom room, Place p, Vector3 size, bool military, ContainerType loot, string name)
            => SupplyCrate(room, p, size, military, loot, name, true);

        /// <summary>A lidded crate as above; one not <paramref name="stocked"/> holds a few odds and ends at most.</summary>
        public StorageFixture SupplyCrate(RaidRoom room, Place p, Vector3 size, bool military, ContainerType loot, string name, bool stocked)
        {
            Vector3 box = size - new Vector3(0.02f, 0.02f, 0.02f);
            FurnitureBuilder.CrateLoad load = CivicCrateLoad(loot, military);
            Color wood = default;
            Transform t = FixtureBody(room, p, size, true, (m, r) => wood = FurnitureBuilder.CrateBox(m, r, Vector3.zero, box, 0f, military, load), name);
            FixturePart lid = CivicCrateLid(t, FrameOf(p), Vector3.zero, box, 0f, wood, military);
            return StorageFixture.Create(t, name, Interactable.KeyFor("crate", t.position), At(p, 0f, box.y + 0.03f, box.z * 0.2f),
                new[] { lid }, t, Rng.Next(), CivicCrateStock(loot, name, stocked));
        }

        /// <summary>A crate standing on another, within a footprint of <paramref name="size"/>: the bottom one
        /// (<paramref name="under"/>) nailed shut, the top one (<paramref name="top"/>, shifted <paramref name="shift"/> along
        /// the piece and turned <paramref name="topYaw"/>) with a lid to lift — stocked from <paramref name="loot"/>, as a main
        /// container when <paramref name="stocked"/>.</summary>
        public StorageFixture StackedCrate(RaidRoom room, Place p, Vector3 size, Vector3 under, Vector3 top, float shift, float topYaw,
            bool military, ContainerType loot, string name, bool stocked)
        {
            var at = new Vector3(shift, under.y, 0f);
            FurnitureBuilder.CrateLoad load = CivicCrateLoad(loot, military);
            Color wood = default;
            Transform t = FixtureBody(room, p, size, true, (m, r) =>
            {
                FurnitureBuilder.Crate(m, r, Vector3.zero, under, 0f, military);
                wood = FurnitureBuilder.CrateBox(m, r, at, top, topYaw, military, load);
            }, name);
            FixturePart lid = CivicCrateLid(t, FrameOf(p), at, top, topYaw, wood, military);
            return StorageFixture.Create(t, name, Interactable.KeyFor("crate", t.position), At(p, shift, under.y + top.y + 0.03f, top.z * 0.2f),
                new[] { lid }, t, Rng.Next(), CivicCrateStock(loot, name, stocked));
        }

        /// <summary>A footlocker at the foot of a bunk (0.8 × 0.42 × 0.42 m, olive): a soldier's folded kit under a lid that
        /// lifts — a few personal odds and ends.</summary>
        public StorageFixture Footlocker(RaidRoom room, Place p)
            => SupplyCrate(room, p, new Vector3(0.8f, 0.42f, 0.42f), true, ContainerType.Locker, "Footlocker", false);

        private static FurnitureBuilder.CrateLoad CivicCrateLoad(ContainerType loot, bool military)
        {
            if (loot == ContainerType.Locker || loot == ContainerType.Wardrobe) return FurnitureBuilder.CrateLoad.Kit;
            return military ? FurnitureBuilder.CrateLoad.Ammo : FurnitureBuilder.CrateLoad.Straw;
        }

        private static StorageOptions CivicCrateStock(ContainerType loot, string name, bool stocked)
        {
            StorageOptions o = stocked ? MainStock(loot, name.ToLowerInvariant()) : SmallStock(loot, name.ToLowerInvariant());
            o.OpenVerb = "Lift the lid of";
            o.CloseVerb = "Shut the lid of";
            return o;
        }

        // A crate's lid (drawn shut by FurnitureBuilder.CrateLid at `at`, `size`, turned `yaw` within the piece), hinged along
        // its back edge so its front lifts.
        private static FixturePart CivicCrateLid(Transform body, in KitchenFrame f, Vector3 at, Vector3 size, float yaw, Color wood, bool military)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Vector3 hinge = at + q * new Vector3(0f, size.y, -size.z * 0.5f);
            return KitchenParts.Door(body, "Lid", f, d => FurnitureBuilder.CrateLid(d, at, size, yaw, wood, military),
                hinge, q * Vector3.forward, Vector3.up, CloneZConfig.CivicLidAngle).WithSpeed(CloneZConfig.CivicLidSeconds);
        }

        #region Desks, computers and radios
        private static readonly string[] CivicScreenLines =
        {
            "Green letters on black: a memo about the coffee fund, half typed. The cursor blinks after \u201Cand furthermore\u201D.",
            "A list of names and addresses, sorted by street. Some have a little star beside them. You'd rather not know what the star means.",
            "A blinking prompt and nothing else. Somebody shut everything down properly before they left.",
            "A box in the middle of the screen: PRINTER NOT RESPONDING. Nothing is responding.",
            "A game of patience, abandoned halfway. The red seven goes on the black eight. You move it.",
            "A half‑filled spreadsheet: column A shows numbers, column B a sum formula left dangling, the cursor hovers.",
            "A draft report in a text editor, the final paragraph reads “Conclusion …” and then stops, green ink blinking.",
            "A calendar window stuck on March, days after the 12th are blank, the cursor awaiting a new appointment.",
            "An email client listing inbox items, the last subject line reads “Re: …” with no message body displayed.",
            "A command‑line console paused at line 42, the prompt flashing, waiting for a keystroke.",
"A half‑filled spreadsheet shows fuel allocations, column A shows numbers, column B a sum formula left dangling, the cursor hovers.",
"A weather widget displays a bitter‑cold forecast for Kentucky, with snow‑depth predictions flickering.",
"A personal e‑mail draft reads “Hey Mom, …” but stops before the signature, the cursor pulsing.",
"An urgent supply request list titled “MEDICAL RESTOCK” lists bandages, IV bags and an unfinished line.",
"A government bulletin scrolls: “County emergency shelter capacity at 78 %…”, the text pauses briefly before the next line.",
        };

        private static System.Action<InteractContext> s_CivicReadScreen;
        private static int s_CivicScreenLine;

        /// <summary>An office desk with working drawers (<paramref name="w"/> × 0.9 × 0.75 m, front toward the placement's +Z):
        /// three pedestal drawers, each sliding out on its own — the top one the desk's main drawer, one of the raid's main
        /// containers when <paramref name="stocked"/>, else a few odds and ends at most; the two below it and the shallow
        /// pencil drawer under the top lean — on whichever side <paramref name="pedestalRight"/> says (at random if null).
        /// <paramref name="dress"/> draws what stands on it in the desk's frame (the top at 0.76); a lamp it lights with
        /// <see cref="LampLight"/> hangs under the desk. Returns its body.</summary>
        public Transform DrawerDesk(RaidRoom room, Place p, float w, Color wood, bool stocked, string name,
            System.Action<RaidMesh, System.Random> dress, bool? pedestalRight = null)
        {
            const float d = 0.75f;
            bool right = pedestalRight ?? Rng.NextDouble() < 0.5;
            Color paint = wood;
            Transform t = FixtureBody(room, p, new Vector3(w, 0.9f, d), true, (m, r) =>
            {
                paint = FurnitureBuilder.DeskBody(m, r, w, d, wood, right);
                dress?.Invoke(m, r);
            }, name);

            KitchenFrame f = FrameOf(p);
            Vector3 at = t.position;
            float px = (right ? 1f : -1f) * (w * 0.5f - 0.22f);
            var travel = new Vector3(0f, 0f, CloneZConfig.CivicDrawerTravel);
            for (int row = 0; row < 3; row++)
            {
                int n = row;
                var rng = new System.Random(Rng.Next());
                float y = FurnitureBuilder.DeskDrawerY(row);
                FixturePart part = KitchenParts.Drawer(t, "DeskDrawer_" + row, f, m => FurnitureBuilder.DeskDrawer(m, rng, w, d, paint, right, n),
                    new Vector3(px, y, d * 0.5f - 0.025f), travel);
                StorageOptions o = row > 0 ? SpareStock(ContainerType.Desk)
                    : stocked ? MainStock(ContainerType.Desk) : SmallStock(ContainerType.Desk);
                StorageFixture.Create(t, name, Interactable.KeyFor("desk", at, row), f.P(px, y, d * 0.5f + 0.02f), new[] { part }, t, Rng.Next(),
                    CivicDrawer(o, name, row == 0 ? "Top drawer" : row == 1 ? "Middle drawer" : "Bottom drawer"));
            }
            var prng = new System.Random(Rng.Next());
            float cx = (right ? -1f : 1f) * 0.21f;
            FixturePart pencil = KitchenParts.Drawer(t, "PencilDrawer", f, m => FurnitureBuilder.DeskPencilDrawer(m, prng, w, d, paint, right),
                new Vector3(cx, 0.68f, d * 0.5f - 0.03f), new Vector3(0f, 0f, CloneZConfig.CivicPencilDrawerTravel));
            StorageFixture.Create(t, name, Interactable.KeyFor("desk", at, 3), f.P(cx, 0.68f, d * 0.5f + 0.01f), new[] { pencil }, t, Rng.Next(),
                CivicDrawer(SpareStock(ContainerType.Desk), name, "Pencil drawer"));
            return t;
        }

        /// <summary>The computer on a desk whose body drew <see cref="FurnitureBuilder.CrtMonitorBody"/> at <paramref name="at"/>
        /// (the desk's frame), turned <paramref name="yaw"/>: its screen, lit or dark, and the switch to work it — it needs
        /// power, thunks and whines as it warms up, and while it's on there's something on the screen to read.</summary>
        public CivicToggle DeskMonitor(Transform desk, Place p, Vector3 at, float yaw, bool on)
        {
            KitchenFrame f = FrameOf(p);
            var rng = new System.Random(Rng.Next());
            MeshRenderer lit = KitchenParts.PieceInFrame(desk, "ScreenOn", f, at, m => FurnitureBuilder.CrtScreen(m, rng, at, yaw, true));
            MeshRenderer dark = KitchenParts.PieceInFrame(desk, "ScreenOff", f, at, m => FurnitureBuilder.CrtScreen(m, rng, at, yaw, false));
            Vector3 button = f.P(at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.1f, 0.2f, 0.1f));
            CivicToggle toggle = CivicToggle.Create(desk, "Computer", Interactable.KeyFor("computer", button), button, desk,
                    "Switch the computer on", "Switch the computer off")
                .Showing(lit, dark)
                .NeedsPower()
                .Noise(CloneZConfig.CivicMonitorNoise)
                .Notes("The monitor thunks, whines and warms to a green glow.", "The picture shrinks to a bright dot and dies.")
                .StartingOn(on);
            if (s_CivicReadScreen == null) s_CivicReadScreen = c => c.Notify(CivicScreenLines[s_CivicScreenLine++ % CivicScreenLines.Length]);
            toggle.Extra((c, into) =>
            {
                if (toggle.IsOn) into.Add(new InteractAction("Read the screen", s_CivicReadScreen));
            });
            return toggle;
        }

        /// <summary>An olive field radio standing on <paramref name="host"/> (a desk or table drawn at placement
        /// <paramref name="p"/>), at <paramref name="at"/> in the placement's frame turned <paramref name="yaw"/>: the set itself
        /// and its working — the Guard's own nets, or with <see cref="CivicRadio.Band.Citizens"/> the CB channels.</summary>
        public CivicRadio FieldRadio(Transform host, Place p, Vector3 at, float yaw, CivicRadio.Band band = CivicRadio.Band.Guard)
        {
            KitchenFrame f = FrameOf(p);
            var rng = new System.Random(Rng.Next());
            MeshRenderer set = KitchenParts.PieceInFrame(host, "RadioSet", f, at, m => FurnitureBuilder.FieldRadioBody(m, rng, at, yaw));
            if (set != null) set.enabled = true;
            MeshRenderer dial = KitchenParts.PieceInFrame(host, "RadioDial", f, at, m => FurnitureBuilder.FieldRadioDial(m, at, yaw));
            Vector3 knobs = f.P(at + Quaternion.Euler(0f, yaw, 0f) * FurnitureBuilder.FieldRadioKnobs);
            return CivicRadio.Create(host, band == CivicRadio.Band.Guard ? "Field radio" : "CB radio", knobs, dial, band);
        }
        #endregion

        #region Counter, sink and fridge
        /// <summary>A run of stainless-steel counter (<paramref name="len"/> × 1.1 × 0.65 m; the look of
        /// <see cref="FurnitureBuilder.SteelCounter"/>) whose cupboard doors open in pairs on the bays behind them — the first
        /// stocked from <paramref name="loot"/> as a small place, the rest lean — and, at <paramref name="sinkX"/> along the
        /// run when given, a working sink (<see cref="CivicSink"/>; a <paramref name="clinical"/> one to scrub up at).
        /// <paramref name="dress"/> draws what's left on the worktop (its top at 0.96). Returns the body.</summary>
        public Transform SteelCounter(RaidRoom room, Place p, float len, float? sinkX, ContainerType loot, string name,
            System.Action<RaidMesh, System.Random> dress = null, bool clinical = false)
        {
            int doors = Mathf.Max(1, Mathf.RoundToInt(len / 0.5f));
            bool sink = sinkX.HasValue;
            float sx = sinkX ?? 0f;
            Color steel = FurnitureBuilder.Steel;
            Transform t = FixtureBody(room, p, new Vector3(len, 1.1f, 0.65f), true, (m, r) =>
            {
                steel = FurnitureBuilder.SteelCounterBody(m, r, len, doors, sink, sx);
                dress?.Invoke(m, r);
            }, name);

            KitchenFrame f = FrameOf(p);
            Vector3 at = t.position;
            float dw = len / doors;
            for (int i = 0, bay = 0; i < doors; i += 2, bay++)
            {
                int count = Mathf.Min(2, doors - i);
                var parts = new FixturePart[count];
                for (int k = 0; k < count; k++)
                {
                    int n = i + k;
                    bool hingeLeft = n % 2 == 0;
                    float x0 = -len * 0.5f + dw * n, x1 = x0 + dw;
                    parts[k] = KitchenParts.Door(t, "CounterDoor_" + n, f, d => FurnitureBuilder.SteelCounterDoor(d, steel, x0, x1, hingeLeft),
                        new Vector3(hingeLeft ? x0 + 0.003f : x1 - 0.003f, FurnitureBuilder.SteelDoorY0, FurnitureBuilder.SteelDoorFront),
                        hingeLeft ? Vector3.right : Vector3.left, Vector3.forward, CloneZConfig.CivicCounterDoorAngle);
                }
                float mid = -len * 0.5f + dw * (i + count * 0.5f);
                bool under = sink && Mathf.Abs(mid - sx) < dw * count * 0.5f;
                string label = under ? "Cupboard under the sink" : "Cupboard";
                StorageOptions o = bay == 0 ? SmallStock(loot, label.ToLowerInvariant()) : SpareStock(under ? ContainerType.Shelf : loot, label.ToLowerInvariant());
                o.ContainerName = label;
                StorageFixture.Create(t, label, Interactable.KeyFor("countercupboard", at, bay), f.P(mid, 0.5f, FurnitureBuilder.SteelDoorFront + 0.02f),
                    parts, t, Rng.Next(), o);
            }
            if (sink) CivicSink.Create(t, f, sx, clinical ? "Scrub sink" : "Sink", clinical);
            return t;
        }

        /// <summary>A workplace fridge-freezer (a 0.76 × 1.75 × 0.72 m place; the home's cabinet in <paramref name="enamel"/>,
        /// front toward the placement's +Z): the fridge stocked from <paramref name="loot"/> as one of the raid's main
        /// containers (returned), the small freezer above it lean, a lamp that lights while the fridge is open, the cold to
        /// feel and the notes on the door — a <paramref name="medical"/> fridge with a thermometer and a log instead.</summary>
        public StorageFixture WorkFridge(RaidRoom room, Place p, Color enamel, ContainerType loot, string name, bool medical)
        {
            Color c = enamel;
            Transform t = FixtureBody(room, p, new Vector3(0.76f, 1.75f, 0.72f), true, (m, r) =>
            {
                c = DecorPalette.Vary(enamel, 0.03f, r);
                FurnitureBuilder.FridgeCabinet(m, c);
            }, name);
            KitchenFrame f = FrameOf(p);
            Vector3 at = t.position;
            var rng = new System.Random(Rng.Next());
            const float front = FurnitureBuilder.FridgeDoorFront, hinge = FurnitureBuilder.FridgeHingeX;
            FixturePart freezerDoor = KitchenParts.Door(t, "FreezerDoor", f, d => FurnitureBuilder.FridgeDoor(d, rng, c, true),
                new Vector3(hinge, FurnitureBuilder.FreezerDoorY0, front), Vector3.right, Vector3.forward, 105f);
            FixturePart fridgeDoor = KitchenParts.Door(t, "FridgeDoor", f, d => FurnitureBuilder.FridgeDoor(d, rng, c, false),
                new Vector3(hinge, FurnitureBuilder.FridgeDoorY0, front), Vector3.right, Vector3.forward, 105f);
            MeshRenderer lens = KitchenParts.PieceAt(t, "FridgeLamp", f, FurnitureBuilder.FridgeLampLens, FurnitureBuilder.FridgeLampGlow);
            RoomLight lamp = RoomLight.Spawn(t, f.P(0f, 1.08f, 0f), CivicFridge.Lamp, CloneZConfig.FridgeLampRange, CloneZConfig.FridgeLampIntensity, false, false);
            lamp.ShowBulb = false;
            lamp.On = false;

            CivicFridge works = t.gameObject.AddComponent<CivicFridge>();
            works.Init(lens, lamp, medical);
            StorageOptions fo = MainStock(loot, medical ? "medical fridge" : "fridge");
            fo.ContainerName = name;
            fo.ExtraActions = works.FridgeActions;
            fo.OnOpenChanged = works.DoorMoved;
            StorageFixture fridge = StorageFixture.Create(t, name, Interactable.KeyFor("fridge", at), f.P(0.2f, 0.85f, front + 0.01f),
                new[] { fridgeDoor }, t, Rng.Next(), fo);
            StorageOptions zo = SpareStock(loot, "freezer");
            zo.ContainerName = "Freezer";
            zo.ExtraActions = works.FreezerActions;
            StorageFixture freezer = StorageFixture.Create(t, "Freezer", Interactable.KeyFor("freezer", at), f.P(0.2f, 1.48f, front + 0.01f),
                new[] { freezerDoor }, t, Rng.Next(), zo);
            works.Bind(fridge, freezer);
            return fridge;
        }
        #endregion

        #region Beds and seats
        private static readonly string[] CivicUnderBunk =
        {
            "A pair of boots, a sock with a hole in it and a paperback with the cover torn off.",
            "Dust, a gum wrapper and a letter from home, folded small. You put it back.",
            "A shoebox of cassette tapes and a Walkman with no batteries. Somebody's whole summer.",
            "A dented metal badge, rusted and half‑lost, still bearing a faded rank.",
            "A faded photograph of a family, edges curled, smiling under a winter sky.",
            "A cracked pocket‑knife, its blade nicked, lying beside a torn scrap of paper.",
            "A half‑eaten candy bar wrapped in crumpled foil, sticky and forgotten.",
            "A handwritten note “Keep the fire low – J”, scrawled on a torn napkin.",
        };

        private static readonly string[] CivicCharts =
        {
            "BED 2. Admitted 2:40 p.m., fever 103.8. \u201CAgitated. Bit orderly.\u201D Restraints at 7:15. Then nothing.",
            "The chart's been signed off every hour until nine tonight. The last line just says \u201Cmoved to County\u201D.",
            "Name, date of birth, allergies: penicillin. Under observation. Somebody has drawn a line through all of it.",
        };

        private static System.Action<InteractContext> s_CivicLookUnder, s_CivicReadChart, s_CivicFreshPaper;
        private static int s_CivicUnderLine, s_CivicChartLine;

        /// <summary>A steel army bunk bed (0.95 × 1.75 × 2.0 m, head toward the placement's -Z): lie down on the bottom bunk
        /// or climb into the top one to rest, and look under it. Returns its body.</summary>
        public Transform ArmyBunk(RaidRoom room, Place p)
        {
            Transform t = FixtureBody(room, p, new Vector3(0.95f, 1.75f, 2.0f), true, (m, r) => FurnitureBuilder.BunkBed(m, r), "Bunk");
            if (s_CivicLookUnder == null) s_CivicLookUnder = c => c.Notify(CivicUnderBunk[s_CivicUnderLine++ % CivicUnderBunk.Length]);
            CivicRest.Create(t, "Bunk", "bunk", t.position, Yaw(p.Yaw), 1.94f, 0.86f, new[] { 0.56f, 1.46f },
                    new[] { "Lie down on the bottom bunk", "Climb up into the top bunk" }, 0f, CloneZConfig.CivicBunkComfort)
                .Extra((c, into) => into.Add(new InteractAction("Look under the bunk", s_CivicLookUnder).Timed(1.5f, "Looking under the bunk", HumanoidActivity.Search)));
            return t;
        }

        /// <summary>A hospital bed made up to lie on (0.95 × 1.2 × 2.0 m, head toward the placement's -Z): lie down and rest, sit
        /// on its edge, read the chart on the foot rail. Returns its body.</summary>
        public Transform WardBed(RaidRoom room, Place p, string name = "Hospital bed")
        {
            Transform t = FixtureBody(room, p, new Vector3(0.95f, 1.2f, 2.0f), true, (m, r) => FurnitureBuilder.WardBed(m, r), name);
            if (s_CivicReadChart == null) s_CivicReadChart = c => c.Notify(CivicCharts[s_CivicChartLine++ % CivicCharts.Length]);
            CivicRest.Create(t, name, "bed", t.position, Yaw(p.Yaw), 1.94f, 0.84f, new[] { FurnitureBuilder.WardMattressTop + 0.02f },
                    new[] { "Lie down and rest" }, CloneZConfig.CivicBedEdgeHeight, CloneZConfig.BedEdgeComfort)
                .Extra((c, into) => into.Add(new InteractAction("Read the chart", s_CivicReadChart)));
            return t;
        }

        /// <summary>The clinic's examination couch (on a 0.95 × 1.0 × 2.0 m place, head toward the placement's -Z): lie down on
        /// it, sit on its edge, draw down a fresh length of paper. Returns its body.</summary>
        public Transform ExamCouch(RaidRoom room, Place p)
        {
            Transform t = FixtureBody(room, p, new Vector3(0.95f, 1.0f, 2.0f), true, (m, r) => FurnitureBuilder.ExamCouch(m, r), "Examination couch");
            if (s_CivicFreshPaper == null) s_CivicFreshPaper = c =>
            {
                LivingFixtures.Calm(c, CloneZConfig.TidyPanicRelief);
                c.Notify("You draw a fresh length of paper down the couch and tear it off clean. Neat. Pointless. It helps.");
            };
            CivicRest.Create(t, "Examination couch", "couch", t.position, Yaw(p.Yaw), 1.9f, 0.7f, new[] { FurnitureBuilder.ExamCouchTop + 0.01f },
                    new[] { "Lie down on the couch" }, FurnitureBuilder.ExamCouchTop + 0.01f, CloneZConfig.BedEdgeComfort)
                .Extra((c, into) => into.Add(new InteractAction("Pull down fresh paper", s_CivicFreshPaper)));
            return t;
        }

        /// <summary>Something to sit on, drawn by <paramref name="draw"/> (a chair, a stool, a wheelchair): a body of its own —
        /// solid only when <paramref name="solid"/> — whose sitter's hips rest at <paramref name="seatLocal"/> in its frame.</summary>
        public SeatFixture SeatPiece(RaidRoom room, Place p, Vector3 size, bool solid, System.Action<RaidMesh, System.Random> draw, string name,
            Vector3 seatLocal, float comfort, string sit = "Sit down", string stand = "Stand up")
        {
            Transform t = FixtureBody(room, p, size, solid, draw, name);
            return SeatFixture.Create(t, name, seatLocal, Yaw(p.Yaw), comfort, sit, stand, false);
        }

        /// <summary>A swivel office chair (0.55 × 1.0 × 0.55 m place) to sit in.</summary>
        public SeatFixture OfficeChairSeat(RaidRoom room, Place p, Color fabric)
            => SeatPiece(room, p, new Vector3(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, fabric), "Office chair",
                new Vector3(0f, 0.5f, 0.02f), CloneZConfig.CivicOfficeChairComfort);

        /// <summary>A plain wooden chair (0.45 × 0.9 × 0.45 m place) to sit on.</summary>
        public SeatFixture WoodChairSeat(RaidRoom room, Place p, Color wood, string name = "Chair")
            => SeatPiece(room, p, new Vector3(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, wood), name,
                new Vector3(0f, CloneZConfig.ChairSeatHeight, CloneZConfig.ChairSeatBack), CloneZConfig.ChairComfort);

        /// <summary>A shop stool (0.45 × 0.7 × 0.45 m place) to perch on.</summary>
        public SeatFixture StoolSeat(RaidRoom room, Place p, Color seat)
            => SeatPiece(room, p, new Vector3(0.45f, 0.7f, 0.45f), false, (m, r) => FurnitureBuilder.Stool(m, r, seat), "Stool",
                new Vector3(0f, 0.68f, 0f), CloneZConfig.CivicStoolComfort, "Perch on the stool", "Get down off the stool");

        /// <summary>A wheelchair (0.7 × 1.0 × 0.8 m place) to sit in.</summary>
        public SeatFixture WheelchairSeat(RaidRoom room, Place p)
            => SeatPiece(room, p, new Vector3(0.7f, 1f, 0.8f), false, (m, r) => FurnitureBuilder.Wheelchair(m, r), "Wheelchair",
                new Vector3(0f, 0.53f, 0.02f), CloneZConfig.CivicWheelchairComfort, "Sit in the wheelchair", "Get out of the wheelchair");

        /// <summary>A fabric sofa (<paramref name="w"/> × 0.85 × 0.88 m) with a seat at each end. Returns its body.</summary>
        public Transform SofaSeats(RaidRoom room, Place p, float w, Color fabric, string name = "Sofa")
        {
            Transform t = FixtureBody(room, p, new Vector3(w, 0.85f, 0.88f), true, (m, r) => FurnitureBuilder.Sofa(m, r, w, fabric), name);
            int n = w > 1.4f ? 2 : 1;
            for (int i = 0; i < n; i++)
                SeatFixture.Create(t, name, new Vector3(n == 1 ? 0f : (i == 0 ? -w : w) * 0.24f, 0.56f, 0.06f), Yaw(p.Yaw),
                    CloneZConfig.CivicSofaComfort, "Sit on the sofa", "Get up off the sofa", false);
            return t;
        }
        #endregion

        #region Shelves, screens and things to look at
        private static readonly string[] CivicDripLines =
        {
            "Half a bag of saline, the line clamped off and coiled round the pole.",
            "The bag's run dry. The line hangs loose, its needle still taped to a strip of gauze.",
            "SODIUM CHLORIDE 0.9%, says the label, and a time in biro: 18:30. Nobody came to change it.",
        };

        private static readonly string[] CivicPatienceLines =
        {
            "You lay out a game of patience with the dog-eared deck left on the table. It comes out on the very last card.",
            "Patience again. A black king stays buried under everything, so you cheat, a little. Nobody's counting.",
            "Somebody has marked the aces in biro. You play a hand anyway. It passes the time and steadies your hands.",
        };

        private static System.Action<InteractContext> s_CivicPatience;
        private static int s_CivicPatienceLine;

        /// <summary>A table drawn by <paramref name="draw"/> (top at 0.76): its top a place to put things down — or, given
        /// <paramref name="stock"/>, whatever was left lying on it to go through — and with <paramref name="chairs"/> the
        /// nearest chair pulled up to it to sit at, and a game of patience to while away a minute.</summary>
        public StorageFixture WorkTable(RaidRoom room, Place p, Vector3 size, System.Action<RaidMesh, System.Random> draw, string name,
            StorageOptions stock = null, bool chairs = false)
        {
            Transform t = FixtureBody(room, p, size, true, draw, name);
            Vector3 centre = t.position;
            StorageOptions o = stock ?? new StorageOptions
            {
                Noun = "table",
                ContainerName = "Table top",
                StartEmpty = true,
                SearchTime = 0f,
                LookLabel = "Put things on the table",
                RaidBoost = false,
            };
            if (chairs)
            {
                if (s_CivicPatience == null) s_CivicPatience = c =>
                {
                    LivingFixtures.Calm(c, CloneZConfig.ReadMagazinePanicRelief);
                    c.Notify(CivicPatienceLines[s_CivicPatienceLine++ % CivicPatienceLines.Length]);
                };
                o.ExtraActions = (ctx, into) =>
                {
                    float range = CloneZConfig.TableChairRange;
                    bool atTable = ctx.Seat != null && ctx.Seat.IsSeated && SeatFixture.IsAround(ctx.Seat.CurrentSeat, centre, range);
                    SeatFixture chair = atTable ? null : SeatFixture.NearestAround(centre, range, ctx.Position);
                    into.Add(new InteractAction("Sit at the table", c => { if (chair != null) chair.SitDown(c); })
                        .DisabledIf(chair == null && !atTable, "There's no chair at the table")
                        .DisabledIf(atTable, "You're already sitting at the table"));
                    into.Add(new InteractAction("Play a hand of patience", s_CivicPatience).Timed(8f, "Playing patience", HumanoidActivity.Use));
                };
            }
            return StorageFixture.Create(t, name, Interactable.KeyFor("table", centre), centre + Vector3.up * 0.8f, null, t, Rng.Next(), o);
        }

        /// <summary>A bookcase (0.9 × 1.8 × 0.32 m) of <paramref name="wood"/> to browse — a thing or two to be had — and to read
        /// from: the Guard post's manuals or the clinic's reference books (<see cref="CivicReading"/>).</summary>
        public StorageFixture ReadingShelf(RaidRoom room, Place p, Color wood, int stockKind, CivicReading.Shelf shelf, string name = "Bookcase")
        {
            var size = new Vector3(0.9f, 1.8f, 0.32f);
            Transform t = FixtureBody(room, p, size, true, (m, r) => FurnitureBuilder.Bookshelf(m, r, wood, stockKind), name);
            StorageOptions o = SpareStock(ContainerType.Shelf, name.ToLowerInvariant());
            o.LookLabel = "Browse the shelves";
            if (shelf == CivicReading.Shelf.Army) o.ExtraActions = CivicReading.AddArmyActions;
            else o.ExtraActions = CivicReading.AddClinicActions;
            return StorageFixture.Create(t, name, Interactable.KeyFor("bookcase", t.position), At(p, 0f, 1.2f, 0.17f), null, t, Rng.Next(), o);
        }

        /// <summary>A three-panel folding privacy screen (a 1.8 × 1.8 × 0.6 m place, pale curtain on chrome): its outer panels
        /// fold in to draw it back, and open out again.</summary>
        public CivicToggle PrivacyScreen(RaidRoom room, Place p)
        {
            Color cloth = default;
            Transform t = FixtureBody(room, p, new Vector3(1.8f, 1.8f, 0.6f), false, (m, r) =>
            {
                cloth = DecorPalette.Vary(new Color(0.60f, 0.70f, 0.66f), 0.05f, r);
                FurnitureBuilder.ScreenPanel(m, cloth, 1);
            }, "Privacy screen");
            KitchenFrame f = FrameOf(p);
            FixturePart left = CivicScreenWing(t, f, cloth, 0, -150f), right = CivicScreenWing(t, f, cloth, 2, -200f);
            return CivicToggle.Create(t, "Privacy screen", Interactable.KeyFor("screen", t.position), f.P(0f, 1.2f, 0.16f), t,
                    "Fold the screen back", "Pull the screen round")
                .Moving(left, right)
                .Noise(1f);
        }

        // An outer panel of the privacy screen, folding round its hinge on the middle panel (behind it, or round in front).
        private static FixturePart CivicScreenWing(Transform body, in KitchenFrame f, Color cloth, int panel, float angle)
        {
            var m = new RaidMesh();
            m.SetFrame(f.Origin, f.Yaw);
            FurnitureBuilder.ScreenPanel(m, cloth, panel);
            m.ResetFrame();
            Vector3 hinge = f.P(FurnitureBuilder.ScreenPosts[panel == 0 ? 1 : 2]);
            return FixturePart.Hinge(body, panel == 0 ? "PanelL" : "PanelR", m, hinge, Vector3.up, angle).WithSpeed(0.9f);
        }

        /// <summary>A drip stand (a 0.5 × 1.9 × 0.5 m place) with its bag to check.</summary>
        public CivicLook IvStand(RaidRoom room, Place p)
        {
            Transform t = FixtureBody(room, p, new Vector3(0.5f, 1.9f, 0.5f), false, (m, r) => FurnitureBuilder.IvStand(m, r, Vector3.zero), "Drip stand");
            return CivicLook.Create(t, "Drip stand", At(p, 0.1f, 1.62f, 0f), t, "Check the drip bag", CivicDripLines, Rng.Next(CivicDripLines.Length))
                .Taking(1.2f, "Checking the drip");
        }

        /// <summary>A piece drawn by <paramref name="draw"/> to look over or handle (a stack of tyres, a jerry can): [E]
        /// <paramref name="label"/> tells the next of <paramref name="lines"/>; <paramref name="point"/> is where the hand goes,
        /// in the placement's frame.</summary>
        public CivicLook LookPiece(RaidRoom room, Place p, Vector3 size, bool solid, System.Action<RaidMesh, System.Random> draw, string name,
            Vector3 point, string label, string[] lines)
        {
            Transform t = FixtureBody(room, p, size, solid, draw, name);
            return CivicLook.Create(t, name, At(p, point.x, point.y, point.z), t, label, lines, Rng.Next(Mathf.Max(1, lines.Length)));
        }

        /// <summary>A rack of long guns drawn by <paramref name="draw"/>, chained up: look it over (there's seldom anything loose
        /// on it), or try to lift a rifle out.</summary>
        public StorageFixture WeaponRack(RaidRoom room, Place p, Vector3 size, System.Action<RaidMesh, System.Random> draw, string name)
        {
            Transform t = FixtureBody(room, p, size, true, draw, name);
            StorageOptions o = SmallStock(ContainerType.GunLocker, "weapon rack");
            o.EmptyChance = CloneZConfig.CivicRackEmptyChance;
            o.MaxStacks = 1;
            o.LookLabel = "Look over the rack";
            o.ExtraActions = CivicRackActions;
            return StorageFixture.Create(t, name, Interactable.KeyFor("weaponrack", t.position), At(p, 0f, 1.0f, size.z * 0.5f + 0.05f), null, t, Rng.Next(), o);
        }

        private static void CivicRackActions(InteractContext ctx, List<InteractAction> into)
        {
            if (s_CivicTryRifle == null) s_CivicTryRifle = c => c.Notify(CivicRackLines[s_CivicRackLine++ % CivicRackLines.Length]);
            into.Add(new InteractAction("Try to lift out a rifle", s_CivicTryRifle));
        }
        #endregion

        #region The garage's car
        /// <summary>The sedan on the garage floor (a 1.8 × 1.4 × 4.4 m place, front toward the placement's +Z) in
        /// <paramref name="paint"/>: its trunk lid lifts on whatever was left in the trunk, and its front passenger door opens
        /// on the glovebox — both small places. Returns its body.</summary>
        public Transform GarageCar(RaidRoom room, Place p, Color paint)
        {
            Color c = paint;
            Transform t = FixtureBody(room, p, new Vector3(1.8f, 1.4f, 4.4f), true, (m, r) => c = FurnitureBuilder.GarageCarBody(m, r, paint), "Car");
            KitchenFrame f = FrameOf(p);
            Vector3 at = t.position;
            FixturePart lid = KitchenParts.Door(t, "TrunkLid", f, d => FurnitureBuilder.GarageCarTrunkLid(d, c),
                new Vector3(0f, FurnitureBuilder.CarBeltY + 0.03f, FurnitureBuilder.CarTrunkZ1), Vector3.back, Vector3.up, CloneZConfig.CivicCarTrunkAngle)
                .WithSpeed(0.8f);
            StorageOptions trunk = SmallStock(ContainerType.ToolBench, "trunk");
            trunk.ContainerName = "Car trunk";
            StorageFixture.Create(t, "Trunk", Interactable.KeyFor("cartrunk", at), f.P(0f, 1.0f, FurnitureBuilder.CarTrunkZ0 + 0.2f), new[] { lid }, t, Rng.Next(), trunk);

            FixturePart door = KitchenParts.Door(t, "PassengerDoor", f, d => FurnitureBuilder.GarageCarDoor(d, c),
                new Vector3(FurnitureBuilder.CarWid * 0.5f, 0.6f, FurnitureBuilder.CarDoorZ1), Vector3.back, Vector3.right, CloneZConfig.CivicCarDoorAngle)
                .WithSpeed(0.6f);
            StorageOptions glove = SmallStock(ContainerType.Desk, "passenger door");
            glove.ContainerName = "Glovebox";
            glove.LookLabel = "Rummage in the glovebox";
            StorageFixture.Create(t, "Glovebox", Interactable.KeyFor("glovebox", at), f.P(FurnitureBuilder.CarWid * 0.5f + 0.03f, 0.85f, 0.45f),
                new[] { door }, t, Rng.Next(), glove);
            return t;
        }
        #endregion
    }
}

