using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>The supplies the view HUDs show as small filling icons.</summary>
    public enum HudNeed { Water, Food, Warmth, Battery }

    /// <summary>
    /// Readings shared by the per-view HUDs, worded and coloured like the isometric status row: white while fine,
    /// amber past the warning level, pulsing red past the critical one.
    /// </summary>
    public static class HudReadouts
    {
        public static readonly Color Cold = new Color(0.45f, 0.72f, 1f, 1f);
        public static readonly Color Hot = new Color(1f, 0.5f, 0.3f, 1f);
        public static readonly Color Dim = new Color(0.70f, 0.73f, 0.78f, 0.9f);

        /// <summary>White while fine, amber past <paramref name="warnAt"/>, pulsing red past <paramref name="criticalAt"/> (bad01: 1 = worst).</summary>
        public static Color Severity(float bad01, float warnAt, float criticalAt)
        {
            if (bad01 >= criticalAt) return HudUI.WithAlpha(HudUI.Danger, HudUI.Pulse(6f, 0.35f));
            if (bad01 >= warnAt) return HudUI.Amber;
            return HudUI.Ink;
        }

        /// <summary>0 fine, 1 warning, 2 critical.</summary>
        public static int Level(float bad01, float warnAt, float criticalAt) => bad01 >= criticalAt ? 2 : bad01 >= warnAt ? 1 : 0;

        /// <summary>A supply's level (what its icon fills to), colour and severity; false when it doesn't apply (no battery carried).</summary>
        public static bool Need(HudContext ctx, HudNeed need, out float level01, out Color color, out int severity)
        {
            level01 = 1f;
            color = HudUI.Ink;
            severity = 0;
            PlayerStats s = ctx.Stats;
            switch (need)
            {
                case HudNeed.Water:
                    if (s == null) return false;
                    level01 = 1f - s.Thirst;
                    severity = Level(s.Thirst, 0.6f, 0.85f);
                    color = Severity(s.Thirst, 0.6f, 0.85f);
                    return true;
                case HudNeed.Food:
                    if (s == null) return false;
                    level01 = 1f - s.Hunger;
                    severity = Level(s.Hunger, 0.6f, 0.85f);
                    color = Severity(s.Hunger, 0.6f, 0.85f);
                    return true;
                case HudNeed.Warmth:
                    if (s == null) return false;
                    float t = s.Temperature;
                    level01 = t;
                    if (t < CloneZConfig.TempComfortLow)
                    {
                        bool critical = t < CloneZConfig.TempHypothermiaThreshold + 0.02f;
                        severity = critical ? 2 : 1;
                        color = critical ? HudUI.WithAlpha(Cold, HudUI.Pulse(5f, 0.35f)) : Cold;
                    }
                    else if (t > CloneZConfig.TempComfortHigh)
                    {
                        bool critical = t > CloneZConfig.TempHyperthermiaThreshold - 0.01f;
                        severity = critical ? 2 : 1;
                        color = critical ? HudUI.WithAlpha(Hot, HudUI.Pulse(5f, 0.35f)) : Hot;
                    }
                    return true;
                default:
                    PlayerGear gear = ctx.Gear;
                    float b = gear != null ? gear.BatteryCharge01 : -1f;
                    if (b < 0f) return false;
                    level01 = b;
                    severity = Level(1f - b, 0.7f, 0.9f);
                    color = Severity(1f - b, 0.7f, 0.9f);
                    return true;
            }
        }

        /// <summary>The health colour: green through amber to red, throbbing when it's nearly gone.</summary>
        public static Color Health(float hp01)
        {
            Color c = HudUI.Condition(hp01);
            return hp01 < 0.25f ? Color.Lerp(c, new Color(1f, 0.85f, 0.8f, 1f), (1f - HudUI.Pulse(5f, 0f)) * 0.35f) : c;
        }

        /// <summary>The stamina colour: white, amber when low, pulsing red when spent.</summary>
        public static Color Stamina(float e)
        {
            if (e < 0.1f) return HudUI.WithAlpha(HudUI.Danger, HudUI.Pulse(7f, 0.35f));
            if (e < CloneZConfig.EnduranceDanger) return HudUI.Danger;
            if (e < CloneZConfig.EnduranceWarning) return HudUI.Amber;
            return HudUI.Ink;
        }

        /// <summary>The running-man frame for how the survivor is moving, advancing a two-frame stride.</summary>
        public static HudIcon Pose(HudContext ctx, ref float stride, float dt)
        {
            PlayerStats stats = ctx.Stats;
            if (stats == null) return HudIcon.Stand;
            PlayerController pc = ctx.Controller;
            bool moving = stats.Exertion != Exertion.Idle && (pc == null || pc.CurrentSpeed > 0.05f);
            bool sneaking = pc != null && pc.IsSneaking;
            bool running = stats.Exertion == Exertion.Running || stats.Exertion == Exertion.Sprinting;
            if (stats.IsDead || !moving)
            {
                stride = 0f;
                return sneaking && !stats.IsDead ? HudIcon.Sneak1 : HudIcon.Stand;
            }
            stride += dt * (running ? 6.5f : sneaking ? 2.8f : 4f);
            bool alt = ((int)stride & 1) == 1;
            if (sneaking) return alt ? HudIcon.Sneak2 : HudIcon.Sneak1;
            if (running) return alt ? HudIcon.Run2 : HudIcon.Run1;
            return alt ? HudIcon.Walk2 : HudIcon.Walk1;
        }

        /// <summary>A large soft dark shade centred on a screen corner, so light text stays readable over snow.</summary>
        public static Image CornerShade(Transform parent, Vector2 corner, Vector2 size, float alpha)
        {
            Image img = HudUI.Icon(parent, "Shade", HudIcon.Glow, new Color(0f, 0f, 0f, alpha), corner, Vector2.zero, 1f, false);
            img.preserveAspect = false;
            img.rectTransform.sizeDelta = size;
            return img;
        }
    }

    /// <summary>A supply icon whose interior fills with its level, tinted by severity (the status row's style).</summary>
    public sealed class HudGauge
    {
        public readonly RectTransform Root;
        private readonly Image _outline, _fill;
        private readonly bool _twoLayer;
        private readonly float _min, _max;
        private float _shownLevel = -1f;

        public HudGauge(Transform parent, string name, HudIcon outline, HudIcon fill, bool twoLayer, bool horizontal, Vector2 anchor, Vector2 pos, float size)
        {
            Root = HudUI.Rect(parent, name, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var c = new Vector2(0.5f, 0.5f);
            if (twoLayer)
            {
                _outline = HudUI.Icon(Root, "Outline", outline, HudUI.WithAlpha(HudUI.Ink, 0.28f), c, Vector2.zero, size);
                _fill = HudUI.Icon(Root, "Level", outline, HudUI.Ink, c, Vector2.zero, size);
            }
            else
            {
                _fill = HudUI.Icon(Root, "Level", fill, HudUI.WithAlpha(HudUI.Ink, 0.5f), c, Vector2.zero, size, false);
                _outline = HudUI.Icon(Root, "Outline", outline, HudUI.Ink, c, Vector2.zero, size);
            }
            _twoLayer = twoLayer;
            HudUI.MakeFilled(_fill, horizontal);
            HudIconArt.FillRange(twoLayer ? outline : fill, out _min, out _max);
        }

        public static HudGauge For(HudNeed need, Transform parent, Vector2 anchor, Vector2 pos, float size)
        {
            switch (need)
            {
                case HudNeed.Water: return new HudGauge(parent, "Water", HudIcon.Bottle, HudIcon.BottleFill, false, false, anchor, pos, size);
                case HudNeed.Food: return new HudGauge(parent, "Food", HudIcon.Cutlery, HudIcon.Cutlery, true, false, anchor, pos, size);
                case HudNeed.Warmth: return new HudGauge(parent, "Warmth", HudIcon.Thermometer, HudIcon.ThermometerFill, false, false, anchor, pos, size);
                default: return new HudGauge(parent, "Battery", HudIcon.Battery, HudIcon.BatteryFill, false, true, anchor, pos, size);
            }
        }

        public void Set(float level01, Color color)
        {
            // Quantised so a slowly draining stat doesn't rebuild the mesh every frame.
            float level = Mathf.Round(Mathf.Clamp01(level01) * 64f) / 64f;
            if (level != _shownLevel)
            {
                _shownLevel = level;
                _fill.fillAmount = Mathf.Lerp(_min, _max, level);
            }
            if (_twoLayer)
            {
                _outline.color = HudUI.WithAlpha(color, color.a * 0.28f);
                _fill.color = color;
            }
            else
            {
                _outline.color = color;
                _fill.color = HudUI.WithAlpha(color, color.a * 0.5f);
            }
        }
    }

    /// <summary>
    /// A horizontal bar that snaps down when its value drops and leaves a pale trail that drains after a beat (the
    /// "how much did that hit take" read of action games), easing up again as the value recovers. Optional segment
    /// ticks split it into equal parts.
    /// </summary>
    public sealed class HudTrailBar
    {
        public readonly RectTransform Root;
        private readonly Image _trail, _fill;
        private readonly float _quant;
        private float _shown = -1f, _trailValue, _last, _hold;

        public HudTrailBar(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int segments, Color track, Color trail)
        {
            Root = HudUI.Rect(parent, name, anchor, pivot, pos, size);
            var bg = Root.gameObject.AddComponent<Image>();
            bg.sprite = HudIcons.Get(HudIcon.White);
            bg.color = track;
            bg.raycastTarget = false;
            UITheme.AddShadow(bg);
            _trail = FillImage(Root, "Trail", trail);
            _fill = FillImage(Root, "Fill", HudUI.Ink);
            for (int i = 1; i < segments; i++)
            {
                RectTransform tick = HudUI.Rect(Root, "Tick", new Vector2(i / (float)segments, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, size.y + 2f));
                var img = tick.gameObject.AddComponent<Image>();
                img.sprite = HudIcons.Get(HudIcon.White);
                img.color = new Color(0f, 0f, 0f, 0.62f);
                img.raycastTarget = false;
            }
            _quant = Mathf.Max(64f, size.x);
        }

        private static Image FillImage(RectTransform parent, string name, Color color)
        {
            RectTransform rt = HudUI.Stretch(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = HudIcons.Get(HudIcon.White);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Shows <paramref name="value01"/> in <paramref name="color"/>, animating by <paramref name="dt"/>.</summary>
        public void Set(float value01, Color color, float dt)
        {
            value01 = Mathf.Clamp01(value01);
            if (_shown < 0f) _shown = _trailValue = _last = value01;
            if (value01 < _last - 0.0005f) _hold = 0.5f;   // just lost some: hold the trail a moment first
            _last = value01;

            _shown = value01 < _shown ? value01 : Mathf.MoveTowards(_shown, value01, dt * 0.6f);
            if (_trailValue < _shown) _trailValue = _shown;
            else if (_hold > 0f) _hold -= dt;
            else _trailValue = Mathf.MoveTowards(_trailValue, _shown, dt * 0.8f);

            _fill.fillAmount = Mathf.Round(_shown * _quant) / _quant;
            _trail.fillAmount = Mathf.Round(_trailValue * _quant) / _quant;
            _fill.color = color;
        }
    }

    /// <summary>
    /// What's in the survivor's hands, polled once a frame for the view HUDs: the weapon, the gun and its magazine,
    /// the reserve (re-counted four times a second), reloading and wear. <see cref="Version"/> bumps whenever any of
    /// it changes, so the widgets rebuild their text only then.
    /// </summary>
    public sealed class HudWeaponState
    {
        /// <summary>The item in hand (a gun, a melee weapon or a tool); null for bare hands.</summary>
        public ItemDef Weapon { get; private set; }
        public ItemDef Gun { get; private set; }
        public int Loaded { get; private set; }
        public int Reserve { get; private set; }
        public int Capacity { get; private set; }
        public bool Reloading { get; private set; }
        public bool ShowCondition { get; private set; }
        public float Condition01 { get; private set; } = 1f;
        public int Version { get; private set; }
        /// <summary>Seconds since anything here last changed (a shot, a reload, a swap, ammo picked up).</summary>
        public float Idle { get; private set; }

        private bool _polled;
        private float _reserveTimer;

        public void Poll(HudContext ctx, float dt)
        {
            PlayerInventory inv = ctx.Inventory;
            PlayerCombat combat = ctx.Combat;
            ItemDef gun = combat != null ? combat.EquippedFirearm : null;
            ItemDef weapon = inv != null ? inv.EquippedWeapon : null;
            if (weapon == null) weapon = gun;

            bool swapped = !_polled || weapon != Weapon || gun != Gun;
            int reserve = Reserve;
            _reserveTimer -= dt;
            if (swapped || _reserveTimer <= 0f)
            {
                _reserveTimer = 0.25f;
                reserve = gun != null && inv != null ? inv.AmmoReserve(gun.AmmoId) : 0;
            }
            int loaded = gun != null && combat != null ? combat.LoadedAmmo : 0;
            int capacity = gun != null ? Mathf.Max(1, gun.MagazineSize) : 0;
            bool reloading = gun != null && combat != null && combat.IsReloading;
            bool wears = weapon != null && weapon.IsDamageable && !weapon.UsesCharge && inv != null;
            float condition = wears ? Mathf.Round(Mathf.Clamp01(inv.EquippedCondition01) * 50f) / 50f : 1f;

            if (swapped || loaded != Loaded || reserve != Reserve || capacity != Capacity || reloading != Reloading
                || wears != ShowCondition || condition != Condition01)
            {
                _polled = true;
                Weapon = weapon;
                Gun = gun;
                Loaded = loaded;
                Reserve = reserve;
                Capacity = capacity;
                Reloading = reloading;
                ShowCondition = wears;
                Condition01 = condition;
                Version++;
                Idle = 0f;
            }
            else Idle += dt;
        }

        /// <summary>True when the magazine is at or under a third (and the gun isn't being reloaded).</summary>
        public bool Low => Gun != null && !Reloading && Loaded <= Mathf.Max(1, Mathf.CeilToInt(Capacity * 0.3f));

        /// <summary>The weapon's name as the HUD prints it ("Bare hands" when empty).</summary>
        public string Name => Weapon == null ? "Bare hands"
            : !string.IsNullOrEmpty(Weapon.DisplayName) ? Weapon.DisplayName : Weapon.Id;
    }
}
