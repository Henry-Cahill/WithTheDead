using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The top-right row, right-aligned: the gas-mask "Filter 72%" panel (only with a mask on), a dim
    /// activity icon (lights up while building / farming / searching), the digital watch (teal
    /// seven-segment time + AM/PM, outdoor temperature in °F and the date) and a bell that glows red
    /// while any zombie is hunting the survivor.
    /// </summary>
    public sealed class HudWatchRow
    {
        private const float Margin = 20f, Top = 16f, RowHeight = 66f, BellSize = 26f, ActivitySize = 30f;
        private const float WatchWidth = 146f, FilterWidth = 98f;
        private static readonly Color Ghost = new Color(0.247f, 0.824f, 0.78f, 0.075f);
        private static readonly Color TealDim = new Color(0.247f, 0.824f, 0.78f, 0.85f);
        private static readonly Color BellIdle = new Color(1f, 1f, 1f, 0.32f);
        private static readonly Color BellAlarm = new Color(1f, 0.22f, 0.18f, 1f);
        private static readonly Color Toxic = new Color(0.78f, 0.95f, 0.25f, 1f);

        private readonly HudContext _ctx;
        private readonly HudSevenSegment[] _digits = new HudSevenSegment[4];
        private Text _ampm, _info;
        private GameObject _filterRoot;
        private Image _filterFrame;
        private Text _filterNumber, _filterPercent;
        private Image _activity, _bell, _bellGlow;

        private int _shownMinute = -1, _shownTemp10 = int.MinValue, _shownDay = -1, _shownFilter = -1;
        private int _activityState = -1;
        private bool _filterLow, _filterGas;
        private float _scanTimer;
        private bool _hunted, _bellShown;

        public HudWatchRow(HudContext ctx, RectTransform root)
        {
            _ctx = ctx;
            float centreY = -Top - RowHeight * 0.5f;

            // Bell (far right).
            _bellGlow = HudUI.Icon(root, "BellGlow", HudIcon.Glow, Color.clear, new Vector2(1f, 1f),
                new Vector2(-Margin - BellSize * 0.5f, centreY), BellSize * 2.4f, false);
            _bellGlow.enabled = false;
            _bell = HudUI.Icon(root, "Bell", HudIcon.Bell, BellIdle, new Vector2(1f, 1f),
                new Vector2(-Margin - BellSize * 0.5f, centreY), BellSize);

            // Watch.
            float watchRight = -Margin - BellSize - 10f;
            RectTransform watch = HudUI.Panel(root, "Watch", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(watchRight, -Top), new Vector2(WatchWidth, RowHeight), out _);
            watch.GetComponent<Image>().color = new Color(0.012f, 0.04f, 0.042f, 0.82f);   // a faintly teal LCD face
            BuildWatchFace(watch);

            // Activity icon.
            float activityRight = watchRight - WatchWidth - 10f;
            _activity = HudUI.Icon(root, "Activity", HudIcon.Hammer, HudUI.WithAlpha(HudUI.Ink, 0.16f), new Vector2(1f, 1f),
                new Vector2(activityRight - ActivitySize * 0.5f, centreY), ActivitySize);

            // Gas-mask filter.
            float filterRight = activityRight - ActivitySize - 10f;
            RectTransform filter = HudUI.Panel(root, "Filter", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(filterRight, -Top), new Vector2(FilterWidth, RowHeight), out _filterFrame);
            HudUI.Label(filter, "Caption", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.88f, 0.88f, 0.75f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -7f), new Vector2(80f, 16f)).text = "Filter";
            _filterNumber = HudUI.Label(filter, "Value", 30, TextAnchor.LowerRight, HudUI.Ink,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(68f, 5f), new Vector2(60f, 34f), FontStyle.Bold);
            _filterPercent = HudUI.Label(filter, "Percent", 15, TextAnchor.LowerLeft, HudUI.Ink,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 9f), new Vector2(20f, 18f), FontStyle.Bold);
            _filterPercent.text = "%";
            _filterRoot = filter.gameObject;
            _filterRoot.SetActive(false);
        }

        private void BuildWatchFace(RectTransform watch)
        {
            const float digitW = 17f, digitH = 30f, thick = 4f, space = 5f, colonGap = 12f, left = 11f, top = -9f;
            float x = left;
            for (int i = 0; i < 4; i++)
            {
                _digits[i] = new HudSevenSegment(watch, new Vector2(x, top), digitW, digitH, thick, HudUI.Teal, Ghost);
                x += digitW + space;
                if (i == 1)
                {
                    // The colon: two small squares centred in a wider gap between the hours and the minutes.
                    float gapStart = x - space;
                    float cx = gapStart + colonGap * 0.5f;
                    for (int k = 0; k < 2; k++)
                    {
                        RectTransform dot = HudUI.Rect(watch, "Colon", new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                            new Vector2(cx, top - digitH * (k == 0 ? 0.3f : 0.7f)), new Vector2(thick, thick));
                        var img = dot.gameObject.AddComponent<Image>();
                        img.sprite = HudIcons.Get(HudIcon.White);
                        img.color = HudUI.Teal;
                        img.raycastTarget = false;
                    }
                    x = gapStart + colonGap;
                }
            }
            _ampm = HudUI.Label(watch, "AmPm", 13, TextAnchor.UpperLeft, HudUI.Teal, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x + 1f, top - 1f), new Vector2(30f, 16f), FontStyle.Bold);
            _ampm.text = "AM";
            _info = HudUI.Label(watch, "TempDate", 15, TextAnchor.LowerLeft, TealDim, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(left, 6f), new Vector2(WatchWidth - left * 2f, 18f));
            _info.text = "";
        }

        /// <summary>Refreshes the row: the watch only when the minute / temperature / date changes.</summary>
        public void Tick(float dt)
        {
            UpdateWatch();
            UpdateFilter();
            UpdateActivity();
            UpdateBell();
        }

        private void UpdateWatch()
        {
            TimeOfDay tod = TimeOfDay.Instance;
            int day = 0;
            if (tod != null)
            {
                float hour = tod.Hour;
                int h = Mathf.FloorToInt(hour) % 24;
                int m = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
                int key = h * 60 + m;
                if (key != _shownMinute)
                {
                    _shownMinute = key;
                    int h12 = h % 12 == 0 ? 12 : h % 12;
                    _digits[0].Set(h12 / 10);
                    _digits[1].Set(h12 % 10);
                    _digits[2].Set(m / 10);
                    _digits[3].Set(m % 10);
                    _ampm.text = h < 12 ? "AM" : "PM";
                }
                day = tod.Day;
            }

            int temp10 = Mathf.RoundToInt(Climate.AirTemperatureF * 10f);
            if (temp10 != _shownTemp10 || day != _shownDay)
            {
                _shownTemp10 = temp10;
                _shownDay = day;
                string temp = (temp10 / 10f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "\u00b0F";
                _info.text = tod != null ? temp + "  " + tod.DateText : temp;
            }
        }

        private void UpdateFilter()
        {
            PlayerGear gear = _ctx.Gear;
            bool has = gear != null && gear.HasGasMask;
            HudUI.Show(_filterRoot, has);
            if (!has) return;

            int pct = Mathf.Clamp(Mathf.RoundToInt(gear.FilterCharge01 * 100f), 0, 100);
            bool low = pct < 20;
            bool gas = gear.GasConcentration > 0.01f;
            if (pct != _shownFilter) { _shownFilter = pct; _filterNumber.text = HudUI.Int(pct); }

            if (gas)
            {
                // Breathing through the filter: pulse the readout and flash the frame toxic green.
                float p = HudUI.Pulse(6f, 0.35f);
                Color c = HudUI.WithAlpha(low ? HudUI.Danger : HudUI.Ink, p);
                _filterNumber.color = c;
                _filterPercent.color = c;
                _filterFrame.color = Color.Lerp(HudUI.FrameColor, Toxic, p * 0.8f);
            }
            else if (low != _filterLow || gas != _filterGas)
            {
                Color c = low ? HudUI.Danger : HudUI.Ink;
                _filterNumber.color = c;
                _filterPercent.color = c;
                _filterFrame.color = HudUI.FrameColor;
            }
            _filterLow = low;
            _filterGas = gas;
        }

        private void UpdateActivity()
        {
            bool building = (_ctx.Builder != null && _ctx.Builder.IsBuilding) || (_ctx.Farming != null && _ctx.Farming.IsBusy);
            bool searching = _ctx.Interactor != null && _ctx.Interactor.IsSearching;
            int state = searching ? 2 : building ? 1 : 0;
            if (state != _activityState)
            {
                _activityState = state;
                _activity.sprite = HudIcons.Get(searching ? HudIcon.Magnifier : HudIcon.Hammer);
                if (state == 0) _activity.color = HudUI.WithAlpha(HudUI.Ink, 0.16f);
            }
            if (state != 0) _activity.color = HudUI.WithAlpha(HudUI.Ink, HudUI.Pulse(5f, 0.6f));
        }

        private void UpdateBell()
        {
            _scanTimer -= Time.unscaledDeltaTime;
            if (_scanTimer <= 0f)
            {
                // A cheap periodic scan (never per frame): is anything chasing or attacking the survivor?
                _scanTimer = 0.5f;
                _hunted = false;
                PlayerStats stats = _ctx.Stats;
                if (stats != null && !stats.IsDead)
                {
                    ZombieAI[] zombies = Object.FindObjectsByType<ZombieAI>();
                    for (int i = 0; i < zombies.Length; i++)
                    {
                        ZombieAI z = zombies[i];
                        if (z != null && z.isActiveAndEnabled && z.IsHunting) { _hunted = true; break; }
                    }
                }
            }

            if (_hunted)
            {
                float p = HudUI.Pulse(7f, 0.45f);
                _bell.color = Color.Lerp(HudUI.WithAlpha(BellAlarm, 0.75f), BellAlarm, p);
                _bellGlow.color = HudUI.WithAlpha(BellAlarm, 0.18f + 0.3f * p);
                if (!_bellShown) { _bellShown = true; _bellGlow.enabled = true; }
            }
            else if (_bellShown)
            {
                _bellShown = false;
                _bellGlow.enabled = false;
                _bell.color = BellIdle;
            }
        }
    }
}
