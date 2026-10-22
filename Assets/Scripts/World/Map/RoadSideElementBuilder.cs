using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Builds concrete kerbs alongside roads that have a sidewalk.
    /// </summary>
    internal static class RoadSideElementBuilder
    {
        private const float KerbHeight = 0.08f; // metres tall concrete kerb
        private const float KerbCapSize = 0.06f; // width/depth of the end‑cap slab (metres)
        private const float DrainWidth = 0.12f; // metres, width/depth of metal drain
        private const float DrainHeight = 0.04f; // metres tall
        private const float DrainRotJitter = 5f; // degrees of random yaw added to the drain
        private const float CapHeightJitter = 0.02f; // max random height variation (metres)
        private const float SnowMoundHeight = 0.04f; // metres, thin snow drift on kerb top
        private const float TrashBinWidth = 0.35f; // metres, approximate width of a 1990s metal trash bin
        internal static void BuildKerbs(KentuckyMap.Site site, KentuckyMap.Street street, MapBuild build)
        {
            if (!build.HasSidewalk(street.Data.id)) return; // no sidewalk → no kerb

            var paintMat = Resources.Load<Material>(WorldStore.KerbMaterialPath);
            var surf = paintMat != null
                ? new Surf(paintMat, Vector2.zero, 0f)
                : DecorMaterials.Concrete;
            var snowSurf = DecorMaterials.Snow; // white concrete‑like snow material defined in DecorMaterials

            var pts = street.Pts;
            for (int i = 0; i < pts.Count - 1; i++) {

                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                var dir = (b - a).normalized;
                float len = Vector2.Distance(a, b);
                float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                const float sidewalkWidth = 1.5f; // Match KentuckyMap's sidewalk builder.
                var normal = new Vector2(-dir.y, dir.x);
                float edgeOffset = street.Data.width * 0.5f + sidewalkWidth;
                for (int side = -1; side <= 1; side += 2) {

                    var centre = (a + b) * 0.5f + normal * (side * edgeOffset);
                    var position = new Vector3(centre.x, 0f, centre.y);
                    var box = site.Overhead.At(position);
                    float segmentHeight = KerbHeight + (float)(build.Random.NextDouble() * CapHeightJitter);
                    box.Box(Vector3.zero, new Vector3(0.20f, segmentHeight, len), yaw, surf);
                    BuildKerbCap(site, a, normal, side, edgeOffset, yaw, build, surf);
                    BuildKerbDrain(site, a, normal, side, edgeOffset, yaw, build, surf);
                    BuildKerbCap(site, b, normal, side, edgeOffset, yaw, build, surf);
            // new snow drifts on each end point
            BuildKerbSnow(site, a, normal, side, edgeOffset, yaw, snowSurf);
            BuildKerbSnow(site, b, normal, side, edgeOffset, yaw, snowSurf);
            // randomly place a trash bin on ~10 % of segments (once per side)
            if (build.Random.NextDouble() < 0.10)
            {
                // place the bin near the midpoint of the segment so it isn’t duplicated at both ends
                var midPoint = (a + b) * 0.5f;
                BuildTrashBin(site, midPoint, normal, side, edgeOffset, build);
            }
                }
                // -------------------------------------------------
                // Add street lamps on a subset of segments
                // -------------------------------------------------
                if (build.HasSidewalk(street.Data.id) && build.HasStreetLamp(street.Data.id))
                {
                    // place a single lamp at the midpoint of the segment
                    StreetLampBuilder.BuildLamp(site, street, build);
                }
            }
        }

        private static void BuildKerbCap(KentuckyMap.Site site, Vector2 point, Vector2 normal,
            int side, float edgeOffset, float yaw, MapBuild build, Surf surf)
        {
            var centre = point + normal * (side * edgeOffset);
            float height = KerbHeight + (float)(build.Random.NextDouble() * CapHeightJitter);
            var box = site.Overhead.At(new Vector3(centre.x, 0f, centre.y));
            box.Box(Vector3.zero,
                new Vector3(KerbCapSize, height, KerbCapSize),
                yaw, surf);
        }

        private static void BuildKerbDrain(KentuckyMap.Site site, Vector2 point, Vector2 normal,
            int side, float edgeOffset, float yaw, MapBuild build, Surf surf)
        {
            // Determine centre similar to cap.
            var centre = point + normal * (side * edgeOffset);
            var box = site.Overhead.At(new Vector3(centre.x, 0f, centre.y));
            // Random rotation jitter around vertical axis.
            float rotJitter = (float)(build.Random.NextDouble() * DrainRotJitter * 2.0 - DrainRotJitter);
            float finalYaw = yaw + rotJitter;
            // Drain height is fixed, width/depth defined by DrainWidth.
            box.Box(Vector3.zero,
                new Vector3(DrainWidth, DrainHeight, DrainWidth),
                finalYaw, surf);
        }


        // Adds a thin snow drift on the kerb top at a segment end point.
        private static void BuildKerbSnow(KentuckyMap.Site site, Vector2 point, Vector2 normal,
            int side, float edgeOffset, float yaw, Surf snowSurf)
        {
            // centre is the same as cap/drain
            var centre = point + normal * (side * edgeOffset);
            var box = site.Overhead.At(new Vector3(centre.x, KerbHeight, centre.y));
            // a thin flat box sitting on top of the kerb; width/depth match the kerb width
            box.Box(Vector3.zero,
                new Vector3(KerbCapSize, SnowMoundHeight, KerbCapSize),
                yaw, snowSurf);
        }

    
        // Builds a simple rectangular trash bin on the sidewalk.
        private static void BuildTrashBin(KentuckyMap.Site site, Vector2 point, Vector2 normal,
            int side, float edgeOffset, MapBuild build)
        {
            // trash bins belong to the walkable ground – use Town.Out and a PropCollider
            // offset a little away from the curb so it sits on the sidewalk
            const float sidewalkSetback = 0.4f; // metres from the kerb edge
            var centre = point + normal * (side * (edgeOffset + sidewalkSetback));
            var pos = new Vector3(centre.x, 0f, centre.y);
            if (!site.Free(pos, TrashBinWidth * 0.5f)) return; // avoid overlap
            var box = site.Town.Out.Solid(pos);
            // simple rectangular bin
            box.Box(Vector3.zero,
                new Vector3(TrashBinWidth, 0.60f, TrashBinWidth),
                0f, DecorPalette.Metal(new Color(0.5f, 0.5f, 0.5f)));
            site.Take(pos, TrashBinWidth * 0.5f);
        }


    }
}
