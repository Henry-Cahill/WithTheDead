using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CloneZ
{
    // Grid view input: hover tooltips, drag-and-drop (a ghost tile follows the cursor and snaps onto the
    // cells under it over a green / amber / red preview of where it would land; R turns it; Esc or a
    // right-click cancels), double-click / Ctrl+click quick transfers and the right-click context menu.
    // The relays on tiles, slots and tabs (GridPointerRelay) call in here.
    public partial class InventoryUIController
    {
        private const float DoubleClickSeconds = 0.35f;
        private const float AutoScrollBand = 28f;      // a held item near a list's top / bottom edge scrolls it
        private const float AutoScrollSpeed = 480f;

        private static readonly Color GFitAll = new Color(0.3f, 0.85f, 0.4f, 0.4f);
        private static readonly Color GFitPart = new Color(0.95f, 0.75f, 0.25f, 0.4f);
        private static readonly Color GFitNone = new Color(0.95f, 0.3f, 0.25f, 0.4f);

        /// <summary>The item being dragged, where it came from and how it's held.</summary>
        private sealed class DragState
        {
            public bool Active;
            public GridSide From;
            public ItemStack Stack;         // the live stack (pack / container)
            public WorldItem Ground;        // or the ground pile
            public ItemStack Shown;         // what the ghost draws
            public ItemDef Def;
            public int Count;               // units carried: the lot, or half with Shift held
            public bool Rotated;
            public int GrabX, GrabY;        // the footprint cell held under the cursor
        }

        /// <summary>Kinds of drop locations for a dragged item.</summary>
        private enum DropKind { None, Cell, Equip, Window }

        // Why a drop would be refused (the status line explains it on release).
        private enum DropRefusal { None, NoRoom, TooHeavy, StackFull, NotWearable, AlreadyWorn }

        /// <summary>Where a drag would land if released now, and how much of it would fit.</summary>
        private struct DropTarget
        {
            public DropKind Kind;
            public SectionView View;        // Cell
            public int CellX, CellY;        // the cell under the cursor
            public int X, Y;                // where the footprint's top-left would go
            public ItemStack Merge;         // a matching stack under the cursor to top up
            public int Slot;                // Equip: index into _equip
            public GridWindow Window;       // Window: the window under the cursor, off its cells
            public int Fit;                 // 0 none, 1 some, 2 all
            public DropRefusal Why;
        }

        private readonly DragState _drag = new DragState();
        private int _dragEndFrame = -1;
        private float _lastClickTime = -1f;
        private ItemStack _lastClickStack;
        private uint _lastClickGround;
        private int _lastClickSlot = -1;

        #region Pointer events (from GridPointerRelay)
        /// <summary>Handles pointer entering a UI element, showing tooltips or highlights based on drag state.</summary>
        internal void GridPointerEnter(GridPointerRelay r)
        {
            if (r == null || !IsOpen || !_gridMode) return;
            _hover = r;
            switch (r.Element)
            {
                // Handle hover over an inventory tile.
                case GridElement.Tile:
                {
                    TileView t = TileFor(r);
                    if (t == null) return;
                    if (!IsDragSource(t)) t.Bg.color = Lit(t.BaseColor);
                    if (_drag.Active) return;
                    ShowTooltip(t.Shown.Def, TileInfo(t));
                    _tipOwner = r;
                    break;
                }
                // Handle hover over an equipment slot.
                case GridElement.EquipSlot:
                {
                    EquipView ev = EquipFor(r);
                    if (ev == null) return;
                    ev.Bg.color = Lit(ev.BaseColor);
                    if (_drag.Active) return;
                    ShowEquipTooltip(ev);
                    _tipOwner = r;
                    break;
                }
                // Handle hover over a tab or other UI element.
                default:
                {
                    TabView tab = TabFor(r);
                    if (tab == null) return;
                    tab.Bg.color = Lit(tab.BaseColor);
                    if (_drag.Active) return;
                    ShowHint(TabHint(tab));
                    _tipOwner = r;
                    break;
                }
            }
        }

        internal void GridPointerExit(GridPointerRelay r)
        {
            if (r == null) return;
            if (_hover == r) _hover = null;
            switch (r.Element)
            {
                // Handle hover over an inventory tile.
                case GridElement.Tile:
                {
                    TileView t = TileFor(r);
                    if (t != null) t.Bg.color = IsDragSource(t) ? Faded(t.BaseColor) : t.BaseColor;
                    break;
                }
                // Handle hover over an equipment slot.
                case GridElement.EquipSlot:
                {
                    EquipView ev = EquipFor(r);
                    if (ev != null) ev.Bg.color = ev.BaseColor;
                    break;
                }
                // Handle hover over a tab or other UI element.
                default:
                {
                    TabView tab = TabFor(r);
                    if (tab != null) tab.Bg.color = tab.BaseColor;
                    break;
                }
            }
            if (_tipOwner == r)
            {
                _tipOwner = null;
                HideTooltip(null);
            }
        }

        // Right-press opens the context menu (a right-press during a drag cancels the drag instead).
        internal void GridPointerDown(GridPointerRelay r, PointerEventData e)
        {
            if (r == null || !IsOpen || !_gridMode || e.button != PointerEventData.InputButton.Right) return;
            if (_drag.Active || _dragEndFrame == Time.frameCount) return;
            if (r.Element == GridElement.Tile)
            {
                TileView t = TileFor(r);
                if (t != null) ShowTileMenu(t);
            }
            else if (r.Element == GridElement.EquipSlot)
            {
                EquipView ev = EquipFor(r);
                if (ev != null) ShowEquipMenu(ev);
            }
        }

        /// <summary>Handles left‑clicks on UI elements, performing quick‑transfer or navigation actions.</summary>
        internal void GridPointerClick(GridPointerRelay r, PointerEventData e)
        {
            if (r == null || !IsOpen || !_gridMode || _drag.Active || e.button != PointerEventData.InputButton.Left) return;
            if (e.dragging || _dragEndFrame == Time.frameCount) return;   // the release that ended a drag isn't a click
            switch (r.Element)
            {
                // Handle a click on an inventory tile.
                case GridElement.Tile:
                {
                    // Double-click — or Ctrl+click — sends the whole stack to the other window.
                    TileView t = TileFor(r);
                    if (t != null && (CtrlHeld() || IsDoubleClick(t.Stack, t.Ground, -1))) QuickTransfer(t);
                    break;
                }
                // Handle hover over an equipment slot.
                case GridElement.EquipSlot:
                {
                    EquipView ev = EquipFor(r);
                    if (ev != null && IsDoubleClick(null, null, r.Index)) TakeOffSlot(ev.Slot);
                    break;
                }
                case GridElement.LeftTab:
                {
                    TabView tab = TabFor(r);
                    if (tab != null) ScrollToSection(tab.Section);
                    break;
                }
                case GridElement.RightTab:
                {
                    TabView tab = TabFor(r);
                    if (tab == null) break;
                    if (ReferenceEquals(tab.Container, null)) PickFloor();
                    else PickContainer(tab.Container);   // a destroyed container is ignored there
                    break;
                }
            }
        }

        // Pressing a tile and moving past the drag threshold picks the item up (Shift: half the stack).
        internal void GridBeginDrag(GridPointerRelay r, PointerEventData e)
        {
            if (!IsOpen || !_gridMode || _drag.Active || _menu != null || e.button != PointerEventData.InputButton.Left) return;
            TileView t = TileFor(r);
            if (t == null) return;
            if (t.Ground != null ? !t.Ground.StillThere : !Holds(t.View.Inventory, t.Stack)) return;

            int total = t.Shown.Count;
            _drag.From = t.View.Side;
            _drag.Stack = t.Stack;
            _drag.Ground = t.Ground;
            _drag.Shown = t.Shown;
            _drag.Def = t.Shown.Def;
            _drag.Count = total > 1 && ShiftHeld() ? total / 2 : total;
            _drag.Rotated = t.Rotated;

            // The cell of the item the cursor took hold of stays under the cursor (the tile sits 1 px into its cell).
            RectTransformUtility.ScreenPointToLocalPointInRectangle(t.Rt, e.pressPosition, null, out Vector2 local);
            _drag.GrabX = Mathf.Clamp(Mathf.FloorToInt((local.x + 1f) / Cell), 0, t.W - 1);
            _drag.GrabY = Mathf.Clamp(Mathf.FloorToInt((1f - local.y) / Cell), 0, t.H - 1);
            _drag.Active = true;

            HideTooltip(null);
            _tipOwner = null;
            t.Bg.color = Faded(t.BaseColor);
            PaintGhost();
            _ghost.Rt.gameObject.SetActive(true);
            _ghost.Rt.SetAsLastSibling();
            ShowDropPreview(e.position);
        }

        private TileView TileFor(GridPointerRelay r)
        {
            if (r == null || r.Element != GridElement.Tile || r.Index < 0 || r.Index >= _tilesUsed) return null;
            TileView t = _tiles[r.Index];
            return t.Relay == r && t.Shown != null && t.View != null ? t : null;
        }

        private EquipView EquipFor(GridPointerRelay r) =>
            r != null && r.Element == GridElement.EquipSlot && _equip != null && r.Index >= 0 && r.Index < _equip.Length ? _equip[r.Index] : null;

        private TabView TabFor(GridPointerRelay r)
        {
            if (r == null) return null;
            GridWindow w = r.Element == GridElement.LeftTab ? _leftWin : r.Element == GridElement.RightTab ? _rightWin : null;
            return w != null && r.Index >= 0 && r.Index < w.TabsUsed ? w.Tabs[r.Index] : null;
        }

        private bool IsDoubleClick(ItemStack stack, WorldItem ground, int slot)
        {
            float now = Time.unscaledTime;
            uint gid = ground != null ? ground.Handle.Id : 0u;
            bool same = _lastClickTime >= 0f && now - _lastClickTime <= DoubleClickSeconds
                && _lastClickStack == stack && _lastClickGround == gid && _lastClickSlot == slot;
            if (same)
            {
                _lastClickTime = -1f;
                return true;
            }
            _lastClickTime = now;
            _lastClickStack = stack;
            _lastClickGround = gid;
            _lastClickSlot = slot;
            return false;
        }

        private static bool CtrlHeld() => Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
        private static bool ShiftHeld() => Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        #endregion

        #region Dragging
        // Runs every frame while an item is held: R turns it, a right-press cancels, releasing drops it.
        private void UpdateDrag()
        {
            Mouse m = Mouse.current;
            if (m == null || !DragStillValid()) { CancelDrag(); return; }
            if (m.rightButton.wasPressedThisFrame)
            {
                CancelDrag();
                _dragEndFrame = Time.frameCount;   // so the same press doesn't also open a context menu
                return;
            }
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) RotateDrag();

            Vector2 screen = m.position.ReadValue();
            if (m.leftButton.wasReleasedThisFrame || !m.leftButton.isPressed)
            {
                CompleteDrag(screen);
                return;
            }
            AutoScroll(_leftWin, screen);
            AutoScroll(_rightWin, screen);
            ShowDropPreview(screen);
        }

        /// <summary>Lets go of a held item without moving it (Esc, a right-click, the window closing).</summary>
        private void CancelDrag()
        {
            if (!_drag.Active) return;
            EndDrag();
            MarkDirty();   // un-fades the source tile
        }

        /// <summary>Ends a drag operation without moving the item (Esc, right‑click, or window close).</summary>
        private void EndDrag()
        {
            _drag.Active = false;
            _drag.Stack = null;
            _drag.Ground = null;
            _drag.Shown = null;
            _drag.Def = null;
            if (_ghost != null && _ghost.Rt.gameObject.activeSelf) _ghost.Rt.gameObject.SetActive(false);
            HidePreview();
        }

        // The held item must still be where it was picked up (an eaten apple, a pile someone else took…).
        private bool DragStillValid()
        {
            switch (_drag.From)
            {
                case GridSide.Player:
                    if (_player == null || !Holds(_player.Inventory, _drag.Stack)) return false;
                    _drag.Count = Mathf.Min(_drag.Count, _drag.Stack.Count);
                    return true;
                case GridSide.Container:
                    if (!Holds(_openInventory, _drag.Stack)) return false;
                    _drag.Count = Mathf.Min(_drag.Count, _drag.Stack.Count);
                    return true;
                // Handle hover over a tab or other UI element.
                default:
                    return _drag.Ground != null && _drag.Ground.StillThere && _drag.Ground.Count > 0;
            }
        }

        private bool IsDragSource(TileView t)
        {
            if (!_drag.Active || t == null) return false;
            if (_drag.Stack != null) return t.Stack == _drag.Stack;
            return _drag.Ground != null && t.Ground != null && t.Ground.Handle.Id == _drag.Ground.Handle.Id;
        }

        // Turns the held item 90°; the cell under the cursor turns with it.
        private void RotateDrag()
        {
            Vector2Int b = InventoryLayout.BaseFootprint(_drag.Def);
            if (b.x == b.y) return;
            _drag.Rotated = !_drag.Rotated;
            int g = _drag.GrabX;
            _drag.GrabX = _drag.GrabY;
            _drag.GrabY = g;
            Vector2Int f = InventoryLayout.Footprint(_drag.Def, _drag.Rotated);
            _drag.GrabX = Mathf.Clamp(_drag.GrabX, 0, f.x - 1);
            _drag.GrabY = Mathf.Clamp(_drag.GrabY, 0, f.y - 1);
            PaintGhost();
        }

        private void PaintGhost()
        {
            Vector2Int f = InventoryLayout.Footprint(_drag.Def, _drag.Rotated);
            _ghost.Shown = _drag.Shown;
            _ghost.W = f.x;
            _ghost.H = f.y;
            _ghost.Rotated = _drag.Rotated;
            _ghost.InHand = _drag.Stack != null && _player != null && _player.EquippedWeapon != null && _drag.Stack == _player.EquippedStack;
            _ghost.Rt.sizeDelta = new Vector2(f.x * Cell - 1f, f.y * Cell - 1f);
            PaintTile(_ghost);
            _ghost.Count.text = _drag.Count > 1 ? CountText(_drag.Count) : "";
            Color c = _ghost.BaseColor;
            c.a = 0.9f;
            _ghost.Bg.color = c;
        }

        // Over cells the ghost snaps onto them above a preview (green: all of it fits, amber: some of it,
        // red: none); anywhere else it simply follows the cursor.
        /// <summary>Shows a preview of where the dragged item would land, with colour indicating fit.</summary>
        private void ShowDropPreview(Vector2 screen)
        {
            DropTarget t = ResolveDrop(screen);
            bool cell = false, wear = false;
            // If the cursor is over a grid cell, calculate preview rectangle.
            if (t.Kind == DropKind.Cell)
            {
                SectionView v = t.View;
                // Determine the footprint of the dragged item (or merged stack) to size the preview.
                Vector2Int f = InventoryLayout.Footprint(_drag.Def, _drag.Rotated);
                int px = t.X, py = t.Y, pw = f.x, ph = f.y;
                // If merging onto an existing stack, use its footprint instead of the dragged item's.
                if (t.Merge != null)
                {
                    Vector2Int mf = InventoryLayout.Footprint(t.Merge);
                    px = t.Merge.GridX;
                    py = t.Merge.GridY;
                    pw = mf.x;
                    ph = mf.y;
                }
                // Show the cell preview and position the ghost at the cell's top‑left corner.
                cell = ShowCellPreview(v, px, py, pw, ph, t.Fit);
                // Position the ghost visual under the cursor when not over a cell.
                _ghost.Rt.position = v.Grid.TransformPoint(new Vector3(px * Cell + 1f, -(py * Cell + 1f), 0f));
            }
            else
            {
                // If the cursor is over an equipment slot, show the equip preview.
                if (t.Kind == DropKind.Equip) wear = ShowEquipPreview(t);
                // Convert screen position to local UI coordinates for the ghost when not over a cell or equip.
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_gridRootRt, screen, null, out Vector2 local))
                    _ghost.Rt.anchoredPosition = local + new Vector2(-(_drag.GrabX * Cell + Cell * 0.5f), _drag.GrabY * Cell + Cell * 0.5f);
            }
            // Hide the cell preview if not currently showing a valid cell preview.
            if (!cell && _preview.gameObject.activeSelf) _preview.gameObject.SetActive(false);
            // Hide the equip preview if not currently showing a valid equip preview.
            if (!wear && _equipPreview.gameObject.activeSelf) _equipPreview.gameObject.SetActive(false);
        }

        /// <summary>Shows a cell‑level preview rectangle while dragging.</summary>
        private bool ShowCellPreview(SectionView v, int x, int y, int w, int h, int fit)
        {
            // Clamped to the drawn cells (a growable grid simply grows once the item lands).
            int x0 = Mathf.Max(0, x), y0 = Mathf.Max(0, y);
            int x1 = Mathf.Min(v.Cols, x + w), y1 = Mathf.Min(v.Rows, y + h);
            if (x1 <= x0 || y1 <= y0) return false;
            if (_preview.parent != v.Grid) _preview.SetParent(v.Grid, false);
            if (_preview.GetSiblingIndex() != v.Grid.childCount - 1) _preview.SetAsLastSibling();
            _preview.anchoredPosition = new Vector2(x0 * Cell + 1f, -(y0 * Cell + 1f));
            _preview.sizeDelta = new Vector2((x1 - x0) * Cell - 1f, (y1 - y0) * Cell - 1f);
            _previewImg.color = fit >= 2 ? GFitAll : fit == 1 ? GFitPart : GFitNone;
            if (!_preview.gameObject.activeSelf) _preview.gameObject.SetActive(true);
            return true;
        }

        // Clothing lights up the slot it will actually go on; anything else reds the slot under the cursor.
        private bool ShowEquipPreview(DropTarget t)
        {
            if (_equip == null) return false;
            int slot = t.Slot;
            if (t.Fit > 0)
                for (int i = 0; i < _equip.Length; i++)
                    if (_equip[i].Slot == _drag.Def.ClothingSlot) { slot = i; break; }
            if (slot < 0 || slot >= _equip.Length) return false;
            RectTransform target = _equip[slot].Rt;
            if (_equipPreview.parent != target) _equipPreview.SetParent(target, false);
            if (_equipPreview.GetSiblingIndex() != target.childCount - 1) _equipPreview.SetAsLastSibling();
            _equipPreviewImg.color = t.Fit > 0 ? GFitAll : GFitNone;
            if (!_equipPreview.gameObject.activeSelf) _equipPreview.gameObject.SetActive(true);
            return true;
        }

        private void HidePreview()
        {
            if (_preview != null && _preview.gameObject.activeSelf) _preview.gameObject.SetActive(false);
            if (_equipPreview != null && _equipPreview.gameObject.activeSelf) _equipPreview.gameObject.SetActive(false);
        }

        // A held item near the top or bottom edge of a window's list scrolls it.
        private static void AutoScroll(GridWindow w, Vector2 screen)
        {
            if (w == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(w.Viewport, screen, null, out Vector2 local)) return;
            Rect r = w.Viewport.rect;
            if (local.x < r.xMin || local.x > r.xMax) return;
            float dir = 0f;
            if (local.y > r.yMax - AutoScrollBand && local.y < r.yMax + 6f) dir = -1f;
            else if (local.y < r.yMin + AutoScrollBand && local.y > r.yMin - 6f) dir = 1f;
            if (dir == 0f) return;
            float max = Mathf.Max(0f, w.Content.rect.height - r.height);
            if (max <= 0f) return;
            Vector2 p = w.Content.anchoredPosition;
            p.y = Mathf.Clamp(p.y + dir * AutoScrollSpeed * Time.unscaledDeltaTime, 0f, max);
            w.Content.anchoredPosition = p;
        }
        #endregion

        #region Dropping
        // What's under the cursor: a worn-slot tile, a cell of a section, or just a window's panel.
        /// <summary>Determines what is under the cursor (cell, equip slot or window) and computes drop feasibility.</summary>
        private DropTarget ResolveDrop(Vector2 screen)
        {
            var t = new DropTarget { Slot = -1 };
            GridWindow win = WindowAt(screen);
            if (win == null || _drag.Def == null) return t;

            if (win.Left && _equip != null)
                for (int i = 0; i < _equip.Length; i++)
                    if (RectTransformUtility.RectangleContainsScreenPoint(_equip[i].Rt, screen, null))
                    {
                        t.Kind = DropKind.Equip;
                        t.Slot = i;
                        t.Why = !_drag.Def.IsClothing ? DropRefusal.NotWearable : WornAlready(_drag.Def) ? DropRefusal.AlreadyWorn : DropRefusal.None;
                        t.Fit = t.Why == DropRefusal.None ? 2 : 0;
                        return t;
                    }

            if (RectTransformUtility.RectangleContainsScreenPoint(win.Viewport, screen, null))
                for (int i = 0; i < win.SectionsUsed; i++)
                {
                    SectionView v = win.Sections[i];
                    if (v.Collapsed || !CellAt(v, screen, out int cx, out int cy)) continue;
                    t.Kind = DropKind.Cell;
                    t.View = v;
                    t.CellX = cx;
                    t.CellY = cy;
                    EvaluateCell(ref t);
                    return t;
                }

            t.Kind = DropKind.Window;
            t.Window = win;
            return t;
        }

        // The window under a screen point — the one in front, should they overlap.
        private GridWindow WindowAt(Vector2 screen)
        {
            if (_leftWin == null || _rightWin == null) return null;
            bool inLeft = RectTransformUtility.RectangleContainsScreenPoint(_leftWin.Root, screen, null);
            bool inRight = RectTransformUtility.RectangleContainsScreenPoint(_rightWin.Root, screen, null);
            if (inLeft && inRight) return _leftWin.Root.GetSiblingIndex() > _rightWin.Root.GetSiblingIndex() ? _leftWin : _rightWin;
            return inLeft ? _leftWin : inRight ? _rightWin : null;
        }

        // The drawn cell of a section under a screen point.
        private static bool CellAt(SectionView v, Vector2 screen, out int cx, out int cy)
        {
            cx = cy = -1;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(v.Grid, screen, null, out Vector2 local)) return false;
            float x = local.x, y = -local.y;   // the grid's origin is its top-left corner
            if (x < 0f || y < 0f) return false;
            cx = Mathf.FloorToInt(x / Cell);
            cy = Mathf.FloorToInt(y / Cell);
            return cx < v.Cols && cy < v.Rows;
        }

        // Would the held item fit at this cell, and how much of it? Mirrors what the inventory model will
        // accept on release: a move / split within the same grid, a transfer from the other window (the pack
        // minds weight; a container doesn't), or topping up a matching stack under the cursor.
        /// <summary>Evaluates whether the dragged item fits at the candidate cell and how much of it can be placed.</summary>
        private void EvaluateCell(ref DropTarget t)
        {
            SectionView v = t.View;
            ItemDef def = _drag.Def;
            t.X = t.CellX - _drag.GrabX;
            t.Y = t.CellY - _drag.GrabY;
            t.Fit = 0;
            t.Why = DropRefusal.NoRoom;

            if (v.Side == GridSide.Ground)
            {
                // Anything from the pack can go down; the ground packs itself, so the exact cell is moot.
                if (_drag.From == GridSide.Player) { t.Fit = 2; t.Why = DropRefusal.None; }
                return;
            }

            InventoryLayout layout = v.Inventory != null ? v.Inventory.Layout : null;
            GridSection s = v.Section;
            if (layout == null || s == null || _drag.From == GridSide.Ground && v.Side != GridSide.Player) return;
            bool same = _drag.From == v.Side;           // within the pack, or within the container
            bool weigh = v.Side == GridSide.Player;

            ItemStack under = layout.StackAt(s, t.CellX, t.CellY);
            if (under != null && under != _drag.Stack)
            {
                if (under.Def != def) return;
                if (under.Count >= def.MaxStack) { t.Why = DropRefusal.StackFull; return; }
                t.Merge = under;
                int n = MergeRoom(v, under);
                t.Fit = n <= 0 ? 0 : n >= _drag.Count ? 2 : 1;
                t.Why = t.Fit > 0 ? DropRefusal.None : DropRefusal.TooHeavy;
                return;
            }

            if (same)
            {
                bool fits;
                if (_drag.Count >= _drag.Stack.Count)
                {
                    fits = layout.Fits(_drag.Stack, s, t.X, t.Y, _drag.Rotated, false);
                    if (fits && weigh && !layout.Fits(_drag.Stack, s, t.X, t.Y, _drag.Rotated, true)) { t.Why = DropRefusal.TooHeavy; return; }
                }
                else
                {
                    fits = layout.FitsNew(def, 0f, s, t.X, t.Y, _drag.Rotated, false);
                    if (fits && !ReferenceEquals(_drag.Stack.Section, s) && !layout.HasWeightRoom(s, def.Weight * _drag.Count)) { t.Why = DropRefusal.TooHeavy; return; }
                }
                if (fits) { t.Fit = 2; t.Why = DropRefusal.None; }
                return;
            }

            if (!layout.FitsNew(def, 0f, s, t.X, t.Y, _drag.Rotated, false)) return;
            int room = Mathf.Min(_drag.Count, Mathf.Max(1, def.MaxStack));
            if (weigh) room = Mathf.Min(room, UnitsByWeight(v.Inventory, s, def));
            t.Fit = room <= 0 ? 0 : room >= _drag.Count ? 2 : 1;
            t.Why = t.Fit > 0 ? DropRefusal.None : DropRefusal.TooHeavy;
        }

        // How many held units a matching stack can take: its free stack room, and — in the pack — the weight
        // left in that stack's section (and overall, when the units come from outside the pack).
        private int MergeRoom(SectionView v, ItemStack under)
        {
            ItemDef def = under.Def;
            int n = Mathf.Min(_drag.Count, def.MaxStack - under.Count);
            if (v.Side != GridSide.Player || def.Weight <= 0f || v.Inventory == null || v.Inventory.Layout == null) return n;

            bool within = _drag.From == GridSide.Player;
            bool capped = false;
            float room = 0f;
            if (!within)
            {
                capped = true;
                room = v.Inventory.MaxWeight - v.Inventory.CurrentWeight;
            }
            GridSection s = under.Section;
            if (s != null && !s.Unlimited && !(within && ReferenceEquals(_drag.Stack.Section, s)))
            {
                float sectionRoom = s.Capacity - v.Inventory.Layout.WeightIn(s);
                room = capped ? Mathf.Min(room, sectionRoom) : sectionRoom;
                capped = true;
            }
            if (!capped) return n;
            return Mathf.Max(0, Mathf.Min(n, Mathf.FloorToInt((room + 1e-4f) / def.Weight)));
        }

        // Units of an item a section of an arranged inventory can still take by weight (as Inventory.AddAt reckons).
        private static int UnitsByWeight(Inventory inv, GridSection s, ItemDef def)
        {
            if (def.Weight <= 0f) return int.MaxValue;
            float room = inv.MaxWeight - inv.CurrentWeight;
            if (!s.Unlimited && inv.Layout != null) room = Mathf.Min(room, s.Capacity - inv.Layout.WeightIn(s));
            return Mathf.Max(0, Mathf.FloorToInt((room + 1e-4f) / def.Weight));
        }

        private void CompleteDrag(Vector2 screen)
        {
            DropTarget t = ResolveDrop(screen);
            if (DragStillValid())
            {
                switch (t.Kind)
                {
                    case DropKind.Cell: DropOnCell(t); break;
                    case DropKind.Equip:
                        if (t.Fit > 0) WearDragged();
                        else GridStatus(_leftWin, RefusalText(t));
                        break;
                    case DropKind.Window: DropOnWindow(t.Window); break;
                }
            }
            EndDrag();
            _dragEndFrame = Time.frameCount;
            MarkDirty();
        }

        /// <summary>Handles dropping an item onto a grid cell, including merging, moving or splitting.</summary>
        private void DropOnCell(DropTarget t)
        {
            SectionView v = t.View;
            if (v.Side == GridSide.Ground)
            {
                if (_drag.From != GridSide.Player) return;   // ground to ground: it only re-sorts
                _player.Drop(_drag.Stack, _drag.Count);
                KeepHandsHonest();
                return;
            }
            if (t.Fit <= 0)
            {
                if (!(_drag.From == v.Side && t.X == _drag.Stack.GridX && t.Y == _drag.Stack.GridY)) GridStatus(v.Window, RefusalText(t));
                return;
            }

            GridSection s = v.Section;
            Inventory dest = v.Inventory;
            bool weigh = v.Side == GridSide.Player;
            // Topping up a stack aims at the cell under the cursor; a placement at the footprint's top-left.
            int cx = t.Merge != null ? t.CellX : t.X;
            int cy = t.Merge != null ? t.CellY : t.Y;

            if (_drag.From == GridSide.Ground)
            {
                if (_player.PickUpAt(_drag.Ground, _drag.Count, s, cx, cy, _drag.Rotated) <= 0) GridStatus(v.Window, RefusalText(t));
                return;
            }

            Inventory src = _drag.From == GridSide.Player ? _player.Inventory : _openInventory;
            if (src == dest)
            {
                // If merging onto an existing stack, use its footprint instead of the dragged item's.
                if (t.Merge != null) dest.MergeInto(t.Merge, dest, _drag.Stack, MergeRoom(v, t.Merge), enforceWeight: false);
                else if (_drag.Count >= _drag.Stack.Count) dest.MoveTo(_drag.Stack, s, t.X, t.Y, _drag.Rotated, weigh);
                else dest.SplitTo(_drag.Stack, _drag.Count, s, t.X, t.Y, _drag.Rotated);
                return;
            }

            if (src.TransferToCell(dest, _drag.Stack, _drag.Count, s, cx, cy, _drag.Rotated, weigh) <= 0) GridStatus(v.Window, RefusalText(t));
            if (src == _player.Inventory) KeepHandsHonest();
        }

        // Dropped on the other window's panel (off its cells): the item goes across wherever it fits.
        private void DropOnWindow(GridWindow win)
        {
            if (win == null) return;
            switch (_drag.From)
            {
                case GridSide.Player:
                    if (!win.Left) SendFromPack(_drag.Stack, _drag.Count);
                    break;
                case GridSide.Container:
                    if (win.Left) TakeIntoPack(_drag.Stack, _drag.Count);
                    break;
                // Handle hover over a tab or other UI element.
                default:
                    if (win.Left && _player.PickUp(_drag.Ground, _drag.Count) <= 0) GridStatus(_leftWin, "Too heavy \u2014 no room in the pack");
                    break;
            }
        }

        private void WearDragged()
        {
            switch (_drag.From)
            {
                case GridSide.Player: WearFromPack(_drag.Stack); break;
                case GridSide.Container: WearFromContainer(_drag.Stack); break;
                // Handle hover over a tab or other UI element.
                default: WearFromGround(_drag.Ground); break;
            }
        }

        // Uses a pack stack, except that a second copy of the garment already on its slot is refused:
        // PlayerClothing.Equip ignores an identical garment, yet PlayerInventory.WearFromStack still lets
        // go of the stack, so the copy would simply vanish.
        private void WearFromPack(ItemStack s)
        {
            if (s != null && !RefuseWornAlready(s.Def)) UseStack(s);
        }

        private bool WornAlready(ItemDef d) =>
            d != null && d.IsClothing && _clothing != null && _clothing.GetWorn(d.ClothingSlot) == d;

        private bool RefuseWornAlready(ItemDef d)
        {
            if (!WornAlready(d)) return false;
            GridStatus(_leftWin, "Already wearing " + d.DisplayName);
            return true;
        }

        /// <summary>Formats the reason a drop was rejected for display in the status line.</summary>
        private string RefusalText(DropTarget t)
        {
            switch (t.Why)
            {
                case DropRefusal.TooHeavy:
                    return t.View != null && t.View.Section != null ? "Too heavy for " + t.View.Section.Name : "Too heavy";
                case DropRefusal.StackFull: return "That stack is full";
                case DropRefusal.NotWearable: return "That can't be worn";
                case DropRefusal.AlreadyWorn: return "Already wearing one";
                // Handle hover over a tab or other UI element.
                default: return "No room there";
            }
        }
        #endregion

        #region Quick transfers and wearing
        // Double-click / Ctrl+click: the whole stack to the other window.
        private void QuickTransfer(TileView t)
        {
            switch (t.View.Side)
            {
                case GridSide.Player:
                    if (Holds(_player.Inventory, t.Stack)) SendFromPack(t.Stack, t.Stack.Count);
                    break;
                case GridSide.Container:
                    if (Holds(_openInventory, t.Stack)) TakeIntoPack(t.Stack, t.Stack.Count);
                    break;
                // Handle hover over a tab or other UI element.
                default:
                    if (t.Ground != null && t.Ground.StillThere && _player.PickUp(t.Ground) <= 0)
                        GridStatus(_leftWin, "Too heavy \u2014 no room in the pack");
                    MarkDirty();
                    break;
            }
        }

        // From the pack to the loot window: into the open container (wherever it fits), or onto the ground.
        private void SendFromPack(ItemStack stack, int count)
        {
            if (!Holds(_player.Inventory, stack)) return;
            if (_openInventory != null) _player.Inventory.TransferTo(_openInventory, stack, count, enforceWeight: false);
            else _player.Drop(stack, Mathf.Min(count, stack.Count));
            KeepHandsHonest();
            MarkDirty();
        }

        // From the open container into the pack, as far as weight and space allow.
        private void TakeIntoPack(ItemStack stack, int count)
        {
            if (!Holds(_openInventory, stack)) return;
            int moved = _openInventory.TransferTo(_player.Inventory, stack, count, enforceWeight: true);
            if (moved < count) GridStatus(_leftWin, moved == 0 ? "Too heavy \u2014 no room in the pack" : "Only " + moved + " of " + count + " fit in the pack");
            MarkDirty();
        }

        // Clothing goes straight from a container or the floor onto the body: through the pack (forced —
        // it's only passing through) and worn from there, so the usual swap hands back what it replaces.
        private void WearFromContainer(ItemStack s)
        {
            if (!Holds(_openInventory, s) || !s.Def.IsClothing || RefuseWornAlready(s.Def)) return;
            ItemStack passing = PassThroughPack(s.Def, s.SpawnHours, s.Def.IsDamageable ? s.Condition : -1, s.TimesRepaired, s.Extra);
            if (passing == null) return;
            _openInventory.Remove(s, 1);
            _player.Use(passing);
            MarkDirty();
        }

        /// <summary>Handles dropping a clothing item from the ground onto the player, passing through the pack.</summary>
        private void WearFromGround(WorldItem w)
        {
            if (w == null || w.Def == null || !w.Def.IsClothing || !w.StillThere || RefuseWornAlready(w.Def)) return;
            ItemStack passing = PassThroughPack(w.Def, w.SpawnHours, w.Def.IsDamageable ? w.Condition : -1, w.TimesRepaired, w.Extra);
            if (passing == null) return;
            w.Take(1);
            _player.Use(passing);
            MarkDirty();
        }

        /// <summary>Creates a temporary stack in the pack to wear a garment, ensuring it passes through inventory weight checks.</summary>
        private ItemStack PassThroughPack(ItemDef def, float spawnHours, int condition, int timesRepaired, ItemExtra extra)
        {
            List<ItemStack> stacks = _player.Inventory.Stacks;
            int before = stacks.Count;
            if (_player.Inventory.Add(def, 1, enforceWeight: false, spawnHours: spawnHours, condition: condition, timesRepaired: timesRepaired, extra: extra) <= 0)
                return null;
            if (stacks.Count > before) return stacks[stacks.Count - 1];   // a new stack lands at the end
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Def == def) return stacks[i];
            return null;
        }

        // "Split": half the stack into the first free spot — its own section first, then any with room.
        private void SplitHalf(Inventory inv, ItemStack s)
        {
            if (!Holds(inv, s) || s.Count < 2 || inv.Layout == null) return;
            int half = s.Count / 2;
            InventoryLayout layout = inv.Layout;
            bool done = layout.TryFindSpot(s.Def, 0f, false, out GridSection sec, out int x, out int y, out bool rot, s.Section)
                && inv.SplitTo(s, half, sec, x, y, rot) != null;
            if (!done)
                done = layout.TryFindSpot(s.Def, s.Def.Weight * half, true, out sec, out x, out y, out rot)
                    && inv.SplitTo(s, half, sec, x, y, rot) != null;
            if (!done) GridStatus(inv == _player.Inventory ? _leftWin : _rightWin, "No free spot to split it into");
            MarkDirty();
        }

        private void RotateInPlace(Inventory inv, ItemStack s)
        {
            if (Holds(inv, s) && s.IsPlaced) inv.MoveTo(s, s.Section, s.GridX, s.GridY, !s.Rotated, enforceWeight: false);
            MarkDirty();
        }
        #endregion

        #region Context menus and tooltips
        private void ShowTileMenu(TileView t)
        {
            var options = new List<MenuOption>(12);
            ItemDef d = t.Shown.Def;
            switch (t.View.Side)
            {
                case GridSide.Player: PackMenu(options, t.Stack, t.InHand); break;
                case GridSide.Container: ContainerMenu(options, t.Stack); break;
                // Handle hover over a tab or other UI element.
                default: GroundMenu(options, t.Ground); break;
            }
            int n = t.Shown.Count;
            ShowMenu(d.DisplayName + (n > 1 ? "  (" + n + ")" : ""), options);
        }

        // The pack's verbs, as in the list view, with Eat / Drink spelled out, plus Split and Rotate.
        private void PackMenu(List<MenuOption> options, ItemStack s, bool inHand)
        {
            ItemDef d = s.Def;
            if (inHand) options.Add(new MenuOption("Unequip", Unequip));
            else if (d.IsClothing) options.Add(new MenuOption("Wear", () => WearFromPack(s)));
            else if (d.IsUsable) options.Add(new MenuOption(GridUseVerb(d), () => UseStack(s)));
            if (_player.CanRepair(s)) options.Add(new MenuOption("Repair", () => RepairStack(s)));
            if (d.Cookable) options.Add(new MenuOption(_nearStove ? "Cook" : "Cook  (needs a stove)", () => CookStack(s), _nearStove));
            if (!string.IsNullOrEmpty(d.PurifiedId)) options.Add(new MenuOption(_nearStove ? "Boil" : "Boil  (needs a stove)", () => BoilStack(s), _nearStove));
            if (_openInventory != null)
            {
                options.Add(new MenuOption(s.Count > 1 ? "Store all " + s.Count : "Store", () => SendFromPack(s, s.Count)));
                if (s.Count > 1) options.Add(new MenuOption("Store 1", () => SendFromPack(s, 1)));
            }
            options.Add(new MenuOption(s.Count > 1 ? "Drop all " + s.Count : "Drop", () => DropStack(s, s.Count)));
            if (s.Count > 1) options.Add(new MenuOption("Drop 1", () => DropStack(s, 1)));
            AddSplitRotate(options, _player.Inventory, s);
            AddFeatureVerbs(options, s);
        }

        // Verbs contributed by feature modules (attach a mod, strip a gun, insure, throw a bolt...).
        private readonly List<ItemMenuAction> _featureVerbs = new List<ItemMenuAction>();
        private void AddFeatureVerbs(List<MenuOption> options, ItemStack s)
        {
            ItemActions.Gather(_player, s, _featureVerbs);
            for (int i = 0; i < _featureVerbs.Count; i++)
            {
                ItemMenuAction a = _featureVerbs[i];
                System.Action run = a.Run;
                options.Add(new MenuOption(a.Label, run == null ? null : (System.Action)(() => { run(); MarkDirty(); }), a.Enabled));
            }
        }

        private void ContainerMenu(List<MenuOption> options, ItemStack s)
        {
            options.Add(new MenuOption(s.Count > 1 ? "Take all " + s.Count : "Take", () => TakeIntoPack(s, s.Count)));
            if (s.Count > 1) options.Add(new MenuOption("Take 1", () => TakeIntoPack(s, 1)));
            if (s.Def.IsClothing) options.Add(new MenuOption("Wear", () => WearFromContainer(s)));
            AddSplitRotate(options, _openInventory, s);
        }

        private void GroundMenu(List<MenuOption> options, WorldItem w)
        {
            options.Add(new MenuOption(w.Count > 1 ? "Take all " + w.Count : "Take", () => TakeGround(w, int.MaxValue)));
            if (w.Count > 1) options.Add(new MenuOption("Take 1", () => TakeGround(w, 1)));
            if (w.Def.IsClothing) options.Add(new MenuOption("Wear", () => WearFromGround(w)));
        }

        private void TakeGround(WorldItem w, int max)
        {
            if (w == null || !w.StillThere) return;
            if (_player.PickUp(w, max) <= 0) GridStatus(_leftWin, "Too heavy \u2014 no room in the pack");
            MarkDirty();
        }

        private void AddSplitRotate(List<MenuOption> options, Inventory inv, ItemStack s)
        {
            if (inv == null || inv.Layout == null) return;
            if (s.Count > 1) options.Add(new MenuOption("Split", () => SplitHalf(inv, s)));
            Vector2Int b = InventoryLayout.BaseFootprint(s.Def);
            if (b.x != b.y && s.IsPlaced)
                options.Add(new MenuOption("Rotate", () => RotateInPlace(inv, s),
                    inv.Layout.Fits(s, s.Section, s.GridX, s.GridY, !s.Rotated, false)));
        }

        private static string GridUseVerb(ItemDef d)
        {
            if (d.CustomUse && !string.IsNullOrEmpty(d.UseVerb)) return d.UseVerb;
            if (d.IsClothing) return "Wear";
            if (d.IsEquippable) return "Equip";
            if (d.IsCanned) return "Open & eat";
            if (d.Category == ItemCategory.Drink || d.HoldsWater) return "Drink";
            if (d.Category == ItemCategory.Food) return "Eat";
            return "Use";
        }

        private void ShowEquipMenu(EquipView ev)
        {
            PlayerClothing.WornPiece p = _clothing != null ? _clothing.GetPiece(ev.Slot) : default;
            if (!p.Exists) return;
            ClothingSlot slot = ev.Slot;
            var options = new List<MenuOption>(2) { new MenuOption("Take off", () => TakeOffSlot(slot)) };
            if (p.Def.Gear == GearKind.NightVision && PlayerGear.Instance != null)
                options.Add(new MenuOption("Switch night vision on / off  (N)", () => { PlayerGear.Instance.ToggleNightVision(); MarkDirty(); }));
            ShowMenu(p.Def.DisplayName, options);
        }

        // Lines about the particular item hovered, under the catalogue entry every item of its kind shares.
        private string TileInfo(TileView t)
        {
            ItemStack s = t.Shown;
            ItemDef d = s.Def;
            var sb = new System.Text.StringBuilder(240);
            if (t.Ground != null) InfoLine(sb, "Status", "On the ground");
            else if (t.InHand) InfoLine(sb, "Status", "In hand");
            if (s.Count > 1) InfoLine(sb, "Stack", "\u00d7" + s.Count + "  \u00b7  " + Kg(s.Weight) + " kg");
            if (d.UsesCharge) InfoLine(sb, d.Gear == GearKind.GasMask ? "Filter" : "Battery", Pct(s.Charge01));
            else if (d.IsDamageable)
                InfoLine(sb, "Wear", ItemConditionTiers.Label(s.Condition01) + "  \u00b7  " + Pct(s.Condition01) + "  (" + s.Condition + " / " + d.MaxCondition + ")"
                    + (s.TimesRepaired > 0 ? "  \u00b7  repaired \u00d7" + s.TimesRepaired : ""));
            if (d.Perishable)
                InfoLine(sb, "Freshness", s.State + "  \u00b7  " + Mathf.RoundToInt(s.AgeHours) + "h old of ~" + Mathf.RoundToInt(d.ShelfLifeHours) + "h");
            InfoLine(sb, "Size", t.W + "\u00d7" + t.H + (t.Rotated ? "  (turned)" : ""));
            sb.Append("\n<size=12><color=#8b93a0>Drag to move \u00b7 R turns it while held \u00b7 Shift-drag takes half \u00b7 double-click sends it across \u00b7 right-click for more</color></size>");
            return Rule + sb.ToString();
        }

        private void ShowEquipTooltip(EquipView ev)
        {
            PlayerClothing.WornPiece p = _clothing != null ? _clothing.GetPiece(ev.Slot) : default;
            if (!p.Exists)
            {
                ShowHint("<b>" + ev.Slot + "</b>\n<size=13><color=#9aa0a8>Nothing worn. Drag clothing here to put it on.</color></size>");
                return;
            }
            var sb = new System.Text.StringBuilder(200);
            InfoLine(sb, "Status", "Worn \u00b7 " + ev.Slot.ToString().ToLowerInvariant());
            if (p.Def.UsesCharge) InfoLine(sb, p.Def.Gear == GearKind.GasMask ? "Filter" : "Battery", Pct(p.Condition01));
            else if (p.Def.IsDamageable) InfoLine(sb, "Wear", ItemConditionTiers.Label(p.Condition01) + "  \u00b7  " + Pct(p.Condition01) + "  (" + p.Condition + " / " + p.Def.MaxCondition + ")");
            sb.Append("\n<size=12><color=#8b93a0>Right-click or double-click to take it off</color></size>");
            ShowTooltip(p.Def, Rule + sb.ToString());
        }

        private string TabHint(TabView t)
        {
            if (t.Section != null)
            {
                GridSection s = t.Section.Section;
                if (s == null || _player == null || _player.Layout == null) return null;
                float kg = _player.Layout.WeightIn(s);
                return "<b>" + s.Name + "</b>\n<size=13><color=#9aa0a8>" + s.Width + "\u00d7" + s.Height + " cells  \u00b7  "
                    + kg.ToString("0.0") + (s.Unlimited ? " kg" : " / " + s.Capacity.ToString("0.0") + " kg") + "</color></size>";
            }
            if (ReferenceEquals(t.Container, null))
            {
                int units = 0;
                float kg = 0f;
                for (int i = 0; i < _ground.Count; i++)
                {
                    units += _ground[i].Count;
                    kg += _ground[i].Def.Weight * _ground[i].Count;
                }
                return "<b>Ground</b>\n<size=13><color=#9aa0a8>" + (units == 0 ? "Nothing on the ground within reach"
                    : Plural(units, "item") + "  \u00b7  " + kg.ToString("0.0") + " kg within reach") + "</color></size>";
            }
            LootContainer c = t.Container;
            if (c == null) return null;   // gone since the tabs were drawn
            string status;
            if (_interactor != null && _interactor.SearchTarget == c) status = "Searching\u2026";
            else if (!c.Searched && c.SearchTime > 0f) status = "Unsearched \u2014 click to search it";
            else
            {
                CountContents(c.Contents, out int items, out float ckg);
                status = items == 0 ? "Empty" : Plural(items, "item") + "  \u00b7  " + ckg.ToString("0.0") + " kg";
            }
            return "<b>" + c.DisplayName + "</b>\n<size=13><color=#9aa0a8>" + status + "  \u00b7  "
                + FlatDistance(c.transform.position).ToString("0.0") + " m</color></size>";
        }
        #endregion
    }
}
