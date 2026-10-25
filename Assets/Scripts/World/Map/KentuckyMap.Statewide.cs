using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The whole state as one raid map. The zone's real place is where the raid starts: the square around it is built at
    // once, like the fixed map's, and keeps the raid's exits, spawn and the zone's overlays; from there the rest of the
    // state is built around the survivor a piece at a time, as they go (KentuckyStreamer).
    public static partial class KentuckyMap
    {
        /// <summary>The side of a streamed map piece (chunk) in metres; 64 of them make a world-store region.</summary>
        internal const int ChunkSize = 128;

        /// <summary>Builds the start of the statewide map for <paramref name="zone"/> under <paramref name="world"/> and
        /// leaves a <see cref="KentuckyStreamer"/> to build the rest of the state as the survivor travels; or returns
        /// null with the reason in <paramref name="why"/>, having built nothing.</summary>
        private static RaidMapInfo BuildStatewide(Transform world, ZoneDef zone, out string why)
        {
            if (!OpenStatewide(zone, out MapBuild build, out MapPlace place, out why)) return null;

            float half = Mathf.Clamp(zone.RealHalfSize, 60f, 400f);
            var info = new RaidMapInfo { HalfSize = half, PlayerSpawn = new Vector3(0f, 0.6f, 0f) };
            CeilingVisibility.Clear();
            KentuckyStreamer streamer = KentuckyStreamer.Create(world, zone, build, place);
            try
            {
                streamer.BuildStart(half, info);
            }
            catch (System.Exception e)
            {
                why = "building " + place.name + " failed: " + e;
                streamer.Discard();
                return null;
            }
            LastStats = "Kentucky map, the whole state from " + place.name + " (build " + build.Id + "): " + streamer.Summary
                + ", " + info.Exits.Count + " exits. " + build.Attribution;
            Debug.Log("[CloneZ] " + LastStats);
            return info;
        }

        // The world store's current build and the zone's place in it, or false with the reason.
        private static bool OpenStatewide(ZoneDef zone, out MapBuild build, out MapPlace place, out string why)
        {
            place = null;
            build = WorldStore.OpenCurrent(out why);
            if (build == null) return false;
            if (build.RegionSize % ChunkSize != 0)
            {
                why = "build " + build.Id + "'s " + build.RegionSize + " m regions don't divide into " + ChunkSize + " m pieces";
                return false;
            }
            try
            {
                place = build.FindPlace(zone.RealPlace);
            }
            catch (System.Exception e)
            {
                why = "reading build " + build.Id + ": " + e.GetType().Name + ": " + e.Message;
                return false;
            }
            if (place == null)
            {
                why = "build " + build.Id + " has no place called " + zone.RealPlace;
                return false;
            }
            return true;
        }

        /// <summary>Begins building the map for <paramref name="zone"/> under <paramref name="world"/> a step at a time, for
        /// a loading screen to show going up (the hub's lobby in the town: <see cref="KettleLobby"/>). A statewide zone's
        /// start square is built over the job's steps; any other map is built at once, here.</summary>
        public static MapBuildJob BeginBuild(Transform world, ZoneDef zone)
        {
            var job = new MapBuildJob();
            if (!zone.RealStatewide)
            {
                job.Finished(Build(world, zone, out string why), why);
                return job;
            }
            MapGameplay.ForgetMap();
            StreetLamp.ForgetSwitches();
            if (!OpenStatewide(zone, out MapBuild build, out MapPlace place, out string fail))
            {
                job.Finished(null, fail);
                return job;
            }
            float half = Mathf.Clamp(zone.RealHalfSize, 60f, 400f);
            var info = new RaidMapInfo { HalfSize = half, PlayerSpawn = new Vector3(0f, 0.6f, 0f) };
            CeilingVisibility.Clear();
            KentuckyStreamer streamer = KentuckyStreamer.Create(world, zone, build, place);
            job.Start(streamer, streamer.BuildStartSteps(half, info), info, build, place);
            return job;
        }

        /// <summary>
        /// A raid map being built a step at a time (see <see cref="BeginBuild"/>). Call <see cref="Step"/> until it returns
        /// false: <see cref="Info"/> is then the map, or null with the reason in <see cref="Why"/>, having built nothing.
        /// </summary>
        public sealed class MapBuildJob
        {
            /// <summary>The map once it's built; null while building, or when it failed.</summary>
            public RaidMapInfo Info { get; private set; }
            /// <summary>Why the build failed ("" while it hasn't).</summary>
            public string Why { get; private set; } = "";
            /// <summary>How far through the build it is, 0 to 1.</summary>
            public float Progress { get; private set; }
            /// <summary>What's going up now, for a loading screen ("Plowing the roads").</summary>
            public string Stage { get; private set; } = "Reading the map";
            public bool Done { get; private set; }

            private KentuckyStreamer _streamer;
            private IEnumerator<float> _steps;
            private RaidMapInfo _pending;
            private MapBuild _build;
            private MapPlace _place;

            internal void Start(KentuckyStreamer streamer, IEnumerator<float> steps, RaidMapInfo info, MapBuild build, MapPlace place)
            {
                _streamer = streamer;
                _steps = steps;
                _pending = info;
                _build = build;
                _place = place;
            }

            internal void Finished(RaidMapInfo info, string why)
            {
                Info = info;
                Why = why ?? "";
                Progress = 1f;
                Done = true;
            }

            /// <summary>Builds the next step; false once the map is built (or failed).</summary>
            public bool Step()
            {
                if (Done) return false;
                try
                {
                    if (_steps.MoveNext())
                    {
                        Progress = Mathf.Clamp01(_steps.Current);
                        Stage = StageOf(_streamer.StartStage);
                        return true;
                    }
                }
                catch (System.Exception e)
                {
                    _streamer.Discard();
                    Finished(null, "building " + _place.name + " failed: " + e);
                    return false;
                }
                LastStats = "Kentucky map, the whole state from " + _place.name + " (build " + _build.Id + "): " + _streamer.Summary
                    + ", " + _pending.Exits.Count + " exits. " + _build.Attribution;
                Debug.Log("[CloneZ] " + LastStats);
                Finished(_pending, "");
                return false;
            }
        }

        /// <summary>A pass's step (the name it yields) as a stage of the town going up, for a loading screen.</summary>
        public static string StageOf(string step)
        {
            if (string.IsNullOrEmpty(step)) return "Reading the map";
            if (step.StartsWith("interior")) return "Furnishing the houses";
            switch (step)
            {
                case "ground": case "ground mask": case "snow tone": case "fields": case "lots": case "cemeteries": case "water":
                case "grounds": case "work yards": case "data":
                    return "Laying the snow";
                case "road network": case "streets": case "sidewalks": case "junctions": case "road markings": case "bridges":
                case "paths": case "railways": case "scenery roads": case "berms and driveways": case "driveway plan":
                    return "Plowing the roads";
                case "buildings": case "cutaway":
                    return "Raising the buildings";
                case "street lights": case "utility lines": case "utility poles": case "power lines": case "wires":
                case "service drops": case "signs": case "street furniture": case "fences and walls":
                    return "Stringing the power lines";
                case "mapped trees": case "street trees": case "yard trees": case "grounds trees": case "creek trees": case "woods":
                case "nature plan": case "tree rows": case "fence rows":
                    return "Planting the trees";
                case "driveway cars": case "kerbside cars": case "parking lot cars": case "wrecks": case "buses and cruisers":
                    return "Parking the cars";
                case "plough piles": case "shovelled snow": case "fence drifts": case "wall drifts": case "park litter":
                    return "Drifting the snow";
                default:
                    return "Finishing the town";
            }
        }

        /// <summary>The exits where the start square's streets leave it, and the survivor's spot on the sidewalk nearest
        /// its middle (<paramref name="free"/> says whether a spot, in world metres, is clear).</summary>
        internal static void PlanStart(Area start, float half, RaidMapInfo info, System.Func<Vector2, bool> free)
        {
            MapGameplay.DefineExits(start, half, info);
            MapGameplay.PickSpawn(start, half, free, out Vector2 spot, out float yaw);
            info.PlayerSpawn = To3(spot, 0.6f);
            info.PlayerSpawnYaw = yaw;
        }

        /// <summary>One streamed piece being built: what it's built from and where, and (while it lasts) the site.</summary>
        internal sealed class ChunkJob
        {
            public MapBuild Build;
            public MapRegionIndex Region;
            public string Place;
            public int Cx, Cz, Seed;
            /// <summary>The piece's baked root (under the world) and overhead root; both sit at the origin while it's
            /// built and are moved to <see cref="Offset"/> afterwards.</summary>
            public Transform Root, Overhead;
            public Vector3 Offset;
            /// <summary>How much of the piece to build.</summary>
            public Detail Detail;
            /// <summary>The piece's data: read off the main thread beforehand (see <see cref="ReadArea"/>), else by the
            /// first step.</summary>
            public Area Area;
            /// <summary>Bake the meshes on this thread, at once, rather than on a worker over the next frames (a piece
            /// built all in one go: the start square).</summary>
            public bool Synchronous;
            public Site Site;
            public RaidTown Town;
            /// <summary>The meshes being packed on a worker (between the "objects" and "meshes" steps).</summary>
            public MeshBake Bake;

            /// <summary>The piece's data as its first step would read it (safe on any thread).</summary>
            public Area ReadArea() => new Area(Place, Build.Id, Build.Attribution, Region, Cx, Cz);

            /// <summary>Lets go of what a piece dropped half-built still holds (the mesh buffers being packed).</summary>
            public void Abandon()
            {
                Bake?.Dispose();
                Bake = null;
            }
        }

        /// <summary>What <see cref="ChunkSteps"/> yields while it waits on a worker thread: the streamer leaves the piece
        /// for the next frame rather than asking again at once.</summary>
        internal const string WaitStep = "(wait)";

        /// <summary>Builds one streamed piece a step at a time: its square of the region's data (unless it was read
        /// beforehand), then every pass the fixed map makes, then its colliders and meshes (packed on a worker thread).
        /// Each step is a few milliseconds at most, as far as the passes' own steps allow.</summary>
        internal static IEnumerable<string> ChunkSteps(ChunkJob job)
        {
            if (job.Area == null)
            {
                job.Area = job.ReadArea();
                yield return "data";
            }
            job.Town = new RaidTown(job.Root, job.Seed, new RaidMapInfo { HalfSize = job.Area.Half }, job.Overhead);
            job.Site = new Site(job.Build, job.Town, job.Area, true, job.Offset, job.Detail);
            foreach (string step in Passes(job.Site)) yield return step;
            if (job.Town.PendingColliders > 0)
            {
                while (!job.Town.MakeColliders(Site.StepSliceMs)) yield return "colliders";
                yield return "colliders";
            }
            // Every batch's object now; their meshes packed off the main thread and filled in a frame or so later.
            MeshBake bake = job.Town.Bake;
            job.Site.Overhead.EmitLater("Overhead", job.Town.Overhead, true, bake);
            job.Site.Roofs.EmitLater("Roofs", job.Town.Overhead, false, bake);
            job.Town.Out.EmitLater("Outdoor", bake);
            job.Town.Far.EmitLater("Scenery", bake);
            bake = job.Town.TakeBake();
            if (bake != null)
            {
                job.Bake = bake;
                if (job.Synchronous)
                {
                    bake.Allocate();
                    bake.Pack();
                }
                else
                {
                    bake.PackAsync();
                    yield return "objects";
                    while (!bake.Packed) yield return WaitStep;
                }
                job.Bake = null;
                bake.Apply();
            }
            DecorPalette.Apply();
            yield return "meshes";
        }

        /// <summary>Plain walkable snow over a piece whose build failed, so the state has no hole in it.</summary>
        internal static void BareGround(Transform root, float half)
        {
            var ground = new GameObject("Ground");
            ground.transform.SetParent(root, false);
            var m = new RaidMesh();
            m.Box(new Vector3(0f, -0.5f, 0f), new Vector3(half * 2f + 1.6f, 1f, half * 2f + 1.6f), DecorMaterials.Snow);
            m.ApplyTo(ground, Vector3.zero, ShadowCastingMode.Off);
            var col = ground.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.5f, 0f);
            col.size = new Vector3(half * 2f + 1.6f, 1f, half * 2f + 1.6f);
        }
    }
}
