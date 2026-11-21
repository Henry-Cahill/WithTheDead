using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's ground: walkable snow over the playable square (and, on a fixed map, a snowfield out to the
    /// horizon), the land-use fills (ponds and lakes frozen over, parking lots plowed down to the asphalt) and the
    /// waterways (rivers, streams, canals, ditches) as ice. Water and lots are kept clear of props.
    /// </summary>
    internal static class GroundPass
    {
        private const float SnowBankWidth = 0.2f;   // the plowed snow along a parking lot's edge

        private static Surf LotLine => DecorPalette.Matte(new Color(0.9f, 0.9f, 0.9f));

        internal static IEnumerable<string> Build(Site site)
        {
            Snow(site);
            yield return "ground";
            WaterAndLots(site);
            yield return "water and lots";
            Waterways(site);
            yield return "waterways";
        }

        // Walkable snow with one collider over the playable square (like the procedural town's), and a snowfield
        // without a collider under the scenery beyond, so the horizon isn't empty.
        private static void Snow(Site site)
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
            if (site.Chunked) return;   // a streamed map's neighbouring pieces are the horizon

            float far = site.Half + SceneryReach + 40f;
            var field = new RaidMesh();
            field.Flat(new Vector3(0f, -0.02f, 0f), far * 2f, far * 2f, 0f, DecorMaterials.Snow);
            field.Emit("Snowfield", site.Town.Scenery, Vector3.zero, ShadowCastingMode.Off);
        }

        // Ponds and lakes freeze over; parking lots are plowed down to the asphalt (under a skin of gravel dust), a white
        // line round their edge and the plowed snow banked along it. Only the parts inside the playable square are
        // drawn, and each one's bounding box is kept clear of props.
        private static void WaterAndLots(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Patch p in site.Area.Areas)
            {
                bool water = p.Data.kind == "water", lot = p.Data.kind == "parking";
                if (!water && !lot) continue;
                List<Vector2> inside = MapGeometry.ClipPolygon(p.Pts, square);
                if (inside.Count < 3) continue;
                if (water) FillIce(site.Town.Out, inside);
                else
                {
                    Fill(site.Town.Out, inside, MapStyle.LotY, DecorMaterials.Asphalt);
                    Fill(site.Town.Out, inside, MapStyle.LotY + 0.001f, DecorMaterials.Gravel);
                    var edge = new List<Vector2>(inside) { inside[0] };
                    Ribbon(site.Town.Out, edge, 0.07f, MapStyle.LotY + 0.002f, LotLine);
                    Ribbon(site.Town.Out, edge, SnowBankWidth, MapStyle.LotY + 0.001f, MapStyle.Ice);
                }
                site.Take(MapGeometry.Bounds(inside));
            }
        }

        // Rivers, streams, canals and ditches as ice, kept clear of props half a metre past their banks.
        private static void Waterways(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Line l in site.Area.Lines)
            {
                if (l.IsRailway) continue;
                float width = Mathf.Clamp(l.Data.width, 1f, 60f);
                foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, square))
                {
                    Ribbon(site.Town.Out, piece, width, MapStyle.IceY, MapStyle.Ice);
                    site.TakeLine(piece, width * 0.5f + 0.5f);
                }
            }
        }

        // Ice over a polygon, its height varied a millimetre or so from corner to corner for a less glassy look.
        private static void FillIce(OutdoorBatches into, List<Vector2> poly)
        {
            List<int> tris = MapGeometry.Triangulate(poly);
            if (tris.Count == 0) return;
            var heights = new float[poly.Count];
            for (int i = 0; i < poly.Count; i++)
                heights[i] = MapStyle.IceY + (Mathf.PerlinNoise(poly[i].x * 0.13f, poly[i].y * 0.13f) - 0.5f) * 0.002f;
            RaidMesh m = into.Flat(To3(MapGeometry.Middle(poly), 0f));
            for (int i = 0; i + 2 < tris.Count; i += 3)
                m.Tri(To3(poly[tris[i]], heights[tris[i]]), To3(poly[tris[i + 1]], heights[tris[i + 1]]),
                    To3(poly[tris[i + 2]], heights[tris[i + 2]]), MapStyle.Ice);
        }

        /// <summary>A flat polygon at height <paramref name="y"/>, face up, into the chunk of its middle (asphalt varied a
        /// millimetre or so from corner to corner, like the ice).</summary>
        private static void Fill(OutdoorBatches into, List<Vector2> poly, float y, in Surf surf)
        {
            List<int> tris = MapGeometry.Triangulate(poly);
            if (tris.Count == 0) return;
            RaidMesh m = into.Flat(To3(MapGeometry.Middle(poly), 0f));
            var heights = new float[poly.Count];
            bool asphalt = surf.Mat == DecorMaterials.Asphalt.Mat;
            for (int i = 0; i < poly.Count; i++)
                heights[i] = asphalt ? y + (Mathf.PerlinNoise(poly[i].x * 0.13f, poly[i].y * 0.13f) - 0.5f) * 0.002f : y;
            for (int i = 0; i + 2 < tris.Count; i += 3)
                m.Tri(To3(poly[tris[i]], heights[tris[i]]), To3(poly[tris[i + 1]], heights[tris[i + 1]]),
                    To3(poly[tris[i + 2]], heights[tris[i + 2]]), surf);
        }

        /// <summary>A flat band <paramref name="width"/> wide along a line at height <paramref name="y"/>, mitred at the
        /// bends, drawn face-up into the chunk of each segment's middle.</summary>
        private static void Ribbon(OutdoorBatches into, List<Vector2> pts, float width, float y, in Surf surf)
        {
            if (pts.Count < 2) return;
            List<Vector2> left = MapGeometry.OffsetLine(pts, width * 0.5f), right = MapGeometry.OffsetLine(pts, -width * 0.5f);
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                RaidMesh m = into.Flat(To3((pts[i] + pts[i + 1]) * 0.5f, 0f));
                Vector3 aL = To3(left[i], y), bL = To3(left[i + 1], y), bR = To3(right[i + 1], y), aR = To3(right[i], y);
                m.Tri(aL, bL, bR, surf);   // clockwise seen from above: the face looks up
                m.Tri(aL, bR, aR, surf);
            }
        }
    }
}
