using System.Collections.Generic;
using UnityEngine;
using static CloneZ.ContainerType;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The real map's loot, room by room. The game fills a container from the loot table of its type (ItemDatabase,
    // ContentDatabase: the Fridge holds perishables and drinks, the Pantry dry food, the MedCabinet medicine, the
    // GunLocker firearms and ammunition, the ToolBench tools and parts, the Wardrobe clothes, the Shelf materials and
    // odds and ends, the Desk papers, books and small things, the Locker work clothes and kit, the Crate a bit of
    // everything), so a building's loot follows what it was through the types of container its rooms hold: a pharmacy's
    // sales floor is mostly medicine cabinets, a gun shop's gun lockers, a feed store's crates and tool benches.
    internal static partial class MapGameplay
    {
        private static readonly ContainerType[] NoContainers = new ContainerType[0];

        /// <summary>One purpose's containers, room by room: the plain pools, and (rich) with the rarer finds a well
        /// stocked place adds.</summary>
        private sealed class Profile
        {
            public readonly string Purpose;
            /// <summary>How many containers it holds next to an ordinary building of its size (a vacant store few, a
            /// hardware store many).</summary>
            public float Stock = 1f;
            private readonly Dictionary<string, ContainerType[]> _plain = new Dictionary<string, ContainerType[]>();
            private readonly Dictionary<string, ContainerType[]> _rich = new Dictionary<string, ContainerType[]>();
            private ContainerType[] _else, _elseRich;
            private readonly List<string> _last = new List<string>(4);

            public Profile(string purpose) { Purpose = purpose; }

            public bool HasElse => _else != null;

            public Profile Room(string room, params ContainerType[] pool) => Rooms(new[] { room }, pool);

            public Profile Rooms(string[] rooms, params ContainerType[] pool)
            {
                _last.Clear();
                foreach (string r in rooms)
                {
                    _plain[r] = pool;
                    _rich[r] = pool;
                    _last.Add(r);
                }
                return this;
            }

            /// <summary>Any other room.</summary>
            public Profile Else(params ContainerType[] pool)
            {
                _else = _elseRich = pool;
                _last.Clear();
                _last.Add(null);
                return this;
            }

            /// <summary>What the last rooms given add where the Zone is well stocked.</summary>
            public Profile Rare(params ContainerType[] extra)
            {
                foreach (string r in _last)
                {
                    if (r == null) _elseRich = Join(_else, extra);
                    else _rich[r] = Join(_plain[r], extra);
                }
                return this;
            }

            public Profile Stocked(float stock)
            {
                Stock = stock;
                return this;
            }

            public bool TryGet(string room, bool rich, out ContainerType[] pool)
                => (rich ? _rich : _plain).TryGetValue(room, out pool);

            public ContainerType[] Default(bool rich) => (rich ? _elseRich : _else) ?? NoContainers;

            private static ContainerType[] Join(ContainerType[] a, ContainerType[] b)
            {
                var all = new ContainerType[a.Length + b.Length];
                a.CopyTo(all, 0);
                b.CopyTo(all, a.Length);
                return all;
            }
        }

        private static Dictionary<string, Profile> s_Profiles;
        private static Profile s_Generic;

        private static Profile P(string purpose)
        {
            var p = new Profile(purpose);
            s_Profiles[purpose] = p;
            return p;
        }

        // Every purpose's rooms. A pool's first entry is the room's signature container (the kitchen's fridge, the
        // bathroom's medicine cabinet, the armoury's gun locker); repeats weigh more.
        private static void DefineProfiles()
        {
            s_Profiles = new Dictionary<string, Profile>();
            const string Living = RoomKinds.Living, Kitchen = RoomKinds.Kitchen, Bathroom = RoomKinds.Bathroom,
                Bedroom = RoomKinds.Bedroom, Hall = RoomKinds.Hall, Sales = RoomKinds.Sales, Stock = RoomKinds.Stock,
                Office = RoomKinds.Office, Classroom = RoomKinds.Classroom, Nave = RoomKinds.Nave, Workshop = RoomKinds.Workshop,
                Garage = RoomKinds.Garage, Storage = RoomKinds.Storage, Ward = RoomKinds.Ward, Cells = RoomKinds.Cells,
                Kitchenette = RoomKinds.Kitchenette, Open = RoomKinds.Open;
            string[] work = { Workshop, Garage }, back = { Stock, Storage };

            // What any building's rooms hold when its purpose has nothing to add (a kitchen is a kitchen).
            s_Generic = new Profile("generic")
                .Room(Living, Desk, Shelf, Wardrobe, Desk)
                .Room(Kitchen, Fridge, Pantry, Pantry, Shelf).Rare(Pantry)
                .Room(Kitchenette, Fridge, Pantry)
                .Room(Bathroom, MedCabinet)
                .Room(Bedroom, Wardrobe, Desk, Wardrobe).Rare(Locker)
                .Room(Hall, Wardrobe, Shelf)
                .Room(Sales, Shelf, Shelf, Pantry, Desk)
                .Room(Stock, Crate, Shelf, Crate, Pantry)
                .Room(Office, Desk, Desk, Shelf, Locker)
                .Room(Storage, Shelf, Crate, Locker, Crate)
                .Rooms(work, ToolBench, Shelf, Crate, ToolBench)
                .Room(Open, Crate, Shelf, Crate, Locker)
                .Room(Classroom, Desk, Shelf, Desk)
                .Room(Nave, Shelf, Desk)
                .Room(Ward, MedCabinet, Locker, MedCabinet, Desk)
                .Room(Cells)
                .Else(Shelf, Desk, Crate);

            // Homes. A Kentucky farmhouse keeps a gun cabinet and a basement of canned goods; town flats less so.
            P(Purposes.Home)
                .Room(Living, Desk, Shelf, Wardrobe, Desk).Rare(GunLocker)
                .Room(Bedroom, Wardrobe, Desk, Wardrobe).Rare(GunLocker, Locker)
                .Room(Office, Desk, Shelf, Desk).Rare(GunLocker)
                .Room(Hall, Wardrobe)
                .Rooms(work, ToolBench, Shelf, Crate, ToolBench).Rare(Fridge)
                .Room(Storage, Pantry, Shelf, Crate, ToolBench).Rare(Pantry)
                .Else(Wardrobe, Desk, Shelf, Pantry);
            P(Purposes.Trailer).Stocked(0.85f)
                .Room(Living, Desk, Shelf).Rare(GunLocker)
                .Room(Bedroom, Wardrobe, Desk).Rare(GunLocker)
                .Room(Storage, Shelf, Crate)
                .Rooms(work, ToolBench, Crate)
                .Else(Wardrobe, Desk, Pantry);
            P(Purposes.Apartments)
                .Room(Living, Desk, Shelf, Wardrobe)
                .Room(Bedroom, Wardrobe, Desk, Wardrobe).Rare(Locker)
                .Room(Storage, Shelf, Crate, Locker)
                .Room(Hall, Wardrobe)
                .Else(Wardrobe, Desk, Shelf);
            P(Purposes.Hotel)
                .Room(Bedroom, Wardrobe, Desk)
                .Room(Sales, Desk, Desk, Shelf)
                .Room(Office, Desk, Locker)
                .Room(Storage, Shelf, Shelf, Locker, Crate)
                .Room(Kitchen, Fridge, Pantry, Pantry)
                .Room(Hall, Shelf)
                .Else(Wardrobe, Desk);

            // Outbuildings and the farm.
            P(Purposes.Outbuilding).Else(ToolBench, Crate, Shelf, Crate).Rare(Fridge);
            P(Purposes.Shed).Stocked(0.7f).Else(ToolBench, Crate, Shelf);
            P(Purposes.Garage).Else(ToolBench, Shelf, Crate).Rare(Fridge);
            P(Purposes.Barn)
                .Rooms(new[] { Open, Storage }, Crate, ToolBench, Crate, Shelf).Rare(GunLocker)
                .Rooms(work, ToolBench, ToolBench, Shelf)
                .Else(Crate, ToolBench, Crate, Shelf).Rare(GunLocker);
            P(Purposes.Greenhouse).Stocked(0.6f).Else(Crate, ToolBench, Shelf);

            // Food and fuel.
            P(Purposes.Grocery).Stocked(1.3f)
                .Room(Sales, Pantry, Pantry, Fridge, Shelf, Pantry, Fridge, Desk).Rare(Pantry, Fridge)
                .Room(Stock, Crate, Pantry, Fridge, Pantry, Shelf)
                .Room(Office, Desk, Desk)
                .Else(Pantry, Shelf, Fridge);
            P(Purposes.Convenience).Stocked(1.15f)
                .Room(Sales, Pantry, Fridge, Shelf, Pantry, Desk).Rare(MedCabinet)
                .Room(Stock, Crate, Pantry, Fridge)
                .Room(Office, Desk)
                .Else(Pantry, Fridge);
            P(Purposes.GeneralStore).Stocked(1.2f)
                .Room(Sales, Pantry, Shelf, ToolBench, Pantry, Wardrobe, Desk).Rare(GunLocker)
                .Room(Stock, Crate, Pantry, Shelf, ToolBench)
                .Else(Pantry, Shelf, Crate);
            P(Purposes.Fuel).Stocked(1.1f)
                .Room(Sales, Pantry, Fridge, Shelf, Desk, Pantry).Rare(MedCabinet)
                .Room(Stock, Crate, Shelf, Pantry)
                .Rooms(work, ToolBench, Shelf, Crate, ToolBench)
                .Else(Pantry, Fridge, Shelf);
            P(Purposes.Liquor)
                .Room(Sales, Fridge, Pantry, Shelf, Desk, Pantry).Rare(Desk)
                .Room(Stock, Crate, Pantry, Fridge)
                .Else(Pantry, Fridge);
            P(Purposes.Restaurant)
                .Room(Kitchen, Fridge, Fridge, Pantry, Pantry, Shelf).Rare(Fridge)
                .Room(Sales, Desk, Pantry)
                .Rooms(back, Pantry, Fridge, Crate, Pantry)
                .Room(Office, Desk)
                .Else(Pantry, Fridge);
            P(Purposes.Bar)
                .Room(Sales, Fridge, Pantry, Desk, Fridge).Rare(GunLocker)
                .Room(Kitchen, Fridge, Pantry)
                .Rooms(back, Crate, Fridge, Pantry)
                .Room(Office, Desk)
                .Else(Fridge, Pantry);
            P(Purposes.Distillery)
                .Rooms(new[] { Open, Storage, Stock }, Crate, Crate, Pantry, Shelf)
                .Room(Sales, Pantry, Shelf, Desk)
                .Room(Office, Desk)
                .Rooms(work, ToolBench, Crate)
                .Else(Crate, Pantry);

            // Stores.
            P(Purposes.Pharmacy).Stocked(1.2f)
                .Room(Sales, MedCabinet, MedCabinet, Shelf, Pantry, MedCabinet, Desk).Rare(MedCabinet)
                .Room(Stock, MedCabinet, Crate, MedCabinet, Shelf)
                .Room(Office, Desk, MedCabinet)
                .Else(MedCabinet, Shelf, Desk);
            P(Purposes.Hardware).Stocked(1.3f)
                .Room(Sales, Shelf, ToolBench, Shelf, Crate, Shelf, Desk).Rare(ToolBench)
                .Room(Stock, Crate, Shelf, ToolBench, Crate)
                .Rooms(work, ToolBench, ToolBench, Shelf)
                .Room(Office, Desk)
                .Else(Shelf, ToolBench, Crate);
            P(Purposes.FarmSupply).Stocked(1.2f)
                .Room(Sales, Shelf, Crate, ToolBench, Pantry, Desk).Rare(GunLocker)
                .Room(Stock, Crate, Crate, Shelf)
                .Rooms(work, ToolBench, Crate)
                .Else(Crate, Shelf, ToolBench);
            P(Purposes.Guns).Stocked(1.1f)
                .Room(Sales, GunLocker, GunLocker, Shelf, Crate, Desk, Wardrobe).Rare(GunLocker)
                .Room(Stock, GunLocker, Crate, Crate, Locker)
                .Room(Office, Desk, GunLocker)
                .Rooms(work, ToolBench, GunLocker)
                .Else(GunLocker, Crate, Shelf);
            P(Purposes.Auto)
                .Rooms(work, ToolBench, ToolBench, Shelf, Crate).Rare(ToolBench)
                .Room(Sales, Shelf, ToolBench, Desk, Shelf)
                .Room(Stock, Shelf, Crate, ToolBench)
                .Room(Office, Desk, Desk)
                .Else(ToolBench, Shelf, Crate);
            P(Purposes.Clothing)
                .Room(Sales, Wardrobe, Wardrobe, Shelf, Locker, Desk).Rare(Wardrobe)
                .Room(Stock, Wardrobe, Crate, Locker)
                .Else(Wardrobe, Shelf);
            P(Purposes.DepartmentStore).Stocked(1.3f)
                .Room(Sales, Shelf, Pantry, Wardrobe, Shelf, MedCabinet, ToolBench, Desk, Fridge, Wardrobe).Rare(GunLocker)
                .Room(Stock, Crate, Shelf, Pantry, Crate)
                .Room(Office, Desk)
                .Else(Shelf, Pantry, Wardrobe);
            P(Purposes.Furniture).Stocked(0.8f)
                .Room(Sales, Wardrobe, Desk, Shelf, Wardrobe)
                .Room(Stock, Crate, Shelf)
                .Else(Wardrobe, Desk);
            P(Purposes.Electronics).Stocked(0.9f)
                .Room(Sales, Shelf, Desk, Shelf)
                .Room(Stock, Crate, Shelf)
                .Else(Shelf, Desk);
            P(Purposes.Salon).Stocked(0.8f)
                .Room(Sales, Shelf, Desk, MedCabinet)
                .Room(Stock, Shelf)
                .Else(Shelf, Desk);
            P(Purposes.PetStore)
                .Room(Sales, Pantry, Shelf, MedCabinet, Desk)
                .Room(Stock, Pantry, Crate)
                .Else(Pantry, Shelf);
            P(Purposes.Laundry).Stocked(0.8f)
                .Room(Sales, Wardrobe, Wardrobe, Shelf, Desk)
                .Else(Wardrobe, Shelf);
            P(Purposes.Shop)
                .Room(Sales, Shelf, Desk, Shelf, Locker)
                .Room(Stock, Crate, Shelf)
                .Room(Office, Desk)
                .Else(Shelf, Desk);
            P(Purposes.Vacant).Stocked(0.3f).Else(Crate);
            P(Purposes.StorageRental).Stocked(1.3f)
                .Rooms(new[] { Open, Storage, Stock }, Crate, Crate, Shelf, ToolBench, Wardrobe, Locker).Rare(GunLocker)
                .Room(Office, Desk)
                .Else(Crate, Shelf, ToolBench, Wardrobe);
            P(Purposes.Commercial)
                .Room(Sales, Shelf, Pantry, Desk, Shelf)
                .Room(Stock, Crate, Shelf, Pantry)
                .Room(Office, Desk, Shelf)
                .Else(Shelf, Desk, Crate);

            // Money, mail, offices.
            P(Purposes.Bank)
                .Room(Office, Desk, Desk, Locker).Rare(GunLocker)
                .Room(Sales, Desk, Desk)
                .Room(Storage, Locker, Locker, Desk).Rare(GunLocker)
                .Else(Desk, Locker);
            P(Purposes.PostOffice)
                .Room(Sales, Desk, Shelf)
                .Rooms(back, Crate, Shelf, Crate, Locker)
                .Room(Office, Desk, Locker)
                .Else(Crate, Shelf, Desk);
            P(Purposes.Office)
                .Room(Office, Desk, Desk, Shelf, Locker)
                .Room(Open, Desk, Desk, Shelf)
                .Room(Sales, Desk)
                .Room(Storage, Shelf, Crate)
                .Else(Desk, Shelf);

            // Public safety: the armouries and the medicine.
            P(Purposes.Police).Stocked(1.1f)
                .Room(Office, Desk, Desk, Locker, GunLocker).Rare(GunLocker)
                .Room(Storage, GunLocker, Locker, GunLocker, Crate).Rare(GunLocker)
                .Room(Cells)
                .Room(Sales, Desk, Locker)
                .Room(Hall, Locker)
                .Rooms(work, ToolBench, Locker)
                .Else(Desk, Locker, GunLocker);
            P(Purposes.FireStation).Stocked(1.1f)
                .Room(Garage, Locker, Locker, ToolBench, MedCabinet).Rare(ToolBench)
                .Room(Workshop, ToolBench, Locker)
                .Rooms(new[] { Kitchen, Kitchenette }, Fridge, Pantry, Pantry)
                .Room(Bedroom, Locker, Wardrobe)
                .Room(Office, Desk, MedCabinet)
                .Room(Storage, Shelf, ToolBench, MedCabinet, Locker)
                .Else(Locker, ToolBench, MedCabinet);
            P(Purposes.Prison)
                .Room(Cells, Locker)
                .Room(Office, Desk, GunLocker, Locker)
                .Room(Storage, GunLocker, Locker, Crate)
                .Room(Kitchen, Pantry, Fridge, Pantry)
                .Room(Ward, MedCabinet, Locker)
                .Else(Locker, Desk);
            P(Purposes.Courthouse)
                .Room(Office, Desk, Desk, Shelf, Locker).Rare(GunLocker)
                .Room(Storage, Shelf, Locker, Shelf).Rare(GunLocker)
                .Room(Cells)
                .Room(Hall, Shelf)
                .Else(Desk, Shelf);
            P(Purposes.Civic)
                .Room(Office, Desk, Desk, Shelf, Locker)
                .Room(Hall, Shelf)
                .Room(Storage, Shelf, Locker, Crate)
                .Room(Sales, Desk)
                .Else(Desk, Shelf, Locker);

            // Medicine.
            P(Purposes.Hospital).Stocked(1.25f)
                .Room(Ward, MedCabinet, MedCabinet, Locker, Desk).Rare(MedCabinet)
                .Room(Office, Desk, MedCabinet, Desk)
                .Room(Storage, MedCabinet, MedCabinet, Shelf, Locker).Rare(MedCabinet)
                .Room(Sales, Desk, MedCabinet)
                .Room(Hall, Locker)
                .Room(Kitchen, Fridge, Pantry)
                .Else(MedCabinet, Locker, Desk);
            P(Purposes.Clinic).Stocked(1.1f)
                .Room(Ward, MedCabinet, Desk, MedCabinet).Rare(MedCabinet)
                .Room(Office, Desk, MedCabinet)
                .Room(Storage, MedCabinet, Shelf)
                .Room(Sales, Desk)
                .Else(MedCabinet, Desk);
            P(Purposes.Vet)
                .Room(Ward, MedCabinet, Pantry, Desk)
                .Room(Office, Desk)
                .Room(Storage, Pantry, MedCabinet, Shelf)
                .Room(Sales, Pantry, Shelf, Desk)
                .Else(MedCabinet, Pantry);
            P(Purposes.Care)
                .Rooms(new[] { Ward, Bedroom }, Wardrobe, MedCabinet, Desk)
                .Room(Kitchen, Fridge, Pantry, Pantry)
                .Room(Office, Desk, MedCabinet)
                .Room(Storage, Shelf, Pantry, MedCabinet)
                .Else(Wardrobe, MedCabinet, Desk);

            // Schools, churches and the rest of town.
            P(Purposes.School)
                .Room(Classroom, Desk, Shelf, Desk)
                .Room(Hall, Locker, Locker)
                .Room(Kitchen, Pantry, Fridge, Pantry, Fridge)
                .Room(Office, Desk, MedCabinet, Desk)
                .Rooms(work, ToolBench, Shelf)
                .Room(Storage, Shelf, Crate, Locker)
                .Room(Open, Locker, Crate, Locker)
                .Room(Sales, Pantry)
                .Else(Desk, Locker, Shelf);
            P(Purposes.Childcare).Stocked(0.8f)
                .Room(Classroom, Shelf, Desk, Pantry)
                .Room(Kitchen, Fridge, Pantry)
                .Room(Office, Desk, MedCabinet)
                .Else(Shelf, Desk, Pantry);
            // Kentucky's churches ran the food pantry and the clothes closet.
            P(Purposes.Church).Stocked(0.8f)
                .Room(Nave, Shelf, Desk)
                .Room(Office, Desk, Shelf)
                .Room(Kitchen, Pantry, Pantry, Fridge)
                .Room(Storage, Pantry, Wardrobe, Crate, Shelf)
                .Room(Hall, Pantry, Desk)
                .Room(Classroom, Desk, Shelf)
                .Else(Shelf, Desk, Pantry);
            P(Purposes.Library).Stocked(0.9f)
                .Rooms(new[] { Open, Hall, Sales, Classroom }, Shelf, Shelf, Desk)
                .Room(Office, Desk, Shelf)
                .Room(Storage, Shelf, Crate)
                .Else(Shelf, Desk);
            P(Purposes.Community)
                .Rooms(new[] { Hall, Open }, Desk, Pantry)
                .Room(Kitchen, Fridge, Pantry, Pantry)
                .Room(Sales, Pantry, Fridge, Desk)
                .Room(Office, Desk)
                .Room(Storage, Shelf, Crate)
                .Else(Pantry, Desk, Shelf);
            P(Purposes.Funeral).Stocked(0.7f)
                .Room(Office, Desk, Desk)
                .Room(Storage, MedCabinet, Shelf, Wardrobe)
                .Rooms(new[] { Hall, Nave })
                .Else(Desk, Shelf);

            // Work.
            P(Purposes.Warehouse)
                .Room(Open, Crate, Crate, Shelf, ToolBench, Shelf).Rare(Crate)
                .Room(Office, Desk, Locker)
                .Rooms(work, ToolBench, ToolBench, Shelf)
                .Room(Storage, Crate, Shelf)
                .Else(Crate, Shelf);
            P(Purposes.Industrial)
                .Room(Open, ToolBench, Crate, Shelf, Locker, ToolBench).Rare(ToolBench)
                .Rooms(work, ToolBench, ToolBench, Shelf, Crate)
                .Room(Office, Desk, Locker)
                .Room(Storage, Crate, Shelf, Locker)
                .Else(ToolBench, Crate, Locker);

            P(Purposes.Ruin).Stocked(0.3f).Else(Crate);
            P(Purposes.None).Stocked(0f).Else();
        }

        private static Profile ProfileOf(string purpose)
        {
            if (s_Profiles == null) DefineProfiles();
            return purpose != null && s_Profiles.TryGetValue(purpose, out Profile p) ? p : s_Profiles[Purposes.Commercial];
        }

        // Rooms every building furnishes alike: one with nothing to say about them keeps the generic pools.
        private static bool Domestic(string room) => room == RoomKinds.Kitchen || room == RoomKinds.Kitchenette
            || room == RoomKinds.Bathroom || room == RoomKinds.Bedroom || room == RoomKinds.Living;

        /// <summary>The pool for a room of a building of <paramref name="purpose"/> (see <see cref="ContainersFor(MapBuilding, string)"/>);
        /// <paramref name="rich"/> adds the rarer finds of a well-stocked place. Shared arrays: don't change them.</summary>
        internal static ContainerType[] PoolFor(string purpose, string room, bool rich)
        {
            Profile p = ProfileOf(purpose);
            if (p.Stock <= 0f) return NoContainers;
            ContainerType[] pool;
            if (!string.IsNullOrEmpty(room))
            {
                if (p.TryGet(room, rich, out pool)) return pool;
                if (Domestic(room) && s_Generic.TryGet(room, rich, out pool)) return pool;
                if (room == RoomKinds.Cells) return NoContainers;
            }
            if (p.HasElse) return p.Default(rich);
            return !string.IsNullOrEmpty(room) && s_Generic.TryGet(room, rich, out pool) ? pool : s_Generic.Default(rich);
        }

        /// <summary>
        /// The kinds of container a room of a building holds, for the interiors to put in its furniture. A weighted pool:
        /// draw with <c>pool[rng.Next(pool.Length)]</c> (repeats weigh more); the first entry is the room's signature
        /// container (the kitchen's fridge, the bathroom's medicine cabinet, the bedroom's wardrobe, an armoury's gun
        /// locker), for its main piece of storage. Empty when the room holds no loot (cells, a funeral parlour's
        /// chapel, a bus shelter). <paramref name="room"/> is a <see cref="RoomKinds"/> kind; any other string gets the
        /// building's general pool. What the building was for comes from <see cref="PurposeOf(MapBuilding)"/>: its use,
        /// its name, its kind and size, and once its site has been <see cref="Prepare"/>d, the point of interest
        /// standing in it and its surroundings. A shared array: don't change it. The pool says what kinds; how many of a
        /// room's pieces hold loot is <see cref="Allot"/>'s.
        /// </summary>
        internal static ContainerType[] ContainersFor(MapBuilding b, string room) => PoolFor(PurposeOf(b), room, false);

        /// <summary>The kinds of container a building holds, whatever the room: its purpose's general pool.</summary>
        internal static ContainerType[] ContainersFor(MapBuilding b) => PoolFor(PurposeOf(b), null, false);

        /// <summary>The site-aware <see cref="ContainersFor(MapBuilding, string)"/>: the building's point of interest
        /// and surroundings for certain, and the rarer finds where the Zone is well stocked (<see cref="LootRichnessAt"/>):
        /// far from the start town and in the bigger places.</summary>
        internal static ContainerType[] ContainersFor(Site site, Footprint f, string room)
        {
            Prepare(site);
            return PoolFor(PurposeOf(site, f), room, RichnessOf(site, f) >= RichFrom);
        }

        /// <summary>Richness from which a pool adds its rarer finds.</summary>
        private const float RichFrom = 1.15f;

        /// <summary>How well stocked a building of a site is (<see cref="LootRichnessAt"/> at its middle).</summary>
        internal static float RichnessOf(Site site, Footprint f)
        {
            UseFrame(site);
            return LootRichnessAt(site.WorldOffset + To3(f.Middle, 0f));
        }

        #region How many
        // How many of a building's pieces of storage hold loot. Every loot container rolls its table afresh each raid
        // (RaidWorldRules.RollLoot: one to four draws, a quarter of the fixtures bare), so about two stacks each whatever
        // the piece, and a building's loot is its count of containers: a drawer, a cupboard door and a freezer compartment
        // count one each. A furnished building has far more of those than it should hold loot in (a raid-town kitchen
        // alone has a dozen), and the real map has many more buildings to the hectare than the raid town the Central
        // Economy's stock was set for, so the count is budgeted: each room's share by its kind and floor (RoomNeed), the
        // building's sum scaled by what it was (its purpose's stock) and how well stocked the Zone is there
        // (LootRichnessAt), within what a building of its kind and size can hold (BuildingCap, raised where the Zone is
        // richer). In the start town that's four or five for a house (the fridge and a kitchen cupboard, the bathroom
        // cabinet, a wardrobe, a desk), seven for a downtown store, thirteen for a pharmacy, eighteen or so for a
        // supermarket, eleven or more for a school; seven or eight for a house 50 km out. Bardstown's 400 m square (117
        // buildings) holds some 600 in its buildings, about 1,100 stacks.
        private const int SaltBudget = 0x6A60;

        /// <summary>How many loot containers a room of kind <paramref name="room"/> (<see cref="RoomKinds"/>) with
        /// <paramref name="area"/> m² of floor holds in an ordinary building where the Zone is ordinarily stocked: its
        /// share before the building's purpose and the Zone's richness scale it (see <see cref="Allot"/>).</summary>
        internal static float RoomNeed(string room, float area)
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

        /// <summary>The most loot containers a building of <paramref name="purpose"/> with <paramref name="area"/> m² of
        /// furnished floor holds where the Zone is ordinarily stocked (a richer Zone raises it in step: see
        /// <see cref="CapAt"/>).</summary>
        internal static int BuildingCap(string purpose, float area)
        {
            switch (purpose)
            {
                case Purposes.None: return 0;
                case Purposes.Vacant: case Purposes.Ruin: return 1;
                case Purposes.Shed: case Purposes.Greenhouse: return 2;
                case Purposes.Garage: case Purposes.Outbuilding: return 3;
                case Purposes.Barn: return 4;
                case Purposes.Trailer: return 5;
                case Purposes.Home: return Mathf.Clamp(Mathf.RoundToInt(3f + area / 40f), 4, 9);
                case Purposes.Church: case Purposes.Funeral: case Purposes.Childcare: return 6;
                case Purposes.Apartments: case Purposes.Hotel: case Purposes.Care:
                    return Mathf.Clamp(Mathf.RoundToInt(4f + area / 50f), 6, 24);
                case Purposes.Warehouse: case Purposes.Industrial: case Purposes.StorageRental: case Purposes.Distillery:
                    return Mathf.Clamp(Mathf.RoundToInt(3f + area / 300f), 4, 12);
                case Purposes.Grocery: case Purposes.DepartmentStore:
                    return Mathf.Clamp(Mathf.RoundToInt(4f + area / 60f), 6, 24);
                case Purposes.School: case Purposes.Hospital: case Purposes.Prison:
                    return Mathf.Clamp(Mathf.RoundToInt(4f + area / 150f), 8, 28);
                default: return Mathf.Clamp(Mathf.RoundToInt(4f + area / 60f), 5, 16);
            }
        }

        /// <summary>
        /// How many of each room's pieces of storage hold loot, for the interiors: <paramref name="rooms"/> are the rooms'
        /// kinds (<see cref="RoomKinds"/>) and <paramref name="areas"/> their floor areas in m², in the plan's order. Fills
        /// <paramref name="counts"/> (at least as long as <paramref name="rooms"/>) and returns the building's total, which
        /// the counts add up to. A room's count is the number of <see cref="LootContainer"/>s in it, whatever holds them:
        /// a fridge counts one and its freezer another, a dresser one per drawer, a bank of lockers one per locker. Build
        /// the room's signature piece first (the pool's first entry: <see cref="ContainersFor(Site, Footprint, string)"/>),
        /// and once the count is used up, furniture without storage (it still reads as a kitchen, but only so many things
        /// in it open). Deterministic: the same building gets the same counts in every build of its piece.
        /// </summary>
        internal static int Allot(Site site, Footprint f, IReadOnlyList<string> rooms, IReadOnlyList<float> areas, int[] counts)
        {
            Prepare(site);
            return Allot(f.Data.id, PurposeOf(site, f), RichnessOf(site, f), rooms, areas, counts);
        }

        /// <summary><see cref="Allot(Site, Footprint, IReadOnlyList{string}, IReadOnlyList{float}, int[])"/> without the
        /// site: the building's purpose from its own data (and what <see cref="Prepare"/> found for it), the Zone's
        /// richness at <paramref name="world"/>, its middle in world metres.</summary>
        internal static int Allot(MapBuilding b, Vector3 world, IReadOnlyList<string> rooms, IReadOnlyList<float> areas, int[] counts)
            => Allot(b != null ? b.id : 0L, PurposeOf(b), LootRichnessAt(world), rooms, areas, counts);

        private static int Allot(long id, string purpose, float richness, IReadOnlyList<string> rooms, IReadOnlyList<float> areas, int[] counts)
        {
            int n = rooms.Count;
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
            int total = Mathf.Min(CapAt(purpose, floor, richness), Dither(sum * ProfileOf(purpose).Stock * richness, id));
            if (total <= 0 || sum <= 0f) return 0;
            // Each room's share of the total, then the rest to the rooms nearest their next one (the first such room on a tie).
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

        /// <summary>The loot containers a building without rooms holds (a shape the interiors couldn't split): its floor
        /// worth of an ordinary open room, scaled and capped as <see cref="Allot"/> scales a building's rooms.</summary>
        internal static int LootBudget(Site site, Footprint f)
        {
            Prepare(site);
            string purpose = PurposeOf(site, f);
            float richness = RichnessOf(site, f);
            float target = Mathf.Clamp(f.Size / 45f, 1f, 7f) * ProfileOf(purpose).Stock * richness;
            return Mathf.Min(CapAt(purpose, f.Size, richness), Dither(target, f.Data.id));
        }

        /// <summary>How many of a room's pieces of storage hold loot taken on its own (a room furnished apart from its
        /// building; <see cref="Allot"/> shares a whole building's out): its <see cref="RoomNeed"/> by the building's
        /// purpose and the Zone's richness there.</summary>
        internal static int ContainerCount(Site site, Footprint f, string room, float roomArea)
        {
            Prepare(site);
            string purpose = PurposeOf(site, f);
            float richness = RichnessOf(site, f);
            float n = RoomNeed(room, roomArea) * ProfileOf(purpose).Stock * richness;
            return Mathf.Min(CapAt(purpose, Mathf.Max(roomArea, f.Size), richness), Dither(n, f.Data.id ^ HumanUtil.StableHash(room)));
        }

        /// <summary><see cref="BuildingCap"/> where the Zone's <paramref name="richness"/> is as given: raised in step where
        /// it's richer than usual, never lowered (a picked-over town holds fewer by the count, not by the cap).</summary>
        internal static int CapAt(string purpose, float area, float richness)
            => Mathf.RoundToInt(BuildingCap(purpose, area) * Mathf.Max(1f, richness));

        // A fractional count made whole, rounded up as often as its fraction says (from the building's id, so the same
        // every build).
        private static int Dither(float value, long id)
        {
            if (value <= 0f) return 0;
            int whole = Mathf.FloorToInt(value);
            return whole + (MapStyle.Unit(id, SaltBudget) < value - whole ? 1 : 0);
        }
        #endregion

        /// <summary>What a loose container of <paramref name="type"/> is called in a building of <paramref name="purpose"/>,
        /// or null for its usual name.</summary>
        internal static string ContainerName(string purpose, ContainerType type)
        {
            switch (purpose)
            {
                case Purposes.Pharmacy:
                    return type == MedCabinet ? "Pharmacy shelf" : type == Shelf ? "Drugstore shelf" : null;
                case Purposes.Grocery: case Purposes.Convenience: case Purposes.GeneralStore: case Purposes.Fuel:
                    return type == Pantry ? "Grocery shelf" : type == Fridge ? "Drinks cooler" : type == Desk ? "Checkout" : null;
                case Purposes.Liquor:
                    return type == Pantry ? "Liquor shelf" : type == Fridge ? "Beer cooler" : type == Desk ? "Counter" : null;
                case Purposes.Police:
                    return type == GunLocker ? "Armory locker" : type == Locker ? "Evidence locker" : null;
                case Purposes.Courthouse:
                    return type == GunLocker ? "Bailiff's locker" : type == Shelf ? "Records shelf" : null;
                case Purposes.Guns:
                    return type == GunLocker ? "Gun case" : type == Crate ? "Ammunition crate" : null;
                case Purposes.FireStation:
                    return type == Locker ? "Turnout locker" : type == MedCabinet ? "First-aid cabinet" : null;
                case Purposes.Hospital: case Purposes.Clinic: case Purposes.Care:
                    return type == MedCabinet ? "Supply cabinet" : null;
                case Purposes.Vet:
                    return type == MedCabinet ? "Veterinary cabinet" : type == Pantry ? "Feed shelf" : null;
                case Purposes.Hardware:
                    return type == ToolBench ? "Tool display" : type == Shelf ? "Hardware shelf" : null;
                case Purposes.FarmSupply:
                    return type == Crate ? "Seed bin" : type == Pantry ? "Feed shelf" : type == ToolBench ? "Tool rack" : null;
                case Purposes.Auto:
                    return type == ToolBench ? "Workbench" : type == Shelf ? "Parts shelf" : null;
                case Purposes.School:
                    return type == Locker ? "School locker" : type == Desk ? "Teacher's desk" : null;
                case Purposes.Church:
                    return type == Pantry ? "Food pantry shelf" : type == Wardrobe ? "Clothes closet" : null;
                case Purposes.Bank:
                    return type == Locker ? "Safe-deposit locker" : type == Desk ? "Teller's drawer" : null;
                case Purposes.PostOffice:
                    return type == Crate ? "Mail sack bin" : null;
                case Purposes.Barn:
                    return type == Crate ? "Feed bin" : type == ToolBench ? "Workbench" : type == GunLocker ? "Gun cabinet" : null;
                case Purposes.Home: case Purposes.Trailer:
                    return type == GunLocker ? "Gun cabinet" : null;
                case Purposes.Distillery:
                    return type == Crate ? "Barrel rack" : type == Pantry ? "Bottle shelf" : null;
                case Purposes.StorageRental:
                    return type == Crate ? "Storage unit" : null;
                default:
                    return null;
            }
        }

        #region Loose containers
        /// <summary>Loose containers to suit a building the interiors couldn't split into rooms: its purpose's general
        /// pool, as many as its <see cref="LootBudget"/> (about one per 45 m² of floor, scaled by how well stocked it and
        /// the Zone there are, at most seven), standing clear of the walls, of each other and of the door; drawn from
        /// <paramref name="rng"/>, the building's own stream.</summary>
        internal static void Loot(Site site, Footprint f, System.Random rng, Vector2 doorPoint)
        {
            string purpose = PurposeOf(site, f);
            Profile profile = ProfileOf(purpose);
            float richness = RichnessOf(site, f);
            ContainerType[] kinds = PoolFor(purpose, null, richness >= RichFrom);
            if (kinds.Length == 0 || profile.Stock <= 0f) return;
            int count = Mathf.Min(7, LootBudget(site, f));
            var placed = new List<Vector2>(count);
            for (int i = 0; i < count && site.Containers < MaxContainers; i++)
            {
                for (int attempt = 0; attempt < 14; attempt++)
                {
                    var at = new Vector2(Mathf.Lerp(f.Bounds.xMin, f.Bounds.xMax, (float)rng.NextDouble()),
                        Mathf.Lerp(f.Bounds.yMin, f.Bounds.yMax, (float)rng.NextDouble()));
                    if (!MapGeometry.Contains(f.Pts, at) || MapGeometry.DistanceToEdge(f.Pts, at) < 0.9f
                        || Vector2.Distance(at, doorPoint) < 2f || Near(placed, at, 1.3f)) continue;
                    // The first is the building's signature container, the rest drawn from its pool.
                    ContainerType type = i == 0 ? kinds[0] : kinds[rng.Next(kinds.Length)];
                    LootContainer box = LootContainer.Spawn(site.Loot, type, To3(at, 0f), rng.Next());
                    string name = ContainerName(purpose, type);
                    if (name != null && box != null) box.DisplayName = name;
                    placed.Add(at);
                    site.Containers++;
                    break;
                }
            }
        }
        #endregion
    }
}
