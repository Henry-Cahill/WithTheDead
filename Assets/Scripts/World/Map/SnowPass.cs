using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's lying snow, past what the ground, the roads and the roofs carry: the plough's dirty heaps in
    /// the parking lots' corners (planned before the lots fill, <see cref="PlanPiles"/>, and solid when they're big),
    /// drifts banked against the buildings' walls (deep in the lee of the north-west wind, a scoured ridge on the windward
    /// side, none where the sidewalk's been shovelled, at the doors or on the lots) and along the fences and walls, the
    /// heaps shovelled off the driveways beside their mouths, and here and there a snowman in a front yard. Full sites only.
    /// </summary>
    internal static class SnowPass
    {
        // Salts for the snow's random choices (MapStyle).
        private const int SaltPile = 0x5A00, SaltDrift = 0x5A10, SaltFence = 0x5A20, SaltShovel = 0x5A30, SaltSnowman = 0x5A40;

        /// <summary>Which way the January wind blows the snow: from the north-west, so it banks up on the south-east sides.</summary>
        private static readonly Vector2 Downwind = new Vector2(0.6f, -0.8f);

        private static readonly Color Snow = DecorProps.SnowWhite, Shade = DecorProps.SnowShade;
        private static readonly Color Dirty = new Color(0.62f, 0.62f, 0.61f), Grit = new Color(0.42f, 0.41f, 0.40f);

        internal static IEnumerable<string> Build(Site site)
        {
            if (site.Detail == Detail.Lite) yield break;
            NaturePlan plan = NaturePlan.For(site);
            PlanPiles(site, plan);
            LotPiles(site, plan);
            yield return "plough piles";
            foreach (string step in WallDrifts(site, plan)) yield return step;
            yield return "wall drifts";
            FenceDrifts(site, plan);
            yield return "fence drifts";
            YardSnow(site, plan);
            yield return "yard snow";
        }

        /// <summary>Kept for <see cref="StreetscapePass"/>, which calls it until it drops the call: this pass heaps its own
        /// snow now (<see cref="Build"/>).</summary>
        internal static void Piles(Site site) { }

        #region Plough piles
        /// <summary>
        /// Plans the plough's heaps in the parking lots inside the square (once a site; <see cref="VehiclePass"/> calls it
        /// before filling the stalls): in most of a lot's own sharp corners (not the corners a seam cuts), a few metres in,
        /// bigger in bigger lots; each kept clear (<see cref="NaturePlan.Piles"/>).
        /// </summary>
        internal static void PlanPiles(Site site, NaturePlan plan)
        {
            if (plan.PilesPlanned) return;
            plan.PilesPlanned = true;
            if (plan.Lite) return;
            float half = plan.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Patch pa in site.Area.Areas)
            {
                if (pa.Data.kind != "parking") continue;
                List<Vector2> poly = MapGeometry.ClipPolygon(pa.Pts, square);
                if (poly.Count < 3) continue;
                float area = Mathf.Abs(MapGeometry.SignedArea(poly));
                if (area < 150f) continue;
                float scale = Mathf.Clamp(Mathf.Sqrt(area) / 45f, 0.6f, 1.6f);
                int made = 0, n = poly.Count;
                for (int i = 0; i < n && made < 4; i++)
                {
                    Vector2 prev = poly[(i + n - 1) % n], at = poly[i], next = poly[(i + 1) % n];
                    if (plan.CutVertex(at) || (at - prev).sqrMagnitude < 1f || (next - at).sqrMagnitude < 1f) continue;
                    Vector2 d0 = (at - prev).normalized, d1 = (next - at).normalized;
                    if (d0.x * d1.y - d0.y * d1.x <= 0f || Vector2.Angle(-d0, d1) > 140f) continue;   // a convex corner, not too blunt
                    long key = NaturePlan.Combine((long)System.Math.Round(plan.AbsX(at.x)), (long)System.Math.Round(plan.AbsZ(at.y)));
                    if (MapStyle.Unit(key, SaltPile) < 0.2f) continue;
                    Vector2 inward = (d1 - d0).normalized;
                    float w = (2.2f + MapStyle.Unit(key, SaltPile + 1) * 1.6f) * scale;
                    float depth = (1.8f + MapStyle.Unit(key, SaltPile + 2) * 1.2f) * scale;
                    float h = (0.8f + MapStyle.Unit(key, SaltPile + 3) * 0.7f) * Mathf.Sqrt(scale);
                    Vector2 c = at + inward * (Mathf.Max(w, depth) * 0.5f + 0.6f);
                    if (!plan.InSquare(c, 1f) || !MapGeometry.Contains(poly, c) || plan.WallDistance(c, 3f) < 1.2f) continue;
                    plan.Piles.Add(new NaturePlan.Pile { At = c, Yaw = Mathf.Atan2(inward.x, inward.y) * Mathf.Rad2Deg, Width = w, Depth = depth, Height = h, Key = key });
                    plan.KeepRect(c, inward, depth * 0.5f + 0.7f, w * 0.5f + 0.7f, NaturePlan.KeepKind.Pile);
                    made++;
                }
            }
        }

        // The planned heaps: grey-white lumps of ploughed snow, grit in them, a collider round the big ones.
        private static void LotPiles(Site site, NaturePlan plan)
        {
            foreach (NaturePlan.Pile p in plan.Piles)
            {
                System.Random rng = MapStyle.Rng(p.Key, SaltPile + 9);
                Vector3 at = To3(p.At, 0f);
                RaidMesh m = site.Town.Out.Solid(at);
                m.SetFrame(at, p.Yaw);
                Heap(m, rng, p.Width, p.Depth, p.Height, true);
                m.ResetFrame();
                if (p.Height > 0.7f)
                    site.Town.PropCollider(at + Vector3.up * (p.Height * 0.4f), new Vector3(p.Width * 0.75f, p.Height * 0.8f, p.Depth * 0.75f), p.Yaw);
                site.Take(p.At, Mathf.Max(p.Width, p.Depth) * 0.5f);
            }
        }

        /// <summary>A heap of snow <paramref name="w"/> wide (x), <paramref name="d"/> deep (z) and <paramref name="h"/>
        /// high round the frame's origin: a broad base, lumps on it, a crown; grey and gritty when the plough made it.</summary>
        private static void Heap(RaidMesh m, System.Random rng, float w, float d, float h, bool ploughed)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            Surf Pick() => DecorPalette.Matte(!ploughed ? (rng.NextDouble() < 0.5 ? Snow : Shade)
                : rng.NextDouble() < 0.45 ? Dirty : rng.NextDouble() < 0.6 ? Shade : Snow);
            m.Ellipsoid(new Vector3(0f, 0f, 0f), new Vector3(w, h * 1.3f, d), Quaternion.Euler(0f, R(-8f, 8f), 0f), Pick(), 8, 3);
            int lumps = ploughed ? rng.Next(3, 5) : rng.Next(1, 3);
            for (int i = 0; i < lumps; i++)
            {
                var off = new Vector3(R(-0.28f, 0.28f) * w, h * R(0.15f, 0.4f), R(-0.28f, 0.28f) * d);
                float k = R(0.45f, 0.7f);
                m.Ellipsoid(off, new Vector3(w * k, h * k * 1.3f, d * k), Quaternion.Euler(0f, R(0f, 180f), 0f), Pick(), 7, 3);
            }
            m.Ellipsoid(new Vector3(R(-0.1f, 0.1f) * w, h * 0.62f, R(-0.1f, 0.1f) * d), new Vector3(w * 0.4f, h * 0.7f, d * 0.4f), Pick(), 6, 3);
            if (!ploughed) return;
            Surf grit = DecorPalette.Matte(Grit);
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3(R(-0.3f, 0.3f) * w, h * R(0.25f, 0.55f), R(-0.3f, 0.3f) * d), new Vector3(R(0.08f, 0.2f), 0.05f, R(0.08f, 0.2f)),
                    Quaternion.Euler(R(-20f, 20f), R(0f, 90f), R(-20f, 20f)), grit);
        }
        #endregion

        #region Drifts
        private const float DriftStep = 1.1f;   // metres between a drift's cross-sections
        private static float[] s_T = new float[64], s_H = new float[64], s_D = new float[64];

        // Grows the scratch arrays to hold n stations.
        private static void Room(int n)
        {
            if (s_T.Length >= n) return;
            int size = Mathf.NextPowerOfTwo(n);
            s_T = new float[size];
            s_H = new float[size];
            s_D = new float[size];
        }

        /// <summary>
        /// A bank of snow along a straight run from <paramref name="a"/> (unit <paramref name="dir"/>) out towards
        /// <paramref name="outward"/>: at each of <paramref name="n"/> stations (<see cref="s_T"/> metres along) its own
        /// height and depth (<see cref="s_H"/>, <see cref="s_D"/>; 0 for none there), its cross-section a convex wedge
        /// from just behind the line (so no gap shows at the wall) out to the ground. Flat-shaded; nothing where the bank
        /// is flat on both sides of a gap.
        /// </summary>
        private static void Bank(RaidMesh m, Vector2 a, Vector2 dir, Vector2 outward, int n, in Surf snow)
        {
            for (int i = 0; i + 1 < n; i++)
            {
                if (s_H[i] < 0.01f && s_H[i + 1] < 0.01f) continue;
                Vector2 p0 = a + dir * s_T[i], p1 = a + dir * s_T[i + 1];
                Vector3 b0 = Point(p0, outward, -0.05f, s_H[i]), b1 = Point(p1, outward, -0.05f, s_H[i + 1]);
                Vector3 m0 = Point(p0, outward, s_D[i] * 0.42f, s_H[i] * 0.68f), m1 = Point(p1, outward, s_D[i + 1] * 0.42f, s_H[i + 1] * 0.68f);
                Vector3 f0 = Point(p0, outward, Mathf.Max(0.05f, s_D[i]), 0f), f1 = Point(p1, outward, Mathf.Max(0.05f, s_D[i + 1]), 0f);
                Quad(m, b0, b1, m1, m0, snow);
                Quad(m, m0, m1, f1, f0, snow);
            }
        }

        private static Vector3 Point(Vector2 along, Vector2 outward, float off, float h) => To3(along + outward * off, h + 0.004f);

        // Two triangles over a quad whose corners run round it, turned to face up.
        private static void Quad(RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, in Surf s)
        {
            if (Vector3.Cross(b - a, c - a).y < 0f)
            {
                Vector3 t = b;
                b = d;
                d = t;
            }
            m.Tri(a, b, c, s);
            m.Tri(a, c, d, s);
        }

        /// <summary>How deep the snow banks against a face looking along <paramref name="outward"/>: deep in the lee of the
        /// wind, a scoured ridge on the windward side (half of which are bare).</summary>
        private static bool Exposure(Vector2 outward, float u, out float depth, out float height)
        {
            float lee = Vector2.Dot(outward, Downwind);
            if (lee > 0.35f)
            {
                depth = 1.2f + u * 0.8f;
                height = 0.42f + u * 0.26f;
            }
            else if (lee > -0.35f)
            {
                depth = 0.8f + u * 0.4f;
                height = 0.26f + u * 0.12f;
            }
            else
            {
                depth = 0.5f;
                height = 0.16f;
                if (u < 0.5f) return false;
            }
            return true;
        }

        // Snow banked along the foot of the walls of every building the building pass raised (its shells), drifting in and
        // out along the wall; none at a door, on the road, on a lot or under a parked car; squeezed between neighbours.
        private static IEnumerable<string> WallDrifts(Site site, NaturePlan plan)
        {
            var doors = new Dictionary<Footprint, EnterableBuilding>();
            foreach (EnterableBuilding e in site.Enterables)
                if (e.Footprint != null) doors[e.Footprint] = e;
            var seen = new HashSet<Footprint>();
            Surf snow = DecorPalette.Matte(Snow);
            int done = 0;
            for (int s = 0; s < site.Shells.Count; s++)
            {
                Footprint f = site.Shells[s].Footprint;
                if (f == null || f.Pts.Count < 3 || f.Size < 12f || f.Data.kind == "canopy" || !seen.Add(f)) continue;
                if (!f.Owned && !plan.InSquare(f.Middle, 0f)) continue;   // scenery past the edge
                doors.TryGetValue(f, out EnterableBuilding entry);
                List<Vector2> p = f.Pts;
                RaidMesh m = site.Town.Out.Solid(To3(f.Middle, 0f));
                for (int i = 0; i < p.Count; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % p.Count];
                    float len = Vector2.Distance(a, b);
                    if (len < 1.2f) continue;
                    Vector2 dir = (b - a) / len, outward = new Vector2(dir.y, -dir.x);
                    long key = NaturePlan.Combine(f.Data.id, i);
                    if (!Exposure(outward, MapStyle.Unit(key, SaltDrift), out float depth, out float height)) continue;
                    int n = Mathf.Max(2, Mathf.CeilToInt(len / DriftStep) + 1);
                    Room(n);
                    for (int k = 0; k < n; k++)
                    {
                        float t = len * k / (n - 1);
                        Vector2 at = a + dir * t;
                        float wave = plan.NoiseAt(at, 5.0, SaltDrift + 1);
                        float h = height * (0.45f + 0.75f * wave), d = depth * (0.75f + 0.5f * wave);
                        float end = Mathf.Min(t, len - t);
                        if (end < 0.9f) h *= Mathf.Lerp(0.3f, 1f, end / 0.9f);
                        Vector2 foot = at + outward * (d * 0.5f);
                        if (NearDoor(entry, at, 1.7f) || plan.RoadClearance(foot, 1f) < 0.2f || plan.LandAt(foot) == Land.Lot || plan.IsKept(foot, 0.3f))
                            h = 0f;
                        else if (plan.WallDistance(at + outward * (d + 0.3f), 0.6f) < 0.35f)
                        {
                            d *= 0.5f;
                            h *= 0.6f;
                        }
                        s_T[k] = t;
                        s_H[k] = h;
                        s_D[k] = d;
                    }
                    Bank(m, a, dir, outward, n, snow);
                }
                if (++done % 12 == 0) yield return "wall drifts";
            }
        }

        private static bool NearDoor(EnterableBuilding e, Vector2 at, float within)
        {
            if (e == null) return false;
            foreach (Doorway d in e.Doors)
                if (Vector2.Distance(d.At, at) < within) return true;
            return false;
        }

        // Banks along the fences, walls and hedges on their lee side, coming and going with the noise; none where they'd
        // lie on a road, a lot or a parked car. The cross-sections stand on the state's lattice, so a fence cut by a
        // seam drifts on unbroken.
        private static void FenceDrifts(Site site, NaturePlan plan)
        {
            Surf snow = DecorPalette.Matte(Snow);
            foreach (Line l in site.Area.Barriers)
            {
                string kind = l.Data.kind;
                if (kind != "fence" && kind != "wall" && kind != "hedge" && kind != "retaining_wall") continue;
                for (int i = 0; i + 1 < l.Pts.Count; i++)
                {
                    Vector2 a = l.Pts[i], b = l.Pts[i + 1];
                    float len = Vector2.Distance(a, b);
                    if (len < 0.5f) continue;
                    Vector2 dir = (b - a) / len, normal = new Vector2(-dir.y, dir.x);
                    float lee = Vector2.Dot(normal, Downwind);
                    Vector2 outward = lee >= 0f ? normal : -normal;
                    float scale = Mathf.Lerp(0.55f, 1f, Mathf.Abs(lee));
                    bool cutA = plan.CutVertex(a), cutB = plan.CutVertex(b);
                    // Stations where the segment crosses the lattice's lines, and its ends.
                    bool alongX = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
                    double from = alongX ? plan.AbsX(a.x) : plan.AbsZ(a.y), to = alongX ? plan.AbsX(b.x) : plan.AbsZ(b.y);
                    double span = to - from;
                    double step = DriftStep * (alongX ? Mathf.Abs(dir.x) : Mathf.Abs(dir.y));
                    long k0 = (long)System.Math.Floor(System.Math.Min(from, to) / step) + 1, k1 = (long)System.Math.Ceiling(System.Math.Max(from, to) / step) - 1;
                    int n = (int)System.Math.Max(0, k1 - k0 + 1) + 2;
                    Room(n);
                    int c = 0;
                    s_T[c++] = 0f;
                    if (span > 0)
                        for (long k = k0; k <= k1; k++) s_T[c++] = (float)((k * step - from) / span) * len;
                    else
                        for (long k = k1; k >= k0; k--) s_T[c++] = (float)((k * step - from) / span) * len;
                    s_T[c++] = len;
                    for (int j = 0; j < c; j++)
                    {
                        Vector2 at = a + dir * s_T[j];
                        float wave = plan.NoiseAt(at, 9.0, SaltFence), gap = plan.NoiseAt(at, 31.0, SaltFence + 1);
                        float h = gap < 0.35f ? 0f : (0.2f + 0.22f * wave) * scale * Mathf.Clamp01((gap - 0.35f) * 6f);
                        float d = (0.7f + 0.6f * wave) * scale;
                        if ((j == 0 && !cutA) || (j == c - 1 && !cutB)) h *= 0.3f;
                        Vector2 foot = at + outward * (d * 0.5f);
                        if (!plan.InSquare(foot, -0.5f) || plan.RoadClearance(foot, 1f) < 0.3f || plan.LandAt(foot) == Land.Lot
                            || plan.LandAt(foot) == Land.Water || plan.IsKept(foot, 0.3f) || plan.WallDistance(foot, 0.6f) < 0.4f)
                            h = 0f;
                        s_H[j] = h;
                        s_D[j] = d;
                    }
                    Bank(site.Town.Out.Solid(To3((a + b) * 0.5f, 0f)), a, dir, outward, c, snow);
                }
            }
        }
        #endregion

        #region Yards
        // The heaps shovelled off each driveway, either side of its mouth, and a snowman in one front yard in twenty.
        private static void YardSnow(Site site, NaturePlan plan)
        {
            foreach (NaturePlan.CarSpot s in plan.Driveways)
            {
                if (s.StripWidth <= 0f) continue;
                Vector2 dir = (s.At - s.Mouth).normalized, across = new Vector2(-dir.y, dir.x);
                for (int side = -1; side <= 1; side += 2)
                {
                    long key = NaturePlan.Combine(s.Key, 40 + side);
                    if (MapStyle.Unit(key, SaltShovel) < 0.35f) continue;
                    Vector2 at = s.Mouth + dir * (1.0f + MapStyle.Unit(key, SaltShovel + 1) * 2.5f) + across * (side * (s.StripWidth * 0.5f + 0.75f));
                    if (!plan.InSquare(at, 1f) || plan.RoadClearance(at, 2f) < 0.4f || plan.IsKept(at, 0.5f) || plan.WallDistance(at, 1.5f) < 1f) continue;
                    System.Random rng = MapStyle.Rng(key, SaltShovel + 2);
                    Vector3 c = To3(at, 0f);
                    RaidMesh m = site.Town.Out.Solid(c);
                    m.SetFrame(c, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg);
                    Heap(m, rng, 0.9f + (float)rng.NextDouble() * 0.6f, 1.2f + (float)rng.NextDouble() * 0.8f, 0.32f + (float)rng.NextDouble() * 0.2f, false);
                    m.ResetFrame();
                    site.Take(at, 0.7f);
                }
            }
            foreach (Footprint f in site.Area.Buildings)
            {
                if (!NaturePlan.IsHome(f.Data) || f.Data.kind == "apartments" || !plan.InSquare(f.Middle, 0f)) continue;
                long key = f.Data.id;
                if (MapStyle.Unit(key, SaltSnowman) >= 0.05f) continue;
                NaturePlan.HouseFrame hf = plan.FrameOf(f);
                if (!hf.Street) continue;
                for (int k = 0; k < 4; k++)
                {
                    long sk = NaturePlan.Combine(key, 60 + k);
                    Vector2 at = hf.At(Mathf.Lerp(hf.T0, hf.T1, MapStyle.Unit(sk, SaltSnowman + 1)), hf.N0 - 2.5f - MapStyle.Unit(sk, SaltSnowman + 2) * 3f);
                    if (!plan.InSquare(at, 1f) || plan.RoadClearance(at, 3f) < 2.4f || plan.WallDistance(at, 2f) < 1.6f || plan.IsKept(at, 1f)) continue;
                    if (!site.FreeBeside(at, 0.6f) && !InBox(f.Bounds, at, 1.5f)) continue;
                    Vector3 c = To3(at, 0f);
                    RaidMesh m = site.Town.Out.Solid(c);
                    m.SetFrame(c, Mathf.Atan2(-hf.N.x, -hf.N.y) * Mathf.Rad2Deg + (MapStyle.Unit(sk, SaltSnowman + 3) - 0.5f) * 50f);
                    Snowman(m, MapStyle.Rng(sk, SaltSnowman + 4));
                    m.ResetFrame();
                    site.Take(at, 0.6f);
                    break;
                }
            }
        }

        private static bool InBox(Rect box, Vector2 p, float margin)
            => p.x > box.xMin - margin && p.x < box.xMax + margin && p.y > box.yMin - margin && p.y < box.yMax + margin;

        /// <summary>A snowman facing +Z: three balls, coal eyes and buttons, a carrot, stick arms; a scarf and an old hat on
        /// some.</summary>
        private static void Snowman(RaidMesh m, System.Random rng)
        {
            float s = 0.85f + (float)rng.NextDouble() * 0.3f;
            Surf snow = DecorPalette.Matte(Snow), coal = DecorPalette.Matte(new Color(0.06f, 0.06f, 0.06f));
            Surf stick = DecorPalette.Matte(new Color(0.30f, 0.22f, 0.15f));
            float r0 = 0.42f * s, r1 = 0.31f * s, r2 = 0.22f * s;
            float y0 = r0 * 0.85f, y1 = y0 + r0 * 0.8f + r1 * 0.75f, y2 = y1 + r1 * 0.8f + r2 * 0.8f;
            m.Ellipsoid(new Vector3(0f, y0, 0f), new Vector3(r0, r0 * 0.95f, r0) * 2f, snow, 8, 5);
            m.Ellipsoid(new Vector3(0f, y1, 0f), new Vector3(r1, r1, r1) * 2f, snow, 8, 4);
            m.Ellipsoid(new Vector3(0f, y2, 0f), new Vector3(r2, r2, r2) * 2f, snow, 7, 4);
            for (int i = 0; i < 3; i++)
                m.Box(new Vector3(0f, y1 + r1 * (0.45f - i * 0.4f), r1 * (0.97f - Mathf.Abs(0.45f - i * 0.4f) * 0.25f)), new Vector3(0.05f, 0.05f, 0.04f), coal);
            for (int side = -1; side <= 1; side += 2)
            {
                m.Box(new Vector3(side * r2 * 0.38f, y2 + r2 * 0.25f, r2 * 0.9f), new Vector3(0.045f, 0.045f, 0.03f), coal);
                Vector3 from = new Vector3(side * r1 * 0.85f, y1 + r1 * 0.2f, 0f);
                Vector3 to = from + new Vector3(side * (0.45f + (float)rng.NextDouble() * 0.2f), 0.25f + (float)rng.NextDouble() * 0.2f, (float)rng.NextDouble() * 0.2f - 0.1f);
                m.Rod(from, to, 0.035f, stick, 3);
                m.Rod(Vector3.Lerp(from, to, 0.7f), Vector3.Lerp(from, to, 0.7f) + new Vector3(side * 0.08f, 0.14f, 0.04f), 0.025f, stick, 3);
            }
            m.Cylinder(new Vector3(0f, y2, r2 * 0.85f), 0.035f, 0.22f * s, 5, DecorPalette.Matte(new Color(0.85f, 0.42f, 0.10f)), Quaternion.Euler(90f, 0f, 0f), 0.1f, false);
            if (rng.NextDouble() < 0.5)
                m.Cylinder(new Vector3(0f, y1 + r1 * 0.72f, 0f), r1 * 0.72f, 0.09f, 8,
                    DecorPalette.Matte(rng.NextDouble() < 0.5 ? new Color(0.62f, 0.10f, 0.09f) : new Color(0.14f, 0.24f, 0.46f)), Quaternion.identity, 0.9f, false);
            if (rng.NextDouble() < 0.4)
            {
                Surf hat = DecorPalette.Matte(new Color(0.09f, 0.09f, 0.10f));
                m.Cylinder(new Vector3(0f, y2 + r2 * 0.8f, 0f), r2 * 1.05f, 0.03f, 8, hat);
                m.Cylinder(new Vector3(0f, y2 + r2 * 0.8f, 0f), r2 * 0.68f, 0.2f * s, 8, hat);
            }
        }
        #endregion
    }
}
