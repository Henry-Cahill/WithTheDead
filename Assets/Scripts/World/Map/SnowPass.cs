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
            WallDrifts(site, plan);
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
        // Drifts along the foot of every wall the square's buildings stand on: lumps a couple of metres long, deepest where
        // the wall faces away from the wind; none at a door, on a lot, on the shovelled sidewalk, under a parked car or
        // against a neighbour's wall.
        private static void WallDrifts(Site site, NaturePlan plan)
        {
            var doors = new Dictionary<Footprint, EnterableBuilding>();
            foreach (EnterableBuilding e in site.Enterables)
                if (e.Footprint != null) doors[e.Footprint] = e;
            Surf snow = DecorPalette.Matte(Snow), shade = DecorPalette.Matte(Shade);
            foreach (Footprint f in site.Area.Buildings)
            {
                if (f.Pts.Count < 3 || f.Size < 12f || f.Data.kind == "canopy" || !plan.InSquare(f.Middle, 0f)) continue;
                doors.TryGetValue(f, out EnterableBuilding entry);
                List<Vector2> p = f.Pts;
                for (int i = 0; i < p.Count; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % p.Count];
                    float len = Vector2.Distance(a, b);
                    if (len < 1.2f) continue;
                    Vector2 d = (b - a) / len, outward = new Vector2(d.y, -d.x);
                    long key = NaturePlan.Combine(f.Data.id, i);
                    float lee = Vector2.Dot(outward, Downwind), size = MapStyle.Unit(key, SaltDrift);
                    float depth, height;
                    if (lee > 0.35f)
                    {
                        depth = 0.9f + size * 0.7f;
                        height = 0.36f + size * 0.24f;
                    }
                    else if (lee > -0.35f)
                    {
                        depth = 0.55f + size * 0.3f;
                        height = 0.22f + size * 0.12f;
                    }
                    else
                    {
                        if (size < 0.5f) continue;   // half the windward walls are scoured bare
                        depth = 0.4f;
                        height = 0.15f;
                    }
                    int lumps = Mathf.Max(1, Mathf.CeilToInt(len / 1.9f));
                    float step = len / lumps;
                    Quaternion facing = Quaternion.LookRotation(new Vector3(outward.x, 0f, outward.y));
                    for (int k = 0; k < lumps; k++)
                    {
                        long lk = NaturePlan.Combine(key, k);
                        float t = step * (k + 0.5f) + (MapStyle.Unit(lk, SaltDrift + 1) - 0.5f) * step * 0.3f;
                        float dep = depth * (0.75f + MapStyle.Unit(lk, SaltDrift + 2) * 0.35f);
                        Vector2 wall = a + d * t, foot = wall + outward * (dep * 0.5f - 0.06f);
                        if (!plan.InSquare(foot, 0.6f) || NearDoor(entry, wall, 1.9f)) continue;
                        if (plan.RoadClearance(foot, 3f) < 1.4f || plan.IsKept(foot, 0.4f) || plan.LandAt(foot) == Land.Lot) continue;
                        if (plan.WallDistance(foot + outward * (dep * 0.5f + 0.3f), 0.5f) < 0.3f) continue;
                        float lumpLen = Mathf.Min(step * (1.15f + MapStyle.Unit(lk, SaltDrift + 3) * 0.3f), len);
                        float hgt = height * (0.8f + MapStyle.Unit(lk, SaltDrift + 4) * 0.4f);
                        Vector3 c = To3(foot, 0.02f);
                        site.Town.Out.Solid(c).Ellipsoid(c, new Vector3(lumpLen, hgt * 2f, dep), facing, MapStyle.Unit(lk, SaltDrift + 5) < 0.6f ? snow : shade, 6, 2);
                    }
                }
            }
        }

        private static bool NearDoor(EnterableBuilding e, Vector2 at, float within)
        {
            if (e == null) return false;
            foreach (Doorway d in e.Doors)
                if (Vector2.Distance(d.At, at) < within) return true;
            return false;
        }

        // Drifts along the fences, walls and hedges, on their lee side, in runs with gaps (none where a road or path crosses).
        private static void FenceDrifts(Site site, NaturePlan plan)
        {
            var stations = new List<NaturePlan.Station>(64);
            Surf snow = DecorPalette.Matte(Snow), shade = DecorPalette.Matte(Shade);
            foreach (Line l in site.Area.Barriers)
            {
                string kind = l.Data.kind;
                if (kind != "fence" && kind != "wall" && kind != "hedge" && kind != "retaining_wall") continue;
                plan.Stations(l.Pts, 2.2f, stations);
                foreach (NaturePlan.Station s in stations)
                {
                    long key = NaturePlan.Combine(l.Data.id, s.K);
                    if (plan.NoiseAt(s.At, 24.0, SaltFence) < 0.38f || MapStyle.Unit(key, SaltFence) < 0.12f) continue;
                    Vector2 normal = new Vector2(-s.Dir.y, s.Dir.x);
                    float lee = Vector2.Dot(normal, Downwind);
                    if (Mathf.Abs(lee) < 0.15f && MapStyle.Unit(key, SaltFence + 1) < 0.5f) continue;   // the wind runs along it
                    Vector2 side = lee >= 0f ? normal : -normal;
                    float dep = 0.55f + MapStyle.Unit(key, SaltFence + 2) * 0.4f, hgt = 0.2f + MapStyle.Unit(key, SaltFence + 3) * 0.16f;
                    Vector2 foot = s.At + side * (dep * 0.5f + 0.05f);
                    if (!plan.InSquare(foot, 0.6f) || plan.RoadClearance(foot, 2f) < 0.8f || plan.IsKept(foot, 0.3f)) continue;
                    Land land = plan.LandAt(foot);
                    if (land == Land.Lot || land == Land.Water || plan.WallDistance(foot, 1f) < 0.5f) continue;
                    Vector3 c = To3(foot, 0.02f);
                    Quaternion facing = Quaternion.LookRotation(new Vector3(side.x, 0f, side.y));
                    site.Town.Out.Solid(c).Ellipsoid(c, new Vector3(2.5f, hgt * 2f, dep), facing, MapStyle.Unit(key, SaltFence + 4) < 0.6f ? snow : shade, 6, 2);
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
