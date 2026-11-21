using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The living room's, bedroom's and store room's furniture (see HomeFurniture): the dining table (a surface to keep
    // things on, and a chair to pull up to it), the dining chairs and the armchair to sit on, the dresser's three
    // drawers and the bedside table's drawer (slid out on FixtureParts), the bookcase to browse and read from, the
    // steel shelving and the cardboard boxes in the store room, and the TV on the dresser and the radio on the kitchen
    // worktop. The standard lamp is left as it is (its switch is the lamp's own).
    public partial class HomeFurniture
    {
        // Handed from DrawLiving to AttachLiving: the drawers' wood, or the TV's screen / the radio's dial (local) and size.
        private Color _livingWood;
        private Vector3 _livingAnchor;
        private Vector2 _livingSize;

        private static readonly string[] LivingDrawerNames = { "Top drawer", "Middle drawer", "Bottom drawer" };
        private static readonly string[] LivingBoxLabels =
        {
            "XMAS DECS \u2014 FRAGILE.", "TAXES 1989\u201391. DO NOT THROW OUT.", "KITCHEN \u2014 MISC.", "BOOKS (HEAVY!)",
            "DAVID'S TROPHIES.", "SUMMER CLOTHES.",
        };

        partial void DrawLiving(RaidMesh m, System.Random rng, ref bool drawn, ref RaidMesh glow, ref Vector3 bulb)
        {
            switch (Kind)
            {
                case HomeFurnitureKind.Dresser:
                    // The carcass only: the drawers are FixtureParts (AttachLiving).
                    _livingWood = FurnitureBuilder.DresserBody(m, rng, FurnitureBuilder.Walnut);
                    drawn = true;
                    break;

                case HomeFurnitureKind.Nightstand:
                {
                    bulb = m.ToMeshSpace(FurnitureBuilder.BedsideTable(m, rng, FurnitureBuilder.Walnut, out _livingWood));
                    glow = new RaidMesh();
                    glow.SetFrame(transform.position, Yaw);
                    glow.Cylinder(new Vector3(0f, 0.836f, -0.04f), 0.158f, 0.208f, 10, DecorPalette.Glow(LampGlow), Quaternion.identity, 0.65f, false);
                    drawn = true;
                    break;
                }

                case HomeFurnitureKind.Armchair:
                    FurnitureBuilder.Armchair(m, rng, Fabrics[rng.Next(Fabrics.Length)]);
                    drawn = true;
                    break;

                case HomeFurnitureKind.Tv:
                    _livingAnchor = FurnitureBuilder.PortableTv(m, rng, out _livingSize);
                    drawn = true;
                    break;

                case HomeFurnitureKind.Radio:
                    _livingAnchor = FurnitureBuilder.KitchenRadio(m, rng, out _livingSize);
                    drawn = true;
                    break;
            }
        }

        partial void AttachLiving()
        {
            switch (Kind)
            {
                case HomeFurnitureKind.Table: LivingAttachTable(); break;
                case HomeFurnitureKind.Chair:
                    SeatFixture.Create(transform, "Dining chair", new Vector3(0f, CloneZConfig.ChairSeatHeight, CloneZConfig.ChairSeatBack), Yaw,
                        CloneZConfig.ChairComfort, "Sit down", "Stand up", false);
                    break;
                case HomeFurnitureKind.Armchair:
                    SeatFixture.Create(transform, "Armchair", new Vector3(0f, CloneZConfig.ArmchairSeatHeight, CloneZConfig.ArmchairSeatBack), Yaw,
                        CloneZConfig.ArmchairComfort, "Sit down in the armchair", "Get up out of the armchair", true);
                    break;
                case HomeFurnitureKind.Dresser: LivingAttachDresser(); break;
                case HomeFurnitureKind.Nightstand: LivingAttachBedside(); break;
                case HomeFurnitureKind.Bookshelf: LivingAttachBookcase(); break;
                case HomeFurnitureKind.Shelf: LivingAttachShelving(); break;
                case HomeFurnitureKind.Boxes: LivingAttachBoxes(); break;
                case HomeFurnitureKind.Tv: LivingAttachTv(); break;
                case HomeFurnitureKind.Radio: LivingAttachRadio(); break;
            }
        }

        private Vector3 LivingAt(Vector3 local) => transform.position + Quaternion.Euler(0f, Yaw, 0f) * local;
        private Vector3 LivingDir(Vector3 local) => Quaternion.Euler(0f, Yaw, 0f) * local;

        // A fresh mesh drawn in this piece's frame (world space).
        private RaidMesh LivingMesh()
        {
            var m = new RaidMesh();
            m.SetFrame(transform.position, Yaw);
            return m;
        }

        // A mesh that lights up while its fixture is on (a TV picture, a radio dial): unshadowed, out of the navmesh, dark at first.
        private Renderer LivingGlow(RaidMesh m, string name)
        {
            m.ResetFrame();
            GameObject go = m.Emit(name, transform, transform.position, ShadowCastingMode.Off);
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;   // the geometry is in world axes
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var r = go.GetComponent<Renderer>();
            r.receiveShadows = false;
            r.enabled = false;
            return r;
        }

        #region Storage
        // The dining table: a surface to keep things on (nothing on it to start with), and a chair to pull up to it.
        private void LivingAttachTable()
        {
            Vector3 p = transform.position;
            StorageFixture.Create(transform, "Dining table", Interactable.KeyFor("table", p), p + Vector3.up * 0.78f, null, transform, Seed + 13,
                new StorageOptions
                {
                    Noun = "table",
                    ContainerName = "Table top",
                    StartEmpty = true,
                    SearchTime = 0f,
                    LookLabel = "Put things on the table",
                    ExtraActions = LivingTableActions,
                });
        }

        private void LivingTableActions(InteractContext ctx, System.Collections.Generic.List<InteractAction> into)
        {
            Vector3 p = transform.position;
            float range = CloneZConfig.TableChairRange;
            bool atTable = ctx.Seat != null && ctx.Seat.IsSeated && SeatFixture.IsAround(ctx.Seat.CurrentSeat, p, range);
            SeatFixture chair = atTable ? null : SeatFixture.NearestAround(p, range, ctx.Position);
            into.Add(new InteractAction("Sit at the table", c => { if (chair != null) chair.SitDown(c); })
                .DisabledIf(chair == null, "There's no chair at the table")
                .DisabledIf(atTable, "You're already sitting at the table"));
            into.Add(new InteractAction("Wipe down the table", c =>
            {
                c.Notify("You wipe the table over with your sleeve and set the chairs square to it. Old habits.");
                LivingFixtures.Calm(c, 1f);
            }).Timed(2f, "Wiping the table", HumanoidActivity.Use));
        }

        // The dresser's three drawers, each sliding out along the front, each its own storage.
        private void LivingAttachDresser()
        {
            Vector3 p = transform.position;
            Color wood = _livingWood.a > 0f ? _livingWood : FurnitureBuilder.Walnut;
            Vector3 travel = LivingDir(Vector3.forward * CloneZConfig.DresserDrawerTravel);
            for (int row = 0; row < 3; row++)
            {
                RaidMesh dm = LivingMesh();
                FurnitureBuilder.DresserDrawer(dm, row, wood);
                float y = FurnitureBuilder.DresserDrawerY(row);
                FixturePart drawer = FixturePart.Slide(transform, "Drawer_" + row, dm, LivingAt(new Vector3(0f, y, 0.205f)), travel);
                string name = LivingDrawerNames[row];
                StorageFixture.Create(transform, name, Interactable.KeyFor("dresser", p, row), LivingAt(new Vector3(0f, y, 0.24f)),
                    new[] { drawer }, transform, Seed + 101 * (row + 1), new StorageOptions
                    {
                        Noun = name.ToLowerInvariant(),
                        ContainerName = "Dresser \u2014 " + name.ToLowerInvariant(),
                        LootType = ContainerType.Wardrobe,
                        EmptyChance = CloneZConfig.DresserDrawerEmptyChance,
                        MaxStacks = CloneZConfig.DresserDrawerMaxStacks,
                    });
            }
        }

        // The bedside table's drawer (its lamp is drawn with the body and switched by the lamp's own fixture).
        private void LivingAttachBedside()
        {
            Vector3 p = transform.position;
            RaidMesh dm = LivingMesh();
            FurnitureBuilder.BedsideDrawer(dm, _livingWood.a > 0f ? _livingWood : FurnitureBuilder.Walnut);
            FixturePart drawer = FixturePart.Slide(transform, "Drawer", dm, LivingAt(new Vector3(0f, 0.4f, 0.185f)),
                LivingDir(Vector3.forward * CloneZConfig.BedsideDrawerTravel));
            StorageFixture.Create(transform, "Bedside drawer", Interactable.KeyFor("bedside", p), LivingAt(new Vector3(0f, 0.4f, 0.22f)),
                new[] { drawer }, transform, Seed + 37, new StorageOptions
                {
                    Noun = "bedside drawer",
                    ContainerName = "Bedside drawer",
                    LootType = ContainerType.Desk,
                    EmptyChance = CloneZConfig.BedsideDrawerEmptyChance,
                    MaxStacks = CloneZConfig.BedsideDrawerMaxStacks,
                });
        }

        // The bookcase: open shelves to browse, and something to read.
        private void LivingAttachBookcase()
        {
            Vector3 p = transform.position;
            StorageFixture.Create(transform, "Bookcase", Interactable.KeyFor("bookcase", p), LivingAt(new Vector3(0f, 1.2f, 0.17f)), null, transform, Seed + 53,
                new StorageOptions
                {
                    Noun = "bookcase",
                    LookLabel = "Browse the shelves",
                    LootType = ContainerType.Shelf,
                    EmptyChance = CloneZConfig.BookcaseEmptyChance,
                    MaxStacks = CloneZConfig.BookcaseMaxStacks,
                    ExtraActions = LivingReading.AddActions,
                });
        }

        private void LivingAttachShelving()
        {
            Vector3 p = transform.position;
            StorageFixture.Create(transform, "Steel shelving", Interactable.KeyFor("shelving", p), LivingAt(new Vector3(0f, 1.05f, 0.24f)), null, transform, Seed + 71,
                new StorageOptions
                {
                    Noun = "shelving",
                    LookLabel = "Search the shelves",
                    LootType = ContainerType.Shelf,
                    EmptyChance = CloneZConfig.ShelvingEmptyChance,
                    MaxStacks = CloneZConfig.ShelvingMaxStacks,
                });
        }

        private void LivingAttachBoxes()
        {
            Vector3 p = transform.position;
            StorageFixture.Create(transform, "Cardboard boxes", Interactable.KeyFor("boxes", p), LivingAt(new Vector3(0f, 0.55f, 0.24f)), null, transform, Seed + 89,
                new StorageOptions
                {
                    Noun = "boxes",
                    LookLabel = "Rummage through the boxes",
                    LootType = ContainerType.Crate,
                    EmptyChance = CloneZConfig.BoxesEmptyChance,
                    MaxStacks = CloneZConfig.BoxesMaxStacks,
                    ExtraActions = LivingBoxActions,
                });
        }

        private void LivingBoxActions(InteractContext ctx, System.Collections.Generic.List<InteractAction> into)
        {
            int seed = Seed;
            into.Add(new InteractAction("Read the labels", c =>
            {
                int a = (seed & 0x7fff) % LivingBoxLabels.Length, b = (a + 1 + ((seed >> 4) & 1)) % LivingBoxLabels.Length;
                c.Notify("Marker pen on parcel tape: \u201C" + LivingBoxLabels[a] + "\u201D \u201C" + LivingBoxLabels[b] + "\u201D Somebody's life, taped shut.");
            }));
        }
        #endregion

        #region The TV and the radio
        // The portable TV on the dresser: its body is drawn already; here the lit picture, the glow on the room and the fixture.
        private void LivingAttachTv()
        {
            Vector3 facing = LivingDir(Vector3.forward);
            Vector3 screen = LivingAt(_livingAnchor);
            RaidMesh pm = LivingMesh();
            FurnitureBuilder.TvScreenGlow(pm, _livingAnchor, _livingSize);
            Renderer picture = LivingGlow(pm, "TvPicture");
            RoomLight light = RoomLight.Spawn(transform, screen + facing * 0.35f, TvFixture.LightColour,
                CloneZConfig.TvLightRange, CloneZConfig.TvLightIntensity, false, false);
            light.ShowBulb = false;
            light.On = false;
            TvFixture.Create(transform, screen + facing * 0.03f, facing, picture, light);
        }

        // The kitchen radio on the worktop: the dial's glow and the fixture.
        private void LivingAttachRadio()
        {
            RaidMesh dm = LivingMesh();
            FurnitureBuilder.RadioDialGlow(dm, _livingAnchor, _livingSize);
            Renderer dial = LivingGlow(dm, "RadioDial");
            RadioFixture.Create(transform, LivingAt(new Vector3(0.03f, 0.1f, 0.09f)), LivingDir(Vector3.forward), dial);
        }
        #endregion
    }
}
