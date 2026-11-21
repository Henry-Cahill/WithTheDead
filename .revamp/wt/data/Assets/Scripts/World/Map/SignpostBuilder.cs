using UnityEngine;
using System;

namespace CloneZ
{
    /// <summary>
    /// Creates a wooden pole and a metal street‑name sign.
    /// </summary>
    internal static class SignpostBuilder
    {
        // metres of random height variation for the pole (±10 %)
        private const float PoleHeightVariance = 0.1f;
        // max yaw (degrees) applied to the sign board for a “hand‑painted” look
        private const float MaxSignYaw = 5f;
        private static readonly UnityEngine.Vector3 LanternSize = new UnityEngine.Vector3(0.12f, 0.18f, 0.12f);

        internal static void BuildSignpost(KentuckyMap.Site site, Vector2 point, Vector2 normal,
            int side, float edgeOffset, MapBuild build, string streetLabel)
{

            // deterministic pseudo‑random based on street name (or fallback id) to keep the map stable
            int randSeed = streetLabel?.GetHashCode() ?? build.Id.GetHashCode();
            var rnd = new System.Random(randSeed);
            // apply a small random yaw to the sign board (0 = parallel to road)
            float signYaw = (float)(rnd.NextDouble() * 2.0 - 1.0) * MaxSignYaw;
            // vary pole height by ±PoleHeightVariance (e.g. 0.9 – 1.1 × base)
            float poleHeightScale = 1f + (float)(rnd.NextDouble() * 2.0 - 1.0) * PoleHeightVariance;
            float poleHeight = RoadSideElementBuilder.SignPostPoleHeight * poleHeightScale;
            // Position the pole a short setback from the curb so it sits on the sidewalk.
            const float sidewalkSetback = 0.6f; // metres from kerb edge
            var centre = point + normal * (side * (edgeOffset + sidewalkSetback));
            var pos = new Vector3(centre.x, 0f, centre.y);
            float radius = RoadSideElementBuilder.SignPostPoleRadius;
            if (!site.Free(pos, radius)) return;
            var poleBox = site.Town.Out.Solid(pos);
            var poleSurf = DecorMaterials.WoodFloor(new Color(0.43f, 0.33f, 0.26f));
            // foot of the pole – a short wooden cylinder that sits flush with the sidewalk
            var footBox = site.Town.Out.Solid(pos);
            float footHeight = 0.05f;
            float footRadius = RoadSideElementBuilder.SignPostPoleRadius * 1.2f;
            footBox.Cylinder(new Vector3(0f, footHeight * 0.5f, 0f), footRadius, footHeight, 8, poleSurf);
            // pole cylinder with varied height
            poleBox.Cylinder(new Vector3(0f, poleHeight * 0.5f, 0f),
                RoadSideElementBuilder.SignPostPoleRadius,
                poleHeight,
                8, poleSurf);
            // ----- sign -----
            // wooden sign board with painted street name – background colour matches aged wood
            var boardBg = new Color(0.45f, 0.35f, 0.25f);
            var signSurf = DecorMaterials.Sign(streetLabel, boardBg, Color.white, out float _);
            // apply the random yaw around the vertical axis
            var signCenter = pos + new Vector3(0f, poleHeight - 0.1f, 0f)
                + new Vector3(normal.x, 0f, normal.y) * 0.1f;
            var signBox = site.Town.Out.Solid(signCenter);
            signBox.Box(Vector3.zero,
                new Vector3(0.50f, 0.20f, 0.02f), // width, height, depth of wooden board
                signYaw, signSurf);
            // small lantern – using the same Sign material but with an empty label and bright amber colour
            var lanternSurf = DecorMaterials.Sign(string.Empty, new Color(0.9f, 0.7f, 0.2f), Color.black, out float _);
            var lanternPos = pos + new Vector3(0f, poleHeight + LanternSize.y * 0.5f, 0f);
            var lanternBox = site.Town.Out.Solid(lanternPos);
            lanternBox.Box(Vector3.zero, LanternSize, 0f, lanternSurf);
            float totalRadius = Mathf.Max(radius, LanternSize.x * 0.5f);
            site.Take(pos, totalRadius);
        }
    }
}
