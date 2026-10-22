using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Builds a low metal fence along a polygon edge – period‑correct for a 1993 rural Kentucky parking lot.
    /// Uses the existing StreetLampMetal material for a plausible metal look.
    /// </summary>
    internal static class ParkingLotFenceHelper
    {
        // distance between fence posts (metres)
        private const float PostInterval = 2f;
        // post dimensions (width, height, depth) in metres
        private static readonly Vector3 PostSize = new Vector3(0.04f, 0.5f, 0.04f);
        // horizontal bar dimensions (thickness; length will be set per segment)
        private static readonly Vector3 BarThickness = new Vector3(0.02f, 0.02f, 0.02f);
        // material – reuse the street‑lamp metal matte colour defined in KentuckyMap.Ground.cs
        private static readonly Surf MetalSurf = KentuckyMap.StreetLampMetal;

        /// <summary>
        /// Adds fence posts and connecting top bars along the given edge polyline.
        /// </summary>
        /// <param name="into">OutdoorBatches to receive the geometry.</param>
        /// <param name="edge">Closed polyline that defines the lot edge (already used for the ice ribbon).</param>
        /// <param name="y">Height at which the fence sits (just above the ice edge).</param>
        public static void AddFence(OutdoorBatches into, List<Vector2> edge, float y)
        {
            // Sample positions along the edge at a fixed interval.
            var sampled = new List<Vector2>(KentuckyMap.SampleAlong(edge, PostInterval));
            if (sampled.Count == 0) return;

            // Create a post at each sampled position.
            foreach (Vector2 pos in sampled)
            {
                Vector3 postCentre = KentuckyMap.To3(pos, y + PostSize.y * 0.5f);
                var postMesh = into.Solid(postCentre);
                postMesh.Box(postCentre, PostSize, 0f, MetalSurf);
            }

            // Connect successive posts with a horizontal top bar.
            for (int i = 0; i < sampled.Count; i++)
            {
                Vector2 a = sampled[i];
                Vector2 b = sampled[(i + 1) % sampled.Count];
                // Only add a bar if the two points are close enough (prevents long spans across gaps).
                if (Vector2.Distance(a, b) > PostInterval + 0.1f) continue;

                Vector2 mid2D = (a + b) * 0.5f;
                float length = Vector2.Distance(a, b);
                // Bar runs horizontally between the two posts, centred 0.25 m above post tops.
                Vector3 barSize = new Vector3(length, BarThickness.y, BarThickness.z);
                float yaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                Vector3 barCentre = KentuckyMap.To3(mid2D, y + PostSize.y + BarThickness.y * 0.5f);
                var barMesh = into.Solid(barCentre);
                barMesh.Box(barCentre, barSize, yaw, MetalSurf);
            }
        }
    }
}
