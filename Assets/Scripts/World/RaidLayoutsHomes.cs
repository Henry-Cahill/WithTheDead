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
        /// <summary>A family house: the 12 × 10 ranch (living room, kitchen, bedroom, bathroom, den) or
        /// the 10 × 9 cottage (open living / kitchenette, bedroom, bathroom, laundry).</summary>
        public static RaidBuilding House(RaidTown town, string name, Vector2 center, int turns, bool mirror, bool small)
        {
            System.Random pick = new System.Random(town.Rng.Next());
            var b = new RaidBuilding(town, name, center, turns, mirror, small ? 10f : 12f, small ? 9f : 10f)
            {
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

            // Living room.
            Color fabric = Pick(rng, Fabrics);
            b.Furnish(living, b.Wst(living, -2.0f, 0.88f), Sz(2.0f, 0.85f, 0.88f), true, (m, r) => FurnitureBuilder.Sofa(m, r, 2.0f, fabric), "sofa");
            b.Furnish(living, new RaidBuilding.Place(-3.7f, -2.0f, 90f), Sz(1.0f, 0.45f, 0.55f), true, (m, r) => FurnitureBuilder.CoffeeTable(m, r, 1.0f, 0.55f), "coffee table");
            b.Furnish(living, new RaidBuilding.Place(-1.9f, -2.0f, 270f), Sz(1.0f, 1.5f, 0.45f), true, (m, r) => FurnitureBuilder.TvStand(m, r), "tv");
            b.Furnish(living, new RaidBuilding.Place(-5.2f, -4.2f, 30f), Sz(0.95f, 0.85f, 0.88f), true, (m, r) => FurnitureBuilder.Sofa(m, r, 0.95f, Pick(r, Fabrics)), "armchair");
            b.Furnish(living, b.N(living, -5.2f, 0.32f), Sz(0.9f, 1.8f, 0.32f), true, (m, r) => FurnitureBuilder.Bookshelf(m, r, FurnitureBuilder.Oak), "bookshelf");
            b.RugAt(-3.6f, -2.0f, 2.4f, 1.7f, 90f, rng.Next(3));
            b.Poster(living, 'N', -1.6f, 1.55f, 0.6f, 0.45f, 2);
            b.CeilingLight(living, -3.0f, -2.2f);
            b.Litter(living, 0.7f);

            // Kitchen.
            Color cab = Chance(rng, 0.5) ? new Color(0.50f, 0.36f, 0.22f) : new Color(0.78f, 0.76f, 0.70f);
            Color top = new Color(0.66f, 0.60f, 0.46f);
            b.Furnish(kitchen, b.S(kitchen, 3.5f, 0.62f), Sz(2.4f, 1.1f, 0.62f), true, (m, r) => FurnitureBuilder.KitchenCounter(m, r, 2.4f, cab, top, true), "counter");
            b.Furnish(kitchen, b.S(kitchen, 5.2f, 0.66f), Sz(0.76f, 1.1f, 0.66f), true, (m, r) => FurnitureBuilder.Stove(m, r, new Color(0.84f, 0.83f, 0.78f)), "stove");
            b.Container(kitchen, ContainerType.Fridge, "Fridge", b.S(kitchen, 1.6f, 0.72f), Sz(0.76f, 1.75f, 0.72f),
                (m, r) => FurnitureBuilder.Fridge(m, r, Chance(r, 0.6) ? FurnitureBuilder.Cream : new Color(0.52f, 0.46f, 0.24f)));
            if (Chance(rng, 0.5))
            b.Furnish(kitchen, new RaidBuilding.Place(3.2f, -2.0f, 0f), Sz(1.2f, 0.8f, 0.8f), true, (m, r) => FurnitureBuilder.Table(m, r, 1.2f, 0.8f, FurnitureBuilder.Pine), "table");
            b.Furnish(kitchen, new RaidBuilding.Place(2.2f, -2.0f, 90f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Pine), "chair");
            b.Furnish(kitchen, new RaidBuilding.Place(4.2f, -2.1f, 250f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Pine), "chair");
            b.Clock(kitchen, 'N', 1.0f, 2.1f);
            b.CeilingLight(kitchen, 3.0f, -2.2f);
            b.Litter(kitchen, 0.8f);

            // Bedroom.
            Color blanket = Pick(rng, Blankets);
            b.Furnish(bed, b.N(bed, -4.6f, 2.0f), Sz(1.4f, 1.1f, 2.0f), true, (m, r) => FurnitureBuilder.Bed(m, r, 1.4f, 2.0f, blanket, FurnitureBuilder.Walnut), "bed");
            b.Furnish(bed, b.N(bed, -3.45f, 0.4f), Sz(0.45f, 1.0f, 0.4f), true, (m, r) =>
            {
                bool lit = Chance(r, 0.4);
                Vector3 bulb = FurnitureBuilder.Nightstand(m, r, FurnitureBuilder.Walnut, lit);
                if (lit) b.LampLight(RaidBuilding.WorldOf(m, bulb), false);
            }, "nightstand");
            b.Container(bed, ContainerType.Wardrobe, "Wardrobe", b.Wst(bed, 1.5f, 0.6f), Sz(1.0f, 1.95f, 0.6f), (m, r) => FurnitureBuilder.Wardrobe(m, r, FurnitureBuilder.Walnut));
            b.Furnish(bed, b.E(bed, 1.3f, 0.48f), Sz(1.0f, 0.95f, 0.48f), true, (m, r) => FurnitureBuilder.Dresser(m, r, FurnitureBuilder.Walnut), "dresser");
            b.RugAt(-3.0f, 2.3f, 1.6f, 1.0f, 0f, rng.Next(3));
            b.CeilingLight(bed, -3.8f, 2.6f);
            b.Litter(bed, 0.5f);

            // Bathroom.
            b.Furnish(bath, b.S(bath, 0.4f, 0.75f), Sz(1.6f, 0.7f, 0.75f), true, (m, r) => FurnitureBuilder.Bathtub(m, r), "tub");
            b.Furnish(bath, b.E(bath, 3.0f, 0.6f), Sz(0.45f, 0.8f, 0.6f), true, (m, r) => FurnitureBuilder.Toilet(m, r), "toilet");
            b.Container(bath, ContainerType.MedCabinet, "Bathroom Cabinet", b.N(bath, 0.9f, 0.45f), Sz(0.55f, 1.8f, 0.45f), (m, r) => FurnitureBuilder.WashBasin(m, r));
            b.CeilingLight(bath, 0f, 2.8f);
            b.Litter(bath, 0.4f);

            // Den: a child's room or a junk room with a desk.
            if (Chance(rng, 0.5))
            {
                Color bl = Pick(rng, Blankets);
                b.Furnish(den, b.N(den, 2.3f, 2.0f), Sz(1.0f, 1.1f, 2.0f), true, (m, r) => FurnitureBuilder.Bed(m, r, 1.0f, 2.0f, bl, FurnitureBuilder.Pine), "bed");
                if (Chance(rng, 0.4))
                    b.Container(den, ContainerType.Wardrobe, "Wardrobe", b.E(den, 1.3f, 0.6f), Sz(1.0f, 1.95f, 0.6f), (m, r) => FurnitureBuilder.Wardrobe(m, r, FurnitureBuilder.Pine));
                else
                    b.Furnish(den, b.E(den, 1.3f, 0.6f), Sz(1.0f, 1.95f, 0.6f), true, (m, r) => FurnitureBuilder.Wardrobe(m, r, FurnitureBuilder.Pine), "wardrobe");
            }
            else
            {
                b.Furnish(den, b.N(den, 4.8f, 0.75f), Sz(1.5f, 0.9f, 0.75f), true, (m, r) => OfficeDesk(b, m, r, 1.5f, FurnitureBuilder.Oak, false, 1, false), "desk");
                b.Furnish(den, new RaidBuilding.Place(4.8f, 3.65f, 180f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, Pick(r, Fabrics)), "chair");
                b.Furnish(den, b.Wst(den, 3.5f, 0.32f), Sz(0.9f, 1.8f, 0.32f), true, (m, r) => FurnitureBuilder.Bookshelf(m, r, FurnitureBuilder.Pine), "bookshelf");
                if (Chance(rng, 0.3))
                    b.Container(den, ContainerType.Crate, "Storage Box", b.E(den, 1.4f, 0.6f), Sz(0.9f, 0.6f, 0.6f), (m, r) => FurnitureBuilder.Crate(m, r, Vector3.zero, Sz(0.88f, 0.58f, 0.58f), 0f, false));
            }
            b.Clutter(den, 2, false);
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

            // Open living room with a kitchenette along the east wall.
            Color cab = new Color(0.78f, 0.76f, 0.70f);
            b.Furnish(living, b.E(living, -2.8f, 0.62f), Sz(1.6f, 1.1f, 0.62f), true, (m, r) => FurnitureBuilder.KitchenCounter(m, r, 1.6f, cab, new Color(0.66f, 0.60f, 0.46f), true), "counter");
            b.UpperCabinets(living, 'E', -2.8f, 1.6f, cab);
            b.Furnish(living, b.E(living, -4.0f, 0.66f), Sz(0.76f, 1.1f, 0.66f), true, (m, r) => FurnitureBuilder.Stove(m, r, new Color(0.84f, 0.83f, 0.78f)), "stove");
            b.Container(living, ContainerType.Fridge, "Fridge", b.E(living, -1.5f, 0.72f), Sz(0.76f, 1.75f, 0.72f), (m, r) => FurnitureBuilder.Fridge(m, r, FurnitureBuilder.Cream));
            b.Furnish(living, new RaidBuilding.Place(2.2f, -2.4f, 0f), Sz(1.0f, 0.8f, 0.8f), true, (m, r) => FurnitureBuilder.Table(m, r, 1.0f, 0.8f, FurnitureBuilder.Pine), "table");
            b.Furnish(living, new RaidBuilding.Place(1.3f, -2.4f, 90f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Pine), "chair");
            Color fabric = Pick(rng, Fabrics);
            b.Furnish(living, b.Wst(living, -2.0f, 0.88f), Sz(2.0f, 0.85f, 0.88f), true, (m, r) => FurnitureBuilder.Sofa(m, r, 2.0f, fabric), "sofa");
            b.Furnish(living, new RaidBuilding.Place(-2.9f, -2.0f, 90f), Sz(1.0f, 0.45f, 0.55f), true, (m, r) => FurnitureBuilder.CoffeeTable(m, r, 1.0f, 0.55f), "coffee table");
            b.Furnish(living, new RaidBuilding.Place(-1.6f, -2.0f, 270f), Sz(1.0f, 1.5f, 0.45f), true, (m, r) => FurnitureBuilder.TvStand(m, r), "tv");
            b.RugAt(-2.9f, -2.0f, 2.2f, 1.6f, 90f, rng.Next(3));
            b.CeilingLight(living, -2.5f, -2.0f);
            b.CeilingLight(living, 2.5f, -2.0f);
            b.Litter(living, 0.7f);

            // Bedroom.
            Color blanket = Pick(rng, Blankets);
            b.Furnish(bed, b.N(bed, -3.8f, 2.0f), Sz(1.4f, 1.1f, 2.0f), true, (m, r) => FurnitureBuilder.Bed(m, r, 1.4f, 2.0f, blanket, FurnitureBuilder.Oak), "bed");
            b.Furnish(bed, b.N(bed, -2.65f, 0.4f), Sz(0.45f, 1.0f, 0.4f), true, (m, r) =>
            {
                bool lit = Chance(r, 0.35);
                Vector3 bulb = FurnitureBuilder.Nightstand(m, r, FurnitureBuilder.Oak, lit);
                if (lit) b.LampLight(RaidBuilding.WorldOf(m, bulb), false);
            }, "nightstand");
            b.Container(bed, ContainerType.Wardrobe, "Wardrobe", b.E(bed, 3.6f, 0.6f), Sz(1.0f, 1.95f, 0.6f), (m, r) => FurnitureBuilder.Wardrobe(m, r, FurnitureBuilder.Oak));
            b.Furnish(bed, b.Wst(bed, 1.2f, 0.48f), Sz(1.0f, 0.95f, 0.48f), true, (m, r) => FurnitureBuilder.Dresser(m, r, FurnitureBuilder.Oak), "dresser");
            b.CeilingLight(bed, -2.5f, 2.2f);
            b.Litter(bed, 0.5f);

            // Bathroom.
            b.Furnish(bath, b.N(bath, 0.5f, 0.6f), Sz(0.45f, 0.8f, 0.6f), true, (m, r) => FurnitureBuilder.Toilet(m, r), "toilet");
            b.Container(bath, ContainerType.MedCabinet, "Bathroom Cabinet", b.E(bath, 2.6f, 0.45f), Sz(0.55f, 1.8f, 0.45f), (m, r) => FurnitureBuilder.WashBasin(m, r));
            b.CeilingLight(bath, 1.0f, 2.6f);
            b.Litter(bath, 0.4f);

            // Laundry / storeroom.
            for (int i = 0; i < 2; i++)
            {
                float z = 3.2f + i * 0.72f;
                b.Furnish(laundry, b.Wst(laundry, z, 0.65f), Sz(0.68f, 0.9f, 0.65f), true, (m, r) =>
                {
                    m.Box(new Vector3(0f, 0.43f, 0f), Sz(0.66f, 0.86f, 0.62f), DecorPalette.Gloss(new Color(0.84f, 0.84f, 0.82f)));
                    m.Cylinder(new Vector3(0f, 0.45f, 0.31f), 0.2f, 0.02f, 14, DecorPalette.Glass(new Color(0.2f, 0.22f, 0.24f)), Quaternion.Euler(90f, 0f, 0f));
                    m.Box(new Vector3(0f, 0.8f, 0.25f), Sz(0.6f, 0.08f, 0.12f), DecorPalette.Matte(new Color(0.7f, 0.7f, 0.68f)));
                }, "washer");
            }
            b.Container(laundry, ContainerType.Shelf, "Shelf", b.N(laundry, 4.0f, 0.45f), Sz(1.4f, 1.9f, 0.45f),
                (m, r) => FurnitureBuilder.MetalShelf(m, r, 1.4f, 0.45f, 1.9f, 4, 0, new Color(0.40f, 0.40f, 0.38f)));
            b.CeilingLight(laundry, 3.5f, 2.5f);
            b.Litter(laundry, 0.6f);
        }
        #endregion
    }
}
