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
            // attach metadata that tells the flicker script whether this lamp is on a primary road
            var meta = lampRoot.AddComponent<StreetLampMetadata>();
            meta.IsPrimaryRoad = street.Data.width >= 6f; // 6 m+ considered a primary road
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
            // add period‑accurate lamp arm with support brackets
            AddLampArm(lampRoot.transform, pole.transform, rng, street.Data.kind);
            var rootCol = lampRoot.AddComponent<SphereCollider>();
            rootCol.radius = 0.4f; // increased to include arm
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
            // ---- winter ice decoration -------------------------------------------------
            int lampHash = lampRoot.transform.position.GetHashCode();
            int icicleCount = 1 + (lampHash & 0x3); // 1‑4 icicles, varies per lamp
            GameObject iceCluster = new GameObject("IceCluster");
            iceCluster.transform.SetParent(lampRoot.transform, false);
            for (int i = 0; i < icicleCount; i++)
            {
                GameObject icicle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                icicle.name = "Icicle_" + i;
                icicle.transform.SetParent(iceCluster.transform, false);
                float yPos = UnityEngine.Random.Range(1.8f, 2.3f);
                icicle.transform.localPosition = new Vector3(0f, yPos, 0f);
                icicle.transform.localRotation = Quaternion.Euler(
                    UnityEngine.Random.Range(-10f, 10f),
                    UnityEngine.Random.Range(0f, 360f),
                    0f);
                icicle.transform.localScale = new Vector3(0.02f, 0.12f, 0.02f);
                var rend = icicle.GetComponent<Renderer>();
                // Cached and URP-safe; tinting the lamp's shared `mat` here would recolour every later lamp.
                rend.sharedMaterial = CloneZUtil.Mat(new Color(0.85f, 0.85f, 0.90f), 0.55f);
                var col = icicle.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
            }
            // NOTE: the IceCluster is a visual only decoration; it does not affect gameplay
            // other than the optional light‑dimming performed in StreetLampFlicker.
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

        // Helper to add a period‑accurate lamp arm and support brackets.
        private static void AddLampArm(Transform parent, Transform pole, System.Random rng, string streetKind)
        {
            // Use the porch metal material for the arm and brackets.
            var metalMat = WorldStore.GetPorchMaterial();
            if (metalMat == null) return;

            // Determine lengths based on street kind.
            float horizLen = streetKind == "residential" ? 0.4f : 0.7f; // metres
            float vertHeight = 0.5f; // metres above pole top
            float thickness = 0.04f; // metres

            // Root for the arm group.
            var armRoot = new GameObject("LampArm");
            armRoot.transform.SetParent(parent, false);

            // Vertical support bar attached near the top of the pole.
            var vert = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vert.name = "ArmVertical";
            vert.transform.SetParent(armRoot.transform, false);
            vert.transform.localScale = new Vector3(thickness, vertHeight, thickness);

            // Position: bottom sits on top of the pole.
            float poleTopY = pole.localPosition.y + pole.localScale.y;
            vert.transform.localPosition = new Vector3(0f, poleTopY + vertHeight * 0.5f, 0f);
            var vertMat = new Material(metalMat);
            vertMat.color = pole.GetComponent<Renderer>().material.color;
            vert.GetComponent<Renderer>().material = vertMat;

            // Horizontal arm extending outward.
            var horiz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            horiz.name = "ArmHorizontal";
            horiz.transform.SetParent(armRoot.transform, false);
            horiz.transform.localScale = new Vector3(horizLen, thickness, thickness);
            // Small random yaw jitter for hand‑painted feel.
            float yawJitter = (float)(rng.NextDouble() * 6.0 - 3.0); // ±3°
            horiz.transform.localRotation = Quaternion.Euler(0f, yawJitter, 0f);
            // Position: attached to top of vertical bar.
            horiz.transform.localPosition = new Vector3(horizLen * 0.5f, poleTopY + vertHeight, 0f);
            var horizMat = new Material(metalMat);
            horizMat.color = pole.GetComponent<Renderer>().material.color;
            horiz.GetComponent<Renderer>().material = horizMat;

            // Simple colliders for interaction.
            vert.AddComponent<BoxCollider>();
            horiz.AddComponent<BoxCollider>();

            // Slight random vertical offset for variation.
            float yOffsetJitter = (float)(rng.NextDouble() * 0.04 - 0.02);
            armRoot.transform.localPosition += new Vector3(0f, yOffsetJitter, 0f);
        }

            private static void AddFinial(Transform parent, Transform pole, System.Random rng)
        {
            var metalMat = WorldStore.GetPorchMaterial();
            if (metalMat == null) return;
            var fin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fin.name = "Finial";
            fin.transform.SetParent(parent, false);
            fin.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            float poleHeight = pole.localScale.y;
            float poleBaseY = pole.localPosition.y;
            fin.transform.localPosition = new Vector3(0f, poleBaseY + poleHeight + 0.06f, 0f);
            var finMat = new Material(metalMat);
            finMat.color = pole.GetComponent<Renderer>().material.color * 0.85f;
            fin.GetComponent<Renderer>().material = finMat;
        }
        }

}
