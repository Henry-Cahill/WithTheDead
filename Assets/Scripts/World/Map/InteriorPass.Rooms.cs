using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Walls as runs and pieces, the floors' outlines and finishes, and the furnishing of one room: where a piece can stand in
    // it (against a wall, clear of the doorways and the windows, or out on the floor), the loot-bearing pieces its kind of
    // room holds (MapGameplay.ContainersFor), its ceiling lights and its litter. The recipes for each kind of room are in
    // InteriorPass.Homes and InteriorPass.Trades.
    internal static partial class InteriorPass
    {
        #region Runs and pieces
        /// <summary>Partitions along one wall line, merged: from <see cref="A"/> to <see cref="B"/> on the rooms' lines, the wall
        /// itself from <see cref="Start"/> to <see cref="End"/> (a free end reaching on into the outside wall).</summary>
        internal sealed class Run
        {
            public bool AlongX;
            public float Coord, A, B, Start, End, SkinA, SkinB;
            public readonly List<PlanWall> Walls = new List<PlanWall>(2);
            public readonly List<PlanWall> Doors = new List<PlanWall>(1);
        }

        /// <summary>One stretch of a run (a solid wall, or the head over a doorway), as an object of its own.</summary>
        internal sealed class Piece
        {
            public bool AlongX;
            public float Coord, S, E;
            public PlanWall Door;
            public Run Run;
            public GameObject Object;
            public CutawayOccluder Occluder;
            /// <summary>Where its object stands in the frame.</summary>
            public Vector3 Centre;
            /// <summary>In the frame (the piece's object sits in the turned frame).</summary>
            public readonly RaidMesh Mesh = new RaidMesh();
            /// <summary>In the world's axes: what RaidBuilding hangs on the wall (cupboards, a mirror cabinet, a clock's dial).</summary>
            public RaidMesh Fittings;
        }

        internal static List<Run> Runs(InteriorPlan plan)
        {
            var walls = new List<PlanWall>(plan.Walls.Count);
            foreach (PlanWall w in plan.Walls) if (!w.Open) walls.Add(w);
            walls.Sort((a, b) => a.AlongX != b.AlongX ? a.AlongX.CompareTo(b.AlongX)
                : Mathf.Abs(a.Coord - b.Coord) > 0.02f ? a.Coord.CompareTo(b.Coord) : a.A.CompareTo(b.A));
            var runs = new List<Run>();
            Run cur = null;
            foreach (PlanWall w in walls)
            {
                if (cur != null && cur.AlongX == w.AlongX && Mathf.Abs(cur.Coord - w.Coord) < 0.02f && w.A <= cur.B + 0.02f)
                    cur.B = Mathf.Max(cur.B, w.B);
                else
                {
                    cur = new Run { AlongX = w.AlongX, Coord = w.Coord, A = w.A, B = w.B };
                    runs.Add(cur);
                }
                cur.Walls.Add(w);
                if (w.HasDoor) cur.Doors.Add(w);
            }
            foreach (Run r in runs)
            {
                // As the raid town's walls meet: the runs along X carry the corners, those along Z stop at their faces.
                r.Start = r.AlongX ? r.A - Half : r.A + Half;
                r.End = r.AlongX ? r.B + Half : r.B - Half;
                r.SkinA = r.A;
                r.SkinB = r.B;
                // An end that meets no other partition runs on into the outside wall (a slanting wall, a room stopping short).
                if (!Joined(runs, r, r.A))
                {
                    float reach = Reach(plan, r, r.A, -1);
                    if (reach < r.Start) { r.Start = reach; r.SkinA = reach; }
                }
                if (!Joined(runs, r, r.B))
                {
                    float reach = Reach(plan, r, r.B, 1);
                    if (reach > r.End) { r.End = reach; r.SkinB = reach; }
                }
                foreach (PlanWall w in r.Walls)
                {
                    if (Mathf.Abs(w.A - r.A) < 0.01f) w.SkinA = r.SkinA;
                    else w.SkinA = w.A;
                    if (Mathf.Abs(w.B - r.B) < 0.01f) w.SkinB = r.SkinB;
                    else w.SkinB = w.B;
                }
            }
            return runs;
        }

        // True when another partition crosses or meets run `r` at `at` along it.
        private static bool Joined(List<Run> runs, Run r, float at)
        {
            foreach (Run q in runs)
            {
                if (q.AlongX == r.AlongX || Mathf.Abs(q.Coord - at) > 0.05f) continue;
                if (r.Coord >= q.A - 0.05f && r.Coord <= q.B + 0.05f) return true;
            }
            return false;
        }

        // Where a run's free end, carried on along its line, reaches the middle of the outside wall (at most 2 m on).
        private static float Reach(InteriorPlan plan, Run r, float at, int dir)
        {
            Vector2 o = r.AlongX ? new Vector2(at, r.Coord) : new Vector2(r.Coord, at);
            Vector2 d = r.AlongX ? new Vector2(dir, 0f) : new Vector2(0f, dir);
            float best = float.MaxValue;
            List<Vector2> p = plan.Local;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            {
                Vector2 a = p[j], b = p[i], e = b - a;
                float den = d.x * e.y - d.y * e.x;
                if (Mathf.Abs(den) < 1e-6f) continue;
                Vector2 ao = a - o;
                float t = (ao.x * e.y - ao.y * e.x) / den, u = (ao.x * d.y - ao.y * d.x) / den;
                if (t > -0.05f && u >= 0f && u <= 1f && t < best) best = t;
            }
            if (best == float.MaxValue) return at;
            float end = at + dir * Mathf.Clamp(best - plan.Thick * 0.5f, 0f, 2f);
            return end;
        }

        /// <summary>A room's floor: its rectangle to the walls' faces, or to the shared line where it runs open into a
        /// neighbour.</summary>
        internal static Rect FloorRect(InteriorPlan plan, int index)
        {
            PlanRoom r = plan.Rooms[index];
            Rect R = r.R;
            float x0 = R.xMin + Half, x1 = R.xMax - Half, z0 = R.yMin + Half, z1 = R.yMax - Half;
            foreach (PlanWall w in plan.Walls)
            {
                if (!w.Open || (w.Minus != index && w.Plus != index)) continue;
                if (w.Length < 0.5f * (w.AlongX ? R.width : R.height)) continue;
                if (w.AlongX)
                {
                    if (w.Minus == index) z1 = R.yMax;
                    else z0 = R.yMin;
                }
                else
                {
                    if (w.Minus == index) x1 = R.xMax;
                    else x0 = R.xMin;
                }
            }
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }

        internal static Surf FloorSurf(PlanRoom room)
        {
            switch (room.Floor)
            {
                case FloorKind.LinoBeige: return DecorMaterials.Linoleum(new Color(0.62f, 0.57f, 0.46f), new Color(0.70f, 0.66f, 0.55f), false);
                case FloorKind.LinoGreen: return DecorMaterials.Linoleum(new Color(0.52f, 0.60f, 0.56f), new Color(0.62f, 0.68f, 0.64f), false);
                case FloorKind.Checker: return DecorMaterials.Linoleum(new Color(0.76f, 0.74f, 0.66f), new Color(0.20f, 0.20f, 0.22f), true);
                case FloorKind.Wood: return DecorMaterials.WoodFloor(new Color(0.46f, 0.31f, 0.19f));
                case FloorKind.WoodDark: return DecorMaterials.WoodFloor(new Color(0.33f, 0.22f, 0.14f));
                case FloorKind.Carpet: return DecorMaterials.Carpet(room.FloorTint);
                case FloorKind.Concrete: return DecorMaterials.Concrete;
                case FloorKind.Plywood: return DecorMaterials.Planks(new Color(0.52f, 0.44f, 0.32f));
                default: return DecorMaterials.Linoleum(new Color(0.47f, 0.47f, 0.46f), new Color(0.56f, 0.56f, 0.54f), false);
            }
        }

        /// <summary>A room's name as a door's starts with it: "Living room", "Men's room".</summary>
        internal static string Title(string room)
        {
            if (string.IsNullOrEmpty(room)) return "Room";
            string[] words = room.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (w.Length == 0) continue;
                w = w.ToLowerInvariant();
                words[i] = i == 0 ? char.ToUpperInvariant(w[0]) + w.Substring(1) : w;
            }
            return string.Join(" ", words);
        }
        #endregion

        #region A room to furnish
        /// <summary>One room being furnished, in the frame: its floor inside the walls, what's taken, and per wall (0 south, 1
        /// east, 2 north, 3 west) the gaps where nothing may stand (doorways, open sides), the windows tall pieces keep clear of,
        /// and whether it's a partition (where wall fittings go, so they hide with it).</summary>
        internal sealed class RoomCtx
        {
            public InteriorBuild Build;
            public RaidBuilding B;
            public InteriorPlan Plan;
            public PlanRoom P;
            public RaidRoom R;
            public int Index;
            public System.Random Rng;
            public Rect Inner;
            public ContainerType[] Pool;
            public readonly List<Rect> Taken = new List<Rect>(24);
            /// <summary>The taken rectangles of pieces taller than a desk (nothing hangs on the wall behind them).</summary>
            public readonly List<Rect> TallRects = new List<Rect>(8);
            public readonly List<Rect> Keep = new List<Rect>(6);
            public readonly List<Vector2>[] Gaps = { new List<Vector2>(2), new List<Vector2>(2), new List<Vector2>(2), new List<Vector2>(2) };
            public readonly List<Vector2>[] Panes = { new List<Vector2>(2), new List<Vector2>(2), new List<Vector2>(2), new List<Vector2>(2) };
            public readonly bool[] Inside = new bool[4];
            private int _pool;

            /// <summary>Pieces placed so far that hold loot (see <see cref="TakeLoot"/>).</summary>
            public int LootUsed;

            /// <summary>How many more of the room's pieces may hold loot (its share of the building's, set with its plan).</summary>
            public int LootLeft => Mathf.Max(0, (P.Loot) - LootUsed);

            /// <summary>Takes one piece's worth of the room's loot budget: false when it's spent (the piece is plain furniture).</summary>
            public bool TakeLoot()
            {
                if (LootLeft <= 0) return false;
                LootUsed++;
                return true;
            }

            /// <summary>Gives back a piece's worth that went unused (it didn't fit).</summary>
            public void GiveBack() => LootUsed = Mathf.Max(0, LootUsed - 1);

            /// <summary>True once the building has as many pieces with bodies of their own as it may (the rest are plainer).</summary>
            public bool Crowded => Build.Bodies >= Plan.PieceCap;

            /// <summary>True when the building may have another lamp (desk, bedside or standard) besides its ceilings'.</summary>
            public bool LampLeft => Build.Lamps < Plan.LampCap - Plan.CeilingLamps;

            public float Area => Inner.width * Inner.height;
            public float Width => Inner.width;
            public float Depth => Inner.height;

            /// <summary>A wall's length.</summary>
            public float Len(int side) => side == 0 || side == 2 ? Inner.width : Inner.height;

            /// <summary>The next loot kind for a storage piece: the room's own first, then from its pool; null when the loot
            /// tables give this room nothing.</summary>
            public ContainerType? NextLoot()
            {
                if (Pool == null || Pool.Length == 0) return null;
                ContainerType t = _pool == 0 ? Pool[0] : Pool[Rng.Next(Pool.Length)];
                _pool++;
                return t;
            }

            /// <summary>The wall face's frame coordinate (z of the south and north walls, x of the east and west).</summary>
            public float Face(int side) => side == 0 ? Inner.yMin : side == 1 ? Inner.xMax : side == 2 ? Inner.yMax : Inner.xMin;

            /// <summary>The extent along a wall.</summary>
            public void Span(int side, out float lo, out float hi)
            {
                lo = side == 0 || side == 2 ? Inner.xMin : Inner.yMin;
                hi = side == 0 || side == 2 ? Inner.xMax : Inner.yMax;
            }

            /// <summary>The frame point `along` a wall, `out` from its face into the room.</summary>
            public Vector2 OnWall(int side, float along, float outFrom)
            {
                switch (side)
                {
                    case 0: return new Vector2(along, Inner.yMin + outFrom);
                    case 1: return new Vector2(Inner.xMax - outFrom, along);
                    case 2: return new Vector2(along, Inner.yMax - outFrom);
                    default: return new Vector2(Inner.xMin + outFrom, along);
                }
            }

            /// <summary>The yaw (RaidBuilding's local degrees) of a piece with its back to wall `side`, facing into the room.</summary>
            public static float YawOf(int side) => side == 0 ? 0f : side == 1 ? 270f : side == 2 ? 180f : 90f;

            /// <summary>The wall the room's main door is in (-1: none).</summary>
            public int DoorSide = -1;
            public float DoorAlong;

            /// <summary>A piece <paramref name="w"/> wide and <paramref name="d"/> deep with its back to wall <paramref name="side"/>
            /// (<paramref name="h"/> high: a tall one keeps off the windows), placed as near <paramref name="pref"/> of the way
            /// along it as it fits (negative: anywhere), clear of doorways and of what's there already; taken at once.</summary>
            public bool Wall(int side, float w, float d, float h, float pref, out RaidBuilding.Place place, float gap = 0.02f)
            {
                place = default;
                Span(side, out float lo, out float hi);
                float min = lo + w * 0.5f + 0.02f, max = hi - w * 0.5f - 0.02f;
                if (max < min) return false;
                float want = pref < 0f ? Mathf.Lerp(min, max, (float)Rng.NextDouble()) : Mathf.Lerp(min, max, Mathf.Clamp01(pref));
                int steps = Mathf.CeilToInt((max - min) / 0.1f);
                for (int k = 0; k <= steps * 2; k++)
                {
                    float off = (k + 1) / 2 * 0.1f * (k % 2 == 0 ? 1f : -1f);
                    float t = want + off;
                    if (t < min - 1e-3f || t > max + 1e-3f) continue;
                    Rect fp = Foot(side, t, w, d + gap);
                    if (!Fits(side, t, w, h, fp)) continue;
                    place = PlaceOn(side, t, d, gap);
                    Taken.Add(fp);
                    if (h > 1.2f) TallRects.Add(fp);
                    return true;
                }
                return false;
            }

            /// <summary>As <see cref="Wall"/>, on the first of <paramref name="sides"/> where it fits.</summary>
            public bool AnyWall(int[] sides, float w, float d, float h, float pref, out RaidBuilding.Place place, out int side)
            {
                foreach (int s in sides)
                    if (Wall(s, w, d, h, pref, out place)) { side = s; return true; }
                place = default;
                side = -1;
                return false;
            }

            /// <summary>A piece against wall <paramref name="side"/> exactly at <paramref name="t"/> along it, if it fits there.</summary>
            public bool WallAt(int side, float t, float w, float d, float h, out RaidBuilding.Place place, float gap = 0.02f)
            {
                place = default;
                Span(side, out float lo, out float hi);
                if (t - w * 0.5f < lo + 0.01f || t + w * 0.5f > hi - 0.01f) return false;
                Rect fp = Foot(side, t, w, d + gap);
                if (!Fits(side, t, w, h, fp)) return false;
                place = PlaceOn(side, t, d, gap);
                Taken.Add(fp);
                if (h > 1.2f) TallRects.Add(fp);
                return true;
            }

            /// <summary>The longest stretch of wall <paramref name="side"/> with no doorway in it.</summary>
            public void FreeRun(int side, out float a, out float b)
            {
                Span(side, out float lo, out float hi);
                var cuts = new List<Vector2>(Gaps[side]);
                cuts.Sort((x, y) => x.x.CompareTo(y.x));
                a = lo;
                b = lo;
                float cursor = lo;
                foreach (Vector2 g in cuts)
                {
                    if (g.x - cursor > b - a) { a = cursor; b = Mathf.Min(g.x, hi); }
                    cursor = Mathf.Max(cursor, g.y);
                }
                if (hi - cursor > b - a) { a = cursor; b = hi; }
            }

            public RaidBuilding.Place PlaceOn(int side, float t, float d, float gap)
            {
                switch (side)
                {
                    case 0: return new RaidBuilding.Place(t, Inner.yMin + d * 0.5f + gap, 0f);
                    case 1: return new RaidBuilding.Place(Inner.xMax - d * 0.5f - gap, t, 270f);
                    case 2: return new RaidBuilding.Place(t, Inner.yMax - d * 0.5f - gap, 180f);
                    default: return new RaidBuilding.Place(Inner.xMin + d * 0.5f + gap, t, 90f);
                }
            }

            public Rect Foot(int side, float t, float w, float d)
            {
                switch (side)
                {
                    case 0: return new Rect(t - w * 0.5f, Inner.yMin, w, d);
                    case 1: return new Rect(Inner.xMax - d, t - w * 0.5f, d, w);
                    case 2: return new Rect(t - w * 0.5f, Inner.yMax - d, w, d);
                    default: return new Rect(Inner.xMin, t - w * 0.5f, d, w);
                }
            }

            private bool Fits(int side, float t, float w, float h, Rect fp)
            {
                float a = t - w * 0.5f, b = t + w * 0.5f;
                foreach (Vector2 g in Gaps[side]) if (a < g.y && b > g.x) return false;
                if (h > 1.0f) foreach (Vector2 g in Panes[side]) if (a < g.y + 0.05f && b > g.x - 0.05f) return false;
                return Clear(fp);
            }

            /// <summary>True when nothing stands or is kept clear in a frame rectangle.</summary>
            public bool Clear(Rect fp)
            {
                var shrunk = new Rect(fp.x + 0.01f, fp.y + 0.01f, fp.width - 0.02f, fp.height - 0.02f);
                foreach (Rect r in Taken) if (r.Overlaps(shrunk)) return false;
                foreach (Rect r in Keep) if (r.Overlaps(shrunk)) return false;
                return true;
            }

            /// <summary>A piece out on the floor, <paramref name="w"/> × <paramref name="d"/> in the frame's axes (turned
            /// <paramref name="yaw"/>, a multiple of 90), as near <paramref name="near"/> as fits with <paramref name="margin"/>
            /// round it; taken at once.</summary>
            public bool Floor(float w, float d, float yaw, Vector2 near, float margin, out RaidBuilding.Place place)
            {
                place = default;
                bool turned = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(yaw, 270f)) < 1f;
                float fw = turned ? d : w, fd = turned ? w : d;
                float reach = Mathf.Max(Inner.width, Inner.height);
                for (float r = 0f; r <= reach; r += 0.2f)
                {
                    int n = r < 0.01f ? 1 : Mathf.Max(8, Mathf.RoundToInt(r * 6f));
                    for (int i = 0; i < n; i++)
                    {
                        float ang = i * Mathf.PI * 2f / n;
                        Vector2 c = near + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                        var fp = new Rect(c.x - fw * 0.5f, c.y - fd * 0.5f, fw, fd);
                        if (fp.xMin < Inner.xMin + margin || fp.xMax > Inner.xMax - margin || fp.yMin < Inner.yMin + margin || fp.yMax > Inner.yMax - margin) continue;
                        var grown = new Rect(fp.x - margin, fp.y - margin, fp.width + 2f * margin, fp.height + 2f * margin);
                        if (!Clear(grown)) continue;
                        place = new RaidBuilding.Place(c.x, c.y, yaw);
                        Taken.Add(fp);
                        return true;
                    }
                }
                return false;
            }

            /// <summary>A piece out on the floor exactly over frame rectangle <paramref name="fp"/>, if it's inside the room and
            /// clear with <paramref name="margin"/> round it; taken at once.</summary>
            public bool FloorAt(Rect fp, float margin)
            {
                if (fp.xMin < Inner.xMin + 0.02f || fp.xMax > Inner.xMax - 0.02f || fp.yMin < Inner.yMin + 0.02f || fp.yMax > Inner.yMax - 0.02f) return false;
                var grown = new Rect(fp.x - margin, fp.y - margin, fp.width + 2f * margin, fp.height + 2f * margin);
                if (!Clear(grown)) return false;
                Taken.Add(fp);
                return true;
            }

            /// <summary>A wall-face character for RaidBuilding's wall fittings ('S', 'E', 'N', 'W').</summary>
            public static char Char(int side) => side == 0 ? 'S' : side == 1 ? 'E' : side == 2 ? 'N' : 'W';

            /// <summary>The walls in the order to try them: partitions first (fittings hang there best), the longest first.</summary>
            public int[] Sides(bool insideFirst)
            {
                var s = new List<int> { 0, 1, 2, 3 };
                s.Sort((a, b) =>
                {
                    if (insideFirst && Inside[a] != Inside[b]) return Inside[a] ? -1 : 1;
                    return Len(b).CompareTo(Len(a));
                });
                return s.ToArray();
            }

            /// <summary>The wall facing <paramref name="side"/>.</summary>
            public static int Opposite(int side) => (side + 2) & 3;
        }

        private static RoomCtx Context(InteriorBuild build, int index)
        {
            InteriorPlan plan = build.Plan;
            PlanRoom p = plan.Rooms[index];
            var c = new RoomCtx
            {
                Build = build, B = build.B, Plan = plan, P = p, R = build.Rooms[index], Index = index,
                Rng = new System.Random(unchecked(plan.Seed * 977 + index * 131 + 7)),
                Inner = Rect.MinMaxRect(p.R.xMin + Half, p.R.yMin + Half, p.R.xMax - Half, p.R.yMax - Half),
            };
            c.Pool = p.Pool ?? NoPool;
            Rect inner = c.Inner;
            float longest = -1f;
            // Doorways and open sides between rooms.
            float[] walled = new float[4];
            foreach (PlanWall w in plan.Walls)
            {
                if (w.Minus != index && w.Plus != index) continue;
                int side = w.AlongX ? (w.Minus == index ? 2 : 0) : (w.Minus == index ? 1 : 3);
                if (w.Open)
                {
                    c.Gaps[side].Add(new Vector2(w.A - 0.1f, w.B + 0.1f));
                    c.Keep.Add(Zone(c, side, w.A, w.B, 0.9f));
                    continue;
                }
                walled[side] += w.Length;
                if (!w.HasDoor) continue;
                float a = w.DoorPos - w.DoorWidth * 0.5f, b = w.DoorPos + w.DoorWidth * 0.5f;
                c.Gaps[side].Add(new Vector2(a - 0.12f, b + 0.12f));
                c.Keep.Add(Zone(c, side, a - 0.15f, b + 0.15f, 1.15f));
                if (w.Length > longest) { longest = w.Length; c.DoorSide = side; c.DoorAlong = w.DoorPos; }
            }
            for (int s = 0; s < 4; s++) c.Inside[s] = walled[s] > 0.5f * c.Len(s);
            // Outside doors: wider keep-clear, and the room's main door when it has one.
            foreach (PlanEntrance en in plan.Entrances)
            {
                if (en.Room != index) continue;
                int side = NearestSide(inner, en.At, out float along);
                if (side < 0) continue;
                float half = en.Width * 0.5f;
                c.Gaps[side].Add(new Vector2(along - half - 0.2f, along + half + 0.2f));
                c.Keep.Add(Zone(c, side, along - half - 0.25f, along + half + 0.25f, 1.5f));
                c.DoorSide = side;
                c.DoorAlong = along;
            }
            foreach (PlanWindow w in plan.Windows)
            {
                if (w.Room != index) continue;
                int side = NearestSide(inner, (w.A + w.B) * 0.5f, out float along);
                if (side < 0) continue;
                float a = side == 0 || side == 2 ? w.A.x : w.A.y, b = side == 0 || side == 2 ? w.B.x : w.B.y;
                c.Panes[side].Add(new Vector2(Mathf.Min(a, b), Mathf.Max(a, b)));
            }
            return c;
        }

        // The wall of a room's floor a frame point lies on (within 0.6 m), and how far along it.
        private static int NearestSide(Rect inner, Vector2 q, out float along)
        {
            along = 0f;
            float best = 0.6f;
            int side = -1;
            for (int s = 0; s < 4; s++)
            {
                float d, t;
                switch (s)
                {
                    case 0: d = Mathf.Abs(q.y - inner.yMin); t = q.x; break;
                    case 1: d = Mathf.Abs(q.x - inner.xMax); t = q.y; break;
                    case 2: d = Mathf.Abs(q.y - inner.yMax); t = q.x; break;
                    default: d = Mathf.Abs(q.x - inner.xMin); t = q.y; break;
                }
                bool within = s == 0 || s == 2 ? t >= inner.xMin - 0.3f && t <= inner.xMax + 0.3f : t >= inner.yMin - 0.3f && t <= inner.yMax + 0.3f;
                if (within && d < best) { best = d; side = s; along = t; }
            }
            return side;
        }

        // The floor kept clear in front of a stretch of wall `a`..`b`, `depth` deep.
        private static Rect Zone(RoomCtx c, int side, float a, float b, float depth)
        {
            Rect i = c.Inner;
            switch (side)
            {
                case 0: return Rect.MinMaxRect(a, i.yMin - 0.2f, b, i.yMin + depth);
                case 1: return Rect.MinMaxRect(i.xMax - depth, a, i.xMax + 0.2f, b);
                case 2: return Rect.MinMaxRect(a, i.yMax - depth, b, i.yMax + 0.2f);
                default: return Rect.MinMaxRect(i.xMin - 0.2f, a, i.xMin + depth, b);
            }
        }
        #endregion

        #region Furnishing a room
        /// <summary>Furnishes room <paramref name="index"/> of the build: its recipe, then its lights and its litter.</summary>
        internal static void Furnish(InteriorBuild build, int index)
        {
            RoomCtx c = Context(build, index);
            if (c.Inner.width < 0.8f || c.Inner.height < 0.8f) return;
            switch (c.P.Role)
            {
                case RoomRole.Living: Living(c); break;
                case RoomRole.Dining: Dining(c); break;
                case RoomRole.Kitchen: Kitchen(c); break;
                case RoomRole.Bathroom: Bathroom(c, false); break;
                case RoomRole.Restroom: Bathroom(c, true); break;
                case RoomRole.Bedroom: case RoomRole.Guest: Bedroom(c); break;
                case RoomRole.Hall: Hall(c); break;
                case RoomRole.Den: Den(c); break;
                case RoomRole.Laundry: Laundry(c); break;
                case RoomRole.Closet: Closet(c); break;
                case RoomRole.HomeGarage: case RoomRole.Bays: Garage(c); break;
                case RoomRole.Shed: Shed(c); break;
                case RoomRole.Sales: Sales(c); break;
                case RoomRole.Stock: case RoomRole.Records: case RoomRole.Sorting: case RoomRole.Dispensary: Stock(c); break;
                case RoomRole.BackOffice: case RoomRole.Office: case RoomRole.Sacristy: Office(c, false); break;
                case RoomRole.OpenOffice: case RoomRole.Squad: Office(c, true); break;
                case RoomRole.Lobby: case RoomRole.Waiting: case RoomRole.Narthex: Lobby(c); break;
                case RoomRole.BreakRoom: case RoomRole.DayRoom: BreakRoom(c); break;
                case RoomRole.Vault: Vault(c); break;
                case RoomRole.Stacks: Stacks(c); break;
                case RoomRole.Meeting: case RoomRole.Chapel: Meeting(c); break;
                case RoomRole.Eatery: Eatery(c); break;
                case RoomRole.CommercialKitchen: CommercialKitchen(c); break;
                case RoomRole.Bar: Bar(c); break;
                case RoomRole.Exam: Exam(c); break;
                case RoomRole.Ward: Ward(c); break;
                case RoomRole.Cells: Cells(c); break;
                case RoomRole.Armory: Armory(c); break;
                case RoomRole.Bunks: Bunks(c); break;
                case RoomRole.Nave: Nave(c); break;
                case RoomRole.Classroom: Classroom(c); break;
                case RoomRole.Gym: Gym(c); break;
                case RoomRole.Warehouse: Warehouse(c); break;
                case RoomRole.Workshop: Workshop(c); break;
                case RoomRole.Barn: Barn(c); break;
                case RoomRole.Greenhouse: Greenhouse(c); break;
            }
            Lights(c);
            Litter(c, LitterFor(c.P.Role));
        }

        // The room's ceiling fittings, as its plan has them (none past the building's lamp budget), spread along its longer
        // axis.
        private static void Lights(RoomCtx c)
        {
            int want = c.P.Lamps;
            if (want <= 0) return;
            bool alongX = c.Inner.width >= c.Inner.height;
            for (int i = 0; i < want; i++)
            {
                float k = (i + 0.5f) / want;
                float lx = alongX ? Mathf.Lerp(c.Inner.xMin, c.Inner.xMax, k) : c.Inner.center.x;
                float lz = alongX ? c.Inner.center.y : Mathf.Lerp(c.Inner.yMin, c.Inner.yMax, k);
                c.B.CeilingLight(c.R, lx, lz, alongX ? 0f : 90f);
            }
        }

        private static float LitterFor(RoomRole role)
        {
            switch (role)
            {
                case RoomRole.Bathroom: case RoomRole.Restroom: case RoomRole.Closet: return 0.15f;
                case RoomRole.Warehouse: case RoomRole.Barn: case RoomRole.Greenhouse: case RoomRole.Gym: case RoomRole.Nave: return 0.12f;
                case RoomRole.Stock: case RoomRole.Workshop: case RoomRole.Bays: case RoomRole.HomeGarage: case RoomRole.Shed: return 0.35f;
                case RoomRole.Living: case RoomRole.Dining: case RoomRole.Kitchen: case RoomRole.Bedroom: case RoomRole.Den:
                case RoomRole.Laundry: case RoomRole.Guest:
                    return 0.3f;
                default: return 0.45f;
            }
        }

        // Litter drifted toward the walls and corners, clear of the furniture (the raid town's, laid in the frame).
        private static void Litter(RoomCtx c, float perSqm)
        {
            Rect inner = c.Inner;
            int count = Mathf.Min(60, Mathf.RoundToInt(inner.width * inner.height * perSqm));
            for (int i = 0; i < count; i++)
            {
                float u = (float)c.Rng.NextDouble(), v = (float)c.Rng.NextDouble();
                if (c.Rng.NextDouble() < 0.5) u = u < 0.5f ? u * u : 1f - (1f - u) * (1f - u);
                var q = new Vector2(Mathf.Lerp(inner.xMin + 0.15f, inner.xMax - 0.15f, u), Mathf.Lerp(inner.yMin + 0.15f, inner.yMax - 0.15f, v));
                var probe = new Rect(q.x - 0.1f, q.y - 0.1f, 0.2f, 0.2f);
                bool under = false;
                foreach (Rect f in c.Taken) if (f.Overlaps(probe)) { under = true; break; }
                if (under) continue;
                DecorProps.Litter(c.B.Flat, c.Rng, c.B.W(q.x, 0.036f, q.y), false);
            }
        }
        #endregion

        #region Pieces every kind of room uses
        private static readonly ContainerType[] NoPool = new ContainerType[0];

        /// <summary>A storage piece suited to loot of kind <paramref name="type"/> against one of the room's walls: shelving,
        /// lockers, a gun safe, a medicine cabinet, a filing cabinet or a desk, a tool bench, a crate, a fridge, a wardrobe.
        /// It holds loot while the room's budget lasts (<see cref="RoomCtx.TakeLoot"/>); past it, the shelving, lockers,
        /// filing cabinets, benches, crates and wardrobes are plain furniture, and the rest isn't placed. False when nothing
        /// was placed.</summary>
        private static bool LootPiece(RoomCtx c, ContainerType type, int[] sides, float pref = -1f)
        {
            bool loot = c.TakeLoot();
            bool placed = loot ? StoragePiece(c, type, sides, pref) : PlainPiece(c, type, sides, pref);
            if (loot && !placed) c.GiveBack();
            return placed;
        }

        // A storage piece that holds loot.
        private static bool StoragePiece(RoomCtx c, ContainerType type, int[] sides, float pref)
        {
            RaidBuilding b = c.B;
            RaidBuilding.Place p;
            switch (type)
            {
                case ContainerType.Shelf: case ContainerType.Pantry:
                {
                    float w = c.Rng.NextDouble() < 0.5 ? 1.2f : 1.0f;
                    if (!c.AnyWall(sides, w, 0.45f, 1.9f, pref, out p, out _)) return false;
                    int stock = type == ContainerType.Pantry ? 2 : c.Rng.Next(4);
                    b.Shelving(c.R, p, new Vector3(w, 1.9f, 0.45f), 4, stock, ShelfGrey, type, type == ContainerType.Pantry ? "Pantry shelves" : "Shelves", true);
                    return true;
                }
                case ContainerType.Locker:
                {
                    int n = c.Rng.Next(2, 4);
                    float w = n * 0.5f;
                    if (!c.AnyWall(sides, w, 0.5f, 1.9f, pref, out p, out _)) return false;
                    b.Lockers(c.R, p, new Vector3(w, 1.9f, 0.5f), n, LockerPaints[c.Rng.Next(LockerPaints.Length)], ContainerType.Locker, "Locker");
                    return true;
                }
                case ContainerType.GunLocker:
                    if (!c.AnyWall(sides, 0.8f, 0.6f, 1.9f, pref, out p, out _)) return false;
                    b.GunSafe(c.R, p, ContainerType.GunLocker, "Gun locker");
                    return true;
                case ContainerType.MedCabinet:
                    if (!c.AnyWall(sides, 0.9f, 0.42f, 1.8f, pref, out p, out _)) return false;
                    b.MedicineCabinet(c.R, p, ContainerType.MedCabinet, "Medicine cabinet");
                    return true;
                case ContainerType.Desk:
                    if (c.Rng.NextDouble() < 0.5 && c.AnyWall(sides, 0.46f, 0.62f, 1.4f, pref, out p, out _))
                    {
                        b.FilingCabinet(c.R, p, 4, Putty);
                        return true;
                    }
                    if (!c.AnyWall(sides, 1.4f, 0.75f, 0.9f, pref, out p, out _)) return false;
                    b.DrawerDesk(c.R, p, 1.4f, Woods[c.Rng.Next(Woods.Length)], true, "Desk", (m, r) => DeskClutter(m, r, 1.1f));
                    return true;
                case ContainerType.ToolBench:
                    if (!c.AnyWall(sides, 1.8f, 0.75f, 1.9f, pref, out p, out _)) return false;
                    b.ToolBench(c.R, p, ContainerType.ToolBench, "Tool bench");
                    return true;
                case ContainerType.Crate:
                    if (!c.AnyWall(sides, 0.9f, 0.62f, 0.62f, pref, out p, out _)) return false;
                    b.SupplyCrate(c.R, p, new Vector3(0.9f, 0.6f, 0.62f), false, ContainerType.Crate, "Crate");
                    return true;
                case ContainerType.Fridge:
                    if (!c.AnyWall(sides, 0.76f, 0.72f, 1.75f, pref, out p, out _)) return false;
                    b.WorkFridge(c.R, p, FurnitureBuilder.Cream, ContainerType.Fridge, "Fridge", false);
                    return true;
                case ContainerType.Wardrobe:
                    if (!c.AnyWall(sides, 1.0f, 0.6f, 1.95f, pref, out p, out _)) return false;
                    b.Wardrobe(c.R, p, Woods[c.Rng.Next(Woods.Length)]);
                    return true;
                default:
                    return false;
            }
        }

        // The same piece as plain furniture (drawn into the building's meshes: no fixture, nothing to search), for storage
        // past the room's loot budget; false for the pieces that only make sense holding something.
        private static bool PlainPiece(RoomCtx c, ContainerType type, int[] sides, float pref)
        {
            RaidBuilding b = c.B;
            RaidBuilding.Place p;
            switch (type)
            {
                case ContainerType.Shelf: case ContainerType.Pantry:
                {
                    float w = c.Rng.NextDouble() < 0.5 ? 1.2f : 1.0f;
                    if (!c.AnyWall(sides, w, 0.45f, 1.9f, pref, out p, out _)) return false;
                    int stock = type == ContainerType.Pantry ? 2 : c.Rng.Next(4);
                    b.Furnish(c.R, p, new Vector3(w, 1.9f, 0.45f), true, (m, r) => FurnitureBuilder.MetalShelf(m, r, w, 0.45f, 1.9f, 4, stock, ShelfGrey), "Shelves");
                    return true;
                }
                case ContainerType.Locker:
                {
                    int n = c.Rng.Next(2, 4);
                    Color paint = LockerPaints[c.Rng.Next(LockerPaints.Length)];
                    if (!c.AnyWall(sides, n * 0.5f, 0.5f, 1.9f, pref, out p, out _)) return false;
                    b.Furnish(c.R, p, new Vector3(n * 0.5f, 1.9f, 0.5f), true, (m, r) => FurnitureBuilder.LockerBank(m, r, n, paint), "Lockers");
                    return true;
                }
                case ContainerType.Desk:
                    if (!c.AnyWall(sides, 0.46f, 0.62f, 1.4f, pref, out p, out _)) return false;
                    b.Furnish(c.R, p, new Vector3(0.46f, 1.4f, 0.62f), true, (m, r) => FurnitureBuilder.FilingCabinet(m, r, 4, Putty), "Filing cabinet");
                    return true;
                case ContainerType.ToolBench:
                    if (!c.AnyWall(sides, 1.8f, 0.75f, 1.9f, pref, out p, out _)) return false;
                    b.Furnish(c.R, p, new Vector3(1.8f, 1.9f, 0.75f), true, (m, r) => FurnitureBuilder.ToolBenchBody(m, r), "Tool bench");
                    return true;
                case ContainerType.Crate:
                    if (!c.AnyWall(sides, 0.9f, 0.62f, 0.62f, pref, out p, out _)) return false;
                    b.Furnish(c.R, p, new Vector3(0.9f, 0.55f, 0.62f), true, (m, r) => FurnitureBuilder.Crate(m, r, Vector3.zero, new Vector3(0.88f, 0.5f, 0.58f), 0f, false), "Crate");
                    return true;
                case ContainerType.Wardrobe:
                {
                    Color wood = Woods[c.Rng.Next(Woods.Length)];
                    if (!c.AnyWall(sides, 1.0f, 0.6f, 1.95f, pref, out p, out _)) return false;
                    b.Furnish(c.R, p, new Vector3(1.0f, 1.95f, 0.6f), true, (m, r) => FurnitureBuilder.Wardrobe(m, r, wood), "Wardrobe");
                    return true;
                }
                default:
                    return false;
            }
        }

        /// <summary>Up to <paramref name="count"/> more storage pieces holding loot from the room's pool, against its walls, as
        /// far as its loot budget goes.</summary>
        private static void MoreLoot(RoomCtx c, int count, int[] sides)
        {
            count = Mathf.Min(count, c.LootLeft);
            for (int i = 0; i < count; i++)
            {
                ContainerType? t = c.NextLoot();
                if (t == null) return;
                LootPiece(c, t.Value, sides);
            }
        }

        /// <summary>What's left of the room's loot budget, as storage pieces from its pool, against its walls.</summary>
        private static void RestOfLoot(RoomCtx c, int[] sides, int max = 4) => MoreLoot(c, max, sides);

        /// <summary>Open steel shelving (racking, records shelves, bottle shelves) at a placement, stocked as
        /// <paramref name="stock"/> says while the room's loot budget lasts, else plain.</summary>
        private static void ShelvingAt(RoomCtx c, RaidBuilding.Place p, Vector3 size, int levels, int stockKind, Color frame, string name, StorageOptions stock)
        {
            if (c.TakeLoot()) c.B.Shelving(c.R, p, size, levels, stockKind, frame, name, stock);
            else c.B.Furnish(c.R, p, size, true, (m, r) => FurnitureBuilder.MetalShelf(m, r, size.x, size.z, size.y, levels, stockKind, frame), name);
        }

        /// <summary>How many storage pieces a room of this size is worth, beyond what its recipe gives it.</summary>
        private static int LootCount(RoomCtx c, float perSqm, int max) => Mathf.Clamp(Mathf.FloorToInt(c.Area * perSqm), 0, max);

        private static readonly Color ShelfGrey = new Color(0.40f, 0.40f, 0.38f);
        private static readonly Color Putty = new Color(0.62f, 0.60f, 0.52f);
        private static readonly Color[] LockerPaints = { new Color(0.30f, 0.38f, 0.46f), new Color(0.55f, 0.58f, 0.60f), new Color(0.28f, 0.32f, 0.27f), FurnitureBuilder.Maroon };
        private static readonly Color[] Woods = { FurnitureBuilder.Oak, FurnitureBuilder.Walnut, FurnitureBuilder.Pine };
        private static readonly Color[] Upholstery =
        {
            new Color(0.56f, 0.44f, 0.18f), new Color(0.36f, 0.24f, 0.16f), new Color(0.30f, 0.32f, 0.22f), new Color(0.30f, 0.36f, 0.44f),
            new Color(0.40f, 0.14f, 0.12f), new Color(0.46f, 0.38f, 0.30f),
        };
        private static readonly Color[] Quilts =
        {
            new Color(0.55f, 0.20f, 0.16f), new Color(0.22f, 0.30f, 0.46f), new Color(0.62f, 0.56f, 0.34f), new Color(0.28f, 0.40f, 0.30f),
            new Color(0.50f, 0.40f, 0.52f), new Color(0.70f, 0.66f, 0.58f),
        };

        /// <summary>Loose papers, a mug and pens on a desk top (the desk's frame, its top at 0.76).</summary>
        private static void DeskClutter(RaidMesh m, System.Random r, float w)
        {
            int n = r.Next(1, 4);
            for (int i = 0; i < n; i++)
                m.FlatUV(new Vector3(MapStyle.Range(r, -w * 0.35f, w * 0.35f), 0.762f + i * 0.002f, MapStyle.Range(r, -0.15f, 0.2f)), 0.21f, 0.28f,
                    MapStyle.Range(r, -40f, 40f), DecorMaterials.Paper(r.Next(3)), new Rect(0f, 0f, 1f, 1f));
            if (r.NextDouble() < 0.6)
                m.Cylinder(new Vector3(MapStyle.Range(r, -w * 0.4f, w * 0.4f), 0.76f, MapStyle.Range(r, 0f, 0.2f)), 0.04f, 0.1f, 8,
                    DecorPalette.Gloss(Mugs[r.Next(Mugs.Length)]));
        }

        // A few colours for the small things (each new colour costs the shared palette an upload).
        private static readonly Color[] Mugs =
        {
            new Color(0.86f, 0.84f, 0.78f), new Color(0.62f, 0.16f, 0.12f), new Color(0.20f, 0.32f, 0.52f), new Color(0.30f, 0.44f, 0.30f),
        };

        /// <summary>A few clutter pieces (box stacks, barrels, crates, cardboard boxes) against free walls.</summary>
        private static void Clutter(RoomCtx c, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int k = c.Rng.Next(4);
                int side = c.Rng.Next(4);
                RaidBuilding.Place p;
                switch (k)
                {
                    case 0:
                        if (!c.Wall(side, 0.6f, 0.5f, 0.9f, -1f, out p)) continue;
                        int n = 1 + c.Rng.Next(3);
                        c.B.Furnish(c.R, p, new Vector3(0.6f, 0.4f * n, 0.5f), n > 1, (m, r) => FurnitureBuilder.BoxStack(m, r, Vector3.zero, 0f, n), "boxes");
                        break;
                    case 1:
                        if (!c.Wall(side, 0.62f, 0.62f, 0.9f, -1f, out p)) continue;
                        Color paint = BarrelPaints[c.Rng.Next(BarrelPaints.Length)];
                        c.B.Furnish(c.R, p, new Vector3(0.62f, 0.9f, 0.62f), true, (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, paint), "barrel");
                        break;
                    case 2:
                        if (!c.Wall(side, 0.5f, 0.5f, 0.4f, -1f, out p)) continue;
                        c.B.Furnish(c.R, p, new Vector3(0.5f, 0.35f, 0.5f), false,
                            (m, r) => FurnitureBuilder.CardboardBox(m, r, Vector3.zero, new Vector3(0.48f, 0.34f, 0.4f), (float)r.NextDouble() * 30f - 15f), "box");
                        break;
                    default:
                        if (!c.Wall(side, 0.9f, 0.62f, 0.6f, -1f, out p)) continue;
                        c.B.Furnish(c.R, p, new Vector3(0.9f, 0.55f, 0.62f), true, (m, r) => FurnitureBuilder.Crate(m, r, Vector3.zero, new Vector3(0.88f, 0.5f, 0.58f), 0f, false), "crate");
                        break;
                }
            }
        }

        private static readonly Color[] BarrelPaints =
        {
            new Color(0.16f, 0.26f, 0.42f), new Color(0.45f, 0.24f, 0.10f), new Color(0.24f, 0.28f, 0.18f), new Color(0.12f, 0.12f, 0.13f),
        };

        /// <summary>A picture, a calendar or a notice on one of the room's partitions (kind as <see cref="RaidBuilding.Poster"/>).</summary>
        private static void WallDecor(RoomCtx c, int kind)
        {
            foreach (int side in c.Sides(true))
            {
                if (!c.Inside[side] || c.Len(side) < 1.4f) continue;
                c.Span(side, out float lo, out float hi);
                for (int tries = 0; tries < 6; tries++)
                {
                    float t = Mathf.Lerp(lo + 0.5f, hi - 0.5f, (float)c.Rng.NextDouble());
                    bool clear = true;
                    foreach (Vector2 g in c.Gaps[side]) if (t > g.x - 0.4f && t < g.y + 0.4f) clear = false;
                    // Nothing tall in front of it.
                    Rect probe = c.Foot(side, t, 0.6f, 0.5f);
                    foreach (Rect r in c.Taken) if (r.Overlaps(probe) && Tall(c, r)) clear = false;
                    if (!clear) continue;
                    if (kind == 4) c.B.Clock(c.R, RoomCtx.Char(side), t, 2.05f);
                    else c.B.Poster(c.R, RoomCtx.Char(side), t, 1.55f, kind == 2 ? 0.6f : 0.45f, kind == 2 ? 0.45f : 0.65f, kind);
                    return;
                }
            }
        }

        // Pieces taller than a desk: recorded as tall when they were placed (see Tall below).
        private static bool Tall(RoomCtx c, Rect r) => c.TallRects.Contains(r);
        #endregion
    }
}
