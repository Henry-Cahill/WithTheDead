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
    /// gets no others). Each kind keeps its distance from the lamps already planned, so no corner gets a cluster. Some
    /// still burn (more downtown, fewer out of town) and those become <see cref="StreetLamp"/>s, which in play borrow the
    /// runtime's few lights only while they're among the nearest to the survivor; an editor build gives the burning lamps
    /// nearest the site's middle lights of their own, up to a small cap, for previews and the map harness.
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

        /// <summary>The burning lamps of an editor build that get a light of their own: six to a streamed piece's worth of
        /// ground, at most two dozen (in play the runtime's pool lights the nearest instead).</summary>
        internal static int EditorLights(Ctx ctx) => Mathf.Clamp(Mathf.RoundToInt(6f * ctx.Half * ctx.Half / (64f * 64f)), 2, 24);

        /// <summary>Chooses every lamp: mapped ones, the poles that get an arm, and the lamps on their own.</summary>
        internal static void Plan(Ctx ctx)
        {
            ctx.Lamps.Clear();
            ctx.AllLamps.Clear();
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
                Street s = NearestRoad(ctx, n.At, 14f, out Vector2 on, out _);
                Vector2 reach = s != null && (on - n.At).sqrMagnitude > 0.01f ? (on - n.At).normalized : Vector2.up;
                if (s != null) count[s] = count.TryGetValue(s, out int k) ? k + 1 : 1;
                if (!ctx.Site.FreeBeside(n.At, 0.35f) || Near(ctx, n.At, 3f)) continue;
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
                if (!Lampable(ctx, p, mapped)) continue;
                Crossing c = ctx.NearestCrossing(p.At, 24f);
                if (c == null || c.TopRank < 2) continue;
                float d = (c.At - p.At).sqrMagnitude;
                if (nearestD.TryGetValue(c, out float best) && best <= d) continue;
                nearestD[c] = d;
                nearestTo[c] = p;
            }
            var corner = new HashSet<Pole>(nearestTo.Values);
            // The corners' first, then the poles between them.
            for (int pass = 0; pass < 2; pass++)
                foreach (Pole p in ctx.Poles)
                {
                    if (corner.Contains(p) != (pass == 0) || !Lampable(ctx, p, mapped)) continue;
                    bool town = ctx.Density.Town(p.At);
                    bool want;
                    if (pass == 0) want = town || MapStyle.Unit(p.Key, SaltPoleLamp) < 0.55f;
                    else want = town && !ctx.Density.Downtown(p.At) && RoadRank(p.Street) <= 3 && MapStyle.Unit(p.Key, SaltPoleLamp) < 0.3f;
                    if (!want || Near(ctx, p.At, 14f)) continue;
                    p.Lamp = Lamp(ctx, LampKind.CobraOnPole, p.At, p.ToRoad, p.Key, town ? 0.3f : 0.22f);
                    ctx.AllLamps.Add(p.Lamp);
                }
        }

        // A pole of a line along a road, inside the square, not on its edge, on a street that may have lamps.
        private static bool Lampable(Ctx ctx, Pole p, HashSet<Street> mapped)
            => p.Street != null && !p.Edge && p.Height < 15f && ctx.Inside(p.At) && !mapped.Contains(p.Street) && p.Street.Data.lit != 2;

        // Black post-tops with acorn globes along both sidewalks downtown, staggered from side to side.
        private static void Downtown(Ctx ctx, HashSet<Street> mapped)
        {
            foreach (Street s in ctx.Site.Area.Streets)
            {
                if (s.Data.tunnel || s.Data.bridge || RoadRank(s) < 2 || RoadRank(s) > 5 || mapped.Contains(s) || s.Data.lit == 2) continue;
                foreach ((Vector2 p, Vector2 d, long k) in Lattice(ctx, s.Pts, AcornSpacing * 0.5f))
                {
                    if (!ctx.Inside(p) || !ctx.Density.Downtown(p)) continue;
                    int side = (k & 1) == 0 ? 1 : -1;
                    Vector2 at = p + Left(d) * (side * Roadside(s, side));
                    if (!ctx.Site.FreeBeside(at, 0.5f) || NearCrossingMiddle(ctx, at) || Near(ctx, at, 11f)) continue;
                    Add(ctx, LampKind.Acorn, at, -Left(d) * side, ctx.KeyOf(at), 0.7f);
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
                int side = MapStyle.Unit(NameId(s), 0x5E1D) < 0.5f ? -1 : 1;
                float off = Roadside(s, side) + 0.2f;
                foreach ((Vector2 p, Vector2 d, long _) in Lattice(ctx, s.Pts, DavitSpacing))
                {
                    if (!ctx.Inside(p) || ctx.Density.Downtown(p) || (!ctx.Density.Town(p) && s.Data.lit != 1)) continue;
                    Vector2 at = p + Left(d) * (off * side);
                    if (!ctx.Site.FreeBeside(at, 0.5f) || NearCrossingMiddle(ctx, at) || Near(ctx, at, 24f)) continue;
                    Add(ctx, LampKind.Davit, at, -Left(d) * side, ctx.KeyOf(at), 0.35f);
                }
            }
        }

        // A farmyard's dusk-to-dawn light: one pole between a barn and the house nearest it.
        // Iterate over all farm‑related buildings to place a single pole between each barn/silo and its nearest house.
        private static void Farms(Ctx ctx)
        {
            // Collect positions of already placed farm lights to avoid clustering.
            var lights = new List<Vector2>();
            foreach (Footprint f in ctx.Site.Area.Buildings)
            {
                string k = f.Data.kind;
                // Skip structures that aren't farms or are outside the current map area.
                if (k != "barn" && k != "silo" && k != "farm_auxiliary" || !ctx.Inside(f.Middle)) continue;
                Footprint home = null;
                float best = 80f * 80f;
                // Find the closest house or trailer to anchor the farm light pole.
                foreach (Footprint h in ctx.Site.Area.Buildings)
                {
                    if (h.Data.kind != "house" && h.Data.kind != "trailer") continue;
                    float d = (h.Middle - f.Middle).sqrMagnitude;
                    if (d < best) { best = d; home = h; }
                }
                if (home == null) continue;
                bool near = false;
                // Avoid placing another farm light if one is already nearby (within 70 m).
                foreach (Vector2 l in lights) if ((l - f.Middle).sqrMagnitude < 70f * 70f) near = true;
                if (near) continue;
                Vector2 toHome = (home.Middle - f.Middle).normalized;
                // Position the pole halfway out from the farm building toward the house, offset by half the building size plus a margin.
                Vector2 at = f.Middle + toHome * (Mathf.Max(f.Bounds.width, f.Bounds.height) * 0.5f + 4f);
                // Reserve a small area for the pole so other geometry won't intersect it.
                if (!ctx.Site.Free(at, 0.5f)) continue;
                // Record the placement and create the lamp geometry.
                lights.Add(f.Middle);
                ctx.Site.Take(at, 0.5f);
                Add(ctx, LampKind.Farm, at, -toHome, MapStyle.Hash(f.Data.id, SaltFarm), 0.45f);
            }
        }

        // A lamp planned within this of the spot already.
        private static bool Near(Ctx ctx, Vector2 at, float within)
        {
            float r2 = within * within;
            foreach (LampPlan l in ctx.AllLamps)
                if ((l.At - at).sqrMagnitude < r2) return true;
            return false;
        }

        /// <summary>Creates a LampPlan and registers it for later processing.</summary>
        private static void Add(Ctx ctx, LampKind kind, Vector2 at, Vector2 reach, long key, float litChance)
        {
            LampPlan lamp = Lamp(ctx, kind, at, reach, key, litChance);
            lamp.Height = kind == LampKind.Acorn ? 3.7f : kind == LampKind.Davit ? 9.2f : kind == LampKind.Farm ? 7.2f : 8.4f;
            ctx.Lamps.Add(lamp);
            ctx.AllLamps.Add(lamp);
            if (ctx.Inside(at)) ctx.Site.Take(at, 0.45f);
        }

        /// <summary>Creates a LampPlan with lighting parameters and determines if it should be lit.</summary>
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
            Crossing c = ctx.NearestCrossing(at, 14f);
            if (c == null) return false;
            float w = 0f;
            for (int k = 0; k < c.Arms.Count; k++) w = Mathf.Max(w, c.Setback(k));
            return (c.At - at).sqrMagnitude < (w + 1.2f) * (w + 1.2f);
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

        /// <summary>Draws the lamps standing on their own, then makes every burning lamp (on the poles too) a
        /// <see cref="StreetLamp"/>.</summary>
        internal static IEnumerable<string> Draw(Ctx ctx)
        {
            foreach (LampPlan lamp in ctx.Lamps)
            {
                Draw(ctx, lamp, lamp.Height);
                if (ctx.Site.StepDue) yield return "street lights";
            }
            ctx.Lamps.Clear();
            foreach (string step in AttachAll(ctx)) yield return step;
        }

        // Render a single lamp according to its type and lighting state.
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
                    if (lamp.Lit && inside) Burn(ctx, lamp, at, h, head);
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
                    if (lamp.Lit && inside) Burn(ctx, lamp, at, h, head);
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
                    if (lamp.Lit && inside) Burn(ctx, lamp, at, h, head);
                    else ctx.Collider(at, 0f, new Vector3(0.28f, 4f, 0.28f), 0f);
                    break;
                }
            }
        }

        private static void Burn(Ctx ctx, LampPlan lamp, Vector2 foot, float poleHeight, Vector3 head)
            => ctx.Burning.Add(new Burning { Lamp = lamp, Foot = foot, PoleHeight = poleHeight, Head = head });

        // Every burning lamp made a StreetLamp. In an editor build the first few get lights of their own, spread over the
        // site: the lamp nearest each 64 m cell's middle first, then each cell's next, nearest the site's middle first.
        // Order and attach burning lamps, assigning lights to a limited subset for the editor.
        private static IEnumerable<string> AttachAll(Ctx ctx)
        {
            List<Burning> burning = ctx.Burning;
            var order = new List<(int rank, float d, long key, int i)>(burning.Count);
            // Group burning lamps by map cell to spread lights evenly across the site.
            var perCell = new Dictionary<long, List<int>>();
            for (int i = 0; i < burning.Count; i++)
            {
                Vector2 f = burning[i].Foot;
                long cell = ((long)Mathf.FloorToInt(f.x / 64f) << 32) ^ (uint)Mathf.FloorToInt(f.y / 64f);
                if (!perCell.TryGetValue(cell, out List<int> list)) perCell[cell] = list = new List<int>(4);
                list.Add(i);
            }
            foreach (KeyValuePair<long, List<int>> kv in perCell)
            {
                List<int> list = kv.Value;
                var middle = new Vector2((Mathf.Floor(burning[list[0]].Foot.x / 64f) + 0.5f) * 64f, (Mathf.Floor(burning[list[0]].Foot.y / 64f) + 0.5f) * 64f);
                // Sort lamps in each cell by distance to the cell centre to prioritize central ones.
                list.Sort((a, b) =>
                {
                    int c = (burning[a].Foot - middle).sqrMagnitude.CompareTo((burning[b].Foot - middle).sqrMagnitude);
                    return c != 0 ? c : burning[a].Lamp.Key.CompareTo(burning[b].Lamp.Key);
                });
                for (int r = 0; r < list.Count; r++)
                    // Combine per‑cell ordering into a global order, ranking by cell rank then distance.
                    order.Add((r, burning[list[r]].Foot.sqrMagnitude, burning[list[r]].Lamp.Key, list[r]));
            }
            order.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : a.d != b.d ? a.d.CompareTo(b.d) : a.key.CompareTo(b.key));
            // Determine how many lamps should get actual light objects in the editor build.
            int lights = EditorLights(ctx);
            for (int k = 0; k < order.Count; k++)
            {
                Attach(ctx, burning[order[k].i], k < lights);
                if (ctx.Site.StepDue) yield return "street lights";
            }
            burning.Clear();
        }

        /// <summary>Makes a burning lamp a <see cref="StreetLamp"/>: its lens glowing under the head (where the arm ends, or
        /// the globe's middle) and the spot light it shines with.</summary>
        // Create a StreetLamp from a burning lamp, optionally adding a light source for editor preview.
        private static void Attach(Ctx ctx, Burning b, bool light)
        {
            LampPlan lamp = b.Lamp;
            Vector3 head = b.Head;
            Color colour = lamp.Kind == LampKind.Acorn ? Incandescent : lamp.Mercury ? Mercury : Sodium;
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
            var pole = new Vector3(lamp.Kind == LampKind.Acorn ? 0.3f : 0.32f, Mathf.Min(b.PoleHeight, 4f), lamp.Kind == LampKind.Acorn ? 0.3f : 0.32f);
            StreetLamp.Attach(ctx.Site.Town, To3(b.Foot, 0f), pole, lens, yaw, shape, size, colour, range, intensity, angle, light, lamp.Flicker);
        }
    }
}
