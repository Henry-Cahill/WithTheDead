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
            // Attempt to open the current world build, handling any errors gracefully
            try
            {
                // Resolve the root directory of the world store, respecting the CLONEZ_WORLD env variable if set
                string root = Root;
                // Build the full file system path to the JSON that declares which build is currently active
                string currentPath = Path.Combine(root, "world", "current.json");
                // If the current.json file is missing, we cannot determine the active build – report why and abort
                if (!File.Exists(currentPath))
                {
                    why = "there's no " + currentPath + " (is the NAS reachable, and has a build been published?)";
                    return null;
                }
                // Parse the current.json file to get the build identifier; this may fail if the JSON is malformed
                MapCurrent current = JsonUtility.FromJson<MapCurrent>(File.ReadAllText(currentPath));
                // Ensure the JSON contained a valid build name before proceeding
                if (current == null || string.IsNullOrEmpty(current.build))
                {
                    why = currentPath + " doesn't name a build";
                    return null;
                }
                // Construct the path to the specific build folder indicated by the current.json entry
                string folder = Path.Combine(root, "world", "builds", current.build);
                // Load the manifest that describes the build's metadata (regions, format, attribution, etc.)
                MapManifest manifest = JsonUtility.FromJson<MapManifest>(File.ReadAllText(Path.Combine(folder, "manifest.json")));
                // Verify that the manifest's format version is within the range this game knows how to read
                if (manifest == null || manifest.format < MapBuild.OldestFormat || manifest.format > MapBuild.Format)
                {
                    why = "build " + current.build + " is format " + (manifest != null ? manifest.format : 0)
                        + "; this game reads formats " + MapBuild.OldestFormat + " to " + MapBuild.Format;
                    return null;
                }
                // No error reason – the build opened successfully
                why = null;
                // Instantiate a MapBuild object that the rest of the engine can query for regions, places, etc.
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
        /// <summary>The newest data format this game reads (worldbuild.py's FORMAT); it reads every format back to
        /// <see cref="OldestFormat"/>, as each only adds to the one before (see <see cref="MapRegion"/>).</summary>
        public const int Format = 2;
        /// <summary>The oldest data format this game still reads.</summary>
        public const int OldestFormat = 1;

        public readonly string Id;
        /// <summary>The build's folder in the store.</summary>
        public readonly string Folder;
        public readonly MapManifest Manifest;
        /// <summary>The store's validation folder (world/validation, beside the builds): what was found of the buildings
        /// and the corrections the game applies to them (see <see cref="MapValidation"/>).</summary>
        public readonly string ValidationFolder;
        /// <summary>Apply the corrections in <see cref="ValidationFolder"/> to the regions read. The game always does; a
        /// tool may want the data as mapped.</summary>
        public bool UseCorrections = true;
        private readonly HashSet<long> _regions = new HashSet<long>();
        private MapPlace[] _places;

        public MapBuild(string id, string folder, MapManifest manifest)
        {
            Id = id;
            Folder = folder;
            Manifest = manifest;
            ValidationFolder = MapValidation.FolderOf(folder);
            if (manifest.regions != null)
                foreach (MapRegionEntry r in manifest.regions) _regions.Add(Key(r.rx, r.rz));
        }

        /// <summary>The side of a region in metres (8,192).</summary>
        public int RegionSize => Manifest.region > 0 ? Manifest.region : 8192;

        /// <summary>The credit line the game shows for the map data.</summary>
        public string Attribution => string.IsNullOrEmpty(Manifest.attribution)
            ? "Map data © OpenStreetMap contributors (ODbL)" : Manifest.attribution;

        private static long Key(int rx, int rz) => ((long)rx << 32) | (uint)rz;

        /// <summary>True when the build has a region file at (<paramref name="rx"/>, <paramref name="rz"/>).</summary>
        public bool HasRegion(int rx, int rz) => _regions.Contains(Key(rx, rz));

        /// <summary>The grid rectangle (metres) the build's regions cover; false when it has none.</summary>
        public bool TryGetExtent(out double x0, out double z0, out double x1, out double z1)
        {
            x0 = z0 = x1 = z1 = 0;
            if (Manifest.regions == null || Manifest.regions.Length == 0) return false;
            int rx0 = int.MaxValue, rz0 = int.MaxValue, rx1 = int.MinValue, rz1 = int.MinValue;
            foreach (MapRegionEntry r in Manifest.regions)
            {
                rx0 = Math.Min(rx0, r.rx); rz0 = Math.Min(rz0, r.rz);
                rx1 = Math.Max(rx1, r.rx); rz1 = Math.Max(rz1, r.rz);
            }
            int size = RegionSize;
            x0 = (double)rx0 * size; z0 = (double)rz0 * size;
            x1 = (double)(rx1 + 1) * size; z1 = (double)(rz1 + 1) * size;
            return true;
        }

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

        /// <summary>Reads one region file (gzipped JSON), with its buildings' corrections applied (unless
        /// <see cref="UseCorrections"/> is off). Safe on any thread.</summary>
        public MapRegion LoadRegion(int rx, int rz)
        {
            string path = Path.Combine(Folder, "regions", rx + "_" + rz + ".json.gz");
            MapRegion region;
            using (FileStream file = File.OpenRead(path))
            using (var gz = new GZipStream(file, CompressionMode.Decompress))
            using (var reader = new StreamReader(gz))
                region = JsonUtility.FromJson<MapRegion>(reader.ReadToEnd());
            if (UseCorrections) MapValidation.Apply(region, MapValidation.Load(ValidationFolder, rx, rz));
            return region;
        }
    }
}
