using UnityEngine;
using CloneZ;

namespace CloneZ
{
    internal static class PorchBuilder
    {
        /// <summary>
        /// Adds a modest front porch (low steps and a wooden railing) to a building footprint
        /// when MapBuild.HasPorch(buildingId) returns true.
        /// </summary>
        public static void Build(KentuckyMap.Site site, KentuckyMap.Footprint fp, MapBuild build)
        {
            if (!build.HasPorch(fp.Data.id)) return;
            // Determine porch depth and step height (period‑appropriate, small)
            const float stepDepth = 0.8f; // metres
            const float stepHeight = 0.2f; // metres
            const int stepCount = 3;
            const float railingHeight = 0.9f;
            // Front edge centre (assume first point of footprint is front side)
            Vector2 front = fp.Pts[0];
            Vector2 dir = (fp.Pts[1] - fp.Pts[0]).normalized; // edge direction
            Vector2 normal = new Vector2(-dir.y, dir.x); // outward normal
            // Build steps
            for (int i = 0; i < stepCount; i++)
            {
                float curDepth = stepDepth * (i + 1);
                float curHeight = stepHeight * (i + 1);
                Vector3 pos = new Vector3(front.x + normal.x * curDepth, curHeight / 2f, front.y + normal.y * curDepth);
                var m = site.Overhead.At(pos);
                // a simple box for each step
                m.Box(new Vector3(0f, 0f, 0f), new Vector3(stepDepth, stepHeight, fp.Size * 0.4f), DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f)));

            }
            // Build simple railing at the top step
            Vector3 railPos = new Vector3(front.x + normal.x * (stepDepth * stepCount), stepHeight * stepCount + railingHeight / 2f, front.y + normal.y * (stepDepth * stepCount));
            var railMesh = site.Overhead.At(railPos);
                railMesh.Box(new Vector3(0f, 0f, 0f), new Vector3(stepDepth, railingHeight, 0.07f), DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f)));

            // Reserve space so other generators don’t overlap
            float radius = stepDepth * stepCount + 1f;
            site.Take(new Vector2(front.x + normal.x * radius, front.y + normal.y * radius), radius);
        }
    }
}
