using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The signs' faces. The standard ones (stop, yield, speed limits, one way, plaques, the flag, the historical marker,
    // vending fronts) share one atlas made once a session; what changes from place to place (street names on the blades,
    // route shields, a water tower's name) goes into a small atlas of the site's own, let go of with it. Both are drawn with
    // an alpha-tested material, so shaped signs (octagons, shields, triangles) are plain quads.
    internal static partial class StreetscapePass
    {
        internal static Color32 C32(float r, float g, float b, float a = 1f)
            => new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), (byte)(a * 255f));

        internal static readonly Color32 SignWhite = C32(0.93f, 0.93f, 0.90f);
        internal static readonly Color32 SignBlack = C32(0.07f, 0.07f, 0.07f);
        internal static readonly Color32 SignRed = C32(0.70f, 0.08f, 0.08f);
        internal static readonly Color32 SignGreen = C32(0.02f, 0.38f, 0.20f);
        internal static readonly Color32 SignYellow = C32(0.95f, 0.76f, 0.10f);
        internal static readonly Color32 SignBlue = C32(0.08f, 0.22f, 0.52f);
        internal static readonly Color32 SignGrey = C32(0.55f, 0.56f, 0.57f);
        internal static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        /// <summary>A material for an alpha-tested sign atlas (lit, a little glossy, both faces drawn by separate quads).</summary>
        internal static Material CutoutMaterial(Texture2D tex, string name, bool twoSided = false)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh) { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset, enableInstancing = true };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.3f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            if (twoSided && m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off);
            m.renderQueue = (int)RenderQueue.AlphaTest;
            return m;
        }

        /// <summary>The standard signs, painted once a session into one atlas.</summary>
        internal static class SignArt
        {
            private const int W = 512, H = 512;
            private static Texture2D s_Tex;
            private static Material s_Mat;
            private static readonly Dictionary<string, Rect> s_Cells = new Dictionary<string, Rect>();

            /// <summary>The atlas's surface: fitted UVs from <see cref="Cell"/>.</summary>
            public static Surf Surf
            {
                get
                {
                    Make();
                    return new Surf(s_Mat, Vector2.zero, 0f);
                }
            }

            /// <summary>The UV rectangle of a sign in the atlas.</summary>
            public static Rect Cell(string name)
            {
                Make();
                return s_Cells.TryGetValue(name, out Rect r) ? r : s_Cells["blank"];
            }

            private static void Make()
            {
                if (s_Tex != null && s_Mat != null) return;
                s_Cells.Clear();
                var px = new Color32[W * H];
                var p = new Painter(px, W, H);
                // Row 1 (top): the shaped regulatory signs, 128 px each.
                Add(p, "stop", 0, 384, 128, 128, () => Octagon(p, 0, 384, 128, SignRed, SignWhite, "STOP", 3));
                Add(p, "stopback", 128, 384, 128, 128, () => Octagon(p, 128, 384, 128, SignGrey, SignGrey, null, 0));
                Add(p, "yield", 256, 384, 128, 128, () => Yield(p, 256, 384, 128, false));
                Add(p, "yieldback", 384, 384, 128, 128, () => Yield(p, 384, 384, 128, true));
                // Row 2: do not enter, its back, the flag, the historical marker.
                Add(p, "donotenter", 0, 256, 128, 128, () => DoNotEnter(p, 0, 256, 128, false));
                Add(p, "roundback", 128, 256, 128, 128, () => DoNotEnter(p, 128, 256, 128, true));
                Add(p, "flag", 256, 320, 128, 64, () => Flag(p, 256, 320, 128, 64));
                Add(p, "marker", 384, 320, 128, 64, () => Marker(p, 384, 320, 128, 64));
                Add(p, "diamondback", 256, 256, 64, 64, () => Diamond(p, 256, 256, 64, SignGrey, null));
                Add(p, "deadend", 320, 256, 64, 64, () => Diamond(p, 320, 256, 64, SignYellow, "DEAD|END"));
                Add(p, "deer", 384, 256, 64, 64, () => Diamond(p, 384, 256, 64, SignYellow, "DEER|XING"));
                Add(p, "stopahead", 448, 256, 64, 64, () => Diamond(p, 448, 256, 64, SignYellow, "STOP|AHEAD"));
                // Row 3: speed limits (48 x 64), one way arrows (96 x 32) and route plaques (64 x 24).
                int[] limits = { 25, 35, 45, 55, 65 };
                for (int i = 0; i < limits.Length; i++)
                {
                    int x = i * 48, lim = limits[i];
                    Add(p, "speed" + lim, x, 192, 48, 64, () => Speed(p, x, 192, lim));
                }
                Add(p, "oneway_r", 240, 224, 96, 32, () => OneWay(p, 240, 224, true));
                Add(p, "oneway_l", 240, 192, 96, 32, () => OneWay(p, 240, 192, false));
                string[] plaques = { "NORTH", "SOUTH", "EAST", "WEST" };
                for (int i = 0; i < plaques.Length; i++)
                {
                    int x = 336 + (i % 2) * 64, y = 232 - (i / 2) * 24;
                    string text = plaques[i];
                    Add(p, text, x, y, 64, 24, () => Plaque(p, x, y, 64, 24, SignWhite, SignBlack, text, 1));
                }
                Add(p, "JCT", 464, 232, 48, 24, () => Plaque(p, 464, 232, 48, 24, SignWhite, SignBlack, "JCT", 1));
                Add(p, "blank", 464, 208, 48, 24, () => Plaque(p, 464, 208, 48, 24, SignGrey, SignGrey, null, 1));
                // Row 4: the payphone's sign, the bus stop, vending fronts, newspaper box faces.
                Add(p, "telephone", 0, 160, 128, 32, () => Plaque(p, 0, 160, 128, 32, SignBlue, SignWhite, "TELEPHONE", 2));
                Add(p, "busstop", 128, 160, 64, 32, () => Plaque(p, 128, 160, 64, 32, SignBlue, SignWhite, "BUS|STOP", 1));
                Add(p, "schoolbus", 192, 160, 64, 32, () => Plaque(p, 192, 160, 64, 32, SignYellow, SignBlack, "SCHOOL|BUS STOP", 1));
                Add(p, "nopark", 352, 160, 32, 48, () => Plaque(p, 352, 160, 32, 48, SignWhite, SignRed, "NO|PARK|ING", 1));
                Add(p, "allway", 288, 168, 64, 24, () => Plaque(p, 288, 168, 64, 24, SignRed, SignWhite, "ALL WAY", 1));
                Add(p, "soda_red", 0, 32, 64, 128, () => Vending(p, 0, 32, C32(0.72f, 0.07f, 0.07f), "ICE|COLD|SODA"));
                Add(p, "soda_blue", 64, 32, 64, 128, () => Vending(p, 64, 32, C32(0.10f, 0.22f, 0.60f), "DRINK|POP"));
                Add(p, "soda_green", 128, 32, 64, 128, () => Vending(p, 128, 32, C32(0.10f, 0.42f, 0.18f), "LEMON|LIME"));
                Add(p, "paper_cj", 192, 96, 48, 64, () => NewsBox(p, 192, 96, SignBlue, "COURIER|JOURNAL"));
                Add(p, "paper_hl", 240, 96, 48, 64, () => NewsBox(p, 240, 96, SignWhite, "HERALD|LEADER"));
                Add(p, "paper_us", 288, 96, 48, 64, () => NewsBox(p, 288, 96, C32(0.15f, 0.40f, 0.75f), "USA|TODAY"));
                Add(p, "paper_local", 336, 96, 48, 64, () => NewsBox(p, 336, 96, C32(0.85f, 0.75f, 0.15f), "LOCAL|NEWS"));
                Add(p, "usps", 384, 96, 64, 64, () => Plaque(p, 384, 96, 64, 64, SignBlue, SignWhite, "U.S.|MAIL", 1));
                Add(p, "tag", 448, 96, 64, 32, () => Plaque(p, 448, 96, 64, 32, SignWhite, SignBlack, "TAG", 1));
                Add(p, "ice", 448, 128, 64, 32, () => Plaque(p, 448, 128, 64, 32, SignBlue, SignWhite, "ICE", 2));

                s_Tex = new Texture2D(W, H, TextureFormat.RGBA32, true)
                {
                    name = "StreetSigns", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, anisoLevel = 4,
                    hideFlags = HideFlags.DontUnloadUnusedAsset,
                };
                s_Tex.SetPixels32(px);
                s_Tex.Apply(true, false);
                s_Mat = CutoutMaterial(s_Tex, "StreetSigns");
            }

            private static void Add(Painter p, string name, int x, int y, int w, int h, System.Action paint)
            {
                paint();
                // Half a texel in, so bilinear filtering never reaches a neighbour.
                s_Cells[name] = new Rect((x + 0.5f) / W, (y + 0.5f) / H, (w - 1f) / W, (h - 1f) / H);
            }

            private static void Octagon(Painter p, int x0, int y0, int size, Color32 fill, Color32 rim, string text, int scale)
            {
                float c = size * 0.5f, lim = size * 0.5f - 1f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - c), dy = Mathf.Abs(y + 0.5f - c);
                        // An octagon: inside both the square and the square turned 45 degrees.
                        float d = Mathf.Max(Mathf.Max(dx, dy), (dx + dy) * 0.7071f);
                        if (d > lim) continue;
                        bool band = d > lim - size * 0.065f && d < lim - size * 0.025f;
                        p.Set(x0 + x, y0 + y, band ? rim : fill);
                    }
                if (text != null) p.TextCentred(text, x0 + size / 2, y0 + size / 2 + DecorFont.GlyphH * scale / 2, scale, rim);
            }

            private static void Yield(Painter p, int x0, int y0, int size, bool back)
            {
                // A triangle pointing down: the top edge along the cell's top.
                float top = size - 2f, bottom = 6f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float fy = y + 0.5f;
                        if (fy < bottom || fy > top) continue;
                        float half = (fy - bottom) / (top - bottom) * (size * 0.5f - 2f);
                        float dx = Mathf.Abs(x + 0.5f - size * 0.5f);
                        if (dx > half) continue;
                        if (back) { p.Set(x0 + x, y0 + y, SignGrey); continue; }
                        // A red band round a white middle.
                        float inner = half - size * 0.16f;
                        bool white = dx < inner && fy < top - size * 0.14f && fy > bottom + size * 0.3f;
                        p.Set(x0 + x, y0 + y, white ? SignWhite : SignRed);
                    }
                if (!back) p.TextCentred("YIELD", x0 + size / 2, y0 + (int)(size * 0.70f), 1, SignRed);
            }

            private static void DoNotEnter(Painter p, int x0, int y0, int size, bool back)
            {
                float c = size * 0.5f, r = size * 0.5f - 1f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - c, dy = y + 0.5f - c;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > r) continue;
                        if (back) { p.Set(x0 + x, y0 + y, SignGrey); continue; }
                        bool bar = Mathf.Abs(dy) < size * 0.08f && Mathf.Abs(dx) < r * 0.75f;
                        p.Set(x0 + x, y0 + y, bar || d > r - 2f ? SignWhite : SignRed);
                    }
                if (!back)
                {
                    p.TextCentred("DO NOT", x0 + size / 2, y0 + (int)(size * 0.80f), 1, SignWhite);
                    p.TextCentred("ENTER", x0 + size / 2, y0 + (int)(size * 0.30f), 1, SignWhite);
                }
            }

            private static void Flag(Painter p, int x0, int y0, int w, int h)
            {
                Color32 red = C32(0.68f, 0.10f, 0.14f), blue = C32(0.12f, 0.16f, 0.38f);
                for (int y = 0; y < h; y++)
                {
                    int stripe = (h - 1 - y) * 13 / h;
                    for (int x = 0; x < w; x++)
                    {
                        bool canton = x < w * 0.4f && (h - 1 - y) < h * 7 / 13;
                        Color32 c = canton ? blue : (stripe % 2 == 0 ? red : SignWhite);
                        if (canton && (x % 5 == 2) && ((h - 1 - y) % 4 == 1)) c = SignWhite;
                        p.Set(x0 + x, y0 + y, c);
                    }
                }
            }

            private static void Marker(Painter p, int x0, int y0, int w, int h)
            {
                Color32 green = C32(0.06f, 0.24f, 0.14f), gold = C32(0.82f, 0.68f, 0.28f);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        bool rim = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
                        p.Set(x0 + x, y0 + y, rim ? gold : green);
                    }
                p.TextCentred("KENTUCKY", x0 + w / 2, y0 + h - 4, 1, gold);
                // The marker's text, too small to read from the road: rows of gold dashes.
                for (int row = 0; row < 5; row++)
                {
                    int y = y0 + h - 18 - row * 8;
                    for (int x = x0 + 8; x < x0 + w - 8; x++)
                        if (((x * 7 + row * 13) % 11) > 2) { p.Set(x, y, gold); p.Set(x, y - 1, gold); }
                }
            }

            private static void Diamond(Painter p, int x0, int y0, int size, Color32 fill, string text)
            {
                float c = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Abs(x + 0.5f - c) + Mathf.Abs(y + 0.5f - c);
                        if (d > c - 0.5f) continue;
                        p.Set(x0 + x, y0 + y, d > c - 3f && text != null ? SignBlack : fill);
                    }
                if (text == null) return;
                string[] lines = text.Split('|');
                int top = y0 + size / 2 + lines.Length * 5;
                for (int i = 0; i < lines.Length; i++) p.TextCentred(lines[i], x0 + size / 2, top - i * 10, 1, SignBlack);
            }

            private static void Speed(Painter p, int x0, int y0, int limit)
            {
                const int w = 48, h = 64;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        bool rim = x == 1 || y == 1 || x == w - 2 || y == h - 2;
                        bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                        p.Set(x0 + x, y0 + y, rim ? SignBlack : edge ? SignGrey : SignWhite);
                    }
                p.TextCentred("SPEED", x0 + w / 2, y0 + h - 5, 1, SignBlack);
                p.TextCentred("LIMIT", x0 + w / 2, y0 + h - 15, 1, SignBlack);
                p.TextCentred(limit.ToString(), x0 + w / 2, y0 + 30, 3, SignBlack);
            }

            private static void OneWay(Painter p, int x0, int y0, bool right)
            {
                const int w = 96, h = 32;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                        p.Set(x0 + x, y0 + y, edge ? SignWhite : SignBlack);
                    }
                // A white arrow along the middle with ONE WAY on it.
                for (int y = 9; y < 23; y++)
                    for (int x = 6; x < 80; x++)
                    {
                        int ax = right ? x : w - 1 - x;
                        p.Set(x0 + ax, y0 + y, SignWhite);
                    }
                for (int y = 4; y < 28; y++)
                {
                    int half = 12 - Mathf.Abs(y - 16);
                    for (int k = 0; k < Mathf.Max(0, half + 1); k++)
                    {
                        int x = 78 + k;
                        int ax = right ? x : w - 1 - x;
                        p.Set(x0 + ax, y0 + y, SignWhite);
                    }
                }
                p.TextCentred("ONE WAY", x0 + (right ? 42 : w - 42), y0 + 19, 1, SignBlack);
            }

            private static void Plaque(Painter p, int x0, int y0, int w, int h, Color32 fill, Color32 ink, string text, int scale)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        bool rim = x == 1 || y == 1 || x == w - 2 || y == h - 2;
                        bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                        p.Set(x0 + x, y0 + y, edge ? SignGrey : rim ? ink : fill);
                    }
                if (text == null) return;
                string[] lines = text.Split('|');
                int lineH = DecorFont.GlyphH * scale + 2;
                int top = y0 + h / 2 + lines.Length * lineH / 2 - 1;
                for (int i = 0; i < lines.Length; i++) p.TextCentred(lines[i], x0 + w / 2, top - i * lineH, scale, ink);
            }

            private static void Vending(Painter p, int x0, int y0, Color32 body, string text)
            {
                const int w = 64, h = 128;
                Color32 light = C32(0.95f, 0.92f, 0.80f);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color32 c = body;
                        // The lit panel with the brand down its left side; the coin slot and buttons on the right.
                        if (x >= 4 && x < 40 && y >= 40 && y < 120) c = light;
                        else if (x >= 46 && x < 58 && y >= 70 && y < 116 && (y % 8) < 5) c = C32(0.85f, 0.85f, 0.82f);
                        else if (x >= 4 && x < 60 && y >= 8 && y < 24) c = SignBlack;
                        p.Set(x0 + x, y0 + y, c);
                    }
                string[] lines = text.Split('|');
                for (int i = 0; i < lines.Length; i++) p.TextCentred(lines[i], x0 + 22, y0 + 108 - i * 12, 1, body);
            }

            private static void NewsBox(Painter p, int x0, int y0, Color32 body, string text)
            {
                const int w = 48, h = 64;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color32 c = body;
                        if (x >= 6 && x < 42 && y >= 22 && y < 46) c = C32(0.55f, 0.60f, 0.62f);   // the window, a paper behind
                        if (x >= 10 && x < 38 && y >= 26 && y < 42) c = C32(0.80f, 0.78f, 0.72f);
                        p.Set(x0 + x, y0 + y, c);
                    }
                Color32 ink = body.r + body.g + body.b > 500 ? SignBlack : SignWhite;
                string[] lines = text.Split('|');
                for (int i = 0; i < lines.Length; i++) p.TextCentred(lines[i], x0 + w / 2, y0 + h - 4 - i * 9, 1, ink);
            }
        }

        /// <summary>Paints into an atlas's pixels (row 0 at the bottom).</summary>
        internal readonly struct Painter
        {
            private readonly Color32[] _px;
            private readonly int _w, _h;

            public Painter(Color32[] px, int w, int h)
            {
                _px = px;
                _w = w;
                _h = h;
            }

            public void Set(int x, int y, Color32 c)
            {
                if (x < 0 || y < 0 || x >= _w || y >= _h) return;
                _px[y * _w + x] = c;
            }

            public void Fill(int x0, int y0, int w, int h, Color32 c)
            {
                for (int y = y0; y < y0 + h; y++)
                    for (int x = x0; x < x0 + w; x++) Set(x, y, c);
            }

            /// <summary>Text centred on <paramref name="cx"/> with its top at <paramref name="top"/>.</summary>
            public void TextCentred(string text, int cx, int top, int scale, Color32 ink)
                => DecorFont.Draw(_px, _w, _h, text, cx - DecorFont.Width(text, scale) / 2, top, scale, ink);
        }

        /// <summary>
        /// A site's own sign faces: the street names on its blades, its route shields, a water tower's painted name. Laid out
        /// on shelves as they're asked for (the UVs are final at once), painted into the pixels as they come, uploaded once at
        /// the end of the pass. The texture and its material go with the site (see <see cref="SiteSignAssets"/>).
        /// </summary>
        internal sealed class NameAtlas
        {
            // Pixel buffers by side, reused from one site to the next (several sites may be built at once, a step at a time).
            private static readonly Dictionary<int, Stack<Color32[]>> s_Buffers = new Dictionary<int, Stack<Color32[]>>();

            private readonly int _w, _h;
            private Color32[] _px;
            private readonly Painter _p;
            private readonly Texture2D _tex;
            private readonly Material _mat;
            private readonly Dictionary<string, Rect> _done = new Dictionary<string, Rect>();
            private int _x, _y, _shelf;
            private bool _dirty;

            public NameAtlas(Site site)
            {
                _w = _h = site.Chunked ? 256 : 512;
                _px = TakeBuffer(_w);
                _p = new Painter(_px, _w, _h);
                _tex = new Texture2D(_w, _h, TextureFormat.RGBA32, true)
                {
                    name = "StreetNames", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, anisoLevel = 4,
                };
                _mat = CutoutMaterial(_tex, "StreetNames");
                _mat.hideFlags = HideFlags.None;
                var holder = new GameObject("StreetNames");
                holder.transform.SetParent(site.Town.World, false);
                SiteSignAssets assets = holder.AddComponent<SiteSignAssets>();
                assets.Texture = _tex;
                assets.Material = _mat;
            }

            public Surf Surf => new Surf(_mat, Vector2.zero, 0f);

            /// <summary>A green street-name blade with <paramref name="name"/> (abbreviated, in capitals); its width over
            /// its height in <paramref name="aspect"/>. False when the atlas is full.</summary>
            public bool Blade(string name, out Rect uv, out float aspect)
            {
                string text = BladeText(name);
                int scale = DecorFont.Width(text, 2) + 14 <= _w ? 2 : 1;
                int w = Mathf.Min(_w, DecorFont.Width(text, scale) + 14), h = 20;
                aspect = w / (float)h;
                return Cell("blade:" + text, w, h, (x, y) =>
                {
                    _p.Fill(x, y, w, h, SignGreen);
                    for (int i = 0; i < w; i++) { _p.Set(x + i, y + 1, SignWhite); _p.Set(x + i, y + h - 2, SignWhite); }
                    for (int j = 1; j < h - 1; j++) { _p.Set(x + 1, y + j, SignWhite); _p.Set(x + w - 2, y + j, SignWhite); }
                    _p.TextCentred(text, x + w / 2, y + h / 2 + DecorFont.GlyphH * scale / 2, scale, SignWhite);
                }, out uv);
            }

            /// <summary>A route shield for one route ("US 31E", "KY 245", "I 64"); its back's cell in
            /// <paramref name="back"/>. False when it isn't a route with a shield, or the atlas is full.</summary>
            public bool Shield(string route, out Rect uv, out Rect back)
            {
                back = default;
                uv = default;
                if (!ParseRoute(route, out string net, out string number)) return false;
                const int s = 48;
                bool ok = Cell("shield:" + net + number, s, s, (x, y) => PaintShield(x, y, s, net, number, false), out uv);
                return ok && Cell("shieldback:" + net, s, s, (x, y) => PaintShield(x, y, s, net, number, true), out back);
            }

            /// <summary>Big painted letters on a clear ground (a water tower's name); false when the atlas is full.</summary>
            public bool Lettering(string text, Color32 ink, out Rect uv, out float aspect)
            {
                text = text.ToUpperInvariant();
                if (text.Length > 18) text = text.Substring(0, 18);
                int scale = DecorFont.Width(text, 4) + 8 <= _w ? 4 : 2;
                int w = Mathf.Min(_w, DecorFont.Width(text, scale) + 8), h = DecorFont.GlyphH * scale + 8;
                aspect = w / (float)h;
                return Cell("letters:" + text, w, h, (x, y) => _p.TextCentred(text, x + w / 2, y + h - 5, scale, ink), out uv);
            }

            private bool Cell(string key, int w, int h, System.Action<int, int> paint, out Rect uv)
            {
                if (_done.TryGetValue(key, out uv)) return true;
                if (_px == null) return false;   // finished: nothing more is painted
                if (_x + w > _w)
                {
                    _x = 0;
                    _y += _shelf + 1;
                    _shelf = 0;
                }
                if (_y + h > _h) return false;
                paint(_x, _y);
                uv = new Rect((_x + 0.5f) / _w, (_y + 0.5f) / _h, (w - 1f) / _w, (h - 1f) / _h);
                _done[key] = uv;
                _x += w + 1;
                _shelf = Mathf.Max(_shelf, h);
                _dirty = true;
                return true;
            }

            private void PaintShield(int x0, int y0, int s, string net, string number, bool back)
            {
                float c = s * 0.5f;
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float fx = x + 0.5f, fy = y + 0.5f;
                        float dx = Mathf.Abs(fx - c);
                        Color32 col = Clear;
                        switch (net)
                        {
                            case "US":
                            {
                                // A white shield on a black square.
                                col = SignBlack;
                                float half = c - 5f, low = s * 0.42f;
                                bool inside = fy > 4f && fy < s - 4f && dx < half;
                                if (inside && fy < low)
                                {
                                    float k = (low - fy) / (low - 4f);
                                    inside = dx < half * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k)) + 0.5f;
                                }
                                if (inside && fy > s - 9f && dx < 4f) inside = fy < s - 7f;   // the notch at the top
                                if (inside) col = SignWhite;
                                if (back) col = SignGrey;
                                break;
                            }
                            case "I":
                            {
                                // Blue below, a red band at the top, a white rim.
                                float half = c - 2f, low = s * 0.40f;
                                bool inside = fy > 2f && fy < s - 2f && dx < half;
                                if (inside && fy < low)
                                {
                                    float k = (low - fy) / (low - 2f);
                                    inside = dx < half * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k)) + 0.5f;
                                }
                                if (inside && fy > s - 6f) inside = dx < half - (fy - (s - 6f)) * 1.2f + (dx < 4f ? -2f : 0f);
                                if (!inside) break;
                                bool rim = dx > half - 2.5f || fy < 4f || fy > s - 5f;
                                col = back ? SignGrey : rim ? SignWhite : fy > s * 0.74f ? SignRed : SignBlue;
                                break;
                            }
                            default:
                            {
                                // Kentucky's route marker: black numbers on a white rectangle with a black rim.
                                bool inside = fx > 3f && fx < s - 3f && fy > 7f && fy < s - 7f;
                                if (!inside) break;
                                bool rim = fx < 5f || fx > s - 5f || fy < 9f || fy > s - 9f;
                                col = back ? SignGrey : rim ? SignBlack : SignWhite;
                                break;
                            }
                        }
                        _p.Set(x0 + x, y0 + y, col);
                    }
                if (back) return;
                Color32 ink = net == "I" ? SignWhite : SignBlack;
                int scale = number.Length <= 3 ? 2 : 1;
                int top = y0 + s / 2 + DecorFont.GlyphH * scale / 2 - (net == "I" ? 3 : 0);
                _p.TextCentred(number, x0 + s / 2, top, scale, ink);
                if (net == "I") _p.TextCentred("I", x0 + s / 2, y0 + s - 6, 1, SignWhite);
            }

            /// <summary>Uploads what was painted (the texture keeps no copy of its own) and lets go of the pixels.</summary>
            public void Finish()
            {
                if (_px == null) return;
                if (_dirty)
                {
                    _tex.SetPixels32(_px);
                    _tex.Apply(true, true);
                    _dirty = false;
                }
                if (!s_Buffers.TryGetValue(_w, out Stack<Color32[]> free)) s_Buffers[_w] = free = new Stack<Color32[]>(2);
                if (free.Count < 4) free.Push(_px);
                _px = null;
            }

            private static Color32[] TakeBuffer(int side)
            {
                if (s_Buffers.TryGetValue(side, out Stack<Color32[]> free) && free.Count > 0)
                {
                    Color32[] px = free.Pop();
                    System.Array.Clear(px, 0, px.Length);
                    return px;
                }
                return new Color32[side * side];
            }
        }

        /// <summary>One route of a street's ref split into its network ("US", "KY", "I") and number.</summary>
        internal static bool ParseRoute(string route, out string net, out string number)
        {
            net = number = null;
            if (string.IsNullOrEmpty(route)) return false;
            string r = route.Trim().ToUpperInvariant().Replace("-", " ");
            int space = r.IndexOf(' ');
            if (space <= 0)
            {
                // "US41", "KY3548": letters then digits.
                int i = 0;
                while (i < r.Length && char.IsLetter(r[i])) i++;
                if (i == 0 || i >= r.Length) return false;
                net = r.Substring(0, i);
                number = r.Substring(i);
            }
            else
            {
                net = r.Substring(0, space);
                number = r.Substring(space + 1).Trim();
            }
            if (net != "US" && net != "KY" && net != "I") return false;
            // "31E Truck", "60 Business", "45 Bypass": the number alone on the shield.
            int end = 0;
            while (end < number.Length && (char.IsLetterOrDigit(number[end])) && end < 4) end++;
            number = number.Substring(0, end);
            return number.Length > 0 && char.IsDigit(number[0]);
        }

        /// <summary>The routes in a street's ref ("US 31E;US 62" gives both).</summary>
        internal static List<string> Routes(string refs)
        {
            var list = new List<string>(3);
            if (string.IsNullOrEmpty(refs)) return list;
            foreach (string part in refs.Split(';'))
            {
                string t = part.Trim();
                if (t.Length > 0 && !list.Contains(t)) list.Add(t);
            }
            return list;
        }

        /// <summary>A street name as a blade shows it: capitals, the usual abbreviations, a route's number alone.</summary>
        internal static string BladeText(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string n = name.Split(';')[0].Trim().ToUpperInvariant();
            var sb = new StringBuilder(n.Length);
            foreach (string word in n.Split(' '))
            {
                if (word.Length == 0) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(Abbreviate(word));
            }
            string s = sb.ToString();
            return s.Length > 22 ? s.Substring(0, 22) : s;
        }

        private static string Abbreviate(string w)
        {
            switch (w)
            {
                case "NORTH": return "N";
                case "SOUTH": return "S";
                case "EAST": return "E";
                case "WEST": return "W";
                case "STREET": return "ST";
                case "AVENUE": return "AVE";
                case "ROAD": return "RD";
                case "DRIVE": return "DR";
                case "LANE": return "LN";
                case "COURT": return "CT";
                case "BOULEVARD": return "BLVD";
                case "CIRCLE": return "CIR";
                case "PLACE": return "PL";
                case "PARKWAY": return "PKWY";
                case "HIGHWAY": return "HWY";
                case "ALLEY": return "ALY";
                case "TRAIL": return "TRL";
                case "TERRACE": return "TER";
                case "SQUARE": return "SQ";
                case "MOUNT": return "MT";
                case "SAINT": return "ST";
                default: return w;
            }
        }
    }

    /// <summary>Lets go of a site's sign atlas with the site (a streamed piece's root is destroyed when it unloads).</summary>
    [ExecuteAlways]
    internal sealed class SiteSignAssets : MonoBehaviour
    {
        public Texture2D Texture;
        public Material Material;

        private void OnDestroy()
        {
            if (Material != null)
            {
                if (Application.isPlaying) Destroy(Material);
                else DestroyImmediate(Material);
            }
            if (Texture != null)
            {
                if (Application.isPlaying) Destroy(Texture);
                else DestroyImmediate(Texture);
            }
        }
    }
}
