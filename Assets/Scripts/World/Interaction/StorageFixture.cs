using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>How a <see cref="StorageFixture"/> is filled and worded.</summary>
    public sealed class StorageOptions
    {
        /// <summary>Lower-case phrase for prompts ("kitchen cupboard"); defaults to the display name in lower case.</summary>
        public string Noun;
        /// <summary>The container's title in the inventory panel; defaults to the display name.</summary>
        public string ContainerName;
        /// <summary>Loot table the contents are rolled from (see <see cref="ItemDatabase.RollLoot"/>).</summary>
        public ContainerType LootType = ContainerType.Shelf;
        /// <summary>Starts empty (a table top, a towel cupboard): nothing is rolled.</summary>
        public bool StartEmpty;
        /// <summary>Odds the fixture starts empty anyway — the starter home is meant to be lean.</summary>
        public float EmptyChance = 0.35f;
        /// <summary>At most this many stacks survive the roll (the rest are dropped).</summary>
        public int MaxStacks = 2;
        /// <summary>Seconds of rummaging before the contents show (negative: the loot type's default; 0: instant).</summary>
        public float SearchTime = -1f;
        /// <summary>Verbs for a fixture with doors / drawers.</summary>
        public string OpenVerb = "Open", CloseVerb = "Close";
        /// <summary>Label for the "show me what's inside" action ("Look inside", "Browse the shelves").</summary>
        public string LookLabel;
        /// <summary>Noise radius of a door/drawer being worked.</summary>
        public float Noise = CloneZConfig.FixtureNoiseSmall;
        /// <summary>Extra actions listed after the storage ones ("Check the temperature").</summary>
        public Action<InteractContext, List<InteractAction>> ExtraActions;
        /// <summary>Called whenever the doors / drawers open (true) or close (false), e.g. to light the fridge.</summary>
        public Action<bool> OnOpenChanged;
        /// <summary>In an extraction raid the town's storage is fuller than the lean wave home's: the odds above are scaled
        /// (<see cref="CloneZConfig.RaidStorageEmptyScale"/>, <see cref="CloneZConfig.RaidStorageExtraStacks"/>). False keeps
        /// them exactly as given — for storage the raid stocks itself (<see cref="RaidBuilding.MainStock"/>).</summary>
        public bool RaidBoost = true;
    }

    /// <summary>
    /// Anything you can keep things in: a cupboard, a drawer, a fridge compartment, the mirror cabinet, a set
    /// of shelves, a table top. It owns a fixture <see cref="LootContainer"/> (listed in the inventory's
    /// container column like any other, and saved with the containers under its
    /// <see cref="LootContainer.FixtureKey"/>) and, optionally, the <see cref="FixturePart"/> doors or
    /// drawers that open to reach it.
    ///
    /// [E] on a closed one opens it and shows (or starts searching) the contents; [E] on an open one closes it;
    /// Q adds "look inside" and any extra actions. Doors also swing open by themselves while the contents are
    /// on screen (opened from the inventory's container column), and stay open until closed.
    /// </summary>
    public class StorageFixture : Interactable
    {
        private string _name;
        private string _key;
        private Vector3 _point;
        private Transform _occlusion;
        private FixturePart[] _parts;
        private StorageOptions _opt;
        private bool _open;

        /// <summary>The storage itself.</summary>
        public LootContainer Container { get; private set; }

        public bool IsOpen => _open;
        public bool HasParts => _parts != null && _parts.Length > 0;
        public IReadOnlyList<FixturePart> Parts => _parts;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _occlusion != null ? _occlusion : transform;
        public override string SaveKey => _key;

        private string Noun => _opt.Noun ?? (_name ?? "cupboard").ToLowerInvariant();

        /// <summary>
        /// Builds a storage fixture on <paramref name="host"/> (the furniture root or wall piece it belongs to).
        /// <paramref name="key"/> must be unique and stable (see <see cref="Interactable.KeyFor"/>);
        /// <paramref name="interactPoint"/> is where the survivor reaches (a door front's middle);
        /// <paramref name="parts"/> are the doors / drawers (may be null or empty for open shelving);
        /// <paramref name="occlusionRoot"/> is the furniture root whose collider shouldn't block the reach
        /// (null for wall-mounted storage). <paramref name="seed"/> rolls the contents deterministically.
        /// </summary>
        public static StorageFixture Create(Transform host, string displayName, string key, Vector3 interactPoint,
            FixturePart[] parts, Transform occlusionRoot, int seed, StorageOptions options = null)
        {
            var go = new GameObject("Storage_" + displayName);
            go.transform.SetParent(host, false);
            go.transform.position = interactPoint;
            go.transform.rotation = Quaternion.identity;
            var f = go.AddComponent<StorageFixture>();
            f._name = displayName;
            f._key = key;
            f._point = interactPoint;
            f._occlusion = occlusionRoot;
            f._parts = parts ?? new FixturePart[0];
            f._opt = options ?? new StorageOptions();
            float emptyChance = f._opt.EmptyChance;
            int maxStacks = f._opt.MaxStacks;
            if (GameSession.IsExtraction && f._opt.RaidBoost && !f._opt.StartEmpty)
            {
                emptyChance *= CloneZConfig.RaidStorageEmptyScale;
                if (maxStacks >= 0 && maxStacks < int.MaxValue - CloneZConfig.RaidStorageExtraStacks) maxStacks += CloneZConfig.RaidStorageExtraStacks;
            }
            f.Container = LootContainer.SpawnFixture(go.transform, interactPoint, f._opt.LootType,
                f._opt.ContainerName ?? displayName, key + "/items", seed, f._opt.StartEmpty, emptyChance,
                maxStacks, f._opt.SearchTime);
            f.Container.Owner = f;
            return f;
        }

        private void Update()
        {
            // Opened from the inventory's container column (or being searched): swing the doors open to match.
            if (_open || !HasParts) return;
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi != null && Container != null && (pi.ViewedContainer == Container || pi.SearchTarget == Container))
                SetOpen(true, false);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            string noun = Noun;
            // Knock action adds noise without opening the storage
            into.Add(new InteractAction("Knock on the " + noun, c => { MakeNoise(_opt.Noise); }));
            bool searched = Container == null || Container.Searched || Container.SearchTime <= 0f;
            string look = _opt.LookLabel ?? (searched ? "Look inside the " + noun : "Search the " + noun);
            if (HasParts)
            {
                if (!_open)
                    into.Add(new InteractAction(_opt.OpenVerb + " the " + noun, c => { SetOpen(true, true); ShowContents(c); }));
                else
                {
                    into.Add(new InteractAction(_opt.CloseVerb + " the " + noun, c => SetOpen(false, true)));
                    into.Add(new InteractAction(look, ShowContents));
                }
            }
            else into.Add(new InteractAction(look, ShowContents));
            _opt.ExtraActions?.Invoke(ctx, into);
        }

        private void ShowContents(InteractContext ctx)
        {
            if (Container == null || ctx.Interactor == null) return;
            if (HasParts && !_open) SetOpen(true, true);
            ctx.Interactor.SearchOrOpen(Container);
        }

        /// <summary>Opens or closes the doors / drawers (closing also puts the inventory panel away if it shows this).</summary>
        public void SetOpen(bool open, bool noisy)
        {
            if (_open == open) return;
            _open = open;
            for (int i = 0; i < _parts.Length; i++)
                if (_parts[i] != null) _parts[i].SetOpen(open);
            if (noisy) MakeNoise(_opt.Noise);
            if (!open)
            {
                PlayerInteractor pi = PlayerInteractor.Current;
                if (pi != null && Container != null && pi.ViewedContainer == Container) pi.CloseInventory();
            }
            _opt.OnOpenChanged?.Invoke(open);
        }

        public override void CaptureState(FixtureSave save) => save.State = _open ? 1 : 0;

        public override void RestoreState(FixtureSave save)
        {
            bool open = (save.State & 1) != 0;
            _open = open;
            for (int i = 0; i < _parts.Length; i++)
                if (_parts[i] != null) _parts[i].SetOpen(open, true);
            _opt.OnOpenChanged?.Invoke(open);
        }
    }

    /// <summary>What's kept in the fixtures of a piece of furniture or a wall (its cupboards, drawers, cabinets).</summary>
    public static class FixtureContents
    {
        private static readonly List<LootContainer> s_Found = new List<LootContainer>(8);

        /// <summary>The first fixture storage under <paramref name="root"/> that holds something, or null.</summary>
        public static LootContainer FirstHolding(Transform root)
        {
            if (root == null) return null;
            root.GetComponentsInChildren(true, s_Found);
            LootContainer found = null;
            for (int i = 0; i < s_Found.Count; i++)
                if (s_Found[i] != null && s_Found[i].IsFixture && !s_Found[i].IsEmpty) { found = s_Found[i]; break; }
            s_Found.Clear();
            return found;
        }

        /// <summary>Drops everything kept in the fixtures under <paramref name="root"/> onto the floor beneath them
        /// (a wall battered down with its cupboards full).</summary>
        public static void Spill(Transform root)
        {
            if (root == null) return;
            root.GetComponentsInChildren(true, s_Found);
            for (int i = 0; i < s_Found.Count; i++)
            {
                LootContainer c = s_Found[i];
                if (c == null || !c.IsFixture || c.IsEmpty) continue;
                Vector3 at = c.transform.position;
                List<ItemStack> stacks = c.Contents.Stacks;
                for (int s = 0; s < stacks.Count; s++)
                {
                    ItemStack st = stacks[s];
                    if (st == null || st.Def == null) continue;
                    Vector2 jitter = UnityEngine.Random.insideUnitCircle * 0.35f;
                    WorldItem.Spawn(null, at + new Vector3(jitter.x, 0f, jitter.y), st.Def, st.Count,
                        st.Def.Perishable ? st.SpawnHours : float.NaN,
                        st.Def.IsDamageable ? st.Condition : -1,
                        st.TimesRepaired, st.Extra);
                }
                c.Contents.Clear();
            }
            s_Found.Clear();
        }
    }
}
