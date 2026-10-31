using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace CloneZ
{
    // The buildings' validation: what the people checking the map found of each building (in the editor's Kentucky Atlas,
    // which builds it the way the game does, and in Tools/World/atlas, a browser map over aerial photographs), and the
    // corrections the game applies when it builds it. Kept in the world store beside the builds, one file per region of
    // the grid: world/validation/regions/<rx>_<rz>.json (a building is in the region its middle is in, as in the builds).
    // Records are keyed by the building's id (OpenStreetMap's area id: way id × 2, relation id × 2 + 1), so they outlive
    // any one build. JSON read and written with JsonUtility, so every field is public, named as in the files, and always
    // written: "", 0 and false mean "not checked" or "no correction". Tools/World/atlas/atlas.py writes the same files:
    // change both together.

    /// <summary>One region's validation file: world/validation/regions/&lt;rx&gt;_&lt;rz&gt;.json.</summary>
    [Serializable]
    public sealed class BuildingChecksFile
    {
        public int format = MapValidation.Format;
        public int rx, rz;
        public BuildingCheck[] buildings = new BuildingCheck[0];
    }

    /// <summary>What was found of one building, and how the game should build it instead.</summary>
    [Serializable]
    public sealed class BuildingCheck
    {
        public long id;
        public BuildingChecks checks = new BuildingChecks();
        public string note = "";
        public BuildingFix fix = new BuildingFix();
        /// <summary>What the game made of it when it was last built in the editor's atlas.</summary>
        public BuildingSeen game = new BuildingSeen();
        /// <summary>The build it was checked against, who saved it ("unity@PC", "web@PC") and when (ISO 8601).</summary>
        public string build = "", by = "", updated = "";
    }

    /// <summary>Each check is "" (not checked), "ok", "bad" or "na" (doesn't apply: a shed's inside).</summary>
    [Serializable]
    public sealed class BuildingChecks
    {
        /// <summary>The outline matches the building on the aerial photographs (and it's still there).</summary>
        public string footprint = "";
        /// <summary>What it is and what it was used for (the data's kind and use, or the corrections').</summary>
        public string kind = "";
        /// <summary>How the game builds it from outside: archetype, storeys, roof, walls.</summary>
        public string outside = "";
        public string windows = "";
        /// <summary>Its doors are where they should be and can be gone through.</summary>
        public string doors = "";
        /// <summary>Its interior: rooms, furniture, loot.</summary>
        public string inside = "";
    }

    /// <summary>
    /// Corrections the game applies when it reads the building (<see cref="MapValidation.Apply"/>): "" or 0 leaves the
    /// data (or the game's own choice) as it is. <see cref="kind"/>, <see cref="use"/>, <see cref="name"/>,
    /// <see cref="roof"/>, <see cref="material"/> and <see cref="storeys"/> replace what the map data says (so the
    /// interiors and the loot follow a corrected kind and use); <see cref="arch"/>, <see cref="style"/> and
    /// <see cref="facing"/> override the building pass's choices (BuildingPass.Classify).
    /// </summary>
    [Serializable]
    public sealed class BuildingFix
    {
        /// <summary>Not there (pulled down, or never was): the game leaves it out.</summary>
        public bool hide;
        /// <summary>One of <see cref="MapValidation.Kinds"/>.</summary>
        public string kind = "";
        /// <summary>An amenity ("fuel", "police"...) or "shop:&lt;kind&gt;"; "none" for no use at all.</summary>
        public string use = "";
        public string name = "";
        /// <summary>The building pass's archetype (BuildingPass.Arch: "House", "Store", "Church"...).</summary>
        public string arch = "";
        /// <summary>A house's style (BuildingPass.HouseStyle: "Ranch", "Victorian"...).</summary>
        public string style = "";
        /// <summary>One of <see cref="MapValidation.Roofs"/>.</summary>
        public string roof = "";
        /// <summary>One of <see cref="MapValidation.Materials"/>.</summary>
        public string material = "";
        /// <summary>The way the front (and its door) faces: one of <see cref="MapValidation.Facings"/>.</summary>
        public string facing = "";
        public int storeys;
    }

    /// <summary>What the game made of a building when the atlas built it (for the record, and for the browser tool,
    /// which can't build it).</summary>
    [Serializable]
    public sealed class BuildingSeen
    {
        /// <summary>When it was built (ISO 8601); "" when it never was.</summary>
        public string seen = "";
        public string arch = "", style = "", roof = "", wall = "";
        public int storeys;
        public bool enterable;
        public int doors, windows, blockedDoors;
        public bool furnished;
        /// <summary>What the interior was laid out as, and its rooms ("Living, Kitchen, Bedroom×2").</summary>
        public string use = "", rooms = "";
        public int containers, lights;
        /// <summary>What the atlas's own checks found wrong, one per line.</summary>
        public string problems = "";
    }

    /// <summary>How far a building's checks have got.</summary>
    public enum BuildingStatus { Unchecked, Partial, Ok, Bad }

    /// <summary>Reads, writes and applies the buildings' validation files (see the top of this file).</summary>
    public static class MapValidation
    {
        public const int Format = 1;
        public const string Ok = "ok", Bad = "bad", NotApplicable = "na";
        /// <summary>A <see cref="BuildingFix.use"/> that clears the data's use.</summary>
        public const string NoUse = "none";

        /// <summary>The checks, as named in the files, and as the tools label them.</summary>
        public static readonly string[] CheckNames = { "footprint", "kind", "outside", "windows", "doors", "inside" };
        public static readonly string[] CheckLabels = { "Footprint", "Kind / use", "Outside", "Windows", "Doors", "Inside" };

        /// <summary>The world builder's building kinds (worldbuild.py's BUILDING_KINDS).</summary>
        public static readonly string[] Kinds =
        {
            "house", "trailer", "apartments", "hotel", "commercial", "office", "retail", "industrial", "warehouse", "church",
            "school", "hospital", "civic", "garage", "shed", "barn", "silo", "greenhouse", "canopy", "ruin",
        };
        /// <summary>Roof shapes as the data names them (format 2's roof).</summary>
        public static readonly string[] Roofs = { "flat", "gabled", "hipped", "pyramidal", "skillion", "gambrel", "mansard", "round" };
        /// <summary>Wall materials as the data names them (format 2's material).</summary>
        public static readonly string[] Materials = { "brick", "wood", "vinyl", "stone", "concrete", "metal", "glass", "stucco" };
        /// <summary>The ways a front can face, clockwise from the grid's north (the game's +z).</summary>
        public static readonly string[] Facings = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        /// <summary>The validation folder of the store a build is in: world/validation beside world/builds.</summary>
        public static string FolderOf(string buildFolder)
        {
            if (string.IsNullOrEmpty(buildFolder)) return null;
            string builds = Path.GetDirectoryName(buildFolder.TrimEnd('\\', '/'));
            string world = string.IsNullOrEmpty(builds) ? null : Path.GetDirectoryName(builds);
            return Path.Combine(string.IsNullOrEmpty(world) ? buildFolder : world, "validation");
        }

        /// <summary>The file a region's records are in.</summary>
        public static string PathOf(string folder, int rx, int rz)
            => Path.Combine(folder, "regions", rx.ToString(CultureInfo.InvariantCulture) + "_" + rz.ToString(CultureInfo.InvariantCulture) + ".json");

        /// <summary>A region's records, or null when it has none (or the file can't be read: logged). Safe on any
        /// thread.</summary>
        public static BuildingChecksFile Load(string folder, int rx, int rz)
        {
            if (string.IsNullOrEmpty(folder)) return null;
            string path = PathOf(folder, rx, rz);
            try
            {
                return File.Exists(path) ? Read(path) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Couldn't read the building checks in " + path + ": " + e.GetType().Name + ": " + e.Message);
                return null;
            }
        }

        private static BuildingChecksFile Read(string path)
        {
            BuildingChecksFile file = JsonUtility.FromJson<BuildingChecksFile>(File.ReadAllText(path));
            if (file == null) return null;
            if (file.buildings == null) file.buildings = new BuildingCheck[0];
            foreach (BuildingCheck r in file.buildings) Complete(r);
            return file;
        }

        /// <summary>
        /// Saves one building's record into its region's file: read again just before writing, so a record saved
        /// meanwhile by the other tool isn't lost, the building's old record replaced (or dropped when
        /// <paramref name="record"/> says nothing: <see cref="IsEmpty"/>), and written under a temporary name and renamed,
        /// as everything in the store is. Throws when the store can't be written, or the file there can't be read
        /// (rather than write over it).
        /// </summary>
        public static void Save(string folder, int rx, int rz, BuildingCheck record)
        {
            string path = PathOf(folder, rx, rz);
            string dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            BuildingChecksFile file = File.Exists(path) ? Read(path) : null;
            if (file == null) file = new BuildingChecksFile();
            var list = new List<BuildingCheck>(file.buildings.Length + 1);
            foreach (BuildingCheck r in file.buildings)
                if (r != null && r.id != record.id) list.Add(r);
            Complete(record);
            if (!IsEmpty(record)) list.Add(record);
            list.Sort((a, b) => a.id.CompareTo(b.id));
            file.format = Format;
            file.rx = rx;
            file.rz = rz;
            file.buildings = list.ToArray();

            string tmp = Path.Combine(dir, "." + Path.GetFileName(path) + "." + Environment.MachineName + "."
                + System.Diagnostics.Process.GetCurrentProcess().Id + ".tmp");
            File.WriteAllText(tmp, JsonUtility.ToJson(file, true) + "\n");
            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }
            try { File.Replace(tmp, path, null); }
            catch (IOException)
            {
                // Some shares refuse ReplaceFile: then a moment without the file beats losing the record.
                File.Delete(path);
                File.Move(tmp, path);
            }
        }

        /// <summary>
        /// Applies a region's corrections (<paramref name="file"/>, from <see cref="Load"/>) to the region just read: a
        /// hidden building is taken out, the others get the corrected data and keep their <see cref="BuildingFix"/> in
        /// <see cref="MapBuilding.fix"/> for the building pass. Returns how many buildings were corrected. Safe on any
        /// thread (the streamer reads regions on workers).
        /// </summary>
        public static int Apply(MapRegion region, BuildingChecksFile file)
        {
            if (region == null || region.buildings == null || file == null || file.buildings == null) return 0;
            Dictionary<long, BuildingFix> fixes = null;
            foreach (BuildingCheck r in file.buildings)
            {
                if (r == null || r.fix == null || !HasFix(r.fix)) continue;
                if (fixes == null) fixes = new Dictionary<long, BuildingFix>();
                fixes[r.id] = r.fix;
            }
            if (fixes == null) return 0;
            int applied = 0;
            List<MapBuilding> kept = null;
            MapBuilding[] all = region.buildings;
            for (int i = 0; i < all.Length; i++)
            {
                MapBuilding b = all[i];
                if (b != null && fixes.TryGetValue(b.id, out BuildingFix fix))
                {
                    applied++;
                    if (fix.hide)
                    {
                        if (kept == null)
                        {
                            kept = new List<MapBuilding>(all.Length);
                            for (int k = 0; k < i; k++) kept.Add(all[k]);
                        }
                        continue;
                    }
                    Patch(b, fix);
                }
                if (kept != null) kept.Add(b);
            }
            if (kept != null) region.buildings = kept.ToArray();
            return applied;
        }

        private static void Patch(MapBuilding b, BuildingFix fix)
        {
            if (!string.IsNullOrEmpty(fix.kind) && fix.kind != b.kind)
            {
                // The builder writes a kind's default storeys when none are mapped: keep "none mapped" meaning that.
                if (b.levels == DefaultLevels(b.kind)) b.levels = DefaultLevels(fix.kind);
                b.kind = fix.kind;
            }
            if (fix.use == NoUse) b.use = "";
            else if (!string.IsNullOrEmpty(fix.use)) b.use = fix.use;
            if (!string.IsNullOrEmpty(fix.name)) b.name = fix.name;
            if (!string.IsNullOrEmpty(fix.roof)) b.roof = fix.roof;
            if (!string.IsNullOrEmpty(fix.material)) b.material = fix.material;
            if (fix.storeys > 0) b.levels = Math.Max(1, Math.Min(fix.storeys, 40));
            b.fix = fix;
        }

        /// <summary>The storeys the world builder writes for a kind when the data has none (worldbuild.py's
        /// DEFAULT_LEVELS).</summary>
        public static int DefaultLevels(string kind)
        {
            switch (kind)
            {
                case "apartments": case "hotel": case "hospital": return 3;
                case "office": case "school": case "civic": return 2;
                default: return 1;
            }
        }

        /// <summary>True when a correction changes anything.</summary>
        public static bool HasFix(BuildingFix f)
            => f != null && (f.hide || f.storeys > 0 || !string.IsNullOrEmpty(f.kind) || !string.IsNullOrEmpty(f.use)
                || !string.IsNullOrEmpty(f.name) || !string.IsNullOrEmpty(f.arch) || !string.IsNullOrEmpty(f.style)
                || !string.IsNullOrEmpty(f.roof) || !string.IsNullOrEmpty(f.material) || !string.IsNullOrEmpty(f.facing));

        /// <summary>True when a record says nothing: nothing checked, no note, no correction.</summary>
        public static bool IsEmpty(BuildingCheck r)
        {
            if (r == null) return true;
            for (int i = 0; i < CheckNames.Length; i++)
                if (!string.IsNullOrEmpty(GetCheck(r.checks, i))) return false;
            return string.IsNullOrEmpty(r.note) && !HasFix(r.fix);
        }

        /// <summary>Unchecked (no check made), Bad (any check failed), Ok (every check passed or doesn't apply), else
        /// Partial.</summary>
        public static BuildingStatus StatusOf(BuildingCheck r)
        {
            if (r == null || r.checks == null) return BuildingStatus.Unchecked;
            int done = 0;
            for (int i = 0; i < CheckNames.Length; i++)
            {
                string v = GetCheck(r.checks, i);
                if (v == Bad) return BuildingStatus.Bad;
                if (v == Ok || v == NotApplicable) done++;
            }
            return done == 0 ? BuildingStatus.Unchecked : done == CheckNames.Length ? BuildingStatus.Ok : BuildingStatus.Partial;
        }

        /// <summary>Check <paramref name="i"/> (in <see cref="CheckNames"/> order).</summary>
        public static string GetCheck(BuildingChecks c, int i)
        {
            if (c == null) return "";
            switch (i)
            {
                case 0: return c.footprint ?? "";
                case 1: return c.kind ?? "";
                case 2: return c.outside ?? "";
                case 3: return c.windows ?? "";
                case 4: return c.doors ?? "";
                case 5: return c.inside ?? "";
                default: return "";
            }
        }

        public static void SetCheck(BuildingChecks c, int i, string value)
        {
            value = value ?? "";
            switch (i)
            {
                case 0: c.footprint = value; break;
                case 1: c.kind = value; break;
                case 2: c.outside = value; break;
                case 3: c.windows = value; break;
                case 4: c.doors = value; break;
                case 5: c.inside = value; break;
            }
        }

        /// <summary>The direction (x east, y north) a <see cref="BuildingFix.facing"/> names; false for none.</summary>
        public static bool TryFacing(string facing, out Vector2 dir)
        {
            int i = string.IsNullOrEmpty(facing) ? -1 : Array.IndexOf(Facings, facing.Trim().ToUpperInvariant());
            if (i < 0)
            {
                dir = Vector2.zero;
                return false;
            }
            float a = i * 45f * Mathf.Deg2Rad;
            dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            return true;
        }

        /// <summary>A copy of a record (the tools edit a copy, and save it or throw it away).</summary>
        public static BuildingCheck Copy(BuildingCheck r)
        {
            BuildingCheck copy = r == null ? new BuildingCheck() : JsonUtility.FromJson<BuildingCheck>(JsonUtility.ToJson(r));
            return Complete(copy);
        }

        /// <summary>A record with nothing missing (a hand-edited file may leave parts out).</summary>
        public static BuildingCheck Complete(BuildingCheck r)
        {
            if (r == null) return null;
            if (r.checks == null) r.checks = new BuildingChecks();
            if (r.fix == null) r.fix = new BuildingFix();
            if (r.game == null) r.game = new BuildingSeen();
            r.note = r.note ?? "";
            r.build = r.build ?? "";
            r.by = r.by ?? "";
            r.updated = r.updated ?? "";
            return r;
        }

        /// <summary>The time now as the files have it (ISO 8601 with the UTC offset, to the second).</summary>
        public static string Now() => DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
    }
}
