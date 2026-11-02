using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace CloneZ
{
    // The walls' stand-in. A building's walls object is baked into the NavMesh, so it's never changed (but for a hole battered
    // through it, OutsideWall, after which its stand-in is made again: WallsChanged): while some of its
    // facings are cut, or the storeys above are lifted, it only casts shadows and a stand-in under the site's overhead root
    // shows the walls instead. The stand-in is a copy of the walls mesh with its triangles sorted by facing (each goes with
    // the wall it stands nearest to, measured to the middle of each wall's thickness, so a wall's end at a corner stays with
    // its own wall), cut off at the ground storey's top when anything of the walls (a tall window's head, the paper of a
    // tall room) reaches above it, so nothing floats once the storeys above are lifted; plus, for every facing, a stub of its
    // bottom StubHeight metres (the walls cut through, with the openings where the doors are) and a cap closing it. Cutting a
    // facing swaps its walls for its stub by emptying sub-meshes, which costs no allocation. Stand-ins are made a slice at a
    // time a frame (MapCutaway.Work) for the buildings round the survivor before they're needed, at once only when walls must
    // be cut and theirs isn't ready, and let go of when the survivor has gone on.
    internal static partial class MapCutaway
    {
        /// <summary>What's left of a cut wall, PZ-style: its bottom this many metres, capped.</summary>
        internal const float StubHeight = 0.42f;

        /// <summary>The cap on a cut wall: a dark section line, so the cut reads at a glance.</summary>
        internal static readonly Color CapColour = new Color(0.30f, 0.28f, 0.26f);

        /// <summary>Walls whose ground storey stops lower than this are never clipped (a canopy's posts, a ruin's stumps).</summary>
        private const float MinClip = 1.5f;

        /// <summary>Stand-ins kept at most (the farthest unused go first), and how far from the survivor an unused one goes.</summary>
        internal static int MaxOverlays = 24;
        internal static float EvictRadius = 40f;

        private const MeshUpdateFlags Toggle = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
        private const MeshUpdateFlags Quiet = Toggle | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds;

        // What the walls show: the walls object, nothing (no stand-in could be made: the walls are cut whole), or the stand-in.
        internal const int WallsWhole = 0, WallsNone = 1, WallsStandIn = 2;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        /// <summary>Vertex data for the stand‑in mesh.</summary>
        private struct StandInVertex
        {
            public Vector3 Position, Normal;
            public Vector2 Uv;
        }

        private static readonly VertexAttributeDescriptor[] s_StandInLayout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        // Scratch for the stand-in being made (main thread, one at a time, kept between its slices).
        private static readonly List<Vector3> s_V = new List<Vector3>(4096);
        private static readonly List<Vector3> s_N = new List<Vector3>(4096);
        private static readonly List<Vector2> s_U = new List<Vector2>(4096);
        private static readonly List<int> s_Idx = new List<int>(16384);
        private static readonly List<int> s_Read = new List<int>(8192);
        private static int[] s_SubStart = new int[16], s_SubCount = new int[16];
        private static readonly List<List<int>> s_Lists = new List<List<int>>(64);
        private static readonly List<Vector3>[] s_Cuts = { new List<Vector3>(64), new List<Vector3>(64), new List<Vector3>(64), new List<Vector3>(64) };
        private static readonly Dictionary<long, int> s_Ends = new Dictionary<long, int>(256);
        private static readonly List<Vector2> s_Loop = new List<Vector2>(64);
        private static Vector2[] s_LineA = new Vector2[32], s_LineB = new Vector2[32];
        private static bool[] s_Used = new bool[256];
        private static StandInVertex[] s_Packed = new StandInVertex[4096];
        private static int[] s_Packed32 = new int[16384];
        private static ushort[] s_Packed16 = new ushort[16384];
        private static bool s_Warned;

        /// <summary>The buildings that have a stand-in now.</summary>
        private static readonly List<Building> s_Overlays = new List<Building>(32);

        /// <summary>Stand-ins made so far: how many, the time that took in all (ms, every slice), the longest slice (ms), the
        /// longest a stand-in took in all (ms), and how many were let go of again.</summary>
        internal static int OverlaysMade, OverlaysEvicted;
        internal static double OverlayMs, OverlaySliceMaxMs, OverlayWholeMaxMs;
        /// <summary>The longest slice (for the harness): the stage it began at and the one it ended at (4: the stand-in
        /// finished), the walls' vertices, and whether a garbage collection fell in it.</summary>
        internal static int OverlaySliceMaxFrom, OverlaySliceMaxTo, OverlaySliceMaxVertices;
        internal static bool OverlaySliceMaxGc;

        /// <summary>Stand-ins alive now, and their meshes' size in bytes.</summary>
        internal static int OverlaysAlive => s_Overlays.Count;
        internal static long OverlayBytes;

        /// <summary>The stand-in being made, a slice at a time.</summary>
        private sealed class OverlayJob
        {
            public Building B;
            public int Stage, Mats, Slots, Lines, Sub, Tri, Vertices;
            public Material[] Materials;
            public Material StubMat; // ← new
            public bool Clip;
            public float ClipTop;
            public double Ms;
        }

        private static readonly OverlayJob s_Job = new OverlayJob();

        // Sub-mesh slot (before packing) of facing g's standing walls (kind 0) or stub (kind 1) in material m, or its cap
        // (kind 2).
        private static int Part(int kind, int g, int m, int mats) => kind == 2 ? 8 * mats + g : (kind * 4 + g) * mats + m;

        private static bool OverlayReady(Building b) => b.Overlay != null && b.OverlayMesh != null;

        /// <summary>True when the building's walls need its stand-in to show the parts in <paramref name="mask"/> cut.</summary>
        private static bool NeedsStandIn(Building b, int mask)
            => b.Walls != null && ((mask & WallBits) != 0 || ((mask & UpperBit) != 0 && b.ClipWalls));

        // Puts the walls in the state the building's cut asks for (see Apply): whole, the stand-in with the cut facings lowered
        // (a cut needing it is held back until it's ready: see Ready), or, when no stand-in can be made, cut whole.
        private static void ShowWalls(Building b)
        {
            if (b.Walls == null) return;
            int mask = b.Applied, cut = mask & WallBits;
            int state = !NeedsStandIn(b, mask) ? WallsWhole : OverlayReady(b) ? WallsStandIn : cut != 0 ? WallsNone : WallsWhole;
            if (state != b.WallState)
            {
                b.Walls.shadowCastingMode = state == WallsWhole ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
                if (b.Overlay != null) b.Overlay.enabled = state == WallsStandIn;
                b.WallState = state;
            }
            if (state == WallsStandIn) Lower(b, cut);
        }

        // The stand-in's facings in `cut` lowered to their stubs, the others standing.
        private static void Lower(Building b, int cut)
        {
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

        /// <summary>Add a comment describing ShowPart.</summary>
        private static void ShowPart(Building b, int slot, bool on)
        {
            int sub = b.OverlayMap[slot];
            if (sub < 0) return;
            SubMeshDescriptor d = b.OverlayParts[sub];
            if (!on) d.indexCount = 0;
            b.OverlayMesh.SetSubMesh(sub, d, Toggle);
        }

        /// <summary>A building's walls mesh cut (a hole battered through one of its outside walls): its stand-in, a copy of the
        /// walls as they were, let go of, and made again at once if its walls are cut now (else when they next are).</summary>
        internal static void WallsChanged(long id)
        {
            if (!s_ById.TryGetValue(id, out Building b)) return;
            bool showing = b.Applied != 0 && OverlayReady(b);
            ReleaseOverlay(b);
            if (!showing || !b.Measured || b.OverlayFailed) return;
            MakeOverlayNow(b);
            ShowWalls(b);
        }

        /// <summary>Lets go of a building's stand-in (its site is going, or the survivor has gone on).</summary>
        private static void ReleaseOverlay(Building b)
        {
            if (ReferenceEquals(s_Job.B, b)) AbortOverlayJob();
            if (b.WallState == WallsStandIn && b.Walls != null)
            {
                b.Walls.shadowCastingMode = b.Applied != 0 ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
                b.WallState = b.Applied != 0 ? WallsNone : WallsWhole;
            }
            if (b.Overlay != null) Destroy(b.Overlay.gameObject);
            if (b.OverlayMesh != null)
            {
                OverlayBytes -= b.OverlayBytes;
                Destroy(b.OverlayMesh);
            }
            b.Overlay = null;
            b.OverlayMesh = null;
            b.OverlayParts = null;
            b.OverlayMap = null;
            b.OverlayShown = -1;
            b.OverlayBytes = 0;
            s_Overlays.Remove(b);
        }

        private static void Destroy(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        // The unused stand-ins of buildings the survivor has left behind, and the farthest past the cap.
        private static void Evict(Vector2 at)
        {
            for (int i = s_Overlays.Count - 1; i >= 0; i--)
            {
                Building b = s_Overlays[i];
                if (b.Applied != 0 || b.Site == null) continue;
                if (RectDistance(b.Bounds, at) <= EvictRadius) continue;
                ReleaseOverlay(b);
                OverlaysEvicted++;
            }
            while (s_Overlays.Count > MaxOverlays)
            {
                Building far = null;
                float farD = -1f;
                for (int i = 0; i < s_Overlays.Count; i++)
                {
                    Building b = s_Overlays[i];
                    if (b.Applied != 0) continue;
                    float d = RectDistance(b.Bounds, at);
                    if (d > farD) { farD = d; far = b; }
                }
                if (far == null) break;
                ReleaseOverlay(far);
                OverlaysEvicted++;
            }
        }

        #region Making a stand-in
        // Makes building b's stand-in now, whatever the time (the harness's pictures).
        private static void MakeOverlayNow(Building b)
        {
            if (!ReferenceEquals(s_Job.B, b))
            {
                AbortOverlayJob();
                if (!StartOverlayJob(b)) return;
            }
            StepOverlayJob(long.MaxValue);
        }

        private static void AbortOverlayJob()
        {
            s_Job.B = null;
            s_Job.Materials = null;
            s_Job.Stage = 0;
        }

        // Starts making b's stand-in: false when it can't be (no walls, not baked yet, or unreadable: then never).
        private static bool StartOverlayJob(Building b)
        {
            if (b.Walls == null || b.Site == null || b.OverlayFailed || OverlayReady(b)) return false;
            Mesh src = b.WallsMesh;
            if (src == null || src.vertexCount == 0 || !b.Measured) return false;   // not baked yet: later
            Transform parent = b.Site.Overlays;
            Material[] materials = b.Walls.sharedMaterials;
            if (!src.isReadable || parent == null || materials.Length == 0 || src.subMeshCount == 0)
            {
                Failed(b, "its walls mesh can't be read");
                return false;
            }
            s_Job.B = b;
            s_Job.Stage = 0;
            s_Job.Materials = materials;
            s_Job.StubMat = new Material(materials[0]);
            s_Job.StubMat.color = GetStubColour(b);
            s_Job.Mats = src.subMeshCount;
            s_Job.Clip = b.ClipWalls;
            s_Job.ClipTop = b.ClipTop;
            s_Job.Ms = 0;
            return true;
        }

        private static void Failed(Building b, string why)
        {
            b.OverlayFailed = true;
            if (ReferenceEquals(s_Job.B, b)) AbortOverlayJob();
            if (!s_Warned) Debug.LogWarning("[MapCutaway] couldn't make the cut walls of building " + b.Id + " (" + why + "); its walls go whole when cut.");
            s_Warned = true;
        }

        // Runs the stand-in being made until it's done or `deadline` (stopwatch ticks) passes. True when it's done (made, or
        // given up on).
        private static bool StepOverlayJob(long deadline)
        {
            OverlayJob job = s_Job;
            Building b = job.B;
            if (b == null) return true;
            if (b.Site == null || b.Walls == null || b.WallsMesh == null)
            {
                AbortOverlayJob();
                return true;
            }
            long started = Stopwatch.GetTimestamp();
            int from = job.Stage, collections = System.GC.CollectionCount(0);
            bool done = false;
            try
            {
                if (job.Stage == 0)
                {
                    ReadWalls(job);
                    job.Stage = 1;
                }
                if (job.Stage == 1)
                {
                    if (!Triangles(job, deadline)) return false;
                    job.Stage = 2;
                }
                if (job.Stage == 2)
                {
            // Create a cap material with per‑building colour variation
            Color capCol = GetCapColour(b);
            Surf cap = DecorPalette.Matte(capCol);
                    for (int g = 0; g < 4; g++) Caps(s_Cuts[g], s_Lists[Part(2, g, 0, job.Mats)], cap.Uv);
                    job.Stage = 3;
                    if (Stopwatch.GetTimestamp() > deadline) return false;
                }
                FinishOverlay(job);
                AbortOverlayJob();
                done = true;
                return true;
            }
            catch (System.Exception e)
            {
                Failed(b, e.GetType().Name + ": " + e.Message);
                done = true;
                return true;
            }
            finally
            {
                double ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
                job.Ms += ms;
                OverlayMs += ms;
                if (ms > OverlaySliceMaxMs && deadline != long.MaxValue)
                {
                    OverlaySliceMaxMs = ms;
                    OverlaySliceMaxFrom = from;
                    OverlaySliceMaxTo = done ? 4 : job.Stage;
                    OverlaySliceMaxVertices = job.Vertices;
                    OverlaySliceMaxGc = System.GC.CollectionCount(0) != collections;
                }
            }
        }

        // The walls mesh's vertices and every sub-mesh's triangles, the wall lines, and empty part lists.
        private static void ReadWalls(OverlayJob job)
        {
            Building b = job.B;
            Mesh src = b.WallsMesh;
            int mats = job.Mats;
            job.Slots = 8 * mats + 4;
            for (int i = 0; i < job.Slots; i++)
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
            job.Vertices = s_V.Count;
            if (s_SubStart.Length < mats)
            {
                s_SubStart = new int[mats * 2];
                s_SubCount = new int[mats * 2];
            }
            s_Idx.Clear();
            for (int m = 0; m < mats; m++)
            {
                s_SubStart[m] = s_Idx.Count;
                if (src.GetTopology(m) == MeshTopology.Triangles)
                {
                    s_Read.Clear();
                    src.GetIndices(s_Read, m, true);
                    s_Idx.AddRange(s_Read);
                }
                s_SubCount[m] = s_Idx.Count - s_SubStart[m];
            }
            job.Lines = WallLines(b);
            LineGrid(job.Lines, src.bounds);
            job.Sub = 0;
            job.Tri = 0;
        }

        // Sorts the triangles into their facing's standing walls and stub, a slice at a time: false when the deadline passed.
        private static bool Triangles(OverlayJob job, long deadline)
        {
            Building b = job.B;
            int mats = job.Mats;
            for (; job.Sub < mats; job.Sub++, job.Tri = 0)
            {
                int m = job.Sub, start = s_SubStart[m], count = s_SubCount[m];
                for (; job.Tri + 2 < count; job.Tri += 3)
                {
                    if ((job.Tri & 255) == 0 && job.Tri > 0 && Stopwatch.GetTimestamp() > deadline) return false;
                    int i0 = s_Idx[start + job.Tri], i1 = s_Idx[start + job.Tri + 1], i2 = s_Idx[start + job.Tri + 2];
                    int g = FacingOf(b, job.Lines, (s_V[i0] + s_V[i1] + s_V[i2]) * (1f / 3f));
                    List<int> standing = s_Lists[Part(0, g, m, mats)];
                    if (job.Clip) Below(i0, i1, i2, job.ClipTop, standing, null);
                    else
                    {
                        standing.Add(i0);
                        standing.Add(i1);
                        standing.Add(i2);
                    }
                    Below(i0, i1, i2, b.StubBase + StubHeight, s_Lists[Part(1, g, m, mats)], s_Cuts[g]);
                }
            }
            return true;
        }

        // The stand-in's mesh (packed straight into its buffers, as RaidMesh bakes) and object.
        private static void FinishOverlay(OverlayJob job)
        {
            Building b = job.B;
            Mesh src = b.WallsMesh;
            int slots = job.Slots, mats = job.Mats, vertices = s_V.Count;
            var map = new int[slots];
            int subs = 0, indices = 0;
            for (int i = 0; i < slots; i++)
            {
                map[i] = s_Lists[i].Count > 0 ? subs++ : -1;
                indices += s_Lists[i].Count;
            }
            if (subs == 0)
            {
                Failed(b, "it has no triangles");
                return;
            }
            if (s_Packed.Length < vertices) s_Packed = new StandInVertex[Mathf.NextPowerOfTwo(vertices)];
            for (int i = 0; i < vertices; i++)
            {
                s_Packed[i].Position = s_V[i];
                s_Packed[i].Normal = s_N[i];
                s_Packed[i].Uv = s_U[i];
            }
            var mesh = new Mesh { name = "Walls cut " + b.Id };
            mesh.SetVertexBufferParams(vertices, s_StandInLayout);
            mesh.SetVertexBufferData(s_Packed, 0, 0, vertices, 0, Quiet);
            bool wide = vertices > 65535;
            mesh.SetIndexBufferParams(indices, wide ? IndexFormat.UInt32 : IndexFormat.UInt16);
            if (wide && s_Packed32.Length < indices) s_Packed32 = new int[Mathf.NextPowerOfTwo(indices)];
            if (!wide && s_Packed16.Length < indices) s_Packed16 = new ushort[Mathf.NextPowerOfTwo(indices)];
            int k = 0;
            for (int i = 0; i < slots; i++)
            {
                List<int> list = s_Lists[i];
                for (int t = 0; t < list.Count; t++)
                {
                    if (wide) s_Packed32[k++] = list[t];
                    else s_Packed16[k++] = (ushort)list[t];
                }
            }
            if (wide) mesh.SetIndexBufferData(s_Packed32, 0, 0, indices, Quiet);
            else mesh.SetIndexBufferData(s_Packed16, 0, 0, indices, Quiet);
            Bounds bounds = src.bounds;
            mesh.subMeshCount = subs;
            var parts = new SubMeshDescriptor[subs];
            var used = new Material[subs];
            Surf cap = DecorPalette.Matte(CapColour);
            DecorPalette.Apply();
            int startAt = 0;
            for (int i = 0; i < slots; i++)
            {
                if (map[i] < 0) continue;
                int n = s_Lists[i].Count;
                parts[map[i]] = new SubMeshDescriptor(startAt, n) { bounds = bounds, firstVertex = 0, vertexCount = vertices };
                mesh.SetSubMesh(map[i], parts[map[i]], Quiet);
                used[map[i]] = i >= 8 * mats ? cap.Mat : i >= 4 * mats ? job.StubMat : job.Materials[Mathf.Min(i % mats, job.Materials.Length - 1)];
                startAt += n;
            }
            mesh.bounds = bounds;

            var go = new GameObject(mesh.name) { layer = b.Walls.gameObject.layer };
            go.transform.SetParent(b.Site.Overlays, false);
            go.transform.SetPositionAndRotation(b.Walls.transform.position, b.Walls.transform.rotation);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = used;
            mr.shadowCastingMode = ShadowCastingMode.Off;   // the walls themselves still cast every shadow
            mr.receiveShadows = true;
            mr.lightProbeUsage = b.Walls.lightProbeUsage;
            mr.reflectionProbeUsage = b.Walls.reflectionProbeUsage;
            mr.renderingLayerMask = b.Walls.renderingLayerMask;
            mr.enabled = false;
            b.Overlay = mr;
            b.OverlayMesh = mesh;
            b.OverlayParts = parts;
            b.OverlayMap = map;
            b.OverlayMaterials = mats;
            b.OverlayShown = -1;
            b.OverlayBytes = (long)vertices * 32 + (long)indices * (wide ? 4 : 2);
            OverlayBytes += b.OverlayBytes;
            OverlaysMade++;
            if (job.Ms > OverlayWholeMaxMs) OverlayWholeMaxMs = job.Ms;
            s_Overlays.Add(b);
        }
        #endregion

        #region Facings
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

        // The facing of the wall line nearest p (mesh space): every line for a simple outline, else the lines in p's cell of a
        // grid and ring after ring of cells round it until no farther cell could hold a nearer one.
        private static int FacingOf(Building b, int lines, Vector3 p)
        {
            var q = new Vector2(p.x, p.z);
            float best = float.MaxValue;
            int at = -1;
            if (lines <= BruteLines)
            {
                for (int line = 0; line < lines; line++)
                {
                    float d = LineDistance2(line, q);
                    if (d >= best) continue;
                    best = d;
                    at = line;
                }
                return at >= 0 ? b.EdgeGroup[at] : 0;
            }
            int ci = Mathf.Clamp(Mathf.FloorToInt((q.x - s_GX) / s_CW), 0, s_GW - 1);
            int cj = Mathf.Clamp(Mathf.FloorToInt((q.y - s_GZ) / s_CH), 0, s_GH - 1);
            float ring = Mathf.Min(s_CW, s_CH);
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
                            float d = LineDistance2(line, q);
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

        private static float LineDistance2(int line, Vector2 q)
        {
            Vector2 a = s_LineA[line], ab = s_LineB[line] - a;
            float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
            return (a + ab * t - q).sqrMagnitude;
        }

        // Outlines with this many walls or fewer are searched whole; more get a grid (see LineGrid).
        private const int BruteLines = 12;

        // A grid over the walls' mesh (mesh space, cells about LineCell metres), each cell listing the wall lines whose box
        // touches it: s_GridLines[s_GridStart[cell] .. s_GridStart[cell + 1]).
        private const float LineCell = 2f;
        private const int MaxLineCells = 128;   // along a side
        private static int[] s_GridStart = new int[1025], s_GridFill = new int[1024], s_GridLines = new int[1024];
        private static int s_GW, s_GH;
        private static float s_GX, s_GZ, s_CW, s_CH;

        private static void LineGrid(int lines, Bounds mesh)
        {
            if (lines <= BruteLines) return;
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
                int total = s_GridStart[cells];
                if (s_GridLines.Length < total) s_GridLines = new int[total * 2];
            }
        }
        #endregion

        // Returns a deterministic rustic brown colour for a building's stub material.
        private static Color GetStubColour(Building b)
        {
            // hash the building id into a hue‑like value (0‑1)
            uint h = (uint)(b.Id * 0x9e3779b9u);
            float t = (h & 0xFFFFu) / 65535f; // 0‑1
            // blend between two rustic browns
            return Color.Lerp(new Color(0.45f, 0.33f, 0.22f), new Color(0.55f, 0.40f, 0.28f), t);
        }

        // Returns a per‑building colour variation for the cap on cut walls.
        private static Color GetCapColour(Building b)
        {
            uint h = (uint)(b.Id * 0x85ebca6bu);
            float t = (h & 0xFFFFu) / 65535f;
            // blend between the base cap colour and a slightly darker tone
            return Color.Lerp(CapColour, new Color(0.20f, 0.18f, 0.16f), t);
        }

        #region Cutting triangles
        // The part of triangle (i0, i1, i2) below height h into `into`; with `cuts`, the points where its edges cross h (in
        // pairs, one pair per crossing) into it, for the caps.
        private static void Below(int i0, int i1, int i2, float h, List<int> into, List<Vector3> cuts)
        {
            float y0 = s_V[i0].y, y1 = s_V[i1].y, y2 = s_V[i2].y;
            if (y0 <= h && y1 <= h && y2 <= h)
            {
                into.Add(i0);
                into.Add(i1);
                into.Add(i2);
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
                into.Add(p0);
                into.Add(p1);
                into.Add(p2);
            }
            if (count == 4)
            {
                into.Add(p0);
                into.Add(p2);
                into.Add(p3);
            }
            if (cuts != null && onA >= 0 && onB >= 0)
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

        /// <summary>Add a comment for Put.</summary>
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
            Vector3 n = s_N[a] + (s_N[c] - s_N[a]) * t;
            s_V.Add(p);
            s_N.Add(n.sqrMagnitude > 1e-8f ? n.normalized : s_N[a]);
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
        #endregion
    }
}
