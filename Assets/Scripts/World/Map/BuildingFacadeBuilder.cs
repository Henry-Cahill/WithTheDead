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
            if (fp.Pts.Count < 3 || fp.Size < 6f) return;
            var square = Rect.MinMaxRect(-site.Half, -site.Half, site.Half, site.Half);
            bool inside = fp.Inside(site.Half, 1f);
            if (!inside && fp.Bounds.Overlaps(square)) return;

            long id = fp.Data.id;
            if (build.RoofHasSnow(id) && (!inside || fp.Data.kind != "ruin"))
            {
                float roofY = inside && fp.Data.kind == "canopy" ? 4.2f : KentuckyMap.HeightOf(fp.Data);
                float snowY = roofY + 0.011f;
                RaidMesh roof = inside
                    ? site.Overhead.At(new Vector3(fp.Middle.x, roofY, fp.Middle.y))
                    : site.Town.Far.Solid(new Vector3(fp.Middle.x, roofY, fp.Middle.y));
                var triangles = MapGeometry.Triangulate(fp.Pts);
                for (int i = 0; i + 2 < triangles.Count; i += 3)
                {
                    Vector2 a = fp.Pts[triangles[i]];
                    Vector2 b = fp.Pts[triangles[i + 1]];
                    Vector2 c = fp.Pts[triangles[i + 2]];
                    roof.Tri(new Vector3(a.x, snowY, a.y), new Vector3(b.x, snowY, b.y),
                        new Vector3(c.x, snowY, c.y), DecorMaterials.Snow);
                }
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
var doorPoint = (a + b) * 0.5f;
                    var porchSize = new Vector3(1.5f, 0.2f, 0.8f);
                    // Half a porch-depth out from the wall's midpoint, sitting on the ground.
                    var porchCenter = new Vector3(doorPoint.x + outward.x * 0.4f, 0.1f, doorPoint.y + outward.y * 0.4f);
                    RaidMesh details = inside ? site.Town.Out.Solid(porchCenter) : site.Town.Far.Solid(porchCenter);
                    details.Box(porchCenter, porchSize, 0f, DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f)));
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
                    RaidMesh details = inside ? site.Town.Out.Solid(postCenter) : site.Town.Far.Solid(postCenter);
                    details.Box(postCenter, postSize, DecorMaterials.WoodFloor(new Color(0.55f, 0.40f, 0.26f)));
                }
            }
        }
    }
}
