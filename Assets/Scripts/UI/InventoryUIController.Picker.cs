using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    // The CONTAINERS column down the right of the inventory: Project Zomboid's strip of container
    // buttons, made verbose. It lists the floor and every container within reach — furniture, corpses,
    // dropped sacks — with what each holds (once searched), its weight and its distance. Clicking one
    // opens it in the loot pane; an unsearched one is rummaged through first, its bar filling here.
    // The grid view's loot-window tabs reuse GatherNearby / PickContainer / PickFloor.
    public partial class InventoryUIController
    {
        private const float PickerW = 200f;
        private const float PickerRowH = 58f;
        private const float PickerRefreshInterval = 0.3f;

        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_ColorId = Shader.PropertyToID("_Color");

        private RectTransform _pickerContent;
        private Text _pickerSubHeader;
        private readonly List<LootContainer> _nearby = new List<LootContainer>();
        private readonly List<WorldItem> _pickerFloor = new List<WorldItem>();
        private float _pickerTimer;
        private int _pickerSignature;

        // The container being searched and its row's bar, so the bar fills every frame without a rebuild.
        private LootContainer _pickerSearching;
        private RectTransform _pickerSearchFill;
        private Text _pickerSearchText;

        private void BuildPicker(RectTransform window, float x, float listH)
        {
            Text title = Label(window, new Vector2(0f, 1f), new Vector2(x, -PaneTop), new Vector2(PickerW, 28f), 20, TextAnchor.UpperLeft, Color.white);
            title.text = "CONTAINERS";
            _pickerSubHeader = Label(window, new Vector2(0f, 1f), new Vector2(x, -(PaneTop + 24f)), new Vector2(PickerW, 16f), 14, TextAnchor.UpperLeft, HeaderDim);
            _pickerContent = Column(window, new Vector2(0f, 1f), new Vector2(x, -ColumnTop), new Vector2(PickerW, listH));
        }

        // Re-checks what's in reach a few times a second (a corpse can drop beside you, or rot away)
        // and keeps a running search's bar moving every frame.
        private void TickPicker()
        {
            if (_pickerContent == null || _player == null) return;
            _pickerTimer -= Time.unscaledDeltaTime;
            if (_pickerTimer <= 0f)
            {
                _pickerTimer = PickerRefreshInterval;
                RefreshPicker(force: false);
            }
            UpdatePickerSearch();
        }

        // Rebuilds the column when what's in reach (or in any of it) changed, or when forced.
        private void RefreshPicker(bool force)
        {
            if (_pickerContent == null || _player == null) return;
            GatherNearby();
            WorldItem.Near(_player.transform.position, CloneZConfig.InteractRange, _pickerFloor);
            int sig = PickerSignature();
            if (!force && sig == _pickerSignature) return;
            _pickerSignature = sig;
            BuildPickerRows();
        }

        // Every container within reach — the same reach the world uses to search one — nearest first.
        // The open container stays listed even if it sits just past that reach.
        private void GatherNearby()
        {
            _nearby.Clear();
            Vector3 p = _player.transform.position;
            float r2 = CloneZConfig.InteractRange * CloneZConfig.InteractRange;
            List<LootContainer> all = LootContainer.All;
            for (int i = 0; i < all.Count; i++)
            {
                LootContainer c = all[i];
                if (c == null || c.Contents == null) continue;
                if (c == _openContainer || (c.transform.position - p).sqrMagnitude <= r2) _nearby.Add(c);
            }
            _nearby.Sort((a, b) => (a.transform.position - p).sqrMagnitude.CompareTo((b.transform.position - p).sqrMagnitude));
        }

        private int PickerSignature()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + Identity(_openContainer);
                h = h * 31 + Identity(_interactor != null ? _interactor.SearchTarget : null);
                h = h * 31 + _pickerFloor.Count;
                for (int i = 0; i < _pickerFloor.Count; i++) h = h * 31 + _pickerFloor[i].Count;
                for (int i = 0; i < _nearby.Count; i++)
                {
                    LootContainer c = _nearby[i];
                    h = h * 31 + Identity(c);
                    h = h * 31 + (c.Searched ? 1 : 0);
                    CountContents(c.Contents, out int items, out float kg);
                    h = h * 31 + items;
                    h = h * 31 + Mathf.RoundToInt(kg * 10f);
                }
                return h;
            }
        }

        // A per-object identity for the signature (0 for none or destroyed).
        private static int Identity(Object o) => o != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o) : 0;

        private void BuildPickerRows()
        {
            ClearChildren(_pickerContent);
            _pickerSearching = _interactor != null ? _interactor.SearchTarget : null;
            _pickerSearchFill = null;
            _pickerSearchText = null;

            // The floor always leads the column, as in PZ.
            int units = 0;
            float kg = 0f;
            for (int i = 0; i < _pickerFloor.Count; i++)
            {
                WorldItem w = _pickerFloor[i];
                if (w == null || w.Def == null) continue;
                units += w.Count;
                kg += w.Def.Weight * w.Count;
            }
            string floorStatus = units == 0 ? "Nothing on the ground" : Plural(units, "item") + "  \u00b7  " + kg.ToString("0.0") + " kg";
            float y = AddPickerRow(0f, "Floor", "Fl", new Color(0.4f, 0.38f, 0.33f), floorStatus, units == 0 ? TextFaint : TextDim,
                "", _openInventory == null, PickFloor, searching: false);

            for (int i = 0; i < _nearby.Count; i++)
            {
                LootContainer c = _nearby[i];
                bool searching = _pickerSearching != null && c == _pickerSearching;
                string status;
                Color statusCol;
                if (searching) { status = "Searching\u2026"; statusCol = WaterCol; }
                else if (!c.Searched && c.SearchTime > 0f) { status = "Unsearched"; statusCol = AmberCol; }
                else
                {
                    // Contents only show once searched, so an unopened crate keeps its secrets.
                    CountContents(c.Contents, out int items, out float ckg);
                    status = items == 0 ? "Empty" : Plural(items, "item") + "  \u00b7  " + ckg.ToString("0.0") + " kg";
                    statusCol = items == 0 ? TextFaint : TextDim;
                }
                LootContainer target = c;
                y = AddPickerRow(y, c.DisplayName, ContainerGlyph(c), ContainerColor(c), status, statusCol,
                    FlatDistance(c.transform.position).ToString("0.0") + " m", c == _openContainer, () => PickContainer(target), searching);
            }

            if (_pickerSubHeader != null)
                _pickerSubHeader.text = _nearby.Count == 0 ? "Nothing to search in reach" : Plural(_nearby.Count, "container") + " in reach";
            _pickerContent.sizeDelta = new Vector2(0f, Mathf.Max(y, PickerRowH));
        }

        private float AddPickerRow(float y, string name, string glyph, Color swatch, string status, Color statusCol,
            string distance, bool selected, System.Action onClick, bool searching)
        {
            var go = new GameObject("Container", typeof(RectTransform));
            go.SetActive(false);   // configure the button before it wakes, so it doesn't flash its default tint
            var rt = (RectTransform)go.transform;
            rt.SetParent(_pickerContent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta = new Vector2(PickerW, PickerRowH - 4f);
            var img = go.AddComponent<Image>();
            img.color = Color.white;   // the button tint below supplies the actual colour
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            Color baseCol = selected ? new Color(0.3f, 0.42f, 0.56f, 0.55f) : new Color(1f, 1f, 1f, 0.05f);
            ColorBlock cb = btn.colors;
            cb.normalColor = baseCol;
            cb.highlightedColor = selected ? baseCol : new Color(0.45f, 0.6f, 0.75f, 0.22f);
            cb.pressedColor = new Color(0.45f, 0.6f, 0.75f, 0.35f);
            cb.selectedColor = baseCol;
            cb.disabledColor = baseCol;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick());
            go.SetActive(true);

            if (selected) NewImage(rt, "Selected", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, PickerRowH - 4f), new Color(0.5f, 0.68f, 0.9f));

            RectTransform sw = NewImage(rt, "Swatch", new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(28f, 28f), swatch);
            Text g = Label(sw, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f), 12, TextAnchor.MiddleCenter, IconTextColor(swatch));
            g.text = glyph;
            g.fontStyle = FontStyle.Bold;
            g.raycastTarget = false;

            // Name on the top line at full width; status on the second, with the distance at its right.
            Text n = Label(rt, new Vector2(0f, 1f), new Vector2(44f, -6f), new Vector2(PickerW - 52f, 20f), 15, TextAnchor.UpperLeft, selected ? Color.white : TextMain);
            n.raycastTarget = false;
            n.text = FitText(n, name, "", PickerW - 52f);

            float statusW = PickerW - 52f;
            if (!string.IsNullOrEmpty(distance))
            {
                Text d = Label(rt, new Vector2(1f, 1f), new Vector2(-8f, -28f), new Vector2(40f, 16f), 11, TextAnchor.UpperRight, TextFaint);
                d.raycastTarget = false;
                d.text = distance;
                statusW -= 42f;
            }

            Text s = Label(rt, new Vector2(0f, 1f), new Vector2(44f, -27f), new Vector2(statusW, 16f), 12, TextAnchor.UpperLeft, statusCol);
            s.raycastTarget = false;
            s.text = FitText(s, status, "", statusW);

            if (searching)
            {
                RectTransform barBg = NewImage(rt, "SearchBar", new Vector2(0f, 0f), new Vector2(44f, 6f), new Vector2(PickerW - 56f, 4f), new Color(0f, 0f, 0f, 0.55f));
                _pickerSearchFill = NewImage(barBg, "Fill", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 4f), new Color(0.5f, 0.72f, 0.9f));
                _pickerSearchText = s;
            }
            return y + PickerRowH;
        }

        // Fills the searched row's bar; when a search starts or stops, redraws the column instead.
        private void UpdatePickerSearch()
        {
            LootContainer target = _interactor != null ? _interactor.SearchTarget : null;
            if (target != _pickerSearching)
            {
                RefreshPicker(force: true);
                return;
            }
            if (target == null || _pickerSearchFill == null) return;
            float t = Mathf.Clamp01(_interactor.SearchProgress);
            _pickerSearchFill.sizeDelta = new Vector2((PickerW - 56f) * t, 4f);
            if (_pickerSearchText != null) _pickerSearchText.text = "Searching\u2026  " + Mathf.RoundToInt(t * 100f) + "%";
        }

        // Opens a container from the column. A searched one opens at once; an unsearched one is
        // rummaged through first (the usual timed search — the survivor stands still while the window
        // is open), and the interactor opens it here when the search completes.
        private void PickContainer(LootContainer c)
        {
            if (c == null) return;
            // Picking anything else abandons a rummage still under way, so it can't yank the loot pane
            // (and with it the target of Store) over to that container a moment later.
            if (_interactor != null && _interactor.SearchTarget != null && _interactor.SearchTarget != c) _interactor.CancelSearch();
            if (c == _openContainer) return;
            if (c.Searched || c.SearchTime <= 0f || _interactor == null)
            {
                OpenContainer(c);
                return;
            }
            if (_interactor.SearchTarget == c) return;   // already rummaging through it
            _interactor.SearchOrOpen(c);
            if (_gridMode) PollTabs();   // the grid's tab starts filling
            else RefreshPicker(force: true);
        }

        // The FLOOR row: like picking a container, it abandons any rummage still under way.
        private void PickFloor()
        {
            if (_interactor != null && _interactor.SearchTarget != null) _interactor.CancelSearch();
            ShowFloor();
        }

        private static string ContainerGlyph(LootContainer c)
        {
            if (c.IsSack) return "Sk";
            switch (c.Type)
            {
                case ContainerType.Locker: return "Lk";
                case ContainerType.Fridge: return "Fr";
                case ContainerType.MedCabinet: return "+";
                case ContainerType.Desk: return "Dk";
                case ContainerType.ToolBench: return "Tb";
                case ContainerType.Wardrobe: return "Wd";
                case ContainerType.Pantry: return "Pa";
                case ContainerType.GunLocker: return "Gn";
                case ContainerType.Shelf: return "Sh";
                case ContainerType.Corpse: return "\u2020";
                default: return "Cr";
            }
        }

        // The container's own colour, read off its box in the world, so the swatch matches what you see.
        private static Color ContainerColor(LootContainer c)
        {
            Renderer r = c.GetComponent<Renderer>();
            Material m = r != null ? r.sharedMaterial : null;
            Color col = new Color(0.5f, 0.45f, 0.38f);
            if (m != null)
            {
                if (m.HasProperty(s_BaseColorId)) col = m.GetColor(s_BaseColorId);
                else if (m.HasProperty(s_ColorId)) col = m.GetColor(s_ColorId);
            }
            col.a = 1f;
            return col;
        }
    }
}
