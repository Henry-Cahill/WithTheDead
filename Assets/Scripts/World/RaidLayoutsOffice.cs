using UnityEngine;

namespace CloneZ
{
    internal static partial class RaidLayouts
    {
        #region County office (photo 3)
        /// <summary>The county office: an open-plan floor of desks with CRTs and lamps (photo 3), a lobby
        /// with reception, a break room, the manager's office, records and a utility room. 20 × 14 m. Everything in it
        /// works: the desks' drawers, the swivel chairs, the filing cabinets, the copier (it jams), the water cooler, the
        /// break room's sink, counter, fridge, table and vending machine, the manager's bookcase, the records shelving, the
        /// utility room's water heater and the drums.</summary>
        public static RaidBuilding Office(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "CountyOffice", center, turns, false, 20f, 14f)
            {
                Kind = RaidBuildingKind.Office,
                Exterior = ExteriorStyle.Brick,
                ExteriorColor = new Color(0.56f, 0.46f, 0.36f),
                DoorWood = new Color(0.46f, 0.32f, 0.20f),
                Casing = new Color(0.62f, 0.60f, 0.54f),
            };
            System.Random rng = b.Rng;
            RaidRoom open = b.Room("Open Office", -10f, -7f, 3f, 3f, OfficeBlue, FloorKind.LinoBeige);
            open.Lit = true;
            RaidRoom lobby = b.Room("Lobby", 3f, -7f, 10f, -1f, TealBeige, FloorKind.LinoGrey);
            lobby.Lit = Chance(rng, 0.6);
            RaidRoom brk = b.Room("Break Room", 3f, -1f, 10f, 3f, KitchenMint, FloorKind.Checker);
            brk.Lit = Chance(rng, 0.55);
            brk.Flicker = Chance(rng, 0.5);
            RaidRoom mgr = b.Room("Manager", -10f, 3f, -3f, 7f, BrownGreen, FloorKind.Carpet);
            mgr.FloorTint = new Color(0.24f, 0.30f, 0.22f);
            mgr.Light = LightStyle.Dome;
            RaidRoom rec = b.Room("Records", -3f, 3f, 3f, 7f, OfficeBlue, FloorKind.LinoGrey);
            RaidRoom util = b.Room("Utility", 3f, 3f, 10f, 7f, GarageGrey, FloorKind.Concrete);
            util.Light = LightStyle.Bulb;

            b.Door(6.5f, -7f, 1.8f, true, true);
            b.Door(3f, -4.0f, 1.5f);
            b.Door(5.0f, -1f, 1.4f);
            b.Door(3f, 1.0f, 1.4f);
            b.Door(-5.5f, 3f, 1.4f);
            b.Door(0.6f, 3f, 1.4f);
            b.Door(8.3f, 3f, 1.4f);
            b.Door(6.0f, 7f, 1.5f);
            b.Door(-10f, -3.0f, 1.4f);
            b.Window(-8.0f, -7f); b.Window(-4.5f, -7f); b.Window(-1.0f, -7f); b.Window(8.8f, -7f);
            b.Window(-10f, 0.8f); b.Window(-10f, 5.0f);
            b.Window(-6.5f, 7f); b.Window(0f, 7f);
            b.Window(10f, -4f); b.Window(10f, 1.0f);
            b.BuildShell();

            // --- Open office: the photo-3 corner on the north wall ---
            WorkDesk(b, open, b.N(open, -2.4f, 0.75f), 1.5f, FurnitureBuilder.Oak, true, 2, true, true, "Office desk");
            DrumAt(b, open, b.N(open, -3.75f, 0.6f), new Color(0.16f, 0.26f, 0.42f), false);
            DrumAt(b, open, b.N(open, -0.95f, 0.6f), new Color(0.45f, 0.24f, 0.10f), false);
            SeatAt(b, open, new RaidBuilding.Place(-2.4f, 1.55f, 0f), Sz(0.45f, 0.7f, 0.45f), (m, r) => FurnitureBuilder.Stool(m, r, new Color(0.16f, 0.16f, 0.18f)),
                "Stool", new Vector3(0f, CloneZConfig.RaidStoolSeatHeight, 0f), CloneZConfig.RaidStoolComfort, "Sit on the stool", "Get down off the stool");
            b.Furnish(open, new RaidBuilding.Place(-0.9f, 0.9f, 20f), Sz(0.5f, 0.4f, 0.4f), false, (m, r) => FurnitureBuilder.CardboardBox(m, r, Vector3.zero, Sz(0.5f, 0.38f, 0.4f), 0f), "box");
            b.RugAt(-2.4f, 0.3f, 2.4f, 1.6f, 0f, 1);
            b.Poster(open, 'N', -2.4f, 1.75f, 0.45f, 0.65f, 1);
            b.Clock(open, 'N', -7.0f, 2.2f);

            // A pod of four desks, back to back: two of them the day's working desks, two half cleared.
            WorkDesk(b, open, new RaidBuilding.Place(-7.25f, -2.375f, 180f), 1.5f, Putty, false, 0, false, true, "Office desk");
            WorkDesk(b, open, new RaidBuilding.Place(-5.75f, -2.375f, 180f), 1.5f, Putty, false, 1, false, false);
            WorkDesk(b, open, new RaidBuilding.Place(-7.25f, -1.625f, 0f), 1.5f, Putty, Chance(rng, 0.3), 0, false, false);
            WorkDesk(b, open, new RaidBuilding.Place(-5.75f, -1.625f, 0f), 1.5f, Putty, true, 2, false, true, "Office desk");
            Color chair = new Color(0.20f, 0.24f, 0.32f);
            SwivelChairAt(b, open, new RaidBuilding.Place(-7.25f, -3.2f, 0f), chair);
            SwivelChairAt(b, open, new RaidBuilding.Place(-5.7f, -3.3f, 18f), chair);
            SwivelChairAt(b, open, new RaidBuilding.Place(-7.3f, -0.8f, 180f), chair);
            SwivelChairAt(b, open, new RaidBuilding.Place(-5.75f, -0.75f, 205f), chair);

            b.FilingCabinet(open, b.Wst(open, 2.05f, 0.62f), 4, Putty);
            b.FilingCabinet(open, b.Wst(open, 2.53f, 0.62f), 4, Putty);
            WorkDesk(b, open, b.S(open, -1.0f, 0.75f), 1.5f, FurnitureBuilder.Oak, false, 1, false, false);
            SwivelChairAt(b, open, new RaidBuilding.Place(-1.0f, -5.7f, 180f), chair);
            CopierAt(b, open, b.E(open, -1.5f, 0.6f));
            WaterCoolerAt(b, open, b.E(open, -6.0f, 0.32f));
            b.Furnish(open, new RaidBuilding.Place(-9.4f, -6.4f, 0f), Sz(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "plant");
            b.CeilingLight(open, -6.5f, -2.0f);
            b.CeilingLight(open, -2.4f, -1.0f);
            b.CeilingLight(open, 0.8f, -4.5f);
            b.SpawnPoint(open, -2.4f, -1.0f, 0f);
            b.Litter(open, 0.9f);

            // --- Lobby ---
            WorkDesk(b, lobby, new RaidBuilding.Place(8.2f, -3.3f, 180f), 2.0f, FurnitureBuilder.Walnut, false, 0, false, false, "Reception desk");
            SwivelChairAt(b, lobby, new RaidBuilding.Place(8.4f, -2.55f, 180f), new Color(0.22f, 0.20f, 0.18f));
            WoodChairAt(b, lobby, b.Wst(lobby, -6.3f, 0.45f), FurnitureBuilder.Walnut);
            WoodChairAt(b, lobby, b.Wst(lobby, -5.75f, 0.45f), FurnitureBuilder.Walnut);
            b.Furnish(lobby, new RaidBuilding.Place(9.4f, -6.4f, 0f), Sz(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "plant");
            b.Poster(lobby, 'E', -1.9f, 1.5f, 0.45f, 0.65f, 3);
            b.CeilingLight(lobby, 6.5f, -4.0f);
            b.Litter(lobby, 0.7f);

            // --- Break room: the home's own sink unit, counter and fridge, wall cupboards over them, a table to eat at ---
            RaidBuilding.Place sink = b.N(brk, 3.75f, 0.63f);
            Transform sinkBody = b.FixtureBody(brk, sink, Sz(1.22f, 1.0f, 0.63f), false, null, "Sink");
            WaterSource.Spawn(sinkBody, b.At(sink), WaterSourceKind.Tap, b.Yaw(sink.Yaw));
            b.HomePiece(brk, b.N(brk, 4.99f, 0.63f), HomeFurnitureKind.Counter);
            b.UpperCabinets(brk, 'N', 4.37f, 2.44f, FurnitureBuilder.HomeCabinetPaint);
            b.HomePiece(brk, b.E(brk, -0.3f, 0.70f), HomeFurnitureKind.Fridge);
            b.HomePiece(brk, new RaidBuilding.Place(6.4f, 1.3f, 0f), HomeFurnitureKind.Table);
            b.HomePiece(brk, new RaidBuilding.Place(6.4f, 0.55f, 0f), HomeFurnitureKind.Chair);
            b.HomePiece(brk, new RaidBuilding.Place(6.4f, 2.05f, 180f), HomeFurnitureKind.Chair);
            VendingAt(b, brk, b.S(brk, 8.0f, 0.8f), brk.Lit);
            b.CeilingLight(brk, 6.5f, 1.0f);
            b.Litter(brk, 0.8f);

            // --- Manager ---
            WorkDesk(b, mgr, new RaidBuilding.Place(-8.0f, 5.0f, 270f), 1.8f, FurnitureBuilder.Walnut, false, Chance(rng, 0.5) ? 2 : 1, false, true, "Manager's desk");
            SwivelChairAt(b, mgr, new RaidBuilding.Place(-8.85f, 5.0f, 90f), new Color(0.18f, 0.10f, 0.08f));
            WoodChairAt(b, mgr, new RaidBuilding.Place(-6.9f, 4.5f, 270f), FurnitureBuilder.Walnut);
            WoodChairAt(b, mgr, new RaidBuilding.Place(-6.9f, 5.55f, 255f), FurnitureBuilder.Walnut);
            BookcaseAt(b, mgr, b.E(mgr, 5.5f, 0.32f), FurnitureBuilder.Walnut);
            b.FilingCabinet(mgr, b.N(mgr, -4.5f, 0.62f), 4, Putty);
            b.RugAt(-7.6f, 5.0f, 2.2f, 1.6f, 90f, 0);
            b.Poster(mgr, 'E', 4.0f, 1.6f, 0.5f, 0.7f, 2);
            b.CeilingLight(mgr, -6.5f, 5.0f);
            b.Litter(mgr, 0.6f);

            // --- Records ---
            Color records = new Color(0.40f, 0.42f, 0.40f);
            b.Shelving(rec, b.Wst(rec, 5.0f, 0.5f), Sz(1.4f, 1.9f, 0.5f), 5, 1, records, ContainerType.Shelf, "Records shelf", true);
            b.Shelving(rec, b.E(rec, 5.0f, 0.5f), Sz(1.4f, 1.9f, 0.5f), 5, 1, records, ContainerType.Shelf, "Records shelf", false);
            b.FilingCabinet(rec, b.N(rec, -1.6f, 0.62f), 4, Putty);
            b.FilingCabinet(rec, b.N(rec, 1.6f, 0.62f), 4, Putty);
            b.Clutter(rec, 3, false);
            b.CeilingLight(rec, 0f, 5f);
            b.Litter(rec, 1.2f);

            // --- Utility ---
            WaterHeaterAt(b, util, new RaidBuilding.Place(9.4f, 4.8f, 270f));
            b.Shelving(util, b.N(util, 8.4f, 0.5f), Sz(1.4f, 1.9f, 0.5f), 4, 3, new Color(0.30f, 0.32f, 0.34f), ContainerType.Shelf, "Supply shelf", true);
            DrumAt(b, util, b.Wst(util, 5.5f, 0.6f), new Color(0.12f, 0.12f, 0.13f), false);
            DrumAt(b, util, b.Wst(util, 6.15f, 0.6f), new Color(0.16f, 0.26f, 0.42f), false);
            b.Clutter(util, 2, false);
            b.CeilingLight(util, 6.5f, 5.0f);
            b.Litter(util, 0.6f);

            b.DoorSign(6.5f, -7f, "COUNTY OFFICE", new Color(0.12f, 0.18f, 0.32f), new Color(0.88f, 0.86f, 0.80f));
            b.Finish();
            return b;
        }
        #endregion

        #region Office furniture, made usable
        /// <summary>A swivel office chair upholstered in <paramref name="fabric"/> (0.55 × 1.0 × 0.55, walk-round like the
        /// old chairs): sat in, spun round, given a shove (<see cref="RaidStoreSwivelSeat"/>).</summary>
        private static void SwivelChairAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, Color fabric)
        {
            Transform t = b.FixtureBody(room, p, Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, fabric), "Office chair");
            RaidStoreSwivelSeat.Create(t, b.Yaw(p.Yaw));
        }

        /// <summary>Something to sit on (a stool, a wooden chair) of <paramref name="size"/>, walk-round like the old ones:
        /// the hips rest at <paramref name="seat"/> in its own frame (<see cref="SeatFixture"/>).</summary>
        private static void SeatAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, Vector3 size, System.Action<RaidMesh, System.Random> draw,
            string name, Vector3 seat, float comfort, string sit, string stand)
        {
            Transform t = b.FixtureBody(room, p, size, false, draw, name);
            SeatFixture.Create(t, name, seat, b.Yaw(p.Yaw), comfort, sit, stand, false);
        }

        /// <summary>A plain wooden chair (0.45 × 0.9 × 0.45) to sit on, like the home's dining chairs.</summary>
        private static void WoodChairAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, Color wood)
            => SeatAt(b, room, p, Sz(0.45f, 0.9f, 0.45f), (m, r) => FurnitureBuilder.WoodChair(m, r, wood), "Chair",
                new Vector3(0f, CloneZConfig.ChairSeatHeight, CloneZConfig.ChairSeatBack), CloneZConfig.ChairComfort, "Sit down", "Stand up");

        /// <summary>A 55-gallon drum (0.6 × 0.9 × 0.6) painted <paramref name="paint"/>, its lid to prise off and whatever's
        /// settled in it to look through — seldom much.</summary>
        private static void DrumAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, Color paint, bool hazard)
        {
            Color c = paint;
            Transform t = b.FixtureBody(room, p, Sz(0.6f, 0.9f, 0.6f), true, (m, r) => c = FurnitureBuilder.OfficeDrumBody(m, r, paint, hazard), "Drum");
            KitchenFrame f = b.FrameOf(p);
            FixturePart lid = KitchenParts.Door(t, "DrumLid", f, m => FurnitureBuilder.OfficeDrumLid(m, c), new Vector3(-0.3f, 0.882f, 0f),
                Vector3.right, Vector3.up, 100f);
            StorageFixture.Create(t, "Drum", Interactable.KeyFor("drum", t.position), b.At(p, 0f, 0.95f, 0.32f), new[] { lid }, t, b.Rng.Next(),
                new StorageOptions
                {
                    Noun = "drum",
                    OpenVerb = "Prise the lid off",
                    CloseVerb = "Put the lid back on",
                    LookLabel = "Look in the drum",
                    LootType = ContainerType.Shelf,
                    EmptyChance = CloneZConfig.RaidDrumEmptyChance,
                    MaxStacks = CloneZConfig.RaidDrumMaxStacks,
                    Noise = CloneZConfig.RaidDrumLidNoise,
                    RaidBoost = false,
                });
        }

        /// <summary>The photocopier (0.9 × 1.1 × 0.6): its lid lifts, it copies — noisily — and it jams (<see cref="RaidStoreCopier"/>).</summary>
        private static void CopierAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p)
        {
            Transform t = b.FixtureBody(room, p, Sz(0.9f, 1.1f, 0.6f), true, (m, r) => FurnitureBuilder.OfficeCopierBody(m, r), "Photocopier");
            KitchenFrame f = b.FrameOf(p);
            FixturePart lid = KitchenParts.Door(t, "CopierLid", f, FurnitureBuilder.OfficeCopierLid, FurnitureBuilder.OfficeCopierLidHinge,
                Vector3.forward, Vector3.up, 70f);
            MeshRenderer jam = KitchenParts.PieceAt(t, "JamLamp", f, FurnitureBuilder.OfficeCopierJamLamp, FurnitureBuilder.OfficeCopierJamGlow);
            RaidStoreCopier.Create(t, f.P(FurnitureBuilder.OfficeCopierPanel), lid, jam, b.Rng.Next());
        }

        /// <summary>The bottled-water cooler (0.32 × 1.4 × 0.32): drink, fill a bottle, watch the jug go down
        /// (<see cref="RaidStoreWaterCooler"/>).</summary>
        private static void WaterCoolerAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p)
        {
            Transform t = b.FixtureBody(room, p, Sz(0.32f, 1.4f, 0.32f), true, (m, r) => FurnitureBuilder.OfficeWaterCoolerBody(m, r), "Water cooler");
            KitchenFrame f = b.FrameOf(p);
            MeshRenderer water = KitchenParts.PieceAt(t, "JugWater", f, new Vector3(0f, FurnitureBuilder.OfficeJugBase + 0.012f, 0f), FurnitureBuilder.OfficeJugWater);
            RaidStoreWaterCooler.Create(t, f.P(FurnitureBuilder.OfficeCoolerTaps), water, b.Rng.Next());
        }

        /// <summary>The snack machine (0.9 × 1.85 × 0.8), glowing when <paramref name="lit"/>: thumped, or its glass put in
        /// (<see cref="RaidStoreVending"/>).</summary>
        private static void VendingAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, bool lit)
        {
            Transform t = b.FixtureBody(room, p, Sz(0.9f, 1.85f, 0.8f), true, (m, r) => FurnitureBuilder.OfficeVendingBody(m, r), "Vending machine");
            KitchenFrame f = b.FrameOf(p);
            int seed = b.Rng.Next();
            var wr = new System.Random(seed);
            var at = new Vector3(-0.1f, 1.1f, 0.37f);
            MeshRenderer glass = KitchenParts.PieceInFrame(t, "VendingGlass", f, at, m => FurnitureBuilder.OfficeVendingWindow(m, wr, lit, false));
            MeshRenderer smashed = KitchenParts.PieceInFrame(t, "VendingSmashed", f, at, m => FurnitureBuilder.OfficeVendingWindow(m, wr, lit, true));
            RaidStoreVending.Create(t, f.P(FurnitureBuilder.OfficeVendingPanel), f.P(FurnitureBuilder.OfficeVendingTray), glass, smashed, lit, seed);
        }

        /// <summary>The utility room's gas water heater (0.6 × 1.6 × 0.6, its pipes running on up to the ceiling): the tank
        /// to drain, the pilot to check (<see cref="RaidStoreWaterHeater"/>).</summary>
        private static void WaterHeaterAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p)
        {
            Transform t = b.FixtureBody(room, p, Sz(0.6f, 1.6f, 0.6f), true, (m, r) => FurnitureBuilder.OfficeWaterHeater(m, r), "Water heater");
            KitchenFrame f = b.FrameOf(p);
            MeshRenderer pilot = KitchenParts.PieceAt(t, "PilotFlame", f, FurnitureBuilder.OfficeHeaterPilot + new Vector3(0f, 0f, 0.003f),
                FurnitureBuilder.OfficeHeaterPilotGlow);
            RaidStoreWaterHeater.Create(t, f.P(FurnitureBuilder.OfficeHeaterValve), f.P(0f, 0.55f, 0.36f), pilot, b.Rng.Next());
        }

        /// <summary>A bookcase (0.9 × 1.8 × 0.32) in <paramref name="wood"/>: its shelves to browse, and something to read
        /// (<see cref="LivingReading"/>) — the home's bookcase in the office's own wood.</summary>
        private static void BookcaseAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, Color wood)
        {
            Transform t = b.FixtureBody(room, p, Sz(0.9f, 1.8f, 0.32f), true, (m, r) => FurnitureBuilder.Bookshelf(m, r, wood), "Bookcase");
            StorageOptions o = RaidBuilding.SmallStock(ContainerType.Shelf, "bookcase");
            o.LookLabel = "Browse the shelves";
            o.ExtraActions = LivingReading.AddActions;
            StorageFixture.Create(t, "Bookcase", Interactable.KeyFor("bookcase", t.position), b.At(p, 0f, 1.2f, 0.18f), null, t, b.Rng.Next(), o);
        }
        #endregion
    }
}
