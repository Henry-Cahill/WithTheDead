using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    public enum ItemCategory { Food, Drink, Medical, Weapon, Firearm, Ammo, Material, Clothing, Misc }

    /// <summary>Where a piece of clothing is worn. One item per slot at a time. The first five are body
    /// areas (they take hits and dress the model); the rest carry gear: a backpack, a chest rig, a thigh
    /// bag, head-mounted optics (night vision), a face mask (gas mask), body armour (a vest or plate carrier)
    /// and the secure pouch that keeps its contents through death.</summary>
    public enum ClothingSlot { Head, Torso, Legs, Feet, Hands, Back, Rig, Thigh, Eyes, Mask, Armor, Pouch }

    /// <summary>What special job an item does beyond its category. Containers (backpacks, rigs, thigh
    /// bags) are worn and add a carry section; devices run down a charge stored in the stack's
    /// <see cref="ItemStack.Condition"/> (see <see cref="ItemDef.UsesCharge"/>); refills top them up.</summary>
    public enum GearKind { None, Container, NightVision, Flashlight, GasMask, Battery, Filter }

    /// <summary>Freshness of a perishable food item, from newly-found to spoiled.</summary>
    public enum FoodState { Fresh, Stale, Rotten }

    /// <summary>
    /// A static definition for a kind of item. Values are loosely grounded in
    /// Project Zomboid item data (weights in kg; food/thirst expressed as a
    /// fraction of the matching 0..1 stat). Definitions are shared and immutable;
    /// per-instance quantity lives in <see cref="ItemStack"/>.
    /// </summary>
    public class ItemDef
    {
        public string Id;
        public string DisplayName;
        public ItemCategory Category;
        public float Weight = 0.1f;     // kg per unit
        public int MaxStack = 1;
        public Color Tint = Color.white;

        // Consumable effects (applied once per unit used).
        public float HungerRestore;     // fraction removed from Hunger (0..1)
        public float ThirstRestore;     // fraction removed from Thirst (0..1)
        public float HealthRestore;     // points added to Health (0..100)
        public float InfectionReduce;   // points removed from Infection (0..100)

        // Food freshness & cooking. Perishable food rots over in-game hours (fresh -> stale ->
        // rotten); the per-instance age lives on ItemStack, driven by the world clock.
        public bool Perishable;          // decays over time; spoiled food feeds less and can sicken you
        public float ShelfLifeHours = 72f; // in-game hours from fresh to fully rotten
        public bool Cookable;            // can be cooked at a stove into CookedId
        public string CookedId;          // what this item becomes once cooked
        public bool IsCookedFood;        // already a cooked dish (Cooking skill boosts its nutrition)
        public bool RawHazard;           // eating it raw risks food poisoning even when fresh (raw meat)
        public bool IsCanned;            // a sealed tin — needs a can opener (or a knife) to get into

        // Nutrition — PZ B42 values (calories + macros in grams per unit) ripped from the item
        // scripts. Data only for now: not consumed by a metabolism system yet (gameplay still uses
        // HungerRestore); persisted so a future full-nutrition slice can read the real numbers.
        public float Calories;
        public float Carbohydrates;
        public float Proteins;
        public float Lipids;

        // Farming — a seed sown at a tilled plot grows into a crop over in-game hours.
        public bool IsSeed;              // can be planted at a farm plot
        public string CropId;            // which crop this seed grows (a FarmPlot crop key)

        // Water — a fillable vessel (empty or full) drunk to slake thirst, filled at a tap or a
        // rain barrel, and (for tainted rain water) boiled clean at a stove. See PlayerHydration.
        public bool IsWaterContainer;    // a bottle/canteen, whether empty or full
        public bool HoldsWater;          // currently full of water (drinkable)
        public bool WaterTainted;        // its water is unsafe — a raw drink risks a sick stomach
        public string EmptyId;           // what a full vessel becomes once drained
        public string FillCleanId;       // what an empty vessel becomes filled from a clean source
        public string FillTaintedId;     // what an empty vessel becomes filled from rain / a barrel
        public string PurifiedId;        // what tainted water becomes once boiled at a stove

        // Medical effects — act on the wound/pain model in PlayerWounds.
        public bool StopsBleeding;       // dresses the worst open wound (a bandage or a rag)
        public bool SterileBandage;      // a clean dressing (rags are dirty and risk sepsis)
        public bool Disinfects;          // cleans wounds so they won't fester
        public bool CuresWoundInfection; // a course of antibiotics clears sepsis
        public float PainRelief;         // 0..1 fraction of felt pain suppressed (painkillers)
        public bool CalmsPanic;          // beta blockers steady the nerves and hold panic down

        // Clothing — worn on a body slot; insulates against the cold and can turn a bite.
        public bool IsClothing;
        public ClothingSlot ClothingSlot;
        public float Insulation;         // 0..~0.15 warmth added when worn (dry; soaked clothes insulate less)
        public float BiteDefense;        // 0..1 chance this garment stops a bite landing on its area
        public float ScratchDefense;     // 0..1 chance it turns a scratch on its area
        public float WaterResistance;    // 0..1 keeps the rain off (a raincoat soaks far slower)

        // Melee weapon — equipping swaps the player's PlayerCombat values.
        public bool IsWeapon;
        public float WeaponDamage;
        public float WeaponRange;
        public float WeaponArc;
        public float SwingCooldown;
        public float Knockback;
        public bool TwoHanded;           // a heavy arm that occupies both hands (blocks an off-hand item)
        public PlayerSkills.Skill WeaponSkill = PlayerSkills.Skill.Blunt; // which melee skill this trains

        // Firearm — equipping enters ranged mode in PlayerCombat.
        public bool IsFirearm;
        public float FirearmDamage;      // per pellet, raw (the zombie applies the x1.5 received mult)
        public float FirearmRange;       // metres
        public int PelletsPerShot = 1;   // >1 for shotguns
        public int MagazineSize = 1;
        public float FireCooldown = 0.3f;
        public float ReloadTime = 1.8f;
        public float FirearmSpread;      // the gun's own base scatter (deg half-angle)
        public string AmmoId;            // item id this firearm consumes
        public float FirearmNoise = 30f; // metres of attraction a shot radiates

        // Condition & durability — damageable gear (weapons, tools, guns) wears with use and
        // eventually breaks, so loot stays a renewable need. The per-instance wear lives on
        // ItemStack.Condition; these describe the item type. Damageable items must be MaxStack = 1
        // (a unique condition can't be shared across a stack). Grounded in PZ's InventoryItem
        // Condition / ConditionLowerChanceOneIn (an axe wears far slower than a kitchen knife).
        public int MaxCondition;             // 0 = never wears; >0 = degrades from full down to 0 (breaks)
        public int ConditionLossChance = 10; // 1-in-N chance a use costs a point (higher N = tougher)

        // Gear — wearable containers, powered devices and their refills (schema v6 columns).
        public GearKind Gear;
        public float ContainerCapacity;      // kg a worn container holds (Gear == Container)
        public int GridW, GridH;             // the worn container's grid, in cells
        public float ChargeSeconds;          // real seconds of use a full battery / filter lasts (devices)
        public int SizeW, SizeH;             // footprint in inventory-grid cells (0 = derived, see InventoryLayout)

        /// <summary>A device whose <see cref="ItemStack.Condition"/> is its charge (battery or filter,
        /// 0..<see cref="MaxCondition"/>) rather than wear: it never breaks, can sit at 0 (flat / spent)
        /// and is topped up with a refill rather than repaired.</summary>
        public bool UsesCharge => Gear == GearKind.NightVision || Gear == GearKind.Flashlight || Gear == GearKind.GasMask;

        /// <summary>A battery or filter that tops up a device's charge when used.</summary>
        public bool IsRefill => Gear == GearKind.Battery || Gear == GearKind.Filter;

        /// <summary>Worn gear that opens its own section of the carry grid (backpack, chest rig, thigh bag).</summary>
        public bool IsWornContainer => Gear == GearKind.Container && IsClothing && ContainerCapacity > 0f;

        /// <summary>The condition to restore a saved instance with (for <see cref="Inventory.Add"/>):
        /// the saved value for damageable gear, or -1 (pristine) when none was recorded. Wear never
        /// legitimately saves at 0 (a worn-out item breaks), but a device's charge can — a flat battery
        /// must come back flat, not freshly charged.</summary>
        public int RestoredCondition(int saved) => IsDamageable && (saved > 0 || UsesCharge) ? saved : -1;

        // Optional flavour line shown at the foot of the tooltip.
        public string Description;

        /// <summary>Using this item is handled by a feature module (see <see cref="ItemActions"/>), not the base inventory.</summary>
        public bool CustomUse;
        /// <summary>The verb shown for <see cref="CustomUse"/> items ("Throw", "Read", "Wear on belt"); defaults to "Use".</summary>
        public string UseVerb;

        /// <summary>Extra tooltip lines from feature modules (ballistics, armour class, an artifact's known properties).
        /// Each provider returns its lines for a def, or null.</summary>
        public static readonly List<System.Func<ItemDef, string>> TooltipProviders = new List<System.Func<ItemDef, string>>();

        /// <summary>True if this item can be consumed (food, drink, or provides health benefits).</summary>
        public bool IsConsumable =>
            HungerRestore > 0f || ThirstRestore > 0f || HealthRestore > 0f || InfectionReduce > 0f;
        public bool IsMedical =>
            StopsBleeding || Disinfects || CuresWoundInfection || PainRelief > 0f || CalmsPanic;
        public bool IsEquippable => IsWeapon || IsFirearm || IsClothing;
        public bool IsUsable => IsConsumable || IsMedical || IsEquippable || IsRefill || Gear == GearKind.Flashlight || CustomUse;

        /// <summary>Gear that wears out with use and can break (weapons, firearms, tools).</summary>
        public bool IsDamageable => MaxCondition > 0;

        /// <summary>Anything eaten or drunk — the only things freshness applies to.</summary>
        public bool IsFood => Category == ItemCategory.Food || Category == ItemCategory.Drink;

        /// <summary>Builds the rich-text body shown when the item is hovered in the inventory.</summary>
        public string Tooltip()
        {
            var sb = new System.Text.StringBuilder(160);
            sb.Append("<b>").Append(DisplayName).Append("</b>\n");
            sb.Append("<size=13><color=#8b93a0>").Append(Category.ToString())
              .Append("    ").Append(Weight.ToString("0.##")).Append(" kg</color></size>");

            if (HungerRestore > 0f) StatLine(sb, "Hunger", "-" + Pct(HungerRestore));
            if (ThirstRestore > 0f) StatLine(sb, "Thirst", "-" + Pct(ThirstRestore));
            if (HealthRestore > 0f) StatLine(sb, "Heals", HealthRestore.ToString("0") + " HP");
            if (InfectionReduce > 0f) StatLine(sb, "Cures", InfectionReduce.ToString("0") + " infection");
            if (Perishable) StatLine(sb, "Keeps", "~" + Mathf.RoundToInt(ShelfLifeHours) + "h before spoiling");
            if (Cookable)
            {
                ItemDef cooked = ItemDatabase.Get(CookedId);
                StatLine(sb, "Cook", "at a stove \u2192 " + (cooked != null ? cooked.DisplayName : CookedId));
            }
            if (RawHazard) StatLine(sb, "<color=#e0a53c>Raw</color>", "cook it first \u2014 eating it raw risks illness");
            if (IsSeed)
            {
                ItemDef crop = ItemDatabase.Get(CropId);
                StatLine(sb, "Plant", "sow at a tilled plot \u2192 " + (crop != null ? crop.DisplayName : CropId));
            }
            if (IsWaterContainer && !HoldsWater) StatLine(sb, "Empty", "fill at a tap or rain barrel");
            if (WaterTainted) StatLine(sb, "<color=#e0a53c>Tainted</color>", "boil at a stove before drinking");
            if (StopsBleeding) StatLine(sb, "Bandage", SterileBandage ? "Sterile" : "Improvised");
            if (Disinfects) StatLine(sb, "Disinfects", "wound");
            if (CuresWoundInfection) StatLine(sb, "Antibiotic", "treats sepsis");
            if (PainRelief > 0f) StatLine(sb, "Pain relief", Pct(PainRelief));
            if (CalmsPanic) StatLine(sb, "Calms", "panic");

            if (IsWeapon)
            {
                StatLine(sb, "Damage", WeaponDamage.ToString("0.0"));
                StatLine(sb, "Reach", WeaponRange.ToString("0.0") + " m");
                StatLine(sb, "Speed", (1f / Mathf.Max(0.01f, SwingCooldown)).ToString("0.0") + " /s");
            }
            if (IsFirearm)
            {
                string dmg = PelletsPerShot > 1
                    ? FirearmDamage.ToString("0.0") + " x" + PelletsPerShot
                    : FirearmDamage.ToString("0.0");
                StatLine(sb, "Damage", dmg);
                StatLine(sb, "Range", FirearmRange.ToString("0") + " m");
                StatLine(sb, "Magazine", MagazineSize.ToString());
                ItemDef ammo = ItemDatabase.Get(AmmoId);
                StatLine(sb, "Ammo", ammo != null ? ammo.DisplayName : AmmoId);
            }
            if (Category == ItemCategory.Ammo) StatLine(sb, "Type", "Ammunition");

            if (IsDamageable && !UsesCharge) StatLine(sb, "Condition", MaxCondition + " \u00b7 " + DurabilityWord() + (IsClothing ? " (wears when struck)" : " (wears with use)"));
            if (TwoHanded) StatLine(sb, "Grip", "Two-handed");
            if (IsCanned) StatLine(sb, "<color=#e0a53c>Sealed</color>", "needs a can opener or a knife");

            switch (Gear)
            {
                case GearKind.Container:
                    StatLine(sb, "Holds", ContainerCapacity.ToString("0.#") + " kg \u00b7 " + GridW + "\u00d7" + GridH + " grid");
                    break;
                case GearKind.NightVision:
                    StatLine(sb, "Battery", Mathf.RoundToInt(ChargeSeconds / 60f) + " min \u00b7 toggle with N");
                    break;
                case GearKind.Flashlight:
                    StatLine(sb, "Battery", Mathf.RoundToInt(ChargeSeconds / 60f) + " min \u00b7 toggle with L");
                    break;
                case GearKind.GasMask:
                    StatLine(sb, "Filter", Mathf.RoundToInt(ChargeSeconds / 60f) + " min in toxic gas");
                    break;
                case GearKind.Battery:
                    StatLine(sb, "Use", "recharges night vision or a flashlight");
                    break;
                case GearKind.Filter:
                    StatLine(sb, "Use", "swaps a fresh filter into a gas mask");
                    break;
            }

            if (IsClothing)
            {
                StatLine(sb, "Worn on", ClothingSlot.ToString());
                if (Insulation > 0f) StatLine(sb, "Warmth", "+" + Insulation.ToString("0.00"));
                if (BiteDefense > 0f) StatLine(sb, "Bite defence", Pct(BiteDefense));
                if (ScratchDefense > 0f) StatLine(sb, "Scratch defence", Pct(ScratchDefense));
                if (WaterResistance > 0f) StatLine(sb, "Waterproof", Pct(WaterResistance));
            }

            if (!string.IsNullOrEmpty(Description))
                sb.Append("\n<size=13><color=#9aa0a8><i>").Append(Description).Append("</i></color></size>");
            for (int p = 0; p < TooltipProviders.Count; p++)
            {
                string extra = null;
                try { extra = TooltipProviders[p](this); }
                catch (System.Exception e) { Debug.LogException(e); }
                if (!string.IsNullOrEmpty(extra)) sb.Append('\n').Append(extra);
            }
            return sb.ToString();
        }

        private static string Pct(float frac01) => Mathf.RoundToInt(frac01 * 100f) + "%";
        private string DurabilityWord()
            => ConditionLossChance >= 24 ? "rugged" : ConditionLossChance >= 12 ? "sturdy" : "fragile";
        private static void StatLine(System.Text.StringBuilder sb, string key, string value)
            => sb.Append("\n<color=#c8ccd2>").Append(key).Append(":</color> <b>").Append(value).Append("</b>");
    }

    /// <summary>A quantity of a given item held within an <see cref="Inventory"/>.</summary>
    public class ItemStack
    {
        public readonly ItemDef Def;
        public int Count;

        /// <summary>World hour this stack was created — the origin for perishable ageing.
        /// Merges keep the older stamp (see <see cref="Inventory.Add"/>) so food can't be
        /// "refreshed" by shuffling it between containers.</summary>
        public float SpawnHours;

        /// <summary>Per-instance wear for damageable gear (weapons/tools/guns), from
        /// <see cref="ItemDef.MaxCondition"/> down to 0. Damageable items are MaxStack = 1 so each
        /// carries its own condition; at 0 the item breaks. Non-damageable stacks leave this at 0.</summary>
        public int Condition;

        /// <summary>How many times this item has been repaired. Each repair restores less and caps
        /// lower than the last, so a heavily-patched item never returns to pristine (mirrors PZ's
        /// diminishing repair). Non-repaired / non-damageable stacks leave this at 0.</summary>
        public int TimesRepaired;

        /// <summary>Where this stack sits in its inventory's grid (see <see cref="InventoryLayout"/>):
        /// the section, the top-left cell and whether it is turned 90°. Runtime-only — never saved;
        /// a loaded inventory is packed afresh. Null <see cref="Section"/> means not yet placed.</summary>
        public GridSection Section;
        public int GridX, GridY;
        public bool Rotated;

        public bool IsPlaced => Section != null;

        /// <summary>Per-instance extras (found-in-raid, insurance, gun mods and dirt, upgrades, a note), or
        /// null for a plain stack. Carried through every transfer and save (see <see cref="ItemExtra"/>).</summary>
        public ItemExtra Extra;

        /// <summary>For a powered device (<see cref="ItemDef.UsesCharge"/>): battery / filter left, 0..1.</summary>
        public float Charge01 => Def != null && Def.UsesCharge && Def.MaxCondition > 0
            ? Mathf.Clamp01((float)Condition / Def.MaxCondition) : 0f;

        public ItemStack(ItemDef def, int count)
        {
            Def = def;
            Count = count;
            SpawnHours = def != null && def.Perishable ? TimeOfDay.Now : 0f;
            Condition = def != null ? def.MaxCondition : 0;   // damageable gear starts pristine
        }

        public float Weight => Def.Weight * Count;

        /// <summary>Condition 1 (pristine) .. 0 (broken). Always 1 for gear that doesn't wear.</summary>
        public float Condition01
        {
            get
            {
                if (Def == null || Def.MaxCondition <= 0) return 1f;
                return Mathf.Clamp01((float)Condition / Def.MaxCondition);
            }
        }

        /// <summary>How many in-game hours this perishable food has aged (0 for non-perishables).</summary>
        public float AgeHours => Def != null && Def.Perishable ? Mathf.Max(0f, TimeOfDay.Now - SpawnHours) : 0f;

        /// <summary>Freshness 1 (just-made) .. 0 (fully rotten). Always 1 for non-perishables.</summary>
        public float Freshness01
        {
            get
            {
                if (Def == null || !Def.Perishable || Def.ShelfLifeHours <= 0f) return 1f;
                return Mathf.Clamp01(1f - AgeHours / Def.ShelfLifeHours);
            }
        }

        /// <summary>Fresh / Stale / Rotten, from the elapsed fraction of the shelf life.</summary>
        public FoodState State
        {
            get
            {
                if (Def == null || !Def.Perishable) return FoodState.Fresh;
                float f = AgeHours / Mathf.Max(0.01f, Def.ShelfLifeHours);
                if (f >= 1f) return FoodState.Rotten;
                if (f >= CloneZConfig.FoodStaleFraction) return FoodState.Stale;
                return FoodState.Fresh;
            }
        }
    }

    /// <summary>
    /// The catalogue of every item plus the loot tables used to fill world
    /// containers. Built lazily on first access; no scene assets required.
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<string, ItemDef> s_Items = new Dictionary<string, ItemDef>();
        private static Dictionary<ContainerType, LootTable> s_Tables;
        private static bool s_Init;

        /// <summary>Retrieves the item definition for the given identifier, or null if not found.</summary>
        public static ItemDef Get(string id)
        {
            EnsureInit();
            return s_Items.TryGetValue(id, out ItemDef def) ? def : null;
        }

        /// <summary>
        /// Loads the static content catalogue, with the content database as the source of truth.
        /// The item INSERT scripts (Assets/Resources/curated_items.txt + pz_items.txt), the loot seed in
        /// <see cref="ContentDatabase"/> and <see cref="FarmCrop.BuildSeed"/> are only a bootstrap seed:
        /// they are written to the database just once — when no usable database exists yet (a first run,
        /// a corrupt file, or an incompatible schema). Whenever a usable database is present it is loaded
        /// as-is and never overwritten, so edits made in the database persist and win over the scripts.
        /// If the database can't be opened at all, the catalogue is empty (SQLite is required for content).
        /// </summary>
        /// <summary>Ensures the static content catalogue is loaded, reading from the database or seeding if needed.</summary>
        public static void EnsureInit()
        {
            if (s_Init) return;
            s_Init = true;

            List<FarmCrop> seedCrops = FarmCrop.BuildSeed();

            string path = ContentDatabase.FilePath;
            try
            {
                if (!ContentDatabase.HasUsableContent(path))
                {
                    // The whole catalogue is authored as INSERT statements (curated + bulk); execute both to seed.
                    ContentDatabase.Write(path, LoadCuratedItemsSql(), LoadBulkItemsSql(), seedCrops);
                    Debug.Log("[CloneZ] No usable content database — seeded from the item INSERT scripts.");
                }

                ContentDatabase.Read(path, out Dictionary<string, ItemDef> items,
                    out Dictionary<ContainerType, LootTable> tables, out List<FarmCrop> crops);
                s_Items = items;
                s_Tables = tables;
                FarmCrop.SetAll(crops);
                Debug.Log("[CloneZ] Content loaded from database (" + s_Items.Count + " items). Source of truth: " + path);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CloneZ] Content DB unavailable — no items/loot loaded: " + e.Message);
                FarmCrop.SetAll(seedCrops);
            }

            // Code-defined content (the Zone's items, ballistics, medical kit...) fills in whatever the database
            // doesn't define; a database row always wins an id clash.
            if (s_Tables == null) s_Tables = new Dictionary<ContainerType, LootTable>();
            ContentRegistry.Apply(s_Items, s_Tables);
        }

        /// <summary>Every item id in the catalogue (code content included).</summary>
        /// <summary>Gets every item identifier currently in the catalogue (including code-defined items).</summary>
        public static IEnumerable<string> AllIds
        {
            get { EnsureInit(); return s_Items.Keys; }
        }

        /// <summary>
        /// Optional gate on loot generation (the Central Economy): asked for each rolled item id; false skips the
        /// draw. Null lets everything through.
        /// </summary>
        /// <summary>Optional filter for loot generation; returns false to skip an item, null to allow all.</summary>
        public static System.Func<string, bool> LootGate;

        /// <summary>Told about every stack loot generation creates (the Central Economy counts them).</summary>
        /// <summary>Callback invoked whenever loot is rolled, reporting the item id and quantity.</summary>
        public static System.Action<string, int> LootRolled;

        // ---- Item catalogue -----------------------------------------------------------------------
        // The whole catalogue is authored as INSERT statements in two bundled resources: the 68 curated
        // items (curated_items.txt) and the full PZ catalogue (pz_items.txt), generated from the decompiled
        // item scripts. Both are executed as-is by ContentDatabase while (re)seeding the content DB — no
        // per-launch parse once the DB exists. Curated items are seeded first so they win any id clash.
        /// <summary>Loads the INSERT SQL for curated items from Resources.</summary>
        private static string LoadCuratedItemsSql() => LoadItemsResource("curated_items");
        /// <summary>Loads the INSERT SQL for bulk items from Resources.</summary>
        private static string LoadBulkItemsSql() => LoadItemsResource("pz_items");

        /// <summary>Loads a TextAsset resource by name, used for SQL scripts.</summary>
        private static string LoadItemsResource(string name)
        {
            TextAsset ta = Resources.Load<TextAsset>(name);
            if (ta == null) { Debug.LogWarning("[CloneZ] item resource '" + name + "' missing; skipped."); return null; }
            return ta.text;
        }

        #region Loot tables
        /// <summary>One weighted entry in a loot table: an item id, its pick weight and the
        /// quantity range rolled when picked. Public so <see cref="ContentDatabase"/> can persist it.</summary>
        /// <summary>A single entry in a loot table: item id, weight, and quantity range.</summary>
        public struct LootEntry
        {
            public string Id; public int Weight; public int Min; public int Max;
            public LootEntry(string id, int weight, int min, int max) { Id = id; Weight = weight; Min = min; Max = max; }
        }

        /// <summary>A container's loot table: how many draws to roll, the odds of an empty draw,
        /// and the weighted entries. Public so <see cref="ContentDatabase"/> can persist it.</summary>
        /// <summary>Defines the loot table for a container: roll counts, empty weight, and entry list.</summary>
        public class LootTable
        {
            public int MinRolls; public int MaxRolls; public int EmptyWeight; public LootEntry[] Entries;
        }

        /// <summary>Fills <paramref name="dest"/> with a random draw from the container's loot table. In a raid every
        /// rolled stack is marked found-in-raid; the sandbox's loot abundance and the zone's loot multiplier scale
        /// the number of draws, and the Central Economy (<see cref="LootGate"/>) can veto items at their cap.</summary>
        /// <summary>Draws random loot from a container's table into an inventory, respecting abundance and economy.</summary>
        public static void RollLoot(ContainerType type, System.Random rng, Inventory dest)
        {
            EnsureInit();
            if (s_Tables == null || !s_Tables.TryGetValue(type, out LootTable table)) return;

            int rolls = rng.Next(table.MinRolls, table.MaxRolls + 1);
            float abundance = SandboxSettings.Current.LootAbundance * (GameSession.IsExtraction ? ZoneCatalog.Current.LootMultiplier : 1f);
            if (!Mathf.Approximately(abundance, 1f))
            {
                float scaled = rolls * Mathf.Max(0f, abundance);
                rolls = Mathf.FloorToInt(scaled) + (rng.NextDouble() < scaled - Mathf.Floor(scaled) ? 1 : 0);
            }
            ItemExtra raidLoot = GameSession.IsExtraction ? new ItemExtra { FoundInRaid = true } : null;
            for (int r = 0; r < rolls; r++)
            {
                int total = table.EmptyWeight;
                for (int i = 0; i < table.Entries.Length; i++) total += table.Entries[i].Weight;
                if (total <= 0) return;

                int pick = rng.Next(total);
                if (pick < table.EmptyWeight) continue;   // an empty draw — containers are often sparse
                pick -= table.EmptyWeight;

                for (int i = 0; i < table.Entries.Length; i++)
                {
                    LootEntry e = table.Entries[i];
                    if (pick < e.Weight)
                    {
                        ItemDef def = Get(e.Id);
                        int n = rng.Next(e.Min, e.Max + 1);
                        if (def != null && n > 0 && (LootGate == null || LootGate(def.Id)))
                        {
                            int added = dest.Add(def, n, enforceWeight: false, extra: raidLoot);
                            if (added > 0) LootRolled?.Invoke(def.Id, added);
                        }
                        break;
                    }
                    pick -= e.Weight;
                }
            }
        }
        #endregion
    }
}
