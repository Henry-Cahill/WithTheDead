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

        private static void Bridges(Net net)
        {
            foreach (Chain c in net.Chains)
            {
                if (!c.W.Bridge || c.To - c.From < 1f) continue;
                for (int side = -1; side <= 1; side += 2) Parapet(net, c, side);
                if (!net.Full) continue;
                GuardRail(net, c, true);
                GuardRail(net, c, false);
            }
            if (net.Full) Medians(net);
        }

        // One parapet: a concrete wall just outside the carriageway's edge with a snowy cap, a post at each end, a
        // collider along it; on the deck, snow the plough pushed against it.
        private static void Parapet(Net net, Chain c, int side)
        {
            Look look = net.Look;
            Side d = side > 0 ? c.L : c.R;
            float off = side * (d.Edge + ParapetThick * 0.5f + 0.05f);
            SamplesOf(c, c.From, c.To, 0f, net.Samples2);
            List<Sample> s = net.Samples2;
            for (int i = 0; i + 1 < s.Count; i++)
            {
                Vector2 a = s[i].P + s[i].N * off, b = s[i + 1].P + s[i + 1].N * off;
                Vector2 dir = b - a;
                float len = dir.magnitude;
                if (len < 0.05f) continue;
                Vector3 mid = To3((a + b) * 0.5f, ParapetHeight * 0.5f);
                RaidMesh m = net.Out.Solid(mid);
                Beam(m, a, b, 0f, ParapetHeight, ParapetThick, look.Parapet);
                Beam(m, a, b, ParapetHeight, 0.06f, ParapetThick + 0.08f, look.ParapetCap);
                net.Site.Town.PropCollider(mid, new Vector3(len, ParapetHeight + 0.1f, ParapetThick), -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
            if (s.Count >= 2)
            {
                EndPost(net, s[0].P + s[0].N * off);
                EndPost(net, s[s.Count - 1].P + s[s.Count - 1].N * off);
            }
            if (!net.Full) return;
            int seed = Seed(c.W, side > 0 ? 0xB21 : 0xB22);
            SamplesOf(c, c.From, c.To, 2f, net.Samples);
            Fit(net.Samples.Count);
            for (int i = 0; i < net.Samples.Count; i++)
            {
                s_A[i] = side * (d.Edge - 0.25f - 0.45f * Noise(net.Samples[i].P, 3.5f, seed));
                s_B[i] = side * d.Edge;
            }
            Band(net, net.Samples, s_A, s_B, DetailY + 0.001f, DetailY + 0.05f, look.Snow, c.Lift);
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
        private static void GuardRail(Net net, Chain c, bool start)
        {
            Look look = net.Look;
            Arm arm = start ? c.A0 : c.A1;
            if (arm != null)
            {
                Joint j = start ? c.J0 : c.J1;
                if (j != null && j.Arms.Count > 2) return;
            }
            float s = start ? c.From : c.To;
            PointOn(c, s, out Vector2 p, out Vector2 d);
            Vector2 back = start ? -d : d;
            for (int side = -1; side <= 1; side += 2)
            {
                Side sd = side > 0 ? c.L : c.R;
                Vector2 outward = Left(d) * side;
                Vector2 a = p + outward * (sd.Edge + 0.4f);
                Vector2 b = a + back * 8f + outward * 0.8f;
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
                Vector2 heading = (c.P[c.P.Count - 1] - c.P[0]) * (with ? 1f : -1f);
                if (heading.x < -0.01f || (Mathf.Abs(heading.x) <= 0.01f && heading.y < 0f)) continue;
                int side = with ? 1 : -1;   // the median is left of the traffic
                Side d = side > 0 ? c.L : c.R;
                SamplesOf(c, c.From, c.To, 4f, net.Samples);
                List<Sample> s = net.Samples;
                int n = s.Count;
                if (n < 2) continue;
                FitSides(n);
                for (int i = 0; i < n; i++)
                {
                    Vector2 nrm = s[i].N * side;
                    float gap = -1f;
                    for (float g = 0.6f; g <= 6f; g += 0.4f)
                    {
                        if (!OnPavement(net, s[i].P + nrm * (d.Edge + g), c, 0f, false)) continue;
                        gap = g;
                        break;
                    }
                    s_KerbOk[i] = gap > 0f;
                    s_WalkIn[i] = gap > 0f ? d.Edge + gap * 0.5f : 0f;
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
