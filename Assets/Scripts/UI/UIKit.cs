using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The shared builder for code-made uGUI: canvases, panels, labels, buttons, scroll lists, toggles, sliders
    /// and text fields in one consistent dark style. Every new screen and HUD widget is built with this so the
    /// hub, the menus and the HUD read as one game.
    /// </summary>
    public static class UIKit
    {
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

        private static Font s_Font;
        /// <summary>Provides the default UI font, loading it lazily from Unity resources.</summary>
        public static Font Font => s_Font != null ? s_Font : (s_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        #region Canvas and rects
        /// <summary>Creates a new UI canvas with the given name and sorting order, optionally parented, for building UI screens.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        /// <summary>Ensures an EventSystem exists in the scene, creating one if necessary, so UI input works.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.SetActive(false);
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
            es.SetActive(true);
        }

        /// <summary>Creates a RectTransform GameObject with the given name, parented under the supplied Transform.</summary>
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Stretches a rect over its parent with optional insets (left, bottom, right, top).</summary>
        /// <summary>Creates a stretched rect covering its parent, optionally applying inset margins.</summary>
        public static RectTransform Stretch(RectTransform rt, float l = 0f, float b = 0f, float r = 0f, float t = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Anchors a rect at a normalised point with a fixed size.</summary>
        /// <summary>Places a RectTransform at a normalized anchor point with a fixed size and pivot.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Creates an Image component on a new UI panel under the given parent.</summary>
        public static Image Panel(Transform parent, string name, Color color)
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }
        #endregion

        #region Widgets
        /// <summary>Creates a Text UI element with the specified content and styling.</summary>
        public static Text Label(Transform parent, string text, int size = 18, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? TextColor;
            t.alignment = align;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            UITheme.AddShadow(t);
            return t;
        }

        /// <summary>Creates a Button UI element with a label and click callback, applying the default style.</summary>
        public static Button Button(Transform parent, string text, Action onClick, Color? color = null, int fontSize = 18)
        {
            Color c = color ?? ButtonColor;
            RectTransform rt = Rect("Button", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            cb.colorMultiplier = 1.2f;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            Text label = Label(rt, text, fontSize, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 6f, 2f, 6f, 2f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Add subtle decorative rivets at the button corners (4×4 px) using Accent colour
            // Top‑left
            var rivetTL = Rect("RivetTL", rt);
            rivetTL.sizeDelta = new Vector2(4f, 4f);
            rivetTL.anchorMin = rivetTL.anchorMax = new Vector2(0f, 1f);
            rivetTL.pivot = new Vector2(0f, 1f);
            var imgTL = rivetTL.gameObject.AddComponent<Image>();
            imgTL.color = Accent;
            // Top‑right
            var rivetTR = Rect("RivetTR", rt);
            rivetTR.sizeDelta = new Vector2(4f, 4f);
            rivetTR.anchorMin = rivetTR.anchorMax = new Vector2(1f, 1f);
            rivetTR.pivot = new Vector2(1f, 1f);
            var imgTR = rivetTR.gameObject.AddComponent<Image>();
            imgTR.color = Accent;
            // Bottom‑left
            var rivetBL = Rect("RivetBL", rt);
            rivetBL.sizeDelta = new Vector2(4f, 4f);
            rivetBL.anchorMin = rivetBL.anchorMax = new Vector2(0f, 0f);
            rivetBL.pivot = new Vector2(0f, 0f);
            var imgBL = rivetBL.gameObject.AddComponent<Image>();
            imgBL.color = Accent;
            // Bottom‑right
            var rivetBR = Rect("RivetBR", rt);
            rivetBR.sizeDelta = new Vector2(4f, 4f);
            rivetBR.anchorMin = rivetBR.anchorMax = new Vector2(1f, 0f);
            rivetBR.pivot = new Vector2(1f, 0f);
            var imgBR = rivetBR.gameObject.AddComponent<Image>();
            imgBR.color = Accent;

            return btn;
        }

        /// <summary>Sets the displayed text of a Button's child Text component.</summary>
        public static void SetButtonText(Button b, string text)
        {
            Text t = b != null ? b.GetComponentInChildren<Text>() : null;
            if (t != null) t.text = text;
        }

        /// <summary>A vertical scroll view; add rows to the returned content (it sizes itself to fit them).</summary>
        /// <summary>Creates a vertical scroll list container; rows can be added to its content.</summary>
        public static RectTransform ScrollList(Transform parent, string name, float spacing = 4f, int padding = 6)
        {
            RectTransform view = Rect(name, parent);
            var bg = view.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.25f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 28f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            view.gameObject.AddComponent<RectMask2D>();

            RectTransform content = Rect("Content", view);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = new RectOffset(padding, padding, padding, padding);
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = view;
            return content;
        }

        /// <summary>A horizontal row of a fixed height inside a layout (for list rows).</summary>
        /// <summary>Creates a horizontal row container for list entries, with optional background.</summary>
        public static RectTransform Row(Transform parent, float height, Color? background = null, float spacing = 8f, int padding = 6)
        {
            RectTransform rt = Rect("Row", parent);
            if (background.HasValue) rt.gameObject.AddComponent<Image>().color = background.Value;
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(padding, padding, 2, 2);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            Size(rt.gameObject, -1f, height);
            return rt;
        }

        /// <summary>A vertical stack (for sidebars and forms).</summary>
        /// <summary>Creates a vertical column container for stacking UI elements.</summary>
        public static RectTransform Column(Transform parent, float spacing = 6f, int padding = 8)
        {
            RectTransform rt = Rect("Column", parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return rt;
        }

        /// <summary>Sets a layout element's preferred size (-1 leaves an axis alone) and flexible width.</summary>
        /// <summary>Sets a LayoutElement's preferred size and flexible width on a GameObject.</summary>
        public static LayoutElement Size(GameObject go, float width, float height, float flexibleWidth = -1f)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (width >= 0f) { le.preferredWidth = width; le.minWidth = width; }
            if (height >= 0f) { le.preferredHeight = height; le.minHeight = height; }
            if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
            return le;
        }

        /// <summary>Creates a toggle with a label, handling value changes via the provided callback.</summary>
        public static Toggle Toggle(Transform parent, string label, bool value, Action<bool> onChanged)
        {
            RectTransform row = Row(parent, 30f, null, 10f, 2);
            RectTransform box = Rect("Box", row);
            var bg = box.gameObject.AddComponent<Image>();
            bg.color = ButtonColor;
            Size(box.gameObject, 26f, 26f);
            RectTransform tick = Rect("Tick", box);
            Stretch(tick, 5f, 5f, 5f, 5f);
            var tickImg = tick.gameObject.AddComponent<Image>();
            tickImg.color = Accent;
            var toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = tickImg;
            toggle.isOn = value;
            if (onChanged != null) toggle.onValueChanged.AddListener(v => onChanged(v));
            Text t = Label(row, label, 17);
            Size(t.gameObject, -1f, 26f, 1f);
            return toggle;
        }

        /// <summary>Creates a slider with numeric range, displaying its value and invoking a callback on change.</summary>
        public static Slider Slider(Transform parent, string label, float min, float max, float value, Action<float> onChanged, string format = "0.00", bool whole = false)
        {
            RectTransform row = Row(parent, 32f, null, 10f, 2);
            Text name = Label(row, label, 17);
            Size(name.gameObject, 260f, 28f);
            RectTransform track = Rect("Slider", row);
            Size(track.gameObject, -1f, 22f, 1f);
            var bg = track.gameObject.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.18f, 0.21f);
            RectTransform fillArea = Rect("Fill Area", track);
            Stretch(fillArea, 2f, 2f, 2f, 2f);
            RectTransform fill = Rect("Fill", fillArea);
            Stretch(fill);
            fill.gameObject.AddComponent<Image>().color = new Color(Accent.r * 0.7f, Accent.g * 0.7f, Accent.b * 0.7f);
            RectTransform handleArea = Rect("Handle Area", track);
            Stretch(handleArea, 6f, 0f, 6f, 0f);
            RectTransform handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(12f, 0f);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.color = Accent;
            var s = track.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = hImg;
            s.minValue = min;
            s.maxValue = max;
            s.wholeNumbers = whole;
            s.value = value;
            Text readout = Label(row, value.ToString(format), 16, TextDim, TextAnchor.MiddleRight);
            Size(readout.gameObject, 70f, 28f);
            s.onValueChanged.AddListener(v => { readout.text = v.ToString(format); onChanged?.Invoke(v); });
            return s;
        }

        /// <summary>Creates an input field with a default value and placeholder, invoking a callback when edited.</summary>
        public static InputField Input(Transform parent, string value, string placeholder, Action<string> onChanged, int fontSize = 18)
        {
            RectTransform rt = Rect("Input", parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.055f, 0.065f);
            Text text = Label(rt, value, fontSize, TextColor, TextAnchor.MiddleLeft);
            text.raycastTarget = false;
            Stretch(text.rectTransform, 8f, 2f, 8f, 2f);
            text.supportRichText = false;
            Text ph = Label(rt, placeholder, fontSize, TextDim, TextAnchor.MiddleLeft, FontStyle.Italic);
            Stretch(ph.rectTransform, 8f, 2f, 8f, 2f);
            var input = rt.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            input.text = value;
            input.targetGraphic = bg;
            if (onChanged != null) input.onValueChanged.AddListener(v => onChanged(v));
            return input;
        }

        /// <summary>A thin bar that fills left-to-right (health, progress, durability).</summary>
        /// <summary>Creates a thin bar UI element that fills proportionally based on a value (0-1).</summary>
        public static Image Bar(Transform parent, Color fillColor, float value01, float height = 8f)
        {
            RectTransform rt = Rect("Bar", parent);
            rt.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            Size(rt.gameObject, -1f, height, 1f);
            RectTransform f = Rect("Fill", rt);
            f.anchorMin = Vector2.zero;
            f.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
            f.offsetMin = Vector2.zero;
            f.offsetMax = Vector2.zero;
            var img = f.gameObject.AddComponent<Image>();
            img.color = fillColor;
            return img;
        }

        /// <summary>Updates the fill amount of a bar Image to a new proportion (0-1).</summary>
        public static void SetBar(Image fill, float value01)
        {
            if (fill == null) return;
            RectTransform f = fill.rectTransform;
            f.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
        }

        /// <summary>Removes all child GameObjects under a Transform, cleaning up a UI container.</summary>
        public static void Clear(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        public static string Colored(string s, Color c) => "<color=" + Hex(c) + ">" + s + "</color>";
        #endregion
    }

    /// <summary>
    /// A full-screen modal panel (a trader, the stash, the hideout, settings). Opening one blocks gameplay input
    /// and frees the cursor; Esc or the close button shuts it. Subclasses build into <see cref="Body"/>.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        public static UIScreen Current { get; private set; }

        protected Canvas Canvas;
        protected RectTransform Window;
        protected RectTransform Body;
        protected Text TitleText;
        protected Text StatusText;
        private bool _hadLock;
        private CursorLockMode _prevLock;
        private bool _prevVisible;

        /// <summary>Raised whenever any screen closes (the hub refreshes its prompts).</summary>
        public static event Action Closed;

        protected virtual Vector2 WindowSize => new Vector2(1500f, 860f);

        /// <summary>Creates and opens a screen of type <typeparamref name="T"/> (closing any other first).</summary>
        public static T Show<T>(Action<T> configure = null) where T : UIScreen
        {
            if (Current != null) Current.Close();
            var go = new GameObject(typeof(T).Name);
            T s = go.AddComponent<T>();
            configure?.Invoke(s);
            s.OpenInternal();
            return s;
        }

        private void OpenInternal()
        {
            Current = this;
            Canvas = UIKit.CreateCanvas(GetType().Name + " Canvas", 300, transform);
            RectTransform root = (RectTransform)Canvas.transform;
            Image shade = UIKit.Panel(root, "Shade", UIKit.Backdrop);
            UIKit.Stretch(shade.rectTransform);

            Image win = UIKit.Panel(root, "Window", UIKit.PanelColor);
            Window = UIKit.Place(win.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WindowSize);
            UITheme.AddTopAccent(Window, UIKit.Accent, 3f);

            TitleText = UIKit.Label(Window, Title, 30, UIKit.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(TitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -10f), new Vector2(WindowSize.x - 200f, 46f));
            Button close = UIKit.Button(Window, "Close  [Esc]", Close, UIKit.ButtonColor, 16);
            UIKit.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -12f), new Vector2(140f, 36f));

            StatusText = UIKit.Label(Window, "", 16, UIKit.Warn, TextAnchor.MiddleLeft);
            UIKit.Place(StatusText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 8f), new Vector2(WindowSize.x - 48f, 28f));

            Body = UIKit.Rect("Body", Window);
            UIKit.Stretch(Body, 20f, 40f, 20f, 64f);

            _hadLock = true;
            _prevLock = Cursor.lockState;
            _prevVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UIState.GameplayBlocked = true;
            Build();
        }

        /// <summary>The window title.</summary>
        protected abstract string Title { get; }

        /// <summary>Builds the screen's content into <see cref="Body"/>.</summary>
        protected abstract void Build();

        /// <summary>Clears and rebuilds the body (after a purchase, an upgrade...).</summary>
        public void Rebuild()
        {
            UIKit.Clear(Body);
            Build();
        }

        public void SetStatus(string msg, bool bad = false)
        {
            if (StatusText == null) return;
            StatusText.text = msg ?? "";
            StatusText.color = bad ? UIKit.Bad : UIKit.Good;
        }

        protected virtual void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && UIState.EscapeConsumedFrame != Time.frameCount)
            {
                UIState.EscapeConsumedFrame = Time.frameCount;
                Close();
            }
        }

        public virtual void Close()
        {
            if (this == null) return;
            if (Current == this) Current = null;
            UIState.GameplayBlocked = false;
            if (_hadLock)
            {
                Cursor.lockState = _prevLock;
                Cursor.visible = _prevVisible;
            }
            OnClosed();
            Destroy(gameObject);
            Closed?.Invoke();
        }

        protected virtual void OnClosed() { }
    }

    /// <summary>
    /// The shared HUD canvas feature widgets live on (a detector screen, the limb doll, the visibility lamp...).
    /// It hides itself in the diegetic and hardcore HUD modes, where the world itself carries the information.
    /// </summary>
    public class HudLayer : MonoBehaviour
    {
        private static HudLayer s_Instance;
        private Canvas _canvas;
        private CanvasGroup _group;

        public static RectTransform Root
        {
            get
            {
                if (s_Instance == null)
                {
                    var go = new GameObject("HudLayer");
                    s_Instance = go.AddComponent<HudLayer>();
                    s_Instance._canvas = UIKit.CreateCanvas("HudLayer Canvas", 40, go.transform);
                    s_Instance._group = s_Instance._canvas.gameObject.AddComponent<CanvasGroup>();
                    s_Instance._group.blocksRaycasts = false;
                    s_Instance._group.interactable = false;
                }
                return (RectTransform)s_Instance._canvas.transform;
            }
        }

        /// <summary>A widget slot anchored at a screen point (0..1), with a size in reference pixels.</summary>
        public static RectTransform Anchor(string name, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
            => UIKit.Place(UIKit.Rect(name, Root), anchor, pivot, offset, size);

        private void Update()
        {
            if (_group == null) return;
            bool hidden = GameSettings.Current.HudHidden || UIScreen.Current != null;
            _group.alpha = hidden ? 0f : 1f;
        }

        private void OnDestroy() { if (s_Instance == this) s_Instance = null; }
    }
}
