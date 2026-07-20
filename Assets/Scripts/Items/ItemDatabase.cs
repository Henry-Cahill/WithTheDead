using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    public enum ItemCategory { Food, Drink, Medical, Weapon, Firearm, Ammo, Material, Clothing, Misc }

    /// <summary>Where a piece of clothing is worn. One item per slot at a time.</summary>
    public enum ClothingSlot { Head, Torso, Legs, Feet, Hands }

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

        // Optional flavour line shown at the foot of the tooltip.
        public string Description;

        public bool IsConsumable =>
            HungerRestore > 0f || ThirstRestore > 0f || HealthRestore > 0f || InfectionReduce > 0f;
        public bool IsMedical =>
            StopsBleeding || Disinfects || CuresWoundInfection || PainRelief > 0f || CalmsPanic;
        public bool IsEquippable => IsWeapon || IsFirearm || IsClothing;
        public bool IsUsable => IsConsumable || IsMedical || IsEquippable;

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

            if (IsDamageable) StatLine(sb, "Condition", MaxCondition + " \u00b7 " + DurabilityWord() + " (wears with use)");

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

        public static ItemDef Get(string id)
        {
            EnsureInit();
            return s_Items.TryGetValue(id, out ItemDef def) ? def : null;
        }

        /// <summary>
        /// Loads the static content catalogue, with the content database as the source of truth.
        /// The in-code <see cref="BuildItems"/> / <see cref="BuildTables"/> catalogues (plus
        /// <see cref="FarmCrop.BuildSeed"/>) are only a bootstrap seed: they are written to the
        /// database (see <see cref="ContentDatabase"/>) just once — when no usable database exists
        /// yet (a first run, a corrupt file, or an incompatible schema). Whenever a usable database
        /// is present it is loaded as-is and never overwritten, so edits made in the database
        /// persist and win over the built-in catalogue. If the database can't be opened at all the
        /// seed is used directly, so the game always has its content.
        /// </summary>
        public static void EnsureInit()
        {
            if (s_Init) return;
            s_Init = true;

            BuildItems();     // bootstrap seed -> s_Items
            BuildTables();    // bootstrap seed -> s_Tables
            List<FarmCrop> seedCrops = FarmCrop.BuildSeed();

            string path = ContentDatabase.FilePath;
            try
            {
                if (!ContentDatabase.HasUsableContent(path))
                {
                    ContentDatabase.Write(path, new List<ItemDef>(s_Items.Values), s_Tables, seedCrops);
                    Debug.Log("[CloneZ] No usable content database — seeded the built-in catalogue.");
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
                Debug.LogError("[CloneZ] Content DB unavailable, using built-in catalogue: " + e.Message);
                FarmCrop.SetAll(seedCrops);
            }
        }

        private static void Add(ItemDef def) => s_Items[def.Id] = def;

        private static void BuildItems()
        {
            Color food = new Color(0.85f, 0.6f, 0.25f);
            Color drink = new Color(0.3f, 0.6f, 0.9f);
            Color med = new Color(0.9f, 0.35f, 0.35f);
            Color steel = new Color(0.7f, 0.72f, 0.78f);
            Color wood = new Color(0.55f, 0.42f, 0.28f);
            Color misc = new Color(0.6f, 0.6f, 0.62f);

            // ---- Food (HungerRestore = fraction of the 0..1 Hunger stat) ----
            Add(new ItemDef { Id = "cannedbeans", DisplayName = "Canned Beans", Category = ItemCategory.Food, Weight = 0.9f, MaxStack = 6, Tint = food, HungerRestore = 0.50f });
            Add(new ItemDef { Id = "cannedsoup", DisplayName = "Canned Soup", Category = ItemCategory.Food, Weight = 0.9f, MaxStack = 6, Tint = food, HungerRestore = 0.45f, ThirstRestore = 0.10f });
            Add(new ItemDef { Id = "chips", DisplayName = "Potato Chips", Category = ItemCategory.Food, Weight = 0.2f, MaxStack = 8, Tint = food, HungerRestore = 0.20f });
            Add(new ItemDef { Id = "crackers", DisplayName = "Crackers", Category = ItemCategory.Food, Weight = 0.2f, MaxStack = 8, Tint = food, HungerRestore = 0.18f });
            Add(new ItemDef { Id = "apple", DisplayName = "Apple", Category = ItemCategory.Food, Weight = 0.15f, MaxStack = 10, Tint = new Color(0.8f, 0.3f, 0.25f), HungerRestore = 0.15f, ThirstRestore = 0.05f, Perishable = true, ShelfLifeHours = 130f });
            Add(new ItemDef { Id = "breadloaf", DisplayName = "Bread", Category = ItemCategory.Food, Weight = 0.4f, MaxStack = 4, Tint = food, HungerRestore = 0.35f, Perishable = true, ShelfLifeHours = 100f });
            Add(new ItemDef { Id = "cerealbox", DisplayName = "Cereal", Category = ItemCategory.Food, Weight = 0.5f, MaxStack = 4, Tint = food, HungerRestore = 0.30f });

            // ---- Raw ingredients (perishable; cook them at a stove to feed better and stay safe) ----
            Color raw = new Color(0.78f, 0.42f, 0.42f);
            Color cooked = new Color(0.62f, 0.44f, 0.30f);
            Color veg = new Color(0.75f, 0.62f, 0.35f);
            Add(new ItemDef { Id = "rawmeat", DisplayName = "Raw Meat", Category = ItemCategory.Food, Weight = 0.6f, MaxStack = 3, Tint = raw, HungerRestore = 0.40f, Perishable = true, ShelfLifeHours = 30f, Cookable = true, CookedId = "cookedmeat", RawHazard = true, Description = "Spoils quickly. Cook it before it turns \u2014 and before you eat it." });
            Add(new ItemDef { Id = "cookedmeat", DisplayName = "Cooked Meat", Category = ItemCategory.Food, Weight = 0.5f, MaxStack = 3, Tint = cooked, HungerRestore = 0.55f, Perishable = true, ShelfLifeHours = 48f, IsCookedFood = true, Description = "A proper meal. Filling and safe \u2014 while it lasts." });
            Add(new ItemDef { Id = "chicken", DisplayName = "Raw Chicken", Category = ItemCategory.Food, Weight = 0.6f, MaxStack = 3, Tint = raw, HungerRestore = 0.38f, Perishable = true, ShelfLifeHours = 26f, Cookable = true, CookedId = "cookedchicken", RawHazard = true, Description = "Eating raw chicken is a very bad idea. Cook it." });
            Add(new ItemDef { Id = "cookedchicken", DisplayName = "Cooked Chicken", Category = ItemCategory.Food, Weight = 0.5f, MaxStack = 3, Tint = cooked, HungerRestore = 0.52f, Perishable = true, ShelfLifeHours = 44f, IsCookedFood = true });
            Add(new ItemDef { Id = "potato", DisplayName = "Potato", Category = ItemCategory.Food, Weight = 0.3f, MaxStack = 6, Tint = veg, HungerRestore = 0.22f, Perishable = true, ShelfLifeHours = 240f, Cookable = true, CookedId = "bakedpotato", Description = "Keeps for ages in the pantry. Far better baked." });
            Add(new ItemDef { Id = "bakedpotato", DisplayName = "Baked Potato", Category = ItemCategory.Food, Weight = 0.3f, MaxStack = 6, Tint = cooked, HungerRestore = 0.40f, Perishable = true, ShelfLifeHours = 72f, IsCookedFood = true });

            // ---- Garden crops (grown from seed at a farm plot; edible raw, spoil in time) ----
            Add(new ItemDef { Id = "cabbage", DisplayName = "Cabbage", Category = ItemCategory.Food, Weight = 0.4f, MaxStack = 6, Tint = new Color(0.55f, 0.72f, 0.42f), HungerRestore = 0.24f, ThirstRestore = 0.04f, Perishable = true, ShelfLifeHours = 160f, Description = "Home-grown and hearty. Keeps well in a cool pantry." });
            Add(new ItemDef { Id = "tomato", DisplayName = "Tomato", Category = ItemCategory.Food, Weight = 0.2f, MaxStack = 8, Tint = new Color(0.82f, 0.28f, 0.22f), HungerRestore = 0.16f, ThirstRestore = 0.08f, Perishable = true, ShelfLifeHours = 90f, Description = "Ripe and juicy. Doesn't last long off the vine." });
            Add(new ItemDef { Id = "carrot", DisplayName = "Carrot", Category = ItemCategory.Food, Weight = 0.2f, MaxStack = 8, Tint = new Color(0.85f, 0.5f, 0.2f), HungerRestore = 0.18f, ThirstRestore = 0.05f, Perishable = true, ShelfLifeHours = 200f, Description = "Crunchy and keeps for weeks. Good raw." });

            // ---- Drink (ThirstRestore = fraction of the 0..1 Thirst stat) ----
            Add(new ItemDef { Id = "waterbottle", DisplayName = "Water Bottle", Category = ItemCategory.Drink, Weight = 0.8f, MaxStack = 4, Tint = drink, ThirstRestore = 0.50f, IsWaterContainer = true, HoldsWater = true, EmptyId = "emptybottle", Description = "Clean drinking water. Keep the empty \u2014 refill it at a tap or barrel." });
            Add(new ItemDef { Id = "soda", DisplayName = "Soda Can", Category = ItemCategory.Drink, Weight = 0.4f, MaxStack = 6, Tint = drink, ThirstRestore = 0.35f, HungerRestore = 0.05f });
            Add(new ItemDef { Id = "juicebox", DisplayName = "Juice Box", Category = ItemCategory.Drink, Weight = 0.3f, MaxStack = 6, Tint = drink, ThirstRestore = 0.30f, HungerRestore = 0.05f });

            // Fillable vessels: an empty bottle refilled at a tap (clean) or a rain barrel (tainted).
            Color emptyBottle = new Color(0.6f, 0.72f, 0.8f);
            Color murkyWater = new Color(0.45f, 0.55f, 0.42f);
            Add(new ItemDef { Id = "emptybottle", DisplayName = "Empty Bottle", Category = ItemCategory.Misc, Weight = 0.2f, MaxStack = 4, Tint = emptyBottle, IsWaterContainer = true, FillCleanId = "waterbottle", FillTaintedId = "taintedwater", Description = "An empty water bottle. Fill it at a tap or a rain barrel." });
            Add(new ItemDef { Id = "taintedwater", DisplayName = "Tainted Water", Category = ItemCategory.Drink, Weight = 0.8f, MaxStack = 4, Tint = murkyWater, ThirstRestore = 0.50f, IsWaterContainer = true, HoldsWater = true, WaterTainted = true, EmptyId = "emptybottle", PurifiedId = "waterbottle", Description = "Collected rain water. Boil it at a stove to make it safe \u2014 or gamble your stomach." });

            // ---- Medical (act on the wound / pain / infection model) ----
            Add(new ItemDef { Id = "bandage", DisplayName = "Bandage", Category = ItemCategory.Medical, Weight = 0.1f, MaxStack = 8, Tint = med, StopsBleeding = true, SterileBandage = true, Description = "Sterile dressing. Stops bleeding and keeps the wound clean." });
            Add(new ItemDef { Id = "pills", DisplayName = "Painkillers", Category = ItemCategory.Medical, Weight = 0.1f, MaxStack = 6, Tint = med, PainRelief = 0.6f, Description = "Dulls pain for a while. Won't close a wound." });
            Add(new ItemDef { Id = "antibiotics", DisplayName = "Antibiotics", Category = ItemCategory.Medical, Weight = 0.1f, MaxStack = 4, Tint = med, CuresWoundInfection = true, InfectionReduce = 18f, Description = "Clears an infected wound over time; slows the virus a little." });
            Add(new ItemDef { Id = "disinfectant", DisplayName = "Disinfectant", Category = ItemCategory.Medical, Weight = 0.5f, MaxStack = 3, Tint = med, Disinfects = true, Description = "Cleans open wounds so they won't fester." });
            Add(new ItemDef { Id = "firstaid", DisplayName = "First Aid Kit", Category = ItemCategory.Medical, Weight = 1.2f, MaxStack = 2, Tint = med, StopsBleeding = true, SterileBandage = true, Disinfects = true, CuresWoundInfection = true, PainRelief = 0.5f, HealthRestore = 15f, Description = "Everything to dress, disinfect and close a nasty wound." });
            Add(new ItemDef { Id = "betablockers", DisplayName = "Beta Blockers", Category = ItemCategory.Medical, Weight = 0.1f, MaxStack = 6, Tint = new Color(0.55f, 0.5f, 0.8f), CalmsPanic = true, Description = "Steadies the nerves. Keeps panic at bay for a while." });

            // ---- Materials / misc (no direct use yet; weight to manage) ----
            Add(new ItemDef { Id = "plank", DisplayName = "Wooden Plank", Category = ItemCategory.Material, Weight = 1.5f, MaxStack = 4, Tint = wood });
            Add(new ItemDef { Id = "nails", DisplayName = "Box of Nails", Category = ItemCategory.Material, Weight = 0.2f, MaxStack = 6, Tint = steel });
            Add(new ItemDef { Id = "ducttape", DisplayName = "Duct Tape", Category = ItemCategory.Material, Weight = 0.3f, MaxStack = 3, Tint = misc });
            Add(new ItemDef { Id = "rag", DisplayName = "Ripped Sheets", Category = ItemCategory.Material, Weight = 0.1f, MaxStack = 10, Tint = misc, StopsBleeding = true, SterileBandage = false, Description = "Improvised bandage. Stops bleeding but risks infection." });
            Add(new ItemDef { Id = "rope", DisplayName = "Rope", Category = ItemCategory.Material, Weight = 0.8f, MaxStack = 3, Tint = wood });
            Add(new ItemDef { Id = "battery", DisplayName = "Battery", Category = ItemCategory.Misc, Weight = 0.2f, MaxStack = 6, Tint = misc });

            // ---- Farming tools & seeds (a trowel breaks ground and pulls crops; a can waters them) ----
            Color tool = new Color(0.45f, 0.5f, 0.55f);
            Color seed = new Color(0.62f, 0.54f, 0.34f);
            Add(new ItemDef { Id = "trowel", DisplayName = "Garden Trowel", Category = ItemCategory.Material, Weight = 0.4f, MaxStack = 1, Tint = tool, MaxCondition = 8, ConditionLossChance = 12, Description = "Breaks new ground into a plot and pulls up ripe crops." });
            Add(new ItemDef { Id = "wateringcan", DisplayName = "Watering Can", Category = ItemCategory.Material, Weight = 1.1f, MaxStack = 1, Tint = new Color(0.4f, 0.6f, 0.7f), MaxCondition = 10, ConditionLossChance = 20, Description = "Waters a thirsty plot to keep the crop growing." });
            Add(new ItemDef { Id = "potatoseeds", DisplayName = "Potato Seeds", Category = ItemCategory.Material, Weight = 0.05f, MaxStack = 12, Tint = seed, IsSeed = true, CropId = "potato", Description = "Sow at a tilled plot. Hardy and quick to grow." });
            Add(new ItemDef { Id = "carrotseeds", DisplayName = "Carrot Seeds", Category = ItemCategory.Material, Weight = 0.05f, MaxStack = 12, Tint = seed, IsSeed = true, CropId = "carrot" });
            Add(new ItemDef { Id = "tomatoseeds", DisplayName = "Tomato Seeds", Category = ItemCategory.Material, Weight = 0.05f, MaxStack = 12, Tint = seed, IsSeed = true, CropId = "tomato" });
            Add(new ItemDef { Id = "cabbageseeds", DisplayName = "Cabbage Seeds", Category = ItemCategory.Material, Weight = 0.05f, MaxStack = 12, Tint = seed, IsSeed = true, CropId = "cabbage" });

            // ---- Ammunition ----
            Color brass = new Color(0.80f, 0.66f, 0.30f);
            Add(new ItemDef { Id = "ammo9mm", DisplayName = "9mm Rounds", Category = ItemCategory.Ammo, Weight = 0.012f, MaxStack = 60, Tint = brass, Description = "Handgun cartridges. Feeds the M9 pistol." });
            Add(new ItemDef { Id = "shotgunshells", DisplayName = "Shotgun Shells", Category = ItemCategory.Ammo, Weight = 0.05f, MaxStack = 24, Tint = new Color(0.7f, 0.2f, 0.18f), Description = "12-gauge buckshot. Feeds the shotgun." });

            // ---- Firearms (equipping enters ranged mode; loud — gunfire draws the horde) ----
            Color gunmetal = new Color(0.20f, 0.21f, 0.24f);
            Add(new ItemDef { Id = "pistol", DisplayName = "M9 Pistol", Category = ItemCategory.Firearm, Weight = 1.1f, Tint = gunmetal, IsFirearm = true, FirearmDamage = 1.7f, FirearmRange = 26f, PelletsPerShot = 1, MagazineSize = 15, FireCooldown = 0.22f, ReloadTime = 1.6f, FirearmSpread = 0.6f, AmmoId = "ammo9mm", FirearmNoise = CloneZConfig.NoiseGunshotPistol, Knockback = 1.5f, MaxCondition = 10, ConditionLossChance = 40, Description = "Reliable 9mm sidearm. Accurate when standing still." });
            Add(new ItemDef { Id = "shotgun", DisplayName = "Shotgun", Category = ItemCategory.Firearm, Weight = 3.2f, Tint = new Color(0.28f, 0.22f, 0.18f), IsFirearm = true, FirearmDamage = 0.9f, FirearmRange = 16f, PelletsPerShot = 7, MagazineSize = 6, FireCooldown = 0.85f, ReloadTime = 2.6f, FirearmSpread = 7f, AmmoId = "shotgunshells", FirearmNoise = CloneZConfig.NoiseGunshotShotgun, Knockback = 3.5f, MaxCondition = 10, ConditionLossChance = 35, Description = "Pump-action. Devastating up close, useless at range." });

            // ---- Melee weapons (equipping overrides PlayerCombat values) ----
            Add(new ItemDef { Id = "kitchenknife", DisplayName = "Kitchen Knife", Category = ItemCategory.Weapon, Weight = 0.3f, Tint = steel, IsWeapon = true, WeaponDamage = 1.1f, WeaponRange = 1.9f, WeaponArc = 60f, SwingCooldown = 0.35f, Knockback = 1.5f, WeaponSkill = PlayerSkills.Skill.Blade, MaxCondition = 8, ConditionLossChance = 10 });
            Add(new ItemDef { Id = "hammer", DisplayName = "Hammer", Category = ItemCategory.Weapon, Weight = 0.8f, Tint = steel, IsWeapon = true, WeaponDamage = 1.2f, WeaponRange = 1.9f, WeaponArc = 70f, SwingCooldown = 0.5f, Knockback = 2.5f, MaxCondition = 10, ConditionLossChance = 25 });
            Add(new ItemDef { Id = "fryingpan", DisplayName = "Frying Pan", Category = ItemCategory.Weapon, Weight = 1.0f, Tint = steel, IsWeapon = true, WeaponDamage = 1.3f, WeaponRange = 2.0f, WeaponArc = 90f, SwingCooldown = 0.6f, Knockback = 3.5f, MaxCondition = 10, ConditionLossChance = 18 });
            Add(new ItemDef { Id = "crowbar", DisplayName = "Crowbar", Category = ItemCategory.Weapon, Weight = 1.5f, Tint = steel, IsWeapon = true, WeaponDamage = 1.6f, WeaponRange = 2.2f, WeaponArc = 80f, SwingCooldown = 0.6f, Knockback = 4.0f, MaxCondition = 12, ConditionLossChance = 35 });
            Add(new ItemDef { Id = "baseballbat", DisplayName = "Baseball Bat", Category = ItemCategory.Weapon, Weight = 1.2f, Tint = wood, IsWeapon = true, WeaponDamage = 1.8f, WeaponRange = 2.6f, WeaponArc = 100f, SwingCooldown = 0.7f, Knockback = 5.5f, MaxCondition = 10, ConditionLossChance = 22 });
            Add(new ItemDef { Id = "fireaxe", DisplayName = "Fire Axe", Category = ItemCategory.Weapon, Weight = 3.0f, Tint = new Color(0.8f, 0.3f, 0.2f), IsWeapon = true, WeaponDamage = 2.6f, WeaponRange = 2.4f, WeaponArc = 80f, SwingCooldown = 0.9f, Knockback = 4.5f, WeaponSkill = PlayerSkills.Skill.Blade, MaxCondition = 12, ConditionLossChance = 30 });

            // ---- Clothing (worn on a slot; insulates against the cold, can turn a bite) ----
            Color cloth = new Color(0.52f, 0.55f, 0.62f);
            Color denim = new Color(0.28f, 0.36f, 0.52f);
            Color leather = new Color(0.35f, 0.24f, 0.18f);
            // Head
            Add(new ItemDef { Id = "cap", DisplayName = "Baseball Cap", Category = ItemCategory.Clothing, Weight = 0.2f, Tint = new Color(0.4f, 0.45f, 0.6f), IsClothing = true, ClothingSlot = ClothingSlot.Head, Insulation = 0.02f, ScratchDefense = 0.10f, Description = "Keeps the sun off. Not much else." });
            Add(new ItemDef { Id = "beanie", DisplayName = "Wool Hat", Category = ItemCategory.Clothing, Weight = 0.2f, Tint = new Color(0.5f, 0.35f, 0.35f), IsClothing = true, ClothingSlot = ClothingSlot.Head, Insulation = 0.06f, Description = "Snug knitted hat. Real warmth for cold nights." });
            Add(new ItemDef { Id = "bikehelmet", DisplayName = "Bike Helmet", Category = ItemCategory.Clothing, Weight = 0.7f, Tint = new Color(0.8f, 0.8f, 0.2f), IsClothing = true, ClothingSlot = ClothingSlot.Head, Insulation = 0.01f, BiteDefense = 0.35f, ScratchDefense = 0.30f, Description = "Hard shell. Teeth don't get through the top of your skull." });
            // Torso
            Add(new ItemDef { Id = "tshirt", DisplayName = "T-Shirt", Category = ItemCategory.Clothing, Weight = 0.3f, Tint = cloth, IsClothing = true, ClothingSlot = ClothingSlot.Torso, Insulation = 0.03f, ScratchDefense = 0.08f, Description = "Basic cotton tee." });
            Add(new ItemDef { Id = "hoodie", DisplayName = "Hoodie", Category = ItemCategory.Clothing, Weight = 0.8f, Tint = new Color(0.45f, 0.45f, 0.5f), IsClothing = true, ClothingSlot = ClothingSlot.Torso, Insulation = 0.09f, BiteDefense = 0.10f, ScratchDefense = 0.15f, Description = "Warm and comfy. A little padding against the dead." });
            Add(new ItemDef { Id = "jacket", DisplayName = "Padded Jacket", Category = ItemCategory.Clothing, Weight = 1.4f, Tint = new Color(0.30f, 0.34f, 0.40f), IsClothing = true, ClothingSlot = ClothingSlot.Torso, Insulation = 0.13f, BiteDefense = 0.20f, ScratchDefense = 0.25f, Description = "Thick and warm. Good against the cold and the horde alike." });
            Add(new ItemDef { Id = "leatherjacket", DisplayName = "Leather Jacket", Category = ItemCategory.Clothing, Weight = 1.8f, Tint = leather, IsClothing = true, ClothingSlot = ClothingSlot.Torso, Insulation = 0.10f, BiteDefense = 0.40f, ScratchDefense = 0.45f, Description = "The best bite protection you'll find on your back." });
            Add(new ItemDef { Id = "raincoat", DisplayName = "Rain Coat", Category = ItemCategory.Clothing, Weight = 0.9f, Tint = new Color(0.85f, 0.8f, 0.25f), IsClothing = true, ClothingSlot = ClothingSlot.Torso, Insulation = 0.05f, ScratchDefense = 0.12f, WaterResistance = 0.85f, Description = "Sheds the rain and keeps you dry in a downpour." });
            // Legs
            Add(new ItemDef { Id = "shorts", DisplayName = "Shorts", Category = ItemCategory.Clothing, Weight = 0.2f, Tint = cloth, IsClothing = true, ClothingSlot = ClothingSlot.Legs, Insulation = 0.02f, Description = "Cool and light. No help in the cold." });
            Add(new ItemDef { Id = "jeans", DisplayName = "Jeans", Category = ItemCategory.Clothing, Weight = 0.6f, Tint = denim, IsClothing = true, ClothingSlot = ClothingSlot.Legs, Insulation = 0.06f, BiteDefense = 0.15f, ScratchDefense = 0.22f, Description = "Tough denim. Turns the odd bite on the leg." });
            Add(new ItemDef { Id = "trousers", DisplayName = "Work Trousers", Category = ItemCategory.Clothing, Weight = 0.7f, Tint = new Color(0.35f, 0.34f, 0.28f), IsClothing = true, ClothingSlot = ClothingSlot.Legs, Insulation = 0.07f, BiteDefense = 0.12f, ScratchDefense = 0.20f, Description = "Hard-wearing and warm." });
            // Feet
            Add(new ItemDef { Id = "sneakers", DisplayName = "Sneakers", Category = ItemCategory.Clothing, Weight = 0.5f, Tint = new Color(0.7f, 0.7f, 0.72f), IsClothing = true, ClothingSlot = ClothingSlot.Feet, Insulation = 0.03f, Description = "Light and quiet on your feet." });
            Add(new ItemDef { Id = "boots", DisplayName = "Leather Boots", Category = ItemCategory.Clothing, Weight = 1.0f, Tint = leather, IsClothing = true, ClothingSlot = ClothingSlot.Feet, Insulation = 0.06f, BiteDefense = 0.20f, ScratchDefense = 0.30f, Description = "Sturdy boots. Kick through the muck and keep the cold out." });
            // Hands
            Add(new ItemDef { Id = "gloves", DisplayName = "Wool Gloves", Category = ItemCategory.Clothing, Weight = 0.1f, Tint = new Color(0.5f, 0.4f, 0.4f), IsClothing = true, ClothingSlot = ClothingSlot.Hands, Insulation = 0.05f, BiteDefense = 0.12f, ScratchDefense = 0.18f, Description = "Warm hands make for a steadier grip." });
            Add(new ItemDef { Id = "leathergloves", DisplayName = "Leather Gloves", Category = ItemCategory.Clothing, Weight = 0.2f, Tint = leather, IsClothing = true, ClothingSlot = ClothingSlot.Hands, Insulation = 0.04f, BiteDefense = 0.30f, ScratchDefense = 0.35f, Description = "Grabbing hands stay safe behind thick leather." });
        }

        #region Loot tables
        /// <summary>One weighted entry in a loot table: an item id, its pick weight and the
        /// quantity range rolled when picked. Public so <see cref="ContentDatabase"/> can persist it.</summary>
        public struct LootEntry
        {
            public string Id; public int Weight; public int Min; public int Max;
            public LootEntry(string id, int weight, int min, int max) { Id = id; Weight = weight; Min = min; Max = max; }
        }

        /// <summary>A container's loot table: how many draws to roll, the odds of an empty draw,
        /// and the weighted entries. Public so <see cref="ContentDatabase"/> can persist it.</summary>
        public class LootTable
        {
            public int MinRolls; public int MaxRolls; public int EmptyWeight; public LootEntry[] Entries;
        }

        private static void BuildTables()
        {
            s_Tables = new Dictionary<ContainerType, LootTable>
            {
                [ContainerType.Crate] = new LootTable { MinRolls = 2, MaxRolls = 4, EmptyWeight = 6, Entries = new[] {
                    new LootEntry("cannedbeans", 8, 1, 2), new LootEntry("cannedsoup", 6, 1, 2), new LootEntry("crackers", 6, 1, 3),
                    new LootEntry("plank", 6, 1, 2), new LootEntry("nails", 5, 1, 2), new LootEntry("ducttape", 4, 1, 1),
                    new LootEntry("rope", 3, 1, 1), new LootEntry("baseballbat", 2, 1, 1), new LootEntry("crowbar", 2, 1, 1),
                    new LootEntry("shotgunshells", 3, 2, 6), new LootEntry("ammo9mm", 3, 4, 10),
                    new LootEntry("battery", 4, 1, 2), new LootEntry("raincoat", 3, 1, 1), new LootEntry("trousers", 4, 1, 1),
                    new LootEntry("emptybottle", 5, 1, 2),
                    new LootEntry("potatoseeds", 4, 1, 3), new LootEntry("cabbageseeds", 3, 1, 2), new LootEntry("tomatoseeds", 3, 1, 2),
                    new LootEntry("trowel", 2, 1, 1),
                    new LootEntry("leathergloves", 2, 1, 1), new LootEntry("leatherjacket", 1, 1, 1) } },
                [ContainerType.Locker] = new LootTable { MinRolls = 2, MaxRolls = 4, EmptyWeight = 8, Entries = new[] {
                    new LootEntry("rag", 8, 1, 3), new LootEntry("bandage", 5, 1, 2), new LootEntry("chips", 5, 1, 2),
                    new LootEntry("soda", 5, 1, 2), new LootEntry("kitchenknife", 3, 1, 1), new LootEntry("baseballbat", 2, 1, 1),
                    new LootEntry("pistol", 1, 1, 1), new LootEntry("ammo9mm", 3, 4, 10),
                    new LootEntry("pills", 4, 1, 1), new LootEntry("battery", 3, 1, 1),
                    new LootEntry("tshirt", 6, 1, 1), new LootEntry("hoodie", 4, 1, 1), new LootEntry("jeans", 5, 1, 1),
                    new LootEntry("emptybottle", 5, 1, 2),
                    new LootEntry("sneakers", 4, 1, 1), new LootEntry("beanie", 4, 1, 1), new LootEntry("cap", 4, 1, 1),
                    new LootEntry("jacket", 2, 1, 1), new LootEntry("gloves", 3, 1, 1), new LootEntry("boots", 2, 1, 1) } },
                [ContainerType.Fridge] = new LootTable { MinRolls = 2, MaxRolls = 4, EmptyWeight = 4, Entries = new[] {
                    new LootEntry("waterbottle", 9, 1, 2), new LootEntry("soda", 8, 1, 3), new LootEntry("juicebox", 6, 1, 2),
                    new LootEntry("emptybottle", 6, 1, 2),
                    new LootEntry("cannedsoup", 6, 1, 2), new LootEntry("apple", 7, 1, 3), new LootEntry("breadloaf", 5, 1, 1),
                    new LootEntry("cerealbox", 4, 1, 1), new LootEntry("cannedbeans", 6, 1, 2),
                    new LootEntry("rawmeat", 5, 1, 2), new LootEntry("chicken", 4, 1, 1), new LootEntry("potato", 6, 1, 3),
                    new LootEntry("cabbage", 4, 1, 1), new LootEntry("tomato", 5, 1, 2), new LootEntry("carrot", 5, 1, 2) } },
                [ContainerType.MedCabinet] = new LootTable { MinRolls = 1, MaxRolls = 3, EmptyWeight = 5, Entries = new[] {
                    new LootEntry("bandage", 9, 1, 3), new LootEntry("pills", 7, 1, 2), new LootEntry("antibiotics", 4, 1, 1),
                    new LootEntry("disinfectant", 5, 1, 2), new LootEntry("firstaid", 2, 1, 1), new LootEntry("rag", 5, 1, 2),
                    new LootEntry("betablockers", 4, 1, 2) } },
                [ContainerType.Desk] = new LootTable { MinRolls = 1, MaxRolls = 3, EmptyWeight = 9, Entries = new[] {
                    new LootEntry("crackers", 5, 1, 2), new LootEntry("chips", 5, 1, 2), new LootEntry("pills", 4, 1, 1),
                    new LootEntry("bandage", 4, 1, 1), new LootEntry("battery", 5, 1, 2), new LootEntry("ducttape", 4, 1, 1),
                    new LootEntry("ammo9mm", 3, 3, 8), new LootEntry("kitchenknife", 2, 1, 1),
                    new LootEntry("betablockers", 2, 1, 1) } },
                [ContainerType.ToolBench] = new LootTable { MinRolls = 2, MaxRolls = 4, EmptyWeight = 5, Entries = new[] {
                    new LootEntry("nails", 8, 1, 3), new LootEntry("plank", 7, 1, 2), new LootEntry("ducttape", 6, 1, 2),
                    new LootEntry("hammer", 5, 1, 1), new LootEntry("crowbar", 4, 1, 1), new LootEntry("fireaxe", 2, 1, 1),
                    new LootEntry("shotgun", 1, 1, 1), new LootEntry("shotgunshells", 3, 2, 6),
                    new LootEntry("fryingpan", 3, 1, 1), new LootEntry("rope", 5, 1, 1), new LootEntry("battery", 4, 1, 2),
                    new LootEntry("trowel", 4, 1, 1), new LootEntry("wateringcan", 3, 1, 1),
                    new LootEntry("potatoseeds", 4, 1, 3), new LootEntry("carrotseeds", 3, 1, 2) } },
                [ContainerType.Corpse] = new LootTable { MinRolls = 1, MaxRolls = 2, EmptyWeight = 34, Entries = new[] {
                    new LootEntry("chips", 6, 1, 1), new LootEntry("crackers", 5, 1, 1), new LootEntry("soda", 4, 1, 1),
                    new LootEntry("bandage", 5, 1, 1), new LootEntry("pills", 4, 1, 1), new LootEntry("battery", 4, 1, 1),
                    new LootEntry("ammo9mm", 4, 2, 6), new LootEntry("shotgunshells", 2, 1, 3),
                    new LootEntry("kitchenknife", 2, 1, 1), new LootEntry("rag", 5, 1, 2),
                    new LootEntry("tshirt", 4, 1, 1), new LootEntry("jeans", 3, 1, 1), new LootEntry("cap", 3, 1, 1),
                    new LootEntry("sneakers", 3, 1, 1), new LootEntry("gloves", 2, 1, 1), new LootEntry("boots", 1, 1, 1) } },
            };
        }

        /// <summary>Fills <paramref name="dest"/> with a random draw from the container's loot table.</summary>
        public static void RollLoot(ContainerType type, System.Random rng, Inventory dest)
        {
            EnsureInit();
            if (s_Tables == null || !s_Tables.TryGetValue(type, out LootTable table)) return;

            int rolls = rng.Next(table.MinRolls, table.MaxRolls + 1);
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
                        if (def != null && n > 0) dest.Add(def, n, enforceWeight: false);
                        break;
                    }
                    pick -= e.Weight;
                }
            }
        }
        #endregion
    }
}
