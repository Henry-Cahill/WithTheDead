using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Offices and the rooms of public buildings: desks with their chairs and filing cabinets, a desk pod on an office floor,
    // the lobby's reception (a bank's teller counter, a post office's), the break room, the vault, the library's stacks and a
    // meeting hall's rows of chairs.
    internal static partial class InteriorPass
    {
        private static readonly Color[] ChairFabrics = { new Color(0.20f, 0.24f, 0.32f), new Color(0.22f, 0.20f, 0.18f), new Color(0.30f, 0.12f, 0.10f), new Color(0.24f, 0.28f, 0.22f) };

        private static void Office(RoomCtx c, bool floor)
        {
            RaidBuilding b = c.B;
            System.Random rng = c.Rng;
            int[] order = c.Sides(false);
            Color fabric = ChairFabrics[rng.Next(ChairFabrics.Length)];
            Color wood = Woods[rng.Next(Woods.Length)];
            if (floor && c.Width >= 4.2f && c.Depth >= 4.2f)
            {
                // Pods of desks back to back across the floor, a swivel chair at each (as many as the building's furniture
                // budget takes).
                Rect i = c.Inner;
                int cols = Mathf.Clamp(Mathf.FloorToInt((i.width - 1.6f) / 3.4f), 1, 6), rows = Mathf.Clamp(Mathf.FloorToInt((i.height - 1.6f) / 4.2f), 1, 6);
                for (int cx = 0; cx < cols; cx++)
                    for (int cz = 0; cz < rows; cz++)
                    {
                        if (c.Crowded) break;
                        float x = Mathf.Lerp(i.xMin + 1.7f, i.xMax - 1.7f, cols == 1 ? 0.5f : cx / (float)(cols - 1));
                        float z = Mathf.Lerp(i.yMin + 2.1f, i.yMax - 2.1f, rows == 1 ? 0.5f : cz / (float)(rows - 1));
                        if (!c.FloorAt(new Rect(x - 1.5f, z - 0.75f, 3.0f, 1.5f), 0.7f)) continue;
                        for (int k = 0; k < 4; k++)
                        {
                            float dx = (k & 1) == 0 ? -0.75f : 0.75f;
                            bool south = k < 2;
                            var desk = new RaidBuilding.Place(x + dx, z + (south ? -0.375f : 0.375f), south ? 180f : 0f);
                            RaidLayouts.WorkDesk(b, c.R, desk, 1.5f, Putty, rng.NextDouble() < 0.3, DeskLampOf(c, 0.25), false, k == 0 && c.TakeLoot());
                            var chair = new RaidBuilding.Place(x + dx, z + (south ? -1.2f : 1.2f), (south ? 0f : 180f) + MapStyle.Range(rng, -25f, 25f));
                            SwivelChair(c, chair, fabric);
                        }
                    }
                for (int k = 0; k < 2; k++)
                    if (c.AnyWall(order, 0.46f, 0.62f, 1.4f, -1f, out RaidBuilding.Place cab, out _)) Filing(c, cab);
                if (c.AnyWall(order, 0.9f, 0.6f, 1.1f, -1f, out RaidBuilding.Place copier, out _)) Copier(c, copier);
                if (c.AnyWall(order, 0.32f, 0.32f, 1.4f, -1f, out RaidBuilding.Place cooler, out _)) WaterCooler(c, cooler);
            }
            else
            {
                // A desk facing into the room from the wall across from the door, its chair behind it, a visitor's chair before it;
                // in a narrow room, against the wall.
                int deskSide = c.DoorSide >= 0 ? RoomCtx.Opposite(c.DoorSide) : order[0];
                float dw = Mathf.Min(1.6f, c.Len(deskSide) - 0.4f);
                float deep = deskSide == 0 || deskSide == 2 ? c.Depth : c.Width;
                bool placed = false;
                string deskName = c.P.Role == RoomRole.Sacristy ? "Desk" : "Office desk";
                int lamp = DeskLampOf(c, 0.85);
                if (dw >= 1.1f && deep >= 3.4f)
                {
                    c.Span(deskSide, out float lo, out float hi);
                    float mid = (lo + hi) * 0.5f, reach = Mathf.Max(0f, (hi - lo - dw) * 0.5f - 0.1f);
                    for (int k = 0; k < 7 && !placed; k++)
                    {
                        float t = mid + (k + 1) / 2 * 0.5f * (k % 2 == 0 ? 1f : -1f);
                        if (Mathf.Abs(t - mid) > reach + 1e-3f) continue;
                        Rect zone = c.Foot(deskSide, t, dw + 0.1f, 2.3f);
                        if (!c.Clear(zone)) continue;
                        c.Taken.Add(zone);
                        Vector2 at = c.OnWall(deskSide, t, 1.15f), seat = c.OnWall(deskSide, t, 0.45f), visitor = c.OnWall(deskSide, t + 0.25f, 1.95f);
                        RaidLayouts.WorkDesk(b, c.R, new RaidBuilding.Place(at.x, at.y, RoomCtx.YawOf(deskSide) + 180f), dw, wood, rng.NextDouble() < 0.25, lamp,
                            false, true, deskName);
                        SwivelChair(c, new RaidBuilding.Place(seat.x, seat.y, RoomCtx.YawOf(deskSide)), fabric);
                        b.WoodChairSeat(c.R, new RaidBuilding.Place(visitor.x, visitor.y, RoomCtx.YawOf(deskSide) + 180f), wood);
                        placed = true;
                    }
                }
                if (!placed && dw >= 1.1f && c.Wall(deskSide, dw, 0.75f, 0.9f, 0.5f, out RaidBuilding.Place desk))
                {
                    RaidLayouts.WorkDesk(b, c.R, desk, dw, wood, rng.NextDouble() < 0.25, lamp, false, true, deskName);
                    Vector2 seat = c.OnWall(deskSide, deskSide == 0 || deskSide == 2 ? desk.X : desk.Z, 0.75f + 0.45f);
                    if (c.Floor(0.55f, 0.55f, RoomCtx.YawOf(deskSide) + 180f, seat, 0.02f, out RaidBuilding.Place chair)) SwivelChair(c, chair, fabric);
                }
                if (c.P.Role == RoomRole.Sacristy && c.AnyWall(order, 1.0f, 0.6f, 1.95f, -1f, out RaidBuilding.Place robes, out _)) b.Wardrobe(c.R, robes, FurnitureBuilder.Walnut);
                if (c.AnyWall(order, 0.46f, 0.62f, 1.4f, -1f, out RaidBuilding.Place cab, out _)) Filing(c, cab);
                if (c.Area > 9f && c.AnyWall(order, 0.9f, 0.32f, 1.8f, -1f, out RaidBuilding.Place books, out _)) Bookcase(c, books, wood);
                if (c.Plan.Use == InteriorUse.Bank || c.P.Role == RoomRole.BackOffice && c.Rng.NextDouble() < 0.4)
                    if (c.AnyWall(order, 0.8f, 0.6f, 1.9f, 1f, out RaidBuilding.Place safe, out _)) Safe(c, safe);
            }
            MoreLoot(c, LootCount(c, 0.06f, 3), order);
            if (c.Area > 8f && c.AnyWall(order, 0.4f, 0.4f, 1.0f, 0f, out RaidBuilding.Place plant, out _))
                b.Furnish(c.R, plant, new Vector3(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "plant");
            WallDecor(c, c.Rng.NextDouble() < 0.5 ? 1 : 3);
        }

        private static void Lobby(RoomCtx c)
        {
            RaidBuilding b = c.B;
            System.Random rng = c.Rng;
            int[] order = c.Sides(false);
            InteriorUse use = c.Plan.Use;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            int back = RoomCtx.Opposite(door);
            float across = door == 0 || door == 2 ? c.Depth : c.Width;
            if (c.P.Role == RoomRole.Narthex)
            {
                // A table of hymnals and bulletins, a bench, the notices.
                if (c.AnyWall(order, 1.2f, 0.5f, 0.8f, 0.2f, out RaidBuilding.Place table, out _))
                    b.WorkTable(c.R, table, new Vector3(1.2f, 0.8f, 0.5f), (m, r) =>
                    {
                        FurnitureBuilder.Table(m, r, 1.2f, 0.5f, FurnitureBuilder.Walnut);
                        for (int k = 0; k < 3; k++) m.Box(new Vector3(-0.4f + k * 0.3f, 0.8f, 0f), new Vector3(0.16f, 0.06f, 0.22f), DecorPalette.Matte(new Color(0.20f, 0.12f, 0.10f)));
                    }, "Table");
                if (c.AnyWall(order, 1.6f, 0.45f, 0.9f, 0.8f, out RaidBuilding.Place bench, out _)) Pew(c, bench, 1.6f, false);
                WallDecor(c, 3);
                return;
            }
            if ((use == InteriorUse.Bank || use == InteriorUse.PostOffice || use == InteriorUse.Police) && across >= 4f)
            {
                // The counter across the room, the public's side by the door.
                float len = Mathf.Min(c.Len(back) - 1.6f, 6f);
                if (len >= 1.6f && c.Wall(back, len, 0.72f, 1.4f, 0.5f, out RaidBuilding.Place counter))
                {
                    // Out from the back wall, with room to stand behind it.
                    Vector2 off = c.OnWall(back, 0f, 1.2f) - c.OnWall(back, 0f, 0f);
                    var p = new RaidBuilding.Place(counter.X + off.x, counter.Z + off.y, counter.Yaw + 180f);
                    Checkout(c, p, len);
                    var behind = c.Foot(back, back == 0 || back == 2 ? counter.X : counter.Z, len, 1.9f);
                    c.Taken.Add(behind);
                    if (use == InteriorUse.PostOffice) PostBoxes(c);
                }
            }
            else if (c.Area > 10f)
            {
                // The reception desk facing the door.
                float dw = Mathf.Min(1.8f, c.Len(back) - 0.6f);
                if (dw > 1.1f && c.Wall(back, dw, 0.75f, 0.9f, 0.5f, out RaidBuilding.Place desk))
                {
                    RaidLayouts.WorkDesk(b, c.R, desk, dw, Woods[rng.Next(Woods.Length)], false, 0, false, false, use == InteriorUse.Clinic ? "Reception desk" : "Front desk");
                    Vector2 seat = c.OnWall(back, back == 0 || back == 2 ? desk.X : desk.Z, 0.75f + 0.4f);
                    if (c.Floor(0.55f, 0.55f, RoomCtx.YawOf(back) + 180f, seat, 0.02f, out RaidBuilding.Place chair)) SwivelChair(c, chair, ChairFabrics[0]);
                }
            }
            // Chairs along the walls to wait in.
            int chairs = Mathf.Clamp(Mathf.FloorToInt(c.Area / (c.P.Role == RoomRole.Waiting ? 2.5f : 5f)), 1, 10);
            Color seatColour = c.P.Role == RoomRole.Waiting ? new Color(0.30f, 0.40f, 0.42f) : FurnitureBuilder.Walnut;
            for (int k = 0; k < chairs; k++)
            {
                if (!c.AnyWall(order, 0.5f, 0.45f, 0.9f, -1f, out RaidBuilding.Place chair, out _)) break;
                b.WoodChairSeat(c.R, chair, seatColour, c.P.Role == RoomRole.Waiting ? "Waiting-room chair" : "Chair");
            }
            if (c.AnyWall(order, 0.4f, 0.4f, 1.0f, 0f, out RaidBuilding.Place plant, out _))
                b.Furnish(c.R, plant, new Vector3(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "plant");
            if (c.Area > 18f) MoreLoot(c, 1, order);
            WallDecor(c, 3);
        }

        // A wall of brass post-office boxes (a fixed piece drawn on the wall).
        private static void PostBoxes(RoomCtx c)
        {
            foreach (int side in c.Sides(true))
            {
                if (!c.Wall(side, 2.0f, 0.2f, 1.9f, 0.5f, out RaidBuilding.Place p)) continue;
                c.B.Furnish(c.R, p, new Vector3(2.0f, 1.9f, 0.2f), true, (m, r) =>
                {
                    m.Box(new Vector3(0f, 1.0f, -0.05f), new Vector3(2.0f, 1.8f, 0.1f), DecorPalette.Matte(FurnitureBuilder.Walnut));
                    Surf door = DecorPalette.Metal(FurnitureBuilder.Brass), glass = DecorPalette.Glass(new Color(0.15f, 0.15f, 0.16f));
                    for (int y = 0; y < 8; y++)
                        for (int x = 0; x < 10; x++)
                        {
                            var at = new Vector3(-0.9f + x * 0.2f, 0.25f + y * 0.2f, 0.005f);
                            m.Box(at, new Vector3(0.17f, 0.17f, 0.012f), door);
                            m.Box(at + new Vector3(0f, 0.03f, 0.008f), new Vector3(0.08f, 0.06f, 0.004f), glass);
                        }
                }, "post boxes");
                return;
            }
        }

        private static void BreakRoom(RoomCtx c)
        {
            RaidBuilding b = c.B;
            Kitchenette(c);
            if (c.AnyWall(c.Sides(true), 0.72f, 0.70f, 1.72f, -1f, out RaidBuilding.Place fridge, out _)) b.HomePiece(c.R, fridge, HomeFurnitureKind.Fridge);
            TableAndChairs(c, false);
            if (c.P.Role == RoomRole.DayRoom)
            {
                if (c.AnyWall(c.Sides(false), 2.0f, 0.88f, 0.85f, -1f, out RaidBuilding.Place sofa, out int ss))
                {
                    b.HouseSofa(c.R, sofa, 2.0f, Upholstery[c.Rng.Next(Upholstery.Length)]);
                    if (c.Wall(RoomCtx.Opposite(ss), 1.0f, 0.45f, 1.1f, 0.5f, out RaidBuilding.Place tv)) b.HouseTv(c.R, tv);
                }
            }
            else if (c.AnyWall(c.Sides(false), 0.9f, 0.8f, 1.85f, -1f, out RaidBuilding.Place vend, out _)) Vending(c, vend, c.P.Lit);
            WallDecor(c, 1);
        }

        private static void Vault(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(false);
            for (int k = 0; k < 2; k++)
                if (c.AnyWall(order, 0.8f, 0.6f, 1.9f, -1f, out RaidBuilding.Place safe, out _)) Safe(c, safe);
            // The safe-deposit boxes.
            if (c.AnyWall(order, 1.6f, 0.45f, 2.0f, 0.5f, out RaidBuilding.Place boxes, out _))
                b.Furnish(c.R, boxes, new Vector3(1.6f, 2.0f, 0.45f), true, (m, r) =>
                {
                    m.Box(new Vector3(0f, 1.0f, -0.02f), new Vector3(1.6f, 2.0f, 0.41f), DecorPalette.Metal(new Color(0.46f, 0.46f, 0.44f)));
                    Surf face = DecorPalette.Metal(new Color(0.62f, 0.60f, 0.52f));
                    for (int y = 0; y < 9; y++)
                        for (int x = 0; x < 6; x++)
                            m.Box(new Vector3(-0.65f + x * 0.26f, 0.2f + y * 0.2f, 0.19f), new Vector3(0.24f, 0.18f, 0.02f), face);
                }, "safe-deposit boxes");
            MoreLoot(c, 1, order);
        }

        private static void Stacks(RoomCtx c)
        {
            RaidBuilding b = c.B;
            // Bookcases round the walls, rows of them back to back across the floor, a reading table or two, the desk by the door.
            int[] order = c.Sides(false);
            for (int k = 0; k < 12; k++)
                if (!c.AnyWall(order, 0.9f, 0.32f, 1.8f, -1f, out RaidBuilding.Place shelf, out _)) break;
                else Bookcase(c, shelf, FurnitureBuilder.Oak);
            Rect i = c.Inner;
            bool alongZ = i.height >= i.width;
            int rows = Mathf.Clamp(Mathf.FloorToInt(((alongZ ? i.width : i.height) - 3.2f) / 2.2f), 0, 8);
            float runLen = (alongZ ? i.height : i.width) * 0.55f;
            for (int r = 0; r < rows; r++)
            {
                float across = (alongZ ? i.xMin : i.yMin) + 1.9f + r * 2.2f;
                float mid = (alongZ ? i.yMax : i.xMax) - 1.4f - runLen * 0.5f;
                int n = Mathf.Max(1, Mathf.FloorToInt(runLen / 0.9f));
                Rect fp = alongZ ? new Rect(across - 0.33f, mid - n * 0.45f, 0.66f, n * 0.9f) : new Rect(mid - n * 0.45f, across - 0.33f, n * 0.9f, 0.66f);
                if (!c.FloorAt(fp, 0.6f)) continue;
                for (int k = 0; k < n; k++)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float a = (alongZ ? fp.yMin : fp.xMin) + 0.45f + k * 0.9f;
                        var p = alongZ ? new RaidBuilding.Place(across + s * 0.17f, a, s > 0 ? 90f : 270f) : new RaidBuilding.Place(a, across + s * 0.17f, s > 0 ? 0f : 180f);
                        b.Furnish(c.R, p, new Vector3(0.9f, 1.8f, 0.32f), true, (m, rr) => FurnitureBuilder.Bookshelf(m, rr, FurnitureBuilder.Oak, 1), "Bookcase");
                    }
            }
            for (int k = 0; k < 2; k++)
            {
                if (!c.Floor(1.6f, 0.9f, 0f, c.Inner.center, 0.7f, out RaidBuilding.Place table)) break;
                b.WorkTable(c.R, table, new Vector3(1.6f, 0.9f, 0.9f), (m, r) => FurnitureBuilder.Table(m, r, 1.6f, 0.9f, FurnitureBuilder.Oak), "Reading table", null, true);
                b.WoodChairSeat(c.R, new RaidBuilding.Place(table.X - 0.4f, table.Z - 0.75f, 0f), FurnitureBuilder.Oak);
                b.WoodChairSeat(c.R, new RaidBuilding.Place(table.X + 0.4f, table.Z + 0.75f, 180f), FurnitureBuilder.Oak);
            }
            MoreLoot(c, 1, order);
        }

        // A meeting hall, a council chamber, a funeral home's chapel: rows of chairs facing the table (or the casket) at the far
        // end.
        private static void Meeting(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            int front = RoomCtx.Opposite(door);
            bool chapel = c.P.Role == RoomRole.Chapel;
            bool fellowship = c.Plan.Use == InteriorUse.Church && !chapel;
            float len = c.Len(front);
            if (fellowship)
            {
                // Long tables with folding chairs down both sides, for the suppers.
                for (int k = 0; k < 4; k++)
                {
                    if (!c.Floor(2.4f, 2.0f, front == 0 || front == 2 ? 0f : 90f, c.Inner.center, 0.5f, out RaidBuilding.Place longTable)) break;
                    var top = new RaidBuilding.Place(longTable.X, longTable.Z, longTable.Yaw);
                    b.WorkTable(c.R, top, new Vector3(2.4f, 0.8f, 0.8f), (m, r) => FurnitureBuilder.Table(m, r, 2.4f, 0.8f, new Color(0.70f, 0.66f, 0.56f)), "Table", null, true);
                    Quaternion turn = Quaternion.Euler(0f, longTable.Yaw, 0f);
                    for (int s = 0; s < 4; s++)
                    {
                        Vector3 off = turn * new Vector3((s & 1) == 0 ? -0.6f : 0.6f, 0f, (s & 2) == 0 ? -0.62f : 0.62f);
                        b.WoodChairSeat(c.R, new RaidBuilding.Place(longTable.X + off.x, longTable.Z + off.z, longTable.Yaw + ((s & 2) == 0 ? 0f : 180f)), FurnitureBuilder.Pine);
                    }
                }
                MoreLoot(c, 1, c.Sides(false));
                return;
            }
            // The table (the casket on its bier) facing the room from the far wall.
            float tw = Mathf.Min(chapel ? 2.2f : 2.6f, len - 1f);
            if (tw > 1.2f && c.Wall(front, tw, 0.8f, 0.9f, 0.5f, out RaidBuilding.Place table, 0.6f))
            {
                if (chapel)
                    b.Furnish(c.R, table, new Vector3(tw, 1.0f, 0.8f), true, (m, r) =>
                    {
                        m.Box(new Vector3(0f, 0.35f, 0f), new Vector3(tw * 0.9f, 0.7f, 0.6f), DecorPalette.Matte(new Color(0.30f, 0.14f, 0.18f)));
                        m.Box(new Vector3(0f, 0.86f, 0f), new Vector3(2.0f, 0.3f, 0.62f), DecorPalette.Gloss(FurnitureBuilder.Walnut));
                        m.Box(new Vector3(0f, 1.02f, 0f), new Vector3(1.96f, 0.04f, 0.58f), DecorPalette.Gloss(new Color(0.36f, 0.22f, 0.14f)));
                    }, "casket");
                else
                    b.WorkTable(c.R, table, new Vector3(tw, 0.8f, 0.8f), (m, r) => FurnitureBuilder.Table(m, r, tw, 0.8f, FurnitureBuilder.Walnut), "Table");
            }
            ChairRows(c, front, chapel ? new Color(0.30f, 0.14f, 0.18f) : FurnitureBuilder.Oak, 2.2f);
            if (chapel)
                for (int k = 0; k < 2; k++)
                    if (c.Wall(front, 0.4f, 0.4f, 1.0f, k == 0 ? 0.1f : 0.9f, out RaidBuilding.Place plant))
                        b.Furnish(c.R, plant, new Vector3(0.4f, 1f, 0.4f), false, (m, r) => FurnitureBuilder.DeadPlant(m, r, Vector3.zero), "flowers");
            WallDecor(c, 2);
        }

        // Rows of chairs facing wall `front`, an aisle down the middle, keeping `clear` metres before the front wall.
        private static void ChairRows(RoomCtx c, int front, Color wood, float clear)
        {
            Rect i = c.Inner;
            bool facingZ = front == 0 || front == 2;
            float width = facingZ ? i.width : i.height, depth = facingZ ? i.height : i.width;
            int rows = Mathf.Clamp(Mathf.FloorToInt((depth - clear - 1.6f) / 0.95f), 0, 12);
            int perSide = Mathf.Clamp(Mathf.FloorToInt((width * 0.5f - 0.9f) / 0.55f), 0, 8);
            float yaw = RoomCtx.YawOf(RoomCtx.Opposite(front));
            for (int r = 0; r < rows; r++)
            {
                float d = clear + 0.5f + r * 0.95f;
                for (int s = -1; s <= 1; s += 2)
                    for (int k = 0; k < perSide; k++)
                    {
                        float a = (facingZ ? i.center.x : i.center.y) + s * (0.75f + k * 0.55f);
                        Vector2 q = c.OnWall(front, a, d);
                        var fp = new Rect(q.x - 0.24f, q.y - 0.24f, 0.48f, 0.48f);
                        if (!c.FloorAt(fp, 0.02f)) continue;
                        c.B.WoodChairSeat(c.R, new RaidBuilding.Place(q.x, q.y, yaw), wood);
                    }
            }
        }

        #region Office fittings
        // A filing cabinet: one to search while the room's loot budget lasts, else plain.
        private static void Filing(RoomCtx c, RaidBuilding.Place p)
        {
            if (c.TakeLoot()) c.B.FilingCabinet(c.R, p, 4, Putty);
            else c.B.Furnish(c.R, p, new Vector3(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, Putty), "Filing cabinet");
        }

        private static void SwivelChair(RoomCtx c, RaidBuilding.Place p, Color fabric)
        {
            Transform t = c.B.FixtureBody(c.R, p, new Vector3(0.55f, 1f, 0.55f), false, (m, r) => FurnitureBuilder.OfficeChair(m, r, fabric), "Office chair");
            RaidStoreSwivelSeat.Create(t, c.B.Yaw(p.Yaw));
        }

        private static void Bookcase(RoomCtx c, RaidBuilding.Place p, Color wood)
        {
            RaidBuilding b = c.B;
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.9f, 1.8f, 0.32f), true, (m, r) => FurnitureBuilder.Bookshelf(m, r, wood), "Bookcase");
            StorageOptions o = RaidBuilding.SmallStock(ContainerType.Shelf, "bookcase");
            o.LookLabel = "Browse the shelves";
            o.ExtraActions = LivingReading.AddActions;
            StorageFixture.Create(t, "Bookcase", Interactable.KeyFor("bookcase", t.position), b.At(p, 0f, 1.2f, 0.18f), null, t, b.Rng.Next(), o);
        }

        private static void Copier(RoomCtx c, RaidBuilding.Place p)
        {
            RaidBuilding b = c.B;
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.9f, 1.1f, 0.6f), true, (m, r) => FurnitureBuilder.OfficeCopierBody(m, r), "Photocopier");
            KitchenFrame f = b.FrameOf(p);
            FixturePart lid = KitchenParts.Door(t, "CopierLid", f, FurnitureBuilder.OfficeCopierLid, FurnitureBuilder.OfficeCopierLidHinge, Vector3.forward, Vector3.up, 70f);
            MeshRenderer jam = KitchenParts.PieceAt(t, "JamLamp", f, FurnitureBuilder.OfficeCopierJamLamp, FurnitureBuilder.OfficeCopierJamGlow);
            RaidStoreCopier.Create(t, f.P(FurnitureBuilder.OfficeCopierPanel), lid, jam, b.Rng.Next());
        }

        private static void Vending(RoomCtx c, RaidBuilding.Place p, bool lit)
        {
            RaidBuilding b = c.B;
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.9f, 1.85f, 0.8f), true, (m, r) => FurnitureBuilder.OfficeVendingBody(m, r), "Vending machine");
            KitchenFrame f = b.FrameOf(p);
            int seed = b.Rng.Next();
            var wr = new System.Random(seed);
            var at = new Vector3(-0.1f, 1.1f, 0.37f);
            MeshRenderer glass = KitchenParts.PieceInFrame(t, "VendingGlass", f, at, m => FurnitureBuilder.OfficeVendingWindow(m, wr, lit, false));
            MeshRenderer smashed = KitchenParts.PieceInFrame(t, "VendingSmashed", f, at, m => FurnitureBuilder.OfficeVendingWindow(m, wr, lit, true));
            RaidStoreVending.Create(t, f.P(FurnitureBuilder.OfficeVendingPanel), f.P(FurnitureBuilder.OfficeVendingTray), glass, smashed, lit, seed);
        }
        #endregion
    }
}
