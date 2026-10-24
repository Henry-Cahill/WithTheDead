using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;
using static CloneZ.StreetscapePass;

namespace CloneZ
{
    /// <summary>
    /// Overhead power, the way rural and small-town Kentucky strings it: wooden poles along nearly every road on one side
    /// (a crossarm and three wires on the bigger roads, a single wire on the pole top on the small ones; a neutral, often a
    /// low-voltage run and a fat telephone cable lower down), pole transformers where homes cluster, service drops to the
    /// houses, guy wires where a line ends or turns; the mapped distribution lines on their own poles; and the mapped
    /// transmission lines on steel lattice towers or wooden H-frames striding across the country.
    /// <para>The lines along the roads are worked out per site from its own street pieces; where a street leaves a streamed
    /// piece, a pole stands exactly where its line crosses the edge, so the two pieces' lines meet on it. Mapped lines are
    /// followed through the region's data (every mapped vertex is a support): a piece draws the supports inside it and the
    /// spans that start at them, so a span crossing into the next piece is drawn once, whole. Where a line leaves the region's
    /// data, each side hangs its half of the span level at the boundary.</para>
    /// </summary>
    internal static class PowerPass
    {
        private const int SaltSide = 0x5E1D, SaltRear = 0x2EA2, SaltPole = 0x9013, SaltTransformer = 0x7F0A, SaltWood = 0x3D21,
            SaltPhone = 0x7E1E, SaltTower = 0x70E4, SaltMinor = 0x3141;
        private const float CornerTurn = 40f;   // degrees: a sharper bend gets a pole of its own
        private const float MaxDrop = 42f;   // the longest service drop
        private static readonly Color Conductor = new Color(0.43f, 0.45f, 0.48f);
        private static readonly Color GuyGrey = new Color(0.52f, 0.53f, 0.54f);
        private static readonly Color GuardYellow = new Color(0.86f, 0.70f, 0.10f);
        private static readonly Color InsulatorBrown = new Color(0.40f, 0.22f, 0.12f);
        private static readonly Color InsulatorGlass = new Color(0.50f, 0.66f, 0.60f);
        private static readonly Color TransformerGrey = new Color(0.50f, 0.54f, 0.56f);
        private static readonly Color TowerSteel = new Color(0.56f, 0.58f, 0.60f);

        #region Lines along the roads
        // A run of one road's pieces joined end to end, as its line follows them.
        private sealed class Chain
        {
            public readonly List<Vector2> C = new List<Vector2>();   // centreline
            public readonly List<Vector2> L = new List<Vector2>();   // the line's way, beside the road
            public readonly List<Street> Seg = new List<Street>();   // the street of each segment
            public readonly List<Vector2> SegDir = new List<Vector2>();   // each segment's way in its piece's own order
            public float[] Arc;
            public string Name;
            public float Side;
        }

        private struct Anchor
        {
            public float T;
            public bool Edge, Terminal, Corner;
        }

        /// <summary>Works out the lines along the roads: where each pole stands and what it carries, and hangs their
        /// wires. The poles are drawn later (<see cref="DrawPoles"/>), once the street lights have picked theirs.</summary>
        internal static void PlanStreetLines(Ctx ctx)
        {
            Site site = ctx.Site;
            var pieces = new List<Street>();
            foreach (Street s in site.Area.Streets)
                if (Carries(s) && !NearMappedLine(ctx, s)) pieces.Add(s);
            var crossings = new Dictionary<long, Crossing>(ctx.Crossings.Count);
            foreach (Crossing c in ctx.Crossings) crossings[SnapKey(c.At)] = c;
            int line = ctx.Poles.Count > 0 ? ctx.Poles[ctx.Poles.Count - 1].Line + 1 : 0;
            int firstPole = ctx.Poles.Count;
            foreach (Chain chain in Chains(ctx, pieces))
            {
                if (ctx.Density.CityCore(chain.C[chain.C.Count / 2])) continue;
                PlaceLine(ctx, chain, crossings, line++);
            }
            Taps(ctx, firstPole);
        }

        // A line that ends short of a junction (or where its road's pieces turn round) taps the nearest pole of another
        // line, across the road if need be, so the lines make one network.
        private static void Taps(Ctx ctx, int firstPole)
        {
            var joined = new HashSet<long>();
            for (int i = firstPole; i < ctx.Poles.Count; i++)
            {
                Pole t = ctx.Poles[i];
                if (!t.Terminal) continue;
                Pole best = null;
                int bestIndex = -1;
                float bestD = 34f;
                for (int j = firstPole; j < ctx.Poles.Count; j++)
                {
                    Pole q = ctx.Poles[j];
                    if (q.Line == t.Line) continue;
                    float d = Vector2.Distance(q.At, t.At);
                    if (d >= bestD || d < 3f) continue;
                    bestD = d;
                    best = q;
                    bestIndex = j;
                }
                if (best == null) continue;
                long pair = i < bestIndex ? ((long)i << 32) | (uint)bestIndex : ((long)bestIndex << 32) | (uint)i;
                if (!joined.Add(pair) || ctx.Blocks.Crosses(t.At, best.At, WirePad)) continue;
                // A tap carries one primary and the neutral (and the low-voltage run and telephone where both have them).
                Span(ctx, t, best, true);
            }
        }

        // The streets a line runs along: roads from residential up, but not motorways, divided highways, bridges or
        // tunnels; one residential street in six has its line behind the houses instead.
        private static bool Carries(Street s)
        {
            if (s.Data.tunnel || s.Data.bridge || s.Pts.Count < 2) return false;
            int rank = RoadRank(s);
            if (rank < 2 || s.Data.kind == "motorway" || s.Data.kind.EndsWith("_link", System.StringComparison.Ordinal)) return false;
            if (s.Data.oneway && rank >= 4 && (s.Data.lanes >= 2 || s.Data.kind == "trunk")) return false;
            if ((s.Data.kind == "residential" || s.Data.kind == "living_street") && MapStyle.Unit(NameId(s), SaltRear) < 0.17f) return false;
            return true;
        }

        // A mapped distribution line already runs along this street piece.
        private static bool NearMappedLine(Ctx ctx, Street s)
        {
            if (ctx.Site.Area.Power.Count == 0) return false;
            int near = 0, total = 0;
            foreach (Vector2 p in MapGeometry.SampleAlong(s.Pts, 12f))
            {
                total++;
                foreach (Line l in ctx.Site.Area.Power)
                {
                    if (l.Data.kind != "minor_line") continue;
                    if (Nearest(l.Pts, p, out _, out _) < 25f) { near++; break; }
                }
            }
            return total > 0 && near * 2 >= total;
        }

        // The pieces joined into runs: one road (by name) carried on through its pieces where one ends where the next
        // starts (both drawn the same way: the line keeps to one side of each piece's own direction, so a road whose pieces
        // run against each other gets two runs, joined by a span across it) and turns less than a right angle.
        private static List<Chain> Chains(Ctx ctx, List<Street> pieces)
        {
            var chains = new List<Chain>();
            var used = new bool[pieces.Count];
            var keys = new string[pieces.Count];
            // The pieces by where they start and where they end.
            var starts = new Dictionary<long, List<int>>(pieces.Count);
            var ends = new Dictionary<long, List<int>>(pieces.Count);
            for (int i = 0; i < pieces.Count; i++)
            {
                List<Vector2> pts = pieces[i].Pts;
                keys[i] = NameKey(pieces[i]);
                FileUnder(starts, SnapKey(pts[0]), i);
                FileUnder(ends, SnapKey(pts[pts.Count - 1]), i);
            }
            var run = new List<Street>();
            for (int i = 0; i < pieces.Count; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                run.Clear();
                run.Add(pieces[i]);
                string name = keys[i];
                // Forwards from the run's end, then backwards from its start.
                for (int dirn = 0; dirn < 2; dirn++)
                {
                    while (true)
                    {
                        Street end = dirn == 0 ? run[run.Count - 1] : run[0];
                        Vector2 tip = dirn == 0 ? end.Pts[end.Pts.Count - 1] : end.Pts[0];
                        Vector2 inDir = dirn == 0 ? EndDir(end) : StartDir(end);
                        int found = -1;
                        if ((dirn == 0 ? starts : ends).TryGetValue(SnapKey(tip), out List<int> near))
                            foreach (int j in near)
                            {
                                if (used[j] || keys[j] != name) continue;
                                Street s = pieces[j];
                                Vector2 joint = dirn == 0 ? s.Pts[0] : s.Pts[s.Pts.Count - 1];
                                if ((joint - tip).sqrMagnitude > 0.0025f) continue;
                                Vector2 outDir = dirn == 0 ? StartDir(s) : EndDir(s);
                                if (Vector2.Angle(inDir, outDir) > 80f) continue;
                                found = j;
                                break;
                            }
                        if (found < 0) break;
                        used[found] = true;
                        if (dirn == 0) run.Add(pieces[found]);
                        else run.Insert(0, pieces[found]);
                    }
                }
                chains.Add(MakeChain(ctx, run, name));
            }
            return chains;
        }

        private static void FileUnder(Dictionary<long, List<int>> map, long key, int i)
        {
            if (!map.TryGetValue(key, out List<int> list)) map[key] = list = new List<int>(2);
            list.Add(i);
        }

        private static Vector2 StartDir(Street s) => (s.Pts[1] - s.Pts[0]).normalized;

        private static Vector2 EndDir(Street s) => (s.Pts[s.Pts.Count - 1] - s.Pts[s.Pts.Count - 2]).normalized;

        private static Chain MakeChain(Ctx ctx, List<Street> run, string name)
        {
            var c = new Chain { Name = name };
            c.Side = MapStyle.Unit(NameId(run[0]), SaltSide) < 0.5f ? 1f : -1f;
            foreach (Street s in run)
            {
                List<Vector2> moved = MapGeometry.OffsetLine(s.Pts, c.Side * Roadside(s, (int)c.Side));
                // Where the piece meets the edge, a streamed piece's neighbour carries the line on from a pole they share:
                // its spot comes from the data alone, so both put it in the same place (a metre further out, clear of a
                // carriageway the roads widened).
                int last = s.Pts.Count - 1;
                float edgeOff = MappedHalf(s) + Verge(s) + 1f;
                if (OnEdge(ctx, s.Pts[0])) moved[0] = s.Pts[0] + Left(StartDir(s)) * (c.Side * edgeOff);
                if (OnEdge(ctx, s.Pts[last])) moved[last] = s.Pts[last] + Left(EndDir(s)) * (c.Side * edgeOff);
                for (int i = 0; i < s.Pts.Count; i++)
                {
                    Vector2 p = s.Pts[i], q = moved[i];
                    if (c.C.Count > 0 && (c.C[c.C.Count - 1] - p).sqrMagnitude < 0.0025f)
                    {
                        // The joint of two pieces: one point, halfway between their lines.
                        c.L[c.L.Count - 1] = (c.L[c.L.Count - 1] + q) * 0.5f;
                        continue;
                    }
                    if (c.C.Count > 0)
                    {
                        c.Seg.Add(s);
                        c.SegDir.Add((p - c.C[c.C.Count - 1]).normalized);
                    }
                    c.C.Add(p);
                    c.L.Add(q);
                }
            }
            c.Arc = new float[c.L.Count];
            for (int i = 1; i < c.L.Count; i++) c.Arc[i] = c.Arc[i - 1] + Vector2.Distance(c.L[i - 1], c.L[i]);
            return c;
        }

        private static long SnapKey(Vector2 p) => ((long)Mathf.RoundToInt(p.x * 20f) << 32) ^ (uint)Mathf.RoundToInt(p.y * 20f);

        private static void PlaceLine(Ctx ctx, Chain c, Dictionary<long, Crossing> crossings, int line)
        {
            int n = c.C.Count;
            if (n < 2 || c.Seg.Count == 0) return;
            float total = c.Arc[n - 1];
            Street main = c.Seg[0];
            int rank = RoadRank(main);
            float spacing = rank >= 3 ? 44f : main.Data.kind == "unclassified" ? 50f : 38f;
            if (total < 12f && !OnEdge(ctx, c.C[0]) && !OnEdge(ctx, c.C[n - 1])) return;

            // Where the crossing roads cut across the line, no pole: (from, to) along it.
            var zones = new List<Vector2>();
            var anchors = new List<Anchor>();
            for (int i = 0; i < n; i++)
            {
                if (!crossings.TryGetValue(SnapKey(c.C[i]), out Crossing x)) continue;
                float w = 0f;
                foreach (JunctionArm a in x.Arms)
                    if (NameKey(a.Street) != c.Name) w = Mathf.Max(w, HalfWidth(a.Street) + 2.6f);
                if (w <= 0f) continue;
                zones.Add(new Vector2(c.Arc[i] - w, c.Arc[i] + w));
            }

            // Its ends: a pole on the edge where the road leaves the site, else one short of where it ends.
            if (OnEdge(ctx, c.C[0])) anchors.Add(new Anchor { T = 0f, Edge = true });
            else anchors.Add(new Anchor { T = Mathf.Min(total, ZoneEnd(zones, 0f, 1.2f)), Terminal = true });
            if (OnEdge(ctx, c.C[n - 1])) anchors.Add(new Anchor { T = total, Edge = true });
            else anchors.Add(new Anchor { T = Mathf.Max(0f, ZoneStart(zones, total, total - 1.2f)), Terminal = true });
            // Its sharp bends.
            for (int i = 1; i + 1 < n; i++)
            {
                float turn = Vector2.Angle(c.C[i] - c.C[i - 1], c.C[i + 1] - c.C[i]);
                if (turn < CornerTurn) continue;
                float t = c.Arc[i];
                if (InZone(zones, t, out Vector2 z))
                {
                    if (z.x > 2f) anchors.Add(new Anchor { T = z.x, Corner = true });
                    if (z.y < total - 2f) anchors.Add(new Anchor { T = z.y, Corner = true });
                }
                else anchors.Add(new Anchor { T = t, Corner = true });
            }
            anchors.Sort((a, b) => a.T.CompareTo(b.T));
            if (anchors[anchors.Count - 1].T - anchors[0].T < 6f && !anchors[0].Edge && !anchors[anchors.Count - 1].Edge) return;

            var placed = new List<(float t, Anchor a)>();
            for (int k = 0; k < anchors.Count; k++)
            {
                Anchor a = anchors[k];
                // A pole where the line crosses the site's edge is shared with the neighbouring piece: dropped where it
                // would stand on a carriageway as the roads drew it, or in a building.
                if (a.Edge && !EdgeClear(ctx, PointAt(c, a.T, out _))) continue;
                if (placed.Count > 0 && a.T - placed[placed.Count - 1].t < 5f)
                {
                    // Two anchors this close are one pole (an edge pole wins).
                    if (a.Edge) placed[placed.Count - 1] = (a.T, a);
                    continue;
                }
                if (placed.Count > 0)
                {
                    float t0 = placed[placed.Count - 1].t, d = a.T - t0;
                    int spans = Mathf.Max(1, Mathf.CeilToInt(d / (spacing * 1.12f)));
                    for (int s = 1; s < spans; s++)
                    {
                        float t = t0 + d * s / spans;
                        if (InZone(zones, t, out Vector2 z))
                        {
                            t = t - z.x < z.y - t ? z.x : z.y;
                            if (t < t0 + 6f || t > a.T - 6f) continue;
                        }
                        if (TryFree(ctx, c, ref t, t0 + 5f, a.T - 5f, zones)) placed.Add((t, new Anchor { T = t }));
                    }
                }
                if (!a.Edge)
                {
                    float t = a.T;
                    float lo = placed.Count > 0 ? placed[placed.Count - 1].t + 4f : 0f;
                    if (!TryFree(ctx, c, ref t, lo, total, zones)) continue;
                    a.T = t;
                }
                placed.Add((a.T, a));
            }
            if (placed.Count < 2) return;
            // A span whose wires would stray from the road (round a bend) or pass over a building gets a pole between.
            for (int k = 0, guard = 0; k + 1 < placed.Count && guard < 64; guard++)
            {
                float t0 = placed[k].t, t1 = placed[k + 1].t;
                if (t1 - t0 >= 15f)
                {
                    Vector2 a = PointAt(c, t0, out _), b = PointAt(c, t1, out _);
                    if (Deviation(c, t0, t1, a, b) > 1.3f || ctx.Blocks.Crosses(a, b, WirePad))
                    {
                        float t = (t0 + t1) * 0.5f;
                        if (TryFree(ctx, c, ref t, t0 + 6.5f, t1 - 6.5f, zones))
                        {
                            placed.Insert(k + 1, (t, new Anchor { T = t }));
                            continue;
                        }
                    }
                }
                k++;
            }

            bool three = rank >= 3;
            bool secondary = main.Data.kind != "unclassified" || ctx.Density.Town(c.C[n / 2]);
            bool phone = MapStyle.Unit(NameId(main), SaltPhone) < 0.8f;
            int first = ctx.Poles.Count;
            Pole prev = null;
            foreach ((float t, Anchor a) in placed)
            {
                Vector2 at = PointAt(c, t, out int seg);
                Vector2 dir = c.SegDir[Mathf.Clamp(seg, 0, c.SegDir.Count - 1)];
                Vector2 across = Left(dir);
                if (a.Corner && seg + 2 < n && Mathf.Abs(t - c.Arc[seg + 1]) < 0.5f)
                {
                    // At the bend itself: square to the bisector.
                    Vector2 d0 = (c.L[seg + 1] - c.L[seg]).normalized, d1 = (c.L[seg + 2] - c.L[seg + 1]).normalized;
                    Vector2 bis = d0 + d1;
                    if (bis.sqrMagnitude > 1e-4f) across = Left(bis.normalized);
                }
                Street street = c.Seg[Mathf.Clamp(seg, 0, c.Seg.Count - 1)];
                long key = ctx.KeyOf(at);
                // A pole shared with a neighbouring piece is fitted from its own street's data, so both draw it alike.
                var pole = new Pole
                {
                    At = at, Across = across, Height = 9.3f + 1.3f * MapStyle.Unit(key, SaltPole),
                    ThreePhase = a.Edge ? RoadRank(street) >= 3 : three, Edge = a.Edge, Terminal = a.Terminal, Line = line,
                    Street = street, Key = key, Secondary = a.Edge ? street.Data.kind != "unclassified" : secondary,
                    Telephone = phone, ToRoad = (-c.Side * Left(dir)).normalized,
                };
                if (!a.Edge && secondary && ctx.Density.Homes(at, 35f) > 0 && MapStyle.Unit(key, SaltTransformer) < 0.4f
                    && (prev == null || !prev.Transformer)) pole.Transformer = true;
                ctx.Poles.Add(pole);
                if (ctx.Inside(at)) ctx.Site.Take(at, 0.45f);
                prev = pole;
            }
            // Guys where the line ends short (pulling away from the last span) and at its corners.
            for (int i = first; i < ctx.Poles.Count; i++)
            {
                Pole p = ctx.Poles[i];
                Pole before = i > first ? ctx.Poles[i - 1] : null, after = i + 1 < ctx.Poles.Count ? ctx.Poles[i + 1] : null;
                if (p.Terminal && (before == null) != (after == null))
                {
                    Pole other = before ?? after;
                    p.Guy = (p.At - other.At).normalized;
                }
                else if (before != null && after != null && Vector2.Angle(p.At - before.At, after.At - p.At) > CornerTurn)
                {
                    Vector2 pull = (before.At - p.At).normalized + (after.At - p.At).normalized;
                    if (pull.sqrMagnitude > 1e-4f) p.Guy = -pull.normalized;
                }
            }
            for (int i = first + 1; i < ctx.Poles.Count; i++)
            {
                Pole a = ctx.Poles[i - 1], b = ctx.Poles[i];
                // Still over a building (no room for a pole between): the line breaks there.
                if (!ctx.Blocks.Crosses(a.At, b.At, WirePad)) Span(ctx, a, b);
            }
        }

        /// <summary>How close a span's wires may pass a building's walls (its eaves reach out past them).</summary>
        private const float WirePad = 0.7f;

        private static bool OnEdge(Ctx ctx, Vector2 p) => Mathf.Abs(p.x) >= ctx.Reach - 0.02f || Mathf.Abs(p.y) >= ctx.Reach - 0.02f;

        // A pole on the site's edge: off the carriageways and out of the buildings. A streamed piece judges the carriageways
        // by the data alone, as its neighbour does, so the pole they share stands or falls in both.
        private static bool EdgeClear(Ctx ctx, Vector2 p)
            => ctx.Roads.Clearance(p, 2f, ctx.Site.Chunked) >= 0.4f && !ctx.Blocks.Contains(p, 0.3f);

        // How far the line strays from the straight wire between t0 (at a) and t1 (at b).
        private static float Deviation(Chain c, float t0, float t1, Vector2 a, Vector2 b)
        {
            float worst = 0f;
            for (int i = 0; i < c.L.Count; i++)
            {
                if (c.Arc[i] <= t0 || c.Arc[i] >= t1) continue;
                worst = Mathf.Max(worst, MapGeometry.DistanceToSegment(c.L[i], a, b));
            }
            return worst;
        }

        private static bool InZone(List<Vector2> zones, float t, out Vector2 zone)
        {
            foreach (Vector2 z in zones)
                if (t > z.x && t < z.y) { zone = z; return true; }
            zone = default;
            return false;
        }

        // Past the zone (if any) that covers t, or t + pad.
        private static float ZoneEnd(List<Vector2> zones, float t, float pad) => InZone(zones, t + 0.01f, out Vector2 z) ? z.y : t + pad;

        private static float ZoneStart(List<Vector2> zones, float t, float fallback) => InZone(zones, t - 0.01f, out Vector2 z) ? z.x : fallback;

        // A free spot for a pole at t along the line, or a step or three either way (within lo..hi, out of the zones): off
        // the roads and driveways, clear of what already stands there, out of the buildings.
        private static bool TryFree(Ctx ctx, Chain c, ref float t, float lo, float hi, List<Vector2> zones)
        {
            for (int k = 0; k < 7; k++)
            {
                float shift = (k + 1) / 2 * 1.6f * (k % 2 == 0 ? 1f : -1f);
                float tt = t + shift;
                if (tt < lo || tt > hi || InZone(zones, tt, out _)) continue;
                if (!ctx.Roadside(PointAt(c, tt, out _), 0.4f)) continue;
                t = tt;
                return true;
            }
            return false;
        }

        private static Vector2 PointAt(Chain c, float t, out int seg)
        {
            int n = c.L.Count;
            for (int i = 1; i < n; i++)
            {
                if (t > c.Arc[i] && i < n - 1) continue;
                float len = c.Arc[i] - c.Arc[i - 1];
                float k = len > 1e-4f ? Mathf.Clamp01((t - c.Arc[i - 1]) / len) : 0f;
                seg = i - 1;
                return Vector2.Lerp(c.L[i - 1], c.L[i], k);
            }
            seg = 0;
            return c.L[0];
        }
        #endregion

        #region Wires
        /// <summary>Where wire <paramref name="slot"/> leaves a pole: 0-2 the primaries (crossarm ends and pole top, or the
        /// pole top alone), 3 the neutral, 4 the low-voltage run, 5 the telephone cable.</summary>
        private static Vector3 Slot(Pole p, int slot, Vector2 across)
        {
            float h = p.Height;
            Vector2 at = p.At;
            switch (slot)
            {
                case 0: return p.ThreePhase ? To3(at - across * 1.05f, h - 0.24f) : To3(at, h + 0.22f);
                case 1: return To3(at + across * 1.05f, h - 0.24f);
                case 2: return To3(at, h + 0.22f);
                case 3: return To3(at + across * 0.16f, h - 1.35f);
                case 4: return To3(at - across * 0.16f, h - 2.25f);
                default: return To3(at + across * 0.2f, h - 3.3f);
            }
        }

        // The wires of one span between two poles of a line (a tap: one primary only).
        private static void Span(Ctx ctx, Pole a, Pole b, bool tap = false)
        {
            float len = Vector2.Distance(a.At, b.At);
            if (len < 2f || len > 95f) return;
            Vector2 acrossB = Vector2.Dot(a.Across, b.Across) < 0f ? -b.Across : b.Across;
            bool far = !ctx.Inside((a.At + b.At) * 0.5f);
            // Past a fixed map's edge only the wires seen from afar: the primaries and the neutral.
            bool ring = far && !ctx.Site.Chunked;
            float sagHigh = 0.15f + len * 0.012f, sagLow = 0.2f + len * 0.02f;
            if (!tap && a.ThreePhase && b.ThreePhase)
                for (int k = 0; k < 3; k++)
                    ctx.Wires.Add(Slot(a, k, a.Across), Slot(b, k, acrossB), sagHigh, 0.017f, Conductor, far);
            else ctx.Wires.Add(Slot(a, a.ThreePhase ? 2 : 0, a.Across), Slot(b, b.ThreePhase ? 2 : 0, acrossB), sagHigh, 0.017f, Conductor, far);
            ctx.Wires.Add(Slot(a, 3, a.Across), Slot(b, 3, acrossB), sagHigh + 0.1f, 0.015f, Conductor, far);
            if (ring) return;
            if (a.Secondary && b.Secondary)
                ctx.Wires.Add(Slot(a, 4, a.Across), Slot(b, 4, acrossB), sagLow, 0.022f, WireBlack, far);
            if (a.Telephone && b.Telephone)
                ctx.Wires.Add(Slot(a, 5, a.Across), Slot(b, 5, acrossB), sagLow + 0.15f, 0.03f, WireBlack, far);
        }

        /// <summary>Strings the drops from the poles to the buildings they feed: one each, from the nearest pole within
        /// reach, unless a building stands in the way; at most three from a pole. Downtown's shops are fed from the
        /// alleys behind them, out of sight. A drop comes down to a weatherhead on a short mast up the wall (through the
        /// eaves of a single storey; on a taller building, the upper storey's wall), clear of the roof.</summary>
        internal static void ServiceDrops(Ctx ctx)
        {
            if (ctx.Poles.Count == 0) return;
            Site site = ctx.Site;
            var drops = new Dictionary<Pole, int>();
            foreach (BuildingShell shell in site.Shells)
            {
                Footprint f = shell.Footprint;
                if (f == null || !ctx.Inside(f.Middle) || f.Size < 25f || shell.WallTop < 2f || !Powered(f.Data.kind)) continue;
                bool home = f.Data.kind == "house" || f.Data.kind == "trailer" || f.Data.kind == "apartments";
                if (!home && ctx.Density.Downtown(f.Middle)) continue;
                Pole best = null;
                float bestD = MaxDrop, bestScore = float.MaxValue;
                Vector2 attach = default;
                foreach (Pole p in ctx.Poles)
                {
                    if (p.Height > 15f || drops.TryGetValue(p, out int n) && n >= 3) continue;
                    if (Mathf.Abs(p.At.x - f.Middle.x) > MaxDrop + 30f || Mathf.Abs(p.At.y - f.Middle.y) > MaxDrop + 30f) continue;
                    Vector2 q = NearestOnOutline(f.Pts, p.At);
                    float d = Vector2.Distance(q, p.At);
                    if (d >= MaxDrop) continue;
                    // A pole on the house's own side of the street wins over one across it.
                    float score = d + (ctx.Roads.Clearance((q + p.At) * 0.5f, 1f) < 0f ? 14f : 0f);
                    if (score >= bestScore) continue;
                    bestScore = score;
                    bestD = d;
                    best = p;
                    attach = q;
                }
                if (best == null || bestD < 2f) continue;
                Vector2 outward = (best.At - attach).normalized;
                Vector2 end = attach + outward * 0.12f;
                // Over no other building, and not back across this one's own outline (an L-shaped house's wing).
                if (ctx.Blocks.Crosses(best.At, end, 0.35f, f) || BuildingGrid.Meets(f.Pts, best.At, end, 0f)) continue;
                drops[best] = drops.TryGetValue(best, out int had) ? had + 1 : 1;
                float top = Mathf.Clamp(shell.WallTop, 2.4f, 4.6f) + 0.75f;
                Vector2 side = Vector2.Dot(best.Across, outward) >= 0f ? best.Across : -best.Across;
                Vector3 pole = To3(best.At + side * 0.14f, best.Height - 2.3f);
                ctx.Wires.Add(pole, To3(end + outward * 0.05f, top - 0.08f), 0.3f + bestD * 0.01f, 0.02f, WireBlack, false);
                // The mast up the wall and its weatherhead, bent towards the pole.
                RaidMesh m = ctx.High(attach);
                Vector3 mast = To3(end, top - 0.55f);
                m.Box(mast, new Vector3(0.05f, 1.1f, 0.05f), DarkMetal);
                m.Box(To3(end + outward * 0.04f, top + 0.02f), new Vector3(0.11f, 0.07f, 0.11f), DarkMetal);
                m.Box(To3(end - outward * 0.05f, top - 0.95f), new Vector3(0.08f, 0.05f, 0.1f), DarkMetal);
            }
        }

        private static bool Powered(string kind)
        {
            switch (kind)
            {
                case "garage": case "shed": case "canopy": case "ruin": case "silo": case "greenhouse": case "barn": return false;
                default: return true;
            }
        }

        private static Vector2 NearestOnOutline(List<Vector2> pts, Vector2 p)
        {
            float best = float.MaxValue;
            Vector2 on = pts[0];
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i], ab = pts[(i + 1) % pts.Count] - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-6f ? Mathf.Clamp(Vector2.Dot(p - a, ab) / len2, 0.15f, 0.85f) : 0f;
                Vector2 q = a + ab * t;
                float d = (q - p).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                on = q;
            }
            return on;
        }

        #endregion

        #region Drawing the poles
        /// <summary>Draws the poles of the lines along the roads (and the mapped distribution lines'), with their
        /// fittings, transformers, guys and street lights.</summary>
        internal static void DrawPoles(Ctx ctx)
        {
            foreach (Pole p in ctx.Poles)
                if (p.Height < 15f) DrawPole(ctx, p);
        }

        private static void DrawPole(Ctx ctx, Pole p)
        {
            bool inside = ctx.Inside(p.At);
            // Past a fixed map's edge the poles are scenery, seen from afar: a plain pole and its crossarm.
            bool ring = !inside && !ctx.Site.Chunked;
            Surf wood = Matte(PoleWoods[MapStyle.Pick(p.Key, SaltWood, PoleWoods.Length)]);
            float h = p.Height;
            Vector3 foot = To3(p.At, 0f);
            ctx.Low(p.At).Cylinder(foot, 0.14f, h, ring ? 5 : 7, wood, Quaternion.identity, 0.72f);
            bool burning = p.Lamp != null && p.Lamp.Lit && inside;
            if (!burning)
            {
                if (inside) ctx.Collider(p.At, 0f, new Vector3(0.3f, 4f, 0.3f), 0f);
                else if (OwnsEdge(ctx, p.At)) ctx.Site.Town.PropCollider(To3(p.At, 2f), new Vector3(0.3f, 4f, 0.3f), 0f);
            }

            RaidMesh m = ctx.High(p.At);
            m.SetFrame(foot, Yaw(Left(p.Across)));
            // Local frame: x across the line, z along it, y up.
            if (ring)
            {
                if (p.ThreePhase) m.Box(new Vector3(0f, h - 0.45f, 0f), new Vector3(2.44f, 0.1f, 0.09f), wood);
                m.ResetFrame();
                return;
            }
            Surf insulator = Gloss(MapStyle.Unit(p.Key, SaltWood + 1) < 0.5f ? InsulatorBrown : InsulatorGlass);
            if (p.ThreePhase)
            {
                m.Box(new Vector3(0f, h - 0.45f, 0f), new Vector3(2.44f, 0.1f, 0.09f), wood);
                m.Box(new Vector3(0f, h - 0.39f, 0f), new Vector3(2.36f, 0.03f, 0.085f), SnowSurf);
                m.Rod(new Vector3(-0.6f, h - 0.48f, 0f), new Vector3(0f, h - 1.05f, 0f), 0.035f, DarkMetal);
                m.Rod(new Vector3(0.6f, h - 0.48f, 0f), new Vector3(0f, h - 1.05f, 0f), 0.035f, DarkMetal);
                for (int s = -1; s <= 1; s += 2)
                    m.Cylinder(new Vector3(s * 1.05f, h - 0.40f, 0f), 0.05f, 0.15f, 6, insulator, Quaternion.identity, 0.8f);
            }
            m.Cylinder(new Vector3(0f, h - 0.02f, 0f), 0.05f, 0.2f, 6, insulator, Quaternion.identity, 0.8f);
            m.Box(new Vector3(0f, h - 0.12f, 0.07f), new Vector3(0.05f, 0.2f, 0.05f), DarkMetal);
            // The neutral's bracket, the low-voltage rack and the telephone cable's bolt.
            m.Box(new Vector3(0.1f, h - 1.35f, 0f), new Vector3(0.14f, 0.05f, 0.05f), DarkMetal);
            m.Cylinder(new Vector3(0.16f, h - 1.42f, 0f), 0.035f, 0.1f, 5, insulator);
            if (p.Secondary)
            {
                m.Box(new Vector3(-0.12f, h - 2.25f, 0f), new Vector3(0.05f, 0.45f, 0.06f), DarkMetal);
                m.Cylinder(new Vector3(-0.16f, h - 2.32f, 0f), 0.04f, 0.12f, 5, Matte(new Color(0.85f, 0.84f, 0.80f)));
            }
            if (p.Telephone) m.Box(new Vector3(0.14f, h - 3.3f, 0f), new Vector3(0.12f, 0.1f, 0.1f), Matte(WireBlack));

            Vector2 toRoad = p.ToRoad.sqrMagnitude > 0.5f ? p.ToRoad : p.Across;
            var road = new Vector3(Vector2.Dot(toRoad, p.Across), 0f, Vector2.Dot(toRoad, Left(p.Across)));
            if (p.Transformer)
            {
                // A pole can on the field side, its lid snowed over, a jumper down from the line.
                Vector3 can = -road * 0.36f + new Vector3(0f, h - 2.95f, 0f);
                Surf grey = Gloss(TransformerGrey);
                m.Cylinder(can, 0.24f, 0.82f, 10, grey);
                m.Cylinder(can + new Vector3(0f, 0.82f, 0f), 0.26f, 0.05f, 10, grey);
                m.Cylinder(can + new Vector3(0f, 0.87f, 0f), 0.24f, 0.04f, 10, SnowSurf);
                m.Box(can + road * 0.2f + new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.5f, 0.08f), DarkMetal);
                m.Cylinder(can + new Vector3(0.08f, 0.87f, 0f), 0.025f, 0.14f, 5, insulator);
                Vector3 top = m.ToMeshSpace(can + new Vector3(0.08f, 1.01f, 0f));
                Vector3 line = m.ToMeshSpace(p.ThreePhase ? new Vector3(-1.05f, h - 0.3f, 0f) : new Vector3(0f, h + 0.1f, 0f));
                ctx.Wires.Add(line, top, 0.15f, 0.008f, Conductor, !inside);
            }
            if (p.Lamp != null)
            {
                Vector3 root = road * 0.14f + new Vector3(0f, h - 2.05f, 0f);
                Vector3 tip = road * 1.95f + new Vector3(0f, h - 1.8f, 0f);
                m.Rod(root, tip, 0.05f, Galvanised);
                m.Rod(road * 0.14f + new Vector3(0f, h - 2.55f, 0f), road * 0.9f + new Vector3(0f, h - 1.95f, 0f), 0.03f, Galvanised);
                Vector3 head = m.ToMeshSpace(tip);
                m.ResetFrame();
                CobraHead(ctx, p.Lamp, head, toRoad, p.Height < 15f ? 0.30f : 0.36f);
                if (burning) ctx.Burning.Add(new Burning { Lamp = p.Lamp, Foot = p.At, PoleHeight = h, Head = head });
            }
            m.ResetFrame();
            if (p.Guy.sqrMagnitude > 0.5f) Guy(ctx, p, inside);
        }

        /// <summary>True for a spot on a streamed piece's west or south edge: the one of the two pieces sharing a pole
        /// there that gives it its collider.</summary>
        internal static bool OwnsEdge(Ctx ctx, Vector2 p)
            => ctx.Site.Chunked && Mathf.Abs(p.x) <= ctx.Half + 0.05f && Mathf.Abs(p.y) <= ctx.Half + 0.05f
                && (p.x <= -ctx.Half + 0.05f || p.y <= -ctx.Half + 0.05f);

        // A guy wire from near the pole's top to an anchor in the ground behind it; in town, a yellow guard round its foot.
        private static void Guy(Ctx ctx, Pole p, bool inside)
        {
            Vector2 ground = p.At + p.Guy * (p.Height * 0.42f);
            if (!ctx.Roadside(ground, 0.3f))
            {
                ground = p.At + p.Guy * (p.Height * 0.28f);
                if (!ctx.Roadside(ground, 0.3f)) return;
            }
            Vector3 top = To3(p.At, p.Height - 0.7f), anchor = To3(ground, 0.02f);
            ctx.Wires.Add(top, anchor, 0.02f, 0.01f, GuyGrey, !inside);
            RaidMesh m = ctx.Low(ground);
            m.Box(anchor + new Vector3(0f, 0.03f, 0f), new Vector3(0.18f, 0.06f, 0.18f), Concrete);
            if (ctx.Density.Town(p.At))
                m.Rod(anchor, Vector3.Lerp(anchor, top, 2.3f / Vector3.Distance(anchor, top)), 0.06f, Gloss(GuardYellow));
            if (ctx.Inside(ground)) ctx.Site.Take(ground, 0.3f);
        }
        #endregion

        #region Mapped lines
        /// <summary>The mapped power lines: distribution lines on wooden poles (full detail only) and transmission lines on
        /// towers, with their wires.</summary>
        internal static void MappedLines(Ctx ctx)
        {
            Site site = ctx.Site;
            if (site.Area.Power.Count == 0) return;
            var done = new HashSet<MapLine>();
            int regionDm = site.Build.RegionSize * 10;
            int line = ctx.Poles.Count > 0 ? ctx.Poles[ctx.Poles.Count - 1].Line + 1 : 0;
            foreach (Line l in site.Area.Power)
            {
                if (!done.Add(l.Data) || l.Data.pts == null || l.Data.pts.Length < 4) continue;
                bool minor = l.Data.kind == "minor_line";
                if (minor && !ctx.Full) continue;
                if (!Locate(site.Area, l.Data, out double ox, out double oz)) continue;
                int count = l.Data.pts.Length / 2;
                var at = new Vector2[count];
                var inRegion = new bool[count];
                for (int j = 0; j < count; j++)
                {
                    int x = l.Data.pts[2 * j], z = l.Data.pts[2 * j + 1];
                    at[j] = new Vector2((float)(ox + x * 0.1), (float)(oz + z * 0.1));
                    inRegion[j] = x >= 0 && z >= 0 && x < regionDm && z < regionDm;
                }
                Tower look = minor ? Tower.Wood : TowerOf(l.Data, at);
                var supports = new Pole[count];
                for (int j = 0; j < count; j++)
                {
                    if (!inRegion[j] || Mathf.Abs(at[j].x) >= ctx.Reach - 0.01f || Mathf.Abs(at[j].y) >= ctx.Reach - 0.01f) continue;
                    supports[j] = Support(ctx, l.Data, at, inRegion, j, look, line);
                }
                for (int j = 0; j < count; j++)
                {
                    if (supports[j] == null) continue;
                    if (j + 1 < count)
                    {
                        if (inRegion[j + 1]) MappedSpan(ctx, supports[j], Support(ctx, l.Data, at, inRegion, j + 1, look, line, false), look);
                        else HalfSpan(ctx, supports[j], at[j], at[j + 1], regionDm, ox, oz, look);
                    }
                    if (j > 0 && !inRegion[j - 1]) HalfSpan(ctx, supports[j], at[j], at[j - 1], regionDm, ox, oz, look);
                }
                line++;
            }
        }

        private enum Tower { Wood, HFrame, Lattice }

        private static Tower TowerOf(MapLine data, Vector2[] at)
        {
            var spans = new List<float>(at.Length);
            for (int j = 1; j < at.Length; j++) spans.Add(Vector2.Distance(at[j - 1], at[j]));
            spans.Sort();
            float median = spans.Count > 0 ? spans[spans.Count / 2] : 150f;
            if (median < 125f || MapStyle.Unit(data.id, SaltTower) < 0.15f) return Tower.HFrame;
            return Tower.Lattice;
        }

        // Where a mapped line's region data sits in the site: from one of its pieces, whose inner points are the line's own
        // vertices (cutting adds points only at a piece's ends).
        private static bool Locate(Area area, MapLine data, out double ox, out double oz)
        {
            ox = oz = 0;
            int[] v = data.pts;
            int count = v.Length / 2;
            foreach (Line piece in area.Power)
            {
                if (piece.Data != data) continue;
                List<Vector2> p = piece.Pts;
                for (int k = 1; k + 1 < p.Count; k++)
                    for (int j = 0; j < count; j++)
                    {
                        double cx = p[k].x - v[2 * j] * 0.1, cz = p[k].y - v[2 * j + 1] * 0.1;
                        if (Fits(p, k + 1, v, j, j + 1, cx, cz) && Fits(p, k - 1, v, j, j - 1, cx, cz))
                        {
                            ox = cx;
                            oz = cz;
                            return true;
                        }
                    }
                if (p.Count == 2)
                {
                    // No inner point: it may still end at one of the line's own ends.
                    for (int e = 0; e < 2; e++)
                    {
                        int j = e == 0 ? 0 : count - 1, k = e == 0 ? 0 : 1;
                        double cx = p[k].x - v[2 * j] * 0.1, cz = p[k].y - v[2 * j + 1] * 0.1;
                        if (Fits(p, 1 - k, v, j, e == 0 ? 1 : count - 2, cx, cz))
                        {
                            ox = cx;
                            oz = cz;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Point k of a piece lies on the line's segment from vertex j towards vertex jn (or on vertex jn itself).
        private static bool Fits(List<Vector2> p, int k, int[] v, int j, int jn, double ox, double oz)
        {
            int count = v.Length / 2;
            if (k < 0 || k >= p.Count || jn < 0 || jn >= count) return false;
            var a = new Vector2((float)(ox + v[2 * j] * 0.1), (float)(oz + v[2 * j + 1] * 0.1));
            var b = new Vector2((float)(ox + v[2 * jn] * 0.1), (float)(oz + v[2 * jn + 1] * 0.1));
            return MapGeometry.DistanceToSegment(p[k], a, b) < 0.06f && Vector2.Distance(p[k], a) > 0.01f;
        }

        // The support at vertex j: square to the bend's bisector there. A wooden pole the mapping put on a carriageway as the
        // roads drew it (mapping is good to a metre or two) steps off it sideways, when every span it carries is this site's
        // to draw (so a neighbouring piece's span still ends on it).
        private static Pole Support(Ctx ctx, MapLine data, Vector2[] at, bool[] inRegion, int j, Tower look, int line, bool draw = true)
        {
            Vector2 dir = Vector2.zero;
            if (j > 0) dir += (at[j] - at[j - 1]).normalized;
            if (j + 1 < at.Length) dir += (at[j + 1] - at[j]).normalized;
            if (dir.sqrMagnitude < 1e-4f) dir = j + 1 < at.Length ? at[j + 1] - at[j] : at[j] - at[j - 1];
            dir.Normalize();
            long key = MapStyle.Hash(data.id, j);
            var p = new Pole
            {
                At = at[j], Across = Left(dir), Line = line, Key = key, Secondary = false, Telephone = look == Tower.Wood,
                ThreePhase = look == Tower.Wood && MapStyle.Unit(data.id, SaltMinor) < 0.45f,
                Height = look == Tower.Wood ? 9.6f + 1.2f * MapStyle.Unit(key, SaltPole) : look == Tower.HFrame ? 15f : 27f,
            };
            if (look == Tower.Wood && ctx.Inside(at[j]) && (j == 0 || ctx.Inside(at[j - 1])) && (j + 1 >= at.Length || ctx.Inside(at[j + 1]))
                && ctx.Roads.Clearance(at[j], 2f) < 0.4f)
                for (int k = 1; k <= 6; k++)
                {
                    Vector2 q = at[j] + p.Across * ((k + 1) / 2 * (k % 2 == 1 ? 1f : -1f));
                    if (ctx.Roads.Clearance(q, 2f) < 0.4f || ctx.Blocks.Contains(q, 0.3f)) continue;
                    p.At = q;
                    break;
                }
            if (!draw) return p;
            bool terminal = j == 0 || j == at.Length - 1;
            if (look == Tower.Wood)
            {
                if (terminal) p.Guy = j == 0 ? (at[0] - at[1]).normalized : (at[j] - at[j - 1]).normalized;
                ctx.Poles.Add(p);
            }
            else DrawTower(ctx, p, look);
            if (ctx.Inside(p.At)) ctx.Site.Take(p.At, look == Tower.Lattice ? 3.6f : look == Tower.HFrame ? 2.8f : 0.45f);
            return p;
        }

        private static void MappedSpan(Ctx ctx, Pole a, Pole b, Tower look)
        {
            if (look == Tower.Wood)
            {
                if (!ctx.Blocks.Crosses(a.At, b.At, 0.3f)) Span(ctx, a, b);
                return;
            }
            if (!ctx.Full) return;
            float len = Vector2.Distance(a.At, b.At);
            if (len < 5f) return;
            Vector2 acrossB = Vector2.Dot(a.Across, b.Across) < 0f ? -b.Across : b.Across;
            bool far = !ctx.Inside((a.At + b.At) * 0.5f);
            float sag = 1.2f + len * 0.028f;
            for (int k = 0; k < 3; k++)
                ctx.Wires.Add(TowerSlot(a, k, a.Across, look), TowerSlot(b, k, acrossB, look), sag, 0.026f, Conductor, far);
            for (int k = 3; k < 5; k++)
                ctx.Wires.Add(TowerSlot(a, k, a.Across, look), TowerSlot(b, k, acrossB, look), sag * 0.85f, 0.011f, GuyGrey, far);
        }

        // Half a span, from a support to where the line leaves the region's data, levelling out there at a height both
        // regions agree on.
        private static void HalfSpan(Ctx ctx, Pole a, Vector2 from, Vector2 to, int regionDm, double ox, double oz, Tower look)
        {
            if (!ctx.Full) return;
            // The region's edge between the two vertices, in site metres.
            float x0 = (float)ox, z0 = (float)oz, x1 = (float)(ox + regionDm * 0.1), z1 = (float)(oz + regionDm * 0.1);
            float t = 1f;
            Vector2 d = to - from;
            if (to.x < x0 && d.x < 0f) t = Mathf.Min(t, (x0 - from.x) / d.x);
            if (to.x >= x1 && d.x > 0f) t = Mathf.Min(t, (x1 - from.x) / d.x);
            if (to.y < z0 && d.y < 0f) t = Mathf.Min(t, (z0 - from.y) / d.y);
            if (to.y >= z1 && d.y > 0f) t = Mathf.Min(t, (z1 - from.y) / d.y);
            Vector2 edge = from + d * Mathf.Clamp01(t);
            var b = new Pole { At = edge, Across = a.Across, Height = a.Height, ThreePhase = a.ThreePhase, Telephone = a.Telephone };
            bool far = !ctx.Inside((from + edge) * 0.5f);
            int wires = look == Tower.Wood ? (a.ThreePhase ? 4 : 2) : 5;
            for (int k = 0; k < wires; k++)
            {
                Vector3 p, q;
                if (look == Tower.Wood)
                {
                    int slot = a.ThreePhase ? (k < 3 ? k : 3) : (k == 0 ? 0 : 3);
                    p = Slot(a, slot, a.Across);
                    q = Slot(b, slot, a.Across);
                    q.y -= 0.6f;
                }
                else
                {
                    p = TowerSlot(a, k, a.Across, look);
                    q = TowerSlot(b, k, a.Across, look);
                    q.y -= 4f;
                }
                ctx.Wires.AddHalf(p, q, k < 3 ? 0.026f : 0.012f, k < 3 ? Conductor : GuyGrey, far);
            }
        }

        /// <summary>Where a tower's wire <paramref name="slot"/> hangs: 0-2 the phases at the insulator strings' feet, 3-4
        /// the shield wires on the peaks.</summary>
        private static Vector3 TowerSlot(Pole p, int slot, Vector2 across, Tower look)
        {
            if (look == Tower.HFrame)
            {
                if (slot < 3) return To3(p.At + across * ((slot - 1) * 3.2f), 11.0f);
                return To3(p.At + across * (slot == 3 ? -2.4f : 2.4f), 15.2f);
            }
            if (slot < 3) return To3(p.At + across * ((slot - 1) * 5.8f), 17.3f);
            return To3(p.At + across * (slot == 3 ? -2.8f : 2.8f), 26.6f);
        }

        // A transmission tower: a tapering steel lattice with a wide crossarm and two peaks for the shield wires, or a
        // wooden H-frame; its legs (and their colliders) on the ground, the rest above.
        private static void DrawTower(Ctx ctx, Pole p, Tower look)
        {
            bool inside = ctx.Inside(p.At);
            Vector3 foot = To3(p.At, 0f);
            float yaw = Yaw(Left(p.Across));
            RaidMesh low = ctx.Low(p.At), high = ctx.High(p.At);
            low.SetFrame(foot, yaw);
            high.SetFrame(foot, yaw);
            if (look == Tower.HFrame)
            {
                Surf wood = Matte(PoleWoods[MapStyle.Pick(p.Key, SaltWood, PoleWoods.Length)]);
                for (int s = -1; s <= 1; s += 2)
                {
                    low.Cylinder(new Vector3(s * 2.4f, 0f, 0f), 0.2f, 15.2f, 7, wood, Quaternion.identity, 0.7f);
                    if (inside) ctx.Site.Town.PropCollider(To3(p.At + p.Across * (s * 2.4f), 2f), new Vector3(0.4f, 4f, 0.4f), 0f);
                }
                high.Box(new Vector3(0f, 12.6f, 0f), new Vector3(7.6f, 0.26f, 0.24f), wood);
                high.Box(new Vector3(0f, 12.76f, 0f), new Vector3(7.4f, 0.05f, 0.22f), SnowSurf);
                if (ctx.Full)
                {
                    high.Rod(new Vector3(-2.3f, 12.4f, 0f), new Vector3(2.3f, 8.6f, 0f), 0.12f, wood);
                    high.Rod(new Vector3(2.3f, 12.4f, 0f), new Vector3(-2.3f, 8.6f, 0f), 0.12f, wood);
                    for (int k = 0; k < 3; k++)
                        high.Cylinder(new Vector3((k - 1) * 3.2f, 11.0f, 0f), 0.1f, 1.5f, 6, Gloss(InsulatorBrown), Quaternion.identity, 0.9f);
                }
                low.ResetFrame();
                high.ResetFrame();
                return;
            }
            Surf steel = Metal(TowerSteel);
            const float baseHalf = 3.4f, waist = 0.9f, waistY = 19.5f, topHalf = 0.65f, topY = 25f;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    low.Rod(new Vector3(sx * baseHalf, 0f, sz * baseHalf), new Vector3(sx * waist, waistY, sz * waist), 0.2f, steel);
                    low.Box(new Vector3(sx * baseHalf, 0.15f, sz * baseHalf), new Vector3(0.6f, 0.3f, 0.6f), Concrete);
                    if (inside)
                        ctx.Site.Town.PropCollider(foot + Quaternion.Euler(0f, yaw, 0f) * new Vector3(sx * baseHalf, 1.5f, sz * baseHalf),
                            new Vector3(0.5f, 3f, 0.5f), 0f);
                    high.Rod(new Vector3(sx * waist, waistY, sz * waist), new Vector3(sx * topHalf, topY, sz * topHalf), 0.14f, steel);
                }
            if (ctx.Full)
            {
                // Girts and cross-bracing, panel by panel, on all four faces.
                const int panels = 5;
                for (int k = 0; k < panels; k++)
                {
                    float y0 = waistY * k / panels, y1 = waistY * (k + 1) / panels;
                    float w0 = Mathf.Lerp(baseHalf, waist, y0 / waistY), w1 = Mathf.Lerp(baseHalf, waist, y1 / waistY);
                    for (int face = 0; face < 4; face++)
                    {
                        Quaternion q = Quaternion.Euler(0f, face * 90f, 0f);
                        Vector3 a0 = q * new Vector3(-w0, y0, w0), b0 = q * new Vector3(w0, y0, w0);
                        Vector3 a1 = q * new Vector3(-w1, y1, w1), b1 = q * new Vector3(w1, y1, w1);
                        high.Rod(a0, b1, 0.07f, steel);
                        high.Rod(b0, a1, 0.07f, steel);
                        if (k > 0) high.Rod(a0, b0, 0.06f, steel);
                    }
                }
            }
            // The crossarm: two chords each side, braced to the body, insulator strings hanging from its ends and middle.
            float armY = waistY + 0.2f;
            for (int sz = -1; sz <= 1; sz += 2)
            {
                high.Rod(new Vector3(-6.4f, armY, sz * 0.45f), new Vector3(6.4f, armY, sz * 0.45f), 0.14f, steel);
                high.Rod(new Vector3(-6.4f, armY, sz * 0.05f), new Vector3(-waist, armY + 1.6f, sz * waist), 0.1f, steel);
                high.Rod(new Vector3(6.4f, armY, sz * 0.05f), new Vector3(waist, armY + 1.6f, sz * waist), 0.1f, steel);
            }
            for (int s = -1; s <= 1; s += 2)
                high.Rod(new Vector3(s * topHalf, topY, 0f), new Vector3(s * 2.8f, topY + 1.6f, 0f), 0.12f, steel);
            if (ctx.Full)
                for (int k = 0; k < 3; k++)
                    high.Cylinder(new Vector3((k - 1) * 5.8f, armY - 2.2f, 0f), 0.13f, 2.2f, 6, Gloss(InsulatorGlass), Quaternion.identity, 1f);
            low.ResetFrame();
            high.ResetFrame();
        }
        #endregion

        #region Shared with the street lights
        /// <summary>A cobra-head street light at the end of an arm (<paramref name="end"/>, world), reaching along
        /// <paramref name="reach"/>: an aluminium housing with a rounded nose, a dark refractor under it (a burning one
        /// gets its glowing lens when it's made a <see cref="StreetLamp"/>) and snow on top.</summary>
        internal static void CobraHead(Ctx ctx, LampPlan lamp, Vector3 end, Vector2 reach, float size)
        {
            Vector2 at = new Vector2(end.x, end.z);
            RaidMesh m = ctx.High(at);
            m.SetFrame(end, Yaw(reach));
            Color body = MapStyle.Unit(lamp.Key, 0x60B) < 0.7f ? new Color(0.62f, 0.64f, 0.66f) : new Color(0.30f, 0.30f, 0.27f);
            float l = size * 2.1f, w = size, hgt = size * 0.45f;
            Quaternion tilt = Quaternion.Euler(-5f, 0f, 0f);
            m.Box(new Vector3(0f, 0f, l * 0.35f), new Vector3(w, hgt, l * 0.9f), tilt, Gloss(body));
            m.Ellipsoid(new Vector3(0f, 0f, l * 0.82f), new Vector3(w, hgt, l * 0.32f), tilt, Gloss(body), 8, 4);
            m.Box(new Vector3(0f, hgt * 0.55f, l * 0.4f), new Vector3(w * 0.92f, 0.03f, l * 0.85f), tilt, SnowSurf);
            if (!lamp.Lit)
                m.Box(new Vector3(0f, -hgt * 0.55f, l * 0.48f), new Vector3(w * 0.8f, hgt * 0.35f, l * 0.6f), tilt,
                    DecorPalette.Glass(new Color(0.30f, 0.30f, 0.28f)));
            m.ResetFrame();
        }
        #endregion
    }
}
