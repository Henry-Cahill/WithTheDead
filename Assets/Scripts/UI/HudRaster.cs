namespace CloneZ
{
    /// <summary>
    /// A tiny anti-aliased vector rasteriser for the HUD's procedural line art. Every primitive is a
    /// signed distance field evaluated per pixel, so each edge gets about one pixel of smooth coverage
    /// at any texture size. Shapes are authored in "units" (64 across a square icon by default, y up,
    /// origin bottom-left) and can be rotated as a group with <see cref="SetTransform"/>.
    /// Pure C# with no engine types: <see cref="HudIcons"/> turns the coverage into sprites.
    /// </summary>
    public sealed class HudRaster
    {
        /// <summary>Texture size in pixels.</summary>
        public readonly int Width, Height;
        /// <summary>Pixels per authoring unit.</summary>
        public readonly float PixelsPerUnit;
        /// <summary>Coverage per pixel (0..1), row-major from the bottom row up.</summary>
        public readonly float[] Alpha;

        /// <summary>When true, primitives cut coverage away instead of adding it.</summary>
        public bool Erase;

        private float _cos = 1f, _sin, _pivotX, _pivotY, _offX, _offY;

        private delegate float Sdf(float x, float y);

        public HudRaster(int width, int height, float unitsAcross = 64f)
        {
            Width = width;
            Height = height;
            PixelsPerUnit = width / unitsAcross;
            Alpha = new float[width * height];
        }

        /// <summary>Rotates (counter-clockwise, degrees) every following shape about a pivot, then shifts it.</summary>
        public void SetTransform(float rotationDeg, float pivotX = 32f, float pivotY = 32f, float offsetX = 0f, float offsetY = 0f)
        {
            double r = rotationDeg * System.Math.PI / 180.0;
            _cos = (float)System.Math.Cos(r);
            _sin = (float)System.Math.Sin(r);
            _pivotX = pivotX; _pivotY = pivotY;
            _offX = offsetX; _offY = offsetY;
        }

        /// <summary>Back to the identity transform.</summary>
        public void ResetTransform() => SetTransform(0f, 0f, 0f);

        #region Primitives
        /// <summary>A stroke with round caps.</summary>
        public void Line(float x0, float y0, float x1, float y1, float width)
        {
            float h = width * 0.5f;
            Fill(Min(x0, x1) - h, Min(y0, y1) - h, Max(x0, x1) + h, Max(y0, y1) + h,
                (x, y) => SegDist(x, y, x0, y0, x1, y1) - h);
        }

        /// <summary>A polyline through (x, y) pairs with round joins; closed joins the last point to the first.</summary>
        public void Path(float width, bool closed, params float[] xy)
        {
            int n = xy.Length / 2;
            for (int i = 0; i + 1 < n; i++)
                Line(xy[2 * i], xy[2 * i + 1], xy[2 * i + 2], xy[2 * i + 3], width);
            if (closed && n > 2)
                Line(xy[2 * n - 2], xy[2 * n - 1], xy[0], xy[1], width);
        }

        /// <summary>A filled circle.</summary>
        public void Disc(float cx, float cy, float r)
        {
            Fill(cx - r, cy - r, cx + r, cy + r, (x, y) => Len(x - cx, y - cy) - r);
        }

        /// <summary>A circle outline.</summary>
        public void Ring(float cx, float cy, float r, float width)
        {
            float h = width * 0.5f, e = r + h;
            Fill(cx - e, cy - e, cx + e, cy + e, (x, y) => Abs(Len(x - cx, y - cy) - r) - h);
        }

        /// <summary>A circular arc stroke with round caps, counter-clockwise from <paramref name="fromDeg"/>
        /// to <paramref name="toDeg"/> (0° = +x; the end may exceed 360).</summary>
        public void Arc(float cx, float cy, float r, float width, float fromDeg, float toDeg)
        {
            float h = width * 0.5f, e = r + h;
            float sweep = toDeg - fromDeg;
            double a0 = fromDeg * System.Math.PI / 180.0, a1 = toDeg * System.Math.PI / 180.0;
            float ex0 = cx + r * (float)System.Math.Cos(a0), ey0 = cy + r * (float)System.Math.Sin(a0);
            float ex1 = cx + r * (float)System.Math.Cos(a1), ey1 = cy + r * (float)System.Math.Sin(a1);
            Fill(cx - e, cy - e, cx + e, cy + e, (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float ang = (float)(System.Math.Atan2(dy, dx) * 180.0 / System.Math.PI);
                float rel = ang - fromDeg;
                rel -= 360f * (float)System.Math.Floor(rel / 360f);
                if (rel <= sweep) return Abs(Len(dx, dy) - r) - h;
                return Min(Len(x - ex0, y - ey0), Len(x - ex1, y - ey1)) - h;
            });
        }

        /// <summary>A filled axis-aligned ellipse.</summary>
        public void Ellipse(float cx, float cy, float rx, float ry)
        {
            Fill(cx - rx, cy - ry, cx + rx, cy + ry, (x, y) => EllipseDist(x - cx, y - cy, rx, ry));
        }

        /// <summary>An axis-aligned ellipse outline.</summary>
        public void EllipseRing(float cx, float cy, float rx, float ry, float width)
        {
            float h = width * 0.5f;
            Fill(cx - rx - h, cy - ry - h, cx + rx + h, cy + ry + h, (x, y) => Abs(EllipseDist(x - cx, y - cy, rx, ry)) - h);
        }

        /// <summary>A filled rounded rectangle.</summary>
        public void Box(float x0, float y0, float x1, float y1, float radius)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, hx = Abs(x1 - x0) * 0.5f, hy = Abs(y1 - y0) * 0.5f;
            Fill(cx - hx, cy - hy, cx + hx, cy + hy, (x, y) => BoxDist(x - cx, y - cy, hx, hy, radius));
        }

        /// <summary>A rounded rectangle outline (the stroke is centred on the edge).</summary>
        public void BoxOutline(float x0, float y0, float x1, float y1, float radius, float width)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, hx = Abs(x1 - x0) * 0.5f, hy = Abs(y1 - y0) * 0.5f;
            float h = width * 0.5f;
            Fill(cx - hx - h, cy - hy - h, cx + hx + h, cy + hy + h, (x, y) => Abs(BoxDist(x - cx, y - cy, hx, hy, radius)) - h);
        }

        /// <summary>A filled polygon through (x, y) pairs (even-odd rule, any winding).</summary>
        public void Polygon(params float[] xy)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i + 1 < xy.Length; i += 2)
            {
                minX = Min(minX, xy[i]); maxX = Max(maxX, xy[i]);
                minY = Min(minY, xy[i + 1]); maxY = Max(maxY, xy[i + 1]);
            }
            Fill(minX, minY, maxX, maxY, (x, y) => PolyDist(x, y, xy));
        }

        /// <summary>A star / burst: <paramref name="points"/> tips alternating between two radii.</summary>
        public void Star(float cx, float cy, int points, float outer, float inner, float startDeg)
        {
            var xy = new float[points * 4];
            for (int i = 0; i < points * 2; i++)
            {
                double a = (startDeg + i * 180.0 / points) * System.Math.PI / 180.0;
                float r = (i & 1) == 0 ? outer : inner;
                xy[2 * i] = cx + r * (float)System.Math.Cos(a);
                xy[2 * i + 1] = cy + r * (float)System.Math.Sin(a);
            }
            Polygon(xy);
        }

        /// <summary>A radial glow: full at the centre fading to nothing at <paramref name="r"/> (not an SDF shape).</summary>
        public void Glow(float cx, float cy, float r, float power)
        {
            for (int py = 0; py < Height; py++)
                for (int px = 0; px < Width; px++)
                {
                    float ux = (px + 0.5f) / PixelsPerUnit, uy = (py + 0.5f) / PixelsPerUnit;
                    float t = 1f - Len(ux - cx, uy - cy) / r;
                    if (t <= 0f) continue;
                    float a = (float)System.Math.Pow(t, power);
                    Plot(py * Width + px, a);
                }
        }

        /// <summary>Clears every pixel.</summary>
        public void Clear() => System.Array.Clear(Alpha, 0, Alpha.Length);
        #endregion

        #region Core
        // Evaluates a local-space SDF over the transformed bounding box of a shape.
        private void Fill(float x0, float y0, float x1, float y1, Sdf sdf)
        {
            const float pad = 1.5f;
            float bx0 = float.MaxValue, by0 = float.MaxValue, bx1 = float.MinValue, by1 = float.MinValue;
            Corner(x0, y0, ref bx0, ref by0, ref bx1, ref by1);
            Corner(x1, y0, ref bx0, ref by0, ref bx1, ref by1);
            Corner(x0, y1, ref bx0, ref by0, ref bx1, ref by1);
            Corner(x1, y1, ref bx0, ref by0, ref bx1, ref by1);

            int ix0 = Clamp((int)System.Math.Floor(bx0 * PixelsPerUnit - pad), 0, Width - 1);
            int ix1 = Clamp((int)System.Math.Ceiling(bx1 * PixelsPerUnit + pad), 0, Width - 1);
            int iy0 = Clamp((int)System.Math.Floor(by0 * PixelsPerUnit - pad), 0, Height - 1);
            int iy1 = Clamp((int)System.Math.Ceiling(by1 * PixelsPerUnit + pad), 0, Height - 1);

            for (int py = iy0; py <= iy1; py++)
            {
                for (int px = ix0; px <= ix1; px++)
                {
                    // Pixel centre -> canvas units -> local (inverse rotation about the pivot).
                    float dx = (px + 0.5f) / PixelsPerUnit - _offX - _pivotX;
                    float dy = (py + 0.5f) / PixelsPerUnit - _offY - _pivotY;
                    float lx = _pivotX + dx * _cos + dy * _sin;
                    float ly = _pivotY - dx * _sin + dy * _cos;
                    float cov = 0.5f - sdf(lx, ly) * PixelsPerUnit;
                    if (cov <= 0f) continue;
                    Plot(py * Width + px, cov > 1f ? 1f : cov);
                }
            }
        }

        private void Plot(int i, float cov)
        {
            if (Erase) { float v = 1f - cov; if (v < Alpha[i]) Alpha[i] = v; }
            else if (cov > Alpha[i]) Alpha[i] = cov;
        }

        // Grows a canvas-space bounding box by one local corner pushed through the transform.
        private void Corner(float x, float y, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            float dx = x - _pivotX, dy = y - _pivotY;
            float cx = _pivotX + dx * _cos - dy * _sin + _offX;
            float cy = _pivotY + dx * _sin + dy * _cos + _offY;
            minX = Min(minX, cx); maxX = Max(maxX, cx);
            minY = Min(minY, cy); maxY = Max(maxY, cy);
        }

        private static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            float len2 = bax * bax + bay * bay;
            float h = len2 > 1e-6f ? (pax * bax + pay * bay) / len2 : 0f;
            h = h < 0f ? 0f : h > 1f ? 1f : h;
            return Len(pax - bax * h, pay - bay * h);
        }

        private static float BoxDist(float px, float py, float hx, float hy, float r)
        {
            r = Min(r, Min(hx, hy));
            float qx = Abs(px) - hx + r, qy = Abs(py) - hy + r;
            float ox = Max(qx, 0f), oy = Max(qy, 0f);
            return Len(ox, oy) + Min(Max(qx, qy), 0f) - r;
        }

        // Inigo Quilez's cheap ellipse distance approximation — plenty for one pixel of anti-aliasing.
        private static float EllipseDist(float px, float py, float rx, float ry)
        {
            float k0 = Len(px / rx, py / ry);
            float k1 = Len(px / (rx * rx), py / (ry * ry));
            if (k1 < 1e-6f) return -Min(rx, ry);
            return k0 * (k0 - 1f) / k1;
        }

        private static float PolyDist(float px, float py, float[] v)
        {
            int n = v.Length / 2;
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = v[2 * i], yi = v[2 * i + 1], xj = v[2 * j], yj = v[2 * j + 1];
                d = Min(d, SegDist(px, py, xj, yj, xi, yi));
                if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside ? -d : d;
        }

        private static float Len(float x, float y) => (float)System.Math.Sqrt(x * x + y * y);
        private static float Abs(float v) => v < 0f ? -v : v;
        private static float Min(float a, float b) => a < b ? a : b;
        private static float Max(float a, float b) => a > b ? a : b;
        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
        #endregion
    }
}
