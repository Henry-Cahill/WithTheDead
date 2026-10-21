using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Builds thin white lane markings on road centre lines.
    /// </summary>
    internal static class RoadMarkingsBuilder
    {
        internal const float SideLineOffset = 0.25f; // metres from road edge
        private const float KerbHeight = 0.08f; // metres tall concrete kerb

        /// <summary>
        /// Adds road markings for a given street if the deterministic decision says so.
        /// </summary>
        internal static void BuildForStreet(KentuckyMap.Site site, KentuckyMap.Street street, MapBuild build)
        {
            // only run if this segment was selected for markings
            if (!build.HasRoadMarkings(street.Data.id)) return;

            // material – use the same “road” material but tint white; the map already has a white paint texture
            var paintMat = Resources.Load<Material>("Materials/RoadMarking");
            if (paintMat == null) paintMat = Resources.Load<Material>("Materials/DefaultWhite");

            float thickness = build.GetRoadMarkingThickness();
            var pts = street.Pts;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                // direction on the XZ‑plane
                var dir = (b - a).normalized;
                // length of this sub‑segment
                float len = Vector2.Distance(a, b);
                // centre of segment
                var centre = (a + b) * 0.5f;
                var box = site.Overhead.At(new Vector3(centre.x, 0.01f, centre.y));
                var surf = new Surf(paintMat, Vector2.zero, 0f);
                // dash loop
                float yaw = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                float dashLen = 0.8f; // length of a white dash
                float gapLen = 0.4f; // gap between dashes
                float travelled = 0f;
                while (travelled < len)
                {
                    float curLen = Mathf.Min(dashLen, len - travelled);
                    var dashCenter = new Vector3(0f, 0f, travelled + curLen * 0.5f - len * 0.5f);
                    box.Box(dashCenter, new Vector3(0.12f, thickness, curLen), yaw, surf);
                    travelled += curLen + gapLen;
                }
                // no collider needed – markings are purely decorative
            }
            // side-line strips (always visible)
            BuildSideLines(site, street, build, new Surf(paintMat, Vector2.zero, 0f));
            // kerbs for sidewalks
            RoadSideElementBuilder.BuildKerbs(site, street, build);
        }

        private static void BuildSideLines(KentuckyMap.Site site, KentuckyMap.Street street, MapBuild build, Surf surf)
        {
            var pts = street.Pts;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                var dir = (b - a).normalized;
                float len = Vector2.Distance(a, b);
                float yaw = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                // left edge
                var leftCenter = (a + b) * 0.5f + new Vector2(-dir.y, dir.x) * SideLineOffset;
                var leftBox = site.Overhead.At(new Vector3(leftCenter.x, 0.01f, leftCenter.y));
                leftBox.Box(Vector3.zero, new Vector3(0.05f, build.GetRoadMarkingThickness(), len), yaw, surf);
                // right edge
                var rightCenter = (a + b) * 0.5f + new Vector2(dir.y, -dir.x) * SideLineOffset;
                var rightBox = site.Overhead.At(new Vector3(rightCenter.x, 0.01f, rightCenter.y));
                rightBox.Box(Vector3.zero, new Vector3(0.05f, build.GetRoadMarkingThickness(), len), yaw, surf);
            }
        }
    }
}
