using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What each building is and how it's built, from what the data says (kind, use, name, the rare storeys, roof shape,
    // material and colours) and, since it seldom says much, from its outline and surroundings: a narrow footprint near
    // the courthouse sharing walls with its neighbours is a two- or three-storey brick store with a cast-iron front; a
    // 14 x 9 m box on a suburban street is a brick ranch; a long 12 m-wide shed out in the county is a tobacco barn; a
    // 4.3 x 20 m box is a single-wide trailer. Every choice is drawn from the building's own id (MapStyle), so it comes
    // out the same wherever and whenever it's built.
    internal static partial class BuildingPass
    {
        internal enum Arch
        {
            House, Trailer, Shed, Garage, Barn, TobaccoBarn, Silo, Row, Store, Office, Apartments, Motel, Church, School,
            Civic, Courthouse, Hospital, Warehouse, Industrial, Rickhouse, Canopy, Greenhouse, Ruin, Parking,
        }

        internal enum HouseStyle { None, Ranch, Cottage, Bungalow, Shotgun, TwoStorey, Foursquare, Farmhouse, Victorian, Cape }
        internal enum RoofKind { Flat, Gable, Hip, Pyramid, Skillion, Gambrel, Round, Mansard }
        internal enum SnowCover { Full, Partial, Bare }
        internal enum WindowKind { None, Sash, Picture, Storefront, Ribbon, Arched, Small, Band }
        internal enum DoorKind { House, Shop, Double, Plain }

        private const int SaltPlan = 0x51A7, SaltWindowsLit = 0x11E7;

        /// <summary>How one building is built.</summary>
        private sealed class Plan
        {
            public Arch Arch;
            public HouseStyle Style;
            public string Use = "", Name = "";
            public float Core, Town, Population, Density;
            public bool[] Party;
            public int PartyCount;
            /// <summary>The wall facing the road (the front door's), or -1.</summary>
            public int Front = -1;
            public Vector2 RoadDir;
            public float RoadDist = 999f;
            public int RoadRank;

            public int Storeys = 1;
            /// <summary>Floor to floor of the ground storey: its walls stop at GroundStorey, a shop's sign band fills the
            /// rest.</summary>
            public float FirstStorey = GroundStorey;
            public float Storey = 2.9f;
            /// <summary>Top of the walls under a pitched roof; a flat roof's deck.</summary>
            public float Eave;
            /// <summary>A flat roof's parapet above the deck.</summary>
            public float Parapet;
            public float TaggedHeight;

            public RoofKind Roof = RoofKind.Gable;
            public float Pitch = 30f;
            public float Overhang = 0.35f;
            public RoofCover Covering = RoofCover.Shingle;
            public int CoverTone;
            public SnowCover Snow = SnowCover.Full;
            public bool Icicles, Heated = true;
            public int Chimneys;
            public bool ChimneyOutside, RidgeVent, Steeple, Belfry, Cupola, RoofUnits;

            public WallKind Wall = WallKind.Siding;
            public int WallTone;
            public Color Trim = new Color(0.93f, 0.93f, 0.90f), Accent = new Color(0.11f, 0.20f, 0.15f);
            public float Foundation = 0.35f;
            public Surf FoundationSurf;
            public bool CornerBoards, Belts, Cornice;

            public WindowKind GroundWindow = WindowKind.Sash, UpperWindow = WindowKind.Sash, FrontWindow = WindowKind.Sash;
            public float WindowWidth = 0.95f, WindowHeight = 1.45f, Sill = 0.85f;
            public float Spacing = 3.1f;
            public bool Shutters, Lit;
            public DoorKind Door = DoorKind.House;
            public bool BackDoor = true;
            /// <summary>Overhead, roll-up or sliding doors along the front (garages, warehouses, barns, fire stations).</summary>
            public int BigDoors;
            public float BigDoorWidth = 2.7f, BigDoorHeight = 2.3f;

            public bool Porch, FullPorch, KneeWall, Stoop, Mailbox, PorchLight, Swing;
            public float PorchDepth = 2.1f;
            public bool Storefront, Awning, SignBand, GhostSign;
            public string Sign;
            public int AwningTone, SignTone;
            public bool Enterable = true, Fuel;
            /// <summary>A tower's walls all glass, its floors painted on as a curtain wall.</summary>
            public bool Curtain;
            /// <summary>A mechanical penthouse on a tall flat roof.</summary>
            public bool Penthouse;
            /// <summary>The big doors open onto a loading dock a truck bed high.</summary>
            public bool Dock;
            /// <summary>An apartment block's balconies on its upper floors, and its stair up the outside.</summary>
            public bool Balconies, OutsideStair;
            /// <summary>A downtown front's parapet: 0 straight, 1 raised in the middle, 2 stepped, 3 a pediment.</summary>
            public int FrontParapet;

            /// <summary>The highest point of the walls (a flat roof's parapet included).</summary>
            public float WallHeight => Roof == RoofKind.Flat ? Eave + Parapet : Eave;
        }

        private static int DefaultLevels(string kind)
        {
            switch (kind)
            {
                case "apartments": case "hotel": case "hospital": return 3;
                case "office": case "school": case "civic": return 2;
                default: return 1;
            }
        }

        private static bool Has(string s, string part) => s.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static Plan Classify(Context ctx, Footprint f, Shape sh, List<Footprint> near)
        {
            MapBuilding b = f.Data;
            var rng = MapStyle.Rng(b.id, SaltPlan);
            var p = new Plan();
            ctx.Neighbourhood(f, 40f, out float cover, out int count);
            p.Density = cover;
            p.Party = ctx.PartyWalls(f, near, out p.PartyCount);
            ctx.Urban(f, cover, count, out p.Core, out p.Town, out p.Population);
            if (p.PartyCount > 0) p.Core = Mathf.Max(p.Core, Mathf.Min(1f, p.Town * 0.9f));
            Spot poi = ctx.Inside(f);
            p.Use = !string.IsNullOrEmpty(b.use) ? b.use : poi != null ? poi.Data.kind : "";
            p.Name = !string.IsNullOrEmpty(b.name) ? b.name : poi != null && !string.IsNullOrEmpty(poi.Data.name) ? poi.Data.name : "";
            string land = ctx.LandUse(f.Middle);
            if (ctx.NearestRoad(f.Middle, 90f, out float roadDist, out Street road, out Vector2 toward))
            {
                p.RoadDist = roadDist;
                p.RoadRank = road.Rank;
                p.RoadDir = toward;
            }
            p.Front = FrontWall(f, p);

            float area = f.Size, width = sh.Width, length = sh.Length, aspect = sh.Aspect;
            bool city = p.Population >= 60000f;
            p.Arch = ArchOf(ctx, f, sh, p, land, area, width, length, aspect, rng);
            switch (p.Arch)
            {
                case Arch.House: House(p, f, sh, rng); break;
                case Arch.Trailer: Trailer(p, rng); break;
                case Arch.Shed: Shed(p, rng); break;
                case Arch.Garage: Garage(p, rng); break;
                case Arch.Barn: Barn(p, area, rng); break;
                case Arch.TobaccoBarn: TobaccoBarn(p, rng); break;
                case Arch.Silo: Silo(p, sh, rng); break;
                case Arch.Row: Row(p, area, city, rng); break;
                case Arch.Store: Store(p, area, rng); break;
                case Arch.Office: Office(p, area, city, rng); break;
                case Arch.Parking: Parking(p, area, city, rng); break;
                case Arch.Apartments: Apartments(p, area, rng); break;
                case Arch.Motel: Motel(p, rng); break;
                case Arch.Church: Church(p, area, rng); break;
                case Arch.School: School(p, area, rng); break;
                case Arch.Civic: Civic(p, area, rng); break;
                case Arch.Courthouse: Courthouse(p, rng); break;
                case Arch.Hospital: Hospital(p, area, rng); break;
                case Arch.Warehouse: case Arch.Industrial: Warehouse(p, area, sh, rng); break;
                case Arch.Rickhouse: Rickhouse(p, rng); break;
                case Arch.Canopy: Canopy(p, ctx, f, rng); break;
                case Arch.Greenhouse: Greenhouse(p, rng); break;
                default: Ruin(p, rng); break;
            }
            Tags(p, b, sh);
            Finish(p, b, sh, rng);
            return p;
        }

        // The wall the front door goes in: the longest facing the road, never one shared with a neighbour.
        private static int FrontWall(Footprint f, Plan p)
        {
            List<Vector2> pts = f.Pts;
            int best = -1;
            float bestScore = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                if (p.Party[i]) continue;
                Vector2 a = pts[i], c = pts[(i + 1) % pts.Count];
                float len = Vector2.Distance(a, c);
                if (len < 1.6f) continue;
                Vector2 d = (c - a) / len, outward = new Vector2(d.y, -d.x);
                float facing = p.RoadDir == Vector2.zero ? 0.5f : Vector2.Dot(outward, p.RoadDir);
                float score = Mathf.Min(len, 14f) * (facing > 0.35f ? 1f + facing * 2f : 0.2f);
                if (score <= bestScore) continue;
                bestScore = score;
                best = i;
            }
            return best;
        }

        private static Arch ArchOf(Context ctx, Footprint f, Shape sh, Plan p, string land, float area, float width, float length,
            float aspect, System.Random rng)
        {
            MapBuilding b = f.Data;
            string kind = b.kind ?? "", use = p.Use, name = p.Name;
            float core = p.Core, town = p.Town;
            bool round = sh.Round > 0.86f && f.Pts.Count >= 8 && area < 600f;
            bool trailerShape = width >= 3.2f && width <= 5.6f && length >= 10f && length <= 27f && aspect >= 2.4f
                && sh.Fill >= 0.85f && area < 145f;
            bool barnShape = aspect >= 1.8f && width >= 8f && width <= 18f && area >= 140f;
            bool distillery = Has(name, "distill") || Has(name, "bourbon") || Has(name, "rickhouse") || Has(name, "rackhouse");
            // Storeys the data gives (its default for the kind means it gave none): a tower isn't a shop row.
            int tagged = b.levels > 0 && b.levels != DefaultLevels(kind) ? b.levels : 0;

            // A building mapped as parking is a parking deck (a lot is an area, not a building).
            if ((use == "parking" || Has(name, "parking garage") || Has(name, "parking deck")) && area >= 350f && kind != "house" && kind != "garage")
                return Arch.Parking;

            switch (kind)
            {
                case "ruin": return Arch.Ruin;
                case "canopy": return Arch.Canopy;
                case "silo": return Arch.Silo;
                case "greenhouse": return Arch.Greenhouse;
                case "trailer": return Arch.Trailer;
                case "shed": return area > 90f && town < 0.4f ? Arch.Barn : Arch.Shed;
                case "garage": return area > 140f ? Arch.Warehouse : Arch.Garage;
                case "barn": return barnShape && rng.NextDouble() < 0.6 ? Arch.TobaccoBarn : Arch.Barn;
                case "church": return Arch.Church;
                case "school": return Arch.School;
                case "hospital": return Arch.Hospital;
                case "civic": return use == "courthouse" || use == "townhall" || Has(name, "court") ? Arch.Courthouse : Arch.Civic;
                case "hotel": return tagged >= 5 ? Arch.Office : core > 0.5f && area < 2500f ? Arch.Row : Arch.Motel;
                case "apartments": return core > 0.6f && p.PartyCount > 0 && tagged < 5 ? Arch.Row : Arch.Apartments;
                case "office": return core > 0.55f && area < 900f && tagged < 6 ? Arch.Row : Arch.Office;
                case "retail": return core > 0.5f && area < 1500f ? Arch.Row : Arch.Store;
                case "industrial": return distillery ? Arch.Rickhouse : Arch.Industrial;
            }

            switch (use)
            {
                case "place_of_worship": return Arch.Church;
                case "school": case "kindergarten": case "college": case "university": case "childcare": return Arch.School;
                case "hospital": return Arch.Hospital;
                case "courthouse": case "townhall": return area >= 250f ? Arch.Courthouse : Arch.Civic;
                case "police": case "fire_station": case "library": case "post_office": case "community_centre":
                case "social_facility": case "prison": case "theatre": case "cinema":
                    return core > 0.6f && p.PartyCount > 0 ? Arch.Row : Arch.Civic;
                case "clinic": case "doctors": case "dentist": case "veterinary": return core > 0.6f ? Arch.Row : Arch.Office;
                case "fuel": return Arch.Store;
                case "shelter": return Arch.Canopy;
            }
            bool shop = use.StartsWith("shop:") || use == "bank" || use == "restaurant" || use == "fast_food" || use == "cafe"
                || use == "bar" || use == "pub" || use == "ice_cream" || use == "car_repair" || use == "car_wash";
            if (round) return town < 0.6f || area > 40f ? Arch.Silo : Arch.Shed;
            if (kind == "warehouse")
            {
                if (distillery) return Arch.Rickhouse;
                if (core > 0.6f && area < 6000f) return Arch.Row;
                if (shop || land == "retail" || land == "commercial") return Arch.Store;
                return land == "industrial" || area > 9000f ? Arch.Industrial : Arch.Warehouse;
            }
            if (kind == "house")
            {
                if (trailerShape && core < 0.35f && !shop) return Arch.Trailer;
                if (shop && (core > 0.3f || area > 90f)) return core > 0.5f ? Arch.Row : Arch.Store;
                if (area < 24f) return Arch.Shed;
                if (area < 75f && aspect < 2.3f && ctx.BiggerNeighbour(f, 32f, 1.6f))
                    return town > 0.3f ? Arch.Garage : area > 45f && rng.NextDouble() < 0.5 ? Arch.Barn : Arch.Shed;
                if (core > 0.55f && (p.PartyCount > 0 || p.Density > 0.4f || (p.RoadRank >= 4 && p.RoadDist < 10f))) return Arch.Row;
                if (land == "industrial" && area > 90f) return Arch.Warehouse;
                if (land == "farmyard" && area > 110f && barnShape) return Arch.TobaccoBarn;
                return Arch.House;
            }
            // "commercial": tagged so, or an untagged building of 250 to 2,500 m². Downtown it's a commercial row's,
            // unless it's too big or too tall to be a store's: then an office block (a bank, a state office, a hotel).
            if (core > 0.45f) return (area > 2600f || tagged >= 7) && !shop ? Arch.Office : Arch.Row;
            if (shop) return Arch.Store;
            if (tagged >= 4) return Arch.Office;
            if (town > 0.45f)
            {
                if (land == "industrial") return area < 700f ? Arch.Industrial : Arch.Warehouse;
                if (land == "retail" || land == "commercial" || (p.RoadRank >= 3 && p.RoadDist < 30f && rng.NextDouble() < 0.6))
                    return Arch.Store;
                if (land == "school") return Arch.School;
                if (land == "churchyard") return Arch.Church;
                if (area < 480f && (land == "residential" || p.RoadRank <= 2)) return Arch.House;
                return rng.NextDouble() < 0.5 ? Arch.Store : Arch.Warehouse;
            }
            if (land == "industrial") return Arch.Industrial;
            if (barnShape && rng.NextDouble() < 0.7) return Arch.TobaccoBarn;
            if (area < 450f && aspect < 2.3f && p.RoadDist < 60f && rng.NextDouble() < 0.55) return Arch.House;
            return area > 700f && rng.NextDouble() < 0.5 ? Arch.Warehouse : Arch.Barn;
        }

        private static int Pick(System.Random rng, params float[] weights)
        {
            float sum = 0f;
            for (int i = 0; i < weights.Length; i++) sum += weights[i];
            float r = (float)rng.NextDouble() * sum;
            for (int i = 0; i < weights.Length; i++)
            {
                r -= weights[i];
                if (r <= 0f) return i;
            }
            return weights.Length - 1;
        }

        // The same without a params array a call (the plan is drawn for every building a streamed piece builds).
        private static readonly float[] s_Weights = new float[8];

        private static int Pick(System.Random rng, float a, float b) => PickOf(rng, 2, a, b, 0f, 0f, 0f, 0f, 0f);
        private static int Pick(System.Random rng, float a, float b, float c) => PickOf(rng, 3, a, b, c, 0f, 0f, 0f, 0f);
        private static int Pick(System.Random rng, float a, float b, float c, float d) => PickOf(rng, 4, a, b, c, d, 0f, 0f, 0f);
        private static int Pick(System.Random rng, float a, float b, float c, float d, float e) => PickOf(rng, 5, a, b, c, d, e, 0f, 0f);
        private static int Pick(System.Random rng, float a, float b, float c, float d, float e, float f) => PickOf(rng, 6, a, b, c, d, e, f, 0f);
        private static int Pick(System.Random rng, float a, float b, float c, float d, float e, float f, float g) => PickOf(rng, 7, a, b, c, d, e, f, g);

        private static int PickOf(System.Random rng, int n, float a, float b, float c, float d, float e, float f, float g)
        {
            float[] w = s_Weights;
            w[0] = a; w[1] = b; w[2] = c; w[3] = d; w[4] = e; w[5] = f; w[6] = g;
            float sum = 0f;
            for (int i = 0; i < n; i++) sum += w[i];
            float r = (float)rng.NextDouble() * sum;
            for (int i = 0; i < n; i++)
            {
                r -= w[i];
                if (r <= 0f) return i;
            }
            return n - 1;
        }

        // One of a few values by index (what an array literal indexed at once would give, without the array).
        private static T Of<T>(int i, T a, T b) => i <= 0 ? a : b;
        private static T Of<T>(int i, T a, T b, T c) => i <= 0 ? a : i == 1 ? b : c;
        private static T Of<T>(int i, T a, T b, T c, T d) => i <= 0 ? a : i == 1 ? b : i == 2 ? c : d;
        private static T Of<T>(int i, T a, T b, T c, T d, T e) => i <= 0 ? a : i == 1 ? b : i == 2 ? c : i == 3 ? d : e;
        private static T Of<T>(int i, T a, T b, T c, T d, T e, T f) => i <= 3 ? Of(i, a, b, c, d) : i == 4 ? e : f;
        private static T Of<T>(int i, T a, T b, T c, T d, T e, T f, T g) => i <= 3 ? Of(i, a, b, c, d) : i == 4 ? e : i == 5 ? f : g;
        private static T Of<T>(int i, T a, T b, T c, T d, T e, T f, T g, T h, T k, T l)
            => i <= 3 ? Of(i, a, b, c, d) : i <= 6 ? Of(i - 4, e, f, g) : Of(i - 7, h, k, l);

        private static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        private static bool Chance(System.Random rng, float p) => rng.NextDouble() < p;

        #region Archetypes
        private static void House(Plan p, Footprint f, Shape sh, System.Random rng)
        {
            float area = f.Size, aspect = sh.Aspect;
            bool frontIsEnd = p.Front >= 0 && sh.EdgeAxis[p.Front] == 1;   // the front wall runs across the house: a gable to the street
            bool rural = p.Town < 0.3f, old = p.Core > 0.18f || p.Town > 0.88f;
            HouseStyle style;
            if (p.Town > 0.35f && sh.Width <= 7.2f && aspect >= 1.5f && area < 135f && frontIsEnd) style = HouseStyle.Shotgun;
            else if (rural)
            {
                if (aspect >= 1.65f && sh.Width <= 9.8f && area >= 70f && !frontIsEnd && Chance(rng, 0.6f)) style = HouseStyle.Farmhouse;
                else if (area >= 105f) style = Of(Pick(rng, 0.6f, 0.25f, 0.15f), HouseStyle.Ranch, HouseStyle.TwoStorey, HouseStyle.Cape);
                else style = HouseStyle.Cottage;
            }
            else if (old)
            {
                if (aspect < 1.35f && area >= 80f) style = Of(Pick(rng, 0.55f, 0.25f, 0.2f), HouseStyle.Foursquare, HouseStyle.Victorian, HouseStyle.Bungalow);
                else style = Of(Pick(rng, 0.35f, 0.35f, 0.15f, 0.15f), HouseStyle.Victorian, HouseStyle.Bungalow, HouseStyle.TwoStorey, HouseStyle.Cape);
            }
            else style = Of(Pick(rng, 0.55f, 0.2f, 0.15f, 0.1f), HouseStyle.Ranch, HouseStyle.TwoStorey, HouseStyle.Cape, HouseStyle.Bungalow);
            if (area < 62f && style != HouseStyle.Shotgun) style = HouseStyle.Cottage;
            else if (area > 240f && (style == HouseStyle.Cottage || style == HouseStyle.Bungalow)) style = HouseStyle.TwoStorey;
            if (style == HouseStyle.Farmhouse && frontIsEnd) style = HouseStyle.TwoStorey;
            p.Style = style;

            bool brick;
            p.Overhang = 0.4f;
            p.Chimneys = 1;
            switch (style)
            {
                case HouseStyle.Ranch:
                    p.Roof = Chance(rng, 0.55f) ? RoofKind.Hip : RoofKind.Gable;
                    p.Pitch = Range(rng, 19f, 26f);
                    p.Overhang = 0.5f;
                    brick = Chance(rng, 0.55f);
                    p.Stoop = true;
                    p.Porch = Chance(rng, 0.3f);
                    p.Chimneys = Chance(rng, 0.55f) ? 1 : 0;
                    p.FrontWindow = WindowKind.Picture;
                    break;
                case HouseStyle.Cottage:
                    p.Roof = aspect < 1.2f && Chance(rng, 0.5f) ? RoofKind.Pyramid : RoofKind.Gable;
                    p.Pitch = Range(rng, 32f, 40f);
                    p.Overhang = 0.35f;
                    brick = Chance(rng, 0.15f);
                    p.Porch = Chance(rng, 0.65f);
                    p.ChimneyOutside = Chance(rng, 0.45f);
                    break;
                case HouseStyle.Bungalow:
                    p.Roof = Chance(rng, 0.8f) ? RoofKind.Gable : RoofKind.Hip;
                    p.Pitch = Range(rng, 26f, 34f);
                    p.Overhang = 0.6f;
                    brick = Chance(rng, 0.4f);
                    p.Porch = Chance(rng, 0.85f);
                    p.FullPorch = true;
                    p.KneeWall = Chance(rng, 0.5f);
                    p.PorchDepth = 2.4f;
                    p.ChimneyOutside = Chance(rng, 0.4f);
                    break;
                case HouseStyle.Shotgun:
                    p.Roof = RoofKind.Gable;
                    p.Pitch = Range(rng, 35f, 42f);
                    p.Overhang = 0.3f;
                    brick = Chance(rng, 0.15f);
                    p.Porch = Chance(rng, 0.8f);
                    p.FullPorch = true;
                    p.PorchDepth = 2.0f;
                    p.Foundation = 0.55f;
                    p.WindowHeight = 1.7f;
                    break;
                case HouseStyle.TwoStorey:
                    p.Storeys = 2;
                    p.Roof = Chance(rng, 0.6f) ? RoofKind.Gable : RoofKind.Hip;
                    p.Pitch = Range(rng, 30f, 38f);
                    brick = Chance(rng, 0.4f);
                    p.Porch = Chance(rng, 0.7f);
                    p.FullPorch = Chance(rng, 0.3f);
                    p.ChimneyOutside = Chance(rng, 0.4f);
                    break;
                case HouseStyle.Foursquare:
                    p.Storeys = 2;
                    p.Roof = Chance(rng, 0.8f) ? RoofKind.Hip : RoofKind.Pyramid;
                    p.Pitch = Range(rng, 24f, 30f);
                    p.Overhang = 0.55f;
                    brick = Chance(rng, 0.45f);
                    p.Porch = Chance(rng, 0.85f);
                    p.FullPorch = true;
                    p.PorchDepth = 2.4f;
                    p.KneeWall = brick && Chance(rng, 0.5f);
                    break;
                case HouseStyle.Farmhouse:
                    p.Storeys = 2;
                    p.Roof = RoofKind.Gable;
                    p.Pitch = Range(rng, 32f, 40f);
                    p.Overhang = 0.3f;
                    brick = Chance(rng, 0.25f);
                    p.Porch = Chance(rng, 0.85f);
                    p.FullPorch = Chance(rng, 0.6f);
                    p.ChimneyOutside = Chance(rng, 0.65f);
                    p.Chimneys = p.ChimneyOutside ? 2 : 1;
                    p.Foundation = 0.55f;
                    break;
                case HouseStyle.Victorian:
                    p.Storeys = 2;
                    p.Roof = Chance(rng, 0.7f) ? RoofKind.Gable : RoofKind.Hip;
                    p.Pitch = Range(rng, 40f, 50f);
                    brick = Chance(rng, 0.15f);
                    p.Porch = Chance(rng, 0.85f);
                    p.FullPorch = Chance(rng, 0.6f);
                    p.Chimneys = Chance(rng, 0.5f) ? 2 : 1;
                    p.Foundation = 0.6f;
                    p.WindowHeight = 1.75f;
                    p.Storey = 3.1f;
                    break;
                default:   // Cape
                    p.Roof = RoofKind.Gable;
                    p.Pitch = Range(rng, 42f, 48f);
                    p.Overhang = 0.3f;
                    brick = Chance(rng, 0.2f);
                    p.Porch = Chance(rng, 0.35f);
                    p.Stoop = !p.Porch;
                    p.ChimneyOutside = Chance(rng, 0.5f);
                    break;
            }
            if (p.Storeys == 1 && area > 200f && style != HouseStyle.Ranch && Chance(rng, 0.5f)) p.Storeys = 2;
            p.Wall = brick ? WallKind.Brick : WallKind.Siding;
            p.WallTone = brick ? Of(Pick(rng, 3f, 1.5f, 2f, 1.5f, 1.5f, 1f), 0, 1, 2, 3, 4, 6)
                : old || rural ? Of(rng.Next(10), 0, 0, 1, 2, 3, 4, 5, 6, 8, 9) : Of(rng.Next(7), 0, 1, 3, 5, 6, 7, 9);
            p.Trim = brick ? TrimTones[Of(Pick(rng, 5f, 2f, 2f, 1f), TrimWhite, TrimCream, TrimBrown, TrimBlack)]
                : TrimTones[Of(Pick(rng, 6f, 1.5f, 1f, 1f, 1f), TrimWhite, TrimCream, TrimGreen, TrimBlack, TrimBrown)];
            p.Accent = ShutterTones[rng.Next(ShutterTones.Length - 1)];
            p.Shutters = Chance(rng, brick ? 0.35f : 0.5f);
            p.CornerBoards = !brick;
            p.Covering = rural && Chance(rng, style == HouseStyle.Farmhouse ? 0.5f : 0.25f) ? RoofCover.Metal : RoofCover.Shingle;
            p.CoverTone = p.Covering == RoofCover.Metal
                ? Of(Pick(rng, 4f, 2f, 1.5f, 1.5f), MetalGalv, MetalRed, MetalGreen, MetalRusty)
                : Of(Pick(rng, 3f, 1.5f, 2f, 2f, 0.8f, 0.4f, 0.4f), ShingleCharcoal, ShingleBlack, ShingleGrey, ShingleBrown, ShingleGreen, ShingleRed, ShingleBlue);
            if (style == HouseStyle.Victorian && Chance(rng, 0.25f)) p.Covering = RoofCover.Slate;
            if (p.Foundation < 0.5f) p.Foundation = Range(rng, 0.3f, 0.5f);
            p.FoundationSurf = brick ? WallSurf(WallKind.Block, 2) : Chance(rng, 0.5f) ? WallSurf(WallKind.Brick, 1) : WallSurf(WallKind.Block, 2);
            p.Spacing = Range(rng, 2.8f, 3.6f);
            p.Mailbox = !rural && Chance(rng, 0.45f);
            p.PorchLight = Chance(rng, 0.45f);
            p.Swing = p.Porch && Chance(rng, 0.3f);
            p.Lit = Chance(rng, 0.22f);
            p.Icicles = Chance(rng, 0.7f);
        }

        private static void Trailer(Plan p, System.Random rng)
        {
            p.Wall = WallKind.Trailer;
            p.WallTone = rng.Next(TrailerBodies.Length);
            p.Roof = Chance(rng, 0.7f) ? RoofKind.Gable : RoofKind.Round;
            p.Pitch = Range(rng, 4f, 7f);
            p.Overhang = 0.12f;
            p.Covering = RoofCover.Metal;
            p.CoverTone = Chance(rng, 0.6f) ? MetalWhite : MetalGalv;
            p.Eave = 2.75f;
            p.Foundation = 0f;
            p.Trim = TrimTones[TrimGrey];
            p.WindowWidth = 0.9f;
            p.WindowHeight = 0.95f;
            p.Sill = 1.15f;
            p.Spacing = 2.8f;
            p.Stoop = true;
            p.BackDoor = Chance(rng, 0.5f);
            p.PorchLight = Chance(rng, 0.3f);
            p.Lit = Chance(rng, 0.2f);
            p.Icicles = Chance(rng, 0.3f);
        }

        private static void Shed(Plan p, System.Random rng)
        {
            int kind = Pick(rng, 5f, 3f, 2f);
            p.Roof = kind == 0 ? RoofKind.Gable : kind == 1 ? RoofKind.Skillion : RoofKind.Gambrel;
            p.Pitch = p.Roof == RoofKind.Skillion ? Range(rng, 10f, 16f) : Range(rng, 24f, 34f);
            p.Overhang = 0.15f;
            p.Eave = Range(rng, 2.25f, 2.5f);
            p.Wall = Chance(rng, 0.65f) ? WallKind.Boards : WallKind.Metal;
            p.WallTone = p.Wall == WallKind.Boards ? Of(rng.Next(4), BoardRed, BoardGrey, 3, 4) : rng.Next(MetalTones.Length);
            p.Covering = Chance(rng, 0.6f) ? RoofCover.Metal : RoofCover.Shingle;
            p.CoverTone = p.Covering == RoofCover.Metal ? Of(rng.Next(4), MetalGalv, MetalRusty, MetalRed, MetalGreen) : rng.Next(4);
            p.Foundation = 0.12f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 1);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.Small;
            p.Spacing = 6f;
            p.Door = DoorKind.Plain;
            p.BackDoor = false;
            p.Heated = false;
            p.Trim = TrimTones[TrimWhite];
        }

        private static void Garage(Plan p, System.Random rng)
        {
            p.Roof = Chance(rng, 0.8f) ? RoofKind.Gable : RoofKind.Hip;
            p.Pitch = Range(rng, 20f, 30f);
            p.Overhang = 0.3f;
            p.Eave = 2.7f;
            bool brick = Chance(rng, 0.3f);
            p.Wall = brick ? WallKind.Brick : Chance(rng, 0.7f) ? WallKind.Siding : WallKind.Block;
            p.WallTone = rng.Next(TonesOf(p.Wall).Length);
            p.Covering = Chance(rng, 0.75f) ? RoofCover.Shingle : RoofCover.Metal;
            p.CoverTone = rng.Next(TonesOf(p.Covering).Length);
            p.Foundation = 0.15f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 1);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.Small;
            p.Spacing = 5f;
            p.BigDoors = 1;
            p.BigDoorWidth = Chance(rng, 0.5f) ? 2.6f : 4.8f;
            p.BigDoorHeight = 2.15f;
            p.Door = DoorKind.Plain;
            p.BackDoor = false;
            p.Heated = false;
            p.Trim = TrimTones[TrimWhite];
            p.CornerBoards = p.Wall == WallKind.Siding;
        }

        private static void Barn(Plan p, float area, System.Random rng)
        {
            bool pole = area > 500f && Chance(rng, 0.55f);
            int kind = pole ? 0 : Pick(rng, 5.5f, 3f);
            p.Roof = kind == 1 ? RoofKind.Gambrel : RoofKind.Gable;
            p.Pitch = pole ? Range(rng, 14f, 20f) : Range(rng, 33f, 40f);
            p.Overhang = 0.35f;
            p.Eave = pole ? Range(rng, 4.2f, 5.2f) : Range(rng, 3.6f, 4.6f);
            p.Wall = pole ? WallKind.Metal : WallKind.Boards;
            p.WallTone = pole ? Of(rng.Next(4), 0, 1, 2, 6) : Of(Pick(rng, 3f, 2f, 2.5f, 1.5f, 1f), BoardRed, BoardRed, BoardGrey, BoardBlack, 3);
            p.Covering = RoofCover.Metal;
            p.CoverTone = Of(Pick(rng, 5f, 2f, 1.5f, 1f, 0.5f), MetalGalv, MetalRusty, MetalRed, MetalGreen, MetalWhite);
            p.Foundation = 0.2f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 1);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.Small;
            p.Spacing = 7f;
            p.BigDoors = 1;
            p.BigDoorWidth = pole ? 5f : 3.6f;
            p.BigDoorHeight = Mathf.Min(p.Eave - 0.4f, 3.8f);
            p.Door = DoorKind.Plain;
            p.BackDoor = Chance(rng, 0.5f);
            p.Heated = false;
            p.Trim = p.Wall == WallKind.Boards && p.WallTone == BoardRed ? TrimTones[TrimWhite] : TrimTones[TrimGrey];
        }

        private static void TobaccoBarn(Plan p, System.Random rng)
        {
            p.Roof = RoofKind.Gable;
            p.Pitch = Range(rng, 28f, 34f);
            p.Overhang = 0.3f;
            p.Eave = Range(rng, 5.0f, 6.4f);
            p.Wall = WallKind.Boards;
            p.WallTone = Of(Pick(rng, 5.5f, 3.5f, 1f), BoardBlack, BoardGrey, BoardRed);
            p.Covering = RoofCover.Metal;
            p.CoverTone = Of(Pick(rng, 5f, 3.5f, 1.5f), MetalGalv, MetalRusty, MetalRed);
            p.RidgeVent = Chance(rng, 0.4f);
            p.Foundation = 0f;
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.None;
            p.BigDoors = 1;
            p.BigDoorWidth = 3.8f;
            p.BigDoorHeight = Mathf.Min(p.Eave - 0.5f, 4.2f);
            p.Door = DoorKind.Plain;
            p.BackDoor = true;
            p.Heated = false;
            p.Trim = TrimTones[TrimGrey];
        }

        private static void Silo(Plan p, Shape sh, System.Random rng)
        {
            float d = Mathf.Sqrt(sh.Area / Mathf.PI) * 2f;
            bool bin = d > 4.5f && Chance(rng, 0.7f);
            p.Roof = bin ? RoofKind.Pyramid : RoofKind.Round;
            p.Eave = bin ? d * Range(rng, 0.7f, 1.1f) : Mathf.Clamp(d * Range(rng, 2.6f, 3.6f), 10f, 22f);
            p.Pitch = bin ? 30f : 0f;
            p.Wall = bin ? WallKind.Metal : WallKind.Concrete;
            p.WallTone = bin ? 0 : 0;
            p.Covering = RoofCover.Metal;
            p.CoverTone = bin ? MetalGalv : Chance(rng, 0.3f) ? MetalBlue : MetalGalv;
            p.Enterable = false;
            p.Heated = false;
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.None;
            p.BackDoor = false;
        }

        // A downtown commercial building: brick, a shop front with display windows, a transom and a sign band, tall
        // sash windows above under stone lintels, a cornice along a parapet hiding a flat roof.
        private static void Row(Plan p, float area, bool city, System.Random rng)
        {
            int storeys;
            if (city) storeys = area < 400f ? 2 + rng.Next(2) : area < 1200f ? 3 + rng.Next(2) : 4 + rng.Next(3);
            else storeys = Of(Pick(rng, 2f, 5.5f, p.Core > 0.75f ? 2.5f : 1.2f), 1, 2, 3);
            if (area > 700f && storeys < 2) storeys = 2;
            p.Storeys = storeys;
            p.FirstStorey = 3.9f;
            p.Storey = 3.3f;
            p.Roof = RoofKind.Flat;
            p.Parapet = Range(rng, 0.6f, 1.1f);
            int wall = Pick(rng, 8.5f, 0.6f, 0.5f, 0.4f);
            p.Wall = wall == 0 ? WallKind.Brick : wall == 1 ? WallKind.Stone : wall == 2 ? WallKind.Stucco : WallKind.Block;
            p.WallTone = p.Wall == WallKind.Brick ? Of(Pick(rng, 3f, 2f, 2f, 1.5f, 1f, 1f), 0, 1, 2, 3, 6, BrickPainted) : rng.Next(TonesOf(p.Wall).Length);
            p.Trim = TrimTones[Of(rng.Next(6), TrimGreen, TrimBlack, TrimOxblood, TrimCream, TrimWhite, TrimBrown)];
            p.Accent = p.Trim;
            p.Foundation = 0.12f;
            p.FoundationSurf = WallSurf(WallKind.Stone, 0);
            p.Belts = storeys >= 2 && Chance(rng, 0.6f);
            p.Cornice = true;
            p.Storefront = true;
            p.FrontWindow = WindowKind.Storefront;
            p.GroundWindow = WindowKind.Small;
            p.UpperWindow = WindowKind.Sash;
            p.WindowWidth = Range(rng, 0.9f, 1.05f);
            p.WindowHeight = Range(rng, 1.8f, 2.1f);
            p.Sill = 0.8f;
            p.Spacing = Range(rng, 2.2f, 2.7f);
            p.Door = DoorKind.Shop;
            p.Awning = Chance(rng, 0.45f);
            p.AwningTone = rng.Next(AwningTones.Length);
            p.SignBand = true;
            p.SignTone = rng.Next(4);
            p.GhostSign = storeys >= 2 && Chance(rng, 0.3f);
            p.Covering = RoofCover.Membrane;
            p.CoverTone = 0;
            p.Chimneys = Chance(rng, 0.5f) ? 1 : 0;
            p.RoofUnits = Chance(rng, 0.65f);
            // Victorian fronts: a parapet raised in the middle, stepped, or under a pediment, over two storeys or three.
            p.FrontParapet = storeys >= 2 && storeys <= 3 ? Pick(rng, 3.5f, 3f, 1.5f, 2f) : 0;
            p.Lit = Chance(rng, 0.15f);
            p.Icicles = Chance(rng, 0.3f);
        }

        // Out of the middle of town: a one-storey store, restaurant, bank or gas station's shop, flat-roofed behind a
        // sign band.
        private static void Store(Plan p, float area, System.Random rng)
        {
            bool food = p.Use == "restaurant" || p.Use == "fast_food" || p.Use == "cafe" || p.Use == "ice_cream";
            bool rural = p.Town < 0.4f;
            p.Storeys = 1;
            p.FirstStorey = 3.0f;
            if (food && Chance(rng, 0.6f))
            {
                p.Roof = RoofKind.Mansard;
                p.Pitch = 68f;
                p.Covering = RoofCover.Shingle;
                p.CoverTone = Of(rng.Next(4), ShingleBrown, ShingleCharcoal, ShingleRed, ShingleGreen);
            }
            else if (rural && area < 500f && Chance(rng, 0.45f))
            {
                // A country store: a gable roof behind a false front.
                p.Roof = RoofKind.Gable;
                p.Pitch = Range(rng, 20f, 28f);
                p.Covering = RoofCover.Metal;
                p.CoverTone = Of(rng.Next(3), MetalGalv, MetalRed, MetalGreen);
            }
            else
            {
                p.Roof = RoofKind.Flat;
                p.Covering = RoofCover.Membrane;
                p.CoverTone = rng.Next(2);
            }
            p.Eave = Range(rng, 3.6f, 4.3f);
            p.Parapet = p.Roof == RoofKind.Flat ? Range(rng, 0.5f, 1.0f) : 0f;
            int wall = Pick(rng, 4f, 3.5f, rural ? 2f : 0.6f, 1.5f);
            p.Wall = wall == 0 ? WallKind.Block : wall == 1 ? WallKind.Brick : wall == 2 ? WallKind.Metal : WallKind.Stucco;
            p.WallTone = rng.Next(TonesOf(p.Wall).Length);
            if (p.Wall == WallKind.Brick && p.WallTone == BrickPainted) p.WallTone = 0;
            p.Trim = TrimTones[Of(rng.Next(5), TrimWhite, TrimBrown, TrimGreen, TrimOxblood, TrimGrey)];
            p.Foundation = 0.1f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 1);
            p.Storefront = true;
            p.FrontWindow = WindowKind.Storefront;
            p.GroundWindow = WindowKind.Small;
            p.Spacing = 5f;
            p.Door = DoorKind.Shop;
            p.SignBand = true;
            p.SignTone = rng.Next(4);
            p.Awning = rural && Chance(rng, 0.2f);
            p.AwningTone = rng.Next(AwningTones.Length);
            p.RoofUnits = p.Roof == RoofKind.Flat;
            p.Lit = Chance(rng, 0.25f);
            p.Icicles = Chance(rng, 0.25f);
            if (p.Use == "fuel") p.Sign = Of(rng.Next(4), "FOOD MART", "GAS FOOD", "QUIK STOP", "CONVENIENCE");
        }

        // An office block, a bank, a state office: flat-roofed (now and then hipped), ribbon windows, taller the bigger its
        // footprint and the nearer the middle of a bigger town; a tower's top floors over a mechanical penthouse, now and
        // then all glass.
        private static void Office(Plan p, float area, bool city, System.Random rng)
        {
            int byArea = Mathf.Clamp(Mathf.RoundToInt(1f + Mathf.Sqrt(area) / 30f + p.Core * 1.2f), 1, 6);
            p.Storeys = city ? Mathf.Max(byArea, 2 + rng.Next(3)) : Mathf.Max(byArea, area > 900f ? 2 : 1 + rng.Next(2));
            p.Storey = 3.4f;
            p.FirstStorey = p.Storeys >= 3 ? 3.8f : GroundStorey;
            p.Roof = p.Storeys <= 2 && Chance(rng, 0.3f) ? RoofKind.Hip : RoofKind.Flat;
            p.Pitch = 22f;
            p.Overhang = 0.5f;
            p.Parapet = 0.6f;
            p.Wall = Of(Pick(rng, 5f, 2.5f, 1f, 1f), WallKind.Brick, WallKind.Concrete, WallKind.Stone, WallKind.Block);
            p.WallTone = rng.Next(TonesOf(p.Wall).Length);
            if (p.Wall == WallKind.Brick && p.WallTone == BrickPainted) p.WallTone = 2;
            p.Trim = TrimTones[Of(rng.Next(4), TrimWhite, TrimBrown, TrimBlack, TrimGrey)];
            p.Foundation = 0.2f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 0);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.Ribbon;
            p.WindowHeight = 1.5f;
            p.Sill = 0.9f;
            p.Spacing = 3.4f;
            p.Door = DoorKind.Double;
            p.Covering = p.Roof == RoofKind.Flat ? RoofCover.Membrane : RoofCover.Shingle;
            p.CoverTone = rng.Next(2);
            p.RoofUnits = p.Roof == RoofKind.Flat;
            p.Lit = Chance(rng, 0.25f);
        }

        // A parking deck: open concrete floors behind low walls, a stair tower at a corner.
        private static void Parking(Plan p, float area, bool city, System.Random rng)
        {
            p.Storeys = city ? 3 + rng.Next(4) : area > 3000f ? 3 + rng.Next(2) : 2 + rng.Next(2);
            p.FirstStorey = 3.0f;
            p.Storey = 3.0f;
            p.Roof = RoofKind.Flat;
            p.Parapet = 1.05f;
            p.Wall = WallKind.Concrete;
            p.WallTone = rng.Next(ConcreteTones.Length);
            p.Trim = TrimTones[TrimGrey];
            p.Foundation = 0f;
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.None;
            p.Enterable = false;
            p.Heated = false;
            p.BackDoor = false;
            p.Covering = RoofCover.Membrane;
            p.CoverTone = 1;
        }

        private static void Apartments(Plan p, float area, System.Random rng)
        {
            p.Storeys = area < 300f ? 2 : Chance(rng, 0.6f) ? 2 : 3;
            p.Roof = area < 900f && Chance(rng, 0.55f) ? (Chance(rng, 0.5f) ? RoofKind.Hip : RoofKind.Gable) : RoofKind.Flat;
            p.Pitch = Range(rng, 20f, 26f);
            p.Overhang = 0.45f;
            p.Parapet = 0.5f;
            p.Wall = Chance(rng, 0.6f) ? WallKind.Brick : WallKind.Siding;
            p.WallTone = rng.Next(TonesOf(p.Wall).Length);
            if (p.Wall == WallKind.Brick && p.WallTone == BrickPainted) p.WallTone = 3;
            p.Trim = TrimTones[Of(rng.Next(3), TrimWhite, TrimCream, TrimBrown)];
            p.Foundation = 0.3f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 1);
            p.FrontWindow = WindowKind.Picture;
            p.Spacing = 3.0f;
            p.Covering = p.Roof == RoofKind.Flat ? RoofCover.Membrane : RoofCover.Shingle;
            p.CoverTone = rng.Next(TonesOf(p.Covering).Length);
            p.Stoop = true;
            p.Lit = Chance(rng, 0.4f);
            p.Icicles = Chance(rng, 0.4f);
            p.RoofUnits = p.Roof == RoofKind.Flat;
            // A garden apartment block: balconies on its upper floors, a stair up its front.
            p.Balconies = Chance(rng, 0.55f);
            p.OutsideStair = Chance(rng, 0.45f);
        }

        private static void Motel(Plan p, System.Random rng)
        {
            p.Storeys = Chance(rng, 0.6f) ? 1 : 2;
            p.Roof = Chance(rng, 0.5f) ? RoofKind.Gable : RoofKind.Flat;
            p.Pitch = 18f;
            p.Overhang = 0.9f;
            p.Parapet = 0.4f;
            p.Wall = Chance(rng, 0.6f) ? WallKind.Block : WallKind.Brick;
            p.WallTone = rng.Next(TonesOf(p.Wall).Length);
            p.Trim = TrimTones[Of(rng.Next(3), TrimWhite, TrimBrown, TrimGreen)];
            p.Accent = ShutterTones[rng.Next(5)];
            p.Foundation = 0.12f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 1);
            p.FrontWindow = WindowKind.Picture;
            p.Spacing = 3.6f;
            p.Covering = p.Roof == RoofKind.Flat ? RoofCover.Membrane : RoofCover.Shingle;
            p.CoverTone = rng.Next(TonesOf(p.Covering).Length);
            p.SignBand = false;
            p.Lit = Chance(rng, 0.35f);
            p.Stoop = false;
        }

        private static void Church(Plan p, float area, System.Random rng)
        {
            bool small = area < 280f, big = area > 900f;
            bool frame = small ? Chance(rng, 0.7f) : Chance(rng, 0.15f);
            p.Roof = RoofKind.Gable;
            p.Pitch = Range(rng, 44f, 52f);
            p.Overhang = 0.35f;
            p.Eave = small ? Range(rng, 4.2f, 4.8f) : big ? Range(rng, 7.0f, 8.5f) : Range(rng, 5.4f, 6.4f);
            p.Wall = frame ? WallKind.Siding : big && Chance(rng, 0.25f) ? WallKind.Stone : WallKind.Brick;
            p.WallTone = frame ? 0 : p.Wall == WallKind.Stone ? 0 : Of(rng.Next(4), 0, 1, 3, 2);
            p.Trim = TrimTones[TrimWhite];
            p.Foundation = 0.5f;
            p.FoundationSurf = WallSurf(WallKind.Stone, 1);
            p.CornerBoards = frame;
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.Arched;
            p.WindowWidth = big ? 1.4f : 1.1f;
            p.WindowHeight = Mathf.Min(p.Eave - 1.6f, big ? 4.2f : 3.0f);
            p.Sill = 1.1f;
            p.Spacing = big ? 4.2f : 3.6f;
            p.Door = DoorKind.Double;
            p.Stoop = true;
            p.Steeple = !small || Chance(rng, 0.5f);
            p.Belfry = !p.Steeple;
            p.Covering = Of(Pick(rng, 5f, big ? 4f : 1.5f, 2f), RoofCover.Shingle, RoofCover.Slate, RoofCover.Metal);
            p.CoverTone = p.Covering == RoofCover.Shingle ? Of(rng.Next(3), ShingleCharcoal, ShingleBlack, ShingleGrey)
                : p.Covering == RoofCover.Metal ? Of(rng.Next(3), MetalGalv, MetalGreen, MetalRed) : 0;
            p.Lit = Chance(rng, 0.2f);
            p.Icicles = Chance(rng, 0.4f);
        }

        private static void School(Plan p, float area, System.Random rng)
        {
            p.Storeys = area > 3500f ? 2 : Chance(rng, 0.35f) ? 2 : 1;
            p.Storey = 3.6f;
            p.FirstStorey = 3.6f;
            p.Roof = RoofKind.Flat;
            p.Eave = 0f;
            p.Parapet = 0.7f;
            p.Wall = Chance(rng, 0.85f) ? WallKind.Brick : WallKind.Block;
            p.WallTone = p.Wall == WallKind.Brick ? Of(rng.Next(4), 0, 3, 4, 2) : rng.Next(BlockTones.Length);
            p.Trim = TrimTones[Of(rng.Next(3), TrimWhite, TrimGrey, TrimBrown)];
            p.Foundation = 0.25f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 0);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.Ribbon;
            p.WindowHeight = 1.9f;
            p.Sill = 0.9f;
            p.Spacing = 3.6f;
            p.Door = DoorKind.Double;
            p.Covering = RoofCover.Membrane;
            p.RoofUnits = true;
            p.Belts = true;
            p.Lit = Chance(rng, 0.15f);
        }

        private static void Civic(Plan p, float area, System.Random rng)
        {
            bool fire = p.Use == "fire_station" || Has(p.Name, "fire");
            p.Storeys = area > 600f || Chance(rng, 0.4f) ? 2 : 1;
            p.Storey = 3.5f;
            p.FirstStorey = fire ? 4.2f : 3.2f;
            p.Roof = Chance(rng, 0.7f) ? RoofKind.Flat : RoofKind.Hip;
            p.Pitch = 24f;
            p.Overhang = 0.5f;
            p.Parapet = 0.8f;
            p.Wall = Chance(rng, 0.75f) ? WallKind.Brick : WallKind.Stone;
            p.WallTone = p.Wall == WallKind.Brick ? Of(rng.Next(4), 0, 1, 2, 3) : rng.Next(StoneTones.Length);
            p.Trim = TrimTones[TrimWhite];
            p.Foundation = 0.4f;
            p.FoundationSurf = WallSurf(WallKind.Stone, 0);
            p.Cornice = true;
            p.Belts = true;
            p.WindowHeight = 1.9f;
            p.Spacing = 2.8f;
            p.Door = DoorKind.Double;
            p.Stoop = true;
            p.Covering = p.Roof == RoofKind.Flat ? RoofCover.Membrane : RoofCover.Slate;
            p.SignBand = true;
            p.SignTone = 3;
            if (fire)
            {
                p.BigDoors = 2;
                p.BigDoorWidth = 3.6f;
                p.BigDoorHeight = 3.6f;
            }
            p.RoofUnits = p.Roof == RoofKind.Flat;
        }

        private static void Courthouse(Plan p, System.Random rng)
        {
            p.Storeys = 2;
            p.Storey = 4.0f;
            p.FirstStorey = 4.0f;
            p.Roof = RoofKind.Hip;
            p.Pitch = Range(rng, 22f, 28f);
            p.Overhang = 0.6f;
            p.Wall = Chance(rng, 0.6f) ? WallKind.Brick : WallKind.Stone;
            p.WallTone = p.Wall == WallKind.Brick ? Of(rng.Next(2), 0, 1) : 0;
            p.Trim = TrimTones[TrimWhite];
            p.Foundation = 0.8f;
            p.FoundationSurf = WallSurf(WallKind.Stone, 0);
            p.Belts = true;
            p.WindowWidth = 1.1f;
            p.WindowHeight = 2.4f;
            p.Sill = 0.9f;
            p.Spacing = 3.0f;
            p.Door = DoorKind.Double;
            p.Stoop = true;
            p.Cupola = true;
            p.Covering = Chance(rng, 0.5f) ? RoofCover.Slate : RoofCover.Metal;
            p.CoverTone = p.Covering == RoofCover.Metal ? MetalGreen : 0;
            p.Lit = true;
            p.Icicles = true;
        }

        private static void Hospital(Plan p, float area, System.Random rng)
        {
            p.Storeys = area < 2500f ? 2 + rng.Next(2) : 3 + rng.Next(3);
            p.Storey = 3.6f;
            p.FirstStorey = 4.0f;
            p.Roof = RoofKind.Flat;
            p.Parapet = 0.6f;
            p.Penthouse = p.Storeys >= 4;
            p.Wall = Chance(rng, 0.7f) ? WallKind.Brick : WallKind.Concrete;
            p.WallTone = p.Wall == WallKind.Brick ? Of(rng.Next(3), 0, 2, 4) : rng.Next(ConcreteTones.Length);
            p.Trim = TrimTones[TrimWhite];
            p.Foundation = 0.3f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 0);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.Band;
            p.WindowHeight = 1.5f;
            p.Spacing = 3.2f;
            p.Door = DoorKind.Double;
            p.Covering = RoofCover.Membrane;
            p.RoofUnits = true;
            p.Lit = true;
        }

        private static void Warehouse(Plan p, float area, Shape sh, System.Random rng)
        {
            bool old = p.Arch == Arch.Industrial && p.Core > 0.25f && Chance(rng, 0.6f);
            p.Eave = area < 1500f ? Range(rng, 4.8f, 5.8f) : area < 6000f ? Range(rng, 6.2f, 7.6f) : Range(rng, 7.6f, 9.5f);
            bool pitched = sh.Blocks.Count == 1 && !old && Chance(rng, 0.6f);
            p.Roof = pitched ? RoofKind.Gable : RoofKind.Flat;
            p.Pitch = Range(rng, 6f, 10f);
            p.Overhang = 0.25f;
            p.Parapet = old ? 0.8f : 0.35f;
            p.Wall = old ? WallKind.Brick : Chance(rng, 0.75f) ? WallKind.Metal : WallKind.Block;
            p.WallTone = old ? Of(rng.Next(3), 0, 1, 2) : rng.Next(TonesOf(p.Wall).Length);
            p.Trim = TrimTones[TrimGrey];
            p.Foundation = 0.3f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 1);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = old ? WindowKind.Ribbon : WindowKind.Small;
            p.WindowHeight = old ? 2.2f : 0.9f;
            p.Sill = old ? 1.0f : 2.2f;
            p.Spacing = old ? 3.2f : 8f;
            p.Door = DoorKind.Plain;
            p.BigDoors = Mathf.Clamp(Mathf.RoundToInt(sh.Length / 14f), 1, 5);
            p.BigDoorWidth = 3.6f;
            p.BigDoorHeight = Mathf.Min(4.2f, p.Eave - 1f);
            // A distribution warehouse's doors open onto a loading dock.
            p.Dock = p.Arch == Arch.Warehouse && !old && area > 600f && Chance(rng, 0.5f);
            if (p.Dock)
            {
                p.BigDoorWidth = 3.0f;
                p.BigDoorHeight = Mathf.Min(p.Eave - 0.6f, DockHeight + 3.0f);
            }
            p.Covering = pitched ? RoofCover.Metal : RoofCover.Membrane;
            p.CoverTone = pitched ? Of(rng.Next(3), MetalGalv, MetalWhite, MetalBlue) : 1;
            p.RoofUnits = !pitched;
            p.Heated = false;
            p.BackDoor = Chance(rng, 0.5f);
        }

        // A bourbon rickhouse: six iron-clad floors of barrels, small windows in rows (a row a floor), a long ridge vent.
        private static void Rickhouse(Plan p, System.Random rng)
        {
            p.Eave = Range(rng, 16f, 21f);
            p.Storey = 3.2f;
            p.FirstStorey = 3.2f;
            p.Storeys = Mathf.Max(3, Mathf.RoundToInt((p.Eave - 1f) / p.Storey));
            p.Roof = RoofKind.Gable;
            p.Pitch = Range(rng, 12f, 16f);
            p.Overhang = 0.3f;
            p.RidgeVent = true;
            p.Wall = Chance(rng, 0.7f) ? WallKind.Metal : WallKind.Brick;
            p.WallTone = p.Wall == WallKind.Metal ? 0 : 1;
            p.Trim = TrimTones[TrimGrey];
            p.Foundation = 0.6f;
            p.FoundationSurf = WallSurf(WallKind.Stone, 1);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.Small;
            p.WindowHeight = 0.7f;
            p.Sill = 1.6f;
            p.Spacing = 4.5f;
            p.Door = DoorKind.Plain;
            p.BigDoors = 1;
            p.BigDoorWidth = 3f;
            p.BigDoorHeight = 3f;
            p.Covering = RoofCover.Metal;
            p.CoverTone = MetalGalv;
            p.Heated = false;
        }

        private static void Canopy(Plan p, Context ctx, Footprint f, System.Random rng)
        {
            p.Fuel = p.Use == "fuel" || ctx.FuelNear(f.Middle, 28f);
            p.Roof = p.Fuel ? RoofKind.Flat : f.Size < 14f ? RoofKind.Skillion : RoofKind.Gable;
            p.Pitch = p.Roof == RoofKind.Gable ? 18f : 8f;
            p.Eave = p.Fuel ? Range(rng, 4.5f, 5.0f) : 2.6f;
            p.Overhang = 0.2f;
            p.Covering = p.Fuel ? RoofCover.Membrane : RoofCover.Metal;
            p.CoverTone = p.Fuel ? 1 : Of(rng.Next(4), MetalGalv, MetalGreen, MetalRed, MetalRusty);
            p.Enterable = false;
            p.Heated = false;
            p.SignTone = rng.Next(4);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.None;
        }


        private static void Greenhouse(Plan p, System.Random rng)
        {
            p.Roof = Chance(rng, 0.65f) ? RoofKind.Round : RoofKind.Gable;
            p.Pitch = 26f;
            p.Eave = p.Roof == RoofKind.Round ? 1.2f : 2.2f;
            p.Overhang = 0.05f;
            p.Wall = WallKind.Metal;
            p.WallTone = 2;
            p.Foundation = 0.2f;
            p.FoundationSurf = WallSurf(WallKind.Concrete, 1);
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.None;
            p.Door = DoorKind.Plain;
            p.BackDoor = false;
            p.Heated = false;
            p.Snow = SnowCover.Partial;
        }

        private static void Ruin(Plan p, System.Random rng)
        {
            p.Wall = Chance(rng, 0.6f) ? WallKind.Brick : WallKind.Stone;
            p.WallTone = p.Wall == WallKind.Brick ? rng.Next(4) : 1;
            p.Eave = Range(rng, 1.8f, 2.8f);
            p.Roof = RoofKind.Flat;
            p.Foundation = 0f;
            p.GroundWindow = p.UpperWindow = p.FrontWindow = WindowKind.None;
            p.Door = DoorKind.Plain;
            p.BackDoor = false;
            p.Heated = false;
        }
        #endregion

        // What the data does say overrides the guesses: storeys, height, roof shape, wall material, colours.
        private static void Tags(Plan p, MapBuilding b, Shape sh)
        {
            if (b.levels > 0 && b.levels != DefaultLevels(b.kind) && p.Arch != Arch.Silo && p.Arch != Arch.Canopy
                && p.Arch != Arch.Church && p.Arch != Arch.Rickhouse)
                p.Storeys = Mathf.Clamp(b.levels, 1, 40);
            if (b.height > 0f) p.TaggedHeight = Mathf.Clamp(b.height, 2.4f, 160f);
            switch (b.roof)
            {
                case "flat": p.Roof = RoofKind.Flat; if (p.Parapet <= 0f) p.Parapet = 0.4f; break;
                case "gabled": p.Roof = RoofKind.Gable; break;
                case "hipped": case "half-hipped": p.Roof = RoofKind.Hip; break;
                case "pyramidal": p.Roof = RoofKind.Pyramid; break;
                case "skillion": p.Roof = RoofKind.Skillion; break;
                case "gambrel": p.Roof = RoofKind.Gambrel; break;
                case "mansard": p.Roof = RoofKind.Mansard; break;
                case "dome": case "round": p.Roof = RoofKind.Round; break;
            }
            if (p.Roof != RoofKind.Flat && (p.Pitch < 3f || p.Pitch > 70f)) p.Pitch = p.Roof == RoofKind.Round ? 0f : 30f;
            if (p.Roof != RoofKind.Flat && p.Covering == RoofCover.Membrane) p.Covering = RoofCover.Shingle;
            switch (b.material)
            {
                case "brick": p.Wall = WallKind.Brick; break;
                case "wood": p.Wall = p.Arch == Arch.Barn || p.Arch == Arch.TobaccoBarn || p.Arch == Arch.Shed ? WallKind.Boards : WallKind.Siding; break;
                case "vinyl": p.Wall = WallKind.Siding; break;
                case "stone": p.Wall = WallKind.Stone; break;
                case "concrete": p.Wall = WallKind.Concrete; break;
                case "metal": p.Wall = WallKind.Metal; break;
                case "stucco": p.Wall = WallKind.Stucco; break;
                case "glass": p.Wall = WallKind.Concrete; p.UpperWindow = WindowKind.Band; p.Curtain = p.Storeys >= 3; break;
            }
            Color[] tones = TonesOf(p.Wall);
            if (p.WallTone >= tones.Length) p.WallTone = 0;
            int tone = NearestTone(b.colour, tones);
            if (tone >= 0) p.WallTone = tone;
            Color[] covers = TonesOf(p.Covering);
            if (p.CoverTone >= covers.Length) p.CoverTone = 0;
            int roofTone = NearestTone(b.roofColour, covers);
            if (roofTone >= 0) p.CoverTone = roofTone;
            else if (p.Roof != RoofKind.Flat && !string.IsNullOrEmpty(b.roofColour) && TryParseHex(b.roofColour, out Color rc) && rc.r > 0.45f && rc.g < 0.3f)
            {
                p.Covering = RoofCover.Metal;
                p.CoverTone = MetalRed;
            }
        }

        // Heights from the storeys, and the snow lying on the roof.
        private static void Finish(Plan p, MapBuilding b, Shape sh, System.Random rng)
        {
            if (p.Eave <= 0f) p.Eave = p.FirstStorey + (p.Storeys - 1) * p.Storey;
            if (p.TaggedHeight > 0f)
            {
                float rise = p.Roof == RoofKind.Flat ? p.Parapet : Mathf.Tan(p.Pitch * Mathf.Deg2Rad) * sh.HalfW;
                if (p.Roof == RoofKind.Round) rise = sh.HalfW * 0.6f;
                p.Eave = Mathf.Max(2.4f, p.TaggedHeight - rise);
                if (p.Arch != Arch.Silo && p.Arch != Arch.Church && p.Arch != Arch.TobaccoBarn && p.Arch != Arch.Barn
                    && p.Arch != Arch.Warehouse && p.Arch != Arch.Industrial && p.Arch != Arch.Rickhouse)
                    p.Storeys = Mathf.Max(1, Mathf.RoundToInt((p.Eave - p.FirstStorey) / p.Storey) + 1);
            }
            if (p.Arch == Arch.Canopy) p.Storeys = 1;
            // A tall block's roof: a mechanical penthouse on it, a higher parapet; now and then a tower all of glass.
            if (p.Roof == RoofKind.Flat && p.Storeys >= 5 && (p.Arch == Arch.Office || p.Arch == Arch.Hospital || p.Arch == Arch.Apartments
                || p.Arch == Arch.Row || p.Arch == Arch.Civic))
            {
                p.Penthouse = true;
                p.RoofUnits = true;
                p.Parapet = Mathf.Max(p.Parapet, p.Storeys >= 8 ? 1.0f : 0.7f);
                if (p.Arch == Arch.Office && p.Storeys >= 10 && MapStyle.Unit(b.id, SaltPlan + 11) < 0.45f) p.Curtain = true;
            }

            float r = (float)rng.NextDouble();
            if (p.Roof == RoofKind.Flat) p.Snow = r < 0.82f ? SnowCover.Full : SnowCover.Partial;
            else if (p.Arch == Arch.Greenhouse) p.Snow = SnowCover.Partial;
            else if (p.Covering == RoofCover.Metal) p.Snow = r < 0.3f ? SnowCover.Full : r < 0.65f ? SnowCover.Partial : SnowCover.Bare;
            else if (!p.Heated) p.Snow = r < 0.6f ? SnowCover.Full : r < 0.9f ? SnowCover.Partial : SnowCover.Bare;
            else p.Snow = r < 0.42f ? SnowCover.Full : r < 0.78f ? SnowCover.Partial : SnowCover.Bare;
            if (p.Roof != RoofKind.Flat && p.Pitch >= 48f && p.Snow == SnowCover.Full) p.Snow = SnowCover.Partial;
            if (!p.Heated) p.Icicles = false;
            if (p.Snow == SnowCover.Bare) p.Icicles = p.Icicles && Chance(rng, 0.3f);
        }
    }
}
