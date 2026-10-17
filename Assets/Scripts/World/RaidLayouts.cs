using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The raid map's authored buildings. Each method raises one building in its local frame (front on
    /// local -Z) — rooms, doorways, windows — and furnishes it room by room so the interiors read like
    /// the reference photos: the National Guard post's operations room (photo 1: steel counter and
    /// sink, cream fridge, maroon lockers, wooden desk with lamp and field radio, crates, boxes, the
    /// "U.S. ARMY" poster) and the county office's open-plan floor (photo 3: desks, CRT monitors,
    /// articulated lamps, stools, filing cabinets, barrels, a rug, ceiling tiles).
    /// </summary>
    internal static partial class RaidLayouts
    {
        #region Wall schemes
        public static readonly WallDesign ArmyMaroon = new WallDesign { Key = "armymaroon", Wainscot = new Color(0.36f, 0.08f, 0.07f), Paper = new Color(0.80f, 0.76f, 0.62f), Trim = new Color(0.24f, 0.15f, 0.09f), Lower = WainscotStyle.Beadboard, Upper = PaperStyle.Stripes };
        public static readonly WallDesign TealBeige = new WallDesign { Key = "tealbeige", Wainscot = new Color(0.10f, 0.26f, 0.26f), Paper = new Color(0.78f, 0.72f, 0.58f), Trim = new Color(0.20f, 0.14f, 0.09f), Lower = WainscotStyle.Beadboard, Upper = PaperStyle.Damask };
        public static readonly WallDesign BrownGreen = new WallDesign { Key = "browngreen", Wainscot = new Color(0.33f, 0.21f, 0.12f), Paper = new Color(0.55f, 0.62f, 0.48f), Trim = new Color(0.22f, 0.14f, 0.08f), Lower = WainscotStyle.Panels, Upper = PaperStyle.Stripes };
        public static readonly WallDesign NavyGrey = new WallDesign { Key = "navygrey", Wainscot = new Color(0.13f, 0.16f, 0.27f), Paper = new Color(0.72f, 0.74f, 0.74f), Trim = new Color(0.20f, 0.20f, 0.22f), Lower = WainscotStyle.Panels, Upper = PaperStyle.Pinstripe };
        public static readonly WallDesign OfficeBlue = new WallDesign { Key = "officeblue", Wainscot = new Color(0.30f, 0.34f, 0.40f), Paper = new Color(0.80f, 0.76f, 0.66f), Trim = new Color(0.18f, 0.18f, 0.20f), Lower = WainscotStyle.Panels, Upper = PaperStyle.Pinstripe };
        public static readonly WallDesign ClinicGreen = new WallDesign { Key = "clinic", Wainscot = new Color(0.50f, 0.62f, 0.55f), Paper = new Color(0.84f, 0.84f, 0.79f), Trim = new Color(0.35f, 0.38f, 0.36f), Lower = WainscotStyle.Paint, Upper = PaperStyle.Plain };
        public static readonly WallDesign GarageGrey = new WallDesign { Key = "garage", Wainscot = new Color(0.30f, 0.31f, 0.32f), Paper = new Color(0.58f, 0.58f, 0.55f), Trim = new Color(0.20f, 0.20f, 0.20f), Lower = WainscotStyle.Paint, Upper = PaperStyle.Plain, RailHeight = 1.2f };
        public static readonly WallDesign Canvas = new WallDesign { Key = "canvas", Wainscot = new Color(0.30f, 0.32f, 0.20f), Paper = new Color(0.34f, 0.36f, 0.23f), Trim = new Color(0.24f, 0.25f, 0.16f), Lower = WainscotStyle.Paint, Upper = PaperStyle.Plain, RailHeight = 5f };
        public static readonly WallDesign Mustard = new WallDesign { Key = "mustard", Wainscot = new Color(0.40f, 0.26f, 0.14f), Paper = new Color(0.74f, 0.62f, 0.36f), Trim = new Color(0.28f, 0.18f, 0.10f), Lower = WainscotStyle.Beadboard, Upper = PaperStyle.Floral };
        public static readonly WallDesign Rose = new WallDesign { Key = "rose", Wainscot = new Color(0.30f, 0.10f, 0.13f), Paper = new Color(0.76f, 0.60f, 0.58f), Trim = new Color(0.22f, 0.12f, 0.10f), Lower = WainscotStyle.Panels, Upper = PaperStyle.Damask };
        public static readonly WallDesign KitchenMint = new WallDesign { Key = "kitchenmint", Wainscot = new Color(0.80f, 0.80f, 0.76f), Paper = new Color(0.62f, 0.74f, 0.66f), Trim = new Color(0.30f, 0.32f, 0.30f), Lower = WainscotStyle.Paint, Upper = PaperStyle.Pinstripe, RailHeight = 1.2f };
        public static readonly WallDesign StoreRed = new WallDesign { Key = "storered", Wainscot = new Color(0.52f, 0.13f, 0.11f), Paper = new Color(0.82f, 0.80f, 0.72f), Trim = new Color(0.25f, 0.20f, 0.18f), Lower = WainscotStyle.Paint, Upper = PaperStyle.Plain };

        private static readonly WallDesign[] HomeLiving = { Mustard, TealBeige, Rose, BrownGreen };
        private static readonly WallDesign[] HomeBed = { Rose, BrownGreen, TealBeige, NavyGrey };
        #endregion

        #region Helpers
        private static Vector3 Sz(float w, float h, float d) => new Vector3(w, h, d);
        private static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
        private static bool Chance(System.Random r, double p) => r.NextDouble() < p;

        private static readonly Color OliveDrab = new Color(0.30f, 0.33f, 0.22f);
        private static readonly Color Putty = new Color(0.62f, 0.60f, 0.52f);
        private static readonly Color GreenShade = new Color(0.14f, 0.28f, 0.20f);
        private static readonly Color BlackShade = new Color(0.10f, 0.10f, 0.11f);
        private static readonly Color CreamShade = new Color(0.74f, 0.70f, 0.56f);

        /// <summary>Loose papers, a mug and pens on a desk top (local desk frame, top at 0.76).</summary>
        private static void DeskClutter(RaidMesh m, System.Random r, float w)
        {
            int n = r.Next(1, 4);
            for (int i = 0; i < n; i++)
                m.FlatUV(new Vector3(R(r, -w * 0.35f, w * 0.35f), 0.762f + i * 0.002f, R(r, -0.15f, 0.2f)), 0.21f, 0.28f, R(r, -40f, 40f), DecorMaterials.Paper(r.Next(3)), new Rect(0f, 0f, 1f, 1f));
            if (Chance(r, 0.6)) m.Cylinder(new Vector3(R(r, -w * 0.4f, w * 0.4f), 0.76f, R(r, 0f, 0.2f)), 0.04f, 0.1f, 8, DecorPalette.Gloss(new Color(R(r, 0.5f, 0.9f), R(r, 0.4f, 0.8f), R(r, 0.3f, 0.7f))));
            if (Chance(r, 0.4)) m.Box(new Vector3(R(r, -w * 0.3f, w * 0.3f), 0.79f, -0.1f), new Vector3(0.22f, 0.06f, 0.3f), Quaternion.Euler(0f, R(r, -20f, 20f), 0f), DecorPalette.Matte(new Color(0.62f, 0.52f, 0.30f)));
        }

        /// <summary>An office desk with a CRT, keyboard and (optionally) an articulated lamp; the lamp's
        /// light is spawned when lit. Desk front faces local +Z.</summary>
        private static void OfficeDesk(RaidBuilding b, RaidMesh m, System.Random r, float w, Color wood, bool screenOn, int lamp, bool lampShadows)
        {
            FurnitureBuilder.Desk(m, r, w, 0.75f, wood, r.NextDouble() < 0.5);
            FurnitureBuilder.CrtMonitor(m, r, new Vector3(0.1f, 0.76f, -0.1f), R(r, -12f, 12f), screenOn);
            FurnitureBuilder.Keyboard(m, r, new Vector3(0.08f, 0.76f, 0.2f), R(r, -6f, 6f));
            DeskClutter(m, r, w);
            if (lamp > 0)
            {
                bool lit = lamp == 2;
                Vector3 bulb = FurnitureBuilder.DeskLamp(m, r, new Vector3(-w * 0.5f + 0.2f, 0.76f, -0.18f), R(r, 15f, 40f), r.NextDouble() < 0.5 ? GreenShade : BlackShade, lit);
                if (lit) b.LampLight(RaidBuilding.WorldOf(m, bulb), lampShadows);
            }
        }
        #endregion

        #region National Guard post (photo 1)
        /// <summary>The National Guard post / Army surplus: operations room with kitchenette, lockers and
        /// desks (photo 1), an armory, barracks, a storeroom and the CO's office. 18 × 14 m.</summary>
        public static RaidBuilding ArmyPost(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "NationalGuard", center, turns, false, 18f, 14f)
            {
                Exterior = ExteriorStyle.Brick,
                ExteriorColor = new Color(0.50f, 0.31f, 0.22f),
                DoorWood = new Color(0.40f, 0.26f, 0.14f),
                Casing = new Color(0.34f, 0.22f, 0.12f),
            };
            System.Random rng = b.Rng;
            RaidRoom ops = b.Room("Operations", -9f, -7f, 1f, 2f, ArmyMaroon, FloorKind.LinoGrey);
            ops.Lit = true;
            b.Overgrown.Add(ops);
            RaidRoom armory = b.Room("Armory", 1f, -7f, 9f, -1f, NavyGrey, FloorKind.Concrete);
            armory.Lit = Chance(rng, 0.4);
            armory.Light = LightStyle.ShopLamp;
            RaidRoom barracks = b.Room("Barracks", 1f, -1f, 9f, 7f, BrownGreen, FloorKind.Wood);
            barracks.Lit = Chance(rng, 0.6);
            barracks.Flicker = Chance(rng, 0.5);
            RaidRoom storage = b.Room("Storeroom", -9f, 2f, -2f, 7f, GarageGrey, FloorKind.Concrete);
            storage.Light = LightStyle.Bulb;
            storage.Lit = Chance(rng, 0.4);
            RaidRoom office = b.Room("CO Office", -2f, 2f, 1f, 7f, ArmyMaroon, FloorKind.WoodDark);
            b.Overgrown.Add(office);

            b.Door(-3.5f, -7f, 1.5f);
            b.Door(1f, -4.6f, 1.4f);
            b.Door(1f, 0.5f, 1.4f);
            b.Door(-3.4f, 2f, 1.4f);
            b.Door(-0.5f, 2f, 1.4f);
            b.Door(9f, 2.6f, 1.4f);
            b.Door(-6f, 7f, 1.8f);
            b.Window(-7.2f, -7f); b.Window(-0.6f, -7f); b.Window(4.5f, -7f);
            b.Window(-9f, -2.4f); b.Window(-9f, 4.5f);
            b.Window(-0.5f, 7f, 1.0f); b.Window(5f, 7f);
            b.Window(9f, -5.4f); b.Window(9f, 0.4f);
            b.BuildShell();

            // --- Operations room: the photo-1 room ---
            b.Furnish(ops, b.N(ops, -7.55f, 0.65f), Sz(2.6f, 1.1f, 0.65f), true, (m, r) =>
            {
                FurnitureBuilder.SteelCounter(m, r, 2.6f, true);
                FurnitureBuilder.BoxStack(m, r, new Vector3(-0.92f, 0.96f, -0.04f), 6f, 2);
                if (Chance(r, 0.7)) FurnitureBuilder.CardboardBox(m, r, new Vector3(0.95f, 0.96f, 0f), Sz(0.36f, 0.26f, 0.3f), -12f);
            }, "steel counter");
            b.Container(ops, ContainerType.Fridge, "Fridge", b.N(ops, -5.75f, 0.72f), Sz(0.76f, 1.75f, 0.72f),
                (m, r) => FurnitureBuilder.Fridge(m, r, FurnitureBuilder.Cream));
            b.Furnish(ops, b.N(ops, -4.85f, 0.5f), Sz(0.6f, 1.1f, 0.5f), true, (m, r) => FurnitureBuilder.BoxStack(m, r, Vector3.zero, 0f, 3), "box stack");
            b.Poster(ops, 'N', -1.95f, 1.55f, 0.55f, 0.8f, 0);

            b.Container(ops, ContainerType.Locker, "Locker", b.Wst(ops, 0.2f, 0.5f), Sz(1.5f, 1.9f, 0.5f),
                (m, r) => FurnitureBuilder.LockerBank(m, r, 3, FurnitureBuilder.Maroon));
            b.Container(ops, ContainerType.Desk, "Desk", b.Wst(ops, -2.4f, 0.75f), Sz(1.6f, 0.9f, 0.75f), (m, r) =>
            {
                FurnitureBuilder.Desk(m, r, 1.6f, 0.75f, FurnitureBuilder.Oak, true);
                FurnitureBuilder.FieldRadio(m, r, new Vector3(-0.3f, 0.76f, -0.08f), 8f);
                Vector3 bulb = FurnitureBuilder.DeskLamp(m, r, new Vector3(0.55f, 0.76f, -0.16f), 25f, GreenShade, true);
                b.LampLight(RaidBuilding.WorldOf(m, bulb), true);
                DeskClutter(m, r, 1.2f);
            });
            b.Furnish(ops, new RaidBuilding.Place(-7.55f, -2.4f, 270f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, new Color(0.20f, 0.22f, 0.18f)), "chair");
            b.Container(ops, ContainerType.Crate, "Supply Crate", b.Wst(ops, -5.6f, 0.62f), Sz(0.9f, 1.05f, 0.62f), (m, r) =>
            {
                FurnitureBuilder.Crate(m, r, Vector3.zero, Sz(0.88f, 0.5f, 0.6f), 0f, true);
                FurnitureBuilder.Crate(m, r, new Vector3(0.02f, 0.5f, 0f), Sz(0.78f, 0.42f, 0.54f), 4f, true);
                FurnitureBuilder.AmmoCan(m, r, new Vector3(-0.1f, 0.92f, 0f), 20f);
            });
            b.Furnish(ops, b.S(ops, -7.25f, 0.45f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Oak), "chair");
            b.Furnish(ops, b.S(ops, -6.6f, 0.45f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Oak), "chair");
            b.Furnish(ops, b.S(ops, -1.65f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.16f, 0.26f, 0.40f)), "barrel");
            b.Furnish(ops, b.S(ops, -0.98f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.26f, 0.30f, 0.18f)), "barrel");

            b.Container(ops, ContainerType.Desk, "Desk", b.E(ops, -2.0f, 0.75f), Sz(1.5f, 0.9f, 0.75f),
                (m, r) => OfficeDesk(b, m, r, 1.5f, FurnitureBuilder.Oak, true, 2, true));
            b.Furnish(ops, new RaidBuilding.Place(-0.3f, -2.0f, 0f), Sz(0.45f, 0.7f, 0.45f), false, (m, r) => FurnitureBuilder.Stool(m, r, new Color(0.22f, 0.12f, 0.10f)), "stool");
            b.Furnish(ops, b.E(ops, -3.25f, 0.62f), Sz(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, OliveDrab), "filing cabinet");
            b.RugAt(-4.6f, -2.4f, 2.6f, 1.8f, 0f, 0);
            b.Clock(ops, 'S', -5.4f, 2.2f);
            b.CeilingLight(ops, -6.2f, -2.4f, 90f);
            b.CeilingLight(ops, -1.9f, -2.4f, 90f);
            b.SpawnPoint(ops, -3.0f, -2.0f, 315f);   // facing the photo-1 corner: lockers, counter, fridge
            b.Litter(ops, 1.0f);

            // --- Armory ---
            b.Container(armory, ContainerType.GunLocker, "Gun Locker", b.E(armory, -3.0f, 0.6f), Sz(0.8f, 1.9f, 0.6f), (m, r) => FurnitureBuilder.GunSafe(m, r));
            b.Container(armory, ContainerType.Locker, "Locker", b.E(armory, -2.05f, 0.5f), Sz(1.0f, 1.9f, 0.5f),
                (m, r) => FurnitureBuilder.LockerBank(m, r, 2, new Color(0.28f, 0.32f, 0.27f)));
            b.Furnish(armory, b.N(armory, 5.0f, 0.35f), Sz(2.0f, 1.6f, 0.35f), true, (m, r) => WeaponRack(m, r), "weapon rack");
            b.Container(armory, ContainerType.Crate, "Ammo Crate", b.S(armory, 6.6f, 0.62f), Sz(0.9f, 0.75f, 0.62f), (m, r) =>
            {
                FurnitureBuilder.Crate(m, r, Vector3.zero, Sz(0.88f, 0.55f, 0.6f), 0f, true);
                FurnitureBuilder.AmmoCan(m, r, new Vector3(-0.2f, 0.55f, 0f), 5f);
                FurnitureBuilder.AmmoCan(m, r, new Vector3(0.2f, 0.55f, 0.02f), -8f);
            });
            b.Furnish(armory, b.S(armory, 7.9f, 0.62f), Sz(0.9f, 1.0f, 0.62f), true, (m, r) =>
            {
                FurnitureBuilder.Crate(m, r, Vector3.zero, Sz(0.88f, 0.5f, 0.6f), 0f, true);
                FurnitureBuilder.Crate(m, r, new Vector3(0f, 0.5f, 0f), Sz(0.8f, 0.45f, 0.55f), -5f, true);
            }, "crates");
            b.Furnish(armory, new RaidBuilding.Place(4.5f, -4.0f, 0f), Sz(1.6f, 0.9f, 0.8f), true, (m, r) =>
            {
                FurnitureBuilder.Table(m, r, 1.6f, 0.8f, OliveDrab);
                for (int i = 0; i < 3; i++) FurnitureBuilder.AmmoCan(m, r, new Vector3(-0.5f + i * 0.45f, 0.755f, R(r, -0.15f, 0.15f)), R(r, -20f, 20f));
            }, "table");
            b.CeilingLight(armory, 5f, -4f);
            b.Litter(armory, 0.6f);
            b.Clutter(armory, 1, true);

            // --- Barracks ---
            b.Furnish(barracks, b.N(barracks, 2.6f, 2.0f), Sz(0.95f, 1.75f, 2.0f), true, (m, r) => FurnitureBuilder.BunkBed(m, r), "bunk");
            b.Furnish(barracks, b.N(barracks, 7.4f, 2.0f), Sz(0.95f, 1.75f, 2.0f), true, (m, r) => FurnitureBuilder.BunkBed(m, r), "bunk");
            b.Furnish(barracks, new RaidBuilding.Place(2.6f, 4.6f, 180f), Sz(0.8f, 0.42f, 0.42f), true,
                (m, r) => FurnitureBuilder.Crate(m, r, Vector3.zero, Sz(0.8f, 0.4f, 0.42f), 0f, true), "footlocker");
            b.Furnish(barracks, new RaidBuilding.Place(7.4f, 4.6f, 180f), Sz(0.8f, 0.42f, 0.42f), true,
                (m, r) => FurnitureBuilder.Crate(m, r, Vector3.zero, Sz(0.8f, 0.4f, 0.42f), 0f, true), "footlocker");
            b.Container(barracks, ContainerType.Locker, "Wall Locker", b.Wst(barracks, 3.0f, 0.5f), Sz(1.0f, 1.9f, 0.5f),
                (m, r) => FurnitureBuilder.LockerBank(m, r, 2, new Color(0.30f, 0.33f, 0.24f)));
            b.Furnish(barracks, new RaidBuilding.Place(5.0f, 1.6f, 0f), Sz(1.2f, 0.8f, 0.8f), true, (m, r) => FurnitureBuilder.Table(m, r, 1.2f, 0.8f, FurnitureBuilder.Oak), "table");
            b.Furnish(barracks, new RaidBuilding.Place(5.0f, 0.75f, 0f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Oak), "chair");
            b.Furnish(barracks, new RaidBuilding.Place(5.1f, 2.45f, 190f), Sz(0.45f, 0.9f, 0.45f), false, (m, r) => FurnitureBuilder.WoodChair(m, r, FurnitureBuilder.Oak), "chair");
            b.Poster(barracks, 'W', 5.2f, 1.5f, 0.5f, 0.72f, 0);
            b.CeilingLight(barracks, 5f, 3f);
            b.Litter(barracks, 0.6f);
            b.Clutter(barracks, 2, true);

            // --- Storeroom ---
            b.Container(storage, ContainerType.Shelf, "Supply Shelf", b.Wst(storage, 2.9f, 0.5f), Sz(1.4f, 1.9f, 0.5f),
                (m, r) => FurnitureBuilder.MetalShelf(m, r, 1.4f, 0.5f, 1.9f, 4, 3, new Color(0.34f, 0.36f, 0.33f)));
            b.Furnish(storage, b.Wst(storage, 5.9f, 0.5f), Sz(1.4f, 1.9f, 0.5f), true,
                (m, r) => FurnitureBuilder.MetalShelf(m, r, 1.4f, 0.5f, 1.9f, 4, 3, new Color(0.34f, 0.36f, 0.33f)), "shelf");
            b.Furnish(storage, b.N(storage, -3.0f, 0.62f), Sz(0.9f, 0.6f, 0.62f), true,
                (m, r) => FurnitureBuilder.Crate(m, r, Vector3.zero, Sz(0.88f, 0.58f, 0.6f), 0f, true), "crate");
            b.Furnish(storage, b.E(storage, 4.2f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.45f, 0.24f, 0.10f)), "barrel");
            b.Furnish(storage, b.E(storage, 4.86f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.24f, 0.28f, 0.18f)), "barrel");
            b.Furnish(storage, new RaidBuilding.Place(-5.5f, 4.0f, 0f), Sz(1.2f, 1.0f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, true), "pallet");
            b.CeilingLight(storage, -5.5f, 4.5f);
            b.Litter(storage, 0.5f);

            // --- CO office ---
            b.Container(office, ContainerType.Desk, "Desk", b.N(office, -0.5f, 0.75f), Sz(1.5f, 0.9f, 0.75f), (m, r) =>
            {
                FurnitureBuilder.Desk(m, r, 1.5f, 0.75f, FurnitureBuilder.Walnut, false);
                bool lit = Chance(r, 0.6);
                Vector3 bulb = FurnitureBuilder.DeskLamp(m, r, new Vector3(0.5f, 0.76f, -0.18f), -25f, GreenShade, lit);
                if (lit) b.LampLight(RaidBuilding.WorldOf(m, bulb), false);
                DeskClutter(m, r, 1.2f);
            });
            b.Furnish(office, new RaidBuilding.Place(-0.5f, 5.7f, 0f), Sz(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, new Color(0.16f, 0.10f, 0.08f)), "chair");
            b.Furnish(office, b.E(office, 4.0f, 0.62f), Sz(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, OliveDrab), "filing cabinet");
            b.Furnish(office, b.Wst(office, 4.5f, 0.32f), Sz(0.9f, 1.8f, 0.32f), true, (m, r) => FurnitureBuilder.Bookshelf(m, r, FurnitureBuilder.Walnut), "bookshelf");
            b.Poster(office, 'W', 3.0f, 1.6f, 0.5f, 0.72f, 0);
            b.Litter(office, 0.8f);

            b.DoorSign(-3.5f, -7f, "NATIONAL GUARD", new Color(0.20f, 0.26f, 0.16f), new Color(0.86f, 0.84f, 0.74f));
            b.Finish();
            return b;
        }

        /// <summary>A wall rack of rifles (photo-1 kit), front facing +Z.</summary>
        private static void WeaponRack(RaidMesh m, System.Random r)
        {
            Surf wood = DecorPalette.Matte(new Color(0.32f, 0.22f, 0.12f));
            m.Box(new Vector3(-0.95f, 0.8f, -0.1f), Sz(0.06f, 1.6f, 0.1f), wood);
            m.Box(new Vector3(0.95f, 0.8f, -0.1f), Sz(0.06f, 1.6f, 0.1f), wood);
            m.Box(new Vector3(0f, 0.25f, 0f), Sz(1.9f, 0.08f, 0.3f), wood);
            m.Box(new Vector3(0f, 1.3f, -0.08f), Sz(1.9f, 0.06f, 0.12f), wood);
            int guns = r.Next(2, 6);
            for (int i = 0; i < guns; i++)
            {
                float x = -0.75f + i * 0.37f;
                Quaternion lean = Quaternion.Euler(-8f, 0f, R(r, -3f, 3f));
                m.Box(new Vector3(x, 0.38f, 0.02f), Sz(0.07f, 0.22f, 0.12f), lean, DecorPalette.Matte(new Color(0.30f, 0.20f, 0.11f)));
                m.Box(new Vector3(x, 0.95f, -0.04f), Sz(0.05f, 0.95f, 0.06f), lean, DecorPalette.Metal(new Color(0.12f, 0.12f, 0.13f)));
                m.Box(new Vector3(x, 0.78f, 0.0f), Sz(0.04f, 0.18f, 0.1f), lean, DecorPalette.Metal(new Color(0.12f, 0.12f, 0.13f)));
            }
        }
        #endregion
    }
}
