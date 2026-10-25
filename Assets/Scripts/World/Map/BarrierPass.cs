using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;
using static CloneZ.StreetscapePass;

namespace CloneZ
{
    /// <summary>
    /// The mapped barriers (format 2), as Kentucky builds them: plank horse fences, white or creosote-black, and woven-wire
    /// or barbed-wire farm fences on the long runs out in the country; chain link, board privacy fences and white pickets
    /// round the yards; chain link topped with barbed wire round the substations and pump stations; dry-stone rock fences
    /// (the Bluegrass's limestone walls with their coping of upright stones) and brick or block garden walls; hedges; W-beam
    /// guard rails on their posts; concrete retaining walls. Every style is the barrier's own (from its id, name and length),
    /// so a fence looks the same in every streamed piece it crosses; posts stand on a world-anchored lattice. Snow lies on
    /// every rail and coping. Fences leave a gap (and a gate, swung open) at a mapped gate, and where the mapping runs one
    /// across a carriageway or into a building. Each straight run gets a collider; tree rows are the vegetation's. Past a
    /// fixed map's edge and on a Lite piece, each stretch is one plain shape seen from afar: no posts, gates or colliders.
    /// The textures are the barriers' own, small, made once a session.
    /// </summary>
    internal static class BarrierPass
    {
        private const int SaltStyle = 0xBA77, SaltShade = 0xBA78;

        private enum Style { Plank, PlankBlack, WovenWire, Barbed, ChainLink, Security, Privacy, Picket, DryStone, Brick, Block, Hedge, GuardRail, Retaining }

        private static readonly Color WhitePaint = new Color(0.88f, 0.88f, 0.86f);
        private static readonly Color Creosote = new Color(0.09f, 0.08f, 0.08f);
        private static readonly Color FenceWood = new Color(0.46f, 0.36f, 0.26f);
        private static readonly Color HedgeGreen = new Color(0.17f, 0.24f, 0.14f);

        /// <summary>A full site's barriers: in full inside the square, plain past a fixed map's edge.</summary>
        internal static IEnumerable<string> Build(Ctx ctx)
        {
            Site site = ctx.Site;
            if (site.Area.Barriers.Count == 0) yield break;
            float half = ctx.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            var gates = new List<Vector2>();
            foreach (Node n in site.Area.Nodes) if (n.Data.kind == "gate") gates.Add(n.At);
            var drives = new Drives();
            foreach (RoadPass.DrivewaySpot d in RoadPass.Driveways(site))
                if (d.Dir.sqrMagnitude > 0.5f) drives.Add(d.At, d.At + d.Dir * (d.Length + 1.5f));
            var runs = new Runs();
            s_Runs.Remove(site);
            s_Runs.Add(site, runs);
            foreach (Line l in site.Area.Barriers)
            {
                if (l.Data.kind == "tree_row" || l.Pts.Count < 2) continue;
                Style style = StyleOf(l.Data);
                // A texture made for the first time (once a session) gets a slice of its own.
                if (Warm(style, false) && site.StepDue) yield return "fences and walls";
                foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, square))
                    foreach (string step in Draw(ctx, l.Data, style, piece, gates, drives, runs)) yield return step;
                if (!site.Chunked)
                {
                    if (Warm(style, true) && site.StepDue) yield return "fences and walls";
                    foreach (List<Vector2> piece in Outside(l.Pts, half, ctx.Reach)) Plain(ctx, l.Data, style, piece);
                }
                if (site.StepDue) yield return "fences and walls";
            }
        }

        /// <summary>A Lite site's barriers: each straight stretch one plain shape.</summary>
        internal static IEnumerable<string> Lite(Ctx ctx)
        {
            Site site = ctx.Site;
            if (site.Area.Barriers.Count == 0) yield break;
            float half = ctx.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Line l in site.Area.Barriers)
            {
                if (l.Data.kind == "tree_row" || l.Pts.Count < 2) continue;
                Style style = StyleOf(l.Data);
                if (Warm(style, true) && site.StepDue) yield return "fences and walls";
                foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, square)) Plain(ctx, l.Data, style, piece);
                if (site.StepDue) yield return "fences and walls";
            }
        }

        // Makes the texture a style is drawn with (in full, or plain) if it isn't made yet; true when it made one.
        private static bool Warm(Style style, bool plain)
        {
            switch (style)
            {
                case Style.Plank: return plain && s_PlankMat == null && PlankPanel.Valid;
                case Style.PlankBlack: return plain && s_BlackMat == null && BlackPanel.Valid;
                case Style.WovenWire: return s_FieldMat == null && FieldFence.Valid;
                case Style.Barbed: return plain && s_FieldMat == null && FieldFence.Valid;
                case Style.ChainLink: case Style.Security: return s_ChainMat == null && ChainLink.Valid;
                case Style.Privacy: return s_BoardTex == null && Boards.Valid;
                case Style.Picket: return s_PicketMat == null && Picket.Valid;
                case Style.DryStone: return s_StoneTex == null && DryStone.Valid;
                case Style.Brick: return s_BrickTex == null && Brick.Valid;
                case Style.Block: return s_BlockTex == null && Block.Valid;
                default: return false;
            }
        }

        // The pieces of a line past the playable square, out to the reach: the four bands round the square.
        private static IEnumerable<List<Vector2>> Outside(List<Vector2> pts, float half, float reach)
        {
            if (reach <= half + 0.01f) yield break;
            var bands = new[]
            {
                Rect.MinMaxRect(-reach, -reach, -half, reach), Rect.MinMaxRect(half, -reach, reach, reach),
                Rect.MinMaxRect(-half, half, half, reach), Rect.MinMaxRect(-half, -reach, half, -half),
            };
            foreach (Rect band in bands)
                foreach (List<Vector2> piece in MapGeometry.ClipLine(pts, band))
                    yield return piece;
        }

        // The barrier's own look, from what's mapped about it (its id, name and length in the region's data).
        private static Style StyleOf(MapLine data)
        {
            float u = MapStyle.Unit(data.id, SaltStyle);
            float len = Length(data.pts);
            string name = data.name ?? "";
            switch (data.kind)
            {
                case "wall":
                    if (len >= 60f) return u < 0.72f ? Style.DryStone : Style.Brick;
                    return u < 0.55f ? Style.Brick : u < 0.8f ? Style.DryStone : Style.Block;
                case "hedge": return Style.Hedge;
                case "guard_rail": return Style.GuardRail;
                case "retaining_wall": return Style.Retaining;
            }
            if (Has(name, "substation", "pump", "station", "plant", "tank", "treatment", "water", "sewer", "jail", "prison", "tower"))
                return Style.Security;
            if (len >= 150f) return u < 0.25f ? Style.Plank : u < 0.42f ? Style.PlankBlack : u < 0.78f ? Style.WovenWire : Style.Barbed;
            if (len >= 40f)
                return u < 0.3f ? Style.ChainLink : u < 0.45f ? Style.Plank : u < 0.7f ? Style.Privacy : u < 0.85f ? Style.WovenWire : Style.Picket;
            return u < 0.4f ? Style.Privacy : u < 0.7f ? Style.ChainLink : Style.Picket;
        }

        private static float Length(int[] pts)
        {
            if (pts == null) return 0f;
            float len = 0f;
            for (int i = 2; i + 1 < pts.Length; i += 2)
                len += Mathf.Sqrt((float)(pts[i] - pts[i - 2]) * (pts[i] - pts[i - 2]) + (float)(pts[i + 1] - pts[i - 1]) * (pts[i + 1] - pts[i - 1])) * 0.1f;
            return len;
        }

        private static bool Has(string s, params string[] words)
        {
            foreach (string w in words)
                if (s.IndexOf(w, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static float Height(Style s)
        {
            switch (s)
            {
                case Style.Plank: case Style.PlankBlack: return 1.35f;
                case Style.WovenWire: return 1.2f;
                case Style.Barbed: return 1.2f;
                case Style.ChainLink: return 1.2f;
                case Style.Security: return 2.1f;
                case Style.Privacy: return 1.8f;
                case Style.Picket: return 1.0f;
                case Style.DryStone: return 1.15f;
                case Style.Brick: return 1.5f;
                case Style.Block: return 1.6f;
                case Style.Hedge: return 1.5f;
                case Style.GuardRail: return 0.75f;
                default: return 1.1f;
            }
        }

        private static float Thickness(Style s)
        {
            switch (s)
            {
                case Style.DryStone: return 0.7f;
                case Style.Brick: case Style.Block: return 0.3f;
                case Style.Hedge: return 1.0f;
                case Style.Retaining: return 0.4f;
                case Style.GuardRail: return 0.35f;
                default: return 0.12f;
            }
        }

        private static float PostSpacing(Style s)
        {
            switch (s)
            {
                case Style.WovenWire: case Style.Barbed: return 3.6f;
                case Style.ChainLink: case Style.Security: return 3f;
                case Style.GuardRail: return 1.9f;
                default: return 2.4f;
            }
        }

        #region In full
        // One piece of a barrier inside the square: its runs between the gates, the carriageways and the buildings, each
        // straight stretch drawn and given a collider, posts on the lattice and at the corners. Yields part way along a
        // long piece when the step has had its time (a farm's fence may run for kilometres).
        private static IEnumerable<string> Draw(Ctx ctx, MapLine data, Style style, List<Vector2> pts, List<Vector2> gates, Drives drives, Runs runs)
        {
            Site site = ctx.Site;
            float h = Height(style), thick = Thickness(style);
            bool fence = data.kind == "fence";
            var cuts = new List<Vector2>();   // (from, to) along the piece, cut out for gates
            MapGeometry.Extent(pts, out float x0, out float z0, out float x1, out float z1);
            if (fence)
                foreach (Vector2 g in gates)
                {
                    if (g.x < x0 - 0.8f || g.x > x1 + 0.8f || g.y < z0 - 0.8f || g.y > z1 + 0.8f) continue;
                    float d = Nearest(pts, g, out Vector2 on, out _);
                    if (d > 0.8f) continue;
                    float t = ArcOf(pts, on);
                    float w = style == Style.Picket || style == Style.Privacy || style == Style.ChainLink ? 0.6f : 2.2f;
                    cuts.Add(new Vector2(t - w, t + w));
                    Gate(ctx, pts, t, w, style);
                }
            float arc = 0f;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.05f) continue;
                Vector2 d = (b - a) / len;
                // The stretch's runs between the cuts.
                float s = 0f;
                while (s < len - 0.05f)
                {
                    bool moved = true;
                    while (moved)
                    {
                        moved = false;
                        foreach (Vector2 c in cuts)
                            if (c.x - arc <= s && c.y - arc > s) { s = c.y - arc; moved = true; }
                    }
                    if (s >= len - 0.05f) break;
                    float e = len;
                    foreach (Vector2 c in cuts)
                    {
                        float c0 = c.x - arc;
                        if (c0 > s && c0 < e) e = c0;
                    }
                    if (e - s > 0.05f)
                        foreach (string step in Spans(ctx, data, style, a, d, s, e, h, thick, i > 0 && s < 0.01f, drives, runs))
                            yield return step;
                    s = e;
                }
                arc += len;
            }
            site.TakeLine(pts, thick * 0.5f + 0.35f);
        }

        // The roads' driveways (from the kerb to the house), by 8 m cell: a fence leaves them open.
        private sealed class Drives
        {
            private const float Cell = 8f, Reach = 1.9f;
            private readonly List<(Vector2 a, Vector2 b)> _segs = new List<(Vector2 a, Vector2 b)>();
            private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();

            /// <summary>Adds a driveway segment from a to b.</summary>
            public void Add(Vector2 a, Vector2 b)
            {
                int i = _segs.Count;
                _segs.Add((a, b));
                int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - Reach) / Cell), x1 = Mathf.FloorToInt((Mathf.Max(a.x, b.x) + Reach) / Cell);
                int z0 = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - Reach) / Cell), z1 = Mathf.FloorToInt((Mathf.Max(a.y, b.y) + Reach) / Cell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        long key = Key(x, z);
                        if (!_cells.TryGetValue(key, out List<int> list)) _cells[key] = list = new List<int>(2);
                        list.Add(i);
                    }
            }

            // Within reach of a driveway's line.
            /// <summary>Checks whether point p lies within reach of any driveway segment.</summary>
            public bool On(Vector2 p)
            {
                if (!_cells.TryGetValue(Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell)), out List<int> list)) return false;
                foreach (int i in list)
                    if (MapGeometry.DistanceToSegment(p, _segs[i].a, _segs[i].b) < Reach) return true;
                return false;
            }

            private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
        }

        // The stretch from s to e along a + d * t, less where it would stand in a building, cross a carriageway as the
        // roads drew it or a driveway (a metre at a time); guard rails and retaining walls belong at the road's edge and
        // may touch it. Yields every 16 m or so once the step has had its time.
        private static IEnumerable<string> Spans(Ctx ctx, MapLine data, Style style, Vector2 a, Vector2 d, float s, float e, float h, float thick,
            bool corner, Drives drives, Runs runs)
        {
            float clear = style == Style.GuardRail || style == Style.Retaining ? -0.3f : 0.25f;
            float start = -1f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(e - s));
            float step = (e - s) / steps;
            for (int k = 0; k <= steps; k++)
            {
                bool open = false;
                if (k < steps)
                {
                    Vector2 mid = a + d * (s + (k + 0.5f) * step);
                    open = ctx.Roads.Clearance(mid, 1.5f) >= clear && !ctx.Blocks.Contains(mid, 0f) && !drives.On(mid);
                }
                float t = s + k * step;
                if (open && start < 0f) start = t;
                else if (!open && start >= 0f)
                {
                    if (t - start > 0.3f)
                    {
                        Run(ctx, data, style, a + d * start, a + d * t, d, h, thick, corner && start <= s + 0.01f);
                        runs.Add(a + d * start, a + d * t, Mathf.Max(thick, 0.15f) * 0.5f);
                    }
                    start = -1f;
                }
                if ((k & 15) == 15 && ctx.Site.StepDue) yield return "fences and walls";
            }
        }

        #region For the passes after this one
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Site, Runs> s_Runs =
            new System.Runtime.CompilerServices.ConditionalWeakTable<Site, Runs>();

        /// <summary>True when a barrier this pass drew in full on <paramref name="site"/> (a fence, wall, hedge, guard rail
        /// or retaining wall run, less the gaps it left at gates, carriageways, buildings and the roads' driveways) crosses
        /// the segment from <paramref name="a"/> to <paramref name="b"/> or comes within <paramref name="pad"/> of it. For
        /// the passes after this one that lay something along the ground (a driveway's strip, a path) and shouldn't run it
        /// through a fence. False before this pass ran, and on Lite pieces (whose barriers are plain shapes with no
        /// colliders).</summary>
        internal static bool Crosses(Site site, Vector2 a, Vector2 b, float pad = 0.3f)
            => s_Runs.TryGetValue(site, out Runs runs) && runs.Crosses(a, b, pad);

        // The runs drawn in full on a site, by 16 m cell.
        private sealed class Runs
        {
            private const float Cell = 16f;
            private readonly List<(Vector2 a, Vector2 b, float half)> _runs = new List<(Vector2 a, Vector2 b, float half)>();
            private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();

            /// <summary>Adds a run segment from a to b with half‑width padding.</summary>
            public void Add(Vector2 a, Vector2 b, float half)
            {
                int i = _runs.Count;
                _runs.Add((a, b, half));
                int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - half) / Cell), x1 = Mathf.FloorToInt((Mathf.Max(a.x, b.x) + half) / Cell);
                int z0 = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - half) / Cell), z1 = Mathf.FloorToInt((Mathf.Max(a.y, b.y) + half) / Cell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        long key = Key(x, z);
                        if (!_cells.TryGetValue(key, out List<int> list)) _cells[key] = list = new List<int>(4);
                        list.Add(i);
                    }
            }

            /// <summary>Tests whether the segment a‑b (with optional padding) crosses any recorded runs.</summary>
            public bool Crosses(Vector2 a, Vector2 b, float pad)
            {
                pad = Mathf.Max(0f, pad);
                int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - pad) / Cell), x1 = Mathf.FloorToInt((Mathf.Max(a.x, b.x) + pad) / Cell);
                int z0 = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - pad) / Cell), z1 = Mathf.FloorToInt((Mathf.Max(a.y, b.y) + pad) / Cell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!_cells.TryGetValue(Key(x, z), out List<int> list)) continue;
                        foreach (int i in list)
                        {
                            (Vector2 c, Vector2 d, float half) = _runs[i];
                            if (Gap(a, b, c, d) < pad + half) return true;
                        }
                    }
                return false;
            }

            private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

            // How far apart two segments come (0 where they cross).
            private static float Gap(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                float d1 = Cross(b - a, c - a), d2 = Cross(b - a, d - a), d3 = Cross(d - c, a - c), d4 = Cross(d - c, b - c);
                if (((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f))) return 0f;
                return Mathf.Min(Mathf.Min(MapGeometry.DistanceToSegment(a, c, d), MapGeometry.DistanceToSegment(b, c, d)),
                    Mathf.Min(MapGeometry.DistanceToSegment(c, a, b), MapGeometry.DistanceToSegment(d, a, b)));
            }

            private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
        }
        #endregion

        private static float ArcOf(List<Vector2> pts, Vector2 p)
        {
            float arc = 0f, best = float.MaxValue, at = 0f;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a;
                float len = ab.magnitude;
                if (len < 1e-4f) continue;
                float t = Mathf.Clamp(Vector2.Dot(p - a, ab) / (len * len), 0f, 1f);
                float dist = (a + ab * t - p).sqrMagnitude;
                if (dist < best) { best = dist; at = arc + t * len; }
                arc += len;
            }
            return at;
        }

        // One straight run from a to b.
        private static void Run(Ctx ctx, MapLine data, Style style, Vector2 a, Vector2 b, Vector2 d, float h, float thick, bool corner)
        {
            float len = Vector2.Distance(a, b);
            Vector2 mid = (a + b) * 0.5f;
            float yaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            RaidMesh m = ctx.Low(mid);
            long shadeKey = data.id;
            switch (style)
            {
                case Style.Plank:
                case Style.PlankBlack:
                {
                    Surf paint = Matte(style == Style.Plank ? WhitePaint : Creosote);
                    for (int k = 0; k < 4; k++)
                    {
                        float y = 0.35f + k * 0.32f;
                        m.Box(To3(mid, y), new Vector3(len, 0.14f, 0.035f), yaw, paint);
                    }
                    m.Box(To3(mid, 1.35f + 0.015f), new Vector3(len, 0.03f, 0.04f), yaw, SnowSurf);
                    Posts(ctx, a, b, d, style, 1.42f, paint, corner);
                    break;
                }
                case Style.WovenWire:
                case Style.Barbed:
                {
                    Vector3 n = To3(Left(d), 0f);
                    if (style == Style.WovenWire)
                    {
                        m.Upright(To3(mid, 0.6f), n, len, 1.2f, FieldFence);
                        m.Box(To3(mid, 1.33f), new Vector3(len, 0.008f, 0.008f), yaw, DarkMetal);
                    }
                    else
                        for (int k = 0; k < 4; k++) m.Box(To3(mid, 0.4f + k * 0.26f), new Vector3(len, 0.008f, 0.008f), yaw, DarkMetal);
                    Posts(ctx, a, b, d, style, 1.32f, Matte(FenceWood), corner);
                    break;
                }
                case Style.ChainLink:
                case Style.Security:
                {
                    Vector3 n = To3(Left(d), 0f);
                    m.Upright(To3(mid, h * 0.5f), n, len, h, ChainLink);
                    m.Box(To3(mid, h), new Vector3(len, 0.045f, 0.045f), yaw, Galvanised);
                    m.Box(To3(mid, h + 0.03f), new Vector3(len, 0.015f, 0.04f), yaw, SnowSurf);
                    if (style == Style.Security)
                        for (int k = 0; k < 3; k++)
                            m.Box(To3(mid, h + 0.1f + k * 0.12f) + n * (0.08f + k * 0.08f), new Vector3(len, 0.008f, 0.008f), yaw, DarkMetal);
                    Posts(ctx, a, b, d, style, h + 0.05f, Galvanised, corner);
                    break;
                }
                case Style.Privacy:
                {
                    Color wood = Shade(FenceWood, shadeKey);
                    m.Box(To3(mid, 0.95f), new Vector3(len, 1.7f, 0.03f), yaw, Boards);
                    m.Box(To3(mid, 1.82f), new Vector3(len, 0.05f, 0.1f), yaw, Matte(wood * 0.85f));
                    m.Box(To3(mid, 1.86f), new Vector3(len, 0.03f, 0.09f), yaw, SnowSurf);
                    Posts(ctx, a, b, d, style, 1.85f, Matte(wood * 0.8f), corner);
                    break;
                }
                case Style.Picket:
                {
                    Vector3 n = To3(Left(d), 0f);
                    m.Upright(To3(mid, 0.5f), n, len, 1.0f, Picket);
                    Posts(ctx, a, b, d, style, 1.05f, Matte(WhitePaint), corner);
                    break;
                }
                case Style.DryStone:
                {
                    Surf stone = DryStone;
                    m.Box(To3(mid, 0.3f), new Vector3(len + 0.1f, 0.6f, 0.72f), yaw, stone);
                    m.Box(To3(mid, 0.8f), new Vector3(len + 0.1f, 0.4f, 0.55f), yaw, stone);
                    // The coping: upright stones along the top, then the snow on them.
                    m.Box(To3(mid, 1.05f), new Vector3(len + 0.05f, 0.18f, 0.46f), yaw, Matte(new Color(0.48f, 0.47f, 0.44f)));
                    m.Box(To3(mid, 1.17f), new Vector3(len + 0.05f, 0.07f, 0.48f), yaw, SnowSurf);
                    break;
                }
                case Style.Brick:
                case Style.Block:
                {
                    Surf wall = style == Style.Brick ? Brick : Block;
                    m.Box(To3(mid, h * 0.5f), new Vector3(len + 0.3f, h, 0.3f), yaw, wall);
                    m.Box(To3(mid, h + 0.04f), new Vector3(len + 0.34f, 0.08f, 0.38f), yaw, Concrete);
                    m.Box(To3(mid, h + 0.1f), new Vector3(len + 0.3f, 0.05f, 0.36f), yaw, SnowSurf);
                    break;
                }
                case Style.Hedge:
                {
                    // A clipped hedge: a body, a rounded crown in lumps a few metres long (each a little higher or lower),
                    // snow on each lump.
                    Surf green = Matte(Shade(HedgeGreen, shadeKey));
                    m.Box(To3(mid, 0.66f), new Vector3(len + 0.4f, 1.32f, 0.95f), yaw, green);
                    int lumps = Mathf.Max(1, Mathf.RoundToInt(len / 3f));
                    float piece = (len + 0.3f) / lumps;
                    for (int k = 0; k < lumps; k++)
                    {
                        Vector2 p = Vector2.Lerp(a, b, (k + 0.5f) / lumps);
                        float top = 1.42f + 0.12f * (MapStyle.Unit(ctx.KeyOf(p), SaltShade + 1) - 0.5f);
                        m.Box(To3(p, (1.32f + top) * 0.5f), new Vector3(piece, top - 1.32f + 0.02f, 0.8f), yaw, green);
                        m.Box(To3(p, top + 0.03f), new Vector3(piece - 0.1f, 0.06f, 0.62f), yaw, SnowSurf);
                    }
                    break;
                }
                case Style.GuardRail:
                {
                    // The W-beam: two flutes, on blockouts off its posts.
                    m.Box(To3(mid, 0.6f), new Vector3(len, 0.15f, 0.08f), yaw, Galvanised);
                    m.Box(To3(mid, 0.45f), new Vector3(len, 0.15f, 0.06f), yaw, Galvanised);
                    m.Box(To3(mid, 0.69f), new Vector3(len, 0.03f, 0.08f), yaw, SnowSurf);
                    Posts(ctx, a, b, d, style, 0.72f, Matte(FenceWood * 0.8f), corner);
                    break;
                }
                default:
                {
                    m.Box(To3(mid, h * 0.5f), new Vector3(len + 0.4f, h, 0.4f), yaw, Concrete);
                    m.Box(To3(mid, h + 0.03f), new Vector3(len + 0.4f, 0.06f, 0.42f), yaw, SnowSurf);
                    break;
                }
            }
            if (style != Style.GuardRail || len > 0.5f)
                ctx.Collider(mid, 0f, new Vector3(len, Mathf.Min(h, 2f), Mathf.Max(thick, 0.15f)), yaw);
        }

        // Posts along a run on the world's lattice (the same points whichever piece draws the run; see
        // StreetLights.Lattice), and one at its start where it turns a corner.
        private static void Posts(Ctx ctx, Vector2 a, Vector2 b, Vector2 d, Style style, float h, Surf surf, bool corner)
        {
            float spacing = PostSpacing(style);
            RaidMesh m = ctx.Low((a + b) * 0.5f);
            if (corner) Post(m, a, h + 0.05f, style, surf, true);
            float len = Vector2.Distance(a, b);
            if (len < 0.5f) return;
            Vector2 dir = (b - a) / len;
            float along0 = Vector2.Dot(a + ctx.Origin, dir);
            for (long k = (long)Mathf.Ceil(along0 / spacing); ; k++)
            {
                float t = k * spacing - along0;
                if (t > len) break;
                if (t < 0f) continue;
                bool big = style == Style.WovenWire || style == Style.Barbed ? k % 3 == 0 : true;
                Post(m, a + dir * t, h, style, surf, big);
            }
        }

        private static void Post(RaidMesh m, Vector2 p, float h, Style style, Surf surf, bool big)
        {
            switch (style)
            {
                case Style.ChainLink:
                case Style.Security:
                    m.Cylinder(To3(p, 0f), 0.035f, h, 6, surf);
                    break;
                case Style.WovenWire:
                case Style.Barbed:
                    if (big) m.Cylinder(To3(p, 0f), 0.07f, h, 6, surf);
                    else m.Box(To3(p, h * 0.5f), new Vector3(0.04f, h, 0.04f), DarkMetal);   // a steel T-post
                    break;
                case Style.GuardRail:
                    m.Box(To3(p, h * 0.5f), new Vector3(0.15f, h, 0.18f), surf);
                    break;
                default:
                    m.Box(To3(p, h * 0.5f), new Vector3(0.11f, h, 0.11f), surf);
                    m.Box(To3(p, h + 0.015f), new Vector3(0.11f, 0.03f, 0.11f), SnowSurf);
                    break;
            }
        }

        // A gate in a fence: open on its post at one side of the gap.
        private static void Gate(Ctx ctx, List<Vector2> pts, float t, float w, Style style)
        {
            Vector2 at = PointAt(pts, t - w, out Vector2 d);
            RaidMesh m = ctx.Low(at);
            float h = style == Style.Privacy ? 1.75f : style == Style.Picket ? 0.95f : 1.25f;
            Surf post = style == Style.ChainLink || style == Style.Security ? Galvanised : Matte(FenceWood);
            m.Box(To3(at, h * 0.5f + 0.05f), new Vector3(0.14f, h + 0.1f, 0.14f), post);
            Vector2 open = Left(d);
            float leaf = w * 2f - 0.15f;
            Vector3 h3 = To3(at + open * 0.08f, 0f), e3 = To3(at + open * leaf, 0f);
            Surf tube = style == Style.Picket ? Matte(WhitePaint) : style == Style.Privacy ? Matte(FenceWood) : Galvanised;
            int rails = style == Style.Privacy ? 3 : 4;
            for (int r = 0; r < rails; r++)
            {
                float y = 0.2f + r * (h - 0.3f) / (rails - 1);
                m.Rod(h3 + new Vector3(0f, y, 0f), e3 + new Vector3(0f, y, 0f), 0.035f, tube);
            }
            m.Rod(e3 + new Vector3(0f, 0.15f, 0f), e3 + new Vector3(0f, h, 0f), 0.04f, tube);
            m.Rod(h3 + new Vector3(0f, h - 0.1f, 0f), Vector3.Lerp(h3, e3, 0.6f) + new Vector3(0f, 0.2f, 0f), 0.03f, tube);
            ctx.Collider(at, 0f, new Vector3(0.16f, h, 0.16f), 0f);
        }

        private static Vector2 PointAt(List<Vector2> pts, float t, out Vector2 dir)
        {
            float arc = 0f;
            dir = Vector2.right;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                float len = Vector2.Distance(pts[i], pts[i + 1]);
                if (len < 1e-4f) continue;
                dir = (pts[i + 1] - pts[i]) / len;
                if (arc + len >= t || i + 2 == pts.Count) return pts[i] + dir * Mathf.Clamp(t - arc, 0f, len);
                arc += len;
            }
            return pts[0];
        }

        // A colour a little lighter or darker, by the barrier (three shades, so the palette stays small).
        private static Color Shade(Color c, long key)
        {
            float k = 0.9f + 0.1f * MapStyle.Pick(key, SaltShade, 3);
            return new Color(c.r * k, c.g * k, c.b * k, 1f);
        }
        #endregion

        #region Plain
        // One piece seen from afar: each straight stretch one shape, rails and wire as a see-through panel, walls and hedges
        // as blocks with snow on top.
        private static void Plain(Ctx ctx, MapLine data, Style style, List<Vector2> pts)
        {
            float h = Height(style);
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.3f) continue;
                Vector2 d = (b - a) / len, mid = (a + b) * 0.5f;
                float yaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                Vector3 n = To3(Left(d), 0f);
                RaidMesh m = ctx.Low(mid);
                switch (style)
                {
                    case Style.Plank: case Style.PlankBlack:
                        m.Upright(To3(mid, 0.71f), n, len, 1.42f, style == Style.Plank ? PlankPanel : BlackPanel);
                        break;
                    case Style.WovenWire: case Style.Barbed:
                        m.Upright(To3(mid, 0.6f), n, len, 1.2f, FieldFence);
                        break;
                    case Style.ChainLink: case Style.Security:
                        m.Upright(To3(mid, h * 0.5f), n, len, h, ChainLink);
                        break;
                    case Style.Picket:
                        m.Upright(To3(mid, 0.5f), n, len, 1.0f, Picket);
                        break;
                    case Style.Privacy:
                        m.Box(To3(mid, 0.9f), new Vector3(len, 1.8f, 0.05f), yaw, Boards);
                        break;
                    case Style.GuardRail:
                        m.Box(To3(mid, 0.55f), new Vector3(len, 0.3f, 0.08f), yaw, Galvanised);
                        break;
                    case Style.Hedge:
                        m.Box(To3(mid, 0.75f), new Vector3(len + 0.4f, 1.5f, 0.95f), yaw, Matte(Shade(HedgeGreen, data.id)));
                        m.Box(To3(mid, 1.52f), new Vector3(len + 0.3f, 0.05f, 0.85f), yaw, SnowSurf);
                        break;
                    default:
                    {
                        float t = Mathf.Max(Thickness(style), 0.3f);
                        Surf wall = style == Style.DryStone ? DryStone : style == Style.Brick ? Brick : style == Style.Block ? Block : Concrete;
                        m.Box(To3(mid, h * 0.5f), new Vector3(len + 0.1f, h, t), yaw, wall);
                        m.Box(To3(mid, h + 0.03f), new Vector3(len + 0.1f, 0.06f, t + 0.04f), yaw, SnowSurf);
                        break;
                    }
                }
            }
        }
        #endregion

        #region Textures
        private static Texture2D s_ChainTex, s_PicketTex, s_FieldTex, s_StoneTex, s_PlankTex, s_BlackTex, s_BrickTex, s_BlockTex, s_BoardTex;
        private static Material s_ChainMat, s_PicketMat, s_FieldMat, s_PlankMat, s_BlackMat;
        // The textured walls' surfaces, kept (making one names its material by its texture's name, which costs).
        private static Surf s_StoneSurf, s_BrickSurf, s_BlockSurf, s_BoardSurf;

        // Chain link: galvanised diamonds, see-through; tiles every 0.6 m.
        private static Surf ChainLink
        {
            get
            {
                if (s_ChainMat == null || s_ChainTex == null)
                {
                    s_ChainTex = Cutout("ChainLink", 64, (x, y) =>
                    {
                        int a = (x + y) & 15, b = (x - y + 64) & 15;
                        return a < 2 || b < 2 ? new Color32(168, 171, 175, 255) : Clear;
                    });
                    s_ChainMat = CutoutMaterial(s_ChainTex, "ChainLink", true);
                }
                return new Surf(s_ChainMat, Vector2.zero, 1f / 0.6f);
            }
        }

        // White pickets with pointed tops on two rails; one tile is a metre of fence, a metre high.
        private static Surf Picket
        {
            get
            {
                if (s_PicketMat == null || s_PicketTex == null)
                {
                    s_PicketTex = Cutout("Picket", 128, (x, y) =>
                    {
                        var white = new Color32(226, 226, 220, 255);
                        bool rail = (y >= 26 && y < 34) || (y >= 92 && y < 100);
                        int px = x % 16;
                        int top = 116 - System.Math.Abs(px - 6) * 2;
                        bool picket = px < 12 && y >= 4 && y < top;
                        return picket || rail ? white : Clear;
                    });
                    s_PicketMat = CutoutMaterial(s_PicketTex, "Picket", true);
                }
                return new Surf(s_PicketMat, Vector2.zero, 1f);
            }
        }

        // Woven field fence: horizontal wires closer together near the ground, stays every 30 cm; a tile is 1.2 m.
        private static Surf FieldFence
        {
            get
            {
                if (s_FieldMat == null || s_FieldTex == null)
                {
                    int[] rows = { 4, 14, 25, 37, 50, 64, 80, 97, 116 };
                    s_FieldTex = Cutout("FieldFence", 128, (x, y) =>
                    {
                        bool wire = x % 32 < 2;
                        foreach (int r in rows) if (y >= r && y < r + 2) wire = true;
                        return wire && y < 120 ? new Color32(150, 152, 150, 255) : Clear;
                    });
                    s_FieldMat = CutoutMaterial(s_FieldTex, "FieldFence", true);
                }
                return new Surf(s_FieldMat, Vector2.zero, 1f / 1.2f);
            }
        }

        // A plank fence seen from afar: four rails with snow along their tops and a post every 2.4 m, as a see-through
        // panel; the tile is 2.4 m square, the fence its lower 1.42 m.
        private static Surf PlankPanel => Panel(ref s_PlankTex, ref s_PlankMat, "PlankPanel", new Color32(224, 224, 219, 255));

        private static Surf BlackPanel => Panel(ref s_BlackTex, ref s_BlackMat, "PlankPanelBlack", new Color32(26, 23, 22, 255));

        private static Surf Panel(ref Texture2D tex, ref Material mat, string name, Color32 paint)
        {
            if (mat == null || tex == null)
            {
                var snow = new Color32(219, 227, 242, 255);
                tex = Cutout(name, 128, (x, y) =>
                {
                    float m = y * 2.4f / 128f;
                    bool post = x < 6 && m < 1.42f;
                    if (post) return m > 1.39f ? snow : paint;
                    for (int k = 0; k < 4; k++)
                    {
                        float bottom = 0.28f + k * 0.32f, top = bottom + 0.14f;
                        if (m >= bottom && m < top) return m > top - 0.025f ? snow : paint;
                    }
                    return Clear;
                });
                mat = CutoutMaterial(tex, name, true);
            }
            return new Surf(mat, Vector2.zero, 1f / 2.4f);
        }

        // Dry-stone limestone: flat stones laid in rough courses, dark gaps between; a tile is 1.6 m.
        private static Surf DryStone
        {
            get
            {
                if (s_StoneSurf.Mat != null) return s_StoneSurf;
                if (s_StoneTex == null)
                {
                    const int S = 128;
                    var px = new Color32[S * S];
                    var rng = new System.Random(0x570E);
                    int y = 0;
                    while (y < S)
                    {
                        int rowH = 6 + rng.Next(7);
                        if (y + rowH > S) rowH = S - y;
                        int x = -rng.Next(15);
                        while (x < S)
                        {
                            int w = 9 + rng.Next(18);
                            float g = 0.52f + (float)rng.NextDouble() * 0.22f;
                            var c = new Color32((byte)(g * 255), (byte)(g * 0.97f * 255), (byte)(g * 0.9f * 255), 255);
                            for (int yy = y; yy < y + rowH; yy++)
                                for (int xx = x; xx < x + w; xx++)
                                {
                                    int wx = ((xx % S) + S) % S;
                                    bool gap = yy - y < 1 || xx - x < 1;
                                    px[yy * S + wx] = gap ? new Color32(48, 46, 42, 255) : c;
                                }
                            x += w;
                        }
                        y += rowH;
                    }
                    s_StoneTex = Opaque("DryStone", S, px);
                }
                return s_StoneSurf = DecorMaterials.Planar(s_StoneTex, 1.6f, 0.05f);
            }
        }

        // A garden wall's red-brown brick in running bond, pale mortar; a tile is 1.2 m (six bricks by eighteen courses).
        private static Surf Brick
        {
            get
            {
                if (s_BrickSurf.Mat != null) return s_BrickSurf;
                if (s_BrickTex == null)
                    s_BrickTex = Opaque("BarrierBrick", 128, Bricks(128, 18, 6, new Color(0.55f, 0.29f, 0.22f), new Color(0.66f, 0.63f, 0.58f), 0xB21C));
                return s_BrickSurf = DecorMaterials.Planar(s_BrickTex, 1.2f, 0.05f);
            }
        }

        // Grey concrete block, stack-bonded courses of 40 by 20 cm; a tile is 1.6 m.
        private static Surf Block
        {
            get
            {
                if (s_BlockSurf.Mat != null) return s_BlockSurf;
                if (s_BlockTex == null)
                    s_BlockTex = Opaque("BarrierBlock", 128, Bricks(128, 8, 4, new Color(0.62f, 0.62f, 0.60f), new Color(0.50f, 0.50f, 0.48f), 0xB10C));
                return s_BlockSurf = DecorMaterials.Planar(s_BlockTex, 1.6f, 0.05f);
            }
        }

        // A privacy fence's boards: upright, 15 cm wide, a dark seam between, the grain running up them; a tile is 1.2 m.
        private static Surf Boards
        {
            get
            {
                if (s_BoardSurf.Mat != null) return s_BoardSurf;
                if (s_BoardTex == null)
                {
                    const int S = 128;
                    var px = new Color32[S * S];
                    var rng = new System.Random(0xB0A2);
                    var tones = new float[8];
                    for (int i = 0; i < tones.Length; i++) tones[i] = 0.86f + (float)rng.NextDouble() * 0.22f;
                    for (int x = 0; x < S; x++)
                    {
                        int board = x / 16;
                        float grain = 0.94f + 0.06f * Mathf.Sin(x * 1.7f + board * 3.1f);
                        for (int y = 0; y < S; y++)
                        {
                            float k = x % 16 == 0 ? 0.45f : tones[board] * grain * (0.97f + 0.03f * Mathf.Sin(y * 0.21f + board));
                            px[y * S + x] = new Color32((byte)Mathf.Clamp(FenceWood.r * k * 255f, 0f, 255f),
                                (byte)Mathf.Clamp(FenceWood.g * k * 255f, 0f, 255f), (byte)Mathf.Clamp(FenceWood.b * k * 255f, 0f, 255f), 255);
                        }
                    }
                    s_BoardTex = Opaque("BarrierBoards", S, px);
                }
                return s_BoardSurf = DecorMaterials.Planar(s_BoardTex, 1.2f, 0.06f);
            }
        }

        // Bricks or blocks in courses (running bond when there are more than four to a course), each a shade of its own,
        // with mortar lines.
        private static Color32[] Bricks(int size, int courses, int perCourse, Color body, Color mortar, int seed)
        {
            var px = new Color32[size * size];
            var rng = new System.Random(seed);
            float courseH = size / (float)courses, brickW = size / (float)perCourse;
            var shade = new float[courses * (perCourse + 1)];
            for (int i = 0; i < shade.Length; i++) shade[i] = 0.88f + (float)rng.NextDouble() * 0.24f;
            Color32 m = new Color32((byte)(mortar.r * 255f), (byte)(mortar.g * 255f), (byte)(mortar.b * 255f), 255);
            bool bond = perCourse > 4;
            for (int y = 0; y < size; y++)
            {
                int course = Mathf.Min(courses - 1, (int)(y / courseH));
                float cy = y - course * courseH;
                float shift = bond && course % 2 == 1 ? brickW * 0.5f : 0f;
                for (int x = 0; x < size; x++)
                {
                    float fx = Mathf.Repeat(x + shift, size);
                    int brick = (int)(fx / brickW);
                    float cx = fx - brick * brickW;
                    if (cy < 1f || cx < 1f)
                    {
                        px[y * size + x] = m;
                        continue;
                    }
                    float k = shade[course * (perCourse + 1) + brick];
                    px[y * size + x] = new Color32((byte)Mathf.Clamp(body.r * k * 255f, 0f, 255f), (byte)Mathf.Clamp(body.g * k * 255f, 0f, 255f),
                        (byte)Mathf.Clamp(body.b * k * 255f, 0f, 255f), 255);
                }
            }
            return px;
        }

        private static Texture2D Opaque(string name, int size, Color32[] px)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            tex.SetPixels32(px);
            tex.Apply(true, false);
            return tex;
        }

        // A tiling cut-out texture painted by a function, with its mips' coverage kept up so a fence doesn't fade away
        // with distance.
        private static Texture2D Cutout(string name, int size, System.Func<int, int, Color32> paint)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++) px[y * size + x] = paint(x, y);
            tex.SetPixels32(px, 0);
            int s = size;
            Color32[] prev = px;
            for (int level = 1; level < tex.mipmapCount; level++)
            {
                int n = Mathf.Max(1, s / 2);
                var next = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        int r = 0, g = 0, b = 0, a = 0, solid = 0;
                        for (int k = 0; k < 4; k++)
                        {
                            Color32 c = prev[Mathf.Min(s - 1, y * 2 + k / 2) * s + Mathf.Min(s - 1, x * 2 + k % 2)];
                            a += c.a;
                            if (c.a == 0) continue;
                            r += c.r; g += c.g; b += c.b; solid++;
                        }
                        float cover = Mathf.Min(1f, a / 4f / 255f * 1.9f);
                        next[y * n + x] = solid == 0 ? Clear : new Color32((byte)(r / solid), (byte)(g / solid), (byte)(b / solid), (byte)(cover * 255f));
                    }
                tex.SetPixels32(next, level);
                prev = next;
                s = n;
            }
            tex.Apply(false, false);
            return tex;
        }
        #endregion
    }
}
