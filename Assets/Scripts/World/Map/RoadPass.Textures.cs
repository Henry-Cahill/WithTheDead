using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The road pass's own tileable textures, painted once a session and shared by every chunk: wet ploughed asphalt
    /// (salt-dusted where it's dried), road concrete, packed snow with grit, salted sidewalk concrete and snow-dusted rail
    /// ballast. Each repeats over a span that divides 252 m (<see cref="RaidMesh.PlanarPeriod"/>), so chunk seams never
    /// show. Candidates for <see cref="DecorMaterials"/>.
    /// </summary>
    internal static class RoadTextures
    {
        private static Texture2D s_Wet, s_Concrete, s_Packed, s_Sidewalk, s_Ballast;

        /// <summary>Ploughed asphalt: a fine aggregate under a faint, broad tone, nothing that would show the repeat (14 m);
        /// its cracks, seams and potholes are drawn along each road (RoadPass.Wear) so they never repeat.</summary>
        public static Texture2D WetAsphalt() => s_Wet != null ? s_Wet : s_Wet = Paint("Raid_roads_wet_asphalt_v2", 512, WetAsphaltAt, 0);

        /// <summary>Weathered road concrete (repeat 7 m).</summary>
        public static Texture2D RoadConcrete() => s_Concrete != null ? s_Concrete : s_Concrete = Paint("Raid_roads_concrete", 256, ConcreteAt, 0);

        /// <summary>Snow packed hard by tyres, grey with grit and the odd icy glint (repeat 9 m).</summary>
        public static Texture2D PackedSnow() => s_Packed != null ? s_Packed : s_Packed = Paint("Raid_roads_packed_snow", 256, PackedAt, 0);

        /// <summary>Sidewalk concrete with salt stains (repeat 3 m).</summary>
        public static Texture2D Sidewalk() => s_Sidewalk != null ? s_Sidewalk : s_Sidewalk = Paint("Raid_roads_sidewalk", 128, SidewalkAt, 0);

        /// <summary>Rail ballast with snow lying in drifts across it (repeat 3 m).</summary>
        public static Texture2D SnowyBallast() => s_Ballast != null ? s_Ballast : s_Ballast = Paint("Raid_roads_ballast", 128, BallastAt, 0);

        private delegate Color PixelAt(float u, float v, int x, int y);

        private static Texture2D Paint(string name, int size, PixelAt at, int cracks)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = C32(at((x + 0.5f) / size, (y + 0.5f) / size, x, y));
            // Cracks (and tar-sealed seams): wandering dark lines, wrapped so the texture still tiles.
            var rng = new System.Random(name.Length * 7919 + cracks);
            for (int k = 0; k < cracks * 3; k++)
            {
                float cx = rng.Next(size), cy = rng.Next(size), ang = (float)(rng.NextDouble() * Mathf.PI * 2f);
                int len = 30 + rng.Next(size);
                bool tar = k % 3 == 0;
                var colour = tar ? new Color32(14, 14, 16, 255) : new Color32(24, 24, 26, 255);
                for (int i = 0; i < len; i++)
                {
                    ang += ((float)rng.NextDouble() - 0.5f) * (tar ? 0.25f : 0.7f);
                    cx += Mathf.Cos(ang);
                    cy += Mathf.Sin(ang);
                    px[Wrap((int)cy, size) * size + Wrap((int)cx, size)] = colour;
                    if (tar) px[Wrap((int)cy + 1, size) * size + Wrap((int)cx, size)] = colour;
                }
            }
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            t.SetPixels32(px);
            t.Apply(true, false);
            return t;
        }

        private static Color WetAsphaltAt(float u, float v, int x, int y)
        {
            float tone = Fbm(u, v, 2, 2, 101);
            float g = 0.15f + 0.045f * (tone - 0.5f) + (R01(x, y, 102) - 0.5f) * 0.05f + (Value(u, v, 96, 103) - 0.5f) * 0.035f;
            var c = new Color(g * 0.97f, g, g * 1.06f);
            float r = R01(x, y, 104);
            if (r > 0.992f) c = Color.Lerp(c, new Color(0.40f, 0.40f, 0.41f), 0.5f);   // a pale stone, a salt grain
            else if (r < 0.004f) c *= 0.6f;
            c.a = 1f;
            return c;
        }

        private static Color ConcreteAt(float u, float v, int x, int y)
        {
            float g = 0.50f + Fbm(u, v, 4, 4, 201) * 0.12f + (R01(x, y, 202) - 0.5f) * 0.04f;
            var c = new Color(g, g * 0.99f, g * 0.96f);
            float wet = Fbm(u, v, 3, 3, 203);
            if (wet < 0.36f) c *= 0.82f + wet * 0.5f;
            c.a = 1f;
            return c;
        }

        private static Color PackedAt(float u, float v, int x, int y)
        {
            float n = Fbm(u, v, 3, 4, 301) * 0.7f + Fbm(u, v, 18, 2, 302) * 0.3f;
            Color c = Color.Lerp(new Color(0.62f, 0.62f, 0.63f), new Color(0.80f, 0.81f, 0.83f), Mathf.SmoothStep(0.25f, 0.8f, n));
            float r = R01(x, y, 303);
            if (r > 0.985f) c = Color.Lerp(c, new Color(0.44f, 0.40f, 0.36f), 0.7f);
            else if (r < 0.006f) c = new Color(0.92f, 0.94f, 0.97f);
            float grime = Fbm(u, v, 5, 3, 304);
            if (grime > 0.62f) c = Color.Lerp(c, new Color(0.55f, 0.53f, 0.50f), (grime - 0.62f) * 1.6f);
            return c;
        }

        private static Color SidewalkAt(float u, float v, int x, int y)
        {
            float g = 0.60f + Fbm(u, v, 4, 3, 401) * 0.10f + (R01(x, y, 402) - 0.5f) * 0.05f;
            var c = new Color(g, g * 0.99f, g * 0.97f);
            float salt = Fbm(u, v, 3, 3, 403);
            if (salt > 0.6f) c = Color.Lerp(c, new Color(0.80f, 0.80f, 0.79f), (salt - 0.6f) * 1.8f);
            if (R01(x, y, 404) > 0.995f) c *= 0.7f;
            c.a = 1f;
            return c;
        }

        private static Color BallastAt(float u, float v, int x, int y)
        {
            float stone = Value(u, v, 40, 501);
            float g = 0.22f + stone * 0.2f + (R01(x, y, 502) - 0.5f) * 0.06f;
            var c = new Color(g * 1.05f, g, g * 0.92f);
            float snow = Fbm(u, v, 4, 3, 503) + (Value(u, v, 40, 504) - 0.5f) * 0.25f;
            return Color.Lerp(c, new Color(0.84f, 0.86f, 0.91f), Mathf.SmoothStep(0.48f, 0.6f, snow));
        }

        #region Tileable noise
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

        private static int Wrap(int i, int n)
        {
            int r = i % n;
            return r < 0 ? r + n : r;
        }

        private static float Value(float u, float v, int cells, int seed)
        {
            float x = u * cells, y = v * cells;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int x0 = Wrap(xi, cells), x1 = Wrap(xi + 1, cells), y0 = Wrap(yi, cells), y1 = Wrap(yi + 1, cells);
            float a = R01(x0, y0, seed), b = R01(x1, y0, seed), c = R01(x0, y1, seed), d = R01(x1, y1, seed);
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

        private static Color32 C32(Color c) => new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255), 255);
        #endregion
    }
}
