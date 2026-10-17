using UnityEngine;
using UnityEngine.EventSystems;

namespace CloneZ
{
    /// <summary>What kind of grid-inventory element a <see cref="GridPointerRelay"/> sits on.</summary>
    public enum GridElement { Tile, EquipSlot, LeftTab, RightTab }

    /// <summary>
    /// Forwards the pointer events of one element of the grid inventory — an item tile, an equipment
    /// slot or a container tab — to its <see cref="InventoryUIController"/>, tagged with the element's
    /// kind and pool index (elements are pooled and rebound on every refresh, so the controller maps the
    /// index to whatever the element shows right now). It also claims the drag events, so pressing a
    /// tile and moving starts an item drag rather than scrolling the list beneath it.
    /// </summary>
    public sealed class GridPointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>The inventory window this element belongs to.</summary>
        public InventoryUIController Owner;
        public GridElement Element;
        /// <summary>The element's slot in its pool (tiles) or its position in its strip (slots, tabs).</summary>
        public int Index;

        public void OnPointerEnter(PointerEventData eventData) { if (Owner != null) Owner.GridPointerEnter(this); }

        public void OnPointerExit(PointerEventData eventData) { if (Owner != null) Owner.GridPointerExit(this); }

        public void OnPointerDown(PointerEventData eventData) { if (Owner != null) Owner.GridPointerDown(this, eventData); }

        public void OnPointerClick(PointerEventData eventData) { if (Owner != null) Owner.GridPointerClick(this, eventData); }

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = true;

        public void OnBeginDrag(PointerEventData eventData) { if (Owner != null) Owner.GridBeginDrag(this, eventData); }

        // The drag itself is followed by the controller from the mouse (it must outlive a pooled tile
        // that gets rebound mid-drag); these two only claim the events from the scroll view.
        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData) { }
    }
}
