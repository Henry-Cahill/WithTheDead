using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Kerbs, verges and sidewalks along the carriageways, round the junctions' corners, with ramps where crosswalks meet
    // them and gaps where driveways cross them.
    internal static partial class RoadPass
    {
        private enum CutKind { Ramp, Driveway }

        private struct Cut
        {
            public float S0, S1;
            public CutKind Kind;
        }

        private static bool[] s_KerbOk = new bool[256], s_WalkOk = new bool[256];
        private static float[] s_WalkIn = new float[256], s_WalkOut = new float[256];
        private static readonly List<Vector2> s_Ring = new List<Vector2>(32);

        private static void FitSides(int n)
        {
            Fit(n);
            if (s_KerbOk.Length >= n) return;
            int size = Mathf.NextPowerOfTwo(n);
            s_KerbOk = new bool[size];
            s_WalkOk = new bool[size];
            s_WalkIn = new float[size];
            s_WalkOut = new float[size];
        }

        private static void AddCut(Chain c, float s0, float s1, CutKind kind, int side = 0)
        {
            s0 = Mathf.Max(s0, c.From);
            s1 = Mathf.Min(s1, c.To);
            if (s1 - s0 < 0.3f) return;
            var cut = new Cut { S0 = s0, S1 = s1, Kind = kind };
            if (side >= 0) (c.CutsL ??= new List<Cut>()).Add(cut);
            if (side <= 0) (c.CutsR ??= new List<Cut>()).Add(cut);
        }

        /// <summary>The cut a point along a chain's side falls in: -1 none, else its kind.</summary>
        private static int CutAt(List<Cut> cuts, float s)
        {
            if (cuts == null) return -1;
            for (int i = 0; i < cuts.Count; i++)
                if (s > cuts[i].S0 && s < cuts[i].S1) return (int)cuts[i].Kind;
            return -1;
        }

        /// <summary>The chain whose carriageway <paramref name="q"/> lies on, nearest its middle, and how far along it.</summary>
        private static bool NearestChain(Net net, Vector2 q, out Chain best, out float along)
        {
            best = null;
            along = 0f;
            float bestLat = float.MaxValue;
            int stamp = net.NewStamp(net.PaveItems.Count + net.PatchItems.Count);
            net.PaveGrid.Query(q.x, q.y, q.x, q.y, net.Query, net.Stamp, stamp);
            foreach (int it in net.Query)
            {
                if (it >= net.PaveItems.Count) continue;
                (Chain c, int k) = net.PaveItems[it];
                Vector2 a = c.P[k];
                float len = c.Cum[k + 1] - c.Cum[k];
                if (len < 1e-4f) continue;
                Vector2 d = (c.P[k + 1] - a) / len;
                float t = Vector2.Dot(q - a, d);
                if (t < -0.05f || t > len + 0.05f) continue;
                float lat = Cross(d, q - a);
                if (lat > c.L.Edge + 0.5f || -lat > c.R.Edge + 0.5f) continue;
                float off = Mathf.Abs(lat - (c.L.Edge - c.R.Edge) * 0.5f);
                if (off >= bestLat) continue;
                bestLat = off;
                best = c;
                along = c.Cum[k] + Mathf.Clamp(t, 0f, len);
            }
            return best != null;
        }

        // Where the kerbs open: ramps where crosswalks reach them (at junction arms marked for one, and at the crossings
        // mapped mid-block), and driveways.
        private static void PlanCuts(Net net)
        {
            foreach (Node n in net.Site.Area.Nodes)
            {
                if (n.Data.kind != "crossing" || !net.Square.Contains(n.At)) continue;
                if (!NearestChain(net, n.At, out Chain c, out float s)) continue;
                if (c.A0 != null && s - c.From < 7f)
                {
                    if (c.A0.W.Cls <= Cls.Minor) c.A0.Crosswalk = true;
                    continue;
                }
                if (c.A1 != null && c.To - s < 7f)
                {
                    if (c.A1.W.Cls <= Cls.Minor) c.A1.Crosswalk = true;
                    continue;
                }
                if (s - c.From < 2f || c.To - s < 2f) continue;
                (c.Crossings ??= new List<float>()).Add(s);
                AddCut(c, s - 1.5f, s + 1.5f, CutKind.Ramp);
            }
            foreach (Chain c in net.Chains)
            {
                if (c.A0 != null && c.A0.Crosswalk) AddCut(c, c.From + 0.3f, c.From + 3.3f, CutKind.Ramp);
                if (c.A1 != null && c.A1.Crosswalk) AddCut(c, c.To - 3.3f, c.To - 0.3f, CutKind.Ramp);
            }
            if (net.Full) PlanDriveways(net);
        }

        private static void SidesAndCorners(Net net)
        {
            foreach (Chain c in net.Chains)
            {
                if (c.To - c.From < 0.05f) continue;
                Sides(net, c, 1);
                Sides(net, c, -1);
            }
            foreach (Joint j in net.Joints)
                if (j.Patch != null) Corners(net, j);
        }

        // One side of a chain: the kerb (its face and top), the verge, the sidewalk out to its edge or the first wall,
        // its winter cover, a snowy bank past it, and the ramps down to the crosswalks. Nothing where another road or a
        // building takes the ground.
        private static void Sides(Net net, Chain c, int side)
        {
            Side d = side > 0 ? c.L : c.R;
            if (!d.Kerb && !d.Walk) return;
            Look look = net.Look;
            List<Cut> cuts = side > 0 ? c.CutsL : c.CutsR;
            List<Sample> s = net.Samples;
            float step = net.Full ? 1f : 4f;
            s.Clear();
            float at = c.From;
            if (cuts != null)
            {
                cuts.Sort((x, y) => x.S0.CompareTo(y.S0));
                foreach (Cut cut in cuts)
                {
                    if (cut.S0 > at) SamplesOf(c, at, cut.S0, step, s, true);
                    SamplesOf(c, Mathf.Max(at, cut.S0), Mathf.Max(at, cut.S1), 0f, s, true);
                    at = Mathf.Max(at, cut.S1);
                }
            }
            SamplesOf(c, at, c.To, step, s, true);
            int n = s.Count;
            if (n < 2) return;
            FitSides(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 nrm = s[i].N * side;
                float k = Mathf.Max(0.5f, nrm.magnitude);
                Vector2 unit = nrm / k;
                Vector2 back = s[i].P + nrm * (d.Edge + KerbWidth + 0.3f);
                bool blocked = OnPavement(net, back, c, 0f) || InBuilding(net, back, 0f);
                s_KerbOk[i] = d.Kerb && !blocked;
                float wi = d.WalkIn, wo = d.WalkOut;
                bool walk = d.Walk && !blocked;
                if (walk)
                {
                    float room = RayToBuilding(net, s[i].P + nrm * wi, unit, (wo - wi) * k + 0.3f);
                    float limit = wi + (room - 0.05f) / k;
                    if (limit < wo) wo = limit;
                    if (wo < wi + 0.35f) walk = false;
                    else if (OnPavement(net, s[i].P + nrm * ((wi + wo) * 0.5f), c, 0f)) walk = false;
                }
                s_WalkOk[i] = walk;
                s_WalkIn[i] = wi;
                s_WalkOut[i] = wo;
            }
            float roadY = RoadY + c.W.Order * 0.0003f;
            // Segments: the kerb where both ends have it and no cut opens it; ramps take the walk's first 1.3 m.
            for (int i = 0; i + 1 < n; i++)
            {
                int cut = CutAt(cuts, (s[i].S + s[i + 1].S) * 0.5f);
                bool kerb = s_KerbOk[i] && s_KerbOk[i + 1] && cut < 0;
                s_Keep[i] = kerb;
                if (cut == (int)CutKind.Ramp && d.Kerb && d.Walk && s_WalkOk[i] && s_WalkOk[i + 1])
                    Ramp(net, c, s[i], s[i + 1], side, d, roadY);
            }
            if (d.Kerb)
            {
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = side * d.Edge;
                    s_B[i] = side * (d.Edge + KerbWidth);
                }
                Band(net, s, s_A, s_B, KerbTop, KerbTop, look.Kerb, c.Lift, s_Keep);
                for (int i = 0; i + 1 < n; i++)
                {
                    if (!s_Keep[i]) continue;
                    float l0 = c.Lift != null ? c.Lift.At(s[i].S) : 0f, l1 = c.Lift != null ? c.Lift.At(s[i + 1].S) : 0f;
                    Vector2 a = s[i].P + s[i].N * (side * d.Edge), b = s[i + 1].P + s[i + 1].N * (side * d.Edge);
                    Vector3 toward = To3(-s[i].N * side, 0f);
                    Vector3 a0 = To3(a, roadY + l0), b0 = To3(b, roadY + l1), b1 = To3(b, KerbTop + l1), a1 = To3(a, KerbTop + l0);
                    Face(net.Out.Flat((a0 + b1) * 0.5f), a0, b0, b1, a1, toward, look.KerbFace);
                }
                if (d.WalkIn > d.Edge + KerbWidth + 0.05f || !d.Walk)
                {
                    // The verge between the kerb and a set-back sidewalk (or, with no sidewalk, a strip of it).
                    float outer = d.Walk ? d.WalkIn : d.Edge + KerbWidth + 0.6f;
                    for (int i = 0; i < n; i++)
                    {
                        s_A[i] = side * (d.Edge + KerbWidth);
                        s_B[i] = side * outer;
                    }
                    Band(net, s, s_A, s_B, KerbTop - 0.004f, d.Walk ? KerbTop - 0.004f : 0.004f, look.Snow, c.Lift, s_Keep);
                }
            }
            if (!d.Walk) return;
            bool downtown = c.W.Downtown || (d.Mapped && c.W.Town && c.W.Setback < 6f);
            for (int i = 0; i + 1 < n; i++)
            {
                int cut = CutAt(cuts, (s[i].S + s[i + 1].S) * 0.5f);
                s_Keep[i] = s_WalkOk[i] && s_WalkOk[i + 1];
                if (cut == (int)CutKind.Ramp)
                {
                    s_WalkIn[i] = Mathf.Max(s_WalkIn[i], d.Edge + 1.3f);
                    s_WalkIn[i + 1] = Mathf.Max(s_WalkIn[i + 1], d.Edge + 1.3f);
                }
            }
            for (int i = 0; i < n; i++)
            {
                s_A[i] = side * s_WalkIn[i];
                s_B[i] = side * s_WalkOut[i];
            }
            Band(net, s, s_A, s_B, KerbTop, KerbTop, look.Sidewalk, c.Lift, s_Keep);
            if (!net.Full) return;
            int seed = Seed(c.W, side > 0 ? 0x51D : 0x51E);
            if (downtown)
            {
                // Shovelled in front of the shops: snow left against the walls and in lumps by the kerb.
                for (int i = 0; i < n; i++)
                {
                    float wIn = s_WalkIn[i], wOut = s_WalkOut[i];
                    s_A[i] = side * Mathf.Max(wIn, wOut - 0.2f - 0.5f * Noise(s[i].P, 3.5f, seed));
                    s_B[i] = side * wOut;
                }
                Band(net, s, s_A, s_B, KerbTop + 0.003f, KerbTop + 0.003f, look.Snow, c.Lift, s_Keep);
            }
            else
            {
                // Snow over the sidewalk, a trodden line down it.
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = side * s_WalkIn[i];
                    s_B[i] = side * s_WalkOut[i];
                }
                Band(net, s, s_A, s_B, KerbTop + 0.003f, KerbTop + 0.003f, look.Snow, c.Lift, s_Keep);
                for (int i = 0; i < n; i++)
                {
                    float mid = (s_WalkIn[i] + s_WalkOut[i]) * 0.5f + 0.15f * (Noise(s[i].P, 4.5f, seed + 1) - 0.5f);
                    float half = 0.22f + 0.12f * Noise(s[i].P, 2.25f, seed + 2);
                    s_A[i] = side * (mid - half);
                    s_B[i] = side * (mid + half);
                    s_YA[i] = s_WalkOk[i] ? 1f : 0f;
                }
                for (int i = 0; i + 1 < n; i++)
                    s_Keep[i] = s_YA[i] > 0f && s_YA[i + 1] > 0f && Noise(s[i].P, 7f, seed + 3) > 0.3f;
                Band(net, s, s_A, s_B, KerbTop + 0.005f, KerbTop + 0.005f, look.Trodden, c.Lift, s_Keep);
            }
            // A low bank of snow past the sidewalk's outer edge, where no wall stands on it.
            for (int i = 0; i < n; i++)
            {
                s_A[i] = side * s_WalkOut[i];
                s_B[i] = side * (s_WalkOut[i] + 0.35f);
            }
            for (int i = 0; i + 1 < n; i++)
                s_Keep[i] = s_WalkOk[i] && s_WalkOk[i + 1] && s_WalkOut[i] >= d.WalkOut - 0.01f && s_WalkOut[i + 1] >= d.WalkOut - 0.01f;
            Band(net, s, s_A, s_B, KerbTop + 0.003f, 0.004f, look.Snow, c.Lift, s_Keep);
        }

        // A kerb ramp: the walk's first 1.3 m sloping down to the gutter between two samples.
        private static void Ramp(Net net, Chain c, Sample a, Sample b, int side, Side d, float roadY)
        {
            Vector2 a0 = a.P + a.N * (side * d.Edge), b0 = b.P + b.N * (side * d.Edge);
            Vector2 a1 = a.P + a.N * (side * (d.Edge + 1.3f)), b1 = b.P + b.N * (side * (d.Edge + 1.3f));
            float la = c.Lift != null ? c.Lift.At(a.S) : 0f, lb = c.Lift != null ? c.Lift.At(b.S) : 0f;
            Vector3 p0 = To3(a0, roadY + 0.006f + la), p1 = To3(b0, roadY + 0.006f + lb), q1 = To3(b1, KerbTop + lb), q0 = To3(a1, KerbTop + la);
            Face(net.Out.Flat((p0 + q1) * 0.5f), p0, p1, q1, q0, Vector3.up, net.Look.Sidewalk);
        }

        // The corners of a junction: the kerb round each corner whose two sides are kerbed, the sidewalk round it (a fan
        // from where the two sidewalks' outer edges meet, or a strip along a straight side), snow where it's a residential
        // corner; and a plough's pile of snow on an unkerbed corner.
        private static void Corners(Net net, Joint j)
        {
            Look look = net.Look;
            foreach (Corner corner in j.Corners)
            {
                Arm a = j.Arms[corner.A], b = j.Arms[corner.B];
                Side sa = a.SL, sb = b.SR;
                if (corner.Path.Count < 2) continue;
                if (!sa.Kerb || !sb.Kerb)
                {
                    if (net.Full && corner.Rounded && !sa.Kerb && !sb.Kerb && a.W.Carriage && b.W.Carriage
                        && a.W.Cls <= Cls.Service && b.W.Cls <= Cls.Service) PloughPile(net, corner);
                    continue;
                }
                Chain line = ChainOf(corner.Path);
                if (line.Length < 0.05f) continue;
                List<Sample> s = net.Samples2;
                SamplesOf(line, 0f, line.Length, 0f, s);
                int n = s.Count;
                Fit(n);
                // The path runs from arm A to arm B with the road on its left: the kerb's top and the corner behind it
                // are on its right.
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = 0f;
                    s_B[i] = -KerbWidth;
                }
                Band(net, s, s_A, s_B, KerbTop, KerbTop, look.Kerb);
                float roadY = RoadY + j.Top.Order * 0.0003f;
                for (int i = 0; i + 1 < n; i++)
                {
                    Vector3 a0 = To3(s[i].P, roadY), b0 = To3(s[i + 1].P, roadY), b1 = To3(s[i + 1].P, KerbTop), a1 = To3(s[i].P, KerbTop);
                    Face(net.Out.Flat((a0 + b1) * 0.5f), a0, b0, b1, a1, To3(s[i].N, 0f), look.KerbFace);
                }
                if (!sa.Walk || !sb.Walk)
                {
                    for (int i = 0; i < n; i++)
                    {
                        s_A[i] = -KerbWidth;
                        s_B[i] = -(KerbWidth + 0.6f);
                    }
                    Band(net, s, s_A, s_B, KerbTop - 0.004f, 0.004f, look.Snow);
                    continue;
                }
                bool snowy = (sa.WalkIn > sa.Edge + KerbWidth + 0.05f || !a.W.Downtown) && !j.Downtown;
                Surf surf = snowy ? look.Snow : look.Sidewalk;
                Vector2 outerA = a.CutAt + Left(a.CutDir) * sa.WalkOut, outerB = b.CutAt - Left(b.CutDir) * sb.WalkOut;
                bool fanned = false;
                if (corner.Convex)
                {
                    // Where the two sidewalks' outer edges meet: the block's corner.
                    Vector2 rhs = -b.Nrm * sb.WalkOut - a.Nrm * sa.WalkOut;
                    float den = Cross(a.Dir, b.Dir);
                    if (Mathf.Abs(den) > 0.1f)
                    {
                        float u = Cross(rhs, b.Dir) / den, v = Cross(rhs, a.Dir) / den;
                        Vector2 q = j.At + a.Nrm * sa.WalkOut + a.Dir * u;
                        if (u > -1f && v > -1f && (q - j.At).sqrMagnitude < 40f * 40f && !InBuilding(net, Vector2.Lerp(q, s[n / 2].P, 0.5f), 0f))
                        {
                            s_Ring.Clear();
                            s_Ring.Add(outerA);
                            for (int i = 0; i < n; i++) s_Ring.Add(s[i].P - s[i].N * KerbWidth);
                            s_Ring.Add(outerB);
                            FanOpen(net, q, s_Ring, KerbTop, surf);
                            fanned = true;
                        }
                    }
                }
                if (!fanned)
                {
                    for (int i = 0; i < n; i++)
                    {
                        float t = line.Length > 0f ? s[i].S / line.Length : 0f;
                        float w = Mathf.Lerp(sa.WalkOut - sa.Edge, sb.WalkOut - sb.Edge, t);
                        s_A[i] = -KerbWidth;
                        s_B[i] = -w;
                    }
                    Band(net, s, s_A, s_B, KerbTop, KerbTop, surf);
                }
            }
        }

        /// <summary>Triangles from <paramref name="hub"/> to each pair of neighbouring points (not closing the ring), face up.</summary>
        private static void FanOpen(Net net, Vector2 hub, List<Vector2> pts, float y, in Surf surf)
        {
            Vector3 h = To3(hub, y);
            RaidMesh m = net.Out.Flat(h);
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 p = pts[i], q = pts[i + 1];
                if (Mathf.Abs(Cross(p - hub, q - hub)) < 1e-4f) continue;
                Tri(m, h, To3(p, y), To3(q, y), Vector3.up, surf);
            }
        }

        // The snow a plough leaves heaped on a corner as it turns.
        private static void PloughPile(Net net, Corner corner)
        {
            Vector2 mid = corner.Path[corner.Path.Count / 2];
            Vector2 out2 = (mid - corner.Centre);
            if (out2.sqrMagnitude < 1e-4f) return;
            Vector2 at = mid - out2.normalized * 1.3f;
            if (!net.Site.FreeBeside(at, 0.9f) || InBuilding(net, at, 0.5f)) return;
            System.Random rng = MapStyle.Rng(MapStyle.Key(at), 0x9113);
            float w = MapStyle.Range(rng, 1.4f, 2.4f), depth = MapStyle.Range(rng, 1.0f, 1.6f), h = MapStyle.Range(rng, 0.35f, 0.6f);
            float yaw = Mathf.Atan2(out2.x, out2.y) * Mathf.Rad2Deg;
            Vector3 at3 = To3(at, 0f);
            RaidMesh m = net.Out.Solid(at3);
            m.SetFrame(at3, yaw);
            DecorProps.SnowPile(m, rng, Vector3.zero, w, depth, h, 0f);
            m.ResetFrame();
            net.Site.Take(at, 1.2f);
        }
    }
}
