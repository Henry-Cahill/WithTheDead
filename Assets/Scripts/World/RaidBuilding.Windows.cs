using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The raid town's windows: the starter home's double-hung window — a fixed upper sash over a lower one that slides up,
    /// a sash lock, and on the room side whatever a building of its kind hangs there (a house's curtains, an office's or a
    /// clinic's blind, an army post's blackout cloth, nothing in a shop front) — fitted in every window opening as a working
    /// <see cref="RaidWindow"/>, glazed, smashed or boarded over as the town was left. A house's windows may wear shutters
    /// outside, and those facing the street a planter of last summer's dead flowers under the snow.
    /// </summary>
    internal sealed partial class RaidBuilding
    {
        private const float SashTrack = 0.024f;    // each sash's offset from the wall line (the lower one room-side)
        private const float SashLift = 0.48f;      // how far the lower sash slides up
        private const float BlindAcross = 0.075f;  // an inside-mounted blind's distance from the wall line, in the reveal
        private const float BoardsOff = 0.075f;    // the boards' distance out from the wall's face (clear of any shutters)

        private static readonly Color SashPaint = new Color(0.70f, 0.68f, 0.62f);
        private static readonly Color PaneTint = DecorPalette.ClearTint;

        private static readonly Color[] CurtainFabrics =
        {
            new Color(0.62f, 0.40f, 0.42f), new Color(0.66f, 0.52f, 0.22f), new Color(0.46f, 0.54f, 0.42f), new Color(0.20f, 0.26f, 0.40f),
            new Color(0.45f, 0.14f, 0.16f), new Color(0.72f, 0.64f, 0.50f), new Color(0.46f, 0.56f, 0.66f), new Color(0.42f, 0.30f, 0.22f),
        };

        private static readonly Color[] ShutterPaints =
        {
            new Color(0.17f, 0.27f, 0.21f), new Color(0.12f, 0.12f, 0.13f), new Color(0.18f, 0.22f, 0.32f), new Color(0.40f, 0.14f, 0.12f),
            new Color(0.84f, 0.83f, 0.78f),
        };

        private static readonly Color[] PlanterPaints =
        {
            new Color(0.17f, 0.27f, 0.21f), new Color(0.84f, 0.83f, 0.78f), new Color(0.46f, 0.30f, 0.18f),
        };

        private readonly Dictionary<RaidRoom, Color> _curtainFabric = new Dictionary<RaidRoom, Color>();
        private int _shutterStyle = -1, _planterStyle = -1;   // per house: -1 not yet decided, 0 none, else a paint + 1

        // The clear opening between the sill and head linings, and the height where the sashes' meeting rails meet.
        private const float SashBottom = SillHeight + 0.03f;
        private const float SashTop = HeadHeight - 0.024f;
        private const float SashMeet = (SashBottom + SashTop) * 0.5f;

        // The fixed part of a window, drawn into its segment: the upper sash in the outer track with the sash lock's keeper on
        // its meeting rail — and, outside a house, its shutters and planter.
        private void WindowFrame(WallPiece p)
        {
            Opening o = p.Opening;
            WallRun r = o.Run;
            var l = new OpeningLine(r.AlongX, r.Coord);
            int outside = OutsideSide(r, o.Pos);
            int roomSide = outside != 0 ? -outside : 1;
            float outer = -roomSide * SashTrack;
            FurnitureBuilder.OpeningSash(p.Mesh, l, o.Pos, o.Width - 0.05f, SashMeet - 0.02f, SashTop, outer, false, roomSide, SashPaint);
            p.Mesh.Box(l.P(o.Pos, SashMeet + 0.034f, outer), l.Size(0.04f, 0.008f, 0.03f), DecorPalette.Metal(FurnitureBuilder.Brass));
            if (outside != 0 && Kind == RaidBuildingKind.House) HouseWindowDecor(p, l, outside);
        }

        // Shutters either side outside (if this house has them and no other opening crowds the window), and on the street
        // front a planter.
        private void HouseWindowDecor(WallPiece p, in OpeningLine l, int outside)
        {
            Opening o = p.Opening;
            if (_shutterStyle < 0) _shutterStyle = Rng.NextDouble() < 0.65 ? 1 + Rng.Next(ShutterPaints.Length) : 0;
            if (_planterStyle < 0) _planterStyle = Rng.NextDouble() < 0.5 ? 1 + Rng.Next(PlanterPaints.Length) : 0;
            bool roomy = true;
            foreach (Opening other in o.Run.Openings)
            {
                if (other == o) continue;
                float gap = other.Kind == OpeningKind.Door ? 0.95f : 0.8f;   // a door keeps the wall beside its casing clear (a porch lamp)
                if (other.A < o.B + gap && other.B > o.A - gap) roomy = false;
            }
            if (_shutterStyle > 0 && roomy)
                FurnitureBuilder.OpeningShutters(p.Mesh, l, o.Pos, o.Width, SillHeight, HeadHeight, Half, outside, ShutterPaints[_shutterStyle - 1]);
            bool street = Vector3.Dot(RunNormal(o.Run, outside), Dir(0f, -1f)) > 0.7f;
            if (_planterStyle > 0 && street)
                FurnitureBuilder.OpeningFlowerBox(p.Mesh, Rng, l, o.Pos, o.Width, SillHeight, Half, outside, PlanterPaints[_planterStyle - 1]);
        }

        // The window's working parts, once its segment is an object and the room is furnished: the lower sash (and its lock's
        // lever), each sash's glazing, the boards, the dressing; then the RaidWindow that works them.
        /// <summary>
        /// Builds the interactive window components (sashes, locks, glazing, boards, curtains/blinds) and registers them with the RaidWindow system.
        /// </summary>
        private void FitWindow(WallPiece p)
        {
            Opening o = p.Opening;
            WallRun r = o.Run;
            var l = new OpeningLine(r.AlongX, r.Coord);
            int outside = OutsideSide(r, o.Pos);
            int roomSide = outside != 0 ? -outside : 1;
            float sashW = o.Width - 0.05f;
            float outer = -roomSide * SashTrack, inner = roomSide * SashTrack;
            bool glazed = o.Intact;
            Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);

            Transform segment = OpeningObject(o);
            // Create a root GameObject to hold all window parts and attach a NavMeshModifier so the window geometry is ignored by NavMesh baking.
            var root = new GameObject("WindowFittings");
            root.transform.SetParent(segment, false);
            root.transform.SetPositionAndRotation(RunPoint(r, o.Pos, 0f, 0f), Quaternion.identity);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            // The lower sash in the inner track, its lock's body on the meeting rail. A smashed window's lever is left turned
            // on it: there's no working that sash any more.
            // Initialize the lower sash mesh on the inner track; its lock sits on the meeting rail. If the window is smashed, the lever stays turned up because it can't be used.
            var lower = new RaidMesh();
            FurnitureBuilder.OpeningSash(lower, l, o.Pos, sashW, SashBottom, SashMeet + 0.02f, inner, true, roomSide, SashPaint);
            lower.Box(l.P(o.Pos, SashMeet + 0.024f, inner), l.Size(0.05f, 0.008f, 0.032f), brass);

            // If the window is intact, create the latch lever geometry; otherwise leave it as a static box.
            if (!glazed) lower.Box(l.P(o.Pos, SashMeet + 0.042f, 0f), l.Size(0.02f, 0.008f, 0.06f), brass);
            FixturePart sash = FixturePart.Slide(root.transform, "LowerSash", lower, l.P(o.Pos, SashBottom, inner), Vector3.up * SashLift)
                .WithSpeed(CloneZConfig.WindowSashSeconds);
            Renderer latchShut = null, latchOpen = null;
            if (glazed)
            {
                latchShut = WindowBox(sash.transform, "LatchShut", l.P(o.Pos, SashMeet + 0.042f, 0f), l.Size(0.02f, 0.008f, 0.06f), brass);
                latchOpen = WindowBox(sash.transform, "LatchOpen", l.P(o.Pos + 0.03f, SashMeet + 0.032f, inner), l.Size(0.06f, 0.008f, 0.02f), brass);
            }

            // Each sash's glazing: glass, or the shards left in the frame. The lower one rides on its sash.
            int seed = Rng.Next();
            var shards = new System.Random(seed);
            MeshFilter upperGlass = WindowGlazing(root.transform, "UpperGlazing", l, o.Pos, sashW, SashMeet - 0.02f, SashTop, outer, false, glazed, shards);
            MeshFilter lowerGlass = WindowGlazing(sash.transform, "LowerGlazing", l, o.Pos, sashW, SashBottom, SashMeet + 0.02f, inner, true, glazed, shards);

            Renderer boards = null;
            if (o.Boarded)
            {
                int face = outside != 0 ? outside : -roomSide;
                var m = new RaidMesh();
                FurnitureBuilder.OpeningBoards(m, Rng, l, o.Pos, o.Width, SillHeight, face, Half + BoardsOff);
                boards = WindowPart(m, "Boards", root.transform, l.P(o.Pos, SillHeight + 0.6f, face * (Half + BoardsOff)), ShadowCastingMode.On);
            }

            RaidRoom room = RoomAt(RunPoint(r, o.Pos, 0f, roomSide * 0.6f));
            RaidWindow.Dressing dressing = DressingFor(o, room, roomSide, outside);
            Renderer dressedBack = null, dressedAcross = null;
            if (dressing != RaidWindow.Dressing.None)
            {
                var back = new RaidMesh();
                var across = new RaidMesh();
                if (dressing == RaidWindow.Dressing.Curtains)
                {
                    RoomExtent(r, roomSide, o.Pos, out float lo, out float hi);
                    FurnitureBuilder.OpeningCurtains(back, across, l, o.Pos, o.Width, SillHeight, HeadHeight, Half, lo, hi, roomSide, CurtainFabricFor(room));
                }
                else
                {
                    bool cloth = dressing == RaidWindow.Dressing.Cloth;
                    Color stuff = cloth ? (Rng.NextDouble() < 0.5 ? new Color(0.24f, 0.26f, 0.16f) : new Color(0.10f, 0.10f, 0.11f))
                        : Kind == RaidBuildingKind.Clinic ? new Color(0.80f, 0.84f, 0.78f) : new Color(0.82f, 0.78f, 0.66f);
                    FurnitureBuilder.OpeningBlind(back, across, l, o.Pos, o.Width - 0.068f, SashBottom, HeadHeight, BlindAcross, roomSide, stuff, cloth);
                }
                Vector3 origin = RunPoint(r, o.Pos, 0f, 0f);
                dressedBack = WindowPart(back, "DressingOpen", root.transform, origin, ShadowCastingMode.On);
                dressedAcross = WindowPart(across, "DressingDrawn", root.transform, origin, ShadowCastingMode.On);
            }
            DecorPalette.Apply();

            // A fresh raid: a whole window mostly shut and latched; its curtains drawn now and then.
            bool open = glazed && Rng.NextDouble() < CloneZConfig.RaidWindowOpenChance;
            bool unlatched = glazed && (open || Rng.NextDouble() >= CloneZConfig.RaidWindowLatchedChance);
            bool drawn = dressing != RaidWindow.Dressing.None && Rng.NextDouble() < CloneZConfig.RaidWindowDrawnChance;

            var setup = new HomeWindow.Setup
            {
                Name = room != null ? OpeningRoomTitle(room.Name) + " window" : "Window",
                Sash = sash,
                CurtainsOpen = dressedBack,
                CurtainsDrawn = dressedAcross,
                LatchShut = latchShut,
                LatchOpen = latchOpen,
                Screen = new Bounds(RunPoint(r, o.Pos, (SashBottom + SashTop) * 0.5f, 0f), RunSize(r, sashW, SashTop - SashBottom, 0.02f)),
                Point = RunPoint(r, o.Pos, (SillHeight + HeadHeight) * 0.5f, 0f),
                Normal = RunNormal(r, 1),
                Along = r.AlongX ? Vector3.right : Vector3.forward,
                OutSide = outside,
                Blind = dressing == RaidWindow.Dressing.Blind || dressing == RaidWindow.Dressing.Cloth,
                Open = open,
                Drawn = drawn,
                Unlatched = unlatched,
                Occluder = segment.GetComponent<CutawayOccluder>(),
            };
            var raid = new RaidWindow.Raid
            {
                Glass = glazed ? RaidWindow.Glazing.Intact : RaidWindow.Glazing.Smashed,
                Boarded = o.Boarded,
                Dressing = dressing,
                Upper = upperGlass,
                Lower = lowerGlass,
                Boards = boards,
                Line = l,
                Mid = o.Pos,
                SashWidth = sashW,
                UpperY0 = SashMeet - 0.02f,
                UpperY1 = SashTop,
                UpperAcross = outer,
                LowerY0 = SashBottom,
                LowerY1 = SashMeet + 0.02f,
                LowerAcross = inner,
                Seed = seed,
            };
            RaidWindow.Attach(root, setup, raid);
        }

        /// <summary>The wood of the statewide map's window linings (its interiors' casing, a shade lighter, as the raid town's).</summary>
        private static readonly Color MapWindowWood = new Color(0.40f, 0.26f, 0.15f);

        /// <summary>The fixed part of a statewide map's working window, drawn into its wall's mesh as the building is raised (so it
        /// stands whether the floor is furnished or not, and the cutaway takes it down with the wall): the linings of the
        /// opening's reveal, and the upper sash in its outer track with the sash lock's keeper on its meeting rail.
        /// <paramref name="middle"/> is the opening's middle on the wall's centre line at its floor and <paramref name="inward"/>
        /// the way into the room; the rest as <see cref="FitWindowIn"/> takes them, which fits the parts that work.</summary>
        internal static void MapWindowFrame(RaidMesh m, Vector3 middle, Vector3 inward, float width, float sill, float head, float face)
        {
            Vector3 was = m.FrameOrigin;
            Quaternion wasTurn = m.FrameRotation;
            m.SetFrame(middle, Quaternion.LookRotation(inward, Vector3.up));
            var l = new OpeningLine(true, 0f);
            float sashW = width - 0.05f, cy = (sill + head) * 0.5f, top = head - 0.024f, meet = (sill + 0.03f + top) * 0.5f, depth = face * 2f + 0.01f;
            Surf wood = DecorPalette.Matte(MapWindowWood);
            for (int e = -1; e <= 1; e += 2) m.Box(l.P(e * (width * 0.5f - 0.012f), cy, 0f), l.Size(0.024f, head - sill, depth), wood);
            m.Box(l.P(0f, head - 0.012f, 0f), l.Size(width, 0.024f, depth), wood);
            m.Box(l.P(0f, sill + 0.015f, 0f), l.Size(width, 0.03f, depth), wood);
            FurnitureBuilder.OpeningSash(m, l, 0f, sashW, meet - 0.02f, top, -SashTrack, false, 1, SashPaint);
            m.Box(l.P(0f, meet + 0.034f, -SashTrack), l.Size(0.04f, 0.008f, 0.03f), DecorPalette.Metal(FurnitureBuilder.Brass));
            m.SetFrame(was, wasTurn);
        }

        /// <summary>A statewide map's working window, as <see cref="FitWindowIn"/> fits it.</summary>
        internal struct MapWindow
        {
            /// <summary>The opening's middle on the wall's centre line at the room's floor, and the way into the room.</summary>
            public Vector3 Middle, Inward;
            /// <summary>The opening's width, its bottom and top over the floor, and half the wall's thickness.</summary>
            public float Width, Sill, Head, Face;
            /// <summary>How far the room's wall runs either way from the middle (where a curtain rod must stop), and where along
            /// it from the middle the survivor reaches for the window (clear of a partition meeting the wall across it).</summary>
            public float Lo, Hi, Handle;
            /// <summary>The room it lights (null: none), what hangs there and the curtains' stuff.</summary>
            public RaidRoom Room;
            public RaidWindow.Dressing Dressing;
            public Color Fabric;
            /// <summary>How the town left it.</summary>
            public RaidWindow.Glazing Glass;
            public bool Boarded;
            /// <summary>Its own random stream: the same window every time it's fitted.</summary>
            public int Seed;
        }

        /// <summary>A curtain fabric for a room's windows on the statewide map, from the room's own seed.</summary>
        internal static Color MapCurtainFabric(int seed)
        {
            var rng = new System.Random(seed);
            return DecorPalette.Vary(CurtainFabrics[rng.Next(CurtainFabrics.Length)], 0.06f, rng);
        }

        /// <summary>
        /// The working parts of a statewide map's window, fitted in an opening through a wall at any angle whose fixed part
        /// (<see cref="MapWindowFrame"/>) the wall already has: the starter home's lower sash with its glass (or what's left of
        /// it), the upper sash's glass, the boards (only while it's boarded: they're made when they're first nailed on), what
        /// hangs on the room side, and the <see cref="RaidWindow"/> that works them, all under <paramref name="parent"/> —
        /// as few objects as will do, none casting shadows. Everything it rolls comes from the window's own seed, so a window
        /// taken out and fitted again (<see cref="WindowStreamer"/>) is the same window.
        /// </summary>
        internal static RaidWindow FitWindowIn(Transform parent, in MapWindow w, RaidBuildingKind kind)
        {
            var rng = new System.Random(w.Seed);
            Vector3 middle = w.Middle;
            float width = w.Width, sill = w.Sill, head = w.Head, face = w.Face;
            RaidWindow.Dressing dressing = w.Dressing;
            // Drawn in the wall's frame (x along it, z into the room), with the world as the meshes' space.
            Quaternion turn = Quaternion.LookRotation(w.Inward, Vector3.up);
            var l = new OpeningLine(true, 0f);
            const int roomSide = 1, outside = -1;
            float sashW = width - 0.05f, cy = (sill + head) * 0.5f;
            float bottom = sill + 0.03f, top = head - 0.024f, meet = (bottom + top) * 0.5f;
            float outer = -SashTrack, inner = SashTrack;
            bool glazed = w.Glass == RaidWindow.Glazing.Intact;
            Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);

            var root = new GameObject("Window");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(middle + Vector3.up * cy, Quaternion.identity);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            // The lower sash in the inner track, sliding up behind the upper one, its lock's body on the meeting rail (and a
            // smashed window's lever left turned on it: there's no working that sash any more).
            RaidMesh lower = InFrame(middle, turn);
            FurnitureBuilder.OpeningSash(lower, l, 0f, sashW, bottom, meet + 0.02f, inner, true, roomSide, SashPaint);
            lower.Box(l.P(0f, meet + 0.024f, inner), l.Size(0.05f, 0.008f, 0.032f), brass);
            if (!glazed) lower.Box(l.P(0f, meet + 0.042f, 0f), l.Size(0.02f, 0.008f, 0.06f), brass);
            float lift = Mathf.Max(0.1f, top - meet - 0.04f);
            FixturePart sash = FixturePart.Slide(root.transform, "LowerSash", lower, lower.ToMeshSpace(l.P(0f, bottom, inner)), Vector3.up * lift,
                ShadowCastingMode.Off).WithSpeed(CloneZConfig.WindowSashSeconds);

            // Each sash's glazing: glass, or the shards left in the frame. The lower one rides on its sash.
            int seed = rng.Next();
            var shards = new System.Random(seed);
            MeshFilter upperGlass = FramedGlazing(root.transform, "UpperGlazing", middle, turn, l, sashW, meet - 0.02f, top, outer, false, glazed, shards);
            MeshFilter lowerGlass = FramedGlazing(sash.transform, "LowerGlazing", middle, turn, l, sashW, bottom, meet + 0.02f, inner, true, glazed, shards);

            Renderer boards = null;
            if (w.Boarded)
            {
                RaidMesh planks = InFrame(middle, turn);
                FurnitureBuilder.OpeningBoards(planks, new System.Random(seed ^ RaidWindow.BoardsSalt), l, 0f, width, sill, outside, face + BoardsOff, head);
                boards = WindowPart(planks, "Boards", root.transform, planks.ToMeshSpace(l.P(0f, cy, outside * (face + BoardsOff))),
                    ShadowCastingMode.On);
            }

            Renderer dressedBack = null, dressedAcross = null;
            if (dressing != RaidWindow.Dressing.None)
            {
                RaidMesh back = InFrame(middle, turn), across = InFrame(middle, turn);
                if (dressing == RaidWindow.Dressing.Curtains)
                    FurnitureBuilder.OpeningCurtains(back, across, l, 0f, width, sill, head, face, w.Lo, w.Hi, roomSide, w.Fabric);
                else
                {
                    bool cloth = dressing == RaidWindow.Dressing.Cloth;
                    Color stuff = cloth ? (rng.NextDouble() < 0.5 ? new Color(0.24f, 0.26f, 0.16f) : new Color(0.10f, 0.10f, 0.11f))
                        : kind == RaidBuildingKind.Clinic ? new Color(0.80f, 0.84f, 0.78f) : new Color(0.82f, 0.78f, 0.66f);
                    FurnitureBuilder.OpeningBlind(back, across, l, 0f, width - 0.068f, bottom, head, BlindAcross, roomSide, stuff, cloth);
                }
                dressedBack = WindowPart(back, "DressingOpen", root.transform, middle, ShadowCastingMode.Off);
                dressedAcross = WindowPart(across, "DressingDrawn", root.transform, middle, ShadowCastingMode.Off);
            }
            DecorPalette.Apply();

            // As it was left: a whole window mostly shut and latched; its curtains drawn now and then.
            bool open = glazed && rng.NextDouble() < CloneZConfig.RaidWindowOpenChance;
            bool unlatched = glazed && (open || rng.NextDouble() >= CloneZConfig.RaidWindowLatchedChance);
            bool drawn = dressing != RaidWindow.Dressing.None && rng.NextDouble() < CloneZConfig.RaidWindowDrawnChance;
            RaidRoom room = w.Room;
            float handle = w.Handle;
            bool boarded = w.Boarded;

            var setup = new HomeWindow.Setup
            {
                Name = room != null ? OpeningRoomTitle(room.Name) + " window" : "Window",
                Sash = sash,
                CurtainsOpen = dressedBack,
                CurtainsDrawn = dressedAcross,
                Screen = new Bounds(middle + Vector3.up * ((bottom + top) * 0.5f), new Vector3(sashW, top - bottom, 0.02f)),
                ScreenTurn = turn,
                Point = middle + turn * Vector3.right * handle + Vector3.up * cy,
                Normal = w.Inward,
                Along = turn * Vector3.right,
                OutSide = outside,
                Blind = dressing == RaidWindow.Dressing.Blind || dressing == RaidWindow.Dressing.Cloth,
                Open = open,
                Drawn = drawn,
                Unlatched = unlatched,
                Floor = middle.y,
            };
            var raid = new RaidWindow.Raid
            {
                Glass = glazed ? RaidWindow.Glazing.Intact : RaidWindow.Glazing.Smashed,
                Boarded = boarded,
                Dressing = dressing,
                Upper = upperGlass,
                Lower = lowerGlass,
                Boards = boards,
                Line = l,
                Mid = 0f,
                SashWidth = sashW,
                UpperY0 = meet - 0.02f,
                UpperY1 = top,
                UpperAcross = outer,
                LowerY0 = bottom,
                LowerY1 = meet + 0.02f,
                LowerAcross = inner,
                Seed = seed,
                Framed = true,
                FrameAt = middle,
                FrameTurn = turn,
                Boardable = true,
                OpeningWidth = width,
                Sill = sill,
                Head = head,
                BoardsOff = face + BoardsOff,
                BoardsSide = outside,
            };
            return RaidWindow.Attach(root, setup, raid);
        }

        /// <summary>A window in an opening a partition walls off from inside (its room stops short of the wall there), as
        /// <see cref="FitWindowIn"/> takes them, whose fixed part the wall has (<see cref="MapWindowFrame"/>): its lower sash
        /// shut, and both sashes' glass, nothing that works. Returns its root.</summary>
        internal static GameObject FixedWindowIn(Transform parent, Vector3 middle, Vector3 inward, float width, float sill, float head)
        {
            Quaternion turn = Quaternion.LookRotation(inward, Vector3.up);
            var l = new OpeningLine(true, 0f);
            float sashW = width - 0.05f, cy = (sill + head) * 0.5f, bottom = sill + 0.03f, top = head - 0.024f, meet = (bottom + top) * 0.5f;
            var root = new GameObject("Window (walled off)");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(middle + Vector3.up * cy, Quaternion.identity);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            RaidMesh m = InFrame(middle, turn);
            FurnitureBuilder.OpeningSash(m, l, 0f, sashW, bottom, meet + 0.02f, SashTrack, true, 1, SashPaint);
            FurnitureBuilder.OpeningPane(m, l, 0f, sashW, bottom, meet + 0.02f, SashTrack, true, PaneTint);
            FurnitureBuilder.OpeningPane(m, l, 0f, sashW, meet - 0.02f, top, -SashTrack, false, PaneTint);
            WindowPart(m, "Sashes", root.transform, middle, ShadowCastingMode.Off);
            DecorPalette.Apply();
            return root;
        }

        // What hangs at a window, by what the building is: a house's curtains (a blind where furniture crowds the wall
        // beside it), an office's or a clinic's blind, the depot office's too, an army post's blackout cloth (or nothing),
        // nothing in a shop front.
        // Determine whether the window should have curtains or a blind based on building type and surrounding space.
        private RaidWindow.Dressing DressingFor(Opening o, RaidRoom room, int roomSide, int outside)
        {
            if (room == null || outside == 0) return RaidWindow.Dressing.None;
            switch (Kind)
            {
                case RaidBuildingKind.House:
                    return CurtainsFit(o, room, roomSide) ? RaidWindow.Dressing.Curtains : RaidWindow.Dressing.Blind;
                case RaidBuildingKind.Office:
                case RaidBuildingKind.Clinic:
                    return RaidWindow.Dressing.Blind;
                case RaidBuildingKind.Army:
                    return Rng.NextDouble() < 0.55 ? RaidWindow.Dressing.Cloth : RaidWindow.Dressing.None;
                case RaidBuildingKind.Depot:
                    return room.Name.ToLowerInvariant().Contains("office") ? RaidWindow.Dressing.Blind : RaidWindow.Dressing.None;
                default:
                    return RaidWindow.Dressing.None;
            }
        }

        // True when the strip of wall the curtains hang over (the window and a stride either side) is clear of furniture.
        private bool CurtainsFit(Opening o, RaidRoom room, int roomSide)
        {
            Vector3 a = RunPoint(o.Run, o.A - 0.32f, 0f, roomSide * Half);
            Vector3 b = RunPoint(o.Run, o.B + 0.32f, 0f, roomSide * (Half + 0.16f));
            Rect strip = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
            return !room.Overlaps(strip, false);
        }

        // The room's inner extent along the run at `along`, on `side` (where a curtain rod must stop short of the corners).
        private void RoomExtent(WallRun r, int side, float along, out float lo, out float hi)
        {
            lo = r.A;
            hi = r.B;
            RoomsOnSide(r, side, _sideRooms);
            foreach (var sr in _sideRooms)
                if (along > sr.a && along < sr.b) { lo = sr.a; hi = sr.b; }
        }

        // One fabric for every window of a room.
        private Color CurtainFabricFor(RaidRoom room)
        {
            if (room == null) return CurtainFabrics[0];
            if (!_curtainFabric.TryGetValue(room, out Color c))
                _curtainFabric[room] = c = DecorPalette.Vary(CurtainFabrics[Rng.Next(CurtainFabrics.Length)], 0.06f, Rng);
            return c;
        }

        // A sash's glazing as an object of its own (so it can break): glass, or the shards left in the frame.
        private static MeshFilter WindowGlazing(Transform parent, string name, in OpeningLine l, float mid, float w, float y0, float y1,
            float across, bool lower, bool glazed, System.Random rng)
        {
            var m = new RaidMesh();
            if (glazed) FurnitureBuilder.OpeningPane(m, l, mid, w, y0, y1, across, lower, PaneTint);
            else FurnitureBuilder.OpeningShards(m, rng, l, mid, w, y0, y1, across, lower);
            GameObject go = m.Emit(name, parent, l.P(mid, (y0 + y1) * 0.5f, across), ShadowCastingMode.Off);
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;
            return go.GetComponent<MeshFilter>();
        }

        // A small box (a sash lock's lever) as an object of its own under `parent`, to be shown or hidden.
        private static Renderer WindowBox(Transform parent, string name, Vector3 centre, Vector3 size, in Surf s)
        {
            var m = new RaidMesh();
            m.Box(centre, size, s);
            return WindowPart(m, name, parent, centre, ShadowCastingMode.Off);
        }

        // World-axis geometry emitted as its own object under `parent`, returning its renderer.
        private static Renderer WindowPart(RaidMesh m, string name, Transform parent, Vector3 origin, ShadowCastingMode shadows)
        {
            GameObject go = m.Emit(name, parent, origin, shadows);
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;
            return go.GetComponent<Renderer>();
        }

        // A mesh drawn in a window's turned frame (FitWindowIn): from `at`, x along the wall and z into the room, with the
        // world as its mesh space.
        private static RaidMesh InFrame(Vector3 at, Quaternion turn)
        {
            var m = new RaidMesh();
            m.SetFrame(at, turn);
            return m;
        }

        // A sash's glazing in a window's turned frame, as an object of its own (so it can break): glass, or the shards left.
        private static MeshFilter FramedGlazing(Transform parent, string name, Vector3 at, Quaternion turn, in OpeningLine l, float w,
            float y0, float y1, float across, bool lower, bool glazed, System.Random rng)
        {
            RaidMesh m = InFrame(at, turn);
            if (glazed) FurnitureBuilder.OpeningPane(m, l, 0f, w, y0, y1, across, lower, PaneTint);
            else FurnitureBuilder.OpeningShards(m, rng, l, 0f, w, y0, y1, across, lower);
            GameObject go = m.Emit(name, parent, m.ToMeshSpace(l.P(0f, (y0 + y1) * 0.5f, across)), ShadowCastingMode.Off);
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;
            return go.GetComponent<MeshFilter>();
        }
    }
}
