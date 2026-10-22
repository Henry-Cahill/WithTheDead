using UnityEngine;
using CloneZ;

namespace CloneZ
{
    internal static class StreetLampBuilder
    {
        // Builds a single lamp post on the given street segment.
        // The lamp sits on the sidewalk side, near the road edge.
        internal static void BuildLamp(KentuckyMap.Site site, KentuckyMap.Street street, MapBuild build)
        {
            // choose the midpoint of the street polyline
            var pts = street.Pts;
            if (pts.Count < 2) return;
            Vector2 a = pts[0];
            Vector2 b = pts[pts.Count - 1];
            Vector2 mid = (a + b) * 0.5f;
            // offset outward by half the sidewalk width + small gap
            float offset = street.Data.SidewalkWidth() * 0.5f + 0.2f;
            // street direction vector (ignore vertical y)
            Vector2 dir = (b - a).normalized;
            // perpendicular to the right (assuming road runs forward)
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector3 pos = new Vector3(mid.x + perp.x * offset, 0f, mid.y + perp.y * offset);
            // reserve space (radius ~0.3m)
            if (!site.Free(new Vector2(pos.x, pos.z), 0.3f)) return;
            site.Take(new Vector2(pos.x, pos.z), 0.3f);
            // create a parent GameObject under the outside town layer
            GameObject lampRoot = new GameObject("StreetLamp");
            lampRoot.transform.position = pos;
            lampRoot.transform.SetParent(site.Town.World, false);
            // ---- pole ----
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "Pole";
            pole.transform.SetParent(lampRoot.transform, false);
            pole.transform.localScale = new Vector3(0.1f, 2.5f, 0.1f);
            pole.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            var mat = build.GetStreetLampMaterial();
            if (mat != null) pole.GetComponent<Renderer>().material = mat;
            // add a simple prop collider
            var col = pole.AddComponent<BoxCollider>();
            col.size = pole.transform.localScale;
            site.Town.PropCollider(col.center, col.size, 0f);
            // ---- lamp head ----
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(lampRoot.transform, false);
            head.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
            head.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            if (mat != null) head.GetComponent<Renderer>().material = mat;
            // ---- point light ----
            var light = lampRoot.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 2f;
            light.color = new Color(1f, 0.9f, 0.7f); // warm amber
            light.transform.localPosition = new Vector3(0f, 2.55f, 0f);
        }
    }
}
