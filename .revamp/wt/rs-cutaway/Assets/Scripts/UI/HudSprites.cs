using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Procedural sprites for the per-view HUDs and the pause menu that aren't line art (see <see cref="HudIcons"/>
    /// for that): soft scrims that keep text readable over bright snow, a dark vignette, the damage-direction arc
    /// and the bodycam's scanlines. White with the shape in alpha, so an <c>Image.color</c> tints them. Baked once.
    /// </summary>
    public static class HudSprites
    {
        private static Sprite s_ScrimLeft, s_ScrimBottom, s_Vignette, s_DamageArc;
        private static Texture2D s_Scanlines;

        /// <summary>Opaque at the left edge, easing to clear at the right (mirror it with a negative x scale).</summary>
        public static Sprite ScrimLeft => s_ScrimLeft != null ? s_ScrimLeft : (s_ScrimLeft = Ramp(true));

        /// <summary>Opaque at the bottom edge, easing to clear at the top.</summary>
        public static Sprite ScrimBottom => s_ScrimBottom != null ? s_ScrimBottom : (s_ScrimBottom = Ramp(false));

        /// <summary>Clear in the middle, opaque toward the corners.</summary>
        public static Sprite Vignette
        {
            get
            {
                if (s_Vignette != null) return s_Vignette;
                const int size = 128;
                var px = new Color32[size * size];
                float c = (size - 1) * 0.5f, maxD = Mathf.Sqrt(2f) * c;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / maxD;
                        float a = Mathf.Pow(Mathf.InverseLerp(0.3f, 1f, d), 1.4f);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                s_Vignette = Bake(px, size, size, "HUD_Vignette");
                return s_Vignette;
            }
        }

        /// <summary>A soft-edged arc across the top of a circle (±32°), for the damage-direction indicator: rotate
        /// the image about the screen centre to point it.</summary>
        public static Sprite DamageArc
        {
            get
            {
                if (s_DamageArc != null) return s_DamageArc;
                const int size = 256;
                const float radius = 112f, halfWidth = 9f, halfAngle = 32f;
                var px = new Color32[size * size];
                float c = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - c, dy = y - c;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, 90f));
                        float radial = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(halfWidth * 0.35f, halfWidth, Mathf.Abs(r - radius)));
                        float inner = r < radius ? Mathf.Clamp01(1f - (radius - r) / (halfWidth * 3.5f)) * 0.35f : 0f;   // a faint glow inward
                        float angular = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(halfAngle * 0.45f, halfAngle, ang));
                        float a = Mathf.Clamp01(Mathf.Max(radial, inner)) * angular;
                        px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                s_DamageArc = Bake(px, size, size, "HUD_DamageArc");
                return s_DamageArc;
            }
        }

        /// <summary>One dark line every three pixels, tiled vertically (point-filtered, repeating) for a CRT/VHS look.</summary>
        public static Texture2D Scanlines
        {
            get
            {
                if (s_Scanlines != null) return s_Scanlines;
                s_Scanlines = new Texture2D(1, 3, TextureFormat.RGBA32, false)
                {
                    name = "HUD_Scanlines",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point,
                };
                s_Scanlines.SetPixels32(new[] { new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0) });
                s_Scanlines.Apply(false, true);
                return s_Scanlines;
            }
        }

        private static Sprite Ramp(bool horizontal)
        {
            const int n = 128;
            int w = horizontal ? n : 2, h = horizontal ? 2 : n;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = (horizontal ? x : y) / (float)(n - 1);
                    float a = Mathf.Pow(1f - t, 1.7f);   // eased, so the scrim has no visible edge
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            return Bake(px, w, h, horizontal ? "HUD_ScrimLeft" : "HUD_ScrimBottom");
        }

        private static Sprite Bake(Color32[] px, int w, int h, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            Sprite s = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = name;
            return s;
        }
    }
}
