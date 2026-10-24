using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Flat roofs: most of a downtown's and every big building's, and what has to make one read as a roof from the camera
    // and from the air rather than a blank slab. The deck (tar, or grey gravel ballast) under the snow, the snow drifted
    // deep against the parapets on the lee of the north-west wind and melted back round whatever's warm (the units, the
    // vents), much more of it gone from a partly bare roof; the parapet's coping round the edge, a cornice on a downtown
    // front and, over two or three storeys, a raised panel, steps or a pediment with the builder's date stone; and what
    // stands on it, set out on the building's own axes the way a mechanical contractor would: package units and their
    // condensers, exhaust fans, vent stacks, a roof hatch, rows of skylights on a warehouse, a stair and elevator
    // penthouse on a tall block, expansion curbs across the biggest. A Lite roof keeps the deck, the penthouse and a unit
    // or two: the silhouette.
    internal static partial class BuildingPass
    {
        private const int SaltDrift = 0xD41F, SaltMelt = 0x3E17, SaltParapet = 0x9A4A;

        private static void FlatRoof(Job j)
        {
            Plan p = j.P;
            Footprint f = j.F;
            RaidMesh m = j.Upper;
            float deck = p.Eave;
            bool snowy = p.Snow != SnowCover.Bare;
            Surf cover = RoofCoverSurf(p, false);
            float surface = deck + (snowy ? 0.12f : 0.03f);
            Deck(m, j, surface, snowy ? RoofSnow : cover);
            j.Top = Mathf.Max(j.Top, deck + p.Parapet);
            if (j.Lite)
            {
                if (p.Penthouse) Penthouse(j, deck, surface, snowy);
                else if (p.RoofUnits && f.Size > 400f) LiteUnit(j, deck);
                return;
            }
            float coping = deck + p.Parapet;
            Copings(j, coping);
            if (p.FrontParapet > 0 && p.Front >= 0 && !j.Party(p.Front)) FrontParapet(j, p.Front, coping);
            System.Random rng = MapStyle.Rng(j.Id, SaltUnits);
            // Where the snow has gone: a tar roof's gravel ballast, grey.
            Surf bare = p.Covering == RoofCover.Membrane ? RoofSurf(RoofCover.Membrane, 1) : cover;
            if (snowy) Drifts(j, m, surface, rng);
            if (snowy && p.Heated && f.Size > 700f && MapStyle.Unit(j.Id, SaltMelt) < 0.7f) MeltLines(j, m, surface + 0.004f);
            Rect keep = default;
            if (p.Penthouse) keep = Penthouse(j, deck, surface, snowy);
            if (p.RoofUnits || f.Size > 220f) Furniture(j, deck, surface, snowy, bare, keep, rng);
            if (p.Snow == SnowCover.Partial) Patches(j, m, surface + 0.006f, rng, bare, Mathf.Clamp(2 + Mathf.RoundToInt(f.Size / 250f), 2, 10), 0.7f, 2.2f);
            else if (!snowy) Patches(j, m, surface + 0.006f, rng, RoofSnow, Mathf.Clamp(2 + Mathf.RoundToInt(f.Size / 200f), 2, 10), 0.5f, 1.6f);
        }

        private static readonly List<float> s_Span = new List<float>(16);

        // Where the line u = c (frame coordinates; v = c when <paramref name="alongU"/>) crosses the footprint: the
        // stretches of it inside, as pairs of v (or u) in <see cref="s_Span"/>.
        private static void Spans(Job j, float c, bool alongU)
        {
            s_Span.Clear();
            Shape sh = j.Sh;
            List<Vector2> pts = j.F.Pts;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = sh.Local(pts[i]), b = sh.Local(pts[(i + 1) % n]);
                float ka = alongU ? a.y : a.x, kb = alongU ? b.y : b.x;
                if ((ka > c) == (kb > c)) continue;
                float t = (c - ka) / (kb - ka);
                s_Span.Add(alongU ? a.x + (b.x - a.x) * t : a.y + (b.y - a.y) * t);
            }
            s_Span.Sort();
        }

        // Lines melted through the snow along the joists of a heated building's big roof, where their steel carries the
        // warmth up: faint grey stripes a couple of metres apart across the short way, clear of the parapets.
        private static void MeltLines(Job j, RaidMesh m, float y)
        {
            Shape sh = j.Sh;
            float spacing = Mathf.Lerp(1.8f, 2.6f, MapStyle.Unit(j.Id, SaltMelt + 1));
            Surf melt = DecorPalette.Matte(new Color(0.74f, 0.77f, 0.81f));
            int lines = Mathf.FloorToInt((sh.HalfL * 2f - 2f) / spacing);
            float u0 = -(lines - 1) * spacing * 0.5f;
            for (int k = 0; k < lines; k++)
            {
                float u = u0 + k * spacing;
                Spans(j, u, false);
                for (int s = 0; s + 1 < s_Span.Count; s += 2)
                {
                    float v0 = s_Span[s] + 1.1f, v1 = s_Span[s + 1] - 1.1f;
                    if (v1 - v0 < 2f) continue;
                    m.Flat(To3(sh.World(u, (v0 + v1) * 0.5f), y), 0.22f, v1 - v0, sh.Yaw, melt);
                }
            }
        }

        // The roof's top over the whole footprint at height y.
        private static void Deck(RaidMesh m, Job j, float y, in Surf s)
        {
            List<int> tris = j.Tris;
            List<Vector2> pts = j.TriPts;
            for (int t = 0; t + 2 < tris.Count; t += 3)
                m.Tri(To3(pts[tris[t]], y), To3(pts[tris[t + 1]], y), To3(pts[tris[t + 2]], y), s);
        }

        private static Surf CopingSurf(Plan p) => p.Wall == WallKind.Brick || p.Wall == WallKind.Stone
            ? WallSurf(WallKind.Stone, 0) : DecorPalette.Metal(new Color(0.60f, 0.60f, 0.58f));

        // The coping along every parapet; a cornice instead along a downtown front and whatever faces the street.
        private static void Copings(Job j, float coping)
        {
            Plan p = j.P;
            RaidMesh m = j.Upper;
            Surf cap = CopingSurf(p);
            for (int i = 0; i < j.N; i++)
            {
                float len = j.Len[i];
                if (len < 0.1f) continue;
                bool cornice = p.Cornice && !j.Party(i) && (i == p.Front || FacesRoad(j, i));
                if (cornice)
                {
                    Cornice(j, i, coping);
                    continue;
                }
                Vector2 c = j.At(i, len * 0.5f) - j.Out[i] * (Thick * 0.5f);
                m.Box(new Vector3(c.x, coping + 0.05f, c.y), new Vector3(len + 0.1f, 0.1f, Thick + 0.12f), j.Yaw(i), cap);
            }
        }

        private static bool FacesRoad(Job j, int edge) => j.P.RoadDir != Vector2.zero && Vector2.Dot(j.Out[edge], j.P.RoadDir) > 0.7f
            && j.Len[edge] > 3f;

        // A pressed-metal cornice on brackets along the top of a downtown front, over a frieze.
        private static void Cornice(Job j, int i, float top)
        {
            RaidMesh m = j.Upper;
            float len = j.Len[i];
            Surf trim = j.P.Wall == WallKind.Brick && j.P.WallTone != BrickPainted ? j.TrimS : DecorPalette.Matte(j.P.Trim * 0.9f);
            OnWallBox(j, m, i, len * 0.5f, top - 0.16f, len + 0.36f, 0.32f, 0.42f, trim, -0.1f);
            OnWallBox(j, m, i, len * 0.5f, top - 0.55f, len, 0.42f, 0.08f, trim);
            int brackets = Mathf.Max(2, Mathf.RoundToInt(len / 0.9f));
            for (int k = 0; k <= brackets; k++)
            {
                float s = Mathf.Lerp(0.15f, len - 0.15f, k / (float)brackets);
                OnWallBox(j, m, i, s, top - 0.48f, 0.1f, 0.3f, 0.28f, trim);
            }
            if (j.P.Snow != SnowCover.Bare) OnWallBox(j, m, i, len * 0.5f, top + 0.04f, len + 0.2f, 0.07f, 0.3f, RoofSnow, 0.02f);
            j.Top = Mathf.Max(j.Top, top + 0.08f);
        }

        // Over a downtown front's cornice: a raised panel with a date stone, a stepped parapet, or a pediment.
        private static void FrontParapet(Job j, int e, float top)
        {
            Plan p = j.P;
            float len = j.Len[e];
            if (len < 5f) return;
            RaidMesh m = j.Upper;
            Surf cap = CopingSurf(p);
            Surf stone = WallSurf(WallKind.Stone, 0);
            float mid = len * 0.5f, u = MapStyle.Unit(j.Id, SaltParapet);
            Vector2 a = j.A[e], d = j.Dir[e], inset = -j.Out[e] * (Thick * 0.5f);
            float yaw = j.Yaw(e);
            float w, h;
            switch (p.FrontParapet)
            {
                case 1:
                    w = Mathf.Clamp(len * Mathf.Lerp(0.3f, 0.5f, u), 2.4f, 8f);
                    h = Mathf.Lerp(0.55f, 0.9f, u);
                    WallBox(j, m, a, d, inset, mid - w * 0.5f, mid + w * 0.5f, top, top + h, yaw, j.WallS, false);
                    OnWallBox(j, m, e, mid, top + h + 0.05f, w + 0.14f, 0.1f, Thick + 0.12f, cap, -Thick - 0.06f);
                    OnWall(j, m, e, mid, top + h * 0.5f, Mathf.Min(1.4f, w * 0.4f), h * 0.55f, 0.012f, stone);
                    break;
                case 2:
                    w = Mathf.Clamp(len * 0.55f, 3f, 9f);
                    h = Mathf.Lerp(0.4f, 0.6f, u);
                    WallBox(j, m, a, d, inset, mid - w * 0.5f, mid + w * 0.5f, top, top + h, yaw, j.WallS, false);
                    WallBox(j, m, a, d, inset, mid - w * 0.25f, mid + w * 0.25f, top + h, top + h * 2f, yaw, j.WallS, false);
                    OnWallBox(j, m, e, mid, top + h + 0.05f, w + 0.14f, 0.1f, Thick + 0.12f, cap, -Thick - 0.06f);
                    OnWallBox(j, m, e, mid, top + h * 2f + 0.05f, w * 0.5f + 0.14f, 0.1f, Thick + 0.12f, cap, -Thick - 0.06f);
                    OnWall(j, m, e, mid, top + h * 1.5f, Mathf.Min(1.2f, w * 0.3f), h * 0.6f, 0.012f, stone);
                    h *= 2f;
                    break;
                default:
                {
                    // A pediment: a low gable of the wall's brick, its coping up both slopes.
                    w = Mathf.Clamp(len * 0.5f, 3f, 8f);
                    h = w * Mathf.Lerp(0.14f, 0.2f, u);
                    Vector2 c0 = j.At(e, mid - w * 0.5f) + inset, c1 = j.At(e, mid + w * 0.5f) + inset, cm = j.At(e, mid) + inset;
                    Vector3 o = To3(j.Out[e], 0f) * (Thick * 0.5f);
                    Vector3 l = To3(c0, top), r = To3(c1, top), apex = To3(cm, top + h);
                    Tri3(m, l + o, r + o, apex + o, To3(j.Out[e], 0f), j.WallS);
                    Tri3(m, l - o, r - o, apex - o, To3(-j.Out[e], 0f), j.WallS);
                    Vector3 lift = Vector3.up * 0.06f;
                    Beam(m, l + lift, apex + lift, Thick + 0.14f, 0.1f, Vector3.up, cap);
                    Beam(m, r + lift, apex + lift, Thick + 0.14f, 0.1f, Vector3.up, cap);
                    OnWall(j, m, e, mid, top + h * 0.4f, Mathf.Min(1.0f, w * 0.2f), h * 0.35f, 0.012f, stone);
                    break;
                }
            }
            j.Top = Mathf.Max(j.Top, top + h + 0.12f);
        }

        // Snow drifted against the inside of the parapets on the lee of the north-west wind (and now and then another):
        // a slope from high on the wall down to the deck, a metre or two out.
        private static void Drifts(Job j, RaidMesh m, float y, System.Random rng)
        {
            Plan p = j.P;
            float wall = Mathf.Min(p.Parapet - 0.12f, 0.6f);
            if (wall < 0.15f) return;
            var lee = new Vector2(-0.6f, 0.8f);
            for (int i = 0; i < j.N; i++)
            {
                float len = j.Len[i];
                if (len < 3f) continue;
                Vector2 o = j.Out[i];
                if (Vector2.Dot(o, lee) < 0.3f && MapStyle.Unit(j.Id, SaltDrift + i) > 0.12f) continue;
                float h = wall * Range(rng, 0.65f, 1f);
                float reach = Mathf.Min(Range(rng, 1.0f, 2.2f), j.Sh.HalfW * 0.7f);
                float s0 = Thick + CornerTrim(j, i, true, reach), s1 = len - Thick - CornerTrim(j, i, false, reach);
                if (s1 - s0 < reach * 2f) continue;
                Vector2 inner = -o * Thick;
                Vector3 topA = To3(j.At(i, s0) + inner, y + h), topB = To3(j.At(i, s1) + inner, y + h);
                Vector3 footA = To3(j.At(i, s0) + inner, y), footB = To3(j.At(i, s1) + inner, y);
                Vector3 toeA = To3(j.At(i, s0 + reach * 0.7f) + inner - o * reach, y), toeB = To3(j.At(i, s1 - reach * 0.7f) + inner - o * reach, y);
                Vector3 n = (To3(-o, 0f) * h + Vector3.up * reach).normalized;
                Surf snow = RoofSnow;
                Quad4(m, toeA, toeB, topB, topA, n, snow);
                Tri3(m, topA, toeA, footA, To3(-j.Dir[i], 0f), snow);
                Tri3(m, topB, footB, toeB, To3(j.Dir[i], 0f), snow);
            }
        }

        // How far a strip of depth <paramref name="reach"/> along the inside of wall i must keep from its start (or end)
        // corner so as not to run through the next wall where they meet at a sharp angle.
        private static float CornerTrim(Job j, int i, bool start, float reach)
        {
            int other = start ? (i + j.N - 1) % j.N : (i + 1) % j.N;
            Vector2 a = start ? -j.Dir[other] : j.Dir[other], b = start ? j.Dir[i] : -j.Dir[i];
            float cos = Vector2.Dot(a, b), sin = Mathf.Abs(a.x * b.y - a.y * b.x);
            bool convex = (start ? j.Dir[other].x * j.Dir[i].y - j.Dir[other].y * j.Dir[i].x : j.Dir[i].x * j.Dir[other].y - j.Dir[i].y * j.Dir[other].x) > 0f;
            if (!convex || cos <= 0f) return 0.1f;
            return Mathf.Min(reach * 3f, reach * cos / Mathf.Max(0.2f, sin)) + 0.1f;
        }

        // Irregular patches of <paramref name="s"/> over the deck: the bare roof where the snow has gone, or the last of the
        // snow on a bare roof, clear of the walls; each a cluster of overlapping blobs.
        private static void Patches(Job j, RaidMesh m, float y, System.Random rng, in Surf s, int count, float r0, float r1)
        {
            Footprint f = j.F;
            for (int k = 0, tries = 0; k < count && tries < count * 4; tries++)
            {
                var at = new Vector2(Mathf.Lerp(f.Bounds.xMin, f.Bounds.xMax, (float)rng.NextDouble()),
                    Mathf.Lerp(f.Bounds.yMin, f.Bounds.yMax, (float)rng.NextDouble()));
                float rx = Range(rng, r0, r1), rz = rx * Range(rng, 0.5f, 0.9f);
                if (!MapGeometry.Contains(f.Pts, at) || MapGeometry.DistanceToEdge(f.Pts, at) < rx * 1.8f + Thick + 0.3f) continue;
                k++;
                float yaw = Range(rng, 0f, 180f);
                m.FlatBlob(To3(at, y), rx, rz, yaw, 0.3f, rng, s);
                int lobes = 1 + rng.Next(3);
                for (int l = 0; l < lobes; l++)
                {
                    float a = Range(rng, 0f, Mathf.PI * 2f);
                    Vector2 off = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rx * Range(rng, 0.5f, 0.9f);
                    m.FlatBlob(To3(at + off, y + 0.002f * (l + 1)), rx * Range(rng, 0.4f, 0.7f), rz * Range(rng, 0.4f, 0.7f), Range(rng, 0f, 180f), 0.3f, rng, s);
                }
            }
        }

        // A mechanical penthouse: the stair and elevator machinery over a tall block, a box on the roof's middle, its own
        // coping and snow and a band of louvres. Returns its rectangle in frame coordinates (empty when it didn't fit).
        private static Rect Penthouse(Job j, float deck, float surface, bool snowy)
        {
            Plan p = j.P;
            Shape sh = j.Sh;
            float hl = Mathf.Clamp(sh.HalfL * 0.28f, 2.5f, 9f), hw = Mathf.Clamp(sh.HalfW * 0.42f, 2.2f, 6f);
            float shift = (MapStyle.Unit(j.Id, SaltUnits + 7) - 0.5f) * sh.HalfL * 0.4f;
            Vector2 c = Vector2.zero;
            bool fits = false;
            for (int tries = 0; tries < 3 && !fits; tries++)
            {
                c = new Vector2(tries == 0 ? shift : 0f, 0f);
                fits = Fits(j, c, hl, hw, 0.8f);
                if (!fits)
                {
                    hl *= 0.72f;
                    hw *= 0.72f;
                }
            }
            if (!fits || hl < 1.2f || hw < 1.2f) return default;
            float h = p.Storeys >= 12 ? 4.6f : 3.6f;
            Vector2 at = sh.World(c);
            RaidMesh m = j.Upper;
            Surf wall = p.Curtain ? WallSurf(WallKind.Concrete, 1) : j.WallS;
            m.Box(To3(at, deck + h * 0.5f), new Vector3(hl * 2f, h, hw * 2f), sh.Yaw, wall);
            if (!j.Lite)
            {
                Surf louvre = DecorPalette.Matte(new Color(0.20f, 0.21f, 0.22f));
                for (int s = -1; s <= 1; s += 2)
                    m.Upright(To3(at + sh.V * (s * (hw + 0.01f)), deck + h * 0.72f), To3(sh.V * s, 0f), hl * 1.4f, h * 0.28f, louvre);
                m.Upright(To3(at + sh.U * (hl + 0.01f), deck + h * 0.4f), To3(sh.U, 0f), 1.0f, 2.1f, DecorPalette.Matte(new Color(0.45f, 0.46f, 0.47f)));
            }
            m.Box(To3(at, deck + h + 0.05f), new Vector3(hl * 2f + 0.2f, 0.1f, hw * 2f + 0.2f), sh.Yaw, CopingSurf(p));
            if (snowy) m.Box(To3(at, deck + h + 0.14f), new Vector3(hl * 2f - 0.12f, 0.1f, hw * 2f - 0.12f), sh.Yaw, RoofSnow);
            j.Top = Mathf.Max(j.Top, deck + h + 0.2f);
            return Rect.MinMaxRect(c.x - hl, c.y - hw, c.x + hl, c.y + hw);
        }

        // True when a rectangle of half sides hl (along U) and hw centred at c (frame coordinates) stands inside the
        // footprint with <paramref name="margin"/> to spare.
        private static bool Fits(Job j, Vector2 c, float hl, float hw, float margin)
        {
            Shape sh = j.Sh;
            List<Vector2> pts = j.F.Pts;
            for (int k = 0; k < 5; k++)
            {
                Vector2 q = k == 4 ? c : c + new Vector2((k & 1) == 0 ? -hl : hl, (k & 2) == 0 ? -hw : hw);
                Vector2 w = sh.World(q);
                if (!MapGeometry.Contains(pts, w) || MapGeometry.DistanceToEdge(pts, w) < margin) return false;
            }
            return true;
        }

        // One unit on a far roof, for its silhouette.
        private static void LiteUnit(Job j, float deck)
        {
            Shape sh = j.Sh;
            if (!Fits(j, Vector2.zero, 1.2f, 0.8f, 1f)) return;
            j.Upper.Box(To3(sh.O, deck + 0.6f), new Vector3(2.4f, 1.2f, 1.6f), sh.Yaw, DecorPalette.Matte(new Color(0.66f, 0.67f, 0.66f)));
        }

        private enum Kit { Unit, Condenser, Fan, Skylight, Hatch, Vent }

        // What stands on a flat roof, on a lattice along the building's axes: the kinds and how many by what the building
        // is and how big, each spot inside the footprint and clear of the parapets and the penthouse.
        private static void Furniture(Job j, float deck, float surface, bool snowy, in Surf cover, Rect keep, System.Random rng)
        {
            Plan p = j.P;
            Shape sh = j.Sh;
            float area = j.F.Size;
            int units, fans, skylights, condensers;
            switch (p.Arch)
            {
                case Arch.Row:
                    units = area > 450f ? 2 : 1;
                    condensers = Chance(rng, 0.5f) ? 1 : 0;
                    fans = 0;
                    skylights = Chance(rng, 0.35f) ? 1 : 0;
                    break;
                case Arch.Store:
                    units = Mathf.Clamp(Mathf.RoundToInt(area / 170f), 1, 8);
                    condensers = 0;
                    fans = Mathf.Clamp(Mathf.RoundToInt(area / 600f), 0, 3);
                    skylights = 0;
                    break;
                case Arch.Warehouse: case Arch.Industrial:
                    units = Mathf.Clamp(Mathf.RoundToInt(area / 1100f), 1, 5);
                    condensers = 0;
                    fans = Mathf.Clamp(Mathf.RoundToInt(area / 450f), 1, 10);
                    skylights = Mathf.Clamp(Mathf.RoundToInt(area / 160f), 2, 30);
                    break;
                case Arch.School:
                    units = Mathf.Clamp(Mathf.RoundToInt(area / 320f), 1, 10);
                    condensers = 0;
                    fans = Mathf.Clamp(Mathf.RoundToInt(area / 500f), 1, 6);
                    skylights = Mathf.Clamp(Mathf.RoundToInt(area / 600f), 0, 6);
                    break;
                case Arch.Apartments: case Arch.Motel:
                    units = 0;
                    condensers = Mathf.Clamp(Mathf.RoundToInt(area / 70f), 2, 12);
                    fans = 0;
                    skylights = 0;
                    break;
                default:
                    units = Mathf.Clamp(Mathf.RoundToInt(area / 240f), 1, 8);
                    condensers = Mathf.Clamp(Mathf.RoundToInt(area / 900f), 0, 3);
                    fans = Mathf.Clamp(Mathf.RoundToInt(area / 700f), 0, 4);
                    skylights = 0;
                    break;
            }
            int want = units + condensers + fans + skylights + 1;
            // The lattice: rows along the length, spaced so the wanted things about fill it.
            float du = Mathf.Clamp(Mathf.Sqrt(area / Mathf.Max(1, want)) * 0.9f, 3.2f, 12f);
            float dv = Mathf.Clamp(du * 0.8f, 3f, 9f);
            int nu = Mathf.Max(1, Mathf.FloorToInt((sh.HalfL * 2f - 2f) / du)), nv = Mathf.Max(1, Mathf.FloorToInt((sh.HalfW * 2f - 2f) / dv));
            float u0 = -(nu - 1) * du * 0.5f, v0 = -(nv - 1) * dv * 0.5f;
            RaidMesh m = j.Upper;
            // Matte, not metal: painted galvanised steel under a winter sky, which never glares at night.
            Surf metal = DecorPalette.Matte(new Color(0.66f, 0.67f, 0.66f)), dark = DecorPalette.Matte(new Color(0.14f, 0.14f, 0.15f));
            Surf glass = DecorPalette.Matte(new Color(0.24f, 0.28f, 0.31f));
            // A big building's plant is bigger.
            float scale = Mathf.Clamp(Mathf.Sqrt(area) / 34f, 1f, 2.2f);
            float yaw = sh.Yaw;
            bool hatch = false;
            int placed = 0, skip = rng.Next(3);
            for (int b = 0; b < nv && placed < want; b++)
                for (int a = 0; a < nu && placed < want; a++)
                {
                    // Every spot of a big lattice isn't needed: take them in a scattered order so they spread over the roof.
                    if (want < nu * nv && ((a * 7 + b * 3 + skip) % Mathf.Max(1, nu * nv / want)) != 0) continue;
                    var c = new Vector2(u0 + a * du + Range(rng, -0.4f, 0.4f), v0 + b * dv);
                    if (keep.width > 0f && c.x > keep.xMin - 2f && c.x < keep.xMax + 2f && c.y > keep.yMin - 2f && c.y < keep.yMax + 2f) continue;
                    if (!Fits(j, c, 1.0f * scale, 0.8f * scale, 0.9f)) continue;
                    Kit kit = skylights > 0 && (b & 1) == 0 && p.Arch != Arch.Row ? Kit.Skylight
                        : units > 0 ? Kit.Unit : condensers > 0 ? Kit.Condenser : fans > 0 ? Kit.Fan : skylights > 0 ? Kit.Skylight
                        : !hatch ? Kit.Hatch : Kit.Vent;
                    switch (kit)
                    {
                        case Kit.Unit: units--; break;
                        case Kit.Condenser: condensers--; break;
                        case Kit.Fan: fans--; break;
                        case Kit.Skylight: skylights--; break;
                        case Kit.Hatch: hatch = true; break;
                    }
                    placed++;
                    RoofThing(j, m, kit, sh.World(c), deck, surface, snowy, yaw, scale, metal, dark, glass, cover, rng);
                }
            // Vent stacks by the walls.
            int vents = Mathf.Clamp(Mathf.RoundToInt(area / 300f), 1, 5);
            for (int k = 0, tries = 0; k < vents && tries < vents * 4; tries++)
            {
                var c = new Vector2(Range(rng, -sh.HalfL + 1.2f, sh.HalfL - 1.2f), (rng.NextDouble() < 0.5 ? -1f : 1f) * (sh.HalfW - Range(rng, 0.8f, 1.6f)));
                Vector2 w = sh.World(c);
                if (!MapGeometry.Contains(j.F.Pts, w) || MapGeometry.DistanceToEdge(j.F.Pts, w) < 0.6f) continue;
                k++;
                m.Cylinder(To3(w, deck), Range(rng, 0.05f, 0.1f), Range(rng, 0.5f, 1.1f), 6, metal);
            }
            if (area > 3000f) Curbs(j, m, surface);
        }

        // One thing on a flat roof, at w (plan) on the deck, <paramref name="scale"/> times its usual size.
        private static void RoofThing(Job j, RaidMesh m, Kit kit, Vector2 w, float deck, float surface, bool snowy, float yaw, float scale,
            in Surf metal, in Surf dark, in Surf glass, in Surf cover, System.Random rng)
        {
            Shape sh = j.Sh;
            switch (kit)
            {
                case Kit.Unit:
                {
                    // A packaged air handler: a long box, a grille down one side, ducts into the roof, a ring melted round it.
                    var size = new Vector3(Range(rng, 1.8f, 2.8f), Range(rng, 0.9f, 1.3f), Range(rng, 1.1f, 1.6f)) * scale;
                    size.y = Mathf.Min(size.y, 2.2f);
                    if (snowy) m.FlatBlob(To3(w, surface + 0.006f), size.x * 0.8f, size.z * 0.85f, yaw, 0.25f, rng, cover);
                    m.Box(To3(w, deck + size.y * 0.5f + 0.15f), size, yaw, metal);
                    m.Box(To3(w, deck + 0.075f), new Vector3(size.x * 0.9f, 0.15f, size.z * 0.9f), yaw, dark);
                    m.Upright(To3(w + sh.V * (size.z * 0.5f + 0.005f), deck + 0.15f + size.y * 0.5f), To3(sh.V, 0f), size.x * 0.8f, size.y * 0.6f, dark);
                    if (snowy) m.Box(To3(w, deck + 0.15f + size.y + 0.04f), new Vector3(size.x * 0.96f, 0.08f, size.z * 0.96f), yaw, RoofSnow);
                    break;
                }
                case Kit.Condenser:
                {
                    // A condenser: a small box, its fan grille on top.
                    float s = Range(rng, 0.7f, 0.95f);
                    m.Box(To3(w, deck + s * 0.45f), new Vector3(s, s * 0.9f, s), yaw, DecorPalette.Matte(new Color(0.72f, 0.70f, 0.64f)));
                    m.Cylinder(To3(w, deck + s * 0.9f), s * 0.36f, 0.02f, 10, dark);
                    if (snowy) m.FlatBlob(To3(w, surface + 0.006f), s * 0.9f, s * 0.8f, yaw, 0.3f, rng, cover);
                    break;
                }
                case Kit.Fan:
                {
                    // An exhaust fan: a squat drum under a mushroom cap.
                    float k = Mathf.Min(scale, 1.6f);
                    m.Cylinder(To3(w, deck), 0.3f * k, 0.5f * k, 8, metal);
                    m.Cylinder(To3(w, deck + 0.5f * k), 0.52f * k, 0.16f * k, 10, metal, Quaternion.identity, 0.6f);
                    if (snowy) m.Cylinder(To3(w, deck + 0.66f * k), 0.3f * k, 0.06f, 8, RoofSnow);
                    break;
                }
                case Kit.Skylight:
                {
                    // A skylight on its curb, half under the snow.
                    var size = new Vector3(2.4f, 0.4f, 1.3f);
                    if (scale > 1.4f) size.x = 3.6f;
                    m.Box(To3(w, deck + size.y * 0.5f), size, yaw, metal);
                    m.Flat(To3(w, deck + size.y + 0.01f), size.x * 0.9f, size.z * 0.8f, yaw, glass);
                    if (snowy)
                        m.Box(To3(w - sh.U * (size.x * 0.22f), deck + size.y + 0.05f), new Vector3(size.x * 0.5f, 0.07f, size.z * 0.84f), yaw, RoofSnow);
                    break;
                }
                case Kit.Hatch:
                    m.Box(To3(w, deck + 0.25f), new Vector3(0.95f, 0.5f, 0.95f), yaw, metal);
                    if (snowy) m.Box(To3(w, deck + 0.54f), new Vector3(0.9f, 0.07f, 0.9f), yaw, RoofSnow);
                    break;
                default:
                    m.Cylinder(To3(w, deck), 0.08f, 0.9f, 6, metal);
                    break;
            }
        }

        // Expansion joints across a very big roof: low curbs every 30 m or so, across its short way.
        private static void Curbs(Job j, RaidMesh m, float surface)
        {
            Shape sh = j.Sh;
            Surf curb = DecorPalette.Matte(new Color(0.42f, 0.42f, 0.41f));
            int n = Mathf.FloorToInt(sh.HalfL * 2f / 30f);
            for (int k = 1; k <= n; k++)
            {
                float u = -sh.HalfL + sh.HalfL * 2f * k / (n + 1);
                Spans(j, u, false);
                for (int s = 0; s + 1 < s_Span.Count; s += 2)
                {
                    float v0 = s_Span[s] + 0.4f, v1 = s_Span[s + 1] - 0.4f;
                    if (v1 - v0 < 2f) continue;
                    m.Box(To3(sh.World(u, (v0 + v1) * 0.5f), surface + 0.1f), new Vector3(0.35f, 0.2f, v1 - v0), sh.Yaw, curb);
                }
            }
        }
    }
}
