#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using static CloneZ.KentuckyMap;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloneZ
{
    /// <summary>What the game made of one building when the Kentucky Atlas built it (<see cref="KentuckyAtlas.Build"/>),
    /// and what the atlas's own checks found. Positions are the preview's world, whose origin is the middle of the
    /// building's piece (<see cref="OriginX"/>, <see cref="OriginZ"/> on the grid).</summary>
    public sealed class AtlasReport
    {
        public long Id;
        /// <summary>The building pass raised it (false: too small, left out, or its piece failed: see <see cref="Error"/>).</summary>
        public bool Built;
        public string Error = "";
        public string Arch = "", Style = "", Roof = "", Wall = "";
        public int Storeys;
        public float Top;
        public bool Enterable;
        public int Doors, Windows, BlockedDoors;
        /// <summary>The interior was furnished (<see cref="Use"/>, <see cref="Rooms"/>...), as it is round a survivor.</summary>
        public bool Furnished;
        public string Use = "", Rooms = "";
        public int RoomCount, Containers, Lights, Objects;
        /// <summary>Its floors (<see cref="LevelLayout"/>): "" for the ground floor alone, else what there is ("basement +
        /// 2 storeys"), and a line for each level: what was furnished there and how the dead get to it.</summary>
        public string Levels = "";
        public readonly List<string> LevelLines = new List<string>();
        /// <summary>For a building with floors: the levels whose landing a NavMesh path from outside the front door reaches.</summary>
        public int LevelsReached;
        /// <summary>Its furnished floors' fixtures (doors, windows, switches, cupboards...), and those a survivor can't use from
        /// anywhere they can stand (<see cref="KentuckyAtlas"/>'s reach check, by the game's own targeting rules).</summary>
        public int Fixtures, FixturesOutOfReach;
        /// <summary>For the survey's breach check: holes knocked through its partitions, and those the NavMesh then led through.</summary>
        public int Breaches, BreachesThrough, OutsideBreaches, OutsideThrough;
        /// <summary>For the survey's window check: its working windows, its fixed ones (real openings, clear glass) and its painted
        /// ones; the working and fixed ones looked through, those the eye went through, and those boarded or curtained.</summary>
        public int WindowsWorking, WindowsClear, WindowsPainted, WindowsLooked, WindowsSeen, WindowsScreened;
        /// <summary>What the checks found wrong, and what they found worth knowing.</summary>
        public readonly List<string> Problems = new List<string>();
        public readonly List<string> Notes = new List<string>();
        /// <summary>The building in the preview, and a spot outside its front door looking at it.</summary>
        public Bounds Bounds;
        public Vector3 DoorSpot, DoorOutward = Vector3.back;
        public bool HasDoorSpot;
        /// <summary>The grid point at the preview's origin.</summary>
        public double OriginX, OriginZ;
        public double BuildMs, FurnishMs;
        public string BuiltAt = "";

        /// <summary>The spot outside the front door on the grid, and the way that faces the door (degrees from north).</summary>
        public double SpotGridX => OriginX + (HasDoorSpot ? DoorSpot.x : Bounds.center.x);
        public double SpotGridZ => OriginZ + (HasDoorSpot ? DoorSpot.z : Bounds.center.z - Bounds.extents.z - 2f);
        public float SpotYaw => HasDoorSpot ? Mathf.Atan2(-DoorOutward.x, -DoorOutward.z) * Mathf.Rad2Deg : 0f;

        /// <summary>One line on what was built.</summary>
        public string Summary => !Built ? "Not built: " + Error
            : Arch + (string.IsNullOrEmpty(Style) || Style == "None" ? "" : "/" + Style) + ", " + Storeys + (Storeys == 1 ? " storey, " : " storeys, ")
              + Roof + " roof, " + Wall + (Enterable ? "; enterable" : "; shut");

        /// <summary>The record's snapshot of it (<see cref="BuildingCheck.game"/>).</summary>
        public BuildingSeen ToSeen() => new BuildingSeen
        {
            seen = MapValidation.Now(), arch = Arch, style = Style == "None" ? "" : Style, roof = Roof, wall = Wall, storeys = Storeys,
            enterable = Enterable, doors = Doors, windows = Windows, blockedDoors = BlockedDoors, furnished = Furnished, use = Use,
            rooms = Rooms, containers = Containers, lights = Lights, problems = string.Join("\n", Problems),
        };
    }

    /// <summary>
    /// What the editor's Kentucky Atlas (WithTheDead ▸ Kentucky Atlas, CloneZ.EditorTools.KentuckyAtlasWindow) needs of the
    /// map's insides. <see cref="Build"/> builds the streamed piece that owns a building in the open scene, the way
    /// <see cref="KentuckyStreamer"/> builds it (Full detail, the zone's seed; no loot roll, no NavMesh), furnishes the
    /// interiors round it as <see cref="InteriorStreamer"/> does round a survivor, and reports what the game made of it
    /// with checks of its doors (can each be reached from both sides?), windows and inside. The preview is never saved
    /// with the scene, and is taken down (with the statics it touched) by <see cref="Clear"/>, which runs by itself before
    /// play mode (it keeps the scene and the statics), before scripts reload and when the scene closes. Also: sending the
    /// survivor to a building in a running raid (<see cref="GoThere"/>).
    /// </summary>
    public static class KentuckyAtlas
    {
        private const string RootName = "Kentucky Atlas (preview)";
        private static readonly int SolidMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);
        private static GameObject s_Root;
        private static readonly List<GameObject> s_Pieces = new List<GameObject>(), s_Overheads = new List<GameObject>();
        private static readonly List<GameObject> s_Extra = new List<GameObject>();
        private static readonly List<Mesh> s_Meshes = new List<Mesh>();
        private static float s_Clock = 100f;
        private static bool s_UpperShown = true;

        /// <summary>A preview stands in the scene.</summary>
        public static bool HasScene => s_Root != null;

        /// <summary>Roofs, upper storeys and ceilings are on show (off: the rooms seen from above, as the cutaway shows them).</summary>
        public static bool UpperShown => s_UpperShown;

        /// <summary>The building pass's archetypes and house styles (BuildingPass.Arch and HouseStyle), for corrections.</summary>
        public static string[] Archetypes => Enum.GetNames(typeof(BuildingPass.Arch));

        public static string[] HouseStyles
        {
            get
            {
                var names = new List<string>(Enum.GetNames(typeof(BuildingPass.HouseStyle)));
                names.Remove(nameof(BuildingPass.HouseStyle.None));
                return names.ToArray();
            }
        }

        /// <summary>Triangles covering an outline, as index triples (the map's own ear clipping). Safe on any thread.</summary>
        public static List<int> Triangulate(List<Vector2> outline) => MapGeometry.Triangulate(outline);

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            // Play mode keeps the scene and the statics (no domain or scene reload): never take a preview into it.
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingEditMode) Clear();
            };
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorSceneManager.sceneClosing += (scene, removing) =>
            {
                if (s_Root != null && s_Root.scene == scene) Clear();
            };
        }

        #region Building it
        /// <summary>
        /// Builds the piece that owns <paramref name="building"/> (region <paramref name="rx"/>, <paramref name="rz"/> of
        /// <paramref name="build"/>), and its eight neighbours when <paramref name="neighbours"/>, in the open scene round
        /// the origin, with the corrections in the store applied unless <paramref name="corrections"/> is off (and
        /// <paramref name="pending"/>, when given, as this building's correction instead of the stored one: what the atlas
        /// is editing, not saved yet); furnishes the interiors round it and checks it. Takes down any preview built before.
        /// Edit mode only.
        /// </summary>
        public static AtlasReport Build(MapBuild build, int rx, int rz, MapBuilding building, bool neighbours, bool corrections,
            BuildingFix pending = null)
        {
            var report = new AtlasReport { Id = building.id, BuiltAt = MapValidation.Now() };
            if (Application.isPlaying)
            {
                report.Error = "the atlas builds in edit mode only";
                return report;
            }
            Clear();
            // Read as mapped: the corrections are applied here, so the one being edited can stand in for the stored one.
            var source = new MapBuild(build.Id, build.Folder, build.Manifest) { UseCorrections = false };
            var fixes = new Corrections { On = corrections, Id = building.id, Pending = pending };
            const int size = ChunkSize;
            int per = source.RegionSize / size;
            if (building.pts == null || building.pts.Length < 6 || per <= 0)
            {
                report.Error = "it has no outline";
                return report;
            }

            // Its piece holds the average of its outline's points (as MapRegionIndex gives buildings to pieces); the
            // preview's origin is that piece's middle.
            double sx = 0, sz = 0;
            int n = building.pts.Length / 2;
            for (int k = 0; k + 1 < building.pts.Length; k += 2)
            {
                sx += building.pts[k];
                sz += building.pts[k + 1];
            }
            int cx = rx * per + Mathf.Clamp((int)Math.Floor(sx / n / (size * 10.0)), 0, per - 1);
            int cz = rz * per + Mathf.Clamp((int)Math.Floor(sz / n / (size * 10.0)), 0, per - 1);
            report.OriginX = (cx + 0.5) * size;
            report.OriginZ = (cz + 0.5) * size;
            var middle = new Vector2((float)((double)rx * source.RegionSize + sx / n / 10.0 - report.OriginX),
                (float)((double)rz * source.RegionSize + sz / n / 10.0 - report.OriginZ));

            ZoneDef zone = StatewideZone();
            bool winter = Climate.Winter;
            List<BuildingPass.Inspected> inspect = BuildingPass.Inspect;
            var noted = new List<BuildingPass.Inspected>();
            var before = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
            var regions = new Dictionary<long, MapRegionIndex>();
            var clock = Stopwatch.StartNew();
            try
            {
                s_Root = new GameObject(RootName);
                Transform world = new GameObject("Streamed").transform;
                world.SetParent(s_Root.transform, false);
                // The streamer's snowfield under every piece (KentuckyStreamer.MakeGround): the ground past the edge of the
                // piece built here, where a neighbour's own would be in the game.
                var under = new GameObject("Ground");
                under.transform.SetParent(s_Root.transform, false);
                var underBox = under.AddComponent<BoxCollider>();
                underBox.center = new Vector3(0f, -0.5f, 0f);
                underBox.size = new Vector3(size * 3f, 1f, size * 3f);
                var overhead = new GameObject("RaidOverhead");
                overhead.transform.SetParent(s_Root.transform, false);
                overhead.AddComponent<CeilingVisibility>();
                Climate.Winter = zone == null || zone.Winter;
                BuildingPass.Inspect = noted;
                InteriorStreamer.ToolMode = true;

                // Its own piece first (its notes are the first ones), then the ring round it.
                string error = BuildPiece(source, fixes, regions, zone, cx, cz, Vector3.zero, world, overhead.transform);
                int own = noted.Count;
                if (neighbours)
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (dx != 0 || dz != 0)
                                BuildPiece(source, fixes, regions, zone, cx + dx, cz + dz, new Vector3(dx * size, 0f, dz * size), world, overhead.transform);
                report.BuildMs = clock.Elapsed.TotalMilliseconds;
                Physics.SyncTransforms();

                BuildingPass.Inspected? found = null;
                float best = float.MaxValue;
                for (int i = 0; i < own; i++)
                {
                    if (noted[i].F.Data.id != building.id) continue;
                    float d = (noted[i].F.Middle - middle).sqrMagnitude;
                    if (d >= best) continue;
                    best = d;
                    found = noted[i];
                }
                if (found == null)
                {
                    report.Error = error ?? (fixes.Hides(source, rx, rz) ? "a correction leaves it out (not there)"
                        : "the building pass left it out (too small, or not in its piece's data)");
                    report.Problems.Add("Not built: " + report.Error);
                    return report;
                }
                Describe(report, found.Value);
                Furnish(report, found.Value);
                CheckOpenings(report, found.Value);
                CheckLevels(report, found.Value);
                CheckFixtures(report, found.Value);
                if (BreachTest) CheckBreaches(report, found.Value);
                if (WindowTest) CheckWindows(report, found.Value);
            }
            catch (Exception e)
            {
                report.Error = e.GetType().Name + ": " + e.Message;
                report.Problems.Add("The build threw: " + report.Error);
                Debug.LogException(e);
            }
            finally
            {
                BuildingPass.Inspect = inspect;
                InteriorStreamer.ToolMode = false;
                Climate.Winter = winter;
                // What the build left at the scene's top level (the interiors' streamer) goes with the preview.
                foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!before.Contains(go) && go != s_Root) s_Extra.Add(go);
                KeepOutOfScene();
                report.FurnishMs = clock.Elapsed.TotalMilliseconds - report.BuildMs;
            }
            return report;
        }

        // The statewide zone (its place names every piece and its seed seeds them, as the streamer's do).
        private static ZoneDef StatewideZone()
        {
            foreach (ZoneDef z in ZoneCatalog.All)
                if (z.RealStatewide && !string.IsNullOrEmpty(z.RealPlace)) return z;
            return null;
        }

        // The corrections a preview is built with: the store's, with the one being edited in place of its building's.
        private sealed class Corrections
        {
            public bool On;
            public long Id;
            public BuildingFix Pending;

            public BuildingChecksFile For(MapBuild build, int rx, int rz)
            {
                if (!On) return null;
                BuildingChecksFile file = MapValidation.Load(build.ValidationFolder, rx, rz);
                if (Pending == null) return file;
                var list = new List<BuildingCheck>();
                if (file != null)
                    foreach (BuildingCheck r in file.buildings)
                        if (r != null && r.id != Id) list.Add(r);
                list.Add(new BuildingCheck { id = Id, fix = Pending });
                return new BuildingChecksFile { rx = rx, rz = rz, buildings = list.ToArray() };
            }

            public MapRegion Read(MapBuild build, int rx, int rz)
            {
                if (!build.HasRegion(rx, rz)) return null;
                MapRegion data = build.LoadRegion(rx, rz);
                MapValidation.Apply(data, For(build, rx, rz));
                return data;
            }

            public bool Hides(MapBuild build, int rx, int rz)
            {
                BuildingChecksFile file = For(build, rx, rz);
                if (file == null) return false;
                foreach (BuildingCheck r in file.buildings)
                    if (r != null && r.id == Id && r.fix != null && r.fix.hide) return true;
                return false;
            }
        }

        // KentuckyStreamer's steps for one piece, all at once, its meshes baked on this thread.
        private static string BuildPiece(MapBuild build, Corrections fixes, Dictionary<long, MapRegionIndex> regions, ZoneDef zone, int cx,
            int cz, Vector3 middle, Transform world, Transform overhead)
        {
            const int size = ChunkSize;
            int per = build.RegionSize / size;
            int rx = cx >= 0 ? cx / per : -((-cx + per - 1) / per), rz = cz >= 0 ? cz / per : -((-cz + per - 1) / per);
            long key = ((long)rx << 32) | (uint)rz;
            if (!regions.TryGetValue(key, out MapRegionIndex region))
                regions[key] = region = new MapRegionIndex(fixes.Read(build, rx, rz), rx, rz, build.RegionSize, size);
            string name = "Piece " + cx + "_" + cz;
            var root = new GameObject(name);
            root.SetActive(false);
            root.transform.SetParent(world, false);
            var over = new GameObject(name);
            over.SetActive(false);
            over.transform.SetParent(overhead, false);
            s_Pieces.Add(root);
            s_Overheads.Add(over);
            int seed = zone != null ? zone.LayoutSeed : 1780;
            var job = new ChunkJob
            {
                Build = build, Region = region, Place = zone != null ? zone.RealPlace : "Bardstown", Cx = cx, Cz = cz,
                Seed = unchecked(seed * 486187739 + cx * 73856093 ^ cz * 19349663), Root = root.transform, Overhead = over.transform,
                Offset = middle, Detail = Detail.Full, Synchronous = true,
            };
            List<Mesh> outerMeshes = RaidMesh.CreatedMeshes;
            List<string> outerSigns = DecorTextures.SignsUsed;
            Vector3 outerUv = RaidMesh.PlanarOrigin;
            object owner = Shelter.Owner;
            Func<string, bool> gate = ItemDatabase.LootGate;
            Action<string, int> rolled = ItemDatabase.LootRolled;
            RaidMesh.CreatedMeshes = s_Meshes;
            DecorTextures.SignsUsed = new List<string>();
            RaidMesh.PlanarOrigin = new Vector3((float)Wrap((cx + 0.5) * size), 0f, (float)Wrap((cz + 0.5) * size));
            Shelter.Owner = root;
            ItemDatabase.LootGate = null;
            ItemDatabase.LootRolled = null;
            string error = null;
            try
            {
                using (IEnumerator<string> steps = ChunkSteps(job).GetEnumerator())
                    while (true)
                    {
                        if (job.Site != null) job.Site.BeginStep();
                        if (!steps.MoveNext()) break;
                    }
            }
            catch (Exception e)
            {
                job.Abandon();
                error = "its piece " + cx + "_" + cz + " failed to build: " + e.GetType().Name + ": " + e.Message;
                Debug.LogError("[KentuckyAtlas] " + error + "\n" + e);
            }
            finally
            {
                RaidMesh.CreatedMeshes = outerMeshes;
                DecorTextures.SignsUsed = outerSigns;
                RaidMesh.PlanarOrigin = outerUv;
                Shelter.Owner = owner;
                ItemDatabase.LootGate = gate;
                ItemDatabase.LootRolled = rolled;
            }
            root.transform.position = middle;
            over.transform.position = middle;
            root.SetActive(true);
            over.SetActive(s_UpperShown);
            return error;
        }

        private static double Wrap(double v)
        {
            double r = v % RaidMesh.PlanarPeriod;
            return r < 0 ? r + RaidMesh.PlanarPeriod : r;
        }

        private static void Describe(AtlasReport r, BuildingPass.Inspected b)
        {
            r.Built = true;
            r.Arch = b.Arch.ToString();
            r.Style = b.Arch == BuildingPass.Arch.House ? b.Style.ToString() : "";
            r.Roof = b.Roof.ToString();
            r.Wall = b.Wall.ToString();
            r.Storeys = b.Storeys;
            r.Top = b.Top;
            r.Enterable = b.Record != null;
            r.Doors = b.Record != null ? b.Record.Doors.Count : 0;
            r.Windows = b.Record != null ? b.Record.Windows.Count : 0;
            Rect f = b.F.Bounds;
            float top = Mathf.Max(b.Top, 3f);
            r.Bounds = new Bounds(new Vector3(f.center.x, top * 0.5f, f.center.y), new Vector3(f.width, top, f.height));
            if (b.Lite) r.Problems.Add("Built in Lite detail: no doors, interior or colliders");
            bool shutByDesign = b.Arch == BuildingPass.Arch.Silo || b.Arch == BuildingPass.Arch.Canopy || b.Arch == BuildingPass.Arch.Ruin
                || b.Arch == BuildingPass.Arch.Parking;
            if (!r.Enterable)
            {
                if (shutByDesign) r.Notes.Add("Shut, as every " + b.Arch + " is: no door, no inside");
                else r.Problems.Add("No door: a " + b.Arch + " should be enterable");
            }
            else if (r.Windows == 0 && b.Arch != BuildingPass.Arch.Shed && b.Arch != BuildingPass.Arch.Garage && b.Arch != BuildingPass.Arch.Barn
                     && b.Arch != BuildingPass.Arch.TobaccoBarn && b.Arch != BuildingPass.Arch.Warehouse && b.Arch != BuildingPass.Arch.Industrial
                     && b.Arch != BuildingPass.Arch.Rickhouse && b.Arch != BuildingPass.Arch.Greenhouse)
                r.Problems.Add("No windows on the ground floor");
        }

        // The interiors round it, a frame at a time as the game furnishes them round a survivor standing in it: its own,
        // then (for a second at most) its neighbours within reach.
        private static void Furnish(AtlasReport r, BuildingPass.Inspected b)
        {
            InteriorStreamer s = InteriorStreamer.Active;
            if (!r.Enterable || b.Lite) return;
            if (s == null)
            {
                r.Problems.Add("Inside: nothing was planned for any building in its piece");
                return;
            }
            var plans = new List<InteriorPlan>();
            s.Plans(plans);
            if (!plans.Exists(p => p.Id == r.Id))
            {
                r.Problems.Add("Inside: no plan could be made for it (see the console)");
                return;
            }
            var focus = new List<Vector3> { new Vector3(b.F.Middle.x, 0f, b.F.Middle.y) };
            var clock = Stopwatch.StartNew();
            int idle = 0, last = s.Furnished + s.FailedCount;
            while (s.BuildOf(r.Id) == null && clock.Elapsed.TotalSeconds < 30 && idle < 600)
            {
                s_Clock += 1f / 60f;
                s.Pump(focus, s_Clock);
                int now = s.Furnished + s.FailedCount;
                idle = now == last ? idle + 1 : 0;
                last = now;
            }
            InteriorBuild ib = s.BuildOf(r.Id);
            var more = Stopwatch.StartNew();
            idle = 0;
            while (more.Elapsed.TotalSeconds < 1.0 && idle < 30)
            {
                s_Clock += 1f / 60f;
                s.Pump(focus, s_Clock);
                int now = s.Furnished + s.FailedCount;
                idle = now == last ? idle + 1 : 0;
                last = now;
            }
            if (ib == null)
            {
                r.Problems.Add("Inside: furnishing it failed (see the console)");
                return;
            }
            r.Furnished = true;
            r.Use = ib.Plan.Use.ToString();
            r.RoomCount = ib.Plan.Rooms.Count;
            var rooms = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (PlanRoom room in ib.Plan.Rooms)
            {
                rooms.TryGetValue(room.Role.ToString(), out int k);
                rooms[room.Role.ToString()] = k + 1;
            }
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, int> kv in rooms)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(kv.Key);
                if (kv.Value > 1) sb.Append('×').Append(kv.Value);
            }
            r.Rooms = sb.ToString();
            r.Containers = ib.Containers.Count;
            r.Lights = ib.Lights;
            r.Objects = ib.Objects;
            if (r.RoomCount == 0) r.Problems.Add("Inside: no rooms");
            if (ib.Troubles != null)
                foreach (string t in ib.Troubles) r.Problems.Add("Inside: " + t);
        }

        // Every door reached from both sides (nothing solid where a survivor stands to go through it), and the spot
        // outside the front door.
        private static void CheckOpenings(AtlasReport r, BuildingPass.Inspected b)
        {
            EnterableBuilding rec = b.Record;
            if (rec == null || b.Lite) return;
            if (s_Root != null) global::CloneZ.FixtureReach.Reinsert(s_Root.transform);
            Physics.SyncTransforms();
            for (int i = 0; i < rec.Doors.Count; i++)
            {
                Doorway d = rec.Doors[i];
                var at = new Vector3(d.At.x, 0f, d.At.y);
                Vector3 outward = new Vector3(d.Outward.x, 0f, d.Outward.y).normalized;
                string label = (d.Edge == b.Front ? "Front door" : "Door " + (i + 1)) + " (wall " + d.Edge + ", " + Compass(outward) + ")";
                string outside = Blocker(at + outward * 0.9f), inside = Blocker(at - outward * 0.9f);
                bool into = MapGeometry.Contains(b.F.Pts, d.At - d.Outward.normalized * 0.9f);
                if (outside != null) r.Problems.Add(label + ": blocked outside by " + outside);
                if (inside != null) r.Problems.Add(label + ": blocked inside by " + inside);
                if (!into) r.Problems.Add(label + ": doesn't lead inside the outline");
                if (outside != null || inside != null || !into) r.BlockedDoors++;
                if (!r.HasDoorSpot || d.Edge == b.Front)
                {
                    r.HasDoorSpot = true;
                    r.DoorSpot = at + outward * 1.6f;
                    r.DoorOutward = outward;
                }
            }
            if (rec.Doors.Count == 0) r.Problems.Add("Listed as enterable, but it has no doorway");
            if (rec.Doors.Count > 0 && r.BlockedDoors == 0) r.Notes.Add("Every door can be reached from both sides");
        }

        #region Fixtures within reach
        // A building without floors above or below the ground one: its ground floor's fixtures (CheckLevels does every floor
        // of one with them).
        private static void CheckFixtures(AtlasReport r, BuildingPass.Inspected b)
        {
            if (!r.Furnished || (b.Record != null && b.Record.Levels != null)) return;
            InteriorStreamer s = InteriorStreamer.Active;
            InteriorBuild ib = s != null ? s.BuildOf(r.Id) : null;
            if (ib != null) r.Notes.Add("Fixtures:" + FixturesOn(r, "Inside", ib, 0f).Substring(1));
        }

        // A floor's fixtures as the survivor reaches them (FixtureReach: PlayerInteractor's rules). A note for the floor's line;
        // the fixtures nobody can use go in the problems.
        private static string FixturesOn(AtlasReport r, string name, InteriorBuild ib, float floor)
        {
            if (ib == null || ib.Root == null) return "";
            if (s_Root != null) global::CloneZ.FixtureReach.Reinsert(s_Root.transform);
            var fixtures = new List<Interactable>();
            ib.Root.GetComponentsInChildren(true, fixtures);
            if (ib.Town != null && ib.Town.Overhead != null)
            {
                var over = new List<Interactable>();
                ib.Town.Overhead.GetComponentsInChildren(true, over);
                fixtures.AddRange(over);
            }
            Physics.SyncTransforms();
            int total = 0, off = 0;
            var bad = new List<string>();
            foreach (Interactable f in fixtures)
            {
                if (f == null) continue;
                total++;
                if (!f.isActiveAndEnabled)
                {
                    off++;
                    continue;
                }
                string why = global::CloneZ.FixtureReach.Why(f, floor);
                if (why != null && why.StartsWith("no floor", StringComparison.Ordinal)) why += Where(ib.Plan, f.InteractPoint, floor);
                if (why != null) bad.Add(global::CloneZ.FixtureReach.Name(f) + RoomOf(ib.Plan, f.InteractPoint) + ": " + why);
            }
            r.Fixtures += total;
            r.FixturesOutOfReach += bad.Count;
            if (bad.Count > 0)
                r.Problems.Add("Fixtures: " + name + ": " + bad.Count + " of " + total + " can't be used from anywhere a survivor stands ("
                    + string.Join("; ", bad.GetRange(0, Mathf.Min(4, bad.Count))) + (bad.Count > 4 ? "; ..." : "") + ")");
            return "; " + total + " fixtures" + (bad.Count == 0 ? ", every one in a survivor's reach" : ", **" + bad.Count + " out of reach**")
                + (off > 0 ? " (" + off + " switched off)" : "");
        }

        private static string RoomOf(InteriorPlan plan, Vector3 p)
        {
            if (plan == null) return "";
            int i = plan.RoomAt(plan.ToLocal(new Vector2(p.x, p.z)));
            return i >= 0 ? " in the " + plan.Rooms[i].Name : "";
        }

        // Where a fixture with no floor round it stands: in or out of its building's outline, and what's under it, looking
        // down from well above its floor.
        private static string Where(InteriorPlan plan, Vector3 p, float floor)
        {
            var at = new Vector2(p.x, p.z);
            bool inside = plan != null && plan.Footprint != null && MapGeometry.Contains(plan.Footprint.Pts, at);
            var sb = new StringBuilder(" [at ").Append(F1(p.x)).Append(", ").Append(F1(p.z)).Append(inside ? ", inside the outline" : ", **outside the outline**");
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(p.x, floor + 6f, p.z), Vector3.down, 12f, ~(1 << CloneZConfig.IgnoreRaycastLayer),
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            sb.Append("; under it from above: ");
            if (hits.Length == 0) sb.Append("nothing");
            for (int i = 0; i < hits.Length && i < 3; i++)
                sb.Append(i == 0 ? "" : ", ").Append(PathOf(hits[i].collider.transform)).Append(" at ").Append(F1(hits[i].point.y));
            return sb.Append(']').ToString();
        }
        #endregion

        #region Breaches
        /// <summary>For the survey (<c>-clonez-atlas-breach</c>): each furnished floor's longest breakable partition knocked
        /// through, and the NavMesh baked round the building again, checked (see <see cref="CheckBreaches"/>).</summary>
        public static bool BreachTest;
        /// <summary>Where the breach check leaves a picture of each hole the NavMesh doesn't lead through (null: none).</summary>
        public static string BreachPictures;

        // Every furnished floor: its longest partition with room for a hole at its middle knocked through, as the build tool
        // does: the hole cut clean out of the wall's mesh and colliders, and a NavMesh baked round the building as the streamer
        // bakes the map's leading through it (where before it went round by the doors, if at all); boards nailed across it
        // close it, and taking them down opens it again. Then the next longest wall: the place the dead pressed against it
        // with their prey beyond pick to batter (a survivor's blows do nothing to it, a blast's do). And the ground floor taken
        // down and raised again, its holes with it.
        private static void CheckBreaches(AtlasReport r, BuildingPass.Inspected b)
        {
            InteriorStreamer s = InteriorStreamer.Active;
            if (s == null || !r.Furnished) return;
            for (int level = -1; level <= 6; level++)
            {
                InteriorBuild ib = s.BuildOf(r.Id, level);
                if (ib == null || ib.Root == null) continue;
                string name = LevelName(level);
                try { BreachLevel(r, b, ib, name, level == 0 && string.IsNullOrEmpty(r.Levels)); }
                catch (Exception e)
                {
                    r.Problems.Add("Breach: " + name + ": the check threw " + e.GetType().Name + ": " + e.Message);
                    Debug.LogException(e);
                }
            }
        }

        private static void BreachLevel(AtlasReport r, BuildingPass.Inspected b, InteriorBuild ib, string name, bool rebuild)
        {
            float floor = ib.Plan.FloorY;
            BakeFloor(b, floor);
            var walls = new List<PartitionWall>(ib.Root.GetComponentsInChildren<PartitionWall>(true));
            walls.Sort((x, y) => y.Length.CompareTo(x.Length));
            PartitionWall knock = null, batter = null;
            int roomy = 0;
            foreach (PartitionWall w in walls)
            {
                w.AimAt(w.transform.position + Vector3.up);
                if (!w.CanDemolish(out _)) continue;
                roomy++;
                if (!w.Walkable(w.Aimed)) continue;
                if (knock == null) knock = w;
                else
                {
                    batter = w;
                    break;
                }
            }
            if (knock == null)
            {
                var why = new StringBuilder();
                if (walls.Count > 0)
                {
                    PartitionWall longest = walls[0];
                    BreakableWall.Explain = new List<string>();
                    try { longest.AimAt(longest.transform.position + Vector3.up); }
                    finally
                    {
                        why.Append(" (the longest, ").Append(F1(longest.Length)).Append(" m, from ").Append(F1(-longest.Length * 0.5f)).Append(" to ")
                            .Append(F1(longest.Length * 0.5f)).Append(", blocked by ")
                            .Append(string.Join(", ", BreakableWall.Explain.GetRange(0, Mathf.Min(14, BreakableWall.Explain.Count)))).Append(')');
                        BreakableWall.Explain = null;
                    }
                }
                r.Notes.Add("Breach: " + name + ": " + walls.Count + " breakable partitions, " + roomy + " with room for a hole at its middle, "
                    + "none the NavMesh leads up to both sides there" + why + (ib.Plan.Level == 0 ? "; outside: " + Outside(r, b, ib, name) : ""));
                return;
            }
            var line = new StringBuilder("Breach: ").Append(name).Append(": a ").Append(F1(knock.Length)).Append(" m partition knocked through");
            knock.AimBox(out Vector3 centre, out _, out _);
            Vector3 across = (knock.WorldAt(0f, 0f, 1f) - knock.WorldAt(0f, 0f, 0f)).normalized;
            Vector3 one = centre + across * 1.1f, other = centre - across * 1.1f;
            one.y = other.y = floor + 0.1f;
            line.Append(" (from ").Append(RoomOf(ib.Plan, one)).Append(" to ").Append(RoomOf(ib.Plan, other)).Append(')');
            float before = PathBetween(one, other);
            knock.Dismantle();
            WallBreach hole = null;
            foreach (WallBreach h in knock.Holes)
                if (h != null && h.IsOpen) hole = h;
            if (hole == null)
            {
                r.Problems.Add("Breach: " + name + ": knocking the wall through made no hole");
                return;
            }
            r.Breaches++;
            Physics.SyncTransforms();
            int left = knock.TrianglesIn(hole), solid = knock.CollidersAcross(hole);
            if (left != 0) r.Problems.Add("Breach: " + name + ": " + left + " of the wall's triangles left inside the hole");
            if (solid != 0) r.Problems.Add("Breach: " + name + ": " + solid + " of the wall's colliders still across the hole");
            float through = BreachPath(b, floor, one, other);
            bool ok = through > 0f && through < 3.6f;
            if (ok) r.BreachesThrough++;
            else
            {
                Vector3 mid = centre;
                mid.y = floor + 0.1f;
                var sources = new List<NavMeshBuildSource>();
                foreach (GameObject piece in s_Pieces)
                    if (piece != null) KentuckyStreamer.Collect(piece.transform, null, sources);
                string there = Nearby(sources, mid, 0.2f);
                // A building's outside wall standing along the partition (where the map's footprints overlap) is the map's.
                if (there.Contains("Building shells/"))
                    r.Notes.Add("Breach: " + name + ": an outside wall stands in the hole (" + there + "): the map's footprints overlap there");
                else
                    r.Problems.Add("Breach: " + name + ": the NavMesh doesn't lead through the hole (" + Way(before) + " before, " + Way(through)
                        + " after; on one side " + Around(one, floor) + "; in the hole " + Around(mid, floor) + ", baked there: "
                        + there + "; on the other " + Around(other, floor) + ")");
                if (BreachPictures != null)
                    BreachPicture(System.IO.Path.Combine(BreachPictures, "breach_" + r.Id + "_" + ib.Plan.Level + ".png"), mid, floor, one, other);
            }
            line.Append(": the NavMesh's way from one side to the other ").Append(Way(before)).Append(" before, ").Append(Way(through)).Append(" through the hole");
            bool shots = BreachPictures != null && s_BreachShots < 3;
            if (shots) Look(System.IO.Path.Combine(BreachPictures, "hole_" + r.Id + "_" + ib.Plan.Level + ".png"), centre, across, floor);

            hole.BoardOver(1);
            Physics.SyncTransforms();
            if (shots)
            {
                s_BreachShots++;
                Look(System.IO.Path.Combine(BreachPictures, "boarded_" + r.Id + "_" + ib.Plan.Level + ".png"), centre, across, floor);
            }
            float boarded = BreachPath(b, floor, one, other);
            if (knock.CollidersAcross(hole) == 0) r.Problems.Add("Breach: " + name + ": nothing solid across the boarded hole");
            if (boarded > 0f && boarded < 3.6f) r.Problems.Add("Breach: " + name + ": the NavMesh still leads through the boarded hole (" + Way(boarded) + ")");
            hole.GetSalvage(out int planks, out _);
            hole.Dismantle();
            Physics.SyncTransforms();
            float again = BreachPath(b, floor, one, other);
            if (ok && !(again > 0f && again < 3.6f)) r.Problems.Add("Breach: " + name + ": taking the boards down didn't open it again (" + Way(again) + ")");
            line.Append("; boarded ").Append(Way(boarded)).Append(", the boards down (").Append(planks).Append(" planks back) ").Append(Way(again));

            if (batter != null) line.Append("; ").Append(Batter(r, name, batter));
            else line.Append("; no second wall to batter");
            if (ib.Plan.Level == 0) line.Append("; outside: ").Append(Outside(r, b, ib, name));
            if (rebuild) line.Append("; ").Append(Rebuilt(r, name, ib));
            r.Notes.Add(line.ToString());
        }

        // The ground floor's outside walls: the longest stretch with room somewhere along it for a hole that the NavMesh comes up
        // to both sides of knocked through there (cut clean out of the shell's walls and the site's colliders, the NavMesh leading
        // in through it from outside, boards closing it, their coming down opening it again); and the dead outside pressed
        // against the next such stretch with their prey inside picking it to batter.
        private static string Outside(AtlasReport r, BuildingPass.Inspected b, InteriorBuild ib, string name)
        {
            Transform site = ib.Root.transform.parent;
            Transform walls = site != null ? site.Find("Building shells/Walls " + ib.Plan.Id) : null;
            if (walls == null) return "no shell of its own";
            var list = new List<OutsideWall>(walls.GetComponentsInChildren<OutsideWall>(true));
            list.Sort((x, y) => y.Length.CompareTo(x.Length));
            OutsideWall knock = null, batter = null;
            int roomy = 0;
            foreach (OutsideWall w in list)
            {
                if (!RoomAlong(w)) continue;
                roomy++;
                if (knock == null) knock = w;
                else if (batter == null) batter = w;
            }
            if (knock == null)
            {
                string why = "";
                if (list.Count > 0)
                {
                    OutsideWall longest = list[0];
                    BreakableWall.Explain = new List<string>();
                    try { longest.AimAt(longest.transform.position + Vector3.up); }
                    finally
                    {
                        why = " (the longest, " + F1(longest.Length) + " m, from " + F1(-longest.Length * 0.5f) + " to " + F1(longest.Length * 0.5f)
                            + ", blocked by " + string.Join(", ", BreakableWall.Explain.GetRange(0, Mathf.Min(14, BreakableWall.Explain.Count))) + ")";
                        BreakableWall.Explain = null;
                    }
                }
                return list.Count + " stretches, none with room for a hole the NavMesh comes up to both sides of" + why;
            }
            string room = roomy + " of " + list.Count + " stretches with room for a hole; ";
            knock.AimBox(out Vector3 centre, out _, out _);
            Vector3 across = (knock.WorldAt(0f, 0f, 1f) - knock.WorldAt(0f, 0f, 0f)).normalized;   // outward
            Vector3 outside = centre + across * 1.2f, inside = centre - across * 1.2f;
            outside.y = inside.y = 0.1f;
            float before = PathBetween(outside, inside);
            knock.Dismantle();
            WallBreach hole = null;
            foreach (WallBreach h in knock.Holes)
                if (h != null && h.IsOpen) hole = h;
            if (hole == null)
            {
                r.Problems.Add("Breach: " + name + ": knocking an outside wall through made no hole");
                return "no hole";
            }
            r.OutsideBreaches++;
            Physics.SyncTransforms();
            int left = knock.TrianglesIn(hole), solid = knock.CollidersAcross(hole);
            if (left != 0) r.Problems.Add("Breach: " + name + ": " + left + " of the outside wall's triangles left inside the hole");
            if (solid != 0) r.Problems.Add("Breach: " + name + ": " + solid + " colliders still across the hole in the outside wall");
            float through = BreachPath(b, 0f, outside, inside);
            bool ok = through > 0f && through < 3.8f;
            if (ok) r.OutsideThrough++;
            else
            {
                r.Problems.Add("Breach: " + name + ": the NavMesh doesn't lead in through the hole in the outside wall (" + Way(before) + " before, "
                    + Way(through) + " after; outside " + Around(outside, 0f) + "; inside " + Around(inside, 0f) + ")");
                if (BreachPictures != null)
                    BreachPicture(System.IO.Path.Combine(BreachPictures, "breach_" + r.Id + "_out.png"), new Vector3(centre.x, 0.1f, centre.z), 0f, outside, inside);
            }
            bool shots = BreachPictures != null && s_OutsideShots < 2;
            if (shots)
            {
                s_OutsideShots++;
                Look(System.IO.Path.Combine(BreachPictures, "outside_" + r.Id + ".png"), centre, across, 0f);
                Look(System.IO.Path.Combine(BreachPictures, "outside_in_" + r.Id + ".png"), centre, -across, 0f);
            }
            hole.BoardOver(-1);
            Physics.SyncTransforms();
            float boarded = BreachPath(b, 0f, outside, inside);
            if (knock.CollidersAcross(hole) == 0) r.Problems.Add("Breach: " + name + ": nothing solid across the boarded hole in the outside wall");
            if (boarded > 0f && boarded < 3.8f) r.Problems.Add("Breach: " + name + ": the NavMesh still leads in through the boarded outside wall");
            hole.Dismantle();
            Physics.SyncTransforms();
            float again = BreachPath(b, 0f, outside, inside);
            if (ok && !(again > 0f && again < 3.8f)) r.Problems.Add("Breach: " + name + ": taking the boards off the outside wall didn't open it again");
            string said = room + "a " + F1(knock.Length) + " m stretch knocked through, the way in " + Way(before) + " before, " + Way(through)
                + " through the hole, " + Way(boarded) + " boarded, " + Way(again) + " with the boards down";
            said += batter == null ? "; no second stretch to batter" : BatterOutside(r, name, batter);
            // The game's view of the survivor just inside, the cutaway's stand-in of the walls up as another hole's knocked through.
            if (shots) said += CutawayAfter(r, System.IO.Path.Combine(BreachPictures, "outside_cut_" + r.Id + ".png"), inside, batter, list);
            return said;
        }

        // The dead outside against `batter` where it has room, their prey a couple of metres in: the place they pick to batter.
        private static string BatterOutside(AtlasReport r, string name, OutsideWall batter)
        {
            batter.AimBox(out Vector3 at, out _, out _);
            Vector3 o = (batter.WorldAt(0f, 0f, 1f) - batter.WorldAt(0f, 0f, 0f)).normalized;
            Vector3 zombie = at + o * 0.7f, prey = at - o * 2.5f;
            zombie.y = prey.y = 0.05f;
            bool listed = BreakableWall.All.Contains(batter);
            if (!listed) BreakableWall.All.Add(batter);
            try
            {
                WallBreach spot = BreakableWall.NearestObstructing(zombie, prey, CloneZConfig.ZombieAttackRange + 0.8f);
                if (spot == null || spot.Wall != batter)
                {
                    r.Problems.Add("Breach: " + name + ": the dead outside with their prey inside pick " + (spot == null ? "nowhere (" + BreakableWall.LastMiss + ")" : "another wall")
                        + " to batter");
                    return "; the dead outside pick nowhere to batter";
                }
                return "; the dead outside batter the next stretch";
            }
            finally
            {
                if (!listed) BreakableWall.All.Remove(batter);
            }
        }

        // The game's isometric view of the survivor at `inside`, the building's walls cut away round them as the cutaway has it;
        // with another hole knocked through its outside walls (in `then`, the stretch the dead were set on, else wherever one
        // has room) while the cutaway's stand-in of the walls is up, so it's made again round the new hole, as it is when the
        // dead break through with a survivor inside. Says how that went.
        private static string CutawayAfter(AtlasReport r, string file, Vector3 inside, OutsideWall then, List<OutsideWall> walls)
        {
            return IsoView(r, file, new Vector3(inside.x, 0f, inside.z), () =>
            {
                int made = MapCutaway.OverlaysMade;
                if (!KnockAnother(then, walls)) return "; no room for a second hole for the cutaway's picture";
                if (MapCutaway.OverlaysMade > made) return "; the cutaway's stand-in made again round a second hole";
                r.Problems.Add("Breach: the cutaway's stand-in of the walls wasn't made again when a second hole was knocked through them");
                return "; the cutaway's stand-in not made again round a second hole";
            }, "Breach: the cutaway's picture of a hole in an outside wall");
        }

        // The game's isometric view of a survivor (red) standing at `feet`, the walls round them cut away as the cutaway has
        // them, lit by day, to a PNG; `whileCut` (if any) runs once the cut is made, before the picture, and what it says is
        // returned. A failure is a problem of `what`.
        private static string IsoView(AtlasReport r, string file, Vector3 feet, Func<string> whileCut, string what)
        {
            if (s_Root == null) return "";
            foreach (MapCutawaySite site in s_Root.GetComponentsInChildren<MapCutawaySite>(true)) site.Register();
            MapCutaway.ResolveAll();
            var rig = new GameObject("[KentuckyAtlas] cutaway picture");
            var iso = new IsoCutaway();
            string said = "";
            try
            {
                var numbers = new GameObject("numbers");
                numbers.SetActive(false);
                numbers.transform.SetParent(rig.transform, false);
                IsoCameraFollow f = numbers.AddComponent<IsoCameraFollow>();
                float yaw = f.IsoYaw, pitch = f.IsoPitch;
                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.transform.SetParent(rig.transform, false);
                cam.enabled = false;
                cam.orthographic = true;
                cam.orthographicSize = 9f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.30f, 0.42f, 0.55f);
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 400f;
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.transform.SetParent(rig.transform, false);
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.intensity = 1.1f;
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                RenderSettings.sun = sun;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.48f);
                RenderSettings.fog = false;
                GameObject figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(figure.GetComponent<Collider>());
                figure.transform.SetParent(rig.transform, false);
                figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                var mat = new Material(lit != null ? lit : Shader.Find("Standard"));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.75f, 0.12f, 0.10f));
                figure.GetComponent<Renderer>().sharedMaterial = mat;
                var feetMark = new GameObject("Feet").transform;
                feetMark.SetParent(rig.transform, false);
                MapCutaway.RestoreAll();
                MapCutaway.Run(feet, yaw, pitch, 60f * Mathf.Cos(pitch * Mathf.Deg2Rad) + 1f, true, 5000f, true);
                if (whileCut != null) said = whileCut();
                figure.transform.position = feet + Vector3.up * 0.9f;
                Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
                cam.transform.SetPositionAndRotation(feet + Vector3.up * 1.2f - rot * Vector3.forward * 60f, rot);
                feetMark.position = feet;
                Physics.SyncTransforms();
                iso.Tick(cam.transform.position, cam.transform.forward, true, feetMark, 1.3f, true);
                Shoot(cam, file);
            }
            catch (Exception e)
            {
                r.Problems.Add(what + " threw " + e.GetType().Name + ": " + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                iso.RestoreAll();
                MapCutaway.RestoreAll();
                MapCutaway.Clear();
                Object.DestroyImmediate(rig);
            }
            return said;
        }

        // Knocks a hole through `first` where it was aimed, else the first of `walls` with room for one anywhere along it (the
        // build tool's say: the NavMesh needn't come up to it). False when none has room.
        private static bool KnockAnother(OutsideWall first, List<OutsideWall> walls)
        {
            if (first != null && !float.IsNaN(first.Aimed))
            {
                first.AimAt(first.WorldAt(first.Aimed, 1f, 0f));
                if (first.CanDemolish(out _))
                {
                    first.Dismantle();
                    return true;
                }
            }
            foreach (OutsideWall w in walls)
            {
                float reach = w.Length * 0.5f - BreakableWall.Width * 0.5f;
                for (int k = 0; ; k++)
                {
                    float d = (k + 1) / 2 * (k % 2 == 0 ? -1f : 1f);
                    if (Mathf.Abs(d) > reach + 0.6f) break;
                    w.AimAt(w.WorldAt(d, 1f, 0f));
                    if (!w.CanDemolish(out _)) continue;
                    w.Dismantle();
                    return true;
                }
            }
            return false;
        }

        private static int s_OutsideShots;

        // The first spot along `w` (its middle, then a metre either side at a time) with room for a hole that the NavMesh comes
        // up to both sides of, the build tool left aimed there; false when there's none.
        private static bool RoomAlong(OutsideWall w)
        {
            float reach = w.Length * 0.5f - BreakableWall.Width * 0.5f;
            for (int k = 0; ; k++)
            {
                float d = (k + 1) / 2 * (k % 2 == 0 ? -1f : 1f);   // 0, +1, -1, +2, -2, ...
                if (Mathf.Abs(d) > reach + 0.6f) return false;
                w.AimAt(w.WorldAt(d, 1f, 0f));
                if (w.CanDemolish(out _) && w.Walkable(w.Aimed)) return true;
            }
        }

        // The dead pressed against `w` at its middle with their prey a couple of metres beyond it: the place they pick to batter,
        // what a survivor's swing and a blast do to it, and that once it's broken through they pick it no more.
        private static string Batter(AtlasReport r, string name, PartitionWall w)
        {
            w.AimBox(out Vector3 centre, out _, out _);
            Vector3 across = (w.WorldAt(0f, 0f, 1f) - w.WorldAt(0f, 0f, 0f)).normalized;
            Vector3 zombie = centre + across * 0.6f, prey = centre - across * 2.5f;
            zombie.y = prey.y = w.transform.position.y + 0.05f;
            bool listed = BreakableWall.All.Contains(w);
            if (!listed) BreakableWall.All.Add(w);
            try
            {
                WallBreach spot = BreakableWall.NearestObstructing(zombie, prey, CloneZConfig.ZombieAttackRange + 0.8f);
                if (spot == null || spot.Wall != w)
                {
                    r.Problems.Add("Breach: " + name + ": the dead at a wall with their prey beyond it pick " + (spot == null ? "nowhere" : "another wall") + " to batter");
                    return "the dead pick nowhere to batter";
                }
                float full = spot.Health;
                spot.TakeDamage(new DamageInfo(500f, DamageKind.Melee, spot.transform.position, -across, null));
                float swung = spot.Health;
                spot.TakeDamage(new DamageInfo(50f, DamageKind.Explosion, spot.transform.position, -across, null));
                float blasted = spot.Health;
                if (swung != full) r.Problems.Add("Breach: " + name + ": a survivor's swing hurt the wall");
                if (!(blasted < full) || spot.State != WallBreach.Intact) r.Problems.Add("Breach: " + name + ": a small blast didn't crack the wall");
                bool same = BreakableWall.NearestObstructing(zombie, prey, CloneZConfig.ZombieAttackRange + 0.8f) == spot;
                if (!same) r.Problems.Add("Breach: " + name + ": the dead don't keep at the place they were battering");
                spot.BreakThrough(false);
                bool done = BreakableWall.NearestObstructing(zombie, prey, CloneZConfig.ZombieAttackRange + 0.8f) == null;
                if (!done) r.Problems.Add("Breach: " + name + ": the dead still batter where the wall's broken through");
                Vector3 at = spot.transform.position;
                float off = new Vector2(at.x - centre.x, at.z - centre.z).magnitude;
                return "the dead batter the next wall " + F1(off) + " m from its middle (" + F1(full) + " sound, " + F1(blasted)
                    + " after a small blast, a survivor's swing nothing)";
            }
            finally
            {
                if (!listed) BreakableWall.All.Remove(w);
            }
        }

        // The ground floor taken down and raised again, as the streamer does behind and before the survivors: its holes kept.
        private static string Rebuilt(AtlasReport r, string name, InteriorBuild ib)
        {
            int holes = 0;
            foreach (PartitionWall w in ib.Root.GetComponentsInChildren<PartitionWall>(true))
                foreach (WallBreach h in w.Holes)
                    if (h != null && h.State != WallBreach.Intact) holes++;
            Transform parent = ib.Root.transform.parent, overhead = ib.Town != null && ib.Town.Overhead != null ? ib.Town.Overhead.parent : null;
            ib.Discard();
            var again = new InteriorBuild(ib.Plan, parent, overhead, false);
            List<Mesh> meshes = RaidMesh.CreatedMeshes;
            RaidMesh.CreatedMeshes = again.Meshes;
            try { while (again.Advance()) { } }
            finally { RaidMesh.CreatedMeshes = meshes; }
            s_Meshes.AddRange(again.Meshes);
            int kept = 0;
            foreach (PartitionWall w in again.Root.GetComponentsInChildren<PartitionWall>(true))
                foreach (WallBreach h in w.Holes)
                    if (h != null && h.State != WallBreach.Intact) kept++;
            if (kept != holes) r.Problems.Add("Breach: " + name + ": raised again with " + kept + " of its " + holes + " holes");
            return "raised again with " + kept + " of its " + holes + " holes";
        }

        // The NavMesh's way (its length, m) between two points either side of a wall on a NavMesh baked round the building now;
        // -1 when there's none (or none walkable near either point).
        private static float BreachPath(BuildingPass.Inspected b, float floor, Vector3 one, Vector3 other)
            => BakeFloor(b, floor) ? PathBetween(one, other) : -1f;

        // A NavMesh round the building's floor at `floor`, from the preview's render meshes as the streamer collects them.
        private static bool BakeFloor(BuildingPass.Inspected b, float floor)
        {
            DropNav();
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            if (settings.agentTypeID == -1) settings.agentTypeID = 0;
            settings.minRegionArea = 2f;
            var sources = new List<NavMeshBuildSource>();
            foreach (GameObject piece in s_Pieces)
                if (piece != null) KentuckyStreamer.Collect(piece.transform, null, sources);
            Rect f = b.F.Bounds;
            var bounds = new Bounds(new Vector3(f.center.x, floor + 1f, f.center.y), new Vector3(f.width + 16f, 8f, f.height + 16f));
            s_NavData = new NavMeshData(0) { name = "Kentucky Atlas" };
            s_Nav = NavMesh.AddNavMeshData(s_NavData);
            return NavMeshBuilder.UpdateNavMeshData(s_NavData, settings, sources, bounds);
        }

        // The way (m) between two points on the NavMesh as it is; -1 when there's none.
        private static float PathBetween(Vector3 one, Vector3 other)
        {
            if (!NavMesh.SamplePosition(one, out NavMeshHit a, 0.8f, NavMesh.AllAreas) || !NavMesh.SamplePosition(other, out NavMeshHit c, 0.8f, NavMesh.AllAreas))
                return -1f;
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(a.position, c.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete ? Length(path) : -1f;
        }

        private static string Way(float length) => length < 0f ? "none" : F1(length) + " m";

        private static int s_BreachShots;

        // A picture of a hole at eye height from a couple of metres out on its +across side (or `distance` out, `rise` over eye
        // height), lit by a lamp over the shoulder and one beyond it.
        private static void Look(string file, Vector3 hole, Vector3 across, float floor, float distance = 2.6f, float rise = 0.5f)
        {
            var rig = new GameObject("[KentuckyAtlas] hole picture");
            try
            {
                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.transform.SetParent(rig.transform, false);
                cam.enabled = false;
                cam.fieldOfView = 70f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 60f;
                Vector3 at = new Vector3(hole.x, floor + 1.1f, hole.z), eye = at + across * distance + Vector3.up * rise;
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(at - eye));
                var lamp = new GameObject("Lamp").AddComponent<Light>();
                lamp.transform.SetParent(rig.transform, false);
                lamp.type = LightType.Point;
                lamp.range = 12f;
                lamp.intensity = 3f;
                lamp.transform.position = eye + Vector3.up * 0.3f + Vector3.Cross(Vector3.up, across) * 0.6f;
                var through = new GameObject("Beyond").AddComponent<Light>();
                through.transform.SetParent(rig.transform, false);
                through.type = LightType.Point;
                through.range = 8f;
                through.intensity = 2f;
                through.transform.position = at - across * 2f + Vector3.up * 0.8f;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.37f);
                RenderSettings.fog = false;
                Shoot(cam, file);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[KentuckyAtlas] hole picture " + file + ": " + e.Message);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        // Straight down on a hole (from under the ceiling), the NavMesh green over the floor and the two points the way was
        // looked for between marked (red), to a PNG.
        private static void BreachPicture(string file, Vector3 hole, float floor, Vector3 one, Vector3 other)
        {
            var rig = new GameObject("[KentuckyAtlas] breach picture");
            try
            {
                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.transform.SetParent(rig.transform, false);
                cam.enabled = false;
                cam.orthographic = true;
                cam.orthographicSize = 3.2f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 4f;
                cam.transform.SetPositionAndRotation(new Vector3(hole.x, floor + 2.1f, hole.z), Quaternion.Euler(90f, 0f, 0f));
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.transform.SetParent(rig.transform, false);
                sun.type = LightType.Directional;
                sun.intensity = 1.1f;
                sun.transform.rotation = Quaternion.Euler(70f, -30f, 0f);
                RenderSettings.sun = sun;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.52f);
                RenderSettings.fog = false;
                foreach (Vector3 p in new[] { one, other })
                {
                    GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.DestroyImmediate(mark.GetComponent<Collider>());
                    mark.transform.SetParent(rig.transform, false);
                    mark.transform.position = new Vector3(p.x, floor + 0.3f, p.z);
                    mark.transform.localScale = new Vector3(0.18f, 0.3f, 0.18f);
                    Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                    var mat = new Material(lit != null ? lit : Shader.Find("Standard"));
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.85f, 0.1f, 0.1f));
                    mark.GetComponent<Renderer>().sharedMaterial = mat;
                }
                NavOverlay(rig.transform, floor - 0.4f, floor + 1.5f);
                Shoot(cam, file);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[KentuckyAtlas] breach picture " + file + ": " + e.Message);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }
        #endregion

        #region Windows
        /// <summary>For the survey (<c>-clonez-atlas-windows</c>): every furnished floor's windows fitted as the window streamer
        /// fits them round a survivor, and each working and fixed one looked through (see <see cref="CheckWindows"/>).</summary>
        public static bool WindowTest;
        /// <summary>Where the window check leaves its pictures (null: none).</summary>
        public static string WindowPictures;
        private static int s_WindowShots, s_WindowIsoShots;
        private static readonly RaycastHit[] s_SightHits = new RaycastHit[16];

        // Every furnished floor's windows fitted (as the window streamer fits them round a survivor), and each working and fixed
        // window (a real opening) looked through, straight through its middle: nothing solid stops the eye in the wall but the
        // window's own boards or drawn curtains (meant to); its glass is see-through (alpha-blended: a fitted window's glazing,
        // a fixed one's in the shell, the stand-in glass in the working ones); and on the ground floor the NavMesh doesn't lead
        // through it. The painted ones (no floor at them) counted. The first few pictured from the street, from outside and
        // from the room, and in the game's view from inside.
        private static void CheckWindows(AtlasReport r, BuildingPass.Inspected b)
        {
            EnterableBuilding rec = b.Record;
            if (rec == null || b.Lite || rec.Footprint == null) return;
            InteriorStreamer s = InteriorStreamer.Active;
            LevelLayout L = rec.Levels;
            var fitted = new List<RaidWindow>();
            var interiors = new List<Transform>();
            Transform site = null;
            for (int level = L != null ? L.Lowest : 0; s != null && level <= (L != null ? L.Top : 0); level++)
            {
                InteriorBuild ib = s.BuildOf(r.Id, level);
                if (ib == null || ib.Root == null) continue;
                site = ib.Root.transform.parent;
                interiors.Add(ib.Root.transform);
                ib.FitSomeWindows(int.MaxValue);
                fitted.AddRange(ib.Root.GetComponentsInChildren<RaidWindow>(true));
            }
            Physics.SyncTransforms();
            List<Vector2> p = rec.Footprint.Pts;
            int n = p.Count;
            float t = rec.WallThickness > 0.01f ? rec.WallThickness : 0.22f;
            var problems = new List<string>();
            var lookOnto = new List<string>();
            bool clearLow = false, clearHigh = false, baked = false;
            int through = 0;
            WindowSpan pick = default;
            Vector3 pickFace = Vector3.zero, pickOut = Vector3.zero;
            float pickY = 0f, pickW = 0f;
            foreach (WindowSpan w in rec.Windows)
            {
                if (w.Working) r.WindowsWorking++;
                else if (w.Clear)
                {
                    r.WindowsClear++;
                    if (w.Storey == 0) clearLow = true;
                    else clearHigh = true;
                }
                else
                {
                    r.WindowsPainted++;
                    continue;
                }
                if (site == null || w.Edge < 0 || w.Edge >= n) continue;
                Vector2 a = p[w.Edge], c = p[(w.Edge + 1) % n];
                float len = Vector2.Distance(a, c);
                if (len < 0.05f) continue;
                Vector2 d = (c - a) / len, o = new Vector2(d.y, -d.x);
                float floor = L != null ? L.FloorOf(w.Storey) : 0f, along = (w.From + w.To) * 0.5f, y = floor + (w.Sill + w.Head) * 0.5f;
                Vector2 q = a + d * along;
                Vector3 face = site.TransformPoint(new Vector3(q.x, y, q.y)), outward = site.TransformDirection(new Vector3(o.x, 0f, o.y)).normalized;
                string name = LevelName(w.Storey) + " " + (w.Working ? "working" : "fixed") + " window " + F1(along) + " m along wall " + w.Edge;
                // Its fitted window, if it's a working one: boarded over, or its curtains drawn, it's meant to stop the eye.
                RaidWindow win = null;
                float best = 0.6f;
                foreach (RaidWindow fw in fitted)
                {
                    if (fw == null) continue;
                    float dist = Vector3.Distance(fw.InteractPoint, face - outward * (t * 0.5f));
                    if (dist >= best) continue;
                    best = dist;
                    win = fw;
                }
                r.WindowsLooked++;
                if (win != null && (win.IsBoarded || win.CurtainsDrawn))
                {
                    r.WindowsScreened++;
                    continue;
                }
                // Five lines of sight across it at its middle height: through if any meets nothing in the wall but what's inside the
                // building standing against it there (a partition's end, a flight of stairs), which is only what it looks onto.
                bool clear = false;
                Collider blocker = null;
                for (int k = 0; k < 5 && !clear; k++)
                {
                    Vector2 sq = a + d * Mathf.Lerp(w.From, w.To, 0.1f + 0.2f * k);
                    Vector3 sf = site.TransformPoint(new Vector3(sq.x, y, sq.y));
                    int hits = Physics.RaycastNonAlloc(sf + outward, -outward, s_SightHits, t + 2f, ~(1 << CloneZConfig.IgnoreRaycastLayer), QueryTriggerInteraction.Ignore);
                    Collider stop = null;
                    for (int h = 0; h < hits; h++)
                    {
                        float depth = s_SightHits[h].distance - 1f;   // into the wall from its outer face
                        if (depth > -0.08f && depth < t + 0.08f) stop = s_SightHits[h].collider;
                    }
                    if (stop == null) clear = true;
                    else if (blocker == null || (Inside(blocker, interiors) && !Inside(stop, interiors))) blocker = stop;
                }
                if (clear) r.WindowsSeen++;
                else if (Inside(blocker, interiors)) lookOnto.Add(name + " looks onto " + Owner(blocker));
                else problems.Add(name + ": the eye's stopped in the wall by " + Owner(blocker));
                if (w.Storey == 0)
                {
                    // The dead don't walk in through it.
                    if (!baked) baked = BakeFloor(b, 0f);
                    if (baked && LeadsThrough(site, a, d, o, t, w, face + outward * 1.2f, face - outward * (t + 1.2f)))
                    {
                        through++;
                        problems.Add(name + ": the NavMesh leads in through it");
                    }
                    // The widest on the ground floor pictured, a fixed one before a working one.
                    float width = w.To - w.From + (w.Clear ? 10f : 0f);
                    if (width > pickW)
                    {
                        pickW = width;
                        pick = w;
                        pickFace = face;
                        pickOut = outward;
                        pickY = y;
                    }
                }
            }
            if (baked) DropNav();

            // The glass: a fitted window's glazing, the shell's in its fixed windows and the stand-in glass in its working ones.
            int opaque = 0;
            foreach (RaidWindow fw in fitted)
            {
                if (fw == null || !fw.IsGlazed) continue;
                foreach (Renderer rr in fw.GetComponentsInChildren<Renderer>(true))
                    if (rr.name.EndsWith("Glazing", StringComparison.Ordinal) && !SeeThrough(rr, true)) opaque++;
            }
            if (opaque > 0) problems.Add(opaque + " of the fitted windows' glazing not see-through");
            string id = r.Id.ToString(CultureInfo.InvariantCulture);
            bool wallsClear = false, upperClear = false;
            if (s_Root != null)
                foreach (MeshRenderer mr in s_Root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string nm = mr.name;
                    if (nm == "Walls " + id) wallsClear |= SeeThrough(mr, false);
                    else if (nm == "Upper " + id || nm.StartsWith("Upper " + id + " L", StringComparison.Ordinal)) upperClear |= SeeThrough(mr, false);
                    else if (nm.StartsWith("Panes " + id + " L", StringComparison.Ordinal) && !SeeThrough(mr, true))
                        problems.Add("the stand-in glass " + nm + " not see-through");
                }
            if (clearLow && !wallsClear) problems.Add("the ground floor's fixed windows' glass not see-through");
            if (clearHigh && !upperClear) problems.Add("the upper floors' fixed windows' glass not see-through");

            if (r.WindowsWorking + r.WindowsClear + r.WindowsPainted == 0) return;
            r.Notes.Add("Windows: " + r.WindowsWorking + " working (" + fitted.Count + " fitted), " + r.WindowsClear + " fixed, " + r.WindowsPainted
                + " painted; the eye through " + r.WindowsSeen + " of the " + r.WindowsLooked + " looked through, " + r.WindowsScreened
                + " boarded or curtained" + (lookOnto.Count > 0 ? ", " + string.Join(", ", lookOnto) : "")
                + (through > 0 ? "; the NavMesh leads in through " + through : "")
                + (problems.Count > 0 ? "; " + string.Join("; ", problems) : ""));
            foreach (string problem in problems) r.Problems.Add("Windows: " + problem);

            if (WindowPictures == null || pickW <= 0f) return;
            if (s_WindowShots < 4)
            {
                s_WindowShots++;
                Vector3 middle = pickFace - pickOut * (t * 0.5f);
                string at = (pick.Clear ? "fixed" : "working") + "_" + id;
                Look(System.IO.Path.Combine(WindowPictures, "window_" + at + "_out.png"), middle, pickOut, pickY - 1.1f);
                Look(System.IO.Path.Combine(WindowPictures, "window_" + at + "_in.png"), middle, -pickOut, pickY - 1.1f);
                Look(System.IO.Path.Combine(WindowPictures, "window_" + at + "_street.png"), middle, pickOut, pickY - 1.1f, 9f, 2.2f);
            }
            if (s_WindowIsoShots < 3)
            {
                s_WindowIsoShots++;
                Vector3 feet = pickFace - pickOut * (t + 1.6f);
                IsoView(r, System.IO.Path.Combine(WindowPictures, "window_" + id + "_iso.png"), new Vector3(feet.x, 0f, feet.z), null,
                    "Windows: the game's view from inside");
            }
        }

        // True when a NavMesh path from `outside` to `inside` (either side of window w on the footprint's wall from a along d,
        // outward o, t thick) crosses the wall's line within the window.
        private static bool LeadsThrough(Transform site, Vector2 a, Vector2 d, Vector2 o, float t, in WindowSpan w, Vector3 outside, Vector3 inside)
        {
            outside.y = inside.y = 0.1f;
            if (!NavMesh.SamplePosition(outside, out NavMeshHit from, 0.6f, NavMesh.AllAreas) || !NavMesh.SamplePosition(inside, out NavMeshHit to, 0.6f, NavMesh.AllAreas))
                return false;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            Vector3[] corners = path.corners;
            Vector2 line = a - o * (t * 0.5f);
            for (int k = 1; k < corners.Length; k++)
            {
                Vector3 l0 = site.InverseTransformPoint(corners[k - 1]), l1 = site.InverseTransformPoint(corners[k]);
                Vector2 p0 = new Vector2(l0.x, l0.z), p1 = new Vector2(l1.x, l1.z);
                float s0 = Vector2.Dot(p0 - line, o), s1 = Vector2.Dot(p1 - line, o);
                if ((s0 > 0f) == (s1 > 0f)) continue;
                Vector2 x = p0 + (p1 - p0) * (s0 / (s0 - s1));
                float along = Vector2.Dot(x - a, d);
                if (along > w.From - 0.05f && along < w.To + 0.05f) return true;
            }
            return false;
        }

        // True when the renderer's materials are all alpha-blended (`all`), or (else) when any of them is.
        private static bool SeeThrough(Renderer r, bool all)
        {
            Material[] mats = r.sharedMaterials;
            bool any = false;
            foreach (Material m in mats)
            {
                bool clear = m != null && m.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if (all && !clear) return false;
                any |= clear;
            }
            return any;
        }

        private static string Owner(Collider c) => c.transform.parent != null ? c.transform.parent.name + "/" + c.name : c.name;

        // True for a collider inside the building (under one of its floors' interiors, or its floors and stairs).
        private static bool Inside(Collider c, List<Transform> interiors)
        {
            if (c == null) return false;
            foreach (Transform root in interiors)
                if (root != null && c.transform.IsChildOf(root)) return true;
            return c.name.StartsWith("Levels ", StringComparison.Ordinal) || c.name.StartsWith("Stairwell ", StringComparison.Ordinal);
        }
        #endregion

        #region Levels
        private static NavMeshDataInstance s_Nav;
        private static NavMeshData s_NavData;
        // The levels of the building the preview was last built for.
        private static LevelLayout s_LastLevels;
        private static long s_LastLevelsId;
        private static BuildingPass.Inspected? s_LastInspected;

        // A building with floors above or below the ground one: every floor furnished where it stands (nothing of it under its
        // floor or over its ceiling, and every fixture's handle on it), and a way to each floor's landing from outside the
        // front door on a NavMesh baked round it as the streamer bakes the map's (the dead follow the survivors up and down).
        private static void CheckLevels(AtlasReport r, BuildingPass.Inspected b)
        {
            EnterableBuilding rec = b.Record;
            if (rec == null || b.Lite) return;
            LevelLayout L = rec.Levels;
            if (L == null)
            {
                if (b.Storeys > 1)
                {
                    Vector2[] block = Levels.MainBlock(b.F);
                    string size = block.Length == 4 ? F1((block[1] - block[0]).magnitude) + " × " + F1((block[3] - block[0]).magnitude) + " m" : "none";
                    r.Problems.Add("Levels: " + b.Storeys + " storeys but no stairs (no room for a stairwell clear of the doors in its main block, " + size + ")");
                }
                return;
            }
            r.Levels = (L.Basement ? "basement + " : "") + (L.Top + 1) + (L.Top == 0 ? " storey" : " storeys");
            s_LastLevels = L;
            s_LastLevelsId = r.Id;
            s_LastInspected = b;
            r.Notes.Add("Levels: " + r.Levels + "; a stairwell of " + F1(L.Length) + " × " + F1(L.Width) + " m off a landing hall of "
                + F1(L.Hall.width) + " × " + F1(L.Hall.height) + " m" + (L.Narrow ? " beside it, its landing open onto that"
                    : L.OpenSide ? " across the building, its landing open on its side" : ""));

            // Every floor furnished, as the streamer does them round a survivor (the ground floor was, by Furnish).
            var builds = new Dictionary<int, InteriorBuild>();
            InteriorStreamer s = InteriorStreamer.Active;
            if (s != null && r.Furnished)
            {
                var plans = new List<InteriorPlan>();
                s.Plans(plans);
                var wanted = new List<int>();
                foreach (InteriorPlan p in plans)
                    if (p.Id == r.Id) wanted.Add(p.Level);
                var focus = new List<Vector3> { new Vector3(b.F.Middle.x, 0f, b.F.Middle.y) };
                var clock = Stopwatch.StartNew();
                int idle = 0, last = s.Furnished + s.FailedCount;
                while (clock.Elapsed.TotalSeconds < 30 && idle < 600)
                {
                    bool all = true;
                    foreach (int level in wanted)
                        if (s.BuildOf(r.Id, level) == null) { all = false; break; }
                    if (all) break;
                    s_Clock += 1f / 60f;
                    s.Pump(focus, s_Clock);
                    int now = s.Furnished + s.FailedCount;
                    idle = now == last ? idle + 1 : 0;
                    last = now;
                }
                foreach (int level in wanted)
                {
                    InteriorBuild ib = s.BuildOf(r.Id, level);
                    if (ib != null) builds[level] = ib;
                }
            }

            Physics.SyncTransforms();
            bool baked = BakeAround(b, L);
            // From just inside the front door (the doorway itself is the game's business, not the stairs').
            NavMeshHit start = default;
            Vector3 entry = r.DoorSpot - r.DoorOutward * 2.6f;
            bool from = baked && r.HasDoorSpot && NavMesh.SamplePosition(entry, out start, 1.2f, NavMesh.AllAreas) && Mathf.Abs(start.position.y) < 0.45f;
            if (baked && r.HasDoorSpot && !from) r.Problems.Add("Levels: nothing walkable on the NavMesh just inside the front door");
            var path = new NavMeshPath();
            if (from && NavMesh.SamplePosition(r.DoorSpot, out NavMeshHit outside, 2f, NavMesh.AllAreas))
                r.Notes.Add(NavMesh.CalculatePath(outside.position, start.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete
                    ? "The NavMesh runs in through the front door"
                    : "The NavMesh doesn't run in through the front door (the doorway is narrower than its agents)");
            try
            {
                for (int level = L.Lowest; level <= L.Top; level++)
                {
                    float y0 = L.FloorOf(level), y1 = L.CeilingOf(level);
                    string name = LevelName(level);
                    var line = new StringBuilder(name).Append(" (its floor at ").Append(F1(y0)).Append(" m): ");
                    if (builds.TryGetValue(level, out InteriorBuild ib))
                    {
                        line.Append(ib.Plan.Rooms.Count).Append(" rooms (").Append(RoomList(ib.Plan)).Append("), ").Append(ib.Containers.Count)
                            .Append(" containers");
                        CheckHeights(r, name, ib, y0, y1);
                        if (ib.Troubles != null)
                            foreach (string t in ib.Troubles) r.Problems.Add("Levels: " + name + ": " + t);
                        line.Append(FixturesOn(r, name, ib, y0));
                        if (baked) line.Append(Doorways(ib));
                    }
                    else if (r.Furnished)
                    {
                        line.Append("not furnished");
                        if (level != 0) r.Problems.Add("Levels: " + name + " wasn't furnished (see the console)");
                    }
                    if (from)
                    {
                        Vector2 hall = L.ToSite(L.Hall.center);
                        var target = new Vector3(hall.x, y0 + 0.1f, hall.y);
                        if (!NavMesh.SamplePosition(target, out NavMeshHit end, 1.2f, NavMesh.AllAreas) || Mathf.Abs(end.position.y - y0) > 0.45f)
                        {
                            r.Problems.Add("Levels: no NavMesh on " + name + "'s landing");
                            line.Append("; no NavMesh on its landing");
                        }
                        else if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                        {
                            r.Problems.Add("Levels: the dead can't get to " + name + " from inside the front door");
                            line.Append("; **no way there** from inside the front door");
                            Vector3[] corners = path.corners;
                            if (corners.Length > 0)
                            {
                                Vector3 stop = corners[corners.Length - 1];
                                line.Append(" (the path stops at ").Append(F1(stop.x)).Append(", ").Append(F1(stop.y)).Append(", ").Append(F1(stop.z))
                                    .Append(", ").Append(F1(Vector3.Distance(stop, end.position))).Append(" m short; ").Append(Around(stop, stop.y)).Append(')');
                            }
                            if (level > L.Lowest) line.Append("; the flights up to it: ").Append(StairProbe(L, level - 1, out bool gap)).Append(gap ? CoreMap(L, level - 1) : "");
                            else if (level < L.Top) line.Append("; the flights up from it: ").Append(StairProbe(L, level, out bool gap)).Append(gap ? CoreMap(L, level) : "");
                        }
                        else
                        {
                            r.LevelsReached++;
                            line.Append("; reached from inside the front door (").Append(F1(Length(path))).Append(" m)");
                        }
                    }
                    r.LevelLines.Add(line.ToString());
                }
            }
            finally
            {
                DropNav();
            }
            r.Notes.AddRange(r.LevelLines);
        }

        private static string LevelName(int level) => level < 0 ? "Basement" : level == 0 ? "Ground floor" : "Floor " + (level + 1);

        // A floor's doorways between its rooms the dead don't get through on the NavMesh (from a step before one to a step past
        // it), with what a body in the first few would touch.
        private static string Doorways(InteriorBuild ib)
        {
            InteriorPlan plan = ib.Plan;
            var path = new NavMeshPath();
            int doors = 0, shut = 0;
            var sb = new StringBuilder();
            foreach (PlanWall w in plan.Walls)
            {
                if (!w.HasDoor) continue;
                doors++;
                Vector2 mid = w.AlongX ? new Vector2(w.DoorPos, w.Coord) : new Vector2(w.Coord, w.DoorPos);
                Vector2 n = w.AlongX ? new Vector2(0f, 1f) : new Vector2(1f, 0f);
                Vector2 a = plan.ToSite(mid - n * 0.8f), b = plan.ToSite(mid + n * 0.8f), c = plan.ToSite(mid);
                float y = plan.FloorY;
                bool through = NavMesh.SamplePosition(new Vector3(a.x, y + 0.05f, a.y), out NavMeshHit ha, 0.5f, NavMesh.AllAreas)
                    && NavMesh.SamplePosition(new Vector3(b.x, y + 0.05f, b.y), out NavMeshHit hb, 0.5f, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete
                    && Length(path) < 4f;
                if (through) continue;
                shut++;
                if (shut <= 2)
                    sb.Append(shut == 1 ? " (" : "; ").Append(plan.Rooms[w.Minus].Name).Append('–').Append(plan.Rooms[w.Plus].Name).Append(": ")
                        .Append(Around(new Vector3(c.x, y + 0.05f, c.y), y));
            }
            if (shut == 0) return doors == 0 ? "" : "; the dead get through all " + doors + " of its doorways";
            return "; the dead don't get through " + shut + " of its " + doors + " doorways" + sb + ")";
        }

        // Storey `level`'s stairs on the NavMesh, from its landing hall to the next floor's: whether it covers the hall, the
        // landing, the flight up, the half landing, the flight on up, the landing above and the hall above (x where it does,
        // - where it doesn't), and between two it covers whether the dead get from the one to the other (> where they do, /
        // where they don't).
        private static string StairProbe(LevelLayout L, int level, out bool gap)
        {
            const float h = RaidBuilding.Half, fw = Levels.FlightWidth;
            float f0 = L.FloorOf(level), f1 = L.FloorOf(level + 1), rise = (f1 - f0) * 0.5f;
            float near = L.Near, far = near + L.Run, mid = L.Width * 0.5f, a = h + fw * 0.5f, b = L.Width - h - fw * 0.5f;
            Vector2 hall = L.ToSite(L.Hall.center);
            var probes = new[]
            {
                (hall, f0), (L.CoreSite(near * 0.5f, mid), f0), (L.CoreSite(near + L.Run * 0.5f, a), f0 + rise * 0.5f),
                (L.CoreSite(far + Levels.Landing * 0.5f, mid), f0 + rise), (L.CoreSite(near + L.Run * 0.5f, b), f0 + rise * 1.5f),
                (L.CoreSite(near * 0.5f, mid), f1), (hall, f1),
            };
            var sb = new StringBuilder();
            var path = new NavMeshPath();
            Vector3 last = default;
            bool had = false;
            string around = null;
            for (int i = 0; i < probes.Length; i++)
            {
                (Vector2 p, float y) = probes[i];
                var at = new Vector3(p.x, y + 0.05f, p.y);
                bool on = NavMesh.SamplePosition(at, out NavMeshHit hit, 0.35f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - y) < 0.3f;
                bool linked = true;
                if (i > 0)
                {
                    linked = !on || !had || NavMesh.CalculatePath(last, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
                    sb.Append(!on || !had ? ' ' : linked ? '>' : '/');
                }
                sb.Append(on ? 'x' : '-');
                if (around == null && !on) around = Around(at, y);
                else if (around == null && !linked)
                {
                    Vector3[] cs = path.corners;
                    Vector3 stop = cs.Length > 0 ? cs[cs.Length - 1] : last;
                    Vector2 d = L.ToLocal(new Vector2(stop.x, stop.z)) - L.Corner;
                    around = "(the way on stops " + F1(Vector3.Distance(stop, hit.position)) + " m short, " + F1(Vector2.Dot(d, L.Up)) + " m along the core and "
                        + F1(Vector2.Dot(d, L.Side)) + " m across) " + Around(stop, stop.y);
                }
                had = on;
                if (on) last = hit.position;
            }
            sb.Append(" (hall, landing, flight up, half landing, flight on, landing above, hall above)");
            gap = around != null;
            if (around != null) sb.Append("; at the first gap ").Append(around);
            return sb.ToString();
        }

        // Storey `level`'s stairwell as the NavMesh covers it, a quarter metre a cell at the height a body stands there (on the
        // landing, up a flight, on the half landing), as an indented block for the report: x where it does; where it doesn't,
        // a letter for the render mesh (keyed below the map) with the lowest surface over it between a body's knee and its
        // head, else L for the stairs' own collider in a body's way, S the stairwell's walls, # anything else, . nothing;
        // | the wall between the flights.
        private static string CoreMap(LevelLayout L, int level)
        {
            const float h = RaidBuilding.Half, fw = Levels.FlightWidth, cell = 0.25f;
            float f0 = level == 0 ? Levels.GroundFloorY : L.FloorOf(level), f1 = level + 1 == 0 ? Levels.GroundFloorY : L.FloorOf(level + 1);
            float rise = (f1 - f0) * 0.5f, near = L.Near, far = near + L.Run, a1 = h + fw, b0 = a1 + Levels.Spine;
            var sb = new StringBuilder("\n  ```\n  near end at the left, the A side (the flight up) at the top\n");
            var hits = new Collider[8];
            int nu = Mathf.CeilToInt(L.Length / cell), nv = Mathf.CeilToInt(L.Width / cell);
            float HeightAt(float u, float v) => u < near ? f0 : u > far ? f0 + rise : v <= a1 ? f0 + (u - near) / L.Run * rise : f0 + rise + (far - u) / L.Run * rise;
            var keys = new List<string>();
            var sources = new List<NavMeshBuildSource>();
            foreach (GameObject piece in s_Pieces)
                if (piece != null) KentuckyStreamer.Collect(piece.transform, null, sources);
            int[,] over = Overhead(L, cell, nu, nv, HeightAt, f0 - 0.5f, f1 + 2.5f, sources, keys);
            string bare = null;
            for (int iv = 0; iv < nv; iv++)
            {
                float v = (iv + 0.5f) * cell;
                sb.Append("  ");
                for (int iu = 0; iu < nu; iu++)
                {
                    float u = (iu + 0.5f) * cell;
                    bool spine = u >= near && u <= far && v > a1 && v < b0 + cell * 0.5f;
                    if (spine) { sb.Append('|'); continue; }
                    float y = HeightAt(u, v);
                    Vector2 p = L.CoreSite(u, v);
                    var at = new Vector3(p.x, y, p.y);
                    bool on = NavMesh.SamplePosition(at + Vector3.up * 0.05f, out NavMeshHit hit, 0.2f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - y) < 0.3f
                        && new Vector2(hit.position.x - at.x, hit.position.z - at.z).magnitude < 0.15f;
                    if (on) { sb.Append('x'); continue; }
                    if (bare == null && u < near && u > 0.3f && v > 0.7f && v < L.Width - 0.7f)
                        bare = "by the first bare cell on the landing (" + F1(u) + " m along, " + F1(v) + " m across), within a body's reach: " + Nearby(sources, at, 0.6f);
                    if (over[iu, iv] >= 0) { sb.Append((char)('a' + over[iu, iv])); continue; }
                    char what = '.';
                    int n = Physics.OverlapCapsuleNonAlloc(at + Vector3.up * 0.55f, at + Vector3.up * 1.75f, 0.2f, hits, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < n; i++)
                    {
                        string name = hits[i].transform.name;
                        if (name == "Ground") continue;   // the snow's collider, a metre deep under the whole piece (a body passes through it over a basement)
                        char c = name.StartsWith("Levels ", StringComparison.Ordinal) ? 'L' : name.StartsWith("Stairwell ", StringComparison.Ordinal) ? 'S' : '#';
                        if (what == '.' || c == '#') what = c;
                    }
                    sb.Append(what);
                }
                sb.Append('\n');
            }
            for (int k = 0; k < keys.Count; k++) sb.Append("  ").Append((char)('a' + k)).Append(": ").Append(keys[k]).Append('\n');
            if (bare != null) sb.Append("  ").Append(bare).Append('\n');
            return sb.Append("  ```").ToString();
        }

        // The render meshes the NavMesh is baked from with a triangle within `reach` of a point, between just over its floor and
        // a body's height (by each triangle's extent).
        private static string Nearby(List<NavMeshBuildSource> sources, Vector3 at, float reach)
        {
            var names = new List<string>();
            foreach (NavMeshBuildSource s in sources)
            {
                if (s.shape != NavMeshBuildSourceShape.Mesh || !(s.sourceObject is Mesh mesh) || !mesh.isReadable) continue;
                string name = s.component != null ? PathOf(s.component.transform) : mesh.name;
                if (names.Contains(name)) continue;
                Vector3[] verts = mesh.vertices;
                int[] tris = mesh.triangles;
                Matrix4x4 m = s.transform;
                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    Vector3 p = m.MultiplyPoint3x4(verts[tris[t]]), q = m.MultiplyPoint3x4(verts[tris[t + 1]]), r = m.MultiplyPoint3x4(verts[tris[t + 2]]);
                    if (Mathf.Max(p.y, q.y, r.y) < at.y + 0.1f || Mathf.Min(p.y, q.y, r.y) > at.y + 2f) continue;
                    float dx = Mathf.Max(0f, Mathf.Min(p.x, q.x, r.x) - at.x, at.x - Mathf.Max(p.x, q.x, r.x));
                    float dz = Mathf.Max(0f, Mathf.Min(p.z, q.z, r.z) - at.z, at.z - Mathf.Max(p.z, q.z, r.z));
                    if (dx * dx + dz * dz > reach * reach) continue;
                    names.Add(name + " (" + F1(Mathf.Min(p.y, q.y, r.y)) + ".." + F1(Mathf.Max(p.y, q.y, r.y)) + " m)");
                    break;
                }
            }
            return names.Count == 0 ? "nothing" : string.Join(", ", names);
        }

        // For each cell of the core, which of the render meshes the NavMesh is baked from has the lowest surface over it between
        // a body's knee and its head (keyed into `keys` by object and material, with the heights it was found at; at most
        // 26), or -1.
        private static int[,] Overhead(LevelLayout L, float cell, int nu, int nv, Func<float, float, float> heightAt, float y0, float y1,
            List<NavMeshBuildSource> sources, List<string> keys)
        {
            var who = new int[nu, nv];
            var low = new float[nu, nv];
            for (int iu = 0; iu < nu; iu++)
                for (int iv = 0; iv < nv; iv++) { who[iu, iv] = -1; low[iu, iv] = float.MaxValue; }
            var tri = new Vector3[3];
            var heights = new List<Vector2>();
            foreach (NavMeshBuildSource s in sources)
            {
                if (s.shape != NavMeshBuildSourceShape.Mesh || !(s.sourceObject is Mesh mesh) || !mesh.isReadable) continue;
                Vector3[] verts = mesh.vertices;
                Matrix4x4 m = s.transform;
                Material[] mats = s.component != null && s.component.TryGetComponent(out MeshRenderer mr) ? mr.sharedMaterials : null;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    int[] tris = mesh.GetTriangles(sub);
                    int key = -1;
                    for (int t = 0; t + 2 < tris.Length; t += 3)
                    {
                        float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue, ylo = float.MaxValue, yhi = float.MinValue;
                        for (int k = 0; k < 3; k++)
                        {
                            Vector3 w = m.MultiplyPoint3x4(verts[tris[t + k]]);
                            Vector2 d = L.ToLocal(new Vector2(w.x, w.z)) - L.Corner;
                            tri[k] = new Vector3(Vector2.Dot(d, L.Up), w.y, Vector2.Dot(d, L.Side));
                            u0 = Mathf.Min(u0, tri[k].x); u1 = Mathf.Max(u1, tri[k].x);
                            v0 = Mathf.Min(v0, tri[k].z); v1 = Mathf.Max(v1, tri[k].z);
                            ylo = Mathf.Min(ylo, w.y); yhi = Mathf.Max(yhi, w.y);
                        }
                        if (u1 < 0f || u0 > nu * cell || v1 < 0f || v0 > nv * cell || yhi < y0 || ylo > y1) continue;
                        float den = (tri[1].z - tri[2].z) * (tri[0].x - tri[2].x) + (tri[2].x - tri[1].x) * (tri[0].z - tri[2].z);
                        if (Mathf.Abs(den) < 1e-6f) continue;   // upright: the colliders show the walls
                        for (int iu = Mathf.Max(0, Mathf.FloorToInt(u0 / cell)); iu < nu && iu * cell <= u1; iu++)
                            for (int iv = Mathf.Max(0, Mathf.FloorToInt(v0 / cell)); iv < nv && iv * cell <= v1; iv++)
                            {
                                float u = (iu + 0.5f) * cell, v = (iv + 0.5f) * cell;
                                float a = ((tri[1].z - tri[2].z) * (u - tri[2].x) + (tri[2].x - tri[1].x) * (v - tri[2].z)) / den;
                                float b = ((tri[2].z - tri[0].z) * (u - tri[2].x) + (tri[0].x - tri[2].x) * (v - tri[2].z)) / den;
                                if (a < -1e-4f || b < -1e-4f || a + b > 1f + 1e-4f) continue;
                                float y = a * tri[0].y + b * tri[1].y + (1f - a - b) * tri[2].y, floor = heightAt(u, v);
                                if (y < floor + 0.25f || y > floor + 2f || y >= low[iu, iv]) continue;
                                if (key < 0)
                                {
                                    string name = (s.component != null ? PathOf(s.component.transform) : mesh.name)
                                        + (mats != null && sub < mats.Length && mats[sub] != null ? " [" + mats[sub].name + "]" : "");
                                    key = keys.IndexOf(name);
                                    if (key < 0 && keys.Count < 26) { keys.Add(name); heights.Add(new Vector2(float.MaxValue, float.MinValue)); key = keys.Count - 1; }
                                    if (key < 0) continue;
                                }
                                low[iu, iv] = y;
                                who[iu, iv] = key;
                                heights[key] = new Vector2(Mathf.Min(heights[key].x, y), Mathf.Max(heights[key].y, y));
                            }
                    }
                }
            }
            for (int k = 0; k < keys.Count; k++)
                if (heights[k].x <= heights[k].y) keys[k] += " at " + F1(heights[k].x) + ".." + F1(heights[k].y) + " m";
            return who;
        }

        // What's round a point of the stairs the NavMesh misses or doesn't reach: the nearest NavMesh, what's underfoot, and the
        // colliders a body standing there would touch (the NavMesh is baked from the render meshes they go with).
        private static string Around(Vector3 at, float floor)
        {
            var sb = new StringBuilder();
            sb.Append(NavMesh.SamplePosition(at, out NavMeshHit near, 2.5f, NavMesh.AllAreas)
                ? "the NavMesh nearest " + F1(new Vector2(near.position.x - at.x, near.position.z - at.z).magnitude) + " m off at " + F1(near.position.y) + " m"
                : "no NavMesh within 2.5 m");
            Vector3 foot = new Vector3(at.x, floor, at.z);
            sb.Append(Physics.Raycast(foot + Vector3.up * 1.0f, Vector3.down, out RaycastHit under, 1.6f, ~0, QueryTriggerInteraction.Ignore)
                ? "; underfoot " + PathOf(under.collider.transform) + " at " + F1(under.point.y) + " m"
                : "; nothing underfoot");
            var touched = new List<string>();
            foreach (Collider c in Physics.OverlapCapsule(foot + Vector3.up * 0.8f, foot + Vector3.up * 1.5f, 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                string what = PathOf(c.transform);
                if (!touched.Contains(what)) touched.Add(what);
            }
            for (int i = 0; i < touched.Count && i < 6; i++) sb.Append(i == 0 ? "; a body there touches " : ", ").Append(touched[i]);
            return sb.ToString();
        }

        private static string F1(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        private static float Length(NavMeshPath path)
        {
            float d = 0f;
            Vector3[] c = path.corners;
            for (int i = 1; i < c.Length; i++) d += Vector3.Distance(c[i - 1], c[i]);
            return d;
        }

        private static string RoomList(InteriorPlan plan)
        {
            var rooms = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (PlanRoom room in plan.Rooms)
            {
                rooms.TryGetValue(room.Role.ToString(), out int k);
                rooms[room.Role.ToString()] = k + 1;
            }
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, int> kv in rooms)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(kv.Key);
                if (kv.Value > 1) sb.Append('×').Append(kv.Value);
            }
            return sb.ToString();
        }

        // Everything furnished on a floor stands on it: no renderer reaches under its floor or over its ceiling, and every
        // fixture's handle is on it (the furnishing counts its heights from the floor: a miss would hang a piece on the
        // ground floor or in the sky).
        private static void CheckHeights(AtlasReport r, string name, InteriorBuild ib, float y0, float y1)
        {
            int low = 0, high = 0, handles = 0;
            string lowAt = null, highAt = null, handleAt = null;
            var renderers = new List<Renderer>();
            if (ib.Root != null) renderers.AddRange(ib.Root.GetComponentsInChildren<Renderer>(true));
            if (ib.Town != null && ib.Town.Overhead != null) renderers.AddRange(ib.Town.Overhead.GetComponentsInChildren<Renderer>(true));
            foreach (Renderer rd in renderers)
            {
                if (rd == null || !rd.enabled) continue;
                Bounds bb = rd.bounds;
                if (bb.size.sqrMagnitude < 1e-8f) continue;
                if (bb.min.y < y0 - 0.35f) { low++; lowAt ??= PathOf(rd.transform) + " at " + F1(bb.min.y) + " m"; }
                if (bb.max.y > y1 + 0.35f) { high++; highAt ??= PathOf(rd.transform) + " up to " + F1(bb.max.y) + " m"; }
            }
            if (ib.Root != null)
                foreach (Interactable it in ib.Root.GetComponentsInChildren<Interactable>(true))
                {
                    float y = it.InteractPoint.y;
                    if (y >= y0 - 0.35f && y <= y1 + 0.1f) continue;
                    handles++;
                    handleAt ??= it.DisplayName + " at " + F1(y) + " m";
                }
            if (low > 0) r.Problems.Add("Levels: " + name + ": " + low + " things reach under its floor (" + lowAt + ")");
            if (high > 0) r.Problems.Add("Levels: " + name + ": " + high + " things reach over its ceiling (" + highAt + ")");
            if (handles > 0) r.Problems.Add("Levels: " + name + ": " + handles + " fixtures are reached off the floor (" + handleAt + ")");
        }

        private static string PathOf(Transform t) => t.parent != null ? t.parent.name + "/" + t.name : t.name;

        // A NavMesh round the building, from the preview's render meshes, as the streamer collects and bakes the map's.
        private static bool BakeAround(BuildingPass.Inspected b, LevelLayout L)
        {
            DropNav();
            if (s_Root == null) return false;
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            if (settings.agentTypeID == -1) settings.agentTypeID = 0;
            settings.minRegionArea = 2f;
            var sources = new List<NavMeshBuildSource>();
            // The pieces' world roots, as the streamer collects them (their overhead roots are never baked).
            foreach (GameObject piece in s_Pieces)
                if (piece != null) KentuckyStreamer.Collect(piece.transform, null, sources);
            Rect f = b.F.Bounds;
            float lo = L.Floors[0], hi = L.Floors[L.Count];
            var bounds = new Bounds(new Vector3(f.center.x, (lo + hi) * 0.5f, f.center.y), new Vector3(f.width + 24f, hi - lo + 6f, f.height + 24f));
            s_NavData = new NavMeshData(0) { name = "Kentucky Atlas" };
            s_Nav = NavMesh.AddNavMeshData(s_NavData);
            return NavMeshBuilder.UpdateNavMeshData(s_NavData, settings, sources, bounds);
        }

        private static void DropNav()
        {
            if (s_Nav.valid) NavMesh.RemoveNavMeshData(s_Nav);
            s_Nav = default;
            if (s_NavData != null) Object.DestroyImmediate(s_NavData);
            s_NavData = null;
        }

        /// <summary>
        /// Pictures of the building the preview was built for, if it has floors above or below the ground one: the game's
        /// isometric view with a survivor stand-in on each floor's landing and the cutaway run for them (the map's, which
        /// lifts what's over their floor, and the partitions' ray cutaway), and one from outside with nothing cut. Written
        /// into <paramref name="dir"/> as <c>levels_&lt;id&gt;_&lt;level&gt;.png</c>; returns a markdown table row for each.
        /// </summary>
        public static List<string> LevelPictures(AtlasReport r, string dir)
        {
            var rows = new List<string>();
            if (s_Root == null || string.IsNullOrEmpty(r.Levels)) return rows;
            LevelLayout L = s_LastLevelsId == r.Id ? s_LastLevels : null;
            if (L == null) return rows;
            var sites = new List<MapCutawaySite>(s_Root.GetComponentsInChildren<MapCutawaySite>(true));
            foreach (MapCutawaySite site in sites) site.Register();
            MapCutaway.ResolveAll();
            var rig = new GameObject("[KentuckyAtlas] pictures");
            var iso = new IsoCutaway();
            try
            {
                // The NavMesh round it, drawn over the pictures (green) a floor at a time, where the dead can walk.
                bool nav = s_LastInspected.HasValue && BakeAround(s_LastInspected.Value, L);
                var numbers = new GameObject("numbers");
                numbers.SetActive(false);
                numbers.transform.SetParent(rig.transform, false);
                IsoCameraFollow f = numbers.AddComponent<IsoCameraFollow>();
                float yaw = f.IsoYaw, pitch = f.IsoPitch;
                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.transform.SetParent(rig.transform, false);
                cam.enabled = false;
                cam.orthographic = true;
                cam.orthographicSize = 9f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.30f, 0.42f, 0.55f);
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 400f;
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.transform.SetParent(rig.transform, false);
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.intensity = 1.1f;
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                RenderSettings.sun = sun;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.48f);
                RenderSettings.fog = false;
                GameObject figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(figure.GetComponent<Collider>());
                figure.transform.SetParent(rig.transform, false);
                figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                var mat = new Material(lit != null ? lit : Shader.Find("Standard"));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.75f, 0.12f, 0.10f));
                figure.GetComponent<Renderer>().sharedMaterial = mat;
                var feetMark = new GameObject("Feet").transform;
                feetMark.SetParent(rig.transform, false);

                Vector2 hall = L.ToSite(L.Hall.center);
                float t = 1000f;
                float reach = 60f * Mathf.Cos(pitch * Mathf.Deg2Rad) + 1f;
                for (int level = L.Lowest - 1; level <= L.Top; level++)
                {
                    bool outside = level < L.Lowest;
                    Vector3 feet = outside ? new Vector3(r.DoorSpot.x, 0f, r.DoorSpot.z) : new Vector3(hall.x, L.FloorOf(level), hall.y);
                    MapCutaway.RestoreAll();
                    if (!outside) MapCutaway.Run(feet, yaw, pitch, reach, true, t += 10f, true);
                    figure.SetActive(!outside);
                    figure.transform.position = feet + Vector3.up * 0.9f;
                    Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
                    Vector3 focus = (outside ? new Vector3(r.Bounds.center.x, 0f, r.Bounds.center.z) : feet) + Vector3.up * 1.2f;
                    cam.orthographicSize = outside ? Mathf.Max(9f, r.Bounds.extents.magnitude * 0.9f) : 9f;
                    cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * 60f, rot);
                    if (!outside)
                    {
                        feetMark.position = feet;
                        Physics.SyncTransforms();
                        iso.Tick(cam.transform.position, cam.transform.forward, true, feetMark, 1.3f, true);
                    }
                    string tag = outside ? "outside" : level < 0 ? "B" + (-level) : level.ToString(CultureInfo.InvariantCulture);
                    string file = "levels_" + r.Id + "_" + tag + ".png";
                    // The floor's own NavMesh, and the flights up from it.
                    float lo = outside ? -0.6f : L.FloorOf(level) - 0.3f, hi = outside ? 1.0f : L.CeilingOf(level) - 0.4f;
                    GameObject overlay = nav ? NavOverlay(rig.transform, lo, hi) : null;
                    try
                    {
                        Shoot(cam, System.IO.Path.Combine(dir, file));
                        int? shown = MapCutaway.ShownLevel(r.Id);
                        rows.Add("| `" + file + "` | " + r.Id + " " + (outside ? "from outside, nothing cut" : LevelName(level) + ", the survivor on its landing")
                            + " | " + (outside ? "" : shown == null ? "not registered" : shown == int.MaxValue ? "nothing" : "everything over level " + shown)
                            + (outside ? "" : "; partitions cut: " + iso.HiddenCount) + " |");
                        if (!outside)
                        {
                            // The stairwell from straight above, close, with the NavMesh on it.
                            Vector2 core = L.CoreSite(L.Length * 0.5f, L.Width * 0.5f);
                            cam.orthographicSize = Mathf.Max(L.Length, L.Width) * 0.7f;
                            cam.transform.SetPositionAndRotation(new Vector3(core.x, L.FloorOf(level) + 40f, core.y), Quaternion.Euler(90f, yaw, 0f));
                            string top = "levels_" + r.Id + "_" + tag + "_stairs.png";
                            Shoot(cam, System.IO.Path.Combine(dir, top));
                            rows.Add("| `" + top + "` | " + r.Id + " " + LevelName(level) + "'s stairwell from above, the NavMesh green | |");
                        }
                    }
                    catch (Exception e)
                    {
                        rows.Add("| `" + file + "` | **failed**: " + e.Message + " | |");
                    }
                    finally
                    {
                        iso.RestoreAll();
                        if (overlay != null) Object.DestroyImmediate(overlay);
                    }
                }
            }
            finally
            {
                MapCutaway.RestoreAll();
                MapCutaway.Clear();
                DropNav();
                Object.DestroyImmediate(rig);
            }
            return rows;
        }

        // The baked NavMesh between heights y0 and y1 as a green sheet a little above it, under `parent`.
        private static GameObject NavOverlay(Transform parent, float y0, float y1)
        {
            NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
            if (tri.vertices == null || tri.vertices.Length == 0) return null;
            var verts = new List<Vector3>(tri.vertices.Length);
            var idx = new List<int>(tri.indices.Length);
            for (int i = 0; i < tri.vertices.Length; i++) verts.Add(tri.vertices[i] + Vector3.up * 0.04f);
            for (int t = 0; t + 2 < tri.indices.Length; t += 3)
            {
                float y = (tri.vertices[tri.indices[t]].y + tri.vertices[tri.indices[t + 1]].y + tri.vertices[tri.indices[t + 2]].y) / 3f;
                if (y < y0 || y > y1) continue;
                idx.Add(tri.indices[t]);
                idx.Add(tri.indices[t + 1]);
                idx.Add(tri.indices[t + 2]);
            }
            if (idx.Count == 0) return null;
            var mesh = new Mesh { name = "[KentuckyAtlas] NavMesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateNormals();
            s_Meshes.Add(mesh);
            var go = new GameObject("NavMesh");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(unlit != null ? unlit : Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.1f, 1f, 0.2f, 1f));
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.1f, 1f, 0.2f, 1f));
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static void Shoot(Camera cam, string path)
        {
            const int w = 1280, h = 800;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
            rt.Create();
            RenderTexture active = RenderTexture.active;
            Texture2D tex = null;
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    cam.enabled = true;
                    var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = rt };
                    if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(cam, request))
                        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam, request);
                    else
                    {
                        cam.targetTexture = rt;
                        cam.Render();
                        cam.targetTexture = null;
                    }
                    cam.enabled = false;
                }
                RenderTexture.active = rt;
                tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                tex.Apply(false);
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                if (tex != null) Object.DestroyImmediate(tex);
            }
        }
        #endregion

        // What stands where a survivor would to go through a door (null: nothing), on whatever floor is there (a porch).
        private static string Blocker(Vector3 p)
        {
            float floor = 0f;
            if (Physics.Raycast(new Vector3(p.x, 2.4f, p.z), Vector3.down, out RaycastHit hit, 3.4f, SolidMask, QueryTriggerInteraction.Ignore)
                && hit.normal.y > 0.7f && hit.point.y < 1.3f)
                floor = Mathf.Max(0f, hit.point.y);
            Collider[] hits = Physics.OverlapCapsule(new Vector3(p.x, floor + 0.5f, p.z), new Vector3(p.x, floor + 1.6f, p.z), 0.28f,
                SolidMask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return null;
            Transform t = hits[0].transform;
            return t.parent != null ? t.parent.name + "/" + t.name : t.name;
        }

        private static string Compass(Vector3 dir)
        {
            float a = Mathf.Repeat(Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 360f);
            return MapValidation.Facings[Mathf.RoundToInt(a / 45f) % 8] + "-facing";
        }

        // Never saved with the scene.
        private static void KeepOutOfScene()
        {
            var roots = new List<GameObject>(s_Extra);
            if (s_Root != null) roots.Add(s_Root);
            foreach (GameObject go in roots)
            {
                if (go == null) continue;
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                    t.gameObject.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }
        }
        #endregion

        #region Looking at it
        /// <summary>Takes the preview down, and what it left in the statics. Does nothing in play mode.</summary>
        public static void Clear()
        {
            if (Application.isPlaying) return;
            try
            {
                if (InteriorStreamer.Active != null) InteriorStreamer.Active.Teardown();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[KentuckyAtlas] taking the interiors down: " + e.Message);
            }
            InteriorStreamer.ToolMode = false;
            DropNav();
            s_LastLevels = null;
            s_LastLevelsId = 0;
            s_LastInspected = null;
            foreach (GameObject p in s_Pieces)
                if (p != null) Shelter.Forget(p);
            foreach (GameObject go in s_Extra)
                if (go != null) Object.DestroyImmediate(go);
            if (s_Root != null) Object.DestroyImmediate(s_Root);
            foreach (Mesh m in s_Meshes)
                if (m != null) Object.DestroyImmediate(m);
            bool had = s_Root != null || s_Pieces.Count > 0;
            s_Root = null;
            s_Pieces.Clear();
            s_Overheads.Clear();
            s_Extra.Clear();
            s_Meshes.Clear();
            s_UpperShown = true;
            if (!had) return;
            Shelter.Clear();
            CeilingVisibility.Clear();
            MapGameplay.ForgetMap();
            StreetLamp.ForgetSwitches();
        }

        /// <summary>Shows or hides the roofs, upper storeys and ceilings, to look into the rooms from above.</summary>
        public static void ShowUpper(bool show)
        {
            s_UpperShown = show;
            foreach (GameObject o in s_Overheads)
                if (o != null) o.SetActive(show);
        }

        /// <summary>Points the Scene view at the building: from the game's isometric angle, straight down on its rooms, or
        /// from outside its front door at eye height.</summary>
        public static void Frame(AtlasReport r, AtlasView view)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null) sv = EditorWindow.GetWindow<SceneView>();
            if (r == null || sv == null) return;
            Vector3 c = r.Bounds.center;
            float size = Mathf.Max(6f, Mathf.Max(r.Bounds.size.x, r.Bounds.size.z));
            switch (view)
            {
                case AtlasView.Plan:
                    sv.LookAt(new Vector3(c.x, 1f, c.z), Quaternion.Euler(90f, 0f, 0f), size * 0.75f);
                    break;
                case AtlasView.Door when r.HasDoorSpot:
                    Quaternion look = Quaternion.LookRotation(-r.DoorOutward) * Quaternion.Euler(8f, 0f, 0f);
                    sv.LookAt(r.DoorSpot + Vector3.up * 1.6f - r.DoorOutward * 2.2f, look, 1.4f);
                    break;
                default:
                    sv.LookAt(c, Quaternion.Euler(38f, 45f, 0f), size * 1.1f);
                    break;
            }
            sv.Repaint();
        }
        #endregion

        #region In a raid
        /// <summary>A Kentucky raid is running with a survivor to send.</summary>
        public static bool InRaid => Application.isPlaying && KentuckyStreamer.Active != null && Actor.LocalPlayer != null;

        /// <summary>Sends the local survivor to a point on the grid (facing <paramref name="yaw"/> degrees from north), for
        /// trying a building's doors, windows and inside in the game; the state is built round them as they land. Returns
        /// why it couldn't, or null.</summary>
        public static string GoThere(double gridX, double gridZ, float yaw)
        {
            if (!Application.isPlaying) return "Enter Play Mode and start a raid in Kentucky first.";
            KentuckyStreamer s = KentuckyStreamer.Active;
            if (s == null) return "No raid in Kentucky is running (the statewide map isn't streaming).";
            Actor me = Actor.LocalPlayer;
            PlayerInteractor pi = me != null ? me.GetComponent<PlayerInteractor>() : null;
            if (pi == null || pi.Context == null) return "There's no survivor to send yet.";
            var at = new Vector3((float)(gridX - s.OriginX), 0.05f, (float)(gridZ - s.OriginZ));
            pi.Context.Teleport(at, yaw);
            Physics.SyncTransforms();
            if (IsoCameraFollow.Instance != null) IsoCameraFollow.Instance.Snap();
            Debug.Log("[KentuckyAtlas] Sent the survivor to " + gridX.ToString("F1", CultureInfo.InvariantCulture) + " E "
                + gridZ.ToString("F1", CultureInfo.InvariantCulture) + " N (world " + at + ")");
            return null;
        }

        /// <summary>A spot just outside a building that was never built in the editor (the middle of its longest wall, a
        /// couple of metres out), on the grid, and the way that faces it.</summary>
        public static void SpotOutside(MapBuilding b, int rx, int rz, int regionSize, out double gridX, out double gridZ, out float yaw)
        {
            var pts = new List<Vector2>(b.pts.Length / 2);
            for (int k = 0; k + 1 < b.pts.Length; k += 2) pts.Add(new Vector2(b.pts[k] * 0.1f, b.pts[k + 1] * 0.1f));
            MapGeometry.MakeAnticlockwise(pts);
            int best = 0;
            float longest = -1f;
            for (int i = 0; i < pts.Count; i++)
            {
                float len = Vector2.Distance(pts[i], pts[(i + 1) % pts.Count]);
                if (len <= longest) continue;
                longest = len;
                best = i;
            }
            Vector2 a = pts[best], c = pts[(best + 1) % pts.Count], d = (c - a).normalized, outward = new Vector2(d.y, -d.x);
            Vector2 spot = (a + c) * 0.5f + outward * 2.5f;
            gridX = (double)rx * regionSize + spot.x;
            gridZ = (double)rz * regionSize + spot.y;
            yaw = Mathf.Atan2(-outward.x, -outward.y) * Mathf.Rad2Deg;
        }
        #endregion
    }

    /// <summary>How <see cref="KentuckyAtlas.Frame"/> shows a building.</summary>
    public enum AtlasView { Iso, Plan, Door }
}
#endif
