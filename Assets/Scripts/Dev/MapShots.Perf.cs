#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CloneZ
{
    // The streaming performance part of the map harness (the full run only): each test chunk built in Full and in Lite
    // detail; determinism across build orders; what the things a build makes cost (micro-benchmarks: colliders, meshes,
    // RaidMesh's primitives, objects, triangulation, region files); and simulated walks and drives through the state with
    // the real KentuckyStreamer, frame by frame at 60 fps, measuring what each frame costs the main thread.
    public static partial class MapShots
    {
        #region Perf report
        [Serializable]
        public sealed class PerfReport
        {
            public List<DetailPair> detail = new List<DetailPair>();
            /// <summary>A piece built, another one far off, then the first again: they must match.</summary>
            public DeterminismReport order = new DeterminismReport();
            /// <summary>A piece built in Full, then in Lite, then in Full again (an upgrade): the Fulls must match.</summary>
            public DeterminismReport afterLite = new DeterminismReport();
            /// <summary>As <see cref="order"/>, in Lite.</summary>
            public DeterminismReport liteOrder = new DeterminismReport();
            public List<CostReport> costs = new List<CostReport>();
            public StreamStart start = new StreamStart();
            public List<RouteReport> routes = new List<RouteReport>();
            public LogReport routesLog = new LogReport();
        }

        [Serializable]
        public sealed class DetailPair
        {
            public string label;
            public int cx, cz;
            /// <summary>Lite came out exactly like Full (no pass builds less in Lite yet).</summary>
            public bool identical;
            public double fullMs, liteMs, ratio, fullSlowestMs, liteSlowestMs;
            public string fullSlowest, liteSlowest;
            public long fullVertices, liteVertices;
            public int fullObjects, liteObjects, fullRenderers, liteRenderers, fullColliders, liteColliders, fullLights, liteLights;
        }

        [Serializable]
        public sealed class CostReport
        {
            public string group, name, note;
            public int n;
            public double ms, usEach;
            public bool ok = true;
        }

        [Serializable]
        public sealed class StreamStart
        {
            public bool built;
            public string error;
            /// <summary>KentuckyStreamer.BuildStart: the start square's pieces, built at once (the loading screen).</summary>
            public double ms;
            public int pieces, meshes, regions;
            public long vertices;
        }

        [Serializable]
        public sealed class RouteReport
        {
            public string name, what;
            public float seconds, speed;
            public int frames, buildFrames, steps, finished, finishedFull, finishedLite, upgraded, unloaded;
            /// <summary>The streamer's main-thread ms per frame.</summary>
            public double meanMs, p50Ms, p95Ms, p99Ms, maxMs, totalMs;
            public int over5, over8, over16, over33;
            public string longestStep;
            public double longestStepMs;
            /// <summary>Seconds until the piece under the survivor was built, and until the nine around them were Full
            /// (-1: never).</summary>
            public float ownBuiltAfter = -1f, ringAfter = -1f;
            /// <summary>Frames the survivor stood where nothing was built; frames the nine around them weren't all Full.</summary>
            public int holeFrames, ringFrames;
            public int pieces, litePieces, meshes, regions;
            public long vertices;
            public double managedMB, totalMB, pooledMB;
            /// <summary>The steps that made the slow frames (over 8 ms) slow: how often, and the worst.</summary>
            public List<StepTiming> slowest = new List<StepTiming>();
        }

        [Serializable]
        public sealed class NonFiniteReport
        {
            /// <summary>Meshes checked (each once a scan) in the square, the test chunks and the streamed walks.</summary>
            public int meshes;
            /// <summary>Meshes found in the scene with a position, normal or bounds that isn't a finite number: where they hang
            /// and what's wrong (the NavMesh bake skips such a mesh, leaving a hole).</summary>
            public List<string> offenders = new List<string>();
            /// <summary>Meshes RaidMesh found such vertices in while baking them (it baked them harmless, so they don't show in
            /// the scene): the stage, then where they hang.</summary>
            public List<string> baked = new List<string>();
        }
        #endregion

        private sealed partial class Harness
        {
            #region Lite against Full, and determinism across build orders
            // The chunk tests' pieces, each built in Full and then in Lite.
            private void DetailComparison()
            {
                ChunkParents();
                try
                {
                    foreach (Target t in PerfTargets())
                    {
                        int cx = ChunkOf(t.X), cz = ChunkOf(t.Z);
                        var pair = new DetailPair { label = t.Label, cx = cx, cz = cz };
                        // Each built three times, Full and Lite in turn: the middle run of each is the one reported (a single build
                        // can catch a garbage collection).
                        var fulls = new List<ChunkReport>(3);
                        var lites = new List<ChunkReport>(3);
                        Fingerprint pf = default, pl = default;
                        for (int run = 0; run < 3; run++)
                        {
                            fulls.Add(BuildCounted(t, cx, cz, KentuckyMap.Detail.Full, out pf));
                            lites.Add(BuildCounted(t, cx, cz, KentuckyMap.Detail.Lite, out pl));
                        }
                        fulls.Sort((a, b) => a.totalMs.CompareTo(b.totalMs));
                        lites.Sort((a, b) => a.totalMs.CompareTo(b.totalMs));
                        ChunkReport full = fulls[1], lite = lites[1];
                        pair.fullMs = full.totalMs;
                        pair.liteMs = lite.totalMs;
                        pair.ratio = full.totalMs > 0 ? Math.Round(lite.totalMs / full.totalMs, 3) : 0;
                        pair.fullSlowest = full.slowestStep;
                        pair.fullSlowestMs = full.slowestMs;
                        pair.liteSlowest = lite.slowestStep;
                        pair.liteSlowestMs = lite.slowestMs;
                        pair.fullVertices = full.counts.vertices;
                        pair.liteVertices = lite.counts.vertices;
                        pair.fullObjects = full.counts.gameObjects;
                        pair.liteObjects = lite.counts.gameObjects;
                        pair.fullRenderers = full.counts.renderers;
                        pair.liteRenderers = lite.counts.renderers;
                        pair.fullColliders = full.counts.colliders;
                        pair.liteColliders = lite.counts.colliders;
                        pair.fullLights = full.counts.lights;
                        pair.liteLights = lite.counts.lights;
                        pair.identical = pf.Unordered == pl.Unordered && pf.Other == pl.Other && pf.Objects == pl.Objects;
                        _r.perf.detail.Add(pair);
                        Note("detail " + t.Label + ": Full " + F0(pair.fullMs) + " ms, Lite " + F0(pair.liteMs) + " ms (" + F0(pair.ratio * 100) + "%)"
                            + (pair.identical ? ", identical" : ""));
                    }
                }
                finally
                {
                    DropChunkParents();
                }
            }

            private List<Target> PerfTargets()
            {
                var targets = new List<Target> { new Target { Label = "start-centre", Place = _place.name, X = _place.x, Z = _place.z } };
                Target rural = Rural();
                if (rural != null) targets.Add(rural);
                AddPlace(targets, "louisville-downtown", "Louisville");
                AddPlace(targets, "lexington-downtown", "Lexington");
                AddPlace(targets, "hazard", "Hazard");
                return targets;
            }

            // One piece built (warm: its region is read already) and counted, then unloaded.
            private ChunkReport BuildCounted(Target t, int cx, int cz, KentuckyMap.Detail detail, out Fingerprint print)
            {
                var rep = new ChunkReport { label = t.Label, place = t.Place, cx = cx, cz = cz, detail = detail.ToString() };
                var middle = new Vector3((float)((cx + 0.5) * MapApi.ChunkSize - _place.x), 0f, (float)((cz + 0.5) * MapApi.ChunkSize - _place.z));
                MapApi.Chunk c;
                Begin(rep.log);
                try
                {
                    c = MapApi.BuildChunk(_build, _place.name, cx, cz, _zone.LayoutSeed, middle, _streamed, _overheadRoot, detail);
                }
                finally
                {
                    End();
                }
                try
                {
                    if (c.Failed)
                    {
                        _mapErrors++;
                        rep.failed = true;
                        rep.error = c.Error;
                    }
                    rep.totalMs = Math.Round(c.TotalMs, 2);
                    foreach (StepTiming s in c.Steps)
                        if (s.ms > rep.slowestMs)
                        {
                            rep.slowestMs = Math.Round(s.ms, 2);
                            rep.slowestStep = s.name;
                        }
                    rep.counts = Count(t.Label, new[] { c.Root.transform, c.Overhead.transform });
                    ScanNonFinite("chunk " + t.Label + " (" + detail + ")", new[] { c.Root.transform, c.Overhead.transform });
                    print = Print(c.Root.transform, c.Overhead.transform);
                    return rep;
                }
                finally
                {
                    MapApi.Unload(c);
                }
            }

            // Bardstown's middle piece, then a Louisville one, then Bardstown's again: a piece must come out the same whatever
            // was built before it (each machine in a co-op raid builds its pieces in its own order). Then Lite and Full again
            // (an upgrade), and the same order test in Lite.
            private void OrderDeterminism()
            {
                int ax = ChunkOf(_place.x), az = ChunkOf(_place.z);
                MapPlace lou = _build.FindPlace("Louisville");
                int bx = lou != null ? ChunkOf(lou.x) : ax + 5, bz = lou != null ? ChunkOf(lou.z) : az;
                PerfReport p = _r.perf;
                ChunkParents();
                try
                {
                    Fingerprint a1 = BuildPrint(ax, az, KentuckyMap.Detail.Full);
                    BuildPrint(bx, bz, KentuckyMap.Detail.Full);
                    Fingerprint a2 = BuildPrint(ax, az, KentuckyMap.Detail.Full);
                    Compare(p.order, a1, a2);
                    Label(p.order, "start-centre, Full, with a Louisville piece built in between", ax, az);
                    BuildPrint(ax, az, KentuckyMap.Detail.Lite);
                    Fingerprint a3 = BuildPrint(ax, az, KentuckyMap.Detail.Full);
                    Compare(p.afterLite, a1, a3);
                    Label(p.afterLite, "start-centre, Full, then Lite, then Full again (an upgrade)", ax, az);
                    Fingerprint l1 = BuildPrint(ax, az, KentuckyMap.Detail.Lite);
                    BuildPrint(bx, bz, KentuckyMap.Detail.Lite);
                    Fingerprint l2 = BuildPrint(ax, az, KentuckyMap.Detail.Lite);
                    Compare(p.liteOrder, l1, l2);
                    Label(p.liteOrder, "start-centre, Lite, with a Louisville piece built in between", ax, az);
                    foreach (DeterminismReport d in new[] { p.order, p.afterLite, p.liteOrder })
                    {
                        Note("determinism (" + d.label + "): " + (d.match ? "" : "MISMATCH: ") + d.detail);
                        if (!d.match) Remark("Determinism (" + d.label + "): " + d.detail + "; first: " + string.Join("; ", d.differences));
                    }
                }
                finally
                {
                    DropChunkParents();
                }
            }

            private static void Label(DeterminismReport d, string label, int cx, int cz)
            {
                d.label = label;
                d.cx = cx;
                d.cz = cz;
            }
            #endregion
        }

        private struct BoxSpec
        {
            public Vector3 C, S;
            public float Yaw;
        }

        // RaidMesh's primitives as they were before the revamp made them fast (List storage, a rotation per corner), kept
        // only to check the fast ones draw exactly the same.
        private sealed class LegacyMesh
        {
            public readonly List<Vector3> V = new List<Vector3>(), N = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<Material> Mats = new List<Material>();
            public readonly List<List<int>> Tris = new List<List<int>>();
            private Vector3 _origin;
            private Quaternion _rot = Quaternion.identity;

            private static readonly Vector3[] s_FaceN = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

            public void SetFrame(Vector3 origin, float yaw) { _origin = origin; _rot = Quaternion.Euler(0f, yaw, 0f); }
            public void ResetFrame() { _origin = Vector3.zero; _rot = Quaternion.identity; }

            private List<int> TrisFor(Material m)
            {
                for (int i = 0; i < Mats.Count; i++) if (Mats[i] == m) return Tris[i];
                Mats.Add(m);
                var list = new List<int>();
                Tris.Add(list);
                return list;
            }

            private static Vector2 UvAt(Vector3 p, Vector3 n, in Surf s)
            {
                if (!s.Planar) return s.Uv;
                float k = s.PerMeter;
                p += RaidMesh.PlanarOrigin;
                if (n.y > 0.7f || n.y < -0.7f) return new Vector2(p.x * k, p.z * k);
                Vector3 t = Vector3.Cross(n, Vector3.up);
                float len = t.magnitude;
                if (len < 1e-4f) return new Vector2(p.x * k, p.z * k);
                t /= len;
                return new Vector2(Vector3.Dot(p, t) * k, p.y * k);
            }

            private int Add(Vector3 p, Vector3 n, in Surf s) { V.Add(p); N.Add(n); Uv.Add(UvAt(p, n, s)); return V.Count - 1; }
            private int Add(Vector3 p, Vector3 n, Vector2 uv) { V.Add(p); N.Add(n); Uv.Add(uv); return V.Count - 1; }

            public void Box(Vector3 center, Vector3 size, Quaternion localRot, in Surf s, bool skipBottom = false)
            {
                Quaternion rot = _rot * localRot;
                Vector3 c = _origin + _rot * center;
                List<int> tris = TrisFor(s.Mat);
                for (int f = 0; f < 6; f++)
                {
                    if (skipBottom && f == 1) continue;
                    Vector3 n = s_FaceN[f], u = new Vector3(n.y, n.z, n.x), v = Vector3.Cross(n, u), wn = rot * n;
                    Vector3 c0 = Vector3.Scale((n - u - v) * 0.5f, size), c1 = Vector3.Scale((n + u - v) * 0.5f, size);
                    Vector3 c2 = Vector3.Scale((n + u + v) * 0.5f, size), c3 = Vector3.Scale((n - u + v) * 0.5f, size);
                    int b = Add(c + rot * c0, wn, s);
                    Add(c + rot * c1, wn, s);
                    Add(c + rot * c2, wn, s);
                    Add(c + rot * c3, wn, s);
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                }
            }

            public void Quad(Vector3 center, Vector3 halfRight, Vector3 halfUp, in Surf s)
            {
                Vector3 c = _origin + _rot * center, r = _rot * halfRight, u = _rot * halfUp, n = Vector3.Cross(u, r).normalized;
                List<int> tris = TrisFor(s.Mat);
                int b = Add(c - r - u, n, s);
                Add(c - r + u, n, s);
                Add(c + r + u, n, s);
                Add(c + r - u, n, s);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }

            public void QuadUV(Vector3 center, Vector3 halfRight, Vector3 halfUp, in Surf s, Rect uv)
            {
                Vector3 c = _origin + _rot * center, r = _rot * halfRight, u = _rot * halfUp, n = Vector3.Cross(u, r).normalized;
                List<int> tris = TrisFor(s.Mat);
                int b = Add(c - r - u, n, new Vector2(uv.xMin, uv.yMin));
                Add(c - r + u, n, new Vector2(uv.xMin, uv.yMax));
                Add(c + r + u, n, new Vector2(uv.xMax, uv.yMax));
                Add(c + r - u, n, new Vector2(uv.xMax, uv.yMin));
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, in Surf s)
            {
                Vector3 wa = _origin + _rot * a, wb = _origin + _rot * b, wc = _origin + _rot * c;
                Vector3 n = Vector3.Cross(wb - wa, wc - wa).normalized;
                List<int> tris = TrisFor(s.Mat);
                int i0 = Add(wa, n, s);
                Add(wb, n, s);
                Add(wc, n, s);
                tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
            }

            public void Diamond(Vector3 center, Vector3 right, Vector3 up, in Surf s)
            {
                Vector3 c = _origin + _rot * center, r = _rot * right, u = _rot * up, n = Vector3.Cross(u, r).normalized;
                List<int> tris = TrisFor(s.Mat);
                int top = Add(c + u, n, s), rt = Add(c + r, n, s), bot = Add(c - u, n, s), lf = Add(c - r, n, s);
                tris.Add(top); tris.Add(rt); tris.Add(bot);
                tris.Add(top); tris.Add(bot); tris.Add(lf);
            }

            public void Blob(Vector3 center, Vector3 right, Vector3 up, float radiusX, float radiusY, int points, float jag, System.Random rng, in Surf s)
            {
                points = Mathf.Clamp(points, 5, 24);
                Vector3 c = _origin + _rot * center, r = _rot * right, u = _rot * up, n = Vector3.Cross(u, r).normalized;
                List<int> tris = TrisFor(s.Mat);
                int ci = Add(c, n, s), first = V.Count;
                for (int i = 0; i < points; i++)
                {
                    float a = i * Mathf.PI * 2f / points;
                    float k = 1f - jag * (float)rng.NextDouble();
                    Add(c + r * (Mathf.Sin(a) * radiusX * k) + u * (Mathf.Cos(a) * radiusY * k), n, s);
                }
                for (int i = 0; i < points; i++) { tris.Add(ci); tris.Add(first + i); tris.Add(first + (i + 1) % points); }
            }

            public void Cylinder(Vector3 baseCenter, float radius, float height, int sides, in Surf s, Quaternion localRot, float topScale, bool caps)
            {
                sides = Mathf.Clamp(sides, 3, 24);
                Quaternion rot = _rot * localRot;
                Vector3 b0 = _origin + _rot * baseCenter, up = rot * Vector3.up, top = b0 + up * height;
                float rTop = radius * topScale;
                List<int> tris = TrisFor(s.Mat);
                int wall = V.Count;
                float slope = (radius - rTop) / Mathf.Max(0.001f, height);
                for (int i = 0; i <= sides; i++)
                {
                    float yaw = (float)i / sides * Mathf.PI * 2f;
                    Vector3 dir = rot * new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
                    Vector3 nrm = (dir + up * slope).normalized;
                    Add(top + dir * rTop, nrm, s);
                    Add(b0 + dir * radius, nrm, s);
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
                    int tc = Add(top, up, s), ring = V.Count;
                    for (int i = 0; i <= sides; i++)
                    {
                        float yaw = (float)i / sides * Mathf.PI * 2f;
                        Add(top + rot * new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)) * rTop, up, s);
                    }
                    for (int i = 0; i < sides; i++) { tris.Add(tc); tris.Add(ring + i); tris.Add(ring + i + 1); }
                }
                int bc = Add(b0, -up, s), bring = V.Count;
                for (int i = 0; i <= sides; i++)
                {
                    float yaw = (float)i / sides * Mathf.PI * 2f;
                    Add(b0 + rot * new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)) * radius, -up, s);
                }
                for (int i = 0; i < sides; i++) { tris.Add(bc); tris.Add(bring + i + 1); tris.Add(bring + i); }
            }

            public void Ellipsoid(Vector3 center, Vector3 size, Quaternion localRot, in Surf s, int segments, int rings)
            {
                Quaternion rot = _rot * localRot;
                Vector3 c = _origin + _rot * center;
                List<int> tris = TrisFor(s.Mat);
                int start = V.Count;
                var inv = new Vector3(1f / Mathf.Max(0.001f, size.x), 1f / Mathf.Max(0.001f, size.y), 1f / Mathf.Max(0.001f, size.z));
                for (int y = 0; y <= rings; y++)
                {
                    float pitch = (float)y / rings * Mathf.PI, sy = Mathf.Cos(pitch), r = Mathf.Sin(pitch);
                    for (int x = 0; x <= segments; x++)
                    {
                        float yaw = (float)x / segments * Mathf.PI * 2f;
                        var unit = new Vector3(Mathf.Sin(yaw) * r, sy, Mathf.Cos(yaw) * r);
                        Vector3 p = Vector3.Scale(unit * 0.5f, size);
                        Vector3 n = rot * Vector3.Scale(unit, inv).normalized;
                        Add(c + rot * p, n, s);
                    }
                }
                for (int y = 0; y < rings; y++)
                    for (int x = 0; x < segments; x++)
                    {
                        int a = start + y * (segments + 1) + x, b = a + segments + 1;
                        tris.Add(a); tris.Add(b); tris.Add(a + 1);
                        tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                    }
            }
        }

        private sealed partial class Harness
        {
            #region What things cost
            private void Costs()
            {
                bool rig = _rig != null && _rig.activeSelf;
                if (_rig != null) _rig.SetActive(false);   // the stand-in survivor's collider mustn't catch the test rays
                var root = new GameObject("[MapShots] costs");
                try
                {
                    Fence("equivalence", CostEquivalence);
                    Fence("colliders", () => CostColliders(root.transform));
                    Fence("meshes", () => CostMeshes(root.transform));
                    Fence("primitives", CostPrimitives);
                    Fence("objects", () => CostObjects(root.transform));
                    Fence("triangulation", CostTriangulation);
                    Fence("regions", CostRegions);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                    if (_rig != null) _rig.SetActive(rig);
                }
            }

            private void Fence(string group, Action act)
            {
                try
                {
                    act();
                }
                catch (Exception e)
                {
                    _r.perf.costs.Add(new CostReport { group = group, name = "(failed)", ok = false, note = Brief(e) });
                    Remark("Cost test " + group + " failed: " + Brief(e));
                }
            }

            private CostReport AddCost(string group, string name, int n, double ms, string note = null)
            {
                var c = new CostReport { group = group, name = name, n = n, ms = Math.Round(ms, 3), usEach = n > 0 ? Math.Round(ms * 1000.0 / n, 3) : 0, note = note };
                _r.perf.costs.Add(c);
                return c;
            }

            // 1,600 wall-like boxes turned with eight buildings, and 400 posts square to the grid: as RaidTown.PropCollider
            // makes them today (an object for every turned box), grouped on an object per angle, as one MeshCollider cooked
            // on the main thread, and as one baked on a worker first (Physics.BakeMesh). Each: making them (under an inactive
            // root), switching them on, 3,000 rays (which must hit alike), and destroying them.
            private void CostColliders(Transform parent)
            {
                List<BoxSpec> boxes = WallSet();
                var opts = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning
                    | MeshColliderCookingOptions.WeldColocatedVertices | MeshColliderCookingOptions.UseFastMidphase;
                for (int strategy = 0; strategy < 4; strategy++)
                {
                    string name = strategy == 0 ? "an object per turned box (the procedural town's way)" : strategy == 1 ? "grouped: an object per angle (the Kentucky map's way)"
                        : strategy == 2 ? "one MeshCollider, cooked on the main thread" : "one MeshCollider, baked on a worker";
                    var root = new GameObject("colliders " + strategy);
                    root.SetActive(false);
                    root.transform.SetParent(parent, false);
                    int objects = 1;
                    Mesh mesh = null;
                    double workerMs = 0;
                    var sw = Stopwatch.StartNew();
                    if (strategy == 0) objects += CollidersToday(boxes, root.transform);
                    else if (strategy == 1) objects += CollidersGrouped(boxes, root.transform);
                    else
                    {
                        mesh = BoxesMesh(boxes);
                        if (strategy == 3)
                        {
                            EntityId id = mesh.GetEntityId();
                            var worker = Stopwatch.StartNew();
                            Task.Run(() => Physics.BakeMesh(id, false, opts)).Wait();
                            workerMs = worker.Elapsed.TotalMilliseconds;
                        }
                        var go = new GameObject("MeshCollider");
                        go.transform.SetParent(root.transform, false);
                        var mc = go.AddComponent<MeshCollider>();
                        mc.cookingOptions = opts;
                        mc.sharedMesh = mesh;
                        objects++;
                    }
                    double make = sw.Elapsed.TotalMilliseconds - workerMs;
                    sw.Restart();
                    root.SetActive(true);
                    double activate = sw.Elapsed.TotalMilliseconds;
                    Physics.SyncTransforms();
                    sw.Restart();
                    CastRays(3000, out int hits, out double distance);
                    double rays = sw.Elapsed.TotalMilliseconds;
                    sw.Restart();
                    Object.DestroyImmediate(root);
                    if (mesh != null) Object.DestroyImmediate(mesh);
                    double destroy = sw.Elapsed.TotalMilliseconds;
                    AddCost("colliders", name, boxes.Count, make + activate, "make " + F2(make) + " + switch on " + F2(activate) + " ms"
                        + (workerMs > 0 ? " (+ " + F2(workerMs) + " ms baking on the worker)" : "") + "; " + objects + " objects; destroy "
                        + F2(destroy) + " ms; 3,000 rays " + F2(rays) + " ms, " + hits + " hits, " + distance.ToString("0.0", CultureInfo.InvariantCulture) + " m");
                }
            }

            private static List<BoxSpec> WallSet()
            {
                var list = new List<BoxSpec>(2000);
                var rng = new System.Random(19930117);
                for (int b = 0; b < 8; b++)
                {
                    float yaw = 5f + (float)rng.NextDouble() * 80f;
                    Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                    var o = new Vector3((float)rng.NextDouble() * 100f - 50f, 0f, (float)rng.NextDouble() * 100f - 50f);
                    for (int i = 0; i < 200; i++)
                    {
                        Vector3 c = o + q * new Vector3((float)rng.NextDouble() * 24f - 12f, 1.5f, (float)rng.NextDouble() * 24f - 12f);
                        list.Add(new BoxSpec { C = c, S = new Vector3(1f + (float)rng.NextDouble() * 5f, 3f, 0.22f), Yaw = (i & 1) == 0 ? yaw : yaw + 90f });
                    }
                }
                for (int i = 0; i < 400; i++)
                    list.Add(new BoxSpec
                    {
                        C = new Vector3((float)rng.NextDouble() * 120f - 60f, 0.5f, (float)rng.NextDouble() * 120f - 60f),
                        S = new Vector3(0.4f, 1f, 0.6f), Yaw = (i % 4) * 90f,
                    });
                return list;
            }

            // RaidTown.PropCollider as it is today.
            private static int CollidersToday(List<BoxSpec> boxes, Transform parent)
            {
                var shared = new GameObject("PropColliders");
                shared.transform.SetParent(parent, false);
                int objects = 1;
                foreach (BoxSpec b in boxes)
                {
                    float k = Mathf.Repeat(b.Yaw, 90f);
                    if (k < 1f || k > 89f)
                    {
                        bool swap = Mathf.Abs(Mathf.Repeat(b.Yaw, 180f) - 90f) < 45f;
                        var bc = shared.AddComponent<BoxCollider>();
                        bc.center = b.C;
                        bc.size = swap ? new Vector3(b.S.z, b.S.y, b.S.x) : b.S;
                    }
                    else
                    {
                        var go = new GameObject("PropCollider");
                        go.transform.SetParent(shared.transform, false);
                        go.transform.SetPositionAndRotation(b.C, Quaternion.Euler(0f, b.Yaw, 0f));
                        go.AddComponent<BoxCollider>().size = b.S;
                        objects++;
                    }
                }
                return objects;
            }

            // The same boxes on one object per angle (to a hundredth of a degree, turned a quarter at most).
            private static int CollidersGrouped(List<BoxSpec> boxes, Transform parent)
            {
                var groups = new Dictionary<int, Transform>();
                int objects = 0;
                foreach (BoxSpec b in boxes)
                {
                    float r = Mathf.Repeat(b.Yaw, 90f);
                    int key;
                    bool swap;
                    if (r < 1f || r > 89f)
                    {
                        key = 0;
                        swap = Mathf.Abs(Mathf.Repeat(b.Yaw, 180f) - 90f) < 45f;
                    }
                    else
                    {
                        key = Mathf.RoundToInt(r * 100f);
                        swap = (Mathf.FloorToInt(b.Yaw / 90f) & 1) != 0;
                    }
                    if (!groups.TryGetValue(key, out Transform g))
                    {
                        var go = new GameObject("Colliders");
                        go.transform.SetParent(parent, false);
                        go.transform.localRotation = Quaternion.Euler(0f, key / 100f, 0f);
                        groups[key] = g = go.transform;
                        objects++;
                    }
                    var bc = g.gameObject.AddComponent<BoxCollider>();
                    bc.center = Quaternion.Inverse(g.localRotation) * b.C;
                    bc.size = swap ? new Vector3(b.S.z, b.S.y, b.S.x) : b.S;
                }
                return objects;
            }

            // Corner bits: 1 = +x, 2 = +y, 4 = +z; each face's corners round its edge.
            private static readonly int[] s_BoxFaces = { 1, 3, 7, 5, 0, 4, 6, 2, 2, 6, 7, 3, 0, 1, 5, 4, 4, 5, 7, 6, 0, 2, 3, 1 };

            // The boxes as one collision mesh: eight corners and twelve outward-facing triangles each.
            private static Mesh BoxesMesh(List<BoxSpec> boxes)
            {
                var v = new List<Vector3>(boxes.Count * 8);
                var t = new List<int>(boxes.Count * 36);
                foreach (BoxSpec b in boxes)
                {
                    Quaternion q = Quaternion.Euler(0f, b.Yaw, 0f);
                    Vector3 ax = q * new Vector3(b.S.x * 0.5f, 0f, 0f), ay = new Vector3(0f, b.S.y * 0.5f, 0f), az = q * new Vector3(0f, 0f, b.S.z * 0.5f);
                    int i0 = v.Count;
                    for (int k = 0; k < 8; k++)
                        v.Add(b.C + ((k & 1) != 0 ? ax : -ax) + ((k & 2) != 0 ? ay : -ay) + ((k & 4) != 0 ? az : -az));
                    for (int f = 0; f < 6; f++)
                    {
                        int a = i0 + s_BoxFaces[f * 4], bb = i0 + s_BoxFaces[f * 4 + 1], c = i0 + s_BoxFaces[f * 4 + 2], d = i0 + s_BoxFaces[f * 4 + 3];
                        Vector3 outward = (v[a] + v[bb] + v[c] + v[d]) * 0.25f - b.C;
                        if (Vector3.Dot(Vector3.Cross(v[bb] - v[a], v[c] - v[a]), outward) < 0f)
                        {
                            int s = bb;
                            bb = d;
                            d = s;
                        }
                        t.Add(a); t.Add(bb); t.Add(c);
                        t.Add(a); t.Add(c); t.Add(d);
                    }
                }
                var mesh = new Mesh { name = "colliders", indexFormat = v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(v);
                mesh.SetTriangles(t, 0);
                return mesh;
            }

            private static void CastRays(int n, out int hits, out double distance)
            {
                var rng = new System.Random(7);
                hits = 0;
                distance = 0;
                for (int i = 0; i < n; i++)
                {
                    var o = new Vector3((float)rng.NextDouble() * 140f - 70f, i % 3 == 0 ? 0.3f : 1.2f, (float)rng.NextDouble() * 140f - 70f);
                    float a = (float)(rng.NextDouble() * Math.PI * 2.0);
                    var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    if (!Physics.Raycast(o, d, out RaycastHit h, 12f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    hits++;
                    distance += h.distance;
                }
            }

            private static string F2(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);

            // Forty batches of 300 boxes (7,200 vertices, three materials), as a piece's outdoor batches: baked one by one as
            // RaidMesh.Emit does today, and all at once through MeshBake (Unity's MeshData API) packed on the main thread and
            // on a worker. The meshes must come out alike.
            private void CostMeshes(Transform parent)
            {
                const int batches = 40, boxes = 300;
                var sources = new List<RaidMesh>(batches);
                Surf matte = DecorPalette.Matte(new Color(0.55f, 0.5f, 0.45f)), gloss = DecorPalette.Gloss(new Color(0.2f, 0.3f, 0.4f));
                Surf snow = DecorMaterials.Snow;
                var rng = new System.Random(5);
                var origins = new List<Vector3>(batches);
                for (int i = 0; i < batches; i++)
                {
                    var m = new RaidMesh();
                    var o = new Vector3((i % 8) * 35f, 0f, (i / 8) * 35f);
                    origins.Add(o);
                    for (int k = 0; k < boxes; k++)
                    {
                        var c = o + new Vector3((float)rng.NextDouble() * 35f, (float)rng.NextDouble() * 6f, (float)rng.NextDouble() * 35f);
                        m.Box(c, new Vector3(0.2f + (float)rng.NextDouble() * 3f, 0.2f + (float)rng.NextDouble() * 3f, 0.2f), (float)rng.NextDouble() * 360f,
                            k % 3 == 0 ? matte : k % 3 == 1 ? gloss : snow);
                    }
                    sources.Add(m);
                }
                long vertices = 0;
                foreach (RaidMesh m in sources) vertices += m.VertexCount;
                List<Mesh> outer = RaidMesh.CreatedMeshes;
                var today = new List<Mesh>();
                var baked = new List<Mesh>();
                var root = new GameObject("meshes");
                root.SetActive(false);
                root.transform.SetParent(parent, false);
                try
                {
                    RaidMesh.CreatedMeshes = today;
                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < batches; i++) sources[i].Emit("Batch", root.transform, origins[i]);
                    double emit = sw.Elapsed.TotalMilliseconds;
                    RaidMesh.CreatedMeshes = outer;
                    AddCost("meshes", "today: RaidMesh.Emit, one by one", batches, emit, F0(vertices) + " vertices; GameObjects, components and meshes");

                    for (int run = 0; run < 2; run++)
                    {
                        bool worker = run == 1;
                        var bake = new MeshBake();
                        sw.Restart();
                        var made = new List<Mesh>(batches);
                        for (int i = 0; i < batches; i++)
                        {
                            var go = new GameObject("Batch");
                            go.transform.SetParent(root.transform, false);
                            go.transform.position = origins[i];
                            var mesh = new Mesh { name = "Batch" };
                            made.Add(mesh);
                            go.AddComponent<MeshFilter>().sharedMesh = mesh;
                            var mr = go.AddComponent<MeshRenderer>();
                            mr.sharedMaterials = sources[i].Materials;
                            bake.Add(sources[i], origins[i], mesh);
                        }
                        double objects = sw.Elapsed.TotalMilliseconds;
                        sw.Restart();
                        bake.Allocate();
                        double allocate = sw.Elapsed.TotalMilliseconds, pack;
                        try
                        {
                            if (worker)
                            {
                                var w = Stopwatch.StartNew();
                                Task.Run(bake.Pack).Wait();
                                pack = w.Elapsed.TotalMilliseconds;
                            }
                            else
                            {
                                sw.Restart();
                                bake.Pack();
                                pack = sw.Elapsed.TotalMilliseconds;
                            }
                        }
                        catch
                        {
                            bake.Dispose();
                            baked.AddRange(made);
                            throw;
                        }
                        sw.Restart();
                        bake.Apply();
                        double apply = sw.Elapsed.TotalMilliseconds;
                        baked.AddRange(made);
                        double main = objects + allocate + apply + (worker ? 0 : pack);
                        AddCost("meshes", worker ? "MeshBake, packed on a worker" : "MeshBake, packed on the main thread", batches, main,
                            "objects " + F2(objects) + ", allocate " + F2(allocate) + ", pack " + F2(pack) + (worker ? " (worker)" : "") + ", apply " + F2(apply)
                            + " ms" + (run == 0 ? "; same as today's: " + SameMeshes(today, made) : ""));
                    }
                }
                finally
                {
                    RaidMesh.CreatedMeshes = outer;
                    Object.DestroyImmediate(root);
                    foreach (Mesh m in today) Object.DestroyImmediate(m);
                    foreach (Mesh m in baked) Object.DestroyImmediate(m);
                }
            }

            // "yes" when every mesh has the same vertices, normals, UVs, triangles and bounds as its twin.
            private static string SameMeshes(List<Mesh> a, List<Mesh> b)
            {
                if (a.Count != b.Count) return "no (" + a.Count + " vs " + b.Count + " meshes)";
                var va = new List<Vector3>();
                var vb = new List<Vector3>();
                var ua = new List<Vector2>();
                var ub = new List<Vector2>();
                var ia = new List<int>();
                var ib = new List<int>();
                for (int i = 0; i < a.Count; i++)
                {
                    Mesh x = a[i], y = b[i];
                    if (x.vertexCount != y.vertexCount || x.subMeshCount != y.subMeshCount) return "no (mesh " + i + "'s counts)";
                    x.GetVertices(va);
                    y.GetVertices(vb);
                    for (int k = 0; k < va.Count; k++) if ((va[k] - vb[k]).sqrMagnitude > 1e-10f) return "no (mesh " + i + "'s vertices)";
                    x.GetNormals(va);
                    y.GetNormals(vb);
                    for (int k = 0; k < va.Count; k++) if ((va[k] - vb[k]).sqrMagnitude > 1e-10f) return "no (mesh " + i + "'s normals)";
                    x.GetUVs(0, ua);
                    y.GetUVs(0, ub);
                    for (int k = 0; k < ua.Count; k++) if ((ua[k] - ub[k]).sqrMagnitude > 1e-10f) return "no (mesh " + i + "'s UVs)";
                    for (int s = 0; s < x.subMeshCount; s++)
                    {
                        x.GetTriangles(ia, s);
                        y.GetTriangles(ib, s);
                        if (ia.Count != ib.Count) return "no (mesh " + i + "'s triangles)";
                        for (int k = 0; k < ia.Count; k++) if (ia[k] != ib[k]) return "no (mesh " + i + "'s triangles)";
                    }
                    if ((x.bounds.center - y.bounds.center).sqrMagnitude > 1e-6f || (x.bounds.size - y.bounds.size).sqrMagnitude > 1e-6f)
                        return "no (mesh " + i + "'s bounds)";
                }
                return "yes";
            }

            // RaidMesh's primitives, each many times into a fresh mesh; and a few the way they were drawn before.
            private void CostPrimitives()
            {
                Surf matte = DecorPalette.Matte(new Color(0.5f, 0.5f, 0.5f)), snow = DecorMaterials.Snow;
                var at = new Vector3(3.2f, 1.1f, -7.4f);
                var size = new Vector3(1.2f, 0.4f, 0.3f);
                Quaternion turned = Quaternion.Euler(0f, 33f, 0f);
                Prim("Box, turned, flat colour", 20000, m => m.Box(at, size, 33f, matte));
                PrimOld("before: Box, turned, flat colour", 20000, m => m.Box(at, size, turned, matte));
                Prim("Box, turned, planar texture", 20000, m => m.Box(at, size, 33f, snow));
                PrimOld("before: Box, turned, planar texture", 20000, m => m.Box(at, size, turned, snow));
                Prim("Box, square, flat colour", 20000, m => m.Box(at, size, matte));
                Prim("Box in a turned frame (furniture)", 20000, m => { m.SetFrame(at, 21f); m.Box(new Vector3(0.3f, 0.2f, 0.1f), size, matte); });
                Prim("Quad (Upright)", 20000, m => m.Upright(at, new Vector3(0.6f, 0f, 0.8f), 1f, 1.2f, matte));
                Prim("Tri", 20000, m => m.Tri(at, at + Vector3.forward, at + Vector3.right, matte));
                Prim("Cylinder, 8 sides", 5000, m => m.Cylinder(at, 0.2f, 2f, 8, matte));
                PrimOld("before: Cylinder, 8 sides", 5000, m => m.Cylinder(at, 0.2f, 2f, 8, matte, Quaternion.identity, 1f, true));
                Prim("Ellipsoid, 8 x 5", 3000, m => m.Ellipsoid(at, size, matte));
                PrimOld("before: Ellipsoid, 8 x 5", 3000, m => m.Ellipsoid(at, size, Quaternion.identity, matte, 8, 5));
            }

            private void PrimOld(string name, int n, Action<LegacyMesh> draw)
            {
                var warm = new LegacyMesh();
                for (int i = 0; i < 50; i++) draw(warm);
                var m = new LegacyMesh();
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < n; i++) draw(m);
                AddCost("primitives", name, n, sw.Elapsed.TotalMilliseconds, F0(m.V.Count) + " vertices");
            }

            // The fast RaidMesh against the old primitives, call for call: thousands of random primitives of every kind (in
            // turned frames, flat colours and planar textures) must draw the same vertices, normals, UVs, triangles and
            // materials.
            private void CostEquivalence()
            {
                var rng = new System.Random(1993);
                var mesh = new RaidMesh();
                var old = new LegacyMesh();
                Surf[] surfs = { DecorPalette.Matte(new Color(0.3f, 0.6f, 0.2f)), DecorPalette.Gloss(new Color(0.7f, 0.1f, 0.1f)),
                    DecorMaterials.Snow, DecorMaterials.Concrete };
                Vector3 planar = RaidMesh.PlanarOrigin;
                RaidMesh.PlanarOrigin = new Vector3(37.5f, 0f, 121.25f);
                const int calls = 4000;
                try
                {
                    for (int i = 0; i < calls; i++)
                    {
                        Surf s = surfs[rng.Next(surfs.Length)];
                        Vector3 a = R3(rng, 40f), b = R3(rng, 3f), c = R3(rng, 3f);
                        var size = new Vector3(0.02f + (float)rng.NextDouble() * 5f, 0.02f + (float)rng.NextDouble() * 5f, 0.02f + (float)rng.NextDouble() * 5f);
                        Quaternion q = rng.Next(3) == 0 ? Quaternion.Euler(R(rng, 360f), R(rng, 360f), R(rng, 360f)) : Quaternion.Euler(0f, R(rng, 720f), 0f);
                        int frame = rng.Next(6);
                        if (frame == 0)
                        {
                            Vector3 fo = R3(rng, 30f);
                            float fy = R(rng, 360f);
                            mesh.SetFrame(fo, fy);
                            old.SetFrame(fo, fy);
                        }
                        else if (frame == 1)
                        {
                            mesh.ResetFrame();
                            old.ResetFrame();
                        }
                        switch (rng.Next(8))
                        {
                            case 0:
                                bool skip = rng.Next(4) == 0;
                                mesh.Box(a, size, q, s, skip);
                                old.Box(a, size, q, s, skip);
                                break;
                            case 1:
                                mesh.Quad(a, b, c, s);
                                old.Quad(a, b, c, s);
                                break;
                            case 2:
                                var uv = new Rect((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
                                mesh.QuadUV(a, b, c, s, uv);
                                old.QuadUV(a, b, c, s, uv);
                                break;
                            case 3:
                                mesh.Tri(a, a + b, a + c, s);
                                old.Tri(a, a + b, a + c, s);
                                break;
                            case 4:
                                mesh.Diamond(a, b, c, s);
                                old.Diamond(a, b, c, s);
                                break;
                            case 5:
                                int seed = rng.Next(), points = 3 + rng.Next(24);
                                float rx = 0.1f + (float)rng.NextDouble() * 3f, ry = 0.1f + (float)rng.NextDouble() * 3f, jag = (float)rng.NextDouble() * 0.5f;
                                mesh.Blob(a, b.normalized, c.normalized, rx, ry, points, jag, new System.Random(seed), s);
                                old.Blob(a, b.normalized, c.normalized, rx, ry, points, jag, new System.Random(seed), s);
                                break;
                            case 6:
                                int sides = 1 + rng.Next(26);
                                float radius = 0.02f + (float)rng.NextDouble(), height = 0.05f + (float)rng.NextDouble() * 6f;
                                float top = rng.Next(5) == 0 ? 0f : (float)rng.NextDouble() * 1.5f;
                                bool caps = rng.Next(4) != 0;
                                mesh.Cylinder(a, radius, height, sides, s, q, top, caps);
                                old.Cylinder(a, radius, height, sides, s, q, top, caps);
                                break;
                            default:
                                int segments = 3 + rng.Next(12), rings = 2 + rng.Next(8);
                                mesh.Ellipsoid(a, size, q, s, segments, rings);
                                old.Ellipsoid(a, size, q, s, segments, rings);
                                break;
                        }
                    }
                }
                finally
                {
                    RaidMesh.PlanarOrigin = planar;
                }
                var v = new List<Vector3>();
                var n = new List<Vector3>();
                var uvs = new List<Vector2>();
                var idx = new List<int>();
                var mats = new List<Material>();
                mesh.CopyGeometry(v, n, uvs, idx, mats);
                var oldIdx = new List<int>();
                foreach (List<int> t in old.Tris)
                {
                    oldIdx.AddRange(t);
                    oldIdx.Add(-1);
                }
                int exactV = 0, exactN = 0, exactUv = 0;
                double dv = 0, dn = 0, duv = 0;
                int count = Math.Min(v.Count, old.V.Count);
                for (int i = 0; i < count; i++)
                {
                    if (v[i] == old.V[i] && v[i].x.Equals(old.V[i].x) && v[i].y.Equals(old.V[i].y) && v[i].z.Equals(old.V[i].z)) exactV++;
                    if (n[i].x.Equals(old.N[i].x) && n[i].y.Equals(old.N[i].y) && n[i].z.Equals(old.N[i].z)) exactN++;
                    if (uvs[i].x.Equals(old.Uv[i].x) && uvs[i].y.Equals(old.Uv[i].y)) exactUv++;
                    dv = Math.Max(dv, (v[i] - old.V[i]).magnitude);
                    dn = Math.Max(dn, (n[i] - old.N[i]).magnitude);
                    duv = Math.Max(duv, (uvs[i] - old.Uv[i]).magnitude);
                }
                bool sameTris = idx.Count == oldIdx.Count;
                for (int i = 0; sameTris && i < idx.Count; i++) sameTris = idx[i] == oldIdx[i];
                bool sameMats = mats.Count == old.Mats.Count;
                for (int i = 0; sameMats && i < mats.Count; i++) sameMats = mats[i] == old.Mats[i];
                bool ok = v.Count == old.V.Count && sameTris && sameMats && dv < 1e-4 && dn < 1e-4 && duv < 1e-3;
                CostReport r = AddCost("equivalence", "the fast RaidMesh against the old primitives, call for call", calls, 0,
                    (ok ? "SAME" : "DIFFERENT") + ": " + F0(v.Count) + " vs " + F0(old.V.Count) + " vertices; bit-exact positions " + F0(exactV)
                    + ", normals " + F0(exactN) + ", UVs " + F0(exactUv) + "; largest difference " + dv.ToString("G3", CultureInfo.InvariantCulture)
                    + " m, " + dn.ToString("G3", CultureInfo.InvariantCulture) + " (normals), " + duv.ToString("G3", CultureInfo.InvariantCulture)
                    + " (UVs); triangles " + (sameTris ? "same" : "DIFFERENT") + ", materials " + (sameMats ? "same" : "DIFFERENT"));
                r.ok = ok;
                if (!ok)
                {
                    _harnessErrors++;
                    Remark("RaidMesh's fast primitives don't draw what the old ones did: " + r.note);
                }
                mesh.Release();
            }

            private static float R(System.Random rng, float span) => ((float)rng.NextDouble() - 0.5f) * span;

            private static Vector3 R3(System.Random rng, float span) => new Vector3(R(rng, span), R(rng, span), R(rng, span));

            private void Prim(string name, int n, Action<RaidMesh> draw)
            {
                var warm = new RaidMesh();
                for (int i = 0; i < 50; i++) draw(warm);
                var m = new RaidMesh();
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < n; i++) draw(m);
                double ms = sw.Elapsed.TotalMilliseconds;
                AddCost("primitives", name, n, ms, F0(m.VertexCount) + " vertices");
            }

            // The objects a build makes, many of each under an inactive root, then switched on and destroyed.
            private void CostObjects(Transform parent)
            {
                var root = new GameObject("objects");
                root.SetActive(false);
                root.transform.SetParent(parent, false);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 2000; i++)
                {
                    var go = new GameObject("o");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(i % 50, 0f, i / 50);
                }
                AddCost("objects", "empty GameObject, parented and placed", 2000, sw.Elapsed.TotalMilliseconds);
                var mesh = new Mesh { name = "cost" };
                Material[] mats = { DecorPalette.Matte(Color.grey).Mat };
                sw.Restart();
                for (int i = 0; i < 1000; i++)
                {
                    var go = new GameObject("r");
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials = mats;
                }
                AddCost("objects", "GameObject + MeshFilter + MeshRenderer", 1000, sw.Elapsed.TotalMilliseconds);
                var holder = new GameObject("boxes");
                holder.transform.SetParent(root.transform, false);
                sw.Restart();
                for (int i = 0; i < 2000; i++) holder.AddComponent<BoxCollider>().center = new Vector3(i % 50, 0f, i / 50);
                AddCost("objects", "BoxCollider added to one object", 2000, sw.Elapsed.TotalMilliseconds);
                sw.Restart();
                for (int i = 0; i < 200; i++) LootContainer.Spawn(root.transform, ContainerType.Crate, new Vector3(i % 20, 0f, 50f + i / 20), i);
                AddCost("objects", "LootContainer.Spawn (a crate)", 200, sw.Elapsed.TotalMilliseconds);
                sw.Restart();
                for (int i = 0; i < 100; i++) RoomLight.Spawn(root.transform, new Vector3(i, 3f, 70f), Color.white, 8f, 1f, false, false);
                AddCost("objects", "RoomLight.Spawn", 100, sw.Elapsed.TotalMilliseconds);
                sw.Restart();
                root.SetActive(true);
                AddCost("objects", "switching all of the above on", 1, sw.Elapsed.TotalMilliseconds, "edit mode: no Awake or OnEnable runs");
                sw.Restart();
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mesh);
                AddCost("objects", "destroying all of the above", 1, sw.Elapsed.TotalMilliseconds);
            }

            // MapGeometry.Triangulate over every footprint of Louisville's nine pieces and Bardstown's middle one.
            private void CostTriangulation()
            {
                MapPlace lou = _build.FindPlace("Louisville");
                var pieces = new List<(int cx, int cz)> { (ChunkOf(_place.x), ChunkOf(_place.z)) };
                if (lou != null)
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++) pieces.Add((ChunkOf(lou.x) + dx, ChunkOf(lou.z) + dz));
                int footprints = 0, maxPoints = 0, big = 0;
                double total = 0, worst = 0, areas = 0;
                foreach ((int cx, int cz) in pieces)
                {
                    var sw = Stopwatch.StartNew();
                    KentuckyMap.Area area = MapApi.ChunkArea(_build, _place.name, cx, cz);
                    areas += sw.Elapsed.TotalMilliseconds;
                    foreach (KentuckyMap.Footprint f in area.Buildings)
                    {
                        sw.Restart();
                        MapGeometry.Triangulate(f.Pts);
                        double ms = sw.Elapsed.TotalMilliseconds;
                        total += ms;
                        worst = Math.Max(worst, ms);
                        footprints++;
                        maxPoints = Math.Max(maxPoints, f.Pts.Count);
                        if (f.Pts.Count > 100) big++;
                    }
                }
                AddCost("triangulation", "MapGeometry.Triangulate, per footprint", footprints, total, "worst " + F2(worst) + " ms; the biggest has "
                    + maxPoints + " points; " + big + " over 100");
                AddCost("triangulation", "KentuckyMap.Area for a piece (the data step)", pieces.Count, areas);
            }

            // The biggest region read so far, read again as MapBuild.LoadRegion does (StreamReader.ReadToEnd), and through one
            // pooled buffer and an exactly sized string; then sorted into chunks.
            private void CostRegions()
            {
                MapApi.RegionTiming biggest = default;
                foreach (MapApi.RegionTiming t in MapApi.Loaded)
                    if (t.Exists && t.ReadMs > biggest.ReadMs) biggest = t;
                if (!biggest.Exists) return;
                string path = Path.Combine(_build.Folder, "regions", biggest.Rx + "_" + biggest.Rz + ".json.gz");
                MethodInfo allocated = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Public | BindingFlags.Static);
                Func<long> bytes = () => allocated != null ? (long)allocated.Invoke(null, null) : 0L;
                long b0 = bytes();
                var sw = Stopwatch.StartNew();
                MapRegion today = _build.LoadRegion(biggest.Rx, biggest.Rz);
                double ms = sw.Elapsed.TotalMilliseconds;
                long garbage = bytes() - b0;
                AddCost("regions", "MapBuild.LoadRegion " + biggest.Rx + "_" + biggest.Rz + " (today)", 1, ms, allocated != null ? F0(garbage / 1048576.0) + " MB allocated" : null);
                byte[] buffer = new byte[4 << 20];
                b0 = bytes();
                sw.Restart();
                int length;
                using (FileStream file = File.OpenRead(path))
                using (var gz = new GZipStream(file, CompressionMode.Decompress))
                {
                    length = 0;
                    while (true)
                    {
                        if (length == buffer.Length) Array.Resize(ref buffer, buffer.Length * 2);
                        int n = gz.Read(buffer, length, buffer.Length - length);
                        if (n <= 0) break;
                        length += n;
                    }
                }
                string json = Encoding.UTF8.GetString(buffer, 0, length);
                MapRegion pooled = JsonUtility.FromJson<MapRegion>(json);
                ms = sw.Elapsed.TotalMilliseconds;
                garbage = bytes() - b0;
                AddCost("regions", "the same through one buffer and an exactly sized string", 1, ms, (allocated != null ? F0(garbage / 1048576.0) + " MB allocated; " : "")
                    + F0(length / 1048576.0) + " MB of JSON; same data: " + (pooled != null && today != null && pooled.buildings?.Length == today.buildings?.Length ? "yes" : "no"));
                sw.Restart();
                var index = new MapRegionIndex(pooled, biggest.Rx, biggest.Rz, _build.RegionSize, MapApi.ChunkSize);
                AddCost("regions", "sorting it into chunks (MapRegionIndex)", 1, sw.Elapsed.TotalMilliseconds, index.Data != null ? "" : "no data");
            }
            #endregion

            #region The NavMesh over the Louisville block
            // What the streamer would collect from the nine pieces, and the NavMesh over them baked at once.
            private void BakeBlock(BlockReport block, List<MapApi.Chunk> kept)
            {
                try
                {
                    var sources = new List<NavMeshBuildSource>();
                    var sw = Stopwatch.StartNew();
                    foreach (MapApi.Chunk k in kept) KentuckyStreamer.Collect(k.Root.transform, null, sources);
                    block.navCollectMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);
                    block.navSources = sources.Count;
                    if (kept.Count == 0) return;
                    NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
                    if (settings.agentTypeID == -1) settings.agentTypeID = 0;
                    settings.minRegionArea = 2f;
                    Vector3 middle = kept[0].Middle;
                    var bounds = new Bounds(new Vector3(middle.x, 20f, middle.z), new Vector3(MapApi.ChunkSize * 3f, 80f, MapApi.ChunkSize * 3f));
                    sw.Restart();
                    NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
                    block.navBakeMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);
                    if (data != null) Object.DestroyImmediate(data);
                    Note("Louisville block NavMesh: " + sources.Count + " sources collected in " + F0(block.navCollectMs) + " ms, baked in "
                        + F0(block.navBakeMs) + " ms");
                }
                catch (Exception e)
                {
                    block.navError = Brief(e);
                    Remark("The Louisville block's NavMesh bake failed: " + block.navError);
                }
            }
            #endregion

            #region Walking and driving through the state
            // KentuckyStreamer as the game makes it for the statewide zone (its start square built at once), then driven frame by
            // frame at 60 fps: each frame the survivor moves, the streamer does a frame's work (Simulate: everything Update does
            // but the NavMesh and the agents), and what that cost the main thread is recorded; frames are padded to 16.7 ms of
            // wall clock so the regions read in the background take their real time.
            private void Routes()
            {
                PerfReport p = _r.perf;
                ZoneDef zone = StatewideZone();
                float half = Mathf.Clamp(zone.RealHalfSize, 60f, 400f);
                float arena = CloneZConfig.ArenaHalfSize;
                List<GameObject> before = SceneRoots();
                var world = new GameObject("World (streamed)").transform;
                KentuckyStreamer s = null;
                Begin(p.routesLog);
                try
                {
                    var info = new RaidMapInfo { HalfSize = half, PlayerSpawn = new Vector3(0f, 0.6f, 0f) };
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        s = KentuckyStreamer.Create(world, zone, _build, _place);
                        s.BuildStart(half, info);
                        p.start.built = true;
                    }
                    catch (Exception e)
                    {
                        _mapErrors++;
                        p.start.error = Brief(e);
                        Debug.LogError("[MapShots] the streamer's start failed: " + e);
                        return;
                    }
                    p.start.ms = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
                    s.Census(out p.start.pieces, out p.start.meshes, out p.start.vertices, out p.start.regions);
                    Note("streamer start: " + p.start.pieces + " pieces in " + F0(p.start.ms) + " ms");

                    Vector3 spawn = info.PlayerSpawn;
                    spawn.y = 0f;
                    p.routes.Add(Drive(s, "settle", "standing at the spawn while the country around the start is built", spawn, Vector2.zero, 0f, 8f));
                    p.routes.Add(Drive(s, "drive-east", "driving east from the spawn at 90 km/h", spawn, new Vector2(1f, 0f), 25f, 40f));
                    MapPlace lou = _build.FindPlace("Louisville");
                    if (lou != null)
                    {
                        var at = new Vector3((float)(lou.x - _place.x), 0f, (float)(lou.z - _place.z));
                        p.routes.Add(Drive(s, "teleport-louisville", "set down in downtown Louisville (" + F0(at.magnitude / 1000f) + " km away), standing",
                            at, Vector2.zero, 0f, 12f));
                        p.routes.Add(Drive(s, "drive-louisville", "driving south through downtown Louisville at 43 km/h", at, new Vector2(0f, -1f), 12f, 30f));
                    }
                }
                finally
                {
                    End();
                    var built = new List<Transform> { world };
                    foreach (GameObject go in SceneRoots())
                        if (!before.Contains(go) && go.transform != world) built.Add(go.transform);
                    ScanNonFinite("the streamed walks (what stood at the end)", built);
                    if (s != null) s.Teardown();
                    foreach (GameObject go in SceneRoots())
                        if (!before.Contains(go)) Object.DestroyImmediate(go);
                    Shelter.Clear();
                    CeilingVisibility.Clear();
                    CloneZConfig.ResetArena(arena);
                }
            }

            // A copy of the start zone, statewide as the game plays it.
            private ZoneDef StatewideZone()
            {
                var copy = new ZoneDef();
                foreach (FieldInfo f in typeof(ZoneDef).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (!f.IsInitOnly) f.SetValue(copy, f.GetValue(_zone));
                copy.RealStatewide = true;
                return copy;
            }

            private RouteReport Drive(KentuckyStreamer s, string name, string what, Vector3 from, Vector2 heading, float speed, float seconds)
            {
                var r = new RouteReport { name = name, what = what, speed = speed, seconds = seconds };
                var stats = new KentuckyStreamer.TickStats();
                int frames = Mathf.CeilToInt(seconds * 60f);
                var costs = new List<double>(frames);
                var slow = new Dictionary<string, StepTiming>();
                var clock = new Stopwatch();
                const double frameMs = 1000.0 / 60.0;
                s.SimulatedFocus.Clear();
                s.SimulatedFocus.Add(from);
                for (int i = 0; i < frames; i++)
                {
                    clock.Restart();
                    float t = i / 60f;
                    Vector3 at = from + new Vector3(heading.x, 0f, heading.y) * (speed * t);
                    s.SimulatedFocus[0] = at;
                    s.Simulate(stats);
                    costs.Add(stats.Ms);
                    r.totalMs += stats.Ms;
                    r.steps += stats.Steps;
                    if (stats.Steps > 0) r.buildFrames++;
                    r.finished += stats.Finished;
                    r.finishedFull += stats.FinishedFull;
                    r.finishedLite += stats.FinishedLite;
                    r.upgraded += stats.Upgraded;
                    r.unloaded += stats.Unloaded;
                    if (stats.LongestStepMs > r.longestStepMs)
                    {
                        r.longestStepMs = Math.Round(stats.LongestStepMs, 2);
                        r.longestStep = stats.LongestStep;
                    }
                    if (stats.Ms > 8.0 && stats.LongestStep != null)
                    {
                        if (!slow.TryGetValue(stats.LongestStep, out StepTiming st)) slow[stats.LongestStep] = st = new StepTiming { name = stats.LongestStep };
                        st.count++;
                        st.ms = Math.Round(st.ms + stats.LongestStepMs, 2);
                        st.maxMs = Math.Max(st.maxMs, Math.Round(stats.LongestStepMs, 2));
                    }
                    if (s.DetailAt(at) == null) r.holeFrames++;
                    else if (r.ownBuiltAfter < 0f) r.ownBuiltAfter = t;
                    if (!RingFull(s, at)) r.ringFrames++;
                    else if (r.ringAfter < 0f) r.ringAfter = t;
                    while (clock.Elapsed.TotalMilliseconds < frameMs - 0.75) Thread.Sleep(1);
                }
                r.frames = frames;
                costs.Sort();
                r.meanMs = Math.Round(r.totalMs / Math.Max(1, frames), 3);
                r.p50Ms = Math.Round(Percentile(costs, 0.5), 2);
                r.p95Ms = Math.Round(Percentile(costs, 0.95), 2);
                r.p99Ms = Math.Round(Percentile(costs, 0.99), 2);
                r.maxMs = Math.Round(costs.Count > 0 ? costs[costs.Count - 1] : 0, 2);
                r.totalMs = Math.Round(r.totalMs, 1);
                foreach (double c in costs)
                {
                    if (c > 5.0) r.over5++;
                    if (c > 8.0) r.over8++;
                    if (c > 16.0) r.over16++;
                    if (c > 33.0) r.over33++;
                }
                var worst = new List<StepTiming>(slow.Values);
                worst.Sort((a, b) => b.maxMs.CompareTo(a.maxMs));
                for (int i = 0; i < worst.Count && i < 6; i++) r.slowest.Add(worst[i]);
                s.Census(out r.pieces, out r.litePieces, out r.meshes, out r.vertices, out r.regions);
                r.managedMB = Math.Round(GC.GetTotalMemory(true) / 1048576.0, 1);
                r.totalMB = Math.Round(Profiler.GetTotalAllocatedMemoryLong() / 1048576.0, 1);
                r.pooledMB = Math.Round(RaidMesh.PooledBytes / 1048576.0, 1);
                Note("route " + name + ": " + frames + " frames, mean " + F2(r.meanMs) + " ms, p99 " + F2(r.p99Ms) + " ms, max " + F2(r.maxMs)
                    + " ms (" + r.longestStep + "), " + r.finished + " pieces built, " + r.unloaded + " let go, " + r.holeFrames + " frames on unbuilt ground");
                return r;
            }

            // True when the nine pieces around <paramref name="at"/> are built in Full.
            private static bool RingFull(KentuckyStreamer s, Vector3 at)
            {
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (s.DetailAt(at + new Vector3(dx * MapApi.ChunkSize, 0f, dz * MapApi.ChunkSize)) != KentuckyMap.Detail.Full) return false;
                return true;
            }

            private static double Percentile(List<double> sorted, double q)
            {
                if (sorted.Count == 0) return 0;
                int i = Mathf.Clamp((int)Math.Ceiling(q * sorted.Count) - 1, 0, sorted.Count - 1);
                return sorted[i];
            }
            #endregion

            #region The report's perf sections
            // Where each test piece's main-thread time went.
            private void BreakdownTable(StringBuilder md)
            {
                md.Append("### Where a piece's time goes\n\nFrom RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects ")
                    .Append("holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, ")
                    .Append("baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh ")
                    .Append("sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.\n\n")
                    .Append("| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |\n|---|---|--:|---|---|---|--:|--:|---|\n");
                var all = new List<ChunkReport>(_r.chunks);
                all.AddRange(_r.louisvilleBlock.chunks);
                foreach (ChunkReport c in all)
                {
                    double rest = c.totalMs - c.colliderMs - c.lightMs - c.emitMs;
                    md.Append("| ").Append(Esc(c.label)).Append(" | ").Append(c.detail).Append(" | ").Append(F0(c.totalMs)).Append(" | ")
                        .Append(c.madeColliders).Append(" / ").Append(c.madeColliderObjects).Append(" / ").Append(F1(c.colliderMs)).Append(" | ")
                        .Append(c.madeLights).Append(" / ").Append(F1(c.lightMs)).Append(" | ")
                        .Append(c.bakedMeshes).Append(" / ").Append(F0(c.bakedVertices)).Append(" / ").Append(F1(c.bakeMs)).Append(" | ")
                        .Append(F1(c.emitMs)).Append(" | ").Append(F1(rest)).Append(" | ").Append(c.navSources).Append(" / ").Append(F1(c.navCollectMs)).Append(" |\n");
                }
                BlockReport b = _r.louisvilleBlock;
                if (b.navSources > 0 || b.navError != null)
                    md.Append("\nThe Louisville block's NavMesh: ").Append(b.navSources).Append(" sources collected in ").Append(F0(b.navCollectMs))
                        .Append(" ms; baked at once (as one 384 m square) in ").Append(F0(b.navBakeMs)).Append(" ms")
                        .Append(b.navError != null ? " (failed: " + Esc(b.navError) + ")" : "").Append(". In the game the bake runs on worker threads, tile by tile.\n");
                md.Append('\n');
            }

            private void PerfMarkdown(StringBuilder md)
            {
                PerfReport p = _r.perf;
                if (p.detail.Count == 0 && p.costs.Count == 0 && p.routes.Count == 0 && !p.order.ran) return;
                md.Append("## Streaming performance\n\n");
                if (p.detail.Count > 0)
                {
                    md.Append("### Lite against Full\n\nEach test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).\n\n")
                        .Append("| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite ")
                        .Append("| renderers Full / Lite | colliders Full / Lite | lights Full / Lite |\n|---|--:|--:|--:|---|---|---|---|---|---|---|\n");
                    foreach (DetailPair d in p.detail)
                        md.Append("| ").Append(Esc(d.label)).Append(d.identical ? " (identical)" : "").Append(" | ").Append(F0(d.fullMs)).Append(" | ")
                            .Append(F0(d.liteMs)).Append(" | ").Append(F0(d.ratio * 100)).Append("% | ").Append(Esc(d.fullSlowest)).Append(' ').Append(F1(d.fullSlowestMs))
                            .Append(" | ").Append(Esc(d.liteSlowest)).Append(' ').Append(F1(d.liteSlowestMs)).Append(" | ").Append(F0(d.fullVertices)).Append(" / ")
                            .Append(F0(d.liteVertices)).Append(" | ").Append(F0(d.fullObjects)).Append(" / ").Append(F0(d.liteObjects)).Append(" | ")
                            .Append(F0(d.fullRenderers)).Append(" / ").Append(F0(d.liteRenderers)).Append(" | ").Append(F0(d.fullColliders)).Append(" / ")
                            .Append(F0(d.liteColliders)).Append(" | ").Append(d.fullLights).Append(" / ").Append(d.liteLights).Append(" |\n");
                    md.Append('\n');
                }
                if (p.order.ran)
                {
                    md.Append("### Determinism across build orders\n\n");
                    foreach (DeterminismReport d in new[] { p.order, p.afterLite, p.liteOrder })
                        if (d.ran)
                            md.Append("- ").Append(Esc(d.label)).Append(": **").Append(d.match ? "match" : "MISMATCH").Append("**: ").Append(Esc(d.detail))
                                .Append(d.differences.Count > 0 ? "; first: " + Esc(string.Join("; ", d.differences)) : "").Append('\n');
                    md.Append('\n');
                }
                if (p.costs.Count > 0)
                {
                    md.Append("### What things cost\n\nMicro-benchmarks, warm, in the editor (Mono, edit mode).\n\n| group | what | n | ms | µs each | notes |\n|---|---|--:|--:|--:|---|\n");
                    foreach (CostReport c in p.costs)
                        md.Append("| ").Append(c.group).Append(" | ").Append(Esc(c.name)).Append(" | ").Append(F0(c.n)).Append(" | ").Append(F2(c.ms)).Append(" | ")
                            .Append(F2(c.usEach)).Append(" | ").Append(Esc(c.note)).Append(" |\n");
                    md.Append('\n');
                }
                if (p.start.built || p.start.error != null || p.routes.Count > 0)
                {
                    md.Append("### Walking and driving through the state\n\nKentuckyStreamer as the statewide game makes it, its start square built at once (")
                        .Append(p.start.built ? p.start.pieces + " pieces in " + F0(p.start.ms) + " ms, " + F0(p.start.vertices) + " vertices" : "failed: " + Esc(p.start.error))
                        .Append("), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so ")
                        .Append("the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames ")
                        .Append("the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.\n\n")
                        .Append("| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) ")
                        .Append("| upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end ")
                        .Append("| regions | managed / total MB |\n|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|\n");
                    foreach (RouteReport r in p.routes)
                        md.Append("| ").Append(r.name).Append(" | ").Append(r.frames).Append(" (").Append(r.buildFrames).Append(") | ").Append(F2(r.meanMs)).Append(" | ")
                            .Append(F2(r.p50Ms)).Append(" | ").Append(F2(r.p95Ms)).Append(" | ").Append(F2(r.p99Ms)).Append(" | ").Append(F2(r.maxMs)).Append(" | ")
                            .Append(r.over5).Append(" / ").Append(r.over8).Append(" / ").Append(r.over16).Append(" / ").Append(r.over33).Append(" | ")
                            .Append(Esc(r.longestStep)).Append(' ').Append(F1(r.longestStepMs)).Append(" | ").Append(r.finished).Append(" (").Append(r.finishedFull)
                            .Append(" / ").Append(r.finishedLite).Append(") | ").Append(r.upgraded).Append(" | ").Append(r.unloaded).Append(" | ")
                            .Append(r.ownBuiltAfter < 0 ? "never" : F1(r.ownBuiltAfter)).Append(" | ").Append(r.ringAfter < 0 ? "never" : F1(r.ringAfter)).Append(" | ")
                            .Append(r.holeFrames).Append(" / ").Append(r.ringFrames).Append(" | ").Append(r.pieces).Append(r.litePieces > 0 ? " (" + r.litePieces + " Lite)" : "")
                            .Append(" / ").Append(F0(r.meshes)).Append(" / ")
                            .Append(F0(r.vertices)).Append(" | ").Append(r.regions).Append(" | ").Append(F0(r.managedMB)).Append(" / ").Append(F0(r.totalMB))
                            .Append(r.pooledMB > 0 ? " (pool " + F0(r.pooledMB) + ")" : "").Append(" |\n");
                    md.Append('\n');
                    foreach (RouteReport r in p.routes)
                    {
                        md.Append("- **").Append(r.name).Append("**: ").Append(Esc(r.what));
                        if (r.slowest.Count > 0)
                        {
                            md.Append("; the frames over 8 ms were made by: ");
                            for (int i = 0; i < r.slowest.Count; i++)
                                md.Append(i > 0 ? ", " : "").Append(Esc(r.slowest[i].name)).Append(" (").Append(r.slowest[i].count).Append("x, worst ")
                                    .Append(F1(r.slowest[i].maxMs)).Append(" ms)");
                        }
                        md.Append('\n');
                    }
                    md.Append('\n');
                    LogSection(md, "Logged while walking and driving", p.routesLog);
                }
            }

            // The meshes that aren't finite numbers, right under the start square's counts: they're errors.
            private void NonFiniteMarkdown(StringBuilder md)
            {
                NonFiniteReport n = _r.nonFinite;
                if (n.meshes == 0 && n.baked.Count == 0) return;
                md.Append("### Meshes that aren't finite numbers\n\n");
                if (n.offenders.Count == 0 && n.baked.Count == 0)
                {
                    md.Append("None: every one of the ").Append(F0(n.meshes)).Append(" meshes checked (the square, the test chunks, what stood at the end ")
                        .Append("of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.\n\n");
                    return;
                }
                md.Append("**").Append(n.offenders.Count + n.baked.Count).Append(" error(s).** A mesh with a NaN or infinite vertex is skipped whole by ")
                    .Append("the NavMesh bake (`RuntimeNavMeshBuilder: Source mesh has invalid vertex data`), leaving a hole. ")
                    .Append(F0(n.meshes)).Append(" meshes checked.\n\n");
                if (n.offenders.Count > 0)
                {
                    md.Append("In the scene (where it hangs, the mesh, what's wrong):\n\n");
                    foreach (string s in n.offenders) md.Append("- ").Append(Esc(s)).Append('\n');
                    md.Append('\n');
                }
                if (n.baked.Count > 0)
                {
                    md.Append("Baked by RaidMesh with such vertices (it folded them away, so the meshes are sound; the geometry asked for wasn't):\n\n");
                    foreach (string s in n.baked) md.Append("- ").Append(Esc(s)).Append('\n');
                    md.Append('\n');
                }
            }
            #endregion

            #region Non-finite geometry
            // The roots' children grouped by their names with the digits folded ("Walls 1234" and "Walls 98" are "Walls #"),
            // counted, the ones with the most material slots first (at most <paramref name="max"/>).
            private static List<SceneCounts> Groups(List<Transform> roots, int max)
            {
                var byName = new Dictionary<string, List<Transform>>();
                var order = new List<string>();
                foreach (Transform root in roots)
                {
                    if (root == null) continue;
                    foreach (Transform child in root)
                    {
                        string key = root.name + "/" + Fold(child.name);
                        if (!byName.TryGetValue(key, out List<Transform> list))
                        {
                            byName[key] = list = new List<Transform>();
                            order.Add(key);
                        }
                        list.Add(child);
                    }
                }
                var all = new List<SceneCounts>(order.Count);
                foreach (string key in order) all.Add(Count(key, byName[key]));
                all.Sort((a, b) => b.materialSlots.CompareTo(a.materialSlots));
                if (all.Count > max) all.RemoveRange(max, all.Count - max);
                return all;
            }

            private static string Fold(string name)
            {
                var sb = new StringBuilder(name.Length);
                bool digits = false;
                foreach (char ch in name)
                {
                    if (char.IsDigit(ch))
                    {
                        if (!digits) sb.Append('#');
                        digits = true;
                    }
                    else
                    {
                        sb.Append(ch);
                        digits = false;
                    }
                }
                return sb.ToString();
            }
            private readonly List<Vector3> _scanV = new List<Vector3>(1 << 14), _scanN = new List<Vector3>(1 << 14);
            private int _bakedSeen;

            // Every mesh under <paramref name="roots"/> (once each) checked for positions, normals and bounds that aren't finite
            // numbers: each offender listed with its place in the hierarchy, and counted as a map error.
            private void ScanNonFinite(string where, IEnumerable<Transform> roots)
            {
                NonFiniteReport r = _r.nonFinite;
                var seen = new HashSet<Mesh>();
                foreach (Transform root in roots)
                {
                    if (root == null) continue;
                    foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        Mesh m = mf.sharedMesh;
                        if (m == null || !seen.Add(m)) continue;
                        r.meshes++;
                        Bounds b = m.bounds;
                        bool bounds = !Finite(b.center) || !Finite(b.extents);
                        int positions = 0, normals = 0, n = 0;
                        if (m.isReadable)
                        {
                            m.GetVertices(_scanV);
                            n = _scanV.Count;
                            for (int i = 0; i < n; i++) if (!Finite(_scanV[i])) positions++;
                            m.GetNormals(_scanN);
                            for (int i = 0; i < _scanN.Count; i++) if (!Finite(_scanN[i])) normals++;
                        }
                        if (!bounds && positions == 0 && normals == 0) continue;
                        _mapErrors++;
                        string line = where + ": " + RaidMesh.PathOf(mf.transform) + " (mesh '" + m.name + "'): " + positions + " of " + n
                            + " positions, " + normals + " normals" + (bounds ? ", the bounds" : "") + " not finite";
                        if (r.offenders.Count < 100) r.offenders.Add(line);
                        Note("NON-FINITE " + line);
                    }
                }
                _scanV.Clear();
                _scanN.Clear();
            }

            // The meshes RaidMesh baked NaN or infinite vertices into (harmless) since the last look, put down to <paramref name="stage"/>.
            private void TakeBaked(string stage)
            {
                lock (RaidMesh.NonFinite)
                {
                    for (; _bakedSeen < RaidMesh.NonFinite.Count; _bakedSeen++)
                    {
                        _mapErrors++;
                        if (_r.nonFinite.baked.Count < 100) _r.nonFinite.baked.Add(stage + ": " + RaidMesh.NonFinite[_bakedSeen]);
                    }
                }
            }

            private static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
                || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
            #endregion
        }
    }
}
#endif
