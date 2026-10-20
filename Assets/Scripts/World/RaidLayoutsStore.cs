using UnityEngine;

namespace CloneZ
{
    internal static partial class RaidLayouts
    {
        #region Gas station
        /// <summary>The gas station's food mart: the checkout counter with its till, gondolas of shelves to browse from
        /// either aisle, glass-door drinks coolers lit from within, a stock room with its shelving and a locked floor safe
        /// (work the dial), and a restroom with the home's own toilet and basin. 14 × 10 m; the pumps and canopy are added
        /// outside by the map.</summary>
        public static RaidBuilding GasStation(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "FoodMart", center, turns, false, 14f, 10f)
            {
                Kind = RaidBuildingKind.Store,
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

            CheckoutAt(b, store, b.Wst(store, -2.2f, 0.72f), 2.2f);
            GondolaAt(b, store, new RaidBuilding.Place(-1.0f, -1.6f, 90f), 3.2f, Chance(rng, 0.5));
            GondolaAt(b, store, new RaidBuilding.Place(2.4f, -1.6f, 90f), 3.2f, Chance(rng, 0.5));
            bool power = store.Lit;
            DrinksCoolerAt(b, store, b.E(store, -0.85f, 0.75f), power, true);
            DrinksCoolerAt(b, store, b.E(store, 0.1f, 0.75f), power, false);
            b.Clutter(store, 2, false);
            b.Poster(store, 'N', -1.2f, 1.6f, 0.45f, 0.65f, 3);
            b.CeilingLight(store, -3.0f, -1.8f);
            b.CeilingLight(store, 3.5f, -1.8f);
            b.Litter(store, 1.3f);

            Color shelf = new Color(0.40f, 0.40f, 0.38f);
            b.Shelving(back, b.N(back, -1.0f, 0.5f), Sz(1.8f, 1.9f, 0.5f), 4, 0, shelf, ContainerType.Shelf, "Stock shelf", false);
            b.Shelving(back, b.E(back, 3.2f, 0.5f), Sz(1.4f, 1.9f, 0.5f), 4, 0, shelf, ContainerType.Shelf, "Stock shelf", true);
            SafeAt(b, back, b.Wst(back, 2.4f, 0.6f));
            b.Clutter(back, 2, false);
            b.CeilingLight(back, -2.5f, 3.2f);
            b.Litter(back, 0.6f);

            b.HomePiece(rest, b.N(rest, 6.2f, 0.70f), HomeFurnitureKind.Toilet);
            b.HomePiece(rest, b.N(rest, 3.0f, 0.45f), HomeFurnitureKind.Basin);
            b.CeilingLight(rest, 4.5f, 3.5f);
            b.Litter(rest, 0.5f);

            b.DoorSign(-2.5f, -5f, "FOOD MART", new Color(0.70f, 0.12f, 0.10f), new Color(0.92f, 0.90f, 0.82f));
            b.Finish();
            return b;
        }
        #endregion

        #region Quarantine tent (toxic hot spot)
        /// <summary>A military quarantine tent (6 × 9 m canvas, open flap at the front) holding the
        /// map's best loot: a weapons locker, an equipment locker, a medical kit and supply crates — and the field desk
        /// whose radio is still on the Guard's nets, a camp cot to rest on and a drum of decon fluid.</summary>
        public static RaidBuilding QuarantineTent(RaidTown town, Vector2 center, int turns)
        {
            var b = new RaidBuilding(town, "QuarantineTent", center, turns, false, 6f, 9f)
            {
                Kind = RaidBuildingKind.Tent,
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

            StorageFixture gun = b.GunSafe(tent, b.N(tent, -1.8f, 0.6f), ContainerType.GunLocker, "Weapons Locker");
            StorageFixture kit = b.Lockers(tent, b.N(tent, 0.3f, 0.5f), Sz(1.0f, 1.9f, 0.5f), 2, new Color(0.26f, 0.30f, 0.20f), ContainerType.Locker, "Equipment Locker");
            b.SupplyCrate(tent, b.E(tent, 2.5f, 0.62f), Sz(0.9f, 0.6f, 0.62f), true, ContainerType.Crate, "Supply Crate");
            b.MedicineCabinet(tent, b.Wst(tent, 1.0f, 0.42f), ContainerType.MedCabinet, "Field Medical Kit");
            Enrich(gun != null ? gun.Container : null, b.Rng);
            Enrich(kit != null ? kit.Container : null, b.Rng);

            FieldDeskAt(b, tent, b.E(tent, -1.0f, 0.75f));
            RaidBuilding.Place cot = b.Wst(tent, -2.4f, 0.8f);
            Transform cotBody = b.FixtureBody(tent, cot, Sz(1.9f, 0.5f, 0.8f), true, (m, r) => FurnitureBuilder.TentCot(m, r, OliveDrab), "Camp cot");
            RaidStoreCot.Create(cotBody, b.Yaw(cot.Yaw));
            DrumAt(b, tent, new RaidBuilding.Place(1.6f, -3.6f, 0f), new Color(0.62f, 0.52f, 0.12f), true);
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

        /// <summary>The tent's field desk (1.5 × 0.9 × 0.75): an olive table with its lamp, its papers, and the field radio
        /// still on the Guard's nets (<see cref="RaidTownFieldRadio"/>).</summary>
        private static void FieldDeskAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p)
        {
            Vector3 dial = Vector3.zero;
            Transform t = b.FixtureBody(room, p, Sz(1.5f, 0.9f, 0.75f), true, (m, r) =>
            {
                FurnitureBuilder.Table(m, r, 1.5f, 0.75f, OliveDrab);
                dial = FurnitureBuilder.TownFieldRadio(m, r, new Vector3(-0.3f, 0.755f, -0.05f), 0f);
                b.DeskLamp(m, r, new Vector3(0.45f, 0.755f, -0.15f), 30f, GreenShade, true, false);
                DeskClutter(m, r, 1.2f);
            }, "Field desk");
            KitchenFrame f = b.FrameOf(p);
            MeshRenderer glow = KitchenParts.PieceAt(t, "RadioDial", f, dial, FurnitureBuilder.TownFieldRadioDial);
            RaidTownFieldRadio.Create(t, "Field radio", f.P(-0.3f, 0.95f, 0.2f), glow, b.Rng.Next());
        }
        #endregion

        #region Food mart furniture, made usable
        /// <summary>The checkout counter (<paramref name="len"/> × 1.4 × 0.72) with its cash register: the till's drawer
        /// shoots out on "Open the till" (a little loose change's worth of odds and ends), "Ring it up" for old times'
        /// sake, and the shelves under the counter — the store's register stock, as before.</summary>
        private static void CheckoutAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, float len)
        {
            float rx = 0f;
            Transform t = b.FixtureBody(room, p, Sz(len, 1.4f, 0.72f), true, (m, r) => rx = FurnitureBuilder.MartCounterBody(m, r, len), "Checkout counter");
            KitchenFrame f = b.FrameOf(p);
            var dr = new System.Random(b.Rng.Next());
            FixturePart drawer = KitchenParts.Drawer(t, "CashDrawer", f, m => FurnitureBuilder.MartTillDrawer(m, dr, rx),
                new Vector3(rx, FurnitureBuilder.MartTillY, FurnitureBuilder.MartTillFront), new Vector3(0f, 0f, CloneZConfig.RaidTillTravel));
            Vector3 tillPoint = f.P(rx, FurnitureBuilder.MartTillY, FurnitureBuilder.MartTillFront + 0.04f);
            StorageFixture till = null;
            StorageOptions o = RaidBuilding.SmallStock(ContainerType.Desk, "till");
            o.ContainerName = "Cash drawer";
            o.CloseVerb = "Shut";
            o.Noise = CloneZConfig.RaidTillBellNoise;
            o.ExtraActions = (ctx, list) => list.Add(new InteractAction("Ring it up", c =>
            {
                NoiseSystem.Emit(tillPoint, CloneZConfig.RaidTillBellNoise);
                if (till != null) till.SetOpen(true, false);
                c.Notify("You punch in a sale. The register chatters out a receipt \u2014 TOTAL $0.00, THANK YOU COME AGAIN \u2014 and the drawer rings open.");
            }));
            till = StorageFixture.Create(t, "Till", Interactable.KeyFor("till", t.position), tillPoint, new[] { drawer }, t, b.Rng.Next(), o);

            StorageOptions under = RaidBuilding.MainStock(ContainerType.Desk, "counter");
            under.ContainerName = "Register";
            under.LookLabel = "Search under the counter";
            StorageFixture.Create(t, "Checkout counter", Interactable.KeyFor("register", t.position), f.P(len * 0.2f, 0.7f, 0.39f), null, t, b.Rng.Next(), under);
        }

        /// <summary>A double-sided gondola of store shelves (<paramref name="len"/> × 1.5 × 0.9), each face browsed from its own
        /// aisle: one the store's stocked shelves (a main container, as before), the other picked over.</summary>
        private static void GondolaAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, float len, bool frontStocked)
        {
            Transform t = b.FixtureBody(room, p, Sz(len, 1.5f, 0.9f), true, (m, r) => FurnitureBuilder.Gondola(m, r, len), "Store shelves");
            for (int side = 0; side < 2; side++)
            {
                bool stocked = (side == 0) == frontStocked;
                StorageOptions o = stocked ? RaidBuilding.MainStock(ContainerType.Pantry, "shelves") : RaidBuilding.SmallStock(ContainerType.Pantry, "shelves");
                o.ContainerName = "Store shelves";
                o.LookLabel = "Browse the shelves";
                // No occlusion root: the gondola itself stands between a face and the far aisle.
                StorageFixture.Create(t, "Store shelves", Interactable.KeyFor("gondola", t.position, side), b.At(p, 0f, 1.0f, side == 0 ? 0.48f : -0.48f),
                    null, null, b.Rng.Next(), o);
            }
        }

        /// <summary>A glass-door drinks cooler (0.9 × 2.0 × 0.75): the door swings out on its hinge, a cool light burns
        /// inside while the store has its lights (<paramref name="lit"/>), and the drinks are a main container when
        /// <paramref name="stocked"/>, else a few cans at most.</summary>
        private static void DrinksCoolerAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p, bool lit, bool stocked)
        {
            Transform t = b.FixtureBody(room, p, Sz(0.9f, 2.0f, 0.75f), true, (m, r) => FurnitureBuilder.MartCoolerBody(m, r, lit), "Drinks cooler");
            KitchenFrame f = b.FrameOf(p);
            FixturePart door = KitchenParts.Door(t, "CoolerDoor", f, FurnitureBuilder.MartCoolerDoor, FurnitureBuilder.MartCoolerHinge,
                Vector3.right, Vector3.forward, CloneZConfig.RaidMartCoolerDoorAngle);
            if (lit) b.Town.Light(t, f.P(0f, 1.1f, 0.05f), new Color(0.80f, 0.90f, 1f), CloneZConfig.RaidMartCoolerLightRange,
                CloneZConfig.RaidMartCoolerLightIntensity, false, false);
            StorageOptions o = stocked ? RaidBuilding.MainStock(ContainerType.Fridge, "drinks cooler") : RaidBuilding.SmallStock(ContainerType.Fridge, "drinks cooler");
            o.ExtraActions = (ctx, list) => list.Add(new InteractAction("Feel how cold it is", c => c.Notify(lit
                ? "Cold air rolls out over your boots. The compressor's still humming away."
                : "No colder than the shop. The compressor's silent \u2014 somebody's pulled the plug.")));
            StorageFixture.Create(t, "Drinks cooler", Interactable.KeyFor("cooler", t.position), f.P(0.2f, 1.05f, FurnitureBuilder.MartCoolerFront + 0.07f),
                new[] { door }, t, b.Rng.Next(), o);
        }

        /// <summary>The stock room's floor safe (0.8 × 1.9 × 0.6): locked until somebody works the dial
        /// (<see cref="RaidStoreSafe"/>), then a safe like any other — a coin flip whether the takings were cleared out.</summary>
        private static void SafeAt(RaidBuilding b, RaidRoom room, RaidBuilding.Place p)
        {
            Color steel = new Color(0.20f, 0.23f, 0.21f);
            Transform t = b.FixtureBody(room, p, Sz(0.8f, 1.9f, 0.6f), true, (m, r) => {
                steel = FurnitureBuilder.MartSafeBody(m, r);
                if (b.Rng.NextDouble() < 0.5)
                    m.Box(new Vector3(0.25f, 0.9f, 0.31f), new Vector3(0.10f, 0.30f, 0.02f), DecorPalette.Matte(steel));
            }, "Safe");
            KitchenFrame f = b.FrameOf(p);
            FixturePart door = KitchenParts.Door(t, "SafeDoor", f, m => FurnitureBuilder.MartSafeDoor(m, steel), FurnitureBuilder.MartSafeHinge,
                Vector3.right, Vector3.forward, CloneZConfig.RaidSafeDoorAngle);
            StorageFixture safe = StorageFixture.Create(t, "Safe", Interactable.KeyFor("safe", t.position), f.P(0.1f, 1.1f, 0.36f), new[] { door }, t, b.Rng.Next(),
                new StorageOptions
                {
                    Noun = "safe",
                    LootType = ContainerType.GunLocker,
                    EmptyChance = CloneZConfig.RaidSafeEmptyChance,
                    MaxStacks = CloneZConfig.RaidSafeMaxStacks,
                    Noise = CloneZConfig.FixtureNoiseDoor,
                    RaidBoost = false,
                });
            RaidStoreSafe.Create(t, f.P(FurnitureBuilder.MartSafeDial + new Vector3(0f, 0f, 0.03f)), safe);
        }
        #endregion
    }
}
