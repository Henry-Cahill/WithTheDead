using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Captions for what the survivor hears — a radio or TV broadcast (<see cref="Subtitles"/>): the latest two
    /// lines at the bottom-centre above the toasts, the source's name tinted in its colour, each fading out once
    /// its time is up. The newest sits lowest.
    /// </summary>
    public sealed class HudSubtitles
    {
        private const int Lines = 2;
        private const float Fade = 0.8f, Bottom = 300f, Spacing = 46f, Width = 1100f;

        private readonly Text[] _texts = new Text[Lines];
        private readonly float[] _timers = new float[Lines];
        private readonly Subtitles.Line[] _lines = new Subtitles.Line[Lines];

        public HudSubtitles(RectTransform root)
        {
            for (int i = 0; i < Lines; i++)
            {
                _texts[i] = HudUI.Label(root, "Subtitle", 18, TextAnchor.LowerCenter, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, Bottom + i * Spacing), new Vector2(Width, 44f));
                _texts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                _texts[i].enabled = false;
            }
            Subtitles.Shown += OnShown;
        }

        /// <summary>Stops listening (the HUD is going away).</summary>
        public void Dispose() => Subtitles.Shown -= OnShown;

        private void OnShown(Subtitles.Line line)
        {
            for (int i = Lines - 1; i > 0; i--)
            {
                _lines[i] = _lines[i - 1];
                _timers[i] = _timers[i - 1];
            }
            _lines[0] = line;
            _timers[0] = line.Seconds;
            for (int i = 0; i < Lines; i++)
            {
                if (_timers[i] <= 0f) continue;
                Subtitles.Line l = _lines[i];
                string hex = ColorUtility.ToHtmlStringRGB(l.Color);
                _texts[i].text = string.IsNullOrEmpty(l.Source)
                    ? l.Text
                    : "<color=#" + hex + "><b>" + l.Source + ":</b></color>  " + l.Text;
            }
        }

        /// <summary>Counts the captions down and fades them out (hidden while a panel blocks the screen).</summary>
        public void Tick(float dt, bool blocked)
        {
            for (int i = 0; i < Lines; i++)
            {
                Text t = _texts[i];
                if (_timers[i] > 0f) _timers[i] -= dt;
                bool on = !blocked && _timers[i] > 0f;
                if (t.enabled != on) t.enabled = on;
                if (on) t.color = HudUI.WithAlpha(new Color(0.96f, 0.96f, 0.93f, 1f), Mathf.Clamp01(_timers[i] / Fade));
            }
        }
    }
}
