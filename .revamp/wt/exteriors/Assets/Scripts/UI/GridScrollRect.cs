using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A <see cref="ScrollRect"/> that scrolls with the mouse wheel and its scrollbar but not by
    /// dragging its content: in the grid inventory a press-and-drag belongs to the items, so an empty
    /// cell dragged by mistake shouldn't fling the whole list about.
    /// </summary>
    public class GridScrollRect : ScrollRect
    {
        public override void OnBeginDrag(PointerEventData eventData) { }

        public override void OnDrag(PointerEventData eventData) { }

        public override void OnEndDrag(PointerEventData eventData) { }
    }
}
