using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Bottom-right: the mode box (amber "EXTRACTION MODE", or the wave and kill count in wave survival)
    /// with its two tiny buttons, the status icon row beneath it — battery | water, food, body
    /// temperature | health with a wound badge — each filling to its level and turning amber / red
    /// (pulsing when critical), and the info panel the buttons (or M) toggle: the raid's exits with
    /// distance, bearing and state plus the time left until dawn, or the run's stats in wave survival.
    /// </summary>
    public sealed class HudStatusRow
    {
        private const float Margin = 20f, IconSize = 30f, IconBottom = 16f, BoxBottom = 56f, BoxHeight = 30f, BoxWidth = 252f;
        private const float PanelWidth = 300f, RowHeight = 21f;
        private const int MaxRows = 10;
        private static readonly Color Cold = new Color(0.45f, 0.72f, 1f, 1f);
        private static readonly Color Hot = new Color(1f, 0.5f, 0.3f, 1f);
        private static readonly Color Absent = new Color(1f, 1f, 1f, 0.22f);

        private sealed class StatusIcon
        {
            public Image Outline, Fill;
            public float Min, Max;
            public bool TwoLayer;
        }

        private sealed class InfoRow
        {
            public RectTransform Root;
            public Image Arrow;
            public Text Name, Value, State;
            public string ShownName;
            public int ShownValue = int.MinValue, ShownState = -1;
        }

        private readonly HudContext _ctx;
        private readonly bool _raid;
        private StatusIcon _battery, _water, _food, _temp, _health;
        private Image _badge;
        private Text _modeLabel;
        private int _shownWave = -1, _shownKills = -1;

        private RectTransform _panel;
        private Text _panelTitle, _panelRight;
        private readonly List<InfoRow> _rows = new List<InfoRow>();
        private int _rowsBuilt = -1;
        private float _panelTimer;
        private int _shownDawn = int.MinValue;
        private Camera _cam;

        /// <summary>True while the exits / run info panel is open.</summary>
        public bool InfoOpen { get; private set; }

        /// <summary>Builds the icon row into <paramref name="iconsRoot"/> and the mode box with its info panel into
        /// <paramref name="boxRoot"/> (separate so a view can hide the icons yet keep the box); both are full-screen rects.</summary>
        public HudStatusRow(HudContext ctx, RectTransform iconsRoot, RectTransform boxRoot)
        {
            _ctx = ctx;
            _raid = GameSession.IsExtraction;
            BuildIcons(iconsRoot);
            BuildBox(boxRoot);
            BuildPanel(boxRoot);
        }

        #region Build
        private void BuildIcons(RectTransform root)
        {
            // Right to left: health | temperature, food, water | battery.
            float x = -Margin;
            _health = Status(root, "Health", ref x, HudIcon.Cross, HudIcon.CrossFill, false, false);
            _badge = HudUI.Icon(root, "WoundBadge", HudIcon.Disc, HudUI.Danger, new Vector2(1f, 0f),
                new Vector2(x + IconSize + 10f - 3f, IconBottom + IconSize - 3f), 10f);
            _badge.enabled = false;
            Separator(root, ref x);
            _temp = Status(root, "Temperature", ref x, HudIcon.Thermometer, HudIcon.ThermometerFill, false, false);
            x -= 2f;
            _food = Status(root, "Food", ref x, HudIcon.Cutlery, HudIcon.Cutlery, true, false);
            x -= 2f;
            _water = Status(root, "Water", ref x, HudIcon.Bottle, HudIcon.BottleFill, false, false);
            Separator(root, ref x);
            _battery = Status(root, "Battery", ref x, HudIcon.Battery, HudIcon.BatteryFill, false, true);
        }

        // Places one status icon with its right edge at x and moves x past it (plus a gap).
        private StatusIcon Status(RectTransform root, string name, ref float x, HudIcon outline, HudIcon fill, bool twoLayer, bool horizontal)
        {
            var s = new StatusIcon { TwoLayer = twoLayer };
            var anchor = new Vector2(1f, 0f);
            var pos = new Vector2(x - IconSize * 0.5f, IconBottom + IconSize * 0.5f);
            if (twoLayer)
            {
                s.Outline = HudUI.Icon(root, name, outline, HudUI.WithAlpha(HudUI.Ink, 0.28f), anchor, pos, IconSize);
                s.Fill = HudUI.Icon(root, name + "Level", outline, HudUI.Ink, anchor, pos, IconSize);
            }
            else
            {
                s.Fill = HudUI.Icon(root, name + "Level", fill, HudUI.WithAlpha(HudUI.Ink, 0.5f), anchor, pos, IconSize, false);
                s.Outline = HudUI.Icon(root, name, outline, HudUI.Ink, anchor, pos, IconSize);
            }
            HudUI.MakeFilled(s.Fill, horizontal);
            HudIconArt.FillRange(twoLayer ? outline : fill, out s.Min, out s.Max);
            x -= IconSize + 10f;
            return s;
        }

        private static void Separator(RectTransform root, ref float x)
        {
            x -= 2f;
            RectTransform rt = HudUI.Rect(root, "Separator", new Vector2(1f, 0f), new Vector2(1f, 0.5f),
                new Vector2(x, IconBottom + IconSize * 0.5f), new Vector2(2f, 22f));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = HudIcons.Get(HudIcon.White);
            img.color = new Color(1f, 1f, 1f, 0.5f);
            img.raycastTarget = false;
            UITheme.AddShadow(img);
            x -= 2f + 12f;
        }

        private void BuildBox(RectTransform root)
        {
            RectTransform box = HudUI.Panel(root, "ModeBox", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-Margin, BoxBottom), new Vector2(BoxWidth, BoxHeight), out _);
            _modeLabel = HudUI.Label(box, "Mode", 14, TextAnchor.MiddleLeft, HudUI.Amber, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(10f, 0f), new Vector2(170f, 20f), FontStyle.Bold);
            _modeLabel.text = _raid ? "EXTRACTION MODE" : "WAVE 0  \u00b7  KILLS 0";
            TinyButton(box, "+", new Vector2(-6f, 0f), 20f);
            TinyButton(box, _raid ? "Exits" : "Stats", new Vector2(-30f, 0f), 44f);
        }

        /// <summary>Creates a tiny button inside the mode box.</summary>
        // Helper that builds a small button with the given label, position and width.
        private void TinyButton(RectTransform parent, string label, Vector2 pos, float width)
        {
            Image bg = HudUI.Sliced(parent, "Button " + label, HudIcon.PanelFill, new Color(0.34f, 0.36f, 0.40f, 0.95f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), pos, new Vector2(width, 18f));
            bg.raycastTarget = true;
            Image frame = HudUI.Sliced(bg.rectTransform, "Frame", HudIcon.PanelFrame, new Color(1f, 1f, 1f, 0.22f),
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            frame.rectTransform.anchorMax = Vector2.one;
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.62f, 0.62f, 0.62f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.AddListener(OnButton);
            Text text = HudUI.Label(bg.rectTransform, "Text", 11, TextAnchor.MiddleCenter, HudUI.Ink, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 18f), label == "+" ? FontStyle.Bold : FontStyle.Normal);
            text.text = label;
        }

        private void OnButton()
        {
            ToggleInfo();
            // Don't leave the button selected, or Space / Enter would keep clicking it.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void BuildPanel(RectTransform root)
        {
            _panel = HudUI.Panel(root, "InfoPanel", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-Margin, BoxBottom + BoxHeight + 6f), new Vector2(PanelWidth, 60f), out _);
            UITheme.AddTopAccent(_panel, HudUI.WithAlpha(HudUI.Amber, 0.7f), 2f);
            _panelTitle = HudUI.Label(_panel, "Title", 13, TextAnchor.UpperLeft, HudUI.Amber, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, -8f), new Vector2(140f, 18f), FontStyle.Bold);
            _panelTitle.text = _raid ? "EXITS" : "THIS RUN";
            _panelRight = HudUI.Label(_panel, "Right", 13, TextAnchor.UpperRight, HudUI.Ink, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10f, -8f), new Vector2(160f, 18f));
            _panelRight.text = "";
            _panel.gameObject.SetActive(false);
        }

        private void EnsureRows(int count)
        {
            if (count == _rowsBuilt) return;
            _rowsBuilt = count;
            while (_rows.Count < count)
            {
                int i = _rows.Count;
                var row = new InfoRow();
                row.Root = HudUI.Rect(_panel, "Row", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, -30f - i * RowHeight), new Vector2(PanelWidth, RowHeight));
                row.Arrow = HudUI.Icon(row.Root, "Bearing", HudIcon.Arrow, HudUI.Amber, new Vector2(0f, 0.5f), new Vector2(18f, 0f), 14f);
                row.Name = HudUI.Label(row.Root, "Name", 13, TextAnchor.MiddleLeft, HudUI.Ink, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(32f, 0f), new Vector2(150f, RowHeight));
                row.Value = HudUI.Label(row.Root, "Value", 13, TextAnchor.MiddleRight, HudUI.Ink, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-64f, 0f), new Vector2(80f, RowHeight));
                row.State = HudUI.Label(row.Root, "State", 12, TextAnchor.MiddleRight, HudUI.Good, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-10f, 0f), new Vector2(60f, RowHeight), FontStyle.Bold);
                _rows.Add(row);
            }
            for (int i = 0; i < _rows.Count; i++) HudUI.Show(_rows[i].Root.gameObject, i < count);
            _panel.sizeDelta = new Vector2(PanelWidth, 34f + Mathf.Max(1, count) * RowHeight + 6f);
        }
        #endregion

        /// <summary>Opens or closes the exits / run info panel.</summary>
        public void ToggleInfo()
        {
            InfoOpen = !InfoOpen;
            _panel.gameObject.SetActive(InfoOpen);
            _panelTimer = 0f;
            _shownDawn = int.MinValue;
        }

        /// <summary>Refreshes the box, the icon row and (when open) the info panel.</summary>
        public void Tick(float dt)
        {
            UpdateModeLabel();
            UpdateIcons();
            if (InfoOpen) UpdatePanel(dt);
        }

        private void UpdateModeLabel()
        {
            if (_raid) return;
            GameManager gm = GameManager.Instance;
            if (gm == null || (gm.Wave == _shownWave && gm.ZombieKills == _shownKills)) return;
            _shownWave = gm.Wave;
            _shownKills = gm.ZombieKills;
            _modeLabel.text = "WAVE " + HudUI.Int(_shownWave) + "  \u00b7  KILLS " + HudUI.Int(_shownKills);
        }

        private void UpdateIcons()
        {
            PlayerStats stats = _ctx.Stats;
            PlayerGear gear = _ctx.Gear;

            float battery = gear != null ? gear.BatteryCharge01 : -1f;
            if (battery < 0f) SetStatus(_battery, 0f, Absent);
            else SetStatus(_battery, battery, Severity(1f - battery, 0.7f, 0.9f));

            if (stats == null) return;
            SetStatus(_water, 1f - stats.Thirst, Severity(stats.Thirst, 0.6f, 0.85f));
            SetStatus(_food, 1f - stats.Hunger, Severity(stats.Hunger, 0.6f, 0.85f));

            float t = stats.Temperature;
            float tempLevel;
            Color tempColor;
            GetTempFillAndColor(t, out tempLevel, out tempColor);
            SetStatus(_temp, tempLevel, tempColor);

            float hp = stats.HealthNormalized;
            SetStatus(_health, hp, Severity(1f - hp, 0.5f, 0.75f));

            PlayerWounds wounds = _ctx.Wounds;
            bool hurt = stats.Infected || (wounds != null && wounds.WoundCount > 0);
            if (_badge.enabled != hurt) _badge.enabled = hurt;
            if (hurt)
            {
                bool bleeding = wounds != null && wounds.IsBleeding;
                _badge.color = bleeding ? HudUI.WithAlpha(HudUI.Danger, HudUI.Pulse(6f, 0.45f)) : HudUI.Danger;
            }
        }

        // White while fine, amber past the warning level, pulsing red past the critical one (bad01: 1 = worst).
        private static Color Severity(float bad01, float warnAt, float criticalAt)
        {
            if (bad01 >= criticalAt) return HudUI.WithAlpha(HudUI.Danger, HudUI.Pulse(6f, 0.35f));
            if (bad01 >= warnAt) return HudUI.Amber;
            return HudUI.Ink;
        }

        private static void SetStatus(StatusIcon s, float level01, Color color)
        {
            // Quantised so a slowly draining stat doesn't rebuild the mesh every frame.
            float level = Mathf.Round(Mathf.Clamp01(level01) * 64f) / 64f;
            s.Fill.fillAmount = Mathf.Lerp(s.Min, s.Max, level);
            if (s.TwoLayer)
            {
                s.Outline.color = HudUI.WithAlpha(color, color.a * 0.28f);
                s.Fill.color = color;
            }
            else
            {
                s.Outline.color = color;
                s.Fill.color = HudUI.WithAlpha(color, color.a * 0.5f);
            }
        }

        // Helper for the temperature HUD element – normalises the fill amount and picks the appropriate colour.
        private static void GetTempFillAndColor(float temperature, out float level01, out Color color)
        {
            // Normalise temperature within the comfort range.
            level01 = Mathf.InverseLerp(CloneZConfig.TempComfortLow, CloneZConfig.TempComfortHigh, temperature);
            // Determine colour exactly as the old code did.
            color = HudUI.Ink;
            if (temperature < CloneZConfig.TempComfortLow)
            {
                // Cold side – pulse when very close to hypothermia.
                bool pulse = temperature < CloneZConfig.TempHypothermiaThreshold + 0.02f;
                color = pulse ? HudUI.WithAlpha(Cold, HudUI.Pulse(5f, 0.35f)) : Cold;
            }
            else if (temperature > CloneZConfig.TempComfortHigh)
            {
                // Hot side – pulse when very close to hyperthermia.
                bool pulse = temperature > CloneZConfig.TempHyperthermiaThreshold - 0.01f;
                color = pulse ? HudUI.WithAlpha(Hot, HudUI.Pulse(5f, 0.35f)) : Hot;
            }
        }

        #region Info panel
        private void UpdatePanel(float dt)
        {
            if (_raid) UpdateExits(dt);
            else UpdateRunStats(dt);
        }

        private void UpdateExits(float dt)
        {
            ExtractionManager em = ExtractionManager.Instance;
            int count = em != null ? Mathf.Min(em.Exits.Count, MaxRows) : 0;
            EnsureRows(Mathf.Max(count, 1));

            if (count == 0)
            {
                InfoRow none = _rows[0];
                if (none.ShownState != 99)
                {
                    none.ShownState = 99;
                    none.ShownName = null;
                    none.Name.text = "No exits known";
                    none.Value.text = "";
                    none.State.text = "";
                    none.Arrow.enabled = false;
                }
                return;
            }

            if (_cam == null) _cam = Camera.main;
            Vector3 player = _ctx.Stats != null ? _ctx.Stats.transform.position : Vector3.zero;
            float camYaw = _cam != null ? _cam.transform.eulerAngles.y : 0f;

            _panelTimer -= dt;
            bool refresh = _panelTimer <= 0f;
            if (refresh) _panelTimer = 0.25f;

            for (int i = 0; i < count; i++)
            {
                RaidExit exit = em.Exits[i];
                InfoRow row = _rows[i];
                if (exit == null) continue;
                Vector3 to = exit.Position - player;
                to.y = 0f;

                // The arrow points at the exit relative to where the camera faces (screen up = camera forward).
                float bearing = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                row.Arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, camYaw - bearing);
                if (!row.Arrow.enabled) row.Arrow.enabled = true;

                if (!refresh && row.ShownState >= 0) continue;
                if (!ReferenceEquals(row.ShownName, exit.Name))
                {
                    row.ShownName = exit.Name;
                    row.Name.text = string.IsNullOrEmpty(exit.Name) ? "Exit" : exit.Name;
                }
                int metres = Mathf.RoundToInt(to.magnitude);
                if (metres != row.ShownValue)
                {
                    row.ShownValue = metres;
                    row.Value.text = HudUI.Int(metres) + " m";
                }
                int state = exit.Open ? 1 : 0;
                if (state != row.ShownState)
                {
                    row.ShownState = state;
                    row.State.text = exit.Open ? "OPEN" : "CLOSED";
                    row.State.color = exit.Open ? HudUI.Good : HudUI.Danger;
                    row.Arrow.color = exit.Open ? HudUI.Amber : HudUI.WithAlpha(HudUI.Ink, 0.45f);
                }
            }

            int dawn = Mathf.CeilToInt(Mathf.Max(0f, em.RaidSecondsRemaining));
            if (dawn != _shownDawn)
            {
                _shownDawn = dawn;
                _panelRight.text = "Dawn in " + HudUI.Clock(dawn);
                _panelRight.color = dawn < 300 ? HudUI.Amber : HudUI.Ink;
            }
        }

        private void UpdateRunStats(float dt)
        {
            EnsureRows(3);
            _panelTimer -= dt;
            if (_panelTimer > 0f) return;
            _panelTimer = 0.5f;

            GameManager gm = GameManager.Instance;
            int survived = gm != null ? Mathf.FloorToInt(gm.TimeSurvived) : 0;
            SetRunRow(0, "Survived", survived, null, HudUI.Ink);

            WeatherSystem weather = WeatherSystem.Instance;
            SetRunRow(1, "Weather", int.MinValue, weather != null ? weather.Label : "\u2014", HudUI.Ink);

            WaterMains water = WaterMains.Instance;
            bool on = water == null || water.IsOn;
            SetRunRow(2, "Water", int.MinValue, on ? "ON" : "SHUT OFF", on ? new Color(0.55f, 0.8f, 0.95f) : new Color(0.92f, 0.42f, 0.35f));
        }

        // A stat row: a label on the left and either a duration (seconds) or a fixed string on the right.
        private void SetRunRow(int i, string label, int seconds, string value, Color valueColor)
        {
            InfoRow row = _rows[i];
            if (row.Arrow.enabled) row.Arrow.enabled = false;
            if (!ReferenceEquals(row.ShownName, label))
            {
                row.ShownName = label;
                row.Name.text = label;
                row.Value.text = "";
                row.State.rectTransform.sizeDelta = new Vector2(150f, RowHeight);
            }
            if (value == null)
            {
                if (seconds != row.ShownValue) { row.ShownValue = seconds; row.State.text = HudUI.Clock(seconds); }
            }
            else
            {
                row.State.text = value;
            }
            row.State.color = valueColor;
        }
        #endregion
    }
}
