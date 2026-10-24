using System;

namespace CloneZ
{
    // The real-world map's data, as the world builder writes it (Tools/World/worldbuild.py, format 1). The files are
    // JSON read with JsonUtility, so every field is public and named exactly as in the files. Coordinates are
    // decimetres from the region's south-west corner on Kentucky's flat grid (EPSG:3088, metres east and north), in
    // flat lists: [x0, z0, x1, z1, ...]. Plain data only: how anything looks is up to the map's passes.
    // Map data © OpenStreetMap contributors (ODbL).

    /// <summary>world/current.json: the build the game and the servers load.</summary>
    [Serializable]
    public sealed class MapCurrent
    {
        public string build;
        public string published;
        public string attribution;
    }

    /// <summary>A build's manifest.json: its grid and the regions it has.</summary>
    [Serializable]
    public sealed class MapManifest
    {
        public int format;
        public string id;
        public string attribution;
        public int cell;
        public int region;
        public MapRegionEntry[] regions;
    }

    /// <summary>One region listed in the manifest, with how many of each feature it holds.</summary>
    [Serializable]
    public sealed class MapRegionEntry
    {
        public int rx, rz, streets, lines, buildings, areas, pois;
    }

    /// <summary>A build's places.json: the named places (cities, towns, villages...), largest first.</summary>
    [Serializable]
    public sealed class MapPlaces
    {
        public int format;
        public MapPlace[] places;
    }

    /// <summary>A named place, at its position on the grid in metres.</summary>
    [Serializable]
    public sealed class MapPlace
    {
        public long id;
        public string kind, name;
        public int population;
        public double x, z;
    }

    /// <summary>One region file (regions/&lt;rx&gt;_&lt;rz&gt;.json.gz): 8,192 m square, south-west corner at
    /// (rx, rz) × size on the grid.</summary>
    [Serializable]
    public sealed class MapRegion
    {
        public int format, rx, rz, size;
        public MapStreet[] streets;
        public MapLine[] lines;
        public MapBuilding[] buildings;
        public MapArea[] areas;
        public MapPoi[] pois;
    }

    /// <summary>A piece of street (OpenStreetMap's highway=<see cref="kind"/>), clipped to its region.</summary>
    [Serializable]
    public sealed class MapStreet
    {
        public long id;
        public string kind, name;
        /// <summary>Carriageway width in metres.</summary>
        public float width;
        public int lanes, layer;
        public bool oneway, bridge, tunnel;
        public int[] pts;
    }

    /// <summary>A river, stream, canal, ditch or railway, clipped to its region.</summary>
    [Serializable]
    public sealed class MapLine
    {
        public long id;
        public string kind, name;
        public float width;
        public int[] pts;
    }

    /// <summary>A building's footprint (its outer wall, no repeated last point) and what it is.</summary>
    [Serializable]
    public sealed class MapBuilding
    {
        public long id;
        /// <summary>house, trailer, apartments, hotel, commercial, office, retail, industrial, warehouse, church,
        /// school, hospital, civic, garage, shed, barn, silo, greenhouse, canopy or ruin.</summary>
        public string kind;
        public string name, address;
        /// <summary>What it's used for, when mapped: an amenity (fuel, police, pharmacy...) or "shop:&lt;kind&gt;".</summary>
        public string use;
        public int levels;
        /// <summary>Height in metres when mapped, else 0.</summary>
        public float height;
        public int[] pts;
    }

    /// <summary>A land-use area (water, parking, forest, farmland, residential, park...), clipped to its region.</summary>
    [Serializable]
    public sealed class MapArea
    {
        public long id;
        public string kind, name;
        public int[] pts;
    }

    /// <summary>A point of interest: an amenity (fuel, police, hospital, school...) or "shop:&lt;kind&gt;".</summary>
    [Serializable]
    public sealed class MapPoi
    {
        public long id;
        public string kind, name;
        public int x, z;
    }
}
