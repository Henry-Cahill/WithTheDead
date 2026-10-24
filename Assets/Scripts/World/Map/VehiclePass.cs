using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's cars, as January 1993 left them (<see cref="CarKit"/>): in most driveways a sedan, a
    /// station wagon or (out in the country) a pickup, nosed up beside the house at the end of a tyre-tracked strip of
    /// packed snow; nose to tail along the kerbs downtown, here and there along the residential streets; in rows in the
    /// parking lots, thicker towards the stores; school buses lined up at the schools, a cruiser outside the police
    /// station, a car at the pumps; and out on the highways the ones that didn't get away: run off into the ditch, left on
    /// the shoulder with a door hanging open, a burnt-out shell, a short jam. Late seventies to early nineties, in the
    /// faded paints of the era, buried in snow. Kerbside cars and parking rows stand on the absolute lattice
    /// (<see cref="NaturePlan.Stations"/>), so neighbouring pieces of a streamed map agree on them. Each car has a box
    /// collider and takes its spot; a Lite site has none. The driveways are planned before the trees grow
    /// (<see cref="PlanDriveways"/>, called by <see cref="VegetationPass"/>). The drivable cars the game parks for the
    /// survivor (<see cref="VehicleModule"/>, on spots <see cref="ZoneLandmarks.PublishPoints"/> finds on the paving)
    /// still find their room: these keep to the kerbs, driveways and stalls.
    /// </summary>
    internal static partial class VehiclePass
    {
        private const float DrivewayY = 0.009f;   // the driveways' packed snow: over the bare snow and the lots, under the streets

        // Salts for the cars' random choices (MapStyle).
        private const int SaltShape = 0xCA00, SaltBody = 0xCA01, SaltPaint = 0xCA02, SaltSnow = 0xCA04, SaltWear = 0xCA05,
            SaltHasCar = 0xCA06, SaltSide = 0xCA07, SaltFace = 0xCA08, SaltJitter = 0xCA0A;

        /// <summary>One site's parking: what's been placed (and where, to keep the cars apart).</summary>
        private sealed class Parking
        {
            public readonly Site Site;
            public readonly NaturePlan Plan;
            public readonly int Max, MaxLoot;
            public int Count, Loot, Yielded;
            public readonly List<NaturePlan.Keep> Placed = new List<NaturePlan.Keep>(64);
            public readonly List<NaturePlan.Station> Stations = new List<NaturePlan.Station>(64);
            /// <summary>Where cars mustn't park: street corners (10 m), the mouths of service roads and lot entrances
            /// (5 m), crossings, bus stops, hydrants and signals.</summary>
            public readonly List<Vector2> Corners = new List<Vector2>(32), Mouths = new List<Vector2>(32), NoParking = new List<Vector2>(32);
            /// <summary>The corners where three or more roads meet, for the odd fender-bender.</summary>
            public readonly List<Junction> Crossroads = new List<Junction>(16);
            public Transform LootRoot;

            public Parking(Site site, NaturePlan plan)
            {
                Site = site;
                Plan = plan;
                Max = Mathf.Max(70, Mathf.RoundToInt(300f * site.AreaShare));
                MaxLoot = Mathf.Max(4, Mathf.RoundToInt(60f * site.AreaShare));
                foreach (Junction j in site.Junctions)
                {
                    int roads = 0, minor = 0;
                    Vector2 first = Vector2.zero, second = Vector2.zero;
                    foreach (JunctionArm a in j.Arms)
                    {
                        string kind = a.Street.Data.kind;
                        if (NaturePlan.FootKind(kind) || (a.Street.Rank == 0 && kind != "living_street")) continue;
                        if (a.Street.Rank >= 2 || kind == "living_street")
                        {
                            if (roads == 0) first = a.Dir;
                            else if (roads == 1) second = a.Dir;
                            roads++;
                        }
                        else minor++;
                    }
                    if (roads >= 3)
                    {
                        Corners.Add(j.At);
                        Crossroads.Add(j);
                    }
                    else if (roads >= 1 && roads + minor >= 3) Mouths.Add(j.At);
                    else if (roads == 2 && Vector2.Dot(first, second) > -0.86f) Corners.Add(j.At);   // a corner between two streets
                    else if (minor >= 2 && roads + minor >= 3) Mouths.Add(j.At);
                }
                foreach (Node n in site.Area.Nodes)
                    if (n.Data.kind == "crossing" || n.Data.kind == "bus_stop" || n.Data.kind == "hydrant" || n.Data.kind == "traffic_signals"
                        || n.Data.kind == "stop" || n.Data.kind == "give_way")
                        NoParking.Add(n.At);
            }

            public bool Overlaps(Vector2 at, Vector2 axis, float halfLength, float halfWidth)
            {
                foreach (NaturePlan.Keep k in Placed)
                    if (NaturePlan.Overlap(at, axis, halfLength, halfWidth, k.At, k.Axis, k.HalfLength, k.HalfWidth)) return true;
                return false;
            }

            /// <summary>True every dozen cars or so: time to let the frame go.</summary>
            public bool Due()
            {
                if (Count - Yielded < 12) return false;
                Yielded = Count;
                return true;
            }
        }

        internal static IEnumerable<string> Build(Site site)
        {
            if (site.Detail == Detail.Lite) yield break;
            NaturePlan plan = NaturePlan.For(site);
            PlanDriveways(site, plan);
            var c = new Parking(site, plan);
            string diag = "";
            int before = 0;
            foreach (string step in DrivewayCars(c)) yield return step;
            diag += "driveways " + (c.Count - before) + " of " + plan.Driveways.Count + ", "; before = c.Count;
            Specials(c);
            diag += "specials " + (c.Count - before) + ", "; before = c.Count;
            yield return "buses and cruisers";
            foreach (string step in KerbCars(c)) yield return step;
            diag += "kerbs " + (c.Count - before) + ", "; before = c.Count;
            foreach (string step in LotCars(c)) yield return step;
            diag += "lots " + (c.Count - before) + ", "; before = c.Count;
            RoadWrecks(c);
            diag += "wrecks " + (c.Count - before) + ", lootable " + c.Loot;
            if (!site.Chunked) Debug.Log("[NatureDiag] cars " + c.Count + ": " + diag + "; corners " + c.Corners.Count + ", mouths " + c.Mouths.Count);
            yield return "wrecks";
        }

        /// <summary>Kept for <see cref="StreetscapePass"/>, which calls it until it drops the call: this pass parks its own
        /// cars now (<see cref="Build"/>).</summary>
        internal static void ParkedCars(Site site) { }

        #region Looks
        // The paints of the cars on Kentucky's roads in 1993, by how common they were.
        private static readonly (Color colour, float weight)[] Paints =
        {
            (new Color(0.86f, 0.86f, 0.84f), 13f), (new Color(0.60f, 0.61f, 0.62f), 8f), (new Color(0.28f, 0.29f, 0.31f), 5f),
            (new Color(0.07f, 0.07f, 0.08f), 5f), (new Color(0.36f, 0.07f, 0.10f), 10f), (new Color(0.60f, 0.08f, 0.07f), 7f),
            (new Color(0.10f, 0.13f, 0.27f), 8f), (new Color(0.24f, 0.35f, 0.54f), 8f), (new Color(0.55f, 0.65f, 0.76f), 4f),
            (new Color(0.10f, 0.23f, 0.16f), 7f), (new Color(0.09f, 0.40f, 0.40f), 3f), (new Color(0.73f, 0.67f, 0.54f), 8f),
            (new Color(0.38f, 0.24f, 0.14f), 6f), (new Color(0.64f, 0.53f, 0.30f), 4f), (new Color(0.84f, 0.79f, 0.52f), 2f),
            (new Color(0.55f, 0.65f, 0.50f), 2f), (new Color(0.62f, 0.32f, 0.12f), 1f),
        };

        private static float s_PaintTotal;

        private static Color PaintOf(System.Random rng)
        {
            if (s_PaintTotal <= 0f)
                foreach ((Color _, float w) in Paints) s_PaintTotal += w;
            float u = (float)rng.NextDouble() * s_PaintTotal;
            foreach ((Color colour, float w) in Paints)
            {
                if (u < w) return DecorPalette.Vary(colour, 0.05f, rng);
                u -= w;
            }
            return Paints[0].colour;
        }

        /// <summary>The kinds of place a car is found in, for what's likely parked there.</summary>
        private enum Where { Town, Country, Street, Highway }

        private static CarBody BodyFor(Where where, float u)
        {
            switch (where)
            {
                case Where.Country:
                    return u < 0.38f ? CarBody.Pickup : u < 0.56f ? CarBody.Sedan : u < 0.66f ? CarBody.BigSedan : u < 0.76f ? CarBody.Suv
                        : u < 0.84f ? CarBody.Wagon : u < 0.88f ? CarBody.Hatch : u < 0.92f ? CarBody.Van : u < 0.96f ? CarBody.Minivan : CarBody.Coupe;
                case Where.Highway:
                    return u < 0.25f ? CarBody.Sedan : u < 0.37f ? CarBody.BigSedan : u < 0.55f ? CarBody.Pickup : u < 0.63f ? CarBody.Suv
                        : u < 0.71f ? CarBody.Minivan : u < 0.77f ? CarBody.Van : u < 0.85f ? CarBody.Wagon : u < 0.94f ? CarBody.Hatch
                        : u < 0.97f ? CarBody.Coupe : CarBody.BoxTruck;
                case Where.Street:
                    return u < 0.3f ? CarBody.Sedan : u < 0.44f ? CarBody.BigSedan : u < 0.54f ? CarBody.Coupe : u < 0.66f ? CarBody.Hatch
                        : u < 0.74f ? CarBody.Wagon : u < 0.88f ? CarBody.Pickup : u < 0.94f ? CarBody.Minivan : u < 0.97f ? CarBody.Suv : CarBody.Van;
                default:
                    return u < 0.3f ? CarBody.Sedan : u < 0.42f ? CarBody.BigSedan : u < 0.52f ? CarBody.Wagon : u < 0.62f ? CarBody.Hatch
                        : u < 0.7f ? CarBody.Coupe : u < 0.84f ? CarBody.Pickup : u < 0.92f ? CarBody.Minivan : u < 0.98f ? CarBody.Suv : CarBody.Van;
            }
        }

        /// <summary>A vehicle's whole look from its key: paint and trim to suit its body, snow about
        /// <paramref name="snow"/> deep (0 to 1), and its windows iced over on most that have sat a while.</summary>
        private static CarLook LookFor(long key, CarBody body, float snow, CarWear wear = CarWear.None)
        {
            System.Random rng = MapStyle.Rng(key, SaltPaint);
            var look = new CarLook { Body = body, Paint = PaintOf(rng), Wear = wear };
            float u() => (float)rng.NextDouble();
            switch (body)
            {
                case CarBody.Pickup:
                    look.TwoTone = u() < 0.35f;
                    look.Second = u() < 0.5f ? new Color(0.84f, 0.84f, 0.82f) : u() < 0.5f ? new Color(0.62f, 0.62f, 0.62f) : new Color(0.66f, 0.56f, 0.40f);
                    look.Chrome = u() < 0.75f;
                    look.Topper = u() < 0.15f;
                    if (look.Topper) look.Second = u() < 0.6f ? new Color(0.86f, 0.84f, 0.78f) : look.Paint;
                    break;
                case CarBody.Suv:
                    look.TwoTone = u() < 0.4f;
                    look.Second = u() < 0.5f ? new Color(0.84f, 0.84f, 0.82f) : new Color(0.66f, 0.56f, 0.40f);
                    look.Chrome = u() < 0.5f;
                    break;
                case CarBody.BigSedan:
                    look.Vinyl = u() < 0.35f;
                    look.Second = u() < 0.4f ? new Color(0.88f, 0.87f, 0.82f) : look.Paint * 0.6f;
                    look.Chrome = u() < 0.85f;
                    break;
                case CarBody.Coupe:
                    look.Vinyl = u() < 0.45f;
                    look.Second = u() < 0.5f ? new Color(0.88f, 0.87f, 0.82f) : look.Paint * 0.55f;
                    look.Chrome = u() < 0.7f;
                    break;
                case CarBody.Wagon:
                    look.Woodgrain = u() < 0.5f;
                    look.Chrome = u() < 0.7f;
                    break;
                case CarBody.Minivan:
                    look.Woodgrain = u() < 0.15f;
                    look.Chrome = u() < 0.1f;
                    break;
                case CarBody.Van:
                    look.Vinyl = u() < 0.4f;   // a windowed passenger van
                    look.TwoTone = u() < 0.3f;
                    look.Second = new Color(0.84f, 0.84f, 0.82f);
                    look.Chrome = u() < 0.5f;
                    break;
                case CarBody.Cruiser:
                    look.Paint = new Color(0.88f, 0.88f, 0.86f);
                    look.Second = new Color(0.10f, 0.18f, 0.45f);
                    break;
                case CarBody.BoxTruck:
                    look.Paint = u() < 0.6f ? new Color(0.86f, 0.86f, 0.84f) : look.Paint;
                    float box = u();
                    look.Second = box < 0.35f ? new Color(0.86f, 0.42f, 0.10f) : box < 0.8f ? new Color(0.90f, 0.90f, 0.88f) : new Color(0.90f, 0.74f, 0.18f);
                    look.Chrome = u() < 0.4f;
                    break;
                case CarBody.Hatch:
                    look.Chrome = u() < 0.15f;
                    break;
                default:
                    look.Chrome = u() < 0.35f;
                    break;
            }
            float s = MapStyle.Unit(key, SaltSnow);
            look.Snow = s < 0.22f ? 0.05f + s * 0.6f : Mathf.Clamp01(snow + (s - 0.6f) * 0.5f);   // a fifth brushed off or lightly dusted
            look.Frosted = MapStyle.Unit(key, SaltSnow + 1) < (look.Snow > 0.3f ? 0.6f : 0.25f);
            return look;
        }
        #endregion

        #region Parking one
        /// <summary>
        /// Parks a vehicle at <paramref name="at"/>, nose to <paramref name="yaw"/> (degrees clockwise from north),
        /// <paramref name="tilt"/> on top (a car in a ditch): drawn into the outdoor batch, a box collider round it, its
        /// spot taken, and one in five (up to the site's share) with a glovebox and a trunk to search. False, placing
        /// nothing, when it's outside the square, over the site's quota or in another's way.
        /// </summary>
        private static bool Park(Parking c, Vector2 at, float yaw, in CarLook look, long key, Quaternion tilt)
        {
            if (c.Count >= c.Max || !c.Plan.InSquare(at, 0f)) return false;
            Vector3 size = CarKit.Size(look.Body);
            Vector2 axis = Axis(yaw);
            if (c.Overlaps(at, axis, size.z * 0.5f + 0.2f, size.x * 0.5f + 0.15f)) return false;
            Vector3 at3 = To3(at, 0f);
            RaidMesh m = c.Site.Town.Out.Solid(at3);
            m.SetFrame(at3, Quaternion.Euler(0f, yaw, 0f) * tilt);
            CarKit.Draw(m, look, MapStyle.Rng(key, SaltShape));
            m.ResetFrame();
            c.Site.Town.PropCollider(at3 + Vector3.up * (size.y * 0.5f), size, yaw);
            float reach = size.z * 0.5f - size.x * 0.5f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(reach * 2f / 1.5f));
            for (int i = 0; i <= steps; i++) c.Site.Take(at + axis * Mathf.Lerp(-reach, reach, i / (float)steps), size.x * 0.5f + 0.1f);
            c.Placed.Add(new NaturePlan.Keep { At = at, Axis = axis, HalfLength = size.z * 0.5f, HalfWidth = size.x * 0.5f, Kind = NaturePlan.KeepKind.Car });
            c.Count++;
            if (tilt == Quaternion.identity) Storage(c, at3, yaw, look, key, size);
            return true;
        }

        private const int SaltLoot = 0xCA30;

        /// <summary>A car's glovebox (on the passenger side of the cab) and its trunk, a pickup's bed or a box truck's box,
        /// as the procedural town's wrecks have them: each a little storage of the game's own, rolled from the car's key.
        /// Burnt-out shells and school buses have none.</summary>
        private static void Storage(Parking c, Vector3 at, float yaw, in CarLook look, long key, Vector3 size)
        {
            if (c.Loot >= c.MaxLoot || (look.Wear & CarWear.Burnt) != 0 || look.Body == CarBody.Bus) return;
            if (MapStyle.Unit(key, SaltLoot) >= 0.2f) return;
            if (c.LootRoot == null)
            {
                c.LootRoot = new GameObject("Cars").transform;
                c.LootRoot.SetParent(c.Site.Town.World, false);
            }
            var host = new GameObject("Car").transform;
            host.SetParent(c.LootRoot, false);
            host.SetPositionAndRotation(at, Quaternion.identity);
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            float hw = size.x * 0.5f, hl = size.z * 0.5f;
            bool box = look.Body == CarBody.BoxTruck, bed = look.Body == CarBody.Pickup || box;
            Vector3 offset = c.Site.WorldOffset;   // keys by where it ends up in the world (a streamed piece is built at the origin)
            int seed = unchecked((int)MapStyle.Hash(key, SaltLoot + 1));
            if (!box)
            {
                Vector3 cab = at + q * new Vector3(hw + 0.12f, 0.95f, 0.3f);
                StorageFixture.Create(host, "Glovebox", Interactable.KeyFor("carglovebox", cab + offset), cab, null, host, seed,
                    new StorageOptions
                    {
                        Noun = "glovebox", LookLabel = "Search the glovebox", LootType = ContainerType.Desk,
                        EmptyChance = CloneZConfig.RaidGloveboxEmptyChance, MaxStacks = CloneZConfig.RaidGloveboxMaxStacks, RaidBoost = false,
                    });
                c.Site.Containers++;
            }
            bool police = look.Body == CarBody.Cruiser;
            string back = box ? "Cargo box" : bed ? "Truck bed" : "Trunk";
            Vector3 rear = at + q * new Vector3(0f, box ? 1.2f : 0.9f, -hl - 0.12f);
            StorageFixture.Create(host, back, Interactable.KeyFor("cartrunk", rear + offset), rear, null, host, seed + 1,
                new StorageOptions
                {
                    Noun = back.ToLowerInvariant(), LookLabel = box ? "Roll up the cargo door" : bed ? "Search the truck bed" : "Pop the trunk",
                    LootType = police ? ContainerType.GunLocker : box ? ContainerType.Shelf : ContainerType.Crate,
                    EmptyChance = police ? CloneZConfig.RaidTrunkEmptyChance + 0.1f : CloneZConfig.RaidTrunkEmptyChance,
                    MaxStacks = police ? 1 : CloneZConfig.RaidTrunkMaxStacks, SearchTime = CloneZConfig.RaidTrunkSearchSeconds,
                    Noise = CloneZConfig.FixtureNoiseDoor, RaidBoost = false,
                });
            c.Site.Containers++;
            c.Loot++;
        }

        /// <summary>The unit plan direction of a yaw (degrees clockwise from north).</summary>
        private static Vector2 Axis(float yaw) => new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));

        /// <summary>The yaw (degrees clockwise from north) of a plan direction.</summary>
        private static float Yaw(Vector2 dir) => Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        #endregion

        #region Driveways
        /// <summary>
        /// Plans the driveways' cars (once a site; <see cref="VegetationPass"/> calls it before planting so the yards leave
        /// them room): for most homes inside the square a car (one in six a second, an apartment block two to four) beside
        /// the house's front corner, on whichever side has room, else in front of it where the yard is deep enough; never
        /// on the sidewalk, in a wall, by the front door or on another driveway. Each car's spot and the strip of its
        /// driveway out to the sidewalk are kept (<see cref="NaturePlan.Kept"/>) and taken in the occupancy grid.
        /// </summary>
        internal static void PlanDriveways(Site site, NaturePlan plan)
        {
            if (plan.DrivewaysPlanned) return;
            plan.DrivewaysPlanned = true;
            if (plan.Lite) return;
            var doors = new Dictionary<Footprint, EnterableBuilding>();
            foreach (EnterableBuilding e in site.Enterables)
                if (e.Footprint != null) doors[e.Footprint] = e;
            foreach (Footprint f in site.Area.Buildings)
            {
                if (!NaturePlan.IsHome(f.Data) || f.Size < 30f || f.Pts.Count < 3 || !plan.InSquare(f.Middle, 0f)) continue;
                long id = f.Data.id;
                float u = MapStyle.Unit(id, SaltHasCar);
                int want = f.Data.kind == "apartments" ? 2 + MapStyle.Pick(id, SaltHasCar + 1, 3) : u < 0.18f ? 0 : u < 0.84f ? 1 : 2;
                if (want == 0) continue;
                NaturePlan.HouseFrame hf = plan.FrameOf(f);
                float first = MapStyle.Unit(id, SaltSide) < 0.5f ? 1f : -1f;
                if (Driveway(site, plan, f, hf, first, want, doors) == 0) Driveway(site, plan, f, hf, -first, want, doors);
            }
        }

        // One side of the house: the first spot that fits, then the second car behind it (or beside it), then the rest of an
        // apartment block's beside those. Returns how many it planned.
        private static int Driveway(Site site, NaturePlan plan, Footprint f, NaturePlan.HouseFrame hf, float side, int want,
            Dictionary<Footprint, EnterableBuilding> doors)
        {
            const float half = 2.6f, lane = 2.65f;
            float t = side > 0f ? hf.T1 + 1.75f : hf.T0 - 1.75f;
            float sidewalk = hf.Street ? hf.KerbN + 2.1f : float.MinValue;   // the sidewalk's far edge, as an n
            float[] spots = { hf.N0 + 1.6f, hf.N0 - 0.6f, hf.N0 - 3.3f };
            long id = f.Data.id;
            bool nose = MapStyle.Unit(id, SaltFace) < 0.72f;   // nosed up to the house, or backed in
            foreach (float n in spots)
            {
                if (n - half < sidewalk + 0.5f) continue;
                Vector2 at = hf.At(t, n);
                if (!CarFits(site, plan, f, at, hf.N, doors)) continue;
                int count = 0;
                Plan(site, plan, f, at, hf.N, nose, NaturePlan.Combine(id, 900), hf.Street ? hf.At(t, sidewalk) : at, ref count);
                for (int k = 1; k < want; k++)
                {
                    Vector2 behind = hf.At(t, n - 5.4f * k);
                    Vector2 beside = hf.At(t + side * lane * k, n);
                    long key = NaturePlan.Combine(id, 900 + k);
                    if (f.Data.kind != "apartments" && n - 5.4f * k - half >= sidewalk + 0.5f && CarFits(site, plan, f, behind, hf.N, doors))
                        Plan(site, plan, f, behind, hf.N, nose, key, Vector2.zero, ref count);
                    else if (CarFits(site, plan, f, beside, hf.N, doors))
                        Plan(site, plan, f, beside, hf.N, MapStyle.Unit(key, SaltFace) < 0.72f, key,
                            hf.Street ? hf.At(t + side * lane * k, sidewalk) : beside, ref count);
                }
                return count;
            }
            return 0;
        }

        private static void Plan(Site site, NaturePlan plan, Footprint home, Vector2 at, Vector2 n, bool nose, long key, Vector2 mouth, ref int count)
        {
            float yaw = Yaw(nose ? n : -n) + (MapStyle.Unit(key, SaltJitter) - 0.5f) * 8f;
            bool strip = mouth != Vector2.zero && mouth != at;
            plan.Driveways.Add(new NaturePlan.CarSpot { At = at, Yaw = yaw, Key = key, Home = home, Mouth = mouth, StripWidth = strip ? 2.7f : 0f });
            plan.KeepRect(at, n, 2.75f, 1.2f, NaturePlan.KeepKind.Car);
            if (strip)
            {
                Vector2 end = at + n * 2.6f;
                plan.KeepRect((mouth + end) * 0.5f, n, Vector2.Distance(mouth, end) * 0.5f, 1.4f, NaturePlan.KeepKind.Driveway);
            }
            for (int k = -1; k <= 1; k++) site.Take(at + n * (k * 1.7f), 1.1f);
            count++;
        }

        // Room for a driveway car: inside the square, off every wall, road and path, out of the water and the pitches, off
        // the other driveways, clear of the front door and porch, and, away from the house's own bounding box (which the
        // buildings take whole in the occupancy grid), free in the grid.
        private static bool CarFits(Site site, NaturePlan plan, Footprint home, Vector2 at, Vector2 axis, Dictionary<Footprint, EnterableBuilding> doors)
        {
            if (!plan.InSquare(at, 2.8f) || plan.IsKept(at, axis, 2.75f, 1.15f)) return false;
            Rect box = home.Bounds;
            for (int k = -1; k <= 1; k++)
            {
                Vector2 q = at + axis * (k * 1.7f);
                if (plan.WallDistance(q, 2f) < 1.25f || plan.RoadClearance(q, 2f) < 1.0f) return false;
                Land land = plan.LandAt(q);
                if (land == Land.Water || land == Land.Pitch || land == Land.Lot) return false;
                if (doors.TryGetValue(home, out EnterableBuilding e))
                    foreach (Doorway d in e.Doors)
                        if (Vector2.Distance(d.At + d.Outward * 1.2f, q) < 2.8f) return false;
                bool byHouse = q.x > box.xMin - 1.3f && q.x < box.xMax + 1.3f && q.y > box.yMin - 1.3f && q.y < box.yMax + 1.3f;
                if (!byHouse && !site.FreeBeside(q, 1.0f)) return false;
            }
            return true;
        }

        // The planned driveway cars, mostly snowed in, each at the end of its strip of tyre-tracked packed snow.
        private static IEnumerable<string> DrivewayCars(Parking c)
        {
            foreach (NaturePlan.CarSpot s in c.Plan.Driveways)
            {
                bool country = c.Plan.Urban(s.At) < 0.035f || s.Home.Data.kind == "trailer";
                float u = MapStyle.Unit(s.Key, SaltBody);
                CarBody body = BodyFor(country ? Where.Country : Where.Town, u);
                CarLook look = LookFor(s.Key, body, 0.62f);
                if (!Park(c, s.At, s.Yaw, look, s.Key, Quaternion.identity)) continue;
                if (s.StripWidth > 0f) Strip(c, s.Mouth, s.At + (s.At - s.Mouth).normalized * 2.6f, s.StripWidth);
                if (c.Due()) yield return "driveway cars";
            }
            yield return "driveway cars";
        }

        private static Surf PackedSnow => DecorPalette.Matte(new Color(0.76f, 0.78f, 0.82f));
        private static Surf Ruts => DecorPalette.Matte(new Color(0.58f, 0.60f, 0.63f));

        /// <summary>A driveway's strip of packed snow from <paramref name="from"/> to <paramref name="to"/>, two darker tyre
        /// ruts down it, flat on the ground.</summary>
        private static void Strip(Parking c, Vector2 from, Vector2 to, float width)
        {
            Vector2 d = to - from;
            float len = d.magnitude;
            if (len < 1f) return;
            Vector2 dir = d / len, across = new Vector2(-dir.y, dir.x);
            float yaw = Yaw(dir);
            Vector3 mid = To3((from + to) * 0.5f, DrivewayY);
            RaidMesh m = c.Site.Town.Out.Flat(mid);
            m.Flat(mid, width, len, yaw, PackedSnow);
            for (int side = -1; side <= 1; side += 2)
                m.Flat(mid + To3(across * (side * 0.78f), 0.0005f), 0.34f, len, yaw, Ruts);
        }
        #endregion
    }
}
