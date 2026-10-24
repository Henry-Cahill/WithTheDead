using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // How thick the Zone is across the real map: the dead are where people lived. Every site built (the fixed square,
    // each streamed piece) leaves a coarse record of itself in 32 m cells, in world metres: how many households'
    // worth of people its buildings held (a house one, a flat block one per 90 m² of floor, a store one per 300 m²...),
    // and its land use. The density at a point weighs those cells around it (about 60 m), damped where no town is near
    // and lifted in the big places (places.json: Louisville's pull reaches 10 km, a village's half a kilometre).
    // Calibrated on the data: downtown Bardstown and its residential streets about 1.4, a rural subdivision 0.6, open
    // farmland 0.05-0.1, downtown Louisville past the cap of 3.
    internal static partial class MapGameplay
    {
        private const float DensityCell = 32f;
        private const float MaxDensity = 3f;
        // Households a hectare holds on an ordinary residential street: the density there is about 1.
        private const float UsualHouseholds = 6f;

        private struct CellStat
        {
            /// <summary>Households' worth of people whose buildings stand in the cell.</summary>
            public float Units;
            /// <summary>What the cell's land use adds (a parking lot, a cemetery...), when its piece has been built.</summary>
            public float Land;
            public bool Known, Water;
        }

        private static readonly Dictionary<long, CellStat> s_Cells = new Dictionary<long, CellStat>();
        // What each site added to the cells' households, so a piece built again replaces its share instead of adding it twice.
        private static readonly Dictionary<long, List<KeyValuePair<long, float>>> s_SiteUnits = new Dictionary<long, List<KeyValuePair<long, float>>>();
        private static object s_DensityEpoch;

        // The map's frame: the start place's grid point is the world's origin; the build's places, sorted into 8 km cells
        // (the big ones, whose pull reaches farther, also apart).
        private static string s_FrameBuild, s_FramePlace;
        private static double s_OriginX, s_OriginZ;
        private static bool s_HasFrame;
        private static MapPlace[] s_Places = new MapPlace[0];
        private static readonly Dictionary<long, List<MapPlace>> s_PlaceGrid = new Dictionary<long, List<MapPlace>>();
        private static readonly List<MapPlace> s_BigPlaces = new List<MapPlace>();
        private const double PlaceCell = 8192.0;
        private const int BigPlace = 15000;

        /// <summary>
        /// How thick the Zone is at <paramref name="world"/> (the dead, the danger), as a multiple of the map's usual (a
        /// small town's residential street is about 1): from the households the buildings around it held, its land use
        /// and the size of the place it's in; 0.03 in open country to 3 in a city's core. Known only where the map has
        /// been built (elsewhere guessed from the places alone); 1 before any of it has.
        /// </summary>
        internal static float DensityAt(Vector3 world)
        {
            if (!s_HasFrame) return 1f;
            int cx = Mathf.FloorToInt(world.x / DensityCell), cz = Mathf.FloorToInt(world.z / DensityCell);
            float tw = 0f, units = 0f, land = 0f, water = 0f;
            bool known = false;
            for (int dz = -2; dz <= 2; dz++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    float w = Kernel[(dz + 2) * 5 + dx + 2];
                    tw += w;
                    if (!s_Cells.TryGetValue(CellKey(cx + dx, cz + dz), out CellStat c) || !c.Known) continue;
                    known = true;
                    units += w * c.Units;
                    land += w * c.Land;
                    if (c.Water) water += w;
                }
            float settled = SettlementAtWorld(world);
            if (!known) return Mathf.Clamp(0.25f + 0.6f * settled, 0.03f, MaxDensity);
            float perHectare = units / tw / (DensityCell * DensityCell / 10000f);
            float local = Mathf.Pow(perHectare / UsualHouseholds, 0.7f);
            float d = 0.05f + local * (0.45f + 0.4f * settled) + 0.3f * settled + land / tw;
            d *= 1f - 0.85f * (water / tw);
            return Mathf.Clamp(d, 0.03f, MaxDensity);
        }

        // A 5×5 Gaussian over the cells (about 60 m either way).
        private static readonly float[] Kernel = MakeKernel();

        private static float[] MakeKernel()
        {
            var k = new float[25];
            for (int z = -2; z <= 2; z++)
                for (int x = -2; x <= 2; x++) k[(z + 2) * 5 + x + 2] = Mathf.Exp(-(x * x + z * z) / 4.5f);
            return k;
        }

        private static long CellKey(int x, int z) => ((long)x << 32) | (uint)z;

        /// <summary>Notes what <paramref name="site"/> holds for <see cref="DensityAt"/>; a piece built again replaces
        /// what it noted before.</summary>
        private static void RecordDensity(Site site)
        {
            UseFrame(site);
            object epoch = Epoch(site);
            if (!ReferenceEquals(epoch, s_DensityEpoch))
            {
                s_Cells.Clear();
                s_SiteUnits.Clear();
                s_DensityEpoch = epoch;
            }
            Vector3 o = site.WorldOffset;
            long siteKey = CellKey(Mathf.RoundToInt(o.x), Mathf.RoundToInt(o.z));
            if (s_SiteUnits.TryGetValue(siteKey, out List<KeyValuePair<long, float>> old))
                foreach (KeyValuePair<long, float> kv in old)
                    if (s_Cells.TryGetValue(kv.Key, out CellStat c))
                    {
                        c.Units = Mathf.Max(0f, c.Units - kv.Value);
                        s_Cells[kv.Key] = c;
                    }

            var mine = new Dictionary<long, float>();
            foreach (Footprint f in site.Area.Buildings)
            {
                float u = Households(f);
                if (u <= 0f) continue;
                long key = CellKey(Mathf.FloorToInt((o.x + f.Middle.x) / DensityCell), Mathf.FloorToInt((o.z + f.Middle.y) / DensityCell));
                mine[key] = mine.TryGetValue(key, out float had) ? had + u : u;
            }
            var list = new List<KeyValuePair<long, float>>(mine.Count);
            foreach (KeyValuePair<long, float> kv in mine)
            {
                s_Cells.TryGetValue(kv.Key, out CellStat c);
                c.Units += kv.Value;
                s_Cells[kv.Key] = c;
                list.Add(kv);
            }
            s_SiteUnits[siteKey] = list;

            // The land under the cells whose middles are this site's own (each cell's land is one site's to say).
            float reach = site.Chunked ? site.Half : site.Half + SceneryReach;
            int x0 = Mathf.CeilToInt((o.x - reach) / DensityCell - 0.5f), x1 = Mathf.FloorToInt((o.x + reach) / DensityCell - 0.5f);
            int z0 = Mathf.CeilToInt((o.z - reach) / DensityCell - 0.5f), z1 = Mathf.FloorToInt((o.z + reach) / DensityCell - 0.5f);
            List<Patch> patches = site.Area.Areas;
            var bounds = new Rect[patches.Count];
            for (int i = 0; i < patches.Count; i++) bounds[i] = MapGeometry.Bounds(patches[i].Pts);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    var local = new Vector2((x + 0.5f) * DensityCell - o.x, (z + 0.5f) * DensityCell - o.z);
                    if (Mathf.Abs(local.x) > reach || Mathf.Abs(local.y) > reach) continue;
                    long key = CellKey(x, z);
                    s_Cells.TryGetValue(key, out CellStat c);
                    c.Known = true;
                    c.Land = 0f;
                    c.Water = false;
                    for (int i = 0; i < patches.Count; i++)
                    {
                        if (!bounds[i].Contains(local) || !MapGeometry.Contains(patches[i].Pts, local)) continue;
                        string kind = patches[i].Data.kind;
                        if (kind == "water") c.Water = true;
                        else c.Land = Mathf.Max(c.Land, LandBonus(kind));
                    }
                    s_Cells[key] = c;
                }
        }

        // Households' worth of people a building held (whoever was in it when it came: residents, staff, customers).
        private static float Households(Footprint f)
        {
            MapBuilding b = f.Data;
            float size = f.Size, levels = Mathf.Max(1, b.levels);
            switch (b.kind)
            {
                case "house": return size >= 60f ? 1f : size >= 30f ? 0.3f : 0.05f;
                case "trailer": return 1f;
                case "apartments": return Mathf.Clamp(size * levels / 90f, 2f, 60f);
                case "hotel": return Mathf.Min(40f, size * levels / 150f);
                case "commercial": case "retail": case "office": case "civic": return Mathf.Min(40f, size * levels / 300f);
                case "school": return Mathf.Min(40f, size / 250f);
                case "hospital": return Mathf.Min(60f, size * levels / 150f);
                case "church": return Mathf.Min(10f, size / 400f);
                case "industrial": case "warehouse": return Mathf.Min(20f, size / 800f);
                default: return 0.05f;
            }
        }

        // Places people gathered draw the dead even with no buildings on them.
        private static float LandBonus(string kind)
        {
            switch (kind)
            {
                case "cemetery": return 0.2f;
                case "parking": case "plaza": case "retail": case "commercial": case "fuel": return 0.15f;
                case "residential": case "school": case "hospital": case "pitch": case "playground": case "park":
                case "churchyard": return 0.1f;
                case "industrial": case "lot": case "farmyard": case "military": return 0.05f;
                default: return 0f;
            }
        }

        #region The map's frame and its places
        // The start place (the world's origin) and the places of the site's build, read once per build.
        private static void UseFrame(Site site)
        {
            MapBuild build = site.Build;
            if (build == null) return;
            if (s_HasFrame && s_FrameBuild == build.Folder && s_FramePlace == site.Area.Place) return;
            MapPlace start = null;
            try
            {
                start = build.FindPlace(site.Area.Place);
                if (s_FrameBuild != build.Folder) LoadPlaces(build);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CloneZ] The map's places couldn't be read (" + e.Message + "); the Zone's density goes by the buildings alone.");
            }
            s_FrameBuild = build.Folder;
            s_FramePlace = site.Area.Place;
            s_HasFrame = true;
            s_OriginX = start != null ? start.x : 0;
            s_OriginZ = start != null ? start.z : 0;
        }

        private static void LoadPlaces(MapBuild build)
        {
            s_Places = new MapPlace[0];
            s_PlaceGrid.Clear();
            s_BigPlaces.Clear();
            string path = Path.Combine(build.Folder, "places.json");
            if (!File.Exists(path)) return;
            MapPlaces file = JsonUtility.FromJson<MapPlaces>(File.ReadAllText(path));
            if (file == null || file.places == null) return;
            s_Places = file.places;
            foreach (MapPlace p in s_Places)
            {
                if (Population(p) >= BigPlace) s_BigPlaces.Add(p);
                long key = PlaceKey((int)System.Math.Floor(p.x / PlaceCell), (int)System.Math.Floor(p.z / PlaceCell));
                if (!s_PlaceGrid.TryGetValue(key, out List<MapPlace> list)) s_PlaceGrid[key] = list = new List<MapPlace>();
                list.Add(p);
            }
        }

        private static long PlaceKey(int x, int z) => ((long)x << 32) | (uint)z;

        /// <summary>A place's people: as mapped, else what its kind usually holds.</summary>
        internal static int Population(MapPlace p)
        {
            if (p.population > 0) return p.population;
            switch (p.kind)
            {
                case "city": return 25000;
                case "town": return 4000;
                case "suburb": return 6000;
                case "neighbourhood": return 1500;
                case "village": return 600;
                case "hamlet": return 80;
                default: return 30;
            }
        }

        /// <summary>How far a place's built-up pull reaches, in metres (Louisville about 11 km, Bardstown 1.8 km, a
        /// village half a kilometre).</summary>
        private static float PlaceRadius(int population) => 600f * Mathf.Pow(population / 1000f, 0.45f);

        // How urban a point is from the places alone: the strongest pull on it, 0 in open country to about 1.4 in a city.
        private static float Pull(MapPlace p, double gx, double gz, out float distance)
        {
            int pop = Population(p);
            float r = PlaceRadius(pop);
            distance = (float)System.Math.Sqrt((p.x - gx) * (p.x - gx) + (p.z - gz) * (p.z - gz));
            if (distance > r * 3f) return 0f;
            float weight = Mathf.Clamp(0.55f + 0.3f * Mathf.Log10(pop / 1000f), 0.25f, 1.8f);
            float t = distance / r;
            return weight * Mathf.Exp(-t * t);
        }

        /// <summary>How urban a world point is from the places alone (0 open country, about 0.9 in a county seat's
        /// middle, 1.4 in Louisville's).</summary>
        internal static float SettlementAtWorld(Vector3 world) => StrongestPlace(world, out _, out _);

        private static float SettlementAt(Site site, Vector2 local)
        {
            UseFrame(site);
            return SettlementAtWorld(site.WorldOffset + To3(local, 0f));
        }

        /// <summary>The place pulling hardest on <paramref name="world"/> and its pull (0 when none does).</summary>
        internal static float StrongestPlace(Vector3 world, out MapPlace place, out float distance)
        {
            place = null;
            distance = float.MaxValue;
            if (!s_HasFrame) return 0f;
            double gx = s_OriginX + world.x, gz = s_OriginZ + world.z;
            float best = 0f;
            for (int i = 0; i < s_BigPlaces.Count; i++) Consider(s_BigPlaces[i], gx, gz, ref best, ref place, ref distance);
            int cx = (int)System.Math.Floor(gx / PlaceCell), cz = (int)System.Math.Floor(gz / PlaceCell);
            for (int z = cz - 1; z <= cz + 1; z++)
                for (int x = cx - 1; x <= cx + 1; x++)
                {
                    if (!s_PlaceGrid.TryGetValue(PlaceKey(x, z), out List<MapPlace> list)) continue;
                    for (int i = 0; i < list.Count; i++)
                        if (Population(list[i]) < BigPlace) Consider(list[i], gx, gz, ref best, ref place, ref distance);
                }
            return best;
        }

        private static void Consider(MapPlace p, double gx, double gz, ref float best, ref MapPlace place, ref float distance)
        {
            float pull = Pull(p, gx, gz, out float d);
            if (pull <= best) return;
            best = pull;
            place = p;
            distance = d;
        }

        /// <summary>The nearest named place to <paramref name="world"/> of at least <paramref name="minPopulation"/>
        /// people (within 60 km), with its distance and where it is in the world; false when there's none (or the map's
        /// places aren't known).</summary>
        internal static bool NearestPlace(Vector3 world, int minPopulation, out MapPlace place, out float distance, out Vector3 at)
        {
            place = null;
            distance = float.MaxValue;
            at = Vector3.zero;
            if (!s_HasFrame || s_Places.Length == 0) return false;
            double gx = s_OriginX + world.x, gz = s_OriginZ + world.z;
            foreach (MapPlace p in s_Places)
            {
                if (Population(p) < minPopulation) continue;
                double dx = p.x - gx, dz = p.z - gz;
                if (System.Math.Abs(dx) > 60000 || System.Math.Abs(dz) > 60000) continue;
                float d = (float)System.Math.Sqrt(dx * dx + dz * dz);
                if (d >= distance) continue;
                distance = d;
                place = p;
            }
            if (place == null) return false;
            at = new Vector3((float)(place.x - s_OriginX), 0f, (float)(place.z - s_OriginZ));
            return true;
        }

        /// <summary>The places of at least <paramref name="minPopulation"/> people within <paramref name="reach"/>
        /// metres of <paramref name="world"/>, as (place, its world point), for naming where roads go.</summary>
        internal static void PlacesAround(Vector3 world, float reach, int minPopulation, List<KeyValuePair<MapPlace, Vector3>> into)
        {
            into.Clear();
            if (!s_HasFrame) return;
            double gx = s_OriginX + world.x, gz = s_OriginZ + world.z;
            foreach (MapPlace p in s_Places)
            {
                if (Population(p) < minPopulation) continue;
                double dx = p.x - gx, dz = p.z - gz;
                if (dx * dx + dz * dz > (double)reach * reach) continue;
                into.Add(new KeyValuePair<MapPlace, Vector3>(p, new Vector3((float)(p.x - s_OriginX), 0f, (float)(p.z - s_OriginZ))));
            }
        }

        /// <summary>True when <paramref name="world"/> is inside <paramref name="place"/>'s built-up pull (its radius).</summary>
        internal static bool Within(MapPlace place, float distance) => place != null && distance < PlaceRadius(Population(place));

        /// <summary>
        /// How well stocked the Zone is at <paramref name="world"/>, as a multiple of the usual: the start town has been
        /// picked over (0.8), and the farther from it the better (1.0 at about 9 km, 1.15 at 20 km, 1.35 at 50 km), more
        /// so in the bigger places (Louisville's core about a fifth richer, a village a tenth poorer). 0.75 to 1.6.
        /// </summary>
        internal static float LootRichnessAt(Vector3 world)
        {
            float km = Mathf.Sqrt(world.x * world.x + world.z * world.z) / 1000f;
            float far = 0.8f + 0.65f * (1f - Mathf.Exp(-km / 25f));
            float pull = StrongestPlace(world, out MapPlace place, out float d);
            float size = 1f;
            if (place != null && pull > 0.05f)
            {
                float r = PlaceRadius(Population(place));
                float within = Mathf.Exp(-(d / r) * (d / r));
                size = 1f + 0.12f * Mathf.Clamp(Mathf.Log10(Population(place) / 10000f), -1f, 2f) * within;
            }
            return Mathf.Clamp(far * size, 0.75f, 1.6f);
        }
        #endregion
    }
}
