using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Ponds, lakes, rivers, streams and ditches in January: the ice (filled with the partition) gets white rims where
    // snow drifted against the shore, snow lying on it in long wind-driven patches, reedy or muddy banks; a big river
    // keeps a dark open channel down its middle between shelves of ice; streams and ditches are dark threads of open
    // water between ice shelves in a muddy bed. Culverts (waterways in tunnels) aren't drawn.
    internal static partial class GroundPass
    {
        private const float StreamStep = 2.5f;   // metres between a waterway's points as drawn (they wobble)

        private static void Water(Ground g)
        {
            foreach (Owner o in g.Owners)
            {
                if (o.Kind != "water" || o.Visible < 1f) continue;
                bool river = HasRiver(g, o);
                Shore(g, o, g.Square, river);
                if (!g.Lite) IcePatches(g, o);
            }
            foreach (Line l in g.Site.Area.Lines)
            {
                if (l.IsRailway || l.Data.tunnel) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, g.Square)) Waterway(g, l, piece, g.Owners, g.Site.Town.Out, true);
                if (g.Site.Chunked) continue;
                for (int b = 0; b < g.Ring.Count; b++)
                    foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, RingBand(g, b))) Waterway(g, l, piece, g.Ring[b], g.Site.Town.Far, false);
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

        // True when a river or canal runs through the water (so it's a river, with muddy banks, not a pond).
        private static bool HasRiver(Ground g, Owner o)
        {
            foreach (Line l in g.Site.Area.Lines)
            {
                if (l.Data.tunnel || (l.Data.kind != "river" && l.Data.kind != "canal")) continue;
                foreach (Vector2 p in l.Pts)
                    if (o.Bounds.Contains(p) && Inside(o.Pts, p)) return true;
            }
            return false;
        }

        #region Shores
        // Along the water's mapped edges: a rim of drifted snow on the ice wherever the far side isn't more water, and
        // a bank on the land side (reedy round ponds and lakes, muddy and stony along rivers) wherever it isn't paving
        // either.
        private static void Shore(Ground g, Owner o, Rect domain, bool river)
        {
            System.Random rng = MapStyle.Rng(o.Id, SaltRim);
            float rim = MapStyle.Range(rng, 1.0f, 2.4f), bank = river ? MapStyle.Range(rng, 2.0f, 3.5f) : MapStyle.Range(rng, 1.4f, 2.6f);
            bool reedy = !river && MapStyle.Unit(o.Id, SaltBank) < 0.75f;
            List<Vector2> p = o.Pts;
            int n = p.Count;
            var side = new int[n];   // per edge: 0 no shore (cut, or more water beyond), 1 rim only (paving beyond), 2 rim and bank
            for (int k = 0; k < n; k++) side[k] = Beside(g, p[k], p[(k + 1) % n], domain);
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
                    bool on = s < n && side[k] >= pass;
                    if (on)
                    {
                        if (chain.Count == 0) chain.Add(p[k]);
                        Densify(chain, p[(k + 1) % n], StreamStep);
                        continue;
                    }
                    if (chain.Count >= 2)
                    {
                        if (pass == 1) RimChain(g, chain, rim, domain, closed);
                        else BankChain(g, chain, bank, reedy, domain, closed);
                    }
                    chain.Clear();
                }
            }
        }

        // What lies beyond a stretch of shore (the right of the anticlockwise outline): 0 the domain's border or more
        // water, 1 paving or a lot, 2 land a bank belongs on.
        private static int Beside(Ground g, Vector2 a, Vector2 b, Rect domain)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 1e-3f || OnBorder(a, b, domain)) return 0;
            Vector2 outward = new Vector2(d.y, -d.x) / len;
            Vector2 q = (a + b) * 0.5f + outward * 1.5f;
            if (Mathf.Abs(q.x) > g.Half || Mathf.Abs(q.y) > g.Half) return 2;
            Owner there = g.OwnerAt(q);
            if (there == null) return 2;
            if (there.Kind == "water") return 0;
            return there.Tier == TierFeature ? 1 : 2;
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

        private static void RimChain(Ground g, List<Vector2> chain, float rim, Rect domain, bool closed)
        {
            var normals = new List<Vector2>(chain.Count);
            Normals(chain, normals);
            var inner = new float[chain.Count];
            for (int i = 0; i < chain.Count; i++)
                inner[i] = rim * (0.55f + 0.9f * WorldNoise(g.World(chain[i]), 7f, SaltRim)) * Taper(chain, i, domain, closed);
            Strip(g.Site.Town.Out, chain, normals, inner, null, DecalY, SnowSurf);   // left of the outline is the ice
        }

        private static void BankChain(Ground g, List<Vector2> chain, float bank, bool reedy, Rect domain, bool closed)
        {
            OutdoorBatches into = g.Site.Town.Out;
            var normals = new List<Vector2>(chain.Count);
            Normals(chain, normals);
            int n = chain.Count;
            var outer = new float[n];
            for (int i = 0; i < n; i++)
                outer[i] = bank * (0.6f + 0.8f * WorldNoise(g.World(chain[i]), 9f, SaltBank)) * Taper(chain, i, domain, closed);
            Strip(into, chain, normals, null, outer, DecalY, BankSurf);   // right of the outline is the land
            if (g.Lite || !reedy) return;
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

        #region Snow on the ice
        // Long patches of snow drifted on the ice, on a 10 m lattice fixed to the world, lying along the wind
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

        /// <summary>The distance from <paramref name="q"/> to the nearest mapped edge of a cut outline (the edges the cut
        /// made along the domain's border don't count; the far side is the next chunk's).</summary>
        private static float RealEdgeDistance(List<Vector2> p, Vector2 q, Rect domain)
        {
            float best = float.MaxValue;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
                if (!OnBorder(p[j], p[i], domain)) best = Mathf.Min(best, MapGeometry.DistanceToSegment(q, p[j], p[i]));
            return best;
        }
        #endregion

        #region Waterways
        // One piece of a waterway inside a domain: through water, a big river's open channel (as wide as the water
        // round it allows); elsewhere a stream, river, canal, ditch or drain of its own: a muddy bed, ice shelves and
        // a dark thread of open water, wobbling as streams do.
        private static void Waterway(Ground g, Line l, List<Vector2> piece, List<Owner> owners, OutdoorBatches into, bool inside)
        {
            if (piece.Count < 2) return;
            string kind = l.Data.kind ?? "";
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
            if (wet * 10 >= pts.Count * 6)
            {
                if (kind == "river" || kind == "canal") Channel(g, l, pts, water, into);
                return;
            }
            bool ditch = kind == "ditch" || kind == "drain";
            var normals = new List<Vector2>(pts.Count);
            Normals(pts, normals);
            int n = pts.Count;
            var bed = new float[n];
            var shelf = new float[n];
            var core = new float[n];
            float open = ditch ? 0.22f : width >= 8f ? 0.55f : 0.42f;
            for (int i = 0; i < n; i++)
            {
                Vector2 w = g.World(pts[i]);
                float wobble = 0.8f + 0.4f * WorldNoise(w, 6f, SaltStream);
                float halfW = water[i] != null ? 0f : width * 0.5f * wobble;   // where it runs into a pond, it ends at the ice
                bed[i] = halfW + (ditch ? 0.5f : 0.8f) * (0.7f + 0.6f * WorldNoise(w, 4f, SaltStream + 1));
                shelf[i] = halfW;
                float thread = WorldNoise(w, ditch ? 9f : 14f, SaltStream + 2);
                core[i] = halfW * open * Smooth(ditch ? 0.42f : 0.2f, ditch ? 0.62f : 0.45f, thread) * (0.75f + 0.5f * WorldNoise(w, 3f, SaltStream + 3));
            }
            Strip(into, pts, normals, bed, bed, BedY, BankSurf);
            Strip(into, pts, normals, shelf, shelf, StreamIceY, IceSurf);
            Strip(into, pts, normals, core, core, StreamWaterY, WaterSurf);
            if (inside) g.Site.TakeLine(piece, width * 0.5f + 0.6f);
        }

        private static Owner WaterAt(List<Owner> owners, Vector2 p)
        {
            for (int i = owners.Count - 1; i >= 0; i--)
            {
                Owner o = owners[i];
                if (!o.Bounds.Contains(p) || !Inside(o.Pts, p)) continue;
                return o.Kind == "water" ? o : null;
            }
            return null;
        }

        // A big river's open channel: dark water down the middle, as wide as about half the water at each point (the
        // rest is shelf ice), with a white lip of snow-covered shelf ice along each side.
        private static void Channel(Ground g, Line l, List<Vector2> pts, Owner[] water, OutdoorBatches into)
        {
            int n = pts.Count;
            var half = new float[n];
            for (int i = 0; i < n; i++)
                half[i] = water[i] != null ? RealEdgeDistance(water[i].Pts, pts[i], Domain(g, pts[i])) : 0f;
            // Smooth the widths along the river, so the channel doesn't jump at every bend of the bank.
            var smooth = new float[n];
            for (int i = 0; i < n; i++)
            {
                float sum = 0f, wsum = 0f;
                for (int k = -4; k <= 4; k++)
                {
                    int j = Mathf.Clamp(i + k, 0, n - 1);
                    float wk = 1f / (1f + Mathf.Abs(k));
                    sum += Mathf.Min(half[j], 400f) * wk;
                    wsum += wk;
                }
                smooth[i] = Mathf.Min(sum / wsum, half[i]);
            }
            var normals = new List<Vector2>(n);
            Normals(pts, normals);
            var lip = new float[n];
            var open = new float[n];
            float share = MapStyle.Range(MapStyle.Rng(l.Data.id, SaltChannel), 0.4f, 0.6f);
            for (int i = 0; i < n; i++)
            {
                Vector2 w = g.World(pts[i]);
                float h = Mathf.Clamp(smooth[i] * share, 0f, 120f) * (0.8f + 0.4f * WorldNoise(w, 18f, SaltChannel));
                float taper = Mathf.Min(1f, smooth[i] / 6f);
                open[i] = h * taper;
                lip[i] = open[i] > 0.05f ? open[i] + 0.5f + 0.6f * WorldNoise(w, 5f, SaltChannel + 1) : 0f;
            }
            Strip(into, pts, normals, lip, lip, StreamIceY, SnowSurf);
            Strip(into, pts, normals, open, open, StreamWaterY, WaterSurf);
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
            float a = Lattice(xi, yi, seed), b = Lattice(xi + 1, yi, seed), c = Lattice(xi, yi + 1, seed), d = Lattice(xi + 1, yi + 1, seed);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        private static float Lattice(int x, int y, int seed) => MapStyle.Unit(((long)x << 32) ^ (uint)y, seed);
        #endregion

        #region Reeds
        /// <summary>A clump of dead reeds and cattails: thin blades of straw and brown, leaning every way, a few with
        /// their cigar heads.</summary>
        private static void ReedClump(OutdoorBatches into, Vector2 at, System.Random rng, float size)
        {
            Vector3 c = To3(at, 0f);
            RaidMesh m = into.Solid(c);
            int blades = rng.Next(6, 11);
            for (int k = 0; k < blades; k++)
            {
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
