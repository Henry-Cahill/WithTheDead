using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The kit a survivor takes into a raid: whatever they extracted with last time, or a fresh
    /// default kit after a death / a missing-in-action / the first raid. The extracted kit is kept in
    /// its own JSON file (<see cref="FilePath"/>), entirely apart from the wave-survival save, which
    /// raids never touch.
    /// </summary>
    public static class RaidLoadout
    {
        /// <summary>File name of the extracted kit under <see cref="Application.persistentDataPath"/>.</summary>
        public const string FileName = "clonez_raid_kit.json";
        private const int KitVersion = 1;

        /// <summary>Full path of the extracted-kit file.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>True when a kit carried out of an earlier raid is waiting for the next one.</summary>
        public static bool HasSavedKit => File.Exists(FilePath);

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
            RaidKit kit = Load();
            if (kit != null && !kit.IsEmpty) Restore(kit, inventory, clothing);
            else ApplyDefault(inventory, clothing);
        }

        /// <summary>Writes the survivor's current kit (worn clothing with its wear / charge, the pack,
        /// the weapon in hand and its loaded rounds) to disk for the next raid. Called on a successful
        /// extraction. Returns the kit that was written (null when there was no survivor).</summary>
        public static RaidKit SaveExtracted(PlayerStats player)
        {
            RaidKit kit = Capture(player);
            if (kit == null) return null;
            RaidKit previous = Load();
            kit.Streak = (previous != null ? Mathf.Max(0, previous.Streak) : 0) + 1;
            Write(kit);
            return kit;
        }

        /// <summary>Forfeits the extracted kit (death, missing in action, abandoning the raid): the next
        /// raid starts with the default kit.</summary>
        public static void Clear()
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
                    clothing.Equip(def, def.RestoredCondition(p.Condition), p.TimesRepaired);
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
    }
}
