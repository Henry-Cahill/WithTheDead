using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The land use as one partition of the ground: which area's ground shows where, drawn as trapezoids at one height.
    internal static partial class GroundPass
    {
        // Where outlines overlap, a higher tier shows; within a tier, the smaller area.
        private const int TierBroad = 0, TierCover = 1, TierFeature = 2;

        /// <summary>A land-use area cut to the square (or to a band of the scenery ring), and the part of it that shows.</summary>
        private sealed class Owner
        {
            public Patch Patch;
            public string Kind;
            public long Id;
            public int Tier;
            /// <summary>The whole area (as mapped, in its region) in square metres: the same in every chunk.</summary>
            public double FullArea;
            /// <summary>The area cut to the domain, anticlockwise.</summary>
            public List<Vector2> Pts;
            public Rect Bounds;
            /// <summary>Its place in the domain's priority order: where areas overlap, the highest rank shows.</summary>
            public int Rank;
            /// <summary>Where its ground shows (emptied once drawn).</summary>
            public readonly List<Trap> Traps = new List<Trap>();
            /// <summary>Square metres of its ground that show.</summary>
            public float Visible;
            /// <summary>See <see cref="Ground.Whole"/>.</summary>
            public List<Vector2> Whole;
            public bool WholeDone;
            /// <summary>On a streamed piece, the area's region in site-local metres, once <see cref="Whole"/> is known:
            /// the whole outline's edges along it are cuts, not shores.</summary>
            public Rect Region;
            /// <summary>For water, what it looks like in January (see <see cref="LookOf"/>).</summary>
            public WaterLook Look;
            public bool LookDone;

            private bool _axisDone;
            private Vector2 _axis;

            /// <summary>The long axis of the area's whole outline (a unit vector, from the data, so the same in every
            /// chunk): the way field rows, headstone rows and parking aisles run.</summary>
            public Vector2 Axis
            {
                get
                {
                    if (!_axisDone)
                    {
                        _axisDone = true;
                        _axis = LongAxis(Patch.Data.pts, Pts);
                    }
                    return _axis;
                }
            }
        }

        /// <summary>A trapezoid of ground: from <see cref="L0"/> to <see cref="R0"/> along y = <see cref="Y0"/>, to
        /// <see cref="L1"/>..<see cref="R1"/> along y = <see cref="Y1"/> (x, z site-local).</summary>
        private struct Trap { public float Y0, Y1, L0, R0, L1, R1; }

        /// <summary>The priority tier of a land-use kind: facilities show over cover, and cover over the broad ground.</summary>
        private static int TierOf(string kind)
        {
            switch (kind)
            {
                case "parking": case "fuel": case "plaza": case "pitch": case "playground": case "cemetery": case "lot":
                    return TierFeature;
                case "water": case "forest": case "farmland": case "meadow": case "grass": case "scrub": case "wetland":
                case "orchard": case "rock": case "sand": case "quarry": case "farmyard":
                    return TierCover;
                default:   // residential, commercial, retail, industrial, park, golf, school, hospital, churchyard,
                    return TierBroad;   // military, airfield, and kinds this game doesn't know (plain ground)
            }
        }

        /// <summary>The surface an area's ground is filled with, or false where the base snow shows (yards, parks, lawns,
        /// cemeteries, pitches, most farmland: their look is the snow plus what stands or lies on it).</summary>
        private static bool FillOf(Owner o, out Surf surf)
        {
            switch (o.Kind)
            {
                // Wide water keeps a channel open through the freeze; the rest has frozen to ice.
                case "water": surf = LookOf(o).Open ? WaterSurf : IceSurf; return true;
                case "forest": surf = ForestFloorSurf; return true;
                case "meadow": case "orchard": surf = MeadowSurf; return true;
                case "scrub": surf = ScrubSurf; return true;
                case "wetland": surf = ReedsSurf; return true;
                case "parking": surf = LotSurf; return true;
                case "fuel": surf = ForecourtSurf; return true;
                // Fixed by the area's id, so every chunk of a plaza is paved the same way.
                case "plaza": surf = MapStyle.Unit(o.Id, SaltPlaza) < 0.5f ? BrickPaversSurf : SlabsSurf; return true;
                case "lot": surf = MudSurf; return true;
                case "sand": surf = SandSurf; return true;
                case "farmyard": surf = FarmyardSurf; return true;
                case "industrial": surf = YardSurf; return true;
                case "quarry": case "rock": surf = RockSurf; return true;
                case "playground": surf = TroddenSurf; return true;
                case "farmland":
                    // Only a hayfield is filled as meadow; the other crops show as rows drawn over the base snow.
                    if (CropOf(o) == Crop.Hay)
                    {
                        surf = MeadowSurf;
                        return true;
                    }
                    break;
            }
            surf = default;
            return false;
        }

        // The square's areas (and a fixed map's scenery ring's, a band at a time), made into one partition and filled;
        // the water and the parking lots taken, so nothing stands on them.
        /// <summary>Divides the square's ground into owners' partitions and fills their ground, handling water and parking.</summary>
        private static void Partition(Ground g)
        {
            g.Owners = Collect(g.Site, g.Square);
            foreach (Owner o in g.Owners)
            {
                if (FillOf(o, out Surf surf)) Fill(g.Site.Town.Out, o, FillY, surf);
                if (o.Kind == "water" || o.Kind == "parking") TakeTraps(g.Site, o);
            }
            if (g.Site.Chunked) return;
            float half = g.Half, reach = g.Half + SceneryReach;
            var bands = new[]
            {
                Rect.MinMaxRect(-reach, -reach, -half, reach), Rect.MinMaxRect(half, -reach, reach, reach),
                Rect.MinMaxRect(-half, half, half, reach), Rect.MinMaxRect(-half, -reach, half, -half),
            };
            foreach (Rect band in bands)
            {
                List<Owner> owners = Collect(g.Site, band);
                g.Ring.Add(owners);
                foreach (Owner o in owners)
                    if (FillOf(o, out Surf surf)) Fill(g.Site.Town.Far, o, FillY, surf);
            }
        }

        /// <summary>Collects owners whose patches intersect the given domain and prepares them for partitioning.</summary>
        private static List<Owner> Collect(Site site, Rect domain)
        {
            var owners = new List<Owner>();
            foreach (Patch p in site.Area.Areas)
            {
                if (p.Pts.Count < 3 || !MapGeometry.Bounds(p.Pts).Overlaps(domain)) continue;
                List<Vector2> pts = MapGeometry.ClipPolygon(p.Pts, domain);
                if (pts.Count < 3 || Mathf.Abs(MapGeometry.SignedArea(pts)) < 0.25f) continue;
                string kind = p.Data.kind ?? "";
                owners.Add(new Owner
                {
                    Patch = p, Kind = kind, Id = p.Data.id, Tier = TierOf(kind), FullArea = AreaOf(p.Data.pts, p.Pts),
                    Pts = pts, Bounds = MapGeometry.Bounds(pts),
                });
            }
            owners.Sort(ByPriority);
            for (int i = 0; i < owners.Count; i++) owners[i].Rank = i;
            Compose(owners);
            return owners;
        }

        /// <summary>Compares two owners for sorting: first by tier, then by descending area, then by id.</summary>
        private static int ByPriority(Owner a, Owner b)
        {
            if (a.Tier != b.Tier) return a.Tier.CompareTo(b.Tier);
            if (a.FullArea != b.FullArea) return b.FullArea.CompareTo(a.FullArea);
            return a.Id.CompareTo(b.Id);
        }

        // Square metres of the area as mapped (decimetre corners, added exactly), or of what's at hand without them.
        /// <summary>Computes the area of a patch from decimetre coordinates or, if unavailable, from fallback vertices.</summary>
        private static double AreaOf(int[] dm, List<Vector2> fallback)
        {
            if (dm == null || dm.Length < 6) return Mathf.Abs(MapGeometry.SignedArea(fallback));
            long sum = 0;
            int n = dm.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++) sum += (long)dm[2 * j] * dm[2 * i + 1] - (long)dm[2 * i] * dm[2 * j + 1];
            return System.Math.Abs(sum) / 200.0;
        }

        #region The partition
        /// <summary>One non-horizontal edge of an owner's cut outline: its x at its lower end and how that x changes with y.</summary>
        private struct Edge
        {
            public float X0, Y0, Y1, Slope;
            public int Owner;
            public float X(float y) => X0 + (y - Y0) * Slope;
        }

        private const float SlabEps = 1e-5f;

        /// <summary>Works out which owner's ground shows where, as trapezoids (<see cref="Owner.Traps"/>): a sweep up
        /// the domain, slab by slab between the outlines' corners (and where their edges cross), each slab's spans
        /// given to the highest-ranked owner inside them by the even-odd rule. Exact, whatever the outlines do (the
        /// slivers and bridges cutting leaves along the domain's edges cancel out), and spans that carry on unchanged
        /// from slab to slab are merged.</summary>
        private static void Compose(List<Owner> owners)
        {
            if (owners.Count == 0) return;
            var edges = new List<Edge>();
            var ys = new List<float>();
            for (int o = 0; o < owners.Count; o++)
            {
                List<Vector2> p = owners[o].Pts;
                for (int i = 0; i < p.Count; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % p.Count];
                    // Horizontal edges neither start nor end a slab, so the sweep never needs them.
                    if (Mathf.Abs(a.y - b.y) < SlabEps) continue;
                    // Point every edge upward so Y0 is always its lower end, which the slab loop assumes.
                    if (a.y > b.y)
                    {
                        Vector2 t = a;
                        a = b;
                        b = t;
                    }
                    edges.Add(new Edge { X0 = a.x, Y0 = a.y, Y1 = b.y, Slope = (b.x - a.x) / (b.y - a.y), Owner = o });
                    ys.Add(a.y);
                    ys.Add(b.y);
                }
            }
            if (edges.Count == 0) return;
            // Every corner is a slab boundary, so no edge starts or ends partway up a slab.
            ys.Sort();
            int u = 0;
            for (int i = 1; i < ys.Count; i++)
                if (ys[i] - ys[u] > SlabEps) ys[++u] = ys[i];
            ys.RemoveRange(u + 1, ys.Count - u - 1);
            // Sorted by their lower end, the edges can be added to the active list in one pass up the domain.
            edges.Sort((e, f) => e.Y0.CompareTo(f.Y0));

            var sweep = new Sweep(owners, edges);
            var active = new List<int>();
            int next = 0;
            for (int s = 0; s + 1 < ys.Count; s++)
            {
                float ya = ys[s], yb = ys[s + 1];
                // An edge that has reached this slab's bottom can no longer bound a run in it.
                for (int i = active.Count - 1; i >= 0; i--)
                    if (edges[active[i]].Y1 <= ya + SlabEps) active.RemoveAt(i);
                // Pick up the edges whose lower end is here; the rest start in a later slab.
                while (next < edges.Count && edges[next].Y0 <= ya + SlabEps)
                {
                    if (edges[next].Y1 > ya + SlabEps) active.Add(next);
                    next++;
                }
                sweep.Slab(ya, yb, active);
            }
            sweep.Finish(ys[ys.Count - 1]);
            foreach (Owner o in owners)
            {
                // Each trapezoid's area is its mean width times its height; the total tells later passes
                // whether the owner is worth drawing at all.
                float area = 0f;
                foreach (Trap t in o.Traps) area += (t.R0 - t.L0 + t.R1 - t.L1) * 0.5f * (t.Y1 - t.Y0);
                o.Visible = area;
            }
        }

        /// <summary>The sweep that turns the outlines' edges into trapezoids, giving each run of a slab to the
        /// highest-ranked owner inside it.</summary>
        private sealed class Sweep
        {
            /// <summary>A span of one owner's ground in the slab just swept, kept so it can be continued into the next one.</summary>
            private struct Open { public int Owner, Left, Right; public float Y0; }

            private readonly List<Owner> _owners;
            private readonly List<Edge> _edges;
            private readonly bool[] _in;
            private readonly List<int> _inside = new List<int>();
            private readonly List<int> _order = new List<int>();
            private readonly List<float> _cuts = new List<float>();
            private Dictionary<long, Open> _open = new Dictionary<long, Open>(), _next = new Dictionary<long, Open>();
            private readonly System.Comparison<int> _byX;
            private float _at;

            public Sweep(List<Owner> owners, List<Edge> edges)
            {
                _owners = owners;
                _edges = edges;
                _in = new bool[owners.Count];
                _byX = (a, b) =>
                {
                    int c = _edges[a].X(_at).CompareTo(_edges[b].X(_at));
                    return c != 0 ? c : _edges[a].Slope.CompareTo(_edges[b].Slope);
                };
            }

            /// <summary>Runs one slab of the sweep: sorts its edges, splits the slab where they cross, then fills each piece.</summary>
            public void Slab(float ya, float yb, List<int> active)
            {
                _order.Clear();
                _order.AddRange(active);
                SortAt((ya + yb) * 0.5f);
                bool crossing = false;
                for (int i = 0; i + 1 < _order.Count && !crossing; i++)
                {
                    Edge a = _edges[_order[i]], b = _edges[_order[i + 1]];
                    crossing = a.X(ya) > b.X(ya) + 1e-4f || a.X(yb) > b.X(yb) + 1e-4f;
                }
                if (!crossing)
                {
                    Run(ya, yb);
                    return;
                }
                // Edges cross inside the slab: cut it where they do, so each piece has a fixed left-to-right order.
                _cuts.Clear();
                for (int i = 0; i < _order.Count; i++)
                    for (int j = i + 1; j < _order.Count; j++)
                    {
                        Edge a = _edges[_order[i]], b = _edges[_order[j]];
                        float ds = a.Slope - b.Slope;
                        if (Mathf.Abs(ds) < 1e-9f) continue;
                        float y = ((b.X0 - b.Y0 * b.Slope) - (a.X0 - a.Y0 * a.Slope)) / ds;
                        if (y > ya + SlabEps && y < yb - SlabEps) _cuts.Add(y);
                    }
                _cuts.Sort();
                float y0 = ya;
                for (int i = 0; i < _cuts.Count; i++)
                {
                    float c = _cuts[i];
                    if (c - y0 <= SlabEps) continue;
                    SortAt((y0 + c) * 0.5f);
                    Run(y0, c);
                    y0 = c;
                }
                if (yb - y0 > SlabEps)
                {
                    SortAt((y0 + yb) * 0.5f);
                    Run(y0, yb);
                }
            }

            private void SortAt(float y)
            {
                _at = y;
                _order.Sort(_byX);
            }

            // One slab with a fixed order of edges: walk it left to right, toggling each owner in and out; every run
            // between two edges goes to the highest owner inside.
            private void Run(float ya, float yb)
            {
                int run = -1, left = -1;
                for (int k = 0; k < _order.Count; k++)
                {
                    int e = _order[k], o = _edges[e].Owner;
                    // Even-odd: every edge crossed takes its owner from outside to inside, or back out.
                    if (_in[o])
                    {
                        _in[o] = false;
                        _inside.Remove(o);
                    }
                    else
                    {
                        _in[o] = true;
                        _inside.Add(o);
                    }
                    // The strip between this edge and the next belongs to the highest-ranked owner inside it.
                    int top = -1;
                    for (int i = 0; i < _inside.Count; i++)
                        if (_inside[i] > top) top = _inside[i];
                    if (top == run) continue;
                    // The strip's owner changed: close the span begun at the previous edge and start a new one.
                    if (run >= 0) Span(run, left, e, ya);
                    run = top;
                    left = e;
                }
                // The flags are scratch within one slab, so clear them for the next.
                for (int i = 0; i < _inside.Count; i++) _in[_inside[i]] = false;
                _inside.Clear();
                // A span from the last slab that wasn't continued into this one ends at this slab's bottom.
                foreach (KeyValuePair<long, Open> kv in _open)
                    if (!_next.ContainsKey(kv.Key)) Close(kv.Value, ya);
                // Swap the two span tables back and forth instead of allocating one per slab.
                Dictionary<long, Open> was = _open;
                _open = _next;
                _next = was;
                _next.Clear();
            }

            private void Span(int owner, int left, int right, float ya)
            {
                long key = ((long)owner << 48) | ((long)left << 24) | (long)right;
                _next[key] = _open.TryGetValue(key, out Open open) ? open : new Open { Owner = owner, Left = left, Right = right, Y0 = ya };
            }

            private void Close(Open o, float y1)
            {
                Edge l = _edges[o.Left], r = _edges[o.Right];
                var t = new Trap { Y0 = o.Y0, Y1 = y1, L0 = l.X(o.Y0), R0 = r.X(o.Y0), L1 = l.X(y1), R1 = r.X(y1) };
                if (t.Y1 - t.Y0 <= SlabEps || (t.R0 - t.L0 < 1e-4f && t.R1 - t.L1 < 1e-4f)) return;
                _owners[o.Owner].Traps.Add(t);
            }

            public void Finish(float y)
            {
                foreach (KeyValuePair<long, Open> kv in _open) Close(kv.Value, y);
                _open.Clear();
            }
        }
        #endregion

        #region Drawing and taking the partition
        /// <summary>An owner's ground at height <paramref name="y"/>, face up, each trapezoid into the batch of its middle.</summary>
        private static void Fill(OutdoorBatches into, Owner o, float y, in Surf surf)
        {
            foreach (Trap t in o.Traps) FillTrap(into, t, y, surf);
        }

        /// <summary>Draws one trapezoid's ground face up at height <paramref name="y"/>, into the batch of its middle.</summary>
        private static void FillTrap(OutdoorBatches into, in Trap t, float y, in Surf surf)
        {
            var a = new Vector3(t.L0, y, t.Y0);
            var b = new Vector3(t.R0, y, t.Y0);
            var c = new Vector3(t.R1, y, t.Y1);
            var d = new Vector3(t.L1, y, t.Y1);
            RaidMesh m = into.Flat(new Vector3((t.L0 + t.R0 + t.L1 + t.R1) * 0.25f, 0f, (t.Y0 + t.Y1) * 0.5f));
            if (t.R1 - t.L1 > 1e-4f) m.Tri(a, d, c, surf);   // clockwise seen from above: the face looks up
            if (t.R0 - t.L0 > 1e-4f) m.Tri(a, c, b, surf);
        }

        // Marks the cells an owner's ground covers as taken, a metre-high strip of each trapezoid at a time.
        private static void TakeTraps(Site site, Owner o)
        {
            foreach (Trap t in o.Traps)
            {
                float h = t.Y1 - t.Y0;
                for (float z = Mathf.Floor(t.Y0); z < t.Y1; z += 1f)
                {
                    float za = Mathf.Max(z, t.Y0), zb = Mathf.Min(z + 1f, t.Y1);
                    float fa = (za - t.Y0) / h, fb = (zb - t.Y0) / h;
                    float l = Mathf.Min(Mathf.Lerp(t.L0, t.L1, fa), Mathf.Lerp(t.L0, t.L1, fb));
                    float r = Mathf.Max(Mathf.Lerp(t.R0, t.R1, fa), Mathf.Lerp(t.R0, t.R1, fb));
                    if (r - l < 0.05f || zb - za < 0.05f) continue;
                    site.Take(Rect.MinMaxRect(l + 0.02f, za + 0.02f, r - 0.02f, zb - 0.02f));
                }
            }
        }

        /// <summary>True when an edge of a cut outline lies along the domain's border (made by the cut, not mapped).</summary>
        private static bool OnBorder(Vector2 a, Vector2 b, Rect domain)
        {
            const float e = 0.01f;
            return (Mathf.Abs(a.x - domain.xMin) < e && Mathf.Abs(b.x - domain.xMin) < e)
                || (Mathf.Abs(a.x - domain.xMax) < e && Mathf.Abs(b.x - domain.xMax) < e)
                || (Mathf.Abs(a.y - domain.yMin) < e && Mathf.Abs(b.y - domain.yMin) < e)
                || (Mathf.Abs(a.y - domain.yMax) < e && Mathf.Abs(b.y - domain.yMax) < e);
        }
        #endregion

        #region Shapes
        /// <summary>The long axis (unit) of the smallest rectangle round an outline: from its mapped corners
        /// (decimetres, so the same wherever it's cut) or, without them, from <paramref name="fallback"/>.</summary>
        private static Vector2 LongAxis(int[] dm, List<Vector2> fallback)
        {
            Box(DataOutline(dm, fallback), out Vector2 axis, out _, out _);
            return axis;
        }

        /// <summary>The smallest rectangle round the points (over the directions of their hull's edges): its long axis
        /// (unit, x &gt;= 0), middle and half sizes along the axis and across it.</summary>
        private static void Box(List<Vector2> pts, out Vector2 axis, out Vector2 middle, out Vector2 halfSize)
        {
            List<Vector2> hull = Hull(pts);
            // The defaults only matter if the hull is empty (fewer than three distinct points).
            axis = Vector2.right;
            middle = hull.Count > 0 ? hull[0] : Vector2.zero;
            halfSize = Vector2.zero;
            float best = float.MaxValue;
            // A minimum-area rectangle has a side flush with a hull edge, so trying those directions is enough.
            for (int i = 0; i < hull.Count; i++)
            {
                Vector2 d = hull[(i + 1) % hull.Count] - hull[i];
                // Repeated hull points have no direction to offer.
                if (d.sqrMagnitude < 1e-6f) continue;
                d.Normalize();
                var n = new Vector2(-d.y, d.x);
                // Project the hull onto d and its normal: the spread along each is the box's size.
                float a0 = float.MaxValue, a1 = float.MinValue, b0 = float.MaxValue, b1 = float.MinValue;
                foreach (Vector2 p in hull)
                {
                    float a = Vector2.Dot(p, d), b = Vector2.Dot(p, n);
                    a0 = Mathf.Min(a0, a); a1 = Mathf.Max(a1, a);
                    b0 = Mathf.Min(b0, b); b1 = Mathf.Max(b1, b);
                }
                float area = (a1 - a0) * (b1 - b0);
                // Keep the first of two near-equal boxes; the epsilon keeps float noise from flipping the choice.
                if (area >= best - 1e-4f) continue;
                best = area;
                // The long axis is the longer side, so swap the extents when the normal is longer.
                bool along = a1 - a0 >= b1 - b0;
                axis = along ? d : n;
                middle = d * ((a0 + a1) * 0.5f) + n * ((b0 + b1) * 0.5f);
                halfSize = along ? new Vector2((a1 - a0) * 0.5f, (b1 - b0) * 0.5f) : new Vector2((b1 - b0) * 0.5f, (a1 - a0) * 0.5f);
            }
            // Fix the sign so the axis doesn't depend on which way round the hull passed.
            if (axis.x < 0f || (axis.x == 0f && axis.y < 0f)) axis = -axis;
        }

        /// <summary>The convex hull (monotone chain), anticlockwise.</summary>
        private static List<Vector2> Hull(List<Vector2> pts)
        {
            var sorted = new List<Vector2>(pts);
            sorted.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var hull = new List<Vector2>(sorted.Count + 1);
            for (int pass = 0; pass < 2; pass++)
            {
                int start = hull.Count;
                for (int i = 0; i < sorted.Count; i++)
                {
                    Vector2 p = pass == 0 ? sorted[i] : sorted[sorted.Count - 1 - i];
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0f) hull.RemoveAt(hull.Count - 1);
                    hull.Add(p);
                }
                hull.RemoveAt(hull.Count - 1);
            }
            return hull;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        /// <summary>The spans (pairs of distances along <paramref name="dir"/> from <paramref name="origin"/>) where the
        /// line through <paramref name="origin"/> runs inside the polygon (even-odd), sorted, into <paramref name="into"/>.</summary>
        private static void Spans(List<Vector2> poly, Vector2 origin, Vector2 dir, List<float> into)
        {
            into.Clear();
            var n = new Vector2(-dir.y, dir.x);
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                float si = Vector2.Dot(poly[i] - origin, n), sj = Vector2.Dot(poly[j] - origin, n);
                if ((si > 0f) == (sj > 0f)) continue;
                float t = si / (si - sj);
                into.Add(Vector2.Dot(Vector2.Lerp(poly[i], poly[j], t) - origin, dir));
            }
            into.Sort();
            if ((into.Count & 1) == 1) into.RemoveAt(into.Count - 1);
        }
        #endregion
    }
}
