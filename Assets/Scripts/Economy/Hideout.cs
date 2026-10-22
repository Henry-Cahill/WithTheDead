using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>One level of a hideout module: what it costs to build and what it gives.</summary>
    public sealed class ModuleLevel
    {
        public int Level;
        public int Money;
        public (string Id, int Count)[] Items = new (string, int)[0];
        public (string Module, int Level)[] Requires = new (string, int)[0];
        public int MinCharacterLevel = 1;
        public string Effect;
    }

    public sealed class ModuleDef
    {
        public string Id, Name, Blurb;
        public bool NeedsPower;
        public readonly List<ModuleLevel> Levels = new List<ModuleLevel>();
    }

    /// <summary>A craft: inputs into outputs, over real time, at a module of some level.</summary>
    public sealed class Recipe
    {
        public string Id, Module;
        public int ModuleLevel = 1;
        public (string Id, int Count)[] Inputs = new (string, int)[0];
        public (string Id, int Count)[] Outputs = new (string, int)[0];
        public float Minutes;                 // real minutes
        public bool Field;                    // can be crafted from the backpack in the field (Metro)
    }

    /// <summary>
    /// The hideout: modules built from loot that run crafts in real time — the best item sink in the genre, and
    /// the reason no piece of junk is ever worthless. A generator powers the rest (it burns fuel while the
    /// hideout works); the data farm fills drives the brokers buy; the water collector and the animal pen
    /// produce on their own; the stash and vault grow your storage and your secure pouch.
    /// </summary>
    public static class Hideout
    {
        public static readonly List<ModuleDef> Modules = new List<ModuleDef>();
        public static readonly List<Recipe> Recipes = new List<Recipe>();

        static Hideout()
        {
            ModuleDef gen = M("generator", "Generator", "Powers the workbench, lab, medstation and data farm. Burns fuel.", false);
            L(gen, 1, 1200, new[] { ("scrap", 4), ("wires", 2) }, null, 1, "Power for every module (burns a gas can every 6 hours)");
            L(gen, 2, 6000, new[] { ("fuseblock", 2), ("carbattery", 1) }, new[] { ("workbench", 1) }, 5, "Burns fuel 30% slower");

            ModuleDef bench = M("workbench", "Workbench", "Ammunition, gun parts, cleaning and repairs. Field crafts need none of this.", true);
            L(bench, 1, 800, new[] { ("scrap", 3), ("plank", 4) }, new[] { ("generator", 1) }, 1, "Ammo and mod crafts; clean guns fully");
            L(bench, 2, 5000, new[] { ("scrap", 8), ("circuitboard", 2) }, null, 6, "Advanced crafts; repairs restore more");
            L(bench, 3, 18000, new[] { ("circuitboard", 4), ("fuseblock", 2), ("goldchain", 2) }, null, 12, "Armour-piercing rounds and suppressors");

            ModuleDef med = M("medstation", "Medstation", "Heals you between raids and makes medicine.", true);
            L(med, 1, 1500, new[] { ("bandage", 4), ("disinfectant", 2) }, new[] { ("generator", 1) }, 1, "Heal fully overnight; craft field kits");
            L(med, 2, 7000, new[] { ("ivkit", 2), ("suturekit", 2) }, null, 6, "Craft surgical kits and blood bags");

            ModuleDef water = M("watercollector", "Water Collector", "Catches and filters rain: a clean bottle every few hours.", false);
            L(water, 1, 1000, new[] { ("tincan", 3), ("emptybottle", 2), ("rope", 1) }, null, 1, "1 clean water bottle every 4 hours");
            L(water, 2, 4000, new[] { ("purifytabs", 5), ("scrap", 4) }, null, 5, "Every 2 hours");

            ModuleDef farm = M("datafarm", "Data Farm", "Racks of salvaged boards that fill encrypted drives for the brokers.", true);
            L(farm, 1, 9000, new[] { ("circuitboard", 4), ("wires", 4) }, new[] { ("generator", 2) }, 8, "1 data drive every 8 hours");
            L(farm, 2, 25000, new[] { ("circuitboard", 8), ("fuseblock", 3) }, null, 14, "Every 5 hours");

            ModuleDef lab = M("chemlab", "Chemistry Lab", "Filters, anti-rad and purification from reagents.", true);
            L(lab, 1, 2500, new[] { ("chemicals", 6), ("emptybottle", 3) }, new[] { ("generator", 1) }, 3, "Craft filters, purification tabs, anti-rad");
            L(lab, 2, 9000, new[] { ("chemicals", 10), ("circuitboard", 2) }, null, 9, "Psi-block and morphine");

            ModuleDef pen = M("animalpen", "Animal Pen", "A few hens and a goat behind the bar. Feed them; they feed you.", false);
            L(pen, 1, 1500, new[] { ("plank", 8), ("nails", 6), ("animalfeed", 2) }, null, 2, "Two hens: eggs while they're fed");
            L(pen, 2, 5000, new[] { ("plank", 10), ("animalfeed", 4), ("rope", 2) }, null, 6, "A milking goat");

            ModuleDef stash = M("stash", "Stash", "Shelving in the back room. More rows, more room.", false);
            L(stash, 1, 0, new (string, int)[0], null, 1, "14 rows");
            L(stash, 2, 6000, new[] { ("plank", 10), ("nails", 10) }, null, 4, "20 rows");
            L(stash, 3, 20000, new[] { ("scrap", 10), ("combolock", 1) }, null, 10, "28 rows");
            L(stash, 4, 60000, new[] { ("scrap", 20), ("goldchain", 4) }, null, 18, "36 rows");

            ModuleDef vault = M("vault", "Pouch Vault", "Upgrades the secure pouch whose contents survive death.", false);
            L(vault, 1, 9000, new[] { ("leathergloves", 1), ("thread", 4), ("goldchain", 1) }, new[] { ("stash", 2) }, 6, "Secure Pouch Mk II (3x2)");
            L(vault, 2, 30000, new[] { ("combolock", 2), ("goldchain", 3) }, new[] { ("stash", 3) }, 12, "Secure Case (3x3)");
            L(vault, 3, 90000, new[] { ("goldchain", 6), ("datadrive", 4) }, new[] { ("stash", 4) }, 20, "Secure Case Mk II (4x3)");

            ModuleDef intel = M("intel", "Intel Centre", "Radios, maps and contacts: insurance comes back sooner, scav runs come round faster, the flea market opens.", true);
            L(intel, 1, 6000, new[] { ("radio_walkie", 1), ("map_zone", 2) }, new[] { ("generator", 1) }, 5, "Scav cooldown -25%; flea market access");
            L(intel, 2, 20000, new[] { ("circuitboard", 4), ("gps", 1) }, null, 12, "Insurance returns a day sooner");

            ModuleDef rest = M("restspace", "Rest Space", "A cot and a heater. You recover faster between raids.", false);
            L(rest, 1, 1200, new[] { ("plank", 6), ("rag", 4) }, null, 1, "Daily healing doubled");

            // Crafts.
            R("ammo9mm_craft", "workbench", 1, 45f, new[] { ("scrap", 1), ("chemicals", 1) }, new[] { ("ammo9mm", 30) });
            R("ammo545_craft", "workbench", 2, 70f, new[] { ("scrap", 2), ("chemicals", 1) }, new[] { ("ammo545_ps", 30) });
            R("ammo545bp_craft", "workbench", 3, 120f, new[] { ("scrap", 2), ("chemicals", 2), ("circuitboard", 1) }, new[] { ("ammo545_bp", 20) });
            R("slug_craft", "workbench", 2, 50f, new[] { ("scrap", 1), ("chemicals", 1) }, new[] { ("slug12", 10) });
            R("suppressor_craft", "workbench", 3, 180f, new[] { ("scrap", 4), ("tincan", 2) }, new[] { ("mod_suppressor9", 1) });
            R("bolt_craft", "workbench", 1, 10f, new[] { ("scrap", 1) }, new[] { ("bolt", 15) });
            R("cleaningkit_craft", "workbench", 1, 30f, new[] { ("rag", 2), ("chemicals", 1) }, new[] { ("cleaningkit", 1) });
            R("fieldkit_craft", "medstation", 1, 40f, new[] { ("bandage", 2), ("pills", 1), ("disinfectant", 1) }, new[] { ("fieldkit", 1) });
            R("surgical_craft", "medstation", 2, 150f, new[] { ("suturekit", 1), ("tweezers", 1), ("fieldkit", 1) }, new[] { ("surgicalkit", 1) });
            R("filter_lab", "chemlab", 1, 60f, new[] { ("chemicals", 2), ("tincan", 1) }, new[] { ("gasmaskfilter", 1) });
            R("purify_lab", "chemlab", 1, 30f, new[] { ("chemicals", 1) }, new[] { ("purifytabs", 5) });
            R("antirad_lab", "chemlab", 1, 90f, new[] { ("chemicals", 2), ("iodine", 1) }, new[] { ("antirad", 1) });
            R("psiblock_lab", "chemlab", 2, 120f, new[] { ("chemicals", 3), ("herbs", 2) }, new[] { ("psiblock", 2) });
            R("morphine_lab", "chemlab", 2, 140f, new[] { ("chemicals", 3), ("pills", 2) }, new[] { ("morphine", 1) });

            // Field crafts (Metro): two resources, from the backpack, anywhere.
            R("field_filter", null, 0, 0.1f, new[] { ("chemicals", 1), ("scrap", 1) }, new[] { ("gasmaskfilter", 1) }, field: true);
            R("field_medkit", null, 0, 0.1f, new[] { ("chemicals", 2), ("rag", 1) }, new[] { ("bandage", 2) }, field: true);
            R("field_ammo", null, 0, 0.1f, new[] { ("chemicals", 1), ("scrap", 2) }, new[] { ("ammo9mm", 12) }, field: true);
            R("field_bolts", null, 0, 0.1f, new[] { ("scrap", 1) }, new[] { ("bolt", 8) }, field: true);
        }

        private static ModuleDef M(string id, string name, string blurb, bool power)
        {
            var d = new ModuleDef { Id = id, Name = name, Blurb = blurb, NeedsPower = power };
            Modules.Add(d);
            return d;
        }

        private static void L(ModuleDef d, int level, int money, (string, int)[] items, (string, int)[] requires, int minLevel, string effect)
            => d.Levels.Add(new ModuleLevel { Level = level, Money = money, Items = items ?? new (string, int)[0], Requires = requires ?? new (string, int)[0], MinCharacterLevel = minLevel, Effect = effect });

        private static void R(string id, string module, int level, float minutes, (string, int)[] inputs, (string, int)[] outputs, bool field = false)
            => Recipes.Add(new Recipe { Id = id, Module = module, ModuleLevel = level, Minutes = minutes, Inputs = inputs, Outputs = outputs, Field = field });

        /// <summary>Returns the module definition with the given identifier.</summary>
        public static ModuleDef Get(string id) => Modules.Find(m => m.Id == id);
        /// <summary>Returns the recipe definition with the given identifier.</summary>
        public static Recipe GetRecipe(string id) => Recipes.Find(r => r.Id == id);

        /// <summary>Initialises all hideout modules for the given character profile.</summary>
        public static void InitModules(CharacterProfile c)
        {
            foreach (ModuleDef m in Modules) c.Module(m.Id);
            c.Module("stash").Level = Mathf.Max(1, c.Module("stash").Level);
        }

        /// <summary>Returns the level of a module for a character, or 0 if none.</summary>
        public static int LevelOf(CharacterProfile c, string module) => c != null ? c.Module(module).Level : 0;

        /// <summary>True while the generator is built and fuelled (powered modules work only then).</summary>
        public static bool Powered(CharacterProfile c) => c != null && LevelOf(c, "generator") > 0 && c.Module("generator").Fuel > 0f;

        #region Building
        /// <summary>Determines if a module can be upgraded, returning false with a reason if not.</summary>
        public static bool CanUpgrade(CharacterProfile c, ModuleDef m, PlayerInventory carried, out string reason)
        {
            reason = null;
            int cur = LevelOf(c, m.Id);
            ModuleLevel next = m.Levels.Find(l => l.Level == cur + 1);
            if (next == null) { reason = "Fully built"; return false; }
            if (c.Level < next.MinCharacterLevel) { reason = "Needs character level " + next.MinCharacterLevel; return false; }
            foreach ((string mod, int lvl) in next.Requires)
                if (LevelOf(c, mod) < lvl) { reason = "Needs " + Get(mod).Name + " level " + lvl; return false; }
            if (Money.Available(carried) < next.Money) { reason = "Needs " + Money.Format(next.Money); return false; }
            foreach ((string id, int n) in next.Items)
                if (Trading.CountOwned(id, carried) < n) { reason = "Needs " + n + "x " + Trading.NameOf(id); return false; }
            return true;
        }

        /// <summary>Attempts to upgrade a module, applying costs and effects; returns success and a message.</summary>
        public static bool Upgrade(CharacterProfile c, ModuleDef m, PlayerInventory carried, out string message)
        {
            if (!CanUpgrade(c, m, carried, out message)) return false;
            ModuleLevel next = m.Levels.Find(l => l.Level == LevelOf(c, m.Id) + 1);
            Money.TrySpend(next.Money, carried);
            foreach ((string id, int n) in next.Items) Trading.TakeOwned(id, n, carried);
            c.Module(m.Id).Level = next.Level;
            ApplyLevelEffects(c, m.Id, next.Level);
            message = m.Name + " is now level " + next.Level + ": " + next.Effect;
            ProfileStore.Save();
            return true;
        }

        /// <summary>Applies the effects of a newly upgraded module level to the character.</summary>
        private static void ApplyLevelEffects(CharacterProfile c, string module, int level)
        {
            switch (module)
            {
                case "stash":
                    int[] rows = { 14, 14, 20, 28, 36 };
                    ProfileStore.SetStashRows(rows[Mathf.Clamp(level, 0, 4)]);
                    break;
                case "vault":
                    c.PouchLevel = Mathf.Max(c.PouchLevel, level);
                    UpgradeKitPouch(c);
                    break;
                case "generator":
                    if (c.Module("generator").Fuel <= 0f) c.Module("generator").Fuel = 6f;
                    break;
                case "animalpen":
                    if (level == 1) { c.Animals.Add(new AnimalState { Kind = "hen", Name = "Dot", LastTickUnix = ProfileStore.NowUnix }); c.Animals.Add(new AnimalState { Kind = "hen", Name = "Pip", LastTickUnix = ProfileStore.NowUnix }); }
                    if (level == 2) c.Animals.Add(new AnimalState { Kind = "goat", Name = "Marta", LastTickUnix = ProfileStore.NowUnix });
                    break;
            }
        }

        // The worn pouch in the kit grows with the vault.
        /// <summary>Ensures the player's kit has a pouch that matches the vault level.</summary>
        private static void UpgradeKitPouch(CharacterProfile c)
        {
            if (c.Kit == null) c.Kit = new RaidKit { Version = 1 };
            string id = ZoneContent.PouchIdForLevel(c.PouchLevel);
            c.Kit.Clothing.RemoveAll(p => p.Slot == (int)ClothingSlot.Pouch);
            c.Kit.Clothing.Add(new RaidKitPiece { Slot = (int)ClothingSlot.Pouch, Id = id });
        }
        #endregion

        #region Crafting and production
        /// <summary>Checks if a recipe can be crafted given the character, inventory and module constraints.</summary>
        public static bool CanCraft(CharacterProfile c, Recipe r, PlayerInventory carried, out string reason)
        {
            reason = null;
            if (!r.Field)
            {
                ModuleDef m = Get(r.Module);
                if (LevelOf(c, r.Module) < r.ModuleLevel) { reason = "Needs " + m.Name + " level " + r.ModuleLevel; return false; }
                if (m.NeedsPower && !Powered(c)) { reason = "The generator is off (no fuel)"; return false; }
                if (c.Crafts.Exists(j => j.ModuleId == r.Module)) { reason = m.Name + " is busy"; return false; }
            }
            foreach ((string id, int n) in r.Inputs)
                if (Trading.CountOwned(id, carried) < n) { reason = "Needs " + n + "x " + Trading.NameOf(id); return false; }
            return true;
        }

        /// <summary>Starts a craft (inputs are consumed now; outputs arrive when it finishes).</summary>
        
        public static bool StartCraft(CharacterProfile c, Recipe r, PlayerInventory carried, out string message)
        {
            if (!CanCraft(c, r, carried, out message)) return false;
            foreach ((string id, int n) in r.Inputs) Trading.TakeOwned(id, n, carried);
            long now = ProfileStore.NowUnix;
            c.Crafts.Add(new CraftJob { RecipeId = r.Id, ModuleId = r.Module, StartUnix = now, DoneUnix = now + (long)(r.Minutes * 60f) });
            message = "Started: " + Describe(r) + " (" + r.Minutes.ToString("0") + " min)";
            ProfileStore.Save();
            return true;
        }

        /// <summary>A field craft from the pack (instant): inputs from the pack, outputs into it.</summary>
        /// <summary>Executes an instant field craft from the pack.</summary>
        public static bool FieldCraft(Recipe r, PlayerInventory inv, out string message)
        {
            message = null;
            if (r == null || !r.Field || inv == null) return false;
            foreach ((string id, int n) in r.Inputs)
                if (Trading.Count(inv.Inventory, id) < n) { message = "Needs " + n + "x " + Trading.NameOf(id); return false; }
            foreach ((string id, int n) in r.Inputs) Trading.Take(inv.Inventory, id, n);
            foreach ((string id, int n) in r.Outputs) inv.Inventory.Add(ItemDatabase.Get(id), n, enforceWeight: false);
            GameEvents.RaiseItemCrafted(r.Outputs[0].Id, r.Outputs[0].Count);
            message = "Crafted " + Describe(r);
            return true;
        }

        /// <summary>Creates a human‑readable description of a recipe’s outputs.</summary>
        public static string Describe(Recipe r)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < r.Outputs.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(r.Outputs[i].Count).Append("x ").Append(Trading.NameOf(r.Outputs[i].Id));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Advances everything that runs on its own: finished crafts deliver to the stash, the generator burns
        /// fuel, the data farm and water collector produce, the animals eat and lay. Called whenever the hub
        /// opens (real time passes between sessions, as Tarkov's hideout does).
        /// </summary>
        /// <summary>Advances all hideout processes: crafts, generator fuel, farms, water, animals; returns notes.</summary>
        public static List<string> Tick(CharacterProfile c)
        {
            var notes = new List<string>();
            if (c == null) return notes;
            long now = ProfileStore.NowUnix;
            Inventory stash = ProfileStore.StashInventory;

            for (int i = c.Crafts.Count - 1; i >= 0; i--)
            {
                CraftJob j = c.Crafts[i];
                if (now < j.DoneUnix) continue;
                Recipe r = GetRecipe(j.RecipeId);
                if (r != null)
                {
                    foreach ((string id, int n) in r.Outputs) stash.Add(ItemDatabase.Get(id), n, enforceWeight: false);
                    notes.Add("Craft finished: " + Describe(r));
                    // New flavour notes – style reference: notes.Add("The generator ran dry. Pour in a gas can.");
                    notes.Add("A distant howl pierces the silent night.");
                    notes.Add("Snow drifts against the hideout's shutters, muffling every footstep.");
                    GameEvents.RaiseItemCrafted(r.Outputs[0].Id, r.Outputs[0].Count);
                }
                c.Crafts.RemoveAt(i);
            }

            HideoutModuleState gen = c.Module("generator");
            float hours = HoursSinceTick(c, now);
            if (gen.Level > 0 && hours > 0f)
            {
                float burn = hours / (gen.Level >= 2 ? 8.5f : 6f);
                gen.Fuel = Mathf.Max(0f, gen.Fuel - burn);
                if (gen.Fuel <= 0f) notes.Add("The generator ran dry. Pour in a gas can.");
                if (gen.Fuel > 0f) notes.Add("The generator’s low hum warms the cramped walls.");
            }

            int farmLvl = LevelOf(c, "datafarm");
            if (farmLvl > 0 && Powered(c)) Produce(c, "datafarm", farmLvl >= 2 ? 5f : 8f, "datadrive", stash, notes, hours);
            int waterLvl = LevelOf(c, "watercollector");
            if (waterLvl > 0) Produce(c, "watercollector", waterLvl >= 2 ? 2f : 4f, "waterbottle", stash, notes, hours);
            // New winter flavour notes after production
            if (farmLvl > 0 && Powered(c)) notes.Add("Condensation gathers on the data farm’s rusted racks.");
            if (waterLvl > 0) notes.Add("Ice clings to the water collector’s pipes, slowing each drip.");
            TickAnimals(c, stash, notes, hours);
            // New animal‑related winter notes
            if (c.Animals.Count > 0) notes.Add("The hens shuffle in the cold, their feathers dusted with frost.");
            if (c.Animals.Exists(a => a.Kind == "goat")) notes.Add("Marta’s milk tastes of pine and frost.");

            SetLastTick(c, now);
            if (notes.Count > 0) ProfileStore.Save();
            return notes;
        }

        // Production accumulates fractional hours in the module's Fuel field for modules that burn none.
        /// <summary>Handles production for a module that generates an item on a timed interval.</summary>
        private static void Produce(CharacterProfile c, string module, float everyHours, string item, Inventory stash, List<string> notes, float hours)
        {
            HideoutModuleState m = c.Module(module);
            m.Fuel += hours;
            int made = 0;
            while (m.Fuel >= everyHours && made < 12) { m.Fuel -= everyHours; made++; }
            if (made <= 0) return;
            stash.Add(ItemDatabase.Get(item), made, enforceWeight: false);
            notes.Add(Get(module).Name + ": +" + made + " " + Trading.NameOf(item));
        }

        /// <summary>Updates animal hunger, production, and adds eggs/milk to stash.</summary>
        private static void TickAnimals(CharacterProfile c, Inventory stash, List<string> notes, float hours)
        {
            if (c.Animals.Count == 0 || hours <= 0f) return;
            int eggs = 0, milk = 0;
            foreach (AnimalState a in c.Animals)
            {
                a.Fed = Mathf.Max(0f, a.Fed - hours / 24f);
                if (a.Fed <= 0f && Trading.Take(stash, "animalfeed", 1) == 0) a.Fed = 1f;   // the pen eats from the stash
                if (a.Fed <= 0f) continue;
                a.Produce += hours / (a.Kind == "goat" ? 12f : 8f);
                while (a.Produce >= 1f) { a.Produce -= 1f; if (a.Kind == "goat") milk++; else eggs++; }
            }
            if (eggs > 0) { stash.Add(ItemDatabase.Get("egg"), eggs, enforceWeight: false); notes.Add("The hens laid " + eggs + " egg" + (eggs == 1 ? "" : "s") + "."); }
            if (milk > 0) { stash.Add(ItemDatabase.Get("goatmilk"), milk, enforceWeight: false); notes.Add("Marta gave " + milk + " milk."); }
        }

        private const string TickFlag = "hideout_tick:";
        private static float HoursSinceTick(CharacterProfile c, long now)
        {
            string f = c.Flags.Find(x => x.StartsWith(TickFlag));
            if (f == null || !long.TryParse(f.Substring(TickFlag.Length), out long last)) return 0f;
            return Mathf.Clamp((now - last) / 3600f, 0f, 24f * 14f);
        }

        private static void SetLastTick(CharacterProfile c, long now)
        {
            c.Flags.RemoveAll(x => x.StartsWith(TickFlag));
            c.Flags.Add(TickFlag + now);
        }

        /// <summary>Pours fuel into the generator (6 hours per can).</summary>
        /// <summary>Pours fuel into the generator (6 hours per can).</summary>
        public static bool Refuel(CharacterProfile c, PlayerInventory carried, out string message)
        {
            message = null;
            if (LevelOf(c, "generator") <= 0) { message = "Build the generator first."; return false; }
            if (Trading.TakeOwned("gascan", 1, carried) <= 0) { message = "You need a gas can."; return false; }
            c.Module("generator").Fuel += 6f;
            message = "Generator fuelled (" + c.Module("generator").Fuel.ToString("0.#") + " h).";
            ProfileStore.Save();
            return true;
        }
        #endregion
    }
}
