using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The world store, where the build machines put the real-world map (Tools/World): <c>\\192.168.1.170\CLONE-Z</c>
    /// on Windows, <c>/mnt/clonez</c> on the Linux machines, or wherever the <c>CLONEZ_WORLD</c> environment variable
    /// points (a local copy, say). Opens the build named in <c>world/current.json</c>. Nothing here throws at its
    /// callers: a store that can't be read gives a reason, and the raid falls back to the procedural town.
    /// </summary>
    public static class WorldStore
    {
        /// <summary>The environment variable that points the game at another copy of the store.</summary>
        public const string RootVariable = "CLONEZ_WORLD";
        public const string StreetLampMaterialPath = "Materials/Metal";
        public const string KerbMaterialPath = "Materials/KerbConcrete";
        public const string StreetLampGlassMaterialPath = "Materials/StreetLampGlass";
        /// <summary>Returns the translucent glass material for lamp heads.</summary>
        private static Material s_StreetLampGlassMaterial;
        public static Material GetStreetLampGlassMaterial()
        {
            if (s_StreetLampGlassMaterial != null) return s_StreetLampGlassMaterial;
            var path = System.IO.Path.Combine(Root, StreetLampGlassMaterialPath);
            s_StreetLampGlassMaterial = UnityEngine.Resources.Load<Material>(path);
            return s_StreetLampGlassMaterial;
        }


        /// <summary>Where the store is on this machine.</summary>
        public static string Root
        {
            get
            {
                string set = Environment.GetEnvironmentVariable(RootVariable);
                if (!string.IsNullOrEmpty(set)) return set;
                return Path.DirectorySeparatorChar == '\\' ? @"\\192.168.1.170\CLONE-Z" : "/mnt/clonez";
            }
        }

        /// <summary>The build the game should load, or null with the reason in <paramref name="why"/>.</summary>
        public static MapBuild OpenCurrent(out string why)
        {
            try
            {
                string root = Root;
                string currentPath = Path.Combine(root, "world", "current.json");
                if (!File.Exists(currentPath))
                {
                    why = "there's no " + currentPath + " (is the NAS reachable, and has a build been published?)";
                    return null;
                }
                MapCurrent current = JsonUtility.FromJson<MapCurrent>(File.ReadAllText(currentPath));
                if (current == null || string.IsNullOrEmpty(current.build))
                {
                    why = currentPath + " doesn't name a build";
                    return null;
                }
                string folder = Path.Combine(root, "world", "builds", current.build);
                MapManifest manifest = JsonUtility.FromJson<MapManifest>(File.ReadAllText(Path.Combine(folder, "manifest.json")));
                if (manifest == null || manifest.format != MapBuild.Format)
                {
                    why = "build " + current.build + " is format " + (manifest != null ? manifest.format : 0)
                        + "; this game reads format " + MapBuild.Format;
                    return null;
                }
                why = null;
                return new MapBuild(current.build, folder, manifest);
            }
            catch (Exception e)
            {
                why = e.GetType().Name + ": " + e.Message;
                return null;
            }
        }
    }

    /// <summary>One published build of the real-world map: its manifest, its named places and its regions.</summary>
    public sealed class MapBuild
        {
            /// <summary>
            /// Determine if a road segment should get painted lane markings.
            /// Deterministic based on the build ID and the segment ID.
            /// Approximately 50 % of road pieces receive markings.
            /// </summary>
            public bool HasRoadMarkings(long segmentId)
            {
                unchecked
                {
                    uint hash = (uint)segmentId ^ 0xA1B2C3D4u;
                    hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                    return (hash % 100) < 50; // 50% chance
                }
            }

            private const float RoadMarkingThickness = 0.03f; // 3 cm thick painted strip

            internal float GetRoadMarkingThickness() => RoadMarkingThickness;
            /// <summary>The street‑lamp material.</summary>
        internal Material GetStreetLampMaterial()
        {
            // Material reference is cached by Unity's Resources system; path must exist.
            return Resources.Load<Material>(WorldStore.StreetLampMaterialPath);
        }
            /// <summary>Determine if a building should get a thin snow layer on its roof.
            /// Deterministic based on the build ID and the building ID.
            /// Approximately 30% of roofs receive snow.</summary>
            public bool RoofHasSnow(long buildingId)
            {
                unchecked
                {
                    uint hash = (uint)buildingId ^ 0x9e3779b9u;
                    hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                    return (hash % 100) < 30; // 30% chance
                }
        }
        /// <summary>
        /// Determine if a road segment should get a sidewalk/curb on its side.
        /// Deterministic based on the build ID and the segment ID.
        /// Approximately 45 % of road pieces receive a sidewalk.
        /// </summary>
        public bool HasSidewalk(long segmentId)
        {
            unchecked
            {
                uint hash = (uint)segmentId ^ 0x517cc1b7u;
                hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                return (hash % 100) < 45; // 45% chance
            }
        }

        /// <summary>
        /// Determines if a building should receive a modest front porch (low railing and steps).
        /// Deterministic based on the build ID and the building ID.
        /// Roughly 40 % of buildings get a porch, giving a hand‑built aesthetic for a 1993 rural Kentucky home.
        /// </summary>
        public bool HasPorch(long buildingId)
        {
            unchecked
            {
                uint hash = (uint)buildingId ^ 0x3c6ef372u;
                hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                return (hash % 100) < 40; // 40% chance
            }
        }

        /// <summary>
        /// Determine if a building should get a low wooden fence around its perimeter.
        /// Deterministic based on the build ID and the building ID.
        /// Roughly 25 % of buildings receive a fence, adding a hand‑painted rural feel.
        /// </summary>
        public bool HasFence(long buildingId)
        {
            unchecked
            {
                uint hash = (uint)buildingId ^ 0xA5B357F9u;
                hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                return (hash % 100) < 25; // 25% chance
            }
        }



        /// <summary>
        /// Determine if an upper‑story window should receive a period‑correct metal grate.
        /// Deterministic based on the build ID and the building ID.
        /// Approximately 35 % of eligible windows get a grate, adding a hand‑built 1993 Kentucky look.
        /// </summary>
        public bool HasWindowGrate(long buildingId)
        {
            unchecked
            {
                uint hash = (uint)buildingId ^ 0x7f4a7c13u;
                hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                return (hash % 100) < 35; // ~35% chance
            }
        }
        /// <summary>
        /// Determine if a road segment should receive a street lamp.
        /// Deterministic based on the build Id and the segment Id.
        /// Approximately 30 % of eligible segments get a lamp, giving the town a hand‑painted rural feel.
        /// </summary>
        public bool HasStreetLamp(long segmentId)
        {
            unchecked
            {
                uint hash = (uint)segmentId ^ 0xD4E5F6A7u;
                hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                return (hash % 100) < 30; // 30% chance
            }
        }
        /// <summary>The data format this game reads (worldbuild.py's FORMAT).</summary>
        public const int Format = 1;

        public readonly string Id;
        /// <summary>The build's folder in the store.</summary>
        public readonly string Folder;
        public readonly MapManifest Manifest;
        private readonly HashSet<long> _regions = new HashSet<long>();
        private MapPlace[] _places;
        private System.Random _rand;

        public MapBuild(string id, string folder, MapManifest manifest)
        {
            Id = id;
            Folder = folder;
            Manifest = manifest;
            if (manifest.regions != null)
                foreach (MapRegionEntry r in manifest.regions) _regions.Add(Key(r.rx, r.rz));
        }

        /// <summary>The side of a region in metres (8,192).</summary>
        public int RegionSize => Manifest.region > 0 ? Manifest.region : 8192;

        /// <summary>Deterministic random seeded by the build Id, useful for consistent visual tweaks.</summary>
        public System.Random Random => _rand ??= new System.Random(Id.GetHashCode());

        /// <summary>Deterministic colour for a building (or other feature) identified by <paramref name="seed"/>.
        /// Uses the build‑id hash combined with the seed to produce a stable hue in a muted range.</summary>
        public Color GetBuildingColour(int seed)
        {
            // combine build id hash with the specific seed so each feature gets its own stream
            int combined = Id.GetHashCode() ^ seed;
            var rand = new System.Random(combined);
            // Generate a hue in [0,1) and muted saturation/value
            float hue = (float)rand.NextDouble();
            float sat = 0.4f + (float)rand.NextDouble() * 0.2f; // 0.4‑0.6
            float val = 0.6f + (float)rand.NextDouble() * 0.2f; // 0.6‑0.8
            return Color.HSVToRGB(hue, sat, val);
        }

        /// <summary>Deterministic colour for a building identified by its long ID.
        /// Delegates to the int‑seed overload.</summary>
        public Color GetBuildingColour(long buildingId) => GetBuildingColour((int)buildingId);

        /// <summary>The credit line the game shows for the map data.</summary>
        public string Attribution => string.IsNullOrEmpty(Manifest.attribution)
            ? "Map data © OpenStreetMap contributors (ODbL)" : Manifest.attribution;

        private static long Key(int rx, int rz) => ((long)rx << 32) | (uint)rz;

        /// <summary>The most populous place called <paramref name="name"/> (any case), or null.</summary>
        public MapPlace FindPlace(string name)
        {
            if (_places == null)
            {
                MapPlaces file = JsonUtility.FromJson<MapPlaces>(File.ReadAllText(Path.Combine(Folder, "places.json")));
                _places = file != null && file.places != null ? file.places : new MapPlace[0];
            }
            foreach (MapPlace p in _places)   // largest first, so a name shared by several places finds the town
                if (string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        /// <summary>Every region overlapping the square of half-side <paramref name="half"/> metres around a point
        /// on the grid, read from the store.</summary>
        public List<MapRegion> RegionsAround(double x, double z, double half)
        {
            int size = RegionSize;
            int rx0 = (int)Math.Floor((x - half) / size), rx1 = (int)Math.Floor((x + half) / size);
            int rz0 = (int)Math.Floor((z - half) / size), rz1 = (int)Math.Floor((z + half) / size);
            var found = new List<MapRegion>();
            for (int rx = rx0; rx <= rx1; rx++)
                for (int rz = rz0; rz <= rz1; rz++)
                    if (_regions.Contains(Key(rx, rz))) found.Add(LoadRegion(rx, rz));
            return found;
        }

        /// <summary>Reads one region file (gzipped JSON).</summary>
        public MapRegion LoadRegion(int rx, int rz)
        {
            string path = Path.Combine(Folder, "regions", rx + "_" + rz + ".json.gz");
            using (FileStream file = File.OpenRead(path))
            using (var gz = new GZipStream(file, CompressionMode.Decompress))
            using (var reader = new StreamReader(gz))
                return JsonUtility.FromJson<MapRegion>(reader.ReadToEnd());
        }
    }
}
