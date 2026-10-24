using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's cars, as January 1993 left them (<see cref="CarKit"/>): at the house end of most driveways a
    /// sedan, a station wagon or (out in the country) a pickup, snowed in, on the driveways the roads laid
    /// (<see cref="RoadPass.Driveways"/>) and, by the homes they didn't reach, at the end of a strip of packed snow of this
    /// pass's own; in the stalls painted on the parking lots (<see cref="GroundPass.Stalls"/>), thicker by the stores, and
    /// nosed up to the edge of the small lots that have none; along the kerbs downtown and here and there along the
    /// residential streets, clear of the junctions, crosswalks, hydrants, bus stops and driveway aprons; school buses lined
    /// up at the schools, a cruiser outside the police station, a car or two on a gas station's forecourt; now and then a
    /// rear-ended pair left at a town stop line; and out on the highways the ones that didn't get away: run off into the
    /// ditch, left on the shoulder with a door hanging open, a burnt-out shell, a short jam. Late seventies to early
    /// nineties, in the faded paints of the era, buried in snow. Every choice comes from a key of the car's own place or
    /// feature (kerbs and highways on the absolute lattice, <see cref="NaturePlan.Stations"/>; stalls and driveways by
    /// where they are), so neighbouring pieces of a streamed map agree, and a site over its limit is thinned evenly,
    /// never by the order its streets come in. Each car has a box collider, takes its spot and keeps off the walls; one in
    /// five has a glovebox (with the horn and the ignition, as the procedural town's wrecks have) and a trunk to search,
    /// the game's own storage (<see cref="StorageFixture"/>), whose contents a streamed piece remembers like any
    /// container's. A Lite site has none. The driveways are planned before the trees grow (<see cref="PlanDriveways"/>,
    /// called by <see cref="VegetationPass"/>). The game's drivable cars (<see cref="VehicleModule"/>) are set down later on
    /// spots <see cref="ZoneLandmarks.PublishPoints"/> finds clear of every collider, these cars' among them.
    /// </summary>
    internal static partial class VehiclePass
    {
        private const float DrivewayY = 0.009f;   // the driveways' packed snow: over the bare snow and the lots, under the streets

        // Salts for the cars' random choices (MapStyle).
        private const int SaltShape = 0xCA00, SaltBody = 0xCA01, SaltPaint = 0xCA02, SaltSnow = 0xCA04, SaltWear = 0xCA05,
            SaltHasCar = 0xCA06, SaltSide = 0xCA07, SaltFace = 0xCA08, SaltJitter = 0xCA0A, SaltThin = 0xCA0B, SaltHorn = 0xCA0C;

        /// <summary>A car found while a step looks for room, parked (or not) once the step knows how many there are.</summary>
        private struct Candidate
        {
            public Vector2 At;
            public float Yaw, Snow;
            public long Key;
            public CarBody Body;
            public CarWear Wear;
        }

        /// <summary>One site's parking: what's been placed (and where, to keep the cars apart).</summary>
        private sealed class Parking
        {
            public readonly Site Site;
            public readonly NaturePlan Plan;
            public readonly int Max, MaxLoot;
            public int Count, Loot;
            public readonly List<NaturePlan.Keep> Placed = new List<NaturePlan.Keep>(64);
            public readonly List<NaturePlan.Station> Stations = new List<NaturePlan.Station>(64);
            public readonly List<Candidate> Found = new List<Candidate>(128);
            /// <summary>Where cars mustn't park: round the street corners (past the junction's paving and crosswalks), at
            /// the mouths of service roads and lot entrances; and near crossings, bus stops, hydrants and signals.</summary>
            public readonly List<(Vector2 at, float within)> Corners = new List<(Vector2, float)>(32), Mouths = new List<(Vector2, float)>(32);
            public readonly List<Vector2> NoParking = new List<Vector2>(32);
            /// <summary>The town crossroads (three or more roads), for the odd rear-ended pair at a stop line.</summary>
            public readonly List<Junction> Crossroads = new List<Junction>(16);
            public Transform LootRoot;

            public Parking(Site site, NaturePlan plan)
            {
                Site = site;
                Plan = plan;
                Max = Mathf.Max(80, Mathf.RoundToInt(420f * site.AreaShare));
                MaxLoot = Mathf.Max(6, Mathf.RoundToInt(70f * site.AreaShare));
                foreach (Junction j in site.Junctions)
                {
                    int roads = 0, minor = 0;
                    Vector2 first = Vector2.zero, second = Vector2.zero;
                    float setback = 0f;
                    for (int i = 0; i < j.Arms.Count; i++)
                    {
                        JunctionArm a = j.Arms[i];
                        string kind = a.Street.Data.kind;
                        if (NaturePlan.FootKind(kind) || (a.Street.Rank == 0 && kind != "living_street")) continue;
                        setback = Mathf.Max(setback, RoadPass.ArmSetback(j, i));
                        if (a.Street.Rank >= 2 || kind == "living_street")
                        {
                            if (roads == 0) first = a.Dir;
                            else if (roads == 1) second = a.Dir;
                            roads++;
                        }
                        else minor++;
                    }
                    float corner = Mathf.Max(10f, setback + 6f), mouth = Mathf.Max(5f, setback + 2.5f);
                    if (roads >= 3)
                    {
                        Corners.Add((j.At, corner));
                        Crossroads.Add(j);
                    }
                    else if (roads >= 1 && roads + minor >= 3) Mouths.Add((j.At, mouth));
                    else if (roads == 2 && Vector2.Dot(first, second) > -0.86f) Corners.Add((j.At, corner));   // a corner between two streets
                    else if (minor >= 2 && roads + minor >= 3) Mouths.Add((j.At, mouth));
                }
                foreach (Node n in site.Area.Nodes)
                    if (n.Data.kind == "crossing" || n.Data.kind == "bus_stop" || n.Data.kind == "hydrant" || n.Data.kind == "traffic_signals"
                        || n.Data.kind == "stop" || n.Data.kind == "give_way")
                        NoParking.Add(n.At);
            }

            public bool Overlaps(Vector2 at, Vector2 axis, float halfLength, float halfWidth)
            {
                foreach (NaturePlan.Keep k in Placed)
                {
                    // Apart when their middles are further apart than their corners reach.
                    float reach = k.HalfLength + k.HalfWidth + halfLength + halfWidth;
                    if ((k.At - at).sqrMagnitude > reach * reach) continue;
                    if (NaturePlan.Overlap(at, axis, halfLength, halfWidth, k.At, k.Axis, k.HalfLength, k.HalfWidth)) return true;
                }
                return false;
            }

            /// <summary>True round a corner or a mouth (or near a no-parking point) at <paramref name="p"/>.</summary>
            public bool NoParkingAt(Vector2 p)
            {
                foreach ((Vector2 at, float within) in Corners)
                    if ((at - p).sqrMagnitude < within * within) return true;
                foreach ((Vector2 at, float within) in Mouths)
                    if ((at - p).sqrMagnitude < within * within) return true;
                return Near(NoParking, p, 4.5f);
            }
        }

        internal static IEnumerable<string> Build(Site site)
        {
            if (site.Detail == Detail.Lite) yield break;
            NaturePlan plan = NaturePlan.For(site);
            plan.CatchUp();
            PlanDriveways(site, plan);
            var c = new Parking(site, plan);
            string diag = "";
            int before = 0;
            foreach (string step in DrivewayCars(c)) yield return step;
            diag += "driveways " + (c.Count - before) + " of " + plan.Driveways.Count + ", "; before = c.Count;
            Specials(c);
            diag += "specials " + (c.Count - before) + ", "; before = c.Count;
            yield return "buses and cruisers";
            foreach (string step in LotCars(c)) yield return step;
            diag += "lots " + (c.Count - before) + ", "; before = c.Count;
            foreach (string step in KerbCars(c)) yield return step;
            diag += "kerbs " + (c.Count - before) + ", "; before = c.Count;
            Crashes(c);
            RoadWrecks(c);
            diag += "wrecks " + (c.Count - before) + ", lootable " + c.Loot;
            if (!site.Chunked) Debug.Log("[NatureDiag] cars " + c.Count + " of " + c.Max + ": " + diag + "; corners " + c.Corners.Count + ", mouths " + c.Mouths.Count);
            yield return "wrecks";
        }

        /// <summary>
        /// Parks the step's candidates (<see cref="Parking.Found"/>), every one when they fit under
        /// <paramref name="budget"/>, else that many picked evenly by a number of each one's own (so a site over its limit
        /// is thinned all over, never by the order its streets or lots come in), in the order they were found.
        /// </summary>
        private static IEnumerable<string> ParkFound(Parking c, int budget, string step)
        {
            List<Candidate> found = c.Found;
            bool[] keep = null;
            if (found.Count > budget)
            {
                keep = new bool[found.Count];
                var order = new List<int>(found.Count);
                for (int i = 0; i < found.Count; i++) order.Add(i);
                order.Sort((a, b) =>
                {
                    float ua = MapStyle.Unit(found[a].Key, SaltThin), ub = MapStyle.Unit(found[b].Key, SaltThin);
                    return ua != ub ? ua.CompareTo(ub) : a.CompareTo(b);
                });
                for (int i = 0; i < Mathf.Max(0, budget); i++) keep[order[i]] = true;
            }
            for (int i = 0; i < found.Count; i++)
            {
                if (keep != null && !keep[i]) continue;
                Candidate k = found[i];
                Park(c, k.At, k.Yaw, LookFor(k.Key, k.Body, k.Snow, k.Wear), k.Key, Quaternion.identity);
                if (c.Site.StepDue) yield return step;
            }
            found.Clear();
        }

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
        /// nothing, when it's outside the square, over the site's limit, in another's way or in a wall.
        /// </summary>
        private static bool Park(Parking c, Vector2 at, float yaw, in CarLook look, long key, Quaternion tilt)
        {
            if (c.Count >= c.Max || !c.Plan.InSquare(at, 0f)) return false;
            Vector3 size = CarKit.Size(look.Body);
            Vector2 axis = Axis(yaw);
            float hl = size.z * 0.5f, hw = size.x * 0.5f;
            if (c.Overlaps(at, axis, hl + 0.2f, hw + 0.15f)) return false;
            float reach = Mathf.Max(0f, hl - hw);
            for (int k = -1; k <= 1; k++)
                if (c.Plan.WallDistance(at + axis * (k * reach), hw + 1f) < hw + 0.15f) return false;
            Vector3 at3 = To3(at, 0f);
            RaidMesh m = c.Site.Town.Out.Solid(at3);
            m.SetFrame(at3, Quaternion.Euler(0f, yaw, 0f) * tilt);
            CarKit.Draw(m, look, MapStyle.Rng(key, SaltShape));
            m.ResetFrame();
            c.Site.Town.PropCollider(at3 + Vector3.up * (size.y * 0.5f), size, yaw);
            int steps = Mathf.Max(1, Mathf.CeilToInt(reach * 2f / 1.5f));
            for (int i = 0; i <= steps; i++) c.Site.Take(at + axis * Mathf.Lerp(-reach, reach, i / (float)steps), hw + 0.1f);
            c.Placed.Add(new NaturePlan.Keep { At = at, Axis = axis, HalfLength = hl, HalfWidth = hw, Kind = NaturePlan.KeepKind.Car });
            c.Count++;
            if (tilt == Quaternion.identity) Storage(c, at3, yaw, look, key, size);
            return true;
        }

        private const int SaltLoot = 0xCA30;

        /// <summary>A car's glovebox (on the passenger side of the cab; a box truck's cab), with the horn and the ignition to
        /// try from the driver's seat, and its trunk, a pickup's bed or a box truck's box: each a little storage of the
        /// game's own, rolled from the car's key, as the procedural town's wrecks have them. Burnt-out shells and school
        /// buses have none.</summary>
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
            Vector3 offset = c.Site.WorldOffset;   // a streamed piece is built at the origin and moved into place after
            int seed = unchecked((int)MapStyle.Hash(key, SaltLoot + 1));
            bool live = MapStyle.Unit(key, SaltHorn) < CloneZConfig.RaidHornLiveChance;
            Vector3 cab = at + q * new Vector3(hw + 0.12f, box ? 1.3f : 0.95f, box ? hl - 0.85f : 0.3f);
            Vector3 cabWorld = cab + offset;
            Fixture(host, box ? "Truck cab" : "Glovebox", "carglovebox", cab, offset, seed,
                new StorageOptions
                {
                    Noun = box ? "cab" : "glovebox", LookLabel = box ? "Search the cab" : "Search the glovebox", LootType = ContainerType.Desk,
                    EmptyChance = CloneZConfig.RaidGloveboxEmptyChance, MaxStacks = CloneZConfig.RaidGloveboxMaxStacks, RaidBoost = false,
                    ExtraActions = (ctx, list) => CabActions(list, cabWorld, live),
                });
            c.Site.Containers++;
            bool police = look.Body == CarBody.Cruiser;
            string back = box ? "Cargo box" : bed ? "Truck bed" : "Trunk";
            Vector3 rear = at + q * new Vector3(0f, box ? 1.2f : 0.9f, -hl - 0.12f);
            Fixture(host, back, "cartrunk", rear, offset, seed + 1,
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

        /// <summary>
        /// A storage at <paramref name="at"/> (site metres) on <paramref name="host"/>. A fixture keeps the point it's
        /// reached at as a place in the world, and a streamed piece is built at the origin and only then moved
        /// <paramref name="offset"/> into place: so it's made at its place in the world (its reach and its key right) and
        /// its object then set back where the piece's move will carry it to the same place.
        /// </summary>
        private static void Fixture(Transform host, string name, string kind, Vector3 at, Vector3 offset, int seed, StorageOptions options)
        {
            Vector3 world = at + offset;
            StorageFixture f = StorageFixture.Create(host, name, Interactable.KeyFor(kind, world), world, null, host, seed, options);
            if (offset != Vector3.zero) f.transform.position = at;
        }

        // From the driver's seat, as in the procedural town's wrecks: the horn (a racket heard across town while there's
        // charge in the battery: a lure, or a mistake) and the ignition. `cab` is in the world.
        private static void CabActions(List<InteractAction> into, Vector3 cab, bool live)
        {
            float next = 0f;
            var horn = new InteractAction("Lean on the horn").Timed(CloneZConfig.RaidHornSeconds, "Leaning on the horn", HumanoidActivity.Use);
            horn.Tick = (ctx, dt, progress) =>
            {
                if (!live || Time.time < next) return;
                next = Time.time + 0.4f;
                NoiseSystem.Emit(cab, CloneZConfig.RaidHornNoise);
            };
            horn.Run = ctx => ctx.Notify(live
                ? "The horn blares across the snow, flat and endless. Every dead ear in town just turned your way."
                : "A dry click from under the dash. The battery's long dead.");
            into.Add(horn);
            into.Add(new InteractAction("Try the ignition", ctx =>
            {
                NoiseSystem.Emit(cab, CloneZConfig.RaidIgnitionNoise);
                ctx.Notify(live
                    ? "No keys, but the wires hang loose under the column. You touch two together: the starter grinds, grinds, and nothing catches."
                    : "No keys, and nothing happens when you twist the wires together. Dead as everything else.");
            }));
        }

        /// <summary>The unit plan direction of a yaw (degrees clockwise from north).</summary>
        private static Vector2 Axis(float yaw) => new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));

        /// <summary>The yaw (degrees clockwise from north) of a plan direction.</summary>
        private static float Yaw(Vector2 dir) => Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        #endregion

        #region Driveways
        /// <summary>
        /// Plans the driveways' cars (once a site; <see cref="VegetationPass"/> calls it before planting so the yards leave
        /// them room). First the driveways the roads laid (<see cref="RoadPass.Driveways"/>): a car at the house end of
        /// most, unless the track's too short to hold one past the sidewalk. Then the homes inside the square the roads
        /// gave none (apartments, farmhouses set far back, homes on service roads...): a car (one in six a second, an
        /// apartment block two to four) beside the house's front corner, on whichever side has room, else in front of it
        /// where the yard is deep enough, at the end of a strip of packed snow out to the sidewalk; never on the sidewalk,
        /// in a wall, by a door or on another driveway. Each car's spot (and a strip's) is kept
        /// (<see cref="NaturePlan.Kept"/>) and taken in the occupancy grid.
        /// </summary>
        internal static void PlanDriveways(Site site, NaturePlan plan)
        {
            if (plan.DrivewaysPlanned) return;
            plan.DrivewaysPlanned = true;
            if (plan.Lite) return;
            var served = new HashSet<Footprint>();
            IReadOnlyList<RoadPass.DrivewaySpot> roads = RoadPass.Driveways(site);
            for (int i = 0; i < roads.Count; i++) RoadDriveway(site, plan, roads[i], served);
            foreach (Footprint f in site.Area.Buildings)
            {
                if (!NaturePlan.IsHome(f.Data) || f.Size < 30f || f.Pts.Count < 3 || !plan.InSquare(f.Middle, 0f) || served.Contains(f)) continue;
                long id = f.Data.id;
                float u = MapStyle.Unit(id, SaltHasCar);
                int want = f.Data.kind == "apartments" ? 2 + MapStyle.Pick(id, SaltHasCar + 1, 3) : u < 0.18f ? 0 : u < 0.84f ? 1 : 2;
                if (want == 0) continue;
                NaturePlan.HouseFrame hf = plan.FrameOf(f);
                float first = MapStyle.Unit(id, SaltSide) < 0.5f ? 1f : -1f;
                if (Driveway(site, plan, f, hf, first, want) == 0) Driveway(site, plan, f, hf, -first, want);
            }
        }

        // The car at the house end of a driveway the roads laid, nosed up to the house most times, backed in now and then;
        // none where the track is too short to hold it past the sidewalk, or where the household got away in it.
        private static void RoadDriveway(Site site, NaturePlan plan, in RoadPass.DrivewaySpot d, HashSet<Footprint> served)
        {
            if (d.Dir.sqrMagnitude < 0.5f || d.Length < 4f) return;
            Footprint home = HomeBeside(plan, d.At + d.Dir * (d.Length * 0.6f), d.Length * 0.5f + 5f);
            if (home != null) served.Add(home);
            long key = home != null ? NaturePlan.Combine(home.Data.id, 960)
                : NaturePlan.Combine((long)System.Math.Round(plan.AbsX(d.At.x) * 2.0), (long)System.Math.Round(plan.AbsZ(d.At.y) * 2.0));
            if (MapStyle.Unit(key, SaltHasCar) < 0.2f) return;
            float len = CarKit.Size(BodyOf(plan, d.At, home, key)).z;
            if (!plan.Roadside(d.At, d.Dir, out float edge, out float back)) back = edge + 0.5f;
            float walk = back - edge + 0.3f;
            float centre = d.Length - len * 0.5f - 0.4f;
            if (centre - len * 0.5f < walk) return;
            Vector2 at = d.At + d.Dir * centre;
            if (!plan.InSquare(at, 2.8f)) return;
            for (int k = -1; k <= 1; k++)
            {
                Vector2 q = at + d.Dir * (k * (len * 0.5f - 0.9f));
                if (plan.WallDistance(q, 2f) < 1.1f || plan.NearDoor(q, 2.4f)) return;
            }
            bool nose = MapStyle.Unit(key, SaltFace) < 0.7f;
            float yaw = Yaw(nose ? d.Dir : -d.Dir) + (MapStyle.Unit(key, SaltJitter) - 0.5f) * 5f;
            plan.Driveways.Add(new NaturePlan.CarSpot { At = at, Yaw = yaw, Key = key, Home = home, Mouth = d.At + d.Dir * walk, Road = true, Dir = d.Dir });
            plan.KeepRect(at, d.Dir, len * 0.5f + 0.2f, 1.2f, NaturePlan.KeepKind.Car);
            for (int k = -1; k <= 1; k++) site.Take(at + d.Dir * (k * 1.7f), 1.1f);
        }

        // The home whose walls come nearest a point within reach.
        private static readonly List<Footprint> s_Near = new List<Footprint>(16);

        private static Footprint HomeBeside(NaturePlan plan, Vector2 p, float within)
        {
            plan.BuildingsNear(p, within, s_Near);
            Footprint best = null;
            float bestD = within;
            foreach (Footprint f in s_Near)
            {
                if (!NaturePlan.IsHome(f.Data)) continue;
                float d = MapGeometry.DistanceToEdge(f.Pts, p);
                if (d >= bestD) continue;
                bestD = d;
                best = f;
            }
            return best;
        }

        // What's parked at a home: out in the country (or at a trailer) a pickup as likely as not.
        private static CarBody BodyOf(NaturePlan plan, Vector2 at, Footprint home, long key)
        {
            bool country = plan.Urban(at) < 0.035f || (home != null && home.Data.kind == "trailer");
            return BodyFor(country ? Where.Country : Where.Town, MapStyle.Unit(key, SaltBody));
        }

        // One side of the house: the first spot that fits, then the second car behind it (or beside it), then the rest of an
        // apartment block's beside those. Returns how many it planned.
        private static int Driveway(Site site, NaturePlan plan, Footprint f, NaturePlan.HouseFrame hf, float side, int want)
        {
            const float half = 2.6f, lane = 2.65f;
            float t = side > 0f ? hf.T1 + 1.75f : hf.T0 - 1.75f;
            // The sidewalk's far edge (or the road's, without one), as an n: where the strip comes out on the street.
            float sidewalk = float.MinValue;
            if (hf.Street)
            {
                sidewalk = hf.KerbN + 2.1f;
                if (plan.Roadside(hf.At(t, hf.KerbN), hf.N, out float _, out float back)) sidewalk = back - hf.Distance + 0.3f;
            }
            float[] spots = { hf.N0 + 1.6f, hf.N0 - 0.6f, hf.N0 - 3.3f };
            long id = f.Data.id;
            bool nose = MapStyle.Unit(id, SaltFace) < 0.72f;   // nosed up to the house, or backed in
            foreach (float n in spots)
            {
                if (n - half < sidewalk + 0.5f) continue;
                Vector2 at = hf.At(t, n);
                if (!CarFits(site, plan, f, at, hf.N)) continue;
                int count = 0;
                Plan(site, plan, f, at, hf.N, nose, NaturePlan.Combine(id, 900), hf.Street ? hf.At(t, sidewalk) : at, ref count);
                for (int k = 1; k < want; k++)
                {
                    Vector2 behind = hf.At(t, n - 5.4f * k);
                    Vector2 beside = hf.At(t + side * lane * k, n);
                    long key = NaturePlan.Combine(id, 900 + k);
                    if (f.Data.kind != "apartments" && n - 5.4f * k - half >= sidewalk + 0.5f && CarFits(site, plan, f, behind, hf.N))
                        Plan(site, plan, f, behind, hf.N, nose, key, Vector2.zero, ref count);
                    else if (CarFits(site, plan, f, beside, hf.N))
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
            plan.Driveways.Add(new NaturePlan.CarSpot { At = at, Yaw = yaw, Key = key, Home = home, Mouth = mouth, StripWidth = strip ? 2.7f : 0f, Dir = n });
            plan.KeepRect(at, n, 2.75f, 1.2f, NaturePlan.KeepKind.Car);
            if (strip)
            {
                Vector2 end = at + n * 2.6f;
                plan.KeepRect((mouth + end) * 0.5f, n, Vector2.Distance(mouth, end) * 0.5f, 1.4f, NaturePlan.KeepKind.Driveway);
            }
            for (int k = -1; k <= 1; k++) site.Take(at + n * (k * 1.7f), 1.1f);
            count++;
        }

        // Room for a driveway car beside its house: inside the square, off every wall, road and path, out of the water and
        // the pitches, off the other driveways and the doorsteps, its middle free in the grid (a porch, its steps, a
        // mailbox...), and away from the house's own outline (which the buildings take in the grid, with a margin) free
        // all along.
        private static bool CarFits(Site site, NaturePlan plan, Footprint home, Vector2 at, Vector2 axis)
        {
            if (!plan.InSquare(at, 2.8f) || plan.IsKept(at, axis, 2.75f, 1.15f) || !site.FreeBeside(at, 0f)) return false;
            Rect box = home.Bounds;
            for (int k = -1; k <= 1; k++)
            {
                Vector2 q = at + axis * (k * 1.7f);
                if (plan.WallDistance(q, 2f) < 1.25f || plan.RoadClearance(q, 2f) < 1.0f || plan.NearDoor(q, 2.6f)) return false;
                Land land = plan.LandAt(q);
                if (land == Land.Water || land == Land.Pitch || land == Land.Lot) return false;
                bool byHouse = q.x > box.xMin - 1.3f && q.x < box.xMax + 1.3f && q.y > box.yMin - 1.3f && q.y < box.yMax + 1.3f;
                if (!byHouse && !site.FreeBeside(q, 1.0f)) return false;
            }
            return true;
        }

        // The planned driveway cars, mostly snowed in; those this pass found room for itself at the end of their strip of
        // tyre-tracked packed snow.
        private static IEnumerable<string> DrivewayCars(Parking c)
        {
            foreach (NaturePlan.CarSpot s in c.Plan.Driveways)
            {
                CarLook look = LookFor(s.Key, BodyOf(c.Plan, s.At, s.Home, s.Key), 0.62f);
                if (!Park(c, s.At, s.Yaw, look, s.Key, Quaternion.identity)) continue;
                if (!s.Road && s.StripWidth > 0f) Strip(c, s.Mouth, s.At + (s.At - s.Mouth).normalized * 2.6f, s.StripWidth);
                if (c.Site.StepDue) yield return "driveway cars";
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
