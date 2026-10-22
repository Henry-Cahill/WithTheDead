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

        /// <summary>An office desk with a CRT, keyboard and (optionally) an articulated lamp; the lamp works from its own
        /// switch (<see cref="RaidBuilding.DeskLamp"/>) and starts on when <paramref name="lamp"/> is 2. Desk front faces local +Z.</summary>
        private static void OfficeDesk(RaidBuilding b, RaidMesh m, System.Random r, float w, Color wood, bool screenOn, int lamp, bool lampShadows)
        {
            FurnitureBuilder.Desk(m, r, w, 0.75f, wood, r.NextDouble() < 0.5);
            FurnitureBuilder.CrtMonitor(m, r, new Vector3(0.1f, 0.76f, -0.1f), R(r, -12f, 12f), screenOn);
            FurnitureBuilder.Keyboard(m, r, new Vector3(0.08f, 0.76f, 0.2f), R(r, -6f, 6f));
            DeskClutter(m, r, w);
            if (lamp > 0)
                b.DeskLamp(m, r, new Vector3(-w * 0.5f + 0.2f, 0.76f, -0.18f), R(r, 15f, 40f), r.NextDouble() < 0.5 ? GreenShade : BlackShade, lamp == 2, lampShadows);
        }

        /// <summary>An office desk as a piece of its own (w × 0.9 × 0.75 m, front toward the placement's +Z): what
        /// <see cref="OfficeDesk"/> draws — the CRT, the keyboard, the clutter and (optionally) the lamp — on a desk whose three
        /// pedestal drawers and pencil drawer each slide out on their own (<see cref="RaidBuilding.DrawerDesk"/>): the top drawer
        /// one of the raid's main containers when <paramref name="stocked"/>, else a few odds and ends at most, the rest lean.
        /// The computer switches on and off (<see cref="RaidBuilding.DeskMonitor"/>), starting as <paramref name="screenOn"/>
        /// says. Returns its body.</summary>
        internal static Transform WorkDesk(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, float w, Color wood, bool screenOn, int lamp,
            bool lampShadows, bool stocked, string name = "Desk")
        {
            var crt = new Vector3(0.1f, 0.76f, -0.1f);
            float crtYaw = 0f;
            Transform t = b.DrawerDesk(room, p, w, wood, stocked, name, (m, r) =>
            {
                crtYaw = R(r, -12f, 12f);
                FurnitureBuilder.CrtMonitorBody(m, r, crt, crtYaw);
                FurnitureBuilder.Keyboard(m, r, new Vector3(0.08f, 0.76f, 0.2f), R(r, -6f, 6f));
                DeskClutter(m, r, w);
                if (lamp > 0)
                    b.DeskLamp(m, r, new Vector3(-w * 0.5f + 0.2f, 0.76f, -0.18f), R(r, 15f, 40f), r.NextDouble() < 0.5 ? GreenShade : BlackShade, lamp == 2, lampShadows);
            });
            b.DeskMonitor(t, p, crt, crtYaw, screenOn);
            return t;
        }
        #endregion

        #region National Guard post (photo 1)
        /// <summary>The National Guard post / Army surplus: operations room with kitchenette, lockers and
        /// desks (photo 1), an armory, barracks, a storeroom and the CO's office. 18 × 14 m. Everything in it works: the
        /// kitchenette's sink and fridge, every locker, drawer, safe and crate, the field radio on the Guard's nets, the
        /// computer, the bunks to rest on, the chairs, and the CO's bookcase.</summary>
        public static RaidBuilding ArmyPost(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "NationalGuard", center, turns, false, 18f, 14f)
            {
                Kind = RaidBuildingKind.Army,
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
            // The kitchenette: a steel counter with a working sink and cupboards under it, and the cream fridge beside it.
            b.SteelCounter(ops, b.N(ops, -7.55f, 0.65f), 2.6f, 0.45f, ContainerType.Pantry, "Steel counter", (m, r) =>
            {
                FurnitureBuilder.BoxStack(m, r, new Vector3(-0.92f, 0.96f, -0.04f), 6f, 2);
                if (Chance(r, 0.7)) FurnitureBuilder.CardboardBox(m, r, new Vector3(1.05f, 0.96f, 0f), Sz(0.36f, 0.26f, 0.3f), -12f);
            });
            b.WorkFridge(ops, b.N(ops, -5.75f, 0.72f), FurnitureBuilder.Cream, ContainerType.Fridge, "Fridge", false);
            b.Furnish(ops, b.N(ops, -4.85f, 0.5f), Sz(0.6f, 1.1f, 0.5f), true, (m, r) => FurnitureBuilder.BoxStack(m, r, Vector3.zero, 0f, 3), "box stack");
            b.Poster(ops, 'N', -1.95f, 1.55f, 0.55f, 0.8f, 0);

            // The lockers, and the radio desk: the Guard's own nets on the field radio, a lamp left burning over it.
            b.Lockers(ops, b.Wst(ops, 0.2f, 0.5f), Sz(1.5f, 1.9f, 0.5f), 3, FurnitureBuilder.Maroon, ContainerType.Locker, "Locker");
            RaidBuilding.Place radioDesk = b.Wst(ops, -2.4f, 0.75f);
            Transform desk = b.DrawerDesk(ops, radioDesk, 1.6f, FurnitureBuilder.Oak, true, "Desk", (m, r) =>
            {
                b.DeskLamp(m, r, new Vector3(0.55f, 0.76f, -0.16f), 25f, GreenShade, true, true);
                DeskClutter(m, r, 1.2f);
            }, true);
            b.FieldRadio(desk, radioDesk, new Vector3(-0.3f, 0.76f, -0.08f), 8f);
            b.OfficeChairSeat(ops, new RaidBuilding.Place(-7.55f, -2.4f, 270f), new Color(0.20f, 0.22f, 0.18f));
            b.StackedCrate(ops, b.Wst(ops, -5.6f, 0.62f), Sz(0.9f, 1.05f, 0.62f), Sz(0.88f, 0.5f, 0.6f), Sz(0.78f, 0.42f, 0.54f), 0.02f, 4f,
                true, ContainerType.Crate, "Supply Crate", true);
            b.WoodChairSeat(ops, b.S(ops, -7.25f, 0.45f), FurnitureBuilder.Oak);
            b.WoodChairSeat(ops, b.S(ops, -6.6f, 0.45f), FurnitureBuilder.Oak);
            b.Furnish(ops, b.S(ops, -1.65f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.16f, 0.26f, 0.40f)), "barrel");
            b.Furnish(ops, b.S(ops, -0.98f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.26f, 0.30f, 0.18f)), "barrel");

            WorkDesk(b, ops, b.E(ops, -2.0f, 0.75f), 1.5f, FurnitureBuilder.Oak, true, 2, true, true);
            b.StoolSeat(ops, new RaidBuilding.Place(-0.3f, -2.0f, 90f), new Color(0.22f, 0.12f, 0.10f));
            b.FilingCabinet(ops, b.E(ops, -3.25f, 0.62f), 4, OliveDrab);
            b.RugAt(-4.6f, -2.4f, 2.6f, 1.8f, 0f, 0);
            b.Clock(ops, 'S', -5.4f, 2.2f);
            b.CeilingLight(ops, -6.2f, -2.4f, 90f);
            b.CeilingLight(ops, -1.9f, -2.4f, 90f);
            b.SpawnPoint(ops, -3.0f, -2.0f, 315f);   // facing the photo-1 corner: lockers, counter, fridge
            b.Litter(ops, 1.0f);

            // --- Armory ---
            b.GunSafe(armory, b.E(armory, -3.0f, 0.6f), ContainerType.GunLocker, "Gun Locker");
            b.Lockers(armory, b.E(armory, -2.05f, 0.5f), Sz(1.0f, 1.9f, 0.5f), 2, new Color(0.28f, 0.32f, 0.27f), ContainerType.Locker, "Locker");
            b.WeaponRack(armory, b.N(armory, 5.0f, 0.35f), Sz(2.0f, 1.6f, 0.35f), (m, r) => WeaponRack(m, r), "Weapon rack");
            b.SupplyCrate(armory, b.S(armory, 6.6f, 0.62f), Sz(0.9f, 0.57f, 0.62f), true, ContainerType.Crate, "Ammo Crate");
            b.StackedCrate(armory, b.S(armory, 7.9f, 0.62f), Sz(0.9f, 1.0f, 0.62f), Sz(0.88f, 0.5f, 0.6f), Sz(0.8f, 0.45f, 0.55f), 0f, -5f,
                true, ContainerType.Crate, "Crate", false);
            StorageOptions cans = RaidBuilding.SpareStock(ContainerType.Crate, "ammo cans");
            cans.ContainerName = "Ammo cans";
            cans.LookLabel = "Check the ammo cans";
            b.WorkTable(armory, new RaidBuilding.Place(4.5f, -4.0f, 0f), Sz(1.6f, 0.9f, 0.8f), (m, r) =>
            {
                FurnitureBuilder.Table(m, r, 1.6f, 0.8f, OliveDrab);
                for (int i = 0; i < 3; i++) FurnitureBuilder.AmmoCan(m, r, new Vector3(-0.5f + i * 0.45f, 0.755f, R(r, -0.15f, 0.15f)), R(r, -20f, 20f));
            }, "Table", cans);
            b.CeilingLight(armory, 5f, -4f);
            b.Litter(armory, 0.6f);
            b.Clutter(armory, 1, true);

            // --- Barracks ---
            b.ArmyBunk(barracks, b.N(barracks, 2.6f, 2.0f));
            b.ArmyBunk(barracks, b.N(barracks, 7.4f, 2.0f));
            b.Footlocker(barracks, new RaidBuilding.Place(2.6f, 4.6f, 180f));
            b.Footlocker(barracks, new RaidBuilding.Place(7.4f, 4.6f, 180f));
            b.Lockers(barracks, b.Wst(barracks, 3.0f, 0.5f), Sz(1.0f, 1.9f, 0.5f), 2, new Color(0.30f, 0.33f, 0.24f), ContainerType.Locker, "Wall Locker");
            b.WorkTable(barracks, new RaidBuilding.Place(5.0f, 1.6f, 0f), Sz(1.2f, 0.8f, 0.8f), (m, r) => FurnitureBuilder.Table(m, r, 1.2f, 0.8f, FurnitureBuilder.Oak), "Table", null, true);
            b.WoodChairSeat(barracks, new RaidBuilding.Place(5.0f, 0.75f, 0f), FurnitureBuilder.Oak);
            b.WoodChairSeat(barracks, new RaidBuilding.Place(5.1f, 2.45f, 190f), FurnitureBuilder.Oak);
            b.Poster(barracks, 'W', 5.2f, 1.5f, 0.5f, 0.72f, 0);
            b.CeilingLight(barracks, 5f, 3f);
            b.Litter(barracks, 0.6f);
            b.Clutter(barracks, 2, true);

            // --- Storeroom ---
            Color shelfGrey = new Color(0.34f, 0.36f, 0.33f);
            b.Shelving(storage, b.Wst(storage, 2.9f, 0.5f), Sz(1.4f, 1.9f, 0.5f), 4, 3, shelfGrey, ContainerType.Shelf, "Supply Shelf", true);
            b.Shelving(storage, b.Wst(storage, 5.9f, 0.5f), Sz(1.4f, 1.9f, 0.5f), 4, 3, shelfGrey, "Shelf", RaidBuilding.SpareStock(ContainerType.Shelf, "shelf"));
            b.SupplyCrate(storage, b.N(storage, -3.0f, 0.62f), Sz(0.9f, 0.6f, 0.62f), true, ContainerType.Crate, "Crate", false);
            b.Furnish(storage, b.E(storage, 4.2f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.45f, 0.24f, 0.10f)), "barrel");
            b.Furnish(storage, b.E(storage, 4.86f, 0.6f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.24f, 0.28f, 0.18f)), "barrel");
            b.Furnish(storage, new RaidBuilding.Place(-5.5f, 4.0f, 0f), Sz(1.2f, 1.0f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, true), "pallet");
            b.CeilingLight(storage, -5.5f, 4.5f);
            b.Litter(storage, 0.5f);

            // --- CO office ---
            b.DrawerDesk(office, b.N(office, -0.5f, 0.75f), 1.5f, FurnitureBuilder.Walnut, true, "Desk", (m, r) =>
            {
                b.DeskLamp(m, r, new Vector3(0.5f, 0.76f, -0.18f), -25f, GreenShade, Chance(r, 0.6), false);
                DeskClutter(m, r, 1.2f);
            }, false);
            b.OfficeChairSeat(office, new RaidBuilding.Place(-0.5f, 5.7f, 0f), new Color(0.16f, 0.10f, 0.08f));
            b.FilingCabinet(office, b.E(office, 4.0f, 0.62f), 4, OliveDrab);
            b.ReadingShelf(office, b.Wst(office, 4.5f, 0.32f), FurnitureBuilder.Walnut, 1, CivicReading.Shelf.Army);
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
var boltMat = DecorPalette.Metal(new Color(0.25f, 0.25f, 0.25f));
            m.Box(new Vector3(-0.95f, 0.8f, -0.1f), Sz(0.06f, 1.6f, 0.1f), wood);
// backboard – thin plywood behind the rack (adds depth and a surface for grime)
         var boardMat = DecorPalette.Matte(new Color(0.34f, 0.24f, 0.14f));
         m.Box(new Vector3(0f, 0.8f, -0.15f), Sz(1.92f, 1.62f, 0.02f), boardMat);

         // thin snow layer on the upper backboard (winter detail)
         if (Chance(r, 0.5f))
         {
             float snowW = R(r, 0.4f, 1.0f); // random width (metres)
             float snowD = R(r, 0.08f, 0.18f); // random depth (metres)
             float snowH = 0.005f; // thin height (metres)
             var snowMat = DecorPalette.Matte(new Color(0.90f, 0.90f, 0.95f));
             // position snow slightly forward of backboard surface
             m.Box(new Vector3(0f, 0.8f, -0.15f - snowD/2f), Sz(snowW, snowH, snowD), snowMat);
         }
        // front strap – reinforces the rack, anchored to the posts
        var strapMat = DecorPalette.Metal(new Color(0.28f, 0.28f, 0.26f));
        m.Box(new Vector3(0f, 0.95f, -0.045f), Sz(1.84f, 0.03f, 0.01f), strapMat);
        // latch hinge pin
        m.Cylinder(new Vector3(-0.85f, 0.71f, -0.045f), 0.006f, 0.024f, 8, boltMat);
        // latch body (metal plate)
        m.Box(new Vector3(-0.84f, 0.71f, -0.06f), Sz(0.04f, 0.02f, 0.004f), boltMat);
        // latch hook (a thin cylinder protruding)
        m.Cylinder(new Vector3(-0.80f, 0.71f, -0.06f), 0.003f, 0.015f, 8, boltMat);
        // paint chip – random brownish patch on front face
        if (Chance(r, 0.35f))
        {
            float chipW = R(r, 0.04f, 0.09f);
            float chipH = 0.001f;
            float chipD = 0.015f;
            var chipMat = DecorPalette.Matte(new Color(0.28f, 0.20f, 0.12f));
            m.Box(
                new Vector3(R(r, -0.9f, 0.9f), 0.92f, R(r, -0.07f, 0.07f)),
                Sz(chipW, chipH, chipD),
                chipMat);
        }
                Vector3 chipPosLeft = new Vector3(-0.95f + (float)r.NextDouble()*0.02f - 0.01f, R(r, 0.4f, 1.2f), R(r, -0.05f, 0.05f));
                m.Box(chipPosLeft, Sz(0.02f, 0.01f, 0.005f), wood);
            m.Box(new Vector3(0.95f, 0.8f, -0.1f), Sz(0.06f, 1.6f, 0.1f), wood);
                Vector3 chipPosRight = new Vector3(0.95f + (float)r.NextDouble()*0.02f - 0.01f, R(r, 0.4f, 1.2f), R(r, -0.05f, 0.05f));
                m.Box(chipPosRight, Sz(0.02f, 0.01f, 0.005f), wood);
            m.Box(new Vector3(0f, 0.25f, 0f), Sz(1.9f, 0.08f, 0.3f), wood);
            // thin weathered grime patches on the lower shelf
            if (Chance(r, 0.4f))
            {
                int patches = r.Next(1, 4);
                for (int i = 0; i < patches; i++)
                {
                    float patchWidth = R(r, 0.2f, 0.5f);
                    float patchDepth = 0.02f;
                    float patchHeight = 0.001f;
                    m.Box(
                        new Vector3(R(r, -0.9f, 0.9f), 0.27f, R(r, -0.13f, 0.13f)),
                        Sz(patchWidth, patchHeight, patchDepth),
                        DecorPalette.Matte(new Color(0.20f, 0.18f, 0.14f)));
                }
            }
            // mounting bolts (reuse existing boltMat)

            float boltRadius = 0.015f; float boltHeight = 0.02f;
            Vector3[] boltPos = { new Vector3(-0.95f, 0.41f, 0.05f), new Vector3(-0.95f, 0.41f, -0.05f), new Vector3(0.95f, 0.41f, 0.05f), new Vector3(0.95f, 0.41f, -0.05f) };
            foreach (var p in boltPos) m.Cylinder(p, boltRadius, boltHeight, 8, boltMat);
            // period‑correct mounting nails on the bottom of the wooden frame
            float nailRadius = 0.006f; float nailHeight = 0.012f;
            Vector3[] nailPos = { new Vector3(-0.95f, 0.02f, -0.1f), new Vector3(-0.95f, 0.02f, 0.1f), new Vector3(0.95f, 0.02f, -0.1f), new Vector3(0.95f, 0.02f, 0.1f) };
            foreach (var p in nailPos) m.Cylinder(p, nailRadius, nailHeight, 8, boltMat);
            m.Box(new Vector3(0f, 1.3f, -0.08f), Sz(1.9f, 0.06f, 0.12f), wood);
            m.Box(new Vector3(0f, 1.31f, -0.08f), Sz(1.9f, 0.002f, 0.3f), DecorPalette.Matte(new Color(0.90f, 0.90f, 0.95f)));
            var bracketMat = DecorPalette.Metal(new Color(0.30f, 0.30f, 0.28f));
            m.Box(new Vector3(-0.95f, 1.33f, -0.08f), Sz(0.02f, 0.10f, 0.02f), bracketMat);
            m.Box(new Vector3(-0.95f, 1.33f, 0.08f), Sz(0.02f, 0.10f, 0.02f), bracketMat);
            m.Box(new Vector3( 0.95f, 1.33f, -0.08f), Sz(0.02f, 0.10f, 0.02f), bracketMat);
            m.Box(new Vector3( 0.95f, 1.33f, 0.08f), Sz(0.02f, 0.10f, 0.02f), bracketMat);
            // rivets on the vertical posts
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -0.95f : 0.95f;
                for (int i = 0; i < 3; i++)
                {
                    float y = 0.85f + i * 0.08f;
                    m.Cylinder(new Vector3(x, y, 0f), 0.005f, 0.01f, 8, boltMat);
                }
            }
            var plateMat = DecorPalette.Metal(new Color(0.25f, 0.25f, 0.25f));
            m.Box(new Vector3(-0.95f, 0.8f, 0.06f), Sz(0.02f, 0.1f, 0.001f), plateMat);
            m.Box(new Vector3( 0.95f, 0.8f, 0.06f), Sz(0.02f, 0.1f, 0.001f), plateMat);
                    // front label plate (rust‑tinted metal)
        var labelMat = DecorPalette.Metal(new Color(0.30f, 0.35f, 0.25f));
        m.Box(new Vector3(0f, 0.77f, 0.07f), Sz(0.25f, 0.015f, 0.002f), labelMat);
        // dust puff – flat gray cylinder on the top shelf
        var dustMat = DecorPalette.Matte(new Color(0.45f, 0.44f, 0.40f));
        m.Cylinder(new Vector3(0f, 1.31f, -0.02f), 0.1f, 0.002f, 8, dustMat);
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
