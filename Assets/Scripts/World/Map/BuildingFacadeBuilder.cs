using System;
using UnityEngine;

namespace CloneZ
{
    // Generates additional façade details for each building using deterministic helpers from MapBuild.
    // This is invoked after the main building geometry has been created.
    public static class BuildingFacadeBuilder
    {
        /// <summary>Loop over all buildings in the site and add façade features.</summary>
        internal static void BuildAll(KentuckyMap.Site site, MapBuild build)
        {
            foreach (var f in site.Area.Buildings)
            {
                BuildFacade(site, f, build);
            }
        }

        // Add thin snow on roofs, porch steps and fence posts where appropriate.
        private static void BuildFacade(KentuckyMap.Site site, KentuckyMap.Footprint fp, MapBuild build)
        {
            long id = fp.Data.id;
            // Use deterministic colour helper – currently the colour is applied elsewhere, but we call it to ensure the API is exercised.
            var colour = build.GetBuildingColour(id);
            // Roof snow – thin layer if the helper says so.
            if (build.RoofHasSnow(id))
            {
                // Approximate roof height: use building height if present, otherwise 0.
                float roofY = fp.Data.height > 0f ? fp.Data.height : 0f;
                roofY += 0.02f * 0.5f; // Snow depth half‑height offset
                var center = new Vector3(fp.Middle.x, roofY, fp.Middle.y);
var size = new Vector3(fp.Bounds.width, 0.02f, fp.Bounds.height);
                site.Overhead.At(fp.Middle).Box(center, size, DecorMaterials.Snow);
            }

            // Optional porch steps for residential buildings.
            if (build.HasPorch(id) && (fp.Data.kind == "house" || fp.Data.kind == "trailer" || fp.Data.kind == "apartments" || fp.Data.kind == "hotel"))
            {
                // Simple box representing stairs in front of the main entrance if a door exists.
                // Find a door edge similar to Enterable logic – reuse the first wall edge.
                var pts = fp.Pts;
                if (pts.Count >= 2)
                {
                    var a = pts[0];
                    var b = pts[1];
                    var d = (b - a).normalized;
                    var outward = new Vector2(d.y, -d.x);
                    // Approximate door point at middle of first edge.
                    var doorPoint = a + d * 0.5f;
                    var porchCenter = new Vector3(doorPoint.x + outward.x * 0.4f, 0.1f, doorPoint.y + outward.y * 0.4f);
                    var porchSize = new Vector3(1.5f, 0.2f, 0.8f);
                    site.Town.Out.Solid(porchCenter).Box(porchCenter, porchSize, 0f, DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f)));
                }
            }

            // Fence posts around the perimeter for applicable buildings.
            if (build.HasFence(id))
            {
                // Place a simple post at each corner of the footprint.
                foreach (var pt in fp.Pts)
                {
                    var postCenter = new Vector3(pt.x, 0.5f, pt.y);
                    var postSize = new Vector3(0.1f, 1f, 0.1f);
                    site.Town.Out.Solid(postCenter).Box(postCenter, postSize, DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f)));
                }
            }
        }
    }
}
