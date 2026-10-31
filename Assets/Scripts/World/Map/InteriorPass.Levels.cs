using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The floors above and below a building's ground floor (KentuckyMap.Levels.cs has their stairwell): the blocks their
    // rooms take (the main block less the stair band, then the wings), and what's laid out there by what the building is
    // used as. Such a floor is come into from the landing hall at the head (or the foot) of the stairs; a corridor runs from
    // it along the stair band's side of the main block, open onto it, with the rooms off the corridor, so each has a door.
    internal static partial class InteriorPass
    {
        private const int SaltUpper = 0x1A74;
        private const float HomeCorridor = 1.3f, FlatsCorridor = 1.5f;

        /// <summary>What an upper floor is used as.</summary>
        private enum Upper { Home, Flats, Guests, Offices, Storage, Classrooms, Wards, Bunks, Meeting }

        // A level's blocks: the main block less the stair band first, then the wings beyond the main block (whatever of each
        // lies outside it); a basement has the main block's alone.
        private static List<Rect> LevelBlocks(LevelLayout L, List<Rect> blocks, int level)
        {
            var into = new List<Rect>(4) { L.Rest };
            if (level < 0) return into;
            var pieces = new List<Rect>(4);
            foreach (Rect b in blocks)
            {
                pieces.Clear();
                Levels.RectLess(b, L.Main, pieces);
                Rect best = default;
                float bestArea = 0f;
                foreach (Rect r in pieces)
                {
                    float area = r.width * r.height;
                    if (r.width >= 2.2f && r.height >= 2.2f && area > bestArea)
                    {
                        best = r;
                        bestArea = area;
                    }
                }
                if (bestArea < 7f) continue;
                bool clear = true;
                foreach (Rect r in into)
                    if (Overlap(r.xMin, r.xMax, best.xMin, best.xMax) > 0.05f && Overlap(r.yMin, r.yMax, best.yMin, best.yMax) > 0.05f)
                    {
                        clear = false;
                        break;
                    }
                if (clear) into.Add(best);
            }
            return into;
        }

        // What's over the ground floor: bedrooms over a home, flats or offices (or the stock) over a shop or a diner on a town
        // street, more of the same over a workplace, a loft over a barn or a warehouse. The same on every upper floor of a
        // building.
        private static Upper UpperOf(InteriorPlan plan)
        {
            float pick = MapStyle.Unit(plan.Id, SaltUpper);
            switch (plan.Use)
            {
                case InteriorUse.House: case InteriorUse.Cottage: case InteriorUse.Trailer: case InteriorUse.Funeral:
                    return Upper.Home;
                case InteriorUse.Apartments: return Upper.Flats;
                case InteriorUse.Motel: return Upper.Guests;
                case InteriorUse.Store: case InteriorUse.Convenience: case InteriorUse.Hardware: case InteriorUse.GasStation:
                case InteriorUse.Salon: case InteriorUse.Pharmacy: case InteriorUse.Restaurant: case InteriorUse.Bar:
                case InteriorUse.BigStore: case InteriorUse.AutoShop:
                    if (plan.Town && plan.Footprint.Size < 700f) return pick < 0.5f ? Upper.Flats : pick < 0.8f ? Upper.Offices : Upper.Storage;
                    return pick < 0.55f ? Upper.Offices : Upper.Storage;
                case InteriorUse.School: return Upper.Classrooms;
                case InteriorUse.Hospital: case InteriorUse.Clinic: return Upper.Wards;
                case InteriorUse.FireStation: return Upper.Bunks;
                case InteriorUse.Church: return Upper.Meeting;
                case InteriorUse.Warehouse: case InteriorUse.Workshop: case InteriorUse.Barn: case InteriorUse.Shed:
                case InteriorUse.Garage: case InteriorUse.Greenhouse:
                    return Upper.Storage;
                default: return Upper.Offices;
            }
        }

        // An upper floor, by what's over the ground floor (UpperOf); the corridor the rooms open off, if there is one.
        private static PlanRoom LayUpper(InteriorPlan plan, LevelLayout L, List<Rect> blocks, System.Random rng)
        {
            Upper use = UpperOf(plan);
            Rect rest = blocks[0];
            PlanRoom spine = null;
            switch (use)
            {
                case Upper.Home: spine = UpperHome(plan, L, rest); break;
                case Upper.Flats: case Upper.Guests: spine = UpperUnits(plan, L, rest, use == Upper.Guests); break;
                case Upper.Storage: Add(plan, StoreRole(plan, rest), rest); break;
                default: spine = UpperRooms(plan, L, rest, use); break;
            }
            for (int i = 1; i < blocks.Count; i++) UpperWing(plan, blocks[i], use, rng);
            return spine;
        }

        // Over a home: the bathroom at the landing's end of the corridor, the bedrooms along the rest of it.
        private static PlanRoom UpperHome(InteriorPlan plan, LevelLayout L, Rect rest)
        {
            if (!Spine(plan, L, rest, HomeCorridor, out PlanRoom spine, out Rect rooms, out bool alongX, out bool fromHigh, out _))
            {
                Add(plan, RoomRole.Bedroom, rest);
                return null;
            }
            float len = alongX ? rooms.width : rooms.height;
            if (len < 2.4f + MinBay)
            {
                Add(plan, RoomRole.Bedroom, rooms);
                return spine;
            }
            int beds = Mathf.Max(1, Mathf.RoundToInt((len - 2.4f) / 3.6f));
            var sizes = new float[beds + 1];
            sizes[0] = 2.4f;
            for (int k = 1; k <= beds; k++) sizes[k] = k == beds ? 0f : (len - 2.4f) / beds;
            Rect[] parts = alongX ? SplitX(rooms, fromHigh, 2.2f, sizes) : SplitZ(rooms, fromHigh, 2.2f, sizes);
            Add(plan, RoomRole.Bathroom, parts[0]);
            for (int k = 1; k < parts.Length; k++) Add(plan, RoomRole.Bedroom, parts[k]);
            return spine;
        }

        // Flats (or a motel's rooms) along the corridor, each with its own door onto it; a floor too small for a corridor is
        // one flat.
        private static PlanRoom UpperUnits(InteriorPlan plan, LevelLayout L, Rect rest, bool motel)
        {
            var bays = new List<Bay>(8);
            if (!SpineBays(plan, L, rest, FlatsCorridor, motel ? 4.4f : 7.2f, bays, out PlanRoom spine))
            {
                Unit(plan, new Bay { R = rest, Side = SideOf(L) }, motel, 0);
                return null;
            }
            for (int i = 0; i < bays.Count; i++) Unit(plan, bays[i], motel, i);
            return spine;
        }

        // Offices (classrooms, wards, a firehouse's bunk rooms, a church's meeting rooms) along the corridor, the restrooms
        // by the landing.
        private static PlanRoom UpperRooms(InteriorPlan plan, LevelLayout L, Rect rest, Upper use)
        {
            float bay = use == Upper.Classrooms ? 7f : use == Upper.Meeting ? 5.5f : use == Upper.Bunks ? 4.5f : 3.6f;
            var bays = new List<Bay>(12);
            if (!SpineBays(plan, L, rest, CorridorWidth, bay, bays, out PlanRoom spine))
            {
                RoomRole only = use == Upper.Classrooms ? RoomRole.Classroom : use == Upper.Wards ? RoomRole.Ward : use == Upper.Bunks ? RoomRole.Bunks
                    : use == Upper.Meeting ? RoomRole.Meeting : RoomRole.OpenOffice;
                Add(plan, only, rest);
                return null;
            }
            int n = bays.Count;
            for (int i = 0; i < n; i++)
            {
                RoomRole role;
                if (i == 0 && n >= 3) role = RoomRole.Restroom;
                else if (use == Upper.Classrooms) role = RoomRole.Classroom;
                else if (use == Upper.Wards) role = i % 2 == 1 ? RoomRole.Ward : RoomRole.Exam;
                else if (use == Upper.Bunks) role = i == 1 ? RoomRole.DayRoom : RoomRole.Bunks;
                else if (use == Upper.Meeting) role = i % 2 == 1 ? RoomRole.Classroom : RoomRole.Meeting;
                else if (n == 1) role = RoomRole.OpenOffice;
                else if (i == n - 1 && n >= 4) role = RoomRole.Meeting;
                else if (i == n - 2 && n >= 6) role = RoomRole.BreakRoom;
                else role = RoomRole.Office;
                Add(plan, role, bays[i].R);
            }
            return spine;
        }

        // A loft: a barn's hayloft, a warehouse's upper floor, the stock kept over a shop.
        private static RoomRole StoreRole(InteriorPlan plan, Rect r)
        {
            if (plan.Use == InteriorUse.Barn) return RoomRole.Barn;
            bool works = plan.Use == InteriorUse.Warehouse || plan.Use == InteriorUse.Workshop;
            return works && r.width * r.height > 60f ? RoomRole.Warehouse : RoomRole.Stock;
        }

        // A wing beyond the main block on an upper floor: another bedroom (or a den, or a closet) over a home, the tenants'
        // laundry over flats, an office or the stock over a workplace.
        private static void UpperWing(InteriorPlan plan, Rect r, Upper use, System.Random rng)
        {
            float area = r.width * r.height, small = Mathf.Min(r.width, r.height);
            RoomRole role;
            switch (use)
            {
                case Upper.Home: role = area >= 9f && small >= 2.6f ? (rng.NextDouble() < 0.75 ? RoomRole.Bedroom : RoomRole.Den) : RoomRole.Closet; break;
                case Upper.Flats: case Upper.Guests: role = area >= 12f ? RoomRole.Laundry : RoomRole.Closet; break;
                case Upper.Storage: role = StoreRole(plan, r); break;
                case Upper.Classrooms: role = area >= 30f ? RoomRole.Classroom : RoomRole.Office; break;
                case Upper.Bunks: role = area >= 14f ? RoomRole.Bunks : RoomRole.Stock; break;
                case Upper.Wards: role = area >= 14f ? RoomRole.Exam : RoomRole.Stock; break;
                default: role = area >= 14f ? RoomRole.Office : RoomRole.Stock; break;
            }
            Add(plan, role, r);
        }

        // A basement: the laundry at the foot of the stairs, a workshop, a den done out in panelling, the stores; a small one
        // a single room.
        private static PlanRoom LayBasement(InteriorPlan plan, LevelLayout L, Rect rest, System.Random rng)
        {
            var bays = new List<Bay>(6);
            if (rest.width * rest.height < 24f || !SpineBays(plan, L, rest, HomeCorridor, 4f, bays, out PlanRoom spine))
            {
                Add(plan, rng.NextDouble() < 0.5 ? RoomRole.Den : RoomRole.Stock, rest);
                return null;
            }
            if (spine != null) spine.Name = "Basement";
            int n = bays.Count;
            for (int i = 0; i < n; i++)
            {
                RoomRole role = n == 1 ? RoomRole.Den
                    : i == 0 ? RoomRole.Laundry
                    : i == n - 1 ? RoomRole.Den
                    : i == 1 ? RoomRole.Workshop
                    : RoomRole.Stock;
                Add(plan, role, bays[i].R);
            }
            return spine;
        }

        // The ground floor's stair hall open onto the room beside it people come in through most, along all the wall they
        // share: the room the front door opens into, where that's beside it (and not a bedroom or a bathroom), so the way in
        // to the stairs has no doorway in it; else the public room (the living room, the shop floor, a lobby, a hall)
        // sharing the most, where any shares a doorway's worth.
        private static void OpenOnto(InteriorPlan plan, PlanRoom hall)
        {
            if (plan.Entrances.Count > 0)
            {
                PlanEntrance en = plan.Entrances[0];
                Vector2 inside = en.At - en.Outward * 0.4f;
                int i = plan.RoomAt(inside);
                if (i < 0) i = plan.NearestRoom(inside);
                PlanRoom entry = i >= 0 ? plan.Rooms[i] : null;
                if (entry != null && entry != hall && entry.Unit < 0 && (Public(entry.Role) || entry.Role == RoomRole.Kitchen || entry.Role == RoomRole.Den)
                    && SharedLength(hall.R, entry.R) > DoorRun)
                {
                    OpenLater(plan, hall, entry);
                    return;
                }
            }
            PlanRoom best = null;
            float bestLen = DoorRun;
            foreach (PlanRoom r in plan.Rooms)
            {
                if (r == hall || r.Unit >= 0 || !Public(r.Role)) continue;
                float shared = SharedLength(hall.R, r.R);
                if (shared <= bestLen) continue;
                bestLen = shared;
                best = r;
            }
            if (best != null) OpenLater(plan, hall, best);
        }

        private static bool Public(RoomRole r) => r == RoomRole.Living || r == RoomRole.Hall || r == RoomRole.Lobby || r == RoomRole.Sales
            || r == RoomRole.Waiting || r == RoomRole.Narthex || r == RoomRole.Eatery || r == RoomRole.Dining || r == RoomRole.OpenOffice
            || r == RoomRole.Bar;

        // How long a wall two room rectangles (on wall centre lines) share.
        private static float SharedLength(Rect a, Rect b)
        {
            const float eps = 0.02f;
            if (Mathf.Abs(a.xMax - b.xMin) < eps || Mathf.Abs(b.xMax - a.xMin) < eps) return Overlap(a.yMin, a.yMax, b.yMin, b.yMax);
            if (Mathf.Abs(a.yMax - b.yMin) < eps || Mathf.Abs(b.yMax - a.yMin) < eps) return Overlap(a.xMin, a.xMax, b.xMin, b.xMax);
            return 0f;
        }

        // A basement's rooms are bare block and concrete under a bulb, but for a den done out in panelling.
        private static void Unfinished(InteriorPlan plan)
        {
            foreach (PlanRoom r in plan.Rooms)
            {
                if (r.Role == RoomRole.Den || r.Role == RoomRole.Stair) continue;
                r.Design = null;
                r.Floor = FloorKind.Concrete;
                r.Light = LightStyle.Bulb;
            }
        }

        // The rest of the main block cut into a corridor `corridor` wide along its stair-band side (it goes into the plan,
        // to be opened onto the landing) and the rectangle beyond it the rooms take: `alongX` when the corridor runs along
        // X, `fromHigh` when its landing end is its high one, `side` the side of the rooms it runs along (as a Bay's). A
        // narrow building's corridor carries its hall on through the rest, as wide. False (and nothing added) when there's
        // no room for both.
        private static bool Spine(InteriorPlan plan, LevelLayout L, Rect rest, float corridor, out PlanRoom spine, out Rect rooms,
            out bool alongX, out bool fromHigh, out int side)
        {
            spine = null;
            rooms = rest;
            if (L.Narrow)
            {
                // The hall beside a narrow building's core carried on through the rest as the corridor, the rooms in line with
                // the core.
                Rect h = L.Hall;
                bool high = h.center.x > rest.center.x;
                alongX = false;
                fromHigh = h.center.y > rest.center.y;
                side = high ? 1 : 3;
                if (rest.width - h.width < MinBay || rest.height < MinBay) return false;
                Rect way = high ? Rect.MinMaxRect(h.xMin, rest.yMin, rest.xMax, rest.yMax) : Rect.MinMaxRect(rest.xMin, rest.yMin, h.xMax, rest.yMax);
                rooms = high ? Rect.MinMaxRect(rest.xMin, rest.yMin, way.xMin, rest.yMax) : Rect.MinMaxRect(way.xMax, rest.yMin, rest.xMax, rest.yMax);
                spine = Add(plan, RoomRole.Hall, way);
                spine.Name = "Corridor";
                return true;
            }
            Vector2 n = L.Side;   // from the stair band toward the rooms
            alongX = Mathf.Abs(n.y) > 0.5f;
            fromHigh = alongX ? L.Hall.center.x > rest.center.x : L.Hall.center.y > rest.center.y;
            side = SideOf(L);
            float across = alongX ? rest.height : rest.width, along = alongX ? rest.width : rest.height;
            // A hall across the building in front of the stairs (a narrow building's) is the corridor already.
            if (L.OpenSide) return across >= MinBay && along >= MinBay;
            if (across < corridor + MinBay || along < MinBay) return false;
            Rect corr;
            switch (side)
            {
                case 3:
                    corr = Rect.MinMaxRect(rest.xMin, rest.yMin, rest.xMin + corridor, rest.yMax);
                    rooms = Rect.MinMaxRect(corr.xMax, rest.yMin, rest.xMax, rest.yMax);
                    break;
                case 1:
                    corr = Rect.MinMaxRect(rest.xMax - corridor, rest.yMin, rest.xMax, rest.yMax);
                    rooms = Rect.MinMaxRect(rest.xMin, rest.yMin, corr.xMin, rest.yMax);
                    break;
                case 2:
                    corr = Rect.MinMaxRect(rest.xMin, rest.yMax - corridor, rest.xMax, rest.yMax);
                    rooms = Rect.MinMaxRect(rest.xMin, rest.yMin, rest.xMax, corr.yMin);
                    break;
                default:
                    corr = Rect.MinMaxRect(rest.xMin, rest.yMin, rest.xMax, rest.yMin + corridor);
                    rooms = Rect.MinMaxRect(rest.xMin, corr.yMax, rest.xMax, rest.yMax);
                    break;
            }
            spine = Add(plan, RoomRole.Hall, corr);
            spine.Name = "Corridor";
            return true;
        }

        // The side of the rest of the main block the stair band lies along, as a Bay's (0 south, 1 east, 2 north, 3 west).
        private static int SideOf(LevelLayout L)
        {
            if (L.Narrow) return L.Up.y > 0f ? 2 : 0;   // the core and its hall across the back
            Vector2 n = L.Side;
            return Mathf.Abs(n.y) > 0.5f ? (n.y < 0f ? 2 : 0) : (n.x > 0f ? 3 : 1);
        }

        // As Spine, with the rooms' rectangle cut into bays of about `bay` metres along the corridor, nearest the landing
        // first.
        private static bool SpineBays(InteriorPlan plan, LevelLayout L, Rect rest, float corridor, float bay, List<Bay> bays, out PlanRoom spine)
        {
            bays.Clear();
            if (!Spine(plan, L, rest, corridor, out spine, out Rect rooms, out bool alongX, out bool fromHigh, out int side)) return false;
            AddBays(rooms, alongX, side, bay, bays);
            float lo = alongX ? rooms.xMin : rooms.yMin, hi = alongX ? rooms.xMax : rooms.yMax;
            for (int i = 0; i < bays.Count; i++)
            {
                Bay b = bays[i];
                float at = alongX ? b.R.center.x : b.R.center.y;
                b.Order = fromHigh ? hi - at : at - lo;
                bays[i] = b;
            }
            bays.Sort((a, b) => a.Order.CompareTo(b.Order));
            return true;
        }
    }
}
