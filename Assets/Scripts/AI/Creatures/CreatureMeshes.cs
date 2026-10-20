using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>The unit shapes creature bodies are assembled from (each one unit across and one unit tall).</summary>
    public enum CreaturePrim : byte { Sphere, Box, Cylinder, Taper, Cone }

    /// <summary>
    /// Bakes one rigid part of a creature (a skull, a thigh, a torso) from a handful of unit primitives into a single
    /// mesh with one submesh per colour, so a body is a dozen renderers rather than a hundred. The finished meshes are
    /// cached by key (<see cref="CreatureParts"/>), so every hound in the Zone shares the same thigh.
    /// </summary>
    public sealed class CreaturePartBuilder
    {
        private readonly List<Vector3> _verts = new List<Vector3>(512);
        private readonly List<Vector3> _norms = new List<Vector3>(512);
        private readonly List<Color> _colors = new List<Color>(4);
        private readonly List<float> _gloss = new List<float>(4);
        private readonly List<List<int>> _tris = new List<List<int>>(4);

        /// <summary>Adds a primitive: centred on <paramref name="pos"/>, rotated by <paramref name="euler"/>, stretched to
        /// <paramref name="size"/> (diameter / length on each axis).</summary>
        /// <summary>Adds a primitive shape to the builder's buffers, merging into a submesh.</summary>
        public CreaturePartBuilder Add(CreaturePrim prim, Vector3 pos, Vector3 euler, Vector3 size, Color color, float gloss = 0.15f)
        {
            UnitMesh u = UnitMesh.For(prim);
            int sub = SubmeshFor(color, gloss);
            Quaternion rot = Quaternion.Euler(euler);
            var inv = new Vector3(1f / Mathf.Max(1e-4f, size.x), 1f / Mathf.Max(1e-4f, size.y), 1f / Mathf.Max(1e-4f, size.z));
            int start = _verts.Count;
            for (int i = 0; i < u.Verts.Length; i++)
            {
                _verts.Add(pos + rot * Vector3.Scale(u.Verts[i], size));
                _norms.Add((rot * Vector3.Scale(u.Norms[i], inv)).normalized);
            }
            List<int> tris = _tris[sub];
            for (int i = 0; i < u.Tris.Length; i++) tris.Add(start + u.Tris[i]);
            return this;
        }

        private int SubmeshFor(Color c, float gloss)
        {
            for (int i = 0; i < _colors.Count; i++)
            {
                Color o = _colors[i];
                if (Mathf.Abs(o.r - c.r) < 0.01f && Mathf.Abs(o.g - c.g) < 0.01f && Mathf.Abs(o.b - c.b) < 0.01f
                    && Mathf.Abs(_gloss[i] - gloss) < 0.02f) return i;
            }
            _colors.Add(c);
            _gloss.Add(gloss);
            _tris.Add(new List<int>(256));
            return _colors.Count - 1;
        }

        /// <summary>Creates the mesh and corresponding materials for this part, caching submeshes by colour and gloss.</summary>
        public Mesh Build(string name, out Material[] materials)
        {
            var mesh = new Mesh { name = name };
            if (_verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_verts);
            mesh.SetNormals(_norms);
            mesh.subMeshCount = _tris.Count;
            for (int i = 0; i < _tris.Count; i++) mesh.SetTriangles(_tris[i], i, false);
            mesh.RecalculateBounds();
            materials = new Material[_colors.Count];
            for (int i = 0; i < _colors.Count; i++) materials[i] = CloneZUtil.Mat(_colors[i], _gloss[i]);
            return mesh;
        }

        /// <summary>The unit primitives, generated once (low-poly: these are small and usually far off).</summary>
        private sealed class UnitMesh
        {
            public Vector3[] Verts, Norms;
            public int[] Tris;

            private static UnitMesh s_Sphere, s_Box, s_Cyl, s_Taper, s_Cone;

            /// <summary>Gets (or creates) the cached unit mesh for the given primitive type.</summary>
        public static UnitMesh For(CreaturePrim p)
            {
                switch (p)
                {
                    case CreaturePrim.Sphere: return s_Sphere ??= Sphere(10, 7);
                    case CreaturePrim.Cylinder: return s_Cyl ??= Frustum(8, 0.5f, 0.5f);
                    case CreaturePrim.Taper: return s_Taper ??= Frustum(8, 0.5f, 0.3f);
                    case CreaturePrim.Cone: return s_Cone ??= Frustum(7, 0.04f, 0.5f);
                    default: return s_Box ??= Box();
                }
            }

            private static UnitMesh Sphere(int seg, int rings)
            {
                int n = (seg + 1) * (rings + 1);
                var v = new Vector3[n];
                var nn = new Vector3[n];
                for (int y = 0; y <= rings; y++)
                {
                    float pitch = (float)y / rings * Mathf.PI;
                    float sy = Mathf.Cos(pitch), r = Mathf.Sin(pitch);
                    for (int x = 0; x <= seg; x++)
                    {
                        float yaw = (float)x / seg * Mathf.PI * 2f;
                        var d = new Vector3(Mathf.Sin(yaw) * r, sy, Mathf.Cos(yaw) * r);
                        int i = y * (seg + 1) + x;
                        nn[i] = d;
                        v[i] = d * 0.5f;
                    }
                }
                var t = new int[seg * rings * 6];
                int k = 0;
                for (int y = 0; y < rings; y++)
                for (int x = 0; x < seg; x++)
                {
                    int a = y * (seg + 1) + x, b = a + seg + 1;
                    t[k++] = a; t[k++] = a + 1; t[k++] = b;
                    t[k++] = a + 1; t[k++] = b + 1; t[k++] = b;
                }
                return new UnitMesh { Verts = v, Norms = nn, Tris = t };
            }

            // A cylinder one unit tall (y -0.5..0.5) whose top and bottom radii may differ (a tapered limb, a cone).
            private static UnitMesh Frustum(int sides, float top, float bottom)
            {
                int wall = (sides + 1) * 2;
                int count = wall + (sides + 1) * 2 + 2;
                var v = new Vector3[count];
                var n = new Vector3[count];
                float slope = bottom - top;   // outward lean of the wall's normal
                for (int i = 0; i <= sides; i++)
                {
                    float yaw = (float)i / sides * Mathf.PI * 2f;
                    float sx = Mathf.Sin(yaw), cz = Mathf.Cos(yaw);
                    Vector3 side = new Vector3(sx, slope, cz).normalized;
                    v[i] = new Vector3(sx * top, 0.5f, cz * top);
                    n[i] = side;
                    v[sides + 1 + i] = new Vector3(sx * bottom, -0.5f, cz * bottom);
                    n[sides + 1 + i] = side;
                    v[wall + i] = v[i];
                    n[wall + i] = Vector3.up;
                    v[wall + sides + 1 + i] = v[sides + 1 + i];
                    n[wall + sides + 1 + i] = Vector3.down;
                }
                int tc = wall + (sides + 1) * 2, bc = tc + 1;
                v[tc] = new Vector3(0f, 0.5f, 0f); n[tc] = Vector3.up;
                v[bc] = new Vector3(0f, -0.5f, 0f); n[bc] = Vector3.down;

                var t = new int[sides * 12];
                int k = 0;
                for (int i = 0; i < sides; i++)
                {
                    int ta = i, tb = i + 1, ba = sides + 1 + i, bb = sides + 2 + i;
                    t[k++] = ta; t[k++] = tb; t[k++] = ba;
                    t[k++] = tb; t[k++] = bb; t[k++] = ba;
                    t[k++] = tc; t[k++] = wall + i + 1; t[k++] = wall + i;
                    t[k++] = bc; t[k++] = wall + sides + 1 + i; t[k++] = wall + sides + 2 + i;
                }
                return new UnitMesh { Verts = v, Norms = n, Tris = t };
            }

            private static UnitMesh Box()
            {
                Vector3[] faces = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
                var v = new Vector3[24];
                var n = new Vector3[24];
                var t = new int[36];
                for (int f = 0; f < 6; f++)
                {
                    Vector3 nf = faces[f];
                    var u = new Vector3(nf.y, nf.z, nf.x);
                    Vector3 w = Vector3.Cross(nf, u);
                    int b = f * 4;
                    v[b] = (nf - u - w) * 0.5f;
                    v[b + 1] = (nf + u - w) * 0.5f;
                    v[b + 2] = (nf + u + w) * 0.5f;
                    v[b + 3] = (nf - u + w) * 0.5f;
                    for (int i = 0; i < 4; i++) n[b + i] = nf;
                    int k = f * 6;
                    t[k] = b; t[k + 1] = b + 2; t[k + 2] = b + 1;
                    t[k + 3] = b; t[k + 4] = b + 3; t[k + 5] = b + 2;
                }
                return new UnitMesh { Verts = v, Norms = n, Tris = t };
            }
        }
    }

    /// <summary>Pivots and cached part meshes for the procedural creature bodies.</summary>
    public static class CreatureParts
    {
        private sealed class Entry
        {
            public Mesh Mesh;
            public Material[] Materials;
        }

        private static readonly Dictionary<string, Entry> s_Cache = new Dictionary<string, Entry>(64);

        /// <summary>An empty transform (a joint) under <paramref name="parent"/>.</summary>
        /// <summary>Creates a new GameObject, optionally attaching a MeshRenderer with a shared mesh and materials.</summary>
        public static Transform Pivot(string name, Transform parent, Vector3 localPos, Vector3 localEuler = default)
        {
            var go = new GameObject(name);
            go.layer = parent != null ? parent.gameObject.layer : CloneZConfig.IgnoreRaycastLayer;
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(localEuler);
            return t;
        }

        /// <summary>Hangs the shared mesh for <paramref name="key"/> (built once by <paramref name="build"/>) on a joint.</summary>
        /// <summary>Finds a mesh (or builds it once) for the given key and attaches it to a new GameObject under the joint.</summary>
        public static MeshRenderer Attach(Transform joint, string key, Action<CreaturePartBuilder> build)
        {
            if (!s_Cache.TryGetValue(key, out Entry e) || e.Mesh == null)
            {
                var b = new CreaturePartBuilder();
                build(b);
                e = new Entry();
                e.Mesh = b.Build(key, out e.Materials);
                s_Cache[key] = e;
            }
            var go = new GameObject(key);
            go.layer = joint.gameObject.layer;
            go.transform.SetParent(joint, false);
            go.AddComponent<MeshFilter>().sharedMesh = e.Mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = e.Materials;
            mr.shadowCastingMode = ShadowCastingMode.On;
            return mr;
        }
    }

    /// <summary>Per-instance see-through materials (a Shade's cloak, psi waves, slam dust). Callers own and destroy them.</summary>
    public static class CreatureMaterials
    {
        private static Shader s_Lit, s_Unlit;

        /// <summary>Creates a translucent lit material for effects, configuring smoothness and metallic.</summary>
        public static Material TransparentLit(Color color, float smoothness = 0.2f)
        {
            if (s_Lit == null) s_Lit = Shader.Find("Universal Render Pipeline/Lit");
            if (s_Lit == null) s_Lit = Shader.Find("Standard");
            var m = new Material(s_Lit) { name = "CreatureCloak" };
            MakeTransparent(m);
            Set(m, "_Smoothness", smoothness);
            Set(m, "_Metallic", 0f);
            SetColor(m, color);
            return m;
        }

        /// <summary>Creates a translucent unlit material for effects, setting its base colour.</summary>
        public static Material TransparentUnlit(Color color)
        {
            if (s_Unlit == null) s_Unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (s_Unlit == null) s_Unlit = Shader.Find("Unlit/Transparent");
            var m = new Material(s_Unlit) { name = "CreatureFx" };
            MakeTransparent(m);
            SetColor(m, color);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m == null) return;
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }

        public static Color ColorOf(Material m)
        {
            if (m == null) return Color.white;
            return m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
        }

        // The same render state URP's material inspector writes for Surface = Transparent, Blend = Alpha.
        private static void MakeTransparent(Material m)
        {
            Set(m, "_Surface", 1f);
            Set(m, "_Blend", 0f);
            Set(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
            Set(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            Set(m, "_SrcBlendAlpha", (float)BlendMode.One);
            Set(m, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            Set(m, "_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>Sets a float property on the material if it exists.</summary>
        private static void Set(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }
    }

    /// <summary>A short-lived expanding, fading shell (a Puppeteer's psi wave, the dust ring of a Thumper's slam).</summary>
    public sealed class CreatureRingFx : MonoBehaviour
    {
        private Material _mat;
        private Color _color;
        private Vector3 _from, _to;
        private float _t, _duration;

        /// <summary>Spawns a short-lived expanding/fading ring effect at the given centre.</summary>
        public static void Spawn(Vector3 center, Color color, Vector3 fromScale, Vector3 toScale, float duration)
        {
            var go = new GameObject("CreatureRingFx");
            go.layer = CloneZConfig.IgnoreRaycastLayer;
            go.transform.position = center;
            go.transform.localScale = fromScale;
            Mesh sphere = SphereMesh();
            go.AddComponent<MeshFilter>().sharedMesh = sphere;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var fx = go.AddComponent<CreatureRingFx>();
            fx._mat = CreatureMaterials.TransparentUnlit(color);
            mr.sharedMaterial = fx._mat;
            fx._color = color;
            fx._from = fromScale;
            fx._to = toScale;
            fx._duration = Mathf.Max(0.05f, duration);
        }

        private static Mesh s_Sphere;

        /// <summary>Gets the shared sphere mesh, creating it if necessary.</summary>
        private static Mesh SphereMesh()
        {
            if (s_Sphere != null) return s_Sphere;
            var b = new CreaturePartBuilder();
            b.Add(CreaturePrim.Sphere, Vector3.zero, Vector3.zero, Vector3.one, Color.white);
            s_Sphere = b.Build("CreatureFxSphere", out _);
            return s_Sphere;
        }

        /// <summary>Creates the ring's material and initializes the expanding/fading properties.</summary>
        private void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _duration);
            float ease = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.LerpUnclamped(_from, _to, ease);
            Color c = _color;
            c.a = _color.a * (1f - k);
            CreatureMaterials.SetColor(_mat, c);
            if (k >= 1f) Destroy(gameObject);
        }

        /// <summary>Cleans up the material when the ring is destroyed.</summary>
        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }
}
