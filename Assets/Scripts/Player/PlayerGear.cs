using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// The survivor's powered and protective gear: night-vision goggles worn in the Eyes slot (toggled
    /// with N, running their battery down), a flashlight carried in the pack (toggled with L — the
    /// best-charged one is used), a gas mask worn in the Mask slot (burning filter inside a
    /// <see cref="ToxicZone"/>; without one, or with the filter spent, the gas sickens and chokes) and the
    /// batteries / filters that top them up (<see cref="TryUseItem"/>, reached through
    /// <see cref="PlayerInventory.Use"/>).
    ///
    /// Charge lives on the items themselves — a worn piece's condition (<see cref="PlayerClothing.SetCondition"/>)
    /// or a carried stack's <see cref="ItemStack.Condition"/>, 0..MaxCondition — so it is saved, dropped
    /// and looted with the item. This component keeps the precise charge as a float and writes the whole
    /// number back only when it changes. It also adds <see cref="PlayerGearVisuals"/>, which draws the gear
    /// on the body. The HUD and the night-vision / flashlight effects read the properties below.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerGear : MonoBehaviour
    {
        public static PlayerGear Instance { get; private set; }

        private const float BreathHeight = 1.55f;       // where the survivor breathes, above the feet
        private const float GasFollowRate = 2.5f;       // 1/s: how quickly the felt concentration follows the air
        private const float GasThreshold = 0.02f;       // below this the air counts as clean
        private const float GasSicknessPerSec = 4f;     // Sickness gained per second at the heart of a cloud
        private const float GasHealthPerSec = 1.5f;     // health lost per second at the heart of a cloud
        private const float CoughNoiseRadius = 10f;     // metres a cough carries to the dead
        private const float CoughMinSeconds = 2.5f, CoughMaxSeconds = 5f;
        private const float GasWarningSeconds = 20f;    // how often the bare-faced gas warning repeats
        private const float FilterLowFraction = 0.2f;   // warn once when the filter drops below this
        private const int NotifyStep = 10;              // UIs hear about a draining charge every 10 condition points

        /// <summary>True when night-vision goggles are worn.</summary>
        public bool HasNightVision { get; protected set; }
        /// <summary>True while the goggles are switched on (and have charge).</summary>
        public bool NightVisionOn { get; protected set; }
        /// <summary>Battery left in the worn goggles, 0..1.</summary>
        public float NightVisionCharge01 { get; protected set; }

        /// <summary>True when a flashlight is carried.</summary>
        public bool HasFlashlight { get; protected set; }
        /// <summary>True while the flashlight is switched on (and has charge).</summary>
        public bool FlashlightOn { get; protected set; }
        /// <summary>Battery left in the flashlight in use, 0..1.</summary>
        public float FlashlightCharge01 { get; protected set; }

        /// <summary>True when a gas mask is worn.</summary>
        public bool HasGasMask { get; protected set; }
        /// <summary>Filter left in the worn mask, 0..1.</summary>
        public float FilterCharge01 { get; protected set; }
        /// <summary>Toxic gas concentration where the survivor stands, 0..1.</summary>
        public float GasConcentration { get; protected set; }

        /// <summary>True while the survivor breathes toxic gas unprotected (no mask, or a spent filter).</summary>
        public bool BreathingGas { get; protected set; }

        /// <summary>Battery shown on the HUD's battery icon: the goggles if worn, else the flashlight;
        /// -1 when the survivor carries nothing battery-powered.</summary>
        public float BatteryCharge01 => HasNightVision ? NightVisionCharge01 : HasFlashlight ? FlashlightCharge01 : -1f;

        /// <summary>The carried flashlight in use (the one switched on, else the best-charged one), or null.</summary>
        public ItemStack ActiveFlashlight => _flash;

        /// <summary>A short message for the player ("The night-vision battery died").</summary>
        public event System.Action<string> Notice;

        /// <summary>Raised each time the survivor coughs on toxic gas (for a sound or an animation).</summary>
        public event System.Action Coughed;

        private PlayerStats _stats;
        private PlayerClothing _clothing;
        private PlayerInventory _inventory;
        private bool _subscribed;

        // Night vision (the Eyes slot): the precise charge in condition units, and the whole number last
        // written back to (or read from) the worn piece.
        private ItemDef _nvgDef;
        private float _nvgCharge;
        private int _nvgWritten;

        // Gas mask (the Mask slot).
        private ItemDef _maskDef;
        private float _filterCharge;
        private int _filterWritten;
        private bool _filterLowWarned;

        // Flashlight (a stack in the pack).
        private ItemStack _flash;
        private float _flashCharge;
        private int _flashWritten;
        private bool _packDirty = true;

        private float _coughTimer;
        private float _gasWarnTimer;

        private PlayerClothing Clothing => _clothing != null ? _clothing : (_clothing = GetComponent<PlayerClothing>());
        private PlayerInventory Pack => _inventory != null ? _inventory : (_inventory = GetComponent<PlayerInventory>());
        private Inventory PackInventory => Pack != null ? Pack.Inventory : null;

        protected virtual void Awake()
        {
            Instance = this;
            _stats = GetComponent<PlayerStats>();
            if (GetComponent<PlayerGearVisuals>() == null) gameObject.AddComponent<PlayerGearVisuals>();
        }

        protected virtual void OnEnable() => Subscribe();

        protected virtual void OnDisable() => Unsubscribe();

        protected virtual void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        protected virtual void Start()
        {
            SyncWorn();
            SyncPack();
            Publish();
        }

        private void Subscribe()
        {
            if (_subscribed || Pack == null) return;
            Pack.Changed += MarkPackDirty;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            if (_inventory != null) _inventory.Changed -= MarkPackDirty;
        }

        private void MarkPackDirty() => _packDirty = true;

        protected virtual void Update()
        {
            if (!_subscribed) Subscribe();
            if (_stats != null && _stats.IsDead) return;
            float dt = Time.deltaTime;

            SyncWorn();
            if (_packDirty) SyncPack();
            else if (_flash != null && _flash.Condition != _flashWritten) LoadFlashCharge();   // recharged from outside

            if (!UIState.GameplayBlocked)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.nKey.wasPressedThisFrame) ToggleNightVision();
                    if (kb.lKey.wasPressedThisFrame) ToggleFlashlight();
                }
            }

            if (dt > 0f)
            {
                DrainNightVision(dt);
                DrainFlashlight(dt);
            }
            UpdateGas(dt);
            Publish();
        }

        #region Using items
        /// <summary>Uses a gear item from the pack: a battery recharges the flattest battery device (the
        /// worn goggles, or goggles / a flashlight in the pack), a filter goes into the gas mask (the worn
        /// one first), a flashlight is switched on or off. True if something happened.</summary>
        public virtual bool TryUseItem(ItemStack stack)
        {
            if (stack == null || stack.Def == null || stack.Count <= 0) return false;
            Inventory inv = PackInventory;
            if (inv == null || !inv.Stacks.Contains(stack)) return false;

            switch (stack.Def.Gear)
            {
                case GearKind.Battery: return UseBattery(inv, stack);
                case GearKind.Filter: return UseFilter(inv, stack);
                case GearKind.Flashlight: return UseFlashlight(stack);
                default: return false;
            }
        }

        private bool UseBattery(Inventory inv, ItemStack battery)
        {
            if (!ChargeFlattest(1f, out string name))
            {
                Say(_nvgDef != null || _flash != null || Carries(inv, GearKind.NightVision)
                    ? "Your batteries are all still full"
                    : "Nothing to put a battery in");
                return false;
            }
            inv.Remove(battery, 1);
            Say("Fresh battery in the " + name);
            return true;
        }

        /// <summary>Puts <paramref name="fraction"/> of a full charge into the flattest battery device (worn goggles
        /// or a carried flashlight / goggles): a fresh battery (1) or a few turns of a hand-crank. False when everything is full.</summary>
        public bool ChargeFlattest(float fraction, out string name)
        {
            name = null;
            Inventory inv = PackInventory;
            if (inv == null) return false;
            SyncWorn();
            SyncPack();

            // The flattest battery device that isn't already full.
            bool worn = false;
            ItemStack target = null;
            float lowest = float.MaxValue;
            if (_nvgDef != null && _nvgDef.MaxCondition > 0 && _nvgWritten < _nvgDef.MaxCondition)
            {
                lowest = _nvgCharge / _nvgDef.MaxCondition;
                worn = true;
            }
            List<ItemStack> stacks = inv.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack s = stacks[i];
                ItemDef d = s.Def;
                if (d == null || s.Count <= 0 || d.MaxCondition <= 0 || s.Condition >= d.MaxCondition) continue;
                if (d.Gear != GearKind.NightVision && d.Gear != GearKind.Flashlight) continue;
                float charge = (s == _flash ? _flashCharge : s.Condition) / d.MaxCondition;
                if (charge < lowest) { lowest = charge; target = s; worn = false; }
            }

            if (!worn && target == null) return false;

            if (worn)
            {
                _nvgCharge = Mathf.Min(_nvgDef.MaxCondition, _nvgCharge + _nvgDef.MaxCondition * fraction);
                _nvgWritten = Mathf.CeilToInt(_nvgCharge);
                if (Clothing != null) Clothing.SetCondition(ClothingSlot.Eyes, _nvgWritten, true);
                name = _nvgDef.DisplayName;
            }
            else
            {
                float now = target == _flash ? _flashCharge : target.Condition;
                target.Condition = Mathf.Min(target.Def.MaxCondition, Mathf.CeilToInt(now + target.Def.MaxCondition * fraction));
                if (target == _flash) LoadFlashCharge();
                name = target.Def.DisplayName;
            }
            Publish();
            return true;
        }

        private bool UseFilter(Inventory inv, ItemStack filter)
        {
            SyncWorn();

            string into = null;
            if (_maskDef != null && _maskDef.MaxCondition > 0 && _filterWritten < _maskDef.MaxCondition)
            {
                _filterCharge = _filterWritten = _maskDef.MaxCondition;
                _filterLowWarned = false;
                if (Clothing != null) Clothing.SetCondition(ClothingSlot.Mask, _maskDef.MaxCondition, true);
                into = "your gas mask";
            }
            else
            {
                // No worn mask needing one: the most-used mask in the pack.
                ItemStack mask = null;
                List<ItemStack> stacks = inv.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                {
                    ItemStack s = stacks[i];
                    ItemDef d = s.Def;
                    if (d == null || d.Gear != GearKind.GasMask || d.MaxCondition <= 0 || s.Condition >= d.MaxCondition) continue;
                    if (mask == null || s.Condition < mask.Condition) mask = s;
                }
                if (mask != null)
                {
                    mask.Condition = mask.Def.MaxCondition;
                    into = "the " + mask.Def.DisplayName + " in your pack";
                }
            }

            if (into == null)
            {
                Say(_maskDef != null || Carries(inv, GearKind.GasMask)
                    ? "The gas mask filter is still fresh"
                    : "No gas mask to fit the filter to");
                return false;
            }
            inv.Remove(filter, 1);
            Say("Screwed a fresh filter into " + into);
            Publish();
            return true;
        }

        private bool UseFlashlight(ItemStack torch)
        {
            if (FlashlightOn && torch == _flash)
            {
                FlashlightOn = false;
                _packDirty = true;
                Publish();
                return true;
            }
            if (torch.Condition <= 0)
            {
                Say("That flashlight's battery is flat");
                return false;
            }
            _flash = torch;
            LoadFlashCharge();
            FlashlightOn = true;
            Publish();
            return true;
        }

        private static bool Carries(Inventory inv, GearKind kind)
        {
            List<ItemStack> stacks = inv.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Def != null && stacks[i].Def.Gear == kind) return true;
            return false;
        }
        #endregion

        #region Toggles
        /// <summary>Switches the worn night-vision goggles on or off (N). They only come on when worn and
        /// holding some charge.</summary>
        public virtual void ToggleNightVision()
        {
            SyncWorn();
            if (NightVisionOn)
            {
                NightVisionOn = false;
                Publish();
                return;
            }
            if (_nvgDef == null) { Say("You aren't wearing night-vision goggles"); return; }
            if (_nvgCharge <= 0f) { Say("The night-vision battery is flat \u2014 use a battery"); return; }
            NightVisionOn = true;
            Publish();
        }

        /// <summary>Switches the flashlight on or off (L) — the carried one holding the most charge.</summary>
        public virtual void ToggleFlashlight()
        {
            if (FlashlightOn)
            {
                FlashlightOn = false;
                _packDirty = true;
                Publish();
                return;
            }
            SyncPack();
            if (_flash == null) { Say("You aren't carrying a flashlight"); return; }
            if (_flashCharge <= 0f) { Say("The flashlight's battery is flat \u2014 use a battery"); return; }
            FlashlightOn = true;
            Publish();
        }
        #endregion

        #region Charge bookkeeping
        // Follows the worn goggles and mask: switches the goggles off when they come off, and re-reads a
        // charge that changed from outside (a refill, a loaded save).
        private void SyncWorn()
        {
            PlayerClothing clothing = Clothing;

            PlayerClothing.WornPiece eyes = clothing != null ? clothing.GetPiece(ClothingSlot.Eyes) : default;
            ItemDef nvg = eyes.Def != null && eyes.Def.Gear == GearKind.NightVision ? eyes.Def : null;
            if (nvg != _nvgDef)
            {
                _nvgDef = nvg;
                NightVisionOn = false;   // goggles taken off go dark; a fresh pair starts switched off
                _nvgCharge = _nvgWritten = nvg != null ? eyes.Condition : 0;
            }
            else if (nvg != null && eyes.Condition != _nvgWritten)
            {
                _nvgCharge = _nvgWritten = eyes.Condition;
            }

            PlayerClothing.WornPiece face = clothing != null ? clothing.GetPiece(ClothingSlot.Mask) : default;
            ItemDef mask = face.Def != null && face.Def.Gear == GearKind.GasMask ? face.Def : null;
            if (mask != _maskDef)
            {
                _maskDef = mask;
                _filterCharge = _filterWritten = mask != null ? face.Condition : 0;
                _filterLowWarned = false;
            }
            else if (mask != null && face.Condition != _filterWritten)
            {
                _filterCharge = _filterWritten = face.Condition;
                _filterLowWarned = false;
            }
        }

        // Follows the pack: a lit flashlight that leaves it goes dark; while off, the one to use next is
        // whichever carried flashlight holds the most charge.
        private void SyncPack()
        {
            _packDirty = false;
            Inventory inv = PackInventory;
            if (_flash != null && (inv == null || _flash.Count <= 0 || !inv.Stacks.Contains(_flash)))
            {
                _flash = null;
                FlashlightOn = false;
            }
            if (FlashlightOn) return;

            ItemStack best = null;
            if (inv != null)
            {
                List<ItemStack> stacks = inv.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                {
                    ItemStack s = stacks[i];
                    if (s.Def == null || s.Count <= 0 || s.Def.Gear != GearKind.Flashlight) continue;
                    if (best == null || s.Condition > best.Condition) best = s;
                }
            }
            if (best != _flash)
            {
                _flash = best;
                LoadFlashCharge();
            }
        }

        private void LoadFlashCharge()
        {
            _flashWritten = _flash != null ? _flash.Condition : 0;
            _flashCharge = _flashWritten;
        }

        private void DrainNightVision(float dt)
        {
            if (!NightVisionOn || _nvgDef == null) return;
            _nvgCharge = Mathf.Max(0f, _nvgCharge - DrainPerSecond(_nvgDef) * dt);
            WriteWorn(ClothingSlot.Eyes, _nvgDef, _nvgCharge, ref _nvgWritten);
            if (_nvgCharge > 0f) return;
            NightVisionOn = false;
            Say("The night-vision battery died");
        }

        private void DrainFlashlight(float dt)
        {
            if (!FlashlightOn || _flash == null) return;
            _flashCharge = Mathf.Max(0f, _flashCharge - DrainPerSecond(_flash.Def) * dt);
            int c = Mathf.Clamp(Mathf.CeilToInt(_flashCharge), 0, Mathf.Max(0, _flash.Def.MaxCondition));
            if (c != _flashWritten)
            {
                bool step = c / NotifyStep != _flashWritten / NotifyStep || c == 0;
                _flash.Condition = c;
                _flashWritten = c;
                if (step && PackInventory != null) PackInventory.RaiseChanged();   // let the pack's charge bars catch up
            }
            if (_flashCharge > 0f) return;
            FlashlightOn = false;
            _packDirty = true;
            Say("The flashlight's battery died");
        }

        // Writes a worn device's charge back as a whole condition when it ticks over — silently, except
        // every NotifyStep points (and when it runs flat) so the UIs catch up now and then.
        private void WriteWorn(ClothingSlot slot, ItemDef def, float charge, ref int written)
        {
            int c = Mathf.Clamp(Mathf.CeilToInt(charge), 0, Mathf.Max(0, def.MaxCondition));
            if (c == written) return;
            bool step = c / NotifyStep != written / NotifyStep || c == 0;
            written = c;
            if (Clothing != null) Clothing.SetCondition(slot, c, step);
        }

        // Condition points a device burns per second of use (a full charge lasts ChargeSeconds).
        private static float DrainPerSecond(ItemDef def) =>
            def != null && def.ChargeSeconds > 0f ? def.MaxCondition / def.ChargeSeconds : 0f;

        private static float Fraction(float charge, ItemDef def) =>
            def != null && def.MaxCondition > 0 ? Mathf.Clamp01(charge / def.MaxCondition) : 0f;

        private void Publish()
        {
            HasNightVision = _nvgDef != null;
            if (!HasNightVision) NightVisionOn = false;
            NightVisionCharge01 = Fraction(_nvgCharge, _nvgDef);

            HasFlashlight = _flash != null;
            if (!HasFlashlight) FlashlightOn = false;
            FlashlightCharge01 = _flash != null ? Fraction(_flashCharge, _flash.Def) : 0f;

            HasGasMask = _maskDef != null;
            FilterCharge01 = Fraction(_filterCharge, _maskDef);
        }
        #endregion

        #region Toxic gas
        // The felt concentration eases after the air. Inside a cloud a worn mask burns filter (faster the
        // thicker the gas); bare-faced — or once the filter is spent — the gas turns the stomach, burns the
        // lungs and sets off a cough the dead can hear.
        private void UpdateGas(float dt)
        {
            float raw = ToxicZone.All.Count > 0 ? ToxicZone.ConcentrationAt(transform.position + Vector3.up * BreathHeight) : 0f;
            if (dt > 0f) GasConcentration = Mathf.Lerp(GasConcentration, raw, 1f - Mathf.Exp(-GasFollowRate * dt));
            if (raw <= 0f && GasConcentration < 0.002f) GasConcentration = 0f;
            _gasWarnTimer = Mathf.Max(0f, _gasWarnTimer - dt);

            bool wasBreathing = BreathingGas;
            BreathingGas = false;
            float c = GasConcentration;
            if (c < GasThreshold || dt <= 0f) return;

            if (_maskDef != null && _filterCharge > 0f)
            {
                _filterCharge = Mathf.Max(0f, _filterCharge - DrainPerSecond(_maskDef) * c * dt);
                WriteWorn(ClothingSlot.Mask, _maskDef, _filterCharge, ref _filterWritten);
                if (_filterCharge <= 0f) Say("The gas mask filter is spent!");
                else if (!_filterLowWarned && Fraction(_filterCharge, _maskDef) < FilterLowFraction)
                {
                    _filterLowWarned = true;
                    Say("Gas mask filter running low");
                }
                return;
            }

            BreathingGas = true;
            if (!wasBreathing) _coughTimer = Random.Range(0.6f, 1.5f);
            _stats.Sickness = Mathf.Min(CloneZConfig.SicknessMax, _stats.Sickness + GasSicknessPerSec * c * dt);
            _stats.EnvironmentDrain(GasHealthPerSec * c * dt, "choked on toxic gas");

            _coughTimer -= dt;
            if (_coughTimer <= 0f)
            {
                _coughTimer = Random.Range(CoughMinSeconds, CoughMaxSeconds) * Mathf.Lerp(1.3f, 0.6f, c);
                NoiseSystem.Emit(transform.position, CoughNoiseRadius);
                Coughed?.Invoke();
            }

            if (_gasWarnTimer <= 0f)
            {
                _gasWarnTimer = GasWarningSeconds;
                Say(_maskDef != null ? "The filter is spent \u2014 the gas is getting through!" : "Toxic gas! You need a gas mask");
            }
        }
        #endregion

        private void Say(string message) => Notice?.Invoke(message);
    }
}
