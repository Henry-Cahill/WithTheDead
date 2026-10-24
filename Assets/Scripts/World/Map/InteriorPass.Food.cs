using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Diners, cafés and bars: booths along the windows, tables and chairs on the floor, the counter with its stools; the
    // kitchen behind with its steel counters, ranges and fridges; the bar's bottles, its stools and its pool table.
    internal static partial class InteriorPass
    {
        private static readonly Color[] Vinyls = { new Color(0.55f, 0.12f, 0.10f), new Color(0.16f, 0.30f, 0.34f), new Color(0.50f, 0.36f, 0.14f), new Color(0.22f, 0.22f, 0.24f) };

        private static void Eatery(RoomCtx c)
        {
            RaidBuilding b = c.B;
            System.Random rng = c.Rng;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            int back = RoomCtx.Opposite(door);
            Color vinyl = Vinyls[rng.Next(Vinyls.Length)];
            Color wood = Woods[rng.Next(Woods.Length)];
            // The counter with its stools along the back of a room deep enough, the till at its end.
            float across = door == 0 || door == 2 ? c.Depth : c.Width;
            if (across >= 5.5f) ServiceCounter(c, back, vinyl, false);
            // Booths under the windows, along the front and the sides.
            int booths = 0;
            foreach (int side in new[] { door, (door + 1) & 3, (door + 3) & 3 })
            {
                if (c.Panes[side].Count == 0 && side != door) continue;
                for (int k = 0; k < 6; k++)
                {
                    if (!c.Wall(side, 1.95f, 1.25f, 0.95f, -1f, out RaidBuilding.Place p)) break;
                    Booth(c, p, vinyl);
                    booths++;
                }
            }
            // Tables of four on the floor.
            Rect i = c.Inner;
            for (float z = i.yMin + 1.4f; z < i.yMax - 1.2f; z += 2.1f)
                for (float x = i.xMin + 1.4f; x < i.xMax - 1.2f; x += 2.1f)
                {
                    if (!c.FloorAt(new Rect(x - 0.4f, z - 0.4f, 0.8f, 0.8f), 0.55f)) continue;
                    var table = new RaidBuilding.Place(x, z, 0f);
                    b.WorkTable(c.R, table, new Vector3(0.8f, 0.8f, 0.8f), (m, r) => FurnitureBuilder.Table(m, r, 0.8f, 0.8f, wood), "Table", null, true);
                    for (int s = 0; s < 4; s++)
                    {
                        if (rng.NextDouble() < 0.2) continue;
                        float yaw = s * 90f;
                        Vector3 off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -0.62f);
                        var chair = new RaidBuilding.Place(x + off.x, z + off.z, yaw + (rng.NextDouble() < 0.3 ? MapStyle.Range(rng, -30f, 30f) : 0f));
                        var fp = new Rect(chair.X - 0.22f, chair.Z - 0.22f, 0.44f, 0.44f);
                        if (!c.Clear(fp) || !c.Inner.Overlaps(fp)) continue;
                        c.Taken.Add(fp);
                        b.WoodChairSeat(c.R, chair, wood);
                    }
                }
            if (c.AnyWall(c.Sides(true), 0.9f, 0.8f, 1.85f, -1f, out RaidBuilding.Place vend, out _) && rng.NextDouble() < 0.4) Vending(c, vend, c.P.Lit);
            WallDecor(c, 3);
            WallDecor(c, 4);
        }

        // A booth against a wall: its table end-on to the wall, a bench either side.
        private static void Booth(RoomCtx c, RaidBuilding.Place p, Color vinyl)
        {
            RaidBuilding b = c.B;
            Surf seat = DecorPalette.Gloss(vinyl), top = DecorPalette.Gloss(new Color(0.78f, 0.74f, 0.64f)), chrome = DecorPalette.Metal(FurnitureBuilder.Chrome);
            Transform t = b.FixtureBody(c.R, p, new Vector3(1.95f, 0.95f, 1.25f), true, (m, r) =>
            {
                m.Box(new Vector3(0f, 0.74f, 0f), new Vector3(0.75f, 0.04f, 1.2f), top);
                m.Cylinder(new Vector3(0f, 0f, 0f), 0.05f, 0.72f, 8, chrome);
                m.Box(new Vector3(0f, 0.01f, 0f), new Vector3(0.4f, 0.02f, 0.4f), chrome);
                for (int s = -1; s <= 1; s += 2)
                {
                    m.Box(new Vector3(s * 0.66f, 0.22f, 0f), new Vector3(0.5f, 0.44f, 1.22f), DecorPalette.Matte(vinyl * 0.6f));
                    m.Box(new Vector3(s * 0.64f, 0.47f, 0f), new Vector3(0.52f, 0.1f, 1.2f), seat);
                    m.Box(new Vector3(s * 0.9f, 0.71f, 0f), new Vector3(0.12f, 0.48f, 1.2f), seat);
                }
            }, "Booth");
            float wy = b.Yaw(p.Yaw);
            for (int s = -1; s <= 1; s += 2)
            {
                float turn = s < 0 ? 90f : -90f;
                Vector3 local = Quaternion.Euler(0f, -turn, 0f) * new Vector3(s * 0.62f, 0.52f, s < 0 ? -0.25f : 0.25f);
                SeatFixture.Create(t, "Booth", local, wy + turn, CloneZConfig.CivicSofaComfort, "Slide into the booth", "Get up", false);
            }
        }

        // The counter along wall `side`, out from it with room behind for whoever served, stools along its front; the till at
        // one end. A bar's has its bottles on shelves behind.
        private static void ServiceCounter(RoomCtx c, int side, Color vinyl, bool bar)
        {
            RaidBuilding b = c.B;
            c.FreeRun(side, out float a, out float e);
            float len = Mathf.Min(e - a - 1.2f, 7f);
            if (len < 2.4f) return;
            float t = (a + e) * 0.5f + (c.Rng.NextDouble() < 0.5 ? -1f : 1f) * Mathf.Max(0f, (e - a - len) * 0.5f - 0.6f);
            // The back counter (or the bottle shelves) against the wall, the serving counter 1.1 m out.
            if (!c.WallAt(side, t, len, 0.6f, bar ? 1.9f : 1.0f, out RaidBuilding.Place backBar)) return;
            Vector2 q = c.OnWall(side, t, 0.6f + 1.0f + 0.36f);
            var front = new Rect(q.x - (side == 0 || side == 2 ? len * 0.5f : 0.36f), q.y - (side == 0 || side == 2 ? 0.36f : len * 0.5f),
                side == 0 || side == 2 ? len : 0.72f, side == 0 || side == 2 ? 0.72f : len);
            Rect behind = c.Foot(side, t, len, 0.6f + 1.0f);
            c.Taken.Add(behind);
            if (bar)
                b.Shelving(c.R, backBar, new Vector3(len, 1.9f, 0.6f), 3, 3, new Color(0.24f, 0.16f, 0.10f), "Bottle shelves",
                    RaidBuilding.MainStock(c.NextLoot() ?? ContainerType.Pantry, "bottle shelves"));
            else
                b.SteelCounter(c.R, backBar, len, len > 3f ? (float?)(-len * 0.5f + 0.6f) : null, c.NextLoot() ?? ContainerType.Pantry, "Back counter",
                    (m, r) => { for (int k = 0; k < 3; k++) m.Cylinder(new Vector3(-len * 0.3f + k * 0.3f, 0.96f, 0f), 0.08f, 0.12f, 8, DecorPalette.Metal(FurnitureBuilder.Chrome)); });
            if (!c.FloorAt(front, 0f)) return;
            var counter = new RaidBuilding.Place(q.x, q.y, RoomCtx.YawOf(side) + 180f);
            Checkout(c, counter, len);
            // Stools along its front.
            int stools = Mathf.FloorToInt((len - 0.4f) / 0.7f);
            for (int k = 0; k < stools; k++)
            {
                float along = t - len * 0.5f + 0.55f + k * 0.7f;
                Vector2 s = c.OnWall(side, along, 0.6f + 1.0f + 0.72f + 0.42f);
                var fp = new Rect(s.x - 0.2f, s.y - 0.2f, 0.4f, 0.4f);
                if (!c.Clear(fp)) continue;
                c.Taken.Add(fp);
                b.StoolSeat(c.R, new RaidBuilding.Place(s.x, s.y, RoomCtx.YawOf(side) + 180f), vinyl);
            }
        }

        private static void CommercialKitchen(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(true);
            bool sink = false;
            // Steel counters round the walls with the sink in one; the ranges (working stoves) between; the walk-in's worth of
            // fridges.
            foreach (int side in order)
            {
                c.FreeRun(side, out float a, out float e);
                float run = e - a - 0.1f;
                if (run < 1.2f) continue;
                float t = a + 0.05f;
                for (int k = 0; k < 2 && run - (t - a) >= 0.8f; k++)
                {
                    if (c.WallAt(side, t + 0.38f, 0.76f, 0.62f, 1.08f, out RaidBuilding.Place range))
                    {
                        b.HouseStove(c.R, range);
                        t += 0.8f;
                    }
                    else break;
                }
                float len = Mathf.Min(e - t - 0.05f, 3.2f);
                if (len >= 1.0f && c.WallAt(side, t + len * 0.5f, len, 0.65f, 1.1f, out RaidBuilding.Place counter))
                {
                    float? sx = !sink && len >= 1.6f ? (float?)(len * 0.5f - 0.5f) : null;
                    sink |= sx.HasValue;
                    ContainerType loot = c.NextLoot() ?? ContainerType.Pantry;
                    b.SteelCounter(c.R, counter, len, sx, loot, "Steel counter", (m, r) =>
                    {
                        for (int k = 0; k < 2; k++)
                            m.Cylinder(new Vector3(-len * 0.3f + k * 0.35f, 0.96f, 0.05f), 0.13f, 0.2f, 10, DecorPalette.Metal(FurnitureBuilder.Steel));
                    });
                }
            }
            for (int k = 0; k < (c.Area > 18f ? 2 : 1); k++)
                if (c.AnyWall(order, 0.76f, 0.72f, 1.75f, -1f, out RaidBuilding.Place fridge, out _))
                    b.WorkFridge(c.R, fridge, FurnitureBuilder.Steel, ContainerType.Fridge, "Fridge", false);
            if (c.Area > 12f && c.Floor(1.6f, 0.8f, c.Inner.width >= c.Inner.height ? 0f : 90f, c.Inner.center, 0.9f, out RaidBuilding.Place prep))
                b.WorkTable(c.R, prep, new Vector3(1.6f, 0.95f, 0.8f), (m, r) =>
                {
                    m.Box(new Vector3(0f, 0.9f, 0f), new Vector3(1.6f, 0.04f, 0.8f), DecorPalette.Metal(FurnitureBuilder.Steel));
                    m.Box(new Vector3(0f, 0.25f, 0f), new Vector3(1.5f, 0.02f, 0.7f), DecorPalette.Metal(FurnitureBuilder.Steel));
                    for (int k = 0; k < 4; k++)
                        m.Box(new Vector3((k & 1) == 0 ? -0.76f : 0.76f, 0.45f, (k & 2) == 0 ? -0.36f : 0.36f), new Vector3(0.04f, 0.9f, 0.04f), DecorPalette.Metal(FurnitureBuilder.Steel));
                }, "Prep table", RaidBuilding.SmallStock(ContainerType.Pantry, "prep table"));
            MoreLoot(c, LootCount(c, 0.05f, 2), order);
            Clutter(c, 1);
        }

        private static void Bar(RoomCtx c)
        {
            RaidBuilding b = c.B;
            System.Random rng = c.Rng;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            Color vinyl = Vinyls[rng.Next(Vinyls.Length)];
            Color wood = FurnitureBuilder.Walnut;
            // The bar along a side wall, the pool table in the middle if there's room round it, tables by the windows.
            int[] order = c.Sides(false);
            int barSide = order[0] == door ? order[1] : order[0];
            ServiceCounter(c, barSide, vinyl, true);
            if (c.Floor(2.3f, 1.3f, c.Inner.width >= c.Inner.height ? 0f : 90f, c.Inner.center, 1.2f, out RaidBuilding.Place pool))
                b.Furnish(c.R, pool, new Vector3(2.3f, 0.85f, 1.3f), true, (m, r) =>
                {
                    Surf rail = DecorPalette.Gloss(new Color(0.28f, 0.16f, 0.08f)), felt = DecorPalette.Matte(new Color(0.10f, 0.36f, 0.20f));
                    m.Box(new Vector3(0f, 0.78f, 0f), new Vector3(2.1f, 0.04f, 1.1f), felt);
                    m.Box(new Vector3(0f, 0.8f, -0.6f), new Vector3(2.3f, 0.1f, 0.1f), rail);
                    m.Box(new Vector3(0f, 0.8f, 0.6f), new Vector3(2.3f, 0.1f, 0.1f), rail);
                    m.Box(new Vector3(-1.1f, 0.8f, 0f), new Vector3(0.1f, 0.1f, 1.1f), rail);
                    m.Box(new Vector3(1.1f, 0.8f, 0f), new Vector3(0.1f, 0.1f, 1.1f), rail);
                    m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(1.9f, 0.6f, 0.9f), rail);
                    for (int k = 0; k < 6; k++)
                        m.Sphere(new Vector3(MapStyle.Range(r, -0.9f, 0.9f), 0.828f, MapStyle.Range(r, -0.45f, 0.45f)), 0.028f,
                            DecorPalette.Gloss(Color.HSVToRGB((float)r.NextDouble(), 0.7f, 0.8f)));
                    m.Rod(new Vector3(-0.6f, 0.83f, 0.2f), new Vector3(0.7f, 0.83f, 0.35f), 0.02f, DecorPalette.Matte(new Color(0.62f, 0.46f, 0.28f)));
                }, "pool table");
            for (int k = 0; k < 4; k++)
            {
                if (!c.Floor(0.7f, 0.7f, 0f, c.OnWall(door, door == 0 || door == 2 ? c.Inner.center.x : c.Inner.center.y, 1.2f), 0.6f, out RaidBuilding.Place table)) break;
                b.WorkTable(c.R, table, new Vector3(0.7f, 0.8f, 0.7f), (m, r) => FurnitureBuilder.Table(m, r, 0.7f, 0.7f, wood), "Table", null, true);
                for (int s = 0; s < 2; s++)
                {
                    float yaw = s * 180f + 90f;
                    Vector3 off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -0.55f);
                    var fp = new Rect(table.X + off.x - 0.2f, table.Z + off.z - 0.2f, 0.4f, 0.4f);
                    if (!c.Clear(fp)) continue;
                    c.Taken.Add(fp);
                    b.StoolSeat(c.R, new RaidBuilding.Place(table.X + off.x, table.Z + off.z, yaw), vinyl);
                }
            }
            WallDecor(c, 3);
            WallDecor(c, 4);
        }
    }
}
