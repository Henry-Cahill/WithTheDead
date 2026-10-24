using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// One real building's interior raised from its plan (<see cref="InteriorPlan"/>), a step at a time: the partitions
    /// (a wall object to each stretch, with its collider, so the isometric camera can cut the ones in its way) and the floors,
    /// drawn on the footprint's own axes under a turned frame object so the boards and tiles run with the walls; the working
    /// doors (the raid town's <see cref="RaidDoor"/>s) in every doorway, the front door's too; a light switch by each room's
    /// door; then the rooms furnished one by one with the raid town's pieces (<see cref="RaidBuilding"/> on the building's own
    /// turn); then the furniture's meshes and the fixtures hung on the walls. Everything goes under one root under the site's
    /// world root (the NavMesh bake reads it) and the ceiling fittings under the site's overhead root; <see cref="Discard"/>
    /// takes it all down again. Built inactive and switched on at the end, so a half-built interior never shows.
    /// </summary>
    internal sealed class InteriorBuild
    {
        private const float Half = RaidBuilding.Half;
        private const float LeafClear = 0.026f, LeafMeet = 0.01f, LeafInset = 0.01f, LeafFoot = 0.03f, KnobHeight = 1.05f;
        private const float SwitchHeight = 1.28f, ToggleTilt = 35f;
        private const float OuterDoorHeight = 2.15f;

        private static readonly Color WallCore = new Color(0.22f, 0.21f, 0.20f);
        private static readonly Color[] FrontDoorPaints =
        {
            new Color(0.46f, 0.13f, 0.10f), new Color(0.17f, 0.27f, 0.21f), new Color(0.16f, 0.20f, 0.30f), new Color(0.80f, 0.76f, 0.64f),
        };

        public readonly InteriorPlan Plan;
        private readonly Transform _parent, _overheadParent;
        private readonly bool _hidden;

        public GameObject Root;
        public RaidTown Town;
        public RaidBuilding B;
        public Transform Frame;
        public readonly List<RaidRoom> Rooms = new List<RaidRoom>();
        public readonly List<InteriorPass.Piece> Pieces = new List<InteriorPass.Piece>();
        /// <summary>The meshes made for it (destroyed with it: meshes outlive their objects).</summary>
        public readonly List<Mesh> Meshes = new List<Mesh>();
        /// <summary>The containers it holds, in the order they were made (the same every build).</summary>
        public readonly List<LootContainer> Containers = new List<LootContainer>();
        /// <summary>The ceiling fittings' lamps, hung from the ceiling once the building is shown (see
        /// <see cref="RaidBuilding.HungLater"/>).</summary>
        private readonly List<HomeLamp> _hung = new List<HomeLamp>(16);
        /// <summary>Every light it made (ceiling fittings and lamps), once it's finished.</summary>
        public int Lights;
        /// <summary>The desk, bedside and standard lamps placed so far (they share the building's lamp budget).</summary>
        public int Lamps;
        /// <summary>Pieces of furniture with a body of their own placed so far (each is an object under the building's root;
        /// see <see cref="InteriorPlan.PieceCap"/>).</summary>
        public int Bodies => B != null && B.Root != null ? B.Root.childCount : 0;
        /// <summary>Its objects, once it's finished.</summary>
        public int Objects;
        public Bounds WorldBounds;
        public bool Done;
        public System.Random Rng;
        /// <summary>Time spent building it, and its slowest step (ms, and the step's name).</summary>
        public double TotalMs, MaxStepMs;
        public string MaxStep = "";
        public int StepCount;
        /// <summary>How long each room took to furnish (ms; null until the rooms are reached).</summary>
        public double[] RoomMs;

        private Vector3 _offset;
        private IEnumerator<string> _steps;

        /// <summary>A building's interior under <paramref name="parent"/> (the site's world root) and
        /// <paramref name="overheadParent"/> (its overhead root). <paramref name="hidden"/>: built inactive and shown at the
        /// end (a streamed piece already on show); else built as the site is.</summary>
        public InteriorBuild(InteriorPlan plan, Transform parent, Transform overheadParent, bool hidden)
        {
            Plan = plan;
            _parent = parent;
            _overheadParent = overheadParent;
            _hidden = hidden;
        }

        /// <summary>The build, a step at a time (each a few milliseconds).</summary>
        public IEnumerable<string> Steps()
        {
            Begin();
            yield return "interior shell";
            Walls();
            yield return "interior walls";
            Floors();
            yield return "interior floors";
            for (int k = 0; k < DoorCount; k += DoorsPerStep)
            {
                Doors(k, DoorsPerStep);
                yield return "interior doors";
            }
            Switches();
            yield return "interior switches";
            RoomMs = new double[Plan.Rooms.Count];
            for (int i = 0; i < Plan.Rooms.Count; i++)
            {
                long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                // One room that can't be furnished leaves it as far as it got, not the building unfurnished.
                try { InteriorPass.Furnish(this, i); }
                catch (System.Exception e) { Trouble("room " + i + " (" + Plan.Rooms[i].Role + ")", e); }
                RoomMs[i] = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                yield return "interior rooms";
            }
            EmitWalls();
            yield return "interior wall meshes";
            Finish();
            yield return "interior finish";
        }

        /// <summary>Runs the build's next step (timing it); false once it's finished. A step that throws throws here.</summary>
        public bool Advance()
        {
            if (_steps == null) _steps = Steps().GetEnumerator();
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            bool more = _steps.MoveNext();
            double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            TotalMs += ms;
            StepCount++;
            if (ms > MaxStepMs)
            {
                MaxStepMs = ms;
                MaxStep = more ? _steps.Current : "(end)";
            }
            return more;
        }

        /// <summary>Name of the step <see cref="Advance"/> last ran.</summary>
        public string Current => _steps != null ? _steps.Current : null;

        /// <summary>What went wrong in a room or a doorway that was left as far as it got (the rest of the building was
        /// furnished), one line each; null when nothing did.</summary>
        public List<string> Troubles;

        // A room or a doorway that threw: noted (and logged once a building), and the build goes on.
        private void Trouble(string what, System.Exception e)
        {
            Troubles ??= new List<string>(2);
            string line = what + ": " + e.GetType().Name + ": " + e.Message;
            if (e.StackTrace != null)
            {
                string[] frames = e.StackTrace.Split('\n');
                for (int k = 0; k < frames.Length && k < 4; k++) line += " < " + frames[k].Trim();
            }
            Troubles.Add(line);
            if (Troubles.Count == 1) Debug.LogWarning("[CloneZ] Interiors: building " + Plan.Id + " (" + Plan.Use + "), " + what + " left unfinished: " + e);
        }

        private const int DoorsPerStep = 4;

        // Doorways between rooms and to the outside, each with its leaves.
        private int DoorCount
        {
            get
            {
                int n = Plan.Entrances.Count;
                foreach (InteriorPass.Piece p in Pieces) if (p.Door != null) n++;
                return n;
            }
        }

        #region Coordinates
        /// <summary>A frame point (local x, height, local z) in the world.</summary>
        public Vector3 World(float lx, float y, float lz) => B.W(lx, y, lz);

        public Vector3 World(Vector2 local, float y) => B.W(local.x, y, local.y);

        /// <summary>A frame direction in the world.</summary>
        public Vector3 WorldDir(Vector2 local)
        {
            Vector2 d = Plan.DirToSite(local);
            return new Vector3(d.x, 0f, d.y);
        }

        /// <summary>A world point in the frame.</summary>
        public Vector2 LocalOf(Vector3 world) => Plan.ToLocal(new Vector2(world.x - _offset.x, world.z - _offset.z));
        #endregion

        #region Setting up
        private void Begin()
        {
            _offset = _parent != null ? _parent.position : Vector3.zero;
            Rng = new System.Random(unchecked(Plan.Seed * 7919 + 13));
            Root = new GameObject("Interior " + Plan.Name);
            if (_hidden) Root.SetActive(false);
            Root.transform.SetParent(_parent, false);
            // The building's own town: its random stream, tallies and overhead root (under the site's), so nothing here draws
            // from the site's stream or depends on the order buildings are furnished in.
            Town = new RaidTown(Root.transform, Plan.Seed, new RaidMapInfo { HalfSize = 4f }, _overheadParent);
            Town.Overhead.name = "Interior " + Plan.Name;
            if (Town.Scenery != null) Object.DestroyImmediate(Town.Scenery.gameObject);
            Vector2 centre = Plan.ToSite(Vector2.zero) + new Vector2(_offset.x, _offset.z);
            Rect ext = Plan.Extent;
            B = new RaidBuilding(Town, Plan.Name, centre, Plan.Yaw, Mathf.Max(1f, ext.width), Mathf.Max(1f, ext.height), Plan.Seed, Root.transform)
            {
                Kind = Plan.Kind,
                WallH = Plan.WallH,
                RegisterShelter = false,
                FlatCeilings = false,
                DoorWood = new Color(0.42f, 0.27f, 0.15f),
                Casing = new Color(0.36f, 0.24f, 0.14f),
            };
            B.ForeignWallMesh = FittingsAt;
            B.ForeignWallObject = WallObjectAt;
            // A town's worth of fittings: none asks for shadows, and they're hung from the ceiling once the building shows.
            B.CeilingShadows = false;
            B.HungLater = _hung;
            foreach (PlanRoom r in Plan.Rooms)
            {
                RaidRoom rr = B.Room(r.Name, r.R.xMin, r.R.yMin, r.R.xMax, r.R.yMax, r.Design ?? RaidLayouts.GarageGrey, r.Floor);
                rr.FloorTint = r.FloorTint;
                rr.Light = r.Light;
                rr.Lit = r.Lit;
                rr.Flicker = r.Flicker;
                Rooms.Add(rr);
            }
            var frame = new GameObject("Frame");
            Frame = frame.transform;
            Frame.SetParent(Root.transform, false);
            Frame.SetPositionAndRotation(World(0f, 0f, 0f), Quaternion.Euler(0f, Plan.Yaw, 0f));
        }

        // A mesh drawn in the frame, as an object of its own under `parent` (the frame or something under it).
        private GameObject EmitLocal(RaidMesh m, string name, Transform parent, Vector3 local, ShadowCastingMode shadows)
        {
            if (m == null || m.IsEmpty) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.identity;
            m.ApplyTo(go, local, shadows);
            return go;
        }
        #endregion

        #region Partitions
        // The partitions as runs along each wall line, cut at the doorways into pieces: each piece an object with its core,
        // the rooms' paper either side, trims, its collider and the cutaway's marker.
        private void Walls()
        {
            var runs = InteriorPass.Runs(Plan);
            foreach (InteriorPass.Run run in runs)
            {
                float start = run.Start, end = run.End;
                run.Doors.Sort((a, b) => a.DoorPos.CompareTo(b.DoorPos));
                float cursor = start;
                foreach (PlanWall door in run.Doors)
                {
                    float a = door.DoorPos - door.DoorWidth * 0.5f, b = door.DoorPos + door.DoorWidth * 0.5f;
                    if (a > cursor + 0.01f) Piece(run, cursor, a, null);
                    Piece(run, a, b, door);
                    cursor = b;
                }
                if (end > cursor + 0.01f) Piece(run, cursor, end, null);
            }
        }

        private void Piece(InteriorPass.Run run, float s, float e, PlanWall door)
        {
            var piece = new InteriorPass.Piece { AlongX = run.AlongX, Coord = run.Coord, S = s, E = e, Door = door, Run = run };
            RaidMesh m = piece.Mesh;
            float mid = (s + e) * 0.5f, len = e - s, h = Plan.WallH;
            float y0 = door != null ? RaidBuilding.DoorHeight : 0f;
            Surf core = DecorPalette.Matte(WallCore);
            if (h - y0 > 0.05f) m.Box(P(run, mid, (y0 + h) * 0.5f, 0f), Size(run, len, h - y0, Half * 2f), core);
            for (int side = -1; side <= 1; side += 2) Skins(run, piece, side, s, e, y0, h);
            if (door != null) DoorFrame(run, piece, door);
            // The piece's object now (doors, switches and fittings hang under it); its mesh once everything is drawn on it.
            Vector3 centre = P(run, mid, 0f, 0f);
            var go = new GameObject(door != null ? "DoorHead" : "Wall");
            go.transform.SetParent(Frame, false);
            go.transform.localPosition = centre;
            piece.Centre = centre;
            if (h - y0 > 0.05f)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.center = P(run, mid, (y0 + h) * 0.5f, 0f) - centre;
                bc.size = Size(run, len, h - y0, Half * 2f);
            }
            piece.Occluder = go.AddComponent<CutawayOccluder>();
            piece.Object = go;
            Pieces.Add(piece);
        }

        // The paper on one side of a piece, room by room, with its skirting, rail and cornice.
        private void Skins(InteriorPass.Run run, InteriorPass.Piece piece, int side, float s, float e, float y0, float y1)
        {
            RaidMesh m = piece.Mesh;
            Vector3 n = N(run, side);
            foreach (PlanWall w in run.Walls)
            {
                float a = Mathf.Max(s, w.SkinA), b = Mathf.Min(e, w.SkinB);
                if (b - a < 0.02f) continue;
                PlanRoom room = Plan.Rooms[side > 0 ? w.Plus : w.Minus];
                float c = (a + b) * 0.5f, l = b - a;
                if (room.Design == null)
                {
                    // A bare room (a shed, a warehouse): block walls.
                    m.Upright(P(run, c, (y0 + y1) * 0.5f, side * (Half + 0.003f)), n, l, y1 - y0,
                        DecorMaterials.WallExterior(ExteriorStyle.CinderBlock, new Color(0.62f, 0.62f, 0.60f)));
                    continue;
                }
                m.Upright(P(run, c, (y0 + y1) * 0.5f, side * (Half + 0.003f)), n, l, y1 - y0, DecorMaterials.WallInterior(room.Design));
                Surf trim = DecorPalette.Matte(room.Design.Trim);
                if (y0 < 0.1f) m.Box(P(run, c, 0.06f, side * (Half + 0.008f)), Size(run, l, 0.12f, 0.016f), trim);
                float rail = room.Design.RailHeight;
                if (rail > y0 + 0.05f && rail < y1 - 0.3f) m.Box(P(run, c, rail + 0.01f, side * (Half + 0.012f)), Size(run, l, 0.06f, 0.024f), trim);
                m.Box(P(run, c, y1 - 0.03f, side * (Half + 0.01f)), Size(run, l, 0.06f, 0.02f), trim);
            }
        }

        // The doorway's linings over the cut wall ends and the casings round it on both faces.
        private void DoorFrame(InteriorPass.Run run, InteriorPass.Piece piece, PlanWall door)
        {
            RaidMesh m = piece.Mesh;
            Surf wood = DecorPalette.Matte(B.Casing);
            float a = door.DoorPos - door.DoorWidth * 0.5f, b = door.DoorPos + door.DoorWidth * 0.5f, dh = RaidBuilding.DoorHeight;
            m.Box(P(run, a + 0.012f, dh * 0.5f, 0f), Size(run, 0.024f, dh, Half * 2f + 0.01f), wood);
            m.Box(P(run, b - 0.012f, dh * 0.5f, 0f), Size(run, 0.024f, dh, Half * 2f + 0.01f), wood);
            m.Box(P(run, door.DoorPos, dh - 0.012f, 0f), Size(run, door.DoorWidth, 0.024f, Half * 2f + 0.01f), wood);
            for (int side = -1; side <= 1; side += 2)
            {
                float off = side * (Half + 0.012f);
                m.Box(P(run, a - 0.035f, (dh + 0.07f) * 0.5f, off), Size(run, 0.07f, dh + 0.07f, 0.024f), wood);
                m.Box(P(run, b + 0.035f, (dh + 0.07f) * 0.5f, off), Size(run, 0.07f, dh + 0.07f, 0.024f), wood);
                m.Box(P(run, door.DoorPos, dh + 0.035f, off), Size(run, door.DoorWidth + 0.14f, 0.07f, 0.024f), wood);
            }
        }

        // A point on a run, in the frame: so far along it, so high, so far across it.
        private static Vector3 P(InteriorPass.Run r, float along, float y, float across)
            => r.AlongX ? new Vector3(along, y, r.Coord + across) : new Vector3(r.Coord + across, y, along);

        private static Vector3 Size(InteriorPass.Run r, float len, float h, float thick)
            => r.AlongX ? new Vector3(len, h, thick) : new Vector3(thick, h, len);

        private static Vector3 N(InteriorPass.Run r, int side) => r.AlongX ? new Vector3(0f, 0f, side) : new Vector3(side, 0f, 0f);

        // The partition piece at a world point on a wall face, for the fittings RaidBuilding hangs there.
        private InteriorPass.Piece PieceAt(Vector3 world)
        {
            Vector2 l = LocalOf(world);
            foreach (InteriorPass.Piece p in Pieces)
            {
                float across = p.AlongX ? l.y : l.x, along = p.AlongX ? l.x : l.y;
                if (Mathf.Abs(across - p.Coord) < 0.2f && along >= p.S - 0.01f && along <= p.E + 0.01f) return p;
            }
            return null;
        }

        private RaidMesh FittingsAt(Vector3 world)
        {
            InteriorPass.Piece p = PieceAt(world);
            if (p == null) return null;
            return p.Fittings ??= new RaidMesh();
        }

        private Transform WallObjectAt(Vector3 world)
        {
            InteriorPass.Piece p = PieceAt(world);
            return p != null && p.Object != null ? p.Object.transform : null;
        }
        #endregion

        #region Floors
        // Each room's floor over its rectangle to the walls' faces (to the shared line where it runs open into its
        // neighbour), and a threshold through every doorway.
        private void Floors()
        {
            var m = new RaidMesh();
            // Under the rooms, the whole outline in the biggest room's floor: what an irregular footprint's rectangles leave
            // along its walls (and the exteriors' plain floor would show through).
            if (Plan.Local != null && Plan.Local.Count >= 3)
            {
                PlanRoom main = Plan.Rooms[0];
                foreach (PlanRoom r in Plan.Rooms) if (r.R.width * r.R.height > main.R.width * main.R.height) main = r;
                Surf under = InteriorPass.FloorSurf(main);
                List<int> tris = MapGeometry.Triangulate(Plan.Local);
                for (int t = 0; t + 2 < tris.Count; t += 3)
                {
                    Vector2 a = Plan.Local[tris[t]], b = Plan.Local[tris[t + 1]], c = Plan.Local[tris[t + 2]];
                    m.Tri(new Vector3(a.x, 0.026f, a.y), new Vector3(b.x, 0.026f, b.y), new Vector3(c.x, 0.026f, c.y), under);
                }
            }
            for (int i = 0; i < Plan.Rooms.Count; i++)
            {
                PlanRoom r = Plan.Rooms[i];
                Rect f = InteriorPass.FloorRect(Plan, i);
                m.Flat(new Vector3(f.center.x, 0.03f, f.center.y), f.width, f.height, 0f, InteriorPass.FloorSurf(r));
            }
            Surf sill = DecorPalette.Matte(new Color(0.40f, 0.30f, 0.20f));
            foreach (PlanWall w in Plan.Walls)
            {
                if (!w.HasDoor) continue;
                Vector2 c = w.Point(w.DoorPos);
                if (w.AlongX) m.Flat(new Vector3(c.x, 0.033f, c.y), w.DoorWidth, Half * 2f + 0.02f, 0f, sill);
                else m.Flat(new Vector3(c.x, 0.033f, c.y), Half * 2f + 0.02f, w.DoorWidth, 0f, sill);
            }
            EmitLocal(m, "Floors", Frame, Vector3.zero, ShadowCastingMode.Off);
        }
        #endregion

        #region Doors
        // A working door in every doorway between rooms, and in every outside doorway.
        private void Doors(int from, int count)
        {
            int index = 0, to = from + count;
            foreach (InteriorPass.Piece p in Pieces)
            {
                if (p.Door == null) continue;
                if (index++ < from) continue;
                if (index > to) return;
                PlanWall w = p.Door;
                PlanRoom minus = Plan.Rooms[w.Minus], plus = Plan.Rooms[w.Plus];
                int swing = SwingInto(minus, plus);
                Vector2 c = w.Point(w.DoorPos);
                Vector2 alongL = w.AlongX ? Vector2.right : Vector2.up, normalL = w.AlongX ? Vector2.up : Vector2.right;
                PlanRoom into = swing > 0 ? plus : minus;
                RoomExtent(into, w.AlongX, out float ra, out float rb);
                try
                {
                    Hang(World(c, 0f), WorldDir(alongL), WorldDir(normalL), w.DoorWidth, Half, swing, w.DoorPos, ra, rb,
                        Name(minus, plus, into), null, plus, minus, false, false, p.Object.transform, p.Occluder, RaidBuilding.DoorHeight);
                }
                catch (System.Exception e) { Trouble("the door between rooms " + w.Minus + " and " + w.Plus, e); }
            }
            for (int k = 0; k < Plan.Entrances.Count; k++)
            {
                if (index++ < from) continue;
                if (index > to) return;
                PlanEntrance en = Plan.Entrances[k];
                if (en.Width < 0.6f) continue;
                Vector2 along = new Vector2(-en.Outward.y, en.Outward.x);
                // The doorway's middle on the outside wall's centre line.
                Vector2 c = en.At + en.Outward * (Plan.Thick * 0.5f);
                PlanRoom room = en.Room >= 0 ? Plan.Rooms[en.Room] : null;
                float ra = -0.5f, rb = 0.5f;
                if (room != null)
                {
                    bool alongX = Mathf.Abs(along.x) >= Mathf.Abs(along.y);
                    RoomExtent(room, alongX, out ra, out rb);
                    float at = alongX ? c.x : c.y;
                    ra -= at;
                    rb -= at;
                }
                bool shop = Plan.Kind == RaidBuildingKind.Store || Plan.Kind == RaidBuildingKind.Clinic || Plan.Use == InteriorUse.Bank
                    || Plan.Use == InteriorUse.PostOffice || Plan.Use == InteriorUse.Office;
                bool glass = shop && Plan.Town && k == 0;
                string name = k == 0 ? "Front door" : Vector2.Dot(en.Outward, Vector2.up) > 0.7f ? "Back door" : "Side door";
                // Across the frame's own axes a slanting wall's door still works on the world's.
                try
                {
                    Hang(World(c, 0f), WorldDir(along).normalized, WorldDir(en.Outward).normalized, en.Width, Plan.Thick * 0.5f, -1, 0f, ra, rb,
                        name, "inside the " + Plan.Noun, null, null, true, glass, Root.transform, null, OuterDoorHeight);
                }
                catch (System.Exception e) { Trouble("the " + name.ToLowerInvariant(), e); }
            }
        }

        // Which way a door between two rooms swings: into the room that isn't a way through, else into the smaller.
        private static int SwingInto(PlanRoom minus, PlanRoom plus)
        {
            bool mPass = minus.Role == RoomRole.Hall || minus.Role == RoomRole.Lobby || minus.Role == RoomRole.Narthex || minus.Role == RoomRole.Waiting;
            bool pPass = plus.Role == RoomRole.Hall || plus.Role == RoomRole.Lobby || plus.Role == RoomRole.Narthex || plus.Role == RoomRole.Waiting;
            if (mPass != pPass) return mPass ? 1 : -1;
            float am = minus.R.width * minus.R.height, ap = plus.R.width * plus.R.height;
            return ap <= am ? 1 : -1;
        }

        private static void RoomExtent(PlanRoom r, bool alongX, out float a, out float b)
        {
            a = (alongX ? r.R.xMin : r.R.yMin) + Half;
            b = (alongX ? r.R.xMax : r.R.yMax) - Half;
        }

        private static string Name(PlanRoom minus, PlanRoom plus, PlanRoom into)
        {
            PlanRoom wash = Wash(plus) ? plus : Wash(minus) ? minus : null;
            PlanRoom named = wash ?? (Passage(into) ? (into == plus ? minus : plus) : into);
            return InteriorPass.Title(named.Name) + " door";
        }

        private static bool Wash(PlanRoom r) => r.Role == RoomRole.Bathroom || r.Role == RoomRole.Restroom;

        private static bool Passage(PlanRoom r) => r.Role == RoomRole.Hall || r.Role == RoomRole.Lobby;

        // A pair of leaves hung in a doorway (one leaf in a narrow one) and the RaidDoor that works them. `along` and `normal`
        // are world directions (the normal toward the +1 side), `swing` the side they open toward; `ra`..`rb` the room's
        // wall either side of the doorway's middle (`mid` along the same line), which bounds how far an open leaf leans.
        private void Hang(Vector3 centre, Vector3 along, Vector3 normal, float width, float halfThick, int swing, float mid, float ra, float rb,
            string name, string lockFrom, PlanRoom plus, PlanRoom minus, bool outer, bool glass, Transform parent, CutawayOccluder occluder,
            float doorHeight)
        {
            var root = new GameObject("Door");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(centre, Quaternion.identity);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            bool pair = width >= 1.1f;
            int leavesN = pair ? 2 : 1;
            float leafW = pair ? (width - 2f * LeafClear - LeafMeet) * 0.5f : width - 2f * LeafClear;
            float leafH = doorHeight - LeafFoot - 0.03f;
            Vector3 into = normal * swing;
            float across = swing * (halfThick - LeafInset);
            float leanPick = 5f + (float)Rng.NextDouble() * 25f;
            Color wood = B.DoorWood;
            if (outer && Plan.Kind == RaidBuildingKind.House && !glass && Rng.NextDouble() < 0.6) wood = FrontDoorPaints[Rng.Next(FrontDoorPaints.Length)];
            var leaves = new FixturePart[leavesN];
            var hinges = new Vector3[leavesN];
            var reaches = new Vector3[leavesN];
            var angles = new float[leavesN];
            for (int i = 0; i < leavesN; i++)
            {
                bool atA = i == 0;
                float hingeAlong = atA ? -width * 0.5f + LeafClear : width * 0.5f - LeafClear;
                Vector3 hinge = centre + along * hingeAlong + normal * across + Vector3.up * LeafFoot;
                Vector3 reach = atA ? along : -along;
                float free = (atA ? hingeAlong - (ra - mid) : (rb - mid) - hingeAlong) - 0.12f;
                float maxLean = Mathf.Asin(Mathf.Clamp01(free / Mathf.Max(0.1f, leafW))) * Mathf.Rad2Deg;
                float lean = Mathf.Min(maxLean, leanPick);
                var m = new RaidMesh();
                m.SetFrame(hinge, Mathf.Atan2(reach.x, reach.z) * Mathf.Rad2Deg - 90f);
                FurnitureBuilder.DoorLeaf(m, Rng, leafW, leafH, wood, glass);
                m.ResetFrame();
                float angle = Mathf.Sign(Vector3.SignedAngle(reach, into, Vector3.up)) * (90f + lean);
                leaves[i] = FixturePart.Hinge(root.transform, "Leaf", m, hinge, Vector3.up, angle).WithSpeed(CloneZConfig.DoorSwingSeconds);
                hinges[i] = hinge;
                reaches[i] = reach;
                angles[i] = angle;
            }

            bool shut, locked = false;
            int lockSide = 0;
            float health;
            bool deadbolt = false, peephole = false;
            if (outer)
            {
                lockSide = -1;
                deadbolt = true;
                peephole = name == "Front door" && Plan.Kind == RaidBuildingKind.House && !glass;
                shut = Rng.NextDouble() < CloneZConfig.RaidExteriorDoorShutChance;
                locked = shut && Rng.NextDouble() < CloneZConfig.RaidExteriorDoorLockChance;
                health = glass ? CloneZConfig.RaidDoorHealthGlass : CloneZConfig.RaidDoorHealthExterior;
            }
            else
            {
                PlanRoom wash = plus != null && Wash(plus) ? plus : minus != null && Wash(minus) ? minus : null;
                if (wash != null)
                {
                    lockSide = wash == plus ? 1 : -1;
                    lockFrom = "inside the " + wash.Name.ToLowerInvariant();
                }
                shut = Rng.NextDouble() >= CloneZConfig.RaidInteriorDoorOpenChance;
                health = CloneZConfig.RaidDoorHealthInterior;
            }
            Vector3 thick = new Vector3(width, doorHeight, halfThick * 2f);
            var setup = new HomeDoor.Setup
            {
                Name = name,
                Noun = char.ToLowerInvariant(name[0]) + name.Substring(1),
                Leaves = leaves,
                Point = centre + Vector3.up * KnobHeight,
                Normal = normal,
                Leaf = new Bounds(centre + Vector3.up * (doorHeight * 0.5f), thick),
                Gap = new Bounds(centre + Vector3.up * (Plan.WallH * 0.5f), new Vector3(width, Plan.WallH, halfThick * 2f)),
                Block = Aabb(centre + Vector3.up * ((0.25f + doorHeight) * 0.5f), along, normal, width - 0.1f, doorHeight - 0.25f, halfThick * 2f + 0.1f),
                LockSide = lockSide,
                LockFrom = lockFrom,
                Deadbolt = deadbolt,
                Peephole = peephole,
                Open = !shut,
                Locked = locked,
                SeeThrough = glass,
                Occluder = occluder,
            };
            bool alongX = Mathf.Abs(along.x) >= Mathf.Abs(along.z);
            Rect fb = Plan.Footprint.Bounds;
            var raid = new RaidDoor.Raid
            {
                Health = health,
                Line = new OpeningLine(alongX, alongX ? centre.z : centre.x),
                Mid = alongX ? centre.x : centre.z,
                Width = width,
                Swing = swing,
                Building = new Rect(fb.x + _offset.x, fb.y + _offset.z, fb.width, fb.height),
                Hinges = hinges,
                Reaches = reaches,
                Angles = angles,
                Wood = wood,
                Seed = Rng.Next(),
            };
            RaidDoor.Attach(root, setup, raid);
            // The door's collider and its NavMesh carve sit in the world's axes; turn them to the wall.
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            Transform solid = root.transform.Find("ShutLeaves"), carve = root.transform.Find("Carver");
            if (solid != null) solid.rotation = q;
            if (carve != null) carve.rotation = q;
        }

        // The world box round a turned box (the door's "nobody stands here" check runs in the world's axes).
        private static Bounds Aabb(Vector3 centre, Vector3 along, Vector3 normal, float len, float h, float thick)
        {
            float x = Mathf.Abs(along.x) * len + Mathf.Abs(normal.x) * thick, z = Mathf.Abs(along.z) * len + Mathf.Abs(normal.z) * thick;
            return new Bounds(centre, new Vector3(x, h, z));
        }
        #endregion

        #region Switches
        // A light switch inside each lit-able room's first doorway, clear of the door's open leaves, worked on the room's
        // circuit (a room the lamp budget left dark has none).
        private void Switches()
        {
            for (int i = 0; i < Plan.Rooms.Count; i++)
            {
                if (Plan.Rooms[i].Lamps <= 0) continue;
                bool wired = false;
                foreach (InteriorPass.Piece p in Pieces)
                {
                    if (p.Door == null || (p.Door.Minus != i && p.Door.Plus != i)) continue;
                    if (TrySwitch(i, p)) { wired = true; break; }
                }
                if (!wired) EntranceSwitch(i);
            }
        }

        /// <summary>Where the light switches are (frame points on the walls' faces), by room: the room's furnishing keeps
        /// its tall pieces off them.</summary>
        public readonly List<KeyValuePair<int, Vector2>> SwitchSpots = new List<KeyValuePair<int, Vector2>>();

        private const float Plate = 0.07f;

        private bool TrySwitch(int room, InteriorPass.Piece door)
        {
            PlanWall w = door.Door;
            int side = w.Plus == room ? 1 : -1;
            PlanRoom r = Plan.Rooms[room];
            RoomExtent(r, w.AlongX, out float ra, out float rb);
            int swing = SwingInto(Plan.Rooms[w.Minus], Plan.Rooms[w.Plus]);
            float leaf = swing == side ? (w.DoorWidth * 0.5f) + 0.05f : 0f;
            for (int e = 1; e >= -1; e -= 2)
            {
                float at = w.DoorPos + e * (w.DoorWidth * 0.5f + 0.07f + 0.15f + Plate * 0.5f + leaf);
                if (at - Plate < ra + 0.1f || at + Plate > rb - 0.1f) continue;
                InteriorPass.Piece wall = null;
                foreach (InteriorPass.Piece p in Pieces)
                    if (p.Door == null && p.Run == door.Run && at - Plate >= p.S && at + Plate <= p.E) { wall = p; break; }
                if (wall == null) continue;
                Vector3 faceL = P(wall.Run, at, Mathf.Max(SwitchHeight, RailClear(r)), side * (Half + 0.004f)), nL = N(wall.Run, side);
                Wire(wall.Object.transform, wall.Mesh, faceL, nL, room);
                return true;
            }
            return false;
        }

        // A room no partition's doorway leads into (a one-room building, or one reached through an open side) has its switch
        // by its outside door instead, on the latch side if there's wall there: on the outside wall's inner face, its plate
        // an object of its own.
        private bool EntranceSwitch(int room)
        {
            PlanRoom r = Plan.Rooms[room];
            foreach (PlanEntrance en in Plan.Entrances)
            {
                if (en.Room != room || en.Width < 0.6f || en.Outward.sqrMagnitude < 1e-6f) continue;
                Vector2 outward = en.Outward.normalized;
                Vector2 along = new Vector2(-outward.y, outward.x);
                // The outside door's single leaf hangs at its -along end and opens inward against the wall there.
                for (int e = 1; e >= -1; e -= 2)
                {
                    Vector2 at = en.At + along * (e * (en.Width * 0.5f + 0.07f + 0.15f + Plate * 0.5f + (e < 0 && en.Width < 1.1f ? en.Width * 0.5f + 0.05f : 0f)));
                    if (!SolidWall(room, at, outward, along, en)) continue;
                    var n3 = new Vector3(-outward.x, 0f, -outward.y);
                    Vector3 faceL = new Vector3(at.x, Mathf.Max(SwitchHeight, RailClear(r)), at.y) + n3 * 0.004f;
                    var host = new GameObject("Light switch plate");
                    host.transform.SetParent(Frame, false);
                    host.transform.localPosition = faceL;
                    var m = new RaidMesh();
                    Wire(host.transform, m, faceL, n3, room);
                    m.ApplyTo(host, faceL, ShadowCastingMode.Off);
                    return true;
                }
            }
            return false;
        }

        // True when a switch at `at` on the outside wall's inner face is on plain wall of `room`: no window or other doorway
        // there, and no partition meeting the wall under it.
        private bool SolidWall(int room, Vector2 at, Vector2 outward, Vector2 along, PlanEntrance self)
        {
            for (int s = -1; s <= 1; s++)
                if (Plan.RoomAt(at - outward * 0.3f + along * (s * (Plate * 0.5f + Half + 0.05f))) != room) return false;
            foreach (PlanWindow w in Plan.Windows)
            {
                Vector2 ab = w.B - w.A;
                float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(at - w.A, ab) / ab.sqrMagnitude) : 0f;
                if ((w.A + ab * t - at).sqrMagnitude < (Plate + 0.12f) * (Plate + 0.12f)) return false;
            }
            foreach (PlanEntrance en in Plan.Entrances)
            {
                if (en.At == self.At) continue;
                float reach = en.Width * 0.5f + Plate + 0.15f;
                if ((en.At - at).sqrMagnitude < reach * reach) return false;
            }
            return true;
        }

        // The plate, its slot and the working switch with its toggle, on a wall face at `faceL` (frame) facing `nL`: the
        // plate drawn into `mesh` (the wall's), the switch under `parent`.
        private void Wire(Transform parent, RaidMesh mesh, Vector3 faceL, Vector3 nL, int room)
        {
            FurnitureBuilder.SwitchPlate(mesh, faceL, nL, Plate);
            FurnitureBuilder.SwitchSlot(mesh, faceL, nL);
            Vector3 face = Frame.TransformPoint(faceL), n = Frame.TransformDirection(nL);
            LightSwitch sw = LightSwitch.Create(parent, "Light switch", face + n * 0.03f, face, n, new[] { B.CircuitOf(Rooms[room]) });
            Vector3 axis = Vector3.Cross(n, Vector3.up);
            var toggleMesh = new RaidMesh();
            FurnitureBuilder.SwitchToggle(toggleMesh, face + n * 0.009f, n, ToggleTilt);
            FixturePart toggle = FixturePart.Hinge(sw.transform, "Toggle", toggleMesh, face + n * 0.009f, axis, ToggleTilt * 2f, ShadowCastingMode.Off).WithSpeed(0.08f);
            sw.SetToggles(new[] { toggle });
            SwitchSpots.Add(new KeyValuePair<int, Vector2>(room, new Vector2(faceL.x, faceL.z)));
        }

        // A switch keeps clear of the chair rail behind it.
        private static float RailClear(PlanRoom r)
        {
            if (r.Design == null) return SwitchHeight;
            float rail = r.Design.RailHeight;
            return rail + 0.04f > SwitchHeight - 0.07f && rail - 0.02f < SwitchHeight + 0.07f ? rail + 0.115f : SwitchHeight;
        }
        #endregion

        #region Finishing
        // The partitions' meshes, switch plates and all; then their fittings (cupboards, mirror cabinets drawn on the wall).
        private void EmitWalls()
        {
            foreach (InteriorPass.Piece p in Pieces)
            {
                if (p.Object == null) continue;
                if (!p.Mesh.IsEmpty) p.Mesh.ApplyTo(p.Object, p.Centre, ShadowCastingMode.On);
                if (p.Fittings == null || p.Fittings.IsEmpty) continue;
                GameObject f = p.Fittings.Emit("Fittings", p.Object.transform, p.Object.transform.position, ShadowCastingMode.On);
                if (f == null) continue;
                f.transform.rotation = Quaternion.identity;   // drawn in the world's axes
                f.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            }
        }

        private void Finish()
        {
            B.Finish();
            Root.GetComponentsInChildren(true, s_Lights);
            Lights = s_Lights.Count;
            s_Lights.Clear();
            KeepLoot();
            Root.GetComponentsInChildren(true, Containers);
            Bounds b = new Bounds(World(Plan.Extent.center, Plan.WallH * 0.5f), Vector3.zero);
            foreach (Vector2 p in Plan.Footprint.Pts) b.Encapsulate(new Vector3(p.x + _offset.x, 0f, p.y + _offset.z));
            b.Encapsulate(b.center + Vector3.up * Plan.WallH * 0.5f);
            WorldBounds = b;
            if (_hidden) Root.SetActive(true);
            HangLamps();
            Objects = CountObjects(Root.transform) + (Town.Overhead != null ? CountObjects(Town.Overhead) : 0);
            Done = true;
        }

        // The ceiling fittings' lamps, hung from the ceiling now the building's awake: their glowing shades show only in the
        // views that show the ceilings, their bare bulbs in the isometric view. In edit mode (the harness) nothing wakes, so
        // they're left as the isometric view has them.
        private void HangLamps()
        {
            foreach (HomeLamp lamp in _hung)
            {
                if (lamp == null) continue;
                if (Application.isPlaying)
                {
                    if (lamp.isActiveAndEnabled) lamp.HangFromCeiling();
                    continue;
                }
                RoomLight light = lamp.GetComponent<RoomLight>();
                if (light != null) light.ShowBulb = true;
                Transform glow = lamp.transform.Find("Glow");
                Renderer r = glow != null ? glow.GetComponent<Renderer>() : null;
                if (r != null) r.enabled = false;
            }
            _hung.Clear();
        }

        private static readonly List<RoomLight> s_Lights = new List<RoomLight>(32);
        private static readonly List<LootContainer> s_Boxes = new List<LootContainer>(64);
        private static readonly List<ContainerType> s_Kinds = new List<ContainerType>(8);

        /// <summary>Loot containers stripped of their storage by <see cref="KeepLoot"/> (for the harness).</summary>
        public int Stripped;

        // Each room keeps as many loot containers as its plan allots it (every drawer, cupboard and compartment its furniture
        // came with counts one): one of each kind its pool names first, the signature kind leading, then the rest in the
        // order they were made; the others' storage is taken out and their furniture stays, shut.
        private void KeepLoot()
        {
            Root.GetComponentsInChildren(true, s_Boxes);
            int n = Plan.Rooms.Count;
            var byRoom = new List<LootContainer>[n];
            foreach (LootContainer box in s_Boxes)
            {
                if (box == null || box.IsSack || box.Type == ContainerType.Corpse) continue;
                Vector2 l = LocalOf(box.transform.position);
                int room = Plan.RoomAt(l);
                if (room < 0) room = Plan.NearestRoom(l);
                if (room < 0)
                {
                    Strip(box);
                    continue;
                }
                (byRoom[room] ??= new List<LootContainer>(8)).Add(box);
            }
            s_Boxes.Clear();
            for (int i = 0; i < n; i++)
            {
                List<LootContainer> list = byRoom[i];
                PlanRoom r = Plan.Rooms[i];
                int keep = Mathf.Max(0, r.Loot);
                if (list == null || list.Count <= keep) continue;
                var chosen = new bool[list.Count];
                int kept = 0;
                s_Kinds.Clear();
                if (r.Pool != null)
                    foreach (ContainerType t in r.Pool)
                        if (!s_Kinds.Contains(t)) s_Kinds.Add(t);
                for (bool more = true; kept < keep && more;)
                {
                    more = false;
                    foreach (ContainerType t in s_Kinds)
                    {
                        if (kept >= keep) break;
                        for (int k = 0; k < list.Count; k++)
                        {
                            if (chosen[k] || list[k].Type != t) continue;
                            chosen[k] = true;
                            kept++;
                            more = true;
                            break;
                        }
                    }
                }
                for (int k = 0; k < list.Count && kept < keep; k++)
                    if (!chosen[k]) { chosen[k] = true; kept++; }
                for (int k = 0; k < list.Count; k++)
                    if (!chosen[k]) Strip(list[k]);
            }
        }

        // Takes a container's storage out of its furniture: a fixture's holder goes whole (its doors and drawers stay shut on
        // the piece), a free-standing container keeps its body.
        private void Strip(LootContainer box)
        {
            if (box == null) return;
            Stripped++;
            if (box.Owner is StorageFixture fixture && fixture != null)
            {
                Object.DestroyImmediate(fixture.gameObject);
                return;
            }
            if (box.IsFixture && box.transform.childCount == 0 && box.GetComponent<Renderer>() == null)
            {
                Object.DestroyImmediate(box.gameObject);
                return;
            }
            Object.DestroyImmediate(box);
        }

        private static int CountObjects(Transform t)
        {
            int n = 1;
            for (int i = 0; i < t.childCount; i++) n += CountObjects(t.GetChild(i));
            return n;
        }

        /// <summary>Takes the interior down: its objects, its overhead root and its meshes.</summary>
        public void Discard()
        {
            if (Root != null) InteriorPass.Kill(Root);
            if (Town != null && Town.Overhead != null) InteriorPass.Kill(Town.Overhead.gameObject);
            for (int i = 0; i < Meshes.Count; i++) InteriorPass.Kill(Meshes[i]);
            Meshes.Clear();
            Containers.Clear();
            _hung.Clear();
        }
        #endregion
    }
}
