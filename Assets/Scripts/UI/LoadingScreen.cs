using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The card shown while a world is built: where you're going and when (the winter night over a small Kentucky town
    /// behind it, the snow coming down), a bar that fills as the town goes up, and a tip. It outlives the scene reload,
    /// so a transition reads as one card from the click to the first frame of the new world, and fades out once the
    /// bootstrapper is done. <see cref="Run"/> defers work until the card has actually been drawn, since a world is
    /// built on the main thread and nothing redraws while it is.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        private static LoadingScreen s_Instance;

        private static readonly string[] Tips =
        {
            "The Kettle's front door opens straight onto Bardstown. Step outside and the raid begins.",
            "You lose what you carried if you die \u2014 never what you learned, never your secure pouch.",
            "Every street is where it really is. Learn the town like a local.",
            "Dawn ends the raid. Be through an exit before the sky goes grey.",
            "Noise carries on a still winter night. Sneak (Ctrl) past what you can't fight.",
            "The dead leave a freshly arrived survivor be for a minute. Use it to get your bearings.",
            "Press M in a raid to see which exits are open, and what each one asks.",
            "At a landing-zone exit, fire a flare and hold out until the helicopter comes.",
            "The bar keeps your kit and your stash between raids. Pay the rent before it comes due.",
            "Cold kills slowly. Dress for the night before you step out the door.",
        };

        private static readonly Vector2 _fullAnchor = new Vector2(1f, 1f);
        private CanvasGroup _group;
        private Text _title, _subtitle, _stage, _tip;
        private RectTransform _fill;
        private Image _fillImage;
        private float _progress = -1f, _shown, _tipAt;
        private int _tipIndex;
        private bool _hiding;
        private bool _breathing; // true when progress unknown, animates bar

        /// <summary>True while the card is up (and not on its way out).</summary>
        public static bool Showing => s_Instance != null && !s_Instance._hiding;

        /// <summary>Puts the card up (or changes what an open one says): <paramref name="title"/> large,
        /// <paramref name="subtitle"/> under it. The bar shows no progress until <see cref="SetProgress"/>.</summary>
        public static void Show(string title, string subtitle)
        {
            if (s_Instance == null)
            {
                var go = new GameObject("[LoadingScreen]");
                DontDestroyOnLoad(go);
                s_Instance = go.AddComponent<LoadingScreen>();
                s_Instance.Build();
            }
            LoadingScreen s = s_Instance;
            s._hiding = false;
            s._group.alpha = 1f;
            s._title.text = title ?? "";
            s._subtitle.text = subtitle ?? "";
            s._shown = Time.unscaledTime;
            SetProgress(-1f, "");
        }

        /// <summary>How far the build is (0 to 1; below 0 for "no idea yet") and what's going up now.</summary>
        public static void SetProgress(float progress01, string stage)
        {
            LoadingScreen s = s_Instance;
            if (s == null) return;
            s._progress = progress01;
            bool known = progress01 >= 0f;
            float p = Mathf.Clamp01(progress01);
            s._breathing = !known;
            if (known)
                s._fill.anchorMax = new Vector2(p, 1f);
            else
                s._fill.anchorMax = _fullAnchor;
            s._stage.text = string.IsNullOrEmpty(stage) ? "" : known ? stage + "   " + Mathf.RoundToInt(p * 100f) + "%" : stage;
        }

        /// <summary>Fades the card out (and away).</summary>
        public static void Hide()
        {
            LoadingScreen s = s_Instance;
            if (s == null || s._hiding) return;
            s._hiding = true;
            s.StartCoroutine(s.FadeOut());
        }

        /// <summary>Runs <paramref name="action"/> once the card has been drawn (straight away when there's no card).</summary>
        public static void Run(System.Action action)
        {
            if (action == null) return;
            if (s_Instance == null) { action(); return; }
            s_Instance.StartCoroutine(s_Instance.After(action));
        }

        /// <summary>A line naming the date of the raids' first night, for the card's subtitle ("Friday, December 17, 1993").</summary>
        public static string NightOf()
            => new System.DateTime(TimeOfDay.Year, CloneZConfig.RaidStartMonth, CloneZConfig.RaidStartDay)
                .ToString("dddd, MMMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);

        private IEnumerator After(System.Action action)
        {
            // Two frames: the first ends with the card on screen, the second runs the work (which may block for a while).
            yield return null;
            yield return null;
            action();
        }

        private IEnumerator FadeOut()
        {
            // A card that only flashed up stays a moment, so the cut reads as a fade rather than a flicker.
            while (Time.unscaledTime - _shown < 0.35f) yield return null;
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / 0.6f);
                yield return null;
            }
            if (s_Instance == this) s_Instance = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas("Loading Canvas", 5000, transform);
            var root = (RectTransform)canvas.transform;
            _group = canvas.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = true;
            Image black = UIKit.Panel(root, "Black", Color.black);
            UIKit.Stretch(black.rectTransform);
            WinterBackdrop.Build(root, 140, true, 17);

            _title = UIKit.Label(root, "", 68, new Color(0.96f, 0.92f, 0.84f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_title.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 90f));
            _subtitle = UIKit.Label(root, "", 24, new Color(0.70f, 0.74f, 0.80f), TextAnchor.MiddleCenter);
            UIKit.Place(_subtitle.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(1600f, 36f));

            RectTransform bar = UIKit.Place(UIKit.Rect("Bar", root), new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 6f));
            Image back = bar.gameObject.AddComponent<Image>();
            back.color = new Color(1f, 1f, 1f, 0.12f);
            back.raycastTarget = false;
            _fillImage = UIKit.Panel(bar, "Fill", UIKit.Accent);
            _fillImage.raycastTarget = false;
            _fill = _fillImage.rectTransform;
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = new Vector2(0f, 1f);
            _fill.offsetMin = _fill.offsetMax = Vector2.zero;
            _stage = UIKit.Label(root, "", 18, new Color(0.80f, 0.82f, 0.86f), TextAnchor.MiddleCenter);
            UIKit.Place(_stage.rectTransform, new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(900f, 28f));

            _tip = UIKit.Label(root, "", 18, new Color(0.62f, 0.66f, 0.72f), TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.Place(_tip.rectTransform, new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 30f));
            _tipIndex = Random.Range(0, Tips.Length);
            _tip.text = Tips[_tipIndex];
            _tipAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (Time.unscaledTime - _tipAt > 6f)
            {
                _tipAt = Time.unscaledTime;
                _tipIndex = (_tipIndex + 1) % Tips.Length;
                _tip.text = Tips[_tipIndex];
            }
            // With no progress to show, the bar breathes, so a card that's waiting doesn't look stuck.
            if (_breathing)
            {
                float a = 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f);
                _fillImage.color = new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, a);
            }
            else
                _fillImage.color = UIKit.Accent;
        }
    }
}
