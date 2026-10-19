using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The end-of-raid report: EXTRACTED / KILLED IN ACTION / MISSING IN ACTION, how long the raid ran,
    /// the kills, the exit used and every item carried out (or lost) with its count and weight, then
    /// "Next Raid" (straight into a fresh raid) or "Main Menu". Freezes the world while it is up. Built
    /// from code (canvas sort 150: above the HUD and the panels, below the title screen) by
    /// <see cref="ExtractionManager"/> when a raid ends.
    /// </summary>
    public class RaidSummaryUI : MonoBehaviour
    {
        /// <summary>The report on screen, or null.</summary>
        public static RaidSummaryUI Current { get; private set; }

        private const int RowsPerColumn = 16;
        private const float RowHeight = 26f;
        private const float KeyDelay = 0.5f;          // real seconds before Enter is accepted

        private static readonly Color AmberText = new Color(0.95f, 0.68f, 0.22f);
        private static readonly Color DimText = new Color(0.55f, 0.58f, 0.64f);
        private static readonly Color BodyText = new Color(0.82f, 0.84f, 0.88f);
        private static readonly Color RaidButton = new Color(0.62f, 0.46f, 0.12f);
        private static readonly Color MenuButton = new Color(0.24f, 0.34f, 0.46f);

        private Font _font;
        private float _shownAt;
        private bool _leaving;

        /// <summary>Puts the report up for a finished raid (once; later calls return the open one).</summary>
        public static RaidSummaryUI Show(RaidSummary summary)
        {
            if (summary == null) return null;
            if (Current != null) return Current;
            var ui = new GameObject("RaidSummary").AddComponent<RaidSummaryUI>();
            ui.Build(summary);
            return ui;
        }

        /// <summary>Colour used for an outcome's title and accents.</summary>
        public static Color OutcomeColor(ExtractionManager.RaidState outcome)
        {
            switch (outcome)
            {
                case ExtractionManager.RaidState.Extracted: return new Color(0.40f, 0.86f, 0.42f);
                case ExtractionManager.RaidState.Dead: return new Color(0.86f, 0.22f, 0.20f);
                default: return new Color(0.92f, 0.62f, 0.18f);
            }
        }

        /// <summary>The report's headline for an outcome.</summary>
        public static string OutcomeTitle(ExtractionManager.RaidState outcome)
        {
            switch (outcome)
            {
                case ExtractionManager.RaidState.Extracted: return "EXTRACTED";
                case ExtractionManager.RaidState.Dead: return "KILLED IN ACTION";
                default: return "MISSING IN ACTION";
            }
        }

        private void Awake()
        {
            Current = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            if (_leaving) return;
            Keyboard kb = Keyboard.current;
            if (kb == null || Time.unscaledTime - _shownAt < KeyDelay) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) OnNextRaid();
        }

        // Keep the world frozen and the cursor free even if a panel underneath closes and lets go.
        private void LateUpdate()
        {
            if (_leaving) return;
            Freeze();
        }

        private static void Freeze()
        {
            Time.timeScale = 0f;
            UIState.GameplayBlocked = true;
            if (!Cursor.visible) Cursor.visible = true;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        }

        private void OnNextRaid() => Leave(true);

        private void OnMainMenu() => Leave(false);

        // Reloading the scene lets the bootstrapper rebuild from scratch: back to the hub (the combined loop —
        // every path returns to the hub), straight into a new raid without a profile, or to the title screen.
        private void Leave(bool nextRaid)
        {
            if (_leaving) return;
            _leaving = true;
            Time.timeScale = 1f;
            UIState.GameplayBlocked = false;
            if (nextRaid && ProfileStore.ActiveCharacter != null)
            {
                ExtractionManager.ReturnFromRaid();
                return;
            }
            GameSession.AutoStartRaid = nextRaid;
            GameFlow.Reload();
        }

        #region UI construction
        private void Build(RaidSummary s)
        {
            Freeze();
            _shownAt = Time.unscaledTime;
            Color accent = OutcomeColor(s.Outcome);
            bool kept = s.Outcome == ExtractionManager.RaidState.Extracted;

            var canvasGo = new GameObject("RaidSummary Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;   // above the HUD and panels, below the pause menu / title screen
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform root = canvas.GetComponent<RectTransform>();

            var page = new GameObject("Page");
            var pageRt = page.AddComponent<RectTransform>();
            pageRt.SetParent(root, false);
            Stretch(pageRt);
            page.AddComponent<CanvasGroup>();
            var fader = page.AddComponent<UIFader>();   // fades in on unscaled time (the world is frozen)

            // Shade out the frozen world (and anything the HUD put up) behind the report.
            var dim = new GameObject("Dim");
            var dimRt = dim.AddComponent<RectTransform>();
            dimRt.SetParent(pageRt, false);
            Stretch(dimRt);
            dim.AddComponent<Image>().color = new Color(0.02f, 0.025f, 0.035f, 0.93f);

            var panel = new GameObject("Panel");
            var prt = panel.AddComponent<RectTransform>();
            prt.SetParent(pageRt, false);
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0f, 14f);
            prt.sizeDelta = new Vector2(1160f, 880f);
            panel.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.10f, 0.97f);
            UITheme.AddTopAccent(prt, accent, 4f);

            var top = new Vector2(0.5f, 1f);
            Label(prt, "Caption", top, new Vector2(0f, -22f), new Vector2(1000f, 24f), 17, TextAnchor.MiddleCenter, AmberText)
                .text = "EXTRACTION MODE  \u00b7  RAID REPORT";
            Label(prt, "Title", top, new Vector2(0f, -54f), new Vector2(1100f, 80f), 62, TextAnchor.MiddleCenter, accent)
                .text = OutcomeTitle(s.Outcome);
            Label(prt, "Detail", top, new Vector2(0f, -140f), new Vector2(1100f, 30f), 21, TextAnchor.MiddleCenter, BodyText)
                .text = s.DetailLine();

            // Raid time | kills | exit | clock.
            string clock = string.IsNullOrEmpty(s.ArrivedClock) ? "\u2014" : s.ArrivedClock + "  \u2192  " + s.EndedClock;
            Stat(prt, -435f, "RAID TIME", RaidSummary.FormatDuration(s.DurationSeconds));
            Stat(prt, -145f, "KILLS", s.Kills.ToString());
            Stat(prt, 145f, "EXIT", string.IsNullOrEmpty(s.ExitName) ? "\u2014" : s.ExitName);
            Stat(prt, 435f, "CLOCK", clock);

            var rule = new GameObject("Rule");
            var ruleRt = rule.AddComponent<RectTransform>();
            ruleRt.SetParent(prt, false);
            ruleRt.anchorMin = ruleRt.anchorMax = ruleRt.pivot = top;
            ruleRt.anchoredPosition = new Vector2(0f, -272f);
            ruleRt.sizeDelta = new Vector2(1080f, 2f);
            var ruleImg = rule.AddComponent<Image>();
            ruleImg.color = new Color(0.24f, 0.26f, 0.30f, 1f);
            ruleImg.raycastTarget = false;

            string heading = (kept ? "CARRIED OUT" : "LOST") + "  \u00b7  " + s.TotalKg.ToString("0.0") + " kg";
            Label(prt, "ItemsHeading", top, new Vector2(0f, -288f), new Vector2(1080f, 30f), 22, TextAnchor.MiddleLeft,
                kept ? OutcomeColor(ExtractionManager.RaidState.Extracted) : OutcomeColor(ExtractionManager.RaidState.Dead))
                .text = heading;

            BuildItemRows(prt, s);

            var bottom = new Vector2(0.5f, 0f);
            bool toHub = ProfileStore.ActiveCharacter != null;
            Button next = MakeButton(prt, bottom, new Vector2(-170f, 40f), new Vector2(300f, 60f), toHub ? "Return to the Hub" : "Next Raid", RaidButton, OnNextRaid);
            MakeButton(prt, bottom, new Vector2(170f, 40f), new Vector2(300f, 60f), "Main Menu", MenuButton, OnMainMenu);
            next.Select();

            Label(pageRt, "Hint", new Vector2(0.5f, 0.025f), Vector2.zero, new Vector2(1200f, 24f), 15, TextAnchor.MiddleCenter, DimText)
                .text = kept
                    ? "Enter \u2014 Next Raid  \u00b7  your kit is waiting for the next raid"
                    : "Enter \u2014 Next Raid  \u00b7  you'll go in with the default kit";

            fader.Show();
        }

        // Two columns of "2x  Bandage ........ 0.20 kg" rows; anything past the room left says how many more.
        private void BuildItemRows(RectTransform panel, RaidSummary s)
        {
            List<RaidSummaryLine> lines = s.Lines;
            var top = new Vector2(0.5f, 1f);
            const float firstRowY = -330f;
            if (lines.Count == 0)
            {
                Label(panel, "Nothing", top, new Vector2(0f, firstRowY), new Vector2(1080f, RowHeight), 18, TextAnchor.MiddleLeft, DimText)
                    .text = "Nothing at all.";
                return;
            }

            int capacity = RowsPerColumn * 2;
            int shown = lines.Count <= capacity ? lines.Count : capacity - 1;
            for (int i = 0; i < shown; i++)
            {
                RaidSummaryLine line = lines[i];
                float colX = i < RowsPerColumn ? -280f : 280f;
                float y = firstRowY - (i % RowsPerColumn) * RowHeight;

                string name = line.Count > 1 ? line.Count + "\u00d7  " + line.Name : line.Name;
                if (!string.IsNullOrEmpty(line.Note)) name += "  <color=#8b93a0>(" + line.Note + ")</color>";
                Text nameText = Label(panel, "Item", top, new Vector2(colX - 50f, y), new Vector2(420f, RowHeight), 18, TextAnchor.MiddleLeft, BodyText);
                nameText.supportRichText = true;
                nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameText.verticalOverflow = VerticalWrapMode.Truncate;
                nameText.text = name;

                Label(panel, "Kg", top, new Vector2(colX + 210f, y), new Vector2(100f, RowHeight), 16, TextAnchor.MiddleRight, DimText)
                    .text = line.Kg.ToString("0.00") + " kg";
            }

            if (shown < lines.Count)
            {
                float y = firstRowY - (RowsPerColumn - 1) * RowHeight;
                Label(panel, "More", top, new Vector2(230f, y), new Vector2(420f, RowHeight), 18, TextAnchor.MiddleLeft, DimText)
                    .text = "\u2026 and " + (lines.Count - shown) + " more";
            }
        }

        private void Stat(RectTransform panel, float x, string caption, string value)
        {
            var top = new Vector2(0.5f, 1f);
            Label(panel, caption, top, new Vector2(x, -186f), new Vector2(270f, 20f), 14, TextAnchor.MiddleCenter, DimText)
                .text = caption;
            Label(panel, caption + "Value", top, new Vector2(x, -210f), new Vector2(280f, 40f), 28, TextAnchor.MiddleCenter, Color.white)
                .text = value;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var txt = go.AddComponent<Text>();
            txt.font = _font; txt.fontSize = fontSize; txt.alignment = align; txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            UITheme.AddShadow(txt);
            return txt;
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
            cb.disabledColor = new Color(0.18f, 0.18f, 0.2f, 0.9f);
            cb.selectedColor = cb.highlightedColor;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var txtGo = new GameObject("Text");
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.SetParent(rt, false);
            Stretch(txtRt);
            var txt = txtGo.AddComponent<Text>();
            txt.font = _font; txt.fontSize = 22; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = text;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.raycastTarget = false;
            UITheme.AddShadow(txt);
            return btn;
        }
        #endregion
    }

    /// <summary>Everything the raid report shows, gathered by <see cref="ExtractionManager"/> as the raid ends.</summary>
    public sealed class RaidSummary
    {
        public ExtractionManager.RaidState Outcome;
        /// <summary>Name of the exit used (null unless extracted).</summary>
        public string ExitName;
        public float DurationSeconds;
        public int Kills;
        /// <summary>Watch times the raid began and ended at, e.g. "12:40 AM".</summary>
        public string ArrivedClock, EndedClock;
        /// <summary>How the survivor died (the game's cause line, e.g. "was torn apart").</summary>
        public string Cause;
        /// <summary>Raids survived in a row with this kit (extractions only).</summary>
        public int Streak;
        /// <summary>The kit carried out (or lost): worn pieces first, then the pack.</summary>
        public readonly List<RaidSummaryLine> Lines = new List<RaidSummaryLine>();
        public float TotalKg;

        /// <summary>The one-line story of the raid under the headline.</summary>
        public string DetailLine()
        {
            string who = GameSession.SurvivorName;
            switch (Outcome)
            {
                case ExtractionManager.RaidState.Extracted:
                    string where = string.IsNullOrEmpty(ExitName) ? "an exit" : ExitName;
                    string streak = Streak > 1 ? "  \u00b7  " + Streak + " raids in a row" : "";
                    return who + " made it out at " + where + ". The kit below goes into the next raid." + streak;
                case ExtractionManager.RaidState.Dead:
                    string cause = string.IsNullOrEmpty(Cause) ? "died in the raid" : Cause;
                    return who + " " + cause + ". Everything carried is lost.";
                default:
                    return "Dawn broke before " + who + " reached an exit. Everything carried is lost.";
            }
        }

        /// <summary>Lists a kit: one line per worn piece, then the pack merged by item (with the weapon in
        /// hand and its loaded rounds noted), and adds up the weight.</summary>
        public void AddKit(RaidKit kit)
        {
            Lines.Clear();
            TotalKg = 0f;
            if (kit == null) return;

            if (kit.Clothing != null)
            {
                for (int i = 0; i < kit.Clothing.Count; i++)
                {
                    RaidKitPiece p = kit.Clothing[i];
                    ItemDef def = p != null && !string.IsNullOrEmpty(p.Id) ? ItemDatabase.Get(p.Id) : null;
                    if (def == null) continue;
                    Lines.Add(new RaidSummaryLine { Def = def, Name = def.DisplayName, Count = 1, Kg = def.Weight, Note = "worn" });
                    TotalKg += def.Weight;
                }
            }
            int worn = Lines.Count;

            if (kit.Items != null)
            {
                for (int i = 0; i < kit.Items.Count; i++)
                {
                    RaidKitStack st = kit.Items[i];
                    ItemDef def = st != null && !string.IsNullOrEmpty(st.Id) && st.Count > 0 ? ItemDatabase.Get(st.Id) : null;
                    if (def == null) continue;
                    float kg = def.Weight * st.Count;
                    TotalKg += kg;
                    RaidSummaryLine line = null;
                    for (int k = worn; k < Lines.Count; k++)
                        if (Lines[k].Def == def) { line = Lines[k]; break; }
                    if (line == null) Lines.Add(new RaidSummaryLine { Def = def, Name = def.DisplayName, Count = st.Count, Kg = kg });
                    else { line.Count += st.Count; line.Kg += kg; }
                }
            }

            ItemDef weapon = string.IsNullOrEmpty(kit.EquippedWeaponId) ? null : ItemDatabase.Get(kit.EquippedWeaponId);
            if (weapon != null)
            {
                for (int k = worn; k < Lines.Count; k++)
                {
                    if (Lines[k].Def != weapon) continue;
                    Lines[k].Note = weapon.IsFirearm && kit.LoadedAmmo > 0 ? "in hand, " + kit.LoadedAmmo + " loaded" : "in hand";
                    break;
                }
                // Rounds sitting in the magazine weigh their share too.
                ItemDef ammo = weapon.IsFirearm && kit.LoadedAmmo > 0 && !string.IsNullOrEmpty(weapon.AmmoId)
                    ? ItemDatabase.Get(weapon.AmmoId) : null;
                if (ammo != null) TotalKg += ammo.Weight * kit.LoadedAmmo;
            }

            if (Lines.Count - worn > 1) Lines.Sort(worn, Lines.Count - worn, LineOrder.Instance);
        }

        /// <summary>"23:41" (minutes:seconds), or "1:02:05" past an hour.</summary>
        public static string FormatDuration(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int h = total / 3600, m = (total / 60) % 60, s = total % 60;
            return h > 0 ? string.Format("{0}:{1:00}:{2:00}", h, m, s) : string.Format("{0}:{1:00}", m, s);
        }

        // Weapons and ammo first, then medical, food and drink, then the rest; by name within a group.
        private sealed class LineOrder : IComparer<RaidSummaryLine>
        {
            public static readonly LineOrder Instance = new LineOrder();

            public int Compare(RaidSummaryLine a, RaidSummaryLine b)
            {
                int ra = Rank(a.Def), rb = Rank(b.Def);
                if (ra != rb) return ra.CompareTo(rb);
                return string.CompareOrdinal(a.Name, b.Name);
            }

            private static int Rank(ItemDef d)
            {
                if (d == null) return 99;
                switch (d.Category)
                {
                    case ItemCategory.Firearm: return 0;
                    case ItemCategory.Weapon: return 1;
                    case ItemCategory.Ammo: return 2;
                    case ItemCategory.Medical: return 3;
                    case ItemCategory.Food: return 4;
                    case ItemCategory.Drink: return 5;
                    case ItemCategory.Clothing: return 6;
                    case ItemCategory.Misc: return 7;
                    default: return 8;
                }
            }
        }
    }

    /// <summary>One row of the raid report: an item, how many and how heavy, plus an optional note
    /// ("worn", "in hand, 15 loaded").</summary>
    public sealed class RaidSummaryLine
    {
        public ItemDef Def;
        public string Name;
        public int Count;
        public float Kg;
        public string Note;
    }
}
