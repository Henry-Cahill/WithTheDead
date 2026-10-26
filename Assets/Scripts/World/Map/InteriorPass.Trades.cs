using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Warehouses, workshops, barns and greenhouses: racking in rows with the aisles between, pallets by the door; the
    // workshop's benches, sawhorses and drums; the barn's stalls and hay; the greenhouse's potting benches.
    internal static partial class InteriorPass
    {
        private static readonly Color RackOrange = new Color(0.62f, 0.32f, 0.10f), RackBlue = new Color(0.16f, 0.26f, 0.44f);
        private static readonly Surf Metal = DecorPalette.Matte(new Color(0.70f, 0.70f, 0.70f)); // brushed‑metal look for fixtures

        private static void TryPlaceExtinguisher(RoomCtx c)
        {
            // try a few random walls; stop after one successful placement
            for (int i = 0; i < 4; i++)
            {
                int side = c.Rng.Next(4);
                if (!c.AnyWall(new[] { side }, 0.4f, 0.85f, 0.2f, -1f, out RaidBuilding.Place p, out _)) continue;
                var b = c.B; // building reference
                b.Furnish(c.R, p, new Vector3(0.2f, 0.7f, 0.15f), true, (m, r) =>
                {
                    // red cylinder body
                    m.Cylinder(new Vector3(0f, 0.35f, 0f), 0.07f, 0.7f, 12, DecorPalette.Matte(new Color(0.55f, 0.10f, 0.10f)));
                    // metal cap
                    m.Box(new Vector3(0f, 0.71f, 0f), new Vector3(0.12f, 0.05f, 0.12f), Metal);
                }, "extinguisher");
                break;
            }
        }

        private static void AddIndustrialLight(RoomCtx c, RaidBuilding.Place p)
        {
            // a small metal cage and a glowing bulb (emissive colour is approximated with a bright matte)
            var light = DecorPalette.Matte(new Color(0.95f, 0.85f, 0.60f));
            var b = c.B; // building reference
            b.Furnish(c.R, p, new Vector3(0.3f, 0.1f, 0.3f), true, (m, r) =>
            {
                // metal cage
                m.Box(new Vector3(0f, 0f, 0f), new Vector3(0.28f, 0.18f, 0.28f), Metal);
                // bulb – a small yellow cube to simulate a filament
                m.Box(new Vector3(0f, 0.11f, 0f), new Vector3(0.12f, 0.04f, 0.12f), light);
            }, "industrial light");
        }        private static void Warehouse(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            Rect i = c.Inner;
            // Racking in rows running back from the door end, a forklift aisle between; no more than twenty bays of it.
            bool rowsAlongZ = door == 0 || door == 2;
            float span = rowsAlongZ ? i.width : i.height, run = rowsAlongZ ? i.height : i.width;
            float clearFront = Mathf.Min(4f, run * 0.25f), clearBack = 0.9f;
            float usable = run - clearFront - clearBack;
            Color frame = c.Rng.NextDouble() < 0.5 ? RackOrange : RackBlue;
            int rows = Mathf.Clamp(Mathf.FloorToInt((span - 1.2f) / 3.3f), 0, 8);
            int budget = 20;
            for (int r = 0; r < rows && usable >= 2.4f; r++)
            {
                float across = (rowsAlongZ ? i.xMin : i.yMin) + 1.2f + 0.45f + r * 3.3f;
                if (across + 0.45f > (rowsAlongZ ? i.xMax : i.yMax) - 0.8f) break;
                int units = Mathf.Min(budget, Mathf.FloorToInt(usable / 3.6f));
                if (units <= 0) break;
                float len = Mathf.Min(3.6f, usable / units);
                // Correct front/back clearance: both north (0) and south (2) doors need clearFront space.
                float from = (rowsAlongZ ? i.yMin : i.xMin) + (door == 0 || door == 2 ? clearFront : clearBack);
                for (int u = 0; u < units; u++)
                {
                    float a = from + u * len + len * 0.5f;
                    var fp = rowsAlongZ ? new Rect(across - 0.45f, a - len * 0.5f + 0.05f, 0.9f, len - 0.1f) : new Rect(a - len * 0.5f + 0.05f, across - 0.45f, len - 0.1f, 0.9f);
                    if (!c.FloorAt(fp, 0.3f)) continue;
                    var p = rowsAlongZ ? new RaidBuilding.Place(across, a, 90f) : new RaidBuilding.Place(a, across, 0f);
                    ContainerType loot = c.NextLoot() ?? ContainerType.Crate;
                    StorageOptions o = c.Rng.NextDouble() < 0.3 ? RaidBuilding.MainStock(loot, "racking") : RaidBuilding.SpareStock(loot, "racking");
                    ShelvingAt(c, p, new Vector3(len - 0.1f, 2.6f, 0.9f), 4, 2, frame, "Racking", o);
                    budget--;
                }
            }
            // ---- add industrial lighting and safety props ----
            int lightCount = Mathf.Max(1, Mathf.FloorToInt(c.Area / 30f)); // roughly one light per 30 m²
            float spacing = (c.Inner.width - 2f) / (lightCount - 1); // leave 1 m margin each side
            for (int li = 0; li < lightCount; li++)
            {
                float x = c.Inner.xMin + 1f + li * spacing;
                // lights hang from the ceiling, roughly 2.4 m high, centered in the aisle
                var place = new RaidBuilding.Place(x, c.Inner.center.y, 0f);
                AddIndustrialLight(c, place);
            }
            // place up to two fire extinguishers on random walls
            for (int e = 0; e < 2; e++) TryPlaceExtinguisher(c);
            // Pallets and stacks of boxes by the door.

            for (int k = 0; k < Mathf.Clamp(Mathf.FloorToInt(c.Area / 60f), 1, 6); k++)
            {
                Vector2 near = c.OnWall(door, (door == 0 || door == 2 ? i.center.x : i.center.y) + MapStyle.Range(c.Rng, -span * 0.4f, span * 0.4f), 2.2f);
                if (!c.Floor(1.2f, 1.0f, 0f, near, 0.4f, out RaidBuilding.Place pallet)) break;
                bool loaded = c.Rng.NextDouble() < 0.6;
                if (loaded && c.Rng.NextDouble() < 0.5 && c.TakeLoot())
                    b.StackedCrate(c.R, pallet, new Vector3(1.2f, 1.4f, 1.0f), new Vector3(1.1f, 0.8f, 0.95f), new Vector3(0.8f, 0.55f, 0.7f), 0.05f,
                        MapStyle.Range(c.Rng, -10f, 10f), false, c.NextLoot() ?? ContainerType.Crate, "Crate", false);
                else
                    b.Furnish(c.R, pallet, new Vector3(1.2f, loaded ? 1.2f : 0.15f, 1.0f), loaded, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, loaded), "pallet");
            }
            Clutter(c, Mathf.Clamp(Mathf.FloorToInt(c.Area / 40f), 1, 5));
        }

        private static void Workshop(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int[] order = c.Sides(true);
            LootPiece(c, ContainerType.ToolBench, order);
            if (c.Area > 25f) LootPiece(c, ContainerType.ToolBench, order);
            MoreLoot(c, LootCount(c, 0.06f, 3), c.Sides(false));
            // Sawhorses with a plank across, a generator or a compressor, drums and tyres.
            if (c.Area > 12f && c.Floor(1.4f, 0.8f, c.Rng.NextDouble() < 0.5 ? 0f : 90f, c.Inner.center, 0.5f, out RaidBuilding.Place horses))
                b.Furnish(c.R, horses, new Vector3(1.4f, 0.8f, 0.8f), true, (m, r) =>
                {
                    Surf plank = DecorPalette.Matte(new Color(0.66f, 0.52f, 0.34f));
                    m.Box(new Vector3(-0.5f, 0.66f, 0f), new Vector3(0.08f, 0.08f, 0.75f), plank);
                    m.Box(new Vector3(0.5f, 0.66f, 0f), new Vector3(0.08f, 0.08f, 0.75f), plank);
                    for (int k = 0; k < 4; k++)
                        m.Box(new Vector3((k & 1) == 0 ? -0.5f : 0.5f, 0.32f, (k & 2) == 0 ? -0.3f : 0.3f), new Vector3(0.05f, 0.66f, 0.05f), plank);
                    m.Box(new Vector3(0f, 0.72f, 0.05f), new Vector3(1.4f, 0.03f, 0.3f), plank);
                }, "sawhorses");
            if (c.Rng.NextDouble() < 0.5 && c.AnyWall(c.Sides(false), 0.9f, 0.6f, 0.7f, -1f, out RaidBuilding.Place gen, out _))
                b.Furnish(c.R, gen, new Vector3(0.9f, 0.7f, 0.6f), true, (m, r) => DecorProps.Generator(m, r), "generator");
            if (c.AnyWall(c.Sides(false), 0.7f, 0.7f, 0.9f, -1f, out RaidBuilding.Place tyres, out _))
            {
                int n = 2 + c.Rng.Next(3);
                b.Furnish(c.R, tyres, new Vector3(0.7f, 0.25f * n, 0.7f), true, (m, r) => FurnitureBuilder.TyreStack(m, r, Vector3.zero, n), "tyres");
            }
            Clutter(c, 2);
            WallDecor(c, 4);
        }

        private static void Barn(RoomCtx c)
        {
            RaidBuilding b = c.B;
            int door = c.DoorSide >= 0 ? c.DoorSide : 0;
            Rect i = c.Inner;
            Surf boards = DecorPalette.Matte(new Color(0.42f, 0.30f, 0.20f)), hay = DecorPalette.Matte(new Color(0.78f, 0.66f, 0.36f));
            var metal = DecorPalette.Matte(new Color(0.70f, 0.70f, 0.70f)); // brushed‑metal look for hinges, latch, nails
            // Stalls along one long side: plank partitions out from the wall, a gate stood open.
            int stallSide = (door + 1) & 3;
            if (c.Len((door + 3) & 3) > c.Len(stallSide)) stallSide = (door + 3) & 3;
            float across = stallSide == 0 || stallSide == 2 ? c.Depth : c.Width;
            if (across >= 6f && c.Len(stallSide) >= 6f)
            {
                c.Span(stallSide, out float lo, out float hi);
                int n = Mathf.Clamp(Mathf.FloorToInt((hi - lo - 1f) / 3f), 1, 8);
                // Fixed off‑by‑one: generate exactly one partition per stall (n partitions) and space them evenly.
                float pitch = n > 1 ? (hi - lo - 1f) / (n - 1) : 0f; // spacing so first and last stalls sit at lo+0.5 and hi‑0.5
                for (int k = 0; k < n; k++)
                {
                    float t = lo + 0.5f + k * pitch;
                    Vector2 q = c.OnWall(stallSide, t, 1.4f);
                    var fp = stallSide == 0 || stallSide == 2 ? new Rect(t - 0.05f, q.y - 1.4f, 0.1f, 2.8f) : new Rect(q.x - 1.4f, t - 0.05f, 2.8f, 0.1f);
                    if (!c.Clear(fp)) continue;
                    c.Taken.Add(fp);
                    b.Furnish(c.R, new RaidBuilding.Place(q.x, q.y, RoomCtx.YawOf(stallSide) + 90f), new Vector3(2.8f, 1.3f, 0.1f), true, (m, r) =>
                    {
for (int p = 0; p < 4; p++) m.Box(new Vector3(0f, 0.2f + p * 0.32f, 0f), new Vector3(2.8f, 0.2f, 0.05f), boards);
                         m.Box(new Vector3(1.37f, 0.65f, 0f), new Vector3(0.1f, 1.3f, 0.1f), boards);
                         // left hinge
                         m.Box(new Vector3(0.15f, 1.20f, 0f), new Vector3(0.07f, 0.15f, 0.02f), metal);
                         // right hinge
                         m.Box(new Vector3(2.65f, 1.20f, 0f), new Vector3(0.07f, 0.15f, 0.02f), metal);
                         // simple latch
                         m.Box(new Vector3(1.38f, 0.58f, 0f), new Vector3(0.20f, 0.05f, 0.02f), metal);
                         // top wear stripe (darkened wood)
                         m.Box(new Vector3(0f, 1.28f, 0f), new Vector3(2.8f, 0.02f, 0.1f), boards);
                         // nails
                         int nailCount = r.Next(3, 6);
                         for (int n = 0; n < nailCount; n++)
                         {
                             float nx = MapStyle.Range(r, 0.2f, 2.6f);
                             float ny = MapStyle.Range(r, 0.3f, 1.0f);
                             m.Cylinder(new Vector3(nx, ny, 0f), 0.01f, 0.03f, 8, metal);
                         }
                         // optional weathered sign
                         if (r.NextDouble() < 0.05)
                         {
                             // simple sign: thin plank with darker paint stroke
                             m.Box(new Vector3(1.4f, 0.90f, 0.055f), new Vector3(0.6f, 0.12f, 0.01f), boards);
                             // dark paint stripe on the sign
                             m.Box(new Vector3(1.4f, 0.94f, 0.056f), new Vector3(0.55f, 0.02f, 0.01f),
                                 DecorPalette.Matte(new Color(0.40f, 0.30f, 0.20f)));
                         }
                    }, "stall partition");
                }
                // The stalls' floor is taken; straw in them.
                Rect stalls = c.Foot(stallSide, (lo + hi) * 0.5f, hi - lo, 2.9f);
                c.Taken.Add(stalls);
                for (int k = 0; k < n; k++)
                {
                    Vector2 q = c.OnWall(stallSide, lo + 0.5f + (k + 0.5f) * pitch, 1.3f);
                    b.Furnish(c.R, new RaidBuilding.Place(q.x, q.y, MapStyle.Range(c.Rng, 0f, 360f)), new Vector3(0.1f, 0.05f, 0.1f), false, (m, r) =>
                        m.FlatBlob(new Vector3(0f, 0.035f, 0f), 1.0f, 0.8f, 0f, 0.3f, r, hay), "straw");
                }
            }
            // Hay bales stacked against the far wall.
            int back = RoomCtx.Opposite(door);
            for (int k = 0; k < Mathf.Clamp(Mathf.FloorToInt(c.Area / 25f), 1, 5); k++)
            {
                int layers = 1 + c.Rng.Next(3);
                if (!c.Wall(back, 2.0f, 1.0f, 0.4f * layers, -1f, out RaidBuilding.Place bales)) break;
                b.Furnish(c.R, bales, new Vector3(2.0f, 0.42f * layers, 1.0f), true, (m, r) =>
                {
                    for (int y = 0; y < layers; y++)
                        for (int x = 0; x < 2; x++)
                            for (int z = 0; z < 2; z++)
                            {
                                if (y == layers - 1 && r.NextDouble() < 0.25) continue;
                                m.Box(new Vector3(-0.5f + x * 1.0f + MapStyle.Range(r, -0.05f, 0.05f), 0.2f + y * 0.41f, -0.24f + z * 0.48f),
                                    new Vector3(0.95f, 0.4f, 0.46f), MapStyle.Range(r, -4f, 4f), hay);
                            }
                }, "hay bales");
            }
            MoreLoot(c, LootCount(c, 0.03f, 3), c.Sides(false));
            for (int k = 0; k < 2; k++)
                if (c.AnyWall(c.Sides(false), 0.62f, 0.62f, 0.9f, -1f, out RaidBuilding.Place barrel, out _))
                    b.Furnish(c.R, barrel, new Vector3(0.62f, 0.9f, 0.62f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, BarrelPaints[r.Next(BarrelPaints.Length)]), "barrel");
            if (c.Area > 40f) Clutter(c, 2);
        }

        private static void Greenhouse(RoomCtx c)
        {
            RaidBuilding b = c.B;
            Rect i = c.Inner;
            bool alongX = i.width >= i.height;
            float len = (alongX ? i.width : i.height) - 2.4f;
            Surf wood = DecorPalette.Matte(new Color(0.50f, 0.40f, 0.28f)), clay = DecorPalette.Matte(new Color(0.62f, 0.34f, 0.20f)), soil = DecorPalette.Matte(new Color(0.20f, 0.15f, 0.10f));
            int rows = Mathf.Clamp(Mathf.FloorToInt(((alongX ? i.height : i.width) - 1.0f) / 2.0f), 0, 6);
            for (int r = 0; r < rows && len > 1.5f; r++)
            {
                float across = (alongX ? i.yMin : i.xMin) + 1.0f + 0.45f + r * 2.0f;
                float mid = alongX ? i.center.x : i.center.y;
                var fp = alongX ? new Rect(mid - len * 0.5f, across - 0.45f, len, 0.9f) : new Rect(across - 0.45f, mid - len * 0.5f, 0.9f, len);
                if (!c.FloorAt(fp, 0.2f)) continue;
                float l = len;
                b.Furnish(c.R, alongX ? new RaidBuilding.Place(mid, across, 0f) : new RaidBuilding.Place(across, mid, 90f), new Vector3(l, 0.85f, 0.9f), true, (m, rr) =>
                {
                    m.Box(new Vector3(0f, 0.76f, 0f), new Vector3(l, 0.04f, 0.9f), wood);
                    int legs = Mathf.Max(2, Mathf.CeilToInt(l / 1.5f) + 1);
                    for (int k = 0; k < legs; k++)
                        for (int s = -1; s <= 1; s += 2)
                            m.Box(new Vector3(-l * 0.5f + 0.05f + (l - 0.1f) * k / (legs - 1), 0.37f, s * 0.4f), new Vector3(0.05f, 0.74f, 0.05f), wood);
                    int pots = Mathf.FloorToInt(l / 0.3f);
                    for (int k = 0; k < pots; k++)
                    {
                        if (rr.NextDouble() < 0.3) continue;
                        var at = new Vector3(-l * 0.5f + 0.15f + k * 0.3f, 0.78f, MapStyle.Range(rr, -0.25f, 0.25f));
                        m.Cylinder(at, 0.09f, 0.14f, 8, clay);
                        m.Cylinder(at + new Vector3(0f, 0.13f, 0f), 0.08f, 0.012f, 8, soil);
                        if (rr.NextDouble() < 0.5) FurnitureBuilder.DeadPlant(m, rr, at + new Vector3(0f, 0.02f, 0f));
                    }
                }, "potting bench");
            }
            // Sacks of compost and the tools on a shelf.
            MoreLoot(c, LootCount(c, 0.03f, 2), c.Sides(false));
            Clutter(c, 1);
        }
    }
}
