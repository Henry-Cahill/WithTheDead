using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;
using static CloneZ.StreetscapePass;

namespace CloneZ
{
    /// <summary>
    /// The small things along the streets: fire hydrants (mapped, and at the town's corners and along its blocks, a town's
    /// in one colour), blue collection boxes, the kerbside mailbox of nearly every house out of the middle of town, benches,
    /// trash cans and newspaper boxes downtown, payphones and soda machines at the gas stations and stores, bollards, farm
    /// gates across the tracks and drives, bus stop signs. Mapped ones where they're mapped (nudged off the carriageway),
    /// heuristics elsewhere; each with a collider if it blocks, each claiming its spot.
    /// </summary>
    internal static class StreetFurniture
    {
        private const int SaltHydrant = 0x4D2A, SaltColour = 0x4D2B, SaltMail = 0x3A11, SaltMailLook = 0x3A12, SaltCorner = 0xC04E,
            SaltStore = 0x5704, SaltBench = 0xBE7C;
        private const float MidBlock = 120f;
        private static readonly Color[] HydrantColours =
        {
            new Color(0.62f, 0.10f, 0.08f), new Color(0.62f, 0.10f, 0.08f), new Color(0.84f, 0.66f, 0.12f), new Color(0.66f, 0.67f, 0.68f),
        };
        private static readonly Color[] MailboxColours =
        {
            new Color(0.08f, 0.08f, 0.08f), new Color(0.08f, 0.08f, 0.08f), new Color(0.62f, 0.63f, 0.64f), new Color(0.82f, 0.81f, 0.78f),
            new Color(0.20f, 0.28f, 0.20f), new Color(0.36f, 0.20f, 0.14f),
        };
        private static readonly Color UspsBlue = new Color(0.10f, 0.20f, 0.42f);
        private static readonly string[] Papers = { "paper_cj", "paper_hl", "paper_us", "paper_local" };
        private static readonly Color[] PaperBodies =
        {
            new Color(0.10f, 0.22f, 0.52f), new Color(0.85f, 0.85f, 0.82f), new Color(0.15f, 0.40f, 0.75f), new Color(0.85f, 0.75f, 0.15f),
        };

        internal static void Build(Ctx ctx)
        {
            var hydrants = new List<Vector2>();
            MappedNodes(ctx, hydrants);
            Hydrants(ctx, hydrants);
            Mailboxes(ctx);
            Downtown(ctx);
            Stores(ctx);
        }

        #region Placing
        // A spot for a mapped thing: off the carriageway if it was mapped on it, facing the nearest road.
        private static bool Settle(Ctx ctx, Vector2 at, float radius, out Vector2 spot, out Vector2 facing)
        {
            spot = at;
            facing = Vector2.up;
            Street s = NearestRoad(ctx, at, 25f, out Vector2 on, out Vector2 d);
            if (s != null)
            {
                Vector2 off = at - on;
                float dist = off.magnitude;
                Vector2 side = dist > 0.05f ? off / dist : Left(d);
                int sd = Vector2.Dot(side, Left(d)) >= 0f ? 1 : -1;
                if (dist < EdgeOf(s, sd) + 0.4f) spot = on + side * Roadside(s, sd);
                facing = -side;
            }
            if (!ctx.Inside(spot)) return false;
            if (ctx.Site.FreeBeside(spot, radius)) return true;
            for (int k = 1; k <= 4; k++)
            {
                Vector2 q = spot + Left(facing) * (k % 2 == 0 ? k * 0.6f : -k * 0.6f);
                if (!ctx.Site.FreeBeside(q, radius)) continue;
                spot = q;
                return true;
            }
            return false;
        }

        private static void Claim(Ctx ctx, Vector2 at, float radius) => ctx.Site.Take(at, radius);
        #endregion

        #region Mapped
        private static void MappedNodes(Ctx ctx, List<Vector2> hydrants)
        {
            foreach (Node n in ctx.Site.Area.Nodes)
            {
                if (!ctx.Inside(n.At)) continue;
                string k = n.Data.kind;
                switch (k)
                {
                    case "hydrant":
                        if (!Settle(ctx, n.At, 0.35f, out Vector2 h, out Vector2 hf)) break;
                        Hydrant(ctx, h, hf, HydrantColour(ctx, h));
                        hydrants.Add(h);
                        break;
                    case "post_box":
                        if (Settle(ctx, n.At, 0.45f, out Vector2 p, out Vector2 pf)) CollectionBox(ctx, p, pf);
                        break;
                    case "bench":
                        if (!Settle(ctx, n.At, 0.9f, out Vector2 b, out Vector2 bf)) break;
                        if (n.Data.dir >= 0) bf = Facing(n.Data.dir);
                        Bench(ctx, b, bf);
                        break;
                    case "waste_basket":
                        if (Settle(ctx, n.At, 0.4f, out Vector2 w, out Vector2 _)) TrashCan(ctx, w, MapStyle.Hash(n.Data.id, 1));
                        break;
                    case "telephone":
                        if (Settle(ctx, n.At, 0.5f, out Vector2 t, out Vector2 tf)) Payphone(ctx, t, tf);
                        break;
                    case "vending":
                        if (!InBuilding(ctx.Site.Area, n.At) && Settle(ctx, n.At, 0.7f, out Vector2 v, out Vector2 vf))
                            Vending(ctx, v, -vf, MapStyle.Hash(n.Data.id, 2));
                        break;
                    case "bollard":
                        if (ctx.Site.Free(n.At, 0.2f) || ctx.Site.FreeBeside(n.At, 0.2f)) Bollard(ctx, n.At, MapStyle.Hash(n.Data.id, 3));
                        break;
                    case "gate":
                        Gate(ctx, n);
                        break;
                    case "bus_stop":
                        if (Settle(ctx, n.At, 0.3f, out Vector2 s, out Vector2 sf)) BusStop(ctx, s, sf);
                        break;
                }
            }
        }

        private static Vector2 Facing(int dir)
        {
            float r = dir * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r));
        }

        private static bool InBuilding(Area area, Vector2 p)
        {
            foreach (Footprint f in area.Buildings)
                if (f.Bounds.Contains(p) && MapGeometry.Contains(f.Pts, p)) return true;
            return false;
        }
        #endregion

        #region Hydrants
        // A town's hydrants share their colours (red with white caps, yellow, silver): one choice per kilometre square.
        private static (Color body, Color cap) HydrantColour(Ctx ctx, Vector2 at)
        {
            Vector2 w = at + ctx.Origin;
            long cell = ((long)Mathf.FloorToInt(w.x / 1000f) << 32) ^ (uint)Mathf.FloorToInt(w.y / 1000f);
            Color body = HydrantColours[MapStyle.Pick(cell, SaltColour, HydrantColours.Length)];
            Color cap = MapStyle.Unit(cell, SaltColour + 1) < 0.5f ? new Color(0.85f, 0.85f, 0.82f) : body;
            return (body, cap);
        }

        // At most town corners, and every block or so along the town's streets.
        private static void Hydrants(Ctx ctx, List<Vector2> mapped)
        {
            bool Near(Vector2 p, float r)
            {
                foreach (Vector2 q in mapped) if ((q - p).sqrMagnitude < r * r) return true;
                return false;
            }
            foreach (Crossing c in ctx.Crossings)
            {
                if (!ctx.Inside(c.At) || c.TopRank < 2 || c.TopRank > 5 || !ctx.Density.Town(c.At)) continue;
                long key = ctx.KeyOf(c.At);
                if (MapStyle.Unit(key, SaltHydrant) > 0.62f || Near(c.At, 45f)) continue;
                if (!Corner(ctx, c, MapStyle.Pick(key, SaltCorner, 8), 0f, 0.35f, out Vector2 at, out Vector2 facing)) continue;
                Hydrant(ctx, at, facing, HydrantColour(ctx, at));
                mapped.Add(at);
            }
            foreach (Street s in ctx.Site.Area.Streets)
            {
                int rank = RoadRank(s);
                if (rank < 2 || rank > 5 || s.Data.tunnel || s.Data.bridge) continue;
                foreach ((Vector2 p, Vector2 d, long k) in StreetLights.Lattice(ctx, s.Pts, MidBlock))
                {
                    if (!ctx.Inside(p) || !ctx.Density.Town(p) || Near(p, 50f)) continue;
                    if (MapStyle.Unit(k ^ s.Data.id, SaltHydrant) > 0.6f) continue;
                    int side = (k & 1) == 0 ? 1 : -1;
                    Vector2 at = p + Left(d) * (side * Roadside(s, side));
                    if (!ctx.Site.FreeBeside(at, 0.35f) || ctx.NearestCrossing(at, 12f) != null) continue;
                    Hydrant(ctx, at, -Left(d) * side, HydrantColour(ctx, at));
                    mapped.Add(at);
                }
            }
        }

        private static void Hydrant(Ctx ctx, Vector2 at, Vector2 facing, (Color body, Color cap) look)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            m.SetFrame(foot, Yaw(facing));
            Surf body = Gloss(look.body), cap = Gloss(look.cap);
            m.Cylinder(Vector3.zero, 0.15f, 0.07f, 8, body);
            m.Cylinder(new Vector3(0f, 0.07f, 0f), 0.105f, 0.5f, 8, body);
            m.Cylinder(new Vector3(0f, 0.57f, 0f), 0.13f, 0.05f, 8, body);
            m.Ellipsoid(new Vector3(0f, 0.64f, 0f), new Vector3(0.22f, 0.15f, 0.22f), cap, 8, 4);
            m.Cylinder(new Vector3(0f, 0.7f, 0f), 0.03f, 0.06f, 5, cap);
            m.CylinderX(new Vector3(0f, 0.42f, 0f), 0.045f, 0.34f, 6, cap);
            m.Cylinder(new Vector3(0f, 0.38f, 0.08f), 0.065f, 0.08f, 8, cap, Quaternion.Euler(90f, 0f, 0f));
            m.Ellipsoid(new Vector3(0f, 0.71f, 0f), new Vector3(0.17f, 0.06f, 0.17f), SnowSurf, 8, 3);
            m.ResetFrame();
            ctx.Collider(at, 0f, new Vector3(0.36f, 0.75f, 0.36f), 0f);
            Claim(ctx, at, 0.35f);
        }
        #endregion

        #region Mailboxes
        // A mailbox on a post at the road's edge in front of nearly every house and trailer out of the middle of town.
        private static void Mailboxes(Ctx ctx)
        {
            var placed = new List<Vector2>();
            foreach (Footprint f in ctx.Site.Area.Buildings)
            {
                string k = f.Data.kind;
                if (k != "house" && k != "trailer" || !ctx.Inside(f.Middle) || f.Size < 30f) continue;
                if (ctx.Density.Downtown(f.Middle)) continue;
                bool town = ctx.Density.Town(f.Middle);
                if (MapStyle.Unit(f.Data.id, SaltMail) > (town ? 0.75f : 0.95f)) continue;
                Street s = NearestRoad(ctx, f.Middle, 40f, out Vector2 on, out Vector2 d, 1);
                if (s == null || RoadRank(s) > 5) continue;
                Vector2 toHouse = f.Middle - on;
                if (toHouse.sqrMagnitude < 1f) continue;
                Vector2 side = Vector2.Dot(toHouse, Left(d)) >= 0f ? Left(d) : -Left(d);
                int sd = Vector2.Dot(side, Left(d)) >= 0f ? 1 : -1;
                float shift = (MapStyle.Unit(f.Data.id, SaltMail + 1) < 0.5f ? -1f : 1f) * 2.4f;
                // At the kerb, or at the road's very edge out of town (where the carrier reaches it from the car).
                float off = Mathf.Max(RoadPass.KerbOffset(s, sd), EdgeOf(s, sd)) + (town ? 0.4f : 0.75f);
                Vector2 at = on + d * shift + side * off;
                bool crowded = false;
                foreach (Vector2 q in placed) if ((q - at).sqrMagnitude < 2.2f) crowded = true;
                if (crowded || !ctx.Inside(at) || !ctx.Site.FreeBeside(at, 0.3f)) continue;
                placed.Add(at);
                Mailbox(ctx, at, -side, MapStyle.Rng(f.Data.id, SaltMailLook));
            }
        }

        private static void Mailbox(Ctx ctx, Vector2 at, Vector2 facing, System.Random rng)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            m.SetFrame(foot, Yaw(facing));
            bool metalPost = rng.NextDouble() < 0.3;
            Surf post = metalPost ? Metal(new Color(0.40f, 0.41f, 0.42f)) : Matte(new Color(0.38f, 0.29f, 0.20f));
            m.Box(new Vector3(0f, 0.55f, -0.12f), metalPost ? new Vector3(0.05f, 1.1f, 0.05f) : new Vector3(0.09f, 1.1f, 0.09f), post);
            m.Box(new Vector3(0f, 1.08f, 0f), new Vector3(0.12f, 0.04f, 0.4f), post);
            Color c = MailboxColours[rng.Next(MailboxColours.Length)];
            Surf box = Gloss(c);
            // The tunnel-topped box, its door to the road, a red flag at its side.
            m.Box(new Vector3(0f, 1.18f, 0.02f), new Vector3(0.18f, 0.14f, 0.48f), box);
            m.Cylinder(new Vector3(0f, 1.25f, -0.22f), 0.09f, 0.48f, 8, box, Quaternion.Euler(90f, 0f, 0f), 1f);
            m.Box(new Vector3(0f, 1.2f, 0.265f), new Vector3(0.17f, 0.2f, 0.01f), Gloss(c * 0.85f));
            bool up = rng.NextDouble() < 0.3;
            Surf red = Gloss(new Color(0.75f, 0.10f, 0.06f));
            if (up) m.Box(new Vector3(0.1f, 1.38f, 0.1f), new Vector3(0.01f, 0.2f, 0.04f), red);
            else m.Box(new Vector3(0.1f, 1.22f, 0.18f), new Vector3(0.01f, 0.04f, 0.2f), red);
            m.Box(new Vector3(0f, 1.345f, 0.02f), new Vector3(0.14f, 0.025f, 0.46f), SnowSurf);
            m.ResetFrame();
            ctx.Collider(at, 0f, new Vector3(0.2f, 1.3f, 0.2f), 0f);
            Claim(ctx, at, 0.3f);
        }
        #endregion

        #region Downtown
        // Downtown's corners: a trash can, a bench, a row of newspaper boxes, a collection box, now and then a payphone;
        // and benches along its blocks.
        private static void Downtown(Ctx ctx)
        {
            bool box = false;
            foreach (Crossing c in ctx.Crossings)
            {
                if (!ctx.Inside(c.At) || !ctx.Density.Downtown(c.At)) continue;
                long key = ctx.KeyOf(c.At);
                int corner = MapStyle.Pick(key, SaltCorner, 8);
                if (MapStyle.Unit(key, SaltCorner + 1) < 0.65f && Corner(ctx, c, corner, 0.6f, 0.4f, out Vector2 t, out _))
                    TrashCan(ctx, t, key);
                if (MapStyle.Unit(key, SaltCorner + 2) < 0.55f && Corner(ctx, c, corner + 1, 0.5f, 1.2f, out Vector2 n, out Vector2 nf))
                    NewsBoxes(ctx, n, nf, key);
                if ((!box || MapStyle.Unit(key, SaltCorner + 3) < 0.25f) && Corner(ctx, c, corner + 2, 0.5f, 0.45f, out Vector2 u, out Vector2 uf))
                {
                    CollectionBox(ctx, u, uf);
                    box = true;
                }
                if (MapStyle.Unit(key, SaltCorner + 4) < 0.2f && Corner(ctx, c, corner + 3, 0.7f, 0.5f, out Vector2 p, out Vector2 pf))
                    Payphone(ctx, p, pf);
            }
            foreach (Street s in ctx.Site.Area.Streets)
            {
                int rank = RoadRank(s);
                if (rank < 2 || rank > 5 || s.Data.tunnel || s.Data.bridge) continue;
                foreach ((Vector2 p, Vector2 d, long k) in StreetLights.Lattice(ctx, s.Pts, 38f))
                {
                    if (!ctx.Inside(p) || !ctx.Density.Downtown(p) || MapStyle.Unit(k ^ s.Data.id, SaltBench) > 0.45f) continue;
                    int side = (k & 1) == 0 ? 1 : -1;
                    Vector2 at = p + Left(d) * (side * (Roadside(s, side) + 0.5f));
                    if (!ctx.Site.FreeBeside(at, 0.9f) || ctx.NearestCrossing(at, 10f) != null) continue;
                    Bench(ctx, at, -Left(d) * side);
                }
            }
        }

        // A spot on the sidewalk by one of a crossing's corners (corner index taken round its arms), a little way along
        // one of its streets past the junction's kerb return, <paramref name="back"/> behind the roadside line; facing
        // that street.
        private static bool Corner(Ctx ctx, Crossing c, int index, float back, float radius, out Vector2 at, out Vector2 facing)
        {
            at = c.At;
            facing = Vector2.up;
            int n = c.Arms.Count;
            if (n == 0) return false;
            for (int tries = 0; tries < n * 2; tries++)
            {
                int k = (index + tries) % n;
                JunctionArm a = c.Arms[k];
                int sign = ((index + tries) / n) % 2 == 0 ? 1 : -1;
                // Left of the arm's way is the street's side ApproachRight(k).
                int side = sign > 0 ? c.ApproachRight(k) : -c.ApproachRight(k);
                Vector2 q = c.At + a.Dir * (c.Setback(k) + 2f) + Left(a.Dir) * (sign * (Roadside(a.Street, side) + back));
                if (!ctx.Inside(q) || !ctx.Site.FreeBeside(q, radius)) continue;
                at = q;
                facing = -Left(a.Dir) * sign;
                return true;
            }
            return false;
        }

        private static void TrashCan(Ctx ctx, Vector2 at, long key)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            bool drum = MapStyle.Unit(key, 0x7A54) < 0.5f;
            Color c = drum ? new Color(0.14f, 0.30f, 0.18f) : new Color(0.10f, 0.10f, 0.10f);
            if (drum)
            {
                m.Cylinder(foot, 0.29f, 0.88f, 10, Gloss(c));
                m.Cylinder(foot + new Vector3(0f, 0.3f, 0f), 0.3f, 0.03f, 10, Gloss(c * 0.8f));
                m.Cylinder(foot + new Vector3(0f, 0.6f, 0f), 0.3f, 0.03f, 10, Gloss(c * 0.8f));
            }
            else
            {
                // A steel-mesh basket on a pedestal.
                m.Cylinder(foot, 0.08f, 0.25f, 6, Matte(c));
                m.Cylinder(foot + new Vector3(0f, 0.25f, 0f), 0.27f, 0.62f, 10, Metal(new Color(0.16f, 0.17f, 0.17f)), Quaternion.identity, 1.1f);
            }
            m.Cylinder(foot + new Vector3(0f, 0.88f, 0f), 0.25f, 0.04f, 10, SnowSurf);
            ctx.Collider(at, 0f, new Vector3(0.58f, 0.9f, 0.58f), 0f);
            Claim(ctx, at, 0.4f);
        }

        private static void Bench(Ctx ctx, Vector2 at, Vector2 facing)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            m.SetFrame(foot, Yaw(facing));
            Surf iron = Matte(new Color(0.08f, 0.09f, 0.08f));
            Surf wood = Matte(new Color(0.42f, 0.30f, 0.20f));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * 0.82f, 0.22f, 0.05f), new Vector3(0.06f, 0.44f, 0.5f), iron);
                m.Box(new Vector3(s * 0.82f, 0.62f, -0.2f), new Vector3(0.06f, 0.42f, 0.08f), iron);
            }
            for (int i = 0; i < 3; i++) m.Box(new Vector3(0f, 0.45f, 0.2f - i * 0.13f), new Vector3(1.72f, 0.035f, 0.1f), wood);
            for (int i = 0; i < 2; i++) m.Box(new Vector3(0f, 0.62f + i * 0.17f, -0.22f), new Vector3(1.72f, 0.09f, 0.03f), wood);
            m.Box(new Vector3(0f, 0.48f, 0.07f), new Vector3(1.62f, 0.03f, 0.36f), SnowSurf);
            m.ResetFrame();
            ctx.Block(at, new Vector3(1.75f, 0.8f, 0.55f), Yaw(facing));
            Claim(ctx, at, 0.9f);
        }

        // A row of coin-op newspaper boxes, the papers of the day in their windows.
        private static void NewsBoxes(Ctx ctx, Vector2 at, Vector2 facing, long key)
        {
            int count = 2 + MapStyle.Pick(key, 0x9E55, 3);
            Vector2 along = Left(facing);
            RaidMesh m = ctx.Low(at);
            for (int i = 0; i < count; i++)
            {
                int kind = MapStyle.Pick(key, 0x9E56 + i, Papers.Length);
                Vector2 p = at + along * ((i - (count - 1) * 0.5f) * 0.5f);
                Vector3 c = To3(p, 0.52f);
                float yaw = Yaw(facing);
                m.Box(c, new Vector3(0.44f, 1.04f, 0.4f), yaw, Gloss(PaperBodies[kind]));
                m.Upright(To3(p + facing * 0.201f, 0.74f), To3(facing, 0f), 0.4f, 0.5f, SignArt.Surf, SignArt.Cell(Papers[kind]));
                m.Box(To3(p, 1.055f), new Vector3(0.42f, 0.03f, 0.38f), yaw, SnowSurf);
            }
            ctx.Block(at, new Vector3(count * 0.5f, 1.05f, 0.42f), Yaw(facing));
            Claim(ctx, at, count * 0.3f);
        }

        // The blue collection box on its four feet, the eagle's place taken by U.S. MAIL.
        private static void CollectionBox(Ctx ctx, Vector2 at, Vector2 facing)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            m.SetFrame(foot, Yaw(facing));
            Surf blue = Gloss(UspsBlue);
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3(i % 2 == 0 ? -0.2f : 0.2f, 0.1f, i < 2 ? -0.2f : 0.2f), new Vector3(0.05f, 0.2f, 0.05f), Gloss(UspsBlue * 0.7f));
            m.Box(new Vector3(0f, 0.66f, 0f), new Vector3(0.52f, 0.92f, 0.5f), blue);
            m.CylinderX(new Vector3(0f, 1.12f, 0f), 0.26f, 0.52f, 10, blue, 0f);
            m.Box(new Vector3(0f, 1.0f, 0.26f), new Vector3(0.32f, 0.07f, 0.03f), Metal(new Color(0.6f, 0.6f, 0.6f)));
            m.ResetFrame();
            m.Upright(foot + To3(facing, 0f) * 0.252f + new Vector3(0f, 0.62f, 0f), To3(facing, 0f), 0.36f, 0.36f, SignArt.Surf, SignArt.Cell("usps"));
            m.Ellipsoid(foot + new Vector3(0f, 1.36f, 0f), new Vector3(0.5f, 0.1f, 0.42f), SnowSurf, 8, 3);
            ctx.Block(at, new Vector3(0.55f, 1.35f, 0.55f), Yaw(facing));
            Claim(ctx, at, 0.45f);
        }

        // A payphone on its pedestal: the shroud, the phone, TELEPHONE over it.
        private static void Payphone(Ctx ctx, Vector2 at, Vector2 facing)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            m.SetFrame(foot, Yaw(-facing));
            Surf steel = Metal(new Color(0.66f, 0.68f, 0.70f));
            m.Box(new Vector3(0f, 0.5f, -0.05f), new Vector3(0.1f, 1.0f, 0.1f), steel);
            m.Box(new Vector3(0f, 1.42f, -0.1f), new Vector3(0.62f, 0.86f, 0.03f), steel);
            for (int s = -1; s <= 1; s += 2) m.Box(new Vector3(s * 0.3f, 1.42f, 0.06f), new Vector3(0.02f, 0.86f, 0.32f), steel);
            m.Box(new Vector3(0f, 1.38f, -0.02f), new Vector3(0.24f, 0.48f, 0.14f), Matte(new Color(0.12f, 0.12f, 0.13f)));
            m.Box(new Vector3(0.08f, 1.45f, 0.06f), new Vector3(0.06f, 0.22f, 0.06f), Matte(new Color(0.05f, 0.05f, 0.05f)));
            m.Box(new Vector3(0f, 1.9f, 0f), new Vector3(0.64f, 0.18f, 0.36f), Gloss(new Color(0.12f, 0.24f, 0.55f)));
            m.Box(new Vector3(0f, 2.0f, 0f), new Vector3(0.62f, 0.03f, 0.34f), SnowSurf);
            m.ResetFrame();
            Vector3 front = foot + To3(-facing, 0f) * 0.185f + new Vector3(0f, 1.9f, 0f);
            m.Upright(front, To3(-facing, 0f), 0.6f, 0.15f, SignArt.Surf, SignArt.Cell("telephone"));
            ctx.Block(at, new Vector3(0.64f, 2f, 0.4f), Yaw(facing));
            Claim(ctx, at, 0.5f);
        }

        // A soda machine, its lit front to <paramref name="front"/>.
        private static void Vending(Ctx ctx, Vector2 at, Vector2 front, long key)
        {
            string[] faces = { "soda_red", "soda_blue", "soda_green" };
            Color[] bodies = { new Color(0.66f, 0.07f, 0.07f), new Color(0.10f, 0.20f, 0.55f), new Color(0.10f, 0.38f, 0.16f) };
            int kind = MapStyle.Pick(key, 0x50DA, faces.Length);
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            float yaw = Yaw(front);
            m.Box(foot + new Vector3(0f, 0.92f, 0f), new Vector3(0.95f, 1.84f, 0.8f), yaw, Gloss(bodies[kind]));
            Vector3 n = To3(front, 0f);
            m.Upright(foot + n * 0.401f + new Vector3(0f, 0.92f, 0f), n, 0.93f, 1.82f, SignArt.Surf, SignArt.Cell(faces[kind]));
            // The lit panel behind its plastic glows after dark.
            Vector3 side = Vector3.Cross(n, Vector3.up).normalized;
            m.Upright(foot + n * 0.404f + new Vector3(0f, 1.27f, 0f) - side * 0.12f, n, 0.5f, 1.12f, DecorPalette.Glow(new Color(0.85f, 0.82f, 0.70f)));
            m.Box(foot + new Vector3(0f, 1.855f, 0f), new Vector3(0.93f, 0.03f, 0.78f), yaw, SnowSurf);
            ctx.Block(at, new Vector3(0.95f, 1.85f, 0.8f), yaw);
            Claim(ctx, at, 0.7f);
        }

        private static void Bollard(Ctx ctx, Vector2 at, long key)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            if (MapStyle.Unit(key, 0xB011) < 0.6f)
            {
                Surf yellow = Gloss(new Color(0.86f, 0.70f, 0.10f));
                m.Cylinder(foot, 0.1f, 0.95f, 8, yellow);
                m.Ellipsoid(foot + new Vector3(0f, 0.95f, 0f), new Vector3(0.2f, 0.12f, 0.2f), yellow, 8, 3);
            }
            else m.Box(foot + new Vector3(0f, 0.45f, 0f), new Vector3(0.28f, 0.9f, 0.28f), Concrete);
            m.Ellipsoid(foot + new Vector3(0f, 1.0f, 0f), new Vector3(0.16f, 0.05f, 0.16f), SnowSurf, 6, 3);
            ctx.Collider(at, 0f, new Vector3(0.24f, 1f, 0.24f), 0f);
            Claim(ctx, at, 0.25f);
        }

        private static void BusStop(Ctx ctx, Vector2 at, Vector2 facing)
        {
            RaidMesh m = ctx.Low(at);
            m.Box(To3(at, 1.25f), new Vector3(0.06f, 2.5f, 0.045f), Metal(new Color(0.56f, 0.58f, 0.60f)));
            Vector2 along = Left(facing);
            Vector3 c = To3(at + along * 0.0f, 2.25f);
            m.Box(c, new Vector3(0.46f, 0.24f, 0.016f), Yaw(along), Metal(new Color(0.58f, 0.59f, 0.6f)));
            m.Upright(c + To3(along, 0f) * 0.0095f, To3(along, 0f), 0.46f, 0.24f, SignArt.Surf, SignArt.Cell("busstop"));
            m.Upright(c - To3(along, 0f) * 0.0095f, -To3(along, 0f), 0.46f, 0.24f, SignArt.Surf, SignArt.Cell("busstop"));
            ctx.Collider(at, 0f, new Vector3(0.1f, 2.4f, 0.1f), 0f);
            Claim(ctx, at, 0.3f);
        }
        #endregion

        #region Gates
        // A mapped gate across a track or a drive: a galvanised tube farm gate swung open on its post.
        private static void Gate(Ctx ctx, Node n)
        {
            Street s = null;
            Vector2 on = n.At, d = Vector2.right;
            float best = 1.2f;
            foreach (Street st in ctx.Site.Area.Streets)
            {
                if (st.Data.tunnel) continue;
                float dist = Nearest(st.Pts, n.At, out Vector2 q, out Vector2 sd);
                if (dist >= best) continue;
                best = dist;
                s = st;
                on = q;
                d = sd;
            }
            if (s == null) return;
            float half = Mathf.Clamp(HalfWidth(s), 0.6f, 2.5f);
            float side = MapStyle.Unit(n.Data.id, 0x6A7E) < 0.5f ? 1f : -1f;
            Vector2 hinge = on + Left(d) * ((half + 0.25f) * side);
            if (!ctx.Inside(hinge) || !ctx.Site.FreeBeside(hinge, 0.2f) && !ctx.Site.Free(hinge, 0.2f)) return;
            float leaf = half * 2f + 0.2f;
            // Open: swung back along the way, a little past square.
            float swing = 70f + 30f * MapStyle.Unit(n.Data.id, 0x6A7F);
            Vector2 closed = -Left(d) * side;
            float a = swing * Mathf.Deg2Rad * (MapStyle.Unit(n.Data.id, 0x6A80) < 0.5f ? 1f : -1f);
            var open = new Vector2(closed.x * Mathf.Cos(a) - closed.y * Mathf.Sin(a), closed.x * Mathf.Sin(a) + closed.y * Mathf.Cos(a));
            RaidMesh m = ctx.Low(hinge);
            Surf wood = Matte(PoleWoods[1]);
            m.Box(To3(hinge, 0.75f), new Vector3(0.18f, 1.5f, 0.18f), wood);
            Vector2 latch = on - Left(d) * ((half + 0.25f) * side);
            if (ctx.Inside(latch) && (ctx.Site.FreeBeside(latch, 0.2f) || ctx.Site.Free(latch, 0.2f)))
            {
                ctx.Low(latch).Box(To3(latch, 0.65f), new Vector3(0.16f, 1.3f, 0.16f), wood);
                ctx.Collider(latch, 0f, new Vector3(0.18f, 1.3f, 0.18f), 0f);
                Claim(ctx, latch, 0.2f);
            }
            Surf tube = Galvanised;
            Vector3 h3 = To3(hinge + open * 0.1f, 0f), e3 = To3(hinge + open * leaf, 0f);
            for (int r = 0; r < 5; r++)
            {
                float y = 0.2f + r * 0.27f;
                m.Rod(h3 + new Vector3(0f, y, 0f), e3 + new Vector3(0f, y, 0f), 0.035f, tube);
            }
            m.Rod(h3 + new Vector3(0f, 0.15f, 0f), h3 + new Vector3(0f, 1.35f, 0f), 0.04f, tube);
            m.Rod(e3 + new Vector3(0f, 0.15f, 0f), e3 + new Vector3(0f, 1.35f, 0f), 0.04f, tube);
            m.Rod(h3 + new Vector3(0f, 1.3f, 0f), Vector3.Lerp(h3, e3, 0.5f) + new Vector3(0f, 0.2f, 0f), 0.03f, tube);
            m.Rod(h3 + new Vector3(0f, 1.32f, 0f), e3 + new Vector3(0f, 1.32f, 0f), 0.02f, SnowSurf);
            ctx.Collider(hinge, 0f, new Vector3(0.2f, 1.5f, 0.2f), 0f);
            Claim(ctx, hinge, 0.25f);
        }
        #endregion

        #region Stores
        // At the gas stations and convenience stores: a payphone by the lot, soda machines and an ice chest along the
        // front wall.
        private static void Stores(Ctx ctx)
        {
            var done = new HashSet<Footprint>();
            foreach (Spot p in ctx.Site.Area.Points)
            {
                string k = p.Data.kind;
                if (k != "fuel" && k != "shop:convenience" && k != "shop:supermarket" && k != "shop:alcohol") continue;
                Footprint f = NearestBuilding(ctx.Site.Area, p.At, 30f);
                if (f != null && done.Add(f)) Store(ctx, f, k == "fuel");
            }
            foreach (Footprint f in ctx.Site.Area.Buildings)
            {
                string use = f.Data.use;
                if ((use == "fuel" || use == "shop:convenience") && done.Add(f)) Store(ctx, f, use == "fuel");
            }
        }

        private static Footprint NearestBuilding(Area area, Vector2 p, float within)
        {
            Footprint best = null;
            float bestD = within * within;
            foreach (Footprint f in area.Buildings)
            {
                if (f.Data.kind == "canopy") continue;
                float d = (f.Middle - p).sqrMagnitude;
                if (MapGeometry.Contains(f.Pts, p)) d = 0f;
                if (d >= bestD) continue;
                bestD = d;
                best = f;
            }
            return best;
        }

        private static void Store(Ctx ctx, Footprint f, bool fuel)
        {
            if (!ctx.Inside(f.Middle)) return;
            // The wall facing the nearest road.
            Street s = NearestRoad(ctx, f.Middle, 60f, out Vector2 on, out _);
            if (s == null) return;
            int edge = -1;
            float best = -2f;
            List<Vector2> pts = f.Pts;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                if ((b - a).sqrMagnitude < 9f) continue;
                Vector2 d = (b - a).normalized, outward = new Vector2(d.y, -d.x);
                float score = Vector2.Dot(outward, (on - (a + b) * 0.5f).normalized);
                if (score > best) { best = score; edge = i; }
            }
            if (edge < 0) return;
            Vector2 w0 = pts[edge], w1 = pts[(edge + 1) % pts.Count];
            Vector2 wd = (w1 - w0).normalized, wo = new Vector2(wd.y, -wd.x);
            float len = Vector2.Distance(w0, w1);
            long key = MapStyle.Hash(f.Data.id, SaltStore);
            int machines = 1 + MapStyle.Pick(key, 1, 2);
            for (int i = 0; i < machines; i++)
            {
                Vector2 at = w0 + wd * (len * 0.2f + i * 1.05f) + wo * 0.6f;
                if (ctx.Inside(at) && ctx.Site.FreeBeside(at, 0.55f)) Vending(ctx, at, wo, key + i);
            }
            if (fuel && MapStyle.Unit(key, 2) < 0.6f)
            {
                Vector2 at = w0 + wd * (len * 0.78f) + wo * 0.55f;
                if (ctx.Inside(at) && ctx.Site.FreeBeside(at, 0.6f)) IceChest(ctx, at, wo);
            }
            if (MapStyle.Unit(key, 3) < 0.75f)
            {
                Vector2 at = (w0 + w1) * 0.5f + wo * Mathf.Min(8f, Vector2.Distance((w0 + w1) * 0.5f, on) * 0.6f);
                if (ctx.Inside(at) && ctx.Site.FreeBeside(at, 0.5f)) Payphone(ctx, at, -wo);
            }
        }

        private static void IceChest(Ctx ctx, Vector2 at, Vector2 front)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh m = ctx.Low(at);
            float yaw = Yaw(front);
            m.Box(foot + new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 1.1f, 0.72f), yaw, Gloss(new Color(0.88f, 0.89f, 0.90f)));
            Vector3 n = To3(front, 0f);
            m.Upright(foot + n * 0.361f + new Vector3(0f, 0.8f, 0f), n, 0.9f, 0.3f, SignArt.Surf, SignArt.Cell("ice"));
            m.Box(foot + new Vector3(0f, 1.115f, 0f), new Vector3(1.28f, 0.03f, 0.7f), yaw, SnowSurf);
            ctx.Block(at, new Vector3(1.3f, 1.1f, 0.72f), yaw);
            Claim(ctx, at, 0.7f);
        }
        #endregion
    }
}
