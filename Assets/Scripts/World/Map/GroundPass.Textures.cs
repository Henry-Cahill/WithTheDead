using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The ground's own textures, painted once a session and shared (static caches, like DecorTextures'): a cool snow
    // that reads white under the map's warm daylight, and the land-use surfaces. The textures with things lying on
    // snow (stubble, tufts, reeds, footprints) are painted over the very snow pixels the base ground shows, with the
    // same 9 m repeat, so a band of stubble or a meadow meets the plain snow without an edge. Every repeat divides
    // RaidMesh.PlanarPeriod (252 m), so streamed chunks meet seamlessly.
    internal static partial class GroundPass
    {
        private static readonly Dictionary<string, Texture2D> s_Tex = new Dictionary<string, Texture2D>();
        private static Color[] s_SnowPx;
        private const int SnowSize = 512;

        #region Surfaces
        private static Surf IceSurf => DecorMaterials.Planar(Tex("ice", PaintIce), 21f, 0.62f);
        private static Surf WaterSurf => DecorMaterials.Planar(Tex("water", PaintWater), 9f, 0.85f);
        private static Surf ForestFloorSurf => DecorMaterials.Planar(Tex("forest", PaintForestFloor), 9f, 0.12f);
        private static Surf MeadowSurf => DecorMaterials.Planar(Tex("meadow", PaintMeadow), 9f, 0.22f);
        private static Surf ScrubSurf => DecorMaterials.Planar(Tex("scrub", PaintScrub), 9f, 0.2f);
        private static Surf ReedsSurf => DecorMaterials.Planar(Tex("reeds", PaintReeds), 9f, 0.25f);
        private static Surf StubbleSurf => DecorMaterials.Planar(Tex("stubble", PaintStubble), 9f, 0.24f);
        private static Surf FurrowSurf => DecorMaterials.Planar(Tex("furrow", PaintFurrow), 9f, 0.24f);
        private static Surf TroddenSurf => DecorMaterials.Planar(Tex("trodden", PaintTrodden), 9f, 0.3f);
        private static Surf FarmyardSurf => DecorMaterials.Planar(Tex("farmyard", PaintFarmyard), 9f, 0.2f);
        private static Surf LotSurf => DecorMaterials.Planar(Tex("lot", PaintLot), 12f, 0.3f);
        private static Surf PaversSurf => DecorMaterials.Planar(Tex("pavers", PaintPavers), 6f, 0.2f);
        private static Surf ForecourtSurf => DecorMaterials.Planar(Tex("forecourt", PaintForecourt), 6f, 0.22f);
        private static Surf MudSurf => DecorMaterials.Planar(Tex("mud", PaintMud), 9f, 0.25f);
        private static Surf YardSurf => DecorMaterials.Planar(Tex("yard", PaintYard), 9f, 0.15f);
        private static Surf RockSurf => DecorMaterials.Planar(Tex("rock", PaintRock), 9f, 0.15f);
        private static Surf BankSurf => DecorMaterials.Planar(Tex("bank", PaintBank), 9f, 0.15f);

        /// <summary>Paints the textures the ground needs nearly everywhere, so the first streamed chunk with a pond or a
        /// wood doesn't pay for them mid-game.</summary>
        private static void Warm()
        {
            if (s_Tex.Count > 0) return;
            SnowTex();
            Tex("forest", PaintForestFloor);
            Tex("lot", PaintLot);
            Tex("ice", PaintIce);
            Tex("stubble", PaintStubble);
            Tex("meadow", PaintMeadow);
        }

        private static Texture2D SnowTex() => Tex("snow", PaintSnow);

        private static Texture2D Tex(string key, System.Func<int, Color[]> paint)
        {
            if (s_Tex.TryGetValue(key, out Texture2D t) && t != null) return t;
            int size = key == "snow" || key == "ice" || key == "lot" || key == "pavers" || key == "forest" ? 512
                : key == "water" ? 128 : key == "mud" || key == "yard" || key == "rock" || key == "bank" || key == "forecourt" ? 256
                : SnowSize;
            Color[] px = paint(size);
            var c32 = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++) c32[i] = px[i];
            t = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Raid_ground_" + key,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            t.SetPixels32(c32);
            t.Apply(true, true);
            s_Tex[key] = t;
            return t;
        }
        #endregion

        #region Noise (tileable over the texture)
        private static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        private static float Rand(int x, int y, int seed) => (Hash(x, y, seed) & 0xFFFFFF) / 16777215f;

        private static int Wrap(int i, int n)
        {
            int r = i % n;
            return r < 0 ? r + n : r;
        }

        /// <summary>Value noise over the unit square, <paramref name="cx"/> by <paramref name="cy"/> cells, repeating
        /// every 1 in u and v (and in u + v, u - v: feed it those for diagonal grain).</summary>
        private static float Noise(float u, float v, int cx, int cy, int seed)
        {
            float x = u * cx, y = v * cy;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int x0 = Wrap(xi, cx), x1 = Wrap(xi + 1, cx), y0 = Wrap(yi, cy), y1 = Wrap(yi + 1, cy);
            float a = Rand(x0, y0, seed), b = Rand(x1, y0, seed), c = Rand(x0, y1, seed), d = Rand(x1, y1, seed);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        private static float Fbm(float u, float v, int cells, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Noise(u, v, cells << o, cells << o, seed + o * 31) * amp;
                norm += amp;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);
        #endregion

        #region Brushes (wrapping round the texture's edges)
        private static void Dab(Color[] px, int s, float x, float y, float radius, Color c, float alpha)
        {
            int r = Mathf.CeilToInt(radius + 0.5f);
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            for (int j = -r; j <= r; j++)
                for (int i = -r; i <= r; i++)
                {
                    float dx = cx + i + 0.5f - x, dy = cy + j + 0.5f - y;
                    float k = Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy)) * alpha;
                    if (k <= 0f) continue;
                    int idx = Wrap(cy + j, s) * s + Wrap(cx + i, s);
                    px[idx] = Color.Lerp(px[idx], c, k);
                }
        }

        private static void Stroke(Color[] px, int s, float x, float y, float angle, float length, float width, Color c, float alpha)
        {
            float dx = Mathf.Cos(angle), dy = Mathf.Sin(angle);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length * 1.5f));
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                Dab(px, s, x + dx * length * t, y + dy * length * t, width * 0.5f * (1f - 0.4f * t), c, alpha);
            }
        }

        private static void Ellipse(Color[] px, int s, float x, float y, float rx, float ry, float angle, Color c, float alpha, float soft)
        {
            float ca = Mathf.Cos(angle), sa = Mathf.Sin(angle);
            int r = Mathf.CeilToInt(Mathf.Max(rx, ry) + 1f);
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            for (int j = -r; j <= r; j++)
                for (int i = -r; i <= r; i++)
                {
                    float dx = cx + i + 0.5f - x, dy = cy + j + 0.5f - y;
                    float a = (dx * ca + dy * sa) / rx, b = (-dx * sa + dy * ca) / ry;
                    float d = Mathf.Sqrt(a * a + b * b);
                    float k = (1f - Smooth(1f - soft, 1f, d)) * alpha;
                    if (k <= 0f) continue;
                    int idx = Wrap(cy + j, s) * s + Wrap(cx + i, s);
                    px[idx] = Color.Lerp(px[idx], c, k);
                }
        }

        private static Color Pick(System.Random rng, Color a, Color b) => Color.Lerp(a, b, (float)rng.NextDouble());

        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        #endregion

        #region Snow and what lies on it
        /// <summary>Wind-packed snow: soft undulations, faint diagonal ripples of crust (the winter wind is from the
        /// north-west), a fine grain and the odd glint. Cool enough to read white under the warm sun.</summary>
        private static Color[] PaintSnow(int s)
        {
            if (s_SnowPx != null && s_SnowPx.Length == s * s) return (Color[])s_SnowPx.Clone();
            var px = new Color[s * s];
            var hollow = new Color(0.70f, 0.79f, 0.93f);
            var crest = new Color(0.87f, 0.93f, 1.00f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    float soft = Fbm(u, v, 3, 3, 101);
                    float ripple = Noise(u + v, u - v, 11, 2, 102) * 0.6f + Noise(u + v, u - v, 23, 3, 103) * 0.4f;
                    float grain = Noise(u, v, 64, 64, 104);
                    float t = 0.52f * soft + 0.3f * ripple + 0.18f * grain;
                    Color c = Color.Lerp(hollow, crest, Smooth(0.28f, 0.72f, t));
                    c = Scale(c, 1f + (Rand(x, y, 105) - 0.5f) * 0.035f);
                    float glint = Rand(x, y, 106);
                    if (glint > 0.9975f) c = Color.Lerp(c, Color.white, 0.65f);
                    else if (glint < 0.002f) c = Scale(c, 0.9f);
                    px[y * s + x] = c;
                }
            s_SnowPx = px;
            return (Color[])px.Clone();
        }

        // The base snow, or its copy at another size (the derived textures all share the snow's own size).
        private static Color[] SnowCopy(int s) => PaintSnow(s);

        /// <summary>Corn stubble: snapped tan stalks and the odd husk poking through the snow (on the rows' bands).</summary>
        private static Color[] PaintStubble(int s)
        {
            Color[] px = SnowCopy(s);
            var rng = new System.Random(201);
            var shadow = new Color(0.52f, 0.60f, 0.74f);
            for (int i = 0; i < s * s / 100; i++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s), a = R(rng, 0f, Mathf.PI * 2f), len = R(rng, 2.5f, 6.5f);
                Stroke(px, s, x + 1.2f, y - 1.2f, a, len, 1.6f, shadow, 0.35f);
                Stroke(px, s, x, y, a, len, R(rng, 1.2f, 2.2f), Pick(rng, new Color(0.60f, 0.49f, 0.32f), new Color(0.42f, 0.34f, 0.23f)), 0.95f);
            }
            for (int i = 0; i < s * s / 900; i++)
                Ellipse(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 1.5f, 3f), R(rng, 0.8f, 1.5f), R(rng, 0f, 3.2f),
                    new Color(0.74f, 0.66f, 0.49f), 0.8f, 0.4f);
            return px;
        }

        /// <summary>Soybean or tobacco ground: specks and clods of dark soil and a few short grey stems in the snow.</summary>
        private static Color[] PaintFurrow(int s)
        {
            Color[] px = SnowCopy(s);
            var rng = new System.Random(211);
            for (int i = 0; i < s * s / 120; i++)
                Ellipse(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 1f, 2.6f), R(rng, 0.8f, 1.8f), R(rng, 0f, 3.2f),
                    Pick(rng, new Color(0.36f, 0.30f, 0.26f), new Color(0.27f, 0.23f, 0.21f)), R(rng, 0.6f, 0.95f), 0.5f);
            for (int i = 0; i < s * s / 400; i++)
                Stroke(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 0f, 6.3f), R(rng, 2f, 4f), 1.1f, new Color(0.45f, 0.40f, 0.35f), 0.8f);
            return px;
        }

        // Clumps of dead grass, more of them where a low noise says so (they grow in patches).
        private static void Tufts(Color[] px, int s, System.Random rng, int count, float size, Color a, Color b, int seed)
        {
            var shadow = new Color(0.55f, 0.62f, 0.76f);
            for (int placed = 0, tries = 0; placed < count && tries < count * 4; tries++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s);
                if (rng.NextDouble() > Smooth(0.3f, 0.7f, Fbm(x / s, y / s, 4, 2, seed))) continue;
                placed++;
                int blades = rng.Next(4, 9);
                Ellipse(px, s, x + 1.5f, y - 1.5f, size * 0.9f, size * 0.6f, 0.6f, shadow, 0.3f, 0.8f);
                for (int k = 0; k < blades; k++)
                    Stroke(px, s, x + R(rng, -1f, 1f), y + R(rng, -1f, 1f), R(rng, 0f, 6.3f), R(rng, size * 0.6f, size * 1.4f), 1.1f,
                        Pick(rng, a, b), 0.9f);
            }
        }

        /// <summary>Pasture and hay ground: tufts of dead grass poking through.</summary>
        private static Color[] PaintMeadow(int s)
        {
            Color[] px = SnowCopy(s);
            Tufts(px, s, new System.Random(221), s * s / 220, 5f, new Color(0.72f, 0.63f, 0.43f), new Color(0.55f, 0.47f, 0.32f), 222);
            return px;
        }

        /// <summary>Scrub: twiggy clumps, dark in the snow, and dead grass between.</summary>
        private static Color[] PaintScrub(int s)
        {
            Color[] px = SnowCopy(s);
            var rng = new System.Random(231);
            Tufts(px, s, rng, s * s / 500, 5f, new Color(0.70f, 0.61f, 0.42f), new Color(0.52f, 0.45f, 0.31f), 232);
            for (int i = 0; i < s * s / 1100; i++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s);
                Ellipse(px, s, x + 2f, y - 2f, R(rng, 7f, 12f), R(rng, 5f, 9f), R(rng, 0f, 3.2f), new Color(0.60f, 0.67f, 0.80f), 0.45f, 0.9f);
                int twigs = rng.Next(5, 11);
                for (int k = 0; k < twigs; k++)
                    Stroke(px, s, x + R(rng, -2f, 2f), y + R(rng, -2f, 2f), R(rng, 0f, 6.3f), R(rng, 5f, 13f), 1.3f,
                        Pick(rng, new Color(0.30f, 0.25f, 0.22f), new Color(0.42f, 0.35f, 0.30f)), 0.9f);
            }
            return px;
        }

        /// <summary>A frozen marsh: reed beds standing in the snow, cattail heads, grey pools of ice between.</summary>
        private static Color[] PaintReeds(int s)
        {
            Color[] px = SnowCopy(s);
            var rng = new System.Random(241);
            for (int i = 0; i < s * s / 9000; i++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s), rx = R(rng, 10f, 30f), ry = R(rng, 6f, 18f), a = R(rng, 0f, 3.2f);
                Ellipse(px, s, x, y, rx + 2f, ry + 2f, a, new Color(0.86f, 0.91f, 0.99f), 0.8f, 0.5f);
                Ellipse(px, s, x, y, rx, ry, a, new Color(0.52f, 0.62f, 0.73f), 0.85f, 0.25f);
            }
            for (int placed = 0, tries = 0; placed < s * s / 75 && tries < s * s / 20; tries++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s);
                if (rng.NextDouble() > Smooth(0.25f, 0.65f, Fbm(x / s, y / s, 5, 2, 242))) continue;
                placed++;
                Stroke(px, s, x, y, R(rng, 0f, 6.3f), R(rng, 5f, 13f), R(rng, 1.1f, 1.8f),
                    Pick(rng, new Color(0.70f, 0.61f, 0.42f), new Color(0.50f, 0.42f, 0.28f)), 0.95f);
                if (rng.NextDouble() < 0.04) Dab(px, s, x, y, 1.6f, new Color(0.30f, 0.21f, 0.15f), 1f);
            }
            return px;
        }

        /// <summary>Trampled snow: grey packed patches and trails of boot prints.</summary>
        private static Color[] PaintTrodden(int s)
        {
            Color[] px = SnowCopy(s);
            var rng = new System.Random(251);
            var packed = new Color(0.66f, 0.73f, 0.84f);
            for (int i = 0; i < 60; i++)
                Ellipse(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 12f, 40f), R(rng, 8f, 26f), R(rng, 0f, 3.2f), packed, 0.45f, 0.9f);
            float step = s / 9f * 0.7f;   // a stride, at the texture's 9 m
            for (int trail = 0; trail < 34; trail++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s), heading = R(rng, 0f, 6.3f);
                for (int k = 0; k < 26; k++)
                {
                    heading += R(rng, -0.25f, 0.25f);
                    float side = (k & 1) == 0 ? 1f : -1f;
                    float ox = -Mathf.Sin(heading) * side * step * 0.18f, oy = Mathf.Cos(heading) * side * step * 0.18f;
                    Ellipse(px, s, x + ox, y + oy, step * 0.2f, step * 0.085f, heading, new Color(0.60f, 0.68f, 0.81f), 0.7f, 0.5f);
                    x += Mathf.Cos(heading) * step;
                    y += Mathf.Sin(heading) * step;
                }
            }
            return px;
        }

        /// <summary>A farmyard: trampled snow, frozen mud where the tractors turn, scattered straw.</summary>
        private static Color[] PaintFarmyard(int s)
        {
            Color[] px = PaintTrodden(s);
            var rng = new System.Random(261);
            for (int i = 0; i < 45; i++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s), rx = R(rng, 10f, 32f), ry = R(rng, 6f, 20f), a = R(rng, 0f, 3.2f);
                Ellipse(px, s, x, y, rx, ry, a, new Color(0.40f, 0.33f, 0.27f), 0.8f, 0.6f);
                Ellipse(px, s, x, y, rx * 0.5f, ry * 0.4f, a, new Color(0.30f, 0.25f, 0.21f), 0.6f, 0.8f);
            }
            for (int i = 0; i < s * s / 600; i++)
                Stroke(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 0f, 6.3f), R(rng, 3f, 7f), 1.1f, new Color(0.76f, 0.66f, 0.42f), 0.85f);
            return px;
        }
        #endregion

        #region Woods, ice and water
        /// <summary>The floor of a winter wood: thin snow, shaded, with drifts of oak and maple leaves, pine needles and
        /// twigs showing through.</summary>
        private static Color[] PaintForestFloor(int s)
        {
            Color[] px = SnowCopy(s);
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                px[i] = new Color(c.r * 0.84f, c.g * 0.86f, c.b * 0.88f);
            }
            var rng = new System.Random(301);
            var leaves = new[]
            {
                new Color(0.47f, 0.32f, 0.20f), new Color(0.38f, 0.29f, 0.21f), new Color(0.42f, 0.37f, 0.31f),
                new Color(0.52f, 0.38f, 0.24f), new Color(0.31f, 0.25f, 0.19f),
            };
            for (int placed = 0, tries = 0; placed < s * s / 22 && tries < s * s / 5; tries++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s);
                float patch = Fbm(x / s, y / s, 4, 3, 302);
                if (rng.NextDouble() > Smooth(0.42f, 0.62f, patch)) continue;
                placed++;
                Ellipse(px, s, x, y, R(rng, 1.2f, 2.8f), R(rng, 0.8f, 1.8f), R(rng, 0f, 3.2f), leaves[rng.Next(leaves.Length)],
                    R(rng, 0.55f, 1f), 0.5f);
            }
            for (int i = 0; i < s * s / 260; i++)
                Stroke(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 0f, 6.3f), R(rng, 2f, 5f), 0.9f, new Color(0.29f, 0.24f, 0.18f), 0.7f);
            for (int i = 0; i < s * s / 1400; i++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s), a = R(rng, 0f, 6.3f), len = R(rng, 8f, 26f);
                Stroke(px, s, x, y, a, len, R(rng, 1.2f, 2.2f), new Color(0.27f, 0.23f, 0.20f), 0.9f);
                Stroke(px, s, x + Mathf.Cos(a) * len * 0.5f, y + Mathf.Sin(a) * len * 0.5f, a + R(rng, -0.9f, 0.9f), len * 0.4f, 1f,
                    new Color(0.30f, 0.25f, 0.21f), 0.8f);
            }
            return px;
        }

        /// <summary>Lake ice: grey-blue, darker where it froze clear, white with wind-driven snow in streaks, with a
        /// web of pressure cracks and trapped bubbles (repeats every 21 m).</summary>
        private static Color[] PaintIce(int s)
        {
            var px = new Color[s * s];
            var clear = new Color(0.30f, 0.40f, 0.51f);
            var grey = new Color(0.52f, 0.61f, 0.71f);
            var frost = new Color(0.83f, 0.89f, 0.97f);
            const int cells = 7;
            var seeds = new Vector2[cells * cells];
            for (int j = 0; j < cells; j++)
                for (int i = 0; i < cells; i++)
                    seeds[j * cells + i] = new Vector2(i + 0.15f + 0.7f * Rand(i, j, 311), j + 0.15f + 0.7f * Rand(i, j, 312));
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    Color c = Color.Lerp(clear, grey, Smooth(0.25f, 0.75f, Fbm(u, v, 3, 4, 313)));
                    float streak = Noise(u + v, u - v, 13, 3, 314) * 0.55f + Fbm(u, v, 6, 3, 315) * 0.45f;
                    c = Color.Lerp(c, frost, Smooth(0.5f, 0.72f, streak) * 0.92f);
                    // Pressure cracks: where the two nearest cell seeds are nearly as near (the Voronoi edges).
                    float gx = u * cells, gy = v * cells;
                    int ix = Mathf.FloorToInt(gx), iy = Mathf.FloorToInt(gy);
                    float d1 = float.MaxValue, d2 = float.MaxValue;
                    for (int j = -1; j <= 1; j++)
                        for (int i = -1; i <= 1; i++)
                        {
                            int cx = ix + i, cy = iy + j;
                            Vector2 p = seeds[Wrap(cy, cells) * cells + Wrap(cx, cells)] + new Vector2(cx - Wrap(cx, cells), cy - Wrap(cy, cells));
                            float d = (p.x - gx) * (p.x - gx) + (p.y - gy) * (p.y - gy);
                            if (d < d1)
                            {
                                d2 = d1;
                                d1 = d;
                            }
                            else if (d < d2) d2 = d;
                        }
                    float edge = (Mathf.Sqrt(d2) - Mathf.Sqrt(d1)) * s / cells;   // pixels from the crack
                    float wobble = Noise(u, v, 40, 40, 316);
                    if (edge < 1.3f + wobble) c = Color.Lerp(c, new Color(0.88f, 0.93f, 1f), 0.75f * (1f - edge / (1.3f + wobble)));
                    else if (edge < 2.6f + wobble) c = Scale(c, 0.93f);
                    float r = Rand(x, y, 317);
                    if (r > 0.9965f) c = Color.Lerp(c, Color.white, 0.6f);
                    px[y * s + x] = c;
                }
            return px;
        }

        /// <summary>Open winter water: dark and cold, with faint ripples (glossy, so it catches the sky).</summary>
        private static Color[] PaintWater(int s)
        {
            var px = new Color[s * s];
            var deep = new Color(0.06f, 0.10f, 0.13f);
            var ripple = new Color(0.14f, 0.19f, 0.23f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    float n = Noise(u + v, u - v, 3, 14, 321) * 0.6f + Fbm(u, v, 4, 3, 322) * 0.4f;
                    px[y * s + x] = Color.Lerp(deep, ripple, Smooth(0.45f, 0.8f, n));
                }
            return px;
        }

        /// <summary>A bank in winter: frozen mud, flattened dead grass and reeds, stones, snow lying in patches.</summary>
        private static Color[] PaintBank(int s)
        {
            var px = new Color[s * s];
            var mud = new Color(0.35f, 0.29f, 0.24f);
            var snow = new Color(0.80f, 0.86f, 0.96f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    Color c = Scale(mud, 0.85f + 0.3f * Fbm(u, v, 8, 3, 331));
                    c = Color.Lerp(c, snow, Smooth(0.5f, 0.66f, Fbm(u, v, 4, 4, 332)) * 0.95f);
                    px[y * s + x] = c;
                }
            var rng = new System.Random(333);
            for (int i = 0; i < s * s / 30; i++)
                Stroke(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 0f, 6.3f), R(rng, 3f, 7f), 1.1f,
                    Pick(rng, new Color(0.66f, 0.56f, 0.38f), new Color(0.48f, 0.40f, 0.28f)), 0.85f);
            for (int i = 0; i < s * s / 330; i++)
                Ellipse(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 1.5f, 3.5f), R(rng, 1.2f, 2.8f), R(rng, 0f, 3.2f),
                    Pick(rng, new Color(0.52f, 0.51f, 0.49f), new Color(0.38f, 0.37f, 0.36f)), 1f, 0.4f);
            return px;
        }
        #endregion

        #region Hard ground
        /// <summary>A plowed parking lot: worn asphalt with its aggregate showing, packed snow polished by tyres in
        /// broad patches, salt haze, oil spots and sealed cracks (repeats every 12 m).</summary>
        private static Color[] PaintLot(int s)
        {
            var px = new Color[s * s];
            var packed = new Color(0.63f, 0.68f, 0.76f);
            var salt = new Color(0.44f, 0.45f, 0.47f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    float g = 0.19f + 0.07f * Fbm(u, v, 6, 4, 401) + (Rand(x, y, 402) - 0.5f) * 0.035f;
                    if (Rand(x, y, 403) > 0.975f) g += 0.07f;
                    var c = new Color(g, g, g * 1.06f);
                    c = Color.Lerp(c, salt, Smooth(0.55f, 0.75f, Fbm(u, v, 5, 2, 404)) * 0.45f);
                    float snow = Fbm(u, v, 3, 4, 405);
                    c = Color.Lerp(c, new Color(0.42f, 0.45f, 0.50f), Smooth(0.5f, 0.56f, snow) * 0.6f);
                    c = Color.Lerp(c, packed, Smooth(0.55f, 0.66f, snow) * 0.92f);
                    px[y * s + x] = c;
                }
            var rng = new System.Random(406);
            for (int i = 0; i < 9; i++)
                Ellipse(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 8f, 22f), R(rng, 6f, 14f), R(rng, 0f, 3.2f),
                    new Color(0.09f, 0.09f, 0.10f), R(rng, 0.35f, 0.6f), 0.8f);
            for (int k = 0; k < 6; k++)
            {
                float x = R(rng, 0, s), y = R(rng, 0, s), a = R(rng, 0f, 6.3f);
                bool sealed_ = k < 3;
                for (int i = 0; i < 70; i++)
                {
                    a += R(rng, -0.35f, 0.35f);
                    x += Mathf.Cos(a) * 2f;
                    y += Mathf.Sin(a) * 2f;
                    Dab(px, s, x, y, sealed_ ? 1.6f : 0.7f, sealed_ ? new Color(0.06f, 0.06f, 0.07f) : new Color(0.10f, 0.10f, 0.11f), 0.9f);
                }
            }
            return px;
        }

        /// <summary>Brick pavers in a running bond, snow packed into the joints and lying trodden in patches (6 m).</summary>
        private static Color[] PaintPavers(int s)
        {
            var px = new Color[s * s];
            const int across = 30, rows = 60;   // 20 by 10 cm bricks
            float bw = s / (float)across, bh = s / (float)rows;
            var joint = new Color(0.80f, 0.86f, 0.95f);
            var trodden = new Color(0.72f, 0.78f, 0.88f);
            for (int y = 0; y < s; y++)
            {
                int row = Mathf.FloorToInt((y + 0.5f) / bh);
                float shift = (row & 1) * bw * 0.5f;
                for (int x = 0; x < s; x++)
                {
                    float fx = Mathf.Repeat(x + 0.5f + shift, s);
                    int col = Mathf.FloorToInt(fx / bw);
                    float inX = fx - col * bw, inY = y + 0.5f - row * bh;
                    float k = 0.82f + 0.3f * Rand(Wrap(col, across), Wrap(row, rows), 411);
                    Color c = Scale(Rand(Wrap(col, across), Wrap(row, rows), 412) < 0.15f ? new Color(0.40f, 0.24f, 0.22f) : new Color(0.52f, 0.30f, 0.23f), k);
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    c = Scale(c, 0.92f + 0.12f * Noise(u, v, 48, 48, 413));
                    if (inX < 1.1f || inY < 1.1f) c = joint;
                    c = Color.Lerp(c, trodden, 0.12f + Smooth(0.48f, 0.6f, Fbm(u, v, 3, 4, 414)) * 0.78f);
                    px[y * s + x] = c;
                }
            }
            return px;
        }

        /// <summary>A gas station forecourt: 3 m concrete slabs, cut joints, oil and tyre stains, slush in the joints.</summary>
        private static Color[] PaintForecourt(int s)
        {
            var px = new Color[s * s];
            float slab = s / 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    float g = 0.55f + 0.08f * Fbm(u, v, 5, 3, 421) + (Rand(x, y, 422) - 0.5f) * 0.03f;
                    var c = new Color(g, g * 0.99f, g * 0.96f);
                    float jx = Mathf.Repeat(x + 0.5f, slab), jy = Mathf.Repeat(y + 0.5f, slab);
                    if (jx < 1.2f || jy < 1.2f) c = new Color(0.30f, 0.30f, 0.31f);
                    else if (jx < 3.5f || jy < 3.5f) c = Color.Lerp(c, new Color(0.74f, 0.79f, 0.87f), 0.45f);
                    c = Color.Lerp(c, new Color(0.32f, 0.31f, 0.30f), Smooth(0.6f, 0.75f, Fbm(u, v, 4, 3, 423)) * 0.5f);
                    px[y * s + x] = c;
                }
            var rng = new System.Random(424);
            for (int i = 0; i < 10; i++)
                Ellipse(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 5f, 14f), R(rng, 4f, 9f), R(rng, 0f, 3.2f),
                    new Color(0.16f, 0.15f, 0.15f), R(rng, 0.3f, 0.55f), 0.85f);
            return px;
        }

        // Ground of some colour, mottled, with snow lying where a noise is high.
        private static Color[] Earthy(int s, Color baseColour, float mottle, float snowFrom, int seed)
        {
            var px = new Color[s * s];
            var snow = new Color(0.79f, 0.85f, 0.95f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    Color c = Scale(baseColour, 1f - mottle + 2f * mottle * Fbm(u, v, 6, 3, seed) + (Rand(x, y, seed + 1) - 0.5f) * 0.12f);
                    c = Color.Lerp(c, snow, Smooth(snowFrom, snowFrom + 0.12f, Fbm(u, v, 3, 4, seed + 2)) * 0.95f);
                    px[y * s + x] = c;
                }
            return px;
        }

        /// <summary>A vacant lot or building site: frozen mud and gravel, iced-over puddles, snow in patches.</summary>
        private static Color[] PaintMud(int s)
        {
            Color[] px = Earthy(s, new Color(0.38f, 0.32f, 0.26f), 0.18f, 0.55f, 431);
            var rng = new System.Random(434);
            for (int i = 0; i < s * s / 60; i++)
                Dab(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 0.5f, 1.2f), Pick(rng, new Color(0.55f, 0.53f, 0.50f), new Color(0.30f, 0.28f, 0.26f)), 0.9f);
            for (int i = 0; i < 9; i++)
                Ellipse(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 6f, 16f), R(rng, 4f, 9f), R(rng, 0f, 3.2f), new Color(0.55f, 0.63f, 0.72f), 0.9f, 0.3f);
            return px;
        }

        /// <summary>An industrial yard: grey gravel under dirty, rutted snow, oil and rust stains.</summary>
        private static Color[] PaintYard(int s)
        {
            Color[] px = Earthy(s, new Color(0.45f, 0.44f, 0.43f), 0.15f, 0.5f, 441);
            var rng = new System.Random(444);
            for (int i = 0; i < s * s / 25; i++)
                Dab(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 0.4f, 1f), Pick(rng, new Color(0.58f, 0.57f, 0.55f), new Color(0.30f, 0.30f, 0.30f)), 0.8f);
            for (int i = 0; i < 7; i++)
                Ellipse(px, s, R(rng, 0, s), R(rng, 0, s), R(rng, 5f, 15f), R(rng, 4f, 10f), R(rng, 0f, 3.2f),
                    rng.NextDouble() < 0.5 ? new Color(0.14f, 0.13f, 0.13f) : new Color(0.42f, 0.27f, 0.18f), 0.45f, 0.8f);
            for (int i = 0; i < px.Length; i++) px[i] = Color.Lerp(px[i], new Color(0.50f, 0.50f, 0.50f), 0.12f);
            return px;
        }

        /// <summary>Bare limestone (quarries, outcrops): pale grey beds and joints, snow lying in every hollow.</summary>
        private static Color[] PaintRock(int s)
        {
            var px = new Color[s * s];
            var stone = new Color(0.62f, 0.60f, 0.56f);
            var snow = new Color(0.82f, 0.88f, 0.97f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s, v = (y + 0.5f) / s;
                    float beds = Mathf.Abs(Mathf.Sin((v * 6f + 0.3f * Fbm(u, v, 4, 2, 451)) * Mathf.PI * 2f));
                    Color c = Scale(stone, 0.8f + 0.25f * Fbm(u, v, 8, 3, 452) - (beds < 0.08f ? 0.25f : 0f));
                    c = Color.Lerp(c, snow, Smooth(0.45f, 0.58f, Fbm(u, v, 4, 4, 453)) * 0.95f);
                    px[y * s + x] = c;
                }
            return px;
        }
        #endregion

        #region The snow's tone
        private static Texture2D s_ToneTex;
        private static Material s_ToneMat;

        /// <summary>The see-through material the snow's tone is drawn with: a small table looked up by each vertex's
        /// uv (u: scoured crust 0 .. untouched 0.5 .. fresh drift 1; v: clean 0 .. grimy 1), blended over the ground.</summary>
        private static Material ToneMaterial()
        {
            if (s_ToneMat != null && s_ToneTex != null) return s_ToneMat;
            const int w = 64, h = 16;
            var px = new Color32[w * h];
            var scour = new Color(0.60f, 0.68f, 0.80f);
            var drift = new Color(0.96f, 0.98f, 1.00f);
            var grime = new Color(0.47f, 0.45f, 0.43f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = x / (w - 1f), d = y / (h - 1f);
                    float sc = Smooth(0.46f, 0.06f, t), dr = Smooth(0.54f, 0.94f, t);
                    Color c = t < 0.5f ? scour : drift;
                    float a = 0.34f * sc + 0.30f * dr;
                    c = Color.Lerp(c, grime, d);
                    a = Mathf.Lerp(a, Mathf.Max(a, 0.5f), d);
                    c.a = a;
                    px[y * w + x] = c;
                }
            s_ToneTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Raid_ground_tone",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            s_ToneTex.SetPixels32(px);
            s_ToneTex.Apply(false, true);

            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh) { name = "RaidGroundTone", hideFlags = HideFlags.DontUnloadUnusedAsset, enableInstancing = true };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", s_ToneTex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", s_ToneTex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            SetIf(m, "_Smoothness", 0.25f);
            SetIf(m, "_Metallic", 0f);
            // The render state URP's material inspector writes for Surface = Transparent, Blend = Alpha.
            SetIf(m, "_Surface", 1f);
            SetIf(m, "_Blend", 0f);
            SetIf(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetIf(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetIf(m, "_SrcBlendAlpha", (float)BlendMode.One);
            SetIf(m, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            SetIf(m, "_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent - 50;   // before other see-through things: it's the ground
            s_ToneMat = m;
            return m;
        }

        private static void SetIf(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }
        #endregion
    }
}
