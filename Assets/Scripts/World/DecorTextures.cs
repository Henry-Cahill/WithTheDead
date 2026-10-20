using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>The interior wall finishes the raid map paints rooms with: a dark lower wainscot under a
    /// pale paper, PZ / reference-photo style.</summary>
    internal enum WainscotStyle { Beadboard, Panels, Paint }

    internal enum PaperStyle { Stripes, Damask, Pinstripe, Plain, Floral }

    internal enum ExteriorStyle { Siding, Brick, CinderBlock, Canvas, Stucco }

    /// <summary>One room's wall scheme (lower wainscot + upper paper + trims).</summary>
    internal sealed class WallDesign
    {
        public string Key;
        public Color Wainscot;
        public Color Paper;
        public Color Trim;        // baseboard + chair rail
        public WainscotStyle Lower;
        public PaperStyle Upper;
        public float RailHeight = 0.9f;
        /// <summary>How aged the finish is: 1 = the raid town's grime, smoke, water streaks and peeling
        /// paper; 0 = freshly hung.</summary>
        public float Wear = 1f;
    }

    /// <summary>
    /// Procedurally painted textures for the raid map — snow, asphalt, linoleum, planks, ceiling tiles,
    /// wallpaper and wainscot, siding and brick, rugs, posters, papers and signs — and the summer lawn,
    /// mulch and asphalt around the wave-survival starter home. Every texture is made once per session
    /// and cached; all are tileable where they repeat. Nothing is loaded from disk.
    /// </summary>
    internal static class DecorTextures
    {
        /// <summary>World height a wall texture spans (its v runs 0..1 from floor to wall top), and the
        /// horizontal distance it covers before repeating.</summary>
        public const float WallSpan = 2.8f;

        private static readonly Dictionary<string, Texture2D> s_Cache = new Dictionary<string, Texture2D>(64);

        #region Noise
        private static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        private static float R01(int x, int y, int seed) => (Hash(x, y, seed) & 0xFFFFFF) / 16777215f;

        private static int Wrap(int i, int n) { int r = i % n; return r < 0 ? r + n : r; }

        /// <summary>Tileable value noise over the unit square with <paramref name="cells"/> lattice cells.</summary>
        private static float Value(float u, float v, int cells, int seed)
        {
            float x = u * cells, y = v * cells;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int x0 = Wrap(xi, cells), x1 = Wrap(xi + 1, cells);
            int y0 = Wrap(yi, cells), y1 = Wrap(yi + 1, cells);
            float a = R01(x0, y0, seed), b = R01(x1, y0, seed);
            float c = R01(x0, y1, seed), d = R01(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Fbm(float u, float v, int cells, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Value(u, v, cells << o, seed + o * 31) * amp;
                norm += amp;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        private static Color32 C32(Color c) => new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255), 255);

        private static Color Mul(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);
        #endregion

        #region Cache
        private static bool TryGet(string key, out Texture2D tex) => s_Cache.TryGetValue(key, out tex) && tex != null;

        private static Texture2D Finish(string key, int w, int h, Color32[] px, bool repeat = true, bool point = false)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = "Raid_" + key,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = point ? FilterMode.Point : FilterMode.Bilinear,
                anisoLevel = 4,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            t.SetPixels32(px);
            t.Apply(true, false);
            s_Cache[key] = t;
            return t;
        }
        #endregion

        #region Ground and floors
        /// <summary>Wind-packed snow, pale blue-grey with soft drifts and the odd glint (repeats ~9 m).</summary>
        public static Texture2D Snow()
        {
            if (TryGet("snow", out Texture2D t)) return t;
            const int S = 256;
            var px = new Color32[S * S];
            var dark = new Color(0.70f, 0.75f, 0.83f);
            var light = new Color(0.93f, 0.95f, 0.99f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float n = Fbm(u, v, 3, 4, 11) * 0.75f + Fbm(u, v, 24, 2, 12) * 0.25f;
                    Color c = Color.Lerp(dark, light, Mathf.SmoothStep(0.2f, 0.8f, n));
                    float r = R01(x, y, 13);
                    if (r > 0.997f) c = Color.white;
                    else if (r < 0.006f) c = Mul(c, 0.82f);
                    px[y * S + x] = C32(c);
                }
            return Finish("snow", S, S, px);
        }

        /// <summary>Old cracked asphalt with packed snow in the low spots (repeats ~6 m).</summary>
        public static Texture2D Asphalt()
        {
            if (TryGet("asphalt", out Texture2D t)) return t;
            const int S = 256;
            var px = new Color32[S * S];
            var snow = new Color(0.56f, 0.59f, 0.65f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float n = Fbm(u, v, 4, 4, 21);
                    float g = 0.15f + n * 0.07f + (R01(x, y, 22) - 0.5f) * 0.05f;
                    Color c = new Color(g, g, g * 1.04f);
                    float s = Fbm(u, v, 3, 3, 23);
                    if (s > 0.62f) c = Color.Lerp(c, snow, Mathf.Clamp01((s - 0.62f) / 0.14f) * 0.85f);
                    px[y * S + x] = C32(c);
                }
            // Cracks: a few wandering dark lines (wrapped so the texture still tiles).
            var rng = new System.Random(24);
            var crack = new Color32(22, 22, 24, 255);
            for (int k = 0; k < 7; k++)
            {
                float cx = rng.Next(S), cy = rng.Next(S);
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2f);
                int len = 40 + rng.Next(90);
                for (int i = 0; i < len; i++)
                {
                    ang += ((float)rng.NextDouble() - 0.5f) * 0.7f;
                    cx += Mathf.Cos(ang); cy += Mathf.Sin(ang);
                    px[Wrap((int)cy, S) * S + Wrap((int)cx, S)] = crack;
                    if (rng.NextDouble() < 0.4) px[Wrap((int)cy + 1, S) * S + Wrap((int)cx, S)] = crack;
                }
            }
            return Finish("asphalt", S, S, px);
        }

        /// <summary>Sidewalk / slab concrete with expansion joints at the texture edge (repeats 2 m).</summary>
        public static Texture2D Concrete()
        {
            if (TryGet("concrete", out Texture2D t)) return t;
            const int S = 128;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float g = 0.50f + Fbm(u, v, 4, 3, 31) * 0.12f + (R01(x, y, 32) - 0.5f) * 0.05f;
                    Color c = new Color(g, g * 0.99f, g * 0.96f);
                    if (x < 1 || y < 1) c = Mul(c, 0.6f);
                    px[y * S + x] = C32(c);
                }
            return Finish("concrete", S, S, px);
        }

        /// <summary>Rail ballast: dark grey-brown stones (repeats 1.5 m).</summary>
        public static Texture2D Gravel()
        {
            if (TryGet("gravel", out Texture2D t)) return t;
            const int S = 128;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float n = Value(u, v, 40, 41);
                    float g = 0.22f + n * 0.18f + (R01(x, y, 42) - 0.5f) * 0.06f;
                    px[y * S + x] = C32(new Color(g * 1.05f, g, g * 0.92f));
                }
            return Finish("gravel", S, S, px);
        }

        /// <summary>Sheet linoleum laid as 30 cm tiles with flecks, seams and scuffs (repeats 1.2 m).
        /// <paramref name="checker"/> alternates the base and second colour, kitchen style.</summary>
        public static Texture2D Linoleum(Color a, Color b, bool checker)
        {
            string key = "lino" + ColorKey(a) + ColorKey(b) + (checker ? "c" : "f");
            if (TryGet(key, out Texture2D t)) return t;
            const int S = 128, T = 32;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int tx = x / T, ty = y / T;
                    Color c = checker ? (((tx + ty) & 1) == 0 ? a : b) : Color.Lerp(a, b, R01(tx, ty, 51) * 0.35f);
                    c = Mul(c, 0.94f + R01(tx, ty, 52) * 0.1f);
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    c = Mul(c, 0.92f + Fbm(u, v, 4, 3, 53) * 0.14f);
                    float r = R01(x, y, 54);
                    if (r > 0.94f) c = Mul(c, 0.78f);
                    else if (r < 0.03f) c = Mul(c, 1.12f);
                    if (x % T == 0 || y % T == 0) c = Mul(c, 0.62f);
                    px[y * S + x] = C32(c);
                }
            return Finish(key, S, S, px);
        }

        /// <summary>Worn plank flooring running along X, 15 cm boards with butt joints (repeats 2.4 m).</summary>
        public static Texture2D WoodFloor(Color baseColor)
        {
            string key = "wood" + ColorKey(baseColor);
            if (TryGet(key, out Texture2D t)) return t;
            const int S = 256, Row = 16;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                int row = y / Row;
                int offset = (int)(R01(row, 0, 61) * S);
                for (int x = 0; x < S; x++)
                {
                    int xs = Wrap(x + offset, S);
                    int plank = xs / 96;
                    float tone = 0.78f + R01(row, plank, 62) * 0.36f;
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float grain = Mathf.Sin(u * Mathf.PI * 2f * 7f + Fbm(u, v, 4, 2, 63 + row) * 9f) * 0.05f;
                    Color c = Mul(baseColor, tone + grain + (R01(x, y, 64) - 0.5f) * 0.05f);
                    if (y % Row == 0) c = Mul(baseColor, 0.35f);
                    else if (xs % 96 == 0) c = Mul(baseColor, 0.45f);
                    px[y * S + x] = C32(c);
                }
            }
            return Finish(key, S, S, px);
        }

        /// <summary>Close-pile carpet: the colour with a fine, mottled weave (repeats 1 m).</summary>
        public static Texture2D Carpet(Color baseColor)
        {
            string key = "carpet" + ColorKey(baseColor);
            if (TryGet(key, out Texture2D t)) return t;
            const int S = 64;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float k = 0.86f + Fbm(u, v, 4, 2, 71) * 0.18f + (R01(x, y, 72) - 0.5f) * 0.12f;
                    px[y * S + x] = C32(Mul(baseColor, k));
                }
            return Finish(key, S, S, px);
        }

        /// <summary>Suspended-ceiling tiles (60 cm) in a dark T-bar grid, with water stains and a missing
        /// tile, as in the office photo (repeats 2.4 m).</summary>
        public static Texture2D CeilingTiles()
        {
            if (TryGet("ceiling", out Texture2D t)) return t;
            const int S = 256, T = 64;
            var px = new Color32[S * S];
            var tile = new Color(0.74f, 0.74f, 0.71f);
            var stain = new Color(0.50f, 0.42f, 0.30f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int tx = x / T, ty = y / T;
                    Color c = Mul(tile, 0.95f + R01(tx, ty, 81) * 0.08f);
                    float r = R01(x, y, 82);
                    if (r > 0.9f) c = Mul(c, 0.8f);
                    else if (r > 0.86f) c = Mul(c, 0.9f);

                    // Two stained tiles: brown blooms with a darker tide line.
                    if ((tx == 1 && ty == 2) || (tx == 3 && ty == 0))
                    {
                        float cx = tx * T + T * 0.5f, cy = ty * T + T * 0.45f;
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / (T * 0.42f);
                        d += (Value((x + 0.5f) / S, (y + 0.5f) / S, 16, 83) - 0.5f) * 0.5f;
                        if (d < 1f) c = Color.Lerp(c, stain, (1f - d) * 0.75f);
                        if (d > 0.85f && d < 1f) c = Mul(c, 0.8f);
                    }
                    // A missing tile: the dark void above with a dangling cable.
                    if (tx == 2 && ty == 3)
                    {
                        c = new Color(0.05f, 0.05f, 0.055f);
                        if (Mathf.Abs((x - tx * T) - (y - ty * T) * 0.6f - 12f) < 1.2f) c = new Color(0.18f, 0.16f, 0.12f);
                    }
                    if (x % T < 2 || y % T < 2) c = new Color(0.40f, 0.40f, 0.39f);
                    px[y * S + x] = C32(c);
                }
            return Finish("ceiling", S, S, px);
        }
        #endregion

        #region Walls
        /// <summary>An interior wall finish spanning the full wall height: baseboard, wainscot, chair rail
        /// and wallpaper, aged with grime and water streaks as far as the design's wear (repeats every 2.8 m).</summary>
        public static Texture2D WallInterior(WallDesign d)
        {
            string key = "wall_" + d.Key;
            if (TryGet(key, out Texture2D t)) return t;
            const int S = 256;
            const float mpp = WallSpan / S;   // metres per pixel
            var px = new Color32[S * S];
            int seed = StableHash(d.Key) & 0xFFFF;

            // Water streaks run down from the top at a few columns.
            var streakX = new int[5];
            var streakLen = new float[5];
            for (int i = 0; i < streakX.Length; i++)
            {
                streakX[i] = (int)(R01(i, 1, seed) * S);
                streakLen[i] = 0.3f + R01(i, 2, seed) * 1.1f;
            }

            for (int y = 0; y < S; y++)
            {
                float h = (y + 0.5f) * mpp;
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float hx = (x + 0.5f) * mpp;
                    Color c;
                    if (h < 0.1f) c = d.Trim;
                    else if (h < d.RailHeight - 0.02f) c = Wainscot(d, hx, h, x, y);
                    else if (h < d.RailHeight + 0.05f) c = Mul(d.Trim, h < d.RailHeight + 0.01f ? 0.8f : 1.05f);
                    else c = Paper(d, hx, h, x, y, seed);

                    float grime = 0.88f + Fbm(u, v, 4, 3, seed + 5) * 0.2f;
                    if (h > d.RailHeight + 0.05f)
                    {
                        // Darker toward the ceiling (smoke) and just above the rail (scuffs).
                        grime *= 1f - Mathf.Clamp01((h - 2.2f) / 0.6f) * 0.18f;
                        for (int i = 0; i < streakX.Length && d.Wear >= 0.5f; i++)
                        {
                            int dx = Mathf.Abs(x - streakX[i]);
                            if (dx > S / 2) dx = S - dx;
                            float top = WallSpan - streakLen[i];
                            if (dx < 3 && h > top) grime *= 0.86f + dx * 0.03f + (1f - (h - top) / streakLen[i]) * 0.05f;
                        }
                    }
                    grime = 1f + (grime - 1f) * d.Wear;
                    px[y * S + x] = C32(Mul(c, grime));
                }
            }
            return Finish(key, S, S, px);
        }

        private static Color Wainscot(WallDesign d, float hx, float h, int x, int y)
        {
            Color c = d.Wainscot;
            switch (d.Lower)
            {
                case WainscotStyle.Beadboard:
                {
                    float f = Mathf.Repeat(hx, 0.1f);
                    if (f < 0.008f) c = Mul(c, 0.55f);
                    else if (f < 0.016f) c = Mul(c, 1.15f);
                    c = Mul(c, 0.95f + R01(x / 9, 3, 91) * 0.1f);
                    break;
                }
                case WainscotStyle.Panels:
                {
                    float f = Mathf.Repeat(hx, 0.7f);
                    bool frame = f < 0.07f || f > 0.63f || h < 0.2f || h > 0.76f;
                    bool bevel = !frame && (f < 0.09f || f > 0.61f || h < 0.22f || h > 0.74f);
                    if (bevel) c = Mul(c, 0.7f);
                    else if (!frame) c = Mul(c, 1.08f);
                    break;
                }
                default:
                    if (h > d.RailHeight - 0.06f) c = Mul(c, 0.8f);
                    break;
            }
            return Mul(c, 0.96f + (R01(x, y, 92) - 0.5f) * 0.06f);
        }

        private static Color Paper(WallDesign d, float hx, float h, int x, int y, int seed)
        {
            Color c = d.Paper;
            switch (d.Upper)
            {
                case PaperStyle.Stripes:
                {
                    float f = Mathf.Repeat(hx, 0.2f);
                    if (f < 0.1f) c = Mul(c, 0.9f);
                    if (Mathf.Abs(f - 0.1f) < 0.006f) c = Mul(c, 0.78f);
                    break;
                }
                case PaperStyle.Pinstripe:
                {
                    float f = Mathf.Repeat(hx, 0.1f);
                    if (f < 0.006f) c = Mul(c, 0.72f);
                    break;
                }
                case PaperStyle.Damask:
                {
                    float fx = Mathf.Repeat(hx, 0.14f) / 0.14f - 0.5f;
                    float fy = Mathf.Repeat(h + (Mathf.Repeat(hx, 0.28f) < 0.14f ? 0f : 0.09f), 0.18f) / 0.18f - 0.5f;
                    float r = Mathf.Abs(fx) * 1.4f + Mathf.Abs(fy);
                    if (r < 0.3f) c = Mul(c, 0.82f);
                    else if (r < 0.36f) c = Mul(c, 0.92f);
                    break;
                }
                case PaperStyle.Floral:
                {
                    float fx = Mathf.Repeat(hx + (Mathf.Repeat(h, 0.32f) < 0.16f ? 0f : 0.08f), 0.16f) / 0.16f - 0.5f;
                    float fy = Mathf.Repeat(h, 0.16f) / 0.16f - 0.5f;
                    float r = Mathf.Sqrt(fx * fx + fy * fy);
                    if (r < 0.14f) c = Color.Lerp(c, new Color(0.55f, 0.25f, 0.22f), 0.45f);
                    else if (r < 0.26f && Mathf.Abs(Mathf.Sin(Mathf.Atan2(fy, fx) * 3f)) > 0.6f) c = Color.Lerp(c, new Color(0.35f, 0.42f, 0.30f), 0.35f);
                    break;
                }
            }
            // Peeling: occasional lighter blotches where the paper has lifted (aged walls only).
            float peel = Value((x + 0.5f) / 256f, (y + 0.5f) / 256f, 10, seed + 9);
            if (peel > 0.8f && d.Wear >= 0.5f) c = Color.Lerp(c, new Color(0.80f, 0.78f, 0.72f), 0.4f);
            return Mul(c, 0.97f + (R01(x, y, 93) - 0.5f) * 0.04f);
        }

        /// <summary>An exterior wall finish spanning the full wall height (siding boards, brick, cinder
        /// block, tent canvas or stucco) over a concrete foundation band (repeats every 2.8 m).</summary>
        public static Texture2D WallExterior(ExteriorStyle style, Color color)
        {
            string key = "ext" + (int)style + ColorKey(color);
            if (TryGet(key, out Texture2D t)) return t;
            const int S = 256;
            const float mpp = WallSpan / S;
            var px = new Color32[S * S];
            var mortar = new Color(0.55f, 0.54f, 0.50f);
            for (int y = 0; y < S; y++)
            {
                float h = (y + 0.5f) * mpp;
                for (int x = 0; x < S; x++)
                {
                    float hx = (x + 0.5f) * mpp;
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    Color c = color;
                    switch (style)
                    {
                        case ExteriorStyle.Siding:
                        {
                            float f = Mathf.Repeat(h - 0.3f, 0.2f) / 0.2f;
                            c = Mul(color, 0.78f + f * 0.28f);
                            if (f < 0.07f) c = Mul(color, 0.45f);
                            break;
                        }
                        case ExteriorStyle.Brick:
                        {
                            int course = Mathf.FloorToInt(h / 0.0778f);
                            float off = (course & 1) == 0 ? 0f : 0.1f;
                            float fx = Mathf.Repeat(hx + off, 0.2f);
                            float fy = Mathf.Repeat(h, 0.0778f);
                            c = Mul(color, 0.8f + R01(Mathf.FloorToInt((hx + off) / 0.2f), course, 101) * 0.35f);
                            if (fx < 0.012f || fy < 0.012f) c = mortar;
                            break;
                        }
                        case ExteriorStyle.CinderBlock:
                        {
                            int course = Mathf.FloorToInt(h / 0.2f);
                            float off = (course & 1) == 0 ? 0f : 0.2f;
                            float fx = Mathf.Repeat(hx + off, 0.4f);
                            float fy = Mathf.Repeat(h, 0.2f);
                            c = Mul(color, 0.9f + R01(Mathf.FloorToInt((hx + off) / 0.4f), course, 102) * 0.15f);
                            if (fx < 0.012f || fy < 0.012f) c = Mul(color, 0.7f);
                            break;
                        }
                        case ExteriorStyle.Canvas:
                        {
                            float f = Mathf.Repeat(hx, 1.4f);
                            if (f < 0.015f) c = Mul(color, 0.7f);
                            c = Mul(c, 0.92f + Fbm(u, v, 6, 3, 103) * 0.16f);
                            break;
                        }
                        default:
                            c = Mul(color, 0.9f + Fbm(u, v, 8, 3, 104) * 0.2f);
                            break;
                    }
                    if (style != ExteriorStyle.Canvas && h < 0.3f)
                        c = new Color(0.42f, 0.42f, 0.40f) * (0.9f + R01(x, y, 105) * 0.15f);
                    float grime = 0.86f + Fbm(u, v, 4, 3, 106) * 0.2f;
                    grime *= 1f - Mathf.Clamp01((0.9f - h) / 0.6f) * 0.15f;
                    px[y * S + x] = C32(Mul(c, grime));
                }
            }
            return Finish(key, S, S, px);
        }
        #endregion

        #region Props
        /// <summary>Rough-sawn boards for crates and pallets (repeats 0.6 m).</summary>
        public static Texture2D Planks(Color color)
        {
            string key = "planks" + ColorKey(color);
            if (TryGet(key, out Texture2D t)) return t;
            const int S = 64, Row = 16;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int row = y / Row;
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float k = 0.82f + R01(row, 1, 111) * 0.25f + Mathf.Sin(u * Mathf.PI * 2f * 3f + Fbm(u, v, 4, 2, 112) * 6f) * 0.05f;
                    Color c = Mul(color, k);
                    if (y % Row == 0) c = Mul(color, 0.4f);
                    int nx = x % 32, ny = y % Row;
                    if ((nx == 3 || nx == 28) && ny == 8) c = new Color(0.2f, 0.2f, 0.2f);
                    px[y * S + x] = C32(c);
                }
            return Finish(key, S, S, px);
        }

        /// <summary>Rusty corrugated sheet metal (repeats 0.8 m).</summary>
        public static Texture2D Corrugated(Color color)
        {
            string key = "corr" + ColorKey(color);
            if (TryGet(key, out Texture2D t)) return t;
            const int S = 64;
            var px = new Color32[S * S];
            var rust = new Color(0.42f, 0.24f, 0.12f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    Color c = Mul(color, 0.78f + 0.26f * Mathf.Sin(u * Mathf.PI * 2f * 8f));
                    // deterministic rust‑spot test for subtle rivet‑like specks
                    float spot = R01(x, y, 122);
                    if (spot < 0.018f) c = Mul(c, 0.45f); // dark centre of rivet (~2 % of pixels)
                    else if (spot < 0.030f) c = Mul(c, 0.65f); // brushed‑metal edge highlight
                    float r = Fbm(u, v, 4, 3, 121);
                    if (r > 0.55f) c = Color.Lerp(c, rust, Mathf.Clamp01((r - 0.55f) / 0.2f) * 0.8f);
                    // Add tiny low‑gloss metal scratches
                    float scr = R01(x, y, 124);
                    if (scr < 0.004f) {
                        int len = 2 + (int)(R01(x, y, 125) * 3);
                        float shade = 0.55f;
                        for (int i = 0; i < len; i++) {
                            int ix = Wrap(x + i, S);
                            px[y * S + ix] = C32(Mul(c, shade));
                        }
                    }
                    px[y * S + x] = C32(c);
                }
            return Finish(key, S, S, px);
        }

        /// <summary>Diagonal yellow / black hazard stripes (repeats 0.6 m).</summary>
        public static Texture2D HazardStripes()
        {
            if (TryGet("hazard", out Texture2D t)) return t;
            const int S = 64;
            var px = new Color32[S * S];
            var yel = new Color32(214, 170, 30, 255);
            var blk = new Color32(24, 22, 20, 255);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    px[y * S + x] = ((x + y) / 16) % 2 == 0 ? yel : blk;
            return Finish("hazard", S, S, px);
        }

        /// <summary>A worn Persian-style rug (fitted to the rug quad).</summary>
        public static Texture2D Rug(int variant)
        {
            string key = "rug" + variant;
            if (TryGet(key, out Texture2D t)) return t;
            Color field, border, accent, light;
            switch (variant % 3)
            {
                case 0: field = new Color(0.46f, 0.10f, 0.09f); border = new Color(0.12f, 0.14f, 0.26f); accent = new Color(0.78f, 0.62f, 0.34f); light = new Color(0.82f, 0.76f, 0.62f); break;
                case 1: field = new Color(0.14f, 0.20f, 0.36f); border = new Color(0.50f, 0.14f, 0.10f); accent = new Color(0.80f, 0.70f, 0.48f); light = new Color(0.78f, 0.74f, 0.64f); break;
                default: field = new Color(0.22f, 0.30f, 0.20f); border = new Color(0.44f, 0.30f, 0.12f); accent = new Color(0.76f, 0.60f, 0.30f); light = new Color(0.80f, 0.74f, 0.58f); break;
            }
            const int W = 128, H = 96;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int e = Mathf.Min(Mathf.Min(x, W - 1 - x), Mathf.Min(y, H - 1 - y));
                    Color c;
                    if (e < 3) c = Mul(border, 0.8f);
                    else if (e < 12) c = ((x + y) / 4) % 3 == 0 ? accent : border;
                    else if (e < 14) c = light;
                    else
                    {
                        float fx = (x - W * 0.5f) / (W * 0.5f - 14f), fy = (y - H * 0.5f) / (H * 0.5f - 14f);
                        float dm = Mathf.Abs(fx) * 1.3f + Mathf.Abs(fy) * 1.6f;
                        if (dm < 0.35f) c = accent;
                        else if (dm < 0.5f) c = border;
                        else if (dm < 0.58f) c = light;
                        else c = ((x / 6 + y / 6) & 1) == 0 && (x % 6 == 3 || y % 6 == 3) ? Mul(accent, 0.8f) : field;
                    }
                    float wear = Fbm((x + 0.5f) / W, (y + 0.5f) / H, 4, 3, 131 + variant);
                    c = Color.Lerp(c, light, Mathf.Clamp01((wear - 0.62f) * 2.5f) * 0.4f);
                    c = Mul(c, 0.9f + (R01(x, y, 132) - 0.5f) * 0.12f);
                    px[y * W + x] = C32(c);
                }
            return Finish(key, W, H, px, repeat: false);
        }

        /// <summary>A wall poster: 0 = "U.S. ARMY" recruiting poster, 1 = calendar, 2 = landscape
        /// print, 3 = "SAFETY FIRST" notice.</summary>
        public static Texture2D Poster(int kind)
        {
            string key = "poster" + kind;
            if (TryGet(key, out Texture2D t)) return t;
            const int W = 64, H = 96;
            var px = new Color32[W * H];
            switch (kind)
            {
                case 0: PaintArmyPoster(px, W, H); break;
                case 1: PaintCalendar(px, W, H); break;
                case 2: PaintLandscape(px, W, H); break;
                default: PaintSafety(px, W, H); break;
            }
            // Age the paper: yellowed, blotchy.
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float n = Fbm((x + 0.5f) / W, (y + 0.5f) / H, 4, 3, 141 + kind);
                    Color c = px[y * W + x];
                    c = Color.Lerp(c, new Color(0.72f, 0.64f, 0.44f), 0.12f + n * 0.12f);
                    px[y * W + x] = C32(Mul(c, 0.9f + n * 0.12f));
                }
            return Finish(key, W, H, px, repeat: false, point: true);
        }

        private static void Fill(Color32[] px, int w, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    px[y * w + x] = c;
        }

        private static bool InStar(float x, float y, float cx, float cy, float r)
        {
            // A five-pointed star: compare the radius against a 10-point star polygon outline.
            float dx = x - cx, dy = y - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > r) return false;
            float a = Mathf.Atan2(dx, dy);
            float seg = Mathf.PI * 2f / 10f;
            float k = Mathf.Repeat(a, seg * 2f) / seg;            // 0..2 across one point
            float tip = k < 1f ? k : 2f - k;                      // 0 at a tip, 1 between tips
            float limit = Mathf.Lerp(r, r * 0.42f, tip);
            return d < limit;
        }

        private static void PaintArmyPoster(Color32[] px, int w, int h)
        {
            var olive = new Color32(78, 88, 50, 255);
            var red = new Color32(170, 36, 30, 255);
            var white = new Color32(232, 228, 214, 255);
            var blue = new Color32(30, 44, 98, 255);
            var black = new Color32(18, 18, 16, 255);
            Fill(px, w, 0, 0, w, h, olive);
            // Stripes along the top.
            for (int i = 0; i < 4; i++) Fill(px, w, 0, h - 4 - i * 4, w, h - i * 4, (i & 1) == 0 ? red : white);
            // A white star on a blue disc.
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x - 32f, dy = y - 56f;
                    if (dx * dx + dy * dy < 17f * 17f) px[y * w + x] = blue;
                    if (InStar(x, y, 32f, 56f, 14f)) px[y * w + x] = white;
                }
            DecorFont.DrawCentered(px, w, h, "U.S.", 33, 31, 2, black);
            DecorFont.DrawCentered(px, w, h, "U.S.", 32, 32, 2, white);
            DecorFont.DrawCentered(px, w, h, "ARMY", 33, 15, 2, black);
            DecorFont.DrawCentered(px, w, h, "ARMY", 32, 16, 2, white);
            Fill(px, w, 0, 0, w, 2, red);
        }

        private static void PaintCalendar(Color32[] px, int w, int h)
        {
            var paper = new Color32(226, 222, 210, 255);
            var red = new Color32(160, 40, 34, 255);
            var grey = new Color32(120, 118, 112, 255);
            Fill(px, w, 0, 0, w, h, paper);
            PaintLandscapeArea(px, w, 4, 52, w - 4, h - 4);
            DecorFont.DrawCentered(px, w, h, "DEC 1993", 32, 49, 1, red);
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 7; c++)
                {
                    int x0 = 4 + c * 8, y0 = 4 + r * 7;
                    for (int x = x0; x < x0 + 8; x++) { px[y0 * w + x] = grey; }
                    for (int y = y0; y < y0 + 7; y++) { px[y * w + x0] = grey; }
                    if (r == 1 && c == 3) Fill(px, w, x0 + 2, y0 + 2, x0 + 6, y0 + 5, red);
                }
        }

        private static void PaintLandscape(Color32[] px, int w, int h)
        {
            Fill(px, w, 0, 0, w, h, new Color32(40, 34, 26, 255));
            PaintLandscapeArea(px, w, 3, 3, w - 3, h - 3);
        }

        private static void PaintLandscapeArea(Color32[] px, int w, int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    float v = (float)(y - y0) / (y1 - y0);
                    float u = (float)(x - x0) / (x1 - x0);
                    Color c = Color.Lerp(new Color(0.85f, 0.62f, 0.40f), new Color(0.30f, 0.45f, 0.70f), v);
                    float ridge = 0.45f + Mathf.Sin(u * 7f) * 0.08f + Mathf.Sin(u * 17f + 1f) * 0.04f;
                    if (v < ridge) c = Color.Lerp(new Color(0.18f, 0.26f, 0.16f), new Color(0.28f, 0.36f, 0.22f), v / ridge);
                    float sx = u - 0.72f, sy = v - 0.72f;
                    if (sx * sx + sy * sy < 0.006f) c = new Color(0.95f, 0.85f, 0.55f);
                    px[y * w + x] = C32(c);
                }
        }

        private static void PaintSafety(Color32[] px, int w, int h)
        {
            var yel = new Color32(222, 190, 60, 255);
            var green = new Color32(36, 110, 52, 255);
            var black = new Color32(20, 20, 18, 255);
            Fill(px, w, 0, 0, w, h, yel);
            Fill(px, w, 0, 0, w, 3, black); Fill(px, w, 0, h - 3, w, h, black);
            Fill(px, w, 26, 44, 38, 80, green); Fill(px, w, 14, 56, 50, 68, green);
            DecorFont.DrawCentered(px, w, h, "SAFETY", 32, 34, 1, black);
            DecorFont.DrawCentered(px, w, h, "FIRST", 32, 24, 1, black);
        }

        /// <summary>A loose sheet: 0 = typed memo, 1 = newsprint, 2 = lined notepaper (fitted UVs).</summary>
        public static Texture2D Paper(int kind)
        {
            string key = "paper" + kind;
            if (TryGet(key, out Texture2D t)) return t;
            const int W = 32, H = 40;
            var px = new Color32[W * H];
            var rng = new System.Random(150 + kind);
            Color bg = kind == 1 ? new Color(0.70f, 0.69f, 0.64f) : new Color(0.86f, 0.85f, 0.80f);
            for (int i = 0; i < px.Length; i++) px[i] = C32(Mul(bg, 0.94f + (float)rng.NextDouble() * 0.08f));
            var ink = new Color32(70, 70, 72, 255);
            if (kind == 2)
            {
                var blue = new Color32(120, 140, 180, 255);
                for (int y = 4; y < H - 4; y += 3) for (int x = 2; x < W - 2; x++) px[y * W + x] = blue;
                for (int y = 2; y < H - 2; y++) px[y * W + 5] = new Color32(190, 90, 90, 255);
                for (int y = 7; y < H - 6; y += 3)
                {
                    int len = 8 + rng.Next(W - 16);
                    for (int x = 7; x < 7 + len; x++) if (rng.NextDouble() < 0.7) px[(y + 1) * W + x] = ink;
                }
            }
            else
            {
                int cols = kind == 1 ? 2 : 1;
                int colW = (W - 4) / cols;
                if (kind == 1) for (int y = H - 9; y < H - 3; y++) for (int x = 2; x < W - 2; x++) px[y * W + x] = ink;
                for (int c = 0; c < cols; c++)
                    for (int y = kind == 1 ? H - 12 : H - 5; y > 3; y -= 2)
                    {
                        int len = colW - 2 - rng.Next(kind == 1 ? 3 : 10);
                        for (int x = 2 + c * colW; x < 2 + c * colW + len; x++) px[y * W + x] = ink;
                    }
            }
            return Finish(key, W, H, px, repeat: false);
        }

        /// <summary>Shelf stock seen from the front: rows of coloured boxes, tins and jars (fitted UVs;
        /// <paramref name="kind"/> 0 = groceries, 1 = books, 2 = medicine, 3 = tools and parts).</summary>
        public static Texture2D Stock(int kind)
        {
            string key = "stock" + kind;
            if (TryGet(key, out Texture2D t)) return t;
            const int W = 64, H = 32;
            var px = new Color32[W * H];
            var rng = new System.Random(160 + kind);
            Color shadow = new Color(0.06f, 0.06f, 0.06f);
            for (int i = 0; i < px.Length; i++) px[i] = C32(shadow);
            Color[] palette;
            switch (kind)
            {
                case 1: palette = new[] { new Color(0.45f, 0.12f, 0.10f), new Color(0.14f, 0.22f, 0.40f), new Color(0.20f, 0.34f, 0.20f), new Color(0.62f, 0.50f, 0.30f), new Color(0.30f, 0.20f, 0.14f), new Color(0.70f, 0.66f, 0.56f) }; break;
                case 2: palette = new[] { new Color(0.86f, 0.86f, 0.84f), new Color(0.80f, 0.30f, 0.26f), new Color(0.30f, 0.50f, 0.72f), new Color(0.90f, 0.80f, 0.40f), new Color(0.60f, 0.80f, 0.70f) }; break;
                case 3: palette = new[] { new Color(0.60f, 0.16f, 0.12f), new Color(0.24f, 0.26f, 0.28f), new Color(0.50f, 0.48f, 0.44f), new Color(0.70f, 0.56f, 0.20f), new Color(0.20f, 0.30f, 0.45f) }; break;
                default: palette = new[] { new Color(0.80f, 0.20f, 0.16f), new Color(0.90f, 0.75f, 0.25f), new Color(0.25f, 0.45f, 0.75f), new Color(0.85f, 0.85f, 0.80f), new Color(0.30f, 0.60f, 0.30f), new Color(0.90f, 0.50f, 0.20f) }; break;
            }
            int x0 = 0;
            while (x0 < W)
            {
                int iw = kind == 1 ? 2 + rng.Next(3) : 4 + rng.Next(6);
                int ih = kind == 1 ? 18 + rng.Next(12) : 10 + rng.Next(18);
                if (rng.NextDouble() < 0.18) { x0 += iw; continue; }   // a gap where something was taken
                Color c = palette[rng.Next(palette.Length)];
                for (int y = 0; y < ih && y < H; y++)
                    for (int x = x0; x < x0 + iw && x < W; x++)
                    {
                        float k = x == x0 ? 0.7f : 1f;
                        if (kind != 1 && y > ih / 3 && y < ih / 3 + 3) k *= 1.25f;
                        px[y * W + x] = C32(Mul(c, k));
                    }
                x0 += iw + (kind == 1 ? 0 : 1);
            }
            return Finish(key, W, H, px, repeat: false);
        }

        /// <summary>A painted sign board with <paramref name="text"/> (fitted UVs). Returns the texture;
        /// its aspect (width / height) is in <paramref name="aspect"/>.</summary>
        public static Texture2D Sign(string text, Color bg, Color fg, out float aspect)
        {
            string key = "sign_" + text + ColorKey(bg) + ColorKey(fg);
            const int Scale = 2, Pad = 4;
            int w = DecorFont.Width(text, Scale) + Pad * 2;
            int h = DecorFont.GlyphH * Scale + Pad * 2;
            aspect = (float)w / h;
            if (TryGet(key, out Texture2D t)) return t;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool edge = x < 1 || y < 1 || x >= w - 1 || y >= h - 1;
                    float n = R01(x, y, 171) * 0.08f;
                    px[y * w + x] = C32(edge ? Mul(fg, 0.8f) : Mul(bg, 0.94f + n));
                }
            DecorFont.Draw(px, w, h, text, Pad, h - 1 - Pad, Scale, C32(fg));
            return Finish(key, w, h, px, repeat: false, point: true);
        }

        /// <summary>A hazard placard: yellow triangle, "!" and "DANGER / TOXIC GAS" (fitted UVs).</summary>
        public static Texture2D HazardSign()
        {
            if (TryGet("hazsign", out Texture2D t)) return t;
            const int W = 64, H = 64;
            var px = new Color32[W * H];
            var white = new Color32(226, 224, 216, 255);
            var yel = new Color32(222, 186, 40, 255);
            var black = new Color32(18, 18, 16, 255);
            var red = new Color32(170, 30, 26, 255);
            Fill(px, W, 0, 0, W, H, white);
            Fill(px, W, 0, H - 14, W, H, red);
            DecorFont.DrawCentered(px, W, H, "DANGER", 32, H - 4, 1, white);
            for (int y = 18; y < 48; y++)
            {
                float half = (48 - y) * 0.62f;
                for (int x = 0; x < W; x++)
                {
                    float d = Mathf.Abs(x - 32f);
                    if (d < half) px[y * W + x] = d > half - 2.5f || y < 20 ? black : yel;
                }
            }
            Fill(px, W, 31, 26, 34, 38, black);
            Fill(px, W, 31, 22, 34, 24, black);
            DecorFont.DrawCentered(px, W, H, "TOXIC", 32, 15, 1, black);
            DecorFont.DrawCentered(px, W, H, "GAS", 32, 7, 1, black);
            return Finish("hazsign", W, H, px, repeat: false, point: true);
        }
        #endregion

        #region Summer ground (the wave-survival starter home)
        /// <summary>A mown summer lawn: mottled greens, short blade strokes and a few sun-bleached patches
        /// (repeats 3 m).</summary>
        public static Texture2D Grass()
        {
            if (TryGet("grass", out Texture2D t)) return t;
            const int S = 256;
            var px = new Color32[S * S];
            var dark = new Color(0.17f, 0.27f, 0.11f);
            var light = new Color(0.33f, 0.44f, 0.18f);
            var dry = new Color(0.50f, 0.47f, 0.27f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float n = Fbm(u, v, 4, 4, 151);
                    Color c = Color.Lerp(dark, light, Mathf.SmoothStep(0.25f, 0.75f, n));
                    float d = Fbm(u, v, 3, 3, 152);
                    if (d > 0.64f) c = Color.Lerp(c, dry, Mathf.Clamp01((d - 0.64f) / 0.16f) * 0.5f);
                    px[y * S + x] = C32(Mul(c, 0.94f + (R01(x, y, 153) - 0.5f) * 0.12f));
                }
            // Blade strokes: short streaks, lighter tips catching the sun and darker ones in shadow.
            var rng = new System.Random(154);
            for (int k = 0; k < 2600; k++)
            {
                int bx = rng.Next(S), by = rng.Next(S), len = 2 + rng.Next(4);
                float tone = rng.NextDouble() < 0.55 ? 1.22f : 0.74f;
                for (int i = 0; i < len; i++)
                {
                    int idx = Wrap(by + i, S) * S + Wrap(bx + (i == len - 1 && (k & 1) == 0 ? 1 : 0), S);
                    px[idx] = C32(Mul(px[idx], tone));
                }
            }
            return Finish("grass", S, S, px);
        }

        /// <summary>Bark mulch or turned garden soil in <paramref name="color"/>: clumped chips with pale
        /// flecks and dark hollows (repeats 1.5 m).</summary>
        public static Texture2D Mulch(Color color)
        {
            string key = "mulch" + ColorKey(color);
            if (TryGet(key, out Texture2D t)) return t;
            const int S = 128;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float chips = Value(u, v, 32, 161) * 0.6f + Fbm(u, v, 4, 3, 162) * 0.4f;
                    Color c = Mul(color, 0.72f + chips * 0.55f);
                    float r = R01(x, y, 163);
                    if (r > 0.96f) c = Mul(c, 1.35f);
                    else if (r < 0.08f) c = Mul(c, 0.6f);
                    px[y * S + x] = C32(c);
                }
            return Finish(key, S, S, px);
        }

        /// <summary>Sun-faded summer asphalt: grey aggregate with tar-sealed cracks and the odd oil stain
        /// (repeats ~6 m).</summary>
        public static Texture2D DryAsphalt()
        {
            if (TryGet("asphalt_dry", out Texture2D t)) return t;
            const int S = 256;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                    float n = Fbm(u, v, 4, 4, 171);
                    float g = 0.19f + n * 0.07f + (R01(x, y, 172) - 0.5f) * 0.06f;
                    Color c = new Color(g, g, g * 1.03f);
                    float oil = Fbm(u, v, 3, 3, 173);
                    if (oil > 0.7f) c = Mul(c, 1f - Mathf.Clamp01((oil - 0.7f) / 0.1f) * 0.3f);
                    px[y * S + x] = C32(c);
                }
            // Crack sealant: a few thin wandering tar lines (wrapped so the texture still tiles).
            var rng = new System.Random(174);
            var tar = new Color32(30, 30, 32, 255);
            for (int k = 0; k < 4; k++)
            {
                float cx = rng.Next(S), cy = rng.Next(S);
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2f);
                int len = 30 + rng.Next(70);
                for (int i = 0; i < len; i++)
                {
                    ang += ((float)rng.NextDouble() - 0.5f) * 0.5f;
                    cx += Mathf.Cos(ang); cy += Mathf.Sin(ang);
                    px[Wrap((int)cy, S) * S + Wrap((int)cx, S)] = tar;
                }
            }
            return Finish("asphalt_dry", S, S, px);
        }
        #endregion

        private static string ColorKey(Color c)
            => ((int)(c.r * 63f)).ToString("X2") + ((int)(c.g * 63f)).ToString("X2") + ((int)(c.b * 63f)).ToString("X2");

        /// <summary>A string hash that is the same on every run (string.GetHashCode may not be).</summary>
        public static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
                return h & 0x7FFFFFFF;
            }
        }
    }
}
