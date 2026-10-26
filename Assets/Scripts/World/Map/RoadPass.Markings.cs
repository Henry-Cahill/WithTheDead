using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Faded paint on the ploughed roads, partly under slush: double yellow down two-way main roads, a dashed yellow on
    // the lesser ones, white lane lines between lanes running the same way, white edge lines on highways and rural main
    // roads (yellow on a divided highway's median side, open at side roads' mouths), stop bars on the approaches that
    // give way, crosswalks downtown, at signals and where crossings are mapped. Packed snow hides the paint on the side
    // streets. A road's paint carries on through the junctions it runs through.
    internal static partial class RoadPass
    {
        private const float LineWidth = 0.1f;
        private const float CrosswalkLaneHalf = 0.25f; // half the width of the central lane (≈0.5 m total)
        private const float DashLength = 3f, DashPeriod = 12f;

        /// <summary>Where a line runs across a chain: about its centre line, a fraction across its lanes, or by one of its
        /// travel lanes' outer ends.</summary>
        private enum Along { Centre, Lanes, LeftEdge, RightEdge }

        private static float OffsetAt(Chain c, float s, Along kind, float f)
        {
            switch (kind)
            {
                case Along.Centre: return (EdgeLaneAt(c, 1, s) - EdgeLaneAt(c, -1, s)) * 0.5f + f;
                case Along.Lanes: return LaneAt(c, s, f);
                case Along.LeftEdge: return EdgeLaneAt(c, 1, s) - f;
                default: return -(EdgeLaneAt(c, -1, s) - f);
            }
        }

        private static IEnumerable<string> Markings(Net net)
        {
            Look look = net.Look;
            foreach (Chain c in net.Chains)
            {
                Way w = c.W;
                if (w.Winter != Winter.Ploughed || w.Cls > Cls.Minor || c.To - c.From < 1f) continue;
                float from = c.From + EndGap(c.A0), to = c.To - EndGap(c.A1);
                int seed = Seed(w, 0x3A7);
                if (to - from > 0.5f)
                    foreach (Part p in c.Parts)
                    {
                        float a = Mathf.Max(from, p.S0), b = Mathf.Min(to, p.S1);
                        if (b - a < 0.5f) continue;
                        Way pw = p.Run.W;
                        if (pw.Centre == CentreLine.Double)
                        {
                            SolidLine(net, c, a, b, Along.Centre, -0.11f, 0, look.Yellow, seed);
                            SolidLine(net, c, a, b, Along.Centre, 0.11f, 0, look.Yellow, seed + 1);
                        }
                        else if (pw.Centre == CentreLine.Dashed) Dashes(net, c, a, b, Along.Centre, 0f, look.Yellow, seed + 2);
                        if (pw.OneWay)
                        {
                            int lanes = Mathf.Max(1, pw.LanesR);
                            for (int k = 1; k < lanes; k++) Dashes(net, c, a, b, Along.Lanes, (float)k / lanes, look.White, seed + 10 + k);
                        }
                        else if (pw.Centre != CentreLine.None)
                        {
                            int each = Mathf.Max(1, pw.LanesL);
                            for (int k = 1; k < each; k++)
                            {
                                Dashes(net, c, a, b, Along.Lanes, 0.5f + 0.5f * k / each, look.White, seed + 20 + k);
                                Dashes(net, c, a, b, Along.Lanes, 0.5f - 0.5f * k / each, look.White, seed + 30 + k);
                            }
                        }
                        Side l = SideOf(p, 1), r = SideOf(p, -1);
                        if (l.EdgeLine) SolidLine(net, c, a, b, Along.LeftEdge, 0.15f, 1, l.EdgeYellow ? look.Yellow : look.White, seed + 40);
                        if (r.EdgeLine) SolidLine(net, c, a, b, Along.RightEdge, 0.15f, -1, r.EdgeYellow ? look.Yellow : look.White, seed + 41);
                    }
                if (c.Crossings != null)
                    foreach (float s in c.Crossings) Crosswalk(net, c, s - 1.45f, s + 1.45f, seed + 50);
                StallLines(net, c, seed + 60);
                if (net.Site.StepDue) yield return "road markings";
            }
            foreach (Joint j in net.Joints)
            {
                if (!j.HasPatch) continue;
                foreach (Arm a in j.Arms)
                {
                    Chain c = a.Chain;
                    if (a.Through || c == null || c.W.Winter != Winter.Ploughed || c.To - c.From < 1f) continue;
                    int seed = Seed(c.W, 0x5709);
                    if (a.Crosswalk)
                    {
                        float near = a.Trim + 0.3f, far = a.Trim + 3.3f;
                        if (a.AtStart) Crosswalk(net, c, near, far, seed);
                        else Crosswalk(net, c, c.Length - far, c.Length - near, seed);
                    }
                    if (!a.StopBar) continue;
                    // Behind a crosswalk at the junction's mouth (a mapped one up to 9 m out), else a metre short of the patch.
                    float at = a.Trim + (a.Crosswalk ? 4.2f : 1.0f);
                    if (c.Crossings != null)
                        foreach (float x in c.Crossings)
                        {
                            float from = a.AtStart ? x : c.Length - x;
                            if (from > a.Trim && from < a.Trim + 9f) at = Mathf.Max(at, from + 1.75f);
                        }
                    float sm = a.AtStart ? at + 0.22f : c.Length - at - 0.22f;
                    if (sm < c.From + 0.3f || sm > c.To - 0.3f || InMouth(c, 1, sm) || InMouth(c, -1, sm)) continue;
                    PointOn(c, sm, out Vector2 mid, out Vector2 _);
                    if (InOtherJunction(net, mid, j)) continue;
                    float left = EdgeLaneAt(c, 1, sm), right = EdgeLaneAt(c, -1, sm);
                    float centre = (left - right) * 0.5f;
                    float lo, hi;
                    if (c.W.OneWay)
                    {
                        lo = -right;
                        hi = left;
                    }
                    else if (a.AtStart)
                    {
                        lo = centre + 0.2f;
                        hi = left;
                    }
                    else
                    {
                        lo = -right;
                        hi = centre - 0.2f;
                    }
                    Bar(net, c, sm, 0.225f, lo, hi, net.Look.White, seed + 1);
                }
            }
        }

        /// <summary>True when <paramref name="q"/> lies on the patch or a mouth of a junction other than
        /// <paramref name="j"/>.</summary>
        private static bool InOtherJunction(Net net, Vector2 q, Joint j)
        {
            int stamp = net.NewStamp(net.PaveItems.Count + net.PatchItems.Count);
            net.PaveGrid.Query(q.x, q.y, q.x, q.y, net.Query, net.Stamp, stamp);
            foreach (int it in net.Query)
            {
                if (it < net.PaveItems.Count) continue;
                (Joint o, int poly) = net.PatchItems[it - net.PaveItems.Count];
                if (o != j && InPatch(o, poly, q)) return true;
            }
            return false;
        }

        /// <summary>Metres along the kerb between two stalls' lines: angled stalls (60 degrees to the kerb) or parallel.</summary>
        private const float AngledStall = 3.2f, ParallelStall = 6.5f;

        // The parking lanes' stall lines, half lost under the slush: lines at 60 degrees from the lane's edge to the kerb,
        // leaning the way the traffic on that side runs, or ticks between parallel stalls; spaced on the street's whole
        // mapped length so neighbouring chunks agree, none near a junction, mouths, crosswalks or driveways. Each whole
        // stall between two lines goes on the list KerbStalls hands out (for the parked cars).
        private static void StallLines(Net net, Chain c, int seed)
        {
            foreach (Part p in c.Parts)
            {
                Way pw = p.Run.W;
                if (pw.Cls <= Cls.Trunk) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    Side sd = SideOf(p, side);
                    float depth = sd.Edge - sd.Lane;
                    if (depth < 1.5f) continue;
                    bool angled = depth > 4f;
                    float step = angled ? AngledStall : ParallelStall;
                    // Traffic along +s on the right, or on both sides of a one-way street run the chain's way.
                    bool forward = pw.OneWay ? !p.Rev : side < 0;
                    float lean = angled ? (forward ? 1f : -1f) * depth / Mathf.Tan(60f * Mathf.Deg2Rad) : 0f;
                    // Clear of a junction at the chain's ends; where it runs off the piece, the neighbour carries on.
                    float a = Mathf.Max(c.From + (c.J0 != null ? 5f : 0.3f), p.S0), b = Mathf.Min(c.To - (c.J1 != null ? 5f : 0.3f), p.S1);
                    if (b - a < step) continue;
                    float wa = ArcOf(p, a), wb = ArcOf(p, b);
                    float lo = Mathf.Min(wa, wb), hi = Mathf.Max(wa, wb);
                    List<Cut> cuts = side > 0 ? c.CutsL : c.CutsR;
                    bool lastOk = false;
                    float lastS = 0f;
                    for (float x = Mathf.Ceil(lo / step) * step; x <= hi; x += step)
                    {
                        float s = p.Rev ? a + (wa - x) : a + (x - wa);
                        float s1 = s + lean;
                        bool ok = s1 >= a && s1 <= b && CutAt(cuts, s) < 0 && CutAt(cuts, s1) < 0
                            && !InMouth(c, side, s - 4f) && !InMouth(c, side, s + 4f) && !InMouth(c, side, s1);
                        if (ok)
                        {
                            PointOn(c, s, out Vector2 p0, out Vector2 d0);
                            if (Noise(p0, 2.25f, seed + side) >= 0.3f)
                            {
                                PointOn(c, s1, out Vector2 p1, out Vector2 d1);
                                Vector2 from = p0 + Left(d0) * (side * (EdgeLaneAt(c, side, s) + 0.15f));
                                Vector2 to = p1 + Left(d1) * (side * (EdgeAt(c, side, s1) - 0.3f));
                                s_Crack.Clear();
                                s_Crack.Add(from);
                                s_Crack.Add(Vector2.Lerp(from, to, 0.55f + 0.3f * Noise(p1, 1.75f, seed + 5)));
                                StrokeAt(net, s_Crack, LineWidth, net.Look.White, MarkY);
                            }
                            if (lastOk) AddStall(net, c, pw, side, lastS, s, lean, angled, forward, cuts);
                        }
                        lastOk = ok;
                        lastS = s;
                    }
                }
            }
        }

        // The stall between the lines at s0 and s1 on one side of a chain, unless a cut or a mouth falls inside it.
        private static void AddStall(Net net, Chain c, Way pw, int side, float s0, float s1, float lean, bool angled, bool forward, List<Cut> cuts)
        {
            float lo = Mathf.Min(s0, s0 + lean, s1, s1 + lean), hi = Mathf.Max(s0, s0 + lean, s1, s1 + lean);
            if (cuts != null)
                foreach (Cut k in cuts)
                    if (k.Kind != CutKind.Walk && k.S1 > lo && k.S0 < hi) return;
            float sm = (s0 + s1 + lean) * 0.5f;
            if (InMouth(c, side, sm)) return;
            PointOn(c, sm, out Vector2 p, out Vector2 d);
            float lane = EdgeLaneAt(c, side, sm), edge = EdgeAt(c, side, sm);
            Vector2 at = p + Left(d) * (side * (lane + edge) * 0.5f);
            if (!Finite(at) || !net.Square.Contains(at)) return;
            Vector2 heading = forward ? d : -d;
            if (angled)
            {
                // Along the stall's lines, from the lane's edge to the kerb: nose in.
                PointOn(c, sm - lean * 0.5f, out Vector2 q0, out Vector2 e0);
                PointOn(c, sm + lean * 0.5f, out Vector2 q1, out Vector2 e1);
                heading = q1 + Left(e1) * (side * edge) - (q0 + Left(e0) * (side * lane));
            }
            if (heading.sqrMagnitude < 1e-6f) return;
            heading.Normalize();
            net.Stalls.Add(new KerbStall { At = at, Yaw = Mathf.Atan2(heading.x, heading.y) * Mathf.Rad2Deg, Angled = angled, Street = pw.S });
        }

        // How far before a junction's patch the lane lines stop: short of a crosswalk and its stop bar, or a stop bar.
        private static float EndGap(Arm a)
        {
            if (a == null) return 0f;
            if (a.Crosswalk) return a.StopBar ? 4.9f : 3.6f;
            return a.StopBar ? 1.7f : 0.6f;
        }

        // A solid line, broken where slush lies over it (and, beside a kerb line, where side roads join).
        private static void SolidLine(Net net, Chain c, float from, float to, Along kind, float f, int mouthSide, in Surf surf, int seed)
        {
            List<Sample> s = net.Samples;
            SamplesOf(c, from, to, 1.5f, s);
            int n = s.Count;
            if (n < 2) return;
            Fit(n);
            // Clear any leftover flags from the previous call – only the first (n‑1) entries matter.
            System.Array.Clear(s_Keep, 0, n - 1);
            for (int i = 0; i < n; i++)
            {
                float off = OffsetAt(c, s[i].S, kind, f);
                s_A[i] = off - LineWidth * 0.5f;
                s_B[i] = off + LineWidth * 0.5f;
            }
            for (int i = 0; i + 1 < n; i++)
                s_Keep[i] = Noise((s[i].P + s[i + 1].P) * 0.5f, 3f, seed) > 0.3f
                    && (mouthSide == 0 || !InMouth(c, mouthSide, (s[i].S + s[i + 1].S) * 0.5f));
            Band(net, s, s_A, s_B, MarkY, MarkY, surf, c.Lift, s_Keep);
        }

        // Dashes 3 m long every 12 m, in step with the whole mapped way's length so neighbouring chunks' dashes line up.
        private static void Dashes(Net net, Chain c, float from, float to, Along kind, float f, in Surf surf, int seed)
        {
            foreach (Part p in c.Parts)
            {
                float a = Mathf.Max(from, p.S0), b = Mathf.Min(to, p.S1);
                if (b - a < 0.2f) continue;
                float wa = ArcOf(p, a), wb = ArcOf(p, b);
                float lo = Mathf.Min(wa, wb), hi = Mathf.Max(wa, wb);
                for (float d0 = Mathf.Floor(lo / DashPeriod) * DashPeriod; d0 < hi; d0 += DashPeriod)
                {
                    float x0 = Mathf.Max(d0, lo), x1 = Mathf.Min(d0 + DashLength, hi);
                    if (x1 - x0 < 0.3f) continue;
                    float s0 = p.Rev ? a + (wa - x1) : a + (x0 - wa), s1 = p.Rev ? a + (wa - x0) : a + (x1 - wa);
                    PointOn(c, (s0 + s1) * 0.5f, out Vector2 mid, out Vector2 _);
                    if (Noise(mid, 3.5f, seed) < 0.25f) continue;
                    SamplesOf(c, s0, s1, 0f, net.Samples2);
                    Fit(net.Samples2.Count);
                    for (int i = 0; i < net.Samples2.Count; i++)
                    {
                        float off = OffsetAt(c, net.Samples2[i].S, kind, f);
                        s_A[i] = off - LineWidth * 0.5f;
                        s_B[i] = off + LineWidth * 0.5f;
                    }
                    Band(net, net.Samples2, s_A, s_B, MarkY, MarkY, surf, c.Lift);
                }
            }
        }

        /// <summary>How far along its whole mapped way a point <paramref name="s"/> metres along the chain is.</summary>
        private static float ArcOf(Part p, float s)
        {
            Way w = p.Run.W;
            return p.Rev ? w.ArcOffset + w.Cum[p.Run.V1] - (s - p.S0) : w.ArcOffset + w.Cum[p.Run.V0] + (s - p.S0);
        }

        // Crosswalks now include a faint white lane (the walkers’ lane) between the two painted bars,
        /// matching period‑accurate rural road markings of 1993 Kentucky.
        private static void Crosswalk(Net net, Chain c, float s0, float s1, int seed)
        {
            if (s1 - s0 < 1f) return;
            float m = (s0 + s1) * 0.5f, l = EdgeAt(c, 1, m), r = EdgeAt(c, -1, m);
            // Draw the two white bars at the edges of the road.
            Bar(net, c, s0 + 0.15f, 0.15f, -r + 0.25f, l - 0.25f, net.Look.White, seed);
            Bar(net, c, s1 - 0.15f, 0.15f, -r + 0.25f, l - 0.25f, net.Look.White, seed + 1);
            // Compute inner edges of the two white bars.
            float barLeft = l - 0.25f; // left bar inner edge (positive side)
            float barRight = -r + 0.25f; // right bar inner edge (positive side)
            // Centre of the space between the bars.
            float laneCenterOffset = (barLeft + barRight) * 0.5f;
            CrosswalkLaneBetweenBars(net, c,
                s0 + 0.15f, s1 - 0.15f,
                laneCenterOffset, // offset from road centre line
                net.Look.White, seed + 2);
        }

// A straight painted bar square across a chain at <paramref name="s"/> (its direction there), <paramref name="half"/>
// either way along it, from <paramref name="lo"/> to <paramref name="hi"/> across it, in pieces; the slush at the
// gutters has buried an end piece here and there (never the middle: a lone piece left reads as a stray dash).
private static void CrosswalkLaneBetweenBars(Net net, Chain c, float s0, float s1, float centreOffset, in Surf surf, int seed)
        {
            // Bail if the stretch is tiny.
            if (s1 - s0 < 0.2f) return;
            // Mid‑point provides direction.
            float m = (s0 + s1) * 0.5f;
            PointOn(c, m, out Vector2 p, out Vector2 d);
            Vector2 n = Left(d);
            // Shift the whole lane by centreOffset from the road centre line.
            Vector2 laneOrigin = p + n * centreOffset;
            // Build the quad spanning the lane width (CrosswalkLaneHalf = 0.25f).
            Vector3 bl = To3(laneOrigin + n * (-CrosswalkLaneHalf) - d * (s1 - s0) * 0.5f, MarkY);
            Vector3 tl = To3(laneOrigin + n * (-CrosswalkLaneHalf) + d * (s1 - s0) * 0.5f, MarkY);
            Vector3 tr = To3(laneOrigin + n * ( CrosswalkLaneHalf) + d * (s1 - s0) * 0.5f, MarkY);
            Vector3 br = To3(laneOrigin + n * ( CrosswalkLaneHalf) - d * (s1 - s0) * 0.5f, MarkY);
            // Occasionally skip a piece to simulate slush covering the paint.
            if (Noise(p, 2f, seed) < 0.2f) return;
            Face(net.Out.Flat((bl + tr) * 0.5f), bl, tl, tr, br, Vector3.up, surf);
        }

        // A straight painted bar square across a chain at <paramref name="s"/> (its direction there), <paramref name="half"/>
        // either way along it, from <paramref name="lo"/> to <paramref name="hi"/> across it, in pieces; the slush at the
        // gutters has buried an end piece here and there (never the middle: a lone piece left reads as a stray dash).
        private static void Bar(Net net, Chain c, float s, float half, float lo, float hi, in Surf surf, int seed)
        {
            if (hi - lo < 0.2f || s < 0f || s > c.Length) return;
            PointOn(c, s, out Vector2 p, out Vector2 _);
            Vector2 d = ChainDir(c, s), n = Left(d);
            int pieces = Mathf.Max(1, Mathf.RoundToInt((hi - lo) / 0.7f));
            for (int k = 0; k < pieces; k++)
            {
                float a = lo + (hi - lo) * k / pieces, b = lo + (hi - lo) * (k + 1) / pieces;
                if (pieces > 3 && (k == 0 || k == pieces - 1) && Noise(p + n * ((a + b) * 0.5f), 1.75f, seed) < 0.4f) continue;
                Vector3 p0 = To3(p + n * a - d * half, JunctionMarkY), p1 = To3(p + n * a + d * half, JunctionMarkY);
                Vector3 q1 = To3(p + n * b + d * half, JunctionMarkY), q0 = To3(p + n * b - d * half, JunctionMarkY);
                Face(net.Out.Flat((p0 + q1) * 0.5f), p0, p1, q1, q0, Vector3.up, surf);
            }
        }
    }
}
