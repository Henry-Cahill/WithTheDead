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
        // height of the lower and upper horizontal rails measured from the ground (metres)
        private const float LowerRailHeight = 0.25f;
        private const float UpperRailHeight = 0.40f;
        // latch bar thickness (metres) – a thin metal piece that hangs across the gate
        private const float LatchThickness = 0.02f;
// metal cap dimensions (width, height, depth) placed atop each post
         private static readonly Vector3 PostCapSize = new Vector3(0.04f, 0.02f, 0.04f);
                // segments longer than this become a gate (period‑correct driveway entry)
         private const float GateMinSpan = 3f;
         // dimensions for the "No Parking" sign (metres)
         private const float SignWidth = 0.8f;
         private const float SignHeight = 0.4f;
         private const float SignDepth = 0.02f;
         private static readonly Vector3 SignSize = new Vector3(SignWidth, SignHeight, SignDepth);
        // material – reuse the street‑lamp metal matte colour defined in KentuckyMap.Ground.cs
        private static readonly Surf MetalSurf = KentuckyMap.StreetLampMetal;
        /// <summary>
        /// Creates a simple metal latch across a gate.
        /// </summary>
        private static void AddGateLatch(OutdoorBatches into, Vector3 gateCentre, float gateYaw, float gateSpan)
        {
            // latch runs across ~60 % of the gate width, sits a little higher than the gate panel
            float latchLength = gateSpan * 0.6f;
            Vector3 latchSize = new Vector3(latchLength, LatchThickness, BarThickness.z);
            // raise the latch 0.08 m above the gate panel for a realistic look
            Vector3 latchCentre = gateCentre + new Vector3(0f, LatchThickness * 2f, 0f);
            var latchMesh = into.Solid(latchCentre);
            latchMesh.Box(latchCentre, latchSize, gateYaw, MetalSurf);
        }

        /// <summary>
        /// Adds a horizontal rail between two fence posts.
        /// </summary>
        /// <param name="into">OutdoorBatches to receive the geometry.</param>
        /// <param name="a">Start point of the edge (2‑D).</param>
        /// <param name="b">End point of the edge (2‑D).</param>
        /// <param name="y">Base ground height of the fence.</param>
        /// <param name="railHeight">Height from ground at which the rail centre sits.</param>
        private static void AddRail(OutdoorBatches into, Vector2 a, Vector2 b, float y, float railHeight)
        {
            float span = Vector2.Distance(a, b);
            // yaw of the rail (degrees, clockwise from +X)
            float yaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            Vector2 mid2D = (a + b) * 0.5f;
            // rail thickness uses the same Y thickness as BarThickness.y
            Vector3 railSize = new Vector3(span, BarThickness.y, BarThickness.z);
            // centre is at the computed height plus half the rail thickness so it sits on the intended level
            Vector3 centre = KentuckyMap.To3(mid2D, y + railHeight);
            var railMesh = into.Solid(centre);
            railMesh.Box(centre, railSize, yaw, MetalSurf);
        }


        // Adds a “No Parking” sign at the lot entrance.
        private static void AddEntranceSign(OutdoorBatches into, Vector2 pos, float y, float yaw)
        {
            // place the sign a little outward from the fence edge (0.3 m) and at eye height (1.2 m)
            Vector3 centre = KentuckyMap.To3(
                pos + new Vector2(Mathf.Cos(yaw * Mathf.Deg2Rad), Mathf.Sin(yaw * Mathf.Deg2Rad)) * 0.3f,
                y + 1.2f);
            var signMesh = into.Solid(centre);
            signMesh.Box(centre, SignSize, yaw, MetalSurf);
        }

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
                // apply a tiny random offset (±5 cm) so the fence looks hand‑built
                Vector2 jitter = new Vector2(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
                Vector2 jitteredPos = pos + jitter;
                Vector3 postCentre = KentuckyMap.To3(jitteredPos, y + PostSize.y * 0.5f);
                var postMesh = into.Solid(postCentre);
                postMesh.Box(postCentre, PostSize, 0f, MetalSurf);
                // Add metal cap atop the post
                Vector3 capCentre = KentuckyMap.To3(pos, y + PostSize.y + PostCapSize.y * 0.5f);
                var capMesh = into.Solid(capCentre);
                capMesh.Box(capCentre, PostCapSize, 0f, MetalSurf);
            }

            // add a “No Parking” sign at the first post (entrance)
            if (sampled.Count > 0)
            {
                Vector2 first = sampled[0];
                // yaw points outward from the lot; compute the outward normal of the first edge segment
                Vector2 next = sampled[1 % sampled.Count];
                float edgeYaw = -Mathf.Atan2(next.y - first.y, next.x - first.x) * Mathf.Rad2Deg;
                AddEntranceSign(into, first, y, edgeYaw);
            }

            for (int i = 0; i < sampled.Count; i++)
            {
                Vector2 a = sampled[i];
                Vector2 b = sampled[(i + 1) % sampled.Count];
                float span = Vector2.Distance(a, b);
                // If the gap is wide enough, place a gate instead of a simple top bar.
                    // Wide driveway gap – build a gate panel and hang a period‑correct metal latch across it.
                {
                 Vector2 mid2D = (a + b) * 0.5f;
                    float yaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                    Vector3 gateSize = new Vector3(span, PostSize.y, BarThickness.z);
                    Vector3 gateCentre = KentuckyMap.To3(mid2D, y + PostSize.y * 0.5f);
                    var gateMesh = into.Solid(gateCentre);
                    gateMesh.Box(gateCentre, gateSize, yaw, MetalSurf);
                    // add a simple metal latch across the gate panel
                    AddGateLatch(into, gateCentre, yaw, span);
                    continue; // skip normal bar creation for this segment
                }
                // Existing short‑span bar logic (unchanged, but keep the 0.1 m tolerance check).
                if (span > PostInterval + 0.1f) continue;
                Vector2 barMid2D = (a + b) * 0.5f;
                float length = span;
                Vector3 barSize = new Vector3(length, BarThickness.y, BarThickness.z);
                float barYaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                 Vector3 barCentre = KentuckyMap.To3(barMid2D, y + PostSize.y + BarThickness.y * 0.5f);
                // add lower and upper horizontal rails for short‑span segments
                AddRail(into, a, b, y, LowerRailHeight);
                AddRail(into, a, b, y, UpperRailHeight);
            }
        }
    }
}
