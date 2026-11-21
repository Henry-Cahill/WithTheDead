using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The raid town's houses (RaidLayoutsHomes.cs) fitted out the way the starter home is: the home's own sink unit, gas
    /// range and bed, wall cupboards over the worktop, a wardrobe, the living room's sofa, coffee table and TV, the ranch's
    /// bath, the laundry's washing machines and a desk chair, and the bathroom's mirror cabinet and towel rail — each a
    /// body of its own (<see cref="FixtureBody"/>) with its moving parts and fixtures, or a wall fitting hung under its wall
    /// segment (<see cref="Later"/>, <see cref="WallObjectAt"/>) — and, outside, the front yard's mailbox, doormat, house
    /// number and outside tap. Every piece claims its floor in its room (and warns when it doesn't fit, as the rest of the
    /// furnishing does).
    /// </summary>
    internal sealed partial class RaidBuilding
    {
        private const float WallCupboardDoorAngle = 100f;
        private const float WallCupboardInnerDoorAngle = 82f;   // doors meeting another bay's at a partition stop short of it
        private const float WardrobeDoorAngle = 100f;
        private const float BathCabinetW = 0.50f, BathCabinetH = 0.60f, BathCabinetD = 0.14f;
        private const float BathCabinetDoorAngle = 100f;
        private const float BathRailLength = 0.48f, BathTowelWidth = 0.40f;
        private const float BathTowelDamp = 0.72f;   // how much darker the towel hangs while it's damp

        private static readonly Color[] HouseTowels =
        {
            new Color(0.46f, 0.60f, 0.74f), new Color(0.86f, 0.60f, 0.50f), new Color(0.56f, 0.68f, 0.52f), new Color(0.88f, 0.84f, 0.72f),
            new Color(0.80f, 0.74f, 0.42f),
        };

        #region Placement
        /// <summary>A placement <paramref name="x"/> across and <paramref name="z"/> out from <paramref name="p"/>'s middle in that
        /// piece's own frame (pieces are turned, never mirrored), turned a further <paramref name="yaw"/> (local degrees) — the
        /// radio standing on a counter, the TV on a dresser.</summary>
        public Place OffsetPlace(Place p, float x, float z, float yaw = 0f)
        {
            if (Mirror) x = -x;
            float r = p.Yaw * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Place(p.X + x * c + z * s, p.Z - x * s + z * c, p.Yaw + yaw);
        }

        // Marks the floor under a fitting that brings its own body (the sink unit, the stove, the bed) taken in its room.
        private void TakeFloor(RaidRoom room, Place p, Vector3 size, string what)
        {
            if (room == null) return;
            Rect fp = FootRect(At(p), Yaw(p.Yaw), size);
            CheckClear(room, fp, what);
            room.Furniture.Add(fp);
        }
        #endregion

        #region Kitchen
        /// <summary>The starter home's sink unit — a <see cref="WaterSource"/> tap with its <see cref="KitchenSink"/> (the tap, the
        /// plug, the washing-up, the cupboard under the bowl) — against a wall at a placement (1.22 × 1.0 × 0.63 m, front +Z).</summary>
        public WaterSource HouseSink(RaidRoom room, Place p)
        {
            TakeFloor(room, p, new Vector3(1.22f, 1.0f, 0.63f), "sink");
            return WaterSource.Spawn(Root, At(p), WaterSourceKind.Tap, Yaw(p.Yaw));
        }

        /// <summary>The starter home's gas range — a <see cref="CookingStation"/> with its <see cref="KitchenStove"/> (four burners,
        /// the oven, cooking) — at a placement (0.76 × 1.08 × 0.62 m, front +Z).</summary>
        public CookingStation HouseStove(RaidRoom room, Place p)
        {
            TakeFloor(room, p, new Vector3(0.76f, 1.08f, 0.62f), "stove");
            return CookingStation.Spawn(Root, At(p), Yaw(p.Yaw));
        }

        /// <summary>Wall cupboards above a counter on a room wall (<paramref name="t"/>: the local coordinate of their middle
        /// along it), <paramref name="len"/> long, painted <paramref name="cabinet"/> — made like the starter home's: the carcass,
        /// shelf and crockery drawn into the wall segment's mesh (they vanish with it when the camera cuts the wall away), and
        /// for each bay of about 1.2 m two doors meeting in the middle, hung under the segment as moving parts, with a cupboard's
        /// worth of storage behind them (a few things at most).</summary>
        public void UpperCabinets(RaidRoom room, char side, float t, float len, Color cabinet)
        {
            Vector3 at = WallFace(room, side, t, 0f, out Vector3 n);
            float yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            var tone = new FurnitureBuilder.KitchenTone(cabinet, FurnitureBuilder.HomeWorktop);
            RaidMesh m = WallMeshAt(at + Vector3.up * 1.8f);
            m.SetFrame(at, yaw);
            FurnitureBuilder.RaidWallCupboardBody(m, Rng, tone, len);
            m.ResetFrame();
            // Keep the switches, vines and grime placed at Finish off the stretch of wall the cupboards cover.
            KeepWallClear(at + Vector3.up * ((FurnitureBuilder.WallCupY0 + FurnitureBuilder.WallCupY1) * 0.5f), n,
                len * 0.5f + 0.04f, (FurnitureBuilder.WallCupY1 - FurnitureBuilder.WallCupY0) * 0.5f + 0.04f);
            int seed = Rng.Next();
            Later(() => HangWallCupboards(at, yaw, tone, len, seed));
        }

        // The wall cupboards' doors and storage, under the wall segment they hang on.
        private void HangWallCupboards(Vector3 at, float yaw, FurnitureBuilder.KitchenTone tone, float len, int seed)
        {
            Transform wall = WallObjectAt(at + Vector3.up * 1.8f);
            var kf = new KitchenFrame(at, yaw);
            int bays = FurnitureBuilder.WallCupBays(len);
            float bay = len / bays, front = FurnitureBuilder.WallCupDepth + 0.02f;
            const float y0 = FurnitureBuilder.WallCupY0, y1 = FurnitureBuilder.WallCupY1;
            for (int i = 0; i < bays; i++)
            {
                float x0 = -len * 0.5f + bay * i, x1 = x0 + bay, mid = (x0 + x1) * 0.5f;
                float lx0 = x0 + 0.002f, lx1 = mid - 0.003f, rx0 = mid + 0.003f, rx1 = x1 - 0.002f;
                FixturePart left = KitchenParts.Door(wall, "WallCupboardDoorL", kf, d => FurnitureBuilder.RaidWallCupboardDoor(d, tone, lx0, lx1, true),
                    new Vector3(lx0, y0, front), Vector3.right, Vector3.forward, i == 0 ? WallCupboardDoorAngle : WallCupboardInnerDoorAngle);
                FixturePart right = KitchenParts.Door(wall, "WallCupboardDoorR", kf, d => FurnitureBuilder.RaidWallCupboardDoor(d, tone, rx0, rx1, false),
                    new Vector3(rx1, y0, front), Vector3.left, Vector3.forward, i == bays - 1 ? WallCupboardDoorAngle : WallCupboardInnerDoorAngle);
                StorageFixture.Create(wall, "Wall cupboard", Interactable.KeyFor("wallcupboard", kf.P(mid, 0f, 0f)), kf.P(mid, (y0 + y1) * 0.5f, front + 0.01f),
                    new[] { left, right }, null, seed + i * 7919, SmallStock(ContainerType.Pantry, "wall cupboard"));
            }
        }
        #endregion

        #region Bedrooms
        /// <summary>The starter home's double bed (a <see cref="SleepSpot"/> with its <see cref="BedFixture"/>: sleep, lie down and
        /// rest, sit on the edge, make it) at a placement, made up with a <paramref name="quilt"/> of the house's own, its head
        /// toward the placement's back — against the wall for a placement made with <see cref="N"/> and the like; its
        /// 1.4 × 2.0 m marked taken.</summary>
        public SleepSpot HouseBed(RaidRoom room, Place p, Color quilt)
        {
            TakeFloor(room, p, new Vector3(SleepSpot.Width, 1.1f, SleepSpot.Length), "bed");
            return SleepSpot.Spawn(Root, At(p), Yaw(p.Yaw), quilt);
        }

        /// <summary>A two-door wooden wardrobe (1.0 × 1.95 × 0.6 m, front +Z): a body of its own with the doors hung on its outer
        /// edges as moving parts and the clothes rail behind them, stocked as one of the raid's main containers. Returns its storage.</summary>
        public StorageFixture Wardrobe(RaidRoom room, Place p, Color wood)
        {
            Color doors = wood;
            Transform t = FixtureBody(room, p, new Vector3(1.0f, 1.95f, 0.6f), true, (m, r) => doors = FurnitureBuilder.WardrobeBody(m, r, wood), "Wardrobe");
            KitchenFrame f = FrameOf(p);
            const float y0 = FurnitureBuilder.WardrobeDoorY0, front = FurnitureBuilder.WardrobeFront, h = FurnitureBuilder.WardrobeHalf;
            FixturePart left = KitchenParts.Door(t, "WardrobeDoorL", f, d => FurnitureBuilder.WardrobeDoor(d, doors, true),
                new Vector3(-h + 0.002f, y0, front), Vector3.right, Vector3.forward, WardrobeDoorAngle);
            FixturePart right = KitchenParts.Door(t, "WardrobeDoorR", f, d => FurnitureBuilder.WardrobeDoor(d, doors, false),
                new Vector3(h - 0.002f, y0, front), Vector3.left, Vector3.forward, WardrobeDoorAngle);
            StorageOptions o = MainStock(ContainerType.Wardrobe, "wardrobe");
            o.ContainerName = "Wardrobe";
            return StorageFixture.Create(t, "Wardrobe", Interactable.KeyFor("wardrobe", t.position), f.P(0f, 1.1f, front + 0.04f),
                new[] { left, right }, t, Rng.Next(), o);
        }

        /// <summary>Switches on the lamp of one of the home's own pieces (a bedside table, a standard lamp) with
        /// <paramref name="chance"/> — left burning when the house was emptied. The lamp keeps its own switch.</summary>
        public void LeaveLampOn(HomeFurniture piece, double chance)
        {
            if (piece == null || Rng.NextDouble() >= chance) return;
            HomeLamp lamp = piece.GetComponentInChildren<HomeLamp>();
            if (lamp != null) lamp.SetSwitchedOn(true);
        }
        #endregion

        #region Living rooms and studies
        /// <summary>A fabric sofa (<paramref name="width"/> × 0.85 × 0.88 m, front +Z) as a body of its own: a place to sit at
        /// either end of a full-length one (one in the middle of a love seat), where the survivor can sink back, and the gap down
        /// the back of the cushions to search — seldom with anything in it. Returns its body.</summary>
        public Transform HouseSofa(RaidRoom room, Place p, float width, Color fabric)
        {
            Transform t = FixtureBody(room, p, new Vector3(width, 0.85f, 0.88f), true, (m, r) => FurnitureBuilder.Sofa(m, r, width, fabric), "Sofa");
            float yaw = Yaw(p.Yaw);
            int seats = width >= 1.6f ? 2 : 1;
            for (int i = 0; i < seats; i++)
            {
                float x = seats == 1 ? 0f : (i == 0 ? -1f : 1f) * CloneZConfig.SofaSeatSpread;
                SeatFixture.Create(t, "Sofa", new Vector3(x, CloneZConfig.SofaSeatHeight, CloneZConfig.SofaSeatBack), yaw, CloneZConfig.SofaComfort,
                    "Sit down on the sofa", "Get up off the sofa", true, "Sink back into the sofa");
            }
            StorageFixture.Create(t, "Sofa cushions", Interactable.KeyFor("sofacushions", t.position), At(p, 0f, 0.52f, -0.16f), null, t, Rng.Next(),
                new StorageOptions
                {
                    Noun = "sofa cushions",
                    ContainerName = "Down the back of the sofa",
                    LootType = ContainerType.Desk,
                    EmptyChance = CloneZConfig.SofaCushionsEmptyChance,
                    MaxStacks = CloneZConfig.SofaCushionsMaxStacks,
                    RaidBoost = false,
                    LookLabel = "Search down the back of the cushions",
                    ExtraActions = RaidHomeLiving.AddCushionActions,
                });
            return t;
        }

        /// <summary>A low coffee table (<paramref name="w"/> × 0.45 × <paramref name="d"/> m) with its magazines and ashtray: a
        /// surface to put things down on (nothing on it to start with), and a magazine to flick through. Returns its body.</summary>
        public Transform HouseCoffeeTable(RaidRoom room, Place p, float w, float d)
        {
            Transform t = FixtureBody(room, p, new Vector3(w, 0.45f, d), true, (m, r) => FurnitureBuilder.CoffeeTable(m, r, w, d), "Coffee table");
            StorageFixture.Create(t, "Coffee table", Interactable.KeyFor("coffeetable", t.position), At(p, 0f, 0.47f, 0f), null, t, Rng.Next(),
                new StorageOptions
                {
                    Noun = "coffee table",
                    ContainerName = "Coffee table",
                    StartEmpty = true,
                    SearchTime = 0f,
                    LookLabel = "Put things on the coffee table",
                    ExtraActions = RaidHomeLiving.AddCoffeeTableActions,
                });
            return t;
        }

        /// <summary>The living room's TV: a console set with rabbit ears on a low stand with the VCR (1.0 × 1.1 × 0.45 m, front
        /// +Z), working like the starter home's portable (<see cref="TvFixture"/>: three channels, three volumes, its picture
        /// glowing on the room) and watchable from the sofa across the room.</summary>
        public TvFixture HouseTv(RaidRoom room, Place p)
        {
            Vector3 centre = Vector3.zero;
            Vector2 size = Vector2.zero;
            Transform t = FixtureBody(room, p, new Vector3(1.0f, 1.1f, 0.45f), true, (m, r) =>
            {
                FurnitureBuilder.TvStandBody(m, r);
                centre = FurnitureBuilder.ConsoleTv(m, r, out size);
            }, "TV");
            Vector3 facing = DirOf(p, Vector3.forward);
            Vector3 screen = At(p, centre.x, centre.y, centre.z);
            var pm = new RaidMesh();
            pm.SetFrame(At(p), Yaw(p.Yaw));
            FurnitureBuilder.TvScreenGlow(pm, centre, size);
            pm.ResetFrame();
            Renderer picture = GlowPiece(pm, "TvPicture", t);
            RoomLight light = RoomLight.Spawn(t, screen + facing * 0.35f, TvFixture.LightColour, CloneZConfig.TvLightRange,
                CloneZConfig.TvLightIntensity, false, false);
            light.ShowBulb = false;
            light.On = false;
            return TvFixture.Create(t, screen + facing * 0.03f, facing, picture, light, CloneZConfig.RaidTvReach, "TV", true);
        }

        // A mesh that lights up while its fixture is on (a TV's picture): unshadowed, out of the navmesh, dark at first.
        private static Renderer GlowPiece(RaidMesh m, string name, Transform parent)
        {
            GameObject go = m.Emit(name, parent, parent.position, ShadowCastingMode.Off);
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;   // the geometry is in world axes
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var r = go.GetComponent<Renderer>();
            r.receiveShadows = false;
            r.enabled = false;
            return r;
        }

        /// <summary>A swivel office chair (0.5 × 0.95 × 0.5 m, front +Z) to sit in at a desk. Returns its body.</summary>
        public Transform HouseDeskChair(RaidRoom room, Place p, Color fabric)
        {
            Transform t = FixtureBody(room, p, new Vector3(0.5f, 0.95f, 0.5f), true, (m, r) => FurnitureBuilder.OfficeChair(m, r, fabric), "Desk chair");
            SeatFixture.Create(t, "Desk chair", new Vector3(0f, CloneZConfig.OfficeChairSeatHeight, 0.02f), Yaw(p.Yaw), CloneZConfig.OfficeChairComfort,
                "Sit down", "Stand up", false);
            return t;
        }
        #endregion

        #region Bathrooms and the laundry
        /// <summary>The ranch's bath (1.6 × 0.56 × 0.75 m, its back to the wall, front +Z) — a <see cref="RaidHomeBath"/>: taps, a
        /// plug, a bathful of water to drink, wash in or soak in. <paramref name="tapsPlusX"/> puts the taps and the spout at the
        /// piece's own +X end, else its -X end.</summary>
        public RaidHomeBath HouseBath(RaidRoom room, Place p, bool tapsPlusX)
        {
            float end = tapsPlusX ? 1f : -1f;
            var size = new Vector3(FurnitureBuilder.BathLength, FurnitureBuilder.BathHeight, FurnitureBuilder.BathDepth);
            Transform t = FixtureBody(room, p, size, true, (m, r) => FurnitureBuilder.BathTub(m, r, end), "Bath");
            KitchenFrame f = FrameOf(p);
            RaidHomeBath bath = RaidHomeBath.Create(t, f, end);
            FixturePart hot = TapHead(t, "HotTap", f, FurnitureBuilder.BathTapHead(end, true), true);
            FixturePart cold = TapHead(t, "ColdTap", f, FurnitureBuilder.BathTapHead(end, false), false);
            Renderer stream = KitchenParts.PieceInFrame(t, "BathStream", f, FurnitureBuilder.BathSpout(end), d => FurnitureBuilder.BathStream(d, end));
            Renderer water = KitchenParts.PieceInFrame(t, "BathWater", f, new Vector3(0f, FurnitureBuilder.BathWaterTopY, 0f), FurnitureBuilder.BathWaterSheet);
            Renderer plugIn = KitchenParts.PieceInFrame(t, "PlugIn", f, FurnitureBuilder.BathDrain(end), d => FurnitureBuilder.BathPlug(d, end, true));
            Renderer plugOut = KitchenParts.PieceInFrame(t, "PlugOut", f, FurnitureBuilder.BathDrain(end), d => FurnitureBuilder.BathPlug(d, end, false));
            bath.SetParts(hot, cold, stream, water, plugIn, plugOut);
            return bath;
        }

        // A tap's cross-head drawn round its own middle, turning about the vertical a quarter round to run (hot and cold opposite ways).
        private static FixturePart TapHead(Transform parent, string name, in KitchenFrame f, Vector3 local, bool hot)
        {
            Vector3 at = f.P(local);
            var m = new RaidMesh();
            m.SetFrame(at, f.Yaw);
            FurnitureBuilder.BathTapCross(m, hot);
            m.ResetFrame();
            return FixturePart.Hinge(parent, name, m, at, Vector3.up, hot ? CloneZConfig.BathTapAngle : -CloneZConfig.BathTapAngle,
                ShadowCastingMode.Off).WithSpeed(0.3f);
        }

        /// <summary>The starter home's mirror cabinet hung on a room wall over the basin (<paramref name="t"/> along the wall, its
        /// middle <paramref name="y"/> up): the carcass drawn into the wall segment's mesh, the mirrored door hinged to swing open,
        /// and its storage (a <see cref="MirrorCabinet"/>, with a look in the mirror) stocked as one of the raid's main containers.</summary>
        public void HangBathCabinet(RaidRoom room, char side, float t, float y)
        {
            Vector3 back = WallFace(room, side, t, y, out Vector3 n);
            FurnitureBuilder.MirrorCabinetBody(WallMeshAt(back), back, n, BathCabinetW, BathCabinetH, BathCabinetD);
            KeepWallClear(back, n, BathCabinetW * 0.5f + 0.04f, BathCabinetH * 0.5f + 0.04f);
            int seed = Rng.Next();
            Later(() =>
            {
                Transform wall = WallObjectAt(back);
                Vector3 front = back + n * BathCabinetD;
                Vector3 right = Vector3.Cross(n, Vector3.up).normalized;
                var door = new RaidMesh();
                FurnitureBuilder.MirrorCabinetDoor(door, front, n, right, BathCabinetW, BathCabinetH);
                FixturePart part = FixturePart.Hinge(wall, "MirrorDoor", door, front + right * (BathCabinetW * 0.5f), Vector3.Cross(n, right), BathCabinetDoorAngle);
                MirrorCabinet.Create(wall, front + n * 0.03f, part, seed, MainStock(ContainerType.MedCabinet, "bathroom cabinet"));
            });
        }

        /// <summary>The starter home's chrome towel rail on a room wall (<paramref name="t"/> along it, <paramref name="y"/> up) with
        /// a towel over it (a <see cref="TowelRail"/>: dry yourself off after a soak).</summary>
        public void HangTowelRail(RaidRoom room, char side, float t, float y)
        {
            Vector3 at = WallFace(room, side, t, y, out Vector3 n);
            Vector3 along = Vector3.Cross(Vector3.up, n).normalized;
            Vector3 bar = FurnitureBuilder.TowelRailBar(WallMeshAt(at), at, n, along, BathRailLength);
            KeepWallClear(at - Vector3.up * 0.15f, n, BathRailLength * 0.5f + 0.05f, 0.3f);   // the rail and the towel hanging off it
            Color dry = HouseTowels[Rng.Next(HouseTowels.Length)];
            var damp = new Color(dry.r * BathTowelDamp, dry.g * BathTowelDamp, dry.b * BathTowelDamp);
            Later(() =>
            {
                Transform wall = WallObjectAt(at);
                TowelRail rail = TowelRail.Create(wall, bar + n * 0.07f - Vector3.up * 0.12f, at, n);
                Transform pivot = new GameObject("Towel").transform;
                pivot.SetParent(rail.transform, false);
                pivot.SetPositionAndRotation(bar, Quaternion.identity);
                var dryMesh = new RaidMesh();
                FurnitureBuilder.TowelOnRail(dryMesh, bar, n, along, BathTowelWidth, dry);
                var dampMesh = new RaidMesh();
                FurnitureBuilder.TowelOnRail(dampMesh, bar, n, along, BathTowelWidth, damp);
                rail.SetTowel(pivot,
                    BathroomFixtures.Emit(dryMesh, "TowelDry", pivot, bar, true, true),
                    BathroomFixtures.Emit(dampMesh, "TowelDamp", pivot, bar, true, false));
            });
        }

        /// <summary>A front-loading washing machine (0.68 × 0.9 × 0.65 m, front +Z): its porthole door opens on the drum (somebody's
        /// last load: a lean storage), and with the power and the water on it runs a wash by itself — a long, noisy cycle the
        /// dead can hear (<see cref="RaidHomeWasher"/>). Returns its controls.</summary>
        public RaidHomeWasher HouseWasher(RaidRoom room, Place p)
        {
            Transform t = FixtureBody(room, p, new Vector3(0.68f, 0.9f, 0.65f), true, (m, r) => FurnitureBuilder.WasherBody(m, r), "Washing machine");
            KitchenFrame f = FrameOf(p);
            Vector3 hole = FurnitureBuilder.WasherPorthole;
            FixturePart door = KitchenParts.Door(t, "Porthole", f, FurnitureBuilder.WasherDoor,
                new Vector3(hole.x - FurnitureBuilder.WasherRing - 0.025f, hole.y, hole.z + 0.018f), Vector3.right, Vector3.forward,
                CloneZConfig.WasherDoorAngle);
            StorageFixture drum = StorageFixture.Create(t, "Washing machine", Interactable.KeyFor("washerdrum", t.position), f.P(hole.x, hole.y, hole.z + 0.06f),
                new[] { door }, t, Rng.Next(), new StorageOptions
                {
                    Noun = "washing machine",
                    ContainerName = "Washing machine drum",
                    LootType = ContainerType.Wardrobe,
                    EmptyChance = CloneZConfig.WasherEmptyChance,
                    MaxStacks = CloneZConfig.WasherMaxStacks,
                    RaidBoost = false,
                    LookLabel = "Feel round inside the drum",
                    Noise = CloneZConfig.WasherDoorNoise,
                });
            int seed = Rng.Next();
            Renderer suds = KitchenParts.PieceAt(t, "Suds", f, hole, d => FurnitureBuilder.WasherSuds(d, new System.Random(seed)));
            return RaidHomeWasher.Create(t, f, drum, suds);
        }
        #endregion

        #region The front yard
        // The house's front door (on the local -Z wall at local x doorLx) faces the street: its frame for the yard's fittings is
        // the starter home's own, turned to it — the street toward -Z, the door toward +Z.
        private float YardYaw
        {
            get
            {
                Vector3 street = Dir(0f, -1f);
                return Mathf.Atan2(-street.x, -street.z) * Mathf.Rad2Deg;
            }
        }

        /// <summary>The coir welcome mat on the step outside the front door (at local x <paramref name="doorLx"/>): wipe your feet,
        /// or lift it to look underneath (a <see cref="DoormatFixture"/>).</summary>
        public void FrontDoormat(float doorLx)
        {
            float yaw = YardYaw;
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            const float d = FurnitureBuilder.DoormatDepth;
            Vector3 at = W(doorLx, 0.04f, -HalfD - Half - 0.1f - d * 0.5f);
            RaidMesh under = Town.Out.Flat(at);
            under.SetFrame(at, yaw);
            FurnitureBuilder.UnderDoormat(under);
            under.ResetFrame();
            var mat = new RaidMesh();
            mat.SetFrame(at, yaw);
            FurnitureBuilder.Doormat(mat, Rng);
            mat.ResetFrame();
            DoormatFixture fixture = DoormatFixture.Create(Root, at + q * new Vector3(0f, 0.03f, 0.04f - d * 0.5f));
            // Lifting the near edge turns the mat up about its far edge, toward the door.
            FixturePart part = FixturePart.Hinge(fixture.transform, "Mat", mat, at + q * new Vector3(0f, 0f, d * 0.5f), q * Vector3.right, 65f,
                ShadowCastingMode.Off).WithSpeed(0.25f);
            fixture.SetMat(part);
        }

        /// <summary>A rural mailbox on its post at <paramref name="pos"/> (world, by the end of the front walk), its door to the
        /// street: the house's own <paramref name="post"/> to read (a <see cref="MailboxFixture"/>), and the red flag.</summary>
        public void YardMailbox(Vector3 pos, string[][] post)
        {
            float yaw = YardYaw;
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            RaidMesh m = Town.Out.Solid(pos);
            m.SetFrame(pos, yaw);
            FurnitureBuilder.YardMailbox(m);
            m.ResetFrame();
            MailboxFixture fixture = MailboxFixture.Create(Root, pos + q * FurnitureBuilder.MailboxReach, post);
            var door = new RaidMesh();
            door.SetFrame(pos, yaw);
            FurnitureBuilder.YardMailboxDoor(door);
            door.ResetFrame();
            FixturePart doorPart = FixturePart.Hinge(fixture.transform, "Door", door, pos + q * FurnitureBuilder.MailboxDoorHinge, q * Vector3.left, 100f);
            var flag = new RaidMesh();
            flag.SetFrame(pos, yaw);
            FurnitureBuilder.YardMailboxFlag(flag);
            flag.ResetFrame();
            FixturePart flagPart = FixturePart.Hinge(fixture.transform, "Flag", flag, pos + q * FurnitureBuilder.MailboxFlagPivot, q * Vector3.right, 90f)
                .WithSpeed(0.3f);
            fixture.SetParts(doorPart, flagPart);
        }

        /// <summary>The enamelled house-number plate on the front wall beside the door (at local x <paramref name="doorLx"/>), on the
        /// wall segment it's fixed to (an <see cref="OutdoorHouseNumber"/>, read from the yard).</summary>
        public void HouseNumber(float doorLx, string number)
        {
            Vector3 n = Dir(0f, -1f);
            Vector3 at = W(doorLx - 0.95f, 1.62f, -HalfD - Half - 0.004f);
            string line = RaidHomeLiving.NumberLine(number, Rng);
            Later(() =>
            {
                Transform wall = WallObjectAt(at);
                var m = new RaidMesh();
                FurnitureBuilder.HouseNumberPlate(m, at, n, number);
                GameObject plate = m.Emit("HouseNumber", wall, at);
                if (plate != null) plate.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                OutdoorHouseNumber.Create(wall, number, at + n * 0.04f, at, n, line);
            });
        }

        /// <summary>The outside tap on the house's local west wall (at local z <paramref name="lz"/>): a standpipe with a red wheel
        /// and a hose reel beside it (a <see cref="HoseSpigot"/>: clean mains water to drink or bottle from the yard).</summary>
        public void OutsideTap(float lz)
        {
            Vector3 face = W(-HalfW - Half - 0.004f, 0f, lz);
            Vector3 n = Dir(-1f, 0f);
            float yaw = Mathf.Atan2(n.z, -n.x) * Mathf.Rad2Deg;   // the tap's frame: out from the wall toward -X
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            RaidMesh m = Town.Out.Solid(face);
            m.SetFrame(face, yaw);
            Vector3 coupling = FurnitureBuilder.HoseTap(m, out Vector3 nozzle, out Vector3 nozzleDir);
            m.ResetFrame();

            Vector3 body = face + q * FurnitureBuilder.HoseTapBody;
            HoseSpigot tap = HoseSpigot.Create(Root, body + Vector3.up * 0.04f, face + q * coupling, face, n);
            var wheel = new RaidMesh();
            wheel.SetFrame(face, yaw);
            FurnitureBuilder.HoseTapWheel(wheel);
            wheel.ResetFrame();
            FixturePart wheelPart = FixturePart.Hinge(tap.transform, "Wheel", wheel, body + Vector3.up * 0.032f, Vector3.down, 270f, ShadowCastingMode.Off)
                .WithSpeed(0.6f);

            var water = new RaidMesh();
            water.SetFrame(face, yaw);
            FurnitureBuilder.HoseTrickle(water, nozzle, nozzleDir);
            water.ResetFrame();
            Renderer trickle = null;
            GameObject go = water.Emit("Trickle", tap.transform, face + q * nozzle, ShadowCastingMode.Off);
            if (go != null)
            {
                go.transform.rotation = Quaternion.identity;
                go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                trickle = go.GetComponent<Renderer>();
                trickle.enabled = false;
            }
            tap.SetParts(wheelPart, trickle);
        }
        #endregion
    }
}
