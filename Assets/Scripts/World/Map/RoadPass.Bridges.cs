using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Road bridges: concrete parapets with snow on their caps along both edges of the deck (solid: they block), heavier
    // posts at their ends, W-beam guard rail flaring away from each end along the approach, ploughed snow banked against
    // the parapets; and concrete median barriers between a divided highway's carriageways where they run close.
    internal static partial class RoadPass
    {
        /// <summary>Heights added along a chain (a raised deck), by arc length; linear between its points.</summary>
        private sealed class Profile
        {
            private readonly float[] _s, _y;

            public Profile(float[] s, float[] y)
            {
                _s = s;
                _y = y;
            }

            public float At(float s)
            {
                if (_s.Length == 0) return 0f;
                if (s <= _s[0]) return _y[0];
                for (int i = 1; i < _s.Length; i++)
                    if (s <= _s[i]) return Mathf.Lerp(_y[i - 1], _y[i], (s - _s[i - 1]) / Mathf.Max(1e-4f, _s[i] - _s[i - 1]));
                return _y[_y.Length - 1];
            }
        }

        private const float ParapetHeight = 0.82f, ParapetThick = 0.34f;
        private static readonly List<Vector2> s_Under = new List<Vector2>(8), s_Pieces = new List<Vector2>(8);

        private static void Bridges(Net net)
        {
            foreach (Chain c in net.Chains)
            {
                if (!c.W.Bridge || c.To - c.From < 1f) continue;
                Underpasses(net, c, c, null, s_Under);
                for (int side = -1; side <= 1; side += 2) Parapet(net, c, side, s_Under);
                if (!net.Full) continue;
                GuardRail(net, c, true);
                GuardRail(net, c, false);
            }
            if (net.Full) Medians(net);
        }

        // One parapet: a concrete wall just outside the carriageway's edge with a snowy cap, a post at each end, a
        // collider along it; on the deck, snow the plough pushed against it. Open where a side road joins the bridge, and
        // over whatever passes beneath (the map is flat: a road, path or railway under the bridge crosses at grade, so a
        // wall there would block it), with a post either side of the gap.
        /// <summary>Builds the concrete parapet walls, caps, posts and associated snow on a bridge deck.</summary>
        private static void Parapet(Net net, Chain c, int side, List<Vector2> under)
        {
            Look look = net.Look;
            ProfileSamples(c, c.From, c.To, 0f, net.Samples2);
            List<Sample> s = net.Samples2;
            // Iterate over each segment between samples to generate parapet sections, skipping those that intersect a mouth opening.
            for (int i = 0; i + 1 < s.Count; i++)
            {
                if (InMouth(c, side, (s[i].S + s[i + 1].S) * 0.5f)) continue;
                // Determine the stretch of bridge not overlapped by any underpass, storing pieces for parapet generation.
                Outside(s[i].S, s[i + 1].S, under, s_Pieces);
                foreach (Vector2 piece in s_Pieces)
                {
                    // For each non‑overlapping piece, compute its start and end points on the parapet line.
                    Vector2 a = ParapetPoint(c, side, piece.x, s[i], s[i + 1]), b = ParapetPoint(c, side, piece.y, s[i], s[i + 1]);
                    Vector2 dir = b - a;
                    float len = dir.magnitude;
                    // Skip tiny fragments that would produce invisible geometry.
                    if (len < 0.05f) continue;
                    Vector3 mid = To3((a + b) * 0.5f, ParapetHeight * 0.5f);
                    RaidMesh m = net.Out.Solid(mid);
                    Beam(m, a, b, 0f, ParapetHeight, ParapetThick, look.Parapet);
                    Beam(m, a, b, ParapetHeight, 0.06f, ParapetThick + 0.08f, look.ParapetCap);
                    net.Site.Town.PropCollider(mid, new Vector3(len, ParapetHeight + 0.1f, ParapetThick), -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    // Add end posts at the piece boundaries unless the piece already abuts the next sample.
                    if (piece.x > s[i].S + 1e-3f) EndPost(net, a);
                    if (piece.y < s[i + 1].S - 1e-3f) EndPost(net, b);
                }
            }
            // Ensure posts at the very start and end of the parapet line.
            if (s.Count >= 2)
            {
                EndPost(net, ParapetPoint(c, side, s[0].S, s[0], s[1]));
                EndPost(net, ParapetPoint(c, side, s[s.Count - 1].S, s[s.Count - 2], s[s.Count - 1]));
            }
            // Early‑out for non‑full maps where snow detail is unnecessary.
            if (!net.Full) return;
            // Generate a deterministic noise seed based on the bridge side to vary snow patterns.
            int seed = Seed(c.W, side > 0 ? 0xB21 : 0xB22);
            // Populate sampling points along the bridge to drive snow placement.
            SamplesOf(c, c.From, c.To, 2f, net.Samples);
            Fit(net.Samples.Count);
            for (int i = 0; i < net.Samples.Count; i++)
            {
                float e = EdgeAt(c, side, net.Samples[i].S);
                // Compute left/right snow edges using the parapet edge and a noisy offset for variation.
                s_A[i] = side * (e - 0.25f - 0.45f * Noise(net.Samples[i].P, 3.5f, seed));
                s_B[i] = side * e;
            }
            for (int i = 0; i + 1 < net.Samples.Count; i++)
            {
                float sm = (net.Samples[i].S + net.Samples[i + 1].S) * 0.5f;
                // Keep snow only where the parapet is not under a mouth opening or crossed by another feature.
                s_Keep[i] = !InMouth(c, side, sm) && !Under(under, sm);
            }
            // Add snow bands using the pre‑computed edges and a small vertical offset to sit on the parapet.
            Band(net, net.Samples, s_A, s_B, DetailY + 0.001f, DetailY + 0.05f, look.Snow, c.Lift, s_Keep);
        }

        // The parapet's line at arc length sv between two samples: mitred at the samples themselves.
        private static Vector2 ParapetPoint(Chain c, int side, float sv, in Sample s0, in Sample s1)
        {
            float o = side * (EdgeAt(c, side, sv) + ParapetThick * 0.5f + 0.05f);
            if (Mathf.Abs(sv - s0.S) < 1e-3f) return s0.P + s0.N * o;
            if (Mathf.Abs(sv - s1.S) < 1e-3f) return s1.P + s1.N * o;
            PointOn(c, sv, out Vector2 p, out Vector2 d);
            return p + Left(d) * o;
        }

        /// <summary>The stretches of <paramref name="line"/> (arc lengths x to y, sorted, merged) that something else passes
        /// beneath at grade: another carriageway with its sidewalks, a path or a railway crossing it, widened for the
        /// angle they cross at. Ways that only join it (ending on it) don't count; nor do <paramref name="self"/> and
        /// <paramref name="selfRail"/>, the line's own.</summary>
        private static void Underpasses(Net net, Chain line, Chain self, Line selfRail, List<Vector2> into)
        {
            into.Clear();
            MapGeometry.Extent(line.P, out float x0, out float z0, out float x1, out float z1);
            var box = Rect.MinMaxRect(x0 - 1f, z0 - 1f, x1 + 1f, z1 + 1f);
            // For each chain in the map, find any intersecting features that pass beneath the bridge at grade.
            foreach (Chain o in net.Chains)
            {
                if (o == self || o.To - o.From < 0.5f) continue;
                Vector2 first = o.P[0], last = o.P[o.P.Count - 1];
                for (int k = 0; k + 1 < o.P.Count; k++)
                {
                    // Ensure the candidate segment lies within the bridge’s longitudinal bounds and overlaps the search box.
                    if (o.Cum[k + 1] < o.From || o.Cum[k] > o.To || !Overlaps(box, o.P[k], o.P[k + 1])) continue;
                    if (!CrossAt(line, o.P[k], o.P[k + 1], out float s, out float sin, out float v)) continue;
                    Vector2 q = Vector2.Lerp(o.P[k], o.P[k + 1], v);
                    if ((q - first).sqrMagnitude < 1f || (q - last).sqrMagnitude < 1f) continue;
                    float so = Mathf.Lerp(o.Cum[k], o.Cum[k + 1], v);
                    // Record the half‑width needed for the underpass based on the widest crossing feature.
                    float half = 0f;
                    // Iterate both sides of the bridge (left/right) to place symmetric guard rails.
                    for (int side = -1; side <= 1; side += 2)
                    {
                        ProfAt(o, side, so, out Prof d);
                        half = Mathf.Max(half, d.Walk ? d.WalkOut : d.Kerb ? d.Edge + KerbWidth : d.Edge + 0.5f);
                    }
                    AddUnder(line, s, sin, half, into);
                }
            }
            foreach (Way p in net.Ways)
            {
                // Process path features similarly, ignoring bridges and ensuring sufficient length.
                if (p.Cls != Cls.Path || p.Bridge || p.P.Count < 2) continue;
                Vector2 first = p.P[0], last = p.P[p.P.Count - 1];
                for (int k = 0; k + 1 < p.P.Count; k++)
                {
                    if (!Overlaps(box, p.P[k], p.P[k + 1]) || !CrossAt(line, p.P[k], p.P[k + 1], out float s, out float sin, out float v)) continue;
                    Vector2 q = Vector2.Lerp(p.P[k], p.P[k + 1], v);
                    if ((q - first).sqrMagnitude < 1f || (q - last).sqrMagnitude < 1f) continue;
                    // Add a buffered width for paths, clamped to a reasonable range, plus an extra margin.
                    AddUnder(line, s, sin, Mathf.Clamp(p.Half, 0.5f, 2f) + 0.3f, into);
                }
            }
            foreach (Line l in net.Site.Area.Lines)
            {
                // Handle railway lines: only consider non‑tunnel tracks with enough points.
                if (l == selfRail || !l.IsRailway || l.Data.tunnel || l.Pts.Count < 2) continue;
                Vector2 first = l.Pts[0], last = l.Pts[l.Pts.Count - 1];
                for (int k = 0; k + 1 < l.Pts.Count; k++)
                {
                    if (!Overlaps(box, l.Pts[k], l.Pts[k + 1]) || !CrossAt(line, l.Pts[k], l.Pts[k + 1], out float s, out float sin, out float v)) continue;
                    Vector2 q = Vector2.Lerp(l.Pts[k], l.Pts[k + 1], v);
                    if ((q - first).sqrMagnitude < 1f || (q - last).sqrMagnitude < 1f) continue;
                    // Add a fixed half‑width for railways (including the track gauge plus clearance).
                    AddUnder(line, s, sin, 2.4f, into);
                }
            }
        }

        private static bool Overlaps(Rect box, Vector2 a, Vector2 b)
            => Mathf.Max(a.x, b.x) >= box.xMin && Mathf.Min(a.x, b.x) <= box.xMax && Mathf.Max(a.y, b.y) >= box.yMin && Mathf.Min(a.y, b.y) <= box.yMax;

        // Where segment a-b crosses the line: the arc length along the line, the sine of the angle they cross at and how
        // far along the segment (0 to 1).
        private static bool CrossAt(Chain line, Vector2 a, Vector2 b, out float s, out float sin, out float v)
        {
            s = sin = v = 0f;
            Vector2 e = b - a;
            float elen = e.magnitude;
            if (elen < 1e-4f) return false;
            for (int k = 0; k + 1 < line.P.Count; k++)
            {
                Vector2 p = line.P[k], d = line.P[k + 1] - p;
                float dlen = line.Cum[k + 1] - line.Cum[k];
                float den = Cross(d, e);
                if (dlen < 1e-4f || Mathf.Abs(den) < 1e-6f) continue;
                Vector2 w = a - p;
                float t = Cross(w, e) / den, u = Cross(w, d) / den;
                if (t < 0f || t > 1f || u < 0f || u > 1f) continue;
                s = line.Cum[k] + t * dlen;
                sin = Mathf.Abs(den) / (dlen * elen);
                v = u;
                return true;
            }
            return false;
        }

        // Adds the stretch a crossing at s takes (half its width either side, over the angle), merging overlaps.
        private static void AddUnder(Chain line, float s, float sin, float half, List<Vector2> into)
        {
            float reach = Mathf.Min(half / Mathf.Max(0.3f, sin) + 0.4f, 40f);
            float a = Mathf.Max(0f, s - reach), b = Mathf.Min(line.Length, s + reach);
            for (int i = 0; i < into.Count; i++)
            {
                if (b < into[i].x || a > into[i].y) continue;
                a = Mathf.Min(a, into[i].x);
                b = Mathf.Max(b, into[i].y);
                into.RemoveAt(i);
                i = -1;
            }
            int at = 0;
            while (at < into.Count && into[at].x < a) at++;
            into.Insert(at, new Vector2(a, b));
        }

        /// <summary>The parts of [<paramref name="lo"/>, <paramref name="hi"/>] outside the sorted stretches
        /// <paramref name="under"/>.</summary>
        private static void Outside(float lo, float hi, List<Vector2> under, List<Vector2> into)
        {
            into.Clear();
            float at = lo;
            foreach (Vector2 u in under)
            {
                if (u.y <= at || u.x >= hi) continue;
                if (u.x > at) into.Add(new Vector2(at, u.x));
                at = Mathf.Max(at, u.y);
            }
            if (hi > at) into.Add(new Vector2(at, hi));
        }

        private static bool Under(List<Vector2> under, float s)
        {
            foreach (Vector2 u in under)
                if (s > u.x && s < u.y) return true;
            return false;
        }

        private static void EndPost(Net net, Vector2 at)
        {
            Vector3 c = To3(at, 0.55f);
            RaidMesh m = net.Out.Solid(c);
            m.Box(c, new Vector3(0.6f, 1.1f, 0.6f), net.Look.Parapet);
            m.Box(c + Vector3.up * 0.58f, new Vector3(0.66f, 0.06f, 0.66f), net.Look.ParapetCap);
            net.Site.Town.PropCollider(c, new Vector3(0.6f, 1.1f, 0.6f), 0f);
        }

        // W-beam guard rail on wooden posts, carrying on 8 m from a bridge end beside the approach and flaring away from
        // the road; left out where the bridge ends at a junction's patch it would block.
        /// <summary>Creates a W-beam guard rail extending from a bridge end along the approach, with posts.</summary>
        private static void GuardRail(Net net, Chain c, bool start)
        {
            Look look = net.Look;
            Arm arm = start ? c.A0 : c.A1;
            if (arm != null)
            {
                Joint j = start ? c.J0 : c.J1;
                // Skip guard‑rail creation if the bridge end is attached to a complex joint (more than two arms).
                if (j != null && j.Arms.Count > 2) return;
            }
            // Determine the start (or end) position along the bridge chain for the rail.
            float s = start ? c.From : c.To;
            PointOn(c, s, out Vector2 p, out Vector2 d);
            Vector2 back = start ? -d : d;
            for (int side = -1; side <= 1; side += 2)
            {
                float edge = EdgeAt(c, side, s);
                // Compute the outward normal for the current side (left/right).
                Vector2 outward = Left(d) * side;
                Vector2 a = p + outward * (edge + 0.4f);
                Vector2 b = a + back * 8f + outward * 0.8f;
                // If the guard‑rail would intersect a building or pavement, shorten it to avoid clipping.
                if (InBuilding(net, b, 0.3f) || OnPavement(net, b, c, 0.1f)) b = a + back * 3f + outward * 0.3f;
                Vector3 mid = To3((a + b) * 0.5f, 0.55f);
                RaidMesh m = net.Out.Solid(mid);
                Beam(m, a, b, 0.42f, 0.32f, 0.08f, look.GuardRail);
                float len = Vector2.Distance(a, b);
                for (float t = 0f; t <= len + 0.01f; t += 1.9f)
                {
                    Vector2 q = Vector2.Lerp(a, b, t / Mathf.Max(0.01f, len)) + outward * 0.12f;
                    m.Box(To3(q, 0.4f), new Vector3(0.15f, 0.8f, 0.15f), look.GuardPost);
                }
                Vector2 dir = b - a;
                net.Site.Town.PropCollider(mid, new Vector3(len, 0.8f, 0.25f), -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
        }

        // Where a divided highway's two carriageways run within a few metres of each other, a concrete barrier down the
        // middle of the gap: a one-way motorway or trunk chain finds the nearest opposite carriageway on its left.
        private static void Medians(Net net)
        {
            Look look = net.Look;
            foreach (Chain c in net.Chains)
            {
                Way w = c.W;
                if (w.Cls > Cls.Trunk || !w.OneWay || w.Link || c.To - c.From < 4f) continue;
                bool with = ChainRunsWithWay(c);
                // Both carriageways would find the same gap: the one heading east (or north) builds the barrier.
                // Determine the heading direction of the carriageway; only the east/north‑facing side builds the median.
                Vector2 heading = (c.P[c.P.Count - 1] - c.P[0]) * (with ? 1f : -1f);
                // Skip carriageways that run west or south, ensuring only one side creates the median.
                if (heading.x < -0.01f || (Mathf.Abs(heading.x) <= 0.01f && heading.y < 0f)) continue;
                // The median sits on the left side of traffic (side = 1 for right‑hand traffic, -1 otherwise).
                int side = with ? 1 : -1;   // the median is left of the traffic
                // Sample points along the carriageway at 4 m intervals for median placement.
                SamplesOf(c, c.From, c.To, 4f, net.Samples);
                List<Sample> s = net.Samples;
                int n = s.Count;
                if (n < 2) continue;
                FitSides(n);
                for (int i = 0; i < n; i++)
                {
                    Vector2 nrm = s[i].N * side;
                    float edge = EdgeAt(c, side, s[i].S);
                    float gap = -1f;
                    // Incrementally test increasing gaps to find the first that fits on pavement.
                    for (float g = 0.6f; g <= 6f; g += 0.4f)
                    {
                        if (!OnPavement(net, s[i].P + nrm * (edge + g), c, 0f, false)) continue;
                        gap = g;
                        break;
                    }
                    s_KerbOk[i] = gap > 0f && !InMouth(c, side, s[i].S);
                    s_WalkIn[i] = gap > 0f ? edge + gap * 0.5f : 0f;
                }
                for (int i = 0; i + 1 < n; i++)
                {
                    if (!s_KerbOk[i] || !s_KerbOk[i + 1]) continue;
                    Vector2 a = s[i].P + s[i].N * (side * s_WalkIn[i]), b = s[i + 1].P + s[i + 1].N * (side * s_WalkIn[i + 1]);
                    Vector2 dir = b - a;
                    float len = dir.magnitude;
                    if (len < 0.1f) continue;
                    Vector3 mid = To3((a + b) * 0.5f, 0.4f);
                    RaidMesh m = net.Out.Solid(mid);
                    Beam(m, a, b, 0f, 0.3f, 0.6f, look.Parapet);
                    Beam(m, a, b, 0.3f, 0.52f, 0.24f, look.Parapet);
                    Beam(m, a, b, 0.82f, 0.05f, 0.3f, look.ParapetCap);
                    net.Site.Town.PropCollider(mid, new Vector3(len, 0.85f, 0.5f), -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                }
            }
        }
    }
}
