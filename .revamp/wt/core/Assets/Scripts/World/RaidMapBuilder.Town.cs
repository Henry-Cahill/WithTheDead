using UnityEngine;

namespace CloneZ
{
    public static partial class RaidMapBuilder
    {
        #region Exits
        // Four ways out on the map edges; two or three are open each raid.
        private static void DefineExits(RaidTown town)
        {
            const float d = 63f;
            town.Info.Exits.Add(new RaidExit { Name = "Road Block", Position = new Vector3(0f, 0f, d), Radius = 4.5f, ExtractSeconds = 8f, Hint = "Police barricade, north end of Main St" });
            town.Info.Exits.Add(new RaidExit { Name = "Rail Bridge", Position = new Vector3(d, 0f, 0f), Radius = 4.5f, ExtractSeconds = 8f, Hint = "Rail crossing, east end of Depot Rd" });
            town.Info.Exits.Add(new RaidExit { Name = "Army Checkpoint", Position = new Vector3(0f, 0f, -d), Radius = 4.5f, ExtractSeconds = 8f, Hint = "Guard checkpoint, south end of Main St" });
            town.Info.Exits.Add(new RaidExit { Name = "Boat Dock", Position = new Vector3(-d, 0f, 0f), Radius = 4.5f, ExtractSeconds = 8f, Hint = "River dock, west end of Depot Rd" });

            int open = 2 + town.Rng.Next(2);
            var order = new int[] { 0, 1, 2, 3 };
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = town.Rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (int i = 0; i < order.Length; i++) town.Info.Exits[order[i]].Open = i < open;

            // Nothing solid may stand in (or crowd) an exit zone.
            foreach (RaidExit e in town.Info.Exits) town.Keepout.Add(Around(e.Position, e.Radius + 2f, e.Radius + 2f));
        }
        #endregion

        #region Buildings
        private static void BuildTown(RaidTown town)
        {
            RaidBuilding army = RaidLayouts.ArmyPost(town, new Vector2(22f, -24f), 1);
            RaidBuilding office = RaidLayouts.Office(town, new Vector2(19f, 16f), 0);
            RaidBuilding gas = RaidLayouts.GasStation(town, new Vector2(18f, 46f), 1);
            RaidBuilding depot = RaidLayouts.Warehouse(town, new Vector2(50f, 42f), 0);
            RaidBuilding clinic = RaidLayouts.Clinic(town, new Vector2(-22f, -18f), 2);
            RaidBuilding tent = RaidLayouts.QuarantineTent(town, new Vector2(-24f, -40f), 2);
            RaidBuilding houseA = RaidLayouts.House(town, "House_Elm", new Vector2(-20f, 16f), 0, false, false);
            RaidBuilding houseB = RaidLayouts.House(town, "House_Oak", new Vector2(-38f, 16f), 0, true, false);
            RaidBuilding houseC = RaidLayouts.House(town, "Cottage_North", new Vector2(-19f, 38f), 3, false, true);
            RaidBuilding houseD = RaidLayouts.House(town, "House_East", new Vector2(44f, -16f), 2, town.Rng.NextDouble() < 0.5, false);
            RaidBuilding houseE = RaidLayouts.House(town, "Cottage_West", new Vector2(-50f, -16f), 2, true, true);
            RaidBuilding[] all = { army, office, gas, depot, clinic, tent, houseA, houseB, houseC, houseD, houseE };
            foreach (RaidBuilding b in all)
            {
                town.Buildings.Add(b);
                town.Keepout.Add(Grow(b.Footprint, 1.2f));
                b.ExteriorDoorZones(town.Keepout);
            }

            DressArmyPost(town, army);
            DressOffice(town, office);
            DressGasStation(town, gas);
            DressDepot(town, depot);
            DressClinic(town, clinic, tent);
            DressHouse(town, houseA, -3.0f, 4.5f);
            DressHouse(town, houseB, -3.0f, 4.5f);
            DressHouse(town, houseC, -1.5f, 8.0f);
            DressHouse(town, houseD, -3.0f, 4.5f);
            DressHouse(town, houseE, -1.5f, 5.0f);
        }

        /// <summary>Draws a prop into the outdoor mesh at a world placement, with an optional collider
        /// (<paramref name="size"/>.y = 0 for none), and keeps the spot clear of later props.</summary>
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

        /// <summary>A wrecked car (with its collider) at a world placement, its glovebox and trunk to search
        /// (<see cref="WreckStorage"/>).</summary>
        private static void Wreck(RaidTown town, Vector3 pos, float yaw, DecorProps.CarKind kind, Color paint)
        {
            RaidMesh m = town.Out.Solid(pos);
            m.SetFrame(pos, yaw);
            DecorProps.Car(m, town.Rng, kind, paint, out Vector3 size);
            m.ResetFrame();
            town.PropCollider(pos + Vector3.up * (size.y * 0.5f), new Vector3(size.x, size.y, size.z), yaw);
            float r = Mathf.Max(size.x, size.z) * 0.5f;
            town.Keepout.Add(Around(pos, r, r));
            WreckStorage(town, pos, yaw, kind, size);
        }

        private static readonly Color[] CarPaints =
        {
            new Color(0.48f, 0.10f, 0.09f), new Color(0.18f, 0.26f, 0.42f), new Color(0.62f, 0.58f, 0.46f),
            new Color(0.30f, 0.22f, 0.14f), new Color(0.22f, 0.32f, 0.26f), new Color(0.55f, 0.55f, 0.56f),
        };

        private static Color Paint(RaidTown town) => CarPaints[town.Rng.Next(CarPaints.Length)];

        private static void DressArmyPost(RaidTown town, RaidBuilding b)
        {
            // Front yard on Main St: a gravel lot, sandbag walls either side of the door, a flagpole,
            // and the unit's truck.
            Paved(town, Rect.MinMaxRect(6.5f, -33f, 14.6f, -15f), 0.014f, DecorMaterials.Gravel);
            town.Keepout.Add(Rect.MinMaxRect(6.5f, -33f, 14.6f, -15f));
            Prop(town, new Vector3(12.6f, 0f, -23.4f), 90f, new Vector3(2.4f, 0.7f, 0.5f), true, (m, r) => DecorProps.SandbagWall(m, r, 2.4f, 4));
            Prop(town, new Vector3(12.6f, 0f, -17.6f), 90f, new Vector3(2.4f, 0.7f, 0.5f), true, (m, r) => DecorProps.SandbagWall(m, r, 2.4f, 4));
            Prop(town, new Vector3(9.5f, 0f, -16.5f), 0f, new Vector3(0.4f, 7f, 0.4f), true, (m, r) =>
            {
                m.Box(new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.3f, 0.8f), DecorMaterials.Concrete);
                m.Cylinder(new Vector3(0f, 0.3f, 0f), 0.06f, 7f, 8, DecorPalette.Metal(new Color(0.6f, 0.6f, 0.6f)), Quaternion.identity, 0.6f);
                m.Box(new Vector3(0.55f, 6.2f, 0f), new Vector3(1.1f, 0.65f, 0.02f), Quaternion.Euler(0f, 0f, -8f), DecorPalette.Matte(new Color(0.40f, 0.12f, 0.12f)));
                m.Box(new Vector3(0.3f, 6.37f, 0.012f), new Vector3(0.5f, 0.3f, 0.01f), Quaternion.Euler(0f, 0f, -8f), DecorPalette.Matte(new Color(0.14f, 0.18f, 0.34f)));
            });
            Wreck(town, new Vector3(9.6f, 0f, -29.8f), 4f, DecorProps.CarKind.ArmyTruck, Color.white);
            DumpsterProp(town, new Vector3(31.5f, 0f, -20f), 90f);
            for (int i = 0; i < 3; i++) DecorProps.TrashBag(town.Out.Solid(new Vector3(31.5f, 0f, -22f)), town.Rng, new Vector3(R(town.Rng, 30.8f, 32.4f), 0f, R(town.Rng, -22.6f, -21.4f)));
            TrashBagsProp(town, new Vector3(31.6f, 0f, -22f));
            Vector3 sign = new Vector3(7.6f, 0f, -26.5f);
            Prop(town, sign, 270f, new Vector3(2.2f, 2f, 0.2f), true, (m, r) => DecorProps.PostSign(m, r, "ARMORY", new Color(0.20f, 0.26f, 0.16f), new Color(0.86f, 0.84f, 0.74f), 0.5f, 1.2f));
        }

        private static void DressOffice(RaidTown town, RaidBuilding b)
        {
            // Parking lot to the east with two dead cars, a walk to the front door and a dumpster out back.
            Paved(town, Rect.MinMaxRect(30.5f, 6.5f, 38f, 24f), 0.014f, DecorMaterials.Asphalt);
            town.Keepout.Add(Rect.MinMaxRect(30.5f, 6.5f, 38f, 24f));
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector3(34.2f, 0.018f, 9f + i * 3f);
                town.Out.Flat(p).Flat(p, 7f, 0.1f, 0f, DecorPalette.Matte(LineWhite * 0.75f));
            }
            Wreck(town, new Vector3(34.4f, 0f, 13.5f), 88f, DecorProps.CarKind.Sedan, Paint(town));
            Wreck(town, new Vector3(34.0f, 0f, 19.6f), 95f, DecorProps.CarKind.Wagon, Paint(town));
            Paved(town, Rect.MinMaxRect(24.6f, 6.5f, 26.4f, 8.9f), 0.028f, DecorMaterials.Concrete);
            DumpsterProp(town, new Vector3(13f, 0f, 25f), 0f);
            for (int i = 0; i < 4; i++) DecorProps.TrashBag(town.Out.Solid(new Vector3(13f, 0f, 26.5f)), town.Rng, new Vector3(R(town.Rng, 11.5f, 15f), 0f, R(town.Rng, 26.2f, 27.2f)));
            TrashBagsProp(town, new Vector3(13.2f, 0f, 26.7f));
        }

        private static void DressGasStation(RaidTown town, RaidBuilding b)
        {
            // Forecourt between the store and Main St: concrete apron, two pump islands, the canopy on
            // four pillars (its roof shows only from inside views), a price sign and a dead car.
            Rect apron = Rect.MinMaxRect(6.6f, 38f, 13f, 54f);
            Paved(town, apron, 0.03f, DecorMaterials.Concrete);
            town.Keepout.Add(apron);
            Vector3[] pumps = { new Vector3(9.4f, 0f, 43f), new Vector3(9.4f, 0f, 49f) };
            foreach (Vector3 p in pumps) PumpProp(town, p, 0f);
            float[] px = { 7.4f, 11.4f };
            float[] pz = { 40.5f, 51.5f };
            Surf pillar = DecorPalette.Matte(new Color(0.78f, 0.78f, 0.76f));
            foreach (float x in px)
                foreach (float z in pz)
                {
                    var p = new Vector3(x, 0f, z);
                    Prop(town, p, 0f, new Vector3(0.35f, 4.2f, 0.35f), true, (m, r) =>
                    {
                        m.Box(new Vector3(0f, 2.1f, 0f), new Vector3(0.32f, 4.2f, 0.32f), pillar);
                        m.Box(new Vector3(0f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 0.4f), DecorMaterials.Hazard);
                        m.Cylinder(new Vector3(0f, 1.5f, 0.18f), 0.015f, 0.25f, 12, DecorPalette.Metal(new Color(0.40f, 0.35f, 0.33f)), Quaternion.Euler(0f, (float)(r.NextDouble()*10.0 - 5.0), 0f), 1f);
                        // Tiny rust‑stained bolt detail on the pillar
                        m.Cylinder(new Vector3(0.12f, 2.0f, 0f), 0.01f, 0.015f, 6, DecorPalette.Metal(new Color(0.5f, 0.4f, 0.3f)), Quaternion.identity, 1f);
                        // 1️⃣ nozzle – small metal cylinder protruding forward
                        m.Cylinder(new Vector3(0f, 4.2f + 0.05f, 0.15f), 0.04f, 0.08f, 12,
                            DecorPalette.Metal(new Color(0.45f, 0.44f, 0.42f)),
                            Quaternion.Euler(0f, 0f, 0f), 1f);
                        // 2️⃣ gauge plate – thin rectangular metal piece
                        m.Box(new Vector3(0.07f, 4.0f, 0.07f), new Vector3(0.12f, 0.02f, 0.04f),
                            DecorPalette.Metal(new Color(0.43f, 0.43f, 0.40f)));
                        // 3️⃣ rust streak – long thin dark box on side
                        m.Box(new Vector3(0.12f, 3.5f, 0f), new Vector3(0.02f, 0.30f, 0.005f),
                            DecorPalette.Matte(new Color(0.35f, 0.20f, 0.10f)));
                        // 4️⃣ tiny glow emitter on nozzle tip
                        m.Cylinder(new Vector3(0f, 4.2f + 0.09f, 0.15f), 0.015f, 0.01f, 6,
                            DecorPalette.Glow(new Color(1f, 0.55f, 0.20f)), Quaternion.identity, 1f);
                    });
                }
            // Canopy roof: under the overhead root with a thin collider so the shoulder camera stays below.
            var roof = new RaidMesh();
            Vector3 rc = new Vector3(9.4f, 4.35f, 46f);
            roof.Box(rc, new Vector3(6.4f, 0.3f, 13.4f), DecorPalette.Matte(new Color(0.80f, 0.80f, 0.78f)));
            roof.Box(rc + new Vector3(0f, -0.1f, 0f), new Vector3(6.5f, 0.12f, 13.5f), DecorPalette.Matte(new Color(0.66f, 0.12f, 0.10f)));
            roof.Box(rc + new Vector3(0f, 0.2f, 0f), new Vector3(6.1f, 0.08f, 13.1f), DecorPalette.Matte(DecorProps.SnowWhite));
            for (int i = 0; i < 4; i++)
            {
                bool lit = town.Rng.NextDouble() < 0.4;
                Vector3 lp = new Vector3(9.4f, 4.19f, 41f + i * 3.4f);
                roof.Box(lp, new Vector3(0.5f, 0.02f, 1.0f), lit ? DecorPalette.Glow(new Color(0.95f, 0.92f, 0.82f)) : DecorPalette.Matte(new Color(0.5f, 0.5f, 0.48f)));
                if (lit) town.Light(b.Root, lp - Vector3.up * 0.16f, new Color(0.92f, 0.94f, 1f), 9f, 1.8f, town.Rng.NextDouble() < 0.3, true);
            }
            GameObject canopy = roof.Emit("Canopy", town.Overhead, rc);
            if (canopy != null) CeilingVisibility.Register(canopy.GetComponent<MeshRenderer>());
            town.OverheadCollider(rc, new Vector3(6.4f, 0.3f, 13.4f), Quaternion.identity);

            Wreck(town, new Vector3(7.0f, 0f, 46f), 2f, DecorProps.CarKind.Pickup, Paint(town));
            Prop(town, new Vector3(5.4f, 0f, 36.5f), 270f, new Vector3(2.4f, 3.2f, 0.3f), true, (m, r) => {
                // Base price sign
                DecorProps.PostSign(m, r, "GAS 1.09", new Color(0.86f, 0.84f, 0.78f), new Color(0.62f, 0.10f, 0.08f), 0.5f, 2.6f);
                // Add subtle metal rivets at the four corners of the sign board
                // Rough approximation of sign width based on aspect ~1.5
                float signHeight = 0.5f;
                float signBottom = 2.6f;
                float signAspect = 1.5f;
                float signWidth = signHeight * signAspect;
                float rivetRadius = 0.02f;
                Color rivetColor = new Color(0.6f, 0.6f, 0.6f);
                // Top‑left
                m.Cylinder(new Vector3(-signWidth * 0.5f + rivetRadius, signBottom + signHeight - rivetRadius, 0.01f), rivetRadius, rivetRadius, 6, DecorPalette.Matte(rivetColor));
                // Top‑right
                m.Cylinder(new Vector3(signWidth * 0.5f - rivetRadius, signBottom + signHeight - rivetRadius, 0.01f), rivetRadius, rivetRadius, 6, DecorPalette.Matte(rivetColor));
                // Bottom‑left
                m.Cylinder(new Vector3(-signWidth * 0.5f + rivetRadius, signBottom + rivetRadius, 0.01f), rivetRadius, rivetRadius, 6, DecorPalette.Matte(rivetColor));
                // Bottom‑right
                m.Cylinder(new Vector3(signWidth * 0.5f - rivetRadius, signBottom + rivetRadius, 0.01f), rivetRadius, rivetRadius, 6, DecorPalette.Matte(rivetColor));
            });
            DumpsterProp(town, new Vector3(25f, 0f, 43f), 90f);
        }

        private static void DressDepot(RaidTown town, RaidBuilding b)
        {
            // The depot yard: a wide lot off Depot Rd with a box truck, a dead car and stacked pallets.
            Paved(town, Rect.MinMaxRect(38.5f, 6.5f, 62f, 32.8f), 0.014f, DecorMaterials.Asphalt);
            town.Keepout.Add(Rect.MinMaxRect(38.5f, 6.5f, 62f, 32.8f));
            Wreck(town, new Vector3(56f, 0f, 22f), 0f, DecorProps.CarKind.ArmyTruck, Color.white);
            Wreck(town, new Vector3(42f, 0f, 17f), 170f, DecorProps.CarKind.Pickup, Paint(town));
            DumpsterProp(town, new Vector3(40f, 0f, 31f), 0f);
            Prop(town, new Vector3(60.5f, 0f, 30f), 0f, new Vector3(1.2f, 1.2f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 0f, true));
            Prop(town, new Vector3(60.5f, 0f, 27.8f), 0f, new Vector3(1.2f, 1.2f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, 90f, true));
            Prop(town, new Vector3(48f, 0f, 29f), 0f, new Vector3(2.4f, 1.4f, 2.0f), true, (m, r) => DecorProps.SnowPile(m, r, Vector3.zero, 2.6f, 2.2f, 1.2f, 20f));
        }

        private static void DressClinic(RaidTown town, RaidBuilding clinic, RaidBuilding tent)
        {
            DumpsterProp(town, new Vector3(-33.6f, 0f, -14f), 90f);
            Wreck(town, new Vector3(-10f, 0f, -16f), 182f, DecorProps.CarKind.Sedan, new Color(0.82f, 0.82f, 0.80f));

            // The quarantine site behind the clinic: a toxic cloud centred on the tent, warning barrels and
            // a hazard sign at its edge, sandbags at the flap and a generator.
            Vector3 c = new Vector3(-24f, 0f, -40f);
            ToxicZone.Spawn(town.Scenery, c, 12f);
            Prop(town, new Vector3(-24f, 0f, -26.8f), 0f, new Vector3(0.7f, 2f, 0.3f), true, (m, r) => DecorProps.HazardSign(m, r));
            Vector3[] barrels =
            {
                new Vector3(-28.5f, 0f, -27.6f), new Vector3(-29.2f, 0f, -28.1f), new Vector3(-19.4f, 0f, -27.7f),
                new Vector3(-13.6f, 0f, -36f), new Vector3(-34.2f, 0f, -40f), new Vector3(-34.6f, 0f, -40.7f),
            };
            foreach (Vector3 p in barrels)
                Prop(town, p, 0f, new Vector3(0.6f, 0.9f, 0.6f), true, (m, r) => { FurnitureBuilder.Barrel(m, r, Vector3.zero, new Color(0.66f, 0.54f, 0.12f), true); m.Cylinder(Vector3.zero + new Vector3(0f, 0.44f, 0f), 0.298f, 0.018f, 12, DecorPalette.Metal(new Color(0.55f, 0.45f, 0.20f))); });
            // Caution tape strung between stakes across the approach.
            Surf tape = DecorMaterials.Hazard;
            float[] stakes = { -31f, -27f, -21f, -17f };
            for (int i = 0; i < stakes.Length; i++)
            {
                var p = new Vector3(stakes[i], 0f, -28.5f + (i % 2) * 0.3f);
                RaidMesh m = town.Out.Solid(p);
                m.Box(p + new Vector3(0f, 0.55f, 0f), new Vector3(0.06f, 1.1f, 0.06f), DecorPalette.Matte(new Color(0.55f, 0.45f, 0.30f)));
                if (i < stakes.Length - 1 && i != 1)
                {
                    var q = new Vector3(stakes[i + 1], 0f, -28.5f + ((i + 1) % 2) * 0.3f);
                    m.Rod(p + Vector3.up * 0.95f, q + Vector3.up * 0.9f, 0.06f, tape);
                }
            }
            Prop(town, new Vector3(-28.5f, 0f, -35f), 90f, new Vector3(1.8f, 0.7f, 0.5f), true, (m, r) => DecorProps.SandbagWall(m, r, 1.8f, 4));
            Prop(town, new Vector3(-19.5f, 0f, -35f), 90f, new Vector3(1.8f, 0.7f, 0.5f), true, (m, r) => DecorProps.SandbagWall(m, r, 1.8f, 4));
            GeneratorProp(town, new Vector3(-19.4f, 0f, -42f), 0f);
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector3(R(town.Rng, -32f, -16f), 0f, R(town.Rng, -50f, -30f));
                if (town.Blocked(Around(p, 0.4f, 0.4f))) continue;
                DecorProps.TrashBag(town.Out.Solid(p), town.Rng, p);
            }
            // A caged warning lamp on a post by the flap, burning red-orange.
            Vector3 lampPost = new Vector3(-22.4f, 0f, -34.2f);
            Prop(town, lampPost, 0f, new Vector3(0.2f, 2.4f, 0.2f), true, (m, r) =>
            {
                m.Box(new Vector3(0f, 1.2f, 0f), new Vector3(0.08f, 2.4f, 0.08f), DecorPalette.Metal(new Color(0.3f, 0.32f, 0.28f)));
                m.Box(new Vector3(0f, 2.42f, 0.12f), new Vector3(0.05f, 0.05f, 0.3f), DecorPalette.Metal(new Color(0.3f, 0.32f, 0.28f)));
                m.Box(new Vector3(0f, 2.48f, 0.24f), new Vector3(0.2f, 0.06f, 0.2f), DecorPalette.Matte(new Color(0.15f, 0.15f, 0.14f)));
                // Add subtle weathered metal cap atop the post
                m.Cylinder(new Vector3(0f, 2.45f, 0f), 0.03f, 0.02f, 8, DecorPalette.Metal(new Color(0.45f, 0.45f, 0.44f)));
            });
            town.Light(tent.Root, lampPost + new Vector3(0f, 2.3f, 0.24f), new Color(1f, 0.45f, 0.25f), 6f, 1.4f, true, false);
        }

        /// <summary>A house's front yard, fitted like the starter home's: the front walk out to the sidewalk, the mailbox by it
        /// with the house's last post in it, the welcome mat on the step, the number by the door and the outside tap on the
        /// side wall — all of them working — and maybe a car in the drive.</summary>
        private static void DressHouse(RaidTown town, RaidBuilding b, float doorLx, float yard)
        {
            Vector3 door = b.W(doorLx, 0f, -b.HalfD - 0.1f);
            Vector3 dir = b.Dir(0f, -1f);
            Vector3 end = door + dir * yard;
            bool alongX = Mathf.Abs(dir.x) > 0.5f;
            Rect walk = Rect.MinMaxRect(Mathf.Min(door.x, end.x), Mathf.Min(door.z, end.z), Mathf.Max(door.x, end.x), Mathf.Max(door.z, end.z));
            walk = alongX ? Rect.MinMaxRect(walk.xMin, walk.yMin - 0.6f, walk.xMax, walk.yMax + 0.6f)
                          : Rect.MinMaxRect(walk.xMin - 0.6f, walk.yMin, walk.xMax + 0.6f, walk.yMax);
            Paved(town, walk, 0.028f, DecorMaterials.Concrete);

            Vector3 side = b.Dir(1f, 0f);
            Vector3 box = end - dir * 0.7f + side * 1.3f;
            // Always reserve mailbox space to prevent later props from intersecting, even if the mailbox cannot be placed due to blockage.
            town.Keepout.Add(Around(box, 0.3f, 0.3f));
            if (!town.Blocked(Around(box, 0.3f, 0.3f)))
            {
                b.YardMailbox(box, RaidHomeLiving.Post(town.Rng));
                // Add a small mailbox flag on ~40% of mailboxes
                if (town.Rng.NextDouble() < 0.4)
                {
                    var m = town.Out.Solid(box);
                    // metal pole at the top front of the mailbox
                    m.Cylinder(box + new Vector3(0f, 0.15f, 0f), 0.02f, 0.3f, 6,
                        DecorPalette.Metal(new Color(0.5f, 0.5f, 0.5f)), Quaternion.identity, 1f);
                    // red‑brown cloth flag attached to the pole
                    m.Box(box + new Vector3(0f, 0.30f, 0f), new Vector3(0.15f, 0.1f, 0.02f),
                        DecorPalette.Matte(new Color(0.6f, 0.1f, 0.1f)));
                }
            }
            b.FrontDoormat(doorLx);

        // Colour palette for flower heads on potted plants.
        Color[] flowerCols = {
            new Color(0.90f, 0.20f, 0.20f), // red
            new Color(0.20f, 0.70f, 0.20f), // green
            new Color(0.90f, 0.80f, 0.20f), // yellow
            new Color(0.20f, 0.20f, 0.90f)  // blue
        };

        // Small potted‑plant on the front walk, a bit forward of the door.
        Vector3 plantPos = door + dir * 0.45f;
        town.Keepout.Add(Around(plantPos, 0.18f, 0.18f));
        var potMesh = town.Out.Solid(plantPos);
        // Rust‑tinted metal pot
        potMesh.Cylinder(plantPos + new Vector3(0f, 0.06f, 0f), 0.07f, 0.10f, 8,
            DecorPalette.Metal(new Color(0.55f, 0.45f, 0.38f)), Quaternion.identity, 1f);
        // Dark brown soil inside the pot
        potMesh.Cylinder(plantPos + new Vector3(0f, 0.13f, 0f), 0.065f, 0.02f, 6,
            DecorPalette.Matte(new Color(0.30f, 0.22f, 0.12f)), Quaternion.identity, 1f);
        // Stem
        potMesh.Cylinder(plantPos + new Vector3(0f, 0.16f, 0f), 0.01f, 0.15f, 6,
            DecorPalette.Matte(new Color(0.10f, 0.40f, 0.10f)), Quaternion.identity, 1f);
        // Flower head with random colour
        Color flowerCol = flowerCols[town.Rng.Next(flowerCols.Length)];
        potMesh.Cylinder(plantPos + new Vector3(0f, 0.31f, 0f), 0.03f, 0.02f, 6,
            DecorPalette.Matte(flowerCol), Quaternion.identity, 1f);
// ---- front‑door hardware detail ----
{
    // the door centre is already stored in the local variable 'door'
    var m = town.Out.Solid(door);
    float doorZ = -b.HalfD - 0.1f;
    Quaternion rotation = Quaternion.Euler(0f, b.Yaw(0f), 0f);
    // 1. brass‑coloured knob – cylinder 0.02 m radius, 0.01 m height, centred 0.9 m up, 0.1 m right of centre
    m.Cylinder(b.W(doorLx + 0.10f, 0.90f, doorZ), 0.02f, 0.01f, 12,
        DecorPalette.Metal(new Color(0.70f, 0.55f, 0.35f)), rotation * Quaternion.Euler(0f, 0f, b.Mirror ? -90f : 90f), 1f);
    // 2. small latch plate – box 0.03 m × 0.02 m × 0.01 m, left of knob, a little lower
    m.Box(b.W(doorLx - 0.08f, 0.85f, doorZ), new Vector3(0.03f, 0.02f, 0.01f), rotation,
        DecorPalette.Matte(new Color(0.80f, 0.60f, 0.30f)));
    // 3. wooden door edge trim – thin box 0.02 m thick, runs the full height (1.9 m) and width (0.8 m)
    m.Box(b.W(doorLx, 0.95f, doorZ - 0.02f), new Vector3(0.80f, 1.90f, 0.02f), rotation,
        DecorPalette.Matte(new Color(0.55f, 0.38f, 0.22f)));
    // 4. peephole – tiny cylinder 0.015 m radius, 0.005 m depth, centred 1.1 m up, centred horizontally
    m.Cylinder(b.W(doorLx, 1.10f, doorZ + 0.015f), 0.015f, 0.005f, 8,
        DecorPalette.Metal(new Color(0.30f, 0.30f, 0.30f)), rotation, 1f);
    // 5. rust spot – very small cylinder 0.01 m radius, 0.004 m height, bottom‑right of knob
    m.Cylinder(b.W(doorLx + 0.07f, 0.78f, doorZ), 0.01f, 0.004f, 6,
        DecorPalette.Metal(new Color(0.55f, 0.35f, 0.20f)), rotation, 1f);
    // 6. subtle wear – two thin, dark quads (very flat boxes) to suggest paint chips
    m.Box(b.W(doorLx - 0.12f, 0.62f, doorZ + 0.001f), new Vector3(0.05f, 0.01f, 0.001f), rotation,
        DecorPalette.Matte(new Color(0.30f, 0.25f, 0.22f)));
    m.Box(b.W(doorLx + 0.12f, 0.55f, doorZ + 0.001f), new Vector3(0.04f, 0.008f, 0.001f), rotation,
        DecorPalette.Matte(new Color(0.30f, 0.25f, 0.22f)));

    // 7. left hinge – thin metal cylinder, 0.015 m radius, 0.08 m height, 0.03 m from door centre, 1.0 m up
    float hingeOffsetX = -0.38f; // offset from door centre (approx. left edge)
    float hingeHeight = 1.00f;
    m.Cylinder(b.W(doorLx + hingeOffsetX, hingeHeight, doorZ),
        0.015f, 0.08f, 12,
        DecorPalette.Metal(new Color(0.65f, 0.60f, 0.55f)),
        rotation * Quaternion.Euler(0f, 0f, b.Mirror ? -90f : 90f), 1f);

    // 8. right hinge – same as left but mirrored to the opposite side
    float hingeOffsetX_R = 0.38f;
    m.Cylinder(b.W(doorLx + hingeOffsetX_R, hingeHeight, doorZ),
        0.015f, 0.08f, 12,
        DecorPalette.Metal(new Color(0.65f, 0.60f, 0.55f)),
        rotation * Quaternion.Euler(0f, 0f, b.Mirror ? 90f : -90f), 1f);

    // 9. strike‑plate – thin metal rectangle framing the knob, 0.06 m tall, 0.04 m deep, 0.12 m wide
    Vector3 platePos = b.W(doorLx + 0.10f, 0.90f, doorZ);
    m.Box(platePos + new Vector3(0f, 0f, 0.025f),
        new Vector3(0.12f, 0.06f, 0.04f),
        rotation,
        DecorPalette.Metal(new Color(0.68f, 0.58f, 0.45f)));

    // 10. door‑bell – small square button, 0.04 m side, 0.006 m thick, 0.20 m right of knob, 1.12 m up
    Vector3 bellPos = b.W(doorLx + 0.25f, 1.12f, doorZ);
    m.Box(bellPos + new Vector3(0f, 0f, 0.02f),
        new Vector3(0.04f, 0.006f, 0.04f),
        rotation,
        DecorPalette.Matte(new Color(0.30f, 0.30f, 0.28f)));

    // 11. optional tiny rust spot on right hinge (≈30 % chance)
    if (town.Rng.NextDouble() < 0.30)
    {
        m.Cylinder(b.W(doorLx + hingeOffsetX_R, hingeHeight + 0.04f, doorZ),
            0.008f, 0.015f, 6,
            DecorPalette.Metal(new Color(0.55f, 0.30f, 0.15f)),
            rotation, 1f);
    }
}
            b.HouseNumber(doorLx, (3 + town.Rng.Next(60)).ToString());
            // ---- outdoor‑tap hardware detail ----
            {
                float tapOffset = b.HalfD > 4.75f ? 0.6f : 0.3f;
                Vector3 sideDir = b.Dir(1f, 0f);
                Vector3 tapPos = door + sideDir * (b.HalfW - tapOffset);
                DecorExtensions.AddOutdoorTap(town, tapPos, b.HalfD > 4.75f);
            }

            // A driveway beside the house with a dead car on it (about half the houses).
            if (town.Rng.NextDouble() < 0.6)
            {
                Vector3 car = b.W(b.HalfW + 3.2f, 0f, -b.HalfD + 1.2f);
                Rect cr = alongX ? Around(car, 2.4f, 1.6f) : Around(car, 1.6f, 2.4f);
                if (!town.Blocked(cr) && !OnRoad(cr))
                {
                    Rect drive = Around(car, 1.6f, 1.6f);
                    Vector3 kerb = car + dir * (yard + 1.2f);
                    drive = Rect.MinMaxRect(Mathf.Min(drive.xMin, kerb.x - 1.5f), Mathf.Min(drive.yMin, kerb.z - 1.5f), Mathf.Max(drive.xMax, kerb.x + 1.5f), Mathf.Max(drive.yMax, kerb.z + 1.5f));
                    if (alongX) drive = Rect.MinMaxRect(drive.xMin, car.z - 1.5f, drive.xMax, car.z + 1.5f);
                    else drive = Rect.MinMaxRect(car.x - 1.5f, drive.yMin, car.x + 1.5f, drive.yMax);
                    Paved(town, drive, 0.016f, DecorMaterials.Asphalt);
                    Wreck(town, car, b.Yaw(R(town.Rng, -6f, 6f) + (town.Rng.NextDouble() < 0.5 ? 0f : 180f)),
                        town.Rng.NextDouble() < 0.5 ? DecorProps.CarKind.Sedan : DecorProps.CarKind.Wagon, Paint(town));
                    // Add a small driveway lamp post with a faint orange‑yellow glow beside the car
                    Vector3 lampPos = car + (alongX ? new Vector3(0f, 0f, 1.5f) : new Vector3(1.5f, 0f, 0f));
                    Prop(town, lampPos, 0f, new Vector3(0.15f, 2.0f, 0.15f), true, (m, r) =>
                    {
                        // 1️⃣ metal pole – same as before
                        m.Box(new Vector3(0f, 1.0f, 0f), new Vector3(0.07f, 2.0f, 0.07f),
                        DecorPalette.Metal(new Color(0.35f, 0.33f, 0.30f)));
                        // 2️⃣ base plate at ground level (cylinder, radius 0.09 m, height 0.02 m)
                        m.Cylinder(new Vector3(0f, 0.01f, 0f), 0.09f, 0.02f, 12,
                        DecorPalette.Metal(new Color(0.40f, 0.38f, 0.35f)));
                        // 3️⃣ rusted bolt on the base plate (tiny cylinder, radius 0.012 m, height 0.025 m)
                        m.Cylinder(new Vector3(0.05f, 0.03f, 0f), 0.012f, 0.025f, 6,
                        DecorPalette.Metal(new Color(0.55f, 0.30f, 0.15f)));
                        // 4️⃣ horizontal cross‑bar midway up the pole (thin box, 0.12 m × 0.02 m × 0.02 m)
                        m.Box(new Vector3(0f, 1.6f, 0f), new Vector3(0.12f, 0.02f, 0.02f),
                        DecorPalette.Metal(new Color(0.45f, 0.44f, 0.42f)));
                        // 5️⃣ hanging glass lantern (cylinder, radius 0.04 m, height 0.06 m) – slight orange‑yellow glow
                        m.Cylinder(new Vector3(0f, 1.85f, 0f), 0.04f, 0.06f, 12,
                        DecorPalette.Glow(new Color(1f, 0.55f, 0.20f)));
                        // 6️⃣ subtle rust streak on the pole (flat box, 0.02 m × 0.005 m × 0.5 m)
                        m.Box(new Vector3(0f, 1.2f, -0.04f), new Vector3(0.02f, 0.005f, 0.5f),
                        DecorPalette.Matte(new Color(0.35f, 0.20f, 0.10f)));
                        // 7️⃣ original glass shade (kept, now sits just below the lantern)
                        m.Box(new Vector3(0f, 2.1f, 0f), new Vector3(0.12f, 0.05f, 0.12f),
                        DecorPalette.Matte(new Color(0.15f, 0.14f, 0.13f)));
                    });
                    town.Light(b.Root, lampPos + new Vector3(0f, 2.2f, 0f), new Color(1f, 0.55f, 0.2f), 5f, 1.2f, true, false);
                }
            }
        }
        #endregion
    }
}
