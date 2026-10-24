using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>What a spot of ground is mapped as, for the nature passes (unmapped land is <see cref="None"/>).</summary>
    internal enum Land : byte
    {
        None, Water, Lot, Forest, Scrub, Wetland, Park, Cemetery, Grass, Meadow, Farmland, Farmyard, Orchard, Residential,
        Business, School, Golf, Pitch, Bare,
    }

    /// <summary>
    /// What the nature passes (<see cref="VegetationPass"/>, <see cref="VehiclePass"/>, <see cref="SnowPass"/>) know about
    /// a site, worked out once when the first of them asks (<see cref="For"/>): where the site lies on the state's grid,
    /// so woods, fence rows, kerbside parking and parking rows fall on one absolute lattice and every piece that builds
    /// a stretch of them builds the same trees and cars, meeting its neighbours along the seams; quick lookups of the
    /// roads (with their kerbs and sidewalks as the roads drew them), buildings, doors, land use and power lines round a
    /// point; the overhead wires the streetscape strung (read back from its meshes); how built up the ground is and how
    /// wooded the region; and the spots kept clear for one another (the roads' driveways, the driveways' cars, the lots'
    /// plough piles).
    /// </summary>
    internal sealed class NaturePlan
    {
        private static readonly ConditionalWeakTable<Site, NaturePlan> s_Plans = new ConditionalWeakTable<Site, NaturePlan>();
        private static readonly Dictionary<string, (double x, double z, bool found)> s_Places
            = new Dictionary<string, (double, double, bool)>();

        /// <summary>The site's plan, made the first time any nature pass asks for it.</summary>
        internal static NaturePlan For(Site site) => s_Plans.GetValue(site, s => new NaturePlan(s));

        public readonly Site Site;
        public readonly bool Lite;
        /// <summary>The playable square's half-side, and how far the site's data reaches (past the square on a fixed map,
        /// where the scenery ring gets its own, cheaper trees).</summary>
        public readonly float Half, Reach;
        /// <summary>Where the site's middle is on the state's grid (EPSG:3088 metres).</summary>
        public readonly double OriginX, OriginZ;
        /// <summary>The share of open, unmapped country the region's woods cover, 0 to 1 (thin in the Bluegrass, thick in
        /// the eastern mountains).</summary>
        public readonly float Woodland;
        private readonly int _regionSize;

        private NaturePlan(Site site)
        {
            Site = site;
            Lite = site.Detail == Detail.Lite;
            Half = site.Half;
            Reach = site.Chunked ? site.Half : site.Half + SceneryReach;
            _regionSize = site.Build != null ? site.Build.RegionSize : 8192;
            PlaceOrigin(site, out OriginX, out OriginZ);
            Woodland = RegionalWoodland(OriginX, OriginZ);
            _roadCells = new Buckets(Reach, BucketCell);
            _buildingCells = new Buckets(Reach, BucketCell);
            _doorCells = new Buckets(Reach, BucketCell);
            _powerCells = new Buckets(Reach, BucketCell);
            IndexRoads();
            IndexBuildings();
            IndexDoors();
            IndexPower();
            _landMin = -Reach - LandCell;
            _landN = Mathf.CeilToInt((Reach * 2f + LandCell * 2f) / LandCell);
            _land = new byte[_landN * _landN];
            _landRank = new byte[_landN * _landN];
            RasterLand();
            _dMin = -Reach - DensityCell;
            _dN = Mathf.CeilToInt((Reach * 2f + DensityCell * 2f) / DensityCell);
            _built = new float[_dN * _dN];
            _business = new float[_dN * _dN];
            Density();
            if (!Lite)
            {
                KeepRoadDriveways();
                IndexWires();
            }
        }

        // Where the site's middle is on the grid: its place's grid point plus the site's offset from it (a streamed piece
        // is placed relative to the start town, which is its area's place). Without the place, the site's own frame.
        private static void PlaceOrigin(Site site, out double x, out double z)
        {
            x = site.WorldOffset.x;
            z = site.WorldOffset.z;
            if (site.Build == null || string.IsNullOrEmpty(site.Area.Place)) return;
            string key = site.Build.Id + "|" + site.Area.Place;
            if (!s_Places.TryGetValue(key, out (double x, double z, bool found) p))
            {
                MapPlace place = null;
                try
                {
                    place = site.Build.FindPlace(site.Area.Place);
                }
                catch (Exception)
                {
                    place = null;
                }
                p = place != null ? (place.x, place.z, true) : (0d, 0d, false);
                s_Places[key] = p;
            }
            if (!p.found) return;
            x += p.x;
            z += p.z;
        }

        #region Frames and lattices
        public double AbsX(float x) => OriginX + x;
        public double AbsZ(float z) => OriginZ + z;
        public Vector2 Local(double ax, double az) => new Vector2((float)(ax - OriginX), (float)(az - OriginZ));

        /// <summary>Inside the playable square, at least <paramref name="margin"/> in from its edge.</summary>
        public bool InSquare(Vector2 p, float margin) => Mathf.Abs(p.x) <= Half - margin && Mathf.Abs(p.y) <= Half - margin;

        /// <summary>In a fixed map's scenery ring: past the playable square, within the data's reach.</summary>
        public bool InRing(Vector2 p) => !Site.Chunked && !InSquare(p, 0f) && Mathf.Abs(p.x) <= Reach && Mathf.Abs(p.y) <= Reach;

        /// <summary>A key for a pair of whole numbers (lattice cells, stations), for <see cref="MapStyle"/>'s hashes.</summary>
        public static long Combine(long a, long b) => unchecked(a * 73856093L ^ b * 19349663L ^ (a + b) * 83492791L);

        /// <summary>A point of the absolute lattice of <paramref name="cell"/> metres: one per cell, jittered inside it.</summary>
        internal struct CellPoint
        {
            public Vector2 At;
            public long Key;
        }

        /// <summary>The cells of the absolute lattice of <paramref name="cell"/> metres whose jittered points fall in
        /// <paramref name="r"/> (site metres), each with a key of its own. <paramref name="jitter"/> is the share of the
        /// cell a point may stray over.</summary>
        public void Cells(Rect r, float cell, float jitter, int salt, List<CellPoint> into)
        {
            into.Clear();
            long ix0 = (long)Math.Floor((AbsX(r.xMin) - cell) / cell), ix1 = (long)Math.Floor((AbsX(r.xMax) + cell) / cell);
            long iz0 = (long)Math.Floor((AbsZ(r.yMin) - cell) / cell), iz1 = (long)Math.Floor((AbsZ(r.yMax) + cell) / cell);
            for (long iz = iz0; iz <= iz1; iz++)
                for (long ix = ix0; ix <= ix1; ix++)
                {
                    long key = Combine(ix, iz);
                    double ax = (ix + 0.5 + (MapStyle.Unit(key, salt) - 0.5) * jitter) * cell;
                    double az = (iz + 0.5 + (MapStyle.Unit(key, salt + 1) - 0.5) * jitter) * cell;
                    Vector2 at = Local(ax, az);
                    if (r.Contains(at)) into.Add(new CellPoint { At = at, Key = key });
                }
        }

        /// <summary>A point along a line at a station of the absolute lattice (see <see cref="Stations"/>).</summary>
        internal struct Station
        {
            public Vector2 At, Dir;
            /// <summary>The station's number on its axis (with the axis in its lowest bit): the same in every piece of the
            /// line.</summary>
            public long K;
            public int Segment;
        }

        /// <summary>
        /// Points every <paramref name="spacing"/> metres or so along a line, where it crosses the lines of an absolute
        /// lattice on whichever axis its segment runs closer to: the same points, numbered alike, whichever piece of the
        /// line a site holds, so pieces cut at a seam carry on one another's rhythm.
        /// </summary>
        public void Stations(List<Vector2> pts, float spacing, List<Station> into)
        {
            into.Clear();
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                Vector2 d = b - a;
                float len = d.magnitude;
                if (len < 0.05f) continue;
                Vector2 dir = d / len;
                bool alongX = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
                double step = spacing * (alongX ? Mathf.Abs(dir.x) : Mathf.Abs(dir.y));
                double from = alongX ? AbsX(a.x) : AbsZ(a.y), to = alongX ? AbsX(b.x) : AbsZ(b.y);
                double lo = Math.Min(from, to), hi = Math.Max(from, to);
                long k0 = (long)Math.Ceiling(lo / step), k1 = (long)Math.Floor(hi / step);
                for (long k = k0; k <= k1; k++)
                {
                    double v = k * step;
                    if (v >= hi) continue;   // half-open, so a vertex on a station isn't counted twice
                    float t = (float)((v - from) / (to - from));
                    into.Add(new Station { At = a + d * t, Dir = dir, K = k * 2 + (alongX ? 0 : 1), Segment = i });
                }
            }
        }

        /// <summary>True for a vertex the clipping made: on the square's (or a fixed map's reach's) edge, or on a region's.</summary>
        public bool CutVertex(Vector2 p)
        {
            if (Mathf.Abs(Mathf.Abs(p.x) - Half) < 0.02f || Mathf.Abs(Mathf.Abs(p.y) - Half) < 0.02f) return true;
            if (!Site.Chunked && (Mathf.Abs(Mathf.Abs(p.x) - Reach) < 0.02f || Mathf.Abs(Mathf.Abs(p.y) - Reach) < 0.02f)) return true;
            double rx = AbsX(p.x) / _regionSize, rz = AbsZ(p.y) / _regionSize;
            return Math.Abs(rx - Math.Round(rx)) * _regionSize < 0.02 || Math.Abs(rz - Math.Round(rz)) * _regionSize < 0.02;
        }
        #endregion

        #region Noise and the region's woods
        /// <summary>Smooth value noise in [0, 1) on an absolute lattice of <paramref name="cell"/> metres.</summary>
        public static float Noise(double x, double z, double cell, int salt)
        {
            double fx = x / cell, fz = z / cell;
            long ix = (long)Math.Floor(fx), iz = (long)Math.Floor(fz);
            float tx = (float)(fx - ix), tz = (float)(fz - iz);
            tx = tx * tx * (3f - 2f * tx);
            tz = tz * tz * (3f - 2f * tz);
            float a = MapStyle.Unit(Combine(ix, iz), salt), b = MapStyle.Unit(Combine(ix + 1, iz), salt);
            float c = MapStyle.Unit(Combine(ix, iz + 1), salt), d = MapStyle.Unit(Combine(ix + 1, iz + 1), salt);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
        }

        /// <summary>The noise at a site point.</summary>
        public float NoiseAt(Vector2 p, double cell, int salt) => Noise(AbsX(p.x), AbsZ(p.y), cell, salt);

        private const int SaltWoods = 0x700D;
        private static float[] s_WoodsRanks;

        private static float WoodsRaw(double x, double z)
            => 0.55f * Noise(x, z, 620.0, SaltWoods) + 0.30f * Noise(x, z, 205.0, SaltWoods + 1) + 0.15f * Noise(x, z, 66.0, SaltWoods + 2);

        /// <summary>The woods' noise at a grid point, evened out to [0, 1) (its rank among samples taken all over the
        /// state), so "below the region's share" covers that share of the land.</summary>
        public static float WoodsNoise(double x, double z)
        {
            if (s_WoodsRanks == null)
            {
                var samples = new float[2048];
                for (int i = 0; i < samples.Length; i++)
                    samples[i] = WoodsRaw(1.0e6 + (i * 7919.37) % 8.0e5, 1.0e6 + (i * 104729.13) % 3.0e5);
                Array.Sort(samples);
                s_WoodsRanks = samples;
            }
            float raw = WoodsRaw(x, z);
            int at = Array.BinarySearch(s_WoodsRanks, raw);
            if (at < 0) at = ~at;
            return at / (float)s_WoodsRanks.Length;
        }

        /// <summary>
        /// True where unmapped land lies under the region's woods: the woods' noise below the region's share of woodland,
        /// which thins out where the ground is built up. <paramref name="depth"/> says how far in (0 at the woods' edge,
        /// where the cedars crowd).
        /// </summary>
        public bool Wooded(Vector2 p, out float depth)
        {
            depth = 0f;
            if (LandAt(p) != Land.None) return false;
            float share = Woodland * Mathf.Clamp01(1f - Urban(p) * 7f);
            if (share <= 0.01f) return false;
            float u = WoodsNoise(AbsX(p.x), AbsZ(p.y));
            depth = share - u;
            return u < share;
        }

        // The share of woodland round some of the state's towns (rough, from the land cover), for a smooth estimate
        // anywhere: the Bluegrass is open horse and cattle country, the Knobs and the Pennyroyal half wooded, the
        // eastern coalfield nearly all woods. Grid x, grid z, share.
        private static readonly double[] WoodAnchors =
        {
            1609968, 1190860, 0.12,   1604236, 1208942, 0.15,   1631420, 1209692, 0.15,   1637927, 1185030, 0.22,
            1576786, 1207630, 0.35,   1628246, 1157987, 0.30,   1586287, 1146096, 0.28,   1499177, 1213189, 0.25,
            1524927, 1163838, 0.42,   1490376, 1151010, 0.38,   1628409, 1138104, 0.55,   1601872, 1084829, 0.62,
            1648074, 1089555, 0.72,   1647244, 1069601, 0.76,   1726788, 1104727, 0.86,   1716463, 1059826, 0.88,
            1785725, 1132126, 0.86,   1762395, 1152205, 0.82,   1709180, 1138068, 0.86,   1702989, 1207910, 0.74,
            1771494, 1242599, 0.66,   1672835, 1258675, 0.42,   1607236, 1305921, 0.40,   1438320, 1073434, 0.30,
            1485536, 1073549, 0.36,   1536159, 1112180, 0.46,   1344900, 1060541, 0.24,   1379905, 1160790, 0.24,
            1345033, 1111844, 0.42,   1246629, 1087114, 0.30,   1271674, 1033898, 0.36,   1682138, 1032394, 0.86,
            1460000, 1100000, 0.74,   1676594, 1092929, 0.84,   1759748, 1091203, 0.88,
        };

        private static float RegionalWoodland(double x, double z)
        {
            double sum = 0.0, weights = 0.0;
            for (int i = 0; i + 2 < WoodAnchors.Length; i += 3)
            {
                double dx = x - WoodAnchors[i], dz = z - WoodAnchors[i + 1];
                double w = 1.0 / (dx * dx + dz * dz + 6.25e8);
                weights += w;
                sum += w * WoodAnchors[i + 2];
            }
            return (float)(sum / weights);
        }
        #endregion

        #region Roads
        /// <summary>A straight piece of a street (paths included), as the lookups see it.</summary>
        internal struct RoadSeg
        {
            public Vector2 A, B;
            /// <summary>Half the drawn width.</summary>
            public float Half;
            public int Street;
            public int Rank;
            /// <summary>A footway, path, cycleway, steps...: no carriageway.</summary>
            public bool Foot;
        }

        private const float BucketCell = 16f;
        private readonly List<RoadSeg> _roads = new List<RoadSeg>(256);
        private readonly Buckets _roadCells;
        private int[] _roadSeen;
        private int _roadTick;

        public IReadOnlyList<RoadSeg> Roads => _roads;

        private void IndexRoads()
        {
            List<Street> streets = Site.Area.Streets;
            for (int s = 0; s < streets.Count; s++)
            {
                Street st = streets[s];
                if (st.Data.tunnel) continue;
                bool foot = FootKind(st.Data.kind);
                for (int i = 0; i + 1 < st.Pts.Count; i++)
                {
                    Vector2 a = st.Pts[i], b = st.Pts[i + 1];
                    if ((b - a).sqrMagnitude < 1e-4f) continue;
                    float half = st.Width * 0.5f;
                    _roadCells.Add(_roads.Count, Rect.MinMaxRect(Mathf.Min(a.x, b.x) - half, Mathf.Min(a.y, b.y) - half,
                        Mathf.Max(a.x, b.x) + half, Mathf.Max(a.y, b.y) + half));
                    _roads.Add(new RoadSeg { A = a, B = b, Half = half, Street = s, Rank = st.Rank, Foot = foot });
                }
            }
            _roadSeen = new int[_roads.Count];
        }

        internal static bool FootKind(string kind) => kind == "footway" || kind == "path" || kind == "cycleway"
            || kind == "bridleway" || kind == "steps" || kind == "pedestrian" || kind == "track";

        /// <summary>How far <paramref name="p"/> is from the nearest road's edge (negative on it), looking no further
        /// than <paramref name="within"/> metres past an edge (and returning that when there's none so near). Paths and
        /// tracks count unless <paramref name="carriageways"/>.</summary>
        public float RoadClearance(Vector2 p, float within, bool carriageways = false)
        {
            float best = within;
            _roadTick++;
            _roadCells.Range(p, within, out int x0, out int z0, out int x1, out int z1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    List<int> cell = _roadCells.At(x, z);
                    if (cell == null) continue;
                    for (int k = 0; k < cell.Count; k++)
                    {
                        int i = cell[k];
                        if (_roadSeen[i] == _roadTick) continue;
                        _roadSeen[i] = _roadTick;
                        RoadSeg r = _roads[i];
                        if (carriageways && r.Foot) continue;
                        float d = MapGeometry.DistanceToSegment(p, r.A, r.B) - r.Half;
                        if (d < best) best = d;
                    }
                }
            return best;
        }

        /// <summary>The nearest road of at least <paramref name="minRank"/> (no paths) whose middle line is within
        /// <paramref name="within"/> metres: its piece, how far its middle line is and the point on it nearest
        /// <paramref name="p"/>.</summary>
        public bool NearestRoad(Vector2 p, float within, int minRank, out RoadSeg seg, out float dist, out Vector2 foot)
        {
            seg = default;
            foot = p;
            dist = within;
            bool found = false;
            _roadTick++;
            _roadCells.Range(p, within, out int x0, out int z0, out int x1, out int z1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    List<int> cell = _roadCells.At(x, z);
                    if (cell == null) continue;
                    for (int k = 0; k < cell.Count; k++)
                    {
                        int i = cell[k];
                        if (_roadSeen[i] == _roadTick) continue;
                        _roadSeen[i] = _roadTick;
                        RoadSeg r = _roads[i];
                        if (r.Foot || r.Rank < minRank) continue;
                        Vector2 ab = r.B - r.A;
                        float t = Mathf.Clamp01(Vector2.Dot(p - r.A, ab) / ab.sqrMagnitude);
                        Vector2 q = r.A + ab * t;
                        float d = Vector2.Distance(p, q);
                        if (d >= dist) continue;
                        dist = d;
                        seg = r;
                        foot = q;
                        found = true;
                    }
                }
            return found;
        }

        /// <summary>The street piece a <see cref="RoadSeg"/> belongs to.</summary>
        public Street StreetOf(in RoadSeg seg) => Site.Area.Streets[seg.Street];

        /// <summary>For the nearest street within 6 m of <paramref name="p"/> (a point by its edge), on the side
        /// <paramref name="away"/> points to: how far from the street's line its carriageway ends (<paramref name="edge"/>,
        /// its kerb face or edge) and its sidewalk does (<paramref name="back"/>, or, without one, the back of its kerb),
        /// as the roads drew them (see <see cref="RoadPass.SidewalkOuter"/>). False when there's no street so near.</summary>
        public bool Roadside(Vector2 p, Vector2 away, out float edge, out float back)
        {
            edge = back = 0f;
            if (!NearestRoad(p, 6f, 1, out RoadSeg seg, out float _, out Vector2 _)) return false;
            Street st = StreetOf(seg);
            Vector2 d = seg.B - seg.A;
            int side = d.x * away.y - d.y * away.x >= 0f ? 1 : -1;
            edge = RoadPass.EdgeOffset(st, side);
            float outer = RoadPass.SidewalkOuter(st, side);
            back = Mathf.Max(edge, outer > 0f ? outer : RoadPass.KerbOffset(st, side));
            return true;
        }

        /// <summary>True when <paramref name="p"/> is on the nearest street's sidewalk (from the back of its kerb to the
        /// sidewalk's outer edge, as the roads drew them), or within <paramref name="margin"/> of it.</summary>
        public bool OnSidewalk(Vector2 p, float margin)
        {
            if (!NearestRoad(p, 15f, 1, out RoadSeg seg, out float dist, out Vector2 foot)) return false;
            Street st = StreetOf(seg);
            Vector2 d = seg.B - seg.A;
            int side = d.x * (p.y - foot.y) - d.y * (p.x - foot.x) >= 0f ? 1 : -1;
            float outer = RoadPass.SidewalkOuter(st, side);
            return outer > 0f && dist > RoadPass.KerbOffset(st, side) - margin && dist < outer + margin;
        }
        #endregion

        #region Buildings
        private readonly List<Footprint> _buildings = new List<Footprint>(128);
        private readonly Buckets _buildingCells;
        private int[] _buildingSeen;
        private int _buildingTick;

        /// <summary>Every footprint the site knows: its own and, on a streamed piece, its neighbours' reaching in.</summary>
        public IReadOnlyList<Footprint> Buildings => _buildings;

        private void IndexBuildings()
        {
            foreach (Footprint f in Site.Area.Buildings) AddBuilding(f);
            foreach (Footprint f in Site.Area.Foreign) AddBuilding(f);
            _buildingSeen = new int[_buildings.Count];
        }

        private void AddBuilding(Footprint f)
        {
            if (f.Pts.Count < 3) return;
            _buildingCells.Add(_buildings.Count, f.Bounds);
            _buildings.Add(f);
        }

        /// <summary>How far <paramref name="p"/> is from the nearest building's wall (negative inside one), looking no
        /// further than <paramref name="within"/> (and returning that when there's none so near).</summary>
        public float WallDistance(Vector2 p, float within) => WallDistance(p, within, out Footprint _);

        public float WallDistance(Vector2 p, float within, out Footprint nearest)
        {
            nearest = null;
            float best = within;
            _buildingTick++;
            _buildingCells.Range(p, within, out int x0, out int z0, out int x1, out int z1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    List<int> cell = _buildingCells.At(x, z);
                    if (cell == null) continue;
                    for (int k = 0; k < cell.Count; k++)
                    {
                        int i = cell[k];
                        if (_buildingSeen[i] == _buildingTick) continue;
                        _buildingSeen[i] = _buildingTick;
                        Footprint f = _buildings[i];
                        Rect b = f.Bounds;
                        if (p.x < b.xMin - best || p.x > b.xMax + best || p.y < b.yMin - best || p.y > b.yMax + best) continue;
                        float d = MapGeometry.DistanceToEdge(f.Pts, p);
                        if (MapGeometry.Contains(f.Pts, p)) d = -d;
                        if (d >= best) continue;
                        best = d;
                        nearest = f;
                    }
                }
            return best;
        }

        /// <summary>The footprints whose bounds come within <paramref name="within"/> of <paramref name="p"/>.</summary>
        public void BuildingsNear(Vector2 p, float within, List<Footprint> into)
        {
            into.Clear();
            _buildingTick++;
            _buildingCells.Range(p, within, out int x0, out int z0, out int x1, out int z1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    List<int> cell = _buildingCells.At(x, z);
                    if (cell == null) continue;
                    for (int k = 0; k < cell.Count; k++)
                    {
                        int i = cell[k];
                        if (_buildingSeen[i] == _buildingTick) continue;
                        _buildingSeen[i] = _buildingTick;
                        Rect b = _buildings[i].Bounds;
                        if (p.x < b.xMin - within || p.x > b.xMax + within || p.y < b.yMin - within || p.y > b.yMax + within) continue;
                        into.Add(_buildings[i]);
                    }
                }
        }

        /// <summary>Homes: houses, trailers, apartments (the yards with trees, the driveways with cars).</summary>
        public static bool IsHome(MapBuilding b) => b.kind == "house" || b.kind == "trailer" || b.kind == "apartments";

        /// <summary>Shops, offices, works, churches, schools and the like: the frontages cars crowd in front of.</summary>
        public static bool IsBusiness(MapBuilding b)
        {
            switch (b.kind)
            {
                case "commercial": case "retail": case "office": case "industrial": case "warehouse": case "church":
                case "school": case "hospital": case "civic": case "hotel":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>The nearest wall within <paramref name="within"/> of <paramref name="p"/> (outside every building): the
        /// unit plan direction towards it and how far it is. False when there's none so near (or p is inside one).</summary>
        public bool NearestWall(Vector2 p, float within, out Vector2 dir, out float dist)
        {
            dir = Vector2.zero;
            dist = WallDistance(p, within, out Footprint f);
            if (f == null || dist <= 0.01f) return false;
            List<Vector2> pts = f.Pts;
            float best = float.MaxValue;
            Vector2 on = p;
            for (int i = 0, k = pts.Count - 1; i < pts.Count; k = i++)
            {
                Vector2 a = pts[k], ab = pts[i] - a;
                float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                Vector2 q = a + ab * t;
                float d = (q - p).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                on = q;
            }
            Vector2 to = on - p;
            float len = to.magnitude;
            if (len < 1e-3f) return false;
            dir = to / len;
            return true;
        }
        #endregion

        #region Doors
        // Every enterable building's doors: the middle of each on its wall, and its doorstep a metre out.
        private readonly List<Vector2> _doors = new List<Vector2>(64), _doorWalls = new List<Vector2>(64);
        private readonly Buckets _doorCells;

        private void IndexDoors()
        {
            foreach (EnterableBuilding e in Site.Enterables)
                foreach (Doorway d in e.Doors)
                {
                    Vector2 at = d.At + d.Outward * 1f;
                    _doorCells.Add(_doors.Count, new Rect(at.x, at.y, 0f, 0f));
                    _doors.Add(at);
                    _doorWalls.Add(d.At);
                }
        }

        /// <summary>True when the middle of a door lies on the wall from <paramref name="a"/> to <paramref name="b"/>.</summary>
        public bool DoorOnWall(Vector2 a, Vector2 b)
        {
            Vector2 mid = (a + b) * 0.5f;
            _doorCells.Range(mid, Vector2.Distance(a, b) * 0.5f + 1.5f, out int x0, out int z0, out int x1, out int z1);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    List<int> cell = _doorCells.At(x, z);
                    if (cell == null) continue;
                    for (int k = 0; k < cell.Count; k++)
                        if (MapGeometry.DistanceToSegment(_doorWalls[cell[k]], a, b) < 0.4f) return true;
                }
            return false;
        }

        /// <summary>True when <paramref name="p"/> is within <paramref name="within"/> of a doorstep (a metre out from the
        /// middle of any enterable building's door): the way in is left clear.</summary>
        public bool NearDoor(Vector2 p, float within)
        {
            _doorCells.Range(p, within, out int x0, out int z0, out int x1, out int z1);
            float w2 = within * within;
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    List<int> cell = _doorCells.At(x, z);
                    if (cell == null) continue;
                    for (int k = 0; k < cell.Count; k++)
                        if ((_doors[cell[k]] - p).sqrMagnitude < w2) return true;
                }
            return false;
        }
        #endregion

        #region Power lines and overhead wires
        // The mapped power lines' pieces, each with the half-width of the strip kept clear of trees under it.
        private readonly List<(Vector2 a, Vector2 b, float half)> _power = new List<(Vector2, Vector2, float)>(16);
        private readonly Buckets _powerCells;

        private void IndexPower()
        {
            foreach (Line l in Site.Area.Power)
            {
                // A transmission line runs down a cleared right of way; a distribution line keeps its poles clear.
                float half = l.Data.kind == "line" ? 13f : 3f;
                for (int i = 0; i + 1 < l.Pts.Count; i++)
                {
                    Vector2 a = l.Pts[i], b = l.Pts[i + 1];
                    if ((b - a).sqrMagnitude < 1e-4f) continue;
                    _powerCells.Add(_power.Count, Rect.MinMaxRect(Mathf.Min(a.x, b.x) - half, Mathf.Min(a.y, b.y) - half,
                        Mathf.Max(a.x, b.x) + half, Mathf.Max(a.y, b.y) + half));
                    _power.Add((a, b, half));
                }
            }
        }

        /// <summary>True under a mapped power line's cleared strip: its right of way through the woods.</summary>
        public bool UnderPowerLine(Vector2 p)
        {
            if (_power.Count == 0) return false;
            _powerCells.Range(p, 0f, out int x0, out int z0, out int x1, out int z1);
            List<int> cell = _powerCells.At(x0, z0);
            if (cell == null) return false;
            for (int k = 0; k < cell.Count; k++)
            {
                (Vector2 a, Vector2 b, float half) = _power[cell[k]];
                if (MapGeometry.DistanceToSegment(p, a, b) < half) return true;
            }
            return false;
        }

        private const float WireCell = 2f;
        // The lowest overhead wire over each 2 m cell of a Full site's square, read back from the wires the streetscape
        // strung (see IndexWires).
        private readonly Dictionary<long, float> _wires = new Dictionary<long, float>();
        private static readonly List<Vector3> s_WireVerts = new List<Vector3>(4096);

        /// <summary>
        /// Reads the overhead wires back from the meshes the streetscape pass hung under the site's overhead root (one
        /// "Wires" object per cell; every three vertices a ring round a strand, and each ring three vertices on from the
        /// last along the same strand): each stretch between two rings marks the cells it passes over with its height,
        /// so the trees can keep their crowns off the lines. Nothing found (no wires, or a mesh that can't be read): no
        /// wires to keep off.
        /// </summary>
        private void IndexWires()
        {
            Transform root = Site.Town.Overhead;
            if (root == null) return;
            Matrix4x4 toSite = root.worldToLocalMatrix;
            for (int c = 0; c < root.childCount; c++)
            {
                Transform t = root.GetChild(c);
                if (t.name != "Wires") continue;
                var mf = t.GetComponent<MeshFilter>();
                Mesh mesh = mf != null ? mf.sharedMesh : null;
                if (mesh == null || !mesh.isReadable) continue;
                mesh.GetVertices(s_WireVerts);
                Matrix4x4 m = toSite * t.localToWorldMatrix;
                for (int i = 0; i + 3 < s_WireVerts.Count; i += 3)
                {
                    Vector3 a = m.MultiplyPoint3x4(s_WireVerts[i]), b = m.MultiplyPoint3x4(s_WireVerts[i + 3]);
                    float len = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                    if (len > 8f || float.IsNaN(len)) continue;   // the next strand's first ring
                    int steps = Mathf.Max(1, Mathf.CeilToInt(len / 1f));
                    for (int k = 0; k <= steps; k++)
                    {
                        Vector3 q = Vector3.Lerp(a, b, k / (float)steps);
                        if (!InSquare(new Vector2(q.x, q.z), -WireCell)) continue;
                        long key = Combine(Mathf.FloorToInt(q.x / WireCell), Mathf.FloorToInt(q.z / WireCell));
                        if (!_wires.TryGetValue(key, out float low) || q.y < low) _wires[key] = q.y;
                    }
                }
            }
            s_WireVerts.Clear();
        }

        /// <summary>The nearest overhead wire (in plan) within <paramref name="within"/> of <paramref name="p"/> hanging
        /// lower than <paramref name="below"/>: the unit direction towards it, how far it is and how high it hangs there.</summary>
        public bool WireNear(Vector2 p, float within, float below, out Vector2 dir, out float dist, out float height)
        {
            dir = Vector2.zero;
            dist = within;
            height = 0f;
            if (_wires.Count == 0) return false;
            int r = Mathf.CeilToInt(within / WireCell);
            int cx = Mathf.FloorToInt(p.x / WireCell), cz = Mathf.FloorToInt(p.y / WireCell);
            bool found = false;
            for (int z = cz - r; z <= cz + r; z++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (!_wires.TryGetValue(Combine(x, z), out float y) || y >= below) continue;
                    var at = new Vector2((x + 0.5f) * WireCell, (z + 0.5f) * WireCell);
                    float d = Vector2.Distance(at, p);
                    if (d >= dist) continue;
                    dist = d;
                    height = y;
                    dir = d > 1e-3f ? (at - p) / d : Vector2.up;
                    found = true;
                }
            return found;
        }
        #endregion

        #region Land use
        private const float LandCell = 2f;
        private readonly float _landMin;
        private readonly int _landN;
        private readonly byte[] _land, _landRank;
        private static readonly List<float> s_Crossings = new List<float>(32);

        // Which mapped use wins where areas overlap (a park in a residential area is the park).
        private static readonly byte[] LandRank =
        {
            0, 100, 90, 55, 53, 54, 70, 75, 45, 40, 38, 60, 58, 20, 30, 68, 72, 80, 52,
        };

        /// <summary>What the ground at <paramref name="p"/> is mapped as.</summary>
        public Land LandAt(Vector2 p)
        {
            int i = Mathf.FloorToInt((p.x - _landMin) / LandCell), j = Mathf.FloorToInt((p.y - _landMin) / LandCell);
            if (i < 0 || j < 0 || i >= _landN || j >= _landN) return Land.None;
            return (Land)_land[j * _landN + i];
        }

        /// <summary>What an area's kind counts as.</summary>
        public static Land LandOf(string kind)
        {
            switch (kind)
            {
                case "water": return Land.Water;
                case "parking": return Land.Lot;
                case "forest": case "wood": return Land.Forest;
                case "scrub": case "heath": return Land.Scrub;
                case "wetland": return Land.Wetland;
                case "park": case "garden": case "recreation_ground": case "village_green": return Land.Park;
                case "cemetery": case "churchyard": case "grave_yard": return Land.Cemetery;
                case "grass": return Land.Grass;
                case "meadow": return Land.Meadow;
                case "farmland": return Land.Farmland;
                case "farmyard": return Land.Farmyard;
                case "orchard": case "vineyard": return Land.Orchard;
                case "residential": return Land.Residential;
                case "commercial": case "retail": case "industrial": case "military": case "airfield": case "fuel":
                case "plaza": case "railway": case "construction":
                    return Land.Business;
                case "school": case "hospital": case "college": case "university": return Land.School;
                case "golf": return Land.Golf;
                case "pitch": case "playground": case "stadium": case "track": return Land.Pitch;
                case "quarry": case "sand": case "rock": case "lot": case "landfill": case "brownfield": return Land.Bare;
                default: return Land.None;
            }
        }

        private void RasterLand()
        {
            foreach (Patch p in Site.Area.Areas)
            {
                Land kind = LandOf(p.Data.kind);
                if (kind != Land.None && p.Pts.Count >= 3) Fill(p.Pts, kind);
            }
        }

        // Scanline fill of a polygon into the land grid (even-odd), cells by their middles.
        private void Fill(List<Vector2> poly, Land kind)
        {
            byte rank = LandRank[(int)kind];
            MapGeometry.Extent(poly, out float x0, out float z0, out float x1, out float z1);
            int j0 = Mathf.Max(0, Mathf.FloorToInt((z0 - _landMin) / LandCell)), j1 = Mathf.Min(_landN - 1, Mathf.FloorToInt((z1 - _landMin) / LandCell));
            for (int j = j0; j <= j1; j++)
            {
                float z = _landMin + (j + 0.5f) * LandCell;
                s_Crossings.Clear();
                for (int i = 0, k = poly.Count - 1; i < poly.Count; k = i++)
                {
                    Vector2 a = poly[k], b = poly[i];
                    if ((a.y > z) == (b.y > z)) continue;
                    s_Crossings.Add(a.x + (z - a.y) * (b.x - a.x) / (b.y - a.y));
                }
                s_Crossings.Sort();
                for (int t = 0; t + 1 < s_Crossings.Count; t += 2)
                {
                    int i0 = Mathf.Max(0, Mathf.CeilToInt((s_Crossings[t] - _landMin) / LandCell - 0.5f));
                    int i1 = Mathf.Min(_landN - 1, Mathf.FloorToInt((s_Crossings[t + 1] - _landMin) / LandCell - 0.5f));
                    for (int i = i0; i <= i1; i++)
                    {
                        int at = j * _landN + i;
                        if (rank <= _landRank[at]) continue;
                        _land[at] = (byte)kind;
                        _landRank[at] = rank;
                    }
                }
            }
        }
        #endregion

        #region How built up
        private const float DensityCell = 32f;
        private readonly float _dMin;
        private readonly int _dN;
        private readonly float[] _built, _business;

        // The share of the ground under buildings (and under shops, works and the like) in each cell and the eight round
        // it.
        private void Density()
        {
            var built = new float[_dN * _dN];
            var business = new float[_dN * _dN];
            foreach (Footprint f in _buildings)
            {
                int i = Mathf.Clamp(Mathf.FloorToInt((f.Middle.x - _dMin) / DensityCell), 0, _dN - 1);
                int j = Mathf.Clamp(Mathf.FloorToInt((f.Middle.y - _dMin) / DensityCell), 0, _dN - 1);
                built[j * _dN + i] += f.Size;
                if (IsBusiness(f.Data)) business[j * _dN + i] += f.Size;
            }
            float area = 9f * DensityCell * DensityCell;
            for (int j = 0; j < _dN; j++)
                for (int i = 0; i < _dN; i++)
                {
                    float b = 0f, c = 0f;
                    for (int dj = -1; dj <= 1; dj++)
                        for (int di = -1; di <= 1; di++)
                        {
                            int x = i + di, z = j + dj;
                            if (x < 0 || z < 0 || x >= _dN || z >= _dN) continue;
                            b += built[z * _dN + x];
                            c += business[z * _dN + x];
                        }
                    _built[j * _dN + i] = b / area;
                    _business[j * _dN + i] = c / area;
                }
        }

        private int DensityIndex(Vector2 p)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt((p.x - _dMin) / DensityCell), 0, _dN - 1);
            int j = Mathf.Clamp(Mathf.FloorToInt((p.y - _dMin) / DensityCell), 0, _dN - 1);
            return j * _dN + i;
        }

        /// <summary>The share of the ground within 50 m or so under buildings: 0 in open country, 0.1 or so in a
        /// suburb, 0.3 and more downtown.</summary>
        public float Urban(Vector2 p) => _built[DensityIndex(p)];

        /// <summary>As <see cref="Urban"/>, counting shops, works, churches and the like only.</summary>
        public float Business(Vector2 p) => _business[DensityIndex(p)];
        #endregion

        #region Water
        /// <summary>How far <paramref name="p"/> is from the nearest stream, river or ditch bank (as mapped), up to
        /// <paramref name="within"/>.</summary>
        public float WaterDistance(Vector2 p, float within)
        {
            float best = within;
            foreach (Line l in Site.Area.Lines)
            {
                if (l.IsRailway || l.Data.tunnel) continue;
                float half = Mathf.Clamp(l.Data.width, 1f, 60f) * 0.5f;
                for (int i = 0; i + 1 < l.Pts.Count; i++)
                {
                    Vector2 a = l.Pts[i], b = l.Pts[i + 1];
                    float reach = best + half;
                    if (p.x < Mathf.Min(a.x, b.x) - reach || p.x > Mathf.Max(a.x, b.x) + reach
                        || p.y < Mathf.Min(a.y, b.y) - reach || p.y > Mathf.Max(a.y, b.y) + reach) continue;
                    float d = MapGeometry.DistanceToSegment(p, a, b) - half;
                    if (d < best) best = d;
                }
            }
            if (LandAt(p) == Land.Water) best = Mathf.Min(best, 0f);
            return best;
        }
        #endregion

        #region Kept spots
        /// <summary>What a kept spot is kept for.</summary>
        internal enum KeepKind : byte { Car, Driveway, Pile }

        /// <summary>A rectangle one nature pass keeps for another (a parked car, the strip of a driveway, a plough pile).</summary>
        internal struct Keep
        {
            public Vector2 At, Axis;
            public float HalfLength, HalfWidth;
            public KeepKind Kind;
        }

        public readonly List<Keep> Kept = new List<Keep>();

        public void KeepRect(Vector2 at, Vector2 axis, float halfLength, float halfWidth, KeepKind kind)
            => Kept.Add(new Keep { At = at, Axis = axis.normalized, HalfLength = halfLength, HalfWidth = halfWidth, Kind = kind });

        // The driveways the roads laid (aprons and packed tracks up to the houses), from a couple of metres out in the
        // street, where a kerbside car would block the apron, to the track's end.
        private void KeepRoadDriveways()
        {
            IReadOnlyList<RoadPass.DrivewaySpot> spots = RoadPass.Driveways(Site);
            for (int i = 0; i < spots.Count; i++)
            {
                RoadPass.DrivewaySpot d = spots[i];
                if (d.Dir.sqrMagnitude < 0.5f) continue;
                const float from = -2.2f;
                float to = Mathf.Max(d.Length, 1f);
                KeepRect(d.At + d.Dir * ((from + to) * 0.5f), d.Dir, (to - from) * 0.5f, 1.7f, KeepKind.Driveway);
            }
        }

        /// <summary>True when a disc of <paramref name="radius"/> at <paramref name="p"/> touches a kept spot.</summary>
        public bool IsKept(Vector2 p, float radius)
        {
            for (int i = 0; i < Kept.Count; i++)
            {
                Keep k = Kept[i];
                Vector2 d = p - k.At;
                float along = Vector2.Dot(d, k.Axis), across = k.Axis.x * d.y - k.Axis.y * d.x;
                if (Mathf.Abs(along) < k.HalfLength + radius && Mathf.Abs(across) < k.HalfWidth + radius) return true;
            }
            return false;
        }

        /// <summary>True when the rectangle overlaps a kept spot (separating axes).</summary>
        public bool IsKept(Vector2 at, Vector2 axis, float halfLength, float halfWidth)
        {
            for (int i = 0; i < Kept.Count; i++)
            {
                Keep k = Kept[i];
                if (Overlap(at, axis, halfLength, halfWidth, k.At, k.Axis, k.HalfLength, k.HalfWidth)) return true;
            }
            return false;
        }

        /// <summary>True when two rectangles (middle, unit long axis, half sizes) overlap.</summary>
        public static bool Overlap(Vector2 a, Vector2 au, float al, float aw, Vector2 b, Vector2 bu, float bl, float bw)
        {
            Vector2 av = new Vector2(-au.y, au.x), bv = new Vector2(-bu.y, bu.x), d = b - a;
            return !Apart(d, au, au, av, al, aw, bu, bv, bl, bw) && !Apart(d, av, au, av, al, aw, bu, bv, bl, bw)
                && !Apart(d, bu, au, av, al, aw, bu, bv, bl, bw) && !Apart(d, bv, au, av, al, aw, bu, bv, bl, bw);
        }

        private static bool Apart(Vector2 d, Vector2 axis, Vector2 au, Vector2 av, float al, float aw, Vector2 bu, Vector2 bv, float bl, float bw)
        {
            float ra = al * Mathf.Abs(Vector2.Dot(au, axis)) + aw * Mathf.Abs(Vector2.Dot(av, axis));
            float rb = bl * Mathf.Abs(Vector2.Dot(bu, axis)) + bw * Mathf.Abs(Vector2.Dot(bv, axis));
            return Mathf.Abs(Vector2.Dot(d, axis)) > ra + rb;
        }

        /// <summary>A car waiting in a driveway, planned before the trees so they leave it room (VehiclePass draws it).</summary>
        internal struct CarSpot
        {
            public Vector2 At;
            /// <summary>Degrees clockwise from north the car's nose points.</summary>
            public float Yaw;
            public long Key;
            /// <summary>The home it belongs to (null for a road's driveway no home was found for).</summary>
            public Footprint Home;
            /// <summary>Where the driveway meets the street's sidewalk, and the width of the strip this pass draws to the
            /// car (0: none, the roads drew the driveway).</summary>
            public Vector2 Mouth;
            public float StripWidth;
            /// <summary>On a driveway the roads laid: <see cref="Mouth"/> is where it leaves the sidewalk.</summary>
            public bool Road;
            /// <summary>Along the driveway, from the street towards the house.</summary>
            public Vector2 Dir;
        }

        /// <summary>The driveways' cars (<see cref="VehiclePass.PlanDriveways"/>).</summary>
        public readonly List<CarSpot> Driveways = new List<CarSpot>();
        public bool DrivewaysPlanned;

        /// <summary>A plough pile planned in a parking lot (<see cref="SnowPass"/> draws it; the stalls keep clear).</summary>
        internal struct Pile
        {
            public Vector2 At;
            public float Yaw, Width, Depth, Height;
            public long Key;
        }

        public readonly List<Pile> Piles = new List<Pile>();
        public bool PilesPlanned;
        #endregion

        #region Homes
        /// <summary>
        /// A building seen from its street: its middle, the unit vectors along the street (<see cref="T"/>) and away from
        /// it (<see cref="N"/>, from the street towards the building), and the footprint's extent along them from the
        /// middle (<see cref="N0"/> &lt; 0 is its front).
        /// </summary>
        internal struct HouseFrame
        {
            public Vector2 Mid, T, N;
            public float T0, T1, N0, N1;
            /// <summary>A street was found: <see cref="Distance"/> from the middle to its middle line, <see cref="RoadHalf"/>
            /// its half-width, <see cref="Rank"/> its class.</summary>
            public bool Street;
            public float Distance, RoadHalf;
            public int Rank;

            public Vector2 At(float t, float n) => Mid + T * t + N * n;

            /// <summary>The kerb's line, as an n (negative: in front of the house).</summary>
            public float KerbN => -(Distance - RoadHalf);
        }

        private readonly Dictionary<Footprint, HouseFrame> _frames = new Dictionary<Footprint, HouseFrame>();
        private const int SaltFacing = 0x4F5E;

        /// <summary>The building as seen from its nearest street (a residential one or better within 50 m, else a service
        /// road within 30 m; without one it faces a way of its own).</summary>
        public HouseFrame FrameOf(Footprint f)
        {
            if (_frames.TryGetValue(f, out HouseFrame hf)) return hf;
            hf = new HouseFrame { Mid = f.Middle };
            bool found = NearestRoad(f.Middle, 50f, 2, out RoadSeg seg, out float dist, out Vector2 foot)
                || NearestRoad(f.Middle, 30f, 1, out seg, out dist, out foot);
            if (found && dist > 0.5f)
            {
                hf.N = (f.Middle - foot) / dist;
                hf.Street = true;
                hf.Distance = dist;
                hf.RoadHalf = seg.Half;
                hf.Rank = seg.Rank;
            }
            else
            {
                float a = MapStyle.Unit(f.Data.id, SaltFacing) * Mathf.PI * 2f;
                hf.N = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            hf.T = new Vector2(-hf.N.y, hf.N.x);
            hf.T0 = hf.N0 = float.MaxValue;
            hf.T1 = hf.N1 = float.MinValue;
            foreach (Vector2 q in f.Pts)
            {
                Vector2 d = q - f.Middle;
                float t = Vector2.Dot(d, hf.T), n = Vector2.Dot(d, hf.N);
                hf.T0 = Mathf.Min(hf.T0, t);
                hf.T1 = Mathf.Max(hf.T1, t);
                hf.N0 = Mathf.Min(hf.N0, n);
                hf.N1 = Mathf.Max(hf.N1, n);
            }
            _frames[f] = hf;
            return hf;
        }
        #endregion

        /// <summary>A coarse bucket grid over the site's reach, for the lookups.</summary>
        private sealed class Buckets
        {
            private readonly float _min, _cell;
            private readonly int _n;
            private readonly List<int>[] _cells;

            public Buckets(float reach, float cell)
            {
                _cell = cell;
                _min = -reach - cell;
                _n = Mathf.Max(1, Mathf.CeilToInt((reach * 2f + cell * 2f) / cell));
                _cells = new List<int>[_n * _n];
            }

            public void Add(int item, Rect r)
            {
                int x0 = Clamp(r.xMin), x1 = Clamp(r.xMax), z0 = Clamp(r.yMin), z1 = Clamp(r.yMax);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                        (_cells[z * _n + x] ??= new List<int>(4)).Add(item);
            }

            public void Range(Vector2 p, float radius, out int x0, out int z0, out int x1, out int z1)
            {
                x0 = Clamp(p.x - radius);
                x1 = Clamp(p.x + radius);
                z0 = Clamp(p.y - radius);
                z1 = Clamp(p.y + radius);
            }

            public List<int> At(int x, int z) => _cells[z * _n + x];

            private int Clamp(float v) => Mathf.Clamp(Mathf.FloorToInt((v - _min) / _cell), 0, _n - 1);
        }
    }
}
