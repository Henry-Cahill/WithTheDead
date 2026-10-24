using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The roofs. A pitched roof goes on the blocks of the outline (BuildingPass.Shape): the main block's ridge along its
    // length; a wing's ridge out from the block it joins, running into that block's roof to its ridge so the two meet in
    // valleys; a shallow addition along a block's side gets a lean-to under its eave; a two-storey house's rear ell may
    // drop a storey. Gable, hip, pyramid, lean-to, gambrel, barrel and mansard roofs, each with fascia, soffits, barge
    // boards and ridge caps, and the snow on it: a slab on each face over the band of it the snow still covers (all of it
    // but the eave and ridge on a cold roof, the lower or upper half when it's slid off a metal roof or melted on a warm
    // one, little on a south-facing slope), icicles along the eaves of heated buildings. A roof that won't go on blocks
    // is flat, behind a parapet with a coping, a cornice on a downtown front, units and vents on top.
    internal static partial class BuildingPass
    {
        private const int SaltSnow = 0x5A0A, SaltIcicles = 0x1C1E, SaltUnits = 0x4AC0;

        /// <summary>One block of a pitched roof.</summary>
        private sealed class RoofPart
        {
            /// <summary>The block, in frame coordinates (x = u, y = v).</summary>
            public Rect R;
            public bool AlongU;
            public RoofKind Kind;
            /// <summary>The top of the walls under it.</summary>
            public float Eave;
            public float Pitch;
            public int Parent = -1;
            /// <summary>Which side of the block meets its parent: 0 u-min, 1 u-max, 2 v-min, 3 v-max.</summary>
            public int Side = -1;
            /// <summary>How far its roof reaches into its parent's through that side.</summary>
            public float Ext;
            /// <summary>A lean-to's height at its high side.</summary>
            public float High;
            public float Ridge;
        }

        #region Planning
        private static readonly List<Rect> s_OneBlock = new List<Rect>(1);
        private static readonly List<float> s_Ts = new List<float>(16);

        private static void PlanRoof(Job j)
        {
            Plan p = j.P;
            Shape sh = j.Sh;
            List<Rect> blocks = sh.Blocks;
            if (p.Roof != RoofKind.Flat)
            {
                if (blocks.Count == 0)
                {
                    if (sh.Fill >= 0.8f && (sh.Orthogonal || sh.Fill >= 0.88f))
                    {
                        s_OneBlock.Clear();
                        s_OneBlock.Add(sh.Box);
                        blocks = s_OneBlock;
                        // A gable end on a slanted wall would stand off it: hip the roof instead.
                        if (!sh.Orthogonal && (p.Roof == RoofKind.Gable || p.Roof == RoofKind.Gambrel || p.Roof == RoofKind.Skillion))
                            p.Roof = RoofKind.Hip;
                    }
                    else
                    {
                        p.Roof = RoofKind.Flat;
                        if (p.Parapet < 0.3f) p.Parapet = 0.35f;
                        p.Covering = RoofCover.Membrane;
                        p.CoverTone = 0;
                    }
                }
                if ((p.Roof == RoofKind.Mansard || p.Roof == RoofKind.Round) && blocks.Count != 1)
                {
                    if (p.Roof == RoofKind.Mansard)
                    {
                        p.Roof = RoofKind.Flat;
                        p.Parapet = Mathf.Max(p.Parapet, 0.5f);
                    }
                    else p.Roof = RoofKind.Gable;
                }
            }
            if (p.Roof == RoofKind.Flat || p.Roof == RoofKind.Mansard)
            {
                float top = p.Roof == RoofKind.Flat ? p.Eave + p.Parapet : p.Eave;
                for (int i = 0; i < j.N; i++) j.Tops[i].Add(new Seg { S0 = 0f, S1 = j.Len[i], Top = top });
                return;
            }
            j.Parts = Parts(j, blocks);
            EdgeTops(j);
        }

        private static float Overlap(float a0, float a1, float b0, float b1) => Mathf.Min(a1, b1) - Mathf.Max(a0, b0);

        private static List<RoofPart> Parts(Job j, List<Rect> blocks)
        {
            Plan p = j.P;
            var parts = new List<RoofPart>(blocks.Count);
            float tanMain = Mathf.Tan(p.Pitch * Mathf.Deg2Rad);
            Vector2 front = p.Front >= 0 ? j.Out[p.Front] : Vector2.zero;
            for (int k = 0; k < blocks.Count; k++)
            {
                Rect r = blocks[k];
                var part = new RoofPart { R = r, Kind = p.Roof, Eave = p.Eave, Pitch = p.Pitch, AlongU = r.width >= r.height };
                if (part.Kind == RoofKind.Pyramid && Mathf.Max(r.width, r.height) > Mathf.Min(r.width, r.height) * 1.3f) part.Kind = RoofKind.Hip;
                parts.Add(part);
                if (k == 0) continue;
                int parent = -1, side = -1;
                float shared = 0.3f;
                for (int q = 0; q < k; q++)
                {
                    Rect o = parts[q].R;
                    float s;
                    if (Mathf.Abs(r.xMin - o.xMax) < 0.05f && (s = Overlap(r.yMin, r.yMax, o.yMin, o.yMax)) > shared) { shared = s; parent = q; side = 0; }
                    if (Mathf.Abs(r.xMax - o.xMin) < 0.05f && (s = Overlap(r.yMin, r.yMax, o.yMin, o.yMax)) > shared) { shared = s; parent = q; side = 1; }
                    if (Mathf.Abs(r.yMin - o.yMax) < 0.05f && (s = Overlap(r.xMin, r.xMax, o.xMin, o.xMax)) > shared) { shared = s; parent = q; side = 2; }
                    if (Mathf.Abs(r.yMax - o.yMin) < 0.05f && (s = Overlap(r.xMin, r.xMax, o.xMin, o.xMax)) > shared) { shared = s; parent = q; side = 3; }
                }
                if (parent < 0) continue;
                RoofPart par = parts[parent];
                part.Parent = parent;
                part.Side = side;
                float depth = side < 2 ? r.width : r.height, wide = side < 2 ? r.height : r.width;
                bool atParentEnd = (side < 2) == par.AlongU;
                float parentHalf = (par.AlongU ? par.R.height : par.R.width) * 0.5f;
                float parentTan = Mathf.Tan(par.Pitch * Mathf.Deg2Rad);
                bool wing = (depth >= wide * 0.6f || depth > 4.5f) && p.Roof != RoofKind.Skillion;
                if (wing)
                {
                    part.AlongU = side < 2;
                    part.Kind = p.Roof == RoofKind.Gambrel || p.Roof == RoofKind.Round ? RoofKind.Gable
                        : p.Roof == RoofKind.Pyramid ? RoofKind.Hip : p.Roof;
                    float half = wide * 0.5f;
                    if (par.Kind == RoofKind.Skillion) part.Kind = RoofKind.Gable;
                    // A wing wider than its parent would ride over its ridge: flatten it to meet it.
                    if (half > parentHalf + 0.05f && !atParentEnd) part.Pitch = Mathf.Atan(parentTan * parentHalf / half) * Mathf.Rad2Deg;
                    if (atParentEnd)
                        part.Ext = par.Kind == RoofKind.Hip || par.Kind == RoofKind.Pyramid ? Mathf.Min(half + 0.1f, (par.AlongU ? par.R.width : par.R.height) * 0.5f) : 0.25f;
                    else part.Ext = parentHalf;
                    // A two-storey house's rear ell is often a storey lower.
                    Vector2 toPart = j.Sh.World(r.center) - j.Sh.World(par.R.center);
                    if (p.Storeys >= 2 && r.width * r.height < par.R.width * par.R.height * 0.5f && Vector2.Dot(toPart, front) < 0f
                        && MapStyle.Unit(j.F.Data.id, SaltPlan + k) < 0.5f)
                    {
                        part.Eave = p.FirstStorey;
                        part.Ext = 0.2f;
                    }
                }
                else
                {
                    // A lean-to along the parent's side, its high edge tucked under the parent's eave.
                    part.Kind = RoofKind.Skillion;
                    part.AlongU = side >= 2;
                    part.High = atParentEnd ? par.Eave - 0.12f : par.Eave - p.Overhang * parentTan - 0.08f;
                    part.Pitch = 14f;
                    float low = part.High - Mathf.Tan(part.Pitch * Mathf.Deg2Rad) * depth;
                    if (low < 2.3f)
                    {
                        low = Mathf.Min(2.3f, part.High - 0.15f);
                        part.Pitch = Mathf.Atan((part.High - low) / Mathf.Max(0.5f, depth)) * Mathf.Rad2Deg;
                    }
                    part.Eave = low;
                    part.Ext = 0.15f;
                }
            }
            for (int k = 0; k < parts.Count; k++)
            {
                RoofPart part = parts[k];
                if (part.Kind == RoofKind.Skillion && part.Parent < 0)
                {
                    // A free-standing lean-to (a shed): its high side at the back.
                    float depth = part.AlongU ? part.R.height : part.R.width;
                    part.High = part.Eave + Mathf.Tan(part.Pitch * Mathf.Deg2Rad) * depth;
                }
            }
            return parts;
        }

        // Each wall's top, stretch by stretch: the eave of the block behind it.
        private static void EdgeTops(Job j)
        {
            Shape sh = j.Sh;
            List<RoofPart> parts = j.Parts;
            for (int i = 0; i < j.N; i++)
            {
                float len = j.Len[i];
                List<Seg> tops = j.Tops[i];
                s_Ts.Clear();
                s_Ts.Add(0f);
                s_Ts.Add(len);
                Vector2 a = sh.Local(j.A[i]);
                float du = Vector2.Dot(j.Dir[i], sh.U), dv = Vector2.Dot(j.Dir[i], sh.V);
                foreach (RoofPart part in parts)
                {
                    AddCut(a.x, du, part.R.xMin, len);
                    AddCut(a.x, du, part.R.xMax, len);
                    AddCut(a.y, dv, part.R.yMin, len);
                    AddCut(a.y, dv, part.R.yMax, len);
                }
                s_Ts.Sort();
                for (int k = 0; k + 1 < s_Ts.Count; k++)
                {
                    float t0 = s_Ts[k], t1 = s_Ts[k + 1];
                    if (t1 - t0 < 0.02f) continue;
                    Vector2 probe = sh.Local(j.At(i, (t0 + t1) * 0.5f) - j.Out[i] * 0.3f);
                    float top = parts[0].Eave;
                    foreach (RoofPart part in parts)
                    {
                        Rect r = part.R;
                        if (probe.x < r.xMin - 0.05f || probe.x > r.xMax + 0.05f || probe.y < r.yMin - 0.05f || probe.y > r.yMax + 0.05f) continue;
                        top = part.Eave;
                        break;
                    }
                    if (tops.Count > 0 && Mathf.Abs(tops[tops.Count - 1].Top - top) < 0.01f)
                    {
                        Seg last = tops[tops.Count - 1];
                        last.S1 = t1;
                        tops[tops.Count - 1] = last;
                    }
                    else tops.Add(new Seg { S0 = t0, S1 = t1, Top = top });
                }
                if (tops.Count == 0) tops.Add(new Seg { S0 = 0f, S1 = len, Top = parts[0].Eave });
            }
        }

        private static void AddCut(float start, float rate, float at, float len)
        {
            if (Mathf.Abs(rate) < 1e-3f) return;
            float t = (at - start) / rate;
            if (t > 0.05f && t < len - 0.05f) s_Ts.Add(t);
        }

        // Where a lower block meets a higher one, the higher one's wall above the lower roof.
        private static void StepWalls(Job j)
        {
            if (j.Parts == null) return;
            Shape sh = j.Sh;
            foreach (RoofPart part in j.Parts)
            {
                if (part.Parent < 0) continue;
                RoofPart par = j.Parts[part.Parent];
                float from = part.Kind == RoofKind.Skillion ? part.High - 0.1f : part.Eave;
                if (par.Eave <= from + 0.05f) continue;
                Rect r = part.R, o = par.R;
                Vector2 a, b;
                switch (part.Side)
                {
                    case 0: a = new Vector2(r.xMin, Mathf.Max(r.yMin, o.yMin)); b = new Vector2(r.xMin, Mathf.Min(r.yMax, o.yMax)); break;
                    case 1: a = new Vector2(r.xMax, Mathf.Max(r.yMin, o.yMin)); b = new Vector2(r.xMax, Mathf.Min(r.yMax, o.yMax)); break;
                    case 2: a = new Vector2(Mathf.Max(r.xMin, o.xMin), r.yMin); b = new Vector2(Mathf.Min(r.xMax, o.xMax), r.yMin); break;
                    default: a = new Vector2(Mathf.Max(r.xMin, o.xMin), r.yMax); b = new Vector2(Mathf.Min(r.xMax, o.xMax), r.yMax); break;
                }
                Vector2 wa = sh.World(a), wb = sh.World(b);
                Vector2 d = wb - wa;
                float len = d.magnitude;
                if (len < 0.1f) continue;
                Vector2 c = (wa + wb) * 0.5f;
                j.Upper.Box(new Vector3(c.x, (from + par.Eave) * 0.5f, c.y), new Vector3(len + Thick, par.Eave - from, Thick),
                    -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, j.WallS);
            }
        }
        #endregion

        #region Pitched roofs
        private static void Roof(Job j)
        {
            Plan p = j.P;
            if (j.Parts == null)
            {
                if (p.Roof == RoofKind.Mansard) MansardRoof(j);
                else FlatRoof(j);
                return;
            }
            for (int k = 0; k < j.Parts.Count; k++) PartRoof(j, j.Parts[k]);
        }

        // A block's frame: x along its ridge, y across it.
        private struct Axes
        {
            public Vector2 X, Y;   // world directions
            public float X0, X1, Y0, Y1;
            public Vector2 O;
            public Vector3 P(float x, float y, float h)
            {
                Vector2 q = O + X * x + Y * y;
                return new Vector3(q.x, h, q.y);
            }
            public Vector3 Out(int side) => side == 0 ? To3(-X, 0f) : side == 1 ? To3(X, 0f) : side == 2 ? To3(-Y, 0f) : To3(Y, 0f);
        }

        private static Axes AxesOf(Shape sh, RoofPart part)
        {
            Rect r = part.R;
            return part.AlongU
                ? new Axes { X = sh.U, Y = sh.V, O = sh.O, X0 = r.xMin, X1 = r.xMax, Y0 = r.yMin, Y1 = r.yMax }
                : new Axes { X = sh.V, Y = sh.U, O = sh.O, X0 = r.yMin, X1 = r.yMax, Y0 = r.xMin, Y1 = r.xMax };
        }

        // The part's side (0 x-min end, 1 x-max end, 2 y-min, 3 y-max) its parent is on, or -1.
        private static int AttachedSide(RoofPart part)
        {
            if (part.Side < 0) return -1;
            return part.AlongU ? part.Side : part.Side < 2 ? part.Side + 2 : part.Side - 2;
        }

        private static void PartRoof(Job j, RoofPart part)
        {
            Axes ax = AxesOf(j.Sh, part);
            switch (part.Kind)
            {
                case RoofKind.Hip: case RoofKind.Pyramid: HipRoof(j, part, ax); break;
                case RoofKind.Skillion: LeanTo(j, part, ax); break;
                case RoofKind.Gambrel: GambrelRoof(j, part, ax); break;
                case RoofKind.Round: BarrelRoof(j, part, ax); break;
                default: GableRoof(j, part, ax); break;
            }
            j.Top = Mathf.Max(j.Top, part.Ridge);
        }

        private static void GableRoof(Job j, RoofPart part, Axes ax)
        {
            Plan p = j.P;
            int att = AttachedSide(part);
            float t = Mathf.Tan(part.Pitch * Mathf.Deg2Rad);
            float ov = p.Overhang, rake = Mathf.Min(ov, 0.35f);
            float x0 = ax.X0 - (att == 0 ? part.Ext : rake), x1 = ax.X1 + (att == 1 ? part.Ext : rake);
            float y0 = ax.Y0 - ov, y1 = ax.Y1 + ov, yc = (ax.Y0 + ax.Y1) * 0.5f, half = (ax.Y1 - ax.Y0) * 0.5f;
            float e = part.Eave, ridge = e + t * half, low = e - t * ov;
            part.Ridge = ridge;
            RaidMesh m = j.Upper;
            Vector3 up = Vector3.up;
            for (int s = 0; s < 2; s++)
            {
                float ye = s == 0 ? y0 : y1;
                Vector3 a = ax.P(x0, ye, low), b = ax.P(x1, ye, low), c = ax.P(x1, yc, ridge), d = ax.P(x0, yc, ridge);
                Vector3 n = SlopeNormal(ax.Out(s == 0 ? 2 : 3), part.Pitch);
                RoofFace4(j, m, a, b, c, d, n, low, ridge);
                if (!j.Lite)
                {
                    Eave(j, m, a, b, ax.Out(s == 0 ? 2 : 3), ax.P(x0, s == 0 ? ax.Y0 : ax.Y1, low), ax.P(x1, s == 0 ? ax.Y0 : ax.Y1, low), n, low, ridge);
                }
            }
            for (int end = 0; end < 2; end++)
            {
                if (att == end) continue;
                float x = end == 0 ? ax.X0 : ax.X1, xr = end == 0 ? x0 : x1;
                Vector3 o = ax.Out(end);
                Tri3(m, ax.P(x, ax.Y0, e), ax.P(x, ax.Y1, e), ax.P(x, yc, ridge), o, j.WallS);
                if (j.Lite) continue;
                Beam(m, ax.P(xr, y0, low) + o * 0.02f, ax.P(xr, yc, ridge) + o * 0.02f, 0.2f, 0.05f, o, j.TrimS);
                Beam(m, ax.P(xr, y1, low) + o * 0.02f, ax.P(xr, yc, ridge) + o * 0.02f, 0.2f, 0.05f, o, j.TrimS);
                GableVent(j, part, ax, end, e, ridge, yc);
            }
            if (!j.Lite) Beam(m, ax.P(x0, yc, ridge + 0.03f), ax.P(x1, yc, ridge + 0.03f), 0.3f, 0.08f, up, RidgeSurf(p));
        }

        private static void HipRoof(Job j, RoofPart part, Axes ax)
        {
            Plan p = j.P;
            int att = AttachedSide(part);
            float t = Mathf.Tan(part.Pitch * Mathf.Deg2Rad);
            float ov = p.Overhang;
            float x0 = ax.X0 - (att == 0 ? part.Ext : ov), x1 = ax.X1 + (att == 1 ? part.Ext : ov);
            float y0 = ax.Y0 - (att == 2 ? part.Ext : ov), y1 = ax.Y1 + (att == 3 ? part.Ext : ov);
            float low = part.Eave - t * ov;
            float wx = x1 - x0, wy = y1 - y0;
            RaidMesh m = j.Upper;
            Vector3 c00 = ax.P(x0, y0, low), c10 = ax.P(x1, y0, low), c11 = ax.P(x1, y1, low), c01 = ax.P(x0, y1, low);
            Vector3 r0, r1;
            float ridge;
            if (wx >= wy)
            {
                float h = wy * 0.5f, yc = (y0 + y1) * 0.5f;
                ridge = low + t * h;
                r0 = ax.P(x0 + h, yc, ridge);
                r1 = ax.P(x1 - h, yc, ridge);
                RoofPoly(j, m, c00, c10, r1, r0, SlopeNormal(ax.Out(2), part.Pitch), low, ridge);
                RoofPoly(j, m, c11, c01, r0, r1, SlopeNormal(ax.Out(3), part.Pitch), low, ridge);
                RoofTri(j, m, c01, c00, r0, SlopeNormal(ax.Out(0), part.Pitch), low, ridge);
                RoofTri(j, m, c10, c11, r1, SlopeNormal(ax.Out(1), part.Pitch), low, ridge);
            }
            else
            {
                float h = wx * 0.5f, xc = (x0 + x1) * 0.5f;
                ridge = low + t * h;
                r0 = ax.P(xc, y0 + h, ridge);
                r1 = ax.P(xc, y1 - h, ridge);
                RoofPoly(j, m, c01, c00, r0, r1, SlopeNormal(ax.Out(0), part.Pitch), low, ridge);
                RoofPoly(j, m, c10, c11, r1, r0, SlopeNormal(ax.Out(1), part.Pitch), low, ridge);
                RoofTri(j, m, c00, c10, r0, SlopeNormal(ax.Out(2), part.Pitch), low, ridge);
                RoofTri(j, m, c11, c01, r1, SlopeNormal(ax.Out(3), part.Pitch), low, ridge);
            }
            part.Ridge = ridge;
            if (j.Lite) return;
            Surf cap = RidgeSurf(p);
            Vector3 up = Vector3.up;
            if ((r1 - r0).sqrMagnitude > 0.01f) Beam(m, r0 + up * 0.03f, r1 + up * 0.03f, 0.3f, 0.08f, up, cap);
            Vector3 lift = up * 0.03f;
            if (att != 0 && att != 2) Beam(m, c00 + lift, r0 + lift, 0.24f, 0.07f, up, cap);
            if (att != 1 && att != 2) Beam(m, c10 + lift, (wx >= wy ? r1 : r0) + lift, 0.24f, 0.07f, up, cap);
            if (att != 1 && att != 3) Beam(m, c11 + lift, r1 + lift, 0.24f, 0.07f, up, cap);
            if (att != 0 && att != 3) Beam(m, c01 + lift, (wx >= wy ? r0 : r1) + lift, 0.24f, 0.07f, up, cap);
            for (int side = 0; side < 4; side++)
            {
                if (side == att) continue;
                Vector3 a, b, wa, wb;
                switch (side)
                {
                    case 0: a = c01; b = c00; wa = ax.P(ax.X0, y1 - ov + (att == 3 ? ov - part.Ext : 0f), low); wb = ax.P(ax.X0, y0 + ov, low); break;
                    case 1: a = c10; b = c11; wa = ax.P(ax.X1, y0 + ov, low); wb = ax.P(ax.X1, y1 - ov, low); break;
                    case 2: a = c00; b = c10; wa = ax.P(x0 + ov, ax.Y0, low); wb = ax.P(x1 - ov, ax.Y0, low); break;
                    default: a = c11; b = c01; wa = ax.P(x1 - ov, ax.Y1, low); wb = ax.P(x0 + ov, ax.Y1, low); break;
                }
                Vector3 n = SlopeNormal(ax.Out(side), part.Pitch);
                Eave(j, m, a, b, ax.Out(side), wa, wb, n, low, ridge);
            }
        }

        private static void LeanTo(Job j, RoofPart part, Axes ax)
        {
            Plan p = j.P;
            int att = AttachedSide(part);
            // High side: toward the parent, or the back (y max) of a free-standing one.
            int highSide = att == 2 || att == 3 ? att : 3, lowSide = highSide == 2 ? 3 : 2;
            float t = Mathf.Tan(part.Pitch * Mathf.Deg2Rad);
            float ov = Mathf.Min(p.Overhang, 0.3f);
            float yLow = lowSide == 2 ? ax.Y0 : ax.Y1, yHigh = highSide == 2 ? ax.Y0 : ax.Y1;
            float dirLow = lowSide == 2 ? -1f : 1f;
            float ext = att >= 0 ? part.Ext : 0.1f;
            float x0 = ax.X0 - (att == 0 ? part.Ext : ov), x1 = ax.X1 + (att == 1 ? part.Ext : ov);
            float hLow = part.Eave, hHigh = part.High > hLow ? part.High : hLow + t * Mathf.Abs(yHigh - yLow);
            float yLowOut = yLow + dirLow * ov, yHighOut = yHigh - dirLow * ext;
            float lowOut = hLow - t * ov, highOut = hHigh + t * ext;
            part.Ridge = highOut;
            RaidMesh m = j.Upper;
            Vector3 a = ax.P(x0, yLowOut, lowOut), b = ax.P(x1, yLowOut, lowOut), c = ax.P(x1, yHighOut, highOut), d = ax.P(x0, yHighOut, highOut);
            Vector3 n = SlopeNormal(ax.Out(lowSide), part.Pitch);
            RoofFace4(j, m, a, b, c, d, n, lowOut, highOut);
            for (int end = 0; end < 2; end++)
            {
                if (att == end) continue;
                float x = end == 0 ? ax.X0 : ax.X1;
                Tri3(m, ax.P(x, yLow, hLow), ax.P(x, yHigh, hLow), ax.P(x, yHigh, hHigh), ax.Out(end), j.WallS);
                if (!j.Lite)
                {
                    float xr = end == 0 ? x0 : x1;
                    Beam(m, ax.P(xr, yLowOut, lowOut) + ax.Out(end) * 0.02f, ax.P(xr, yHighOut, highOut) + ax.Out(end) * 0.02f, 0.18f, 0.05f, ax.Out(end), j.TrimS);
                }
            }
            if (att < 0)
                Quad4(m, ax.P(ax.X0, yHigh, hLow), ax.P(ax.X1, yHigh, hLow), ax.P(ax.X1, yHigh, hHigh), ax.P(ax.X0, yHigh, hHigh), ax.Out(highSide), j.WallS);
            if (!j.Lite) Eave(j, m, a, b, ax.Out(lowSide), ax.P(x0 + ov, yLow, lowOut), ax.P(x1 - ov, yLow, lowOut), n, lowOut, highOut);
        }

        private static void GambrelRoof(Job j, RoofPart part, Axes ax)
        {
            Plan p = j.P;
            int att = AttachedSide(part);
            const float Knee = 0.42f;
            float t1 = Mathf.Tan(62f * Mathf.Deg2Rad), t2 = Mathf.Tan(26f * Mathf.Deg2Rad);
            float half = (ax.Y1 - ax.Y0) * 0.5f, yc = (ax.Y0 + ax.Y1) * 0.5f, dk = half * Knee;
            float e = part.Eave, ek = e + t1 * dk, ridge = ek + t2 * (half - dk);
            float ov = 0.15f, rake = 0.25f, low = e - t1 * ov;
            float x0 = ax.X0 - (att == 0 ? part.Ext : rake), x1 = ax.X1 + (att == 1 ? part.Ext : rake);
            part.Ridge = ridge;
            RaidMesh m = j.Upper;
            for (int s = 0; s < 2; s++)
            {
                float sign = s == 0 ? -1f : 1f, wall = s == 0 ? ax.Y0 : ax.Y1;
                float yOut = wall + sign * ov, yKnee = wall - sign * dk;
                Vector3 o = ax.Out(s == 0 ? 2 : 3);
                Vector3 nLow = SlopeNormal(o, 62f), nUp = SlopeNormal(o, 26f);
                Vector3 a = ax.P(x0, yOut, low), b = ax.P(x1, yOut, low), kb = ax.P(x1, yKnee, ek), ka = ax.P(x0, yKnee, ek);
                RoofFace4(j, m, a, b, kb, ka, nLow, low, ek);
                RoofFace4(j, m, ka, kb, ax.P(x1, yc, ridge), ax.P(x0, yc, ridge), nUp, ek, ridge);
                if (!j.Lite) Eave(j, m, a, b, o, ax.P(x0, wall, low), ax.P(x1, wall, low), nLow, low, ek);
            }
            List<Vector3> pent = s_Pent;
            for (int end = 0; end < 2; end++)
            {
                if (att == end) continue;
                float x = end == 0 ? ax.X0 : ax.X1;
                pent.Clear();
                pent.Add(ax.P(x, ax.Y0, e));
                pent.Add(ax.P(x, ax.Y0 + dk, ek));
                pent.Add(ax.P(x, yc, ridge));
                pent.Add(ax.P(x, ax.Y1 - dk, ek));
                pent.Add(ax.P(x, ax.Y1, e));
                Poly(m, pent, ax.Out(end), j.WallS);
                if (!j.Lite) GableVent(j, part, ax, end, ek - 0.6f, ridge, yc);
            }
            if (!j.Lite) Beam(m, ax.P(x0, yc, ridge + 0.03f), ax.P(x1, yc, ridge + 0.03f), 0.3f, 0.08f, Vector3.up, RidgeSurf(p));
        }

        // A barrel roof: a hoop house, a Quonset hut, a trailer's curved top.
        private static void BarrelRoof(Job j, RoofPart part, Axes ax)
        {
            Plan p = j.P;
            int segs = j.Lite ? 5 : 8;
            float half = (ax.Y1 - ax.Y0) * 0.5f, yc = (ax.Y0 + ax.Y1) * 0.5f;
            float rise = p.Arch == Arch.Greenhouse ? half * 0.9f : p.Arch == Arch.Trailer ? 0.3f : half * 0.6f;
            float x0 = ax.X0 - 0.08f, x1 = ax.X1 + 0.08f, e = part.Eave;
            part.Ridge = e + rise;
            RaidMesh m = j.Upper;
            bool plastic = p.Arch == Arch.Greenhouse;
            Surf skin = plastic ? DecorPalette.Matte(new Color(0.84f, 0.87f, 0.86f)) : RoofCoverSurf(p, false);
            List<Vector3> arc = s_Arc;
            arc.Clear();
            for (int k = 0; k <= segs; k++)
            {
                float y = ax.Y0 + (ax.Y1 - ax.Y0) * k / segs;
                float q = (y - yc) / half;
                arc.Add(new Vector3(y, e + rise * Mathf.Sqrt(Mathf.Max(0f, 1f - q * q)), 0f));
            }
            for (int k = 0; k < segs; k++)
            {
                Vector3 pa = arc[k], pb = arc[k + 1];
                Vector3 a = ax.P(x0, pa.x, pa.y), b = ax.P(x1, pa.x, pa.y), c = ax.P(x1, pb.x, pb.y), d = ax.P(x0, pb.x, pb.y);
                Vector3 n = Vector3.Cross(b - a, d - a).normalized;
                if (n.y < 0f) n = -n;
                Quad4(m, a, b, c, d, n, skin);
                if (!j.Lite && n.y > 0.8f && p.Snow != SnowCover.Bare)
                    Slab(m, Scratch4(a, b, c, d), n, -1f, 99f, 0.07f);
            }
            List<Vector3> cap = s_Pent;
            for (int end = 0; end < 2; end++)
            {
                float x = end == 0 ? ax.X0 : ax.X1;
                cap.Clear();
                for (int k = 0; k <= segs; k++) cap.Add(ax.P(x, arc[k].x, arc[k].y));
                Poly(m, cap, ax.Out(end), plastic ? skin : j.WallS);
            }
            if (plastic && !j.Lite)
            {
                // The hoops.
                Surf steel = DecorPalette.Metal(new Color(0.55f, 0.56f, 0.57f));
                int hoops = Mathf.Max(2, Mathf.RoundToInt((ax.X1 - ax.X0) / 1.8f));
                for (int h = 0; h <= hoops; h++)
                {
                    float x = Mathf.Lerp(ax.X0, ax.X1, h / (float)hoops);
                    for (int k = 0; k < segs; k++)
                        Beam(m, ax.P(x, arc[k].x, arc[k].y + 0.02f), ax.P(x, arc[k + 1].x, arc[k + 1].y + 0.02f), 0.05f, 0.05f, Vector3.up, steel);
                }
            }
        }

        // A fast-food mansard: a steep shingled band round a flat roof, hanging past the walls.
        private static void MansardRoof(Job j)
        {
            Plan p = j.P;
            Shape sh = j.Sh;
            float e = p.Eave, top = e + 0.95f;
            Rect outer = Rect.MinMaxRect(-sh.HalfL - 0.45f, -sh.HalfW - 0.45f, sh.HalfL + 0.45f, sh.HalfW + 0.45f);
            Rect inner = Rect.MinMaxRect(-sh.HalfL + 0.15f, -sh.HalfW + 0.15f, sh.HalfL - 0.15f, sh.HalfW - 0.15f);
            float bottom = e - 0.35f;
            RaidMesh m = j.Upper;
            Surf band = RoofCoverSurf(p, false);
            Vector3 O(float u, float v, float h) => To3(sh.World(u, v), h);
            Vector3[] o = { O(outer.xMin, outer.yMin, bottom), O(outer.xMax, outer.yMin, bottom), O(outer.xMax, outer.yMax, bottom), O(outer.xMin, outer.yMax, bottom) };
            Vector3[] i = { O(inner.xMin, inner.yMin, top), O(inner.xMax, inner.yMin, top), O(inner.xMax, inner.yMax, top), O(inner.xMin, inner.yMax, top) };
            Vector3[] outs = { To3(-sh.V, 0f), To3(sh.U, 0f), To3(sh.V, 0f), To3(-sh.U, 0f) };
            for (int k = 0; k < 4; k++)
            {
                int k1 = (k + 1) % 4;
                Vector3 n = SlopeNormal(outs[k], 68f);
                Quad4(m, o[k], o[k1], i[k1], i[k], n, band);
                if (!j.Lite) Beam(m, o[k] - Vector3.up * 0.08f, o[k1] - Vector3.up * 0.08f, 0.05f, 0.18f, Vector3.up, j.TrimS);
            }
            Quad4(m, i[0], i[1], i[2], i[3], Vector3.up, p.Snow == SnowCover.Bare ? RoofCoverSurf(p, false) : RoofSnow);
            j.Top = Mathf.Max(j.Top, top);
        }
        #endregion

        #region Faces, snow, eaves
        private static Vector3 SlopeNormal(Vector3 outward, float pitch)
        {
            float a = pitch * Mathf.Deg2Rad;
            return (outward * Mathf.Sin(a) + Vector3.up * Mathf.Cos(a)).normalized;
        }

        private static Surf RoofCoverSurf(Plan p, bool fitted) => fitted ? RoofSurfFitted(p.Covering, p.CoverTone) : RoofSurf(p.Covering, p.CoverTone);

        private static Surf RidgeSurf(Plan p)
        {
            Color c = TonesOf(p.Covering)[Mathf.Clamp(p.CoverTone, 0, TonesOf(p.Covering).Length - 1)];
            return p.Covering == RoofCover.Metal ? DecorPalette.Metal(c * 0.9f) : DecorPalette.Matte(c * 0.8f);
        }

        private static void Tri3(RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 normal, in Surf s)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, normal) >= 0f) m.Tri(a, b, c, s);
            else m.Tri(a, c, b, s);
        }

        // A rectangular roof face (a, b along the eave, c, d along the top), its covering laid along the eave, and its snow.
        private static void RoofFace4(Job j, RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, float low, float high)
        {
            Plan p = j.P;
            if (j.Lite)
            {
                Quad4(m, a, b, c, d, n, LiteRoof(j, n));
                return;
            }
            Vector3 center = (a + b + c + d) * 0.25f;
            Vector3 upHalf = (d - a) * 0.5f;
            float width = Vector3.Distance(a, b);
            Vector3 right = Vector3.Cross(n, upHalf).normalized * (width * 0.5f);
            float period = RoofPeriod(p.Covering);
            m.QuadUV(center, right, upHalf, RoofCoverSurf(p, true), new Rect(0f, 0f, width / period, upHalf.magnitude * 2f / period));
            SnowOn(j, m, Scratch4(a, b, c, d), n, low, high);
        }

        private static void RoofPoly(Job j, RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, float low, float high)
        {
            List<Vector3> face = Scratch4(a, b, c, d);
            if ((c - d).sqrMagnitude < 1e-4f) face.RemoveAt(3);
            Poly(m, face, n, j.Lite ? LiteRoof(j, n) : RoofCoverSurf(j.P, false));
            if (!j.Lite) SnowOn(j, m, Scratch4(a, b, c, d), n, low, high);
        }

        private static void RoofTri(Job j, RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 n, float low, float high)
        {
            Tri3(m, a, b, c, n, j.Lite ? LiteRoof(j, n) : RoofCoverSurf(j.P, false));
            if (j.Lite) return;
            List<Vector3> face = Scratch4(a, b, c, c);
            face.RemoveAt(3);
            SnowOn(j, m, face, n, low, high);
        }

        // A Lite roof's one surface: its snow, or its covering where the snow's gone.
        private static Surf LiteRoof(Job j, Vector3 n)
        {
            Plan p = j.P;
            bool snow = p.Snow == SnowCover.Full || (p.Snow == SnowCover.Partial && n.z > -0.3f);
            return snow ? RoofSnow : RoofCoverSurf(p, false);
        }

        private static readonly List<Vector3> s_Face = new List<Vector3>(8), s_Pent = new List<Vector3>(16), s_Arc = new List<Vector3>(16);

        private static List<Vector3> Scratch4(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            s_Face.Clear();
            s_Face.Add(a);
            s_Face.Add(b);
            s_Face.Add(c);
            s_Face.Add(d);
            return s_Face;
        }

        // The band of a face the snow covers (fractions of its height from the eave), by how snowy the roof is, which way
        // the face looks (south-facing faces lose it first) and how steep it is.
        private static bool SnowBand(Job j, Vector3 n, out float a, out float b)
        {
            Plan p = j.P;
            bool south = n.z < -0.35f, north = n.z > 0.35f;
            bool steep = n.y < 0.62f;
            int mode = MapStyle.Pick(j.F.Data.id, SaltSnow, 3);
            a = 0.07f;
            b = 0.94f;
            switch (p.Snow)
            {
                case SnowCover.Full:
                    if (steep) a = 0.3f;
                    return true;
                case SnowCover.Partial:
                    if (mode == 0) { a = 0.05f; b = south ? 0.3f : 0.6f; }           // melted from the warm ridge down
                    else if (mode == 1) { a = south ? 0.62f : 0.42f; b = 0.95f; }      // slid off the eaves
                    else if (south) { a = 0.5f; b = 0.9f; }                            // the sunny side going
                    if (steep) a = Mathf.Max(a, 0.35f);
                    return b - a > 0.08f;
                default:
                    if (!north) return false;
                    a = 0.06f;
                    b = 0.3f;
                    return true;
            }
        }

        private static void SnowOn(Job j, RaidMesh m, List<Vector3> face, Vector3 n, float low, float high)
        {
            if (!SnowBand(j, n, out float a, out float b)) return;
            float span = high - low;
            Slab(m, face, n, low + span * a, low + span * b, 0.09f);
        }

        private static readonly List<Vector3> s_Clip = new List<Vector3>(10), s_ClipB = new List<Vector3>(10);

        // A slab of snow on a planar face, over the part of it between heights y0 and y1: its top and its edges.
        private static void Slab(RaidMesh m, List<Vector3> face, Vector3 n, float y0, float y1, float thick)
        {
            ClipY(face, s_ClipB, y0, true);
            ClipY(s_ClipB, s_Clip, y1, false);
            int k = s_Clip.Count;
            if (k < 3) return;
            Vector3 off = n * thick;
            Surf snow = RoofSnow;
            Vector3 n0 = Vector3.Cross(s_Clip[1] - s_Clip[0], s_Clip[2] - s_Clip[0]);
            bool flip = Vector3.Dot(n0, n) < 0f;
            for (int i = 1; i + 1 < k; i++)
            {
                if (flip) m.Tri(s_Clip[0] + off, s_Clip[i + 1] + off, s_Clip[i] + off, snow);
                else m.Tri(s_Clip[0] + off, s_Clip[i] + off, s_Clip[i + 1] + off, snow);
            }
            Vector3 centre = Vector3.zero;
            for (int i = 0; i < k; i++) centre += s_Clip[i];
            centre /= k;
            for (int i = 0; i < k; i++)
            {
                Vector3 p0 = s_Clip[i], p1 = s_Clip[(i + 1) % k];
                if ((p1 - p0).sqrMagnitude < 1e-4f) continue;
                Vector3 mid = (p0 + p1) * 0.5f;
                Vector3 outward = mid - centre;
                outward -= n * Vector3.Dot(outward, n);
                Quad4(m, p0, p1, p1 + off, p0 + off, outward, snow);
            }
        }

        // The part of a convex polygon above (or below) height y.
        private static void ClipY(List<Vector3> src, List<Vector3> dst, float y, bool keepAbove)
        {
            dst.Clear();
            int n = src.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = src[(i + n - 1) % n], b = src[i];
                bool ina = keepAbove ? a.y >= y : a.y <= y, inb = keepAbove ? b.y >= y : b.y <= y;
                if (inb)
                {
                    if (!ina) dst.Add(Vector3.Lerp(a, b, (y - a.y) / (b.y - a.y)));
                    dst.Add(b);
                }
                else if (ina) dst.Add(Vector3.Lerp(a, b, (y - a.y) / (b.y - a.y)));
            }
        }

        // Along an eave from a to b: the fascia board, the soffit back to the wall (wa to wb) and, on a heated building
        // whose snow reaches the eave, icicles.
        private static void Eave(Job j, RaidMesh m, Vector3 a, Vector3 b, Vector3 outward, Vector3 wa, Vector3 wb, Vector3 n, float low, float high)
        {
            Beam(m, a - Vector3.up * 0.09f + outward * 0.01f, b - Vector3.up * 0.09f + outward * 0.01f, 0.05f, 0.2f, Vector3.up, j.TrimS);
            Vector3 drop = Vector3.up * 0.18f;
            Quad4(m, a - drop, b - drop, wb - drop, wa - drop, Vector3.down, j.TrimS);
            if (!j.P.Icicles || !SnowBand(j, n, out float sa, out _) || sa > 0.15f) return;
            Icicles(j, m, a - Vector3.up * 0.16f, b - Vector3.up * 0.16f, outward);
        }

        private static void Icicles(Job j, RaidMesh m, Vector3 a, Vector3 b, Vector3 outward)
        {
            float len = Vector3.Distance(a, b);
            if (len < 0.5f) return;
            var dice = new Dice(j.Id ^ Mathf.RoundToInt(a.x * 10f + a.z * 7f), SaltIcicles);
            Surf ice = DecorPalette.Gloss(new Color(0.80f, 0.88f, 0.95f));
            Vector3 along = (b - a) / len;
            int count = Mathf.FloorToInt(len / 0.3f);
            for (int k = 0; k < count; k++)
            {
                if (dice.Chance(0.4f)) continue;
                float s = (k + 0.5f) * len / count + dice.Range(-0.08f, 0.08f);
                float r = dice.Next();
                float l = 0.1f + r * r * 0.7f, w = dice.Range(0.04f, 0.09f);
                Vector3 top = a + along * s;
                Vector3 l0 = top - along * w, l1 = top + along * w, tip = top - Vector3.up * l + outward * 0.01f;
                Tri3(m, l0, l1, tip, outward, ice);
                Tri3(m, l0, l1, tip, -outward, ice);
            }
        }

        // A louvred vent or a small window up in an outside gable, or a barn's hay door.
        private static void GableVent(Job j, RoofPart part, Axes ax, int end, float e, float ridge, float yc)
        {
            if (j.P.Roof == RoofKind.Skillion || ridge - e < 1.6f) return;
            float x = end == 0 ? ax.X0 : ax.X1;
            Vector3 o = ax.Out(end);
            // Only on an outside wall: something must stand beyond this end's wall line.
            Vector2 probe = (Vector2)(ax.O + ax.X * (x + (end == 0 ? -0.5f : 0.5f)) + ax.Y * yc);
            if (MapGeometry.Contains(j.F.Pts, probe)) return;
            Vector3 c = ax.P(x, yc, e + (ridge - e) * 0.38f) + o * 0.02f;
            bool barn = j.P.Arch == Arch.Barn || j.P.Arch == Arch.TobaccoBarn;
            float w = barn ? 1.3f : 0.7f, h = barn ? 1.5f : Mathf.Min(0.9f, (ridge - e) * 0.4f);
            if (barn) c.y = e + h * 0.5f + 0.3f;
            j.Upper.Upright(c, o, w + 0.14f, h + 0.14f, j.TrimS);
            j.Upper.Upright(c + o * 0.005f, o, w, h, barn ? DecorPalette.Matte(TonesOf(j.P.Wall)[j.P.WallTone] * 0.7f) : DecorPalette.Matte(new Color(0.16f, 0.16f, 0.17f)));
            if (!barn)
                for (int k = 1; k < 4; k++)
                    j.Upper.Upright(c + o * 0.01f + Vector3.up * (h * (k / 4f - 0.5f)), o, w, 0.035f, j.TrimS);
        }
        #endregion
    }
}
