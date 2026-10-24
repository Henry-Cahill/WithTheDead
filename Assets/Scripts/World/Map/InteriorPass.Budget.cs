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

        /// <summary>Lamps a home may have in all; and the ones its ceilings leave for desk, bedside and standard lamps.</summary>
        private const int HomeLamps = 7, HomeSpareLamps = 2;
        /// <summary>The most lamps any building has (a school, a hospital), and those kept for desk lamps.</summary>
        private const int MaxLamps = 14, WorkSpareLamps = 3;

        private static readonly List<PlanRoom> s_ByNeed = new List<PlanRoom>(32);

        private static void Budget(InteriorPlan plan, Site site)
        {
            Footprint f = plan.Footprint;
            bool home = plan.Kind == RaidBuildingKind.House;
            float floor = 0f;
            foreach (PlanRoom r in plan.Rooms) floor += InnerArea(r);

            // Loot: each room's pool and the gameplay layer's count for it, trimmed to the building's budget (the biggest
            // counts first, a room keeping one piece while it can).
            int total = 0;
            foreach (PlanRoom r in plan.Rooms)
            {
                r.Pool = MapGameplay.ContainersFor(site, f, r.Kind);
                r.Loot = r.Pool.Length == 0 ? 0 : MapGameplay.ContainerCount(site, f, r.Kind, InnerArea(r));
                total += r.Loot;
            }
            int cap = LootCap(plan, floor);
            for (int guard = 0; total > cap && guard < 512; guard++)
            {
                PlanRoom most = null;
                foreach (PlanRoom r in plan.Rooms)
                    if (r.Loot > 1 && (most == null || r.Loot > most.Loot)) most = r;
                if (most == null)
                    foreach (PlanRoom r in plan.Rooms)
                        if (r.Loot > 0 && (most == null || InnerArea(r) < InnerArea(most))) most = r;
                if (most == null) break;
                most.Loot--;
                total--;
            }

            // Lamps: a ceiling fitting or more for each room by its size, the rooms people use first, within the building's
            // budget; what's left of it goes to desk, bedside and standard lamps as the rooms are furnished.
            plan.LampCap = home ? HomeLamps : Mathf.Clamp(6 + plan.Rooms.Count / 2, 8, MaxLamps);
            int ceilings = plan.LampCap - (home ? HomeSpareLamps : WorkSpareLamps);
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

        // How many pieces holding loot a building may have in all, by what it is and how big.
        private static int LootCap(InteriorPlan plan, float floor)
        {
            switch (plan.Use)
            {
                case InteriorUse.Shed: case InteriorUse.Garage: case InteriorUse.Greenhouse: case InteriorUse.Restrooms: return 4;
                case InteriorUse.Cottage: case InteriorUse.Trailer: return 8;
                case InteriorUse.House: return 12;
                case InteriorUse.Church: case InteriorUse.Barn: return Mathf.Clamp(Mathf.RoundToInt(floor / 40f), 4, 10);
                default: return Mathf.Clamp(Mathf.RoundToInt(6f + floor / 25f), 8, 40);
            }
        }

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

        // A fitting for a small room, more for a big one (the corridor's along its length).
        private static int LampsWanted(PlanRoom r)
        {
            float a = InnerArea(r);
            if (a < 2.5f) return 0;
            if (r.Role == RoomRole.Closet || r.Role == RoomRole.Bathroom || r.Role == RoomRole.Restroom) return 1;
            if (r.Role == RoomRole.Hall) return Mathf.Max(r.R.width, r.R.height) > 9f ? 2 : 1;
            return a < 40f ? 1 : a < 110f ? 2 : a < 260f ? 3 : 4;
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
