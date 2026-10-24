using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Shops: the sales floor (the checkout by the door, gondolas down the middle, shelving round the walls, the coolers along
    // the back; a pharmacy's counter, a salon's chairs), and the stock rooms behind.
    internal static partial class InteriorPass
    {
        private static void Sales(RoomCtx c)
        {
            RaidBuilding b = c.B;
            InteriorUse use = c.Plan.Use;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            int back = RoomCtx.Opposite(door);
            // The checkout counter beside the way in, side-on to it.
            if (c.P.Entry && use != InteriorUse.Salon && c.Area > 12f)
            {
                float len = Mathf.Clamp(c.Len(door) * 0.3f, 1.6f, 2.6f);
                c.Span(door, out float lo, out float hi);
                bool low = Mathf.InverseLerp(lo, hi, c.DoorAlong) < 0.5f;
                // The side wall nearer the door, the counter at the door's end of it.
                int side = door == 0 || door == 2 ? (low ? 3 : 1) : (low ? 0 : 2);
                float pref = door == 1 || door == 2 ? 0.85f : 0.15f;
                if (c.Wall(side, len, 0.72f, 1.4f, pref, out RaidBuilding.Place counter) || c.Wall(RoomCtx.Opposite(side), len, 0.72f, 1.4f, pref, out counter))
                    Checkout(c, counter, len);
            }
            switch (use)
            {
                case InteriorUse.Salon:
                    Salon(c);
                    return;
                case InteriorUse.AutoShop:
                    if (c.AnyWall(c.Sides(false), 2.2f, 0.72f, 1.4f, 0.5f, out RaidBuilding.Place parts, out _)) Checkout(c, parts, 2.2f);
                    break;
                case InteriorUse.Pharmacy:
                    // The dispensing counter across the back, the medicine cabinets behind it.
                    if (c.Wall(back, Mathf.Min(3f, c.Len(back) - 1f), 0.72f, 1.4f, 0.5f, out RaidBuilding.Place rx)) Checkout(c, rx, Mathf.Min(3f, c.Len(back) - 1f));
                    for (int i = 0; i < 2; i++) LootPiece(c, ContainerType.MedCabinet, new[] { back, (back + 1) & 3, (back + 3) & 3 });
                    break;
            }
            Gondolas(c, door);
            // Shelving round the walls (not the front, where the windows are), the room's own loot on it.
            int shelves = Mathf.Clamp(Mathf.FloorToInt(c.Area / 9f), 1, 10);
            int[] walls = { back, (door + 1) & 3, (door + 3) & 3 };
            for (int i = 0; i < shelves; i++)
            {
                ContainerType t = c.NextLoot() ?? ContainerType.Shelf;
                if (t != ContainerType.Shelf && t != ContainerType.Pantry && t != ContainerType.MedCabinet && t != ContainerType.Crate) t = ContainerType.Shelf;
                if (!LootPiece(c, t, walls, i % 2 == 0 ? 0.2f : 0.8f)) break;
            }
            if (use == InteriorUse.Convenience || use == InteriorUse.GasStation || use == InteriorUse.BigStore || use == InteriorUse.Store && c.Plan.Town)
            {
                bool lit = c.P.Lit;
                int coolers = use == InteriorUse.BigStore ? 4 : 2;
                for (int i = 0; i < coolers; i++)
                    if (c.Wall(back, 0.9f, 0.75f, 2.0f, 0.15f + 0.2f * i, out RaidBuilding.Place cooler))
                    {
                        c.TakeLoot();
                        DrinksCooler(c, cooler, lit, i == 0);
                    }
            }
            if (c.Plan.Kind == RaidBuildingKind.Store) WallDecor(c, 3);
        }

        // Double-sided gondolas in rows down the floor from the front, the aisles between them, cut across where they'd run long.
        private static void Gondolas(RoomCtx c, int door)
        {
            if (c.Area < 22f) return;
            bool rowsAlongZ = door == 0 || door == 2;
            Rect i = c.Inner;
            float front = 2.4f, back = 1.3f, sideMargin = 1.5f;
            float span = rowsAlongZ ? i.width - 2f * sideMargin : i.height - 2f * sideMargin;
            float depth = (rowsAlongZ ? i.height : i.width) - front - back;
            if (span < 0.9f || depth < 1.6f) return;
            int rows = Mathf.Clamp(Mathf.FloorToInt((span + 1.2f) / 2.1f), 1, 12);
            float pitch = rows > 1 ? (span - 0.9f) / (rows - 1) : 0f;
            for (int r = 0; r < rows; r++)
            {
                float across = (rowsAlongZ ? i.xMin : i.yMin) + sideMargin + 0.45f + r * pitch + (rows == 1 ? span * 0.5f - 0.45f : 0f);
                // The run from the front margin to the back, in lengths of up to 4.8 m with a cross aisle between.
                float from = door == 0 || door == 3 ? (rowsAlongZ ? i.yMin : i.xMin) + front : (rowsAlongZ ? i.yMin : i.xMin) + back;
                float to = from + depth;
                int segs = Mathf.Max(1, Mathf.CeilToInt((depth + 1.2f) / 6.0f));
                float segLen = (depth - (segs - 1) * 1.2f) / segs;
                if (segLen < 1.2f) continue;
                for (int s = 0; s < segs; s++)
                {
                    float a = from + s * (segLen + 1.2f), mid = a + segLen * 0.5f;
                    var fp = rowsAlongZ ? new Rect(across - 0.45f, a, 0.9f, segLen) : new Rect(a, across - 0.45f, segLen, 0.9f);
                    if (!c.FloorAt(fp, 0.5f)) continue;
                    var place = rowsAlongZ ? new RaidBuilding.Place(across, mid, 90f) : new RaidBuilding.Place(mid, across, 0f);
                    Gondola(c, place, segLen, c.Rng.NextDouble() < 0.5);
                }
            }
        }

        private static void Salon(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(true);
            int side = order[0];
            Surf mirror = DecorPalette.Glass(new Color(0.55f, 0.60f, 0.64f));
            Color seat = new Color(0.40f, 0.12f, 0.12f);
            for (int k = 0; k < 3; k++)
            {
                if (!c.Wall(side, 0.9f, 0.5f, 0.95f, 0.2f + 0.3f * k, out RaidBuilding.Place counter)) break;
                b.Furnish(c.R, counter, new Vector3(0.9f, 1.9f, 0.5f), true, (m, r) =>
                {
                    m.Box(new Vector3(0f, 0.42f, 0f), new Vector3(0.9f, 0.84f, 0.45f), DecorPalette.Matte(new Color(0.82f, 0.80f, 0.74f)));
                    m.Box(new Vector3(0f, 1.45f, -0.2f), new Vector3(0.7f, 0.9f, 0.02f), mirror);
                    for (int j = 0; j < 3; j++)
                        m.Cylinder(new Vector3(-0.3f + j * 0.15f, 0.84f, 0f), 0.03f, 0.12f + 0.05f * j, 8, DecorPalette.Gloss(new Color(0.3f + 0.2f * j, 0.4f, 0.6f)));
                }, "salon counter");
                Vector2 front = c.OnWall(side, side == 0 || side == 2 ? counter.X : counter.Z, 0.5f + 0.55f);
                if (c.Floor(0.55f, 0.55f, RoomCtx.YawOf(side) + 180f, front, 0.02f, out RaidBuilding.Place chair)) b.OfficeChairSeat(c.R, chair, seat);
            }
            for (int k = 0; k < 3; k++)
                if (c.AnyWall(c.Sides(false), 0.45f, 0.45f, 0.9f, -1f, out RaidBuilding.Place wait, out _)) b.WoodChairSeat(c.R, wait, FurnitureBuilder.Pine, "Waiting chair");
            MoreLoot(c, 2, order);
            WallDecor(c, 3);
        }

        private static void Stock(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(false);
            RoomRole role = c.P.Role;
            if (role == RoomRole.Records)
            {
                for (int i = 0; i < Mathf.Clamp(Mathf.FloorToInt(c.Area / 5f), 1, 6); i++)
                    if (c.AnyWall(order, 0.46f, 0.62f, 1.4f, -1f, out RaidBuilding.Place cab, out _)) Filing(c, cab);
                for (int i = 0; i < 2; i++)
                    if (c.AnyWall(order, 1.4f, 0.5f, 1.9f, -1f, out RaidBuilding.Place sh, out _))
                        ShelvingAt(c, sh, new Vector3(1.4f, 1.9f, 0.5f), 5, 1, ShelfGrey, "Records shelf",
                            i == 0 ? RaidBuilding.MainStock(ContainerType.Shelf, "records shelf") : RaidBuilding.SmallStock(ContainerType.Shelf, "records shelf"));
                Clutter(c, 2);
                return;
            }
            if (role == RoomRole.Dispensary)
            {
                LootPiece(c, ContainerType.MedCabinet, order);
                if (c.AnyWall(order, 0.76f, 0.72f, 1.75f, -1f, out RaidBuilding.Place fridge, out _))
                    b.WorkFridge(c.R, fridge, new Color(0.86f, 0.86f, 0.84f), ContainerType.Fridge, "Medical fridge", true);
            }
            if (role == RoomRole.Sorting && c.Floor(1.6f, 0.8f, 0f, c.Inner.center, 0.6f, out RaidBuilding.Place table))
                b.WorkTable(c.R, table, new Vector3(1.6f, 0.9f, 0.8f), (m, r) =>
                {
                    FurnitureBuilder.Table(m, r, 1.6f, 0.8f, new Color(0.70f, 0.70f, 0.68f));
                    for (int k = 0; k < 4; k++) FurnitureBuilder.CardboardBox(m, r, new Vector3(MapStyle.Range(r, -0.6f, 0.6f), 0.755f, MapStyle.Range(r, -0.2f, 0.2f)),
                        new Vector3(0.3f, 0.2f, 0.24f), MapStyle.Range(r, 0f, 90f));
                }, "Sorting table", SpareStock(ContainerType.Desk, "mail"));
            // Shelving round the walls, the room's loot on it; crates and a pallet on the floor of a big one.
            int n = Mathf.Clamp(Mathf.FloorToInt(c.Area / 4.5f), 1, 8);
            for (int i = 0; i < n; i++)
            {
                ContainerType t = c.NextLoot() ?? ContainerType.Shelf;
                if (!LootPiece(c, t, order)) LootPiece(c, ContainerType.Shelf, order);
            }
            if (c.Area > 12f && c.Floor(1.1f, 1.1f, 0f, c.Inner.center, 0.5f, out RaidBuilding.Place crates))
            {
                if (c.TakeLoot())
                    b.StackedCrate(c.R, crates, new Vector3(1.1f, 1.4f, 1.1f), new Vector3(1.05f, 0.8f, 1.05f), new Vector3(0.8f, 0.55f, 0.75f), 0.05f,
                        MapStyle.Range(c.Rng, -10f, 10f), false, ContainerType.Crate, "Crate", true);
                else b.Furnish(c.R, crates, new Vector3(1.1f, 1.2f, 1.1f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, true), "pallet");
            }
            if (c.Area > 20f && c.Floor(1.2f, 1.0f, 0f, c.Inner.center, 0.6f, out RaidBuilding.Place pallet))
                b.Furnish(c.R, pallet, new Vector3(1.2f, 1.0f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, true), "pallet");
            Clutter(c, c.Area > 12f ? 3 : 1);
        }

        #region The raid town's shop fittings
        // The checkout counter with its cash register: the till's drawer, and the register stock under the counter.
        private static void Checkout(RoomCtx c, RaidBuilding.Place p, float len)
        {
            RaidBuilding b = c.B;
            c.TakeLoot();
            float rx = 0f;
            Transform t = b.FixtureBody(c.R, p, new Vector3(len, 1.4f, 0.72f), true, (m, r) => rx = FurnitureBuilder.MartCounterBody(m, r, len), "Checkout counter");
            KitchenFrame f = b.FrameOf(p);
            var dr = new System.Random(b.Rng.Next());
            FixturePart drawer = KitchenParts.Drawer(t, "CashDrawer", f, m => FurnitureBuilder.MartTillDrawer(m, dr, rx),
                new Vector3(rx, FurnitureBuilder.MartTillY, FurnitureBuilder.MartTillFront), new Vector3(0f, 0f, CloneZConfig.RaidTillTravel));
            StorageOptions o = RaidBuilding.SmallStock(ContainerType.Desk, "till");
            o.ContainerName = "Cash drawer";
            o.CloseVerb = "Shut";
            o.Noise = CloneZConfig.RaidTillBellNoise;
            StorageFixture.Create(t, "Till", Interactable.KeyFor("till", t.position), f.P(rx, FurnitureBuilder.MartTillY, FurnitureBuilder.MartTillFront + 0.04f),
                new[] { drawer }, t, b.Rng.Next(), o);
            StorageOptions under = RaidBuilding.MainStock(c.NextLoot() ?? ContainerType.Desk, "counter");
            under.ContainerName = "Register";
            under.LookLabel = "Search under the counter";
            StorageFixture.Create(t, "Checkout counter", Interactable.KeyFor("register", t.position), f.P(len * 0.2f, 0.7f, 0.39f), null, t, b.Rng.Next(), under);
        }

        // A double-sided gondola: its stock searchable while the room's loot budget lasts (one face stocked, the other picked
        // over), else plain shelving.
        private static void Gondola(RoomCtx c, RaidBuilding.Place p, float len, bool frontStocked)
        {
            RaidBuilding b = c.B;
            if (!c.TakeLoot())
            {
                b.Furnish(c.R, p, new Vector3(len, 1.5f, 0.9f), true, (m, r) => FurnitureBuilder.Gondola(m, r, len), "Store shelves");
                return;
            }
            Transform t = b.FixtureBody(c.R, p, new Vector3(len, 1.5f, 0.9f), true, (m, r) => FurnitureBuilder.Gondola(m, r, len), "Store shelves");
            ContainerType loot = c.Pool != null && c.Pool.Length > 0 ? c.Pool[0] : ContainerType.Pantry;
            int side = frontStocked ? 0 : 1;
            StorageOptions o = RaidBuilding.MainStock(loot, "shelves");
            o.ContainerName = "Store shelves";
            o.LookLabel = "Browse the shelves";
            StorageFixture.Create(t, "Store shelves", Interactable.KeyFor("gondola", t.position, side), b.At(p, 0f, 1.0f, side == 0 ? 0.48f : -0.48f),
                null, null, b.Rng.Next(), o);
        }

        // A glass-door drinks cooler, lit inside while the store has its lights.
        private static void DrinksCooler(RoomCtx c, RaidBuilding.Place p, bool lit, bool stocked)
        {
            RaidBuilding b = c.B;
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.9f, 2.0f, 0.75f), true, (m, r) => FurnitureBuilder.MartCoolerBody(m, r, lit), "Drinks cooler");
            KitchenFrame f = b.FrameOf(p);
            FixturePart door = KitchenParts.Door(t, "CoolerDoor", f, FurnitureBuilder.MartCoolerDoor, FurnitureBuilder.MartCoolerHinge,
                Vector3.right, Vector3.forward, CloneZConfig.RaidMartCoolerDoorAngle);
            StorageOptions o = stocked ? RaidBuilding.MainStock(ContainerType.Fridge, "drinks cooler") : RaidBuilding.SmallStock(ContainerType.Fridge, "drinks cooler");
            StorageFixture.Create(t, "Drinks cooler", Interactable.KeyFor("cooler", t.position), f.P(0.2f, 1.05f, FurnitureBuilder.MartCoolerFront + 0.07f),
                new[] { door }, t, b.Rng.Next(), o);
        }

        // The stock room's or the office's floor safe: locked until somebody works the dial.
        private static void Safe(RoomCtx c, RaidBuilding.Place p)
        {
            RaidBuilding b = c.B;
            Color steel = new Color(0.20f, 0.23f, 0.21f);
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.8f, 1.9f, 0.6f), true, (m, r) => steel = FurnitureBuilder.MartSafeBody(m, r), "Safe");
            KitchenFrame f = b.FrameOf(p);
            FixturePart door = KitchenParts.Door(t, "SafeDoor", f, m => FurnitureBuilder.MartSafeDoor(m, steel), FurnitureBuilder.MartSafeHinge,
                Vector3.right, Vector3.forward, CloneZConfig.RaidSafeDoorAngle);
            StorageFixture safe = StorageFixture.Create(t, "Safe", Interactable.KeyFor("safe", t.position), f.P(0.1f, 1.1f, 0.36f), new[] { door }, t, b.Rng.Next(),
                new StorageOptions
                {
                    Noun = "safe", LootType = ContainerType.GunLocker, EmptyChance = CloneZConfig.RaidSafeEmptyChance,
                    MaxStacks = CloneZConfig.RaidSafeMaxStacks, Noise = CloneZConfig.FixtureNoiseDoor, RaidBoost = false,
                });
            RaidStoreSafe.Create(t, f.P(FurnitureBuilder.MartSafeDial + new Vector3(0f, 0f, 0.03f)), safe);
        }

        private static StorageOptions SpareStock(ContainerType type, string noun) => RaidBuilding.SpareStock(type, noun);
        #endregion
    }
}
