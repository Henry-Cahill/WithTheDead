using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The code-built inventory / loot UI, in two interchangeable views (toggled from either; the choice
    /// persists in PlayerPrefs "clonez.inventoryView"):
    ///
    /// GRID (the default) — two floating, draggable, resizable Inventory-Tetris windows at the top-left:
    /// the survivor (worn-slot strip, then a grid per carry section — pockets, rig, thigh bag, backpack)
    /// and the loot (the open container's grid, or the ground within reach), with drag-and-drop between
    /// cells, a tab strip on each window and footer transfer / tidy buttons. See the .Grid*.cs partials.
    ///
    /// LIST — a Project Zomboid-style window in three panes:
    ///   • YOU — the bag as a verbose details list (Type / Category / Weight / Status), with worn
    ///     clothing and the weapon in hand pinned on top above a divider, the way PZ lists them;
    ///   • the open container — or the FLOOR within reach — in the same verbose list;
    ///   • CONTAINERS — everything lootable within reach (the floor, furniture, corpses, sacks) with
    ///     item count, weight, distance and whether it's been searched. Click one to open it; an
    ///     unsearched one is rummaged through first, just as in the world.
    /// Identical items share one row that expands (►/▼) into a row per item, each with its own
    /// condition or freshness bar. Rows carry inline actions, a right-click (or "…") context menu and
    /// double-click to move an item across. The list and container-column halves live in
    /// InventoryUIController.List.cs / .Picker.cs.
    ///
    /// Either way, opening it sets <see cref="UIState.GameplayBlocked"/> so clicks don't leak into
    /// combat, and frees the cursor. Requires an EventSystem with an InputSystemUIInputModule.
    /// </summary>
    public partial class InventoryUIController : MonoBehaviour
    {
        /// <summary>Indicates whether the inventory UI window is currently open.</summary>
        public bool IsOpen { get; private set; }
        /// <summary>The container shown in the loot pane / window (null when it shows the floor).</summary>
        public LootContainer CurrentContainer => _openContainer;

        // Window layout in reference pixels (1920×1080); the window shrinks to fit smaller canvases.
        private const float WinW = 1616f;
        private const float WinH = 780f;
        private const float Margin = 28f;
        private const float PaneGap = 20f;
        private const float PaneTop = 60f;      // pane titles
        private const float ColumnTop = 100f;   // column-title strips, and the top of the container column
        private const float ListTop = 126f;     // the scrolling item lists
        private const float FooterH = 46f;

        private static readonly Color HeaderDim = new Color(0.66f, 0.7f, 0.76f);

        private PlayerInventory _player;
        private PlayerClothing _clothing;
        private PlayerInteractor _interactor;
        private Font _font;

        private GameObject _root;              // dim backdrop + window (toggled on/off)
        private UIFader _fader;
        private RectTransform _window;
        private RectTransform _uiCanvasRect;

        private Text _playerHeader;
        private Image _weightMeterFill;
        private GameObject _storeAllButton;
        private Text _lootHeader;
        private Text _lootSubHeader;

        // The two item panes: the bag (left) and the open container or the floor (middle).
        private readonly Pane _bagPane = new Pane();
        private readonly Pane _lootPane = new Pane();

        private LootContainer _openContainer;
        private Inventory _openInventory;      // held apart so it can be unsubscribed even once the container is destroyed
        private System.Action _markDirty;
        private bool _dirty;
        private bool _nearStove;

        // The ground items within reach, listed as the FLOOR whenever no container is open.
        private readonly List<WorldItem> _floorItems = new List<WorldItem>();

        // Hover tooltip
        private GameObject _tooltipRoot;
        private RectTransform _tooltipBg;
        private Text _tooltipText;
        private ItemDef _tooltipDef;

        // The right-click / "…" context menu (null while closed)
        private GameObject _menu;

        /// <summary>Initialises the UI with the given player inventory and related components.</summary>
        public void Bind(PlayerInventory player)
        {
            _player = player;
            _clothing = player != null ? player.GetComponent<PlayerClothing>() : null;
            _interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _markDirty = MarkDirty;
            LoadGridPrefs();
            Build();
            // PlayerInventory.Changed covers the bag and equipping; clothing raises its own.
            if (_player != null) _player.Changed += _markDirty;
            if (_clothing != null) _clothing.Changed += _markDirty;
            Hide();
        }

        private void OnDestroy()
        {
            if (_player != null) _player.Changed -= _markDirty;
            if (_clothing != null) _clothing.Changed -= _markDirty;
            DetachContainer();
            if (IsOpen) UIState.GameplayBlocked = false;
        }

        private void OnDisable() => UIState.GameplayBlocked = false;

        private void MarkDirty() => _dirty = true;

        private void Update()
        {
            if (!IsOpen)
            {
                if (_tooltipRoot != null && _tooltipRoot.activeSelf) HideTooltip(null);
                return;
            }

            // The open container vanished (a corpse rotted away, or it was dismantled): show the floor.
            if (_openInventory != null && _openContainer == null) ShowFloor();

            if (_gridMode) TickGrid();
            else TickPicker();
            if (_tooltipRoot != null && _tooltipRoot.activeSelf) PositionTooltip();
        }

        // Rebuilds at most once a frame, however many changes landed (a Take All fires one per stack).
        private void LateUpdate()
        {
            if (!IsOpen || !_dirty) return;
            _dirty = false;
            Refresh();
        }

        #region Tooltip
        /// <summary>Shows an item's tooltip by the cursor using the default extra information.</summary>
        public void ShowTooltip(ItemDef def) => ShowTooltip(def, null);

        /// <summary>Shows an item's tooltip by the cursor; <paramref name="extra"/> appends lines about
        /// the particular item hovered (its wear, freshness, where it is) below the catalogue entry.</summary>
        public void ShowTooltip(ItemDef def, string extra)
        {
            if (def == null || _tooltipRoot == null || _menu != null) return;
            _tooltipDef = def;
            PresentTooltip(string.IsNullOrEmpty(extra) ? def.Tooltip() : def.Tooltip() + extra);
        }

        // A plain rich-text tooltip that isn't about an item (a tab, an empty worn slot).
        private void ShowHint(string text)
        {
            if (string.IsNullOrEmpty(text) || _tooltipRoot == null || _menu != null) return;
            _tooltipDef = null;
            PresentTooltip(text);
        }

        private void PresentTooltip(string body)
        {
            _tooltipText.text = body;

            const float w = 320f;
            _tooltipText.rectTransform.sizeDelta = new Vector2(w - 24f, 400f);
            float h = _tooltipText.preferredHeight + 20f;
            _tooltipBg.sizeDelta = new Vector2(w, h);
            _tooltipText.rectTransform.sizeDelta = new Vector2(w - 24f, h - 20f);

            _tooltipRoot.SetActive(true);
            _tooltipRoot.transform.SetAsLastSibling();
            PositionTooltip();
        }

        /// <summary>Hides the currently displayed tooltip, optionally only if it matches the given item.</summary>
        public void HideTooltip(ItemDef def)
        {
            if (def != null && def != _tooltipDef) return;   // a newer row already took over
            _tooltipDef = null;
            if (_tooltipRoot != null) _tooltipRoot.SetActive(false);
        }

        private void PositionTooltip()
        {
            if (_uiCanvasRect == null || _tooltipBg == null || Mouse.current == null) return;
            Vector2 screen = Mouse.current.position.ReadValue();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiCanvasRect, screen, null, out Vector2 local))
                return;

            Vector2 size = _tooltipBg.sizeDelta;
            Rect r = _uiCanvasRect.rect;
            float x = local.x + 20f;
            float y = local.y - 20f;
            if (x + size.x > r.xMax) x = local.x - 20f - size.x;   // flip to the left near the right edge
            if (y - size.y < r.yMin) y = r.yMin + size.y;          // ride above the bottom edge
            x = Mathf.Clamp(x, r.xMin, r.xMax - size.x);
            y = Mathf.Clamp(y, r.yMin + size.y, r.yMax);
            _tooltipBg.anchoredPosition = new Vector2(x, y);
        }
        #endregion

        #region Open / close
        /// <summary>Raised when the local survivor opens / leaves a container (the netcode locks and syncs it).</summary>
        public static event System.Action<LootContainer> ContainerOpened, ContainerClosed;

        /// <summary>Opens the window on a container — or, when it's already open, switches the loot pane
        /// to it (picked from the container column, or a search started there just finished).</summary>
        public void OpenContainer(LootContainer container)
        {
            if (container == null) return;
            if (IsOpen && container == _openContainer) { MarkDirty(); return; }
            DetachContainer();
            _openContainer = container;
            ContainerOpened?.Invoke(container);
            _openInventory = container.Contents;
            container.Searched = true;
            if (_gridMode) container.EnsureLayout();   // arranged before we listen, so it doesn't refresh twice
            if (_openInventory != null) _openInventory.Changed += _markDirty;
            if (IsOpen) MarkDirty();
            else OpenInternal();
        }

        /// <summary>Opens the inventory UI without displaying any container, showing only the player's bag and the floor.</summary>
        public void OpenInventoryOnly()
        {
            DetachContainer();
            OpenInternal();
        }

        /// <summary>Switches the loot pane to the floor around the survivor.</summary>
        public void ShowFloor()
        {
            DetachContainer();
            MarkDirty();
        }

        private void DetachContainer()
        {
            if (_openInventory != null && _markDirty != null) _openInventory.Changed -= _markDirty;
            if (_openContainer != null) ContainerClosed?.Invoke(_openContainer);
            _openInventory = null;
            _openContainer = null;
        }

        private void OpenInternal()
        {
            IsOpen = true;
            UIState.GameplayBlocked = true;
            ShowActiveRoot();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (_gridMode && _leftWin != null)
            {
                KeepOnScreen(_leftWin.Root);
                KeepOnScreen(_rightWin.Root);
                _gridPoll = 0f;
            }
            _dirty = false;
            Refresh();
        }

        /// <summary>Closes the inventory UI, cancelling any active actions and restoring gameplay input.</summary>
        public void Close()
        {
            // Esc while an item is held (or a menu is up) only lets go of that: PlayerInteractor routes Esc here.
            if (IsOpen && ConsumeEscape()) return;
            CancelDrag();
            DetachContainer();
            // A rummage started from the container column belongs to this window; closing abandons it.
            if (_interactor != null && _interactor.IsSearching) _interactor.CancelSearch();
            IsOpen = false;
            UIState.GameplayBlocked = false;
            HideMenu();
            HideTooltip(null);   // the tooltip floats outside the modal, so dismiss it explicitly
            _hover = null;
            _tipOwner = null;
            HideRoots();
        }

        private void Hide()
        {
            IsOpen = false;
            UIState.GameplayBlocked = false;
            HideRoots();
        }

        // Shows the active view's root (fading the other out).
        private void ShowActiveRoot()
        {
            if (_gridMode)
            {
                if (_fader != null) _fader.Hide();
                if (_gridFader != null) _gridFader.Show();
            }
            else
            {
                if (_gridFader != null) _gridFader.Hide();
                if (_fader != null) _fader.Show();
            }
        }

        private void HideRoots()
        {
            if (_fader != null) _fader.Hide();
            if (_gridFader != null) _gridFader.Hide();
        }

        private int _escFrame = -1;
        private bool _escConsumed;

        // In the grid view Esc first lets go of a held item, then shuts an open menu, and only then closes
        // the window. This window's Update and PlayerInteractor (through Close) both see the same press, in
        // no fixed order, so whichever asks first decides for that frame. The list view is unchanged.
        private bool ConsumeEscape()
        {
            Keyboard kb = Keyboard.current;
            if (!_gridMode || kb == null || !kb.escapeKey.wasPressedThisFrame) return false;
            if (_escFrame == Time.frameCount) return _escConsumed;
            _escFrame = Time.frameCount;
            _escConsumed = true;
            if (_drag.Active) CancelDrag();
            else if (_menu != null) HideMenu();
            else _escConsumed = false;
            return _escConsumed;
        }
        #endregion

        #region Actions
        // Rows outlive a frame (and the menu holds them longer), so every action first checks that the
        // stack is still where the row found it.
        private static bool Holds(Inventory inv, ItemStack stack) =>
            inv != null && stack != null && stack.Count > 0 && inv.Stacks.Contains(stack);

        private void TakeFromContainer(ItemStack stack, int count)
        {
            if (!Holds(_openInventory, stack)) return;
            _openInventory.TransferTo(_player.Inventory, stack, count, enforceWeight: true);
            MarkDirty();
        }

        private void StoreInContainer(ItemStack stack, int count)
        {
            if (_openInventory == null || !Holds(_player.Inventory, stack)) return;
            _player.Inventory.TransferTo(_openInventory, stack, count, enforceWeight: false);
            KeepHandsHonest();
            MarkDirty();
        }

        // Grabs a ground pile into the pack (as much as fits, up to max); any overflow stays put.
        private void TakeFromFloor(WorldItem item, int max = int.MaxValue)
        {
            if (item == null || _player == null || !item.StillThere) return;
            _player.PickUp(item, max);
            MarkDirty();
        }

        private void TakeAll()
        {
            if (_openInventory != null) _openInventory.TransferAllTo(_player.Inventory, enforceWeight: true);
            else TakeAllFromFloor();
            MarkDirty();
        }

        // Sweeps everything within reach off the floor and into the pack; anything that won't
        // fit the carry weight is left where it lies.
        private void TakeAllFromFloor()
        {
            if (_player == null) return;
            RefreshFloorItems();
            var snapshot = new List<WorldItem>(_floorItems);
            for (int i = 0; i < snapshot.Count; i++)
            {
                WorldItem w = snapshot[i];
                if (w != null) _player.PickUp(w);
            }
        }

        // Whole-row moves for a collapsed stack: every item of that kind in the pane.
        private void TakeEntries(List<Entry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.Kind == EntryKind.Floor)
                {
                    if (e.Pile != null && e.Pile.StillThere) _player.PickUp(e.Pile);
                }
                else if (Holds(_openInventory, e.Stack))
                    _openInventory.TransferTo(_player.Inventory, e.Stack, e.Stack.Count, enforceWeight: true);
            }
            MarkDirty();
        }

        private void StoreEntries(List<Entry> entries)
        {
            if (_openInventory == null) return;
            for (int i = 0; i < entries.Count; i++)
                if (Holds(_player.Inventory, entries[i].Stack))
                    _player.Inventory.TransferTo(_openInventory, entries[i].Stack, entries[i].Stack.Count, enforceWeight: false);
            KeepHandsHonest();
            MarkDirty();
        }

        private void DropEntries(List<Entry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
                if (Holds(_player.Inventory, entries[i].Stack))
                    _player.Drop(entries[i].Stack, entries[i].Stack.Count);
            KeepHandsHonest();
            MarkDirty();
        }

        private void UseStack(ItemStack stack)
        {
            if (!Holds(_player.Inventory, stack)) return;
            _player.Use(stack);
            MarkDirty();
        }

        private void DropStack(ItemStack stack, int count)
        {
            if (!Holds(_player.Inventory, stack)) return;
            _player.Drop(stack, Mathf.Min(count, stack.Count));
            KeepHandsHonest();
            MarkDirty();
        }

        private void Unequip() { _player.EquipBareHands(); MarkDirty(); }
        private void TakeOffSlot(ClothingSlot slot) { _player.TakeOff(slot); MarkDirty(); }
        private void DropAllStacks() { _player.DropAll(); MarkDirty(); }
        private void StoreAll() { if (_openInventory != null) { _player.StoreAllInto(_openInventory); MarkDirty(); } }
        private void CookStack(ItemStack stack) { if (Holds(_player.Inventory, stack) && _player.TryCook(stack)) MarkDirty(); }
        private void BoilStack(ItemStack stack) { if (Holds(_player.Inventory, stack) && _player.TryPurify(stack)) MarkDirty(); }
        private void RepairStack(ItemStack stack) { if (Holds(_player.Inventory, stack) && _player.TryRepair(stack)) MarkDirty(); }

        // The weapon in hand is the first bag stack of its kind; once the last one has left the bag
        // (dropped or stored) the survivor falls back to bare fists rather than swinging thin air.
        private void KeepHandsHonest()
        {
            if (_player != null && _player.EquippedWeapon != null && _player.EquippedStack == null)
                _player.EquipBareHands();
        }
        #endregion

        #region Context menu
        /// <summary>One line of a row's context menu.</summary>
        private readonly struct MenuOption
        {
            public readonly string Label;
            public readonly System.Action Action;
            public readonly bool Enabled;

            public MenuOption(string label, System.Action action, bool enabled = true)
            {
                Label = label;
                Action = action;
                Enabled = enabled && action != null;
            }
        }

        // Pops a PZ-style context menu at the cursor (right-click a row or a tile, or a row's "…" button),
        // over whichever view is showing.
        private void ShowMenu(string title, List<MenuOption> options, float width = 220f)
        {
            HideMenu();
            HideTooltip(null);
            Transform host = _gridMode && _gridRoot != null ? _gridRoot.transform : _root != null ? _root.transform : null;
            if (host == null || options == null || options.Count == 0) return;

            // A full-screen, invisible layer under the menu: clicking anywhere off the menu dismisses it.
            var layer = new GameObject("ContextMenu", typeof(RectTransform));
            layer.SetActive(false);
            var layerRt = (RectTransform)layer.transform;
            layerRt.SetParent(host, false);
            layerRt.anchorMin = Vector2.zero;
            layerRt.anchorMax = Vector2.one;
            layerRt.offsetMin = Vector2.zero;
            layerRt.offsetMax = Vector2.zero;
            layer.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var dismiss = layer.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(HideMenu);
            layer.SetActive(true);
            _menu = layer;

            float w = width;
            const float itemH = 26f, titleH = 26f, pad = 4f;
            float h = titleH + options.Count * itemH + pad * 2f;

            Vector2 local = Vector2.zero;
            if (Mouse.current != null)
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRt, Mouse.current.position.ReadValue(), null, out local);
            Rect r = layerRt.rect;
            float x = Mathf.Clamp(local.x + 2f, r.xMin + 4f, r.xMax - w - 4f);
            float y = Mathf.Clamp(local.y + 2f, r.yMin + h + 4f, r.yMax - 4f);

            RectTransform panel = Panel(layerRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h), new Color(0.07f, 0.08f, 0.1f, 0.98f));
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(x, y);
            UITheme.AddTopAccent(panel, new Color(0.5f, 0.68f, 0.9f, 0.6f), 2f);

            Text head = Label(panel, new Vector2(0f, 1f), new Vector2(10f, -pad), new Vector2(w - 20f, titleH), 13, TextAnchor.MiddleLeft, new Color(0.6f, 0.66f, 0.74f));
            head.text = FitText(head, title, "", w - 20f);
            head.raycastTarget = false;

            for (int i = 0; i < options.Count; i++)
            {
                MenuOption opt = options[i];
                System.Action act = opt.Action;
                Button b = MakeButton(panel, new Vector2(0f, 1f), new Vector2(pad, -(pad + titleH + i * itemH)), new Vector2(w - pad * 2f, itemH - 2f),
                    opt.Label, new Color(0.17f, 0.19f, 0.23f), () => { HideMenu(); act?.Invoke(); }, 14);
                Text bt = b.GetComponentInChildren<Text>();
                bt.alignment = TextAnchor.MiddleLeft;
                bt.rectTransform.offsetMin = new Vector2(10f, 0f);
                b.interactable = opt.Enabled;
                if (!opt.Enabled) bt.color = new Color(1f, 1f, 1f, 0.38f);
            }
        }

        private void HideMenu()
        {
            if (_menu == null) return;
            Destroy(_menu);
            _menu = null;
        }
        #endregion

        #region Refresh
        private void Refresh()
        {
            if (_player == null) return;
            if (_gridMode)
            {
                RefreshGrid();
                return;
            }
            HideTooltip(null);   // rows are about to be rebuilt
            HideMenu();
            FitWindow();
            _nearStove = _player.CanCookHere();

            RefreshPlayerHeader();
            BuildBagList();

            if (_storeAllButton != null) _storeAllButton.SetActive(_openInventory != null);
            if (_openInventory != null) BuildContainerList();
            else BuildFloorList();   // no container open: the loot pane lists the floor within reach
            RefreshLootHeader();

            RefreshPicker(force: true);
        }

        private void RefreshPlayerHeader()
        {
            float load = _player.Load;
            int worn = _clothing != null ? _clothing.WornCount : 0;
            string header = "YOU   " + _player.Weight.ToString("0.0") + " / " + _player.Capacity.ToString("0.0") + " kg"
                + (_player.EquippedWeapon != null ? "     Wielding: " + _player.EquippedWeapon.DisplayName : "     Bare hands")
                + (worn > 0 ? "     Wearing " + worn : "");
            _playerHeader.text = FitText(_playerHeader, header, "", ListW);
            Color warn = load >= 1f ? new Color(0.95f, 0.3f, 0.2f)
                : load >= CloneZConfig.EncumbranceStart ? new Color(0.95f, 0.7f, 0.2f)
                : Color.white;
            _playerHeader.color = warn;

            if (_weightMeterFill != null)
            {
                Vector3 ms = _weightMeterFill.rectTransform.localScale;
                ms.x = Mathf.Clamp01(load);
                _weightMeterFill.rectTransform.localScale = ms;
                _weightMeterFill.color = load >= 1f || load >= CloneZConfig.EncumbranceStart ? warn : new Color(0.5f, 0.72f, 0.5f);
            }
        }

        private void RefreshLootHeader()
        {
            if (_openInventory != null && _openContainer != null)
            {
                CountContents(_openInventory, out int items, out float kg);
                _lootHeader.text = FitText(_lootHeader, _openContainer.DisplayName.ToUpperInvariant(), "", ListW - 140f);
                _lootSubHeader.text = (items == 0 ? "Empty" : Plural(items, "item") + "  \u00b7  " + kg.ToString("0.0") + " kg")
                    + "  \u00b7  " + FlatDistance(_openContainer.transform.position).ToString("0.0") + " m away";
                return;
            }

            int units = 0;
            float floorKg = 0f;
            for (int i = 0; i < _floorItems.Count; i++)
            {
                WorldItem w = _floorItems[i];
                if (w == null || w.Def == null) continue;
                units += w.Count;
                floorKg += w.Def.Weight * w.Count;
            }
            _lootHeader.text = "FLOOR";
            _lootSubHeader.text = _floorItems.Count == 0
                ? "Nothing on the ground within reach"
                : Plural(_floorItems.Count, "pile") + "  \u00b7  " + Plural(units, "item") + "  \u00b7  " + floorKg.ToString("0.0") + " kg within reach";
        }

        // Shrinks the window to fit a canvas narrower or shorter than the reference layout.
        private void FitWindow()
        {
            if (_window == null || _uiCanvasRect == null) return;
            Rect r = _uiCanvasRect.rect;
            if (r.width <= 0f || r.height <= 0f) return;
            float s = Mathf.Min(1f, (r.width - 16f) / WinW, (r.height - 16f) / WinH);
            _window.localScale = new Vector3(s, s, 1f);
        }

        private static void CountContents(Inventory inv, out int items, out float kg)
        {
            items = 0;
            kg = 0f;
            if (inv == null) return;
            for (int i = 0; i < inv.Stacks.Count; i++)
            {
                items += inv.Stacks[i].Count;
                kg += inv.Stacks[i].Weight;
            }
        }

        // Ground distance from the survivor, ignoring height (a tall wardrobe isn't "further away").
        private float FlatDistance(Vector3 position)
        {
            if (_player == null) return 0f;
            Vector3 d = position - _player.transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private static string Plural(int n, string noun) => n + " " + noun + (n == 1 ? "" : "s");
        #endregion

        #region Build
        private void Build()
        {
            var canvasGo = new GameObject("Inventory Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 105;   // above the HUD (100)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform root = canvas.GetComponent<RectTransform>();
            _uiCanvasRect = root;

            // Dim modal backdrop.
            _root = new GameObject("Modal");
            var modalRt = _root.AddComponent<RectTransform>();
            modalRt.SetParent(root, false);
            modalRt.anchorMin = Vector2.zero; modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = Vector2.zero; modalRt.offsetMax = Vector2.zero;
            _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            _fader = _root.AddComponent<UIFader>();

            _window = Panel(modalRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(WinW, WinH), new Color(0.11f, 0.12f, 0.14f, 0.98f));
            // four tiny rivets at the window corners – metal fasteners appropriate for a 1993 rural UI
            // size: 8 × 8 pixels; colour: same as the top accent but fully opaque
            Color rivetColor = new Color(0.5f, 0.68f, 0.9f, 1f);
            // top‑left
            var rivetTL = new GameObject("RivetTL", typeof(RectTransform));
            var rtTL = (RectTransform)rivetTL.transform;
            rtTL.SetParent(_window, false);
            rtTL.anchorMin = rtTL.anchorMax = new Vector2(0f, 1f);
            rtTL.pivot = new Vector2(0f, 1f);
            rtTL.anchoredPosition = Vector2.zero;
            rtTL.sizeDelta = new Vector2(8f, 8f);
            var imgTL = rtTL.gameObject.AddComponent<Image>();
            imgTL.color = rivetColor;
            // top‑right
            var rivetTR = new GameObject("RivetTR", typeof(RectTransform));
            var rtTR = (RectTransform)rivetTR.transform;
            rtTR.SetParent(_window, false);
            rtTR.anchorMin = rtTR.anchorMax = new Vector2(1f, 1f);
            rtTR.pivot = new Vector2(1f, 1f);
            rtTR.anchoredPosition = Vector2.zero;
            rtTR.sizeDelta = new Vector2(8f, 8f);
            var imgTR = rtTR.gameObject.AddComponent<Image>();
            imgTR.color = rivetColor;
            // bottom‑left
            var rivetBL = new GameObject("RivetBL", typeof(RectTransform));
            var rtBL = (RectTransform)rivetBL.transform;
            rtBL.SetParent(_window, false);
            rtBL.anchorMin = rtBL.anchorMax = new Vector2(0f, 0f);
            rtBL.pivot = new Vector2(0f, 0f);
            rtBL.anchoredPosition = Vector2.zero;
            rtBL.sizeDelta = new Vector2(8f, 8f);
            var imgBL = rtBL.gameObject.AddComponent<Image>();
            imgBL.color = rivetColor;
            // bottom‑right
            var rivetBR = new GameObject("RivetBR", typeof(RectTransform));
            var rtBR = (RectTransform)rivetBR.transform;
            rtBR.SetParent(_window, false);
            rtBR.anchorMin = rtBR.anchorMax = new Vector2(1f, 0f);
            rtBR.pivot = new Vector2(1f, 0f);
            rtBR.anchoredPosition = Vector2.zero;
            rtBR.sizeDelta = new Vector2(8f, 8f);
            var imgBR = rtBR.gameObject.AddComponent<Image>();
            imgBR.color = rivetColor;
            UITheme.AddTopAccent(_window, new Color(0.5f, 0.68f, 0.9f, 0.6f));

            Text title = Label(_window, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(600f, 40f), 30, TextAnchor.UpperCenter, new Color(0.95f, 0.92f, 0.85f));
            title.text = "INVENTORY";

            MakeButton(_window, new Vector2(1f, 1f), new Vector2(-18f, -14f), new Vector2(40f, 34f), "X", new Color(0.5f, 0.15f, 0.15f), Close);

            // Back to the grid windows (the default view).
            MakeButton(_window, new Vector2(1f, 1f), new Vector2(-66f, -14f), new Vector2(110f, 34f), "Grid view", new Color(0.28f, 0.34f, 0.44f), () => SetViewMode(true));

            // Dump the whole pack onto the floor (top-left, above the bag pane).
            MakeButton(_window, new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(130f, 34f), "Drop All", new Color(0.42f, 0.26f, 0.18f), DropAllStacks);

            // Store the whole pack into the open container (shown only while one is open).
            _storeAllButton = MakeButton(_window, new Vector2(0f, 1f), new Vector2(156f, -14f), new Vector2(130f, 34f), "Store All", new Color(0.28f, 0.34f, 0.44f), StoreAll).gameObject;
            _storeAllButton.SetActive(false);

            float bagX = Margin;
            float lootX = bagX + ListW + PaneGap;
            float pickX = lootX + ListW + PaneGap;
            float listH = WinH - ListTop - FooterH;

            // ---- YOU (left): the bag, worn clothing and the weapon in hand ----
            _playerHeader = Label(_window, new Vector2(0f, 1f), new Vector2(bagX, -PaneTop), new Vector2(ListW, 28f), 20, TextAnchor.UpperLeft, Color.white);

            // Encumbrance meter sits just under the header.
            var wmBg = new GameObject("WeightMeterBg");
            var wmBgRt = wmBg.AddComponent<RectTransform>();
            wmBgRt.SetParent(_window, false);
            wmBgRt.anchorMin = new Vector2(0f, 1f); wmBgRt.anchorMax = new Vector2(0f, 1f); wmBgRt.pivot = new Vector2(0f, 1f);
            wmBgRt.anchoredPosition = new Vector2(bagX, -(PaneTop + 28f));
            wmBgRt.sizeDelta = new Vector2(300f, 6f);
            wmBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            var wmFill = new GameObject("WeightMeterFill");
            var wmFillRt = wmFill.AddComponent<RectTransform>();
            wmFillRt.SetParent(wmBgRt, false);
            wmFillRt.anchorMin = new Vector2(0f, 0f); wmFillRt.anchorMax = new Vector2(0f, 1f); wmFillRt.pivot = new Vector2(0f, 0.5f);
            wmFillRt.sizeDelta = new Vector2(296f, -2f);
            wmFillRt.anchoredPosition = new Vector2(2f, 0f);
            _weightMeterFill = wmFill.AddComponent<Image>();
            _weightMeterFill.color = new Color(0.5f, 0.72f, 0.5f);

            BuildColumnHeader(_window, bagX, -ColumnTop, _bagPane);
            _bagPane.Content = Column(_window, new Vector2(0f, 1f), new Vector2(bagX, -ListTop), new Vector2(ListW, listH));

            // ---- the open container, or the FLOOR (middle) ----
            _lootHeader = Label(_window, new Vector2(0f, 1f), new Vector2(lootX, -PaneTop), new Vector2(ListW - 140f, 28f), 20, TextAnchor.UpperLeft, Color.white);
            _lootSubHeader = Label(_window, new Vector2(0f, 1f), new Vector2(lootX, -(PaneTop + 24f)), new Vector2(ListW - 140f, 16f), 14, TextAnchor.UpperLeft, HeaderDim);
            MakeButton(_window, new Vector2(0f, 1f), new Vector2(lootX + ListW - 120f, -(PaneTop - 2f)), new Vector2(120f, 30f), "Take All", new Color(0.2f, 0.4f, 0.25f), TakeAll);
            BuildColumnHeader(_window, lootX, -ColumnTop, _lootPane);
            _lootPane.Content = Column(_window, new Vector2(0f, 1f), new Vector2(lootX, -ListTop), new Vector2(ListW, listH));

            // ---- CONTAINERS (right): everything lootable within reach ----
            BuildPicker(_window, pickX, WinH - ColumnTop - FooterH);

            Label(_window, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(WinW - 60f, 24f), 15, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.6f))
                .text = "Double-click to move an item      \u2022      Right-click or \u2026 for more      \u2022      \u25ba opens a stack      \u2022      Click a column title to sort      \u2022      Pick a container on the right      \u2022      E / Esc  close";

            BuildGrid(root);      // the grid windows: a sibling of the list modal, under the tooltip
            BuildTooltip(root);
        }

        private void BuildTooltip(RectTransform root)
        {
            _tooltipRoot = new GameObject("Tooltip");
            _tooltipBg = _tooltipRoot.AddComponent<RectTransform>();
            _tooltipBg.SetParent(root, false);
            _tooltipBg.anchorMin = new Vector2(0.5f, 0.5f);
            _tooltipBg.anchorMax = new Vector2(0.5f, 0.5f);
            _tooltipBg.pivot = new Vector2(0f, 1f);
            _tooltipBg.sizeDelta = new Vector2(320f, 120f);
            var bg = _tooltipRoot.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.09f, 0.96f);
            bg.raycastTarget = false;

            _tooltipText = Label(_tooltipBg, new Vector2(0f, 1f), new Vector2(12f, -10f), new Vector2(296f, 100f), 16, TextAnchor.UpperLeft, Color.white);
            _tooltipText.raycastTarget = false;
            _tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tooltipText.verticalOverflow = VerticalWrapMode.Overflow;

            _tooltipRoot.SetActive(false);
        }
        #endregion

        #region uGUI helpers
        private RectTransform Panel(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Panel");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return rt;
        }

        private RectTransform Column(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            // Scroll view root occupies the visible column area.
            var scrollGo = new GameObject("ScrollView");
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.SetParent(parent, false);
            scrollRt.anchorMin = anchor; scrollRt.anchorMax = anchor; scrollRt.pivot = new Vector2(0f, 1f);
            scrollRt.anchoredPosition = pos; scrollRt.sizeDelta = size;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            // Viewport clips the rows to the column bounds.
            var vpGo = new GameObject("Viewport");
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.SetParent(scrollRt, false);
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpRt.pivot = new Vector2(0f, 1f);
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // invisible, but catches the scroll wheel
            vpGo.AddComponent<RectMask2D>();

            // Content holds the rows; the list builders set its height so it can scroll.
            var contentGo = new GameObject("Content");
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.SetParent(vpRt, false);
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            scroll.viewport = vpRt;
            scroll.content = contentRt;
            return contentRt;
        }

        private Text Label(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject("Label");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = _font; text.fontSize = fontSize; text.alignment = align; text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            UITheme.AddShadow(text);
            return text;
        }

        private Button MakeButton(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, Color color,
            UnityEngine.Events.UnityAction onClick, int fontSize = 16)
        {
            var go = new GameObject("Button");
            go.SetActive(false);   // configure the tint before the button wakes, so rebuilt rows don't flicker
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = new Color(Mathf.Min(color.r + 0.12f, 1f), Mathf.Min(color.g + 0.12f, 1f), Mathf.Min(color.b + 0.12f, 1f), 1f);
            cb.pressedColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 1f);
            cb.selectedColor = cb.highlightedColor;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var txtGo = new GameObject("Text");
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.SetParent(rt, false);
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.font = _font; txt.fontSize = fontSize; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = text;
            go.SetActive(true);
            return btn;
        }

        /// <summary>A borderless text button that only lightens on hover (column titles, expand arrows).
        /// Returns its label so callers can restyle it.</summary>
        private Text FlatButton(RectTransform parent, Vector2 pos, Vector2 size, string text, int fontSize,
            UnityEngine.Events.UnityAction onClick, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("FlatButton", typeof(RectTransform));
            go.SetActive(false);   // configure the tint before the button wakes, so it doesn't flash white
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = Color.white;   // invisible until hovered: the tint below sets the alpha
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0f);
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.1f);
            cb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
            cb.selectedColor = cb.normalColor;
            cb.disabledColor = cb.normalColor;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var txtGo = new GameObject("Text", typeof(RectTransform));
            var txtRt = (RectTransform)txtGo.transform;
            txtRt.SetParent(rt, false);
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(4f, 0f); txtRt.offsetMax = new Vector2(-4f, 0f);
            var txt = txtGo.AddComponent<Text>();
            txt.font = _font; txt.fontSize = fontSize; txt.alignment = align; txt.color = color; txt.text = text;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.raycastTarget = false;
            go.SetActive(true);
            return txt;
        }

        private static void ClearChildren(RectTransform rt)
        {
            for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
        }

        /// <summary>Trims <paramref name="text"/> with an ellipsis so that it — plus a
        /// <paramref name="suffix"/> that is always kept, such as a stack count — fits
        /// <paramref name="maxWidth"/> in the label's font.</summary>
        private static string FitText(Text label, string text, string suffix, float maxWidth)
        {
            suffix ??= "";
            if (label == null || string.IsNullOrEmpty(text)) return text + suffix;

            TextGenerationSettings settings = label.GetGenerationSettings(Vector2.zero);
            TextGenerator gen = label.cachedTextGeneratorForLayout;
            float ppu = label.pixelsPerUnit > 0f ? label.pixelsPerUnit : 1f;
            float Width(string s) => gen.GetPreferredWidth(s, settings) / ppu;

            if (Width(text + suffix) <= maxWidth) return text + suffix;
            int lo = 0, hi = text.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (Width(text.Substring(0, mid).TrimEnd() + "\u2026" + suffix) <= maxWidth) lo = mid;
                else hi = mid - 1;
            }
            return text.Substring(0, lo).TrimEnd() + "\u2026" + suffix;
        }
        #endregion
    }
}
