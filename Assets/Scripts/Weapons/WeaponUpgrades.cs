using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The technician's branching upgrade trees (STALKER-style). Each tree has three tiers of two mutually
    /// exclusive options: taking one locks the other for good, and each tier needs one of the tier before it.
    /// Upgrades are stored on the item itself (<see cref="ItemExtra.Upgrades"/>), so they travel with it — and
    /// are lost with it.
    /// </summary>
    public static class WeaponUpgrades
    {
        public sealed class Node
        {
            public string Id, Name, Effect;
            public int Tier;              // 1..3
            public int Cost;              // military-grade rounds
            public string Part;           // a part the technician needs (consumed), or null
            public bool ForArmor;
        }

        public static readonly List<Node> Nodes = new List<Node>
        {
            // Weapons.
            new Node { Id = "w_acc1", Tier = 1, Name = "Lapped barrel",     Effect = "Spread -12%",              Cost = 220,  Part = "scrap" },
            new Node { Id = "w_rel1", Tier = 1, Name = "Polished action",   Effect = "Jams -45%",                Cost = 200,  Part = "scrap" },
            new Node { Id = "w_acc2", Tier = 2, Name = "Free-floated rail", Effect = "Spread -15%, recoil -10%",  Cost = 480,  Part = "circuitboard" },
            new Node { Id = "w_rof2", Tier = 2, Name = "Lightened bolt",    Effect = "Fire rate +15%",           Cost = 450,  Part = "scrap" },
            new Node { Id = "w_dmg3", Tier = 3, Name = "Hot-loaded chamber",Effect = "Damage +8%",               Cost = 900,  Part = "chemicals" },
            new Node { Id = "w_mag3", Tier = 3, Name = "Widened mag well",  Effect = "Magazine +5, reload -10%", Cost = 850,  Part = "scrap" },
            // Armour and suits.
            new Node { Id = "a_dur1",  Tier = 1, ForArmor = true, Name = "Reinforced stitching", Effect = "Armour wears 30% slower", Cost = 260, Part = "thread" },
            new Node { Id = "a_lite1", Tier = 1, ForArmor = true, Name = "Trimmed padding",      Effect = "Movement penalty halved", Cost = 240, Part = "scrap" },
            new Node { Id = "a_rad2",  Tier = 2, ForArmor = true, Name = "Sealed liner",         Effect = "+20% radiation & chemical resistance", Cost = 520, Part = "chemicals" },
            new Node { Id = "a_plate2",Tier = 2, ForArmor = true, Name = "Ceramic inserts",      Effect = "+1 armour class (max 6)", Cost = 640, Part = "scrap" },
            new Node { Id = "a_belt3", Tier = 3, ForArmor = true, Name = "Artifact harness",     Effect = "+1 artifact slot",        Cost = 900, Part = "circuitboard" },
            new Node { Id = "a_carry3",Tier = 3, ForArmor = true, Name = "Load-bearing frame",   Effect = "+6 kg carry",             Cost = 820, Part = "scrap" },
        };

        public static Node Get(string id)
        {
            for (int i = 0; i < Nodes.Count; i++) if (Nodes[i].Id == id) return Nodes[i];
            return null;
        }

        /// <summary>The two options of a tier for weapons or armour.</summary>
        public static void TierOptions(bool armor, int tier, List<Node> into)
        {
            into.Clear();
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].ForArmor == armor && Nodes[i].Tier == tier) into.Add(Nodes[i]);
        }

        /// <summary>Whether an item can take an upgrade now, and why not.</summary>
        public static bool CanApply(ItemStack stack, Node node, out string reason)
        {
            reason = null;
            if (stack == null || node == null) { reason = "Nothing to upgrade"; return false; }
            bool isArmor = Ballistics.Armor(stack.Def.Id) != null;
            bool isGun = stack.Def.IsFirearm;
            if (node.ForArmor ? !isArmor : !isGun) { reason = node.ForArmor ? "Only armour and suits" : "Only firearms"; return false; }
            List<string> have = stack.Extra != null ? stack.Extra.Upgrades : null;
            if (have != null && have.Contains(node.Id)) { reason = "Already fitted"; return false; }
            for (int i = 0; i < Nodes.Count; i++)
            {
                Node other = Nodes[i];
                if (other.ForArmor == node.ForArmor && other.Tier == node.Tier && other.Id != node.Id && have != null && have.Contains(other.Id))
                { reason = "Locked: " + other.Name + " took this tier"; return false; }
            }
            if (node.Tier > 1)
            {
                bool prev = false;
                if (have != null)
                    for (int i = 0; i < have.Count; i++)
                    {
                        Node h = Get(have[i]);
                        if (h != null && h.ForArmor == node.ForArmor && h.Tier == node.Tier - 1) { prev = true; break; }
                    }
                if (!prev) { reason = "Needs a tier " + (node.Tier - 1) + " upgrade first"; return false; }
            }
            return true;
        }

        public static void Apply(ItemDef def, List<string> upgrades, ref WeaponBuild b)
        {
            if (upgrades == null) return;
            for (int i = 0; i < upgrades.Count; i++)
            {
                switch (upgrades[i])
                {
                    case "w_acc1": b.Spread *= 0.88f; break;
                    case "w_rel1": b.JamMult *= 0.55f; break;
                    case "w_acc2": b.Spread *= 0.85f; b.Recoil *= 0.9f; break;
                    case "w_rof2": b.FireRateMult *= 1.15f; break;
                    case "w_dmg3": b.DamageMult *= 1.08f; break;
                    case "w_mag3": b.MagSize += 5; b.ReloadMult *= 0.9f; break;
                }
            }
        }

        /// <summary>Armour stats with this piece's upgrades applied (a fresh copy; the table entry is untouched).</summary>
        public static ArmorStats Effective(ArmorStats a, ItemExtra extra)
        {
            if (a == null) return null;
            if (extra == null || extra.Upgrades.Count == 0) return a;
            var e = new ArmorStats
            {
                Id = a.Id, Class = a.Class, Zones = a.Zones, BluntThrough = a.BluntThrough, MovePenalty = a.MovePenalty,
                ErgoPenalty = a.ErgoPenalty, Resist = a.Resist, ArtifactSlots = a.ArtifactSlots, CarryBonusKg = a.CarryBonusKg,
            };
            List<string> u = extra.Upgrades;
            if (u.Contains("a_lite1")) e.MovePenalty *= 0.5f;
            if (u.Contains("a_rad2")) { e.Resist.Radiation += 0.2f; e.Resist.Chemical += 0.2f; }
            if (u.Contains("a_plate2") && e.Class > 0) e.Class = Mathf.Min(6, e.Class + 1);
            if (u.Contains("a_belt3")) e.ArtifactSlots += 1;
            if (u.Contains("a_carry3")) e.CarryBonusKg += 6f;
            return e;
        }

        /// <summary>How fast this piece's durability wears (reinforced stitching slows it).</summary>
        public static float WearMult(ItemExtra extra) => extra != null && extra.Upgrades.Contains("a_dur1") ? 0.7f : 1f;
    }
}
