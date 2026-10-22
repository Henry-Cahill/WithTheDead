using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Builds thin white lane markings on road centre lines.
    /// Now also builds concrete curbs with collidable flat sidewalk strips on both sides of every road.
    /// </summary>
    internal static class RoadMarkingsBuilder
    {
        internal const float SideLineOffset = 0.25f; // metres from road edge
        private const float KerbHeight = 0.08f; // metres tall concrete kerb
        private const float SidewalkOffset = 0.6f; // metres from road edge (kerb + sidewalk)
        private const float SidewalkThickness = 0.02f; // metres tall flat strip

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
            BuildKerbsAndSidewalks(site, street, build);
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

        private static void BuildKerbsAndSidewalks(KentuckyMap.Site site, KentuckyMap.Street street,
            MapBuild build)
        {
            // load concrete kerb material (the map already defines a kerb material path)
            var kerbMat = Resources.Load<Material>(WorldStore.KerbMaterialPath);
            if (kerbMat == null) kerbMat = Resources.Load<Material>("Materials/DefaultConcrete");
            var kerbSurf = new Surf(kerbMat, Vector2.zero, 0f);
            // sidewalk uses the same white paint material as lane markings
            var paintMat = Resources.Load<Material>("Materials/RoadMarking");
            if (paintMat == null) paintMat = Resources.Load<Material>("Materials/DefaultWhite");
            var walkSurf = new Surf(paintMat, Vector2.zero, 0f);

            var pts = street.Pts;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                var dir = (b - a).normalized;
                float len = Vector2.Distance(a, b);
                float yaw = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                // left side – kerb
                var leftKerbCenter = (a + b) * 0.5f + new Vector2(-dir.y, dir.x) * SideLineOffset;
                var leftKerbBox = site.Town.Out.Solid(new Vector3(leftKerbCenter.x, 0f, leftKerbCenter.y));
                leftKerbBox.Box(Vector3.zero,
                    new Vector3(0.10f, KerbHeight, len), // width, height, length
                    yaw, kerbSurf);
                // left side – sidewalk strip (just beyond the curb)
                var leftWalkCenter = leftKerbCenter + new Vector2(-dir.y, dir.x) * SidewalkOffset;
                var leftWalkBox = site.Town.Out.Solid(new Vector3(leftWalkCenter.x, 0f, leftWalkCenter.y));
                leftWalkBox.Box(Vector3.zero,
                    new Vector3(0.20f, SidewalkThickness, len),
                    yaw, walkSurf);
                // right side – kerb
                var rightKerbCenter = (a + b) * 0.5f + new Vector2(dir.y, -dir.x) * SideLineOffset;
                var rightKerbBox = site.Town.Out.Solid(new Vector3(rightKerbCenter.x, 0f, rightKerbCenter.y));
                rightKerbBox.Box(Vector3.zero,
                    new Vector3(0.10f, KerbHeight, len),
                    yaw, kerbSurf);
                // right side – sidewalk strip
                var rightWalkCenter = rightKerbCenter + new Vector2(dir.y, -dir.x) * SidewalkOffset;
                var rightWalkBox = site.Town.Out.Solid(new Vector3(rightWalkCenter.x, 0f, rightWalkCenter.y));
                rightWalkBox.Box(Vector3.zero,
                    new Vector3(0.20f, SidewalkThickness, len),
                    yaw, walkSurf);
            }
        }
    }
}
