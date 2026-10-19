using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace CloneZ
{
    /// <summary>
    /// A feature that contributes items, loot-table entries or patches to existing items (ammunition and armour,
    /// artifacts, medical kit...). Every implementation in the game assembly is found and run once, right after
    /// the content database loads, in <see cref="Order"/>.
    /// </summary>
    public interface IContentProvider
    {
        int Order { get; }
        void Register(ContentBuilder b);
    }

    /// <summary>
    /// Code-side content. The SQLite content database stays the source of truth for any id it already holds — a
    /// row there always wins, so designers can override a code item without recompiling — and code fills in
    /// the items the database doesn't know yet (new features) and appends loot entries.
    /// </summary>
    public static class ContentRegistry
    {
        private static bool s_Applied;

        internal static void Apply(Dictionary<string, ItemDef> items, Dictionary<ContainerType, ItemDatabase.LootTable> tables)
        {
            if (s_Applied) return;
            s_Applied = true;
            var b = new ContentBuilder(items, tables);
            foreach (IContentProvider p in FindProviders())
            {
                try { p.Register(b); }
                catch (Exception e) { Debug.LogError("[CloneZ] Content provider " + p.GetType().Name + " failed: " + e); }
            }
            b.FlushLoot();
            Debug.Log("[CloneZ] Code content registered: " + b.Added + " new items, " + b.Patched + " patched, " + b.LootAdded + " loot entries.");
        }

        private static IEnumerable<IContentProvider> FindProviders()
        {
            Type iface = typeof(IContentProvider);
            return typeof(ContentRegistry).Assembly.GetTypes()
                .Where(t => iface.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (IContentProvider)Activator.CreateInstance(t))
                .OrderBy(p => p.Order);
        }
    }

    /// <summary>The handle content providers write through.</summary>
    [Preserve]
    public sealed class ContentBuilder
    {
        private readonly Dictionary<string, ItemDef> _items;
        private readonly Dictionary<ContainerType, ItemDatabase.LootTable> _tables;
        private readonly Dictionary<ContainerType, List<ItemDatabase.LootEntry>> _pendingLoot = new Dictionary<ContainerType, List<ItemDatabase.LootEntry>>();
        private readonly HashSet<string> _fromDb;

        public int Added { get; private set; }
        public int Patched { get; private set; }
        public int LootAdded { get; private set; }

        internal ContentBuilder(Dictionary<string, ItemDef> items, Dictionary<ContainerType, ItemDatabase.LootTable> tables)
        {
            _items = items;
            _tables = tables;
            _fromDb = new HashSet<string>(items.Keys);
        }

        /// <summary>True when the database itself defines this id (code must then leave its values alone).</summary>
        public bool InDatabase(string id) => _fromDb.Contains(id);

        public ItemDef Get(string id) => id != null && _items.TryGetValue(id, out ItemDef d) ? d : null;

        /// <summary>Defines an item unless the database already has one with this id (then that one is returned).</summary>
        public ItemDef Item(string id, string name, ItemCategory category, float weight, int maxStack = 1,
            int sizeW = 1, int sizeH = 1, string description = null, Color? tint = null)
        {
            if (_items.TryGetValue(id, out ItemDef existing)) return existing;
            var def = new ItemDef
            {
                Id = id,
                DisplayName = name,
                Category = category,
                Weight = weight,
                MaxStack = Mathf.Max(1, maxStack),
                SizeW = sizeW,
                SizeH = sizeH,
                Description = description,
                Tint = tint ?? DefaultTint(category),
            };
            _items[id] = def;
            Added++;
            return def;
        }

        /// <summary>Edits a code-defined item, or a database item the database leaves blank in that respect (the
        /// patch decides). Returns false when the item doesn't exist.</summary>
        public bool Patch(string id, Action<ItemDef> patch)
        {
            ItemDef d = Get(id);
            if (d == null || patch == null) return false;
            patch(d);
            Patched++;
            return true;
        }

        /// <summary>Adds a weighted entry to a container type's loot table.</summary>
        public void Loot(ContainerType table, string id, int weight, int min = 1, int max = 1)
        {
            if (weight <= 0 || Get(id) == null) return;
            if (!_pendingLoot.TryGetValue(table, out List<ItemDatabase.LootEntry> list))
                _pendingLoot[table] = list = new List<ItemDatabase.LootEntry>();
            list.Add(new ItemDatabase.LootEntry(id, weight, min, Mathf.Max(min, max)));
        }

        internal void FlushLoot()
        {
            foreach (KeyValuePair<ContainerType, List<ItemDatabase.LootEntry>> kv in _pendingLoot)
            {
                if (!_tables.TryGetValue(kv.Key, out ItemDatabase.LootTable t))
                    _tables[kv.Key] = t = new ItemDatabase.LootTable { MinRolls = 1, MaxRolls = 3, EmptyWeight = 10, Entries = new ItemDatabase.LootEntry[0] };
                var merged = new List<ItemDatabase.LootEntry>(t.Entries ?? new ItemDatabase.LootEntry[0]);
                merged.AddRange(kv.Value);
                t.Entries = merged.ToArray();
                LootAdded += kv.Value.Count;
            }
            _pendingLoot.Clear();
        }

        private static Color DefaultTint(ItemCategory c)
        {
            switch (c)
            {
                case ItemCategory.Food: return new Color(0.75f, 0.55f, 0.32f);
                case ItemCategory.Drink: return new Color(0.40f, 0.62f, 0.78f);
                case ItemCategory.Medical: return new Color(0.85f, 0.85f, 0.88f);
                case ItemCategory.Weapon: return new Color(0.55f, 0.55f, 0.58f);
                case ItemCategory.Firearm: return new Color(0.25f, 0.26f, 0.27f);
                case ItemCategory.Ammo: return new Color(0.78f, 0.62f, 0.30f);
                case ItemCategory.Material: return new Color(0.55f, 0.50f, 0.42f);
                case ItemCategory.Clothing: return new Color(0.38f, 0.42f, 0.34f);
                default: return new Color(0.6f, 0.6f, 0.62f);
            }
        }
    }
}
