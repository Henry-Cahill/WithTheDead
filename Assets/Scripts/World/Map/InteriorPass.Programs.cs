using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What a real building is used as (from its mapped kind and use, its size and its neighbourhood), and its ground floor
    // split into rooms to suit: houses with their living rooms by the front door and their bedrooms off a hall, shops with
    // their stock rooms behind, offices round a corridor, churches, schools, warehouses, barns and the rest.
    internal static partial class InteriorPass
    {
        private const int SaltPlan = 0x1A70, SaltUse = 0x1A71, SaltStyle = 0x1A72;

        #region What the building is used as
        /// <summary>What the enterable building <paramref name="e"/> is used as, or <see cref="InteriorUse.None"/> for a building
        /// with nothing to furnish (a canopy, a ruin, a silo): what it was for as the gameplay layer reads it
        /// (<paramref name="purpose"/>: its use, the point of interest standing in it, its name), else by its kind, size and
        /// surroundings.</summary>
        internal static InteriorUse UseOf(Site site, EnterableBuilding e, out bool town, out string purpose)
        {
            MapBuilding b = e.Footprint.Data;
            string use = b.use ?? "", kind = b.kind ?? "";
            float area = e.Footprint.Size;
            town = InTown(site, e.Footprint);
            purpose = MapGameplay.PurposeOf(site, e.Footprint);
            if (kind == "canopy" || kind == "ruin" || kind == "silo") return InteriorUse.None;
            InteriorUse byPurpose = ByPurpose(purpose, e, area, town);
            if (byPurpose != InteriorUse.None) return byPurpose;
            if (purpose == MapGameplay.Purposes.None || purpose == MapGameplay.Purposes.Ruin) return InteriorUse.None;
            float pick = MapStyle.Unit(b.id, SaltUse);
            InteriorUse byUse = ByUse(use, area);
            if (byUse != InteriorUse.None || use == "shelter" || use == "parking" || use == "car_wash") return byUse;
            switch (kind)
            {
                case "canopy": case "ruin": case "silo": return InteriorUse.None;
                case "shed": return area < 120f ? InteriorUse.Shed : InteriorUse.Workshop;
                case "garage": return area > 90f ? InteriorUse.Workshop : InteriorUse.Garage;
                case "barn": return InteriorUse.Barn;
                case "greenhouse": return InteriorUse.Greenhouse;
                case "trailer": return InteriorUse.Trailer;
                case "apartments": return area < 150f ? InteriorUse.House : InteriorUse.Apartments;
                case "hotel": return InteriorUse.Motel;
                case "church": return InteriorUse.Church;
                case "school": return InteriorUse.School;
                case "hospital": return area < 600f ? InteriorUse.Clinic : InteriorUse.Hospital;
                case "office": return InteriorUse.Office;
                case "civic": return InteriorUse.Civic;
                case "industrial": return area < 250f ? InteriorUse.Workshop : pick < 0.5f ? InteriorUse.Workshop : InteriorUse.Warehouse;
                case "warehouse": return town && area < 4000f && pick < 0.35f ? InteriorUse.BigStore : InteriorUse.Warehouse;
                case "retail": return area < 140f ? InteriorUse.Convenience : area > 900f ? InteriorUse.BigStore : InteriorUse.Store;
                case "house":
                    // building=yes under 250 m² comes as a house: the small ones are the outbuildings round a home.
                    if (area < 32f) return InteriorUse.Shed;
                    if (area < 48f) return e.Footprint.Bounds.width > 5.5f || e.Footprint.Bounds.height > 5.5f ? InteriorUse.Garage : InteriorUse.Shed;
                    if (area < 66f) return InteriorUse.Cottage;
                    return InteriorUse.House;
                default:
                    // building=yes from 250 to 2,500 m², or a commercial building nobody said more about: a shop, an office or a
                    // diner on a town street; a workshop, a barn or a store shed out in the country.
                    if (!town)
                    {
                        if (area < 400f) return pick < 0.45f ? InteriorUse.Workshop : pick < 0.8f ? InteriorUse.Barn : InteriorUse.Warehouse;
                        return pick < 0.35f ? InteriorUse.Workshop : InteriorUse.Warehouse;
                    }
                    if (area < 110f) return pick < 0.45f ? InteriorUse.Store : pick < 0.7f ? InteriorUse.Office : pick < 0.85f ? InteriorUse.Restaurant : InteriorUse.Salon;
                    if (area < 700f)
                        return pick < 0.38f ? InteriorUse.Store : pick < 0.6f ? InteriorUse.Office : pick < 0.74f ? InteriorUse.Restaurant
                            : pick < 0.82f ? InteriorUse.Hardware : pick < 0.9f ? InteriorUse.Bar : InteriorUse.AutoShop;
                    return pick < 0.4f ? InteriorUse.BigStore : pick < 0.62f ? InteriorUse.Store : pick < 0.82f ? InteriorUse.Office : InteriorUse.Warehouse;
            }
        }

        private static InteriorUse ByUse(string use, float area)
        {
            if (use.Length == 0) return InteriorUse.None;
            if (use.StartsWith("shop:", System.StringComparison.Ordinal))
            {
                switch (use.Substring(5))
                {
                    case "supermarket": case "department_store": case "mall": case "variety_store": case "furniture": case "wholesale":
                        return area < 300f ? InteriorUse.Store : InteriorUse.BigStore;
                    case "convenience": case "alcohol": case "kiosk": case "tobacco": case "newsagent":
                        return InteriorUse.Convenience;
                    case "chemist": return InteriorUse.Pharmacy;
                    case "doityourself": case "hardware": case "country_store": case "trade": case "paint": case "farm":
                        return InteriorUse.Hardware;
                    case "car_repair": case "tyres": case "car": case "car_parts": case "motorcycle":
                        return InteriorUse.AutoShop;
                    case "hairdresser": case "beauty": return InteriorUse.Salon;
                    case "funeral_directors": return InteriorUse.Funeral;
                    case "storage_rental": return InteriorUse.Warehouse;
                    default: return area > 900f ? InteriorUse.BigStore : InteriorUse.Store;
                }
            }
            switch (use)
            {
                case "pharmacy": return InteriorUse.Pharmacy;
                case "restaurant": case "fast_food": case "cafe": case "ice_cream": case "food_court": return InteriorUse.Restaurant;
                case "bar": case "pub": case "nightclub": case "casino": return InteriorUse.Bar;
                case "bank": case "money_lender": return InteriorUse.Bank;
                case "post_office": return InteriorUse.PostOffice;
                case "library": return InteriorUse.Library;
                case "townhall": case "courthouse": case "community_centre": case "social_facility": case "social_centre":
                case "events_venue": case "conference_centre": case "arts_centre": case "theatre": case "cinema":
                    return InteriorUse.Civic;
                case "police": case "prison": return InteriorUse.Police;
                case "fire_station": return InteriorUse.FireStation;
                case "clinic": case "doctors": case "dentist": case "veterinary": return area > 900f ? InteriorUse.Hospital : InteriorUse.Clinic;
                case "hospital": return area < 600f ? InteriorUse.Clinic : InteriorUse.Hospital;
                case "fuel": return InteriorUse.GasStation;
                case "place_of_worship": return InteriorUse.Church;
                case "school": case "university": case "college": case "kindergarten": case "childcare": return InteriorUse.School;
                case "toilets": return InteriorUse.Restrooms;
                default: return InteriorUse.None;
            }
        }

        // What a building's purpose (MapGameplay.Purposes) makes its ground floor; None for a purpose that says too little
        // (a plain commercial building), which the kind and the surroundings then decide.
        private static InteriorUse ByPurpose(string purpose, EnterableBuilding e, float area, bool town)
        {
            switch (purpose)
            {
                case MapGameplay.Purposes.Home: return area < 66f ? InteriorUse.Cottage : InteriorUse.House;
                case MapGameplay.Purposes.Outbuilding:
                    if (area >= 48f) return InteriorUse.Cottage;
                    return e.Footprint.Bounds.width > 5.5f || e.Footprint.Bounds.height > 5.5f ? InteriorUse.Garage : InteriorUse.Shed;
                case MapGameplay.Purposes.Shed: return area < 120f ? InteriorUse.Shed : InteriorUse.Workshop;
                case MapGameplay.Purposes.Garage: return area > 90f ? InteriorUse.Workshop : InteriorUse.Garage;
                case MapGameplay.Purposes.Barn: return InteriorUse.Barn;
                case MapGameplay.Purposes.Greenhouse: return InteriorUse.Greenhouse;
                case MapGameplay.Purposes.Apartments: return area < 150f ? InteriorUse.House : InteriorUse.Apartments;
                case MapGameplay.Purposes.Trailer: return InteriorUse.Trailer;
                case MapGameplay.Purposes.Hotel: return InteriorUse.Motel;
                case MapGameplay.Purposes.Grocery: return area < 300f ? InteriorUse.Store : InteriorUse.BigStore;
                case MapGameplay.Purposes.Convenience: case MapGameplay.Purposes.Liquor: return area > 600f ? InteriorUse.Store : InteriorUse.Convenience;
                case MapGameplay.Purposes.Fuel: return area > 900f ? InteriorUse.BigStore : InteriorUse.GasStation;
                case MapGameplay.Purposes.Pharmacy: return InteriorUse.Pharmacy;
                case MapGameplay.Purposes.Hardware: case MapGameplay.Purposes.FarmSupply: return InteriorUse.Hardware;
                case MapGameplay.Purposes.Auto: return InteriorUse.AutoShop;
                case MapGameplay.Purposes.Salon: return InteriorUse.Salon;
                case MapGameplay.Purposes.Guns: case MapGameplay.Purposes.Clothing: case MapGameplay.Purposes.DepartmentStore:
                case MapGameplay.Purposes.Furniture: case MapGameplay.Purposes.Electronics: case MapGameplay.Purposes.PetStore:
                case MapGameplay.Purposes.Laundry: case MapGameplay.Purposes.Shop: case MapGameplay.Purposes.Vacant:
                case MapGameplay.Purposes.GeneralStore:
                    return area > 900f ? InteriorUse.BigStore : InteriorUse.Store;
                case MapGameplay.Purposes.Restaurant: return InteriorUse.Restaurant;
                case MapGameplay.Purposes.Bar: return InteriorUse.Bar;
                case MapGameplay.Purposes.Bank: return InteriorUse.Bank;
                case MapGameplay.Purposes.PostOffice: return InteriorUse.PostOffice;
                case MapGameplay.Purposes.Police: case MapGameplay.Purposes.Prison: return InteriorUse.Police;
                case MapGameplay.Purposes.FireStation: return InteriorUse.FireStation;
                case MapGameplay.Purposes.Hospital: return area < 600f ? InteriorUse.Clinic : InteriorUse.Hospital;
                case MapGameplay.Purposes.Clinic: case MapGameplay.Purposes.Vet: case MapGameplay.Purposes.Care:
                    return area > 900f ? InteriorUse.Hospital : InteriorUse.Clinic;
                case MapGameplay.Purposes.School: case MapGameplay.Purposes.Childcare: return InteriorUse.School;
                case MapGameplay.Purposes.Church: return InteriorUse.Church;
                case MapGameplay.Purposes.Civic: case MapGameplay.Purposes.Courthouse: case MapGameplay.Purposes.Community:
                    return InteriorUse.Civic;
                case MapGameplay.Purposes.Library: return InteriorUse.Library;
                case MapGameplay.Purposes.Funeral: return InteriorUse.Funeral;
                case MapGameplay.Purposes.StorageRental: case MapGameplay.Purposes.Distillery: return InteriorUse.Warehouse;
                case MapGameplay.Purposes.Office: return InteriorUse.Office;
                case MapGameplay.Purposes.Warehouse: return town && area < 4000f && MapStyle.Unit(e.Footprint.Data.id, SaltUse) < 0.25f
                    ? InteriorUse.BigStore : InteriorUse.Warehouse;
                case MapGameplay.Purposes.Industrial:
                    return area < 250f || MapStyle.Unit(e.Footprint.Data.id, SaltUse) < 0.5f ? InteriorUse.Workshop : InteriorUse.Warehouse;
                default: return InteriorUse.None;
            }
        }

        // A town building: neighbours close round it and a proper street near it.
        private static bool InTown(Site site, Footprint f)
        {
            int near = 0;
            foreach (Footprint o in site.Area.Buildings)
                if (o != f && (o.Middle - f.Middle).sqrMagnitude < 45f * 45f) near++;
            foreach (Footprint o in site.Area.Foreign)
                if ((o.Middle - f.Middle).sqrMagnitude < 45f * 45f) near++;
            if (near < 5) return false;
            foreach (Street s in site.Area.Streets)
            {
                if (s.Rank < 2 || s.Data.tunnel) continue;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                    if (MapGeometry.DistanceToSegment(f.Middle, s.Pts[i], s.Pts[i + 1]) < 40f) return true;
            }
            return near >= 12;
        }

        private static RaidBuildingKind KindOf(InteriorUse use)
        {
            switch (use)
            {
                case InteriorUse.Cottage: case InteriorUse.House: case InteriorUse.Trailer: case InteriorUse.Apartments: case InteriorUse.Motel:
                    return RaidBuildingKind.House;
                case InteriorUse.Store: case InteriorUse.BigStore: case InteriorUse.Convenience: case InteriorUse.Hardware:
                case InteriorUse.GasStation: case InteriorUse.Salon: case InteriorUse.Restaurant: case InteriorUse.Bar:
                    return RaidBuildingKind.Store;
                case InteriorUse.Pharmacy: case InteriorUse.Clinic: case InteriorUse.Hospital: return RaidBuildingKind.Clinic;
                case InteriorUse.Shed: case InteriorUse.Garage: case InteriorUse.AutoShop: case InteriorUse.Warehouse: case InteriorUse.Workshop:
                case InteriorUse.Barn: case InteriorUse.Greenhouse: case InteriorUse.FireStation:
                    return RaidBuildingKind.Depot;
                default: return RaidBuildingKind.Office;
            }
        }

        private static string NounOf(InteriorUse use)
        {
            switch (use)
            {
                case InteriorUse.Cottage: case InteriorUse.House: return "house";
                case InteriorUse.Trailer: return "trailer";
                case InteriorUse.Apartments: return "building";
                case InteriorUse.Motel: return "motel";
                case InteriorUse.Restaurant: return "restaurant";
                case InteriorUse.Bar: return "bar";
                case InteriorUse.Bank: return "bank";
                case InteriorUse.PostOffice: return "post office";
                case InteriorUse.Library: return "library";
                case InteriorUse.Clinic: return "clinic";
                case InteriorUse.Hospital: return "hospital";
                case InteriorUse.Police: return "station";
                case InteriorUse.FireStation: return "firehouse";
                case InteriorUse.Church: return "church";
                case InteriorUse.Funeral: return "funeral home";
                case InteriorUse.School: return "school";
                case InteriorUse.Barn: return "barn";
                case InteriorUse.Shed: return "shed";
                case InteriorUse.Garage: case InteriorUse.AutoShop: return "garage";
                case InteriorUse.Warehouse: case InteriorUse.Workshop: return "building";
                case InteriorUse.Greenhouse: return "greenhouse";
                case InteriorUse.Office: case InteriorUse.Civic: return "building";
                default: return "store";
            }
        }
        #endregion

        #region The plan
        /// <summary>The ground floor of <paramref name="e"/> laid out in rooms, or null when there's nothing to lay out (no
        /// use for it, or no room-sized rectangle in its outline: such a building gets loose loot instead).</summary>
        internal static InteriorPlan MakePlan(Site site, EnterableBuilding e)
        {
            Footprint f = e.Footprint;
            if (f.Pts.Count < 3 || f.Size < 9f) return null;
            InteriorUse use = UseOf(site, e, out bool town, out string purpose);
            if (use == InteriorUse.None) return null;
            MapBuilding b = f.Data;
            var plan = new InteriorPlan
            {
                Record = e, Use = use, Purpose = purpose, Kind = KindOf(use), Noun = NounOf(use), Town = town,
                Name = "KY" + b.id, Seed = unchecked((int)MapStyle.Hash(b.id, SaltPlan)),
                Thick = e.WallThickness > 0.05f ? e.WallThickness : 0.22f,
                WallH = Mathf.Clamp((e.StoreyHeight > 1f ? e.StoreyHeight : 3f) - 0.15f, 2.4f, 4.2f),
            };
            ChooseFrame(e, out plan.Ex, out plan.Ez);
            plan.Yaw = Mathf.Atan2(plan.Ez.x, plan.Ez.y) * Mathf.Rad2Deg;
            // The frame's origin in the middle of the footprint's extent along its own axes.
            plan.Origin = f.Middle;
            var local = new List<Vector2>(f.Pts.Count);
            foreach (Vector2 p in f.Pts) local.Add(plan.ToLocal(p));
            MapGeometry.Extent(local, out float x0, out float z0, out float x1, out float z1);
            plan.Origin = plan.ToSite(new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f));
            for (int i = 0; i < f.Pts.Count; i++) local[i] = plan.ToLocal(f.Pts[i]);
            plan.Local = local;

            // Big plain sheds and halls are worth a coarser grid; the outside walls' inner faces bound the rooms.
            float margin = Mathf.Max(0.04f, plan.Thick - Half);
            List<Rect> blocks = Blocks(local, margin, 7f);
            if (blocks.Count == 0 || blocks[0].width < 2.2f || blocks[0].height < 2.2f) return null;

            foreach (Doorway d in e.Doors)
            {
                Vector2 outward = plan.DirToLocal(d.Outward).normalized;
                plan.Entrances.Add(new PlanEntrance
                {
                    At = plan.ToLocal(d.At) - outward * plan.Thick, Outward = outward, Width = d.Width > 0.5f ? d.Width : 1.2f, Room = -1,
                });
            }
            Vector2 door = plan.Entrances.Count > 0 ? plan.Entrances[0].At : new Vector2(blocks[0].center.x, blocks[0].yMin);
            var rng = new System.Random(plan.Seed);
            Lay(plan, blocks, door, rng);
            if (plan.Rooms.Count == 0) return null;
            for (int i = 0; i < plan.Rooms.Count; i++) plan.Rooms[i].Index = i;
            foreach (PlanRoom r in plan.Rooms)
                foreach (PlanRoom o in r.OpenRooms)
                    if (o.Index != r.Index && !r.OpenTo.Contains(o.Index)) r.OpenTo.Add(o.Index);

            // The rooms the outside doors open into (the nearest, for a door in a slanting wall), then the partitions.
            for (int k = 0; k < plan.Entrances.Count; k++)
            {
                PlanEntrance en = plan.Entrances[k];
                Vector2 inside = en.At - en.Outward * 0.4f;
                en.Room = plan.RoomAt(inside);
                if (en.Room < 0) en.Room = plan.NearestRoom(inside);
                plan.Entrances[k] = en;
                if (en.Room >= 0) plan.Rooms[en.Room].Entry = true;
            }
            if (plan.Entrances.Count == 0) plan.Rooms[0].Entry = true;
            Partition(plan, rng);
            Windows(plan, e);
            Style(plan, new System.Random(unchecked(plan.Seed ^ SaltStyle)));
            Budget(plan, site);
            return plan;
        }

        // The ground floor's windows in the frame, on the inner face, each with the room it lights.
        private static void Windows(InteriorPlan plan, EnterableBuilding e)
        {
            List<Vector2> p = e.Footprint.Pts;
            foreach (WindowSpan w in e.Windows)
            {
                if (w.Storey != 0 || w.Edge < 0 || w.Edge >= p.Count) continue;
                Vector2 a = p[w.Edge], c = p[(w.Edge + 1) % p.Count];
                float len = Vector2.Distance(a, c);
                if (len < 0.05f) continue;
                Vector2 d = (c - a) / len, outward = new Vector2(d.y, -d.x);
                Vector2 inset = -outward * plan.Thick;
                Vector2 la = plan.ToLocal(a + d * w.From + inset), lb = plan.ToLocal(a + d * w.To + inset);
                Vector2 lo = plan.DirToLocal(outward).normalized;
                Vector2 mid = (la + lb) * 0.5f - lo * 0.4f;
                int room = plan.RoomAt(mid);
                if (room < 0) room = plan.NearestRoom(mid);
                plan.Windows.Add(new PlanWindow { A = la, B = lb, Outward = lo, Sill = w.Sill, Head = w.Head, Room = room });
            }
        }

        // The rooms, by what the building is used as. The main block is the biggest rectangle; the others (an L's leg, a
        // wing) take what fits them.
        private static void Lay(InteriorPlan plan, List<Rect> blocks, Vector2 door, System.Random rng)
        {
            Rect m = blocks[0];
            bool east = door.x > m.center.x;
            switch (plan.Use)
            {
                case InteriorUse.Shed: Single(plan, m, RoomRole.Shed); break;
                case InteriorUse.Garage: Single(plan, m, RoomRole.HomeGarage); break;
                case InteriorUse.Greenhouse: Single(plan, m, RoomRole.Greenhouse); break;
                case InteriorUse.Restrooms: Restrooms(plan, m, east); break;
                case InteriorUse.Cottage: Cottage(plan, m, east, rng); break;
                case InteriorUse.House: House(plan, m, east, rng); break;
                case InteriorUse.Trailer: Trailer(plan, m, east, rng); break;
                case InteriorUse.Apartments: Units(plan, m, door, false, rng); break;
                case InteriorUse.Motel: Units(plan, m, door, true, rng); break;
                case InteriorUse.Restaurant: case InteriorUse.Bar: Eatery(plan, m, east, plan.Use == InteriorUse.Bar, rng); break;
                case InteriorUse.Store: case InteriorUse.BigStore: case InteriorUse.Convenience: case InteriorUse.Hardware:
                case InteriorUse.GasStation: case InteriorUse.Salon: case InteriorUse.Pharmacy:
                    Shop(plan, m, east, rng); break;
                case InteriorUse.AutoShop: AutoShop(plan, m, east, rng); break;
                case InteriorUse.Church: Church(plan, m, east, rng); break;
                case InteriorUse.School: School(plan, m, door, rng); break;
                case InteriorUse.FireStation: FireStation(plan, m, east, rng); break;
                case InteriorUse.Warehouse: case InteriorUse.Workshop: Works(plan, m, east, plan.Use == InteriorUse.Workshop ? RoomRole.Workshop : RoomRole.Warehouse, rng); break;
                case InteriorUse.Barn: Barn(plan, m, east, rng); break;
                default: Offices(plan, m, door, rng); break;
            }
            for (int i = 1; i < blocks.Count; i++) Wing(plan, blocks[i], rng);
        }

        private static PlanRoom Add(InteriorPlan plan, RoomRole role, Rect r, int unit = -1)
        {
            var room = new PlanRoom { Role = role, R = r, Kind = KindFor(role), Name = NameFor(role, plan.Use), Unit = unit };
            plan.Rooms.Add(room);
            return room;
        }

        private static void Single(InteriorPlan plan, Rect m, RoomRole role) => Add(plan, role, m);

        private static void Restrooms(InteriorPlan plan, Rect m, bool east)
        {
            if (m.width >= 4.6f) foreach (Rect r in SplitX(m, east, 2.2f, -0.5f, 0f)) Add(plan, RoomRole.Restroom, r);
            else Add(plan, RoomRole.Restroom, m);
        }
        #endregion

        #region Homes
        // A cabin: one room to live in (a bed in the corner, a kitchenette along a wall), with a bathroom behind when it's
        // deep enough for one.
        private static void Cottage(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            bool alongZ = m.height >= m.width;
            float len = alongZ ? m.height : m.width, across = alongZ ? m.width : m.height;
            if (len < 5.6f || across < 3f)
            {
                Add(plan, RoomRole.Living, m);
                return;
            }
            // The bathroom across the back (or the far end), with a bedroom beside it when there's room.
            Rect[] parts = alongZ ? SplitZ(m, true, 2.3f, 2.3f, 0f) : SplitX(m, !east, 2.3f, 2.3f, 0f);
            Rect back = parts[0], main = parts[1];
            Add(plan, RoomRole.Living, main);
            float backLen = alongZ ? back.width : back.height;
            if (backLen >= 5.2f)
            {
                Rect[] b = alongZ ? SplitX(back, east, 2.2f, 2.4f, 0f) : SplitZ(back, false, 2.2f, 2.4f, 0f);
                Add(plan, RoomRole.Bathroom, b[0]);
                Add(plan, RoomRole.Bedroom, b[1]);
            }
            else Add(plan, RoomRole.Bathroom, back);
        }

        // A house: the living room by the front door with the kitchen behind it, and the bedrooms and the bathroom off a hall
        // on the other side (a ranch, wider than deep), or down the house behind the living room (a deep lot's house); a
        // shotgun house's rooms one behind the other.
        private static void House(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height, area = w * d;
            if (area < 52f)
            {
                Cottage(plan, m, east, rng);
                return;
            }
            if (w >= d * 0.95f && w >= 7.6f) WideHouse(plan, m, east, rng);
            else if (w < 5.8f) Shotgun(plan, m, east, rng);
            else DeepHouse(plan, m, east, rng);
        }

        private static void WideHouse(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height, area = w * d;
            float pubW = Mathf.Clamp(w * (area > 150f ? 0.44f : 0.5f), 3.6f, w - 4.0f);
            Rect[] lr = SplitX(m, east, 2.6f, pubW, 0f);
            Rect pub = lr[0], priv = lr[1];

            // The public side: the living room at the front, the kitchen (and in a big house a dining room) behind.
            PlanRoom living, kitchen = null;
            if (d >= 6.2f)
            {
                Rect[] pz = SplitZ(pub, false, 2.6f, Mathf.Clamp(d * 0.55f, 3.4f, d - 2.8f), 0f);
                living = Add(plan, RoomRole.Living, pz[0]);
                if (area >= 165f && pz[1].width >= 6.6f)
                {
                    Rect[] kd = SplitX(pz[1], !east, 2.8f, -0.5f, 0f);
                    kitchen = Add(plan, RoomRole.Kitchen, kd[0]);
                    PlanRoom dining = Add(plan, RoomRole.Dining, kd[1]);
                    OpenLater(plan, dining, living);
                }
                else kitchen = Add(plan, RoomRole.Kitchen, pz[1]);
            }
            else living = Add(plan, RoomRole.Living, pub);

            // The private side: a hall down its middle with bedrooms either side and the bathroom by the hall's end; or, too
            // shallow for a hall, a bedroom and a bathroom side by side opening off the living room and the kitchen.
            if (d >= 7.2f && priv.width >= 3.4f)
            {
                float front = Mathf.Clamp((d - 1.2f) * 0.52f, 2.8f, d - 1.2f - 2.6f);
                Rect[] pz = SplitZ(priv, false, 2.4f, front, 1.2f, 0f);
                PlanRoom hall = Add(plan, RoomRole.Hall, pz[1]);
                // The hall opens straight into the room it runs into (the living room, or the kitchen behind it).
                PlanRoom into = kitchen != null && Overlap(pz[1].yMin, pz[1].yMax, kitchen.R.yMin, kitchen.R.yMax)
                    > Overlap(pz[1].yMin, pz[1].yMax, living.R.yMin, living.R.yMax) ? kitchen : living;
                OpenLater(plan, hall, into);
                Rect frontRow = pz[0], backRow = pz[2];
                if (frontRow.width >= 7.4f)
                    foreach (Rect r in SplitX(frontRow, east, 3.0f, -0.5f, 0f)) Add(plan, RoomRole.Bedroom, r);
                else Add(plan, RoomRole.Bedroom, frontRow);
                if (backRow.width >= 5.2f)
                {
                    float extra = backRow.width - 2.5f;
                    if (extra >= 6.4f && rng.NextDouble() < 0.7)
                    {
                        Rect[] b = SplitX(backRow, east, 2.4f, 2.5f, -0.45f, 0f);
                        Add(plan, RoomRole.Bathroom, b[0]);
                        Add(plan, RoomRole.Bedroom, b[1]);
                        Add(plan, rng.NextDouble() < 0.5 ? RoomRole.Den : RoomRole.Laundry, b[2]);
                    }
                    else
                    {
                        Rect[] b = SplitX(backRow, east, 2.4f, 2.5f, 0f);
                        Add(plan, RoomRole.Bathroom, b[0]);
                        Add(plan, RoomRole.Bedroom, b[1]);
                    }
                }
                else Add(plan, RoomRole.Bathroom, backRow);
            }
            else
            {
                Rect[] pz = SplitZ(priv, false, 2.2f, -0.6f, 0f);
                Add(plan, RoomRole.Bedroom, pz[0]);
                Add(plan, RoomRole.Bathroom, pz[1]);
            }
        }

        private static void DeepHouse(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float w = m.width, d = m.height;
            float frontD = Mathf.Clamp(d * 0.36f, 3.4f, 5.6f), backD = Mathf.Clamp(d * 0.3f, 3.0f, 5.0f);
            if (d - frontD - backD < 2.6f)
            {
                // Two bands only: living room and a bedroom at the front, the kitchen and the bathroom behind.
                Rect[] fb = SplitZ(m, false, 2.6f, -0.55f, 0f);
                Rect[] f = SplitX(fb[0], east, 2.8f, -0.58f, 0f);
                PlanRoom liv = Add(plan, RoomRole.Living, f[0]);
                Add(plan, RoomRole.Bedroom, f[1]);
                Rect[] k = SplitX(fb[1], east, 2.2f, -0.62f, 0f);
                Add(plan, RoomRole.Kitchen, k[0]);
                Add(plan, RoomRole.Bathroom, k[1]);
                return;
            }
            Rect[] bands = SplitZ(m, false, 2.6f, frontD, 0f, backD);
            Rect front = bands[0], mid = bands[1], back = bands[2];
            PlanRoom living;
            if (w >= 8f)
            {
                Rect[] fr = SplitX(front, east, 3f, -0.6f, 0f);
                living = Add(plan, RoomRole.Living, fr[0]);
                Add(plan, RoomRole.Bedroom, fr[1]);
            }
            else living = Add(plan, RoomRole.Living, front);
            PlanRoom kitchen;
            if (w >= 9.4f)
            {
                Rect[] bk = SplitX(back, east, 3f, -0.55f, 0f);
                kitchen = Add(plan, RoomRole.Kitchen, bk[0]);
                Add(plan, rng.NextDouble() < 0.6 ? RoomRole.Dining : RoomRole.Laundry, bk[1]);
            }
            else kitchen = Add(plan, RoomRole.Kitchen, back);
            // The hall runs down the door's side from the living room to the kitchen, the rooms off it on the far side.
            Rect[] mh = SplitX(mid, east, 1.2f, 1.2f, 0f);
            PlanRoom hall = Add(plan, RoomRole.Hall, mh[0]);
            OpenLater(plan, hall, living);
            OpenLater(plan, hall, kitchen);
            Rect rooms = mh[1];
            if (rooms.height >= 5.0f)
            {
                Rect[] rr = SplitZ(rooms, false, 2.2f, 0f, 2.4f);
                Add(plan, RoomRole.Bedroom, rr[0]);
                Add(plan, RoomRole.Bathroom, rr[1]);
            }
            else Add(plan, RoomRole.Bathroom, rooms);
        }

        // Too narrow for anything but rooms one behind the other: you walk through each to reach the next.
        private static void Shotgun(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            float d = m.height;
            if (d < 9f)
            {
                Rect[] two = SplitZ(m, false, 2.6f, -0.55f, 0f);
                Add(plan, RoomRole.Living, two[0]);
                Rect[] k = m.width >= 4.6f ? SplitX(two[1], east, 2.2f, 0f, 2.3f) : SplitZ(two[1], false, 2.2f, 0f, 2.3f);
                Add(plan, RoomRole.Kitchen, k[0]);
                Add(plan, RoomRole.Bathroom, k[1]);
                return;
            }
            Rect[] seq = SplitZ(m, false, 2.6f, Mathf.Clamp(d * 0.3f, 3.2f, 5f), Mathf.Clamp(d * 0.28f, 3f, 4.5f), 0f);
            Add(plan, RoomRole.Living, seq[0]);
            Add(plan, RoomRole.Bedroom, seq[1]);
            Rect back = seq[2];
            if (m.width >= 4.6f)
            {
                Rect[] k = SplitX(back, east, 2.2f, 0f, 2.3f);
                Add(plan, RoomRole.Kitchen, k[0]);
                Add(plan, RoomRole.Bathroom, k[1]);
            }
            else
            {
                Rect[] k = SplitZ(back, false, 2.2f, 0f, 2.3f);
                Add(plan, RoomRole.Kitchen, k[0]);
                Add(plan, RoomRole.Bathroom, k[1]);
            }
        }

        // A mobile home: the living room at the end by the door, the kitchen, the bathroom off a passage along the back wall,
        // and the bedroom at the far end.
        private static void Trailer(InteriorPlan plan, Rect m, bool east, System.Random rng)
        {
            bool alongX = m.width >= m.height;
            float len = alongX ? m.width : m.height, across = alongX ? m.height : m.width;
            if (len < 7f || across < 2.4f)
            {
                Add(plan, RoomRole.Living, m);
                return;
            }
            float liv = Mathf.Clamp(len * 0.3f, 3f, 5.5f), kit = Mathf.Clamp(len * 0.22f, 2.4f, 4f), bath = 2.4f;
            if (len - liv - kit - bath < 2.6f)
            {
                Rect[] s = alongX ? SplitX(m, east, 2.4f, -0.45f, 0f, 2.3f) : SplitZ(m, false, 2.4f, -0.45f, 0f, 2.3f);
                Add(plan, RoomRole.Living, s[0]);
                Add(plan, RoomRole.Bedroom, s[1]);
                Add(plan, RoomRole.Bathroom, s[2]);
                return;
            }
            Rect[] parts = alongX ? SplitX(m, east, 2.2f, liv, kit, bath, 0f) : SplitZ(m, false, 2.2f, liv, kit, bath, 0f);
            Add(plan, RoomRole.Living, parts[0]);
            PlanRoom kitchen = Add(plan, RoomRole.Kitchen, parts[1]);
            Rect mid = parts[2];
            PlanRoom bed = Add(plan, RoomRole.Bedroom, parts[3]);
            if (across >= 3.4f)
            {
                Rect[] mp = alongX ? SplitZ(mid, false, 1.0f, 0f, 1.1f) : SplitX(mid, !east, 1.0f, 1.1f, 0f);
                Add(plan, RoomRole.Bathroom, alongX ? mp[0] : mp[1]);
                PlanRoom hall = Add(plan, RoomRole.Hall, alongX ? mp[1] : mp[0]);
                OpenLater(plan, hall, kitchen);
                OpenLater(plan, hall, bed);
            }
            else Add(plan, RoomRole.Bathroom, mid);
        }

        // Apartments (or a motel): a corridor along the building from a lobby at the front door, the flats (or the guest
        // rooms) either side of it, each with its own door off the corridor.
        private static void Units(InteriorPlan plan, Rect m, Vector2 door, bool motel, System.Random rng)
        {
            var bays = new List<Bay>(16);
            if (Mathf.Min(m.width, m.height) < 7.4f || !Corridor(plan, m, door, RoomRole.Lobby, 3.2f, motel ? 4.4f : 7.2f, bays))
            {
                plan.Rooms.Clear();
                if (motel) Offices(plan, m, door, rng);
                else House(plan, m, door.x > m.center.x, rng);
                return;
            }
            for (int i = 0; i < bays.Count; i++) Unit(plan, bays[i], motel, i);
        }

        // One flat: the living room (with its kitchenette) on the corridor, the bedroom and bathroom beyond; or one guest room
        // with its bathroom.
        private static void Unit(InteriorPlan plan, Bay bay, bool motel, int no)
        {
            Rect u = bay.R;
            bool alongX = bay.Side == 0 || bay.Side == 2;   // the corridor runs along the bay's x sides
            float wide = alongX ? u.width : u.height, deep = alongX ? u.height : u.width;
            if (motel || wide < 5.4f || deep < 5.0f)
            {
                if (wide >= 4.4f)
                {
                    Rect[] s = alongX ? SplitX(u, false, 2.2f, 0f, 2.2f) : SplitZ(u, false, 2.2f, 0f, 2.2f);
                    Add(plan, motel ? RoomRole.Guest : RoomRole.Living, s[0], no);
                    Add(plan, RoomRole.Bathroom, s[1], no);
                }
                else Add(plan, motel ? RoomRole.Guest : RoomRole.Living, u, no);
                return;
            }
            // Across the flat: the living room on the corridor side, the bedroom and the bathroom side by side beyond.
            Rect[] across = alongX ? SplitZ(u, bay.Side == 2, 2.6f, -0.52f, 0f) : SplitX(u, bay.Side == 1, 2.6f, -0.52f, 0f);
            Add(plan, RoomRole.Living, across[0], no);
            Rect[] beyond = alongX ? SplitX(across[1], false, 2.2f, 0f, 2.4f) : SplitZ(across[1], false, 2.2f, 0f, 2.4f);
            Add(plan, RoomRole.Bedroom, beyond[0], no);
            Add(plan, RoomRole.Bathroom, beyond[1], no);
        }

        // An extra block beside the main one: a garage, a den, a bedroom, a laundry, a pantry; a workplace's stock room or office.
        private static void Wing(InteriorPlan plan, Rect r, System.Random rng)
        {
            float area = r.width * r.height, small = Mathf.Min(r.width, r.height);
            switch (plan.Use)
            {
                case InteriorUse.House: case InteriorUse.Cottage: case InteriorUse.Trailer:
                    if (area >= 18f && small >= 3.0f && !Has(plan, RoomRole.HomeGarage) && rng.NextDouble() < 0.45) Add(plan, RoomRole.HomeGarage, r);
                    else if (area >= 9f && small >= 2.6f)
                    {
                        double k = rng.NextDouble();
                        Add(plan, k < 0.35 ? RoomRole.Den : k < 0.7 ? RoomRole.Bedroom : RoomRole.Laundry, r);
                    }
                    else Add(plan, area < 6f ? RoomRole.Closet : RoomRole.Laundry, r);
                    break;
                case InteriorUse.Church: Add(plan, area >= 25f ? RoomRole.Meeting : RoomRole.Sacristy, r); break;
                case InteriorUse.School: Add(plan, area >= 30f ? RoomRole.Classroom : RoomRole.Office, r); break;
                case InteriorUse.Warehouse: case InteriorUse.Workshop: case InteriorUse.Barn: case InteriorUse.Shed: case InteriorUse.Garage:
                    Add(plan, area >= 30f ? (plan.Use == InteriorUse.Barn ? RoomRole.Barn : RoomRole.Workshop) : RoomRole.Stock, r); break;
                case InteriorUse.Restaurant: case InteriorUse.Bar: Add(plan, area >= 20f ? RoomRole.Eatery : RoomRole.Stock, r); break;
                case InteriorUse.Clinic: case InteriorUse.Hospital: Add(plan, area >= 16f ? RoomRole.Exam : RoomRole.Stock, r); break;
                case InteriorUse.Police: Add(plan, area >= 16f ? RoomRole.Office : RoomRole.Armory, r); break;
                case InteriorUse.Apartments: case InteriorUse.Motel: Add(plan, area >= 12f ? RoomRole.Laundry : RoomRole.Closet, r); break;
                case InteriorUse.Greenhouse: Add(plan, RoomRole.Greenhouse, r); break;
                default: Add(plan, area >= 14f && rng.NextDouble() < 0.5 ? RoomRole.Office : RoomRole.Stock, r); break;
            }
        }

        private static bool Has(InteriorPlan plan, RoomRole role)
        {
            foreach (PlanRoom r in plan.Rooms)
                if (r.Role == role) return true;
            return false;
        }

        private static float Overlap(float a0, float a1, float b0, float b1) => Mathf.Max(0f, Mathf.Min(a1, b1) - Mathf.Max(a0, b0));

        // Rooms aren't indexed until the plan's laid out: an open pair is recorded by reference and resolved then.
        private static void OpenLater(InteriorPlan plan, PlanRoom a, PlanRoom b)
        {
            a.OpenRooms.Add(b);
            b.OpenRooms.Add(a);
        }
        #endregion
    }
}
