using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The carriageways: each chain between its junction trims in its winter surface, the junctions' patches and mouths,
    // the slush, tyre tracks and ruts on them, and past a fixed map's edge the same roads as scenery.
    internal static partial class RoadPass
    {
        // Scratch buffers for the bands (the map is built on the main thread, one site at a time).
        private static float[] s_A = new float[256], s_B = new float[256], s_YA = new float[256], s_YB = new float[256];
        private static bool[] s_Keep = new bool[256];
        private static readonly List<float> s_Lanes = new List<float>(8);

        private static void Fit(int n)
        {
            if (s_A.Length >= n) return;
            int size = Mathf.NextPowerOfTwo(n);
            s_A = new float[size];
            s_B = new float[size];
            s_YA = new float[size];
            s_YB = new float[size];
            s_Keep = new bool[size];
        }

        /// <summary>A strip along the samples between left offsets <paramref name="a"/>[i] and <paramref name="b"/>[i]
        /// (negative: right of the line), its edges at heights <paramref name="ya"/>[i] and <paramref name="yb"/>[i]
        /// (plus a raised deck's lift), facing up; segments whose <paramref name="keep"/> is false are left out.</summary>
        private static void BandY(Net net, List<Sample> s, float[] a, float[] b, float[] ya, float[] yb, in Surf surf,
            Profile lift, bool[] keep, bool solid)
        {
            for (int i = 0; i + 1 < s.Count; i++)
            {
                if (keep != null && !keep[i]) continue;
                float l0 = lift != null ? lift.At(s[i].S) : 0f, l1 = lift != null ? lift.At(s[i + 1].S) : 0f;
                Vector3 p0 = To3(s[i].P + s[i].N * a[i], ya[i] + l0), p1 = To3(s[i + 1].P + s[i + 1].N * a[i + 1], ya[i + 1] + l1);
                Vector3 q1 = To3(s[i + 1].P + s[i + 1].N * b[i + 1], yb[i + 1] + l1), q0 = To3(s[i].P + s[i].N * b[i], yb[i] + l0);
                Vector3 mid = (p0 + q1) * 0.5f;
                Face(solid ? net.Out.Solid(mid) : net.Out.Flat(mid), p0, p1, q1, q0, Vector3.up, surf);
            }
        }

        /// <summary><see cref="BandY"/> at one height per edge.</summary>
        private static void Band(Net net, List<Sample> s, float[] a, float[] b, float ya, float yb, in Surf surf,
            Profile lift = null, bool[] keep = null, bool solid = false)
        {
            Fit(s.Count);
            for (int i = 0; i < s.Count; i++)
            {
                s_YA[i] = ya;
                s_YB[i] = yb;
            }
            BandY(net, s, a, b, s_YA, s_YB, surf, lift, keep, solid);
        }

        /// <summary>A line of its own (a path, a corner's kerb line, a railway) as a chain: its arc lengths and mitred
        /// offsets, for <see cref="SamplesOf"/>.</summary>
        private static Chain ChainOf(List<Vector2> pts)
        {
            var c = new Chain();
            for (int i = 0; i < pts.Count; i++)
                if (c.P.Count == 0 || (pts[i] - c.P[c.P.Count - 1]).sqrMagnitude > 1e-4f) c.P.Add(pts[i]);
            c.Cum = new float[c.P.Count];
            for (int i = 1; i < c.P.Count; i++) c.Cum[i] = c.Cum[i - 1] + Vector2.Distance(c.P[i - 1], c.P[i]);
            c.Length = c.P.Count > 0 ? c.Cum[c.P.Count - 1] : 0f;
            c.N = new Vector2[c.P.Count];
            for (int i = 0; i < c.P.Count; i++)
            {
                Vector2 dPrev = i > 0 ? (c.P[i] - c.P[i - 1]).normalized : Vector2.zero;
                Vector2 dNext = i + 1 < c.P.Count ? (c.P[i + 1] - c.P[i]).normalized : dPrev;
                if (i == 0) dPrev = dNext;
                Vector2 m = Left(dPrev) + Left(dNext);
                m = m.sqrMagnitude > 1e-6f ? m.normalized : Left(dNext);
                float k = Vector2.Dot(m, Left(dNext));
                c.N[i] = m / Mathf.Max(0.5f, float.IsNaN(k) ? 1f : k);
            }
            return c;
        }

        private static Surf BaseSurf(Look look, Way w)
        {
            switch (w.Winter)
            {
                case Winter.Ploughed: return w.Concrete ? look.Concrete : look.Asphalt;
                case Winter.Packed: case Winter.Ruts: return look.Packed;
                case Winter.Paved: return look.Paved;
                default: return look.Trodden;
            }
        }

        private static int Seed(Way w, int salt) => (int)(MapStyle.Hash(w.S.Data.id, salt) & 0x3fffffff);

        // Where the wheels run: the middle of each travel lane, as fractions across the travel lanes (0 their right
        // end, 1 their left).
        private static void LaneFractions(Chain c, List<float> into)
        {
            into.Clear();
            Way w = c.W;
            ProfAt(c, 1, c.From, out Prof l);
            ProfAt(c, -1, c.From, out Prof r);
            float width = l.Lane + r.Lane;
            if (w.OneWay || width < 5.4f || w.Cls >= Cls.Service)
            {
                int n = w.OneWay ? Mathf.Max(1, w.LanesR) : 1;
                if (width < 3.0f * n) n = Mathf.Max(1, Mathf.FloorToInt(width / 3.0f));
                for (int k = 0; k < n; k++) into.Add((k + 0.5f) / n);
                return;
            }
            int left = Mathf.Max(1, w.LanesL), right = Mathf.Max(1, w.LanesR);
            for (int k = 0; k < left; k++) into.Add(0.5f + 0.5f * (k + 0.5f) / left);
            for (int k = 0; k < right; k++) into.Add(0.5f - 0.5f * (k + 0.5f) / right);
        }

        private static IEnumerable<string> Carriageways(Net net)
        {
            Look look = net.Look;
            foreach (Chain c in net.Chains)
            {
                if (c.To - c.From < 0.02f) continue;
                Way w = c.W;
                float y = RoadY + w.Order * 0.0003f;
                ProfileSamples(c, c.From, c.To, 0f, net.Samples);
                List<Sample> s = net.Samples;
                Fit(s.Count);
                for (int i = 0; i < s.Count; i++)
                {
                    s_A[i] = -EdgeAt(c, -1, s[i].S);
                    s_B[i] = EdgeAt(c, 1, s[i].S);
                }
                Band(net, s, s_A, s_B, y, y, BaseSurf(look, w), c.Lift);
                if (net.Full)
                {
                    Wear(net, c);
                    if (net.Site.StepDue) yield return "streets";
                    WinterDetail(net, c);
                }
                TakeChain(net, c);
                if (net.Site.StepDue) yield return "streets";
            }
            foreach (Joint j in net.Joints)
            {
                if (!j.HasPatch) continue;
                Surf surf = BaseSurf(look, j.Top);
                if (j.Patch != null && j.Patch.Count >= 3) Polygon(net.Out, j.Patch, j.At, PatchY, surf);
                foreach (List<Vector2> cap in j.Caps) Polygon(net.Out, cap, MapGeometry.Middle(cap), PatchY, surf);
                if (net.Full && j.Top.Winter == Winter.Ploughed) PatchSlush(net, j);
                TakePatch(net, j);
                if (net.Site.StepDue) yield return "streets";
            }
        }

        /// <summary>A face-up polygon: fanned from <paramref name="hub"/> when it sees every edge (most junction patches),
        /// else ear-clipped (fanned anyway should that fail).</summary>
        private static void Polygon(OutdoorBatches into, List<Vector2> poly, Vector2 hub, float y, in Surf s)
        {
            if (StarFrom(poly, hub))
            {
                Fan(into, hub, poly, y, s);
                return;
            }
            List<int> tris = MapGeometry.Triangulate(poly, 200);
            if (tris.Count < (poly.Count - 2) * 3)
            {
                Fan(into, hub, poly, y, s);
                return;
            }
            RaidMesh m = into.Flat(To3(hub, y));
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = To3(poly[tris[i]], y), b = To3(poly[tris[i + 1]], y), c = To3(poly[tris[i + 2]], y);
                if (!Finite(a) || !Finite(b) || !Finite(c)) continue;
                m.Tri(a, b, c, s);
            }
        }

        /// <summary>True when every edge of the anticlockwise polygon turns left seen from <paramref name="hub"/> (so a fan
        /// from it covers the polygon exactly once).</summary>
        private static bool StarFrom(List<Vector2> poly, Vector2 hub)
        {
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % n];
                if (Cross(p - hub, q - hub) <= 1e-4f) return false;
            }
            return true;
        }

        // The winter on a carriageway: slush along the edges (not across a side road's mouth), over the crown and
        // between the wheel tracks of a ploughed road; dark tyre tracks pressed into a packed one; two ruts down a farm
        // track. Every edge wanders a little, by noise over the ground that neighbouring chunks agree on.
        private static void WinterDetail(Net net, Chain c)
        {
            Way w = c.W;
            Look look = net.Look;
            List<Sample> s = net.Samples;
            SamplesOf(c, c.From, c.To, StepFull, s);
            int n = s.Count;
            if (n < 2) return;
            Fit(n);
            int seed = Seed(w, 0x5A1);
            LaneFractions(c, s_Lanes);
            switch (w.Winter)
            {
                case Winter.Ploughed:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        bool any = false;
                        for (int i = 0; i < n; i++)
                        {
                            ProfAt(c, side, s[i].S, out Prof d);
                            float wander = Noise(s[i].P, 3.5f, seed + side);
                            s_A[i] = side * (d.Edge - 0.35f - 0.45f * wander);
                            s_B[i] = side * d.Edge;
                        }
                        for (int i = 0; i + 1 < n; i++)
                        {
                            s_Keep[i] = !InMouth(c, side, (s[i].S + s[i + 1].S) * 0.5f);
                            any |= s_Keep[i];
                        }
                        if (!any) continue;
                        Band(net, s, s_A, s_B, DetailY, DetailY, look.Slush, c.Lift, s_Keep);
                    }
                    if (!w.OneWay && w.Centre != CentreLine.None)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            float centre = (EdgeLaneAt(c, 1, s[i].S) - EdgeLaneAt(c, -1, s[i].S)) * 0.5f;
                            float half = 0.18f + 0.3f * Noise(s[i].P, 3f, seed + 7);
                            s_A[i] = centre - half;
                            s_B[i] = centre + half;
                            s_Keep[i] = Noise(s[i].P, 7f, seed + 9) > 0.45f && !Crossing(c, s[i].S);
                        }
                        Band(net, s, s_A, s_B, BuryY, BuryY, look.Slush, c.Lift, s_Keep);
                    }
                    // Between the wheel paths: broken, lumpy, and churned away where the traffic crosses a junction.
                    for (int k = 0; k < s_Lanes.Count; k++)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            float lc = LaneAt(c, s[i].S, s_Lanes[k]);
                            float half = 0.1f + 0.26f * Noise(s[i].P, 2.25f, seed + 20 + k);
                            float shift = 0.15f * (Noise(s[i].P, 6f, seed + 40 + k) - 0.5f);
                            s_A[i] = lc + shift - half;
                            s_B[i] = lc + shift + half;
                            s_Keep[i] = Noise(s[i].P, 3.5f, seed + 60 + k) > 0.48f && !Crossing(c, s[i].S);
                        }
                        Band(net, s, s_A, s_B, DetailY, DetailY, look.SlushLight, c.Lift, s_Keep);
                    }
                    break;
                case Winter.Packed:
                    Surf track = w.Gravel ? look.TrackGravel : look.Track;
                    for (int k = 0; k < s_Lanes.Count; k++)
                        for (int t = -1; t <= 1; t += 2)
                        {
                            for (int i = 0; i < n; i++)
                            {
                                float lc = LaneAt(c, s[i].S, s_Lanes[k]) + t * 0.8f;
                                float shift = 0.24f * (Noise(s[i].P, 4.5f, seed + k * 2 + (t > 0 ? 1 : 0)) - 0.5f);
                                float half = 0.15f + 0.05f * Noise(s[i].P, 2.25f, seed + 77 + k);
                                s_A[i] = lc + shift - half;
                                s_B[i] = lc + shift + half;
                                s_Keep[i] = Noise(s[i].P, 3.5f, seed + 90 + k * 2 + t) > 0.12f;
                            }
                            Band(net, s, s_A, s_B, DetailY, DetailY, track, c.Lift, s_Keep);
                        }
                    break;
                case Winter.Ruts:
                    for (int t = -1; t <= 1; t += 2)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            float shift = 0.12f * (Noise(s[i].P, 4.5f, seed + t) - 0.5f);
                            s_A[i] = t * 0.78f + shift - 0.15f;
                            s_B[i] = t * 0.78f + shift + 0.15f;
                        }
                        Band(net, s, s_A, s_B, DetailY, DetailY, look.Rut, c.Lift);
                    }
                    break;
            }
        }

        private static float EdgeLaneAt(Chain c, int side, float s)
        {
            ProfAt(c, side, s, out Prof p);
            return p.Lane;
        }

        /// <summary>True where a chain runs through a junction: a side road's mouth opens on either side of it there.</summary>
        private static bool Crossing(Chain c, float s) => c.Mouths.Count > 0 && (InMouth(c, 1, s) || InMouth(c, -1, s));

        /// <summary>The left offset of a point a fraction <paramref name="f"/> across the travel lanes.</summary>
        private static float LaneAt(Chain c, float s, float f)
        {
            float lo = -EdgeLaneAt(c, -1, s), hi = EdgeLaneAt(c, 1, s);
            return lo + (hi - lo) * f;
        }

        // Slush the plough left along a ploughed junction's kerb returns.
        private static void PatchSlush(Net net, Joint j)
        {
            foreach (Corner corner in j.Corners)
            {
                if (corner.Path.Count < 2 || (!corner.Rounded && corner.Convex)) continue;
                Chain line = ChainOf(corner.Path);
                if (line.Length < 0.5f) continue;
                SamplesOf(line, 0f, line.Length, 0f, net.Samples2);
                Fit(net.Samples2.Count);
                for (int i = 0; i < net.Samples2.Count; i++)
                {
                    float t = net.Samples2[i].S / line.Length;
                    s_A[i] = 0f;
                    s_B[i] = 0.25f + 0.55f * Mathf.Sin(t * Mathf.PI) * Noise(net.Samples2[i].P, 3f, 0x5A7);
                }
                Band(net, net.Samples2, s_A, s_B, PatchDetailY, PatchDetailY, net.Look.Slush);
            }
        }

        private static readonly List<Vector2> s_Take = new List<Vector2>(64);

        // The road's surface kept clear of props: the carriageway's middle line between its trims and its half width,
        // in runs where the width holds steady.
        private static void TakeChain(Net net, Chain c)
        {
            SamplesOf(c, c.From, c.To, c.Ramps ? 3f : 0f, net.Samples2);
            List<Sample> s = net.Samples2;
            if (s.Count < 2) return;
            s_Take.Clear();
            float runHalf = -1f;
            for (int i = 0; i < s.Count; i++)
            {
                float l = EdgeAt(c, 1, s[i].S), r = EdgeAt(c, -1, s[i].S);
                float shift = (l - r) * 0.5f, half = (l + r) * 0.5f;
                if (runHalf >= 0f && Mathf.Abs(half - runHalf) > 0.4f && s_Take.Count >= 2)
                {
                    net.Site.TakeRoad(new List<Vector2>(s_Take), runHalf, 0.3f);
                    Vector2 last = s_Take[s_Take.Count - 1];
                    s_Take.Clear();
                    s_Take.Add(last);
                    runHalf = half;
                }
                if (runHalf < 0f) runHalf = half;
                else runHalf = Mathf.Max(runHalf, half);
                s_Take.Add(s[i].P + s[i].N * shift);
            }
            if (s_Take.Count >= 2) net.Site.TakeRoad(new List<Vector2>(s_Take), runHalf, 0.3f);
        }

        // A junction's patch and mouths kept clear of props, a metre's cell at a time (Lite: their boxes, roughly).
        private static void TakePatch(Net net, Joint j)
        {
            if (!net.Full)
            {
                if (j.Patch != null && j.Patch.Count >= 3) net.Site.Take(j.Bounds);
                foreach (Rect b in j.CapBounds) net.Site.Take(b);
                return;
            }
            if (j.Patch != null && j.Patch.Count >= 3) TakeCells(net, j.Patch, j.Bounds);
            for (int k = 0; k < j.Caps.Count; k++) TakeCells(net, j.Caps[k], j.CapBounds[k]);
        }

        private static void TakeCells(Net net, List<Vector2> poly, Rect b)
        {
            for (float z = Mathf.Floor(b.yMin) + 0.5f; z < b.yMax; z += 1f)
                for (float x = Mathf.Floor(b.xMin) + 0.5f; x < b.xMax; x += 1f)
                {
                    var p = new Vector2(x, z);
                    if (MapGeometry.Contains(poly, p)) net.Site.Take(p, 0.4f);
                }
        }

        // A fixed map's roads past the edge: the same surfaces (no seam of colour at the edge), sidewalks flat, no
        // junction patches, kerbs or markings.
        private static void Scenery(Net net)
        {
            float half = net.Half;
            Look look = net.Look;
            var known = new Dictionary<Street, Way>();
            foreach (Way w in net.Ways) if (w.Carriage && !known.ContainsKey(w.S)) known[w.S] = w;
            foreach (Street s in net.Site.Area.Streets)
            {
                if (s.Data.tunnel || s.Pts.Count < 2) continue;
                foreach (List<Vector2> piece in OutsidePieces(s.Pts, half))
                {
                    var w = new Way { S = s, P = piece };
                    Classify(w);
                    if (!w.Carriage) continue;
                    if (known.TryGetValue(s, out Way inside))
                    {
                        w.L.CopyFrom(inside.L);
                        w.R.CopyFrom(inside.R);
                        w.Winter = inside.Winter;
                    }
                    Chain c = ChainOf(piece);
                    if (c.Length < 0.3f) continue;
                    SamplesOf(c, 0f, c.Length, 0f, net.Samples);
                    Fit(net.Samples.Count);
                    float y = RoadY + w.Order * 0.0003f;
                    for (int i = 0; i < net.Samples.Count; i++)
                    {
                        s_A[i] = -w.R.Edge;
                        s_B[i] = w.L.Edge;
                        s_YA[i] = s_YB[i] = y;
                    }
                    FarBand(net, net.Samples, BaseSurf(look, w));
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Side d = side > 0 ? w.L : w.R;
                        if (!d.Walk) continue;
                        for (int i = 0; i < net.Samples.Count; i++)
                        {
                            s_A[i] = side * d.Edge;
                            s_B[i] = side * d.WalkOut;
                            s_YA[i] = s_YB[i] = KerbTop;
                        }
                        FarBand(net, net.Samples, d.WalkIn > d.Edge + KerbWidth + 0.3f ? look.Snow : look.Sidewalk);
                    }
                }
            }
        }

        // A band into the scenery batches (s_A, s_B, s_YA, s_YB filled).
        private static void FarBand(Net net, List<Sample> s, in Surf surf)
        {
            for (int i = 0; i + 1 < s.Count; i++)
            {
                Vector3 p0 = To3(s[i].P + s[i].N * s_A[i], s_YA[i]), p1 = To3(s[i + 1].P + s[i + 1].N * s_A[i + 1], s_YA[i + 1]);
                Vector3 q1 = To3(s[i + 1].P + s[i + 1].N * s_B[i + 1], s_YB[i + 1]), q0 = To3(s[i].P + s[i].N * s_B[i], s_YB[i]);
                Face(net.Site.Town.Far.Flat((p0 + q1) * 0.5f), p0, p1, q1, q0, Vector3.up, surf);
            }
        }

        /// <summary>The pieces of a line outside the playable square: cut to the four bands around it, which together
        /// cover everything out to the scenery's reach without overlapping.</summary>
        private static IEnumerable<List<Vector2>> OutsidePieces(List<Vector2> pts, float half)
        {
            float reach = half + SceneryReach;
            var bands = new[]
            {
                Rect.MinMaxRect(-reach, -reach, -half, reach), Rect.MinMaxRect(half, -reach, reach, reach),
                Rect.MinMaxRect(-half, half, half, reach), Rect.MinMaxRect(-half, -reach, half, -half),
            };
            foreach (Rect band in bands)
                foreach (List<Vector2> piece in MapGeometry.ClipLine(pts, band))
                    yield return piece;
        }
    }
}
