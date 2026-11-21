using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Bottom-left: a running-man pictogram that mirrors how the survivor is moving (standing,
    /// walking, sprinting, sneaking — with a two-frame stride) and a long thin white stamina bar
    /// (<see cref="PlayerStats.Endurance"/>) that turns amber, then pulsing red, as it runs low.
    /// </summary>
    public sealed class HudStamina
    {
        private const float Margin = 18f, IconSize = 36f, BarWidth = 300f, BarHeight = 5f;
        private static readonly Color Track = new Color(1f, 1f, 1f, 0.16f);

        private readonly HudContext _ctx;
        private readonly Image _figure, _fill;
        private HudIcon _shownPose = HudIcon.White;
        private float _stride, _shown = -1f;

        public HudStamina(HudContext ctx, RectTransform root)
        {
            _ctx = ctx;
            _figure = HudUI.Icon(root, "Movement", HudIcon.Stand, HudUI.Ink, Vector2.zero,
                new Vector2(Margin + IconSize * 0.5f, 14f + IconSize * 0.5f), IconSize);
            _fill = HudUI.Bar(root, "Stamina", Vector2.zero, new Vector2(0f, 0.5f),
                new Vector2(Margin + IconSize + 8f, 14f + IconSize * 0.5f), new Vector2(BarWidth, BarHeight), Track, HudUI.Ink, out RectTransform bar);
            UITheme.AddShadow(bar.GetComponent<Image>());
        }

        /// <summary>Picks the pose / stride frame and eases the bar toward the survivor's endurance.</summary>
        public void Tick(float dt)
        {
            PlayerStats stats = _ctx.Stats;
            if (stats == null) return;

            PlayerController pc = _ctx.Controller;
            bool moving = stats.Exertion != Exertion.Idle && (pc == null || pc.CurrentSpeed > 0.05f);
            bool sneaking = pc != null && pc.IsSneaking;
            bool running = stats.Exertion == Exertion.Running || stats.Exertion == Exertion.Sprinting;

            HudIcon pose;
            if (stats.IsDead || !moving)
            {
                _stride = 0f;
                pose = sneaking && !stats.IsDead ? HudIcon.Sneak1 : HudIcon.Stand;
            }
            else
            {
                // Two frames per step; quicker cadence when running, slower when creeping.
                _stride += dt * (running ? 6.5f : sneaking ? 2.8f : 4f);
                bool alt = ((int)_stride & 1) == 1;
                if (sneaking) pose = alt ? HudIcon.Sneak2 : HudIcon.Sneak1;
                else if (running) pose = alt ? HudIcon.Run2 : HudIcon.Run1;
                else pose = alt ? HudIcon.Walk2 : HudIcon.Walk1;
            }
            if (pose != _shownPose)
            {
                _shownPose = pose;
                _figure.sprite = HudIcons.Get(pose);
            }

            float e = Mathf.Clamp01(stats.Endurance);
            _shown = _shown < 0f ? e : Mathf.MoveTowards(_shown, e, dt * 2f);
            _fill.fillAmount = Mathf.Round(_shown * BarWidth) / BarWidth;

            Color c;
            if (e < 0.1f) c = HudUI.WithAlpha(HudUI.Danger, HudUI.Pulse(7f, 0.35f));
            else if (e < CloneZConfig.EnduranceDanger) c = HudUI.Danger;
            else if (e < CloneZConfig.EnduranceWarning) c = HudUI.Amber;
            else c = HudUI.Ink;
            _fill.color = c;
            _figure.color = e < CloneZConfig.EnduranceDanger ? Color.Lerp(HudUI.Ink, c, 0.6f) : HudUI.Ink;
        }
    }
}
