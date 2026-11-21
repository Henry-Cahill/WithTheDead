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

        /// <summary>Attach (or refresh) a soft drop shadow on a uGUI graphic.</summary>
        public static void AddShadow(Graphic graphic)
        {
            if (graphic == null) return;
            var shadow = graphic.GetComponent<Shadow>();
            if (shadow == null) shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = ShadowOffset;
        }

        /// <summary>Add a thin accent strip along the top edge of a panel.</summary>
        public static void AddTopAccent(RectTransform panel, Color color, float height = 3f)
        {
            if (panel == null) return;
            var go = new GameObject("Accent");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
