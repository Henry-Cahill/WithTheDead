using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Shared styling helpers for the code-built UI. Keeps the runtime HUD and
    /// panels visually consistent (and legible over the bright world) without a
    /// design asset. Intentionally tiny — just enough to de-duplicate the polish.
    /// </summary>
    public static class UITheme
    {
        // A soft drop shadow so light text stays readable over the bright ground.
        public static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.65f);
        public static readonly Vector2 ShadowOffset = new Vector2(1.4f, -1.4f);

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
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
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
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
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
