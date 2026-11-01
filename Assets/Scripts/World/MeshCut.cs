using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Cuts a box out of a mesh where it stands: what lies inside the box goes, what crosses its faces is clipped at them, and
    /// the rest stays as it was (the hole battered through a wall, <see cref="PartitionWall"/>). Positions, normals and the
    /// first UV set carry over, interpolated where an edge is clipped; each sub-mesh keeps its material slot. For the meshes
    /// made here (<see cref="RaidMesh"/>): readable, and with no other vertex data worth keeping.
    /// </summary>
    internal static class MeshCut
    {
        private struct V
        {
            public Vector3 P, B, N;   // in the mesh, in the box, and the normal
            public Vector2 Uv;
        }

        private static readonly List<Vector3> s_Pos = new List<Vector3>(1024), s_Nrm = new List<Vector3>(1024), s_Box = new List<Vector3>(1024);
        private static readonly List<Vector2> s_Uv = new List<Vector2>(1024);
        private static readonly List<int> s_Tris = new List<int>(2048);
        private static readonly List<List<int>> s_Out = new List<List<int>>(4);
        private static V[] s_Poly = new V[16], s_Next = new V[16];
        private static int s_Count;

        /// <summary>Takes out of <paramref name="mesh"/> whatever lies inside the box from <paramref name="min"/> to
        /// <paramref name="max"/>, in the space <paramref name="toBox"/> takes the mesh's own into (turned and moved, never
        /// scaled). False, and the mesh left alone, when nothing of it reached into the box.</summary>
        internal static bool RemoveBox(Mesh mesh, Matrix4x4 toBox, Vector3 min, Vector3 max)
        {
            if (mesh == null || !mesh.isReadable) return false;
            for (int s = 0; s < mesh.subMeshCount; s++)
                if (mesh.GetTopology(s) != MeshTopology.Triangles) return false;
            mesh.GetVertices(s_Pos);
            int n = s_Pos.Count;
            if (n == 0) return false;
            mesh.GetNormals(s_Nrm);
            mesh.GetUVs(0, s_Uv);
            bool normals = s_Nrm.Count == n, uvs = s_Uv.Count == n;
            s_Box.Clear();
            for (int i = 0; i < n; i++) s_Box.Add(toBox.MultiplyPoint3x4(s_Pos[i]));

            int subs = mesh.subMeshCount;
            while (s_Out.Count < subs) s_Out.Add(new List<int>(256));
            bool changed = false;
            for (int s = 0; s < subs; s++)
            {
                List<int> into = s_Out[s];
                into.Clear();
                mesh.GetTriangles(s_Tris, s);
                for (int t = 0; t + 2 < s_Tris.Count; t += 3)
                {
                    int i0 = s_Tris[t], i1 = s_Tris[t + 1], i2 = s_Tris[t + 2];
                    Vector3 a = s_Box[i0], b = s_Box[i1], c = s_Box[i2];
                    if (Apart(a, b, c, min, max))
                    {
                        into.Add(i0);
                        into.Add(i1);
                        into.Add(i2);
                        continue;
                    }
                    changed = true;
                    if (Within(a, min, max) && Within(b, min, max) && Within(c, min, max)) continue;
                    // Straddling a face: the parts of it in each of the six slabs round the box, which between them hold
                    // everything outside it and never overlap.
                    for (int region = 0; region < 6; region++)
                    {
                        Start(i0, i1, i2, normals, uvs);
                        if (!Region(region, min, max)) continue;
                        Emit(into, normals, uvs);
                    }
                }
            }
            if (!changed)
            {
                Clean();
                return false;
            }

            mesh.Clear();
            if (s_Pos.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(s_Pos);
            if (normals) mesh.SetNormals(s_Nrm);
            if (uvs) mesh.SetUVs(0, s_Uv);
            mesh.subMeshCount = subs;
            for (int s = 0; s < subs; s++) mesh.SetTriangles(s_Out[s], s, false);
            mesh.RecalculateBounds();
            Clean();
            return true;
        }

        private static void Clean()
        {
            s_Pos.Clear();
            s_Nrm.Clear();
            s_Uv.Clear();
            s_Box.Clear();
            s_Tris.Clear();
            for (int i = 0; i < s_Out.Count; i++) s_Out[i].Clear();
        }

        // True when the triangle lies wholly beyond one of the box's faces (it can't reach in).
        private static bool Apart(Vector3 a, Vector3 b, Vector3 c, Vector3 min, Vector3 max)
            => (a.x <= min.x && b.x <= min.x && c.x <= min.x) || (a.x >= max.x && b.x >= max.x && c.x >= max.x)
            || (a.y <= min.y && b.y <= min.y && c.y <= min.y) || (a.y >= max.y && b.y >= max.y && c.y >= max.y)
            || (a.z <= min.z && b.z <= min.z && c.z <= min.z) || (a.z >= max.z && b.z >= max.z && c.z >= max.z);

        private static bool Within(Vector3 p, Vector3 min, Vector3 max)
            => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z;

        private static void Start(int i0, int i1, int i2, bool normals, bool uvs)
        {
            s_Count = 3;
            Put(0, i0, normals, uvs);
            Put(1, i1, normals, uvs);
            Put(2, i2, normals, uvs);
        }

        private static void Put(int k, int i, bool normals, bool uvs)
        {
            s_Poly[k] = new V
            {
                P = s_Pos[i], B = s_Box[i], N = normals ? s_Nrm[i] : Vector3.up, Uv = uvs ? s_Uv[i] : Vector2.zero,
            };
        }

        // Clips the polygon to one of the six slabs outside the box: x below it, x above it; within it in x, y below or above;
        // within it in x and y, z below or above. False when nothing is left.
        private static bool Region(int region, Vector3 min, Vector3 max)
        {
            switch (region)
            {
                case 0: return Keep(0, min.x, -1f);
                case 1: return Keep(0, max.x, 1f);
                case 2: return Keep(0, min.x, 1f) && Keep(0, max.x, -1f) && Keep(1, min.y, -1f);
                case 3: return Keep(0, min.x, 1f) && Keep(0, max.x, -1f) && Keep(1, max.y, 1f);
                case 4: return Keep(0, min.x, 1f) && Keep(0, max.x, -1f) && Keep(1, min.y, 1f) && Keep(1, max.y, -1f) && Keep(2, min.z, -1f);
                default: return Keep(0, min.x, 1f) && Keep(0, max.x, -1f) && Keep(1, min.y, 1f) && Keep(1, max.y, -1f) && Keep(2, max.z, 1f);
            }
        }

        // Keeps the part of the polygon where sign * (coordinate - value) >= 0 (Sutherland-Hodgman).
        private static bool Keep(int axis, float value, float sign)
        {
            int m = 0;
            for (int k = 0; k < s_Count; k++)
            {
                V cur = s_Poly[k], next = s_Poly[(k + 1) % s_Count];
                float dc = sign * (cur.B[axis] - value), dn = sign * (next.B[axis] - value);
                if (dc >= 0f) Add(ref m, cur);
                if ((dc >= 0f) != (dn >= 0f)) Add(ref m, Lerp(cur, next, dc / (dc - dn)));
            }
            V[] swap = s_Poly;
            s_Poly = s_Next;
            s_Next = swap;
            s_Count = m;
            return m >= 3;
        }

        private static void Add(ref int m, in V v)
        {
            if (m >= s_Next.Length) System.Array.Resize(ref s_Next, s_Next.Length * 2);
            s_Next[m++] = v;
        }

        private static V Lerp(in V a, in V b, float t) => new V
        {
            P = Vector3.LerpUnclamped(a.P, b.P, t),
            B = Vector3.LerpUnclamped(a.B, b.B, t),
            N = Vector3.LerpUnclamped(a.N, b.N, t),
            Uv = Vector2.LerpUnclamped(a.Uv, b.Uv, t),
        };

        // The clipped polygon as a fan of triangles over new vertices (it keeps the triangle's winding).
        private static void Emit(List<int> into, bool normals, bool uvs)
        {
            int first = s_Pos.Count;
            for (int k = 0; k < s_Count; k++)
            {
                V v = s_Poly[k];
                s_Pos.Add(v.P);
                s_Box.Add(v.B);
                if (normals) s_Nrm.Add(v.N.sqrMagnitude > 1e-12f ? v.N.normalized : Vector3.up);
                if (uvs) s_Uv.Add(v.Uv);
            }
            for (int k = 1; k + 1 < s_Count; k++)
            {
                into.Add(first);
                into.Add(first + k);
                into.Add(first + k + 1);
            }
        }
    }
}
