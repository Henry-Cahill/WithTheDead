using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A weight-limited bag of <see cref="ItemStack"/>s, used both for the player's
    /// carried items and for world containers. Stacks respect each item's MaxStack.
    /// Containers pass <c>enforceWeight: false</c> so they can hold anything.
    /// </summary>
    public class Inventory
    {
        public readonly List<ItemStack> Stacks = new List<ItemStack>();
        public float MaxWeight;

        public event System.Action Changed;

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

        public bool WouldFit(ItemDef def) => CurrentWeight + def.Weight <= MaxWeight + 1e-4f;

        /// <summary>Adds up to <paramref name="count"/> units, honouring weight (unless
        /// <paramref name="enforceWeight"/> is false) and stack limits. Returns the amount added.
        /// When <paramref name="spawnHours"/> is given (a transfer from another stack), the
        /// created/merged perishable stack inherits the older age so food can't be laundered fresh.
        /// When <paramref name="condition"/> is &gt;= 0, a new damageable stack keeps that wear
        /// (a transfer / restore) instead of starting pristine, so gear can't be repaired by moving it.</summary>
        public int Add(ItemDef def, int count, bool enforceWeight = true, float spawnHours = float.NaN, int condition = -1)
        {
            if (def == null || count <= 0) return 0;
            bool carryAge = def.Perishable && !float.IsNaN(spawnHours);
            bool carryCondition = def.IsDamageable && condition >= 0;
            int added = 0;
            while (count > 0)
            {
                if (enforceWeight && CurrentWeight + def.Weight > MaxWeight + 1e-4f) break;
                ItemStack stack = FindStackWithRoom(def);
                if (stack != null)
                {
                    stack.Count++;
                    // Mixing newer food into an older stack ages down to the worse of the two.
                    if (carryAge) stack.SpawnHours = Mathf.Min(stack.SpawnHours, spawnHours);
                }
                else
                {
                    var ns = new ItemStack(def, 1);
                    if (carryAge) ns.SpawnHours = spawnHours;
                    if (carryCondition) ns.Condition = Mathf.Clamp(condition, 0, def.MaxCondition);
                    Stacks.Add(ns);
                }
                added++;
                count--;
            }
            if (added > 0) Changed?.Invoke();
            return added;
        }

        public void Remove(ItemStack stack, int count)
        {
            if (stack == null || count <= 0) return;
            stack.Count -= count;
            if (stack.Count <= 0) Stacks.Remove(stack);
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
                stack.Def != null && stack.Def.IsDamageable ? stack.Condition : -1);    // and wear, so it isn't laundered
            if (moved > 0) Remove(stack, moved);
            return moved;
        }

        public void TransferAllTo(Inventory other, bool enforceWeight = true)
        {
            for (int i = Stacks.Count - 1; i >= 0; i--)
                TransferTo(other, Stacks[i], Stacks[i].Count, enforceWeight);
        }

        private ItemStack FindStackWithRoom(ItemDef def)
        {
            for (int i = 0; i < Stacks.Count; i++)
                if (Stacks[i].Def == def && Stacks[i].Count < def.MaxStack) return Stacks[i];
            return null;
        }
    }
}
