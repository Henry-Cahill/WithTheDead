using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The kit a survivor takes into a raid and brings back out. With a profile (the normal game) the kit
    /// belongs to the active character: it is dressed onto the body at insertion, written back on extraction,
    /// and on a death only the secure pouch's contents survive — insured gear is filed with the trader, and
    /// under the corpse-recovery rule everything else waits on the body in the zone. A scav run gets a random
    /// kit and its haul goes straight to the stash. Without a profile (old saves) the original single-file kit
    /// is used.
    /// </summary>
    public static class RaidLoadout
    {
        /// <summary>File name of the pre-profile extracted kit under <see cref="Application.persistentDataPath"/>.</summary>
        public const string FileName = "clonez_raid_kit.json";
        private const int KitVersion = 1;

        /// <summary>Full path of the pre-profile extracted-kit file.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>True when a kit carried out of an earlier raid is waiting for the next one.</summary>
        public static bool HasSavedKit
        {
            get
            {
                CharacterProfile c = ProfileStore.ActiveCharacter;
                return c != null ? c.Kit != null && !c.Kit.IsEmpty : File.Exists(FilePath);
            }
        }

        // The fresh kit: a winter outfit with a school bag, a sidearm and the bare essentials.
        private static readonly string[] s_DefaultClothing =
            { "greenbeanie", "camojacket", "cargopants", "boots", "gloves", "schoolbag" };

        private static readonly (string Id, int Count)[] s_DefaultItems =
        {
            ("pistol", 1), ("ammo9mm", 24), ("kitchenknife", 1), ("flashlight", 1), ("battery", 1),
            ("bandage", 2), ("waterbottle", 1), ("crackers", 2),
        };

        private const string DefaultWeapon = "pistol";

        /// <summary>Dresses and packs the survivor for a raid. Clothing first (worn bags add the grid
        /// sections the items then pack into), then the items, then the weapon in hand.</summary>
        public static void Apply(PlayerInventory inventory, PlayerClothing clothing)
        {
            ItemDatabase.EnsureInit();
            if (GameSession.IsScavRun) { ApplyScavKit(inventory, clothing); return; }

            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null)
            {
                if (c.Kit != null && !c.Kit.IsEmpty) Restore(c.Kit, inventory, clothing);
                EnsurePouch(clothing, c.PouchLevel);
                if (c.Kit != null) PlacePouchItems(c.Kit, inventory);
                return;
            }

            RaidKit kit = LoadLegacyFile();
            if (kit != null && !kit.IsEmpty) Restore(kit, inventory, clothing);
            else ApplyDefault(inventory, clothing);
        }

        /// <summary>Writes the survivor's current kit (worn clothing with its wear / charge, the pack,
        /// the weapon in hand and its loaded rounds) back for the next raid. Called on a successful
        /// extraction. Returns the kit that was written (null when there was no survivor).</summary>
        public static RaidKit SaveExtracted(PlayerStats player)
        {
            RaidKit kit = Capture(player);
            if (kit == null) return null;
            if (GameSession.IsMonsterRun) return kit;   // a hunter brings nothing home but its pay
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null)
            {
                if (GameSession.IsScavRun)
                {
                    // A scav's haul is the prize: everything carried out lands in the main character's stash.
                    DepositToStash(kit);
                    kit.Streak = 0;
                }
                else
                {
                    kit.Streak = (c.Kit != null ? Mathf.Max(0, c.Kit.Streak) : 0) + 1;
                    c.Kit = kit;
                }
                ProfileStore.Save();
                return kit;
            }

            RaidKit previous = LoadLegacyFile();
            kit.Streak = (previous != null ? Mathf.Max(0, previous.Streak) : 0) + 1;
            Write(kit);
            return kit;
        }

        /// <summary>
        /// The kit is lost (death, missing in action, abandoning the raid). Only what sits in the secure pouch
        /// comes home; insured pieces are filed with their trader (returned in a few days unless someone took
        /// them); under the corpse-recovery rule the rest waits on the body where it fell. Returns the kit as it
        /// was carried (for the raid report).
        /// </summary>
        public static RaidKit Forfeit(PlayerStats player)
        {
            RaidKit carried = Capture(player);
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null) { DeleteLegacyFile(); return carried; }
            if (GameSession.IsBorrowedBody) return carried;   // the scav's kit (or the hunter's claws) was never yours

            var kept = new RaidKit { Version = KitVersion, SavedAtUtc = DateTime.UtcNow.ToString("o") };
            var lost = new List<StackSave>();
            if (carried != null)
            {
                for (int i = 0; i < carried.Clothing.Count; i++)
                {
                    RaidKitPiece p = carried.Clothing[i];
                    if (p.Slot == (int)ClothingSlot.Pouch) kept.Clothing.Add(p);
                    else lost.Add(new StackSave { Id = p.Id, Count = 1, Condition = p.Condition, TimesRepaired = p.TimesRepaired, Extra = p.Extra });
                }
                for (int i = 0; i < carried.Items.Count; i++)
                {
                    RaidKitStack s = carried.Items[i];
                    if (s.InPouch) kept.Items.Add(s);
                    else lost.Add(new StackSave { Id = s.Id, Count = s.Count, Condition = s.Condition, TimesRepaired = s.TimesRepaired, Extra = s.Extra });
                }
            }
            c.Kit = kept;
            InsuranceDesk.FileClaims(c, lost, player != null ? player.transform.position : Vector3.zero);
            if (SandboxSettings.Current.Death == DeathPenalty.CorpseRecovery && player != null && lost.Count > 0)
                CorpseRecovery.Record(c, GameSession.ZoneId, player.transform.position, lost);
            ProfileStore.Save();
            return carried;
        }

        /// <summary>Forfeits the kit without a survivor at hand (abandoned from a menu). Kept for older callers.</summary>
        public static void Clear()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null) { DeleteLegacyFile(); return; }
            Forfeit(GameManager.Instance != null ? GameManager.Instance.Player : null);
        }

        /// <summary>Reads the pre-profile kit file, or null.</summary>
        public static RaidKit LoadLegacyFile() => Load();

        public static void DeleteLegacyFile()
        {
            try
            {
                string path = FilePath;
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Could not clear the raid kit: " + e.Message);
            }
        }

        // Everything a scav carried out goes into the stash (worn pieces too).
        private static void DepositToStash(RaidKit kit)
        {
            Inventory stash = ProfileStore.StashInventory;
            if (stash == null || kit == null) return;
            for (int i = 0; i < kit.Clothing.Count; i++)
            {
                RaidKitPiece p = kit.Clothing[i];
                ProfileStore.AddSave(stash, new StackSave { Id = p.Id, Count = 1, Condition = p.Condition, TimesRepaired = p.TimesRepaired, Extra = p.Extra });
            }
            for (int i = 0; i < kit.Items.Count; i++)
            {
                RaidKitStack s = kit.Items[i];
                ProfileStore.AddSave(stash, new StackSave { Id = s.Id, Count = s.Count, Condition = s.Condition, TimesRepaired = s.TimesRepaired, Extra = s.Extra });
            }
        }

        // The secure pouch is always worn (its size follows the character's progress).
        private static void EnsurePouch(PlayerClothing clothing, int level)
        {
            if (clothing == null) return;
            ItemDef pouch = ItemDatabase.Get(ZoneContent.PouchIdForLevel(level));
            if (pouch == null) return;
            ItemDef worn = clothing.GetWorn(ClothingSlot.Pouch);
            if (worn != pouch) clothing.Equip(pouch);
        }

        // Items that were in the pouch go back into it, so a second death keeps them too.
        private static void PlacePouchItems(RaidKit kit, PlayerInventory inventory)
        {
            if (inventory == null || kit == null) return;
            GridSection pouch = inventory.PouchSection;
            InventoryLayout layout = inventory.Layout;
            if (pouch == null || layout == null) return;
            var wanted = new List<string>();
            for (int i = 0; i < kit.Items.Count; i++) if (kit.Items[i].InPouch) wanted.Add(kit.Items[i].Id);
            if (wanted.Count == 0) return;
            List<ItemStack> stacks = inventory.Inventory.Stacks;
            for (int w = 0; w < wanted.Count; w++)
            {
                for (int i = 0; i < stacks.Count; i++)
                {
                    ItemStack st = stacks[i];
                    if (st.Def == null || st.Def.Id != wanted[w] || st.Section == pouch) continue;
                    if (MoveIntoSection(layout, st, pouch)) break;
                }
            }
        }

        private static bool MoveIntoSection(InventoryLayout layout, ItemStack st, GridSection section)
        {
            for (int rot = 0; rot < 2; rot++)
                for (int y = 0; y < section.Height; y++)
                    for (int x = 0; x < section.Width; x++)
                        if (layout.Fits(st, section, x, y, rot == 1, false) && layout.Move(st, section, x, y, rot == 1, false))
                            return true;
            return false;
        }

        // A scav's random kit: cheap clothes, a cheap gun, a little ammo and junk. Nothing from the stash.
        private static void ApplyScavKit(PlayerInventory inventory, PlayerClothing clothing)
        {
            var rng = new System.Random(GameSession.RaidSeed ^ 0x5CA7);
            string[][] outfits =
            {
                new[] { "beanie", "hoodie", "jeans", "sneakers", "fannypack" },
                new[] { "cap", "jacket", "trousers", "boots", "schoolbag" },
                new[] { "greenbeanie", "leatherjacket", "cargopants", "boots", "hikingbag" },
            };
            string[] outfit = outfits[rng.Next(outfits.Length)];
            if (clothing != null)
                foreach (string id in outfit)
                {
                    ItemDef d = ItemDatabase.Get(id);
                    if (d != null && d.IsClothing) clothing.Equip(d, Mathf.RoundToInt(d.MaxCondition * (0.4f + (float)rng.NextDouble() * 0.5f)));
                }
            if (inventory == null) return;
            string[] guns = { "pistol", "shotgun", "smg9", "mosin" };
            string gun = guns[rng.Next(guns.Length)];
            ItemDef g = ItemDatabase.Get(gun);
            if (g != null)
            {
                inventory.AddStarter(gun, 1);
                ItemStack gs = inventory.Inventory.Stacks.Find(s => s.Def == g);
                if (gs != null && g.MaxCondition > 0) gs.Condition = Mathf.Max(1, Mathf.RoundToInt(g.MaxCondition * (0.35f + (float)rng.NextDouble() * 0.4f)));
                inventory.AddStarter(g.AmmoId, g.MagazineSize + rng.Next(0, g.MagazineSize * 2));
                inventory.Equip(g);
            }
            string[] junk = { "bandage", "crackers", "waterbottle", "bolt", "rag", "cannedbeans", "scrap", "chemicals", "lighter" };
            for (int i = 0; i < 4; i++) inventory.AddStarter(junk[rng.Next(junk.Length)], 1 + rng.Next(3));
        }

        /// <summary>Snapshots what the survivor is wearing and carrying right now (nothing is written).</summary>
        public static RaidKit Capture(PlayerStats player)
        {
            if (player == null) return null;
            var kit = new RaidKit { Version = KitVersion, SavedAtUtc = DateTime.UtcNow.ToString("o") };

            var clothing = player.GetComponent<PlayerClothing>();
            if (clothing != null)
            {
                ClothingSlot[] slots = PlayerClothing.Slots;
                for (int i = 0; i < slots.Length; i++)
                {
                    PlayerClothing.WornPiece piece = clothing.GetPiece(slots[i]);
                    if (!piece.Exists) continue;
                    kit.Clothing.Add(new RaidKitPiece
                    {
                        Slot = (int)slots[i],
                        Id = piece.Def.Id,
                        Condition = piece.Condition,
                        TimesRepaired = piece.TimesRepaired,
                        Extra = ItemExtra.Serialize(piece.Extra),
                    });
                }
            }

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null && inventory.Inventory != null)
            {
                // Perishables carry their age rather than a clock stamp: every raid restarts the clock.
                float now = TimeOfDay.Now;
                List<ItemStack> stacks = inventory.Inventory.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                {
                    ItemStack st = stacks[i];
                    if (st == null || st.Def == null || st.Count <= 0) continue;
                    kit.Items.Add(new RaidKitStack
                    {
                        Id = st.Def.Id,
                        Count = st.Count,
                        AgeHours = st.Def.Perishable ? Mathf.Max(0f, now - st.SpawnHours) : 0f,
                        Condition = st.Condition,
                        TimesRepaired = st.TimesRepaired,
                        Extra = ItemExtra.Serialize(st.Extra),
                        InPouch = st.Section != null && st.Section.Source != null && st.Section.Slot == ClothingSlot.Pouch,
                    });
                }
                kit.EquippedWeaponId = inventory.EquippedWeapon != null ? inventory.EquippedWeapon.Id : null;
            }

            var combat = player.GetComponent<PlayerCombat>();
            if (combat != null && combat.EquippedFirearm != null) kit.LoadedAmmo = combat.LoadedAmmo;
            return kit;
        }

        /// <summary>Reads the extracted kit from disk, or null when there is none (or it can't be read).</summary>
        public static RaidKit Load()
        {
            try
            {
                string path = FilePath;
                if (!File.Exists(path)) return null;
                RaidKit kit = JsonUtility.FromJson<RaidKit>(File.ReadAllText(path));
                if (kit == null || kit.Version <= 0) return null;
                if (kit.Clothing == null) kit.Clothing = new List<RaidKitPiece>();
                if (kit.Items == null) kit.Items = new List<RaidKitStack>();
                return kit;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Could not read the raid kit (starting with the default kit): " + e.Message);
                return null;
            }
        }

        private static void Write(RaidKit kit)
        {
            try
            {
                // Write beside the real file first so a crash mid-write never leaves a torn kit behind.
                string path = FilePath;
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(kit, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Could not save the raid kit: " + e.Message);
            }
        }

        private static void Restore(RaidKit kit, PlayerInventory inventory, PlayerClothing clothing)
        {
            if (clothing != null)
            {
                for (int i = 0; i < kit.Clothing.Count; i++)
                {
                    RaidKitPiece p = kit.Clothing[i];
                    ItemDef def = p != null && !string.IsNullOrEmpty(p.Id) ? ItemDatabase.Get(p.Id) : null;
                    if (def == null || !def.IsClothing) continue;
                    clothing.Equip(def, def.RestoredCondition(p.Condition), p.TimesRepaired, ItemExtra.Parse(p.Extra));
                }
            }
            if (inventory == null) return;

            float now = TimeOfDay.Now;
            var items = new List<StackSave>(kit.Items.Count);
            bool weaponCarried = false;
            for (int i = 0; i < kit.Items.Count; i++)
            {
                RaidKitStack st = kit.Items[i];
                if (st == null || string.IsNullOrEmpty(st.Id) || st.Count <= 0) continue;
                if (ItemDatabase.Get(st.Id) == null) continue;   // an item since removed from the catalogue
                items.Add(new StackSave
                {
                    Id = st.Id,
                    Count = st.Count,
                    SpawnHours = now - Mathf.Max(0f, st.AgeHours),
                    Condition = st.Condition,
                    TimesRepaired = st.TimesRepaired,
                    Extra = st.Extra,
                });
                if (st.Id == kit.EquippedWeaponId) weaponCarried = true;
            }

            // Only re-arm with a weapon that actually came back in the pack.
            inventory.RestoreState(items, weaponCarried ? kit.EquippedWeaponId : null, kit.LoadedAmmo);
        }

        private static void ApplyDefault(PlayerInventory inventory, PlayerClothing clothing)
        {
            if (clothing != null)
            {
                for (int i = 0; i < s_DefaultClothing.Length; i++)
                {
                    ItemDef def = ItemDatabase.Get(s_DefaultClothing[i]);
                    if (def != null) clothing.Equip(def);
                }
            }
            if (inventory == null) return;
            for (int i = 0; i < s_DefaultItems.Length; i++)
                inventory.AddStarter(s_DefaultItems[i].Id, s_DefaultItems[i].Count);
            ItemDef weapon = ItemDatabase.Get(DefaultWeapon);
            if (weapon != null) inventory.Equip(weapon);   // chambers a magazine from the pack
        }
    }

    /// <summary>The kit carried out of a raid, as written to <see cref="RaidLoadout.FilePath"/> (JSON
    /// via <see cref="JsonUtility"/>, so plain fields and lists only).</summary>
    [Serializable]
    public class RaidKit
    {
        public int Version;
        public string SavedAtUtc;
        /// <summary>Raids survived in a row with this kit.</summary>
        public int Streak;
        public List<RaidKitPiece> Clothing = new List<RaidKitPiece>();
        public List<RaidKitStack> Items = new List<RaidKitStack>();
        /// <summary>The weapon in hand (null for bare hands) and, for a firearm, the rounds in it.</summary>
        public string EquippedWeaponId;
        public int LoadedAmmo;

        /// <summary>True when nothing at all was carried out (the next raid then gets the default kit).</summary>
        public bool IsEmpty => (Clothing == null || Clothing.Count == 0) && (Items == null || Items.Count == 0);
    }

    /// <summary>A worn piece in a <see cref="RaidKit"/>: slot, item id and its wear (or, for night vision
    /// and gas masks, its charge) plus repair history.</summary>
    [Serializable]
    public class RaidKitPiece
    {
        public int Slot;                // (int)ClothingSlot
        public string Id;
        public int Condition;
        public int TimesRepaired;
        public string Extra;            // ItemExtra.Serialize; null when plain
    }

    /// <summary>A carried stack in a <see cref="RaidKit"/>. Perishables keep their age in in-game hours.</summary>
    [Serializable]
    public class RaidKitStack
    {
        public string Id;
        public int Count;
        public float AgeHours;
        public int Condition;
        public int TimesRepaired;
        public string Extra;            // ItemExtra.Serialize; null when plain
        public bool InPouch;            // carried in the secure pouch (survives death)
    }
}
