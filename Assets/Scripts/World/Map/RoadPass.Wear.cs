using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Old asphalt's wear on the ploughed roads, drawn rather than painted into the texture so it never repeats:
    // transverse cracks every few metres (some sealed with tar), cracks along the lane joints and the wheel paths, the
    // odd pothole the frost opened; on concrete, the joints between its slabs. Placed along each street's whole mapped
    // length (its arc), so neighbouring chunks agree; it lies on the asphalt, under the slush and the paint.
    internal static partial class RoadPass
    {
        private const float WearY = RoadY + 0.0012f;
        private const float WearStep = 3.5f, SlabLength = 4.5f;
        private static readonly List<Vector2> s_Crack = new List<Vector2>(24);

        private static void Wear(Net net, Chain c)
        {
            if (c.W.Winter != Winter.Ploughed) return;
            LaneFractions(c, s_Lanes);
            foreach (Part p in c.Parts)
            {
                float a = Mathf.Max(c.From, p.S0), b = Mathf.Min(c.To, p.S1);
                if (b - a < 1f) continue;
                Way pw = p.Run.W;
                float wa = ArcOf(p, a), wb = ArcOf(p, b);
                float lo = Mathf.Min(wa, wb), hi = Mathf.Max(wa, wb);
                float step = pw.Concrete ? SlabLength : WearStep;
                long id = pw.S.Data.id;
                for (long k = (long)System.Math.Floor(lo / step); k * step < hi; k++)
                {
                    long key = id * 7919L + k;
                    float arc = pw.Concrete ? k * step : (k + MapStyle.Unit(key, 0xC1)) * step;
                    if (arc < lo + 0.4f || arc > hi - 0.4f) continue;
                    float s = p.Rev ? a + (wa - arc) : a + (arc - wa);
                    float u = MapStyle.Unit(key, 0xC2);
                    if (pw.Concrete)
                    {
                        SlabJoint(net, c, s, 0.025f, net.Look.Crack);
                        if (u < 0.2f) Transverse(net, c, s + step * 0.5f, key);
                        continue;
                    }
                    if (u < 0.42f) Transverse(net, c, s, key);
                    else if (u < 0.6f) Longitudinal(net, c, s, b, key);
                    else if (u < 0.635f) Pothole(net, c, s, key);
                }
            }
        }

        // A crack across the road, from one edge right across or part way, wandering as it goes.
        private static void Transverse(Net net, Chain c, float s, long key)
        {
            if (s < c.From + 0.3f || s > c.To - 0.3f) return;
            float left = EdgeAt(c, 1, s) - 0.15f, right = -(EdgeAt(c, -1, s) - 0.15f);
            if (left - right < 1.5f) return;
            float x0 = right, x1 = left;
            if (MapStyle.Unit(key, 0xC3) < 0.5f)
            {
                float reach = Mathf.Lerp(0.35f, 0.75f, MapStyle.Unit(key, 0xC4)) * (left - right);
                if (MapStyle.Unit(key, 0xC5) < 0.5f) x1 = right + reach;
                else x0 = left - reach;
            }
            bool tar = MapStyle.Unit(key, 0xC6) < 0.4f;
            System.Random rng = MapStyle.Rng(key, 0xC7);
            int m = Mathf.Clamp(Mathf.CeilToInt((x1 - x0) / 0.6f), 2, 20);
            s_Crack.Clear();
            float along = 0f;
            for (int i = 0; i <= m; i++)
            {
                along = Mathf.Clamp(along + ((float)rng.NextDouble() - 0.5f) * (tar ? 0.12f : 0.24f), -0.5f, 0.5f);
                PointOn(c, Mathf.Clamp(s + along, c.From, c.To), out Vector2 p, out Vector2 d);
                s_Crack.Add(p + Left(d) * Mathf.Lerp(x0, x1, (float)i / m));
            }
            Stroke(net, s_Crack, tar ? 0.07f : 0.035f, tar ? net.Look.Tar : net.Look.Crack);
        }

        // A crack along the road: down the joint by the centre line, or in a wheel path.
        private static void Longitudinal(Net net, Chain c, float s, float end, long key)
        {
            float s1 = Mathf.Min(s + Mathf.Lerp(2.5f, 9f, MapStyle.Unit(key, 0xC8)), Mathf.Min(end, c.To) - 0.3f);
            if (s1 - s < 1f || s_Lanes.Count == 0) return;
            bool centre = !c.W.OneWay && MapStyle.Unit(key, 0xC9) < 0.4f;
            float lane = s_Lanes[MapStyle.Pick(key, 0xCA, s_Lanes.Count)];
            float side = MapStyle.Unit(key, 0xCB) < 0.5f ? -1f : 1f;
            System.Random rng = MapStyle.Rng(key, 0xCC);
            int n = Mathf.Clamp(Mathf.CeilToInt((s1 - s) / 0.7f), 2, 20);
            s_Crack.Clear();
            float wander = 0f;
            for (int i = 0; i <= n; i++)
            {
                float ss = Mathf.Lerp(s, s1, (float)i / n);
                wander = Mathf.Clamp(wander + ((float)rng.NextDouble() - 0.5f) * 0.14f, -0.25f, 0.25f);
                float x = centre ? OffsetAt(c, ss, Along.Centre, side * 0.35f) : LaneAt(c, ss, lane) + side * 0.85f;
                PointOn(c, ss, out Vector2 p, out Vector2 d);
                s_Crack.Add(p + Left(d) * (x + wander));
            }
            Stroke(net, s_Crack, 0.03f, net.Look.Crack);
        }

        // A frost pothole in a wheel path: a ragged dark hollow with water in it.
        private static void Pothole(Net net, Chain c, float s, long key)
        {
            if (s_Lanes.Count == 0 || s < c.From + 1f || s > c.To - 1f) return;
            System.Random rng = MapStyle.Rng(key, 0xCD);
            float x = LaneAt(c, s, s_Lanes[MapStyle.Pick(key, 0xCE, s_Lanes.Count)]) + (rng.NextDouble() < 0.5 ? -0.8f : 0.8f);
            PointOn(c, s, out Vector2 p, out Vector2 d);
            Vector2 at = p + Left(d) * x;
            float r = MapStyle.Range(rng, 0.22f, 0.5f);
            s_Ring.Clear();
            for (int i = 0; i < 9; i++)
            {
                float ang = i * Mathf.PI * 2f / 9f;
                s_Ring.Add(at + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (r * MapStyle.Range(rng, 0.7f, 1.15f)));
            }
            s_Ring.Add(s_Ring[0]);
            FanOpen(net, at, s_Ring, WearY + 0.0003f, net.Look.Pothole);
        }

        // A straight line across the road (a concrete slab's joint).
        private static void SlabJoint(Net net, Chain c, float s, float width, in Surf surf)
        {
            if (s < c.From + 0.2f || s > c.To - 0.2f) return;
            PointOn(c, s, out Vector2 p, out Vector2 d);
            Vector2 n = Left(d);
            s_Crack.Clear();
            s_Crack.Add(p - n * (EdgeAt(c, -1, s) - 0.1f));
            s_Crack.Add(p + n * (EdgeAt(c, 1, s) - 0.1f));
            Stroke(net, s_Crack, width, surf);
        }

        /// <summary>A thin flat line along <paramref name="pts"/>, <paramref name="width"/> wide, on the asphalt.</summary>
        private static void Stroke(Net net, List<Vector2> pts, float width, in Surf surf) => StrokeAt(net, pts, width, surf, WearY);

        /// <summary>A thin flat line along <paramref name="pts"/> at height <paramref name="y"/>.</summary>
        private static void StrokeAt(Net net, List<Vector2> pts, float width, in Surf surf, float y)
        {
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], e = pts[i + 1] - a;
                float len = e.magnitude;
                if (len < 1e-3f) continue;
                Vector2 n = Left(e / len) * (width * 0.5f);
                Vector3 p0 = To3(a - n, y), p1 = To3(pts[i + 1] - n, y), q1 = To3(pts[i + 1] + n, y), q0 = To3(a + n, y);
                Face(net.Out.Flat((p0 + q1) * 0.5f), p0, p1, q1, q0, Vector3.up, surf);
            }
        }
    }
}
