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
    /// <remarks>
    /// Built for speed, as the streamed map draws millions of vertices while the survivor travels: the geometry lives in
    /// plain arrays (rented from a shared pool and given back by <see cref="Release"/>), a box turns its eight corners
    /// rather than its 24 vertices, a prism its ring once rather than three times, round shapes take their sines from
    /// tables, and meshes are baked straight from the arrays (<see cref="ToMesh"/>) or many at once off the main thread
    /// (<see cref="MeshBake"/>). Every point is still turned by Unity's own <c>Quaternion * Vector3</c>, fed exactly the
    /// values it always was, so the vertices come out bit for bit as they always have (the map harness checks this
    /// against a copy of the old code). Not thread-safe: one thread draws into a mesh at a time.
    /// </remarks>
    internal sealed class RaidMesh
    {
        private static readonly Vector3[] s_No3 = new Vector3[0];
        private static readonly Vector2[] s_No2 = new Vector2[0];

        private Vector3[] _v = s_No3, _n = s_No3;
        private Vector2[] _uv = s_No2;
        private int _count;
        private Material[] _mats = new Material[4];
        private int[][] _tris = new int[4][];
        private int[] _triCount = new int[4];
        private int _matCount, _lastSub = -1;

        private Vector3 _origin;
        private Quaternion _rot = Quaternion.identity;

        /// <summary>Gets the number of vertices currently stored.</summary>
        public int VertexCount => _count;
        /// <summary>True when no vertices have been added.</summary>
        public bool IsEmpty => _count == 0;

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

        #region Storage
        /// <summary>Removes all accumulated geometry and resets the local frame (the buffers are kept for reuse).</summary>
        public void Clear()
        {
            _count = 0;
            for (int i = 0; i < _matCount; i++)
            {
                _mats[i] = null;
                _triCount[i] = 0;
            }
            _matCount = 0;
            _lastSub = -1;
            ResetFrame();
        }

        /// <summary>Empties the mesh and gives its buffers back to the shared pool (call it once a mesh is baked and won't
        /// be drawn into again; drawing into it afterwards just rents new ones).</summary>
        public void Release()
        {
            Pool.Give(_v);
            Pool.Give(_n);
            Pool.Give(_uv);
            _v = _n = s_No3;
            _uv = s_No2;
            for (int i = 0; i < _tris.Length; i++)
            {
                if (_tris[i] != null) Pool.Give(_tris[i]);
                _tris[i] = null;
            }
            Clear();
        }

        // Room for <paramref name="more"/> vertices.
        private void Reserve(int more)
        {
            int need = _count + more;
            if (need <= _v.Length) return;
            int size = Pool.SizeFor(need);
            Vector3[] v = Pool.Vec3(size), n = Pool.Vec3(size);
            Vector2[] uv = Pool.Vec2(size);
            if (_count > 0)
            {
                System.Array.Copy(_v, v, _count);
                System.Array.Copy(_n, n, _count);
                System.Array.Copy(_uv, uv, _count);
            }
            Pool.Give(_v);
            Pool.Give(_n);
            Pool.Give(_uv);
            _v = v;
            _n = n;
            _uv = uv;
        }

        // The sub-mesh for a material, with room for <paramref name="more"/> indices.
        private int Sub(Material m, int more)
        {
            int s = _lastSub;
            if (s < 0 || _mats[s] != m)
            {
                s = -1;
                for (int i = 0; i < _matCount; i++)
                    if (_mats[i] == m) { s = i; break; }
                if (s < 0)
                {
                    if (_matCount == _mats.Length)
                    {
                        System.Array.Resize(ref _mats, _matCount * 2);
                        System.Array.Resize(ref _tris, _matCount * 2);
                        System.Array.Resize(ref _triCount, _matCount * 2);
                    }
                    s = _matCount++;
                    _mats[s] = m;
                    _triCount[s] = 0;
                }
                _lastSub = s;
            }
            int[] t = _tris[s];
            int need = _triCount[s] + more;
            if (t == null || need > t.Length)
            {
                int[] grown = Pool.Ints(Pool.SizeFor(System.Math.Max(need, 96)));
                if (t != null)
                {
                    System.Array.Copy(t, grown, _triCount[s]);
                    Pool.Give(t);
                }
                _tris[s] = grown;
            }
            return s;
        }

        private void Tri3(int s, int a, int b, int c)
        {
            int[] t = _tris[s];
            int k = _triCount[s];
            t[k] = a;
            t[k + 1] = b;
            t[k + 2] = c;
            _triCount[s] = k + 3;
        }

        // Two triangles over four vertices from <paramref name="b"/>: b, b+1, b+2 and b, b+2, b+3.
        private void Quad4(int s, int b)
        {
            int[] t = _tris[s];
            int k = _triCount[s];
            t[k] = b; t[k + 1] = b + 1; t[k + 2] = b + 2;
            t[k + 3] = b; t[k + 4] = b + 2; t[k + 5] = b + 3;
            _triCount[s] = k + 6;
        }

        /// <summary>
        /// The shared buffer pool: arrays in power-of-two sizes, handed back by <see cref="Release"/> (and by a mesh that
        /// outgrows its arrays) so a streamed map's thousands of batches don't each leave their arrays to the garbage
        /// collector. Thread-safe; it keeps at most a few dozen MB, and never the very biggest arrays.
        /// </summary>
        private static class Pool
        {
            private const int MinShift = 6, MaxShift = 20;   // 64 .. 1M elements are kept
            private const int KeepPerSize = 48;
            private const long KeepBytes = 40L << 20;         // all the arrays kept, together
            private static readonly Stack<Vector3[]>[] s_Vec3 = Buckets<Vector3[]>();
            private static readonly Stack<Vector2[]>[] s_Vec2 = Buckets<Vector2[]>();
            private static readonly Stack<int[]>[] s_Ints = Buckets<int[]>();
            private static readonly object s_Lock = new object();
            private static long s_Bytes;

            private static Stack<T>[] Buckets<T>()
            {
                var b = new Stack<T>[MaxShift + 1];
                for (int i = 0; i < b.Length; i++) b[i] = new Stack<T>();
                return b;
            }

            public static int SizeFor(int need)
            {
                int size = 1 << MinShift;
                while (size < need) size <<= 1;
                return size;
            }

            private static int Shift(int length)
            {
                if (length < (1 << MinShift) || (length & (length - 1)) != 0) return -1;
                int s = 0;
                while ((1 << s) < length) s++;
                return s <= MaxShift ? s : -1;
            }

            private static T Take<T>(Stack<T>[] buckets, int size, int elementBytes, System.Func<int, T> make) where T : class
            {
                int s = Shift(size);
                if (s >= 0)
                    lock (s_Lock)
                        if (buckets[s].Count > 0)
                        {
                            s_Bytes -= (long)size * elementBytes;
                            return buckets[s].Pop();
                        }
                return make(size);
            }

            private static void Put<T>(Stack<T>[] buckets, T array, int length, int elementBytes) where T : class
            {
                int s = Shift(length);
                if (s < 0) return;
                long bytes = (long)length * elementBytes;
                lock (s_Lock)
                {
                    if (buckets[s].Count >= KeepPerSize || s_Bytes + bytes > KeepBytes) return;
                    buckets[s].Push(array);
                    s_Bytes += bytes;
                }
            }

            public static Vector3[] Vec3(int size) => Take(s_Vec3, size, 12, n => new Vector3[n]);
            public static Vector2[] Vec2(int size) => Take(s_Vec2, size, 8, n => new Vector2[n]);
            public static int[] Ints(int size) => Take(s_Ints, size, 4, n => new int[n]);
            public static void Give(Vector3[] a) { if (a != null && a.Length > 0) Put(s_Vec3, a, a.Length, 12); }
            public static void Give(Vector2[] a) { if (a != null && a.Length > 0) Put(s_Vec2, a, a.Length, 8); }
            public static void Give(int[] a) { if (a != null && a.Length > 0) Put(s_Ints, a, a.Length, 4); }

            /// <summary>What the pool holds now, in bytes.</summary>
            public static long Bytes
            {
                get { lock (s_Lock) return s_Bytes; }
            }
        }

        /// <summary>The bytes the shared buffer pool holds (for the map harness).</summary>
        internal static long PooledBytes => Pool.Bytes;
        #endregion

        #region Vertices and UVs
        /// <summary>Added to every position before planar UVs are worked out. A streamed map builds each piece around its
        /// own middle; this puts the piece's middle back where it is on the world grid (wrapped every
        /// <see cref="PlanarPeriod"/> metres), so textures run on unbroken across the pieces' seams. Zero otherwise.</summary>
        internal static Vector3 PlanarOrigin;

        /// <summary>A whole number of repeats of every planar surface (9, 6, 3, 2.8, 2.4, 2, 1.5, 1.2, 1, 0.8, 0.6 and
        /// 0.4 m), so wrapping <see cref="PlanarOrigin"/> by it never shows.</summary>
        internal const double PlanarPeriod = 252.0;

        // How one face's vertices get their UVs: the surface's fixed texel, or planar from the position (on the ground
        // plane for a face looking up or down, else along the face).
        private readonly struct UvRule
        {
            public readonly bool Planar, Flat;
            public readonly float K;
            public readonly Vector3 T;
            public readonly Vector2 Fixed;

            public UvRule(Vector3 n, in Surf s)
            {
                Fixed = s.Uv;
                Planar = s.Planar;
                K = s.PerMeter;
                T = default;
                Flat = true;
                if (!Planar) return;
                if (n.y > 0.7f || n.y < -0.7f) return;
                Vector3 t = Vector3.Cross(n, Vector3.up);
                float len = t.magnitude;
                if (len < 1e-4f) return;
                T = t / len;
                Flat = false;
            }

            public Vector2 At(Vector3 p)
            {
                if (!Planar) return Fixed;
                p += PlanarOrigin;
                return Flat ? new Vector2(p.x * K, p.z * K) : new Vector2(Vector3.Dot(p, T) * K, p.y * K);
            }
        }

        private int Put(Vector3 p, Vector3 n, in UvRule rule)
        {
            int i = _count++;
            _v[i] = p;
            _n[i] = n;
            _uv[i] = rule.At(p);
            return i;
        }

        private int Put(Vector3 p, Vector3 n, Vector2 uv)
        {
            int i = _count++;
            _v[i] = p;
            _n[i] = n;
            _uv[i] = uv;
            return i;
        }
        #endregion

        #region Boxes
        // The four corners of each face of a box (bit 1 = +x, 2 = +y, 4 = +z, in the box's own frame) in the order the
        // faces have always been drawn: up, down, forward, back, right, left.
        private static readonly int[] s_FaceCorners = { 6, 7, 3, 2, 5, 4, 0, 1, 5, 7, 6, 4, 3, 1, 0, 2, 3, 7, 5, 1, 6, 2, 0, 4 };
        private static readonly Vector3[] s_FaceN = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

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
            // Each face's corners are the box's corners (the middle plus the half-size, signed, turned), so the eight are
            // turned once; the face normals are the six axes turned.
            float hx = 0.5f * size.x, hy = 0.5f * size.y, hz = 0.5f * size.z;
            Vector3[] corners = t_Corners ??= new Vector3[8];
            for (int k = 0; k < 8; k++)
                corners[k] = c + rot * new Vector3((k & 1) != 0 ? hx : -hx, (k & 2) != 0 ? hy : -hy, (k & 4) != 0 ? hz : -hz);
            int faces = skipBottom ? 5 : 6;
            Reserve(faces * 4);
            int sub = Sub(s.Mat, faces * 6);
            bool planar = s.Planar;
            for (int f = 0; f < 6; f++)
            {
                if (skipBottom && f == 1) continue;
                Vector3 n = rot * s_FaceN[f];
                int b = _count, at = f * 4;
                if (planar)
                {
                    var rule = new UvRule(n, s);
                    for (int k = 0; k < 4; k++) Put(corners[s_FaceCorners[at + k]], n, rule);
                }
                else
                {
                    Vector2 uv = s.Uv;
                    for (int k = 0; k < 4; k++)
                    {
                        int i = _count++;
                        _v[i] = corners[s_FaceCorners[at + k]];
                        _n[i] = n;
                        _uv[i] = uv;
                    }
                }
                Quad4(sub, b);
            }
        }

        [System.ThreadStatic] private static Vector3[] t_Corners;

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
            var rule = new UvRule(n, s);
            Reserve(4);
            int sub = Sub(s.Mat, 6);
            int b = Put(c - r - u, n, rule);
            Put(c - r + u, n, rule);
            Put(c + r + u, n, rule);
            Put(c + r - u, n, rule);
            Quad4(sub, b);
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
            Reserve(4);
            int sub = Sub(s.Mat, 6);
            int b = Put(c - r - u, n, new Vector2(uv.xMin, uv.yMin));
            Put(c - r + u, n, new Vector2(uv.xMin, uv.yMax));
            Put(c + r + u, n, new Vector2(uv.xMax, uv.yMax));
            Put(c + r - u, n, new Vector2(uv.xMax, uv.yMin));
            Quad4(sub, b);
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
            var rule = new UvRule(n, s);
            Reserve(3);
            int sub = Sub(s.Mat, 3);
            int i0 = Put(wa, n, rule);
            Put(wb, n, rule);
            Put(wc, n, rule);
            Tri3(sub, i0, i0 + 1, i0 + 2);
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
            var rule = new UvRule(n, s);
            Reserve(4);
            int sub = Sub(s.Mat, 6);
            int top = Put(c + u, n, rule);
            int rt = Put(c + r, n, rule);
            int bot = Put(c - u, n, rule);
            int lf = Put(c - r, n, rule);
            Tri3(sub, top, rt, bot);
            Tri3(sub, top, bot, lf);
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
            var rule = new UvRule(n, s);
            Reserve(points + 1);
            int sub = Sub(s.Mat, points * 3);
            int ci = Put(c, n, rule);
            int first = _count;
            for (int i = 0; i < points; i++)
            {
                float a = i * Mathf.PI * 2f / points;
                float k = 1f - jag * (float)rng.NextDouble();
                Put(c + r * (Mathf.Sin(a) * radiusX * k) + u * (Mathf.Cos(a) * radiusY * k), n, rule);
            }
            for (int i = 0; i < points; i++) Tri3(sub, ci, first + i, first + (i + 1) % points);
        }

        /// <summary>An upward-facing irregular blob lying at height <paramref name="y"/>.</summary>
        public void FlatBlob(Vector3 center, float radiusX, float radiusZ, float yaw, float jag, System.Random rng, in Surf s)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Blob(center, q * Vector3.right, q * Vector3.forward, radiusX, radiusZ, 9, jag, rng, s);
        }
        #endregion

        #region Round shapes
        // The unit directions round a prism's axis, kept between a wall and its caps (at most 24 sides, and one to close).
        [System.ThreadStatic] private static Vector3[] t_Ring;
        // The unit circle in 3 to 24 steps, and unit spheres by segments and rings: worked out once (their sines and
        // cosines are most of a round shape's cost).
        private static readonly Vector3[][] s_Circles = MakeCircles();
        private static readonly Dictionary<long, Vector3[]> s_Spheres = new Dictionary<long, Vector3[]>();

        private static Vector3[][] MakeCircles()
        {
            var all = new Vector3[25][];
            for (int sides = 3; sides <= 24; sides++)
            {
                var c = new Vector3[sides + 1];
                for (int i = 0; i <= sides; i++)
                {
                    float yaw = (float)i / sides * Mathf.PI * 2f;
                    c[i] = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
                }
                all[sides] = c;
            }
            return all;
        }

        private static Vector3[] Circle(int sides) => s_Circles[sides];

        private static Vector3[] UnitSphere(int segments, int rings)
        {
            long key = ((long)segments << 32) | (uint)rings;
            lock (s_Spheres)
                if (s_Spheres.TryGetValue(key, out Vector3[] have)) return have;
            var units = new Vector3[(rings + 1) * (segments + 1)];
            for (int y = 0; y <= rings; y++)
            {
                float pitch = (float)y / rings * Mathf.PI;
                float sy = Mathf.Cos(pitch);
                float r = Mathf.Sin(pitch);
                for (int x = 0; x <= segments; x++)
                {
                    float yaw = (float)x / segments * Mathf.PI * 2f;
                    units[y * (segments + 1) + x] = new Vector3(Mathf.Sin(yaw) * r, sy, Mathf.Cos(yaw) * r);
                }
            }
            lock (s_Spheres) s_Spheres[key] = units;
            return units;
        }

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
            Vector3[] ring = t_Ring ??= new Vector3[25];
            Vector3[] circle = Circle(sides);
            for (int i = 0; i <= sides; i++) ring[i] = rot * circle[i];
            bool topCap = caps && rTop > 0.001f;
            Reserve((sides + 1) * 2 + (caps ? (topCap ? 2 : 1) * (sides + 2) : 0));
            int sub = Sub(s.Mat, sides * 6 + (caps ? (topCap ? 2 : 1) * sides * 3 : 0));

            // Side wall: a ring of vertices per end, so caps stay flat-shaded.
            int wall = _count;
            float slope = (radius - rTop) / Mathf.Max(0.001f, height);
            for (int i = 0; i <= sides; i++)
            {
                Vector3 dir = ring[i];
                Vector3 nrm = (dir + up * slope).normalized;
                var rule = new UvRule(nrm, s);
                Put(top + dir * rTop, nrm, rule);
                Put(b0 + dir * radius, nrm, rule);
            }
            for (int i = 0; i < sides; i++)
            {
                int tA = wall + i * 2, bA = tA + 1, tB = tA + 2, bB = tA + 3;
                Tri3(sub, tA, bA, tB);
                Tri3(sub, tB, bA, bB);
            }

            if (!caps) return;
            if (topCap)
            {
                var upRule = new UvRule(up, s);
                int tc = Put(top, up, upRule);
                int first = _count;
                for (int i = 0; i <= sides; i++) Put(top + ring[i] * rTop, up, upRule);
                for (int i = 0; i < sides; i++) Tri3(sub, tc, first + i, first + i + 1);
            }
            Vector3 down = -up;
            var downRule = new UvRule(down, s);
            int bc = Put(b0, down, downRule);
            int bring = _count;
            for (int i = 0; i <= sides; i++) Put(b0 + ring[i] * radius, down, downRule);
            for (int i = 0; i < sides; i++) Tri3(sub, bc, bring + i + 1, bring + i);
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
            // None would divide the sphere by zero (NaN vertices).
            if (segments < 1) segments = 1;
            if (rings < 1) rings = 1;
            Quaternion rot = _rot * localRot;
            Vector3 c = _origin + _rot * center;
            Reserve((rings + 1) * (segments + 1));
            int sub = Sub(s.Mat, rings * segments * 6);
            int start = _count;
            Vector3 inv = new Vector3(1f / Mathf.Max(0.001f, size.x), 1f / Mathf.Max(0.001f, size.y), 1f / Mathf.Max(0.001f, size.z));
            Vector3[] units = UnitSphere(segments, rings);
            for (int i = 0; i < units.Length; i++)
            {
                Vector3 unit = units[i];
                Vector3 p = Vector3.Scale(unit * 0.5f, size);
                Vector3 n = rot * Vector3.Scale(unit, inv).normalized;
                Put(c + rot * p, n, new UvRule(n, s));
            }
            for (int y = 0; y < rings; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int a = start + y * (segments + 1) + x;
                    int b = a + segments + 1;
                    Tri3(sub, a, b, a + 1);
                    Tri3(sub, a + 1, b, b + 1);
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

        // One vertex as the meshes made here store it: position, normal and UV interleaved in one stream.
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

        // Scratch for packing a mesh's vertices and indices (one per thread: MeshBake packs on workers).
        [System.ThreadStatic] private static PackedVertex[] t_Vertices;
        [System.ThreadStatic] private static ushort[] t_Short;
        [System.ThreadStatic] private static int[] t_Int;

        /// <summary>How many indices (three a triangle) the mesh has.</summary>
        public int IndexCount
        {
            get
            {
                int n = 0;
                for (int s = 0; s < _matCount; s++) n += _triCount[s];
                return n;
            }
        }

        // The vertices made relative to <paramref name="origin"/> into the thread's scratch, and what they cover. Vertices with a
        // position, normal or UV that isn't a finite number (a NaN from a degenerate shape upstream) are made harmless: a
        // bad normal points up, a bad UV is zero, a bad position takes the last good one's place and its triangles are
        // folded flat (see PackIndices); <paramref name="bad"/> counts them (0 nearly always).
        private PackedVertex[] PackVertices(Vector3 origin, out Bounds bounds, out int bad)
        {
            PackedVertex[] vs = t_Vertices;
            if (vs == null || vs.Length < _count) t_Vertices = vs = new PackedVertex[Pool.SizeFor(_count)];
            float x0 = float.MaxValue, y0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue, z1 = float.MinValue;
            bad = 0;
            bool[] badAt = null;
            Vector3 lastGood = Vector3.zero;
            for (int i = 0; i < _count; i++)
            {
                Vector3 p = _v[i], n = _n[i];
                Vector2 uv = _uv[i];
                // x - x is 0 for every finite x, NaN for NaN and either infinity.
                if ((p.x - p.x) + (p.y - p.y) + (p.z - p.z) + (n.x - n.x) + (n.y - n.y) + (n.z - n.z) + (uv.x - uv.x) + (uv.y - uv.y) != 0f)
                {
                    bad++;
                    if ((n.x - n.x) + (n.y - n.y) + (n.z - n.z) != 0f) n = Vector3.up;
                    if ((uv.x - uv.x) + (uv.y - uv.y) != 0f) uv = Vector2.zero;
                    if ((p.x - p.x) + (p.y - p.y) + (p.z - p.z) != 0f)
                    {
                        if (badAt == null)
                        {
                            badAt = t_Bad;
                            if (badAt == null || badAt.Length < _count) t_Bad = badAt = new bool[Pool.SizeFor(_count)];
                            System.Array.Clear(badAt, 0, _count);
                        }
                        badAt[i] = true;
                        vs[i].Position = lastGood;
                        vs[i].Normal = n;
                        vs[i].Uv = uv;
                        continue;
                    }
                }
                p.x -= origin.x;
                p.y -= origin.y;
                p.z -= origin.z;
                if (p.x < x0) x0 = p.x;
                if (p.x > x1) x1 = p.x;
                if (p.y < y0) y0 = p.y;
                if (p.y > y1) y1 = p.y;
                if (p.z < z0) z0 = p.z;
                if (p.z > z1) z1 = p.z;
                vs[i].Position = p;
                vs[i].Normal = n;
                vs[i].Uv = uv;
                lastGood = p;
            }
            t_HasBad = badAt != null;
            bounds = x0 <= x1 ? new Bounds(new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0))
                : new Bounds();
            return vs;
        }

        // Which vertices of the mesh being packed on this thread had no finite position (valid while t_HasBad).
        [System.ThreadStatic] private static bool[] t_Bad;
        [System.ThreadStatic] private static bool t_HasBad;

        // Every sub-mesh's indices one after another into the thread's 16-bit scratch (or 32-bit when the vertices don't fit);
        // a triangle touching a vertex PackVertices found no position for is folded onto one corner (no area, nothing drawn).
        private System.Array PackIndices(int indexCount, bool wide)
        {
            bool[] bad = t_HasBad ? t_Bad : null;
            if (wide)
            {
                int[] ix = t_Int;
                if (ix == null || ix.Length < indexCount) t_Int = ix = new int[Pool.SizeFor(indexCount)];
                int k = 0;
                for (int s = 0; s < _matCount; s++)
                {
                    System.Array.Copy(_tris[s], 0, ix, k, _triCount[s]);
                    k += _triCount[s];
                }
                if (bad != null) Fold(ix, indexCount, bad);
                return ix;
            }
            ushort[] sx = t_Short;
            if (sx == null || sx.Length < indexCount) t_Short = sx = new ushort[Pool.SizeFor(indexCount)];
            int j = 0;
            for (int s = 0; s < _matCount; s++)
            {
                int[] t = _tris[s];
                int n = _triCount[s];
                if (bad == null)
                    for (int i = 0; i < n; i++) sx[j++] = (ushort)t[i];
                else
                    for (int i = 0; i + 2 < n; i += 3)
                    {
                        int a = t[i], b = t[i + 1], c = t[i + 2];
                        if (bad[a] || bad[b] || bad[c]) b = c = a;
                        sx[j++] = (ushort)a;
                        sx[j++] = (ushort)b;
                        sx[j++] = (ushort)c;
                    }
            }
            return sx;
        }

        private static void Fold(int[] ix, int count, bool[] bad)
        {
            for (int i = 0; i + 2 < count; i += 3)
                if (bad[ix[i]] || bad[ix[i + 1]] || bad[ix[i + 2]]) ix[i + 1] = ix[i + 2] = ix[i];
        }

        #region Non-finite geometry
        /// <summary>The meshes that came here with vertices whose position, normal or UV wasn't a finite number, as
        /// "hierarchy path (n of m vertices)" (the map harness reports them as errors; the game logs the first few). They
        /// were baked harmless (see <see cref="PackVertices"/>), so the NavMesh bake doesn't skip them.</summary>
        internal static readonly List<string> NonFinite = new List<string>();

        private const int NonFiniteKept = 200, NonFiniteLogged = 12;
        private static int s_NonFiniteLogged;

        internal static void ReportNonFinite(string where, int bad, int vertices)
        {
            string line = where + " (" + bad + " of " + vertices + " vertices)";
            lock (NonFinite)
                if (NonFinite.Count < NonFiniteKept) NonFinite.Add(line);
            if (s_NonFiniteLogged++ < NonFiniteLogged)
                Debug.LogWarning("[CloneZ] A map mesh had vertices that aren't finite numbers (made harmless): " + line);
        }

        /// <summary>Where <paramref name="t"/> stands in the hierarchy ("root/.../t").</summary>
        internal static string PathOf(Transform t)
        {
            if (t == null) return "(none)";
            var sb = new System.Text.StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }
        #endregion

        /// <summary>Bakes the accumulated geometry into a mesh, with vertices made relative to
        /// <paramref name="origin"/> (mesh space). Returns null when empty.</summary>
        public Mesh ToMesh(string name, Vector3 origin) => Bake(name, origin, null);

        // As ToMesh; a bad vertex is reported against <paramref name="owner"/>'s place in the hierarchy (else the name).
        private Mesh Bake(string name, Vector3 origin, Transform owner)
        {
            if (_count == 0) return null;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var mesh = new Mesh { name = name };
            CreatedMeshes?.Add(mesh);
            PackedVertex[] vs = PackVertices(origin, out Bounds bounds, out int bad);
            mesh.SetVertexBufferParams(_count, s_Layout);
            mesh.SetVertexBufferData(vs, 0, 0, _count, 0, QuietUpdate);
            int ic = IndexCount;
            bool wide = _count > 65535;
            mesh.SetIndexBufferParams(ic, wide ? IndexFormat.UInt32 : IndexFormat.UInt16);
            if (wide) mesh.SetIndexBufferData((int[])PackIndices(ic, true), 0, 0, ic, QuietUpdate);
            else mesh.SetIndexBufferData((ushort[])PackIndices(ic, false), 0, 0, ic, QuietUpdate);
            SubMeshes(mesh, bounds);
            mesh.bounds = bounds;
            if (bad > 0) ReportNonFinite(owner != null ? PathOf(owner) : name, bad, _count);
            BakedMeshes++;
            BakedVertices += _count;
            BakeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            return mesh;
        }

        private void SubMeshes(Mesh mesh, Bounds bounds)
        {
            mesh.subMeshCount = _matCount;
            int start = 0;
            for (int s = 0; s < _matCount; s++)
            {
                int n = _triCount[s];
                mesh.SetSubMesh(s, new SubMeshDescriptor(start, n) { bounds = bounds, firstVertex = 0, vertexCount = _count }, QuietUpdate);
                start += n;
            }
        }

        /// <summary>The array of materials used by the generated mesh, in sub‑mesh order.</summary>
        public Material[] Materials
        {
            get
            {
                var m = new Material[_matCount];
                System.Array.Copy(_mats, m, _matCount);
                return m;
            }
        }

        /// <summary>Gives <paramref name="go"/> this geometry (mesh space = the object's local space,
        /// assuming an unscaled, unrotated object at <paramref name="origin"/>).</summary>
        public void ApplyTo(GameObject go, Vector3 origin, ShadowCastingMode shadows = ShadowCastingMode.On)
        {
            Mesh mesh = Bake(go.name, origin, go.transform);
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
            if (_count == 0) return null;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            ApplyTo(go, origin, shadows);
            Emits++;
            EmitTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            return go;
        }

        /// <summary>A GameObject at <paramref name="origin"/> under <paramref name="parent"/> with an empty mesh this
        /// geometry is to be baked into later, all at once with others (<see cref="MeshBake"/>); null when nothing was drawn.</summary>
        internal GameObject EmitLater(string name, Transform parent, Vector3 origin, ShadowCastingMode shadows, MeshBake bake)
        {
            if (_count == 0) return null;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            var mesh = new Mesh { name = name };
            CreatedMeshes?.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = Materials;
            mr.shadowCastingMode = shadows;
            mr.receiveShadows = true;
            bake.Add(this, origin, mesh, go);
            Emits++;
            EmitTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            return go;
        }

        /// <summary>Copies the geometry into <paramref name="d"/> (any thread), vertices relative to
        /// <paramref name="origin"/>, 16-bit indices where they fit; <paramref name="bounds"/> is what it covers. Returns how
        /// many vertices weren't finite (made harmless, see <see cref="PackVertices"/>): the caller reports them.</summary>
        internal int PackInto(Mesh.MeshData d, Vector3 origin, out Bounds bounds)
        {
            PackedVertex[] vs = PackVertices(origin, out bounds, out int bad);
            d.SetVertexBufferParams(_count, s_Layout);
            Unity.Collections.NativeArray<PackedVertex>.Copy(vs, 0, d.GetVertexData<PackedVertex>(0), 0, _count);
            int ic = IndexCount;
            bool wide = _count > 65535;
            d.SetIndexBufferParams(ic, wide ? IndexFormat.UInt32 : IndexFormat.UInt16);
            if (wide) Unity.Collections.NativeArray<int>.Copy((int[])PackIndices(ic, true), 0, d.GetIndexData<int>(), 0, ic);
            else Unity.Collections.NativeArray<ushort>.Copy((ushort[])PackIndices(ic, false), 0, d.GetIndexData<ushort>(), 0, ic);
            d.subMeshCount = _matCount;
            int start = 0;
            for (int s = 0; s < _matCount; s++)
            {
                int n = _triCount[s];
                d.SetSubMesh(s, new SubMeshDescriptor(start, n) { bounds = bounds, firstVertex = 0, vertexCount = _count }, QuietUpdate);
                start += n;
            }
            return bad;
        }

        /// <summary>The positions drawn so far (mesh space, before any origin) into <paramref name="into"/> (cleared first):
        /// for a pass that wants to measure what it drew without reading a baked mesh back.</summary>
        internal void Positions(List<Vector3> into)
        {
            into.Clear();
            for (int i = 0; i < _count; i++) into.Add(_v[i]);
        }

        /// <summary>The least and greatest of x, z, x + z and x - z over the vertices drawn so far (mesh space, less
        /// <paramref name="origin"/>): the octagon in plan that holds them, without reading a baked mesh back. False when
        /// nothing is drawn.</summary>
        internal bool PlanExtents(Vector3 origin, out Vector4 least, out Vector4 most)
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, s0 = float.MaxValue, d0 = float.MaxValue;
            float x1 = float.MinValue, z1 = float.MinValue, s1 = float.MinValue, d1 = float.MinValue;
            for (int i = 0; i < _count; i++)
            {
                float x = _v[i].x - origin.x, z = _v[i].z - origin.z, s = x + z, d = x - z;
                if (x < x0) x0 = x;
                if (x > x1) x1 = x;
                if (z < z0) z0 = z;
                if (z > z1) z1 = z;
                if (s < s0) s0 = s;
                if (s > s1) s1 = s;
                if (d < d0) d0 = d;
                if (d > d1) d1 = d;
            }
            least = new Vector4(x0, z0, s0, d0);
            most = new Vector4(x1, z1, s1, d1);
            return _count > 0;
        }

        /// <summary>The geometry as drawn (world/mesh space, before any origin), for the map harness's checks.</summary>
        internal void CopyGeometry(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, List<Material> materials)
        {
            vertices.Clear();
            normals.Clear();
            uvs.Clear();
            indices.Clear();
            materials.Clear();
            for (int i = 0; i < _count; i++)
            {
                vertices.Add(_v[i]);
                normals.Add(_n[i]);
                uvs.Add(_uv[i]);
            }
            for (int s = 0; s < _matCount; s++)
            {
                materials.Add(_mats[s]);
                for (int i = 0; i < _triCount[s]; i++) indices.Add(_tris[s][i]);
                indices.Add(-1);
            }
        }
        #endregion
    }

    /// <summary>
    /// Bakes many <see cref="RaidMesh"/>es into their meshes in one go through Unity's MeshData API: queue them with
    /// <see cref="Add"/> (or <see cref="RaidMesh.EmitLater"/>), then <see cref="Allocate"/> and <see cref="Apply"/> on the
    /// main thread, with <see cref="Pack"/> (the heavy part: copying the geometry into the meshes' buffers) in between on
    /// any thread (<see cref="PackAsync"/> starts it on a worker). The queued RaidMeshes mustn't change until it's
    /// applied; with <see cref="ReleaseSources"/> their buffers go back to the pool after.
    /// </summary>
    internal sealed class MeshBake
    {
        private readonly List<RaidMesh> _sources = new List<RaidMesh>();
        private readonly List<Vector3> _origins = new List<Vector3>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<GameObject> _owners = new List<GameObject>();
        private Bounds[] _bounds;
        private int[] _bad;
        private Mesh.MeshDataArray _data;
        private bool _allocated;
        private System.Threading.Tasks.Task _packing;

        // Applied meshes tell their renderers (which may be showing already), unlike RaidMesh.QuietUpdate.
        private const MeshUpdateFlags ApplyFlags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices
            | MeshUpdateFlags.DontResetBoneBounds;

        public int Count => _sources.Count;

        /// <summary>Give the queued RaidMeshes' buffers back to the pool once applied (they're not drawn into again).</summary>
        public bool ReleaseSources;

        /// <summary>The vertices queued.</summary>
        public long Vertices
        {
            get
            {
                long n = 0;
                for (int i = 0; i < _sources.Count; i++) n += _sources[i].VertexCount;
                return n;
            }
        }

        /// <summary>The meshes queued, in order.</summary>
        public List<Mesh> Meshes => _meshes;

        /// <summary>Queues <paramref name="source"/>'s geometry for <paramref name="mesh"/>, its vertices relative to
        /// <paramref name="origin"/>; <paramref name="owner"/> (optional) names it if its geometry turns out bad.</summary>
        public void Add(RaidMesh source, Vector3 origin, Mesh mesh, GameObject owner = null)
        {
            _sources.Add(source);
            _origins.Add(origin);
            _meshes.Add(mesh);
            _owners.Add(owner);
        }

        public void Allocate()
        {
            _data = Mesh.AllocateWritableMeshData(_sources.Count);
            _bounds = new Bounds[_sources.Count];
            _bad = new int[_sources.Count];
            _allocated = true;
        }

        public void Pack()
        {
            for (int i = 0; i < _sources.Count; i++) _bad[i] = _sources[i].PackInto(_data[i], _origins[i], out _bounds[i]);
        }

        /// <summary>Allocates (here, on the main thread) and starts <see cref="Pack"/> on a worker thread; <see cref="Packed"/>
        /// says when it's done, and <see cref="Apply"/> waits for it if it isn't.</summary>
        public void PackAsync()
        {
            if (!_allocated) Allocate();
            _packing = System.Threading.Tasks.Task.Run(Pack);
        }

        /// <summary>True when no packing is under way (it finished, or never started on a worker).</summary>
        public bool Packed => _packing == null || _packing.IsCompleted;

        public void Apply()
        {
            if (_packing != null)
            {
                // A worker that failed leaves the job to this thread.
                try { _packing.Wait(); }
                catch (System.AggregateException) { Pack(); }
                _packing = null;
            }
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            Mesh.ApplyAndDisposeWritableMeshData(_data, _meshes, ApplyFlags);
            _allocated = false;
            long vertices = 0;
            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshes[i].bounds = _bounds[i];
                vertices += _sources[i].VertexCount;
                if (_bad[i] > 0)
                    RaidMesh.ReportNonFinite(_owners[i] != null ? RaidMesh.PathOf(_owners[i].transform) : _meshes[i].name, _bad[i], _sources[i].VertexCount);
                if (ReleaseSources) _sources[i].Release();
            }
            RaidMesh.BakedMeshes += _meshes.Count;
            RaidMesh.BakedVertices += vertices;
            RaidMesh.BakeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        }

        /// <summary>Lets go of the buffers when it won't be applied after all.</summary>
        public void Dispose()
        {
            if (_packing != null)
            {
                try { _packing.Wait(); }
                catch (System.AggregateException) { }
                _packing = null;
            }
            if (_allocated) _data.Dispose();
            _allocated = false;
        }
    }
}
