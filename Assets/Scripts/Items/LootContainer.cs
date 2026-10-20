using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    public enum ContainerType { Crate, Locker, Fridge, MedCabinet, Desk, ToolBench, Wardrobe, Pantry, GunLocker, Shelf, Corpse }

    /// <summary>
    /// A searchable world container. It registers itself in a static list so the
    /// player's interactor can find the nearest one by proximity. Contents are
    /// rolled from a loot table when the container is spawned.
    /// </summary>
    public class LootContainer : MonoBehaviour, IDemolishable
    {
        /// <summary>Global list of all active loot containers in the world.</summary>
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

        /// <summary>Save "kind" value for a dropped ground item (world containers use their ContainerType
        /// int; kept clear of every ContainerType value so the two never collide in a save).</summary>
        public const int KindSack = 20;

        /// <summary>The fixture this is the storage of (a cupboard, a drawer, the fridge), or null for a
        /// free-standing container. A fixture's storage has no body of its own: the fixture handles [E], it
        /// can't be dismantled apart from its furniture, and it's saved under <see cref="FixtureKey"/>.</summary>
        public Interactable Owner;

        /// <summary>Stable save id of a fixture's storage (null for ordinary containers). A loaded save
        /// pours its contents back into the live fixture with the same key instead of spawning a box.</summary>
        public string FixtureKey;

        /// <summary>True for the storage of a fixture (see <see cref="Owner"/>).</summary>
        public bool IsFixture => FixtureKey != null || Owner != null;

        /// <summary>Indicates whether the container has no items (null contents or empty stack list).</summary>
        public bool IsEmpty => Contents == null || Contents.Stacks.Count == 0;

        /// <summary>Arranges the contents on a growable grid (once) for the grid inventory view, sized to
        /// suit the container — a crate is wide, a medicine cabinet narrow. Returns the layout.</summary>
        public InventoryLayout EnsureLayout()
        {
            if (Contents == null) return null;
            if (Contents.Layout == null)
                Contents.SetLayout(InventoryLayout.Growable(DisplayName, GridWidthFor(Type, IsSack), GridRowsFor(Type, IsSack)));
            return Contents.Layout;
        }

        /// <summary>Columns of a container's grid in the grid inventory view.</summary>
        public static int GridWidthFor(ContainerType type, bool sack = false)
        {
            if (sack) return InventoryLayout.MaxFootprint;
            switch (type)
            {
                case ContainerType.Crate:
                case ContainerType.ToolBench:
                case ContainerType.Shelf: return 8;
                case ContainerType.Wardrobe: return 7;
                default: return InventoryLayout.MaxFootprint;
            }
        }

        /// <summary>Rows a container's grid shows at minimum (it grows to fit whatever it holds).</summary>
        public static int GridRowsFor(ContainerType type, bool sack = false)
        {
            if (sack) return 2;
            switch (type)
            {
                case ContainerType.Crate:
                case ContainerType.Wardrobe:
                case ContainerType.Fridge:
                case ContainerType.GunLocker:
                case ContainerType.Locker: return 6;
                case ContainerType.Corpse: return 5;
                default: return 4;
            }
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region IDemolishable
        /// <summary>Name used when displaying this container as a demolishable object.</summary>
        public string DemolishName => "container";
        public bool CanDemolish(out string reason)
        {
            if (IsFixture) { reason = "It's part of the " + (Owner != null ? Owner.DisplayName.ToLowerInvariant() : "furniture"); return false; }
            if (IsSack) { reason = "Pick the sack up instead"; return false; }
            if (Type == ContainerType.Corpse) { reason = "Search the body, don't dismantle it"; return false; }
            if (!IsEmpty) { reason = "Empty it before dismantling"; return false; }
            reason = null; return true;
        }
        public void GetSalvage(out int planks, out int nails) { planks = CloneZConfig.SalvageContainerPlanks; nails = CloneZConfig.SalvageContainerNails; }
        public bool CarvesNavMesh => !IsSack && !IsFixture && Type != ContainerType.Corpse;   // world containers carry colliders baked into the navmesh
        public void Dismantle()
        {
            All.Remove(this);
            gameObject.SetActive(false);   // drop out of the navmesh bake before the rebuild runs
            Destroy(gameObject);
        }
        #endregion

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
            // Compute floor Y for potential handle placement
            var floor = position;
            floor.y = BuildPiece.FloorTopAt(floor);

            // Deterministic random to optionally add a small metal front‑handle
            var rng = new System.Random(1994);
            if (rng.NextDouble() < 0.7)
            {
                var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                handle.name = "Handle";
                handle.transform.SetParent(go.transform, false);
                // Position at the front centre of the container
                handle.transform.localPosition = new Vector3(0f, size.y * 0.5f, size.z * 0.5f + 0.02f);
                handle.transform.localScale = new Vector3(0.04f, 0.02f, 0.04f);
                // Use the same material as the container box
                var rend = handle.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = go.GetComponent<Renderer>().sharedMaterial;
            }
            // Add a small metal knob to fridges with 50% chance
            if (type == ContainerType.Fridge && rng.NextDouble() < 0.5)
            {
                var knob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                knob.name = "Knob";
                knob.transform.SetParent(go.transform, false);
                knob.transform.localPosition = new Vector3(0f, size.y * 0.32f, size.z * 0.5f + 0.018f);
                knob.transform.localScale = new Vector3(0.06f, 0.02f, 0.06f);
                var rendKnob = knob.GetComponent<Renderer>();
                if (rendKnob != null) rendKnob.sharedMaterial = go.GetComponent<Renderer>().sharedMaterial;
            }
            var c = go.AddComponent<LootContainer>();
            c.DisplayName = name;
            c.Type = type;
            c.Contents = new Inventory(999f);
            c.SearchTime = SearchTimeFor(type);
            ItemDatabase.RollLoot(type, new System.Random(seed), c.Contents);
            if (type == ContainerType.Crate && StarterHome.Active) DressCrate(go);
            return c;
        }

        /// <summary>The storage of a fixture (see <see cref="StorageFixture"/>): a bodiless container on a child of
        /// <paramref name="parent"/> at <paramref name="position"/>, titled <paramref name="displayName"/>, filled
        /// from <paramref name="lootType"/>'s table — unless <paramref name="startEmpty"/>, or it comes up empty on
        /// <paramref name="emptyChance"/> — and trimmed to <paramref name="maxStacks"/> stacks so the home stays lean.
        /// A negative <paramref name="searchTime"/> takes the loot type's usual rummaging time.</summary>
        public static LootContainer SpawnFixture(Transform parent, Vector3 position, ContainerType lootType, string displayName,
            string fixtureKey, int seed, bool startEmpty = false, float emptyChance = 0f, int maxStacks = int.MaxValue, float searchTime = -1f)
        {
            var go = new GameObject("Storage");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var c = go.AddComponent<LootContainer>();
            c.DisplayName = displayName;
            c.Type = lootType == ContainerType.Corpse ? ContainerType.Shelf : lootType;
            c.FixtureKey = fixtureKey;
            c.Contents = new Inventory(999f);
            c.SearchTime = searchTime >= 0f ? searchTime : SearchTimeFor(c.Type);
            var rng = new System.Random(seed);
            if (!startEmpty && rng.NextDouble() >= emptyChance)
            {
                ItemDatabase.RollLoot(c.Type, rng, c.Contents);
                List<ItemStack> stacks = c.Contents.Stacks;
                if (maxStacks >= 0 && stacks.Count > maxStacks) stacks.RemoveRange(maxStacks, stacks.Count - maxStacks);
            }
            if (c.SearchTime <= 0f || (startEmpty && c.IsEmpty)) c.Searched = true;
            return c;
        }

        /// <summary>Replaces the contents with a saved game's (a fixture's storage, matched by <see cref="FixtureKey"/>).</summary>
        public void RestoreContents(bool searched, List<StackSave> items)
        {
            Contents = new Inventory(999f);
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    StackSave st = items[i];
                    ItemDef def = ItemDatabase.Get(st.Id);
                    if (def != null)
                        Contents.Add(def, st.Count, enforceWeight: false,
                            spawnHours: def.Perishable ? st.SpawnHours : float.NaN,
                            condition: def.RestoredCondition(st.Condition),
                            timesRepaired: st.TimesRepaired, extra: ItemExtra.Parse(st.Extra));
                }
            }
            Searched = searched;
        }

        /// <summary>Size of a dressed storage crate (see <see cref="DressCrate"/>).</summary>
        private static readonly Vector3 CrateSize = new Vector3(1.0f, 0.66f, 0.7f);

        // In the wave-survival world a plain crate (the starter home's supply crate) becomes a slatted pine
        // storage crate standing on the floor, instead of a bare box. Only the root's own mesh and collider
        // change. (The raid town dresses its containers as furniture itself.)
        private static void DressCrate(GameObject go)
        {
            Vector3 floor = go.transform.position;
            floor.y = BuildPiece.FloorTopAt(floor);
            go.transform.localScale = Vector3.one;
            go.transform.position = floor + Vector3.up * (CrateSize.y * 0.5f);
            var box = go.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = Vector3.zero;
                box.size = CrateSize;
            }
            var m = new RaidMesh();
            // Base crate geometry
            FurnitureBuilder.Crate(m, new System.Random(1993), floor, CrateSize, 0f, false);
            // Slatted front panel
            m.Box(floor + new Vector3(0.22f, CrateSize.y * 0.62f, CrateSize.z * 0.5f + 0.004f), new Vector3(0.26f, 0.16f, 0.004f), DecorPalette.Matte(FurnitureBuilder.Paper));
            // Add a small metal latch on the front with deterministic randomness
            var latchRng = new System.Random(1993);
            if (latchRng.NextDouble() < 0.5)
            {
                m.Box(floor + new Vector3(0.45f, CrateSize.y * 0.30f, CrateSize.z * 0.5f + 0.006f), new Vector3(0.08f, 0.06f, 0.02f), DecorPalette.Matte(new Color(0.60f, 0.60f, 0.60f)));
            }
            m.ApplyTo(go, go.transform.position);
            DecorPalette.Apply();
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
        /// A dropped damageable item keeps its <paramref name="condition"/> and repair history so wear
        /// survives the round-trip.</summary>
        public static LootContainer SpawnBag(Vector3 position, ItemDef def, int count, int condition = -1, int timesRepaired = 0)
        {
            GameObject go = CloneZUtil.Box("Sack", position, new Vector3(0.5f, 0.4f, 0.5f), new Color(0.45f, 0.38f, 0.28f));
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);   // a pure pickup shouldn't nudge the player around

            var c = go.AddComponent<LootContainer>();
            c.DisplayName = "Sack";
            c.IsSack = true;
            c.Contents = new Inventory(999f);
            c.Contents.Add(def, count, enforceWeight: false, condition: condition, timesRepaired: timesRepaired);
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
                var type = (ContainerType)Mathf.Clamp(kind, 0, (int)ContainerType.Shelf);
                Style(type, out Vector3 size, out Color color, out string name);
                position.y = size.y * 0.5f;
                GameObject go = CloneZUtil.Box(name, position, size, color, parent);
                c = go.AddComponent<LootContainer>();
                c.DisplayName = name;
                c.Type = type;
                c.SearchTime = SearchTimeFor(type);
                if (type == ContainerType.Crate && StarterHome.Active) DressCrate(go);
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
                            condition: def.RestoredCondition(st.Condition),
                            timesRepaired: st.TimesRepaired, extra: ItemExtra.Parse(st.Extra));
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
                case ContainerType.Wardrobe:
                    size = new Vector3(1.0f, 2.0f, 0.6f); color = new Color(0.50f, 0.36f, 0.24f); name = "Wardrobe"; break;
                case ContainerType.Pantry:
                    size = new Vector3(1.0f, 1.7f, 0.6f); color = new Color(0.62f, 0.50f, 0.36f); name = "Pantry"; break;
                case ContainerType.GunLocker:
                    size = new Vector3(0.8f, 1.9f, 0.6f); color = new Color(0.24f, 0.26f, 0.30f); name = "Gun Locker"; break;
                case ContainerType.Shelf:
                    size = new Vector3(1.4f, 1.8f, 0.5f); color = new Color(0.50f, 0.46f, 0.40f); name = "Shelf"; break;
                default:
                    size = new Vector3(1.1f, 1.0f, 1.1f); color = new Color(0.55f, 0.42f, 0.28f); name = "Crate"; break;
            }
        }
    }
}
