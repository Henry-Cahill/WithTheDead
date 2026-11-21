using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Everything the HUD widgets read, gathered once by <see cref="HUDController.Bind"/>. Any of these
    /// may be null (the widgets degrade gracefully); <see cref="Gear"/> falls back to the singleton.
    /// </summary>
    public sealed class HudContext
    {
        public PlayerStats Stats;
        public PlayerInventory Inventory;
        public PlayerInteractor Interactor;
        public PlayerCombat Combat;
        public PlayerSleep Sleep;
        public PlayerSkills Skills;
        public PlayerBuilder Builder;
        public PlayerWounds Wounds;
        public PlayerPanic Panic;
        public PlayerFoodSickness Sickness;
        public PlayerFarming Farming;
        public PlayerHydration Hydration;
        public PlayerController Controller;
        public PlayerTemperature Temperature;
        public PlayerClothing Clothing;
        public PlayerGear GearComponent;

        /// <summary>The canvas root the widgets are built under.</summary>
        public RectTransform Root;

        /// <summary>The survivor's powered gear (the bound component, else the singleton).</summary>
        public PlayerGear Gear => GearComponent != null ? GearComponent : PlayerGear.Instance;
    }

    /// <summary>
    /// Small uGUI construction and formatting helpers shared by the HUD widgets: rects, tinted
    /// procedural images, labels with drop shadows, framed panels, filled bars, cached number strings
    /// (so per-frame updates don't allocate) and the shared HUD palette.
    /// </summary>
    public static class HudUI
    {
        // Palette.
        public static readonly Color Ink = new Color(0.95f, 0.95f, 0.95f, 0.94f);
        public static readonly Color Amber = new Color(1f, 0.70f, 0.22f, 1f);
        public static readonly Color Danger = new Color(1f, 0.30f, 0.24f, 1f);
        public static readonly Color Good = new Color(0.42f, 0.88f, 0.42f, 1f);
        public static readonly Color Teal = new Color(0.247f, 0.824f, 0.78f, 1f);
        public static readonly Color PanelColor = new Color(0.025f, 0.03f, 0.04f, 0.74f);
        public static readonly Color FrameColor = new Color(1f, 1f, 1f, 0.14f);

        private static Font _font;
        /// <summary>The built-in legacy UI font.</summary>
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>A plain rect: anchored at <paramref name="anchor"/>, pivoted at <paramref name="pivot"/>.</summary>
        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>A rect stretched over its whole parent.</summary>
        public static RectTransform Stretch(Transform parent, string name)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>A non-interactive image showing a procedural icon (centre-pivoted).</summary>
        public static Image Icon(Transform parent, string name, HudIcon icon, Color color, Vector2 anchor, Vector2 pos, float size, bool shadow = true)
        {
            RectTransform rt = Rect(parent, name, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = HudIcons.Get(icon);
            img.color = color;
            img.raycastTarget = false;
            img.preserveAspect = true;
            if (shadow) UITheme.AddShadow(img);
            return img;
        }

        /// <summary>A 9-sliced image (panel fill / frame / key cap). <paramref name="ppuMultiplier"/> scales the borders.</summary>
        public static Image Sliced(Transform parent, string name, HudIcon sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float ppuMultiplier = 2f)
        {
            RectTransform rt = Rect(parent, name, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = HudIcons.Get(sprite);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppuMultiplier;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A dark translucent panel with a thin light frame. Returns the panel rect; the frame
        /// image (for state colouring) comes back through <paramref name="frame"/>.</summary>
        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, out Image frame)
        {
            Image fill = Sliced(parent, name, HudIcon.PanelFill, PanelColor, anchor, pivot, pos, size);
            RectTransform rt = fill.rectTransform;
            frame = Sliced(rt, "Frame", HudIcon.PanelFrame, FrameColor, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            RectTransform frt = frame.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>A legacy UI label with a soft drop shadow.</summary>
        public static Text Label(Transform parent, string name, int fontSize, TextAnchor align, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = Rect(parent, name, anchor, pivot, pos, size);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            UITheme.AddShadow(text);
            return text;
        }

        /// <summary>A thin horizontal bar: a dark track and a left-to-right filled image (returned).</summary>
        public static Image Bar(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color track, Color fill, out RectTransform barRoot)
        {
            RectTransform rt = Rect(parent, name, anchor, pivot, pos, size);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = HudIcons.Get(HudIcon.White);
            bg.color = track;
            bg.raycastTarget = false;
            barRoot = rt;

            RectTransform frt = Stretch(rt, "Fill");
            var img = frt.gameObject.AddComponent<Image>();
            img.sprite = HudIcons.Get(HudIcon.White);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            img.color = fill;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Makes an image fill from one edge by a 0..1 amount.</summary>
        public static void MakeFilled(Image img, bool horizontal)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = horizontal ? Image.FillMethod.Horizontal : Image.FillMethod.Vertical;
            img.fillOrigin = horizontal ? (int)Image.OriginHorizontal.Left : (int)Image.OriginVertical.Bottom;
            img.fillAmount = 1f;
        }

        /// <summary>SetActive only when the state actually changes.</summary>
        public static void Show(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        /// <summary>The colour with a new alpha.</summary>
        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        /// <summary>A 0..1 sine pulse between <paramref name="min"/> and 1 (unscaled time, keeps going while paused).</summary>
        public static float Pulse(float speed, float min = 0.4f)
            => min + (1f - min) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed));

        /// <summary>Green (good) through amber to red (bad) for a 0..1 condition/charge.</summary>
        public static Color Condition(float v01)
        {
            v01 = Mathf.Clamp01(v01);
            return v01 > 0.5f ? Color.Lerp(Amber, Good, (v01 - 0.5f) * 2f) : Color.Lerp(Danger, Amber, v01 * 2f);
        }

        private static string[] _ints;
        /// <summary>A cached decimal string for 0..999 (anything else allocates).</summary>
        public static string Int(int v)
        {
            if (v < 0 || v > 999) return v.ToString();
            if (_ints == null) _ints = new string[1000];
            return _ints[v] ?? (_ints[v] = v.ToString());
        }

        private static string[] _twoDigits;
        /// <summary>A cached zero-padded "00".."99".</summary>
        public static string Two(int v)
        {
            v = Mathf.Clamp(v, 0, 99);
            if (_twoDigits == null) _twoDigits = new string[100];
            return _twoDigits[v] ?? (_twoDigits[v] = v.ToString("00"));
        }

        /// <summary>"m:ss" for a duration in seconds (allocates — call only when the second changes).</summary>
        public static string Clock(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            return Int(totalSeconds / 60) + ":" + Two(totalSeconds % 60);
        }

        /// <summary>Letter-spaced capitals for small headings, "PAUSED" → "P A U S E D" (allocates — build once).</summary>
        public static string Spaced(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length * 2);
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(char.ToUpperInvariant(s[i]));
            }
            return sb.ToString();
        }
    }
}
