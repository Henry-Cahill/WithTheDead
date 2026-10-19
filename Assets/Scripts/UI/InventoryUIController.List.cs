using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    // The verbose, Project Zomboid-style details list shared by the bag and loot panes: column titles
    // to sort by (Type / Category / Weight / Status), one collapsible row per kind of item, and a row
    // per item — each with its own condition or freshness — once a row is expanded.
    public partial class InventoryUIController
    {
        private enum SortKey { Category, Name, Weight, Status }
        private enum EntryKind { Bag, Worn, Container, Floor }

        /// <summary>One listed item: a stack in the bag or a container, a worn garment, or a floor pile.</summary>
        private sealed class Entry
        {
            public EntryKind Kind;
            public ItemStack Stack;     // the live stack (bag / container), or a display copy (worn / floor)
            public WorldItem Pile;      // the floor pile this entry mirrors
            public ClothingSlot Slot;   // the body slot a worn garment is on
            public bool InHand;         // the bag stack backing the weapon in hand
            public int Order;           // listing order: the stable tie-break when sorting
        }

        /// <summary>Every listed item of one kind — a single collapsible row, PZ-style.</summary>
        private sealed class Group
        {
            public ItemDef Def;
            public readonly List<Entry> Entries = new List<Entry>(2);
            public int Units;
            public float Weight;
        }

        /// <summary>Per-pane state that survives a refresh: the sort, and which rows are expanded.</summary>
        private sealed class Pane
        {
            public RectTransform Content;
            public SortKey Sort = SortKey.Category;
            public bool Descending;
            public readonly HashSet<ItemDef> Expanded = new HashSet<ItemDef>();
            public readonly List<ItemDef> Expandable = new List<ItemDef>();
            public readonly Text[] SortLabels = new Text[4];
            public Text ExpandAllLabel;
        }

        /// <summary>A row's actions: its two inline buttons (use + move) and its full context menu.</summary>
        private sealed class RowActions
        {
            public string UseLabel;
            public System.Action Use;
            public string MoveLabel;
            public System.Action Move;
            public readonly List<MenuOption> Menu = new List<MenuOption>(8);
        }

        // Row geometry — x offsets from the row's left edge.
        private const float ListW = 660f;
        private const float RowH = 30f;
        private const float SubRowH = 26f;
        private const float ColHeaderH = 24f;
        private const float ArrowX = 2f, ArrowW = 18f;
        private const float IconX = 24f, IconW = 24f;
        private const float NameX = 54f, NameW = 188f;
        private const float CatX = 248f, CatW = 80f;
        private const float WeightX = 330f, WeightW = 44f;
        private const float StatusX = 382f, StatusBarW = 48f, StatusTextW = 70f;
        private const float ButtonW = 58f, MoreW = 24f, ButtonGap = 4f;

        private static readonly string[] s_SortTitles = { "Category", "Type", "Weight", "Status" };

        private static readonly Color RowEven = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color RowOdd = new Color(1f, 1f, 1f, 0.02f);
        private static readonly Color RowPinned = new Color(0.45f, 0.62f, 0.85f, 0.12f);
        private static readonly Color RowSub = new Color(0f, 0f, 0f, 0.22f);
        private static readonly Color TextMain = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color TextDim = new Color(0.72f, 0.74f, 0.78f);
        private static readonly Color TextFaint = new Color(1f, 1f, 1f, 0.38f);
        private static readonly Color CategoryText = new Color(0.6f, 0.6f, 0.8f);   // PZ's category-column tint
        private static readonly Color WornText = new Color(0.62f, 0.8f, 1f);
        private static readonly Color GoodCol = new Color(0.5f, 0.79f, 0.54f);
        private static readonly Color WarnCol = new Color(0.85f, 0.76f, 0.29f);
        private static readonly Color BadCol = new Color(0.85f, 0.41f, 0.29f);
        private static readonly Color RottenCol = new Color(0.5f, 0.68f, 0.29f);
        private static readonly Color AmberCol = new Color(0.88f, 0.65f, 0.24f);
        private static readonly Color WaterCol = new Color(0.5f, 0.78f, 0.95f);

        // A faint rule between the catalogue tooltip and the lines about the particular item hovered.
        private const string Rule = "\n<color=#3c444e>\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500</color>";

        #region Pane contents
        // The bag: the weapon in hand and the worn clothing pinned on top (the way PZ lists equipped
        // items), then everything carried.
        private void BuildBagList()
        {
            var pinned = new List<Entry>(6);
            ItemStack inHand = _player.EquippedWeapon != null ? _player.EquippedStack : null;
            if (inHand != null) pinned.Add(new Entry { Kind = EntryKind.Bag, Stack = inHand, InHand = true });

            if (_clothing != null)
            {
                ClothingSlot[] slots = PlayerClothing.Slots;
                for (int i = 0; i < slots.Length; i++)
                {
                    PlayerClothing.WornPiece piece = _clothing.GetPiece(slots[i]);
                    if (!piece.Exists) continue;
                    // A display copy carries the garment's wear so its row reads like any other item.
                    var shown = new ItemStack(piece.Def, 1) { Condition = piece.Condition, TimesRepaired = piece.TimesRepaired, Extra = piece.Extra };
                    pinned.Add(new Entry { Kind = EntryKind.Worn, Stack = shown, Slot = slots[i] });
                }
            }

            List<ItemStack> stacks = _player.Inventory.Stacks;
            var carried = new List<Entry>(stacks.Count);
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i] != inHand && stacks[i].Def != null)
                    carried.Add(new Entry { Kind = EntryKind.Bag, Stack = stacks[i], Order = i });

            BuildList(_bagPane, pinned, carried, "Your bag is empty");
        }

        private void BuildContainerList()
        {
            List<ItemStack> stacks = _openInventory.Stacks;
            var entries = new List<Entry>(stacks.Count);
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Def != null)
                    entries.Add(new Entry { Kind = EntryKind.Container, Stack = stacks[i], Order = i });
            BuildList(_lootPane, null, entries, "Empty");
        }

        private void BuildFloorList()
        {
            RefreshFloorItems();
            var entries = new List<Entry>(_floorItems.Count);
            for (int i = 0; i < _floorItems.Count; i++)
            {
                WorldItem w = _floorItems[i];
                if (w == null || w.Def == null) continue;
                // A throwaway stack mirrors the pile so its wear / freshness read exactly as in the bag.
                entries.Add(new Entry { Kind = EntryKind.Floor, Stack = DisplayStackFor(w), Pile = w, Order = i });
            }
            BuildList(_lootPane, null, entries, "Nothing on the floor");
        }

        // Gathers the ground items within reach of the player (only the chunks within reach are
        // searched, rather than every item in the world).
        private void RefreshFloorItems()
        {
            _floorItems.Clear();
            if (_player == null) return;
            WorldItem.Near(_player.transform.position, CloneZConfig.InteractRange, _floorItems);
        }

        // A display-only stack carrying a ground item's per-instance state (age / wear) so floor rows
        // reuse the same freshness and condition readouts as the bag.
        private static ItemStack DisplayStackFor(WorldItem item)
        {
            var s = new ItemStack(item.Def, item.Count);
            if (item.Def.Perishable) s.SpawnHours = item.SpawnHours;
            if (item.Def.IsDamageable)
            {
                s.Condition = Mathf.Clamp(item.Condition, 0, item.Def.MaxCondition);
                s.TimesRepaired = item.TimesRepaired;
            }
            s.Extra = item.Extra;
            return s;
        }
        #endregion

        #region List building
        private void BuildList(Pane pane, List<Entry> pinned, List<Entry> entries, string emptyText)
        {
            RectTransform list = pane.Content;
            ClearChildren(list);
            pane.Expandable.Clear();

            float y = 0f;
            int pinnedCount = pinned != null ? pinned.Count : 0;
            for (int i = 0; i < pinnedCount; i++) y = AddEntryRow(list, pinned[i], y, RowPinned, sub: false);

            List<Group> groups = GroupEntries(entries);
            SortGroups(groups, pane.Sort, pane.Descending);
            if (pinnedCount > 0 && groups.Count > 0) y = AddDivider(list, y);

            bool anyExpanded = false;
            for (int g = 0; g < groups.Count; g++)
            {
                Group group = groups[g];
                Color bg = g % 2 == 0 ? RowEven : RowOdd;
                if (group.Entries.Count == 1)
                {
                    y = AddEntryRow(list, group.Entries[0], y, bg, sub: false);
                    continue;
                }

                pane.Expandable.Add(group.Def);
                bool open = pane.Expanded.Contains(group.Def);
                anyExpanded |= open;
                y = AddGroupRow(pane, group, y, bg, open);
                if (!open) continue;
                for (int e = 0; e < group.Entries.Count; e++) y = AddEntryRow(list, group.Entries[e], y, RowSub, sub: true);
            }

            // An empty bag or container reads a quiet placeholder instead of a blank void.
            if (pinnedCount == 0 && groups.Count == 0)
            {
                Text empty = Label(list, new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(400f, 26f), 16, TextAnchor.MiddleLeft, TextFaint);
                empty.text = emptyText;
                empty.fontStyle = FontStyle.Italic;
                empty.raycastTarget = false;
                y = 40f;
            }

            if (pane.ExpandAllLabel != null)
            {
                pane.ExpandAllLabel.text = anyExpanded ? "\u2212 All" : "+ All";
                pane.ExpandAllLabel.color = pane.Expandable.Count > 0 ? HeaderDim : new Color(1f, 1f, 1f, 0.2f);
            }
            UpdateSortLabels(pane);

            // Size the scroll content so the whole list can be scrolled into view.
            list.sizeDelta = new Vector2(0f, Mathf.Max(y, RowH));
        }

        // Folds the entries into one group per kind of item.
        private static List<Group> GroupEntries(List<Entry> entries)
        {
            var groups = new List<Group>();
            var byDef = new Dictionary<ItemDef, Group>();
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                ItemDef def = e.Stack.Def;
                if (!byDef.TryGetValue(def, out Group g))
                {
                    g = new Group { Def = def };
                    byDef.Add(def, g);
                    groups.Add(g);
                }
                g.Entries.Add(e);
                g.Units += e.Stack.Count;
                g.Weight += e.Stack.Weight;
            }

            // Within a row the best piece leads — the sharpest blade, the freshest food — so the row's
            // Use / Equip button picks it.
            for (int i = 0; i < groups.Count; i++)
                if (groups[i].Entries.Count > 1)
                    groups[i].Entries.Sort((a, b) =>
                    {
                        int c = StatusValue(b.Stack).CompareTo(StatusValue(a.Stack));
                        return c != 0 ? c : a.Order.CompareTo(b.Order);
                    });
            return groups;
        }

        private static void SortGroups(List<Group> groups, SortKey key, bool descending)
        {
            groups.Sort((a, b) =>
            {
                int name = string.Compare(a.Def.DisplayName, b.Def.DisplayName, System.StringComparison.OrdinalIgnoreCase);
                int c = key switch
                {
                    SortKey.Name => name,
                    SortKey.Weight => a.Weight.CompareTo(b.Weight),
                    SortKey.Status => GroupStatusValue(a).CompareTo(GroupStatusValue(b)),
                    _ => ((int)a.Def.Category).CompareTo((int)b.Def.Category),
                };
                if (descending) c = -c;
                if (c != 0) return c;
                if (name != 0) return name;   // ties read A→Z
                return string.CompareOrdinal(a.Def.Id, b.Def.Id);
            });
        }

        // How good an item is, for sorting: its condition or freshness (0..1), or 2 when it has neither.
        private static float StatusValue(ItemStack s)
        {
            if (s.Def.IsDamageable) return s.Condition01;
            if (s.Def.Perishable) return s.Freshness01;
            return 2f;
        }

        private static float GroupStatusValue(Group g)
        {
            float v = 2f;
            for (int i = 0; i < g.Entries.Count; i++) v = Mathf.Min(v, StatusValue(g.Entries[i].Stack));
            return v;
        }
        #endregion

        #region Rows
        // A row standing for every item of one kind in the pane; ►/▼ expands it into a row per item.
        private float AddGroupRow(Pane pane, Group g, float y, Color bg, bool open)
        {
            ItemDef def = g.Def;
            RowActions acts = GroupActions(pane, g, open);
            System.Action menu = () => ShowMenu(def.DisplayName + "  (" + g.Units + ")", acts.Menu);
            RectTransform row = NewRow(pane.Content, y, RowH, bg, def, GroupInfo(g), acts.Move, menu);

            FlatButton(row, new Vector2(ArrowX, 0f), new Vector2(ArrowW, RowH - 8f), open ? "\u25bc" : "\u25ba", 10,
                () => ToggleExpanded(pane, def), TextDim);
            AddIcon(row, def, badge: false);
            AddName(row, def.DisplayName, " (" + g.Units + ")", RowH, 15, TextMain, 0f);
            AddCell(row, CatX, CatW, RowH, def.Category.ToString(), 13, CategoryText, TextAnchor.MiddleLeft);
            AddCell(row, WeightX, WeightW, RowH, Kg(g.Weight), 13, TextDim, TextAnchor.MiddleRight);
            GroupStatus(g, out float fill, out Color barCol, out string text, out Color textCol);
            AddStatus(row, RowH, fill, barCol, text, textCol);
            AddButtons(row, RowH, acts, menu);
            return y + RowH;
        }

        // One item: a top-level row when it's the only one of its kind (or pinned), otherwise an
        // indented sub-row under its expanded group.
        private float AddEntryRow(RectTransform list, Entry e, float y, Color bg, bool sub)
        {
            ItemStack s = e.Stack;
            ItemDef def = s.Def;
            float h = sub ? SubRowH : RowH;
            RowActions acts = EntryActions(e);
            System.Action menu = () => ShowMenu(def.DisplayName + (s.Count > 1 ? "  (" + s.Count + ")" : ""), acts.Menu);
            RectTransform row = NewRow(list, y, h, bg, def, EntryInfo(e), acts.Move, menu);

            if (!sub) AddIcon(row, def, badge: e.Kind == EntryKind.Worn || e.InHand);
            string count = s.Count <= 1 ? "" : sub ? "  \u00d7" + s.Count : " (" + s.Count + ")";
            AddName(row, def.DisplayName, count, h, sub ? 14 : 15, sub ? TextDim : TextMain, sub ? 12f : 0f);

            string cat;
            Color catCol = CategoryText;
            if (e.Kind == EntryKind.Worn) { cat = "Worn \u00b7 " + e.Slot; catCol = WornText; }
            else if (e.InHand) { cat = "Equipped"; catCol = WornText; }
            else if (sub) { cat = InstanceNote(s); catCol = TextFaint; }
            else cat = def.Category.ToString();
            AddCell(row, CatX, CatW, h, cat, sub ? 12 : 13, catCol, TextAnchor.MiddleLeft);

            // Worn clothes ride on the body rather than in the bag, so their weight reads faded.
            AddCell(row, WeightX, WeightW, h, Kg(s.Weight), sub ? 12 : 13, e.Kind == EntryKind.Worn ? TextFaint : TextDim, TextAnchor.MiddleRight);

            EntryStatus(s, out float fill, out Color barCol, out string text, out Color textCol);
            AddStatus(row, h, fill, barCol, text, textCol);
            AddButtons(row, h, acts, menu);
            return y + h;
        }

        // The row backdrop doubles as its hover zone: tooltip, double-click to move, right-click for more.
        private RectTransform NewRow(RectTransform list, float y, float h, Color bg, ItemDef def, string info,
            System.Action onDoubleClick, System.Action onRightClick)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(list, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta = new Vector2(ListW, h - 2f);
            var img = go.AddComponent<Image>();
            img.color = bg;
            go.AddComponent<TooltipTrigger>().Init(this, def, img, info, onDoubleClick, onRightClick);
            return rt;
        }

        // The item's tint with a one-letter category glyph; worn / in-hand items get PZ's green tick.
        private void AddIcon(RectTransform row, ItemDef def, bool badge)
        {
            RectTransform icon = NewImage(row, "Icon", new Vector2(0f, 0.5f), new Vector2(IconX, 0f), new Vector2(IconW, IconW), def.Tint);
            Text glyph = Label(icon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(IconW, IconW), 14, TextAnchor.MiddleCenter, IconTextColor(def.Tint));
            glyph.text = CategoryGlyph(def.Category);
            glyph.fontStyle = FontStyle.Bold;
            glyph.raycastTarget = false;
            if (!badge) return;

            RectTransform tick = NewImage(icon, "Equipped", new Vector2(1f, 0f), new Vector2(4f, -4f), new Vector2(13f, 13f), new Color(0.24f, 0.62f, 0.32f));
            Text t = Label(tick, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(13f, 13f), 11, TextAnchor.MiddleCenter, Color.white);
            t.text = "\u221a";
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
        }

        private void AddName(RectTransform row, string name, string suffix, float h, int size, Color color, float indent)
        {
            float w = NameW - indent;
            Text t = AddCell(row, NameX + indent, w, h, "", size, color, TextAnchor.MiddleLeft);
            t.text = FitText(t, name, suffix, w);   // long names trim with "…" but keep their count
        }

        private Text AddCell(RectTransform row, float x, float w, float h, string text, int size, Color color, TextAnchor align)
        {
            Text t = Label(row, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(w, h), size, align, color);
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        // The verbose Status column: a condition / freshness bar when the item has one, plus a word or %.
        private void AddStatus(RectTransform row, float h, float fill, Color barCol, string text, Color textCol)
        {
            float x = StatusX;
            if (fill >= 0f)
            {
                RectTransform bar = NewImage(row, "Bar", new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(StatusBarW, 6f), new Color(0f, 0f, 0f, 0.55f));
                NewImage(bar, "Fill", new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2((StatusBarW - 2f) * Mathf.Clamp01(fill), 4f), barCol);
                x += StatusBarW + 6f;
            }
            AddCell(row, x, StatusX + StatusBarW + 6f + StatusTextW - x, h, text, 12, textCol, TextAnchor.MiddleLeft);
        }

        // The inline buttons, right-aligned: [Use] [Move] […]. Everything else lives in the "…" menu.
        private void AddButtons(RectTransform row, float h, RowActions acts, System.Action menu)
        {
            float bh = h - 8f;
            float x = -4f;
            MakeButton(row, new Vector2(1f, 0.5f), new Vector2(x, 0f), new Vector2(MoreW, bh), "\u2026", new Color(0.26f, 0.28f, 0.32f), () => menu(), 13);
            x -= MoreW + ButtonGap;
            if (acts.Move != null)
            {
                System.Action move = acts.Move;
                MakeButton(row, new Vector2(1f, 0.5f), new Vector2(x, 0f), new Vector2(ButtonW, bh), acts.MoveLabel, MoveColor(acts.MoveLabel), () => move(), 13);
            }
            x -= ButtonW + ButtonGap;   // the Use slot stays put even without a move, keeping the columns aligned
            if (acts.Use != null)
            {
                System.Action use = acts.Use;
                MakeButton(row, new Vector2(1f, 0.5f), new Vector2(x, 0f), new Vector2(ButtonW, bh), acts.UseLabel, UseColor(acts.UseLabel), () => use(), 13);
            }
        }

        // A hairline between the pinned (worn / in hand) rows and the rest of the bag, as in PZ.
        private static float AddDivider(RectTransform list, float y)
        {
            NewImage(list, "Divider", new Vector2(0f, 1f), new Vector2(4f, -(y + 3f)), new Vector2(ListW - 8f, 1f), new Color(1f, 1f, 1f, 0.28f));
            return y + 7f;
        }

        private static RectTransform NewImage(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }
        #endregion

        #region Row actions
        private RowActions EntryActions(Entry e)
        {
            var a = new RowActions();
            ItemStack s = e.Stack;
            switch (e.Kind)
            {
                case EntryKind.Worn:
                {
                    ClothingSlot slot = e.Slot;
                    a.MoveLabel = "Take off";
                    a.Move = () => TakeOffSlot(slot);
                    a.Menu.Add(new MenuOption("Take off", a.Move));
                    break;
                }
                case EntryKind.Container:
                    a.MoveLabel = "Take";
                    a.Move = () => TakeFromContainer(s, s.Count);
                    a.Menu.Add(new MenuOption(s.Count > 1 ? "Take all " + s.Count : "Take", a.Move));
                    if (s.Count > 1) a.Menu.Add(new MenuOption("Take 1", () => TakeFromContainer(s, 1)));
                    break;
                case EntryKind.Floor:
                {
                    WorldItem pile = e.Pile;
                    a.MoveLabel = "Take";
                    a.Move = () => TakeFromFloor(pile);
                    a.Menu.Add(new MenuOption(s.Count > 1 ? "Take all " + s.Count : "Take", a.Move));
                    if (s.Count > 1) a.Menu.Add(new MenuOption("Take 1", () => TakeFromFloor(pile, 1)));
                    break;
                }
                default:
                    AddBagActions(a, s, e.InHand);
                    break;
            }
            return a;
        }

        private void AddBagActions(RowActions a, ItemStack s, bool inHand)
        {
            ItemDef d = s.Def;
            if (inHand) { a.UseLabel = "Unequip"; a.Use = Unequip; }
            else if (d.IsUsable) { a.UseLabel = UseVerb(d); a.Use = () => UseStack(s); }
            if (a.Use != null) a.Menu.Add(new MenuOption(a.UseLabel, a.Use));

            // Worn gear can be patched up when a material (duct tape, or thread + a needle for
            // clothing) is on hand; the option only shows when a repair would actually help.
            if (_player.CanRepair(s)) a.Menu.Add(new MenuOption("Repair", () => RepairStack(s)));
            // Raw ingredients cook, and tainted water boils clean, only at a stove.
            if (d.Cookable) a.Menu.Add(new MenuOption(_nearStove ? "Cook" : "Cook  (needs a stove)", () => CookStack(s), _nearStove));
            if (!string.IsNullOrEmpty(d.PurifiedId)) a.Menu.Add(new MenuOption(_nearStove ? "Boil" : "Boil  (needs a stove)", () => BoilStack(s), _nearStove));

            System.Action dropAll = () => DropStack(s, s.Count);
            if (_openInventory != null)
            {
                a.MoveLabel = "Store";
                a.Move = () => StoreInContainer(s, s.Count);
                a.Menu.Add(new MenuOption(s.Count > 1 ? "Store all " + s.Count : "Store", a.Move));
                if (s.Count > 1) a.Menu.Add(new MenuOption("Store 1", () => StoreInContainer(s, 1)));
            }
            else
            {
                a.MoveLabel = "Drop";
                a.Move = dropAll;
            }
            a.Menu.Add(new MenuOption(s.Count > 1 ? "Drop all " + s.Count : "Drop", dropAll));
            if (s.Count > 1) a.Menu.Add(new MenuOption("Drop 1", () => DropStack(s, 1)));
            AddFeatureVerbs(a.Menu, s);
        }

        // A collapsed row moves every item of its kind at once; Use / Equip picks the best one.
        private RowActions GroupActions(Pane pane, Group g, bool open)
        {
            var a = new RowActions();
            var entries = new List<Entry>(g.Entries);   // the closures act on this snapshot
            ItemDef def = g.Def;
            string all = "all " + g.Units;
            switch (g.Entries[0].Kind)
            {
                case EntryKind.Container:
                case EntryKind.Floor:
                    a.MoveLabel = "Take";
                    a.Move = () => TakeEntries(entries);
                    a.Menu.Add(new MenuOption("Take " + all, a.Move));
                    break;
                default:
                {
                    ItemStack best = g.Entries[0].Stack;
                    if (def.IsUsable)
                    {
                        a.UseLabel = UseVerb(def);
                        a.Use = () => UseStack(best);
                        a.Menu.Add(new MenuOption(a.UseLabel + "  (best one)", a.Use));
                    }
                    System.Action dropAll = () => DropEntries(entries);
                    if (_openInventory != null)
                    {
                        a.MoveLabel = "Store";
                        a.Move = () => StoreEntries(entries);
                        a.Menu.Add(new MenuOption("Store " + all, a.Move));
                    }
                    else
                    {
                        a.MoveLabel = "Drop";
                        a.Move = dropAll;
                    }
                    a.Menu.Add(new MenuOption("Drop " + all, dropAll));
                    break;
                }
            }
            a.Menu.Add(new MenuOption(open ? "Collapse" : "Expand  (a row per item)", () => ToggleExpanded(pane, def)));
            return a;
        }

        private static string UseVerb(ItemDef d) => d.CustomUse && !string.IsNullOrEmpty(d.UseVerb) ? d.UseVerb
            : d.IsClothing ? "Wear" : d.IsEquippable ? "Equip" : d.IsCanned ? "Open" : "Use";

        private static Color MoveColor(string label)
        {
            switch (label)
            {
                case "Take": return new Color(0.2f, 0.4f, 0.25f);
                case "Store": return new Color(0.28f, 0.32f, 0.4f);
                case "Take off": return new Color(0.3f, 0.34f, 0.46f);
                default: return new Color(0.4f, 0.28f, 0.2f);   // Drop
            }
        }

        private static Color UseColor(string label) =>
            label == "Unequip" ? new Color(0.38f, 0.32f, 0.22f) : new Color(0.2f, 0.38f, 0.42f);
        #endregion

        #region Column titles, sorting and expanding
        // A pane's column titles (PZ's "Type | Category" header, plus Weight and Status). Each title
        // sorts the pane; the toggle on the left expands or collapses every stacked row.
        private void BuildColumnHeader(RectTransform parent, float x, float y, Pane pane)
        {
            RectTransform strip = Panel(parent, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(ListW, ColHeaderH), new Color(0f, 0f, 0f, 0.4f));
            pane.ExpandAllLabel = FlatButton(strip, new Vector2(ArrowX, 0f), new Vector2(IconX + IconW - ArrowX, ColHeaderH - 4f), "+ All", 11,
                () => ToggleExpandAll(pane), HeaderDim);
            pane.SortLabels[(int)SortKey.Name] = FlatButton(strip, new Vector2(NameX - 4f, 0f), new Vector2(NameW + 4f, ColHeaderH - 2f), "Type", 13,
                () => SetSort(pane, SortKey.Name), HeaderDim, TextAnchor.MiddleLeft);
            pane.SortLabels[(int)SortKey.Category] = FlatButton(strip, new Vector2(CatX - 4f, 0f), new Vector2(CatW + 2f, ColHeaderH - 2f), "Category", 13,
                () => SetSort(pane, SortKey.Category), HeaderDim, TextAnchor.MiddleLeft);
            pane.SortLabels[(int)SortKey.Weight] = FlatButton(strip, new Vector2(WeightX - 4f, 0f), new Vector2(WeightW + 8f, ColHeaderH - 2f), "Weight", 13,
                () => SetSort(pane, SortKey.Weight), HeaderDim, TextAnchor.MiddleRight);
            pane.SortLabels[(int)SortKey.Status] = FlatButton(strip, new Vector2(StatusX - 4f, 0f), new Vector2(StatusBarW + StatusTextW + 10f, ColHeaderH - 2f), "Status", 13,
                () => SetSort(pane, SortKey.Status), HeaderDim, TextAnchor.MiddleLeft);
            UpdateSortLabels(pane);
        }

        // Clicking a column title sorts by it; clicking it again flips the direction.
        private void SetSort(Pane pane, SortKey key)
        {
            if (pane.Sort == key) pane.Descending = !pane.Descending;
            else
            {
                pane.Sort = key;
                pane.Descending = key == SortKey.Weight;   // heaviest first; the rest start A→Z / worst first
            }
            MarkDirty();
        }

        private static void UpdateSortLabels(Pane pane)
        {
            for (int k = 0; k < pane.SortLabels.Length; k++)
            {
                Text t = pane.SortLabels[k];
                if (t == null) continue;
                bool on = (int)pane.Sort == k;
                t.text = s_SortTitles[k] + (on ? (pane.Descending ? "  \u25bc" : "  \u25b2") : "");
                t.color = on ? Color.white : HeaderDim;
            }
        }

        private void ToggleExpanded(Pane pane, ItemDef def)
        {
            if (!pane.Expanded.Remove(def)) pane.Expanded.Add(def);
            MarkDirty();
        }

        // Expands every stacked row, or — when any is open — folds them all back up.
        private void ToggleExpandAll(Pane pane)
        {
            bool anyOpen = false;
            for (int i = 0; i < pane.Expandable.Count; i++) anyOpen |= pane.Expanded.Contains(pane.Expandable[i]);
            if (anyOpen) pane.Expanded.Clear();
            else for (int i = 0; i < pane.Expandable.Count; i++) pane.Expanded.Add(pane.Expandable[i]);
            MarkDirty();
        }
        #endregion

        #region Readouts
        // One item's Status cell: wear for gear, freshness for food, else the most useful word.
        private static void EntryStatus(ItemStack s, out float fill, out Color barCol, out string text, out Color textCol)
        {
            ItemDef d = s.Def;
            fill = -1f;
            barCol = Color.clear;
            text = "\u2014";
            textCol = TextFaint;
            if (d.IsDamageable)
            {
                fill = s.Condition01;
                barCol = ConditionColor(fill);
                text = Pct(fill);
                textCol = barCol;
            }
            else if (d.Perishable)
            {
                fill = s.Freshness01;
                FoodState st = s.State;
                barCol = FreshnessColor(st);
                text = st.ToString();
                textCol = barCol;
            }
            else if (d.IsWaterContainer)
            {
                text = !d.HoldsWater ? "Empty" : d.WaterTainted ? "Tainted" : "Clean";
                textCol = !d.HoldsWater ? TextFaint : d.WaterTainted ? AmberCol : WaterCol;
            }
            else if (d.IsCanned) { text = "Sealed"; textCol = TextDim; }
            else if (d.RawHazard) { text = "Raw"; textCol = AmberCol; }
            else if (d.IsSeed) { text = "Seed"; textCol = GoodCol; }
        }

        // A stacked row's Status: the average wear (with its range), or the stalest food in it.
        private static void GroupStatus(Group g, out float fill, out Color barCol, out string text, out Color textCol)
        {
            ItemDef d = g.Def;
            if (d.IsDamageable)
            {
                float lo = 1f, hi = 0f, sum = 0f;
                for (int i = 0; i < g.Entries.Count; i++)
                {
                    float c = g.Entries[i].Stack.Condition01;
                    lo = Mathf.Min(lo, c);
                    hi = Mathf.Max(hi, c);
                    sum += c;
                }
                fill = sum / g.Entries.Count;
                barCol = ConditionColor(fill);
                int loPct = Mathf.RoundToInt(lo * 100f), hiPct = Mathf.RoundToInt(hi * 100f);
                text = loPct == hiPct ? hiPct + "%" : loPct + "\u2013" + hiPct + "%";
                textCol = barCol;
                return;
            }
            if (d.Perishable)
            {
                float lo = 1f;
                FoodState worst = FoodState.Fresh;
                for (int i = 0; i < g.Entries.Count; i++)
                {
                    ItemStack s = g.Entries[i].Stack;
                    lo = Mathf.Min(lo, s.Freshness01);
                    if (s.State > worst) worst = s.State;
                }
                fill = lo;
                barCol = FreshnessColor(worst);
                text = worst.ToString();
                textCol = barCol;
                return;
            }
            EntryStatus(g.Entries[0].Stack, out fill, out barCol, out text, out textCol);
            if (text == "\u2014")
            {
                text = g.Entries.Count + (g.Entries[0].Kind == EntryKind.Floor ? " piles" : " stacks");
                textCol = TextFaint;
            }
        }

        // A sub-row's per-item note in the category column: its repairs, or how long the food has been about.
        private static string InstanceNote(ItemStack s)
        {
            if (s.Def.IsDamageable && s.TimesRepaired > 0) return "Repaired \u00d7" + s.TimesRepaired;
            if (s.Def.Perishable) return Mathf.RoundToInt(s.AgeHours) + "h old";
            return "";
        }

        // Tooltip lines about the particular item hovered — where it is, its stack, wear and freshness —
        // under the catalogue description every item of its kind shares.
        private static string EntryInfo(Entry e)
        {
            ItemStack s = e.Stack;
            ItemDef d = s.Def;
            var sb = new System.Text.StringBuilder(160);
            if (e.Kind == EntryKind.Worn) InfoLine(sb, "Status", "Worn on the " + e.Slot.ToString().ToLowerInvariant());
            else if (e.InHand) InfoLine(sb, "Status", "In hand");
            else if (e.Kind == EntryKind.Floor) InfoLine(sb, "Status", "On the floor");
            if (s.Count > 1) InfoLine(sb, "Stack", "\u00d7" + s.Count + "  \u00b7  " + Kg(s.Weight) + " kg");
            if (d.IsDamageable)
                InfoLine(sb, "Wear", ItemConditionTiers.Label(s.Condition01) + "  \u00b7  " + Pct(s.Condition01) + "  (" + s.Condition + " / " + d.MaxCondition + ")"
                    + (s.TimesRepaired > 0 ? "  \u00b7  repaired \u00d7" + s.TimesRepaired : ""));
            if (d.Perishable)
                InfoLine(sb, "Freshness", s.State + "  \u00b7  " + Mathf.RoundToInt(s.AgeHours) + "h old of ~" + Mathf.RoundToInt(d.ShelfLifeHours) + "h");
            return sb.Length == 0 ? null : Rule + sb.ToString();
        }

        private static string GroupInfo(Group g)
        {
            var sb = new System.Text.StringBuilder(200);
            string noun = g.Entries[0].Kind == EntryKind.Floor ? "piles" : "stacks";
            InfoLine(sb, "Here", "\u00d7" + g.Units + " in " + g.Entries.Count + " " + noun + "  \u00b7  " + Kg(g.Weight) + " kg");
            if (g.Def.IsDamageable)
            {
                float lo = 1f, hi = 0f;
                for (int i = 0; i < g.Entries.Count; i++)
                {
                    float c = g.Entries[i].Stack.Condition01;
                    lo = Mathf.Min(lo, c);
                    hi = Mathf.Max(hi, c);
                }
                InfoLine(sb, "Wear", Pct(lo) + " \u2013 " + Pct(hi));
            }
            else if (g.Def.Perishable)
            {
                FoodState best = FoodState.Rotten, worst = FoodState.Fresh;
                for (int i = 0; i < g.Entries.Count; i++)
                {
                    FoodState st = g.Entries[i].Stack.State;
                    if (st < best) best = st;
                    if (st > worst) worst = st;
                }
                InfoLine(sb, "Freshness", best == worst ? best.ToString() : best + " \u2013 " + worst);
            }
            sb.Append("\n<size=12><color=#8b93a0>\u25ba opens the row into a line per item.</color></size>");
            return Rule + sb.ToString();
        }

        private static void InfoLine(System.Text.StringBuilder sb, string key, string value)
            => sb.Append("\n<color=#c8ccd2>").Append(key).Append(":</color> <b>").Append(value).Append("</b>");

        private static string Kg(float kg) => kg >= 10f ? kg.ToString("0.0") : kg.ToString("0.0#");
        private static string Pct(float frac01) => Mathf.RoundToInt(frac01 * 100f) + "%";
        private static Color ConditionColor(float c01) => c01 > 0.66f ? GoodCol : c01 > 0.33f ? WarnCol : BadCol;
        private static Color FreshnessColor(FoodState s) => s == FoodState.Rotten ? RottenCol : s == FoodState.Stale ? WarnCol : GoodCol;

        // A one-letter glyph for the row icon, so categories read at a glance.
        private static string CategoryGlyph(ItemCategory cat)
        {
            switch (cat)
            {
                case ItemCategory.Food: return "F";
                case ItemCategory.Drink: return "D";
                case ItemCategory.Medical: return "+";
                case ItemCategory.Weapon: return "W";
                case ItemCategory.Firearm: return "G";
                case ItemCategory.Ammo: return "A";
                case ItemCategory.Clothing: return "C";
                case ItemCategory.Material: return "M";
                default: return "\u00b7";   // Misc
            }
        }

        // Black or white glyph, whichever reads better on the item's tint.
        private static Color IconTextColor(Color bg)
        {
            float lum = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return lum > 0.55f ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
        }
        #endregion
    }
}
