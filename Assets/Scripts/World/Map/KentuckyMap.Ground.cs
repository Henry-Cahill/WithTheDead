using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The ground of the real-world map: snow over everything, frozen water, plowed parking lots, the streets (asphalt
    // for roads, gravel for tracks, trodden snow for paths), the rivers and streams as ice, and the railways.
    public static partial class KentuckyMap
    {
        internal const float StreetY = 0.012f;
        // Height of the raised kerb strip (above the road surface).
        private const float KerbHeight = 0.04f;
        // Width of the kerb strip.
        private const float KerbWidth = 0.22f;
        // Kerb colour.
        private static readonly Color KerbColor = new Color(0.62f, 0.62f, 0.60f);
        // Height of the raised kerb strip (above the road surface).
        private const float LotY = 0.008f;
        private const float SnowBankWidth = 0.2f; // thin snow‑bank edge width for parking lots
        private const float IceY = 0.006f;
        // Snow‑drift parameters – small ellipsoids that sit just above the road surface to suggest plowed snow.
        private const float SnowDriftMinSize = 0.35f; // metres (half‑width of the smallest drift)
        private const float SnowDriftMaxSize = 0.65f; // metres (half‑width of the largest drift)
        private const float SnowDriftHeight = 0.25f; // metres (vertical radius of drifts)
        private const float SnowDriftInterval = 8f; // metres between possible drift centres

                private static Surf Ice => DecorPalette.Gloss(new Color(0.60f, 0.68f, 0.76f));
        private static Surf KerbSurf => DecorPalette.Matte(KerbColor);
        private static readonly Color StreetLampMetalColor = new Color(0.55f, 0.55f, 0.55f);
        internal static Surf StreetLampMetal => DecorPalette.Matte(StreetLampMetalColor);
        internal static readonly Color StreetLampLightColor = new Color(1f, 0.93f, 0.70f); // warm yellow
        private static readonly Surf TroddenSnow = DecorPalette.Matte(new Color(0.74f, 0.77f, 0.82f));
        private static Surf CentreLine => DecorPalette.Matte(new Color(0.78f, 0.66f, 0.22f));
        private static Surf RailSteel => DecorPalette.Matte(new Color(0.30f, 0.27f, 0.25f));
        private static Surf Sleeper => DecorPalette.Matte(new Color(0.26f, 0.20f, 0.15f));

        // Walkable snow with one collider over the playable square (like the procedural town's), and a snowfield
        // without a collider under the scenery beyond, so the horizon isn't empty.
        internal static IEnumerable<Vector2> SampleAlong(List<Vector2> pts, float interval)
        {
            if (pts.Count < 2) yield break;
            float accumulated = 0f;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float segLen = Vector2.Distance(a, b);
                Vector2 dir = (b - a) / segLen;
                while (accumulated + interval <= segLen)
                {
                    accumulated += interval;
                    yield return a + dir * accumulated;
                }
                accumulated = (accumulated + segLen) - Mathf.Floor((accumulated + segLen) / interval) * interval;
            }
        }
        private static void BuildGround(Site site)
        {
            float half = site.Half + 0.8f;
            var ground = new GameObject("Ground");
            ground.transform.SetParent(site.Town.World, false);
            var m = new RaidMesh();
            m.Box(new Vector3(0f, -0.5f, 0f), new Vector3(half * 2f, 1f, half * 2f), DecorMaterials.Snow);
            m.ApplyTo(ground, Vector3.zero, ShadowCastingMode.Off);
            var col = ground.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.5f, 0f);
            col.size = new Vector3(half * 2f, 1f, half * 2f);

            float far = site.Half + SceneryReach + 40f;
            var field = new RaidMesh();
            field.Flat(new Vector3(0f, -0.02f, 0f), far * 2f, far * 2f, 0f, DecorMaterials.Snow);
            field.Emit("Snowfield", site.Town.Scenery, Vector3.zero, ShadowCastingMode.Off);
        }

        // Ponds and lakes freeze over; parking lots are plowed down to the asphalt. Only the parts inside the
        // playable square are drawn.
        private static void BuildWaterAndLots(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Patch p in site.Area.Areas)
            {
                bool water = p.Data.kind == "water", lot = p.Data.kind == "parking";
                if (!water && !lot) continue;
                List<Vector2> inside = MapGeometry.ClipPolygon(p.Pts, square);
                if (inside.Count < 3) continue;
if (water) {
                     FillIce(site.Town.Out, inside);
                 } else {
                     // Base asphalt surface for parking lots
                      Fill(site.Town.Out, inside, LotY, DecorMaterials.Asphalt);
                      // Thin gravel overlay to give a dusty, period‑correct look
                      Fill(site.Town.Out, inside, LotY + 0.001f, DecorMaterials.Gravel);
                      // Parking lot white line markings
                      var lineSurf = DecorPalette.Matte(new Color(0.9f, 0.9f, 0.9f));
                      var border = new List<Vector2>(inside);
                      border.Add(inside[0]);
                      Ribbon(site.Town.Out, border, 0.07f, LotY + 0.002f, lineSurf);
                 }
                 // Reserve the parking‑lot footprint so later building placement can’t overlap it
                    site.Take(Rect.MinMaxRect(Min(inside, true), Min(inside, false), Max(inside, true), Max(inside, false)));
                 // Add thin snow‑bank edge around parking lots for a plowed look
                 if (lot)
                 {
                     var edge = new List<Vector2>(inside);
                     edge.Add(inside[0]); // close loop
                     Ribbon(site.Town.Out, edge, SnowBankWidth, LotY + 0.001f, Ice);
                    ParkingLotFenceHelper.AddFence(site.Town.Out, edge, LotY + 0.002f);
                 }
        }
        }

        private static void BuildStreets(Site site, MapBuild build)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel) continue;
                float width = Mathf.Clamp(s.Data.width, 1.2f, 30f);
                Surf surf = StreetSurface(s.Data.kind);
                bool road = Rank(s.Data.kind) >= 2;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square))
                {
// Main road surface
                     Ribbon(site.Town.Out, piece, width, StreetY, surf);
                     // Add thin icy crust overlay for footways/paths (trodden snow surfaces)
                     if (surf.Mat == TroddenSnow.Mat)
                     {
                         Ribbon(site.Town.Out, piece, width * 0.96f, StreetY + 0.001f, Ice);
                     }
                     // Add subtle brighter overlay for asphalt roads
                     if (surf.Mat == DecorMaterials.Asphalt.Mat)
                     {
                         var varSurf = RandomAsphaltOverlay(piece[0]);
                         Ribbon(site.Town.Out, piece, width * 0.96f, StreetY + 0.001f, varSurf);
                     }
                    // Raised kerb strip along the road edge
                    if (road)
                    {
                        for (int side = -1; side <= 1; side += 2)
                        {
                            List<Vector2> kerb = OffsetLine(piece, side * (width - KerbWidth) * 0.5f);
                            Ribbon(site.Town.Out, kerb, KerbWidth, StreetY + KerbHeight, KerbSurf);
                            // thin wear strip just inside the kerb – gives the road a lived‑in look
                            Ribbon(site.Town.Out, OffsetLine(piece, side * (width * 0.5f - KerbWidth - 0.02f)), 0.04f, StreetY + 0.001f, RandomGravelOverlay(piece[0]));
                            Ribbon(site.Town.Out, kerb, KerbWidth, StreetY + KerbHeight + 0.001f, Ice);
                        }
                    }
                    // Add subtle snow‑drift piles beside each side of the road.
                    for (int side = -1; side <= 1; side += 2)
                    {
                        // Derive a deterministic RNG from the first point of the piece and the side.
                        int seed = ((int)(piece[0].x * 73856093)) ^ ((int)(piece[0].y * 19349663)) ^ (side * 83492791);
                        var rng = new System.Random(seed);
                        AddSnowDrifts(site.Town.Out, piece, width, side, rng);
                    }
                    // Place street‑lamp poles along main roads (road rank >= 2) every 30 m.
                    if (road && surf.Mat == DecorMaterials.Asphalt.Mat)
                    {
                        const float lampInterval = 30f;
                        foreach (Vector2 lampPos in SampleAlong(piece, lampInterval))
                        {
                            // Determine lamp yaw: align with road direction at the sampled point.
                            // Find the segment that contains the point.
                            float yaw = 0f;
                            for (int i = 0; i + 1 < piece.Count; i++)
                            {
                                Vector2 a = piece[i], b = piece[i + 1];
                                if (Vector2.Distance(a, lampPos) + Vector2.Distance(lampPos, b) - Vector2.Distance(a, b) < 0.01f)
                                {
                                    Vector2 d = (b - a).normalized;
                                    yaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                                    break;
                                }
                            }
                            StreetLampHelper.PlaceLamp(site.Town.Out, site, lampPos, yaw);
                        }
                    }
                    RoadMarkingsBuilder.BuildForStreet(site, s, build);
                    // Place period‑correct road signposts every 60 m on main roads.
                    if (road && surf.Mat == DecorMaterials.Asphalt.Mat)
                    {
                        const float signInterval = 60f;
                        foreach (Vector2 signPos in SampleAlong(piece, signInterval))
                        {
                            // Determine sign yaw: align with road direction at the sampled point.
                            float signYaw = 0f;
                            for (int i = 0; i + 1 < piece.Count; i++)
                            {
                                Vector2 a = piece[i], b = piece[i + 1];
                                if (Vector2.Distance(a, signPos) + Vector2.Distance(signPos, b) - Vector2.Distance(a, b) < 0.01f)
                                {
                                    Vector2 d = (b - a).normalized;
                                    signYaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                                    break;
                                }
                            }
                            SignpostHelper.PlaceSignpost(site.Town.Out, site, signPos, signYaw);
                        }
                    }
                site.TakeLine(piece, width * 0.5f + 0.3f);
                    if (road && width >= 7f) DashedLine(site.Town.Out, piece, StreetY + 0.003f);
                }
                // The road out of town carries on past the edge, as scenery.
                if (road)
                {
                    foreach (List<Vector2> piece in OutsidePieces(s.Pts, half))
                    {
                        Ribbon(site.Town.Far, piece, width, StreetY, surf);
                        // Kerb on the outer road extension
                        for (int side = -1; side <= 1; side += 2)
                        {
                            List<Vector2> kerb = OffsetLine(piece, side * (width - KerbWidth) * 0.5f);
                            Ribbon(site.Town.Far, kerb, KerbWidth, StreetY + KerbHeight, KerbSurf);
                            Ribbon(site.Town.Far, kerb, KerbWidth, StreetY + KerbHeight + 0.001f, Ice);
                        }
                    }
                }
            }
        }

        /// <summary>The pieces of a line outside the playable square: cut to the four bands around it, which together
        /// cover everything out to the scenery's reach without overlapping.</summary>
        private static IEnumerable<List<Vector2>> OutsidePieces(List<Vector2> pts, float half)
        {
            float reach = half + SceneryReach;
            var bands = new[]
            {
                Rect.MinMaxRect(-reach, -reach, -half, reach), Rect.MinMaxRect(half, -reach, reach, reach),
                Rect.MinMaxRect(-half, half, half, reach), Rect.MinMaxRect(-half, -reach, half, -half),
            };
            foreach (Rect band in bands)
                foreach (List<Vector2> piece in MapGeometry.ClipLine(pts, band))
                    yield return piece;
        }

        private static Surf StreetSurface(string kind)
        {
            switch (kind)
            {
                case "track": return DecorMaterials.Gravel;
                case "footway": case "path": case "cycleway": case "bridleway": case "steps": case "pedestrian":
                    return TroddenSnow;
                default: return DecorMaterials.Asphalt;
            }
        }

        // Rivers, streams and ditches as ice; railways as a gravel bed with sleepers and two rails.
        private static void BuildLines(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Line l in site.Area.Lines)
            {
                bool rail = l.Data.kind == "rail" || l.Data.kind == "abandoned" || l.Data.kind == "disused";
                float width = Mathf.Clamp(l.Data.width, 1f, 60f);
                foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, square))
                {
                    if (!rail)
                    {
                        Ribbon(site.Town.Out, piece, width, IceY, Ice);
                        site.TakeLine(piece, width * 0.5f + 0.5f);
                        continue;
                    }
                    Ribbon(site.Town.Out, piece, 3f, LotY, DecorMaterials.Gravel);
                    Rails(site.Town.Out, piece);
                    site.TakeLine(piece, 2f);
                }
            }
        }

        private static void Rails(OutdoorBatches into, List<Vector2> pts)
        {
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.05f) continue;
                Vector2 d = (b - a) / len, n = new Vector2(-d.y, d.x);
                float yaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                Vector3 mid = To3((a + b) * 0.5f, 0f);
                RaidMesh m = into.Solid(mid);
                for (int side = -1; side <= 1; side += 2)
                    m.Box(To3((a + b) * 0.5f + n * (0.72f * side), 0.09f), new Vector3(len, 0.12f, 0.07f), yaw, RailSteel);
                for (float s = 0.3f; s < len; s += 0.65f)
                    m.Box(To3(a + d * s, 0.03f), new Vector3(0.22f, 0.06f, 2.4f), yaw, Sleeper);
            }
        }

        /// <summary>A flat band <paramref name="width"/> wide along a line at height <paramref name="y"/>, mitred at the
        /// bends, drawn face-up into the chunk of each segment's middle.</summary>
        private static void Ribbon(OutdoorBatches into, List<Vector2> pts, float width, float y, in Surf surf)
        {
            int n = pts.Count;
            if (n < 2) return;
            float hw = width * 0.5f;
            var left = new Vector2[n];
            var right = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 dPrev = (i > 0 ? pts[i] - pts[i - 1] : pts[1] - pts[0]).normalized;
                Vector2 dNext = (i < n - 1 ? pts[i + 1] - pts[i] : dPrev).normalized;
                Vector2 nPrev = new Vector2(-dPrev.y, dPrev.x), nNext = new Vector2(-dNext.y, dNext.x);
                Vector2 miter = nPrev + nNext;
                miter = miter.sqrMagnitude > 1e-6f ? miter.normalized : nNext;
                float stretch = 1f / Mathf.Max(0.4f, Vector2.Dot(miter, nNext));   // sharp bends are cut short
                left[i] = pts[i] + miter * (hw * stretch);
                right[i] = pts[i] - miter * (hw * stretch);
            }
            for (int i = 0; i + 1 < n; i++)
            {
                RaidMesh m = into.Flat(To3((pts[i] + pts[i + 1]) * 0.5f, 0f));
                Vector3 aL = To3(left[i], y), bL = To3(left[i + 1], y), bR = To3(right[i + 1], y), aR = To3(right[i], y);
                m.Tri(aL, bL, bR, surf);   // clockwise seen from above: the face looks up
                m.Tri(aL, bR, aR, surf);
            }
        }

        /// <summary>The line moved sideways by <paramref name="offset"/> (to the left of its direction when positive),
        /// mitred at the bends as <see cref="Ribbon"/> does.</summary>
        private static List<Vector2> OffsetLine(List<Vector2> pts, float offset)
        {
            int n = pts.Count;
            var moved = new List<Vector2>(n);
            if (n < 2)
            {
                moved.AddRange(pts);
                return moved;
            }
            for (int i = 0; i < n; i++)
            {
                Vector2 dPrev = (i > 0 ? pts[i] - pts[i - 1] : pts[1] - pts[0]).normalized;
                Vector2 dNext = (i < n - 1 ? pts[i + 1] - pts[i] : dPrev).normalized;
                Vector2 nPrev = new Vector2(-dPrev.y, dPrev.x), nNext = new Vector2(-dNext.y, dNext.x);
                Vector2 miter = nPrev + nNext;
                miter = miter.sqrMagnitude > 1e-6f ? miter.normalized : nNext;
                float stretch = 1f / Mathf.Max(0.4f, Vector2.Dot(miter, nNext));
                moved.Add(pts[i] + miter * (offset * stretch));
            }
            return moved;
        }

        // A faded centre line: 3 m dashes every 9 m.
        private static void DashedLine(OutdoorBatches into, List<Vector2> pts, float y)
        {
            float carried = 0f;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.05f) continue;
                Vector2 d = (b - a) / len;
                float yaw = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                for (float s = carried; s + 3f <= len; s += 9f)
                {
                    Vector3 at = To3(a + d * (s + 1.5f), y);
                    into.Flat(at).Flat(at, 0.12f, 3f, yaw, CentreLine);
                }
                carried = Mathf.Repeat(carried - len, 9f);
            }
        }

        /// <summary>A flat polygon at height <paramref name="y"/>, face up, into the chunk of its middle.</summary>
        // Fill ice polygon with subtle per‑vertex height variation for a more natural look
        private static void FillIce(OutdoorBatches into, List<Vector2> poly)
        {
            List<int> tris = MapGeometry.Triangulate(poly);
            if (tris.Count == 0) return;
            // Compute a random height offset per vertex using deterministic Perlin noise
            int vCount = poly.Count;
            float[] heights = new float[vCount];
            for (int i = 0; i < vCount; i++)
            {
                // Scale coordinates to get gentle variation; clamp to small range
                float noise = Mathf.PerlinNoise(poly[i].x * 0.13f, poly[i].y * 0.13f);
                heights[i] = IceY + (noise - 0.5f) * 0.002f; // +-0.001f variation
            }
            RaidMesh m = into.Flat(To3(MapGeometry.Middle(poly), 0f));
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = To3(poly[tris[i]], heights[tris[i]]);
                Vector3 b = To3(poly[tris[i + 1]], heights[tris[i + 1]]);
                Vector3 c = To3(poly[tris[i + 2]], heights[tris[i + 2]]);
                m.Tri(a, b, c, Ice);
            }
        }

        // Generic polygon fill used for asphalt parking lots and other surfaces
        private static void Fill(OutdoorBatches into, List<Vector2> poly, float y, in Surf surf)
        {
            List<int> tris = MapGeometry.Triangulate(poly);
            if (tris.Count == 0) return;
            RaidMesh m = into.Flat(To3(MapGeometry.Middle(poly), 0f));
            bool asphalt = surf.Mat == DecorMaterials.Asphalt.Mat;
            float[] heights = null;
            if (asphalt)
            {
                int vCount = poly.Count;
                heights = new float[vCount];
                for (int i = 0; i < vCount; i++)
                    heights[i] = y + (Mathf.PerlinNoise(poly[i].x * 0.13f, poly[i].y * 0.13f) - 0.5f) * 0.002f;
            }
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                if (asphalt)
                    m.Tri(To3(poly[tris[i]], heights[tris[i]]), To3(poly[tris[i + 1]], heights[tris[i + 1]]), To3(poly[tris[i + 2]], heights[tris[i + 2]]), surf);
                else
                    m.Tri(To3(poly[tris[i]], y), To3(poly[tris[i + 1]], y), To3(poly[tris[i + 2]], y), surf);
            }
        }

        private static float Min(List<Vector2> pts, bool x)
        {
            float v = float.MaxValue;
            foreach (Vector2 p in pts) v = Mathf.Min(v, x ? p.x : p.y);
            return v;
        }

        private static float Max(List<Vector2> pts, bool x)
        {
            float v = float.MinValue;
            foreach (Vector2 p in pts) v = Mathf.Max(v, x ? p.x : p.y);
            return v;
        }

        private static Surf RandomAsphaltOverlay(Vector2 seed)
        {
            // small deterministic jitter using Perlin noise; keeps values in a plausible range
            float r = Mathf.PerlinNoise(seed.x * 0.13f, seed.y * 0.13f) * 0.04f;
            // base colour is a mild grey, adjust slightly by r on each channel
            Color baseCol = new Color(0.68f, 0.68f, 0.66f);
            Color varied = new Color(baseCol.r + r, baseCol.g + r, baseCol.b + r);
            return DecorPalette.Matte(varied);
        }

        /// <summary>Place a few small snow‑drift ellipsoids alongside a road piece.</summary>
        // Returns the normalized road direction for the segment that contains 'point'.
        // If the point falls exactly on a vertex, the direction of the next segment is used.
        private static Vector2 RoadDirectionAt(List<Vector2> piece, Vector2 point)
        {
            for (int i = 0; i + 1 < piece.Count; i++)
            {
                Vector2 a = piece[i], b = piece[i + 1];
                float segLen = Vector2.Distance(a, b);
                if (segLen < 1e-6f) continue;
                // Check if point lies on this segment (within a tiny tolerance).
                if (Mathf.Abs(Vector2.Distance(a, point) + Vector2.Distance(point, b) - segLen) < 0.01f)
                    return (b - a).normalized;
            }
            // Fallback: use the direction of the first segment.
            return (piece[1] - piece[0]).normalized;
        }

        private static void AddSnowDrifts(OutdoorBatches into, List<Vector2> piece, float roadWidth, int side, System.Random rng)
        {
            // side = -1 for left, +1 for right of the centre line.
            float offset = (roadWidth * 0.5f) + 0.3f; // start a little outside the kerb
            foreach (Vector2 pos in SampleAlong(piece, SnowDriftInterval))
            {
                // Randomly skip some positions for natural spacing.
                if (rng.NextDouble() < 0.4) continue;
                // Use road direction for snow drift placement
                Vector2 dir = RoadDirectionAt(piece, pos);
                Vector2 perp = new Vector2(-dir.y, dir.x); // perpendicular to the road
                Vector2 lateral = perp.normalized * offset * side;
                Vector2 driftPos = pos + lateral;
                // Random size within the defined range.
                float halfWidth = SnowDriftMinSize + (float)rng.NextDouble() * (SnowDriftMaxSize - SnowDriftMinSize);
                Vector3 centre = To3(driftPos, StreetY + 0.001f); // just above the road surface
                RaidMesh m = into.Solid(centre);
                // Ellipsoid (horizontal radius X/Z = halfWidth, vertical radius = SnowDriftHeight)
                m.Ellipsoid(centre, new Vector3(halfWidth, SnowDriftHeight, halfWidth), DecorMaterials.Snow);
            }
        }

        private static Surf RandomGravelOverlay(Vector2 seed)
        {
            // small deterministic colour jitter for gravel using Perlin noise; keeps values in a plausible range
            float r = Mathf.PerlinNoise(seed.x * 0.13f, seed.y * 0.13f) * 0.03f;
            // base colour approximates typical gravel grey; adjust slightly by r on each channel
            Color baseCol = new Color(0.55f, 0.55f, 0.55f);
            Color varied = new Color(baseCol.r + r, baseCol.g + r, baseCol.b + r);
            return DecorPalette.Matte(varied);
        }    }
}
