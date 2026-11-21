using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The walls' stand-in. A building's walls object is baked into the NavMesh, so it's never changed: while some of its
    // facings are cut it only casts shadows, and a stand-in under the site's overhead root shows the rest. The stand-in is
    // a copy of the walls mesh with its triangles sorted by facing (each goes with the wall it stands nearest to, measured
    // to the middle of each wall's thickness, so a wall's end at a corner stays with its own wall), plus, for every
    // facing, a stub of its bottom StubHeight metres (the walls cut through, with the openings where the doors are) and a
    // cap closing it; cutting a facing swaps its walls for its stub by emptying sub-meshes, which costs no allocation. It's
    // made the first time a building is cut and kept until its site goes.
    internal static partial class MapCutaway
    {
        /// <summary>What's left of a cut wall, PZ-style: its bottom this many metres, capped.</summary>
        internal const float StubHeight = 0.42f;

        /// <summary>The cap on a cut wall: a dark section line, so the cut reads at a glance.</summary>
        internal static readonly Color CapColour = new Color(0.30f, 0.28f, 0.26f);

        private const int Parts = 9;   // per facing: walls, stub; then caps (see Part)
        private const MeshUpdateFlags QuietUpdate = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;

        // Scratch for building stand-ins (main thread, one at a time).
        private static readonly List<Vector3> s_V = new List<Vector3>(4096);
        private static readonly List<Vector3> s_N = new List<Vector3>(4096);
        private static readonly List<Vector2> s_U = new List<Vector2>(4096);
        private static readonly List<int> s_Src = new List<int>(8192);
        private static readonly List<List<int>> s_Lists = new List<List<int>>(64);
        private static readonly List<Vector3>[] s_Cuts = { new List<Vector3>(64), new List<Vector3>(64), new List<Vector3>(64), new List<Vector3>(64) };
        private static readonly Dictionary<long, int> s_Ends = new Dictionary<long, int>(256);
        private static readonly List<Vector2> s_Loop = new List<Vector2>(64);
        private static Vector2[] s_LineA = new Vector2[32], s_LineB = new Vector2[32];
        private static bool[] s_Used = new bool[256];
        private static bool s_Warned;

        /// <summary>Stand-ins made so far, the time that took in all (ms) and the longest one (ms): the cost of a first cut.</summary>
        internal static int OverlaysMade;
        internal static double OverlayMs, OverlayMaxMs;

        // Sub-mesh slot (before packing) of facing g's walls (kind 0) or stub (kind 1) in material m, or its cap (kind 2).
        private static int Part(int kind, int g, int m, int mats) => kind == 2 ? 8 * mats + g : (kind * 4 + g) * mats + m;

        /// <summary>Shows the stand-in, with the facings in <paramref name="cut"/> (bits 0-3) lowered to their stubs; makes it
        /// the first time. When it can't be made, the building's walls all stay hidden.</summary>
        private static void ShowOverlay(Building b, int cut)
        {
            if (b.Overlay == null && (b.OverlayFailed || !BuildOverlay(b))) return;
            b.Overlay.enabled = true;
            int mats = b.OverlayMaterials;
            for (int g = 0; g < 4; g++)
            {
                bool lowered = (cut & (1 << g)) != 0;
                if (b.OverlayShown >= 0 && ((b.OverlayShown & (1 << g)) != 0) == lowered) continue;
                for (int m = 0; m < mats; m++)
                {
                    ShowPart(b, Part(0, g, m, mats), !lowered);
                    ShowPart(b, Part(1, g, m, mats), lowered);
                }
                ShowPart(b, Part(2, g, 0, mats), lowered);
            }
            b.OverlayShown = cut;
        }

        private static void ShowPart(Building b, int slot, bool on)
        {
            int sub = b.OverlayMap[slot];
            if (sub < 0) return;
            SubMeshDescriptor d = b.OverlayParts[sub];
            if (!on) d.indexCount = 0;
            b.OverlayMesh.SetSubMesh(sub, d, QuietUpdate);
        }

        /// <summary>Lets go of a building's stand-in (its site is going).</summary>
        private static void ReleaseOverlay(Building b)
        {
            if (b.Overlay != null) Destroy(b.Overlay.gameObject);
            if (b.OverlayMesh != null) Destroy(b.OverlayMesh);
            b.Overlay = null;
            b.OverlayMesh = null;
            b.OverlayParts = null;
            b.OverlayMap = null;
            b.OverlayShown = -1;
        }

        private static void Destroy(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        private static bool BuildOverlay(Building b)
        {
            if (b.WallsMesh != null && b.WallsMesh.vertexCount == 0) return false;   // queued on its site's bake, not baked yet
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                MeshFilter mf = b.Walls.GetComponent<MeshFilter>();
                Mesh src = mf != null ? mf.sharedMesh : null;
                Transform parent = b.Site != null ? b.Site.Overlays : null;
                if (src == null || !src.isReadable || parent == null)
                {
                    b.OverlayFailed = true;
                    return false;
                }
                Material[] materials = b.Walls.sharedMaterials;
                int mats = src.subMeshCount;
                if (materials.Length == 0 || mats == 0)
                {
                    b.OverlayFailed = true;
                    return false;
                }
                int slots = 8 * mats + 4;
                for (int i = 0; i < slots; i++)
                {
                    if (i == s_Lists.Count) s_Lists.Add(new List<int>(256));
                    s_Lists[i].Clear();
                }
                for (int g = 0; g < 4; g++) s_Cuts[g].Clear();
                s_V.Clear();
                s_N.Clear();
                s_U.Clear();
                src.GetVertices(s_V);
                src.GetNormals(s_N);
                src.GetUVs(0, s_U);
                while (s_N.Count < s_V.Count) s_N.Add(Vector3.up);
                while (s_U.Count < s_V.Count) s_U.Add(Vector2.zero);

                int lines = WallLines(b);
                LineGrid(lines, src.bounds);
                for (int m = 0; m < mats; m++)
                {
                    s_Src.Clear();
                    src.GetIndices(s_Src, m, true);
                    for (int t = 0; t + 2 < s_Src.Count; t += 3)
                    {
                        int i0 = s_Src[t], i1 = s_Src[t + 1], i2 = s_Src[t + 2];
                        int g = FacingOf(b, lines, (s_V[i0] + s_V[i1] + s_V[i2]) * (1f / 3f));
                        List<int> walls = s_Lists[Part(0, g, m, mats)];
                        walls.Add(i0);
                        walls.Add(i1);
                        walls.Add(i2);
                        Stub(i0, i1, i2, s_Lists[Part(1, g, m, mats)], s_Cuts[g]);
                    }
                }
                Surf cap = DecorPalette.Matte(CapColour);
                DecorPalette.Apply();
                for (int g = 0; g < 4; g++) Caps(s_Cuts[g], s_Lists[Part(2, g, 0, mats)], cap.Uv);

                var map = new int[slots];
                var used = new List<Material>(slots);
                var mesh = new Mesh { name = "Walls cut " + b.Id };
                if (s_V.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(s_V);
                mesh.SetNormals(s_N);
                mesh.SetUVs(0, s_U);
                int subs = 0;
                for (int i = 0; i < slots; i++) map[i] = s_Lists[i].Count > 0 ? subs++ : -1;
                mesh.subMeshCount = Mathf.Max(1, subs);
                for (int i = 0; i < slots; i++)
                {
                    if (map[i] < 0) continue;
                    mesh.SetTriangles(s_Lists[i], map[i], false);
                    used.Add(i >= 8 * mats ? cap.Mat : materials[Mathf.Min(i % mats, materials.Length - 1)]);
                }
                mesh.RecalculateBounds();
                var parts = new SubMeshDescriptor[subs];
                for (int i = 0; i < subs; i++) parts[i] = mesh.GetSubMesh(i);

                var go = new GameObject(mesh.name) { layer = b.Walls.gameObject.layer };
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(b.Walls.transform.position, b.Walls.transform.rotation);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = used.ToArray();
                mr.shadowCastingMode = ShadowCastingMode.Off;   // the walls themselves still cast every shadow
                mr.receiveShadows = true;
                mr.lightProbeUsage = b.Walls.lightProbeUsage;
                mr.reflectionProbeUsage = b.Walls.reflectionProbeUsage;
                mr.renderingLayerMask = b.Walls.renderingLayerMask;
                b.Overlay = mr;
                b.OverlayMesh = mesh;
                b.OverlayParts = parts;
                b.OverlayMap = map;
                b.OverlayMaterials = mats;
                b.OverlayShown = -1;
                return true;
            }
            catch (System.Exception e)
            {
                b.OverlayFailed = true;
                if (!s_Warned) Debug.LogWarning("[MapCutaway] couldn't make the cut walls of building " + b.Id + "; its walls go whole: " + e);
                s_Warned = true;
                return false;
            }
            finally
            {
                double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                OverlaysMade++;
                OverlayMs += ms;
                if (ms > OverlayMaxMs) OverlayMaxMs = ms;
            }
        }

        // The middle of each wall's thickness (mesh space), run on past its ends by half the thickness, as the walls stand
        // just inside the footprint and overlap at the corners.
        private static int WallLines(Building b)
        {
            int n = b.Local.Length;
            if (s_LineA.Length < n)
            {
                s_LineA = new Vector2[n * 2];
                s_LineB = new Vector2[n * 2];
            }
            float half = Mathf.Max(0.02f, b.WallThickness * 0.5f);
            for (int i = 0; i < n; i++)
            {
                Vector2 a = b.Local[i] - b.Middle, c = b.Local[(i + 1) % n] - b.Middle;
                Vector2 d = c - a;
                float len = d.magnitude;
                if (len < 1e-4f)
                {
                    s_LineA[i] = s_LineB[i] = a;
                    continue;
                }
                d /= len;
                var inward = new Vector2(-d.y, d.x);
                s_LineA[i] = a + inward * half - d * half;
                s_LineB[i] = c + inward * half + d * half;
            }
            return n;
        }

        // The nearest wall line to p (mesh space) decides its facing: the lines in p's cell of the grid first, then ring
        // after ring of cells round it until no farther cell could hold a nearer line.
        private static int FacingOf(Building b, int lines, Vector3 p)
        {
            var q = new Vector2(p.x, p.z);
            int ci = Mathf.Clamp(Mathf.FloorToInt((q.x - s_GX) / s_CW), 0, s_GW - 1);
            int cj = Mathf.Clamp(Mathf.FloorToInt((q.y - s_GZ) / s_CH), 0, s_GH - 1);
            float best = float.MaxValue, ring = Mathf.Min(s_CW, s_CH);
            int at = -1;
            for (int r = 0; ; r++)
            {
                bool any = false;
                for (int j = cj - r; j <= cj + r; j++)
                {
                    if (j < 0 || j >= s_GH) continue;
                    bool full = r == 0 || j == cj - r || j == cj + r;
                    for (int i = ci - r; i <= ci + r; i += full ? 1 : 2 * r)
                    {
                        if (i < 0 || i >= s_GW) continue;
                        any = true;
                        int cell = j * s_GW + i;
                        for (int k = s_GridStart[cell]; k < s_GridStart[cell + 1]; k++)
                        {
                            int line = s_GridLines[k];
                            Vector2 a = s_LineA[line], ab = s_LineB[line] - a;
                            float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                            float d = (a + ab * t - q).sqrMagnitude;
                            if (d >= best) continue;
                            best = d;
                            at = line;
                        }
                    }
                }
                if (!any) break;
                float clear = r * ring;   // nothing in the rings past this one is nearer than this
                if (at >= 0 && best <= clear * clear) break;
            }
            return at >= 0 ? b.EdgeGroup[at] : 0;
        }

        // A grid over the walls' mesh (mesh space, cells about LineCell metres), each cell listing the wall lines whose box
        // touches it: s_GridLines[s_GridStart[cell] .. s_GridStart[cell + 1]).
        private const float LineCell = 2f;
        private const int MaxLineCells = 128;   // along a side
        private static int[] s_GridStart = new int[1025], s_GridFill = new int[1024], s_GridLines = new int[1024];
        private static int s_GW, s_GH;
        private static float s_GX, s_GZ, s_CW, s_CH;

        private static void LineGrid(int lines, Bounds mesh)
        {
            float x0 = mesh.min.x, z0 = mesh.min.z, x1 = mesh.max.x, z1 = mesh.max.z;
            for (int i = 0; i < lines; i++)
            {
                x0 = Mathf.Min(x0, Mathf.Min(s_LineA[i].x, s_LineB[i].x));
                x1 = Mathf.Max(x1, Mathf.Max(s_LineA[i].x, s_LineB[i].x));
                z0 = Mathf.Min(z0, Mathf.Min(s_LineA[i].y, s_LineB[i].y));
                z1 = Mathf.Max(z1, Mathf.Max(s_LineA[i].y, s_LineB[i].y));
            }
            s_GX = x0;
            s_GZ = z0;
            s_CW = Mathf.Max(LineCell, (x1 - x0) / MaxLineCells);
            s_CH = Mathf.Max(LineCell, (z1 - z0) / MaxLineCells);
            s_GW = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / s_CW));
            s_GH = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / s_CH));
            int cells = s_GW * s_GH;
            if (s_GridStart.Length < cells + 1)
            {
                s_GridStart = new int[cells + 1];
                s_GridFill = new int[cells];
            }
            System.Array.Clear(s_GridStart, 0, cells + 1);
            int total = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int line = 0; line < lines; line++)
                {
                    Vector2 a = s_LineA[line], c = s_LineB[line];
                    int i0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, c.x) - s_GX) / s_CW), 0, s_GW - 1);
                    int i1 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.x, c.x) - s_GX) / s_CW), 0, s_GW - 1);
                    int j0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.y, c.y) - s_GZ) / s_CH), 0, s_GH - 1);
                    int j1 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.y, c.y) - s_GZ) / s_CH), 0, s_GH - 1);
                    for (int j = j0; j <= j1; j++)
                        for (int i = i0; i <= i1; i++)
                        {
                            int cell = j * s_GW + i;
                            if (pass == 0) s_GridStart[cell + 1]++;
                            else s_GridLines[s_GridFill[cell]++] = line;
                        }
                }
                if (pass > 0) break;
                for (int cell = 0; cell < cells; cell++)
                {
                    s_GridStart[cell + 1] += s_GridStart[cell];
                    s_GridFill[cell] = s_GridStart[cell];
                }
                total = s_GridStart[cells];
                if (s_GridLines.Length < total) s_GridLines = new int[total * 2];
            }
        }

        // The part of triangle (i0, i1, i2) below StubHeight into stub, the points where its edges cross that height (in
        // pairs, one pair per crossing) into cuts.
        private static void Stub(int i0, int i1, int i2, List<int> stub, List<Vector3> cuts)
        {
            const float h = StubHeight;
            float y0 = s_V[i0].y, y1 = s_V[i1].y, y2 = s_V[i2].y;
            if (y0 <= h && y1 <= h && y2 <= h)
            {
                stub.Add(i0);
                stub.Add(i1);
                stub.Add(i2);
                return;
            }
            if (y0 >= h && y1 >= h && y2 >= h) return;
            // Walk the triangle in its own winding, keeping what's below and adding a point where an edge crosses; the
            // points on the cut (crossings, or a corner lying right on it) make the segment the cap is built from.
            int count = 0;
            int p0 = 0, p1 = 0, p2 = 0, p3 = 0;
            int onA = -1, onB = -1;
            for (int k = 0; k < 3; k++)
            {
                int a = k == 0 ? i0 : k == 1 ? i1 : i2, c = k == 0 ? i1 : k == 1 ? i2 : i0;
                float ya = s_V[a].y, yc = s_V[c].y;
                if (ya <= h) Put(ref count, ref p0, ref p1, ref p2, ref p3, a);
                if (ya == h) On(ref onA, ref onB, a);
                if (!((ya < h && yc > h) || (ya > h && yc < h))) continue;
                int x = Crossing(a, c, h);
                Put(ref count, ref p0, ref p1, ref p2, ref p3, x);
                On(ref onA, ref onB, x);
            }
            if (count >= 3)
            {
                stub.Add(p0);
                stub.Add(p1);
                stub.Add(p2);
            }
            if (count == 4)
            {
                stub.Add(p0);
                stub.Add(p2);
                stub.Add(p3);
            }
            if (onA >= 0 && onB >= 0)
            {
                cuts.Add(s_V[onA]);
                cuts.Add(s_V[onB]);
            }
        }

        private static void On(ref int a, ref int b, int v)
        {
            if (a < 0) a = v;
            else if (b < 0) b = v;
        }

        private static void Put(ref int count, ref int p0, ref int p1, ref int p2, ref int p3, int v)
        {
            switch (count++)
            {
                case 0: p0 = v; break;
                case 1: p1 = v; break;
                case 2: p2 = v; break;
                default: p3 = v; break;
            }
        }

        // A new vertex where the edge a-c crosses height h, worked out from its lower end so the two triangles sharing the
        // edge get the very same point.
        private static int Crossing(int a, int c, float h)
        {
            if (s_V[a].y > s_V[c].y)
            {
                int swap = a;
                a = c;
                c = swap;
            }
            Vector3 va = s_V[a], vc = s_V[c];
            float t = (h - va.y) / (vc.y - va.y);
            var p = new Vector3(va.x + (vc.x - va.x) * t, h, va.z + (vc.z - va.z) * t);
            s_V.Add(p);
            s_N.Add((s_N[a] + (s_N[c] - s_N[a]) * t).normalized);
            s_U.Add(s_U[a] + (s_U[c] - s_U[a]) * t);
            return s_V.Count - 1;
        }

        // Closes the stubs: the crossing segments of each closed wall piece join into loops, each filled with a cap facing up.
        private static void Caps(List<Vector3> cuts, List<int> into, Vector2 uv)
        {
            int segs = cuts.Count / 2;
            if (segs < 3) return;
            s_Ends.Clear();
            if (s_Used.Length < segs) s_Used = new bool[segs * 2];
            for (int i = 0; i < segs; i++)
            {
                s_Used[i] = false;
                AddEnd(Round(cuts[2 * i]), i);
                AddEnd(Round(cuts[2 * i + 1]), i);
            }
            for (int start = 0; start < segs; start++)
            {
                if (s_Used[start]) continue;
                s_Used[start] = true;
                s_Loop.Clear();
                s_Loop.Add(new Vector2(cuts[2 * start].x, cuts[2 * start].z));
                long first = Round(cuts[2 * start]);
                Vector3 here = cuts[2 * start + 1];
                long key = Round(here);
                int seg = start;
                bool closed = false;
                for (int guard = 0; guard <= segs; guard++)
                {
                    if (key == first)
                    {
                        closed = true;
                        break;
                    }
                    s_Loop.Add(new Vector2(here.x, here.z));
                    int next = Other(key, seg);
                    if (next < 0 || s_Used[next]) break;
                    s_Used[next] = true;
                    seg = next;
                    here = Round(cuts[2 * seg]) == key ? cuts[2 * seg + 1] : cuts[2 * seg];
                    key = Round(here);
                }
                if (closed) Fill(into, uv);
            }
        }

        // Each end point, rounded to the millimetre, to the (at most two) segments meeting there, packed: the first in the
        // low 16 bits, the second in the high, each plus one.
        private static void AddEnd(long key, int seg)
        {
            if (seg >= 0xFFFF) return;
            if (!s_Ends.TryGetValue(key, out int v)) s_Ends[key] = seg + 1;
            else if ((v >> 16) == 0) s_Ends[key] = v | ((seg + 1) << 16);
        }

        private static int Other(long key, int seg)
        {
            if (!s_Ends.TryGetValue(key, out int v)) return -1;
            int a = (v & 0xFFFF) - 1, b = ((v >> 16) & 0xFFFF) - 1;
            return a == seg ? b : a;
        }

        private static long Round(Vector3 p) => ((long)Mathf.RoundToInt(p.x * 1000f) << 32) ^ (uint)Mathf.RoundToInt(p.z * 1000f);

        // Fills the loop in s_Loop with triangles facing up, at StubHeight.
        private static void Fill(List<int> into, Vector2 uv)
        {
            // Drop repeated and collinear points (a wall's face is two triangles, so each side comes in two pieces).
            for (int i = s_Loop.Count - 1; i >= 0 && s_Loop.Count >= 3; i--)
            {
                int n = s_Loop.Count;
                Vector2 a = s_Loop[(i + n - 1) % n], p = s_Loop[i], c = s_Loop[(i + 1) % n];
                Vector2 ap = p - a, pc = c - p;
                if (ap.sqrMagnitude < 1e-8f || Mathf.Abs(ap.x * pc.y - ap.y * pc.x) < 1e-6f) s_Loop.RemoveAt(i);
            }
            if (s_Loop.Count < 3 || Mathf.Abs(MapGeometry.SignedArea(s_Loop)) < 1e-4f) return;
            List<int> tris = MapGeometry.Triangulate(s_Loop);   // wound clockwise seen from above: facing up
            int first = s_V.Count;
            for (int i = 0; i < s_Loop.Count; i++)
            {
                s_V.Add(new Vector3(s_Loop[i].x, StubHeight, s_Loop[i].y));
                s_N.Add(Vector3.up);
                s_U.Add(uv);
            }
            for (int i = 0; i < tris.Count; i++) into.Add(first + tris[i]);
        }
    }
}
