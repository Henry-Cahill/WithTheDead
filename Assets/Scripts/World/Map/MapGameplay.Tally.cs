using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What a built square holds, for the log and the map harness: its loot containers building by building (the most a
    // building of its kind and size should hold: BuildingCap), and the stacks they rolled by category against the
    // Central Economy's default nominal stock for a map (CentralEconomy.Defaults; the gate only applies outside the
    // streamed map). One line per fixed square, so the economy can be checked after any pass changes what's furnished.
    internal static partial class MapGameplay
    {
        /// <summary>The last fixed square's loot tally (one line, also logged).</summary>
        internal static string LastLootStats { get; private set; } = "";

        // The Central Economy's default nominal stacks per category (ItemCategory order), as CentralEconomy.Defaults sets them.
        private static readonly int[] Nominal = { 160, 100, 70, 30, 14, 80, 180, 80, 140 };

        private const float TallyCell = 16f;

        private static void TallyLoot(Site site)
        {
            if (site.Chunked || site.Town == null || site.Town.World == null) return;
            List<EnterableBuilding> buildings = site.Enterables;
            // The enterables by the 16 m cells their bounds cover.
            var grid = new Dictionary<long, List<int>>();
            for (int i = 0; i < buildings.Count; i++)
            {
                Footprint f = buildings[i]?.Footprint;
                if (f == null) continue;
                Rect r = f.Bounds;
                for (int z = Mathf.FloorToInt(r.yMin / TallyCell); z <= Mathf.FloorToInt(r.yMax / TallyCell); z++)
                    for (int x = Mathf.FloorToInt(r.xMin / TallyCell); x <= Mathf.FloorToInt(r.xMax / TallyCell); x++)
                    {
                        long key = CellKey(x, z);
                        if (!grid.TryGetValue(key, out List<int> list)) grid[key] = list = new List<int>(2);
                        list.Add(i);
                    }
            }

            var inBuilding = new int[buildings.Count];
            var stacksBy = new int[Nominal.Length];
            int containers = 0, outside = 0, stacks = 0, empty = 0;
            foreach (LootContainer c in site.Town.World.GetComponentsInChildren<LootContainer>(true))
            {
                if (c == null || c.IsSack || c.Type == ContainerType.Corpse) continue;
                containers++;
                if (c.Contents == null || c.Contents.Stacks.Count == 0) empty++;
                else
                    foreach (ItemStack s in c.Contents.Stacks)
                    {
                        if (s == null || s.Def == null) continue;
                        stacks++;
                        int k = (int)s.Def.Category;
                        if (k >= 0 && k < stacksBy.Length) stacksBy[k]++;
                    }
                Vector3 w = c.transform.position - site.WorldOffset;
                var p = new Vector2(w.x, w.z);
                int at = -1;
                if (grid.TryGetValue(CellKey(Mathf.FloorToInt(p.x / TallyCell), Mathf.FloorToInt(p.y / TallyCell)), out List<int> near))
                    foreach (int i in near)
                    {
                        Footprint f = buildings[i].Footprint;
                        if (f.Bounds.Contains(p) && MapGeometry.Contains(f.Pts, p)) { at = i; break; }
                    }
                if (at >= 0) inBuilding[at]++;
                else outside++;
            }

            // Houses and the rest apart: their medians and the fullest of each; the buildings past their cap.
            var homes = new List<int>();
            var others = new List<int>();
            int over = 0, worst = -1, worstOver = 0, inside = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                Footprint f = buildings[i]?.Footprint;
                if (f == null) continue;
                string purpose = PurposeOf(site, f);
                int n = inBuilding[i];
                inside += n;
                bool home = purpose == Purposes.Home || purpose == Purposes.Trailer || purpose == Purposes.Apartments;
                (home ? homes : others).Add(n);
                int cap = BuildingCap(purpose, f.Size);
                if (n <= cap) continue;
                over++;
                if (n - cap > worstOver) { worstOver = n - cap; worst = i; }
            }

            var sb = new StringBuilder(256);
            sb.Append("Loot at ").Append(site.Area.Place).Append(": ").Append(containers).Append(" containers (")
                .Append(inside).Append(" in ").Append(buildings.Count).Append(" buildings, ").Append(outside)
                .Append(" outside: cars, yards), ").Append(stacks).Append(" stacks rolled, ").Append(empty).Append(" empty; homes ");
            Spread(sb, homes);
            sb.Append(", the rest ");
            Spread(sb, others);
            sb.Append("; ").Append(over).Append(" over their cap");
            if (worst >= 0)
            {
                Footprint f = buildings[worst].Footprint;
                string purpose = PurposeOf(site, f);
                sb.Append(" (fullest: ").Append(NameOf(site, f) ?? purpose).Append(", ").Append(Mathf.RoundToInt(f.Size)).Append(" m², ")
                    .Append(inBuilding[worst]).Append(" for a cap of ").Append(BuildingCap(purpose, f.Size)).Append(')');
            }
            sb.Append(". Stacks by category against the Central Economy's nominal:");
            for (int k = 0; k < Nominal.Length; k++)
                sb.Append(k == 0 ? " " : ", ").Append(((ItemCategory)k).ToString().ToLowerInvariant()).Append(' ')
                    .Append(stacksBy[k]).Append('/').Append(Nominal[k]);
            sb.Append('.');
            LastLootStats = sb.ToString();
            Debug.Log("[CloneZ] " + LastLootStats);
        }

        // "median 5, max 9 (64)": a list of counts in brief.
        private static void Spread(StringBuilder sb, List<int> counts)
        {
            if (counts.Count == 0)
            {
                sb.Append("none");
                return;
            }
            counts.Sort();
            sb.Append("median ").Append(counts[counts.Count / 2]).Append(", max ").Append(counts[counts.Count - 1])
                .Append(" (").Append(counts.Count).Append(')');
        }
    }
}
