using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;
using static CloneZ.StreetscapePass;

namespace CloneZ
{
    /// <summary>
    /// The street lights, as a Kentucky town had them in 1993: cobra heads on arms off the wooden utility poles (at the
    /// corners and every few poles along the town's streets, at the junctions out in the country), galvanised davit poles
    /// along the main roads through town, black post-tops with acorn globes along the downtown sidewalks, a mercury
    /// "dusk-to-dawn" light on a pole in the farmyards, and the mapped lamps where they're mapped (a street with mapped lamps
    /// gets no others). Some still burn — more downtown, fewer out of town — and those become <see cref="StreetLamp"/>s; the
    /// real lights are capped per site and the runtime lights only the nearest anyway.
    /// </summary>
    internal static class StreetLights
    {
        private const int SaltLit = 0x117A, SaltFlicker = 0xF11C, SaltMercury = 0x3E7C, SaltPoleLamp = 0x9A3B, SaltFarm = 0xFA53,
            SaltBronze = 0xB207;
        private const float AcornSpacing = 27f, DavitSpacing = 46f;
        private static readonly Color Sodium = new Color(1f, 0.60f, 0.28f);
        private static readonly Color Mercury = new Color(0.78f, 0.90f, 1f);
        private static readonly Color Incandescent = new Color(1f, 0.80f, 0.55f);
        private static readonly Color AcornBlack = new Color(0.06f, 0.07f, 0.07f);
        private static readonly Color GlobeMilk = new Color(0.72f, 0.72f, 0.68f);

        /// <summary>Chooses every lamp: mapped ones, the poles that get an arm, and the lamps on their own.</summary>
        internal static void Plan(Ctx ctx)
        {
            ctx.Lamps.Clear();
            ctx.Lights = 0;
            // Twelve real lights to a streamed piece's worth of ground (the runtime lights only the nearest anyway).
            ctx.MaxLights = Mathf.Min(80, Mathf.RoundToInt(12f * ctx.Half * ctx.Half / (64f * 64f)));
            var mapped = new HashSet<Street>();
            Mapped(ctx, mapped);
            OnPoles(ctx, mapped);
            Downtown(ctx, mapped);
            MainRoads(ctx, mapped);
            Farms(ctx);
        }

        // The mapped street lamps, each by the road it lights (streets with two or more mapped get no others).
        private static void Mapped(Ctx ctx, HashSet<Street> mapped)
        {
            var count = new Dictionary<Street, int>();
            foreach (Node n in ctx.Site.Area.Nodes)
            {
                if (n.Data.kind != "street_lamp" || !ctx.Inside(n.At)) continue;
                Street s = NearestRoad(ctx.Site, n.At, 14f, out Vector2 on, out _);
                Vector2 reach = s != null && (on - n.At).sqrMagnitude > 0.01f ? (on - n.At).normalized : Vector2.up;
                if (s != null) count[s] = count.TryGetValue(s, out int k) ? k + 1 : 1;
                if (!ctx.Site.FreeBeside(n.At, 0.35f)) continue;
                LampKind kind = ctx.Density.Downtown(n.At) ? LampKind.Acorn : s != null && RoadRank(s) >= 4 ? LampKind.Davit : LampKind.Wood;
                Add(ctx, kind, n.At, reach, MapStyle.Hash(n.Data.id, SaltLit), kind == LampKind.Acorn ? 0.55f : 0.4f);
            }
            foreach (KeyValuePair<Street, int> kv in count)
                if (kv.Value >= 2) mapped.Add(kv.Key);
        }

        // An arm and a cobra head on some of the utility poles along the roads: the pole nearest each junction, and in town
        // every third pole or so.
        private static void OnPoles(Ctx ctx, HashSet<Street> mapped)
        {
            var nearestTo = new Dictionary<Crossing, Pole>();
            var nearestD = new Dictionary<Crossing, float>();
            foreach (Pole p in ctx.Poles)
            {
                if (p.Street == null || p.Edge || p.Height > 15f || mapped.Contains(p.Street) || p.Street.Data.lit == 2) continue;
                Crossing c = ctx.NearestCrossing(p.At, 24f);
                if (c == null || c.TopRank < 2) continue;
                float d = (c.At - p.At).sqrMagnitude;
                if (nearestD.TryGetValue(c, out float best) && best <= d) continue;
                nearestD[c] = d;
                nearestTo[c] = p;
            }
            var corner = new HashSet<Pole>(nearestTo.Values);
            foreach (Pole p in ctx.Poles)
            {
                if (p.Street == null || p.Edge || p.Height > 15f || mapped.Contains(p.Street) || p.Street.Data.lit == 2) continue;
                bool town = ctx.Density.Town(p.At);
                if (ctx.Density.Downtown(p.At) && !corner.Contains(p)) continue;
                bool want;
                if (corner.Contains(p)) want = town || MapStyle.Unit(p.Key, SaltPoleLamp) < 0.55f;
                else want = town && RoadRank(p.Street) <= 3 && MapStyle.Unit(p.Key, SaltPoleLamp) < 0.3f;
                if (!want) continue;
                p.Lamp = Lamp(ctx, LampKind.CobraOnPole, p.At, p.ToRoad, p.Key, town ? 0.3f : 0.22f);
            }
        }

        // Black post-tops with acorn globes along both sidewalks downtown, staggered from side to side.
        private static void Downtown(Ctx ctx, HashSet<Street> mapped)
        {
            foreach (Street s in ctx.Site.Area.Streets)
            {
                if (s.Data.tunnel || s.Data.bridge || RoadRank(s) < 2 || RoadRank(s) > 5 || mapped.Contains(s) || s.Data.lit == 2) continue;
                float off = HalfWidth(s) + 0.6f;
                foreach ((Vector2 p, Vector2 d, long k) in Lattice(ctx, s.Pts, AcornSpacing * 0.5f))
                {
                    if (!ctx.Inside(p) || !ctx.Density.Downtown(p)) continue;
                    float side = (k & 1) == 0 ? 1f : -1f;
                    Vector2 at = p + Left(d) * (off * side);
                    if (!ctx.Site.FreeBeside(at, 0.5f) || NearCrossingMiddle(ctx, at)) continue;
                    Add(ctx, LampKind.Acorn, at, -Left(d) * side, ctx.KeyOf(at), 0.55f);
                }
            }
        }

        // Galvanised davit poles along the main roads through town (and the streets mapped as lit), opposite the wires.
        private static void MainRoads(Ctx ctx, HashSet<Street> mapped)
        {
            foreach (Street s in ctx.Site.Area.Streets)
            {
                if (s.Data.tunnel || s.Data.bridge || mapped.Contains(s) || s.Data.lit == 2) continue;
                int rank = RoadRank(s);
                bool main = rank >= 4 && s.Data.kind != "motorway";
                if (!main && !(s.Data.lit == 1 && rank >= 2)) continue;
                // The far side from the utility line (which keeps to one side of each piece by its road's name).
                float side = MapStyle.Unit(NameId(s), 0x5E1D) < 0.5f ? -1f : 1f;
                float off = HalfWidth(s) + Verge(s) + 0.2f;
                foreach ((Vector2 p, Vector2 d, long _) in Lattice(ctx, s.Pts, DavitSpacing))
                {
                    if (!ctx.Inside(p) || ctx.Density.Downtown(p) || (!ctx.Density.Town(p) && s.Data.lit != 1)) continue;
                    Vector2 at = p + Left(d) * (off * side);
                    if (!ctx.Site.FreeBeside(at, 0.5f) || NearCrossingMiddle(ctx, at)) continue;
                    Add(ctx, LampKind.Davit, at, -Left(d) * side, ctx.KeyOf(at), 0.35f);
                }
            }
        }

        // A farmyard's dusk-to-dawn light: one pole between a barn and the house nearest it.
        private static void Farms(Ctx ctx)
        {
            var lights = new List<Vector2>();
            foreach (Footprint f in ctx.Site.Area.Buildings)
            {
                string k = f.Data.kind;
                if (k != "barn" && k != "silo" && k != "farm_auxiliary" || !ctx.Inside(f.Middle)) continue;
                Footprint home = null;
                float best = 80f * 80f;
                foreach (Footprint h in ctx.Site.Area.Buildings)
                {
                    if (h.Data.kind != "house" && h.Data.kind != "trailer") continue;
                    float d = (h.Middle - f.Middle).sqrMagnitude;
                    if (d < best) { best = d; home = h; }
                }
                if (home == null) continue;
                bool near = false;
                foreach (Vector2 l in lights) if ((l - f.Middle).sqrMagnitude < 70f * 70f) near = true;
                if (near) continue;
                Vector2 toHome = (home.Middle - f.Middle).normalized;
                Vector2 at = f.Middle + toHome * (Mathf.Max(f.Bounds.width, f.Bounds.height) * 0.5f + 4f);
                if (!ctx.Site.Free(at, 0.5f)) continue;
                lights.Add(f.Middle);
                ctx.Site.Take(at, 0.5f);
                Add(ctx, LampKind.Farm, at, -toHome, MapStyle.Hash(f.Data.id, SaltFarm), 0.45f);
            }
        }

        private static void Add(Ctx ctx, LampKind kind, Vector2 at, Vector2 reach, long key, float litChance)
        {
            LampPlan lamp = Lamp(ctx, kind, at, reach, key, litChance);
            lamp.Height = kind == LampKind.Acorn ? 3.7f : kind == LampKind.Davit ? 9.2f : kind == LampKind.Farm ? 7.2f : 8.4f;
            ctx.Lamps.Add(lamp);
            if (ctx.Inside(at)) ctx.Site.Take(at, 0.45f);
        }

        private static LampPlan Lamp(Ctx ctx, LampKind kind, Vector2 at, Vector2 reach, long key, float litChance)
        {
            bool lit = ctx.Inside(at) && MapStyle.Unit(key, SaltLit) < litChance;
            return new LampPlan
            {
                Kind = kind, At = at, Reach = reach.sqrMagnitude > 1e-4f ? reach.normalized : Vector2.up, Key = key, Lit = lit,
                Flicker = lit && MapStyle.Unit(key, SaltFlicker) < 0.16f,
                Mercury = kind == LampKind.Farm || (kind != LampKind.Acorn && kind != LampKind.Davit && MapStyle.Unit(key, SaltMercury) < 0.5f),
            };
        }

        // Too near a crossing's middle for a post (the corners' kerb returns).
        private static bool NearCrossingMiddle(Ctx ctx, Vector2 at)
        {
            Crossing c = ctx.NearestCrossing(at, 9f);
            if (c == null) return false;
            float w = 0f;
            foreach (JunctionArm a in c.Arms) w = Mathf.Max(w, a.HalfWidth);
            return (c.At - at).sqrMagnitude < (w + 2.5f) * (w + 2.5f);
        }

        /// <summary>Points along a polyline every <paramref name="spacing"/> metres of the world (the same points whichever
        /// piece of the street a site holds), with the segment's direction and the point's index along the world's
        /// lattice.</summary>
        internal static IEnumerable<(Vector2 p, Vector2 d, long k)> Lattice(Ctx ctx, List<Vector2> pts, float spacing)
        {
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.5f) continue;
                Vector2 d = (b - a) / len;
                // The segment's own axis, measured from the world's origin: its multiples of the spacing.
                float along0 = Vector2.Dot(a + ctx.Origin, d);
                long k0 = (long)Mathf.Ceil(along0 / spacing);
                for (long k = k0; ; k++)
                {
                    float t = k * spacing - along0;
                    if (t > len) break;
                    if (t < 0f) continue;
                    yield return (a + d * t, d, k);
                }
            }
        }

        /// <summary>Draws the lamps standing on their own.</summary>
        internal static void Draw(Ctx ctx)
        {
            foreach (LampPlan lamp in ctx.Lamps) Draw(ctx, lamp, lamp.Height);
            ctx.Lamps.Clear();
        }

        private static void Draw(Ctx ctx, LampPlan lamp, float h)
        {
            Vector2 at = lamp.At;
            Vector3 foot = To3(at, 0f);
            bool inside = ctx.Inside(at);
            RaidMesh low = ctx.Low(at), high = ctx.High(at);
            Vector3 head;
            switch (lamp.Kind)
            {
                case LampKind.Acorn:
                {
                    Surf black = Gloss(AcornBlack);
                    low.Cylinder(foot, 0.2f, 0.55f, 8, black, Quaternion.identity, 0.6f);
                    low.Cylinder(foot + new Vector3(0f, 0.55f, 0f), 0.075f, h - 0.85f, 8, black, Quaternion.identity, 0.85f);
                    low.Cylinder(foot + new Vector3(0f, h - 0.32f, 0f), 0.13f, 0.12f, 8, black);
                    head = foot + new Vector3(0f, h + 0.06f, 0f);
                    if (!lamp.Lit) high.Ellipsoid(head, new Vector3(0.40f, 0.54f, 0.40f), Gloss(GlobeMilk), 10, 6);
                    high.Cylinder(head + new Vector3(0f, 0.25f, 0f), 0.16f, 0.1f, 8, black, Quaternion.identity, 0.5f);
                    high.Cylinder(head + new Vector3(0f, 0.35f, 0f), 0.03f, 0.16f, 6, black, Quaternion.identity, 0.4f);
                    high.Cylinder(head + new Vector3(0f, 0.3f, 0f), 0.11f, 0.04f, 8, SnowSurf);
                    if (lamp.Lit && inside) Attach(ctx, lamp, at, h, head);
                    else ctx.Collider(at, 0f, new Vector3(0.3f, 3f, 0.3f), 0f);
                    break;
                }
                case LampKind.Davit:
                {
                    bool bronze = MapStyle.Unit(lamp.Key, SaltBronze) < 0.3f;
                    Surf steel = bronze ? Gloss(new Color(0.24f, 0.21f, 0.17f)) : Galvanised;
                    low.Cylinder(foot, 0.32f, 0.18f, 8, Concrete);
                    low.Box(foot + new Vector3(0f, 0.22f, 0f), new Vector3(0.36f, 0.08f, 0.36f), steel);
                    low.Cylinder(foot + new Vector3(0f, 0.18f, 0f), 0.11f, h - 0.6f, 8, steel, Quaternion.identity, 0.6f);
                    // The davit: up and out over the road in an easy curve.
                    Vector3 r = To3(lamp.Reach, 0f);
                    Vector3 p0 = foot + new Vector3(0f, h - 0.5f, 0f), p1 = foot + r * 0.45f + new Vector3(0f, h, 0f);
                    Vector3 p2 = foot + r * 1.3f + new Vector3(0f, h + 0.2f, 0f), p3 = foot + r * 2.4f + new Vector3(0f, h + 0.12f, 0f);
                    high.Rod(p0, p1, 0.09f, steel, 6);
                    high.Rod(p1, p2, 0.08f, steel, 6);
                    high.Rod(p2, p3, 0.07f, steel, 6);
                    head = p3;
                    PowerPass.CobraHead(ctx, lamp, head, lamp.Reach, 0.34f);
                    if (lamp.Lit && inside) Attach(ctx, lamp, at, h, head);
                    else ctx.Collider(at, 0f, new Vector3(0.3f, 4f, 0.3f), 0f);
                    break;
                }
                default:
                {
                    // A wooden pole of its own (a mapped lamp, a farmyard's light): an arm and a cobra head, or a reflector.
                    Surf wood = Matte(PoleWoods[MapStyle.Pick(lamp.Key, 0x3D21, PoleWoods.Length)]);
                    low.Cylinder(foot, 0.13f, h, 7, wood, Quaternion.identity, 0.75f);
                    Vector3 r = To3(lamp.Reach, 0f);
                    float arm = lamp.Kind == LampKind.Farm ? 1.0f : 1.8f;
                    Vector3 root = foot + r * 0.13f + new Vector3(0f, h - 0.5f, 0f), tip = foot + r * arm + new Vector3(0f, h - 0.25f, 0f);
                    high.Rod(root, tip, 0.045f, Galvanised);
                    head = tip;
                    if (lamp.Kind == LampKind.Farm)
                    {
                        // The farm light's reflector: a shallow shade over a round lens.
                        high.Cylinder(tip + new Vector3(0f, -0.12f, 0f), 0.3f, 0.16f, 10, Gloss(new Color(0.20f, 0.28f, 0.24f)), Quaternion.identity, 0.35f);
                        high.Cylinder(tip + new Vector3(0f, 0.02f, 0f), 0.11f, 0.03f, 8, SnowSurf);
                        if (!lamp.Lit) high.Ellipsoid(tip + new Vector3(0f, -0.15f, 0f), new Vector3(0.26f, 0.12f, 0.26f), Gloss(GlobeMilk), 8, 4);
                    }
                    else PowerPass.CobraHead(ctx, lamp, head, lamp.Reach, 0.3f);
                    if (lamp.Lit && inside) Attach(ctx, lamp, at, h, head);
                    else ctx.Collider(at, 0f, new Vector3(0.28f, 4f, 0.28f), 0f);
                    break;
                }
            }
        }

        /// <summary>Makes a burning lamp a <see cref="StreetLamp"/>: its lens glowing under the head at
        /// <paramref name="head"/> (where the arm ends, or the globe's middle) and, while the site has lights to spare, a real
        /// spot light.</summary>
        internal static void Attach(Ctx ctx, LampPlan lamp, Vector2 foot, float poleHeight, Vector3 head)
        {
            Color colour = lamp.Kind == LampKind.Acorn ? Incandescent : lamp.Mercury ? Mercury : Sodium;
            bool light = ctx.Lights < ctx.MaxLights;
            if (light) ctx.Lights++;
            float yaw = Yaw(lamp.Reach);
            Vector3 lens;
            StreetLamp.Lens shape;
            float size, range, intensity, angle;
            switch (lamp.Kind)
            {
                case LampKind.Acorn:
                    lens = head;
                    shape = StreetLamp.Lens.Globe;
                    size = 0.42f;
                    range = 14f;
                    intensity = 6f;
                    angle = 165f;
                    break;
                case LampKind.Farm:
                    lens = head + new Vector3(0f, -0.16f, 0f);
                    shape = StreetLamp.Lens.Dome;
                    size = 0.27f;
                    range = 19f;
                    intensity = 9f;
                    angle = 140f;
                    break;
                default:
                {
                    float s = lamp.Kind == LampKind.Davit ? 0.34f : 0.3f;
                    lens = head + To3(lamp.Reach, 0f) * (s * 2.1f * 0.48f) + new Vector3(0f, -s * 0.2f, 0f);
                    shape = StreetLamp.Lens.Cobra;
                    size = s;
                    range = 24f;
                    intensity = lamp.Kind == LampKind.Davit ? 16f : 13f;
                    angle = 135f;
                    break;
                }
            }
            var pole = new Vector3(lamp.Kind == LampKind.Acorn ? 0.3f : 0.32f, Mathf.Min(poleHeight, 4f), lamp.Kind == LampKind.Acorn ? 0.3f : 0.32f);
            StreetLamp.Attach(ctx.Site.Town, To3(foot, 0f), pole, lens, yaw, shape, size, colour, range, intensity, angle, light, lamp.Flicker);
        }
    }
}
