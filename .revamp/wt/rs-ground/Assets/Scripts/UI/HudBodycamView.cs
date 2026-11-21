using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The bodycam view's HUD: the on-screen display of a 1993 camcorder rather than a game HUD. A blinking REC
    /// light and the tape counter top-left, the battery top-right, the date and time stamp bottom-left, the magazine
    /// and a heart-rate readout bottom-right, viewfinder brackets in the corners and faint scanlines over it all. A
    /// "MOTION" alert flashes while something is hunting the survivor and the worst conditions run across the top
    /// in amber. There's no crosshair: aim with the weapon itself.
    /// </summary>
    public sealed class HudBodycamView
    {
        private const float Inset = 76f, BracketInset = 44f, BracketArm = 66f, BracketThick = 3f;
        private static readonly Color OsdColor = new Color(0.96f, 0.96f, 0.92f, 0.95f);
        private static readonly Color RecRed = new Color(1f, 0.16f, 0.12f, 1f);
        private static readonly string[] Months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        private readonly HudContext _ctx;
        private readonly HudWeaponState _weapon = new HudWeaponState();
        private readonly RectTransform _root;
        private readonly RawImage _scanlines;
        private readonly Image _recDot;
        private readonly Text _counter, _date, _time, _mag, _heart, _motion, _warnings;
        private readonly HudGauge _battery;
        private float _shownHeight = -1f;

        private float _tape;
        private int _shownTape = -1, _shownMinute = -1, _shownDay = -1, _shownBpm = -1, _shownVersion = -1;
        private float _bpm = 72f, _bpmTimer;
        private string _shownWarnings;

        public HudBodycamView(HudContext ctx, RectTransform root)
        {
            _ctx = ctx;
            _root = root;

            _scanlines = new GameObject("Scanlines", typeof(RectTransform)).AddComponent<RawImage>();
            _scanlines.rectTransform.SetParent(root, false);
            UIKit.Stretch(_scanlines.rectTransform);
            _scanlines.texture = HudSprites.Scanlines;
            _scanlines.color = new Color(1f, 1f, 1f, 0.11f);
            _scanlines.raycastTarget = false;

            Bracket(root, new Vector2(0f, 1f), 1f, -1f);
            Bracket(root, new Vector2(1f, 1f), -1f, -1f);
            Bracket(root, new Vector2(0f, 0f), 1f, 1f);
            Bracket(root, new Vector2(1f, 0f), -1f, 1f);

            // Top-left: REC and the tape counter.
            var tl = new Vector2(0f, 1f);
            _recDot = HudUI.Icon(root, "RecLight", HudIcon.Disc, RecRed, tl, new Vector2(Inset + 10f, -Inset - 4f), 20f);
            Osd(root, "Rec", 32, TextAnchor.MiddleLeft, tl, new Vector2(0f, 0.5f), new Vector2(Inset + 28f, -Inset - 4f), 140f).text = "REC";
            _counter = Osd(root, "Counter", 24, TextAnchor.MiddleLeft, tl, new Vector2(0f, 0.5f), new Vector2(Inset, -Inset - 40f), 220f);
            Osd(root, "Speed", 20, TextAnchor.MiddleLeft, tl, new Vector2(0f, 0.5f), new Vector2(Inset + 128f, -Inset - 40f), 60f).text = "SP";

            // Top-right: the battery and the focus mode.
            var tr = new Vector2(1f, 1f);
            _battery = new HudGauge(root, "Battery", HudIcon.Battery, HudIcon.BatteryFill, false, true, tr, new Vector2(-Inset - 26f, -Inset - 4f), 52f);
            Osd(root, "Focus", 20, TextAnchor.MiddleRight, tr, new Vector2(1f, 0.5f), new Vector2(-Inset, -Inset - 40f), 120f).text = "AF";

            // Bottom-left: the date stamp.
            var bl = Vector2.zero;
            _time = Osd(root, "Time", 28, TextAnchor.MiddleLeft, bl, new Vector2(0f, 0.5f), new Vector2(Inset, Inset + 2f), 300f);
            _date = Osd(root, "Date", 28, TextAnchor.MiddleLeft, bl, new Vector2(0f, 0.5f), new Vector2(Inset, Inset + 38f), 300f);

            // Bottom-right: the magazine and the heart rate.
            var br = new Vector2(1f, 0f);
            _heart = Osd(root, "Heart", 24, TextAnchor.MiddleRight, br, new Vector2(1f, 0.5f), new Vector2(-Inset, Inset + 2f), 260f);
            _mag = Osd(root, "Magazine", 24, TextAnchor.MiddleRight, br, new Vector2(1f, 0.5f), new Vector2(-Inset, Inset + 36f), 260f);

            // Top-centre, below the compass and the raid banners: alerts.
            var tc = new Vector2(0.5f, 1f);
            _motion = Osd(root, "Motion", 24, TextAnchor.MiddleCenter, tc, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), 600f);
            _motion.text = "\u25cf MOTION";
            _motion.color = RecRed;
            _motion.enabled = false;
            _warnings = Osd(root, "Warnings", 22, TextAnchor.MiddleCenter, tc, new Vector2(0.5f, 0.5f), new Vector2(0f, -162f), 1000f);
            _warnings.color = HudUI.Amber;
            _warnings.text = "";
        }

        /// <summary>A new recording starts each time the bodycam view is entered.</summary>
        public void Wake()
        {
            _tape = 0f;
            _shownTape = -1;
        }

        /// <summary>Refreshes the display. <paramref name="warnings"/> is the moodles' summary, <paramref name="hunted"/>
        /// whether anything is hunting the survivor.</summary>
        public void Tick(float dt, float unscaledDt, string warnings, bool hunted)
        {
            float h = _root.rect.height;
            if (Mathf.Abs(h - _shownHeight) > 0.5f)
            {
                _shownHeight = h;
                _scanlines.uvRect = new Rect(0f, 0f, 1f, Mathf.Max(1f, h / 3f));
            }

            float t = Time.unscaledTime;
            _recDot.enabled = Mathf.Repeat(t, 1.2f) < 0.75f;

            _tape += dt;
            int tape = Mathf.FloorToInt(_tape);
            if (tape != _shownTape)
            {
                _shownTape = tape;
                _counter.text = HudUI.Int(tape / 3600) + ":" + HudUI.Two(tape / 60 % 60) + ":" + HudUI.Two(tape % 60);
            }

            UpdateStamp();
            UpdateBattery();
            UpdateMagazine(dt);
            UpdateHeart(dt, unscaledDt);

            _motion.enabled = hunted && Mathf.Repeat(t, 0.8f) < 0.5f;
            if (!ReferenceEquals(warnings, _shownWarnings))
            {
                _shownWarnings = warnings;
                _warnings.text = warnings ?? "";
            }
        }

        private void UpdateStamp()
        {
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod == null) return;
            float hour = tod.Hour;
            int hh = Mathf.FloorToInt(hour) % 24;
            int mm = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
            int key = hh * 60 + mm;
            if (key != _shownMinute)
            {
                _shownMinute = key;
                int h12 = hh % 12 == 0 ? 12 : hh % 12;
                _time.text = HudUI.Int(h12) + ":" + HudUI.Two(mm) + (hh < 12 ? " AM" : " PM");
            }
            if (tod.Day != _shownDay)
            {
                _shownDay = tod.Day;
                System.DateTime d = tod.Date;
                _date.text = Months[Mathf.Clamp(d.Month - 1, 0, 11)] + " " + HudUI.Int(d.Day) + "  " + d.Year;
            }
        }

        private void UpdateBattery()
        {
            bool has = HudReadouts.Need(_ctx, HudNeed.Battery, out float level, out Color c, out _);
            _battery.Set(has ? level : 1f, has ? c : OsdColor);
        }

        private void UpdateMagazine(float dt)
        {
            _weapon.Poll(_ctx, dt);
            HudWeaponState w = _weapon;
            if (w.Version == _shownVersion) return;
            _shownVersion = w.Version;
            if (w.Gun == null)
            {
                _mag.text = w.Weapon != null ? w.Name.ToUpperInvariant() : "";
                _mag.color = OsdColor;
                return;
            }
            _mag.text = w.Reloading ? "MAG  --/" + HudUI.Int(w.Reserve) : "MAG  " + HudUI.Int(w.Loaded) + "/" + HudUI.Int(w.Reserve);
            _mag.color = w.Loaded == 0 && !w.Reloading ? HudUI.Danger : w.Low ? HudUI.Amber : OsdColor;
        }

        // A plausible pulse: resting, then up with fear, effort and blood loss; eased so it drifts like a real one.
        private void UpdateHeart(float dt, float unscaledDt)
        {
            PlayerStats s = _ctx.Stats;
            if (s == null) return;
            float target = 68f + s.PanicNormalized * 58f + (1f - s.HealthNormalized) * 34f;
            switch (s.Exertion)
            {
                case Exertion.Sprinting: target += 46f; break;
                case Exertion.Running: target += 28f; break;
                case Exertion.Walking: target += 9f; break;
            }
            _bpm = Mathf.Lerp(_bpm, Mathf.Min(target, 196f), 1f - Mathf.Exp(-0.7f * dt));
            _bpmTimer -= unscaledDt;
            if (_bpmTimer > 0f) return;
            _bpmTimer = 0.5f;
            int bpm = s.IsDead ? 0 : Mathf.RoundToInt(_bpm);
            if (bpm == _shownBpm) return;
            _shownBpm = bpm;
            _heart.text = bpm <= 0 ? "HR  ---" : "HR  " + (bpm < 100 ? "0" : "") + HudUI.Int(bpm) + " BPM";
            _heart.color = bpm >= 150 ? HudUI.Danger : bpm >= 120 ? HudUI.Amber : OsdColor;
        }

        private static Text Osd(Transform parent, string name, int size, TextAnchor align, Vector2 anchor, Vector2 pivot, Vector2 pos, float width)
        {
            Text t = HudUI.Label(parent, name, size, align, OsdColor, anchor, pivot, pos, new Vector2(width, size + 8f), FontStyle.Bold);
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }

        // One L-shaped viewfinder bracket in a corner; sx / sy point its arms into the screen.
        private static void Bracket(RectTransform root, Vector2 corner, float sx, float sy)
        {
            var pivot = new Vector2(sx > 0f ? 0f : 1f, sy > 0f ? 0f : 1f);
            var pos = new Vector2(BracketInset * sx, BracketInset * sy);
            Arm(root, corner, pivot, pos, new Vector2(BracketArm, BracketThick));
            Arm(root, corner, pivot, pos, new Vector2(BracketThick, BracketArm));
        }

        private static void Arm(RectTransform root, Vector2 corner, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            RectTransform rt = HudUI.Rect(root, "Bracket", corner, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = HudIcons.Get(HudIcon.White);
            img.color = new Color(OsdColor.r, OsdColor.g, OsdColor.b, 0.8f);
            img.raycastTarget = false;
            UITheme.AddShadow(img);
        }
    }
}
