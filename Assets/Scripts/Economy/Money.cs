using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The money: military-grade 5.45 rounds (<see cref="ZoneContent.Currency"/>). It is a real item — it sits in
    /// the stash, it can be carried into a raid (and lost there), paid at an exit, or loaded into a rifle and
    /// fired, which spends your savings one shot at a time.
    /// </summary>
    public static class Money
    {
        /// <summary>The item ID for money (currency).</summary>
        public static string Id => ZoneContent.Currency;

        /// <summary>Formats an amount of money with thousand separators and the MG suffix.</summary>
        public static string Format(int amount) => amount.ToString("N0") + " MG";

        /// <summary>Counts the total amount of money stored in the given inventory.</summary>
        public static int Count(Inventory inv)
        {
            if (inv == null) return 0;
            int n = 0;
            for (int i = 0; i < inv.Stacks.Count; i++)
                if (inv.Stacks[i].Def != null && inv.Stacks[i].Def.Id == Id) n += inv.Stacks[i].Count;
            return n;
        }

        /// <summary>Money in the stash plus, when given, in the pack.</summary>
        public static int Available(PlayerInventory carried = null)
            => Count(ProfileStore.StashInventory) + (carried != null ? Count(carried.Inventory) : 0);

        /// <summary>Takes <paramref name="amount"/> from the stash, then the pack. All or nothing.</summary>

        /// <summary>Takes <paramref name="amount"/> from the stash, then the pack. All or nothing.</summary>
        public static bool TrySpend(int amount, PlayerInventory carried = null)
        {
            if (amount == 0) return true; if (amount < 0) return false;
            if (Available(carried) < amount) return false;
            int left = Take(ProfileStore.StashInventory, amount);
            if (left > 0 && carried != null) left = Take(carried.Inventory, left);
            return left <= 0;
        }


        public static int Take(Inventory inv, int amount)
        {
            if (inv == null) return amount;
            for (int i = inv.Stacks.Count - 1; i >= 0 && amount > 0; i--)
            {
                ItemStack s = inv.Stacks[i];
                if (s.Def == null || s.Def.Id != Id) continue;
                int t = Mathf.Min(amount, s.Count);
                inv.Remove(s, t);
                amount -= t;
            }
            return amount;
        }

        /// <summary>Pays money into the stash (sales, quest rewards, insurance refunds).</summary>


        public static void Give(int amount)
        {
            if (amount <= 0) return;
            ItemDef def = ItemDatabase.Get(Id);
            Inventory stash = ProfileStore.StashInventory;
            if (def == null || stash == null) return;
            stash.Add(def, amount, enforceWeight: false);
        }

        /// <summary>A rough market value for an item (traders and the flea market start from this).</summary>

        public static int BaseValue(ItemDef d)
        {
            if (d == null) return 0;
            if (d.Id == Id) return 1;
            ArtifactCatalog.Def art = ArtifactCatalog.Get(d.Id);
            if (art != null) return art.Value;
            AmmoStats ammo = Ballistics.Ammo(d.Id);
            if (ammo != null) return Mathf.Max(1, Mathf.RoundToInt(ammo.Damage * 0.02f + ammo.Penetration * 0.06f));
            ArmorStats armor = Ballistics.Armor(d.Id);
            if (armor != null) return 350 + armor.Class * armor.Class * 260 + armor.ArtifactSlots * 400;
            ModStats mod = Ballistics.Mod(d.Id);
            if (mod != null) return mod.Zoom > 2f ? 1600 : mod.Noise < 0.6f ? 1300 : 450;
            if (d.IsFirearm) return 600 + d.MagazineSize * 25 + Mathf.RoundToInt(d.FirearmRange * 8f);
            if (d.IsWeapon) return 80 + Mathf.RoundToInt(d.WeaponDamage * 90f);
            if (d.IsWornContainer) return 120 + Mathf.RoundToInt(d.ContainerCapacity * 40f);
            if (d.IsClothing) return 60 + Mathf.RoundToInt((d.Insulation * 600f) + d.BiteDefense * 200f);
            switch (d.Category)
            {
                case ItemCategory.Medical: return 90;
                case ItemCategory.Food: return 25 + Mathf.RoundToInt(d.HungerRestore * 200f);
                case ItemCategory.Drink: return 20 + Mathf.RoundToInt(d.ThirstRestore * 160f);
                case ItemCategory.Ammo: return 2;
                case ItemCategory.Material: return 30 + Mathf.RoundToInt(d.Weight * 10f);
                default: return 60;
            }
        }
    }

    /// <summary>What a fresh character owns: a little money, a basic kit to wear, a few things in the stash.</summary>
    public static class StarterPack
    {
        public static void Apply(CharacterProfile c)
        {
            ItemDatabase.EnsureInit();
            c.Stash.Clear();
            AddStash(c, ZoneContent.Currency, 2500);
            AddStash(c, "ammo9mm", 60);
            AddStash(c, "bandage", 4);
            AddStash(c, "splint", 1);
            AddStash(c, "fieldkit", 1);
            AddStash(c, "crackers", 3);
            AddStash(c, "waterbottle", 2);
            AddStash(c, "gasmaskfilter", 1);
            AddStash(c, "bolt", 20);
            AddStash(c, "detector_echo", 1);
            AddStash(c, "map_zone", 1, "kestrel");
            AddStash(c, "compass", 1);
            AddStash(c, "cleaningkit", 1);

            c.Kit = new RaidKit { Version = 1, SavedAtUtc = System.DateTime.UtcNow.ToString("o") };
            string[] worn = { "greenbeanie", "camojacket", "cargopants", "boots", "gloves", "schoolbag", ZoneContent.PouchIdForLevel(0) };
            foreach (string id in worn)
            {
                ItemDef d = ItemDatabase.Get(id);
                if (d != null && d.IsClothing) c.Kit.Clothing.Add(new RaidKitPiece { Slot = (int)d.ClothingSlot, Id = id, Condition = d.MaxCondition });
            }
            AddKit(c, "pistol", 1);
            AddKit(c, "ammo9mm", 24);
            AddKit(c, "kitchenknife", 1);
            AddKit(c, "flashlight", 1);
            AddKit(c, "battery", 1);
            AddKit(c, "bandage", 2);
            AddKit(c, "waterbottle", 1);
            AddKit(c, "crackers", 2);
            AddKit(c, "bolt", 10);
            AddKit(c, "watch_wrist", 1);
            c.Kit.EquippedWeaponId = "pistol";
            c.Kit.LoadedAmmo = 15;

            ProfessionCatalog.ApplyKit(c);

            foreach (string trader in TraderCatalog.Ids) c.Trader(trader);
            Hideout.InitModules(c);
            if (string.IsNullOrEmpty(c.BloodType)) c.BloodType = BloodTypes.Roll();
        }

        public static void AddStash(CharacterProfile c, string id, int count, string note = null)
        {
            ItemDef d = ItemDatabase.Get(id);
            if (d == null || count <= 0) return;
            while (count > 0)
            {
                int n = Mathf.Min(count, Mathf.Max(1, d.MaxStack));
                c.Stash.Add(new StackSave
                {
                    Id = id, Count = n, Condition = d.MaxCondition,
                    Extra = note != null ? ItemExtra.Serialize(new ItemExtra { Note = note }) : null,
                });
                count -= n;
            }
        }

        public static void AddKit(CharacterProfile c, string id, int count)
        {
            ItemDef d = ItemDatabase.Get(id);
            if (d == null || count <= 0) return;
            while (count > 0)
            {
                int n = Mathf.Min(count, Mathf.Max(1, d.MaxStack));
                c.Kit.Items.Add(new RaidKitStack { Id = id, Count = n, Condition = d.MaxCondition });
                count -= n;
            }
        }
    }
}
