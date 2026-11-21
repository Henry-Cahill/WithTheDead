using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's street furniture: wooden utility poles along every street, the town's street lights (most
    /// dead, a few still burning near the middle, each burning one a <see cref="StreetLamp"/> that can be switched),
    /// signposts along the main roads, hydrants along the town streets, a low metal fence round every parking lot, and
    /// litter in the parks (<see cref="ParkLitter"/>, built last of all from <see cref="MapGameplay.Build"/>). Everything
    /// stands where the occupancy grid is still free, roadside things just off the road (<see cref="Site.FreeBeside"/>),
    /// and takes its spot. After the hydrants this pass also builds the roadside snow piles (<see cref="SnowPass.Piles"/>)
    /// and the parked cars (<see cref="VehiclePass.ParkedCars"/>), so they claim their spots before the trees, as they
    /// always have.
    /// </summary>
    internal static class StreetscapePass
    {
        private const float PoleSpacing = 30f;   // wooden utility poles
        private const float PoleHeight = 3.0f;
        private const float PoleRadius = 0.07f;
        private const int MaxLights = 90;   // street lights
        private const int LitLights = 10;   // ...of which at most this many still burn, per 400 m square
        private const float SignpostSpacing = 60f;
        private const int MaxHydrants = 70;
        private const float HydrantSpacing = 30f;
        private const int MaxParkProps = 200;
        private const float FencePostInterval = 2f;   // parking lot fences
        private const float FenceGateSpan = 3f;   // a gap this wide or wider between posts is a gate
        private const float FenceLowerRail = 0.25f, FenceUpperRail = 0.40f, FenceLatch = 0.02f;
        private const int SaltSignpost = 0x5167, SaltLotFence = 0xFE7C;

        private static readonly Vector3 FencePost = new Vector3(0.04f, 0.5f, 0.04f);
        private static readonly Vector3 FenceCap = new Vector3(0.04f, 0.02f, 0.04f);
        private static readonly Vector3 FenceBar = new Vector3(0.02f, 0.02f, 0.02f);
        private static readonly Vector3 NoParkingSign = new Vector3(0.8f, 0.4f, 0.02f);
        private static readonly Color PoleWood = new Color(0.55f, 0.40f, 0.26f);
        private static readonly Color StreetLightColor = new Color(1f, 0.72f, 0.42f);
        private static readonly Color SignBoard = new Color(0.78f, 0.73f, 0.56f);   // aged wood

        // The bolts at the signpost board's corners, on its front.
        private static readonly Vector3[] SignBolts =
        {
            new Vector3(-0.56f, 1.68f, 0.095f), new Vector3(0.56f, 1.68f, 0.095f),
            new Vector3(-0.56f, 2.12f, 0.095f), new Vector3(0.56f, 2.12f, 0.095f),
        };

        private static Surf GreyMetal => DecorPalette.Matte(new Color(0.55f, 0.55f, 0.55f));
        private static Surf BoltMetal => DecorPalette.Matte(new Color(0.48f, 0.48f, 0.48f));

        internal static IEnumerable<string> Build(Site site)
        {
            UtilityPoles(site);
            yield return "utility poles";
            StreetLights(site);
            yield return "street lights";
            Signposts(site);
            yield return "signposts";
            Hydrants(site);
            yield return "hydrants";
            LotFences(site);
            yield return "lot fences";
            SnowPass.Piles(site);
            yield return "snow piles";
            VehiclePass.ParkedCars(site);
            yield return "parked cars";
        }

        // Wooden utility poles every 30 m at the roadside of every street, on one side of it (as the lines run), clear of
        // the roads and of whatever already stands.
        private static void UtilityPoles(Site site)
        {
            Surf wood = DecorMaterials.WoodFloor(PoleWood);
            var poleSize = new Vector3(PoleRadius * 2f, PoleHeight, PoleRadius * 2f);
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel) continue;
                float side = (s.Data.id & 1) == 0 ? -1f : 1f;
                float walked = PoleSpacing * 0.5f;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], b = s.Pts[i + 1];
                    float len = Vector2.Distance(a, b);
                    if (len < 0.05f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(-d.y, d.x);
                    for (; walked <= len; walked += PoleSpacing)
                    {
                        Vector2 at = a + d * walked + n * ((s.Width * 0.5f + 1.2f) * side);
                        if (!site.FreeBeside(at, 0.5f)) continue;
                        site.Take(at, 0.5f);
                        Vector2 toRoad = -n * side;
                        Vector3 at3 = To3(at, 0f);
                        RaidMesh m = site.Town.Out.Solid(at3);
                        m.SetFrame(at3, Mathf.Atan2(toRoad.x, toRoad.y) * Mathf.Rad2Deg);
                        m.Box(new Vector3(0f, PoleHeight * 0.5f, 0f), poleSize, wood);
                        m.Box(new Vector3(0f, PoleHeight - 0.75f, 0f), new Vector3(0.02f, 0.02f, 0.4f), wood);   // the cross-piece, across the wires
                        m.ResetFrame();
                        site.Town.PropCollider(at3 + Vector3.up * (PoleHeight * 0.5f), poleSize, 0f);
                    }
                    walked -= len;
                }
            }
        }

        // Street lights along the town's streets, every 34 m on alternate sides; one in twenty leans after a crash. The
        // ones nearest the middle are picked first, and a handful of them (a streamed piece's share of the fixed square's
        // handful) still burn, a quarter of those flickering and the three nearest casting shadows.
        private static void StreetLights(Site site)
        {
            var poles = new List<(Vector2 at, float yaw)>();
            foreach (Street s in site.Area.Streets)
            {
                if (!s.IsTownStreet || s.Data.tunnel || s.Data.bridge) continue;
                float side = (s.Data.id & 1) == 0 ? 1f : -1f, walked = 17f;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], b = s.Pts[i + 1];
                    float len = Vector2.Distance(a, b);
                    if (len < 0.05f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(-d.y, d.x);
                    for (; walked <= len; walked += 34f, side = -side)
                    {
                        Vector2 at = a + d * walked + n * ((s.Data.width * 0.5f + 1.1f) * side);
                        if (!site.FreeBeside(at, 0.8f)) continue;
                        Vector2 toRoad = -n * side;
                        poles.Add((at, Mathf.Atan2(toRoad.x, toRoad.y) * Mathf.Rad2Deg));
                        site.Take(at, 0.8f);
                    }
                    walked -= len;
                }
            }
            poles.Sort((p, q) => p.at.sqrMagnitude.CompareTo(q.at.sqrMagnitude));
            var rng = new System.Random(site.Town.Rng.Next());
            int litCount = 0;
            int litLights = site.Chunked ? Mathf.Max(1, Mathf.RoundToInt(LitLights * site.AreaShare)) : LitLights;
            var poleSize = new Vector3(0.3f, 5.5f, 0.3f);
            for (int i = 0; i < poles.Count && i < MaxLights; i++)
            {
                float lean = rng.NextDouble() < 0.05 ? MapStyle.Range(rng, 8f, 14f) : 0f;
                bool lit = litCount < litLights && lean == 0f && rng.NextDouble() < 0.8;
                Vector3 at = To3(poles[i].at, 0f);
                RaidMesh m = site.Town.Out.Solid(at);
                float poleYaw = poles[i].yaw + MapStyle.Range(rng, -5f, 5f);
                m.SetFrame(at, poleYaw);
                Vector3 lamp = m.ToMeshSpace(DecorProps.StreetLight(m, rng, lit, lean));
                m.ResetFrame();
                if (!lit)
                {
                    site.Town.PropCollider(at + Vector3.up * (poleSize.y * 0.5f), poleSize, poleYaw);
                    continue;
                }
                StreetLamp.Attach(site.Town, at, poleYaw, poleSize, lamp, StreetLightColor, 13f, 2.2f, rng.NextDouble() < 0.25, i < 3);
                litCount++;
            }
        }

        // Road signposts every 60 m along the main roads, at the roadside on alternate sides, the board towards the road.
        private static void Signposts(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel || s.Rank < 2) continue;
                float side = (s.Data.id & 1) == 0 ? -1f : 1f;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square))
                    foreach (Vector2 p in MapGeometry.SampleAlong(piece, SignpostSpacing))
                    {
                        Vector2 d = MapGeometry.DirectionAt(piece, p), n = new Vector2(-d.y, d.x);
                        Vector2 at = p + n * ((s.Width * 0.5f + 0.9f) * side);
                        Vector2 toRoad = -n * side;
                        side = -side;
                        if (!site.FreeBeside(at, 0.6f)) continue;
                        site.Take(at, 0.6f);
                        Signpost(site, at, Mathf.Atan2(toRoad.x, toRoad.y) * Mathf.Rad2Deg);
                    }
            }
        }

        // A metal pole with a bracket, an aged wooden board bolted to it facing <paramref name="yaw"/> (give or take 5
        // degrees), and a collider round the pole.
        private static void Signpost(Site site, Vector2 pos, float yaw)
        {
            System.Random rng = MapStyle.Rng(MapStyle.Key(pos), SaltSignpost);
            float facing = yaw + (float)(rng.NextDouble() * 10.0 - 5.0);
            Vector3 at = To3(pos, 0f);
            RaidMesh m = site.Town.Out.Solid(at);
            m.SetFrame(at, facing);
            m.Box(new Vector3(0f, 1.25f, 0f), new Vector3(0.1f, 2.5f, 0.1f), GreyMetal);
            m.Box(new Vector3(0f, 1.8f, 0.06f), new Vector3(0.2f, 0.1f, 0.02f), GreyMetal);
            float shade = (float)(rng.NextDouble() * 0.08 - 0.04);
            var board = new Color(Mathf.Clamp01(SignBoard.r + shade), Mathf.Clamp01(SignBoard.g + shade), Mathf.Clamp01(SignBoard.b + shade));
            m.Box(new Vector3(0f, 1.9f, 0.08f), new Vector3(1.2f, 0.5f, 0.02f), DecorPalette.Matte(board));
            foreach (Vector3 bolt in SignBolts) m.Box(bolt, new Vector3(0.02f, 0.02f, 0.02f), BoltMetal);
            m.ResetFrame();
            site.Town.PropCollider(at + Vector3.up * 1.25f, new Vector3(0.1f, 2.5f, 0.1f), 0f);
        }

        // Hydrants every 30 m along the town's streets, a little off the kerb, alternating sides as they're placed.
        private static void Hydrants(Site site)
        {
            var rng = new System.Random(site.Town.Rng.Next());
            int placed = 0;
            foreach (Street s in site.Area.Streets)
            {
                if (!s.IsTownStreet || s.Data.tunnel || s.Data.bridge) continue;
                float side = 1f;
                float walked = HydrantSpacing * 0.5f;
                for (int i = 0; i + 1 < s.Pts.Count && placed < MaxHydrants; i++)
                {
                    Vector2 a = s.Pts[i], b = s.Pts[i + 1];
                    float len = Vector2.Distance(a, b);
                    if (len < 0.05f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(-d.y, d.x);
                    for (; walked <= len && placed < MaxHydrants; walked += HydrantSpacing)
                    {
                        Vector2 at = a + d * walked + n * (s.Data.width * 0.5f + MapStyle.Range(rng, 0.6f, 1.2f)) * side;
                        if (!site.FreeBeside(at, 0.6f)) continue;
                        Vector2 toRoad = -n * side;
                        float yaw = Mathf.Atan2(toRoad.x, toRoad.y) * Mathf.Rad2Deg + MapStyle.Range(rng, -10f, 10f);
                        Vector3 at3 = To3(at, 0f);
                        RaidMesh m = site.Town.Out.Solid(at3);
                        m.SetFrame(at3, yaw);
                        DecorProps.Hydrant(m, rng);
                        m.ResetFrame();
                        site.Town.PropCollider(at3 + Vector3.up * 0.38f, new Vector3(0.44f, 0.76f, 0.44f), yaw);
                        site.Take(at, 0.6f);
                        placed++;
                        side = -side;
                    }
                    walked -= len;
                }
            }
        }

        // A low metal fence round every parking lot inside the square.
        private static void LotFences(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Patch p in site.Area.Areas)
            {
                if (p.Data.kind != "parking") continue;
                List<Vector2> inside = MapGeometry.ClipPolygon(p.Pts, square);
                if (inside.Count < 3) continue;
                var edge = new List<Vector2>(inside) { inside[0] };
                LotFence(site.Town.Out, edge, MapStyle.LotY + 0.002f, MapStyle.Rng(p.Data.id, SaltLotFence));
            }
        }

        // Capped posts every 2 m along the closed <paramref name="edge"/>, set by hand (a few centimetres off), with a
        // bar and two rails between them; a gate panel with a latch across a wider gap; a "No Parking" sign at the first
        // post.
        private static void LotFence(OutdoorBatches into, List<Vector2> edge, float y, System.Random rng)
        {
            var posts = new List<Vector2>(MapGeometry.SampleAlong(edge, FencePostInterval));
            if (posts.Count == 0) return;
            Surf metal = GreyMetal;
            foreach (Vector2 pos in posts)
            {
                var jitter = new Vector2(MapStyle.Range(rng, -0.05f, 0.05f), MapStyle.Range(rng, -0.05f, 0.05f));
                Vector3 post = To3(pos + jitter, y + FencePost.y * 0.5f);
                into.Solid(post).Box(post, FencePost, 0f, metal);
                Vector3 cap = To3(pos, y + FencePost.y + FenceCap.y * 0.5f);
                into.Solid(cap).Box(cap, FenceCap, 0f, metal);
            }

            Vector2 first = posts[0], next = posts[1 % posts.Count];
            float signYaw = -Mathf.Atan2(next.y - first.y, next.x - first.x) * Mathf.Rad2Deg;
            Vector3 sign = To3(first + new Vector2(Mathf.Cos(signYaw * Mathf.Deg2Rad), Mathf.Sin(signYaw * Mathf.Deg2Rad)) * 0.3f, y + 1.2f);
            into.Solid(sign).Box(sign, NoParkingSign, signYaw, metal);

            for (int i = 0; i < posts.Count; i++)
            {
                Vector2 a = posts[i], b = posts[(i + 1) % posts.Count];
                float span = Vector2.Distance(a, b);
                float yaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                if (span >= FenceGateSpan)
                {
                    Vector3 gate = To3((a + b) * 0.5f, y + FencePost.y * 0.5f);
                    into.Solid(gate).Box(gate, new Vector3(span, FencePost.y, FenceBar.z), yaw, metal);
                    Vector3 latch = gate + new Vector3(0f, FenceLatch * 2f, 0f);
                    into.Solid(latch).Box(latch, new Vector3(span * 0.6f, FenceLatch, FenceBar.z), yaw, metal);
                    continue;
                }
                if (span > FencePostInterval + 0.1f) continue;
                Vector3 bar = To3((a + b) * 0.5f, y + FencePost.y + FenceBar.y * 0.5f);
                into.Solid(bar).Box(bar, new Vector3(span, FenceBar.y, FenceBar.z), yaw, metal);
                for (int r = 0; r < 2; r++)
                {
                    Vector3 rail = To3((a + b) * 0.5f, y + (r == 0 ? FenceLowerRail : FenceUpperRail));
                    into.Solid(rail).Box(rail, new Vector3(span, FenceBar.y, FenceBar.z), yaw, metal);
                }
            }
        }

        /// <summary>Up to five trash bags or bits of litter in every park, anywhere still free, each with a small
        /// collider: the last thing a site gets (from <see cref="MapGameplay.Build"/>), after the trees and the places'
        /// signs have had their pick of the spots.</summary>
        internal static void ParkLitter(Site site)
        {
            var rng = new System.Random(site.Town.Rng.Next());
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            int placed = 0;
            foreach (Patch p in site.Area.Areas)
            {
                if (p.Data.kind != "park") continue;
                List<Vector2> inside = MapGeometry.ClipPolygon(p.Pts, square);
                if (inside.Count < 3) continue;
                MapGeometry.Extent(inside, out float x0, out float z0, out float x1, out float z1);
                int want = Mathf.Min(5, MaxParkProps - placed);
                for (int i = 0, tries = 0; i < want && tries < want * 6; ++tries)
                {
                    var at = new Vector2(MapStyle.Range(rng, x0, x1), MapStyle.Range(rng, z0, z1));
                    if (!MapGeometry.Contains(inside, at) || !site.Free(at, 0.7f)) continue;
                    float yaw = (float)rng.NextDouble() * 360f;
                    Vector3 at3 = To3(at, 0f);
                    RaidMesh m = site.Town.Out.Solid(at3);
                    m.SetFrame(at3, yaw);
                    if (rng.NextDouble() < 0.5) DecorProps.TrashBag(m, rng, Vector3.zero);
                    else DecorProps.Litter(m, rng, Vector3.zero, true);
                    m.ResetFrame();
                    site.Town.PropCollider(at3 + Vector3.up * 0.25f, new Vector3(0.4f, 0.4f, 0.4f), yaw);
                    site.Take(at, 0.7f);
                    placed++;
                    i++;
                }
                if (placed >= MaxParkProps) break;
            }
        }
    }
}
