using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// One rectangular grid of cells inside an <see cref="Inventory"/>: the survivor's pockets, a
    /// worn backpack / chest rig / thigh bag, or a world container. Items occupy a footprint of cells
    /// (<see cref="InventoryLayout.Footprint(ItemDef, bool)"/>) and a section may cap the weight it
    /// carries. World containers and the floor are <see cref="Growable"/>: they add rows as needed
    /// and never refuse an item for want of space.
    /// </summary>
    public sealed class GridSection
    {
        /// <summary>Header text, e.g. "Clone Z's Inventory" or "Chest Rig - Tactical".</summary>
        public string Name;
        /// <summary>The worn item that provides this section (null for the pockets or a world container).</summary>
        public readonly ItemDef Source;
        /// <summary>The worn slot it comes from (meaningful only when <see cref="Source"/> is set).</summary>
        public readonly ClothingSlot Slot;
        public int Width;
        /// <summary>Rows. For a <see cref="Growable"/> section this is only the minimum shown.</summary>
        public int Height;
        /// <summary>Kilograms this section can carry; zero or less means unlimited.</summary>
        public float Capacity;
        public readonly bool Growable;

        public GridSection(string name, int width, int height, float capacity, bool growable = false,
            ItemDef source = null, ClothingSlot slot = ClothingSlot.Head)
        {
            Name = name;
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            Capacity = capacity;
            Growable = growable;
            Source = source;
            Slot = slot;
        }

        /// <summary>True for a worn container's section (backpack, rig, thigh bag).</summary>
        public bool IsWorn => Source != null;
        public bool Unlimited => Capacity <= 0f;
    }

    /// <summary>How "Tidy up" orders a grid before repacking it.</summary>
    public enum GridSortMode { Size, Category, Name, Weight }

    /// <summary>
    /// The spatial side of an <see cref="Inventory"/> — Inventory-Tetris style. Every stack sits at a
    /// cell of one <see cref="GridSection"/> (its <see cref="ItemStack.Section"/> / GridX / GridY /
    /// Rotated); stacks never overlap and stay inside their section. <see cref="Inventory.Add"/>
    /// consults the layout so a new stack needs a free spot (and, when weight is enforced, a section
    /// with weight to spare). Placements are runtime-only: a loaded inventory is simply packed again.
    ///
    /// Mutations raise the owner's <see cref="Inventory.Changed"/> so UIs refresh.
    /// </summary>
    public sealed class InventoryLayout
    {
        private const float WeightEpsilon = 1e-4f;

        /// <summary>No item is larger than this many cells along either side.</summary>
        public const int MaxFootprint = 6;

        public readonly List<GridSection> Sections = new List<GridSection>();

        /// <summary>The inventory this layout arranges (set by <see cref="Inventory.SetLayout"/>).</summary>
        public Inventory Owner { get; internal set; }

        public InventoryLayout() { }

        public InventoryLayout(GridSection single)
        {
            if (single != null) Sections.Add(single);
        }

        /// <summary>A single growable section — the shape of a world container or the floor.</summary>
        public static InventoryLayout Growable(string name, int width, int minRows)
            => new InventoryLayout(new GridSection(name, Mathf.Max(MaxFootprint, width), minRows, 0f, growable: true));

        #region Footprints
        /// <summary>
        /// The cells an item covers when not rotated (x = columns, y = rows). Explicit sizes from the
        /// catalogue win; otherwise it is derived from what the item is: long guns and two-handed
        /// weapons are long, garments fold to 2×2, small consumables take a single cell.
        /// </summary>
        public static Vector2Int BaseFootprint(ItemDef def)
        {
            if (def == null) return Vector2Int.one;
            if (def.SizeW > 0 && def.SizeH > 0) return Clamp(def.SizeW, def.SizeH);

            if (def.IsFirearm)
                return def.TwoHanded || def.Weight >= 2.5f ? new Vector2Int(4, 2) : new Vector2Int(2, 1);

            if (def.IsWeapon)
            {
                if (def.TwoHanded || def.WeaponRange >= 2.6f) return new Vector2Int(4, 1);
                if (def.WeaponRange >= 2.1f) return new Vector2Int(3, 1);
                if (def.WeaponRange >= 1.6f || def.Weight >= 0.7f) return new Vector2Int(2, 1);
                return Vector2Int.one;
            }

            if (def.IsClothing)
            {
                switch (def.ClothingSlot)
                {
                    case ClothingSlot.Torso:
                    case ClothingSlot.Legs:
                    case ClothingSlot.Mask:
                    case ClothingSlot.Pouch:
                    case ClothingSlot.Rig: return new Vector2Int(2, 2);
                    case ClothingSlot.Feet:
                    case ClothingSlot.Eyes: return new Vector2Int(2, 1);
                    case ClothingSlot.Armor:
                    case ClothingSlot.Back: return new Vector2Int(3, 3);
                    case ClothingSlot.Thigh: return new Vector2Int(1, 2);
                    default: return Vector2Int.one;   // hats, gloves
                }
            }

            if (def.Category == ItemCategory.Material && def.Weight >= 1.2f) return new Vector2Int(3, 1);   // planks, poles
            if (def.Weight >= 3f) return new Vector2Int(2, 2);
            if (def.Weight >= 1.2f) return new Vector2Int(2, 1);
            return Vector2Int.one;
        }

        /// <summary>The cells an item covers in the given orientation (rotation swaps the sides).</summary>
        public static Vector2Int Footprint(ItemDef def, bool rotated)
        {
            Vector2Int f = BaseFootprint(def);
            return rotated ? new Vector2Int(f.y, f.x) : f;
        }

        /// <summary>The cells a placed stack covers.</summary>
        public static Vector2Int Footprint(ItemStack stack) =>
            stack == null ? Vector2Int.one : Footprint(stack.Def, stack.Rotated);

        private static Vector2Int Clamp(int w, int h) =>
            new Vector2Int(Mathf.Clamp(w, 1, MaxFootprint), Mathf.Clamp(h, 1, MaxFootprint));
        #endregion

        #region Queries
        /// <summary>Kilograms currently carried in a section.</summary>
        public float WeightIn(GridSection s)
        {
            if (Owner == null || s == null) return 0f;
            float w = 0f;
            List<ItemStack> stacks = Owner.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (ReferenceEquals(stacks[i].Section, s)) w += stacks[i].Weight;
            return w;
        }

        /// <summary>The number of rows actually occupied in a section (the extent of its lowest item).</summary>
        public int RowsUsed(GridSection s)
        {
            if (Owner == null || s == null) return 0;
            int rows = 0;
            List<ItemStack> stacks = Owner.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack st = stacks[i];
                if (!ReferenceEquals(st.Section, s)) continue;
                rows = Mathf.Max(rows, st.GridY + Footprint(st).y);
            }
            return rows;
        }

        /// <summary>Rows a UI should draw: the fixed height, or for a growable section enough to show
        /// every item plus one spare row to drop into.</summary>
        public int RowsShown(GridSection s)
        {
            if (s == null) return 0;
            return s.Growable ? Mathf.Max(s.Height, RowsUsed(s) + 1) : s.Height;
        }

        /// <summary>The stack covering a cell, or null.</summary>
        public ItemStack StackAt(GridSection s, int x, int y)
        {
            if (Owner == null || s == null) return null;
            List<ItemStack> stacks = Owner.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack st = stacks[i];
                if (!ReferenceEquals(st.Section, s)) continue;
                Vector2Int f = Footprint(st);
                if (x >= st.GridX && x < st.GridX + f.x && y >= st.GridY && y < st.GridY + f.y) return st;
            }
            return null;
        }

        public bool InBounds(GridSection s, int x, int y, int w, int h)
        {
            if (s == null || x < 0 || y < 0 || x + w > s.Width) return false;
            return s.Growable || y + h <= s.Height;
        }

        /// <summary>True when no stack (other than <paramref name="ignore"/>) covers any cell of the area.</summary>
        public bool IsAreaFree(GridSection s, int x, int y, int w, int h, ItemStack ignore = null)
        {
            if (Owner == null || s == null) return false;
            List<ItemStack> stacks = Owner.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack st = stacks[i];
                if (st == ignore || !ReferenceEquals(st.Section, s)) continue;
                Vector2Int f = Footprint(st);
                if (x < st.GridX + f.x && st.GridX < x + w && y < st.GridY + f.y && st.GridY < y + h) return false;
            }
            return true;
        }

        /// <summary>True when the section can take <paramref name="addKg"/> more (a stack already in the
        /// section that is only being moved within it, <paramref name="moving"/>, doesn't count twice).</summary>
        public bool HasWeightRoom(GridSection s, float addKg, ItemStack moving = null)
        {
            if (s == null) return false;
            if (s.Unlimited) return true;
            float w = WeightIn(s);
            if (moving != null && ReferenceEquals(moving.Section, s)) w -= moving.Weight;
            return w + addKg <= s.Capacity + WeightEpsilon;
        }

        /// <summary>Would <paramref name="stack"/> (already in <see cref="Owner"/>) fit at this spot?
        /// Its own current cells don't block it.</summary>
        public bool Fits(ItemStack stack, GridSection s, int x, int y, bool rotated, bool checkWeight)
        {
            if (stack == null || !Sections.Contains(s)) return false;
            Vector2Int f = Footprint(stack.Def, rotated);
            if (!InBounds(s, x, y, f.x, f.y) || !IsAreaFree(s, x, y, f.x, f.y, stack)) return false;
            return !checkWeight || HasWeightRoom(s, stack.Weight, stack);
        }

        /// <summary>Would a new stack of <paramref name="def"/> weighing <paramref name="addKg"/> fit at
        /// this spot? (For drops arriving from another inventory or the floor.)</summary>
        public bool FitsNew(ItemDef def, float addKg, GridSection s, int x, int y, bool rotated, bool checkWeight)
        {
            if (def == null || !Sections.Contains(s)) return false;
            Vector2Int f = Footprint(def, rotated);
            if (!InBounds(s, x, y, f.x, f.y) || !IsAreaFree(s, x, y, f.x, f.y)) return false;
            return !checkWeight || HasWeightRoom(s, addKg);
        }

        /// <summary>Finds the first free spot for a new stack, scanning sections in order (pockets first
        /// for the survivor), rows top to bottom, trying each item both ways round.</summary>
        public bool TryFindSpot(ItemDef def, float addKg, bool checkWeight,
            out GridSection section, out int x, out int y, out bool rotated, GridSection only = null)
        {
            Vector2Int f = BaseFootprint(def);
            for (int i = 0; i < Sections.Count; i++)
            {
                GridSection s = Sections[i];
                if (only != null && !ReferenceEquals(s, only)) continue;
                if (checkWeight && !HasWeightRoom(s, addKg)) continue;
                if (FindInSection(s, f, null, out x, out y, out rotated)) { section = s; return true; }
            }
            section = null; x = 0; y = 0; rotated = false;
            return false;
        }

        // First-fit scan of one section; growable sections are searched one footprint past their last row.
        private bool FindInSection(GridSection s, Vector2Int f, ItemStack ignore, out int fx, out int fy, out bool rot)
        {
            int rows = s.Growable ? RowsUsed(s) + Mathf.Max(f.x, f.y) : s.Height;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < s.Width; x++)
                {
                    if (InBounds(s, x, y, f.x, f.y) && IsAreaFree(s, x, y, f.x, f.y, ignore))
                    { fx = x; fy = y; rot = false; return true; }
                    if (f.x != f.y && InBounds(s, x, y, f.y, f.x) && IsAreaFree(s, x, y, f.y, f.x, ignore))
                    { fx = x; fy = y; rot = true; return true; }
                }
            fx = 0; fy = 0; rot = false;
            return false;
        }
        #endregion

        #region Placement
        /// <summary>Repositions a stack already in <see cref="Owner"/> (another section, another cell or
        /// turned). Returns false, changing nothing, when the spot doesn't fit.</summary>
        public bool Move(ItemStack stack, GridSection s, int x, int y, bool rotated, bool checkWeight = true)
        {
            if (!Fits(stack, s, x, y, rotated, checkWeight)) return false;
            Assign(stack, s, x, y, rotated);
            Owner?.RaiseChanged();
            return true;
        }

        /// <summary>Gives a stack the first free spot (weight-checked when asked). False when none.</summary>
        public bool AutoPlace(ItemStack stack, bool checkWeight)
        {
            if (stack == null) return false;
            Vector2Int f = BaseFootprint(stack.Def);
            for (int i = 0; i < Sections.Count; i++)
            {
                GridSection s = Sections[i];
                if (checkWeight && !HasWeightRoom(s, stack.Weight, stack)) continue;
                if (FindInSection(s, f, stack, out int x, out int y, out bool rot))
                {
                    Assign(stack, s, x, y, rot);
                    return true;
                }
            }
            return false;
        }

        internal static void Assign(ItemStack stack, GridSection s, int x, int y, bool rotated)
        {
            stack.Section = s;
            stack.GridX = x;
            stack.GridY = y;
            stack.Rotated = rotated;
        }

        /// <summary>
        /// Repairs the arrangement after the sections changed (a bag came off, the pockets shrank) or
        /// stacks arrived unplaced: keeps every valid, non-overlapping placement and re-places the rest,
        /// largest first — respecting section weight where possible, otherwise just finding room.
        /// Returns the stacks left with no room at all (the caller decides their fate — the survivor
        /// drops them).
        /// </summary>
        public List<ItemStack> Validate()
        {
            var unplaced = new List<ItemStack>();
            if (Owner == null) return unplaced;

            var taken = new Dictionary<GridSection, HashSet<int>>();
            var toPlace = new List<ItemStack>();
            List<ItemStack> stacks = Owner.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack st = stacks[i];
                GridSection s = st.Section;
                Vector2Int f = Footprint(st);
                if (s == null || !Sections.Contains(s) || !InBounds(s, st.GridX, st.GridY, f.x, f.y) || !Claim(taken, s, st.GridX, st.GridY, f))
                {
                    st.Section = null;
                    toPlace.Add(st);
                }
            }

            if (toPlace.Count == 0) return unplaced;
            toPlace.Sort(BySize);
            for (int i = 0; i < toPlace.Count; i++)
            {
                ItemStack st = toPlace[i];
                if (!AutoPlace(st, checkWeight: true) && !AutoPlace(st, checkWeight: false)) unplaced.Add(st);
            }
            Owner.RaiseChanged();
            return unplaced;
        }

        // Marks a footprint's cells as used; false if any was already claimed (an overlap).
        private static bool Claim(Dictionary<GridSection, HashSet<int>> taken, GridSection s, int x, int y, Vector2Int f)
        {
            if (!taken.TryGetValue(s, out HashSet<int> cells)) taken[s] = cells = new HashSet<int>();
            for (int cy = y; cy < y + f.y; cy++)
                for (int cx = x; cx < x + f.x; cx++)
                    if (cells.Contains(cy * s.Width + cx)) return false;
            for (int cy = y; cy < y + f.y; cy++)
                for (int cx = x; cx < x + f.x; cx++)
                    cells.Add(cy * s.Width + cx);
            return true;
        }

        /// <summary>
        /// "Tidy up": repacks every stack in the given order (biggest first by default), honouring
        /// section weight where it can. All-or-nothing — if the new packing can't hold everything the
        /// old arrangement is restored and false is returned, so tidying never spills anything.
        /// </summary>
        public bool Tidy(GridSortMode mode = GridSortMode.Size)
        {
            if (Owner == null) return false;
            var stacks = new List<ItemStack>(Owner.Stacks);
            var before = new (GridSection s, int x, int y, bool r)[stacks.Count];
            for (int i = 0; i < stacks.Count; i++)
            {
                before[i] = (stacks[i].Section, stacks[i].GridX, stacks[i].GridY, stacks[i].Rotated);
                stacks[i].Section = null;
            }

            var order = new List<ItemStack>(stacks);
            order.Sort(Comparer(mode));
            for (int i = 0; i < order.Count; i++)
            {
                ItemStack st = order[i];
                if (AutoPlace(st, checkWeight: true) || AutoPlace(st, checkWeight: false)) continue;

                for (int k = 0; k < stacks.Count; k++)
                    Assign(stacks[k], before[k].s, before[k].x, before[k].y, before[k].r);
                return false;
            }
            Owner.RaiseChanged();
            return true;
        }

        /// <summary>The ordering "Tidy up" uses for a sort mode.</summary>
        public static System.Comparison<ItemStack> Comparer(GridSortMode mode)
        {
            switch (mode)
            {
                case GridSortMode.Category: return ByCategory;
                case GridSortMode.Name: return ByName;
                case GridSortMode.Weight: return ByWeight;
                default: return BySize;
            }
        }

        private static int Area(ItemStack s) { Vector2Int f = BaseFootprint(s.Def); return f.x * f.y; }
        private static string NameOf(ItemStack s) => s.Def != null ? s.Def.DisplayName ?? s.Def.Id : "";

        private static int BySize(ItemStack a, ItemStack b)
        {
            int c = Area(b).CompareTo(Area(a));
            return c != 0 ? c : ByCategory(a, b);
        }

        private static int ByCategory(ItemStack a, ItemStack b)
        {
            int c = ((int)a.Def.Category).CompareTo((int)b.Def.Category);
            return c != 0 ? c : ByName(a, b);
        }

        private static int ByName(ItemStack a, ItemStack b)
        {
            int c = string.CompareOrdinal(NameOf(a), NameOf(b));
            return c != 0 ? c : b.Count.CompareTo(a.Count);
        }

        private static int ByWeight(ItemStack a, ItemStack b)
        {
            int c = b.Weight.CompareTo(a.Weight);
            return c != 0 ? c : ByName(a, b);
        }
        #endregion

        #region Packing loose pieces (the floor)
        /// <summary>
        /// Packs arbitrary items (e.g. the ground piles within reach, which aren't stacks of an
        /// inventory) into a growable grid <paramref name="width"/> cells wide, first-fit in the given
        /// order. Fills <paramref name="cells"/> with each item's footprint rectangle and
        /// <paramref name="rotated"/> with its orientation, index-aligned with <paramref name="defs"/>.
        /// Returns the number of rows used.
        /// </summary>
        public static int PackGrowable(IList<ItemDef> defs, int width, List<RectInt> cells, List<bool> rotated)
        {
            cells.Clear();
            rotated.Clear();
            width = Mathf.Max(MaxFootprint, width);
            var used = new HashSet<int>();
            int rows = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                Vector2Int f = BaseFootprint(defs[i]);
                bool placed = false;
                int limit = rows + Mathf.Max(f.x, f.y);
                for (int y = 0; y < limit && !placed; y++)
                    for (int x = 0; x < width && !placed; x++)
                    {
                        if (TryClaimLoose(used, width, x, y, f.x, f.y)) { cells.Add(new RectInt(x, y, f.x, f.y)); rotated.Add(false); placed = true; }
                        else if (f.x != f.y && TryClaimLoose(used, width, x, y, f.y, f.x)) { cells.Add(new RectInt(x, y, f.y, f.x)); rotated.Add(true); placed = true; }
                    }
                if (!placed) { cells.Add(new RectInt(0, limit, f.x, f.y)); rotated.Add(false); TryClaimLoose(used, width, 0, limit, f.x, f.y); }
                RectInt r = cells[cells.Count - 1];
                rows = Mathf.Max(rows, r.yMax);
            }
            return rows;
        }

        private static bool TryClaimLoose(HashSet<int> used, int width, int x, int y, int w, int h)
        {
            if (x + w > width) return false;
            for (int cy = y; cy < y + h; cy++)
                for (int cx = x; cx < x + w; cx++)
                    if (used.Contains(cy * width + cx)) return false;
            for (int cy = y; cy < y + h; cy++)
                for (int cx = x; cx < x + w; cx++)
                    used.Add(cy * width + cx);
            return true;
        }
        #endregion
    }
}
