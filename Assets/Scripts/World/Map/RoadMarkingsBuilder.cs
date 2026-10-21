using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Builds thin white lane markings on road centre lines.
    /// </summary>
    internal static class RoadMarkingsBuilder
    {
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
                // build a box with length = len, width = 0.12 m (standard dash width), height = thickness
                var centre = (a + b) * 0.5f;
                var box = site.Overhead.At(new Vector3(centre.x, 0.01f, centre.y));
                // size vector: x width, y height, z length (notice orientation: we align length along Z by default)
                var surf = new Surf(paintMat, Vector2.zero, 0f);
                box.Box(new Vector3(0f, 0f, 0f), new Vector3(0.12f, thickness, len), surf);
                // no collider needed – markings are purely decorative
            }
        }
    }
}
