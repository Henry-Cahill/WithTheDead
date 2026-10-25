using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A weight-limited bag of <see cref="ItemStack"/>s, used both for the player's
    /// carried items and for world containers. Stacks respect each item's MaxStack.
    /// Containers pass <c>enforceWeight: false</c> so they can hold anything.
    ///
    /// An inventory may carry an <see cref="InventoryLayout"/> — a grid every stack occupies a spot
    /// in. With one, a new stack also needs a free spot (and, when weight is enforced, a section with
    /// weight to spare); a forced add that finds no room still lands, unplaced, for the owner to deal
    /// with (the survivor drops it — see <see cref="PlayerInventory"/>).
    /// </summary>
    public class Inventory
    {
        public readonly List<ItemStack> Stacks = new List<ItemStack>();
        public float MaxWeight;

        public event System.Action Changed;

        /// <summary>The grid arrangement, or null for a plain (unarranged) inventory.</summary>
        public InventoryLayout Layout { get; private set; }

        public Inventory(float maxWeight) { MaxWeight = maxWeight; }

        public float CurrentWeight
        {
            get
            {
                float w = 0f;
                for (int i = 0; i < Stacks.Count; i++) w += Stacks[i].Weight;
                return w;
            }
        }

        /// <summary>Arranges this inventory on a grid (packing whatever it already holds).</summary>
        public void SetLayout(InventoryLayout layout)
        {
            if (Layout != null) Layout.Owner = null;
            Layout = layout;
            if (layout != null)
            {
                layout.Owner = this;
                layout.Validate();
            }
            Changed?.Invoke();
        }

        /// <summary>Raised by the layout after it rearranges stacks.</summary>
        internal void RaiseChanged() => Changed?.Invoke();

        /// <summary>True when one more unit would fit — by weight, and by grid space when arranged.</summary>
        public bool WouldFit(ItemDef def, ItemExtra extra = null)
        {
            if (def == null || CurrentWeight + def.Weight > MaxWeight + 1e-4f) return false;
            if (Layout == null || FindStackWithRoom(def, true, extra) != null) return true;
            return Layout.TryFindSpot(def, def.Weight, true, out _, out _, out _, out _);
        }

        /// <summary>Adds up to <paramref name="count"/> units, honouring weight (unless
        /// <paramref name="enforceWeight"/> is false) and stack limits. Returns the amount added.
        /// When <paramref name="spawnHours"/> is given (a transfer from another stack), the
        /// created/merged perishable stack inherits the older age so food can't be laundered fresh.
        /// When <paramref name="condition"/> is &gt;= 0, a new damageable stack keeps that wear
        /// (a transfer / restore) instead of starting pristine, so gear can't be repaired by moving it.
        /// <paramref name="timesRepaired"/> carries the item's repair history alongside its wear, and
        /// <paramref name="extra"/> its per-instance extras (merging only into stacks whose extras agree).</summary>
        public int Add(ItemDef def, int count, bool enforceWeight = true, float spawnHours = float.NaN, int condition = -1, int timesRepaired = 0, ItemExtra extra = null)
        {
            if (def == null || count <= 0) return 0;
            bool carryAge = def.Perishable && !float.IsNaN(spawnHours);
            int added = 0;
            while (count > 0)
            {
                if (enforceWeight && CurrentWeight + def.Weight > MaxWeight + 1e-4f) break;
                ItemStack stack = FindStackWithRoom(def, enforceWeight, extra);
                if (stack != null)
                {
                    stack.Count++;
                    // Mixing newer food into an older stack ages down to the worse of the two.
                    if (carryAge) stack.SpawnHours = Mathf.Min(stack.SpawnHours, spawnHours);
                }
                else
                {
                    GridSection section = null;
                    int gx = 0, gy = 0;
                    bool rot = false;
                    if (Layout != null && !Layout.TryFindSpot(def, def.Weight, enforceWeight, out section, out gx, out gy, out rot))
                    {
                        if (enforceWeight) break;   // no room left on the grid
                        section = null;             // forced in unplaced; the owner decides what happens to it
                    }
                    ItemStack ns = NewStack(def, spawnHours, condition, timesRepaired, extra);
                    if (section != null) InventoryLayout.Assign(ns, section, gx, gy, rot);
                    Stacks.Add(ns);
                }
                added++;
                count--;
            }
            if (added > 0) Changed?.Invoke();
            return added;
        }

        /// <summary>Adds up to one stack's worth as a new stack at a chosen grid spot (a drag-and-drop
        /// arriving from elsewhere). Returns the number added (0 if the spot doesn't fit).</summary>
        public int AddAt(ItemDef def, int count, GridSection section, int x, int y, bool rotated,
            bool enforceWeight = true, float spawnHours = float.NaN, int condition = -1, int timesRepaired = 0, ItemExtra extra = null)
        {
            // Validate inputs and ensure layout/section exist.
            if (def == null || count <= 0 || Layout == null || section == null) return 0;
            // Clamp count to a sensible maximum: at most one stack's worth.
            // Limit count to one stack's max size; ensures we don't over-add.
            count = Mathf.Min(count, Mathf.Max(1, def.MaxStack));
            // If weight enforcement is on, ensure there is enough overall and section capacity for the items.
            if (enforceWeight)
            {
                float room = MaxWeight - CurrentWeight;
                if (!section.Unlimited) room = Mathf.Min(room, section.Capacity - Layout.WeightIn(section));
                if (def.Weight > 0f) count = Mathf.Min(count, Mathf.FloorToInt((room + 1e-4f) / def.Weight));
                if (count <= 0) return 0;
            }
            // Verify the layout can accommodate the new stack at the requested spot with the given rotation.
            if (!Layout.FitsNew(def, def.Weight * count, section, x, y, rotated, enforceWeight)) return 0;

            // Build the new stack and place it on the layout.
            ItemStack ns = NewStack(def, spawnHours, condition, timesRepaired, extra);
            ns.Count = count;
            InventoryLayout.Assign(ns, section, x, y, rotated);
            Stacks.Add(ns);
            Changed?.Invoke();
            return count;
        }

        /// <summary>Moves a stack of this inventory to another spot (or turns it). False if it won't fit.</summary>
        public bool MoveTo(ItemStack stack, GridSection section, int x, int y, bool rotated, bool enforceWeight = true)
            => Layout != null && Stacks.Contains(stack) && Layout.Move(stack, section, x, y, rotated, enforceWeight);

        /// <summary>Splits <paramref name="count"/> units off a stack into a new stack at a free spot of
        /// this inventory. Returns the new stack, or null if the spot doesn't fit.</summary>
        public ItemStack SplitTo(ItemStack stack, int count, GridSection section, int x, int y, bool rotated)
        {
            if (Layout == null || section == null || stack == null || !Stacks.Contains(stack) || count <= 0 || count >= stack.Count) return null;
            if (!Layout.FitsNew(stack.Def, 0f, section, x, y, rotated, false)) return null;
            if (!section.Unlimited && !ReferenceEquals(stack.Section, section)
                && !Layout.HasWeightRoom(section, stack.Def.Weight * count)) return null;

            ItemStack ns = NewStack(stack.Def, stack.SpawnHours, stack.Def.IsDamageable ? stack.Condition : -1, stack.TimesRepaired, stack.Extra);
            ns.Count = count;
            stack.Count -= count;
            InventoryLayout.Assign(ns, section, x, y, rotated);
            Stacks.Add(ns);
            Changed?.Invoke();
            return ns;
        }

        /// <summary>Tops up <paramref name="target"/> (a stack of this inventory) with up to
        /// <paramref name="count"/> units of <paramref name="from"/>, which lives in
        /// <paramref name="source"/> (this inventory or another). Same item only, within MaxStack and —
        /// when enforced — the target section's weight. Returns the number moved.</summary>
        public int MergeInto(ItemStack target, Inventory source, ItemStack from, int count, bool enforceWeight = true)
        {
            if (target == null || from == null || source == null || target == from) return 0;
            if (target.Def != from.Def || !Stacks.Contains(target) || !source.Stacks.Contains(from)) return 0;
            if (!ItemExtra.CanStack(target.Extra, from.Extra)) return 0;
            int moved = Mathf.Min(count, from.Count, target.Def.MaxStack - target.Count);
            if (enforceWeight && target.Def.Weight > 0f)
            {
                // From elsewhere the whole pack must take the weight; within this pack only a move into
                // another section shifts weight — onto that section's cap.
                bool fromOutside = source != this;
                bool crossSection = !fromOutside && Layout != null && !ReferenceEquals(target.Section, from.Section);
                float room = fromOutside ? MaxWeight - CurrentWeight : float.MaxValue;
                if ((fromOutside || crossSection) && Layout != null && target.Section != null && !target.Section.Unlimited)
                    room = Mathf.Min(room, target.Section.Capacity - Layout.WeightIn(target.Section));
                if (room < float.MaxValue)
                    moved = Mathf.Min(moved, Mathf.FloorToInt((room + 1e-4f) / target.Def.Weight));
            }
            if (moved <= 0) return 0;

            if (target.Def.Perishable) target.SpawnHours = Mathf.Min(target.SpawnHours, from.SpawnHours);
            target.Count += moved;
            source.Remove(from, moved);   // raises source.Changed
            if (source != this) Changed?.Invoke();
            return moved;
        }

        /// <summary>Moves up to <paramref name="count"/> of a stack into <paramref name="other"/> at a chosen
        /// grid spot: merging into a matching stack that already covers that cell, otherwise starting a
        /// new stack there. Returns the amount actually moved.</summary>
        public int TransferToCell(Inventory other, ItemStack stack, int count, GridSection section, int x, int y, bool rotated, bool enforceWeight = true)
        {
            // Ensure both inventories and the source stack are valid before proceeding.
            if (other == null || stack == null || other.Layout == null || !Stacks.Contains(stack)) return 0;
            // Clamp count to the amount actually present in the stack.
            count = Mathf.Min(count, stack.Count);
            if (count <= 0) return 0;

            // Look for an existing stack at the target cell.
            ItemStack there = other.Layout.StackAt(section, x, y);
            // If there's a stack there and it's a different one, try to merge if same item type.
            if (there != null && there != stack)
                return there.Def == stack.Def ? other.MergeInto(there, this, stack, count, enforceWeight) : 0;

            // If the destination inventory is the same as the source, either move the whole stack or split it.
            if (other == this)
            {
                // Moving the whole stack to a new spot.
                if (count >= stack.Count) return MoveTo(stack, section, x, y, rotated, enforceWeight) ? count : 0;
                // Splitting part of the stack into a new spot.
                return SplitTo(stack, count, section, x, y, rotated) != null ? count : 0;
            }

            // Otherwise, add a new stack to the other inventory and remove the transferred amount from this one.
            int moved = other.AddAt(stack.Def, count, section, x, y, rotated, enforceWeight, stack.SpawnHours,
                stack.Def != null && stack.Def.IsDamageable ? stack.Condition : -1, stack.TimesRepaired, stack.Extra);
            if (moved > 0) Remove(stack, moved);
            return moved;
        }

        private static ItemStack NewStack(ItemDef def, float spawnHours, int condition, int timesRepaired, ItemExtra extra = null)
        {
            var ns = new ItemStack(def, 1);
            if (def.Perishable && !float.IsNaN(spawnHours)) ns.SpawnHours = spawnHours;
            if (def.IsDamageable && condition >= 0)
            {
                ns.Condition = Mathf.Clamp(condition, 0, def.MaxCondition);
                ns.TimesRepaired = Mathf.Max(0, timesRepaired);
            }
            ns.Extra = ItemExtra.CopyOf(extra);
            return ns;
        }

        public void Remove(ItemStack stack, int count)
        {
            if (stack == null || count <= 0) return;
            stack.Count -= count;
            if (stack.Count <= 0)
            {
                Stacks.Remove(stack);
                stack.Section = null;
            }
            Changed?.Invoke();
        }

        /// <summary>Empties every stack (used when restoring a saved inventory).</summary>
        public void Clear()
        {
            Stacks.Clear();
            Changed?.Invoke();
        }

        /// <summary>Moves up to <paramref name="count"/> of a stack into another inventory.
        /// Returns the amount actually moved (may be capped by the target's capacity).</summary>
        public int TransferTo(Inventory other, ItemStack stack, int count, bool enforceWeight = true)
        {
            if (other == null || stack == null) return 0;
            count = Mathf.Min(count, stack.Count);
            int moved = other.Add(stack.Def, count, enforceWeight, stack.SpawnHours,   // carry freshness across
                stack.Def != null && stack.Def.IsDamageable ? stack.Condition : -1,     // and wear, so it isn't laundered
                stack.TimesRepaired, stack.Extra);                                      // (along with its repair history and extras)
            if (moved > 0) Remove(stack, moved);
            return moved;
        }

        public void TransferAllTo(Inventory other, bool enforceWeight = true)
        {
            for (int i = Stacks.Count - 1; i >= 0; i--)
                TransferTo(other, Stacks[i], Stacks[i].Count, enforceWeight);
        }

        // A stack of the same item with room to merge into (and matching extras). On an arranged inventory
        // with weight enforced, a stack whose section is already at its weight cap doesn't count.
        private ItemStack FindStackWithRoom(ItemDef def, bool enforceWeight, ItemExtra extra = null)
        {
            for (int i = 0; i < Stacks.Count; i++)
            {
                ItemStack s = Stacks[i];
                if (s.Def != def || s.Count >= def.MaxStack) continue;
                if (!ItemExtra.CanStack(s.Extra, extra)) continue;
                if (enforceWeight && Layout != null && s.Section != null && !Layout.HasWeightRoom(s.Section, def.Weight)) continue;
                return s;
            }
            return null;
        }
    }
}
