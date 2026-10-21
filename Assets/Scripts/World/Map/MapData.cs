using System;
using UnityEngine;

namespace CloneZ
{
    // The real-world map's data, as the world builder writes it (Tools/World/worldbuild.py, format 1). The files are
    // JSON read with JsonUtility, so every field is public and named exactly as in the files. Coordinates are
    // decimetres from the region's south-west corner on Kentucky's flat grid (EPSG:3088, metres east and north), in
    // flat lists: [x0, z0, x1, z1, ...]. Map data © OpenStreetMap contributors (ODbL).

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
        /// <summary>
        /// Approximate sidewalk width (metres) for this street.
        /// Uses the highway kind to choose a period‑appropriate value.
        /// </summary>
        public float SidewalkWidth()
        {
            // Common OSM highway classifications.
            // Wider sidewalks for major roads, narrower for residential streets.
            switch (kind)
            {
                case "motorway":
                case "trunk":
                case "primary":
                case "secondary":
                    return 2.5f; // broad sidewalks for main thoroughfares
                case "tertiary":
                case "residential":
                case "living_street":
                    return 1.5f; // typical residential sidewalk
                case "service":
                case "unclassified":
                    return 1.0f; // minimal sidewalk
                default:
                    // Fallback based on carriageway width if kind is unknown
                    return Mathf.Max(1.0f, width * 0.3f);
    }

    }
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
        /// <summary>What it's used for, when mapped: an amenity (fuel, police, pharmacy...) or "shop:<kind>".</summary>
        public string use;
        public int levels;
        /// <summary>Height in metres when mapped, else 0.</summary>
        public float height;
        public int[] pts;
        public Color GetTint()
        {
            // deterministic hash from id → hue offset in [0,0.1]
            float hue = ((id * 0x9E3779B9) & 0xFFFF) / 65535f * 0.1f;
            float sat = 0.6f + ((id >> 16) & 0xFF) / 255f * 0.2f;
            float val = 0.7f + ((id >> 24) & 0xFF) / 255f * 0.2f;
            return Color.HSVToRGB(hue, sat, val);
        }

        /// <summary>Effective building height: returns the mapped height if set (>0), otherwise a deterministic default based on levels.</summary>
        public float EffectiveHeight()
        {
            if (height > 0f)
                return Mathf.Clamp(height, 2.6f, 90f);
            // Default: each level ~3 meters plus a small roof offset.
            float storeys = Mathf.Max(1, levels) * 3.0f;
            return storeys + 0.4f;
        }

        /// <summary>
        /// Returns a subtle roof overhang (metres) based on building kind.
        /// Residential buildings get deeper eaves; commercial/industrial get shallower ones.
        /// The value is deterministic per-id to keep variation stable across runs.
        /// </summary>
        public float RoofOverhang()
        {
            float baseOverhang = kind switch
            {
                // Residential types – deeper eaves
                "house" => 0.8f,
                "trailer" => 0.8f,
                "apartment" => 0.8f,
                "apartments" => 0.8f,
                // Commercial / industrial – shallower eaves
                "commercial" => 0.4f,
                "office" => 0.4f,
                "retail" => 0.4f,
                "industrial" => 0.4f,
                "warehouse" => 0.4f,
                "garage" => 0.4f,
                "shed" => 0.4f,
                "barn" => 0.4f,
                "silo" => 0.4f,
                "greenhouse" => 0.4f,
                "canopy" => 0.4f,
                "hotel" => 0.4f,
                "civic" => 0.4f,
                "school" => 0.4f,
                "hospital" => 0.4f,
                "church" => 0.4f,
                _ => 0.5f // default moderate overhang
            };
            // deterministic small variation per id (0‑0.2 m)
            float variation = ((id * 0x9E3779B9) & 0xFFFF) / 65535f * 0.2f;
            return baseOverhang + variation;
        }
    }

    /// <summary>A land-use area (water, parking, forest, farmland, residential, park...), clipped to its region.</summary>
    [Serializable]
    public sealed class MapArea
    {
        public long id;
        public string kind, name;
        public int[] pts;

        /// <summary>Provides a deterministic tree‑placement interval for forest land‑use areas. Returns 0 m for non‑forest areas; for forests returns a base spacing of 3 m plus a pseudo‑random offset in [0, 1] derived from the area's id.</summary>
        public float GetTreeSpacing()
        {
            if (kind != "forest")
                return 0f;
            // deterministic offset based on id, similar to other hashing patterns
            float offset = ((id * 0x4F1BBCDC) & 0xFFFF) / 65535f;
            return 3f + offset;
        }
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
