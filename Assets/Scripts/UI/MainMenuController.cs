using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The title screen shown on boot. Starts an extraction raid (the default), begins a fresh
    /// wave-survival run, continues a saved one (only when a save exists on disk) or quits. Built
    /// entirely from code like the rest of the UI; the <see cref="GameBootstrapper"/> shows it before
    /// any world is built and it tears itself down once a choice is made.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private GameBootstrapper _boot;
        private Font _font;

        /// <summary>Wires the menu to the bootstrapper that will build the world on a choice.</summary>
        public void Bind(GameBootstrapper boot)
        {
            _boot = boot;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        private void Build()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var canvasGo = new GameObject("MainMenu Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;   // above every in-game panel
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            RectTransform root = canvas.GetComponent<RectTransform>();

            // Full-screen opaque backdrop (there's no world camera behind the title screen).
            var bg = new GameObject("Backdrop");
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.SetParent(root, false);
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 1f);

            Label(root, "Title", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1000f, 130f), 96, TextAnchor.MiddleCenter, new Color(0.85f, 0.22f, 0.20f))
                .text = "CLONE-Z";
            Label(root, "Subtitle", new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(1000f, 44f), 26, TextAnchor.MiddleCenter, new Color(0.70f, 0.74f, 0.80f))
                .text = "Knox County  \u00b7  Winter 1993";

            bool hasSave = SaveSystem.HasSave;

            var anchor = new Vector2(0.5f, 0.50f);
            const float step = 80f;
            var raidColor = new Color(0.62f, 0.46f, 0.12f);
            var newColor = new Color(0.30f, 0.42f, 0.24f);
            var contColor = hasSave ? new Color(0.24f, 0.34f, 0.46f) : new Color(0.20f, 0.22f, 0.26f);
            var quitColor = new Color(0.42f, 0.26f, 0.22f);

            MakeButton(root, anchor, Vector2.zero, new Vector2(380f, 64f), "Start Raid  (Extraction)", raidColor, OnStartRaid);
            MakeButton(root, anchor, new Vector2(0f, -step), new Vector2(380f, 64f), "Wave Survival  (New)", newColor, OnNewGame);
            Button cont = MakeButton(root, anchor, new Vector2(0f, -step * 2f), new Vector2(380f, 64f), "Continue Wave Survival", contColor, OnContinue);
            cont.interactable = hasSave;
            MakeButton(root, anchor, new Vector2(0f, -step * 3f), new Vector2(380f, 64f), "Quit", quitColor, OnQuit);

            Label(root, "Hint", new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(1400f, 60f), 16, TextAnchor.MiddleCenter, new Color(0.5f, 0.54f, 0.6f))
                .text = "Raid: arrive under spawn protection, loot the town, reach an exit before dawn \u2014 die and you lose your kit\n" +
                        (hasSave
                            ? "Wave Survival: Continue resumes your last run  \u00b7  New starts fresh (overwrites the save)"
                            : "Wave Survival: hold the starter house against escalating waves (no save found yet)");
        }

        #region Button handlers
        private void OnStartRaid()
        {
            if (_boot != null) _boot.StartRaid();
            Destroy(gameObject);
        }

        private void OnNewGame()
        {
            if (_boot != null) _boot.StartNewGame();
            Destroy(gameObject);
        }

        private void OnContinue()
        {
            if (!SaveSystem.HasSave) return;
            if (_boot != null) _boot.ContinueGame();
            Destroy(gameObject);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion

        #region UI helpers
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
            txt.font = _font; txt.fontSize = 22; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.text = text;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            UITheme.AddShadow(txt);
            return btn;
        }
        #endregion
    }
}
