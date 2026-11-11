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
            // 9x19 FMJ: a hot factory load, a shade better than the plain rounds, with cleaner wounds.
            Ammo(b, "ammo9mm_fmj", "9mm FMJ", "9x19", 60f, 24f, 0.30f, 0.12f,
                "Factory full-metal-jacket, loaded hot. Hits a little harder than plain 9mm and gets through soft armour a little more often, but the wounds bleed less.", 1, 60);
            // 12g flechette: a bundle of steel darts that flies as one projectile, not a spread of pellets.
            Ammo(b, "ammo12g_flechette", "12g Flechette Shells", "12g", 45f, 18f, 0.25f, 0.30f,
                "Steel darts that fly as one bundle: a single hit, not a spread, but it often gets through the soft armour that stops buckshot.", 1, 30);
            // 5.45x39 FMJ: militia ball, a shade better than PS.
            Ammo(b, "ammo545_fmj", "5.45x39 FMJ", "545", 52f, 32f, 0.38f, 0.18f,
                "Militia ball ammunition. Hits and penetrates a shade better than PS, but wears armour down more slowly.", 1, 120);
            // 7.62x54R AP: hardened steel core, the deepest-penetrating round there is.
            Ammo(b, "ammo762r_ap", "7.62x54R AP", "762r", 90f, 68f, 0.70f, 0.10f,
                "Hardened steel core. Gets through even the heaviest armour more often than not and hits harder than SNB, but the wounds seldom bleed.", 1, 20);
            // The special rounds do something no plain round does, and their cards say what (WeaponItems' tooltip).
            // 5.45x39 tracer: a red streak marks each round's flight (CombatFX.Tracer); no other round draws a trail.
            Ammo(b, "ammo545_tracer", "5.45x39 Tracer", "545", 48f, 30f, 0.40f, 0.20f,
                "A burning base marks each round's flight with a red streak to wherever it stops. Handy for walking fire onto a target.", 1, 90);
            Ballistics.Ammo("ammo545_tracer").Tracer = true;
            // 9x19 and 12g incendiary: whoever they hit catches fire (Aflame, as in a molotov's flames).
            Ammo(b, "ammo9mm_inc", "9mm Incendiary", "9x19", 55f, 25f, 0.40f, 0.45f,
                "Whoever it hits catches fire and burns on for a few seconds. The dead keep coming while they burn, and set alight whoever they crowd against.");
            Ballistics.Ammo("ammo9mm_inc").Incendiary = true;
            Ammo(b, "ammo12g_inc", "12g Incendiary Shells", "12g", 48f, 6f, 0.30f, 0.40f,
                "One burning slug: whoever it hits catches fire. It hits far lighter than a lead slug; the fire does the rest.");
            Ballistics.Ammo("ammo12g_inc").Incendiary = true;
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

            RegisterKentuckyGuns(b);
            RegisterZoneGuns(b);

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
            float recoil, float ergo, bool auto, string desc, int w, int h, System.Action<GunStats> tune = null)
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
            var stats = new GunStats { Id = id, Caliber = caliber, Slots = slots, Recoil = recoil, Ergonomics = ergo, FullAuto = auto };
            tune?.Invoke(stats);
            Ballistics.Register(stats);
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
            if (m.Light)
            {
                // A battery device like the flashlight: its charge is the stack's condition, and moves onto the gun it's fitted
                // to (ItemExtra.LightUsed) and back off with it.
                def.Gear = GearKind.WeaponLight;
                def.MaxCondition = 100;
                def.ChargeSeconds = WeaponLightBatterySeconds;
            }
            Ballistics.Register(m);
        }

        /// <summary>How long a weapon light shines on one battery (seconds); the flashlight's lasts 20 minutes.</summary>
        public const float WeaponLightBatterySeconds = 900f;

        /// <summary>The Kentucky guns: what a farmhouse, a gun shop and a police station held in 1993 (see the loot in
        /// ZoneContent.Gear.cs), and the civilian calibres they fire.</summary>
        public static readonly string[] KentuckyGuns = { "rev38", "m1911", "rifle22", "riotgun", "rifle3006", "lever3030", "ar15" };

        /// <summary>The Zone's Soviet guns, found in its military caches and sold by the traders' upper tiers.</summary>
        public static readonly string[] ZoneGuns = { "svd", "pkm", "aks74u" };

        private static void RegisterKentuckyGuns(ContentBuilder b)
        {
            Ammo(b, "ammo38", ".38 Special Rounds", "38sp", 48f, 14f, 0.3f, 0.25f,
                "Lead round-nose .38 Special, fifty to the box. Slow and soft: it hits well and stops at kevlar.", 1, 50);
            Ammo(b, "ammo45", ".45 ACP Rounds", "45acp", 64f, 16f, 0.35f, 0.22f,
                "Fat, slow .45 ball. Knocks a man down; armour shrugs it off.", 1, 50);
            ItemDef r22 = Ammo(b, "ammo22", ".22 LR Rounds", "22lr", 24f, 4f, 0.1f, 0.12f,
                "Rimfire plinking rounds by the brick. Light, cheap and quiet — enough for a squirrel, or a careful head shot.", 1, 100);
            if (r22 != null) { r22.Weight = 0.004f; r22.Tint = new Color(0.74f, 0.48f, 0.26f); }
            ItemDef r3006 = Ammo(b, "ammo3006", ".30-06 Rounds", "3006", 90f, 36f, 0.6f, 0.4f,
                "Soft-point deer loads for the '06. Terrible wounds, but plates still stop most of them.", 1, 40);
            if (r3006 != null) r3006.Weight = 0.02f;
            ItemDef r3030 = Ammo(b, "ammo3030", ".30-30 Rounds", "3030", 72f, 26f, 0.45f, 0.35f,
                "Flat-nosed .30-30, safe in a tube magazine. The Kentucky deer round since grandpa's day.", 1, 40);
            if (r3030 != null) r3030.Weight = 0.018f;
            Ammo(b, "ammo556", "5.56x45 Rounds", "556", 50f, 30f, 0.45f, 0.3f,
                "M193 ball. Light and fast; it tumbles and fragments in flesh.", 1, 60);
            Ammo(b, "ammo556_m855", "5.56x45 M855 Rounds", "556", 47f, 42f, 0.55f, 0.18f,
                "Green-tipped NATO rounds with a steel penetrator. Punches through soft armour.", 1, 60);

            Tint(Gun(b, "rev38", ".38 Revolver", "38sp", "ammo38", 6, 0.32f, 3f, 22f, 0.7f, 30f, 0.9f, false, 160,
                new[] { ModSlot.Sight, ModSlot.Tactical }, 1f, 72f, false,
                "A six-shot service revolver in .38 Special: a deputy's sidearm, a farmhouse's nightstand gun. Simple and sure, " +
                "slow to reload.", 2, 1,
                s => { s.Sound = SfxIds.GunRevolver; s.ReloadSound = SfxIds.ReloadRevolver; s.Hold = new GunHold(0.141f, 0.371f, 0.141f, 0.371f); }),
                new Color(0.16f, 0.16f, 0.18f));
            Tint(Gun(b, "m1911", "M1911 Pistol", "45acp", "ammo45", 7, 0.26f, 1.7f, 24f, 0.65f, 34f, 1.1f, false, 140,
                new[] { ModSlot.Sight, ModSlot.Muzzle, ModSlot.Tactical, ModSlot.Magazine }, 1.15f, 66f, false,
                "Government-model .45, older than anyone who carried it. Seven fat rounds that hit hard and drop fast.", 2, 1,
                s => { s.Sound = SfxIds.GunPistol45; s.ReloadSound = SfxIds.ReloadPistol; s.Hold = new GunHold(0.167f, 0.443f, 0.167f, 0.443f); }),
                new Color(0.18f, 0.18f, 0.2f));
            Tint(Gun(b, "rifle22", ".22 Rifle", "22lr", "ammo22", 10, 0.18f, 1.8f, 45f, 0.45f, 16f, 2.3f, true, 150,
                new[] { ModSlot.Sight, ModSlot.Stock, ModSlot.Barrel, ModSlot.Magazine }, 0.35f, 68f, false,
                "A semi-automatic .22 with a ten-round rotary magazine: every farm kid's first gun. Quiet enough not to bring the " +
                "street down on you; the dead need a head shot.", 4, 1,
                s => { s.Sound = SfxIds.GunRimfire; s.ReloadSound = SfxIds.ReloadRifle; s.Hold = new GunHold(0.297f, 0.496f, 0.58f, 0.617f); }),
                new Color(0.6f, 0.43f, 0.26f));
            Tint(Gun(b, "riotgun", "Police Pump Shotgun", "12g", "shotgunshells", 7, 0.75f, 3.2f, 14f, 7.5f, 42f, 3.4f, true, 180,
                new[] { ModSlot.Sight, ModSlot.Stock, ModSlot.Barrel, ModSlot.Tactical }, 1.7f, 48f, false,
                "The squad car's 12 gauge: a short barrel, an extended tube and a matte finish that won't shine at night. Seven " +
                "shells of buckshot, and a lot of noise.", 4, 2,
                s => { s.Sound = SfxIds.GunRiot; s.ReloadSound = SfxIds.ReloadShotgun; s.Hold = new GunHold(0.268f, 0.477f, 0.578f, 0.584f); }),
                new Color(0.12f, 0.12f, 0.13f));
            Tint(Gun(b, "rifle3006", ".30-06 Hunting Rifle", "3006", "ammo3006", 4, 1.15f, 3.4f, 140f, 0.22f, 58f, 3.6f, true, 180,
                new[] { ModSlot.Sight, ModSlot.Stock, ModSlot.Barrel, ModSlot.Muzzle }, 1.7f, 34f, false,
                "A walnut-stocked bolt action in .30-06, sighted in every November for deer season. Four in the magazine, and " +
                "each one ends an argument.", 5, 1,
                s => { s.Sound = SfxIds.GunHunting; s.ReloadSound = SfxIds.ReloadHunting; s.AdsZoom = 1.2f; s.Hold = new GunHold(0.277f, 0.315f, 0.515f, 0.65f); }),
                new Color(0.42f, 0.25f, 0.13f));
            Tint(Gun(b, "lever3030", ".30-30 Lever-Action", "3030", "ammo3030", 6, 0.7f, 3.8f, 100f, 0.35f, 52f, 3f, true, 170,
                new[] { ModSlot.Sight, ModSlot.Barrel }, 1.35f, 52f, false,
                "Brush gun of the Kentucky woods: a lever-action .30-30 with a six-round tube. Quick to work, slow to load.", 4, 1,
                s => { s.Sound = SfxIds.GunLever; s.ReloadSound = SfxIds.ReloadLever; s.Hold = new GunHold(0.243f, 0.568f, 0.565f, 0.671f); }),
                new Color(0.4f, 0.24f, 0.12f));
            Tint(Gun(b, "ar15", "AR-15 Rifle", "556", "ammo556", 30, 0.13f, 2.4f, 85f, 0.6f, 46f, 3.2f, true, 150,
                new[] { ModSlot.Sight, ModSlot.Muzzle, ModSlot.Stock, ModSlot.Grip, ModSlot.Magazine, ModSlot.Tactical, ModSlot.Barrel },
                0.85f, 55f, false,
                "A civilian AR-15 sporter: semi-automatic only, light and soft-shooting, with the carry-handle sights it left the " +
                "factory with.", 4, 2,
                s => { s.Sound = SfxIds.GunAr15; s.ReloadSound = SfxIds.ReloadRifle; s.Hold = new GunHold(0.293f, 0.496f, 0.55f, 0.72f); }),
                new Color(0.11f, 0.11f, 0.12f));
        }

        private static void RegisterZoneGuns(ContentBuilder b)
        {
            Tint(Gun(b, "svd", "SVD Marksman Rifle", "762r", "ammo762r_lps", 10, 0.38f, 2.8f, 150f, 0.3f, 58f, 4.3f, true, 170,
                new[] { ModSlot.Sight, ModSlot.Muzzle, ModSlot.Stock, ModSlot.Magazine }, 1.45f, 32f, false,
                "The Soviet marksman's rifle: ten rounds of 7.62x54R, semi-automatic, out to as far as you can see. The Zone's " +
                "quiet men carry them.", 5, 2,
                s => { s.Sound = SfxIds.GunDmr; s.ReloadSound = SfxIds.ReloadRifle; s.DamageMult = 1.05f; s.AdsZoom = 1.25f; s.Hold = new GunHold(0.273f, 0.441f, 0.449f, 0.74f); }),
                new Color(0.4f, 0.2f, 0.11f));
            Tint(Gun(b, "pkm", "PKM Machine Gun", "762r", "ammo762r_lps", 100, 0.092f, 6.5f, 120f, 1.2f, 62f, 7.5f, true, 240,
                new[] { ModSlot.Sight, ModSlot.Tactical }, 1.9f, 14f, true,
                "A belt-fed machine gun with a hundred-round box. Heavy, loud and hungry, and nothing in the Zone walks through it.", 5, 2,
                s => { s.Sound = SfxIds.GunMachineGun; s.ReloadSound = SfxIds.ReloadBelt; s.Hold = new GunHold(0.351f, 0.37f, 0.46f, 0.586f); }),
                new Color(0.11f, 0.11f, 0.12f));
            Tint(Gun(b, "aks74u", "AKS-74U Carbine", "545", "ammo545_ps", 30, 0.085f, 2.4f, 50f, 1.1f, 50f, 2.7f, true, 130,
                new[] { ModSlot.Sight, ModSlot.Muzzle, ModSlot.Stock, ModSlot.Grip, ModSlot.Magazine, ModSlot.Tactical }, 1.35f, 58f, true,
                "The short AK the tank crews and the special police carried: a 5.45 carbine with a folding stock. A fireball of a " +
                "muzzle blast, and handy in a stairwell.", 3, 2,
                s => { s.Sound = SfxIds.GunCarbine; s.ReloadSound = SfxIds.ReloadRifle; s.DamageMult = 0.94f; s.Hold = new GunHold(0.382f, 0.557f, 0.705f, 0.746f); }),
                new Color(0.33f, 0.15f, 0.12f));
        }

        // The colour a gun shows as when its model can't be drawn (the tinted fallback, the inventory's swatch).
        private static void Tint(ItemDef def, Color c)
        {
            if (def != null) def.Tint = c;
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
