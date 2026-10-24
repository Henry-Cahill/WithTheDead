using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What stands on the snow in the land use with a purpose (Full detail, inside the square, kept clear of where the
    // later passes build streets, sidewalks, railways, buildings and fences): cemeteries' rows of headstones (and a
    // few rows behind rural churches), playground equipment, ball fields' backstops, goals and hoops, and the pallets,
    // drums and crates stacked against warehouses and factories. All batched with the outdoor props; colliders only
    // for the big pieces.
    internal static partial class GroundPass
    {
        private const int MaxStones = 3000;
        private const int StonesPerStep = 600;

        private static readonly Color[] StoneColours =
        {
            new Color(0.46f, 0.46f, 0.47f), new Color(0.37f, 0.37f, 0.39f), new Color(0.23f, 0.23f, 0.25f),
            new Color(0.58f, 0.47f, 0.45f), new Color(0.72f, 0.72f, 0.70f), new Color(0.63f, 0.63f, 0.59f),
            new Color(0.61f, 0.55f, 0.46f), new Color(0.52f, 0.52f, 0.52f),
        };

        private static IEnumerable<string> Grounds(Ground g)
        {
            int stones = 0;
            foreach (Owner o in g.Owners)
            {
                if (o.Visible < 20f) continue;
                switch (o.Kind)
                {
                    case "cemetery":
                    case "churchyard":
                        if (o.Kind == "churchyard" && !RuralChurch(g, o)) break;
                        foreach (int placed in Graves(g, o, o.Kind == "churchyard", MaxStones - stones))
                        {
                            stones += placed;
                            yield return "cemeteries";
                        }
                        break;
                    case "playground": Playground(g, o); break;
                    case "pitch": Pitch(g, o); break;
                }
            }
            yield return "grounds";
            Yards(g);
            yield return "yards";
        }

        #region Cemeteries
        // A churchyard gets graves when the church stands out in the country: little else built round it.
        private static bool RuralChurch(Ground g, Owner o)
        {
            if (o.Visible < 1200f) return false;
            Rect near = o.Bounds;
            near.xMin -= 60f; near.yMin -= 60f; near.xMax += 60f; near.yMax += 60f;
            int buildings = 0;
            foreach (Footprint f in g.Site.Area.Buildings)
                if (near.Contains(f.Middle) && ++buildings > 14) return false;
            return MapStyle.Unit(o.Id, SaltGraves) < 0.8f;
        }

        // Rows of headstones across the cemetery's long axis, on a lattice fixed to the world (rows 3 m or so apart,
        // stones every 1.5 to 2.2 m, facing east as the old graves do), with empty plots, family plots of matching
        // stones, and the odd bare patch where a cedar can stand; each stone drawn by the chunk it stands in. Yields
        // the stones placed every few hundred, so a big cemetery spreads over several steps.
        private static IEnumerable<int> Graves(Ground g, Owner o, bool churchyard, int budget)
        {
            if (budget <= 0) yield break;
            Vector2 d = o.Axis, n = new Vector2(-d.y, d.x);
            Vector2 face = n.x >= 0f ? n : -n;   // headstones face east-ish, along the rows' normal
            float faceYaw = Mathf.Atan2(face.x, face.y) * Mathf.Rad2Deg;
            float rowGap = 2.8f + 0.7f * MapStyle.Unit(o.Id, SaltGraves + 1);
            float stoneGap = 1.5f + 0.7f * MapStyle.Unit(o.Id, SaltGraves + 2);
            float rowPhase = MapStyle.Unit(o.Id, SaltGraves + 3) * rowGap;
            float empty = churchyard ? 0.4f : 0.16f + 0.14f * MapStyle.Unit(o.Id, SaltGraves + 4);
            float old = MapStyle.Unit(o.Id, SaltGraves + 5);   // older cemeteries: more marble, obelisks, leaning stones
            float offN = Vector2.Dot(g.Offset, n), offD = Vector2.Dot(g.Offset, d);
            Rect b = o.Bounds;
            float s0 = float.MaxValue, s1 = float.MinValue, t0 = float.MaxValue, t1 = float.MinValue;
            for (int c = 0; c < 4; c++)
            {
                var corner = new Vector2((c & 1) == 0 ? b.xMin : b.xMax, (c & 2) == 0 ? b.yMin : b.yMax);
                float s = Vector2.Dot(corner, n) + offN, t = Vector2.Dot(corner, d) + offD;
                s0 = Mathf.Min(s0, s); s1 = Mathf.Max(s1, s);
                t0 = Mathf.Min(t0, t); t1 = Mathf.Max(t1, t);
            }
            int placed = 0, since = 0;
            int k0 = Mathf.CeilToInt((s0 - rowPhase) / rowGap), k1 = Mathf.FloorToInt((s1 - rowPhase) / rowGap);
            for (int k = k0; k <= k1 && placed < budget; k++)
            {
                float stonePhase = MapStyle.Unit(((long)k << 20) ^ o.Id, SaltGraves + 6) * stoneGap;
                int j0 = Mathf.CeilToInt((t0 - stonePhase) / stoneGap), j1 = Mathf.FloorToInt((t1 - stonePhase) / stoneGap);
                for (int j = j0; j <= j1 && placed < budget; j++)
                {
                    long key = ((long)k << 32) ^ (uint)j;
                    uint h = MapStyle.Hash(key, (int)(o.Id & 0x7fffffff) ^ SaltStone);
                    if ((h & 0xffff) < empty * 65536f) continue;
                    float s = k * rowGap + rowPhase + (((h >> 16) & 0xff) / 255f - 0.5f) * 0.3f;
                    float t = j * stoneGap + stonePhase + (((h >> 24) & 0xff) / 255f - 0.5f) * 0.25f;
                    Vector2 at = n * (s - offN) + d * (t - offD);
                    if (!g.Square.Contains(at) || TreePlot(g.World(at), o.Id) || !g.Shows(o, at) || g.Blocked(at)) continue;
                    if (RealEdgeDistance(o.Pts, at, g.Square) < 1.1f) continue;
                    Headstone(g, at, faceYaw, key, o.Id, old);
                    placed++;
                    if (++since < StonesPerStep) continue;
                    yield return since;
                    since = 0;
                }
            }
            if (since > 0) yield return since;
        }

        // A bare 7 m patch in every 24 m square of a cemetery (fixed to the world), where the trees can stand.
        private static bool TreePlot(Vector2 w, long id)
        {
            const float block = 24f, plot = 7f;
            int bx = Mathf.FloorToInt(w.x / block), bz = Mathf.FloorToInt(w.y / block);
            long key = ((long)bx << 32) ^ (uint)bz;
            float px = bx * block + MapStyle.Unit(key, SaltPlot) * (block - plot), pz = bz * block + MapStyle.Unit(key, SaltPlot + 1) * (block - plot);
            return w.x >= px && w.x < px + plot && w.y >= pz && w.y < pz + plot;
        }

        private static void Headstone(Ground g, Vector2 at, float yaw, long key, long cemetery, float old)
        {
            System.Random rng = MapStyle.Rng(key ^ (cemetery << 7), SaltStone + 1);
            // Families: neighbouring stones share their stone and shape more often than not.
            long family = ((key >> 32) << 32) ^ ((uint)key >> 2);
            System.Random fam = MapStyle.Rng(family ^ cemetery, SaltStone + 2);
            bool matching = rng.NextDouble() < 0.55;
            System.Random pick = matching ? fam : rng;
            float roll = (float)pick.NextDouble();
            Color stone = StoneColours[pick.Next(StoneColours.Length)];
            if (rng.NextDouble() < old * 0.35) stone = StoneColours[4 + rng.Next(3)];   // old marble and sandstone
            stone = DecorPalette.Vary(stone, 0.06f, rng);
            Surf s = DecorPalette.Matte(stone);
            Surf snow = DecorPalette.Matte(new Color(0.85f, 0.90f, 0.98f));
            Vector3 p = To3(at, 0f);
            RaidMesh m = g.Site.Town.Out.Solid(p);
            m.SetFrame(p, yaw + MapStyle.Range(rng, -4f, 4f));
            float lean = rng.NextDouble() < 0.1 + old * 0.2 ? MapStyle.Range(rng, -6f, 6f) : 0f;
            Quaternion tilt = Quaternion.Euler(lean, 0f, MapStyle.Range(rng, -2f, 2f) * (lean != 0f ? 1f : 0f));
            float obelisks = 0.02f + old * 0.06f;
            if (roll < obelisks)
            {
                float h = MapStyle.Range(rng, 1.8f, 3.0f);
                m.Box(new Vector3(0f, 0.2f, 0f), new Vector3(0.85f, 0.4f, 0.85f), s);
                m.Flat(new Vector3(0f, 0.405f, 0f), 0.8f, 0.8f, 0f, snow);
                m.Box(new Vector3(0f, 0.52f, 0f), new Vector3(0.6f, 0.24f, 0.6f), s);
                m.Cylinder(new Vector3(0f, 0.64f, 0f), 0.24f, h, 4, s, Quaternion.Euler(0f, 45f, 0f), 0.62f);
                m.Cylinder(new Vector3(0f, 0.64f + h, 0f), 0.15f, 0.3f, 4, s, Quaternion.Euler(0f, 45f, 0f), 0f);
                g.Site.Town.PropCollider(p + Vector3.up * 0.6f, new Vector3(0.85f, 1.2f, 0.85f), yaw);
                g.Site.Take(at, 0.6f);
            }
            else if (roll < obelisks + 0.08f)
            {
                // A cross.
                float h = MapStyle.Range(rng, 0.9f, 1.3f);
                m.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(0.13f, h, 0.13f), tilt, s);
                m.Box(new Vector3(0f, h * 0.7f, 0f), new Vector3(0.56f, 0.12f, 0.13f), tilt, s);
                m.Flat(new Vector3(0f, h + 0.004f, 0f), 0.12f, 0.12f, 0f, snow);
                g.Site.Take(at, 0f);
            }
            else if (roll < obelisks + 0.2f)
            {
                // A family stone: wide and low on a base.
                float w = MapStyle.Range(rng, 1.2f, 1.6f), h = MapStyle.Range(rng, 0.6f, 0.85f);
                m.Box(new Vector3(0f, 0.08f, 0f), new Vector3(w + 0.16f, 0.16f, 0.42f), s);
                m.Box(new Vector3(0f, 0.16f + h * 0.5f, 0f), new Vector3(w, h, 0.22f), s);
                m.Flat(new Vector3(0f, 0.165f + h, 0f), w, 0.22f, 0f, snow);
                g.Site.Take(at, 0f);
            }
            else
            {
                // An upright tablet: square-topped, or with a rounded top; small ones for children and footstones.
                bool small = rng.NextDouble() < 0.1;
                float w = small ? MapStyle.Range(rng, 0.3f, 0.42f) : MapStyle.Range(rng, 0.48f, 0.85f);
                float h = small ? MapStyle.Range(rng, 0.3f, 0.45f) : MapStyle.Range(rng, 0.55f, 1.0f);
                float th = MapStyle.Range(rng, 0.1f, 0.18f);
                bool rounded = !small && rng.NextDouble() < 0.3 + old * 0.2;
                if (rounded)
                {
                    float body = h - w * 0.5f;
                    m.Box(new Vector3(0f, body * 0.5f, 0f), new Vector3(w, body, th), tilt, s);
                    m.Cylinder(new Vector3(0f, body, -th * 0.5f), w * 0.5f, th, 10, s, tilt * Quaternion.Euler(90f, 0f, 0f));
                }
                else
                {
                    m.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, th), tilt, s);
                    if (lean == 0f) m.Flat(new Vector3(0f, h + 0.004f, 0f), w, th, 0f, snow);
                    else m.Box(tilt * new Vector3(0f, h + 0.01f, 0f), new Vector3(w + 0.02f, 0.03f, th + 0.02f), tilt, snow);
                }
                g.Site.Take(at, 0f);
            }
            m.ResetFrame();
        }
        #endregion

        #region Playgrounds
        // Up to five pieces of 1993 playground: swings, a slide, a climbing frame, a merry-go-round, a seesaw, in a zigzag
        // down the playground's long axis, each over a trampled patch of frozen mud and with a collider round it.
        private static void Playground(Ground g, Owner o)
        {
            List<Vector2> whole = g.Whole(o) ?? o.Pts;
            Box(whole, out Vector2 d, out Vector2 mid, out Vector2 half);
            var n = new Vector2(-d.y, d.x);
            float area = Mathf.Abs(MapGeometry.SignedArea(whole));
            int count = Mathf.Clamp(Mathf.RoundToInt(area / 80f), 1, 5);
            float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg - 90f;   // a piece's local +x along the playground
            Color[] paints = { new Color(0.62f, 0.17f, 0.13f), new Color(0.17f, 0.29f, 0.55f), new Color(0.20f, 0.40f, 0.24f), new Color(0.78f, 0.62f, 0.16f) };
            for (int i = 0; i < count; i++)
            {
                System.Random rng = MapStyle.Rng(o.Id * 31 + i, SaltPlay);
                Vector2 at = mid + d * ((-half.x + (i + 0.5f) * half.x * 2f / count) * 0.8f) + n * (((i & 1) == 0 ? -0.3f : 0.3f) * half.y);
                int kind = rng.Next(5);
                Vector2 size = kind == 0 ? new Vector2(4.2f, 2.4f) : kind == 1 ? new Vector2(4f, 1.2f) : kind == 2 ? new Vector2(2.6f, 2.6f)
                    : kind == 3 ? new Vector2(2.5f, 2.5f) : new Vector2(3.2f, 0.8f);
                if (!g.Square.Contains(at) || !g.Shows(o, at) || !FitsIn(g, whole, at, d, n, size * 0.5f)) continue;
                Vector3 p = To3(at, 0f);
                Vector3 patch = To3(at, DecalY);
                g.Site.Town.Out.Flat(patch).FlatBlob(patch, size.x * 0.55f + 0.6f, size.y * 0.55f + 0.6f, yaw, 0.25f, rng, MudSurf);
                RaidMesh m = g.Site.Town.Out.Solid(p);
                m.SetFrame(p, yaw);
                Surf paint = DecorPalette.Matte(paints[rng.Next(paints.Length)]);
                Surf metal = DecorPalette.Metal(new Color(0.55f, 0.57f, 0.58f));
                Surf snow = DecorPalette.Matte(new Color(0.85f, 0.90f, 0.98f));
                float height;
                switch (kind)
                {
                    case 0: height = Swings(m, rng, paint, metal, snow); break;
                    case 1: height = Slide(m, paint, metal, snow); break;
                    case 2: height = Climber(m, paint, snow); break;
                    case 3: height = Roundabout(m, paint, metal, snow); break;
                    default: height = Seesaw(m, paint, metal, snow); break;
                }
                m.ResetFrame();
                g.Site.Town.PropCollider(p + Vector3.up * (height * 0.5f), new Vector3(size.x, height, size.y), yaw);
                Take(g.Site, at, d, n, size * 0.5f + new Vector2(0.4f, 0.4f));
            }
        }

        // True when a rectangle (half sizes along d and n) round a point is inside the outline and clear of streets,
        // buildings and fences.
        private static bool FitsIn(Ground g, List<Vector2> outline, Vector2 at, Vector2 d, Vector2 n, Vector2 half)
        {
            for (int k = 0; k < 4; k++)
            {
                Vector2 q = at + d * ((k & 1) == 0 ? -half.x : half.x) + n * ((k & 2) == 0 ? -half.y : half.y);
                if (!Inside(outline, q) || !g.Square.Contains(q) || g.Blocked(q)) return false;
            }
            return g.Clear(at, 0.5f);
        }

        private static void Take(Site site, Vector2 at, Vector2 d, Vector2 n, Vector2 half)
        {
            Vector2 ex = new Vector2(Mathf.Abs(d.x) * half.x + Mathf.Abs(n.x) * half.y, Mathf.Abs(d.y) * half.x + Mathf.Abs(n.y) * half.y);
            site.Take(Rect.MinMaxRect(at.x - ex.x, at.y - ex.y, at.x + ex.x, at.y + ex.y));
        }

        private static float Swings(RaidMesh m, System.Random rng, in Surf paint, in Surf metal, in Surf snow)
        {
            const float top = 2.4f, halfLen = 1.9f, spread = 1.0f;
            for (int s = -1; s <= 1; s += 2)
            {
                m.Rod(new Vector3(s * halfLen, 0f, -spread), new Vector3(s * halfLen, top, 0f), 0.07f, paint);
                m.Rod(new Vector3(s * halfLen, 0f, spread), new Vector3(s * halfLen, top, 0f), 0.07f, paint);
            }
            m.Rod(new Vector3(-halfLen - 0.05f, top, 0f), new Vector3(halfLen + 0.05f, top, 0f), 0.09f, paint);
            m.Box(new Vector3(0f, top + 0.06f, 0f), new Vector3(halfLen * 2f, 0.025f, 0.1f), snow);
            int seats = rng.Next(2, 4);
            Surf rubber = DecorPalette.Matte(new Color(0.10f, 0.10f, 0.11f));
            for (int i = 0; i < seats; i++)
            {
                float x = -halfLen + (i + 0.5f) * halfLen * 2f / seats;
                float swing = MapStyle.Range(rng, -0.15f, 0.15f);
                for (int c = -1; c <= 1; c += 2)
                    m.Rod(new Vector3(x + c * 0.2f, top, 0f), new Vector3(x + c * 0.2f, 0.52f, swing), 0.015f, metal);
                m.Box(new Vector3(x, 0.5f, swing), new Vector3(0.46f, 0.04f, 0.18f), rubber);
                m.Box(new Vector3(x, 0.53f, swing), new Vector3(0.42f, 0.02f, 0.15f), snow);
            }
            return top;
        }

        private static float Slide(RaidMesh m, in Surf paint, in Surf metal, in Surf snow)
        {
            const float deck = 1.8f;
            for (int s = -1; s <= 1; s += 2)
            {
                m.Rod(new Vector3(-1.7f, 0f, s * 0.24f), new Vector3(-1.35f, deck + 0.9f, s * 0.24f), 0.05f, paint);
                m.Rod(new Vector3(-0.75f, 0f, s * 0.3f), new Vector3(-0.75f, deck + 0.9f, s * 0.3f), 0.05f, paint);
            }
            for (int r = 1; r <= 5; r++)
            {
                float t = r / 6f;
                m.Rod(new Vector3(-1.7f + 0.35f * t, (deck + 0.9f) * t, -0.24f), new Vector3(-1.7f + 0.35f * t, (deck + 0.9f) * t, 0.24f), 0.035f, metal);
            }
            m.Box(new Vector3(-1.0f, deck, 0f), new Vector3(0.62f, 0.05f, 0.62f), paint);
            m.Box(new Vector3(-1.0f, deck + 0.04f, 0f), new Vector3(0.58f, 0.03f, 0.58f), snow);
            var a = new Vector3(-0.7f, deck, 0f);
            var b = new Vector3(1.9f, 0.3f, 0f);
            Vector3 mid = (a + b) * 0.5f;
            float len = Vector3.Distance(a, b);
            Quaternion tilt = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            m.Box(mid, new Vector3(len, 0.04f, 0.5f), tilt, metal);
            m.Box(mid + tilt * new Vector3(0f, 0.08f, 0.26f), new Vector3(len, 0.16f, 0.03f), tilt, paint);
            m.Box(mid + tilt * new Vector3(0f, 0.08f, -0.26f), new Vector3(len, 0.16f, 0.03f), tilt, paint);
            m.Box(mid + tilt * new Vector3(0f, 0.035f, 0f), new Vector3(len * 0.95f, 0.03f, 0.44f), tilt, snow);
            m.Rod(new Vector3(1.6f, 0f, 0f), new Vector3(1.6f, 0.45f, 0f), 0.06f, paint);
            return deck + 0.9f;
        }

        private static float Climber(RaidMesh m, in Surf paint, in Surf snow)
        {
            const float h = 1.8f, w = 1.2f;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0) m.Rod(new Vector3(i * w, 0f, j * w), new Vector3(i * w, h, j * w), 0.045f, paint);
            for (int level = 1; level <= 3; level++)
            {
                float y = h * level / 3f;
                for (int i = -1; i <= 1; i++)
                {
                    m.Rod(new Vector3(-w, y, i * w), new Vector3(w, y, i * w), 0.04f, paint);
                    m.Rod(new Vector3(i * w, y, -w), new Vector3(i * w, y, w), 0.04f, paint);
                }
            }
            for (int i = -1; i <= 1; i++)
            {
                m.Box(new Vector3(0f, h + 0.035f, i * w), new Vector3(w * 2f, 0.02f, 0.06f), snow);
                m.Box(new Vector3(i * w, h + 0.035f, 0f), new Vector3(0.06f, 0.02f, w * 2f), snow);
            }
            return h;
        }

        private static float Roundabout(RaidMesh m, in Surf paint, in Surf metal, in Surf snow)
        {
            m.Cylinder(new Vector3(0f, 0f, 0f), 0.12f, 0.35f, 8, metal);
            m.Cylinder(new Vector3(0f, 0.3f, 0f), 1.15f, 0.07f, 14, paint);
            m.Cylinder(new Vector3(0f, 0.37f, 0f), 1.1f, 0.02f, 14, snow);
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI * 0.5f + 0.4f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                m.Rod(dir * 0.15f + Vector3.up * 0.37f, dir * 0.95f + Vector3.up * 0.37f, 0.035f, metal);
                m.Rod(dir * 0.95f + Vector3.up * 0.37f, dir * 0.95f + Vector3.up * 0.95f, 0.035f, metal);
                m.Rod(dir * 0.95f + Vector3.up * 0.95f, dir * 0.2f + Vector3.up * 0.95f, 0.035f, metal);
            }
            return 1f;
        }

        private static float Seesaw(RaidMesh m, in Surf paint, in Surf metal, in Surf snow)
        {
            m.Box(new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.35f), metal);
            Quaternion tilt = Quaternion.Euler(0f, 0f, 11f);
            m.Box(new Vector3(0f, 0.52f, 0f), new Vector3(3.2f, 0.06f, 0.28f), tilt, paint);
            m.Box(new Vector3(0f, 0.56f, 0f), new Vector3(3.1f, 0.025f, 0.24f), tilt, snow);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 end = tilt * new Vector3(s * 1.2f, 0f, 0f) + new Vector3(0f, 0.55f, 0f);
                m.Rod(end - new Vector3(0f, 0f, 0.14f), end + new Vector3(0f, 0.3f, -0.14f), 0.03f, metal);
                m.Rod(end + new Vector3(0f, 0f, 0.14f), end + new Vector3(0f, 0.3f, 0.14f), 0.03f, metal);
                m.Rod(end + new Vector3(0f, 0.3f, -0.14f), end + new Vector3(0f, 0.3f, 0.14f), 0.03f, metal);
            }
            return 1f;
        }
        #endregion

        #region Pitches
        // What a pitch's shape says it is: a football or soccer field (goals at both ends), a tennis court (net and a
        // fenced court), a basketball court (two hoops), or a ball diamond (a backstop behind home plate and benches
        // along the baselines). The snow hides the lines.
        private static void Pitch(Ground g, Owner o)
        {
            List<Vector2> whole = g.Whole(o) ?? o.Pts;
            Box(whole, out Vector2 d, out Vector2 mid, out Vector2 half);
            float len = half.x * 2f, wid = half.y * 2f;
            float area = Mathf.Abs(MapGeometry.SignedArea(whole));
            float fill = area / Mathf.Max(1f, len * wid);
            var n = new Vector2(-d.y, d.x);
            System.Random rng = MapStyle.Rng(o.Id, SaltPitch);
            Surf white = DecorPalette.Matte(new Color(0.86f, 0.86f, 0.84f));
            Surf snow = DecorPalette.Matte(new Color(0.85f, 0.90f, 0.98f));
            if (len >= 80f && len <= 135f && wid >= 40f && wid <= 85f && fill > 0.72f)
            {
                bool football = rng.NextDouble() < 0.6;
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 at = mid + d * (s * (half.x - (football ? 1.5f : 3f)));
                    if (!g.Square.Contains(at) || g.Blocked(at)) continue;
                    float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg + (s < 0 ? 180f : 0f);   // local +z: out of the field
                    Vector3 p = To3(at, 0f);
                    RaidMesh m = g.Site.Town.Out.Solid(p);
                    m.SetFrame(p, yaw);
                    if (football) GoalPosts(m, DecorPalette.Matte(new Color(0.84f, 0.70f, 0.16f)), snow);
                    else SoccerGoal(m, white, snow);
                    m.ResetFrame();
                    g.Site.Take(at, 1f);
                }
                return;
            }
            if (len >= 30f && len <= 45f && wid >= 14f && wid <= 25f && fill > 0.8f)
            {
                Court(g, mid, d, n, half, true, rng, white, snow);
                return;
            }
            if (len >= 20f && len <= 34f && wid >= 11f && wid <= 21f && fill > 0.8f)
            {
                Court(g, mid, d, n, half, false, rng, white, snow);
                return;
            }
            if (area >= 1800f && len <= 140f) Diamond(g, whole, mid, rng, snow);
        }

        private static void GoalPosts(RaidMesh m, in Surf yellow, in Surf snow)
        {
            m.Rod(new Vector3(0f, 0f, 1.2f), new Vector3(0f, 2.4f, 1.2f), 0.18f, yellow);
            m.Rod(new Vector3(0f, 2.4f, 1.2f), new Vector3(0f, 3.05f, 0f), 0.14f, yellow);
            m.Rod(new Vector3(-2.82f, 3.05f, 0f), new Vector3(2.82f, 3.05f, 0f), 0.12f, yellow);
            m.Box(new Vector3(0f, 3.13f, 0f), new Vector3(5.6f, 0.03f, 0.1f), snow);
            for (int s = -1; s <= 1; s += 2) m.Rod(new Vector3(s * 2.82f, 3.05f, 0f), new Vector3(s * 2.82f, 9f, 0f), 0.1f, yellow);
        }

        private static void SoccerGoal(RaidMesh m, in Surf white, in Surf snow)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                m.Rod(new Vector3(s * 3.66f, 0f, 0f), new Vector3(s * 3.66f, 2.44f, 0f), 0.11f, white);
                m.Rod(new Vector3(s * 3.66f, 2.44f, 0f), new Vector3(s * 3.66f, 1.4f, 1.6f), 0.05f, white);
                m.Rod(new Vector3(s * 3.66f, 0f, 1.8f), new Vector3(s * 3.66f, 1.4f, 1.6f), 0.05f, white);
            }
            m.Rod(new Vector3(-3.7f, 2.44f, 0f), new Vector3(3.7f, 2.44f, 0f), 0.11f, white);
            m.Box(new Vector3(0f, 2.51f, 0f), new Vector3(7.3f, 0.025f, 0.1f), snow);
            m.Rod(new Vector3(-3.66f, 1.4f, 1.6f), new Vector3(3.66f, 1.4f, 1.6f), 0.05f, white);
        }

        // A tennis court (net posts and net across the middle, a chain-link fence round it) or a basketball court (a
        // hoop at each end).
        private static void Court(Ground g, Vector2 mid, Vector2 d, Vector2 n, Vector2 half, bool tennis, System.Random rng, in Surf white, in Surf snow)
        {
            Surf metal = DecorPalette.Metal(new Color(0.42f, 0.44f, 0.45f));
            float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            if (tennis)
            {
                Vector3 c = To3(mid, 0f);
                if (!g.Square.Contains(mid)) return;
                RaidMesh m = g.Site.Town.Out.Solid(c);
                m.SetFrame(c, yaw + 90f);   // local x across the court, along the net
                Surf green = DecorPalette.Matte(new Color(0.19f, 0.29f, 0.22f));
                for (int s = -1; s <= 1; s += 2) m.Cylinder(new Vector3(s * 6.4f, 0f, 0f), 0.05f, 1.07f, 6, green);
                m.Box(new Vector3(0f, 0.55f, 0f), new Vector3(12.8f, 0.85f, 0.02f), DecorPalette.Matte(new Color(0.16f, 0.17f, 0.17f)));
                m.Box(new Vector3(0f, 0.99f, 0f), new Vector3(12.8f, 0.06f, 0.04f), white);
                m.ResetFrame();
                // The fence: posts every 3 m round the court's edge, a top and a bottom rail.
                var corners = new Vector2[4];
                for (int k = 0; k < 4; k++)
                    corners[k] = mid + d * ((k == 0 || k == 3) ? -half.x + 0.3f : half.x - 0.3f) + n * (k < 2 ? -half.y + 0.3f : half.y - 0.3f);
                for (int k = 0; k < 4; k++)
                {
                    Vector2 a = corners[k], b = corners[(k + 1) % 4];
                    float len = Vector2.Distance(a, b);
                    int posts = Mathf.Max(1, Mathf.RoundToInt(len / 3f));
                    Vector3 a3 = To3(a, 0f), b3 = To3(b, 0f);
                    if (!g.Square.Contains(a) || !g.Square.Contains(b)) continue;
                    RaidMesh f = g.Site.Town.Out.Solid((a3 + b3) * 0.5f);
                    for (int i = 0; i < posts; i++)
                    {
                        Vector3 q = Vector3.Lerp(a3, b3, i / (float)posts);
                        f.Rod(q, q + Vector3.up * 3f, 0.05f, metal);
                    }
                    f.Rod(a3 + Vector3.up * 3f, b3 + Vector3.up * 3f, 0.04f, metal);
                    f.Rod(a3 + Vector3.up * 0.1f, b3 + Vector3.up * 0.1f, 0.03f, metal);
                    f.Box((a3 + b3) * 0.5f + Vector3.up * 3.04f, new Vector3(0.06f, 0.02f, len), Quaternion.LookRotation((b3 - a3).normalized), snow);
                }
                return;
            }
            for (int s = -1; s <= 1; s += 2)
            {
                Vector2 at = mid + d * (s * (half.x - 0.4f));
                if (!g.Square.Contains(at) || g.Blocked(at)) continue;
                Vector3 p = To3(at, 0f);
                RaidMesh m = g.Site.Town.Out.Solid(p);
                m.SetFrame(p, yaw + (s > 0 ? 180f : 0f));   // local +z: into the court
                m.Cylinder(Vector3.zero, 0.07f, 3.3f, 6, metal);
                m.Rod(new Vector3(0f, 3.0f, 0f), new Vector3(0f, 3.1f, 0.9f), 0.06f, metal);
                m.Box(new Vector3(0f, 3.25f, 0.95f), new Vector3(1.8f, 1.05f, 0.04f), white);
                m.Box(new Vector3(0f, 3.79f, 0.95f), new Vector3(1.8f, 0.03f, 0.07f), snow);
                Surf orange = DecorPalette.Metal(new Color(0.75f, 0.32f, 0.10f));
                m.Box(new Vector3(0f, 3.05f, 1.2f), new Vector3(0.46f, 0.02f, 0.03f), orange);
                m.Box(new Vector3(0f, 3.05f, 1.42f), new Vector3(0.46f, 0.02f, 0.03f), orange);
                m.Box(new Vector3(-0.22f, 3.05f, 1.31f), new Vector3(0.03f, 0.02f, 0.24f), orange);
                m.Box(new Vector3(0.22f, 3.05f, 1.31f), new Vector3(0.03f, 0.02f, 0.24f), orange);
                m.ResetFrame();
                g.Site.Town.PropCollider(p + Vector3.up * 1.6f, new Vector3(0.3f, 3.2f, 0.3f), 0f);
                g.Site.Take(at, 0.5f);
            }
        }

        // A ball diamond: home plate at the outline's squarest corner (diamonds are mapped as fans from it), a
        // chain-link backstop curving behind it and a players' bench along each baseline.
        private static void Diamond(Ground g, List<Vector2> whole, Vector2 mid, System.Random rng, in Surf snow)
        {
            List<Vector2> hull = Hull(whole);
            if (hull.Count < 3) return;
            int home = -1;
            float best = float.MaxValue;
            for (int i = 0; i < hull.Count; i++)
            {
                Vector2 a = hull[(i + hull.Count - 1) % hull.Count] - hull[i], b = hull[(i + 1) % hull.Count] - hull[i];
                if (a.magnitude < 15f || b.magnitude < 15f) continue;
                float angle = Vector2.Angle(a, b);
                float score = Mathf.Abs(angle - 90f) + (float)rng.NextDouble() * 3f;
                if (score < best)
                {
                    best = score;
                    home = i;
                }
            }
            if (home < 0 || best > 25f) return;
            Vector2 plate = hull[home];
            Vector2 toField = ((hull[(home + 1) % hull.Count] - plate).normalized + (hull[(home + hull.Count - 1) % hull.Count] - plate).normalized).normalized;
            plate += toField * 6f;   // home plate sits a little in from the corner of the mapped fan
            Vector2 back = -toField;
            Surf metal = DecorPalette.Metal(new Color(0.45f, 0.47f, 0.48f));
            Surf mesh = DecorPalette.Matte(new Color(0.47f, 0.49f, 0.50f));
            // Five panels on an arc of 7 m behind the plate, 4.5 m high.
            const int panels = 5;
            const float radius = 7f, height = 4.5f, arc = 110f;
            for (int i = 0; i < panels; i++)
            {
                float a0 = (-arc * 0.5f + arc * i / panels) * Mathf.Deg2Rad, a1 = (-arc * 0.5f + arc * (i + 1) / panels) * Mathf.Deg2Rad;
                Vector2 p0 = plate + Rotate(back, a0) * radius, p1 = plate + Rotate(back, a1) * radius;
                Vector2 c = (p0 + p1) * 0.5f;
                if (!g.Square.Contains(c) || g.Blocked(c)) continue;
                Vector3 q0 = To3(p0, 0f), q1 = To3(p1, 0f);
                RaidMesh m = g.Site.Town.Out.Solid(To3(c, 0f));
                m.Rod(q0, q0 + Vector3.up * height, 0.08f, metal);
                m.Rod(q1, q1 + Vector3.up * height, 0.08f, metal);
                m.Rod(q0 + Vector3.up * height, q1 + Vector3.up * height, 0.05f, metal);
                m.Rod(q0 + Vector3.up * 1.2f, q1 + Vector3.up * 1.2f, 0.04f, metal);
                float panel = Vector2.Distance(p0, p1);
                Quaternion along = Quaternion.LookRotation((q1 - q0).normalized);
                m.Box(To3(c, height * 0.5f + 0.05f), new Vector3(0.02f, height - 0.1f, panel), along, mesh);
                m.Box(To3(c, height + 0.04f), new Vector3(0.07f, 0.02f, panel), along, snow);
                float yaw = along.eulerAngles.y;
                g.Site.Town.PropCollider(To3(c, height * 0.5f), new Vector3(0.15f, height, panel), yaw);
                g.Site.Take(c, 0.6f);
            }
            // The benches along the baselines.
            Surf wood = DecorPalette.Matte(new Color(0.47f, 0.37f, 0.27f));
            for (int s = -1; s <= 1; s += 2)
            {
                Vector2 line = Rotate(toField, s * 45f * Mathf.Deg2Rad);
                Vector2 side = new Vector2(-line.y, line.x) * s;
                Vector2 at = plate + line * 11f + side * 5f;
                if (!g.Square.Contains(at) || g.Blocked(at)) continue;
                Vector3 p = To3(at, 0f);
                RaidMesh m = g.Site.Town.Out.Solid(p);
                m.SetFrame(p, Mathf.Atan2(line.x, line.y) * Mathf.Rad2Deg);
                m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.05f, 4.5f), wood);
                m.Box(new Vector3(0f, 0.475f, 0f), new Vector3(0.3f, 0.02f, 4.4f), snow);
                for (int k = -1; k <= 1; k++) m.Box(new Vector3(0f, 0.22f, k * 2f), new Vector3(0.3f, 0.44f, 0.06f), metal);
                m.ResetFrame();
                g.Site.Take(at, 0.5f);
            }
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
        #endregion

        #region Yards
        // Against the walls of warehouses and factories (and of anything in an industrial area): stacks of pallets,
        // groups of drums, crates and the odd dumpster, under snow, each group with one collider.
        private static void Yards(Ground g)
        {
            foreach (Footprint f in g.Site.Area.Buildings)
            {
                string kind = f.Data.kind ?? "";
                bool industrial = kind == "industrial" || kind == "warehouse";
                if (!industrial)
                {
                    Owner o = g.Square.Contains(f.Middle) ? g.OwnerAt(f.Middle) : null;
                    industrial = o != null && o.Kind == "industrial" && kind != "house" && kind != "church";
                }
                if (!industrial || f.Size < 150f) continue;
                System.Random rng = MapStyle.Rng(f.Data.id, SaltYard);
                int groups = 1 + (f.Size > 1500f ? 1 : 0) + rng.Next(0, 2);
                for (int k = 0; k < groups; k++) YardGroup(g, f, rng);
            }
        }

        private static void YardGroup(Ground g, Footprint f, System.Random rng)
        {
            List<Vector2> p = f.Pts;
            // A wall picked by its length: the long walls get the most.
            float total = 0f;
            for (int i = 0; i < p.Count; i++) total += Vector2.Distance(p[i], p[(i + 1) % p.Count]);
            float pick = (float)rng.NextDouble() * total;
            int e = 0;
            for (; e < p.Count; e++)
            {
                float len = Vector2.Distance(p[e], p[(e + 1) % p.Count]);
                if (pick <= len) break;
                pick -= len;
            }
            e = Mathf.Min(e, p.Count - 1);
            Vector2 a = p[e], b = p[(e + 1) % p.Count];
            float wall = Vector2.Distance(a, b);
            if (wall < 6f) return;
            Vector2 along = (b - a) / wall, outward = new Vector2(along.y, -along.x);   // anticlockwise outline: out is to the right
            Vector2 at = a + along * (wall * MapStyle.Range(rng, 0.2f, 0.8f)) + outward * MapStyle.Range(rng, 2.6f, 3.4f);
            if (!g.Clear(at, 1.4f)) return;
            float yaw = Mathf.Atan2(along.x, along.y) * Mathf.Rad2Deg - 90f;   // the group's local +x along the wall
            Vector3 c = To3(at, 0f);
            RaidMesh m = g.Site.Town.Out.Solid(c);
            m.SetFrame(c, yaw);
            Surf snow = DecorPalette.Matte(new Color(0.84f, 0.89f, 0.97f));
            float width = 0f, height = 0f;
            int what = rng.Next(4);
            if (what == 0)
            {
                // Pallets in two or three stacks.
                Surf wood = DecorMaterials.Planks(new Color(0.58f, 0.47f, 0.33f));
                int stacks = rng.Next(2, 4);
                for (int s = 0; s < stacks; s++)
                {
                    float h = 0.14f * rng.Next(3, 10), x = (s - (stacks - 1) * 0.5f) * 1.35f;
                    m.Box(new Vector3(x, h * 0.5f, MapStyle.Range(rng, -0.1f, 0.1f)), new Vector3(1.2f, h, 1.0f), MapStyle.Range(rng, -6f, 6f), wood);
                    m.Flat(new Vector3(x, h + 0.005f, 0f), 1.15f, 0.95f, 0f, snow);
                    height = Mathf.Max(height, h);
                }
                width = stacks * 1.35f;
            }
            else if (what == 1)
            {
                // Drums, standing in a cluster.
                Color[] drum = { new Color(0.17f, 0.29f, 0.52f), new Color(0.45f, 0.26f, 0.15f), new Color(0.12f, 0.12f, 0.13f), new Color(0.72f, 0.58f, 0.15f) };
                Color colour = drum[rng.Next(drum.Length)];
                int count = rng.Next(3, 8);
                for (int i = 0; i < count; i++)
                {
                    float x = (i % 4 - 1.5f) * 0.62f, z = (i / 4) * 0.62f - 0.3f;
                    Surf s = DecorPalette.Metal(rng.NextDouble() < 0.25 ? drum[rng.Next(drum.Length)] : colour);
                    m.Cylinder(new Vector3(x, 0f, z), 0.29f, 0.88f, 8, s);
                    m.Cylinder(new Vector3(x, 0.88f, z), 0.27f, 0.02f, 8, snow);
                }
                width = 2.6f;
                height = 0.9f;
            }
            else if (what == 2)
            {
                // Crates, some stacked.
                Surf wood = DecorMaterials.Planks(new Color(0.52f, 0.43f, 0.31f));
                int count = rng.Next(2, 5);
                float x = -1.2f;
                for (int i = 0; i < count; i++)
                {
                    float sz = MapStyle.Range(rng, 0.8f, 1.3f);
                    bool stacked = i > 0 && rng.NextDouble() < 0.3;
                    float y = stacked ? 1.0f : 0f;
                    if (!stacked) x += sz * 0.5f + 0.15f;
                    m.Box(new Vector3(x, y + sz * 0.5f, 0f), new Vector3(sz, sz, sz), MapStyle.Range(rng, -8f, 8f), wood);
                    m.Flat(new Vector3(x, y + sz + 0.005f, 0f), sz * 0.95f, sz * 0.95f, 0f, snow);
                    height = Mathf.Max(height, y + sz);
                    if (!stacked) x += sz * 0.5f;
                }
                width = Mathf.Max(1.5f, x + 1.2f);
            }
            else
            {
                DecorProps.Dumpster(m, rng);
                width = 1.9f;
                height = 1.3f;
            }
            m.ResetFrame();
            g.Site.Town.PropCollider(c + Vector3.up * (height * 0.5f), new Vector3(width + 0.2f, height, 1.2f), yaw);
            Take(g.Site, at, along, outward, new Vector2(width * 0.5f + 0.4f, 1f));
        }
        #endregion
    }
}
