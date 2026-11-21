using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The over-the-shoulder view's HUD, laid out like a modern third-person action game. Bottom-left, the vitals: a
    /// segmented health bar that leaves a pale trail when a hit lands (with the number beside it), the stamina bar
    /// with the running man under it, and the supplies above — water, food, warmth, battery — with the most urgent
    /// condition spelled out. Bottom-right, the weapon card: the gun's silhouette, a big magazine count over the
    /// reserve, a pip per round in the magazine and the light / night-vision state; or a melee weapon's name and
    /// wear. Both corners sit on a soft shade so they read over bright snow.
    /// </summary>
    public sealed class HudShoulderView
    {
        private const float Margin = 30f, Bottom = 26f, IconX = 46f, BarX = 72f, BarWidth = 280f;
        private const float CardRight = -Margin - 92f - 14f;   // the card's text column ends left of the silhouette
        private const int MaxPips = 30;
        private const float PipStep = 8f;
        private static readonly Color Track = new Color(0f, 0f, 0f, 0.5f);
        private static readonly Color Trail = new Color(1f, 0.86f, 0.78f, 0.75f);
        private static readonly Color PipEmpty = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color LightOn = new Color(1f, 0.87f, 0.38f, 1f);
        private static readonly Color NvgOn = new Color(0.45f, 1f, 0.45f, 1f);
        private static readonly HudNeed[] Needs = { HudNeed.Water, HudNeed.Food, HudNeed.Warmth, HudNeed.Battery };

        private readonly HudContext _ctx;
        private readonly HudWeaponState _weapon = new HudWeaponState();

        // Vitals.
        private readonly HudGauge _cross;
        private readonly HudTrailBar _health, _stamina;
        private readonly Text _healthNumber, _condition;
        private readonly Image _figure;
        private readonly HudGauge[] _needs = new HudGauge[Needs.Length];
        private HudIcon _shownPose = HudIcon.White;
        private float _stride;
        private int _shownHealth = -1;
        private int _conditionKey = int.MinValue;

        // Weapon card.
        private readonly Image _silhouette;
        private readonly Text _ammo, _name;
        private readonly Image[] _pips = new Image[MaxPips];
        private readonly GameObject _magBarRoot;
        private readonly Image _magBar;
        private readonly GameObject _wearRoot;
        private readonly Image _wear;
        private readonly Image _light, _lightBattery, _nvg;
        private readonly GameObject _lightRoot, _nvgRoot;
        private int _shownVersion = -1, _shownPips = -1, _shownLoadedPips = -1;
        private int _nameState = -1;
        private int _lightState = -1, _nvgState = -1;

        public HudShoulderView(HudContext ctx, RectTransform root)
        {
            _ctx = ctx;
            var bl = Vector2.zero;
            var br = new Vector2(1f, 0f);

            // Bottom-left: vitals.
            HudReadouts.CornerShade(root, bl, new Vector2(980f, 440f), 0.5f);
            float needsY = Bottom + 80f, healthY = Bottom + 44f, staminaY = Bottom + 16f;
            for (int i = 0; i < Needs.Length; i++)
                _needs[i] = HudGauge.For(Needs[i], root, bl, new Vector2(IconX + i * 32f, needsY), 24f);
            _condition = HudUI.Label(root, "Condition", 16, TextAnchor.MiddleLeft, HudUI.Amber, bl, new Vector2(0f, 0.5f),
                new Vector2(IconX + Needs.Length * 32f - 6f, needsY), new Vector2(300f, 22f), FontStyle.Bold);
            _condition.text = "";

            _cross = new HudGauge(root, "Health", HudIcon.Cross, HudIcon.CrossFill, false, false, bl, new Vector2(IconX, healthY), 26f);
            _health = new HudTrailBar(root, "HealthBar", bl, new Vector2(0f, 0.5f), new Vector2(BarX, healthY), new Vector2(BarWidth, 12f), 10, Track, Trail);
            _healthNumber = HudUI.Label(root, "HealthValue", 22, TextAnchor.MiddleLeft, HudUI.Ink, bl, new Vector2(0f, 0.5f),
                new Vector2(BarX + BarWidth + 12f, healthY), new Vector2(70f, 28f), FontStyle.Bold);

            _figure = HudUI.Icon(root, "Movement", HudIcon.Stand, HudUI.Ink, bl, new Vector2(IconX, staminaY), 26f);
            _stamina = new HudTrailBar(root, "StaminaBar", bl, new Vector2(0f, 0.5f), new Vector2(BarX, staminaY), new Vector2(BarWidth, 5f), 0, Track, new Color(1f, 1f, 1f, 0.35f));

            // Bottom-right: the weapon card.
            HudReadouts.CornerShade(root, br, new Vector2(1040f, 460f), 0.5f);
            _silhouette = HudUI.Icon(root, "Silhouette", HudIcon.Fist, HudUI.Ink, br, new Vector2(-Margin - 46f, Bottom + 52f), 92f);
            _ammo = HudUI.Label(root, "Ammo", 52, TextAnchor.LowerRight, HudUI.Ink, br, new Vector2(1f, 0f),
                new Vector2(CardRight, Bottom + 26f), new Vector2(420f, 62f), FontStyle.Bold);
            _name = HudUI.Label(root, "Weapon", 15, TextAnchor.LowerRight, HudReadouts.Dim, br, new Vector2(1f, 0f),
                new Vector2(CardRight, Bottom + 92f), new Vector2(420f, 20f), FontStyle.Bold);

            for (int i = 0; i < MaxPips; i++)
            {
                RectTransform pip = HudUI.Rect(root, "Round", br, new Vector2(1f, 0.5f), new Vector2(CardRight - i * PipStep, Bottom + 16f), new Vector2(5f, 13f));
                var img = pip.gameObject.AddComponent<Image>();
                img.sprite = HudIcons.Get(HudIcon.White);
                img.color = PipEmpty;
                img.raycastTarget = false;
                pip.gameObject.SetActive(false);
                _pips[i] = img;
            }
            _magBar = HudUI.Bar(root, "Magazine", br, new Vector2(1f, 0.5f), new Vector2(CardRight, Bottom + 16f), new Vector2(220f, 5f),
                Track, HudUI.Ink, out RectTransform magRoot);
            _magBarRoot = magRoot.gameObject;
            _magBarRoot.SetActive(false);
            _wear = HudUI.Bar(root, "Wear", br, new Vector2(1f, 0.5f), new Vector2(CardRight, Bottom + 16f), new Vector2(200f, 5f),
                Track, HudUI.Good, out RectTransform wearRoot);
            _wearRoot = wearRoot.gameObject;
            _wearRoot.SetActive(false);

            // The light and the goggles, above the silhouette (only while carried).
            float gadgetY = Bottom + 130f;
            _light = HudUI.Icon(root, "Light", HudIcon.Flashlight, HudUI.Ink, br, new Vector2(-Margin - 16f, gadgetY), 30f);
            _lightRoot = _light.gameObject;
            _lightBattery = HudUI.Bar(_light.rectTransform, "Battery", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f),
                new Vector2(26f, 3f), Track, HudUI.Good, out _);
            _nvg = HudUI.Icon(root, "NightVision", HudIcon.Goggles, HudUI.Ink, br, new Vector2(-Margin - 16f - 40f, gadgetY), 30f);
            _nvgRoot = _nvg.gameObject;
            _lightRoot.SetActive(false);
            _nvgRoot.SetActive(false);
        }

        public void Tick(float dt)
        {
            UpdateVitals(dt);
            UpdateWeapon(dt);
            UpdateGadgets();
        }

        #region Vitals
        private void UpdateVitals(float dt)
        {
            PlayerStats s = _ctx.Stats;
            if (s == null) return;

            float hp = s.HealthNormalized;
            Color hc = HudReadouts.Health(hp);
            _health.Set(hp, hc, dt);
            _cross.Set(hp, hp < 0.5f ? hc : HudUI.Ink);
            int shown = Mathf.CeilToInt(hp * 100f);
            if (s.IsDead) shown = 0;
            if (shown != _shownHealth)
            {
                _shownHealth = shown;
                _healthNumber.text = HudUI.Int(shown);
            }
            _healthNumber.color = hp < 0.25f ? hc : HudUI.Ink;

            float e = Mathf.Clamp01(s.Endurance);
            Color ec = HudReadouts.Stamina(e);
            _stamina.Set(e, ec, dt);
            HudIcon pose = HudReadouts.Pose(_ctx, ref _stride, dt);
            if (pose != _shownPose)
            {
                _shownPose = pose;
                _figure.sprite = HudIcons.Get(pose);
            }
            _figure.color = e < CloneZConfig.EnduranceDanger ? Color.Lerp(HudUI.Ink, ec, 0.6f) : HudUI.Ink;

            for (int i = 0; i < Needs.Length; i++)
            {
                bool has = HudReadouts.Need(_ctx, Needs[i], out float level, out Color c, out _);
                if (!has) c = new Color(1f, 1f, 1f, 0.2f);
                _needs[i].Set(has ? level : 0f, c);
            }
            UpdateCondition(s);
        }

        // The single most urgent thing wrong with the survivor, in words (rebuilt only when it changes).
        private void UpdateCondition(PlayerStats s)
        {
            PlayerWounds w = _ctx.Wounds;
            bool bleeding = w != null && w.IsBleeding;
            int infected = s.Infected ? Mathf.RoundToInt(s.Infection) : -1;
            int wounds = w != null ? w.WoundCount : 0;
            int key = (bleeding ? 1 : 0) | ((infected + 1) << 1) | (wounds << 9);
            if (key == _conditionKey)
            {
                if (bleeding) _condition.color = HudUI.WithAlpha(HudUI.Danger, HudUI.Pulse(6f, 0.45f));
                return;
            }
            _conditionKey = key;
            if (bleeding)
            {
                _condition.text = "BLEEDING";
                _condition.color = HudUI.Danger;
            }
            else if (infected >= 0)
            {
                _condition.text = "INFECTED  " + HudUI.Int(infected) + "%";
                _condition.color = HudUI.Danger;
            }
            else if (wounds > 0)
            {
                _condition.text = wounds == 1 ? "1 WOUND" : HudUI.Int(wounds) + " WOUNDS";
                _condition.color = HudUI.Amber;
            }
            else _condition.text = "";
        }
        #endregion

        #region Weapon card
        private void UpdateWeapon(float dt)
        {
            _weapon.Poll(_ctx, dt);
            HudWeaponState w = _weapon;
            if (w.Version != _shownVersion)
            {
                _shownVersion = w.Version;
                RebuildCard(w);
            }

            // States that animate between changes: the reload pulse, an empty magazine.
            int nameState = w.Gun == null ? 0 : w.Reloading ? 1 : w.Loaded == 0 ? (w.Reserve > 0 ? 2 : 3) : 0;
            if (nameState != _nameState)
            {
                _nameState = nameState;
                switch (nameState)
                {
                    case 1: _name.text = "RELOADING\u2026"; break;
                    case 2: _name.text = "EMPTY  \u00b7  R TO RELOAD"; _name.color = HudUI.Amber; break;
                    case 3: _name.text = "OUT OF AMMO"; _name.color = HudUI.Danger; break;
                    default: _name.text = w.Gun != null ? Caption(w) : w.Weapon != null ? "MELEE" : ""; _name.color = HudReadouts.Dim; break;
                }
            }
            if (nameState == 1)
            {
                _name.color = HudUI.WithAlpha(HudUI.Amber, HudUI.Pulse(7f, 0.4f));
                _ammo.color = HudUI.WithAlpha(HudUI.Ink, 0.35f);
            }
        }

        private void RebuildCard(HudWeaponState w)
        {
            _silhouette.sprite = HudIcons.Get(HudHands.IconFor(w.Weapon));
            _silhouette.color = w.Weapon != null ? HudUI.Ink : HudUI.WithAlpha(HudUI.Ink, 0.55f);
            _nameState = -1;   // re-word the caption under the new state

            if (w.Gun != null)
            {
                Color number = w.Loaded == 0 ? HudUI.Danger : w.Low ? HudUI.Amber : HudUI.Ink;
                _ammo.fontSize = 52;
                _ammo.color = number;
                _ammo.text = HudUI.Int(w.Loaded) + "<size=24><color=#AEB4BE>  / " + HudUI.Int(w.Reserve) + "</color></size>";
                HudUI.Show(_wearRoot, false);
                bool pips = w.Capacity <= MaxPips;
                HudUI.Show(_magBarRoot, !pips);
                SetPips(pips ? w.Capacity : 0, w.Loaded);
                if (!pips)
                {
                    _magBar.fillAmount = Mathf.Clamp01(w.Loaded / (float)Mathf.Max(1, w.Capacity));
                    _magBar.color = number;
                }
                return;
            }

            SetPips(0, 0);
            HudUI.Show(_magBarRoot, false);
            _ammo.fontSize = 30;
            _ammo.color = w.Weapon != null ? HudUI.Ink : HudUI.WithAlpha(HudUI.Ink, 0.6f);
            _ammo.text = w.Weapon != null ? w.Name.ToUpperInvariant() : "UNARMED";
            HudUI.Show(_wearRoot, w.ShowCondition);
            if (w.ShowCondition)
            {
                _wear.fillAmount = w.Condition01;
                _wear.color = HudUI.Condition(w.Condition01);
            }
        }

        private static string Caption(HudWeaponState w) => w.Name.ToUpperInvariant();

        private void SetPips(int capacity, int loaded)
        {
            capacity = Mathf.Clamp(capacity, 0, MaxPips);
            loaded = Mathf.Clamp(loaded, 0, capacity);
            if (capacity != _shownPips)
            {
                _shownPips = capacity;
                _shownLoadedPips = -1;
                for (int i = 0; i < MaxPips; i++) HudUI.Show(_pips[i].gameObject, i < capacity);
            }
            if (loaded == _shownLoadedPips) return;
            _shownLoadedPips = loaded;
            // Rounds empty from the far end toward the gun, like a magazine seen from the side.
            for (int i = 0; i < capacity; i++) _pips[i].color = i < loaded ? HudUI.Ink : PipEmpty;
        }
        #endregion

        private void UpdateGadgets()
        {
            PlayerGear gear = _ctx.Gear;
            bool hasLight = gear != null && gear.HasFlashlight;
            bool lightOn = hasLight && gear.FlashlightOn;
            int ls = !hasLight ? 0 : lightOn ? 2 : 1;
            if (ls != _lightState)
            {
                _lightState = ls;
                HudUI.Show(_lightRoot, hasLight);
                _light.color = lightOn ? LightOn : HudUI.WithAlpha(HudUI.Ink, 0.7f);
            }
            if (hasLight)
            {
                float c = Mathf.Clamp01(gear.FlashlightCharge01);
                _lightBattery.fillAmount = c;
                _lightBattery.color = HudUI.Condition(c);
            }

            bool hasNvg = gear != null && gear.HasNightVision;
            bool nvgOn = hasNvg && gear.NightVisionOn;
            int ns = !hasNvg ? 0 : nvgOn ? 2 : 1;
            if (ns == _nvgState) return;
            _nvgState = ns;
            HudUI.Show(_nvgRoot, hasNvg);
            _nvg.color = nvgOn ? NvgOn : HudUI.WithAlpha(HudUI.Ink, 0.7f);
        }
    }
}
