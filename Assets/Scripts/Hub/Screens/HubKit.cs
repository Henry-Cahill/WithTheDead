using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Where an item the hub's screens work on lives.</summary>
    public enum HubItemSource { Stash, Kit, Worn }

    /// <summary>
    /// One item a hub screen can act on: a live stack in the stash or the carried pack, or a piece worn on the
    /// survivor's body. Worn pieces aren't stacks, so they are worked on through a copy (<see cref="Stack"/>)
    /// that <see cref="Commit"/> dresses back onto the body.
    /// </summary>
    public sealed class HubItemRef
    {
        public HubItemSource Source;
        public ItemStack Stack;
        public Inventory Owner;              // null for a worn piece
        public ClothingSlot Slot;            // worn pieces only

        public ItemDef Def => Stack != null ? Stack.Def : null;

        public string SourceLabel => Source == HubItemSource.Stash ? "Stash" : Source == HubItemSource.Kit ? "Carried" : "Worn";

        /// <summary>True while the item is still where it was found (it wasn't sold, dropped or taken off meanwhile).</summary>
        public bool StillValid
        {
            get
            {
                if (Stack == null || Stack.Def == null) return false;
                if (Source != HubItemSource.Worn) return Owner != null && Owner.Stacks.Contains(Stack);
                PlayerClothing clothing = HubKit.Clothing;
                return clothing != null && clothing.GetWorn(Slot) == Stack.Def;
            }
        }

        /// <summary>Writes a worn piece's copy back onto the body (stash and pack stacks are edited in place).</summary>
        public void Commit()
        {
            if (Source == HubItemSource.Worn && Stack != null)
                HubKit.ReplaceWorn(Slot, Stack.Def, Stack.Condition, Stack.TimesRepaired, Stack.Extra);
        }

        public bool Same(HubItemRef other)
        {
            if (other == null || other.Source != Source) return false;
            return Source == HubItemSource.Worn ? other.Slot == Slot && other.Def == Def : ReferenceEquals(other.Stack, Stack);
        }
    }

    /// <summary>
    /// The survivor's kit as the hub sees it: in the hub the survivor is a live body, so the carried kit is the
    /// local player's <see cref="PlayerInventory"/> (plus what is worn). Everything here tolerates a missing body
    /// (tests, the main menu) by treating the kit as empty. After the screens change the kit, <see cref="Commit"/>
    /// snapshots it into the character profile so a sale or an upgrade can never be undone (or duplicated) by
    /// the next load.
    /// </summary>
    public static class HubKit
    {
        public static PlayerInventory Carried
        {
            get
            {
                Actor a = Actor.LocalPlayer;
                return a != null ? a.GetComponent<PlayerInventory>() : null;
            }
        }

        public static PlayerClothing Clothing
        {
            get
            {
                Actor a = Actor.LocalPlayer;
                return a != null ? a.GetComponent<PlayerClothing>() : null;
            }
        }

        public static PlayerStats Stats
        {
            get
            {
                Actor a = Actor.LocalPlayer;
                return a != null ? a.GetComponent<PlayerStats>() : null;
            }
        }

        /// <summary>The carried pack's inventory, or null.</summary>
        public static Inventory Pack
        {
            get
            {
                PlayerInventory p = Carried;
                return p != null ? p.Inventory : null;
            }
        }

        /// <summary>Money carried in the pack (not the stash).</summary>
        public static int MoneyCarried => Money.Count(Pack);

        /// <summary>
        /// Snapshots the live body's kit into the active character (hub only). Skipped when the body was clearly
        /// not dressed from the profile (no secure pouch worn), so a bare test body never wipes a saved kit.
        /// </summary>
        public static void Commit()
        {
            if (!GameSession.IsHub) return;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            PlayerStats stats = Stats;
            PlayerClothing clothing = Clothing;
            if (c == null || stats == null || clothing == null || clothing.GetWorn(ClothingSlot.Pouch) == null) return;
            RaidKit kit = RaidLoadout.Capture(stats);
            if (kit == null) return;
            kit.Streak = c.Kit != null ? c.Kit.Streak : 0;
            c.Kit = kit;
        }

        /// <summary>After any change to the kit: drop a weapon that's no longer carried, commit the kit, save.</summary>
        public static void AfterChange()
        {
            FixEquipped();
            Commit();
            ProfileStore.Save();
        }

        /// <summary>Falls back to bare hands when the weapon in hand was sold or handed over.</summary>
        public static void FixEquipped()
        {
            PlayerInventory inv = Carried;
            if (inv == null || inv.EquippedWeapon == null) return;
            if (inv.Count(inv.EquippedWeapon.Id) <= 0) inv.EquipBareHands();
        }

        #region Worn pieces
        /// <summary>A stack-shaped copy of a worn piece (for APIs that take an <see cref="ItemStack"/>).</summary>
        public static ItemStack ProxyOf(PlayerClothing.WornPiece p)
        {
            if (!p.Exists) return null;
            return new ItemStack(p.Def, 1)
            {
                Condition = p.Condition,
                TimesRepaired = p.TimesRepaired,
                Extra = p.Extra != null ? p.Extra.Clone() : null,
            };
        }

        /// <summary>
        /// Re-dresses a worn slot with <paramref name="def"/> (the same piece with new wear / extras, or a
        /// replacement such as a bigger secure pouch). Whatever was stowed in the piece's own pockets is put back
        /// on the same cells, so the pack's arrangement survives the swap.
        /// </summary>
        public static bool ReplaceWorn(ClothingSlot slot, ItemDef def, int condition, int timesRepaired, ItemExtra extra)
        {
            PlayerClothing clothing = Clothing;
            if (clothing == null || def == null) return false;
            PlayerClothing.WornPiece old = clothing.GetPiece(slot);
            if (!old.Exists) return false;

            PlayerInventory inv = Carried;
            InventoryLayout layout = inv != null ? inv.Layout : null;
            GridSection oldSection = FindSection(layout, old.Def, slot);
            var stowed = new List<ItemStack>();
            var cells = new List<Vector3Int>();
            if (oldSection != null)
            {
                List<ItemStack> stacks = inv.Inventory.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                {
                    if (!ReferenceEquals(stacks[i].Section, oldSection)) continue;
                    stowed.Add(stacks[i]);
                    cells.Add(new Vector3Int(stacks[i].GridX, stacks[i].GridY, stacks[i].Rotated ? 1 : 0));
                }
            }

            clothing.Remove(slot);
            clothing.Equip(def, def.IsDamageable ? condition : -1, timesRepaired, extra);

            if (layout != null && stowed.Count > 0)
            {
                GridSection fresh = FindSection(layout, def, slot);
                List<ItemStack> stacks = inv.Inventory.Stacks;
                for (int i = 0; i < stowed.Count; i++)
                    if (stacks.Contains(stowed[i])) stowed[i].Section = null;
                for (int i = 0; i < stowed.Count; i++)
                {
                    ItemStack st = stowed[i];
                    if (!stacks.Contains(st)) continue;
                    bool placed = fresh != null && layout.Move(st, fresh, cells[i].x, cells[i].y, cells[i].z == 1, false);
                    if (!placed && !layout.AutoPlace(st, true)) layout.AutoPlace(st, false);
                }
            }
            return true;
        }

        private static GridSection FindSection(InventoryLayout layout, ItemDef def, ClothingSlot slot)
        {
            if (layout == null || def == null) return null;
            for (int i = 0; i < layout.Sections.Count; i++)
                if (layout.Sections[i].Source == def && layout.Sections[i].Slot == slot) return layout.Sections[i];
            return null;
        }

        /// <summary>Dresses the body in the secure pouch the character's vault level calls for (after a vault upgrade).</summary>
        public static void SyncPouch(CharacterProfile c)
        {
            PlayerClothing clothing = Clothing;
            if (clothing == null || c == null) return;
            ItemDef want = ItemDatabase.Get(ZoneContent.PouchIdForLevel(c.PouchLevel));
            ItemDef worn = clothing.GetWorn(ClothingSlot.Pouch);
            if (want == null || worn == null || worn == want) return;
            ReplaceWorn(ClothingSlot.Pouch, want, -1, 0, null);
        }

        /// <summary>The secure pouch's grid section on the body, or null.</summary>
        public static GridSection PouchSection
        {
            get
            {
                PlayerInventory inv = Carried;
                return inv != null ? inv.PouchSection : null;
            }
        }

        public static bool InPouch(ItemStack s) => s != null && s.Section != null && s.Section.Source != null && s.Section.Slot == ClothingSlot.Pouch;
        #endregion

        #region Gathering
        /// <summary>
        /// Collects items matching <paramref name="filter"/> from the worn slots, the carried pack and the stash
        /// (in that order), each group sorted by category and name. The secure pouch itself is never offered.
        /// </summary>
        public static void Collect(List<HubItemRef> into, Func<ItemDef, bool> filter, bool worn, bool kit, bool stash)
        {
            into.Clear();
            if (worn)
            {
                PlayerClothing clothing = Clothing;
                if (clothing != null)
                {
                    ClothingSlot[] slots = PlayerClothing.Slots;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == ClothingSlot.Pouch) continue;
                        PlayerClothing.WornPiece p = clothing.GetPiece(slots[i]);
                        if (!p.Exists || (filter != null && !filter(p.Def))) continue;
                        into.Add(new HubItemRef { Source = HubItemSource.Worn, Slot = slots[i], Stack = ProxyOf(p) });
                    }
                }
            }
            if (kit) AddFrom(into, Pack, HubItemSource.Kit, filter);
            if (stash) AddFrom(into, ProfileStore.StashInventory, HubItemSource.Stash, filter);
        }

        private static void AddFrom(List<HubItemRef> into, Inventory inv, HubItemSource source, Func<ItemDef, bool> filter)
        {
            if (inv == null) return;
            List<ItemStack> sorted = HubUI.Sorted(inv, s => filter == null || filter(s.Def));
            for (int i = 0; i < sorted.Count; i++)
                into.Add(new HubItemRef { Source = source, Owner = inv, Stack = sorted[i] });
        }
        #endregion
    }
}
