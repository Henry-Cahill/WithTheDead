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
        private static readonly Color PaneTint = new Color(0.10f, 0.13f, 0.15f);

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
            var root = new GameObject("WindowFittings");
            root.transform.SetParent(segment, false);
            root.transform.SetPositionAndRotation(RunPoint(r, o.Pos, 0f, 0f), Quaternion.identity);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            // The lower sash in the inner track, its lock's body on the meeting rail. A smashed window's lever is left turned
            // on it: there's no working that sash any more.
            var lower = new RaidMesh();
            FurnitureBuilder.OpeningSash(lower, l, o.Pos, sashW, SashBottom, SashMeet + 0.02f, inner, true, roomSide, SashPaint);
            lower.Box(l.P(o.Pos, SashMeet + 0.024f, inner), l.Size(0.05f, 0.008f, 0.032f), brass);
            // Small brass pull‑handle on the interior side of the lower sash

            if (!glazed) lower.Box(l.P(o.Pos, SashMeet + 0.042f, 0f), l.Size(0.02f, 0.008f, 0.06f), brass);
            FixturePart sash = FixturePart.Slide(root.transform, "LowerSash", lower, l.P(o.Pos, SashBottom, inner), Vector3.up * SashLift)
                .WithSpeed(CloneZConfig.WindowSashSeconds);
            // Small brass pull‑handle on the interior side of the lower sash
            WindowBox(sash.transform, "PullHandle", l.P(o.Pos + 0.025f, SashBottom + 0.12f, inner), new Vector3(0.018f, 0.018f, 0.006f), brass);
            Renderer latchShut = null, latchOpen = null;
            if (glazed)
            {
                latchShut = WindowBox(sash.transform, "LatchShut", l.P(o.Pos, SashMeet + 0.042f, 0f), l.Size(0.02f, 0.008f, 0.06f), brass);
                latchOpen = WindowBox(sash.transform, "LatchOpen", l.P(o.Pos + 0.03f, SashMeet + 0.032f, inner), l.Size(0.06f, 0.008f, 0.02f), brass);
            // tiny brass latch knob on the lever
            WindowBox(latchOpen.transform, "LatchKnob", l.P(o.Pos + 0.03f, SashMeet + 0.038f, inner + 0.011f), new Vector3(0.012f, 0.012f, 0.005f), brass);
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

        // What hangs at a window, by what the building is: a house's curtains (a blind where furniture crowds the wall
        // beside it), an office's or a clinic's blind, the depot office's too, an army post's blackout cloth (or nothing),
        // nothing in a shop front.
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
    }
}
