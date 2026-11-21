using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What each street piece is to the roads: its class, winter state, carriageway edges per side, kerbs, sidewalks
    // (from the footways mapped beside it, else from how much of a town it runs through), markings; and the shapes the
    // rest of the pass builds from them (joints, runs, chains, mouths), with the grid the drawing's queries use.
    internal static partial class RoadPass
    {
        private enum Cls { Motorway, Trunk, Primary, Secondary, Tertiary, Minor, Service, Track, Path }

        private enum Winter { Ploughed, Packed, Ruts, Trodden, Paved }

        private enum CentreLine { None, Double, Dashed }

        private const float SnapGrid = 0.05f;
        private const float FootwayWidth = 1.8f;
        private const float ParallelParking = 2.4f, AngledParking = 5.4f;

        /// <summary>One side of a carriageway, seen along the way it's drawn: offsets in metres from the street's line.</summary>
        private sealed class Side
        {
            public float Edge;              // the carriageway's edge (the kerb face where there's a kerb)
            public bool Kerb;
            public float WalkIn, WalkOut;   // the sidewalk between these (both 0: none)
            public bool Mapped;             // the sidewalk is a footway mapped beside the street
            public bool Berm, EdgeLine, EdgeYellow;
            public float Lane;              // where the travel lanes end (edge lines, parking beyond)
            public bool Walk => WalkOut > WalkIn + 0.25f;

            public void CopyFrom(Side o)
            {
                Edge = o.Edge; Kerb = o.Kerb; WalkIn = o.WalkIn; WalkOut = o.WalkOut; Mapped = o.Mapped; Berm = o.Berm;
                EdgeLine = o.EdgeLine; EdgeYellow = o.EdgeYellow; Lane = o.Lane;
            }
        }

        /// <summary>A street piece inside the square, as the roads see it.</summary>
        private sealed class Way
        {
            public int Index;
            public Street S;
            public List<Vector2> P;
            public float[] Cum;
            public float Length;
            public Cls Cls;
            public bool Link, OneWay, Bridge, Gravel, Concrete;
            public int Layer;
            public Winter Winter;
            public readonly Side L = new Side(), R = new Side();
            public float Half;              // half the mapped carriageway
            public float Density, Shops, Setback = 99f, SetbackL = 99f, SetbackR = 99f;
            public bool Town, Downtown;
            public CentreLine Centre;
            public int LanesL, LanesR;      // travel lanes left and right of the centre line
            public float ArcOffset;         // the piece's first point, along its whole mapped way
            public int Order;
            // Footways found running beside it (lateral distance), each side.
            public List<float> NearL, NearR;
            // A path's samples every 2 m: the carriageway each runs beside (if any) and on which side of it; covered
            // where that side took the footway for its sidewalk (the street draws it then, not the path).
            public Way[] Beside;
            public bool[] BesideLeft, Covered;
            public bool Carriage => Cls <= Cls.Track;
            public bool Steps => S.Data.kind == "steps";
        }

        private sealed class Arm
        {
            public Way W;
            public int Vertex, Step;        // the joint's vertex in the way, and which way the arm runs (+1, -1)
            public Vector2 Dir, Nrm;        // away from the joint, and its left
            public float Angle, HL, HR;     // carriageway edges left and right of Dir
            public Side SL, SR;             // the way's sides as seen along Dir
            public Run Run;
            public float Cap, Trim;
            public float TanL, TanR;        // how far along it the corners on its left and right leave it
            public Vector2 CutAt, CutDir, EndL, EndR;
            public bool Entering, Major, StopBar, Crosswalk, Through;
            public Chain Chain;             // the chain leaving the junction by this arm
            public bool AtStart;            // ...from its start (else into its end)
        }

        private sealed class Corner
        {
            public int A, B;                // arm A's left side to arm B's right side
            public readonly List<Vector2> Path = new List<Vector2>();   // the kerb line, from arm A's side to arm B's
            public bool Convex, Rounded, Sharp, Skip;
            public Vector2 Meet;            // where the two kerb lines meet (Convex)
            public Vector2 Centre;          // the fillet's centre (Rounded)
            public float Radius, Tangent;   // the fillet's radius, and how far from Meet it leaves each kerb
            public Vector2 DirA, DirB;      // the kerb lines' directions where they meet
            public readonly List<Vector2> Arc = new List<Vector2>();    // the fillet alone, tangent to tangent
            // Sidewalks set back past wide verges (see PlanSetBacks): where their inner and outer edges meet, and how far
            // out along each arm its own sidewalk starts.
            public bool SetBack;
            public Vector2 InMeet, OutMeet;
            public float WalkA, WalkB;
        }

        private sealed class Joint
        {
            public Vector2 At;
            public readonly List<Arm> Arms = new List<Arm>();
            public bool Link;               // one road carrying on: no patch
            public Arm ThroughA, ThroughB;  // the road running straight through a junction (its chain isn't cut there)
            public Chain OnChain;           // ...the chain it runs on, how far along it, and its arm pointing along it
            public float AlongS;
            public Arm Fwd, Back;
            public List<Vector2> Patch;     // a full junction's paved patch, counter-clockwise, or null
            public Rect Bounds;
            public readonly List<List<Vector2>> Caps = new List<List<Vector2>>();   // a through junction's mouths
            public readonly List<Rect> CapBounds = new List<Rect>();
            public Way Top;                 // the arm whose surface the patch takes
            public readonly List<Corner> Corners = new List<Corner>();
            public Corner[] All;            // every corner by its first arm, the skipped ones too
            public bool Signals, Downtown;
            public bool Through => ThroughA != null;
            public bool HasPatch => (Patch != null && Patch.Count >= 3) || Caps.Count > 0;
        }

        private sealed class Run
        {
            public Way W;
            public int V0, V1;
            public Joint J0, J1;            // junctions (not links, not through junctions passed) at its ends
            public Arm A0, A1;
            public Run Next0, Next1;        // the run carrying on past a link or through junction at each end...
            public int NextEnd0, NextEnd1;  // ...and which of its ends meets this one
            public bool Seen;
            public float Length => W.Cum[V1] - W.Cum[V0];
        }

        private sealed class Part
        {
            public Run Run;
            public bool Rev;
            public float S0, S1;            // along the chain
        }

        /// <summary>Where a side road joins a chain running through its junction: the chain's kerb, sidewalk, berm and
        /// edge line on <see cref="Side"/> (+1 left) open between <see cref="Lo"/> and <see cref="Hi"/> along it.</summary>
        private sealed class Mouth
        {
            public Joint J;
            public float S, Lo, Hi;
            public int Side;
        }

        /// <summary>Runs strung together where a road simply carries on (past a link or straight through a junction):
        /// drawn as one ribbon, its bends mitred, its cross-section changing from part to part along it.</summary>
        private sealed class Chain
        {
            public readonly List<Vector2> P = new List<Vector2>();
            public float[] Cum;
            public Vector2[] N;             // each point's mitred left offset for 1 m
            public float Length;
            public readonly List<Part> Parts = new List<Part>();
            public Way W;                   // its first part's way (the class, winter state and surface are the same throughout)
            public float Trim0, Trim1;
            public Arm A0, A1;
            public Joint J0, J1;
            public bool Closed;
            public float From => Trim0;
            public float To => Length - Trim1;
            public Profile Lift;            // a raised bridge's deck heights, or null
            public int Id;
            public List<Cut> CutsL, CutsR;  // where the kerbs, sidewalks and berms open, each side
            public List<float> Crossings;   // mapped crossings along it, away from its ends
            public readonly List<Mouth> Mouths = new List<Mouth>();
            // The cross-section along it, each side (0 left, 1 right): knots at arc lengths KnotS, four values each in
            // KnotV (edge, sidewalk in, sidewalk out, lane end), linear between knots.
            public readonly float[][] KnotS = new float[2][], KnotV = new float[2][];
            public float ReachL, ReachR;    // the widest the carriageway gets, each side
            public bool Ramps;              // the cross-section changes somewhere along it
        }

        /// <summary>A chain's cross-section on one side at one point (see <see cref="ProfAt"/>).</summary>
        private struct Prof
        {
            public float Edge, WalkIn, WalkOut, Lane;
            public Side Of;                 // the side of the part there: its kerb, sidewalk, berm and edge line flags
            public bool Kerb => Of.Kerb;
            public bool Walk => Of.Walk && WalkOut > WalkIn + 0.25f;
            public float KerbBack => Of.Kerb ? Edge + KerbWidth : Edge;
        }

        private struct Sample
        {
            public Vector2 P, N;            // on the line; the left offset for 1 m (mitred at bends)
            public float S;
        }

        /// <summary>Everything one road pass works on.</summary>
        private sealed class Net
        {
            public readonly Site Site;
            public readonly bool Full;
            public readonly float Half;
            public readonly Rect Square;
            public readonly Look Look;
            public readonly List<Way> Ways = new List<Way>();
            public readonly List<Joint> Joints = new List<Joint>();
            public readonly List<Run> Runs = new List<Run>();
            public readonly List<Chain> Chains = new List<Chain>();
            public readonly List<Driveway> Driveways = new List<Driveway>();
            public readonly List<KerbStall> Stalls = new List<KerbStall>();
            public readonly List<Footprint> Prints = new List<Footprint>();
            public CellGrid PrintGrid, PaveGrid, WalkGrid, EdgeGrid;
            public readonly List<(Chain c, int k)> PaveItems = new List<(Chain, int)>();
            public readonly List<(Chain c, int k, int side)> WalkItems = new List<(Chain, int, int)>();
            public readonly List<(Joint j, int poly)> PatchItems = new List<(Joint, int)>();
            // The outlines of everything paved (ribbon sides and ends, patches, mouths), for rays (RayToRoad).
            public readonly List<Vector2> EdgeA = new List<Vector2>(), EdgeB = new List<Vector2>();
            public readonly List<Chain> EdgeChain = new List<Chain>();
            public readonly List<Joint> EdgeJoint = new List<Joint>();
            public readonly Dictionary<long, Joint> JointAt = new Dictionary<long, Joint>();
            public readonly List<int> Query = new List<int>(64);
            public readonly List<Sample> Samples = new List<Sample>(256);
            public readonly List<Sample> Samples2 = new List<Sample>(256);
            public int[] Stamp = new int[0];
            public int StampNow;

            public Net(Site site)
            {
                Site = site;
                Full = site.Detail == Detail.Full;
                Half = site.Half;
                Square = Rect.MinMaxRect(-Half, -Half, Half, Half);
                Look = new Look();
            }

            public OutdoorBatches Out => Site.Town.Out;

            /// <summary>A fresh query stamp over <paramref name="count"/> items (no allocation once grown).</summary>
            public int NewStamp(int count)
            {
                if (Stamp.Length < count) Stamp = new int[Mathf.NextPowerOfTwo(count + 1)];
                if (++StampNow == int.MaxValue)
                {
                    System.Array.Clear(Stamp, 0, Stamp.Length);
                    StampNow = 1;
                }
                return StampNow;
            }
        }

        /// <summary>A uniform grid of item lists over a square, for "what's near here" queries.</summary>
        private sealed class CellGrid
        {
            private readonly float _min, _cell;
            private readonly int _n;
            private readonly List<int>[] _cells;

            public CellGrid(float half, float cell)
            {
                _cell = cell;
                _n = Mathf.Max(1, Mathf.CeilToInt(half * 2f / cell));
                _min = -_n * cell * 0.5f;
                _cells = new List<int>[_n * _n];
            }

            private int Clamp(float v) => Mathf.Clamp(Mathf.FloorToInt((v - _min) / _cell), 0, _n - 1);

            public void Add(float x0, float z0, float x1, float z1, int item)
            {
                int cx0 = Clamp(x0), cx1 = Clamp(x1), cz0 = Clamp(z0), cz1 = Clamp(z1);
                for (int z = cz0; z <= cz1; z++)
                    for (int x = cx0; x <= cx1; x++)
                    {
                        List<int> l = _cells[z * _n + x] ??= new List<int>(4);
                        l.Add(item);
                    }
            }

            /// <summary>Every item in the cells the box touches, once each (<paramref name="stamp"/> marks them).</summary>
            public void Query(float x0, float z0, float x1, float z1, List<int> into, int[] stamps, int stamp)
            {
                into.Clear();
                int cx0 = Clamp(x0), cx1 = Clamp(x1), cz0 = Clamp(z0), cz1 = Clamp(z1);
                for (int z = cz0; z <= cz1; z++)
                    for (int x = cx0; x <= cx1; x++)
                    {
                        List<int> l = _cells[z * _n + x];
                        if (l == null) continue;
                        for (int i = 0; i < l.Count; i++)
                        {
                            int it = l[i];
                            if (it < stamps.Length && stamps[it] == stamp) continue;
                            if (it < stamps.Length) stamps[it] = stamp;
                            into.Add(it);
                        }
                    }
            }
        }

        #region Analysis
        private static void Analyse(Net net)
        {
            Site site = net.Site;
            List<Street> streets = site.Area.Streets;
            for (int i = 0; i < streets.Count; i++)
            {
                Street s = streets[i];
                if (s.Data.tunnel || s.Pts.Count < 2) continue;
                foreach (List<Vector2> piece in site.Chunked ? Whole(s.Pts) : MapGeometry.ClipLine(s.Pts, net.Square))
                {
                    Way w = NewWay(net, s, piece);
                    if (w != null) Classify(w);
                }
            }
            WayArcs(net);
            IndexBuildings(net);
            Townness(net);
            FindFootwaysBeside(net);
            foreach (Way w in net.Ways) DecideSides(net, w);
            // A footway is drawn by the street whose side took it for its sidewalk; elsewhere it's a path of its own.
            foreach (Way p in net.Ways)
            {
                if (p.Covered == null) continue;
                for (int i = 0; i < p.Covered.Length; i++)
                {
                    Way w = p.Beside[i];
                    p.Covered[i] = w != null && (p.BesideLeft[i] ? w.L : w.R).Mapped;
                }
            }
        }

        private static IEnumerable<List<Vector2>> Whole(List<Vector2> pts)
        {
            yield return pts;
        }

        private static Way NewWay(Net net, Street s, List<Vector2> pts)
        {
            // Drop repeated points (they make zero-length segments).
            var p = new List<Vector2>(pts.Count);
            for (int i = 0; i < pts.Count; i++)
                if (p.Count == 0 || (pts[i] - p[p.Count - 1]).sqrMagnitude > 1e-4f) p.Add(pts[i]);
            if (p.Count < 2) return null;
            var w = new Way { Index = net.Ways.Count, S = s, P = p, Cum = new float[p.Count] };
            for (int i = 1; i < p.Count; i++) w.Cum[i] = w.Cum[i - 1] + Vector2.Distance(p[i - 1], p[i]);
            w.Length = w.Cum[p.Count - 1];
            if (w.Length < 0.3f || float.IsNaN(w.Length) || float.IsInfinity(w.Length)) return null;
            net.Ways.Add(w);
            return w;
        }

        private static void Classify(Way w)
        {
            MapStreet d = w.S.Data;
            string kind = d.kind ?? "";
            w.Link = kind.EndsWith("_link");
            switch (w.Link ? kind.Substring(0, kind.Length - 5) : kind)
            {
                case "motorway": w.Cls = Cls.Motorway; break;
                case "trunk": w.Cls = Cls.Trunk; break;
                case "primary": w.Cls = Cls.Primary; break;
                case "secondary": w.Cls = Cls.Secondary; break;
                case "tertiary": w.Cls = Cls.Tertiary; break;
                case "residential": case "unclassified": case "living_street": case "road": case "busway": w.Cls = Cls.Minor; break;
                case "service": w.Cls = Cls.Service; break;
                case "track": w.Cls = Cls.Track; break;
                default: w.Cls = Cls.Path; break;
            }
            w.OneWay = d.oneway;
            w.Bridge = d.bridge;
            w.Layer = d.layer;
            string surface = d.surface ?? "";
            w.Gravel = surface == "gravel" || surface == "dirt";
            w.Concrete = surface == "concrete";
            float width = d.width;
            if (w.Carriage) width = Mathf.Clamp(width > 0.5f ? width : 6f, w.Cls == Cls.Track ? 2.6f : 3.2f, 30f);
            else width = Mathf.Clamp(width > 0.2f ? width : 1.5f, 0.9f, kind == "pedestrian" ? 12f : 4f);
            w.Half = width * 0.5f;
            w.L.Edge = w.R.Edge = w.L.Lane = w.R.Lane = w.Half;
            if (w.Cls <= Cls.Trunk)
            {
                // Shoulders outside the travel lanes (the mapped width): a wide one on the right, a narrow one by a median.
                float shoulder = w.Link ? 1.0f : w.Cls == Cls.Motorway ? 2.4f : 1.5f;
                w.R.Edge += shoulder;
                w.L.Edge += w.OneWay ? Mathf.Min(1.0f, shoulder * 0.5f) : shoulder;
            }
            switch (w.Cls)
            {
                case Cls.Motorway: case Cls.Trunk: case Cls.Primary: case Cls.Secondary: case Cls.Tertiary:
                    w.Winter = w.Gravel ? Winter.Packed : Winter.Ploughed;
                    break;
                case Cls.Minor: case Cls.Service:
                    w.Winter = Winter.Packed;
                    break;
                case Cls.Track:
                    w.Winter = Winter.Ruts;
                    break;
                default:
                    w.Winter = kind == "pedestrian" ? Winter.Paved : Winter.Trodden;
                    break;
            }
            w.Order = w.Carriage ? Mathf.Max(0, Cls.Service - w.Cls) : 0;
        }

        // The arc length from the start of each piece's whole mapped way to its first point, so dashes keep their
        // rhythm across a chunk's edge (each chunk sees only its own stretch of a way). The region's data is in
        // decimetres from its corner; finding where that corner lies in site metres takes any feature mapped at an exact
        // data point (a node, a point of interest, an unclipped street vertex).
        private static void WayArcs(Net net)
        {
            Area area = net.Site.Area;
            var fx = new List<double>(4);
            var fz = new List<double>(4);
            foreach (Node n in area.Nodes) AddFrame(fx, fz, n.At.x - n.Data.x * 0.1, n.At.y - n.Data.z * 0.1);
            foreach (Spot p in area.Points) AddFrame(fx, fz, p.At.x - p.Data.x * 0.1, p.At.y - p.Data.z * 0.1);
            foreach (Way w in net.Ways)
            {
                if (!w.Carriage) continue;
                if (FrameOf(w, fx, fz)) continue;
                // No frame known yet for this way's region: one from its own vertices.
                int[] d = w.S.Data.pts;
                if (d == null || w.P.Count < 3) continue;
                Vector2 v = w.P[1], next = w.P[2];
                for (int k = 0; k + 3 < d.Length; k += 2)
                {
                    double ox = v.x - d[k] * 0.1, oz = v.y - d[k + 1] * 0.1;
                    if (Mathf.Abs((float)(ox + d[k + 2] * 0.1) - next.x) > 0.003f || Mathf.Abs((float)(oz + d[k + 3] * 0.1) - next.y) > 0.003f) continue;
                    AddFrame(fx, fz, ox, oz);
                    break;
                }
                FrameOf(w, fx, fz);
            }
        }

        private static void AddFrame(List<double> fx, List<double> fz, double x, double z)
        {
            for (int i = 0; i < fx.Count; i++)
                if (System.Math.Abs(fx[i] - x) < 0.02 && System.Math.Abs(fz[i] - z) < 0.02) return;
            fx.Add(x);
            fz.Add(z);
        }

        // Tries each frame: the right one lays the way's mapped points along the piece.
        private static bool FrameOf(Way w, List<double> fx, List<double> fz)
        {
            int[] d = w.S.Data.pts;
            if (d == null || d.Length < 4) return false;
            Vector2 first = w.P[0], last = w.P[w.P.Count - 1];
            for (int f = 0; f < fx.Count; f++)
            {
                double ox = fx[f], oz = fz[f];
                float arc = 0f, firstArc = -1f;
                bool lastOn = false;
                var a = new Vector2((float)(ox + d[0] * 0.1), (float)(oz + d[1] * 0.1));
                for (int k = 2; k + 1 < d.Length; k += 2)
                {
                    var b = new Vector2((float)(ox + d[k] * 0.1), (float)(oz + d[k + 1] * 0.1));
                    float len = Vector2.Distance(a, b);
                    if (firstArc < 0f && MapGeometry.DistanceToSegment(first, a, b) < 0.03f) firstArc = arc + Vector2.Distance(a, first);
                    if (!lastOn && MapGeometry.DistanceToSegment(last, a, b) < 0.03f) lastOn = true;
                    arc += len;
                    a = b;
                }
                if (firstArc < 0f || !lastOn) continue;
                w.ArcOffset = firstArc;
                return true;
            }
            return false;
        }

        private static void IndexBuildings(Net net)
        {
            Area area = net.Site.Area;
            float reach = net.Half + 40f;
            foreach (Footprint f in area.Buildings) AddPrint(net, f, reach);
            foreach (Footprint f in area.Foreign) AddPrint(net, f, reach);
            net.PrintGrid = new CellGrid(reach, 12f);
            for (int i = 0; i < net.Prints.Count; i++)
            {
                Rect b = net.Prints[i].Bounds;
                net.PrintGrid.Add(b.xMin, b.yMin, b.xMax, b.yMax, i);
            }
        }

        private static void AddPrint(Net net, Footprint f, float reach)
        {
            Rect b = f.Bounds;
            if (f.Pts.Count < 3 || b.xMax < -reach || b.xMin > reach || b.yMax < -reach || b.yMin > reach) return;
            net.Prints.Add(f);
        }

        private static bool IsShop(string kind) => kind == "commercial" || kind == "retail" || kind == "office"
            || kind == "hotel" || kind == "civic";

        // How much of a town each carriageway runs through: buildings within 35 m of its line, per 100 m (and how many
        // of them are shops, offices and the like), and how far their nearest corners stand back from its edge, each side.
        private static void Townness(Net net)
        {
            var left = new List<float>(32);
            var right = new List<float>(32);
            foreach (Way w in net.Ways)
            {
                if (!w.Carriage || w.Cls == Cls.Track) continue;
                left.Clear();
                right.Clear();
                int shops = 0;
                int stamp = net.NewStamp(net.Prints.Count);
                MapGeometry.Extent(w.P, out float x0, out float z0, out float x1, out float z1);
                net.PrintGrid.Query(x0 - 35f, z0 - 35f, x1 + 35f, z1 + 35f, net.Query, net.Stamp, stamp);
                foreach (int bi in net.Query)
                {
                    List<Vector2> pts = net.Prints[bi].Pts;
                    float best = float.MaxValue, side = 0f;
                    for (int v = 0; v < pts.Count; v++)
                    {
                        Vector2 q = pts[v];
                        for (int k = 0; k + 1 < w.P.Count; k++)
                        {
                            Vector2 a = w.P[k], ab = w.P[k + 1] - a;
                            float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                            float dist = (a + ab * t - q).magnitude;
                            if (dist >= best) continue;
                            best = dist;
                            side = Cross(ab, q - a);
                        }
                    }
                    if (best > 35f + w.Half) continue;
                    (side >= 0f ? left : right).Add(Mathf.Max(0f, best - w.Half));
                    if (IsShop(net.Prints[bi].Data.kind)) shops++;
                }
                int count = left.Count + right.Count;
                float per = 100f / Mathf.Max(40f, w.Length);
                w.Density = count * per;
                w.Shops = shops * per;
                // The nearer quarter of the buildings stand this far back: the fronts, not the garages behind them.
                w.SetbackL = Quartile(left);
                w.SetbackR = Quartile(right);
                left.AddRange(right);
                w.Setback = Quartile(left);
                w.Town = (w.Density >= 2.5f && w.Setback <= 12f) || w.Density >= 8f || w.S.Data.lit == 1;
                w.Downtown = w.Density >= 4f && w.Setback <= 3.5f;
            }
        }

        private static float Quartile(List<float> v)
        {
            if (v.Count == 0) return 99f;
            v.Sort();
            return v[v.Count / 4];
        }

        private static float Median(List<float> v)
        {
            if (v.Count == 0) return 99f;
            v.Sort();
            return v[v.Count / 2];
        }

        // Footways mapped beside a street may be its sidewalks: every 2 m of every footway, the nearest carriageway it runs
        // alongside (within 20 degrees, past the carriageway's edge, within 11 m of it) gets a note of how far out it is.
        private static void FindFootwaysBeside(Net net)
        {
            var grid = new CellGrid(net.Half + 4f, 10f);
            var items = new List<(Way w, int k)>();
            foreach (Way w in net.Ways)
            {
                if (w.Cls < Cls.Primary || w.Cls > Cls.Minor || w.Link) continue;
                for (int k = 0; k + 1 < w.P.Count; k++)
                {
                    Vector2 a = w.P[k], b = w.P[k + 1];
                    float r = w.Half + 12f;
                    grid.Add(Mathf.Min(a.x, b.x) - r, Mathf.Min(a.y, b.y) - r, Mathf.Max(a.x, b.x) + r, Mathf.Max(a.y, b.y) + r, items.Count);
                    items.Add((w, k));
                }
            }
            if (items.Count == 0) return;
            foreach (Way p in net.Ways)
            {
                if (p.Cls != Cls.Path || p.Steps) continue;
                int samples = Mathf.Max(1, Mathf.CeilToInt(p.Length / 2f));
                p.Covered = new bool[samples];
                p.Beside = new Way[samples];
                p.BesideLeft = new bool[samples];
                for (int i = 0; i < samples; i++)
                {
                    float s = (i + 0.5f) * p.Length / samples;
                    PointAt(p, s, out Vector2 q, out Vector2 t);
                    int stamp = net.NewStamp(items.Count);
                    grid.Query(q.x, q.y, q.x, q.y, net.Query, net.Stamp, stamp);
                    Way best = null;
                    float bestD = float.MaxValue;
                    bool bestLeft = false;
                    foreach (int it in net.Query)
                    {
                        (Way w, int k) = items[it];
                        Vector2 a = w.P[k], ab = w.P[k + 1] - a;
                        float len = ab.magnitude;
                        if (len < 0.05f) continue;
                        Vector2 d = ab / len;
                        if (Mathf.Abs(Vector2.Dot(d, t)) < 0.94f) continue;
                        float along = Vector2.Dot(q - a, d);
                        if (along < -1f || along > len + 1f) continue;
                        float lat = Cross(d, q - a);
                        float dist = Mathf.Abs(lat);
                        if (dist < w.Half + 0.3f || dist > w.Half + 11f || dist >= bestD) continue;
                        best = w;
                        bestD = dist;
                        bestLeft = lat > 0f;
                    }
                    if (best == null) continue;
                    p.Beside[i] = best;
                    p.BesideLeft[i] = bestLeft;
                    if (bestLeft) (best.NearL ??= new List<float>()).Add(bestD);
                    else (best.NearR ??= new List<float>()).Add(bestD);
                }
            }
        }

        /// <summary>The point and direction <paramref name="s"/> metres along a way.</summary>
        private static void PointAt(Way w, float s, out Vector2 p, out Vector2 dir)
        {
            int k = 0;
            while (k + 2 < w.P.Count && w.Cum[k + 1] < s) k++;
            Vector2 a = w.P[k], b = w.P[k + 1];
            float len = w.Cum[k + 1] - w.Cum[k];
            dir = len > 1e-5f ? (b - a) / len : Vector2.right;
            p = a + dir * Mathf.Clamp(s - w.Cum[k], 0f, len);
        }

        // Kerbs, sidewalks, berms, edge lines, the winter state and the markings of a carriageway. A footway mapped
        // along a side is its sidewalk: at the kerb, or past a parking lane (parallel, or angled where the street is
        // lined with shops), or past a snowy verge however wide. Without one, the side takes the sidewalk tag, else what
        // the town around it suggests.
        private static void DecideSides(Net net, Way w)
        {
            if (!w.Carriage) return;
            string tag = w.S.Data.sidewalk ?? "";
            bool townClass = w.Cls >= Cls.Primary && w.Cls <= Cls.Minor && !w.Link;
            bool main = w.Cls >= Cls.Primary && w.Cls <= Cls.Tertiary && !w.Link;
            // A shopping street: lined with buildings, most of them shops, offices and the like.
            bool shopping = w.Density >= 6f && w.Shops >= 0.5f * w.Density;
            if (w.Cls == Cls.Minor && w.Downtown) w.Winter = Winter.Ploughed;
            for (int side = 0; side < 2; side++)
            {
                Side d = side == 0 ? w.L : w.R;
                List<float> near = side == 0 ? w.NearL : w.NearR;
                float setback = side == 0 ? w.SetbackL : w.SetbackR;
                bool tagged = tag == "both" || (side == 0 ? tag == "left" : tag == "right");
                bool footway = townClass && near != null && near.Count * 2f >= Mathf.Min(10f, w.Length * 0.3f);
                float dist = footway ? Median(near) : 0f;
                float gap = dist - FootwayWidth * 0.5f - d.Edge;
                if (footway)
                {
                    float far = dist + FootwayWidth * 0.5f + 0.25f;
                    d.Kerb = true;
                    if (gap < 2.0f)
                    {
                        // At the kerb: paved from the kerb to the footway's far side.
                        d.Mapped = true;
                        d.WalkIn = d.Edge + KerbWidth;
                        d.WalkOut = Mathf.Max(d.WalkIn + 1.5f, far);
                    }
                    else if (main && shopping && gap <= 4.5f)
                    {
                        // A lane of parallel parking between the traffic and the sidewalk.
                        d.Mapped = true;
                        d.Edge += ParallelParking;
                        d.WalkIn = d.Edge + KerbWidth;
                        d.WalkOut = Mathf.Max(d.WalkIn + 1.5f, far);
                    }
                    else if (main && shopping && gap <= 9f)
                    {
                        // Angled parking in front of the buildings.
                        d.Mapped = true;
                        d.Edge += AngledParking;
                        d.WalkIn = d.Edge + KerbWidth;
                        d.WalkOut = Mathf.Max(d.WalkIn + 1.5f, far);
                    }
                    else
                    {
                        // A snowy verge between the kerb and the sidewalk, however wide: a threshold here would flip
                        // between two chunks that see the footway a little nearer or farther (each measures its own piece).
                        d.Mapped = true;
                        d.WalkIn = Mathf.Max(d.Edge + KerbWidth + 0.4f, dist - FootwayWidth * 0.5f - 0.25f);
                        d.WalkOut = Mathf.Max(d.WalkIn + 1.2f, far);
                    }
                }
                else if (tagged || (tag != "no" && townClass && w.Town))
                {
                    d.Kerb = true;
                    float sb = setback < 50f ? setback : w.Setback;
                    if (w.Downtown || (tagged && sb < 3.5f))
                    {
                        d.WalkIn = d.Edge + KerbWidth;
                        d.WalkOut = d.WalkIn + Mathf.Clamp(sb - 0.3f, 2.0f, 4.5f);
                    }
                    else
                    {
                        float verge = sb >= 7f ? 1.4f : 0.5f;
                        d.WalkIn = d.Edge + KerbWidth + verge;
                        d.WalkOut = d.WalkIn + 1.5f;
                    }
                }
                d.Berm = (w.Winter == Winter.Ploughed || w.Winter == Winter.Packed) && !w.Bridge;
                d.EdgeLine = w.Winter == Winter.Ploughed && (w.Cls <= Cls.Trunk || (w.Cls <= Cls.Tertiary && !d.Kerb && !w.Town));
                d.EdgeYellow = side == 0 && w.OneWay && w.Cls <= Cls.Trunk;
            }
            // The travel lanes keep the mapped width on the street's line; parking lanes and shoulders lie beyond them.
            w.L.Lane = w.R.Lane = w.Half;
            // A street parked along is the middle of town.
            if (main && (w.L.Edge > w.Half + 1f || w.R.Edge > w.Half + 1f)) w.Downtown = true;
            int lanes = w.S.Data.lanes;
            if (w.OneWay)
            {
                w.LanesL = 0;
                w.LanesR = Mathf.Clamp(lanes > 0 ? lanes : Mathf.RoundToInt(w.Half * 2f / 3.6f), 1, 6);
            }
            else
            {
                int each = Mathf.Clamp(lanes > 0 ? (lanes + 1) / 2 : (w.Half * 2f >= 13f && w.Cls <= Cls.Secondary ? 2 : 1), 1, 4);
                w.LanesL = w.LanesR = each;
            }
            if (w.Winter == Winter.Ploughed && w.Cls <= Cls.Tertiary && !w.OneWay)
                w.Centre = w.Cls <= Cls.Secondary || w.Town ? CentreLine.Double : CentreLine.Dashed;
        }
        #endregion
    }
}
