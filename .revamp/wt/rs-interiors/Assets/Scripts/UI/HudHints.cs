using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The key-hint chips along the bottom-centre (two compact rows of key caps + actions). They fade out
    /// after ~25 s of play to keep the screen clean; F1 brings them back (and hides them again). Hidden
    /// while a modal panel blocks gameplay.
    /// </summary>
    public sealed class HudHints
    {
        private const float AutoHideSeconds = 25f, FadeSpeed = 2.5f, ChipHeight = 18f, RowGap = 4f, ChipGap = 8f;
        private static readonly Color CapColor = new Color(0.20f, 0.22f, 0.28f, 0.92f);
        private static readonly Color KeyColor = new Color(0.96f, 0.95f, 0.82f, 1f);
        private static readonly Color ActionColor = new Color(1f, 1f, 1f, 0.72f);

        private readonly GameObject _bar;
        private readonly CanvasGroup _group;
        private float _playTime;
        private bool _visible = true, _autoHidden;

        public HudHints(RectTransform root)
        {
            RectTransform bar = HudUI.Rect(root, "HintBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f),
                new Vector2(1100f, ChipHeight * 2f + RowGap));
            _bar = bar.gameObject;
            _group = _bar.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var hints = new List<(string key, string action)>
            {
                ("WASD", "Move"), ("LMB", "Attack"), ("RMB", "Shove"), ("R", "Reload"), ("Shift", "Sprint"),
                ("Ctrl", "Sneak"), ("E", "Use"), ("Q", "Actions"), ("I", "Inventory"), ("B", "Build"), ("F", "Farm"),
                ("G", "Water"), ("K", "Skills"), ("H", "Health"), ("T", "Sleep"), ("N", "Night vision"),
                ("L", "Light"),
            };
            if (GameSession.IsExtraction) hints.Add(("M", "Exits"));
            hints.Add(("V", "View"));
            hints.Add(("F1", "Hints"));

            int firstRow = (hints.Count + 1) / 2;
            BuildRow(bar, hints, 0, firstRow, ChipHeight + RowGap);
            BuildRow(bar, hints, firstRow, hints.Count, 0f);
        }

        /// <summary>Auto-hides after a while of play, toggles on F1, hides while gameplay is blocked.</summary>
        public void Tick(float dt, bool blocked)
        {
            HudUI.Show(_bar, !blocked);
            if (blocked) return;

            _playTime += dt;
            if (!_autoHidden && _playTime >= AutoHideSeconds)
            {
                _autoHidden = true;
                _visible = false;
            }
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame)
            {
                _autoHidden = true;      // the player has taken charge of the hints
                _visible = !_visible;
            }

            float target = _visible ? 1f : 0f;
            if (_group.alpha != target)
                _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime * FadeSpeed);
        }

        private static void BuildRow(RectTransform bar, List<(string key, string action)> hints, int from, int to, float y)
        {
            var chips = new List<(RectTransform rt, float w)>(to - from);
            float total = 0f;
            for (int i = from; i < to; i++)
            {
                RectTransform rt = Chip(bar, hints[i].key, hints[i].action, out float w);
                chips.Add((rt, w));
                total += w + ChipGap;
            }
            total -= ChipGap;
            float cursor = -total * 0.5f;
            foreach (var (rt, w) in chips)
            {
                rt.anchoredPosition = new Vector2(cursor, y);
                cursor += w + ChipGap;
            }
        }

        private static RectTransform Chip(RectTransform parent, string key, string action, out float width)
        {
            RectTransform chip = HudUI.Rect(parent, "Chip", new Vector2(0.5f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(10f, ChipHeight));

            Image cap = HudUI.Sliced(chip, "Cap", HudIcon.PanelFill, CapColor, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(20f, ChipHeight));
            Text keyText = HudUI.Label(cap.rectTransform, "Key", 11, TextAnchor.MiddleCenter, KeyColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(40f, ChipHeight), FontStyle.Bold);
            keyText.text = key;
            float keyW = Mathf.Ceil(keyText.preferredWidth) + 10f;
            cap.rectTransform.sizeDelta = new Vector2(keyW, ChipHeight);
            keyText.rectTransform.sizeDelta = new Vector2(keyW, ChipHeight);

            Text actText = HudUI.Label(chip, "Action", 12, TextAnchor.MiddleLeft, ActionColor, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(keyW + 5f, 0f), new Vector2(60f, ChipHeight));
            actText.text = action;
            float actW = Mathf.Ceil(actText.preferredWidth);
            actText.rectTransform.sizeDelta = new Vector2(actW, ChipHeight);

            width = keyW + 5f + actW;
            chip.sizeDelta = new Vector2(width, ChipHeight);
            return chip;
        }
    }
}
