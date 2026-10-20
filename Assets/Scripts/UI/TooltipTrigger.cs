using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Attached to each inventory row; reports pointer enter/exit to the
    /// <see cref="InventoryUIController"/> so it can show a hover tooltip for the item, and turns a
    /// left double-click or a right-click on the row into the row's move / context-menu actions.
    /// Pointer enter/exit bubble from child buttons, so the whole row is a hover zone.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float DoubleClickTime = 0.35f;

        private InventoryUIController _owner;
        private ItemDef _def;
        private string _extra;
        private Image _row;
        private Color _baseColor;
        private Color _hoverColor;
        private System.Action _onDoubleClick;
        private System.Action _onRightClick;
        private float _lastLeftClick = -1f;

        /// <param name="extra">Rich-text lines about this particular item, appended to the tooltip.</param>
        /// <param name="onDoubleClick">Run on a left double-click (moves the item across, PZ-style).</param>
        /// <param name="onRightClick">Run on a right-click (opens the row's context menu).</param>
        /// <summary>
        /// Initializes the tooltip trigger with its owner, item definition, optional UI row image, extra tooltip text, and click callbacks.
        /// </summary>
        public void Init(InventoryUIController owner, ItemDef def, Image row = null, string extra = null,
            System.Action onDoubleClick = null, System.Action onRightClick = null)
        {
            _owner = owner;
            _def = def;
            _extra = extra;
            _row = row;
            _onDoubleClick = onDoubleClick;
            _onRightClick = onRightClick;
            if (_row != null)
            {
                _baseColor = _row.color;
                _hoverColor = new Color(0.45f, 0.6f, 0.75f, 0.22f);
            }
        }

        /// <summary>
        /// Handles pointer entering the UI row, applying hover colour and showing the tooltip.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_row != null) _row.color = _hoverColor;
            if (_owner != null && _def != null) _owner.ShowTooltip(_def, _extra);
        }

        /// <summary>
        /// Handles pointer exiting the UI row, restoring the base colour and hiding the tooltip.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_row != null) _row.color = _baseColor;
            if (_owner != null) _owner.HideTooltip(_def);
        }

        /// <summary>
        /// Handles pointer clicks, invoking the appropriate right‑click or double‑click callbacks.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                _onRightClick?.Invoke();
                return;
            }
            if (eventData.button != PointerEventData.InputButton.Left) return;

            // Timed here rather than read from eventData.clickCount, so it behaves the same under any input module.
            float now = Time.unscaledTime;
            if (_lastLeftClick >= 0f && now - _lastLeftClick <= DoubleClickTime)
            {
                _lastLeftClick = -1f;
                _onDoubleClick?.Invoke();
            }
            else _lastLeftClick = now;
        }
    }
}
