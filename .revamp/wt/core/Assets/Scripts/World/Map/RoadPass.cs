using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's roads: the carriageways (asphalt for roads, gravel for tracks, trodden snow for paths) with
    /// raised kerbs, the snowy concrete sidewalks along them, the faded centre lines and the railways. Each road piece
    /// inside the square is kept clear of props (<see cref="Site.TakeRoad"/>); on a fixed map the roads out of town carry
    /// on past its edge as scenery (a streamed map's next piece draws them whole).
    /// </summary>
    internal static class RoadPass
    {
        private const float KerbHeight = 0.04f;   // the raised kerb strip, above the road
        private const float KerbWidth = 0.22f;
        private const float SidewalkWidth = 1.5f;
        private const float SnowDepth = 0.05f;   // the thin snow on the sidewalks

        private static Surf KerbSurf => DecorPalette.Matte(new Color(0.62f, 0.62f, 0.60f));
        private static Surf TroddenSnow => DecorPalette.Matte(new Color(0.74f, 0.77f, 0.82f));
        private static Surf CentreLineSurf => DecorPalette.Matte(new Color(0.78f, 0.66f, 0.22f));
        private static Surf RailSteel => DecorPalette.Matte(new Color(0.30f, 0.27f, 0.25f));
        private static Surf Sleeper => DecorPalette.Matte(new Color(0.26f, 0.20f, 0.15f));

        internal static IEnumerable<string> Build(Site site)
        {
            Streets(site);
            yield return "streets";
            Sidewalks(site);
            yield return "sidewalks";
            CentreLines(site);
            yield return "road markings";
            Railways(site);
            yield return "railways";
        }

        // The carriageways, each with a skin a shade lighter (asphalt) or of ice (trodden paths); along both edges of
        // every road a raised kerb with an icy top and a worn strip just inside it.
        private static void Streets(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel) continue;
                float width = s.Width;
                Surf surf = StreetSurface(s.Data.kind);
                bool road = s.Rank >= 2;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square))
                {
                    Ribbon(site.Town.Out, piece, width, MapStyle.StreetY, surf);
                    if (surf.Mat == TroddenSnow.Mat) Ribbon(site.Town.Out, piece, width * 0.96f, MapStyle.StreetY + 0.001f, MapStyle.Ice);
                    if (surf.Mat == DecorMaterials.Asphalt.Mat)
                        Ribbon(site.Town.Out, piece, width * 0.96f, MapStyle.StreetY + 0.001f, AsphaltSkin(piece[0]));
                    if (road)
                        for (int side = -1; side <= 1; side += 2)
                        {
                            List<Vector2> kerb = MapGeometry.OffsetLine(piece, side * (width - KerbWidth) * 0.5f);
                            Ribbon(site.Town.Out, kerb, KerbWidth, MapStyle.StreetY + KerbHeight, KerbSurf);
                            Ribbon(site.Town.Out, MapGeometry.OffsetLine(piece, side * (width * 0.5f - KerbWidth - 0.02f)), 0.04f,
                                MapStyle.StreetY + 0.001f, WearStrip(piece[0]));
                            Ribbon(site.Town.Out, kerb, KerbWidth, MapStyle.StreetY + KerbHeight + 0.001f, MapStyle.Ice);
                        }
                    site.TakeRoad(piece, width * 0.5f, 0.3f);
                }
                if (!road || site.Chunked) continue;
                foreach (List<Vector2> piece in OutsidePieces(s.Pts, half))
                {
                    Ribbon(site.Town.Far, piece, width, MapStyle.StreetY, surf);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        List<Vector2> kerb = MapGeometry.OffsetLine(piece, side * (width - KerbWidth) * 0.5f);
                        Ribbon(site.Town.Far, kerb, KerbWidth, MapStyle.StreetY + KerbHeight, KerbSurf);
                        Ribbon(site.Town.Far, kerb, KerbWidth, MapStyle.StreetY + KerbHeight + 0.001f, MapStyle.Ice);
                    }
                }
            }
        }

        // Concrete sidewalks under a thin layer of snow along both sides of every street, drawn like the roads: inside
        // the playable square with the outdoor ground, past the edge as scenery where the road carries on.
        private static void Sidewalks(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square)) Sidewalks(site.Town.Out, s, piece);
                if (!site.Chunked && s.Rank >= 2)
                    foreach (List<Vector2> piece in OutsidePieces(s.Pts, half)) Sidewalks(site.Town.Far, s, piece);
            }
        }

        private static void Sidewalks(OutdoorBatches into, Street s, List<Vector2> pts)
        {
            float halfRoad = s.Width * 0.5f;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                Vector2 dir = (b - a).normalized;
                Vector2 normal = new Vector2(-dir.y, dir.x);
                float length = Vector2.Distance(a, b);
                Vector2 mid = (a + b) * 0.5f;
                float yaw = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;   // the boxes' length runs along the street
                var size = new Vector3(length, 0.04f, SidewalkWidth);
                var snowSize = new Vector3(length, SnowDepth, SidewalkWidth);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 at = mid + normal * ((halfRoad + SidewalkWidth * 0.5f) * side);
                    RaidMesh m = into.Flat(To3(at, 0f));
                    m.Box(To3(at, MapStyle.StreetY + 0.01f), size, yaw, DecorMaterials.Concrete);
                    m.Box(To3(at, MapStyle.StreetY + 0.01f + 0.02f + SnowDepth * 0.5f), snowSize, yaw, DecorMaterials.Snow);
                }
            }
        }

        // A faded yellow centre line down the asphalt, inside the playable square: 3 m dashes every 9 m on the wide
        // roads, 2 m dashes every 3 m on the narrower ones mapped with two lanes or more.
        private static void CentreLines(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel || StreetSurface(s.Data.kind).Mat != DecorMaterials.Asphalt.Mat) continue;
                bool wide = s.Rank >= 2 && s.Width >= 7f;
                if (!wide && s.Data.lanes < 2) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square))
                    Dashes(site.Town.Out, piece, wide ? 3f : 2f, wide ? 9f : 3f);
            }
        }

        // A dash <paramref name="dash"/> long every <paramref name="period"/> metres from the line's start, measured along
        // it, a dash that runs over a bend split there.
        private static void Dashes(OutdoorBatches into, List<Vector2> pts, float dash, float period)
        {
            float travelled = 0f;   // along the line, to the current segment's start
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.05f)
                {
                    travelled += len;
                    continue;
                }
                Vector2 d = (b - a) / len;
                float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;   // the dash's length (local +Z) along the road
                for (float start = Mathf.Floor(travelled / period) * period; start < travelled + len; start += period)
                {
                    float from = Mathf.Max(start, travelled), to = Mathf.Min(start + dash, travelled + len);
                    if (to - from < 0.05f) continue;
                    Vector3 at = To3(a + d * ((from + to) * 0.5f - travelled), MapStyle.StreetY + 0.003f);
                    into.Flat(at).Flat(at, 0.12f, to - from, yaw, CentreLineSurf);
                }
                travelled += len;
            }
        }

        // The railways: a gravel bed with sleepers and two rails, kept clear of props.
        private static void Railways(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Line l in site.Area.Lines)
            {
                if (!l.IsRailway) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(l.Pts, square))
                {
                    Ribbon(site.Town.Out, piece, 3f, MapStyle.LotY, DecorMaterials.Gravel);
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
                RaidMesh m = into.Solid(To3((a + b) * 0.5f, 0f));
                for (int side = -1; side <= 1; side += 2)
                    m.Box(To3((a + b) * 0.5f + n * (0.72f * side), 0.09f), new Vector3(len, 0.12f, 0.07f), yaw, RailSteel);
                for (float s = 0.3f; s < len; s += 0.65f)
                    m.Box(To3(a + d * s, 0.03f), new Vector3(0.22f, 0.06f, 2.4f), yaw, Sleeper);
            }
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

        // The lighter skin over the asphalt, a shade different from piece to piece.
        private static Surf AsphaltSkin(Vector2 at)
        {
            float r = Mathf.PerlinNoise(at.x * 0.13f, at.y * 0.13f) * 0.04f;
            return DecorPalette.Matte(new Color(0.68f + r, 0.68f + r, 0.66f + r));
        }

        // The worn strip just inside the kerb, a shade different from piece to piece.
        private static Surf WearStrip(Vector2 at)
        {
            float r = Mathf.PerlinNoise(at.x * 0.13f, at.y * 0.13f) * 0.03f;
            return DecorPalette.Matte(new Color(0.55f + r, 0.55f + r, 0.55f + r));
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
