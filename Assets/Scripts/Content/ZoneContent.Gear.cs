using UnityEngine;

namespace CloneZ
{
    public sealed partial class ZoneContent
    {
        /// <summary>Every artifact id (their effects live in <see cref="ArtifactCatalog"/>; descriptions never tell).</summary>
        public static readonly string[] ArtifactIds =
        {
            "art_hollowstone", "art_coldspark", "art_emberheart", "art_mosscalm", "art_needlebloom", "art_driftshell",
            "art_lodeknot", "art_whisperglass", "art_greywax", "art_saltlung", "art_sunkenstar",
        };

        public static readonly string[] BloodBags = { "bloodbag_o_neg", "bloodbag_o_pos", "bloodbag_a_pos", "bloodbag_b_pos", "bloodbag_ab_pos", "bloodbag_a_neg" };

        #region Zone gear: bolts, detectors, artifacts
        private static void RegisterZoneGear(ContentBuilder b)
        {
            ItemDef bolt = Tool(b, "bolt", "Bolts", 0.04f, 30, "Thrown ahead to find the safe path. Anomalies react to them before they react to you.", "Throw");
            bolt.Tint = new Color(0.62f, 0.62f, 0.64f);
            Tool(b, "detector_echo", "Echo Detector", 0.4f, 1, "Tier 1. Clicks faster as an artifact gets closer. Blind to the rarer ones.", "Switch on", 2, 1);
            Tool(b, "detector_bearing", "Bearing Detector", 0.5f, 1, "Tier 2. A needle swings toward the nearest artifact it can sense.", "Switch on", 2, 1);
            Tool(b, "detector_lattice", "Lattice Detector", 0.6f, 1, "Tier 3. A lattice screen that plots artifacts — even the ones that hide.", "Switch on", 2, 1);
            Tool(b, "leadcase", "Lead-lined Case", 1.6f, 1, "Carry artifacts inside and they stop leaking into you.", null, 2, 2);

            for (int i = 0; i < ArtifactIds.Length; i++)
            {
                string id = ArtifactIds[i];
                ItemDef a = b.Item(id, ArtifactCatalog.NameOf(id), ItemCategory.Misc, 0.5f, 1, 1, 1,
                    id == "art_sunkenstar"
                        ? "The prize every squad in the Zone is after tonight. It hums. It shouldn't."
                        : "Alien litter. Nobody knows what it does until someone carries it long enough to find out.");
                a.CustomUse = true;
                a.UseVerb = "Wear on belt";
                a.Tint = ArtifactCatalog.ColorOf(id);
            }

            Tool(b, "pda", "Dead Runner's PDA", 0.3f, 1, "Somebody's last messages. Sometimes a stash location, sometimes a route.", "Read");
            Tool(b, "map_zone", "Zone Map", 0.1f, 1, "A hand-annotated map of one Zone. No dot says where you are — that part is up to you.", "Unfold", 1, 1);
            Tool(b, "compass", "Compass", 0.1f, 1, "Points north. In the Zone, usually.", null);
            Tool(b, "gps", "GPS Unit", 0.3f, 1, "Rare pre-Passing receiver. Puts a dot on your map.", "Switch on");
            Tool(b, "watch_wrist", "Wristwatch", 0.05f, 1, "Time, a filter timer and a little lamp that glows when you're easy to see.", null);
            Tool(b, "lodestar_page", "Burned Journal Page", 0.02f, 5, "Part of a pilgrim's journal. It mentions a light at the bottom of the Spillway.", "Read");
            Tool(b, "lodestar_hymnal", "Choir Hymnal", 0.2f, 1, "The Hollow Choir sing to something. The verses read like directions.", "Read");
            Tool(b, "lodestar_photo", "Survey Photograph", 0.02f, 1, "An aerial photograph of the dam, one spot circled in grease pencil.", "Read");
        }
        #endregion

        #region Medical (limbs, blood, diseases, radiation)
        private static void RegisterMedical(ContentBuilder b)
        {
            Med(b, "splint", "Splint", 0.2f, 4, "Sets a broken arm or leg so it can take weight again.", "Apply");
            Med(b, "suturekit", "Suture Kit", 0.15f, 3, "Needle and thread for a deep wound. Hurts.", "Stitch");
            Med(b, "tweezers", "Tweezers", 0.05f, 1, "Pulls glass shards and bullet fragments out of a wound.", "Use");
            Med(b, "tourniquet", "Tourniquet", 0.1f, 4, "Stops a heavy bleed on a limb. Leave it on too long and you'll lose the limb's strength.", "Apply");
            Med(b, "hemostat", "Hemostatic Gauze", 0.1f, 4, "Packs a heavy bleed anywhere on the body.", "Apply");
            Med(b, "fieldkit", "Field Medkit", 0.5f, 1, "Dressings and painkillers. Patches limbs and stops light bleeding.", "Use", 1, 2);
            Med(b, "surgicalkit", "Surgical Kit", 1.2f, 1, "Restores a destroyed limb enough to use it again.", "Operate", 2, 1);
            Med(b, "morphine", "Morphine Injector", 0.05f, 3, "Kills the pain. All of it, for a while.", "Inject");
            Med(b, "ivkit", "IV Kit", 0.2f, 3, "Lines and needles for a blood transfusion.", null);
            Med(b, "bloodtest", "Blood Test Kit", 0.1f, 3, "Tells you a blood type. Use it on yourself or someone else.", "Test");
            Med(b, "bloodbag_empty", "Empty Blood Bag", 0.1f, 3, "Draw a pint from a willing donor (and an IV kit).", null);
            for (int i = 0; i < BloodBags.Length; i++)
            {
                string type = BloodTypes.FromBagId(BloodBags[i]);
                Med(b, BloodBags[i], "Blood Bag (" + type + ")", 0.5f, 1, "A pint of " + type + " blood. The wrong type will kill you.", "Transfuse");
            }
            Med(b, "tetracycline", "Tetracycline", 0.05f, 6, "Antibiotic for cholera and other gut infections.", "Take");
            Med(b, "charcoal", "Charcoal Tablets", 0.05f, 6, "Settles a poisoned stomach.", "Take");
            Med(b, "vitamins", "Vitamins", 0.05f, 10, "Helps shake off a cold, and keeps one away.", "Take");
            Med(b, "purifytabs", "Purification Tablets", 0.02f, 10, "Makes tainted water safe without boiling it.", "Drop in water");
            Med(b, "antirad", "Anti-rad Injector", 0.05f, 3, "Flushes radiation out of the body. Leaves you nauseous.", "Inject");
            Med(b, "iodine", "Potassium Iodide", 0.03f, 10, "Shields the body from radiation for a while.", "Take");
            Med(b, "psiblock", "Psi-block Pills", 0.03f, 6, "Dulls the mind. The Puppeteer's voice slides off it.", "Take");
            Med(b, "vodka", "Cheap Vodka", 0.7f, 1, "Old runners swear it flushes radiation. It definitely flushes judgement.", "Drink", 1, 2);
        }

        private static ItemDef Med(ContentBuilder b, string id, string name, float weight, int stack, string desc, string verb, int w = 1, int h = 1)
        {
            ItemDef d = b.Item(id, name, ItemCategory.Medical, weight, stack, w, h, desc);
            if (!string.IsNullOrEmpty(verb)) { d.CustomUse = true; d.UseVerb = verb; }
            return d;
        }
        #endregion

        #region Survival, tools and crafting resources
        private static void RegisterSurvival(ContentBuilder b)
        {
            Mat(b, "chemicals", "Chemicals", 0.3f, 20, "Reagents scavenged from labs and garages. Half of every field craft.");
            Mat(b, "scrap", "Scrap Metal", 0.4f, 20, "Springs, screws and plate. The other half of every field craft.");
            Mat(b, "circuitboard", "Circuit Board", 0.15f, 10, "Salvaged electronics. Detectors, drones and data farms run on these.");
            Mat(b, "wires", "Copper Wire", 0.2f, 10, "Stripped copper wire.");
            Tool(b, "cleaningkit", "Gun Cleaning Kit", 0.3f, 1, "Rods, brushes and oil. Cleans the fouling out of a gun.", "Clean gun");
            Tool(b, "lighter", "Lighter", 0.05f, 1, "A little flame: light to see by, and to start a cooking fire.", "Flick");
            Tool(b, "handcrank", "Hand-crank Charger", 0.6f, 1, "Wind it to bring a flat battery back to life. Noisy.", "Crank");
            Tool(b, "fishingrod", "Fishing Rod", 1.1f, 1, "Cast it at open water.", "Fish", 4, 1);
            Mat(b, "bait", "Bait Worms", 0.02f, 20, "Fish will bite on these.");
            Food(b, "rawfish", "Raw Fish", 0.5f, 0.18f, "cookedfish", true, 24f);
            Food(b, "cookedfish", "Cooked Fish", 0.4f, 0.25f, null, false, 48f);
            Tool(b, "snare", "Snare Trap", 0.4f, 3, "Set it in the woods and come back later.", "Set");
            Food(b, "rawrabbit", "Raw Rabbit", 0.8f, 0.2f, "cookedrabbit", true, 24f);
            Food(b, "cookedrabbit", "Cooked Rabbit", 0.6f, 0.3f, null, false, 48f);
            Food(b, "berries", "Wild Berries", 0.1f, 0.06f, null, false, 36f);
            Food(b, "mushrooms", "Forest Mushrooms", 0.1f, 0.07f, null, false, 30f);
            Med(b, "herbs", "Medicinal Herbs", 0.05f, 10, "Chewed, they ease pain a little and settle a stomach.", "Chew");
            Food(b, "mutantmeat", "Mutant Meat", 1.0f, 0.22f, "cookedmutantmeat", true, 20f);
            Food(b, "cookedmutantmeat", "Cooked Mutant Meat", 0.8f, 0.32f, null, false, 40f);
            Food(b, "egg", "Egg", 0.06f, 0.06f, null, false, 96f);
            Food(b, "goatmilk", "Goat Milk", 0.5f, 0.05f, null, false, 48f, 0.18f);
            Mat(b, "animalfeed", "Animal Feed", 0.5f, 10, "Grain for the hideout's animals.");
            Tool(b, "handcuffs", "Handcuffs", 0.3f, 2, "Restrain someone who can't fight back — or has their hands up.", null);
            Tool(b, "combolock", "Combination Lock", 0.4f, 2, "Locks a gate or a door you built.", null);
            Tool(b, "breachcharge", "Breaching Charge", 1.2f, 3, "Blows a door, gate or wall section open. Everyone hears it.", "Arm", 2, 1);
            Tool(b, "radio_walkie", "Walkie-talkie", 0.4f, 1, "Talk to anyone on the same channel, whatever the distance.", "Change channel");
            Tool(b, "drone_fpv", "FPV Drone", 1.4f, 1, "A flying camera you pilot by hand. Can carry a grenade.", "Launch", 2, 2);
            Tool(b, "drone_ground", "Ground Drone", 2.6f, 1, "A tracked scout with a camera. Quiet, slow, brave.", "Deploy", 3, 2);
            Tool(b, "tablet", "Field Tablet", 0.6f, 1, "Loadouts, gun builds and the map, on a cracked screen.", "Open", 2, 1);
            Tool(b, "grenade", "Frag Grenade", 0.6f, 4, "Pull, throw, count.", "Throw");
            Tool(b, "carkey", "Car Key", 0.02f, 1, "Opens and starts one particular car.", null);
            Mat(b, "carwheel", "Car Wheel", 9f, 1, "A whole wheel, tyre and all.", 3, 3);
            Mat(b, "carbattery", "Car Battery", 12f, 1, "Lead-acid. Might still hold a charge.", 2, 2);
            Mat(b, "sparkplug", "Spark Plug", 0.1f, 4, "No spark, no drive.");
            Mat(b, "radiator", "Radiator", 4f, 1, "Keeps an engine from cooking itself.", 3, 2);
            ItemDef gas = Tool(b, "gascan", "Gas Can", 4.5f, 1, "Fuel for a car or a generator. Full, for now.", "Pour", 2, 2);
            gas.MaxCondition = 20;           // litres, tracked as the can's "charge"
            gas.ConditionLossChance = 1;

            string[] skills = { "aiming", "firstaid", "mechanics", "carpentry", "foraging", "fishing", "cooking", "metabolism" };
            string[] titles = { "Marksman's Notebook", "Field Medicine Vol. 1", "Engines for Idiots", "The Joinery Handbook",
                                "Edible Plants of the Zone", "The Patient Angler", "Cooking on Nothing", "Ration Discipline" };
            for (int i = 0; i < skills.Length; i++)
                Tool(b, "book_" + skills[i], titles[i], 0.4f, 1, "Reading it multiplies the experience you gain in one skill.", "Read", 1, 2);
        }

        private static ItemDef Mat(ContentBuilder b, string id, string name, float w, int stack, string desc, int sw = 1, int sh = 1)
            => b.Item(id, name, ItemCategory.Material, w, stack, sw, sh, desc);

        private static ItemDef Tool(ContentBuilder b, string id, string name, float w, int stack, string desc, string verb, int sw = 1, int sh = 1)
        {
            ItemDef d = b.Item(id, name, ItemCategory.Misc, w, stack, sw, sh, desc);
            if (!string.IsNullOrEmpty(verb)) { d.CustomUse = true; d.UseVerb = verb; }
            return d;
        }

        private static void Food(ContentBuilder b, string id, string name, float w, float hunger, string cooked, bool raw, float shelf, float thirst = 0f)
        {
            ItemDef d = b.Item(id, name, ItemCategory.Food, w, 4, 1, 1, null);
            if (b.InDatabase(id)) return;
            d.HungerRestore = hunger;
            d.ThirstRestore = thirst;
            d.Perishable = true;
            d.ShelfLifeHours = shelf;
            if (!string.IsNullOrEmpty(cooked)) { d.Cookable = true; d.CookedId = cooked; }
            d.RawHazard = raw;
            d.IsCookedFood = id.StartsWith("cooked");
            d.MaxStack = 4;
        }
        #endregion

        #region Economy: barter goods, data, quest items, secure pouches
        private static void RegisterEconomyItems(ContentBuilder b)
        {
            Mat(b, "goldchain", "Gold Chain", 0.05f, 5, "Barter goods. Fences love it.");
            Mat(b, "datadrive", "Encrypted Data Drive", 0.05f, 10, "The hideout's data farm fills these overnight. Brokers pay well.");
            Mat(b, "fuseblock", "Fuse Block", 0.3f, 5, "Industrial fuses. The generator needs them.");
            Mat(b, "institute_sample", "Sealed Sample Case", 0.8f, 3, "Institute property. Contents: Zone soil, probably.", 2, 1);
            Mat(b, "warden_tag", "Warden Dog Tag", 0.02f, 20, "Taken from a cordon trooper. Proof of something.");
            Mat(b, "fence_ledger", "Fence's Ledger", 0.3f, 1, "Names, debts and dates. Someone wants it back.");
            Mat(b, "zone_permit", "Zone Permit", 0.01f, 5, "A stamped pass. The Wardens let the bearer through the belt — once.");

            string[] pouchNames = { "Secure Pouch", "Secure Pouch Mk II", "Secure Case", "Secure Case Mk II" };
            int[] pw = { 2, 3, 3, 4 }, ph = { 2, 2, 3, 3 };
            for (int i = 0; i < 4; i++)
            {
                ItemDef p = b.Item("securepouch_" + (i + 1), pouchNames[i], ItemCategory.Clothing, 0.3f, 1, 2, 2,
                    "Whatever is inside survives your death. Its size grows as you progress.");
                if (b.InDatabase(p.Id)) continue;
                p.IsClothing = true;
                p.ClothingSlot = ClothingSlot.Pouch;
                p.Gear = GearKind.Container;
                p.GridW = pw[i];
                p.GridH = ph[i];
                p.ContainerCapacity = 2f + i * 1.5f;
                p.Tint = new Color(0.20f, 0.22f, 0.24f);
            }
        }

        /// <summary>The secure pouch item for a pouch level (0..3).</summary>
        public static string PouchIdForLevel(int level) => "securepouch_" + (Mathf.Clamp(level, 0, 3) + 1);
        #endregion

        #region Clothing as inventory
        // DayZ: every worn piece carries its own cargo. Jackets, coats and trousers get pocket grids sized by
        // their cut, unless the database already gives them a container.
        private static void PatchClothingPockets(ContentBuilder b)
        {
            void Pockets(string id, int w, int h, float kg) => b.Patch(id, d =>
            {
                if (d.Gear != GearKind.None || d.ContainerCapacity > 0f) return;
                d.Gear = GearKind.Container;
                d.GridW = w;
                d.GridH = h;
                d.ContainerCapacity = kg;
            });
            Pockets("camojacket", 2, 2, 2.5f);
            Pockets("parka", 2, 2, 2.5f);
            Pockets("jacket", 2, 1, 1.5f);
            Pockets("leatherjacket", 2, 1, 1.5f);
            Pockets("raincoat", 2, 1, 1.2f);
            Pockets("hoodie", 2, 1, 1f);
            Pockets("cargopants", 2, 2, 2.5f);
            Pockets("trousers", 2, 1, 1.5f);
            Pockets("jeans", 2, 1, 1.2f);
            Pockets("shorts", 1, 1, 0.5f);
        }
        #endregion

        #region Loot
        private static void RegisterLoot(ContentBuilder b)
        {
            // Gun lockers and the dead soldiers' corpses carry the military kit.
            b.Loot(ContainerType.GunLocker, "ak74", 3);
            b.Loot(ContainerType.GunLocker, "mosin", 2);
            b.Loot(ContainerType.GunLocker, "smg9", 3);
            b.Loot(ContainerType.GunLocker, "ammo545_ps", 6, 10, 30);
            b.Loot(ContainerType.GunLocker, "ammo545_bp", 2, 5, 15);
            b.Loot(ContainerType.GunLocker, Currency, 3, 10, 40);
            b.Loot(ContainerType.GunLocker, "ammo762r_lps", 3, 5, 15);
            b.Loot(ContainerType.GunLocker, "ammo9mm_ap", 3, 10, 25);
            b.Loot(ContainerType.GunLocker, "slug12", 2, 4, 10);
            b.Loot(ContainerType.GunLocker, "armor_vest_c2", 3);
            b.Loot(ContainerType.GunLocker, "armor_vest_c3", 2);
            b.Loot(ContainerType.GunLocker, "armor_plate_c4", 1);
            b.Loot(ContainerType.GunLocker, "helmet_c3", 2);
            b.Loot(ContainerType.GunLocker, "mod_reddot", 2);
            b.Loot(ContainerType.GunLocker, "mod_suppressor9", 1);
            b.Loot(ContainerType.GunLocker, "mod_foregrip", 2);
            b.Loot(ContainerType.GunLocker, "mod_mag_extended", 1);
            b.Loot(ContainerType.GunLocker, "grenade", 2, 1, 2);
            b.Loot(ContainerType.GunLocker, "flare", 2, 1, 2);
            b.Loot(ContainerType.GunLocker, "cleaningkit", 2);

            b.Loot(ContainerType.MedCabinet, "splint", 5, 1, 2);
            b.Loot(ContainerType.MedCabinet, "suturekit", 3);
            b.Loot(ContainerType.MedCabinet, "tweezers", 3);
            b.Loot(ContainerType.MedCabinet, "tourniquet", 3);
            b.Loot(ContainerType.MedCabinet, "hemostat", 3);
            b.Loot(ContainerType.MedCabinet, "fieldkit", 3);
            b.Loot(ContainerType.MedCabinet, "morphine", 2);
            b.Loot(ContainerType.MedCabinet, "ivkit", 2);
            b.Loot(ContainerType.MedCabinet, "bloodtest", 2);
            b.Loot(ContainerType.MedCabinet, "tetracycline", 3, 1, 3);
            b.Loot(ContainerType.MedCabinet, "charcoal", 3, 1, 3);
            b.Loot(ContainerType.MedCabinet, "vitamins", 3, 2, 5);
            b.Loot(ContainerType.MedCabinet, "purifytabs", 3, 2, 6);
            b.Loot(ContainerType.MedCabinet, "antirad", 2);
            b.Loot(ContainerType.MedCabinet, "iodine", 2, 1, 4);
            b.Loot(ContainerType.MedCabinet, "bloodbag_o_pos", 1);
            b.Loot(ContainerType.MedCabinet, "bloodbag_a_pos", 1);

            b.Loot(ContainerType.ToolBench, "scrap", 8, 1, 4);
            b.Loot(ContainerType.ToolBench, "chemicals", 6, 1, 3);
            b.Loot(ContainerType.ToolBench, "wires", 4, 1, 3);
            b.Loot(ContainerType.ToolBench, "circuitboard", 3);
            b.Loot(ContainerType.ToolBench, "sparkplug", 3);
            b.Loot(ContainerType.ToolBench, "cleaningkit", 2);
            b.Loot(ContainerType.ToolBench, "handcrank", 2);
            b.Loot(ContainerType.ToolBench, "gascan", 2);
            b.Loot(ContainerType.ToolBench, "fuseblock", 2);
            b.Loot(ContainerType.ToolBench, "combolock", 2);
            b.Loot(ContainerType.ToolBench, "book_mechanics", 1);
            b.Loot(ContainerType.ToolBench, "book_carpentry", 1);

            b.Loot(ContainerType.Desk, "pda", 2);
            b.Loot(ContainerType.Desk, "map_zone", 2);
            b.Loot(ContainerType.Desk, "compass", 2);
            b.Loot(ContainerType.Desk, "goldchain", 2);
            b.Loot(ContainerType.Desk, "circuitboard", 2);
            b.Loot(ContainerType.Desk, "lighter", 3);
            b.Loot(ContainerType.Desk, "bolt", 3, 3, 10);
            b.Loot(ContainerType.Desk, "book_aiming", 1);
            b.Loot(ContainerType.Desk, "book_firstaid", 1);
            b.Loot(ContainerType.Desk, "book_foraging", 1);
            b.Loot(ContainerType.Desk, "book_fishing", 1);
            b.Loot(ContainerType.Desk, "book_cooking", 1);
            b.Loot(ContainerType.Desk, "book_metabolism", 1);
            b.Loot(ContainerType.Desk, "radio_walkie", 1);
            b.Loot(ContainerType.Desk, "gps", 1);

            b.Loot(ContainerType.Pantry, "purifytabs", 2, 2, 5);
            b.Loot(ContainerType.Pantry, "bait", 2, 3, 8);
            b.Loot(ContainerType.Pantry, "animalfeed", 2, 1, 3);
            b.Loot(ContainerType.Wardrobe, "armor_vest_c2", 1);
            b.Loot(ContainerType.Wardrobe, "suit_runner", 1);
            b.Loot(ContainerType.Wardrobe, "helmet_c2", 1);
            b.Loot(ContainerType.Shelf, "bolt", 3, 5, 15);
            b.Loot(ContainerType.Shelf, "fishingrod", 1);
            b.Loot(ContainerType.Shelf, "snare", 2);
            b.Loot(ContainerType.Shelf, "carwheel", 1);
            b.Loot(ContainerType.Shelf, "carbattery", 1);
            b.Loot(ContainerType.Shelf, "radiator", 1);
            b.Loot(ContainerType.Crate, "scrap", 4, 1, 3);
            b.Loot(ContainerType.Crate, "chemicals", 3, 1, 2);
            b.Loot(ContainerType.Crate, Currency, 2, 5, 20);
            b.Loot(ContainerType.Crate, "ammo545_ps", 2, 5, 20);
            b.Loot(ContainerType.Locker, "armor_vest_c2", 2);
            b.Loot(ContainerType.Locker, "helmet_c2", 2);
            b.Loot(ContainerType.Locker, "ammo9mm_hp", 2, 10, 20);
            b.Loot(ContainerType.Locker, "zone_permit", 1);

            b.Loot(ContainerType.Corpse, "pda", 2);
            b.Loot(ContainerType.Corpse, "bolt", 3, 3, 8);
            b.Loot(ContainerType.Corpse, Currency, 3, 3, 15);
            b.Loot(ContainerType.Corpse, "ammo545_ps", 2, 5, 15);
            b.Loot(ContainerType.Corpse, "warden_tag", 1);
        }
        #endregion
    }
}
