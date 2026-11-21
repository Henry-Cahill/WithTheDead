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
        private static readonly Color Snow = new Color(0.95f, 0.95f, 0.97f);

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
            if (piece.gameObject.GetComponent<CutawayOccluder>() == null) piece.gameObject.AddComponent<CutawayOccluder>();
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
                // Snow‑lip trim at the very top of the exterior wall
                m.Box(f.P(xmid, WallH - 0.015f, side * (Half + 0.012f)), f.Size(xlen, 0.02f, 0.004f), DecorPalette.Matte(TrimWhite * 1.07f));
            }
        }

        // A white corner board running up the outer edge of a corner, above the foundation.
        private static void CornerBoard(RaidMesh m, Vector3 corner)
        {
            float sx = corner.x < Outline.center.x ? -1f : 1f, sz = corner.z < Outline.center.y ? -1f : 1f;
            var o = new Vector3(corner.x + sx * (Half - 0.045f), 0f, corner.z + sz * (Half - 0.045f));
            m.Box(new Vector3(o.x, (0.3f + WallH - 0.01f) * 0.5f, o.z), new Vector3(0.13f, WallH - 0.31f, 0.13f), DecorPalette.Matte(TrimWhite));
            // Subtle weathered nail detail near top‑inner edge of the board
            m.Box(new Vector3(o.x + sx * 0.04f,
                               (0.3f + WallH - 0.01f) * 0.5f + (WallH - 0.31f) * 0.45f,
                               o.z + sz * 0.04f),
                  new Vector3(0.02f, 0.02f, 0.01f),
                  DecorPalette.Matte(TrimWhite * 0.8f));
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
            // ----- window‑hardware details -----
            Surf lining = DecorPalette.Matte(TrimWhite * 0.95f);
            m.Box(f.P(o0 + 0.012f, cy, 0f), f.Size(0.024f, hh, thick), lining);
            m.Box(f.P(o1 - 0.012f, cy, 0f), f.Size(0.024f, hh, thick), lining);
            m.Box(f.P(f.Mid, WindowHead - 0.012f, 0f), f.Size(w, 0.024f, thick), lining);
            // small nail heads along the sill – darkened trim colour
            for (int i = 0; i < 4; i++)
            {
                float nailX = f.Mid - WindowWidth * 0.5f + 0.12f + i * 0.35f; // spread across the opening
                m.Box(f.P(nailX, WindowSill + 0.02f, 0f),
                    new Vector3(0.02f, 0.02f, 0.005f),
                    DecorPalette.Matte(TrimWhite * 0.8f));
            }


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
                // Snow‑compact on the exterior sill – small random depth for each window
                if (rng.NextDouble() < 0.75)
                {
                    float snowDepth = 0.008f + (float)rng.NextDouble() * 0.006f; // 0.8–1.4 cm
                    // centre the box on the sill, raise half the depth above the sill plane
                    m.Box(f.P(f.Mid, WindowSill + snowDepth * 0.5f, 0f),
                        new Vector3(WindowWidth - 0.06f, snowDepth, 0.02f),
                        DecorPalette.Matte(Snow));
                }
                float latchY = WindowSill + 0.32f; // about a foot above the sill
                float latchZ = side * (Half + 0.004f); // flush with the interior face
                Vector3 plateSize = new Vector3(0.06f, 0.008f, 0.015f);
                Color brass = new Color(0.78f, 0.61f, 0.43f);
                m.Box(f.P(f.Mid, latchY, latchZ), plateSize, DecorPalette.Matte(brass));
                // latch lever – same brass tone, hinged on the right edge of the plate
                Vector3 leverSize = new Vector3(0.008f, 0.04f, 0.015f);
                float leverX = f.Mid + 0.028f; // right edge of the plate (half width + small gap)
                m.Box(f.P(leverX, latchY, latchZ), leverSize, DecorPalette.Matte(brass));
                // occasional wear chip on the latch plate
                if (rng.NextDouble() < 0.35)
                {
                    float chipY = latchY + (float)(rng.NextDouble() - 0.5) * 0.004f;
                    float chipX = f.Mid + (float)(rng.NextDouble() - 0.5) * 0.02f;
                    m.Box(f.P(chipX, chipY, latchZ + 0.001f), new Vector3(0.012f, 0.006f, 0.001f), DecorPalette.Matte(brass * 0.6f));
                }
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
            // exterior lock plate (metal, darkened)
            float lockY = DoorTop * 0.5f + 0.30f; // around knob height
            float lockX = o0 - 0.04f;
            float lockZ = 0f;
                      m.Box(f.P(lockX, lockY, lockZ), new Vector3(0.015f, 0.05f, 0.02f), DecorPalette.Matte(TrimWhite * 0.55f));
                    // Top wooden jamb cap – runs across the doorway just under the lintel.
                    float capTopY = DoorTop - 0.01f; // 1 cm below the top edge
                    Vector3 capTopSize = new Vector3(gap + 0.04f, 0.018f, 0.012f);
                    m.Box(f.P(f.Mid, capTopY, 0f), capTopSize, DecorPalette.Matte(TrimWhite * 0.95f));

                    // Bottom wooden jamb cap – sits on the floor edge.
                    float capBotY = FloorTop * 0.5f + 0.011f; // half floor thickness + 1 cm
                    Vector3 capBotSize = new Vector3(gap + 0.04f, 0.018f, 0.012f);
                     m.Box(f.P(f.Mid, capBotY, 0f), capBotSize, DecorPalette.Matte(TrimWhite * 0.95f));

                     // Rust‑stained nail heads on the left and right jambs.
                     int nailCount = rng.Next(0, 3);
                     for (int i = 0; i < nailCount; i++)
                     {
                         float nailY = FloorTop * 0.5f + 0.04f + (float)rng.NextDouble() * 0.12f;
                         float leftX = o0 - 0.03f + (float)rng.NextDouble() * 0.01f;
                         float rightX = o1 + 0.03f + (float)rng.NextDouble() * 0.01f;
                         Vector3 nailSize = new Vector3(0.018f, 0.018f, 0.006f);
                         Color nailColor = TrimWhite * 0.6f;
                         m.Box(f.P(leftX, nailY, 0f), nailSize, DecorPalette.Matte(nailColor));
                         m.Box(f.P(rightX, nailY, 0f), nailSize, DecorPalette.Matte(nailColor));
                     }

                     // Metal strike plate (behind the lock) – slightly recessed, darkened.
                     float strikeZ = -0.005f; // recessed into the jamb
                     Vector3 strikeSize = new Vector3(0.014f, 0.045f, 0.008f);
                     m.Box(f.P(lockX, lockY, strikeZ), strikeSize, DecorPalette.Matte(new Color(0.48f, 0.41f, 0.36f) * 0.7f));

                    // Optional weathered top cap variation.
                    if (rng.NextDouble() < 0.3)
                    {
                        // Slightly weathered top cap.
                        m.Box(f.P(f.Mid, capTopY, 0f), capTopSize, DecorPalette.Matte(TrimWhite * 0.85f));
                    }
                     // Door‑bell button (brass‑tinted metal)
                     float bellY = lockY + 0.12f; // sit above the lock plate
                     float bellX = lockX; // same X as the lock plate
                     float bellZ = lockZ;
                     Vector3 bellSize = new Vector3(0.03f, 0.02f, 0.015f);
                     Color brass = new Color(0.78f, 0.61f, 0.43f);
                     m.Box(f.P(bellX, bellY, bellZ), bellSize, DecorPalette.Matte(brass));
                     // Back‑plate for the door‑bell button
                     float plateY = bellY - 0.01f;
                     Vector3 plateSize = new Vector3(0.05f, 0.005f, 0.01f);
                     m.Box(f.P(bellX, plateY, bellZ), plateSize, DecorPalette.Matte(TrimWhite * 0.7f));
                     // Simple brass knocker (two short boxes forming a U‑shape)
                     float knockerX = o0 - 0.06f; // left of the lock plate
                     float knockerBaseY = DoorTop * 0.5f + 0.05f; // near eye level
                     float knockerZ = 0f;
                     Vector3 knockerBar = new Vector3(0.005f, 0.04f, 0.015f);
                     m.Box(f.P(knockerX, knockerBaseY, knockerZ), knockerBar, DecorPalette.Matte(brass));
                     m.Box(f.P(knockerX, knockerBaseY - 0.025f, knockerZ), knockerBar, DecorPalette.Matte(brass));
                     // Optional house‑number plaque (white with dark “numbers”)
                     if (rng.NextDouble() < 0.5)
                     {
                         float plaqueX = o1 + 0.06f; // right of the right jamb
                         float plaqueY = DoorTop * 0.5f; // centred vertically
                         float plaqueZ = 0f;
                         Vector3 plaqueSize = new Vector3(0.12f, 0.02f, 0.005f);
                         m.Box(f.P(plaqueX, plaqueY, plaqueZ), plaqueSize, DecorPalette.Matte(TrimWhite));
                         // Simulated dark numerals as small paint‑chip rectangles
                         for (int i = 0; i < 3; i++)
                         {
                             float numY = plaqueY + (float)(rng.NextDouble() - 0.5) * 0.01f;
                             float numX = plaqueX - 0.04f + i * 0.03f;
                             m.Box(f.P(numX, numY, plaqueZ + 0.001f), new Vector3(0.018f, 0.012f, 0.001f), DecorPalette.Matte(TrimWhite * 0.25f));
                         }
                     }
                     // Occasional paint‑chip wear on the door‑bell button
                     if (rng.NextDouble() < 0.4)
                     {
                         float chipY = bellY + (float)(rng.NextDouble() - 0.5) * 0.005f;
                         float chipX = bellX + (float)(rng.NextDouble() - 0.5) * 0.01f;
                         m.Box(f.P(chipX, chipY, bellZ + 0.001f), new Vector3(0.012f, 0.006f, 0.001f), DecorPalette.Matte(brass * 0.6f));
                     }
            // rubber door‑stop (soft, dark brown)
            float stopY = FloorTop * 0.5f + 0.02f; // centre just above floor; 2 cm clearance
            float stopX = o0 - 0.02f;
            float stopZ = 0f;
            m.Box(f.P(stopX, stopY, stopZ), new Vector3(0.015f, 0.04f, 0.015f), DecorPalette.Matte(new Color(0.18f, 0.12f, 0.08f)));
            m.Box(f.P(o1 + 0.015f, hingeY, 0f), f.Size(0.03f, 0.08f, 0.02f), DecorPalette.Matte(TrimWhite * 0.7f));
            // interior lever‑handle (metal, slight variation)
            float handleY = DoorTop * 0.5f + 0.12f; // height ~0.4 m above floor
            float handleX = o1 + 0.03f; // a few centimetres out from the right jamb
            float handleZ = 0f; // centred on the doorway plane
            m.Box(f.P(handleX, handleY, handleZ), new Vector3(0.02f, 0.12f, 0.025f), DecorPalette.Matte(TrimWhite * 0.65f));
            for (int side = -1; side <= 1; side += 2)
            {
                HomeRoom room = rooms[(side + 1) / 2];
                Surf casing = DecorPalette.Matte(room != null ? room.Design.Trim : TrimWhite);
                float off = side * (Half + 0.012f);
                m.Box(f.P(o0 - 0.035f, (DoorTop + 0.07f) * 0.5f, off), f.Size(0.07f, DoorTop + 0.07f, 0.024f), casing);
                // occasional low paint‑chip on exterior casing
                if (rng.NextDouble() < 0.4)
                {
                    float chipY = 0.15f; // just above floor level
                    float chipX = o0 - 0.08f + (float)rng.NextDouble() * 0.06f;
                    float chipZ = (float)rng.NextDouble() * 0.02f - 0.01f;
                    m.Box(f.P(chipX, chipY, chipZ), new Vector3(0.04f, 0.02f, 0.004f), DecorPalette.Matte(TrimWhite * 0.5f));
                }
                m.Box(f.P(o1 + 0.035f, (DoorTop + 0.07f) * 0.5f, off), f.Size(0.07f, DoorTop + 0.07f, 0.024f), casing);
                m.Box(f.P(f.Mid, DoorTop + 0.035f, off), f.Size(gap + 0.14f, 0.07f, 0.024f), casing);
            }
            // Snow draped across the doorway threshold – thin random layer
            if (rng.NextDouble() < 0.65)
            {
                float snowDepth = 0.008f + (float)rng.NextDouble() * 0.006f; // 0.8–1.4 cm
                // raise the snow half its thickness above the threshold surface
                m.Box(f.P(f.Mid, (FloorTop + 0.012f) * 0.5f + snowDepth * 0.5f, 0f),
                    new Vector3(gap, snowDepth, Half * 2f + 0.05f),
                    DecorPalette.Matte(Snow));
            }
            // Small drift on the interior side of the threshold
            if (rng.NextDouble() < 0.4)
            {
                float driftDepth = 0.006f + (float)rng.NextDouble() * 0.004f;
                float driftX = f.Mid + (float)(rng.NextDouble() - 0.5) * (gap * 0.5f);
                m.Box(f.P(driftX, (FloorTop + 0.012f) * 0.5f + driftDepth * 0.5f, 0f),
                    new Vector3(0.12f, driftDepth, Half * 2f + 0.03f),
                    DecorPalette.Matte(Snow));
            }

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
