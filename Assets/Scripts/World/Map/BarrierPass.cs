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
    /// every rail and coping. Fences leave a gap (and a gate, swung open) at a mapped gate. Each straight run gets a collider;
    /// tree rows are the vegetation's.
    /// </summary>
    internal static class BarrierPass
    {
        private const int SaltStyle = 0xBA77, SaltShade = 0xBA78;

        private enum Style { Plank, PlankBlack, WovenWire, Barbed, ChainLink, Security, Privacy, Picket, DryStone, Brick, Block, Hedge, GuardRail, Retaining }

        private static readonly Color WhitePaint = new Color(0.88f, 0.88f, 0.86f);
        private static readonly Color Creosote = new Color(0.09f, 0.08f, 0.08f);
        private static readonly Color FenceWood = new Color(0.46f, 0.36f, 0.26f);
        private static readonly Color HedgeGreen = new Color(0.17f, 0.24f, 0.14f);

        internal static void Build(Ctx ctx)
        {
            Site site = ctx.Site;
            if (site.Area.Barriers.Count == 0) return;
            float half = ctx.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            var gates = new List<Vector2>();
            foreach (Node n in site.Area.Nodes) if (n.Data.kind == "gate") gates.Add(n.At);
            foreach (Line l in site.Area.Barriers)
            {
                if (l.Data.kind == "tree_row" || l.Pts.Count < 2) continue;
                Style style = StyleOf(l.Data);
                foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, square)) Draw(ctx, l.Data, style, piece, gates);
            }
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

        // One piece of a barrier inside the square: its runs between gates, each straight stretch drawn and given a
        // collider, posts on the lattice and at the corners.
        private static void Draw(Ctx ctx, MapLine data, Style style, List<Vector2> pts, List<Vector2> gates)
        {
            Site site = ctx.Site;
            float h = Height(style), thick = Thickness(style);
            bool fence = data.kind == "fence";
            var cuts = new List<Vector2>();   // (from, to) along the piece, cut out for gates
            if (fence)
                foreach (Vector2 g in gates)
                {
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
                    if (e - s > 0.05f) Run(ctx, data, style, a + d * s, a + d * e, d, h, thick, i > 0 && s < 0.01f);
                    s = e;
                }
                arc += len;
            }
            site.TakeLine(pts, thick * 0.5f + 0.35f);
        }

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
                    Surf boards = DecorMaterials.Planks(Shade(FenceWood, shadeKey));
                    m.Box(To3(mid, 0.95f), new Vector3(len, 1.7f, 0.03f), yaw, boards);
                    m.Box(To3(mid, 1.82f), new Vector3(len, 0.05f, 0.1f), yaw, Matte(Shade(FenceWood, shadeKey) * 0.85f));
                    m.Box(To3(mid, 1.86f), new Vector3(len, 0.03f, 0.09f), yaw, SnowSurf);
                    Posts(ctx, a, b, d, style, 1.85f, Matte(Shade(FenceWood, shadeKey) * 0.8f), corner);
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
                    Surf wall = style == Style.Brick
                        ? DecorMaterials.WallExterior(ExteriorStyle.Brick, Shade(new Color(0.56f, 0.30f, 0.24f), shadeKey))
                        : DecorMaterials.WallExterior(ExteriorStyle.CinderBlock, new Color(0.62f, 0.62f, 0.60f));
                    m.Box(To3(mid, h * 0.5f), new Vector3(len + 0.3f, h, 0.3f), yaw, wall);
                    m.Box(To3(mid, h + 0.04f), new Vector3(len + 0.34f, 0.08f, 0.38f), yaw, Concrete);
                    m.Box(To3(mid, h + 0.1f), new Vector3(len + 0.3f, 0.05f, 0.36f), yaw, SnowSurf);
                    break;
                }
                case Style.Hedge:
                {
                    Surf green = Matte(Shade(HedgeGreen, shadeKey));
                    m.Box(To3(mid, 0.72f), new Vector3(len + 0.4f, 1.44f, 0.95f), yaw, green);
                    int lumps = Mathf.Max(1, Mathf.RoundToInt(len / 1.6f));
                    for (int k = 0; k < lumps; k++)
                    {
                        Vector2 p = Vector2.Lerp(a, b, (k + 0.5f) / lumps);
                        m.Ellipsoid(To3(p, 1.45f), new Vector3(1.5f, 0.42f, 1.0f), Quaternion.Euler(0f, yaw, 0f), green, 6, 3);
                        m.Ellipsoid(To3(p, 1.58f), new Vector3(1.3f, 0.26f, 0.85f), Quaternion.Euler(0f, yaw, 0f), SnowSurf, 6, 3);
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

        // Posts along a run on the world's lattice (and one at its start where it turns a corner).
        private static void Posts(Ctx ctx, Vector2 a, Vector2 b, Vector2 d, Style style, float h, Surf surf, bool corner)
        {
            float spacing = PostSpacing(style);
            var pts = new List<Vector2>(2) { a, b };
            RaidMesh m = ctx.Low((a + b) * 0.5f);
            if (corner) Post(m, a, h + 0.05f, style, surf, true);
            foreach ((Vector2 p, Vector2 _, long k) in StreetLights.Lattice(ctx, pts, spacing))
            {
                bool big = style == Style.WovenWire || style == Style.Barbed ? k % 3 == 0 : true;
                Post(m, p, h, style, surf, big);
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

        #region Textures
        private static Texture2D s_ChainTex, s_PicketTex, s_FieldTex, s_StoneTex;
        private static Material s_ChainMat, s_PicketMat, s_FieldMat;

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

        // Dry-stone limestone: flat stones laid in rough courses, dark gaps between; a tile is 1.6 m.
        private static Surf DryStone
        {
            get
            {
                if (s_StoneTex == null)
                {
                    const int S = 256;
                    var px = new Color32[S * S];
                    var rng = new System.Random(0x570E);
                    int y = 0;
                    while (y < S)
                    {
                        int rowH = 12 + rng.Next(14);
                        if (y + rowH > S) rowH = S - y;
                        int x = -rng.Next(30);
                        while (x < S)
                        {
                            int w = 18 + rng.Next(36);
                            float g = 0.52f + (float)rng.NextDouble() * 0.22f;
                            var c = new Color32((byte)(g * 255), (byte)(g * 0.97f * 255), (byte)(g * 0.9f * 255), 255);
                            for (int yy = y; yy < y + rowH; yy++)
                                for (int xx = x; xx < x + w; xx++)
                                {
                                    int wx = ((xx % S) + S) % S;
                                    bool gap = yy - y < 2 || xx - x < 2;
                                    px[yy * S + wx] = gap ? new Color32(48, 46, 42, 255) : c;
                                }
                            x += w;
                        }
                        y += rowH;
                    }
                    s_StoneTex = new Texture2D(S, S, TextureFormat.RGBA32, true)
                    {
                        name = "DryStone", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4,
                        hideFlags = HideFlags.DontUnloadUnusedAsset,
                    };
                    s_StoneTex.SetPixels32(px);
                    s_StoneTex.Apply(true, false);
                }
                return DecorMaterials.Planar(s_StoneTex, 1.6f, 0.05f);
            }
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
