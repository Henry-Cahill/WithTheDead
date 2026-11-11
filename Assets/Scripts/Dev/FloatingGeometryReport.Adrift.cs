#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CloneZ
{
    public static partial class FloatingGeometryReport
    {
        #region Touching nothing
        // Pieces of the world drawn (not part of a character) that, with everything they touch, touch nothing reaching the
        // ground (y 0 in every world the game builds): a prop hanging in mid-room, a cap hovering over its wall, a lamp off
        // its post. Two pieces touch when their bounds come within Gap of each other; but where one is more than Big across
        // and the other isn't (a building's merged walls round a chair, a yard's hedges under a crate), the big one's bounds
        // say nothing of where its surfaces are, and its triangles decide. A piece also rests on a collider (its real
        // shape) within Gap of its bounds when the collider reaches the ground or meets a piece that does. Lines, trails
        // and particles aren't solid, what sight and shots pass through isn't either, and nothing a camera carries is part
        // of the world.
        private const float Gap = 0.05f, Ground = 0.05f, Big = 3f, Cell = 8f, Huge = 64f;
        private static readonly int Solids = ~(1 << CloneZConfig.IgnoreRaycastLayer);

        private static void Adrift(StringBuilder sb, List<Finding> found, HashSet<Transform> owners)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var pieces = new List<Renderer>();
            var lo = new List<Vector3>();
            var hi = new List<Vector3>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (!Solid(r) || UnderAny(r.transform, owners)) continue;
                Bounds b = r.bounds;
                if (!IsFinite(b.min) || !IsFinite(b.max)) continue;
                pieces.Add(r);
                lo.Add(b.min);
                hi.Add(b.max);
            }
            int n = pieces.Count;
            var sets = new Sets(n);
            for (int i = 0; i < n; i++)
                if (lo[i].y <= Ground) sets.Ground(i);

            // Piece against piece, through a grid of cells over the ground; the few huge ones (the ground) against all.
            var cells = new Dictionary<long, List<int>>();
            var huge = new List<int>();
            for (int i = 0; i < n; i++)
            {
                if (hi[i].x - lo[i].x > Huge || hi[i].z - lo[i].z > Huge) { huge.Add(i); continue; }
                for (int x = CellOf(lo[i].x - Gap), x1 = CellOf(hi[i].x + Gap); x <= x1; x++)
                    for (int z = CellOf(lo[i].z - Gap), z1 = CellOf(hi[i].z + Gap); z <= z1; z++)
                    {
                        long key = CellKey(x, z);
                        if (!cells.TryGetValue(key, out List<int> cell)) cells[key] = cell = new List<int>();
                        cell.Add(i);
                    }
            }
            foreach (List<int> cell in cells.Values)
                for (int a = 0; a < cell.Count; a++)
                    for (int c = a + 1; c < cell.Count; c++)
                        sets.Meet(cell[a], cell[c], lo, hi);
            foreach (int h in huge)
                for (int i = 0; i < n; i++)
                    if (i != h) sets.Meet(i, h, lo, hi);

            // What's still adrift, against what its bounds meet that bounds alone can't settle: the colliders there, by
            // their real shapes, and the big pieces round it (or inside it), by their triangles. Again until nothing
            // changes, since a piece found resting can hold up another.
            Physics.SyncTransforms();
            var spread = new Vector3(Gap, Gap, Gap);
            var touching = new Dictionary<int, Collider[]>();
            var holding = new HashSet<Collider>();
            var meshes = new Dictionary<Renderer, Triangles>();
            var tried = new HashSet<long>();
            var pairs = new List<int>();
            var hits = new Collider[256];
            for (bool more = true; more;)
            {
                more = false;
                for (int i = 0; i < n; i++)
                {
                    if (sets.Grounded(i)) continue;
                    if (!touching.TryGetValue(i, out Collider[] near))
                    {
                        int k = Physics.OverlapBoxNonAlloc((lo[i] + hi[i]) * 0.5f, (hi[i] - lo[i]) * 0.5f + spread, hits, Quaternion.identity, Solids, QueryTriggerInteraction.Ignore);
                        touching[i] = near = new Collider[k];
                        Array.Copy(hits, near, k);
                    }
                    foreach (Collider c in near)
                    {
                        if (!holding.Contains(c) && !Holds(c, owners, sets, lo, hi, cells, huge)) continue;
                        holding.Add(c);
                        sets.Ground(i);
                        break;
                    }
                    if (!sets.Grounded(i))
                    {
                        Mismatched(i, lo, hi, cells, huge, pairs);
                        pairs.Sort((x, y) => sets.Grounded(y).CompareTo(sets.Grounded(x)));   // what already rests first
                    }
                    foreach (int j in pairs)
                    {
                        if (sets.Grounded(i)) break;
                        if (!tried.Add(((long)Math.Min(i, j) << 32) | (uint)Math.Max(i, j))) continue;
                        int big = IsBig(lo[j], hi[j]) ? j : i, small = big == j ? i : j;
                        if (MeshMeets(pieces[big], lo[small] - spread, hi[small] + spread, meshes)) sets.Union(i, j);
                    }
                    pairs.Clear();
                    if (sets.Grounded(i)) more = true;
                }
            }

            // Each group still adrift, by its biggest drawn piece.
            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                if (sets.Grounded(i) || !Drawn(pieces[i])) continue;
                int set = sets.Find(i);
                if (!groups.TryGetValue(set, out List<int> list)) groups[set] = list = new List<int>();
                list.Add(i);
            }
            var rows = new List<(string key, string line, string members, Object target)>();
            foreach (List<int> list in groups.Values)
            {
                int main = Biggest(list, pieces, lo, hi);
                var all = new Bounds((lo[main] + hi[main]) * 0.5f, hi[main] - lo[main]);
                var members = new StringBuilder();
                foreach (int i in list)
                {
                    all.Encapsulate(new Bounds((lo[i] + hi[i]) * 0.5f, hi[i] - lo[i]));
                    if (i != main && members.Length < 600) members.Append("        with ").Append(PathOf(pieces[i].transform)).Append(" (").Append(pieces[i].GetType().Name).AppendLine(")");
                }
                Renderer r = pieces[main];
                string name = PathOf(r.transform) + " (" + r.GetType().Name + ")";
                string line = name + (list.Count > 1 ? " and " + (list.Count - 1) + " more piece(s) touching it" : "") + ", centre " + F(all.center)
                    + " size " + F(all.size) + ", " + Below(all) + ", material " + (r.sharedMaterial != null ? r.sharedMaterial.name : "none");
                rows.Add(("adrift " + name, line, members.ToString(), r.gameObject));
            }
            rows.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            sb.AppendLine().Append("== Drawn, touching nothing that reaches the ground (not part of a character): ").Append(rows.Count)
              .Append("  (").Append(n).Append(" pieces of the world, ").Append(clock.ElapsedMilliseconds).AppendLine(" ms)");
            for (int i = 0; i < rows.Count; i++)
            {
                if (i < MaxListed) sb.Append("    ").AppendLine(rows[i].line).Append(rows[i].members);
                else if (i == MaxListed) sb.AppendLine("    ...");
                found.Add(new Finding(rows[i].key, rows[i].line + ": touches nothing that reaches the ground", rows[i].target));
            }
        }

        // The pieces near i that the bounds couldn't settle: within Gap of it, one of the two big and the other not.
        private static void Mismatched(int i, List<Vector3> lo, List<Vector3> hi, Dictionary<long, List<int>> cells, List<int> huge, List<int> into)
        {
            var seen = new HashSet<int>();
            bool big = IsBig(lo[i], hi[i]);
            void Consider(int j)
            {
                if (j != i && seen.Add(j) && IsBig(lo[j], hi[j]) != big && Within(lo[i], hi[i], lo[j], hi[j])) into.Add(j);
            }
            if (hi[i].x - lo[i].x <= Huge && hi[i].z - lo[i].z <= Huge)
                for (int x = CellOf(lo[i].x - Gap), x1 = CellOf(hi[i].x + Gap); x <= x1; x++)
                    for (int z = CellOf(lo[i].z - Gap), z1 = CellOf(hi[i].z + Gap); z <= z1; z++)
                        if (cells.TryGetValue(CellKey(x, z), out List<int> cell))
                            foreach (int j in cell) Consider(j);
            foreach (int j in huge) Consider(j);
        }

        /// <summary>True when nothing solid of the world, a collider or a drawn piece's triangles, comes into
        /// <paramref name="box"/>: a clear spot for the smoke test's floating test prop.</summary>
        public static bool IsClear(Bounds box)
        {
            if (Physics.CheckBox(box.center, box.extents, Quaternion.identity, Solids, QueryTriggerInteraction.Ignore)) return false;
            var meshes = new Dictionary<Renderer, Triangles>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                if (Solid(r) && r.bounds.Intersects(box) && MeshMeets(r, box.min, box.max, meshes)) return false;
            return true;
        }

        // A piece's triangles in world space, read once; none when its mesh can't be read as drawn (then it's taken to
        // meet whatever its bounds come near).
        private sealed class Triangles
        {
            public Vector3[] Points;
            public int[] Corners;
        }

        private static bool MeshMeets(Renderer r, Vector3 min, Vector3 max, Dictionary<Renderer, Triangles> cache)
        {
            if (!cache.TryGetValue(r, out Triangles t)) cache[r] = t = Read(r);
            if (t.Corners == null) return true;
            Vector3 centre = (min + max) * 0.5f, half = (max - min) * 0.5f;
            Vector3[] p = t.Points;
            int[] c = t.Corners;
            for (int k = 0; k + 2 < c.Length; k += 3)
            {
                Vector3 a = p[c[k]], b = p[c[k + 1]], d = p[c[k + 2]];
                if (Mathf.Max(a.x, Mathf.Max(b.x, d.x)) < min.x || Mathf.Min(a.x, Mathf.Min(b.x, d.x)) > max.x
                    || Mathf.Max(a.y, Mathf.Max(b.y, d.y)) < min.y || Mathf.Min(a.y, Mathf.Min(b.y, d.y)) > max.y
                    || Mathf.Max(a.z, Mathf.Max(b.z, d.z)) < min.z || Mathf.Min(a.z, Mathf.Min(b.z, d.z)) > max.z) continue;
                if (TriangleMeetsBox(a - centre, b - centre, d - centre, half)) return true;
            }
            return false;
        }

        private static Triangles Read(Renderer r)
        {
            Mesh m = r is MeshRenderer && !r.isPartOfStaticBatch && r.TryGetComponent(out MeshFilter f) ? f.sharedMesh : null;
            if (m == null || !m.isReadable) return new Triangles();
            Vector3[] points = m.vertices;
            Matrix4x4 toWorld = r.localToWorldMatrix;
            for (int i = 0; i < points.Length; i++) points[i] = toWorld.MultiplyPoint3x4(points[i]);
            var corners = new List<int>();
            for (int s = 0; s < m.subMeshCount; s++)
            {
                MeshTopology topology = m.GetTopology(s);
                if (topology == MeshTopology.Triangles) corners.AddRange(m.GetIndices(s));
                else if (topology == MeshTopology.Quads)
                {
                    int[] q = m.GetIndices(s);
                    for (int k = 0; k + 3 < q.Length; k += 4) corners.AddRange(new[] { q[k], q[k + 1], q[k + 2], q[k], q[k + 2], q[k + 3] });
                }
            }
            return new Triangles { Points = points, Corners = corners.ToArray() };
        }

        // Whether the triangle (a, b, c), relative to a box's centre, meets the box of half-size h: the separating axes
        // of Akenine-Möller's test (the box's three, the triangle's normal, and the nine edge-by-axis cross products).
        private static bool TriangleMeetsBox(Vector3 a, Vector3 b, Vector3 c, Vector3 h)
        {
            if (Mathf.Min(a.x, Mathf.Min(b.x, c.x)) > h.x || Mathf.Max(a.x, Mathf.Max(b.x, c.x)) < -h.x
                || Mathf.Min(a.y, Mathf.Min(b.y, c.y)) > h.y || Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < -h.y
                || Mathf.Min(a.z, Mathf.Min(b.z, c.z)) > h.z || Mathf.Max(a.z, Mathf.Max(b.z, c.z)) < -h.z) return false;
            Vector3 e0 = b - a, e1 = c - b, e2 = a - c;
            if (Apart(Vector3.Cross(e0, e1), a, b, c, h)) return false;
            foreach (Vector3 e in new[] { e0, e1, e2 })
                if (Apart(new Vector3(0f, -e.z, e.y), a, b, c, h) || Apart(new Vector3(e.z, 0f, -e.x), a, b, c, h) || Apart(new Vector3(-e.y, e.x, 0f), a, b, c, h))
                    return false;
            return true;
        }

        private static bool Apart(Vector3 axis, Vector3 a, Vector3 b, Vector3 c, Vector3 h)
        {
            float pa = Vector3.Dot(axis, a), pb = Vector3.Dot(axis, b), pc = Vector3.Dot(axis, c);
            float reach = h.x * Mathf.Abs(axis.x) + h.y * Mathf.Abs(axis.y) + h.z * Mathf.Abs(axis.z);
            return Mathf.Min(pa, Mathf.Min(pb, pc)) > reach || Mathf.Max(pa, Mathf.Max(pb, pc)) < -reach;
        }

        // The biggest of a group of pieces (by the volume of its bounds; the first by path among equals), to name it by.
        private static int Biggest(List<int> group, List<Renderer> pieces, List<Vector3> lo, List<Vector3> hi)
        {
            int main = group[0];
            foreach (int i in group)
            {
                float vi = Volume(lo[i], hi[i]), vm = Volume(lo[main], hi[main]);
                if (vi > vm || vi == vm && string.CompareOrdinal(PathOf(pieces[i].transform), PathOf(pieces[main].transform)) < 0) main = i;
            }
            return main;
        }

        // A collider holds up what meets it when it reaches the ground, or its bounds meet a piece that does.
        private static bool Holds(Collider c, HashSet<Transform> owners, Sets sets, List<Vector3> lo, List<Vector3> hi,
            Dictionary<long, List<int>> cells, List<int> huge)
        {
            if (c == null || UnderAny(c.transform, owners)) return false;
            Bounds b = c.bounds;
            Vector3 cmin = b.min, cmax = b.max;
            if (cmin.y <= Ground) return true;
            if (!IsFinite(cmin) || !IsFinite(cmax)) return false;
            if (cmax.x - cmin.x > Huge || cmax.z - cmin.z > Huge)
            {
                for (int j = 0; j < lo.Count; j++)
                    if (sets.Grounded(j) && Touch(lo[j], hi[j], cmin, cmax)) return true;
                return false;
            }
            for (int x = CellOf(cmin.x - Gap), x1 = CellOf(cmax.x + Gap); x <= x1; x++)
                for (int z = CellOf(cmin.z - Gap), z1 = CellOf(cmax.z + Gap); z <= z1; z++)
                    if (cells.TryGetValue(CellKey(x, z), out List<int> cell))
                        foreach (int j in cell)
                            if (sets.Grounded(j) && Touch(lo[j], hi[j], cmin, cmax)) return true;
            foreach (int j in huge)
                if (sets.Grounded(j) && Touch(lo[j], hi[j], cmin, cmax)) return true;
            return false;
        }

        // Bounds within Gap of each other, and the two much of a size: where one is more than Big across and the other
        // isn't, the big one's bounds say nothing of where its surfaces are (see Mismatched).
        private static bool Touch(Vector3 amin, Vector3 amax, Vector3 bmin, Vector3 bmax)
            => Within(amin, amax, bmin, bmax) && IsBig(amin, amax) == IsBig(bmin, bmax);

        private static bool Within(Vector3 amin, Vector3 amax, Vector3 bmin, Vector3 bmax)
            => !(amin.x > bmax.x + Gap || bmin.x > amax.x + Gap || amin.y > bmax.y + Gap || bmin.y > amax.y + Gap
                 || amin.z > bmax.z + Gap || bmin.z > amax.z + Gap);

        private static bool IsBig(Vector3 min, Vector3 max) => Mathf.Max(max.x - min.x, Mathf.Max(max.y - min.y, max.z - min.z)) > Big;

        // What a group adrift hangs over, and the nearest solid surface round it (a box swept out of its bounds along each
        // axis), for the reader.
        private static string Below(Bounds b)
        {
            var from = new Vector3(b.center.x, b.min.y - 0.01f, b.center.z);
            string under = Physics.Raycast(from, Vector3.down, out RaycastHit hit, 200f, Solids, QueryTriggerInteraction.Ignore)
                ? hit.distance.ToString("0.00") + " m above " + PathOf(hit.collider.transform)
                : "nothing under it";
            float best = float.MaxValue;
            string nearest = null;
            foreach (Vector3 dir in new[] { Vector3.left, Vector3.right, Vector3.down, Vector3.up, Vector3.back, Vector3.forward })
                if (Physics.BoxCast(b.center, b.extents, dir, out RaycastHit h, Quaternion.identity, 2f, Solids, QueryTriggerInteraction.Ignore) && h.distance < best)
                {
                    best = h.distance;
                    nearest = h.distance.ToString("0.00") + " m off to " + Axis(dir) + " (" + PathOf(h.collider.transform) + ")";
                }
            return under + ", the nearest surface " + (nearest ?? "more than 2 m off");
        }

        private static string Axis(Vector3 d) => d.x < 0f ? "-X" : d.x > 0f ? "+X" : d.y < 0f ? "-Y" : d.y > 0f ? "+Y" : d.z < 0f ? "-Z" : "+Z";

        // Solid geometry with something to draw, not carried by a camera. What sight and shots pass through (the Ignore
        // Raycast layer: characters and creatures, a grenade in flight, the isometric view's fog-of-war shade) isn't.
        private static bool Solid(Renderer r)
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer || r is BillboardRenderer) return false;
            if (r.gameObject.layer == CloneZConfig.IgnoreRaycastLayer) return false;
            if (r is MeshRenderer && (!r.TryGetComponent(out MeshFilter mf) || mf.sharedMesh == null)) return false;
            if (r is SkinnedMeshRenderer s && s.sharedMesh == null) return false;
            return r.GetComponentInParent<Camera>() == null;
        }

        private static bool Drawn(Renderer r) => r.enabled && !r.forceRenderingOff && r.shadowCastingMode != ShadowCastingMode.ShadowsOnly;

        private static float Volume(Vector3 min, Vector3 max) => (max.x - min.x) * (max.y - min.y) * (max.z - min.z);

        private static int CellOf(float v) => Mathf.FloorToInt(v / Cell);

        private static long CellKey(int x, int z) => ((long)x << 32) ^ (uint)z;

        // The pieces in sets of those touching, each set knowing whether anything in it reaches the ground.
        private sealed class Sets
        {
            private readonly int[] _up;
            private readonly bool[] _ground;

            public Sets(int n)
            {
                _up = new int[n];
                _ground = new bool[n];
                for (int i = 0; i < n; i++) _up[i] = i;
            }

            public int Find(int i)
            {
                while (_up[i] != i)
                {
                    _up[i] = _up[_up[i]];
                    i = _up[i];
                }
                return i;
            }

            public bool Grounded(int i) => _ground[Find(i)];

            public void Ground(int i) => _ground[Find(i)] = true;

            // Puts two pieces in one set if they touch, unless both sets already reach the ground (nothing to learn).
            public void Meet(int i, int j, List<Vector3> lo, List<Vector3> hi)
            {
                int a = Find(i), b = Find(j);
                if (a == b || _ground[a] && _ground[b] || !Touch(lo[i], hi[i], lo[j], hi[j])) return;
                _up[b] = a;
                _ground[a] |= _ground[b];
            }

            public void Union(int i, int j)
            {
                int a = Find(i), b = Find(j);
                if (a == b) return;
                _up[b] = a;
                _ground[a] |= _ground[b];
            }
        }
        #endregion
    }
}
#endif
