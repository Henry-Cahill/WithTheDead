using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>What a room of a real building is for, finer than <see cref="RoomKinds"/> (which the loot tables read): it
    /// picks the room's finishes and the recipe that furnishes it.</summary>
    internal enum RoomRole
    {
        Living, Dining, Kitchen, Bathroom, Bedroom, Hall, Den, Laundry, Closet, HomeGarage,
        Sales, Stock, BackOffice, Restroom, Lobby, Office, OpenOffice, Records, BreakRoom, Vault, Sorting, Stacks, Meeting,
        Eatery, CommercialKitchen, Bar,
        Waiting, Exam, Ward, Dispensary,
        Squad, Cells, Armory,
        Bays, DayRoom, Bunks,
        Narthex, Nave, Sacristy, Chapel,
        Classroom, Gym,
        Warehouse, Workshop, Barn, Greenhouse, Shed, Guest,
        /// <summary>A stairwell: its landing and flights are the building's own (BuildingPass.Levels); nothing is put in it.</summary>
        Stair,
    }

    /// <summary>What a real building's ground floor is used as, read from its mapped kind and use, its size and where it
    /// stands: decides how it's split into rooms.</summary>
    internal enum InteriorUse
    {
        None, Shed, Garage, Cottage, House, Trailer, Apartments, Motel,
        Store, BigStore, Convenience, Pharmacy, Hardware, AutoShop, GasStation, Salon,
        Restaurant, Bar,
        Office, Bank, PostOffice, Library, Civic, Clinic, Hospital, Police, FireStation, Funeral,
        Church, School, Warehouse, Workshop, Barn, Greenhouse, Restrooms,
    }

    /// <summary>
    /// A real building's ground floor laid out in rooms on the footprint's own axes (the frame: <see cref="Ex"/>,
    /// <see cref="Ez"/> round <see cref="Origin"/>, all site-local metres; its main door on local -Z). Room rectangles run
    /// on wall centre lines as <see cref="RaidRoom.Local"/> does: an outside wall's line lies <see cref="RaidBuilding.Half"/>
    /// inside its inner face's outer side, so a piece set against it stands just off the plaster. Pure data, made by
    /// <see cref="InteriorPass"/> while the site is built (deterministic, from the building's id) and built then or later.
    /// </summary>
    internal sealed class InteriorPlan
    {
        public EnterableBuilding Record;
        public InteriorUse Use;
        /// <summary>What the building was for, as the loot tables see it (<see cref="MapGameplay.Purposes"/>).</summary>
        public string Purpose;
        public RaidBuildingKind Kind;
        /// <summary>The building's own name for objects and light circuits ("KY123456").</summary>
        public string Name;
        /// <summary>What a door names the building from inside ("the house", "the store").</summary>
        public string Noun;
        public int Seed;
        /// <summary>Town (dense, on streets) rather than open country.</summary>
        public bool Town;
        /// <summary>How many lamps (ceiling fittings, desk, bedside and standard lamps) the building may have in all, and
        /// how many of them its rooms' ceilings take (see <see cref="PlanRoom.Lamps"/>).</summary>
        public int LampCap, CeilingLamps;
        /// <summary>The most pieces of furniture with a body of their own (an object, a collider, often a fixture) the
        /// building may have: the big ones get plainer as they fill.</summary>
        public int PieceCap;
        public Vector2 Origin, Ex, Ez;
        /// <summary>The frame's turn, as <see cref="RaidBuilding.Yaw"/> counts it.</summary>
        public float Yaw;
        /// <summary>The ceiling's height; the outside walls' thickness.</summary>
        public float WallH, Thick;
        public readonly List<PlanRoom> Rooms = new List<PlanRoom>();
        public readonly List<PlanWall> Walls = new List<PlanWall>();
        public readonly List<PlanEntrance> Entrances = new List<PlanEntrance>();
        public readonly List<PlanWindow> Windows = new List<PlanWindow>();
        /// <summary>The footprint in the frame.</summary>
        public List<Vector2> Local;

        /// <summary>Gets the building's footprint from its record.</summary>
        public Footprint Footprint => Record.Footprint;
        /// <summary>Data about the building from its footprint.</summary>
        public MapBuilding Data => Record.Footprint.Data;
        /// <summary>Unique identifier of the building.</summary>
        public long Id => Record.Footprint.Data.id;

        /// <summary>The level of the building this plan lays out (0 the ground floor, -1 a basement, 1 the floor above it...)
        /// and its floor's height: the rooms are planned and furnished on the ground and lifted there.</summary>
        public int Level;
        public float FloorY;

        /// <summary>What tells this plan's level from the building's others (the streamer's jobs, what was left in it): the
        /// building's id on the ground floor, the id with the level in its top bits on the others.</summary>
        public long Key => Level == 0 ? Id : Id ^ ((long)(Level + 16) << 56);

        /// <summary>Converts a site‑space position to local frame coordinates.</summary>
        public Vector2 ToLocal(Vector2 site)
        {
            Vector2 d = site - Origin;
            return new Vector2(Vector2.Dot(d, Ex), Vector2.Dot(d, Ez));
        }

        /// <summary>Converts a local frame coordinate back to site‑space position.</summary>
        public Vector2 ToSite(Vector2 local) => Origin + Ex * local.x + Ez * local.y;

        /// <summary>Direction vector from a local point towards site space.</summary>
        public Vector2 DirToSite(Vector2 local) => Ex * local.x + Ez * local.y;

        /// <summary>Direction vector from a site point back to local frame axes.</summary>
        public Vector2 DirToLocal(Vector2 site) => new Vector2(Vector2.Dot(site, Ex), Vector2.Dot(site, Ez));

        /// <summary>The rectangle round every room.</summary>
        public Rect Extent
        {
            get
            {
                if (Rooms.Count == 0) return new Rect();
                Rect e = Rooms[0].R;
                for (int i = 1; i < Rooms.Count; i++)
                    e = Rect.MinMaxRect(Mathf.Min(e.xMin, Rooms[i].R.xMin), Mathf.Min(e.yMin, Rooms[i].R.yMin),
                        Mathf.Max(e.xMax, Rooms[i].R.xMax), Mathf.Max(e.yMax, Rooms[i].R.yMax));
                return e;
            }
        }

        /// <summary>The room holding a local point, or -1.</summary>
        public int RoomAt(Vector2 local)
        {
            for (int i = 0; i < Rooms.Count; i++)
                if (Rooms[i].R.Contains(local)) return i;
            return -1;
        }

        /// <summary>The room nearest a local point (by its rectangle), or -1 when there are none.</summary>
        public int NearestRoom(Vector2 local)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < Rooms.Count; i++)
            {
                Rect r = Rooms[i].R;
                float dx = Mathf.Max(0f, Mathf.Max(r.xMin - local.x, local.x - r.xMax));
                float dz = Mathf.Max(0f, Mathf.Max(r.yMin - local.y, local.y - r.yMax));
                float d = dx * dx + dz * dz;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
    }

    /// <summary>One room of a plan.</summary>
    internal sealed class PlanRoom
    {
        public int Index;
        public RoomRole Role;
        /// <summary>The <see cref="RoomKinds"/> kind the loot tables are asked about.</summary>
        public string Kind;
        /// <summary>Name of the room used for debugging or UI.</summary>
        public string Name;
        /// <summary>On wall centre lines, in the frame.</summary>
        public Rect R;
        public WallDesign Design;
        public FloorKind Floor;
        public Color FloorTint = new Color(0.25f, 0.30f, 0.38f);
        public LightStyle Light;
        public bool Lit, Flicker;
        /// <summary>Its ceiling fittings (0: a dark closet, or past the building's lamp budget; no switch then).</summary>
        public int Lamps;
        /// <summary>The kinds of container its storage holds (<see cref="MapGameplay.ContainersFor(Site, Footprint, string)"/>:
        /// the first is its signature piece; empty when it holds no loot), and how many of its pieces should hold loot
        /// (<see cref="MapGameplay.ContainerCount"/>, within the building's budget).</summary>
        public ContainerType[] Pool;
        public int Loot;
        /// <summary>An outside door opens into it.</summary>
        public bool Entry;
        /// <summary>No wall toward these rooms (a hall open to the living room).</summary>
        public readonly List<int> OpenTo = new List<int>(1);
        /// <summary>As <see cref="OpenTo"/>, while the plan is laid out (before the rooms are numbered).</summary>
        public readonly List<PlanRoom> OpenRooms = new List<PlanRoom>(1);
        /// <summary>The flat (or guest room) a room belongs to, -1 for the building's common parts: a flat's rooms open only
        /// onto each other, and only its living room onto the corridor.</summary>
        public int Unit = -1;
        public int Doors;
    }

    /// <summary>A partition between two rooms on a wall centre line, along local X (at z = <see cref="Coord"/>) or along
    /// local Z (at x = <see cref="Coord"/>), from <see cref="A"/> to <see cref="B"/> along it; the room on its - side
    /// and on its + side; and the doorway cut in it, if any.</summary>
    internal sealed class PlanWall
    {
        public bool AlongX;
        public float Coord, A, B;
        public int Minus, Plus;
        /// <summary>Open: the rooms run into each other, no wall at all.</summary>
        public bool Open;
        public bool HasDoor;
        public float DoorPos, DoorWidth;
        /// <summary>How far the rooms' paper runs along it (past A or B where the wall runs on into an outside wall).</summary>
        public float SkinA, SkinB;
        public float Length => B - A;
        public Vector2 Point(float along) => AlongX ? new Vector2(along, Coord) : new Vector2(Coord, along);
    }

    /// <summary>An outside doorway in the frame: its middle on the outside wall's inner face, the way out, its width, and
    /// the room it lets into.</summary>
    internal struct PlanEntrance
    {
        public Vector2 At, Outward;
        public float Width;
        public int Room;
    }

    /// <summary>A ground-floor window in the frame, on the outside wall's inner face from <see cref="A"/> to
    /// <see cref="B"/>, and the room it lights.</summary>
    internal struct PlanWindow
    {
        public Vector2 A, B, Outward;
        public float Sill, Head;
        public int Room;
    }

    internal static partial class InteriorPass
    {
        private const float Half = RaidBuilding.Half;
        /// <summary>Every doorway between rooms: as the raid town's, wide enough for the NavMesh to pass.</summary>
        internal const float DoorWidth = 1.5f;
        /// <summary>Solid wall kept either side of a doorway.</summary>
        private const float Jamb = 0.35f;
        /// <summary>The least wall two rooms share for a doorway between them.</summary>
        private const float DoorRun = DoorWidth + 2f * Jamb;

        #region The frame
        /// <summary>The footprint's own axes: the direction most of its outline runs along (or across), turned a quarter at a
        /// time until its main door looks out along local -Z.</summary>
        private static void ChooseFrame(EnterableBuilding e, out Vector2 ex, out Vector2 ez)
            => ChooseFrame(e.Footprint.Pts, e.Doors.Count > 0 ? e.Doors[0].Outward : new Vector2(0f, -1f), out ex, out ez);

        /// <summary>The frame of an outline (see <see cref="ChooseFrame(EnterableBuilding, out Vector2, out Vector2)"/>) with
        /// its local -Z turned toward <paramref name="outward"/>: the levels' layout uses it before a building has doors.</summary>
        internal static void ChooseFrame(List<Vector2> p, Vector2 outward, out Vector2 ex, out Vector2 ez)
        {
            int n = p.Count;
            float bestScore = -1f, bestAngle = 0f;
            // Evaluate each edge of the footprint as a candidate frame direction – longer edges count more.
            for (int i = 0; i < n; i++)
            {
                Vector2 d = p[(i + 1) % n] - p[i];
                float len = d.magnitude;
                if (len < 0.5f) continue;
                float a = Mod90(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                float score = 0f;
                // Score this edge against all others – favour edges that line up with many long edges
                // Compute a length‑weighted average of edges that are within a few degrees of the best angle
                for (int j = 0; j < n; j++)
                {
                    Vector2 dj = p[(j + 1) % n] - p[j];
                    float lj = dj.magnitude;
                    if (lj < 0.5f) continue;
                    float diff = AngleGap90(Mod90(Mathf.Atan2(dj.y, dj.x) * Mathf.Rad2Deg), a);
                    if (diff < 5f) score += lj * (1f - diff / 10f);
                }
                if (score > bestScore + 1e-3f)
                {
                    bestScore = score;
                    bestAngle = a;
                }
            }
            // Settle on the length-weighted mean of the edges that run that way (within a few degrees).
            float sum = 0f, weight = 0f;
            for (int j = 0; j < n; j++)
            {
                Vector2 dj = p[(j + 1) % n] - p[j];
                float lj = dj.magnitude;
                if (lj < 0.5f) continue;
                float a = Mod90(Mathf.Atan2(dj.y, dj.x) * Mathf.Rad2Deg);
                float off = a - bestAngle;
                if (off > 45f) off -= 90f;
                if (off < -45f) off += 90f;
                if (Mathf.Abs(off) > 5f) continue;
                sum += off * lj;
                weight += lj;
            }
            if (weight > 0f) bestAngle += sum / weight;
            float rad = bestAngle * Mathf.Deg2Rad;
            var baseX = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float best = float.MinValue;
            ex = baseX;
            ez = Perp(baseX);
            // Test the four possible axis orientations and pick the one that points most outward toward the main door
            for (int k = 0; k < 4; k++)
            {
                Vector2 x = Turn90(baseX, k), z = Perp(x);
                float dot = Vector2.Dot(-z, outward);
                if (dot > best + 1e-4f)
                {
                    best = dot;
                    ex = x;
                    ez = z;
                }
            }
        }

        private static float Mod90(float a) => ((a % 90f) + 90f) % 90f;

        private static float AngleGap90(float a, float b)
        {
            float d = Mathf.Abs(a - b);
            return Mathf.Min(d, 90f - d);
        }

        /// <summary>A quarter turn anticlockwise (x toward z).</summary>
        private static Vector2 Perp(Vector2 v) => new Vector2(-v.y, v.x);

        private static Vector2 Turn90(Vector2 v, int k)
        {
            for (int i = 0; i < (k & 3); i++) v = Perp(v);
            return v;
        }
        #endregion

        #region Blocks: the rectangles rooms can fill
        // The largest rectangles of the footprint (less the outside walls), on a grid of `cell` in the frame: the biggest, and
        // a few more that share a doorway's worth of wall with one already taken (an L's leg, a porch room, a garage).
        internal static List<Rect> Blocks(List<Vector2> local, float margin, float minArea)
        {
            var blocks = new List<Rect>(4);
            MapGeometry.Extent(local, out float x0, out float z0, out float x1, out float z1);
            float area = Mathf.Abs(MapGeometry.SignedArea(local));
            float cell = Mathf.Max(0.25f, Mathf.Sqrt(area / 30000f));
            x0 += margin;
            z0 += margin;
            x1 -= margin;
            z1 -= margin;
            if (x1 - x0 < 2f || z1 - z0 < 2f) return blocks;
            int nx = Mathf.Max(1, Mathf.FloorToInt((x1 - x0) / cell + 1e-3f)), nz = Mathf.Max(1, Mathf.FloorToInt((z1 - z0) / cell + 1e-3f));
            // Centre the grid in the extent.
            x0 += ((x1 - x0) - nx * cell) * 0.5f;
            z0 += ((z1 - z0) - nz * cell) * 0.5f;
            int[] owner = Rasterise(local, margin, cell, x0, z0, nx, nz);
            int minCells = Mathf.Max(1, Mathf.CeilToInt(2.0f / cell - 1e-3f));
            int doorCells = Mathf.CeilToInt(DoorRun / cell - 1e-3f);
            var cellRects = new List<RectInt>(4);
            for (int attempt = 0; attempt < 8 && cellRects.Count < 4; attempt++)
            {
                int min = cellRects.Count == 0 ? minCells : Mathf.Max(minCells, Mathf.CeilToInt(2.2f / cell - 1e-3f));
                if (!LargestRect(owner, nx, nz, min, out RectInt r)) break;
                float rArea = r.width * r.height * cell * cell;
                if (cellRects.Count > 0 && rArea < minArea) break;
                bool joined = cellRects.Count == 0;
                for (int k = 0; k < cellRects.Count && !joined; k++) joined = SharedCells(cellRects[k], r) >= doorCells;
                int mark = joined ? cellRects.Count : -3;
                for (int j = r.yMin; j < r.yMax; j++)
                    for (int i = r.xMin; i < r.xMax; i++) owner[j * nx + i] = mark;
                if (joined) cellRects.Add(r);
            }
            foreach (RectInt r in cellRects)
            {
                var rect = Rect.MinMaxRect(x0 + r.xMin * cell, z0 + r.yMin * cell, x0 + r.xMax * cell, z0 + r.yMax * cell);
                blocks.Add(Grow(rect, r, owner, nx, nz, local, margin, cell));
            }
            return blocks;
        }

        // -1 outside (or within `margin` of the outline), -2 free inside; a block's index once it's taken.
        /// <summary>Rasterises the building footprint into a grid of cells marking interior free space and outside margin.</summary>
        private static int[] Rasterise(List<Vector2> local, float margin, float cell, float x0, float z0, int nx, int nz)
        {
            var owner = new int[nx * nz];
            var spans = new List<float>(16);
            var a = new List<Vector2>(8);
            var b = new List<Vector2>(8);
            var c = new List<Vector2>(8);
            float reach = cell * 0.5f + margin;
            // For each grid row, gather intervals at three nearby Z positions (center and +/- reach) to model wall thickness and margin
            for (int j = 0; j < nz; j++)
            {
                float zc = z0 + (j + 0.5f) * cell;
                Intervals(local, zc - reach, spans, a);
                Intervals(local, zc, spans, b);
                Intervals(local, zc + reach, spans, c);
                Intersect(a, b);
                Intersect(a, c);
                // Determine if each cell lies inside the polygon after applying the margin
                for (int i = 0; i < nx; i++)
                {
                    float xa = x0 + i * cell, xb = xa + cell;
                    bool inside = false;
                    for (int k = 0; k < a.Count && !inside; k++) inside = xa >= a[k].x + margin - 1e-4f && xb <= a[k].y - margin + 1e-4f;
                    owner[j * nx + i] = inside ? -2 : -1;
                }
            }
            return owner;
        }

        // The stretches of the line z = `z` inside the polygon (even-odd), as (from, to) pairs.
        private static void Intervals(List<Vector2> p, float z, List<float> xs, List<Vector2> into)
        {
            xs.Clear();
            into.Clear();
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            {
                Vector2 u = p[j], v = p[i];
                if ((u.y > z) == (v.y > z)) continue;
                xs.Add(u.x + (z - u.y) * (v.x - u.x) / (v.y - u.y));
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2) into.Add(new Vector2(xs[k], xs[k + 1]));
        }

        private static readonly List<Vector2> s_Cut = new List<Vector2>(8);

        private static void Intersect(List<Vector2> a, List<Vector2> b)
        {
            s_Cut.Clear();
            foreach (Vector2 p in a)
                foreach (Vector2 q in b)
                {
                    float lo = Mathf.Max(p.x, q.x), hi = Mathf.Min(p.y, q.y);
                    if (hi > lo) s_Cut.Add(new Vector2(lo, hi));
                }
            a.Clear();
            a.AddRange(s_Cut);
        }

        // The biggest rectangle of free cells (-2) at least `min` cells each way (the histogram method).
        /// <summary>Finds the largest axis‑aligned rectangle of free cells in the rasterised grid meeting a minimum size.</summary>
        private static bool LargestRect(int[] owner, int nx, int nz, int min, out RectInt best)
        {
            best = default;
            int bestArea = 0;
            var h = new int[nx];
            var stack = new int[nx + 1];
            for (int j = 0; j < nz; j++)
            {
                // Build a histogram of consecutive free cells (-2) for each column up to the current row
                for (int i = 0; i < nx; i++) h[i] = owner[j * nx + i] == -2 ? h[i] + 1 : 0;
                int top = 0;
                // Process the histogram as a stack to find the maximal rectangle ending at this row
                for (int i = 0; i <= nx; i++)
                {
                    int cur = i < nx ? h[i] : 0;
                    while (top > 0 && h[stack[top - 1]] >= cur)
                    {
                        // Compute rectangle dimensions from popped bar and update best if larger than previous best
                        int height = h[stack[--top]];
                        // Determine left boundary of rectangle based on new stack top
                        int left = top > 0 ? stack[top - 1] + 1 : 0;
                        // Compute rectangle width from current column index and left boundary
                        int width = i - left;
                        if (height >= min && width >= min && width * height > bestArea)
                        {
                            bestArea = width * height;
                            best = new RectInt(left, j - height + 1, width, height);
                        }
                    }
                    stack[top++] = i;
                }
            }
            return bestArea > 0;
        }

        // How many cells of wall two cell rectangles share.
        private static int SharedCells(RectInt a, RectInt b)
        {
            if (a.xMax == b.xMin || b.xMax == a.xMin) return Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            if (a.yMax == b.yMin || b.yMax == a.yMin) return Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
            return 0;
        }

        // A block's sides that face the outside walls (nothing usable beyond them) pushed out to the walls themselves (the
        // grid leaves up to a cell short).
        private static Rect Grow(Rect rect, RectInt r, int[] owner, int nx, int nz, List<Vector2> local, float margin, float cell)
        {
            float limit = cell + 0.05f;
            for (int side = 0; side < 4; side++)
            {
                if (!OutsideBeyond(r, owner, nx, nz, side)) continue;
                float lo = 0f, hi = limit;
                for (int it = 0; it < 7; it++)
                {
                    float mid = (lo + hi) * 0.5f;
                    if (SideClear(Moved(rect, side, mid), side, local, margin)) lo = mid;
                    else hi = mid;
                }
                rect = Moved(rect, side, lo);
            }
            return rect;
        }

        // 0 south (-z), 1 east (+x), 2 north (+z), 3 west (-x).
        private static Rect Moved(Rect r, int side, float by)
        {
            switch (side)
            {
                case 0: return Rect.MinMaxRect(r.xMin, r.yMin - by, r.xMax, r.yMax);
                case 1: return Rect.MinMaxRect(r.xMin, r.yMin, r.xMax + by, r.yMax);
                case 2: return Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, r.yMax + by);
                default: return Rect.MinMaxRect(r.xMin - by, r.yMin, r.xMax, r.yMax);
            }
        }

        private static bool OutsideBeyond(RectInt r, int[] owner, int nx, int nz, int side)
        {
            for (int k = 0; ; k++)
            {
                int i, j;
                switch (side)
                {
                    case 0: i = r.xMin + k; j = r.yMin - 1; if (i >= r.xMax) return true; break;
                    case 1: i = r.xMax; j = r.yMin + k; if (j >= r.yMax) return true; break;
                    case 2: i = r.xMin + k; j = r.yMax; if (i >= r.xMax) return true; break;
                    default: i = r.xMin - 1; j = r.yMin + k; if (j >= r.yMax) return true; break;
                }
                if (i < 0 || j < 0 || i >= nx || j >= nz) continue;
                if (owner[j * nx + i] != -1) return false;
            }
        }

        // True when the side of `r` lies inside the footprint at least `margin` from its outline.
        private static bool SideClear(Rect r, int side, List<Vector2> local, float margin)
        {
            Vector2 a, b;
            switch (side)
            {
                case 0: a = new Vector2(r.xMin, r.yMin); b = new Vector2(r.xMax, r.yMin); break;
                case 1: a = new Vector2(r.xMax, r.yMin); b = new Vector2(r.xMax, r.yMax); break;
                case 2: a = new Vector2(r.xMin, r.yMax); b = new Vector2(r.xMax, r.yMax); break;
                default: a = new Vector2(r.xMin, r.yMin); b = new Vector2(r.xMin, r.yMax); break;
            }
            int steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.4f));
            for (int s = 0; s <= steps; s++)
            {
                Vector2 q = Vector2.Lerp(a, b, s / (float)steps);
                if (!MapGeometry.Contains(local, q) || MapGeometry.DistanceToEdge(local, q) < margin - 0.005f) return false;
            }
            return true;
        }
        #endregion

        #region Splitting rectangles
        /// <summary>Cuts <paramref name="r"/> across X into parts as wide as <paramref name="sizes"/> say, from its west end
        /// (or its east end when <paramref name="fromEast"/>): a positive size is metres, a negative one a share of the whole,
        /// zero takes what's left (once). No part is left narrower than <paramref name="min"/> when that can be helped.</summary>
        private static Rect[] SplitX(Rect r, bool fromEast, float min, params float[] sizes)
        {
            float[] w = Sizes(r.width, min, sizes);
            var parts = new Rect[w.Length];
            float at = fromEast ? r.xMax : r.xMin;
            for (int i = 0; i < w.Length; i++)
            {
                float next = fromEast ? at - w[i] : at + w[i];
                if (i == w.Length - 1) next = fromEast ? r.xMin : r.xMax;
                parts[i] = Rect.MinMaxRect(Mathf.Min(at, next), r.yMin, Mathf.Max(at, next), r.yMax);
                at = next;
            }
            return parts;
        }

        /// <summary>As <see cref="SplitX"/>, across Z from the front (south) end, or from the back when
        /// <paramref name="fromBack"/>.</summary>
        private static Rect[] SplitZ(Rect r, bool fromBack, float min, params float[] sizes)
        {
            float[] w = Sizes(r.height, min, sizes);
            var parts = new Rect[w.Length];
            float at = fromBack ? r.yMax : r.yMin;
            for (int i = 0; i < w.Length; i++)
            {
                float next = fromBack ? at - w[i] : at + w[i];
                if (i == w.Length - 1) next = fromBack ? r.yMin : r.yMax;
                parts[i] = Rect.MinMaxRect(r.xMin, Mathf.Min(at, next), r.xMax, Mathf.Max(at, next));
                at = next;
            }
            return parts;
        }

        // The parts' lengths: metres, shares and the rest, squeezed so they add up to `total` and (where they can) none is
        // under `min`.
        private static float[] Sizes(float total, float min, float[] sizes)
        {
            var w = new float[sizes.Length];
            int rest = -1;
            float fixedSum = 0f;
            for (int i = 0; i < sizes.Length; i++)
            {
                if (sizes[i] > 0f) w[i] = sizes[i];
                else if (sizes[i] < 0f) w[i] = -sizes[i] * total;
                else if (rest < 0) rest = i;
                fixedSum += w[i];
            }
            if (rest < 0) rest = sizes.Length - 1;
            fixedSum -= w[rest];
            w[rest] = total - fixedSum;
            // Too little left for the rest: take it back from the others, the biggest first.
            for (int guard = 0; guard < 16 && w[rest] < min; guard++)
            {
                int big = -1;
                for (int i = 0; i < w.Length; i++)
                    if (i != rest && w[i] > min && (big < 0 || w[i] > w[big])) big = i;
                if (big < 0) break;
                float give = Mathf.Min(min - w[rest], w[big] - min);
                w[big] -= give;
                w[rest] += give;
            }
            return w;
        }
        #endregion

        #region Walls and doorways
        // Every stretch of wall two rooms share, a partition, with the doorways the rooms need: from the room the front door
        // opens into, the cheapest way to every other room (by what connects to what in a home or a workplace), then the
        // extra doors the plan asks for.
        private static void Partition(InteriorPlan plan, System.Random rng)
        {
            List<PlanRoom> rooms = plan.Rooms;
            for (int a = 0; a < rooms.Count; a++)
                for (int b = a + 1; b < rooms.Count; b++)
                    Shared(plan, a, b);
            int entry = 0;
            for (int i = 0; i < rooms.Count; i++)
                if (rooms[i].Entry) { entry = i; break; }

            // Prim's: connect the rooms outward from the entry, the cheapest door first.
            var reached = new bool[rooms.Count];
            reached[entry] = true;
            int count = 1;
            foreach (PlanWall w in plan.Walls)
                if (w.Open) Reach(plan, w, reached, ref count);
            while (count < rooms.Count)
            {
                PlanWall bestWall = null;
                float bestCost = float.MaxValue;
                foreach (PlanWall w in plan.Walls)
                {
                    if (w.Open || w.HasDoor || reached[w.Minus] == reached[w.Plus] || w.Length < DoorRun - 1e-3f) continue;
                    PlanRoom from = rooms[reached[w.Minus] ? w.Minus : w.Plus], to = rooms[reached[w.Minus] ? w.Plus : w.Minus];
                    if (!Allowed(from, to)) continue;
                    float cost = DoorCost(from, to) + from.Doors * 0.3f + 0.01f * (float)rng.NextDouble();
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestWall = w;
                    }
                }
                if (bestWall == null) break;
                CutDoor(plan, bestWall);
                if (!reached[bestWall.Minus]) { reached[bestWall.Minus] = true; count++; }
                if (!reached[bestWall.Plus]) { reached[bestWall.Plus] = true; count++; }
                foreach (PlanWall w in plan.Walls)
                    if (w.Open) Reach(plan, w, reached, ref count);
            }
            // Second doors where people expect them: the kitchen from the living room, the stock room from the shop floor.
            foreach (PlanWall w in plan.Walls)
            {
                if (w.Open || w.HasDoor || w.Length < DoorRun - 1e-3f) continue;
                PlanRoom a = rooms[w.Minus], b = rooms[w.Plus];
                if (Allowed(a, b) && WantsSecondDoor(a, b) && a.Doors < 3 && b.Doors < 3 && !Linked(plan, w.Minus, w.Plus)) CutDoor(plan, w);
            }
            // Rooms nothing reaches (too little wall for a doorway): open into the neighbour they share the most with.
            for (int i = 0; i < rooms.Count; i++)
            {
                if (reached[i]) continue;
                PlanWall best = null;
                foreach (PlanWall w in plan.Walls)
                    if (((w.Minus == i && reached[w.Plus]) || (w.Plus == i && reached[w.Minus])) && Allowed(rooms[w.Minus], rooms[w.Plus]))
                        if (best == null || w.Length > best.Length) best = w;
                if (best == null) continue;
                best.Open = true;
                rooms[best.Minus].OpenTo.Add(best.Plus);
                rooms[best.Plus].OpenTo.Add(best.Minus);
                reached[i] = true;
            }
        }

        // A flat's rooms open only onto each other, and only its living room (or a guest room) onto the common parts.
        private static bool Allowed(PlanRoom a, PlanRoom b)
        {
            if (a.Unit == b.Unit) return true;
            if (a.Unit >= 0 && b.Unit >= 0) return false;
            PlanRoom u = a.Unit >= 0 ? a : b;
            return u.Role == RoomRole.Living || u.Role == RoomRole.Guest;
        }

        private static void Reach(InteriorPlan plan, PlanWall w, bool[] reached, ref int count)
        {
            if (reached[w.Minus] == reached[w.Plus]) return;
            reached[w.Minus] = reached[w.Plus] = true;
            count++;
        }

        // The wall two rooms share, if any: added to the plan.
        /// <summary>Adds a wall segment between two rooms if they share a side, and records whether it is open (no wall) based on room openness.</summary>
        // When a shared wall is found, decide which side is west/east or south/north and store its coordinates
        private static void Shared(InteriorPlan plan, int a, int b)
        {
            Rect ra = plan.Rooms[a].R, rb = plan.Rooms[b].R;
            const float eps = 0.02f;
            PlanWall w = null;
            if (Mathf.Abs(ra.xMax - rb.xMin) < eps || Mathf.Abs(rb.xMax - ra.xMin) < eps)
            {
                float lo = Mathf.Max(ra.yMin, rb.yMin), hi = Mathf.Min(ra.yMax, rb.yMax);
                if (hi - lo > 0.3f)
                {
                    bool aWest = Mathf.Abs(ra.xMax - rb.xMin) < eps;
                    w = new PlanWall { AlongX = false, Coord = aWest ? ra.xMax : ra.xMin, A = lo, B = hi, Minus = aWest ? a : b, Plus = aWest ? b : a };
                }
            }
            else if (Mathf.Abs(ra.yMax - rb.yMin) < eps || Mathf.Abs(rb.yMax - ra.yMin) < eps)
            {
                float lo = Mathf.Max(ra.xMin, rb.xMin), hi = Mathf.Min(ra.xMax, rb.xMax);
                if (hi - lo > 0.3f)
                {
                    bool aSouth = Mathf.Abs(ra.yMax - rb.yMin) < eps;
                    w = new PlanWall { AlongX = true, Coord = aSouth ? ra.yMax : ra.yMin, A = lo, B = hi, Minus = aSouth ? a : b, Plus = aSouth ? b : a };
                }
            }
            if (w == null) return;
            // A stairwell has its own walls (the building's): only its open side onto the landing hall is in the plan.
            bool stair = plan.Rooms[a].Role == RoomRole.Stair || plan.Rooms[b].Role == RoomRole.Stair;
            w.Open = plan.Rooms[a].OpenTo.Contains(b) || plan.Rooms[b].OpenTo.Contains(a);
            if (stair && !w.Open) return;
            plan.Walls.Add(w);
        }

        private static bool Linked(InteriorPlan plan, int a, int b)
        {
            foreach (PlanWall w in plan.Walls)
                if ((w.Open || w.HasDoor) && ((w.Minus == a && w.Plus == b) || (w.Minus == b && w.Plus == a))) return true;
            return false;
        }

        // A doorway in the wall: by the end nearer the front door (the way people come in), clear of the jambs.
        private static void CutDoor(InteriorPlan plan, PlanWall w)
        {
            float lo = w.A + Jamb + DoorWidth * 0.5f, hi = w.B - Jamb - DoorWidth * 0.5f;
            Vector2 entry = plan.Entrances.Count > 0 ? plan.Entrances[0].At : Vector2.zero;
            float toward = w.AlongX ? entry.x : entry.y;
            float pos;
            if (hi - lo < 0.01f) pos = (lo + hi) * 0.5f;
            else
            {
                // A short wall gets its door in the middle; a long one by its end nearer the way in, a little in from the corner.
                bool nearLo = Mathf.Abs(toward - w.A) <= Mathf.Abs(toward - w.B);
                float inset = Mathf.Min(0.25f, (hi - lo) * 0.5f);
                pos = w.Length < 3.6f ? (lo + hi) * 0.5f : nearLo ? lo + inset : hi - inset;
            }
            w.HasDoor = true;
            w.DoorPos = pos;
            w.DoorWidth = DoorWidth;
            plan.Rooms[w.Minus].Doors++;
            plan.Rooms[w.Plus].Doors++;
        }

        // What a door between two rooms costs (cheaper is likelier): halls and corridors open onto everything, a living room
        // onto the kitchen, a bedroom onto a hall before anything else; nobody walks through a bathroom.
        private static float DoorCost(PlanRoom from, PlanRoom to)
        {
            RoomRole a = from.Role, b = to.Role;
            if (Circulation(a) && Circulation(b)) return 1f;
            if (Circulation(a) || Circulation(b))
            {
                RoomRole other = Circulation(a) ? b : a;
                return Dead(other) ? 1.2f : 1.5f;
            }
            if (Pair(a, b, RoomRole.Living, RoomRole.Kitchen) || Pair(a, b, RoomRole.Living, RoomRole.Dining)
                || Pair(a, b, RoomRole.Dining, RoomRole.Kitchen)) return 1.4f;
            if (Pair(a, b, RoomRole.Sales, RoomRole.Stock) || Pair(a, b, RoomRole.Eatery, RoomRole.CommercialKitchen)) return 1.4f;
            if (Dead(b) && Dead(a)) return 9f;
            if (b == RoomRole.Bathroom || b == RoomRole.Restroom)
                return a == RoomRole.Kitchen || a == RoomRole.Eatery || a == RoomRole.Sales ? 7f : a == RoomRole.Bedroom ? 5f : 4f;
            if (b == RoomRole.Bedroom) return a == RoomRole.Living ? 3f : a == RoomRole.Kitchen ? 4.5f : 4f;
            if (Dead(a)) return 8f;
            return 3f;
        }

        private static bool Pair(RoomRole a, RoomRole b, RoomRole x, RoomRole y) => (a == x && b == y) || (a == y && b == x);

        // Rooms people pass through to get elsewhere.
        private static bool Circulation(RoomRole r) => r == RoomRole.Hall || r == RoomRole.Lobby || r == RoomRole.Narthex || r == RoomRole.Waiting;

        // Rooms at the end of the line: never a way through.
        private static bool Dead(RoomRole r) => r == RoomRole.Bathroom || r == RoomRole.Restroom || r == RoomRole.Closet || r == RoomRole.Vault
            || r == RoomRole.Cells || r == RoomRole.Bedroom || r == RoomRole.Guest || r == RoomRole.Laundry;

        private static bool WantsSecondDoor(PlanRoom a, PlanRoom b)
        {
            RoomRole x = a.Role, y = b.Role;
            return Pair(x, y, RoomRole.Living, RoomRole.Kitchen) || Pair(x, y, RoomRole.Dining, RoomRole.Kitchen)
                || Pair(x, y, RoomRole.Sales, RoomRole.Stock) || Pair(x, y, RoomRole.Eatery, RoomRole.CommercialKitchen)
                || (Circulation(x) && !Dead(y) && y != RoomRole.Bedroom) || (Circulation(y) && !Dead(x) && x != RoomRole.Bedroom);
        }
        #endregion
    }
}
