using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The ground of the real-world map: snow over everything, frozen water, plowed parking lots, the streets (asphalt
    // for roads, gravel for tracks, trodden snow for paths), the rivers and streams as ice, and the railways.
    public static partial class KentuckyMap
    {
        private const float StreetY = 0.012f;
        private const float LotY = 0.008f;
        private const float IceY = 0.006f;

        private static Surf Ice => DecorPalette.Gloss(new Color(0.60f, 0.68f, 0.76f));
        private static Surf TroddenSnow => DecorPalette.Matte(new Color(0.74f, 0.77f, 0.82f));
        private static Surf CentreLine => DecorPalette.Matte(new Color(0.78f, 0.66f, 0.22f));
        private static Surf RailSteel => DecorPalette.Matte(new Color(0.30f, 0.27f, 0.25f));
        private static Surf Sleeper => DecorPalette.Matte(new Color(0.26f, 0.20f, 0.15f));

        // Walkable snow with one collider over the playable square (like the procedural town's), and a snowfield
        // without a collider under the scenery beyond, so the horizon isn't empty.
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
                Fill(site.Town.Out, inside, water ? IceY : LotY, water ? Ice : DecorMaterials.Asphalt);
                if (water) site.Take(Rect.MinMaxRect(Min(inside, true), Min(inside, false), Max(inside, true), Max(inside, false)));
            }
        }

        private static void BuildStreets(Site site)
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
                    Ribbon(site.Town.Out, piece, width, StreetY, surf);
                    site.TakeLine(piece, width * 0.5f + 0.3f);
                    if (road && width >= 7f && Rank(s.Data.kind) >= 3) DashedLine(site.Town.Out, piece, StreetY + 0.003f);
                }
                // The road out of town carries on past the edge, as scenery.
                if (road)
                    foreach (List<Vector2> piece in OutsidePieces(s.Pts, half))
                        Ribbon(site.Town.Far, piece, width, StreetY, surf);
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
                float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                for (float s = carried; s + 3f <= len; s += 9f)
                {
                    Vector3 at = To3(a + d * (s + 1.5f), y);
                    into.Flat(at).Flat(at, 0.12f, 3f, yaw, CentreLine);
                }
                carried = Mathf.Repeat(carried - len, 9f);
            }
        }

        /// <summary>A flat polygon at height <paramref name="y"/>, face up, into the chunk of its middle.</summary>
        private static void Fill(OutdoorBatches into, List<Vector2> poly, float y, in Surf surf)
        {
            List<int> tris = MapGeometry.Triangulate(poly);
            if (tris.Count == 0) return;
            RaidMesh m = into.Flat(To3(MapGeometry.Middle(poly), 0f));
            for (int i = 0; i + 2 < tris.Count; i += 3)
                m.Tri(To3(poly[tris[i]], y), To3(poly[tris[i + 1]], y), To3(poly[tris[i + 2]], y), surf);
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
    }
}
