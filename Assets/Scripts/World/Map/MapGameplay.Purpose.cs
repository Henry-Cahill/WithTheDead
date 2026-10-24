using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What each real building was for, as the loot, the landmarks and the danger see it: its mapped use, else the use of
    // a point of interest standing in it, else its name, else its kind, size and surroundings. The data rarely says
    // (13,700 of 932,000 buildings carry a use; 15,000 points of interest; 17,000 names), and "commercial" is most often
    // just an untagged building of 250-2,500 m², so the surroundings decide between a town's store and a farm's barn.
    internal static partial class MapGameplay
    {
        /// <summary>What a building was for (see <see cref="PurposeOf(MapBuilding)"/>). Shared with the interiors so a
        /// building's rooms, furniture and loot agree.</summary>
        internal static class Purposes
        {
            public const string
                Home = "home", Apartments = "apartments", Trailer = "trailer", Hotel = "hotel",
                Outbuilding = "outbuilding", Shed = "shed", Garage = "garage", Barn = "barn", Greenhouse = "greenhouse",
                Grocery = "grocery", Convenience = "convenience", GeneralStore = "general_store", Fuel = "fuel",
                Liquor = "liquor", Pharmacy = "pharmacy", Hardware = "hardware", FarmSupply = "farm_supply", Guns = "guns",
                Auto = "auto", Clothing = "clothing", DepartmentStore = "department_store", Furniture = "furniture",
                Electronics = "electronics", Salon = "salon", PetStore = "pet_store", Laundry = "laundry", Shop = "shop",
                Vacant = "vacant", Restaurant = "restaurant", Bar = "bar", Bank = "bank", PostOffice = "post_office",
                Police = "police", FireStation = "fire_station", Hospital = "hospital", Clinic = "clinic", Vet = "vet",
                Care = "care", School = "school", Childcare = "childcare", Church = "church", Civic = "civic",
                Courthouse = "courthouse", Library = "library", Community = "community", Funeral = "funeral",
                Prison = "prison", StorageRental = "storage_rental", Office = "office", Commercial = "commercial",
                Warehouse = "warehouse", Industrial = "industrial", Distillery = "distillery", Ruin = "ruin", None = "none";
        }

        // A mapped use (an amenity, or "shop:<kind>") and what it makes a building.
        private static readonly Dictionary<string, string> s_UsePurpose = new Dictionary<string, string>
        {
            { "police", Purposes.Police }, { "fire_station", Purposes.FireStation }, { "hospital", Purposes.Hospital },
            { "clinic", Purposes.Clinic }, { "doctors", Purposes.Clinic }, { "dentist", Purposes.Clinic },
            { "veterinary", Purposes.Vet }, { "animal_shelter", Purposes.Vet }, { "social_facility", Purposes.Care },
            { "pharmacy", Purposes.Pharmacy }, { "shop:chemist", Purposes.Pharmacy }, { "shop:medical_supply", Purposes.Pharmacy },
            { "shop:herbalist", Purposes.Pharmacy }, { "shop:nutrition_supplements", Purposes.Pharmacy },
            { "school", Purposes.School }, { "university", Purposes.School }, { "college", Purposes.School },
            { "kindergarten", Purposes.Childcare }, { "childcare", Purposes.Childcare },
            { "place_of_worship", Purposes.Church }, { "townhall", Purposes.Civic }, { "public_building", Purposes.Civic },
            { "courthouse", Purposes.Courthouse }, { "library", Purposes.Library }, { "prison", Purposes.Prison },
            { "post_office", Purposes.PostOffice }, { "post_depot", Purposes.PostOffice }, { "bank", Purposes.Bank },
            { "shop:money_lender", Purposes.Bank }, { "bureau_de_change", Purposes.Bank },
            { "community_centre", Purposes.Community }, { "social_centre", Purposes.Community }, { "events_venue", Purposes.Community },
            { "clubhouse", Purposes.Community }, { "arts_centre", Purposes.Community }, { "conference_centre", Purposes.Community },
            { "theatre", Purposes.Community }, { "cinema", Purposes.Community }, { "stage", Purposes.Community },
            { "studio", Purposes.Office }, { "casino", Purposes.Community }, { "marketplace", Purposes.Grocery },
            { "restaurant", Purposes.Restaurant }, { "fast_food", Purposes.Restaurant }, { "cafe", Purposes.Restaurant },
            { "ice_cream", Purposes.Restaurant }, { "food_court", Purposes.Restaurant }, { "shop:coffee", Purposes.Restaurant },
            { "bar", Purposes.Bar }, { "pub", Purposes.Bar }, { "nightclub", Purposes.Bar }, { "stripclub", Purposes.Bar },
            { "biergarten", Purposes.Bar }, { "fuel", Purposes.Fuel }, { "car_wash", Purposes.Auto },
            { "car_rental", Purposes.Auto }, { "vehicle_inspection", Purposes.Auto }, { "shop:car_repair", Purposes.Auto },
            { "shop:car_parts", Purposes.Auto }, { "shop:tyres", Purposes.Auto }, { "shop:car", Purposes.Auto },
            { "shop:motorcycle", Purposes.Auto }, { "shop:truck_repair", Purposes.Auto }, { "shop:caravan", Purposes.Auto },
            { "shop:boat", Purposes.Auto }, { "shop:bicycle", Purposes.Auto },
            { "shop:supermarket", Purposes.Grocery }, { "shop:grocery", Purposes.Grocery }, { "shop:greengrocer", Purposes.Grocery },
            { "shop:butcher", Purposes.Grocery }, { "shop:deli", Purposes.Grocery }, { "shop:bakery", Purposes.Grocery },
            { "shop:pastry", Purposes.Grocery }, { "shop:confectionery", Purposes.Grocery }, { "shop:health_food", Purposes.Grocery },
            { "shop:spices", Purposes.Grocery }, { "shop:convenience", Purposes.Convenience }, { "shop:kiosk", Purposes.Convenience },
            { "shop:newsagent", Purposes.Convenience }, { "shop:general", Purposes.GeneralStore },
            { "shop:country_store", Purposes.GeneralStore }, { "shop:alcohol", Purposes.Liquor }, { "shop:wine", Purposes.Liquor },
            { "shop:beverages", Purposes.Liquor }, { "shop:doityourself", Purposes.Hardware }, { "shop:hardware", Purposes.Hardware },
            { "shop:trade", Purposes.Hardware }, { "shop:paint", Purposes.Hardware }, { "shop:tiles", Purposes.Hardware },
            { "shop:doors", Purposes.Hardware }, { "shop:lighting", Purposes.Hardware }, { "shop:tool_hire", Purposes.Hardware },
            { "shop:locksmith", Purposes.Hardware }, { "shop:gas", Purposes.Hardware }, { "shop:agrarian", Purposes.FarmSupply },
            { "shop:farm", Purposes.FarmSupply }, { "shop:garden_centre", Purposes.FarmSupply }, { "shop:groundskeeping", Purposes.FarmSupply },
            { "shop:shed", Purposes.FarmSupply }, { "shop:weapons", Purposes.Guns }, { "shop:outdoor", Purposes.Guns },
            { "shop:sports", Purposes.Guns }, { "shop:hunting", Purposes.Guns }, { "shop:military_surplus", Purposes.Guns },
            { "shop:pawnbroker", Purposes.Guns }, { "shop:fishing", Purposes.Guns },
            { "shop:clothes", Purposes.Clothing }, { "shop:shoes", Purposes.Clothing }, { "shop:second_hand", Purposes.Clothing },
            { "shop:charity", Purposes.Clothing }, { "shop:boutique", Purposes.Clothing }, { "shop:fashion_accessories", Purposes.Clothing },
            { "shop:bag", Purposes.Clothing }, { "shop:fabric", Purposes.Clothing }, { "shop:sewing", Purposes.Clothing },
            { "shop:department_store", Purposes.DepartmentStore }, { "shop:mall", Purposes.DepartmentStore },
            { "shop:variety_store", Purposes.DepartmentStore }, { "shop:wholesale", Purposes.DepartmentStore },
            { "shop:furniture", Purposes.Furniture }, { "shop:bed", Purposes.Furniture }, { "shop:carpet", Purposes.Furniture },
            { "shop:flooring", Purposes.Furniture }, { "shop:interior_decoration", Purposes.Furniture },
            { "shop:appliance", Purposes.Furniture }, { "shop:houseware", Purposes.Furniture }, { "shop:household_linen", Purposes.Furniture },
            { "shop:electronics", Purposes.Electronics }, { "shop:mobile_phone", Purposes.Electronics }, { "shop:computer", Purposes.Electronics },
            { "shop:video_games", Purposes.Electronics }, { "shop:hifi", Purposes.Electronics }, { "shop:camera", Purposes.Electronics },
            { "shop:telecommunication", Purposes.Electronics }, { "shop:video", Purposes.Electronics }, { "shop:games", Purposes.Electronics },
            { "shop:music", Purposes.Electronics }, { "shop:musical_instrument", Purposes.Electronics }, { "shop:vacuum_cleaner", Purposes.Electronics },
            { "shop:hairdresser", Purposes.Salon }, { "shop:beauty", Purposes.Salon }, { "shop:cosmetics", Purposes.Salon },
            { "shop:massage", Purposes.Salon }, { "shop:tattoo", Purposes.Salon }, { "shop:hairdresser_supply", Purposes.Salon },
            { "shop:pet", Purposes.PetStore }, { "shop:pet_grooming", Purposes.PetStore }, { "shop:laundry", Purposes.Laundry },
            { "shop:dry_cleaning", Purposes.Laundry }, { "shop:funeral_directors", Purposes.Funeral },
            { "shop:storage_rental", Purposes.StorageRental }, { "shop:vacant", Purposes.Vacant },
            { "toilets", Purposes.None }, { "shelter", Purposes.None }, { "parking", Purposes.None }, { "parking_space", Purposes.None },
            { "bench", Purposes.None }, { "shower", Purposes.None }, { "recycling", Purposes.None }, { "bus_station", Purposes.Community },
        };

        // Words in a building's (or a point's) name and what they make it, first match wins; "=" marks a whole word.
        private static readonly string[][] s_NameWords =
        {
            new[] { Purposes.Funeral, "funeral", "mortuary" },
            new[] { Purposes.Vet, "animal hospital", "veterinary", "animal clinic", "vet clinic" },
            new[] { Purposes.Hotel, "=inn", "hotel", "motel", "suites", "bed and breakfast", "b&b" },
            new[] { Purposes.Police, "police", "sheriff", "state trooper", "national guard", "=armory" },
            new[] { Purposes.Prison, "detention", "correctional", "penitentiary", "=jail" },
            new[] { Purposes.FireStation, "fire station", "fire department", "fire dept", "firehouse", "fire protection", "volunteer fire", "fire & rescue", "fire and rescue" },
            new[] { Purposes.Care, "nursing", "assisted living", "rehabilitation", "retirement", "care center", "convalescent" },
            new[] { Purposes.Hospital, "hospital", "medical center", "medical centre", "regional medical" },
            new[] { Purposes.Clinic, "clinic", "health department", "health center", "dental", "dentist", "family medicine", "physician", "urgent care", "pediatric", "chiropract", "orthodont", "optometr", "medical" },
            new[] { Purposes.Pharmacy, "pharmacy", "=drug", "=drugs", "drugstore", "apothecary" },
            new[] { Purposes.Childcare, "daycare", "day care", "child care", "childcare", "preschool", "head start", "learning center" },
            new[] { Purposes.School, "elementary", "middle school", "high school", "school", "academy", "college", "university", "seminary" },
            new[] { Purposes.Courthouse, "courthouse", "court house", "judicial", "justice center", "fiscal court" },
            new[] { Purposes.Civic, "city hall", "town hall", "government", "municipal", "county clerk", "annex", "water company", "utilities" },
            new[] { Purposes.PostOffice, "post office", "usps" },
            new[] { Purposes.Library, "library", "museum", "archives", "historical society" },
            new[] { Purposes.Church, "church", "chapel", "baptist", "methodist", "presbyterian", "catholic", "cathedral", "tabernacle", "ministries", "ministry", "parish", "lutheran", "episcopal", "pentecostal", "assembly of god", "kingdom hall", "synagogue", "mosque", "temple", "=abbey", "monastery", "fellowship" },
            new[] { Purposes.Bank, "=bank", "credit union", "savings", "=loan", "=loans", "finance" },
            new[] { Purposes.StorageRental, "storage" },
            new[] { Purposes.Distillery, "distillery", "distilling", "distillers", "rickhouse", "bourbon" },
            new[] { Purposes.FarmSupply, "feed and seed", "feed & seed", "=feed", "=seed", "farm supply", "southern states", "=co-op", "tractor", "farm and garden", "garden center" },
            new[] { Purposes.Hardware, "hardware", "lumber", "building supply", "home center", "home improvement", "=lowe's", "home depot", "paint" },
            new[] { Purposes.Guns, "=gun", "=guns", "firearm", "=pawn", "sporting goods", "outdoors", "archery", "bait", "hunting", "army surplus" },
            new[] { Purposes.Liquor, "liquor", "=package", "wine", "spirits", "beverage" },
            new[] { Purposes.DepartmentStore, "walmart", "wal-mart", "kmart", "k-mart", "dollar general", "family dollar", "dollar tree", "=dollar", "department store", "=mall", "variety", "five and dime" },
            new[] { Purposes.Fuel, "truck stop", "travel center", "travel plaza", "gas station", "service station", "filling station", "marathon", "chevron", "exxon", "citgo", "sunoco", "thorntons", "speedway", "=bp", "=shell", "=texaco", "=gulf" },
            new[] { Purposes.Convenience, "food mart", "foodmart", "convenience", "quick stop", "minute mart", "=mart", "=stop" },
            new[] { Purposes.Grocery, "grocery", "supermarket", "kroger", "=iga", "save-a-lot", "food lion", "piggly wiggly", "=foods", "meat", "bakery", "=market", "produce" },
            new[] { Purposes.Auto, "=auto", "automotive", "=tire", "=tires", "=motor", "=motors", "collision", "body shop", "towing", "car wash", "dealership", "=garage", "transmission", "muffler" },
            new[] { Purposes.Restaurant, "restaurant", "=cafe", "café", "diner", "=grill", "pizza", "kitchen", "bbq", "barbecue", "steak", "burger", "mcdonald", "dairy queen", "taco", "chicken", "donut", "doughnut", "coffee", "buffet", "bistro", "eatery", "creamery" },
            new[] { Purposes.Bar, "tavern", "=pub", "saloon", "lounge", "tap house", "taproom", "brewery", "=bar", "nightclub", "night club" },
            new[] { Purposes.Community, "theater", "theatre", "cinema", "community center", "civic center", "recreation", "ymca", "masonic", "=lodge", "=legion", "=vfw", "fairground", "=arena", "stadium", "=gym", "fitness", "=hall" },
            new[] { Purposes.Apartments, "apartment", "=apts", "housing", "townhome", "condominium" },
            new[] { Purposes.Salon, "salon", "barber", "beauty", "=nails", "=hair", "=spa", "tattoo" },
            new[] { Purposes.Clothing, "clothing", "boutique", "consign", "thrift", "goodwill", "=shoes", "apparel", "fashion", "bridal", "western wear" },
            new[] { Purposes.Furniture, "furniture", "mattress", "appliance", "carpet", "flooring" },
            new[] { Purposes.Laundry, "laundr", "cleaners", "dry clean" },
            new[] { Purposes.PetStore, "pet supply", "pet supplies", "pet store", "pet shop", "=pets" },
            new[] { Purposes.Electronics, "electronics", "radio shack", "computer", "video", "music", "=records" },
            new[] { Purposes.Warehouse, "warehouse", "distribution", "freight", "logistics" },
            new[] { Purposes.Industrial, "industries", "manufactur", "=plant", "factory", "=mill", "=works", "foundry", "quarry", "lumber yard" },
            new[] { Purposes.Office, "offices", "=office", "insurance", "state farm", "farm bureau", "realty", "real estate", "attorney", "=law", "newspaper", "accounting", "=radio", "agency" },
            new[] { Purposes.Barn, "=barn", "=farm", "farms", "stable", "=dairy", "orchard" },
            new[] { Purposes.Shop, "gallery", "gifts", "antique", "jewel", "florist", "flowers", "books", "=shop", "store", "outlet", "=sales" },
        };

        // Kinds of building whose kind says more than any point of interest in them.
        private static bool StrongKind(string kind) => kind == "church" || kind == "school" || kind == "hospital"
            || kind == "trailer" || kind == "garage" || kind == "shed" || kind == "barn" || kind == "silo"
            || kind == "greenhouse" || kind == "canopy" || kind == "ruin";

        /// <summary>What a mapped use (an amenity or "shop:&lt;kind&gt;") makes a building, or null when it says nothing.</summary>
        internal static string PurposeOfUse(string use)
        {
            if (string.IsNullOrEmpty(use)) return null;
            if (s_UsePurpose.TryGetValue(use, out string p)) return p;
            return use.StartsWith("shop:", System.StringComparison.Ordinal) ? Purposes.Shop : null;
        }

        private struct NameWord { public string Word, Purpose; public bool Whole; }

        private static NameWord[] s_Words;

        /// <summary>What a name suggests a building was for, or null.</summary>
        internal static string PurposeOfName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (s_Words == null)
            {
                var words = new List<NameWord>(256);
                foreach (string[] row in s_NameWords)
                    for (int k = 1; k < row.Length; k++)
                    {
                        bool whole = row[k][0] == '=';
                        words.Add(new NameWord { Purpose = row[0], Whole = whole, Word = whole ? row[k].Substring(1) : row[k] });
                    }
                s_Words = words.ToArray();
            }
            string lower = name.ToLowerInvariant();
            for (int i = 0; i < s_Words.Length; i++)
                if (HasWord(lower, s_Words[i].Word, s_Words[i].Whole)) return s_Words[i].Purpose;
            return null;
        }

        // A keyword in a lower-case name: anywhere, or (whole) not run into other letters.
        private static bool HasWord(string lower, string word, bool whole)
        {
            int at = lower.IndexOf(word, System.StringComparison.Ordinal);
            if (!whole) return at >= 0;
            while (at >= 0)
            {
                bool before = at == 0 || !char.IsLetter(lower[at - 1]);
                bool after = at + word.Length >= lower.Length || !char.IsLetter(lower[at + word.Length]);
                if (before && after) return true;
                at = at + 1 < lower.Length ? lower.IndexOf(word, at + 1, System.StringComparison.Ordinal) : -1;
            }
            return false;
        }

        // Purposes found for the buildings of sites prepared before their interiors were furnished (Prepare), by building
        // id. Only Prepare fills it, never a later pass, so a building is classed the same way in every build of its piece.
        private static readonly Dictionary<long, string> s_Prepared = new Dictionary<long, string>();
        private static object s_PreparedEpoch;

        /// <summary>What <paramref name="b"/> was for: as found by <see cref="Prepare"/> (the point of interest standing in
        /// it, its surroundings) when its site was prepared, else from its own data alone (<see cref="Classify"/>).</summary>
        internal static string PurposeOf(MapBuilding b)
        {
            if (b == null) return Purposes.Commercial;
            return s_Prepared.TryGetValue(b.id, out string known) ? known : Classify(b);
        }

        /// <summary>What a building was for from its own data: its use, its name, else its kind and size.</summary>
        internal static string Classify(MapBuilding b)
        {
            string kind = b.kind ?? "";
            switch (kind)
            {
                case "church": return Purposes.Church;
                case "school": return b.use == "kindergarten" || b.use == "childcare" ? Purposes.Childcare : Purposes.School;
                case "hospital": return Purposes.Hospital;
                case "trailer": return Purposes.Trailer;
                case "garage": return Purposes.Garage;
                case "shed": return Purposes.Shed;
                case "barn": case "silo": return Purposes.Barn;
                case "greenhouse": return Purposes.Greenhouse;
                case "ruin": return Purposes.Ruin;
                case "canopy": return b.use == "fuel" ? Purposes.Fuel : Purposes.None;
            }
            string p = PurposeOfUse(b.use) ?? PurposeOfName(b.name);
            if (p != null) return p;
            switch (kind)
            {
                case "house":
                    float size = FootprintArea(b.pts);
                    return size < 30f ? Purposes.Shed : size < 60f ? Purposes.Outbuilding : Purposes.Home;
                case "apartments": return Purposes.Apartments;
                case "hotel": return Purposes.Hotel;
                case "office": return Purposes.Office;
                case "civic": return Purposes.Civic;
                case "industrial": return Purposes.Industrial;
                case "warehouse": return Purposes.Warehouse;
                default: return Purposes.Commercial;
            }
        }

        /// <summary>A footprint's area in m² from its mapped outline (decimetres).</summary>
        internal static float FootprintArea(int[] pts)
        {
            if (pts == null || pts.Length < 6) return 0f;
            int n = pts.Length / 2;
            double s = 0;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                s += (double)pts[2 * i] * pts[2 * j + 1] - (double)pts[2 * j] * pts[2 * i + 1];
            }
            return (float)(System.Math.Abs(s) * 0.5 * 0.01);
        }

        #region A site's buildings, read together
        /// <summary>What the gameplay layer reads off one site: each footprint's purpose and display name, and the points
        /// of interest it matched to them.</summary>
        private sealed class SiteInfo
        {
            public readonly Dictionary<Footprint, string> Purpose = new Dictionary<Footprint, string>();
            /// <summary>The name a building goes by: its own, else that of the point of interest standing in it.</summary>
            public readonly Dictionary<Footprint, string> Name = new Dictionary<Footprint, string>();
            /// <summary>The points of interest matched to a building (they're its, not a landmark of their own).</summary>
            public readonly Dictionary<Spot, Footprint> HousedIn = new Dictionary<Spot, Footprint>();
        }

        private static readonly ConditionalWeakTable<Site, SiteInfo> s_Sites = new ConditionalWeakTable<Site, SiteInfo>();

        /// <summary>Reads <paramref name="site"/>'s buildings for what they were before their interiors are furnished, so
        /// <see cref="ContainersFor(MapBuilding, string)"/> knows each building's point of interest and surroundings, the
        /// same in every build of the site. Call it before the interiors pass (it's cheap and does it once per site); the
        /// site-aware overloads call it themselves.</summary>
        internal static void Prepare(Site site)
        {
            UseFrame(site);
            SiteInfo info = Analyse(site);
            object epoch = Epoch(site);
            if (!ReferenceEquals(epoch, s_PreparedEpoch))
            {
                s_Prepared.Clear();
                s_PreparedEpoch = epoch;
            }
            foreach (KeyValuePair<Footprint, string> kv in info.Purpose) s_Prepared[kv.Key.Data.id] = kv.Value;
        }

        // A raid's map: a streamed one is its streamer's, a fixed one its town's. Whatever a raid learns is forgotten
        // with it.
        private static object Epoch(Site site) => KentuckyStreamer.Active != null ? (object)KentuckyStreamer.Active : site.Town.Info;

        /// <summary>Forgets everything the gameplay layer learned of a raid's map (what its buildings were for, its density,
        /// its landmarks, its frame): when a new map starts, and when a streamed map goes, so a finished raid's map isn't held
        /// on to.</summary>
        internal static void ForgetMap()
        {
            s_Prepared.Clear();
            s_PreparedEpoch = null;
            ForgetDensity();
            ForgetLandmarks();
        }

        /// <summary>The purpose of one of <paramref name="site"/>'s buildings, with its point of interest and surroundings.</summary>
        internal static string PurposeOf(Site site, Footprint f)
            => Analyse(site).Purpose.TryGetValue(f, out string p) ? p : PurposeOf(f.Data);

        /// <summary>The name a building goes by (its own, else its point of interest's), or null.</summary>
        internal static string NameOf(Site site, Footprint f)
            => Analyse(site).Name.TryGetValue(f, out string n) ? n : (string.IsNullOrEmpty(f.Data.name) ? null : f.Data.name);

        private static SiteInfo Analyse(Site site)
        {
            if (s_Sites.TryGetValue(site, out SiteInfo info)) return info;
            info = new SiteInfo();
            List<Footprint> buildings = site.Area.Buildings;
            var poi = new Dictionary<Footprint, (string purpose, string name, int rank)>();
            foreach (Spot s in site.Area.Points)
            {
                string p = PurposeOfUse(s.Data.kind);
                if (p == null || p == Purposes.None) continue;
                Footprint home = HouseOf(buildings, s.At);
                if (home == null) continue;
                int rank = PoiRank(p);
                info.HousedIn[s] = home;
                if (poi.TryGetValue(home, out var had) && had.rank >= rank) continue;
                poi[home] = (p, string.IsNullOrEmpty(s.Data.name) ? null : s.Data.name, rank);
            }
            var grid = new Dictionary<long, int>();
            foreach (Footprint f in buildings)
            {
                long key = GridKey(f.Middle, 40f);
                grid[key] = grid.TryGetValue(key, out int c) ? c + 1 : 1;
            }
            foreach (Footprint f in buildings)
            {
                MapBuilding b = f.Data;
                string purpose = null;
                if (!StrongKind(b.kind)) purpose = PurposeOfUse(b.use);
                if (purpose == null && !StrongKind(b.kind) && poi.TryGetValue(f, out var found)) purpose = found.purpose;
                if (purpose == null) purpose = Classify(b);
                // An untagged big building far out in the country is a barn, a poultry house or a machine shed, not a
                // store: few neighbours and no town around it.
                if ((purpose == Purposes.Commercial || purpose == Purposes.Warehouse) && string.IsNullOrEmpty(b.name)
                    && Neighbours(grid, f.Middle) < 6 && SettlementAt(site, f.Middle) < 0.12f)
                    purpose = Purposes.Barn;
                // So is an untagged "house" out there that's long and wide (a tobacco barn, a machine shed): country homes
                // are compact, and trailers far narrower.
                else if (purpose == Purposes.Home && string.IsNullOrEmpty(b.name) && string.IsNullOrEmpty(b.use) && b.levels <= 1
                    && Neighbours(grid, f.Middle) < 6 && SettlementAt(site, f.Middle) < 0.12f && BarnShaped(f.Pts))
                    purpose = Purposes.Barn;
                info.Purpose[f] = purpose;
                string name = !string.IsNullOrEmpty(b.name) ? b.name : poi.TryGetValue(f, out var named) ? named.name : null;
                if (name != null) info.Name[f] = name;
            }
            s_Sites.Add(site, info);
            return info;
        }

        // A footprint shaped like a farm building: its tightest enclosing rectangle (along one of its own edges) at least 9 m
        // wide (wider than a double-wide), 2.2 times as long, 150 m² or more.
        private static bool BarnShaped(List<Vector2> pts)
        {
            int n = pts.Count;
            if (n < 3) return false;
            float bestArea = float.MaxValue, shortSide = 0f, longSide = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 e = pts[(i + 1) % n] - pts[i];
                float len = e.magnitude;
                if (len < 0.5f) continue;
                Vector2 u = e / len, v = new Vector2(-u.y, u.x);
                float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue;
                for (int k = 0; k < n; k++)
                {
                    float a = Vector2.Dot(pts[k], u), b = Vector2.Dot(pts[k], v);
                    if (a < u0) u0 = a;
                    if (a > u1) u1 = a;
                    if (b < v0) v0 = b;
                    if (b > v1) v1 = b;
                }
                float w = u1 - u0, h = v1 - v0;
                if (w * h >= bestArea) continue;
                bestArea = w * h;
                shortSide = Mathf.Min(w, h);
                longSide = Mathf.Max(w, h);
            }
            return bestArea >= 150f && bestArea < float.MaxValue && shortSide >= 9f && longSide >= shortSide * 2.2f;
        }

        // The building a point of interest stands in, else the nearest within 8 m of it (points are often mapped at the
        // door or on the lot).
        private static Footprint HouseOf(List<Footprint> buildings, Vector2 at)
        {
            Footprint best = null;
            float bestDist = 8f;
            foreach (Footprint f in buildings)
            {
                Rect r = f.Bounds;
                if (at.x < r.xMin - 8f || at.x > r.xMax + 8f || at.y < r.yMin - 8f || at.y > r.yMax + 8f) continue;
                if (MapGeometry.Contains(f.Pts, at)) return f;
                float d = MapGeometry.DistanceToEdge(f.Pts, at);
                if (d < bestDist) { bestDist = d; best = f; }
            }
            return best;
        }

        // Which of several points of interest in one building names it: the one that matters most to a survivor.
        private static int PoiRank(string purpose)
        {
            switch (purpose)
            {
                case Purposes.Police: case Purposes.Hospital: case Purposes.FireStation: return 9;
                case Purposes.Guns: case Purposes.Pharmacy: case Purposes.Clinic: return 8;
                case Purposes.Grocery: case Purposes.DepartmentStore: case Purposes.Hardware: case Purposes.FarmSupply: return 7;
                case Purposes.Fuel: case Purposes.Convenience: case Purposes.GeneralStore: case Purposes.Liquor: case Purposes.Auto: return 6;
                case Purposes.School: case Purposes.Church: case Purposes.Courthouse: case Purposes.Civic: case Purposes.PostOffice: return 5;
                case Purposes.Restaurant: case Purposes.Bar: case Purposes.Bank: return 4;
                default: return 2;
            }
        }

        private static long GridKey(Vector2 p, float cell) => ((long)Mathf.FloorToInt(p.x / cell) << 32) | (uint)Mathf.FloorToInt(p.y / cell);

        private static int Neighbours(Dictionary<long, int> grid, Vector2 p)
        {
            int cx = Mathf.FloorToInt(p.x / 40f), cz = Mathf.FloorToInt(p.y / 40f), n = 0;
            for (int z = cz - 1; z <= cz + 1; z++)
                for (int x = cx - 1; x <= cx + 1; x++)
                    if (grid.TryGetValue(((long)x << 32) | (uint)z, out int c)) n += c;
            return n;
        }
        #endregion
    }
}
