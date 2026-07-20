using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    public enum ContainerType { Crate, Locker, Fridge, MedCabinet, Desk, ToolBench, Corpse }

    /// <summary>
    /// A searchable world container. It registers itself in a static list so the
    /// player's interactor can find the nearest one by proximity. Contents are
    /// rolled from a loot table when the container is spawned.
    /// </summary>
    public class LootContainer : MonoBehaviour
    {
        public static readonly List<LootContainer> All = new List<LootContainer>();

        public string DisplayName = "Container";
        public Inventory Contents;
        public bool Searched;
        public float SearchTime;          // seconds to rummage before contents reveal (0 = instant)
        public float Lifetime;            // >0: despawns after this many seconds (corpses)

        /// <summary>The loot-table type this container was built from (Corpse for corpse markers).</summary>
        public ContainerType Type = ContainerType.Crate;
        /// <summary>True for a dropped sack — a collider-less pickup rather than a world container.</summary>
        public bool IsSack;

        /// <summary>Save "kind" value for a dropped sack (world containers use their ContainerType int).</summary>
        public const int KindSack = 7;

        public bool IsEmpty => Contents == null || Contents.Stacks.Count == 0;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        private void Update()
        {
            if (Lifetime > 0f)
            {
                Lifetime -= Time.deltaTime;
                if (Lifetime <= 0f) Destroy(gameObject);
            }
        }

        /// <summary>Builds a container box in the world and fills it from its loot table.</summary>
        public static LootContainer Spawn(Transform parent, ContainerType type, Vector3 position, int seed)
        {
            Style(type, out Vector3 size, out Color color, out string name);
            position.y = size.y * 0.5f;   // rest the box on the ground

            GameObject go = CloneZUtil.Box(name, position, size, color, parent);
            var c = go.AddComponent<LootContainer>();
            c.DisplayName = name;
            c.Type = type;
            c.Contents = new Inventory(999f);
            c.SearchTime = SearchTimeFor(type);
            ItemDatabase.RollLoot(type, new System.Random(seed), c.Contents);
            return c;
        }

        /// <summary>Rolls corpse loot and, only if the body is carrying something, drops a searchable
        /// corpse marker on the ground. Returns null when the corpse yields nothing (keeps the world tidy).</summary>
        public static LootContainer SpawnCorpse(Vector3 position, int seed)
        {
            var rng = new System.Random(seed);
            var contents = new Inventory(999f);
            ItemDatabase.RollLoot(ContainerType.Corpse, rng, contents);
            if (contents.Stacks.Count == 0) return null;   // most bodies carry nothing worth taking

            position.y = 0.12f;
            GameObject go = CloneZUtil.Box("Corpse", position, new Vector3(1.5f, 0.24f, 0.7f), new Color(0.26f, 0.24f, 0.22f));
            go.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);   // a walkable, lootable body

            var c = go.AddComponent<LootContainer>();
            c.DisplayName = "Corpse";
            c.Type = ContainerType.Corpse;
            c.Contents = contents;
            c.SearchTime = CloneZConfig.SearchTimeCorpse;
            c.Lifetime = CloneZConfig.CorpseLifetime;
            return c;
        }

        private static float SearchTimeFor(ContainerType type)
        {
            switch (type)
            {
                case ContainerType.Desk:
                case ContainerType.MedCabinet: return CloneZConfig.SearchTimeSmall;
                case ContainerType.ToolBench: return CloneZConfig.SearchTimeLarge;
                case ContainerType.Corpse: return CloneZConfig.SearchTimeCorpse;
                default: return CloneZConfig.SearchTimeMedium;
            }
        }

        /// <summary>Drops a small sack holding a single stack (used when the player drops items).
        /// A dropped damageable item keeps its <paramref name="condition"/> so wear survives the round-trip.</summary>
        public static LootContainer SpawnBag(Vector3 position, ItemDef def, int count, int condition = -1)
        {
            GameObject go = CloneZUtil.Box("Sack", position, new Vector3(0.5f, 0.4f, 0.5f), new Color(0.45f, 0.38f, 0.28f));
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);   // a pure pickup shouldn't nudge the player around

            var c = go.AddComponent<LootContainer>();
            c.DisplayName = "Sack";
            c.IsSack = true;
            c.Contents = new Inventory(999f);
            c.Contents.Add(def, count, enforceWeight: false, condition: condition);
            c.SearchTime = 0f;      // your own dropped goods — grab them instantly
            c.Searched = true;
            return c;
        }

        /// <summary>Rebuilds a container from a saved game with explicit contents (no random roll).
        /// <paramref name="kind"/> is 0..5 for the world container types, 6 for a corpse and
        /// <see cref="KindSack"/> (7) for a dropped sack.</summary>
        public static LootContainer SpawnSaved(Transform parent, int kind, Vector3 position, float yaw, bool searched, float lifetime, List<StackSave> items)
        {
            LootContainer c;
            if (kind == KindSack)
            {
                GameObject go = CloneZUtil.Box("Sack", position, new Vector3(0.5f, 0.4f, 0.5f), new Color(0.45f, 0.38f, 0.28f), parent);
                StripCollider(go);
                c = go.AddComponent<LootContainer>();
                c.DisplayName = "Sack";
                c.IsSack = true;
                c.Type = ContainerType.Corpse;
                c.SearchTime = 0f;
            }
            else if (kind == (int)ContainerType.Corpse)
            {
                position.y = 0.12f;
                GameObject go = CloneZUtil.Box("Corpse", position, new Vector3(1.5f, 0.24f, 0.7f), new Color(0.26f, 0.24f, 0.22f), parent);
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                StripCollider(go);
                c = go.AddComponent<LootContainer>();
                c.DisplayName = "Corpse";
                c.Type = ContainerType.Corpse;
                c.SearchTime = CloneZConfig.SearchTimeCorpse;
                c.Lifetime = lifetime > 0f ? lifetime : CloneZConfig.CorpseLifetime;
            }
            else
            {
                var type = (ContainerType)Mathf.Clamp(kind, 0, (int)ContainerType.ToolBench);
                Style(type, out Vector3 size, out Color color, out string name);
                position.y = size.y * 0.5f;
                GameObject go = CloneZUtil.Box(name, position, size, color, parent);
                c = go.AddComponent<LootContainer>();
                c.DisplayName = name;
                c.Type = type;
                c.SearchTime = SearchTimeFor(type);
            }

            c.Contents = new Inventory(999f);
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    StackSave st = items[i];
                    ItemDef def = ItemDatabase.Get(st.Id);
                    if (def != null)
                        c.Contents.Add(def, st.Count, enforceWeight: false,
                            spawnHours: def.Perishable ? st.SpawnHours : float.NaN,
                            condition: def.IsDamageable && st.Condition > 0 ? st.Condition : -1);
                }
            }
            c.Searched = searched;
            return c;
        }

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }

        private static void Style(ContainerType type, out Vector3 size, out Color color, out string name)
        {
            switch (type)
            {
                case ContainerType.Locker:
                    size = new Vector3(0.9f, 1.9f, 0.7f); color = new Color(0.36f, 0.42f, 0.48f); name = "Locker"; break;
                case ContainerType.Fridge:
                    size = new Vector3(1.0f, 1.8f, 0.9f); color = new Color(0.85f, 0.86f, 0.88f); name = "Fridge"; break;
                case ContainerType.MedCabinet:
                    size = new Vector3(0.9f, 1.0f, 0.4f); color = new Color(0.92f, 0.92f, 0.94f); name = "Medicine Cabinet"; break;
                case ContainerType.Desk:
                    size = new Vector3(1.6f, 0.9f, 0.9f); color = new Color(0.5f, 0.38f, 0.26f); name = "Desk"; break;
                case ContainerType.ToolBench:
                    size = new Vector3(1.8f, 1.0f, 0.8f); color = new Color(0.32f, 0.3f, 0.33f); name = "Tool Bench"; break;
                default:
                    size = new Vector3(1.1f, 1.0f, 1.1f); color = new Color(0.55f, 0.42f, 0.28f); name = "Crate"; break;
            }
        }
    }
}
