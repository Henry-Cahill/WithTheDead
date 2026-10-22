using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        private const float SignPostHeight = 2.5f; // height of wooden street sign post

        /// <summary>A one-line summary of the last map built.</summary>
        public static string LastStats { get; private set; } = "";

        /// <summary>Builds the map for <paramref name="zone"/> under <paramref name="world"/>, or returns null with the
        /// reason in <paramref name="why"/>, having built nothing.</summary>
        public static RaidMapInfo Build(Transform world, ZoneDef zone, out string why)
        {
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
                BuildGround(site);
                BuildWaterAndLots(site);
                BuildStreets(site, build);
                BuildSidewalks(site);
                BuildLines(site);
                BuildBuildings(site);
                BuildingFacadeBuilder.BuildAll(site, build);



                BuildStreetLights(site);
                BuildParkedCars(site);
                BuildTrees(site);
                BuildPoints(site);
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

        // A failed build mustn't leave half a town for the NavMesh bake or the procedural map to land on.
        private static void Discard(Transform world, RaidTown town)
        {
            for (int i = world.childCount - 1; i >= 0; i--) Object.DestroyImmediate(world.GetChild(i).gameObject);
            if (town.Overhead != null) Object.DestroyImmediate(town.Overhead.gameObject);
            if (town.Scenery != null) Object.DestroyImmediate(town.Scenery.gameObject);
            CeilingVisibility.Clear();
        }

        private static Vector3 To3(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        // Added thin snow layer for roofs and streets
        private static void BuildSidewalks(Site site)
        {
            foreach (Street s in site.Area.Streets)
            {
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i];
                    Vector2 b = s.Pts[i + 1];
                    Vector2 dir = (b - a).normalized;
                    Vector2 normal = new Vector2(-dir.y, dir.x);
                    float halfRoad = s.Data.width * 0.5f;
                    float length = Vector2.Distance(a, b);
                    Vector2 mid = (a + b) * 0.5f;
                    // left sidewalk box
                    Vector2 leftMid = mid + normal * (halfRoad + SidewalkWidth * 0.5f);
                    Vector3 leftCenter = new Vector3(leftMid.x, StreetY + 0.01f, leftMid.y);
                    Vector3 size = new Vector3(length, 0.04f, SidewalkWidth);
                    site.Overhead.At(leftMid).Box(leftCenter, size, DecorMaterials.Concrete);
                    // thin snow overlay on left sidewalk
                    site.Overhead.At(leftMid).Box(new Vector3(leftMid.x, StreetY + 0.01f + 0.02f + SnowDepth * 0.5f, leftMid.y), new Vector3(length, SnowDepth, SidewalkWidth), DecorMaterials.Snow);
                    // right sidewalk box
                    Vector2 rightMid = mid - normal * (halfRoad + SidewalkWidth * 0.5f);
                    Vector3 rightCenter = new Vector3(rightMid.x, StreetY + 0.01f, rightMid.y);
                    site.Overhead.At(rightMid).Box(rightCenter, size, DecorMaterials.Concrete);
                    // thin snow overlay on right sidewalk
                    site.Overhead.At(rightMid).Box(new Vector3(rightMid.x, StreetY + 0.01f + 0.02f + SnowDepth * 0.5f, rightMid.y), new Vector3(length, SnowDepth, SidewalkWidth), DecorMaterials.Snow);
                }
            }
        }

private static void BuildSnow(Site site)
        {
            foreach (Footprint f in site.Area.Buildings)
            {
                if (!f.Inside(site.Half, 2f)) continue;
                // Deterministic snow presence based on building ID (approx 30% chance).
                unchecked
                {
                    uint hash = (uint)f.Data.id ^ 0x9e3779b9u;
                    hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                    bool hasSnow = (hash % 100) < 30;
                    if (!hasSnow) continue;
                }
                // Use mapped height if present, else fallback to 0.
                float roofY = f.Data.height > 0f ? f.Data.height : 0f;
                roofY += SnowDepth * 0.5f;
                Vector3 center = new Vector3(f.Middle.x, roofY, f.Middle.y);
                Vector3 size = new Vector3(f.Size, SnowDepth, f.Size);
                site.Overhead.At(f.Middle).Box(center, size, DecorMaterials.Snow);
            }
        }



        #region Exits and spawn
        // Where streets leave the playable square: the best-ranked street on each side becomes an exit (two at least;
        // a side without a street gets one through the snow).
        private static void DefineExits(Site site)
        {
            float edge = site.Half - 3f;
            var square = Rect.MinMaxRect(-edge, -edge, edge, edge);
            var best = new float[4];
            var at = new Vector2[4];
            var names = new string[4];
            for (int i = 0; i < 4; i++) best[i] = float.MinValue;
            foreach (Street s in site.Area.Streets)
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
                site.Town.Info.Exits.Add(new RaidExit
                {
                    Name = names[side] != null ? names[side] + " (" + SideName(side) + ")" : "Snowfield (" + SideName(side) + ")",
                    Position = To3(at[side], 0f), Radius = 4.5f, ExtractSeconds = 8f, Side = side,
                    Hint = names[side] != null ? "Where " + names[side] + " leaves " + site.Area.Place
                        : "Across the snow, " + SideName(side) + " edge of " + site.Area.Place,
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
        {
            spot = Vector2.zero;
            yaw = 0f;
            float bestDist = float.MaxValue;
            foreach (Street s in site.Area.Streets)
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
                    if (dist >= bestDist || Mathf.Abs(p.x) > site.Half - 5f || Mathf.Abs(p.y) > site.Half - 5f
                        || !site.Free(p, 0.6f)) continue;
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

            /// <summary>True when the whole footprint is inside the playable square (less a margin).</summary>
            public bool Inside(float half, float margin) => Bounds.xMin > -half + margin && Bounds.xMax < half - margin
                && Bounds.yMin > -half + margin && Bounds.yMax < half - margin;
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
        /// and to the reach, so nothing is drawn twice.</summary>
        internal sealed class Area
        {
            public readonly string Place, BuildId, Attribution;
            public readonly float Half;
            internal readonly List<Street> Streets = new List<Street>();
            internal readonly List<Line> Lines = new List<Line>();
            internal readonly List<Footprint> Buildings = new List<Footprint>();
            internal readonly List<Patch> Areas = new List<Patch>();
            internal readonly List<Spot> Points = new List<Spot>();

            public Area(MapBuild build, MapPlace place, float half)
            {
                Place = place.name;
                BuildId = build.Id;
                Attribution = build.Attribution;
                Half = half;
                float reach = half + SceneryReach;
                var near = Rect.MinMaxRect(-reach, -reach, reach, reach);
                int size = build.RegionSize;
                foreach (MapRegion r in build.RegionsAround(place.x, place.z, reach))
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
            internal readonly ChunkedMesh Overhead = new ChunkedMesh(40f);
            public readonly Transform Loot;
            public int Buildings, SceneryBuildings, Containers, Trees;

            private const float Cell = 1f;
            private readonly int _n;
            private readonly bool[] _taken;

            public Site(RaidTown town, Area area)
            {
                Town = town;
                Area = area;
                Half = area.Half;
                _n = Mathf.CeilToInt(Half * 2f / Cell) + 1;
                _taken = new bool[_n * _n];
                Loot = new GameObject("Loot").transform;
                Loot.SetParent(town.World, false);
            }

            private int Index(float v) => Mathf.FloorToInt((v + Half) / Cell);

            /// <summary>Marks a disc as taken.</summary>
            public void Take(Vector2 p, float radius)
            {
                int r = Mathf.CeilToInt(radius / Cell);
                int cx = Index(p.x), cz = Index(p.y);
                for (int z = cz - r; z <= cz + r; z++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if (x >= 0 && z >= 0 && x < _n && z < _n) _taken[z * _n + x] = true;
            }

            /// <summary>Marks a rectangle (world XZ) as taken.</summary>
            public void Take(Rect r)
            {
                for (int z = Mathf.Max(0, Index(r.yMin)); z <= Mathf.Min(_n - 1, Index(r.yMax)); z++)
                    for (int x = Mathf.Max(0, Index(r.xMin)); x <= Mathf.Min(_n - 1, Index(r.xMax)); x++)
                        _taken[z * _n + x] = true;
            }

            /// <summary>Marks a band along a line as taken.</summary>
            public void TakeLine(List<Vector2> pts, float radius)
            {
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    float len = Vector2.Distance(pts[i], pts[i + 1]);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(len / (Cell * 0.5f)));
                    for (int k = 0; k <= steps; k++) Take(Vector2.Lerp(pts[i], pts[i + 1], k / (float)steps), radius);
                }
            }

            /// <summary>True when the disc is inside the playable square and nothing stands there yet.</summary>
            public bool Free(Vector2 p, float radius)
            {
                if (Mathf.Abs(p.x) > Half - radius || Mathf.Abs(p.y) > Half - radius) return false;
                int r = Mathf.CeilToInt(radius / Cell);
                int cx = Index(p.x), cz = Index(p.y);
                for (int z = cz - r; z <= cz + r; z++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if (x >= 0 && z >= 0 && x < _n && z < _n && _taken[z * _n + x]) return false;
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
