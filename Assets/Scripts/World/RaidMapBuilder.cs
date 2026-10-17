using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Builds the extraction raid map under <c>world</c>: a snowbound, run-down 1993 Kentucky town at
    /// night (140 × 140 m). Two crossing streets with sidewalks, kerbs, snow banks and tyre tracks; a
    /// National Guard post, a county office, a gas-station food mart, a depot with a repair garage, a
    /// clinic, five houses and a military quarantine tent inside a toxic-gas cloud — every interior
    /// furnished to match the reference photos, with loot containers, room and desk lamps, vines and
    /// litter. Outside: wrecked cars, dead trees, dumpsters, street lights, snow piles, a checkpoint and
    /// a road block, a fenced map edge and four exits (two or three open per raid). Deterministic for a
    /// seed. Everything that blocks movement or sight has a collider and renders a mesh, so the NavMesh
    /// bake that follows carves it; ceilings, roofs and the scenery past the fence live under separate
    /// roots that the bake never sees. Keep this signature; the bootstrapper calls it.
    /// </summary>
    public static partial class RaidMapBuilder
    {
        /// <summary>A one-line summary of the last map built (buildings, containers, lights, objects).</summary>
        public static string LastStats { get; private set; } = "";

        private const float Half = 70f;
        private const float GroundHalf = 70.8f;
        private const float EdgeLine = 70.6f;

        public static RaidMapInfo Build(Transform world, int seed)
        {
            var info = new RaidMapInfo { HalfSize = Half, PlayerSpawn = new Vector3(0f, 0.6f, 0f) };
            CeilingVisibility.Clear();
            var town = new RaidTown(world, seed, info);

            BuildGround(town);
            BuildStreets(town);
            DefineExits(town);
            BuildTown(town);
            BuildOutdoorProps(town);
            BuildEdge(town);
            BuildScenery(town);
            PickSpawn(town);

            town.EmitOutdoor();
            DecorPalette.Apply();

            int objects = RaidTown.CountObjects(world) + RaidTown.CountObjects(town.Overhead) + RaidTown.CountObjects(town.Scenery);
            LastStats = "Raid map " + seed + ": " + town.Buildings.Count + " buildings, " + town.ContainerCount + " containers, "
                + town.LightCount + " lights, " + objects + " objects, " + CeilingVisibility.Count + " ceilings";
            if (town.Warnings.Count > 0)
                Debug.LogWarning("[CloneZ] Raid map layout notes (" + town.Warnings.Count + "): " + string.Join(" | ", town.Warnings) + "\n" + LastStats);
#if CLONEZ_MAPTEST
            LastTown = town;
#endif
            return info;
        }

#if CLONEZ_MAPTEST
        // Offline layout tests only (never defined in Unity).
        internal static RaidTown LastTown;
#endif

        // The walkable snow (with the ground collider) ends just past the fence line; the scenery root
        // carries the snowfields beyond, so nothing outside the fence is ever baked into the navmesh.
        private static void BuildGround(RaidTown town)
        {
            var ground = new GameObject("Ground");
            ground.transform.SetParent(town.World, false);
            var m = new RaidMesh();
            m.Box(new Vector3(0f, -0.5f, 0f), new Vector3(GroundHalf * 2f, 1f, GroundHalf * 2f), DecorMaterials.Snow);
            m.ApplyTo(ground, Vector3.zero, ShadowCastingMode.Off);
            var col = ground.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.5f, 0f);
            col.size = new Vector3(GroundHalf * 2f, 1f, GroundHalf * 2f);

            // Soft variation: wind-scoured grey patches and fresh white drifts lying on the snow.
            System.Random rng = town.Rng;
            for (int i = 0; i < 140; i++)
            {
                var p = new Vector3(R(rng, -68f, 68f), 0.004f, R(rng, -68f, 68f));
                bool grey = rng.NextDouble() < 0.5;
                Color c = grey ? new Color(0.66f, 0.70f, 0.77f) : new Color(0.88f, 0.90f, 0.95f);
                town.Out.Flat(p).FlatBlob(p, R(rng, 1.5f, 5f), R(rng, 1f, 3.5f), R(rng, 0f, 360f), 0.35f, rng, DecorPalette.Matte(c));
            }
        }

        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        private static Rect Around(Vector3 p, float hx, float hz) => Rect.MinMaxRect(p.x - hx, p.z - hz, p.x + hx, p.z + hz);

        private static Rect Grow(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);
    }
}
