using UnityEngine;

namespace CloneZ
{
    internal static partial class RaidLayouts
    {
        private static readonly Color[] SidingColors =
        {
            new Color(0.82f, 0.80f, 0.74f), new Color(0.80f, 0.74f, 0.52f), new Color(0.56f, 0.62f, 0.68f),
            new Color(0.62f, 0.72f, 0.62f), new Color(0.76f, 0.58f, 0.50f),
        };

        private static readonly Color[] Fabrics =
        {
            new Color(0.56f, 0.44f, 0.18f), new Color(0.36f, 0.24f, 0.16f), new Color(0.30f, 0.32f, 0.22f),
            new Color(0.30f, 0.36f, 0.44f), new Color(0.40f, 0.14f, 0.12f),
        };

        private static readonly Color[] Blankets =
        {
            new Color(0.55f, 0.20f, 0.16f), new Color(0.22f, 0.30f, 0.46f), new Color(0.62f, 0.56f, 0.34f),
            new Color(0.28f, 0.40f, 0.30f), new Color(0.50f, 0.40f, 0.52f),
        };

        private static T Pick<T>(System.Random r, T[] a) => a[r.Next(a.Length)];

        #region Houses
        /// <summary>A family house: the 12 × 10 ranch (living room, kitchen, bedroom, bathroom with a bath, den) or the 10 × 9
        /// cottage (open living room with a kitchenette, bedroom, shower room, laundry), furnished like the starter home with
        /// its own working fittings (RaidBuilding.Home.cs): the kitchen's units, sink, range, fridge and radio, a dining
        /// table and chairs, the sofa, armchair, TV and bookcase, the beds, bedside tables, dressers and wardrobes, the
        /// bathroom's fittings, the laundry's washing machines.</summary>
        public static RaidBuilding House(RaidTown town, string name, Vector2 center, int turns, bool mirror, bool small)
        {
            System.Random pick = new System.Random(town.Rng.Next());
            var b = new RaidBuilding(town, name, center, turns, mirror, small ? 10f : 12f, small ? 9f : 10f)
            {
                Kind = RaidBuildingKind.House,
                Exterior = ExteriorStyle.Siding,
                ExteriorColor = Pick(pick, SidingColors),
                DoorWood = new Color(0.42f, 0.27f, 0.15f),
                Casing = Chance(pick, 0.5) ? new Color(0.36f, 0.24f, 0.14f) : new Color(0.70f, 0.68f, 0.62f),
            };
            if (small) Cottage(b);
            else Ranch(b);
            b.Finish();
            return b;
        }

        private static void Ranch(RaidBuilding b)
        {
            System.Random rng = b.Rng;
            RaidRoom living = b.Room("Living Room", -6f, -5f, 0f, 0.5f, Pick(rng, HomeLiving), Chance(rng, 0.5) ? FloorKind.Wood : FloorKind.Carpet);
            living.FloorTint = Pick(rng, new[] { new Color(0.36f, 0.30f, 0.22f), new Color(0.26f, 0.30f, 0.36f), new Color(0.30f, 0.20f, 0.18f) });
            living.Light = LightStyle.Dome;
            living.Lit = Chance(rng, 0.55);
            RaidRoom kitchen = b.Room("Kitchen", 0f, -5f, 6f, 0.5f, KitchenMint, FloorKind.Checker);
            kitchen.Light = LightStyle.Dome;
            kitchen.Lit = Chance(rng, 0.55);
            kitchen.Flicker = Chance(rng, 0.3);
            RaidRoom bed = b.Room("Bedroom", -6f, 0.5f, -1.5f, 5f, Pick(rng, HomeBed), FloorKind.Carpet);
            bed.FloorTint = Pick(rng, new[] { new Color(0.42f, 0.30f, 0.30f), new Color(0.24f, 0.28f, 0.36f), new Color(0.40f, 0.36f, 0.28f) });
            bed.Light = LightStyle.Dome;
            bed.Lit = Chance(rng, 0.4);
            RaidRoom bath = b.Room("Bathroom", -1.5f, 0.5f, 1.5f, 5f, KitchenMint, FloorKind.LinoGreen);
            bath.Light = LightStyle.Bulb;
            RaidRoom den = b.Room("Den", 1.5f, 0.5f, 6f, 5f, Pick(rng, HomeBed), FloorKind.Wood);
            den.Light = LightStyle.Dome;
            den.Lit = Chance(rng, 0.4);
            if (Chance(rng, 0.5)) b.Overgrown.Add(living);

            b.Door(-3.0f, -5f, 1.5f);
            b.Door(0f, -2.0f, 1.5f);
            b.Door(-3.6f, 0.5f, 1.4f);
            b.Door(-1.5f, 3.6f, 1.4f);
            b.Door(3.0f, 0.5f, 1.4f);
            b.Door(6f, -1.5f, 1.4f);
            b.Window(-5.0f, -5f); b.Window(-1.0f, -5f); b.Window(3.0f, -5f);
            b.Window(-6f, -2.0f); b.Window(-6f, 2.8f);
            b.Window(-2.6f, 5f); b.Window(-0.6f, 5f, 0.8f); b.Window(3.8f, 5f);
            b.Window(6f, 2.8f);
            b.BuildShell();

            // Living room: the sofa against the side wall, facing the TV across the coffee table; an armchair angled at the
            // set from the corner by the front door; a bookcase, and a standard lamp at the end of the sofa.
            Color fabric = Pick(rng, Fabrics);
            b.HouseSofa(living, b.Wst(living, -2.0f, 0.88f), 2.0f, fabric);
            b.HouseCoffeeTable(living, new RaidBuilding.Place(-3.7f, -2.0f, 90f), 1.0f, 0.55f);
            b.HouseTv(living, new RaidBuilding.Place(-1.9f, -2.0f, 270f));
            b.HomePiece(living, new RaidBuilding.Place(-4.9f, -3.95f, 55f), HomeFurnitureKind.Armchair);
            b.HomePiece(living, b.N(living, -5.2f, 0.32f), HomeFurnitureKind.Bookshelf);
            b.LeaveLampOn(b.HomePiece(living, new RaidBuilding.Place(-5.55f, -0.6f, 90f), HomeFurnitureKind.FloorLamp), 0.3);
            b.RugAt(-3.6f, -2.0f, 2.4f, 1.7f, 90f, rng.Next(3));
            b.Poster(living, 'N', -1.6f, 1.55f, 0.6f, 0.45f, 2);
            b.CeilingLight(living, -3.0f, -2.2f);
            b.Litter(living, 0.7f);

            // Kitchen: along the front wall the fridge, a counter with the radio on it and the wall cupboards over it, the sink
            // under the window and the gas range; the dining table and three chairs in the middle of the floor.
            RaidBuilding.Place fridge = b.S(kitchen, 0.78f, 0.70f);
            b.HomePiece(kitchen, fridge, HomeFurnitureKind.Fridge);
            float offX = -0.30f + (float)rng.NextDouble() * 0.04f; // slight random left‑right variation
            float offY = 0.45f + (float)rng.NextDouble() * 0.02f; // slight random height variation
            b.Static.Box(b.W(fridge.X + offX, offY, fridge.Z + 0.34f), new Vector3(0.02f, 0.10f, 0.02f), DecorPalette.Metal(new Color(0.55f, 0.55f, 0.55f))); // metal handle
            RaidBuilding.Place counter = b.S(kitchen, 1.77f, 0.63f);
            b.HomePiece(kitchen, counter, HomeFurnitureKind.Counter);
            b.HomePiece(kitchen, b.OffsetPlace(counter, -0.25f, -0.075f), HomeFurnitureKind.Radio);
            b.UpperCabinets(kitchen, 'S', 1.72f, 1.1f, FurnitureBuilder.HomeCabinetPaint);
            // TODO: add small metal knob primitive box here (rng varies offset and colour)
            b.HouseSink(kitchen, b.S(kitchen, 3.0f, 0.63f));
            b.HouseStove(kitchen, b.S(kitchen, 4.05f, 0.62f));
            b.HomePiece(kitchen, new RaidBuilding.Place(2.7f, -2.6f, 0f), HomeFurnitureKind.Table);
            b.HomePiece(kitchen, new RaidBuilding.Place(1.75f, -2.6f, 90f), HomeFurnitureKind.Chair);
            b.HomePiece(kitchen, new RaidBuilding.Place(3.65f, -2.6f, 270f), HomeFurnitureKind.Chair);
            b.HomePiece(kitchen, new RaidBuilding.Place(2.7f, -1.75f, 180f), HomeFurnitureKind.Chair);
            b.Clock(kitchen, 'N', 1.0f, 2.1f);
            b.CeilingLight(kitchen, 3.0f, -2.2f);
            b.Litter(kitchen, 0.8f);

            // Bedroom: the double bed against the back wall between two bedside tables, the wardrobe by the door, and the
            // dresser with a portable TV on it, turned toward the bed.
            b.HouseBed(bed, b.N(bed, -4.6f, 2.0f), Pick(rng, Blankets));
            b.LeaveLampOn(b.HomePiece(bed, b.N(bed, -3.45f, 0.4f), HomeFurnitureKind.Nightstand), 0.4);
            b.LeaveLampOn(b.HomePiece(bed, b.N(bed, -5.6f, 0.4f), HomeFurnitureKind.Nightstand), 0.2);
            b.Wardrobe(bed, b.Wst(bed, 1.4f, 0.6f), FurnitureBuilder.Walnut);
            RaidBuilding.Place dresser = b.E(bed, 1.95f, 0.5f);
            b.HomePiece(bed, dresser, HomeFurnitureKind.Dresser);
            b.HomePiece(bed, b.OffsetPlace(dresser, -0.18f, 0.05f, 30f), HomeFurnitureKind.Tv);
            b.RugAt(-3.0f, 2.3f, 1.6f, 1.0f, 0f, rng.Next(3));
            b.CeilingLight(bed, -3.8f, 2.6f);
            b.Litter(bed, 0.5f);

            // Bathroom: the bath along the front wall with its taps at the end by the side wall, the toilet with the towel
            // rail over it, and the basin under the mirror cabinet.
            b.HouseBath(bath, b.S(bath, 0.6f, FurnitureBuilder.BathDepth), !b.Mirror);
            b.HomePiece(bath, b.E(bath, 2.4f, 0.7f), HomeFurnitureKind.Toilet);
            b.HangTowelRail(bath, 'E', 2.4f, 1.42f);
            b.HomePiece(bath, b.N(bath, 0.9f, 0.45f), HomeFurnitureKind.Basin);
            b.HangBathCabinet(bath, 'N', 0.9f, 1.5f);
            b.CeilingLight(bath, 0f, 2.8f);
            b.Litter(bath, 0.4f);

            // Den: a study (the desk under the back window with its chair, a bookcase and a reading lamp, and a wardrobe or a
            // storage box) or a spare room (a bed, a homework desk and its chair, a wardrobe and a bookcase).
            Color seat = Pick(rng, Fabrics);
            if (Chance(rng, 0.55))
            {
                WorkDesk(b, den, b.N(den, 4.95f, 0.75f), 1.5f, FurnitureBuilder.Oak, false, 1, false, false);
                b.HouseDeskChair(den, new RaidBuilding.Place(4.95f, 3.7f, 0f), seat);
                b.HomePiece(den, b.Wst(den, 3.5f, 0.32f), HomeFurnitureKind.Bookshelf);
                b.LeaveLampOn(b.HomePiece(den, new RaidBuilding.Place(1.85f, 4.45f, 90f), HomeFurnitureKind.FloorLamp), 0.25);
                if (Chance(rng, 0.5)) b.Wardrobe(den, b.E(den, 1.3f, 0.6f), FurnitureBuilder.Pine);
                else b.SupplyCrate(den, b.E(den, 1.4f, 0.6f), Sz(0.9f, 0.6f, 0.6f), false, ContainerType.Crate, "Storage box");
                b.Clutter(den, 2, false);
            }
            else
            {
                b.HouseBed(den, b.N(den, 2.4f, 2.0f), Pick(rng, Blankets));
                WorkDesk(b, den, b.E(den, 4.1f, 0.75f), 1.2f, FurnitureBuilder.Pine, false, 1, false, false);
                b.HouseDeskChair(den, new RaidBuilding.Place(4.65f, 4.1f, 90f), seat);
                b.Wardrobe(den, b.E(den, 1.3f, 0.6f), FurnitureBuilder.Pine);
                b.HomePiece(den, b.Wst(den, 1.4f, 0.32f), HomeFurnitureKind.Bookshelf);
                b.RugAt(3.4f, 2.3f, 1.4f, 1.0f, 0f, rng.Next(3));
                b.Clutter(den, 1, false);
            }
            b.CeilingLight(den, 3.8f, 2.8f);
            b.Litter(den, 0.5f);
        }

        private static void Cottage(RaidBuilding b)
        {
            System.Random rng = b.Rng;
            RaidRoom living = b.Room("Living Room", -5f, -4.5f, 5f, 0.5f, Pick(rng, HomeLiving), FloorKind.Wood);
            living.Light = LightStyle.Dome;
            living.Lit = Chance(rng, 0.55);
            RaidRoom bed = b.Room("Bedroom", -5f, 0.5f, 0f, 4.5f, Pick(rng, HomeBed), FloorKind.Carpet);
            bed.FloorTint = Pick(rng, new[] { new Color(0.42f, 0.30f, 0.30f), new Color(0.24f, 0.28f, 0.36f) });
            bed.Light = LightStyle.Dome;
            bed.Lit = Chance(rng, 0.3);
            RaidRoom bath = b.Room("Bathroom", 0f, 0.5f, 2f, 4.5f, KitchenMint, FloorKind.Checker);
            bath.Light = LightStyle.Bulb;
            RaidRoom laundry = b.Room("Laundry", 2f, 0.5f, 5f, 4.5f, GarageGrey, FloorKind.Concrete);
            laundry.Light = LightStyle.Bulb;
            if (Chance(rng, 0.5)) b.Overgrown.Add(living);

            b.Door(-1.5f, -4.5f, 1.5f);
            b.Door(-2.5f, 0.5f, 1.4f);
            b.Door(1.0f, 0.5f, 1.4f);
            b.Door(3.5f, 0.5f, 1.4f);
            b.Door(5f, 2.5f, 1.4f);
            b.Window(-3.8f, -4.5f); b.Window(1.5f, -4.5f); b.Window(3.8f, -4.5f);
            b.Window(-5f, -2.0f); b.Window(-5f, 2.5f);
            b.Window(-2.5f, 4.5f);
            b.BuildShell();

            // Open living room: a kitchenette in the corner by the laundry door — the sink unit in the corner, a counter with
            // the radio on it and the wall cupboards over it, the gas range, and the fridge between the front windows; a
            // dining table and chairs; and the sofa, an armchair and a bookcase round the TV.
            b.HouseSink(living, b.E(living, -3.77f, 0.63f));
            RaidBuilding.Place counter = b.E(living, -2.53f, 0.63f);
            b.HomePiece(living, counter, HomeFurnitureKind.Counter);
            b.HomePiece(living, b.OffsetPlace(counter, -0.25f, -0.075f), HomeFurnitureKind.Radio);
            b.UpperCabinets(living, 'E', -2.53f, 1.2f, FurnitureBuilder.HomeCabinetPaint);
            b.HouseStove(living, b.E(living, -1.52f, 0.62f));
            b.HomePiece(living, b.S(living, 2.65f, 0.70f), HomeFurnitureKind.Fridge);
            b.HomePiece(living, new RaidBuilding.Place(2.2f, -2.4f, 0f), HomeFurnitureKind.Table);
            b.HomePiece(living, new RaidBuilding.Place(1.25f, -2.4f, 90f), HomeFurnitureKind.Chair);
            b.HomePiece(living, new RaidBuilding.Place(3.15f, -2.4f, 270f), HomeFurnitureKind.Chair);
            b.HomePiece(living, new RaidBuilding.Place(2.2f, -1.55f, 180f), HomeFurnitureKind.Chair);
            Color fabric = Pick(rng, Fabrics);
            b.HouseSofa(living, b.Wst(living, -2.0f, 0.88f), 2.0f, fabric);
            b.HouseCoffeeTable(living, new RaidBuilding.Place(-2.8f, -2.0f, 90f), 1.0f, 0.55f);
            b.HouseTv(living, new RaidBuilding.Place(-1.6f, -2.0f, 270f));
            b.HomePiece(living, new RaidBuilding.Place(-4.25f, -3.75f, 55f), HomeFurnitureKind.Armchair);
            b.HomePiece(living, b.N(living, -4.35f, 0.32f), HomeFurnitureKind.Bookshelf);
            b.LeaveLampOn(b.HomePiece(living, new RaidBuilding.Place(-4.6f, -0.6f, 90f), HomeFurnitureKind.FloorLamp), 0.3);
            b.RugAt(-2.9f, -2.0f, 2.2f, 1.6f, 90f, rng.Next(3));
            b.CeilingLight(living, -2.5f, -2.0f);
            b.CeilingLight(living, 2.5f, -2.0f);
            b.Litter(living, 0.7f);

            // Bedroom: the bed against the back wall with a bedside table under the window (stood a little off the sill),
            // the wardrobe in the far corner and the dresser by the door.
            b.HouseBed(bed, b.N(bed, -3.8f, 2.0f), Pick(rng, Blankets));
            b.LeaveLampOn(b.HomePiece(bed, new RaidBuilding.Place(-2.65f, 4.11f, 180f), HomeFurnitureKind.Nightstand), 0.35);
            b.Wardrobe(bed, b.E(bed, 3.6f, 0.6f), FurnitureBuilder.Oak);
            b.HomePiece(bed, b.Wst(bed, 1.2f, 0.5f), HomeFurnitureKind.Dresser);
            b.CeilingLight(bed, -2.5f, 2.2f);
            b.Litter(bed, 0.5f);

            // Bathroom: the shower stall against the back wall with the toilet squeezed in beside it, the basin under the
            // mirror cabinet, and the towel rail across from it by the shower.
            b.HomePiece(bath, b.N(bath, 1.03f, 0.9f), HomeFurnitureKind.Shower);
            b.HomePiece(bath, b.N(bath, 0.35f, 0.7f), HomeFurnitureKind.Toilet);
            b.HomePiece(bath, b.E(bath, 2.4f, 0.45f), HomeFurnitureKind.Basin);
            b.HangBathCabinet(bath, 'E', 2.4f, 1.5f);
            b.HangTowelRail(bath, 'W', 3.0f, 1.42f);
            b.CeilingLight(bath, 1.0f, 2.6f);
            b.Litter(bath, 0.4f);

            // Laundry: a pair of washing machines along the wall the bathroom backs onto, and the steel shelving.
            b.HouseWasher(laundry, b.Wst(laundry, 3.2f, 0.65f));
            b.HouseWasher(laundry, b.Wst(laundry, 3.92f, 0.65f));
            b.Shelving(laundry, b.N(laundry, 4.0f, 0.45f), Sz(1.4f, 1.9f, 0.45f), 4, 0, new Color(0.40f, 0.40f, 0.38f), ContainerType.Shelf, "Shelf", true);
            b.CeilingLight(laundry, 3.5f, 2.5f);
            b.Litter(laundry, 0.6f);
        }
        #endregion
    }
}
