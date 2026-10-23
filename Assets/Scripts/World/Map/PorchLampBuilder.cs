using UnityEngine;
using CloneZ;

namespace CloneZ
{
    internal static class PorchLampBuilder
    {
        /// <summary>
        /// Builds a period‑correct porch lantern (pole, glass envelope, and a Light component)
        /// and attaches it to the town geometry so it casts light at night.
        /// </summary>
        public static void CreateLamp(KentuckyMap.Site site, Vector3 postPos, MapBuild build)
        {
            // ----- pole -----
            var polePos = postPos + new Vector3(0f, 0.5f, 0f);
            var poleMesh = site.Town.Out.Solid(polePos);
            poleMesh.Box(Vector3.zero,
                new Vector3(0.07f, 1f, 0.07f),
                DecorMaterials.WoodFloor(build.GetBuildingColour(build.Random.Next())));

            // ----- glass envelope -----
            var glassPos = polePos + new Vector3(0f, 0.55f, 0f);
            var glassMesh = site.Town.Out.Solid(glassPos);
            var glassMat = WorldStore.GetStreetLampGlassMaterial();
            glassMesh.Box(Vector3.zero,
                new Vector3(0.12f, 0.12f, 0.12f),
                new Surf(glassMat, Vector2.zero, 0f));

            // ----- Light component -----
            var lampGO = new GameObject("PorchLampLight");
            lampGO.transform.SetParent(site.Town.World, false);
            lampGO.transform.localPosition = glassPos;
            var light = lampGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.intensity = 2f;
            light.range = 6f;
            light.shadows = LightShadows.Soft;
        }
    }
}
