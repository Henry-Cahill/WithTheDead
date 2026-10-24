using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Kerbs, verges and sidewalks along the carriageways and round the junctions' corners, with ramps where crosswalks
    // meet them and gaps where driveways and side roads cross them. Everything at kerb height stops half way to the next
    // carriageway (whoever is on the far side takes the other half) and at the first wall, so sidewalks between two
    // roads meet in the middle and never spill onto either.
    internal static partial class RoadPass
    {
        private enum CutKind { Ramp, Driveway, Mouth }

        private struct Cut
        {
            public float S0, S1;
            public CutKind Kind;
        }

        private static bool[] s_KerbOk = new bool[256], s_WalkOk = new bool[256], s_Town = new bool[256], s_Ramp = new bool[256], s_Keep2 = new bool[256];
        private static float[] s_WalkIn = new float[256], s_WalkOut = new float[256], s_Edge = new float[256], s_Verge = new float[256];
        private static readonly List<Vector2> s_Ring = new List<Vector2>(32);

        private static void FitSides(int n)
        {
            Fit(n);
            if (s_KerbOk.Length >= n) return;
            int size = Mathf.NextPowerOfTwo(n);
            s_KerbOk = new bool[size];
            s_WalkOk = new bool[size];
            s_Town = new bool[size];
            s_Ramp = new bool[size];
            s_Keep2 = new bool[size];
            s_WalkIn = new float[size];
            s_WalkOut = new float[size];
            s_Edge = new float[size];
            s_Verge = new float[size];
        }

        // The walk's segments of one pass (0: the plain ones, 1: the ramps') into s_Keep2; false when there are none.
        private static bool RampPass(int n, int pass)
        {
            bool any = false;
            for (int i = 0; i + 1 < n; i++)
            {
                s_Keep2[i] = s_Keep[i] && s_Ramp[i] == (pass == 1);
                any |= s_Keep2[i];
            }
            return any;
        }

        // Where the walk starts at sample i in a pass: 1.3 m out on a ramp's segments.
        private static float WalkFrom(int i, int pass) => pass == 1 ? Mathf.Max(s_WalkIn[i], s_Edge[i] + 1.3f) : s_WalkIn[i];

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
                float s = c.Cum[k] + Mathf.Clamp(t, 0f, len);
                float lat = Cross(d, q - a);
                float l = EdgeAt(c, 1, s), r = EdgeAt(c, -1, s);
                if (lat > l + 0.5f || -lat > r + 0.5f) continue;
                float off = Mathf.Abs(lat - (l - r) * 0.5f);
                if (off >= bestLat) continue;
                bestLat = off;
                best = c;
                along = s;
            }
            return best != null;
        }

        // Where the kerbs open: side roads' mouths, ramps where crosswalks reach them, and driveways. Crosswalks go where
        // crossings are mapped (at the crossing, wherever it is along the street); a junction with none mapped within
        // 40 m keeps the crosswalks its shape suggested (signals, downtown).
        private static void PlanCuts(Net net)
        {
            foreach (Chain c in net.Chains)
                foreach (Mouth m in c.Mouths)
                {
                    float len = c.Length;
                    if (c.Closed && m.Lo < 0f)
                    {
                        AddCut(c, len + m.Lo, len, CutKind.Mouth, m.Side);
                        AddCut(c, 0f, m.Hi, CutKind.Mouth, m.Side);
                    }
                    else if (c.Closed && m.Hi > len)
                    {
                        AddCut(c, m.Lo, len, CutKind.Mouth, m.Side);
                        AddCut(c, 0f, m.Hi - len, CutKind.Mouth, m.Side);
                    }
                    else AddCut(c, m.Lo, m.Hi, CutKind.Mouth, m.Side);
                }
            bool anyMapped = false;
            foreach (Node n in net.Site.Area.Nodes)
                if (n.Data.kind == "crossing")
                {
                    anyMapped = true;
                    break;
                }
            if (anyMapped)
                foreach (Joint j in net.Joints)
                {
                    if (j.Link) continue;
                    bool near = false;
                    foreach (Node n in net.Site.Area.Nodes)
                        if (n.Data.kind == "crossing" && (n.At - j.At).sqrMagnitude < 40f * 40f)
                        {
                            near = true;
                            break;
                        }
                    if (near) foreach (Arm a in j.Arms) a.Crosswalk = false;
                }
            foreach (Node n in net.Site.Area.Nodes)
            {
                if (n.Data.kind != "crossing" || !net.Square.Contains(n.At)) continue;
                if (!NearestChain(net, n.At, out Chain c, out float s)) continue;
                if (c.W.Winter != Winter.Ploughed && c.W.Winter != Winter.Packed) continue;
                if (s - c.From < 1.6f || c.To - s < 1.6f || InMouth(c, 1, s) || InMouth(c, -1, s)) continue;
                bool taken = false;
                if (c.Crossings != null)
                    foreach (float o in c.Crossings) taken |= Mathf.Abs(o - s) < 4f;
                if (taken) continue;
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

        private static IEnumerable<string> SidesAndCorners(Net net)
        {
            foreach (Chain c in net.Chains)
            {
                if (c.To - c.From < 0.05f) continue;
                Sides(net, c, 1);
                Sides(net, c, -1);
                if (net.Site.StepDue) yield return "sidewalks";
            }
            foreach (Joint j in net.Joints)
                if (j.Corners.Count > 0 && j.HasPatch) Corners(net, j);
        }

        // One side of a chain: the kerb (its face and top), the verge, the sidewalk out to its edge, the first wall or
        // half way to the next carriageway, its winter cover, a snowy bank past it, and the ramps down to the crosswalks.
        // Nothing where another road takes the ground, nor across a side road's mouth.
        private static void Sides(Net net, Chain c, int side)
        {
            bool any = false;
            foreach (Part part in c.Parts)
            {
                Side sd = SideOf(part, side);
                if (sd.Kerb || sd.Walk) any = true;
            }
            if (!any) return;
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
                    SamplesOf(c, Mathf.Max(at, cut.S0), Mathf.Max(at, cut.S1), cut.Kind == CutKind.Mouth ? 0f : step, s, true);
                    at = Mathf.Max(at, cut.S1);
                }
            }
            SamplesOf(c, at, c.To, step, s, true);
            AddKnots(c, c.From, c.To, s);
            int n = s.Count;
            if (n < 2) return;
            FitSides(n);
            bool anyKerb = false, anyWalk = false;
            for (int i = 0; i < n; i++)
            {
                ProfAt(c, side, s[i].S, out Prof d);
                Vector2 nrm = s[i].N * side;
                float k = Mathf.Max(0.5f, nrm.magnitude);
                Vector2 unit = nrm / k;
                float reach = d.Walk ? d.WalkOut : d.Edge + KerbWidth + 0.6f;
                // Room before the next carriageway: half of it is this side's.
                float room = RayToRoad(net, s[i].P + nrm * d.Edge, unit, (reach - d.Edge) * k * 2f + 1f);
                float lim = d.Edge + room * 0.5f / k;
                Vector2 behind = s[i].P + nrm * (d.Edge + KerbWidth + 0.1f);
                bool blocked = room < (KerbWidth + 0.25f) * k || InBuilding(net, behind, 0f) || OnPavement(net, behind, c, -0.05f);
                s_Edge[i] = d.Edge;
                s_KerbOk[i] = d.Kerb && !blocked;
                float wi = d.WalkIn, wo = d.WalkOut;
                bool walk = d.Walk && !blocked;
                if (walk)
                {
                    float rb = RayToBuilding(net, s[i].P + nrm * wi, unit, (wo - wi) * k + 0.3f);
                    wo = Mathf.Min(wo, Mathf.Min(wi + (rb - 0.05f) / k, lim));
                    if (wo < wi + 0.35f) walk = false;
                }
                s_WalkOk[i] = walk;
                s_WalkIn[i] = wi;
                s_WalkOut[i] = wo;
                s_Verge[i] = d.Walk && d.WalkIn <= d.Edge + KerbWidth + 0.05f ? -1f
                    : Mathf.Min(d.Walk ? d.WalkIn : d.Edge + KerbWidth + 0.6f, Mathf.Max(lim, d.Edge + KerbWidth));
                Way w = c.Parts[PartAt(c, s[i].S)].Run.W;
                s_Town[i] = w.Downtown || (d.Of.Mapped && w.Town && w.Setback < 6f);
                anyKerb |= s_KerbOk[i];
                anyWalk |= walk;
            }
            float roadY = RoadY + c.W.Order * 0.0003f;
            // Segments: the kerb where both ends have it and no cut opens it; ramps take the walk's first 1.3 m.
            for (int i = 0; i + 1 < n; i++)
            {
                int cut = CutAt(cuts, (s[i].S + s[i + 1].S) * 0.5f);
                s_Keep[i] = s_KerbOk[i] && s_KerbOk[i + 1] && cut < 0;
                s_Ramp[i] = net.Full && cut == (int)CutKind.Ramp && s_KerbOk[i] && s_KerbOk[i + 1] && s_WalkOk[i] && s_WalkOk[i + 1];
            }
            for (int i = 0; i + 1 < n; i++)
                if (s_Ramp[i])
                    Ramp(net, c, s[i], s[i + 1], side, s_Edge[i], s_Edge[i + 1], roadY, i == 0 || !s_Ramp[i - 1], i + 2 >= n || !s_Ramp[i + 1]);
            if (anyKerb && net.Full)
            {
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = side * s_Edge[i];
                    s_B[i] = side * (s_Edge[i] + KerbWidth);
                }
                Band(net, s, s_A, s_B, KerbTop, KerbTop, look.Kerb, c.Lift, s_Keep);
                for (int i = 0; i + 1 < n; i++)
                {
                    if (!s_Keep[i]) continue;
                    float l0 = c.Lift != null ? c.Lift.At(s[i].S) : 0f, l1 = c.Lift != null ? c.Lift.At(s[i + 1].S) : 0f;
                    Vector2 a = s[i].P + s[i].N * (side * s_Edge[i]), b = s[i + 1].P + s[i + 1].N * (side * s_Edge[i + 1]);
                    Vector3 toward = To3(-s[i].N * side, 0f);
                    Vector3 a0 = To3(a, roadY + l0), b0 = To3(b, roadY + l1), b1 = To3(b, KerbTop + l1), a1 = To3(a, KerbTop + l0);
                    Face(net.Out.Flat((a0 + b1) * 0.5f), a0, b0, b1, a1, toward, look.KerbFace);
                }
                // The verge between the kerb and a set-back sidewalk (or, with no sidewalk, a strip of it).
                bool verge = false;
                for (int i = 0; i + 1 < n; i++)
                {
                    bool ok = s_Keep[i] && s_Verge[i] > s_Edge[i] + KerbWidth + 0.05f && s_Verge[i + 1] > s_Edge[i + 1] + KerbWidth + 0.05f;
                    s_Keep[i] = ok;
                    verge |= ok;
                }
                if (verge)
                {
                    for (int i = 0; i < n; i++)
                    {
                        s_A[i] = side * (s_Edge[i] + KerbWidth);
                        s_B[i] = side * Mathf.Max(s_Verge[i], s_Edge[i] + KerbWidth);
                        s_YA[i] = KerbTop - 0.004f;
                        s_YB[i] = s_WalkOk[i] ? KerbTop - 0.004f : 0.004f;
                    }
                    BandY(net, s, s_A, s_B, s_YA, s_YB, look.Snow, c.Lift, s_Keep, false);
                }
            }
            if (!anyWalk) return;
            for (int i = 0; i + 1 < n; i++)
            {
                int cut = CutAt(cuts, (s[i].S + s[i + 1].S) * 0.5f);
                s_Keep[i] = s_WalkOk[i] && s_WalkOk[i + 1] && cut != (int)CutKind.Mouth;
            }
            // Lite draws no kerb: a sidewalk at the kerb starts at the carriageway's edge.
            if (!net.Full)
                for (int i = 0; i < n; i++)
                    if (s_WalkIn[i] <= s_Edge[i] + KerbWidth + 0.05f) s_WalkIn[i] = s_Edge[i];
            // The walk, then (Full) its snow: each twice, the ramps' segments on their own from 1.3 m out (the ramp
            // takes the rest), so the walk's inner edge steps round a ramp instead of slanting into its neighbours.
            for (int pass = 0; pass < 2; pass++)
            {
                if (!RampPass(n, pass)) continue;
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = side * WalkFrom(i, pass);
                    s_B[i] = side * s_WalkOut[i];
                }
                Band(net, s, s_A, s_B, KerbTop, KerbTop, look.Sidewalk, c.Lift, s_Keep2);
            }
            if (!net.Full) return;
            int seed = Seed(c.W, side > 0 ? 0x51D : 0x51E);
            // Downtown, shovelled in front of the shops: snow left against the walls. Elsewhere snow over the sidewalk
            // with a trodden line down it.
            for (int i = 0; i + 1 < n; i++) s_YA[i] = s_Keep[i] ? 1f : 0f;
            for (int pass = 0; pass < 2; pass++)
            {
                if (!RampPass(n, pass)) continue;
                for (int i = 0; i < n; i++)
                {
                    float wIn = WalkFrom(i, pass), wOut = s_WalkOut[i];
                    s_A[i] = side * (s_Town[i] ? Mathf.Max(wIn, wOut - 0.2f - 0.5f * Noise(s[i].P, 3.5f, seed)) : wIn);
                    s_B[i] = side * wOut;
                }
                Band(net, s, s_A, s_B, KerbTop + 0.003f, KerbTop + 0.003f, look.Snow, c.Lift, s_Keep2);
            }
            for (int i = 0; i < n; i++)
            {
                float mid = (s_WalkIn[i] + s_WalkOut[i]) * 0.5f + 0.15f * (Noise(s[i].P, 4.5f, seed + 1) - 0.5f);
                float half = 0.22f + 0.12f * Noise(s[i].P, 2.25f, seed + 2);
                s_A[i] = side * (mid - half);
                s_B[i] = side * (mid + half);
            }
            for (int i = 0; i + 1 < n; i++)
                s_Keep[i] = s_YA[i] > 0.5f && !s_Ramp[i] && !s_Town[i] && !s_Town[i + 1] && Noise(s[i].P, 7f, seed + 3) > 0.3f;
            Band(net, s, s_A, s_B, KerbTop + 0.005f, KerbTop + 0.005f, look.Trodden, c.Lift, s_Keep);
            // A low bank of snow past the sidewalk's outer edge, where nothing cut it short.
            for (int i = 0; i < n; i++)
            {
                ProfAt(c, side, s[i].S, out Prof d);
                s_A[i] = side * s_WalkOut[i];
                s_B[i] = side * (s_WalkOut[i] + 0.35f);
                s_Verge[i] = s_WalkOut[i] >= d.WalkOut - 0.01f ? 1f : 0f;
            }
            for (int i = 0; i + 1 < n; i++)
                s_Keep[i] = s_YA[i] > 0.5f && s_Verge[i] > 0.5f && s_Verge[i + 1] > 0.5f;
            Band(net, s, s_A, s_B, KerbTop + 0.003f, 0.004f, look.Snow, c.Lift, s_Keep);
        }

        // A kerb ramp: the walk's first 1.3 m sloping down to the gutter between two samples; at the ramp's ends (first,
        // last), the kerb-high triangle that closes its side.
        private static void Ramp(Net net, Chain c, Sample a, Sample b, int side, float ea, float eb, float roadY, bool first, bool last)
        {
            Vector2 a0 = a.P + a.N * (side * ea), b0 = b.P + b.N * (side * eb);
            Vector2 a1 = a.P + a.N * (side * (ea + 1.3f)), b1 = b.P + b.N * (side * (eb + 1.3f));
            float la = c.Lift != null ? c.Lift.At(a.S) : 0f, lb = c.Lift != null ? c.Lift.At(b.S) : 0f;
            Vector3 p0 = To3(a0, roadY + 0.006f + la), p1 = To3(b0, roadY + 0.006f + lb), q1 = To3(b1, KerbTop + lb), q0 = To3(a1, KerbTop + la);
            RaidMesh m = net.Out.Flat((p0 + q1) * 0.5f);
            Face(m, p0, p1, q1, q0, Vector3.up, net.Look.Sidewalk);
            Vector3 along = To3(b.P - a.P, 0f);
            if (first) Tri(m, p0, To3(a0, KerbTop + la), q0, along, net.Look.KerbFace);
            if (last) Tri(m, p1, To3(b0, KerbTop + lb), q1, -along, net.Look.KerbFace);
        }

        // The corners of a junction: the kerb round each corner whose two sides are kerbed, the sidewalk round it (a fan
        // from where the two sidewalks' outer edges meet when that's close by and clear, else a strip along the kerb,
        // both stopping half way to any other carriageway), snow where it's a residential corner; and a plough's pile of
        // snow on an unkerbed corner.
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
                SamplesOf(line, 0f, line.Length, 1f, s);
                int n = s.Count;
                if (n < 2) continue;
                FitSides(n);
                bool walk = sa.Walk && sb.Walk;
                float wa = (walk ? sa.WalkOut : sa.Edge + KerbWidth + 0.6f) - a.HL, wb = (walk ? sb.WalkOut : sb.Edge + KerbWidth + 0.6f) - b.HR;
                wa = Mathf.Clamp(wa, KerbWidth + 0.3f, 6f);
                wb = Mathf.Clamp(wb, KerbWidth + 0.3f, 6f);
                // The path runs from arm A to arm B with the road on its left: the kerb's top and the corner behind it
                // are on its right.
                for (int i = 0; i < n; i++)
                {
                    Vector2 nrm = -s[i].N;
                    float k = Mathf.Max(0.5f, nrm.magnitude);
                    Vector2 unit = nrm / k;
                    float want = Mathf.Lerp(wa, wb, line.Length > 0f ? s[i].S / line.Length : 0f);
                    float room = RayToRoad(net, s[i].P, unit, want * k * 2f + 1f, j);
                    Vector2 behind = s[i].P + nrm * (KerbWidth + 0.1f);
                    s_KerbOk[i] = room > (KerbWidth + 0.25f) * k && !InBuilding(net, behind, 0f) && !OnPavement(net, behind, null, -0.05f);
                    float lim = room * 0.5f / k;
                    if (corner.Rounded && Mathf.Abs(Vector2.Distance(s[i].P, corner.Centre) - corner.Radius) < 0.6f)
                        lim = Mathf.Min(lim, corner.Radius - 0.05f);
                    float rb = RayToBuilding(net, s[i].P, unit, want * k + 0.3f);
                    s_WalkOut[i] = Mathf.Max(KerbWidth + 0.05f, Mathf.Min(want, Mathf.Min(lim, (rb - 0.05f) / k)));
                }
                for (int i = 0; i + 1 < n; i++) s_Keep[i] = s_KerbOk[i] && s_KerbOk[i + 1];
                if (net.Full)
                {
                    for (int i = 0; i < n; i++)
                    {
                        s_A[i] = 0f;
                        s_B[i] = -KerbWidth;
                    }
                    Band(net, s, s_A, s_B, KerbTop, KerbTop, look.Kerb, null, s_Keep);
                    float roadY = RoadY + j.Top.Order * 0.0003f;
                    for (int i = 0; i + 1 < n; i++)
                    {
                        if (!s_Keep[i]) continue;
                        Vector3 a0 = To3(s[i].P, roadY), b0 = To3(s[i + 1].P, roadY), b1 = To3(s[i + 1].P, KerbTop), a1 = To3(s[i].P, KerbTop);
                        Face(net.Out.Flat((a0 + b1) * 0.5f), a0, b0, b1, a1, To3(s[i].N, 0f), look.KerbFace);
                    }
                }
                // Lite draws no kerbs: whatever lies behind one starts at the kerb line.
                for (int i = 0; i < n; i++) s_A[i] = net.Full ? -KerbWidth : 0f;
                if (!walk)
                {
                    if (!net.Full) continue;
                    for (int i = 0; i < n; i++)
                    {
                        s_B[i] = -Mathf.Max(KerbWidth, s_WalkOut[i]);
                        s_YA[i] = KerbTop - 0.004f;
                        s_YB[i] = 0.004f;
                    }
                    BandY(net, s, s_A, s_B, s_YA, s_YB, look.Snow, null, s_Keep, false);
                    continue;
                }
                bool snowy = (sa.WalkIn > sa.Edge + KerbWidth + 0.05f || !a.W.Downtown) && !j.Downtown;
                Surf surf = snowy ? look.Snow : look.Sidewalk;
                if (!(corner.Convex && corner.Rounded && FanCorner(net, j, corner, a, b, sa, sb, s, surf)))
                {
                    for (int i = 0; i < n; i++) s_B[i] = -s_WalkOut[i];
                    Band(net, s, s_A, s_B, KerbTop, KerbTop, surf, null, s_Keep);
                }
            }
        }

        // A rounded corner's sidewalk as a fan from the block's corner (where the two sidewalks' outer edges meet), when
        // that's close and the fan stays clear of the other roads and of buildings.
        private static bool FanCorner(Net net, Joint j, Corner corner, Arm a, Arm b, Side sa, Side sb, List<Sample> s, in Surf surf)
        {
            float da = sa.WalkOut - a.HL, db = sb.WalkOut - b.HR;
            Vector2 rhs = -b.Nrm * (b.HR + db) - a.Nrm * (a.HL + da);
            float den = Cross(a.Dir, b.Dir);
            if (Mathf.Abs(den) < 0.1f) return false;
            float u = Cross(rhs, b.Dir) / den, v = Cross(rhs, a.Dir) / den;
            Vector2 q = j.At + a.Nrm * (a.HL + da) + a.Dir * u;
            int n = s.Count;
            Vector2 mid = s[n / 2].P;
            if (u < -1f || v < -1f || (q - mid).magnitude > (Mathf.Max(da, db) + corner.Radius) * 1.5f + 1f) return false;
            if (InBuilding(net, Vector2.Lerp(q, mid, 0.5f), 0f) || OnPavement(net, q, null, 0.2f)) return false;
            for (int i = 0; i < n; i++)
            {
                Vector2 m = Vector2.Lerp(q, s[i].P, 0.5f);
                if (OnPavement(net, m, null, 0f) && !InJunction(j, m)) return false;
            }
            Vector2 first = s[0].P, last = s[n - 1].P;
            Vector2 dA = a.Through ? a.Dir : a.CutDir, dB = b.Through ? b.Dir : b.CutDir;
            Vector2 outerA = first + Left(dA) * da, outerB = last - Left(dB) * db;
            s_Ring.Clear();
            s_Ring.Add(outerA);
            for (int i = 0; i < n; i++) s_Ring.Add(s[i].P - s[i].N * KerbWidth);
            s_Ring.Add(outerB);
            FanOpen(net, q, s_Ring, KerbTop, surf);
            return true;
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
            Vector2 out2 = mid - corner.Centre;
            if (out2.sqrMagnitude < 1e-4f) return;
            Vector2 at = mid - out2.normalized * 1.3f;
            if (!net.Site.FreeBeside(at, 0.9f) || InBuilding(net, at, 0.5f) || OnPavement(net, at, null, 0.3f)) return;
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
