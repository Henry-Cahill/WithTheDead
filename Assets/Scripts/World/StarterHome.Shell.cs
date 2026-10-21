using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    internal static partial class StarterHome
    {
        #region Finishes
        private static readonly Color Siding = new Color(0.95f, 0.87f, 0.58f);        // butter-yellow clapboard
        private static readonly Color TrimWhite = new Color(0.88f, 0.87f, 0.82f);      // corner boards, casings, sashes
        private static readonly Color ShutterGreen = new Color(0.17f, 0.27f, 0.21f);
        private static readonly Color FrontDoorRed = new Color(0.46f, 0.13f, 0.10f);
        private static readonly Color CoreColor = new Color(0.20f, 0.19f, 0.18f);      // the wall tops, as in the raid town
        private static readonly Color WarmGlow = new Color(1f, 0.84f, 0.58f);
        private static readonly Color Soil = new Color(0.24f, 0.17f, 0.11f);

        private const float WindowWidth = 1.3f;                                        // glazing between the finished jambs
        private const float WindowJamb = (Tile - WindowWidth) * 0.5f;
        private const float WindowSill = CloneZConfig.HouseWindowSillHeight;
        private const float WindowHead = CloneZConfig.HouseWallHeight - CloneZConfig.HouseWindowHeadHeight;
        private const float DoorTop = CloneZConfig.HouseWallHeight - CloneZConfig.HouseDoorwayLintelHeight;

        private static float DoorGap => Mathf.Min(CloneZConfig.HouseDoorwayGapWidth, Tile - 0.2f);   // as BuildPiece cuts it
        #endregion

        #region Entry
        /// <summary>Builds the finished look onto a house piece standing in one of the home's slots — its
        /// visible geometry and fittings (the piece keeps its own colliders and navmesh carvers). Returns
        /// false outside the wave world, outside the home, and for roofs, which keep their rough look.</summary>
        public static bool TryFinish(BuildPiece piece)
        {
            if (!Active || piece == null) return false;
            switch (piece.Type)
            {
                case BuildPieceType.Floor: return FinishFloor(piece);
                case BuildPieceType.Wall:
                case BuildPieceType.Window:
                case BuildPieceType.Doorway: return FinishWall(piece);
                default: return false;
            }
        }

        // The finished geometry hangs off the piece in world axes (the piece itself may be turned 90°).
        private static void Emit(BuildPiece piece, RaidMesh m, ShadowCastingMode shadows)
        {
            GameObject go = m.Emit("Finish", piece.transform, piece.transform.position, shadows);
            if (go != null) go.transform.rotation = Quaternion.identity;
            DecorPalette.Apply();
        }

        private static bool Is(BuildPiece piece, BuildPieceType type, float x, float z)
        {
            Vector3 p = piece.transform.position;
            return piece.Type == type && Mathf.Abs(p.x - x) < 0.1f && Mathf.Abs(p.z - z) < 0.1f;
        }
        #endregion

        #region Walls
        /// <summary>A wall piece's line: <see cref="Coord"/> across, <see cref="A0"/>..<see cref="A1"/> along.</summary>
        private struct Frame
        {
            public bool AlongX;
            public float Coord, Mid, A0, A1;

            public Vector3 P(float along, float y, float across)
                => AlongX ? new Vector3(along, y, Coord + across) : new Vector3(Coord + across, y, along);

            public Vector3 N(int side) => AlongX ? new Vector3(0f, 0f, side) : new Vector3(side, 0f, 0f);

            public Vector3 Size(float len, float h, float thick) => AlongX ? new Vector3(len, h, thick) : new Vector3(thick, h, len);

            public Vector3 Along => AlongX ? Vector3.right : Vector3.forward;
        }

        /// <summary>A solid stretch of a wall piece: <see cref="S"/>..<see cref="E"/> along it, <see cref="Y0"/>..<see cref="Y1"/> high.</summary>
        private readonly struct Band
        {
            public readonly float S, E, Y0, Y1;
            public Band(float s, float e, float y0, float y1) { S = s; E = e; Y0 = y0; Y1 = y1; }
        }

        private static bool FinishWall(BuildPiece piece)
        {
            Transform t = piece.transform;
            Vector3 c = t.position;
            bool alongX = Mathf.Abs(Mathf.Sin(t.eulerAngles.y * Mathf.Deg2Rad)) < 0.5f;
            var f = new Frame { AlongX = alongX, Coord = alongX ? c.z : c.x, Mid = alongX ? c.x : c.z };
            f.A0 = f.Mid - Tile * 0.5f;
            f.A1 = f.Mid + Tile * 0.5f;

            // What lies either side of the wall: a room of the home or the outdoors. A piece with no room
            // on either side isn't part of the home.
            Vector3 minus = f.P(f.Mid, 0f, -0.5f), plus = f.P(f.Mid, 0f, 0.5f);
            var rooms = new[] { RoomAt(minus.x, minus.z), RoomAt(plus.x, plus.z) };
            if (rooms[0] == null && rooms[1] == null) return false;

            var rng = new System.Random(SlotSeed(piece.Type, c.x, c.z));
            var m = new RaidMesh();
            bool corner0 = IsOuterCorner(f.P(f.A0, 0f, 0f)), corner1 = IsOuterCorner(f.P(f.A1, 0f, 0f));
            foreach (Band b in BandsFor(piece.Type, f)) DrawBand(m, f, b, rooms, corner0, corner1);
            if (f.AlongX && corner0) CornerBoard(m, f.P(f.A0, 0f, 0f));
            if (f.AlongX && corner1) CornerBoard(m, f.P(f.A1, 0f, 0f));

            if (piece.Type == BuildPieceType.Window)
            {
                DrawWindow(piece, m, f, rooms, rng, Is(piece, BuildPieceType.Window, 2f, 3f));
                // The finished window has solid jambs either side of its glazing.
                float h = WindowHead - WindowSill, y = (WindowSill + WindowHead) * 0.5f - WallH * 0.5f;
                var jamb = new Vector3(WindowJamb, h, CloneZConfig.HouseWallThickness);
                piece.AddSolid(new Vector3(-(Tile - WindowJamb) * 0.5f, y, 0f), jamb);
                piece.AddSolid(new Vector3((Tile - WindowJamb) * 0.5f, y, 0f), jamb);
            }
            else if (piece.Type == BuildPieceType.Doorway)
            {
                DrawDoorway(piece, m, f, rooms, rng);
            }
            SlotFixtures(piece, m, f, rng);

            Emit(piece, m, ShadowCastingMode.On);
            piece.gameObject.AddComponent<CutawayOccluder>();
            return true;
        }

        private static List<Band> BandsFor(BuildPieceType type, in Frame f)
        {
            var bands = new List<Band>(4);
            switch (type)
            {
                case BuildPieceType.Window:
                    bands.Add(new Band(f.A0, f.A1, 0f, WindowSill));
                    bands.Add(new Band(f.A0, f.A1, WindowHead, WallH));
                    bands.Add(new Band(f.A0, f.A0 + WindowJamb, WindowSill, WindowHead));
                    bands.Add(new Band(f.A1 - WindowJamb, f.A1, WindowSill, WindowHead));
                    break;
                case BuildPieceType.Doorway:
                {
                    float jamb = (Tile - DoorGap) * 0.5f;
                    bands.Add(new Band(f.A0, f.A0 + jamb, 0f, DoorTop));
                    bands.Add(new Band(f.A1 - jamb, f.A1, 0f, DoorTop));
                    bands.Add(new Band(f.A0, f.A1, DoorTop, WallH));
                    break;
                }
                default:
                    bands.Add(new Band(f.A0, f.A1, 0f, WallH));
                    break;
            }
            return bands;
        }

        private static bool IsOuterCorner(Vector3 p)
        {
            return (Mathf.Abs(p.x - Outline.xMin) < 0.05f || Mathf.Abs(p.x - Outline.xMax) < 0.05f)
                && (Mathf.Abs(p.z - Outline.yMin) < 0.05f || Mathf.Abs(p.z - Outline.yMax) < 0.05f);
        }

        // One solid stretch: the dark core, then each face — wallpaper over wainscot with a baseboard and
        // chair rail on a room side, clapboard over a concrete foundation with a drip cap and frieze board
        // outside. At an outer corner the east-west walls fill the corner square and the siding wraps it.
        private static void DrawBand(RaidMesh m, in Frame f, in Band b, HomeRoom[] rooms, bool corner0, bool corner1)
        {
            bool atA0 = corner0 && Mathf.Abs(b.S - f.A0) < 0.01f, atA1 = corner1 && Mathf.Abs(b.E - f.A1) < 0.01f;
            float h = b.Y1 - b.Y0, cy = (b.Y0 + b.Y1) * 0.5f;
            float cs = b.S - (atA0 && f.AlongX ? Half : 0f), ce = b.E + (atA1 && f.AlongX ? Half : 0f);
            m.Box(f.P((cs + ce) * 0.5f, cy, 0f), f.Size(ce - cs, h, Half * 2f), DecorPalette.Matte(CoreColor));

            for (int side = -1; side <= 1; side += 2)
            {
                HomeRoom room = rooms[(side + 1) / 2];
                Vector3 n = f.N(side);
                if (room != null)
                {
                    float mid = (b.S + b.E) * 0.5f, len = b.E - b.S;
                    m.Upright(f.P(mid, cy, side * (Half + 0.003f)), n, len, h, DecorMaterials.WallInterior(room.Design));
                    Surf trim = DecorPalette.Matte(room.Design.Trim);
                    if (b.Y0 < 0.2f)
                        m.Box(f.P(mid, 0.11f, side * (Half + 0.008f)), f.Size(len, 0.22f, 0.016f), trim);
                    float rail = room.Design.RailHeight;
                    if (b.Y0 < rail && b.Y1 > rail + 0.05f)
                        m.Box(f.P(mid, rail + 0.01f, side * (Half + 0.012f)), f.Size(len, 0.06f, 0.024f), DecorPalette.Matte(room.Design.Trim * 1.05f));
                    continue;
                }
                float xs = b.S - (atA0 ? Half : 0f), xe = b.E + (atA1 ? Half : 0f);
                float xmid = (xs + xe) * 0.5f, xlen = xe - xs;
                m.Upright(f.P(xmid, cy, side * (Half + 0.004f)), n, xlen, h, DecorMaterials.WallExterior(ExteriorStyle.Siding, Siding));
                Surf white = DecorPalette.Matte(TrimWhite);
                if (b.Y0 < 0.29f && b.Y1 > 0.34f)
                    m.Box(f.P(xmid, 0.31f, side * (Half + 0.014f)), f.Size(xlen, 0.04f, 0.028f), white);
                if (b.Y1 > WallH - 0.01f)
                    m.Box(f.P(xmid, WallH - 0.075f, side * (Half + 0.012f)), f.Size(xlen, 0.14f, 0.024f), white);
            }
        }

        // A white corner board running up the outer edge of a corner, above the foundation.
        private static void CornerBoard(RaidMesh m, Vector3 corner)
        {
            float sx = corner.x < Outline.center.x ? -1f : 1f, sz = corner.z < Outline.center.y ? -1f : 1f;
            var o = new Vector3(corner.x + sx * (Half - 0.045f), 0f, corner.z + sz * (Half - 0.045f));
            m.Box(new Vector3(o.x, (0.3f + WallH - 0.01f) * 0.5f, o.z), new Vector3(0.13f, WallH - 0.31f, 0.13f), DecorPalette.Matte(TrimWhite));
        }
        #endregion

        #region Windows
        // Linings, casings and sills round the opening; shutters (and, facing the street, a flower box)
        // outside. The sashes, glass, curtains and blind are the window's working fittings, built by
        // WindowFittings (StarterHome.Windows.cs) — the kitchen window gets a valance and blind instead
        // of curtains, clear of the worktop under it.
        private static void DrawWindow(BuildPiece piece, RaidMesh m, in Frame f, HomeRoom[] rooms, System.Random rng, bool kitchen)
        {
            float o0 = f.Mid - WindowWidth * 0.5f, o1 = f.Mid + WindowWidth * 0.5f;
            float w = WindowWidth, hh = WindowHead - WindowSill, cy = (WindowSill + WindowHead) * 0.5f;
            float thick = Half * 2f + 0.01f;
            Surf lining = DecorPalette.Matte(TrimWhite * 0.95f);
            m.Box(f.P(o0 + 0.012f, cy, 0f), f.Size(0.024f, hh, thick), lining);
            m.Box(f.P(o1 - 0.012f, cy, 0f), f.Size(0.024f, hh, thick), lining);
            m.Box(f.P(f.Mid, WindowHead - 0.012f, 0f), f.Size(w, 0.024f, thick), lining);
            m.Box(f.P(f.Mid, WindowSill + 0.015f, 0f), f.Size(w, 0.03f, thick), lining);

            for (int side = -1; side <= 1; side += 2)
            {
                HomeRoom room = rooms[(side + 1) / 2];
                Surf casing = DecorPalette.Matte(room != null ? room.Design.Trim : TrimWhite);
                float off = side * (Half + 0.012f);
                m.Box(f.P(o0 - 0.035f, cy, off), f.Size(0.07f, hh + 0.14f, 0.024f), casing);
                    // Add subtle paint‑chip detail on the exterior casing
                    if (rng.NextDouble() < 0.5)
                    {
                        float chipY = WindowSill + 0.1f + (float)(rng.NextDouble() * 0.2f);
                        float chipZ = off + (float)(rng.NextDouble() * 0.01f - 0.005f);
                        m.Box(f.P(o0 - 0.035f, chipY, chipZ), f.Size(0.04f, 0.02f, 0.005f), DecorPalette.Matte(TrimWhite * 0.6f));
                    }
                m.Box(f.P(o1 + 0.035f, cy, off), f.Size(0.07f, hh + 0.14f, 0.024f), casing);
                    // Add subtle paint‑chip detail on the exterior casing
                    if (rng.NextDouble() < 0.5)
                    {
                        float chipY = WindowSill + 0.1f + (float)(rng.NextDouble() * 0.2f);
                        float chipZ = off + (float)(rng.NextDouble() * 0.01f - 0.005f);
                        m.Box(f.P(o1 + 0.035f, chipY, chipZ), f.Size(0.04f, 0.02f, 0.005f), DecorPalette.Matte(TrimWhite * 0.6f));
                    }
                m.Box(f.P(f.Mid, WindowHead + 0.035f, off), f.Size(w + 0.14f, 0.07f, 0.024f), casing);
                m.Box(f.P(f.Mid, WindowSill + 0.02f, side * (Half + 0.035f)), f.Size(w + 0.18f, 0.04f, 0.07f), casing);
                if (room != null) continue;
                Shutters(m, f, side);
                if (f.AlongX && side < 0) FlowerBox(m, f, side, rng);   // the street front
            }

            WindowFittings(piece, m, f, rooms, rng, kitchen);
        }

        // A louvred shutter either side of the window, outside.
        private static void Shutters(RaidMesh m, in Frame f, int side)
        {
            const float sw = 0.26f;
            float sh = WindowHead - WindowSill + 0.1f, cy = (WindowSill + WindowHead) * 0.5f;
            Surf panel = DecorPalette.Matte(ShutterGreen), slat = DecorPalette.Matte(ShutterGreen * 0.78f);
            for (int e = -1; e <= 1; e += 2)
            {
                float c = f.Mid + e * (WindowWidth * 0.5f + 0.07f + sw * 0.5f);
                m.Box(f.P(c, cy, side * (Half + 0.02f)), f.Size(sw, sh, 0.03f), panel);
                for (float y = WindowSill + 0.04f; y < WindowHead + 0.03f; y += 0.075f)
                    m.Box(f.P(c, y, side * (Half + 0.038f)), f.Size(sw - 0.06f, 0.024f, 0.012f), slat);
            }
        }

        // A painted planter under the sill, full of geraniums and marigolds.
        private static void FlowerBox(RaidMesh m, in Frame f, int side, System.Random rng)
        {
            float across = side * (Half + 0.12f);
            m.Box(f.P(f.Mid, WindowSill - 0.15f, across), f.Size(WindowWidth + 0.1f, 0.2f, 0.2f), DecorPalette.Matte(ShutterGreen));
            m.Box(f.P(f.Mid, WindowSill - 0.055f, across), f.Size(WindowWidth + 0.04f, 0.012f, 0.16f), DecorPalette.Matte(Soil));
            Color[] blooms = { new Color(0.78f, 0.14f, 0.12f), new Color(0.92f, 0.70f, 0.16f), new Color(0.86f, 0.42f, 0.56f), new Color(0.92f, 0.90f, 0.84f) };
            int n = 10;
            for (int i = 0; i < n; i++)
            {
                float along = f.Mid - WindowWidth * 0.5f + (i + 0.5f) * WindowWidth / n;
                float a = across + side * (float)(rng.NextDouble() - 0.5) * 0.08f;
                m.Ellipsoid(f.P(along, WindowSill + 0.0f, a), new Vector3(0.16f, 0.13f, 0.16f), DecorPalette.Matte(new Color(0.18f, 0.34f, 0.14f) * (0.85f + (float)rng.NextDouble() * 0.3f)), 6, 4);
                if (rng.NextDouble() < 0.75)
                    m.Ellipsoid(f.P(along + (float)(rng.NextDouble() - 0.5) * 0.06f, WindowSill + 0.07f, a), new Vector3(0.07f, 0.06f, 0.07f), DecorPalette.Matte(blooms[rng.Next(blooms.Length)]), 6, 3);
            }
        }
        #endregion

        #region Doorways
        // Linings, casings both sides and a hardwood threshold over the floor's edge. The doors themselves
        // (they open, close and — the front door — lock) are built by DoorFittings (StarterHome.Doors.cs).
        private static void DrawDoorway(BuildPiece piece, RaidMesh m, in Frame f, HomeRoom[] rooms, System.Random rng)
        {
            float gap = DoorGap, o0 = f.Mid - gap * 0.5f, o1 = f.Mid + gap * 0.5f;
            float thick = Half * 2f + 0.01f;
            Surf lining = DecorPalette.Matte(TrimWhite * 0.95f);
            m.Box(f.P(o0 + 0.012f, DoorTop * 0.5f, 0f), f.Size(0.024f, DoorTop, thick), lining);
            m.Box(f.P(o1 - 0.012f, DoorTop * 0.5f, 0f), f.Size(0.024f, DoorTop, thick), lining);
            m.Box(f.P(f.Mid, DoorTop - 0.012f, 0f), f.Size(gap, 0.024f, thick), lining);
            // Small metal hinges for the door
            float hingeY = DoorTop * 0.5f + (float)(rng.NextDouble() - 0.5) * 0.02f;
            m.Box(f.P(o0 - 0.015f, hingeY, 0f), f.Size(0.03f, 0.08f, 0.02f), DecorPalette.Matte(TrimWhite * 0.7f));
            m.Box(f.P(o1 + 0.015f, hingeY, 0f), f.Size(0.03f, 0.08f, 0.02f), DecorPalette.Matte(TrimWhite * 0.7f));
            for (int side = -1; side <= 1; side += 2)
            {
                HomeRoom room = rooms[(side + 1) / 2];
                Surf casing = DecorPalette.Matte(room != null ? room.Design.Trim : TrimWhite);
                float off = side * (Half + 0.012f);
                m.Box(f.P(o0 - 0.035f, (DoorTop + 0.07f) * 0.5f, off), f.Size(0.07f, DoorTop + 0.07f, 0.024f), casing);
                m.Box(f.P(o1 + 0.035f, (DoorTop + 0.07f) * 0.5f, off), f.Size(0.07f, DoorTop + 0.07f, 0.024f), casing);
                m.Box(f.P(f.Mid, DoorTop + 0.035f, off), f.Size(gap + 0.14f, 0.07f, 0.024f), casing);
            }
            m.Box(f.P(f.Mid, (FloorTop + 0.012f) * 0.5f, 0f), f.Size(gap, FloorTop + 0.012f, Half * 2f + 0.05f), DecorPalette.Matte(new Color(0.42f, 0.30f, 0.18f)));

            DoorFittings(piece, m, f, rooms, rng);
        }
        #endregion

        #region Fittings
        // The things hung on particular walls of the home, by area: the kitchen's wall cupboards
        // (StarterHome.Kitchen.cs); the lights, switches, porch lantern, breaker panel, clock, pictures and house
        // number (StarterHome.Lighting.cs); the bathroom's mirror cabinet and towel rail (StarterHome.Bathroom.cs).
        private static void SlotFixtures(BuildPiece piece, RaidMesh m, in Frame f, System.Random rng)
        {
            KitchenWallFixtures(piece, m, f, rng);
            LightingWallFixtures(piece, m, f, rng);
            BathroomWallFixtures(piece, m, f, rng);
        }

        // The per-area fittings. Each receives the piece (a fixture's root goes under it, so it hides with the
        // wall when the camera cuts it away and goes when the wall comes down), the wall's static mesh (anything
        // that never moves can simply be drawn into it), the wall's frame and its stable per-slot random stream.
        static partial void DoorFittings(BuildPiece piece, RaidMesh m, Frame f, HomeRoom[] rooms, System.Random rng);
        static partial void WindowFittings(BuildPiece piece, RaidMesh m, Frame f, HomeRoom[] rooms, System.Random rng, bool kitchen);
        static partial void KitchenWallFixtures(BuildPiece piece, RaidMesh m, Frame f, System.Random rng);
        static partial void LightingWallFixtures(BuildPiece piece, RaidMesh m, Frame f, System.Random rng);
        static partial void BathroomWallFixtures(BuildPiece piece, RaidMesh m, Frame f, System.Random rng);

        /// <summary>A point on a wall's face: <paramref name="side"/> -1/+1 picks the face, <paramref name="along"/>
        /// is the world coordinate along the wall and <paramref name="y"/> the height.</summary>
        private static Vector3 Face(in Frame f, int side, float along, float y) => f.P(along, y, side * (Half + 0.004f));
        #endregion

        #region Floors
        private static bool FinishFloor(BuildPiece piece)
        {
            Vector3 c = piece.transform.position;
            HomeRoom room = RoomAt(c.x, c.z);
            if (room == null) return false;
            var rng = new System.Random(SlotSeed(BuildPieceType.Floor, c.x, c.z));
            var m = new RaidMesh();
            Surf floor = room == Bath
                ? DecorMaterials.Linoleum(new Color(0.90f, 0.90f, 0.88f), new Color(0.50f, 0.66f, 0.68f), true)   // small bathroom tiles
                : room == Store
                ? DecorMaterials.Linoleum(new Color(0.52f, 0.48f, 0.40f), new Color(0.60f, 0.56f, 0.46f), false)
                : c.x < 3f
                    ? DecorMaterials.Linoleum(new Color(0.80f, 0.78f, 0.70f), new Color(0.24f, 0.26f, 0.28f), true)   // the kitchen-diner
                    : DecorMaterials.WoodFloor(new Color(0.52f, 0.35f, 0.20f));
            m.Box(new Vector3(c.x, FloorTop * 0.5f, c.z), new Vector3(Tile, FloorTop, Tile), floor);

            // Rugs and mats, each drawn by the tile under its middle.
            if (Is(piece, BuildPieceType.Floor, 4f, 4f))
            {
                FurnitureBuilder.Rug(m, rng, new Vector3(4.0f, FloorTop - 0.02f, 4.95f), 2.2f, 1.0f, 90f, 0);
                Mat(m, new Vector3(4.0f, FloorTop, 3.42f), 0.9f, 0.5f);
            }
            else if (Is(piece, BuildPieceType.Floor, 2f, 6f))
            {
                FurnitureBuilder.Rug(m, rng, new Vector3(1.95f, FloorTop - 0.02f, 6.0f), 1.6f, 1.3f, 90f, 2);
            }
            else if (Is(piece, BuildPieceType.Floor, 6f, 4f))
            {
                FurnitureBuilder.Rug(m, rng, new Vector3(5.07f, FloorTop - 0.02f, 4.45f), 1.1f, 0.6f, 90f, 1);
            }
            Emit(piece, m, ShadowCastingMode.Off);
            return true;
        }

        // A coir doormat with a dark rubber border.
        private static void Mat(RaidMesh m, Vector3 at, float w, float d)
        {
            m.Flat(at + Vector3.up * 0.006f, w, d, 0f, DecorPalette.Matte(new Color(0.16f, 0.13f, 0.10f)));
            m.Flat(at + Vector3.up * 0.008f, w - 0.08f, d - 0.08f, 0f, DecorPalette.Matte(new Color(0.48f, 0.36f, 0.20f)));
        }
        #endregion
    }
}
