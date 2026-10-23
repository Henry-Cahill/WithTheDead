using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Creates a wooden pole and a metal street‑name sign.
    /// </summary>
    internal static class SignpostBuilder
    {
        internal static void BuildSignpost(KentuckyMap.Site site, Vector2 point, Vector2 normal,
            int side, float edgeOffset, MapBuild build, string streetLabel)
        {
            // Position the pole a short setback from the curb so it sits on the sidewalk.
            const float sidewalkSetback = 0.6f; // metres from kerb edge
            var centre = point + normal * (side * (edgeOffset + sidewalkSetback));
            var pos = new Vector3(centre.x, 0f, centre.y);
            // Avoid overlapping other props.
            float radius = RoadSideElementBuilder.SignPostPoleRadius;
            if (!site.Free(pos, radius)) return;
            // ----- pole -----
            var poleBox = site.Town.Out.Solid(pos);
            // Use a simple cylindrical pole; DecorMaterials.WoodFloor gives a rough wood texture.
            var poleSurf = DecorMaterials.WoodFloor(new Color(0.43f, 0.33f, 0.26f));
            poleBox.Cylinder(new Vector3(0f, RoadSideElementBuilder.SignPostPoleHeight * 0.5f, 0f),
                RoadSideElementBuilder.SignPostPoleRadius,
                RoadSideElementBuilder.SignPostPoleHeight,
                8, poleSurf);
            // ----- sign -----
            // Create a metal rectangular sign with the street name.
            // street name comes from OSM; fall back to the segment id if empty.

            // Create a metal rectangular sign with the street name.
            var signSurf = DecorMaterials.Sign(streetLabel, new Color(0.2f, 0.2f, 0.2f), Color.white, out float _);
            // Sign sits near the top of the pole, offset outward from the road.
            var signCenter = pos + new Vector3(0f, RoadSideElementBuilder.SignPostPoleHeight - 0.1f, 0f)
                + new Vector3(normal.x, 0f, normal.y) * 0.1f;
            var signBox = site.Town.Out.Solid(signCenter);
            signBox.Box(Vector3.zero,
                new Vector3(0.50f, 0.20f, 0.02f), // width, height, depth of metal plate
                0f, signSurf);
            // Mark the space as taken for future prop placement.
            site.Take(pos, radius);
        }
    }
}
