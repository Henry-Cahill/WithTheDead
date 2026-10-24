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
            MapBuild build = WorldStore.OpenCurrent(out why);
            if (build == null) return null;
            if (build.RegionSize % ChunkSize != 0)
            {
                why = "build " + build.Id + "'s " + build.RegionSize + " m regions don't divide into " + ChunkSize + " m pieces";
                return null;
            }
            MapPlace place;
            try
            {
                place = build.FindPlace(zone.RealPlace);
            }
            catch (System.Exception e)
            {
                why = "reading build " + build.Id + ": " + e.GetType().Name + ": " + e.Message;
                return null;
            }
            if (place == null)
            {
                why = "build " + build.Id + " has no place called " + zone.RealPlace;
                return null;
            }

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
