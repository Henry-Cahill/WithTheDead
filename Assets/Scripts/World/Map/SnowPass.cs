using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's lying snow, past what the ground and the roofs already carry: little drifts of plowed snow
    /// along both sides of every street inside the square, and plowed piles at the roadside of the town's streets
    /// (<see cref="Piles"/>, built in <see cref="StreetscapePass"/>'s turn, after the hydrants and before the cars and
    /// the trees, where they've always claimed their spots).
    /// </summary>
    internal static class SnowPass
    {
        private const float DriftInterval = 8f;   // metres between possible drifts along a street
        private const float DriftMinSize = 0.35f;
        private const float DriftMaxSize = 0.65f;
        private const float DriftHeight = 0.25f;
        private const int MaxPiles = 80;
        private const int SaltDrift = 0xD41F;

        internal static IEnumerable<string> Build(Site site)
        {
            Drifts(site);
            yield return "snow drifts";
        }

        // A row of drifts along each side of every street piece inside the square, just past the kerb.
        private static void Drifts(Site site)
        {
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square))
                    for (int side = -1; side <= 1; side += 2)
                        DriftRow(site.Town.Out, piece, s.Width, side, MapStyle.Rng(MapStyle.Key(piece[0]), SaltDrift + side));
            }
        }

        // Drifts every 8 m along one side of a street piece (six in ten of them), each a low mound of its own size.
        private static void DriftRow(OutdoorBatches into, List<Vector2> piece, float roadWidth, int side, System.Random rng)
        {
            float offset = roadWidth * 0.5f + 0.3f;
            foreach (Vector2 pos in MapGeometry.SampleAlong(piece, DriftInterval))
            {
                if (rng.NextDouble() < 0.4) continue;
                Vector2 dir = MapGeometry.DirectionAt(piece, pos);
                Vector2 at = pos + new Vector2(-dir.y, dir.x).normalized * offset * side;
                float size = DriftMinSize + (float)rng.NextDouble() * (DriftMaxSize - DriftMinSize);
                Vector3 centre = To3(at, MapStyle.StreetY + 0.001f);
                into.Solid(centre).Ellipsoid(centre, new Vector3(size, DriftHeight, size), DecorMaterials.Snow);
            }
        }

        /// <summary>Plowed snow piled at the roadside of the town's streets (bridges and tunnels aside): one or two tries a
        /// segment, a little off the kerb where the roadside is free, each pile with a small collider.</summary>
        internal static void Piles(Site site)
        {
            var rng = new System.Random(site.Town.Rng.Next());
            int placed = 0;
            foreach (Street s in site.Area.Streets)
            {
                if (!s.IsTownStreet || s.Data.tunnel || s.Data.bridge) continue;
                for (int i = 0; i + 1 < s.Pts.Count && placed < MaxPiles; i++)
                {
                    Vector2 a = s.Pts[i], b = s.Pts[i + 1];
                    float len = Vector2.Distance(a, b);
                    if (len < 0.1f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(-d.y, d.x);
                    int tries = rng.Next(1, 3);
                    for (int t = 0; t < tries && placed < MaxPiles; t++)
                    {
                        float along = MapStyle.Range(rng, 0f, len);
                        float off = MapStyle.Range(rng, 0.3f, 0.8f);
                        Vector2 at = a + d * along + n * (s.Data.width * 0.5f + off);
                        if (!site.FreeBeside(at, 0.5f)) continue;
                        float w = MapStyle.Range(rng, 0.5f, 1.0f);
                        float depth = MapStyle.Range(rng, 0.5f, 1.0f);
                        float h = MapStyle.Range(rng, 0.12f, 0.28f);
                        float yaw = MapStyle.Range(rng, -15f, 15f);
                        Vector3 at3 = To3(at, 0f);
                        RaidMesh m = site.Town.Out.Solid(at3);
                        m.SetFrame(at3, yaw);
                        DecorProps.SnowPile(m, rng, Vector3.zero, w, depth, h, 0f);
                        m.ResetFrame();
                        site.Town.PropCollider(at3 + Vector3.up * (h * 0.5f), new Vector3(w, h, depth), yaw);
                        site.Take(at, 0.5f);
                        placed++;
                    }
                }
            }
        }
    }
}
