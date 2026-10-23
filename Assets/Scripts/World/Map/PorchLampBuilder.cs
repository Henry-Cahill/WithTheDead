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
                new Surf(build.GetStreetLampMaterial(), Vector2.zero, 0f));

            // ----- glass envelope -----
            var glassPos = polePos + new Vector3(0f, 0.55f, 0f);
            var glassMesh = site.Town.Out.Solid(glassPos);
            var glassMat = WorldStore.GetStreetLampGlassMaterial();
            glassMesh.Box(Vector3.zero,
                new Vector3(0.12f, 0.12f, 0.12f),
                new Surf(glassMat, Vector2.zero, 0f));

            // ----- iron finial -----
            var finialPos = glassPos + new Vector3(0f, 0.07f, 0f);
            var finialMesh = site.Town.Out.Solid(finialPos);
            finialMesh.Box(Vector3.zero,
                new Vector3(0.04f, 0.04f, 0.04f),
                new Surf(build.GetStreetLampMaterial(), Vector2.zero, 0f));

            // ----- reserve footprint -----
            float radius = 0.3f;
            site.Take(postPos, radius);
            site.Take(glassPos, radius);

            // ----- random yaw -----
            float yawDeg = Mathf.Lerp(-15f, 15f, (float)build.Random.NextDouble());
            Quaternion yaw = Quaternion.Euler(0f, yawDeg, 0f);

            // ----- parent GameObject -----
            var lampRoot = new GameObject("PorchLamp");
            lampRoot.transform.SetParent(site.Town.World, false);
            lampRoot.transform.position = postPos;
            lampRoot.transform.rotation = yaw;

            // move meshes under root
            // meshes are placed directly in world space; no need to reparent them

            // ----- Light component -----
            var lampGO = new GameObject("PorchLampLight");
            lampGO.transform.SetParent(lampRoot.transform, false);
            lampGO.transform.localPosition = glassPos - postPos; // local offset
            var light = lampGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.intensity = 2f;
            light.range = 6f;
            light.shadows = LightShadows.Soft;

            // ----- collider for whole lamp -----
            var coll = lampRoot.AddComponent<BoxCollider>();
            coll.center = Vector3.up * 0.5f;
            coll.size = new Vector3(radius * 2f, 1.2f, radius * 2f);

            // attach flicker helper
            lampRoot.AddComponent<PorchLampFlicker>();
        }
    }
}
