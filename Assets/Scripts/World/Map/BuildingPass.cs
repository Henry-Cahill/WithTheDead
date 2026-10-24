using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's buildings, from their mapped footprints. What each one is and how it's built is worked out
    /// first (BuildingPass.Plan: a brick store downtown, a frame house with a porch, a trailer, a tobacco barn...; the
    /// data rarely says, so mostly from its outline and surroundings, BuildingPass.Context and BuildingPass.Shape), then
    /// it's raised: walls by storey with their doors and windows (BuildingPass.Openings), a real roof on the blocks of its
    /// outline with the snow lying on it (BuildingPass.Roofs), and what stands in front of it (BuildingPass.Frontage:
    /// porches, stoops, shop fronts, awnings, signs) or makes it what it is (BuildingPass.Special: steeples, silos, gas
    /// station canopies, ruins). Everything draws through the building's shell (<see cref="Site.Shell"/>): the ground
    /// storey's outside walls and what's on them by the way they face, everything above them in its upper part, so the
    /// isometric cutaway can lift a building's roof and front walls off around the survivor. The ground storey's walls
    /// have colliders (one object per building, in its own frame) and real door openings; each building with a door is
    /// listed in <see cref="Site.Enterables"/> with its doors and ground-floor windows exactly as built, for
    /// <see cref="InteriorPass"/> to furnish. Past the edge of a fixed map, and on a Lite site, buildings are drawn cheaply
    /// (walls, plain windows and the roof, no colliders or frontage); a fixed map builds the buildings its edge cuts
    /// through too: playable when their middle is inside, else as scenery with colliders.
    /// </summary>
    internal static partial class BuildingPass
    {
        private const float GroundStorey = 3.0f;   // the ground storey's walls (with colliders) stop here
        private const float CeilingY = 2.85f;
        private const float Thick = 0.22f;
        private const float DoorWidth = 1.1f, DoubleDoorWidth = 1.8f, DoorHeight = 2.15f;
        private const float StepBudget = 360f;      // build cost (see Job.Cost) between yields

        private const int SaltInterior = 0x1A7E, SaltDetail = 0xDE7A;

        internal static IEnumerable<string> Build(Site site)
        {
            var ctx = new Context(site);
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            var near = new List<Footprint>(16);
            float cost = 0f;
            foreach (Footprint f in site.Area.Buildings)
            {
                if (f.Pts.Count < 3 || f.Size < 6f) continue;
                bool playable = f.Inside(half, 1f) || (Mathf.Abs(f.Middle.x) < half && Mathf.Abs(f.Middle.y) < half);
                Shape sh = ShapeOf(f);
                ctx.Near(f, 1.2f, near);
                Plan plan = Classify(ctx, f, sh, near);
                var job = new Job(site, f, sh, plan)
                {
                    Lite = !playable || site.Detail == Detail.Lite,
                    Scenery = !playable,
                    Solid = playable ? site.Detail == Detail.Full : f.Bounds.Overlaps(square),
                };
                Make(job);
                if (playable) site.Buildings++;
                else site.SceneryBuildings++;
                cost += job.Cost;
                if (cost < StepBudget) continue;
                cost = 0f;
                yield return "buildings";
            }
            yield return "buildings";
        }

        #region The job
        /// <summary>One building being raised.</summary>
        private sealed class Job
        {
            public readonly Site Site;
            public readonly Footprint F;
            public readonly Shape Sh;
            public readonly Plan P;
            public readonly System.Random Rng;
            public bool Lite, Scenery, Solid;
            public BuildingShell Shell;
            public EnterableBuilding Record;
            public float Top, Cost;
            public readonly int N;
            public readonly Vector2[] A, Dir, Out;
            public readonly float[] Len;
            /// <summary>Per wall: its top, by stretch (a lower wing's walls stop lower).</summary>
            public readonly List<Seg>[] Tops;
            /// <summary>The roof blocks of a pitched roof; null for a flat one.</summary>
            public List<RoofPart> Parts;
            public readonly List<Hole> Holes = new List<Hole>();
            /// <summary>The front door's wall and where along it (or -1).</summary>
            public int DoorEdge = -1;
            public float DoorAt;
            public GameObject Colliders;
            public Surf WallS, TrimS, AccentS, GlassS;

            public Job(Site site, Footprint f, Shape sh, Plan p)
            {
                Site = site;
                F = f;
                Sh = sh;
                P = p;
                Rng = MapStyle.Rng(f.Data.id, SaltDetail);
                List<Vector2> pts = f.Pts;
                N = pts.Count;
                A = new Vector2[N];
                Dir = new Vector2[N];
                Out = new Vector2[N];
                Len = new float[N];
                Tops = new List<Seg>[N];
                for (int i = 0; i < N; i++)
                {
                    Vector2 a = pts[i], c = pts[(i + 1) % N];
                    float len = Vector2.Distance(a, c);
                    A[i] = a;
                    Len[i] = len;
                    Vector2 d = len > 1e-4f ? (c - a) / len : Vector2.right;
                    Dir[i] = d;
                    Out[i] = new Vector2(d.y, -d.x);   // an anticlockwise ring: outside is on the right
                    Tops[i] = new List<Seg>(2);
                }
                WallS = WallSurf(p.Wall, p.WallTone);
                TrimS = DecorPalette.Matte(p.Trim);
                AccentS = DecorPalette.Matte(p.Accent);
                GlassS = DecorPalette.Gloss(new Color(0.11f, 0.13f, 0.16f));
                Cost = 4f + f.Size * 0.012f + N * 1.5f;
            }

            public RaidMesh Facing(int edge) => Shell.WallsFacing(Out[edge]);
            public RaidMesh Upper => Shell.Upper;
            public float Yaw(int edge) => -Mathf.Atan2(Dir[edge].y, Dir[edge].x) * Mathf.Rad2Deg;
            public Vector2 At(int edge, float s) => A[edge] + Dir[edge] * s;
            /// <summary>Height of storey <paramref name="k"/>'s floor.</summary>
            public float FloorY(int k) => k <= 0 ? 0f : P.FirstStorey + (k - 1) * P.Storey;
            public bool Party(int edge) => P.Party != null && P.Party[edge];

            /// <summary>The top of wall <paramref name="edge"/> at <paramref name="s"/> along it.</summary>
            public float TopAt(int edge, float s)
            {
                List<Seg> segs = Tops[edge];
                for (int i = 0; i < segs.Count; i++)
                    if (s <= segs[i].S1 + 1e-3f) return segs[i].Top;
                return segs.Count > 0 ? segs[segs.Count - 1].Top : P.WallHeight;
            }
        }

        private struct Seg { public float S0, S1, Top; }

        /// <summary>A door or window: centre <see cref="S"/> along its wall, sill and head above its storey's floor.</summary>
        private struct Hole
        {
            public int Edge, Storey;
            public float S, W, Sill, Head;
            public WindowKind Win;
            public DoorKind Door;
            /// <summary>A closed door drawn on the wall (garage, roll-up, barn, motel room): no gap.</summary>
            public bool Closed;
            public bool Lit;
            public bool IsDoor => Win == WindowKind.None;
            public bool Gap => Win == WindowKind.None && !Closed;
        }

        private static void Make(Job j)
        {
            switch (j.P.Arch)
            {
                case Arch.Silo: SiloBuilding(j); break;
                case Arch.Canopy: CanopyBuilding(j); break;
                case Arch.Ruin: RuinBuilding(j); break;
                default: Ordinary(j); break;
            }
            if (j.Shell != null) j.Shell.Top = Mathf.Max(j.Shell.Top, j.Top);
        }

        private static void Ordinary(Job j)
        {
            Plan p = j.P;
            j.Shell = j.Site.Shell(j.F, j.Scenery);
            PlanRoof(j);
            j.Shell.WallTop = Mathf.Min(GroundStorey, p.WallHeight);
            j.Top = p.WallHeight;
            PlanOpenings(j);
            Walls(j);
            Openings(j);
            Roof(j);
            if (!j.Lite)
            {
                Frontage(j);
                Special(j);
            }
            if (!j.Scenery) Inside(j);
            j.Cost += j.Lite ? 2f : 10f + p.Storeys * j.N * 1.2f + (j.Parts != null ? j.Parts.Count * 6f : 4f);
        }
        #endregion

        #region Walls
        private static readonly List<Vector3> s_Gaps = new List<Vector3>(8);

        // The walls, wall by wall and stretch by stretch: the ground storey's (with colliders) cut round its door
        // openings, everything above in the upper part; then the foundation, the corner boards and the belt courses.
        private static void Walls(Job j)
        {
            Plan p = j.P;
            for (int i = 0; i < j.N; i++)
            {
                float len = j.Len[i];
                if (len < 0.05f) continue;
                Vector2 a = j.A[i], d = j.Dir[i], o = j.Out[i];
                float yaw = j.Yaw(i);
                RaidMesh facing = j.Facing(i);
                Vector2 inset = -o * (Thick * 0.5f);   // the wall stands just inside the mapped outline
                s_Gaps.Clear();
                foreach (Hole h in j.Holes)
                    if (h.Edge == i && h.Storey == 0 && h.Gap) s_Gaps.Add(new Vector3(h.S - h.W * 0.5f, h.S + h.W * 0.5f, h.Head));
                s_Gaps.Sort((x, y) => x.x.CompareTo(y.x));
                foreach (Seg seg in j.Tops[i])
                {
                    float s0 = seg.S0 <= 1e-3f ? -Thick * 0.5f : seg.S0, s1 = seg.S1 >= len - 1e-3f ? len + Thick * 0.5f : seg.S1;
                    float ground = Mathf.Min(seg.Top, GroundStorey);
                    float cur = s0;
                    foreach (Vector3 g in s_Gaps)
                    {
                        if (g.y <= s0 || g.x >= s1) continue;
                        WallBox(j, facing, a, d, inset, cur, g.x, 0f, ground, yaw, j.WallS, true);
                        WallBox(j, facing, a, d, inset, Mathf.Max(g.x, s0), Mathf.Min(g.y, s1), g.z, ground, yaw, j.WallS, true);
                        cur = g.y;
                    }
                    WallBox(j, facing, a, d, inset, cur, s1, 0f, ground, yaw, j.WallS, true);
                    if (seg.Top > GroundStorey + 0.01f)
                        WallBox(j, j.Upper, a, d, inset, s0, s1, GroundStorey, seg.Top, yaw, j.WallS, false);
                }
                if (j.Lite) continue;
                Foundation(j, i, facing, yaw);
                if (p.Belts && !j.Party(i)) Belts(j, i, yaw);
            }
            if (!j.Lite && p.CornerBoards) CornerBoards(j);
            StepWalls(j);
        }

        // One run of wall into <paramref name="into"/>, from s0 to s1 along it and y0 to y1 up, with a collider when
        // it's solid (and the job's walls are).
        private static void WallBox(Job j, RaidMesh into, Vector2 a, Vector2 d, Vector2 inset, float s0, float s1, float y0, float y1,
            float yaw, in Surf surf, bool solid)
        {
            float len = s1 - s0, h = y1 - y0;
            if (len < 0.02f || h < 0.02f) return;
            Vector2 c = a + d * ((s0 + s1) * 0.5f) + inset;
            var center = new Vector3(c.x, (y0 + y1) * 0.5f, c.y);
            var size = new Vector3(len, h, Thick);
            into.Box(center, size, yaw, surf);
            if (solid && j.Solid) Collider(j, center, size, yaw);
        }

        // A concrete, block, brick or stone band along the foot of the wall, standing a little proud, broken at doors.
        private static void Foundation(Job j, int i, RaidMesh facing, float yaw)
        {
            float h = j.P.Foundation;
            if (h < 0.05f || !j.P.FoundationSurf.Valid) return;
            float len = j.Len[i];
            float cur = -0.02f;
            foreach (Vector3 g in s_Gaps)
            {
                Band(j, facing, i, cur, g.x, 0f, h, 0.05f, yaw, j.P.FoundationSurf);
                cur = g.y;
            }
            Band(j, facing, i, cur, len + 0.02f, 0f, h, 0.05f, yaw, j.P.FoundationSurf);
        }

        // A strip along wall i from s0 to s1, y0 to y1, standing <paramref name="proud"/> off its face.
        private static void Band(Job j, RaidMesh m, int i, float s0, float s1, float y0, float y1, float proud, float yaw, in Surf surf)
        {
            if (s1 - s0 < 0.02f || y1 - y0 < 0.01f) return;
            Vector2 c = j.At(i, (s0 + s1) * 0.5f) + j.Out[i] * (proud * 0.5f - 0.04f);
            m.Box(new Vector3(c.x, (y0 + y1) * 0.5f, c.y), new Vector3(s1 - s0, y1 - y0, proud + 0.08f), yaw, surf);
        }

        // Courses of stone or brick standing out between storeys.
        private static void Belts(Job j, int i, float yaw)
        {
            Plan p = j.P;
            float len = j.Len[i];
            Surf stone = j.P.Wall == WallKind.Brick ? WallSurf(WallKind.Stone, 0) : j.TrimS;
            for (int k = 1; k < p.Storeys; k++)
            {
                float y = j.FloorY(k);
                if (y + 0.2f > j.TopAt(i, len * 0.5f)) break;
                Band(j, j.Upper, i, -0.06f, len + 0.06f, y - 0.06f, y + 0.12f, 0.05f, yaw, stone);
            }
        }

        // White boards up the outside corners of a frame house: the ground storey's part with the walls facing that way,
        // the rest with the upper storeys.
        private static void CornerBoards(Job j)
        {
            for (int i = 0; i < j.N; i++)
            {
                int prev = (i + j.N - 1) % j.N;
                if (j.Len[i] < 0.5f || j.Len[prev] < 0.5f) continue;
                // Convex corner: the wall turns left (anticlockwise) here.
                if (j.Dir[prev].x * j.Dir[i].y - j.Dir[prev].y * j.Dir[i].x <= 0.05f) continue;
                float top = Mathf.Min(j.TopAt(i, 0f), j.TopAt(prev, j.Len[prev]));
                float y0 = Mathf.Max(0.02f, j.P.Foundation);
                Vector2 c = j.A[i] + (j.Out[i] + j.Out[prev]) * 0.02f;
                float yaw = j.Yaw(i);
                float g = Mathf.Min(top, GroundStorey);
                if (g > y0) j.Facing(i).Box(new Vector3(c.x, (y0 + g) * 0.5f, c.y), new Vector3(0.14f, g - y0, 0.14f), yaw, j.TrimS);
                if (top > GroundStorey + 0.05f)
                    j.Upper.Box(new Vector3(c.x, (GroundStorey + top) * 0.5f, c.y), new Vector3(0.14f, top - GroundStorey, 0.14f), yaw, j.TrimS);
            }
        }
        #endregion

        #region Colliders
        // A collider for a wall or anything solid: on the building's own object, turned to its frame, when the box runs
        // along the frame (most do: one object per building, not one per wall); else on the town's.
        private static void Collider(Job j, Vector3 center, Vector3 size, float yaw)
        {
            float frame = j.Sh.Yaw;
            float off = Mathf.Repeat(yaw - frame, 90f);
            if (off > 0.5f && off < 89.5f)
            {
                j.Site.Town.PropCollider(center, size, yaw);
                return;
            }
            if (j.Colliders == null)
            {
                j.Colliders = new GameObject("BuildingColliders");
                j.Colliders.transform.SetParent(j.Site.Town.World, false);
                j.Colliders.transform.SetPositionAndRotation(To3(j.Sh.O, 0f), Quaternion.Euler(0f, frame, 0f));
            }
            Vector2 local = j.Sh.Local(new Vector2(center.x, center.z));
            bool quarter = Mathf.Abs(Mathf.DeltaAngle(yaw, frame + 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(yaw, frame - 90f)) < 1f;
            var box = j.Colliders.AddComponent<BoxCollider>();
            box.center = new Vector3(local.x, center.y, local.y);
            box.size = quarter ? new Vector3(size.z, size.y, size.x) : size;
            j.Site.Town.ColliderCount++;
        }
        #endregion

        #region Inside
        // The floor, the ceiling (shown only from inside views), shelter from the weather, the ground the building takes
        // from the props, and its listing for the interiors.
        private static void Inside(Job j)
        {
            Plan p = j.P;
            Footprint f = j.F;
            bool roofed = p.Arch != Arch.Ruin && p.Arch != Arch.Canopy;
            if (!j.Lite)
            {
                List<int> tris = MapGeometry.Triangulate(f.Pts);
                Vector3 middle = To3(f.Middle, 0f);
                RaidMesh floor = j.Site.Town.Out.Flat(middle);
                RaidMesh ceilings = j.Site.Overhead.At(middle);
                Surf floorSurf = FloorOf(p);
                float ceiling = p.Storeys == 1 && p.Eave > 4.5f && p.Arch != Arch.Row ? p.Eave - 0.15f : CeilingY;
                for (int t = 0; t + 2 < tris.Count; t += 3)
                {
                    Vector2 a = f.Pts[tris[t]], b = f.Pts[tris[t + 1]], c = f.Pts[tris[t + 2]];
                    floor.Tri(To3(a, 0.02f), To3(b, 0.02f), To3(c, 0.02f), floorSurf);
                    if (roofed) ceilings.Tri(To3(a, ceiling), To3(c, ceiling), To3(b, ceiling), DecorMaterials.Ceiling);
                }
            }
            Rows(j, roofed && !j.Lite);
            if (j.Record != null) j.Site.Enterables.Add(j.Record);
        }

        // The footprint in strips 2 m deep: each strip's stretches inside it are taken from the props and, under a roof,
        // registered as shelter (a rotated building's bounding box would shelter the snow round its corners).
        private static readonly List<float> s_Xs = new List<float>(16);

        private static void Rows(Job j, bool shelter)
        {
            Footprint f = j.F;
            List<Vector2> p = f.Pts;
            float shelterTop = Mathf.Min(j.P.FirstStorey, j.P.WallHeight);
            for (float z0 = f.Bounds.yMin; z0 < f.Bounds.yMax; z0 += 2f)
            {
                float z1 = Mathf.Min(z0 + 2f, f.Bounds.yMax), zc = (z0 + z1) * 0.5f;
                s_Xs.Clear();
                for (int i = 0, k = p.Count - 1; i < p.Count; k = i++)
                {
                    Vector2 a = p[k], b = p[i];
                    if ((a.y > zc) == (b.y > zc)) continue;
                    s_Xs.Add(a.x + (zc - a.y) * (b.x - a.x) / (b.y - a.y));
                }
                s_Xs.Sort();
                for (int i = 0; i + 1 < s_Xs.Count; i += 2)
                {
                    float x0 = s_Xs[i], x1 = s_Xs[i + 1];
                    if (x1 - x0 < 0.3f) continue;
                    j.Site.Take(Rect.MinMaxRect(x0, z0, x1, z1));
                    if (!shelter || x1 - x0 < 1f) continue;
                    Shelter.Register(j.Site.ToWorld(new Bounds(new Vector3((x0 + x1) * 0.5f, shelterTop * 0.5f, zc),
                        new Vector3(x1 - x0 - 0.4f, shelterTop, Mathf.Max(0.5f, z1 - z0 - 0.1f)))));
                }
            }
        }

        private static Surf FloorOf(Plan p)
        {
            switch (p.Arch)
            {
                case Arch.House: case Arch.Trailer: case Arch.Apartments: case Arch.Motel:
                    return DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f));
                case Arch.Warehouse: case Arch.Industrial: case Arch.Rickhouse: case Arch.Garage: case Arch.Shed: case Arch.Barn:
                case Arch.TobaccoBarn: case Arch.Greenhouse: case Arch.Ruin:
                    return DecorMaterials.Concrete;
                case Arch.Church: case Arch.Courthouse:
                    return DecorMaterials.WoodFloor(new Color(0.46f, 0.32f, 0.20f));
                default:
                    return DecorMaterials.Linoleum(new Color(0.78f, 0.76f, 0.70f), new Color(0.60f, 0.58f, 0.54f), p.Arch == Arch.Store);
            }
        }
        #endregion

        #region Drawing helpers
        // A flat convex polygon facing <paramref name="normal"/>'s way.
        private static void Poly(RaidMesh m, List<Vector3> pts, Vector3 normal, in Surf s)
        {
            if (pts.Count < 3) return;
            Vector3 n = Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]);
            for (int i = 3; i < pts.Count && n.sqrMagnitude < 1e-8f; i++) n = Vector3.Cross(pts[1] - pts[0], pts[i] - pts[0]);
            bool flip = Vector3.Dot(n, normal) < 0f;
            for (int i = 1; i + 1 < pts.Count; i++)
            {
                if (flip) m.Tri(pts[0], pts[i + 1], pts[i], s);
                else m.Tri(pts[0], pts[i], pts[i + 1], s);
            }
        }

        // A quad from its four corners (in order round it), facing <paramref name="normal"/>'s way.
        private static void Quad4(RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, in Surf s)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, normal) >= 0f)
            {
                m.Tri(a, b, c, s);
                m.Tri(a, c, d, s);
            }
            else
            {
                m.Tri(a, c, b, s);
                m.Tri(a, d, c, s);
            }
        }

        // A thin board from a to b (a barge board, a hip cap, a brace), its broad face turned toward
        // <paramref name="up"/>.
        private static void Beam(RaidMesh m, Vector3 a, Vector3 b, float width, float depth, Vector3 up, in Surf s)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            Quaternion q = Quaternion.LookRotation(d / len, up);
            m.Box((a + b) * 0.5f, new Vector3(width, depth, len), q, s);
        }

        // An upright quad on wall <paramref name="edge"/>'s face, centred s along it at height y, standing out by
        // <paramref name="proud"/>.
        private static void OnWall(Job j, RaidMesh m, int edge, float s, float y, float w, float h, float proud, in Surf surf)
        {
            Vector2 c = j.At(edge, s) + j.Out[edge] * proud;
            m.Upright(new Vector3(c.x, y, c.y), To3(j.Out[edge], 0f), w, h, surf);
        }

        // A box against wall <paramref name="edge"/>'s face: w along it, h up, depth out from the face.
        private static void OnWallBox(Job j, RaidMesh m, int edge, float s, float y, float w, float h, float depth, in Surf surf,
            float proud = 0f)
        {
            Vector2 c = j.At(edge, s) + j.Out[edge] * (proud + depth * 0.5f);
            m.Box(new Vector3(c.x, y, c.y), new Vector3(w, h, depth), j.Yaw(edge), surf);
        }
        #endregion
    }
}
