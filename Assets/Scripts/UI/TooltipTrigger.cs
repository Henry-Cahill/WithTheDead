using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Attached to each inventory row; reports pointer enter/exit to the
    /// <see cref="InventoryUIController"/> so it can show a hover tooltip for the item.
    /// Pointer enter/exit bubble from child buttons, so the whole row is a hover zone.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private InventoryUIController _owner;
        private ItemDef _def;
        private Image _row;
        private Color _baseColor;
        private Color _hoverColor;

        public void Init(InventoryUIController owner, ItemDef def, Image row = null)
        {
            _owner = owner;
            _def = def;
            _row = row;
            if (_row != null)
            {
                _baseColor = _row.color;
                _hoverColor = new Color(0.45f, 0.6f, 0.75f, 0.22f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_row != null) _row.color = _hoverColor;
            if (_owner != null && _def != null) _owner.ShowTooltip(_def);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_row != null) _row.color = _baseColor;
            if (_owner != null) _owner.HideTooltip(_def);
        }
    }
}
