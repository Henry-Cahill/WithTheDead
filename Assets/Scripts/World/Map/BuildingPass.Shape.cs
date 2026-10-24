using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What a footprint's outline says about the building on it: its frame (the axes of its smallest bounding rectangle),
    // how full that rectangle is, how round the outline is, whether its walls all run along the frame's axes, and, for
    // such an outline, the rectangles a roof can be built on: the biggest first (the main block), then the wings and
    // ells, found on the grid the outline's own corners make. Real footprints are mostly rectangles (44% have four
    // corners) and L, T and U shapes; the rest get a roof on their bounding rectangle or a flat one.
    internal static partial class BuildingPass
    {
        private const int MaxGrid = 18;   // cuts per axis past which an outline is too ragged to cut into blocks
        private const int MaxParts = 6;

        /// <summary>A footprint's frame and blocks. Frame coordinates (u, v): metres along <see cref="U"/> and
        /// <see cref="V"/> from <see cref="O"/>; V is U turned a quarter anticlockwise.</summary>
        private sealed class Shape
        {
            public Vector2 O, U, V;
            /// <summary>Half the bounding rectangle's sides: along U (the longer) and V.</summary>
            public float HalfL, HalfW;
            public float Area, Perimeter;
            /// <summary>The footprint's share of its bounding rectangle.</summary>
            public float Fill;
            /// <summary>4πA/P²: 1 for a circle, 0.785 for a square.</summary>
            public float Round;
            /// <summary>Every wall (bar a few short ones) runs along U or V.</summary>
            public bool Orthogonal;
            /// <summary>Per wall: 0 along U, 1 along V, -1 neither.</summary>
            public int[] EdgeAxis;
            /// <summary>The roof blocks in frame coordinates (x = u, y = v), the main one first; empty when the outline
            /// doesn't cut into blocks well.</summary>
            public readonly List<Rect> Blocks = new List<Rect>();
            /// <summary>The share of the footprint the blocks cover.</summary>
            public float Covered;

            public float Length => HalfL * 2f;
            public float Width => HalfW * 2f;
            public float Aspect => HalfL / Mathf.Max(0.25f, HalfW);
            /// <summary>The yaw (degrees) that turns a box's local x onto U (and its local z onto V).</summary>
            public float Yaw => -Mathf.Atan2(U.y, U.x) * Mathf.Rad2Deg;
            public Vector2 World(float u, float v) => O + U * u + V * v;
            public Vector2 World(Vector2 uv) => O + U * uv.x + V * uv.y;
            public Vector2 Local(Vector2 p)
            {
                Vector2 d = p - O;
                return new Vector2(Vector2.Dot(d, U), Vector2.Dot(d, V));
            }
            /// <summary>The frame's bounding rectangle in frame coordinates.</summary>
            public Rect Box => Rect.MinMaxRect(-HalfL, -HalfW, HalfL, HalfW);
        }

        private static Shape ShapeOf(Footprint f)
        {
            List<Vector2> p = f.Pts;
            int n = p.Count;
            var s = new Shape { Area = f.Size, EdgeAxis = new int[n] };
            float perimeter = 0f, longest = 0f;
            for (int i = 0; i < n; i++)
            {
                float len = Vector2.Distance(p[i], p[(i + 1) % n]);
                perimeter += len;
                longest = Mathf.Max(longest, len);
            }
            s.Perimeter = perimeter;
            s.Round = perimeter > 0.01f ? 4f * Mathf.PI * f.Size / (perimeter * perimeter) : 0f;

            // The smallest rectangle round the outline has a side along one of its walls; try each (the short ones
            // only when nothing else will do).
            float bestArea = float.MaxValue;
            Vector2 bestDir = Vector2.right;
            float minWall = Mathf.Min(0.5f, longest * 0.5f);
            for (int i = 0; i < n; i++)
            {
                Vector2 e = p[(i + 1) % n] - p[i];
                float len = e.magnitude;
                if (len < minWall) continue;
                Vector2 d = e / len, q = new Vector2(-d.y, d.x);
                float a0 = float.MaxValue, a1 = float.MinValue, b0 = float.MaxValue, b1 = float.MinValue;
                for (int k = 0; k < n; k++)
                {
                    float a = Vector2.Dot(p[k], d), b = Vector2.Dot(p[k], q);
                    if (a < a0) a0 = a;
                    if (a > a1) a1 = a;
                    if (b < b0) b0 = b;
                    if (b > b1) b1 = b;
                }
                float area = (a1 - a0) * (b1 - b0);
                if (area >= bestArea - 1e-3f) continue;
                bestArea = area;
                bestDir = d;
            }
            Vector2 dir = bestDir, perp = new Vector2(-dir.y, dir.x);
            float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue;
            for (int k = 0; k < n; k++)
            {
                float a = Vector2.Dot(p[k], dir), b = Vector2.Dot(p[k], perp);
                u0 = Mathf.Min(u0, a); u1 = Mathf.Max(u1, a);
                v0 = Mathf.Min(v0, b); v1 = Mathf.Max(v1, b);
            }
            if (v1 - v0 > u1 - u0)
            {
                // U along the longer side: turn the frame a quarter.
                Vector2 t = dir;
                dir = perp;
                perp = -t;
                float a0 = u0, a1 = u1;
                u0 = v0; u1 = v1;
                v0 = -a1; v1 = -a0;
            }
            // One way round for every footprint, so the same outline always gets the same frame.
            if (dir.x < -1e-4f || (Mathf.Abs(dir.x) <= 1e-4f && dir.y < 0f))
            {
                dir = -dir;
                perp = -perp;
                float a0 = u0;
                u0 = -u1; u1 = -a0;
                float b0 = v0;
                v0 = -v1; v1 = -b0;
            }
            s.U = dir;
            s.V = perp;
            s.O = dir * ((u0 + u1) * 0.5f) + perp * ((v0 + v1) * 0.5f);
            s.HalfL = (u1 - u0) * 0.5f;
            s.HalfW = (v1 - v0) * 0.5f;
            s.Fill = Mathf.Clamp01(f.Size / Mathf.Max(0.01f, s.Length * s.Width));

            // Walls along the axes; a little slant or a short chamfer still counts.
            float oblique = 0f;
            const float Along = 0.985f;   // cos 10°
            for (int i = 0; i < n; i++)
            {
                Vector2 e = p[(i + 1) % n] - p[i];
                float len = e.magnitude;
                if (len < 1e-3f) { s.EdgeAxis[i] = 0; continue; }
                Vector2 d = e / len;
                if (Mathf.Abs(Vector2.Dot(d, s.U)) >= Along) s.EdgeAxis[i] = 0;
                else if (Mathf.Abs(Vector2.Dot(d, s.V)) >= Along) s.EdgeAxis[i] = 1;
                else
                {
                    s.EdgeAxis[i] = -1;
                    if (len > 0.4f) oblique += len;
                }
            }
            s.Orthogonal = oblique <= Mathf.Max(1.2f, perimeter * 0.04f);
            Blocks(f, s);
            return s;
        }

        // Cuts an orthogonal outline into blocks on the grid of its own corners: the biggest rectangle inside it, then
        // the biggest of what's left, and so on. A near-rectangle is one block: its bounding rectangle.
        private static void Blocks(Footprint f, Shape s)
        {
            s.Blocks.Clear();
            s.Covered = 0f;
            if (f.Pts.Count <= 5 && s.Fill >= 0.9f || s.Fill >= 0.965f)
            {
                s.Blocks.Add(s.Box);
                s.Covered = 1f;
                return;
            }
            if (!s.Orthogonal) return;
            int n = f.Pts.Count;
            var local = new List<Vector2>(n);
            for (int i = 0; i < n; i++) local.Add(s.Local(f.Pts[i]));
            List<float> us = Cuts(local, true, -s.HalfL, s.HalfL), vs = Cuts(local, false, -s.HalfW, s.HalfW);
            int nu = us.Count - 1, nv = vs.Count - 1;
            if (nu < 1 || nv < 1 || nu > MaxGrid || nv > MaxGrid) return;
            var free = new bool[nu, nv];
            for (int i = 0; i < nu; i++)
                for (int j = 0; j < nv; j++)
                    free[i, j] = MapGeometry.Contains(local, new Vector2((us[i] + us[i + 1]) * 0.5f, (vs[j] + vs[j + 1]) * 0.5f));
            var sum = new int[nu + 1, nv + 1];
            float covered = 0f;
            for (int part = 0; part < MaxParts; part++)
            {
                for (int i = 0; i < nu; i++)
                    for (int j = 0; j < nv; j++)
                        sum[i + 1, j + 1] = sum[i, j + 1] + sum[i + 1, j] - sum[i, j] + (free[i, j] ? 1 : 0);
                float best = 0f;
                int bi0 = 0, bi1 = 0, bj0 = 0, bj1 = 0;
                for (int i0 = 0; i0 < nu; i0++)
                    for (int i1 = i0 + 1; i1 <= nu; i1++)
                    {
                        float w = us[i1] - us[i0];
                        for (int j0 = 0; j0 < nv; j0++)
                            for (int j1 = j0 + 1; j1 <= nv; j1++)
                            {
                                int cells = (i1 - i0) * (j1 - j0);
                                if (sum[i1, j1] - sum[i0, j1] - sum[i1, j0] + sum[i0, j0] != cells) break;   // taller ones won't fit either
                                float area = w * (vs[j1] - vs[j0]);
                                if (area <= best + 1e-4f) continue;
                                best = area;
                                bi0 = i0; bi1 = i1; bj0 = j0; bj1 = j1;
                            }
                    }
                float bw = us[bi1] - us[bi0], bh = vs[bj1] - vs[bj0];
                if (best < 2.5f || Mathf.Min(bw, bh) < 1.2f) break;
                s.Blocks.Add(Rect.MinMaxRect(us[bi0], vs[bj0], us[bi1], vs[bj1]));
                covered += best;
                for (int i = bi0; i < bi1; i++)
                    for (int j = bj0; j < bj1; j++)
                        free[i, j] = false;
            }
            s.Covered = Mathf.Clamp01(covered / Mathf.Max(1f, f.Size));
            if (s.Covered < 0.88f) s.Blocks.Clear();
        }

        // The distinct coordinates of an outline's corners along one axis, those within 0.3 m of each other merged, the
        // bounding rectangle's two sides always among them.
        private static List<float> Cuts(List<Vector2> local, bool alongU, float min, float max)
        {
            var raw = new List<float>(local.Count + 2) { min, max };
            for (int i = 0; i < local.Count; i++) raw.Add(alongU ? local[i].x : local[i].y);
            raw.Sort();
            var cuts = new List<float>(raw.Count);
            int start = 0;
            for (int i = 1; i <= raw.Count; i++)
            {
                if (i < raw.Count && raw[i] - raw[i - 1] < 0.3f) continue;
                float sum = 0f;
                for (int k = start; k < i; k++) sum += raw[k];
                float c = sum / (i - start);
                // The ends stay on the bounding rectangle.
                if (start == 0) c = min;
                else if (i == raw.Count) c = max;
                cuts.Add(c);
                start = i;
            }
            return cuts;
        }
    }
}
