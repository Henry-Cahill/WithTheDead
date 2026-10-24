using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The rooms of public buildings: the clinic's examination rooms and wards, the jail's cells and the armory, the church's
    // nave, the school's classrooms and its gym.
    internal static partial class InteriorPass
    {
        private static void Exam(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(true);
            // The couch with its head to a wall, the clinical sink, the medicine cabinet, a stool and a desk.
            if (c.AnyWall(c.Sides(false), 0.95f, 2.0f, 1.0f, c.Rng.NextDouble() < 0.5 ? 0.2f : 0.8f, out RaidBuilding.Place couch, out _)) b.ExamCouch(c.R, couch);
            if (c.AnyWall(order, 1.2f, 0.65f, 1.1f, -1f, out RaidBuilding.Place sink, out _))
                b.SteelCounter(c.R, sink, 1.2f, 0.25f, ContainerType.MedCabinet, "Counter", null, true);
            LootPiece(c, c.NextLoot() ?? ContainerType.MedCabinet, order);
            if (c.Floor(0.45f, 0.45f, 0f, c.Inner.center, 0.2f, out RaidBuilding.Place stool)) b.StoolSeat(c.R, stool, new Color(0.20f, 0.20f, 0.22f));
            if (c.Area > 10f && c.AnyWall(order, 1.2f, 0.75f, 0.9f, -1f, out RaidBuilding.Place desk, out int ds))
            {
                b.DrawerDesk(c.R, desk, 1.2f, Putty, false, "Desk", (m, r) => DeskClutter(m, r, 1.0f));
                Vector2 seat = c.OnWall(ds, ds == 0 || ds == 2 ? desk.X : desk.Z, 0.75f + 0.4f);
                if (c.Floor(0.55f, 0.55f, RoomCtx.YawOf(ds) + 180f, seat, 0.02f, out RaidBuilding.Place chair)) SwivelChair(c, chair, ChairFabrics[0]);
            }
            if (c.Rng.NextDouble() < 0.5 && c.Floor(0.5f, 0.5f, 0f, c.Inner.center, 0.1f, out RaidBuilding.Place iv)) b.IvStand(c.R, iv);
            WallDecor(c, 3);
        }

        private static void Ward(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(false);
            // Beds with their heads to the walls, a drip stand by some, screens between, the nurse's desk.
            int beds = Mathf.Clamp(Mathf.FloorToInt(c.Area / 7f), 1, 10);
            for (int k = 0; k < beds; k++)
            {
                if (!c.AnyWall(order, 0.95f, 2.0f, 1.2f, -1f, out RaidBuilding.Place bed, out int side)) break;
                b.WardBed(c.R, bed);
                float t = side == 0 || side == 2 ? bed.X : bed.Z;
                if (c.Rng.NextDouble() < 0.5 && c.WallAt(side, t + (c.Rng.NextDouble() < 0.5 ? -0.78f : 0.78f), 0.5f, 0.5f, 1.9f, out RaidBuilding.Place iv)) b.IvStand(c.R, iv);
                else if (c.Rng.NextDouble() < 0.4 && c.WallAt(side, t + 0.8f, 0.45f, 0.40f, 0.62f, out RaidBuilding.Place stand))
                    b.HomePiece(c.R, stand, HomeFurnitureKind.Nightstand);
            }
            if (c.Area > 20f && c.Floor(1.8f, 0.6f, 0f, c.Inner.center, 0.3f, out RaidBuilding.Place screen)) b.PrivacyScreen(c.R, screen);
            if (c.Rng.NextDouble() < 0.5 && c.Floor(0.7f, 0.8f, 0f, c.Inner.center, 0.2f, out RaidBuilding.Place wheel)) b.WheelchairSeat(c.R, wheel);
            LootPiece(c, ContainerType.MedCabinet, c.Sides(true));
            MoreLoot(c, LootCount(c, 0.03f, 2), order);
        }

        // A cell block: barred cells along the back wall, each with a cot and a toilet, their doors stood open; the
        // walkway before them.
        private static void Cells(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            int back = RoomCtx.Opposite(door);
            float across = back == 0 || back == 2 ? c.Depth : c.Width;
            float depth = Mathf.Min(2.4f, across - 1.8f);
            c.FreeRun(back, out float a, out float e);
            int n = Mathf.Clamp(Mathf.FloorToInt((e - a - 0.1f) / 2.1f), 0, 5);
            if (depth < 2.1f || n == 0)
            {
                Bunks(c);
                return;
            }
            float cell = (e - a - 0.1f) / n;
            Surf steel = DecorPalette.Metal(new Color(0.34f, 0.36f, 0.36f));
            float yaw = RoomCtx.YawOf(back);
            for (int k = 0; k < n; k++)
            {
                float t0 = a + 0.05f + k * cell;
                // The cot along the back wall, the toilet in the far corner.
                if (c.WallAt(back, t0 + 0.55f, 0.8f, 1.95f, 0.5f, out RaidBuilding.Place cot))
                    b.Furnish(c.R, cot, new Vector3(0.8f, 0.5f, 1.95f), true, (m, r) =>
                    {
                        m.Box(new Vector3(0f, 0.38f, 0f), new Vector3(0.78f, 0.06f, 1.9f), steel);
                        m.Box(new Vector3(0f, 0.45f, 0.05f), new Vector3(0.7f, 0.08f, 1.7f), DecorPalette.Matte(new Color(0.36f, 0.38f, 0.30f)));
                        for (int i = 0; i < 4; i++) m.Box(new Vector3((i & 1) == 0 ? -0.36f : 0.36f, 0.18f, (i & 2) == 0 ? -0.9f : 0.9f), new Vector3(0.04f, 0.36f, 0.04f), steel);
                    }, "Cot");
                if (c.WallAt(back, t0 + cell - 0.35f, 0.42f, 0.70f, 0.8f, out RaidBuilding.Place toilet)) Toilet(c, toilet);
                // The bars: a fixed run, then the door swung back against them.
                float fixedLen = cell - 1.0f;
                Vector2 q = c.OnWall(back, t0 + fixedLen * 0.5f, depth);
                b.Furnish(c.R, new RaidBuilding.Place(q.x, q.y, yaw), new Vector3(fixedLen, 2.3f, 0.08f), true, (m, r) =>
                {
                    Bars(m, fixedLen, 2.3f, steel);
                    // The door, swung open out of the cell beside the fixed run.
                    m.Box(new Vector3(fixedLen * 0.5f + 0.02f, 1.15f, 0.45f), new Vector3(0.03f, 2.2f, 0.9f), steel);
                }, "Cell bars");
                c.Taken.Add(c.Foot(back, t0 + cell * 0.5f, cell, depth + 0.05f));
                // The wall between this cell and the next: bars from the back wall to the front.
                if (k < n - 1)
                {
                    Vector2 w = c.OnWall(back, t0 + cell, depth * 0.5f);
                    b.Furnish(c.R, new RaidBuilding.Place(w.x, w.y, yaw + 90f), new Vector3(depth, 2.3f, 0.06f), true, (m, r) => Bars(m, depth, 2.3f, steel), "Cell bars");
                }
            }
            // The jailer's desk at the walkway's end.
            if (c.AnyWall(new[] { (back + 1) & 3, (back + 3) & 3, door }, 1.2f, 0.75f, 0.9f, -1f, out RaidBuilding.Place desk, out int ds))
            {
                RaidLayouts.WorkDesk(b, c.R, desk, 1.2f, Putty, false, 0, false, false, "Desk");
                Vector2 seat = c.OnWall(ds, ds == 0 || ds == 2 ? desk.X : desk.Z, 0.75f + 0.4f);
                if (c.Floor(0.45f, 0.45f, RoomCtx.YawOf(ds) + 180f, seat, 0.02f, out RaidBuilding.Place chair)) b.WoodChairSeat(c.R, chair, FurnitureBuilder.Oak);
            }
        }

        // A run of jail bars `len` long, `h` high, along the piece's X.
        private static void Bars(RaidMesh m, float len, float h, in Surf steel)
        {
            int rods = Mathf.Max(2, Mathf.RoundToInt(len / 0.13f));
            for (int i = 0; i <= rods; i++)
                m.Box(new Vector3(-len * 0.5f + len * i / rods, h * 0.5f, 0f), new Vector3(0.025f, h, 0.025f), steel);
            m.Box(new Vector3(0f, h - 0.04f, 0f), new Vector3(len, 0.06f, 0.06f), steel);
            m.Box(new Vector3(0f, 1.1f, 0f), new Vector3(len, 0.05f, 0.05f), steel);
            m.Box(new Vector3(0f, 0.04f, 0f), new Vector3(len, 0.06f, 0.06f), steel);
        }

        private static void Armory(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(false);
            LootPiece(c, ContainerType.GunLocker, order);
            if (c.AnyWall(order, 1.2f, 0.4f, 1.6f, -1f, out RaidBuilding.Place rack, out _))
                b.WeaponRack(c.R, rack, new Vector3(1.2f, 1.6f, 0.4f), (m, r) =>
                {
                    Surf wood = DecorPalette.Matte(FurnitureBuilder.Walnut), metal = DecorPalette.Metal(new Color(0.16f, 0.16f, 0.17f));
                    m.Box(new Vector3(0f, 0.15f, 0f), new Vector3(1.2f, 0.3f, 0.36f), wood);
                    m.Box(new Vector3(0f, 1.25f, -0.12f), new Vector3(1.2f, 0.08f, 0.12f), wood);
                    for (int i = 0; i < 5; i++)
                    {
                        if (r.NextDouble() < 0.5) continue;
                        float x = -0.48f + i * 0.24f;
                        m.Box(new Vector3(x, 0.75f, -0.05f), new Vector3(0.05f, 1.0f, 0.08f), metal);
                        m.Box(new Vector3(x, 0.38f, -0.03f), new Vector3(0.06f, 0.25f, 0.1f), wood);
                    }
                    m.Rod(new Vector3(-0.58f, 1.0f, 0.12f), new Vector3(0.58f, 1.0f, 0.12f), 0.012f, metal);
                }, "Gun rack");
            MoreLoot(c, LootCount(c, 0.15f, 3), order);
            for (int k = 0; k < 2; k++)
                if (c.AnyWall(order, 0.9f, 0.62f, 0.62f, -1f, out RaidBuilding.Place crate, out _))
                    b.SupplyCrate(c.R, crate, new Vector3(0.9f, 0.6f, 0.6f), true, ContainerType.GunLocker, "Ammunition crate", k == 0);
        }

        private static void Nave(RoomCtx c)
        {
            RaidBuilding b = c.B;
            Rect i = c.Inner;
            int front = EndFront(c);
            int back = RoomCtx.Opposite(front);
            bool facingZ = front == 0 || front == 2;
            float width = facingZ ? i.width : i.height, depth = facingZ ? i.height : i.width;
            float mid = facingZ ? i.center.x : i.center.y;
            // The chancel: the altar against the front wall on a step, the pulpit to one side, the lectern to the other.
            float chancel = Mathf.Clamp(depth * 0.22f, 2.2f, 4.5f);
            Vector2 step = c.OnWall(front, mid, chancel * 0.5f);
            Surf carpet = DecorPalette.Matte(new Color(0.42f, 0.08f, 0.10f)), wood = DecorPalette.Gloss(FurnitureBuilder.Walnut);
            float stepW = Mathf.Min(width - 0.4f, 6f);
            b.Furnish(c.R, new RaidBuilding.Place(step.x, step.y, RoomCtx.YawOf(front)), new Vector3(stepW, 0.15f, chancel), true, (m, r) =>
            {
                m.Box(new Vector3(0f, 0.075f, 0f), new Vector3(stepW, 0.15f, chancel), wood);
                m.Box(new Vector3(0f, 0.152f, 0f), new Vector3(stepW - 0.6f, 0.005f, chancel - 0.6f), carpet);
            }, "chancel");
            c.Taken.Add(c.Foot(front, mid, stepW, chancel));
            Vector2 altar = c.OnWall(front, mid, 0.6f);
            b.Furnish(c.R, new RaidBuilding.Place(altar.x, altar.y, RoomCtx.YawOf(front)), new Vector3(1.8f, 1.15f, 0.8f), true, (m, r) =>
            {
                m.Box(new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.9f, 0.75f), wood);
                m.Box(new Vector3(0f, 1.05f, 0f), new Vector3(1.9f, 0.02f, 0.8f), DecorPalette.Matte(new Color(0.92f, 0.90f, 0.84f)));
                m.Box(new Vector3(0f, 0.75f, 0.395f), new Vector3(0.7f, 0.6f, 0.01f), carpet);
                m.Box(new Vector3(0f, 1.3f, -0.2f), new Vector3(0.04f, 0.5f, 0.04f), DecorPalette.Metal(FurnitureBuilder.Brass));
                m.Box(new Vector3(0f, 1.42f, -0.2f), new Vector3(0.24f, 0.04f, 0.04f), DecorPalette.Metal(FurnitureBuilder.Brass));
                for (int k = -1; k <= 1; k += 2)
                {
                    m.Cylinder(new Vector3(k * 0.65f, 1.06f, -0.15f), 0.04f, 0.3f, 8, DecorPalette.Metal(FurnitureBuilder.Brass));
                    m.Cylinder(new Vector3(k * 0.65f, 1.36f, -0.15f), 0.02f, 0.12f, 6, DecorPalette.Matte(new Color(0.94f, 0.92f, 0.86f)));
                }
            }, "Altar");
            if (width > 6f)
            {
                Vector2 pulpit = c.OnWall(front, mid + Mathf.Min(width * 0.5f - 1.2f, stepW * 0.5f - 0.6f), chancel - 0.7f);
                b.Furnish(c.R, new RaidBuilding.Place(pulpit.x, pulpit.y, RoomCtx.YawOf(front)), new Vector3(0.9f, 1.25f, 0.7f), true, (m, r) =>
                {
                    m.Box(new Vector3(0f, 0.15f + 0.55f, 0f), new Vector3(0.8f, 1.1f, 0.6f), wood);
                    m.Box(new Vector3(0f, 1.27f, 0.05f), new Vector3(0.7f, 0.04f, 0.45f), wood);
                    m.Box(new Vector3(0f, 0.75f, 0.305f), new Vector3(0.3f, 0.5f, 0.01f), carpet);
                }, "Pulpit");
            }
            // Pews in two blocks either side of the aisle, facing the front.
            float aisle = width >= 9f ? 1.6f : 1.2f;
            float pewLen = Mathf.Min(4.2f, (width - aisle) * 0.5f - 0.7f);
            if (pewLen < 1.2f) pewLen = Mathf.Min(4.2f, width - 1.6f);
            bool twoBlocks = (width - aisle) * 0.5f - 0.7f >= 1.2f;
            int rows = Mathf.Clamp(Mathf.FloorToInt((depth - chancel - 1.0f - 1.6f) / 0.95f), 0, 24);
            for (int r = 0; r < rows; r++)
            {
                float d = chancel + 1.0f + 0.35f + r * 0.95f;
                for (int s = twoBlocks ? -1 : 0; s <= (twoBlocks ? 1 : 0); s += 2)
                {
                    float along = s == 0 ? mid : mid + s * (aisle * 0.5f + pewLen * 0.5f);
                    Vector2 q = c.OnWall(front, along, d);
                    Rect fp = facingZ ? new Rect(along - pewLen * 0.5f, q.y - 0.35f, pewLen, 0.7f) : new Rect(q.x - 0.35f, along - pewLen * 0.5f, 0.7f, pewLen);
                    if (!c.FloorAt(fp, 0.02f)) continue;
                    Pew(c, new RaidBuilding.Place(q.x, q.y, RoomCtx.YawOf(back)), pewLen, true);
                }
            }
            for (int k = 0; k < 2; k++)
                if (c.Wall(back, 0.4f, 0.4f, 1.0f, k == 0 ? 0.05f : 0.95f, out RaidBuilding.Place plant))
                    b.Furnish(c.R, plant, new Vector3(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "flowers");
            MoreLoot(c, 1, new[] { (front + 1) & 3, (front + 3) & 3 });
        }

        // A pew `len` long (0.7 deep, facing the placement's +Z): one body, a seat every 0.9 m along it; with a hymnal
        // shelf on its back when `shelf`.
        private static void Pew(RoomCtx c, RaidBuilding.Place p, float len, bool shelf)
        {
            RaidBuilding b = c.B;
            Surf wood = DecorPalette.Gloss(new Color(0.36f, 0.22f, 0.12f)), cushion = DecorPalette.Matte(new Color(0.42f, 0.10f, 0.12f));
            Transform t = b.FixtureBody(c.R, p, new Vector3(len, 0.9f, 0.7f), true, (m, r) =>
            {
                m.Box(new Vector3(0f, 0.44f, 0.04f), new Vector3(len, 0.05f, 0.42f), wood);
                m.Box(new Vector3(0f, 0.475f, 0.06f), new Vector3(len - 0.1f, 0.03f, 0.38f), cushion);
                m.Box(new Vector3(0f, 0.68f, -0.2f), new Vector3(len, 0.5f, 0.04f), Quaternion.Euler(-8f, 0f, 0f), wood);
                for (int k = -1; k <= 1; k += 2) m.Box(new Vector3(k * (len * 0.5f - 0.03f), 0.45f, -0.02f), new Vector3(0.05f, 0.9f, 0.62f), wood);
                if (shelf)
                {
                    m.Box(new Vector3(0f, 0.72f, -0.29f), new Vector3(len - 0.1f, 0.03f, 0.12f), wood);
                    int books = Mathf.FloorToInt(len / 0.6f);
                    for (int k = 0; k < books; k++)
                        if (r.NextDouble() < 0.7)
                            m.Box(new Vector3(-len * 0.5f + 0.35f + k * 0.6f, 0.8f, -0.29f), new Vector3(0.14f, 0.13f, 0.04f), DecorPalette.Matte(new Color(0.18f, 0.10f, 0.08f)));
                }
            }, "Pew");
            float wy = b.Yaw(p.Yaw);
            int seats = Mathf.Max(1, Mathf.FloorToInt(len / 0.9f));
            for (int k = 0; k < seats; k++)
                SeatFixture.Create(t, "Pew", new Vector3(-len * 0.5f + len * (k + 0.5f) / seats, CloneZConfig.ChairSeatHeight, 0.05f), wy,
                    CloneZConfig.ChairComfort, "Sit in the pew", "Stand up", false);
        }

        private static void Classroom(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            // The front: the partition across from the door (or beside it), where the chalkboard hangs.
            int front = RoomCtx.Opposite(door);
            if (!c.Inside[front])
                foreach (int s in c.Sides(true))
                    if (c.Inside[s] && s != door) { front = s; break; }
            Rect i = c.Inner;
            bool facingZ = front == 0 || front == 2;
            float width = facingZ ? i.width : i.height, depth = facingZ ? i.height : i.width;
            float mid = facingZ ? i.center.x : i.center.y;
            if (c.Inside[front] && width > 3f)
            {
                float bw = Mathf.Min(width - 1.6f, 4.2f);
                c.Span(front, out float lo, out float hi);
                if (c.WallAt(front, mid, bw, 0.08f, 0.9f, out RaidBuilding.Place board))
                    b.Furnish(c.R, board, new Vector3(bw, 2.2f, 0.08f), false, (m, r) =>
                    {
                        m.Box(new Vector3(0f, 1.5f, -0.02f), new Vector3(bw, 1.15f, 0.03f), DecorPalette.Matte(new Color(0.14f, 0.20f, 0.16f)));
                        m.Box(new Vector3(0f, 2.1f, -0.01f), new Vector3(bw + 0.08f, 0.06f, 0.05f), DecorPalette.Matte(FurnitureBuilder.Oak));
                        m.Box(new Vector3(0f, 0.9f, 0.01f), new Vector3(bw + 0.08f, 0.04f, 0.08f), DecorPalette.Matte(FurnitureBuilder.Oak));
                        for (int k = 0; k < 4; k++)
                            m.Box(new Vector3(MapStyle.Range(r, -bw * 0.4f, bw * 0.4f), MapStyle.Range(r, 1.2f, 1.9f), 0.0f), new Vector3(MapStyle.Range(r, 0.3f, 0.9f), 0.015f, 0.003f),
                                DecorPalette.Matte(new Color(0.80f, 0.82f, 0.78f)));
                    }, "chalkboard");
            }
            // The teacher's desk before it, off to one side.
            Vector2 td = c.OnWall(front, mid + width * 0.25f, 1.3f);
            if (c.Floor(1.4f, 0.75f, RoomCtx.YawOf(front) + 180f, td, 0.3f, out RaidBuilding.Place desk))
            {
                b.DrawerDesk(c.R, desk, 1.4f, FurnitureBuilder.Oak, true, "Teacher's desk", (m, r) => DeskClutter(m, r, 1.1f));
                Vector2 toFront = c.OnWall(front, 0f, 0f) - c.OnWall(front, 0f, 1f);
                var seat = new Vector2(desk.X, desk.Z) + toFront * 0.75f;
                var sf = new Rect(seat.x - 0.22f, seat.y - 0.22f, 0.44f, 0.44f);
                if (c.Clear(sf))
                {
                    c.Taken.Add(sf);
                    b.WoodChairSeat(c.R, new RaidBuilding.Place(seat.x, seat.y, RoomCtx.YawOf(front)), FurnitureBuilder.Oak);
                }
            }
            // The pupils' desks in rows facing the front, each with its chair (drawn with it).
            int cols = Mathf.Clamp(Mathf.FloorToInt((width - 1.2f) / 1.05f), 0, 7);
            int rows = Mathf.Clamp(Mathf.FloorToInt((depth - 3.0f - 0.8f) / 1.25f), 0, 6);
            Color top = new Color(0.66f, 0.54f, 0.36f), frame = new Color(0.24f, 0.30f, 0.34f);
            float yaw = RoomCtx.YawOf(RoomCtx.Opposite(front));
            for (int r = 0; r < rows; r++)
                for (int k = 0; k < cols; k++)
                {
                    float along = mid + (k - (cols - 1) * 0.5f) * 1.05f;
                    Vector2 q = c.OnWall(front, along, 3.0f + r * 1.25f);
                    var fp = new Rect(q.x - 0.36f, q.y - 0.5f, 0.72f, 1.0f);
                    if (!facingZ) fp = new Rect(q.x - 0.5f, q.y - 0.36f, 1.0f, 0.72f);
                    if (!c.FloorAt(fp, 0.05f)) continue;
                    bool knocked = c.Rng.NextDouble() < 0.12;
                    b.Furnish(c.R, new RaidBuilding.Place(q.x, q.y, yaw), new Vector3(0.7f, 0.8f, 1.0f), true, (m, rr) =>
                    {
                        Surf metal = DecorPalette.Metal(frame), wood = DecorPalette.Matte(top);
                        m.Box(new Vector3(0f, 0.7f, 0.22f), new Vector3(0.62f, 0.03f, 0.45f), wood);
                        m.Box(new Vector3(0f, 0.6f, 0.22f), new Vector3(0.58f, 0.12f, 0.4f), metal);
                        for (int l = 0; l < 4; l++) m.Box(new Vector3((l & 1) == 0 ? -0.28f : 0.28f, 0.3f, 0.22f + ((l & 2) == 0 ? -0.18f : 0.18f)), new Vector3(0.025f, 0.6f, 0.025f), metal);
                        if (knocked) m.Box(new Vector3(0.05f, 0.2f, -0.3f), new Vector3(0.4f, 0.4f, 0.04f), 30f, wood);
                        else
                        {
                            m.Box(new Vector3(0f, 0.43f, -0.28f), new Vector3(0.4f, 0.03f, 0.38f), wood);
                            m.Box(new Vector3(0f, 0.7f, -0.46f), new Vector3(0.4f, 0.25f, 0.025f), wood);
                            for (int l = 0; l < 4; l++) m.Box(new Vector3((l & 1) == 0 ? -0.18f : 0.18f, 0.21f, -0.28f + ((l & 2) == 0 ? -0.16f : 0.16f)), new Vector3(0.02f, 0.42f, 0.02f), metal);
                        }
                        if (rr.NextDouble() < 0.4)
                            m.FlatUV(new Vector3(MapStyle.Range(rr, -0.15f, 0.15f), 0.718f, 0.22f), 0.21f, 0.28f, MapStyle.Range(rr, -30f, 30f), DecorMaterials.Paper(rr.Next(3)), new Rect(0f, 0f, 1f, 1f));
                    }, "school desk");
                }
            // Cubbies and bookcases along the back, a cupboard, the flag.
            MoreLoot(c, LootCount(c, 0.04f, 2), new[] { door, (front + 1) & 3, (front + 3) & 3 });
            if (c.AnyWall(new[] { door, (front + 1) & 3, (front + 3) & 3 }, 0.9f, 0.32f, 1.8f, -1f, out RaidBuilding.Place books, out _)) Bookcase(c, books, FurnitureBuilder.Oak);
            WallDecor(c, 3);
            WallDecor(c, 4);
        }

        private static void Gym(RoomCtx c)
        {
            RaidBuilding b = c.B;
            Rect i = c.Inner;
            // The court's lines on the floor, a hoop at each end, bleachers along a side, the mats stacked in a corner.
            bool alongX = i.width >= i.height;
            float len = alongX ? i.width : i.height, wid = alongX ? i.height : i.width;
            Surf line = DecorPalette.Matte(new Color(0.86f, 0.84f, 0.76f));
            if (len > 10f && wid > 7f)
            {
                var centre = new RaidBuilding.Place(i.center.x, i.center.y, alongX ? 0f : 90f);
                float cl = len - 1.4f, cw = wid - 2.6f;
                b.Furnish(c.R, centre, new Vector3(0.1f, 0.01f, 0.1f), false, (m, r) =>
                {
                    m.Box(new Vector3(0f, 0.034f, cw * 0.5f), new Vector3(cl, 0.002f, 0.06f), line);
                    m.Box(new Vector3(0f, 0.034f, -cw * 0.5f), new Vector3(cl, 0.002f, 0.06f), line);
                    m.Box(new Vector3(cl * 0.5f, 0.034f, 0f), new Vector3(0.06f, 0.002f, cw), line);
                    m.Box(new Vector3(-cl * 0.5f, 0.034f, 0f), new Vector3(0.06f, 0.002f, cw), line);
                    m.Box(new Vector3(0f, 0.034f, 0f), new Vector3(0.06f, 0.002f, cw), line);
                }, "court lines");
                for (int k = -1; k <= 1; k += 2)
                {
                    int side = alongX ? (k < 0 ? 3 : 1) : (k < 0 ? 0 : 2);
                    if (c.Wall(side, 1.8f, 1.2f, 3.0f, 0.5f, out RaidBuilding.Place hoop))
                        b.Furnish(c.R, hoop, new Vector3(1.8f, 3.4f, 1.2f), false, (m, r) =>
                        {
                            m.Box(new Vector3(0f, 1.6f, -0.5f), new Vector3(0.12f, 3.2f, 0.12f), DecorPalette.Metal(new Color(0.22f, 0.26f, 0.30f)));
                            m.Box(new Vector3(0f, 2.5f, -0.1f), new Vector3(1.8f, 1.05f, 0.04f), DecorPalette.Matte(new Color(0.92f, 0.92f, 0.90f)));
                            m.Box(new Vector3(0f, 2.3f, 0.2f), new Vector3(0.46f, 0.02f, 0.46f), DecorPalette.Metal(new Color(0.85f, 0.35f, 0.10f)));
                        }, "basketball hoop");
                }
            }
            // Folded bleachers along a long side.
            int bench = alongX ? 2 : 1;
            float bl = Mathf.Min(len - 3f, 10f);
            if (bl > 3f && c.Wall(bench, bl, 1.2f, 1.2f, 0.5f, out RaidBuilding.Place bleachers))
                b.Furnish(c.R, bleachers, new Vector3(bl, 1.2f, 1.2f), true, (m, r) =>
                {
                    Surf plank = DecorPalette.Matte(new Color(0.64f, 0.50f, 0.32f));
                    for (int k = 0; k < 3; k++)
                        m.Box(new Vector3(0f, 0.38f + k * 0.38f, 0.4f - k * 0.4f), new Vector3(bl, 0.06f, 0.36f), plank);
                    m.Box(new Vector3(0f, 0.6f, -0.1f), new Vector3(bl, 1.2f, 0.95f), DecorPalette.Matte(new Color(0.30f, 0.30f, 0.32f)));
                }, "bleachers");
            if (c.AnyWall(c.Sides(false), 1.2f, 1.0f, 0.6f, 0f, out RaidBuilding.Place mats, out _))
                b.Furnish(c.R, mats, new Vector3(1.2f, 0.6f, 1.0f), true, (m, r) =>
                {
                    for (int k = 0; k < 4; k++)
                        m.Box(new Vector3(MapStyle.Range(r, -0.04f, 0.04f), 0.07f + k * 0.14f, MapStyle.Range(r, -0.04f, 0.04f)), new Vector3(1.15f, 0.13f, 0.95f),
                            MapStyle.Range(r, -4f, 4f), DecorPalette.Matte(k % 2 == 0 ? new Color(0.20f, 0.30f, 0.52f) : new Color(0.52f, 0.14f, 0.12f)));
                }, "mats");
            MoreLoot(c, 2, c.Sides(false));
        }
    }
}
