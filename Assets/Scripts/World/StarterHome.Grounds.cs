using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    internal static partial class StarterHome
    {
        #region Street
        // The street past the home runs east-west, shifted south of the old centre line so the house
        // fronts a sidewalk instead of standing on the kerb.
        private const float RoadCentre = -1.6f;
        private const float RoadHalf = 3f;
        private const float KerbWidth = 0.22f;
        private const float SidewalkEdge = 2.9f;   // the house's front wall

        private static readonly Color Kerb = new Color(0.66f, 0.66f, 0.63f);
        private static readonly Color LineYellow = new Color(0.80f, 0.66f, 0.20f);
        private static readonly Color LineWhite = new Color(0.80f, 0.80f, 0.77f);

        /// <summary>The street past the front of the home: sun-faded asphalt with a dashed yellow centre line
        /// and white edge lines, kerbs and concrete sidewalks both sides, a storm drain and a manhole cover.
        /// Flat and collider-less, and kept out of every navmesh bake, so it never affects pathing.</summary>
        public static void BuildStreet(Transform world)
        {
            float half = CloneZConfig.ArenaHalfSize;   // span the arena, inside the perimeter walls
            var rng = new System.Random(4242);
            var m = new RaidMesh();
            float north = RoadCentre + RoadHalf, south = RoadCentre - RoadHalf;

            Paved(m, Rect.MinMaxRect(-half, south, half, north), 0.012f, DecorMaterials.DryAsphalt);
            for (float x = -half + 1.5f; x < half - 1.5f; x += 3f)
            {
                if (rng.NextDouble() < 0.08) continue;   // worn away
                m.Flat(new Vector3(x, 0.018f, RoadCentre), 1.5f, 0.14f, 0f, DecorPalette.Matte(LineYellow * (0.85f + (float)rng.NextDouble() * 0.15f)));
            }
            m.Flat(new Vector3(0f, 0.017f, north - 0.25f), half * 2f, 0.12f, 0f, DecorPalette.Matte(LineWhite));
            m.Flat(new Vector3(0f, 0.017f, south + 0.25f), half * 2f, 0.12f, 0f, DecorPalette.Matte(LineWhite));

            // Kerbs, then sidewalks: the north one runs up to the house front, the south one 1.5 m wide.
            Surf kerb = DecorPalette.Matte(Kerb);
            m.Flat(new Vector3(0f, 0.05f, north + KerbWidth * 0.5f), half * 2f, KerbWidth, 0f, kerb);
            m.Flat(new Vector3(0f, 0.05f, south - KerbWidth * 0.5f), half * 2f, KerbWidth, 0f, kerb);
            Paved(m, Rect.MinMaxRect(-half, north + KerbWidth, half, SidewalkEdge), 0.03f, DecorMaterials.Concrete);
            Paved(m, Rect.MinMaxRect(-half, south - KerbWidth - 1.5f, half, south - KerbWidth), 0.03f, DecorMaterials.Concrete);

            // A storm drain in the gutter by the house and a manhole cover in the lane.
            Surf iron = DecorPalette.Metal(new Color(0.10f, 0.10f, 0.11f));
            m.Flat(new Vector3(9.2f, 0.016f, north - 0.25f), 0.9f, 0.42f, 0f, iron);
            for (int i = 0; i < 6; i++)
                m.Flat(new Vector3(8.85f + i * 0.14f, 0.018f, north - 0.25f), 0.05f, 0.34f, 0f, DecorPalette.Matte(new Color(0.03f, 0.03f, 0.03f)));
            m.Cylinder(new Vector3(-6.5f, 0.012f, RoadCentre - 1.4f), 0.36f, 0.008f, 16, iron);
            m.Cylinder(new Vector3(-6.5f, 0.02f, RoadCentre - 1.4f), 0.3f, 0.002f, 16, DecorPalette.Metal(new Color(0.16f, 0.16f, 0.15f)));

            Cosmetic(m.Emit("Street", world, Vector3.zero, ShadowCastingMode.Off));
            DecorPalette.Apply();
        }

        // Under the world root but never part of the navmesh — not the first bake, nor the rebuilds that
        // follow when something baked into it is dismantled.
        private static void Cosmetic(GameObject go)
        {
            if (go != null) go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }

        /// <summary>A flat paved patch in 8 m squares (planar UVs keep the texture continuous across them).</summary>
        private static void Paved(RaidMesh m, Rect r, float y, in Surf s)
        {
            for (float x = r.xMin; x < r.xMax - 0.01f; x += 8f)
                for (float z = r.yMin; z < r.yMax - 0.01f; z += 8f)
                {
                    float x1 = Mathf.Min(r.xMax, x + 8f), z1 = Mathf.Min(r.yMax, z + 8f);
                    m.Flat(new Vector3((x + x1) * 0.5f, y, (z + z1) * 0.5f), x1 - x, z1 - z, 0f, s);
                }
        }
        #endregion

        #region Yard
        private static readonly Rect Lot = Rect.MinMaxRect(-7.5f, SidewalkEdge, 10.5f, 13f);
        private static readonly Color Bark = new Color(0.30f, 0.19f, 0.11f);
        private static readonly Color Timber = new Color(0.36f, 0.27f, 0.17f);
        private static readonly Color[] Foliage =
        {
            new Color(0.14f, 0.28f, 0.12f), new Color(0.19f, 0.33f, 0.14f), new Color(0.12f, 0.24f, 0.13f),
        };
        private static readonly Color[] Blooms =
        {
            new Color(0.80f, 0.16f, 0.14f), new Color(0.94f, 0.74f, 0.18f), new Color(0.88f, 0.46f, 0.62f), new Color(0.90f, 0.90f, 0.86f),
        };

        /// <summary>The home's yard, all cosmetic and kept out of every navmesh bake: a lawn with ragged edges,
        /// a timber-edged vegetable bed round the garden plots, mulched flower beds with shrubs along the
        /// house, stepping stones out to the garden, a concrete stoop and welcome mat at the front door,
        /// a mailbox at the kerb and a hose reel by the rain barrel.</summary>
        public static void DressGrounds(Transform world)
        {
            var rng = new System.Random(5150);
            var flat = new RaidMesh();
            var solid = new RaidMesh();

            Paved(flat, Lot, 0.006f, DecorMaterials.Grass);
            for (int i = 0; i < 46; i++)
            {
                // Tufts spilling past the lawn's back and side edges, so it doesn't end on a ruler line.
                float t = (float)rng.NextDouble();
                Vector3 p;
                switch (i % 3)
                {
                    case 0: p = new Vector3(Mathf.Lerp(Lot.xMin, Lot.xMax, t), 0.005f, Lot.yMax); break;
                    case 1: p = new Vector3(Lot.xMin, 0.005f, Mathf.Lerp(Lot.yMin + 0.6f, Lot.yMax, t)); break;
                    default: p = new Vector3(Lot.xMax, 0.005f, Mathf.Lerp(Lot.yMin + 0.6f, Lot.yMax, t)); break;
                }
                flat.FlatBlob(p, 0.5f + (float)rng.NextDouble() * 0.9f, 0.4f + (float)rng.NextDouble() * 0.7f, (float)rng.NextDouble() * 360f, 0.45f, rng, DecorMaterials.Grass);
            }

            // The vegetable bed round the garden plots, edged with timber.
            Rect bed = Rect.MinMaxRect(-5.9f, 3.3f, -1.7f, 7.3f);
            Paved(flat, bed, 0.01f, DecorMaterials.Mulch(Soil));
            Edging(solid, bed);

            // Flower beds along the west, east and back walls.
            Rect west = Rect.MinMaxRect(0.2f, 3.15f, 0.9f, 8.85f), east = Rect.MinMaxRect(7.1f, 3.15f, 7.8f, 8.85f), back = Rect.MinMaxRect(1.15f, 9.1f, 6.85f, 9.75f);
            Paved(flat, west, 0.012f, DecorMaterials.Mulch(Bark));
            Paved(flat, east, 0.012f, DecorMaterials.Mulch(Bark));
            Paved(flat, back, 0.012f, DecorMaterials.Mulch(Bark));
            foreach (float z in new[] { 3.7f, 4.6f, 7.6f, 8.4f }) Shrub(solid, rng, new Vector3(0.55f, 0f, z));
            foreach (float z in new[] { 3.7f, 4.7f, 5.8f, 7.0f, 8.2f }) Shrub(solid, rng, new Vector3(7.45f, 0f, z));
            foreach (float x in new[] { 1.8f, 3.0f, 5.1f, 6.3f }) Shrub(solid, rng, new Vector3(x, 0f, 9.42f));

            // Stepping stones from the sidewalk round to the garden.
            Vector3[] stones = { new Vector3(-0.15f, 0f, 3.35f), new Vector3(-0.55f, 0f, 3.95f), new Vector3(-0.95f, 0f, 4.55f), new Vector3(-1.3f, 0f, 5.2f) };
            foreach (Vector3 s in stones)
                flat.FlatBlob(s + Vector3.up * 0.014f, 0.26f, 0.22f, (float)rng.NextDouble() * 360f, 0.15f, rng, DecorPalette.Matte(new Color(0.55f, 0.54f, 0.50f) * (0.9f + (float)rng.NextDouble() * 0.15f)));

            // The front stoop and welcome mat.
            solid.Box(new Vector3(4.0f, 0.035f, 2.6f), new Vector3(1.9f, 0.07f, 0.6f), DecorMaterials.Concrete);
            Surf welcome = DecorMaterials.Sign("WELCOME", new Color(0.46f, 0.34f, 0.18f), new Color(0.16f, 0.11f, 0.07f), out float aspect);
            flat.Flat(new Vector3(4.0f, 0.073f, 2.62f), 0.86f, 0.46f, 0f, DecorPalette.Matte(new Color(0.16f, 0.13f, 0.10f)));
            flat.FlatUV(new Vector3(4.0f, 0.075f, 2.62f), 0.78f, 0.78f / aspect, 0f, welcome, new Rect(0f, 0f, 1f, 1f));

            Mailbox(solid, new Vector3(6.1f, 0.03f, 1.95f));
            HoseReel(solid, new Vector3(0.55f, 0.012f, 6.85f));

            Cosmetic(flat.Emit("YardGround", world, Vector3.zero, ShadowCastingMode.Off));
            Cosmetic(solid.Emit("Yard", world, Vector3.zero, ShadowCastingMode.On));
            DecorPalette.Apply();
        }

        // Low timber boards round a bed.
        private static void Edging(RaidMesh m, Rect r)
        {
            Surf wood = DecorPalette.Matte(Timber);
            m.Box(new Vector3(r.center.x, 0.04f, r.yMin), new Vector3(r.width + 0.1f, 0.08f, 0.06f), wood);
            m.Box(new Vector3(r.center.x, 0.04f, r.yMax), new Vector3(r.width + 0.1f, 0.08f, 0.06f), wood);
            m.Box(new Vector3(r.xMin, 0.04f, r.center.y), new Vector3(0.06f, 0.08f, r.height), wood);
            m.Box(new Vector3(r.xMax, 0.04f, r.center.y), new Vector3(0.06f, 0.08f, r.height), wood);
        }

        // A rounded shrub of a few overlapping leaf clumps, now and then in flower.
        private static void Shrub(RaidMesh m, System.Random rng, Vector3 at)
        {
            float s = 0.75f + (float)rng.NextDouble() * 0.4f;
            int clumps = 3 + rng.Next(2);
            for (int i = 0; i < clumps; i++)
            {
                var o = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.3f * s, 0f, ((float)rng.NextDouble() - 0.5f) * 0.3f * s);
                float h = (0.38f + (float)rng.NextDouble() * 0.2f) * s;
                Color c = Foliage[rng.Next(Foliage.Length)] * (0.9f + (float)rng.NextDouble() * 0.2f);
                m.Ellipsoid(at + o + Vector3.up * h * 0.5f, new Vector3(0.5f * s, h, 0.5f * s), DecorPalette.Matte(c), 8, 5);
            }
            if (rng.NextDouble() < 0.5)
            {
                Color bloom = Blooms[rng.Next(Blooms.Length)];
                for (int i = 0; i < 6; i++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    var p = at + new Vector3(Mathf.Sin(a) * 0.2f * s, (0.3f + (float)rng.NextDouble() * 0.2f) * s, Mathf.Cos(a) * 0.2f * s);
                    m.Ellipsoid(p, new Vector3(0.07f, 0.06f, 0.07f), DecorPalette.Matte(bloom), 6, 3);
                }
            }
        }

        // A rural mailbox on a cedar post at the kerb, its door to the street and the red flag down.
        private static void Mailbox(RaidMesh m, Vector3 at)
        {
            m.Box(at + new Vector3(0f, 0.55f, 0f), new Vector3(0.09f, 1.1f, 0.09f), DecorPalette.Matte(new Color(0.40f, 0.28f, 0.17f)));
            m.Box(at + new Vector3(0f, 1.11f, 0f), new Vector3(0.18f, 0.03f, 0.34f), DecorPalette.Matte(new Color(0.40f, 0.28f, 0.17f)));
            Surf box = DecorPalette.Metal(new Color(0.18f, 0.20f, 0.24f));
            m.Box(at + new Vector3(0f, 1.2f, 0f), new Vector3(0.22f, 0.16f, 0.48f), box);
            m.CylinderX(at + new Vector3(0f, 1.28f, 0f), 0.11f, 0.48f, 10, box, 90f);
            m.Box(at + new Vector3(0f, 1.22f, -0.245f), new Vector3(0.2f, 0.2f, 0.012f), DecorPalette.Metal(new Color(0.24f, 0.26f, 0.30f)));
            m.Box(at + new Vector3(0.125f, 1.2f, 0.06f), new Vector3(0.015f, 0.05f, 0.2f), DecorPalette.Matte(new Color(0.72f, 0.12f, 0.10f)));
        }

        // A green garden hose coiled on a wall reel.
        private static void HoseReel(RaidMesh m, Vector3 at)
        {
            Surf frame = DecorPalette.Metal(new Color(0.22f, 0.24f, 0.22f));
            m.Box(at + new Vector3(-0.2f, 0.25f, 0f), new Vector3(0.04f, 0.5f, 0.04f), frame);
            m.Box(at + new Vector3(0.2f, 0.25f, 0f), new Vector3(0.04f, 0.5f, 0.04f), frame);
            m.CylinderX(at + new Vector3(0f, 0.36f, 0f), 0.04f, 0.44f, 8, frame);
            m.CylinderX(at + new Vector3(0f, 0.36f, 0f), 0.2f, 0.3f, 14, DecorPalette.Gloss(new Color(0.16f, 0.42f, 0.18f)));
            m.Rod(at + new Vector3(-0.1f, 0.2f, 0.1f), at + new Vector3(-0.6f, 0.01f, 0.45f), 0.035f, DecorPalette.Gloss(new Color(0.16f, 0.42f, 0.18f)), 6);
        }
        #endregion
    }
}
