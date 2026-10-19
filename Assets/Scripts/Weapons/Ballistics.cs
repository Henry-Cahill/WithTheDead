using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Where an attachment goes on a gun.</summary>
    public enum ModSlot { Sight, Muzzle, Stock, Grip, Barrel, Magazine, Tactical }

    /// <summary>A round's ballistics. Damage is in limb hit points; penetration is compared with armour class x10.</summary>
    public sealed class AmmoStats
    {
        public string Id, Caliber;
        public float Damage, Penetration;
        public float ArmorDamage = 0.5f;     // share of the damage that wears armour durability
        public float Fragmentation = 0.15f;  // chance the wound bleeds heavily
        public int Pellets = 1;
        public float SpreadMult = 1f;
        public float RangeMult = 1f;
        public bool Currency;                // the military-grade rounds that double as money
    }

    /// <summary>A firearm's handling and which attachment slots it has.</summary>
    public sealed class GunStats
    {
        public string Id, Caliber;
        public ModSlot[] Slots = new ModSlot[0];
        public float Recoil = 1f;            // camera kick and the spread bloom of follow-up shots
        public float Ergonomics = 50f;       // 0..100: aim-down-sights speed and sway
        public bool FullAuto;
        public float DamageMult = 1f;
        public float AdsZoom = 1.15f;        // iron-sight magnification
    }

    /// <summary>An attachment and what it does to the gun it's on.</summary>
    public sealed class ModStats
    {
        public string Id;
        public ModSlot Slot;
        public string Caliber;               // null: fits any caliber (sights, grips)
        public float Spread = 1f, Recoil = 1f, Noise = 1f, Range = 1f;
        public float Ergonomics;
        public int MagBonus;
        public float Zoom;                   // 0: none; >1: optical magnification when aiming down sights
        public bool Laser, Light;
        public float AdsSpread = 1f;         // extra tightening only while aiming down sights
    }

    /// <summary>Body armour, a helmet or a Zone suit: class, covered zones and per-type resistances.</summary>
    public sealed class ArmorStats
    {
        public string Id;
        public int Class;                    // 0 (none) .. 6
        public BodyZone[] Zones = new BodyZone[0];
        public float BluntThrough = 0.25f;   // share of a stopped round's damage that still bruises through
        public float MovePenalty;            // 0..1 slower movement
        public float ErgoPenalty;
        public Resistances Resist;
        public int ArtifactSlots;            // suits: how many artifacts fit on the belt
        public float CarryBonusKg;           // exo-frames
    }

    /// <summary>The live numbers of one gun as currently built (mods + technician upgrades + dirt).</summary>
    public struct WeaponBuild
    {
        public float Spread, Recoil, Noise, Range, Ergonomics, Zoom, AdsSpread, DamageMult, JamChance, JamMult, ReloadMult, FireRateMult;
        public int MagSize;
        public bool Laser, Light, Suppressed, FullAuto;
    }

    /// <summary>
    /// The ballistics tables and the penetration model: ammo matters more than the gun. A round either beats the
    /// armour on the zone it hits (full damage, wearing the armour) or is stopped (a share bruises through as
    /// blunt damage, and the armour still wears). Chance to penetrate rises smoothly as penetration passes the
    /// armour's effective class x10, which itself sinks as the armour's durability is shot away.
    /// </summary>
    public static class Ballistics
    {
        private static readonly Dictionary<string, AmmoStats> s_Ammo = new Dictionary<string, AmmoStats>();
        private static readonly Dictionary<string, GunStats> s_Guns = new Dictionary<string, GunStats>();
        private static readonly Dictionary<string, ModStats> s_Mods = new Dictionary<string, ModStats>();
        private static readonly Dictionary<string, ArmorStats> s_Armor = new Dictionary<string, ArmorStats>();

        /// <summary>Registers an ammo definition so it can be looked up by id.</summary>
public static void Register(AmmoStats a) { if (a != null) s_Ammo[a.Id] = a; }
        /// <summary>Registers a gun definition for later lookup by id.</summary>
public static void Register(GunStats g) { if (g != null) s_Guns[g.Id] = g; }
        /// <summary>Registers a mod definition for later lookup by id.</summary>
public static void Register(ModStats m) { if (m != null) s_Mods[m.Id] = m; }
        public static void Register(ArmorStats a) { if (a != null) s_Armor[a.Id] = a; }

        /// <summary>Looks up an ammo definition by its id.</summary>
public static AmmoStats Ammo(string id) { ItemDatabase.EnsureInit(); return id != null && s_Ammo.TryGetValue(id, out AmmoStats a) ? a : null; }
        /// <summary>Looks up a gun definition by its id.</summary>
public static GunStats Gun(string id) { ItemDatabase.EnsureInit(); return id != null && s_Guns.TryGetValue(id, out GunStats g) ? g : null; }
        /// <summary>Looks up a mod definition by its id.</summary>
public static ModStats Mod(string id) { ItemDatabase.EnsureInit(); return id != null && s_Mods.TryGetValue(id, out ModStats m) ? m : null; }
        /// <summary>Looks up an armor definition by its id.</summary>
public static ArmorStats Armor(string id) { ItemDatabase.EnsureInit(); return id != null && s_Armor.TryGetValue(id, out ArmorStats a) ? a : null; }

        /// <summary>All registered ammo definitions.</summary>
public static IEnumerable<AmmoStats> AllAmmo => s_Ammo.Values;
        /// <summary>All registered mod definitions.</summary>
public static IEnumerable<ModStats> AllMods => s_Mods.Values;

/// <summary>Returns the caliber string for a gun or its ammo.</summary>
public static string CaliberOfGun(ItemDef gun)
{
            if (gun == null) return null;
            GunStats g = Gun(gun.Id);
            if (g != null) return g.Caliber;
            AmmoStats a = Ammo(gun.AmmoId);
            return a != null ? a.Caliber : gun.AmmoId;
        }

        /// <summary>True when the round chambers in the gun (same caliber).</summary>
        public static bool Accepts(ItemDef gun, string ammoId)
        {
            if (gun == null || string.IsNullOrEmpty(ammoId)) return false;
            if (ammoId == gun.AmmoId) return true;
            AmmoStats a = Ammo(ammoId);
            return a != null && a.Caliber == CaliberOfGun(gun);
        }

        /// <summary>True when a mod can go on a gun (the gun has the slot, and the caliber matches if the mod cares).</summary>
        public static bool ModFits(ItemDef gun, ModStats mod)
        {
            if (gun == null || mod == null) return false;
            GunStats g = Gun(gun.Id);
            if (g == null) return false;
            bool hasSlot = false;
            for (int i = 0; i < g.Slots.Length; i++) if (g.Slots[i] == mod.Slot) { hasSlot = true; break; }
            return hasSlot && (string.IsNullOrEmpty(mod.Caliber) || mod.Caliber == g.Caliber);
        }

        /// <summary>The armour (if any) protecting a zone among a set of worn pieces.</summary>
        public static ArmorStats ArmorOn(BodyZone zone, ItemDef worn)
        {
            ArmorStats a = worn != null ? Armor(worn.Id) : null;
            if (a == null || a.Class <= 0) return null;
            for (int i = 0; i < a.Zones.Length; i++) if (a.Zones[i] == zone) return a;
            return null;
        }

        /// <summary>Chance (0..1) that a round of <paramref name="penetration"/> beats armour of <paramref name="armorClass"/>
        /// at <paramref name="durability01"/> of its durability.</summary>
        public static float PenetrationChance(float penetration, int armorClass, float durability01)
        {
            if (armorClass <= 0) return 1f;
            float effective = armorClass * 10f * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(durability01));
            float x = (penetration - effective) / 7f;
            return Mathf.Clamp01(1f / (1f + Mathf.Exp(-x)));
        }

        /// <summary>
        /// Resolves a round against armour: returns the damage that reaches the body and how much durability the
        /// armour loses. <paramref name="penetrated"/> reports whether the round got through.
        /// </summary>
        public static float ResolveArmor(float damage, float penetration, float armorDamageShare, ArmorStats armor,
            float durability01, out float durabilityLoss, out bool penetrated)
        {
            durabilityLoss = 0f;
            penetrated = true;
            if (armor == null || armor.Class <= 0 || durability01 <= 0f) return damage;
            penetrated = Random.value < PenetrationChance(penetration, armor.Class, durability01);
            durabilityLoss = Mathf.Max(1f, damage * Mathf.Clamp01(armorDamageShare) * 0.35f * (penetrated ? 1f : 1.4f));
            if (penetrated)
            {
                // A round that barely beats the plate loses some of its energy doing so.
                float margin = Mathf.Clamp01((penetration - armor.Class * 10f) / 20f);
                return damage * Mathf.Lerp(0.7f, 1f, margin);
            }
            return damage * armor.BluntThrough;
        }

        /// <summary>Works out a gun's current numbers from its base stats, attachments, technician upgrades and fouling.</summary>
        public static WeaponBuild Build(ItemStack gunStack)
        {
            ItemDef def = gunStack != null ? gunStack.Def : null;
            var b = new WeaponBuild
            {
                Spread = 1f, Recoil = 1f, Noise = 1f, Range = 1f, Zoom = 0f, AdsSpread = 1f, DamageMult = 1f,
                ReloadMult = 1f, FireRateMult = 1f, JamMult = 1f, MagSize = def != null ? def.MagazineSize : 0,
            };
            if (def == null) return b;
            GunStats g = Gun(def.Id);
            if (g != null)
            {
                b.Recoil = g.Recoil;
                b.Ergonomics = g.Ergonomics;
                b.DamageMult = g.DamageMult;
                b.FullAuto = g.FullAuto;
            }
            ItemExtra x = gunStack.Extra;
            if (x != null)
            {
                for (int i = 0; i < x.Mods.Count; i++)
                {
                    ModStats m = Mod(x.Mods[i]);
                    if (m == null) continue;
                    b.Spread *= m.Spread;
                    b.Recoil *= m.Recoil;
                    b.Noise *= m.Noise;
                    b.Range *= m.Range;
                    b.Ergonomics += m.Ergonomics;
                    b.MagSize += m.MagBonus;
                    b.AdsSpread *= m.AdsSpread;
                    if (m.Zoom > b.Zoom) b.Zoom = m.Zoom;
                    b.Laser |= m.Laser;
                    b.Light |= m.Light;
                    if (m.Slot == ModSlot.Muzzle && m.Noise < 0.6f) b.Suppressed = true;
                }
                WeaponUpgrades.Apply(def, x.Upgrades, ref b);
            }
            b.Ergonomics = Mathf.Clamp(b.Ergonomics, 0f, 100f);

            // Jams: worn guns jam (the original model) and fouled ones jam more; a few upgrades make them reliable.
            float c01 = gunStack.Condition01;
            float wearJam = c01 >= CloneZConfig.FirearmJamConditionThreshold ? 0f
                : CloneZConfig.FirearmJamChanceAtBroken * (1f - c01 / CloneZConfig.FirearmJamConditionThreshold);
            float dirt = gunStack.DirtLevel();
            float dirtJam = dirt <= 0.35f ? 0f : (dirt - 0.35f) * 0.18f;
            b.JamChance = Mathf.Clamp01((wearJam + dirtJam) * b.JamMult);
            b.Spread *= 1f + dirt * 0.25f;
            return b;
        }
    }
}
