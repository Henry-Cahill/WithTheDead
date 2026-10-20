using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>Where the moodles sit: down the right edge with labels (isometric, over the shoulder), or as a
    /// compact row of small tiles at the bottom-left (first person).</summary>
    public enum MoodleDock { Column, Row }

    /// <summary>
    /// Project Zomboid-style moodles: a column of small square tiles down the right edge, one per active
    /// condition (toxic gas, bleeding, infected, panic, cold / hot, sick, tired, hungry, thirsty, pain,
    /// wet, heavy load), each tinted by severity with a white glyph and a short label beside it. Only
    /// active moodles show; the column reflows when the set changes and the worst level pulses. Docked as a
    /// row (<see cref="SetDock"/>) the tiles shrink, lose their labels and a moodle that appears or worsens is
    /// named above the row for a few seconds. <see cref="Summary"/> words the worst of them for the bodycam.
    /// </summary>
    public sealed class HudMoodles
    {
        private const float Margin = 20f, Top = 104f, Tile = 40f, Step = 46f;
        private const float RowScale = 0.75f, RowStep = Tile * RowScale + 6f, CaptionSeconds = 3f;

        private enum Kind { Toxic, Bleeding, Infected, Panic, Thermal, Sick, Tired, Hungry, Thirsty, Pain, Wet, Heavy, Count }

        private sealed class Moodle
        {
            public RectTransform Root;
            public Image Background, Glyph;
            public Text Label;
            public int Level;          // 0 = hidden, 1..4 = severity
            public string Text;
            public bool Cold;
            public HudIcon Icon;
            public int ShownLevel = -1;
            public string ShownText;
            public bool ShownCold;
            public HudIcon ShownIcon = HudIcon.White;
            public int ShownPercent = -1;
            public string PercentText;
        }

        private static readonly Color[] Bad =
        {
            new Color(0.80f, 0.70f, 0.30f, 0.92f), new Color(0.86f, 0.52f, 0.20f, 0.92f),
            new Color(0.82f, 0.30f, 0.16f, 0.94f), new Color(0.66f, 0.10f, 0.10f, 0.96f),
        };
        private static readonly Color[] Chill =
        {
            new Color(0.45f, 0.62f, 0.85f, 0.92f), new Color(0.32f, 0.52f, 0.88f, 0.92f),
            new Color(0.22f, 0.40f, 0.85f, 0.94f), new Color(0.16f, 0.28f, 0.78f, 0.96f),
        };

        private readonly HudContext _ctx;
        private readonly Moodle[] _moodles = new Moodle[(int)Kind.Count];
        private readonly RectTransform _column;
        private readonly Text _caption;
        private MoodleDock _dock = MoodleDock.Column;
        private int _layoutSig = -1;
        private float _evalTimer, _captionTimer;
        private string _summary;
        private int _summarySig = -1;
        private readonly string[] _summaryTexts = new string[3];
        private readonly StringBuilder _sb = new StringBuilder(64);

        public HudMoodles(HudContext ctx, RectTransform root)
        {
            _ctx = ctx;
            RectTransform column = HudUI.Rect(root, "Moodles", Vector2.one, Vector2.one, new Vector2(-Margin, -Top), new Vector2(Tile, Tile));
            _column = column;
            for (int i = 0; i < _moodles.Length; i++)
            {
                var m = new Moodle();
                m.Root = HudUI.Rect(column, ((Kind)i).ToString(), Vector2.one, Vector2.one, Vector2.zero, new Vector2(Tile, Tile));
                m.Background = HudUI.Sliced(m.Root, "Tile", HudIcon.PanelFill, Bad[0], Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                Stretch(m.Background.rectTransform);
                Image frame = HudUI.Sliced(m.Root, "Frame", HudIcon.PanelFrame, new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                Stretch(frame.rectTransform);
                m.Glyph = HudUI.Icon(m.Root, "Glyph", HudIcon.Drop, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, Tile * 0.68f);
                m.Label = HudUI.Label(m.Root, "Label", 14, TextAnchor.MiddleRight, HudUI.Ink, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-8f, 0f), new Vector2(200f, 20f));
                m.Root.gameObject.SetActive(false);
                _moodles[i] = m;
            }

            // The row's caption, at its right end: names a moodle that just appeared or worsened.
            _caption = HudUI.Label(column, "Caption", 15, TextAnchor.MiddleLeft, HudUI.Ink, Vector2.zero, Vector2.zero,
                Vector2.zero, new Vector2(320f, Tile * RowScale), FontStyle.Bold);
            _caption.enabled = false;
        }

        /// <summary>Moves the moodles to <paramref name="dock"/>; a row's bottom-left corner sits at
        /// <paramref name="rowPosition"/> (reference pixels from the screen's bottom-left).</summary>
        public void SetDock(MoodleDock dock, Vector2 rowPosition = default)
        {
            _dock = dock;
            bool row = dock == MoodleDock.Row;
            Vector2 corner = row ? Vector2.zero : Vector2.one;
            _column.anchorMin = corner;
            _column.anchorMax = corner;
            _column.pivot = corner;
            _column.anchoredPosition = row ? rowPosition : new Vector2(-Margin, -Top);
            for (int i = 0; i < _moodles.Length; i++)
            {
                Moodle m = _moodles[i];
                m.Root.anchorMin = corner;
                m.Root.anchorMax = corner;
                m.Root.pivot = corner;
                m.Root.localScale = row ? new Vector3(RowScale, RowScale, 1f) : Vector3.one;
                m.Label.enabled = !row;
            }
            if (!row) _caption.enabled = false;
            _layoutSig = -1;   // reflow along the new edge
            _captionTimer = 0f;
            _caption.text = string.Empty;
        }

        /// <summary>The active moodles at <paramref name="minLevel"/> or worse, worst first, as "BLEEDING  ·  COLD"
        /// (at most three); null when there are none. Rebuilt only when the set or its wording changes.</summary>
        public string Summary(int minLevel, int max)
        {
            max = Mathf.Clamp(max, 1, _summaryTexts.Length);
            minLevel = Mathf.Max(1, minLevel);
            int sig = 0, n = 0;
            bool reworded = false;
            for (int level = 4; level >= minLevel && n < max; level--)
                for (int i = 0; i < _moodles.Length && n < max; i++)
                {
                    Moodle m = _moodles[i];
                    if (m.Level != level) continue;
                    sig = sig * 61 + i * 5 + level;
                    if (!ReferenceEquals(_summaryTexts[n], m.Text)) reworded = true;
                    n++;
                }
            if (sig == _summarySig && !reworded) return _summary;
            _summarySig = sig;
            _sb.Length = 0;
            n = 0;
            for (int level = 4; level >= minLevel && n < max; level--)
                for (int i = 0; i < _moodles.Length && n < max; i++)
                {
                    Moodle m = _moodles[i];
                    if (m.Level != level) continue;
                    _summaryTexts[n++] = m.Text;
                    if (_sb.Length > 0) _sb.Append("  \u00b7  ");
                    _sb.Append(m.Text != null ? m.Text.ToUpperInvariant() : ((Kind)i).ToString().ToUpperInvariant());
                }
            for (int k = n; k < _summaryTexts.Length; k++) _summaryTexts[k] = null;
            _summary = n == 0 ? null : _sb.ToString();
            return _summary;
        }

        /// <summary>Re-evaluates every moodle; texts / colours / layout change only when a level does.</summary>
        public void Tick(float dt)
        {
            PlayerStats s = _ctx.Stats;
            if (s == null) return;
            _evalTimer -= Time.unscaledDeltaTime;
            if (_evalTimer <= 0f)
            {
                _evalTimer = 0.1f;   // the underlying stats drift slowly; ten looks a second is plenty
                Evaluate(s);
            }

            int sig = 0;
            for (int i = 0; i < _moodles.Length; i++)
            {
                Moodle m = _moodles[i];
                if (m.Level > 0) sig |= 1 << i;
                if (m.Level != m.ShownLevel || m.Cold != m.ShownCold)
                {
                    // Name a moodle that appears or worsens (not the ones already there when the HUD came up).
                    if (m.ShownLevel >= 0 && m.Level > m.ShownLevel && m.Text != null)
                    {
                        _caption.text = m.Text;
                        _captionTimer = CaptionSeconds;
                    }
                    m.ShownLevel = m.Level;
                    m.ShownCold = m.Cold;
                    if (m.Level > 0) m.Background.color = (m.Cold ? Chill : Bad)[m.Level - 1];
                }
                if (m.Level <= 0) continue;
                if (!ReferenceEquals(m.Text, m.ShownText)) { m.ShownText = m.Text; m.Label.text = m.Text; }
                if (m.Icon != m.ShownIcon) { m.ShownIcon = m.Icon; m.Glyph.sprite = HudIcons.Get(m.Icon); }
                if (m.Level >= 4)
                {
                    // The worst level throbs so it can't be missed.
                    Color c = (m.Cold ? Chill : Bad)[3];
                    m.Background.color = Color.Lerp(c, new Color(1f, 0.25f, 0.2f, 0.98f), HudUI.Pulse(5f, 0f) * (m.Cold ? 0.15f : 0.45f));
                }
            }

            bool row = _dock == MoodleDock.Row;
            if (_captionTimer > 0f) _captionTimer -= Time.unscaledDeltaTime;
            bool caption = row && _captionTimer > 0f;
            if (_caption.enabled != caption) _caption.enabled = caption;
            if (caption) _caption.color = HudUI.WithAlpha(HudUI.Ink, Mathf.Clamp01(_captionTimer / 0.6f));

            if (sig == _layoutSig) return;
            _layoutSig = sig;
            float at = 0f;
            for (int i = 0; i < _moodles.Length; i++)
            {
                Moodle m = _moodles[i];
                bool on = m.Level > 0;
                HudUI.Show(m.Root.gameObject, on);
                if (!on) continue;
                m.Root.anchoredPosition = row ? new Vector2(at, 0f) : new Vector2(0f, -at);
                at += row ? RowStep : Step;
            }
            if (row) _caption.rectTransform.anchoredPosition = new Vector2(at + 4f, 0f);
        }

        private void Evaluate(PlayerStats s)
        {
            // Breathing toxic gas without a working mask: the most urgent tile of all.
            PlayerGear gear = _ctx.Gear;
            Set(Kind.Toxic, gear != null && gear.BreathingGas ? 4 : 0, "Toxic gas!", HudIcon.Gas);

            // Bleeding: one open wound is bad, several are an emergency.
            PlayerWounds wounds = _ctx.Wounds;
            int bleeding = 0;
            if (wounds != null)
                for (int i = 0; i < wounds.Wounds.Count; i++)
                {
                    Wound w = wounds.Wounds[i];
                    if (w.Bleeding && !w.Bandaged) bleeding++;
                }
            Set(Kind.Bleeding, bleeding == 0 ? 0 : bleeding == 1 ? 2 : bleeding == 2 ? 3 : 4, "Bleeding", HudIcon.Drop);

            // Infected: the zombie virus (with its progress, as before), else a septic wound.
            Moodle inf = _moodles[(int)Kind.Infected];
            if (s.Infected)
            {
                float n = s.InfectionNormalized;
                int pct = Mathf.RoundToInt(s.Infection);
                if (pct != inf.ShownPercent) { inf.ShownPercent = pct; inf.PercentText = "Infected  " + HudUI.Int(pct) + "%"; }
                Set(Kind.Infected, n < 0.25f ? 2 : n < 0.6f ? 3 : 4, inf.PercentText, HudIcon.Virus);
            }
            else if (wounds != null && wounds.HasInfectedWound) Set(Kind.Infected, 2, "Wound infection", HudIcon.Virus);
            else Set(Kind.Infected, 0, null, HudIcon.Virus);

            // Panic, from the panic system's own levels and words.
            PlayerPanic panic = _ctx.Panic;
            int pl = panic != null ? panic.Level
                : s.Panic >= CloneZConfig.PanicLevel4 ? 4 : s.Panic >= CloneZConfig.PanicLevel3 ? 3
                : s.Panic >= CloneZConfig.PanicLevel2 ? 2 : s.Panic >= CloneZConfig.PanicLevel1 ? 1 : 0;
            Set(Kind.Panic, pl, panic != null ? panic.Descriptor : "Panic", HudIcon.Scared);

            // Body temperature: blue tiles when cold, warm ones when hot.
            float t = s.Temperature;
            if (t < CloneZConfig.TempComfortLow)
            {
                int l = t < CloneZConfig.TempHypothermiaThreshold + 0.02f ? 4 : t < 0.25f ? 3 : t < 0.33f ? 2 : 1;
                Set(Kind.Thermal, l, l == 4 ? "Freezing" : l == 3 ? "Very cold" : l == 2 ? "Cold" : "Chilly", HudIcon.Snowflake, true);
            }
            else if (t > CloneZConfig.TempComfortHigh)
            {
                int l = t > CloneZConfig.TempHyperthermiaThreshold - 0.01f ? 4 : t > 0.78f ? 3 : t > 0.7f ? 2 : 1;
                Set(Kind.Thermal, l, l == 4 ? "Overheating" : l == 3 ? "Very hot" : l == 2 ? "Hot" : "Warm", HudIcon.Sun);
            }
            else Set(Kind.Thermal, 0, null, HudIcon.Sun);

            // Food poisoning.
            PlayerFoodSickness sick = _ctx.Sickness;
            int sl = sick != null ? sick.Level
                : s.Sickness >= CloneZConfig.SicknessLevel4 ? 4 : s.Sickness >= CloneZConfig.SicknessLevel3 ? 3
                : s.Sickness >= CloneZConfig.SicknessLevel2 ? 2 : s.Sickness >= CloneZConfig.SicknessLevel1 ? 1 : 0;
            Set(Kind.Sick, sl, sick != null ? sick.Descriptor : "Sick", HudIcon.Nausea);

            float f = s.Fatigue;
            int tl = f >= 0.95f ? 4 : f >= CloneZConfig.FatigueExhaustedThreshold ? 3 : f >= 0.75f ? 2 : f >= 0.6f ? 1 : 0;
            Set(Kind.Tired, tl, tl == 4 ? "Falling asleep" : tl == 3 ? "Exhausted" : tl == 2 ? "Tired" : "Drowsy", HudIcon.Moon);

            float h = s.Hunger;
            int hl = h >= 0.92f ? 4 : h >= 0.75f ? 3 : h >= 0.55f ? 2 : h >= 0.35f ? 1 : 0;
            Set(Kind.Hungry, hl, hl == 4 ? "Starving" : hl == 3 ? "Very hungry" : hl == 2 ? "Hungry" : "Peckish", HudIcon.Cutlery);

            float th = s.Thirst;
            int thl = th >= 0.92f ? 4 : th >= 0.75f ? 3 : th >= 0.55f ? 2 : th >= 0.35f ? 1 : 0;
            Set(Kind.Thirsty, thl, thl == 4 ? "Dehydrated" : thl == 3 ? "Parched" : thl == 2 ? "Very thirsty" : "Thirsty", HudIcon.Bottle);

            float p = s.PainNormalized;
            int pa = p >= 0.8f ? 4 : p >= 0.55f ? 3 : p >= 0.3f ? 2 : p >= 0.1f ? 1 : 0;
            Set(Kind.Pain, pa, pa == 4 ? "Agony" : pa == 3 ? "Severe pain" : pa == 2 ? "Pain" : "Minor pain", HudIcon.Burst);

            float wet = s.Wetness;
            int wl = wet >= 0.8f ? 3 : wet >= 0.55f ? 2 : wet >= CloneZConfig.WetnessDampThreshold ? 1 : 0;
            Set(Kind.Wet, wl, wl == 3 ? "Soaked" : wl == 2 ? "Wet" : "Damp", HudIcon.Droplets);

            PlayerInventory inv = _ctx.Inventory;
            float load = inv != null ? inv.Load : 0f;
            int ll = load >= 1.25f ? 4 : load >= 1f ? 3 : load >= 0.8f ? 2 : load > CloneZConfig.EncumbranceStart ? 1 : 0;
            Set(Kind.Heavy, ll, ll >= 3 ? "Overloaded" : "Heavy load", HudIcon.Weight);
        }

        private void Set(Kind kind, int level, string text, HudIcon icon, bool cold = false)
        {
            Moodle m = _moodles[(int)kind];
            m.Level = level;
            m.Cold = cold;
            if (level <= 0) return;
            m.Text = text;
            m.Icon = icon;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
