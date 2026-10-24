using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CloneZ;

namespace CloneZ
{
    /// <summary>
    /// Builds a raid map from a real place in Kentucky: the streets, buildings, water and land use around the zone's
    /// <see cref="ZoneDef.RealPlace"/>, from the world store's current build (OpenStreetMap data, made by Tools/World;
    /// see <see cref="WorldStore"/>). Laid out like <see cref="RaidMapBuilder"/>'s town: ground-floor walls, floors and
    /// props under <c>world</c> with colliders, so the NavMesh bake carves them; upper storeys, ceilings and roofs under
    /// the overhead root, shown only from inside views; the surroundings past the playable square as scenery without
    /// colliders; exits where streets leave the map. Everything is in metres from the place's centre, x east, z north.
    /// Builds nothing and returns null (with the reason) when the store or the place can't be read, so the caller can
    /// build the procedural town instead. Map data © OpenStreetMap contributors (ODbL).
    /// </summary>
    public static partial class KentuckyMap
    {
        /// <summary>How far past the playable square the surroundings are drawn, as scenery.</summary>
        private const float SceneryReach = 90f;

        private const float SnowDepth = 0.05f; // thin snow sheet applied to horizontal surfaces
        private const float SidewalkWidth = 1.5f; // modest concrete sidewalk width

        /// <summary>A one-line summary of the last map built.</summary>
        public static string LastStats { get; private set; } = "";

        /// <summary>Builds the map for <paramref name="zone"/> under <paramref name="world"/>, or returns null with the
        /// reason in <paramref name="why"/>, having built nothing. A statewide zone (<see cref="ZoneDef.RealStatewide"/>)
        /// gets the whole state, streamed around the survivor (see <see cref="BuildStatewide"/>).</summary>
        public static RaidMapInfo Build(Transform world, ZoneDef zone, out string why)
        {
            if (zone.RealStatewide) return BuildStatewide(world, zone, out why);
            MapBuild build = WorldStore.OpenCurrent(out why);
            if (build == null) return null;
            float half = Mathf.Clamp(zone.RealHalfSize, 60f, 400f);
            Area area;
            try
            {
                MapPlace place = build.FindPlace(zone.RealPlace);
                if (place == null)
                {
                    why = "build " + build.Id + " has no place called " + zone.RealPlace;
                    return null;
                }
                area = new Area(build, place, half);
            }
            catch (System.Exception e)
            {
                why = "reading build " + build.Id + ": " + e.GetType().Name + ": " + e.Message;
                return null;
            }

            var info = new RaidMapInfo { HalfSize = half, PlayerSpawn = new Vector3(0f, 0.6f, 0f) };
            CeilingVisibility.Clear();
            var town = new RaidTown(world, zone.LayoutSeed, info);
            var site = new Site(town, area);
            try
            {
                foreach (string _ in Passes(site, build)) { }
                DefineExits(site);
                PickSpawn(site);
                site.Overhead.Emit("Overhead", town.Overhead, true);
                town.EmitOutdoor();
                DecorPalette.Apply();
            }
            catch (System.Exception e)
            {
                why = "building " + area.Place + " failed: " + e;
                Discard(world, town);
                return null;
            }
            LastStats = "Kentucky map " + area.Place + " (build " + area.BuildId + "): " + site.Buildings + " buildings, "
                + site.SceneryBuildings + " beyond the edge, " + area.Streets.Count + " street pieces, " + site.Containers
                + " containers, " + town.LightCount + " lights, " + site.Trees + " trees, " + info.Exits.Count + " exits. "
                + area.Attribution;
            Debug.Log("[CloneZ] " + LastStats);
            return info;
        }

        /// <summary>Everything the map draws on a site, in order, one pass at a time (a streamed piece is built a few
        /// passes a frame; the fixed square runs them all at once).</summary>
        internal static IEnumerable<string> Passes(Site site, MapBuild build)
        {
            BuildGround(site);
            yield return "ground";
            BuildWaterAndLots(site);
            yield return "water and lots";
            BuildStreets(site, build);
            yield return "streets";
            BuildSidewalks(site);
            yield return "sidewalks";
            BuildLines(site);
            yield return "lines";
            foreach (string step in BuildBuildings(site, build)) yield return step;
            BuildingFacadeBuilder.BuildAll(site, build);
            yield return "facades";
            BuildStreetLights(site);
            yield return "street lights";
            BuildStreetLamps(site);
            yield return "street lamps";
            BuildSignposts(site);
            yield return "signposts";
            BuildMarkedStreetLamps(site, build);
            yield return "marked street lamps";
            BuildHydrants(site);
            yield return "hydrants";
            BuildSnowPiles(site);
            yield return "snow piles";
            BuildParkedCars(site);
            yield return "parked cars";
            BuildTrees(site);
            yield return "trees";
            BuildPoints(site);
            yield return "points";
        }

        // A failed build mustn't leave half a town for the NavMesh bake or the procedural map to land on.
        private static void Discard(Transform world, RaidTown town)
        {
            for (int i = world.childCount - 1; i >= 0; i--) Object.DestroyImmediate(world.GetChild(i).gameObject);
            if (town.Overhead != null) Object.DestroyImmediate(town.Overhead.gameObject);
            if (town.Scenery != null) Object.DestroyImmediate(town.Scenery.gameObject);
            CeilingVisibility.Clear();
        }

        internal static Vector3 To3(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        // Concrete sidewalks under a thin layer of snow along both sides of every street, drawn like the roads: inside
        // the playable square with the outdoor ground, past the edge as scenery where the road carries on.
        private static void BuildSidewalks(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square)) Sidewalks(site.Town.Out, s, piece);
                if (!site.Chunked && Rank(s.Data.kind) >= 2)
                    foreach (List<Vector2> piece in OutsidePieces(s.Pts, half)) Sidewalks(site.Town.Far, s, piece);
            }
        }

        private static void Sidewalks(OutdoorBatches into, Street s, List<Vector2> pts)
        {
            float halfRoad = Mathf.Clamp(s.Data.width, 1.2f, 30f) * 0.5f;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                Vector2 dir = (b - a).normalized;
                Vector2 normal = new Vector2(-dir.y, dir.x);
                float length = Vector2.Distance(a, b);
                Vector2 mid = (a + b) * 0.5f;
                float yaw = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;   // the boxes' length runs along the street
                Vector3 size = new Vector3(length, 0.04f, SidewalkWidth);
                Vector3 snowSize = new Vector3(length, SnowDepth, SidewalkWidth);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 at = mid + normal * ((halfRoad + SidewalkWidth * 0.5f) * side);
                    RaidMesh m = into.Flat(To3(at, 0f));
                    m.Box(To3(at, StreetY + 0.01f), size, yaw, DecorMaterials.Concrete);
                    // thin snow overlay on the sidewalk
                    m.Box(To3(at, StreetY + 0.01f + 0.02f + SnowDepth * 0.5f), snowSize, yaw, DecorMaterials.Snow);
                }
            }
        }

        #region Exits and spawn
        // Where streets leave the playable square: the best-ranked street on each side becomes an exit (two at least;
        // a side without a street gets one through the snow).
        private static void DefineExits(Site site) => DefineExits(site.Area, site.Half, site.Town.Info);

        /// <summary>The exits where <paramref name="area"/>'s streets cross the square of half-side
        /// <paramref name="half"/> around its middle (read from the data alone, so a streamed map can place them before
        /// the ground they stand on is built).</summary>
        internal static void DefineExits(Area area, float half, RaidMapInfo info)
        {
            float edge = half - 3f;
            var square = Rect.MinMaxRect(-edge, -edge, edge, edge);
            var best = new float[4];
            var at = new Vector2[4];
            var names = new string[4];
            for (int i = 0; i < 4; i++) best[i] = float.MinValue;
            foreach (Street s in area.Streets)
            {
                int rank = Rank(s.Data.kind);
                if (rank <= 0 || s.Data.tunnel) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square))
                {
                    for (int end = 0; end < 2; end++)
                    {
                        Vector2 p = end == 0 ? piece[0] : piece[piece.Count - 1];
                        int side = SideOf(p, edge);
                        if (side < 0) continue;
                        float score = rank * 1000f - (side == 0 || side == 2 ? Mathf.Abs(p.x) : Mathf.Abs(p.y));
                        if (score <= best[side]) continue;
                        best[side] = score;
                        at[side] = p;
                        names[side] = string.IsNullOrEmpty(s.Data.name) ? KindName(s.Data.kind) : s.Data.name;
                    }
                }
            }
            int found = 0;
            for (int i = 0; i < 4; i++) if (best[i] > float.MinValue) found++;
            for (int side = 0; side < 4; side++)
            {
                if (best[side] == float.MinValue)
                {
                    if (found >= 2) continue;
                    found++;
                    at[side] = SideMiddle(side, edge);
                    names[side] = null;
                }
                info.Exits.Add(new RaidExit
                {
                    Name = names[side] != null ? names[side] + " (" + SideName(side) + ")" : "Snowfield (" + SideName(side) + ")",
                    Position = To3(at[side], 0f), Radius = 4.5f, ExtractSeconds = 8f, Side = side,
                    Hint = names[side] != null ? "Where " + names[side] + " leaves " + area.Place
                        : "Across the snow, " + SideName(side) + " edge of " + area.Place,
                });
            }
        }

        private static int SideOf(Vector2 p, float edge)
        {
            if (Mathf.Abs(p.y - edge) < 0.01f) return 0;
            if (Mathf.Abs(p.x - edge) < 0.01f) return 1;
            if (Mathf.Abs(p.y + edge) < 0.01f) return 2;
            if (Mathf.Abs(p.x + edge) < 0.01f) return 3;
            return -1;
        }

        private static Vector2 SideMiddle(int side, float edge)
        {
            switch (side)
            {
                case 0: return new Vector2(0f, edge);
                case 1: return new Vector2(edge, 0f);
                case 2: return new Vector2(0f, -edge);
                default: return new Vector2(-edge, 0f);
            }
        }

        private static string SideName(int side) => side == 0 ? "north" : side == 1 ? "east" : side == 2 ? "south" : "west";

        /// <summary>How much a street kind counts as a way out of town (0: not at all).</summary>
        private static int Rank(string kind)
        {
            switch (kind)
            {
                case "motorway": case "trunk": return 6;
                case "primary": return 5;
                case "secondary": return 4;
                case "tertiary": return 3;
                case "unclassified": case "residential": return 2;
                case "service": case "track": return 1;
                default: return 0;
            }
        }

        private static string KindName(string kind)
        {
            switch (kind)
            {
                case "motorway": case "trunk": return "The highway";
                case "primary": case "secondary": return "The state road";
                case "track": return "A farm track";
                case "service": return "A service road";
                default: return "A side street";
            }
        }

        // The survivor starts on the sidewalk of the street nearest the place's centre, facing along it.
        private static void PickSpawn(RaidTown town, Site site, out Vector2 spot, out float yaw)
            => PickSpawn(site.Area, site.Half, p => site.FreeBeside(p, 0.6f), out spot, out yaw);

        /// <summary>The sidewalk spot nearest the middle of <paramref name="area"/> (at least 5 m inside the square of
        /// half-side <paramref name="half"/>) that <paramref name="free"/> finds clear, facing along its street.</summary>
        internal static void PickSpawn(Area area, float half, System.Func<Vector2, bool> free, out Vector2 spot, out float yaw)
        {
            spot = Vector2.zero;
            yaw = 0f;
            float bestDist = float.MaxValue;
            foreach (Street s in area.Streets)
            {
                if (Rank(s.Data.kind) < 2 || s.Data.tunnel) continue;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], b = s.Pts[i + 1];
                    Vector2 ab = b - a;
                    if (ab.sqrMagnitude < 0.01f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(-a, ab) / ab.sqrMagnitude);
                    Vector2 d = ab.normalized;
                    Vector2 p = a + ab * t + new Vector2(-d.y, d.x) * (s.Data.width * 0.5f + 1.2f);
                    float dist = p.magnitude;
                    if (dist >= bestDist || Mathf.Abs(p.x) > half - 5f || Mathf.Abs(p.y) > half - 5f
                        || !free(p)) continue;
                    bestDist = dist;
                    spot = p;
                    yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                }
            }
        }

        private static void PickSpawn(Site site)
        {
            PickSpawn(site.Town, site, out Vector2 spot, out float yaw);
            site.Town.Info.PlayerSpawn = To3(spot, 0.6f);
            site.Town.Info.PlayerSpawnYaw = yaw;
        }
        #endregion

        #region The place's data, in raid-map coordinates
        /// <summary>A street piece: its data and its points.</summary>
        internal sealed class Street
        {
            public readonly MapStreet Data;
            public readonly List<Vector2> Pts;
            public Street(MapStreet data, List<Vector2> pts) { Data = data; Pts = pts; }
        }

        /// <summary>A river, stream or railway piece.</summary>
        internal sealed class Line
        {
            public readonly MapLine Data;
            public readonly List<Vector2> Pts;
            public Line(MapLine data, List<Vector2> pts) { Data = data; Pts = pts; }
        }

        /// <summary>A building's footprint, wound anticlockwise seen from above.</summary>
        internal sealed class Footprint
        {
            public readonly MapBuilding Data;
            public readonly List<Vector2> Pts;
            public readonly Vector2 Middle;
            public readonly Rect Bounds;
            public readonly float Size;
            /// <summary>A streamed map piece builds this building whole and playable even where its outline reaches past
            /// the piece's edge (each building has exactly one owning piece).</summary>
            public bool Owned;

            public Footprint(MapBuilding data, List<Vector2> pts)
            {
                Data = data;
                Pts = pts;
                MapGeometry.MakeAnticlockwise(pts);
                Middle = MapGeometry.Middle(pts);
                Size = Mathf.Abs(MapGeometry.SignedArea(pts));
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
                foreach (Vector2 p in pts)
                {
                    x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.y);
                    x1 = Mathf.Max(x1, p.x); z1 = Mathf.Max(z1, p.y);
                }
                Bounds = Rect.MinMaxRect(x0, z0, x1, z1);
            }

            /// <summary>True when the whole footprint is inside the playable square (less a margin), or the building is
            /// <see cref="Owned"/> by a streamed piece.</summary>
            public bool Inside(float half, float margin) => Owned || (Bounds.xMin > -half + margin && Bounds.xMax < half - margin
                && Bounds.yMin > -half + margin && Bounds.yMax < half - margin);
        }

        /// <summary>A land-use area (water, parking, forest...).</summary>
        internal sealed class Patch
        {
            public readonly MapArea Data;
            public readonly List<Vector2> Pts;
            public Patch(MapArea data, List<Vector2> pts) { Data = data; Pts = pts; }
        }

        /// <summary>A point of interest.</summary>
        internal sealed class Spot
        {
            public readonly MapPoi Data;
            public readonly Vector2 At;
            public Spot(MapPoi data, Vector2 at) { Data = data; At = at; }
        }

        /// <summary>Everything within reach of the place's centre, read from the build's regions and moved to raid-map
        /// coordinates. Streets, lines and areas are cut to their own region (the builder overlaps neighbours a little)
        /// and to the reach, so nothing is drawn twice. A streamed map piece's area holds exactly its own square
        /// (see the chunk constructor).</summary>
        internal sealed class Area
        {
            public readonly string Place, BuildId, Attribution;
            public readonly float Half;
            internal readonly List<Street> Streets = new List<Street>();
            internal readonly List<Line> Lines = new List<Line>();
            internal readonly List<Footprint> Buildings = new List<Footprint>();
            /// <summary>A streamed piece's neighbours' buildings that reach into it: kept clear, never built here.</summary>
            internal readonly List<Footprint> Foreign = new List<Footprint>();
            internal readonly List<Patch> Areas = new List<Patch>();
            internal readonly List<Spot> Points = new List<Spot>();

            public Area(MapBuild build, MapPlace place, float half)
                : this(build, place, half, half + SceneryReach, build.RegionsAround) { }

            /// <summary>The square of half-side <paramref name="half"/> around <paramref name="place"/>, with everything
            /// out to <paramref name="reach"/>, from the regions <paramref name="regionsAround"/> returns for a grid point
            /// and a half-side.</summary>
            public Area(MapBuild build, MapPlace place, float half, float reach,
                System.Func<double, double, double, List<MapRegion>> regionsAround)
            {
                Place = place.name;
                BuildId = build.Id;
                Attribution = build.Attribution;
                Half = half;
                var near = Rect.MinMaxRect(-reach, -reach, reach, reach);
                int size = build.RegionSize;
                foreach (MapRegion r in regionsAround(place.x, place.z, reach))
                {
                    double ox = (double)r.rx * size - place.x, oz = (double)r.rz * size - place.z;
                    var own = Rect.MinMaxRect((float)ox, (float)oz, (float)(ox + size), (float)(oz + size));
                    if (r.streets != null)
                        foreach (MapStreet s in r.streets)
                            foreach (List<Vector2> mine in MapGeometry.ClipLine(ToLocal(s.pts, ox, oz), own))
                                foreach (List<Vector2> piece in MapGeometry.ClipLine(mine, near))
                                    Streets.Add(new Street(s, piece));
                    if (r.lines != null)
                        foreach (MapLine l in r.lines)
                            foreach (List<Vector2> mine in MapGeometry.ClipLine(ToLocal(l.pts, ox, oz), own))
                                foreach (List<Vector2> piece in MapGeometry.ClipLine(mine, near))
                                    Lines.Add(new Line(l, piece));
                    if (r.buildings != null)
                        foreach (MapBuilding b in r.buildings)
                        {
                            List<Vector2> pts = ToLocal(b.pts, ox, oz);
                            if (pts.Count < 3) continue;
                            Vector2 mid = MapGeometry.Middle(pts);
                            if (Mathf.Abs(mid.x) < reach && Mathf.Abs(mid.y) < reach) Buildings.Add(new Footprint(b, pts));
                        }
                    if (r.areas != null)
                        foreach (MapArea a in r.areas)
                        {
                            List<Vector2> pts = MapGeometry.ClipPolygon(MapGeometry.ClipPolygon(ToLocal(a.pts, ox, oz), own), near);
                            if (pts.Count >= 3) Areas.Add(new Patch(a, pts));
                        }
                    if (r.pois != null)
                        foreach (MapPoi p in r.pois)
                        {
                            var at = new Vector2((float)(ox + p.x * 0.1), (float)(oz + p.z * 0.1));
                            if (near.Contains(at)) Points.Add(new Spot(p, at));
                        }
                }
            }

            /// <summary>One piece (chunk) of a streamed map: the square of chunk (<paramref name="cx"/>,
            /// <paramref name="cz"/>) in <paramref name="region"/>, in metres from the square's middle. Its streets, lines
            /// and areas are cut to the square, so neighbouring pieces meet without overlapping; it holds the buildings it
            /// owns whole (see <see cref="MapRegionIndex"/>) and, as <see cref="Foreign"/>, the outlines of its
            /// neighbours' buildings that reach into it. Safe off the main thread.</summary>
            public Area(string place, string buildId, string attribution, MapRegionIndex region, int cx, int cz)
            {
                Place = place;
                BuildId = buildId;
                Attribution = attribution;
                float chunk = region.ChunkSize;
                Half = chunk * 0.5f;
                if (region.Data == null) return;
                var near = Rect.MinMaxRect(-Half, -Half, Half, Half);
                double ox = (double)region.Rx * region.Size - (cx + 0.5) * chunk;
                double oz = (double)region.Rz * region.Size - (cz + 0.5) * chunk;
                var own = Rect.MinMaxRect((float)ox, (float)oz, (float)(ox + region.Size), (float)(oz + region.Size));
                MapRegion r = region.Data;
                MapRegionIndex.Bucket bucket = region.Get(cx, cz);
                foreach (int i in bucket.Streets)
                    foreach (List<Vector2> mine in MapGeometry.ClipLine(ToLocal(r.streets[i].pts, ox, oz), own))
                        foreach (List<Vector2> piece in MapGeometry.ClipLine(mine, near))
                            Streets.Add(new Street(r.streets[i], piece));
                foreach (int i in bucket.Lines)
                    foreach (List<Vector2> mine in MapGeometry.ClipLine(ToLocal(r.lines[i].pts, ox, oz), own))
                        foreach (List<Vector2> piece in MapGeometry.ClipLine(mine, near))
                            Lines.Add(new Line(r.lines[i], piece));
                foreach (int i in bucket.Owned)
                {
                    List<Vector2> pts = ToLocal(r.buildings[i].pts, ox, oz);
                    if (pts.Count >= 3) Buildings.Add(new Footprint(r.buildings[i], pts) { Owned = true });
                }
                foreach (int i in bucket.Foreign)
                {
                    List<Vector2> pts = ToLocal(r.buildings[i].pts, ox, oz);
                    if (pts.Count >= 3) Foreign.Add(new Footprint(r.buildings[i], pts));
                }
                foreach (int i in bucket.Areas)
                {
                    List<Vector2> pts = MapGeometry.ClipPolygon(MapGeometry.ClipPolygon(ToLocal(r.areas[i].pts, ox, oz), own), near);
                    if (pts.Count >= 3) Areas.Add(new Patch(r.areas[i], pts));
                }
                foreach (int i in bucket.Pois)
                {
                    MapPoi p = r.pois[i];
                    Points.Add(new Spot(p, new Vector2((float)(ox + p.x * 0.1), (float)(oz + p.z * 0.1))));
                }
            }

            private static List<Vector2> ToLocal(int[] pts, double ox, double oz)
            {
                var list = new List<Vector2>(pts != null ? pts.Length / 2 : 0);
                if (pts == null) return list;
                for (int i = 0; i + 1 < pts.Length; i += 2) list.Add(new Vector2((float)(ox + pts[i] * 0.1), (float)(oz + pts[i + 1] * 0.1)));
                return list;
            }
        }

        /// <summary>What the build shares while it runs: the town, the place, a coarse map of where things already
        /// stand (so props keep clear of walls, roads and each other), the overhead batches and the tallies.</summary>
        internal sealed class Site
        {
            public readonly RaidTown Town;
            internal readonly Area Area;
            public readonly float Half;
            /// <summary>A piece (chunk) of a streamed map: no scenery past its edge, its owned buildings built whole.</summary>
            public readonly bool Chunked;
            /// <summary>Where the site's middle ends up in the world. The site is built at the origin (so the builders'
            /// local and world positions agree) and its roots are moved here afterwards; anything registered in world
            /// space while it's built (the shelters) is moved by this.</summary>
            public readonly Vector3 WorldOffset;
            internal readonly ChunkedMesh Overhead = new ChunkedMesh(40f);
            public readonly Transform Loot;
            public int Buildings, SceneryBuildings, Containers, Trees;

            private const float Cell = 1f;
            private readonly int _n;
            private readonly bool[] _taken;
            // The roads' bands, kept apart from _taken so roadside props can test the roads exactly (FreeBeside).
            private readonly bool[] _road;
            private readonly List<(List<Vector2> pts, float half, Rect bounds)> _roads = new List<(List<Vector2>, float, Rect)>();

            public Site(RaidTown town, Area area) : this(town, area, false, Vector3.zero) { }

            public Site(RaidTown town, Area area, bool chunked, Vector3 worldOffset)
            {
                Town = town;
                Area = area;
                Half = area.Half;
                Chunked = chunked;
                WorldOffset = worldOffset;
                _n = Mathf.CeilToInt(Half * 2f / Cell) + 1;
                _taken = new bool[_n * _n];
                _road = new bool[_n * _n];
                Loot = new GameObject("Loot").transform;
                Loot.SetParent(town.World, false);
                foreach (Footprint f in area.Foreign) Take(f.Bounds);
            }

            /// <summary>This site's share of the fixed 400 m square the map's per-map limits were tuned for.</summary>
            public float AreaShare => Half * Half / (200f * 200f);

            /// <summary>A box in site coordinates, in the world.</summary>
            public Bounds ToWorld(Bounds b)
            {
                b.center += WorldOffset;
                return b;
            }

            private int Index(float v) => Mathf.FloorToInt((v + Half) / Cell);

            /// <summary>Marks a disc as taken.</summary>
            public void Take(Vector2 p, float radius) => Mark(_taken, p, radius);

            private void Mark(bool[] grid, Vector2 p, float radius)
            {
                int r = Mathf.CeilToInt(radius / Cell);
                int cx = Index(p.x), cz = Index(p.y);
                for (int z = cz - r; z <= cz + r; z++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if (x >= 0 && z >= 0 && x < _n && z < _n) grid[z * _n + x] = true;
            }

            /// <summary>Marks a rectangle (world XZ) as taken.</summary>
            public void Take(Rect r)
            {
                for (int z = Mathf.Max(0, Index(r.yMin)); z <= Mathf.Min(_n - 1, Index(r.yMax)); z++)
                    for (int x = Mathf.Max(0, Index(r.xMin)); x <= Mathf.Min(_n - 1, Index(r.xMax)); x++)
                        _taken[z * _n + x] = true;
            }

            /// <summary>Marks a band along a line as taken.</summary>
            public void TakeLine(List<Vector2> pts, float radius) => MarkLine(_taken, pts, radius);

            /// <summary>Marks a road whose surface reaches <paramref name="half"/> either side of its line: for
            /// <see cref="Free"/> as a band <paramref name="margin"/> wider (which the coarse grid rounds out to a metre
            /// or two past the kerb), for <see cref="FreeBeside"/> exactly.</summary>
            public void TakeRoad(List<Vector2> pts, float half, float margin)
            {
                MarkLine(_road, pts, half + margin);
                if (pts.Count < 2) return;
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
                foreach (Vector2 p in pts)
                {
                    x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.y);
                    x1 = Mathf.Max(x1, p.x); z1 = Mathf.Max(z1, p.y);
                }
                _roads.Add((pts, half, Rect.MinMaxRect(x0 - half, z0 - half, x1 + half, z1 + half)));
            }

            private void MarkLine(bool[] grid, List<Vector2> pts, float radius)
            {
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    float len = Vector2.Distance(pts[i], pts[i + 1]);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(len / (Cell * 0.5f)));
                    for (int k = 0; k <= steps; k++) Mark(grid, Vector2.Lerp(pts[i], pts[i + 1], k / (float)steps), radius);
                }
            }

            /// <summary>True when the disc is inside the playable square and nothing stands there yet.</summary>
            public bool Free(Vector2 p, float radius) => Clear(p, radius, true);

            /// <summary>For things that stand at the roadside (street lights, lamps, hydrants, snow piles): true when the
            /// disc is inside the playable square, clear of everything else standing and clear of every road's surface,
            /// tested exactly. (<see cref="Free"/> tests the roads by their coarse bands, which reach a metre or two past
            /// the kerb, so it never finds a spot at the roadside.)</summary>
            public bool FreeBeside(Vector2 p, float radius)
            {
                if (!Clear(p, radius, false)) return false;
                foreach ((List<Vector2> pts, float half, Rect bounds) in _roads)
                {
                    if (p.x < bounds.xMin - radius || p.x > bounds.xMax + radius || p.y < bounds.yMin - radius
                        || p.y > bounds.yMax + radius) continue;
                    float reach = half + radius;
                    for (int i = 0; i + 1 < pts.Count; i++)
                    {
                        Vector2 a = pts[i], ab = pts[i + 1] - a;
                        float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                        if ((a + ab * t - p).sqrMagnitude < reach * reach) return false;
                    }
                }
                return true;
            }

            private bool Clear(Vector2 p, float radius, bool roads)
            {
                if (Mathf.Abs(p.x) > Half - radius || Mathf.Abs(p.y) > Half - radius) return false;
                int r = Mathf.CeilToInt(radius / Cell);
                int cx = Index(p.x), cz = Index(p.y);
                for (int z = cz - r; z <= cz + r; z++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if (x >= 0 && z >= 0 && x < _n && z < _n && (_taken[z * _n + x] || (roads && _road[z * _n + x]))) return false;
                return true;
            }
        }

        /// <summary>Overhead geometry (upper storeys, ceilings, roofs) gathered in square chunks, so the camera culls
        /// what it can't see; each chunk's renderer is handed to <see cref="CeilingVisibility"/>.</summary>
        internal sealed class ChunkedMesh
        {
            private readonly float _chunk;
            private readonly Dictionary<long, RaidMesh> _meshes = new Dictionary<long, RaidMesh>();

            public ChunkedMesh(float chunk) { _chunk = chunk; }

            public RaidMesh At(Vector3 p)
            {
                long key = ((long)Mathf.FloorToInt(p.x / _chunk) << 32) | (uint)Mathf.FloorToInt(p.z / _chunk);
                if (!_meshes.TryGetValue(key, out RaidMesh m)) _meshes[key] = m = new RaidMesh();
                return m;
            }

            public void Emit(string name, Transform parent, bool ceilings)
            {
                foreach (KeyValuePair<long, RaidMesh> kv in _meshes)
                {
                    var c = new Vector3(((int)(kv.Key >> 32) + 0.5f) * _chunk, 0f, ((int)(kv.Key & 0xffffffff) + 0.5f) * _chunk);
                    GameObject go = kv.Value.Emit(name, parent, c, ShadowCastingMode.On);
                    if (go != null && ceilings) CeilingVisibility.Register(go.GetComponent<Renderer>());
                }
            }
        }
        #endregion
    }
}
