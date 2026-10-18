using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>One extra verb on an item's menu ("Attach to M9", "Strip mods", "Throw").</summary>
    public sealed class ItemMenuAction
    {
        public string Label;
        public Action Run;
        public bool Enabled = true;
        public ItemMenuAction(string label, Action run, bool enabled = true) { Label = label; Run = run; Enabled = enabled && run != null; }
    }

    /// <summary>
    /// The registry of item behaviours added by features: what "Use" does for an item the base inventory doesn't
    /// understand (a splint, a bolt, a blood bag, a skill book), and extra menu verbs on any item in the pack.
    /// </summary>
    public static class ItemActions
    {
        public delegate bool UseHandler(PlayerInventory inv, ItemStack stack);
        public delegate void MenuProvider(PlayerInventory inv, ItemStack stack, List<ItemMenuAction> into);

        private static readonly Dictionary<string, UseHandler> s_ById = new Dictionary<string, UseHandler>();
        private static readonly List<KeyValuePair<Func<ItemDef, bool>, UseHandler>> s_ByRule = new List<KeyValuePair<Func<ItemDef, bool>, UseHandler>>();
        private static readonly List<MenuProvider> s_Menus = new List<MenuProvider>();

        /// <summary>Handles "Use" for one item id.</summary>
        public static void RegisterUse(string itemId, UseHandler handler)
        {
            if (!string.IsNullOrEmpty(itemId) && handler != null) s_ById[itemId] = handler;
        }

        /// <summary>Handles "Use" for every item matching a rule (all artifacts, all blood bags).</summary>
        public static void RegisterUse(Func<ItemDef, bool> rule, UseHandler handler)
        {
            if (rule != null && handler != null) s_ByRule.Add(new KeyValuePair<Func<ItemDef, bool>, UseHandler>(rule, handler));
        }

        public static void RegisterMenu(MenuProvider provider)
        {
            if (provider != null && !s_Menus.Contains(provider)) s_Menus.Add(provider);
        }

        /// <summary>True when some feature knows how to use this item.</summary>
        public static bool HasUse(ItemDef def)
        {
            if (def == null) return false;
            if (s_ById.ContainsKey(def.Id)) return true;
            for (int i = 0; i < s_ByRule.Count; i++) if (s_ByRule[i].Key(def)) return true;
            return false;
        }

        /// <summary>Runs a feature's use handler; true when one handled the item (consumed or not).</summary>
        public static bool TryUse(PlayerInventory inv, ItemStack stack)
        {
            if (inv == null || stack == null || stack.Def == null) return false;
            if (s_ById.TryGetValue(stack.Def.Id, out UseHandler h)) return Safe(h, inv, stack);
            for (int i = 0; i < s_ByRule.Count; i++)
                if (s_ByRule[i].Key(stack.Def)) return Safe(s_ByRule[i].Value, inv, stack);
            return false;
        }

        /// <summary>Collects every feature's extra verbs for a pack item.</summary>
        public static void Gather(PlayerInventory inv, ItemStack stack, List<ItemMenuAction> into)
        {
            into.Clear();
            if (inv == null || stack == null || stack.Def == null) return;
            for (int i = 0; i < s_Menus.Count; i++)
            {
                try { s_Menus[i](inv, stack, into); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private static bool Safe(UseHandler h, PlayerInventory inv, ItemStack s)
        {
            try { return h(inv, s); }
            catch (Exception e) { Debug.LogException(e); return true; }
        }
    }
}
