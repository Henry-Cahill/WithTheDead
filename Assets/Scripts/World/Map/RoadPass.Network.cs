using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What each street piece is to the roads (class, widths per side, kerbs, sidewalks, winter state, markings), where
    // the carriageways meet (joints: junctions with a paved patch, or a road simply carrying on), how the pieces string
    // together into chains drawn in one go, and the indexes the drawing asks "is this spot on a road / in a building?".
    internal static partial class RoadPass
    {
        private enum Cls { Motorway, Trunk, Primary, Secondary, Tertiary, Minor, Service, Track, Path }

        private enum Winter { Ploughed, Packed, Ruts, Trodden, Paved }

        private enum CentreLine { None, Double, Dashed }

        private const float SnapGrid = 0.05f;
        private const float FootwayWidth = 1.8f;

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

            public bool Like(Side o) => Mathf.Abs(Edge - o.Edge) < 0.35f && Kerb == o.Kerb && Walk == o.Walk
                && (!Walk || Mathf.Abs(WalkOut - o.WalkOut) < 0.5f) && Berm == o.Berm;
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
            public float Density, Setback = 99f, SetbackL = 99f, SetbackR = 99f;
            public bool Town, Downtown;
            public CentreLine Centre;
            public int LanesL, LanesR;      // travel lanes left and right of the centre line
            public float ArcOffset;         // the piece's first point, along its whole mapped way
            public int Order;
            // Footways found running beside it (lateral distance, along), each side.
            public List<float> NearL, NearR;
            // A path's samples: covered (beside a road, on a road, in a building) or crossing a carriageway.
            public bool[] Covered;
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
            public Vector2 CutAt, CutDir, EndL, EndR;
            public bool Entering, Major, StopBar, Crosswalk;
            public Chain Chain;             // the chain leaving the junction by this arm
            public bool AtStart;            // ...from its start (else into its end)
        }

        private sealed class Corner
        {
            public int A, B;                // arm A's left side to arm B's right side
            public readonly List<Vector2> Path = new List<Vector2>();   // the kerb line, from arm A's end to arm B's
            public bool Convex, Rounded, Sharp;
            public Vector2 Meet;            // where the two kerb lines meet (Convex)
            public Vector2 Centre;          // the fillet's centre (Rounded)
            public float Radius;
        }

        private sealed class Joint
        {
            public Vector2 At;
            public readonly List<Arm> Arms = new List<Arm>();
            public bool Link;               // one road carrying on: no patch
            public List<Vector2> Patch;     // counter-clockwise, or null
            public Rect Bounds;
            public Way Top;                 // the arm whose surface the patch takes
            public readonly List<Corner> Corners = new List<Corner>();
            public bool Signals, Downtown;
        }

        private sealed class Run
        {
            public Way W;
            public int V0, V1;
            public Joint J0, J1;            // junctions (not links) at its ends
            public Arm A0, A1;
            public Run Next0, Next1;        // the run carrying on past a link at each end...
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

        /// <summary>Runs strung together where a road simply carries on: drawn as one ribbon, its bends mitred.</summary>
        private sealed class Chain
        {
            public readonly List<Vector2> P = new List<Vector2>();
            public float[] Cum;
            public Vector2[] N;             // each point's mitred left offset for 1 m
            public float Length;
            public readonly List<Part> Parts = new List<Part>();
            public Way W;
            public readonly Side L = new Side(), R = new Side();
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
            public readonly List<Footprint> Prints = new List<Footprint>();
            public CellGrid PrintGrid, PaveGrid, WalkGrid;
            public readonly List<(Chain c, int k)> PaveItems = new List<(Chain, int)>();
            public readonly List<(Chain c, int k, int side)> WalkItems = new List<(Chain, int, int)>();
            public readonly List<Joint> PatchItems = new List<Joint>();
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
            if (w.Length < 0.3f) return null;
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

        // How much of a town each carriageway runs through: buildings within 35 m of its line, per 100 m, and how far
        // their nearest corners stand back from its edge, each side.
        private static void Townness(Net net)
        {
            var left = new List<float>(32);
            var right = new List<float>(32);
            foreach (Way w in net.Ways)
            {
                if (!w.Carriage || w.Cls == Cls.Track) continue;
                left.Clear();
                right.Clear();
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
                }
                int count = left.Count + right.Count;
                w.Density = count * 100f / Mathf.Max(40f, w.Length);
                // The nearer quarter of the buildings stand this far back: the fronts, not the garages behind them.
                w.SetbackL = Quartile(left);
                w.SetbackR = Quartile(right);
                left.AddRange(right);
                w.Setback = Quartile(left);
                w.Town = (w.Density >= 2.5f && w.Setback <= 12f) || w.S.Data.lit == 1;
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

        // Footways mapped beside a street are its sidewalks: every 2 m of every footway, the nearest carriageway it runs
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
                for (int i = 0; i < samples; i++)
                {
                    float s = (i + 0.5f) * p.Length / samples;
                    PointAt(p, s, out Vector2 q, out Vector2 t);
                    int stamp = net.NewStamp(items.Count);
                    grid.Query(q.x, q.y, q.x, q.y, net.Query, net.Stamp, stamp);
                    Way best = null;
                    float bestD = float.MaxValue, bestAlong = 0f;
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
                        bestAlong = w.Cum[k] + Mathf.Clamp(along, 0f, len);
                    }
                    if (best == null) continue;
                    p.Covered[i] = true;
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

        // Kerbs, sidewalks, berms, edge lines, the winter state and the markings of a carriageway.
        private static void DecideSides(Net net, Way w)
        {
            if (!w.Carriage) return;
            string tag = w.S.Data.sidewalk ?? "";
            bool townClass = w.Cls >= Cls.Primary && w.Cls <= Cls.Minor && !w.Link;
            if (w.Cls == Cls.Minor && w.Downtown) w.Winter = Winter.Ploughed;
            for (int side = 0; side < 2; side++)
            {
                Side d = side == 0 ? w.L : w.R;
                List<float> near = side == 0 ? w.NearL : w.NearR;
                float setback = side == 0 ? w.SetbackL : w.SetbackR;
                bool tagged = tag == "both" || (side == 0 ? tag == "left" : tag == "right");
                if (near != null && near.Count * 2f >= Mathf.Min(10f, w.Length * 0.3f) && townClass)
                {
                    // A footway mapped beside it: pave up to it when the gap is a parking lane's or a downtown sidewalk's
                    // worth, else keep a snowy verge between the kerb and the footway.
                    float dist = Median(near);
                    float gap = dist - FootwayWidth * 0.5f - d.Edge;
                    float most = w.Cls <= Cls.Tertiary ? 6.5f : 2.5f;
                    d.Kerb = true;
                    d.Mapped = true;
                    if (gap <= most)
                    {
                        d.Edge = Mathf.Max(d.Edge, dist - FootwayWidth * 0.5f - 0.6f);
                        d.WalkIn = d.Edge + KerbWidth;
                    }
                    else d.WalkIn = dist - FootwayWidth * 0.5f - 0.25f;
                    d.WalkOut = dist + FootwayWidth * 0.5f + 0.25f;
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
            // The travel lanes keep the mapped width, centred between the kerbs; widened streets park along them.
            float centre = (w.L.Edge - w.R.Edge) * 0.5f;
            if (w.Cls <= Cls.Trunk) centre = 0f;
            w.L.Lane = w.Half + centre;
            w.R.Lane = w.Half - centre;
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

        #region Joints, junctions and chains
        private static long SnapKey(Vector2 p)
            => ((long)Mathf.RoundToInt(p.x / SnapGrid) << 32) ^ (uint)Mathf.RoundToInt(p.y / SnapGrid);

        private static void Junctions(Net net)
        {
            FindJoints(net);
            MakeRuns(net);
            foreach (Joint j in net.Joints) if (!j.Link) Shape(net, j);
            MakeChains(net);
            IndexPavement(net);
        }

        private static void FindJoints(Net net)
        {
            var touches = new Dictionary<long, List<(Way w, int v)>>();
            var keys = new List<long>();
            foreach (Way w in net.Ways)
            {
                if (!w.Carriage) continue;
                for (int v = 0; v < w.P.Count; v++)
                {
                    long key = SnapKey(w.P[v]);
                    if (!touches.TryGetValue(key, out List<(Way, int)> list))
                    {
                        touches[key] = list = new List<(Way, int)>(2);
                        keys.Add(key);
                    }
                    list.Add((w, v));
                }
            }
            foreach (long key in keys)
            {
                List<(Way w, int v)> list = touches[key];
                if (list.Count < 2) continue;
                var j = new Joint { At = list[0].w.P[list[0].v] };
                foreach ((Way w, int v) in list)
                {
                    AddArm(j, w, v, -1);
                    AddArm(j, w, v, 1);
                }
                if (j.Arms.Count < 2) continue;
                j.Arms.Sort((a, b) => a.Angle.CompareTo(b.Angle));
                j.Link = j.Arms.Count == 2 && Carries(j.Arms[0], j.Arms[1]);
                net.Joints.Add(j);
                net.JointAt[key] = j;
            }
            foreach (Node n in net.Site.Area.Nodes)
            {
                if (n.Data.kind != "traffic_signals") continue;
                foreach (Joint j in net.Joints)
                    if (!j.Link && (j.At - n.At).sqrMagnitude < 15f * 15f) j.Signals = true;
            }
        }

        private static void AddArm(Joint j, Way w, int v, int step)
        {
            int n = v + step;
            if (n < 0 || n >= w.P.Count) return;
            Vector2 d = w.P[n] - w.P[v];
            if (d.sqrMagnitude < 1e-6f) return;
            d.Normalize();
            var a = new Arm
            {
                W = w, Vertex = v, Step = step, Dir = d, Nrm = Left(d), Angle = Mathf.Atan2(d.y, d.x),
                SL = step > 0 ? w.L : w.R, SR = step > 0 ? w.R : w.L,
            };
            a.HL = a.SL.Edge;
            a.HR = a.SR.Edge;
            // Traffic comes in along an arm unless it's a one-way street leaving the junction.
            a.Entering = !w.OneWay || step < 0;
            j.Arms.Add(a);
        }

        // Two arms that are one road carrying on: both are ends of their pieces, nearly opposite, and alike (seen
        // travelling through, one's left side is the other's left side).
        private static bool Carries(Arm a, Arm b)
        {
            bool endA = a.Vertex == 0 || a.Vertex == a.W.P.Count - 1, endB = b.Vertex == 0 || b.Vertex == b.W.P.Count - 1;
            if (!endA || !endB || Vector2.Dot(a.Dir, b.Dir) > -0.5f) return false;
            if (a.W == b.W && a.W.P.Count < 4) return false;
            Way x = a.W, y = b.W;
            if (x.Cls != y.Cls || x.Winter != y.Winter || x.Bridge != y.Bridge || x.OneWay != y.OneWay || x.Link != y.Link) return false;
            return a.SR.Like(b.SL) && a.SL.Like(b.SR);
        }

        // Each carriageway split at the junctions on it into runs; each run's ends know their junction arm or the run
        // they carry on into.
        private static void MakeRuns(Net net)
        {
            var arms = new Dictionary<long, (Joint j, Arm a)>();
            foreach (Joint j in net.Joints)
                foreach (Arm a in j.Arms) arms[((long)a.W.Index << 22) | ((long)a.Vertex << 1) | (a.Step > 0 ? 1L : 0L)] = (j, a);
            foreach (Way w in net.Ways)
            {
                if (!w.Carriage) continue;
                int start = 0;
                for (int v = 1; v < w.P.Count; v++)
                {
                    bool split = v == w.P.Count - 1;
                    if (!split && arms.TryGetValue(((long)w.Index << 22) | ((long)v << 1) | 1L, out (Joint j, Arm a) here) && !here.j.Link) split = true;
                    if (!split) continue;
                    var r = new Run { W = w, V0 = start, V1 = v };
                    if (arms.TryGetValue(((long)w.Index << 22) | ((long)start << 1) | 1L, out (Joint j, Arm a) s))
                    {
                        s.a.Run = r;
                        if (!s.j.Link)
                        {
                            r.J0 = s.j;
                            r.A0 = s.a;
                        }
                    }
                    if (arms.TryGetValue(((long)w.Index << 22) | ((long)v << 1) | 0L, out (Joint j, Arm a) e))
                    {
                        e.a.Run = r;
                        if (!e.j.Link)
                        {
                            r.J1 = e.j;
                            r.A1 = e.a;
                        }
                    }
                    net.Runs.Add(r);
                    start = v;
                }
            }
            // Links: the two runs meeting at a link carry on into each other.
            foreach (Joint j in net.Joints)
            {
                if (!j.Link) continue;
                Arm a = j.Arms[0], b = j.Arms[1];
                if (a.Run == null || b.Run == null) continue;
                int ea = a.Step > 0 ? 0 : 1, eb = b.Step > 0 ? 0 : 1;
                if (ea == 0) { a.Run.Next0 = b.Run; a.Run.NextEnd0 = eb; } else { a.Run.Next1 = b.Run; a.Run.NextEnd1 = eb; }
                if (eb == 0) { b.Run.Next0 = a.Run; b.Run.NextEnd0 = ea; } else { b.Run.Next1 = a.Run; b.Run.NextEnd1 = ea; }
            }
            // How far each junction may reach along its arms: half a run between two junctions, nearly all of a run
            // that ends anywhere else.
            foreach (Run r in net.Runs)
            {
                float len = r.Length;
                if (r.A0 != null) r.A0.Cap = Mathf.Max(0.2f, r.A1 != null ? len * 0.48f : len - 0.3f);
                if (r.A1 != null) r.A1.Cap = Mathf.Max(0.2f, r.A0 != null ? len * 0.48f : len - 0.3f);
            }
        }

        // The corner radius of the kerb (or the paved edge) between two arms.
        private static float CornerRadius(Joint j, Arm a, Arm b)
        {
            if (j.Arms.Count == 2) return a.W.Cls <= Cls.Tertiary ? 9f : 6f;
            Cls lo = a.W.Cls > b.W.Cls ? a.W.Cls : b.W.Cls;
            if (lo == Cls.Track || lo == Cls.Service) return 2.2f;
            if (a.SL.Kerb && b.SR.Kerb) return a.W.Downtown || b.W.Downtown ? 3.5f : 4.5f;
            Cls hi = a.W.Cls < b.W.Cls ? a.W.Cls : b.W.Cls;
            return hi <= Cls.Trunk ? 10f : 7f;
        }

        // A junction's paved patch: each arm cut where the kerb returns between it and its neighbours are done, the
        // corners rounded (a fillet of the corner's radius between the two kerb lines), the polygon run round the
        // junction counter-clockwise.
        private static void Shape(Net net, Joint j)
        {
            int n = j.Arms.Count;
            var tl = new float[n];
            var tr = new float[n];
            var corners = new Corner[n];
            for (int i = 0; i < n; i++)
            {
                Arm a = j.Arms[i], b = j.Arms[(i + 1) % n];
                var c = new Corner { A = i, B = (i + 1) % n };
                corners[i] = c;
                float phi = Mathf.Repeat(b.Angle - a.Angle, Mathf.PI * 2f);
                if (phi < 0.17f || phi > Mathf.PI * 2f - 0.01f) continue;   // arms on top of each other: they just overlap
                if (phi > Mathf.PI - 0.17f) continue;                       // straight on, or the outside of a bend
                c.Convex = true;
                // Where the two kerb lines (a's left edge, b's right edge) meet.
                Vector2 rhs = -b.Nrm * b.HR - a.Nrm * a.HL;
                float den = Cross(a.Dir, b.Dir);
                float s = Cross(rhs, b.Dir) / den, u = Cross(rhs, a.Dir) / den;
                float r = CornerRadius(j, a, b) * Mathf.Clamp(phi / (Mathf.PI * 0.5f), 0.45f, 1.1f);
                float q = r / Mathf.Tan(phi * 0.5f);
                if (s + q > a.Cap || u + q > b.Cap)
                {
                    q = Mathf.Min(a.Cap - s, b.Cap - u);
                    if (q <= 0.05f) q = 0f;
                    r = q * Mathf.Tan(phi * 0.5f);
                }
                tl[i] = Mathf.Clamp(s + q, 0f, a.Cap);
                tr[(i + 1) % n] = Mathf.Clamp(u + q, 0f, b.Cap);
                c.Meet = j.At + a.Nrm * a.HL + a.Dir * s;
                c.Sharp = s <= a.Cap && u <= b.Cap && s > -2f && u > -2f;
                if (r > 0.3f && s + q <= a.Cap + 0.01f && u + q <= b.Cap + 0.01f)
                {
                    c.Rounded = true;
                    c.Radius = r;
                    Vector2 bis = (a.Dir + b.Dir).normalized;
                    c.Centre = c.Meet + bis * (r / Mathf.Sin(phi * 0.5f));
                }
            }
            for (int i = 0; i < n; i++)
            {
                Arm a = j.Arms[i];
                float t = Mathf.Max(tl[i], tr[i]);
                if (n >= 3) t = Mathf.Max(t, 0.5f);
                a.Trim = Mathf.Min(Mathf.Max(t, 0.3f), Mathf.Max(0.2f, a.Cap));
                CutArm(a);
            }
            // The kerb line round each corner, from arm A's left end to arm B's right end.
            for (int i = 0; i < n; i++)
            {
                Corner c = corners[i];
                Arm a = j.Arms[c.A], b = j.Arms[c.B];
                c.Path.Add(a.EndL);
                if (c.Rounded)
                {
                    Vector2 ta = j.At + a.Nrm * a.HL + a.Dir * tl[i], tb = j.At - b.Nrm * b.HR + b.Dir * tr[c.B];
                    if ((ta - a.EndL).sqrMagnitude > 0.01f) c.Path.Add(ta);
                    float a0 = Mathf.Atan2(ta.y - c.Centre.y, ta.x - c.Centre.x), a1 = Mathf.Atan2(tb.y - c.Centre.y, tb.x - c.Centre.x);
                    float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(sweep) * c.Radius / 0.9f), 2, 12);
                    for (int k = 1; k < steps; k++)
                    {
                        float ang = a0 + sweep * k / steps;
                        c.Path.Add(c.Centre + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * c.Radius);
                    }
                    if ((tb - b.EndR).sqrMagnitude > 0.01f) c.Path.Add(tb);
                }
                else if (c.Convex && c.Sharp)
                {
                    if ((c.Meet - a.EndL).sqrMagnitude > 0.01f && (c.Meet - b.EndR).sqrMagnitude > 0.01f) c.Path.Add(c.Meet);
                }
                else if (!c.Convex && n >= 2)
                {
                    // Straight on or round the outside of a bend: through the point where the two edges meet, when
                    // that's near the patch.
                    Vector2 rhs = -b.Nrm * b.HR - a.Nrm * a.HL;
                    float den = Cross(a.Dir, b.Dir);
                    if (Mathf.Abs(den) > 0.05f)
                    {
                        float s = Cross(rhs, b.Dir) / den, u = Cross(rhs, a.Dir) / den;
                        if (s < a.Trim && u < b.Trim && s > -8f && u > -8f) c.Path.Add(j.At + a.Nrm * a.HL + a.Dir * s);
                    }
                }
                c.Path.Add(b.EndR);
                j.Corners.Add(c);
            }
            var poly = new List<Vector2>(n * 6);
            for (int i = 0; i < n; i++)
            {
                Corner c = corners[i];
                for (int k = 0; k + 1 < c.Path.Count; k++) poly.Add(c.Path[k]);
                poly.Add(j.Arms[c.B].EndR);
            }
            // The list above starts at arm 0's left end; each corner's last point is the next arm's right end.
            j.Patch = new List<Vector2>(poly.Count);
            for (int i = 0; i < poly.Count; i++)
                if (j.Patch.Count == 0 || (poly[i] - j.Patch[j.Patch.Count - 1]).sqrMagnitude > 1e-4f) j.Patch.Add(poly[i]);
            j.Bounds = MapGeometry.Bounds(j.Patch);
            Arm top = j.Arms[0];
            foreach (Arm a in j.Arms) if (a.W.Cls < top.W.Cls || (a.W.Cls == top.W.Cls && a.W.Winter < top.W.Winter)) top = a;
            j.Top = top.W;
            j.Downtown = top.W.Downtown;
            foreach (Arm a in j.Arms) a.Major = a.W.Cls == top.W.Cls;
            bool allEqual = true;
            foreach (Arm a in j.Arms) if (!a.Major) allEqual = false;
            // Two equal streets meeting in a T: the stem stops, the street running through doesn't.
            Arm stem = null;
            if (allEqual && n == 3)
                for (int i = 0; i < 3; i++)
                {
                    Arm x = j.Arms[(i + 1) % 3], y = j.Arms[(i + 2) % 3];
                    if (Vector2.Dot(x.Dir, y.Dir) < -0.85f) stem = j.Arms[i];
                }
            foreach (Arm a in j.Arms)
            {
                bool minorRoad = a.W.Cls <= Cls.Minor;
                a.StopBar = a.Entering && minorRoad && n >= 3 && (j.Signals || !a.Major || a == stem || (allEqual && n >= 4));
                a.Crosswalk = minorRoad && n >= 3 && a.SL.Walk && a.SR.Walk && (j.Signals || j.Downtown);
            }
        }

        // Where an arm leaves the patch: its line's point and direction at the trim, and the carriageway's corners there.
        private static void CutArm(Arm a)
        {
            Way w = a.W;
            float s0 = w.Cum[a.Vertex];
            float target = a.Step > 0 ? s0 + a.Trim : s0 - a.Trim;
            target = Mathf.Clamp(target, 0f, w.Length);
            PointAt(w, target, out Vector2 p, out Vector2 d);
            if (a.Step < 0) d = -d;
            a.CutAt = p;
            a.CutDir = d;
            Vector2 left = Left(d);
            a.EndL = p + left * a.HL;
            a.EndR = p - left * a.HR;
        }

        private static void MakeChains(Net net)
        {
            foreach (Run start in net.Runs)
            {
                if (start.Seen) continue;
                // Walk back to where the chain begins (a run end that doesn't carry on), or all the way round a loop.
                Run cur = start;
                bool rev = false;
                bool closed = false;
                for (int guard = 0; guard < 100000; guard++)
                {
                    Run back = rev ? cur.Next1 : cur.Next0;
                    int backEnd = rev ? cur.NextEnd1 : cur.NextEnd0;
                    if (back == null) break;
                    cur = back;
                    rev = backEnd == 0;
                    if (cur == start)
                    {
                        closed = true;
                        break;
                    }
                }
                var c = new Chain { Closed = closed, Id = net.Chains.Count };
                for (int guard = 0; guard < 100000 && cur != null && !cur.Seen; guard++)
                {
                    cur.Seen = true;
                    var part = new Part { Run = cur, Rev = rev, S0 = c.P.Count == 0 ? 0f : c.Length };
                    Way w = cur.W;
                    int from = rev ? cur.V1 : cur.V0, to = rev ? cur.V0 : cur.V1, step = rev ? -1 : 1;
                    for (int v = from; ; v += step)
                    {
                        if (c.P.Count == 0 || (w.P[v] - c.P[c.P.Count - 1]).sqrMagnitude > 1e-4f)
                        {
                            if (c.P.Count > 0) c.Length += Vector2.Distance(c.P[c.P.Count - 1], w.P[v]);
                            c.P.Add(w.P[v]);
                        }
                        if (v == to) break;
                    }
                    part.S1 = c.Length;
                    c.Parts.Add(part);
                    Run next = rev ? cur.Next0 : cur.Next1;
                    int nextEnd = rev ? cur.NextEnd0 : cur.NextEnd1;
                    cur = next;
                    rev = nextEnd == 1;
                }
                if (c.P.Count < 2 || c.Length < 0.05f) continue;
                Part first = c.Parts[0], last = c.Parts[c.Parts.Count - 1];
                c.W = first.Run.W;
                c.L.CopyFrom(first.Rev ? c.W.R : c.W.L);
                c.R.CopyFrom(first.Rev ? c.W.L : c.W.R);
                if (first.Rev)
                {
                    // Seen the other way round, the edge lines' colours swap with the sides.
                    c.L.EdgeYellow = false;
                    c.R.EdgeYellow = c.W.OneWay && c.W.Cls <= Cls.Trunk;
                }
                if (!closed)
                {
                    c.A0 = first.Rev ? first.Run.A1 : first.Run.A0;
                    c.J0 = first.Rev ? first.Run.J1 : first.Run.J0;
                    c.A1 = last.Rev ? last.Run.A0 : last.Run.A1;
                    c.J1 = last.Rev ? last.Run.J0 : last.Run.J1;
                    c.Trim0 = c.A0 != null ? c.A0.Trim : 0f;
                    c.Trim1 = c.A1 != null ? c.A1.Trim : 0f;
                    if (c.A0 != null)
                    {
                        c.A0.Chain = c;
                        c.A0.AtStart = true;
                    }
                    if (c.A1 != null)
                    {
                        c.A1.Chain = c;
                        c.A1.AtStart = false;
                    }
                }
                c.Cum = new float[c.P.Count];
                for (int i = 1; i < c.P.Count; i++) c.Cum[i] = c.Cum[i - 1] + Vector2.Distance(c.P[i - 1], c.P[i]);
                c.Length = c.Cum[c.P.Count - 1];
                c.N = new Vector2[c.P.Count];
                for (int i = 0; i < c.P.Count; i++)
                {
                    bool wrap = closed && (i == 0 || i == c.P.Count - 1);
                    Vector2 prev = i > 0 ? c.P[i - 1] : wrap ? c.P[c.P.Count - 2] : c.P[i];
                    Vector2 next = i + 1 < c.P.Count ? c.P[i + 1] : wrap ? c.P[1] : c.P[i];
                    Vector2 dPrev = (c.P[i] - prev).normalized, dNext = (next - c.P[i]).normalized;
                    if (i == 0 && !wrap) dPrev = dNext;
                    if (i == c.P.Count - 1 && !wrap) dNext = dPrev;
                    Vector2 nPrev = Left(dPrev), nNext = Left(dNext), m = nPrev + nNext;
                    m = m.sqrMagnitude > 1e-6f ? m.normalized : nNext;
                    c.N[i] = m / Mathf.Max(0.5f, Vector2.Dot(m, nNext));
                }
                net.Chains.Add(c);
            }
        }

        // The carriageways (chains between their trims, and the junction patches) and the sidewalks, gridded for "is
        // this spot on a road or a sidewalk" questions.
        private static void IndexPavement(Net net)
        {
            net.PaveGrid = new CellGrid(net.Half + 20f, 8f);
            net.WalkGrid = new CellGrid(net.Half + 20f, 8f);
            int item = 0;
            foreach (Chain c in net.Chains)
            {
                float reach = Mathf.Max(c.L.Edge, c.R.Edge);
                float walk = Mathf.Max(c.L.Walk ? c.L.WalkOut : 0f, c.R.Walk ? c.R.WalkOut : 0f);
                for (int k = 0; k + 1 < c.P.Count; k++)
                {
                    if (c.Cum[k + 1] < c.From || c.Cum[k] > c.To) continue;
                    Vector2 a = c.P[k], b = c.P[k + 1];
                    float x0 = Mathf.Min(a.x, b.x), z0 = Mathf.Min(a.y, b.y), x1 = Mathf.Max(a.x, b.x), z1 = Mathf.Max(a.y, b.y);
                    net.PaveGrid.Add(x0 - reach, z0 - reach, x1 + reach, z1 + reach, item++);
                    net.PaveItems.Add((c, k));
                    if (walk > 0f)
                    {
                        net.WalkGrid.Add(x0 - walk, z0 - walk, x1 + walk, z1 + walk, net.WalkItems.Count);
                        net.WalkItems.Add((c, k, 0));
                    }
                }
            }
            foreach (Joint j in net.Joints)
            {
                if (j.Patch == null || j.Patch.Count < 3) continue;
                net.PaveGrid.Add(j.Bounds.xMin, j.Bounds.yMin, j.Bounds.xMax, j.Bounds.yMax, item++);
                net.PatchItems.Add(j);
            }
        }

        /// <summary>True when <paramref name="q"/> lies on a carriageway (a chain's between its trims, widened by
        /// <paramref name="margin"/>, other than <paramref name="except"/>) or a junction patch.</summary>
        private static bool OnPavement(Net net, Vector2 q, Chain except, float margin, bool patches = true)
        {
            int stamp = net.NewStamp(net.PaveItems.Count + net.PatchItems.Count);
            net.PaveGrid.Query(q.x, q.y, q.x, q.y, net.Query, net.Stamp, stamp);
            foreach (int it in net.Query)
            {
                if (it >= net.PaveItems.Count)
                {
                    if (!patches) continue;
                    Joint j = net.PatchItems[it - net.PaveItems.Count];
                    if (j.Bounds.Contains(q) && MapGeometry.Contains(j.Patch, q)) return true;
                    continue;
                }
                (Chain c, int k) = net.PaveItems[it];
                if (c == except) continue;
                Vector2 a = c.P[k], ab = c.P[k + 1] - a;
                float len = c.Cum[k + 1] - c.Cum[k];
                if (len < 1e-4f) continue;
                Vector2 d = ab / len;
                float along = Vector2.Dot(q - a, d);
                if (along < -0.05f || along > len + 0.05f) continue;
                float s = c.Cum[k] + along;
                if (s < c.From - 0.05f || s > c.To + 0.05f) continue;
                float lat = Cross(d, q - a);
                if (lat <= c.L.Edge + margin && -lat <= c.R.Edge + margin) return true;
            }
            return false;
        }

        /// <summary>True when <paramref name="q"/> lies on a sidewalk band (or the verge before it), other than
        /// <paramref name="except"/>'s.</summary>
        private static bool OnSidewalk(Net net, Vector2 q, Chain except, float margin)
        {
            int stamp = net.NewStamp(net.WalkItems.Count);
            net.WalkGrid.Query(q.x, q.y, q.x, q.y, net.Query, net.Stamp, stamp);
            foreach (int it in net.Query)
            {
                (Chain c, int k, int _) = net.WalkItems[it];
                if (c == except) continue;
                Vector2 a = c.P[k], ab = c.P[k + 1] - a;
                float len = c.Cum[k + 1] - c.Cum[k];
                if (len < 1e-4f) continue;
                Vector2 d = ab / len;
                float along = Vector2.Dot(q - a, d);
                if (along < -0.05f || along > len + 0.05f) continue;
                float s = c.Cum[k] + along;
                if (s < c.From - 0.5f || s > c.To + 0.5f) continue;
                float lat = Cross(d, q - a);
                if (lat > 0f ? c.L.Walk && lat >= c.L.Edge - margin && lat <= c.L.WalkOut + margin
                             : c.R.Walk && -lat >= c.R.Edge - margin && -lat <= c.R.WalkOut + margin) return true;
            }
            return false;
        }

        /// <summary>True when <paramref name="q"/> is inside a building's footprint (grown by <paramref name="margin"/>).</summary>
        private static bool InBuilding(Net net, Vector2 q, float margin)
        {
            int stamp = net.NewStamp(net.Prints.Count);
            net.PrintGrid.Query(q.x - margin, q.y - margin, q.x + margin, q.y + margin, net.Query, net.Stamp, stamp);
            foreach (int bi in net.Query)
            {
                Footprint f = net.Prints[bi];
                Rect b = f.Bounds;
                if (q.x < b.xMin - margin || q.x > b.xMax + margin || q.y < b.yMin - margin || q.y > b.yMax + margin) continue;
                if (MapGeometry.Contains(f.Pts, q)) return true;
                if (margin > 0f && MapGeometry.DistanceToEdge(f.Pts, q) < margin) return true;
            }
            return false;
        }

        /// <summary>How far a ray from <paramref name="from"/> along <paramref name="dir"/> goes before it meets a
        /// building's wall (at most <paramref name="max"/>).</summary>
        private static float RayToBuilding(Net net, Vector2 from, Vector2 dir, float max)
        {
            Vector2 to = from + dir * max;
            int stamp = net.NewStamp(net.Prints.Count);
            net.PrintGrid.Query(Mathf.Min(from.x, to.x), Mathf.Min(from.y, to.y), Mathf.Max(from.x, to.x), Mathf.Max(from.y, to.y),
                net.Query, net.Stamp, stamp);
            float best = max;
            foreach (int bi in net.Query)
            {
                List<Vector2> p = net.Prints[bi].Pts;
                for (int i = 0, k = p.Count - 1; i < p.Count; k = i++)
                {
                    Vector2 e = p[i] - p[k];
                    float den = Cross(dir, e);
                    if (Mathf.Abs(den) < 1e-6f) continue;
                    Vector2 w = p[k] - from;
                    float t = Cross(w, e) / den, u = Cross(w, dir) / den;
                    if (t >= 0f && t < best && u >= 0f && u <= 1f) best = t;
                }
            }
            return best;
        }

        /// <summary>Points along a chain from <paramref name="from"/> to <paramref name="to"/>: its corners in between
        /// and, on longer stretches, one every <paramref name="step"/> metres (0: corners only); added to what
        /// <paramref name="into"/> holds when <paramref name="append"/> (a point it already ends with isn't repeated).</summary>
        private static void SamplesOf(Chain c, float from, float to, float step, List<Sample> into, bool append = false)
        {
            if (!append) into.Clear();
            if (to - from < 0.02f) return;
            for (int k = 0; k + 1 < c.P.Count; k++)
            {
                float s0 = c.Cum[k], s1 = c.Cum[k + 1];
                if (s1 <= from || s0 >= to) continue;
                Vector2 a = c.P[k];
                float len = s1 - s0;
                if (len < 1e-5f) continue;
                Vector2 d = (c.P[k + 1] - a) / len, n = Left(d);
                float lo = Mathf.Max(from, s0), hi = Mathf.Min(to, s1);
                if (into.Count == 0 || lo > into[into.Count - 1].S + 1e-3f)
                    into.Add(new Sample { P = a + d * (lo - s0), N = lo <= s0 + 1e-4f ? c.N[k] : n, S = lo });
                if (step > 0f)
                {
                    int pieces = Mathf.CeilToInt((hi - lo) / step);
                    for (int i = 1; i < pieces; i++)
                    {
                        float s = lo + (hi - lo) * i / pieces;
                        into.Add(new Sample { P = a + d * (s - s0), N = n, S = s });
                    }
                }
                into.Add(new Sample { P = a + d * (hi - s0), N = hi >= s1 - 1e-4f ? c.N[k + 1] : n, S = hi });
            }
        }
        #endregion
    }
}
