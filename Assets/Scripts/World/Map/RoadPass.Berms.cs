using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The snow the ploughs pushed off the carriageways (a low berm along each edge, broken at junctions, side roads'
    // mouths, crosswalks, driveways and here and there where someone dug through), and the driveways: an apron through
    // the kerb and a packed track up to the house.
    internal static partial class RoadPass
    {
        /// <summary>A driveway the roads laid from a street to a house: <see cref="At"/> on the kerb line,
        /// <see cref="Dir"/> towards the house, <see cref="Length"/> of packed snow past the apron (0: an apron only).</summary>
        internal struct DrivewaySpot
        {
            public Vector2 At, Dir;
            public float Length;
        }

        private sealed class Driveway
        {
            public Chain C;
            public int Side;
            public float S, Back, Depth;
            public Vector2 Edge, Dir;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Site, List<DrivewaySpot>> s_Driveways
            = new System.Runtime.CompilerServices.ConditionalWeakTable<Site, List<DrivewaySpot>>();

        /// <summary>The driveways the roads laid on a site (for parked cars), empty before the road pass ran.</summary>
        internal static IReadOnlyList<DrivewaySpot> Driveways(Site site)
            => s_Driveways.TryGetValue(site, out List<DrivewaySpot> list) ? list : (IReadOnlyList<DrivewaySpot>)System.Array.Empty<DrivewaySpot>();

        private static IEnumerable<string> Berms(Net net)
        {
            foreach (Chain c in net.Chains)
            {
                if (c.To - c.From < 1.5f) continue;
                Berm(net, c, 1);
                Berm(net, c, -1);
                if (net.Site.StepDue) yield return "berms and driveways";
            }
        }

        // One berm: a ridge of three lines (the foot on the road, the crest, the foot on the snow or the kerb), its height
        // wandering with the noise, tapering to nothing at its gaps and ends; the road-facing slope grey with grime.
        private static void Berm(Net net, Chain c, int side)
        {
            bool any = false;
            foreach (Part part in c.Parts) if (SideOf(part, side).Berm) any = true;
            if (!any) return;
            Way w = c.W;
            List<Sample> s = net.Samples;
            SamplesOf(c, c.From, c.To, 1.5f, s);
            int n = s.Count;
            if (n < 2) return;
            FitSides(n);
            int seed = Seed(w, side > 0 ? 0xBE1 : 0xBE2);
            List<Cut> cuts = side > 0 ? c.CutsL : c.CutsR;
            float[] h = s_WalkIn;
            for (int i = 0; i < n; i++)
            {
                ProfAt(c, side, s[i].S, out Prof d);
                Way pw = c.Parts[PartAt(c, s[i].S)].Run.W;
                bool kerbed = d.Kerb;
                bool cleared = kerbed && (pw.Downtown || (pw.Winter == Winter.Ploughed && pw.Town));
                float baseH = kerbed ? 0.2f : pw.Winter == Winter.Ploughed ? 0.36f : 0.26f;
                float crestOff = kerbed ? d.Edge - 0.18f : d.Edge + 0.4f;
                float outer = kerbed ? d.Edge + 0.12f : d.Edge + 1.0f + 0.4f * Noise(s[i].P, 3.5f, seed + 3);
                Vector2 crest = s[i].P + s[i].N * (side * crestOff), foot = s[i].P + s[i].N * (side * outer);
                bool gap = !d.Of.Berm || Noise(s[i].P, 12f, seed) < 0.15f
                    || (cleared && Noise(s[i].P, 9f, seed + 1) < 0.62f)
                    || CutAt(cuts, s[i].S) >= 0
                    || OnPavement(net, crest, c, 0.25f) || InBuilding(net, crest, 0.4f)
                    || (!kerbed && (OnPavement(net, foot, c, 0.1f) || InBuilding(net, foot, 0.2f)));
                h[i] = gap ? 0f : baseH * (0.55f + 0.8f * Noise(s[i].P, 4.5f, seed + 2));
                s_A[i] = side * (kerbed ? d.Edge - 0.6f : d.Edge - 0.12f);
                s_B[i] = side * crestOff;
                s_WalkOut[i] = side * outer;
                s_YB[i] = kerbed ? KerbTop + 0.01f : 0.004f;
            }
            // Taper into junctions and dead ends; where the chain runs off the chunk the neighbour carries the berm on.
            if (!AtSquareEdge(net, s[0].P)) h[0] = 0f;
            if (!AtSquareEdge(net, s[n - 1].P)) h[n - 1] = 0f;
            for (int i = 0; i < n; i++)
            {
                s_Edge[i] = s_YB[i];
                s_YA[i] = DetailY + 0.001f;
                s_YB[i] = DetailY + h[i];
                s_Keep[i] = i + 1 < n && (h[i] > 0f || h[i + 1] > 0f);
            }
            BandY(net, s, s_A, s_B, s_YA, s_YB, net.Look.BermGrime, c.Lift, s_Keep, true);
            for (int i = 0; i < n; i++)
            {
                s_A[i] = s_B[i];
                s_B[i] = s_WalkOut[i];
                s_YA[i] = DetailY + h[i];
                s_YB[i] = s_Edge[i];
            }
            BandY(net, s, s_A, s_B, s_YA, s_YB, net.Look.Snow, c.Lift, s_Keep, true);
        }

        private static bool AtSquareEdge(Net net, Vector2 p)
            => Mathf.Abs(Mathf.Abs(p.x) - net.Half) < 0.05f || Mathf.Abs(Mathf.Abs(p.y) - net.Half) < 0.05f;

        /// <summary>The chain nearest <paramref name="q"/> within <paramref name="within"/> metres of its line, how far
        /// along it, which side of it <paramref name="q"/> is (+1 left) and how far from its line.</summary>
        private static bool NearestChainTo(Net net, Vector2 q, float within, out Chain best, out float along, out int side, out float dist)
        {
            best = null;
            along = 0f;
            side = 1;
            dist = within;
            int stamp = net.NewStamp(net.PaveItems.Count + net.PatchItems.Count);
            net.PaveGrid.Query(q.x - within, q.y - within, q.x + within, q.y + within, net.Query, net.Stamp, stamp);
            foreach (int it in net.Query)
            {
                if (it >= net.PaveItems.Count) continue;
                (Chain c, int k) = net.PaveItems[it];
                Vector2 a = c.P[k];
                float len = c.Cum[k + 1] - c.Cum[k];
                if (len < 1e-4f) continue;
                Vector2 d = (c.P[k + 1] - a) / len;
                float t = Mathf.Clamp(Vector2.Dot(q - a, d), 0f, len);
                float s = c.Cum[k] + t;
                if (s < c.From || s > c.To) continue;
                float lat = Cross(d, q - a);
                float off = (q - (a + d * t)).magnitude;
                if (off >= dist) continue;
                dist = off;
                best = c;
                along = s;
                side = lat >= 0f ? 1 : -1;
            }
            return best != null;
        }

        /// <summary>The point and direction <paramref name="s"/> metres along a chain.</summary>
        private static void PointOn(Chain c, float s, out Vector2 p, out Vector2 dir)
        {
            int k = 0;
            while (k + 2 < c.P.Count && c.Cum[k + 1] < s) k++;
            float len = c.Cum[k + 1] - c.Cum[k];
            dir = len > 1e-5f ? (c.P[k + 1] - c.P[k]) / len : Vector2.right;
            p = c.P[k] + dir * Mathf.Clamp(s - c.Cum[k], 0f, len);
        }

        // A driveway for each house standing back from a kerbed or bermed street within 32 m, to one side of it, unless a
        // service road is mapped there already: a cut in the kerb and berm, an apron, a packed track to the house.
        private static void PlanDriveways(Net net)
        {
            Area area = net.Site.Area;
            foreach (Footprint f in area.Buildings)
            {
                string kind = f.Data.kind;
                if ((kind != "house" && kind != "trailer") || f.Size > 450f || !f.Inside(net.Half, 1f)) continue;
                if (!NearestChainTo(net, f.Middle, 32f, out Chain c, out float s, out int side, out float dist)) continue;
                ProfAt(c, side, s, out Prof d);
                Way pw = c.Parts[PartAt(c, s)].Run.W;
                if (pw.Cls < Cls.Primary || pw.Cls > Cls.Minor || (!d.Of.Berm && !d.Kerb) || dist - d.Edge < 5f) continue;
                PointOn(c, s, out Vector2 p, out Vector2 t);
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (Vector2 v in f.Pts)
                {
                    float along = Vector2.Dot(v - p, t);
                    lo = Mathf.Min(lo, along);
                    hi = Mathf.Max(hi, along);
                }
                float first = MapStyle.Pick(f.Data.id, 0xD41, 2) == 0 ? hi + 1.8f : lo - 1.8f;
                float second = first > 0f ? lo - 1.8f : hi + 1.8f;
                float sd = s + first;
                if (!DrivewayFits(net, c, side, sd)) sd = s + second;
                if (!DrivewayFits(net, c, side, sd)) continue;
                PointOn(c, sd, out Vector2 at, out Vector2 dir);
                ProfAt(c, side, sd, out d);
                Vector2 nrm = Left(dir) * side;
                float back = d.Walk ? d.WalkOut : d.Edge + 1.0f;
                Vector2 backPt = at + nrm * back;
                if (InBuilding(net, backPt, 0.3f)) continue;
                float room = RayToBuilding(net, backPt, nrm, 24f);
                float depth = room < 24f ? room - 1.2f : 9f;
                if (depth < 2.5f) depth = 0f;
                for (float k = 0.75f; k < depth; k += 1.5f)
                {
                    Vector2 q = backPt + nrm * k;
                    if (OnPavement(net, q, null, 0.5f) || InBuilding(net, q, 1.6f))
                    {
                        depth = Mathf.Max(0f, k - 1.5f);
                        break;
                    }
                }
                AddCut(c, sd - 2.2f, sd + 2.2f, CutKind.Driveway, side);
                net.Driveways.Add(new Driveway { C = c, Side = side, S = sd, Back = back, Depth = depth, Edge = at + nrm * d.Edge, Dir = nrm });
                for (float k = 0f; k <= back - d.Edge + depth; k += 1.5f) net.Site.Take(at + nrm * (d.Edge + k), 1.2f);
            }
        }

        // Room for a driveway at <paramref name="s"/>: clear of the chain's ends (its junctions) and side roads' mouths, of
        // other driveways on that side and of mapped service roads joining near it.
        private static bool DrivewayFits(Net net, Chain c, int side, float s)
        {
            if (s < c.From + 7f || s > c.To - 7f) return false;
            if (InMouth(c, side, s - 4f) || InMouth(c, side, s) || InMouth(c, side, s + 4f)) return false;
            foreach (Driveway o in net.Driveways)
                if (o.C == c && o.Side == side && Mathf.Abs(o.S - s) < 7.5f) return false;
            PointOn(c, s, out Vector2 p, out Vector2 _);
            foreach (Joint j in net.Joints)
            {
                if ((j.At - p).sqrMagnitude > 14f * 14f) continue;
                foreach (Arm a in j.Arms) if (a.W.Cls == Cls.Service) return false;
            }
            return true;
        }

        private static void DrawDriveways(Net net)
        {
            Look look = net.Look;
            var spots = new List<DrivewaySpot>(net.Driveways.Count);
            foreach (Driveway dw in net.Driveways)
            {
                Chain c = dw.C;
                ProfAt(c, dw.Side, dw.S, out Prof d);
                Vector2 nrm = dw.Dir, along = new Vector2(nrm.y, -nrm.x);
                float roadY = RoadY + c.W.Order * 0.0003f;
                Vector2 e = dw.Edge;
                if (d.Kerb)
                {
                    // The kerb dropped to the gutter, the apron sloping up to the sidewalk's level, then level to its back.
                    Vector3 a0 = To3(e - along * 2.2f, roadY + 0.007f), a1 = To3(e + along * 2.2f, roadY + 0.007f);
                    Vector3 b1 = To3(e + nrm * 0.9f + along * 1.6f, KerbTop + 0.001f), b0 = To3(e + nrm * 0.9f - along * 1.6f, KerbTop + 0.001f);
                    Face(net.Out.Flat((a0 + b1) * 0.5f), a0, a1, b1, b0, Vector3.up, look.Apron);
                    float far = (d.Walk ? d.WalkIn : d.Edge + KerbWidth + 0.6f) - d.Edge;
                    if (far > 0.95f)
                    {
                        Vector3 c0 = To3(e + nrm * far - along * 1.5f, KerbTop + 0.002f), c1 = To3(e + nrm * far + along * 1.5f, KerbTop + 0.002f);
                        Face(net.Out.Flat((b0 + c1) * 0.5f), b0, b1, c1, c0, Vector3.up, look.Apron);
                    }
                }
                else
                {
                    Vector3 a0 = To3(e - along * 2.0f, roadY + 0.004f), a1 = To3(e + along * 2.0f, roadY + 0.004f);
                    Vector3 b1 = To3(e + nrm * 1.0f + along * 1.6f, 0.007f), b0 = To3(e + nrm * 1.0f - along * 1.6f, 0.007f);
                    Face(net.Out.Flat((a0 + b1) * 0.5f), a0, a1, b1, b0, Vector3.up, look.Packed);
                }
                Vector2 start = e + nrm * (dw.Back - d.Edge);
                spots.Add(new DrivewaySpot { At = e, Dir = nrm, Length = dw.Back - d.Edge + dw.Depth });
                if (dw.Depth <= 0f) continue;
                Vector2 end = start + nrm * dw.Depth;
                float y = 0.007f;
                Vector3 s0 = To3(start - along * 1.5f, y), s1 = To3(start + along * 1.5f, y), e1 = To3(end + along * 1.4f, y), e0 = To3(end - along * 1.4f, y);
                Face(net.Out.Flat((s0 + e1) * 0.5f), s0, s1, e1, e0, Vector3.up, look.Packed);
                for (int t = -1; t <= 1; t += 2)
                {
                    Vector2 off = along * (t * 0.75f);
                    Vector3 r0 = To3(start + off - along * 0.16f, y + 0.0015f), r1 = To3(start + off + along * 0.16f, y + 0.0015f);
                    Vector3 r2 = To3(end + off + along * 0.16f - nrm * 0.4f, y + 0.0015f), r3 = To3(end + off - along * 0.16f - nrm * 0.4f, y + 0.0015f);
                    Face(net.Out.Flat((r0 + r2) * 0.5f), r0, r1, r2, r3, Vector3.up, look.Track);
                }
            }
            s_Driveways.Remove(net.Site);
            s_Driveways.Add(net.Site, spots);
        }
    }
}
