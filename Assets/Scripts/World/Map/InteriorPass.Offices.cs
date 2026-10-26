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
                float len = Mathf.Min(c.Len(back) - 1.6f, use == InteriorUse.PostOffice ? 9f : 6f);
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
                if (use == InteriorUse.Bank && c.Area > 40f) BankFloor(c, back);
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

        // A bank's floor before the tellers' counter: the platform (the officers' desks, chairs for the customers across them)
        // to either side, the stand-up desks to write a cheque at down the middle.
        private static void BankFloor(RoomCtx c, int back)
        {
            RaidBuilding b = c.B;
            bool alongX = back == 0 || back == 2;
            float mid = alongX ? c.Inner.center.x : c.Inner.center.y, half = (alongX ? c.Inner.width : c.Inner.height) * 0.5f;
            float depth = alongX ? c.Inner.height : c.Inner.width;
            float yaw = RoomCtx.YawOf(back);
            Color wood = Woods[c.Rng.Next(Woods.Length)];
            int desks = Mathf.Clamp(Mathf.FloorToInt(c.Area / 50f), 1, 4);
            for (int k = 0; k < desks; k++)
            {
                float along = mid + (k % 2 == 0 ? -1f : 1f) * Mathf.Min(half - 1.6f, 2.5f + (k / 2) * 2.4f);
                Vector2 near = c.OnWall(back, along, Mathf.Min(depth * 0.55f, 5.5f));
                if (!c.Floor(1.6f, 2.3f, yaw, near, 0.5f, out RaidBuilding.Place d)) break;
                bool right = c.Rng.NextDouble() < 0.5;
                TableSet(c, d, new Vector3(1.5f, 0.8f, 0.75f), PlatformChairs, 0.9, FurnitureBuilder.Walnut, "Desk",
                    (m, r) => FurnitureBuilder.Desk(m, r, 1.5f, 0.75f, wood, right));
            }
            for (int k = 0; k < 2; k++)
            {
                Vector2 near = c.OnWall(back, mid, Mathf.Min(depth * 0.5f, 4.5f) + k * 2.2f);
                if (!c.Floor(1.3f, 0.7f, yaw, near, 0.6f, out RaidBuilding.Place s)) break;
                b.Furnish(c.R, s, new Vector3(1.3f, 1.15f, 0.7f), true, (m, r) =>
                {
                    Surf wd = DecorPalette.Gloss(FurnitureBuilder.Walnut), top = DecorPalette.Matte(new Color(0.22f, 0.30f, 0.24f));
                    m.Box(new Vector3(0f, 0.5f, 0f), new Vector3(0.3f, 1.0f, 0.3f), wd);
                    m.Box(new Vector3(0f, 0.04f, 0f), new Vector3(0.7f, 0.08f, 0.5f), wd);
                    m.Box(new Vector3(0f, 1.06f, 0f), new Vector3(1.3f, 0.06f, 0.7f), Quaternion.Euler(6f, 0f, 0f), wd);
                    m.Box(new Vector3(0f, 1.095f, 0f), new Vector3(1.2f, 0.01f, 0.6f), Quaternion.Euler(6f, 0f, 0f), top);
                    for (int j = 0; j < 4; j++)
                        if (r.NextDouble() < 0.6)
                            m.FlatUV(new Vector3(-0.45f + j * 0.3f, 1.112f, MapStyle.Range(r, -0.1f, 0.1f)), 0.12f, 0.2f, MapStyle.Range(r, -20f, 20f),
                                DecorMaterials.Paper(r.Next(3)), new Rect(0f, 0f, 1f, 1f));
                }, "Cheque desk");
            }
        }

        /// <summary>A bank officer's desk: the officer's chair behind it, two for the customers across it.</summary>
        private static readonly Vector3[] PlatformChairs =
        {
            new Vector3(0f, -0.68f, 0f), new Vector3(-0.4f, 0.75f, 180f), new Vector3(0.4f, 0.75f, 180f),
        };

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
            if (!Kitchenette(c) && c.AnyWall(c.Sides(true), 0.72f, 0.70f, 1.72f, -1f, out RaidBuilding.Place fridge, out _))
                b.HomePiece(c.R, fridge, HomeFurnitureKind.Fridge);
            if (c.Area > 30f) LongTables(c, c.DoorSide >= 0 ? RoomCtx.Opposite(c.DoorSide) : 2, Mathf.Clamp(Mathf.FloorToInt(c.Area / 28f), 1, 5));
            else TableAndChairs(c, false);
            if (c.P.Role == RoomRole.DayRoom || c.Area > 60f)
            {
                // The day room's (and a big common room's) sofa and television.
                if (c.AnyWall(c.Sides(false), 2.0f, 0.88f, 0.85f, -1f, out RaidBuilding.Place sofa, out int ss))
                {
                    b.HouseSofa(c.R, sofa, 2.0f, Upholstery[c.Rng.Next(Upholstery.Length)]);
                    if (c.Wall(RoomCtx.Opposite(ss), 1.0f, 0.45f, 1.1f, 0.5f, out RaidBuilding.Place tv)) b.HouseTv(c.R, tv);
                }
            }
            if (c.P.Role != RoomRole.DayRoom && c.AnyWall(c.Sides(false), 0.9f, 0.8f, 1.85f, -1f, out RaidBuilding.Place vend, out _)) Vending(c, vend, c.P.Lit);
            WallDecor(c, 1);
        }

        // Up to `count` long tables with folding chairs down both sides, across the floor (a fellowship hall's suppers, a
        // common room).
        private static void LongTables(RoomCtx c, int front, int count)
        {
            for (int k = 0; k < count; k++)
            {
                if (!c.Floor(2.4f, 2.0f, front == 0 || front == 2 ? 0f : 90f, c.Inner.center, 0.5f, out RaidBuilding.Place longTable)) break;
                TableSet(c, longTable, new Vector3(2.4f, 0.8f, 0.8f), LongChairs, 0.75, FurnitureBuilder.Pine, "Table",
                    (m, r) => FurnitureBuilder.Table(m, r, 2.4f, 0.8f, new Color(0.70f, 0.66f, 0.56f)));
            }
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

        // A library's reading room: the reading tables and the card catalogue in front of the door, the stacks in runs across
        // the room's length behind it with the main aisle down one side, shelving along the walls.
        private static void Stacks(RoomCtx c)
        {
            RaidBuilding b = c.B;
            Rect i = c.Inner;
            int[] order = c.Sides(false);
            for (int k = 0; k < 2 && c.LootLeft > 0; k++)
                if (c.AnyWall(order, 0.9f, 0.32f, 1.8f, -1f, out RaidBuilding.Place shelf, out _)) Bookcase(c, shelf, FurnitureBuilder.Oak);
            bool longZ = i.height >= i.width;
            float u0 = longZ ? i.yMin : i.xMin, u1 = longZ ? i.yMax : i.xMax;
            float v0 = longZ ? i.xMin : i.yMin, v1 = longZ ? i.xMax : i.yMax;
            int door = c.DoorSide;
            bool doorAtEnd = door >= 0 && (longZ ? door == 0 || door == 2 : door == 1 || door == 3);
            // The reading room: at the door's end, or in front of a door in a long wall.
            float r0, r1, span = Mathf.Max(4f, (u1 - u0) * 0.3f);
            if (door < 0) { r0 = u0; r1 = u0 + span; }
            else if (doorAtEnd)
            {
                bool low = longZ ? door == 0 : door == 3;
                r0 = low ? u0 : u1 - span;
                r1 = low ? u0 + span : u1;
            }
            else
            {
                r0 = c.DoorAlong - span * 0.5f;
                r1 = c.DoorAlong + span * 0.5f;
            }
            // The stacks: double-sided runs across the room, the main aisle along the side with the door (or the low one).
            bool aisleLow = door >= 0 && !doorAtEnd ? (longZ ? door == 3 : door == 0) : true;
            float runLen = Mathf.Min(8.1f, (v1 - v0) - 1.8f);
            if (runLen >= 1.8f)
            {
                float vStart = aisleLow ? v1 - 0.25f - runLen : v0 + 0.25f, vMid = vStart + runLen * 0.5f;
                int rows = 0;
                for (float u = u0 + 1.2f; u < u1 - 0.9f && rows < 14; u += 2.1f)
                {
                    if (u + 0.33f > r0 - 0.8f && u - 0.33f < r1 + 0.8f) continue;
                    Rect fp = longZ ? new Rect(vStart, u - 0.33f, runLen, 0.66f) : new Rect(u - 0.33f, vStart, 0.66f, runLen);
                    if (!c.FloorAt(fp, 0.3f)) continue;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var p = longZ ? new RaidBuilding.Place(vMid, u + s * 0.17f, s > 0 ? 0f : 180f) : new RaidBuilding.Place(u + s * 0.17f, vMid, s > 0 ? 90f : 270f);
                        b.Furnish(c.R, p, new Vector3(runLen, 1.8f, 0.32f), true, (m, rr) => BookRow(m, rr, runLen), "Stacks");
                    }
                    rows++;
                }
            }
            // Shelving along the walls, in long runs between the windows and the doors.
            foreach (int side in order)
                for (int k = 0; k < 3; k++)
                {
                    float len = 0f;
                    RaidBuilding.Place run = default;
                    foreach (float want in WallRuns)
                        if (c.Wall(side, want, 0.34f, 1.8f, -1f, out run)) { len = want; break; }
                    if (len <= 0f) break;
                    b.Furnish(c.R, run, new Vector3(len, 1.8f, 0.32f), true, (m, rr) => BookRow(m, rr, len), "Shelving");
                }
            // The reading tables and the catalogue in the reading room.
            float um = (r0 + r1) * 0.5f, vm = (v0 + v1) * 0.5f;
            Vector2 near = longZ ? new Vector2(vm, um) : new Vector2(um, vm);
            for (int k = 0; k < 4; k++)
            {
                if (!c.Floor(1.8f, 0.9f, longZ ? 90f : 0f, near, 0.9f, out RaidBuilding.Place table)) break;
                ReadingTable(c, table);
            }
            if (c.Floor(1.2f, 0.5f, longZ ? 0f : 90f, near, 0.8f, out RaidBuilding.Place cat))
                b.Furnish(c.R, cat, new Vector3(1.2f, 1.1f, 0.5f), true, (m, r) => CardCatalogue(m), "Card catalogue");
            MoreLoot(c, 1, order);
        }

        private static readonly float[] WallRuns = { 5.4f, 3.6f, 2.7f, 1.8f };

        // A reading table 1.8 × 0.9 (along the placement's x) with its chairs either side, one body; a green-shaded lamp on
        // it now and then.
        private static void ReadingTable(RoomCtx c, RaidBuilding.Place p)
        {
            bool lamp = c.Rng.NextDouble() < 0.5;
            TableSet(c, p, new Vector3(1.8f, 0.8f, 0.9f), ReadingChairs, 0.8, FurnitureBuilder.Oak, "Reading table", (m, r) =>
            {
                FurnitureBuilder.Table(m, r, 1.8f, 0.9f, FurnitureBuilder.Oak);
                if (!lamp) return;
                m.Cylinder(new Vector3(0f, 0.76f, 0f), 0.05f, 0.3f, 8, DecorPalette.Metal(FurnitureBuilder.Brass));
                m.Box(new Vector3(0f, 1.08f, 0f), new Vector3(0.36f, 0.08f, 0.16f), DecorPalette.Gloss(new Color(0.12f, 0.36f, 0.20f)));
            });
        }

        /// <summary>Where a table's chairs stand round it (its frame: x, z, and the way the chair faces).</summary>
        private static readonly Vector3[] ReadingChairs =
        {
            new Vector3(-0.45f, 0.72f, 180f), new Vector3(0.45f, 0.72f, 180f), new Vector3(-0.45f, -0.72f, 0f), new Vector3(0.45f, -0.72f, 0f),
        };
        private static readonly Vector3[] SquareChairs =
        {
            new Vector3(0f, -0.62f, 0f), new Vector3(-0.62f, 0f, 90f), new Vector3(0f, 0.62f, 180f), new Vector3(0.62f, 0f, 270f),
        };
        private static readonly Vector3[] LongChairs =
        {
            new Vector3(-0.8f, -0.62f, 0f), new Vector3(0f, -0.62f, 0f), new Vector3(0.8f, -0.62f, 0f),
            new Vector3(-0.8f, 0.62f, 180f), new Vector3(0f, 0.62f, 180f), new Vector3(0.8f, 0.62f, 180f),
        };

        // A table (`size` in the placement's frame, drawn by `top`) with wooden chairs pulled up at `spots` (x, z, facing), each
        // there with chance `keep` and some turned askew: one body, a seat for each chair. A dining room's worth of chairs as
        // objects of their own would be five times the objects.
        private static void TableSet(RoomCtx c, RaidBuilding.Place p, Vector3 size, Vector3[] spots, double keep, Color wood, string name,
            System.Action<RaidMesh, System.Random> top, bool solid = true)
        {
            RaidBuilding b = c.B;
            Vector3 pos = b.W(p.X, 0f, p.Z);
            float yaw = b.Yaw(p.Yaw);
            int n = spots.Length;
            var there = new bool[n];
            var askew = new float[n];
            for (int k = 0; k < n; k++)
            {
                there[k] = c.Rng.NextDouble() < keep;
                askew[k] = c.Rng.NextDouble() < 0.3 ? MapStyle.Range(c.Rng, -25f, 25f) : 0f;
            }
            Transform t = b.FixtureBody(c.R, p, size, solid, (m, r) =>
            {
                top?.Invoke(m, r);
                for (int k = 0; k < n; k++)
                {
                    if (!there[k]) continue;
                    m.SetFrame(pos + Quaternion.Euler(0f, yaw, 0f) * new Vector3(spots[k].x, 0f, spots[k].y), yaw + spots[k].z + askew[k]);
                    FurnitureBuilder.WoodChair(m, r, wood);
                }
                m.SetFrame(pos, yaw);
            }, name);
            for (int k = 0; k < n; k++)
            {
                if (!there[k]) continue;
                float seatYaw = yaw + spots[k].z + askew[k];
                Vector3 off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(spots[k].x, 0f, spots[k].y);
                Vector3 seat = Quaternion.Euler(0f, -seatYaw, 0f) * off + new Vector3(0f, CloneZConfig.ChairSeatHeight, CloneZConfig.ChairSeatBack);
                SeatFixture.Create(t, "Chair", seat, seatYaw, CloneZConfig.ChairComfort, "Sit down", "Stand up", false);
            }
        }

        // The card catalogue: a cabinet of little drawers with brass pulls, on legs.
        private static void CardCatalogue(RaidMesh m)
        {
            Surf wood = DecorPalette.Gloss(FurnitureBuilder.Oak), dark = DecorPalette.Matte(FurnitureBuilder.Oak * 0.7f), brass = DecorPalette.Metal(FurnitureBuilder.Brass);
            for (int k = -1; k <= 1; k += 2)
                for (int j = -1; j <= 1; j += 2)
                    m.Box(new Vector3(k * 0.55f, 0.15f, j * 0.2f), new Vector3(0.05f, 0.3f, 0.05f), wood);
            m.Box(new Vector3(0f, 0.7f, 0f), new Vector3(1.2f, 0.8f, 0.48f), wood);
            m.Box(new Vector3(0f, 1.11f, 0f), new Vector3(1.24f, 0.03f, 0.5f), wood);
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 6; col++)
                {
                    var at = new Vector3(-0.5f + col * 0.2f, 0.38f + row * 0.15f, 0.242f);
                    m.Box(at, new Vector3(0.18f, 0.13f, 0.01f), dark);
                    m.Box(at + new Vector3(0f, -0.02f, 0.008f), new Vector3(0.05f, 0.015f, 0.012f), brass);
                }
        }

        // A meeting hall, a council chamber, a funeral home's chapel: rows of chairs facing the table (or the casket) at the far
        // end.
        private static void Meeting(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int front = EndFront(c);
            bool chapel = c.P.Role == RoomRole.Chapel;
            bool fellowship = c.Plan.Use == InteriorUse.Church && !chapel;
            float len = c.Len(front);
            if (fellowship)
            {
                // Long tables with folding chairs down both sides, for the suppers.
                LongTables(c, front, 4);
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

        // Rows of chairs facing wall `front`, an aisle down the middle, keeping `clear` metres before the front wall: each half
        // row one body (its chairs drawn on it, a seat each), a little askew.
        private static void ChairRows(RoomCtx c, int front, Color wood, float clear)
        {
            Rect i = c.Inner;
            bool facingZ = front == 0 || front == 2;
            float width = facingZ ? i.width : i.height, depth = facingZ ? i.height : i.width;
            int rows = Mathf.Clamp(Mathf.FloorToInt((depth - clear - 1.6f) / 0.95f), 0, 14);
            int perSide = Mathf.Clamp(Mathf.FloorToInt((width * 0.5f - 0.9f) / 0.55f), 0, 8);
            if (perSide <= 0) return;
            float yaw = RoomCtx.YawOf(RoomCtx.Opposite(front)), rowLen = perSide * 0.55f;
            var spots = new Vector3[perSide];
            for (int k = 0; k < perSide; k++) spots[k] = new Vector3(-rowLen * 0.5f + 0.275f + k * 0.55f, 0f, 0f);
            for (int r = 0; r < rows; r++)
            {
                float d = clear + 0.5f + r * 0.95f;
                for (int s = -1; s <= 1; s += 2)
                {
                    float a = (facingZ ? i.center.x : i.center.y) + s * (0.475f + rowLen * 0.5f);
                    Vector2 q = c.OnWall(front, a, d);
                    var fp = facingZ ? new Rect(a - rowLen * 0.5f, q.y - 0.24f, rowLen, 0.48f) : new Rect(q.x - 0.24f, a - rowLen * 0.5f, 0.48f, rowLen);
                    if (!c.FloorAt(fp, 0.02f)) continue;
                    TableSet(c, new RaidBuilding.Place(q.x, q.y, yaw), new Vector3(rowLen, 0.9f, 0.45f), spots, 0.9, wood, "Chairs", null, false);
                }
            }
        }

        // The wall a hall faces: an end of its length, across from the door or, with the door in a long side, the end farther
        // from it (a squarish room faces across from its door).
        private static int EndFront(RoomCtx c)
        {
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            Rect i = c.Inner;
            bool longZ = i.height >= i.width;
            bool doorInEnd = longZ ? door == 0 || door == 2 : door == 1 || door == 3;
            bool squarish = Mathf.Max(i.width, i.height) < 1.15f * Mathf.Min(i.width, i.height);
            if (doorInEnd || squarish || c.DoorSide < 0) return RoomCtx.Opposite(door);
            if (longZ) return c.DoorAlong > i.center.y ? 0 : 2;
            return c.DoorAlong > i.center.x ? 3 : 1;
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
            if (!c.TakeLoot())
            {
                b.Furnish(c.R, p, new Vector3(0.9f, 1.8f, 0.32f), true, (m, r) => FurnitureBuilder.Bookshelf(m, r, wood), "Bookcase");
                return;
            }
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.9f, 1.8f, 0.32f), true, (m, r) => FurnitureBuilder.Bookshelf(m, r, wood), "Bookcase");
            StorageOptions o = RaidBuilding.SmallStock(ContainerType.Shelf, "bookcase");
            o.LookLabel = "Browse the shelves";
            o.ExtraActions = LivingReading.AddActions;
            StorageFixture.Create(t, "Bookcase", Interactable.KeyFor("bookcase", t.position), b.At(p, 0f, 1.2f, 0.18f), null, t, b.Rng.Next(), o);
        }

        // One face of a run of library shelving `len` long (the piece's frame, front +Z): the back, the uprights every 0.9 m,
        // five shelves, and on four of them the books, a bay at a time (the stock texture's spines: one quad a bay, not a
        // box a book).
        private static void BookRow(RaidMesh m, System.Random r, float len)
        {
            Surf wood = DecorPalette.Matte(FurnitureBuilder.Oak), books = DecorMaterials.Stock(1);
            m.Box(new Vector3(0f, 0.9f, -0.14f), new Vector3(len, 1.8f, 0.04f), wood);
            int bays = Mathf.Max(1, Mathf.RoundToInt(len / 0.9f));
            float bay = len / bays;
            for (int k = 0; k <= bays; k++) m.Box(new Vector3(-len * 0.5f + bay * k, 0.9f, 0f), new Vector3(0.03f, 1.8f, 0.32f), wood);
            for (int s = 0; s < 5; s++) m.Box(new Vector3(0f, 0.05f + s * 0.42f, 0f), new Vector3(len, 0.025f, 0.32f), wood);
            for (int s = 0; s < 4; s++)
                for (int k = 0; k < bays; k++)
                {
                    if (r.NextDouble() < 0.08) continue;
                    float y = 0.0625f + s * 0.42f, h = 0.3f;
                    m.Upright(new Vector3(-len * 0.5f + bay * (k + 0.5f), y + h * 0.5f, 0.09f), Vector3.forward, bay - 0.04f, h, books,
                        new Rect((float)r.NextDouble() * 0.5f, 0f, 0.5f, 0.95f));
                }
        }

        private static void Copier(RoomCtx c, RaidBuilding.Place p)
        {
            RaidBuilding b = c.B;
            Transform t = b.FixtureBody(c.R, p, new Vector3(0.9f, 1.1f, 0.6f), true, (m, r) => FurnitureBuilder.OfficeCopierBody(m, r), "Photocopier");
            KitchenFrame f = b.FrameOf(p);
            FixturePart lid = KitchenParts.Door(t, "CopierLid", f, FurnitureBuilder.OfficeCopierLid, FurnitureBuilder.OfficeCopierLidHinge, Vector3.forward, Vector3.up, 70f);
            MeshRenderer jam = KitchenParts.PieceAt(t, "JamLamp", f, FurnitureBuilder.OfficeCopierJamLamp, FurnitureBuilder.OfficeCopierJamGlow);
            // Additional visual detail parts for the copier
            MeshRenderer glass = KitchenParts.PieceAt(t, "ScannerGlass", f, new Vector3(0f, 0.55f, 0.285f),
                m => {
                    m.Box(new Vector3(0f, 0.55f, 0.285f), new Vector3(0.8f, 0.9f, 0.02f),
                        DecorPalette.Glass(new Color(0.15f, 0.15f, 0.17f)));
                });
            MeshRenderer tray = KitchenParts.PieceAt(t, "PaperTray", f, new Vector3(0f, 0.15f, 0.2f),
                m => {
                    m.Box(new Vector3(0f, 0.15f, 0.2f), new Vector3(0.7f, 0.1f, 0.3f),
                        DecorPalette.Matte(FurnitureBuilder.Walnut));
                });
            MeshRenderer cord = KitchenParts.PieceAt(t, "PowerCord", f, new Vector3(0.3f, -0.55f, -0.2f),
                m => {
                    m.Cylinder(new Vector3(0.3f, -0.55f, -0.2f), 0.015f, 0.25f, 8,
                        DecorPalette.Metal(FurnitureBuilder.Brass));
                });
            MeshRenderer led = KitchenParts.PieceAt(t, "CopierLED", f, new Vector3(-0.28f, 0.6f, 0.31f),
                m => {
                    m.Sphere(new Vector3(-0.28f, 0.6f, 0.31f), 0.02f,
                        DecorPalette.Glow(new Color(1f, 0.6f, 0.2f)));
                });
            MeshRenderer label = KitchenParts.PieceAt(t, "CopierLabel", f, new Vector3(-0.15f, 0.5f, 0.311f),
                m => {
                    // Random rotation and paper material for a hand‑tacked look
                    var rnd = b.Rng;
                    m.FlatUV(new Vector3(-0.15f, 0.5f, 0.311f), 0.25f, 0.08f,
                        MapStyle.Range(rnd, -10f, 10f),
                        DecorMaterials.Paper(rnd.Next(3)), new Rect(0f,0f,1f,1f));
                });
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
