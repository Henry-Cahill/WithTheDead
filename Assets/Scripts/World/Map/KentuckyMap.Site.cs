using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The real-world map's data in raid-map coordinates (metres from the site's middle, x east, z north), and the site:
    // the build in progress that every pass draws on.
    public static partial class KentuckyMap
    {
        /// <summary>How far past the playable square a fixed (unstreamed) map draws its surroundings, as scenery.</summary>
        internal const float SceneryReach = 90f;

        #region The place's data
        /// <summary>A street piece: its data, its points and how the passes class it.</summary>
        internal sealed class Street
        {
            public readonly MapStreet Data;
            public readonly List<Vector2> Pts;
            /// <summary>How much the street counts as a road: 6 motorway or trunk, 5 primary, 4 secondary, 3 tertiary,
            /// 2 residential or unclassified, 1 service road or track, 0 anything else (paths, living streets...).</summary>
            public readonly int Rank;
            /// <summary>The carriageway's drawn width: as mapped, kept between 1.2 and 30 m.</summary>
            public readonly float Width;
            /// <summary>A street of the town proper (primary to residential, and living streets): the ones with street
            /// lights, hydrants and plowed snow.</summary>
            public readonly bool IsTownStreet;

            public Street(MapStreet data, List<Vector2> pts)
            {
                Data = data;
                Pts = pts;
                Rank = RankOf(data.kind);
                Width = Mathf.Clamp(data.width, 1.2f, 30f);
                IsTownStreet = InTown(data.kind);
            }

            private static int RankOf(string kind)
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

            private static bool InTown(string kind) => kind == "primary" || kind == "secondary" || kind == "tertiary"
                || kind == "residential" || kind == "unclassified" || kind == "living_street";
        }

        /// <summary>A piece of a line feature: a waterway or a railway (<see cref="Area.Lines"/>), a barrier
        /// (<see cref="Area.Barriers"/>) or a power line (<see cref="Area.Power"/>).</summary>
        internal sealed class Line
        {
            public readonly MapLine Data;
            public readonly List<Vector2> Pts;
            public Line(MapLine data, List<Vector2> pts) { Data = data; Pts = pts; }

            /// <summary>A railway (in use, disused or abandoned) rather than a waterway.</summary>
            public bool IsRailway => Data.kind == "rail" || Data.kind == "abandoned" || Data.kind == "disused";
        }

        /// <summary>A building's footprint, wound anticlockwise seen from above.</summary>
        internal sealed class Footprint
        {
            public readonly MapBuilding Data;
            public readonly List<Vector2> Pts;
            public readonly Vector2 Middle;
            public readonly Rect Bounds;
            /// <summary>The footprint's area in square metres.</summary>
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
                Bounds = MapGeometry.Bounds(pts);
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

        /// <summary>A mapped point feature (format 2): a tree, a street lamp, a stop sign, a hydrant...</summary>
        internal sealed class Node
        {
            public readonly MapNode Data;
            public readonly Vector2 At;
            public Node(MapNode data, Vector2 at) { Data = data; At = at; }
        }

        /// <summary>Everything within reach of the place's centre, read from the build's regions and moved to raid-map
        /// coordinates. Streets, lines (waterways, railways, barriers, power lines) and areas are cut to their own region
        /// (the builder overlaps neighbours a little) and to the reach, so nothing is drawn twice. A streamed map piece's
        /// area holds exactly its own square (see the chunk constructor). The format-2 lists stay empty for a format-1
        /// build.</summary>
        internal sealed class Area
        {
            public readonly string Place, BuildId, Attribution;
            public readonly float Half;
            internal readonly List<Street> Streets = new List<Street>();
            /// <summary>Waterways and railways.</summary>
            internal readonly List<Line> Lines = new List<Line>();
            internal readonly List<Footprint> Buildings = new List<Footprint>();
            /// <summary>A streamed piece's neighbours' buildings that reach into it: kept clear, never built here.</summary>
            internal readonly List<Footprint> Foreign = new List<Footprint>();
            internal readonly List<Patch> Areas = new List<Patch>();
            internal readonly List<Spot> Points = new List<Spot>();
            /// <summary>Fences, walls, hedges, guard rails, retaining walls and tree rows (format 2).</summary>
            internal readonly List<Line> Barriers = new List<Line>();
            /// <summary>Power lines (format 2). Every mapped vertex is a support, but cutting a line adds vertices where
            /// it was cut, on the edge of its region or of the area: supports go only at vertices strictly inside those.</summary>
            internal readonly List<Line> Power = new List<Line>();
            /// <summary>Mapped point features: trees, street lamps, signs, hydrants... (format 2).</summary>
            internal readonly List<Node> Nodes = new List<Node>();

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
                        foreach (MapLine l in r.lines) AddLine(Lines, l, ox, oz, own, near);
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
                            Vector2 at = ToLocal(p.x, p.z, ox, oz);
                            if (near.Contains(at)) Points.Add(new Spot(p, at));
                        }
                    if (r.barriers != null)
                        foreach (MapLine l in r.barriers) AddLine(Barriers, l, ox, oz, own, near);
                    if (r.power != null)
                        foreach (MapLine l in r.power) AddLine(Power, l, ox, oz, own, near);
                    if (r.nodes != null)
                        foreach (MapNode n in r.nodes)
                        {
                            Vector2 at = ToLocal(n.x, n.z, ox, oz);
                            if (near.Contains(at)) Nodes.Add(new Node(n, at));
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
                foreach (int i in bucket.Lines) AddLine(Lines, r.lines[i], ox, oz, own, near);
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
                foreach (int i in bucket.Pois) Points.Add(new Spot(r.pois[i], ToLocal(r.pois[i].x, r.pois[i].z, ox, oz)));
                foreach (int i in bucket.Barriers) AddLine(Barriers, r.barriers[i], ox, oz, own, near);
                foreach (int i in bucket.Power) AddLine(Power, r.power[i], ox, oz, own, near);
                foreach (int i in bucket.Nodes) Nodes.Add(new Node(r.nodes[i], ToLocal(r.nodes[i].x, r.nodes[i].z, ox, oz)));
            }

            /// <summary>The unit direction from <paramref name="from"/> to the nearest street (tunnels aside) within
            /// <paramref name="within"/> metres, or zero when there is none.</summary>
            public Vector2 NearestStreetDirection(Vector2 from, float within)
            {
                float best = within;
                Vector2 dir = Vector2.zero;
                foreach (Street s in Streets)
                {
                    if (s.Data.tunnel) continue;
                    for (int i = 0; i + 1 < s.Pts.Count; i++)
                    {
                        Vector2 a = s.Pts[i], ab = s.Pts[i + 1] - a;
                        float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(from - a, ab) / ab.sqrMagnitude) : 0f;
                        Vector2 to = a + ab * t - from;
                        float dist = to.magnitude;
                        if (dist >= best || dist < 1e-3f) continue;
                        best = dist;
                        dir = to / dist;
                    }
                }
                return dir;
            }

            // A line's pieces inside its own region and the reach.
            private static void AddLine(List<Line> into, MapLine l, double ox, double oz, Rect own, Rect near)
            {
                foreach (List<Vector2> mine in MapGeometry.ClipLine(ToLocal(l.pts, ox, oz), own))
                    foreach (List<Vector2> piece in MapGeometry.ClipLine(mine, near))
                        into.Add(new Line(l, piece));
            }

            private static Vector2 ToLocal(int x, int z, double ox, double oz) => new Vector2((float)(ox + x * 0.1), (float)(oz + z * 0.1));

            private static List<Vector2> ToLocal(int[] pts, double ox, double oz)
            {
                var list = new List<Vector2>(pts != null ? pts.Length / 2 : 0);
                if (pts == null) return list;
                for (int i = 0; i + 1 < pts.Length; i += 2) list.Add(ToLocal(pts[i], pts[i + 1], ox, oz));
                return list;
            }
        }
        #endregion

        #region The build in progress
        /// <summary>What the build shares while it runs: the town, the place, a coarse map of where things already
        /// stand (so props keep clear of walls, roads and each other), the overhead batches, what one pass hands the
        /// next (the enterable buildings, the junctions) and the tallies.</summary>
        internal sealed class Site
        {
            /// <summary>The world-store build the site's data comes from.</summary>
            public readonly MapBuild Build;
            public readonly RaidTown Town;
            internal readonly Area Area;
            public readonly float Half;
            /// <summary>A piece (chunk) of a streamed map: no scenery past its edge, its owned buildings built whole.</summary>
            public readonly bool Chunked;
            /// <summary>How much of the site to build: Full near the survivors (and on the fixed square), Lite far off.</summary>
            public readonly Detail Detail;
            /// <summary>Where the site's middle ends up in the world. The site is built at the origin (so the builders'
            /// local and world positions agree) and its roots are moved here afterwards; anything registered in world
            /// space while it's built (the shelters) is moved by this.</summary>
            public readonly Vector3 WorldOffset;
            /// <summary>Upper storeys, ceilings and roofs: shown only from inside views.</summary>
            internal readonly ChunkedMesh Overhead = new ChunkedMesh(40f);
            /// <summary>Roofs and upper storeys that show from the isometric camera too (shared building shells: Lite
            /// sites, scenery), unlike <see cref="Overhead"/>.</summary>
            internal readonly ChunkedMesh Roofs = new ChunkedMesh(40f);
            /// <summary>The building shells handed out by <see cref="Shell"/>, in order.</summary>
            public readonly List<BuildingShell> Shells = new List<BuildingShell>();
            /// <summary>The root the loot containers go under.</summary>
            public readonly Transform Loot;
            /// <summary>The buildings <see cref="BuildingPass"/> made enterable, in the order it built them, for
            /// <see cref="InteriorPass"/> to furnish.</summary>
            public readonly List<EnterableBuilding> Enterables = new List<EnterableBuilding>();
            public int Buildings, SceneryBuildings, Containers, Trees;

            private const float Cell = 1f;
            private readonly int _n;
            private readonly bool[] _taken;
            // The roads' bands, kept apart from _taken so roadside props can test the roads exactly (FreeBeside).
            private readonly bool[] _road;
            private readonly List<(List<Vector2> pts, float half, Rect bounds)> _roads = new List<(List<Vector2>, float, Rect)>();
            private List<Junction> _junctions;

            /// <summary>The fixed square's site.</summary>
            public Site(MapBuild build, RaidTown town, Area area) : this(build, town, area, false, Vector3.zero) { }

            public Site(MapBuild build, RaidTown town, Area area, bool chunked, Vector3 worldOffset, Detail detail = Detail.Full)
            {
                Build = build;
                Town = town;
                Area = area;
                Half = area.Half;
                Chunked = chunked;
                Detail = detail;
                WorldOffset = worldOffset;
                _n = Mathf.CeilToInt(Half * 2f / Cell) + 1;
                _taken = new bool[_n * _n];
                _road = new bool[_n * _n];
                Loot = new GameObject("Loot").transform;
                Loot.SetParent(town.World, false);
                foreach (Footprint f in area.Foreign) Take(f.Bounds);
            }

            /// <summary>Where the site's streets meet (see <see cref="MapJunctions"/>), found the first time they're asked
            /// for.</summary>
            public List<Junction> Junctions => _junctions ??= MapJunctions.Find(this);

            /// <summary>The meshes a building's walls and roof go into (see <see cref="BuildingShell"/>); call once per
            /// building. <paramref name="scenery"/>: a building past the playable edge, drawn with the scenery.</summary>
            public BuildingShell Shell(Footprint footprint, bool scenery = false) => MapCutaway.ShellFor(this, footprint, scenery);

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
                MapGeometry.Extent(pts, out float x0, out float z0, out float x1, out float z1);
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

            /// <summary>For things that stand at the roadside (street lights, hydrants, snow piles): true when the disc is
            /// inside the playable square, clear of everything else standing and clear of every road's surface, tested
            /// exactly. (<see cref="Free"/> tests the roads by their coarse bands, which reach a metre or two past the
            /// kerb, so it never finds a spot at the roadside.)</summary>
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
        /// what it can't see; with <c>ceilings</c>, each chunk's renderer is handed to <see cref="CeilingVisibility"/>
        /// (shown only from inside views).</summary>
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
