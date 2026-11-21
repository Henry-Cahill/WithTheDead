using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    // The Inventory-Tetris grid view — the default; the verbose list is a toggle away. Two floating
    // windows at the top-left of the screen, draggable by their title bars and resizable from their
    // corner grips:
    //   • LEFT — the survivor: a strip of worn-slot tiles, then a scrolling stack of grid sections (the
    //     pockets, then each worn rig / thigh bag / backpack), each a header strip over an icon tile
    //     (its load under it) and its cells, with a tab per section down the window's right edge;
    //   • RIGHT — the open container's growable grid, or the GROUND within reach packed into a 12-wide
    //     grid, with a tab for the ground and for every container in reach.
    // Items are tiles spanning their footprint. Tiles, sections and tabs are pooled and rebound on a
    // refresh, and the cells are one tiled Image per section, so a refresh creates next to nothing.
    // Drag-and-drop, quick transfers and the context menu live in .GridDrag.cs; the footer buttons, the
    // options and the view toggle in .GridActions.cs.
    public partial class InventoryUIController
    {
        #region Grid constants
        private const float Cell = GridSprites.CellPx;          // one cell, in reference pixels
        private const float GTitleH = 22f;                      // title bar
        private const float GPad = 6f;                          // window padding
        private const float GTabW = 28f;                        // the tab strip down the right edge
        private const float GTabGap = 3f;
        private const float GScrollbarW = 5f;
        private const float GSecHeadH = 20f;                    // a section's header strip
        private const float GIconTile = 56f;                    // a section's square icon tile
        private const float GGridX = 4f + GIconTile + 8f;       // where a section's cells start
        private const float GSecGap = 8f;                       // between sections
        private const float GEquipRowH = 32f;                   // the worn-slot strip: two rows of tiles
        private const float GEquipGap = 3f;
        private const float GEquipH = GEquipRowH * 2f + GEquipGap;
        private const float GButtonH = 24f;
        private const float GFootH = GPad + GButtonH + 22f;     // the footer buttons plus a status line
        private const float GLeftW = 440f, GLeftH = 740f;       // default window sizes
        private const float GRightW = 490f, GRightH = 520f;
        private const float GWinX = 40f, GWinY = -40f, GWinGap = 12f;
        private const int GroundCols = 12;
        private const int GroundMinRows = 12;
        private const float GroundRefKg = 50f;                  // the ground tile reads "<kg> / 50"
        private const float GPollInterval = 0.25f;             // the ground and the containers in reach: 4×/s
        private const float GStatusSeconds = 3.5f;

        private static readonly Color GWinBg = new Color32(0x0d, 0x0f, 0x11, 0xe6);     // 90 %
        private static readonly Color GBorder = new Color32(0x5a, 0x5f, 0x66, 0xff);
        private static readonly Color GTitleBg = new Color32(0x1a, 0x1d, 0x21, 0xff);
        private static readonly Color GHeaderBg = new Color32(0x20, 0x24, 0x29, 0xff);
        private static readonly Color GButtonBg = new Color32(0x2a, 0x2e, 0x33, 0xff);
        private static readonly Color GTextDim = new Color(0.64f, 0.67f, 0.72f);
        private static readonly Color GOver = new Color(0.95f, 0.36f, 0.3f);
        private static readonly Color GChargeCol = new Color(0.35f, 0.85f, 0.95f);
        private static readonly Color GAccent = new Color(0.5f, 0.68f, 0.9f);
        private static readonly Color GGroundTint = new Color(0.42f, 0.4f, 0.34f);
        private static readonly Color GPocketTint = new Color(0.45f, 0.5f, 0.56f);
        #endregion

        #region Grid view types
        /// <summary>Whose cells a section view draws.</summary>
        private enum GridSide { Player, Container, Ground }

        /// <summary>One floating window of the grid view and its pooled contents.</summary>
        private sealed class GridWindow
        {
            public bool Left;                  // the survivor's window (else the loot window)
            public RectTransform Root;
            public Text Title;
            public RectTransform Viewport;
            public RectTransform Content;
            public RectTransform TabStrip;
            public Text Status;
            public float StatusTimer;
            public readonly List<SectionView> Sections = new List<SectionView>(4);
            public int SectionsUsed;
            public readonly List<TabView> Tabs = new List<TabView>(6);
            public int TabsUsed;
        }

        /// <summary>A section drawn in a window: its header strip, icon tile and cells.</summary>
        private sealed class SectionView
        {
            public GridWindow Window;
            public RectTransform Root;
            public Text HeaderText;
            public RectTransform Toggle;       // the [V] box after the name
            public Text ToggleText;
            public RectTransform Row;          // icon tile + cells (hidden while folded)
            public Image Icon;
            public Text IconGlyph;
            public Text Caption;
            public RectTransform Grid;         // the cells: top-left origin, one tiled Image
            // What it shows (rebound every refresh).
            public GridSide Side;
            public GridSection Section;        // null for the ground
            public Inventory Inventory;        // null for the ground
            public object Key;                 // what its folded state is remembered by
            public int Cols, Rows;             // rows drawn (a growable grid grows past them)
            public float Top;                  // its offset in the window's scrolling content
            public bool Collapsed;
        }

        /// <summary>A pooled item tile (one more serves as the drag ghost).</summary>
        private sealed class TileView
        {
            public RectTransform Rt;
            public Image Bg;
            public Text Name;
            public Text Count;
            public RectTransform BarBg;
            public RectTransform BarFill;
            public Image BarFillImg;
            public Image Badge;
            public GridPointerRelay Relay;
            public Color BaseColor;
            // What it shows (rebound every refresh).
            public SectionView View;
            public ItemStack Stack;            // the live stack (pack / container), or null for a ground pile
            public WorldItem Ground;           // the ground pile, or null
            public ItemStack Shown;            // what's drawn: the live stack, or a display copy of the pile
            public bool Rotated;
            public bool InHand;
            public int X, Y, W, H;
        }

        /// <summary>A tile of the worn-slot strip.</summary>
        private sealed class EquipView
        {
            public ClothingSlot Slot;
            public RectTransform Rt;
            public Image Bg;
            public Text Caption;
            public Text Name;
            public RectTransform BarBg;
            public RectTransform BarFill;
            public Image BarFillImg;
            public Color BaseColor;
        }

        /// <summary>A tab on a window's right edge: a section (left), or the ground / a container (right).</summary>
        private sealed class TabView
        {
            public RectTransform Rt;
            public Image Bg;
            public Text Glyph;
            public RectTransform Fill;         // a search's progress, rising from the bottom
            public Image Mark;                 // the selected tab's accent
            public Color BaseColor;
            public SectionView Section;        // left tabs
            public LootContainer Container;    // right tabs (null = the ground)
        }
        #endregion

        #region Grid state
        private bool _gridMode = true;
        private GameObject _gridRoot;
        private RectTransform _gridRootRt;
        private UIFader _gridFader;
        private GridWindow _leftWin;
        private GridWindow _rightWin;
        private EquipView[] _equip;

        private readonly List<TileView> _tiles = new List<TileView>(64);
        private int _tilesUsed;
        private TileView _ghost;
        private RectTransform _preview;
        private Image _previewImg;
        private RectTransform _equipPreview;
        private Image _equipPreviewImg;

        // Folded sections, remembered by their GridSection (sections keep their identity) or GroundKey.
        private readonly HashSet<object> _collapsed = new HashSet<object>();
        private static readonly object GroundKey = new object();
        private object _scrollToKey;
        private object _lootShown;   // the container (or GroundKey) the loot window showed last

        // The ground within reach, sorted and packed into the loot window's 12-wide grid.
        private readonly List<WorldItem> _ground = new List<WorldItem>();
        private readonly List<ItemDef> _groundDefs = new List<ItemDef>();
        private readonly List<RectInt> _groundCells = new List<RectInt>();
        private readonly List<bool> _groundRot = new List<bool>();
        private System.Comparison<WorldItem> _groundOrder;
        private int _groundSig;
        private int _tabSig;
        private float _gridPoll;

        // The running search shown on its tab and in the loot window's status line.
        private LootContainer _tabSearching;
        private TabView _searchTab;
        private int _searchPct = -1;

        // The element under the pointer, and the one whose tooltip / hint is showing.
        private GridPointerRelay _hover;
        private GridPointerRelay _tipOwner;

        private readonly Dictionary<ItemDef, string> _shortLabels = new Dictionary<ItemDef, string>();
        private static readonly string[] s_Counts = new string[1000];
        #endregion

        #region Build
        private void BuildGrid(RectTransform canvas)
        {
            _gridRoot = new GameObject("Grid Inventory", typeof(RectTransform));
            _gridRootRt = (RectTransform)_gridRoot.transform;
            _gridRootRt.SetParent(canvas, false);
            _gridRootRt.anchorMin = Vector2.zero;
            _gridRootRt.anchorMax = Vector2.one;
            _gridRootRt.pivot = new Vector2(0.5f, 0.5f);
            _gridRootRt.offsetMin = Vector2.zero;
            _gridRootRt.offsetMax = Vector2.zero;
            // The lightest of dims: the world stays readable around the windows.
            _gridRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
            _gridFader = _gridRoot.AddComponent<UIFader>();
            _groundOrder = CompareGround;

            _leftWin = BuildGridWindow("Survivor Window", true, new Vector2(GWinX, GWinY), new Vector2(GLeftW, GLeftH));
            _rightWin = BuildGridWindow("Loot Window", false, new Vector2(GWinX + GLeftW + GWinGap, GWinY), new Vector2(GRightW, GRightH));
            BuildEquipStrip(_leftWin);

            // The drop preview: a translucent green / amber / red block over the cells an item would cover.
            _previewImg = GImage(_gridRootRt, "Drop Preview", Color.clear);
            _preview = _previewImg.rectTransform;
            _preview.anchorMin = _preview.anchorMax = _preview.pivot = new Vector2(0f, 1f);
            _preview.gameObject.SetActive(false);

            _equipPreviewImg = GImage(_gridRootRt, "Wear Preview", Color.clear);
            _equipPreview = _equipPreviewImg.rectTransform;
            GFill(_equipPreview, 0f, 0f, 0f, 0f);
            _equipPreview.gameObject.SetActive(false);

            _ghost = NewTileView(_gridRootRt, interactive: false);
            _ghost.Rt.anchorMin = _ghost.Rt.anchorMax = new Vector2(0.5f, 0.5f);   // positioned in the root's centred space
            _ghost.Rt.gameObject.SetActive(false);
        }

        private GridWindow BuildGridWindow(string name, bool left, Vector2 pos, Vector2 size)
        {
            var w = new GridWindow { Left = left };
            RectTransform root = GRect(name, _gridRootRt);
            GPlace(root, pos.x, pos.y, size.x, size.y);
            root.gameObject.AddComponent<Image>().color = GWinBg;   // also swallows clicks on the panel
            GBorderLines(root, GBorder);
            w.Root = root;

            // Title bar: drag it to move the window. Close (X) and the list-view toggle sit at its right.
            RectTransform bar = GRect("Title Bar", root);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            bar.pivot = new Vector2(0f, 1f);
            bar.offsetMin = new Vector2(1f, -(GTitleH + 1f));
            bar.offsetMax = new Vector2(-1f, -1f);
            bar.gameObject.AddComponent<Image>().color = GTitleBg;
            var mover = bar.gameObject.AddComponent<GridDragHandle>();
            mover.Target = root;
            mover.Changed = () => KeepOnScreen(root);

            RectTransform close = (RectTransform)GButton(bar, "Close", "X", 13, new Color(0.4f, 0.16f, 0.16f), Close, out _).transform;
            close.anchorMin = close.anchorMax = close.pivot = new Vector2(1f, 0.5f);
            close.anchoredPosition = new Vector2(-2f, 0f);
            close.sizeDelta = new Vector2(GTitleH - 4f, GTitleH - 4f);

            RectTransform toList = (RectTransform)GButton(bar, "List View", "List view", 11, GButtonBg, () => SetViewMode(false), out _).transform;
            toList.anchorMin = toList.anchorMax = toList.pivot = new Vector2(1f, 0.5f);
            toList.anchoredPosition = new Vector2(-(GTitleH + 2f), 0f);
            toList.sizeDelta = new Vector2(64f, GTitleH - 4f);

            w.Title = GText(bar, "Title", 13, TextAnchor.MiddleLeft, Color.white);
            GFill(w.Title.rectTransform, 8f, 0f, GTitleH + 72f, 0f);

            float top = GTitleH + 1f + GPad + (left ? GEquipH + GPad : 0f);
            float tabsRight = GPad;
            float barRight = tabsRight + GTabW + 4f;
            float viewRight = barRight + GScrollbarW + 3f;

            // The scrolling stack of sections (wheel and scrollbar only: a drag belongs to the items).
            RectTransform scrollRt = GRect("Scroll", root);
            GFill(scrollRt, GPad, GFootH, viewRight, top);
            var scroll = scrollRt.gameObject.AddComponent<GridScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 32f;

            RectTransform vp = GRect("Viewport", scrollRt);
            GFill(vp, 0f, 0f, 0f, 0f);
            vp.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // invisible, but catches the wheel
            vp.gameObject.AddComponent<RectMask2D>();

            RectTransform content = GRect("Content", vp);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            scroll.viewport = vp;
            scroll.content = content;
            w.Viewport = vp;
            w.Content = content;

            // A slim scrollbar between the sections and the tabs, shown only when there's more to see.
            RectTransform sbRt = GRect("Scrollbar", root);
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = Vector2.one;
            sbRt.pivot = new Vector2(1f, 1f);
            sbRt.offsetMin = new Vector2(-(barRight + GScrollbarW), GFootH);
            sbRt.offsetMax = new Vector2(-barRight, -top);
            sbRt.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
            RectTransform slide = GRect("Sliding Area", sbRt);
            GFill(slide, 0f, 0f, 0f, 0f);
            Image handle = GImage(slide, "Handle", Color.white, raycast: true);
            GFill(handle.rectTransform, 0f, 0f, 0f, 0f);
            var sb = sbRt.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handle.rectTransform;
            sb.targetGraphic = handle;
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock sbc = sb.colors;
            sbc.normalColor = new Color(1f, 1f, 1f, 0.2f);
            sbc.highlightedColor = new Color(1f, 1f, 1f, 0.32f);
            sbc.pressedColor = new Color(1f, 1f, 1f, 0.42f);
            sbc.selectedColor = sbc.normalColor;
            sbc.colorMultiplier = 1f;
            sb.colors = sbc;
            scroll.verticalScrollbar = sb;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // The tab strip down the right edge.
            RectTransform tabs = GRect("Tabs", root);
            tabs.anchorMin = new Vector2(1f, 0f);
            tabs.anchorMax = Vector2.one;
            tabs.pivot = new Vector2(1f, 1f);
            tabs.offsetMin = new Vector2(-(tabsRight + GTabW), GFootH);
            tabs.offsetMax = new Vector2(-tabsRight, -top);
            tabs.gameObject.AddComponent<RectMask2D>();
            w.TabStrip = tabs;

            // A status line, then the footer buttons — each acting toward the other window.
            w.Status = GText(root, "Status", 11, TextAnchor.MiddleLeft, GTextDim);
            RectTransform st = w.Status.rectTransform;
            st.anchorMin = Vector2.zero;
            st.anchorMax = new Vector2(1f, 0f);
            st.pivot = Vector2.zero;
            st.offsetMin = new Vector2(GPad + 2f, GPad + GButtonH + 3f);
            st.offsetMax = new Vector2(-(GPad + 2f), GPad + GButtonH + 19f);

            RectTransform foot = GRect("Buttons", root);
            foot.anchorMin = Vector2.zero;
            foot.anchorMax = new Vector2(1f, 0f);
            foot.pivot = Vector2.zero;
            foot.offsetMin = new Vector2(GPad, GPad);
            foot.offsetMax = new Vector2(-(GPad + 12f), GPad + GButtonH);
            FooterButton(foot, 0.00f, 0.21f, "Transfer All", () => TransferAll(left), null);
            FooterButton(foot, 0.21f, 0.46f, "Transfer only...", () => ShowTransferOnly(left), null);
            FooterButton(foot, 0.46f, 0.61f, "Tidy up", () => TidyUp(left), null);
            FooterButton(foot, 0.61f, 1.00f, "Transfer / sort options", ShowGridOptions, GridSprites.Gear());

            // The resize grip in the bottom-right corner.
            Image grip = GImage(root, "Resize Grip", new Color(1f, 1f, 1f, 0.45f), raycast: true);
            grip.sprite = GridSprites.Grip();
            RectTransform grt = grip.rectTransform;
            grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(1f, 0f);
            grt.anchoredPosition = new Vector2(-2f, 2f);
            grt.sizeDelta = new Vector2(12f, 12f);
            var sizer = grip.gameObject.AddComponent<GridDragHandle>();
            sizer.Target = root;
            sizer.Resize = true;
            sizer.MinSize = new Vector2(left ? 400f : 485f, 300f);   // the loot window keeps a 12-wide ground grid in view
            sizer.Changed = () =>
            {
                KeepOnScreen(root);
                MarkDirty();   // section headers re-fit their names to the new width
            };
            return w;
        }

        // A footer button stretched over a fraction of the footer, so the row scales with the window.
        private void FooterButton(RectTransform foot, float from, float to, string label, UnityEngine.Events.UnityAction onClick, Sprite icon)
        {
            Button b = GButton(foot, label, label, 12, GButtonBg, onClick, out Text text);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(from, 0f);
            rt.anchorMax = new Vector2(to, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(from > 0f ? 2f : 0f, 0f);
            rt.offsetMax = new Vector2(to < 1f ? -2f : 0f, 0f);
            if (icon == null) return;

            Image ic = GImage(rt, "Icon", new Color(1f, 1f, 1f, 0.85f));
            ic.sprite = icon;
            RectTransform irt = ic.rectTransform;
            irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(6f, 0f);
            irt.sizeDelta = new Vector2(13f, 13f);
            text.rectTransform.offsetMin = new Vector2(22f, 0f);
        }

        // The worn-slot strip: a tile per PlayerClothing slot (Head..Mask), in two rows.
        private void BuildEquipStrip(GridWindow win)
        {
            ClothingSlot[] slots = PlayerClothing.Slots;
            _equip = new EquipView[slots.Length];
            float top = GTitleH + 1f + GPad;
            RectTransform strip = GRect("Worn", win.Root);
            strip.anchorMin = new Vector2(0f, 1f);
            strip.anchorMax = Vector2.one;
            strip.pivot = new Vector2(0f, 1f);
            strip.offsetMin = new Vector2(GPad, -(top + GEquipH));
            strip.offsetMax = new Vector2(-GPad, -top);

            int perRow = (slots.Length + 1) / 2;
            for (int i = 0; i < slots.Length; i++)
            {
                int row = i / perRow, col = i % perRow;
                var ev = new EquipView { Slot = slots[i] };
                RectTransform rt = GRect("Slot " + slots[i], strip);
                rt.anchorMin = new Vector2((float)col / perRow, 1f);
                rt.anchorMax = new Vector2((float)(col + 1) / perRow, 1f);
                rt.pivot = new Vector2(0f, 1f);
                float y = -row * (GEquipRowH + GEquipGap);
                rt.offsetMin = new Vector2(col == 0 ? 0f : GEquipGap * 0.5f, y - GEquipRowH);
                rt.offsetMax = new Vector2(col == perRow - 1 ? 0f : -GEquipGap * 0.5f, y);
                ev.Rt = rt;
                ev.Bg = rt.gameObject.AddComponent<Image>();
                ev.Bg.color = GridSprites.CellFill;
                GBorderLines(rt, GridSprites.CellLine);
                AddRelay(rt.gameObject, GridElement.EquipSlot, i);

                ev.Caption = GText(rt, "Slot", 9, TextAnchor.UpperLeft, GTextDim, shadow: false);
                GFill(ev.Caption.rectTransform, 3f, 0f, 2f, 1f);
                ev.Caption.text = slots[i].ToString().ToUpperInvariant();

                ev.Name = GText(rt, "Item", 11, TextAnchor.MiddleCenter, Color.white);
                GFill(ev.Name.rectTransform, 3f, 5f, 3f, 10f);
                BestFit(ev.Name, 8, 11);

                Image barBg = GImage(rt, "Bar", new Color(0f, 0f, 0f, 0.6f));
                ev.BarBg = barBg.rectTransform;
                ev.BarBg.anchorMin = Vector2.zero;
                ev.BarBg.anchorMax = new Vector2(1f, 0f);
                ev.BarBg.pivot = Vector2.zero;
                ev.BarBg.offsetMin = new Vector2(3f, 2f);
                ev.BarBg.offsetMax = new Vector2(-3f, 4f);
                ev.BarFillImg = GImage(ev.BarBg, "Fill", Color.white);
                ev.BarFill = ev.BarFillImg.rectTransform;
                ev.BarFill.anchorMin = Vector2.zero;
                ev.BarFill.anchorMax = Vector2.one;
                ev.BarFill.offsetMin = Vector2.zero;
                ev.BarFill.offsetMax = Vector2.zero;
                _equip[i] = ev;
            }
        }

        private SectionView NewSectionView(GridWindow win)
        {
            var v = new SectionView { Window = win };
            v.Root = GRect("Section", win.Content);
            v.Root.anchorMin = new Vector2(0f, 1f);
            v.Root.anchorMax = Vector2.one;
            v.Root.pivot = new Vector2(0f, 1f);

            // Header strip "<Name>  [V]": clicking anywhere on it folds the section away (or back).
            RectTransform head = GRect("Header", v.Root);
            head.anchorMin = new Vector2(0f, 1f);
            head.anchorMax = Vector2.one;
            head.pivot = new Vector2(0f, 1f);
            head.offsetMin = new Vector2(0f, -GSecHeadH);
            head.offsetMax = Vector2.zero;
            head.gameObject.SetActive(false);   // tint the button before it wakes
            var headImg = head.gameObject.AddComponent<Image>();
            headImg.color = Color.white;
            var headBtn = head.gameObject.AddComponent<Button>();
            headBtn.targetGraphic = headImg;
            headBtn.colors = Tints(GHeaderBg);
            headBtn.onClick.AddListener(() => ToggleSection(v));
            MouseOnly(headBtn);
            head.gameObject.SetActive(true);

            v.HeaderText = GText(head, "Name", 12, TextAnchor.MiddleLeft, Color.white);
            GFill(v.HeaderText.rectTransform, 6f, 0f, 6f, 0f);
            Image box = GImage(head, "Fold", new Color(1f, 1f, 1f, 0.06f));
            v.Toggle = box.rectTransform;
            v.Toggle.anchorMin = v.Toggle.anchorMax = v.Toggle.pivot = new Vector2(0f, 0.5f);
            v.Toggle.sizeDelta = new Vector2(16f, 14f);
            GBorderLines(v.Toggle, GBorder);
            v.ToggleText = GText(v.Toggle, "Glyph", 8, TextAnchor.MiddleCenter, Color.white, shadow: false);
            GFill(v.ToggleText.rectTransform, 0f, 0f, 0f, 0f);

            // The row: an icon tile with the section's load under it, and the cells beside it.
            v.Row = GRect("Row", v.Root);
            v.Row.anchorMin = new Vector2(0f, 1f);
            v.Row.anchorMax = Vector2.one;
            v.Row.pivot = new Vector2(0f, 1f);
            v.Row.offsetMin = new Vector2(0f, -(GSecHeadH + 4f));
            v.Row.offsetMax = new Vector2(0f, -(GSecHeadH + 4f));

            v.Icon = GImage(v.Row, "Icon", GHeaderBg);
            GPlace(v.Icon.rectTransform, 4f, 0f, GIconTile, GIconTile);
            GBorderLines(v.Icon.rectTransform, GBorder);
            v.IconGlyph = GText(v.Icon.rectTransform, "Glyph", 14, TextAnchor.MiddleCenter, Color.white);
            GFill(v.IconGlyph.rectTransform, 0f, 0f, 0f, 0f);
            v.IconGlyph.fontStyle = FontStyle.Bold;
            v.Caption = GText(v.Row, "Load", 10, TextAnchor.UpperCenter, GTextDim);
            GPlace(v.Caption.rectTransform, 0f, -(GIconTile + 2f), GIconTile + 8f, 14f);

            Image cells = GImage(v.Row, "Cells", Color.white);
            cells.sprite = GridSprites.Cell();
            cells.type = Image.Type.Tiled;
            v.Grid = cells.rectTransform;
            GPlace(v.Grid, GGridX, 0f, Cell + 1f, Cell + 1f);
            return v;
        }

        private TileView NewTileView(RectTransform parent, bool interactive)
        {
            var t = new TileView();
            t.Rt = GRect("Item", parent);
            t.Rt.anchorMin = t.Rt.anchorMax = t.Rt.pivot = new Vector2(0f, 1f);
            t.Bg = t.Rt.gameObject.AddComponent<Image>();
            t.Bg.raycastTarget = interactive;

            t.Name = GText(t.Rt, "Name", 10, TextAnchor.MiddleCenter, Color.white);
            GFill(t.Name.rectTransform, 2f, 5f, 2f, 2f);
            BestFit(t.Name, 7, 11);
            t.Count = GText(t.Rt, "Count", 10, TextAnchor.UpperLeft, Color.white);
            GFill(t.Count.rectTransform, 2f, 0f, 1f, 0f);
            t.Count.fontStyle = FontStyle.Bold;

            // The condition / charge bar along the bottom.
            Image barBg = GImage(t.Rt, "Bar", new Color(0f, 0f, 0f, 0.6f));
            t.BarBg = barBg.rectTransform;
            t.BarBg.anchorMin = Vector2.zero;
            t.BarBg.anchorMax = new Vector2(1f, 0f);
            t.BarBg.pivot = Vector2.zero;
            t.BarBg.offsetMin = new Vector2(2f, 2f);
            t.BarBg.offsetMax = new Vector2(-2f, 5f);
            t.BarFillImg = GImage(t.BarBg, "Fill", Color.white);
            t.BarFill = t.BarFillImg.rectTransform;
            t.BarFill.anchorMin = Vector2.zero;
            t.BarFill.anchorMax = Vector2.one;
            t.BarFill.offsetMin = Vector2.zero;
            t.BarFill.offsetMax = Vector2.zero;

            // The hand badge on the weapon in hand.
            t.Badge = GImage(t.Rt, "In Hand", new Color(0.55f, 0.95f, 0.6f));
            t.Badge.sprite = GridSprites.Hand();
            RectTransform br = t.Badge.rectTransform;
            br.anchorMin = br.anchorMax = br.pivot = Vector2.one;
            br.anchoredPosition = new Vector2(-2f, -2f);
            br.sizeDelta = new Vector2(12f, 12f);
            t.Badge.enabled = false;

            if (interactive) t.Relay = AddRelay(t.Rt.gameObject, GridElement.Tile, _tiles.Count);
            return t;
        }

        private TabView NewTab(GridWindow w, int index)
        {
            var t = new TabView();
            t.Bg = GImage(w.TabStrip, "Tab", GTitleBg, raycast: true);
            t.Rt = t.Bg.rectTransform;
            GPlace(t.Rt, 0f, -index * (GTabW + GTabGap), GTabW, GTabW);
            GBorderLines(t.Rt, GBorder);

            Image fill = GImage(t.Rt, "Search", new Color(0.5f, 0.72f, 0.9f, 0.45f));
            t.Fill = fill.rectTransform;
            t.Fill.anchorMin = Vector2.zero;
            t.Fill.anchorMax = new Vector2(1f, 0f);
            t.Fill.pivot = new Vector2(0.5f, 0f);
            SetTabFill(t, 0f);

            t.Mark = GImage(t.Rt, "Selected", GAccent);
            RectTransform m = t.Mark.rectTransform;
            m.anchorMin = Vector2.zero;
            m.anchorMax = new Vector2(0f, 1f);
            m.pivot = new Vector2(0f, 0.5f);
            m.offsetMin = new Vector2(1f, 1f);
            m.offsetMax = new Vector2(3f, -1f);
            t.Mark.enabled = false;

            t.Glyph = GText(t.Rt, "Glyph", 10, TextAnchor.MiddleCenter, Color.white);
            GFill(t.Glyph.rectTransform, 0f, 0f, 0f, 0f);
            t.Glyph.fontStyle = FontStyle.Bold;
            AddRelay(t.Rt.gameObject, w.Left ? GridElement.LeftTab : GridElement.RightTab, index);
            return t;
        }

        private GridPointerRelay AddRelay(GameObject go, GridElement element, int index)
        {
            var r = go.AddComponent<GridPointerRelay>();
            r.Owner = this;
            r.Element = element;
            r.Index = index;
            return r;
        }
        #endregion

        #region Refresh
        private void RefreshGrid()
        {
            if (_player == null || _leftWin == null) return;
            _nearStove = _player.CanCookHere();
            _tilesUsed = 0;
            RefreshSurvivorWindow();
            RefreshLootWindow();
            for (int i = _tilesUsed; i < _tiles.Count; i++) ReleaseTile(_tiles[i]);
            BindTabs();
            _tabSig = TabSignature();

            if (_scrollToKey != null)
            {
                SectionView v = ViewByKey(_leftWin, _scrollToKey);
                if (v != null) ScrollWindowTo(_leftWin, v.Top);
                _scrollToKey = null;
            }

            // Whatever is now under a pointer that hasn't moved gets its highlight and tooltip back.
            if (_hover != null && _hover.isActiveAndEnabled) GridPointerEnter(_hover);
            else if (_tipOwner != null) { HideTooltip(null); _tipOwner = null; }
        }

        private void RefreshSurvivorWindow()
        {
            GridWindow w = _leftWin;
            float load = _player.Load;
            w.Title.text = FitText(w.Title, GameSession.SurvivorName + "   " + _player.Weight.ToString("0.0") + " / " + _player.Capacity.ToString("0.0"),
                "", Mathf.Max(60f, w.Title.rectTransform.rect.width));
            w.Title.color = load >= 1f ? GOver : load >= CloneZConfig.EncumbranceStart ? AmberCol : Color.white;
            RefreshEquip();

            InventoryLayout layout = _player.Layout;
            float y = 0f;
            int used = 0;
            if (layout != null)
            {
                for (int i = 0; i < layout.Sections.Count; i++)
                {
                    GridSection s = layout.Sections[i];
                    float kg = layout.WeightIn(s);
                    bool over = !s.Unlimited && kg > s.Capacity + 0.001f;
                    string cap = s.Unlimited ? kg.ToString("0.0") + " kg" : kg.ToString("0.#") + " / " + s.Capacity.ToString("0.#");
                    y = BindSection(SectionAt(w, used++), GridSide.Player, s, _player.Inventory, s, s.Width, layout.RowsShown(s),
                        s.Name, SectionGlyph(s), s.Source != null ? s.Source.Tint : GPocketTint, cap, over, y);
                }
            }
            FinishSections(w, used, y);

            // Every placed stack becomes a tile in its section (a stack about to be dropped as overflow has no spot).
            ItemStack inHand = _player.EquippedWeapon != null ? _player.EquippedStack : null;
            List<ItemStack> stacks = _player.Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack st = stacks[i];
                if (st.Def == null || !st.IsPlaced) continue;
                SectionView v = ViewOf(w, st.Section);
                if (v != null && !v.Collapsed) BindTile(v, st, null, st, st.GridX, st.GridY, st.Rotated, st == inHand);
            }
        }

        private void RefreshLootWindow()
        {
            GridWindow w = _rightWin;
            InventoryLayout layout = _openInventory != null && _openContainer != null ? _openContainer.EnsureLayout() : null;

            // A different container (or the ground) starts scrolled to the top.
            object shown = layout != null ? (object)_openContainer : GroundKey;
            if (!ReferenceEquals(shown, _lootShown))
            {
                _lootShown = shown;
                w.Content.anchoredPosition = Vector2.zero;
            }

            if (layout != null && layout.Sections.Count > 0)
            {
                GridSection s = layout.Sections[0];
                float kg = _openInventory.CurrentWeight;
                w.Title.text = FitText(w.Title, _openContainer.DisplayName + "   " + kg.ToString("0.0") + " kg", "",
                    Mathf.Max(60f, w.Title.rectTransform.rect.width));
                w.Title.color = Color.white;
                float y = BindSection(SectionAt(w, 0), GridSide.Container, s, _openInventory, s, s.Width, layout.RowsShown(s),
                    _openContainer.DisplayName, ContainerGlyph(_openContainer), ContainerColor(_openContainer), kg.ToString("0.0") + " kg", false, 0f);
                FinishSections(w, 1, y);

                SectionView v = w.Sections[0];
                if (v.Collapsed) return;
                List<ItemStack> stacks = _openInventory.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                {
                    ItemStack st = stacks[i];
                    if (st.Def != null && ReferenceEquals(st.Section, s)) BindTile(v, st, null, st, st.GridX, st.GridY, st.Rotated, false);
                }
                return;
            }

            // No container open: the ground within reach, sorted and packed into a 12-wide grid.
            GatherGround();
            _groundSig = GroundSignature();
            _groundDefs.Clear();
            float floorKg = 0f;
            for (int i = 0; i < _ground.Count; i++)
            {
                _groundDefs.Add(_ground[i].Def);
                floorKg += _ground[i].Def.Weight * _ground[i].Count;
            }
            int rows = InventoryLayout.PackGrowable(_groundDefs, GroundCols, _groundCells, _groundRot);
            w.Title.text = "Ground   " + floorKg.ToString("0.0") + " kg";
            w.Title.color = Color.white;
            float gy = BindSection(SectionAt(w, 0), GridSide.Ground, null, null, GroundKey, GroundCols, Mathf.Max(GroundMinRows, rows + 1),
                "Ground", "Grd", GGroundTint, floorKg.ToString("0.#") + " / " + GroundRefKg.ToString("0"), floorKg > GroundRefKg, 0f);
            FinishSections(w, 1, gy);

            SectionView gv = w.Sections[0];
            if (gv.Collapsed) return;
            for (int i = 0; i < _ground.Count; i++)
            {
                RectInt r = _groundCells[i];
                BindTile(gv, null, _ground[i], DisplayStackFor(_ground[i]), r.x, r.y, _groundRot[i], false);
            }
        }

        // Lays out one section block from the top of its window's content; returns the next block's top.
        private float BindSection(SectionView v, GridSide side, GridSection section, Inventory inv, object key,
            int cols, int rows, string name, string glyph, Color tint, string caption, bool over, float y)
        {
            v.Side = side;
            v.Section = section;
            v.Inventory = inv;
            v.Key = key;
            v.Cols = Mathf.Max(1, cols);
            v.Rows = Mathf.Max(1, rows);
            v.Collapsed = key != null && _collapsed.Contains(key);
            v.Top = y;

            float width = Mathf.Max(80f, v.Window.Viewport.rect.width);
            v.HeaderText.text = FitText(v.HeaderText, name, "", width - 44f);
            float nameW = Mathf.Min(v.HeaderText.preferredWidth, width - 44f);
            v.Toggle.anchoredPosition = new Vector2(6f + nameW + 8f, 0f);
            v.ToggleText.text = v.Collapsed ? "\u25ba" : "\u25bc";

            float h = GSecHeadH;
            if (v.Row.gameObject.activeSelf == v.Collapsed) v.Row.gameObject.SetActive(!v.Collapsed);
            if (!v.Collapsed)
            {
                tint.a = 1f;
                v.Icon.color = Color.Lerp(GridSprites.CellFill, tint, 0.45f);
                v.IconGlyph.text = glyph;
                v.Caption.text = caption;
                v.Caption.color = over ? GOver : GTextDim;
                v.Grid.sizeDelta = new Vector2(v.Cols * Cell + 1f, v.Rows * Cell + 1f);
                h += 4f + Mathf.Max(GIconTile + 16f, v.Rows * Cell + 1f);
            }
            h += GSecGap;
            v.Root.anchoredPosition = new Vector2(0f, -y);
            v.Root.sizeDelta = new Vector2(0f, h);
            return y + h;
        }

        // Hides the section views a window no longer needs and sizes its scrolling content.
        private static void FinishSections(GridWindow w, int used, float height)
        {
            w.SectionsUsed = used;
            for (int i = used; i < w.Sections.Count; i++)
                if (w.Sections[i].Root.gameObject.activeSelf) w.Sections[i].Root.gameObject.SetActive(false);
            w.Content.sizeDelta = new Vector2(0f, Mathf.Max(height, 1f));
        }

        private SectionView SectionAt(GridWindow w, int index)
        {
            while (w.Sections.Count <= index) w.Sections.Add(NewSectionView(w));
            SectionView v = w.Sections[index];
            if (!v.Root.gameObject.activeSelf) v.Root.gameObject.SetActive(true);
            return v;
        }

        private static SectionView ViewOf(GridWindow w, GridSection s)
        {
            for (int i = 0; i < w.SectionsUsed; i++)
                if (ReferenceEquals(w.Sections[i].Section, s)) return w.Sections[i];
            return null;
        }

        private static SectionView ViewByKey(GridWindow w, object key)
        {
            for (int i = 0; i < w.SectionsUsed; i++)
                if (ReferenceEquals(w.Sections[i].Key, key)) return w.Sections[i];
            return null;
        }

        private void BindTile(SectionView v, ItemStack live, WorldItem ground, ItemStack shown, int x, int y, bool rotated, bool inHand)
        {
            TileView t;
            if (_tilesUsed < _tiles.Count) t = _tiles[_tilesUsed];
            else
            {
                t = NewTileView(v.Grid, interactive: true);
                _tiles.Add(t);
            }
            _tilesUsed++;
            if (t.Rt.parent != v.Grid) t.Rt.SetParent(v.Grid, false);
            if (!t.Rt.gameObject.activeSelf) t.Rt.gameObject.SetActive(true);

            Vector2Int f = InventoryLayout.Footprint(shown.Def, rotated);
            t.View = v;
            t.Stack = live;
            t.Ground = ground;
            t.Shown = shown;
            t.Rotated = rotated;
            t.InHand = inHand;
            t.X = x;
            t.Y = y;
            t.W = f.x;
            t.H = f.y;
            // Inset one pixel so the cell lines frame the item.
            t.Rt.anchoredPosition = new Vector2(x * Cell + 1f, -(y * Cell + 1f));
            t.Rt.sizeDelta = new Vector2(f.x * Cell - 1f, f.y * Cell - 1f);
            PaintTile(t);
            if (IsDragSource(t)) t.Bg.color = Faded(t.BaseColor);
        }

        private static void ReleaseTile(TileView t)
        {
            t.View = null;
            t.Stack = null;
            t.Ground = null;
            t.Shown = null;
            if (t.Rt.gameObject.activeSelf) t.Rt.gameObject.SetActive(false);
        }

        // An item tile: tinted by the item (soured for stale / rotten food), its name (a short label when
        // it's one cell wide), the stack count top-left, a wear / charge bar and the in-hand badge.
        private void PaintTile(TileView t)
        {
            ItemStack s = t.Shown;
            ItemDef d = s.Def;
            t.BaseColor = TileColor(d, s);
            t.Bg.color = t.BaseColor;

            bool narrow = t.W == 1;
            t.Name.text = narrow ? ShortLabel(d) : d.DisplayName;
            t.Name.resizeTextMaxSize = narrow ? 10 : 11;
            t.Name.rectTransform.offsetMax = new Vector2(-2f, t.H == 1 ? -9f : -2f);   // a lone cell leaves the count its corner
            t.Count.text = s.Count > 1 ? CountText(s.Count) : "";

            bool bar = TileBar(s, out float fill, out Color col);
            if (t.BarBg.gameObject.activeSelf != bar) t.BarBg.gameObject.SetActive(bar);
            if (bar)
            {
                t.BarFill.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
                t.BarFillImg.color = col;
            }
            t.Badge.enabled = t.InHand;
        }

        // Worn slots: the garment's name and wear (or a device's charge); a bare slot reads dim.
        private void RefreshEquip()
        {
            if (_equip == null) return;
            for (int i = 0; i < _equip.Length; i++)
            {
                EquipView ev = _equip[i];
                PlayerClothing.WornPiece p = _clothing != null ? _clothing.GetPiece(ev.Slot) : default;
                if (p.Exists)
                {
                    Color tint = p.Def.Tint;
                    tint.a = 1f;
                    ev.BaseColor = Color.Lerp(GridSprites.CellFill, tint, 0.4f);
                    ev.Name.text = p.Def.DisplayName;
                    ev.Caption.color = new Color(0.82f, 0.85f, 0.9f, 0.85f);
                }
                else
                {
                    ev.BaseColor = new Color(GridSprites.CellFill.r, GridSprites.CellFill.g, GridSprites.CellFill.b, 0.55f);
                    ev.Name.text = "";
                    ev.Caption.color = new Color(1f, 1f, 1f, 0.3f);
                }
                ev.Bg.color = ev.BaseColor;
                UpdateEquipBar(ev, p);
            }
        }

        private static void UpdateEquipBar(EquipView ev, PlayerClothing.WornPiece p)
        {
            bool bar = p.Exists && p.Def.IsDamageable;
            if (ev.BarBg.gameObject.activeSelf != bar) ev.BarBg.gameObject.SetActive(bar);
            if (!bar) return;
            float f = p.Condition01;
            ev.BarFill.anchorMax = new Vector2(f, 1f);
            ev.BarFillImg.color = p.Def.UsesCharge ? GChargeCol : WearColor(f);
        }

        // Left: a tab per section. Right: the ground, then every container in reach (nearest first).
        private void BindTabs()
        {
            GridWindow l = _leftWin;
            l.TabsUsed = 0;
            for (int i = 0; i < l.SectionsUsed; i++)
            {
                SectionView v = l.Sections[i];
                TabView t = TabAt(l, l.TabsUsed++);
                t.Section = v;
                t.Container = null;
                Color tint = v.Section != null && v.Section.Source != null ? v.Section.Source.Tint : GPocketTint;
                tint.a = 1f;
                t.BaseColor = Color.Lerp(GTitleBg, tint, 0.3f);
                t.Bg.color = t.BaseColor;
                t.Glyph.text = v.Section != null ? SectionGlyph(v.Section) : "";
                t.Glyph.color = v.Collapsed ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
                t.Mark.enabled = false;
                SetTabFill(t, 0f);
            }
            HideTabsFrom(l);

            GridWindow r = _rightWin;
            r.TabsUsed = 0;
            _searchTab = null;
            LootContainer searching = _interactor != null ? _interactor.SearchTarget : null;
            GatherNearby();

            TabView g = TabAt(r, r.TabsUsed++);
            g.Section = null;
            g.Container = null;
            g.BaseColor = Color.Lerp(GTitleBg, GGroundTint, 0.35f);
            g.Bg.color = g.BaseColor;
            g.Glyph.text = "Grd";
            g.Glyph.color = Color.white;
            g.Mark.enabled = _openInventory == null;
            SetTabFill(g, 0f);

            for (int i = 0; i < _nearby.Count; i++)
            {
                LootContainer c = _nearby[i];
                TabView t = TabAt(r, r.TabsUsed++);
                t.Section = null;
                t.Container = c;
                t.BaseColor = Color.Lerp(GTitleBg, ContainerColor(c), 0.4f);
                t.Bg.color = t.BaseColor;
                t.Glyph.text = ContainerGlyph(c);
                bool unsearched = !c.Searched && c.SearchTime > 0f;
                t.Glyph.color = unsearched ? AmberCol : Color.white;   // still to be rummaged through
                t.Mark.enabled = c == _openContainer;
                bool busy = c == searching;
                if (busy) _searchTab = t;
                SetTabFill(t, busy ? _interactor.SearchProgress : 0f);
            }
            HideTabsFrom(r);
            _tabSearching = searching;
        }

        private TabView TabAt(GridWindow w, int index)
        {
            while (w.Tabs.Count <= index) w.Tabs.Add(NewTab(w, w.Tabs.Count));
            TabView t = w.Tabs[index];
            if (!t.Rt.gameObject.activeSelf) t.Rt.gameObject.SetActive(true);
            return t;
        }

        private static void HideTabsFrom(GridWindow w)
        {
            for (int i = w.TabsUsed; i < w.Tabs.Count; i++)
            {
                TabView t = w.Tabs[i];
                t.Section = null;
                t.Container = null;
                if (t.Rt.gameObject.activeSelf) t.Rt.gameObject.SetActive(false);
            }
        }

        private static void SetTabFill(TabView t, float progress)
        {
            bool on = progress > 0f;
            if (t.Fill.gameObject.activeSelf != on) t.Fill.gameObject.SetActive(on);
            t.Fill.offsetMin = new Vector2(1f, 1f);
            t.Fill.offsetMax = new Vector2(-1f, 1f + (GTabW - 2f) * Mathf.Clamp01(progress));
        }
        #endregion

        #region Ticking
        // Runs every frame while the grid view is open.
        private void TickGrid()
        {
            ConsumeEscape();
            if (_drag.Active) UpdateDrag();
            TickGridStatus(_leftWin);
            TickGridStatus(_rightWin);

            _gridPoll -= Time.unscaledDeltaTime;
            if (_gridPoll <= 0f)
            {
                _gridPoll = GPollInterval;
                PollGround();
                PollTabs();
                RefreshCharge();
            }
            UpdateGridSearch();
        }

        // Ground items raise no events, so the floor is re-read a few times a second while it shows.
        private void PollGround()
        {
            if (_openInventory != null || _player == null) return;
            GatherGround();
            int sig = GroundSignature();
            if (sig != _groundSig) MarkDirty();
        }

        // Containers come into and out of reach (a corpse drops beside you, or rots away).
        private void PollTabs()
        {
            if (_player == null) return;
            GatherNearby();
            if (TabSignature() == _tabSig) return;
            BindTabs();
            _tabSig = TabSignature();
        }

        // Devices drain silently (no Changed event per tick), so their charge bars are re-read here.
        private void RefreshCharge()
        {
            for (int i = 0; i < _tilesUsed; i++)
            {
                TileView t = _tiles[i];
                if (t.Stack == null || t.Shown == null || t.Shown.Def == null || !t.Shown.Def.UsesCharge) continue;
                t.BarFill.anchorMax = new Vector2(t.Stack.Charge01, 1f);
            }
            if (_equip == null || _clothing == null) return;
            for (int i = 0; i < _equip.Length; i++)
            {
                PlayerClothing.WornPiece p = _clothing.GetPiece(_equip[i].Slot);
                if (p.Exists && p.Def.UsesCharge) _equip[i].BarFill.anchorMax = new Vector2(p.Condition01, 1f);
            }
        }

        // A rummage started from a tab: its bar rises on the tab, and the status line counts it up.
        private void UpdateGridSearch()
        {
            LootContainer target = _interactor != null ? _interactor.SearchTarget : null;
            if (target != _tabSearching)
            {
                BindTabs();
                _tabSig = TabSignature();
                _searchPct = -1;
            }
            if (target == null) return;

            float p = Mathf.Clamp01(_interactor.SearchProgress);
            if (_searchTab != null) SetTabFill(_searchTab, p);
            int pct = Mathf.RoundToInt(p * 100f);
            if (pct != _searchPct)
            {
                _searchPct = pct;
                GridStatus(_rightWin, "Searching " + target.DisplayName + "\u2026  " + pct + "%", false);
            }
            _rightWin.StatusTimer = 0.6f;   // fades shortly after the search ends
        }

        private int TabSignature()
        {
            unchecked
            {
                int h = 23;
                h = h * 31 + Identity(_openContainer);
                h = h * 31 + Identity(_interactor != null ? _interactor.SearchTarget : null);
                h = h * 31 + _nearby.Count;
                for (int i = 0; i < _nearby.Count; i++)
                {
                    h = h * 31 + Identity(_nearby[i]);
                    h = h * 31 + (_nearby[i].Searched ? 1 : 0);
                }
                return h;
            }
        }

        private void GatherGround()
        {
            _ground.Clear();
            if (_player == null) return;
            WorldItem.Near(_player.transform.position, CloneZConfig.InteractRange, _ground);
            _ground.Sort(_groundOrder);
        }

        private int GroundSignature()
        {
            unchecked
            {
                int h = 29 + _ground.Count;
                for (int i = 0; i < _ground.Count; i++)
                {
                    h = h * 31 + (int)_ground[i].Handle.Id;
                    h = h * 31 + _ground[i].Count;
                }
                return h;
            }
        }

        // The ground's order follows the chosen sort ("Tidy up" there only re-sorts); ties fall back to
        // the name, the pile size and finally the pile's id, so the packing holds still between refreshes.
        private int CompareGround(WorldItem a, WorldItem b)
        {
            int c;
            switch (_gridSort)
            {
                case GridSortMode.Category: c = ((int)a.Def.Category).CompareTo((int)b.Def.Category); break;
                case GridSortMode.Weight: c = (b.Def.Weight * b.Count).CompareTo(a.Def.Weight * a.Count); break;
                case GridSortMode.Name: c = 0; break;
                default:
                    c = Area(b.Def).CompareTo(Area(a.Def));
                    if (c == 0) c = ((int)a.Def.Category).CompareTo((int)b.Def.Category);
                    break;
            }
            if (c != 0) return c;
            c = string.CompareOrdinal(a.Def.DisplayName, b.Def.DisplayName);
            if (c != 0) return c;
            c = b.Count.CompareTo(a.Count);
            return c != 0 ? c : a.Handle.Id.CompareTo(b.Handle.Id);
        }

        private static int Area(ItemDef d)
        {
            Vector2Int f = InventoryLayout.BaseFootprint(d);
            return f.x * f.y;
        }
        #endregion

        #region Window helpers
        // Keeps a window's title bar on screen and the window no larger than the screen.
        private void KeepOnScreen(RectTransform win)
        {
            if (win == null || _gridRootRt == null) return;
            Rect r = _gridRootRt.rect;
            if (r.width <= 0f || r.height <= 0f) return;
            Vector2 size = win.sizeDelta;
            size.x = Mathf.Min(size.x, r.width - 8f);
            size.y = Mathf.Min(size.y, r.height - 8f);
            win.sizeDelta = size;
            Vector2 p = win.anchoredPosition;   // its top-left, from the screen's top-left
            p.x = Mathf.Clamp(p.x, -(size.x - 80f), r.width - 80f);
            p.y = Mathf.Clamp(p.y, -(r.height - GTitleH - 2f), 0f);
            win.anchoredPosition = p;
        }

        private static void ScrollWindowTo(GridWindow w, float top)
        {
            float max = Mathf.Max(0f, w.Content.rect.height - w.Viewport.rect.height);
            w.Content.anchoredPosition = new Vector2(0f, Mathf.Clamp(top, 0f, max));
        }

        private void ToggleSection(SectionView v)
        {
            if (v == null || v.Key == null) return;
            if (!_collapsed.Remove(v.Key)) _collapsed.Add(v.Key);
            MarkDirty();
        }

        // A left tab: unfold its section if needed and bring it to the top of the list.
        private void ScrollToSection(SectionView v)
        {
            if (v == null || v.Key == null) return;
            if (_collapsed.Remove(v.Key))
            {
                _scrollToKey = v.Key;   // scrolled to once the unfolded section has been laid out
                MarkDirty();
                return;
            }
            ScrollWindowTo(v.Window, v.Top);
        }

        private static void TickGridStatus(GridWindow w)
        {
            if (w == null || w.StatusTimer <= 0f) return;
            w.StatusTimer -= Time.unscaledDeltaTime;
            if (w.StatusTimer <= 0f) w.Status.text = "";
        }

        /// <summary>Shows a short message on a window's status line (amber for a refusal).</summary>
        private void GridStatus(GridWindow w, string message, bool warn = true)
        {
            if (w == null || w.Status == null) return;
            w.Status.text = message ?? "";
            w.Status.color = warn ? AmberCol : GTextDim;
            w.StatusTimer = string.IsNullOrEmpty(message) ? 0f : GStatusSeconds;
        }
        #endregion

        #region Tile readouts
        private static Color TileColor(ItemDef d, ItemStack s)
        {
            Color tint = d.Tint;
            tint.a = 1f;
            Color c = Color.Lerp(GridSprites.CellFill, tint, 0.42f);
            if (d.Perishable)
            {
                FoodState st = s.State;
                if (st == FoodState.Rotten) c = Color.Lerp(c, new Color(0.3f, 0.36f, 0.12f), 0.6f);
                else if (st == FoodState.Stale) c = Color.Lerp(c, new Color(0.5f, 0.44f, 0.16f), 0.35f);
            }
            c.a = 0.96f;
            return c;
        }

        // A device shows its charge (cyan); other worn-out-able gear its wear, green through amber to red.
        private static bool TileBar(ItemStack s, out float fill, out Color col)
        {
            ItemDef d = s.Def;
            if (d.UsesCharge) { fill = s.Charge01; col = GChargeCol; return true; }
            if (d.IsDamageable) { fill = s.Condition01; col = WearColor(fill); return true; }
            fill = 0f;
            col = Color.clear;
            return false;
        }

        private static Color WearColor(float c01) => c01 >= 0.5f
            ? Color.Lerp(AmberCol, GoodCol, (c01 - 0.5f) * 2f)
            : Color.Lerp(BadCol, AmberCol, c01 * 2f);

        private static Color Lit(Color c) { Color l = Color.Lerp(c, Color.white, 0.14f); l.a = c.a; return l; }
        private static Color Faded(Color c) { c.a *= 0.3f; return c; }

        private static string SectionGlyph(GridSection s)
        {
            if (s == null || !s.IsWorn) return "Inv";
            switch (s.Slot)
            {
                case ClothingSlot.Rig: return "Rig";
                case ClothingSlot.Thigh: return "Thg";
                default: return "Bag";
            }
        }

        // A one-cell-wide tile has room for a word: the noun of the name ("Canned Beans" → "Beans"), or a
        // leading calibre ("9mm Rounds" → "9mm"), cut to five letters.
        private string ShortLabel(ItemDef d)
        {
            if (_shortLabels.TryGetValue(d, out string label)) return label;
            string name = d.DisplayName ?? d.Id ?? "?";
            string[] words = name.Split(new[] { ' ', '-', '(', ')', ',', '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            string w = words.Length > 0 ? words[words.Length - 1] : name;
            if (words.Length > 1)
            {
                for (int i = 0; i < words[0].Length; i++)
                    if (char.IsDigit(words[0][i])) { w = words[0]; break; }
                if (w.Length < 3) w = words[0];
            }
            label = w.Length <= 5 ? w : w.Substring(0, 4) + ".";
            _shortLabels[d] = label;
            return label;
        }

        private static string CountText(int n)
        {
            if (n < 0 || n >= s_Counts.Length) return n.ToString();
            return s_Counts[n] ?? (s_Counts[n] = n.ToString());
        }
        #endregion

        #region Grid uGUI helpers
        private static RectTransform GRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        // Anchors and pivots at the top-left: x right, y down (negative).
        private static void GPlace(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        // Stretches over the parent, inset by the given margins.
        private static void GFill(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static Image GImage(RectTransform parent, string name, Color color, bool raycast = false)
        {
            RectTransform rt = GRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        private Text GText(RectTransform parent, string name, int size, TextAnchor align, Color color, bool shadow = true)
        {
            RectTransform rt = GRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            if (shadow) UITheme.AddShadow(t);
            return t;
        }

        private static void BestFit(Text t, int min, int max)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = max;
        }

        // A 1 px frame just inside a rect's edges.
        private static void GBorderLines(RectTransform rt, Color c)
        {
            Edge(rt, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -1f), Vector2.zero, c);
            Edge(rt, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1f), c);
            Edge(rt, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(1f, 0f), c);
            Edge(rt, new Vector2(1f, 0f), Vector2.one, new Vector2(-1f, 0f), Vector2.zero, c);
        }

        private static void Edge(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color c)
        {
            RectTransform rt = GImage(parent, "Edge", c).rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static ColorBlock Tints(Color normal)
        {
            ColorBlock cb = ColorBlock.defaultColorBlock;
            cb.normalColor = normal;
            cb.highlightedColor = Lit(normal);
            cb.pressedColor = Color.Lerp(normal, Color.white, 0.24f);
            cb.selectedColor = normal;
            cb.disabledColor = new Color(normal.r, normal.g, normal.b, normal.a * 0.45f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            return cb;
        }

        // Grid buttons are mouse-only: no keyboard navigation between them, and a click doesn't leave the
        // button selected (where Enter would press it again).
        private static void MouseOnly(Button btn)
        {
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() =>
            {
                if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            });
        }

        // A flat, bordered button with a best-fit label.
        private Button GButton(RectTransform parent, string name, string label, int fontSize, Color bg,
            UnityEngine.Events.UnityAction onClick, out Text text)
        {
            RectTransform rt = GRect(name, parent);
            rt.gameObject.SetActive(false);   // tint before the button wakes, so it doesn't flash white
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.colors = Tints(bg);
            if (onClick != null) btn.onClick.AddListener(onClick);
            MouseOnly(btn);
            GBorderLines(rt, new Color(GBorder.r, GBorder.g, GBorder.b, 0.6f));

            text = GText(rt, "Text", fontSize, TextAnchor.MiddleCenter, Color.white, shadow: false);
            GFill(text.rectTransform, 4f, 0f, 4f, 0f);
            BestFit(text, 8, fontSize);
            text.text = label;
            rt.gameObject.SetActive(true);
            return btn;
        }
        #endregion
    }
}
