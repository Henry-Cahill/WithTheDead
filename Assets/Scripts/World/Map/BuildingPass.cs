using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's buildings, from their mapped footprints. What each one is and how it's built is worked out
    /// first (BuildingPass.Plan: a brick store downtown, a frame house with a porch, a trailer, a tobacco barn...; the
    /// data rarely says, so mostly from its outline and surroundings, BuildingPass.Context and BuildingPass.Shape), then
    /// it's raised: walls by storey with their doors and windows (BuildingPass.Openings, and BuildingPass.Facades for the
    /// upper floors of tall buildings and for cheap ones), a real roof on the blocks of its outline with the snow lying on
    /// it, or a flat one with what stands on it (BuildingPass.Roofs), and what stands in front of it (BuildingPass.Frontage:
    /// porches, stoops, shop fronts, awnings, signs, balconies, loading docks) or makes it what it is (BuildingPass.Special:
    /// steeples, silos, gas station canopies, parking decks, ruins). Everything draws through the building's shell
    /// (<see cref="Site.Shell"/>): the ground storey's outside walls and what's on them by the way they face, everything
    /// above them in its upper part, so the isometric cutaway can lift a building's roof and front walls off around the
    /// survivor. The ground storey's walls have colliders and real door openings; each building with a door is listed in
    /// <see cref="Site.Enterables"/> with its doors and ground-floor windows exactly as built, for <see cref="InteriorPass"/>
    /// to furnish. Past the edge of a fixed map, and on a Lite site, buildings are drawn cheaply (walls, windows painted on,
    /// the roof and its silhouette, no colliders or frontage); a fixed map builds the buildings its edge cuts through too:
    /// playable when their middle is inside, else solid and whole but shut.
    /// </summary>
    /// <remarks>
    /// A streamed piece builds a step at a time, so the pass yields whenever its time slice is used up
    /// (<see cref="Site.StepDue"/>), between buildings and between the stages of a big one; one job's buffers are reused for
    /// every building of a site, and the colliders join the site's merged ones (<see cref="RaidTown.PropCollider"/>).
    /// </remarks>
    internal static partial class BuildingPass
    {
        private const float GroundStorey = 3.0f;   // the ground storey's walls (with colliders) stop here
        private const float CeilingY = 2.85f;
        private const float Thick = 0.22f;
        private const float DoorWidth = 1.1f, DoubleDoorWidth = 1.8f, DoorHeight = 2.15f;
        /// <summary>A loading dock's deck above the yard.</summary>
        private const float DockHeight = 1.2f;

        private const int SaltInterior = 0x1A7E, SaltDetail = 0xDE7A;

        /// <summary>Generates building geometry for the given site, yielding progress tokens for each stage.</summary>
        internal static IEnumerable<string> Build(Site site)
        {
            site.BeginStep();
            ProbeStart();
            PrePaint();
            var ctx = new Context(site);
            ProbeEnd(-2, null);
            while (true)
            {
                ProbeStart();
                bool warming = WarmNext();
                ProbeEnd(-3, null);
                if (!warming) break;
                if (site.StepDue)
                {
                    yield return "buildings";
                    site.BeginStep();
                }
            }
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            var near = new List<Footprint>(16);
            var job = new Job(site);
            List<Footprint> all = site.Area.Buildings;
            for (int b = 0; b < all.Count; b++)
            {
                Footprint f = all[b];
                if (f.Pts.Count < 3 || f.Size < 6f) continue;
                bool playable = f.Inside(half, 1f) || (Mathf.Abs(f.Middle.x) < half && Mathf.Abs(f.Middle.y) < half);
                // A building the fixed square's edge cuts through, its middle outside: solid and whole, but shut.
                bool edge = !playable && !site.Chunked && f.Bounds.Overlaps(square);
                ProbeStart();
                Shape sh = ShapeOf(f);
                ctx.Near(f, 1.2f, near);
                Plan plan = Classify(ctx, f, sh, near);
                if (edge) plan.Enterable = false;
                job.Begin(f, sh, plan, site.Detail == Detail.Lite || (!playable && !edge), !playable, edge,
                    (playable || edge) && site.Detail == Detail.Full);
                ProbeEnd(-1, job);
                while (true)
                {
                    ProbeStart();
                    int stage = job.Stage;
                    bool more = Step(job);
                    ProbeEnd(stage, job);
                    if (!more) break;
                    if (site.StepDue)
                    {
                        yield return "buildings";
                        site.BeginStep();
                    }
                }
#if UNITY_EDITOR
                if (Inspect != null) Note(job);
#endif
                if (playable) site.Buildings++;
                else site.SceneryBuildings++;
                if (!site.StepDue) continue;
                yield return "buildings";
                site.BeginStep();
            }
            yield return "buildings";
        }

        #region The job
        /// <summary>The building being raised; one per site, its buffers reused building after building.</summary>
        private sealed class Job
        {
            public readonly Site Site;
            public Footprint F;
            public Shape Sh;
            public Plan P;
            /// <summary>Drawn cheaply: a Lite site's, or scenery's past the edge.</summary>
            public bool Lite;
            /// <summary>Past the playable square: never listed for the interiors.</summary>
            public bool Scenery;
            /// <summary>Scenery the fixed square's edge cuts through: its own shell, in the world, solid.</summary>
            public bool Edge;
            /// <summary>Its walls and what stands round it get colliders.</summary>
            public bool Solid;
            public BuildingShell Shell;
            public EnterableBuilding Record;
            public float Top;
            /// <summary>The next stage to build (see <see cref="Step"/>), and how far into it (the openings drawn).</summary>
            public int Stage, Cursor;
            public int N;
            public Vector2[] A = new Vector2[32], Dir = new Vector2[32], Out = new Vector2[32];
            public float[] Len = new float[32];
            /// <summary>How far each wall runs on past its start and end, to close the corner with its neighbour.</summary>
            public float[] Run0 = new float[32], Run1 = new float[32];
            /// <summary>Per wall: windows painted on its ground storey, and on its upper storeys (BuildingPass.Facades).</summary>
            public bool[] GridLow = new bool[32], GridHigh = new bool[32];
            /// <summary>Per wall: its top, by stretch (a lower wing's walls stop lower).</summary>
            public List<Seg>[] Tops = new List<Seg>[0];
            /// <summary>The roof blocks of a pitched roof; null for a flat one.</summary>
            public List<RoofPart> Parts;
            public readonly List<Hole> Holes = new List<Hole>(48);
            /// <summary>The front door's wall and where along it (or -1); the top of the step, porch or ground before it.</summary>
            public int DoorEdge = -1;
            public float DoorAt, DoorStep;
            /// <summary>The wall the big doors are in (or -1); how many shops a strip's front holds (0 for one).</summary>
            public int BigEdge = -1, Units;
            /// <summary>Porch lamps given a real light on this site so far (see MaxPorchLights).</summary>
            public int PorchLights;
            public Surf WallS, TrimS, AccentS, GlassS;
            private List<int> _tris;

            public Job(Site site) { Site = site; }

            /// <summary>Initializes the job with building data and configuration.</summary>
            public void Begin(Footprint f, Shape sh, Plan p, bool lite, bool scenery, bool edge, bool solid)
            {
                F = f;
                Sh = sh;
                P = p;
                Lite = lite;
                Scenery = scenery;
                Edge = edge;
                Solid = solid;
                Shell = null;
                Record = null;
                Top = 0f;
                Stage = 0;
                Cursor = 0;
                Parts = null;
                Holes.Clear();
                DoorEdge = -1;
                DoorAt = 0f;
                DoorStep = 0f;
                BigEdge = -1;
                Units = 0;
                _tris = null;
                List<Vector2> pts = f.Pts;
                N = pts.Count;
                if (A.Length < N)
                {
                    int size = Mathf.NextPowerOfTwo(N);
                    A = new Vector2[size];
                    Dir = new Vector2[size];
                    Out = new Vector2[size];
                    Len = new float[size];
                    Run0 = new float[size];
                    Run1 = new float[size];
                    GridLow = new bool[size];
                    GridHigh = new bool[size];
                }
                if (Tops.Length < N)
                {
                    var tops = new List<Seg>[Mathf.NextPowerOfTwo(N)];
                    for (int i = 0; i < tops.Length; i++) tops[i] = i < Tops.Length ? Tops[i] : new List<Seg>(2);
                    Tops = tops;
                }
                for (int i = 0; i < N; i++)
                {
                    Vector2 a = pts[i], c = pts[(i + 1) % N];
                    float len = Vector2.Distance(a, c);
                    A[i] = a;
                    Len[i] = len;
                    Vector2 d = len > 1e-4f ? (c - a) / len : Vector2.right;
                    Dir[i] = d;
                    Out[i] = new Vector2(d.y, -d.x);   // an anticlockwise ring: outside is on the right
                    Tops[i].Clear();
                    GridLow[i] = GridHigh[i] = false;
                }
                // Walls stand just inside the outline: at an outside corner they meet as they are; at an inside one each
                // runs on into the other by the thickness its turn needs.
                for (int i = 0; i < N; i++)
                {
                    int prev = (i + N - 1) % N;
                    float turn = Dir[prev].x * Dir[i].y - Dir[prev].y * Dir[i].x, dot = Vector2.Dot(Dir[prev], Dir[i]);
                    float run = turn >= -1e-3f ? 0f : Mathf.Min(Thick * 2f, Thick * Mathf.Tan(0.5f * Mathf.Atan2(-turn, dot)));
                    Run0[i] = run;
                    Run1[prev] = run;
                }
                WallS = WallSurf(p.Wall, p.WallTone);
                TrimS = DecorPalette.Matte(p.Trim);
                AccentS = DecorPalette.Matte(p.Accent);
                GlassS = DecorPalette.Gloss(new Color(0.11f, 0.13f, 0.16f));
            }

            /// <summary>Gets the mesh that faces outward from the given wall edge.</summary>
            public RaidMesh Facing(int edge) => Shell.WallsFacing(Out[edge]);
            /// <summary>Gets the upper (roof) mesh for the building.</summary>
            public RaidMesh Upper => Shell.Upper;
            /// <summary>Unique identifier for this building.</summary>
            public long Id => F.Data.id;
            /// <summary>Calculates the yaw (rotation) of the wall edge in degrees.</summary>
            public float Yaw(int edge) => -Mathf.Atan2(Dir[edge].y, Dir[edge].x) * Mathf.Rad2Deg;
            /// <summary>Computes a point along the wall edge at a given distance.</summary>
            public Vector2 At(int edge, float s) => A[edge] + Dir[edge] * s;
            /// <summary>Height of storey <paramref name="k"/>'s floor.</summary>
            public float FloorY(int k) => k <= 0 ? 0f : P.FirstStorey + (k - 1) * P.Storey;
            /// <summary>Indicates whether a wall edge has a party (special) feature.</summary>
            public bool Party(int edge) => P.Party != null && edge < P.Party.Length && P.Party[edge];

            /// <summary>The footprint in triangles (index triples into <see cref="TriPts"/>), worked out once.</summary>
            public List<int> Tris
            {
                get
                {
                    if (_tris != null) return _tris;
                    if (F.Pts.Count <= MaxTriangulated) _tris = MapGeometry.Triangulate(F.Pts);
                    else
                    {
                        // A very detailed outline: a simplified copy, a little coarser each time until it's small enough.
                        for (float tol = 0.15f; ; tol *= 2f)
                        {
                            Simplify(F.Pts, tol, _simple);
                            if (_simple.Count <= MaxTriangulated || tol > 3f) break;
                        }
                        _tris = MapGeometry.Triangulate(_simple);
                    }
                    return _tris;
                }
            }

            /// <summary>The points <see cref="Tris"/> indexes: the footprint's own, or its simplified copy.</summary>
            public List<Vector2> TriPts
            {
                get
                {
                    List<int> _ = Tris;
                    return F.Pts.Count <= MaxTriangulated ? F.Pts : _simple;
                }
            }

            private readonly List<Vector2> _simple = new List<Vector2>(MaxTriangulated);

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
            /// <summary>A closed door drawn on the wall (garage, roll-up, barn, motel room, a shut building's): no gap.</summary>
            public bool Closed;
            /// <summary>A garage, roll-up or barn door (always closed).</summary>
            public bool Big;
            public bool Lit;
            /// <summary>Indicates whether the opening is a door (vs a window).</summary>
            public bool IsDoor => Win == WindowKind.None;
            /// <summary>Indicates whether the opening is a gap (a door without a closed flag).</summary>
            public bool Gap => Win == WindowKind.None && !Closed;
            /// <summary>A working window: a real opening through the wall, its fixed sash drawn in the wall and its working parts
            /// a RaidWindow the interiors fit (see MarkWorking).</summary>
            public bool Working;
            /// <summary>A fixed window: a real opening through the wall too, clear glass fixed in it (see MarkClear).</summary>
            public bool Clear;
            /// <summary>Never a working window: a tall building's upper floors' (an office block's don't open).</summary>
            public bool Fixed;
        }

        // The next stage of the job's building; false once it's done. A big building is raised in a few stages (its
        // walls, their openings, its roof, its frontage, its insides) so a streamed piece can stop between them.
        private static bool Step(Job j)
        {
            Plan p = j.P;
            switch (p.Arch)
            {
                case Arch.Silo: SiloBuilding(j); return Done(j);
                case Arch.Canopy: CanopyBuilding(j); return Done(j);
                case Arch.Ruin: RuinBuilding(j); return Done(j);
                case Arch.Parking: if (j.Stage == 0) { ParkingDeck(j); return Done(j); } break;
            }
            switch (j.Stage++)
            {
                case 0:
                    j.Shell = j.Site.Shell(j.F, j.Scenery && !j.Edge);
                    PlanRoof(j);
                    j.Shell.WallTop = Mathf.Min(GroundStorey, p.WallHeight);
                    j.Top = p.WallHeight;
                    PlanOpenings(j);
                    MarkWorking(j);
                    MarkClear(j);
                    Walls(j);
                    return true;
                case 1:
                    if (!Openings(j)) j.Stage = 1;   // more of them: this stage again
                    return true;
                case 2:
                    Roof(j);
                    return true;
                case 3:
                    if (!j.Lite) Frontage(j);
                    return true;
                case 4:
                    if (!j.Lite) Special(j);
                    else LiteSpecial(j);
                    return true;
                default:
                    if (!j.Scenery) Inside(j);
                    // A building the square's edge cuts through keeps the props off the ground it stands on.
                    else if (j.Edge) Rows(j, false);
                    return Done(j);
            }
        }

        private static bool Done(Job j)
        {
            if (j.Shell != null) j.Shell.Top = Mathf.Max(j.Shell.Top, j.Top);
            j.Stage = int.MaxValue;
            return false;
        }

        // In the editor, with -clonez-buildings-profile on the command line: every call of the pass slower than 2 ms is
        // logged with what it was building (stage -2 is the pass's setup, -1 a building's plan).
#if UNITY_EDITOR
        private static readonly bool s_Profiling =
            System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-clonez-buildings-profile") >= 0;
        private static long s_ProbeAt;
        private static int s_ProbeGCs;
#endif

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void ProbeStart()
        {
#if UNITY_EDITOR
            if (!s_Profiling) return;
            s_ProbeGCs = System.GC.CollectionCount(0);
            s_ProbeAt = System.Diagnostics.Stopwatch.GetTimestamp();
            s_MarkAt = s_ProbeAt;
#endif
        }

#if UNITY_EDITOR
        private static long s_MarkAt;
#endif

        // Within a stage: logs the time since the last mark (or the stage's start) when it's over a millisecond.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void Mark(string what, string detail = null)
        {
#if UNITY_EDITOR
            if (!s_Profiling) return;
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double ms = (now - s_MarkAt) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            s_MarkAt = now;
            if (ms >= 1.0) Debug.Log($"[Buildings]   {ms:F1} ms to {what}{(detail != null ? " \"" + detail + "\"" : "")}");
#endif
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void ProbeEnd(int stage, Job j)
        {
#if UNITY_EDITOR
            if (!s_Profiling) return;
            double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - s_ProbeAt) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            if (ms < 2.0) return;
            string what = stage == -3 ? "warm-up" : stage == -2 ? "setup" : stage == -1 ? "plan" : stage == 0 ? "walls" : stage == 1 ? "openings"
                : stage == 2 ? "roof" : stage == 3 ? "frontage" : stage == 4 ? "special" : "inside";
            int gcs = System.GC.CollectionCount(0) - s_ProbeGCs;
            if (gcs > 0) what += $" ({gcs} GC)";
            if (j == null)
            {
                Debug.Log($"[Buildings] {ms:F1} ms {what}");
                return;
            }
            Plan p = j.P;
            Debug.Log($"[Buildings] {ms:F1} ms {what}: way {j.F.Data.id} {p.Arch} {p.Roof} {j.F.Size:F0} m² {j.F.Pts.Count} corners"
                + $" ({j.N} drawn) {p.Storeys} storeys, {j.Holes.Count} openings, lite {j.Lite}, scenery {j.Scenery}, at {j.F.Middle}");
#endif
        }
        #endregion

        #region Walls
        private static readonly List<Vector3> s_Gaps = new List<Vector3>(8);
        private static readonly System.Comparison<Vector3> s_ByStart = (x, y) => x.x.CompareTo(y.x);
        /// <summary>The openings through the wall being drawn: (start, end, bottom, top) along and up it.</summary>
        private static readonly List<Vector4> s_Holes = new List<Vector4>(16);
        private static readonly List<float> s_Cuts = new List<float>(32);
        private static readonly List<Vector2> s_Ys = new List<Vector2>(8);
        private static readonly System.Comparison<Vector2> s_ByBottom = (x, y) => x.x.CompareTo(y.x);

        // The walls, wall by wall and stretch by stretch: the ground storey's (with colliders) cut round its door
        // openings, everything above in the upper part; then the foundation, the corner boards and the belt courses. The
        // working and fixed windows are openings too, on whatever storey they light.
        private static void Walls(Job j)
        {
            Plan p = j.P;
            // Storeys people go up to have solid walls all the way up.
            bool upstairs = j.Record != null && j.Record.Levels != null && j.Record.Levels.Top >= 1;
            for (int i = 0; i < j.N; i++)
            {
                float len = j.Len[i];
                if (len < 0.05f) continue;
                Vector2 a = j.A[i], d = j.Dir[i], o = j.Out[i];
                float yaw = j.Yaw(i);
                RaidMesh facing = j.Facing(i);
                Vector2 inset = -o * (Thick * 0.5f);   // the wall stands just inside the mapped outline
                Gaps(j, i);
                HolesIn(j, i);
                foreach (Seg seg in j.Tops[i])
                {
                    float s0 = seg.S0 <= 1e-3f ? -j.Run0[i] : seg.S0, s1 = seg.S1 >= len - 1e-3f ? len + j.Run1[i] : seg.S1;
                    float ground = Mathf.Min(seg.Top, GroundStorey);
                    WallLess(j, facing, a, d, inset, s0, s1, 0f, ground, yaw, j.WallS, true);
                    if (seg.Top > GroundStorey + 0.01f)
                        WallLess(j, j.Upper, a, d, inset, s0, s1, GroundStorey, seg.Top, yaw, j.WallS, upstairs);
                }
                if (j.Lite) continue;
                Foundation(j, i, facing, yaw);
                if (p.Belts && !j.Party(i)) Belts(j, i, yaw);
            }
            if (!j.Lite && p.CornerBoards) CornerBoards(j);
            StepWalls(j);
        }

        // The door openings in wall i's ground storey, by where they start: (start, end, head).
        private static void Gaps(Job j, int i)
        {
            s_Gaps.Clear();
            foreach (Hole h in j.Holes)
                if (h.Edge == i && h.Storey == 0 && h.Gap) s_Gaps.Add(new Vector3(h.S - h.W * 0.5f, h.S + h.W * 0.5f, h.Head));
            if (s_Gaps.Count > 1) s_Gaps.Sort(s_ByStart);
        }

        // Every opening through wall i: its doorways (from the ground up), its working and fixed windows (from their sills to
        // their heads, an arched one's to where its round head springs), as (start, end, bottom, top).
        private static void HolesIn(Job j, int i)
        {
            s_Holes.Clear();
            foreach (Hole h in j.Holes)
            {
                if (h.Edge != i) continue;
                if (h.Storey == 0 && h.Gap) s_Holes.Add(new Vector4(h.S - h.W * 0.5f, h.S + h.W * 0.5f, 0f, h.Head));
                else if (h.Working || h.Clear)
                {
                    float floor = j.FloorY(h.Storey);
                    s_Holes.Add(new Vector4(h.S - h.W * 0.5f, h.S + h.W * 0.5f, floor + h.Sill, floor + OpenTop(h)));
                }
            }
        }

        // How high a window's opening goes over its storey's floor: an arched one's to the springing of its round head (the
        // head stays coloured glass laid on the wall), the rest to their heads.
        private static float OpenTop(in Hole h) => h.Win == WindowKind.Arched ? h.Head - h.W * 0.5f : h.Head;

        // A run of wall from s0 to s1 along it and y0 to y1 up, less the openings in s_Holes: upright strips split at the
        // openings' sides, each the wall below, between and above whatever openings cross it.
        private static void WallLess(Job j, RaidMesh into, Vector2 a, Vector2 d, Vector2 inset, float s0, float s1, float y0, float y1,
            float yaw, in Surf surf, bool solid)
        {
            s_Cuts.Clear();
            foreach (Vector4 h in s_Holes)
            {
                if (h.y <= s0 || h.x >= s1 || h.w <= y0 || h.z >= y1) continue;
                s_Cuts.Add(Mathf.Max(h.x, s0));
                s_Cuts.Add(Mathf.Min(h.y, s1));
            }
            if (s_Cuts.Count == 0)
            {
                WallBox(j, into, a, d, inset, s0, s1, y0, y1, yaw, surf, solid);
                return;
            }
            s_Cuts.Add(s0);
            s_Cuts.Add(s1);
            s_Cuts.Sort();
            for (int k = 0; k + 1 < s_Cuts.Count; k++)
            {
                float c0 = s_Cuts[k], c1 = s_Cuts[k + 1];
                if (c1 - c0 < 0.005f) continue;
                float mid = (c0 + c1) * 0.5f, cur = y0;
                s_Ys.Clear();
                foreach (Vector4 h in s_Holes)
                    if (h.x < mid && h.y > mid && h.w > y0 && h.z < y1) s_Ys.Add(new Vector2(Mathf.Max(h.z, y0), Mathf.Min(h.w, y1)));
                if (s_Ys.Count > 1) s_Ys.Sort(s_ByBottom);
                foreach (Vector2 g in s_Ys)
                {
                    if (g.x > cur) WallBox(j, into, a, d, inset, c0, c1, cur, g.x, yaw, surf, solid);
                    cur = Mathf.Max(cur, g.y);
                }
                if (cur < y1) WallBox(j, into, a, d, inset, c0, c1, cur, y1, yaw, surf, solid);
            }
        }

        /// <summary>Off: the windows that don't work stay glass painted on the walls, a tall building's upper floors' a painted
        /// grid (the harness's "before" counts).</summary>
        internal static bool FixedWindows = true;

        // Which windows work (open and shut, smash, board up, climb through: a RaidWindow the interiors fit when their floor
        // is furnished): an enterable building's sash, picture, small and narrow ribbon windows, on a storey with a floor to
        // stand at them (the ground one, or one of its levels), and not into its stairwell; and only in a shell of its own
        // (the stand-in glass in them must be able to go). They're real openings in the wall (HolesIn); so are the fixed
        // windows (MarkClear) the rest of a building's with a floor at them are; any others stay glass drawn on it.
        private static void MarkWorking(Job j)
        {
            if (j.Record == null || j.Lite || j.Shell == null || j.Shell.Batched) return;
            LevelLayout L = j.Record.Levels;
            int top = L != null ? L.Top : 0;
            List<WindowSpan> spans = j.Record.Windows;
            for (int i = 0; i < j.Holes.Count; i++)
            {
                Hole h = j.Holes[i];
                if (!Works(j, L, top, h)) continue;
                h.Working = true;
                j.Holes[i] = h;
                int k = SpanOf(j, h);
                if (k < 0) continue;
                WindowSpan w = spans[k];
                w.Working = true;
                spans[k] = w;
            }
        }

        // Which of an enterable building's other windows are real openings too, clear glass fixed in them (a shop's front, a
        // church's, an office's bands, the windows of a tall building's upper floors, those too small, too big or lighting the
        // stairwell): every one on a storey with a floor to stand at it (else there's nothing but the shell to see in it), in a
        // shell of its own; they're seen through from both sides, and so is whatever's beyond them.
        private static void MarkClear(Job j)
        {
            if (!FixedWindows || j.Record == null || j.Lite || j.Shell == null || j.Shell.Batched) return;
            LevelLayout L = j.Record.Levels;
            int top = L != null ? L.Top : 0;
            List<WindowSpan> spans = j.Record.Windows;
            for (int i = 0; i < j.Holes.Count; i++)
            {
                Hole h = j.Holes[i];
                if (h.IsDoor || h.Working || h.Storey < 0 || h.Storey > top || h.W < 0.3f || OpenTop(h) - h.Sill < 0.3f) continue;
                h.Clear = true;
                j.Holes[i] = h;
                int k = SpanOf(j, h);
                if (k < 0) continue;
                WindowSpan w = spans[k];
                w.Clear = true;
                w.Plain = h.Fixed;
                w.Head = OpenTop(h);   // the interiors line and case the opening, not an arched one's head over it
                spans[k] = w;
            }
        }

        // The record's span of window h (or -1).
        private static int SpanOf(Job j, in Hole h)
        {
            List<WindowSpan> spans = j.Record.Windows;
            float from = h.S - h.W * 0.5f;
            for (int k = 0; k < spans.Count; k++)
            {
                WindowSpan w = spans[k];
                if (w.Edge == h.Edge && w.Storey == h.Storey && Mathf.Abs(w.From - from) <= 0.01f) return k;
            }
            return -1;
        }

        private static bool Works(Job j, LevelLayout L, int top, in Hole h)
        {
            if (h.IsDoor || h.Fixed || h.Storey < 0 || h.Storey > top) return false;
            if (h.Win != WindowKind.Sash && h.Win != WindowKind.Picture && h.Win != WindowKind.Small && h.Win != WindowKind.Ribbon) return false;
            if (h.W < 0.5f || h.W > 2.6f || h.Head - h.Sill < 0.6f) return false;
            if (L == null) return true;
            // Not into the stairwell: nobody climbs out over the stairs.
            Vector2 inside = j.At(h.Edge, h.S) - j.Out[h.Edge] * (Thick + 0.4f);
            return !L.Core.Contains(L.ToLocal(inside));
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
            if (solid) Collider(j, center, size, yaw);
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
            int every = p.Storeys > 6 ? Mathf.CeilToInt(p.Storeys / 4f) : 1;   // a tower's every few floors
            for (int k = 1; k < p.Storeys; k += every)
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
        // A collider for a wall or anything solid round a building, when the job's are: one of the site's merged prop
        // colliders (one mesh a site, cooked off the main thread), so a building costs no objects of its own for them.
        private static void Collider(Job j, Vector3 center, Vector3 size, float yaw)
        {
            if (j.Solid) j.Site.Town.PropCollider(center, size, yaw);
        }
        #endregion

        #region Inside
        // The floor, the ceiling (shown only from inside views), shelter from the weather, the ground the building takes
        // from the props, and its listing for the interiors.
        private static void Inside(Job j)
        {
            Plan p = j.P;
            Footprint f = j.F;
            LevelLayout levels = j.Record != null ? j.Record.Levels : null;
            bool roofed = p.Arch != Arch.Ruin && p.Arch != Arch.Canopy;
            if (!j.Lite)
            {
                List<int> tris = j.Tris;
                List<Vector2> pts = j.TriPts;
                Vector3 middle = To3(f.Middle, 0f);
                RaidMesh floor = j.Site.Town.Out.Flat(middle);
                RaidMesh ceilings = j.Site.Overhead.At(middle);
                Surf floorSurf = FloorOf(p);
                Surf ceilingSurf = CeilingSurf;
                float ceiling = p.Storeys == 1 && p.Eave > 4.5f && p.Arch != Arch.Row ? p.Eave - 0.15f : CeilingY;
                // Over a basement the ground floor is the building's own (its levels' structure); a stairwell going up
                // leaves the ground storey's ceiling open over it.
                bool basement = levels != null && levels.Basement, shaft = levels != null && levels.Top >= 1;
                var tri = new List<Vector2>(3);
                var pieces = new List<List<Vector2>>(4);
                for (int t = 0; t + 2 < tris.Count; t += 3)
                {
                    Vector2 a = pts[tris[t]], b = pts[tris[t + 1]], c = pts[tris[t + 2]];
                    if (!basement && !shaft)
                    {
                        floor.Tri(To3(a, 0.02f), To3(b, 0.02f), To3(c, 0.02f), floorSurf);
                        if (roofed) ceilings.Tri(To3(a, ceiling), To3(c, ceiling), To3(b, ceiling), ceilingSurf);
                        continue;
                    }
                    tri.Clear();
                    tri.Add(a);
                    tri.Add(b);
                    tri.Add(c);
                    Pieces(levels, tri, basement ? levels.Main : (Rect?)null, pieces);
                    foreach (List<Vector2> piece in pieces) FacingUp(floor, piece, 0.02f, floorSurf, false);
                    if (!roofed) continue;
                    Pieces(levels, tri, shaft ? levels.Shaft : (Rect?)null, pieces);
                    foreach (List<Vector2> piece in pieces) FacingUp(ceilings, piece, ceiling, ceilingSurf, true);
                }
            }
            Rows(j, roofed && !j.Lite, levels);
            DrawLevels(j);
            if (j.Record != null) j.Site.Enterables.Add(j.Record);
        }

        // A footprint triangle, less a rectangle of the levels' frame (or whole, without one).
        private static void Pieces(LevelLayout levels, List<Vector2> tri, Rect? hole, List<List<Vector2>> into)
        {
            into.Clear();
            if (hole == null || !FrameBounds(levels, tri).Overlaps(hole.Value)) into.Add(new List<Vector2>(tri));
            else Levels.OutsideRect(levels, tri, hole.Value, into);
        }

        // A flat convex polygon at height y, its face up (or down for a ceiling).
        private static void FacingUp(RaidMesh m, List<Vector2> poly, float y, in Surf s, bool down)
        {
            if (poly.Count < 3) return;
            if (MapGeometry.SignedArea(poly) > 0f) poly.Reverse();   // clockwise seen from above: a face drawn through it looks up
            for (int i = 1; i + 1 < poly.Count; i++)
            {
                if (down) m.Tri(To3(poly[0], y), To3(poly[i + 1], y), To3(poly[i], y), s);
                else m.Tri(To3(poly[0], y), To3(poly[i], y), To3(poly[i + 1], y), s);
            }
        }

        // The footprint in strips 2 m deep: each strip's stretches inside it are taken from the props and, under a roof,
        // registered as shelter (a rotated building's bounding box would shelter the snow round its corners). What's taken
        // is the outline's width at the strip's bottom, middle and top, widened to any corner standing in the strip, so a
        // slanting wall's sliver isn't left to the props (they'd stand in the rooms, and hang in a stairwell's shaft over a
        // basement); the shelter is the middle's.
        private static readonly List<float> s_Xs = new List<float>(16);
        private static readonly List<Vector2> s_Spans = new List<Vector2>(16);

        private static void Rows(Job j, bool shelter, LevelLayout levels = null)
        {
            Footprint f = j.F;
            List<Vector2> p = f.Pts;
            // Sheltered from the weather: the ground storey, or every level from the basement's floor to the top ceiling.
            float shelterBottom = levels != null ? levels.Floors[0] : 0f;
            float shelterTop = levels != null ? levels.Floors[levels.Count] : Mathf.Min(j.P.FirstStorey, j.P.WallHeight);
            for (float z0 = f.Bounds.yMin; z0 < f.Bounds.yMax; z0 += 2f)
            {
                float z1 = Mathf.Min(z0 + 2f, f.Bounds.yMax), zc = (z0 + z1) * 0.5f;
                s_Spans.Clear();
                Crossings(p, zc, s_Spans);
                int middle = s_Spans.Count;
                for (int i = 0; i < middle; i++)
                {
                    float x0 = s_Spans[i].x, x1 = s_Spans[i].y;
                    if (!shelter || x1 - x0 < 1f) continue;
                    Shelter.Register(j.Site.ToWorld(new Bounds(new Vector3((x0 + x1) * 0.5f, (shelterBottom + shelterTop) * 0.5f, zc),
                        new Vector3(x1 - x0 - 0.4f, shelterTop - shelterBottom, Mathf.Max(0.5f, z1 - z0 - 0.1f)))));
                }
                Crossings(p, Mathf.Min(z0 + 0.01f, zc), s_Spans);
                Crossings(p, Mathf.Max(z1 - 0.01f, zc), s_Spans);
                foreach (Vector2 v in p)
                {
                    if (v.y < z0 || v.y > z1) continue;
                    int best = -1;
                    float bestD = 2f;
                    for (int i = 0; i < s_Spans.Count; i++)
                    {
                        float d = Mathf.Max(0f, Mathf.Max(s_Spans[i].x - v.x, v.x - s_Spans[i].y));
                        if (d < bestD) { bestD = d; best = i; }
                    }
                    if (best >= 0 && bestD > 0f) s_Spans[best] = new Vector2(Mathf.Min(s_Spans[best].x, v.x), Mathf.Max(s_Spans[best].y, v.x));
                }
                foreach (Vector2 s in s_Spans)
                    if (s.y - s.x >= 0.3f) j.Site.Take(Rect.MinMaxRect(s.x, z0, s.y, z1));
            }
        }

        // The stretches of the outline across the line z (x from, x to), into `into`.
        private static void Crossings(List<Vector2> p, float z, List<Vector2> into)
        {
            s_Xs.Clear();
            for (int i = 0, k = p.Count - 1; i < p.Count; k = i++)
            {
                Vector2 a = p[k], b = p[i];
                if ((a.y > z) == (b.y > z)) continue;
                s_Xs.Add(a.x + (z - a.y) * (b.x - a.x) / (b.y - a.y));
            }
            s_Xs.Sort();
            for (int i = 0; i + 1 < s_Xs.Count; i += 2) into.Add(new Vector2(s_Xs[i], s_Xs[i + 1]));
        }

        // The shared floor and ceiling finishes, kept (DecorMaterials makes a key string on every ask).
        private static Surf s_Concrete, s_Ceiling, s_HomeFloor, s_HallFloor, s_ShopFloor, s_OfficeFloor, s_PorchWood, s_SwingWood;

        private static Surf Cached(ref Surf slot, System.Func<Surf> make)
        {
            if (!slot.Valid) slot = make();
            return slot;
        }

        private static Surf ConcreteSurf => Cached(ref s_Concrete, () => DecorMaterials.Concrete);
        private static Surf CeilingSurf => Cached(ref s_Ceiling, () => DecorMaterials.Ceiling);
        private static Surf PorchWood => Cached(ref s_PorchWood, () => DecorMaterials.WoodFloor(new Color(0.52f, 0.42f, 0.32f)));
        private static Surf SwingWood => Cached(ref s_SwingWood, () => DecorMaterials.WoodFloor(new Color(0.50f, 0.36f, 0.24f)));

        private static Surf FloorOf(Plan p)
        {
            switch (p.Arch)
            {
                case Arch.House: case Arch.Trailer: case Arch.Apartments: case Arch.Motel:
                    return Cached(ref s_HomeFloor, () => DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f)));
                case Arch.Warehouse: case Arch.Industrial: case Arch.Rickhouse: case Arch.Garage: case Arch.Shed: case Arch.Barn:
                case Arch.TobaccoBarn: case Arch.Greenhouse: case Arch.Ruin: case Arch.Parking:
                    return ConcreteSurf;
                case Arch.Church: case Arch.Courthouse:
                    return Cached(ref s_HallFloor, () => DecorMaterials.WoodFloor(new Color(0.46f, 0.32f, 0.20f)));
                case Arch.Store:
                    return Cached(ref s_ShopFloor, () => DecorMaterials.Linoleum(new Color(0.78f, 0.76f, 0.70f), new Color(0.60f, 0.58f, 0.54f), true));
                default:
                    return Cached(ref s_OfficeFloor, () => DecorMaterials.Linoleum(new Color(0.78f, 0.76f, 0.70f), new Color(0.60f, 0.58f, 0.54f), false));
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
            Vector3 f = d / len;
            if (Mathf.Abs(Vector3.Dot(f, up.normalized)) > 0.999f) up = Mathf.Abs(f.y) < 0.9f ? Vector3.up : Vector3.forward;
            Quaternion q = Quaternion.LookRotation(f, up);
            m.Box((a + b) * 0.5f, new Vector3(width, depth, len), q, s);
        }

        // Draw a single front step in front of a porch. Uses the building’s main wall surface.
        // The step is a low box (≈12 cm high, 25 cm deep) centred on the front wall.
        private static void DrawPorchStep(Job j)
        {
            if (!j.P.Porch) return; // only for buildings that have a porch
            int edge = j.P.Front; // the edge that is the building’s front
            // centre of the front wall (roughly where a door would be)
            float s = j.Len[edge] * 0.5f;
            // width covers most of the front wall, but leaves a bit of margin on each side
            float w = Mathf.Max(0f, j.Len[edge] - 0.4f);
            // step geometry: 0.12 m tall, 0.25 m deep, sitting on the ground storey
            OnWallBox(j, j.Facing(edge), edge, s, 0f, w, 0.12f, 0.25f, j.WallS);
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

        #region Dice
        /// <summary>A short run of random numbers from a feature's id and a salt, without allocating (a xorshift over
        /// <see cref="MapStyle.Hash"/>): for the small choices drawn while a building is raised.</summary>
        private struct Dice
        {
            private ulong _s;

            public Dice(long id, int salt)
            {
                _s = ((ulong)MapStyle.Hash(id, salt) << 32) | MapStyle.Hash(id, salt ^ 0x5BD1E995);
                if (_s == 0UL) _s = 0x9E3779B97F4A7C15UL;
            }

            /// <summary>In [0, 1).</summary>
            public float Next()
            {
                _s ^= _s << 13;
                _s ^= _s >> 7;
                _s ^= _s << 17;
                return (_s >> 40) * (1f / 16777216f);
            }

            public float Range(float a, float b) => a + Next() * (b - a);
            public bool Chance(float p) => Next() < p;
            public int Int(int n) => n > 0 ? Mathf.Min(n - 1, (int)(Next() * n)) : 0;
        }
        #endregion
    }
}
