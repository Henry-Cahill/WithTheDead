using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A handle to one item lying on the ground.
    ///
    /// This used to be a <see cref="MonoBehaviour"/>, one per dropped item, holding a few hundred
    /// child renderers and running an <c>Update</c> every frame. A sandbox world accumulates items
    /// without limit, so that arrangement had a ceiling of a few thousand — and registering each spawn
    /// walked the whole list, which made merely filling the world quadratic.
    ///
    /// The items themselves now live as packed records in <see cref="GroundItemStore"/> and are drawn
    /// instanced by <see cref="GroundItemRenderer"/>. Nothing is allocated per item in the world. A
    /// handle like this is only created for the very few items something is actually doing business
    /// with — the one the player is standing over, or the short list shown in the inventory's floor
    /// panel — so the cost is bounded by what is on screen rather than by the size of the world.
    /// </summary>
    public sealed class WorldItem
    {
        public GroundItemStore.Handle Handle { get; private set; }
        public ItemDef Def { get; private set; }
        public int Count { get; private set; }
        public float SpawnHours { get; private set; }
        public int Condition { get; private set; }
        public int TimesRepaired { get; private set; }
        public Vector3 Position { get; private set; }

        private WorldItem() { }

        private static WorldItem From(in GroundItemStore.Handle handle, in GroundItemStore.Item item)
        {
            ItemDef def = GroundItemStore.TypeAt(item.TypeIndex);
            if (def == null) return null;

            return new WorldItem
            {
                Handle = handle,
                Def = def,
                Count = item.Count,
                SpawnHours = item.SpawnHours,
                Condition = item.Condition,
                TimesRepaired = item.TimesRepaired,
                Position = item.Position,
            };
        }

        /// <summary>Text for the pickup prompt, e.g. "Apple \u00d73".</summary>
        public string PickupLabel =>
            Def == null ? "item" : (Count > 1 ? Def.DisplayName + " \u00d7" + Count : Def.DisplayName);

        /// <summary>Drops an item onto the ground. The <paramref name="parent"/> is accepted and ignored
        /// — ground items are no longer scene objects — so existing callers need no change.</summary>
        public static WorldItem Spawn(Transform parent, Vector3 position, ItemDef def, int count,
            float spawnHours = float.NaN, int condition = -1, int timesRepaired = 0)
        {
            if (def == null || count <= 0) return null;

            position.y = 0f;
            uint id = GroundItemStore.Add(def, position, count, spawnHours, condition, timesRepaired);
            if (id == 0) return null;

            long key = GroundItemStore.KeyOf(position.x, position.z);
            var handle = new GroundItemStore.Handle(key, -1, id);
            return GroundItemStore.TryGet(handle, out GroundItemStore.Item item) ? From(handle, item) : null;
        }

        /// <summary>The nearest ground item within <paramref name="radius"/>, or null. Only the chunks
        /// the radius touches are examined, so this does not care how large the world has grown.</summary>
        public static WorldItem Nearest(Vector3 position, float radius)
        {
            return GroundItemStore.TryFindNearest(position, radius, out GroundItemStore.Handle h, out GroundItemStore.Item it)
                ? From(h, it)
                : null;
        }

        /// <summary>Every ground item within <paramref name="radius"/>, for the inventory's floor panel.</summary>
        public static void Near(Vector3 position, float radius, List<WorldItem> into)
        {
            into.Clear();

            GroundItemStore.ChunksInRange(position, radius, s_ChunkScratch);
            float r2 = radius * radius;

            for (int c = 0; c < s_ChunkScratch.Count; c++)
            {
                GroundItemStore.Chunk chunk = s_ChunkScratch[c];
                for (int i = 0; i < chunk.Items.Count; i++)
                {
                    GroundItemStore.Item item = chunk.Items[i];
                    float dx = item.X - position.x;
                    float dz = item.Z - position.z;
                    if (dx * dx + dz * dz > r2) continue;

                    WorldItem w = From(new GroundItemStore.Handle(chunk.Key, i, item.Id), item);
                    if (w != null) into.Add(w);
                }
            }
        }

        // Reused so gathering the floor list does not allocate on every inventory refresh.
        private static readonly List<GroundItemStore.Chunk> s_ChunkScratch = new List<GroundItemStore.Chunk>(9);

        /// <summary>Removes <paramref name="amount"/> units; the pile disappears once empty.
        /// Returns true if the pile was emptied.</summary>
        public bool Take(int amount)
        {
            bool emptied = GroundItemStore.Take(Handle, amount, out _, out int taken);
            Count -= taken;
            return emptied;
        }

        public void Consume() => GroundItemStore.Remove(Handle);

        /// <summary>True while the item is still on the ground and has not been picked up by something
        /// else since this handle was taken.</summary>
        public bool StillThere => GroundItemStore.TryGet(Handle, out _);
    }
}
