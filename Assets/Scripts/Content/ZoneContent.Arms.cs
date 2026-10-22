using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The Zone's content: ammunition and guns with real ballistics, attachments, body armour and Zone suits
    /// (this file), and the Zone gear, artifacts, medicine, tools and loot (ZoneContent.Gear.cs).
    /// </summary>
    public sealed partial class ZoneContent : IContentProvider
    {
        public int Order => 0;

        /// <summary>The military-grade 5.45 round: the hub's currency and the best bullet in the Zone.</summary>
        public const string Currency = "ammo_mg545";

        public void Register(ContentBuilder b)
        {
            RegisterArms(b);
            RegisterArmor(b);
            RegisterZoneGear(b);
            RegisterMedical(b);
            RegisterSurvival(b);
            RegisterEconomyItems(b);
            PatchClothingPockets(b);
            RegisterLoot(b);
        }

        #region Ammunition and guns
        private static void RegisterArms(ContentBuilder b)
        {
            // 9x19: the existing rounds become FMJ; armour-piercing and hollow-point variants join them.
            Ammo(b, "ammo9mm", null, "9x19", 54f, 22f, 0.35f, 0.18f);
            Ammo(b, "ammo9mm_ap", "9mm AP Rounds", "9x19", 44f, 34f, 0.55f, 0.10f, "Steel-cored 9mm. Bites through soft armour.");
            Ammo(b, "ammo9mm_hp", "9mm Hollow-points", "9x19", 70f, 10f, 0.20f, 0.45f, "Expands in flesh. Useless against armour.");
            // 12 gauge.
            Ammo(b, "shotgunshells", null, "12g", 32f, 4f, 0.25f, 0.35f, null, 8);
            Ammo(b, "slug12", "12ga Slugs", "12g", 150f, 22f, 0.65f, 0.40f, "A single heavy lead slug. Stops anything, at a price.", 1, 20);
            // 5.45x39: standard, armour-piercing, and the military-grade rounds that are also the money.
            Ammo(b, "ammo545_ps", "5.45x39 PS Rounds", "545", 50f, 28f, 0.45f, 0.20f, "Standard rifle rounds. Steel core, decent penetration.");
            Ammo(b, "ammo545_bp", "5.45x39 BP Rounds", "545", 46f, 45f, 0.60f, 0.15f, "Armour-piercing. Rare, expensive, worth it.");
            ItemDef mg = Ammo(b, Currency, "Military-grade 5.45 (MG)", "545", 58f, 55f, 0.70f, 0.20f,
                "pre-Passing military stock, still sealed. The best round in the Zone, and the only money anyone takes. Firing it means spending your savings.", 1, 500);
            Ballistics.Ammo(Currency).Currency = true;
            mg.Tint = new Color(0.86f, 0.74f, 0.32f);
            // 7.62x54R for the bolt-action rifle.
            Ammo(b, "ammo762r_lps", "7.62x54R LPS Rounds", "762r", 82f, 42f, 0.60f, 0.25f, "Full-power rifle rounds.", 1, 40);
            Ammo(b, "ammo762r_snb", "7.62x54R SNB Rounds", "762r", 76f, 58f, 0.75f, 0.20f, "Sniper armour-piercing. Goes through nearly anything.", 1, 40);
            // Improvised 9mm FMJ – civilian surplus round, just a little better than the standard.
            Ammo(b, "ammo9mm_fmj", "9mm FMJ", "9x19", 60f, 24f, 0.30f, 0.12f, "Full‑metal jacket. Pierces cheap kevlar, but still whines in soft steel.", 1, 60);
            // 12‑gauge flechette shells – cheap “pigeon‑shot” for cutting through foliage and low‑grade armor.
            Ammo(b, "ammo12g_flechette", "12g Flechette Shells", "12g", 45f, 18f, 0.25f, 0.30f, "Hundreds of tiny steel darts. Stabs rather than smashes.", 1, 30);
            // 5.45×39 FMJ – the “budget” rifle round, mass‑produced for militia.
            Ammo(b, "ammo545_fmj", "5.45x39 FMJ", "545", 52f, 32f, 0.38f, 0.18f, "Standard military bullet. Reliable, affordable, and surprisingly accurate.", 1, 120);
            // 7.62×54R AP – hardened steel core, meant for anti‑material use.
            Ammo(b, "ammo762r_ap", "7.62x54R AP", "762r", 90f, 68f, 0.70f, 0.10f, "Armour‑piercing. Designed to breach hardened plates, but the recoil hurts.", 1, 20);
            // 5.45×39 tracer – flares the bullet’s path, useful for night fire or signaling.
            Ammo(b, "ammo545_tracer", "5.45x39 Tracer", "545", 48f, 30f, 0.40f, 0.20f, "Brightly‑lit rounds. Leaves a short red tail, handy for spotting where you’ve shot.", 1, 90);
            // Signal flares (the flare gun's "ammunition").
            ItemDef flare = b.Item("flare", "Signal Flare", ItemCategory.Ammo, 0.15f, 6, 1, 1,
                "A red signal flare. Fire it at a landing zone and a helicopter comes — and everyone nearby sees where you are.");
            flare.Tint = new Color(0.9f, 0.25f, 0.2f);
            Ballistics.Register(new AmmoStats { Id = "flare", Caliber = "flare", Damage = 8f, Penetration = 0f });

            // The existing guns gain calibers and attachment slots.
            Ballistics.Register(new GunStats { Id = "pistol", Caliber = "9x19", Recoil = 0.9f, Ergonomics = 70f,
                Slots = new[] { ModSlot.Sight, ModSlot.Muzzle, ModSlot.Tactical, ModSlot.Magazine } });
            Ballistics.Register(new GunStats { Id = "shotgun", Caliber = "12g", Recoil = 1.6f, Ergonomics = 45f,
                Slots = new[] { ModSlot.Sight, ModSlot.Stock, ModSlot.Barrel, ModSlot.Tactical } });

            Gun(b, "ak74", "AK-74 Rifle", "545", "ammo545_ps", 30, 0.095f, 2.5f, 70f, 0.9f, 46f, 3.4f, true, 140,
                new[] { ModSlot.Sight, ModSlot.Muzzle, ModSlot.Stock, ModSlot.Grip, ModSlot.Magazine, ModSlot.Tactical, ModSlot.Barrel },
                1.2f, 42f, true, "The Zone's workhorse. Eats the military-grade money as happily as anything else.", 4, 2);
            Gun(b, "mosin", "Mosin Rifle", "762r", "ammo762r_lps", 5, 1.25f, 3.6f, 130f, 0.25f, 55f, 4.1f, true, 160,
                new[] { ModSlot.Sight, ModSlot.Stock, ModSlot.Barrel, ModSlot.Muzzle },
                1.6f, 30f, false, "A century-old bolt action. One shot, one answer.", 5, 1);
            Gun(b, "smg9", "PP-9 SMG", "9x19", "ammo9mm", 30, 0.075f, 2.1f, 38f, 1.3f, 34f, 2.2f, true, 110,
                new[] { ModSlot.Sight, ModSlot.Muzzle, ModSlot.Stock, ModSlot.Grip, ModSlot.Magazine, ModSlot.Tactical },
                0.8f, 58f, true, "Compact and quick. Hoses 9mm at anything close.", 3, 2);
            ItemDef fg = Gun(b, "flaregun", "Flare Gun", "flare", "flare", 1, 1.5f, 2f, 40f, 2f, 20f, 0.6f, false, 40,
                new ModSlot[0], 1f, 70f, false, "Calls the extraction helicopter when fired at a landing zone.", 2, 1);
            fg.Tint = new Color(0.85f, 0.35f, 0.15f);

            // Attachments.
            Mod(b, "mod_reddot", "Red Dot Sight", ModSlot.Sight, null, 0.15f, m => { m.AdsSpread = 0.6f; m.Ergonomics = 2f; m.Zoom = 1.1f; },
                "A reflex sight. Fast, clean target picture.");
            Mod(b, "mod_scope4x", "4x Scope", ModSlot.Sight, null, 0.45f, m => { m.AdsSpread = 0.45f; m.Range = 1.3f; m.Zoom = 3.2f; m.Ergonomics = -6f; },
                "Magnified optic. Narrow view, long reach.");
            Mod(b, "mod_suppressor9", "9mm Suppressor", ModSlot.Muzzle, "9x19", 0.3f, m => { m.Noise = 0.35f; m.Recoil = 0.92f; m.Range = 0.95f; },
                "Turns a gunshot into a cough. The dead hear far less of it.");
            Mod(b, "mod_suppressor545", "5.45 Suppressor", ModSlot.Muzzle, "545", 0.5f, m => { m.Noise = 0.4f; m.Recoil = 0.9f; },
                "Rifle can. Quieter, not quiet.");
            Mod(b, "mod_compensator", "Muzzle Compensator", ModSlot.Muzzle, null, 0.12f, m => { m.Recoil = 0.78f; m.Noise = 1.1f; },
                "Vents gas upward to keep the muzzle down. Louder.");
            Mod(b, "mod_stock_tactical", "Tactical Stock", ModSlot.Stock, null, 0.5f, m => { m.Recoil = 0.75f; m.Ergonomics = 4f; },
                "Adjustable stock with a cheek rest.");
            Mod(b, "mod_stock_folding", "Folding Stock", ModSlot.Stock, null, 0.25f, m => { m.Recoil = 0.9f; m.Ergonomics = 8f; },
                "Light and quick to shoulder.");
            Mod(b, "mod_foregrip", "Vertical Foregrip", ModSlot.Grip, null, 0.12f, m => { m.Recoil = 0.85f; m.Spread = 0.92f; m.Ergonomics = 3f; },
                "Something to hold on to.");
            Mod(b, "mod_barrel_long", "Long Barrel", ModSlot.Barrel, null, 0.6f, m => { m.Range = 1.25f; m.Spread = 0.85f; m.Ergonomics = -5f; },
                "Extra velocity and accuracy, extra weight.");
            Mod(b, "mod_barrel_short", "Short Barrel", ModSlot.Barrel, null, 0.2f, m => { m.Range = 0.85f; m.Spread = 1.1f; m.Ergonomics = 6f; },
                "Handy in corridors.");
            Mod(b, "mod_mag_extended", "Extended Magazine", ModSlot.Magazine, null, 0.25f, m => { m.MagBonus = 10; m.Ergonomics = -3f; },
                "More rounds before the reload that kills you.");
            Mod(b, "mod_laser", "Laser Module", ModSlot.Tactical, null, 0.1f, m => { m.Laser = true; m.Spread = 0.85f; },
                "Puts a dot where the round will go. So does everyone watching.");
            Mod(b, "mod_weaponlight", "Weapon Light", ModSlot.Tactical, null, 0.15f, m => { m.Light = true; },
                "A torch on the rail. Seeing and being seen.");
        }

        private static ItemDef Ammo(ContentBuilder b, string id, string name, string caliber, float damage, float pen,
            float armorDmg, float frag, string desc = null, int pellets = 1, int maxStack = 60)
        {
            ItemDef def = name != null ? b.Item(id, name, ItemCategory.Ammo, 0.012f, maxStack, 1, 1, desc) : b.Get(id);
            if (def == null) return null;
            Ballistics.Register(new AmmoStats
            {
                Id = id, Caliber = caliber, Damage = damage, Penetration = pen, ArmorDamage = armorDmg,
                Fragmentation = frag, Pellets = pellets, SpreadMult = pellets > 1 ? 1f : 1f,
            });
            return def;
        }

        private static ItemDef Gun(ContentBuilder b, string id, string name, string caliber, string ammo, int mag, float cooldown,
            float reload, float range, float spread, float noise, float weight, bool twoHanded, int durability, ModSlot[] slots,
            float recoil, float ergo, bool auto, string desc, int w, int h)
        {
            ItemDef def = b.Item(id, name, ItemCategory.Firearm, weight, 1, w, h, desc);
            if (!b.InDatabase(id))
            {
                def.IsFirearm = true;
                AmmoStats a = Ballistics.Ammo(ammo);
                def.FirearmDamage = a != null ? a.Damage / DamageScale.ArenaToHp : 1.5f;
                def.FirearmRange = range;
                def.PelletsPerShot = 1;
                def.MagazineSize = mag;
                def.FireCooldown = cooldown;
                def.ReloadTime = reload;
                def.FirearmSpread = spread;
                def.AmmoId = ammo;
                def.FirearmNoise = noise;
                def.MaxCondition = durability;
                def.ConditionLossChance = 30;
                def.TwoHanded = twoHanded;
            }
            Ballistics.Register(new GunStats { Id = id, Caliber = caliber, Slots = slots, Recoil = recoil, Ergonomics = ergo, FullAuto = auto });
            return def;
        }

        private static void Mod(ContentBuilder b, string id, string name, ModSlot slot, string caliber, float weight,
            System.Action<ModStats> set, string desc)
        {
            ItemDef def = b.Item(id, name, ItemCategory.Misc, weight, 1, slot == ModSlot.Barrel || slot == ModSlot.Muzzle ? 2 : 1, 1, desc);
            def.CustomUse = true;
            def.UseVerb = "Attach";
            def.Tint = new Color(0.30f, 0.32f, 0.30f);
            var m = new ModStats { Id = id, Slot = slot, Caliber = caliber };
            set?.Invoke(m);
            Ballistics.Register(m);
        }
        #endregion

        #region Armour and suits
        private static readonly BodyZone[] Torso = { BodyZone.Thorax, BodyZone.Stomach };
        private static readonly BodyZone[] TorsoArms = { BodyZone.Thorax, BodyZone.Stomach, BodyZone.LeftArm, BodyZone.RightArm };
        private static readonly BodyZone[] HeadOnly = { BodyZone.Head };
        private static readonly BodyZone[] FullBody = { BodyZone.Thorax, BodyZone.Stomach, BodyZone.LeftArm, BodyZone.RightArm, BodyZone.LeftLeg, BodyZone.RightLeg };

        private static void RegisterArmor(ContentBuilder b)
        {
            Armor(b, "armor_vest_c2", "Soft Armour Vest", ClothingSlot.Armor, 2, Torso, 45, 2.6f, 0.03f, 0f, 0.9f, 0.9f,
                "Kevlar panels. Stops pistol rounds and teeth.", grid: new Vector2Int(2, 1), cap: 1.5f);
            Armor(b, "armor_vest_c3", "Police Vest (Class 3)", ClothingSlot.Armor, 3, Torso, 55, 3.6f, 0.05f, 0f, 0.95f, 0.95f,
                "Heavier soft armour with trauma plates.", grid: new Vector2Int(2, 2), cap: 2.5f);
            Armor(b, "armor_plate_c4", "Plate Carrier (Class 4)", ClothingSlot.Armor, 4, Torso, 65, 6.8f, 0.08f, 0f, 1f, 1f,
                "Steel plates front and back. Rifle rounds bounce. Mostly.", grid: new Vector2Int(3, 2), cap: 4f);
            Armor(b, "armor_assault_c5", "Assault Armour (Class 5)", ClothingSlot.Armor, 5, TorsoArms, 80, 9.5f, 0.12f, 0f, 1f, 1f,
                "Ceramic plates and shoulder guards. Heavy, hot, worth it.", grid: new Vector2Int(3, 2), cap: 4f);
            Armor(b, "armor_juggernaut_c6", "Juggernaut Rig (Class 6)", ClothingSlot.Armor, 6, TorsoArms, 95, 14f, 0.22f, 0f, 1f, 1f,
                "Bomb-disposal grade. Walk slowly through anything.");
            Armor(b, "helmet_c2", "Ballistic Cap", ClothingSlot.Head, 2, HeadOnly, 30, 0.9f, 0.01f, 0.02f, 0.95f, 0.95f, "A light bump helmet.");
            Armor(b, "helmet_c3", "Army Helmet (Class 3)", ClothingSlot.Head, 3, HeadOnly, 40, 1.4f, 0.02f, 0.03f, 1f, 1f, "Steel pot. Rings like a bell.");
            Armor(b, "helmet_c4", "Assault Helmet (Class 4)", ClothingSlot.Head, 4, HeadOnly, 50, 1.6f, 0.03f, 0.03f, 1f, 1f, "Aramid shell, rail mounts.");

            // Zone suits: worn on the torso; resist the Zone's damage types and carry artifacts on a belt.
            Suit(b, "suit_runner", "Runner's Coverall", 2, 60, 3.2f, 0.10f, 2,
                new Resistances { Radiation = 0.2f, Burn = 0.15f, Shock = 0.1f, Chemical = 0.15f, Psi = 0.05f },
                "Patched coverall with sewn-in liners. The Zone's everyday wear.");
            Suit(b, "suit_halo", "Halo Science Suit", 1, 70, 4.5f, 0.12f, 3,
                new Resistances { Radiation = 0.55f, Burn = 0.35f, Shock = 0.3f, Chemical = 0.5f, Psi = 0.35f },
                "Institute closed-circuit suit. Built for anomaly fields, not firefights.");
            Suit(b, "suit_bulwark", "Bulwark Exo-frame", 5, 110, 16f, 0.0f, 2,
                new Resistances { Radiation = 0.25f, Burn = 0.2f, Shock = 0.15f, Chemical = 0.2f, Psi = 0.1f, Explosion = 0.3f },
                "Powered exoskeleton under armour plate. Carries what you can't — slowly.", carryBonus: 25f, movePenalty: 0.18f);
        }

        private static ItemDef Armor(ContentBuilder b, string id, string name, ClothingSlot slot, int cls, BodyZone[] zones,
            int durability, float weight, float movePen, float insulation, float bite, float scratch, string desc,
            Vector2Int? grid = null, float cap = 0f)
        {
            ItemDef def = b.Item(id, name, ItemCategory.Clothing, weight, 1, 0, 0, desc);
            if (!b.InDatabase(id))
            {
                def.IsClothing = true;
                def.ClothingSlot = slot;
                def.MaxCondition = durability;
                def.ConditionLossChance = 60;
                def.Insulation = insulation;
                def.BiteDefense = bite;
                def.ScratchDefense = scratch;
                def.Tint = new Color(0.26f, 0.30f, 0.24f);
                if (grid.HasValue && cap > 0f)
                {
                    def.Gear = GearKind.Container;
                    def.GridW = grid.Value.x;
                    def.GridH = grid.Value.y;
                    def.ContainerCapacity = cap;
                }
            }
            Ballistics.Register(new ArmorStats { Id = id, Class = cls, Zones = zones, MovePenalty = movePen, BluntThrough = cls >= 4 ? 0.18f : 0.28f });
            return def;
        }

        private static void Suit(ContentBuilder b, string id, string name, int cls, int durability, float weight, float insulation,
            int artifactSlots, Resistances resist, string desc, float carryBonus = 0f, float movePenalty = 0.04f)
        {
            ItemDef def = b.Item(id, name, ItemCategory.Clothing, weight, 1, 2, 3, desc);
            if (!b.InDatabase(id))
            {
                def.IsClothing = true;
                def.ClothingSlot = ClothingSlot.Torso;
                def.MaxCondition = durability;
                def.ConditionLossChance = 50;
                def.Insulation = insulation;
                def.BiteDefense = 0.85f;
                def.ScratchDefense = 0.9f;
                def.WaterResistance = 0.6f;
                def.Gear = GearKind.Container;
                def.GridW = 2;
                def.GridH = 2;
                def.ContainerCapacity = 3f;
                def.Tint = new Color(0.45f, 0.48f, 0.36f);
            }
            Ballistics.Register(new ArmorStats
            {
                Id = id, Class = cls, Zones = FullBody, Resist = resist, ArtifactSlots = artifactSlots,
                CarryBonusKg = carryBonus, MovePenalty = movePenalty, BluntThrough = 0.3f,
            });
        }
        #endregion
    }
}
