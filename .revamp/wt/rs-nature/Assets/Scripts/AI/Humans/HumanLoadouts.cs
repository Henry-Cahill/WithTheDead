using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>One weighted gun choice of a loadout; the round defaults to the gun's own.</summary>
    public sealed class GunPick
    {
        public readonly string GunId, AmmoId;
        public readonly int Weight;

        public GunPick(string gunId, int weight, string ammoId = null)
        {
            GunId = gunId;
            Weight = weight;
            AmmoId = ammoId;
        }
    }

    /// <summary>Something a loadout may carry besides its gun and armour: the chance, and how many.</summary>
    public sealed class LootPick
    {
        public readonly string Id;
        public readonly float Chance;
        public readonly int Min, Max;

        public LootPick(string id, float chance, int min = 1, int max = 1)
        {
            Id = id;
            Chance = chance;
            Min = min;
            Max = Mathf.Max(min, max);
        }
    }

    /// <summary>
    /// What one kind of Zone stalker carries, wears and how they fight. Ids are plain strings ("scav",
    /// "freehold", "vanguard", "warden", "choir", "institute", "raider", "lantern") so spawners, quests and saves
    /// can name them. Clothes are lists of item ids per body slot; the first that exists in the catalogue at
    /// random is worn, so a trimmed content database still dresses everyone.
    /// </summary>
    public sealed class HumanLoadout
    {
        public string Id, Faction, Title;
        public GunPick[] Guns = new GunPick[0];
        public int MagsMin = 1, MagsMax = 2;
        public string[] Vests = new string[0];
        public float VestChance;
        public string[] Helmets = new string[0];
        public float HelmetChance;
        public string[] Suits = new string[0];
        public float SuitChance;
        public string[] Head = new string[0], Torso = new string[0], Legs = new string[0], Feet = new string[0], Hands = new string[0];
        public float HeadChance = 0.5f, HandsChance = 0.35f;
        /// <summary>The faction colour worked into the jacket (0 = the garment's own colour).</summary>
        public Color Tint = Color.grey;
        public float TintBlend;
        public LootPick[] Carry = new LootPick[0];
        public int MoneyMin, MoneyMax;
        public float PdaChance;
        public float FlashlightChance = 0.7f;
        public float BackpackChance = 0.3f;
        /// <summary>0 (raw) .. 1 (veteran): accuracy, reaction and use of cover.</summary>
        public float Skill = 0.5f;
        /// <summary>0 (cautious) .. 1 (reckless): pushing, flanking and holding ground.</summary>
        public float Aggression = 0.5f;
        public float SurrenderChance = 0.3f;
        /// <summary>Chance each of up to three attachment rolls lands on the gun.</summary>
        public float ModChance = 0.4f;
        /// <summary>Abstract fighting weight of one member (A-Life fights between squads off-screen).</summary>
        public float Power = 1f;

        /// <summary>Selects a gun for the loadout based on weighted probabilities of each GunPick.</summary>
        public GunPick PickGun(System.Random rng)
        {
            int total = 0;
            for (int i = 0; i < Guns.Length; i++) total += Mathf.Max(0, Guns[i].Weight);
            if (total <= 0) return Guns.Length > 0 ? Guns[0] : null;
            int r = rng.Next(total);
            for (int i = 0; i < Guns.Length; i++)
            {
                r -= Mathf.Max(0, Guns[i].Weight);
                if (r < 0) return Guns[i];
            }
            return Guns[Guns.Length - 1];
        }
    }

    /// <summary>The loadout table and the faction defaults.</summary>
    public static class HumanLoadouts
    {
        public const string Scav = "scav", Raider = "raider", Freehold = "freehold", Vanguard = "vanguard", Warden = "warden",
            Choir = "choir", Institute = "institute", Lantern = "lantern";

        public static readonly string[] Ids = { Scav, Raider, Freehold, Vanguard, Warden, Choir, Institute, Lantern };

        private static readonly Dictionary<string, HumanLoadout> s_Defs = new Dictionary<string, HumanLoadout>();

        public static readonly Color[] SkinTones =
        {
            new Color(0.93f, 0.76f, 0.64f), new Color(0.86f, 0.66f, 0.52f), new Color(0.78f, 0.56f, 0.41f),
            new Color(0.66f, 0.46f, 0.32f), new Color(0.52f, 0.35f, 0.24f), new Color(0.38f, 0.25f, 0.17f),
        };

        public static readonly Color[] HairColors =
        {
            new Color(0.08f, 0.07f, 0.06f), new Color(0.19f, 0.12f, 0.08f), new Color(0.36f, 0.24f, 0.14f),
            new Color(0.64f, 0.53f, 0.34f), new Color(0.52f, 0.52f, 0.50f), new Color(0.46f, 0.22f, 0.11f),
        };

        static HumanLoadouts()
        {
            Add(new HumanLoadout
            {
                Id = Scav, Faction = Factions.Scavs, Title = "Scavenger",
                Guns = new[] { new GunPick("pistol", 40), new GunPick("shotgun", 32), new GunPick("smg9", 20), new GunPick("ak74", 8) },
                MagsMin = 1, MagsMax = 2,
                Vests = new[] { "armor_vest_c2" }, VestChance = 0.15f,
                Helmets = new[] { "helmet_c2" }, HelmetChance = 0.05f,
                Head = new[] { "beanie", "cap", "Hat_BalaclavaFace", "greenbeanie" }, HeadChance = 0.55f,
                Torso = new[] { "hoodie", "leatherjacket", "raincoat", "jacket", "parka" },
                Legs = new[] { "jeans", "trousers", "cargopants" },
                Feet = new[] { "sneakers", "boots" },
                Hands = new[] { "gloves" }, HandsChance = 0.3f,
                Tint = Factions.Get(Factions.Scavs).Color, TintBlend = 0.2f,
                Carry = new[] { new LootPick("bandage", 0.7f, 1, 2), new LootPick("pills", 0.25f), new LootPick("crackers", 0.3f),
                    new LootPick("waterbottle", 0.2f), new LootPick("bolt", 0.3f, 3, 8) },
                MoneyMin = 0, MoneyMax = 14, PdaChance = 0.2f, FlashlightChance = 0.5f, BackpackChance = 0.35f,
                Skill = 0.35f, Aggression = 0.6f, SurrenderChance = 0.45f, ModChance = 0.2f, Power = 0.8f,
            });
            Add(new HumanLoadout
            {
                Id = Raider, Faction = Factions.Scavs, Title = "Raider",
                Guns = new[] { new GunPick("ak74", 45), new GunPick("shotgun", 25, "slug12"), new GunPick("smg9", 30) },
                MagsMin = 2, MagsMax = 3,
                Vests = new[] { "armor_vest_c2", "armor_vest_c3", "armor_vest_c3" }, VestChance = 0.65f,
                Helmets = new[] { "helmet_c2", "helmet_c3" }, HelmetChance = 0.3f,
                Head = new[] { "Hat_BalaclavaFull", "Hat_BalaclavaFace", "beanie" }, HeadChance = 0.7f,
                Torso = new[] { "leatherjacket", "camojacket", "Jacket_ArmyCamoUrban" },
                Legs = new[] { "cargopants", "Trousers_CamoUrban", "jeans" },
                Feet = new[] { "boots", "Shoes_ArmyBoots" },
                Hands = new[] { "leathergloves", "gloves" }, HandsChance = 0.6f,
                Tint = new Color(0.20f, 0.19f, 0.18f), TintBlend = 0.35f,
                Carry = new[] { new LootPick("bandage", 0.8f, 1, 2), new LootPick("fieldkit", 0.3f), new LootPick("goldchain", 0.15f),
                    new LootPick("morphine", 0.1f) },
                MoneyMin = 5, MoneyMax = 30, PdaChance = 0.15f, FlashlightChance = 0.6f, BackpackChance = 0.4f,
                Skill = 0.5f, Aggression = 0.8f, SurrenderChance = 0.3f, ModChance = 0.35f, Power = 1.1f,
            });
            Add(new HumanLoadout
            {
                Id = Freehold, Faction = Factions.Freehold, Title = "Freehold runner",
                Guns = new[] { new GunPick("ak74", 35), new GunPick("shotgun", 28), new GunPick("smg9", 22), new GunPick("mosin", 15) },
                MagsMin = 2, MagsMax = 3,
                Vests = new[] { "armor_vest_c2", "armor_vest_c2", "armor_vest_c3" }, VestChance = 0.5f,
                Helmets = new[] { "helmet_c2" }, HelmetChance = 0.15f,
                Suits = new[] { "suit_runner" }, SuitChance = 0.3f,
                Head = new[] { "greenbeanie", "beanie", "Hat_BonnieHat_CamoGreen", "cap" }, HeadChance = 0.6f,
                Torso = new[] { "camojacket", "parka", "jacket", "Jacket_ArmyCamoGreen" },
                Legs = new[] { "cargopants", "trousers", "Trousers_CamoGreen" },
                Feet = new[] { "boots", "Shoes_ArmyBoots" },
                Hands = new[] { "gloves", "leathergloves" }, HandsChance = 0.5f,
                Tint = Factions.Get(Factions.Freehold).Color, TintBlend = 0.4f,
                Carry = new[] { new LootPick("bandage", 0.9f, 1, 3), new LootPick("fieldkit", 0.4f), new LootPick("hemostat", 0.2f),
                    new LootPick("bolt", 0.6f, 4, 12), new LootPick("antirad", 0.15f), new LootPick("detector_echo", 0.06f),
                    new LootPick("vodka", 0.1f) },
                MoneyMin = 8, MoneyMax = 40, PdaChance = 0.3f, FlashlightChance = 0.8f, BackpackChance = 0.6f,
                Skill = 0.55f, Aggression = 0.45f, SurrenderChance = 0.3f, ModChance = 0.4f, Power = 1f,
            });
            Add(new HumanLoadout
            {
                Id = Vanguard, Faction = Factions.Vanguard, Title = "Vanguard trooper",
                Guns = new[] { new GunPick("ak74", 55), new GunPick("ak74", 15, "ammo545_bp"), new GunPick("smg9", 15, "ammo9mm_ap"), new GunPick("mosin", 15) },
                MagsMin = 2, MagsMax = 4,
                Vests = new[] { "armor_vest_c3", "armor_plate_c4", "armor_plate_c4", "armor_assault_c5" }, VestChance = 0.9f,
                Helmets = new[] { "helmet_c3", "helmet_c3", "helmet_c4" }, HelmetChance = 0.75f,
                Head = new[] { "Hat_BeretArmy", "Hat_BalaclavaFace" }, HeadChance = 0.4f,
                Torso = new[] { "Jacket_ArmyOliveDrab", "Jacket_ArmyCamoUrban", "camojacket" },
                Legs = new[] { "Trousers_CamoUrban", "Trousers_ArmyService", "cargopants" },
                Feet = new[] { "Shoes_ArmyBoots", "boots" },
                Hands = new[] { "leathergloves", "gloves" }, HandsChance = 0.75f,
                Tint = Factions.Get(Factions.Vanguard).Color, TintBlend = 0.45f,
                Carry = new[] { new LootPick("bandage", 0.9f, 2, 3), new LootPick("fieldkit", 0.6f), new LootPick("morphine", 0.25f),
                    new LootPick("tourniquet", 0.3f), new LootPick("hemostat", 0.3f) },
                MoneyMin = 15, MoneyMax = 60, PdaChance = 0.1f, FlashlightChance = 0.95f, BackpackChance = 0.4f,
                Skill = 0.72f, Aggression = 0.65f, SurrenderChance = 0.12f, ModChance = 0.7f, Power = 1.4f,
            });
            Add(new HumanLoadout
            {
                Id = Warden, Faction = Factions.Wardens, Title = "Cordon warden",
                Guns = new[] { new GunPick("ak74", 70), new GunPick("mosin", 18), new GunPick("smg9", 12) },
                MagsMin = 2, MagsMax = 4,
                Vests = new[] { "armor_vest_c3", "armor_vest_c3", "armor_plate_c4" }, VestChance = 0.9f,
                Helmets = new[] { "helmet_c3" }, HelmetChance = 0.75f,
                Head = new[] { "Hat_Army", "Hat_BaseballCapArmy", "greenbeanie" }, HeadChance = 0.7f,
                Torso = new[] { "Jacket_ArmyCamoGreen", "Jacket_ArmyOliveDrab", "camojacket" },
                Legs = new[] { "Trousers_CamoGreen", "Trousers_ArmyService", "cargopants" },
                Feet = new[] { "Shoes_ArmyBoots", "boots" },
                Hands = new[] { "gloves" }, HandsChance = 0.7f,
                Tint = Factions.Get(Factions.Wardens).Color, TintBlend = 0.5f,
                Carry = new[] { new LootPick("bandage", 0.9f, 1, 2), new LootPick("fieldkit", 0.5f), new LootPick("warden_tag", 0.75f),
                    new LootPick("zone_permit", 0.1f), new LootPick("cannedbeans", 0.25f) },
                MoneyMin = 10, MoneyMax = 40, PdaChance = 0.05f, FlashlightChance = 1f, BackpackChance = 0.2f,
                Skill = 0.65f, Aggression = 0.5f, SurrenderChance = 0.15f, ModChance = 0.5f, Power = 1.3f,
            });
            Add(new HumanLoadout
            {
                Id = Choir, Faction = Factions.Choir, Title = "Hollow Choir zealot",
                Guns = new[] { new GunPick("ak74", 40), new GunPick("shotgun", 30), new GunPick("mosin", 15), new GunPick("smg9", 15) },
                MagsMin = 2, MagsMax = 3,
                Vests = new[] { "armor_vest_c2", "armor_vest_c3" }, VestChance = 0.35f,
                Helmets = new[] { "helmet_c2" }, HelmetChance = 0.05f,
                Head = new[] { "Hat_BalaclavaFull", "Hat_BalaclavaFace" }, HeadChance = 0.8f,
                Torso = new[] { "Jacket_CoatArmy", "PonchoGreen", "raincoat", "parka" },
                Legs = new[] { "trousers", "cargopants" },
                Feet = new[] { "boots" },
                Hands = new[] { "gloves" }, HandsChance = 0.5f,
                Tint = new Color(0.34f, 0.24f, 0.40f), TintBlend = 0.75f,
                Carry = new[] { new LootPick("bandage", 0.6f), new LootPick("lodestar_hymnal", 0.06f), new LootPick("psiblock", 0.25f),
                    new LootPick("lodestar_page", 0.05f) },
                MoneyMin = 0, MoneyMax = 20, PdaChance = 0.05f, FlashlightChance = 0.2f, BackpackChance = 0.15f,
                Skill = 0.55f, Aggression = 0.9f, SurrenderChance = 0f, ModChance = 0.3f, Power = 1.25f,
            });
            Add(new HumanLoadout
            {
                Id = Institute, Faction = Factions.Institute, Title = "Institute field team",
                Guns = new[] { new GunPick("pistol", 50), new GunPick("smg9", 40), new GunPick("shotgun", 10) },
                MagsMin = 1, MagsMax = 3,
                Vests = new[] { "armor_vest_c2" }, VestChance = 0.25f,
                Helmets = new[] { "helmet_c2" }, HelmetChance = 0.1f,
                Suits = new[] { "suit_halo" }, SuitChance = 0.4f,
                Head = new[] { "bikehelmet", "cap", "beanie" }, HeadChance = 0.35f,
                Torso = new[] { "raincoat", "Boilersuit", "jacket" },
                Legs = new[] { "trousers", "jeans" },
                Feet = new[] { "boots", "sneakers" },
                Hands = new[] { "gloves" }, HandsChance = 0.6f,
                Tint = Factions.Get(Factions.Institute).Color, TintBlend = 0.55f,
                Carry = new[] { new LootPick("fieldkit", 0.5f), new LootPick("antirad", 0.45f), new LootPick("bandage", 0.8f, 1, 2),
                    new LootPick("iodine", 0.3f, 1, 3), new LootPick("institute_sample", 0.2f), new LootPick("detector_echo", 0.1f) },
                MoneyMin = 15, MoneyMax = 50, PdaChance = 0.2f, FlashlightChance = 0.9f, BackpackChance = 0.7f,
                Skill = 0.4f, Aggression = 0.25f, SurrenderChance = 0.6f, ModChance = 0.3f, Power = 0.7f,
            });
            Add(new HumanLoadout
            {
                Id = Lantern, Faction = Factions.Lantern, Title = "Lantern smuggler",
                Guns = new[] { new GunPick("smg9", 40), new GunPick("shotgun", 25), new GunPick("ak74", 25), new GunPick("pistol", 10, "ammo9mm_hp") },
                MagsMin = 1, MagsMax = 3,
                Vests = new[] { "armor_vest_c2", "armor_vest_c3" }, VestChance = 0.35f,
                Helmets = new[] { "helmet_c2" }, HelmetChance = 0.1f,
                Head = new[] { "cap", "beanie", "Hat_BalaclavaFace" }, HeadChance = 0.45f,
                Torso = new[] { "leatherjacket", "jacket", "hoodie" },
                Legs = new[] { "jeans", "cargopants" },
                Feet = new[] { "sneakers", "boots" },
                Hands = new[] { "leathergloves" }, HandsChance = 0.4f,
                Tint = Factions.Get(Factions.Lantern).Color, TintBlend = 0.3f,
                Carry = new[] { new LootPick("bandage", 0.7f, 1, 2), new LootPick("goldchain", 0.3f, 1, 2), new LootPick("vodka", 0.2f),
                    new LootPick("datadrive", 0.08f), new LootPick("zone_permit", 0.08f) },
                MoneyMin = 25, MoneyMax = 90, PdaChance = 0.25f, FlashlightChance = 0.6f, BackpackChance = 0.5f,
                Skill = 0.5f, Aggression = 0.4f, SurrenderChance = 0.4f, ModChance = 0.45f, Power = 0.95f,
            });
        }

        private static void Add(HumanLoadout l) => s_Defs[l.Id] = l;

        /// <summary>Retrieves a loadout definition by its identifier, or null if not found.</summary>
        public static HumanLoadout Get(string id) => id != null && s_Defs.TryGetValue(id, out HumanLoadout l) ? l : null;

        /// <summary>Enumerates all defined loadouts.</summary>
        public static IEnumerable<HumanLoadout> All => s_Defs.Values;

        /// <summary>The usual kit for a faction's people (the scavengers mix in a few raiders).</summary>
        public static string DefaultIdFor(string faction, System.Random rng = null)
        {
            switch (faction)
            {
                case Factions.Scavs: return (rng != null ? rng.NextDouble() : Random.value) < 0.25 ? Raider : Scav;
                case Factions.Freehold: return Freehold;
                case Factions.Vanguard: return Vanguard;
                case Factions.Wardens: return Warden;
                case Factions.Choir: return Choir;
                case Factions.Institute: return Institute;
                case Factions.Lantern: return Lantern;
                default: return Scav;
            }
        }

        /// <summary>Reads a loadout string: an id ("vanguard"), optionally with a forced gun after a slash
        /// ("freehold/mosin"). Unknown or empty ids fall back to the faction's default.</summary>
        public static HumanLoadout Resolve(string loadout, string faction, System.Random rng, out string gunOverride)
        {
            gunOverride = null;
            string id = loadout;
            if (!string.IsNullOrEmpty(id))
            {
                int slash = id.IndexOf('/');
                if (slash >= 0)
                {
                    gunOverride = slash + 1 < id.Length ? id.Substring(slash + 1) : null;
                    id = id.Substring(0, slash);
                }
                id = id.Trim().ToLowerInvariant();
            }
            HumanLoadout l = Get(id) ?? Get(DefaultIdFor(faction, rng));
            return l ?? s_Defs[Scav];
        }

        /// <summary>The first item of a list that exists in the catalogue, starting at a random entry.</summary>
        public static ItemDef PickItem(string[] ids, System.Random rng)
        {
            if (ids == null || ids.Length == 0) return null;
            int start = rng.Next(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                ItemDef d = ItemDatabase.Get(ids[(start + i) % ids.Length]);
                if (d != null) return d;
            }
            return null;
        }
    }

    /// <summary>Names for the Zone's people. Invented, plain and a little worn, like their owners.</summary>
    public static class HumanNames
    {
        private static readonly string[] s_First =
        {
            "Bohdan", "Lev", "Marta", "Oskar", "Pavlo", "Dima", "Taras", "Yuri", "Sasha", "Ilsa", "Nadia", "Oleg",
            "Grisha", "Anton", "Fedir", "Mila", "Roman", "Stas", "Vera", "Zhenya", "Hank", "Cole", "Wade", "Jessie",
            "Boone", "Earl", "Darla", "Mack", "Ivo", "Lida", "Petro", "Kostya", "Hal", "Noor", "Emil", "Rada",
        };

        private static readonly string[] s_Nick =
        {
            "Sparrow", "Lamp", "Brick", "Gravel", "Moth", "Wick", "Crowbar", "Static", "Rook", "Fennel", "Salt", "Owl",
            "Ash", "Pike", "Tin", "Badger", "Cinder", "Hollow", "Latch", "Rust", "Thimble", "Gauze", "Flint", "Sleet",
            "Magpie", "Chisel", "Drift", "Weasel", "Copper", "Knot",
        };

        private static readonly string[] s_ChoirNames =
        {
            "Brother Quiet", "Sister Hum", "the Listener", "Brother Ember", "Sister Hollow", "the Cantor", "Brother Grey",
        };

        public static string Roll(string faction, System.Random rng)
        {
            if (faction == Factions.Choir) return s_ChoirNames[rng.Next(s_ChoirNames.Length)];
            string first = s_First[rng.Next(s_First.Length)];
            if (faction == Factions.Vanguard || faction == Factions.Wardens)
            {
                string[] ranks = faction == Factions.Vanguard ? new[] { "Pvt.", "Cpl.", "Sgt." } : new[] { "Pvt.", "Cpl." };
                return ranks[rng.Next(ranks.Length)] + " " + first;
            }
            if (faction == Factions.Institute) return (rng.NextDouble() < 0.5 ? "Dr. " : "") + first;
            return rng.NextDouble() < 0.55 ? first + " '" + s_Nick[rng.Next(s_Nick.Length)] + "'" : s_Nick[rng.Next(s_Nick.Length)];
        }
    }
}
