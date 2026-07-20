using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A code-built loot/inventory panel. Shows the player's carried items on the
    /// left and (when a container is open) the container's contents on the right,
    /// with Take / Take All / Store / Use / Equip / Drop actions. Opening it sets
    /// <see cref="UIState.GameplayBlocked"/> so clicks don't leak into combat.
    /// Requires an EventSystem with an InputSystemUIInputModule in the scene.
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public LootContainer CurrentContainer => _openContainer;

        private PlayerInventory _player;
        private Font _font;

        private GameObject _root;              // dim backdrop + window (toggled on/off)
        private UIFader _fader;
        private RectTransform _playerList;
        private RectTransform _containerList;
        private GameObject _containerColumn;
        private Text _playerHeader;
        private Text _containerHeader;
        private Image _weightMeterFill;

        private LootContainer _openContainer;
        private System.Action _containerChanged;
        private bool _containerMode;

        // Hover tooltip
        private RectTransform _uiCanvasRect;
        private GameObject _tooltipRoot;
        private RectTransform _tooltipBg;
        private Text _tooltipText;
        private ItemDef _tooltipDef;

        public void Bind(PlayerInventory player)
        {
            _player = player;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            if (_player != null) _player.Inventory.Changed += OnPlayerInvChanged;
            Hide();
        }

        private void OnDestroy()
        {
            if (_player != null) _player.Inventory.Changed -= OnPlayerInvChanged;
            if (IsOpen) UIState.GameplayBlocked = false;
        }

        private void OnDisable() => UIState.GameplayBlocked = false;

        private void OnPlayerInvChanged() { if (IsOpen) Refresh(); }

        private void Update()
        {
            // If an open container is destroyed (e.g. a corpse times out), close gracefully.
            if (IsOpen && _containerMode && _openContainer == null)
            {
                Close();
                return;
            }

            if (_tooltipRoot != null && _tooltipRoot.activeSelf)
            {
                if (!IsOpen) { HideTooltip(null); return; }
                PositionTooltip();
            }
        }

        #region Tooltip
        public void ShowTooltip(ItemDef def)
        {
            if (def == null || _tooltipRoot == null) return;
            _tooltipDef = def;
            _tooltipText.text = def.Tooltip();

            const float w = 320f;
            _tooltipText.rectTransform.sizeDelta = new Vector2(w - 24f, 400f);
            float h = _tooltipText.preferredHeight + 20f;
            _tooltipBg.sizeDelta = new Vector2(w, h);
            _tooltipText.rectTransform.sizeDelta = new Vector2(w - 24f, h - 20f);

            _tooltipRoot.SetActive(true);
            _tooltipRoot.transform.SetAsLastSibling();
            PositionTooltip();
        }

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
        public void OpenContainer(LootContainer container)
        {
            if (container == null) return;
            _openContainer = container;
            _containerMode = true;
            container.Searched = true;
            _containerChanged ??= () => { if (IsOpen) Refresh(); };
            container.Contents.Changed += _containerChanged;
            OpenInternal();
        }

        public void OpenInventoryOnly()
        {
            _openContainer = null;
            _containerMode = false;
            OpenInternal();
        }

        private void OpenInternal()
        {
            IsOpen = true;
            UIState.GameplayBlocked = true;
            if (_fader != null) _fader.Show();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Refresh();
        }

        public void Close()
        {
            if (_openContainer != null && _containerChanged != null)
                _openContainer.Contents.Changed -= _containerChanged;
            _openContainer = null;
            IsOpen = false;
            UIState.GameplayBlocked = false;
            HideTooltip(null);   // the tooltip floats outside the modal, so dismiss it explicitly
            if (_fader != null) _fader.Hide();
        }

        private void Hide()
        {
            IsOpen = false;
            UIState.GameplayBlocked = false;
            if (_fader != null) _fader.Hide();
        }
        #endregion

        #region Actions
        private void TakeFromContainer(ItemStack stack)
        {
            if (_openContainer == null) return;
            _openContainer.Contents.TransferTo(_player.Inventory, stack, stack.Count, enforceWeight: true);
            Refresh();
        }

        private void StoreInContainer(ItemStack stack)
        {
            if (_openContainer == null) return;
            _player.Inventory.TransferTo(_openContainer.Contents, stack, stack.Count, enforceWeight: false);
            Refresh();
        }

        private void TakeAll()
        {
            if (_openContainer == null) return;
            _openContainer.Contents.TransferAllTo(_player.Inventory, enforceWeight: true);
            Refresh();
        }

        private void UseStack(ItemStack stack) { _player.Use(stack); Refresh(); }
        private void DropStack(ItemStack stack) { _player.Drop(stack, stack.Count); Refresh(); }
        private void CookStack(ItemStack stack) { if (_player.TryCook(stack)) Refresh(); }
        private void BoilStack(ItemStack stack) { if (_player.TryPurify(stack)) Refresh(); }

        // A short coloured tag showing how fresh a perishable food is (nothing for tinned/dry goods).
        private static string FreshnessSuffix(ItemStack stack)
        {
            if (stack.Def == null || !stack.Def.Perishable) return "";
            switch (stack.State)
            {
                case FoodState.Stale: return "  <size=13><color=#d9c14a>(stale)</color></size>";
                case FoodState.Rotten: return "  <size=13><color=#7fae4a>(rotten)</color></size>";
                default: return "  <size=13><color=#7fc98a>(fresh)</color></size>";
            }
        }

        // A short coloured tag showing how worn a damageable item is (weapons, tools, guns).
        private static string ConditionSuffix(ItemStack stack)
        {
            if (stack.Def == null || !stack.Def.IsDamageable) return "";
            float c01 = stack.Condition01;
            string col = c01 > 0.66f ? "#7fc98a" : c01 > 0.33f ? "#d9c14a" : "#d9694a";
            return "  <size=13><color=" + col + ">[" + Mathf.RoundToInt(c01 * 100f) + "%]</color></size>";
        }
        #endregion

        #region Build / refresh
        private void Refresh()
        {
            if (_player == null) return;
            HideTooltip(null);   // rows are about to be rebuilt
            float load = _player.Load;
            _playerHeader.text = "YOU   " + _player.Weight.ToString("0.0") + " / " + _player.Capacity.ToString("0.0") + " kg"
                + (_player.EquippedWeapon != null ? "     Wielding: " + _player.EquippedWeapon.DisplayName : "     Bare hands");
            _playerHeader.color = load >= 1f ? new Color(0.95f, 0.3f, 0.2f)
                : load >= CloneZConfig.EncumbranceStart ? new Color(0.95f, 0.7f, 0.2f)
                : Color.white;

            if (_weightMeterFill != null)
            {
                Vector3 ms = _weightMeterFill.rectTransform.localScale;
                ms.x = Mathf.Clamp01(load);
                _weightMeterFill.rectTransform.localScale = ms;
                _weightMeterFill.color = load >= 1f ? new Color(0.95f, 0.3f, 0.2f)
                    : load >= CloneZConfig.EncumbranceStart ? new Color(0.95f, 0.7f, 0.2f)
                    : new Color(0.5f, 0.72f, 0.5f);
            }

            BuildRows(_playerList, _player.Inventory, isContainer: false);

            if (_openContainer != null)
            {
                _containerColumn.SetActive(true);
                _containerHeader.text = _openContainer.DisplayName + (_openContainer.IsEmpty ? "   (empty)" : "");
                BuildRows(_containerList, _openContainer.Contents, isContainer: true);
            }
            else
            {
                _containerColumn.SetActive(false);
            }
        }

        private void BuildRows(RectTransform list, Inventory inv, bool isContainer)
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);

            // Standing at a stove unlocks the "Cook" action on raw ingredients in your own bag.
            bool nearStove = !isContainer && _player != null && _player.CanCookHere();

            // Display order groups items by category, then name (does not reorder the bag).
            var ordered = new System.Collections.Generic.List<ItemStack>(inv.Stacks);
            ordered.Sort((a, b) =>
            {
                int c = ((int)a.Def.Category).CompareTo((int)b.Def.Category);
                return c != 0 ? c : string.Compare(a.Def.DisplayName, b.Def.DisplayName, System.StringComparison.OrdinalIgnoreCase);
            });

            const float rowH = 40f;
            for (int i = 0; i < ordered.Count; i++)
            {
                ItemStack stack = ordered[i];

                var rowGo = new GameObject("Row");
                var rowRt = rowGo.AddComponent<RectTransform>();
                rowRt.SetParent(list, false);
                rowRt.anchorMin = new Vector2(0f, 1f); rowRt.anchorMax = new Vector2(0f, 1f); rowRt.pivot = new Vector2(0f, 1f);
                rowRt.anchoredPosition = new Vector2(0f, -i * rowH);
                rowRt.sizeDelta = new Vector2(430f, rowH - 4f);
                var rowImg = rowGo.AddComponent<Image>();
                rowImg.color = new Color(1f, 1f, 1f, i % 2 == 0 ? 0.05f : 0.02f);
                rowGo.AddComponent<TooltipTrigger>().Init(this, stack.Def, rowImg);

                var sw = new GameObject("Swatch");
                var swRt = sw.AddComponent<RectTransform>();
                swRt.SetParent(rowRt, false);
                swRt.anchorMin = new Vector2(0f, 0.5f); swRt.anchorMax = new Vector2(0f, 0.5f); swRt.pivot = new Vector2(0f, 0.5f);
                swRt.anchoredPosition = new Vector2(8f, 0f); swRt.sizeDelta = new Vector2(14f, 14f);
                sw.AddComponent<Image>().color = stack.Def.Tint;

                Text label = Label(rowRt, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(250f, rowH - 6f), 17, TextAnchor.MiddleLeft, new Color(0.92f, 0.92f, 0.92f));
                label.text = stack.Def.DisplayName + (stack.Count > 1 ? "  x" + stack.Count : "") + FreshnessSuffix(stack) + ConditionSuffix(stack);

                if (isContainer)
                {
                    ItemStack s = stack;
                    MakeButton(rowRt, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(80f, rowH - 10f), "Take", new Color(0.2f, 0.4f, 0.25f), () => TakeFromContainer(s));
                }
                else
                {
                    ItemStack s = stack;
                    float bx = -6f;
                    if (_openContainer != null)
                        MakeButton(rowRt, new Vector2(1f, 0.5f), new Vector2(bx, 0f), new Vector2(76f, rowH - 10f), "Store", new Color(0.28f, 0.32f, 0.4f), () => StoreInContainer(s));
                    else
                        MakeButton(rowRt, new Vector2(1f, 0.5f), new Vector2(bx, 0f), new Vector2(76f, rowH - 10f), "Drop", new Color(0.4f, 0.28f, 0.2f), () => DropStack(s));
                    bx -= 82f;

                    if (s.Def.IsUsable)
                    {
                        string verb = s.Def.IsClothing ? "Wear" : s.Def.IsEquippable ? "Equip" : "Use";
                        MakeButton(rowRt, new Vector2(1f, 0.5f), new Vector2(bx, 0f), new Vector2(76f, rowH - 10f), verb, new Color(0.2f, 0.38f, 0.42f), () => UseStack(s));
                        bx -= 82f;
                    }

                    // Raw ingredients gain a Cook button; it only works at a stove (dim otherwise).
                    if (s.Def.Cookable)
                    {
                        Color cookCol = nearStove ? new Color(0.55f, 0.36f, 0.2f) : new Color(0.28f, 0.26f, 0.24f);
                        MakeButton(rowRt, new Vector2(1f, 0.5f), new Vector2(bx, 0f), new Vector2(76f, rowH - 10f), "Cook", cookCol, () => CookStack(s));
                    }

                    // Tainted water can be boiled clean at a stove (the same heat source as cooking).
                    if (!string.IsNullOrEmpty(s.Def.PurifiedId))
                    {
                        Color boilCol = nearStove ? new Color(0.28f, 0.46f, 0.5f) : new Color(0.26f, 0.28f, 0.3f);
                        MakeButton(rowRt, new Vector2(1f, 0.5f), new Vector2(bx, 0f), new Vector2(76f, rowH - 10f), "Boil", boilCol, () => BoilStack(s));
                    }
                }
            }

            // An empty bag or container reads a quiet placeholder instead of a blank void.
            if (ordered.Count == 0)
            {
                var empty = Label(list, new Vector2(0f, 1f), new Vector2(16f, -20f), new Vector2(400f, 26f), 16, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.4f));
                empty.text = isContainer ? "Empty" : "Your bag is empty";
                empty.fontStyle = FontStyle.Italic;
            }

            // Size the scroll content so the whole list can be scrolled into view.
            list.sizeDelta = new Vector2(0f, Mathf.Max(ordered.Count, 1) * rowH);
        }

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

            RectTransform window = Panel(modalRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 580f), new Color(0.11f, 0.12f, 0.14f, 0.98f));
            UITheme.AddTopAccent(window, new Color(0.5f, 0.68f, 0.9f, 0.6f));

            Text title = Label(window, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(900f, 40f), 30, TextAnchor.UpperCenter, new Color(0.95f, 0.92f, 0.85f));
            title.text = "INVENTORY";

            MakeButton(window, new Vector2(1f, 1f), new Vector2(-18f, -14f), new Vector2(40f, 34f), "X", new Color(0.5f, 0.15f, 0.15f), Close);

            // Player column (left).
            _playerHeader = Label(window, new Vector2(0f, 1f), new Vector2(28f, -60f), new Vector2(430f, 28f), 20, TextAnchor.UpperLeft, Color.white);

            // Encumbrance meter sits just under the header.
            var wmBg = new GameObject("WeightMeterBg");
            var wmBgRt = wmBg.AddComponent<RectTransform>();
            wmBgRt.SetParent(window, false);
            wmBgRt.anchorMin = new Vector2(0f, 1f); wmBgRt.anchorMax = new Vector2(0f, 1f); wmBgRt.pivot = new Vector2(0f, 1f);
            wmBgRt.anchoredPosition = new Vector2(28f, -88f);
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

            _playerList = Column(window, new Vector2(0f, 1f), new Vector2(28f, -94f), new Vector2(430f, 440f));

            // Container column (right).
            _containerColumn = new GameObject("ContainerColumn");
            var ccRt = _containerColumn.AddComponent<RectTransform>();
            ccRt.SetParent(window, false);
            ccRt.anchorMin = new Vector2(1f, 1f); ccRt.anchorMax = new Vector2(1f, 1f); ccRt.pivot = new Vector2(1f, 1f);
            ccRt.anchoredPosition = new Vector2(-28f, -60f);
            ccRt.sizeDelta = new Vector2(430f, 470f);

            _containerHeader = Label(ccRt, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(290f, 28f), 20, TextAnchor.UpperLeft, Color.white);
            MakeButton(ccRt, new Vector2(1f, 1f), new Vector2(0f, 2f), new Vector2(120f, 30f), "Take All", new Color(0.2f, 0.4f, 0.25f), TakeAll);
            _containerList = Column(ccRt, new Vector2(0f, 1f), new Vector2(0f, -34f), new Vector2(430f, 430f));

            Label(window, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(900f, 24f), 16, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.6f))
                .text = "E / Esc  close      \u2022      Use to eat / drink / equip      \u2022      Cook or boil water at a stove      \u2022      Store / Drop to offload";

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

            // Content holds the rows; BuildRows sets its height so it can scroll.
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

        private Button MakeButton(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button");
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
            txt.font = _font; txt.fontSize = 16; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = text;
            return btn;
        }
        #endregion
    }
}
