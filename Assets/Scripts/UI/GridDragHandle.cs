using UnityEngine;
using UnityEngine.EventSystems;

namespace CloneZ
{
    /// <summary>
    /// Moves a floating window when its title bar is dragged, or resizes it from its corner grip.
    /// Works in the parent's local space, so it doesn't care about the canvas scale. Pressing it also
    /// raises the window above its siblings. The window must be anchored and pivoted at its top-left.
    /// </summary>
    public sealed class GridDragHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>The window moved or resized.</summary>
        public RectTransform Target;
        /// <summary>True for a resize grip, false for a title bar.</summary>
        public bool Resize;
        /// <summary>The smallest size a resize may shrink the window to.</summary>
        public Vector2 MinSize = new Vector2(260f, 220f);
        /// <summary>Raised after every step (the owner keeps the window on screen).</summary>
        public System.Action Changed;

        private Vector2 _startPointer;
        private Vector2 _startPos;
        private Vector2 _startSize;
        private bool _dragging;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Target != null && eventData.button == PointerEventData.InputButton.Left) Target.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = Target != null && eventData.button == PointerEventData.InputButton.Left
                && ToParent(eventData.position, out _startPointer);
            if (!_dragging) return;
            _startPos = Target.anchoredPosition;
            _startSize = Target.sizeDelta;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || !ToParent(eventData.position, out Vector2 p)) return;
            Vector2 d = p - _startPointer;
            if (Resize)
                Target.sizeDelta = new Vector2(Mathf.Max(MinSize.x, _startSize.x + d.x), Mathf.Max(MinSize.y, _startSize.y - d.y));
            else
                Target.anchoredPosition = _startPos + d;
            Changed?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData) => _dragging = false;

        private bool ToParent(Vector2 screen, out Vector2 local)
        {
            local = Vector2.zero;
            var parent = Target != null ? Target.parent as RectTransform : null;
            return parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out local);
        }
    }
}
