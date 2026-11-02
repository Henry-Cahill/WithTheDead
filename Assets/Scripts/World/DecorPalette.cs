using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// One small point-filtered texture holding every flat colour the raid map uses, one texel per
    /// colour. Flat-coloured geometry points all of its vertices at its colour's texel, so a whole
    /// building's furniture renders with a handful of materials (matte, gloss, metal, glow) instead
    /// of one per colour — far fewer draw calls and shadow-pass draws. Colours are quantised to six
    /// bits a channel so near-identical shades share a texel. Call <see cref="Apply"/> after building.
    /// </summary>
    internal static class DecorPalette
    {
        private const int Size = 128;

        private static Texture2D s_Tex;
        private static Color32[] s_Pixels;
        private static readonly Dictionary<int, int> s_Index = new Dictionary<int, int>(1024);
        private static int s_Count;
        private static bool s_Dirty;

        private static Material s_Matte, s_Gloss, s_Metal, s_Glow, s_Glass, s_Clear, s_Stained;

        private static Texture2D Tex
        {
            get
            {
                if (s_Tex == null)
                {
                    if (s_Pixels == null) s_Pixels = new Color32[Size * Size];
                    s_Tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                    {
                        name = "RaidPalette",
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.DontUnloadUnusedAsset,
                    };
                    s_Dirty = true;
                    s_Matte = s_Gloss = s_Metal = s_Glow = s_Glass = s_Clear = s_Stained = null;
                }
                return s_Tex;
            }
        }

        /// <summary>Uploads any colours added since the last call (cheap; call once a build).</summary>
        public static void Apply()
        {
            Texture2D t = Tex;
            if (!s_Dirty) return;
            t.SetPixels32(s_Pixels);
            t.Apply(false, false);
            s_Dirty = false;
        }

        private static Vector2 UvOf(Color c)
        {
            if (s_Pixels == null) s_Pixels = new Color32[Size * Size];
            int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 63f), 0, 63);
            int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 63f), 0, 63);
            int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 63f), 0, 63);
            int key = (r << 12) | (g << 6) | b;
            if (!s_Index.TryGetValue(key, out int idx))
            {
                // Full (a long trip across the streamed state meets a lot of colours): the nearest colour already in it.
                if (s_Count >= Size * Size) s_Index[key] = idx = Nearest(r, g, b);
                else
                {
                    idx = s_Count++;
                    s_Pixels[idx] = new Color32((byte)(r * 255 / 63), (byte)(g * 255 / 63), (byte)(b * 255 / 63), 255);
                    s_Index[key] = idx;
                    s_Dirty = true;
                }
            }
            int x = idx % Size, y = idx / Size;
            return new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
        }

        private static int Nearest(int r, int g, int b)
        {
            int best = 0, bestD = int.MaxValue;
            for (int i = 0; i < s_Count; i++)
            {
                Color32 p = s_Pixels[i];
                int dr = p.r * 63 / 255 - r, dg = p.g * 63 / 255 - g, db = p.b * 63 / 255 - b;
                int d = dr * dr + dg * dg + db * db;
                if (d >= bestD) continue;
                bestD = d;
                best = i;
            }
            return best;
        }

        private static Material Make(ref Material slot, bool unlit, float smoothness, float metallic, string name)
        {
            Texture2D t = Tex;
            if (slot != null) return slot;
            Shader sh = unlit ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find(unlit ? "Unlit/Texture" : "Standard");
            var m = new Material(sh) { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset, enableInstancing = true };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            slot = m;
            return m;
        }

        /// <summary>Makes a URP Lit material see-through glass, <paramref name="opacity"/> of what's behind it hidden: the render
        /// state URP's inspector writes for Surface Transparent, Blend Alpha; and no shadow, depth or depth-normals pass, so
        /// light and sight go through it. (An alpha-blended surface's emission is weighed by its opacity.)</summary>
        internal static Material SeeThrough(Material m, float opacity)
        {
            if (m == null) return null;
            SetIf(m, "_Surface", 1f);
            SetIf(m, "_Blend", 0f);
            SetIf(m, "_BlendModePreserveSpecular", 0f);
            SetIf(m, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            SetIf(m, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            SetIf(m, "_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            SetIf(m, "_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            SetIf(m, "_ZWrite", 0f);
            if (m.HasProperty("_BaseColor"))
            {
                Color c = m.GetColor("_BaseColor");
                c.a = opacity;
                m.SetColor("_BaseColor", c);
            }
            if (m.HasProperty("_Color"))
            {
                Color c = m.GetColor("_Color");
                c.a = opacity;
                m.SetColor("_Color", c);
            }
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("DepthNormals", false);
            return m;
        }

        private static void SetIf(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }

        private static Material MatteMat => s_Matte != null && s_Tex != null ? s_Matte : Make(ref s_Matte, false, 0.12f, 0f, "RaidPaletteMatte");
        private static Material GlossMat => s_Gloss != null && s_Tex != null ? s_Gloss : Make(ref s_Gloss, false, 0.55f, 0f, "RaidPaletteGloss");
        private static Material MetalMat => s_Metal != null && s_Tex != null ? s_Metal : Make(ref s_Metal, false, 0.5f, 0.65f, "RaidPaletteMetal");
        private static Material GlassMat => s_Glass != null && s_Tex != null ? s_Glass : Make(ref s_Glass, false, 0.9f, 0.1f, "RaidPaletteGlass");
        private static Material GlowMat => s_Glow != null && s_Tex != null ? s_Glow : Make(ref s_Glow, true, 0f, 0f, "RaidPaletteGlow");
        private static Material ClearMat => s_Clear != null && s_Tex != null ? s_Clear : SeeThrough(Make(ref s_Clear, false, 0.95f, 0f, "RaidPaletteWindowGlass"), ClearOpacity);
        private static Material StainedMat => s_Stained != null && s_Tex != null ? s_Stained : SeeThrough(Make(ref s_Stained, false, 0.9f, 0f, "RaidPaletteStainedGlass"), StainedOpacity);

        /// <summary>How much of what's behind it a window's clear glass hides, and a church's coloured glass.</summary>
        private const float ClearOpacity = 0.24f, StainedOpacity = 0.55f;

        /// <summary>The faint grey-blue clear window glass lends what's seen through it.</summary>
        public static readonly Color ClearTint = new Color(0.60f, 0.70f, 0.76f);

        /// <summary>A flat, dull colour (wood, fabric, paint, cardboard, snow).</summary>
        public static Surf Matte(Color c) => new Surf(MatteMat, UvOf(c), 0f);
        /// <summary>A flat colour with a satin sheen (enamel, plastic, lacquer).</summary>
        public static Surf Gloss(Color c) => new Surf(GlossMat, UvOf(c), 0f);
        /// <summary>A flat metallic colour (steel counters, chrome, barrels).</summary>
        public static Surf Metal(Color c) => new Surf(MetalMat, UvOf(c), 0f);
        /// <summary>Glass and screens: dark and mirror-smooth.</summary>
        public static Surf Glass(Color c) => new Surf(GlassMat, UvOf(c), 0f);
        /// <summary>A window's glass, seen through: a faint tint (<paramref name="c"/>) and a sheen over what's behind it. It
        /// casts no shadow and writes no depth, so the sun, the survivor's sight light and the eye all go through.</summary>
        public static Surf WindowGlass(Color c) => new Surf(ClearMat, UvOf(c), 0f);
        /// <summary>A window's clear glass in its usual tint (<see cref="ClearTint"/>).</summary>
        public static Surf WindowGlass() => WindowGlass(ClearTint);
        /// <summary>Coloured glass (a church's), seen through dimly.</summary>
        public static Surf StainedGlass(Color c) => new Surf(StainedMat, UvOf(c), 0f);
        /// <summary>An unlit, self-lit colour (bulbs, lit diffusers, cooler back-lights).</summary>
        public static Surf Glow(Color c) => new Surf(GlowMat, UvOf(c), 0f);

        /// <summary>A colour nudged randomly in brightness (±<paramref name="amount"/>) for variety.</summary>
        public static Color Vary(Color c, float amount, System.Random rng)
        {
            float k = 1f + ((float)rng.NextDouble() * 2f - 1f) * amount;
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1f);
        }
    }
}
