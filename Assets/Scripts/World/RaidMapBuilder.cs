using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Builds the Introduction zone's town under <c>world</c> (and the map a raid falls back on when the Kentucky world
    /// store can't be read): a snowbound 1993 Kentucky crossroads town at night, 140 × 140 m, laid out to a fixed plan
    /// (RaidMapBuilder.Plan.cs). Main St runs north–south and Depot Rd east–west through the middle, each road out ending at
    /// an exit (two or three open per raid); Elm St and Oak St are the residential streets off Main St to the west. The
    /// corners of the crossroads hold the county office, Veterans Park, the National Guard post and the clinic; the
    /// gas-station food mart stands up Main St, the depot by the rail line, a quarantine compound behind the Guard post,
    /// and ten houses line the two residential streets, every interior furnished with loot containers, lamps and working
    /// fittings. The town was left in a hurry but tidy: cleared streets and sidewalks, cars parked in drives and lots,
    /// fenced yards, a few street lights still burning. Deterministic for a seed. Everything that blocks movement or sight
    /// has a collider and renders a mesh, so the NavMesh bake that follows carves it; ceilings, roofs and the scenery past
    /// the fence live under separate roots the bake never sees. Keep this signature; the bootstrapper calls it.
    /// </summary>
    public static partial class RaidMapBuilder
    {
        /// <summary>A one-line summary of the last map built (buildings, containers, lights, objects).</summary>
        public static string LastStats { get; private set; } = "";

        private const float Half = 70f;           // the playable half-size: the survivor is kept within it
        private const float GroundHalf = 70.8f;   // the walkable snow ends just past the fence
        private const float EdgeLine = 70.6f;     // the fence line

        /// <summary>Builds the raid map and returns its info.</summary>
        public static RaidMapInfo Build(Transform world, int seed)
        {
            // Prepare map metadata with world bounds and initial player position.
            var info = new RaidMapInfo { HalfSize = Half, PlayerSpawn = new Vector3(0f, 0.6f, 0f) };
            // Reset ceiling visibility tracking for a fresh map build.
            CeilingVisibility.Clear();
            // Instantiate the town builder with the world root, deterministic seed, and map info.
            var town = new RaidTown(world, seed, info);
            // Create a helper to orchestrate the sequential construction steps.
            var builder = new Builder(town);

            // Generate the ground plane covering the playable area.
            builder.Ground();
            // Place exits that lead out of the raid map.
            builder.Exits();
            // Construct building footprints and interior setups.
            builder.Buildings();
            // Lay out streets connecting the buildings.
            builder.Streets();
            // Add street furniture such as lamps and signs.
            builder.StreetFurniture();
            // Build the outer fence edge that bounds the map.
            builder.Edge();
            // Populate surrounding scenery like trees and props.
            builder.Scenery();
            // Spawn the player at the designated start point.
            builder.Spawn();

            // Emit outdoor objects (trees, props) into the scene hierarchy.
            town.EmitOutdoor();
            // Apply the colour palette to generated materials.
            DecorPalette.Apply();

            // Count total objects for build statistics.
            int objects = RaidTown.CountObjects(world) + RaidTown.CountObjects(town.Overhead) + RaidTown.CountObjects(town.Scenery);
            int fixtures = world.GetComponentsInChildren<Interactable>(true).Length;
            int storage = world.GetComponentsInChildren<LootContainer>(true).Length;
            LastStats = "Raid map " + seed + ": " + town.Buildings.Count + " buildings, " + storage + " containers, "
                + fixtures + " fixtures, " + town.LightCount + " lights, " + objects + " objects, " + CeilingVisibility.Count + " ceilings";
            // Log any layout warnings produced during construction.
            if (town.Warnings.Count > 0)
                Debug.LogWarning("[CloneZ] Raid map layout notes (" + town.Warnings.Count + "): " + string.Join(" | ", town.Warnings) + "\n" + LastStats);
#if UNITY_EDITOR || CLONEZ_MAPTEST
            LastTown = town;
#endif
            // Return the map metadata to the caller.
            return info;
        }

#if UNITY_EDITOR || CLONEZ_MAPTEST
        // The town last built: the town harness (Dev/TownShots.cs) reads its buildings, rooms and doors.
        internal static RaidTown LastTown;
#endif

        /// <summary>One town being built: its state, the buildings raised from the plan, and the kerbs dropped for the drives
        /// and lots (the streets are drawn after the lots, round them).</summary>
        private sealed partial class Builder
        {
            private readonly RaidTown _town;
            private readonly System.Random _rng;

            public Builder(RaidTown town)
            {
                _town = town;
                _rng = town.Rng;
            }

            /// <summary>The walkable snow, with the ground collider, ending just past the fence (the scenery root carries the
            /// snowfields beyond, so nothing outside the fence reaches the navmesh).</summary>
            public void Ground()
            {
                var ground = new GameObject("Ground");
                ground.transform.SetParent(_town.World, false);
                var m = new RaidMesh();
                m.Box(new Vector3(0f, -0.5f, 0f), new Vector3(GroundHalf * 2f, 1f, GroundHalf * 2f), DecorMaterials.Snow);
                m.ApplyTo(ground, Vector3.zero, ShadowCastingMode.Off);
                var col = ground.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.5f, 0f);
                col.size = new Vector3(GroundHalf * 2f, 1f, GroundHalf * 2f);
            }

            /// <summary>A flat patch (lot, drive, walk, forecourt) lying on the snow at height <paramref name="y"/>.</summary>
            private void Pave(Rect r, float y, in Surf s) => Paved(_town, r, y, s);

            /// <summary>Keeps later props off <paramref name="r"/>.</summary>
            private void Reserve(Rect r) => _town.Keepout.Add(r);

            /// <summary>True when nothing already placed claims any of <paramref name="r"/>.</summary>
            private bool Free(Rect r) => !_town.Blocked(r);
        }

        #region Shared helpers
        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        private static bool Chance(System.Random rng, double p) => rng.NextDouble() < p;

        private static Rect Around(Vector3 p, float hx, float hz) => Rect.MinMaxRect(p.x - hx, p.z - hz, p.x + hx, p.z + hz);

        private static Rect Grow(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);

        /// <summary>Draws a prop into the outdoor mesh at a world placement, makes it solid unless <paramref name="solid"/> is
        /// false (<paramref name="size"/> is its collider, in the prop's frame), and keeps the spot clear of later props.</summary>
        private static void Prop(RaidTown town, Vector3 pos, float yaw, Vector3 size, bool solid, System.Action<RaidMesh, System.Random> draw)
        {
            RaidMesh m = town.Out.Solid(pos);
            m.SetFrame(pos, yaw);
            draw(m, town.Rng);
            m.ResetFrame();
            if (solid) town.PropCollider(pos + Vector3.up * (size.y * 0.5f), size, yaw);
            float r = Mathf.Max(size.x, size.z) * 0.5f;
            town.Keepout.Add(Around(pos, r, r));
        }

        /// <summary>A flat patch at height <paramref name="y"/>, cut into 12 m tiles so each lands in its own outdoor chunk.</summary>
        private static void Paved(RaidTown town, Rect r, float y, in Surf s)
        {
            for (float x = r.xMin; x < r.xMax - 0.01f; x += 12f)
                for (float z = r.yMin; z < r.yMax - 0.01f; z += 12f)
                {
                    float x1 = Mathf.Min(r.xMax, x + 12f), z1 = Mathf.Min(r.yMax, z + 12f);
                    var c = new Vector3((x + x1) * 0.5f, y, (z + z1) * 0.5f);
                    town.Out.Flat(c).Flat(c, x1 - x, z1 - z, 0f, s);
                }
        }

        private static readonly Color[] CarPaints =
        {
            new Color(0.48f, 0.10f, 0.09f), new Color(0.18f, 0.26f, 0.42f), new Color(0.62f, 0.58f, 0.46f),
            new Color(0.30f, 0.22f, 0.14f), new Color(0.22f, 0.32f, 0.26f), new Color(0.55f, 0.55f, 0.56f),
            new Color(0.80f, 0.80f, 0.78f), new Color(0.14f, 0.14f, 0.16f),
        };

        private static Color Paint(System.Random rng) => CarPaints[rng.Next(CarPaints.Length)];

        private static DecorProps.CarKind FamilyCar(System.Random rng)
        {
            double r = rng.NextDouble();
            return r < 0.45 ? DecorProps.CarKind.Sedan : r < 0.75 ? DecorProps.CarKind.Wagon : DecorProps.CarKind.Pickup;
        }

        /// <summary>A car left where it was parked (with its collider), its glovebox and trunk to search
        /// (<see cref="WreckStorage"/>).</summary>
        private static void ParkedCar(RaidTown town, Vector3 pos, float yaw, DecorProps.CarKind kind, Color paint)
        {
            RaidMesh m = town.Out.Solid(pos);
            m.SetFrame(pos, yaw);
            DecorProps.Car(m, town.Rng, kind, paint, out Vector3 size);
            m.ResetFrame();
            town.PropCollider(pos + Vector3.up * (size.y * 0.5f), size, yaw);
            float r = Mathf.Max(size.x, size.z) * 0.5f;
            town.Keepout.Add(Around(pos, r, r));
            WreckStorage(town, pos, yaw, kind, size);
        }
        #endregion
    }
}
