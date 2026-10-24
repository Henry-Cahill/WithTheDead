using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A code-built health screen (toggled with H) inspired by Project Zomboid's
    /// health panel. It lists every wound with its state — bleeding, bandaged
    /// (clean or dirty), disinfected, festering or healing — alongside the pain
    /// level and any active infection, and lets the survivor treat themselves:
    /// bandage, disinfect, take antibiotics or painkillers, or use a first-aid kit,
    /// drawing the supplies from their inventory. While open it sets UIState.GameplayBlocked.
    /// </summary>
    public class HealthPanelController : MonoBehaviour
    {
        private const int MaxRows = 6;

        // Cached colours to avoid per‑frame allocations
        private static readonly Color PainHigh = new Color(0.9f, 0.3f, 0.25f);
        private static readonly Color PainMid = new Color(0.95f, 0.7f, 0.25f);
        private static readonly Color PainLow = new Color(0.72f, 0.85f, 0.72f);
        private static readonly Color PanicHigh = new Color(0.9f, 0.35f, 0.4f);
        private static readonly Color PanicMid = new Color(0.9f, 0.62f, 0.4f);
        private static readonly Color PanicLow = new Color(0.74f, 0.76f, 0.9f);
        private static readonly Color TempCold = new Color(0.5f, 0.72f, 0.95f);
        private static readonly Color TempHot = new Color(0.95f, 0.5f, 0.4f);
        private static readonly Color TempNormal = new Color(0.72f, 0.85f, 0.75f);
        private static readonly Color SicknessHigh = new Color(0.85f, 0.85f, 0.3f);
        private static readonly Color SicknessMid = new Color(0.8f, 0.82f, 0.5f);
        private static readonly Color SicknessLow = new Color(0.72f, 0.85f, 0.72f);
        private static readonly Color ClothingLabel = new Color(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color ClothingLabelDisabled = new Color(0.8f, 0.82f, 0.86f, 0.5f);
        private static readonly Color ButtonLabelEnabled = new Color(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color ButtonLabelDisabled = new Color(0.95f, 0.96f, 0.98f, 0.4f);

        private PlayerWounds _wounds;
        private PlayerStats _stats;
        private PlayerInventory _inventory;
        private PlayerPanic _panic;
        private PlayerTemperature _temperature;
        private PlayerClothing _clothing;
        private PlayerFoodSickness _nutrition;
        // Cached inventory supply counts, updated when inventory changes.
        // These replace per‑frame scanning of the inventory.
        private int _bandages;
        private int _disinfectant;
        private int _painkillers;
        private int _antibiotics;
        private int _kits;
        private int _betablockers;
        // Helper to recompute the cached counts from the current inventory.
        private void RefreshSupplyCounts()
        {
            _bandages = _disinfectant = _painkillers = _antibiotics = _kits = _betablockers = 0;
            if (_inventory == null) return;
            var stacks = _inventory.Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                var id = stack.Def.Id;
                int count = stack.Count;
                switch (id)
                {
                    case "bandage":
                    case "rag":
                        _bandages += count;
                        break;
                    case "disinfectant":
                        _disinfectant += count;
                        break;
                    case "pills":
                        _painkillers += count;
                        break;
                    case "antibiotics":
                        _antibiotics += count;
                        break;
                    case "firstaid":
                        _kits += count;
                        break;
                    case "betablockers":
                        _betablockers += count;
                        break;
                }
            }
        }
        private Font _font;
        private GameObject _panelRoot;
        private UIFader _fader;
        private bool _open;

        private Text _painText;
        private Text _panicText;
        private Text _thermalText;
        private Text _sicknessText;
        private Text _clothingText;
        private Text _statusText;
        private Text _emptyText;
        private Text[] _rows;

        // Worn-clothing slots — click one to take the garment off (back into the bag).
        private ClothingSlot[] _clothingSlots;
        private Button[] _clothingBtns;
        private Text[] _clothingLbls;

        // Self-treatment buttons (draw supplies from the inventory).
        private Button _bandageBtn, _disinfectBtn, _painkillerBtn, _antibioticBtn, _firstAidBtn, _calmBtn;
        private Text _bandageLbl, _disinfectLbl, _painkillerLbl, _antibioticLbl, _firstAidLbl, _calmLbl;

        public bool IsOpen => _open;

        public void Bind(PlayerWounds wounds, PlayerStats stats, PlayerInventory inventory = null, PlayerPanic panic = null, PlayerTemperature temperature = null, PlayerClothing clothing = null, PlayerFoodSickness nutrition = null)
        {
            _wounds = wounds;
            _stats = stats;
            // Unsubscribe previous inventory if any
            if (_inventory != null) _inventory.Changed -= RefreshSupplyCounts;
            _inventory = inventory;
            if (_inventory != null) _inventory.Changed += RefreshSupplyCounts;
            _panic = panic;
            _temperature = temperature;
            _clothing = clothing;
            _nutrition = nutrition;
            // Initial cache refresh
            RefreshSupplyCounts();
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.hKey.wasPressedThisFrame) Toggle();
                else if (_open && kb.escapeKey.wasPressedThisFrame) Close();
            }
            if (_open && _wounds != null && _stats != null) Refresh();
        }

        private void Toggle()
        {
            if (_open) Close();
            else Open();
        }

        private void Open()
        {
            if (UIState.GameplayBlocked) return;   // inventory, skills or sleep already owns the screen
            _open = true;
            UIState.GameplayBlocked = true;
            _fader.Show();
            Refresh();
        }

        private void Close()
        {
            _open = false;
            UIState.GameplayBlocked = false;
            _fader.Hide();
        }

        #region Self-treatment
        // Uses the first available of the given item ids on the player (Bandage prefers a
        // clean dressing, then a rag). All the "did it help / consume it" logic lives in
        // PlayerInventory.Use, so the buttons just hand it a stack.
        private void UseFirst(params string[] ids)
        {
            ItemStack stack = FindStack(ids);
            if (stack != null && _inventory != null) _inventory.Use(stack);
            if (_open) Refresh();
        }

        private ItemStack FindStack(string[] ids)
        {
            if (_inventory == null) return null;
            var stacks = _inventory.Inventory.Stacks;
            for (int k = 0; k < ids.Length; k++)
                for (int i = 0; i < stacks.Count; i++)
                    if (stacks[i].Def.Id == ids[k]) return stacks[i];
            return null;
        }

        private int Supply(params string[] ids)
        {
            if (_inventory == null) return 0;
            int n = 0;
            for (int k = 0; k < ids.Length; k++) n += _inventory.Count(ids[k]);
            return n;
        }

        // Takes off whatever is worn on a slot, returning it to the bag.
        private void TakeOffSlot(ClothingSlot slot)
        {
            if (_inventory != null) _inventory.TakeOff(slot);
            if (_open) Refresh();
        }
        #endregion

        private void Refresh()
        {
            // Pain.
            int painPct = Mathf.RoundToInt(_stats.PainNormalized * 100f);
            _painText.text = "PAIN  " + painPct + "%" +
                (_wounds.PainkillersActive ? "   <size=16><color=#7fd0ff>(painkillers)</color></size>" : "");
            _painText.color = _stats.PainNormalized > 0.6f ? PainHigh
                : _stats.PainNormalized > 0.3f ? PainMid
                : PainLow;

            // Panic.
            if (_panicText != null)
            {
                int panicPct = Mathf.RoundToInt(_stats.PanicNormalized * 100f);
                string word = _panic != null ? _panic.Descriptor : "Calm";
                _panicText.text = "PANIC  " + panicPct + "%   <size=15><color=#b0a6d6>(" + word + ")</color></size>" +
                    (_panic != null && _panic.BetaBlockersActive ? "  <size=15><color=#9fd0ff>(beta blockers)</color></size>" : "");
                int lvl = _panic != null ? _panic.Level : 0;
                _panicText.color = lvl >= 3 ? PanicHigh
                    : lvl >= 2 ? PanicMid
                    : PanicLow;
            }

            // Body temperature & wetness.
            if (_thermalText != null)
            {
                float t = _stats.Temperature;
                float wet = _temperature != null ? _temperature.Wetness : _stats.Wetness;
                string desc = _temperature != null ? _temperature.Descriptor : "";
                string wetDesc = _temperature != null ? _temperature.WetDescriptor : "";
                _thermalText.text = "TEMP  " + Mathf.RoundToInt(t * 100f) + "%  <size=15><color=#b9c2cc>(" + desc + ")</color></size>" +
                    "     \u00b7     WET  " + Mathf.RoundToInt(wet * 100f) + "%  <size=15><color=#b9c2cc>(" + wetDesc + ")</color></size>";
                _thermalText.color = t < 0.30f ? TempCold
                    : t > 0.80f ? TempHot
                    : TempNormal;
            }

            // Stomach / food poisoning.
            if (_sicknessText != null)
            {
                int sickPct = Mathf.RoundToInt(_stats.SicknessNormalized * 100f);
                string word = _nutrition != null ? _nutrition.Descriptor : "Fine";
                int lvl = _nutrition != null ? _nutrition.Level : 0;
                _sicknessText.text = "STOMACH  " + sickPct + "%   <size=15><color=#b6c69a>(" + word + ")</color></size>";
                _sicknessText.color = lvl >= 3 ? SicknessHigh
                    : lvl >= 1 ? SicknessMid
                : SicknessLow;
            }

            // Worn clothing — a header plus one take-off button per body slot.
            if (_clothingText != null)
            {
                bool anyWorn = _clothing != null && _clothing.WornCount > 0;
                float ins = _clothing != null ? _clothing.TotalInsulation(_stats.Wetness) : 0f;
                _clothingText.text = "<color=#c8ccd2>Clothing</color>" + (anyWorn
                    ? "   <size=14><color=#9aa6b2>warmth +" + ins.ToString("0.00") + "   \u00b7   take a slot off, then mend it in your bag</color></size>"
                    : "   <size=14><color=#9aa6b2>nothing worn</color></size>");
            }
            if (_clothingBtns != null)
            {
                for (int i = 0; i < _clothingSlots.Length; i++)
                {
                    ItemDef worn = _clothing != null ? _clothing.GetWorn(_clothingSlots[i]) : null;
                    string slotName = _clothingSlots[i].ToString();
                    if (worn != null)
                    {
                        float c01 = _clothing.GetCondition01(_clothingSlots[i]);
                        string cc = c01 > 0.66f ? "#7fc98a" : c01 > 0.33f ? "#d9c14a" : "#d9694a";
                        _clothingLbls[i].text = "<size=12><color=#9aa6b2>" + slotName + "</color></size>\n" + worn.DisplayName
                            + "  <size=11><color=" + cc + ">" + Mathf.RoundToInt(c01 * 100f) + "%</color></size>";
                        _clothingLbls[i].color = ClothingLabel;
                        _clothingBtns[i].interactable = true;
                    }
                    else
                    {
                        _clothingLbls[i].text = "<size=12><color=#9aa6b2>" + slotName + "</color></size>\n<color=#6a7078>\u2014</color>";
                        _clothingLbls[i].color = ClothingLabelDisabled;
                        _clothingBtns[i].interactable = false;
                    }
                }
            }

            // Active conditions.
            string cond = "";
            if (_stats.Infected)
                cond += "<color=#e05ac0>Zombie infection  " + Mathf.RoundToInt(_stats.InfectionNormalized * 100f) + "%</color>\n";
            if (_wounds.HasInfectedWound)
                cond += _wounds.AntibioticsActive
                    ? "<color=#7fe0a0>Antibiotics fighting the infection\u2026</color>"
                    : "<color=#d06cf0>Wound infection \u2014 take antibiotics</color>";
            _statusText.text = cond.TrimEnd('\n');

            // Wound list.
            IReadOnlyList<Wound> list = _wounds.Wounds;
            _emptyText.enabled = list.Count == 0;
            // Determine how many rows should show actual wound descriptions.
            // If there are more wounds than MaxRows, we reserve the last row for a "…and N more" summary.
            int rowsToShow = list.Count;
            bool showMore = false;
            if (list.Count > MaxRows)
            {
                rowsToShow = MaxRows - 1; // leave space for the summary row
                showMore = true;
            }
            // Populate rows with wound descriptions.
            for (int i = 0; i < _rows.Length; i++)
            {
                if (i < rowsToShow)
                {
                    _rows[i].enabled = true;
                    _rows[i].text = DescribeWound(list[i]);
                }
                else
                {
                    _rows[i].enabled = false;
                }
            }
            // If there are extra wounds, show the summary in the last row.
            if (showMore)
            {
                _rows[MaxRows - 1].enabled = true;
                _rows[MaxRows - 1].text = "<i>\u2026and " + (list.Count - rowsToShow) + " more</i>";
            }


// Treatment buttons: show remaining supplies and enable each only when it would help.
            // Use cached inventory counts instead of scanning per‑frame.
            SetButton(_bandageBtn, _bandageLbl, "Bandage", _bandages, _bandages > 0 && _wounds.CanBandage);
            SetButton(_disinfectBtn, _disinfectLbl, "Disinfect", _disinfectant, _disinfectant > 0 && _wounds.CanDisinfect);
            SetButton(_painkillerBtn, _painkillerLbl, "Painkiller", _painkillers, _painkillers > 0 && _stats.Pain > 2f);
            SetButton(_antibioticBtn, _antibioticLbl, "Antibiotic", _antibiotics, _antibiotics > 0 && _wounds.HasInfectedWound);
            SetButton(_firstAidBtn, _firstAidLbl, "First Aid", _kits, _kits > 0 && _wounds.WoundCount > 0);
            SetButton(_calmBtn, _calmLbl, "Calm", _betablockers, _betablockers > 0 && (_panic != null && _stats.Panic >= CloneZConfig.PanicLevel1));
        }

        private static void SetButton(Button btn, Text label, string name, int count, bool usable)
        {
            if (btn == null) return;
            label.text = name + "  x" + count;
            btn.interactable = usable;
            label.color = usable ? ButtonLabelEnabled : ButtonLabelDisabled;
        }

        private static string DescribeWound(Wound w)
        {
            var parts = new List<string>(4);
            if (w.Infected) parts.Add("<color=#d06cf0>INFECTED</color>");
            if (w.Bleeding && !w.Bandaged) parts.Add("<color=#e0503c>BLEEDING</color>");
            if (w.Bandaged) parts.Add(w.CleanBandage
                ? "<color=#78c86e>Bandaged</color>"
                : "<color=#e0a53c>Dirty bandage</color>");
            else if (!w.Bleeding && !w.Infected) parts.Add("Open");
            if (w.Disinfected && !w.Infected) parts.Add("<color=#5ab0e0>Disinfected</color>");

            if (w.Bandaged && !w.Infected)
                parts.Add("Healing " + Mathf.RoundToInt(w.Healing * 100f) + "%");
            else if (!w.Infected && w.Prone && !w.Disinfected && w.Sepsis > 0.05f)
                parts.Add("<color=#e0a53c>Festering " + Mathf.RoundToInt(w.Sepsis * 100f) + "%</color>");

            return "<b>" + w.Name + "</b>   <size=17>" + string.Join("   \u00b7   ", parts) + "</size>";
        }

        #region UI construction
        private void Build()
        {
            var canvasGo = new GameObject("Health Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 106;   // above the HUD (100) and inventory (105)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform root = canvas.GetComponent<RectTransform>();

            const float panelW = 720f;
            const float panelH = 678f;   // tall enough for two rows of worn slots (body, then gear)

            _panelRoot = new GameObject("HealthPanel");
            var prt = _panelRoot.AddComponent<RectTransform>();
            prt.SetParent(root, false);
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(panelW, panelH);
            _panelRoot.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.06f, 0.95f);

// Add a thin dark trim around the panel for a worn‑metal frame effect
var trimGo = new GameObject("PanelTrim");
var trimRt = trimGo.AddComponent<RectTransform>();
trimRt.SetParent(prt, false);
trimRt.anchorMin = trimRt.anchorMax = new Vector2(0.5f, 0.5f);
trimRt.pivot = new Vector2(0.5f, 0.5f);
trimRt.sizeDelta = new Vector2(panelW + 4f, panelH + 4f);
trimGo.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.05f, 0.9f);

// Add rivet decorations at the panel corners
    Rivet(prt, new Vector2(-panelW / 2 + 8f, panelH / 2 - 8f)); // top‑left
    Rivet(prt, new Vector2( panelW / 2 - 8f, panelH / 2 - 8f)); // top‑right
    Rivet(prt, new Vector2(-panelW / 2 + 8f, -panelH / 2 + 8f)); // bottom‑left
    Rivet(prt, new Vector2( panelW / 2 - 8f, -panelH / 2 + 8f)); // bottom‑right
    // Add rust‑tinted hinge decorations at the same corners
    Hinge(prt, new Vector2(-panelW / 2 + 8f, panelH / 2 - 8f)); // top‑left hinge
    Hinge(prt, new Vector2( panelW / 2 - 8f, panelH / 2 - 8f)); // top‑right hinge
    Hinge(prt, new Vector2(-panelW / 2 + 8f, -panelH / 2 + 8f)); // bottom‑left hinge
    Hinge(prt, new Vector2( panelW / 2 - 8f, -panelH / 2 + 8f)); // bottom‑right hinge
    // Subtle snow‑drift strip at the bottom edge of the panel
    var snow = new GameObject("SnowDrift");
    var snowRt = snow.AddComponent<RectTransform>();
    snowRt.SetParent(prt, false);
    snowRt.anchorMin = snowRt.anchorMax = new Vector2(0.5f, 0.5f);
    snowRt.pivot = new Vector2(0.5f, 0.5f);
    float snowHeight = 6f; // modest height for a light snow layer
    snowRt.anchoredPosition = new Vector2(0f, -panelH / 2 + snowHeight / 2f);
    snowRt.sizeDelta = new Vector2(panelW, snowHeight);
    snow.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            Label(prt, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(panelW, 40f), 30, TextAnchor.UpperCenter, new Color(0.95f, 0.7f, 0.7f))
                .text = "HEALTH";

            _painText = Label(prt, "Pain", new Vector2(0f, 1f), new Vector2(30f, -66f), new Vector2((panelW - 60f) * 0.5f, 28f), 22, TextAnchor.MiddleLeft, new Color(0.8f, 0.85f, 0.8f));
            _painText.text = "PAIN  0%";

            _panicText = Label(prt, "Panic", new Vector2(0f, 1f), new Vector2(30f + (panelW - 60f) * 0.5f, -66f), new Vector2((panelW - 60f) * 0.5f, 28f), 22, TextAnchor.MiddleLeft, new Color(0.78f, 0.78f, 0.9f));
            _panicText.text = "PANIC  0%";

            _thermalText = Label(prt, "Thermal", new Vector2(0f, 1f), new Vector2(30f, -92f), new Vector2(panelW - 60f, 26f), 20, TextAnchor.MiddleLeft, new Color(0.72f, 0.85f, 0.75f));
            _thermalText.text = "TEMP  50%     \u00b7     WET  0%";

            _sicknessText = Label(prt, "Sickness", new Vector2(0f, 1f), new Vector2(30f, -118f), new Vector2(panelW - 60f, 24f), 20, TextAnchor.MiddleLeft, new Color(0.72f, 0.85f, 0.72f));
            _sicknessText.text = "STOMACH  0%";

            _clothingText = Label(prt, "Clothing", new Vector2(0f, 1f), new Vector2(30f, -144f), new Vector2(panelW - 60f, 24f), 19, TextAnchor.MiddleLeft, new Color(0.82f, 0.85f, 0.9f));
            _clothingText.text = "Clothing";

            // A take-off button per body slot (Head / Torso / Legs / Feet / Hands).
            _clothingSlots = PlayerClothing.Slots;
            _clothingBtns = new Button[_clothingSlots.Length];
            _clothingLbls = new Text[_clothingSlots.Length];
            for (int i = 0; i < _clothingSlots.Length; i++)
            {
                ClothingSlot slot = _clothingSlots[i];   // capture per iteration for the click handler
                _clothingBtns[i] = ClothingButton(prt, i, _clothingSlots.Length, () => TakeOffSlot(slot), out _clothingLbls[i]);
            }

            _statusText = Label(prt, "Status", new Vector2(0f, 1f), new Vector2(30f, -274f), new Vector2(panelW - 60f, 44f), 18, TextAnchor.UpperLeft, Color.white);
            _statusText.text = "";

            // Divider under the header.
            var div = new GameObject("Divider");
            var drt = div.AddComponent<RectTransform>();
            drt.SetParent(prt, false);
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 1f); drt.pivot = new Vector2(0.5f, 1f);
            drt.anchoredPosition = new Vector2(0f, -316f);
            drt.sizeDelta = new Vector2(panelW - 48f, 2f);
            div.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            _emptyText = Label(prt, "Empty", new Vector2(0.5f, 1f), new Vector2(0f, -356f), new Vector2(panelW - 60f, 30f), 20, TextAnchor.UpperCenter, new Color(0.6f, 0.85f, 0.6f));
            _emptyText.text = "No injuries.";
            _emptyText.enabled = false;

            _rows = new Text[MaxRows];
            float top = -336f;
            for (int i = 0; i < MaxRows; i++)
            {
                _rows[i] = Label(prt, "Wound" + i, new Vector2(0f, 1f), new Vector2(30f, top - i * 34f), new Vector2(panelW - 60f, 30f), 20, TextAnchor.MiddleLeft, new Color(0.9f, 0.9f, 0.92f));
                _rows[i].enabled = false;
            }

            // Self-treatment button row along the foot of the panel.
            _bandageBtn = TreatmentButton(prt, 0, "Bandage", new Color(0.24f, 0.42f, 0.30f), () => UseFirst("bandage", "rag"), out _bandageLbl);
            _disinfectBtn = TreatmentButton(prt, 1, "Disinfect", new Color(0.22f, 0.40f, 0.44f), () => UseFirst("disinfectant"), out _disinfectLbl);
            _painkillerBtn = TreatmentButton(prt, 2, "Painkiller", new Color(0.26f, 0.34f, 0.48f), () => UseFirst("pills"), out _painkillerLbl);
            _antibioticBtn = TreatmentButton(prt, 3, "Antibiotic", new Color(0.38f, 0.28f, 0.46f), () => UseFirst("antibiotics"), out _antibioticLbl);
            _firstAidBtn = TreatmentButton(prt, 4, "First Aid", new Color(0.5f, 0.26f, 0.26f), () => UseFirst("firstaid"), out _firstAidLbl);
            _calmBtn = TreatmentButton(prt, 5, "Calm", new Color(0.30f, 0.30f, 0.52f), () => UseFirst("betablockers"), out _calmLbl);

            Label(prt, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(panelW, 24f), 16, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.55f))
                .text = "Click a treatment to use it  \u2022  click a clothing slot to take it off  \u2022  H or Esc to close";

            UITheme.AddTopAccent(_panelRoot.GetComponent<RectTransform>(), new Color(0.5f, 0.68f, 0.9f, 0.6f));
            _fader = _panelRoot.AddComponent<UIFader>();
        }

        // One of the five treatment buttons, evenly spaced along the bottom of the panel.
        private Button TreatmentButton(RectTransform parent, int index, string text, Color color, UnityEngine.Events.UnityAction onClick, out Text label)
        {
            const float w = 110f, h = 38f, step = 118f;
            float x = (index - 2.5f) * step;   // centre the six buttons
            Button btn = MakeButton(parent, new Vector2(0.5f, 0f), new Vector2(x, 60f), new Vector2(w, h), text, color, onClick);
            label = btn.GetComponentInChildren<Text>();
            return btn;
        }

        // One take-off button per worn slot: the five body slots across the first row, the gear slots
        // (back, rig, thigh, eyes, mask) across a second row beneath.
        private Button ClothingButton(RectTransform parent, int index, int count, UnityEngine.Events.UnityAction onClick, out Text label)
        {
            const float w = 124f, h = 44f, step = 132f, rowStep = 52f;
            const int perRow = 5;
            int row = index / perRow;
            int col = index % perRow;
            int inRow = Mathf.Min(perRow, count - row * perRow);
            float x = (col - (inRow - 1) * 0.5f) * step;   // centre each row of slots
            Button btn = MakeButton(parent, new Vector2(0.5f, 1f), new Vector2(x, -176f - row * rowStep), new Vector2(w, h), "", new Color(0.24f, 0.28f, 0.34f), onClick);
            label = btn.GetComponentInChildren<Text>();
            label.fontSize = 14;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return btn;
        }

        private Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
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
            cb.disabledColor = new Color(0.18f, 0.18f, 0.2f, 0.9f);
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
            txt.font = _font; txt.fontSize = 15; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = text;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            return btn;
        }

private void Hinge(RectTransform parent, Vector2 anchoredPos)
        {
            var go = new GameObject("Hinge");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(6f, 6f);
            var img = go.AddComponent<Image>();
            // rust‑tinted color (brownish)
            img.color = new Color(0.45f, 0.25f, 0.15f, 1f);
        }
        private void Rivet(RectTransform parent, Vector2 anchoredPos)
        {
            var go = new GameObject("Rivet");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(6f, 6f);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.10f, 0.08f, 1f);
        }
        #endregion
    }
}
