using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A hover tooltip panel for a hub screen: one per screen canvas, drawn above everything, following the
    /// mouse and flipping at the screen edges. Rows opt in with <see cref="Attach"/>.
    /// </summary>
    public sealed class HubTooltip : MonoBehaviour
    {
        private const float MaxWidth = 400f;

        private RectTransform _root;
        private RectTransform _canvasRect;
        private Text _text;
        private LayoutElement _textSize;
        private CanvasGroup _group;
        private object _owner;

        /// <summary>Creates a new tooltip attached to the given canvas and returns the component.</summary>
        public static HubTooltip Create(Canvas canvas)
        {
            var canvasRect = (RectTransform)canvas.transform;
            Image bg = UIKit.Panel(canvasRect, "Tooltip", new Color(0.045f, 0.05f, 0.06f, 0.97f));
            bg.raycastTarget = false;
            var outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
            RectTransform rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 1f);
            var v = bg.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 9, 10);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            var fit = bg.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text t = UIKit.Label(rt, "", 15, UIKit.TextColor, TextAnchor.UpperLeft);
            t.lineSpacing = 1.05f;

            var tip = bg.gameObject.AddComponent<HubTooltip>();
            tip._root = rt;
            tip._canvasRect = canvasRect;
            tip._text = t;
            tip._textSize = UIKit.Size(t.gameObject, MaxWidth, -1f);
            tip._group = bg.gameObject.AddComponent<CanvasGroup>();
            tip._group.blocksRaycasts = false;
            tip._group.interactable = false;
            tip._group.alpha = 0f;
            return tip;
        }

        /// <summary>Shows <paramref name="richText"/> on behalf of <paramref name="owner"/> (whose exit hides it again).</summary>
        public void Show(object owner, string richText)
        {
            if (string.IsNullOrEmpty(richText)) { Hide(owner); return; }
            _owner = owner;
            _text.text = richText;
            float w = Mathf.Min(MaxWidth, _text.preferredWidth + 2f);
            _textSize.preferredWidth = w;
            _textSize.minWidth = w;
            transform.SetAsLastSibling();
            _group.alpha = 1f;
            Follow();
        }

                /// <summary>
        /// Hides the tooltip for the specified owner unless another owner is currently showing it.
        /// </summary>
        public void Hide(object owner)
        {
            if (owner != null && !ReferenceEquals(owner, _owner)) return;
            HideAll();
        }

                /// <summary>
        /// Hides the tooltip completely and clears any owner.
        /// </summary>
        public void HideAll()
        {
            _owner = null;
            if (_group != null) _group.alpha = 0f;
        }

        private void LateUpdate()
        {
            if (_group.alpha > 0f) Follow();
        }

        private void Follow()
        {
            Mouse m = Mouse.current;
            if (m == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, m.position.ReadValue(), null, out Vector2 local)) return;
            Vector2 size = _root.rect.size;
            Rect cr = _canvasRect.rect;
            float x = local.x + 20f, y = local.y - 22f;
            if (x + size.x > cr.xMax - 8f) x = local.x - 16f - size.x;
            if (x < cr.xMin + 8f) x = cr.xMin + 8f;
            if (y - size.y < cr.yMin + 8f) y = cr.yMin + 8f + size.y;
            if (y > cr.yMax - 8f) y = cr.yMax - 8f;
            _root.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// Makes <paramref name="go"/> a hover zone showing <paramref name="text"/> (evaluated on hover). A raycast
        /// target is added when the object has none; <paramref name="highlight"/> tints while hovered.
        /// </summary>
        public HubTooltipTrigger Attach(GameObject go, Func<string> text, Image highlight = null)
        {
            if (go == null) return null;
            if (go.GetComponent<Graphic>() == null)
            {
                var hit = go.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
            }
            var trig = go.GetComponent<HubTooltipTrigger>();
            if (trig == null) trig = go.AddComponent<HubTooltipTrigger>();
            trig.Tip = this;
            trig.Text = text;
            trig.SetHighlight(highlight);
            return trig;
        }
    }

    /// <summary>Reports hover on a row to its screen's <see cref="HubTooltip"/> (and runs optional hover callbacks).</summary>
    public sealed class HubTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public HubTooltip Tip;
        public Func<string> Text;
        public Action Entered, Exited;

        private Image _highlight;
        private Color _base;
        private bool _hover;

                /// <summary>
        /// Sets the optional highlight image for this trigger and records its base colour.
        /// </summary>
        public void SetHighlight(Image img)
        {
            _highlight = img;
            if (img != null) _base = img.color;
        }

                /// <summary>
        /// Called when the pointer enters the UI element; shows the tooltip and invokes any enter callbacks.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _hover = true;
            if (_highlight != null) _highlight.color = Lighten(_base);
            Entered?.Invoke();
            if (Tip != null && Text != null)
            {
                string s = null;
                try { s = Text(); }
                catch (Exception e) { Debug.LogException(e); }
                Tip.Show(this, s);
            }
        }

                /// <summary>
        /// Called when the pointer exits the UI element; hides the tooltip.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData) => Leave();

                /// <summary>
        /// Called when the trigger is disabled; ensures the tooltip is hidden if it was showing.
        /// </summary>
        private void OnDisable()
        {
            if (_hover) Leave();
        }

                /// <summary>
        /// Clears hover state, resets highlight colour, invokes exit callbacks and hides the tooltip.
        /// </summary>
        private void Leave()
        {
            _hover = false;
            if (_highlight != null) _highlight.color = _base;
            Exited?.Invoke();
            if (Tip != null) Tip.Hide(this);
        }

        private static Color Lighten(Color c) => new Color(Mathf.Min(1f, c.r + 0.06f), Mathf.Min(1f, c.g + 0.065f), Mathf.Min(1f, c.b + 0.075f), Mathf.Max(c.a, 0.6f));
    }
}
