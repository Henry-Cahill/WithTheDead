using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What stands in front of the buildings: a house's porch (a deck a step up, posts, a railing of balusters or a brick
    // knee wall, a shed roof under the snow, now and then a swing; a lamp by the door), or a concrete stoop; a mailbox on
    // the wall by a town house's door. A shop front's brick piers, cornice and sign band (the name painted on it, or the
    // trade), a striped canvas awning; a fading painted advertisement high on a downtown side wall. Porch decks and
    // steps are walked on (under the world, with colliders a step high); posts and railings hide with the walls facing
    // their way; porch roofs, awnings and signs go up with the roof, out of the NavMesh bake.
    internal static partial class BuildingPass
    {
        private const int SaltPorch = 0x9071, SaltSign = 0x5167, SaltGhost = 0x6057;

        private static readonly string[] Trades =
        {
            "HARDWARE", "DRUGS", "DRY GOODS", "FURNITURE", "JEWELRY", "SHOES", "BARBER SHOP", "CAFE", "BAKERY", "APPLIANCES",
            "FLORIST", "BOOKS", "ANTIQUES", "VARIETY", "GROCERY", "INSURANCE", "LAW OFFICE", "PHOTO STUDIO", "RADIO & TV",
            "FABRICS", "SPORTING GOODS", "GIFTS", "PAWN SHOP", "BEAUTY SHOP", "LOANS", "TAILOR",
        };

        private static readonly string[] Ghosts =
        {
            "FEED & SEED", "GENERAL MDSE", "DRY GOODS", "HARDWARE", "FURNITURE", "TOBACCO", "HOTEL", "LIVERY STABLE",
            "BUGGIES & WAGONS", "DRUGS", "LUMBER", "SHOES", "GROCERIES", "MERCANTILE CO",
        };

        // Sign colours: the board and the lettering.
        private static readonly Color[] SignBoards =
        {
            new Color(0.12f, 0.26f, 0.18f), new Color(0.08f, 0.08f, 0.09f), new Color(0.40f, 0.11f, 0.11f), new Color(0.88f, 0.84f, 0.72f),
            new Color(0.14f, 0.19f, 0.34f), new Color(0.92f, 0.92f, 0.90f),
        };
        private static readonly Color[] SignLetters =
        {
            new Color(0.93f, 0.88f, 0.70f), new Color(0.88f, 0.70f, 0.28f), new Color(0.95f, 0.93f, 0.88f), new Color(0.10f, 0.10f, 0.10f),
            new Color(0.95f, 0.93f, 0.88f), new Color(0.62f, 0.10f, 0.09f),
        };

        private static void Frontage(Job j)
        {
            Plan p = j.P;
            if (p.Storefront && p.Front >= 0 && !j.Party(p.Front)) ShopFront(j, p.Front);
            if (p.GhostSign) GhostSign(j);
            if (j.DoorEdge < 0) return;
            bool porch = p.Porch && j.Len[j.DoorEdge] >= 2.6f;
            if (porch) Porch(j);
            else if (p.Stoop || p.Arch == Arch.House) Stoop(j);
            if (p.Mailbox) WallMailbox(j);
            if (p.PorchLight || porch) DoorLamp(j);
        }

        #region Houses
        // A front porch along the door's wall: the whole wall, or a bay round the door.
        private static void Porch(Job j)
        {
            Plan p = j.P;
            int e = j.DoorEdge;
            float len = j.Len[e];
            System.Random rng = MapStyle.Rng(j.F.Data.id, SaltPorch);
            float s0, s1;
            if (p.FullPorch)
            {
                s0 = 0.15f;
                s1 = len - 0.15f;
                if (s1 - s0 > 14f)
                {
                    s0 = Mathf.Max(0.15f, j.DoorAt - 7f);
                    s1 = Mathf.Min(len - 0.15f, s0 + 14f);
                }
            }
            else
            {
                float w = Mathf.Clamp(len * 0.42f, 3.0f, 5.5f);
                s0 = Mathf.Clamp(j.DoorAt - w * 0.5f, 0.15f, len - 0.15f - w);
                s1 = s0 + w;
            }
            if (s1 - s0 < 2.2f) { Stoop(j); return; }
            float depth = p.PorchDepth;
            Vector2 o = j.Out[e], d = j.Dir[e];
            float yaw = j.Yaw(e);
            const float Deck = 0.18f;
            RaidMesh walls = j.Facing(e);
            Surf wood = DecorMaterials.WoodFloor(new Color(0.52f, 0.42f, 0.32f));
            Surf post = j.TrimS;

            // The deck and a step up to it before the door.
            Vector2 mid = j.At(e, (s0 + s1) * 0.5f) + o * (depth * 0.5f);
            var deckSize = new Vector3(s1 - s0, Deck, depth);
            Vector3 deck = To3(mid, Deck * 0.5f);
            j.Site.Town.Out.Solid(deck).Box(deck, deckSize, yaw, wood);
            if (j.Solid) Collider(j, deck, deckSize, yaw);
            Vector2 stepAt = j.At(e, j.DoorAt) + o * (depth + 0.18f);
            var stepSize = new Vector3(1.5f, Deck * 0.5f, 0.36f);
            Vector3 step = To3(stepAt, Deck * 0.25f);
            j.Site.Town.Out.Solid(step).Box(step, stepSize, yaw, wood);
            if (j.Solid) Collider(j, step, stepSize, yaw);

            // The roof: a shed roof from under the eave down to the posts.
            float wallTop = Mathf.Min(j.TopAt(e, j.DoorAt), GroundStorey);
            float high = p.Storeys > 1 ? GroundStorey - 0.05f : wallTop - p.Overhang * Mathf.Tan(p.Pitch * Mathf.Deg2Rad) - 0.14f;
            if (j.Parts == null) high = Mathf.Min(high, wallTop - 0.12f);
            high = Mathf.Max(high, 2.55f);
            float reach = depth + 0.2f;
            float low = Mathf.Max(2.3f, high - Mathf.Tan(12f * Mathf.Deg2Rad) * reach);
            float postTop = low + (high - low) * 0.2f / reach;
            int bays = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / 2.6f));
            float postSize = p.Style == HouseStyle.Bungalow || p.Style == HouseStyle.Foursquare ? 0.3f : 0.14f;
            for (int k = 0; k <= bays; k++)
            {
                float s = Mathf.Lerp(s0 + postSize * 0.5f, s1 - postSize * 0.5f, k / (float)bays);
                Vector2 at = j.At(e, s) + o * (depth - postSize * 0.5f - 0.02f);
                var size = new Vector3(postSize, postTop - Deck, postSize);
                Vector3 c = To3(at, Deck + size.y * 0.5f);
                walls.Box(c, size, yaw, post);
                if (j.Solid) Collider(j, c, size, yaw);
            }
            Railing(j, e, s0, s1, depth, Deck, postSize, rng);
            PorchRoof(j, e, s0 - 0.1f, s1 + 0.1f, high, low, reach);
            if (p.Swing && s1 - s0 >= 3.4f) Swing(j, e, s0, depth, Deck, postTop, rng);
            j.Site.Take(mid, Mathf.Max(s1 - s0, depth) * 0.5f + 0.4f);
        }

        // Rails between the posts (and back to the wall at the ends), balusters or a knee wall, open at the steps.
        private static void Railing(Job j, int e, float s0, float s1, float depth, float deck, float postSize, System.Random rng)
        {
            Plan p = j.P;
            RaidMesh m = j.Facing(e);
            Vector2 o = j.Out[e];
            float yaw = j.Yaw(e);
            bool knee = p.KneeWall;
            Surf rail = j.TrimS;
            Surf kneeSurf = j.WallS;
            float gapA = j.DoorAt - 0.8f, gapB = j.DoorAt + 0.8f;
            // Along the front.
            RailRun(j, m, j.At(e, s0) + o * (depth - postSize * 0.5f - 0.02f), j.Dir[e], s1 - s0, gapA - s0, gapB - s0, deck, yaw, knee, rail, kneeSurf);
            // The two ends, from the wall to the front corners.
            Vector2 inward = -o;
            float endYaw = -Mathf.Atan2(inward.y, inward.x) * Mathf.Rad2Deg;
            RailRun(j, m, j.At(e, s0) + o * (depth - postSize * 0.5f - 0.02f), inward, depth - 0.1f, -1f, -1f, deck, endYaw, knee, rail, kneeSurf);
            RailRun(j, m, j.At(e, s1) + o * (depth - postSize * 0.5f - 0.02f), inward, depth - 0.1f, -1f, -1f, deck, endYaw, knee, rail, kneeSurf);
        }

        private static void RailRun(Job j, RaidMesh m, Vector2 start, Vector2 dir, float len, float gapA, float gapB, float deck, float yaw, bool knee,
            in Surf rail, in Surf kneeSurf)
        {
            float a = 0f;
            while (a < len)
            {
                float b = gapA > a && gapA < len ? gapA : len;
                if (b - a > 0.2f)
                {
                    Vector2 mid = start + dir * ((a + b) * 0.5f);
                    if (knee)
                    {
                        var size = new Vector3(b - a, 0.85f, 0.26f);
                        Vector3 c = To3(mid, deck + 0.425f);
                        m.Box(c, size, yaw, kneeSurf);
                        m.Box(c + Vector3.up * 0.45f, new Vector3(b - a, 0.06f, 0.32f), yaw, rail);
                        if (j.Solid) Collider(j, c, size, yaw);
                    }
                    else
                    {
                        m.Box(To3(mid, deck + 0.86f), new Vector3(b - a, 0.06f, 0.08f), yaw, rail);
                        m.Box(To3(mid, deck + 0.1f), new Vector3(b - a, 0.05f, 0.07f), yaw, rail);
                        int count = Mathf.FloorToInt((b - a) / 0.22f);
                        Vector3 n = To3(new Vector2(-dir.y, dir.x), 0f);
                        for (int k = 1; k < count; k++)
                        {
                            Vector2 at = start + dir * (a + (b - a) * k / count);
                            Vector3 c = To3(at, deck + 0.48f);
                            m.Upright(c + n * 0.02f, n, 0.04f, 0.72f, rail);
                            m.Upright(c - n * 0.02f, -n, 0.04f, 0.72f, rail);
                        }
                        if (j.Solid) Collider(j, To3(mid, deck + 0.45f), new Vector3(b - a, 0.9f, 0.1f), yaw);
                    }
                }
                if (b >= len) break;
                a = gapB;
                gapA = -1f;
            }
        }

        private static void PorchRoof(Job j, int e, float s0, float s1, float high, float low, float reach)
        {
            Plan p = j.P;
            RaidMesh m = j.Upper;
            Vector2 o = j.Out[e];
            Vector3 a = To3(j.At(e, s0) + o * reach, low), b = To3(j.At(e, s1) + o * reach, low);
            Vector3 c = To3(j.At(e, s1), high), d = To3(j.At(e, s0), high);
            float pitch = Mathf.Atan2(high - low, reach) * Mathf.Rad2Deg;
            Vector3 n = SlopeNormal(To3(o, 0f), pitch);
            RoofFace4(j, m, a, b, c, d, n, low, high);
            Vector3 outward = To3(o, 0f);
            Eave(j, m, a, b, outward, To3(j.At(e, s0), low), To3(j.At(e, s1), low), n, low, high);
            // The ends: boards under the roof's edge.
            Vector3 along = To3(j.Dir[e], 0f);
            Beam(m, a - along * 0.02f, d - along * 0.02f, 0.18f, 0.05f, -along, j.TrimS);
            Beam(m, b + along * 0.02f, c + along * 0.02f, 0.18f, 0.05f, along, j.TrimS);
            j.Top = Mathf.Max(j.Top, high);
        }

        // A porch swing on chains near one end, and the fixture to sit on it.
        private static void Swing(Job j, int e, float s0, float depth, float deck, float top, System.Random rng)
        {
            Vector2 o = j.Out[e];
            float yaw = j.Yaw(e);
            RaidMesh m = j.Facing(e);
            Vector2 at = j.At(e, s0 + 1.1f) + o * (depth * 0.45f);
            float seatY = deck + 0.46f;
            Surf wood = DecorMaterials.WoodFloor(new Color(0.50f, 0.36f, 0.24f));
            Surf chain = DecorPalette.Metal(new Color(0.3f, 0.3f, 0.32f));
            Vector3 seat = To3(at, seatY);
            m.Box(seat, new Vector3(1.25f, 0.06f, 0.48f), yaw, wood);
            Vector3 back = seat + To3(-o, 0f) * 0.22f + Vector3.up * 0.24f;
            m.Box(back, new Vector3(1.25f, 0.42f, 0.05f), yaw, wood);
            Vector3 along = To3(j.Dir[e], 0f);
            for (int s = -1; s <= 1; s += 2)
                m.Rod(seat + along * (0.58f * s), seat + along * (0.58f * s) + Vector3.up * (top - seatY - 0.05f), 0.02f, chain);
            if (j.Scenery) return;
            var fixture = new GameObject("SwingInteractable");
            fixture.transform.SetParent(j.Site.Town.World, false);
            fixture.transform.position = seat;
            fixture.AddComponent<SwingInteractable>();
        }

        // A concrete step before the door.
        private static void Stoop(Job j)
        {
            Plan p = j.P;
            int e = j.DoorEdge;
            bool wide = p.Arch == Arch.Church || p.Arch == Arch.Courthouse || p.Arch == Arch.Civic || p.Arch == Arch.School;
            float w = wide ? DoubleDoorWidth + 1.4f : 1.6f, depth = wide ? 1.6f : 1.0f, h = p.Arch == Arch.Trailer ? 0.2f : 0.13f;
            Surf surf = p.Arch == Arch.Trailer ? DecorMaterials.WoodFloor(new Color(0.52f, 0.42f, 0.32f)) : DecorMaterials.Concrete;
            Vector2 at = j.At(e, j.DoorAt) + j.Out[e] * (depth * 0.5f);
            var size = new Vector3(w, h, depth);
            Vector3 c = To3(at, h * 0.5f);
            float yaw = j.Yaw(e);
            j.Site.Town.Out.Solid(c).Box(c, size, yaw, surf);
            if (j.Solid) Collider(j, c, size, yaw);
            if (p.Arch == Arch.Trailer)
            {
                // A trailer's wooden steps and their rail.
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 r = at + j.Dir[e] * (w * 0.5f - 0.05f) * s;
                    j.Facing(e).Box(To3(r, 0.5f), new Vector3(0.06f, 0.8f, 0.06f), yaw, surf);
                }
            }
            j.Site.Take(at, depth * 0.5f + 0.5f);
        }

        private static void WallMailbox(Job j)
        {
            int e = j.DoorEdge;
            float s = j.DoorAt + (j.DoorAt < j.Len[e] * 0.5f ? 1.0f : -1.0f);
            if (s < 0.3f || s > j.Len[e] - 0.3f) return;
            Color c = new[] { new Color(0.08f, 0.08f, 0.09f), new Color(0.13f, 0.22f, 0.16f), new Color(0.55f, 0.55f, 0.53f) }[MapStyle.Pick(j.F.Data.id, SaltPorch + 2, 3)];
            OnWallBox(j, j.Facing(e), e, s, 1.35f, 0.32f, 0.36f, 0.12f, DecorPalette.Metal(c));
            OnWallBox(j, j.Facing(e), e, s, 1.55f, 0.34f, 0.04f, 0.15f, DecorPalette.Metal(c * 0.8f));
        }

        // A lamp by the door: its glowing globe and, when the building's lamp is on, its light (PorchLamp).
        private static void DoorLamp(Job j)
        {
            int e = j.DoorEdge;
            float side = j.DoorAt < j.Len[e] * 0.5f ? 1f : -1f;
            float s = j.DoorAt + side * 0.85f;
            if (s < 0.3f || s > j.Len[e] - 0.3f) s = j.DoorAt;
            float y = j.P.Arch == Arch.Row || j.P.Arch == Arch.Store ? 2.55f : 2.05f;
            RaidMesh m = j.Facing(e);
            OnWallBox(j, m, e, s, y, 0.12f, 0.2f, 0.06f, DecorPalette.Metal(new Color(0.12f, 0.12f, 0.12f)));
            OnWallBox(j, m, e, s, y, 0.13f, 0.17f, 0.13f, DecorPalette.Glow(new Color(1f, 0.86f, 0.58f)), 0.06f);
            if (!j.P.PorchLight || j.Scenery) return;
            Vector2 at = j.At(e, s) + j.Out[e] * 0.3f;
            PorchLamp.Spawn(j.Site.Town.World, To3(at, y), MapStyle.Unit(j.F.Data.id, SaltPorch + 1) * 1000f);
        }
        #endregion

        #region Shops
        // Brick piers at the ends of the shop front, the cornice over it and the sign band above, an awning over the
        // glass.
        private static void ShopFront(Job j, int e)
        {
            Plan p = j.P;
            float len = j.Len[e];
            if (len < 3f) return;
            RaidMesh walls = j.Facing(e);
            RaidMesh up = j.Upper;
            Surf pier = p.Wall == WallKind.Brick || p.Wall == WallKind.Stone ? j.WallS : j.AccentS;
            OnWallBox(j, walls, e, 0.2f, GroundStorey * 0.5f, 0.4f, GroundStorey, 0.1f, pier);
            OnWallBox(j, walls, e, len - 0.2f, GroundStorey * 0.5f, 0.4f, GroundStorey, 0.1f, pier);
            if (p.Arch == Arch.Row)
            {
                // The storefront cornice and, above it, the sign band.
                OnWallBox(j, up, e, len * 0.5f, GroundStorey + 0.1f, len + 0.04f, 0.2f, 0.22f, j.AccentS);
                Sign(j, e, GroundStorey + 0.55f, 0.55f, len);
            }
            else
            {
                float band = p.WallHeight - 0.15f;
                Sign(j, e, Mathf.Max(GroundStorey + 0.4f, band - 0.45f), Mathf.Min(0.7f, band - GroundStorey - 0.1f), len);
            }
            if (p.Awning) Awning(j, e, 0.45f, len - 0.45f);
        }

        // The building's name (or its trade) on a board over the shop front.
        private static void Sign(Job j, int e, float y, float height, float len)
        {
            Plan p = j.P;
            if (height < 0.25f) return;
            string text = p.Sign;
            long id = j.F.Data.id;
            if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(p.Name)) text = p.Name.ToUpperInvariant();
            if (string.IsNullOrEmpty(text))
            {
                float u = MapStyle.Unit(id, SaltSign);
                if (u < 0.18f) return;
                text = u < 0.24f ? "FOR RENT" : Trades[MapStyle.Pick(id, SaltSign + 1, Trades.Length)];
            }
            if (text.Length > 22) text = text.Substring(0, 22).TrimEnd();
            int tone = Mathf.Clamp(p.SignTone, 0, SignBoards.Length - 1);
            Surf board = DecorMaterials.Sign(text, SignBoards[tone], SignLetters[tone], out float aspect);
            float w = Mathf.Min(len - 1.2f, height * aspect);
            float h = w / aspect;
            if (w < 0.8f) return;
            RaidMesh m = j.Upper;
            OnWallBox(j, m, e, len * 0.5f, y, w + 0.16f, h + 0.12f, 0.05f, DecorPalette.Matte(SignBoards[tone] * 0.8f));
            Vector2 c = j.At(e, len * 0.5f) + j.Out[e] * 0.055f;
            m.Upright(new Vector3(c.x, y, c.y), To3(j.Out[e], 0f), w, h, board, new Rect(0f, 0f, 1f, 1f));
            j.Top = Mathf.Max(j.Top, y + h * 0.5f);
        }

        // A striped canvas awning on its iron frame, sloping out over the sidewalk, a valance along its front and the snow
        // lying on its upper half.
        private static void Awning(Job j, int e, float s0, float s1)
        {
            Plan p = j.P;
            const float Top = 2.95f, Front = 2.35f, Reach = 1.15f;
            Vector2 o = j.Out[e];
            RaidMesh m = j.Upper;
            Surf canvas = AwningSurf(p.AwningTone);
            Vector3 a = To3(j.At(e, s0) + o * Reach, Front), b = To3(j.At(e, s1) + o * Reach, Front);
            Vector3 c = To3(j.At(e, s1), Top), d = To3(j.At(e, s0), Top);
            float pitch = Mathf.Atan2(Top - Front, Reach) * Mathf.Rad2Deg;
            Vector3 n = SlopeNormal(To3(o, 0f), pitch);
            Vector3 centre = (a + b + c + d) * 0.25f, upHalf = (d - a) * 0.5f;
            float width = s1 - s0;
            m.QuadUV(centre, Vector3.Cross(n, upHalf).normalized * (width * 0.5f), upHalf, canvas, new Rect(0f, 0f, width / AwningPeriod, 1f));
            Vector3 down = Vector3.down * 0.28f;
            Vector3 outward = To3(o, 0f);
            m.QuadUV((a + b) * 0.5f + down * 0.5f + outward * 0.005f, (b - a) * 0.5f, -down * 0.5f, canvas, new Rect(0f, 0f, width / AwningPeriod, 0.2f));
            Vector3 along = To3(j.Dir[e], 0f);
            Tri3(m, a, d, a + down, -along, canvas);
            Tri3(m, b, c, b + down, along, canvas);
            Surf iron = DecorPalette.Metal(new Color(0.2f, 0.2f, 0.21f));
            Beam(m, a + down, b + down, 0.04f, 0.04f, Vector3.up, iron);
            if (p.Snow != SnowCover.Bare)
                Slab(m, Face4(a, b, c, d), n, Front + (Top - Front) * 0.35f, Top, 0.06f);
        }

        // A painted advertisement, faded, high on a downtown building's exposed side wall.
        private static void GhostSign(Job j)
        {
            Plan p = j.P;
            int best = -1;
            for (int i = 0; i < j.N; i++)
            {
                if (j.Party(i) || i == p.Front || j.Len[i] < 7f) continue;
                if (best < 0 || j.Len[i] > j.Len[best]) best = i;
            }
            if (best < 0) return;
            float top = j.TopAt(best, j.Len[best] * 0.5f);
            if (top < j.FloorY(1) + 2.6f) return;
            long id = j.F.Data.id;
            string text = Ghosts[MapStyle.Pick(id, SaltGhost, Ghosts.Length)];
            Color bg = new[] { new Color(0.42f, 0.16f, 0.14f), new Color(0.16f, 0.20f, 0.30f), new Color(0.14f, 0.13f, 0.12f) }[MapStyle.Pick(id, SaltGhost + 1, 3)];
            Surf paint = DecorMaterials.Sign(text, bg, new Color(0.82f, 0.80f, 0.72f), out float aspect);
            float w = Mathf.Min(j.Len[best] - 2f, 2.2f * aspect), h = w / aspect;
            if (h > 2.4f) { h = 2.4f; w = h * aspect; }
            float y = Mathf.Min(top - 0.8f - h * 0.5f, j.FloorY(1) + 1.2f + h * 0.5f);
            Vector2 c = j.At(best, j.Len[best] * 0.5f) + j.Out[best] * 0.006f;
            j.Upper.Upright(new Vector3(c.x, y, c.y), To3(j.Out[best], 0f), w, h, paint, new Rect(0f, 0f, 1f, 1f));
        }
        #endregion

        private static List<Vector3> Face4(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var face = new List<Vector3>(4) { a, b, c, d };
            return face;
        }
    }
}
