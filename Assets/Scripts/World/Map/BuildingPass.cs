using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's buildings, from their mapped footprints. Inside the playable square each is enterable: walls
    /// with colliders up to the ground floor's ceiling, a door in the wall facing the nearest street, windows, a floor,
    /// loot to suit what the building is (<see cref="MapGameplay.Loot"/>) and shelter from the weather; the upper
    /// storeys, the ceiling and the snowy roof go on the overhead batches, so the isometric view looks into the rooms as
    /// it does in the procedural town. Past the edge of a fixed map they are scenery: whole and roofed, without
    /// colliders; buildings the edge cuts through aren't raised at all. The door frames and windows are in
    /// BuildingPass.Openings, the shop awnings, porches, mailboxes and fences in BuildingPass.Frontage, the eaves,
    /// chimneys and roof snow in BuildingPass.Roofs.
    /// </summary>
    internal static partial class BuildingPass
    {
        private const float GroundStorey = 3.0f;   // ground-floor walls (with colliders) stop here
        private const float CeilingY = 2.85f;
        private const float Storey = 3.0f;
        private const float Thick = 0.22f;
        private const float DoorWidth = 1.3f;
        private const float DoorHeight = 2.15f;
        private const int BuildingsPerStep = 6;   // a streamed piece spreads its buildings over several frames

        // Salts for the buildings' random choices (MapStyle).
        private const int SaltBuilding = 0x51ED, SaltSiding = 0x51D1, SaltBrick = 0xB41C, SaltStucco = 0x57CC,
            SaltRoofShade = 0x4F00, SaltRoofSnow = 0x5A0A, SaltCornerPosts = 0xFE4C;

        private static readonly Color[] SidingColors =
        {
            new Color(0.86f, 0.84f, 0.76f), new Color(0.80f, 0.82f, 0.80f), new Color(0.70f, 0.76f, 0.80f),
            new Color(0.74f, 0.76f, 0.66f), new Color(0.82f, 0.76f, 0.62f), new Color(0.92f, 0.92f, 0.90f),
            new Color(0.62f, 0.66f, 0.62f),
        };

        private static readonly Color[] BrickColors =
        {
            new Color(0.56f, 0.30f, 0.24f), new Color(0.62f, 0.36f, 0.28f), new Color(0.48f, 0.28f, 0.24f),
            new Color(0.66f, 0.50f, 0.40f),
        };

        /// <summary>The wood stain of the porches, rails, frames and posts.</summary>
        private static readonly Color Stain = new Color(0.55f, 0.40f, 0.26f);

        internal static IEnumerable<string> Build(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            int done = 0;
            foreach (Footprint f in site.Area.Buildings)
            {
                if (f.Pts.Count < 3 || f.Size < 6f) continue;
                if (f.Inside(half, 1f))
                {
                    Enterable(site, f, MapStyle.Rng(f.Data.id, SaltBuilding));
                    site.Buildings++;
                }
                else if (!f.Bounds.Overlaps(square))
                {
                    Scenery(site, f);
                    site.SceneryBuildings++;
                }
                if (Residential(f.Data.kind)) Mailbox(site, f);
                if (++done % BuildingsPerStep == 0) yield return "buildings";
            }
            Roofs(site);
            yield return "roofs";
            Extras(site);
            yield return "roof snow and fence posts";
        }

        private static void Enterable(Site site, Footprint f, System.Random rng)
        {
            MapBuilding b = f.Data;
            float height = b.kind == "ruin" ? 1.6f : HeightOf(b);
            Surf outside = ExteriorOf(b);
            if (b.kind == "canopy")
            {
                Canopy(site, f, outside);
                return;
            }
            List<Vector2> p = f.Pts;
            int door = DoorEdge(site, f);
            Vector2 doorPoint = new Vector2(float.MaxValue, float.MaxValue);
            float doorYaw = 0f;
            Vector2 doorOutward = Vector2.zero;
            float groundTop = Mathf.Min(height, GroundStorey);
            bool home = Residential(b.kind);
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 a = p[i], c = p[(i + 1) % p.Count];
                float len = Vector2.Distance(a, c);
                if (len < 0.05f) continue;
                Vector2 d = (c - a) / len, outward = new Vector2(d.y, -d.x);   // anticlockwise ring: outside is right
                float yaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                Vector2 inset = -outward * (Thick * 0.5f);   // the wall stands just inside the mapped outline
                if (i == door)
                {
                    float d0 = len * 0.5f - DoorWidth * 0.5f, d1 = len * 0.5f + DoorWidth * 0.5f;
                    Wall(site, a, d, inset, -Thick * 0.5f, d0, 0f, groundTop, yaw, outside, true);
                    Wall(site, a, d, inset, d1, len + Thick * 0.5f, 0f, groundTop, yaw, outside, true);
                    Wall(site, a, d, inset, d0, d1, DoorHeight, groundTop, yaw, outside, true);
                    doorPoint = a + d * (len * 0.5f);
                    doorYaw = yaw;
                    doorOutward = outward;
                    site.Take(doorPoint + outward * 1.2f, 1.1f);   // keep the doorstep clear of props
                    DoorFrame(site, a, d, len * 0.5f, yaw, outward, outside);
                    if (Retail(b)) Awning(site, doorPoint, doorOutward, doorYaw, b, rng);
                }
                else Wall(site, a, d, inset, -Thick * 0.5f, len + Thick * 0.5f, 0f, groundTop, yaw, outside, true);
                if (height > groundTop)
                    Wall(site, a, d, inset, -Thick * 0.5f, len + Thick * 0.5f, groundTop, height, yaw, outside, false);
                if (b.kind != "ruin" && b.kind != "silo") Windows(site, a, d, outward, len, height, i == door ? len * 0.5f : -1f, rng, outside);
                if (home && i != door) WallRail(site, a, d, outward, len, yaw);
            }
            if (door >= 0)
            {
                DoorKnob(site, doorPoint, doorOutward, doorYaw);
                if (b.kind == "house" || b.kind == "trailer") Porch(site, doorPoint, doorOutward, doorYaw, rng);
                if (home)
                {
                    Path(site, doorPoint, doorOutward, doorYaw);
                    PorchLight(site, b, doorPoint, doorOutward);
                }
            }

            List<int> tris = MapGeometry.Triangulate(p);
            Vector3 middle = To3(f.Middle, 0f);
            RaidMesh floor = site.Town.Out.Flat(middle);
            RaidMesh over = site.Overhead.At(middle);
            Surf floorSurf = FloorOf(b, rng);
            Surf roof = RoofSurf(b);
            float ceiling = Mathf.Min(CeilingY, height - 0.05f);
            bool roofed = b.kind != "ruin";
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                Vector2 A = p[tris[t]], B = p[tris[t + 1]], C = p[tris[t + 2]];
                floor.Tri(To3(A, 0.02f), To3(B, 0.02f), To3(C, 0.02f), floorSurf);   // face up; the navmesh covers it
                if (!roofed) continue;
                over.Tri(To3(A, height), To3(B, height), To3(C, height), roof);   // the snowy roof
                over.Tri(To3(A, ceiling), To3(C, ceiling), To3(B, ceiling), DecorMaterials.Ceiling);   // face down
            }
            if (roofed)
                Shelter.Register(site.ToWorld(new Bounds(new Vector3(f.Bounds.center.x, GroundStorey * 0.5f, f.Bounds.center.y),
                    new Vector3(Mathf.Max(0.5f, f.Bounds.width - 0.4f), GroundStorey, Mathf.Max(0.5f, f.Bounds.height - 0.4f)))));
            site.Take(f.Bounds);
            if (door >= 0) MapGameplay.Loot(site, f, rng, doorPoint);
        }

        private static void Wall(Site site, Vector2 a, Vector2 d, Vector2 inset, float s0, float s1, float y0, float y1,
            float yaw, in Surf surf, bool solid)
        {
            float len = s1 - s0, h = y1 - y0;
            if (len < 0.02f || h < 0.02f) return;
            Vector2 c = a + d * ((s0 + s1) * 0.5f) + inset;
            var center = new Vector3(c.x, (y0 + y1) * 0.5f, c.y);
            var size = new Vector3(len, h, Thick);
            if (solid)
            {
                site.Town.Out.Solid(center).Box(center, size, yaw, surf);
                site.Town.PropCollider(center, size, yaw);
            }
            else site.Overhead.At(center).Box(center, size, yaw, surf);
        }

        // A gas-station or drive-through canopy: posts with colliders, and the roof overhead.
        private static void Canopy(Site site, Footprint f, in Surf posts)
        {
            const float top = 4.2f;
            int step = Mathf.Max(1, f.Pts.Count / 4);
            for (int i = 0; i < f.Pts.Count; i += step)
            {
                Vector2 at = Vector2.MoveTowards(f.Pts[i], f.Middle, 0.4f);
                var center = To3(at, top * 0.5f);
                var size = new Vector3(0.35f, top, 0.35f);
                site.Town.Out.Solid(center).Box(center, size, posts);
                site.Town.PropCollider(center, size, 0f);
            }
            List<int> tris = MapGeometry.Triangulate(f.Pts);
            RaidMesh over = site.Overhead.At(To3(f.Middle, 0f));
            Surf underside = DecorPalette.Matte(new Color(0.82f, 0.82f, 0.80f));
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                Vector2 A = f.Pts[tris[t]], B = f.Pts[tris[t + 1]], C = f.Pts[tris[t + 2]];
                over.Tri(To3(A, top), To3(B, top), To3(C, top), DecorMaterials.Snow);
                over.Tri(To3(A, top - 0.25f), To3(C, top - 0.25f), To3(B, top - 0.25f), underside);
            }
        }

        // Past the edge: whole walls and a snowy roof, no colliders, no floor.
        private static void Scenery(Site site, Footprint f)
        {
            float height = HeightOf(f.Data);
            Surf outside = ExteriorOf(f.Data);
            Surf roof = RoofSurf(f.Data);
            RaidMesh m = site.Town.Far.Solid(To3(f.Middle, 0f));
            List<Vector2> p = f.Pts;
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 a = p[i], c = p[(i + 1) % p.Count];
                float len = Vector2.Distance(a, c);
                if (len < 0.05f) continue;
                Vector2 d = (c - a) / len, outward = new Vector2(d.y, -d.x);
                Vector2 mid = (a + c) * 0.5f - outward * (Thick * 0.5f);
                m.Box(To3(mid, height * 0.5f), new Vector3(len + Thick, height, Thick), -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, outside);
            }
            List<int> tris = MapGeometry.Triangulate(p);
            for (int t = 0; t + 2 < tris.Count; t += 3)
                m.Tri(To3(p[tris[t]], height), To3(p[tris[t + 1]], height), To3(p[tris[t + 2]], height), roof);
        }

        // Snow lying on three roofs in ten (inside the square and as scenery) and fence posts at the corners of one
        // building in four.
        private static void Extras(Site site)
        {
            var square = Rect.MinMaxRect(-site.Half, -site.Half, site.Half, site.Half);
            foreach (Footprint f in site.Area.Buildings)
            {
                if (f.Pts.Count < 3 || f.Size < 6f) continue;
                bool inside = f.Inside(site.Half, 1f);
                if (!inside && f.Bounds.Overlaps(square)) continue;
                if (MapStyle.Unit(f.Data.id, SaltRoofSnow) < 0.3f && (!inside || f.Data.kind != "ruin")) RoofSnow(site, f, inside);
                if (MapStyle.Unit(f.Data.id, SaltCornerPosts) < 0.25f) CornerPosts(site, f, inside);
            }
        }

        // The door goes in the longest wall facing the nearest street (or the longest wall, when none faces one);
        // -1 when no wall is long enough for a door.
        private static int DoorEdge(Site site, Footprint f)
        {
            Vector2 toStreet = site.Area.NearestStreetDirection(f.Middle, 60f);
            int best = -1;
            float bestScore = 0f;
            for (int i = 0; i < f.Pts.Count; i++)
            {
                Vector2 a = f.Pts[i], c = f.Pts[(i + 1) % f.Pts.Count];
                float len = Vector2.Distance(a, c);
                if (len < DoorWidth + 1.0f) continue;
                Vector2 d = (c - a) / len, outward = new Vector2(d.y, -d.x);
                float facing = toStreet == Vector2.zero ? 0.5f : Vector2.Dot(outward, toStreet);
                float score = len * (facing > 0.3f ? 1f + facing : 0.25f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>The building's height in metres: as mapped, or from its storeys and kind.</summary>
        internal static float HeightOf(MapBuilding b)
        {
            if (b.height > 0f) return Mathf.Clamp(b.height, 2.6f, 90f);
            float storeys = Mathf.Max(1, b.levels) * Storey;
            switch (b.kind)
            {
                case "shed": case "garage": case "canopy": return 2.7f;
                case "trailer": return 2.9f;
                case "barn": return 6.5f;
                case "warehouse": case "industrial": return Mathf.Max(storeys, 6.5f);
                case "church": return storeys + 3.5f;
                case "silo": return 14f;
                default: return storeys + 0.4f;   // a parapet above the top storey
            }
        }

        private static bool Residential(string kind) => kind == "house" || kind == "trailer" || kind == "apartments" || kind == "hotel";

        // The snowy roof: near-white, a shade lighter or darker from building to building.
        private static Surf RoofSurf(MapBuilding b)
        {
            float shade = 0.95f + 0.01f * MapStyle.Pick(b.id, SaltRoofShade, 5);
            return DecorPalette.Matte(new Color(shade, shade, shade));
        }

        // Siding for houses and trailers, red planks for barns, corrugated metal for industry, cinder block for garages,
        // sheds and ruins, brick or (one in four) stucco for the rest.
        private static Surf ExteriorOf(MapBuilding b)
        {
            switch (b.kind)
            {
                case "house": case "trailer":
                    return DecorMaterials.WallExterior(ExteriorStyle.Siding, SidingColors[MapStyle.Pick(b.id, SaltSiding, SidingColors.Length)]);
                case "barn":
                    return DecorMaterials.Planks(new Color(0.46f, 0.18f, 0.14f));
                case "industrial": case "warehouse": case "silo": case "greenhouse":
                    return DecorMaterials.Corrugated(new Color(0.56f, 0.58f, 0.60f));
                case "garage": case "shed": case "ruin": case "canopy":
                    return DecorMaterials.WallExterior(ExteriorStyle.CinderBlock, new Color(0.66f, 0.66f, 0.64f));
                default:
                    if (MapStyle.Unit(b.id, SaltStucco) < 0.25f)
                        return DecorMaterials.WallExterior(ExteriorStyle.Stucco, new Color(0.80f, 0.78f, 0.72f));
                    return DecorMaterials.WallExterior(ExteriorStyle.Brick, BrickColors[MapStyle.Pick(b.id, SaltBrick, BrickColors.Length)]);
            }
        }

        private static Surf FloorOf(MapBuilding b, System.Random rng)
        {
            switch (b.kind)
            {
                case "house": case "trailer": case "apartments": case "hotel":
                    return DecorMaterials.WoodFloor(Stain);
                case "industrial": case "warehouse": case "garage": case "shed": case "barn": case "silo": case "ruin":
                case "greenhouse":
                    return DecorMaterials.Concrete;
                default:
                    return DecorMaterials.Linoleum(new Color(0.78f, 0.76f, 0.70f), new Color(0.60f, 0.58f, 0.54f), rng.NextDouble() < 0.4);
            }
        }
    }
}
