using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// How a piece of raid-map geometry is skinned: a material plus the way its UVs are generated.
    /// Palette surfaces point every vertex at one texel of <see cref="DecorPalette"/> (so hundreds of
    /// flat colours share a single material); planar surfaces tile a procedural texture in world
    /// metres; fitted surfaces take explicit UVs from the primitive (posters, rugs, signs).
    /// </summary>
    internal readonly struct Surf
    {
        public readonly Material Mat;
        /// <summary>The fixed UV (palette texel) used when the surface is not planar.</summary>
        public readonly Vector2 Uv;
        /// <summary>Texture repeats per metre for planar mapping; 0 = fixed / explicit UVs.</summary>
        public readonly float PerMeter;

        public Surf(Material mat, Vector2 uv, float perMeter)
        {
            Mat = mat;
            Uv = uv;
            PerMeter = perMeter;
        }

        public bool Planar => PerMeter > 0f;
        public bool Valid => Mat != null;
    }

    /// <summary>
    /// Accumulates the boxes, quads, cylinders and blobs the raid map is built from into one mesh
    /// with a sub-mesh per material, so a whole building's furniture (or a wall segment with its
    /// wainscot, trims and vines) is a single renderer instead of hundreds of GameObjects. Primitives
    /// are given in a movable local frame (<see cref="SetFrame"/>) so furniture can be authored at the
    /// origin facing +Z and dropped anywhere at any yaw.
    /// </summary>
    internal sealed class RaidMesh
    {
        private readonly List<Vector3> _v = new List<Vector3>(512);
        private readonly List<Vector3> _n = new List<Vector3>(512);
        private readonly List<Vector2> _uv = new List<Vector2>(512);
        private readonly List<Material> _mats = new List<Material>(8);
        private readonly List<List<int>> _tris = new List<List<int>>(8);

        private Vector3 _origin;
        private Quaternion _rot = Quaternion.identity;

        /// <summary>Gets the number of vertices currently stored.</summary>
        public int VertexCount => _v.Count;
        /// <summary>True when no vertices have been added.</summary>
        public bool IsEmpty => _v.Count == 0;

        #region Frame
        /// <summary>Places the local frame: later primitives are given relative to
        /// <paramref name="origin"/>, rotated by <paramref name="yaw"/> degrees about Y.</summary>
        public void SetFrame(Vector3 origin, float yaw)
        {
            _origin = origin;
            _rot = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>Sets the local frame with explicit rotation quaternion.</summary>
        public void SetFrame(Vector3 origin, Quaternion rotation)
        {
            _origin = origin;
            _rot = rotation;
        }

        /// <summary>Resets the local frame to the origin with no rotation.</summary>
        public void ResetFrame()
        {
            _origin = Vector3.zero;
            _rot = Quaternion.identity;
        }

        /// <summary>Gets the current local frame origin.</summary>
        public Vector3 FrameOrigin => _origin;
/// <summary>Gets the current local frame rotation.</summary>
        public Quaternion FrameRotation => _rot;

        /// <summary>A local-frame point in mesh space.</summary>
        public Vector3 ToMeshSpace(Vector3 local) => _origin + _rot * local;
        #endregion

        /// <summary>Removes all accumulated geometry and resets the local frame.</summary>
        public void Clear()
        {
            _v.Clear();
            _n.Clear();
            _uv.Clear();
            _mats.Clear();
            _tris.Clear();
            ResetFrame();
        }

        private List<int> TrisFor(Material m)
        {
            for (int i = 0; i < _mats.Count; i++)
                if (_mats[i] == m) return _tris[i];
            _mats.Add(m);
            var list = new List<int>(96);
            _tris.Add(list);
            return list;
        }

        /// <summary>Added to every position before planar UVs are worked out. A streamed map builds each piece around its
        /// own middle; this puts the piece's middle back where it is on the world grid (wrapped every
        /// <see cref="PlanarPeriod"/> metres), so textures run on unbroken across the pieces' seams. Zero otherwise.</summary>
        internal static Vector3 PlanarOrigin;

        /// <summary>A whole number of repeats of every planar surface (9, 6, 3, 2.8, 2.4, 2, 1.5, 1.2, 1, 0.8, 0.6 and
        /// 0.4 m), so wrapping <see cref="PlanarOrigin"/> by it never shows.</summary>
        internal const double PlanarPeriod = 252.0;

        private Vector2 UvAt(Vector3 p, Vector3 n, in Surf s)
        {
            if (!s.Planar) return s.Uv;
            float k = s.PerMeter;
            p += PlanarOrigin;
            if (n.y > 0.7f || n.y < -0.7f) return new Vector2(p.x * k, p.z * k);
            Vector3 t = Vector3.Cross(n, Vector3.up);
            float len = t.magnitude;
            if (len < 1e-4f) return new Vector2(p.x * k, p.z * k);
            t /= len;
            return new Vector2(Vector3.Dot(p, t) * k, p.y * k);
        }

        private int AddVert(Vector3 p, Vector3 n, in Surf s)
        {
            _v.Add(p);
            _n.Add(n);
            _uv.Add(UvAt(p, n, s));
            return _v.Count - 1;
        }

        private int AddVert(Vector3 p, Vector3 n, Vector2 uv)
        {
            _v.Add(p);
            _n.Add(n);
            _uv.Add(uv);
            return _v.Count - 1;
        }

        #region Boxes
        private static readonly Vector3[] s_FaceN =
        {
            Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left,
        };

        /// <summary>An axis-aligned (in the local frame) box.</summary>
        public void Box(Vector3 center, Vector3 size, in Surf s) => Box(center, size, Quaternion.identity, s);

        /// <summary>A box rotated about Y by <paramref name="yaw"/> degrees within the local frame.</summary>
        public void Box(Vector3 center, Vector3 size, float yaw, in Surf s) => Box(center, size, Quaternion.Euler(0f, yaw, 0f), s);

        /// <summary>A box with a full local rotation (tilted planks, fallen chairs, leaning arms).</summary>
        public void Box(Vector3 center, Vector3 size, Quaternion localRot, in Surf s, bool skipBottom = false)
        {
            if (!s.Valid) return;
            Quaternion rot = _rot * localRot;
            Vector3 c = _origin + _rot * center;
            List<int> tris = TrisFor(s.Mat);

            for (int f = 0; f < 6; f++)
            {
                if (skipBottom && f == 1) continue;
                Vector3 n = s_FaceN[f];
                Vector3 u = new Vector3(n.y, n.z, n.x);
                Vector3 v = Vector3.Cross(n, u);
                Vector3 wn = rot * n;

                Vector3 c0 = Vector3.Scale((n - u - v) * 0.5f, size);
                Vector3 c1 = Vector3.Scale((n + u - v) * 0.5f, size);
                Vector3 c2 = Vector3.Scale((n + u + v) * 0.5f, size);
                Vector3 c3 = Vector3.Scale((n - u + v) * 0.5f, size);

                int b = AddVert(c + rot * c0, wn, s);
                AddVert(c + rot * c1, wn, s);
                AddVert(c + rot * c2, wn, s);
                AddVert(c + rot * c3, wn, s);

                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }
        }

        /// <summary>A box resting on y = <paramref name="y0"/> (convenience for furniture parts).</summary>
        public void BoxOn(float x, float y0, float z, float w, float h, float d, in Surf s)
            => Box(new Vector3(x, y0 + h * 0.5f, z), new Vector3(w, h, d), s);
        #endregion

        #region Quads and flat shapes
        /// <summary>A single-sided quad. <paramref name="halfRight"/> / <paramref name="halfUp"/> are the
        /// half extents as seen by a viewer in front of it; the face points toward that viewer.</summary>
        public void Quad(Vector3 center, Vector3 halfRight, Vector3 halfUp, in Surf s)
        {
            if (!s.Valid) return;
            Vector3 c = _origin + _rot * center;
            Vector3 r = _rot * halfRight;
            Vector3 u = _rot * halfUp;
            Vector3 n = Vector3.Cross(u, r).normalized;
            List<int> tris = TrisFor(s.Mat);
            int b = AddVert(c - r - u, n, s);
            AddVert(c - r + u, n, s);
            AddVert(c + r + u, n, s);
            AddVert(c + r - u, n, s);
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
        }

        /// <summary>A single-sided quad with explicit UVs: <paramref name="uv"/> maps bottom-left to
        /// top-right as seen from the front (used for posters, rugs, signs and papers).</summary>
        public void QuadUV(Vector3 center, Vector3 halfRight, Vector3 halfUp, in Surf s, Rect uv)
        {
            if (!s.Valid) return;
            Vector3 c = _origin + _rot * center;
            Vector3 r = _rot * halfRight;
            Vector3 u = _rot * halfUp;
            Vector3 n = Vector3.Cross(u, r).normalized;
            List<int> tris = TrisFor(s.Mat);
            int b = AddVert(c - r - u, n, new Vector2(uv.xMin, uv.yMin));
            AddVert(c - r + u, n, new Vector2(uv.xMin, uv.yMax));
            AddVert(c + r + u, n, new Vector2(uv.xMax, uv.yMax));
            AddVert(c + r - u, n, new Vector2(uv.xMax, uv.yMin));
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
        }

        /// <summary>An upward-facing rectangle at height <paramref name="y"/>, rotated by
        /// <paramref name="yaw"/> within the local frame (floors, decals, papers).</summary>
        public void Flat(Vector3 center, float width, float depth, float yaw, in Surf s)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Quad(center, q * new Vector3(width * 0.5f, 0f, 0f), q * new Vector3(0f, 0f, depth * 0.5f), s);
        }

        /// <summary>An upward-facing rectangle with explicit UVs.</summary>
        public void FlatUV(Vector3 center, float width, float depth, float yaw, in Surf s, Rect uv)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            QuadUV(center, q * new Vector3(width * 0.5f, 0f, 0f), q * new Vector3(0f, 0f, depth * 0.5f), s, uv);
        }

        /// <summary>An upright rectangle facing along <paramref name="normal"/> (horizontal), e.g. a poster on
        /// a wall or stock on a shelf front; <paramref name="roll"/> tilts it about the normal (degrees).
        /// Explicit UVs when <paramref name="uv"/> is given, otherwise the surface's own mapping.</summary>
        public void Upright(Vector3 center, Vector3 normal, float width, float height, in Surf s, Rect? uv = null, float roll = 0f)
        {
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
            Vector3 up = Vector3.up;
            if (roll != 0f)
            {
                Quaternion r = Quaternion.AngleAxis(roll, normal);
                right = r * right;
                up = r * up;
            }
            if (uv.HasValue) QuadUV(center, right * (width * 0.5f), up * (height * 0.5f), s, uv.Value);
            else Quad(center, right * (width * 0.5f), up * (height * 0.5f), s);
        }

        /// <summary>A single-sided triangle; its face shows when a, b, c run clockwise as seen.</summary>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, in Surf s)
        {
            if (!s.Valid) return;
            Vector3 wa = _origin + _rot * a, wb = _origin + _rot * b, wc = _origin + _rot * c;
            Vector3 n = Vector3.Cross(wb - wa, wc - wa).normalized;
            List<int> tris = TrisFor(s.Mat);
            int i0 = AddVert(wa, n, s);
            AddVert(wb, n, s);
            AddVert(wc, n, s);
            tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
        }

        /// <summary>A rhombus (leaf, shard) in the plane spanned by <paramref name="right"/> and
        /// <paramref name="up"/>; its four tips are center ± right and center ± up.</summary>
        public void Diamond(Vector3 center, Vector3 right, Vector3 up, in Surf s)
        {
            if (!s.Valid) return;
            Vector3 c = _origin + _rot * center;
            Vector3 r = _rot * right;
            Vector3 u = _rot * up;
            Vector3 n = Vector3.Cross(u, r).normalized;
            List<int> tris = TrisFor(s.Mat);
            int top = AddVert(c + u, n, s);
            int rt = AddVert(c + r, n, s);
            int bot = AddVert(c - u, n, s);
            int lf = AddVert(c - r, n, s);
            tris.Add(top); tris.Add(rt); tris.Add(bot);
            tris.Add(top); tris.Add(bot); tris.Add(lf);
        }

        /// <summary>An irregular flat polygon (stain, patch, puddle) in the plane spanned by
        /// <paramref name="right"/> / <paramref name="up"/> (unit vectors), facing the viewer.</summary>
        public void Blob(Vector3 center, Vector3 right, Vector3 up, float radiusX, float radiusY, int points,
            float jag, System.Random rng, in Surf s)
        {
            if (!s.Valid) return;
            points = Mathf.Clamp(points, 5, 24);
            Vector3 c = _origin + _rot * center;
            Vector3 r = _rot * right;
            Vector3 u = _rot * up;
            Vector3 n = Vector3.Cross(u, r).normalized;
            List<int> tris = TrisFor(s.Mat);
            int ci = AddVert(c, n, s);
            int first = _v.Count;
            for (int i = 0; i < points; i++)
            {
                float a = i * Mathf.PI * 2f / points;
                float k = 1f - jag * (float)rng.NextDouble();
                AddVert(c + r * (Mathf.Sin(a) * radiusX * k) + u * (Mathf.Cos(a) * radiusY * k), n, s);
            }
            for (int i = 0; i < points; i++)
            {
                tris.Add(ci);
                tris.Add(first + i);
                tris.Add(first + (i + 1) % points);
            }
        }

        /// <summary>An upward-facing irregular blob lying at height <paramref name="y"/>.</summary>
        public void FlatBlob(Vector3 center, float radiusX, float radiusZ, float yaw, float jag, System.Random rng, in Surf s)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Blob(center, q * Vector3.right, q * Vector3.forward, radiusX, radiusZ, 9, jag, rng, s);
        }
        #endregion

        #region Round shapes
        /// <summary>A capped prism standing on <paramref name="baseCenter"/> along the local up axis
        /// (rotated by <paramref name="localRot"/>): barrels, posts, stools, tyres, cans.
        /// <paramref name="topScale"/> &lt; 1 tapers it (tree trunks, cones).</summary>
        public void Cylinder(Vector3 baseCenter, float radius, float height, int sides, in Surf s,
            Quaternion localRot, float topScale = 1f, bool caps = true)
        {
            if (!s.Valid) return;
            sides = Mathf.Clamp(sides, 3, 24);
            Quaternion rot = _rot * localRot;
            Vector3 b0 = _origin + _rot * baseCenter;
            Vector3 up = rot * Vector3.up;
            Vector3 top = b0 + up * height;
            float rTop = radius * topScale;
            List<int> tris = TrisFor(s.Mat);

            // Side wall: a ring of vertices per end, so caps stay flat-shaded.
            int wall = _v.Count;
            float slope = (radius - rTop) / Mathf.Max(0.001f, height);
            for (int i = 0; i <= sides; i++)
            {
                float yaw = (float)i / sides * Mathf.PI * 2f;
                Vector3 dir = rot * new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
                Vector3 nrm = (dir + up * slope).normalized;
                AddVert(top + dir * rTop, nrm, s);
                AddVert(b0 + dir * radius, nrm, s);
            }
            for (int i = 0; i < sides; i++)
            {
                int tA = wall + i * 2, bA = tA + 1, tB = tA + 2, bB = tA + 3;
                tris.Add(tA); tris.Add(bA); tris.Add(tB);
                tris.Add(tB); tris.Add(bA); tris.Add(bB);
            }

            if (!caps) return;
            if (rTop > 0.001f)
            {
                int tc = AddVert(top, up, s);
                int ring = _v.Count;
                for (int i = 0; i <= sides; i++)
                {
                    float yaw = (float)i / sides * Mathf.PI * 2f;
                    AddVert(top + rot * new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)) * rTop, up, s);
                }
                for (int i = 0; i < sides; i++) { tris.Add(tc); tris.Add(ring + i); tris.Add(ring + i + 1); }
            }
            int bc = AddVert(b0, -up, s);
            int bring = _v.Count;
            for (int i = 0; i <= sides; i++)
            {
                float yaw = (float)i / sides * Mathf.PI * 2f;
                AddVert(b0 + rot * new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)) * radius, -up, s);
            }
            for (int i = 0; i < sides; i++) { tris.Add(bc); tris.Add(bring + i + 1); tris.Add(bring + i); }
        }

        /// <summary>An upright cylinder (no extra rotation).</summary>
        public void Cylinder(Vector3 baseCenter, float radius, float height, int sides, in Surf s)
            => Cylinder(baseCenter, radius, height, sides, s, Quaternion.identity);

        /// <summary>A cylinder lying along the local X axis, centred on <paramref name="center"/>
        /// (tyres, cans and bottles on their side, logs).</summary>
        public void CylinderX(Vector3 center, float radius, float length, int sides, in Surf s, float yaw = 0f)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, 90f);
            Vector3 axis = q * Vector3.up;
            Cylinder(center - axis * (length * 0.5f), radius, length, sides, s, q);
        }

        /// <summary>A low-poly ellipsoid of the given diameters (snow piles, sandbags, trash bags,
        /// bushes, lamp globes).</summary>
        public void Ellipsoid(Vector3 center, Vector3 size, Quaternion localRot, in Surf s, int segments = 8, int rings = 5)
        {
            if (!s.Valid) return;
            Quaternion rot = _rot * localRot;
            Vector3 c = _origin + _rot * center;
            List<int> tris = TrisFor(s.Mat);
            int start = _v.Count;
            Vector3 inv = new Vector3(1f / Mathf.Max(0.001f, size.x), 1f / Mathf.Max(0.001f, size.y), 1f / Mathf.Max(0.001f, size.z));
            for (int y = 0; y <= rings; y++)
            {
                float pitch = (float)y / rings * Mathf.PI;
                float sy = Mathf.Cos(pitch);
                float r = Mathf.Sin(pitch);
                for (int x = 0; x <= segments; x++)
                {
                    float yaw = (float)x / segments * Mathf.PI * 2f;
                    var unit = new Vector3(Mathf.Sin(yaw) * r, sy, Mathf.Cos(yaw) * r);
                    Vector3 p = Vector3.Scale(unit * 0.5f, size);
                    Vector3 n = rot * Vector3.Scale(unit, inv).normalized;
                    AddVert(c + rot * p, n, s);
                }
            }
            for (int y = 0; y < rings; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int a = start + y * (segments + 1) + x;
                    int b = a + segments + 1;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            }
        }

        /// <summary>Creates an ellipsoid with default orientation (no rotation).</summary>
        public void Ellipsoid(Vector3 center, Vector3 size, in Surf s, int segments = 8, int rings = 5)
            => Ellipsoid(center, size, Quaternion.identity, s, segments, rings);

        /// <summary>Creates a perfect sphere at <paramref name="center"/> with given <paramref name="radius"/>.
        /// Uses the existing Ellipsoid implementation with equal axes.
        /// </summary>
        public void Sphere(Vector3 center, float radius, in Surf s) => Ellipsoid(center, new Vector3(radius, radius, radius), s);

        /// <summary>A thin straight rod between two local points (branches, pipes, wires, lamp arms).</summary>
        public void Rod(Vector3 a, Vector3 b, float thickness, in Surf s, int sides = 0)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            Quaternion q = Quaternion.FromToRotation(Vector3.up, d / len);
            if (sides >= 3) Cylinder(a, thickness * 0.5f, len, sides, s, q, 1f, false);
            else Box((a + b) * 0.5f, new Vector3(thickness, len, thickness), q, s);
        }
        #endregion

        #region Output
        /// <summary>While set, every mesh baked here is added to it, so whoever builds a streamed map piece can
        /// destroy the piece's meshes with it (runtime meshes outlive their GameObjects).</summary>
        internal static List<Mesh> CreatedMeshes;

        /// <summary>Running totals over the process (the map harness reads them around a build): meshes baked, their
        /// vertices and the time it took, and the objects made to show them (<see cref="Emit"/>) with their time.</summary>
        internal static long BakedMeshes, BakedVertices, BakeTicks, Emits, EmitTicks;

        /// <summary>Bakes the accumulated geometry into a mesh, with vertices made relative to
        /// <paramref name="origin"/> (mesh space). Returns null when empty.</summary>
        public Mesh ToMesh(string name, Vector3 origin)
        {
            if (_v.Count == 0) return null;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var mesh = new Mesh { name = name };
            CreatedMeshes?.Add(mesh);
            if (_v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;

            if (origin != Vector3.zero)
            {
                var verts = new List<Vector3>(_v.Count);
                for (int i = 0; i < _v.Count; i++) verts.Add(_v[i] - origin);
                mesh.SetVertices(verts);
            }
            else mesh.SetVertices(_v);

            mesh.SetNormals(_n);
            mesh.SetUVs(0, _uv);
            mesh.subMeshCount = _mats.Count;
            for (int i = 0; i < _tris.Count; i++) mesh.SetTriangles(_tris[i], i, false);
            mesh.RecalculateBounds();
            BakedMeshes++;
            BakedVertices += _v.Count;
            BakeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            return mesh;
        }

        /// <summary>The array of materials used by the generated mesh, in sub‑mesh order.</summary>
        public Material[] Materials => _mats.ToArray();

        /// <summary>Gives <paramref name="go"/> this geometry (mesh space = the object's local space,
        /// assuming an unscaled, unrotated object at <paramref name="origin"/>).</summary>
        public void ApplyTo(GameObject go, Vector3 origin, ShadowCastingMode shadows = ShadowCastingMode.On)
        {
            Mesh mesh = ToMesh(go.name, origin);
            if (mesh == null) return;
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null) mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = Materials;
            mr.shadowCastingMode = shadows;
            mr.receiveShadows = true;
        }

        /// <summary>Creates a GameObject at <paramref name="origin"/> under <paramref name="parent"/>
        /// carrying this geometry. Returns null when nothing was drawn.</summary>
        public GameObject Emit(string name, Transform parent, Vector3 origin, ShadowCastingMode shadows = ShadowCastingMode.On)
        {
            if (_v.Count == 0) return null;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            ApplyTo(go, origin, shadows);
            Emits++;
            EmitTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            return go;
        }

        // One vertex as the meshes MeshBake makes store it: position, normal and UV interleaved.
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct PackedVertex
        {
            public Vector3 Position, Normal;
            public Vector2 Uv;
        }

        private static readonly VertexAttributeDescriptor[] s_Layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        internal const MeshUpdateFlags QuietUpdate = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices
            | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds;

        /// <summary>Copies the geometry into <paramref name="d"/> (any thread), vertices relative to
        /// <paramref name="origin"/>, 16-bit indices where they fit; <paramref name="bounds"/> is what it covers.</summary>
        internal void PackInto(Mesh.MeshData d, Vector3 origin, out Bounds bounds)
        {
            int vc = _v.Count;
            d.SetVertexBufferParams(vc, s_Layout);
            Unity.Collections.NativeArray<PackedVertex> vs = d.GetVertexData<PackedVertex>(0);
            float x0 = float.MaxValue, y0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue, z1 = float.MinValue;
            for (int i = 0; i < vc; i++)
            {
                Vector3 p = _v[i];
                p.x -= origin.x;
                p.y -= origin.y;
                p.z -= origin.z;
                if (p.x < x0) x0 = p.x;
                if (p.x > x1) x1 = p.x;
                if (p.y < y0) y0 = p.y;
                if (p.y > y1) y1 = p.y;
                if (p.z < z0) z0 = p.z;
                if (p.z > z1) z1 = p.z;
                vs[i] = new PackedVertex { Position = p, Normal = _n[i], Uv = _uv[i] };
            }
            bounds = vc > 0 ? new Bounds(new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0))
                : new Bounds();
            int ic = 0;
            for (int s = 0; s < _tris.Count; s++) ic += _tris[s].Count;
            bool wide = vc > 65535;
            d.SetIndexBufferParams(ic, wide ? IndexFormat.UInt32 : IndexFormat.UInt16);
            int k = 0;
            if (wide)
            {
                Unity.Collections.NativeArray<int> ix = d.GetIndexData<int>();
                for (int s = 0; s < _tris.Count; s++)
                {
                    List<int> t = _tris[s];
                    for (int j = 0; j < t.Count; j++) ix[k++] = t[j];
                }
            }
            else
            {
                Unity.Collections.NativeArray<ushort> ix = d.GetIndexData<ushort>();
                for (int s = 0; s < _tris.Count; s++)
                {
                    List<int> t = _tris[s];
                    for (int j = 0; j < t.Count; j++) ix[k++] = (ushort)t[j];
                }
            }
            d.subMeshCount = _tris.Count;
            int start = 0;
            for (int s = 0; s < _tris.Count; s++)
            {
                int n = _tris[s].Count;
                d.SetSubMesh(s, new SubMeshDescriptor(start, n) { bounds = bounds, firstVertex = 0, vertexCount = vc }, QuietUpdate);
                start += n;
            }
        }
        #endregion
    }

    /// <summary>
    /// Bakes many <see cref="RaidMesh"/>es into their meshes in one go through Unity's MeshData API: queue them with
    /// <see cref="Add"/>, then <see cref="Allocate"/> and <see cref="Apply"/> on the main thread, with <see cref="Pack"/>
    /// (the heavy part: copying the geometry into the meshes' buffers) in between on any thread. The queued RaidMeshes
    /// mustn't change until it's applied.
    /// </summary>
    internal sealed class MeshBake
    {
        private readonly List<RaidMesh> _sources = new List<RaidMesh>();
        private readonly List<Vector3> _origins = new List<Vector3>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private Bounds[] _bounds;
        private Mesh.MeshDataArray _data;
        private bool _allocated;

        public int Count => _sources.Count;

        /// <summary>The meshes queued, in order (empty until applied).</summary>
        public List<Mesh> Meshes => _meshes;

        /// <summary>Queues <paramref name="source"/>'s geometry for <paramref name="mesh"/>, its vertices relative to
        /// <paramref name="origin"/>.</summary>
        public void Add(RaidMesh source, Vector3 origin, Mesh mesh)
        {
            _sources.Add(source);
            _origins.Add(origin);
            _meshes.Add(mesh);
        }

        public void Allocate()
        {
            _data = Mesh.AllocateWritableMeshData(_sources.Count);
            _bounds = new Bounds[_sources.Count];
            _allocated = true;
        }

        public void Pack()
        {
            for (int i = 0; i < _sources.Count; i++) _sources[i].PackInto(_data[i], _origins[i], out _bounds[i]);
        }

        public void Apply()
        {
            Mesh.ApplyAndDisposeWritableMeshData(_data, _meshes, RaidMesh.QuietUpdate);
            _allocated = false;
            for (int i = 0; i < _meshes.Count; i++) _meshes[i].bounds = _bounds[i];
        }

        /// <summary>Lets go of the buffers when it won't be applied after all.</summary>
        public void Dispose()
        {
            if (_allocated) _data.Dispose();
            _allocated = false;
        }
    }
}
