using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Per-instance data an item can carry beyond its count, freshness and wear: whether it was
    /// <see cref="FoundInRaid"/>, whether it is <see cref="Insured"/>, the attachments bolted onto a gun
    /// (<see cref="Mods"/>), how fouled a gun is (<see cref="Dirt"/>), the technician
    /// <see cref="Upgrades"/> fitted to a weapon or suit, the round a gun prefers
    /// (<see cref="AmmoChoice"/>) and a free-form <see cref="Note"/> (a PDA's stash tip, a map's zone, a
    /// car key's vehicle). It travels with the stack through every transfer, drop, pick-up and save.
    /// Stacks only merge when their extras agree (<see cref="CanStack"/>), so found-in-raid loot never
    /// blends into bought goods.
    /// </summary>
    [Serializable]
    public sealed class ItemExtra
    {
        public bool FoundInRaid;
        public bool Insured;
        public List<string> Mods = new List<string>();
        public float Dirt;
        public List<string> Upgrades = new List<string>();
        public string AmmoChoice;
        public string Note;
        /// <summary>An artifact worn on the belt (its effects apply) rather than just carried.</summary>
        public bool OnBelt;

        /// <summary>True when nothing beyond the defaults is recorded.</summary>
        public bool IsEmpty => !FoundInRaid && !Insured && Mods.Count == 0 && Dirt <= 0f && !OnBelt
                               && Upgrades.Count == 0 && string.IsNullOrEmpty(AmmoChoice) && string.IsNullOrEmpty(Note);

        /// <summary>A deep copy of these extras (lists duplicated) so the original stays untouched.</summary>
        public ItemExtra Clone()
        {
            var e = new ItemExtra
            {
                FoundInRaid = FoundInRaid,
                Insured = Insured,
                Dirt = Dirt,
                AmmoChoice = AmmoChoice,
                Note = Note,
                OnBelt = OnBelt,
            };
            e.Mods.AddRange(Mods);
            e.Upgrades.AddRange(Upgrades);
            return e;
        }

        /// <summary>A copy, or null for null / empty extras (so plain stacks stay plain).</summary>
        public static ItemExtra CopyOf(ItemExtra e) => e == null || e.IsEmpty ? null : e.Clone();

        /// <summary>Two stacks may merge only when their extras agree on everything that matters per unit.</summary>
        public static bool CanStack(ItemExtra a, ItemExtra b)
        {
            bool aEmpty = a == null || a.IsEmpty, bEmpty = b == null || b.IsEmpty;
            if (aEmpty && bEmpty) return true;
            if (aEmpty || bEmpty)
            {
                ItemExtra other = aEmpty ? b : a;
                return other.IsEmpty;
            }
            return a.FoundInRaid == b.FoundInRaid && a.Insured == b.Insured && !a.OnBelt && !b.OnBelt
                && a.Mods.Count == 0 && b.Mods.Count == 0 && a.Upgrades.Count == 0 && b.Upgrades.Count == 0
                && a.Dirt == b.Dirt && string.Equals(a.AmmoChoice, b.AmmoChoice)
                && string.IsNullOrEmpty(a.Note) && string.IsNullOrEmpty(b.Note);
        }

        /// <summary>Whether <paramref name="id"/> is among the attachments bolted onto this gun.</summary>
        public bool HasMod(string id) => Mods.Contains(id);
        /// <summary>Whether <paramref name="id"/> is among the technician upgrades fitted to this item.</summary>
        public bool HasUpgrade(string id) => Upgrades.Contains(id);

        #region Compact text form (saves, kit files, network)
        /// <summary>Encodes to a short text form: <c>f;i;m=a,b;d=0.25;u=x,y;a=ammo;n=note</c>. Null when empty.</summary>
        public static string Serialize(ItemExtra e)
        {
            if (e == null || e.IsEmpty) return null;
            var sb = new StringBuilder(32);
            if (e.FoundInRaid) sb.Append("f;");
            if (e.Insured) sb.Append("i;");
            if (e.OnBelt) sb.Append("b;");
            if (e.Mods.Count > 0) sb.Append("m=").Append(Join(e.Mods)).Append(';');
            if (e.Dirt > 0f) sb.Append("d=").Append(e.Dirt.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(';');
            if (e.Upgrades.Count > 0) sb.Append("u=").Append(Join(e.Upgrades)).Append(';');
            if (!string.IsNullOrEmpty(e.AmmoChoice)) sb.Append("a=").Append(Uri.EscapeDataString(e.AmmoChoice)).Append(';');
            if (!string.IsNullOrEmpty(e.Note)) sb.Append("n=").Append(Uri.EscapeDataString(e.Note)).Append(';');
            return sb.ToString();
        }

        /// <summary>Decodes <see cref="Serialize"/>'s text form; null / empty text gives null.</summary>
        public static ItemExtra Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var e = new ItemExtra();
            string[] parts = text.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                if (p.Length == 0) continue;
                if (p == "f") { e.FoundInRaid = true; continue; }
                if (p == "i") { e.Insured = true; continue; }
                if (p == "b") { e.OnBelt = true; continue; }
                int eq = p.IndexOf('=');
                if (eq <= 0) continue;
                string k = p.Substring(0, eq), v = p.Substring(eq + 1);
                try
                {
                    switch (k)
                    {
                        case "m": Split(v, e.Mods); break;
                        case "d": float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out e.Dirt); break;
                        case "u": Split(v, e.Upgrades); break;
                        case "a": e.AmmoChoice = Uri.UnescapeDataString(v); break;
                        case "n": e.Note = Uri.UnescapeDataString(v); break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[CloneZ] Bad item extra '" + p + "': " + ex.Message);
                }
            }
            return e.IsEmpty ? null : e;
        }

        private static string Join(List<string> list)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Uri.EscapeDataString(list[i]));
            }
            return sb.ToString();
        }

        private static void Split(string v, List<string> into)
        {
            if (string.IsNullOrEmpty(v)) return;
            string[] items = v.Split(',');
            for (int i = 0; i < items.Length; i++)
                if (items[i].Length > 0) into.Add(Uri.UnescapeDataString(items[i]));
        }
        #endregion
    }

    /// <summary>Convenience accessors for <see cref="ItemStack.Extra"/>.</summary>
    public static class ItemStackExtras
    {
        /// <summary>The stack's extras, created on first use (for writing).</summary>
        public static ItemExtra Ensure(this ItemStack s)
        {
            if (s.Extra == null) s.Extra = new ItemExtra();
            return s.Extra;
        }

        /// <summary>Whether the stack was looted in a raid (containers, corpses, the ground).</summary>
        public static bool IsFoundInRaid(this ItemStack s) => s != null && s.Extra != null && s.Extra.FoundInRaid;
        /// <summary>Whether the stack carries insurance against loss.</summary>
        public static bool IsInsured(this ItemStack s) => s != null && s.Extra != null && s.Extra.Insured;
        /// <summary>The gun's fouling level (0 clean to 1 fouled); 0 when the stack has none recorded.</summary>
        public static float DirtLevel(this ItemStack s) => s != null && s.Extra != null ? s.Extra.Dirt : 0f;

        /// <summary>Marks the stack found-in-raid (raid loot: containers, corpses, the ground).</summary>
        public static void MarkFoundInRaid(this ItemStack s)
        {
            if (s == null) return;
            s.Ensure().FoundInRaid = true;
        }
    }

    /// <summary>DayZ's condition words: pristine, worn, damaged, badly damaged, ruined.</summary>
    public static class ItemConditionTiers
    {
        /// <summary>The condition word for a 0..1 condition value (shown wherever wear is read out).</summary>
        public static string Label(float c01) =>
            c01 <= 0f ? "Ruined" : c01 < 0.3f ? "Badly damaged" : c01 < 0.5f ? "Damaged" : c01 < 0.7f ? "Worn" : "Pristine";
    }
}
