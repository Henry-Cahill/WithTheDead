using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The floors above and below the ground one, and the stairwell between them (see KentuckyMap.Levels.cs): which buildings
    // have them, and their structure, drawn with the building into its shell a level at a time (BuildingShell.Structure:
    // the cutaway hides a level with everything else of it, and the NavMesh is baked from them, so the dead follow the
    // survivors up and down) with one mesh collider for all of it; the stairwell's own walls a level at a time are objects
    // of their own with their colliders (BuildingShell.Stairwells), which the isometric camera cuts away as it does the
    // rooms' partitions. Each level above the lowest has a floor over the whole outline (the ground floor over a basement:
    // over its main block) less the stairwell's shaft; the core holds two flights a storey, one up from each landing to the
    // half landing and one on up to the next floor, with a wall between them, walls of its own where it isn't against an
    // outside wall, and at the top a rail round the opening; a basement has a concrete floor and walls round the main block.
    // The flights are steps to look at and to bake (the NavMesh climbs them) and a smooth ramp each to walk on (a
    // survivor's body doesn't stumble on them).
    internal static partial class BuildingPass
    {
        /// <summary>Thickness of a step's block below its tread, and of a ramp's collider.</summary>
        private const float StepBlock = 0.1f, RampThick = 0.1f, HalfLandingThick = 0.15f;

        private static Surf s_Plaster, s_StairWood;
        private static Surf Plaster => Cached(ref s_Plaster, () => DecorPalette.Matte(new Color(0.80f, 0.78f, 0.72f)));
        private static Surf StairWood => Cached(ref s_StairWood, () => DecorMaterials.WoodFloor(new Color(0.48f, 0.33f, 0.20f)));

        // The levels of a building raised in Full detail with a door and a shell of its own: every storey a floor, and a
        // basement under a house that has one (KentuckyMap.Levels); nothing when there's no room for a stairwell.
        private static void PlanLevels(Job j)
        {
            Plan p = j.P;
            if (j.Record == null || j.Lite || j.Scenery || j.Edge || j.Site.Detail != Detail.Full || j.Shell == null || j.Shell.Batched) return;
            bool basement = (p.Arch == Arch.House || p.Arch == Arch.Row) && Levels.BasementOf(j.F) != null;
            float top = p.Roof == RoofKind.Flat ? p.Eave : Mathf.Max(p.Eave, p.FirstStorey + (p.Storeys - 1) * p.Storey);
            LevelLayout L = Levels.Plan(j.Record, p.Storeys, p.FirstStorey, p.Storey, top, basement);
            if (L == null && basement) L = Levels.Plan(j.Record, p.Storeys, p.FirstStorey, p.Storey, top, false);
            if (L == null) return;
            j.Record.Levels = L;
            j.Record.Storeys = L.Top + 1;
            j.Shell.Levels = L;
            j.Shell.Structure = new RaidMesh[L.Count];
            for (int i = 0; i < L.Count; i++) j.Shell.Structure[i] = new RaidMesh();
        }

        // Every level's floor, the stairs and the stairwell's walls, and their collider.
        private static void DrawLevels(Job j)
        {
            LevelLayout L = j.Record != null ? j.Record.Levels : null;
            if (L == null || j.Shell.Structure == null) return;
            Plan p = j.P;
            bool home = p.Arch == Arch.House || p.Arch == Arch.Apartments || p.Arch == Arch.Motel || p.Arch == Arch.Row;
            Surf floor = FloorOf(p), tread = home ? StairWood : ConcreteSurf;
            var col = new LevelCollision();
            Rect shaft = L.Shaft;
            j.Shell.Stairwells = new GameObject[L.Count];
            for (int level = L.Lowest; level <= L.Top; level++)
            {
                RaidMesh m = j.Shell.Structure[level - L.Lowest];
                float y = L.FloorOf(level);
                if (level < 0) BasementShell(L, m, col);
                else if (level == 0 && L.Basement) GroundOverBasement(L, m, col, floor, shaft);
                else if (level > 0) UpperFloor(j, L, m, col, y, floor, shaft);
                if (level < L.Top) Flights(L, m, col, level, tread);
                j.Shell.Stairwells[level - L.Lowest] = Stairwell(j, L, level);
                if (level == L.Top && L.Top > L.Lowest) TopRail(L, m, col, y);
                if (level == L.Top && level >= 1) TopCeiling(j, L, m);
            }
            col.Emit(j.Site, j.Id, L);
        }

        // The stairwell's own walls on a level, an object of their own with their collider: the isometric camera cuts it
        // away when it stands between the survivor and the camera, as it does the rooms' partitions.
        private static GameObject Stairwell(Job j, LevelLayout L, int level)
        {
            var m = new RaidMesh();
            var col = new LevelCollision();
            CoreWalls(L, m, col, level);
            if (m.IsEmpty) return null;
            string name = "Stairwell " + j.Id + " " + level;
            var go = new GameObject(name);
            go.transform.SetParent(j.Site.Town.World, false);
            Mesh shape = col.Bake(name);
            if (shape != null) go.AddComponent<MeshCollider>().sharedMesh = shape;
            Vector3 origin = To3(L.CoreSite(L.Length * 0.5f, L.Width * 0.5f), L.FloorOf(level));
            GameObject walls = m.Emit("Walls", go.transform, origin, UnityEngine.Rendering.ShadowCastingMode.On);
            if (walls != null) walls.transform.localPosition = origin;
            m.Release();
            go.AddComponent<CutawayOccluder>();
            return go;
        }

        #region Floors
        // A storey's floor over the whole outline, less the stairwell's shaft: its top at the floor, its underside the
        // ceiling of the storey below.
        private static void UpperFloor(Job j, LevelLayout L, RaidMesh m, LevelCollision col, float y, in Surf floor, Rect shaft)
        {
            List<int> tris = j.Tris;
            List<Vector2> pts = j.TriPts;
            var tri = new List<Vector2>(3);
            var pieces = new List<List<Vector2>>(4);
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                tri.Clear();
                tri.Add(pts[tris[t]]);
                tri.Add(pts[tris[t + 1]]);
                tri.Add(pts[tris[t + 2]]);
                pieces.Clear();
                if (!FrameBounds(L, tri).Overlaps(shaft)) pieces.Add(new List<Vector2>(tri));
                else Levels.OutsideRect(L, tri, shaft, pieces);
                foreach (List<Vector2> piece in pieces) SlabPiece(m, col, piece, y, y - Levels.Slab, floor, CeilingSurf);
            }
            ShaftEdge(L, m, col, y);
        }

        // The ground floor over a basement: the main block less the shaft (the rest of the outline is the exteriors' floor on
        // the ground).
        private static void GroundOverBasement(LevelLayout L, RaidMesh m, LevelCollision col, in Surf floor, Rect shaft)
        {
            var rects = new List<Rect>(4);
            Levels.RectLess(L.Main, shaft, rects);
            float y = Levels.GroundFloorY;
            foreach (Rect r in rects) SlabPiece(m, col, Levels.Corners(L, r), y, y - Levels.Slab, floor, ConcreteSurf);
            ShaftEdge(L, m, col, y);
        }

        // The face of the floor's edge along the shaft's near side (the landing's edge, the last riser of the flight up to it).
        private static void ShaftEdge(LevelLayout L, RaidMesh m, LevelCollision col, float y)
        {
            CoreBox(L, m, col, L.Near - 0.04f, L.Near, 0f, L.Width, y - Levels.Slab - 0.2f, y, ConcreteSurf, true);
        }

        // A convex piece of floor: its top face up at `top`, its underside down at `bottom`, both in the collider.
        private static void SlabPiece(RaidMesh m, LevelCollision col, List<Vector2> poly, float top, float bottom, in Surf up, in Surf down)
        {
            if (poly.Count < 3) return;
            if (MapGeometry.SignedArea(poly) > 0f) poly.Reverse();   // clockwise seen from above: the top face looks up
            for (int i = 1; i + 1 < poly.Count; i++)
            {
                Vector3 a = To3(poly[0], top), b = To3(poly[i], top), c = To3(poly[i + 1], top);
                m.Tri(a, b, c, up);
                col.Tri(a, b, c);
                Vector3 a2 = To3(poly[0], bottom), b2 = To3(poly[i], bottom), c2 = To3(poly[i + 1], bottom);
                m.Tri(a2, c2, b2, down);
                col.Tri(a2, c2, b2);
            }
        }

        // The top storey's ceiling, under the roof (the storeys below have the floor above them for one).
        private static void TopCeiling(Job j, LevelLayout L, RaidMesh m)
        {
            List<int> tris = j.Tris;
            List<Vector2> pts = j.TriPts;
            float y = L.CeilingOf(L.Top) - 0.03f;
            for (int t = 0; t + 2 < tris.Count; t += 3)
                m.Tri(To3(pts[tris[t]], y), To3(pts[tris[t + 2]], y), To3(pts[tris[t + 1]], y), CeilingSurf);
        }

        // A basement: a concrete floor over the main block and concrete walls round it, up to the ground floor.
        private static void BasementShell(LevelLayout L, RaidMesh m, LevelCollision col)
        {
            Rect r = L.Main;
            float floor = L.FloorOf(-1);
            SlabPiece(m, col, Levels.Corners(L, r), floor, floor - 0.2f, ConcreteSurf, ConcreteSurf);
            const float t = RaidBuilding.Half;
            FrameBox(L, m, col, r.xMin - t, r.xMax + t, r.yMin - t, r.yMin + t, floor, 0f, ConcreteSurf);
            FrameBox(L, m, col, r.xMin - t, r.xMax + t, r.yMax - t, r.yMax + t, floor, 0f, ConcreteSurf);
            FrameBox(L, m, col, r.xMin - t, r.xMin + t, r.yMin + t, r.yMax - t, floor, 0f, ConcreteSurf);
            FrameBox(L, m, col, r.xMax - t, r.xMax + t, r.yMin + t, r.yMax - t, floor, 0f, ConcreteSurf);
        }
        #endregion

        #region The stairs
        // Storey `level`'s stairs: the flight up from its landing along the A side to the half landing at the far end, and
        // the flight on up along the B side to the next floor's landing, the wall between them.
        private static void Flights(LevelLayout L, RaidMesh m, LevelCollision col, int level, in Surf tread)
        {
            const float h = RaidBuilding.Half, fw = Levels.FlightWidth;
            float f0 = L.FloorOf(level), f1 = L.FloorOf(level + 1), rise = (f1 - f0) * 0.5f, near = L.Near, far = near + L.Run;
            int n = Mathf.Max(2, Mathf.CeilToInt(rise / Levels.MaxRiser - 1e-4f));
            float riser = rise / n, step = L.Run / (n - 1);
            float a0 = h, a1 = h + fw, b0 = h + fw + Levels.Spine, b1 = L.Width - h;
            // The ground floor stands a little above the ground (the exteriors' floor; the floor over a basement).
            if (level == 0) f0 = Levels.GroundFloorY;
            if (level + 1 == 0) f1 = Levels.GroundFloorY;
            rise = (f1 - f0) * 0.5f;
            riser = rise / n;
            for (int i = 1; i < n; i++)
            {
                float top = f0 + i * riser;
                CoreBox(L, m, null, near + (i - 1) * step, near + i * step, a0, a1, top - riser - StepBlock, top, tread, false);
                float topB = f0 + rise + i * riser;
                CoreBox(L, m, null, far - i * step, far - (i - 1) * step, b0, b1, topB - riser - StepBlock, topB, tread, false);
            }
            // The half landing, and the ramps walked up (a body glides up a slope, the NavMesh climbs the steps).
            CoreBox(L, m, col, far, L.Length - h, a0, b1, f0 + rise - HalfLandingThick, f0 + rise, tread, true);
            Ramp(L, col, near - 0.05f, f0, far + 0.05f, f0 + rise, a0, a1);
            Ramp(L, col, far + 0.05f, f0 + rise, near - 0.05f, f1, b0, b1);
            // The wall between the flights, the storey's height (ending at the floor above, or for the top a rail's height).
            CoreBox(L, m, col, near, far, a1, b0, f0, f1, Plaster, true);
        }

        // At the top floor, a rail along the landing's edge where no flight goes on up, and the wall between the flights
        // carried up a rail's height (the flight below arrives beside the open shaft).
        private static void TopRail(LevelLayout L, RaidMesh m, LevelCollision col, float y)
        {
            const float h = RaidBuilding.Half, fw = Levels.FlightWidth;
            float near = L.Near, far = near + L.Run;
            CoreBox(L, m, col, near, near + 0.08f, h, h + fw, y, y + Levels.RailHeight, Plaster, true);
            CoreBox(L, m, col, near, far, h + fw, h + fw + Levels.Spine, y, y + Levels.RailHeight, Plaster, true);
        }

        // The stairwell's own walls on a level: toward the rooms always (but over a landing that opens on that side), and on the
        // other sides where they aren't against an outside wall; from the level's floor to the next.
        private static void CoreWalls(LevelLayout L, RaidMesh m, LevelCollision col, int level)
        {
            const float h = RaidBuilding.Half;
            float y0 = L.FloorOf(level), y1 = L.CeilingOf(level);
            if (L.WallB) CoreBox(L, m, col, L.OpenSide ? L.Near : 0f, L.Length, L.Width - h, L.Width + h, y0, y1, Plaster, true);
            if (L.WallA) CoreBox(L, m, col, 0f, L.Length, -h, h, y0, y1, Plaster, true);
            if (L.WallFar) CoreBox(L, m, col, L.Length - h, L.Length + h, 0f, L.Width, y0, y1, Plaster, true);
            if (L.WallNear) CoreBox(L, m, col, -h, h, 0f, L.Width, y0, y1, Plaster, true);
        }

        // A ramp's collider: a thin slab whose top runs from (u0, y0) to (u1, y1) along the core, across v0..v1.
        private static void Ramp(LevelLayout L, LevelCollision col, float u0, float y0, float u1, float y1, float v0, float v1)
        {
            Vector2 dir = L.DirToSite(L.Up) * Mathf.Sign(u1 - u0);
            float run = Mathf.Abs(u1 - u0), length = Mathf.Sqrt(run * run + (y1 - y0) * (y1 - y0));
            float pitch = Mathf.Atan2(y1 - y0, run) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 0f) * Quaternion.Euler(-pitch, 0f, 0f);
            Vector2 mid = L.CoreSite((u0 + u1) * 0.5f, (v0 + v1) * 0.5f);
            Vector3 centre = new Vector3(mid.x, (y0 + y1) * 0.5f, mid.y) - rot * Vector3.up * (RampThick * 0.5f);
            col.Box(centre, new Vector3(v1 - v0, RampThick, length), rot);
        }
        #endregion

        #region Boxes in the core's and the frame's axes
        // A box spanning u0..u1 along the core and v0..v1 across it, from y0 to y1; into the collider too when `solid`.
        private static void CoreBox(LevelLayout L, RaidMesh m, LevelCollision col, float u0, float u1, float v0, float v1, float y0, float y1,
            in Surf s, bool solid)
        {
            if (u1 - u0 < 0.005f || v1 - v0 < 0.005f || y1 - y0 < 0.005f) return;
            Vector2 c = L.CoreSite((u0 + u1) * 0.5f, (v0 + v1) * 0.5f), up = L.DirToSite(L.Up);
            float yaw = Mathf.Atan2(up.x, up.y) * Mathf.Rad2Deg;
            var centre = new Vector3(c.x, (y0 + y1) * 0.5f, c.y);
            var size = new Vector3(v1 - v0, y1 - y0, u1 - u0);
            if (m != null) m.Box(centre, size, yaw, s);
            if (solid && col != null) col.Box(centre, size, Quaternion.Euler(0f, yaw, 0f));
        }

        // A box spanning x0..x1 and z0..z1 of the frame, from y0 to y1, solid.
        private static void FrameBox(LevelLayout L, RaidMesh m, LevelCollision col, float x0, float x1, float z0, float z1, float y0, float y1, in Surf s)
        {
            if (x1 - x0 < 0.005f || z1 - z0 < 0.005f || y1 - y0 < 0.005f) return;
            Vector2 c = L.ToSite(new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f));
            float yaw = Mathf.Atan2(L.Ez.x, L.Ez.y) * Mathf.Rad2Deg;
            var centre = new Vector3(c.x, (y0 + y1) * 0.5f, c.y);
            var size = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            m.Box(centre, size, yaw, s);
            col.Box(centre, size, Quaternion.Euler(0f, yaw, 0f));
        }

        private static Rect FrameBounds(LevelLayout L, List<Vector2> pts)
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (Vector2 p in pts)
            {
                Vector2 q = L.ToLocal(p);
                x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x);
                z0 = Mathf.Min(z0, q.y); z1 = Mathf.Max(z1, q.y);
            }
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }
        #endregion

        /// <summary>The levels' collider: their floors (both faces), ramps, landings and walls in one mesh, made into one
        /// <see cref="MeshCollider"/> under the site's world root (with a <see cref="LevelStructure"/> saying where the
        /// building's basement is, for the survivors' bodies).</summary>
        private sealed class LevelCollision
        {
            private readonly List<Vector3> _v = new List<Vector3>(256);
            private readonly List<int> _t = new List<int>(512);

            public void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = _v.Count;
                _v.Add(a);
                _v.Add(b);
                _v.Add(c);
                _t.Add(i); _t.Add(i + 1); _t.Add(i + 2);
                _t.Add(i); _t.Add(i + 2); _t.Add(i + 1);
            }

            // The box's six faces by their corners (corner k: x, y, z set by bits 0, 1, 2), each kept both ways round so a body
            // meets it from either side.
            private static readonly int[] s_BoxFaces = { 0, 2, 6, 4, 1, 3, 7, 5, 0, 1, 5, 4, 2, 3, 7, 6, 0, 1, 3, 2, 4, 5, 7, 6 };

            public void Box(Vector3 centre, Vector3 size, Quaternion rot)
            {
                int b = _v.Count;
                for (int k = 0; k < 8; k++)
                    _v.Add(centre + rot * new Vector3((k & 1) != 0 ? size.x * 0.5f : -size.x * 0.5f, (k & 2) != 0 ? size.y * 0.5f : -size.y * 0.5f,
                        (k & 4) != 0 ? size.z * 0.5f : -size.z * 0.5f));
                for (int f = 0; f < 24; f += 4)
                {
                    int p = b + s_BoxFaces[f], q = b + s_BoxFaces[f + 1], r = b + s_BoxFaces[f + 2], s = b + s_BoxFaces[f + 3];
                    _t.Add(p); _t.Add(q); _t.Add(r);
                    _t.Add(p); _t.Add(r); _t.Add(s);
                    _t.Add(p); _t.Add(r); _t.Add(q);
                    _t.Add(p); _t.Add(s); _t.Add(r);
                }
            }

            /// <summary>The collider's mesh (counted with the piece's meshes), or null when nothing went into it.</summary>
            public Mesh Bake(string name)
            {
                if (_t.Count == 0) return null;
                var mesh = new Mesh { name = name };
                if (_v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(_v);
                mesh.SetTriangles(_t, 0);
                mesh.RecalculateBounds();
                RaidMesh.CreatedMeshes?.Add(mesh);
                return mesh;
            }

            public void Emit(Site site, long id, LevelLayout L)
            {
                Mesh mesh = Bake("Levels " + id);
                if (mesh == null) return;
                var go = new GameObject("Levels " + id);
                go.transform.SetParent(site.Town.World, false);
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                if (L.Basement)
                {
                    var s = go.AddComponent<LevelStructure>();
                    s.Basement = Levels.Corners(L, L.Main).ToArray();
                }
            }
        }
    }
}
