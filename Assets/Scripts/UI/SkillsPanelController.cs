using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A code-built skills screen (toggled with K) listing every perk with its
    /// current level and progress to the next. Read-only — it just visualises the
    /// data in <see cref="PlayerSkills"/>. While open it sets UIState.GameplayBlocked
    /// so clicks/keys don't leak into combat, matching the inventory panel.
    /// </summary>
    public class SkillsPanelController : MonoBehaviour
    {
        private PlayerSkills _skills;
        private Font _font;
        private GameObject _panelRoot;
        private UIFader _fader;
        private bool _open;

        private PlayerSkills.Skill[] _order;
        private Text[] _levelTexts;
        private Text[] _xpTexts;
        private Image[] _progressFills;
        private struct SkillCache { public int level; public float progress; }
        private SkillCache[] _skillCache;

        public bool IsOpen => _open;

        public void Bind(PlayerSkills skills) => _skills = skills;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _order = (PlayerSkills.Skill[])System.Enum.GetValues(typeof(PlayerSkills.Skill));
            Build();
            // Initialise skill cache after UI arrays are allocated in Build()
            _skillCache = new SkillCache[_order.Length];
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.kKey.wasPressedThisFrame) Toggle();
                else if (_open && kb.escapeKey.wasPressedThisFrame) Close();
            }
            if (_open && _skills != null) Refresh();
        }

        private void Toggle()
        {
            if (_open) Close();
            else Open();
        }

        private void Open()
        {
            if (UIState.GameplayBlocked) return;   // inventory or sleep already owns the screen
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

        private void Refresh()
        {
            for (int i = 0; i < _order.Length; i++)
            {
                PlayerSkills.Skill s = _order[i];
                int lvl = _skills.GetLevel(s);
                float prog = _skills.LevelProgress(s);
                if (lvl != _skillCache[i].level)
                {
                    _levelTexts[i].text = "Lv " + lvl;
                    _skillCache[i].level = lvl;
                }
                if (!Mathf.Approximately(prog, _skillCache[i].progress))
                {
                    SetFill(_progressFills[i], prog);
                    _xpTexts[i].text = lvl >= CloneZConfig.SkillMaxLevel
                        ? "MAX"
                        : Mathf.RoundToInt(prog * 100f) + "%";
                    _skillCache[i].progress = prog;
                }
            }
        }

        private static void SetFill(Image fill, float amount)
        {
            Vector3 scale = fill.rectTransform.localScale;
            scale.x = Mathf.Clamp01(amount);
            fill.rectTransform.localScale = scale;
        }

        #region UI construction
        private void Build()
        {
            var canvasGo = new GameObject("Skills Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 104;   // above the HUD (100), below the inventory panel (105)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform root = canvas.GetComponent<RectTransform>();

            const float panelW = 560f;
            float panelH = 150f + _order.Length * 52f;

            _panelRoot = new GameObject("SkillsPanel");
            var prt = _panelRoot.AddComponent<RectTransform>();
            prt.SetParent(root, false);
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(panelW, panelH);
            _panelRoot.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.94f);

            Label(prt, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(panelW, 40f), 30, TextAnchor.UpperCenter, new Color(0.95f, 0.9f, 0.65f))
                .text = "SKILLS";

            int n = _order.Length;
            _levelTexts = new Text[n];
            _xpTexts = new Text[n];
            _progressFills = new Image[n];

            float top = -70f;
            for (int i = 0; i < n; i++)
            {
                float y = top - i * 52f;
                Color accent = AccentFor(_order[i]);

                // Skill name (left).
                Label(prt, "Name" + i, new Vector2(0f, 1f), new Vector2(30f, y), new Vector2(300f, 24f), 20, TextAnchor.MiddleLeft, new Color(0.9f, 0.92f, 0.95f))
                    .text = PlayerSkills.NameOf(_order[i]);

                // Level (right).
                _levelTexts[i] = Label(prt, "Level" + i, new Vector2(1f, 1f), new Vector2(-30f, y), new Vector2(120f, 24f), 20, TextAnchor.MiddleRight, accent);
                _levelTexts[i].text = "Lv 0";

                // Progress percentage (just left of the bar's right edge).
                _xpTexts[i] = Label(prt, "Xp" + i, new Vector2(1f, 1f), new Vector2(-30f, y - 26f), new Vector2(90f, 18f), 13, TextAnchor.MiddleRight, new Color(0.7f, 0.74f, 0.8f));
                _xpTexts[i].text = "0%";

                // Progress bar background.
                var bg = new GameObject("Bar" + i);
                var bgRt = bg.AddComponent<RectTransform>();
                bgRt.SetParent(prt, false);
                bgRt.anchorMin = bgRt.anchorMax = new Vector2(0f, 1f); bgRt.pivot = new Vector2(0f, 1f);
                bgRt.anchoredPosition = new Vector2(30f, y - 28f);
                bgRt.sizeDelta = new Vector2(panelW - 150f, 10f);
                bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

                // Progress fill (scaled from the left, 0..1).
                var fillGo = new GameObject("Fill" + i);
                var fillRt = fillGo.AddComponent<RectTransform>();
                fillRt.SetParent(bgRt, false);
                fillRt.anchorMin = new Vector2(0f, 0f); fillRt.anchorMax = new Vector2(0f, 1f); fillRt.pivot = new Vector2(0f, 0.5f);
                fillRt.sizeDelta = new Vector2(panelW - 154f, -2f);
                fillRt.anchoredPosition = new Vector2(2f, 0f);
                _progressFills[i] = fillGo.AddComponent<Image>();
// tiny cap rectangle on the left side of the progress bar
                 var cap = new GameObject("Cap"+i);
                 var capRt = cap.AddComponent<RectTransform>();
                 capRt.SetParent(bgRt, false);
                 capRt.anchorMin = capRt.anchorMax = new Vector2(0f, 0.5f);
                 capRt.pivot = new Vector2(0f, 0.5f);
                 capRt.anchoredPosition = new Vector2(-2f, 0f);
                 capRt.sizeDelta = new Vector2(4f, 14f);
                 _progressFills[i].color = accent;
                 // separator line under each skill row to suggest a worn metal seam
                 var sep = new GameObject("Sep"+i);
                 var sepRt = sep.AddComponent<RectTransform>();
                 sepRt.SetParent(prt, false);
                 sepRt.anchorMin = sepRt.anchorMax = new Vector2(0f, 1f);
                 sepRt.pivot = new Vector2(0f, 1f);
                 sepRt.anchoredPosition = new Vector2(30f, y - 38f);
                 sepRt.sizeDelta = new Vector2(panelW - 150f, 2f);
                 var sepImg = sep.AddComponent<Image>();
                 sepImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            }

            Label(prt, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(panelW, 24f), 16, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.55f))
                .text = "K  or  Esc  to close";

            UITheme.AddTopAccent(_panelRoot.GetComponent<RectTransform>(), new Color(0.5f, 0.68f, 0.9f, 0.6f));
            // Add subtle rivet-like accents at the top of the panel
            for (int i = 0; i < 8; i++)
            {
                var rivet = new GameObject($"Rivet{i}");
                var rt = rivet.AddComponent<RectTransform>();
                rt.SetParent(_panelRoot.GetComponent<RectTransform>(), false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(-260f + i * 60f, -4f);
                rt.sizeDelta = new Vector2(8f, 8f);
                var img = rivet.AddComponent<Image>();
                img.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
            }
            // Add corner reinforcement plates to suggest worn metal edges
            var corners = new (string name, Vector2 anchor, Vector2 pos)[] {
                ("CornerTL", new Vector2(0f, 1f), new Vector2(3f, -3f)),
                ("CornerTR", new Vector2(1f, 1f), new Vector2(-3f, -3f)),
                ("CornerBL", new Vector2(0f, 0f), new Vector2(3f, 3f)),
                ("CornerBR", new Vector2(1f, 0f), new Vector2(-3f, 3f))
            };
            foreach (var (name, anchor, pos) in corners) {
                var go = new GameObject(name);
                var rt = go.AddComponent<RectTransform>();
                rt.SetParent(_panelRoot.GetComponent<RectTransform>(), false);
                rt.anchorMin = rt.anchorMax = anchor;
                rt.pivot = anchor;
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(6f, 6f);
                var img = go.AddComponent<Image>();
                img.color = new Color(0.25f, 0.25f, 0.27f, 0.9f);
            }
            _fader = _panelRoot.AddComponent<UIFader>();
        }

        // Combat skills read warm, physical/agility skills read cool — a quick visual grouping.
        private static Color AccentFor(PlayerSkills.Skill s)
        {
            switch (s)
            {
                case PlayerSkills.Skill.Blade:
                case PlayerSkills.Skill.Blunt:
                case PlayerSkills.Skill.Aiming:
                case PlayerSkills.Skill.Reloading:
                    return new Color(0.95f, 0.55f, 0.35f);   // combat
                case PlayerSkills.Skill.Fitness:
                case PlayerSkills.Skill.Strength:
                    return new Color(0.95f, 0.8f, 0.3f);      // physical
                case PlayerSkills.Skill.Carpentry:
                    return new Color(0.6f, 0.82f, 0.45f);     // crafting / utility
                case PlayerSkills.Skill.Cooking:
                    return new Color(0.95f, 0.68f, 0.4f);     // cooking / hearth
                case PlayerSkills.Skill.Farming:
                    return new Color(0.5f, 0.78f, 0.4f);      // farming / growth
                default:
                    return new Color(0.45f, 0.8f, 0.95f);     // agility / stealth
            }
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
        #endregion
    }
}
