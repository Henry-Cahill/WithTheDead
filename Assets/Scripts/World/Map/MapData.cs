using System;

namespace CloneZ
{
    // The real-world map's data, as the world builder writes it (Tools/World/worldbuild.py, formats 1 and 2). The files
    // are JSON read with JsonUtility, so every field is public and named exactly as in the files. Coordinates are
    // decimetres from the region's south-west corner on Kentucky's flat grid (EPSG:3088, metres east and north), in
    // flat lists: [x0, z0, x1, z1, ...]. Format 2 only adds fields, arrays and area kinds: read from a format-1 build
    // its arrays are null, its strings null and its numbers 0, all of which mean "unknown" (as does ""). Plain data
    // only: how anything looks is up to the map's passes. Map data © OpenStreetMap contributors (ODbL).

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

    /// <summary>One region listed in the manifest, with how many of each feature it holds (the last three from
    /// format 2).</summary>
    [Serializable]
    public sealed class MapRegionEntry
    {
        public int rx, rz, streets, lines, buildings, areas, pois;
        public int barriers, power, nodes;
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
        /// <summary>Format 2: fences, walls, hedges, guard rails, retaining walls and tree rows (<see cref="MapLine.kind"/>
        /// "fence", "wall", "hedge", "guard_rail", "retaining_wall", "tree_row"; <see cref="MapLine.width"/> their
        /// thickness), clipped to the region like the lines.</summary>
        public MapLine[] barriers;
        /// <summary>Format 2: power lines, "line" (transmission, on towers) or "minor_line" (distribution, on wooden
        /// poles), clipped to the region like the lines. Every vertex of the mapped way is a support; clipping adds
        /// vertices on the clip edges, so supports go only at the vertices strictly inside a consumer's own square.</summary>
        public MapLine[] power;
        /// <summary>Format 2: point features; each belongs to the region its point is in.</summary>
        public MapNode[] nodes;
    }

    /// <summary>A piece of street (OpenStreetMap's highway=<see cref="kind"/>), clipped to its region.</summary>
    [Serializable]
    public sealed class MapStreet
    {
        public long id;
        public string kind;
        /// <summary>The street's name; its route number (<see cref="route"/>) when it has no name.</summary>
        public string name;
        /// <summary>Carriageway width in metres.</summary>
        public float width;
        public int lanes, layer;
        public bool oneway, bridge, tunnel;
        public int[] pts;
        /// <summary>Format 2: "asphalt", "concrete", "brick", "gravel", "dirt", or ""/null when not mapped.</summary>
        public string surface;
        /// <summary>Format 2: which sides have a sidewalk: "both", "left", "right", "no" (also when the sidewalk is mapped
        /// as a way of its own), or ""/null when not mapped.</summary>
        public string sidewalk;
        /// <summary>Format 2: whether the street is lit: 0 unknown, 1 yes, 2 no.</summary>
        public int lit;
        /// <summary>Format 2: the route number (OpenStreetMap's ref, e.g. "US 31E"), or ""/null.</summary>
        public string route;
    }

    /// <summary>A river, stream, canal, ditch or railway (or, in <see cref="MapRegion.barriers"/> and
    /// <see cref="MapRegion.power"/>, a barrier or power line), clipped to its region.</summary>
    [Serializable]
    public sealed class MapLine
    {
        public long id;
        public string kind, name;
        public float width;
        public int[] pts;
        /// <summary>Format 2: on a bridge, in a tunnel or culvert, and its layer.</summary>
        public bool bridge, tunnel;
        public int layer;
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
        /// <summary>Format 2: the roof's shape: "flat", "gabled", "hipped", "pyramidal", "skillion", "gambrel",
        /// "mansard", "half-hipped", "dome", "round", or ""/null when not mapped.</summary>
        public string roof;
        /// <summary>Format 2: the walls' material: "brick", "wood", "vinyl", "stone", "concrete", "metal", "glass",
        /// "stucco", or ""/null.</summary>
        public string material;
        /// <summary>Format 2: the walls' and the roof's colours as "#rrggbb", or ""/null.</summary>
        public string colour, roofColour;
        /// <summary>Format 2: storeys in the roof (0 when not mapped), and the year it was built (0 unknown).</summary>
        public int roofLevels, year;
        /// <summary>Not in the files: the correction applied to it when its region was read (<see cref="MapValidation"/>),
        /// for the choices the data can't carry (archetype, style, facing); null when it has none.</summary>
        [NonSerialized] public BuildingFix fix;
    }

    /// <summary>A land-use area (water, parking, forest, farmland, residential, park...; from format 2 also school,
    /// hospital, churchyard, lot, plaza, fuel, airfield and military), clipped to its region. A kind a reader doesn't
    /// know is plain ground.</summary>
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

    /// <summary>Format 2: a mapped point feature: a tree, street_lamp, traffic_signals, stop, give_way, crossing,
    /// level_crossing, bus_stop, hydrant, bench, waste_basket, post_box, telephone, vending, flagpole, water_tower, mast,
    /// memorial, gate or bollard; at (x, z) in decimetres like a point of interest.</summary>
    [Serializable]
    public sealed class MapNode
    {
        public long id;
        public string kind, name;
        public int x, z;
        /// <summary>The way it faces, in degrees clockwise from north, or -1 when unknown.</summary>
        public int dir;
    }
}
