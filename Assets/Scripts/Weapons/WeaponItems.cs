using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The inventory verbs for guns and their parts: attach an attachment (to the gun in hand, else the first gun it
    /// fits), strip every attachment off a gun you found (Metro Exodus: every kill can be an upgrade), clean a fouled
    /// gun with a kit, load a different round of the same calibre, and — in the hub — insure a piece of kit. Also the
    /// tooltip lines that show ballistics (damage, penetration, armour class, build numbers).
    /// </summary>
    public sealed class WeaponItems : IGameModule
    {
        public int Order => 25;

        public void Init()
        {
            ItemActions.RegisterUse(def => Ballistics.Mod(def.Id) != null, Attach);
            ItemActions.RegisterUse("cleaningkit", Clean);
            ItemActions.RegisterMenu(GunVerbs);
            ItemDef.TooltipProviders.Add(Tooltip);
        }

        #region Attach / strip
        private static bool Attach(PlayerInventory inv, ItemStack modStack)
        {
            ModStats mod = Ballistics.Mod(modStack.Def.Id);
            ItemStack gun = PickGunFor(inv, mod);
            if (gun == null) { GameEvents.RaiseToast("None of your guns takes a " + modStack.Def.DisplayName + "."); return true; }
            AttachTo(inv.Inventory, gun, modStack, inv);
            return true;
        }

        /// <summary>Fits a mod from <paramref name="from"/> onto <paramref name="gun"/>, swapping out whatever filled the slot.</summary>
        public static void AttachTo(Inventory from, ItemStack gun, ItemStack modStack, PlayerInventory refresh = null)
        {
            ModStats mod = Ballistics.Mod(modStack.Def.Id);
            // Abort if the mod is unknown or doesn't fit the gun, avoiding unnecessary work and UI noise.
            if (mod == null || !Ballistics.ModFits(gun.Def, mod)) return;
            // Ensure the gun has an ItemExtra component to hold attached mods.
            ItemExtra x = gun.Ensure();
            // Iterate backwards so removing items doesn't affect the loop index.
            for (int i = x.Mods.Count - 1; i >= 0; i--)
            {
                ModStats old = Ballistics.Mod(x.Mods[i]);
                // Only remove mods that occupy the same slot as the new mod.
                if (old == null || old.Slot != mod.Slot) continue;
                // Return the old mod to the player's inventory before discarding it.
                from.Add(ItemDatabase.Get(x.Mods[i]), 1, enforceWeight: false);
                x.Mods.RemoveAt(i);
            }
            // Add the new mod ID to the gun's mod list.
            x.Mods.Add(mod.Id);
            // Remove one instance of the mod stack from the inventory, as it is now attached.
            from.Remove(modStack, 1);
            // Play a UI sound to give audible feedback for the attachment action.
            Sfx.PlayUI(SfxIds.Reload, 0.6f, 1.2f);
            // Show a toast notification confirming the attachment.
            GameEvents.RaiseToast(modStack.Def.DisplayName + " fitted to the " + gun.Def.DisplayName + ".");
        }

        private static ItemStack PickGunFor(PlayerInventory inv, ModStats mod)
        {
            ItemStack inHand = inv.EquippedStack;
            if (inHand != null && Ballistics.ModFits(inHand.Def, mod)) return inHand;
            List<ItemStack> stacks = inv.Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Def != null && stacks[i].Def.IsFirearm && Ballistics.ModFits(stacks[i].Def, mod)) return stacks[i];
            return null;
        }

        /// <summary>Takes every attachment off a gun and puts them in the pack.</summary>
        public static int Strip(Inventory into, ItemStack gun)
        {
            if (gun == null || gun.Extra == null || gun.Extra.Mods.Count == 0) return 0;
            int n = 0;
            foreach (string id in gun.Extra.Mods)
            {
                ItemDef d = ItemDatabase.Get(id);
                if (d == null) continue;
                into.Add(d, 1, enforceWeight: false, extra: gun.IsFoundInRaid() ? new ItemExtra { FoundInRaid = true } : null);
                n++;
            }
            gun.Extra.Mods.Clear();
            return n;
        }
        #endregion

        #region Cleaning
        private static bool Clean(PlayerInventory inv, ItemStack kit)
        {
            ItemStack gun = inv.EquippedStack;
            if (gun == null || !gun.Def.IsFirearm || gun.DirtLevel() <= 0.01f) gun = Dirtiest(inv);
            if (gun == null) { GameEvents.RaiseToast("Your guns are clean."); return true; }
            // A field clean takes most of the fouling out; the workbench in the hideout gets the rest.
            float before = gun.DirtLevel();
            gun.Ensure().Dirt = Mathf.Max(0f, before - 0.75f);
            if (kit.Def.MaxCondition > 0) { kit.Condition--; if (kit.Condition <= 0) inv.Inventory.Remove(kit, 1); }
            else if (Random.value < 0.25f) inv.Inventory.Remove(kit, 1);
            var skills = inv.GetComponent<PlayerSkills>();
            if (skills != null) skills.AddXp(PlayerSkills.Skill.Mechanics, 3f);
            Sfx.PlayUI(SfxIds.Reload, 0.5f, 0.8f);
            GameEvents.RaiseToast("You strip and clean the " + gun.Def.DisplayName + ".");
            return true;
        }

        private static ItemStack Dirtiest(PlayerInventory inv)
        {
            ItemStack best = null;
            List<ItemStack> stacks = inv.Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack s = stacks[i];
                if (s.Def == null || !s.Def.IsFirearm || s.DirtLevel() <= 0.01f) continue;
                if (best == null || s.DirtLevel() > best.DirtLevel()) best = s;
            }
            return best;
        }
        #endregion

        #region Menu verbs
        /// <summary>Populate context‑menu actions for a firearm item, including stripping attachments, loading ammo, cleaning and insurance.</summary>
        private static void GunVerbs(PlayerInventory inv, ItemStack s, List<ItemMenuAction> into)
        {
            ItemDef d = s.Def;
            // If the item is a firearm, add context‑menu options relevant to guns.
            if (d.IsFirearm)
            {
                // Offer a "Strip attachments" action when the gun currently has mods attached.
                if (s.Extra != null && s.Extra.Mods.Count > 0)
                    into.Add(new ItemMenuAction("Strip attachments (" + s.Extra.Mods.Count + ")", () =>
                    {
                        int n = Strip(inv.Inventory, s);
                        GameEvents.RaiseToast("Stripped " + n + " attachment" + (n == 1 ? "" : "s") + " off the " + d.DisplayName + ".");
                    }));
                // Load a different round of the same calibre (the gun in hand only).
                // Load a different calibre of ammo only for the gun currently equipped.
                PlayerCombat combat = inv.GetComponent<PlayerCombat>();
                if (combat != null && inv.EquippedStack == s)
                {
                    var seen = new HashSet<string>();
                    List<ItemStack> stacks = inv.Inventory.Stacks;
                    for (int i = 0; i < stacks.Count; i++)
                    {
                        ItemDef a = stacks[i].Def;
                        // Prevent duplicate ammo entries and ensure the ammo calibre matches the gun.
                        if (a == null || a.Category != ItemCategory.Ammo || !seen.Add(a.Id) || !Ballistics.Accepts(d, a.Id)) continue;
                        // Skip the currently loaded ammo type to avoid redundant load actions.
                        if (a.Id == combat.LoadedAmmoId) continue;
                        string id = a.Id;
                        into.Add(new ItemMenuAction("Load " + a.DisplayName, () => combat.SwitchAmmo(id)));
                    }
                }
                // Add a "Clean" option when the gun is sufficiently fouled and the player has a cleaning kit.
                if (s.DirtLevel() > 0.05f && inv.Has("cleaningkit"))
                    into.Add(new ItemMenuAction("Clean (" + Mathf.RoundToInt(s.DirtLevel() * 100f) + "% fouled)", () =>
                    {
                        ItemStack kit = Find(inv, "cleaningkit");
                        if (kit != null)
                        {
                            ItemStack hand = s;
                            hand.Ensure().Dirt = Mathf.Max(0f, hand.DirtLevel() - 0.75f);
                            if (kit.Def.MaxCondition > 0)
                            {
                                kit.Condition--;
                                if (kit.Condition <= 0) inv.Inventory.Remove(kit, 1);
                            }
                            else if (Random.value < 0.25f)
                            {
                                inv.Inventory.Remove(kit, 1);
                            }
                            var skills = inv.GetComponent<PlayerSkills>();
                            if (skills != null) skills.AddXp(PlayerSkills.Skill.Mechanics, 3f);
                            Sfx.PlayUI(SfxIds.Reload, 0.5f, 0.8f);
                            GameEvents.RaiseToast("Cleaned the " + hand.Def.DisplayName + ".");
                        }
                    }));
            }
            // In the hub, allow insuring an item if it can be insured and isn't already.
            if (GameSession.IsHub && InsuranceDesk.Insurable(s) && !s.IsInsured())
                into.Add(new ItemMenuAction("Insure (" + Money.Format(InsuranceDesk.Premium(s)) + ")", () =>
                {
                    InsuranceDesk.Insure(s, inv, out string msg);
                    GameEvents.RaiseToast(msg);
                }));
        }

        private static ItemStack Find(PlayerInventory inv, string id)
        {
            List<ItemStack> stacks = inv.Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++) if (stacks[i].Def != null && stacks[i].Def.Id == id) return stacks[i];
            return null;
        }
        #endregion

        #region Tooltips
        private static string Tooltip(ItemDef d)
        {
            var sb = new StringBuilder();
            AmmoStats ammo = Ballistics.Ammo(d.Id);
            // Ammo-specific tooltip lines – calibre, damage, penetration, bleeding and special currency flag.
            if (ammo != null && d.Category == ItemCategory.Ammo)
            {
                Line(sb, "Calibre", CaliberName(ammo.Caliber));
                Line(sb, "Damage", Mathf.RoundToInt(ammo.Damage) + (ammo.Pellets > 1 ? " x" + ammo.Pellets : ""));
                Line(sb, "Penetration", Mathf.RoundToInt(ammo.Penetration) + "  (beats class " + Mathf.Clamp(Mathf.FloorToInt(ammo.Penetration / 10f), 0, 6) + ")");
                Line(sb, "Bleeding", Mathf.RoundToInt(ammo.Fragmentation * 100f) + "%");
                if (ammo.Currency) sb.Append("\n<color=#e0c060>The Zone's money: every shot spends it.</color>");
            }
            GunStats gun = Ballistics.Gun(d.Id);
            // Gun-specific tooltip – calibre, fire mode, recoil ergonomics and optional slot list.
            if (gun != null && d.IsFirearm)
            {
                Line(sb, "Calibre", CaliberName(gun.Caliber));
                Line(sb, "Fire", gun.FullAuto ? "Automatic" : "Semi-automatic");
                Line(sb, "Recoil", gun.Recoil.ToString("0.00") + "   Ergonomics " + Mathf.RoundToInt(gun.Ergonomics));
                if (gun.Slots.Length > 0)
                {
                    var slots = new StringBuilder();
                    for (int i = 0; i < gun.Slots.Length; i++) { if (i > 0) slots.Append(", "); slots.Append(gun.Slots[i]); }
                    Line(sb, "Slots", slots.ToString());
                }
            }
            ModStats mod = Ballistics.Mod(d.Id);
            // Mod-specific tooltip – slot name, optional calibre restriction and statistical modifiers.
            if (mod != null)
            {
                Line(sb, "Slot", mod.Slot + (string.IsNullOrEmpty(mod.Caliber) ? "" : " (" + CaliberName(mod.Caliber) + " only)"));
                if (mod.Spread != 1f) Line(sb, "Spread", Pct(mod.Spread));
                if (mod.Recoil != 1f) Line(sb, "Recoil", Pct(mod.Recoil));
                if (mod.Noise != 1f) Line(sb, "Noise", Pct(mod.Noise));
                if (mod.Range != 1f) Line(sb, "Range", Pct(mod.Range));
                if (mod.MagBonus != 0) Line(sb, "Magazine", "+" + mod.MagBonus);
                if (mod.Zoom > 1f) Line(sb, "Zoom", mod.Zoom.ToString("0.#") + "x");
            }
            ArmorStats armor = Ballistics.Armor(d.Id);
            // Armor-specific tooltip – class, covered zones, resistances and other bonuses.
            if (armor != null)
            {
                if (armor.Class > 0) Line(sb, "Armour class", armor.Class.ToString());
                var zones = new StringBuilder();
                for (int i = 0; i < armor.Zones.Length; i++) { if (i > 0) zones.Append(", "); zones.Append(BodyZones.Name(armor.Zones[i])); }
                if (armor.Zones.Length > 0 && armor.Class > 0) Line(sb, "Covers", zones.ToString());
                Resistances r = armor.Resist;
                if (r.Radiation > 0f || r.Burn > 0f || r.Shock > 0f || r.Chemical > 0f || r.Psi > 0f)
                    Line(sb, "Resists", "rad " + Pct0(r.Radiation) + "  burn " + Pct0(r.Burn) + "  shock " + Pct0(r.Shock) + "  chem " + Pct0(r.Chemical) + "  psi " + Pct0(r.Psi));
                if (armor.ArtifactSlots > 0) Line(sb, "Artifact belt", armor.ArtifactSlots + " slot" + (armor.ArtifactSlots == 1 ? "" : "s"));
                if (armor.CarryBonusKg > 0f) Line(sb, "Carry", "+" + armor.CarryBonusKg.ToString("0") + " kg");
                if (armor.MovePenalty > 0f) Line(sb, "Movement", "-" + Mathf.RoundToInt(armor.MovePenalty * 100f) + "%");
            }
            // Trim leading newline before returning the final tooltip string.
            return sb.Length > 0 ? sb.ToString().TrimStart('\n') : null;
        }

        private static string CaliberName(string c)
        {
            switch (c)
            {
                case "9x19": return "9x19mm";
                case "545": return "5.45x39mm";
                case "762r": return "7.62x54R";
                case "12g": return "12 gauge";
                case "flare": return "Signal flare";
                default: return c;
            }
        }

        private static string Pct(float mult) => (mult > 1f ? "+" : "") + Mathf.RoundToInt((mult - 1f) * 100f) + "%";
        private static string Pct0(float v) => Mathf.RoundToInt(v * 100f) + "%";
        private static void Line(StringBuilder sb, string k, string v) => sb.Append("\n<color=#c8ccd2>").Append(k).Append(":</color> <b>").Append(v).Append("</b>");
        #endregion
    }
}
