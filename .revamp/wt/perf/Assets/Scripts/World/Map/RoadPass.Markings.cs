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
                    if (a.StopBar)
                    {
                        float at = a.Trim + (a.Crosswalk ? 4.2f : 1.0f);
                        float s0 = a.AtStart ? at : c.Length - at - 0.45f;
                        if (s0 < c.From - 0.01f || s0 + 0.45f > c.To + 0.01f) continue;
                        float sm = s0 + 0.22f, left = EdgeLaneAt(c, 1, sm), right = EdgeLaneAt(c, -1, sm);
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
                        Across(net, c, s0, s0 + 0.45f, lo, hi, net.Look.White, seed + 1);
                    }
                }
            }
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

        // A crosswalk: two white bars across the whole carriageway, with the walkers' lane between them.
        private static void Crosswalk(Net net, Chain c, float s0, float s1, int seed)
        {
            if (s1 - s0 < 1f) return;
            float m = (s0 + s1) * 0.5f, l = EdgeAt(c, 1, m), r = EdgeAt(c, -1, m);
            Across(net, c, s0, s0 + 0.3f, -r + 0.25f, l - 0.25f, net.Look.White, seed);
            Across(net, c, s1 - 0.3f, s1, -r + 0.25f, l - 0.25f, net.Look.White, seed + 1);
        }

        // A painted bar across a chain between <paramref name="s0"/> and <paramref name="s1"/>, from <paramref name="lo"/>
        // to <paramref name="hi"/> across it, in pieces of which the slush has buried some.
        private static void Across(Net net, Chain c, float s0, float s1, float lo, float hi, in Surf surf, int seed)
        {
            if (hi - lo < 0.2f) return;
            SamplesOf(c, Mathf.Max(0f, s0), Mathf.Min(c.Length, s1), 0f, net.Samples2);
            int n = net.Samples2.Count;
            if (n < 2) return;
            Fit(n);
            int pieces = Mathf.Max(1, Mathf.RoundToInt((hi - lo) / 0.7f));
            for (int k = 0; k < pieces; k++)
            {
                float a = lo + (hi - lo) * k / pieces, b = lo + (hi - lo) * (k + 1) / pieces;
                Vector2 mid = net.Samples2[0].P + net.Samples2[0].N * ((a + b) * 0.5f);
                if (Noise(mid, 1.75f, seed) < 0.22f) continue;
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = a;
                    s_B[i] = b;
                }
                Band(net, net.Samples2, s_A, s_B, JunctionMarkY, JunctionMarkY, surf, c.Lift);
            }
        }
    }
}
