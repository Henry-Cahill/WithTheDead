using UnityEngine;
using CloneZ;

namespace CloneZ
{
    internal static class StreetLampBuilder
    {
        // Builds a single lamp post on the given street segment.
        // The lamp sits on the sidewalk side, near the road edge.
        private const float CAGE_BAR_THICK = 0.03f; // thickness of each cage bar (metres)
        private const float CAGE_HEIGHT = 1.0f; // height of the cage above the concrete base (metres)
internal static void BuildLamp(KentuckyMap.Site site, KentuckyMap.Street street, MapBuild build)
        {
            // only place a lamp if the OSM data marks this segment as having one
            if (!build.HasStreetLamp(street.Data.id)) return;
            // deterministic RNG seeded by street id for visual variance
            var rng = new System.Random(street.Data.id.GetHashCode());

            // choose the deterministic middle‑segment midpoint and yaw
            var sum = Vector2.zero;
            foreach (var p in street.Pts) sum += p;
            Vector2 mid = sum / street.Pts.Count; // deterministic average of all points
            // deterministic yaw: -5° … +5° based on street id hash
            int hash = street.Data.id.GetHashCode();
            float yaw = ((hash & 0xFF) / 255f) * 10f - 5f;
            // add ±2° jitter per segment
            yaw += (float)(rng.NextDouble() * 4.0 - 2.0);
            // offset outward by half the sidewalk width + small gap
            float offset = street.Data.SidewalkWidth() * 0.5f + 0.2f;
            // street direction vector (ignore vertical y)
            Vector2 dir = (street.Pts[street.Pts.Count - 1] - street.Pts[0]).normalized;
            // perpendicular to the right (assuming road runs forward)
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector3 pos = new Vector3(mid.x + perp.x * offset, 0f, mid.y + perp.y * offset);
            // reserve space (radius ~0.3m)
            if (!site.Free(new Vector2(pos.x, pos.z), 0.3f)) return;
            site.Take(new Vector2(pos.x, pos.z), 0.3f);
            // create a parent GameObject under the outside town layer
            GameObject lampRoot = new GameObject("StreetLamp");
            lampRoot.transform.position = pos;
            // apply yaw to lamp root
            lampRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            lampRoot.transform.SetParent(site.Town.World, false);
            // ---- concrete base ----
            var baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.name = "Base";
            baseCube.transform.SetParent(lampRoot.transform, false);
            baseCube.transform.localScale = new Vector3(0.30f, 0.05f, 0.30f);
            baseCube.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            var porchMat = WorldStore.GetPorchMaterial();
            if (porchMat != null) baseCube.GetComponent<Renderer>().material = porchMat;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(lampRoot.transform, false);
            // vary pole dimensions by street kind
            if (street.Data.kind == "residential")
            {
                pole.transform.localScale = new Vector3(0.07f, 1.2f, 0.07f);
                pole.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            }
            else
            {
                pole.transform.localScale = new Vector3(0.09f, 1.3f, 0.09f);
                pole.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            }
            var mat = build.GetStreetLampMaterial();
            if (mat != null) {
                // create a fresh material instance and apply a deterministic hue shift per street
                var poleMat = new Material(mat);
                float hueShift = (float)(rng.NextDouble() * 0.1 - 0.05);
                // Convert to HSV, shift hue, wrap around [0,1]
                Color.RGBToHSV(poleMat.color, out float h, out float s, out float v);
                h = (h + hueShift) % 1f;
                if (h < 0f) h += 1f;
                poleMat.color = Color.HSVToRGB(h, s, v);
                pole.GetComponent<Renderer>().material = poleMat;
            }
            // add metal cage around pole
            AddMetalCage(lampRoot.transform, pole.transform, rng);
            // add decorative finial on top
            AddFinial(lampRoot.transform, pole.transform, rng);
            var rootCol = lampRoot.AddComponent<SphereCollider>();
            rootCol.radius = 0.35f; // slightly larger to include cage & finial
            rootCol.center = new Vector3(0f, pole.transform.localScale.y + 0.4f, 0f);
            site.Town.PropCollider(rootCol.center,
                new Vector3(rootCol.radius * 2, pole.transform.localScale.y + 0.8f, rootCol.radius * 2), 0f);
            // ---- lamp head ----
            var glassMat = WorldStore.GetStreetLampGlassMaterial();
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(lampRoot.transform, false);
            head.transform.localScale = new Vector3(0.28f, 0.18f, 0.28f);
            head.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            if (glassMat != null) head.GetComponent<Renderer>().material = glassMat;
            // ---- point light ----
            var light = lampRoot.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 2f;
            light.color = new Color(1f, 0.9f, 0.7f);
            light.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            lampRoot.AddComponent<StreetLampInteraction>();
            var flicker = lampRoot.AddComponent<StreetLampFlicker>();
            // configure variation based on street kind
            if (street.Data.kind == "residential")
            {
                flicker._maxSinusoid = 0.08f;
                flicker._wobbleSpeed = 0.4f;
                flicker._dipStrength = 0.15f;
            }
            else
            {
                flicker._maxSinusoid = 0.18f;
                flicker._wobbleSpeed = 0.8f;
                flicker._dipStrength = 0.35f;
            }
        }

        // Helper to add a period‑accurate metal cage around the pole.
        private static void AddMetalCage(Transform parent, Transform pole, System.Random rng)
        {
            // use the same porch material (metal look) as the concrete base
            var metalMat = WorldStore.GetPorchMaterial();
            if (metalMat == null) return;
            // four evenly‑spaced vertical bars, wrapped around the pole
            for (int i = 0; i < 4; i++)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = $"CageBar{i}";
                bar.transform.SetParent(parent, false);
                // scale: thin vertical bar that reaches from base to CAGE_HEIGHT
                bar.transform.localScale = new Vector3(CAGE_BAR_THICK, CAGE_HEIGHT, CAGE_BAR_THICK);
                // place at pole centre, then rotate around it
                float angle = i * 90f + (float)(rng.NextDouble() * 5.0 - 2.5); // ±2.5° jitter
                bar.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                // raise so the bottom sits on top of the concrete base (0.05 m) and centre aligns with pole
                bar.transform.localPosition = new Vector3(0f, 0.05f + CAGE_HEIGHT * 0.5f, 0f);
                // copy the pole colour hue shift so the cage matches the pole's patina
                var barMat = new Material(metalMat);
                barMat.color = pole.GetComponent<Renderer>().material.color;
                bar.GetComponent<Renderer>().material = barMat;
            }
        }

        // Helper to add a small decorative finial (sphere) at the very top of the pole.
        private static void AddFinial(Transform parent, Transform pole, System.Random rng)
        {
            var metalMat = WorldStore.GetPorchMaterial();
            if (metalMat == null) return;
            // finial is a tiny sphere, slightly darker than the pole colour
            var fin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fin.name = "Finial";
            fin.transform.SetParent(parent, false);
            fin.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            // pole height = localScale.y, pole bottom offset = localPosition.y
            float poleHeight = pole.localScale.y;
            float poleBaseY = pole.localPosition.y;
            fin.transform.localPosition = new Vector3(0f, poleBaseY + poleHeight + 0.06f, 0f);
            var finMat = new Material(metalMat);
            // darken a little to suggest wrought‑iron patina
            finMat.color = pole.GetComponent<Renderer>().material.color * 0.85f;
            fin.GetComponent<Renderer>().material = finMat;
        }
    }
}
