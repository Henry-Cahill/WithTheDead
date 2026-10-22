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
            const float floorThickness = 0.03f; // metres, thin wooden decking
            const int balusterCount = 5; // number of vertical balusters between posts
            // Front edge centre (assume first point of footprint is front side)
            Vector2 front = fp.Pts[0];
            Vector2 dir = (fp.Pts[1] - fp.Pts[0]).normalized; // edge direction
            Vector2 normal = new Vector2(-dir.y, dir.x); // outward normal
            // Compute the porch’s width along the building façade (used for floor and posts)
            float porchWidth = fp.Size * 0.4f; // a reasonable fraction of the footprint width
            // Determine wood colour for this building
            var colour = build.GetBuildingColour(fp.Data.id);
            // Apply a modest random hue shift (±15 %)
            float hueShift = 0.85f + 0.3f * (float)build.Random.NextDouble();
            colour = new Color(colour.r * hueShift, colour.g * hueShift, colour.b * hueShift);
            // Build steps
            for (int i = 0; i < stepCount; i++)
            {
                float curDepth = stepDepth * (i + 1);
                float curHeight = stepHeight * (i + 1);
                Vector3 pos = new Vector3(front.x + normal.x * curDepth, curHeight / 2f, front.y + normal.y * curDepth);
                // place in ground layer with colliders
                var m = site.Town.Out.Solid(pos);
                // a simple box for each step
                m.Box(new Vector3(0f, 0f, 0f), new Vector3(stepDepth, stepHeight, fp.Size * 0.4f), DecorMaterials.WoodFloor(colour));
            }
            // -------------------- porch floor --------------------
            Vector3 floorCenter = new Vector3(
                front.x + normal.x * (stepDepth * stepCount / 2f),
                floorThickness / 2f,
                front.y + normal.y * (stepDepth * stepCount / 2f));
            var floorMesh = site.Town.Out.Solid(floorCenter);
            floorMesh.Box(new Vector3(0f, 0f, 0f), new Vector3(stepDepth * stepCount, floorThickness, porchWidth), DecorMaterials.WoodFloor(colour));

            // -------------------- side posts --------------------
            float postHeight = stepHeight * stepCount + railingHeight;
            Vector3 leftPostPos = floorCenter + new Vector3(-porchWidth / 2f, postHeight / 2f, 0f);
            Vector3 rightPostPos = floorCenter + new Vector3(porchWidth / 2f, postHeight / 2f, 0f);
            var leftPost = site.Town.Out.Solid(leftPostPos);
            leftPost.Box(new Vector3(0f, 0f, 0f), new Vector3(0.07f, postHeight, 0.07f), DecorMaterials.WoodFloor(colour));
            var rightPost = site.Town.Out.Solid(rightPostPos);
            rightPost.Box(new Vector3(0f, 0f, 0f), new Vector3(0.07f, postHeight, 0.07f), DecorMaterials.WoodFloor(colour));

            // -------------------- top rail --------------------
            Vector3 railPos = floorCenter + new Vector3(0f, postHeight - railingHeight / 2f, 0f);
            var railMesh = site.Town.Out.Solid(railPos);
            railMesh.Box(new Vector3(0f, 0f, 0f), new Vector3(stepDepth * stepCount, railingHeight, 0.07f), DecorMaterials.WoodFloor(colour));

            // -------------------- balusters --------------------
            for (int i = 1; i <= balusterCount; i++)
            {
                float t = (float)i / (balusterCount + 1);
                Vector3 balPos = floorCenter + new Vector3(
                    Mathf.Lerp(-porchWidth / 2f + 0.1f, porchWidth / 2f - 0.1f, t),
                    postHeight - railingHeight / 2f,
                    0f);
                var bal = site.Town.Out.Solid(balPos);
                bal.Box(new Vector3(0f, 0f, 0f), new Vector3(0.04f, railingHeight, 0.04f), DecorMaterials.WoodFloor(colour));
            }
            // Reserve space so other generators don’t overlap
            float radius = stepDepth * stepCount + 1f;
            site.Take(new Vector2(front.x + normal.x * radius, front.y + normal.y * radius), radius);
        }
    }
}
