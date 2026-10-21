using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The raid town's lights, switches and wall decor, working as the starter home's do. Every room's ceiling fittings hang
    /// on a circuit of the room's own (<see cref="CircuitOf"/>; switched on to begin with when the room is
    /// <see cref="RaidRoom.Lit"/>), worked by a wall switch just inside the room's main doorway, on whichever side is clear
    /// of furniture and of the door's open leaf; now and then a fitting's bulb has gone. Desk and bedside lamps have switches
    /// of their own (<see cref="LampLight"/>). A house has a carriage lantern beside its front door on a porch circuit with a
    /// dusk sensor, worked from a second toggle on the switch inside the door. The wall clocks tell the time and the posters
    /// can be looked at and straightened. Switches, lanterns, clocks and posters hang under their wall segments, so they hide
    /// with the wall when the camera cuts it away, and the vines and grime keep clear of them.
    /// </summary>
    internal sealed partial class RaidBuilding
    {
        private const float SwitchHeight = 1.28f;      // as the home's: a little above a chair rail
        private const float ToggleTilt = 35f;          // degrees a toggle stands off level: down when off, up when on
        private const float ToggleSpacing = 0.06f;     // between the toggles of a double switch
        private const float SwitchGap = 0.15f;         // from a door's casing to the near edge of its switch plate..
        private const float SwitchSlide = 0.75f;       // .. or as much farther along the wall as it takes to clear an open leaf
        private const float SwitchBoxDepth = 0.06f;    // a tent's switch box
        private const float SwitchWideDoorway = 2.4f;  // a wider opening (a roll-up door, an archway) is a last resort for a switch
        private const float PorchLanternHeight = 1.85f;

        private readonly Dictionary<RaidRoom, string> _circuits = new Dictionary<RaidRoom, string>();
        private readonly Dictionary<RaidRoom, List<HomeLamp>> _roomLamps = new Dictionary<RaidRoom, List<HomeLamp>>();
        private readonly List<WallFitting> _wallFittings = new List<WallFitting>();
        private readonly List<(RaidRoom room, float a, float b)> _lightSides = new List<(RaidRoom, float, float)>();

        // A wall fitting the vines and grime keep clear of: its centre on a wall face, the way it faces and its half size.
        private struct WallFitting
        {
            public Vector3 At, Normal;
            public float HalfWidth, HalfHeight;
        }

        // Where a room's switch goes: its wall segment, the plate's centre on the wall face, the way it faces, the way along
        // the wall away from the door, whether furniture (or the door's open leaf) stands there, how far it had to slide
        // along the wall to get clear, and the wall to spare beyond it.
        private struct SwitchSpot
        {
            public WallPiece Piece;
            public Vector3 Face, Normal, Away;
            public bool Blocked;
            public float Slide, Spare;
        }

        #region Circuits
        /// <summary>The lighting circuit of <paramref name="room"/> ("raid:House_Elm:Kitchen"), wired up on first use: its light
        /// named for the room in prompts ("kitchen light") and switched on to begin with when the room is <see cref="RaidRoom.Lit"/>.
        /// Every ceiling fitting of the room hangs on it and the switch by the room's door works it; anything else that should
        /// light with the room can hang on it too (<see cref="HomeLamp.Attach"/>'s circuit).</summary>
        public string CircuitOf(RaidRoom room)
        {
            if (room == null) return null;
            if (_circuits.TryGetValue(room, out string circuit)) return circuit;
            circuit = "raid:" + Name + ":" + room.Name;
            if (_circuits.ContainsValue(circuit)) circuit += "#" + Rooms.IndexOf(room);
            LightCircuits.Define(circuit, RoomNoun(room.Name) + " light", room.Lit);
            _circuits[room] = circuit;
            return circuit;
        }

        // A room's name as it reads in a sentence: "Living Room" → "living room", "CO Office" → "CO office".
        private static string RoomNoun(string name)
        {
            if (string.IsNullOrEmpty(name)) return "room";
            string[] words = name.Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length < 2 || words[i] != words[i].ToUpperInvariant()) words[i] = words[i].ToLowerInvariant();
            return string.Join(" ", words);
        }

        // The ceiling fittings on a room's circuit.
        private List<HomeLamp> RoomLamps(RaidRoom room)
        {
            if (!_roomLamps.TryGetValue(room, out List<HomeLamp> lamps)) _roomLamps[room] = lamps = new List<HomeLamp>(2);
            return lamps;
        }

        // Now and then a fitting's bulb has gone; a room that's lit to begin with keeps at least one that works.
        private void BlowSomeBulbs()
        {
            foreach (RaidRoom room in Rooms)
            {
                if (!_roomLamps.TryGetValue(room, out List<HomeLamp> lamps)) continue;
                int working = lamps.Count;
                for (int i = 0; i < lamps.Count; i++)
                {
                    if (Rng.NextDouble() >= CloneZConfig.RaidDeadBulbChance || (room.Lit && working <= 1)) continue;
                    lamps[i].DeadBulb = true;
                    working--;
                }
            }
        }
        #endregion

        #region Lamps
        /// <summary>An articulated desk lamp (<see cref="FurnitureBuilder.DeskLamp"/>) drawn unlit into a piece's mesh
        /// <paramref name="m"/> at <paramref name="at"/> — the piece's own frame, as a draw callback has it — with its bulb's
        /// glow shown only while it's lit and a switch of its own (<see cref="LampLight"/>). It starts <paramref name="on"/>;
        /// one left off may turn out to have a dead bulb. Returns the bulb in mesh space, as FurnitureBuilder.DeskLamp does.</summary>
        public Vector3 DeskLamp(RaidMesh m, System.Random r, Vector3 at, float yaw, Color shade, bool on, bool shadows)
        {
            Vector3 bulb = FurnitureBuilder.DeskLamp(m, r, at, yaw, shade, false);
            var glow = new RaidMesh();
            glow.SetFrame(m.FrameOrigin, m.FrameRotation);
            FurnitureBuilder.DeskLampGlow(glow, at, yaw);
            glow.ResetFrame();
            HomeLamp lamp = FreeStandingLamp(WorldOf(m, bulb), shadows, on, glow, "Desk lamp");
            if (!on && Rng.NextDouble() < CloneZConfig.RaidDeadBulbChance) lamp.DeadBulb = true;
            return bulb;
        }

        /// <summary>A bedside table with its lamp (<see cref="FurnitureBuilder.Nightstand"/>) drawn unlit into a piece's mesh
        /// <paramref name="m"/> (the nightstand's own frame), the shade's glow shown only while the lamp is lit, with a switch
        /// of its own. It starts <paramref name="on"/>; one left off may turn out to have a dead bulb.</summary>
        public void BedsideTable(RaidMesh m, System.Random r, Color wood, bool on)
        {
            Vector3 bulb = FurnitureBuilder.Nightstand(m, r, wood, false);
            var glow = new RaidMesh();
            glow.SetFrame(m.FrameOrigin, m.FrameRotation);
            FurnitureBuilder.NightstandGlow(glow);
            glow.ResetFrame();
            HomeLamp lamp = FreeStandingLamp(WorldOf(m, bulb), false, on, glow, "Bedside lamp");
            if (!on && Rng.NextDouble() < CloneZConfig.RaidDeadBulbChance) lamp.DeadBulb = true;
        }

        // A free-standing lamp — its light, its glow and its own switch — under the piece being drawn, or the building.
        private HomeLamp FreeStandingLamp(Vector3 bulb, bool shadows, bool startOn, RaidMesh glow, string name)
        {
            Transform body = CurrentBody;
            bool flicker = Rng.NextDouble() < CloneZConfig.RaidLampFlickerChance;
            // On static furniture the reach mustn't snag on the building's merged furniture colliders (walls still stop it).
            HomeLamp lamp = HomeLamp.Attach(body != null ? body : Root, bulb, CloneZConfig.RaidLampRange, CloneZConfig.RaidLampIntensity,
                shadows, glow, displayName: name ?? LampNameFor(body), color: DeskLampColor, furniture: body != null ? body : StaticColliderRoot());
            lamp.Flicker = flicker;
            lamp.SetSwitchedOn(startOn);
            Town.LightCount++;
            return lamp;
        }

        // The object carrying the building's static furniture colliders (made here when no piece has needed one yet).
        private Transform StaticColliderRoot()
        {
            if (_colliders == null)
            {
                _colliders = new GameObject("Colliders");
                _colliders.transform.SetParent(Root, false);
            }
            return _colliders.transform;
        }

        private static string LampNameFor(Transform body)
        {
            string n = body != null ? body.name.ToLowerInvariant() : "";
            if (n.Contains("nightstand") || n.Contains("bedside")) return "Bedside lamp";
            if (n.Contains("desk") || n.Contains("table")) return "Desk lamp";
            return "Lamp";
        }
        #endregion

        #region Switches and the porch lantern
        // From DecorateWalls, once the door leaves are hung and before the grime goes on: the room switches, a house's porch
        // lantern, and the bulbs that have gone.
        private void PlanWallLighting()
        {
            _wallFittings.Clear();
            Opening front = Kind == RaidBuildingKind.House ? PorchDoorway() : null;
            string porch = front != null ? PlanPorchLantern(front) : null;
            RaidRoom frontRoom = front != null ? RoomBehindDoor(front) : null;
            foreach (RaidRoom room in Rooms)
            {
                bool withPorch = porch != null && room == frontRoom;
                PlanSwitch(room, withPorch ? front : null, withPorch ? new[] { CircuitOf(room), porch } : new[] { CircuitOf(room) });
            }
            BlowSomeBulbs();
        }

        // A room's switch just inside its main doorway (the front door, for the room behind it), its plate or box drawn into
        // the wall; the fixture and its toggles hang under the segment once it's an object.
        private void PlanSwitch(RaidRoom room, Opening main, string[] circuits)
        {
            bool box = Kind == RaidBuildingKind.Tent;
            if (box && circuits.Length > 1) circuits = new[] { circuits[0] };
            int toggles = circuits.Length;
            float width = box ? 0.11f : (toggles - 1) * ToggleSpacing + 0.07f;
            if (!PickSwitchSpot(room, main, width, out SwitchSpot spot)) return;

            Vector3 n = spot.Normal;
            // The first circuit's toggle farthest from the door, the last nearest it, as the home has them.
            var at = new Vector3[toggles];
            for (int i = 0; i < toggles; i++) at[i] = spot.Face + spot.Away * (((toggles - 1) * 0.5f - i) * ToggleSpacing);
            RaidMesh m = spot.Piece.Mesh;
            Vector3 lever = default;
            if (box)
            {
                lever = FurnitureBuilder.SwitchBox(m, spot.Face, n, SwitchBoxDepth, WallH - 0.02f);
                // The box and the cable running up from it to the top of the canvas.
                float lo = spot.Face.y - 0.1f;
                KeepWallClear(new Vector3(spot.Face.x, (lo + WallH) * 0.5f, spot.Face.z), n, width * 0.5f + 0.04f, (WallH - lo) * 0.5f);
            }
            else
            {
                FurnitureBuilder.SwitchPlate(m, spot.Face, n, width);
                for (int i = 0; i < toggles; i++) FurnitureBuilder.SwitchSlot(m, at[i], n);
                KeepWallClear(spot.Face, n, width * 0.5f + 0.04f, 0.08f);
            }
            WallPiece piece = spot.Piece;
            Vector3 face = spot.Face;
            string name = box ? "Switch box" : toggles > 1 ? "Light switches" : "Light switch";
            Later(() => HangSwitch(piece, face, n, at, circuits, name, box, lever));
        }

        // The switch's fixture and its toggles (a switch box's lever), under the wall segment it's on.
        private void HangSwitch(WallPiece piece, Vector3 face, Vector3 n, Vector3[] at, string[] circuits, string name, bool box, Vector3 lever)
        {
            LightSwitch sw = LightSwitch.Create(ObjectOf(piece), name, face + n * ((box ? SwitchBoxDepth : 0f) + 0.03f), face, n, circuits);
            Vector3 axis = Vector3.Cross(n, Vector3.up);
            var toggles = new FixturePart[circuits.Length];
            for (int i = 0; i < toggles.Length; i++)
            {
                Vector3 pivot = box ? lever : at[i] + n * 0.009f;
                var mesh = new RaidMesh();
                if (box) FurnitureBuilder.BreakerHandle(mesh, pivot, n, ToggleTilt);
                else FurnitureBuilder.SwitchToggle(mesh, pivot, n, ToggleTilt);
                toggles[i] = FixturePart.Hinge(sw.transform, "Toggle", mesh, pivot, axis, ToggleTilt * 2f, ShadowCastingMode.Off).WithSpeed(box ? 0.12f : 0.08f);
            }
            sw.SetToggles(toggles);
        }

        // The spot for a room's switch: beside the main doorway, else beside the room's other doorways in the order they were
        // made (wide openings last), on a side clear of furniture and of the door's open leaf — the main doorway's even when
        // it isn't clear, since its switch works the porch light too. Any doorway's blocked spot beats no switch at all.
        private bool PickSwitchSpot(RaidRoom room, Opening main, float width, out SwitchSpot spot)
        {
            spot = default;
            bool fallback = false;
            for (int pass = 0; pass < 3; pass++)
            {
                if (pass == 0 && main == null) continue;
                foreach (Opening o in _openings)
                {
                    if (o.Kind != OpeningKind.Door) continue;
                    if (pass == 0 ? o != main : (o == main || (o.Width > SwitchWideDoorway) != (pass == 2))) continue;
                    int side = DoorSideFacing(o, room);
                    if (side == 0 || !BestSwitchBeside(room, o, side, width, out SwitchSpot s)) continue;
                    if (!s.Blocked || pass == 0)
                    {
                        spot = s;
                        return true;
                    }
                    if (!fallback)
                    {
                        spot = s;
                        fallback = true;
                    }
                }
            }
            return fallback;
        }

        // The better of the two spots beside a doorway on the room's side — clear before blocked, then the one nearer the door,
        // then the one with more wall to spare — or round the corner when the door leaves no wall beside it.
        private bool BestSwitchBeside(RaidRoom room, Opening o, int side, float width, out SwitchSpot best)
        {
            best = default;
            bool any = false;
            for (int round = 0; round < 2 && !any; round++)
                for (int e = -1; e <= 1; e += 2)
                {
                    SwitchSpot s;
                    bool ok = round == 0 ? SwitchBesideDoor(room, o, side, e, width, out s) : SwitchRoundCorner(room, o, side, e, width, out s);
                    if (!ok) continue;
                    bool better = s.Blocked != best.Blocked ? !s.Blocked
                        : Mathf.Abs(s.Slide - best.Slide) > 0.01f ? s.Slide < best.Slide : s.Spare > best.Spare;
                    if (!any || better) best = s;
                    any = true;
                }
            return any;
        }

        // A spot on the doorway's own wall at end e (-1: the A end, +1: the B end), a hand's width past the casing.
        private bool SwitchBesideDoor(RaidRoom room, Opening o, int side, int e, float width, out SwitchSpot s)
        {
            s = default;
            WallRun r = o.Run;
            if (!RoomStretchOn(r, side, room, o.Pos, out float ra, out float rb)) return false;
            float casing = DoorFrames ? 0.07f : 0f;
            float c = e < 0 ? o.A - casing - SwitchGap - width * 0.5f : o.B + casing + SwitchGap + width * 0.5f;
            return SlideSwitch(room, o, r, side, e, c, ra, rb, width, out s);
        }

        // A spot round the corner from the doorway's end e, on the room's wall that meets the doorway's, a hand's width out
        // from the corner — for a door that leaves no wall beside it (a narrow bathroom's).
        private bool SwitchRoundCorner(RaidRoom room, Opening o, int side, int e, float width, out SwitchSpot s)
        {
            s = default;
            WallRun r = o.Run;
            if (!RoomStretchOn(r, side, room, o.Pos, out float ra, out float rb)) return false;
            float casing = DoorFrames ? 0.07f : 0f;
            if ((e < 0 ? o.A - casing - ra : rb - o.B - casing) > SwitchGap + width + 0.1f) return false;
            float line = e < 0 ? ra - Half : rb + Half;                         // the meeting wall's centre line
            float along = r.Coord + side * (Half + SwitchGap + width * 0.5f);   // along the meeting wall, out from the door's
            WallRun wall = null;
            foreach (WallRun w in _runs)
                if (w.AlongX != r.AlongX && Mathf.Abs(w.Coord - line) < 0.06f && along > w.A && along < w.B) { wall = w; break; }
            if (wall == null) return false;
            int wside = e < 0 ? 1 : -1;   // the room lies on the doorway's side of the meeting wall
            if (!RoomStretchOn(wall, wside, room, along, out float wa, out float wb)) return false;
            if (!SlideSwitch(room, null, wall, wside, side, along, wa, wb, width, out s)) return false;
            s.Spare = 0f;
            return true;
        }

        // The first clear spot for a switch plate on run r's face (on the room's side), starting at c and sliding on along the
        // wall (the way away points) past whatever stands in front of it — the door's own open leaf, more often than not — but
        // no farther than the room's corner, another opening or SwitchSlide. A blocked start is kept when nothing past it is clear.
        private bool SlideSwitch(RaidRoom room, Opening door, WallRun r, int side, int away, float c, float ra, float rb, float width,
            out SwitchSpot s)
        {
            s = default;
            bool found = false;
            float half = width * 0.5f, y = SwitchY(room);
            Vector3 n = RunNormal(r, side);
            for (float slide = 0f; slide <= SwitchSlide + 0.001f; slide += 0.05f)
            {
                float at = c + away * slide;
                // Clear of the corner (far enough from the next wall that the plate is plainly this wall's) and of other openings.
                if (at - half < ra + 0.1f || at + half > rb - 0.1f || OpeningWithin(r, door, at, half + 0.04f, WindowClearance)) break;
                WallPiece piece = PlainSegmentAt(r, at);
                if (piece == null) break;
                Vector3 face = RunPoint(r, at, y, side * (Half + 0.004f));
                bool blocked = SpotBlocked(room, face, n, half);
                if (found && blocked) continue;
                s.Piece = piece;
                s.Face = face;
                s.Normal = n;
                s.Away = (r.AlongX ? Vector3.right : Vector3.forward) * away;
                s.Blocked = blocked;
                s.Slide = slide;
                s.Spare = away < 0 ? at - ra : rb - at;
                found = true;
                if (!blocked) break;
            }
            return found && RoomAt(s.Face + n * 0.3f) == room;
        }

        // The room's stretch of a wall run on one side (the inner faces of its end walls), around a point along the run.
        private bool RoomStretchOn(WallRun r, int side, RaidRoom room, float along, out float a, out float b)
        {
            RoomsOnSide(r, side, _lightSides);
            foreach (var sr in _lightSides)
                if (sr.room == room && along > sr.a - 0.3f && along < sr.b + 0.3f)
                {
                    a = sr.a;
                    b = sr.b;
                    return true;
                }
            a = b = 0f;
            return false;
        }

        // How far past a window's opening a switch keeps clear: its casing, or in a house the curtains gathered back beside it
        // (see FurnitureBuilder.OpeningCurtains).
        private float WindowClearance => Kind == RaidBuildingKind.House ? 0.24f : 0.09f;

        // True when another opening in run r — with its casing (a window's with its curtains, windowMargin past the opening) —
        // comes within half of c.
        private bool OpeningWithin(WallRun r, Opening except, float c, float half, float windowMargin)
        {
            foreach (Opening other in r.Openings)
            {
                if (other == except) continue;
                float margin = other.Kind == OpeningKind.Door ? (DoorFrames ? 0.07f : 0f) : windowMargin;
                if (c + half > other.A - margin && c - half < other.B + margin) return true;
            }
            return false;
        }

        // The plain wall segment of run r at a point along it (null in an opening or off the run).
        private WallPiece PlainSegmentAt(WallRun r, float along)
        {
            foreach (WallPiece p in _pieces)
                if (p.Run == r && p.Opening == null && along >= p.S && along <= p.E) return p;
            return null;
        }

        // Furniture — or a door's open leaf, which PlaceLeaves marks as furniture — standing in front of a plate on the wall, or
        // a fitting already hung there (a poster, a clock, anything kept clear with KeepWallClear).
        private bool SpotBlocked(RaidRoom room, Vector3 face, Vector3 normal, float half)
        {
            if (WallFittingAt(face, normal, half + 0.02f, 0.08f)) return true;
            Vector3 along = Vector3.Cross(normal, Vector3.up);
            Vector3 p = face + along * (half + 0.02f), q = face - along * (half + 0.02f) + normal * 0.45f;
            Rect probe = Rect.MinMaxRect(Mathf.Min(p.x, q.x), Mathf.Min(p.z, q.z), Mathf.Max(p.x, q.x), Mathf.Max(p.z, q.z));
            for (int i = 0; i < room.Furniture.Count; i++)
                if (room.Furniture[i].Overlaps(probe)) return true;
            for (int i = 0; i < room.Reserved.Count; i++)
                if (room.Reserved[i].Overlaps(probe) && !IsDoorClearance(room.Reserved[i])) return true;
            return false;
        }

        // True for the floor kept clear in front of a doorway (as opposed to anything else a room reserves).
        private bool IsDoorClearance(Rect zone)
        {
            foreach (Opening o in _openings)
                if (o.Zones.Contains(zone)) return true;
            return false;
        }

        // The usual switch height, lifted clear of a chair rail that would run behind the plate.
        private float SwitchY(RaidRoom room)
        {
            float y = SwitchHeight, rail = room.Design.RailHeight;
            if (rail + 0.04f > y - 0.07f && rail - 0.02f < y + 0.07f) y = rail + 0.115f;
            return Mathf.Min(y, WallH - 0.3f);
        }

        // Which side of its run a doorway opens onto the room (+1 / -1), or 0 when the room is on neither.
        private int DoorSideFacing(Opening o, RaidRoom room)
        {
            if (RoomAt(RunPoint(o.Run, o.Pos, 0f, 0.6f)) == room) return 1;
            if (RoomAt(RunPoint(o.Run, o.Pos, 0f, -0.6f)) == room) return -1;
            return 0;
        }

        // A house's front door: the first outside door in its front (local -Z) wall.
        private Opening PorchDoorway()
        {
            Vector3 front = Dir(0f, -1f);
            foreach (Opening o in _openings)
            {
                if (o.Kind != OpeningKind.Door) continue;
                int outside = OutsideSide(o.Run, o.Pos);
                if (outside != 0 && Vector3.Dot(RunNormal(o.Run, outside), front) > 0.9f) return o;
            }
            return null;
        }

        // The room an outside door lets into.
        private RaidRoom RoomBehindDoor(Opening o)
        {
            int outside = OutsideSide(o.Run, o.Pos);
            return outside == 0 ? null : RoomAt(RunPoint(o.Run, o.Pos, 0f, -outside * 0.6f));
        }

        // A black carriage lantern on the wall beside a house's front door (on the side with more wall to spare), on the
        // house's porch circuit with a dusk sensor, as the starter home's — switched on, so at night it's lit. Returns the
        // porch circuit, or null when there's no wall for it.
        private string PlanPorchLantern(Opening door)
        {
            WallRun r = door.Run;
            int outside = OutsideSide(r, door.Pos);
            if (outside == 0) return null;
            float casing = DoorFrames ? 0.07f : 0f;
            float best = 0f, bestSpare = -1f;
            for (int e = -1; e <= 1; e += 2)
            {
                float c = e < 0 ? door.A - casing - 0.3f : door.B + casing + 0.3f;
                if (c - 0.12f < r.A + 0.15f || c + 0.12f > r.B - 0.15f || OpeningWithin(r, door, c, 0.12f, 0.09f) || PlainSegmentAt(r, c) == null) continue;
                // The wall to spare beyond: up to the run's end or the next opening.
                float limit = e < 0 ? r.A : r.B;
                foreach (Opening other in r.Openings)
                {
                    if (e < 0 && other.B <= door.A) limit = Mathf.Max(limit, other.B);
                    else if (e > 0 && other.A >= door.B) limit = Mathf.Min(limit, other.A);
                }
                float spare = Mathf.Abs(limit - c);
                if (spare > bestSpare)
                {
                    best = c;
                    bestSpare = spare;
                }
            }
            if (bestSpare < 0f) return null;

            string porch = "raid:" + Name + ":porch";
            LightCircuits.Define(porch, "porch light", true);
            Vector3 n = RunNormal(r, outside);
            Vector3 wall = RunPoint(r, best, PorchLanternHeight, outside * (Half + 0.004f));
            WallPiece piece = PlainSegmentAt(r, best);
            var glow = new RaidMesh();
            Vector3 lamp = FurnitureBuilder.CarriageLantern(piece.Mesh, glow, wall, n, true);
            KeepWallClear(wall + Vector3.up * 0.02f, n, 0.15f, 0.22f);
            Later(() =>
            {
                Transform host = ObjectOf(piece);
                HomeLamp lantern = HomeLamp.Attach(host, lamp, CloneZConfig.RaidPorchLightRange, CloneZConfig.RaidPorchLightIntensity,
                    CloneZConfig.RaidPorchLightShadows, glow, porch, duskSensor: true);
                OutdoorLantern.Create(host, lantern, lamp + n * 0.08f, wall, n);
                Town.LightCount++;
            });
            return porch;
        }
        #endregion

        #region Wall decor
        /// <summary>Marks a patch of wall face as taken by a fitting — centred on <paramref name="at"/> (on the face), facing
        /// <paramref name="normal"/>, <paramref name="halfWidth"/> × <paramref name="halfHeight"/> each way: the light switches
        /// placed at <see cref="Finish"/> slide past it, and the vines and grime keep off it. Posters, clocks, switches and
        /// porch lanterns mark their own; anything else hung on a wall before Finish (a cupboard, a notice board) can too.</summary>
        public void KeepWallClear(Vector3 at, Vector3 normal, float halfWidth, float halfHeight)
            => _wallFittings.Add(new WallFitting { At = at, Normal = normal, HalfWidth = halfWidth, HalfHeight = halfHeight });

        // True when a patch of wall face (centred on at, facing normal) would overlap a fitting marked with KeepWallClear.
        private bool WallFittingAt(Vector3 at, Vector3 normal, float halfWidth, float halfHeight)
        {
            Vector3 along = Vector3.Cross(normal, Vector3.up);
            for (int i = 0; i < _wallFittings.Count; i++)
            {
                WallFitting f = _wallFittings[i];
                if (Vector3.Dot(f.Normal, normal) < 0.9f) continue;
                Vector3 d = f.At - at;
                if (Mathf.Abs(Vector3.Dot(d, normal)) > 0.1f) continue;
                if (Mathf.Abs(Vector3.Dot(d, along)) > halfWidth + f.HalfWidth) continue;
                if (Mathf.Abs(d.y) > halfHeight + f.HalfHeight) continue;
                return true;
            }
            return false;
        }

        // True when a stretch of run r's face on one side — along c ± halfWidth, from y0 up to y1 — would cover a wall fitting.
        private bool CoversFitting(WallRun r, int side, float c, float halfWidth, float y0, float y1)
            => WallFittingAt(RunPoint(r, c, (y0 + y1) * 0.5f, side * (Half + 0.004f)), RunNormal(r, side), halfWidth, (y1 - y0) * 0.5f);

        // A poster's fixture under its wall segment, the frame and print its own object so the fixture can turn it level.
        private void HangPoster(Vector3 at, Vector3 n, float w, float h, int kind, float roll, Color frame, string description)
        {
            // Only a homely print steadies the nerves; a recruiting poster or a notice is just something to read.
            PictureFixture poster = PictureFixture.Create(WallObjectAt(at), at + n * 0.04f, at, n, roll, description, PosterNoun(kind), kind == 2);
            var mesh = new RaidMesh();
            FurnitureBuilder.PictureFrame(mesh, at, n, w, h, frame, kind);
            GameObject go = mesh.Emit("Poster", poster.transform, at, ShadowCastingMode.On);
            if (go == null) return;
            go.transform.rotation = Quaternion.identity;   // drawn in world axes
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            poster.SetFrame(go.transform);
        }

        // What a poster is called, by kind (see DecorTextures.Poster).
        private static string PosterNoun(int kind) => kind == 0 ? "poster" : kind == 1 ? "calendar" : kind == 2 ? "picture" : "notice";

        // What a poster shows, as it reads in the place it hangs (one of a few, from the building's stream).
        private string PosterText(int kind)
        {
            string[] texts;
            switch (kind)
            {
                case 0:
                    texts = Kind == RaidBuildingKind.Army ? ArmyPosters : Kind == RaidBuildingKind.Tent ? TentPosters : KeptArmyPosters;
                    break;
                case 1:
                    texts = Kind == RaidBuildingKind.Office ? OfficeCalendars : Kind == RaidBuildingKind.Depot ? DepotCalendars : Calendars;
                    break;
                case 2:
                    texts = Kind == RaidBuildingKind.House ? HomePrints : OfficePrints;
                    break;
                default:
                    texts = Kind == RaidBuildingKind.Clinic ? ClinicNotices : Kind == RaidBuildingKind.Store ? StoreNotices
                        : Kind == RaidBuildingKind.Office ? OfficeNotices : Kind == RaidBuildingKind.Depot ? DepotNotices : Notices;
                    break;
            }
            return texts[Rng.Next(texts.Length)];
        }

        private static readonly string[] ArmyPosters =
        {
            "A recruiting poster: a white star on a blue disc and U.S. ARMY in square white letters, the olive drab faded to the colour of weak tea.",
            "U.S. ARMY, over a white star. Somebody has given the star a pair of goggle eyes in biro.",
            "The Guard's old recruiting poster, curling at the corners. A drawing pin is missing, so the bottom lifts in every draught.",
            "A recruiting poster. Pencilled down the margin: a phone number, crossed out, and under it, \"ask for Dale\".",
            "A faded Army flyer, the bold lettering half‑eroded: \"JOIN THE RANKS\" beneath a sun‑bleached cartoon soldier holding a rifle.",
            "U.S. ARMY recruitment notice, torn at the edges, the paper yellowed, a handwritten phone number in the margin: 555‑0199.",
            "An old propaganda poster, the star dimmed, a slogan scrawled over the top: \"DEFEND FREEDOM\", the bottom smudged with rain.",
        };

        private static readonly string[] TentPosters =
        {
            "A U.S. ARMY recruiting poster pinned to the canvas, of all places \u2014 somebody's idea of cheering the tent up.",
            "U.S. ARMY. The damp has got into the paper, and the white star has gone blotchy and grey.",
        };

        private static readonly string[] KeptArmyPosters =
        {
            "An old U.S. ARMY recruiting poster, kept long after whoever pinned it up had signed on.",
            "U.S. ARMY, a white star on blue. A son's, maybe, or a father's \u2014 it's been up there for years.",
        };

        private static readonly string[] Calendars =
        {
            "A calendar turned to December 1993, a sunset over the hills above the dates. One square early in the month is blocked in red.",
            "December 1993, under a picture of mountains at sundown. One day is marked in red, with nothing written to say what for.",
        };

        private static readonly string[] OfficeCalendars =
        {
            "The county's free calendar, December 1993. Somebody has pencilled PAYDAY across a Friday and drawn a small smiling face beside it.",
            "December 1993. Every day up to the middle of the month is crossed off in neat blue ink. Then nothing.",
            "December 1993. The road‑clearing schedule for the county is pencilled in red for the first week, a tiny shovel doodle sits beside each day.",
            "December 1993. A heating‑assistance day is marked on the 12th, with a little furnace sketch and a note to check the oil.",
            "December 1993. The school’s snow‑day notice is handwritten on the 18th, a simple snowflake drawn in the margin.",
            "December 1993. A tractor‑maintenance reminder for the 22nd is scrawled in brown ink, a tiny gear symbol beside it.",
            "December 1993. The county fair has been postponed to March, a crossed‑out picture of a ride and “postponed” written underneath.",
            "December 1993. The town‑hall meeting agenda for the 27th is listed: road clearing contracts, winter heating assistance, and a postponed fair."
        };

        private static readonly string[] DepotCalendars =
        {
            "A parts supplier's calendar, December 1993: a mountain sunset over a grid of greasy thumbprints.",
            "December 1993, a tyre company's name across the top. The delivery days are ringed in red, the last of them twice.",
        };

        private static readonly string[] HomePrints =
        {
            "Hills at sundown in a frame from the dime store. It has hung there so long the wallpaper round it has faded.",
            "Blue ridges under an orange sky. A school photo is tucked into the corner of the frame \u2014 a gap-toothed kid, grinning.",
            "A mountain sunset, the kind sold with the frame already on. Whoever chose it must have liked looking at it over supper.",
            "A faded family portrait in a cracked frame, the caption reads ‘The Johnsons, 1972 – happy holidays’.",
            "A clipped newspaper article titled ‘1993 snowstorm leaves town buried’, the ink smudged and corners curled.",
            "A winter‑time countryside photograph: snow‑covered fields stretching to a lone barn under a pale sky.",
            "A retro advertisement for “Harper’s Tractor Co.” – a shiny green tractor with the slogan ‘Built to plow, built to last’.",
            "An old postcard of a snow-capped hill, its stamp and cancellation nearly erased by the years."
        };

        private static readonly string[] OfficePrints =
        {
            "A framed sunset over wooded ridges \u2014 the sort of view you hang where a window ought to be.",
            "A print of the hills at dusk, a size too big for its wall. Somebody's attempt at making the place homely.",
        };

        private static readonly string[] ClinicNotices =
        {
            "A yellow SAFETY FIRST notice with a green cross. The small print lists the warning signs of frostbite, then of hypothermia.",
            "SAFETY FIRST, over a green cross. Below it, a typed card: WASH YOUR HANDS. COVER YOUR COUGH. ASK AT THE DESK. Good advice, a little late.",
        };

        private static readonly string[] StoreNotices =
        {
            "A yellow SAFETY FIRST notice from the company that fills the pumps: no smoking on the forecourt, engines off while filling.",
            "SAFETY FIRST, it says, though a coupon has been taped over the bottom half: two hot dogs and a fountain drink, ninety-nine cents.",
            "A SAFETY FIRST notice, half hidden by a hand-lettered card: WINTER SPECIALS \u2014 ANTIFREEZE, ROCK SALT, HAND WARMERS. ASK AT REGISTER.",
        };

        private static readonly string[] OfficeNotices =
        {
            "The county's SAFETY FIRST notice: lift with your knees, report every spill, know where the first-aid kit is. Nobody ever knew where the first-aid kit was.",
            "SAFETY FIRST. Pinned beneath it, a memo about the fire exits, initialled by everyone on the floor.",
            "WINTER SAFETY NOTICE: icy steps may appear; wear proper boots, clear snow, and report any slick patches.",
            "FUEL STORAGE NOTICE: keep diesel containers sealed, clean spills, and never smoke near pumps.",
            "MORALE NOTICE: payday's Friday – collect your bonus, enjoy a coffee break, and thank a colleague.",
            "EMERGENCY PROCEDURE: fire alarm triggers lockdown; evacuate to the marked exits, then await the sweep.",
            "DAMPNESS WARNING: the office ceiling leaks after rain; report water stains, mop spills, and keep vents clear.",
            "WINTER MORAL BOOST: bring a hot cocoa to the break room – a warm mug beats the cold outside and keeps spirits bright.",
            "SAFETY REMINDER: check that fire extinguishers are mounted securely; the cold can cause seals to stiffen and hinder use.",
            "EQUIPMENT CHECK: all flashlight batteries must be swapped for fresh alkaline cells this week; old ones lose charge in sub‑zero temps.",
            "STOCK NOTICE: the pantry now has extra canned beans and powdered milk – take what you need, but keep some for neighbors.",
            "COMMUNITY UPDATE: the town hall meeting is Thursday at 7 pm in the school gym – agenda includes road clearing contracts and winter heating assistance."
        };

        private static readonly string[] DepotNotices =
        {
            "SAFETY FIRST, a green cross on yellow. A chalk tally below counts the days without an accident. It stops at 212.",
            "A SAFETY FIRST notice freckled with oil. Somebody has written \"GOGGLES, RAY\" across the bottom in marker.",
        };

        private static readonly string[] Notices =
        {
            "A yellow SAFETY FIRST notice with a green cross on it, its corners curling away from the wall.",
            "SAFETY FIRST. The rest of the notice has been torn off, which seems about right.",
        };
        #endregion
    }
}
