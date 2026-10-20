using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The player's carried items plus the currently-equipped melee weapon. Using an
    /// item eats/drinks/heals via <see cref="PlayerStats"/>, or equips it by swapping
    /// <see cref="PlayerCombat"/> values. Weight beyond a comfortable load slows the
    /// player down (encumbrance).
    ///
    /// The pack is arranged on a grid (<see cref="InventoryLayout"/>): the survivor's own pockets,
    /// plus one section for each worn container — a chest rig, a thigh bag and a backpack — each
    /// with its own cells and weight cap. Taking a bag off spills its contents into the remaining
    /// space; anything that no longer fits anywhere is dropped at the survivor's feet.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerInventory : MonoBehaviour
    {
        /// <summary>The pockets' grid (the survivor's base carry section).</summary>
        public const int PocketsWidth = 6, PocketsHeight = 4;

        // Worn slots that can hold a container, in the order their sections are listed: a jacket's and a pair
        // of trousers' own pockets (DayZ: the outfit is the loadout), the armour's pouches, the rig, the thigh
        // bag, the backpack, and last the secure pouch whose contents survive death.
        private static readonly ClothingSlot[] s_ContainerSlots =
            { ClothingSlot.Torso, ClothingSlot.Legs, ClothingSlot.Armor, ClothingSlot.Rig, ClothingSlot.Thigh, ClothingSlot.Back, ClothingSlot.Pouch };

        /// <summary>The secure pouch's grid section (null when none is worn).</summary>
        public GridSection PouchSection
        {
            get
            {
                InventoryLayout layout = Layout;
                if (layout == null) return null;
                for (int i = 0; i < layout.Sections.Count; i++)
                    if (layout.Sections[i].Slot == ClothingSlot.Pouch && layout.Sections[i].Source != null) return layout.Sections[i];
                return null;
            }
        }

        /// <summary>The survivor's carried inventory of items and containers.</summary>
        public Inventory Inventory { get; private set; }
        /// <summary>The currently equipped melee or ranged weapon definition.</summary>
        public ItemDef EquippedWeapon { get; private set; }

        /// <summary>The grid the pack is arranged on.</summary>
        public InventoryLayout Layout => Inventory != null ? Inventory.Layout : null;

        /// <summary>The survivor's own pockets (always the first section).</summary>
        public GridSection Pockets => _pockets;

        /// <summary>Raised when the inventory changes, so UI can update.</summary>
        public event System.Action Changed;

        /// <summary>A short message for the player ("No room for Hammer — dropped it").</summary>
        public event System.Action<string> Notice;

        private PlayerStats _stats;
        private PlayerCombat _combat;
        private PlayerSkills _skills;
        private PlayerWounds _wounds;
        private PlayerPanic _panic;
        private int _lastStrengthLevel = -1;
        private GridSection _pockets;
        private bool _overflowPending;

        private PlayerWounds Wounds => _wounds != null ? _wounds : (_wounds = GetComponent<PlayerWounds>());
        private PlayerPanic Panic => _panic != null ? _panic : (_panic = GetComponent<PlayerPanic>());

        private PlayerFoodSickness _nutrition;
        private PlayerFoodSickness Nutrition => _nutrition != null ? _nutrition : (_nutrition = GetComponent<PlayerFoodSickness>());
        private PlayerSkills Skills => _skills != null ? _skills : (_skills = GetComponent<PlayerSkills>());

        private PlayerClothing _clothing;
        private PlayerClothing Clothing => _clothing != null ? _clothing : (_clothing = GetComponent<PlayerClothing>());

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _combat = GetComponent<PlayerCombat>();
            Inventory = new Inventory(CloneZConfig.PlayerCarryCapacity);
            _pockets = new GridSection(GameSession.SurvivorName + "'s Inventory", PocketsWidth, PocketsHeight, CloneZConfig.PlayerCarryCapacity);
            Inventory.SetLayout(new InventoryLayout(_pockets));
            Inventory.Changed += OnInventoryChanged;
            if (Clothing != null) Clothing.Changed += RebuildSections;
            ItemDatabase.EnsureInit();
        }

        private void OnDestroy()
        {
            if (Inventory != null) Inventory.Changed -= OnInventoryChanged;
            if (_clothing != null) _clothing.Changed -= RebuildSections;
        }

        private void OnInventoryChanged()
        {
            Changed?.Invoke();
            if (_overflowPending || Inventory.Layout == null) return;
            List<ItemStack> stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (!stacks[i].IsPlaced) { _overflowPending = true; break; }
        }

        // Strength raises how much the survivor can haul (PZ maxWeight scales with Strength); traits, artifacts and
        // exo-frames bend it further, so the pockets' cap is re-derived whenever any of them changes.
        private float _lastCapacityKey = -1f;
        private void Update()
        {
            if (_skills == null) _skills = GetComponent<PlayerSkills>();
            if (_skills == null) return;
            int strength = _skills.GetLevel(PlayerSkills.Skill.Strength);
            StatMods mods = PlayerModifiers.Current;
            float key = strength * 1000f + mods.CarryCapacity * 100f + mods.CarryBonusKg;
            if (strength == _lastStrengthLevel && Mathf.Abs(key - _lastCapacityKey) < 0.01f) return;
            _lastStrengthLevel = strength;
            _lastCapacityKey = key;
            RebuildSections();
        }

        // Stacks with no room on the grid (a forced add, or a bag that came off) are dropped at the
        // end of the frame — once every change in it has landed.
        private void LateUpdate()
        {
            if (!_overflowPending) return;
            _overflowPending = false;
            DropOverflow();
        }

        /// <summary>Re-derives the grid sections from what's worn: the pockets (sized by Strength) plus a
        /// section per worn container. Sections that survive keep their identity, so items stay put; a
        /// removed bag's items are re-placed into the remaining space (or dropped next frame).</summary>
        public void RebuildSections()
        {
            InventoryLayout layout = Inventory != null ? Inventory.Layout : null;
            if (layout == null) return;

            float strengthMult = _skills != null ? _skills.CarryCapacityMult : 1f;
            StatMods mods = PlayerModifiers.Current;
            _pockets.Name = GameSession.SurvivorName + "'s Inventory";
            _pockets.Capacity = Mathf.Max(2f, CloneZConfig.PlayerCarryCapacity * strengthMult * mods.CarryCapacity + mods.CarryBonusKg);

            var next = new List<GridSection>(4) { _pockets };
            PlayerClothing clothing = Clothing;
            if (clothing != null)
            {
                for (int i = 0; i < s_ContainerSlots.Length; i++)
                {
                    ClothingSlot slot = s_ContainerSlots[i];
                    ItemDef worn = clothing.GetWorn(slot);
                    if (worn == null || !worn.IsWornContainer) continue;

                    GridSection keep = null;
                    for (int k = 0; k < layout.Sections.Count; k++)
                        if (layout.Sections[k].Source == worn && layout.Sections[k].Slot == slot) { keep = layout.Sections[k]; break; }
                    next.Add(keep ?? new GridSection(worn.DisplayName, worn.GridW, worn.GridH, worn.ContainerCapacity, false, worn, slot));
                }
            }

            bool same = next.Count == layout.Sections.Count;
            for (int i = 0; same && i < next.Count; i++) same = ReferenceEquals(next[i], layout.Sections[i]);

            layout.Sections.Clear();
            layout.Sections.AddRange(next);
            float total = 0f;
            for (int i = 0; i < next.Count; i++) total += next[i].Capacity;
            Inventory.MaxWeight = total;

            if (!same && layout.Validate().Count > 0) _overflowPending = true;
            Changed?.Invoke();
        }

        // Anything still without a spot gets one more chance, then lands on the floor.
        private void DropOverflow()
        {
            InventoryLayout layout = Inventory.Layout;
            if (layout == null) return;

            List<ItemStack> spill = null;
            List<ItemStack> stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (!stacks[i].IsPlaced) (spill ?? (spill = new List<ItemStack>())).Add(stacks[i]);
            if (spill == null) return;

            for (int i = 0; i < spill.Count; i++)
            {
                ItemStack st = spill[i];
                if (layout.AutoPlace(st, checkWeight: true) || layout.AutoPlace(st, checkWeight: false)) continue;
                string name = st.Def != null ? st.Def.DisplayName : "item";
                Drop(st, st.Count);
                Notice?.Invoke("No room for " + name + " \u2014 dropped it");
            }
            if (EquippedWeapon != null && EquippedStack == null) EquipBareHands();
            Inventory.RaiseChanged();
        }

        /// <summary>Carried weight, minus a discount for the weapon in hand — an equipped weapon
        /// weighs less on the carry total (PZ-style), easing encumbrance. Never negative.</summary>
        public float Weight
        {
            get
            {
                float w = Inventory.CurrentWeight;
                if (EquippedWeapon != null)
                    w -= EquippedWeapon.Weight * CloneZConfig.EquippedWeaponWeightReduction;
                return Mathf.Max(0f, w);
            }
        }
        /// <summary>The maximum weight the inventory can carry, based on capacity of sections.</summary>
        public float Capacity => Inventory.MaxWeight;
        /// <summary>The current load factor (weight / capacity) of the pack.</summary>
        public float Load => Capacity > 0f ? Weight / Capacity : 0f;

        /// <summary>1.0 when lightly loaded, easing toward EncumbranceMinMultiplier as capacity fills.</summary>
        public float MoveSpeedMultiplier
        {
            get
            {
                float load = Load;
                if (load <= CloneZConfig.EncumbranceStart) return 1f;
                float t = Mathf.InverseLerp(CloneZConfig.EncumbranceStart, 1f, Mathf.Min(load, 1f));
                return Mathf.Lerp(1f, CloneZConfig.EncumbranceMinMultiplier, t);
            }
        }

        /// <summary>Eats/drinks/heals, treats wounds, or equips a weapon.</summary>
        public void Use(ItemStack stack)
        {
            if (stack == null || stack.Count <= 0) return;
            ItemDef def = stack.Def;

            // Items a feature module knows how to use (bolts, splints, blood bags, books, artifacts...).
            if (def.CustomUse && ItemActions.TryUse(this, stack))
            {
                Changed?.Invoke();
                return;
            }

            // Batteries, filters and the torch are handled by the gear rig (recharge / swap / switch on).
            if (def.IsRefill || def.Gear == GearKind.Flashlight)
            {
                var gear = GetComponent<PlayerGear>();
                if (gear != null && gear.TryUseItem(stack)) Changed?.Invoke();
                return;
            }

            if (def.IsClothing) { WearFromStack(stack); return; }
            if (def.IsEquippable) { PromoteStack(stack); Equip(def); return; }
            if (!def.IsConsumable && !def.IsMedical) return;

            // A sealed tin has to be opened first — cleanly with a can opener, or with a sharp
            // knife (which blunts a touch). No opener and no blade means no meal.
            if (def.IsCanned && !TryOpenCan()) return;

            // Apply every effect the item carries; only spend it if something actually helped
            // (so you can't waste a bandage when there's nothing to bandage).
            bool did = false;
            if (def.HungerRestore > 0f) { _stats.Eat(def.HungerRestore * NutritionMultiplier(stack)); did = true; }
            if (def.ThirstRestore > 0f) { _stats.Drink(def.ThirstRestore * FreshnessMultiplier(stack.State)); did = true; }
            if (def.HealthRestore > 0f) { _stats.Heal(def.HealthRestore); did = true; }
            if (def.InfectionReduce > 0f) { _stats.ReduceInfection(def.InfectionReduce); did = true; }

            // Eating spoiled or raw-hazardous food can bring on food poisoning.
            if (did && def.IsFood && Nutrition != null) Nutrition.OnAte(def, stack.State);

            if (def.IsMedical)
            {
                if (Wounds != null)
                {
                    // A full kit dresses, disinfects and speeds the worst wound in one go.
                    if (def.StopsBleeding && def.Disinfects && def.CuresWoundInfection)
                        did |= Wounds.FirstAid();
                    else if (def.StopsBleeding)
                        did |= Wounds.ApplyBandage(def.SterileBandage);

                    if (def.Disinfects) did |= Wounds.Disinfect();
                    if (def.CuresWoundInfection) did |= Wounds.TreatInfection();
                    if (def.PainRelief > 0f) did |= Wounds.RelievePain(def.PainRelief);
                }

                // Beta blockers calm the nerves — only spent if there's panic to quell.
                if (def.CalmsPanic && Panic != null) did |= Panic.TakeBetaBlockers();
            }

            // A heavy bleed soaks straight through a bandage: say so, rather than silently doing nothing.
            if (!did && def.StopsBleeding && Wounds != null && Wounds.HasHeavyBleed)
                Notice?.Invoke("A bandage won't hold heavy bleeding \u2014 use a tourniquet or haemostatic gauze.");

            if (did)
            {
                if (def.HungerRestore > 0f) Sfx.PlayUI(SfxIds.Eat, 0.6f, Random.Range(0.9f, 1.1f));
                else if (def.ThirstRestore > 0f) Sfx.PlayUI(SfxIds.Drink, 0.6f, Random.Range(0.9f, 1.1f));

                // A raw drink of tainted (rain / barrel) water can turn the stomach.
                if (def.WaterTainted && def.HoldsWater && Nutrition != null) Nutrition.OnDrankTainted();

                Inventory.Remove(stack, 1);

                // Opening a tin leaves the empty can behind as scrap.
                if (def.IsCanned)
                {
                    ItemDef tin = ItemDatabase.Get("tincan");
                    if (tin != null) Inventory.Add(tin, 1, enforceWeight: false);
                }

                // A drained bottle is kept — it can be refilled at a tap or a rain barrel.
                if (def.IsWaterContainer && def.HoldsWater && !string.IsNullOrEmpty(def.EmptyId))
                {
                    ItemDef empty = ItemDatabase.Get(def.EmptyId);
                    if (empty != null) Inventory.Add(empty, 1, enforceWeight: false);
                }
            }
        }

        /// <summary>How much of a food's hunger value survives its spoilage and cooking. Fresh food is
        /// full value; stale/rotten feeds less; a skilled cook wrings more from a cooked dish.</summary>
        private float NutritionMultiplier(ItemStack stack)
        {
            float mult = FreshnessMultiplier(stack.State);
            if (stack.Def.IsCookedFood && Skills != null) mult *= Skills.CookingNutritionMult;
            return mult;
        }

        private static float FreshnessMultiplier(FoodState state)
        {
            switch (state)
            {
                case FoodState.Stale: return CloneZConfig.FoodStaleHungerMult;
                case FoodState.Rotten: return CloneZConfig.FoodRottenHungerMult;
                default: return 1f;
            }
        }

        #region Cooking
        /// <summary>True when the survivor is standing close enough to a lit stove to cook.</summary>
        public bool CanCookHere() => CookingStation.InRange(transform.position, CloneZConfig.CookStationRange);

        /// <summary>Cooks one unit of a raw ingredient at a nearby stove, turning it into its cooked
        /// form (safe and more filling) and training the Cooking skill. Returns true if it cooked.</summary>
        public bool TryCook(ItemStack stack)
        {
            if (stack == null || stack.Count <= 0) return false;
            ItemDef def = stack.Def;
            if (!def.Cookable || string.IsNullOrEmpty(def.CookedId)) return false;
            if (!CanCookHere()) return false;

            ItemDef cooked = ItemDatabase.Get(def.CookedId);
            if (cooked == null) return false;

            Inventory.Remove(stack, 1);
            Inventory.Add(cooked, 1, enforceWeight: false);        // a fresh, hot serving
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Cooking, CloneZConfig.XpCookingPerItem);
            Changed?.Invoke();
            return true;
        }
        #endregion

        #region Water
        /// <summary>True when the pack holds at least one empty, fillable bottle.</summary>
        public bool HasEmptyBottle()
        {
            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemDef d = stacks[i].Def;
                if (d != null && d.IsWaterContainer && !d.HoldsWater && !string.IsNullOrEmpty(d.FillCleanId))
                    return true;
            }
            return false;
        }

        /// <summary>Fills one empty bottle with water — clean from a tap, tainted from a rain barrel.
        /// Returns true if a bottle was filled.</summary>
        public bool FillOneBottle(bool tainted)
        {
            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemDef d = stacks[i].Def;
                if (d == null || !d.IsWaterContainer || d.HoldsWater) continue;

                string fillId = tainted ? d.FillTaintedId : d.FillCleanId;
                ItemDef filled = ItemDatabase.Get(fillId);
                if (filled == null) continue;

                Inventory.Remove(stacks[i], 1);
                Inventory.Add(filled, 1, enforceWeight: false);
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>True when standing close enough to a stove to boil water (the same heat source as cooking).</summary>
        public bool CanBoilHere() => CookingStation.InRange(transform.position, CloneZConfig.CookStationRange);

        /// <summary>Boils one unit of tainted water at a nearby stove, turning it clean. Returns true if it purified.</summary>
        public bool TryPurify(ItemStack stack)
        {
            if (stack == null || stack.Count <= 0) return false;
            ItemDef def = stack.Def;
            if (!def.IsWaterContainer || string.IsNullOrEmpty(def.PurifiedId)) return false;
            if (!CanBoilHere()) return false;

            ItemDef clean = ItemDatabase.Get(def.PurifiedId);
            if (clean == null) return false;

            Inventory.Remove(stack, 1);
            Inventory.Add(clean, 1, enforceWeight: false);
            Changed?.Invoke();
            return true;
        }
        #endregion

        /// <summary>Puts on a garment: wears it and hands whatever it replaced back to the bag. Wearing a
        /// twin of what's already on (a fresher hoodie, goggles with a full battery) swaps the two.</summary>
        private void WearFromStack(ItemStack stack)
        {
            if (Clothing == null || stack == null) return;
            ItemDef def = stack.Def;
            PlayerClothing.WornPiece twin = Clothing.GetPiece(def.ClothingSlot);
            bool swapTwin = twin.Exists && twin.Def == def;
            if (swapTwin) Clothing.Remove(def.ClothingSlot);   // Equip ignores the same item, so take the twin off first

            PlayerClothing.WornPiece prev = Clothing.Equip(def, stack.Condition, stack.TimesRepaired, stack.Extra);
            if (swapTwin) prev = twin;
            Inventory.Remove(stack, 1);                       // it's on your back now, not in the bag
            if (prev.Exists)                                  // the piece it replaced drops into the bag, wear and all
                Inventory.Add(prev.Def, 1, enforceWeight: false,
                    condition: prev.Def.IsDamageable ? prev.Condition : -1, timesRepaired: prev.TimesRepaired, extra: prev.Extra);
            Changed?.Invoke();
        }

        /// <summary>Takes the garment off a slot and returns it to the bag. Returns true if something came off.</summary>
        public bool TakeOff(ClothingSlot slot)
        {
            if (Clothing == null) return false;
            PlayerClothing.WornPiece removed = Clothing.Remove(slot);
            if (!removed.Exists) return false;
            Inventory.Add(removed.Def, 1, enforceWeight: false,
                condition: removed.Def.IsDamageable ? removed.Condition : -1, timesRepaired: removed.TimesRepaired, extra: removed.Extra);
            Changed?.Invoke();
            return true;
        }

        public void Equip(ItemDef weapon)
        {
            if (_combat == null || weapon == null || !weapon.IsEquippable) return;
            EquippedWeapon = weapon;            if (weapon.IsFirearm)
            {
                _combat.EquipFirearm(weapon);
                Changed?.Invoke();
                return;
            }

            _combat.EquipMeleeMode();
            _combat.WeaponDamage = weapon.WeaponDamage;
            _combat.WeaponRange = weapon.WeaponRange;
            _combat.WeaponArc = weapon.WeaponArc;
            _combat.SwingCooldown = weapon.SwingCooldown;
            _combat.SwingKnockback = weapon.Knockback;
            Changed?.Invoke();
        }

        #region Condition / durability
        /// <summary>The pack stack backing the equipped weapon (weapons are unique, MaxStack 1), or null.</summary>
        public ItemStack EquippedStack
        {
            get
            {
                if (EquippedWeapon == null) return null;
                var stacks = Inventory.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                    if (stacks[i].Def == EquippedWeapon) return stacks[i];
                return null;
            }
        }

        // The weapon in hand is the first pack stack of its kind (see EquippedStack), so equipping a
        // particular one — the sharper of two knives — first moves it ahead of its twins.
        private void PromoteStack(ItemStack stack)
        {
            List<ItemStack> stacks = Inventory.Stacks;
            int at = stacks.IndexOf(stack);
            for (int i = 0; i < at; i++)
            {
                if (stacks[i].Def != stack.Def) continue;
                stacks.RemoveAt(at);
                stacks.Insert(i, stack);
                return;
            }
        }

        /// <summary>Condition of the equipped weapon, 0..1 (1 when bare-handed / indestructible).</summary>
        public float EquippedCondition01
        {
            get { ItemStack s = EquippedStack; return s != null ? s.Condition01 : 1f; }
        }

        /// <summary>Rolls whether the equipped firearm jams on this shot — likelier the more worn it is.</summary>
        public bool EquippedFirearmJams()
        {
            ItemStack s = EquippedStack;
            if (s == null || !s.Def.IsDamageable) return false;
            float c01 = s.Condition01;
            if (c01 >= CloneZConfig.FirearmJamConditionThreshold) return false;
            float t = 1f - c01 / CloneZConfig.FirearmJamConditionThreshold;   // 0 at threshold .. 1 at broken
            return Random.value < CloneZConfig.FirearmJamChanceAtBroken * t;
        }

        /// <summary>Applies one wear roll to the equipped weapon after a swing or a shot. With a
        /// 1-in-N chance its condition drops a point; at zero the weapon breaks and the survivor
        /// falls back to bare hands.</summary>
        public void WearEquippedWeapon() => Wear(EquippedStack);

        /// <summary>Applies one wear roll to a carried tool (hammer, trowel, watering can) after a
        /// completed job. A broken tool is removed; if it was also in hand, we revert to bare hands.</summary>
        public void WearTool(string id)
        {
            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Def != null && stacks[i].Def.Id == id) { Wear(stacks[i]); return; }
        }

        // The shared wear roll: chance-gated condition loss, with breakage at zero.
        private void Wear(ItemStack stack)
        {
            if (stack == null || stack.Def == null || !stack.Def.IsDamageable) return;
            if (Random.Range(0, Mathf.Max(1, stack.Def.ConditionLossChance)) != 0) return;   // survived this use

            stack.Condition = Mathf.Max(0, stack.Condition - 1);
            if (stack.Condition > 0) { Changed?.Invoke(); return; }

            // Spent: remove it, and drop to fists if the broken item was the one in hand.
            bool wasEquipped = EquippedWeapon == stack.Def;
            Inventory.Remove(stack, 1);
            if (wasEquipped) EquipBareHands();
            else Changed?.Invoke();
        }

        /// <summary>Drops the equipped weapon back to bare fists (used when a weapon breaks).</summary>
        public void EquipBareHands()
        {
            EquippedWeapon = null;
            if (_combat == null) { Changed?.Invoke(); return; }
            _combat.EquipMeleeMode();               // leaves ranged mode and hands any loaded rounds back
            _combat.WeaponDamage = CloneZConfig.BareHandDamage;
            _combat.WeaponRange = CloneZConfig.BareHandRange;
            _combat.WeaponArc = CloneZConfig.BareHandArc;
            _combat.SwingCooldown = CloneZConfig.BareHandCooldown;
            _combat.SwingKnockback = CloneZConfig.BareHandKnockback;
            Changed?.Invoke();
        }

        // ---- Repair (patch worn gear back up; grounded in PZ's diminishing repair) ----

        /// <summary>The material a repair of this item consumes: thread for clothing, duct tape for
        /// everything else. (Clothing also needs a needle in the pack — a reusable tool.)</summary>
        private static string RepairMaterialFor(ItemDef def) => def != null && def.IsClothing ? "thread" : "ducttape";

        /// <summary>The condition a repair of this stack could reach right now — its reachable cap
        /// falls with each prior repair (so a patched item never fully returns to pristine) and rises
        /// a little with Carpentry. Returned as an absolute value in 0..MaxCondition.</summary>
        private int RepairCap(ItemStack stack)
        {
            int carpentry = Skills != null ? Skills.GetLevel(PlayerSkills.Skill.Carpentry) : 0;
            float capFrac = CloneZConfig.RepairCapBase
                - CloneZConfig.RepairCapDecayPerRepair * stack.TimesRepaired
                + CloneZConfig.RepairCapCarpentryBonus * carpentry;
            capFrac = Mathf.Clamp(capFrac, CloneZConfig.RepairCapFloor, 1f);
            return Mathf.RoundToInt(stack.Def.MaxCondition * capFrac);
        }

        /// <summary>True when a stack is worn gear that a repair could still improve right now — it's
        /// below its next repair cap and the material (and, for clothing, a needle) is on hand.</summary>
        public bool CanRepair(ItemStack stack)
        {
            if (stack == null || stack.Def == null || !stack.Def.IsDamageable) return false;
            if (stack.Def.UsesCharge) return false;                        // a device is recharged, not mended
            if (stack.Condition >= RepairCap(stack)) return false;         // already at (or past) the reachable cap
            if (!Has(RepairMaterialFor(stack.Def))) return false;
            if (stack.Def.IsClothing && !Has("needle")) return false;      // no needle, no mending
            return true;
        }

        /// <summary>Patches a worn item back up: spends one unit of the repair material (duct tape, or
        /// thread for clothing) to raise its condition toward — but never fully back to — its
        /// diminishing cap. Returns true if it repaired.</summary>
        public bool TryRepair(ItemStack stack)
        {
            if (!CanRepair(stack)) return false;

            int cap = RepairCap(stack);
            int restore = Mathf.Max(1, Mathf.RoundToInt(stack.Def.MaxCondition * CloneZConfig.RepairRestoreFraction));
            int newCond = Mathf.Min(cap, stack.Condition + restore);
            if (newCond <= stack.Condition) return false;

            if (Consume(RepairMaterialFor(stack.Def), 1) <= 0) return false;   // the material must actually leave the bag
            stack.Condition = newCond;
            stack.TimesRepaired++;
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Carpentry, CloneZConfig.XpRepairPerFix);
            Changed?.Invoke();
            return true;
        }

        // ---- Opening tins (a canned meal needs a can opener or a sharp blade) ----

        /// <summary>Opens a sealed tin so it can be eaten. A can opener does it cleanly; a sharp
        /// knife works too but takes a little wear. Returns false when neither is on hand.</summary>
        private bool TryOpenCan()
        {
            if (Has("canopener")) return true;
            ItemStack blade = FindSharpBlade();
            if (blade == null) return false;
            WearTool(blade.Def.Id);   // sawing a lid open costs the blade a little
            return true;
        }

        // The first sharp blade in the pack (any Blade-skill melee weapon) — good enough to open a tin.
        private ItemStack FindSharpBlade()
        {
            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemDef d = stacks[i].Def;
                if (d != null && d.IsWeapon && d.WeaponSkill == PlayerSkills.Skill.Blade) return stacks[i];
            }
            return null;
        }
        #endregion

        #region Ammo
        /// <summary>Total rounds of a given ammo id currently carried.</summary>
        public int AmmoReserve(string ammoId)
        {
            if (string.IsNullOrEmpty(ammoId)) return 0;
            int n = 0;
            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Def.Id == ammoId) n += stacks[i].Count;
            return n;
        }

        /// <summary>Removes up to <paramref name="max"/> rounds from the pack; returns how many were taken.</summary>
        public int TakeAmmo(string ammoId, int max)
        {
            if (string.IsNullOrEmpty(ammoId) || max <= 0) return 0;
            int taken = 0;
            var stacks = Inventory.Stacks;
            for (int i = stacks.Count - 1; i >= 0 && taken < max; i--)
            {
                if (stacks[i].Def.Id != ammoId) continue;
                int t = Mathf.Min(max - taken, stacks[i].Count);
                Inventory.Remove(stacks[i], t);
                taken += t;
            }
            return taken;
        }

        /// <summary>Returns unspent rounds to the pack (e.g. when swapping away from a loaded gun).</summary>
        public void ReturnAmmo(string ammoId, int count)
        {
            if (count <= 0) return;
            ItemDef def = ItemDatabase.Get(ammoId);
            if (def != null) Inventory.Add(def, count, enforceWeight: false);
        }
        #endregion

        #region Materials (crafting)
        /// <summary>Total units of a given item id currently carried.</summary>
        public int Count(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            int n = 0;
            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Def.Id == id) n += stacks[i].Count;
            return n;
        }

        /// <summary>True when the pack holds at least <paramref name="count"/> of the item.</summary>
        public bool Has(string id, int count = 1) => Count(id) >= count;

        /// <summary>Removes up to <paramref name="count"/> units of an item id; returns how many were removed.</summary>
        public int Consume(string id, int count)
        {
            if (string.IsNullOrEmpty(id) || count <= 0) return 0;
            int removed = 0;
            var stacks = Inventory.Stacks;
            for (int i = stacks.Count - 1; i >= 0 && removed < count; i--)
            {
                if (stacks[i].Def.Id != id) continue;
                int t = Mathf.Min(count - removed, stacks[i].Count);
                Inventory.Remove(stacks[i], t);
                removed += t;
            }
            return removed;
        }
        #endregion

        /// <summary>Picks a ground item up into the pack, taking as much as the carry weight allows (and
        /// no more than <paramref name="max"/>); any overflow stays on the floor. Perishable age and
        /// damageable wear ride along so the item is exactly what was dropped. Returns the number of
        /// units actually taken.</summary>
        /// <summary>Raised when the local survivor takes from / leaves on the ground (the netcode mirrors it).</summary>
        public static event System.Action<ItemDef, int, Vector3> GroundTaken;
        public static event System.Action<ItemDef, int, Vector3, ItemExtra> GroundDropped;

        public int PickUp(WorldItem item, int max = int.MaxValue)
        {
            if (item == null || item.Def == null || max <= 0) return 0;
            int moved = Inventory.Add(item.Def, Mathf.Min(item.Count, max), enforceWeight: true,
                spawnHours: item.Def.Perishable ? item.SpawnHours : float.NaN,
                condition: item.Def.IsDamageable ? item.Condition : -1,
                timesRepaired: item.TimesRepaired, extra: item.Extra);
            if (moved > 0) { item.Take(moved); GroundTaken?.Invoke(item.Def, moved, item.Position); }
            return moved;
        }

        /// <summary>Picks a ground item up onto a chosen grid spot (a drag from the floor panel): onto a
        /// matching stack covering that cell, or as a new stack there. Weight is enforced. Returns the
        /// number of units taken.</summary>
        public int PickUpAt(WorldItem item, int max, GridSection section, int x, int y, bool rotated)
        {
            InventoryLayout layout = Inventory.Layout;
            if (item == null || item.Def == null || max <= 0 || layout == null) return 0;
            ItemDef def = item.Def;
            int want = Mathf.Min(item.Count, max);
            int moved;

            ItemStack there = layout.StackAt(section, x, y);
            if (there != null)
            {
                if (there.Def != def || !ItemExtra.CanStack(there.Extra, item.Extra)) return 0;
                moved = Mathf.Min(want, def.MaxStack - there.Count);
                if (def.Weight > 0f)
                {
                    float room = Inventory.MaxWeight - Inventory.CurrentWeight;
                    if (!section.Unlimited) room = Mathf.Min(room, section.Capacity - layout.WeightIn(section));
                    moved = Mathf.Min(moved, Mathf.FloorToInt((room + 1e-4f) / def.Weight));
                }
                if (moved <= 0) return 0;
                if (def.Perishable) there.SpawnHours = Mathf.Min(there.SpawnHours, item.SpawnHours);
                there.Count += moved;
                Inventory.RaiseChanged();
            }
            else
            {
                moved = Inventory.AddAt(def, want, section, x, y, rotated, enforceWeight: true,
                    spawnHours: def.Perishable ? item.SpawnHours : float.NaN,
                    condition: def.IsDamageable ? item.Condition : -1,
                    timesRepaired: item.TimesRepaired, extra: item.Extra);
            }
            if (moved > 0) { item.Take(moved); GroundTaken?.Invoke(def, moved, item.Position); }
            return moved;
        }

        /// <summary>Drops a stack onto the ground as a rendered item the player can walk up to and
        /// pick back up. Perishable age and damageable wear ride along so the round-trip changes nothing.</summary>
        public void Drop(ItemStack stack, int count)
        {
            if (stack == null) return;
            count = Mathf.Min(count, stack.Count);
            if (count <= 0) return;

            Vector3 p = transform.position + transform.forward * 0.7f;
            WorldItem.Spawn(null, p, stack.Def, count,
                stack.Def.Perishable ? stack.SpawnHours : float.NaN,
                stack.Def.IsDamageable ? stack.Condition : -1,
                stack.TimesRepaired, stack.Extra);
            GroundDropped?.Invoke(stack.Def, count, p, stack.Extra);
            Inventory.Remove(stack, count);
        }

        /// <summary>Empties the whole pack onto the ground as separate rendered items, scattered in a
        /// ring around the survivor so they don't pile on one spot. Worn clothing stays on; if the
        /// equipped weapon is among the dropped items the survivor reverts to bare hands.</summary>
        public void DropAll()
        {
            var live = Inventory.Stacks;
            if (live.Count == 0) return;

            var snapshot = new List<ItemStack>(live);   // dropping mutates the live list
            bool droppedEquipped = false;
            int n = snapshot.Count;

            for (int i = 0; i < n; i++)
            {
                ItemStack s = snapshot[i];
                if (s == null || s.Def == null) continue;
                if (EquippedWeapon != null && s.Def == EquippedWeapon) droppedEquipped = true;

                float ang = n <= 1 ? 0f : (i / (float)n) * Mathf.PI * 2f;
                Vector3 p = transform.position + transform.forward * 0.3f
                    + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * 0.95f;
                WorldItem.Spawn(null, p, s.Def, s.Count,
                    s.Def.Perishable ? s.SpawnHours : float.NaN,
                    s.Def.IsDamageable ? s.Condition : -1,
                    s.TimesRepaired, s.Extra);
                Inventory.Remove(s, s.Count);
            }

            if (droppedEquipped) EquipBareHands();
        }

        /// <summary>Moves the entire pack into a container. If the equipped weapon is transferred out
        /// the survivor reverts to bare hands (mirrors <see cref="DropAll"/>).</summary>
        public void StoreAllInto(Inventory dest)
        {
            if (dest == null) return;
            bool hadEquipped = EquippedWeapon != null;
            Inventory.TransferAllTo(dest, enforceWeight: false);
            if (hadEquipped && EquippedStack == null) EquipBareHands();   // the weapon left the pack
        }

        public void AddStarter(string id, int count)
        {
            ItemDef def = ItemDatabase.Get(id);
            if (def != null) Inventory.Add(def, count, enforceWeight: false);
        }

        /// <summary>Rebuilds the pack and re-equips the saved weapon from a saved game. Perishable
        /// stacks keep their saved age; a saved firearm's magazine is restored directly (its rounds
        /// were saved apart from the pack reserve, so nothing is double-counted).</summary>
        public void RestoreState(List<StackSave> items, string equippedWeaponId, int loadedAmmo)
        {
            Inventory.Clear();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    StackSave st = items[i];
                    ItemDef def = ItemDatabase.Get(st.Id);
                    if (def == null) continue;
                    Inventory.Add(def, st.Count, enforceWeight: false,
                        spawnHours: def.Perishable ? st.SpawnHours : float.NaN,
                        condition: def.RestoredCondition(st.Condition),
                        timesRepaired: st.TimesRepaired, extra: ItemExtra.Parse(st.Extra));
                }
            }

            EquippedWeapon = null;
            ItemDef weapon = string.IsNullOrEmpty(equippedWeaponId) ? null : ItemDatabase.Get(equippedWeaponId);
            if (weapon != null && weapon.IsEquippable && _combat != null)
            {
                EquippedWeapon = weapon;
                if (weapon.IsFirearm)
                {
                    _combat.RestoreFirearm(weapon, loadedAmmo);
                }
                else
                {
                    _combat.EquipMeleeMode();
                    _combat.WeaponDamage = weapon.WeaponDamage;
                    _combat.WeaponRange = weapon.WeaponRange;
                    _combat.WeaponArc = weapon.WeaponArc;
                    _combat.SwingCooldown = weapon.SwingCooldown;
                    _combat.SwingKnockback = weapon.Knockback;
                }
            }
            else if (_combat != null)
            {
                _combat.EquipMeleeMode();
            }

            Changed?.Invoke();
        }
    }
}
