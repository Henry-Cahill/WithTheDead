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

        /// <summary>Snow surface material, tiled for snowy ground.</summary>
        public static Surf Snow => Planar(DecorTextures.Snow(), 9f, 0.3f);
        /// <summary>Asphalt surface material, tiled for road surfaces.</summary>
        public static Surf Asphalt => Planar(DecorTextures.Asphalt(), 6f, 0.25f);
        /// <summary>Concrete surface material, tiled for interior floors.</summary>
        public static Surf Concrete => Planar(DecorTextures.Concrete(), 2f, 0.1f);
        /// <summary>Gravel surface material, tiled for small ground details.</summary>
        public static Surf Gravel => Planar(DecorTextures.Gravel(), 1.5f, 0.05f);
        /// <summary>Ceiling surface material, tiled for ceiling tiles.</summary>
        public static Surf Ceiling => Planar(DecorTextures.CeilingTiles(), 2.4f, 0.05f);
        /// <summary>Hazard stripe material, tiled for warning patterns.</summary>
        public static Surf Hazard => Planar(DecorTextures.HazardStripes(), 0.6f, 0.3f);
        /// <summary>Hazard sign material, fitted to a quad.</summary>
        public static Surf HazardSign => Fitted(DecorTextures.HazardSign(), 0.2f);

        /// <summary>Creates a planar material for interior walls using the given design.</summary>
        public static Surf WallInterior(WallDesign d) => Planar(DecorTextures.WallInterior(d), DecorTextures.WallSpan, 0.12f);
        /// <summary>Creates a planar material for exterior walls using the given style and colour.</summary>
        public static Surf WallExterior(ExteriorStyle s, Color c) => Planar(DecorTextures.WallExterior(s, c), DecorTextures.WallSpan, 0.08f);

        /// <summary>Creates a Linoleum surface material with optional checker pattern.</summary>
        public static Surf Linoleum(Color a, Color b, bool checker) => Planar(DecorTextures.Linoleum(a, b, checker), 1.2f, 0.35f);
        /// <summary>Creates a wood floor material with the given colour.</summary>
        public static Surf WoodFloor(Color c) => Planar(DecorTextures.WoodFloor(c), 2.4f, 0.3f);
        /// <summary>Creates a carpet material with the given colour.</summary>
        public static Surf Carpet(Color c) => Planar(DecorTextures.Carpet(c), 1f, 0.02f);
        /// <summary>Creates a planks material with the given colour.</summary>
        public static Surf Planks(Color c) => Planar(DecorTextures.Planks(c), 0.6f, 0.08f);
        /// <summary>Creates a corrugated metal material with the given colour.</summary>
        public static Surf Corrugated(Color c) => Planar(DecorTextures.Corrugated(c), 0.8f, 0.35f, 0.3f);

        /// <summary>Creates a rug material for the given variant.</summary>
        public static Surf Rug(int variant) => Fitted(DecorTextures.Rug(variant), 0.02f);
        /// <summary>Creates a poster material of the specified kind.</summary>
        public static Surf Poster(int kind) => Fitted(DecorTextures.Poster(kind), 0.2f);
        /// <summary>Creates a paper material of the specified kind.</summary>
        public static Surf Paper(int kind) => Fitted(DecorTextures.Paper(kind), 0.05f);
        /// <summary>Creates a stock material of the specified kind.</summary>
        public static Surf Stock(int kind) => Fitted(DecorTextures.Stock(kind), 0.2f);

        // Summer ground for the wave-survival starter home's yard and street.
        /// <summary>Creates a grass material for summer ground.</summary>
        public static Surf Grass => Planar(DecorTextures.Grass(), 3f, 0.04f);
        /// <summary>Creates a mulch material with the given colour.</summary>
        public static Surf Mulch(Color c) => Planar(DecorTextures.Mulch(c), 1.5f, 0.02f);
        /// <summary>Creates a dry asphalt material for summer ground.</summary>
        public static Surf DryAsphalt => Planar(DecorTextures.DryAsphalt(), 6f, 0.2f);

        /// <summary>Creates a sign material with the given text and colours, outputting its aspect ratio.</summary>
        public static Surf Sign(string text, Color bg, Color fg, out float aspect)
            => Fitted(DecorTextures.Sign(text, bg, fg, out aspect), 0.3f);
    }
}
