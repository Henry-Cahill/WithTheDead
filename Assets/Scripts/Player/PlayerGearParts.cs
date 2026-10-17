using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Building blocks for <see cref="PlayerGearVisuals"/>: collider-free primitive parts, flat straps
    /// (single boxes, or one smooth ribbon mesh along a path), bands round a limb, and cached glowing
    /// lens materials. Every position is local to the given parent.
    /// </summary>
    internal static class PlayerGearParts
    {
        private static Mesh s_Cube, s_Cylinder, s_Sphere;
        private static readonly Dictionary<int, Material> s_Glow = new Dictionary<int, Material>();

        /// <summary>The built-in mesh of a primitive (cube, cylinder or sphere), borrowed once.</summary>
        public static Mesh MeshOf(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Cylinder: return s_Cylinder != null ? s_Cylinder : (s_Cylinder = Borrow(type));
                case PrimitiveType.Sphere: return s_Sphere != null ? s_Sphere : (s_Sphere = Borrow(type));
                default: return s_Cube != null ? s_Cube : (s_Cube = Borrow(PrimitiveType.Cube));
            }
        }

        // The primitive's shared mesh, taken from a throwaway primitive destroyed on the spot (so its
        // collider never takes part in a physics step).
        private static Mesh Borrow(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        /// <summary>A collider-free primitive (a unit cube, a unit-diameter sphere, or a cylinder one unit
        /// across and two tall) under <paramref name="parent"/>, on the parent's layer.</summary>
        public static Transform Part(Transform parent, string name, PrimitiveType shape, Vector3 position,
            Quaternion rotation, Vector3 scale, Material material, bool shadows = false)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = MeshOf(shape);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return t;
        }

        /// <summary>An axis-aligned box of the given size centred at <paramref name="centre"/>.</summary>
        public static Transform Box(Transform parent, string name, Vector3 centre, Vector3 size, Material material, bool shadows = false)
            => Part(parent, name, PrimitiveType.Cube, centre, Quaternion.identity, size, material, shadows);

        /// <summary>A rotated box of the given size centred at <paramref name="centre"/>.</summary>
        public static Transform Box(Transform parent, string name, Vector3 centre, Vector3 size, Quaternion rotation, Material material, bool shadows = false)
            => Part(parent, name, PrimitiveType.Cube, centre, rotation, size, material, shadows);

        /// <summary>A cylinder of the given length and radius centred at <paramref name="centre"/>, its
        /// axis along <paramref name="axis"/>. The part's local +Y points along the axis.</summary>
        public static Transform Tube(Transform parent, string name, Vector3 centre, Vector3 axis, float length, float radius,
            Material material, bool shadows = false)
        {
            Quaternion q = Quaternion.FromToRotation(Vector3.up, axis.normalized);
            return Part(parent, name, PrimitiveType.Cylinder, centre, q, new Vector3(radius * 2f, length * 0.5f, radius * 2f), material, shadows);
        }

        /// <summary>A flat band round a vertical axis (a belt, a leg strap, a head harness): a squat
        /// elliptic cylinder whose inside is hidden by whatever it wraps.</summary>
        public static Transform Band(Transform parent, string name, Vector3 centre, float radiusX, float radiusZ, float height,
            Quaternion tilt, Material material)
            => Part(parent, name, PrimitiveType.Cylinder, centre, tilt, new Vector3(radiusX * 2f, height * 0.5f, radiusZ * 2f), material);

        /// <summary>A flat strap from <paramref name="a"/> to <paramref name="b"/>, its thickness along
        /// <paramref name="outward"/> (which must not run along the strap).</summary>
        public static Transform Strap(Transform parent, Vector3 a, Vector3 b, float width, float thickness, Material material, Vector3 outward)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return null;
            Vector3 f = d / len;
            Vector3 up = outward - Vector3.Dot(outward, f) * f;
            if (up.sqrMagnitude < 1e-8f) up = Vector3.Cross(f, Vector3.right);
            Quaternion q = Quaternion.LookRotation(f, up);
            return Part(parent, "Strap", PrimitiveType.Cube, (a + b) * 0.5f, q, new Vector3(width, thickness, len), material);
        }

        /// <summary>One smooth strap following <paramref name="path"/> as a single mesh: a flat band
        /// <paramref name="width"/> across and <paramref name="thickness"/> deep, its across-direction
        /// kept square to the path and to <paramref name="side"/> (for a strap running back to front over a
        /// shoulder, <c>Vector3.right</c> keeps its outer face outward). Returns the generated mesh, which
        /// the caller owns and must destroy.</summary>
        public static Mesh Ribbon(Transform parent, string name, List<Vector3> path, Vector3 side, float width, float thickness, Material material)
        {
            int n = path.Count;
            if (n < 2) return null;

            // Per ring: the four faces' edge pairs — outer (outL, outR), right (outR, inR), inner (inR, inL),
            // left (inL, outL) — each with its own normal so the edges stay crisp; plus a cap at either end.
            var verts = new Vector3[n * 8 + 8];
            var norms = new Vector3[verts.Length];
            var tris = new int[(n - 1) * 24 + 12];
            int vi = 0, ti = 0;
            Vector3 startOutL = default, startOutR = default, startInR = default, startInL = default, startT = default;
            Vector3 endOutL = default, endOutR = default, endInR = default, endInL = default, endT = default;

            for (int i = 0; i < n; i++)
            {
                Vector3 t = (path[Mathf.Min(i + 1, n - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 nrm = Vector3.Cross(t, side).normalized;
                Vector3 s = Vector3.Cross(nrm, t).normalized;
                Vector3 hw = s * (width * 0.5f), ht = nrm * (thickness * 0.5f), p = path[i];
                Vector3 outL = p + ht - hw, outR = p + ht + hw, inR = p - ht + hw, inL = p - ht - hw;

                verts[vi] = outL; norms[vi++] = nrm;
                verts[vi] = outR; norms[vi++] = nrm;
                verts[vi] = outR; norms[vi++] = s;
                verts[vi] = inR; norms[vi++] = s;
                verts[vi] = inR; norms[vi++] = -nrm;
                verts[vi] = inL; norms[vi++] = -nrm;
                verts[vi] = inL; norms[vi++] = -s;
                verts[vi] = outL; norms[vi++] = -s;

                if (i == 0) { startOutL = outL; startOutR = outR; startInR = inR; startInL = inL; startT = t; }
                if (i == n - 1) { endOutL = outL; endOutR = outR; endInR = inR; endInL = inL; endT = t; }
            }

            // Each face between ring i and i+1: (a_i, b_i+1, b_i) and (a_i, a_i+1, b_i+1) face outward.
            for (int i = 0; i < n - 1; i++)
            {
                int b0 = i * 8, b1 = b0 + 8;
                for (int f = 0; f < 4; f++)
                {
                    int a = b0 + f * 2, b = a + 1, c = b1 + f * 2 + 1, d = b1 + f * 2;
                    tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                    tris[ti++] = a; tris[ti++] = d; tris[ti++] = c;
                }
            }

            // End caps: the start faces back along the path, the end forward.
            int sc = vi;
            verts[vi] = startOutL; verts[vi + 1] = startOutR; verts[vi + 2] = startInR; verts[vi + 3] = startInL;
            for (int k = 0; k < 4; k++) norms[vi + k] = -startT;
            vi += 4;
            tris[ti++] = sc; tris[ti++] = sc + 1; tris[ti++] = sc + 2;
            tris[ti++] = sc; tris[ti++] = sc + 2; tris[ti++] = sc + 3;

            int ec = vi;
            verts[vi] = endOutL; verts[vi + 1] = endOutR; verts[vi + 2] = endInR; verts[vi + 3] = endInL;
            for (int k = 0; k < 4; k++) norms[vi + k] = endT;
            tris[ti++] = ec; tris[ti++] = ec + 2; tris[ti++] = ec + 1;
            tris[ti++] = ec; tris[ti++] = ec + 3; tris[ti++] = ec + 2;

            var mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return mesh;
        }

        /// <summary>A shared unlit material in a glowing colour (a lit lens, a powered tube).</summary>
        public static Material Glow(Color color)
        {
            int key = (Mathf.RoundToInt(color.r * 255f) << 16) | (Mathf.RoundToInt(color.g * 255f) << 8) | Mathf.RoundToInt(color.b * 255f);
            if (s_Glow.TryGetValue(key, out Material m) && m != null) return m;
            m = CloneZUtil.UnlitMat(color);
            s_Glow[key] = m;
            return m;
        }
    }

    /// <summary>
    /// The survivor's torso surface for one <see cref="HumanoidProportions"/> build — the same profile the
    /// body mesh is lofted from (hips to neck: half width, half depth, forward offset and superellipse
    /// exponent) — so packs, rigs and straps can sit on the body rather than sink into it. Positions are
    /// in model space: a figure standing on y = 0, facing +Z.
    /// </summary>
    internal sealed class PlayerGearTorso
    {
        private readonly float[] _ty, _tw, _td, _tz, _te;

        public PlayerGearTorso(HumanoidProportions p)
        {
            _ty = new[] { 0.800f, 0.815f, 0.845f, 0.885f, 0.925f, 0.975f, 1.030f, 1.080f, 1.140f, 1.210f, 1.290f, 1.360f, 1.410f, 1.450f, 1.480f, 1.500f, 1.512f };
            _tw = new[] { 0f, 0.075f, 0.128f, p.HipW * 0.96f, p.HipW, p.HipW * 0.965f, Mathf.Lerp(p.HipW, p.WaistW, 0.65f), p.WaistW,
                          Mathf.Lerp(p.WaistW, p.ChestW, 0.5f), p.ChestW * 0.97f, p.ChestW, p.ChestW * 1.01f, p.ShoulderW * 1.02f, p.ShoulderW * 0.92f, p.ShoulderW * 0.62f, p.Neck + 0.02f, 0f };
            _td = new[] { 0f, 0.060f, 0.088f, p.HipD * 0.98f, p.HipD, p.HipD * 0.97f, Mathf.Lerp(p.HipD, p.WaistD, 0.6f), p.WaistD,
                          Mathf.Lerp(p.WaistD, p.ChestD, 0.5f), p.ChestD * 0.97f, p.ChestD, p.ChestD * 0.94f, p.ChestD * 0.82f, p.ChestD * 0.70f, p.ChestD * 0.55f, p.Neck + 0.01f, 0f };
            _tz = new[] { 0f, 0f, 0f, -0.006f, -0.010f, -0.004f, p.Belly * 0.6f, p.Belly, p.Belly * 0.5f + 0.008f, 0.016f, 0.020f, 0.014f, 0f, -0.008f, -0.012f, -0.014f, -0.014f };
            _te = new[] { 2f, 2.2f, 2.4f, 2.6f, 2.6f, 2.7f, 2.8f, 2.8f, 2.9f, 3.0f, 3.0f, 3.1f, 3.3f, 3.0f, 2.6f, 2.2f, 2f };
        }

        /// <summary>The torso cross-section at height <paramref name="y"/>: half width, half depth, forward
        /// offset of its centre and the superellipse exponent of its outline.</summary>
        public void Sample(float y, out float w, out float d, out float z, out float e)
        {
            int last = _ty.Length - 1;
            y = Mathf.Clamp(y, _ty[0], _ty[last]);
            int i = 0;
            while (i < last - 1 && y > _ty[i + 1]) i++;
            w = Mathf.Max(0f, Profile(_tw, i, y));
            d = Mathf.Max(0f, Profile(_td, i, y));
            z = Profile(_tz, i, y);
            e = Mathf.Lerp(_te[i], _te[i + 1], (y - _ty[i]) / (_ty[i + 1] - _ty[i]));
        }

        /// <summary>The front (or back) surface z of the torso at height y and sideways offset x, grown by
        /// <paramref name="inflate"/> (the clothes worn over it).</summary>
        public float Surface(float y, float x, float inflate, bool front)
        {
            Sample(y, out float w, out float d, out float z, out float e);
            if (w < 1e-4f || d < 1e-4f) return z;
            w += inflate;
            d += inflate;
            float ax = Mathf.Clamp01(Mathf.Abs(x) / w);
            float k = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(ax, e)), 1f / e);
            return z + (front ? d : -d) * k;
        }

        /// <summary>The height where the torso's outline closes over at sideways offset x — the top of the
        /// shoulder line a strap at that offset crosses.</summary>
        public float TopAt(float x, float inflate)
        {
            float ax = Mathf.Abs(x);
            for (float y = 1.30f; y < 1.512f; y += 0.002f)
            {
                Sample(y, out float w, out _, out _, out _);
                if (w < 1e-4f || w + inflate <= ax) return y;
            }
            return 1.512f;
        }

        /// <summary>Fills <paramref name="path"/> with a strap's centre line at sideways offset x: up the back
        /// from <paramref name="backY"/>, over the shoulder, and down the chest to <paramref name="frontY"/>,
        /// held <paramref name="lift"/> off the (clothed) surface — a little more over the top of the
        /// shoulder, where the outline turns fastest and the points crowd together.</summary>
        public void ShoulderPath(List<Vector3> path, float x, float inflate, float backY, float frontY, float lift)
        {
            const float topExtra = 0.008f, topBand = 0.04f;
            path.Clear();
            float top = TopAt(x, inflate);
            const int steps = 7;
            for (int i = 0; i < steps; i++)
            {
                float u = 1f - (float)i / steps;
                float y = top - (top - backY) * u * u;
                float l = lift + topExtra * Mathf.Clamp01(1f - (top - y) / topBand);
                path.Add(new Vector3(x, y, Surface(y, x, inflate, false) - l));
            }
            Sample(top - 0.004f, out _, out _, out float zc, out _);
            path.Add(new Vector3(x, top + lift + topExtra, zc));
            for (int i = steps - 1; i >= 0; i--)
            {
                float u = 1f - (float)i / steps;
                float y = top - (top - frontY) * u * u;
                float l = lift + topExtra * Mathf.Clamp01(1f - (top - y) / topBand);
                path.Add(new Vector3(x, y, Surface(y, x, inflate, true) + l));
            }
        }

        // Cubic Hermite through the keys, so the profile curves smoothly from hips to waist to chest.
        private float Profile(float[] vs, int i, float y)
        {
            float[] ys = _ty;
            int n = ys.Length;
            float h = ys[i + 1] - ys[i];
            float u = (y - ys[i]) / h;
            float m0 = i > 0 ? (vs[i + 1] - vs[i - 1]) / (ys[i + 1] - ys[i - 1]) : (vs[i + 1] - vs[i]) / h;
            float m1 = i + 2 < n ? (vs[i + 2] - vs[i]) / (ys[i + 2] - ys[i]) : (vs[i + 1] - vs[i]) / h;
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * vs[i] + (u3 - 2f * u2 + u) * h * m0
                 + (-2f * u3 + 3f * u2) * vs[i + 1] + (u3 - u2) * h * m1;
        }
    }
}
