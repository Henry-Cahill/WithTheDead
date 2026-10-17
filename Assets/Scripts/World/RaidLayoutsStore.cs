using UnityEngine;

namespace CloneZ
{
    internal static partial class RaidLayouts
    {
        #region Gas station
        /// <summary>The gas station's food mart: checkout counter, gondolas, glowing drinks coolers, a
        /// stock room and a restroom. 14 × 10 m; the pumps and canopy are added outside by the map.</summary>
        public static RaidBuilding GasStation(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "FoodMart", center, turns, false, 14f, 10f)
            {
                Exterior = ExteriorStyle.Brick,
                ExteriorColor = new Color(0.62f, 0.60f, 0.56f),
                DoorWood = new Color(0.60f, 0.60f, 0.58f),
                Casing = new Color(0.55f, 0.56f, 0.55f),
            };
            System.Random rng = b.Rng;
            RaidRoom store = b.Room("Store", -7f, -5f, 7f, 1.5f, StoreRed, FloorKind.Checker);
            store.Lit = Chance(rng, 0.8);
            RaidRoom back = b.Room("Stock Room", -7f, 1.5f, 2f, 5f, GarageGrey, FloorKind.Concrete);
            back.Light = LightStyle.Bulb;
            RaidRoom rest = b.Room("Restroom", 2f, 1.5f, 7f, 5f, KitchenMint, FloorKind.Checker);
            rest.Light = LightStyle.Bulb;

            b.Door(-2.5f, -5f, 1.8f, true, true);
            b.Door(-4.0f, 1.5f, 1.4f);
            b.Door(4.5f, 1.5f, 1.4f);
            b.Door(-5.0f, 5f, 1.4f);
            b.Window(-5.6f, -5f); b.Window(0.9f, -5f, 1.6f); b.Window(4.6f, -5f, 1.6f);
            b.Window(-7f, -2.0f); b.Window(7f, -2.6f);
            b.BuildShell();

            b.Container(store, ContainerType.Desk, "Register", b.Wst(store, -2.2f, 0.72f), Sz(2.2f, 1.4f, 0.72f), (m, r) => FurnitureBuilder.ShopCounter(m, r, 2.2f));
            b.Container(store, ContainerType.Pantry, "Store Shelf", new RaidBuilding.Place(-1.0f, -1.6f, 90f), Sz(3.2f, 1.5f, 0.9f), (m, r) => FurnitureBuilder.Gondola(m, r, 3.2f));
            b.Container(store, ContainerType.Pantry, "Store Shelf", new RaidBuilding.Place(2.4f, -1.6f, 90f), Sz(3.2f, 1.5f, 0.9f), (m, r) => FurnitureBuilder.Gondola(m, r, 3.2f));
            bool power = store.Lit;
            b.Container(store, ContainerType.Fridge, "Drinks Cooler", b.E(store, -0.85f, 0.75f), Sz(0.9f, 2.0f, 0.75f), (m, r) => FurnitureBuilder.DrinksCooler(m, r, power));
            b.Furnish(store, b.E(store, 0.1f, 0.75f), Sz(0.9f, 2.0f, 0.75f), true, (m, r) => FurnitureBuilder.DrinksCooler(m, r, power), "cooler");
            // A cool light inside the cooler cabinet (its bulb hidden in the body) spills onto the aisle.
            b.Clutter(store, 2, false);
            b.Poster(store, 'N', -1.2f, 1.6f, 0.45f, 0.65f, 3);
            b.CeilingLight(store, -3.0f, -1.8f);
            b.CeilingLight(store, 3.5f, -1.8f);
            b.Litter(store, 1.3f);

            b.Furnish(back, b.N(back, -1.0f, 0.5f), Sz(1.8f, 1.9f, 0.5f), true, (m, r) => FurnitureBuilder.MetalShelf(m, r, 1.8f, 0.5f, 1.9f, 4, 0, new Color(0.40f, 0.40f, 0.38f)), "shelf");
            b.Container(back, ContainerType.Shelf, "Stock Shelf", b.E(back, 3.2f, 0.5f), Sz(1.4f, 1.9f, 0.5f), (m, r) => FurnitureBuilder.MetalShelf(m, r, 1.4f, 0.5f, 1.9f, 4, 0, new Color(0.40f, 0.40f, 0.38f)));
            b.Furnish(back, b.Wst(back, 2.4f, 0.6f), Sz(0.8f, 1.9f, 0.6f), true, (m, r) => FurnitureBuilder.GunSafe(m, r), "safe");
            b.Clutter(back, 2, false);
            b.CeilingLight(back, -2.5f, 3.2f);
            b.Litter(back, 0.6f);

            b.Furnish(rest, b.N(rest, 6.2f, 0.6f), Sz(0.45f, 0.8f, 0.6f), true, (m, r) => FurnitureBuilder.Toilet(m, r), "toilet");
            b.Furnish(rest, b.N(rest, 3.0f, 0.45f), Sz(0.55f, 1.8f, 0.45f), true, (m, r) => FurnitureBuilder.WashBasin(m, r), "basin");
            b.CeilingLight(rest, 4.5f, 3.5f);
            b.Litter(rest, 0.5f);

            b.DoorSign(-2.5f, -5f, "FOOD MART", new Color(0.70f, 0.12f, 0.10f), new Color(0.92f, 0.90f, 0.82f));
            b.Finish();
            return b;
        }
        #endregion

        #region Quarantine tent (toxic hot spot)
        /// <summary>A military quarantine tent (6 × 9 m canvas, open flap at the front) holding the
        /// map's best loot: a weapons locker, an equipment locker, a medical kit and supply crates.</summary>
        public static RaidBuilding QuarantineTent(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "QuarantineTent", center, turns, false, 6f, 9f)
            {
                WallH = 1.9f,
                Exterior = ExteriorStyle.Canvas,
                ExteriorColor = new Color(0.33f, 0.35f, 0.22f),
                WallCore = new Color(0.26f, 0.28f, 0.18f),
                FlatCeilings = false,
                DoorFrames = false,
            };
            RaidRoom tent = b.Room("Tent", -3f, -4.5f, 3f, 4.5f, Canvas, FloorKind.Plywood);
            tent.Light = LightStyle.ShopLamp;
            tent.Lit = true;
            tent.Flicker = true;
            b.Door(0f, -4.5f, 1.6f, false);
            b.BuildShell();

            LootContainer gun = b.Container(tent, ContainerType.GunLocker, "Weapons Locker", b.N(tent, -1.8f, 0.6f), Sz(0.8f, 1.9f, 0.6f), (m, r) => FurnitureBuilder.GunSafe(m, r));
            LootContainer kit = b.Container(tent, ContainerType.Locker, "Equipment Locker", b.N(tent, 0.3f, 0.5f), Sz(1.0f, 1.9f, 0.5f), (m, r) => FurnitureBuilder.LockerBank(m, r, 2, new Color(0.26f, 0.30f, 0.20f)));
            b.Container(tent, ContainerType.Crate, "Supply Crate", b.E(tent, 2.5f, 0.62f), Sz(0.9f, 0.6f, 0.62f), (m, r) => FurnitureBuilder.Crate(m, r, Vector3.zero, Sz(0.88f, 0.58f, 0.6f), 0f, true));
            b.Container(tent, ContainerType.MedCabinet, "Field Medical Kit", b.Wst(tent, 1.0f, 0.42f), Sz(0.9f, 1.8f, 0.42f), (m, r) => FurnitureBuilder.MedCabinet(m, r));
            Enrich(gun, b.Rng);
            Enrich(kit, b.Rng);

            b.Furnish(tent, b.E(tent, -1.0f, 0.75f), Sz(1.5f, 0.9f, 0.75f), true, (m, r) =>
            {
                FurnitureBuilder.Table(m, r, 1.5f, 0.75f, OliveDrab);
                FurnitureBuilder.FieldRadio(m, r, new Vector3(-0.3f, 0.755f, -0.05f), 5f);
                Vector3 bulb = FurnitureBuilder.DeskLamp(m, r, new Vector3(0.45f, 0.755f, -0.15f), 30f, GreenShade, true);
                b.LampLight(RaidBuilding.WorldOf(m, bulb), false);
                DeskClutter(m, r, 1.2f);
            }, "field desk");
            b.Furnish(tent, b.Wst(tent, -2.4f, 0.8f), Sz(1.9f, 0.5f, 0.8f), true, (m, r) =>
            {
                Surf frame = DecorPalette.Metal(new Color(0.25f, 0.27f, 0.22f));
                Surf cloth = DecorPalette.Matte(new Color(0.36f, 0.38f, 0.26f));
                for (int i = 0; i < 4; i++) m.Box(new Vector3(i % 2 == 0 ? -0.85f : 0.85f, 0.2f, i < 2 ? -0.32f : 0.32f), Sz(0.04f, 0.4f, 0.04f), frame);
                m.Box(new Vector3(0f, 0.42f, 0f), Sz(1.9f, 0.04f, 0.75f), cloth);
                m.Box(new Vector3(0.2f, 0.47f, 0f), Sz(1.2f, 0.06f, 0.7f), DecorPalette.Matte(OliveDrab));
            }, "cot");
            b.Furnish(tent, new RaidBuilding.Place(1.6f, -3.6f, 0f), Sz(0.6f, 0.9f, 0.6f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.62f, 0.52f, 0.12f), true), "barrel");
            b.Poster(tent, 'N', 2.2f, 1.2f, 0.4f, 0.4f, 0);
            b.CeilingLight(tent, 0f, 0.5f, 0f, 2.95f);
            b.Litter(tent, 0.8f);

            // The pitched canvas roof lives with the ceilings (hidden from the isometric view).
            Surf canvas = DecorMaterials.WallExterior(ExteriorStyle.Canvas, new Color(0.31f, 0.33f, 0.21f));
            Surf inner = DecorPalette.Matte(new Color(0.24f, 0.26f, 0.16f));
            const float eave = 1.9f, ridge = 3.0f, hw = 3.15f, hl = 4.65f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 e0 = b.W(s * hw, eave, -hl), e1 = b.W(s * hw, eave, hl);
                Vector3 r0 = b.W(0f, ridge, -hl), r1 = b.W(0f, ridge, hl);
                if (s > 0)
                {
                    Quad(b.Ceil, e0, r0, r1, e1, canvas);
                    Quad(b.Ceil, e0, e1, r1, r0, inner);
                }
                else
                {
                    Quad(b.Ceil, e0, e1, r1, r0, canvas);
                    Quad(b.Ceil, e0, r0, r1, e1, inner);
                }
                Vector3 slope = (r0 - e0).normalized;
                town.OverheadCollider((e0 + r1) * 0.5f, new Vector3(0.05f, Vector3.Distance(e0, r0), hl * 2f), Quaternion.LookRotation(e1 - e0, slope));
            }
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 a = b.W(-hw, eave, s * hl), c = b.W(hw, eave, s * hl), t = b.W(0f, ridge, s * hl);
                b.Ceil.Tri(a, t, c, canvas);
                b.Ceil.Tri(a, c, t, canvas);
            }
            b.Ceil.Box(b.W(0f, ridge + 0.02f, 0f), new Vector3(0.12f, 0.06f, hl * 2f + 0.1f), b.Yaw(0f), DecorPalette.Matte(new Color(0.2f, 0.2f, 0.16f)));
            b.Finish();
            return b;
        }

        /// <summary>A double-sided quad from four corners (a→b→c→d clockwise as seen from the front).</summary>
        private static void Quad(RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, in Surf s)
        {
            m.Tri(a, b, c, s);
            m.Tri(a, c, d, s);
        }

        /// <summary>Doubles a container's roll and adds a gas-mask filter and a battery: the prize behind
        /// the gas.</summary>
        private static void Enrich(LootContainer c, System.Random rng)
        {
            if (c == null || c.Contents == null) return;
            ItemDatabase.RollLoot(c.Type, rng, c.Contents);
            ItemDef filter = ItemDatabase.Get("gasmaskfilter");
            if (filter != null) c.Contents.Add(filter, 1, enforceWeight: false);
            ItemDef battery = ItemDatabase.Get("battery");
            if (battery != null && rng.NextDouble() < 0.7) c.Contents.Add(battery, 1, enforceWeight: false);
        }
        #endregion
    }
}
