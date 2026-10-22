using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Builds concrete kerbs alongside roads that have a sidewalk.
    /// </summary>
    internal static class RoadSideElementBuilder
    {
        internal static void BuildKerbs(KentuckyMap.Site site, KentuckyMap.Street street, MapBuild build)
        {
            if (!build.HasSidewalk(street.Data.id)) return; // no sidewalk → no kerb

            var paintMat = Resources.Load<Material>(WorldStore.KerbMaterialPath);
            var surf = paintMat != null
                ? new Surf(paintMat, Vector2.zero, 0f)
                : DecorMaterials.Concrete;

            var pts = street.Pts;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                var dir = (b - a).normalized;
                float len = Vector2.Distance(a, b);
                float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                const float sidewalkWidth = 1.5f; // Match KentuckyMap's sidewalk builder.
                var normal = new Vector2(-dir.y, dir.x);
                float edgeOffset = street.Data.width * 0.5f + sidewalkWidth;
                for (int side = -1; side <= 1; side += 2)
                {
                    var centre = (a + b) * 0.5f + normal * (side * edgeOffset);
                    var position = new Vector3(centre.x, 0f, centre.y);
                    var box = site.Overhead.At(position);
                    box.Box(position, new Vector3(0.20f, RoadSideElementBuilder.KerbHeight, len), yaw, surf);
                }
            }
        }

        private const float KerbHeight = 0.08f; // metres tall concrete kerb
    }
}
