using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // Floors above and below the ground one, and the stairwell between them. A building the survivors can go into is given a
    // floor for each of its storeys (every one, however tall) and, if it's a house of a size to have one (about half of
    // them), a basement under its main block; one switchback stairwell joins them all (LevelLayout). The layout is worked
    // out in the interiors' own frame (InteriorPass.ChooseFrame: the front door on local -Z) on the main block (the largest
    // rectangle of the outline, InteriorPass.Blocks), where a band along one side holds the stair core and the landing hall
    // beside it, the same on every level; the rest of the block and the wings are the rooms'. The building pass draws the
    // floors, the stairs and the stairwell's walls with the building (LevelStructure: they're part of it, with colliders,
    // and in the NavMesh, so the dead follow the survivors up and down); the interiors lay out and furnish every level; the
    // cutaway hides whatever is above the level the survivor is on.
    public static partial class KentuckyMap
    {
        /// <summary>
        /// A building's levels and its stairwell, in its interiors' frame (x along <see cref="Ex"/>, z along <see cref="Ez"/>
        /// from <see cref="Origin"/>, site-local metres; rectangles on wall centre lines, as the interiors' rooms are).
        /// </summary>
        internal sealed class LevelLayout
        {
            public Vector2 Origin, Ex, Ez;
            /// <summary>The main block: the basement's extent, and the rectangle the stair band is taken from.</summary>
            public Rect Main;
            /// <summary>The stair core, the landing hall beside it (the rest of the band) and what's left of the main block
            /// for the rooms.</summary>
            public Rect Core, Hall, Rest;
            /// <summary>The lowest level (-1 with a basement, else 0) and the highest (storeys - 1).</summary>
            public int Lowest, Top;
            /// <summary>Each level's floor, lowest first, then the top storey's ceiling (<see cref="Count"/> + 1 entries).</summary>
            public float[] Floors;
            /// <summary>The core's near corner on its A side (where the flights going up start) and its axes, in the frame:
            /// <see cref="Up"/> runs from the near end (the landing, open to the hall) to the far end (the half landing),
            /// <see cref="Side"/> from the A side (the flight up from each level) to the B side (the flight on up to the next).</summary>
            public Vector2 Corner, Up, Side;
            /// <summary>The core's length along <see cref="Up"/> and width along <see cref="Side"/> (on wall centre lines), and
            /// each flight's run.</summary>
            public float Length, Width, Run;
            /// <summary>The landing's depth at the near end (<see cref="Levels.Landing"/>, or more where the core runs across a
            /// narrow building's whole width).</summary>
            public float Near;
            /// <summary>The core's sides that need a wall of their own (the others are outside walls).</summary>
            public bool WallA, WallB, WallFar, WallNear;
            /// <summary>The landing opens on its B side, onto a hall beside it, rather than at the near end onto a hall in front
            /// of the core: a hall across the building in front of a core across its back, or (<see cref="Narrow"/>) one
            /// beside a core along a narrow building's side.</summary>
            public bool OpenSide;
            /// <summary>A building too narrow for rooms beside its stairs: the core along one side at the back, the hall beside
            /// it (the rest of the width), the rooms across the whole width in front of both.</summary>
            public bool Narrow;

            public int Count => Top - Lowest + 1;
            public bool Basement => Lowest < 0;
            public float FloorOf(int level) => Floors[level - Lowest];
            /// <summary>Where level <paramref name="level"/>'s storey ends: the next floor, or the top storey's ceiling.</summary>
            public float CeilingOf(int level) => Floors[level - Lowest + 1];
            public Vector2 ToSite(Vector2 local) => Origin + Ex * local.x + Ez * local.y;
            public Vector2 ToLocal(Vector2 site)
            {
                Vector2 d = site - Origin;
                return new Vector2(Vector2.Dot(d, Ex), Vector2.Dot(d, Ez));
            }

            /// <summary>A frame direction on the site.</summary>
            public Vector2 DirToSite(Vector2 local) => Ex * local.x + Ez * local.y;

            /// <summary>A point of the core, <paramref name="u"/> along it from the near end and <paramref name="v"/> across it
            /// from the A side, in the frame.</summary>
            public Vector2 CoreLocal(float u, float v) => Corner + Up * u + Side * v;

            /// <summary>The same point on the site.</summary>
            public Vector2 CoreSite(float u, float v) => ToSite(CoreLocal(u, v));

            /// <summary>The opening in every floor above the lowest: the core past its landing.</summary>
            public Rect Shaft => RectOf(CoreLocal(Near, 0f), CoreLocal(Length, Width));

            /// <summary>The level whose storey a point at height <paramref name="y"/> (above the site's ground) is in: the
            /// next one up once the feet are within <paramref name="early"/> of its floor (on the stairs up to it).</summary>
            public int LevelAt(float y, float early)
            {
                int level = Lowest;
                for (int k = Lowest + 1; k <= Top; k++)
                    if (y >= FloorOf(k) - early) level = k;
                return level;
            }

            internal static Rect RectOf(Vector2 a, Vector2 b) => Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>Working out a building's levels, and drawing their floors and stairs (see the top of this file).</summary>
        internal static class Levels
        {
            /// <summary>A flight's width, the wall between the two flights, and the landings' depth: wide enough for the
            /// NavMesh (baked for a 0.5 m agent on 1/6 m voxels, whichever way the building turns) to run up every flight and
            /// round every turn.</summary>
            internal const float FlightWidth = 1.7f, Spine = 0.12f, Landing = 1.7f;
            /// <summary>A step's tread, and the highest a riser may be.</summary>
            internal const float Tread = 0.27f, MaxRiser = 0.19f;
            /// <summary>How deep a basement's floor lies; a floor slab's thickness; the rail round the stairwell at the top.</summary>
            internal const float BasementDepth = 2.7f, Slab = 0.12f, RailHeight = 1.0f;
            /// <summary>The landing hall beside the core is at least this deep (as wide as a flight), and the rooms' share of
            /// the main block at least this wide; a hall beside a narrow building's core at least <see cref="NarrowHall"/>.</summary>
            internal const float MinHall = 1.7f, MinRest = 2.7f, NarrowHall = 1.5f;
            /// <summary>The level-0 floor over a basement stands this high, as the ground floor's own does.</summary>
            internal const float GroundFloorY = 0.02f;
            private const float Half = RaidBuilding.Half;
            private const int SaltBasement = 0xBA5E;

            /// <summary>True for a house that has a basement: a house of a family's size (not an outbuilding, not a trailer, not
            /// a shop), about half of them, chosen by its id so it's the same in every piece and every build. From the data
            /// alone, so the ground (built before the buildings) can leave room for it.</summary>
            internal static bool WantsBasement(Footprint f)
            {
                MapBuilding b = f.Data;
                if (b == null || b.kind != "house" || !string.IsNullOrEmpty(b.use)) return false;
                if (f.Size < 70f || f.Size > 450f) return false;
                return MapStyle.Unit(b.id, SaltBasement) < 0.5f;
            }

            /// <summary>The basement's extent (the main block) on the site, anticlockwise, when the footprint
            /// <see cref="WantsBasement"/> and its main block has room for a stair band; else null.</summary>
            internal static Vector2[] BasementOf(Footprint f)
            {
                if (!WantsBasement(f)) return null;
                Vector2[] block = MainBlock(f);
                if (block.Length < 4) return null;
                // Room for a stairwell in a two-storey house of the usual heights: the building pass decides for good with the
                // real ones (where it can't, the hole under the house is covered by its floor as any other's ground is).
                float side = (block[1] - block[0]).magnitude, other = (block[2] - block[1]).magnitude;
                CoreSize(new[] { -BasementDepth, 0f, 3.0f, 5.9f }, out float length, out float width, out _);
                float wide = Mathf.Max(side, other), deep = Mathf.Min(side, other);
                bool fits = (wide >= length + MinHall && deep - width >= MinRest) || (wide >= length && deep - width - MinHall >= MinRest)
                    || (deep >= length && wide - width - MinHall >= MinRest) || (wide - length >= MinRest && deep - width >= NarrowHall);
                return fits ? block : null;
            }

            /// <summary>The main block of an outline (the largest rectangle in it, less the outside walls), worked out once and
            /// kept on the footprint: its corners on the site, anticlockwise, or an empty array.</summary>
            internal static Vector2[] MainBlock(Footprint f)
            {
                if (f.LevelBlock != null) return f.LevelBlock;
                f.LevelBlock = new Vector2[0];
                if (f.Pts.Count < 3 || f.Size < 9f) return f.LevelBlock;
                InteriorPass.ChooseFrame(f.Pts, new Vector2(0f, -1f), out Vector2 ex, out Vector2 ez);
                Vector2 origin = Origin(f.Pts, ex, ez);
                List<Rect> blocks = InteriorPass.Blocks(Local(f.Pts, origin, ex, ez), Mathf.Max(0.04f, 0.22f - Half), 7f);
                if (blocks.Count == 0) return f.LevelBlock;
                Rect m = blocks[0];
                f.LevelBlock = new[]
                {
                    origin + ex * m.xMin + ez * m.yMin, origin + ex * m.xMax + ez * m.yMin,
                    origin + ex * m.xMax + ez * m.yMax, origin + ex * m.xMin + ez * m.yMax,
                };
                return f.LevelBlock;
            }

            // The frame's origin: the middle of the outline's extent along the frame's axes (as InteriorPass.MakePlan sets it).
            private static Vector2 Origin(List<Vector2> pts, Vector2 ex, Vector2 ez)
            {
                Vector2 mid = MapGeometry.Middle(pts);
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
                foreach (Vector2 p in pts)
                {
                    Vector2 d = p - mid;
                    float x = Vector2.Dot(d, ex), z = Vector2.Dot(d, ez);
                    x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                    z0 = Mathf.Min(z0, z); z1 = Mathf.Max(z1, z);
                }
                return mid + ex * ((x0 + x1) * 0.5f) + ez * ((z0 + z1) * 0.5f);
            }

            private static List<Vector2> Local(List<Vector2> pts, Vector2 origin, Vector2 ex, Vector2 ez)
            {
                var local = new List<Vector2>(pts.Count);
                foreach (Vector2 p in pts)
                {
                    Vector2 d = p - origin;
                    local.Add(new Vector2(Vector2.Dot(d, ex), Vector2.Dot(d, ez)));
                }
                return local;
            }

            // A switchback core's size for floors at these heights: two flights of the tallest storey's half rise (the storeys
            // that have stairs: all but the top one), side by side with the wall between them, a landing at each end (on wall
            // centre lines: Half more on each walled side).
            private static void CoreSize(float[] floors, out float length, out float width, out float run)
            {
                float tallest = 0f;
                for (int i = 0; i + 2 < floors.Length; i++) tallest = Mathf.Max(tallest, floors[i + 1] - floors[i]);
                int risers = Mathf.Max(2, Mathf.CeilToInt(tallest * 0.5f / MaxRiser - 1e-4f));
                run = (risers - 1) * Tread;
                length = Landing + run + Landing + Half;
                width = Half + FlightWidth + Spine + FlightWidth + Half;
            }

            /// <summary>
            /// The levels of an enterable building with <paramref name="storeys"/> storeys (the ground one
            /// <paramref name="firstStorey"/> floor to floor, the others <paramref name="storey"/>; the top one's ceiling at
            /// <paramref name="topCeiling"/>) and a basement when <paramref name="basement"/>; null when it has only the ground
            /// floor, or there's no room for a stairwell clear of its doors.
            /// </summary>
            internal static LevelLayout Plan(EnterableBuilding e, int storeys, float firstStorey, float storey, float topCeiling, bool basement)
            {
                storeys = Mathf.Max(1, storeys);
                if (storeys == 1 && !basement) return null;
                Footprint f = e.Footprint;
                Vector2[] block = MainBlock(f);
                if (block.Length < 4) return null;
                InteriorPass.ChooseFrame(f.Pts, e.Doors.Count > 0 ? e.Doors[0].Outward : new Vector2(0f, -1f), out Vector2 ex, out Vector2 ez);
                Vector2 origin = Origin(f.Pts, ex, ez);
                var L = new LevelLayout { Origin = origin, Ex = ex, Ez = ez, Lowest = basement ? -1 : 0, Top = storeys - 1 };
                Vector2 a = L.ToLocal(block[0]), c = L.ToLocal(block[2]);
                L.Main = LevelLayout.RectOf(a, c);
                L.Floors = new float[L.Count + 1];
                for (int k = L.Lowest; k <= L.Top; k++)
                    L.Floors[k - L.Lowest] = k < 0 ? -BasementDepth : k == 0 ? 0f : firstStorey + (k - 1) * storey;
                L.Floors[L.Count] = Mathf.Max(topCeiling, L.Floors[L.Count - 1] + 2.4f);
                CoreSize(L.Floors, out L.Length, out L.Width, out L.Run);
                L.Near = Landing;
                if (!Band(L, e)) return null;
                Walls(L, f);
                return L;
            }

            // The stair band: along a side of the main block (or its back), the core at the band's far end, the hall in front
            // of it; or, in a building too narrow for that, the core across the whole width of its back with a hall across the
            // building in front of it, which the landing opens onto; or, narrower still, the core along one side at the back
            // with the hall beside it, the landing opening onto that, and the rooms across the whole width in front. The one
            // that leaves the rooms the most room, a house's on the side of its front door (its stair hall), a narrow row's
            // across its back (its shop front whole). None that would put a door into the core (but onto the landing of one
            // that opens on its side), or have the wall between the hall and the rooms meet a door.
            private static bool Band(LevelLayout L, EnterableBuilding e)
            {
                Rect m = L.Main;
                float len = L.Length, wid = L.Width;
                var doors = new List<Vector2>(e.Doors.Count);
                var halves = new List<float>(e.Doors.Count);
                foreach (Doorway d in e.Doors)
                {
                    doors.Add(L.ToLocal(d.At - d.Outward.normalized * 0.4f));
                    halves.Add((d.Width > 0.5f ? d.Width : 1.2f) * 0.5f);
                }
                float doorX = doors.Count > 0 ? doors[0].x : m.center.x;
                bool narrow = m.height > m.width * 1.5f;
                float best = float.MinValue;
                bool found = false;
                for (int option = 0; option < 8; option++)
                {
                    Rect core, hall, rest;
                    Vector2 corner, up, side, cutA, cutB;   // cutA..cutB: the wall between the hall and the rooms
                    bool low = (option & 1) == 0, back = option >= 2, open = option >= 4;
                    float length = len, near = Landing;
                    if (option < 2)
                    {
                        // Along the left (low) or right side, the core at the back, the hall toward the front door.
                        if (m.height < len + MinHall || m.width - wid < MinRest) continue;
                        float x0 = low ? m.xMin : m.xMax - wid, x1 = low ? m.xMin + wid : m.xMax;
                        core = Rect.MinMaxRect(x0, m.yMax - len, x1, m.yMax);
                        hall = Rect.MinMaxRect(x0, m.yMin, x1, m.yMax - len);
                        rest = low ? Rect.MinMaxRect(m.xMin + wid, m.yMin, m.xMax, m.yMax) : Rect.MinMaxRect(m.xMin, m.yMin, m.xMax - wid, m.yMax);
                        corner = new Vector2(low ? m.xMin : m.xMax, m.yMax - len);
                        up = new Vector2(0f, 1f);
                        side = new Vector2(low ? 1f : -1f, 0f);
                        cutA = new Vector2(low ? x1 : x0, m.yMin);
                        cutB = new Vector2(cutA.x, m.yMax - len);
                    }
                    else if (option < 4)
                    {
                        // Across the back, the core at the left (low) or right end, the hall along the rest of it.
                        if (m.width < len + MinHall || m.height - wid < MinRest) continue;
                        float y0 = m.yMax - wid;
                        core = low ? Rect.MinMaxRect(m.xMin, y0, m.xMin + len, m.yMax) : Rect.MinMaxRect(m.xMax - len, y0, m.xMax, m.yMax);
                        hall = low ? Rect.MinMaxRect(m.xMin + len, y0, m.xMax, m.yMax) : Rect.MinMaxRect(m.xMin, y0, m.xMax - len, m.yMax);
                        rest = Rect.MinMaxRect(m.xMin, m.yMin, m.xMax, y0);
                        corner = new Vector2(low ? m.xMin + len : m.xMax - len, m.yMax);
                        up = new Vector2(low ? -1f : 1f, 0f);
                        side = new Vector2(0f, -1f);
                        cutA = new Vector2(hall.xMin, y0);
                        cutB = new Vector2(hall.xMax, y0);
                    }
                    else if (option < 6)
                    {
                        // Across the back from wall to wall, the near landing as long as the width leaves it, opening onto a
                        // hall across the building in front of the core.
                        if (m.width < len || m.height - wid - MinHall < MinRest) continue;
                        float y0 = m.yMax - wid, y1 = y0 - MinHall;
                        length = m.width;
                        near = length - Half - Landing - L.Run;
                        core = Rect.MinMaxRect(m.xMin, y0, m.xMax, m.yMax);
                        hall = Rect.MinMaxRect(m.xMin, y1, m.xMax, y0);
                        rest = Rect.MinMaxRect(m.xMin, m.yMin, m.xMax, y1);
                        corner = new Vector2(low ? m.xMax : m.xMin, m.yMax);
                        up = new Vector2(low ? -1f : 1f, 0f);
                        side = new Vector2(0f, -1f);
                        cutA = new Vector2(m.xMin, y1);
                        cutB = new Vector2(m.xMax, y1);
                    }
                    else
                    {
                        // Along the left (low) or right side at the back, the hall beside it (the rest of the width), the
                        // landing opening onto that, the rooms across the whole width in front.
                        if (m.height - len < MinRest || m.width - wid < NarrowHall) continue;
                        float x0 = low ? m.xMin : m.xMax - wid, x1 = low ? m.xMin + wid : m.xMax, y0 = m.yMax - len;
                        core = Rect.MinMaxRect(x0, y0, x1, m.yMax);
                        hall = low ? Rect.MinMaxRect(x1, y0, m.xMax, m.yMax) : Rect.MinMaxRect(m.xMin, y0, x0, m.yMax);
                        rest = Rect.MinMaxRect(m.xMin, m.yMin, m.xMax, y0);
                        corner = new Vector2(low ? m.xMin : m.xMax, y0);
                        up = new Vector2(0f, 1f);
                        side = new Vector2(low ? 1f : -1f, 0f);
                        cutA = new Vector2(hall.xMin, y0);
                        cutB = new Vector2(hall.xMax, y0);
                    }
                    bool blocked = false;
                    for (int k = 0; k < doors.Count && !blocked; k++)
                    {
                        Vector2 d = doors[k];
                        float u = Vector2.Dot(d - corner, up), v = Vector2.Dot(d - corner, side);
                        bool inCore = u > -0.3f && u < length + 0.3f && v > -0.3f && v < wid + 0.3f;
                        if (inCore && !(open && u < near - halves[k] - 0.1f)) blocked = true;
                        else if (Distance(d, cutA, cutB) < halves[k] + 0.45f) blocked = true;
                    }
                    if (blocked) continue;
                    float score = rest.width * rest.height + Mathf.Min(rest.width, rest.height) * 4f;
                    if (!back && (doorX < m.center.x) == low) score += 25f;   // the stair hall by the front door
                    if (back && narrow) score += 60f;
                    if (back && !open && doors.Count > 0 && Mathf.Abs(doorX - (low ? core.xMax : core.xMin)) < Mathf.Abs(doorX - (low ? core.xMin : core.xMax))) score += 5f;
                    if (open) score -= 20f;   // a hall across the whole building: only where nothing else fits as well
                    if (score <= best) continue;
                    best = score;
                    found = true;
                    L.Core = core;
                    L.Hall = hall;
                    L.Rest = rest;
                    L.Corner = corner;
                    L.Up = up;
                    L.Side = side;
                    L.Length = length;
                    L.Near = near;
                    L.OpenSide = open;
                    L.Narrow = option >= 6;
                }
                return found;
            }

            private static float Distance(Vector2 p, Vector2 a, Vector2 b)
            {
                Vector2 ab = b - a;
                float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                return Vector2.Distance(p, a + ab * t);
            }

            // Which of the core's sides stand against an outside wall (the outline is right behind them) and which need one of
            // their own: the side toward the rooms always does; the others where the outline carries on past (an L's wing),
            // the near end only where the landing opens on its side.
            private static void Walls(LevelLayout L, Footprint f)
            {
                L.WallB = true;
                L.WallA = Beyond(L, f, L.CoreLocal(L.Length * 0.5f, 0f), -L.Side);
                L.WallFar = Beyond(L, f, L.CoreLocal(L.Length, L.Width * 0.5f), L.Up);
                L.WallNear = L.OpenSide && Beyond(L, f, L.CoreLocal(0f, L.Width * 0.5f), -L.Up);
            }

            private static bool Beyond(LevelLayout L, Footprint f, Vector2 local, Vector2 outward)
                => MapGeometry.Contains(f.Pts, L.ToSite(local + outward * 0.45f));

            #region Splitting a building's upper part by storey
            /// <summary>
            /// A leveled building's upper part (everything above the ground storey's walls) split by height into what the
            /// cutaway hides level by level: [0] what stands below level 1's floor (a porch roof, a shop's sign band), [k] the
            /// storey of level k (1 .. Top), and [Top + 1] the roof (above the top storey's ceiling). Walls running across a
            /// floor line are cut there; anything else (a roof's slope, a porch roof, a belt course's top) goes whole with its
            /// middle. Mesh space, as drawn.
            /// </summary>
            internal static RaidMesh[] SplitUpper(RaidMesh upper, LevelLayout L)
            {
                var split = new UpperSplit(upper, L);
                split.Step(int.MaxValue);
                return split.Parts;
            }

            /// <summary><see cref="SplitUpper"/> a slice at a time: a tall building's upper part, its floors' windows and the
            /// rooms' paper round them, runs to more triangles than a streamed piece's step should split at once.</summary>
            internal sealed class UpperSplit
            {
                private readonly RaidMesh _upper;
                private readonly float[] _cuts;
                private readonly List<Vert> _poly = new List<Vert>(8), _below = new List<Vert>(8), _above = new List<Vert>(8);
                private int _s, _t;

                /// <summary>The bands, as <see cref="SplitUpper"/> returns them (whole once <see cref="Step"/> says so).</summary>
                internal readonly RaidMesh[] Parts;

                internal UpperSplit(RaidMesh upper, LevelLayout L)
                {
                    _upper = upper;
                    int bands = L.Top + 2;
                    Parts = new RaidMesh[bands];
                    for (int i = 0; i < bands; i++) Parts[i] = new RaidMesh();
                    _cuts = new float[bands - 1];   // cuts[i]: the bottom of band i + 1
                    for (int k = 1; k <= L.Top; k++) _cuts[k - 1] = L.FloorOf(k);
                    _cuts[bands - 2] = L.CeilingOf(L.Top);
                }

                /// <summary>Splits up to <paramref name="triangles"/> more; true once they're all split.</summary>
                internal bool Step(int triangles)
                {
                    RaidMesh upper = _upper;
                    float[] cuts = _cuts;
                    for (; _s < upper.SubMeshCount; _s++, _t = 0)
                    {
                        Material mat = upper.SubMaterial(_s);
                        int n = upper.SubIndexCount(_s);
                        for (; _t + 2 < n; _t += 3)
                        {
                            if (triangles-- <= 0) return false;
                            int i0 = upper.Index(_s, _t), i1 = upper.Index(_s, _t + 1), i2 = upper.Index(_s, _t + 2);
                            Vector3 a = upper.VertexAt(i0), b = upper.VertexAt(i1), c = upper.VertexAt(i2);
                            float lo = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), hi = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
                            int bandLo = BandOf(cuts, lo + 1e-4f), bandHi = BandOf(cuts, hi - 1e-4f);
                            Vector3 normal = Vector3.Cross(b - a, c - a);
                            bool wall = normal.sqrMagnitude > 1e-12f && Mathf.Abs(normal.normalized.y) < 0.35f;
                            if (bandLo == bandHi || !wall)
                            {
                                int band = bandLo == bandHi ? bandLo : BandOf(cuts, (a.y + b.y + c.y) / 3f);
                                Parts[band].RawTri(mat, a, b, c, upper.NormalAt(i0), upper.NormalAt(i1), upper.NormalAt(i2),
                                    upper.UvAt(i0), upper.UvAt(i1), upper.UvAt(i2));
                                continue;
                            }
                            // A wall across floor lines: cut at each, a band at a time from the bottom.
                            List<Vert> poly = _poly;
                            poly.Clear();
                            poly.Add(new Vert(a, upper.NormalAt(i0), upper.UvAt(i0)));
                            poly.Add(new Vert(b, upper.NormalAt(i1), upper.UvAt(i1)));
                            poly.Add(new Vert(c, upper.NormalAt(i2), upper.UvAt(i2)));
                            for (int band = bandLo; band < bandHi && poly.Count >= 3; band++)
                            {
                                CutAt(poly, cuts[band], _below, _above);
                                Fan(Parts[band], mat, _below);
                                poly.Clear();
                                poly.AddRange(_above);
                            }
                            if (poly.Count >= 3) Fan(Parts[bandHi], mat, poly);
                        }
                    }
                    return true;
                }
            }

            private static int BandOf(float[] cuts, float y)
            {
                int band = 0;
                while (band < cuts.Length && y >= cuts[band]) band++;
                return band;
            }

            private struct Vert
            {
                public Vector3 P, N;
                public Vector2 Uv;
                public Vert(Vector3 p, Vector3 n, Vector2 uv) { P = p; N = n; Uv = uv; }
                public static Vert Lerp(Vert a, Vert b, float t)
                    => new Vert(Vector3.Lerp(a.P, b.P, t), Vector3.Lerp(a.N, b.N, t).normalized, Vector2.Lerp(a.Uv, b.Uv, t));
            }

            // A convex polygon cut by the plane y = h into what's below it and what's above (each in the polygon's own winding).
            private static void CutAt(List<Vert> poly, float h, List<Vert> below, List<Vert> above)
            {
                below.Clear();
                above.Clear();
                for (int i = 0; i < poly.Count; i++)
                {
                    Vert p = poly[i], q = poly[(i + 1) % poly.Count];
                    bool pb = p.P.y < h, qb = q.P.y < h;
                    if (pb) below.Add(p);
                    else above.Add(p);
                    if (pb == qb) continue;
                    var x = Vert.Lerp(p, q, (h - p.P.y) / (q.P.y - p.P.y));
                    below.Add(x);
                    above.Add(x);
                }
            }

            private static void Fan(RaidMesh m, Material mat, List<Vert> poly)
            {
                for (int i = 1; i + 1 < poly.Count; i++)
                    m.RawTri(mat, poly[0].P, poly[i].P, poly[i + 1].P, poly[0].N, poly[i].N, poly[i + 1].N, poly[0].Uv, poly[i].Uv, poly[i + 1].Uv);
            }
            #endregion

            #region Plane geometry
            /// <summary>The parts of a convex polygon (site coordinates) outside a rectangle of the frame, as convex polygons:
            /// before it along z, past it along z, and either side of it within its z span.</summary>
            internal static void OutsideRect(LevelLayout L, List<Vector2> poly, Rect hole, List<List<Vector2>> into)
            {
                var local = new List<Vector2>(poly.Count);
                foreach (Vector2 p in poly) local.Add(L.ToLocal(p));
                for (int region = 0; region < 4; region++)
                {
                    List<Vector2> piece = local;
                    switch (region)
                    {
                        case 0: piece = Clip(piece, new Vector2(0f, -1f), -hole.yMin); break;            // z <= yMin
                        case 1: piece = Clip(piece, new Vector2(0f, 1f), hole.yMax); break;              // z >= yMax
                        case 2:
                            piece = Clip(piece, new Vector2(-1f, 0f), -hole.xMin);                         // x <= xMin
                            piece = Clip(piece, new Vector2(0f, 1f), hole.yMin);                           // z >= yMin
                            piece = Clip(piece, new Vector2(0f, -1f), -hole.yMax);                         // z <= yMax
                            break;
                        default:
                            piece = Clip(piece, new Vector2(1f, 0f), hole.xMax);                           // x >= xMax
                            piece = Clip(piece, new Vector2(0f, 1f), hole.yMin);
                            piece = Clip(piece, new Vector2(0f, -1f), -hole.yMax);
                            break;
                    }
                    if (piece.Count < 3 || Mathf.Abs(MapGeometry.SignedArea(piece)) < 1e-4f) continue;
                    var site = new List<Vector2>(piece.Count);
                    foreach (Vector2 p in piece) site.Add(L.ToSite(p));
                    into.Add(site);
                }
            }

            // The part of a convex polygon where dot(p, n) >= d.
            private static List<Vector2> Clip(List<Vector2> poly, Vector2 n, float d)
            {
                var o = new List<Vector2>(poly.Count + 2);
                for (int i = 0; i < poly.Count; i++)
                {
                    Vector2 p = poly[i], q = poly[(i + 1) % poly.Count];
                    float dp = Vector2.Dot(p, n) - d, dq = Vector2.Dot(q, n) - d;
                    bool pin = dp >= 0f, qin = dq >= 0f;
                    if (pin) o.Add(p);
                    if (pin != qin) o.Add(p + (q - p) * (dp / (dp - dq)));
                }
                return o;
            }

            /// <summary>A rectangle of the frame less a rectangle cut out of it, as up to four rectangles.</summary>
            internal static void RectLess(Rect r, Rect hole, List<Rect> into)
            {
                if (!r.Overlaps(hole))
                {
                    into.Add(r);
                    return;
                }
                float y0 = Mathf.Max(r.yMin, hole.yMin), y1 = Mathf.Min(r.yMax, hole.yMax);
                if (hole.yMin > r.yMin) into.Add(Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, hole.yMin));
                if (hole.yMax < r.yMax) into.Add(Rect.MinMaxRect(r.xMin, hole.yMax, r.xMax, r.yMax));
                if (hole.xMin > r.xMin) into.Add(Rect.MinMaxRect(r.xMin, y0, hole.xMin, y1));
                if (hole.xMax < r.xMax) into.Add(Rect.MinMaxRect(hole.xMax, y0, r.xMax, y1));
            }

            /// <summary>A frame rectangle's corners on the site, clockwise seen from above (a face drawn through them looks up).</summary>
            internal static List<Vector2> Corners(LevelLayout L, Rect r)
                => new List<Vector2> { L.ToSite(new Vector2(r.xMin, r.yMin)), L.ToSite(new Vector2(r.xMin, r.yMax)),
                    L.ToSite(new Vector2(r.xMax, r.yMax)), L.ToSite(new Vector2(r.xMax, r.yMin)) };
            #endregion
        }
    }
}
