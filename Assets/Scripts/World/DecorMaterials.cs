using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Cached URP materials for the raid map's textured surfaces (snow, asphalt, floors, walls,
    /// ceilings, crates, rugs, posters, signs) and the <see cref="Surf"/> handles that tell
    /// <see cref="RaidMesh"/> how to map them. Flat colours live in <see cref="DecorPalette"/>.
    /// </summary>
    internal static class DecorMaterials
    {
        private static readonly Dictionary<string, Material> s_Cache = new Dictionary<string, Material>(64);
        private static Shader s_Lit;

        private static Shader LitShader
        {
            get
            {
                if (s_Lit == null) s_Lit = Shader.Find("Universal Render Pipeline/Lit");
                if (s_Lit == null) s_Lit = Shader.Find("Standard");
                return s_Lit;
            }
        }

        private static Material Lit(Texture2D tex, float smoothness, float metallic)
        {
            string key = tex.name + "_" + Mathf.RoundToInt(smoothness * 20f) + "_" + Mathf.RoundToInt(metallic * 20f);
            if (s_Cache.TryGetValue(key, out Material m) && m != null && m.mainTexture == tex) return m;
            m = new Material(LitShader) { name = tex.name, hideFlags = HideFlags.DontUnloadUnusedAsset, enableInstancing = true };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            s_Cache[key] = m;
            return m;
        }

        /// <summary>A texture tiled in world metres (one repeat every <paramref name="metersPerRepeat"/>).</summary>
        public static Surf Planar(Texture2D tex, float metersPerRepeat, float smoothness = 0.15f, float metallic = 0f)
            => new Surf(Lit(tex, smoothness, metallic), Vector2.zero, 1f / Mathf.Max(0.01f, metersPerRepeat));

        /// <summary>A texture stretched once over a quad with explicit UVs.</summary>
        public static Surf Fitted(Texture2D tex, float smoothness = 0.1f)
            => new Surf(Lit(tex, smoothness, 0f), Vector2.zero, 0f);

        public static Surf Snow => Planar(DecorTextures.Snow(), 9f, 0.3f);
        public static Surf Asphalt => Planar(DecorTextures.Asphalt(), 6f, 0.25f);
        public static Surf Concrete => Planar(DecorTextures.Concrete(), 2f, 0.1f);
        public static Surf Gravel => Planar(DecorTextures.Gravel(), 1.5f, 0.05f);
        public static Surf Ceiling => Planar(DecorTextures.CeilingTiles(), 2.4f, 0.05f);
        public static Surf Hazard => Planar(DecorTextures.HazardStripes(), 0.6f, 0.3f);
        public static Surf HazardSign => Fitted(DecorTextures.HazardSign(), 0.2f);

        public static Surf WallInterior(WallDesign d) => Planar(DecorTextures.WallInterior(d), DecorTextures.WallSpan, 0.12f);
        public static Surf WallExterior(ExteriorStyle s, Color c) => Planar(DecorTextures.WallExterior(s, c), DecorTextures.WallSpan, 0.08f);

        public static Surf Linoleum(Color a, Color b, bool checker) => Planar(DecorTextures.Linoleum(a, b, checker), 1.2f, 0.35f);
        public static Surf WoodFloor(Color c) => Planar(DecorTextures.WoodFloor(c), 2.4f, 0.3f);
        public static Surf Carpet(Color c) => Planar(DecorTextures.Carpet(c), 1f, 0.02f);
        public static Surf Planks(Color c) => Planar(DecorTextures.Planks(c), 0.6f, 0.08f);
        public static Surf Corrugated(Color c) => Planar(DecorTextures.Corrugated(c), 0.8f, 0.35f, 0.3f);

        public static Surf Rug(int variant) => Fitted(DecorTextures.Rug(variant), 0.02f);
        public static Surf Poster(int kind) => Fitted(DecorTextures.Poster(kind), 0.2f);
        public static Surf Paper(int kind) => Fitted(DecorTextures.Paper(kind), 0.05f);
        public static Surf Stock(int kind) => Fitted(DecorTextures.Stock(kind), 0.2f);

        public static Surf Sign(string text, Color bg, Color fg, out float aspect)
            => Fitted(DecorTextures.Sign(text, bg, fg, out aspect), 0.3f);
    }
}
