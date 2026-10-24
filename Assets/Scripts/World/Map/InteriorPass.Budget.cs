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
        private const int HomeLamps = 7, HomeSpareLamps = 2;
        /// <summary>The most lamps any building has (a school, a hospital), and those kept for desk lamps.</summary>
        private const int MaxLamps = 10, WorkSpareLamps = 2;

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
                r.Pool = MapGameplay.ContainersFor(site, f, r.Kind);
            }
            AllotLoot(site, f, plan.Purpose, kinds, areas, counts);
            for (int i = 0; i < n; i++) plan.Rooms[i].Loot = plan.Rooms[i].Pool.Length == 0 ? 0 : counts[i];

            // Lamps: a ceiling fitting or more for each room by its size, the rooms people use first, within the building's
            // budget; what's left of it goes to desk, bedside and standard lamps as the rooms are furnished.
            plan.LampCap = home ? HomeLamps : Mathf.Clamp(4 + plan.Rooms.Count / 3, 6, MaxLamps);
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

        #region How many loot containers
        // How many loot containers each room holds: the gameplay layer's budget for the building (MapGameplay.Allot, on its
        // way to this branch), mirrored here meanwhile on the same model: each room's need by its kind and floor, the sum
        // scaled by how well stocked the Zone is there, within what a building of its purpose and size holds, shared out by
        // the largest remainders. Every container rolls its table afresh each raid, so a building's loot is its count.
        private const int SaltBudget = 0x6A60;

        private static int AllotLoot(Site site, Footprint f, string purpose, string[] rooms, float[] areas, int[] counts)
        {
            int n = rooms.Length;
            for (int i = 0; i < n; i++) counts[i] = 0;
            if (n == 0) return 0;
            var need = new float[n];
            float sum = 0f, floor = 0f;
            for (int i = 0; i < n; i++)
            {
                need[i] = Mathf.Max(0f, RoomNeed(rooms[i], areas[i]));
                sum += need[i];
                floor += Mathf.Max(0f, areas[i]);
            }
            float richness = MapGameplay.RichnessOf(site, f);
            float wanted = sum * StockOf(purpose) * richness;
            int whole = Mathf.FloorToInt(wanted);
            int dithered = wanted <= 0f ? 0 : whole + (MapStyle.Unit(f.Data.id, SaltBudget) < wanted - whole ? 1 : 0);
            int total = Mathf.Min(Mathf.RoundToInt(BuildingCap(purpose, floor) * Mathf.Max(1f, richness)), dithered);
            if (total <= 0 || sum <= 0f) return 0;
            float scale = total / sum;
            int given = 0;
            var rest = new float[n];
            for (int i = 0; i < n; i++)
            {
                float exact = need[i] * scale;
                counts[i] = Mathf.FloorToInt(exact + 1e-4f);
                rest[i] = need[i] > 0f ? exact - counts[i] : -1f;
                given += counts[i];
            }
            for (; given < total; given++)
            {
                int best = -1;
                for (int i = 0; i < n; i++)
                    if (rest[i] >= 0f && (best < 0 || rest[i] > rest[best] + 1e-4f)) best = i;
                if (best < 0) break;
                counts[best]++;
                rest[best] = -1f;
            }
            return given;
        }

        // How stocked a building of a purpose is next to an ordinary one (the gameplay layer's profiles; the few known here).
        private static float StockOf(string purpose)
        {
            switch (purpose)
            {
                case MapGameplay.Purposes.Grocery: case MapGameplay.Purposes.Hardware: return 1.3f;
                case MapGameplay.Purposes.Pharmacy: return 1.2f;
                case MapGameplay.Purposes.Church: return 0.8f;
                case MapGameplay.Purposes.Funeral: return 0.7f;
                case MapGameplay.Purposes.Vacant: return 0.3f;
                case MapGameplay.Purposes.Ruin: return 0.3f;
                default: return 1f;
            }
        }

        private static float RoomNeed(string room, float area)
        {
            switch (room)
            {
                case RoomKinds.Kitchen: return area >= 14f ? 3f : 2f;
                case RoomKinds.Kitchenette: return area >= 10f ? 2f : 1f;
                case RoomKinds.Bathroom: return 1f;
                case RoomKinds.Bedroom: return area >= 16f ? 2f : 1f;
                case RoomKinds.Living: return area < 10f ? 0f : area >= 30f ? 2f : 1f;
                case RoomKinds.Hall: return area >= 12f ? 1f : 0f;
                case RoomKinds.Sales: return Mathf.Min(8f, 1f + area / 40f);
                case RoomKinds.Stock: return Mathf.Min(5f, 1f + area / 30f);
                case RoomKinds.Office: return area >= 20f ? 2f : 1f;
                case RoomKinds.Storage: return Mathf.Min(4f, 1f + area / 25f);
                case RoomKinds.Workshop: case RoomKinds.Garage: return Mathf.Min(4f, 1f + area / 40f);
                case RoomKinds.Classroom: return 1f;
                case RoomKinds.Nave: return area >= 60f ? 1f : 0f;
                case RoomKinds.Ward: return Mathf.Min(4f, 1f + area / 30f);
                case RoomKinds.Cells: return 0f;
                case RoomKinds.Open: return Mathf.Min(6f, 1f + area / 120f);
                default: return Mathf.Min(3f, area / 30f);
            }
        }

        private static int BuildingCap(string purpose, float area)
        {
            switch (purpose)
            {
                case MapGameplay.Purposes.None: return 0;
                case MapGameplay.Purposes.Vacant: case MapGameplay.Purposes.Ruin: return 1;
                case MapGameplay.Purposes.Shed: case MapGameplay.Purposes.Greenhouse: return 2;
                case MapGameplay.Purposes.Garage: case MapGameplay.Purposes.Outbuilding: return 3;
                case MapGameplay.Purposes.Barn: return 4;
                case MapGameplay.Purposes.Trailer: return 5;
                case MapGameplay.Purposes.Home: return Mathf.Clamp(Mathf.RoundToInt(3f + area / 40f), 4, 9);
                case MapGameplay.Purposes.Church: case MapGameplay.Purposes.Funeral: case MapGameplay.Purposes.Childcare: return 6;
                case MapGameplay.Purposes.Apartments: case MapGameplay.Purposes.Hotel: case MapGameplay.Purposes.Care:
                    return Mathf.Clamp(Mathf.RoundToInt(4f + area / 50f), 6, 24);
                case MapGameplay.Purposes.Warehouse: case MapGameplay.Purposes.Industrial: case MapGameplay.Purposes.StorageRental:
                case MapGameplay.Purposes.Distillery:
                    return Mathf.Clamp(Mathf.RoundToInt(3f + area / 300f), 4, 12);
                case MapGameplay.Purposes.Grocery: case MapGameplay.Purposes.DepartmentStore:
                    return Mathf.Clamp(Mathf.RoundToInt(4f + area / 60f), 6, 24);
                case MapGameplay.Purposes.School: case MapGameplay.Purposes.Hospital: case MapGameplay.Purposes.Prison:
                    return Mathf.Clamp(Mathf.RoundToInt(4f + area / 150f), 8, 28);
                default: return Mathf.Clamp(Mathf.RoundToInt(4f + area / 60f), 5, 16);
            }
        }
        #endregion

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
            if (a < 2.5f) return 0;
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
