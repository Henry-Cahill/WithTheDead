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
        /// a mailbox at the kerb and a hose reel with an outside tap by the rain barrel. The mat, the mailbox
        /// and the tap are working fixtures (<see cref="DoormatFixture"/>, <see cref="MailboxFixture"/>,
        /// <see cref="HoseSpigot"/>).</summary>
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
            // subtle expansion joint – a thin dark line along the centre
            solid.Ellipsoid(
                new Vector3(4.0f, 0.069f, 2.6f), // centre (just below top surface)
                new Vector3(1.78f, 0.0015f, 0.006f), // long, thin, slightly raised
                DecorPalette.Matte(new Color(0.45f,0.44f,0.43f)),
                4, 2);
            // weather‑worn concrete patch, slightly offset
            solid.Ellipsoid(
                new Vector3(4.0f - 0.55f + (float)rng.NextDouble() * 0.2f,
                    0.069f,
                    2.6f - 0.20f + (float)rng.NextDouble() * 0.15f),
                new Vector3(0.25f, 0.004f, 0.12f),
                DecorPalette.Matte(new Color(0.38f,0.37f,0.36f)),
                4, 2);
            // Add thin ice patches on the north and south sidewalks.
            IcePatches(solid, Rect.MinMaxRect(-CloneZConfig.ArenaHalfSize, RoadCentre + RoadHalf + KerbWidth, CloneZConfig.ArenaHalfSize, SidewalkEdge), rng);
            IcePatches(solid, Rect.MinMaxRect(-CloneZConfig.ArenaHalfSize, RoadCentre - RoadHalf - KerbWidth - 1.5f, CloneZConfig.ArenaHalfSize, RoadCentre - RoadHalf - KerbWidth), rng);
            // Add a soft snow‑drift clump near the front stoop.
            SnowDrift(solid, new Vector3(4.0f, 0.07f, 2.6f), rng);
            // three tiny gravel stones near the front edge
            for (int i = 0; i < 3; i++)
            {
                float ox = (float)rng.NextDouble() * 0.4f - 0.2f; // ±0.2 m around centre X
                float oz = (float)rng.NextDouble() * 0.2f - 0.1f; // ±0.1 m around centre Z
                solid.Cylinder(
                    new Vector3(4.0f + ox, 0.038f, 2.6f + oz),
                    0.004f, 0.008f, 6,
                    DecorPalette.Matte(new Color(0.55f,0.55f,0.53f)),
                    Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
            }
            // metal plate base (loose fastener) on left‑front corner
            solid.Box(
                new Vector3(4.0f - 0.92f, 0.069f, 2.6f - 0.28f),
                new Vector3(0.025f, 0.001f, 0.012f),
                DecorPalette.Metal(new Color(0.60f,0.60f,0.55f)));
            // nail head
            solid.Cylinder(
                new Vector3(4.0f - 0.92f, 0.070f, 2.6f - 0.28f),
                0.0015f, 0.006f, 8,
                DecorPalette.Metal(new Color(0.60f,0.60f,0.55f)),
                Quaternion.Euler(90f,0f,0f));
            // lone weed sprout
            solid.Ellipsoid(
                new Vector3(4.0f + 0.68f + (float)rng.NextDouble() * 0.06f,
                    0.071f,
                    2.6f - 0.12f + (float)rng.NextDouble() * 0.04f),
                new Vector3(0.02f, 0.03f, 0.02f),
                DecorPalette.Gloss(new Color(0.18f,0.30f,0.13f)),
                4, 2);
            Doormat(world, solid, new Vector3(4.0f, 0.07f, 2.62f));

            Mailbox(world, solid, new Vector3(6.1f, 0.03f, 1.95f));
            HoseReel(world, solid, new Vector3(0.55f, 0.012f, 6.85f));

            Cosmetic(flat.Emit("YardGround", world, Vector3.zero, ShadowCastingMode.Off));
            Cosmetic(solid.Emit("Yard", world, Vector3.zero, ShadowCastingMode.On));
            DecorPalette.Apply();
        }

        // Low timber boards round a bed.
        private static void Edging(RaidMesh m, Rect r)
        {
            Surf wood = DecorPalette.Matte(Timber);
            m.Box(new Vector3(r.center.x, 0.04f, r.yMin), new Vector3(r.width + 0.1f, 0.08f, 0.06f), wood);
                m.Cylinder(new Vector3(r.center.x - (r.width + 0.1f) * 0.5f + 0.02f, 0.081f, r.yMin + 0.03f - 0.02f), 0.004f, 0.012f, 8, wood);
                m.Cylinder(new Vector3(r.center.x + (r.width + 0.1f) * 0.5f - 0.02f, 0.081f, r.yMin + 0.03f - 0.02f), 0.004f, 0.012f, 8, wood);
            m.Box(new Vector3(r.center.x, 0.04f, r.yMax), new Vector3(r.width + 0.1f, 0.08f, 0.06f), wood);
                m.Cylinder(new Vector3(r.center.x - (r.width + 0.1f) * 0.5f + 0.02f, 0.081f, r.yMax + 0.03f - 0.02f), 0.004f, 0.012f, 8, wood);
                m.Cylinder(new Vector3(r.center.x + (r.width + 0.1f) * 0.5f - 0.02f, 0.081f, r.yMax + 0.03f - 0.02f), 0.004f, 0.012f, 8, wood);
            m.Box(new Vector3(r.xMin, 0.04f, r.center.y), new Vector3(0.06f, 0.08f, r.height), wood);
                m.Cylinder(new Vector3(r.xMin + 0.03f - 0.02f, 0.081f, r.center.y - r.height * 0.5f + 0.02f), 0.004f, 0.012f, 8, wood);
                m.Cylinder(new Vector3(r.xMin + 0.03f - 0.02f, 0.081f, r.center.y + r.height * 0.5f - 0.02f), 0.004f, 0.012f, 8, wood);
            m.Box(new Vector3(r.xMax, 0.04f, r.center.y), new Vector3(0.06f, 0.08f, r.height), wood);
                // No screws on far edge board (optional symmetry could be added)
        }

        // Creates a few thin icy patches on a rectangular concrete area.
        // Uses the method’s deterministic 'rng' so the same spots appear each run.
        private static void IcePatches(RaidMesh m, Rect area, System.Random rng)
        {
            var iceMat = DecorPalette.Gloss(new Color(0.82f, 0.92f, 1.00f));
            for (int i = 0; i < 4; i++) // four patches – well under the 12‑primitive limit
            {
                float fx = (float)rng.NextDouble() * area.width + area.xMin;
                float fz = (float)rng.NextDouble() * area.height + area.yMin;
                // thin ellipsoid – 0.002 m tall, ~0.25 m wide/deep, placed just above the concrete surface.
                m.Ellipsoid(new Vector3(fx, 0.004f, fz), new Vector3(0.25f, 0.002f, 0.25f), iceMat, 4, 2);
            }
        }

        // A soft snow‑drift clump placed near the front stoop.
        // Uses the same 'rng' for deterministic offset.
        private static void SnowDrift(RaidMesh m, Vector3 centre, System.Random rng)
        {
            var snowMat = DecorPalette.Matte(new Color(0.93f, 0.94f, 0.97f));
            for (int i = 0; i < 3; i++) // three drifts – still well under the primitive budget
            {
                float ox = (float)rng.NextDouble() * 0.3f - 0.15f; // ±0.15 m X offset
                float oz = (float)rng.NextDouble() * 0.3f - 0.15f; // ±0.15 m Z offset
                // modest ellipsoid – 0.03 m tall, 0.15 m radius, just above ground.
                m.Ellipsoid(centre + new Vector3(ox, 0.006f, oz), new Vector3(0.15f, 0.03f, 0.15f), snowMat, 4, 2);
            }
        }

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

        // The coir welcome mat on the stoop (at: the middle of its underside), hinged on its far edge so the survivor can lift
        // it (see DoormatFixture). Under it lie a square of damp, pale concrete and a dead beetle, both hidden inside the
        // mat until it's lifted.
        private static void Doormat(Transform world, RaidMesh m, Vector3 at)
        {
            const float w = 0.86f, d = 0.46f, thick = 0.012f;
            m.Flat(at + Vector3.up * 0.0015f, w - 0.02f, d - 0.02f, 0f, DecorPalette.Matte(new Color(0.47f, 0.46f, 0.42f)));
            m.Ellipsoid(at + new Vector3(0.14f, 0.004f, -0.07f), new Vector3(0.024f, 0.007f, 0.016f), DecorPalette.Gloss(new Color(0.10f, 0.07f, 0.05f)), 6, 3);

            var mat = new RaidMesh();
            mat.Box(at + Vector3.up * (thick * 0.5f), new Vector3(w, thick, d), DecorPalette.Matte(new Color(0.16f, 0.13f, 0.10f)));
            mat.Flat(at + Vector3.up * (thick + 0.0015f), w - 0.07f, d - 0.07f, 0f, DecorPalette.Matte(new Color(0.46f, 0.34f, 0.18f)));
            Surf welcome = DecorMaterials.Sign("WELCOME", new Color(0.46f, 0.34f, 0.18f), new Color(0.16f, 0.11f, 0.07f), out float aspect);
            mat.FlatUV(at + Vector3.up * (thick + 0.003f), 0.7f, 0.7f / aspect, 0f, welcome, new Rect(0f, 0f, 1f, 1f));

            DoormatFixture fixture = DoormatFixture.Create(world, at + new Vector3(0f, 0.03f, 0.04f - d * 0.5f));
            Cosmetic(fixture.gameObject);
            // Lifting the near edge turns the mat up about its far edge, toward the door.
            FixturePart part = FixturePart.Hinge(fixture.transform, "Mat", mat, at + new Vector3(0f, 0f, d * 0.5f), Vector3.right, 65f,
                ShadowCastingMode.Off).WithSpeed(0.25f);
            fixture.SetMat(part);
        }

        // A rural mailbox on a cedar post at the kerb, its door to the street and the red flag down. The door drops open on
        // its foot and the flag swings up (see MailboxFixture); the post stands in nobody's way.
        private static void Mailbox(Transform world, RaidMesh m, Vector3 at)
        {
            Surf cedar = DecorPalette.Matte(new Color(0.40f, 0.28f, 0.17f));
            m.Box(at + new Vector3(0f, 0.55f, 0f), new Vector3(0.09f, 1.1f, 0.09f), cedar);
            var postRng = new System.Random(8712);
            // Wood grain strips on the cedar post
            m.Ellipsoid(at + new Vector3(-0.02f, 0.55f, 0f), new Vector3(0.015f, 0.9f, 0.015f), DecorPalette.Matte(new Color(0.38f, 0.28f, 0.20f)), 4, 2);
            m.Ellipsoid(at + new Vector3(0f, 0.55f, 0f), new Vector3(0.015f, 0.9f, 0.015f), DecorPalette.Matte(new Color(0.38f, 0.28f, 0.20f)), 4, 2);
            m.Ellipsoid(at + new Vector3(0.02f, 0.55f, 0f), new Vector3(0.015f, 0.9f, 0.015f), DecorPalette.Matte(new Color(0.38f, 0.28f, 0.20f)), 4, 2);
            m.Box(at + new Vector3(0f, 1.11f, 0f), new Vector3(0.18f, 0.03f, 0.34f), cedar);
            Surf box = DecorPalette.Metal(new Color(0.18f, 0.20f, 0.24f));
            m.Box(at + new Vector3(0f, 1.2f, 0f), new Vector3(0.22f, 0.16f, 0.48f), box);
            m.CylinderX(at + new Vector3(0f, 1.28f, 0f), 0.11f, 0.48f, 10, box, 90f);
            // Tiny metal nails on the post
            m.Cylinder(at + new Vector3(0f, 0.25f, 0.045f), 0.0015f, 0.008f, 8, DecorPalette.Metal(new Color(0.6f, 0.6f, 0.55f)), Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(at + new Vector3(0f, 0.55f, 0.045f), 0.0015f, 0.008f, 8, DecorPalette.Metal(new Color(0.6f, 0.6f, 0.55f)), Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(at + new Vector3(0f, 0.85f, 0.045f), 0.0015f, 0.008f, 8, DecorPalette.Metal(new Color(0.6f, 0.6f, 0.55f)), Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(at + new Vector3(0f, 0.25f, -0.045f), 0.0015f, 0.008f, 8, DecorPalette.Metal(new Color(0.6f, 0.6f, 0.55f)), Quaternion.Euler(90f, 0f, 0f));
            // Small chipped paint spot on lower front
            m.Ellipsoid(at + new Vector3(0f, 0.15f, 0.045f), new Vector3(0.02f, 0.03f, 0.001f), DecorPalette.Matte(new Color(0.50f, 0.38f, 0.32f)), 4, 2);
            // The dark mouth behind the door, and the boss the flag turns on.
            Surf dark = DecorPalette.Matte(new Color(0.04f, 0.04f, 0.05f));
            m.Box(at + new Vector3(0f, 1.205f, -0.241f), new Vector3(0.19f, 0.15f, 0.002f), dark);
            m.Cylinder(at + new Vector3(0f, 1.28f, -0.242f), 0.095f, 0.002f, 12, dark, Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(at + new Vector3(0.11f, 1.18f, 0.2f), 0.014f, 0.012f, 8, box, Quaternion.Euler(0f, 0f, -90f));

            MailboxFixture fixture = MailboxFixture.Create(world, at + new Vector3(0f, 1.25f, -0.3f));
            Cosmetic(fixture.gameObject);

            // The door: the box's profile (a square foot under a round top), hinged along its bottom edge.
            var door = new RaidMesh();
            Surf face = DecorPalette.Metal(new Color(0.24f, 0.26f, 0.30f));
            door.Box(at + new Vector3(0f, 1.2f, -0.252f), new Vector3(0.226f, 0.16f, 0.012f), face);
            // Brass surface for pull knob
            Surf brass = DecorPalette.Metal(new Color(0.85f, 0.71f, 0.36f)); // faded brass for a pull knob
            // Slightly darker metal for hinges and plates
            Surf metal = DecorPalette.Metal(new Color(0.20f, 0.22f, 0.24f)); // slightly darker than the door face
            // Thin metal latch plate
            door.Box(at + new Vector3(0f, 1.34f, -0.263f), new Vector3(0.07f, 0.015f, 0.002f), face); // latch plate
            // Bolt protruding from plate
            door.Cylinder(at + new Vector3(0f, 1.34f, -0.269f), 0.003f, 0.02f, 12, face, Quaternion.Euler(-90f, 0f, 0f));
            // Tiny rivet on the plate
            door.Cylinder(at + new Vector3(0f, 1.345f, -0.263f), 0.002f, 0.004f, 8, face, Quaternion.Euler(-90f, 0f, 0f));
            // Subtle rust streak on the door edge for weathering
            door.Ellipsoid(at + new Vector3(0f, 1.33f, -0.265f), new Vector3(0.02f, 0.003f, 0.001f), DecorPalette.Matte(new Color(0.25f, 0.18f, 0.12f)), 4, 2);
            // Brass pull knob on the latch plate’s right side
            door.Cylinder(at + new Vector3(0.02f, 1.34f, -0.263f), 0.004f, 0.002f, 8, brass, Quaternion.Euler(-90f, 0f, 0f));
            // Small rust speck on the door’s upper edge for weathered detail
            door.Ellipsoid(at + new Vector3(0f, 1.33f, -0.263f), new Vector3(0.015f, 0.008f, 0.001f), DecorPalette.Matte(new Color(0.25f, 0.18f, 0.12f)), 4, 2);
            door.Cylinder(at + new Vector3(0f, 1.28f, -0.258f), 0.113f, 0.012f, 12, face, Quaternion.Euler(90f, 0f, 0f));
            door.Box(at + new Vector3(0f, 1.35f, -0.262f), new Vector3(0.04f, 0.018f, 0.01f), DecorPalette.Metal(new Color(0.55f, 0.55f, 0.52f)));
            // Left hinge
            door.Cylinder(at + new Vector3(-0.108f, 1.25f, -0.252f), 0.002f, 0.015f, 8, metal, Quaternion.Euler(0f, 0f, 0f));
            // Right hinge
            door.Cylinder(at + new Vector3( 0.108f, 1.25f, -0.252f), 0.002f, 0.015f, 8, metal, Quaternion.Euler(0f, 0f, 0f));
            // Hinge pins
            door.Cylinder(at + new Vector3(-0.108f, 1.255f, -0.252f), 0.001f, 0.004f, 8, metal, Quaternion.Euler(90f, 0f, 0f));
            door.Cylinder(at + new Vector3( 0.108f, 1.255f, -0.252f), 0.001f, 0.004f, 8, metal, Quaternion.Euler(90f, 0f, 0f));
            // Name-plate base
            door.Box(at + new Vector3(0f, 1.33f, -0.265f), new Vector3(0.07f, 0.001f, 0.02f), metal);
            // Engraved "MAIL"
            door.Box(at + new Vector3(0f, 1.331f, -0.263f), new Vector3(0.058f, 0.0005f, 0.012f), DecorPalette.Matte(new Color(0.12f,0.12f,0.10f)));
            // Extra rust streak lower edge
            door.Ellipsoid(at + new Vector3(0f, 1.20f, -0.260f), new Vector3(0.025f, 0.003f, 0.001f), DecorPalette.Matte(new Color(0.23f, 0.16f, 0.12f)), 4, 2);
            FixturePart doorPart = FixturePart.Hinge(fixture.transform, "Door", door, at + new Vector3(0f, 1.12f, -0.252f), Vector3.right, 100f).WithSpeed(0.3f);

            // The flag: a red arm lying forward along the box's side with the flag at its end; it swings up on the boss.
            var flag = new RaidMesh();
            Surf red = DecorPalette.Matte(new Color(0.72f, 0.12f, 0.10f));
            Vector3 pivot = at + new Vector3(0.126f, 1.18f, 0.2f);
            flag.Box(pivot + new Vector3(0f, 0f, -0.12f), new Vector3(0.008f, 0.016f, 0.26f), red);
            flag.Box(pivot + new Vector3(0f, 0.022f, -0.215f), new Vector3(0.008f, 0.06f, 0.07f), red);
            // Tiny metal bracket at flag hinge pivot
            m.Box(pivot + new Vector3(-0.006f, 0.006f, 0f), new Vector3(0.012f, 0.012f, 0.02f), box);
            FixturePart flagPart = FixturePart.Hinge(fixture.transform, "Flag", flag, pivot, Vector3.right, 90f).WithSpeed(0.3f);

            fixture.SetParts(doorPart, flagPart);
        }

        // A green garden hose coiled on a reel, its brass nozzle out on the lawn, fed from the outside tap: a standpipe between
        // the reel and the wall with a red wheel that turns (see HoseSpigot). While the tap runs, water trickles from the nozzle.
        private static void HoseReel(Transform world, RaidMesh m, Vector3 at)
        {
            Surf frame = DecorPalette.Metal(new Color(0.22f, 0.24f, 0.22f));
            Surf hose = DecorPalette.Gloss(new Color(0.16f, 0.42f, 0.18f));
            Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);
            m.Box(at + new Vector3(-0.2f, 0.25f, 0f), new Vector3(0.04f, 0.5f, 0.04f), frame);
            m.Box(at + new Vector3(0.2f, 0.25f, 0f), new Vector3(0.04f, 0.5f, 0.04f), frame);
            m.CylinderX(at + new Vector3(0f, 0.36f, 0f), 0.04f, 0.44f, 8, frame);
            m.CylinderX(at + new Vector3(0f, 0.36f, 0f), 0.2f, 0.3f, 14, hose);
            Vector3 end = at + new Vector3(-0.6f, 0.01f, 0.45f);
            m.Rod(at + new Vector3(-0.1f, 0.2f, 0.1f), end, 0.035f, hose, 6);
                // Darkened wear stripe along the hose
// Darkened wear stripe moved below
            Vector3 dir = new Vector3(-0.5f, 0f, 0.35f).normalized;
            Vector3 tip = end + dir * 0.08f;
            m.Rod(end, tip, 0.03f, brass, 8);

            // The standpipe, its tap and spout, and the short length of hose from the spout down onto the reel.
            Vector3 pipe = new Vector3(0.83f, at.y, at.z);
            m.Cylinder(pipe, 0.016f, 0.6f, 8, DecorPalette.Metal(new Color(0.52f, 0.54f, 0.53f)));
                // Rust stain on standpipe side
                m.Ellipsoid(pipe + new Vector3(0f, 0.32f, 0.008f), new Vector3(0.02f, 0.01f, 0.005f), DecorPalette.Matte(new Color(0.45f, 0.30f, 0.24f)), 4, 2);
            Vector3 body = pipe + Vector3.up * 0.63f, elbow = body + new Vector3(-0.065f, 0f, 0f);
            m.Box(body, new Vector3(0.045f, 0.04f, 0.045f), brass);
            m.Rod(body, elbow, 0.022f, brass, 8);
            m.Rod(elbow, elbow - Vector3.up * 0.045f, 0.022f, brass, 8);
            Vector3 coupling = elbow - Vector3.up * 0.075f;
            m.Cylinder(coupling, 0.017f, 0.03f, 8, brass);
                // Metal washer on pipe coupling
                m.Cylinder(coupling, 0.012f, 0.001f, 12, DecorPalette.Metal(new Color(0.60f, 0.60f, 0.55f)));
            m.Cylinder(coupling + new Vector3(0f, 0f, 0.015f), 0.009f, 0.004f, 6, brass);
            m.Rod(coupling, at + new Vector3(0.12f, 0.52f, 0f), 0.03f, hose, 6);
            m.Cylinder(body + Vector3.up * 0.018f, 0.005f, 0.016f, 6, brass);

            HoseSpigot tap = HoseSpigot.Create(world, body + Vector3.up * 0.04f, coupling, new Vector3(0.9f, 0f, at.z), Vector3.left);
            Cosmetic(tap.gameObject);

            // The wheel: a red disc with a spoke and a knob, turning anticlockwise (seen from above) to open.
            var wheel = new RaidMesh();
            Surf red = DecorPalette.Matte(new Color(0.66f, 0.12f, 0.09f));
            Vector3 hub = body + Vector3.up * 0.032f;
                // Brass tension knob on the wheel hub
                wheel.Cylinder(hub + new Vector3(0f, 0.015f, 0f), 0.004f, 0.003f, 8, brass);
            wheel.Cylinder(hub, 0.032f, 0.01f, 12, red);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                wheel.Cylinder(hub + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.029f, 0.003f, 0.008f, 6, red);
            }
            wheel.Box(hub + Vector3.up * 0.012f, new Vector3(0.058f, 0.006f, 0.01f), DecorPalette.Matte(new Color(0.42f, 0.07f, 0.05f)));
            wheel.Ellipsoid(hub + new Vector3(0.024f, 0.017f, 0f), new Vector3(0.012f, 0.014f, 0.012f), red, 6, 4);
            FixturePart wheelPart = FixturePart.Hinge(tap.transform, "Wheel", wheel, hub, Vector3.down, 270f, ShadowCastingMode.Off).WithSpeed(0.6f);

            // The trickle: a glistening run of water from the nozzle and a dark wet patch spreading on the lawn.
            var water = new RaidMesh();
            var splash = new System.Random(77);
            Vector3 pool = tip + dir * 0.16f;
            water.FlatBlob(new Vector3(pool.x, 0.009f, pool.z), 0.26f, 0.2f, 35f, 0.3f, splash, DecorPalette.Matte(new Color(0.10f, 0.12f, 0.08f)));
            water.FlatBlob(new Vector3(pool.x, 0.011f, pool.z), 0.15f, 0.11f, 35f, 0.3f, splash, DecorPalette.Gloss(new Color(0.46f, 0.56f, 0.62f)));
            water.Rod(tip, tip + dir * 0.12f - Vector3.up * 0.01f, 0.012f, DecorPalette.Gloss(new Color(0.56f, 0.66f, 0.72f)), 6);
            GameObject trickle = water.Emit("Trickle", tap.transform, tip, ShadowCastingMode.Off);
            trickle.transform.rotation = Quaternion.identity;
            Renderer trickleRenderer = trickle.GetComponent<Renderer>();
            trickleRenderer.enabled = false;

            tap.SetParts(wheelPart, trickleRenderer);
        }
        #endregion
    }
}
