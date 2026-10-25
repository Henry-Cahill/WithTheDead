using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Plane geometry for the real-world map, in the raid map's ground plane: a <see cref="Vector2"/> is (x, z), metres
    /// east and north. Clips lines and polygons to rectangles, tells which way a polygon winds, cuts polygons into
    /// triangles (ear clipping) wound clockwise seen from above, the way <see cref="RaidMesh.Tri"/> draws a face up, and
    /// walks and offsets lines. Pure geometry: nothing here draws, or decides how anything looks.
    /// </summary>
    internal static class MapGeometry
    {
        private const float EPS = 1e-5f;

        #region Polygons
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

        /// <summary>The smallest rectangle holding the points.</summary>
        public static Rect Bounds(List<Vector2> p)
        {
            Extent(p, out float x0, out float z0, out float x1, out float z1);
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }

        /// <summary>The least and greatest x and z of the points.</summary>
        public static void Extent(List<Vector2> p, out float x0, out float z0, out float x1, out float z1)
        {
            x0 = z0 = float.MaxValue;
            x1 = z1 = float.MinValue;
            for (int i = 0; i < p.Count; i++)
            {
                x0 = Mathf.Min(x0, p[i].x); z0 = Mathf.Min(z0, p[i].y);
                x1 = Mathf.Max(x1, p[i].x); z1 = Mathf.Max(z1, p[i].y);
            }
        }

        /// <summary>Makes the polygon run anticlockwise seen from above (reverses it if it doesn't).</summary>
        public static void MakeAnticlockwise(List<Vector2> p)
        {
            if (SignedArea(p) < 0f) p.Reverse();
        }

        /// <summary>True when <paramref name="q"/> is inside the polygon (even-odd rule) or on its edge.</summary>
        public static bool Contains(List<Vector2> p, Vector2 q)
        {
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
                if (DistanceToSegment(q, p[j], p[i]) <= EPS) return true;
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

        /// <summary>The distance from <paramref name="q"/> to the segment from <paramref name="a"/> to <paramref name="b"/>.</summary>
        public static float DistanceToSegment(Vector2 q, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(q, a + ab * t);
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
        #endregion

        #region Clipping
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

            // Pieces that touch end to start (a segment re-entering where the last one left) are one piece.
            var merged = new List<List<Vector2>>();
            foreach (List<Vector2> piece in pieces)
            {
                if (piece == null || piece.Count < 2) continue;
                if (merged.Count > 0)
                {
                    List<Vector2> last = merged[merged.Count - 1];
                    if (Vector2.Distance(last[last.Count - 1], piece[0]) <= EPS)
                    {
                        for (int i = 1; i < piece.Count; i++) last.Add(piece[i]);
                        continue;
                    }
                }
                merged.Add(new List<Vector2>(piece));
            }
            return merged;
        }

        /// <summary>Clips a line segment to the rectangle using Liang–Barsky, returning the parametric range [t0,t1] of the inside portion.</summary>
        private static bool ClipSegment(Vector2 a, Vector2 b, Rect r, out float t0, out float t1)
        {
            t0 = 0f;
            t1 = 1f;
            // Compute the delta for the segment (direction vector components).
            float dx = b.x - a.x, dy = b.y - a.y;
            // p[i] holds the direction factor for the parametric equation; q[i] is the distance from the segment start to the clipping edge.
            // p[i] holds the direction factor for the parametric equation; q[i] is the distance from the segment start to the clipping edge.
            float[] p = { -dx, dx, -dy, dy };
            float[] q = { a.x - r.xMin, r.xMax - a.x, a.y - r.yMin, r.yMax - a.y };
            for (int i = 0; i < 4; i++)
            {
                // If the segment is parallel to the clipping edge (p[i] == 0), it is either completely outside (q[i] < 0) or inside (continue).
                // If the segment is parallel to the clipping edge (p[i] == 0), it is either completely outside (q[i] < 0) or inside (continue).
                if (Mathf.Approximately(p[i], 0f))
                {
                    if (q[i] < 0f) return false;
                    continue;
                }
                float t = q[i] / p[i];
                if (p[i] < 0f)
                {
                    // The intersection is an entering point; tighten the lower bound t0.
                    if (t > t1) return false;
                    if (t > t0) t0 = t;
                }
                else
                {
                    // The intersection is a leaving point; tighten the upper bound t1.
                    if (t < t0) return false;
                    if (t < t1) t1 = t;
                }
            }
            return true;
        }

        /// <summary>The polygon cut to the rectangle (Sutherland–Hodgman), anticlockwise, without near-duplicate points;
        /// empty when fewer than 3 points are left.</summary>
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
                    // When clipping against an edge, we may need to emit the intersection point where the segment enters or exits the rectangle.
                    // When clipping against an edge, we may need to emit the intersection point where the segment enters or exits the rectangle.
                    if (inb)
                    {
                        // If the current vertex is inside but the previous one was outside, add the intersection point at the clipping edge.
                        if (!ina) next.Add(Cross(a, b, r, edge));
                        // Add the current vertex.
                        next.Add(b);
                    }
                    else if (ina) next.Add(Cross(a, b, r, edge));
                }
                cur = next;
            }
            MakeAnticlockwise(cur);
            return RemoveNearDuplicates(cur);
        }

        // Drops points within EPS of the one before (and a last point back on the first): clipping makes them.
        private static List<Vector2> RemoveNearDuplicates(List<Vector2> src)
        {
            if (src == null || src.Count == 0) return new List<Vector2>();
            var result = new List<Vector2>(src.Count) { src[0] };
            for (int i = 1; i < src.Count; i++)
                if (Vector2.Distance(src[i], result[result.Count - 1]) > EPS) result.Add(src[i]);
            if (result.Count >= 2 && Vector2.Distance(result[0], result[result.Count - 1]) <= EPS) result.RemoveAt(result.Count - 1);
            return result.Count < 3 ? new List<Vector2>() : result;
        }

        private static bool Inside(Vector2 p, Rect r, int edge)
        {
            switch (edge)
            {
                case 0: return p.x >= r.xMin - EPS;
                case 1: return p.x <= r.xMax + EPS;
                case 2: return p.y >= r.yMin - EPS;
                default: return p.y <= r.yMax + EPS;
            }
        }

        // Where the segment from a to b crosses the rectangle's edge (a segment along the edge gives its start).
        private static Vector2 Cross(Vector2 a, Vector2 b, Rect r, int edge)
        {
            if (edge < 2)
            {
                float x = edge == 0 ? r.xMin : r.xMax;
                if (Mathf.Approximately(b.x, a.x)) return new Vector2(x, a.y);
                return new Vector2(x, a.y + (b.y - a.y) * (x - a.x) / (b.x - a.x));
            }
            float y = edge == 2 ? r.yMin : r.yMax;
            if (Mathf.Approximately(b.y, a.y)) return new Vector2(a.x, y);
            return new Vector2(a.x + (b.x - a.x) * (y - a.y) / (b.y - a.y), y);
        }
        #endregion

        #region Lines
        /// <summary>Points every <paramref name="interval"/> metres along a line, measured along it across its bends, the
        /// first one <paramref name="interval"/> in from its start.</summary>
        public static IEnumerable<Vector2> SampleAlong(List<Vector2> pts, float interval)
        {
            if (pts.Count < 2) yield break;
            float accumulated = 0f;   // the last point, from the current segment's start (0 or less: on an earlier segment)
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float segLen = Vector2.Distance(a, b);
                Vector2 dir = (b - a) / segLen;
                while (accumulated + interval <= segLen)
                {
                    accumulated += interval;
                    yield return a + dir * accumulated;
                }
                accumulated -= segLen;
            }
        }

        /// <summary>The unit direction of the segment of <paramref name="line"/> that <paramref name="point"/> lies on (the
        /// later one at a vertex), or of its first segment when it lies on none.</summary>
        public static Vector2 DirectionAt(List<Vector2> line, Vector2 point)
        {
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector2 a = line[i], b = line[i + 1];
                float segLen = Vector2.Distance(a, b);
                if (segLen < 1e-6f) continue;
                if (Mathf.Abs(Vector2.Distance(a, point) + Vector2.Distance(point, b) - segLen) < 0.01f) return (b - a).normalized;
            }
            return (line[1] - line[0]).normalized;
        }

        /// <summary>The line moved sideways by <paramref name="offset"/> (to the left of its direction when positive),
        /// mitred at the bends (sharp bends cut short).</summary>
        public static List<Vector2> OffsetLine(List<Vector2> pts, float offset)
        {
            int n = pts.Count;
            var moved = new List<Vector2>(n);
            if (n < 2)
            {
                moved.AddRange(pts);
                return moved;
            }
            for (int i = 0; i < n; i++)
            {
                // For each vertex we compute the normalized direction of the incoming and outgoing segments.
                Vector2 dPrev = (i > 0 ? pts[i] - pts[i - 1] : pts[1] - pts[0]).normalized;
                Vector2 dNext = (i < n - 1 ? pts[i + 1] - pts[i] : dPrev).normalized;
                // Compute left‑hand normals for the incoming and outgoing directions; they point outward from the segment.
                Vector2 nPrev = new Vector2(-dPrev.y, dPrev.x), nNext = new Vector2(-dNext.y, dNext.x);
                // The miter vector is the sum of the two normals; it gives the direction of the offset at the vertex.
                Vector2 miter = nPrev + nNext;
                // Normalise the miter unless the two normals cancel (sharp 180° turn); fall back to the outgoing normal.
                miter = miter.sqrMagnitude > 1e-6f ? miter.normalized : nNext;
                // Stretch factor compensates for the angle between the miter and the outgoing normal; clamp to avoid extreme scaling.
                float stretch = 1f / Mathf.Max(0.4f, Vector2.Dot(miter, nNext));
                moved.Add(pts[i] + miter * (offset * stretch));
            }
            return moved;
        }
        #endregion
    }
}
