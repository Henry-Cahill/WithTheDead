using UnityEngine;

namespace CloneZ
{
    /// <summary>Small helpers shared across the bootstrapper and gameplay.</summary>
    public static class CloneZUtil
    {
        private static Shader s_LitShader;
        private static Shader s_UnlitShader;

        private static Shader LitShader
        {
            get
            {
                if (s_LitShader == null)
                    s_LitShader = Shader.Find("Universal Render Pipeline/Lit");
                // Fall back to the legacy standard shader if URP isn't the active pipeline.
                if (s_LitShader == null) s_LitShader = Shader.Find("Standard");
                return s_LitShader;
            }
        }

        private static Shader UnlitShader
        {
            get
            {
                if (s_UnlitShader == null)
                    s_UnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (s_UnlitShader == null) s_UnlitShader = Shader.Find("Unlit/Color");
                return s_UnlitShader;
            }
        }

        /// <summary>Creates a URP-compatible material so runtime primitives don't render magenta.</summary>
        public static Material Mat(Color color, float smoothness = 0.1f, float metallic = 0f)
        {
            var m = new Material(LitShader) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }

        public static Material UnlitMat(Color color)
        {
            var m = new Material(UnlitShader) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            return m;
        }

        /// <summary>Creates a primitive with a URP material, no default collider fuss.</summary>
        public static GameObject Box(string name, Vector3 pos, Vector3 size, Color color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        /// <summary>Returns a flattened, normalized direction on the XZ plane.</summary>
        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.zero;
        }
    }
}
