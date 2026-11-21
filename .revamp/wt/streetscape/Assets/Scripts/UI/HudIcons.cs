using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Bakes the HUD's procedural art (<see cref="HudIconArt"/>) into sprites on first use and caches
    /// them for the rest of the run. White RGB with the coverage in alpha, so any <c>Image.color</c>
    /// tints them; small icons get mipmaps so they stay smooth when the canvas scales them down.
    /// </summary>
    public static class HudIcons
    {
        private static Sprite[] _cache;

        /// <summary>The sprite for an icon (baked once, re-baked if Unity has unloaded it).</summary>
        public static Sprite Get(HudIcon id)
        {
            if (_cache == null) _cache = new Sprite[System.Enum.GetValues(typeof(HudIcon)).Length];
            Sprite s = _cache[(int)id];
            if (s != null) return s;
            s = Bake(id);
            _cache[(int)id] = s;
            return s;
        }

        private static Sprite Bake(HudIcon id)
        {
            HudIconSpec spec = HudIconArt.Spec(id);
            var raster = new HudRaster(spec.Width, spec.Height, spec.Units);
            HudIconArt.Draw(id, raster);

            var px = new Color32[spec.Width * spec.Height];
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(raster.Alpha[i]) * 255f));

            var tex = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, spec.Mips)
            {
                name = "HUD_" + id,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = spec.Mips ? FilterMode.Trilinear : FilterMode.Bilinear,
            };
            if (spec.Mips) tex.mipMapBias = -0.35f;   // a touch crisper when minified
            tex.SetPixels32(px);
            tex.Apply(spec.Mips, true);

            var border = new Vector4(spec.BorderX, spec.BorderY, spec.BorderX, spec.BorderY);
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, spec.Width, spec.Height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = tex.name;
            return sprite;
        }
    }
}
