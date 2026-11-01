using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;
using System;

namespace CloneZ
{
    // The inside of a building's outside walls, drawn into its shell while the site is built (cheap geometry, so every Full
    // site gets it at once, furnished or not): each stretch of wall papered as the room it bounds, with skirting, rail and
    // cornice, round the doorways; and the inside of every ground-floor window, its casing, sill, sashes and glass and what
    // the room hangs there. The shell's walls are what the isometric cutaway lifts, so this goes with them.
    internal static partial class InteriorPass
    {
        private const float LiningOff = 0.004f;   // the paper's distance in front of the wall's inner face
        private const float DoorTop = 2.2f;       // the outside doorways' head (the exterior pass's doors are a little lower)
        private const float SampleStep = 0.25f;

        private static readonly Color Casing = new Color(0.40f, 0.30f, 0.20f);
        private static readonly Color SashPaint = new Color(0.78f, 0.76f, 0.70f);
        private static readonly Color PaneTint = new Color(0.16f, 0.20f, 0.25f);
        private static readonly Color[] Fabrics =
        {
            new Color(0.62f, 0.40f, 0.42f), new Color(0.66f, 0.52f, 0.22f), new Color(0.46f, 0.54f, 0.42f), new Color(0.20f, 0.26f, 0.40f),
            new Color(0.45f, 0.14f, 0.16f), new Color(0.72f, 0.64f, 0.50f), new Color(0.46f, 0.56f, 0.66f), new Color(0.42f, 0.30f, 0.22f),
        };
        private static readonly Color[] StainedGlass =
        {
            new Color(0.55f, 0.16f, 0.12f), new Color(0.16f, 0.26f, 0.52f), new Color(0.70f, 0.52f, 0.14f), new Color(0.20f, 0.40f, 0.22f),
        };

        private static readonly List<int> s_Rooms = new List<int>(128);
        private static readonly List<float> s_At = new List<float>(128);

        /// <summary>The plan's outside walls lined from inside and its windows dressed, into <paramref name="shell"/>'s walls:
        /// a wall at a time (each step one wall, so the caller can spread a big building over frames). A ground floor's go
        /// into the ground storey's walls, an upper floor's into the upper part's at its floor's height (the cutaway splits
        /// that by level); a basement's walls are bare concrete.</summary>
        private static IEnumerable<bool> Linings(InteriorPlan plan, BuildingShell shell)
        {
            if (plan.Level < 0) yield break;
            List<Vector2> p = plan.Footprint.Pts;
            int n = p.Count;
            float t = plan.Thick, h = plan.WallH;
            var rng = new System.Random(unchecked(plan.Seed * 31 + 7));
            Color fabric = Fabrics[rng.Next(Fabrics.Length)];
            for (int i = 0; i < n; i++)
            {
                if (i > 0) yield return true;
                Vector2 a = p[i], c = p[(i + 1) % n];
                float len = Vector2.Distance(a, c);
                if (len < 0.05f) continue;
                Vector2 d = (c - a) / len, outward = new Vector2(d.y, -d.x);
                Vector2 prevD = (p[i] - p[(i + n - 1) % n]).normalized, nextD = (p[(i + 2) % n] - c).normalized;
                float s0 = InnerEnd(prevD, d, t), s1 = len - InnerEnd(d, nextD, t);
                if (s1 - s0 < 0.1f) continue;
                RaidMesh m = plan.Level == 0 ? shell.WallsFacing(outward) : shell.Upper;
                if (m == null) yield break;
                if (plan.Level != 0) m.SetFrame(new Vector3(0f, plan.FloorY, 0f), 0f);
                float yaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                Vector3 inward = new Vector3(-outward.x, 0f, -outward.y);

                // Which room each bit of the wall bounds.
                s_Rooms.Clear();
                s_At.Clear();
                int steps = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / SampleStep));
                for (int k = 0; k <= steps; k++)
                {
                    float s = Mathf.Lerp(s0, s1, k / (float)steps);
                    Vector2 inside = plan.ToLocal(a + d * s - outward * (t + 0.35f));
                    int room = plan.RoomAt(inside);
                    if (room < 0)
                    {
                        room = plan.NearestRoom(inside);
                        if (room >= 0 && Distance(plan.Rooms[room].R, inside) > 1.2f) room = -1;
                    }
                    s_Rooms.Add(room);
                    s_At.Add(s);
                }
                int start = 0;
                for (int k = 1; k <= steps + 1; k++)
                {
                    if (k <= steps && s_Rooms[k] == s_Rooms[start]) continue;
                    int room = s_Rooms[start];
                    float from = start == 0 ? s0 : Boundary(plan, a, d, outward, t, s_At[start - 1], s_At[start], s_Rooms[start - 1], room);
                    float to = k > steps ? s1 : Boundary(plan, a, d, outward, t, s_At[k - 1], s_At[k], room, s_Rooms[k]);
                    if (room >= 0 && plan.Rooms[room].Design != null && to - from > 0.02f)
                        Stretch(plan, m, plan.Rooms[room], i, a, d, outward, inward, yaw, from, to, t, h);
                    start = k;
                }

                // The windows on this wall, from inside.
                foreach (WindowSpan w in plan.Record.Windows)
                {
                    if (w.Storey != plan.Level || w.Edge != i) continue;
                    Vector2 mid = a + d * ((w.From + w.To) * 0.5f) - outward * (t + 0.35f);
                    int room = plan.RoomAt(plan.ToLocal(mid));
                    if (room < 0) room = plan.NearestRoom(plan.ToLocal(mid));
                    PlanRoom r = room >= 0 ? plan.Rooms[room] : null;
                    if (r != null && r.Design == null) continue;
                    Window(plan, m, r, a, d, inward, yaw, w, t, fabric, rng);
                }
                if (plan.Level != 0) m.ResetFrame();
            }
        }

        // How far along an edge its inner face starts, where the previous edge (direction `before`) meets it (direction
        // `after`): the wall thickness at a square corner, t·tan(turn/2) in general (negative at an inward corner).
        private static float InnerEnd(Vector2 before, Vector2 after, float t)
        {
            float cross = before.x * after.y - before.y * after.x, dot = Vector2.Dot(before, after);
            if (Mathf.Abs(cross) < 0.02f && dot > 0f) return 0f;   // straight on
            float along = t * cross / Mathf.Max(0.05f, 1f + dot);
            return Mathf.Clamp(along, -2f * t, 4f * t);
        }

        private static float Distance(Rect r, Vector2 q)
        {
            float dx = Mathf.Max(0f, Mathf.Max(r.xMin - q.x, q.x - r.xMax));
            float dz = Mathf.Max(0f, Mathf.Max(r.yMin - q.y, q.y - r.yMax));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Where along the wall one room's paper gives way to the next's: on the partition's line when the two share one,
        // else halfway between the samples.
        private static float Boundary(InteriorPlan plan, Vector2 a, Vector2 d, Vector2 outward, float t, float sa, float sb, int ra, int rb)
        {
            if (ra >= 0 && rb >= 0)
                foreach (PlanWall w in plan.Walls)
                {
                    if (!((w.Minus == ra && w.Plus == rb) || (w.Minus == rb && w.Plus == ra))) continue;
                    Vector2 la = plan.ToLocal(a + d * sa - outward * t), lb = plan.ToLocal(a + d * sb - outward * t);
                    float ca = w.AlongX ? la.y : la.x, cb = w.AlongX ? lb.y : lb.x;
                    if (Mathf.Abs(cb - ca) < 1e-4f) break;
                    float k = (w.Coord - ca) / (cb - ca);
                    if (k >= -0.5f && k <= 1.5f) return Mathf.Lerp(sa, sb, Mathf.Clamp01(k));
                }
            return (sa + sb) * 0.5f;
        }

        // Stretch the wall's paper from `from` to `to`, round the openings through it there: the outside doorways (the ground
        // floor's, with their casings and hardware) and the working and fixed windows (their casings are Window's).
        private static void Stretch(InteriorPlan plan, RaidMesh m, PlanRoom room, int edge, Vector2 a, Vector2 d, Vector2 outward,
            Vector3 inward, float yaw, float from, float to, float t, float h)
        {
            WallDesign design = room.Design;
            Surf paper = DecorMaterials.WallInterior(design);
            Surf trim = DecorPalette.Matte(design.Trim);
            // The openings along this stretch, (middle, half width, bottom, top), in order.
            s_Openings.Clear();
            if (plan.Level == 0)
                foreach (Doorway door in plan.Record.Doors)
                {
                    if (door.Edge != edge) continue;
                    float s = Vector2.Dot(door.At - a, d), half = (door.Width > 0.5f ? door.Width : 1.2f) * 0.5f;
                    if (s + half > from + 0.01f && s - half < to) s_Openings.Add(new Vector4(s, half, 0f, DoorTop));
                }
            foreach (WindowSpan w in plan.Record.Windows)
            {
                if (!(w.Working || w.Clear) || w.Storey != plan.Level || w.Edge != edge || w.To <= from + 0.01f || w.From >= to) continue;
                s_Openings.Add(new Vector4((w.From + w.To) * 0.5f, (w.To - w.From) * 0.5f, w.Sill, w.Head));
            }
            if (s_Openings.Count > 1) s_Openings.Sort(s_ByMiddle);
            // The paper goes round the openings in panels; the skirting runs the stretch in one piece but for its doorways, the
            // cornice in one but for an opening reaching up into it (a run of them costs no more than a plain wall's).
            float cursor = from, skirt = from, cornice = from;
            foreach (Vector4 o in s_Openings)
            {
                float o0 = Mathf.Max(cursor, o.x - o.y), o1 = Mathf.Min(to, o.x + o.y);
                if (o1 <= o0) continue;
                bool door = o.z <= 0.02f;
                Panel(m, a, d, outward, inward, yaw, cursor, o0, 0f, h, t, paper, trim, design, false);
                if (!door) Panel(m, a, d, outward, inward, yaw, o0, o1, 0f, o.z, t, paper, trim, design, true);   // its top trim the sill's
                Panel(m, a, d, outward, inward, yaw, o0, o1, o.w, h, t, paper, trim, design, false);
                if (door)
                {
                    Trim(m, a, d, outward, yaw, skirt, o0, Skirting, t, trim);
                    skirt = o1;
                    DoorCasing(m, a, d, outward, inward, yaw, o.x, o.y, t, trim);
                    // Insert hardware after casing.
                    DoorHardware(m, a, d, outward, inward, yaw, o.x, o.y, t);
                }
                if (o.w > h - 0.08f)
                {
                    Trim(m, a, d, outward, yaw, cornice, o0, new Vector4(h - 0.03f, 0.06f, 0.02f, 0.008f), t, trim);
                    cornice = o1;
                }
                cursor = o1;
            }
            Panel(m, a, d, outward, inward, yaw, cursor, to, 0f, h, t, paper, trim, design, false);
            Trim(m, a, d, outward, yaw, skirt, to, Skirting, t, trim);
            Trim(m, a, d, outward, yaw, cornice, to, new Vector4(h - 0.03f, 0.06f, 0.02f, 0.008f), t, trim);
        }

        private static readonly List<Vector4> s_Openings = new List<Vector4>(8);
        private static readonly Comparison<Vector4> s_ByMiddle = (x, y) => x.x.CompareTo(y.x);

        /// <summary>The skirting's (middle height, height, depth, distance in front of the paper).</summary>
        private static readonly Vector4 Skirting = new Vector4(0.06f, 0.12f, 0.016f, 0.008f);

        // A trim run along the wall from `s0` to `s1`: `shape` its middle's height, its height, depth, and distance in front of
        // the paper.
        private static void Trim(RaidMesh m, Vector2 a, Vector2 d, Vector2 outward, float yaw, float s0, float s1, Vector4 shape, float t,
            in Surf trim)
        {
            float len = s1 - s0;
            if (len < 0.02f) return;
            Vector2 at = a + d * ((s0 + s1) * 0.5f) - outward * (t + LiningOff + shape.w);
            m.Box(KentuckyMap.To3(at, shape.x), new Vector3(len, shape.y, shape.z), yaw, trim);
        }

        // Paper over the wall from `s0` to `s1` along it and from `y0` to `y1` up it, with the room's rail where it falls and,
        // with `top`, a trim along its top (under a window, the sill's); the skirting and cornice are the stretch's (Trim).
        private static void Panel(RaidMesh m, Vector2 a, Vector2 d, Vector2 outward, Vector3 inward, float yaw, float s0, float s1,
            float y0, float y1, float t, in Surf paper, in Surf trim, WallDesign design, bool top)
        {
            float len = s1 - s0;
            if (len < 0.02f || y1 - y0 < 0.02f) return;
            Vector2 mid = a + d * ((s0 + s1) * 0.5f) - outward * (t + LiningOff);
            m.Upright(KentuckyMap.To3(mid, (y0 + y1) * 0.5f), inward, len, y1 - y0, paper);
            float rail = design.RailHeight;
            if (rail > y0 + 0.05f && rail < y1 - 0.3f)
                m.Box(KentuckyMap.To3(a + d * ((s0 + s1) * 0.5f) - outward * (t + LiningOff + 0.012f), rail + 0.01f), new Vector3(len, 0.06f, 0.024f), yaw, trim);
            if (top) Trim(m, a, d, outward, yaw, s0, s1, new Vector4(y1 - 0.03f, 0.06f, 0.02f, 0.008f), t, trim);
        }

        // The inside casing round an outside doorway.
        private static void DoorCasing(RaidMesh m, Vector2 a, Vector2 d, Vector2 outward, Vector3 inward, float yaw, float s, float half,
            float t, in Surf trim)
        {
            Surf wood = DecorPalette.Matte(Casing);
            float off = t + 0.012f;
            for (int e = -1; e <= 1; e += 2)
                m.Box(KentuckyMap.To3(a + d * (s + e * (half + 0.035f)) - outward * off, (DoorTop + 0.07f) * 0.5f), new Vector3(0.07f, DoorTop + 0.07f, 0.024f), yaw, wood);
            m.Box(KentuckyMap.To3(a + d * s - outward * off, DoorTop + 0.035f), new Vector3(half * 2f + 0.14f, 0.07f, 0.024f), yaw, wood);
        }

        // Adds a brass handle, knob, kick‑plate and two hinges to a doorway.
        // All sizes are in metres; positions are relative to the doorway centre 's' and half‑width 'half'.
        private static void DoorHardware(RaidMesh m, Vector2 a, Vector2 d, Vector2 outward, Vector3 inward, float yaw, float s, float half, float t)
        {
            // Material for all metal parts.
            var metal = DecorPalette.Metal(FurnitureBuilder.Brass);
            // ---- Kick‑plate (bottom of the door) ----
            // Positioned at the door base, centred on the opening.
            m.Box(
                KentuckyMap.To3(a + d * s - outward * (t + 0.012f), 0.05f), // centre at y = 0.05 m
                new Vector3(half * 2f + 0.14f, 0.10f, 0.025f), // width, height, depth
                yaw, metal);
            // ---- Brass handle (horizontal bar) ----
            // Placed about 1.0 m above the floor, centred on the door.
            float handleY = 1.0f;
            m.Box(
                KentuckyMap.To3(a + d * s - outward * (t + 0.012f), handleY),
                new Vector3(0.30f, 0.025f, 0.02f), // length, height, depth
                yaw, metal);
            // ---- Brass knob (cylindrical button) ----
            // Slightly forward from the handle bar, at the same height.
            m.CylinderX(
                KentuckyMap.To3(a + d * (s + 0.15f) - outward * (t + 0.015f), handleY),
                0.015f, // radius
                0.04f, // length (projected outward)
                8, metal, yaw);
            // ---- Two hinges (one near top, one near bottom) ----
            float[] hingeYs = new[] { 0.40f, 1.60f };
            foreach (float hy in hingeYs)
            {
                // Left‑hand side hinge.
                m.CylinderX(
                    KentuckyMap.To3(a + d * (s - half - 0.015f) - outward * (t + 0.012f), hy),
                    0.01f, 0.04f, 8, metal, yaw);
                // Right‑hand side hinge.
                m.CylinderX(
                    KentuckyMap.To3(a + d * (s + half + 0.015f) - outward * (t + 0.012f), hy),
                    0.01f, 0.04f, 8, metal, yaw);
            }
        }

        // A window from inside: the glass and the sashes in the reveal, the casing, the stool, and what the room hangs there —
        // a home's curtains, a workplace's blind, a church's coloured glass, nothing in a shop or a shed.
        private static void Window(InteriorPlan plan, RaidMesh m, PlanRoom room, Vector2 a, Vector2 d, Vector3 inward, float yaw,
            WindowSpan w, float t, Color fabric, System.Random rng)
        {
            // A tall building's upper floors' fixed windows (hundreds of them): the paper stops round the opening, no more.
            if (w.Plain) return;
            float width = w.To - w.From, sill = Mathf.Max(0.2f, w.Sill), head = Mathf.Max(sill + 0.3f, w.Head);
            if (width < 0.3f) return;
            Vector2 outward = new Vector2(d.y, -d.x);
            float mid = (w.From + w.To) * 0.5f, hgt = head - sill, cy = (sill + head) * 0.5f;
            Vector3 c = KentuckyMap.To3(a + d * mid - outward * (t + 0.006f), cy);   // the glazing, just proud of the paper
            Vector3 along = new Vector3(d.x, 0f, d.y);
            RoomRole role = room != null ? room.Role : RoomRole.Stock;
            bool church = role == RoomRole.Nave || role == RoomRole.Narthex || role == RoomRole.Chapel;
            Surf sash = DecorPalette.Matte(SashPaint);
            // A fixed window's glass and bars are the shell's, in the opening (BuildingPass); a working one's its RaidWindow's.
            if (!w.Working && !w.Clear)
            {
                Color glassTint = church ? StainedGlass[rng.Next(StainedGlass.Length)] : PaneTint;
                m.Upright(c, inward, width, hgt, church ? DecorPalette.Gloss(glassTint) : DecorPalette.Glass(glassTint));
                // The sashes' frames and the meeting rail; a church's tall lights get leading instead.
                Vector3 face = c + inward * 0.02f;
                m.Box(face + Vector3.up * (hgt * 0.5f - 0.025f), new Vector3(width, 0.05f, 0.03f), yaw, sash);
                m.Box(face - Vector3.up * (hgt * 0.5f - 0.03f), new Vector3(width, 0.06f, 0.03f), yaw, sash);
                for (int e = -1; e <= 1; e += 2) m.Box(face + along * (e * (width * 0.5f - 0.025f)), new Vector3(0.05f, hgt, 0.03f), yaw, sash);
                if (church)
                {
                    Surf lead = DecorPalette.Matte(new Color(0.12f, 0.12f, 0.12f));
                    for (int k = 1; k < 4; k++) m.Box(face + Vector3.up * (hgt * (k / 4f - 0.5f)), new Vector3(width, 0.02f, 0.015f), yaw, lead);
                    m.Box(face, new Vector3(0.02f, hgt, 0.015f), yaw, lead);
                }
                else
                {
                    m.Box(face, new Vector3(width, 0.045f, 0.035f), yaw, sash);
                    m.Box(face, new Vector3(0.025f, hgt, 0.025f), yaw, sash);
                }
            }
            // The casing round the opening, and the stool under it.
            Vector2 at = a + d * mid - outward * (t + 0.012f);
            Surf wood = DecorPalette.Matte(Casing);
            for (int e = -1; e <= 1; e += 2)
                m.Box(KentuckyMap.To3(at + d * (e * (width * 0.5f + 0.035f)), cy), new Vector3(0.07f, hgt + 0.14f, 0.024f), yaw, wood);
            m.Box(KentuckyMap.To3(at, head + 0.035f), new Vector3(width + 0.14f, 0.07f, 0.024f), yaw, wood);
            m.Box(KentuckyMap.To3(a + d * mid - outward * (t + 0.03f), sill - 0.01f), new Vector3(width + 0.18f, 0.04f, 0.08f), yaw, wood);
            // A working window's sashes, glass and what hangs there are its RaidWindow's (InteriorBuild.FitWindows).
            if (room == null || w.Working) return;
            // Add extra decorative details to the window opening.
            WindowExtras(m, a, d, outward, inward, yaw, w, t, fabric, rng);

            switch (role)
            {
                case RoomRole.Living: case RoomRole.Bedroom: case RoomRole.Dining: case RoomRole.Den: case RoomRole.Guest:
                case RoomRole.Sacristy: case RoomRole.Bar: case RoomRole.Eatery: case RoomRole.Chapel:
                    Curtains(m, a, d, outward, inward, yaw, mid, width, sill, head, t, fabric, rng);
                    break;
                case RoomRole.Kitchen: case RoomRole.Bathroom:
                    // A cafe curtain over the lower half.
                    Vector2 cafe = a + d * mid - outward * (t + 0.05f);
                    float top = cy + 0.05f;
                    m.Box(KentuckyMap.To3(cafe, top), new Vector3(width + 0.06f, 0.015f, 0.015f), yaw, DecorPalette.Metal(FurnitureBuilder.Brass));
                    m.Box(KentuckyMap.To3(cafe, (top + sill) * 0.5f), new Vector3(width - 0.04f, top - sill - 0.04f, 0.012f), yaw,
                        DecorPalette.Matte(Color.Lerp(fabric, Color.white, 0.55f)));
                    break;
                case RoomRole.Office: case RoomRole.BackOffice: case RoomRole.OpenOffice: case RoomRole.Squad: case RoomRole.Classroom:
                case RoomRole.Exam: case RoomRole.Ward: case RoomRole.Waiting: case RoomRole.Lobby: case RoomRole.Records:
                case RoomRole.Meeting: case RoomRole.Stacks: case RoomRole.BreakRoom: case RoomRole.DayRoom: case RoomRole.Bunks:
                    // A roller blind, part way down.
                    float down = hgt * (0.2f + 0.6f * (float)rng.NextDouble());
                    Vector2 blind = a + d * mid - outward * (t + 0.05f);
                    m.CylinderX(KentuckyMap.To3(blind, head - 0.04f), 0.025f, width, 8, DecorPalette.Matte(new Color(0.84f, 0.80f, 0.68f)), yaw);
                    m.Box(KentuckyMap.To3(blind, head - 0.06f - down * 0.5f), new Vector3(width - 0.02f, down, 0.006f), yaw,
                        DecorPalette.Matte(new Color(0.82f, 0.78f, 0.64f)));
                    m.Box(KentuckyMap.To3(blind, head - 0.06f - down), new Vector3(width, 0.02f, 0.014f), yaw, DecorPalette.Matte(Casing));
                    break;
            }
        }

        // Adds small decorative elements to a window: wooden sill‑cap, top‑moulding, a brass latch, and a tiny potted‑plant.
        private static void WindowExtras(RaidMesh m, Vector2 a, Vector2 d, Vector2 outward, Vector3 inward,
            float yaw, WindowSpan w, float t, Color fabric, System.Random rng)
        {
            // ---- Common geometry helpers ----
            float width = w.To - w.From;
            float sill = Mathf.Max(0.2f, w.Sill);
            float head = Mathf.Max(sill + 0.3f, w.Head);
            float mid = (w.From + w.To) * 0.5f;
            Vector3 basePos = KentuckyMap.To3(a + d * mid - outward * (t + 0.012f), 0f); // centre of the opening, ground‑level
            // ---- Materials ----
            var wood = DecorPalette.Matte(Casing); // reuse existing wood colour
            var metal = DecorPalette.Metal(FurnitureBuilder.Brass); // brass for latch
            var plantPot = DecorPalette.Matte(new Color(0.42f, 0.30f, 0.22f));
            var foliage = DecorPalette.Matte(new Color(0.18f, 0.45f, 0.18f));
            // ---- 1. Wooden sill‑cap (thin lip at the bottom) ----
            // Positioned just below the glass sill, extending a little beyond the opening.
            m.Box(
                KentuckyMap.To3(a + d * mid - outward * (t + 0.014f), sill - 0.01f),
                new Vector3(width + 0.04f, 0.02f, 0.025f),
                yaw, wood);
            // ---- 2. Top moulding (thin trim at the head) ----
            // Sits just above the upper glass edge.
            m.Box(
                KentuckyMap.To3(a + d * mid - outward * (t + 0.014f), head + 0.01f),
                new Vector3(width + 0.04f, 0.02f, 0.025f),
                yaw, wood);
            // ---- 3. Brass latch (small bar with a knob) on the left side ----
            // Bar: short horizontal piece, centred at 0.4 m above the sill. A fixed window has none: nothing opens.
            float latchY = sill + 0.4f;
            float latchX = -width * 0.5f + 0.08f; // offset from left edge of opening
            if (!w.Clear)
            {
                m.Box(
                    KentuckyMap.To3(a + d * (mid + latchX) - outward * (t + 0.012f), latchY),
                    new Vector3(0.12f, 0.015f, 0.018f),
                    yaw, metal);
                // Knob: tiny cylinder projecting outward.
                m.CylinderX(
                    KentuckyMap.To3(a + d * (mid + latchX + 0.07f) - outward * (t + 0.016f), latchY),
                    0.008f, 0.02f, 8, metal, yaw);
            }
            // ---- 4. Tiny potted‑plant on the sill, left side ----
            // Pot: small squat cylinder.
            float potY = sill - 0.015f; // sits on the sill‑cap
            float potX = -width * 0.5f + 0.20f;
            m.CylinderX(
                KentuckyMap.To3(a + d * (mid + potX) - outward * (t + 0.015f), potY),
                0.025f, 0.04f, 8, plantPot, yaw);
            // Foliage: tiny sphere above the pot.
            m.Sphere(
                KentuckyMap.To3(a + d * (mid + potX) - outward * (t + 0.025f), potY + 0.05f),
                0.04f, foliage);
        }
        // A pair of curtains on a rod over the casing, drawn back to either side.
        private static void Curtains(RaidMesh m, Vector2 a, Vector2 d, Vector2 outward, Vector3 inward, float yaw, float mid, float width,
            float sill, float head, float t, Color fabric, System.Random rng)
        {
            float rodY = head + 0.13f, off = t + 0.08f;
            Vector2 rod = a + d * mid - outward * off;
            m.Box(KentuckyMap.To3(rod, rodY), new Vector3(width + 0.5f, 0.024f, 0.024f), yaw, DecorPalette.Metal(FurnitureBuilder.Brass));
            float bottom = sill - 0.12f, hgt = rodY - bottom;
            Surf cloth = DecorPalette.Matte(fabric), fold = DecorPalette.Matte(fabric * 0.84f);
            for (int e = -1; e <= 1; e += 2)
            {
                float panel = 0.26f + 0.08f * (float)rng.NextDouble();
                Vector2 at = a + d * (mid + e * (width * 0.5f + 0.17f - panel * 0.3f)) - outward * (off - 0.02f);
                m.Box(KentuckyMap.To3(at, bottom + hgt * 0.5f), new Vector3(panel, hgt, 0.035f), yaw, cloth);
                m.Box(KentuckyMap.To3(at - outward * 0.02f + d * (e * 0.05f), bottom + hgt * 0.5f), new Vector3(panel * 0.3f, hgt, 0.02f), yaw, fold);
                // The tie-back.
                m.Box(KentuckyMap.To3(at - outward * 0.025f, bottom + hgt * 0.42f), new Vector3(panel + 0.02f, 0.04f, 0.04f), yaw, DecorPalette.Matte(fabric * 0.7f));
            }
        }

    }
}
