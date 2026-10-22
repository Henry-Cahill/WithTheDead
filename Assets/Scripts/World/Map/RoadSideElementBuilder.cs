using UnityEngine;
using System;

namespace CloneZ
{
    /// <summary>
    /// Builds concrete kerbs alongside roads that have a sidewalk.
    /// </summary>
    internal static class RoadSideElementBuilder
    {
        private const float KerbHeight = 0.08f; // metres tall concrete kerb
        private const float KerbCapSize = 0.06f; // width/depth of the end‑cap slab (metres)
        private const float CapHeightJitter = 0.02f; // max random height variation (metres)
        internal static void BuildKerbs(KentuckyMap.Site site, KentuckyMap.Street street, MapBuild build)
        {
            if (!build.HasSidewalk(street.Data.id)) return; // no sidewalk → no kerb

            var paintMat = Resources.Load<Material>(WorldStore.KerbMaterialPath);
            if (paintMat == null) paintMat = Resources.Load<Material>("Materials/DefaultGray");
            var surf = new Surf(paintMat, Vector2.zero, 0f);

            var pts = street.Pts;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                var dir = (b - a).normalized;
                float len = Vector2.Distance(a, b);
                float yaw = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                // place kerb on the outer side of the sidewalk (right side when travelling a→b)
                var offset = new Vector2(dir.y, -dir.x) * (RoadMarkingsBuilder.SideLineOffset + 0.15f);
                var centre = (a + b) * 0.5f + offset;
                var box = site.Overhead.At(new Vector3(centre.x, 0f, centre.y));
                box.Box(Vector3.zero, new Vector3(0.20f, RoadSideElementBuilder.KerbHeight, len), yaw, surf);
                // add end‑caps at both segment ends
                BuildKerbCap(site, a, dir, build, paintMat);
                BuildKerbCap(site, b, dir, build, paintMat);
            }
        }

        private static void BuildKerbCap(KentuckyMap.Site site, Vector2 point, Vector2 dir,
            MapBuild build, Material mat)
        {
            // offset outward the same way the kerb itself is placed
            var offset = new Vector2(dir.y, -dir.x) * (RoadMarkingsBuilder.SideLineOffset + 0.15f);
            var centre = point + offset;
            // randomise height a little for a weathered look
            float height = KerbHeight + (float)(build.Random.NextDouble() * CapHeightJitter);
            var surf = new Surf(mat, Vector2.zero, 0f);
            var box = site.Overhead.At(new Vector3(centre.x, 0f, centre.y));
            float yaw = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            box.Box(Vector3.zero,
                new Vector3(KerbCapSize, height, KerbCapSize),
                yaw, surf);
        }
    }
}
