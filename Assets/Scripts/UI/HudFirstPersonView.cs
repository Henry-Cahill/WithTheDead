using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The first-person view's HUD: as little as possible between the eyes and the world. Bottom-left, two hairline
    /// bars (health with its damage trail, stamina) and only the supplies that are running low; the moodles sit
    /// above them as a row of small tiles (see <see cref="HudMoodles"/>). Bottom-right, a compact magazine count,
    /// a thin magazine bar and the weapon's name. Each corner fades back once nothing has changed for a few seconds
    /// and everything is fine, and comes straight back on a hit, a shot, a reload or a low reading.
    /// </summary>
    public sealed class HudFirstPersonView
    {
        private const float Margin = 32f, Bottom = 30f, IconX = 42f, BarX = 58f, BarWidth = 230f;
        private const float CalmSeconds = 3f, QuietAlpha = 0.28f, WeaponQuietAlpha = 0.42f, FadeSpeed = 2.5f;
        private static readonly Color Track = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color LightOn = new Color(1f, 0.87f, 0.38f, 1f);
        private static readonly Color NvgOn = new Color(0.45f, 1f, 0.45f, 1f);
        private static readonly HudNeed[] Needs = { HudNeed.Water, HudNeed.Food, HudNeed.Warmth, HudNeed.Battery };

        private readonly HudContext _ctx;
        private readonly HudWeaponState _weapon = new HudWeaponState();

        private readonly CanvasGroup _vitalsGroup, _weaponGroup;
        private readonly Image _cross, _figure;
        private readonly HudTrailBar _health, _stamina;
        private readonly HudGauge[] _needs = new HudGauge[Needs.Length];
        private readonly int[] _needSeverity = new int[Needs.Length];
        private int _needsSig = -1;
        private float _calm, _lastHealth = -1f;
        private HudIcon _shownPose = HudIcon.White;
        private float _stride;

        private readonly Text _ammo, _name;
        private readonly Image _magBar;
        private readonly GameObject _magRoot;
        private readonly Image _light, _nvg;
        private int _shownVersion = -1, _lightState = -1, _nvgState = -1;

        public HudFirstPersonView(HudContext ctx, RectTransform root)
        {
            _ctx = ctx;
            var bl = Vector2.zero;
            var br = new Vector2(1f, 0f);

            // Bottom-left: the vitals.
            RectTransform vitals = HudUI.Stretch(root, "Vitals");
            _vitalsGroup = vitals.gameObject.AddComponent<CanvasGroup>();
            _vitalsGroup.blocksRaycasts = false;
            HudReadouts.CornerShade(vitals, bl, new Vector2(760f, 300f), 0.34f);
            float healthY = Bottom + 24f, staminaY = Bottom + 8f, needsY = Bottom + 50f;
            _cross = HudUI.Icon(vitals, "Health", HudIcon.Cross, HudUI.Ink, bl, new Vector2(IconX, healthY), 18f);
            _health = new HudTrailBar(vitals, "HealthBar", bl, new Vector2(0f, 0.5f), new Vector2(BarX, healthY), new Vector2(BarWidth, 4f), 4,
                Track, new Color(1f, 0.86f, 0.78f, 0.7f));
            _figure = HudUI.Icon(vitals, "Movement", HudIcon.Stand, HudUI.Ink, bl, new Vector2(IconX, staminaY), 18f);
            _stamina = new HudTrailBar(vitals, "StaminaBar", bl, new Vector2(0f, 0.5f), new Vector2(BarX, staminaY), new Vector2(BarWidth, 3f), 0,
                Track, new Color(1f, 1f, 1f, 0.3f));
            for (int i = 0; i < Needs.Length; i++)
            {
                _needs[i] = HudGauge.For(Needs[i], vitals, bl, new Vector2(IconX + i * 26f, needsY), 20f);
                _needs[i].Root.gameObject.SetActive(false);
            }

            // Bottom-right: the weapon.
            RectTransform weapon = HudUI.Stretch(root, "Weapon");
            _weaponGroup = weapon.gameObject.AddComponent<CanvasGroup>();
            _weaponGroup.blocksRaycasts = false;
            HudReadouts.CornerShade(weapon, br, new Vector2(760f, 300f), 0.34f);
            _ammo = HudUI.Label(weapon, "Ammo", 36, TextAnchor.LowerRight, HudUI.Ink, br, new Vector2(1f, 0f),
                new Vector2(-Margin, Bottom + 8f), new Vector2(360f, 44f), FontStyle.Bold);
            _magBar = HudUI.Bar(weapon, "Magazine", br, new Vector2(1f, 0.5f), new Vector2(-Margin, Bottom + 3f), new Vector2(150f, 3f),
                Track, HudUI.Ink, out RectTransform magRoot);
            _magRoot = magRoot.gameObject;
            _name = HudUI.Label(weapon, "Weapon", 13, TextAnchor.LowerRight, HudReadouts.Dim, br, new Vector2(1f, 0f),
                new Vector2(-Margin, Bottom + 54f), new Vector2(360f, 18f), FontStyle.Bold);
            _light = HudUI.Icon(weapon, "Light", HudIcon.Flashlight, HudUI.Ink, br, new Vector2(-Margin - 10f, Bottom + 88f), 20f);
            _nvg = HudUI.Icon(weapon, "NightVision", HudIcon.Goggles, HudUI.Ink, br, new Vector2(-Margin - 10f - 28f, Bottom + 88f), 20f);
            _light.gameObject.SetActive(false);
            _nvg.gameObject.SetActive(false);
        }

        /// <summary>Snaps both corners fully visible (the view was just entered).</summary>
        public void Wake()
        {
            _calm = 0f;
            _vitalsGroup.alpha = 1f;
            _weaponGroup.alpha = 1f;
        }

        public void Tick(float dt, float unscaledDt)
        {
            UpdateVitals(dt, unscaledDt);
            UpdateWeapon(dt, unscaledDt);
            UpdateGadgets();
        }

        private void UpdateVitals(float dt, float unscaledDt)
        {
            PlayerStats s = _ctx.Stats;
            if (s == null) return;
            float hp = s.HealthNormalized;
            float e = Mathf.Clamp01(s.Endurance);
            Color hc = HudReadouts.Health(hp);
            _health.Set(hp, hc, dt);
            _cross.color = hp < 0.5f ? hc : HudUI.Ink;
            Color ec = HudReadouts.Stamina(e);
            _stamina.Set(e, ec, dt);
            HudIcon pose = HudReadouts.Pose(_ctx, ref _stride, dt);
            if (pose != _shownPose)
            {
                _shownPose = pose;
                _figure.sprite = HudIcons.Get(pose);
            }

            // Only the supplies that need attention, packed from the left.
            int sig = 0;
            bool warning = false;
            for (int i = 0; i < Needs.Length; i++)
            {
                bool has = HudReadouts.Need(_ctx, Needs[i], out float level, out Color c, out int severity);
                _needSeverity[i] = has ? severity : 0;
                if (_needSeverity[i] > 0)
                {
                    sig |= 1 << i;
                    warning = true;
                    _needs[i].Set(level, c);
                }
            }
            if (sig != _needsSig)
            {
                _needsSig = sig;
                int slot = 0;
                for (int i = 0; i < Needs.Length; i++)
                {
                    bool on = (sig & (1 << i)) != 0;
                    HudUI.Show(_needs[i].Root.gameObject, on);
                    if (on) _needs[i].Root.anchoredPosition = new Vector2(IconX + slot++ * 26f, Bottom + 50f);
                }
            }

            // Fade back once all is well and nothing has changed for a while.
            bool hurt = _lastHealth >= 0f && hp < _lastHealth - 0.0005f;
            _lastHealth = hp;
            bool fine = hp >= 0.97f && e >= 0.97f && !warning && !s.IsDead;
            _calm = fine && !hurt ? _calm + dt : 0f;
            Fade(_vitalsGroup, _calm >= CalmSeconds ? QuietAlpha : 1f, unscaledDt);
        }

        private void UpdateWeapon(float dt, float unscaledDt)
        {
            _weapon.Poll(_ctx, dt);
            HudWeaponState w = _weapon;
            if (w.Version != _shownVersion)
            {
                _shownVersion = w.Version;
                if (w.Gun != null)
                {
                    _ammo.fontSize = 36;
                    _ammo.color = w.Reloading ? HudUI.WithAlpha(HudUI.Ink, 0.4f) : w.Loaded == 0 ? HudUI.Danger : w.Low ? HudUI.Amber : HudUI.Ink;
                    _ammo.text = HudUI.Int(w.Loaded) + "<size=18><color=#AEB4BE>  / " + HudUI.Int(w.Reserve) + "</color></size>";
                    _name.text = w.Reloading ? "RELOADING\u2026" : w.Name.ToUpperInvariant();
                    _name.color = w.Reloading ? HudUI.Amber : HudReadouts.Dim;
                    HudUI.Show(_magRoot, true);
                    _magBar.fillAmount = Mathf.Clamp01(w.Loaded / (float)Mathf.Max(1, w.Capacity));
                    _magBar.color = _ammo.color;
                }
                else
                {
                    _ammo.fontSize = 22;
                    _ammo.color = HudUI.Ink;
                    _ammo.text = w.Weapon != null ? w.Name.ToUpperInvariant() : "UNARMED";
                    _name.text = w.Weapon != null ? "MELEE" : "";
                    _name.color = HudReadouts.Dim;
                    HudUI.Show(_magRoot, w.ShowCondition);
                    if (w.ShowCondition)
                    {
                        _magBar.fillAmount = w.Condition01;
                        _magBar.color = HudUI.Condition(w.Condition01);
                    }
                }
            }
            if (w.Reloading) _name.color = HudUI.WithAlpha(HudUI.Amber, HudUI.Pulse(7f, 0.4f));

            // Bare hands show nothing once settled; a weapon dims back when idle with plenty in the magazine.
            bool attention = w.Idle < 4f || w.Reloading || w.Low;
            float target = w.Weapon == null ? (w.Idle < 2f ? 0.6f : 0f) : attention ? 1f : WeaponQuietAlpha;
            Fade(_weaponGroup, target, unscaledDt);
        }

        private void UpdateGadgets()
        {
            PlayerGear gear = _ctx.Gear;
            bool hasLight = gear != null && gear.HasFlashlight;
            bool lightOn = hasLight && gear.FlashlightOn;
            int ls = !hasLight ? 0 : lightOn ? 2 : 1;
            if (ls != _lightState)
            {
                _lightState = ls;
                HudUI.Show(_light.gameObject, hasLight);
                _light.color = lightOn ? LightOn : HudUI.WithAlpha(HudUI.Ink, 0.6f);
            }
            bool hasNvg = gear != null && gear.HasNightVision;
            bool nvgOn = hasNvg && gear.NightVisionOn;
            int ns = !hasNvg ? 0 : nvgOn ? 2 : 1;
            if (ns == _nvgState) return;
            _nvgState = ns;
            HudUI.Show(_nvg.gameObject, hasNvg);
            _nvg.color = nvgOn ? NvgOn : HudUI.WithAlpha(HudUI.Ink, 0.6f);
        }

        private static void Fade(CanvasGroup g, float target, float dt)
        {
            float a = Mathf.MoveTowards(g.alpha, target, dt * FadeSpeed);
            if (a != g.alpha) g.alpha = a;
        }
    }
}
