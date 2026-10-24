using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Tints one renderer through a <see cref="MaterialPropertyBlock"/>, so fixtures that change colour (a towel drying, a
    /// soiled toilet seat) keep sharing their materials: <see cref="Renderer.material"/> would copy the material for every
    /// renderer it touched, leaking a copy in edit mode and breaking batching.
    /// </summary>
    internal static class FixtureColour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColor = Shader.PropertyToID("_Color");

        /// <summary>The renderer's colour as drawn: its block's tint if it has one, else its shared material's.</summary>
        internal static Color Get(Renderer r)
        {
            if (r == null) return Color.white;
            if (r.HasPropertyBlock())
            {
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                if (!block.isEmpty) return block.GetColor(BaseColor);
            }
            Material m = r.sharedMaterial;
            if (m == null) return Color.white;
            return m.HasProperty(BaseColor) ? m.GetColor(BaseColor) : m.HasProperty(LegacyColor) ? m.GetColor(LegacyColor) : Color.white;
        }

        /// <summary>Tints <paramref name="r"/> <paramref name="c"/>, reusing <paramref name="block"/> (made on first use).</summary>
        internal static void Set(Renderer r, ref MaterialPropertyBlock block, Color c)
        {
            if (r == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColor, c);
            block.SetColor(LegacyColor, c);
            r.SetPropertyBlock(block);
        }
    }
}
