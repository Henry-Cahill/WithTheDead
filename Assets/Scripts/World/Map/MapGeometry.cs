using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Plane geometry for the real-world map, in the raid map's ground plane: a <see cref="Vector2"/> is (x, z), metres
    /// east and north. Clips lines and polygons to rectangles, tells which way a polygon winds, and cuts polygons into
    /// triangles (ear clipping) wound clockwise seen from above, the way <see cref="RaidMesh.Tri"/> draws a face up.
    /// </summary>
    internal static class MapGeometry
    {
        /// <summary>Signed area: positive when the points run anticlockwise seen from above.</summary>
        public static float SignedArea(List<Vector2> p)
        {
            float a = 0f;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) a += p[j].x * p[i].y - p[i].x * p[j].y;
            return a * 0.5f;
        }

        /// <summary>The average of the points (good enough for footprints).</summary>
        public static Vector2 Middle(List<Vector2> p)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < p.Count; i++) sum += p[i];
            return p.Count > 0 ? sum / p.Count : sum;
        }

        /// <summary>Makes the polygon run anticlockwise seen from above (reverses it if it doesn't).</summary>
        public static void MakeAnticlockwise(List<Vector2> p)
        {
            if (SignedArea(p) < 0f) p.Reverse();
        }

        /// <summary>True when <paramref name="q"/> is inside the polygon (even-odd rule).</summary>
        public static bool Contains(List<Vector2> p, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            {
                if ((p[i].y > q.y) != (p[j].y > q.y)
                    && q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>The distance from <paramref name="q"/> to the nearest edge of the polygon.</summary>
        public static float DistanceToEdge(List<Vector2> p, Vector2 q)
        {
            float best = float.MaxValue;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
                best = Mathf.Min(best, DistanceToSegment(q, p[j], p[i]));
            return best;
        }

        public static float DistanceToSegment(Vector2 q, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(q, a + ab * t);
        }

        /// <summary>The pieces of a line inside the rectangle (Liang–Barsky per segment, joined where they meet).</summary>
        public static List<List<Vector2>> ClipLine(List<Vector2> line, Rect r)
        {
            var pieces = new List<List<Vector2>>();
            List<Vector2> cur = null;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector2 a = line[i], b = line[i + 1];
                if (!ClipSegment(a, b, r, out float t0, out float t1))
                {
                    if (cur != null && cur.Count > 1) pieces.Add(cur);
                    cur = null;
                    continue;
                }
                Vector2 start = t0 <= 0f ? a : Vector2.Lerp(a, b, t0);
                Vector2 end = t1 >= 1f ? b : Vector2.Lerp(a, b, t1);
                if (t0 > 0f || cur == null)
                {
                    if (cur != null && cur.Count > 1) pieces.Add(cur);
                    cur = new List<Vector2> { start };
                }
                cur.Add(end);
                if (t1 < 1f)
                {
                    pieces.Add(cur);
                    cur = null;
                }
            }
            if (cur != null && cur.Count > 1) pieces.Add(cur);
            return pieces;
        }

        private static bool ClipSegment(Vector2 a, Vector2 b, Rect r, out float t0, out float t1)
        {
            t0 = 0f;
            t1 = 1f;
            float dx = b.x - a.x, dy = b.y - a.y;
            float[] p = { -dx, dx, -dy, dy };
            float[] q = { a.x - r.xMin, r.xMax - a.x, a.y - r.yMin, r.yMax - a.y };
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Approximately(p[i], 0f))
                {
                    if (q[i] < 0f) return false;
                    continue;
                }
                float t = q[i] / p[i];
                if (p[i] < 0f)
                {
                    if (t > t1) return false;
                    if (t > t0) t0 = t;
                }
                else
                {
                    if (t < t0) return false;
                    if (t < t1) t1 = t;
                }
            }
            return true;
        }

        /// <summary>The polygon cut to the rectangle (Sutherland–Hodgman); may come back with fewer than 3 points.</summary>
        public static List<Vector2> ClipPolygon(List<Vector2> poly, Rect r)
        {
            List<Vector2> cur = poly;
            for (int edge = 0; edge < 4 && cur.Count > 0; edge++)
            {
                var next = new List<Vector2>(cur.Count + 4);
                for (int i = 0; i < cur.Count; i++)
                {
                    Vector2 a = cur[(i + cur.Count - 1) % cur.Count], b = cur[i];
                    bool ina = Inside(a, r, edge), inb = Inside(b, r, edge);
                    if (inb)
                    {
                        if (!ina) next.Add(Cross(a, b, r, edge));
                        next.Add(b);
                    }
                    else if (ina) next.Add(Cross(a, b, r, edge));
                }
                cur = next;
            }
            return cur;
        }

        private static bool Inside(Vector2 p, Rect r, int edge)
        {
            switch (edge)
            {
                case 0: return p.x >= r.xMin;
                case 1: return p.x <= r.xMax;
                case 2: return p.y >= r.yMin;
                default: return p.y <= r.yMax;
            }
        }

        private static Vector2 Cross(Vector2 a, Vector2 b, Rect r, int edge)
        {
            if (edge < 2)
            {
                float x = edge == 0 ? r.xMin : r.xMax;
                return new Vector2(x, a.y + (b.y - a.y) * (x - a.x) / (b.x - a.x));
            }
            float y = edge == 2 ? r.yMin : r.yMax;
            return new Vector2(a.x + (b.x - a.x) * (y - a.y) / (b.y - a.y), y);
        }

        /// <summary>
        /// Triangles covering a simple polygon (no holes), as index triples into <paramref name="poly"/>, each wound
        /// clockwise seen from above. Ear clipping: fine for footprints and clipped areas of a few hundred points;
        /// polygons over <paramref name="maxPoints"/> points, or that fold over themselves, come back partly or not at
        /// all covered.
        /// </summary>
        public static List<int> Triangulate(List<Vector2> poly, int maxPoints = 2000)
        {
            var result = new List<int>();
            int n = poly.Count;
            if (n < 3 || n > maxPoints) return result;
            var ring = new List<int>(n);
            bool anticlockwise = SignedArea(poly) > 0f;
            for (int i = 0; i < n; i++) ring.Add(anticlockwise ? i : n - 1 - i);   // work anticlockwise
            while (ring.Count > 3)
            {
                bool cut = false;
                for (int i = 0; i < ring.Count; i++)
                {
                    int a = ring[(i + ring.Count - 1) % ring.Count], b = ring[i], c = ring[(i + 1) % ring.Count];
                    if (Turn(poly[a], poly[b], poly[c]) <= 1e-6f || AnyInside(poly, ring, a, b, c)) continue;
                    result.Add(a);
                    result.Add(c);
                    result.Add(b);
                    ring.RemoveAt(i);
                    cut = true;
                    break;
                }
                if (!cut) return result;   // folds over itself: leave the rest uncovered
            }
            result.Add(ring[0]);
            result.Add(ring[2]);
            result.Add(ring[1]);
            return result;
        }

        // Positive for a left (anticlockwise) turn at b.
        private static float Turn(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);

        private static bool AnyInside(List<Vector2> poly, List<int> ring, int a, int b, int c)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                int k = ring[i];
                if (k == a || k == b || k == c) continue;
                Vector2 p = poly[k];
                if (p == poly[a] || p == poly[b] || p == poly[c]) continue;
                if (Turn(poly[a], poly[b], p) >= 0f && Turn(poly[b], poly[c], p) >= 0f && Turn(poly[c], poly[a], p) >= 0f)
                    return true;
            }
            return false;
        }
    }
}
