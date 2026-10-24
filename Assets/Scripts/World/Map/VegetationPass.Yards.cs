using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The town's trees: every home's yard (a shade tree out front, an ornamental or an evergreen at a corner, more out
    // back, a farmstead's windbreak) with bushes along its front wall, and the big trees lining the residential streets.
    internal static partial class VegetationPass
    {
        private const int SaltYard = 0x7A0D, SaltFoundation = 0x7A1F, SaltWindbreak = 0x7A2B, SaltStreet = 0x7A3C;

        /// <summary>A yard tree's place round its house, in the house's frame (see <see cref="NaturePlan.HouseFrame"/>).</summary>
        private enum Slot { FrontShade, FrontCorner, Back, BackCorner, Side, BackFar, FrontFar }

        private static readonly Mix ShadeMix = new Mix((TreeKind.Maple, 0.46f), (TreeKind.Oak, 0.34f), (TreeKind.Sycamore, 0.06f),
            (TreeKind.Hackberry, 0.08f), (TreeKind.Tulip, 0.06f));
        private static readonly Mix CornerMix = new Mix((TreeKind.Pear, 0.32f), (TreeKind.Dogwood, 0.22f), (TreeKind.Spruce, 0.28f),
            (TreeKind.Cedar, 0.10f), (TreeKind.WhitePine, 0.08f));
        private static readonly Mix BackMix = new Mix((TreeKind.Oak, 0.30f), (TreeKind.Maple, 0.30f), (TreeKind.WhitePine, 0.16f),
            (TreeKind.Hackberry, 0.14f), (TreeKind.Tulip, 0.05f), (TreeKind.Pear, 0.05f));
        private static readonly Mix EvergreenMix = new Mix((TreeKind.Spruce, 0.34f), (TreeKind.WhitePine, 0.34f), (TreeKind.Cedar, 0.22f),
            (TreeKind.Maple, 0.10f));
        private static readonly Mix FarmMix = new Mix((TreeKind.Oak, 0.36f), (TreeKind.Maple, 0.22f), (TreeKind.Hackberry, 0.16f),
            (TreeKind.Cedar, 0.14f), (TreeKind.Sycamore, 0.06f), (TreeKind.WhitePine, 0.06f));

        private static IEnumerable<string> Yards(Planting p)
        {
            int done = 0;
            foreach (Footprint f in p.Site.Area.Buildings)
            {
                if (!NaturePlan.IsHome(f.Data) || f.Pts.Count < 3 || f.Size < 20f) continue;
                bool inside = p.Plan.InSquare(f.Middle, 0f);
                if (!inside && !p.Plan.InRing(f.Middle) && !p.Site.Chunked) continue;
                Yard(p, f, inside || p.Site.Chunked);
                if (++done % 20 == 0) yield return "yards";
            }
            // A streamed piece also grows the trees its neighbours' houses have in it (each piece plants only inside its
            // own square, so no tree is grown twice).
            foreach (Footprint f in p.Site.Area.Foreign)
            {
                if (!NaturePlan.IsHome(f.Data) || f.Pts.Count < 3 || f.Size < 20f) continue;
                Yard(p, f, true);
                if (++done % 20 == 0) yield return "yards";
            }
            yield return "yards";
        }

        // A home's trees: fewer the closer the houses stand, more out in the country, where a farmhouse also gets a
        // windbreak of cedars and pines behind it; then the bushes along its front.
        private static void Yard(Planting p, Footprint f, bool inside)
        {
            NaturePlan plan = p.Plan;
            NaturePlan.HouseFrame hf = plan.FrameOf(f);
            long id = f.Data.id;
            float urban = plan.Urban(f.Middle);
            bool rural = urban < 0.035f;
            int want = rural ? 3 + MapStyle.Pick(id, SaltYard, 4) : urban < 0.25f ? 1 + MapStyle.Pick(id, SaltYard, 3) : MapStyle.Pick(id, SaltYard, 3);
            if (!inside) want = Mathf.Min(want, 2);
            float side = MapStyle.Unit(id, SaltYard + 1) < 0.5f ? 1f : -1f;
            int planted = 0;
            for (int s = 0; s <= (int)Slot.FrontFar && planted < want; s++)
            {
                var slot = (Slot)s;
                if ((slot == Slot.BackFar || slot == Slot.FrontFar) && !rural) continue;
                long key = NaturePlan.Combine(id, 40 + s);
                if (MapStyle.Unit(key, SaltYard + 2) < 0.15f) continue;   // not every yard has every tree
                if (YardTree(p, f, hf, slot, side, key, inside)) planted++;
            }
            if (inside && !p.Lite) Foundation(p, f, hf);
            if (rural && MapStyle.Unit(id, SaltWindbreak) < 0.4f) Windbreak(p, f, hf);
        }

        private static bool YardTree(Planting p, Footprint f, NaturePlan.HouseFrame hf, Slot slot, float side, long key, bool inside)
        {
            float u(int k) => MapStyle.Unit(key, SaltYard + 10 + k);
            float t, n, wall, room;
            Mix mix;
            switch (slot)
            {
                case Slot.FrontShade:
                    t = Mathf.Lerp(hf.T0, hf.T1, side > 0f ? 0.2f + u(0) * 0.15f : 0.65f + u(0) * 0.15f);
                    n = hf.N0 - 4.5f - u(1) * 4f;
                    mix = ShadeMix; wall = 3.4f; room = 2.0f;
                    break;
                case Slot.FrontCorner:
                    t = side > 0f ? hf.T1 + 1.6f + u(0) * 1.6f : hf.T0 - 1.6f - u(0) * 1.6f;
                    n = hf.N0 - 0.6f - u(1) * 2.8f;
                    mix = CornerMix; wall = 1.8f; room = 1.1f;
                    break;
                case Slot.Back:
                    t = Mathf.Lerp(hf.T0, hf.T1, 0.2f + u(0) * 0.6f);
                    n = hf.N1 + 4.5f + u(1) * 5f;
                    mix = BackMix; wall = 3.0f; room = 1.8f;
                    break;
                case Slot.BackCorner:
                    t = side > 0f ? hf.T0 - 2f - u(0) * 2f : hf.T1 + 2f + u(0) * 2f;
                    n = hf.N1 + 0.5f + u(1) * 3.5f;
                    mix = EvergreenMix; wall = 2.2f; room = 1.2f;
                    break;
                case Slot.Side:
                    t = side > 0f ? hf.T0 - 2.6f - u(0) * 2f : hf.T1 + 2.6f + u(0) * 2f;
                    n = Mathf.Lerp(hf.N0, hf.N1, 0.3f + u(1) * 0.6f);
                    mix = BackMix; wall = 2.4f; room = 1.3f;
                    break;
                case Slot.BackFar:
                    t = Mathf.Lerp(hf.T0, hf.T1, u(0));
                    n = hf.N1 + 10f + u(1) * 8f;
                    mix = FarmMix; wall = 4f; room = 2.0f;
                    break;
                default:
                    t = side > 0f ? hf.T0 - 3f - u(0) * 3f : hf.T1 + 3f + u(0) * 3f;
                    n = hf.N0 - 5f - u(1) * 5f;
                    mix = FarmMix; wall = 4f; room = 2.0f;
                    break;
            }
            Vector2 at = hf.At(t, n);
            if (inside && !YardClear(p, f, at, room, wall)) return false;
            TreeKind kind = mix.Pick(MapStyle.Unit(key, SaltKind));
            float scale = slot == Slot.FrontShade || slot == Slot.BackFar ? 1.05f : 1f;
            return Plant(p, at, kind, HeightOf(kind, key, scale), key, room, TreeTier.Full, false);
        }

        /// <summary>
        /// Room for a yard tree (or a windbreak's) inside the square: clear of every wall by <paramref name="wall"/>, off
        /// the street and its sidewalk, off the driveways, clear of the front porch and of the other yard trees; and, away
        /// from the house's own bounding box (which the buildings take whole in the occupancy grid), free in the grid.
        /// </summary>
        private static bool YardClear(Planting p, Footprint home, Vector2 at, float room, float wall)
        {
            NaturePlan plan = p.Plan;
            if (!plan.InSquare(at, room)) return false;
            if (plan.WallDistance(at, wall + 0.5f) < wall) return false;
            if (plan.RoadClearance(at, 4f) < 2.6f) return false;
            if (plan.IsKept(at, room)) return false;
            Land land = plan.LandAt(at);
            if (land == Land.Water || land == Land.Lot || land == Land.Pitch) return false;
            if (NearDoor(p, home, at, 3.2f) || !p.TreeFree(at, room)) return false;
            Rect box = home.Bounds;
            float m = room + 1.2f;
            bool byHouse = at.x > box.xMin - m && at.x < box.xMax + m && at.y > box.yMin - m && at.y < box.yMax + m;
            return byHouse || p.Site.FreeBeside(at, room);
        }

        private static bool NearDoor(Planting p, Footprint f, Vector2 at, float within)
        {
            if (!p.Doors.TryGetValue(f, out EnterableBuilding e)) return false;
            foreach (Doorway d in e.Doors)
                if (Vector2.Distance(d.At + d.Outward * 1.2f, at) < within) return true;
            return false;
        }

        // Evergreen bushes along the wall that faces the street, a metre or two apart, the porch and the door left clear (on
        // most homes, and on the grounds' buildings that get them).
        private static void Foundation(Planting p, Footprint f, NaturePlan.HouseFrame hf, bool always = false)
        {
            long id = f.Data.id;
            if (!always && MapStyle.Unit(id, SaltFoundation) > 0.62f) return;
            List<Vector2> pts = f.Pts;
            int front = -1;
            float best = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                float len = Vector2.Distance(a, b);
                if (len < 3f) continue;
                Vector2 d = (b - a) / len, outward = new Vector2(d.y, -d.x);
                float score = Vector2.Dot(outward, -hf.N) * len;
                if (score > best)
                {
                    best = score;
                    front = i;
                }
            }
            if (front < 0) return;
            Vector2 s0 = pts[front], s1 = pts[(front + 1) % pts.Count];
            float edge = Vector2.Distance(s0, s1);
            Vector2 dir = (s1 - s0) / edge, outside = new Vector2(dir.y, -dir.x);
            float u = MapStyle.Unit(id, SaltFoundation + 1);
            ShrubKind kind = u < 0.45f ? ShrubKind.Yew : u < 0.8f ? ShrubKind.Boxwood : ShrubKind.Juniper;
            float spacing = 1.4f + MapStyle.Unit(id, SaltFoundation + 2) * 0.9f;
            int k = 0;
            for (float s = 0.9f; s <= edge - 0.9f; s += spacing, k++)
            {
                Vector2 at = s0 + dir * s + outside * 0.75f;
                if (NearDoor(p, f, at, 2.6f) || p.Plan.IsKept(at, 0.6f)) continue;
                if (p.Plan.RoadClearance(at, 2f) < 1.2f || p.Plan.WallDistance(at, 1f) < 0.45f) continue;
                long key = NaturePlan.Combine(id, 200 + k);
                Bush(p, at, kind, 0.7f + MapStyle.Unit(key, SaltFoundation + 3) * 0.5f, key, false);
            }
        }

        // A farmstead's windbreak: a row of cedars, pines and spruces across the back of the house.
        private static void Windbreak(Planting p, Footprint f, NaturePlan.HouseFrame hf)
        {
            long id = f.Data.id;
            float n = hf.N1 + 9f + MapStyle.Unit(id, SaltWindbreak + 1) * 6f;
            float from = hf.T0 - 4f, to = hf.T1 + 4f;
            int k = 0;
            for (float t = from; t <= to; t += 3.6f, k++)
            {
                long key = NaturePlan.Combine(id, 300 + k);
                Vector2 at = hf.At(t, n + (MapStyle.Unit(key, SaltJitter) - 0.5f) * 1.2f);
                bool inside = p.Plan.InSquare(at, 0f);
                if (inside && !YardClear(p, f, at, 1.2f, 3f)) continue;
                TreeKind kind = MapStyle.Unit(key, SaltKind) < 0.55f ? TreeKind.Cedar : MapStyle.Unit(key, SaltKind + 1) < 0.6f ? TreeKind.WhitePine : TreeKind.Spruce;
                Plant(p, at, kind, HeightOf(kind, key), key, 1.2f, TreeTier.Full, false);
            }
        }

        // The big old trees along the town's residential streets, a dozen metres apart behind the sidewalk, wherever
        // homes front the street (shopping streets and downtown keep bare kerbs).
        private static void StreetTrees(Planting p)
        {
            NaturePlan plan = p.Plan;
            foreach (Street st in p.Site.Area.Streets)
            {
                if (st.Data.tunnel || st.Data.bridge || st.Rank < 2 || st.Rank > 4 || NaturePlan.FootKind(st.Data.kind)) continue;
                plan.Stations(st.Pts, 12f, p.Stations);
                foreach (NaturePlan.Station s in p.Stations)
                {
                    float urban = plan.Urban(s.At);
                    if (urban < 0.03f || urban > 0.3f || plan.Business(s.At) > 0.07f) continue;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        long key = NaturePlan.Combine(NaturePlan.Combine(st.Data.id, s.K), side + 7);
                        if (MapStyle.Unit(key, SaltStreet) > 0.55f) continue;
                        Vector2 normal = new Vector2(-s.Dir.y, s.Dir.x) * side;
                        Vector2 at = s.At + normal * (st.Width * 0.5f + 2.7f + MapStyle.Unit(key, SaltJitter) * 0.8f)
                            + s.Dir * ((MapStyle.Unit(key, SaltJitter + 1) - 0.5f) * 2f);
                        if (!HomesNear(p, at, 24f)) continue;
                        if (plan.WallDistance(at, 4f) < 3.4f || plan.IsKept(at, 1.6f) || plan.RoadClearance(at, 3f) < 2.3f) continue;
                        Land land = plan.LandAt(at);
                        if (land == Land.Lot || land == Land.Water || land == Land.Business) continue;
                        bool inside = plan.InSquare(at, 0f);
                        if (inside && (!p.Site.FreeBeside(at, 1.2f) || !p.TreeFree(at, 1.2f))) continue;
                        TreeKind kind = StreetMix.Pick(MapStyle.Unit(key, SaltKind));
                        Plant(p, at, kind, HeightOf(kind, key, 1.05f), key, 1.2f, TreeTier.Full, false);
                    }
                }
            }
        }

        private static readonly List<Footprint> s_Near = new List<Footprint>(16);

        private static bool HomesNear(Planting p, Vector2 at, float within)
        {
            p.Plan.BuildingsNear(at, within, s_Near);
            foreach (Footprint f in s_Near)
                if (NaturePlan.IsHome(f.Data)) return true;
            return false;
        }

        #region Grounds
        private const int SaltGrounds = 0x7A4D;

        // The lawns round the churches, schools, the courthouse, the town hall, the library and the like: big shade trees
        // and evergreens in front, beside and behind, where there's room off the street, and yews along the front.
        private static void Grounds(Planting p)
        {
            var list = new List<Footprint>();
            var seen = new HashSet<Footprint>();
            foreach (Footprint f in p.Site.Area.Buildings)
                if (CivicKind(f.Data.kind) && seen.Add(f)) list.Add(f);
            foreach (Spot s in p.Site.Area.Points)
            {
                if (!CivicUse(s.Data.kind)) continue;
                Footprint at = Housing(p, s.At);
                if (at != null && seen.Add(at)) list.Add(at);
            }
            foreach (Footprint f in list)
            {
                if (f.Pts.Count < 3 || f.Size < 60f || !p.Plan.InSquare(f.Middle, 0f)) continue;
                NaturePlan.HouseFrame hf = p.Plan.FrameOf(f);
                long id = f.Data.id;
                int want = 2 + MapStyle.Pick(id, SaltGrounds, 4), planted = 0;
                for (int k = 0; k < 10 && planted < want; k++)
                {
                    long key = NaturePlan.Combine(id, 700 + k);
                    float u = MapStyle.Unit(key, SaltGrounds + 1), v = MapStyle.Unit(key, SaltGrounds + 2);
                    Vector2 at;
                    switch (k % 5)
                    {
                        case 0: at = hf.At(Mathf.Lerp(hf.T0, hf.T1, 0.1f + u * 0.3f), hf.N0 - 5f - v * 6f); break;
                        case 1: at = hf.At(Mathf.Lerp(hf.T0, hf.T1, 0.6f + u * 0.3f), hf.N0 - 5f - v * 6f); break;
                        case 2: at = hf.At(hf.T0 - 4f - u * 4f, Mathf.Lerp(hf.N0, hf.N1, v)); break;
                        case 3: at = hf.At(hf.T1 + 4f + u * 4f, Mathf.Lerp(hf.N0, hf.N1, v)); break;
                        default: at = hf.At(Mathf.Lerp(hf.T0, hf.T1, u), hf.N1 + 4f + v * 6f); break;
                    }
                    if (!YardClear(p, f, at, 1.8f, 3.6f)) continue;
                    TreeKind kind = GroundsMix.Pick(MapStyle.Unit(key, SaltKind));
                    if (Plant(p, at, kind, HeightOf(kind, key, 1.1f), key, 1.8f, TreeTier.Full, false)) planted++;
                }
                if (!p.Lite && MapStyle.Unit(id, SaltGrounds + 3) < 0.7f) Foundation(p, f, hf, true);
            }
        }

        private static bool CivicKind(string kind) => kind == "church" || kind == "school" || kind == "civic" || kind == "hospital";

        private static bool CivicUse(string kind)
        {
            switch (kind)
            {
                case "courthouse": case "townhall": case "library": case "place_of_worship": case "school": case "police":
                case "post_office": case "fire_station": case "community_centre": case "college":
                    return true;
                default:
                    return false;
            }
        }

        // The footprint a point of interest stands in (or the nearest within 12 m).
        private static Footprint Housing(Planting p, Vector2 at)
        {
            p.Plan.BuildingsNear(at, 12f, s_Near);
            Footprint best = null;
            float bestD = 12f;
            foreach (Footprint f in s_Near)
            {
                if (MapGeometry.Contains(f.Pts, at)) return f;
                float d = MapGeometry.DistanceToEdge(f.Pts, at);
                if (d < bestD)
                {
                    bestD = d;
                    best = f;
                }
            }
            return best;
        }
        #endregion
    }
}
