using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Creates a tiny point‑light for a street‑lamp head.
    /// </summary>
    internal static class LampLightBuilder
    {
        internal static void CreateLampLight(Vector3 worldPos, Transform parent)
        {
            var go = new GameObject("LampLight");
            go.transform.SetParent(parent, worldPositionStays: true);
            go.transform.position = worldPos;
            var lt = go.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = RoadMarkingsBuilder.LampLightColor;
            lt.intensity = RoadMarkingsBuilder.LampLightIntensity;
            lt.range = RoadMarkingsBuilder.LampLightRange;
            lt.shadows = LightShadows.Soft;
            // lights are static – no need for a collider or Rigidbody
        }
    }
}
