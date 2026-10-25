using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Where the carriageways meet and how they string together. A joint is a point where two or more carriageway
    // pieces have a vertex: a link where one road simply carries on, a through junction where one road clearly carries
    // on past side roads (its chain isn't cut: its kerbs open at each side road's mouth, and a mouth patch fills the
    // kerb returns), or a full junction (its arms cut back to the kerb returns, one paved patch between them). Kerb
    // returns are worked out on the arms' real kerb lines (curved arms too), so their fillets sit on the kerbs. Chains
    // keep each part's cross-section, changing over a few metres between parts (held steady across a mouth).
    internal static partial class RoadPass
    {
        private const float Taper = 6f;    // metres over which a chain's cross-section changes from one part to the next

        #region Joints
        private static long SnapKey(Vector2 p)
            => ((long)Mathf.RoundToInt(p.x / SnapGrid) << 32) ^ (uint)Mathf.RoundToInt(p.y / SnapGrid);

        private static void Junctions(Net net)
        {
            FindJoints(net);
            MakeRuns(net);
            ArmCaps(net);
            foreach (Joint j in net.Joints) if (!j.Link) Shape(net, j);
            MakeChains(net);
            FindMouths(net);
            foreach (Chain c in net.Chains)
            {
                BuildProfile(c, 1);
                BuildProfile(c, -1);
            }
            foreach (Joint j in net.Joints) if (j.Through) MouthPatches(net, j);
            IndexPavement(net);
        }

        private static void FindJoints(Net net)
        {
            var touches = new Dictionary<long, List<(Way w, int v)>>();
            var keys = new List<long>();
            foreach (Way w in net.Ways)
            {
                if (!w.Carriage) continue;
                for (int v = 0; v < w.P.Count; v++)
                {
                    long key = SnapKey(w.P[v]);
                    if (!touches.TryGetValue(key, out List<(Way, int)> list))
                    {
                        touches[key] = list = new List<(Way, int)>(2);
                        keys.Add(key);
                    }
                    list.Add((w, v));
                }
            }
            foreach (long key in keys)
            {
                List<(Way w, int v)> list = touches[key];
                if (list.Count < 2) continue;
                var j = new Joint { At = list[0].w.P[list[0].v] };
                foreach ((Way w, int v) in list)
                {
                    AddArm(j, w, v, -1);
                    AddArm(j, w, v, 1);
                }
                if (j.Arms.Count < 2) continue;
                j.Arms.Sort((a, b) => a.Angle.CompareTo(b.Angle));
                j.Link = j.Arms.Count == 2 && Carries(j.Arms[0], j.Arms[1]);
                if (!j.Link && j.Arms.Count >= 3) FindThrough(j);
                net.Joints.Add(j);
                net.JointAt[key] = j;
            }
            foreach (Node n in net.Site.Area.Nodes)
            {
                if (n.Data.kind != "traffic_signals") continue;
                foreach (Joint j in net.Joints)
                    if (!j.Link && (j.At - n.At).sqrMagnitude < 15f * 15f) j.Signals = true;
            }
        }

        private static void AddArm(Joint j, Way w, int v, int step)
        {
            int n = v + step;
            if (n < 0 || n >= w.P.Count) return;
            Vector2 d = w.P[n] - w.P[v];
            if (d.sqrMagnitude < 1e-6f) return;
            d.Normalize();
            var a = new Arm
            {
                W = w, Vertex = v, Step = step, Dir = d, Nrm = Left(d), Angle = Mathf.Atan2(d.y, d.x),
                SL = step > 0 ? w.L : w.R, SR = step > 0 ? w.R : w.L,
            };
            a.HL = a.SL.Edge;
            a.HR = a.SR.Edge;
            // Traffic comes in along an arm unless it's a one-way street leaving the junction.
            a.Entering = !w.OneWay || step < 0;
            j.Arms.Add(a);
        }

        // A link: two arms that are ends of their pieces and one road carrying on, turning by 60 degrees at most.
        private static bool Carries(Arm a, Arm b)
        {
            bool endA = a.Vertex == 0 || a.Vertex == a.W.P.Count - 1, endB = b.Vertex == 0 || b.Vertex == b.W.P.Count - 1;
            if (!endA || !endB || Vector2.Dot(a.Dir, b.Dir) > -0.5f) return false;
            if (a.W == b.W && a.W.P.Count < 4) return false;
            return SameRoad(a, b);
        }

        // The same kind of road on both arms, its one-way traffic coming in by one and leaving by the other. (Their
        // cross-sections may differ: a chain changes from part to part.)
        private static bool SameRoad(Arm a, Arm b)
        {
            Way x = a.W, y = b.W;
            if (x.Cls != y.Cls || x.Winter != y.Winter || x.Bridge != y.Bridge || x.OneWay != y.OneWay || x.Link != y.Link
                || x.Concrete != y.Concrete) return false;
            return !x.OneWay || a.Entering != b.Entering;
        }

        // A through junction: the straightest pair of the junction's top class that is one road carrying on (turning
        // by 53 degrees at most); every other arm a lesser road, or one other arm of the same class (a T or a merge).
        // Two equal roads crossing keep a full patch.
        private static void FindThrough(Joint j)
        {
            int n = j.Arms.Count;
            Cls top = Cls.Path;
            foreach (Arm a in j.Arms) if (a.W.Cls < top) top = a.W.Cls;
            Arm bestA = null, bestB = null;
            float best = 2f;
            for (int i = 0; i < n; i++)
                for (int k = i + 1; k < n; k++)
                {
                    Arm a = j.Arms[i], b = j.Arms[k];
                    if (a.W.Cls != top || b.W.Cls != top) continue;
                    float d = Vector2.Dot(a.Dir, b.Dir);
                    if (d > -0.6f || d >= best || !SameRoad(a, b)) continue;
                    best = d;
                    bestA = a;
                    bestB = b;
                }
            if (bestA == null) return;
            int same = 0;
            foreach (Arm x in j.Arms) if (x != bestA && x != bestB && x.W.Cls == top) same++;
            if (same >= 2 || (same == 1 && n > 3)) return;
            j.ThroughA = bestA;
            j.ThroughB = bestB;
            bestA.Through = bestB.Through = true;
        }

        private static bool Carried(Joint j, Arm a) => j.Link || a.Through;

        private static long ArmKey(Way w, int v, bool forward) => ((long)w.Index << 22) | ((long)v << 1) | (forward ? 1L : 0L);

        // Each carriageway split into runs at the junctions it doesn't carry on through; each run's ends know their
        // junction arm or the run they carry on into.
        private static void MakeRuns(Net net)
        {
            var arms = new Dictionary<long, (Joint j, Arm a)>();
            foreach (Joint j in net.Joints)
                foreach (Arm a in j.Arms) arms[ArmKey(a.W, a.Vertex, a.Step > 0)] = (j, a);
            foreach (Way w in net.Ways)
            {
                if (!w.Carriage) continue;
                int start = 0;
                for (int v = 1; v < w.P.Count; v++)
                {
                    bool split = v == w.P.Count - 1;
                    if (!split && arms.TryGetValue(ArmKey(w, v, true), out (Joint j, Arm a) f) && !Carried(f.j, f.a)) split = true;
                    if (!split && arms.TryGetValue(ArmKey(w, v, false), out (Joint j, Arm a) b) && !Carried(b.j, b.a)) split = true;
                    if (!split) continue;
                    var r = new Run { W = w, V0 = start, V1 = v };
                    if (arms.TryGetValue(ArmKey(w, start, true), out (Joint j, Arm a) s))
                    {
                        s.a.Run = r;
                        if (!Carried(s.j, s.a))
                        {
                            r.J0 = s.j;
                            r.A0 = s.a;
                        }
                    }
                    if (arms.TryGetValue(ArmKey(w, v, false), out (Joint j, Arm a) e))
                    {
                        e.a.Run = r;
                        if (!Carried(e.j, e.a))
                        {
                            r.J1 = e.j;
                            r.A1 = e.a;
                        }
                    }
                    net.Runs.Add(r);
                    start = v;
                }
            }
            // Two runs ending where a road carries on (a link, or a through junction between two pieces) carry on into
            // each other.
            foreach (Joint j in net.Joints)
            {
                Arm a, b;
                if (j.Link)
                {
                    a = j.Arms[0];
                    b = j.Arms[1];
                }
                else if (j.Through)
                {
                    a = j.ThroughA;
                    b = j.ThroughB;
                }
                else continue;
                if (a.Run == null || b.Run == null || a.Run == b.Run) continue;
                bool endA = a.Step > 0 ? a.Run.V0 == a.Vertex : a.Run.V1 == a.Vertex;
                bool endB = b.Step > 0 ? b.Run.V0 == b.Vertex : b.Run.V1 == b.Vertex;
                if (!endA || !endB) continue;
                int ea = a.Step > 0 ? 0 : 1, eb = b.Step > 0 ? 0 : 1;
                if (ea == 0) { a.Run.Next0 = b.Run; a.Run.NextEnd0 = eb; } else { a.Run.Next1 = b.Run; a.Run.NextEnd1 = eb; }
                if (eb == 0) { b.Run.Next0 = a.Run; b.Run.NextEnd0 = ea; } else { b.Run.Next1 = a.Run; b.Run.NextEnd1 = ea; }
            }
        }

        // How far each junction may reach along its arms: just under half way to the next junction on the arm's piece
        // (most of the way when the piece only runs straight through it, past a side road's mouth), nearly to the
        // piece's end where there is none (and 40 m at most).
        private static void ArmCaps(Net net)
        {
            var stops = new Dictionary<long, bool>();
            foreach (Joint j in net.Joints)
            {
                if (j.Link) continue;
                foreach (Arm a in j.Arms)
                {
                    long key = ((long)a.W.Index << 22) | (long)a.Vertex;
                    stops[key] = (!stops.TryGetValue(key, out bool through) || through) && a.Through;
                }
            }
            foreach (Joint j in net.Joints)
            {
                if (j.Link) continue;
                foreach (Arm a in j.Arms)
                {
                    Way w = a.W;
                    float s0 = w.Cum[a.Vertex], d = -1f;
                    for (int k = a.Vertex + a.Step; k >= 0 && k < w.P.Count; k += a.Step)
                    {
                        if (!stops.TryGetValue(((long)w.Index << 22) | (long)k, out bool through)) continue;
                        d = Mathf.Abs(w.Cum[k] - s0) * (through ? 0.8f : 0.48f);
                        break;
                    }
                    if (d < 0f) d = (a.Step > 0 ? w.Length - s0 : s0) - 0.3f;
                    a.Cap = Mathf.Clamp(d, 0.2f, 40f);
                }
            }
        }
        #endregion

        #region Kerb returns
        /// <summary>An arm's kerb line on one side, out from the junction (and 3 m back past it): its points, and the
        /// arc length along the arm's line (from the junction) each lies at.</summary>
        private sealed class KerbLine
        {
            public readonly List<Vector2> P = new List<Vector2>(16);
            public readonly List<float> T = new List<float>(16);

            /// <summary>The point at arc length <paramref name="t"/> (straight on past either end).</summary>
            public Vector2 At(float t)
            {
                int n = P.Count;
                if (n == 0) return Vector2.zero;
                if (n == 1) return P[0];
                int k = 0;
                while (k + 2 < n && T[k + 1] < t) k++;
                float span = T[k + 1] - T[k];
                float f = span > 1e-5f ? (t - T[k]) / span : 0f;
                return P[k] + (P[k + 1] - P[k]) * f;
            }
        }

        private static readonly KerbLine s_KerbA = new KerbLine(), s_KerbB = new KerbLine();

        /// <summary>The kerb line of <paramref name="a"/> on its left (<paramref name="side"/> +1) or right (-1),
        /// <paramref name="half"/> metres off its line, out to <paramref name="reach"/> metres along it. Its first point
        /// is mitred with the road carrying on through the junction, as the through road's ribbon is.</summary>
        private static void ArmKerb(Joint j, Arm a, int side, float half, float reach, KerbLine into)
        {
            into.P.Clear();
            into.T.Clear();
            Way w = a.W;
            Vector2 n0 = a.Nrm;
            Arm other = a.Through ? (a == j.ThroughA ? j.ThroughB : j.ThroughA) : null;
            if (other != null)
            {
                Vector2 m = Left(-other.Dir) + a.Nrm;
                if (m.sqrMagnitude > 1e-6f)
                {
                    m.Normalize();
                    n0 = m / Mathf.Max(0.5f, Vector2.Dot(m, a.Nrm));
                }
            }
            Vector2 p0 = w.P[a.Vertex] + n0 * (side * half);
            into.P.Add(p0 - a.Dir * 3f);
            into.T.Add(-3f);
            into.P.Add(p0);
            into.T.Add(0f);
            float t = 0f;
            for (int k = a.Vertex; ;)
            {
                int nk = k + a.Step;
                if (nk < 0 || nk >= w.P.Count) break;
                Vector2 p = w.P[nk], d = p - w.P[k];
                float len = d.magnitude;
                if (len < 1e-4f)
                {
                    k = nk;
                    continue;
                }
                t += len;
                Vector2 dIn = d / len, nrm = Left(dIn);
                int nn = nk + a.Step;
                if (nn >= 0 && nn < w.P.Count)
                {
                    Vector2 dOut = w.P[nn] - p;
                    if (dOut.sqrMagnitude > 1e-8f)
                    {
                        Vector2 m = Left(dIn) + Left(dOut.normalized);
                        if (m.sqrMagnitude > 1e-6f)
                        {
                            m.Normalize();
                            nrm = m / Mathf.Max(0.5f, Vector2.Dot(m, Left(dIn)));
                        }
                    }
                }
                into.P.Add(p + nrm * (side * half));
                into.T.Add(t);
                if (t >= reach) break;
                k = nk;
            }
            if (into.P.Count == 2)
            {
                // A stub: straight on along the arm.
                into.P.Add(p0 + a.Dir * Mathf.Max(1f, reach));
                into.T.Add(Mathf.Max(1f, reach));
            }
        }

        /// <summary>Where kerb line <paramref name="A"/> first crosses <paramref name="B"/> (first along A): the
        /// point, the arc lengths along each, and each line's direction there.</summary>
        private static bool KerbsMeet(KerbLine A, KerbLine B, out Vector2 at, out float s, out float u, out Vector2 dA, out Vector2 dB)
        {
            at = Vector2.zero;
            s = u = 0f;
            dA = dB = Vector2.zero;
            float best = float.MaxValue;
            for (int i = 0; i + 1 < A.P.Count; i++)
            {
                Vector2 a0 = A.P[i], ea = A.P[i + 1] - a0;
                for (int k = 0; k + 1 < B.P.Count; k++)
                {
                    Vector2 b0 = B.P[k], eb = B.P[k + 1] - b0;
                    float den = Cross(ea, eb);
                    if (Mathf.Abs(den) < 1e-6f) continue;
                    Vector2 w = b0 - a0;
                    float fa = Cross(w, eb) / den, fb = Cross(w, ea) / den;
                    if (fa < 0f || fa > 1f || fb < 0f || fb > 1f) continue;
                    float sa = A.T[i] + (A.T[i + 1] - A.T[i]) * fa;
                    if (sa >= best) continue;
                    best = sa;
                    at = a0 + ea * fa;
                    s = sa;
                    u = B.T[k] + (B.T[k + 1] - B.T[k]) * fb;
                    dA = ea.normalized;
                    dB = eb.normalized;
                }
            }
            return best < float.MaxValue && dA.sqrMagnitude > 0.5f && dB.sqrMagnitude > 0.5f;
        }

        /// <summary>The points of <paramref name="k"/> lying strictly between arc lengths <paramref name="t0"/> and
        /// <paramref name="t1"/>, added in order from t0 to t1.</summary>
        private static void KerbBetween(KerbLine k, float t0, float t1, List<Vector2> into)
        {
            if (Mathf.Abs(t1 - t0) < 0.05f) return;
            if (t0 < t1)
            {
                for (int i = 0; i < k.P.Count; i++)
                    if (k.T[i] > t0 + 0.05f && k.T[i] < t1 - 0.05f) into.Add(k.P[i]);
            }
            else
            {
                for (int i = k.P.Count - 1; i >= 0; i--)
                    if (k.T[i] < t0 - 0.05f && k.T[i] > t1 + 0.05f) into.Add(k.P[i]);
            }
        }

        private static void AddPoint(List<Vector2> path, Vector2 p)
        {
            if (path.Count == 0 || (p - path[path.Count - 1]).sqrMagnitude > 1e-4f) path.Add(p);
        }

        // The corner radius of the kerb (or the paved edge) between two arms.
        private static float CornerRadius(Joint j, Arm a, Arm b)
        {
            if (j.Arms.Count == 2) return a.W.Cls <= Cls.Tertiary ? 9f : 6f;
            Cls lo = a.W.Cls > b.W.Cls ? a.W.Cls : b.W.Cls;
            if (lo == Cls.Track || lo == Cls.Service) return 2.2f;
            if (a.SL.Kerb && b.SR.Kerb) return a.W.Downtown || b.W.Downtown ? 3.5f : 4.5f;
            Cls hi = a.W.Cls < b.W.Cls ? a.W.Cls : b.W.Cls;
            return hi <= Cls.Trunk ? 10f : 7f;
        }

        // A junction's kerb returns: between each pair of neighbouring arms (but across a through road), a fillet of
        // the corner's radius between the two kerb lines where they meet; each arm cut where the returns beside it are
        // done (a through arm isn't cut: its kerb opens instead); a full junction's paved patch run round them
        // counter-clockwise.
        /// <summary>Generate kerb returns and pavement patch for a junction.</summary>
        private static void Shape(Net net, Joint j)
        {
            int n = j.Arms.Count;
            if (j.Through)
            {
                // Across the mouth the through road keeps the wider of its two parts' kerb lines (its chain does too).
                Arm x = j.ThroughA, y = j.ThroughB;
                float l = Mathf.Max(x.HL, y.HR), r = Mathf.Max(x.HR, y.HL);
                x.HL = y.HR = l;
                x.HR = y.HL = r;
            }
            var tl = new float[n];
            var tr = new float[n];
            var corners = new Corner[n];
            j.All = corners;
            KerbLine ka = s_KerbA, kb = s_KerbB;
            for (int i = 0; i < n; i++)
            {
                Arm a = j.Arms[i], b = j.Arms[(i + 1) % n];
                var c = new Corner { A = i, B = (i + 1) % n };
                corners[i] = c;
                if (j.Through && ((a == j.ThroughA && b == j.ThroughB) || (a == j.ThroughB && b == j.ThroughA)))
                {
                    c.Skip = true;
                    continue;
                }
                float phi = Mathf.Repeat(b.Angle - a.Angle, Mathf.PI * 2f);
                if (phi < 0.17f || phi > Mathf.PI * 2f - 0.01f) continue;   // arms on top of each other: they just overlap
                if (phi > Mathf.PI - 0.17f) continue;                       // straight on, or the outside of a bend
                c.Convex = true;
                ArmKerb(j, a, 1, a.HL, a.Cap + 3f, ka);
                ArmKerb(j, b, -1, b.HR, b.Cap + 3f, kb);
                if (!KerbsMeet(ka, kb, out Vector2 meet, out float s, out float u, out Vector2 dA, out Vector2 dB))
                {
                    // The kerb lines as straight lines from the junction.
                    Vector2 rhs = -b.Nrm * b.HR - a.Nrm * a.HL;
                    float den = Cross(a.Dir, b.Dir);
                    s = Cross(rhs, b.Dir) / den;
                    u = Cross(rhs, a.Dir) / den;
                    meet = j.At + a.Nrm * a.HL + a.Dir * s;
                    dA = a.Dir;
                    dB = b.Dir;
                }
                float local = Mathf.Repeat(Mathf.Atan2(dB.y, dB.x) - Mathf.Atan2(dA.y, dA.x), Mathf.PI * 2f);
                if (local < 0.17f || local > Mathf.PI - 0.17f)
                {
                    local = phi;
                    dA = a.Dir;
                    dB = b.Dir;
                }
                float r = CornerRadius(j, a, b) * Mathf.Clamp(local / (Mathf.PI * 0.5f), 0.45f, 1.1f);
                float q = r / Mathf.Tan(local * 0.5f);
                if (s + q > a.Cap || u + q > b.Cap)
                {
                    q = Mathf.Min(a.Cap - s, b.Cap - u);
                    if (q <= 0.05f) q = 0f;
                    r = q * Mathf.Tan(local * 0.5f);
                }
                tl[i] = Mathf.Clamp(s + q, 0f, a.Cap);
                tr[(i + 1) % n] = Mathf.Clamp(u + q, 0f, b.Cap);
                c.Meet = meet;
                c.Sharp = s <= a.Cap && u <= b.Cap && s > -2f && u > -2f;
                if (r > 0.3f && s + q <= a.Cap + 0.01f && u + q <= b.Cap + 0.01f)
                {
                    c.Rounded = true;
                    c.Radius = r;
                    c.DirA = dA;
                    c.DirB = dB;
                    c.Tangent = q;
                    c.Centre = meet + (dA + dB).normalized * (r / Mathf.Sin(local * 0.5f));
                }
            }
            for (int i = 0; i < n; i++)
            {
                Arm a = j.Arms[i];
                a.TanL = tl[i];
                a.TanR = tr[i];
                if (a.Through) a.Trim = 0f;
                else
                {
                    float t = Mathf.Max(tl[i], tr[i]);
                    if (n >= 3) t = Mathf.Max(t, 0.5f);
                    a.Trim = Mathf.Min(Mathf.Max(t, 0.3f), Mathf.Max(0.2f, a.Cap));
                }
                CutArm(a);
            }
            for (int i = 0; i < n; i++)
            {
                Corner c = corners[i];
                if (c.Skip) continue;
                CornerPath(j, c, tl[c.A], tr[c.B]);
                j.Corners.Add(c);
            }
            if (!j.Through)
            {
                var poly = new List<Vector2>(n * 8);
                for (int i = 0; i < n; i++)
                {
                    Corner c = corners[i];
                    for (int k = 0; k + 1 < c.Path.Count; k++) AddPoint(poly, c.Path[k]);
                    AddPoint(poly, j.Arms[c.B].EndR);
                }
                if (poly.Count > 2 && (poly[0] - poly[poly.Count - 1]).sqrMagnitude <= 1e-4f) poly.RemoveAt(poly.Count - 1);
                if (poly.Count >= 3 && MapGeometry.SignedArea(poly) < 0f) poly.Reverse();
                j.Patch = poly;
                j.Bounds = MapGeometry.Bounds(poly);
            }
            Arm top = j.Arms[0];
            foreach (Arm a in j.Arms) if (a.W.Cls < top.W.Cls || (a.W.Cls == top.W.Cls && a.W.Winter < top.W.Winter)) top = a;
            j.Top = top.W;
            j.Downtown = top.W.Downtown;
            foreach (Arm a in j.Arms) a.Major = a.W.Cls == top.W.Cls;
            bool allEqual = true;
            foreach (Arm a in j.Arms) if (!a.Major) allEqual = false;
            // Two equal streets meeting in a T: the stem stops, the street running through doesn't.
            Arm stem = null;
            if (allEqual && n == 3)
                for (int i = 0; i < 3; i++)
                {
                    Arm x = j.Arms[(i + 1) % 3], y = j.Arms[(i + 2) % 3];
                    if (Vector2.Dot(x.Dir, y.Dir) < -0.85f) stem = j.Arms[i];
                }
            foreach (Arm a in j.Arms)
            {
                bool road = a.W.Cls <= Cls.Minor;
                if (j.Through)
                {
                    a.StopBar = a.Entering && road && !a.Through;
                    a.Crosswalk = road && !a.Through && a.SL.Walk && a.SR.Walk && (j.Signals || j.Downtown);
                    continue;
                }
                a.StopBar = a.Entering && road && n >= 3 && (j.Signals || !a.Major || a == stem || (allEqual && n >= 4));
                a.Crosswalk = road && n >= 3 && a.SL.Walk && a.SR.Walk && (j.Signals || j.Downtown);
            }
        }

        // The kerb line round a corner, from where it leaves arm A (A's trim, or a through arm's tangent) in along A's
        // kerb, round the fillet and out along B's kerb to where it leaves B.
        private static void CornerPath(Joint j, Corner c, float tanA, float tanB)
        {
            Arm a = j.Arms[c.A], b = j.Arms[c.B];
            KerbLine ka = s_KerbA, kb = s_KerbB;
            float startA = a.Through ? tanA : a.Trim, endB = b.Through ? tanB : b.Trim;
            ArmKerb(j, a, 1, a.HL, Mathf.Max(startA, tanA) + 1f, ka);
            ArmKerb(j, b, -1, b.HR, Mathf.Max(endB, tanB) + 1f, kb);
            Vector2 ea = a.Through ? ka.At(startA) : a.EndL, eb = b.Through ? kb.At(endB) : b.EndR;
            List<Vector2> path = c.Path;
            path.Add(ea);
            if (c.Rounded)
            {
                Vector2 ta = ka.At(tanA), tb = kb.At(tanB);
                KerbBetween(ka, startA, tanA, path);
                AddPoint(path, ta);
                // The fillet, worked out where the kerbs meet, bent a little so it ends exactly on them.
                Vector2 fa = c.Meet + c.DirA * c.Tangent, fb = c.Meet + c.DirB * c.Tangent;
                Vector2 offA = ta - fa, offB = tb - fb;
                float a0 = Mathf.Atan2(fa.y - c.Centre.y, fa.x - c.Centre.x), a1 = Mathf.Atan2(fb.y - c.Centre.y, fb.x - c.Centre.x);
                float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(sweep) * c.Radius / 0.9f), 2, 12);
                c.Arc.Clear();
                c.Arc.Add(ta);
                for (int k = 1; k < steps; k++)
                {
                    float f = (float)k / steps, ang = a0 + sweep * f;
                    Vector2 p = c.Centre + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * c.Radius + Vector2.Lerp(offA, offB, f);
                    AddPoint(path, p);
                    c.Arc.Add(p);
                }
                AddPoint(path, tb);
                c.Arc.Add(tb);
                KerbBetween(kb, tanB, endB, path);
            }
            else if (c.Convex && c.Sharp)
            {
                if ((c.Meet - ea).sqrMagnitude > 0.01f && (c.Meet - eb).sqrMagnitude > 0.01f) AddPoint(path, c.Meet);
            }
            else if (!c.Convex)
            {
                // Straight on or round the outside of a bend: through the point where the two kerb lines meet, when
                // that's near the patch.
                if (KerbsMeet(ka, kb, out Vector2 m, out float s, out float u, out _, out _))
                {
                    if (s < startA && u < endB && s > -3f && u > -3f) AddPoint(path, m);
                }
                else
                {
                    Vector2 rhs = -b.Nrm * b.HR - a.Nrm * a.HL;
                    float den = Cross(a.Dir, b.Dir);
                    if (Mathf.Abs(den) > 0.05f)
                    {
                        s = Cross(rhs, b.Dir) / den;
                        u = Cross(rhs, a.Dir) / den;
                        if (s < startA && u < endB && s > -8f && u > -8f) AddPoint(path, j.At + a.Nrm * a.HL + a.Dir * s);
                    }
                }
            }
            AddPoint(path, eb);
            Tidy(path);
        }

        /// <summary>Drops the points where a path doubles back on itself over a short step (where a tangent point all but
        /// coincides with an arm's end), until none are left.</summary>
        private static void Tidy(List<Vector2> path)
        {
            for (int guard = 0; guard < 64 && path.Count > 2; guard++)
            {
                int drop = -1;
                for (int i = 1; i + 1 < path.Count; i++)
                {
                    Vector2 ab = path[i] - path[i - 1], bc = path[i + 1] - path[i];
                    float la = ab.magnitude, lb = bc.magnitude;
                    if (la < 1e-4f || lb < 1e-4f || (Vector2.Dot(ab, bc) < -0.2f * la * lb && Mathf.Min(la, lb) < 1.2f))
                    {
                        drop = i;
                        break;
                    }
                }
                if (drop < 0) return;
                path.RemoveAt(drop);
            }
        }

        /// <summary>True when no two edges of the closed polygon cross.</summary>
        private static bool IsSimple(List<Vector2> poly)
        {
            int n = poly.Count;
            if (n < 3) return false;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % n];
                for (int k = i + 2; k < n; k++)
                {
                    if (i == 0 && k == n - 1) continue;
                    Vector2 c = poly[k], d = poly[(k + 1) % n];
                    float o1 = Cross(b - a, c - a), o2 = Cross(b - a, d - a), o3 = Cross(d - c, a - c), o4 = Cross(d - c, b - c);
                    if (o1 * o2 < 0f && o3 * o4 < 0f) return false;
                }
            }
            return true;
        }

        // Where an arm leaves the patch: its line's point and direction at the trim, and the carriageway's corners there.
        private static void CutArm(Arm a)
        {
            Way w = a.W;
            float s0 = w.Cum[a.Vertex];
            float target = a.Step > 0 ? s0 + a.Trim : s0 - a.Trim;
            target = Mathf.Clamp(target, 0f, w.Length);
            PointAt(w, target, out Vector2 p, out Vector2 d);
            if (a.Step < 0) d = -d;
            a.CutAt = p;
            a.CutDir = d;
            Vector2 left = Left(d);
            a.EndL = p + left * a.HL;
            a.EndR = p - left * a.HR;
        }
        #endregion

        #region Chains
        /// <summary>Create chains of connected road runs for later profiling.</summary>
        private static void MakeChains(Net net)
        {
            foreach (Run start in net.Runs)
            {
                if (start.Seen) continue;
                // Walk back to where the chain begins (a run end that doesn't carry on), or all the way round a loop.
                Run cur = start;
                bool rev = false;
                bool closed = false;
                for (int guard = 0; guard < 100000; guard++)
                {
                    Run back = rev ? cur.Next1 : cur.Next0;
                    int backEnd = rev ? cur.NextEnd1 : cur.NextEnd0;
                    if (back == null) break;
                    cur = back;
                    rev = backEnd == 0;
                    if (cur == start)
                    {
                        closed = true;
                        break;
                    }
                }
                var c = new Chain { Closed = closed, Id = net.Chains.Count };
                for (int guard = 0; guard < 100000 && cur != null && !cur.Seen; guard++)
                {
                    cur.Seen = true;
                    var part = new Part { Run = cur, Rev = rev, S0 = c.P.Count == 0 ? 0f : c.Length };
                    Way w = cur.W;
                    int from = rev ? cur.V1 : cur.V0, to = rev ? cur.V0 : cur.V1, step = rev ? -1 : 1;
                    for (int v = from; ; v += step)
                    {
                        if (c.P.Count == 0 || (w.P[v] - c.P[c.P.Count - 1]).sqrMagnitude > 1e-4f)
                        {
                            if (c.P.Count > 0) c.Length += Vector2.Distance(c.P[c.P.Count - 1], w.P[v]);
                            c.P.Add(w.P[v]);
                        }
                        if (v == to) break;
                    }
                    part.S1 = c.Length;
                    c.Parts.Add(part);
                    Run next = rev ? cur.Next0 : cur.Next1;
                    int nextEnd = rev ? cur.NextEnd0 : cur.NextEnd1;
                    cur = next;
                    rev = nextEnd == 1;
                }
                if (c.P.Count < 2 || c.Length < 0.05f) continue;
                Part first = c.Parts[0], last = c.Parts[c.Parts.Count - 1];
                c.W = first.Run.W;
                if (!closed)
                {
                    c.A0 = first.Rev ? first.Run.A1 : first.Run.A0;
                    c.J0 = first.Rev ? first.Run.J1 : first.Run.J0;
                    c.A1 = last.Rev ? last.Run.A0 : last.Run.A1;
                    c.J1 = last.Rev ? last.Run.J0 : last.Run.J1;
                    c.Trim0 = c.A0 != null ? c.A0.Trim : 0f;
                    c.Trim1 = c.A1 != null ? c.A1.Trim : 0f;
                    if (c.A0 != null)
                    {
                        c.A0.Chain = c;
                        c.A0.AtStart = true;
                    }
                    if (c.A1 != null)
                    {
                        c.A1.Chain = c;
                        c.A1.AtStart = false;
                    }
                }
                c.Cum = new float[c.P.Count];
                for (int i = 1; i < c.P.Count; i++) c.Cum[i] = c.Cum[i - 1] + Vector2.Distance(c.P[i - 1], c.P[i]);
                c.Length = c.Cum[c.P.Count - 1];
                c.N = new Vector2[c.P.Count];
                for (int i = 0; i < c.P.Count; i++)
                {
                    bool wrap = closed && (i == 0 || i == c.P.Count - 1);
                    Vector2 prev = i > 0 ? c.P[i - 1] : wrap ? c.P[c.P.Count - 2] : c.P[i];
                    Vector2 next = i + 1 < c.P.Count ? c.P[i + 1] : wrap ? c.P[1] : c.P[i];
                    Vector2 dPrev = (c.P[i] - prev).normalized, dNext = (next - c.P[i]).normalized;
                    if (i == 0 && !wrap) dPrev = dNext;
                    if (i == c.P.Count - 1 && !wrap) dNext = dPrev;
                    Vector2 nPrev = Left(dPrev), nNext = Left(dNext), m = nPrev + nNext;
                    m = m.sqrMagnitude > 1e-6f ? m.normalized : nNext;
                    float k = Vector2.Dot(m, nNext);
                    c.N[i] = m / Mathf.Max(0.5f, float.IsNaN(k) ? 1f : k);
                }
                net.Chains.Add(c);
            }
        }

        /// <summary>A part's side as seen along its chain (+1 left, -1 right).</summary>
        private static Side SideOf(Part p, int side)
        {
            Way w = p.Run.W;
            return p.Rev ? (side > 0 ? w.R : w.L) : (side > 0 ? w.L : w.R);
        }

        /// <summary>The index of the part of <paramref name="c"/> that <paramref name="s"/> metres along it falls in.</summary>
        private static int PartAt(Chain c, float s)
        {
            int lo = 0, hi = c.Parts.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (c.Parts[mid].S1 < s) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        /// <summary>The chain's direction <paramref name="s"/> metres along it (averaged over a vertex).</summary>
        private static Vector2 ChainDir(Chain c, float s)
        {
            PointOn(c, Mathf.Clamp(s - 0.3f, 0f, c.Length), out Vector2 p0, out Vector2 d0);
            PointOn(c, Mathf.Clamp(s + 0.3f, 0f, c.Length), out Vector2 p1, out Vector2 d1);
            Vector2 d = p1 - p0;
            if (d.sqrMagnitude > 1e-8f) return d.normalized;
            d = d0 + d1;
            return d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.right;
        }

        // Each through junction's place on its chain, and its mouths: the chain's sides that side roads join, open from
        // one through arm's kerb return to the other's.
        private static void FindMouths(Net net)
        {
            var where = new Dictionary<long, (Chain c, float s)>();
            foreach (Chain c in net.Chains)
                foreach (Part part in c.Parts)
                {
                    Run r = part.Run;
                    for (int v = r.V0; v <= r.V1; v++)
                    {
                        long key = ((long)r.W.Index << 22) | (long)v;
                        if (where.ContainsKey(key)) continue;
                        float s = part.S0 + (part.Rev ? r.W.Cum[r.V1] - r.W.Cum[v] : r.W.Cum[v] - r.W.Cum[r.V0]);
                        where[key] = (c, s);
                    }
                }
            foreach (Joint j in net.Joints)
            {
                if (!j.Through) continue;
                Arm a = j.ThroughA, b = j.ThroughB;
                if (!where.TryGetValue(((long)a.W.Index << 22) | (long)a.Vertex, out (Chain c, float s) hit)) continue;
                Chain c = hit.c;
                float s = hit.s;
                Vector2 d = ChainDir(c, s);
                Arm fwd = Vector2.Dot(a.Dir, d) >= Vector2.Dot(b.Dir, d) ? a : b, back = fwd == a ? b : a;
                j.OnChain = c;
                j.AlongS = s;
                j.Fwd = fwd;
                j.Back = back;
                for (int side = 1; side >= -1; side -= 2)
                {
                    bool any = false;
                    foreach (Arm x in j.Arms) if (!x.Through && Cross(fwd.Dir, x.Dir) * side > 0f) any = true;
                    if (!any) continue;
                    float lo = side > 0 ? back.TanR : back.TanL, hi = side > 0 ? fwd.TanL : fwd.TanR;
                    c.Mouths.Add(new Mouth { J = j, S = s, Side = side, Lo = s - lo, Hi = s + hi });
                }
            }
        }

        /// <summary>The mouth on <paramref name="side"/> of <paramref name="c"/> at <paramref name="s"/> (a loop's
        /// start and end are one point), or null.</summary>
        private static Mouth MouthNear(Chain c, int side, float s)
        {
            foreach (Mouth m in c.Mouths)
            {
                if (m.Side != side) continue;
                if (Mathf.Abs(m.S - s) < 0.05f) return m;
                if (c.Closed && (Mathf.Abs(m.S - s - c.Length) < 0.05f || Mathf.Abs(m.S - s + c.Length) < 0.05f)) return m;
            }
            return null;
        }

        /// <summary>True when <paramref name="s"/> metres along <paramref name="c"/> lies in a mouth on
        /// <paramref name="side"/>.</summary>
        private static bool InMouth(Chain c, int side, float s)
        {
            for (int i = 0; i < c.Mouths.Count; i++)
            {
                Mouth m = c.Mouths[i];
                if (m.Side != side) continue;
                if (s > m.Lo && s < m.Hi) return true;
                if (c.Closed && ((s - c.Length > m.Lo && s - c.Length < m.Hi) || (s + c.Length > m.Lo && s + c.Length < m.Hi))) return true;
            }
            return false;
        }

        private static readonly List<float> s_KnotS = new List<float>(16), s_KnotV = new List<float>(64);

        // A part's cross-section values on one side (edge, sidewalk in and out, lane end). A part without a sidewalk
        // borrows the nearest part's, so a sidewalk stops where its part does instead of tapering away.
        private static Vector4 PartValues(Chain c, int i, int side)
        {
            Side d = SideOf(c.Parts[i], side);
            if (d.Walk) return new Vector4(d.Edge, d.WalkIn, d.WalkOut, d.Lane);
            for (int k = 1; k < c.Parts.Count; k++)
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    int o = i + sgn * k;
                    if (o < 0 || o >= c.Parts.Count) continue;
                    Side od = SideOf(c.Parts[o], side);
                    if (od.Walk) return new Vector4(d.Edge, od.WalkIn, od.WalkOut, d.Lane);
                }
            return new Vector4(d.Edge, d.Edge, d.Edge, d.Lane);
        }

        private static bool Same(Vector4 a, Vector4 b) => (a - b).sqrMagnitude < 1e-4f;

        /// <summary>Add a knot (profile sample) to the temporary knot lists.
        /// Used while building cross‑section profiles for a chain.
        /// </summary>
        private static void Knot(float s, Vector4 v)
        {
            s_KnotS.Add(s);
            s_KnotV.Add(v.x);
            s_KnotV.Add(v.y);
            s_KnotV.Add(v.z);
            s_KnotV.Add(v.w);
        }

        // The chain's cross-section on one side as knots: each part's values, changing linearly over a few metres where
        // two parts meet; across a mouth held at the wider of the two (as the junction's kerb returns took it).
        /// <summary>Build the cross‑section profile (as knots) for one side of a road chain.
        /// Handles loops, mouths, and linear transitions between parts.
        /// </summary>
        private static void BuildProfile(Chain c, int side)
        {
            s_KnotS.Clear();
            s_KnotV.Clear();
            int np = c.Parts.Count;
            float len = c.Length;
            Vector4 first = PartValues(c, 0, side), last = PartValues(c, np - 1, side);
            // A loop's two ends are one more meeting of two parts.
            Mouth closing = c.Closed && !Same(first, last) ? MouthNear(c, side, 0f) : null;
            float tail = -1f;
            if (c.Closed && !Same(first, last))
            {
                Vector4 m = closing != null ? Vector4.Max(first, last) : (first + last) * 0.5f;
                float headEnd = closing != null ? Mathf.Clamp(closing.Hi > len * 0.5f ? closing.Hi - len : closing.Hi, 0f, len * 0.3f) : 0f;
                float tailStart = closing != null ? Mathf.Clamp(closing.Lo < len * 0.5f ? len + closing.Lo : closing.Lo, len * 0.7f, len) : len;
                float t = Mathf.Min(Taper * 0.5f, (c.Parts[0].S1 - c.Parts[0].S0) * 0.45f);
                Knot(0f, m);
                if (headEnd > 0f) Knot(headEnd, m);
                Knot(headEnd + t, first);
                tail = tailStart;
                c.Ramps = true;
            }
            else Knot(0f, first);
            for (int i = 1; i < np; i++)
            {
                Part pa = c.Parts[i - 1], pb = c.Parts[i];
                Vector4 va = PartValues(c, i - 1, side), vb = PartValues(c, i, side);
                if (Same(va, vb)) continue;
                c.Ramps = true;
                float b = pb.S0, lenA = pa.S1 - pa.S0, lenB = pb.S1 - pb.S0;
                Mouth mouth = MouthNear(c, side, b);
                if (mouth != null)
                {
                    Vector4 m = Vector4.Max(va, vb);
                    Knot(mouth.Lo - Mathf.Min(Taper, Mathf.Max(0.5f, lenA * 0.45f)), va);
                    Knot(mouth.Lo, m);
                    Knot(mouth.Hi, m);
                    Knot(mouth.Hi + Mathf.Min(Taper, Mathf.Max(0.5f, lenB * 0.45f)), vb);
                }
                else
                {
                    float t = Mathf.Min(Taper * 0.5f, lenA * 0.45f, lenB * 0.45f);
                    Knot(b - t, va);
                    Knot(b + t, vb);
                }
            }
            if (tail >= 0f)
            {
                Vector4 m = closing != null ? Vector4.Max(first, last) : (first + last) * 0.5f;
                float t = Mathf.Min(Taper * 0.5f, (c.Parts[np - 1].S1 - c.Parts[np - 1].S0) * 0.45f);
                Knot(tail - t, last);
                if (tail < len) Knot(tail, m);
                Knot(len, m);
            }
            else Knot(len, last);
            int n = s_KnotS.Count, si = side > 0 ? 0 : 1;
            var ks = new float[n];
            var kv = new float[n * 4];
            float reach = 0f;
            for (int k = 0; k < n; k++)
            {
                ks[k] = Mathf.Clamp(k > 0 ? Mathf.Max(s_KnotS[k], ks[k - 1]) : s_KnotS[k], 0f, len);
                for (int q = 0; q < 4; q++) kv[k * 4 + q] = s_KnotV[k * 4 + q];
                reach = Mathf.Max(reach, kv[k * 4]);
            }
            for (int k = 1; k < n; k++) ks[k] = Mathf.Max(ks[k], ks[k - 1]);
            c.KnotS[si] = ks;
            c.KnotV[si] = kv;
            if (side > 0) c.ReachL = reach;
            else c.ReachR = reach;
        }

        /// <summary>The cross-section of <paramref name="c"/> on <paramref name="side"/> (+1 left) at
        /// <paramref name="s"/> metres along it.</summary>
        private static void ProfAt(Chain c, int side, float s, out Prof p)
        {
            p = default;
            p.Of = SideOf(c.Parts[PartAt(c, s)], side);
            int si = side > 0 ? 0 : 1;
            float[] ks = c.KnotS[si], kv = c.KnotV[si];
            int n = ks != null ? ks.Length : 0;
            if (n == 0)
            {
                p.Edge = p.Of.Edge;
                p.WalkIn = p.Of.WalkIn;
                p.WalkOut = p.Of.WalkOut;
                p.Lane = p.Of.Lane;
                return;
            }
            int k = 0;
            if (s >= ks[n - 1]) k = n - 1;
            else if (s > ks[0])
            {
                int lo = 0, hi = n - 1;
                while (hi - lo > 1)
                {
                    int mid = (lo + hi) >> 1;
                    if (ks[mid] <= s) lo = mid;
                    else hi = mid;
                }
                k = lo;
            }
            if (k >= n - 1 || s <= ks[0])
            {
                int at = s <= ks[0] ? 0 : n - 1;
                p.Edge = kv[at * 4];
                p.WalkIn = kv[at * 4 + 1];
                p.WalkOut = kv[at * 4 + 2];
                p.Lane = kv[at * 4 + 3];
                return;
            }
            float span = ks[k + 1] - ks[k], f = span > 1e-5f ? Mathf.Clamp01((s - ks[k]) / span) : 1f;
            p.Edge = Mathf.Lerp(kv[k * 4], kv[k * 4 + 4], f);
            p.WalkIn = Mathf.Lerp(kv[k * 4 + 1], kv[k * 4 + 5], f);
            p.WalkOut = Mathf.Lerp(kv[k * 4 + 2], kv[k * 4 + 6], f);
            p.Lane = Mathf.Lerp(kv[k * 4 + 3], kv[k * 4 + 7], f);
        }

        /// <summary>The carriageway's edge offset of <paramref name="c"/> on <paramref name="side"/> at
        /// <paramref name="s"/>.</summary>
        private static float EdgeAt(Chain c, int side, float s)
        {
            ProfAt(c, side, s, out Prof p);
            return p.Edge;
        }

        private static readonly List<Vector2> s_EdgePts = new List<Vector2>(32);

        /// <summary>Points on the carriageway's edge on <paramref name="side"/> of <paramref name="c"/> from
        /// <paramref name="from"/> to <paramref name="to"/> metres along it (round a loop's start where they wrap).</summary>
        private static void EdgePoints(Net net, Chain c, int side, float from, float to, List<Vector2> into)
        {
            float len = c.Length;
            if (c.Closed && from < 0f)
            {
                EdgeRun(net, c, side, len + from, len, into);
                EdgeRun(net, c, side, 0f, to, into);
            }
            else if (c.Closed && to > len)
            {
                EdgeRun(net, c, side, from, len, into);
                EdgeRun(net, c, side, 0f, to - len, into);
            }
            else EdgeRun(net, c, side, Mathf.Max(0f, from), Mathf.Min(len, to), into);
        }

        private static void EdgeRun(Net net, Chain c, int side, float from, float to, List<Vector2> into)
        {
            // The cross-section holds steady across a mouth (see BuildProfile): the chain's own points are enough.
            SamplesOf(c, from, to, 0f, net.Samples2);
            foreach (Sample q in net.Samples2) AddPoint(into, q.P + q.N * (side * EdgeAt(c, side, q.S)));
        }

        // A through junction's mouth patches: on each side of the through road that side roads join, the kerb returns
        // from one through arm round the side arms to the other, closed back along the through road's edge.
        private static void MouthPatches(Net net, Joint j)
        {
            Chain c = j.OnChain;
            if (c == null || j.All == null) return;
            int n = j.Arms.Count, fi = j.Arms.IndexOf(j.Fwd), bi = j.Arms.IndexOf(j.Back);
            foreach (Mouth m in c.Mouths)
            {
                if (m.J != j) continue;
                int side = m.Side, start = side > 0 ? fi : bi, stop = side > 0 ? bi : fi;
                var poly = new List<Vector2>(32);
                for (int k = start; k != stop; k = (k + 1) % n)
                {
                    Corner corner = j.All[k];
                    if (corner.Skip) continue;
                    foreach (Vector2 q in corner.Path) AddPoint(poly, q);
                }
                // Back along the through road's edge: forward from the back arm's return on the left, backward from the
                // forward arm's on the right.
                s_EdgePts.Clear();
                EdgePoints(net, c, side, m.Lo, m.Hi, s_EdgePts);
                if (side < 0) s_EdgePts.Reverse();
                for (int i = 1; i + 1 < s_EdgePts.Count; i++) AddPoint(poly, s_EdgePts[i]);
                if (poly.Count > 2 && (poly[0] - poly[poly.Count - 1]).sqrMagnitude <= 1e-4f) poly.RemoveAt(poly.Count - 1);
                float area = poly.Count >= 3 ? MapGeometry.SignedArea(poly) : 0f;
                if (Mathf.Abs(area) >= 0.05f && !float.IsNaN(area) && IsSimple(poly))
                {
                    if (area < 0f) poly.Reverse();
                    j.Caps.Add(poly);
                    j.CapBounds.Add(MapGeometry.Bounds(poly));
                    continue;
                }
                // The side roads reach too little way out for one mouth (their next junction is close): each rounded
                // corner's fillet between its kerb lines instead, the ribbons covering the rest.
                for (int k = start; k != stop; k = (k + 1) % n)
                {
                    Corner corner = j.All[k];
                    if (corner.Skip || !corner.Rounded || corner.Arc.Count < 2) continue;
                    var fillet = new List<Vector2>(corner.Arc.Count + 1) { corner.Meet };
                    foreach (Vector2 q in corner.Arc) AddPoint(fillet, q);
                    float fa = fillet.Count >= 3 ? MapGeometry.SignedArea(fillet) : 0f;
                    if (Mathf.Abs(fa) < 0.02f || float.IsNaN(fa) || !IsSimple(fillet)) continue;
                    if (fa < 0f) fillet.Reverse();
                    j.Caps.Add(fillet);
                    j.CapBounds.Add(MapGeometry.Bounds(fillet));
                }
            }
        }
        #endregion

        #region What's where
        // The carriageways (chains between their trims, the junction patches and mouths) and the sidewalks, gridded for
        // "is this spot on a road or a sidewalk" questions; and every paved outline, for rays (RayToRoad).
        private static void IndexPavement(Net net)
        {
            float reachAll = net.Half + 30f;
            net.PaveGrid = new CellGrid(reachAll, 8f);
            net.WalkGrid = new CellGrid(reachAll, 8f);
            net.EdgeGrid = new CellGrid(reachAll, 6f);
            int item = 0;
            foreach (Chain c in net.Chains)
            {
                if (c.To - c.From < 0.02f) continue;
                float reach = Mathf.Max(c.ReachL, c.ReachR);
                float walk = 0f;
                foreach (Part part in c.Parts)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Side d = SideOf(part, side);
                        if (d.Walk) walk = Mathf.Max(walk, d.WalkOut);
                    }
                for (int k = 0; k + 1 < c.P.Count; k++)
                {
                    if (c.Cum[k + 1] < c.From || c.Cum[k] > c.To) continue;
                    Vector2 a = c.P[k], b = c.P[k + 1];
                    float x0 = Mathf.Min(a.x, b.x), z0 = Mathf.Min(a.y, b.y), x1 = Mathf.Max(a.x, b.x), z1 = Mathf.Max(a.y, b.y);
                    net.PaveGrid.Add(x0 - reach, z0 - reach, x1 + reach, z1 + reach, item++);
                    net.PaveItems.Add((c, k));
                    if (walk > 0f)
                    {
                        net.WalkGrid.Add(x0 - walk, z0 - walk, x1 + walk, z1 + walk, net.WalkItems.Count);
                        net.WalkItems.Add((c, k, 0));
                    }
                }
                // The ribbon's outline.
                ProfileSamples(c, c.From, c.To, 4f, net.Samples);
                List<Sample> s = net.Samples;
                for (int i = 0; i < s.Count; i++)
                {
                    Vector2 l = s[i].P + s[i].N * EdgeAt(c, 1, s[i].S), r = s[i].P - s[i].N * EdgeAt(c, -1, s[i].S);
                    if (i == 0 || i == s.Count - 1) AddEdge(net, l, r, c, null);
                    if (i == 0) continue;
                    Vector2 pl = s[i - 1].P + s[i - 1].N * EdgeAt(c, 1, s[i - 1].S), pr = s[i - 1].P - s[i - 1].N * EdgeAt(c, -1, s[i - 1].S);
                    AddEdge(net, pl, l, c, null);
                    AddEdge(net, pr, r, c, null);
                }
            }
            foreach (Joint j in net.Joints)
            {
                if (j.Patch != null && j.Patch.Count >= 3)
                {
                    net.PaveGrid.Add(j.Bounds.xMin, j.Bounds.yMin, j.Bounds.xMax, j.Bounds.yMax, item++);
                    net.PatchItems.Add((j, -1));
                    for (int i = 0; i < j.Patch.Count; i++) AddEdge(net, j.Patch[i], j.Patch[(i + 1) % j.Patch.Count], null, j);
                }
                for (int k = 0; k < j.Caps.Count; k++)
                {
                    Rect b = j.CapBounds[k];
                    net.PaveGrid.Add(b.xMin, b.yMin, b.xMax, b.yMax, item++);
                    net.PatchItems.Add((j, k));
                    List<Vector2> cap = j.Caps[k];
                    for (int i = 0; i < cap.Count; i++) AddEdge(net, cap[i], cap[(i + 1) % cap.Count], null, j);
                }
            }
        }

        private static void AddEdge(Net net, Vector2 a, Vector2 b, Chain c, Joint j)
        {
            if ((a - b).sqrMagnitude < 1e-6f || !Finite(a) || !Finite(b)) return;
            net.EdgeGrid.Add(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), net.EdgeA.Count);
            net.EdgeA.Add(a);
            net.EdgeB.Add(b);
            net.EdgeChain.Add(c);
            net.EdgeJoint.Add(j);
        }

        private static bool Finite(Vector2 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y);

        /// <summary>How far a ray from <paramref name="from"/> along unit <paramref name="dir"/> goes before it meets the
        /// outline of anything paved (at most <paramref name="max"/>); <paramref name="skip"/>'s patches don't count, and
        /// neither does anything nearer than 5 cm (the outline the ray starts on).</summary>
        private static float RayToRoad(Net net, Vector2 from, Vector2 dir, float max, Joint skip = null)
        {
            Vector2 to = from + dir * max;
            int stamp = net.NewStamp(net.EdgeA.Count);
            net.EdgeGrid.Query(Mathf.Min(from.x, to.x), Mathf.Min(from.y, to.y), Mathf.Max(from.x, to.x), Mathf.Max(from.y, to.y),
                net.Query, net.Stamp, stamp);
            float best = max;
            foreach (int e in net.Query)
            {
                if (skip != null && net.EdgeJoint[e] == skip) continue;
                Vector2 a = net.EdgeA[e], ab = net.EdgeB[e] - a;
                float den = Cross(dir, ab);
                if (Mathf.Abs(den) < 1e-7f) continue;
                Vector2 w = a - from;
                float t = Cross(w, ab) / den, u = Cross(w, dir) / den;
                if (t > 0.05f && t < best && u >= -1e-4f && u <= 1.0001f) best = t;
            }
            return best;
        }

        private static bool InPatch(Joint j, int poly, Vector2 q)
        {
            if (poly < 0) return j.Bounds.Contains(q) && MapGeometry.Contains(j.Patch, q);
            return j.CapBounds[poly].Contains(q) && MapGeometry.Contains(j.Caps[poly], q);
        }

        /// <summary>True when <paramref name="q"/> lies on a junction's patch or one of its mouths.</summary>
        private static bool InJunction(Joint j, Vector2 q)
        {
            if (j.Patch != null && j.Patch.Count >= 3 && InPatch(j, -1, q)) return true;
            for (int k = 0; k < j.Caps.Count; k++) if (InPatch(j, k, q)) return true;
            return false;
        }

        /// <summary>True when <paramref name="q"/> lies on a carriageway (a chain's between its trims, widened by
        /// <paramref name="margin"/>, other than <paramref name="except"/>) or a junction's patch or mouth.</summary>
        private static bool OnPavement(Net net, Vector2 q, Chain except, float margin, bool patches = true)
        {
            int stamp = net.NewStamp(net.PaveItems.Count + net.PatchItems.Count);
            net.PaveGrid.Query(q.x, q.y, q.x, q.y, net.Query, net.Stamp, stamp);
            foreach (int it in net.Query)
            {
                if (it >= net.PaveItems.Count)
                {
                    if (!patches) continue;
                    (Joint j, int poly) = net.PatchItems[it - net.PaveItems.Count];
                    if (InPatch(j, poly, q)) return true;
                    continue;
                }
                (Chain c, int k) = net.PaveItems[it];
                if (c == except) continue;
                Vector2 a = c.P[k], ab = c.P[k + 1] - a;
                float len = c.Cum[k + 1] - c.Cum[k];
                if (len < 1e-4f) continue;
                Vector2 d = ab / len;
                float along = Vector2.Dot(q - a, d);
                if (along < -0.05f || along > len + 0.05f) continue;
                float s = c.Cum[k] + along;
                if (s < c.From - 0.05f || s > c.To + 0.05f) continue;
                float lat = Cross(d, q - a);
                if (lat > c.ReachL + margin || -lat > c.ReachR + margin) continue;
                if (lat >= 0f ? lat <= EdgeAt(c, 1, s) + margin : -lat <= EdgeAt(c, -1, s) + margin) return true;
            }
            return false;
        }

        /// <summary>True when <paramref name="q"/> lies on a sidewalk band (or the verge before it), other than
        /// <paramref name="except"/>'s.</summary>
        private static bool OnSidewalk(Net net, Vector2 q, Chain except, float margin)
        {
            int stamp = net.NewStamp(net.WalkItems.Count);
            net.WalkGrid.Query(q.x, q.y, q.x, q.y, net.Query, net.Stamp, stamp);
            foreach (int it in net.Query)
            {
                (Chain c, int k, int _) = net.WalkItems[it];
                if (c == except) continue;
                Vector2 a = c.P[k], ab = c.P[k + 1] - a;
                float len = c.Cum[k + 1] - c.Cum[k];
                if (len < 1e-4f) continue;
                Vector2 d = ab / len;
                float along = Vector2.Dot(q - a, d);
                if (along < -0.05f || along > len + 0.05f) continue;
                float s = c.Cum[k] + along;
                if (s < c.From - 0.5f || s > c.To + 0.5f) continue;
                float lat = Cross(d, q - a);
                int side = lat > 0f ? 1 : -1;
                ProfAt(c, side, s, out Prof p);
                float off = Mathf.Abs(lat);
                if (p.Walk && off >= p.Edge - margin && off <= p.WalkOut + margin) return true;
            }
            return false;
        }

        /// <summary>True when <paramref name="q"/> is inside a building's footprint (grown by <paramref name="margin"/>).</summary>
        private static bool InBuilding(Net net, Vector2 q, float margin)
        {
            int stamp = net.NewStamp(net.Prints.Count);
            net.PrintGrid.Query(q.x - margin, q.y - margin, q.x + margin, q.y + margin, net.Query, net.Stamp, stamp);
            foreach (int bi in net.Query)
            {
                Footprint f = net.Prints[bi];
                Rect b = f.Bounds;
                if (q.x < b.xMin - margin || q.x > b.xMax + margin || q.y < b.yMin - margin || q.y > b.yMax + margin) continue;
                if (MapGeometry.Contains(f.Pts, q)) return true;
                if (margin > 0f && MapGeometry.DistanceToEdge(f.Pts, q) < margin) return true;
            }
            return false;
        }

        /// <summary>How far a ray from <paramref name="from"/> along <paramref name="dir"/> goes before it meets a
        /// building's wall (at most <paramref name="max"/>).</summary>
        private static float RayToBuilding(Net net, Vector2 from, Vector2 dir, float max)
        {
            Vector2 to = from + dir * max;
            int stamp = net.NewStamp(net.Prints.Count);
            net.PrintGrid.Query(Mathf.Min(from.x, to.x), Mathf.Min(from.y, to.y), Mathf.Max(from.x, to.x), Mathf.Max(from.y, to.y),
                net.Query, net.Stamp, stamp);
            float best = max;
            foreach (int bi in net.Query)
            {
                List<Vector2> p = net.Prints[bi].Pts;
                for (int i = 0, k = p.Count - 1; i < p.Count; k = i++)
                {
                    Vector2 e = p[i] - p[k];
                    float den = Cross(dir, e);
                    if (Mathf.Abs(den) < 1e-6f) continue;
                    Vector2 w = p[k] - from;
                    float t = Cross(w, e) / den, u = Cross(w, dir) / den;
                    if (t >= 0f && t < best && u >= 0f && u <= 1f) best = t;
                }
            }
            return best;
        }

        /// <summary>Points along a chain from <paramref name="from"/> to <paramref name="to"/>: its corners in between
        /// and, on longer stretches, one every <paramref name="step"/> metres (0: corners only); added to what
        /// <paramref name="into"/> holds when <paramref name="append"/> (a point it already ends with isn't repeated).</summary>
        private static void SamplesOf(Chain c, float from, float to, float step, List<Sample> into, bool append = false)
        {
            if (!append) into.Clear();
            if (to - from < 0.02f) return;
            for (int k = 0; k + 1 < c.P.Count; k++)
            {
                float s0 = c.Cum[k], s1 = c.Cum[k + 1];
                if (s1 <= from || s0 >= to) continue;
                Vector2 a = c.P[k];
                float len = s1 - s0;
                if (len < 1e-5f) continue;
                Vector2 d = (c.P[k + 1] - a) / len, n = Left(d);
                float lo = Mathf.Max(from, s0), hi = Mathf.Min(to, s1);
                if (into.Count == 0 || lo > into[into.Count - 1].S + 1e-3f)
                    into.Add(new Sample { P = a + d * (lo - s0), N = lo <= s0 + 1e-4f ? c.N[k] : n, S = lo });
                if (step > 0f)
                {
                    int pieces = Mathf.CeilToInt((hi - lo) / step);
                    for (int i = 1; i < pieces; i++)
                    {
                        float s = lo + (hi - lo) * i / pieces;
                        into.Add(new Sample { P = a + d * (s - s0), N = n, S = s });
                    }
                }
                into.Add(new Sample { P = a + d * (hi - s0), N = hi >= s1 - 1e-4f ? c.N[k + 1] : n, S = hi });
            }
        }

        /// <summary><see cref="SamplesOf"/> plus a point at every knot of the chain's cross-section (where a taper starts or
        /// ends), so edges drawn through the samples follow the tapers exactly.</summary>
        private static void ProfileSamples(Chain c, float from, float to, float step, List<Sample> into)
        {
            SamplesOf(c, from, to, step, into);
            AddKnots(c, from, to, into);
        }

        /// <summary>Adds a sample at each knot of <paramref name="c"/>'s cross-section strictly between
        /// <paramref name="from"/> and <paramref name="to"/>, in order, unless a sample lies within 5 cm of it.</summary>
        private static void AddKnots(Chain c, float from, float to, List<Sample> into)
        {
            if (!c.Ramps) return;
            for (int si = 0; si < 2; si++)
            {
                float[] ks = c.KnotS[si];
                if (ks == null) continue;
                for (int k = 0; k < ks.Length; k++)
                {
                    float s = ks[k];
                    if (s <= from + 0.05f || s >= to - 0.05f) continue;
                    int i = 0;
                    while (i < into.Count && into[i].S < s) i++;
                    if ((i < into.Count && into[i].S - s < 0.05f) || (i > 0 && s - into[i - 1].S < 0.05f)) continue;
                    PointOn(c, s, out Vector2 p, out Vector2 d);
                    into.Insert(i, new Sample { P = p, N = Left(d), S = s });
                }
            }
        }
        #endregion
    }
}
