using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The in-game pause menu (Esc). Freezes the world (<c>Time.timeScale = 0</c>) and
    /// offers Resume, Save Game and Save &amp; Quit to the title screen. Esc only opens it
    /// when no other panel is up, so it never collides with the inventory / skills /
    /// health screens (which also close on Esc). Built from code and created by the
    /// <see cref="GameBootstrapper"/> alongside the HUD.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        private Font _font;
        private UIFader _fader;
        private Text _savedFlash;
        private float _savedTimer;
        private bool _open;
        private bool _prevBlocked;   // was some OTHER panel blocking gameplay last frame?

        public bool IsOpen => _open;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        private void OnDisable()
        {
            // Never leave the world frozen if this object is torn down while paused.
            if (_open)
            {
                _open = false;
                UIState.GameplayBlocked = false;
                Time.timeScale = 1f;
            }
        }

        private void Update()
        {
            bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

            if (_open)
            {
                if (esc) Resume();
            }
            else
            {
                bool playing = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
                bool free = !UIState.GameplayBlocked && !UIState.BuildMode;
                // Require a clear previous frame too, so the Esc that closes another panel
                // doesn't also pop the pause menu open on the same press.
                if (esc && playing && free && !_prevBlocked) Open();
            }

            _prevBlocked = UIState.GameplayBlocked && !_open;

            if (_savedTimer > 0f)
            {
                _savedTimer -= Time.unscaledDeltaTime;
                if (_savedFlash != null)
                {
                    Color c = _savedFlash.color;
                    c.a = Mathf.Clamp01(_savedTimer);
                    _savedFlash.color = c;
                    if (_savedTimer <= 0f) _savedFlash.enabled = false;
                }
            }
        }

        private void Open()
        {
            _open = true;
            UIState.GameplayBlocked = true;
            Time.timeScale = 0f;              // freeze the world (zombies, clock, physics)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (_fader != null) _fader.Show();
        }

        private void Resume()
        {
            _open = false;
            UIState.GameplayBlocked = false;
            Time.timeScale = 1f;
            if (_fader != null) _fader.Hide();
        }

        private void OnSave()
        {
            GameManager.Instance?.QuickSave();
            if (_savedFlash != null)
            {
                _savedFlash.enabled = true;
                Color c = _savedFlash.color; c.a = 1f; _savedFlash.color = c;
            }
            _savedTimer = 1.6f;
        }

        private void OnQuitToMenu()
        {
            GameManager.Instance?.QuickSave();
            Time.timeScale = 1f;
            UIState.GameplayBlocked = false;
            GameManager.Instance?.Restart();   // reloads the scene \u2192 the bootstrapper shows the title screen
        }

        #region UI construction
        private void Build()
        {
            var canvasGo = new GameObject("PauseMenu Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 190;   // above the HUD/panels, below the title screen (200)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform root = canvas.GetComponent<RectTransform>();

            var panel = new GameObject("PausePanel");
            var prt = panel.AddComponent<RectTransform>();
            prt.SetParent(root, false);
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            panel.AddComponent<CanvasGroup>();
            _fader = panel.AddComponent<UIFader>();   // starts hidden (Awake sets alpha 0)

            // Dim the frozen world behind the menu.
            var dim = new GameObject("Dim");
            var dimRt = dim.AddComponent<RectTransform>();
            dimRt.SetParent(prt, false);
            dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero; dimRt.offsetMax = Vector2.zero;
            dim.AddComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.78f);

            Label(prt, "Title", new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(600f, 80f), 60, TextAnchor.MiddleCenter, new Color(0.90f, 0.90f, 0.92f))
                .text = "PAUSED";

            var anchor = new Vector2(0.5f, 0.5f);
            const float step = 78f;
            MakeButton(prt, anchor, Vector2.zero, new Vector2(360f, 60f), "Resume", new Color(0.28f, 0.40f, 0.24f), Resume);
            MakeButton(prt, anchor, new Vector2(0f, -step), new Vector2(360f, 60f), "Save Game", new Color(0.24f, 0.34f, 0.46f), OnSave);
            MakeButton(prt, anchor, new Vector2(0f, -step * 2f), new Vector2(360f, 60f), "Save & Quit to Menu", new Color(0.42f, 0.26f, 0.22f), OnQuitToMenu);

            _savedFlash = Label(prt, "Saved", new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(500f, 30f), 20, TextAnchor.MiddleCenter, new Color(0.6f, 0.9f, 0.5f));
            _savedFlash.text = "Game saved";
            _savedFlash.enabled = false;

            Label(prt, "Hint", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(900f, 26f), 15, TextAnchor.MiddleCenter, new Color(0.5f, 0.54f, 0.6f))
                .text = "Esc to resume  \u00b7  F5 quick-saves anytime";
        }

        private Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var txt = go.AddComponent<Text>();
            txt.font = _font; txt.fontSize = fontSize; txt.alignment = align; txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            UITheme.AddShadow(txt);
            return txt;
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
            txt.font = _font; txt.fontSize = 20; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = text;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            UITheme.AddShadow(txt);
            return btn;
        }
        #endregion
    }
}
