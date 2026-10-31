using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Floor finishes for raid-map rooms.</summary>
    internal enum FloorKind { LinoGrey, LinoBeige, LinoGreen, Checker, Wood, WoodDark, Carpet, Concrete, Plywood }

    /// <summary>The kind of ceiling fitting a room's light hangs from.</summary>
    internal enum LightStyle { Panel, Dome, Bulb, ShopLamp }

    /// <summary>One room of a raid-map building: its rectangle on the wall centre lines, finishes,
    /// lighting and the footprints already taken by furniture.</summary>
    internal sealed class RaidRoom
    {
        public string Name;
        public Rect Local;
        public Rect World;
        public WallDesign Design;
        public FloorKind Floor;
        public Color FloorTint = new Color(0.25f, 0.30f, 0.38f);
        public bool Lit;
        public bool Flicker;
        public LightStyle Light = LightStyle.Panel;

        /// <summary>Furniture footprints (world XZ rects).</summary>
        public readonly List<Rect> Furniture = new List<Rect>();
        /// <summary>Space kept clear in front of doorways and around open door leaves.</summary>
        public readonly List<Rect> Reserved = new List<Rect>();

        /// <summary>The floor area inside the walls (world).</summary>
        public Rect Inner => Rect.MinMaxRect(World.xMin + 0.1f, World.yMin + 0.1f, World.xMax - 0.1f, World.yMax - 0.1f);

        /// <summary>Returns true if the given rect overlaps furniture or, optionally, reserved zones.</summary>
        public bool Overlaps(Rect r, bool includeReserved)
        {
            for (int i = 0; i < Furniture.Count; i++) if (Furniture[i].Overlaps(r)) return true;
            if (includeReserved) for (int i = 0; i < Reserved.Count; i++) if (Reserved[i].Overlaps(r)) return true;
            return false;
        }
    }

    internal enum OpeningKind { Door, Window }

    /// <summary>What a raid building is, for the policies its fixtures follow (which doors lock, what hangs at the
    /// windows, how its storage is stocked).</summary>
    internal enum RaidBuildingKind { House, Office, Store, Clinic, Army, Depot, Tent }

    /// <summary>
    /// A furnished building of the raid map, authored in its own local frame (front wall on local -Z,
    /// facing the street) and placed in the world with a quarter-turn rotation and optional mirror.
    /// Walls are derived from the room rectangles; every straight stretch between openings becomes its
    /// own wall segment GameObject (a <see cref="CutawayOccluder"/> with a collider and one merged mesh
    /// carrying the core, both faces' wainscot / wallpaper or siding, trims, posters, vines and grime).
    /// Floors, furniture and debris merge into a few building-wide meshes; ceilings go under a separate
    /// root so they never reach the navmesh bake.
    /// </summary>
    internal sealed partial class RaidBuilding
    {
        public const float DoorHeight = 2.2f;
        /// <summary>The clear height of the doorways in a shell this class raises (the procedural town's): tall enough that the
        /// NavMesh bake (a 2 m agent on 1/6 m voxels) always fits through, whichever way the voxels round.</summary>
        public const float ShellDoorHeight = 2.4f;
        public const float SillHeight = 0.9f;
        public const float HeadHeight = 2.05f;
        public const float Half = 0.1f;   // half the wall thickness

        public readonly RaidTown Town;
        public readonly string Name;
        public readonly Vector2 Center;
        public readonly int Turns;
        public readonly bool Mirror;
        public readonly float HalfW, HalfD;
        public readonly System.Random Rng;

        public float WallH = 2.8f;
        /// <summary>What the building is (set by its layout).</summary>
        public RaidBuildingKind Kind = RaidBuildingKind.House;
        public ExteriorStyle Exterior = ExteriorStyle.Siding;
        public Color ExteriorColor = new Color(0.78f, 0.76f, 0.68f);
        public Color DoorWood = new Color(0.42f, 0.27f, 0.15f);
        public Color Casing = new Color(0.36f, 0.24f, 0.14f);
        public Color WallCore = new Color(0.20f, 0.19f, 0.18f);
        /// <summary>False for buildings that draw their own roof (the tent).</summary>
        public bool FlatCeilings = true;
        /// <summary>False to leave doorways unframed (tent flaps).</summary>
        public bool DoorFrames = true;

        public readonly List<RaidRoom> Rooms = new List<RaidRoom>();
        private readonly List<WallRun> _runs = new List<WallRun>();
        private readonly List<Opening> _openings = new List<Opening>();
        private readonly List<WallPiece> _pieces = new List<WallPiece>();

        /// <summary>Furniture, door leaves, lamps (casts shadows).</summary>
        public readonly RaidMesh Static = new RaidMesh();
        /// <summary>Floors, rugs and litter (no shadows).</summary>
        public readonly RaidMesh Flat = new RaidMesh();
        /// <summary>Ceilings and ceiling fittings (separate root, toggled per view).</summary>
        public readonly RaidMesh Ceil = new RaidMesh();

        public Transform Root;
        private GameObject _colliders;
        private bool _shellBuilt;

        private sealed class WallRun
        {
            public bool AlongX;
            public float Coord, A, B;
            public readonly List<Opening> Openings = new List<Opening>();
        }

        private sealed class Opening
        {
            public WallRun Run;
            public OpeningKind Kind;
            public float Pos, Width;
            public bool Leaf, Glass, Boarded, Intact;
            /// <summary>The clearance zones kept in front of this doorway.</summary>
            public readonly List<Rect> Zones = new List<Rect>(2);
            public float A => Pos - Width * 0.5f;
            public float B => Pos + Width * 0.5f;
        }

        private sealed class WallPiece
        {
            public WallRun Run;
            public float S, E;
            public Opening Opening;
            public readonly RaidMesh Mesh = new RaidMesh();
            public readonly List<Bounds> Colliders = new List<Bounds>(2);
            /// <summary>The emitted segment (set in <see cref="Finish"/>); fixtures hung on the wall go under it.</summary>
            public GameObject Object;
        }

        /// <summary>Creates a building using quarter‑turn rotations and optional mirroring.</summary>
        public RaidBuilding(RaidTown town, string name, Vector2 center, int turns, bool mirror, float width, float depth)
        {
            Town = town;
            Name = name;
            Center = center;
            // Normalize turn count to 0‑3 representing quarter‑turns.
            Turns = ((turns % 4) + 4) % 4;
            Mirror = mirror;
            HalfW = width * 0.5f;
            HalfD = depth * 0.5f;
            // Seed random stream from town's RNG so building‑specific randomness is repeatable.
            Rng = new System.Random(town.Rng.Next());
            Root = new GameObject("Bldg_" + name).transform;
            Root.SetParent(town.World, false);
        }

        // A building turned by any angle (the free frame), not by quarter turns.
        private readonly bool _free;
        private readonly float _freeYaw, _freeCos, _freeSin;

        /// <summary>A building whose local frame is turned by any angle, <paramref name="yaw"/> degrees (as <see cref="Yaw"/>
        /// counts them), instead of quarter turns: a real-world building on the Kentucky map, furnished on its own axes by
        /// <c>InteriorPass</c>, which raises its walls itself. Its random stream is seeded with <paramref name="seed"/>
        /// (nothing is drawn from the town's) and its root goes under <paramref name="parent"/>. Only the furnishing half of
        /// the building is meant for it (rooms, pieces, lights, <see cref="Finish"/>): its rooms' <see cref="RaidRoom.World"/>
        /// rectangles are the boxes round the turned rooms, which the wall builder (<see cref="BuildShell"/>) can't use.</summary>
        public RaidBuilding(RaidTown town, string name, Vector2 center, float yaw, float width, float depth, int seed, Transform parent)
        {
            Town = town;
            Name = name;
            Center = center;
            HalfW = width * 0.5f;
            HalfD = depth * 0.5f;
            Rng = new System.Random(seed);
            _free = true;
            _freeYaw = yaw;
            float r = yaw * Mathf.Deg2Rad;
            _freeCos = Mathf.Cos(r);
            _freeSin = Mathf.Sin(r);
            Root = new GameObject("Bldg_" + name).transform;
            Root.SetParent(parent != null ? parent : town.World, false);
        }

        /// <summary>False for a building whose shelter is registered by whoever raised its walls (the real-world map's).</summary>
        public bool RegisterShelter = true;

        /// <summary>For a building whose walls are raised by someone else (the free frame): the mesh and the object of the
        /// wall at a world point on a wall face, for the fittings hung there (cupboards, posters, clocks), so they hide with
        /// it when the camera cuts it away. Null, or returning null, leaves them on the furniture.</summary>
        public System.Func<Vector3, RaidMesh> ForeignWallMesh;
        /// <inheritdoc cref="ForeignWallMesh"/>
        public System.Func<Vector3, Transform> ForeignWallObject;

        /// <summary>False for ceiling fittings that never ask for shadows (the real-world map's: a town's worth of them).</summary>
        public bool CeilingShadows = true;

        /// <summary>When set, <see cref="CeilingLight"/> lists its lamps here instead of hanging them from the ceiling at once:
        /// <see cref="HomeLamp.HangFromCeiling"/> needs the lamp awake, which a building raised inactive (or in edit mode)
        /// isn't, so its builder hangs them once it's shown.</summary>
        public List<HomeLamp> HungLater;

        #region Frame
        /// <summary>A local (x, z) point in world XZ.</summary>
        public Vector2 P(float lx, float lz)
        {
            if (_free) return new Vector2(Center.x + lx * _freeCos + lz * _freeSin, Center.y - lx * _freeSin + lz * _freeCos);
            if (Mirror) lx = -lx;
            switch (Turns)
            {
                case 0: return new Vector2(Center.x + lx, Center.y + lz);
                case 1: return new Vector2(Center.x + lz, Center.y - lx);
                case 2: return new Vector2(Center.x - lx, Center.y - lz);
                default: return new Vector2(Center.x - lz, Center.y + lx);
            }
        }

        /// <summary>The height of the floor the building's local heights count from: 0 for a ground floor, the floor's height
        /// for one of a real-world building's other levels (furnished where it stands, so every fixture's points are its own).</summary>
        public float BaseY;

        /// <summary>The floor the pieces that otherwise find their own (on the ground, or a house floor built over it) stand
        /// on instead: <see cref="BaseY"/> off the ground floor, else none.</summary>
        public float? Floor => BaseY != 0f ? BaseY : (float?)null;

        /// <summary>Converts local (x, y, z) coordinates to world space.</summary>
        public Vector3 W(float lx, float y, float lz)
        {
            Vector2 p = P(lx, lz);
            return new Vector3(p.x, BaseY + y, p.y);
        }

        /// <summary>A local yaw (degrees; 0 faces local +Z) in world terms.</summary>
        public float Yaw(float localYaw) => (Mirror ? -localYaw : localYaw) + (_free ? _freeYaw : Turns * 90f);

        /// <summary>A local horizontal direction in world terms.</summary>
        public Vector3 Dir(float lx, float lz)
        {
            Vector2 a = P(lx, lz) - P(0f, 0f);
            return new Vector3(a.x, 0f, a.y);
        }

        /// <summary>Creates a world‑space rectangle from the given coordinates.</summary>
        public Rect RectW(float x0, float z0, float x1, float z1)
        {
            Vector2 a = P(x0, z0), b = P(x1, z1);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>The building's footprint in world XZ (outer wall faces).</summary>
        public Rect Footprint => RectW(-HalfW - Half, -HalfD - Half, HalfW + Half, HalfD + Half);
        #endregion

        #region Authoring
        /// <summary>Adds a room (local rect on wall centre lines).</summary>
        public RaidRoom Room(string name, float x0, float z0, float x1, float z1, WallDesign design, FloorKind floor)
        {
            var r = new RaidRoom
            {
                Name = name,
                Local = Rect.MinMaxRect(x0, z0, x1, z1),
                World = RectW(x0, z0, x1, z1),
                Design = design,
                Floor = floor,
            };
            Rooms.Add(r);
            return r;
        }

        /// <summary>A doorway centred on local (lx, lz), which must lie on a wall line. Doorways are at
        /// least 1.5 m wide so the navmesh stays connected through them.</summary>
        public void Door(float lx, float lz, float width = 1.5f, bool leaf = true, bool glass = false)
        {
            AddOpening(lx, lz, OpeningKind.Door, Mathf.Max(1.5f, width), o => { o.Leaf = leaf; o.Glass = glass; });
        }

        /// <summary>A window centred on local (lx, lz) on a wall line: most still glazed, a few smashed or boarded over.</summary>
        public void Window(float lx, float lz, float width = 1.2f)
        {
            AddOpening(lx, lz, OpeningKind.Window, width, o =>
            {
                double r = Rng.NextDouble();
                o.Boarded = r < 0.08;
                o.Intact = !o.Boarded && r < 0.86;
            });
        }

        private readonly List<(Vector2 p, OpeningKind kind, float width, System.Action<Opening> setup)> _pendingOpenings
            = new List<(Vector2, OpeningKind, float, System.Action<Opening>)>();

        private void AddOpening(float lx, float lz, OpeningKind kind, float width, System.Action<Opening> setup)
            => _pendingOpenings.Add((P(lx, lz), kind, width, setup));
        #endregion

        #region Shell
        /// <summary>Derives the walls from the rooms, cuts the openings and builds floors, ceilings
        /// and wall segments. Call once all rooms and openings are declared, before furnishing.</summary>
        public void BuildShell()
        {
            if (_shellBuilt) return;
            _shellBuilt = true;
            DeriveRuns();
            ResolveOpenings();
            BuildPieces();
            BuildFloorsAndCeilings();
            ReserveDoorways();
        }

        /// <summary>Collects wall edge spans from rooms and merges them into continuous runs.</summary>
        private void DeriveRuns()
        {
            // Collect horizontal (x‑aligned) and vertical (z‑aligned) wall edge spans from all rooms.
            var hz = new Dictionary<int, List<Vector2>>();
            var vx = new Dictionary<int, List<Vector2>>();
            // Add each room's wall edges to the appropriate horizontal or vertical map.
            foreach (RaidRoom r in Rooms)
            {
                AddEdge(hz, r.World.yMin, r.World.xMin, r.World.xMax);
                AddEdge(hz, r.World.yMax, r.World.xMin, r.World.xMax);
                AddEdge(vx, r.World.xMin, r.World.yMin, r.World.yMax);
                AddEdge(vx, r.World.xMax, r.World.yMin, r.World.yMax);
            }
            Merge(hz, true);
            Merge(vx, false);
        }

        private static void AddEdge(Dictionary<int, List<Vector2>> map, float coord, float a, float b)
        {
            int key = Mathf.RoundToInt(coord * 20f);
            if (!map.TryGetValue(key, out List<Vector2> list)) map[key] = list = new List<Vector2>();
            list.Add(new Vector2(Mathf.Min(a, b), Mathf.Max(a, b)));
        }

        /// <summary>Merge overlapping edge intervals into wall runs (continuous wall segments).</summary>
        private void Merge(Dictionary<int, List<Vector2>> map, bool alongX)
        {
            // For each coordinate bucket (grouped by wall line), merge overlapping spans into a single run.
            foreach (KeyValuePair<int, List<Vector2>> kv in map)
            {
                List<Vector2> list = kv.Value;
                list.Sort((p, q) => p.x.CompareTo(q.x));
                float a = list[0].x, b = list[0].y;
                for (int i = 1; i <= list.Count; i++)
                {
                    if (i < list.Count && list[i].x <= b + 0.01f) { b = Mathf.Max(b, list[i].y); continue; }
                    // Emit a wall run that covers the merged span.
                    _runs.Add(new WallRun { AlongX = alongX, Coord = kv.Key / 20f, A = a, B = b });
                    if (i < list.Count) { a = list[i].x; b = list[i].y; }
                }
            }
        }

        /// <summary>Match pending opening requests to wall runs, validate placement, and create Opening objects.</summary>
        private void ResolveOpenings()
        {
            // Process each pending opening request (door/window) queued during authoring.
            foreach (var po in _pendingOpenings)
            {
                WallRun run = null;
                float pos = 0f;
                // Find the wall run that aligns with the opening point (within tolerance) and lies within the wall span.
                // Iterate each wall run to generate wall pieces (segments) based on openings.
                foreach (WallRun r in _runs)
                {
                    float across = r.AlongX ? po.p.y : po.p.x;
                    float along = r.AlongX ? po.p.x : po.p.y;
                    if (Mathf.Abs(across - r.Coord) < 0.06f && along > r.A + 0.3f && along < r.B - 0.3f) { run = r; pos = along; break; }
                }
                if (run == null) { Town.Warn(Name + ": opening at " + po.p + " is not on a wall"); continue; }
                var o = new Opening { Run = run, Kind = po.kind, Pos = pos, Width = po.width };
                po.setup?.Invoke(o);
                // Keep a solid jamb at each end of the run.
                float min = run.A + 0.35f + o.Width * 0.5f, max = run.B - 0.35f - o.Width * 0.5f;
                if (min > max) { Town.Warn(Name + ": wall too short for an opening at " + po.p); continue; }
                o.Pos = Mathf.Clamp(o.Pos, min, max);
                bool clash = false;
                // Check against existing openings on this wall run for overlap.
                foreach (Opening other in run.Openings)
                    if (o.A < other.B + 0.3f && o.B > other.A - 0.3f) clash = true;
                if (clash) { Town.Warn(Name + ": overlapping openings at " + po.p); continue; }
                run.Openings.Add(o);
                _openings.Add(o);
            }
            _pendingOpenings.Clear();
        }

        private float RunStart(WallRun r) => r.AlongX ? r.A - Half : r.A + Half;
        private float RunEnd(WallRun r) => r.AlongX ? r.B + Half : r.B - Half;

        /// <summary>Create wall pieces (segments) between openings and fill remaining wall spans.</summary>
        private void BuildPieces()
        {
            foreach (WallRun r in _runs)
            {
                // Sort openings along the run to process them left‑to‑right.
                r.Openings.Sort((p, q) => p.Pos.CompareTo(q.Pos));
                float cursor = RunStart(r), end = RunEnd(r);
                foreach (Opening o in r.Openings)
                {
                    // If there is a gap before this opening, create a solid wall piece.
                    if (o.A > cursor + 0.01f) _pieces.Add(new WallPiece { Run = r, S = cursor, E = o.A });
                    // Create a wall piece that contains this opening.
                    _pieces.Add(new WallPiece { Run = r, S = o.A, E = o.B, Opening = o });
                    cursor = o.B;
                }
                // Add trailing wall piece after the last opening, if any length remains.
                if (end > cursor + 0.01f) _pieces.Add(new WallPiece { Run = r, S = cursor, E = end });
            }
            foreach (WallPiece p in _pieces) DrawPiece(p);
        }

        // World position from run coordinates: along the run, height, and offset across it.
        private static Vector3 RunPoint(WallRun r, float along, float y, float across)
            => r.AlongX ? new Vector3(along, y, r.Coord + across) : new Vector3(r.Coord + across, y, along);

        private static Vector3 RunNormal(WallRun r, int side) => r.AlongX ? new Vector3(0f, 0f, side) : new Vector3(side, 0f, 0f);

        private static Vector3 RunSize(WallRun r, float len, float h, float thick)
            => r.AlongX ? new Vector3(len, h, thick) : new Vector3(thick, h, len);

        /// <summary>Rooms touching the run on one side, with their inner extents along it.</summary>
        private void RoomsOnSide(WallRun r, int side, List<(RaidRoom room, float a, float b)> into)
        {
            into.Clear();
            foreach (RaidRoom room in Rooms)
            {
                Rect w = room.World;
                float edge = r.AlongX ? (side > 0 ? w.yMin : w.yMax) : (side > 0 ? w.xMin : w.xMax);
                if (Mathf.Abs(edge - r.Coord) > 0.06f) continue;
                float a = (r.AlongX ? w.xMin : w.yMin) + Half;
                float b = (r.AlongX ? w.xMax : w.yMax) - Half;
                if (b > r.A && a < r.B) into.Add((room, a, b));
            }
        }

        private readonly List<(RaidRoom room, float a, float b)> _sideRooms = new List<(RaidRoom, float, float)>();

        private void DrawPiece(WallPiece p)
        {
            WallRun r = p.Run;
            RaidMesh m = p.Mesh;
            float len = p.E - p.S, mid = (p.S + p.E) * 0.5f;
            Surf core = DecorPalette.Matte(WallCore);

            // Height bands this piece fills.
            float y0a = 0f, y1a = WallH, y0b = -1f, y1b = -1f;
            if (p.Opening != null)
            {
                if (p.Opening.Kind == OpeningKind.Door) { y0a = ShellDoorHeight; }
                else { y1a = SillHeight; y0b = HeadHeight; y1b = WallH; }
            }
            AddBand(p, mid, len, y0a, y1a, core);
            if (y0b >= 0f) AddBand(p, mid, len, y0b, y1b, core);

            if (p.Opening != null)
            {
                if (p.Opening.Kind == OpeningKind.Door) { if (DoorFrames) DrawDoorFrame(p); }
                else DrawWindow(p);
            }
        }

        private void AddBand(WallPiece p, float mid, float len, float y0, float y1, in Surf core)
        {
            if (y1 - y0 < 0.05f) return;   // e.g. a doorway through a wall lower than a door
            WallRun r = p.Run;
            float h = y1 - y0;
            p.Mesh.Box(RunPoint(r, mid, (y0 + y1) * 0.5f, 0f), RunSize(r, len, h, Half * 2f), core);
            p.Colliders.Add(new Bounds(RunPoint(r, mid, (y0 + y1) * 0.5f, 0f), RunSize(r, len, h, Half * 2f)));
            for (int side = -1; side <= 1; side += 2) SkinBand(p, side, y0, y1);
        }

        private void SkinBand(WallPiece p, int side, float y0, float y1)
        {
            WallRun r = p.Run;
            Vector3 n = RunNormal(r, side);
            RoomsOnSide(r, side, _sideRooms);
            // Interior faces, one per room on this side.
            var covered = new List<Vector2>(2);
            foreach (var sr in _sideRooms)
            {
                float a = Mathf.Max(p.S, sr.a), b = Mathf.Min(p.E, sr.b);
                if (b - a < 0.02f) continue;
                covered.Add(new Vector2(a, b));
                float l = b - a, c = (a + b) * 0.5f;
                p.Mesh.Upright(RunPoint(r, c, (y0 + y1) * 0.5f, side * (Half + 0.003f)), n, l, y1 - y0, DecorMaterials.WallInterior(sr.room.Design));
                Color trim = sr.room.Design.Trim;
                if (y0 < 0.1f)
                    p.Mesh.Box(RunPoint(r, c, 0.06f, side * (Half + 0.008f)), RunSize(r, l, 0.12f, 0.016f), DecorPalette.Matte(trim));
                float rail = sr.room.Design.RailHeight;
                if (y0 < rail && y1 > rail + 0.05f)
                    p.Mesh.Box(RunPoint(r, c, rail + 0.01f, side * (Half + 0.012f)), RunSize(r, l, 0.06f, 0.024f), DecorPalette.Matte(trim * 1.05f));
            }
            // Whatever no room covers is outside: siding / brick / block.
            float s = p.S, e = p.E;
            covered.Sort((u, v) => u.x.CompareTo(v.x));
            float cursor = s;
            for (int i = 0; i <= covered.Count; i++)
            {
                float next = i < covered.Count ? covered[i].x : e;
                if (next - cursor > 0.25f)
                {
                    float a = cursor, b = next;
                    // Exterior faces of N-S walls also wrap the corner posts of the E-W walls.
                    if (!r.AlongX && Mathf.Abs(a - RunStart(r)) < 0.01f) a -= Half * 2f;
                    if (!r.AlongX && Mathf.Abs(b - RunEnd(r)) < 0.01f) b += Half * 2f;
                    p.Mesh.Upright(RunPoint(r, (a + b) * 0.5f, (y0 + y1) * 0.5f, side * (Half + 0.004f)), n, b - a, y1 - y0, DecorMaterials.WallExterior(Exterior, ExteriorColor));
                    // Draw baseboard trim for exterior wall at floor level.
                    DrawBaseboard(p, side, y0, a, b);
                }
                if (i < covered.Count) cursor = Mathf.Max(cursor, covered[i].y);
            }
        }

        /// <summary>Draws a thin wooden baseboard along the bottom of an exterior wall face.</summary>
        private void DrawBaseboard(WallPiece p, int side, float y0, float a, float b)
        {
            // Only draw if the band starts near the floor (y0 ≈ floor level).
            if (y0 > 0.12f) return; // floor‑level tolerance
            WallRun r = p.Run;
            // Length of the wall segment.
            float len = b - a;
            // Center point of the segment.
            float c = (a + b) * 0.5f;
            // Baseboard dimensions: ~7 cm tall, ~2 cm thick.
            const float boardH = 0.07f;
            const float boardT = 0.02f;
            // Slight colour variation using the building’s deterministic RNG.
            Color boardCol = Casing * (0.9f + (float)Rng.NextDouble() * 0.2f);
            var boardMat = DecorPalette.Matte(boardCol);
            // Position the board a little above the floor to avoid Z‑fighting.
            float yPos = y0 + boardH * 0.5f + 0.01f;
            // Offset outward from the wall core (Half) by a small margin.
            float off = side * (Half + 0.011f);
            p.Mesh.Box(RunPoint(r, c, yPos, off), RunSize(r, len, boardH, boardT), boardMat);
        }

        private void DrawDoorFrame(WallPiece p)
        {
            WallRun r = p.Run;
            Opening o = p.Opening;
            Surf wood = DecorPalette.Matte(Casing);
            float w = o.Width;
            // Jamb and head linings cover the cut ends of the wall.
            p.Mesh.Box(RunPoint(r, o.A + 0.012f, ShellDoorHeight * 0.5f, 0f), RunSize(r, 0.024f, ShellDoorHeight, Half * 2f + 0.01f), wood);
            p.Mesh.Box(RunPoint(r, o.B - 0.012f, ShellDoorHeight * 0.5f, 0f), RunSize(r, 0.024f, ShellDoorHeight, Half * 2f + 0.01f), wood);
            p.Mesh.Box(RunPoint(r, o.Pos, ShellDoorHeight - 0.012f, 0f), RunSize(r, w, 0.024f, Half * 2f + 0.01f), wood);
            // Casings on both faces.
            for (int side = -1; side <= 1; side += 2)
            {
                float off = side * (Half + 0.012f);
                p.Mesh.Box(RunPoint(r, o.A - 0.035f, (ShellDoorHeight + 0.07f) * 0.5f, off), RunSize(r, 0.07f, ShellDoorHeight + 0.07f, 0.024f), wood);
                p.Mesh.Box(RunPoint(r, o.B + 0.035f, (ShellDoorHeight + 0.07f) * 0.5f, off), RunSize(r, 0.07f, ShellDoorHeight + 0.07f, 0.024f), wood);
                p.Mesh.Box(RunPoint(r, o.Pos, ShellDoorHeight + 0.035f, off), RunSize(r, w + 0.14f, 0.07f, 0.024f), wood);
            }
        }

        private void DrawWindow(WallPiece p)
        {
            WallRun r = p.Run;
            Opening o = p.Opening;
            Surf wood = DecorPalette.Matte(Casing * 1.1f);
            float w = o.Width, hh = HeadHeight - SillHeight, cy = (SillHeight + HeadHeight) * 0.5f;
            // Linings inside the opening.
            p.Mesh.Box(RunPoint(r, o.A + 0.012f, cy, 0f), RunSize(r, 0.024f, hh, Half * 2f + 0.01f), wood);
            p.Mesh.Box(RunPoint(r, o.B - 0.012f, cy, 0f), RunSize(r, 0.024f, hh, Half * 2f + 0.01f), wood);
            p.Mesh.Box(RunPoint(r, o.Pos, HeadHeight - 0.012f, 0f), RunSize(r, w, 0.024f, Half * 2f + 0.01f), wood);
            p.Mesh.Box(RunPoint(r, o.Pos, SillHeight + 0.015f, 0f), RunSize(r, w, 0.03f, Half * 2f + 0.01f), wood);
            for (int side = -1; side <= 1; side += 2)
            {
                float off = side * (Half + 0.012f);
                p.Mesh.Box(RunPoint(r, o.A - 0.03f, cy, off), RunSize(r, 0.06f, hh + 0.12f, 0.024f), wood);
                p.Mesh.Box(RunPoint(r, o.B + 0.03f, cy, off), RunSize(r, 0.06f, hh + 0.12f, 0.024f), wood);
                p.Mesh.Box(RunPoint(r, o.Pos, HeadHeight + 0.03f, off), RunSize(r, w + 0.12f, 0.06f, 0.024f), wood);
                // A deeper sill ledge, with snow on the outside one.
                p.Mesh.Box(RunPoint(r, o.Pos, SillHeight + 0.02f, side * (Half + 0.035f)), RunSize(r, w + 0.16f, 0.04f, 0.07f), wood);
            }
            // The upper sash, fixed in the outer track (and, outside a house, its shutters and flower box). What moves or
            // changes — the lower sash, the glass, the lock's lever, the dressing, any boards — is fitted once the segment is
            // an object of its own (FitWindow, RaidBuilding.Windows.cs).
            WindowFrame(p);
            int sx = OutsideSide(r, o.Pos);
            if (sx != 0) p.Mesh.Box(RunPoint(r, o.Pos, SillHeight + 0.05f, sx * (Half + 0.04f)), RunSize(r, w + 0.1f, 0.03f, 0.06f), DecorPalette.Matte(DecorProps.SnowWhite));
        }

        /// <summary>Which side of a run (+1 / -1) is outside at <paramref name="along"/>, or 0 for an
        /// interior wall.</summary>
        private int OutsideSide(WallRun r, float along)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                RoomsOnSide(r, side, _sideRooms);
                bool any = false;
                foreach (var sr in _sideRooms) if (along >= sr.a - 0.2f && along <= sr.b + 0.2f) any = true;
                if (!any) return side;
            }
            return 0;
        }

        private void BuildFloorsAndCeilings()
        {
            foreach (RaidRoom room in Rooms)
            {
                Rect inner = room.Inner;
                Vector3 c = new Vector3(inner.center.x, 0.02f, inner.center.y);
                Flat.Flat(c, inner.width, inner.height, 0f, FloorSurf(room));

                Surf ceil = DecorMaterials.Ceiling;
                if (!FlatCeilings) continue;
                Ceil.Box(new Vector3(inner.center.x, WallH + 0.03f, inner.center.y), new Vector3(inner.width + 0.02f, 0.06f, inner.height + 0.02f), ceil);
                Town.OverheadCollider(new Vector3(inner.center.x, WallH + 0.03f, inner.center.y), new Vector3(inner.width + 0.2f, 0.06f, inner.height + 0.2f), Quaternion.identity);
            }

            // Thresholds bridge the floor through each doorway.
            foreach (Opening o in _openings)
            {
                if (o.Kind != OpeningKind.Door) continue;
                WallRun r = o.Run;
                Vector3 c = RunPoint(r, o.Pos, 0.024f, 0f);
                Surf s = DecorPalette.Matte(new Color(0.40f, 0.30f, 0.20f));
                if (r.AlongX) Flat.Flat(c, o.Width, Half * 2f + 0.02f, 0f, s);
                else Flat.Flat(c, Half * 2f + 0.02f, o.Width, 0f, s);
            }
        }

        private static Surf FloorSurf(RaidRoom room)
        {
            switch (room.Floor)
            {
                case FloorKind.LinoBeige: return DecorMaterials.Linoleum(new Color(0.62f, 0.57f, 0.46f), new Color(0.70f, 0.66f, 0.55f), false);
                case FloorKind.LinoGreen: return DecorMaterials.Linoleum(new Color(0.52f, 0.60f, 0.56f), new Color(0.62f, 0.68f, 0.64f), false);
                case FloorKind.Checker: return DecorMaterials.Linoleum(new Color(0.76f, 0.74f, 0.66f), new Color(0.20f, 0.20f, 0.22f), true);
                case FloorKind.Wood: return DecorMaterials.WoodFloor(new Color(0.46f, 0.31f, 0.19f));
                case FloorKind.WoodDark: return DecorMaterials.WoodFloor(new Color(0.33f, 0.22f, 0.14f));
                case FloorKind.Carpet: return DecorMaterials.Carpet(room.FloorTint);
                case FloorKind.Concrete: return DecorMaterials.Concrete;
                case FloorKind.Plywood: return DecorMaterials.Planks(new Color(0.52f, 0.44f, 0.32f));
                default: return DecorMaterials.Linoleum(new Color(0.47f, 0.47f, 0.46f), new Color(0.56f, 0.56f, 0.54f), false);
            }
        }

        /// <summary>Keeps the floor in front of every doorway (both sides) clear of furniture.</summary>
        private void ReserveDoorways()
        {
            foreach (Opening o in _openings)
            {
                if (o.Kind != OpeningKind.Door) continue;
                WallRun r = o.Run;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 a = RunPoint(r, o.A - 0.3f, 0f, side * Half);
                    Vector3 b = RunPoint(r, o.B + 0.3f, 0f, side * (Half + 1.3f));
                    Rect zone = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
                    RaidRoom room = RoomAt(RunPoint(r, o.Pos, 0f, side * 0.6f));
                    // Skip this attempt if the target room does not exist.
                    if (room == null) continue;
                    room.Reserved.Add(zone);
                    o.Zones.Add(zone);
                }
            }
        }

        /// <summary>Hangs the doors once the rooms are furnished, and queues the windows' working parts: a doorway declared
        /// with a leaf gets a pair of leaves hinged at its jambs (<see cref="HangDoor"/>, a working <see cref="RaidDoor"/>),
        /// swinging open into whichever room leaves them clear of furniture and other doorways, to rest leaning back near the
        /// wall beside the gap (never across it). A doorway with no clear swing is left without a door. Every window is
        /// fitted with its sash, glass and dressing (<see cref="FitWindow"/>), once its segment is an object.</summary>
        private void PlaceAllLeaves()
        {
            foreach (Opening o in _openings)
                if (o.Kind == OpeningKind.Door && o.Leaf) PlaceLeaves(o);
            foreach (WallPiece p in _pieces)
            {
                if (p.Opening == null || p.Opening.Kind != OpeningKind.Window) continue;
                WallPiece piece = p;
                Later(() => FitWindow(piece));
            }
        }

        /// <summary>Place door leaf meshes for each door opening flagged as having leaves.</summary>
        private void PlaceLeaves(Opening o)
        {
            // Identify the two adjacent rooms on either side of the doorway (if any).
            WallRun r = o.Run;
            RaidRoom plus = RoomAt(RunPoint(r, o.Pos, 0f, 0.6f)), minus = RoomAt(RunPoint(r, o.Pos, 0f, -0.6f));
            // An outer door swings in; between two rooms, into the smaller one (a bedroom or a bathroom off the living room,
            // an office off the hall) when its swing is clear.
            // Choose which side the door swings into based on room sizes and random tie‑break.
            int first;
            if (plus == null) first = -1;
            else if (minus == null) first = 1;
            else
            {
                float ap = plus.World.width * plus.World.height, am = minus.World.width * minus.World.height;
                first = Mathf.Abs(ap - am) < 0.5f ? (Rng.NextDouble() < 0.5 ? -1 : 1) : (ap < am ? 1 : -1);
            }
            // A pair of leaves fills the doorway (raid doorways are 1.5 m or more), each hinged at its jamb.
            // Compute width of each leaf panel, accounting for clearance gaps.
            float leafW = (o.Width - 2f * LeafClear - LeafMeet) * 0.5f;
            float leanPick = 5f + (float)Rng.NextDouble() * 25f;
            var rects = new Rect[2];
            var leans = new float[2];
            Vector3 along = r.AlongX ? Vector3.right : Vector3.forward;

            // Try up to four leaf placement attempts (two sides, with/without lean).
            for (int attempt = 0; attempt < 4; attempt++)
            {
                // Select which side to attempt first based on earlier swing direction.
                int side = (attempt & 1) == 0 ? first : -first;
                RaidRoom room = side > 0 ? plus : minus;
                if (room == null) continue;
                // Desired leaf lean angle for this attempt (first two attempts try random lean, later attempts try straight).
                float leanWanted = attempt < 2 ? leanPick : 0f;

                // The room's clear extent along this wall bounds how far a leaf may lean.
                // Clamp leaf swing within the unobstructed wall segment of the chosen room.
                float ra = r.A, rb = r.B;
                RoomsOnSide(r, side, _sideRooms);
                foreach (var sr in _sideRooms)
                    if (o.Pos > sr.a && o.Pos < sr.b) { ra = sr.a; rb = sr.b; }
                Vector3 n = RunNormal(r, side);

                // Validate that both leaves fit without intersecting furniture or reserved zones.
                bool clear = true;
                for (int i = 0; i < 2 && clear; i++)
                {
                    bool atA = i == 0;
                    float hingeAlong = atA ? o.A + LeafClear : o.B - LeafClear;
                    Vector3 hinge = RunPoint(r, hingeAlong, 0f, side * (Half - LeafInset));
                    float free = (atA ? hingeAlong - ra : rb - hingeAlong) - 0.12f;
                    float maxLean = Mathf.Asin(Mathf.Clamp01(free / leafW)) * Mathf.Rad2Deg;
                    float lean = Mathf.Min(maxLean, leanWanted);
                    Vector3 away = atA ? -along : along;
                    Vector3 dir = n * Mathf.Cos(lean * Mathf.Deg2Rad) + away * Mathf.Sin(lean * Mathf.Deg2Rad);
                    Vector3 tip = hinge + dir * leafW;
                    rects[i] = Rect.MinMaxRect(Mathf.Min(hinge.x, tip.x) - 0.1f, Mathf.Min(hinge.z, tip.z) - 0.1f, Mathf.Max(hinge.x, tip.x) + 0.1f, Mathf.Max(hinge.z, tip.z) + 0.1f);
                    leans[i] = lean;
                    if (room.Overlaps(rects[i], false)) clear = false;
                    foreach (Rect z in room.Reserved)
                        if (!o.Zones.Contains(z) && z.Overlaps(rects[i])) clear = false;
                }
                if (!clear) continue;

                // Reserve the leaf footprints in the room's furniture list.
                room.Furniture.Add(rects[0]);
                room.Furniture.Add(rects[1]);
                int swing = side;
                float leanA = leans[0], leanB = leans[1];
                // Defer actual leaf mesh creation until after geometry is built.
                Later(() => HangDoor(o, swing, leafW, leanA, leanB));
                return;
            }
        }

        /// <summary>The room containing a world point, or null.</summary>
        public RaidRoom RoomAt(Vector3 p)
        {
            foreach (RaidRoom r in Rooms) if (r.World.Contains(new Vector2(p.x, p.z))) return r;
            return null;
        }

        /// <summary>Adds the ground in front of each outside door (3 m out) to <paramref name="into"/>, so
        /// outdoor props never block a way in.</summary>
        public void ExteriorDoorZones(List<Rect> into)
        {
            foreach (Opening o in _openings)
            {
                if (o.Kind != OpeningKind.Door) continue;
                int outside = OutsideSide(o.Run, o.Pos);
                if (outside == 0) continue;
                Vector3 a = RunPoint(o.Run, o.A - 0.5f, 0f, outside * Half);
                Vector3 b = RunPoint(o.Run, o.B + 0.5f, 0f, outside * (Half + 3f));
                into.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z)));
            }
        }
        #endregion
    }
}
