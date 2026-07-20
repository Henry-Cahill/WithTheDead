using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The player's carried items plus the currently-equipped melee weapon. Using an
    /// item eats/drinks/heals via <see cref="PlayerStats"/>, or equips it by swapping
    /// <see cref="PlayerCombat"/> values. Weight beyond a comfortable load slows the
    /// player down (encumbrance).
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerInventory : MonoBehaviour
    {
        public Inventory Inventory { get; private set; }
        public ItemDef EquippedWeapon { get; private set; }

        public event System.Action Changed;

        private PlayerStats _stats;
        private PlayerCombat _combat;
        private PlayerSkills _skills;
        private PlayerWounds _wounds;
        private PlayerPanic _panic;
        private int _lastStrengthLevel = -1;

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
            Inventory.Changed += () => Changed?.Invoke();
            ItemDatabase.EnsureInit();
        }

        // Strength raises how much the survivor can haul (PZ maxWeight scales with Strength).
        private void Update()
        {
            if (_skills == null) _skills = GetComponent<PlayerSkills>();
            if (_skills == null) return;
            int strength = _skills.GetLevel(PlayerSkills.Skill.Strength);
            if (strength == _lastStrengthLevel) return;
            _lastStrengthLevel = strength;
            Inventory.MaxWeight = CloneZConfig.PlayerCarryCapacity * _skills.CarryCapacityMult;
            Changed?.Invoke();
        }

        public float Weight => Inventory.CurrentWeight;
        public float Capacity => Inventory.MaxWeight;
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

            if (def.IsClothing) { WearFromStack(stack); return; }
            if (def.IsEquippable) { Equip(def); return; }
            if (!def.IsConsumable && !def.IsMedical) return;

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

            if (did)
            {
                // A raw drink of tainted (rain / barrel) water can turn the stomach.
                if (def.WaterTainted && def.HoldsWater && Nutrition != null) Nutrition.OnDrankTainted();

                Inventory.Remove(stack, 1);

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

        /// <summary>Puts on a garment: wears it and hands whatever it replaced back to the bag.</summary>
        private void WearFromStack(ItemStack stack)
        {
            if (Clothing == null || stack == null) return;
            ItemDef def = stack.Def;
            ItemDef prev = Clothing.Equip(def);
            Inventory.Remove(stack, 1);                       // it's on your back now, not in the bag
            if (prev != null && prev != def)
                Inventory.Add(prev, 1, enforceWeight: false); // the piece it replaced drops into the bag
            Changed?.Invoke();
        }

        /// <summary>Takes the garment off a slot and returns it to the bag. Returns true if something came off.</summary>
        public bool TakeOff(ClothingSlot slot)
        {
            if (Clothing == null) return false;
            ItemDef removed = Clothing.Remove(slot);
            if (removed == null) return false;
            Inventory.Add(removed, 1, enforceWeight: false);
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

        /// <summary>Drops a stack into a sack at the player's feet so it can be reclaimed.</summary>
        public void Drop(ItemStack stack, int count)
        {
            if (stack == null) return;
            count = Mathf.Min(count, stack.Count);
            if (count <= 0) return;

            Vector3 p = transform.position + transform.forward * 0.7f;
            p.y = 0.25f;
            LootContainer.SpawnBag(p, stack.Def, count, stack.Def.IsDamageable ? stack.Condition : -1);
            Inventory.Remove(stack, count);
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
                        condition: def.IsDamageable && st.Condition > 0 ? st.Condition : -1);
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
