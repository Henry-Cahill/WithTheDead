using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What a building's rooms are worth, settled with its plan (from its seed and its site, so every build of it agrees,
    // whenever and in whatever order it's furnished): the kinds of container their storage holds and how many of their
    // pieces hold loot (the gameplay layer's tables, within a budget for the building), the ceiling fittings they get
    // within the building's lamp budget, and the one or two rooms whose lights were left on when it was emptied.
    internal static partial class InteriorPass
    {
        private const int SaltLamps = 0x1A74;

        /// <summary>Lamps a home may have in all; and the ones its ceilings leave for desk, bedside and standard lamps. (Its
        /// fridge, stove and TV bring lights of their own, off until they're used.)</summary>
        private const int HomeLamps = 7;
        /// <summary>Spare lamps for homes, dynamic per building.</summary>
        private static int HomeSpareLampsFor(InteriorPlan plan)
        {
            // 2 spare lamps for the smallest house, up‑to 5 for larger ones.
            // Scale with room count (roughly building size) and clamp to [2,5].
            int baseSpare = Mathf.Clamp(plan.Rooms.Count / 5, 2, 5);
            // Add a small random jitter so two identical seeds don’t look identical.
            var rng = new System.Random(unchecked(plan.Seed ^ 0x5A3F));
            return baseSpare + rng.Next(0, 2); // +0 or +1 extra spare lamp
        }
        /// <summary>The most lamps any building has (a school, a hospital), and those kept for desk lamps.</summary>
        private const int MaxLamps = 10;
        /// <summary>Spare lamps for non‑home buildings, dynamic per building.</summary>
        private static int WorkSpareLampsFor(InteriorPlan plan)
        {
            // Offices, schools, hospitals etc. get a base of 2‑3 spare lamps.
            // Scale with room count, clamp to [2,4].
            int baseSpare = Mathf.Clamp(plan.Rooms.Count / 6, 2, 4);
            var rng = new System.Random(unchecked(plan.Seed ^ 0x1B7E));
            return baseSpare + rng.Next(0, 2);
        }

        private static readonly List<PlanRoom> s_ByNeed = new List<PlanRoom>(32);

        private static void Budget(InteriorPlan plan, Site site)
        {
            Footprint f = plan.Footprint;
            bool home = plan.Kind == RaidBuildingKind.House;
            float floor = 0f;
            foreach (PlanRoom r in plan.Rooms) floor += InnerArea(r);

            // Loot: each room's pool, and how many loot containers it holds in all (what its furniture opens on, drawer by
            // drawer: the builder keeps that many, its signature kind first, and takes the storage out of the rest).
            int n = plan.Rooms.Count;
            var kinds = new string[n];
            var areas = new float[n];
            var counts = new int[n];
            for (int i = 0; i < n; i++)
            {
                PlanRoom r = plan.Rooms[i];
                kinds[i] = r.Kind;
                areas[i] = InnerArea(r);
                // Nothing stands on the stairs: the stairwell's share goes to the rooms.
                r.Pool = r.Role == RoomRole.Stair ? System.Array.Empty<ContainerType>() : MapGameplay.ContainersFor(site, f, r.Kind);
            }
            MapGameplay.Allot(site, f, kinds, areas, counts);
            // A room whose kind holds nothing here hands its share to the biggest rooms that do.
            int lost = 0;
            for (int i = 0; i < n; i++)
                if (plan.Rooms[i].Pool.Length == 0) { lost += counts[i]; counts[i] = 0; }
            if (lost > 0)
            {
                var order = new List<int>(n);
                for (int i = 0; i < n; i++) if (plan.Rooms[i].Pool.Length > 0) order.Add(i);
                order.Sort((a, b) => Mathf.Abs(areas[a] - areas[b]) > 0.01f ? areas[b].CompareTo(areas[a]) : a.CompareTo(b));
                for (int k = 0; lost > 0 && order.Count > 0; k++, lost--) counts[order[k % order.Count]]++;
            }
            for (int i = 0; i < n; i++) plan.Rooms[i].Loot = counts[i];

            // Lamps: a ceiling fitting or more for each room by its size, the rooms people use first, within the building's
            // budget; what's left of it goes to desk, bedside and standard lamps as the rooms are furnished.
            plan.LampCap = home ? HomeLamps : Mathf.Clamp(4 + plan.Rooms.Count / 3, 6, MaxLamps);
            int ceilings = plan.LampCap - (home ? HomeSpareLampsFor(plan) : WorkSpareLampsFor(plan));
            s_ByNeed.Clear();
            s_ByNeed.AddRange(plan.Rooms);
            s_ByNeed.Sort((a, b) =>
            {
                int pa = LampPriority(a), pb = LampPriority(b);
                if (pa != pb) return pa.CompareTo(pb);
                float aa = InnerArea(a), ab = InnerArea(b);
                return Mathf.Abs(aa - ab) > 0.01f ? ab.CompareTo(aa) : a.Index.CompareTo(b.Index);
            });
            plan.CeilingLamps = 0;
            foreach (PlanRoom r in s_ByNeed)
            {
                int want = LampsWanted(r);
                r.Lamps = Mathf.Min(want, ceilings - plan.CeilingLamps);
                plan.CeilingLamps += r.Lamps;
            }

            // Left on: seldom more than a room (the living room or the kitchen of a home, a shop's floor, the front office).
            var rng = new System.Random(unchecked(plan.Seed ^ SaltLamps));
            double chance = home ? 0.36 : plan.Kind == RaidBuildingKind.Store ? 0.45 : 0.3;
            int lit = rng.NextDouble() < chance ? 1 : 0;
            if (lit == 1 && rng.NextDouble() < (home ? 0.1 : floor > 400f ? 0.45 : 0.15)) lit = 2;
            foreach (PlanRoom r in plan.Rooms) r.Lit = false;
            for (int k = 0; k < lit; k++)
            {
                PlanRoom best = null;
                float bestScore = float.MinValue;
                foreach (PlanRoom r in plan.Rooms)
                {
                    if (r.Lit || r.Lamps == 0) continue;
                    float score = LitPreference(r.Role) + (float)rng.NextDouble() * 1.5f;
                    if (score > bestScore) { bestScore = score; best = r; }
                }
                if (best == null) break;
                best.Lit = true;
            }

            // The pieces with bodies of their own a building may have before the rest of it is dressed more plainly.
            plan.PieceCap = home ? 70 : Mathf.Clamp(Mathf.RoundToInt(50f + floor * 0.12f), 60, 160);
        }

        /// <summary>A room's floor inside its walls (m²).</summary>
        internal static float InnerArea(PlanRoom r) => Mathf.Max(0f, r.R.width - 2f * Half) * Mathf.Max(0f, r.R.height - 2f * Half);

        // Lower first: the rooms people live and work in, then the private ones, then the ones they pass through or store
        // things in.
        private static int LampPriority(PlanRoom r)
        {
            if (r.Entry) return 0;
            switch (r.Role)
            {
                case RoomRole.Living: case RoomRole.Kitchen: case RoomRole.Sales: case RoomRole.Eatery: case RoomRole.Bar:
                case RoomRole.Nave: case RoomRole.Classroom: case RoomRole.Ward: case RoomRole.Squad: case RoomRole.OpenOffice:
                case RoomRole.Lobby: case RoomRole.Waiting: case RoomRole.Warehouse: case RoomRole.Workshop: case RoomRole.Bays:
                case RoomRole.Barn: case RoomRole.Gym: case RoomRole.Stacks: case RoomRole.Guest:
                    return 0;
                case RoomRole.Bedroom: case RoomRole.Office: case RoomRole.BackOffice: case RoomRole.Dining: case RoomRole.Den:
                case RoomRole.CommercialKitchen: case RoomRole.Exam: case RoomRole.Cells: case RoomRole.DayRoom: case RoomRole.Bunks:
                case RoomRole.BreakRoom: case RoomRole.Meeting: case RoomRole.Chapel: case RoomRole.Sacristy: case RoomRole.Narthex:
                case RoomRole.HomeGarage: case RoomRole.Shed: case RoomRole.Greenhouse:
                    return 1;
                case RoomRole.Bathroom: case RoomRole.Restroom: case RoomRole.Hall: case RoomRole.Stock: case RoomRole.Dispensary:
                case RoomRole.Records: case RoomRole.Sorting: case RoomRole.Vault: case RoomRole.Armory:
                    return 2;
                default:
                    return 3;
            }
        }

        // A fitting for a room (its light reaches the walls of most), two or three for a big hall.
        private static int LampsWanted(PlanRoom r)
        {
            float a = InnerArea(r);
            if (a < 2.5f || r.Role == RoomRole.Stair) return 0;
            return a < 150f ? 1 : a < 400f ? 2 : 3;
        }

        // Which room's light somebody left on.
        private static float LitPreference(RoomRole role)
        {
            switch (role)
            {
                case RoomRole.Living: case RoomRole.Kitchen: case RoomRole.Sales: case RoomRole.Eatery: case RoomRole.Bar:
                case RoomRole.Lobby: case RoomRole.Squad: case RoomRole.Bays: case RoomRole.Ward: case RoomRole.Waiting:
                    return 3f;
                case RoomRole.Bedroom: case RoomRole.Hall: case RoomRole.Office: case RoomRole.BackOffice: case RoomRole.Stock:
                case RoomRole.Workshop: case RoomRole.Cells: case RoomRole.Nave: case RoomRole.DayRoom:
                    return 2f;
                case RoomRole.Bathroom: case RoomRole.Restroom: case RoomRole.Den: case RoomRole.Dining:
                    return 1f;
                default:
                    return 0f;
            }
        }
    }
}
