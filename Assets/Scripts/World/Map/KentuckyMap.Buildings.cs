using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // The real-world map's buildings, from their mapped footprints. Inside the playable square each is enterable: walls
    // with colliders up to the ground floor's ceiling, a door in the wall facing the nearest street, windows, a floor,
    // loot to suit what the building is, and shelter from the weather; the upper storeys, the ceiling and the snowy
    // roof go on the overhead root, so the isometric view looks into the rooms as it does in the procedural town.
    // Past the edge they are scenery: whole and roofed, without colliders.
    public static partial class KentuckyMap
    {
        private const float GroundStorey = 3.0f;   // ground-floor walls (with colliders) stop here
        private const float CeilingY = 2.85f;
        private const float Storey = 3.0f;
        private const float Thick = 0.22f;
        private const float DoorWidth = 1.3f;
        private const float DoorHeight = 2.15f;
        private const float DoorFrameThickness = 0.08f; // thickness of side‑studs and lintel
        private const float DoorLintelHeight = 0.12f; // height of the top lintel piece
        private const int MaxContainers = 450;


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

        private const float EaveHeight = 0.12f; // height of eave trim piece
        private const float EaveDepth = 0.20f; // depth outward from roof edge
        private static Surf Glass => DecorPalette.Gloss(new Color(0.08f, 0.09f, 0.11f));

                private static void BuildRoofEaves(Site site)
        {
            // Add eave trim and a simple chimney to residential buildings (house, trailer, apartments, hotel, or use containing "residential")
            foreach (Footprint f in site.Area.Buildings)
            {
                // Consider residential kinds by use field containing "residential" or kind "house"/"trailer"/"apartments"/"hotel"
                string use = f.Data.use ?? "";
                bool isResidential = use.Contains("residential") ||
                                      f.Data.kind == "house" || f.Data.kind == "trailer" ||
                                      f.Data.kind == "apartments" || f.Data.kind == "hotel";
                if (!isResidential)
                    continue;

                // Determine roof height: use building height if known, else default 4.5f
                float roofY = f.Data.height > 0f ? f.Data.height : 4.5f;
                // Place eave at mid-height of the eave thickness
                float eaveY = roofY + EaveHeight * 0.5f;

                List<Vector2> pts = f.Pts;
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector2 a = pts[i];
                    Vector2 b = pts[(i + 1) % pts.Count];
                    Vector2 d = b - a;
                    float edgeLen = d.magnitude;
                    if (edgeLen < 0.05f) continue;
                    Vector2 n = new Vector2(d.y, -d.x).normalized; // outward normal (right side of anticlockwise)
                    Vector3 pos = To3((a + b) * 0.5f, eaveY);
                    // size: depth outward, height thin, length along edge
                    Vector3 size = new Vector3(EaveDepth, EaveHeight, edgeLen);
                    Quaternion rot = Quaternion.LookRotation(new Vector3(d.x, 0f, d.y));
                    // Use same roof material as building's roof surface
                    Surf roofMat = RoofSurf(f.Data, new System.Random(unchecked((int)f.Data.id) ^ 0x51ED));
                    site.Overhead.At(pos).Box(pos, size, rot, roofMat);
                    // Add a thin snow drift just above the eave for winter visual effect
                    Vector3 snowSize = new Vector3(EaveDepth, 0.02f, edgeLen);
                    Vector3 snowPos = pos + new Vector3(0f, EaveHeight * 0.5f + 0.01f, 0f);
                    site.Overhead.At(snowPos).Box(snowPos, snowSize, rot, DecorMaterials.Snow);
                }

                // Add a simple brick chimney on the roof
                // Basic dimensions – can be tweaked later
                const float chimneyBase = 0.3f;
                const float chimneyHeight = 1.5f;
                // Choose a deterministic random offset for variety
                var rng = new System.Random(unchecked((int)f.Data.id) ^ 0x5A4F);
                // Position near the centre of the footprint (could be refined to a corner)
                Vector3 chimneyPos = To3(f.Middle, roofY + chimneyHeight * 0.5f);
                // Choose a brick material (deterministic colour from BrickColors)
                Surf brickMat = DecorMaterials.WallExterior(ExteriorStyle.Brick, BrickColors[rng.Next(BrickColors.Length)]);
                site.Overhead.At(chimneyPos).Box(chimneyPos, new Vector3(chimneyBase, chimneyHeight, chimneyBase), Quaternion.identity, brickMat);
            }
        }
        private static void BuildBuildings(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Footprint f in site.Area.Buildings)
            {
                if (f.Pts.Count < 3 || f.Size < 6f) continue;
                var rng = new System.Random(unchecked((int)f.Data.id) ^ 0x51ED);
                if (f.Inside(half, 1f))
                {
                    Enterable(site, f, rng);
                    site.Buildings++;
                }
                else if (!f.Bounds.Overlaps(square))
                {
                    Scenery(site, f, rng);
                    site.SceneryBuildings++;
                }
BuildRoofEaves(site);
                // Add a small wooden mailbox for residential footprints (house, trailer, apartments, hotel)
                if (f.Data.kind == "house" || f.Data.kind == "trailer" || f.Data.kind == "apartments" || f.Data.kind == "hotel")
                {
                    // Choose a point near the first edge, offset outward from the wall
                    Vector2 a = f.Pts[0];
                    Vector2 b = f.Pts[1];
                    Vector2 d = (b - a).normalized;
                    Vector2 outward = new Vector2(d.y, -d.x); // outward normal (right side of anticlockwise ring)
                    Vector2 pos2D = (a + b) * 0.5f + outward * (Thick * 0.5f + 0.5f);
                    Vector3 pos = new Vector3(pos2D.x, 0f, pos2D.y);
                    // Add mailbox geometry (small wooden mailbox, not a public box)
                    var mesh = site.Town.Out.Solid(pos);
                    DecorProps.Mailbox(mesh, rng, false);
                }
                }

        }

        private static void BuildDoorFrame(Site site, Vector2 a, Vector2 d, float doorPos, float doorYaw, Vector2 outward, Surf exterior)
        {
            // a = start of wall edge, d = unit direction along edge, doorPos = centre of door from a
            // side studs
            float halfWidth = DoorWidth * 0.5f;
            Vector2 left = a + d * (doorPos - halfWidth);
            Vector2 right = a + d * (doorPos + halfWidth);
            Vector3 offset = new Vector3(outward.x, 0f, outward.y) * (Thick * 0.5f + DoorFrameThickness * 0.5f);
            // left stud
            Vector3 leftCenter = To3(left, DoorHeight * 0.5f) + offset;
            Vector3 leftSize = new Vector3(DoorFrameThickness, DoorHeight, DoorFrameThickness);
            site.Town.Out.Solid(leftCenter).Box(leftCenter, leftSize, doorYaw, exterior);
            site.Town.PropCollider(leftCenter, leftSize, doorYaw);
            // right stud
            Vector3 rightCenter = To3(right, DoorHeight * 0.5f) + offset;
            Vector3 rightSize = leftSize;
            site.Town.Out.Solid(rightCenter).Box(rightCenter, rightSize, doorYaw, exterior);
            site.Town.PropCollider(rightCenter, rightSize, doorYaw);
            // top lintel
            Vector3 lintelCenter = To3(a + d * doorPos, DoorHeight + DoorLintelHeight * 0.5f) + offset;
            Vector3 lintelSize = new Vector3(DoorWidth + 2f * DoorFrameThickness, DoorLintelHeight, DoorFrameThickness);
            site.Town.Out.Solid(lintelCenter).Box(lintelCenter, lintelSize, doorYaw, exterior);
            site.Town.PropCollider(lintelCenter, lintelSize, doorYaw);
        }

        private static void Enterable(Site site, Footprint f, System.Random rng)
        {
            MapBuilding b = f.Data;
            float height = b.kind == "ruin" ? 1.6f : HeightOf(b);
            Surf outside = ExteriorOf(b, rng);
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
                        // Build deterministic wooden door frame around the entrance
                        BuildDoorFrame(site, a, d, len * 0.5f, yaw, outward, outside);
                }
                else Wall(site, a, d, inset, -Thick * 0.5f, len + Thick * 0.5f, 0f, groundTop, yaw, outside, true);
                if (height > groundTop)
                    Wall(site, a, d, inset, -Thick * 0.5f, len + Thick * 0.5f, groundTop, height, yaw, outside, false);
                if (b.kind != "ruin" && b.kind != "silo") Windows(site, a, d, outward, len, height, i == door ? len * 0.5f : -1f, rng, outside);

                // Add low wooden rail fence for residential buildings (house, trailer, apartments, hotel)
                if (b.kind == "house" || b.kind == "trailer" || b.kind == "apartments" || b.kind == "hotel")
                {
                    if (i == door) continue; // skip door segment to keep opening
                    const float railHeight = 0.08f;
                    const float railThickness = 0.04f;
                    // Position rail just outside the wall edge
                    Vector3 railCenter = To3(a + d * ((len) * 0.5f), railHeight * 0.5f) +
                                         new Vector3(outward.x, 0f, outward.y) * (Thick * 0.5f + railThickness * 0.5f);
                    Vector3 railSize = new Vector3(len, railHeight, railThickness);
                    var railMat = DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f));
                    site.Town.Out.Solid(railCenter).Box(railCenter, railSize, yaw, railMat);
                    site.Town.PropCollider(railCenter, railSize, yaw);
                }
            }
            // Add a simple door knob to every entrance
            if (door >= 0)
            {
                float knobDepth = 0.02f; // front‑to‑back thickness of the knob
                float knobRadius = 0.07f; // radius of the knob (half width/height)
                Vector3 knobCenter = To3(doorPoint + doorOutward * (Thick * 0.5f + knobDepth * 0.5f), DoorHeight * 0.5f);
                Vector3 knobSize = new Vector3(knobDepth, knobRadius * 2f, knobRadius * 2f);
                Surf knobMat = DecorPalette.Gloss(new Color(0.8f, 0.8f, 0.8f));
                site.Town.Out.Solid(knobCenter).Box(knobCenter, knobSize, doorYaw, knobMat);
            }
            // Add simple front porch/stoop for residential buildings
            if (door >= 0 && (b.kind == "house" || b.kind == "trailer"))
            {
                Vector3 porchCenter = To3(doorPoint + doorOutward * 0.4f, 0.1f);
                Vector3 porchSize = new Vector3(1.5f, 0.2f, 0.8f);
                site.Town.Out.Solid(porchCenter).Box(porchCenter, porchSize, doorYaw, DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f)));
            }

            // Add short concrete stepping‑stone path for residential buildings (house, trailer, apartments, hotel)
            if (door >= 0 && (b.kind == "house" || b.kind == "trailer" || b.kind == "apartments" || b.kind == "hotel"))
            {
                const float pathLength = 1.0f;
                const float pathWidth = 0.3f;
                const float pathHeight = 0.02f;
                // Center positioned outward from door, after porch offset (0.4) plus half the path length
                Vector3 pathCenter = To3(doorPoint + doorOutward * (0.4f + pathLength * 0.5f), 0.01f);
                Vector3 pathSize = new Vector3(pathWidth, pathHeight, pathLength);
                site.Town.Out.Solid(pathCenter).Box(pathCenter, pathSize, doorYaw, DecorMaterials.Concrete);
                site.Town.PropCollider(pathCenter, pathSize, doorYaw);
            }

            List<int> tris = MapGeometry.Triangulate(p);
            Vector3 middle = To3(f.Middle, 0f);
            RaidMesh floor = site.Town.Out.Flat(middle);
            RaidMesh over = site.Overhead.At(middle);
            Surf floorSurf = FloorOf(b, rng);
            float ceiling = Mathf.Min(CeilingY, height - 0.05f);
            bool roofed = b.kind != "ruin";
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                Vector2 A = p[tris[t]], B = p[tris[t + 1]], C = p[tris[t + 2]];
                floor.Tri(To3(A, 0.02f), To3(B, 0.02f), To3(C, 0.02f), floorSurf);   // face up; the navmesh covers it
                if (!roofed) continue;
                over.Tri(To3(A, height), To3(B, height), To3(C, height), RoofSurf(b, rng));   // the snowy roof
                over.Tri(To3(A, ceiling), To3(C, ceiling), To3(B, ceiling), DecorMaterials.Ceiling);   // face down
            }
            if (roofed)
                Shelter.Register(new Bounds(new Vector3(f.Bounds.center.x, GroundStorey * 0.5f, f.Bounds.center.y),
                    new Vector3(Mathf.Max(0.5f, f.Bounds.width - 0.4f), GroundStorey, Mathf.Max(0.5f, f.Bounds.height - 0.4f))));
            site.Take(f.Bounds);
            if (door >= 0) Loot(site, f, rng, doorPoint);
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

        // Dark panes about every 3.2 m along a wall, on every storey, clear of the door.
        private static void Windows(Site site, Vector2 a, Vector2 d, Vector2 outward, float len, float height, float doorAt, System.Random rng, Surf outside)
        {
            if (len < 2.4f) return;
            int count = Mathf.Max(1, Mathf.FloorToInt(len / 3.2f));
            float step = len / count;
            Vector3 normal = To3(outward, 0f);
            for (int k = 0; k < count; k++)
            {
                float s = step * (k + 0.5f);
                if (doorAt >= 0f && Mathf.Abs(s - doorAt) < 1.5f) continue;
                Vector2 at = a + d * s + outward * 0.012f;
            for (float y = 1.55f; y + 0.7f < height; y += Storey)
            {
                Vector3 c = To3(at, y);
                RaidMesh m = y < GroundStorey ? site.Town.Out.Solid(c) : site.Overhead.At(c);
                m.Upright(c, normal, 1.0f, 1.15f, Glass);
                float sillHeight = 0.08f;
                float sillDepth = 0.02f + 0.02f * (float)rng.NextDouble(); // slight random variation
                var sillCenter = To3(at, y - sillHeight * 0.5f);
                var sillSize = new Vector3(1.0f, sillHeight, sillDepth);
                site.Town.Out.Solid(sillCenter).Box(sillCenter, sillSize, outside);

                // Add simple wooden shutters on each side of the window (outside face)
                const float shutterWidth = 0.26f;
                const float shutterDepth = 0.03f;
                float shutterHeight = Storey - 0.2f; // approximate window height
                // offset from window centre to place shutters
                float offset = 0.5f * 1.0f + 0.07f + shutterWidth * 0.5f;
                // outward offset so shutters sit slightly in front of the wall
                Vector3 outward3 = To3(outward, 0f);
                Vector3 basePos = To3(at, y);
                // first shutter on +d side
                Vector3 center1 = basePos + new Vector3(d.x, 0f, d.y) * offset + outward3 * (Thick * 0.5f + shutterDepth * 0.5f);
                // second shutter on -d side
                Vector3 center2 = basePos - new Vector3(d.x, 0f, d.y) * offset + outward3 * (Thick * 0.5f + shutterDepth * 0.5f);
                var shutterSize = new Vector3(shutterWidth, shutterHeight, shutterDepth);
                site.Town.Out.Solid(center1).Box(center1, shutterSize, outside);
                site.Town.Out.Solid(center2).Box(center2, shutterSize, outside);
                }
            }
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
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                Vector2 A = f.Pts[tris[t]], B = f.Pts[tris[t + 1]], C = f.Pts[tris[t + 2]];
                over.Tri(To3(A, top), To3(B, top), To3(C, top), DecorMaterials.Snow);
                over.Tri(To3(A, top - 0.25f), To3(C, top - 0.25f), To3(B, top - 0.25f), DecorPalette.Matte(new Color(0.82f, 0.82f, 0.80f)));
            }
        }

        // Past the edge: whole walls and a snowy roof, no colliders, no floor.
        private static void Scenery(Site site, Footprint f, System.Random rng)
        {
            float height = HeightOf(f.Data);
            Surf outside = ExteriorOf(f.Data, rng);
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
                m.Tri(To3(p[tris[t]], height), To3(p[tris[t + 1]], height), To3(p[tris[t + 2]], height), RoofSurf(f.Data, rng));
        }

        // The door goes in the longest wall facing the nearest street (or the longest wall, when none faces one);
        // -1 when no wall is long enough for a door.
        private static int DoorEdge(Site site, Footprint f)
        {
            Vector2 toStreet = NearestStreetDirection(site, f.Middle, 60f);
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

        /// <summary>The unit direction from <paramref name="from"/> to the nearest street within
        /// <paramref name="within"/> metres, or zero.</summary>
        private static Vector2 NearestStreetDirection(Site site, Vector2 from, float within)
        {
            float best = within;
            Vector2 dir = Vector2.zero;
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel) continue;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], ab = s.Pts[i + 1] - a;
                    float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(from - a, ab) / ab.sqrMagnitude) : 0f;
                    Vector2 to = a + ab * t - from;
                    float dist = to.magnitude;
                    if (dist >= best || dist < 1e-3f) continue;
                    best = dist;
                    dir = to / dist;
                }
            }
            return dir;
        }

        // A few containers to suit the building, standing clear of the walls, of each other and of the door.
        private static void Loot(Site site, Footprint f, System.Random rng, Vector2 doorPoint)
        {
            ContainerType[] kinds = ContainersFor(f.Data);
            int count = Mathf.Clamp(Mathf.RoundToInt(f.Size / 45f), 1, 5);
            var placed = new List<Vector2>(count);
            for (int i = 0; i < count && site.Containers < MaxContainers; i++)
            {
                for (int attempt = 0; attempt < 14; attempt++)
                {
                    var at = new Vector2(Mathf.Lerp(f.Bounds.xMin, f.Bounds.xMax, (float)rng.NextDouble()),
                        Mathf.Lerp(f.Bounds.yMin, f.Bounds.yMax, (float)rng.NextDouble()));
                    if (!MapGeometry.Contains(f.Pts, at) || MapGeometry.DistanceToEdge(f.Pts, at) < 0.9f
                        || Vector2.Distance(at, doorPoint) < 2f || placed.Exists(q => Vector2.Distance(q, at) < 1.3f)) continue;
                    LootContainer.Spawn(site.Loot, kinds[rng.Next(kinds.Length)], To3(at, 0f), rng.Next());
                    placed.Add(at);
                    site.Containers++;
                    break;
                }
            }
        }

        private static ContainerType[] ContainersFor(MapBuilding b)
        {
            string use = b.use ?? "";
            if (use == "police") return new[] { ContainerType.GunLocker, ContainerType.Locker, ContainerType.Desk };
            if (b.kind == "hospital" || use == "hospital" || use == "clinic" || use == "doctors" || use == "pharmacy" || use == "dentist")
                return new[] { ContainerType.MedCabinet, ContainerType.Shelf, ContainerType.Desk };
            if (use == "fire_station") return new[] { ContainerType.Locker, ContainerType.ToolBench };
            if (use.StartsWith("shop:")) return new[] { ContainerType.Shelf, ContainerType.Shelf, ContainerType.Crate, ContainerType.Pantry };
            switch (b.kind)
            {
                case "house": case "trailer": case "apartments": case "hotel":
                    return new[] { ContainerType.Wardrobe, ContainerType.Pantry, ContainerType.Fridge, ContainerType.Desk };
                case "retail": case "commercial":
                    return new[] { ContainerType.Shelf, ContainerType.Crate, ContainerType.Pantry };
                case "office": case "civic": case "school": case "church":
                    return new[] { ContainerType.Desk, ContainerType.Locker, ContainerType.Shelf };
                default:
                    return new[] { ContainerType.Crate, ContainerType.ToolBench, ContainerType.Locker };
            }
        }

        /// <summary>The building's height in metres: as mapped, or from its storeys and kind.</summary>
        private static float HeightOf(MapBuilding b)
        {
            // Use the deterministic effective height defined on MapBuilding.
            return b.EffectiveHeight();
        }

        private static Surf RoofSurf(MapBuilding b, System.Random rng)
        {
            // Deterministic roof colour variation based on building id.
            // Use the same hashing logic as ExteriorOf's deterministicIndex.
            int deterministicIndex(long id, int count)
            {
                unchecked
                {
                    uint hash = (uint)id ^ 0x9e3779b9u;
                    hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                    return (int)(hash % (uint)count);
                }
            }
            int idx = deterministicIndex(b.id, 5);
            float shade = 0.95f + 0.01f * idx; // 0.95–0.99 brightness
            return DecorPalette.Matte(new Color(shade, shade, shade));
        }

        private static Surf ExteriorOf(MapBuilding b, System.Random rng)
        {
            // Deterministic colour selection based on building id to ensure repeatable façade tints.
            // The colour choice is independent of other random draws for this building.
            int deterministicIndex(long id, int count)
            {
                // Simple hash mixing using xor and multiplication, then modulo.
                unchecked
                {
                    uint hash = (uint)id ^ 0x9e3779b9u;
                    hash = (hash * 0x85ebca6bu) ^ (hash >> 13);
                    return (int)(hash % (uint)count);
                }
            }

            switch (b.kind)
            {
                case "house": case "trailer":
                {
                    int idx = deterministicIndex(b.id, SidingColors.Length);
                    return DecorMaterials.WallExterior(ExteriorStyle.Siding, SidingColors[idx]);
                }
                case "barn":
                    return DecorMaterials.Planks(new Color(0.46f, 0.18f, 0.14f));
                case "industrial": case "warehouse": case "silo": case "greenhouse":
                    return DecorMaterials.Corrugated(new Color(0.56f, 0.58f, 0.60f));
                case "garage": case "shed": case "ruin": case "canopy":
                    return DecorMaterials.WallExterior(ExteriorStyle.CinderBlock, new Color(0.66f, 0.66f, 0.64f));
                default:
                {
                    // For default styles we still want deterministic colour selection.
                    bool useStucco = (deterministicIndex(b.id, 4) < 1); // 1 in 4 chance
                    if (useStucco)
                        return DecorMaterials.WallExterior(ExteriorStyle.Stucco, new Color(0.80f, 0.78f, 0.72f));
                    int idx = deterministicIndex(b.id, BrickColors.Length);
                    return DecorMaterials.WallExterior(ExteriorStyle.Brick, BrickColors[idx]);
                }
            }
        }

        private static Surf FloorOf(MapBuilding b, System.Random rng)
        {
            switch (b.kind)
            {
                case "house": case "trailer": case "apartments": case "hotel":
                    return DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f));
                case "industrial": case "warehouse": case "garage": case "shed": case "barn": case "silo": case "ruin":
                case "greenhouse":
                    return DecorMaterials.Concrete;
                default:
                    return DecorMaterials.Linoleum(new Color(0.78f, 0.76f, 0.70f), new Color(0.60f, 0.58f, 0.54f), rng.NextDouble() < 0.4);
            }
        }
    }
}
