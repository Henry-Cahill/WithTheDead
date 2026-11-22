using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The one palette and the shared styling helpers for the code-built UI. Every colour a screen uses is
    /// defined here (the core palette below, per-area colours in the UITheme.*.cs partials), so the hub, the
    /// menus and the HUD read as one game and can be retuned in one place. Screens never write colour literals.
    /// </summary>
    public static partial class UITheme
    {
        #region Core palette
        public static readonly Color Backdrop = new Color(0.03f, 0.035f, 0.045f, 0.92f);
        public static readonly Color PanelColor = new Color(0.075f, 0.085f, 0.10f, 0.97f);
        public static readonly Color PanelLight = new Color(0.12f, 0.135f, 0.155f, 0.97f);
        public static readonly Color RowColor = new Color(0.10f, 0.11f, 0.13f, 0.95f);
        public static readonly Color RowAlt = new Color(0.085f, 0.095f, 0.11f, 0.95f);
        public static readonly Color Accent = new Color(0.86f, 0.62f, 0.22f);
        public static readonly Color TextColor = new Color(0.90f, 0.91f, 0.93f);
        public static readonly Color TextDim = new Color(0.58f, 0.62f, 0.68f);
        public static readonly Color Good = new Color(0.52f, 0.80f, 0.48f);
        public static readonly Color Bad = new Color(0.88f, 0.40f, 0.34f);
        public static readonly Color Warn = new Color(0.92f, 0.74f, 0.30f);
        public static readonly Color ButtonColor = new Color(0.20f, 0.23f, 0.27f);
        public static readonly Color ButtonAccent = new Color(0.55f, 0.40f, 0.14f);
        public static readonly Color ButtonMuted = new Color(0.13f, 0.15f, 0.18f, 0.92f);
        public static readonly Color ButtonDisabled = new Color(0.18f, 0.18f, 0.20f);

        // Widget parts.
        public static readonly Color ButtonHover = new Color(1.28f, 1.20f, 1.04f, 1f);   // a warm lamp-light hover (tint multiplier)
        public static readonly Color ButtonPressed = new Color(0.82f, 0.78f, 0.72f, 1f);
        public static readonly Color ButtonDisabledTint = new Color(0.55f, 0.55f, 0.55f, 0.6f);
        public static readonly Color Well = new Color(0f, 0f, 0f, 0.25f);                  // scroll lists, insets
        public static readonly Color Track = new Color(0.16f, 0.18f, 0.21f);               // slider track
        public static readonly Color Field = new Color(0.05f, 0.055f, 0.065f);             // text input
        public static readonly Color BarBack = new Color(0f, 0f, 0f, 0.5f);

        // A soft drop shadow so light text stays readable over the bright ground.
        public static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.65f);
        public static readonly Color ElevateShadow = new Color(0f, 0f, 0f, 0.45f);
        public static readonly Vector2 ShadowOffset = new Vector2(1.4f, -1.4f);
        #endregion

        #region Colour helpers
        public static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        /// <summary>Black at the given opacity: scrims, wells, vignettes.</summary>
        public static Color Ink(float alpha) => new Color(0f, 0f, 0f, alpha);

        /// <summary>White at the given opacity: hairlines, sheens, faint fills.</summary>
        public static Color Glass(float alpha) => new Color(1f, 1f, 1f, alpha);

        /// <summary>The same colour at another opacity.</summary>
        public static Color Fade(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

        /// <summary>The colour scaled toward black (k &lt; 1) keeping its alpha unless one is given.</summary>
        public static Color Shade(Color c, float k, float alpha = -1f) => new Color(c.r * k, c.g * k, c.b * k, alpha < 0f ? c.a : alpha);

        /// <summary>The colour brightened by adding to each channel (clamped), keeping its alpha unless one is given.</summary>
        public static Color Lift(Color c, float add, float alpha = -1f) =>
            new Color(Mathf.Min(c.r + add, 1f), Mathf.Min(c.g + add, 1f), Mathf.Min(c.b + add, 1f), alpha < 0f ? c.a : alpha);
        #endregion

        private static Sprite s_Round, s_RoundOutline;

        /// <summary>A 9-sliced rounded-rectangle sprite (white, ~10 px corners), generated once at runtime.</summary>
        public static Sprite RoundSprite => s_Round != null ? s_Round : (s_Round = MakeRounded(false));

        /// <summary>The matching rounded outline ring (for hover/focus borders).</summary>
        public static Sprite RoundOutlineSprite => s_RoundOutline != null ? s_RoundOutline : (s_RoundOutline = MakeRounded(true));

        // Draws a rounded rect (or its 2 px outline ring) into a small texture by signed distance, antialiased,
        // sliced so the corners keep their radius at any size. No art asset needed.
        private static Sprite MakeRounded(bool outline)
        {
            const int size = 32;
            const float radius = 8f;
            const float ring = 2f;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[size * size];
            const float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (outline) a *= Mathf.Clamp01(d + ring + 0.5f);
                    px[y * size + x] = Glass(a);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, Vector4.one * (radius + 2f));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>Gives an Image soft rounded corners (sliced rounded-rect sprite, keeps its colour).</summary>
        public static void Round(Image img)
        {
            if (img == null) return;
            img.sprite = RoundSprite;
            img.type = Image.Type.Sliced;
        }

        /// <summary>A soft drop shadow under a card/window so it lifts off the backdrop.</summary>
        public static void Elevate(Graphic graphic)
        {
            if (graphic == null) return;
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ElevateShadow;
            shadow.effectDistance = new Vector2(0f, -5f);
        }

        /// <summary>Attach (or refresh) a soft drop shadow on a uGUI graphic.</summary>
        public static void AddShadow(Graphic graphic)
        {
            if (graphic == null) return;
            var shadow = graphic.GetComponent<Shadow>();
            if (shadow == null) shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = ShadowOffset;
        }

        /// <summary>Add a thin accent strip along the top edge of a panel (inset keeps it off rounded corners).</summary>
        public static void AddTopAccent(RectTransform panel, Color color, float height = 3f, float inset = 0f)
        {
            if (panel == null) return;
            var go = new GameObject("Accent");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(-inset * 2f, height);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
