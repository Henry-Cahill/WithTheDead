using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What stands around a building, for the guesses the data leaves to us: how far it is from the middle of a town and
    // how big that town is (places.json), how closely built its neighbourhood is, which of its walls it shares with a
    // neighbour, what the point of interest mapped inside it is, the land use it stands in and the road it faces.
    internal static partial class BuildingPass
    {
        private sealed class Context
        {
            public readonly Site Site;
            /// <summary>Towns near the site: site-local x, z and population.</summary>
            private readonly List<Vector3> _places = new List<Vector3>();
            public readonly bool HasPlaces;
            private const float Cell = 24f;
            private readonly Dictionary<long, List<Footprint>> _grid = new Dictionary<long, List<Footprint>>();
            private readonly List<(Patch patch, Rect bounds)> _areas = new List<(Patch, Rect)>();
            private readonly List<Street> _roads = new List<Street>();
            private readonly List<Spot> _fuel = new List<Spot>();
            // The roads' segments by grid cell (index into _segA/_segB/_segRoad), and the points of interest by cell.
            private const float RoadCell = 32f;
            private readonly Dictionary<long, List<int>> _segGrid = new Dictionary<long, List<int>>();
            private readonly List<Vector2> _segA = new List<Vector2>(), _segB = new List<Vector2>();
            private readonly List<int> _segRoad = new List<int>();
            private readonly Dictionary<long, List<Spot>> _spotGrid = new Dictionary<long, List<Spot>>();
            /// <summary>A streamed piece knows only its own square: how far its data reaches from the middle.</summary>
            private readonly float _reach;

            public Context(Site site)
            {
                Site = site;
                _reach = site.Chunked ? site.Half : site.Half + SceneryReach;
                foreach (Footprint f in site.Area.Buildings) Add(f);
                foreach (Footprint f in site.Area.Foreign) Add(f);
                foreach (Patch a in site.Area.Areas) _areas.Add((a, MapGeometry.Bounds(a.Pts)));
                foreach (Street s in site.Area.Streets)
                    if (s.Rank >= 1 && !s.Data.tunnel && s.Pts.Count >= 2) AddRoad(s);
                foreach (Spot p in site.Area.Points)
                {
                    if (p.Data.kind == "fuel") _fuel.Add(p);
                    long key = Key(p.At);
                    if (!_spotGrid.TryGetValue(key, out List<Spot> list)) _spotGrid[key] = list = new List<Spot>(2);
                    list.Add(p);
                }
                HasPlaces = FindPlaces(site, _places);
            }

            private void AddRoad(Street s)
            {
                int index = _roads.Count;
                _roads.Add(s);
                List<Vector2> pts = s.Pts;
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    Vector2 a = pts[i], b = pts[i + 1];
                    int seg = _segA.Count;
                    _segA.Add(a);
                    _segB.Add(b);
                    _segRoad.Add(index);
                    int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) / RoadCell), x1 = Mathf.FloorToInt(Mathf.Max(a.x, b.x) / RoadCell);
                    int z0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) / RoadCell), z1 = Mathf.FloorToInt(Mathf.Max(a.y, b.y) / RoadCell);
                    for (int z = z0; z <= z1; z++)
                        for (int x = x0; x <= x1; x++)
                        {
                            long key = ((long)x << 32) | (uint)z;
                            if (!_segGrid.TryGetValue(key, out List<int> list)) _segGrid[key] = list = new List<int>(4);
                            list.Add(seg);
                        }
                }
            }

            private void Add(Footprint f)
            {
                long key = Key(f.Middle);
                if (!_grid.TryGetValue(key, out List<Footprint> list)) _grid[key] = list = new List<Footprint>(4);
                list.Add(f);
            }

            private static long Key(Vector2 p) => ((long)Mathf.FloorToInt(p.x / Cell) << 32) | (uint)Mathf.FloorToInt(p.y / Cell);

            /// <summary>How much of a town's middle (core) and of a town at all (town) a spot is, 0 to 1, and the population
            /// of the town it's most in.</summary>
            public void Urban(Footprint f, float cover, int count, out float core, out float town, out float population)
            {
                // From the neighbourhood alone: closely built is the middle of something, a few neighbours a town.
                float localCore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.42f, cover));
                float localTown = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2f, 11f, count));
                core = localCore * 0.85f;
                town = localTown * 0.8f;
                population = 0f;
                if (!HasPlaces) return;
                float placeCore = 0f, placeTown = 0f;
                for (int i = 0; i < _places.Count; i++)
                {
                    Vector3 pl = _places[i];
                    float d = Vector2.Distance(f.Middle, new Vector2(pl.x, pl.y));
                    float root = Mathf.Sqrt(pl.z);
                    float rc = Mathf.Clamp(60f + 2.2f * root, 80f, 1000f), rt = Mathf.Clamp(250f + 12f * root, 350f, 9000f);
                    float c = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f * rc, rc, d));
                    float t = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f * rt, rt, d));
                    if (c > placeCore) placeCore = c;
                    if (t > placeTown) placeTown = t;
                    if (t > 0.3f && pl.z > population) population = pl.z;
                }
                // A town's middle is built up: open ground near the courthouse isn't downtown.
                core = Mathf.Max(core, placeCore * Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(0.06f, 0.26f, cover)));
                town = Mathf.Max(town, placeTown);
            }

            /// <summary>The footprint area within <paramref name="radius"/> of <paramref name="f"/>'s middle as a share
            /// of the disc, and how many buildings stand within 60 m; both judged from the part of the neighbourhood the
            /// site can see (a streamed piece knows nothing past its own square: it takes what's beyond to be like
            /// what's inside, so the buildings along its edge don't all look like the edge of town).</summary>
            public void Neighbourhood(Footprint f, float radius, out float cover, out int count)
            {
                float area = 0f;
                count = 0;
                float r2 = radius * radius, far2 = 60f * 60f;
                int reach = Mathf.CeilToInt(60f / Cell);
                int cx = Mathf.FloorToInt(f.Middle.x / Cell), cz = Mathf.FloorToInt(f.Middle.y / Cell);
                for (int z = cz - reach; z <= cz + reach; z++)
                    for (int x = cx - reach; x <= cx + reach; x++)
                    {
                        if (!_grid.TryGetValue(((long)x << 32) | (uint)z, out List<Footprint> list)) continue;
                        for (int i = 0; i < list.Count; i++)
                        {
                            Footprint o = list[i];
                            if (o == f) continue;
                            float d2 = (o.Middle - f.Middle).sqrMagnitude;
                            if (d2 < far2) count++;
                            if (d2 < r2) area += o.Size;
                        }
                    }
                float seen = Seen(f.Middle, radius), seenFar = Seen(f.Middle, 60f);
                cover = (area / seen + f.Size) / (Mathf.PI * r2);
                count = Mathf.RoundToInt(count / seenFar);
            }

            // Points spread evenly over the unit disc (a sunflower).
            private static readonly Vector2[] s_Disc = MakeDisc(48);

            private static Vector2[] MakeDisc(int n)
            {
                var pts = new Vector2[n];
                float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
                for (int i = 0; i < n; i++)
                {
                    float r = Mathf.Sqrt((i + 0.5f) / n), a = i * golden;
                    pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                }
                return pts;
            }

            // The share of the disc of radius r round p that's inside the data the site holds (at least a third).
            private float Seen(Vector2 p, float r)
            {
                if (Mathf.Abs(p.x) + r <= _reach && Mathf.Abs(p.y) + r <= _reach) return 1f;
                int inside = 0;
                for (int i = 0; i < s_Disc.Length; i++)
                {
                    Vector2 q = p + s_Disc[i] * r;
                    if (Mathf.Abs(q.x) <= _reach && Mathf.Abs(q.y) <= _reach) inside++;
                }
                return Mathf.Max(0.33f, inside / (float)s_Disc.Length);
            }

            /// <summary>The neighbours whose outlines come within <paramref name="margin"/> of <paramref name="f"/>'s
            /// bounds.</summary>
            public void Near(Footprint f, float margin, List<Footprint> into)
            {
                into.Clear();
                Rect r = f.Bounds;
                r.xMin -= margin; r.yMin -= margin; r.xMax += margin; r.yMax += margin;
                int x0 = Mathf.FloorToInt((r.xMin - 40f) / Cell), x1 = Mathf.FloorToInt((r.xMax + 40f) / Cell);
                int z0 = Mathf.FloorToInt((r.yMin - 40f) / Cell), z1 = Mathf.FloorToInt((r.yMax + 40f) / Cell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!_grid.TryGetValue(((long)x << 32) | (uint)z, out List<Footprint> list)) continue;
                        for (int i = 0; i < list.Count; i++)
                            if (list[i] != f && list[i].Bounds.Overlaps(r)) into.Add(list[i]);
                    }
            }

            /// <summary>Which of <paramref name="f"/>'s walls a neighbour builds against (a party wall: no door, no
            /// windows), and how many.</summary>
            public bool[] PartyWalls(Footprint f, List<Footprint> near, out int count)
            {
                List<Vector2> p = f.Pts;
                var party = new bool[p.Count];
                count = 0;
                if (near.Count == 0) return party;
                for (int i = 0; i < p.Count; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % p.Count];
                    float len = Vector2.Distance(a, b);
                    if (len < 1.5f) continue;
                    Vector2 d = (b - a) / len;
                    float shared = 0f;
                    foreach (Footprint o in near)
                    {
                        List<Vector2> q = o.Pts;
                        for (int k = 0; k < q.Count; k++)
                        {
                            Vector2 c = q[k], e = q[(k + 1) % q.Count];
                            float olen = Vector2.Distance(c, e);
                            if (olen < 1f) continue;
                            Vector2 od = (e - c) / olen;
                            if (Mathf.Abs(d.x * od.y - d.y * od.x) > 0.13f) continue;   // not parallel
                            Vector2 n = new Vector2(-d.y, d.x);
                            if (Mathf.Abs(Vector2.Dot(c - a, n)) > 0.8f || Mathf.Abs(Vector2.Dot(e - a, n)) > 0.8f) continue;
                            float t0 = Vector2.Dot(c - a, d), t1 = Vector2.Dot(e - a, d);
                            if (t0 > t1) { float t = t0; t0 = t1; t1 = t; }
                            shared += Mathf.Max(0f, Mathf.Min(len, t1) - Mathf.Max(0f, t0));
                        }
                    }
                    if (shared >= len * 0.45f)
                    {
                        party[i] = true;
                        count++;
                    }
                }
                return party;
            }

            /// <summary>The point of interest mapped inside the footprint (an amenity before a shop), or null.</summary>
            public Spot Inside(Footprint f)
            {
                Spot found = null;
                Rect r = f.Bounds;
                int x0 = Mathf.FloorToInt(r.xMin / Cell), x1 = Mathf.FloorToInt(r.xMax / Cell);
                int z0 = Mathf.FloorToInt(r.yMin / Cell), z1 = Mathf.FloorToInt(r.yMax / Cell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!_spotGrid.TryGetValue(((long)x << 32) | (uint)z, out List<Spot> list)) continue;
                        foreach (Spot s in list)
                        {
                            if (!r.Contains(s.At) || !MapGeometry.Contains(f.Pts, s.At)) continue;
                            if (found == null || (found.Data.kind.StartsWith("shop:") && !s.Data.kind.StartsWith("shop:"))) found = s;
                        }
                    }
                return found;
            }

            /// <summary>True when a filling station is mapped within <paramref name="within"/> of the point.</summary>
            public bool FuelNear(Vector2 p, float within)
            {
                for (int i = 0; i < _fuel.Count; i++)
                    if ((_fuel[i].At - p).sqrMagnitude < within * within) return true;
                return false;
            }

            /// <summary>The land use the point stands in (the most telling, when areas overlap), or "".</summary>
            public string LandUse(Vector2 p)
            {
                string best = "";
                int bestRank = 0;
                foreach ((Patch patch, Rect bounds) in _areas)
                {
                    if (!bounds.Contains(p)) continue;
                    int rank = LandRank(patch.Data.kind);
                    if (rank <= bestRank || !MapGeometry.Contains(patch.Pts, p)) continue;
                    best = patch.Data.kind;
                    bestRank = rank;
                }
                return best;
            }

            private static int LandRank(string kind)
            {
                switch (kind)
                {
                    case "school": case "hospital": case "churchyard": case "military": case "fuel": return 6;
                    case "industrial": case "retail": case "commercial": return 5;
                    case "farmyard": return 4;
                    case "residential": return 3;
                    case "farmland": case "meadow": case "orchard": return 2;
                    case "forest": case "grass": case "park": case "cemetery": return 1;
                    default: return 0;
                }
            }

            /// <summary>The nearest road (service roads and up, no tunnels) within <paramref name="within"/>: its
            /// distance and the way to it; false when there's none.</summary>
            public bool NearestRoad(Vector2 p, float within, out float dist, out Street road, out Vector2 toward)
            {
                dist = within;
                road = null;
                toward = Vector2.zero;
                float best = float.MaxValue;
                // The widest road is 30 m: its line can lie that much farther off than its edge.
                float look = within + 15f;
                int x0 = Mathf.FloorToInt((p.x - look) / RoadCell), x1 = Mathf.FloorToInt((p.x + look) / RoadCell);
                int z0 = Mathf.FloorToInt((p.y - look) / RoadCell), z1 = Mathf.FloorToInt((p.y + look) / RoadCell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!_segGrid.TryGetValue(((long)x << 32) | (uint)z, out List<int> segs)) continue;
                        for (int k = 0; k < segs.Count; k++)
                        {
                            int i = segs[k];
                            Street s = _roads[_segRoad[i]];
                            Vector2 a = _segA[i], ab = _segB[i] - a;
                            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                            Vector2 to = a + ab * t - p;
                            float d = Mathf.Max(0f, to.magnitude - s.Width * 0.5f);
                            if (d > within || to.sqrMagnitude < 1e-6f) continue;
                            // A main road a little farther off still counts as the one a building faces.
                            float score = d - s.Rank * 1.5f;
                            // Ties (a segment seen from two cells) keep the first: the same answer whatever the order.
                            if (score >= best) continue;
                            best = score;
                            dist = d;
                            road = s;
                            toward = to.normalized;
                        }
                    }
                return road != null;
            }

            /// <summary>True when a building at least <paramref name="factor"/> times bigger stands within
            /// <paramref name="within"/> (an outbuilding beside its house).</summary>
            public bool BiggerNeighbour(Footprint f, float within, float factor)
            {
                int reach = Mathf.CeilToInt(within / Cell);
                int cx = Mathf.FloorToInt(f.Middle.x / Cell), cz = Mathf.FloorToInt(f.Middle.y / Cell);
                for (int z = cz - reach; z <= cz + reach; z++)
                    for (int x = cx - reach; x <= cx + reach; x++)
                    {
                        if (!_grid.TryGetValue(((long)x << 32) | (uint)z, out List<Footprint> list)) continue;
                        foreach (Footprint o in list)
                            if (o != f && o.Size >= f.Size * factor && (o.Middle - f.Middle).sqrMagnitude < within * within) return true;
                    }
                return false;
            }
        }

        #region Places
        private static string s_PlacesFolder;
        private static double[] s_PlaceX, s_PlaceZ;
        private static float[] s_PlacePop;

        // The towns within reach of the site, in site-local metres (x, z, population). The site's middle on the grid is
        // its start place plus its offset in the world; that's checked against the data (a building's outline is in
        // decimetres from its region's corner), so a site built anywhere else falls back on its neighbourhood alone.
        private static bool FindPlaces(Site site, List<Vector3> into)
        {
            try
            {
                return TryFindPlaces(site, into);
            }
            catch (System.Exception)
            {
                into.Clear();
                return false;
            }
        }

        private static bool TryFindPlaces(Site site, List<Vector3> into)
        {
            if (site.Build == null || site.Area.Buildings.Count == 0) return false;
            LoadPlaces(site.Build);
            if (s_PlaceX == null || s_PlaceX.Length == 0) return false;
            MapPlace start = site.Build.FindPlace(site.Area.Place);
            if (start == null) return false;
            double gx = start.x + site.WorldOffset.x, gz = start.z + site.WorldOffset.z;
            Footprint probe = site.Area.Buildings[0];
            int[] raw = probe.Data.pts;
            if (raw == null || raw.Length < 2) return false;
            int rawX = int.MaxValue, rawZ = int.MaxValue;
            for (int i = 0; i + 1 < raw.Length; i += 2)
            {
                rawX = Mathf.Min(rawX, raw[i]);
                rawZ = Mathf.Min(rawZ, raw[i + 1]);
            }
            double size = site.Build.RegionSize;
            double cornerX = gx + probe.Bounds.xMin - rawX * 0.1, cornerZ = gz + probe.Bounds.yMin - rawZ * 0.1;
            double offX = cornerX / size - System.Math.Round(cornerX / size), offZ = cornerZ / size - System.Math.Round(cornerZ / size);
            if (System.Math.Abs(offX) * size > 2.0 || System.Math.Abs(offZ) * size > 2.0) return false;
            const double Reach = 12000.0;
            for (int i = 0; i < s_PlaceX.Length; i++)
            {
                double dx = s_PlaceX[i] - gx, dz = s_PlaceZ[i] - gz;
                if (dx * dx + dz * dz > Reach * Reach) continue;
                into.Add(new Vector3((float)dx, (float)dz, s_PlacePop[i]));
            }
            return true;
        }

        // places.json, read once a build: the cities, towns, villages and hamlets (a hamlet's population, never mapped,
        // taken as 120).
        private static void LoadPlaces(MapBuild build)
        {
            if (s_PlacesFolder == build.Folder) return;
            s_PlacesFolder = build.Folder;
            s_PlaceX = s_PlaceZ = null;
            s_PlacePop = null;
            try
            {
                MapPlaces file = JsonUtility.FromJson<MapPlaces>(File.ReadAllText(Path.Combine(build.Folder, "places.json")));
                if (file == null || file.places == null) return;
                var xs = new List<double>(file.places.Length);
                var zs = new List<double>(file.places.Length);
                var pops = new List<float>(file.places.Length);
                foreach (MapPlace p in file.places)
                {
                    float pop;
                    switch (p.kind)
                    {
                        case "city": case "town": case "village": pop = Mathf.Max(p.population, p.kind == "village" ? 300 : 2000); break;
                        case "hamlet": pop = p.population > 0 ? p.population : 120; break;
                        default: continue;
                    }
                    xs.Add(p.x);
                    zs.Add(p.z);
                    pops.Add(pop);
                }
                s_PlaceX = xs.ToArray();
                s_PlaceZ = zs.ToArray();
                s_PlacePop = pops.ToArray();
            }
            catch (System.Exception)
            {
                s_PlaceX = s_PlaceZ = null;
                s_PlacePop = null;
            }
        }
        #endregion
    }
}
