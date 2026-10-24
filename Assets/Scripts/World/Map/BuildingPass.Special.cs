using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What makes some buildings what they are: brick chimneys (up the outside of a farmhouse's gable ends from the
    // ground, or through the ridge, capped, often with snow on the cap); a church's steeple (a square tower over the
    // front, a louvred belfry and a spire with a cross) or a little belfry on a country church's ridge; the
    // courthouse's clock cupola and dome; the vent along a tobacco barn's or a rickhouse's ridge; a school's or an
    // office's canopy over its doors; a motel's upstairs walkway; a trailer's hitch. And three kinds drawn whole here:
    // silos and grain bins, gas station canopies over their pumps (and carports and shelters), and ruins.
    internal static partial class BuildingPass
    {
        private const int SaltChimney = 0x5A50, SaltCanopy = 0xCA90, SaltRuin = 0x2B1E;

        // The fuel canopies each site built (MapGameplay may want to leave their pumps to them).
        private static readonly ConditionalWeakTable<Site, List<Vector2>> s_FuelCanopies = new ConditionalWeakTable<Site, List<Vector2>>();

        /// <summary>True when this pass built a gas station canopy with pumps within <paramref name="within"/> metres of
        /// <paramref name="p"/> (site-local) on <paramref name="site"/>.</summary>
        internal static bool FuelCanopyNear(Site site, Vector2 p, float within)
        {
            if (!s_FuelCanopies.TryGetValue(site, out List<Vector2> list)) return false;
            for (int i = 0; i < list.Count; i++)
                if ((list[i] - p).sqrMagnitude < within * within) return true;
            return false;
        }

        private static void Special(Job j)
        {
            Plan p = j.P;
            Chimneys(j);
            if (p.Steeple || p.Belfry) Steeple(j);
            if (p.Cupola) Cupola(j);
            if (p.RidgeVent) RidgeVent(j);
            if (p.Door == DoorKind.Double && j.DoorEdge >= 0 && (p.Arch == Arch.School || p.Arch == Arch.Office || p.Arch == Arch.Hospital))
                EntranceCanopy(j);
            if (p.Arch == Arch.Motel && p.Storeys > 1) Walkway(j);
            if (p.Arch == Arch.Trailer) Hitch(j);
        }

        // What a far building keeps of what makes it what it is: its silhouette (a church's steeple, a courthouse's
        // cupola, a big chimney), drawn plainly.
        private static void LiteSpecial(Job j)
        {
            Plan p = j.P;
            if (p.Steeple || p.Belfry) Steeple(j);
            if (p.Cupola) Cupola(j);
        }

        #region Chimneys
        private static void Chimneys(Job j)
        {
            Plan p = j.P;
            if (p.Chimneys <= 0) return;
            long id = j.F.Data.id;
            Surf brick = WallSurf(WallKind.Brick, MapStyle.Pick(id, SaltChimney, 3) == 0 ? 1 : 0);
            if (j.Parts == null)
            {
                if (p.Roof != RoofKind.Flat) return;
                // Through a flat roof near the back wall.
                int back = p.Front >= 0 ? Opposite(j, p.Front) : -1;
                if (back < 0 || j.Len[back] < 2f) return;
                Vector2 at = j.At(back, j.Len[back] * Mathf.Lerp(0.2f, 0.8f, MapStyle.Unit(id, SaltChimney + 1))) - j.Out[back] * 0.7f;
                Stack(j, j.Upper, at, j.Yaw(back), p.Eave, p.Eave + p.Parapet + 0.9f, 0.5f, 0.8f, brick, id);
                return;
            }
            RoofPart main = j.Parts[0];
            Axes ax = AxesOf(j.Sh, main);
            float yc = (ax.Y0 + ax.Y1) * 0.5f;
            bool gable = main.Kind == RoofKind.Gable || main.Kind == RoofKind.Gambrel;
            int att = AttachedSide(main);
            if (p.ChimneyOutside && gable)
            {
                int placed = 0;
                int first = MapStyle.Pick(id, SaltChimney + 2, 2);
                for (int n = 0; n < 2 && placed < p.Chimneys; n++)
                {
                    int end = (first + n) % 2;
                    if (end == att || !OutsideEnd(j, ax, end, yc)) continue;
                    OutsideChimney(j, main, ax, end, yc, brick, id);
                    placed++;
                }
                if (placed > 0) return;
            }
            // Through the roof at the ridge, a third of the way along.
            float half = (ax.Y1 - ax.Y0) * 0.5f;
            float x0 = ax.X0, x1 = ax.X1;
            if (main.Kind == RoofKind.Hip || main.Kind == RoofKind.Pyramid) { x0 += half; x1 -= half; }
            float x = x1 > x0 ? Mathf.Lerp(x0, x1, MapStyle.Unit(id, SaltChimney + 3) < 0.5f ? 0.3f : 0.7f) : (ax.X0 + ax.X1) * 0.5f;
            float y = yc + (gable ? 0f : 0f);
            Vector3 at3 = ax.P(x, y, 0f);
            float yaw = -Mathf.Atan2(ax.X.y, ax.X.x) * Mathf.Rad2Deg;
            Stack(j, j.Upper, new Vector2(at3.x, at3.z), yaw, main.Ridge - 1.2f, main.Ridge + 0.85f, 0.62f, 0.78f, brick, id);
            if (p.Chimneys > 1 && x1 - x0 > 6f)
            {
                float x2 = Mathf.Lerp(x0, x1, MapStyle.Unit(id, SaltChimney + 3) < 0.5f ? 0.75f : 0.25f);
                Vector3 b3 = ax.P(x2, y, 0f);
                Stack(j, j.Upper, new Vector2(b3.x, b3.z), yaw, main.Ridge - 1.2f, main.Ridge + 0.85f, 0.62f, 0.78f, brick, id + 1);
            }
        }

        // True when the gable end <paramref name="end"/> of the block is an outside wall of the building.
        private static bool OutsideEnd(Job j, Axes ax, int end, float yc)
        {
            float x = end == 0 ? ax.X0 - 0.6f : ax.X1 + 0.6f;
            Vector2 probe = ax.O + ax.X * x + ax.Y * yc;
            return !MapGeometry.Contains(j.F.Pts, probe);
        }

        // A chimney up the outside of a gable end from the ground: a broad base stepping in under the eave to the stack.
        private static void OutsideChimney(Job j, RoofPart main, Axes ax, int end, float yc, in Surf brick, long id)
        {
            float sign = end == 0 ? -1f : 1f;
            float wallX = end == 0 ? ax.X0 : ax.X1;
            float off = (MapStyle.Unit(id, SaltChimney + 4) - 0.5f) * (ax.Y1 - ax.Y0) * 0.3f;
            Vector3 c = ax.P(wallX + sign * 0.38f, yc + off, 0f);
            Vector2 at = new Vector2(c.x, c.z);
            float yaw = -Mathf.Atan2(ax.X.y, ax.X.x) * Mathf.Rad2Deg;
            float shoulder = Mathf.Max(1.8f, main.Eave - 0.5f), top = main.Ridge + 0.75f;
            Vector3 outward = ax.Out(end);
            RaidMesh ground = j.Shell.WallsFacing(new Vector2(outward.x, outward.z));
            // Base: 0.76 out from the wall, 1.5 across; the part below the ground storey's top hides with that wall.
            float g = Mathf.Min(shoulder, GroundStorey);
            ground.Box(To3(at, g * 0.5f), new Vector3(0.76f, g, 1.5f), yaw, brick);
            if (shoulder > GroundStorey) j.Upper.Box(To3(at, (GroundStorey + shoulder) * 0.5f), new Vector3(0.76f, shoulder - GroundStorey, 1.5f), yaw, brick);
            if (j.Solid) Collider(j, To3(at, 1f), new Vector3(0.76f, 2f, 1.5f), yaw);
            Stack(j, j.Upper, at, yaw, shoulder, top, 0.62f, 0.86f, brick, id);
            j.Site.Take(at, 1f);
        }

        // A brick stack from y0 to y1 with a concrete cap, flue pots or a cap of snow.
        private static void Stack(Job j, RaidMesh m, Vector2 at, float yaw, float y0, float y1, float depth, float width, in Surf brick, long id)
        {
            m.Box(To3(at, (y0 + y1) * 0.5f), new Vector3(depth, y1 - y0, width), yaw, brick);
            m.Box(To3(at, y1 + 0.05f), new Vector3(depth + 0.12f, 0.1f, width + 0.12f), yaw, DecorPalette.Matte(new Color(0.58f, 0.57f, 0.54f)));
            float u = MapStyle.Unit(id, SaltChimney + 5);
            if (u < 0.35f)
                for (int k = -1; k <= 1; k += 2)
                    m.Cylinder(To3(at, y1 + 0.1f) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, k * width * 0.22f), 0.09f, 0.32f, 6,
                        DecorPalette.Matte(new Color(0.46f, 0.26f, 0.18f)));
            else if (u < 0.8f && j.P.Snow != SnowCover.Bare)
                m.Box(To3(at, y1 + 0.14f), new Vector3(depth + 0.06f, 0.08f, width + 0.06f), yaw, RoofSnow);
            j.Top = Mathf.Max(j.Top, y1 + 0.4f);
        }
        #endregion

        #region Steeples and cupolas
        // The main roof block's axes and the height of its top: the first block of a pitched roof (x along its ridge), or
        // the frame's own over a flat roof. Which end of it, if any, a wing joins.
        private static Axes MainAxes(Job j, out float top, out int attached)
        {
            if (j.Parts != null && j.Parts.Count > 0)
            {
                RoofPart main = j.Parts[0];
                top = main.Ridge;
                attached = AttachedSide(main);
                return AxesOf(j.Sh, main);
            }
            Shape sh = j.Sh;
            top = j.P.WallHeight;
            attached = -1;
            return new Axes { X = sh.U, Y = sh.V, O = sh.O, X0 = -sh.HalfL, X1 = sh.HalfL, Y0 = -sh.HalfW, Y1 = sh.HalfW };
        }

        // A church's tower over its front end, through the roof to a louvred belfry and a spire; or, on a small church, a
        // belfry on the ridge. A far one is drawn plainly: tower, belfry and spire.
        private static void Steeple(Job j)
        {
            Plan p = j.P;
            Axes ax = MainAxes(j, out float ridge, out int attached);
            Vector2 front = j.DoorEdge >= 0 ? j.Out[j.DoorEdge] : p.RoadDir;
            int end = Vector2.Dot(-ax.X, front) >= Vector2.Dot(ax.X, front) ? 0 : 1;
            if (attached == end) end = 1 - end;
            float span = ax.Y1 - ax.Y0, yc = (ax.Y0 + ax.Y1) * 0.5f;
            float inward = end == 0 ? 1f : -1f;
            float face = end == 0 ? ax.X0 : ax.X1;
            float yaw = -Mathf.Atan2(ax.X.y, ax.X.x) * Mathf.Rad2Deg;
            RaidMesh m = j.Upper;
            bool frame = p.Wall == WallKind.Siding;
            Surf body = j.WallS;
            Surf white = DecorPalette.Matte(new Color(0.93f, 0.93f, 0.90f));
            Surf spire = frame ? white : p.Covering == RoofCover.Metal ? RoofSurf(RoofCover.Metal, p.CoverTone) : RoofSurf(RoofCover.Slate, 0);
            Surf dark = DecorPalette.Matte(new Color(0.10f, 0.10f, 0.11f));
            float t, baseY, bellY;
            Vector2 at;
            bool tower = p.Steeple;
            if (tower)
            {
                t = Mathf.Clamp(span * 0.34f, 2.4f, 5f);
                // On the front end's middle, stepped in until it stands on the footprint (an outline that doesn't fill its
                // frame might leave the end's middle outside).
                float x = face + inward * (t * 0.5f - 0.25f);
                at = Point(ax, x, yc);
                for (int k = 0; k < 4 && !MapGeometry.Contains(j.F.Pts, at); k++)
                {
                    x += inward * t * 0.5f;
                    at = Point(ax, x, yc);
                }
                baseY = GroundStorey;
                bellY = Mathf.Max(ridge, p.WallHeight + 0.6f) + Mathf.Max(1.0f, t * 0.45f);
                m.Box(To3(at, (baseY + bellY) * 0.5f), new Vector3(t, bellY - baseY, t), yaw, body);
                if (!j.Lite)
                {
                    // A band at the belfry's foot, and the tower's door-height lancet over the entry.
                    m.Box(To3(at, bellY + 0.08f), new Vector3(t + 0.2f, 0.16f, t + 0.2f), yaw, white);
                    Vector3 o = end == 0 ? To3(-ax.X, 0f) : To3(ax.X, 0f);
                    m.Upright(To3(at, (baseY + bellY) * 0.5f + 0.8f) + o * (t * 0.5f + 0.01f), o, t * 0.22f, Mathf.Min(2.4f, (bellY - baseY) * 0.35f), dark);
                }
            }
            else
            {
                if (j.Parts == null) return;   // a belfry stands on a ridge
                t = Mathf.Clamp(span * 0.22f, 1.4f, 2.2f);
                at = Point(ax, face + inward * (t * 0.5f + 0.6f), yc);
                baseY = ridge - 0.9f;
                bellY = ridge + 0.2f;
                m.Box(To3(at, (baseY + bellY) * 0.5f), new Vector3(t, bellY - baseY, t), yaw, frame ? white : body);
            }
            // The belfry: open louvred arches on all four sides under a cornice.
            float belfry = t * (tower ? 0.75f : 0.8f), bh = Mathf.Clamp(t * 0.7f, 1.3f, 2.6f);
            float bTop = bellY + 0.16f + bh;
            m.Box(To3(at, (bellY + 0.16f + bTop) * 0.5f), new Vector3(belfry, bh, belfry), yaw, frame ? white : body);
            if (!j.Lite)
            {
                Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                for (int s = 0; s < 4; s++)
                {
                    Vector3 n = Quaternion.Euler(0f, 90f * s, 0f) * (q * Vector3.right);
                    Vector3 c = To3(at, (bellY + 0.16f + bTop) * 0.5f) + n * (belfry * 0.5f + 0.01f);
                    m.Upright(c, n, belfry * 0.5f, bh * 0.7f, dark);
                    for (int k = 1; k < 4; k++) m.Upright(c + n * 0.01f + Vector3.up * (bh * 0.7f * (k / 4f - 0.5f)), n, belfry * 0.5f, 0.05f, white);
                }
                m.Box(To3(at, bTop + 0.1f), new Vector3(belfry + 0.3f, 0.2f, belfry + 0.3f), yaw, white);
            }
            float spireH = tower ? t * Mathf.Lerp(2.2f, 3.2f, MapStyle.Unit(j.Id, SaltDetail + 3)) : t * 1.6f;
            m.Cylinder(To3(at, bTop + 0.2f), belfry * 0.62f, spireH, j.Lite ? 4 : 8, spire, Quaternion.Euler(0f, yaw + (j.Lite ? 45f : 22.5f), 0f), 0.02f, false);
            float tip = bTop + 0.2f + spireH;
            if (!j.Lite)
            {
                Surf gilt = DecorPalette.Metal(new Color(0.72f, 0.62f, 0.32f));
                m.Box(To3(at, tip + 0.45f), new Vector3(0.07f, 0.9f, 0.07f), yaw, gilt);
                m.Box(To3(at, tip + 0.6f), new Vector3(0.45f, 0.07f, 0.07f), yaw, gilt);
            }
            j.Top = Mathf.Max(j.Top, tip + 0.9f);
        }

        private static Vector2 Point(Axes ax, float x, float y) => ax.O + ax.X * x + ax.Y * y;

        // The courthouse's cupola: a square base through the ridge (or on the flat roof), a clock on each face, an
        // octagonal drum and a dome.
        private static void Cupola(Job j)
        {
            Axes ax = MainAxes(j, out float ridge, out _);
            bool flat = j.Parts == null;
            Vector2 at = Point(ax, (ax.X0 + ax.X1) * 0.5f, (ax.Y0 + ax.Y1) * 0.5f);
            float c = Mathf.Clamp((ax.Y1 - ax.Y0) * 0.22f, 2.8f, 4.6f);
            float yaw = -Mathf.Atan2(ax.X.y, ax.X.x) * Mathf.Rad2Deg;
            RaidMesh m = j.Upper;
            Surf white = DecorPalette.Matte(new Color(0.93f, 0.92f, 0.88f));
            Surf dark = DecorPalette.Matte(new Color(0.08f, 0.08f, 0.09f));
            float y0 = flat ? j.P.Eave : ridge - 1.2f, y1 = (flat ? j.P.WallHeight + 1.2f : ridge) + 2.0f;
            m.Box(To3(at, (y0 + y1) * 0.5f), new Vector3(c, y1 - y0, c), yaw, white);
            m.Box(To3(at, y1 + 0.1f), new Vector3(c + 0.3f, 0.2f, c + 0.3f), yaw, white);
            if (!j.Lite)
            {
                Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                float clockY = y1 - 1.1f;
                for (int s = 0; s < 4; s++)
                {
                    Vector3 n = Quaternion.Euler(0f, 90f * s, 0f) * (q * Vector3.right);
                    Vector3 face = To3(at, clockY) + n * (c * 0.5f);
                    m.Cylinder(face, c * 0.3f, 0.05f, 12, DecorPalette.Matte(new Color(0.96f, 0.95f, 0.90f)), Quaternion.FromToRotation(Vector3.up, n));
                    m.Upright(face + n * 0.06f + Vector3.up * c * 0.08f, n, 0.05f, c * 0.16f, dark);
                    m.Upright(face + n * 0.06f + Vector3.Cross(Vector3.up, n) * c * 0.06f, n, c * 0.12f, 0.05f, dark);
                }
            }
            float r = c * 0.36f;
            m.Cylinder(To3(at, y1 + 0.2f), r, 1.5f, j.Lite ? 6 : 8, white, Quaternion.Euler(0f, yaw + 22.5f, 0f));
            Surf dome = j.P.Covering == RoofCover.Metal ? RoofSurf(RoofCover.Metal, j.P.CoverTone) : DecorPalette.Metal(new Color(0.30f, 0.48f, 0.40f));
            m.Ellipsoid(To3(at, y1 + 1.7f), new Vector3(r * 2.1f, r * 1.9f, r * 2.1f), dome, j.Lite ? 6 : 10, j.Lite ? 4 : 6);
            if (!j.Lite) m.Box(To3(at, y1 + 1.7f + r * 0.95f + 0.4f), new Vector3(0.08f, 0.8f, 0.08f), yaw, DecorPalette.Metal(new Color(0.72f, 0.62f, 0.32f)));
            j.Top = Mathf.Max(j.Top, y1 + 1.7f + r + 0.9f);
        }
        // A raised vent along the ridge, capped.
        private static void RidgeVent(Job j)
        {
            if (j.Parts == null) return;
            RoofPart main = j.Parts[0];
            if (main.Kind != RoofKind.Gable) return;
            Axes ax = AxesOf(j.Sh, main);
            float yc = (ax.Y0 + ax.Y1) * 0.5f;
            Vector3 a = ax.P(ax.X0 + 0.8f, yc, main.Ridge + 0.25f), b = ax.P(ax.X1 - 0.8f, yc, main.Ridge + 0.25f);
            RaidMesh m = j.Upper;
            Surf metal = RoofSurf(RoofCover.Metal, j.P.CoverTone);
            Beam(m, a, b, 0.7f, 0.5f, Vector3.up, DecorPalette.Matte(new Color(0.12f, 0.11f, 0.10f)));
            Beam(m, a + Vector3.up * 0.3f, b + Vector3.up * 0.3f, 1.3f, 0.1f, Vector3.up, metal);
            if (j.P.Snow == SnowCover.Full) Beam(m, a + Vector3.up * 0.38f, b + Vector3.up * 0.38f, 1.1f, 0.06f, Vector3.up, RoofSnow);
            j.Top = Mathf.Max(j.Top, main.Ridge + 0.65f);
        }
        #endregion

        #region Small things
        // A flat canopy on two posts over a school's or an office's doors.
        private static void EntranceCanopy(Job j)
        {
            int e = j.DoorEdge;
            float w = DoubleDoorWidth + 1.8f, depth = 1.9f, y = 2.75f;
            Vector2 at = j.At(e, j.DoorAt) + j.Out[e] * (depth * 0.5f);
            float yaw = j.Yaw(e);
            Surf trim = DecorPalette.Matte(new Color(0.86f, 0.86f, 0.84f));
            j.Upper.Box(To3(at, y), new Vector3(w, 0.2f, depth), yaw, trim);
            if (j.P.Snow != SnowCover.Bare) j.Upper.Box(To3(at, y + 0.14f), new Vector3(w - 0.1f, 0.08f, depth - 0.1f), yaw, RoofSnow);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector2 p = at + j.Dir[e] * ((w * 0.5f - 0.15f) * s) + j.Out[e] * (depth * 0.5f - 0.15f);
                Vector3 c = To3(p, (y - 0.1f) * 0.5f);
                var size = new Vector3(0.14f, y - 0.1f, 0.14f);
                j.Facing(e).Box(c, size, yaw, DecorPalette.Metal(new Color(0.4f, 0.4f, 0.42f)));
                if (j.Solid) Collider(j, c, size, yaw);
            }
        }

        // A motel's upstairs walkway along its front: a concrete deck on steel posts with a railing.
        private static void Walkway(Job j)
        {
            int e = j.P.Front;
            if (e < 0) return;
            float len = j.Len[e], depth = 1.6f, y = j.FloorY(1);
            Vector2 mid = j.At(e, len * 0.5f) + j.Out[e] * (depth * 0.5f);
            float yaw = j.Yaw(e);
            j.Upper.Box(To3(mid, y - 0.1f), new Vector3(len, 0.2f, depth), yaw, ConcreteSurf);
            Surf steel = DecorPalette.Metal(new Color(0.38f, 0.40f, 0.42f));
            Vector2 rail = j.At(e, len * 0.5f) + j.Out[e] * (depth - 0.05f);
            j.Upper.Box(To3(rail, y + 0.95f), new Vector3(len, 0.06f, 0.06f), yaw, steel);
            j.Upper.Box(To3(rail, y + 0.45f), new Vector3(len, 0.9f, 0.02f), yaw, DecorPalette.Matte(new Color(0.20f, 0.30f, 0.26f)));
            int posts = Mathf.Max(2, Mathf.RoundToInt(len / 4f));
            for (int k = 0; k <= posts; k++)
            {
                Vector2 p = j.At(e, Mathf.Lerp(0.3f, len - 0.3f, k / (float)posts)) + j.Out[e] * (depth - 0.1f);
                Vector3 c = To3(p, (y - 0.2f) * 0.5f);
                var size = new Vector3(0.12f, y - 0.2f, 0.12f);
                j.Facing(e).Box(c, size, yaw, steel);
                if (j.Solid) Collider(j, c, size, yaw);
            }
        }

        // A single-wide's towing hitch at one end.
        private static void Hitch(Job j)
        {
            Shape sh = j.Sh;
            float end = MapStyle.Unit(j.F.Data.id, SaltPlan + 9) < 0.5f ? -1f : 1f;
            Vector3 a = To3(sh.World(end * sh.HalfL, -sh.HalfW * 0.6f), 0.4f), b = To3(sh.World(end * sh.HalfL, sh.HalfW * 0.6f), 0.4f);
            Vector3 tip = To3(sh.World(end * (sh.HalfL + 1.3f), 0f), 0.42f);
            Surf steel = DecorPalette.Metal(new Color(0.25f, 0.25f, 0.26f));
            RaidMesh m = j.Shell.WallsFacing(end > 0f ? sh.U : -sh.U);
            Beam(m, a, tip, 0.09f, 0.09f, Vector3.up, steel);
            Beam(m, b, tip, 0.09f, 0.09f, Vector3.up, steel);
            m.Box(tip + Vector3.down * 0.2f, new Vector3(0.08f, 0.42f, 0.08f), 0f, steel);
        }
        #endregion

        #region Silos, canopies, ruins
        // A parking deck: a concrete slab a floor, open between its spandrels (the low walls round each deck's edge) on
        // columns, a stair tower at the corner nearest the road with its lamp; the top deck under the snow, lamp posts on it.
        // The ground floor is open to walk into (columns and a curb have colliders) and out of the weather; the decks
        // above are the upper part, lifted with the cutaway.
        private static void ParkingDeck(Job j)
        {
            Plan p = j.P;
            Footprint f = j.F;
            Shape sh = j.Sh;
            j.Shell = j.Site.Shell(f, j.Scenery && !j.Edge);
            int decks = Mathf.Max(1, p.Storeys - 1) + 1;   // the floors over the ground one, the top deck last
            float h = p.Storey, spandrel = 1.05f, slab = 0.3f;
            j.Shell.WallTop = 0.4f;
            RaidMesh up = j.Upper;
            bool snowy = p.Snow != SnowCover.Bare;
            Surf concrete = j.WallS;
            Surf inner = DecorPalette.Matte(ConcreteTones[Mathf.Clamp(p.WallTone, 0, ConcreteTones.Length - 1)] * 0.62f);
            for (int k = 1; k < decks; k++)
            {
                float y = k * h;
                bool top = k == decks - 1;
                Deck(up, j, y, top ? (snowy ? RoofSnow : AsphaltSurf) : inner);
                for (int i = 0; i < j.N; i++)
                {
                    float len = j.Len[i];
                    if (len < 0.1f) continue;
                    WallBox(j, up, j.A[i], j.Dir[i], -j.Out[i] * (Thick * 0.5f), -j.Run0[i], len + j.Run1[i], y - slab, y + spandrel, j.Yaw(i), concrete, false);
                    if (!j.Lite && top) OnWallBox(j, up, i, len * 0.5f, y + spandrel + 0.04f, len + 0.08f, 0.08f, Thick + 0.1f, CopingSurf(p), -Thick - 0.05f);
                }
            }
            float roof = (decks - 1) * h;
            j.Top = roof + spandrel;
            // The columns: at the corners and every seven metres or so round the outline, up through every floor.
            Surf column = concrete;
            for (int i = 0; i < j.N; i++)
            {
                float len = j.Len[i];
                if (len < 1f) continue;
                int n = Mathf.Max(1, Mathf.RoundToInt(len / 7.5f));
                float yaw = j.Yaw(i);
                RaidMesh walls = j.Facing(i);
                for (int c = 0; c < n; c++)
                {
                    float s = len * c / n + 0.3f;
                    Vector2 at = j.At(i, s) - j.Out[i] * 0.3f;
                    var size = new Vector3(0.5f, h - slab, 0.5f);
                    Vector3 centre = To3(at, (h - slab) * 0.5f);
                    walls.Box(centre, size, yaw, column);
                    Collider(j, centre, size, yaw);
                    if (j.Lite) continue;
                    for (int k = 1; k < decks - 1; k++)
                        up.Box(To3(at, k * h + spandrel + (h - slab - spandrel) * 0.5f), new Vector3(0.45f, h - slab - spandrel, 0.45f), yaw, column);
                }
                // A curb round the ground floor, open where the drive comes in (on the front).
                if (i == p.Front && len > 7f)
                {
                    float gap = Mathf.Min(7f, len * 0.4f), mid = len * 0.5f;
                    CurbRun(j, i, 0.3f, mid - gap * 0.5f);
                    CurbRun(j, i, mid + gap * 0.5f, len - 0.3f);
                }
                else CurbRun(j, i, 0.3f, len - 0.3f);
            }
            StairTower(j, decks, h, roof, spandrel, snowy);
            if (!j.Lite)
            {
                // Lamp posts on the top deck.
                Surf pole = DecorPalette.Metal(new Color(0.30f, 0.31f, 0.32f));
                int lamps = Mathf.Clamp(Mathf.RoundToInt(f.Size / 700f), 1, 6);
                for (int k = 0; k < lamps; k++)
                {
                    var c = new Vector2(Mathf.Lerp(-sh.HalfL * 0.6f, sh.HalfL * 0.6f, lamps == 1 ? 0.5f : k / (float)(lamps - 1)), 0f);
                    Vector2 at = sh.World(c);
                    if (!MapGeometry.Contains(f.Pts, at)) continue;
                    up.Cylinder(To3(at, roof), 0.08f, 5.5f, 6, pole);
                    up.Box(To3(at, roof + 5.5f), new Vector3(0.9f, 0.18f, 0.35f), sh.Yaw, pole);
                    up.Box(To3(at, roof + 5.4f), new Vector3(0.7f, 0.04f, 0.25f), sh.Yaw, LampGlobe(0));
                }
                // The ground floor's slab, under its deck.
                Vector3 middle = To3(f.Middle, 0f);
                Deck(j.Site.Town.Out.Flat(middle), j, 0.02f, AsphaltSurf);
            }
            if (!j.Scenery) Rows(j, true);
        }

        private static void CurbRun(Job j, int i, float s0, float s1)
        {
            if (s1 - s0 < 0.3f) return;
            WallBox(j, j.Facing(i), j.A[i], j.Dir[i], -j.Out[i] * (Thick * 0.5f), s0, s1, 0f, 0.35f, j.Yaw(i), j.WallS, true);
        }

        // The stair tower of a parking deck: at the corner nearest the road, up past the top deck, a door at its foot.
        private static void StairTower(Job j, int decks, float h, float roof, float spandrel, bool snowy)
        {
            Shape sh = j.Sh;
            Plan p = j.P;
            Vector2 road = p.RoadDir == Vector2.zero ? -sh.V : p.RoadDir;
            float su = Vector2.Dot(road, sh.U) >= 0f ? 1f : -1f, sv = Vector2.Dot(road, sh.V) >= 0f ? 1f : -1f;
            const float T = 3.6f;
            var c = new Vector2(su * (sh.HalfL - T * 0.5f - 0.3f), sv * (sh.HalfW - T * 0.5f - 0.3f));
            if (!Fits(j, c, T * 0.5f, T * 0.5f, 0.05f)) return;
            Vector2 at = sh.World(c);
            float top = roof + 3.2f;
            float g = Mathf.Min(GroundStorey, top);
            RaidMesh walls = j.Shell.WallsFacing(sh.V * sv);
            var low = new Vector3(T, g, T);
            walls.Box(To3(at, g * 0.5f), low, sh.Yaw, j.WallS);
            Collider(j, To3(at, g * 0.5f), low, sh.Yaw);
            j.Upper.Box(To3(at, (g + top) * 0.5f), new Vector3(T, top - g, T), sh.Yaw, j.WallS);
            j.Upper.Box(To3(at, top + 0.06f), new Vector3(T + 0.2f, 0.12f, T + 0.2f), sh.Yaw, CopingSurf(p));
            if (snowy) j.Upper.Box(To3(at, top + 0.16f), new Vector3(T - 0.1f, 0.08f, T - 0.1f), sh.Yaw, RoofSnow);
            if (!j.Lite)
            {
                // Its glazed stair window up the side facing the road, and a door at the foot.
                Vector3 o = To3(sh.V * sv, 0f);
                j.Upper.Upright(To3(at, (GroundStorey + top) * 0.5f) + o * (T * 0.5f + 0.01f), o, 0.9f, top - GroundStorey - 1f, j.GlassS);
                walls.Upright(To3(at, 1.05f) + o * (T * 0.5f + 0.01f), o, 1.0f, 2.1f, DecorPalette.Matte(new Color(0.42f, 0.44f, 0.46f)));
            }
            j.Top = Mathf.Max(j.Top, top + 0.2f);
        }

        // A grain bin (corrugated steel under a cone) or a stave silo under a dome, with a ladder up its side.
        private static void SiloBuilding(Job j)
        {
            Plan p = j.P;
            Footprint f = j.F;
            j.Shell = j.Site.Shell(f, j.Scenery && !j.Edge);
            float r = Mathf.Sqrt(f.Size / Mathf.PI);
            Vector2 at = f.Middle;
            float h = p.Eave;
            int sides = j.Lite ? 10 : 18;
            bool bin = p.Roof == RoofKind.Pyramid;
            Surf wall = bin ? WallSurf(WallKind.Metal, 0) : p.CoverTone == MetalBlue ? DecorPalette.Gloss(new Color(0.12f, 0.20f, 0.36f)) : WallSurf(WallKind.Concrete, 0);
            float g = Mathf.Min(h, GroundStorey);
            j.Shell.WallsFacing(Vector2.down).Cylinder(To3(at, 0f), r, g, sides, wall);
            if (h - g > 0.05f) j.Shell.Upper.Cylinder(To3(at, g), r, h - g, sides, wall);
            j.Shell.WallTop = g;
            RaidMesh m = j.Upper;
            if (bin)
            {
                float coneH = (r + 0.15f) * Mathf.Tan(30f * Mathf.Deg2Rad);
                m.Cylinder(To3(at, h), r + 0.15f, coneH, sides, RoofSurf(RoofCover.Metal, MetalGalv), Quaternion.identity, 0.08f);
                if (!j.Lite && p.Snow != SnowCover.Bare)
                    m.Cylinder(To3(at, h + coneH * 0.4f + 0.03f), (r + 0.15f) * 0.6f + 0.04f, coneH * 0.52f, sides, RoofSnow, Quaternion.identity, 0.12f);
                m.Cylinder(To3(at, h + coneH * 0.95f), 0.35f, 0.4f, 8, DecorPalette.Metal(new Color(0.55f, 0.56f, 0.57f)));
                j.Top = h + coneH + 0.4f;
            }
            else
            {
                Surf dome = p.CoverTone == MetalBlue ? DecorPalette.Gloss(new Color(0.12f, 0.20f, 0.36f)) : RoofSurf(RoofCover.Metal, MetalGalv);
                m.Ellipsoid(To3(at, h), new Vector3(r * 2.04f, r * 1.3f, r * 2.04f), dome, j.Lite ? 8 : 14, j.Lite ? 4 : 6);
                j.Top = h + r * 0.65f;
            }
            if (!j.Lite)
            {
                // The ladder, on the side toward the road.
                Vector2 side = p.RoadDir == Vector2.zero ? Vector2.down : p.RoadDir;
                Vector2 foot = at + side * (r + 0.12f);
                Vector2 across = new Vector2(-side.y, side.x) * 0.22f;
                Surf steel = DecorPalette.Metal(new Color(0.35f, 0.35f, 0.36f));
                m.Box(To3(foot + across, h * 0.5f + 0.5f), new Vector3(0.05f, h - 1f, 0.05f), 0f, steel);
                m.Box(To3(foot - across, h * 0.5f + 0.5f), new Vector3(0.05f, h - 1f, 0.05f), 0f, steel);
            }
            if (j.Solid) Collider(j, To3(at, h * 0.5f), new Vector3(r * 1.5f, h, r * 1.5f), j.Sh.Yaw);
            if (!j.Scenery) Rows(j, false);
        }

        // A canopy: a gas station's over its pump islands, or a carport's or a shelter's roof on posts.
        private static void CanopyBuilding(Job j)
        {
            Plan p = j.P;
            Shape sh = j.Sh;
            j.Shell = j.Site.Shell(j.F, j.Scenery && !j.Edge);
            j.Shell.WallTop = 0f;
            float yaw = sh.Yaw;
            System.Random rng = MapStyle.Rng(j.F.Data.id, SaltCanopy);
            RaidMesh ground = j.Shell.WallsFacing(-sh.V);
            float top = p.Eave;
            if (p.Fuel)
            {
                float l = sh.Length + 0.1f, w = sh.Width + 0.1f;
                Vector3 mid = To3(sh.O, top - 0.42f);
                Color[] brands = { new Color(0.72f, 0.12f, 0.10f), new Color(0.12f, 0.28f, 0.60f), new Color(0.10f, 0.45f, 0.24f), new Color(0.85f, 0.62f, 0.10f) };
                Surf white = DecorPalette.Matte(new Color(0.93f, 0.93f, 0.91f));
                j.Upper.Box(mid, new Vector3(l, 0.84f, w), yaw, white);
                j.Upper.Box(mid + Vector3.up * 0.1f, new Vector3(l + 0.06f, 0.24f, w + 0.06f), yaw, DecorPalette.Matte(brands[p.SignTone % brands.Length]));
                if (p.Snow != SnowCover.Bare) j.Upper.Box(To3(sh.O, top + 0.04f), new Vector3(l - 0.3f, 0.08f, w - 0.3f), yaw, RoofSnow);
                if (!j.Lite)
                    for (float u = -sh.HalfL + 2f; u <= sh.HalfL - 1.9f; u += 3.6f)
                        for (float v = -sh.HalfW + 2f; v <= sh.HalfW - 1.9f; v += 3.6f)
                            j.Upper.Flat(To3(sh.World(u, v), top - 0.85f), 0.9f, 1.6f, yaw + 180f, DecorPalette.Glow(new Color(0.9f, 0.92f, 0.86f)));
                int cols = sh.Length > 18f ? 3 : 2, rows = sh.Width > 9f ? 2 : 1;
                Surf steel = DecorPalette.Matte(new Color(0.86f, 0.86f, 0.84f));
                for (int a = 0; a < cols; a++)
                    for (int b = 0; b < rows; b++)
                    {
                        float u = cols == 1 ? 0f : Mathf.Lerp(-sh.HalfL * 0.6f, sh.HalfL * 0.6f, a / (float)(cols - 1));
                        float v = rows == 1 ? 0f : (b == 0 ? -1f : 1f) * sh.HalfW * 0.45f;
                        Vector2 at = sh.World(u, v);
                        var size = new Vector3(0.36f, top - 0.84f, 0.36f);
                        Vector3 c = To3(at, size.y * 0.5f);
                        ground.Box(c, size, yaw, steel);
                        if (j.Solid) Collider(j, c, size, yaw);
                        if (j.Lite) continue;
                        // A pump each side of the post on its island.
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 pump = To3(sh.World(u + s * 1.3f, v), 0f);
                            ground.SetFrame(pump, yaw + 90f);
                            DecorProps.GasPump(ground, rng);
                            ground.ResetFrame();
                            if (j.Solid) Collider(j, pump + Vector3.up * 0.9f, new Vector3(1.6f, 1.8f, 1.0f), yaw);
                        }
                    }
                j.Top = top + 0.1f;
                if (!j.Scenery)
                {
                    if (!s_FuelCanopies.TryGetValue(j.Site, out List<Vector2> list))
                    {
                        list = new List<Vector2>();
                        s_FuelCanopies.Add(j.Site, list);
                    }
                    list.Add(sh.O);
                }
            }
            else
            {
                // Posts round the edge and a roof on the bounding rectangle.
                Surf post = DecorPalette.Matte(new Color(0.88f, 0.88f, 0.86f));
                int along = Mathf.Max(1, Mathf.CeilToInt(sh.Length / 3.2f));
                for (int a = 0; a <= along; a++)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector2 at = sh.World(Mathf.Lerp(-sh.HalfL + 0.1f, sh.HalfL - 0.1f, a / (float)along), s * (sh.HalfW - 0.1f));
                        var size = new Vector3(0.12f, top, 0.12f);
                        Vector3 c = To3(at, top * 0.5f);
                        ground.Box(c, size, yaw, post);
                        if (j.Solid) Collider(j, c, size, yaw);
                    }
                s_OneBlock.Clear();
                s_OneBlock.Add(sh.Box);
                j.Parts = Parts(j, s_OneBlock);
                foreach (RoofPart part in j.Parts) PartRoof(j, part);
            }
            if (!j.Scenery) Rows(j, false);
        }

        // Broken walls of brick or stone, stepped and gapped, rubble heaped inside, no roof.
        private static void RuinBuilding(Job j)
        {
            Plan p = j.P;
            j.Shell = j.Site.Shell(j.F, j.Scenery && !j.Edge);
            System.Random rng = MapStyle.Rng(j.F.Data.id, SaltRuin);
            float highest = 0f;
            for (int i = 0; i < j.N; i++)
            {
                float len = j.Len[i];
                if (len < 0.1f) continue;
                Vector2 inset = -j.Out[i] * (Thick * 0.5f);
                float yaw = j.Yaw(i);
                RaidMesh facing = j.Facing(i);
                float s = 0f;
                while (s < len)
                {
                    float piece = Mathf.Min(len - s, Range(rng, 1.0f, 3.2f));
                    float h = rng.NextDouble() < 0.15 ? 0f : Range(rng, 0.3f, p.Eave);
                    if (h > 0.05f)
                    {
                        WallBox(j, facing, j.A[i], j.Dir[i], inset, s, s + piece, 0f, Mathf.Min(h, GroundStorey), yaw, j.WallS, h > 0.7f);
                        if (h > GroundStorey) WallBox(j, j.Upper, j.A[i], j.Dir[i], inset, s, s + piece, GroundStorey, h, yaw, j.WallS, false);
                        // A ragged top: a shorter piece standing on part of it.
                        float step = piece * Range(rng, 0.3f, 0.6f);
                        if (h + 0.3f < p.Eave + 0.4f && !j.Lite)
                            WallBox(j, h + 0.35f > GroundStorey ? j.Upper : facing, j.A[i], j.Dir[i], inset, s, s + step, h, h + Range(rng, 0.2f, 0.5f), yaw, j.WallS, false);
                        highest = Mathf.Max(highest, h + 0.5f);
                    }
                    s += piece;
                }
            }
            j.Shell.WallTop = Mathf.Min(GroundStorey, highest);
            j.Top = highest;
            if (!j.Lite)
            {
                Footprint f = j.F;
                Surf rubble = WallSurf(p.Wall, p.WallTone);
                RaidMesh floor = j.Site.Town.Out.Solid(To3(f.Middle, 0f));
                int heaps = Mathf.Clamp(Mathf.RoundToInt(f.Size / 25f), 2, 7);
                for (int k = 0, tries = 0; k < heaps && tries < heaps * 5; tries++)
                {
                    var at = new Vector2(Mathf.Lerp(f.Bounds.xMin, f.Bounds.xMax, (float)rng.NextDouble()), Mathf.Lerp(f.Bounds.yMin, f.Bounds.yMax, (float)rng.NextDouble()));
                    if (!MapGeometry.Contains(f.Pts, at) || MapGeometry.DistanceToEdge(f.Pts, at) < 0.8f) continue;
                    k++;
                    var size = new Vector3(Range(rng, 1.2f, 2.6f), Range(rng, 0.4f, 0.9f), Range(rng, 1.0f, 2.2f));
                    floor.Ellipsoid(To3(at, 0f), size, Quaternion.Euler(0f, Range(rng, 0f, 180f), 0f), rubble, 7, 4);
                    floor.Ellipsoid(To3(at, size.y * 0.22f), new Vector3(size.x * 0.8f, size.y * 0.7f, size.z * 0.8f), Quaternion.Euler(0f, Range(rng, 0f, 180f), 0f), RoofSnow, 7, 4);
                }
            }
            if (!j.Scenery) Inside(j);
        }
        #endregion
    }
}
