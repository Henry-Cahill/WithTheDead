using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>One thing a trader sells: for money, or as a barter for other items.</summary>
    public sealed class TraderOffer
    {
        public string Id;            // stable key for stock tracking
        public string ItemId;
        public int Count = 1;
        public int Price;            // military-grade rounds (0 for a pure barter)
        public int Loyalty = 1;      // loyalty level needed to see it
        public int Stock = 5;        // per restock
        public (string Id, int Count)[] Barter;   // items given instead of (or as well as) money
        public string Quest;         // unlocked by finishing this quest
    }

    /// <summary>A trader's loyalty level and what it takes to reach it (Tarkov: access is the progression).</summary>
    public sealed class LoyaltyLevel
    {
        public int Level, MinCharacterLevel, MinSpent;
        public float MinRep;
    }

    public sealed class TraderDef
    {
        public string Id, Name, Role, Greeting, Faction;
        public bool Legal;           // the Institute is legal; the fence is the black market
        public float BuyRatio = 0.5f;            // what they pay for your items, as a share of value
        public float ArtifactRatio = 0.6f;       // .. for artifacts
        public ItemCategory[] Buys;              // categories they buy (null = everything)
        public bool BuysArtifacts;
        public bool Repairs, Upgrades, Heals, Insures, Guides, Identifies;
        public float RestockHours = 1f;          // real hours between restocks
        public readonly List<TraderOffer> Offers = new List<TraderOffer>();
        public readonly List<LoyaltyLevel> Levels = new List<LoyaltyLevel>();
    }

    /// <summary>
    /// The people behind the counters at the bar. Two economies, as the novel has them: the Institute pays
    /// a fixed, legal and low price (and tells you what you found); the fence pays far more and asks no
    /// questions — but every artifact he moves draws heat that the Wardens may come to collect.
    /// </summary>
    public static class TraderCatalog
    {
        public const string Barkeep = "barkeep", Fence = "fence", Institute = "institute", Gunsmith = "gunsmith", Medic = "medic", Guide = "guide";
        public static readonly string[] Ids = { Barkeep, Fence, Institute, Gunsmith, Medic, Guide };
        private static readonly Dictionary<string, TraderDef> s_Defs = new Dictionary<string, TraderDef>();

        static TraderCatalog()
        {
            var bar = Def(Barkeep, "Mott", "Barkeep", "The bar's owner. Food, drink, work and gossip — the gossip costs extra.", Factions.Freehold, false, 0.4f);
            bar.Buys = new[] { ItemCategory.Food, ItemCategory.Drink, ItemCategory.Material, ItemCategory.Misc };
            O(bar, "crackers", 45, 1, 20); O(bar, "cannedbeans", 70, 1, 12); O(bar, "waterbottle", 55, 1, 15); O(bar, "soda", 40, 1, 10);
            O(bar, "vodka", 120, 1, 6); O(bar, "bandage", 60, 1, 10); O(bar, "lighter", 80, 1, 5); O(bar, "bait", 10, 1, 30);
            O(bar, "purifytabs", 35, 1, 20); O(bar, "cookedmeat", 110, 2, 6); O(bar, "animalfeed", 60, 2, 10); O(bar, "gascan", 450, 2, 3);
            O(bar, "book_cooking", 900, 2, 1); O(bar, "book_metabolism", 1100, 3, 1); O(bar, "fuseblock", 600, 3, 2);

            var fence = Def(Fence, "Vasko", "Fence", "He buys anything, sells whatever fell off a truck, and never asks where it came from.", Factions.Lantern, false, 0.6f);
            fence.BuysArtifacts = true;
            fence.ArtifactRatio = 1.35f;
            fence.RestockHours = 0.5f;
            O(fence, "ak74", 5200, 1, 1); O(fence, "ammo545_bp", 14, 2, 120); O(fence, "armor_plate_c4", 9800, 2, 1);
            O(fence, "grenade", 900, 2, 4); O(fence, "breachcharge", 2400, 3, 2); O(fence, "gps", 3500, 3, 1);
            O(fence, "zone_permit", 1500, 1, 2); O(fence, "handcuffs", 300, 1, 3); O(fence, "drone_fpv", 6500, 3, 1);
            O(fence, "leadcase", 2200, 2, 1); O(fence, "carkey", 800, 2, 1);
            B(fence, "mod_suppressor545", 2, ("goldchain", 2), ("circuitboard", 1));
            B(fence, "suit_halo", 3, ("art_coldspark", 1), ("art_greywax", 1), ("circuitboard", 2));

            var inst = Def(Institute, "Dr. Brandt", "The Institute", "Legal, polite and cheap. She pays a fixed price for artifacts and tells you exactly what they do.", Factions.Institute, true, 0.35f);
            inst.BuysArtifacts = true;
            inst.ArtifactRatio = 1f;
            inst.Identifies = true;
            inst.Buys = new[] { ItemCategory.Misc };
            O(inst, "detector_echo", 1200, 1, 2); O(inst, "detector_bearing", 4200, 2, 1); O(inst, "detector_lattice", 11000, 3, 1);
            O(inst, "antirad", 380, 1, 6); O(inst, "iodine", 90, 1, 15); O(inst, "psiblock", 260, 2, 6);
            O(inst, "gasmask", 900, 1, 2); O(inst, "gasmaskfilter", 220, 1, 10); O(inst, "suit_halo", 14500, 3, 1);
            O(inst, "suit_runner", 4200, 2, 1); O(inst, "leadcase", 1800, 2, 2); O(inst, "bolt", 2, 1, 200);
            O(inst, "book_foraging", 800, 1, 1); O(inst, "book_firstaid", 900, 2, 1);

            var guns = Def(Gunsmith, "Kasimir", "Technician", "Guns, ammunition, attachments and armour. Repairs and upgrades — choose your branch carefully.", Factions.Vanguard, false, 0.45f);
            guns.Buys = new[] { ItemCategory.Firearm, ItemCategory.Weapon, ItemCategory.Ammo, ItemCategory.Clothing, ItemCategory.Misc };
            guns.Repairs = true; guns.Upgrades = true;
            O(guns, "pistol", 1800, 1, 2); O(guns, "shotgun", 2600, 1, 2); O(guns, "smg9", 3900, 2, 2); O(guns, "ak74", 4800, 2, 2); O(guns, "mosin", 4200, 2, 1);
            O(guns, "ammo9mm", 3, 1, 300); O(guns, "shotgunshells", 6, 1, 120); O(guns, "ammo9mm_ap", 7, 2, 150); O(guns, "ammo9mm_hp", 5, 2, 150);
            O(guns, "slug12", 12, 2, 60); O(guns, "ammo545_ps", 6, 2, 300); O(guns, "ammo545_bp", 12, 3, 150); O(guns, "ammo762r_lps", 14, 2, 80);
            O(guns, "ammo762r_snb", 26, 4, 40); O(guns, "flaregun", 900, 1, 2); O(guns, "flare", 140, 1, 10);
            O(guns, "mod_reddot", 1100, 1, 2); O(guns, "mod_foregrip", 450, 1, 3); O(guns, "mod_weaponlight", 400, 1, 3);
            O(guns, "mod_suppressor9", 1600, 2, 1); O(guns, "mod_compensator", 700, 2, 2); O(guns, "mod_stock_tactical", 800, 2, 2);
            O(guns, "mod_stock_folding", 600, 2, 2); O(guns, "mod_mag_extended", 650, 2, 2); O(guns, "mod_laser", 750, 2, 2);
            O(guns, "mod_scope4x", 2600, 3, 1); O(guns, "mod_suppressor545", 2900, 3, 1); O(guns, "mod_barrel_long", 1200, 3, 1); O(guns, "mod_barrel_short", 700, 2, 1);
            O(guns, "armor_vest_c2", 2400, 1, 2); O(guns, "helmet_c2", 1100, 1, 2); O(guns, "armor_vest_c3", 4600, 2, 2); O(guns, "helmet_c3", 2100, 2, 2);
            O(guns, "armor_plate_c4", 8800, 3, 1); O(guns, "helmet_c4", 3900, 3, 1); O(guns, "armor_assault_c5", 16500, 4, 1);
            O(guns, "armor_juggernaut_c6", 32000, 4, 1); O(guns, "suit_bulwark", 45000, 4, 1);
            O(guns, "cleaningkit", 250, 1, 4); O(guns, "grenade", 800, 3, 3); O(guns, "book_aiming", 1200, 2, 1);
            B(guns, "ak74", 2, ("goldchain", 3), ("circuitboard", 2));
            B(guns, "armor_plate_c4", 3, ("scrap", 6), ("fuseblock", 2), ("goldchain", 1));
            B(guns, "mod_scope4x", 2, ("circuitboard", 3), ("wires", 2));

            var med = Def(Medic, "Sister Agathe", "Medic", "Patches you up, sells what keeps you alive, and holds the bar's blood.", Factions.Freehold, true, 0.4f);
            med.Buys = new[] { ItemCategory.Medical };
            med.Heals = true; med.Insures = true;
            O(med, "bandage", 50, 1, 20); O(med, "splint", 220, 1, 6); O(med, "tourniquet", 260, 1, 6); O(med, "hemostat", 330, 1, 6);
            O(med, "fieldkit", 650, 1, 4); O(med, "disinfectant", 160, 1, 6); O(med, "pills", 120, 1, 8); O(med, "antibiotics", 420, 1, 4);
            O(med, "suturekit", 380, 1, 4); O(med, "tweezers", 140, 1, 3); O(med, "ivkit", 300, 1, 4); O(med, "bloodtest", 150, 1, 4);
            O(med, "tetracycline", 300, 1, 5); O(med, "charcoal", 90, 1, 8); O(med, "vitamins", 70, 1, 10);
            O(med, "morphine", 700, 2, 3); O(med, "bloodbag_o_pos", 900, 2, 2); O(med, "bloodbag_a_pos", 900, 2, 2);
            O(med, "bloodbag_b_pos", 900, 2, 2); O(med, "bloodbag_ab_pos", 900, 2, 2); O(med, "bloodbag_o_neg", 1600, 3, 1);
            O(med, "surgicalkit", 3200, 3, 1); O(med, "betablockers", 400, 2, 4);

            var guide = Def(Guide, "Old Lev", "Guide", "Walks people between camps, for a price. Knows the safe routes, and sells a few of them.", Factions.Freehold, false, 0.3f);
            guide.Guides = true;
            guide.Buys = new[] { ItemCategory.Misc };
            O(guide, "map_zone", 300, 1, 3); O(guide, "compass", 180, 1, 3); O(guide, "bolt", 2, 1, 200);
            O(guide, "flare", 150, 1, 6); O(guide, "zone_permit", 1200, 2, 2); O(guide, "gps", 3800, 3, 1);
            O(guide, "fishingrod", 400, 1, 1); O(guide, "snare", 120, 1, 4); O(guide, "book_fishing", 700, 1, 1);
            O(guide, "radio_walkie", 900, 2, 2); O(guide, "drone_ground", 4800, 3, 1); O(guide, "tablet", 2200, 2, 1);

            foreach (TraderDef d in s_Defs.Values)
            {
                d.Levels.Add(new LoyaltyLevel { Level = 1 });
                d.Levels.Add(new LoyaltyLevel { Level = 2, MinCharacterLevel = 3, MinRep = 0.10f, MinSpent = 8000 });
                d.Levels.Add(new LoyaltyLevel { Level = 3, MinCharacterLevel = 8, MinRep = 0.25f, MinSpent = 30000 });
                d.Levels.Add(new LoyaltyLevel { Level = 4, MinCharacterLevel = 15, MinRep = 0.45f, MinSpent = 90000 });
            }
        }

        private static TraderDef Def(string id, string name, string role, string greeting, string faction, bool legal, float buyRatio)
        {
            var d = new TraderDef { Id = id, Name = name, Role = role, Greeting = greeting, Faction = faction, Legal = legal, BuyRatio = buyRatio };
            s_Defs[id] = d;
            return d;
        }

        private static void O(TraderDef d, string item, int price, int loyalty, int stock)
            => d.Offers.Add(new TraderOffer { Id = d.Id + ":" + item, ItemId = item, Price = price, Loyalty = loyalty, Stock = stock });

        private static void B(TraderDef d, string item, int loyalty, params (string, int)[] barter)
            => d.Offers.Add(new TraderOffer { Id = d.Id + ":b:" + item, ItemId = item, Price = 0, Loyalty = loyalty, Stock = 1, Barter = barter });

        public static TraderDef Get(string id) => id != null && s_Defs.TryGetValue(id, out TraderDef d) ? d : null;
        public static IEnumerable<TraderDef> All => s_Defs.Values;
    }
}
