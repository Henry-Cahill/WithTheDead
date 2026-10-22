using UnityEngine;

namespace CloneZ
{
    // The outdoor fill of the streets and verges (the same props made usable and their interactive pieces live in
    // RaidMapBuilder.Props.cs): dead cars nosed in the road, street lights (most dead, one leaning), the Guard
    // checkpoint and the police road block at the south and north exits, the rail line with its crossbuck, hydrants
    // and mail drop boxes on the sidewalks, bare trees in the yards, plowed snow heaps, walk-through drifts and blown
    // litter.
    public static partial class RaidMapBuilder
    {
        #region Outdoor props
        private static void BuildOutdoorProps(RaidTown town)
        {
            System.Random rng = town.Rng;

            // Dead cars left in the streets, nosed at odd angles.
            (Vector3 p, float yaw)[] street =
            {
                (new Vector3(2.0f, 0f, 27f), 8f), (new Vector3(-2.1f, 0f, -42f), 186f), (new Vector3(-1.8f, 0f, 46f), 172f),
                (new Vector3(2.2f, 0f, -14f), 15f), (new Vector3(28f, 0f, -2.0f), 95f), (new Vector3(-28f, 0f, 2.1f), 268f),
                (new Vector3(-46f, 0f, -1.8f), 84f), (new Vector3(50f, 0f, 2.0f), 280f),
                (new Vector3(10f, 0f, 30f), 45f), (new Vector3(-15f, 0f, -30f), 120f), (new Vector3(35f, 0f, 10f), 200f),
            };
            foreach (var s in street)
            {
                DecorProps.CarKind kind = rng.NextDouble() < 0.4 ? DecorProps.CarKind.Wagon : (rng.NextDouble() < 0.5 ? DecorProps.CarKind.Pickup : DecorProps.CarKind.Sedan);
                Wreck(town, s.p, s.yaw + R(rng, -8f, 8f), kind, Paint(town));
            }

            StreetLights(town);
            Checkpoint(town);
            RoadBlock(town);
            RailLine(town);

            // Hydrants and blue mail drop boxes on the sidewalks.
            Vector3[] hydrants = { new Vector3(5.2f, 0f, -9.5f), new Vector3(-5.2f, 0f, 22f), new Vector3(-19f, 0f, -5.3f), new Vector3(31f, 0f, 5.3f) };
            foreach (Vector3 h in hydrants) HydrantProp(town, h, R(rng, 0f, 360f));
            MailDropProp(town, new Vector3(5.3f, 0f, 19.5f), 270f);
            AddMailDropDetails(town, new Vector3(5.3f, 0f, 19.5f), 270f);
            MailDropProp(town, new Vector3(-5.3f, 0f, -21f), 90f);
            AddMailDropDetails(town, new Vector3(-5.3f, 0f, -21f), 90f);

            // Bare trees in the yards and verges.
            int trees = 0;
            for (int attempt = 0; attempt < 400 && trees < 34; attempt++)
            {
                var p = new Vector3(R(rng, -66f, 66f), 0f, R(rng, -66f, 66f));
                Rect r = Around(p, 1.4f, 1.4f);
                if (town.Blocked(r) || OnRoad(r)) continue;
                float h = R(rng, 4.5f, 7.5f);
                Prop(town, p, R(rng, 0f, 360f), new Vector3(0.45f, 3f, 0.45f), true, (m, rr) => DecorProps.BareTree(m, rr, h));
                trees++;
            }

            // Plowed snow heaps (solid) and low drifts (walk-through).
            int heaps = 0;
            for (int attempt = 0; attempt < 200 && heaps < 9; attempt++)
            {
                var p = new Vector3(R(rng, -64f, 64f), 0f, R(rng, -64f, 64f));
                float w = R(rng, 2f, 3.2f), d = R(rng, 1.6f, 2.6f), h = R(rng, 0.9f, 1.4f);
                Rect r = Around(p, w * 0.6f, d * 0.6f);
                if (town.Blocked(r) || OnRoad(r)) continue;
                Prop(town, p, R(rng, 0f, 180f), new Vector3(w * 0.75f, h, d * 0.75f), true, (m, rr) => DecorProps.SnowPile(m, rr, Vector3.zero, w, d, h, 0f));
                heaps++;
            }
            for (int i = 0; i < 60; i++)
            {
                var p = new Vector3(R(rng, -67f, 67f), 0f, R(rng, -67f, 67f));
                Rect r = Around(p, 1.2f, 1.2f);
                if (town.Blocked(r)) continue;
                DecorProps.SnowPile(town.Out.Solid(p), rng, p, R(rng, 1.2f, 2.6f), R(rng, 0.8f, 1.6f), R(rng, 0.1f, 0.22f), R(rng, 0f, 180f));
            }

            // Litter blown along the streets and into corners.
            for (int i = 0; i < 260; i++)
            {
                var p = new Vector3(R(rng, -68f, 68f), 0.035f, R(rng, -68f, 68f));
                if (rng.NextDouble() < 0.6)
                {
                    // Bias toward the streets.
                    if (rng.NextDouble() < 0.5) p.x = R(rng, -6f, 6f);
                    else p.z = R(rng, -6f, 6f);
                }
                foreach (RaidBuilding b in town.Buildings)
                    if (b.Footprint.Contains(new Vector2(p.x, p.z))) { p.y = -1f; break; }
                if (p.y < 0f) continue;
                DecorProps.Litter(town.Out.Flat(p), rng, p, true);
            }
        }

        private static void StreetLights(RaidTown town)
        {
            System.Random rng = town.Rng;
            (Vector3 p, float yaw)[] poles =
            {
                (new Vector3(6.9f, 0f, -50f), 270f), (new Vector3(6.9f, 0f, -10f), 270f), (new Vector3(6.9f, 0f, 30f), 270f),
                (new Vector3(-6.9f, 0f, -30f), 90f), (new Vector3(-6.9f, 0f, 12f), 90f), (new Vector3(-6.9f, 0f, 52f), 90f),
                (new Vector3(-56f, 0f, 6.9f), 180f), (new Vector3(-14f, 0f, 6.9f), 180f), (new Vector3(46f, 0f, 6.9f), 180f),
                (new Vector3(-34f, 0f, -6.9f), 0f), (new Vector3(26f, 0f, -6.9f), 0f),
            };
            // Most are dead: two or three still burn sodium-orange, one leans after a crash.
            bool[] lit = new bool[poles.Length];
            int count = 2 + rng.Next(2);
            for (int i = 0; i < count; i++) lit[rng.Next(poles.Length)] = true;
            int leaning = rng.Next(poles.Length);
            for (int i = 0; i < poles.Length; i++)
            {
                Vector3 p = poles[i].p;
                float yaw = poles[i].yaw;
                bool on = lit[i] && i != leaning;
                float lean = i == leaning ? R(rng, 8f, 14f) : 0f;
                RaidMesh m = town.Out.Solid(p);
                m.SetFrame(p, yaw);
                Vector3 lamp = DecorProps.StreetLight(m, rng, on, lean);
                // Add a subtle rust‑stained metal bracket near the top of the pole
                // Positioned slightly forward on the pole for visual detail
                Vector3 bracketPos = new Vector3(0f, 5.0f, 0.08f);
                Vector3 bracketSize = new Vector3(0.12f, 0.02f, 0.02f);
                Color rustColor = new Color(0.45f, 0.38f, 0.33f);
                m.Box(bracketPos, bracketSize, DecorPalette.Metal(rustColor));
                Vector3 world = m.ToMeshSpace(lamp);
                m.ResetFrame();
                town.PropCollider(p + Vector3.up * 2.75f, new Vector3(0.3f, 5.5f, 0.3f), yaw);
                town.Keepout.Add(Around(p, 0.6f, 0.6f));
                if (on) town.Light(town.World, world, new Color(1f, 0.72f, 0.42f), 13f, 2.2f, rng.NextDouble() < 0.3, true);
            }
        }

        // South exit: the Guard checkpoint on Main St — sandbags either side, barriers across the road
        // beyond the zone, a guard booth, an army truck and a floodlight still burning.
        private static void Checkpoint(RaidTown town)
        {
            for (int s = -1; s <= 1; s += 2)
                Prop(town, new Vector3(s * 7.6f, 0f, -59.5f), 90f, new Vector3(4f, 0.8f, 0.5f), true, (m, r) => { DecorProps.SandbagWall(m, r, 4f, 5); m.Box(new Vector3(0f, 0.45f, 0.27f), new Vector3(3.8f, 0.02f, 0.04f), DecorPalette.Metal(new Color(0.38f, 0.36f, 0.34f))); float rivetRadius = 0.015f; float rivetHeight = 0.025f; Color rust = new Color(0.45f, 0.38f, 0.33f); m.Cylinder(new Vector3(-1.9f, 0.45f, 0.27f), rivetRadius, rivetHeight, 6, DecorPalette.Metal(rust)); m.Cylinder(new Vector3(1.9f, 0.45f, 0.27f), rivetRadius, rivetHeight, 6, DecorPalette.Metal(rust)); });
            for (int i = -1; i <= 1; i++)
                Prop(town, new Vector3(i * 3.2f, 0f, -69.6f), 0f, new Vector3(2.6f, 0.9f, 0.6f), true, (m, r) => {
                DecorProps.JerseyBarrier(m, r, 2.6f, true);
                // Add tiny rust‑stained rivets on the barrier top surface for subtle wear
                float rivetRadius = 0.015f;
                float rivetHeight = 0.025f;
                Color rust = new Color(0.45f, 0.38f, 0.33f);
                // Position near each end of the barrier, centered on Z
                m.Cylinder(new Vector3(-1.25f, 0.45f, 0f), rivetRadius, rivetHeight, 6, DecorPalette.Metal(rust));
                m.Cylinder(new Vector3(1.25f, 0.45f, 0f), rivetRadius, rivetHeight, 6, DecorPalette.Metal(rust));
            });
            GuardBoothProp(town, new Vector3(9.8f, 0f, -63.5f), 270f);
            // Add rust‑stained door hardware and wear to the guard‑booth
            Prop(town,
                new Vector3(9.8f, 0f, -63.5f), // same world position as the booth
                270f, // same yaw as the booth
                new Vector3(2f, 2.4f, 0.6f), // a modest bounding box – matches the booth size
                true,
                (m, r) =>
                {
                    // rust colour (slightly varied per booth)
                    Color rust = new Color(0.45f, 0.38f, 0.33f);
                    // 1️⃣ left hinge – small cylinder on the door edge
                    // size: radius 0.015 m, height 0.06 m, positioned at (-0.45 m, 0.90 m, 0.28 m) relative to booth centre
                    m.Cylinder(new Vector3(-0.45f, 0.90f, 0.28f), 0.015f, 0.06f, 6, DecorPalette.Metal(rust));
                    // 2️⃣ right hinge – mirror of the left hinge
                    // positioned at (0.45 m, 0.90 m, 0.28 m)
                    m.Cylinder(new Vector3(0.45f, 0.90f, 0.28f), 0.015f, 0.06f, 6, DecorPalette.Metal(rust));
                    // 3️⃣ latch – thin rusted bar across the lower front of the door
                    // size: 0.60 m × 0.02 m × 0.02 m, positioned at (0 m, 0.85 m, 0.31 m)
                    m.Box(new Vector3(0f, 0.85f, 0.31f), new Vector3(0.60f, 0.02f, 0.02f), DecorPalette.Metal(rust));
                    // 4️⃣ worn wood strip – a faint crack/tear in the door panel
                    // size: 0.50 m × 0.02 m × 0.02 m, positioned at (0 m, 0.65 m, 0.33 m)
                    m.Box(new Vector3(0f, 0.65f, 0.33f), new Vector3(0.50f, 0.02f, 0.02f),
                        DecorPalette.Matte(new Color(0.32f, 0.23f, 0.18f)));
                    // 5️⃣ broken glass fragment – a small clear piece near the door top
                    // size: 0.10 m × 0.12 m × 0.01 m, positioned at (0.20 m, 1.10 m, 0.32 m)
                    m.Box(new Vector3(0.20f, 1.10f, 0.32f), new Vector3(0.10f, 0.12f, 0.01f),
                        DecorPalette.Glass(new Color(0.90f, 0.90f, 0.95f)));
                });
            Wreck(town, new Vector3(-10.5f, 0f, -57f), 0f, DecorProps.CarKind.ArmyTruck, Color.white);
            Vector3 tower = new Vector3(8.6f, 0f, -54f);
            Prop(town, tower, 0f, new Vector3(0.4f, 5f, 0.4f), true, (m, r) =>
            {
                m.Box(new Vector3(0f, 0.1f, 0f), new Vector3(0.9f, 0.2f, 0.9f), DecorMaterials.Concrete);
                m.Cylinder(new Vector3(0f, 0.2f, 0f), 0.08f, 4.8f, 8, DecorPalette.Metal(new Color(0.3f, 0.32f, 0.28f)));
                m.Box(new Vector3(0f, 5.0f, 0f), new Vector3(0.9f, 0.1f, 0.2f), DecorPalette.Metal(new Color(0.3f, 0.32f, 0.28f)));
                // Add a rust‑stained metal grill on the front of the floodlight
                float grillZ = 0.12f; // just in front of the top plate
                Vector3 grillPos = new Vector3(0f, 5.05f, grillZ);
                Vector3 grillSize = new Vector3(0.38f, 0.02f, 0.02f);
                Color grillColor = new Color(0.45f, 0.38f, 0.33f); // dull rust tone
                m.Box(grillPos, grillSize, DecorPalette.Metal(grillColor));
                for (int k = -1; k <= 1; k += 2)
                {
                    m.Box(new Vector3(k * 0.32f, 5.15f, 0.05f), new Vector3(0.3f, 0.26f, 0.22f), Quaternion.Euler(25f, 0f, 0f), DecorPalette.Matte(new Color(0.2f, 0.22f, 0.18f)));
                    m.Box(new Vector3(k * 0.32f, 5.1f, 0.17f), new Vector3(0.24f, 0.2f, 0.01f), Quaternion.Euler(25f, 0f, 0f), DecorPalette.Glow(new Color(0.92f, 0.95f, 1f)));
                }
            });
            town.Light(town.World, tower + new Vector3(0f, 4.9f, 0.6f), new Color(0.88f, 0.92f, 1f), 16f, 2.4f, false, true);
            Prop(town, new Vector3(-7.0f, 0f, -52.4f), 0f, new Vector3(2.4f, 2.2f, 0.3f), true, (m, r) => {
DecorProps.PostSign(m, r, "CHECKPOINT", new Color(0.20f, 0.26f, 0.16f), new Color(0.88f, 0.86f, 0.74f), 0.42f, 1.3f);
                 // thin wooden back‑board, slightly behind the paint
                 Vector3 boardPos = new Vector3(0f, 0.6f, -0.01f);
                 Vector3 boardSize = new Vector3(0.45f, 0.02f, 0.28f);
                 m.Box(boardPos, boardSize, DecorPalette.Matte(new Color(0.32f, 0.24f, 0.18f)));
                 // rust‑stained brackets at top left/right
                 Color rust = new Color(0.45f, 0.38f, 0.33f);
                 Vector3 leftBracketPos = new Vector3(-0.22f, 0.72f, 0f);
                 Vector3 rightBracketPos = new Vector3(0.22f, 0.72f, 0f);
                 Vector3 bracketSize = new Vector3(0.04f, 0.02f, 0.02f);
                 m.Box(leftBracketPos, bracketSize, DecorPalette.Metal(rust));
                 m.Box(rightBracketPos, bracketSize, DecorPalette.Metal(rust));
                 // screw heads on brackets
                 float screwR = 0.005f, screwH = 0.01f;
                 m.Cylinder(leftBracketPos + new Vector3(-0.015f, 0.01f, 0f), screwR, screwH, 6, DecorPalette.Metal(rust));
                 m.Cylinder(leftBracketPos + new Vector3( 0.015f, 0.01f, 0f), screwR, screwH, 6, DecorPalette.Metal(rust));
                 m.Cylinder(rightBracketPos + new Vector3(-0.015f, 0.01f, 0f), screwR, screwH, 6, DecorPalette.Metal(rust));
                 m.Cylinder(rightBracketPos + new Vector3( 0.015f, 0.01f, 0f), screwR, screwH, 6, DecorPalette.Metal(rust));
                 // hinged latch with slight random offset
                 System.Random rng = town.Rng;
                 float offsetX = (float)(rng.NextDouble() * 0.04 - 0.02);
                 float offsetZ = (float)(rng.NextDouble() * 0.04 - 0.02);
                 Vector3 latchPos = new Vector3(offsetX, 0.20f, 0.25f + offsetZ);
                 Vector3 latchSize = new Vector3(0.60f, 0.02f, 0.02f);
                 m.Box(latchPos, latchSize, DecorPalette.Metal(rust));
                 // tiny glow‑lamp on latch
                 Vector3 lampPos = latchPos + new Vector3(0f, 0.03f, 0f);
                 Vector3 lampSize = new Vector3(0.02f, 0.02f, 0.01f);
                 m.Box(lampPos, lampSize, DecorPalette.Glow(new Color(1f, 0.9f, 0.6f)));
            });
        }

        // North exit: a police road block with barriers across Main St and a cruiser on the shoulder.
        private static void RoadBlock(RaidTown town)
        {
            for (int i = -1; i <= 1; i++)
                Prop(town, new Vector3(i * 3.2f, 0f, 69.6f), 0f, new Vector3(2.6f, 0.9f, 0.6f), true, (m, r) => {
                DecorProps.JerseyBarrier(m, r, 2.6f, false);
                // Add tiny rust‑stained rivets on the barrier top surface for subtle wear
                float rivetRadius = 0.015f;
                float rivetHeight = 0.025f;
                Color rust = new Color(0.45f, 0.38f, 0.33f);
                // Position near each end of the barrier, centered on Z
                m.Cylinder(new Vector3(-1.25f, 0.45f, 0f), rivetRadius, rivetHeight, 6, DecorPalette.Metal(rust));
                m.Cylinder(new Vector3(1.25f, 0.45f, 0f), rivetRadius, rivetHeight, 6, DecorPalette.Metal(rust));
            });
            Prop(town, new Vector3(-5.8f, 0f, 68.4f), 15f, new Vector3(1.6f, 1.05f, 0.5f), true, (m, r) => DecorProps.Sawhorse(m, r, 1.6f));
            Prop(town, new Vector3(5.6f, 0f, 68.6f), -10f, new Vector3(1.6f, 1.05f, 0.5f), true, (m, r) => DecorProps.Sawhorse(m, r, 1.6f));
            Wreck(town, new Vector3(9.2f, 0f, 66.5f), 120f, DecorProps.CarKind.Police, new Color(0.10f, 0.12f, 0.16f));
            Prop(town, new Vector3(-7.4f, 0f, 66.6f), 180f, new Vector3(2.4f, 2.2f, 0.3f), true, (m, r) => DecorProps.PostSign(m, r, "ROAD CLOSED", new Color(0.86f, 0.84f, 0.78f), new Color(0.12f, 0.12f, 0.12f), 0.4f, 1.2f));
        }

        // East: the rail line on its ballast bed, crossing Depot Rd at the Rail Bridge exit.
        private static void RailLine(RaidTown town)
        {
            const float x0 = 66f;
            Surf sleeper = DecorMaterials.Planks(new Color(0.30f, 0.22f, 0.15f));
            Surf rail = DecorPalette.Metal(new Color(0.36f, 0.30f, 0.26f));
            for (float z = -GroundHalf; z < GroundHalf - 0.1f; z += 10f)
            {
                float z1 = Mathf.Min(GroundHalf, z + 10f);
                // The ballast and sleepers stop where the track crosses Depot Rd and its sidewalks.
                if (z < -6.6f) Paved(town, Rect.MinMaxRect(x0 - 1.9f, z, x0 + 1.9f, Mathf.Min(z1, -6.6f)), 0.02f, DecorMaterials.Gravel);
                if (z1 > 6.6f) Paved(town, Rect.MinMaxRect(x0 - 1.9f, Mathf.Max(z, 6.6f), x0 + 1.9f, z1), 0.02f, DecorMaterials.Gravel);
                RaidMesh m = town.Out.Solid(new Vector3(x0, 0f, (z + z1) * 0.5f));
                float boltRadius = 0.01f;
                float boltHeight = 0.025f;
                Color rust = new Color(0.45f, 0.38f, 0.33f);
                for (int k = -1; k <= 1; k += 2) {
                    m.Box(new Vector3(x0 + k * 0.72f, 0.09f, (z + z1) * 0.5f), new Vector3(0.08f, 0.14f, z1 - z), rail);
                    // bolt near the front end of the rail
                    m.Cylinder(new Vector3(x0 + k * 0.72f, 0.15f, (z + z1) * 0.5f - (z1 - z) * 0.25f), boltRadius, boltHeight, 6, DecorPalette.Metal(rust));
                    // bolt near the rear end of the rail
                    m.Cylinder(new Vector3(x0 + k * 0.72f, 0.15f, (z + z1) * 0.5f + (z1 - z) * 0.25f), boltRadius, boltHeight, 6, DecorPalette.Metal(rust));
                }
                for (float s = z + 0.3f; s < z1; s += 0.65f)
                    if (Mathf.Abs(s) > 6.6f) m.Box(new Vector3(x0, 0.045f, s), new Vector3(2.5f, 0.09f, 0.22f), sleeper);
            }
            for (int s = -1; s <= 1; s += 2)
                Prop(town, new Vector3(61.4f, 0f, s * 7.3f), s > 0 ? 180f : 0f, new Vector3(0.3f, 3f, 0.3f), true, (m, r) => Crossbuck(m));
            Prop(town, new Vector3(-58.5f, 0f, 7.0f), 180f, new Vector3(2.4f, 2.2f, 0.3f), true, (m, r) => DecorProps.PostSign(m, r, "BOAT DOCK", new Color(0.16f, 0.24f, 0.30f), new Color(0.86f, 0.86f, 0.80f), 0.4f, 1.2f));
            // Mooring posts and the first planks of the dock inside the edge.
            RaidMesh d = town.Out.Solid(new Vector3(-68f, 0f, 0f));
            for (float x = -70.6f; x < -65f; x += 0.35f)
                d.Box(new Vector3(x, 0.05f, 0f), new Vector3(0.3f, 0.06f, 3.0f), DecorMaterials.Planks(new Color(0.42f, 0.34f, 0.24f)));
        }

        private static void AddMailDropDetails(RaidTown town, Vector3 pos, float yaw)
        {
            System.Random rng = town.Rng;
            // pick a rust colour that varies slightly per box
            float r = 0.40f + (float)rng.NextDouble() * 0.10f;
            float g = 0.35f + (float)rng.NextDouble() * 0.08f;
            float b = 0.30f + (float)rng.NextDouble() * 0.07f;
            Color rust = new Color(r, g, b);
            // base mesh for the extra geometry – use the same batch as the mailbox so it inherits its transform
            RaidMesh m = town.Out.Solid(pos);
            m.SetFrame(pos, yaw);
            // 1️⃣ metal latch – thin bar across the front lower edge of the box
            // size: 0.60 m × 0.02 m × 0.02 m, positioned 0.20 m above ground, 0.25 m forward from centre
            Vector3 latchPos = new Vector3(0f, 0.20f, 0.25f);
            Vector3 latchSize = new Vector3(0.60f, 0.02f, 0.02f);
            m.Box(latchPos, latchSize, DecorPalette.Metal(rust));
            // 2️⃣ rivet at left end of latch
            float rivetRadius = 0.01f;
            float rivetHeight = 0.025f;
            Vector3 rivetLeft = latchPos + new Vector3(-0.28f, 0.015f, 0f);
            m.Cylinder(rivetLeft, rivetRadius, rivetHeight, 6, DecorPalette.Metal(rust));
            // 3️⃣ rivet at right end of latch
            Vector3 rivetRight = latchPos + new Vector3(0.28f, 0.015f, 0f);
            m.Cylinder(rivetRight, rivetRadius, rivetHeight, 6, DecorPalette.Metal(rust));
            // 4️⃣ top cap – a small rust‑stained plate covering the mailbox lid edge
            // size: 0.30 m × 0.02 m × 0.30 m, positioned 0.78 m above ground (just below the lid)
            Vector3 capPos = new Vector3(0f, 0.78f, 0f);
            Vector3 capSize = new Vector3(0.30f, 0.02f, 0.30f);
            m.Box(capPos, capSize, DecorPalette.Metal(rust));
            m.ResetFrame();
        }

        private static void Crossbuck(RaidMesh m)
        {
            m.Cylinder(Vector3.zero, 0.06f, 3.0f, 8, DecorPalette.Metal(new Color(0.6f, 0.6f, 0.6f)));
            Surf board = DecorPalette.Matte(new Color(0.86f, 0.86f, 0.82f));
            m.Box(new Vector3(0f, 2.6f, 0.06f), new Vector3(1.2f, 0.2f, 0.03f), Quaternion.Euler(0f, 0f, 35f), board);
            m.Box(new Vector3(0f, 2.6f, 0.06f), new Vector3(1.2f, 0.2f, 0.03f), Quaternion.Euler(0f, 0f, -35f), board);
            m.Box(new Vector3(-0.2f, 2.0f, 0.08f), new Vector3(0.22f, 0.22f, 0.12f), DecorPalette.Matte(new Color(0.12f, 0.12f, 0.12f)));
            m.Box(new Vector3(0.2f, 2.0f, 0.08f), new Vector3(0.22f, 0.22f, 0.12f), DecorPalette.Matte(new Color(0.12f, 0.12f, 0.12f)));
            m.Box(new Vector3(-0.2f, 2.0f, 0.141f), new Vector3(0.14f, 0.14f, 0.01f), DecorPalette.Glass(new Color(0.35f, 0.06f, 0.05f)));
            m.Box(new Vector3(0.2f, 2.0f, 0.141f), new Vector3(0.14f, 0.14f, 0.01f), DecorPalette.Glass(new Color(0.35f, 0.06f, 0.05f)));
        }
        #endregion
    }
}
