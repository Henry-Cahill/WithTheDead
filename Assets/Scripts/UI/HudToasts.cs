using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Short fading messages at the bottom-centre, above the interaction prompts — e.g. the inventory's
    /// "No room for Hammer — dropped it". The newest sits lowest; a repeat of the newest just refreshes it.
    /// </summary>
    public sealed class HudToasts
    {
        private const int Lines = 3;
        private const float Life = 4f, Fade = 0.8f, Bottom = 218f, Spacing = 26f;
        private static readonly Color ToastColor = new Color(0.97f, 0.92f, 0.76f, 1f);

        private readonly Text[] _texts = new Text[Lines];
        private readonly string[] _messages = new string[Lines];
        private readonly float[] _timers = new float[Lines];

        public HudToasts(RectTransform root)
        {
            for (int i = 0; i < Lines; i++)
            {
                _texts[i] = HudUI.Label(root, "Toast", 18, TextAnchor.LowerCenter, ToastColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, Bottom + i * Spacing), new Vector2(1000f, 24f));
                _texts[i].enabled = false;
            }
        }

        /// <summary>Queues a message (the oldest of three drops off).</summary>
        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (_timers[0] > 0f && message == _messages[0]) { _timers[0] = Life; return; }
            for (int i = Lines - 1; i > 0; i--)
            {
                _messages[i] = _messages[i - 1];
                _timers[i] = _timers[i - 1];
            }
            _messages[0] = message;
            _timers[0] = Life;
            for (int i = 0; i < Lines; i++)
                if (_messages[i] != null) _texts[i].text = _messages[i];
        }

        /// <summary>Counts the messages down and fades them out.</summary>
        public void Tick(float dt)
        {
            for (int i = 0; i < Lines; i++)
            {
                Text t = _texts[i];
                if (_timers[i] <= 0f)
                {
                    if (t.enabled) t.enabled = false;
                    continue;
                }
                _timers[i] -= dt;
                if (!t.enabled) t.enabled = true;
                t.color = HudUI.WithAlpha(ToastColor, Mathf.Clamp01(_timers[i] / Fade));
            }
        }
    }
}
