using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Top-left hand slots, Project Zomboid HUD-mod style: a circle for the weapon in hand (line-art
    /// silhouette by kind, a condition bar under it and loaded / reserve rounds for a firearm), a circle
    /// for the flashlight (glows yellow while lit, battery bar under it) and a smaller circle beneath for
    /// worn night-vision goggles (green while on).
    /// </summary>
    public sealed class HudHands
    {
        private const float Margin = 20f, Slot = 58f, Gap = 14f, Small = 40f;
        private static readonly Color DiscColor = new Color(0.03f, 0.035f, 0.045f, 0.62f);
        private static readonly Color RingIdle = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color RingEmpty = new Color(1f, 1f, 1f, 0.28f);
        private static readonly Color LightOn = new Color(1f, 0.87f, 0.38f, 1f);
        private static readonly Color NvgOn = new Color(0.45f, 1f, 0.45f, 1f);
        private static readonly Color AmmoColor = new Color(0.94f, 0.92f, 0.8f, 1f);

        private readonly HudContext _ctx;

        private Image _weaponIcon, _weaponRing;
        private GameObject _condRoot;
        private Image _condFill;
        private Text _ammo;
        private Image _lightIcon, _lightRing, _lightGlow;
        private GameObject _battRoot;
        private Image _battFill;
        private GameObject _nvgRoot;
        private Image _nvgIcon, _nvgRing, _nvgGlow;

        private bool _weaponShown;
        private ItemDef _weapon;
        private ItemDef _gun;
        private int _shownLoaded = -1, _shownReserve = -1, _reserve;
        private float _reserveTimer;
        private bool _shownReloading;
        private int _lightState = -1, _nvgState = -1;

        public HudHands(HudContext ctx, RectTransform root)
        {
            _ctx = ctx;
            RectTransform area = HudUI.Rect(root, "HandSlots", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(Margin, -Margin), new Vector2(Slot * 2f + Gap, Slot + Small + 30f));

            // Primary: the weapon in hand.
            RectTransform primary = Circle(area, "Primary", Slot * 0.5f, -Slot * 0.5f, Slot, out _weaponRing, out _, out Image _);
            _weaponIcon = HudUI.Icon(primary, "Icon", HudIcon.Fist, HudUI.Ink, new Vector2(0.5f, 0.5f), Vector2.zero, Slot * 0.74f);
            _condFill = HudUI.Bar(area, "Condition", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(Slot * 0.5f, -Slot - 5f),
                new Vector2(40f, 4f), new Color(0f, 0f, 0f, 0.55f), HudUI.Good, out RectTransform condRt);
            _condRoot = condRt.gameObject;
            _ammo = HudUI.Label(area, "Ammo", 14, TextAnchor.UpperCenter, AmmoColor, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(Slot * 0.5f, -Slot - 12f), new Vector2(120f, 18f));
            _ammo.enabled = false;

            // Secondary: the flashlight.
            float sx = Slot + Gap + Slot * 0.5f;
            RectTransform secondary = Circle(area, "Secondary", sx, -Slot * 0.5f, Slot, out _lightRing, out _lightGlow, out Image _);
            _lightIcon = HudUI.Icon(secondary, "Icon", HudIcon.Flashlight, HudUI.Ink, new Vector2(0.5f, 0.5f), Vector2.zero, Slot * 0.72f);
            _battFill = HudUI.Bar(area, "Battery", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(sx, -Slot - 5f),
                new Vector2(40f, 4f), new Color(0f, 0f, 0f, 0.55f), HudUI.Good, out RectTransform battRt);
            _battRoot = battRt.gameObject;

            // A smaller circle beneath the flashlight for the night-vision goggles.
            RectTransform nvg = Circle(area, "NightVision", sx, -Slot - 14f - Small * 0.5f, Small, out _nvgRing, out _nvgGlow, out Image _);
            _nvgIcon = HudUI.Icon(nvg, "Icon", HudIcon.Goggles, HudUI.Ink, new Vector2(0.5f, 0.5f), Vector2.zero, Small * 0.74f);
            _nvgRoot = nvg.gameObject;

            _weaponRing.color = RingIdle;
            HudUI.Show(_condRoot, false);
            HudUI.Show(_battRoot, false);
            HudUI.Show(_nvgRoot, false);
        }

        /// <summary>Refreshes the slots (cheap; text only changes when a value does).</summary>
        public void Tick(float dt)
        {
            UpdateWeapon(dt);
            UpdateLight();
            UpdateNightVision();
        }

        /// <summary>Refreshes the weapon/hand icon and ammo/readouts based on equipped gear and combat state.</summary>
        private void UpdateWeapon(float dt)
        {
            PlayerInventory inv = _ctx.Inventory;
            PlayerCombat combat = _ctx.Combat;
            // Prefer the explicitly equipped weapon, but fall back to the equipped firearm if nothing is slotted.
            ItemDef weapon = inv != null ? inv.EquippedWeapon : null;
            ItemDef gun = combat != null ? combat.EquippedFirearm : null;
            if (weapon == null) weapon = gun;

            // Only swap the icon when the hand changes to avoid re-baking sprites every frame.
            if (!_weaponShown || weapon != _weapon)
            {
                _weaponShown = true;
                _weapon = weapon;
                _weaponIcon.sprite = HudIcons.Get(IconFor(weapon));
                _weaponIcon.color = weapon != null ? HudUI.Ink : HudUI.WithAlpha(HudUI.Ink, 0.5f);
            }

            // Tools (melee) show wear via condition; charge-based gear uses its own bars elsewhere, so skip here.
            bool showCondition = weapon != null && weapon.IsDamageable && !weapon.UsesCharge && inv != null;
            HudUI.Show(_condRoot, showCondition);
            if (showCondition)
            {
                float c = inv.EquippedCondition01;
                _condFill.fillAmount = c;
                _condFill.color = HudUI.Condition(c);
            }

            // Switching firearms invalidates cached ammo counts so they redraw immediately.
            if (gun != _gun)
            {
                _gun = gun;
                _reserveTimer = 0f;
                _shownLoaded = _shownReserve = -1;
                _shownReloading = false;
            }

            if (gun == null)
            {
                if (_ammo.enabled) _ammo.enabled = false;
                return;
            }
            if (!_ammo.enabled) _ammo.enabled = true;

            // Show reloading text once and hold it until the state flips back to avoid flicker.
            if (combat.IsReloading)
            {
                if (!_shownReloading)
                {
                    _shownReloading = true;
                    _ammo.text = "RELOADING\u2026";
                    _ammo.color = HudUI.Amber;
                }
                return;
            }

            // Reserve counts are looked up via inventory (which may be expensive or change slowly); throttle to 4/s.
            _reserveTimer -= dt;
            if (_reserveTimer <= 0f)
            {
                _reserveTimer = 0.25f;
                _reserve = inv != null ? inv.AmmoReserve(gun.AmmoId) : 0;
            }
            int loaded = combat.LoadedAmmo;
            // Only rebuild the label when counts/state change; colour red when the mag is empty to draw attention.
            if (_shownReloading || loaded != _shownLoaded || _reserve != _shownReserve)
            {
                _shownReloading = false;
                _shownLoaded = loaded;
                _shownReserve = _reserve;
                _ammo.text = HudUI.Int(loaded) + " / " + HudUI.Int(_reserve);
                _ammo.color = loaded > 0 ? AmmoColor : HudUI.Danger;
            }
        }

        /// <summary>Refreshes the flashlight slot (on/off glow and battery) based on gear state.</summary>
        private void UpdateLight()
        {
            PlayerGear gear = _ctx.Gear;
            bool has = gear != null && gear.HasFlashlight;
            bool on = has && gear.FlashlightOn;
            // Map presence+state to a small enum to avoid redundant UI updates when unchanged.
            int state = !has ? 0 : on ? 2 : 1;
            if (state != _lightState)
            {
                _lightState = state;
                _lightIcon.enabled = has;
                // On = bright yellow ring; off but present = idle ring; absent = dim ring.
                _lightRing.color = state == 2 ? HudUI.WithAlpha(LightOn, 0.9f) : has ? RingIdle : RingEmpty;
                _lightIcon.color = on ? LightOn : HudUI.WithAlpha(HudUI.Ink, 0.85f);
                _lightGlow.enabled = on;
                HudUI.Show(_battRoot, has);
            }
            // Subtle pulsing glow while lit (unscaledTime so it doesn't freeze in pause menus).
            if (on) _lightGlow.color = HudUI.WithAlpha(LightOn, 0.26f + 0.06f * Mathf.Sin(Time.unscaledTime * 2.2f));
            if (has)
            {
                // Clamp charge in case of desync between gear state and HUD.
                float c = Mathf.Clamp01(gear.FlashlightCharge01);
                _battFill.fillAmount = c;
                _battFill.color = HudUI.Condition(c);
            }
        }

        /// <summary>Updates the night-vision goggles slot (green glow while active).</summary>
        private void UpdateNightVision()
        {
            PlayerGear gear = _ctx.Gear;
            bool has = gear != null && gear.HasNightVision;
            bool on = has && gear.NightVisionOn;
            // Map presence+state to a small enum to avoid redundant UI updates when unchanged.
            int state = !has ? 0 : on ? 2 : 1;
            if (state == _nvgState) return;
            _nvgState = state;
            HudUI.Show(_nvgRoot, has);
            // Tinted green when active, otherwise dimmed to indicate availability without power.
            _nvgIcon.color = on ? NvgOn : HudUI.WithAlpha(HudUI.Ink, 0.8f);
            _nvgRing.color = on ? HudUI.WithAlpha(NvgOn, 0.9f) : RingIdle;
            _nvgGlow.enabled = on;
            if (on) _nvgGlow.color = HudUI.WithAlpha(NvgOn, 0.22f);
        }

        /// <summary>The silhouette for a weapon: long gun, pistol, blade, axe, hammer, other blunt, or fists.</summary>
        public static HudIcon IconFor(ItemDef def)
        {
            if (def == null) return HudIcon.Fist;
            string id = def.Id ?? "";
            if (def.IsFirearm)
                return def.PelletsPerShot > 1 || def.TwoHanded || Has(id, "rifle") || Has(id, "shotgun") || Has(id, "carbine")
                    ? HudIcon.LongGun : HudIcon.Pistol;
            if (Has(id, "axe") || Has(id, "hatchet")) return HudIcon.Axe;
            if (Has(id, "hammer")) return HudIcon.Hammer;
            if (def.WeaponSkill == PlayerSkills.Skill.Blade || Has(id, "knife") || Has(id, "machete")) return HudIcon.Blade;
            return def.IsWeapon ? HudIcon.Blunt : HudIcon.Fist;
        }

        private static bool Has(string s, string part) => s.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;

        // A round slot: soft glow (hidden), dark disc and a thin light rim, centred at (cx, cy) from the area's top-left.
        private static RectTransform Circle(RectTransform area, string name, float cx, float cy, float size, out Image ring, out Image glow, out Image disc)
        {
            RectTransform rt = HudUI.Rect(area, name, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(cx, cy), new Vector2(size, size));
            glow = HudUI.Icon(rt, "Glow", HudIcon.Glow, Color.clear, new Vector2(0.5f, 0.5f), Vector2.zero, size * 1.9f, false);
            glow.enabled = false;
            disc = HudUI.Icon(rt, "Disc", HudIcon.Disc, DiscColor, new Vector2(0.5f, 0.5f), Vector2.zero, size, false);
            ring = HudUI.Icon(rt, "Rim", HudIcon.Ring, RingIdle, new Vector2(0.5f, 0.5f), Vector2.zero, size, false);
            return rt;
        }
    }
}
