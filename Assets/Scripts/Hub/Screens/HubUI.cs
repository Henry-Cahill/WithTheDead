using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Layout and row builders shared by the hub's screens (on top of <see cref="UIKit"/>): anchored areas and
    /// columns, cards, scroll lists with a scrollbar, item rows (name coloured by category, count, condition,
    /// found-in-raid and insured tags, hover tooltip) and small form controls.
    /// </summary>
    public static class HubUI
    {
        public static readonly Color CardColor = new Color(0.095f, 0.107f, 0.126f, 0.98f);
        public static readonly Color SelectedColor = new Color(0.25f, 0.20f, 0.11f, 0.98f);
        public static readonly Color HeaderColor = new Color(0.13f, 0.145f, 0.17f, 0.98f);
        public static readonly Color Info = new Color(0.52f, 0.72f, 0.92f);
        public static readonly Color Dimmer = new Color(0.42f, 0.45f, 0.50f);

        #region Text and colour
        /// <summary>Returns the colour associated with an item's category, or a default colour for special cases.</summary>
        public static Color CategoryColor(ItemDef d)
        {
            if (d == null) return UIKit.TextColor;
            if (d.Id == Money.Id) return UIKit.Accent;
            if (ArtifactCatalog.IsArtifact(d)) return Color.Lerp(ArtifactCatalog.ColorOf(d.Id), Color.white, 0.25f);
            switch (d.Category)
            {
                case ItemCategory.Food: return new Color(0.88f, 0.76f, 0.50f);
                case ItemCategory.Drink: return new Color(0.50f, 0.76f, 0.94f);
                case ItemCategory.Medical: return new Color(0.94f, 0.52f, 0.52f);
                case ItemCategory.Weapon: return new Color(0.86f, 0.66f, 0.44f);
                case ItemCategory.Firearm: return new Color(0.97f, 0.64f, 0.32f);
                case ItemCategory.Ammo: return new Color(0.90f, 0.82f, 0.48f);
                case ItemCategory.Material: return new Color(0.70f, 0.64f, 0.56f);
                case ItemCategory.Clothing: return new Color(0.64f, 0.80f, 0.58f);
                default: return new Color(0.78f, 0.79f, 0.86f);
            }
        }

        /// <summary>Returns the display name of an item, coloured by its category.</summary>
        public static string ItemName(ItemDef d) => d == null ? "?" : UIKit.Colored(d.DisplayName, CategoryColor(d));

        /// <summary>Returns the display name for an item ID, coloured by its category if known.</summary>
        public static string ItemName(string id)
        {
            ItemDef d = ItemDatabase.Get(id);
            return d != null ? ItemName(d) : id;
        }

        /// <summary>Returns a colour indicating condition quality (good, warn, bad) based on a 0‑1 value.</summary>
        public static Color ConditionColor(float c01) => c01 > 0.66f ? UIKit.Good : c01 > 0.33f ? UIKit.Warn : UIKit.Bad;

        /// <summary>Gets a short text for the condition of an item stack, coloured by condition.</summary>
        public static string ConditionText(ItemStack s)
        {
            if (s == null || s.Def == null || !s.Def.IsDamageable) return "";
            return UIKit.Colored(Mathf.RoundToInt(s.Condition01 * 100f) + "%", ConditionColor(s.Condition01));
        }

        /// <summary>Builds HTML‑style tags for found‑in‑raid and insured markers on an item stack.</summary>
        public static string Tags(ItemStack s)
        {
            var sb = new StringBuilder();
            if (s.IsFoundInRaid()) sb.Append(UIKit.Colored("\u2713 FIR", UIKit.Good));
            if (s.IsInsured()) { if (sb.Length > 0) sb.Append("  "); sb.Append(UIKit.Colored("INS", Info)); }
            return sb.ToString();
        }

        /// <summary>Formats a fraction (0‑1) as a percentage string.</summary>
        public static string Pct(float f) => Mathf.RoundToInt(f * 100f) + "%";

        /// <summary>"+12%" / "-8%" for a multiplier (green when the change is good).</summary>
        /// <summary>Converts a time delta multiplier into a coloured "+12%" / "-8%" string, green when the change is good.</summary>
        public static string Delta(float mult, bool lowerIsBetter)
        {
            int p = Mathf.RoundToInt((mult - 1f) * 100f);
            if (p == 0) return UIKit.Colored("\u00b10%", UIKit.TextDim);
            bool good = lowerIsBetter ? p < 0 : p > 0;
            return UIKit.Colored((p > 0 ? "+" : "") + p + "%", good ? UIKit.Good : UIKit.Bad);
        }

        /// <summary>Formats a duration in seconds as a human readable string (e.g., "1h 23m 45s").</summary>
        public static string Duration(long seconds)
        {
            if (seconds <= 0) return "now";
            if (seconds >= 86400) return (seconds / 86400) + "d " + (seconds % 86400 / 3600) + "h";
            if (seconds >= 3600) return (seconds / 3600) + "h " + (seconds % 3600 / 60).ToString("00") + "m";
            return (seconds / 60) + "m " + (seconds % 60).ToString("00") + "s";
        }

        /// <summary>Formats a fraction of hours as a short string (e.g., "2h 30m" or "45m").</summary>
        public static string Hours(float hours)
        {
            if (hours <= 0f) return "0h";
            int mins = Mathf.RoundToInt(hours * 60f);
            return mins >= 60 ? (mins / 60) + "h " + (mins % 60).ToString("00") + "m" : mins + "m";
        }

        /// <summary>The item's own tooltip plus what is particular to this one stack (wear, repairs, mods, upgrades).</summary>
        public static string StackTooltip(ItemStack s)
        {
            if (s == null || s.Def == null) return null;
            var sb = new StringBuilder(s.Def.Tooltip());
            bool any = false;
            void Line(string k, string v)
            {
                if (!any) { sb.Append("\n<color=#5d636d>\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500</color>"); any = true; }
                sb.Append("\n<color=#c8ccd2>").Append(k).Append(":</color> <b>").Append(v).Append("</b>");
            }
            if (s.Count > 1) Line("Count", s.Count.ToString());
            if (s.Def.IsDamageable)
            {
                string c = s.Condition + " / " + s.Def.MaxCondition + " (" + Pct(s.Condition01) + ")";
                if (s.TimesRepaired > 0) c += ", repaired " + s.TimesRepaired + "\u00d7";
                Line(s.Def.UsesCharge ? "Charge" : "This one", c);
            }
            if (s.IsFoundInRaid()) Line("Found in raid", UIKit.Colored("yes \u2014 traders pay a little more", UIKit.Good));
            if (s.IsInsured()) Line("Insured", UIKit.Colored("comes back if lost and nobody takes it", Info));
            ItemExtra x = s.Extra;
            if (x != null)
            {
                if (x.Mods.Count > 0)
                {
                    var names = new List<string>();
                    for (int i = 0; i < x.Mods.Count; i++) names.Add(Trading.NameOf(x.Mods[i]));
                    Line("Attachments", string.Join(", ", names));
                }
                if (x.Upgrades.Count > 0)
                {
                    var names = new List<string>();
                    for (int i = 0; i < x.Upgrades.Count; i++)
                    {
                        WeaponUpgrades.Node n = WeaponUpgrades.Get(x.Upgrades[i]);
                        names.Add(n != null ? n.Name : x.Upgrades[i]);
                    }
                    Line("Upgrades", string.Join(", ", names));
                }
                if (x.Dirt > 0.05f) Line("Fouling", Pct(x.Dirt));
                if (!string.IsNullOrEmpty(x.Note)) Line("Note", x.Note);
            }
            if (ArtifactCatalog.IsArtifact(s.Def))
            {
                string d = ArtifactCatalog.Describe(s.Def.Id);
                if (!string.IsNullOrEmpty(d) && !sb.ToString().Contains(d)) sb.Append('\n').Append(d);
            }
            return sb.ToString();
        }

        /// <summary>The stacks of an inventory passing <paramref name="filter"/>, by category then name (the inventory isn't reordered).</summary>
        public static List<ItemStack> Sorted(Inventory inv, Func<ItemStack, bool> filter)
        {
            var list = new List<ItemStack>();
            if (inv == null) return list;
            for (int i = 0; i < inv.Stacks.Count; i++)
            {
                ItemStack s = inv.Stacks[i];
                if (s == null || s.Def == null || s.Count <= 0) continue;
                if (filter == null || filter(s)) list.Add(s);
            }
            list.Sort((a, b) =>
            {
                int c = ((int)a.Def.Category).CompareTo((int)b.Def.Category);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.Def.DisplayName, b.Def.DisplayName);
                return c != 0 ? c : b.Count.CompareTo(a.Count);
            });
            return list;
        }

        /// <summary>"2x Gold Chain (1)" for a requirement, green when owned (stash + pack), red when short.</summary>
        /// <summary>Creates a UI requirement string for an item, coloured green if owned enough, red otherwise.</summary>
        public static string Need(string id, int n, PlayerInventory carried)
        {
            int have = Trading.CountOwned(id, carried);
            return UIKit.Colored(n + "x " + Trading.NameOf(id) + " (" + have + ")", have >= n ? UIKit.Good : UIKit.Bad);
        }

        /// <summary>Creates a UI string for a money amount, coloured green if affordable, red otherwise.</summary>
        public static string NeedMoney(int amount, PlayerInventory carried)
            => UIKit.Colored(Money.Format(amount), Money.Available(carried) >= amount ? UIKit.Good : UIKit.Bad);
        #endregion

        #region Areas and cards
        /// <summary>A rect spanning a normalised region of its parent, inset by a few pixels.</summary>
        public static RectTransform Area(Transform parent, float x0, float y0, float x1, float y1, float l = 0f, float b = 0f, float r = 0f, float t = 0f)
        {
            RectTransform rt = UIKit.Rect("Area", parent);
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>A full-width band of a fixed height, <paramref name="top"/> pixels below the parent's top edge.</summary>
        /// <summary>Creates a full‑width band rect a fixed height below the parent's top.</summary>
        public static RectTransform Band(Transform parent, float top, float height)
        {
            RectTransform rt = UIKit.Rect("Band", parent);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(top + height));
            rt.offsetMax = new Vector2(0f, -top);
            return rt;
        }

        /// <summary>Fills the parent below <paramref name="top"/> pixels (and above <paramref name="bottom"/>).</summary>
        /// <summary>Creates a RectTransform that fills the parent between top and bottom offsets.</summary>
        public static RectTransform Below(Transform parent, float top, float bottom = 0f)
            => UIKit.Stretch(UIKit.Rect("Area", parent), 0f, bottom, 0f, top);

        /// <summary>Splits a rect into side-by-side columns by weight, with a gap between them.</summary>
        /// <summary>Splits a rect into side‑by‑side columns weighted by the provided values, with gaps.</summary>
        public static RectTransform[] Columns(Transform parent, float gap, params float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++) total += weights[i];
            var cols = new RectTransform[weights.Length];
            float acc = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                float x0 = acc / total;
                acc += weights[i];
                float x1 = acc / total;
                cols[i] = Area(parent, x0, 0f, x1, 1f, i == 0 ? 0f : gap * 0.5f, 0f, i == weights.Length - 1 ? 0f : gap * 0.5f, 0f);
                cols[i].name = "Column" + i;
            }
            return cols;
        }

        /// <summary>A card background filling <paramref name="area"/>.</summary>
        /// <summary>Creates an Image component for a card background, optionally using a custom colour.</summary>
        public static Image Card(RectTransform area, Color? color = null)
        {
            Image img = area.gameObject.GetComponent<Image>();
            if (img == null) img = area.gameObject.AddComponent<Image>();
            img.color = color ?? CardColor;
            // Add a thin inner worn‑edge border for subtle visual depth
            RectTransform border = UIKit.Rect("Border", area);
            border.offsetMin = new Vector2(1f, 1f);
            border.offsetMax = new Vector2(-1f, -1f);
            var borderImg = border.gameObject.AddComponent<Image>();
            float darken = 0.15f;
            Color baseColor = color ?? CardColor;
            borderImg.color = Color.Lerp(baseColor, Color.black, darken);
            return img;
        }

        /// <summary>A vertical stack filling a rect (for headers and forms that don't scroll).</summary>
        /// <summary>Creates a vertical stack RectTransform inside an area for headers/forms.</summary>
        public static RectTransform Stack(RectTransform area, float spacing = 4f, int padding = 10)
        {
            RectTransform col = UIKit.Column(area, spacing, padding);
            UIKit.Stretch(col);
            return col;
        }

        /// <summary>A scroll list filling <paramref name="area"/> below <paramref name="top"/> pixels, with a thin scrollbar.</summary>
        /// <summary>Creates a scrollable list RectTransform with a thin scrollbar.</summary>
        public static RectTransform Scroll(RectTransform area, float top = 0f, float bottom = 0f, float spacing = 3f)
        {
            RectTransform content = UIKit.ScrollList(area, "List", spacing, 6);
            var view = (RectTransform)content.parent;
            UIKit.Stretch(view, 0f, bottom, 0f, top);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.padding = new RectOffset(6, 16, 6, 8);

            RectTransform sb = UIKit.Rect("Scrollbar", view);
            sb.anchorMin = new Vector2(1f, 0f);
            sb.anchorMax = new Vector2(1f, 1f);
            sb.pivot = new Vector2(1f, 0.5f);
            sb.offsetMin = new Vector2(-8f, 3f);
            sb.offsetMax = new Vector2(-2f, -3f);
            sb.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            RectTransform slide = UIKit.Rect("Sliding Area", sb);
            UIKit.Stretch(slide);
            RectTransform handle = UIKit.Rect("Handle", slide);
            UIKit.Stretch(handle);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.color = new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, 0.55f);
            // Add subtle worn‑edge accent: a thin darker overlay inside the handle
            RectTransform edge = UIKit.Rect("Edge", handle);
            // inset a few pixels to create an inner border effect
            edge.offsetMin = new Vector2(2f, 2f);
            edge.offsetMax = new Vector2(-2f, -2f);
            var edgeImg = edge.gameObject.AddComponent<Image>();
            edgeImg.color = new Color(0f, 0f, 0f, 0.2f);
            var bar = sb.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle;
            bar.targetGraphic = hImg;
            bar.direction = Scrollbar.Direction.BottomToTop;
            var scroll = view.GetComponent<ScrollRect>();
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }

        /// <summary>A bold heading line inside a vertical layout.</summary>
        /// <summary>Creates a bold heading Text inside a vertical layout.</summary>
        public static Text Heading(Transform parent, string text, int size = 20, Color? color = null)
        {
            Text t = UIKit.Label(parent, text, size, color ?? UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Size(t.gameObject, -1f, size + 10f);
            return t;
        }

        /// <summary>A heading drawn straight onto an area's top edge (above a list placed with <see cref="Scroll"/>).</summary>
        /// <summary>Creates a heading Text placed on an area's top edge, above a Scroll list.</summary>
        public static Text AreaHeading(RectTransform area, string text, int size = 20, float height = 34f)
        {
            Text t = UIKit.Label(area, text, size, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            RectTransform rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(8f, -height);
            rt.offsetMax = new Vector2(-8f, 0f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(11, size - 6);
            t.resizeTextMaxSize = size;
            return t;
        }

        /// <summary>A wrapped paragraph inside a vertical layout (its height follows the text).</summary>
        /// <summary>Creates a paragraph Text inside a vertical layout.</summary>
        public static Text Para(Transform parent, string text, int size = 15, Color? color = null, FontStyle style = FontStyle.Normal)
        {
            Text t = UIKit.Label(parent, text, size, color ?? UIKit.TextColor, TextAnchor.UpperLeft, style);
            return t;
        }

        /// <summary>A fixed-height row whose children keep their own heights (centred), unlike <see cref="UIKit.Row"/>.</summary>
        /// <summary>Creates a fixed‑height row RectTransform with optional background colour.</summary>
        public static RectTransform Row(Transform parent, float height, Color? background = null, float spacing = 8f, int padding = 8)
        {
            RectTransform row = UIKit.Row(parent, height, background, spacing, padding);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
            return row;
        }

        /// <summary>A horizontal group sized by its tallest child (cards side by side); children share the height.</summary>
        /// <summary>Creates a horizontal AutoRow RectTransform that sizes to its tallest child.</summary>
        public static RectTransform AutoRow(Transform parent, float spacing = 8f)
        {
            RectTransform rt = UIKit.Rect("AutoRow", parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return rt;
        }

        /// <summary>A vertical card (tinted background, padded) inside a layout; its height follows its content.</summary>
        /// <summary>Creates a vertical card RectTransform with optional colour, spacing and padding.</summary>
        public static RectTransform CardColumn(Transform parent, Color? color = null, float spacing = 3f, int padding = 10, float flexibleWidth = -1f)
        {
            RectTransform col = UIKit.Column(parent, spacing, padding);
            col.gameObject.AddComponent<Image>().color = color ?? CardColor;
            if (flexibleWidth >= 0f) UIKit.Size(col.gameObject, 12f, -1f, flexibleWidth);
            return col;
        }

        /// <summary>A list section header row (full width, tinted).</summary>
        /// <summary>Creates a section header Text row inside a list.</summary>
        public static Text Section(Transform list, string text, int size = 16)
        {
            RectTransform row = Row(list, size + 14f, HeaderColor, 6f, 8);
            Text t = Cell(row, text, -1f, size, UIKit.Accent);
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        /// <summary>A dim one-line note row inside a list.</summary>
        /// <summary>Creates a dimmed note Text row inside a list.</summary>
        public static Text Note(Transform list, string text, Color? color = null, int size = 14)
        {
            Text t = UIKit.Label(list, text, size, color ?? UIKit.TextDim, TextAnchor.UpperLeft);
            return t;
        }

        /// <summary>A single-line text cell in a row: fixed <paramref name="width"/>, or flexible when negative. Shrinks to fit.</summary>
        /// <summary>Creates a single‑line Text cell in a row, with optional width and alignment.</summary>
        public static Text Cell(Transform row, string text, float width, int size = 15, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            Text t = UIKit.Label(row, text, size, color ?? UIKit.TextColor, align);
            if (width >= 0f) UIKit.Size(t.gameObject, width, size + 8f);
            else UIKit.Size(t.gameObject, 12f, size + 8f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, size - 5);
            t.resizeTextMaxSize = size;
            return t;
        }

        /// <summary>A button of a fixed size inside a layout row.</summary>
        /// <summary>Creates a button Text of fixed size inside a layout row.</summary>
        public static Button Btn(Transform parent, string text, Action onClick, float width, float height = 30f, Color? color = null, bool interactable = true, int fontSize = 15)
        {
            Button b = UIKit.Button(parent, text, onClick, color ?? UIKit.ButtonColor, fontSize);
            UIKit.Size(b.gameObject, width, height);
            // Add thin inner dark border
            RectTransform border = UIKit.Rect("Border", b.gameObject.transform);
            border.offsetMin = new Vector2(1f, 1f);
            border.offsetMax = new Vector2(-1f, -1f);
            var borderImg = border.gameObject.AddComponent<Image>();
            borderImg.color = new Color(0f, 0f, 0f, 0.2f);
            b.interactable = interactable;
            Text label = b.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Max(10, fontSize - 5);
                label.resizeTextMaxSize = fontSize;
                if (!interactable) label.color = new Color(1f, 1f, 1f, 0.55f);
            }
            return b;
        }

        /// <summary>A horizontal row of buttons inside a vertical layout.</summary>
        /// <summary>Creates a horizontal row of buttons inside a vertical layout.</summary>
        public static RectTransform ButtonRow(Transform parent, float height = 36f)
        {
            RectTransform row = Row(parent, height, null, 8f, 0);
            return row;
        }

        /// <summary>A flexible spacer inside a row.</summary>
        /// <summary>Adds a flexible spacer RectTransform inside a row.</summary>
        public static void Spacer(Transform row, float flexible = 1f)
        {
            RectTransform rt = UIKit.Rect("Spacer", row);
            UIKit.Size(rt.gameObject, 0f, -1f, flexible);
        }

        /// <summary>A labelled bar inside a vertical layout: "Label ........ value" over a thin fill.</summary>
        /// <summary>Creates a labeled bar with a value and a fill proportion.</summary>
        public static Image LabeledBar(Transform parent, string label, string value, float value01, Color fill, float barHeight = 8f)
        {
            RectTransform row = Row(parent, 24f, null, 6f, 0);
            Cell(row, label, -1f, 15, UIKit.TextColor);
            Cell(row, value, 220f, 15, UIKit.TextDim, TextAnchor.MiddleRight);
            Image img = UIKit.Bar(parent, fill, value01, barHeight);
            return img;
        }

        /// <summary>Makes a row clickable (its background tints like a button).</summary>
        /// <summary>Makes a row clickable, adding a button with tinted background.</summary>
        public static Button Clickable(RectTransform row, Action onClick)
        {
            Image img = row.GetComponent<Image>();
            if (img == null) { img = row.gameObject.AddComponent<Image>(); img.color = UIKit.RowColor; }
            var b = row.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;     // the hover tint comes from the row's tooltip trigger
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        /// <summary>A label with a cycling value button ("HUD mode   [ Minimal ]").</summary>
        /// <summary>Creates a button that cycles through options, showing the current one.</summary>
        public static Button Cycle(Transform parent, string label, string[] options, int index, Action<int> onChanged, bool interactable = true)
        {
            // Defensive: if no options, show a disabled placeholder button
            if (options == null || options.Length == 0)
            {
                var btn = UIKit.Button(parent, "<none>", null, UIKit.ButtonColor, 16);
                btn.interactable = false;
                return btn;
            }
            RectTransform row = UIKit.Row(parent, 32f, null, 10f, 2);
            Text name = UIKit.Label(row, label, 17);
            UIKit.Size(name.gameObject, 260f, 28f);
            int cur = Mathf.Clamp(index, 0, options.Length - 1);
            Button b = null;
            b = UIKit.Button(row, options[cur], () =>
            {
                cur = (cur + 1) % options.Length;
                UIKit.SetButtonText(b, options[cur]);
                Sfx.PlayUI(SfxIds.UiClick);
                onChanged?.Invoke(cur);
            }, UIKit.ButtonColor, 16);
            UIKit.Size(b.gameObject, -1f, 28f, 1f);
            b.interactable = interactable;
            return b;
        }
        #endregion

        #region Item rows
        /// <summary>
        /// The standard item row: name (coloured by category), count, condition, found-in-raid and insured tags.
        /// Hovering shows the item's tooltip (plus <paramref name="extraTooltip"/>). Append buttons to the result.
        /// </summary>
        /// <summary>Creates a standard item row with name, count, condition, tags and tooltip.</summary>
        public static RectTransform ItemRow(Transform list, ItemStack s, HubTooltip tip, int index, string extraTooltip = null, float height = 34f, string nameSuffix = null, Color? background = null)
        {
            Color bg = background ?? (index % 2 == 0 ? UIKit.RowColor : UIKit.RowAlt);
            RectTransform row = Row(list, height, bg, 8f, 8);
            Cell(row, ItemName(s.Def) + (nameSuffix ?? ""), -1f, 16);
            Cell(row, s.Count > 1 ? "x" + s.Count : "", 54f, 15, UIKit.TextDim, TextAnchor.MiddleRight);
            Cell(row, ConditionText(s), 46f, 15, UIKit.TextColor, TextAnchor.MiddleRight);
            Cell(row, Tags(s), 84f, 13, UIKit.TextColor, TextAnchor.MiddleCenter);
            if (tip != null)
                tip.Attach(row.gameObject, () => StackTooltip(s) + (string.IsNullOrEmpty(extraTooltip) ? "" : "\n" + extraTooltip), row.GetComponent<Image>());
            return row;
        }

        /// <summary>A two-line row: a title over a detail line, with room for cells and buttons to the right.</summary>
        /// <summary>Creates a two‑line row with a top title and a bottom detail line.</summary>
        public static RectTransform TwoLineRow(Transform list, string top, string bottom, int index, out Text topText, out Text bottomText, float height = 54f, Color? background = null)
        {
            Color bg = background ?? (index % 2 == 0 ? UIKit.RowColor : UIKit.RowAlt);
            RectTransform row = Row(list, height, bg, 8f, 8);
            RectTransform info = UIKit.Rect("Info", row);
            var v = info.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = true;
            v.spacing = 0f;
            v.padding = new RectOffset(0, 0, 2, 2);
            UIKit.Size(info.gameObject, 12f, height - 4f, 1f);
            topText = Cell(info, top, -1f, 16);
            bottomText = Cell(info, bottom, -1f, 14, UIKit.TextDim);
            return row;
        }
        #endregion
    }
}
