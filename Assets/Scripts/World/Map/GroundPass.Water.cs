using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Ponds, lakes, rivers, streams and ditches in January. Still water is frozen over: grey-blue ice (the partition's
    // fill) with white rims where snow drifted against the shore, snow lying on it in long wind-driven patches, reedy or
    // muddy banks. Water as wide as a river (the Ohio, the Kentucky, the Licking, the Cumberland...) runs open: black
    // water (the fill) between shelves of ice along the banks, ragged where the current broke them, and drift ice down
    // the open channel; a river too narrow for that freezes but for a dark thread down its line. Streams and ditches
    // mapped as lines are dark threads of open water between ice shelves in a muddy bed. Culverts (waterways in tunnels)
    // aren't drawn. A body of water's look comes from its own data (its name, size and shape), so every piece of it
    // agrees.
    internal static partial class GroundPass
    {
        private const float StreamStep = 2.5f;   // metres between a waterway's points as drawn (they wobble)

        /// <summary>What a body of water looks like in January.</summary>
        private struct WaterLook
        {
            /// <summary>Flowing water: muddy, stony banks, and a thread of open water down it when it's frozen.</summary>
            public bool River;
            /// <summary>Wide enough to run open between shelves of ice.</summary>
            public bool Open;
            /// <summary>Its mean width (twice its area over its perimeter), and how far its shelf ice reaches out.</summary>
            public float Width, Shelf;
        }

        private static IEnumerable<string> Water(Ground g)
        {
            Site site = g.Site;
            foreach (string step in WaterIn(g, g.Owners, g.Square, site.Town.Out, true)) yield return step;
            for (int b = 0; b < g.Ring.Count; b++)
                foreach (string step in WaterIn(g, g.Ring[b], RingBand(g, b), site.Town.Far, false)) yield return step;
            foreach (Line l in site.Area.Lines)
            {
                if (l.IsRailway || l.Data.tunnel) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, g.Square)) Waterway(g, l, piece, g.Owners, site.Town.Out, true);
                if (!site.Chunked)
                    for (int b = 0; b < g.Ring.Count; b++)
                        foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, RingBand(g, b))) Waterway(g, l, piece, g.Ring[b], site.Town.Far, false);
                if (site.StepDue) yield return "water";
            }
        }

        private static IEnumerable<string> WaterIn(Ground g, List<Owner> owners, Rect domain, OutdoorBatches into, bool inside)
        {
            foreach (Owner o in owners)
            {
                if (o.Kind != "water" || o.Visible < 1f) continue;
                WaterLook look = LookOf(o);
                Shore(g, o, owners, domain, into, look, inside);
                if (!g.Lite)
                {
                    if (look.Open) Floes(g, o, look, owners, domain, into);
                    else if (inside) IcePatches(g, o);
                }
                if (g.Site.StepDue) yield return "water";
            }
        }

        // The bands round a fixed map's square, in the order Partition made the ring's owners.
        private static Rect RingBand(Ground g, int band)
        {
            float half = g.Half, reach = g.Half + SceneryReach;
            switch (band)
            {
                case 0: return Rect.MinMaxRect(-reach, -reach, -half, reach);
                case 1: return Rect.MinMaxRect(half, -reach, reach, reach);
                case 2: return Rect.MinMaxRect(-half, half, half, reach);
                default: return Rect.MinMaxRect(-half, -reach, half, -half);
            }
        }

        #region What a body of water is
        // From its own outline and name (so every piece of it agrees): flowing water (a river's name, or drawn out like
        // one: a circle's length is twice its mean width, a river piece's many times it) wide enough runs open; still
        // water freezes over unless it's as big as a reservoir.
        /// <summary>Compute a WaterLook describing a water body's characteristics based on its shape and name.</summary>
        private static WaterLook LookOf(Owner o)
        {
            // If this Owner's look has already been computed, reuse it to avoid work
            if (o.LookDone) return o.Look;
            // Mark that we are computing the look so we don't recurse
            o.LookDone = true;
            // Get the polygon points for this water body, using the stored outline if available
            List<Vector2> pts = DataOutline(o.Patch.Data.pts, o.Patch.Pts);
            double perimeter = 0.0;
            // Sum edge lengths to get perimeter for shape analysis
            for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++) perimeter += Vector2.Distance(pts[i], pts[j]);
            // Approximate mean width: area divided by half‑perimeter (circle formula), guard against tiny perimeter
            float width = perimeter > 1.0 ? (float)(2.0 * o.FullArea / perimeter) : 0f;
            // Extract extents of the polygon to infer overall length
            Box(pts, out _, out _, out Vector2 half);
            // Full length across the longest axis (horizontal diameter of bounding box)
            float length = half.x * 2f;
            // Determine water type by matching common name keywords (case‑insensitive)
            string name = o.Patch.Data.name ?? "";
            // Still water names indicate lakes, ponds or reservoirs (no flow)
            bool still = Named(name, "lake") || Named(name, "pond") || Named(name, "reservoir");
            // Flowing water names indicate rivers, creeks etc.
            bool flowing = Named(name, "river") || Named(name, "creek") || Named(name, "fork") || Named(name, "branch")
                || Named(name, "canal") || Named(name, "run");
            // Initialise WaterLook: width from geometry, shelf width clamped to reasonable limits
            var look = new WaterLook { Width = width, Shelf = Mathf.Clamp(width * 0.2f, 3f, 40f) };
            // Heuristic: a river if not still and either named as flowing or geometrically long and narrow
            look.River = !still && (flowing || (length >= width * 4.5f && width > 4f));
            // Open water if river is sufficiently wide, otherwise only large lakes count as open
            look.Open = look.River ? width >= 30f : width >= 150f;
            o.Look = look;
            return look;
        }

        // A whole word of a name, in any case ("Licking River", "Town Branch").
        private static bool Named(string name, string word)
        {
            int at = name.IndexOf(word, System.StringComparison.OrdinalIgnoreCase);
            while (at >= 0)
            {
                int end = at + word.Length;
                if ((at == 0 || !char.IsLetter(name[at - 1])) && (end == name.Length || !char.IsLetter(name[end]))) return true;
                at = name.IndexOf(word, at + 1, System.StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>An area's outline as mapped (decimetre corners, in metres from the first, so floats keep their
        /// precision), or <paramref name="fallback"/> without them.</summary>
        private static List<Vector2> DataOutline(int[] dm, List<Vector2> fallback)
        {
            if (dm == null || dm.Length < 6) return fallback;
            var pts = new List<Vector2>(dm.Length / 2);
            for (int i = 0; i + 1 < dm.Length; i += 2) pts.Add(new Vector2((dm[i] - dm[0]) * 0.1f, (dm[i + 1] - dm[1]) * 0.1f));
            return pts;
        }
        #endregion

        #region Shores
        // Along the water's mapped edges, wherever the far side isn't more water: on frozen water a rim of drifted snow on
        // the ice; on open water the shelf ice, ragged at its outer edge, with snow on it along the shore. And a bank on
        // the land side (reedy round ponds and lakes, muddy and stony along rivers) wherever it isn't paving.
        private static void Shore(Ground g, Owner o, List<Owner> owners, Rect domain, OutdoorBatches into, in WaterLook look, bool inside)
        {
            System.Random rng = MapStyle.Rng(o.Id, SaltRim);
            float rim = MapStyle.Range(rng, 1.0f, 2.4f), bank = look.River ? MapStyle.Range(rng, 2.0f, 3.5f) : MapStyle.Range(rng, 1.4f, 2.6f);
            bool reedy = !look.River && MapStyle.Unit(o.Id, SaltBank) < 0.75f;
            List<Vector2> p = o.Pts;
            int n = p.Count;
            var side = new int[n];   // per edge: 0 no shore (cut, or more water beyond), 1 rim only (paving beyond), 2 rim and bank
            for (int k = 0; k < n; k++) side[k] = Beside(owners, p[k], p[(k + 1) % n], domain);
            var chain = new List<Vector2>();
            for (int pass = 1; pass <= 2; pass++)
            {
                // Chains of edges with this much shore, walked from one without (or round the whole outline, closed).
                int start = -1;
                for (int k = 0; k < n && start < 0; k++)
                    if (side[k] < pass) start = k;
                bool closed = start < 0;
                chain.Clear();
                for (int s = 0; s <= n; s++)
                {
                    int k = (start + 1 + s) % n;
                    if (s < n && side[k] >= pass)
                    {
                        if (chain.Count == 0) chain.Add(p[k]);
                        Densify(chain, p[(k + 1) % n], StreamStep);
                        continue;
                    }
                    if (chain.Count >= 2)
                    {
                        if (pass == 2) BankChain(g, chain, bank, reedy, domain, closed, into, inside);
                        else if (look.Open) ShelfChain(g, chain, look, rim, domain, closed, into);
                        else RimChain(g, chain, rim, domain, closed, into);
                    }
                    chain.Clear();
                }
            }
        }

        // What lies beyond a stretch of shore (the right of the anticlockwise outline): 0 the domain's border or more
        // water, 1 paving or a lot, 2 land a bank belongs on.
        private static int Beside(List<Owner> owners, Vector2 a, Vector2 b, Rect domain)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 1e-3f || OnBorder(a, b, domain)) return 0;
            Vector2 outward = new Vector2(d.y, -d.x) / len;
            Vector2 q = (a + b) * 0.5f + outward * 1.5f;
            if (!domain.Contains(q)) return 2;
            Owner there = TopAt(owners, q);
            if (there == null) return 2;
            if (there.Kind == "water") return 0;
            return there.Tier == TierFeature ? 1 : 2;
        }

        private static Owner TopAt(List<Owner> owners, Vector2 p)
        {
            for (int i = owners.Count - 1; i >= 0; i--)
            {
                Owner o = owners[i];
                if (o.Bounds.Contains(p) && Inside(o.Pts, p)) return o;
            }
            return null;
        }

        // How much of a shore's width to draw at chain point i: full along it, round a closed shore and where it runs off
        // the domain's border (the next chunk carries on), narrowing to nothing where the shore itself ends.
        private static float Taper(List<Vector2> chain, int i, Rect domain, bool closed)
        {
            if (closed) return 1f;
            int n = chain.Count;
            float fromStart = OnEdge(chain[0], domain) ? 99f : i, fromEnd = OnEdge(chain[n - 1], domain) ? 99f : n - 1 - i;
            return Mathf.Min(1f, Mathf.Min(fromStart, fromEnd) * 0.35f + 0.25f);
        }

        private static bool OnEdge(Vector2 p, Rect r)
            => Mathf.Abs(p.x - r.xMin) < 0.01f || Mathf.Abs(p.x - r.xMax) < 0.01f || Mathf.Abs(p.y - r.yMin) < 0.01f || Mathf.Abs(p.y - r.yMax) < 0.01f;

        private static void RimChain(Ground g, List<Vector2> chain, float rim, Rect domain, bool closed, OutdoorBatches into)
        {
            var normals = new List<Vector2>(chain.Count);
            Normals(chain, normals);
            var inner = new float[chain.Count];
            for (int i = 0; i < chain.Count; i++)
                inner[i] = rim * (0.55f + 0.9f * WorldNoise(g.World(chain[i]), 7f, SaltRim)) * Taper(chain, i, domain, closed);
            Strip(into, chain, normals, inner, null, DecalY, SnowSurf);   // left of the outline is the ice
        }

        // Shelf ice along a stretch of an open river's bank: out from the shore by the river's shelf, now back in bays
        // where the current took it, now out in points, its edge jagged; snow lying over most of it, the bare grey ice
        // showing only along its outer edge.
        private static void ShelfChain(Ground g, List<Vector2> chain, in WaterLook look, float rim, Rect domain, bool closed, OutdoorBatches into)
        {
            int n = chain.Count;
            var normals = new List<Vector2>(n);
            SmoothNormals(chain, normals, Mathf.Clamp(Mathf.RoundToInt(look.Shelf / StreamStep * 0.5f), 1, 6), domain);
            var shelf = new float[n];
            var snow = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 w = g.World(chain[i]);
                float k = 0.35f + 1.1f * WorldNoise(w, 26f, SaltShelf) + 0.25f * (WorldNoise(w, 4.5f, SaltShelf + 1) - 0.5f);
                shelf[i] = look.Shelf * Mathf.Clamp(k, 0.12f, 1.6f) * Taper(chain, i, domain, closed);
                float cover = 0.55f + 0.4f * WorldNoise(w, 11f, SaltShelf + 2);
                snow[i] = Mathf.Max(Mathf.Min(shelf[i], rim), shelf[i] * cover - 0.6f);
            }
            Strip(into, chain, normals, shelf, null, ShelfY, IceSurf);
            Strip(into, chain, normals, snow, null, FloeY, SnowSurf);
        }

        private static void BankChain(Ground g, List<Vector2> chain, float bank, bool reedy, Rect domain, bool closed, OutdoorBatches into, bool inside)
        {
            var normals = new List<Vector2>(chain.Count);
            Normals(chain, normals);
            int n = chain.Count;
            var outer = new float[n];
            for (int i = 0; i < n; i++)
                outer[i] = bank * (0.6f + 0.8f * WorldNoise(g.World(chain[i]), 9f, SaltBank)) * Taper(chain, i, domain, closed);
            Strip(into, chain, normals, null, outer, DecalY, BankSurf);   // right of the outline is the land
            if (g.Lite || !reedy || !inside) return;
            for (int i = 1; i + 1 < n; i++)
            {
                long key = MapStyle.Key(g.World(chain[i]));
                if (MapStyle.Unit(key, SaltReeds) > 0.55f) continue;
                System.Random rng = MapStyle.Rng(key, SaltReeds + 1);
                Vector2 at = chain[i] - normals[i] * (outer[i] * MapStyle.Range(rng, 0.2f, 0.8f));
                if (!g.Clear(at, 0.3f)) continue;
                ReedClump(into, at, rng, MapStyle.Range(rng, 0.8f, 1.3f));
            }
        }
        #endregion

        #region Snow and drift ice on the water
        // Long patches of snow drifted on frozen water, on a 10 m lattice fixed to the world, lying along the wind
        // (north-west to south-east), each kept a little in from the shore.
        private static void IcePatches(Ground g, Owner o)
        {
            const float cell = 10f;
            Rect b = o.Bounds;
            Vector2 w0 = g.World(new Vector2(b.xMin, b.yMin)), w1 = g.World(new Vector2(b.xMax, b.yMax));
            int i0 = Mathf.FloorToInt(w0.x / cell), i1 = Mathf.FloorToInt(w1.x / cell);
            int j0 = Mathf.FloorToInt(w0.y / cell), j1 = Mathf.FloorToInt(w1.y / cell);
            Surf snow = SnowSurf;
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    long key = ((long)i << 32) ^ (uint)j;
                    if (MapStyle.Unit(key, SaltIcePatch) > 0.5f) continue;
                    System.Random rng = MapStyle.Rng(key, SaltIcePatch + 1);
                    var w = new Vector2((i + MapStyle.Range(rng, 0.1f, 0.9f)) * cell, (j + MapStyle.Range(rng, 0.1f, 0.9f)) * cell);
                    Vector2 at = w - g.Offset;
                    if (Mathf.Abs(at.x) > g.Half || Mathf.Abs(at.y) > g.Half || !g.Shows(o, at)) continue;
                    float r = MapStyle.Range(rng, 2f, 5.5f), stretch = MapStyle.Range(rng, 1.5f, 3f);
                    if (RealEdgeDistance(o.Pts, at, g.Square) < r * 0.6f) continue;
                    Vector3 c = To3(at, DecalY);
                    // Long along the wind: yaw 45 turns the blob's long (x) axis to point south-east.
                    g.Site.Town.Out.Flat(c).FlatBlob(c, r * stretch, r, 45f + MapStyle.Range(rng, -20f, 20f), 0.3f, rng, snow);
                }
        }

        // Plates of drift ice coming down an open river, on a 9 m lattice fixed to the world, in loose rafts where a slow
        // noise says so, with brash between them; kept off the shelves (as wide as they can reach).
        /// <summary>Generate ice floes and rafts on open water when conditions allow.</summary>
        private static void Floes(Ground g, Owner o, in WaterLook look, List<Owner> owners, Rect domain, OutdoorBatches into)
        {
            // Lattice cell size used to space potential floe positions evenly
            const float cell = 9f;
            // Convert water bounds from map‑local to world coordinates
            Rect b = o.Bounds;
            Vector2 w0 = g.World(b.min), w1 = g.World(b.max);
            int i0 = Mathf.FloorToInt(w0.x / cell), i1 = Mathf.FloorToInt(w1.x / cell);
            int j0 = Mathf.FloorToInt(w0.y / cell), j1 = Mathf.FloorToInt(w1.y / cell);
            float clear = look.Shelf * 1.6f + 1.5f;
            Surf floe = FloeSurf;
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    long key = ((long)i << 32) ^ (uint)j;
                    float raft = WorldNoise(new Vector2((i + 0.5f) * cell, (j + 0.5f) * cell), 70f, SaltFloe);
                    for (int piece = 0; piece < 3; piece++)
                    {
                        float chance = piece == 0 ? 0.12f + 0.6f * Smooth(0.3f, 0.75f, raft) : 0.2f + 0.45f * raft;
                        if (MapStyle.Unit(key, SaltFloe + 1 + piece) >= chance) continue;
                        System.Random rng = MapStyle.Rng(key, SaltFloe + 8 + piece);
                        var w = new Vector2((i + MapStyle.Range(rng, 0.1f, 0.9f)) * cell, (j + MapStyle.Range(rng, 0.1f, 0.9f)) * cell);
                        Vector2 at = w - g.Offset;
                        if (!domain.Contains(at) || !o.Bounds.Contains(at) || TopAt(owners, at) != o) continue;
                        float r = piece == 0 ? MapStyle.Range(rng, 1.4f, 4.2f) : MapStyle.Range(rng, 0.3f, 1.0f);
                        if (ShoreDistance(g, o, at, domain, clear + r) < clear + r) continue;
                        Vector3 c = To3(at, FloeY);
                        into.Flat(c).FlatBlob(c, r * MapStyle.Range(rng, 1f, 1.6f), r, MapStyle.Range(rng, 0f, 180f), 0.45f, rng, floe);
                    }
                }
        }

        /// <summary>The distance from <paramref name="q"/> to the nearest mapped edge of a cut outline (the edges the cut
        /// made along the domain's border don't count; the far side is the next chunk's).</summary>
        private static float RealEdgeDistance(List<Vector2> p, Vector2 q, Rect domain)
        {
            float best = float.MaxValue;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
                if (!OnBorder(p[j], p[i], domain)) best = Mathf.Min(best, MapGeometry.DistanceToSegment(q, p[j], p[i]));
            return best;
        }

        // How far a point of the water is from its real shore, exactly up to <paramref name="need"/>: the domain's own
        // edges first; only on a streamed piece near its edge, where the shore might lie just over the seam, the area's
        // whole outline (so every piece decides alike).
        private static float ShoreDistance(Ground g, Owner o, Vector2 p, Rect domain, float need)
        {
            float d = RealEdgeDistance(o.Pts, p, domain);
            if (d < need || !g.Site.Chunked || Mathf.Min(g.Half - Mathf.Abs(p.x), g.Half - Mathf.Abs(p.y)) >= need) return d;
            List<Vector2> whole = g.Whole(o);
            if (whole == null) return d;
            for (int i = 0, j = whole.Count - 1; i < whole.Count; j = i++)
            {
                Vector2 a = whole[j], c = whole[i];
                if (OnBorder(a, c, o.Region)) continue;   // cut along the region's edge: the water carries on beyond
                d = Mathf.Min(d, MapGeometry.DistanceToSegment(p, a, c));
            }
            return d;
        }
        #endregion

        #region Waterways
        // One piece of a waterway inside a domain: through water, a dark thread of open water down a frozen river (open
        // water needs nothing); elsewhere a stream, river, canal, ditch or drain of its own: a muddy bed, ice shelves and
        // a dark thread of open water, wobbling as streams do.
        private static void Waterway(Ground g, Line l, List<Vector2> piece, List<Owner> owners, OutdoorBatches into, bool inside)
        {
            if (piece.Count < 2) return;
            string kind = l.Data.kind ?? "";
            bool ditch = kind == "ditch" || kind == "drain";
            float width = Mathf.Clamp(l.Data.width, 0.8f, 60f);
            var pts = new List<Vector2>(piece.Count * 4) { piece[0] };
            for (int i = 1; i < piece.Count; i++) Densify(pts, piece[i], StreamStep);
            int wet = 0;
            var water = new Owner[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                water[i] = WaterAt(owners, pts[i]);
                if (water[i] != null) wet++;
            }
            int n = pts.Count;
            var normals = new List<Vector2>(n);
            Normals(pts, normals);
            if (wet * 10 >= n * 6)
            {
                if (!ditch) Thread(g, pts, normals, water, into);
                return;
            }
            var bed = new float[n];
            var shelf = new float[n];
            var core = new float[n];
            float open = ditch ? 0.22f : width >= 8f ? 0.55f : 0.42f;
            for (int i = 0; i < n; i++)
            {
                Vector2 w = g.World(pts[i]);
                float wobble = 0.8f + 0.4f * WorldNoise(w, 6f, SaltStream);
                float halfW = water[i] != null ? 0f : width * 0.5f * wobble;   // where it runs into a pond, it ends at the ice
                bed[i] = water[i] != null ? 0f : halfW + (ditch ? 0.5f : 0.8f) * (0.7f + 0.6f * WorldNoise(w, 4f, SaltStream + 1));
                shelf[i] = halfW;
                float thread = WorldNoise(w, ditch ? 9f : 14f, SaltStream + 2);
                core[i] = halfW * open * Smooth(ditch ? 0.48f : 0.32f, ditch ? 0.66f : 0.55f, thread) * (0.75f + 0.5f * WorldNoise(w, 3f, SaltStream + 3));
            }
            Strip(into, pts, normals, bed, bed, BedY, BankSurf);
            Strip(into, pts, normals, shelf, shelf, StreamIceY, IceSurf);
            Strip(into, pts, normals, core, core, StreamWaterY, WaterSurf);
            if (!inside) return;
            if (!g.Lite && !ditch)
                // Dead reeds and grass standing along the banks.
                for (int i = 1; i + 1 < n; i++)
                {
                    if (bed[i] < 0.5f) continue;
                    long key = MapStyle.Key(g.World(pts[i]));
                    if (MapStyle.Unit(key, SaltReeds + 4) > 0.3f) continue;
                    System.Random rng = MapStyle.Rng(key, SaltReeds + 5);
                    float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                    Vector2 at = pts[i] + normals[i] * (side * bed[i] * MapStyle.Range(rng, 0.75f, 1.05f));
                    if (!g.Clear(at, 0.3f)) continue;
                    ReedClump(into, at, rng, MapStyle.Range(rng, 0.6f, 1.05f));
                }
            g.Site.TakeLine(piece, width * 0.5f + 0.6f);
        }

        private static Owner WaterAt(List<Owner> owners, Vector2 p)
        {
            Owner o = TopAt(owners, p);
            return o != null && o.Kind == "water" ? o : null;
        }

        // Down a frozen river's line: a dark thread of open water where the current keeps the ice from closing, now
        // wider, now gone under the ice.
        private static void Thread(Ground g, List<Vector2> pts, List<Vector2> normals, Owner[] water, OutdoorBatches into)
        {
            int n = pts.Count;
            var core = new float[n];
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                if (water[i] == null) continue;
                WaterLook look = LookOf(water[i]);
                if (look.Open || !look.River) continue;
                Vector2 w = g.World(pts[i]);
                float half = Mathf.Clamp(look.Width * 0.12f, 0.35f, 2.6f);
                core[i] = half * Smooth(0.28f, 0.52f, WorldNoise(w, 16f, SaltChannel)) * (0.7f + 0.6f * WorldNoise(w, 4f, SaltChannel + 1));
                any |= core[i] > 0.01f;
            }
            if (any) Strip(into, pts, normals, core, core, StreamWaterY, WaterSurf);
        }

        // The square or the ring band a point is in (for which cut edges don't count).
        private static Rect Domain(Ground g, Vector2 p)
        {
            if (Mathf.Abs(p.x) <= g.Half && Mathf.Abs(p.y) <= g.Half) return g.Square;
            for (int b = 0; b < 4; b++)
            {
                Rect r = RingBand(g, b);
                if (r.Contains(p)) return r;
            }
            return g.Square;
        }
        #endregion

        #region Drawing along lines
        /// <summary>Adds the points from the last one in <paramref name="pts"/> to <paramref name="to"/>, no more than
        /// <paramref name="step"/> apart.</summary>
        private static void Densify(List<Vector2> pts, Vector2 to, float step)
        {
            Vector2 from = pts[pts.Count - 1];
            float len = Vector2.Distance(from, to);
            int k = Mathf.Max(1, Mathf.CeilToInt(len / step));
            for (int i = 1; i <= k; i++) pts.Add(Vector2.Lerp(from, to, i / (float)k));
        }

        /// <summary>The mitred normals (to the left of the line's way) at each point, long enough that an offset along
        /// one by d lies d from both segments.</summary>
        private static void Normals(List<Vector2> pts, List<Vector2> into)
        {
            into.Clear();
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 dPrev = (i > 0 ? pts[i] - pts[i - 1] : pts[1] - pts[0]).normalized;
                Vector2 dNext = (i < n - 1 ? pts[i + 1] - pts[i] : dPrev).normalized;
                var nPrev = new Vector2(-dPrev.y, dPrev.x);
                var nNext = new Vector2(-dNext.y, dNext.x);
                Vector2 miter = nPrev + nNext;
                miter = miter.sqrMagnitude > 1e-6f ? miter.normalized : nNext;
                into.Add(miter / Mathf.Max(0.5f, Vector2.Dot(miter, nNext)));
            }
        }

        // Unit normals along a line, averaged over k points either side (offset far out, a jagged shore's mitres would
        // cross each other), easing back to the plain ones where the line runs off the domain's border, so the next
        // chunk's carry on from the same.
        /// <summary>Smooth the normals of a shoreline, easing near the map edges to match neighboring chunks.</summary>
        private static void SmoothNormals(List<Vector2> pts, List<Vector2> into, int k, Rect domain)
        {
            // Compute raw normals first; these are the unsmoothed directions at each point
            Normals(pts, into);
            int n = pts.Count;
            var raw = new Vector2[n];
            // Normalise each raw vector, falling back to zero for degenerate cases
            for (int i = 0; i < n; i++) raw[i] = into[i].sqrMagnitude > 1e-8f ? into[i].normalized : Vector2.zero;
            bool startCut = OnEdge(pts[0], domain), endCut = OnEdge(pts[n - 1], domain);
            for (int i = 0; i < n; i++)
            {
                Vector2 sum = Vector2.zero;
                for (int j = Mathf.Max(0, i - k); j <= Mathf.Min(n - 1, i + k); j++) sum += raw[j];
                Vector2 smooth = sum.sqrMagnitude > 1e-6f ? sum.normalized : raw[i];
                float ease = 1f;
                if (startCut) ease = Mathf.Min(ease, i / (float)k);
                if (endCut) ease = Mathf.Min(ease, (n - 1 - i) / (float)k);
                Vector2 v = Vector2.Lerp(raw[i], smooth, Mathf.Clamp01(ease));
                into[i] = v.sqrMagnitude > 1e-6f ? v.normalized : raw[i];
            }
        }

        /// <summary>A band along a line between <paramref name="left"/>[i] to the left of each point and
        /// <paramref name="right"/>[i] to its right (null: the line itself), face up at <paramref name="y"/>.</summary>
        private static void Strip(OutdoorBatches into, List<Vector2> pts, List<Vector2> normals, float[] left, float[] right, float y, in Surf surf)
        {
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                float la = left != null ? left[i] : 0f, lb = left != null ? left[i + 1] : 0f;
                float ra = right != null ? right[i] : 0f, rb = right != null ? right[i + 1] : 0f;
                if (la + ra < 0.02f && lb + rb < 0.02f) continue;
                Vector3 aL = To3(pts[i] + normals[i] * la, y), bL = To3(pts[i + 1] + normals[i + 1] * lb, y);
                Vector3 aR = To3(pts[i] - normals[i] * ra, y), bR = To3(pts[i + 1] - normals[i + 1] * rb, y);
                RaidMesh m = into.Flat(To3((pts[i] + pts[i + 1]) * 0.5f, 0f));
                if (lb + rb >= 0.02f) m.Tri(aL, bL, bR, surf);   // clockwise seen from above: the face looks up
                if (la + ra >= 0.02f) m.Tri(aL, bR, aR, surf);
            }
        }

        /// <summary>Smooth noise in [0, 1] over the world, <paramref name="cell"/> metres a lattice cell: the same in
        /// every chunk that asks about a point.</summary>
        private static float WorldNoise(Vector2 w, float cell, int seed)
        {
            float x = w.x / cell, y = w.y / cell;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = WorldValue(xi, yi, seed), b = WorldValue(xi + 1, yi, seed), c = WorldValue(xi, yi + 1, seed), d = WorldValue(xi + 1, yi + 1, seed);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        private static float WorldValue(int x, int y, int seed) => MapStyle.Unit(((long)x << 32) ^ (uint)y, seed);
        #endregion

        #region Reeds
        /// <summary>A clump of dead reeds and cattails: thin blades of straw and brown, leaning every way, a few with
        /// their cigar heads.</summary>
        private static void ReedClump(OutdoorBatches into, Vector2 at, System.Random rng, float size)
        {
            // Convert the ground position to world space at water level (y=0) for mesh placement
            // Convert the ground position to world space at water level (y=0) for mesh placement
            Vector3 c = To3(at, 0f);
            RaidMesh m = into.Solid(c);
            int blades = rng.Next(6, 11);
            for (int k = 0; k < blades; k++)
            {
                // Random yaw gives each blade a different orientation around the vertical axis, and lean determines tilt
                float yaw = MapStyle.Range(rng, 0f, Mathf.PI * 2f), lean = MapStyle.Range(rng, 0.05f, 0.35f);
                float h = size * MapStyle.Range(rng, 0.55f, 1.15f);
                Vector3 baseAt = c + new Vector3(MapStyle.Range(rng, -0.25f, 0.25f), 0f, MapStyle.Range(rng, -0.25f, 0.25f)) * size;
                var dir = new Vector3(Mathf.Sin(yaw) * lean, 1f, Mathf.Cos(yaw) * lean).normalized;
                var side = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw)) * 0.025f;
                Vector3 tip = baseAt + dir * h;
                Color col = Color.Lerp(new Color(0.70f, 0.60f, 0.40f), new Color(0.47f, 0.38f, 0.25f), (float)rng.NextDouble());
                Surf s = DecorPalette.Matte(col);
                m.Tri(baseAt - side, tip, baseAt + side, s);
                m.Tri(baseAt + side, tip, baseAt - side, s);
                if (rng.NextDouble() < 0.3)
                {
                    Vector3 h0 = baseAt + dir * (h * 0.72f), h1 = baseAt + dir * (h * 0.9f);
                    Vector3 wide = side * 1.8f;
                    Surf head = DecorPalette.Matte(new Color(0.33f, 0.23f, 0.16f));
                    m.Tri(h0 - wide, h1 - wide, h1 + wide, head);
                    m.Tri(h0 - wide, h1 + wide, h0 + wide, head);
                    m.Tri(h0 + wide, h1 + wide, h1 - wide, head);
                    m.Tri(h0 + wide, h1 - wide, h0 - wide, head);
                }
            }
        }
        #endregion
    }
}
