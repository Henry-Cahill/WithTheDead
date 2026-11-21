using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The interaction overlay: a small label hung over whatever the survivor has in focus (a fixture's
    /// name, a container, an item on the floor) and, on Q, the fixture's numbered action menu beside it —
    /// clickable, or picked with 1–9. Disabled actions are listed greyed out with the reason. Driven by
    /// <see cref="PlayerInteractor"/>; this widget only draws.
    /// </summary>
    public sealed class HudInteraction
    {
        private const float RowHeight = 26f, MenuWidth = 400f, TitleHeight = 30f, Pad = 8f, LabelLift = 0.32f;
        private static readonly Color FocusColor = new Color(1f, 0.96f, 0.82f, 0.95f);
        private static readonly Color TitleColor = new Color(1f, 0.86f, 0.45f, 1f);
        private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color RowHover = new Color(1f, 0.85f, 0.4f, 0.22f);
        private static readonly Color KeyColor = new Color(0.96f, 0.95f, 0.82f, 1f);
        private static readonly Color LabelColor = new Color(0.97f, 0.97f, 0.95f, 1f);
        private static readonly Color DisabledColor = new Color(0.62f, 0.62f, 0.62f, 0.85f);

        private readonly HudContext _ctx;
        private readonly RectTransform _root;
        private readonly Text _focus;
        private readonly RectTransform _menu;
        private readonly Text _title;
        private readonly List<Row> _rows = new List<Row>(10);
        private Camera _cam;
        private int _shownVersion = -1;
        private string _shownFocus;

        private sealed class Row
        {
            public RectTransform Rect;
            public Image Background;
            public Text Key;
            public Text Label;
            public Button Button;
        }

        public HudInteraction(HudContext ctx, RectTransform root, Transform worldLayer)
        {
            _ctx = ctx;
            _root = root;

            _focus = HudUI.Label(worldLayer, "FocusLabel", 16, TextAnchor.MiddleCenter, FocusColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 22f), FontStyle.Bold);
            var outline = _focus.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);
            _focus.enabled = false;

            _menu = HudUI.Panel(root, "ActionMenu", new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero, new Vector2(MenuWidth, 100f), out _);
            _menu.GetComponent<Image>().raycastTarget = true;   // clicks between the rows still land on the menu, not the world
            _title = HudUI.Label(_menu, "Title", 17, TextAnchor.MiddleLeft, TitleColor, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(Pad + 2f, -4f), new Vector2(MenuWidth - Pad * 2f, TitleHeight - 6f), FontStyle.Bold);
            UITheme.AddTopAccent(_menu, new Color(1f, 0.78f, 0.3f, 0.85f), 2f);
            _menu.gameObject.SetActive(false);
        }

        /// <summary>Positions the focus label and keeps the menu in step with the interactor.</summary>
        public void Tick(bool blocked)
        {
            PlayerInteractor pi = _ctx.Interactor;
            if (_cam == null) _cam = Camera.main;

            bool menu = !blocked && pi != null && pi.MenuOpen && pi.MenuTarget != null;
            bool focus = !blocked && !menu && pi != null && pi.HasFocus && !pi.IsBusy && !string.IsNullOrEmpty(pi.FocusName);

            if (focus && Place(_focus.rectTransform, pi.FocusPoint + Vector3.up * LabelLift, Vector2.zero, false))
            {
if (!string.Equals(_shownFocus, pi.FocusName))
                    {
                        _shownFocus = pi.FocusName;
                        _focus.text = _shownFocus;
                    }
            }
            else focus = false;
            if (_focus.enabled != focus) _focus.enabled = focus;

            if (_menu.gameObject.activeSelf != menu) _menu.gameObject.SetActive(menu);
            if (!menu)
            {
                _shownVersion = -1;
                return;
            }
            if (pi.MenuVersion != _shownVersion)
            {
                _shownVersion = pi.MenuVersion;
                Rebuild(pi);
            }
            Place(_menu, pi.MenuTarget.InteractPoint, new Vector2(36f, 40f), true);
        }

        // Puts a rect at a world point's place on screen (plus an offset), clamped on screen when asked.
        private bool Place(RectTransform rt, Vector3 world, Vector2 offset, bool clamp)
        {
            if (_cam == null) return false;
            Vector3 sp = _cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, sp, null, out Vector2 local)) return false;
            local += offset;
            if (clamp)
            {
                Rect r = _root.rect;
                Vector2 size = rt.sizeDelta;
                local.x = Mathf.Clamp(local.x, r.xMin + 8f, r.xMax - size.x - 8f);
                local.y = Mathf.Clamp(local.y, r.yMin + size.y + 140f, r.yMax - 8f);   // keep clear of the prompts below
            }
            rt.anchoredPosition = local;
            return true;
        }

        private void Rebuild(PlayerInteractor pi)
        {
            IReadOnlyList<InteractAction> actions = pi.MenuActions;
            _title.text = pi.MenuTarget.DisplayName;
            while (_rows.Count < actions.Count) _rows.Add(MakeRow(_rows.Count));
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool used = i < actions.Count;
                row.Rect.gameObject.SetActive(used);
                if (!used) continue;
                InteractAction a = actions[i];
                row.Key.text = i < 9 ? (i + 1).ToString() : "";
                row.Label.text = a.Enabled
                    ? a.Label + (a.IsTimed ? "\u2026" : "")
                    : a.Label + "  <size=12><i>\u2014 " + (a.DisabledReason ?? "not now") + "</i></size>";
                row.Label.color = a.Enabled ? LabelColor : DisabledColor;
                row.Key.color = a.Enabled ? KeyColor : DisabledColor;
                row.Button.interactable = true;   // a click on a disabled entry explains why
            }
            _menu.sizeDelta = new Vector2(MenuWidth, TitleHeight + actions.Count * RowHeight + Pad);
        }

        private Row MakeRow(int index)
        {
            RectTransform rt = HudUI.Rect(_menu, "Action" + index, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(Pad * 0.5f, -TitleHeight - index * RowHeight), new Vector2(MenuWidth - Pad, RowHeight - 2f));
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = Color.white;
            bg.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = RowColor;
            colors.highlightedColor = RowHover;
            colors.pressedColor = new Color(1f, 0.8f, 0.3f, 0.35f);
            colors.selectedColor = RowColor;
            colors.disabledColor = RowColor;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            int captured = index;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                // Don't leave the row selected: a later Space / Enter would "submit" it again.
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                PlayerInteractor pi = _ctx.Interactor;
                if (pi != null) pi.ChooseMenu(captured);
            });

            Image cap = HudUI.Sliced(rt, "Cap", HudIcon.PanelFill, new Color(0.20f, 0.22f, 0.28f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 0f), new Vector2(22f, 18f));
            Text key = HudUI.Label(cap.rectTransform, "Key", 12, TextAnchor.MiddleCenter, KeyColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(22f, 18f), FontStyle.Bold);
            Text label = HudUI.Label(rt, "Label", 15, TextAnchor.MiddleLeft, LabelColor, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(34f, 0f), new Vector2(MenuWidth - Pad - 40f, RowHeight - 2f));
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return new Row { Rect = rt, Background = bg, Key = key, Label = label, Button = button };
        }
    }
}
