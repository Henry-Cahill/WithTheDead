using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Every item lying on the ground in the world, held as data rather than as scene objects.
    ///
    /// This replaces one GameObject-and-MonoBehaviour per dropped item. That arrangement could not
    /// survive the item counts a sandbox world reaches: registering a spawn walked the whole list, so
    /// filling the world was quadratic; every item ran an <c>Update</c> every frame; and each one
    /// carried a few hundred child renderers. A packed record is about thirty bytes, so a million
    /// items is a few tens of megabytes and no per-frame cost at all beyond the ones being drawn.
    ///
    /// Items are filed by world chunk. That keeps lookups local — the player interacting, or the
    /// renderer gathering what is visible, only ever touches the handful of chunks nearby instead of
    /// scanning the world — and it gives chunk streaming a natural unit to save and drop. Items in an
    /// unloaded chunk keep existing; they are simply not being drawn.
    /// </summary>
    public static class GroundItemStore
    {
        /// <summary>Metres across one chunk of ground. Chosen so a player's interaction radius only
        /// ever touches a couple of chunks.</summary>
        public const float ChunkSize = 32f;

        /// <summary>
        /// One item, or one stack of them, lying somewhere.
        ///
        /// Kept deliberately small and free of references: the item type is an index into
        /// <see cref="GroundItemStore"/>'s type table rather than a string, and the facing is a single
        /// byte, because this struct is multiplied by the size of the world.
        /// </summary>
        public struct Item
        {
            public int TypeIndex;      // into s_Types
            public float X, Z;         // world position; ground items always sit at y = 0
            public int Count;
            public float SpawnHours;   // when it was dropped, for perishables
            public short Condition;
            public byte TimesRepaired;
            public byte Yaw;           // facing, quantised to 256 steps — plenty for a dropped object
            public uint Id;            // stable identity, so a handle survives items moving in the list

            public Vector3 Position => new Vector3(X, 0f, Z);
            public float YawDegrees => Yaw * (360f / 256f);
        }

        /// <summary>A chunk's worth of items. Removal swaps the last item down into the gap, so it
        /// costs nothing, which is why items carry an <see cref="Item.Id"/> rather than trusting an
        /// index to stay put.</summary>
        public sealed class Chunk
        {
            public readonly long Key;
            public readonly List<Item> Items = new List<Item>(16);
            public bool Dirty;         // changed since it was last written to the save

            public Chunk(long key) { Key = key; }
        }

        private static readonly Dictionary<long, Chunk> s_Chunks = new Dictionary<long, Chunk>(256);

        // Item types are interned so the per-item record can hold a small integer instead of a string.
        private static readonly List<ItemDef> s_Types = new List<ItemDef>(256);
        private static readonly Dictionary<string, int> s_TypeIndex = new Dictionary<string, int>(256);

        private static uint s_NextId = 1;

        public static int ChunkCount => s_Chunks.Count;

        /// <summary>Total items held, loaded or not. Walks the chunks, so it is for diagnostics.</summary>
        public static int DebugCountAll()
        {
            int n = 0;
            foreach (Chunk c in s_Chunks.Values) n += c.Items.Count;
            return n;
        }

        // ── Chunk keys ────────────────────────────────────────────────────────────────────────────────

        public static long KeyOf(float x, float z)
        {
            int cx = Mathf.FloorToInt(x / ChunkSize);
            int cz = Mathf.FloorToInt(z / ChunkSize);
            return Key(cx, cz);
        }

        public static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

        public static void Coords(long key, out int cx, out int cz)
        {
            cx = (int)(key >> 32);
            cz = (int)(key & 0xFFFFFFFF);
        }

        // ── Item types ────────────────────────────────────────────────────────────────────────────────

        public static int TypeIndexOf(ItemDef def)
        {
            if (def == null) return -1;
            if (s_TypeIndex.TryGetValue(def.Id, out int i)) return i;

            i = s_Types.Count;
            s_Types.Add(def);
            s_TypeIndex[def.Id] = i;
            return i;
        }

        public static ItemDef TypeAt(int index)
            => index >= 0 && index < s_Types.Count ? s_Types[index] : null;

        // ── Adding and removing ───────────────────────────────────────────────────────────────────────

        /// <summary>Drops an item on the ground. Returns its stable id, or 0 if it could not be placed.</summary>
        public static uint Add(ItemDef def, Vector3 position, int count,
            float spawnHours = float.NaN, int condition = -1, int timesRepaired = 0, float yawDegrees = -1f, ItemExtra extra = null)
        {
            if (def == null || count <= 0) return 0;

            int type = TypeIndexOf(def);
            if (yawDegrees < 0f) yawDegrees = Random.Range(0f, 360f);

            var item = new Item
            {
                TypeIndex = type,
                X = position.x,
                Z = position.z,
                Count = count,
                SpawnHours = float.IsNaN(spawnHours) ? (def.Perishable ? TimeOfDay.Now : 0f) : spawnHours,
                Condition = (short)(condition >= 0 ? condition : (def.IsDamageable ? def.MaxCondition : 0)),
                TimesRepaired = (byte)Mathf.Clamp(timesRepaired, 0, 255),
                Yaw = (byte)(Mathf.RoundToInt(Mathf.Repeat(yawDegrees, 360f) * (256f / 360f)) & 0xFF),
                Id = s_NextId++,
            };
            SetExtra(item.Id, extra);

            Chunk chunk = GetOrCreate(KeyOf(position.x, position.z));
            chunk.Items.Add(item);
            chunk.Dirty = true;
            return item.Id;
        }

        // Per-instance extras (found-in-raid, gun mods…) for the few ground items that carry any, kept
        // beside the packed records so the record itself stays small.
        private static readonly Dictionary<uint, ItemExtra> s_Extras = new Dictionary<uint, ItemExtra>();

        /// <summary>The extras of the ground item with this id, or null.</summary>
        public static ItemExtra ExtraOf(uint id) => s_Extras.TryGetValue(id, out ItemExtra e) ? e : null;

        /// <summary>Attaches (or clears, for null / empty) the extras of a ground item.</summary>
        public static void SetExtra(uint id, ItemExtra extra)
        {
            ItemExtra copy = ItemExtra.CopyOf(extra);
            if (copy == null) s_Extras.Remove(id);
            else s_Extras[id] = copy;
        }

        /// <summary>Takes units off a stack. Returns true when the stack emptied and was removed.</summary>
        public static bool Take(in Handle handle, int amount, out ItemDef def, out int taken)
        {
            def = null;
            taken = 0;
            if (!Resolve(handle, out Chunk chunk, out int index)) return false;

            Item item = chunk.Items[index];
            def = TypeAt(item.TypeIndex);
            taken = Mathf.Min(amount, item.Count);
            item.Count -= taken;
            chunk.Dirty = true;

            if (item.Count <= 0)
            {
                RemoveAt(chunk, index);
                return true;
            }

            chunk.Items[index] = item;
            return false;
        }

        public static bool Remove(in Handle handle)
        {
            if (!Resolve(handle, out Chunk chunk, out int index)) return false;
            RemoveAt(chunk, index);
            return true;
        }

        private static void RemoveAt(Chunk chunk, int index)
        {
            int last = chunk.Items.Count - 1;
            s_Extras.Remove(chunk.Items[index].Id);
            chunk.Items[index] = chunk.Items[last];   // swap the tail down; order on the floor is meaningless
            chunk.Items.RemoveAt(last);
            chunk.Dirty = true;

            // Drop the chunk once nothing is left in it. A sandbox world is explored and looted
            // indefinitely, so keeping an empty entry for every square of ground anything was ever
            // dropped on would grow without limit for no benefit.
            if (chunk.Items.Count == 0) s_Chunks.Remove(chunk.Key);
        }

        private static Chunk GetOrCreate(long key)
        {
            if (!s_Chunks.TryGetValue(key, out Chunk c))
            {
                c = new Chunk(key);
                s_Chunks[key] = c;
            }
            return c;
        }

        public static bool TryGetChunk(long key, out Chunk chunk) => s_Chunks.TryGetValue(key, out chunk);

        // ── Handles ───────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A reference to one item on the ground. It carries the item's stable id as well as where it
        /// was last seen, so it still resolves after other items in the same chunk have been picked up
        /// and shuffled the list around.
        /// </summary>
        public readonly struct Handle
        {
            public readonly long ChunkKey;
            public readonly int HintIndex;
            public readonly uint Id;

            public Handle(long chunkKey, int hintIndex, uint id)
            {
                ChunkKey = chunkKey;
                HintIndex = hintIndex;
                Id = id;
            }

            public bool IsValid => Id != 0;
            public static readonly Handle None = new Handle(0, -1, 0);
        }

        private static bool Resolve(in Handle handle, out Chunk chunk, out int index)
        {
            index = -1;
            if (!handle.IsValid || !s_Chunks.TryGetValue(handle.ChunkKey, out chunk)) { chunk = null; return false; }

            // The hint is nearly always right; only a removal since the handle was taken moves things.
            if (handle.HintIndex >= 0 && handle.HintIndex < chunk.Items.Count &&
                chunk.Items[handle.HintIndex].Id == handle.Id)
            {
                index = handle.HintIndex;
                return true;
            }

            for (int i = 0; i < chunk.Items.Count; i++)
            {
                if (chunk.Items[i].Id != handle.Id) continue;
                index = i;
                return true;
            }

            return false;
        }

        public static bool TryGet(in Handle handle, out Item item)
        {
            if (Resolve(handle, out Chunk chunk, out int index)) { item = chunk.Items[index]; return true; }
            item = default;
            return false;
        }

        // ── Queries ───────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The nearest item to <paramref name="position"/> within <paramref name="radius"/>. Only the
        /// chunks the radius actually overlaps are examined, so this costs the same whether the world
        /// holds a hundred items or ten million.
        /// </summary>
        public static bool TryFindNearest(Vector3 position, float radius, out Handle handle, out Item item)
        {
            handle = Handle.None;
            item = default;

            float best = radius * radius;
            bool found = false;

            int minX = Mathf.FloorToInt((position.x - radius) / ChunkSize);
            int maxX = Mathf.FloorToInt((position.x + radius) / ChunkSize);
            int minZ = Mathf.FloorToInt((position.z - radius) / ChunkSize);
            int maxZ = Mathf.FloorToInt((position.z + radius) / ChunkSize);

            for (int cx = minX; cx <= maxX; cx++)
            {
                for (int cz = minZ; cz <= maxZ; cz++)
                {
                    if (!s_Chunks.TryGetValue(Key(cx, cz), out Chunk chunk)) continue;

                    List<Item> items = chunk.Items;
                    for (int i = 0; i < items.Count; i++)
                    {
                        float dx = items[i].X - position.x;
                        float dz = items[i].Z - position.z;
                        float d2 = dx * dx + dz * dz;
                        if (d2 >= best) continue;

                        best = d2;
                        item = items[i];
                        handle = new Handle(chunk.Key, i, items[i].Id);
                        found = true;
                    }
                }
            }

            return found;
        }

        /// <summary>Adds every chunk overlapping the given world-space square to <paramref name="into"/>.
        /// Used by the renderer to gather what is worth drawing.</summary>
        public static void ChunksInRange(Vector3 centre, float radius, List<Chunk> into)
        {
            into.Clear();

            int minX = Mathf.FloorToInt((centre.x - radius) / ChunkSize);
            int maxX = Mathf.FloorToInt((centre.x + radius) / ChunkSize);
            int minZ = Mathf.FloorToInt((centre.z - radius) / ChunkSize);
            int maxZ = Mathf.FloorToInt((centre.z + radius) / ChunkSize);

            for (int cx = minX; cx <= maxX; cx++)
            {
                for (int cz = minZ; cz <= maxZ; cz++)
                {
                    if (s_Chunks.TryGetValue(Key(cx, cz), out Chunk chunk) && chunk.Items.Count > 0) into.Add(chunk);
                }
            }
        }

        /// <summary>All chunks, for saving.</summary>
        public static Dictionary<long, Chunk>.ValueCollection AllChunks => s_Chunks.Values;

        /// <summary>Empties the world of ground items — used when loading a save or starting over.</summary>
        public static void Clear()
        {
            s_Chunks.Clear();
            s_Extras.Clear();
            s_NextId = 1;
        }

        /// <summary>Restores an item exactly as it was saved, keeping its identity.</summary>
        public static void AddRaw(in Item item)
        {
            Chunk chunk = GetOrCreate(KeyOf(item.X, item.Z));
            chunk.Items.Add(item);
            if (item.Id >= s_NextId) s_NextId = item.Id + 1;
        }
    }
}
