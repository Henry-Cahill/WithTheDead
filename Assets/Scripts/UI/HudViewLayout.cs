using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>How much a HUD cluster matters, which decides which HUD styles keep it on screen.</summary>
    public enum HudTier
    {
        /// <summary>Vitals, the weapon, moodles, combat feedback: the full and minimal styles.</summary>
        Essential,
        /// <summary>The watch, the mode box, the key hints: the full style only.</summary>
        Secondary,
    }

    /// <summary>
    /// One cluster of HUD widgets that a camera view can show, nudge or scale: a full-screen container pivoted on the
    /// corner its widgets hang from, faded through a <see cref="CanvasGroup"/>. The widgets inside keep their own
    /// layout; a view only offsets the cluster (reference pixels) and scales it about that corner.
    /// </summary>
    public sealed class HudGroup
    {
        internal const int ViewCount = 4;

        public readonly RectTransform Rect;
        public readonly CanvasGroup Canvas;
        public readonly HudTier Tier;

        /// <summary>Kept on screen whatever the HUD style (the exits panel while it's open, say), in the views that show it.</summary>
        public bool ForceVisible;

        private readonly bool[] _show = new bool[ViewCount];
        private readonly Vector2[] _offset = new Vector2[ViewCount];
        private readonly float[] _scale = { 1f, 1f, 1f, 1f };

        internal HudGroup(Transform parent, string name, Vector2 pivot, HudTier tier)
        {
            Rect = HudUI.Stretch(parent, name);
            Rect.pivot = pivot;
            Canvas = Rect.gameObject.AddComponent<CanvasGroup>();
            Canvas.alpha = 0f;
            Canvas.interactable = false;
            Canvas.blocksRaycasts = false;
            Tier = tier;
        }

        /// <summary>Shows the cluster in <paramref name="view"/>, shifted by <paramref name="offset"/> and scaled about its corner.</summary>
        public HudGroup In(CameraView view, Vector2 offset = default, float scale = 1f)
        {
            int i = Index(view);
            _show[i] = true;
            _offset[i] = offset;
            _scale[i] = scale;
            return this;
        }

        /// <summary>True when the cluster is part of <paramref name="view"/>'s HUD.</summary>
        public bool ShownIn(CameraView view) => _show[Index(view)];

        /// <summary>True while the cluster is on screen at all (worth ticking).</summary>
        public bool Live => Rect.gameObject.activeSelf;

        internal static int Index(CameraView view) => Mathf.Clamp((int)view, 0, ViewCount - 1);

        internal void Place(CameraView view)
        {
            int i = Index(view);
            Rect.anchoredPosition = _offset[i];
            float s = _scale[i];
            Rect.localScale = new Vector3(s, s, 1f);
        }

        internal void Fade(float target, float step)
        {
            bool on = target > 0f;
            GameObject go = Rect.gameObject;
            if (on && !go.activeSelf) go.SetActive(true);
            if (Canvas.blocksRaycasts != on) Canvas.blocksRaycasts = on;
            if (Canvas.interactable != on) Canvas.interactable = on;
            float a = Mathf.MoveTowards(Canvas.alpha, target, step);
            if (a != Canvas.alpha) Canvas.alpha = a;
            if (!on && a <= 0f && go.activeSelf) go.SetActive(false);
        }
    }

    /// <summary>
    /// The per-view HUD: which clusters each camera view shows and where, filtered by the HUD style setting
    /// (full / minimal / diegetic / hardcore). Changing view fades the HUD out, lays it out for the new view and
    /// fades it back in; changing the style just fades clusters in or out. Runs on unscaled time, so the pause
    /// menu can preview each view's HUD with the world frozen.
    /// </summary>
    public sealed class HudViewLayout
    {
        private const float FadeOutSeconds = 0.09f, FadeInSeconds = 0.22f;

        private readonly List<HudGroup> _groups = new List<HudGroup>();
        private bool _started;

        /// <summary>The view the HUD is currently laid out for (it trails the camera by the fade-out).</summary>
        public CameraView View { get; private set; } = CameraView.Isometric;

        /// <summary>Raised once the HUD has been laid out for a view (at the bottom of the fade, and on the first frame).</summary>
        public event Action<CameraView> ViewApplied;

        /// <summary>Adds a cluster: a full-screen container under <paramref name="parent"/> pivoted on <paramref name="pivot"/>.
        /// It stays active (but transparent) until the first <see cref="Tick"/>, so widgets can measure text as they build.</summary>
        public HudGroup Add(Transform parent, string name, Vector2 pivot, HudTier tier)
        {
            var g = new HudGroup(parent, name, pivot, tier);
            _groups.Add(g);
            return g;
        }

        /// <summary>True when the style keeps a cluster of <paramref name="tier"/> on screen.</summary>
        public static bool StyleShows(HudMode style, HudTier tier)
            => style == HudMode.Full || (style == HudMode.Minimal && tier == HudTier.Essential);

        /// <summary>Follows the camera view and the HUD style.</summary>
        public void Tick(CameraView view, HudMode style, float unscaledDt)
        {
            if (!_started)
            {
                _started = true;
                Apply(view);
                for (int i = 0; i < _groups.Count; i++) _groups[i].Fade(Target(_groups[i], style), float.PositiveInfinity);
                return;
            }

            if (view != View)
            {
                // Fade everything out, then lay the HUD out for the new view at the bottom of the fade.
                float outStep = unscaledDt / FadeOutSeconds;
                bool clear = true;
                for (int i = 0; i < _groups.Count; i++)
                {
                    HudGroup g = _groups[i];
                    g.Fade(0f, outStep);
                    if (g.Live) clear = false;
                }
                if (!clear) return;
                Apply(view);
            }

            float inStep = unscaledDt / FadeInSeconds;
            for (int i = 0; i < _groups.Count; i++) _groups[i].Fade(Target(_groups[i], style), inStep);
        }

        private float Target(HudGroup g, HudMode style)
            => g.ShownIn(View) && (g.ForceVisible || StyleShows(style, g.Tier)) ? 1f : 0f;

        private void Apply(CameraView view)
        {
            View = view;
            for (int i = 0; i < _groups.Count; i++) _groups[i].Place(view);
            ViewApplied?.Invoke(view);
        }
    }
}
