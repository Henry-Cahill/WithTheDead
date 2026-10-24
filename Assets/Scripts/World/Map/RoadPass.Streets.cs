using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The carriageways: each chain between its junction trims in its winter surface, the junction patches over the ends,
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

        /// <summary><see cref="Band"/> between two fixed offsets.</summary>
        private static void BandConst(Net net, List<Sample> s, float a, float b, float y, in Surf surf, Profile lift = null,
            bool[] keep = null)
        {
            Fit(s.Count);
            for (int i = 0; i < s.Count; i++)
            {
                s_A[i] = a;
                s_B[i] = b;
            }
            Band(net, s, s_A, s_B, y, y, surf, lift, keep);
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
                c.N[i] = m / Mathf.Max(0.5f, Vector2.Dot(m, Left(dNext)));
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

        // Where the wheels run: the middle of each travel lane, across the chain (left offsets).
        private static void LaneCentres(Chain c, List<float> into)
        {
            into.Clear();
            Way w = c.W;
            float lo = -c.R.Lane, hi = c.L.Lane, centre = (hi + lo) * 0.5f;
            if (w.OneWay || hi - lo < 5.4f || w.Cls >= Cls.Service)
            {
                int n = w.OneWay ? Mathf.Max(1, w.LanesR) : 1;
                if (hi - lo < 3.0f * n) n = Mathf.Max(1, Mathf.FloorToInt((hi - lo) / 3.0f));
                for (int k = 0; k < n; k++) into.Add(lo + (hi - lo) * (k + 0.5f) / n);
                return;
            }
            int left = Mathf.Max(1, w.LanesL), right = Mathf.Max(1, w.LanesR);
            for (int k = 0; k < left; k++) into.Add(centre + (hi - centre) * (k + 0.5f) / left);
            for (int k = 0; k < right; k++) into.Add(centre - (centre - lo) * (k + 0.5f) / right);
        }

        private static void Carriageways(Net net)
        {
            Look look = net.Look;
            foreach (Chain c in net.Chains)
            {
                if (c.To - c.From < 0.02f) continue;
                Way w = c.W;
                float y = RoadY + w.Order * 0.0003f;
                SamplesOf(c, c.From, c.To, 0f, net.Samples);
                BandConst(net, net.Samples, -c.R.Edge, c.L.Edge, y, BaseSurf(look, w), c.Lift);
                if (net.Full) WinterDetail(net, c);
                TakeChain(net, c);
            }
            foreach (Joint j in net.Joints)
            {
                if (j.Patch == null || j.Patch.Count < 3) continue;
                Fan(net.Out, j.At, j.Patch, PatchY, BaseSurf(look, j.Top));
                if (net.Full && j.Top.Winter == Winter.Ploughed) PatchSlush(net, j);
                TakePatch(net, j);
            }
        }

        // The winter on a carriageway: slush along the edges, over the crown and between the wheel tracks of a ploughed
        // road; dark tyre tracks pressed into a packed one; two ruts down a farm track. Every edge wanders a little, by
        // noise over the ground that neighbouring chunks agree on.
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
            LaneCentres(c, s_Lanes);
            switch (w.Winter)
            {
                case Winter.Ploughed:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Side d = side > 0 ? c.L : c.R;
                        bool parking = d.Edge > d.Lane + 0.8f;
                        for (int i = 0; i < n; i++)
                        {
                            float wander = Noise(s[i].P, 3.5f, seed + side);
                            float inner = parking ? d.Lane + 0.15f + 0.4f * wander : d.Edge - 0.35f - 0.45f * wander;
                            s_A[i] = side * inner;
                            s_B[i] = side * d.Edge;
                        }
                        Band(net, s, s_A, s_B, DetailY, DetailY, parking ? look.SlushLight : look.Slush, c.Lift);
                    }
                    if (!w.OneWay && w.Centre != CentreLine.None)
                    {
                        float centre = (c.L.Lane - c.R.Lane) * 0.5f;
                        for (int i = 0; i < n; i++)
                        {
                            float half = 0.18f + 0.3f * Noise(s[i].P, 3f, seed + 7);
                            s_A[i] = centre - half;
                            s_B[i] = centre + half;
                            s_Keep[i] = Noise(s[i].P, 7f, seed + 9) > 0.45f;
                        }
                        Band(net, s, s_A, s_B, BuryY, BuryY, look.Slush, c.Lift, s_Keep);
                    }
                    for (int k = 0; k < s_Lanes.Count; k++)
                    {
                        float lc = s_Lanes[k];
                        for (int i = 0; i < n; i++)
                        {
                            float half = 0.16f + 0.22f * Noise(s[i].P, 3f, seed + 20 + k);
                            float shift = 0.15f * (Noise(s[i].P, 6f, seed + 40 + k) - 0.5f);
                            s_A[i] = lc + shift - half;
                            s_B[i] = lc + shift + half;
                            s_Keep[i] = Noise(s[i].P, 4.5f, seed + 60 + k) > 0.38f;
                        }
                        Band(net, s, s_A, s_B, DetailY, DetailY, look.SlushLight, c.Lift, s_Keep);
                    }
                    break;
                case Winter.Packed:
                    Surf track = w.Gravel ? look.TrackGravel : look.Track;
                    for (int k = 0; k < s_Lanes.Count; k++)
                        for (int t = -1; t <= 1; t += 2)
                        {
                            float lc = s_Lanes[k] + t * 0.8f;
                            for (int i = 0; i < n; i++)
                            {
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

        // Slush the plough left along a ploughed junction's kerb lines.
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

        // The road's surface kept clear of props: the carriageway's middle line between its trims and its half width.
        private static void TakeChain(Net net, Chain c)
        {
            float shift = (c.L.Edge - c.R.Edge) * 0.5f, half = (c.L.Edge + c.R.Edge) * 0.5f;
            SamplesOf(c, c.From, c.To, 0f, net.Samples2);
            if (net.Samples2.Count < 2) return;
            var pts = new List<Vector2>(net.Samples2.Count);
            foreach (Sample s in net.Samples2) pts.Add(s.P + s.N * shift);
            net.Site.TakeRoad(pts, half, 0.3f);
        }

        // A junction's patch kept clear of props, a metre's cell at a time.
        private static void TakePatch(Net net, Joint j)
        {
            Rect b = j.Bounds;
            for (float z = Mathf.Floor(b.yMin) + 0.5f; z < b.yMax; z += 1f)
                for (float x = Mathf.Floor(b.xMin) + 0.5f; x < b.xMax; x += 1f)
                {
                    var p = new Vector2(x, z);
                    if (MapGeometry.Contains(j.Patch, p)) net.Site.Take(p, 0.4f);
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
