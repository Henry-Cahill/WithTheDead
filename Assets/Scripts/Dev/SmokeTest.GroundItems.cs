using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace CloneZ
{
    /// <summary>
    /// <c>grounditems</c>: the ground items' renderer (<see cref="GroundItemRenderer"/>) under load. In the Kettle it scatters
    /// <see cref="GroundLoad"/> items of <see cref="GroundKinds"/> kinds over the draw range round the survivor, and a pile of
    /// <see cref="GroundPile"/> of a kind nothing has looked at yet somewhere in range but out of view. Then it:
    /// <list type="bullet">
    /// <item>stands still for <see cref="GroundStillFrames"/> frames and logs the renderer's CPU time a frame from its profiler
    /// samples (median, 95th percentile, worst), beside what the old renderer's gather costs over the same items
    /// (<see cref="OldGather"/>: every item in range walked, and its mesh looked up, every frame). It fails if a chunk was
    /// filed again while nothing on the ground changed, or if the renderer's refresh and gather together aren't at least four
    /// times cheaper than the old gather;</item>
    /// <item>fails unless the renderer draws what the old gather would: the same items under the same meshes with the same
    /// matrices, spin and bob included, both on frames that only move what was in view (standing still) and on frames gathered
    /// afresh, and again with the animation off;</item>
    /// <item>fails if the pile out of view had its meshes looked up; then stands on the pile and fails unless it's drawn;</item>
    /// <item>drops an item and picks it up again, failing unless each files just the one chunk again and the item shows and
    /// goes;</item>
    /// <item>fails if any item's model or generated mesh reaches further than the renderer assumes before looking its mesh
    /// up (<see cref="GroundItemRenderer.UnknownReach"/>).</item>
    /// </list>
    /// With <c>-clonez-smoke-shots &lt;dir&gt;</c> the standing-still frames' numbers go to grounditems.csv there. The renderer
    /// needs a graphics device, so leave out <c>-nographics</c>.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private const int GroundLoad = 3000, GroundKinds = 48, GroundPile = 24, GroundStillFrames = 240, GroundOldFrames = 120;
        private const float GroundPileSpread = 1.5f;

        // An item on the view's edge (its bounds within this of a plane) may fall either side of it from float rounding alone.
        private const float GroundEdge = 0.001f;
        // The most a matrix entry may differ from the old gather's: rounding in the spin and bob, nothing more.
        private const float GroundPose = 0.001f;

        private IEnumerator GroundItemsRun()
        {
            yield return ToHub();
            yield return Until(() => !LoadingScreen.Showing, 30f);
            yield return new WaitForSeconds(2f);

            GroundItemRenderer gir = GroundItemRenderer.Instance;
            Actor me = Actor.LocalPlayer;
            Check("ground-item renderer drawing", gir != null && gir.isActiveAndEnabled && SystemInfo.supportsInstancing && me != null,
                gir == null ? "no renderer" : !SystemInfo.supportsInstancing ? "this device can't instance (leave out -nographics)"
                : me == null ? "no survivor" : "");
            if (gir == null || !SystemInfo.supportsInstancing || me == null) yield break;
            me.Untargetable = true;

            var planes = new Plane[6];
            gir.CopyFrustum(planes);
            Vector3 eye = gir.Eye;
            float range = gir.DrawRadius;

            // The kinds, spread through the catalogue so some have generated meshes and some primitive models, and for the
            // pile out of view one whose meshes nothing has asked for yet (a generated one, if it can).
            var ids = new List<string>(ItemDatabase.AllIds);
            ids.Sort(StringComparer.Ordinal);
            IDictionary asked = MeshesAskedFor();
            ItemDef hidden = null;
            for (int i = ids.Count - 1, tries = 0; i >= 0 && tries < 60; i -= 7, tries++)
            {
                ItemDef def = ItemDatabase.Get(ids[i]);
                if (def == null || (asked != null && asked.Contains(def.Id))) continue;
                if (hidden == null) hidden = def;
                TextAsset generated = Resources.Load<TextAsset>(GeneratedItemMeshes.ResourceFolder + "/" + def.Id + "@1");
                if (generated == null) continue;
                Resources.UnloadAsset(generated);
                hidden = def;
                break;
            }
            var kinds = new List<ItemDef>(GroundKinds);
            for (int i = 0, step = Mathf.Max(1, ids.Count / GroundKinds); i < ids.Count && kinds.Count < GroundKinds; i += step)
            {
                ItemDef def = ItemDatabase.Get(ids[i]);
                if (def != null && def != hidden) kinds.Add(def);
            }
            bool spot = FindHiddenSpot(eye, planes, range, out Vector3 pile);
            Check("ground items to scatter", kinds.Count > 0 && hidden != null && spot,
                kinds.Count + " kinds; out of view: " + (hidden != null ? hidden.Id : "no kind left unasked") + (spot
                    ? " at " + Num(Flat(pile - eye).magnitude) + " m" : ", but no NavMesh point in range is out of view"));
            if (kinds.Count == 0 || hidden == null || !spot) yield break;

            var rng = new System.Random(20261107);
            var spawned = new List<(long key, uint id)>(GroundLoad + GroundPile);
            for (int i = 0; i < GroundLoad; i++)
            {
                float r = (range - 1f) * Mathf.Sqrt((float)rng.NextDouble());
                float a = (float)(rng.NextDouble() * Math.PI * 2.0);
                SpawnGroundItem(kinds[i % kinds.Count], new Vector3(eye.x + Mathf.Cos(a) * r, 0f, eye.z + Mathf.Sin(a) * r), rng, spawned);
            }
            for (int i = 0; i < GroundPile; i++)
            {
                float r = GroundPileSpread * Mathf.Sqrt((float)rng.NextDouble());
                float a = (float)(rng.NextDouble() * Math.PI * 2.0);
                SpawnGroundItem(hidden, new Vector3(pile.x + Mathf.Cos(a) * r, 0f, pile.z + Mathf.Sin(a) * r), rng, spawned);
            }
            for (int i = 0; i < 30; i++) yield return null;
            int inRange = ItemsInRange(gir.Eye, range);
            Check("ground items scattered", spawned.Count == GroundLoad + GroundPile && gir.DrawnLastFrame > 0,
                spawned.Count + " dropped, " + inRange + " in range, " + gir.DrawnLastFrame + " drawn in " + gir.BatchesLastFrame
                + " draws from " + gir.ChunksInRange + " chunks");

            // Standing still: the renderer's own profiler samples, frame by frame.
            Recorder total = Recorder.Get(GroundItemRenderer.ProfilerName);
            Recorder refresh = Recorder.Get(GroundItemRenderer.RefreshProfilerName);
            Recorder gather = Recorder.Get(GroundItemRenderer.GatherProfilerName);
            Recorder draw = Recorder.Get(GroundItemRenderer.DrawProfilerName);
            foreach (Recorder r in new[] { total, refresh, gather, draw }) if (r.isValid) r.enabled = true;
            yield return null;
            var totalMs = new List<double>(GroundStillFrames);
            var refreshMs = new List<double>(GroundStillFrames);
            var gatherMs = new List<double>(GroundStillFrames);
            var drawMs = new List<double>(GroundStillFrames);
            var csv = new StringBuilder("frame,renderer_ms,refresh_ms,gather_ms,draw_ms,drawn,draws,chunks_filed,view_reused\n");
            int filedStill = 0, filedForChanges = 0, lookups = gir.MeshLookups, drawnLeast = int.MaxValue, drawnMost = 0, reused = 0;
            int version = GroundItemStore.Version;
            bool changedBefore = false;
            (int, int, int, int) window = ChunkWindow(gir.Eye, range), windowBefore = window;
            for (int f = 0; f < GroundStillFrames; f++)
            {
                yield return null;
                // A chunk filed again is only right when something on the ground changed (or the range took in other chunks)
                // just before that frame.
                bool changed = GroundItemStore.Version != version;
                version = GroundItemStore.Version;
                windowBefore = window;
                window = ChunkWindow(gir.Eye, range);
                if (changed || changedBefore || window != windowBefore) filedForChanges += gir.ChunksRebuiltLastFrame;
                else filedStill += gir.ChunksRebuiltLastFrame;
                changedBefore = changed;

                double t = Ms(total), rf = Ms(refresh), g = Ms(gather), d = Ms(draw);
                totalMs.Add(t);
                refreshMs.Add(rf);
                gatherMs.Add(g);
                drawMs.Add(d);
                drawnLeast = Mathf.Min(drawnLeast, gir.DrawnLastFrame);
                drawnMost = Mathf.Max(drawnMost, gir.DrawnLastFrame);
                if (gir.ViewReusedLastFrame) reused++;
                csv.Append(f).Append(',').Append(Num4(t)).Append(',').Append(Num4(rf)).Append(',').Append(Num4(g)).Append(',')
                    .Append(Num4(d)).Append(',').Append(gir.DrawnLastFrame).Append(',').Append(gir.BatchesLastFrame).Append(',')
                    .Append(gir.ChunksRebuiltLastFrame).Append(',').Append(gir.ViewReusedLastFrame ? 1 : 0).Append('\n');
            }
            foreach (Recorder r in new[] { total, refresh, gather, draw }) if (r.isValid) r.enabled = false;
            bool timed = total.isValid && Quantile(totalMs, 1.0) > 0.0;
            Check("standing still files nothing again", filedStill == 0,
                filedStill + " chunks filed with nothing changed, " + filedForChanges + " for changes made by the hub over "
                + GroundStillFrames + " frames; " + (gir.MeshLookups - lookups) + " meshes looked up (an item crossing the near range); "
                + reused + " frames only moved what was already in view");

            // The pile out of view: in range all along, but nothing of it looked up.
            bool pileLooked = gir.HasLookedUp(hidden);
            Check("a pile in range but out of view has nothing looked up", !pileLooked,
                hidden.Id + " " + Num(Flat(pile - gir.Eye).magnitude) + " m away; " + (pileLooked ? "the renderer looked it up"
                    : asked != null && asked.Contains(hidden.Id) ? "not the renderer, but something else has asked for its meshes since"
                    : "its meshes never loaded"));

            // The same frame as the old gather would have drawn it, animated and still.
            CompareWithOldGather(gir, "standing still");
            gir.Animate = false;
            yield return null;
            yield return null;
            CompareWithOldGather(gir, "standing still, the spin and bob off");
            gir.Animate = true;

            // What the old renderer paid every frame for the same items (its meshes are all looked up by now).
            var old = new OldGather();
            var oldMs = new List<double>(GroundOldFrames);
            var watch = new Stopwatch();
            for (int f = 0; f < GroundOldFrames; f++)
            {
                yield return null;
                gir.CopyFrustum(planes);
                watch.Restart();
                old.Run(gir.Eye, planes, gir.AnimatedAt, gir.DrawRadius, gir.NearRadius, gir.Animate, null);
                oldMs.Add(watch.Elapsed.TotalMilliseconds);
            }
            Debug.Log("[Smoke] grounditems standing still over " + GroundStillFrames + " frames (" + reused + " only moving what was in view), "
                + inRange + " items in range, " + drawnLeast + "-" + drawnMost + " drawn in " + gir.BatchesLastFrame + " draws: the renderer "
                + Num4(Median(totalMs)) + " ms a frame (95th percentile " + Num4(Quantile(totalMs, 0.95)) + ", worst "
                + Num4(Quantile(totalMs, 1.0)) + "): refresh " + Num4(Median(refreshMs)) + ", gather " + Num4(Median(gatherMs)) + ", draw "
                + Num4(Median(drawMs)) + "; the old gather " + Num4(Median(oldMs)) + " ms (95th percentile " + Num4(Quantile(oldMs, 0.95))
                + ", worst " + Num4(Quantile(oldMs, 1.0)) + ") over the same items, so about " + Num4(Median(oldMs) + Median(drawMs))
                + " ms a frame with the same draws" + (timed ? "" : "; the renderer's profiler samples weren't recorded"));
            double gathering = Median(refreshMs) + Median(gatherMs);
            Check("standing still gathers for a fraction of the old cost", timed && gathering * 4.0 < Median(oldMs),
                "refresh and gather " + Num4(gathering) + " ms a frame against the old gather's " + Num4(Median(oldMs)) + " ms");
            if (s_ShotDir != null)
            {
                string file = Path.Combine(s_ShotDir, "grounditems.csv");
                try { File.WriteAllText(file, csv.ToString()); Debug.Log("[Smoke] grounditems frames written to " + file); }
                catch (Exception e) { Debug.LogWarning("[Smoke] grounditems.csv not written: " + e.Message); }
            }

            // Dropping an item and picking it up again: just its own chunk filed again, the item shown and then gone.
            Vector3 at = gir.Eye + new Vector3(0.7f, 0f, 0.4f);
            at.y = 0f;
            uint dropped = GroundItemStore.Add(kinds[0], at, 1);
            yield return null;
            int filedOnDrop = gir.ChunksRebuiltLastFrame;
            bool shownAfterDrop = DrawnAt(gir, at);
            // The frame after a change gathers everything afresh rather than moving what it had: it must agree as well.
            Check("the frame after a drop gathers afresh", !gir.ViewReusedLastFrame);
            CompareWithOldGather(gir, "gathered afresh after a drop");
            bool picked = GroundItemStore.Remove(new GroundItemStore.Handle(GroundItemStore.KeyOf(at.x, at.z), -1, dropped));
            yield return null;
            int filedOnPick = gir.ChunksRebuiltLastFrame;
            bool shownAfterPick = DrawnAt(gir, at);
            Check("a dropped item files just its chunk again and shows", dropped != 0 && filedOnDrop == 1 && shownAfterDrop,
                filedOnDrop + " chunks filed, " + (shownAfterDrop ? "drawn" : "not drawn"));
            Check("picking it up files just its chunk again and it goes", picked && filedOnPick == 1 && !shownAfterPick,
                filedOnPick + " chunks filed, " + (shownAfterPick ? "still drawn" : "gone"));

            // Over to the pile: only the chunks coming into range are filed, and the pile is looked up and drawn.
            var startChunks = new HashSet<long>(ChunksWithItems(gir.Eye, range));
            var seen = new HashSet<long>();
            int filedOnTheWay = 0;
            PlaceSurvivor(me, pile + Vector3.up * 0.05f, 0f);
            for (float until = Time.realtimeSinceStartup + 3f; Time.realtimeSinceStartup < until;)
            {
                yield return null;
                filedOnTheWay += gir.ChunksRebuiltLastFrame;
                foreach (long key in ChunksWithItems(gir.Eye, range)) seen.Add(key);
            }
            seen.ExceptWith(startChunks);
            Check("crossing into other chunks files only those arriving", filedOnTheWay <= seen.Count,
                filedOnTheWay + " filed, " + seen.Count + " with items came into range");
            int pileDrawn = 0;
            gir.VisitDrawn((m, mesh, def) => { if (def == hidden) pileDrawn++; });
            Check("the pile is looked up and drawn once in view", gir.HasLookedUp(hidden) && pileDrawn == GroundPile,
                pileDrawn + " of " + GroundPile + " drawn, " + Num(Flat(pile - gir.Eye).magnitude) + " m from the eye");
            CompareWithOldGather(gir, "by the pile");

            int removed = 0;
            foreach ((long key, uint id) in spawned) if (GroundItemStore.Remove(new GroundItemStore.Handle(key, -1, id))) removed++;
            Check("ground items cleared away", removed == spawned.Count, removed + " of " + spawned.Count);
            yield return null;
            CheckModelReach();
        }

        // GroundItemRenderer.UnknownReach is how far an item is taken to reach before its mesh is looked up, so every item
        // has to fit in it: each model's parts boxed as they're placed and turned (the baked mesh lies inside), plus the
        // height it rests at and the bob. The generated meshes are fitted to these models' size (1.4 m at most when last
        // measured; reading every one of their files would take too long here).
        private void CheckModelReach()
        {
            var unit = new Dictionary<PartShape, Vector3>();
            float worst = 0f;
            string worstId = "none";
            int models = 0;
            foreach (string id in ItemDatabase.AllIds)
            {
                ItemDef def = ItemDatabase.Get(id);
                ItemModelDesc desc = def == null ? null
                    : ItemModels.Describe(def) ?? GeneratedItemModels.Describe(def) ?? FallbackItemModel.Describe(def);
                if (desc == null || desc.Parts.Count == 0) continue;
                models++;
                Vector3 lo = Vector3.positiveInfinity, hi = Vector3.negativeInfinity;
                foreach (Part p in desc.Parts)
                {
                    if (!unit.TryGetValue(p.Shape, out Vector3 u)) unit[p.Shape] = u = UnitHalfSize(p.Shape);
                    Vector3 h = Vector3.Scale(u, new Vector3(Mathf.Abs(p.Scale.x), Mathf.Abs(p.Scale.y), Mathf.Abs(p.Scale.z)));
                    Matrix4x4 r = Matrix4x4.Rotate(p.Rotation);
                    var e = new Vector3(
                        Mathf.Abs(r.m00) * h.x + Mathf.Abs(r.m01) * h.y + Mathf.Abs(r.m02) * h.z,
                        Mathf.Abs(r.m10) * h.x + Mathf.Abs(r.m11) * h.y + Mathf.Abs(r.m12) * h.z,
                        Mathf.Abs(r.m20) * h.x + Mathf.Abs(r.m21) * h.y + Mathf.Abs(r.m22) * h.z);
                    lo = Vector3.Min(lo, p.Position - e);
                    hi = Vector3.Max(hi, p.Position + e);
                }
                float reach = ((hi - lo) * 0.5f).magnitude + Mathf.Abs(desc.BaseHeight) + GroundItemRenderer.BobHeight;
                if (reach > worst)
                {
                    worst = reach;
                    worstId = id;
                }
            }
            Check("every item's model fits the reach assumed before its mesh is looked up",
                models > 0 && worst <= GroundItemRenderer.UnknownReach,
                models + " models, the largest (" + worstId + ") reaching " + Num(worst) + " m of " + Num(GroundItemRenderer.UnknownReach));

            // The generated meshes, read from their files, which only the editor has loose on disk (the mesh pipeline
            // refuses to write one that reaches further, Tools/AssetPipeline/mesh/meshpost.py).
            string folder = Path.Combine(Application.dataPath, "Resources", GeneratedItemMeshes.ResourceFolder);
            if (!Directory.Exists(folder))
            {
                Skip("every generated mesh fits the reach assumed before it's looked up", "no mesh files on disk outside the editor");
                return;
            }
            int meshes = GeneratedMeshReach(folder, out float meshWorst, out string meshWorstFile, out int unreadable);
            Check("every generated mesh fits the reach assumed before it's looked up",
                meshes > 0 && meshWorst <= GroundItemRenderer.UnknownReach,
                meshes + " mesh files, the largest (" + meshWorstFile + ") reaching " + Num(meshWorst) + " m of "
                + Num(GroundItemRenderer.UnknownReach) + (unreadable > 0 ? "; " + unreadable + " couldn't be read" : ""));
        }

        // How far each generated mesh file's levels of detail reach, as the renderer culls them: half the box's diagonal (the
        // vertices lie in it), plus the height the mesh rests at, plus the bob. The layout is meshpost.py's (CZM2, version 3).
        private static int GeneratedMeshReach(string folder, out float worst, out string worstFile, out int unreadable)
        {
            worst = 0f;
            worstFile = "none";
            unreadable = 0;
            int files = 0;
            byte[] body = new byte[1 << 20];
            foreach (string file in Directory.GetFiles(folder, "*.bytes"))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(file);
                    if (data.Length < 28 || BitConverter.ToUInt32(data, 0) != 0x324D5A43 || BitConverter.ToUInt16(data, 4) != 3)
                    {
                        unreadable++;
                        continue;
                    }
                    int lods = BitConverter.ToUInt16(data, 6);
                    float baseHeight = BitConverter.ToSingle(data, 8);
                    uint flags = BitConverter.ToUInt32(data, 20);
                    int size = checked((int)BitConverter.ToUInt32(data, 24));
                    if (body.Length < size) body = new byte[Math.Max(size, body.Length * 2)];
                    int read = 0;
                    using (var packed = new MemoryStream(data, 28, data.Length - 28))
                    using (Stream source = (flags & 1) != 0 ? new DeflateStream(packed, CompressionMode.Decompress) : (Stream)packed)
                    {
                        while (read < size)
                        {
                            int got = source.Read(body, read, size - read);
                            if (got <= 0) break;
                            read += got;
                        }
                    }
                    if (read < size)
                    {
                        unreadable++;
                        continue;
                    }

                    // Per level of detail: vertex and index counts, the box's corner and size, 18 bytes a vertex, 2 an
                    // index, then padding to 4 bytes.
                    for (int lod = 0, at = 0; lod < lods; lod++)
                    {
                        if (at + 32 > size) throw new FormatException("the body ends early");
                        long vertices = BitConverter.ToUInt32(body, at), indices = BitConverter.ToUInt32(body, at + 4);
                        var box = new Vector3(BitConverter.ToSingle(body, at + 20), BitConverter.ToSingle(body, at + 24),
                            BitConverter.ToSingle(body, at + 28));
                        float reach = box.magnitude * 0.5f + Mathf.Abs(baseHeight) + GroundItemRenderer.BobHeight;
                        if (reach > worst)
                        {
                            worst = reach;
                            worstFile = Path.GetFileName(file) + " LOD " + lod;
                        }
                        at = checked((int)(at + 32 + vertices * 18 + indices * 2));
                        at += (4 - at % 4) % 4;
                    }
                    files++;
                }
                catch (Exception)
                {
                    unreadable++;
                }
            }
            return files;
        }

        // Half the size of a primitive at unit scale, along its own axes, at either level of detail.
        private static Vector3 UnitHalfSize(PartShape shape)
        {
            Vector3 h = Vector3.zero;
            foreach (ItemMeshBaker.Lod lod in new[] { ItemMeshBaker.Lod.Near, ItemMeshBaker.Lod.Far })
            {
                foreach (Vector3 v in PrimitiveMesh.For(shape, lod).Verts) h = Vector3.Max(h, new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)));
            }
            return h;
        }

        private static void SpawnGroundItem(ItemDef def, Vector3 at, System.Random rng, List<(long, uint)> spawned)
        {
            uint id = GroundItemStore.Add(def, at, 1, yawDegrees: (float)(rng.NextDouble() * 360.0));
            if (id != 0) spawned.Add((GroundItemStore.KeyOf(at.x, at.z), id));
        }

        // GeneratedItemMeshes' cache: an item's id is in it once anything has asked for its meshes (and loaded them, if any).
        private static IDictionary MeshesAskedFor() =>
            typeof(GeneratedItemMeshes).GetField("s_Items", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary;

        // A NavMesh point in the draw range with room for the pile, and for an item reaching UnknownReach from it, wholly out
        // of view.
        private static bool FindHiddenSpot(Vector3 eye, Plane[] planes, float range, out Vector3 spot)
        {
            float clear = GroundItemRenderer.UnknownReach + GroundPileSpread + 1f;
            for (float d = 25f; d <= range - 5f; d += 5f)
            {
                for (int a = 0; a < 24; a++)
                {
                    float angle = a * 15f * Mathf.Deg2Rad;
                    var probe = new Vector3(eye.x + Mathf.Sin(angle) * d, eye.y, eye.z + Mathf.Cos(angle) * d);
                    if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, 4f, NavMesh.AllAreas)) continue;
                    if (Flat(hit.position - eye).magnitude > range - 5f) continue;
                    var ground = new Vector3(hit.position.x, 0f, hit.position.z);
                    if (GeometryUtility.TestPlanesAABB(planes, new Bounds(ground, Vector3.one * (clear * 2f)))) continue;
                    spot = hit.position;
                    return true;
                }
            }
            spot = default;
            return false;
        }

        private static (int, int, int, int) ChunkWindow(Vector3 eye, float range)
        {
            float size = GroundItemStore.ChunkSize;
            return (Mathf.FloorToInt((eye.x - range) / size), Mathf.FloorToInt((eye.x + range) / size),
                Mathf.FloorToInt((eye.z - range) / size), Mathf.FloorToInt((eye.z + range) / size));
        }

        private static List<long> ChunksWithItems(Vector3 eye, float range)
        {
            var chunks = new List<GroundItemStore.Chunk>();
            GroundItemStore.ChunksInRange(eye, range, chunks);
            var keys = new List<long>(chunks.Count);
            foreach (GroundItemStore.Chunk c in chunks) keys.Add(c.Key);
            return keys;
        }

        private static int ItemsInRange(Vector3 eye, float range)
        {
            var chunks = new List<GroundItemStore.Chunk>();
            GroundItemStore.ChunksInRange(eye, range, chunks);
            int n = 0;
            foreach (GroundItemStore.Chunk c in chunks)
            {
                foreach (GroundItemStore.Item it in c.Items)
                {
                    float dx = it.X - eye.x, dz = it.Z - eye.z;
                    if (dx * dx + dz * dz <= range * range) n++;
                }
            }
            return n;
        }

        private static bool DrawnAt(GroundItemRenderer gir, Vector3 at)
        {
            bool found = false;
            gir.VisitDrawn((m, mesh, def) => { if (m.m03 == at.x && m.m23 == at.z) found = true; });
            return found;
        }

        private static double Ms(Recorder r) => r.isValid ? r.elapsedNanoseconds / 1e6 : 0.0;

        private static string Num4(double v) => v.ToString("0.0000", CultureInfo.InvariantCulture);

        private static double Quantile(List<double> v, double q)
        {
            if (v.Count == 0) return 0.0;
            var sorted = new List<double>(v);
            sorted.Sort();
            return sorted[Mathf.Clamp((int)Math.Ceiling(q * sorted.Count) - 1, 0, sorted.Count - 1)];
        }

        // What the renderer drew in its latest frame, against what the old gather makes of the same frame (the same eye, view
        // and time): everything the old one would draw is drawn, under the same mesh and with the same matrix, and nothing else.
        private void CompareWithOldGather(GroundItemRenderer gir, string when)
        {
            var drawn = new List<(Matrix4x4 m, Mesh mesh, ItemDef def)>(4096);
            gir.VisitDrawn((m, mesh, def) => drawn.Add((m, mesh, def)));
            var planes = new Plane[6];
            gir.CopyFrustum(planes);
            var reference = new List<OldItem>(8192);
            new OldGather().Run(gir.Eye, planes, gir.AnimatedAt, gir.DrawRadius, gir.NearRadius, gir.Animate, reference);

            var byPlace = new Dictionary<(Mesh, float, float), List<int>>(drawn.Count);
            for (int i = 0; i < drawn.Count; i++)
            {
                var key = (drawn[i].mesh, drawn[i].m.m03, drawn[i].m.m23);
                if (!byPlace.TryGetValue(key, out List<int> list)) byPlace[key] = list = new List<int>(1);
                list.Add(i);
            }

            var used = new bool[drawn.Count];
            int shown = 0, culled = 0, edge = 0, missing = 0, extra = 0, posed = 0;
            float worst = 0f;
            string first = null;
            foreach (OldItem o in reference)
            {
                bool show = o.Margin >= 0f, onEdge = Mathf.Abs(o.Margin) < GroundEdge;
                if (onEdge) edge++;
                int found = -1;
                float diff = float.MaxValue;
                if (byPlace.TryGetValue((o.Mesh, o.Matrix.m03, o.Matrix.m23), out List<int> here))
                {
                    foreach (int i in here)
                    {
                        if (used[i]) continue;
                        float d = MatrixDifference(drawn[i].m, o.Matrix);
                        if (d < diff) { diff = d; found = i; }
                    }
                }

                if (found >= 0)
                {
                    used[found] = true;
                    if (!show && !onEdge) { extra++; first = first ?? o.Def.Id + " drawn though out of view"; continue; }
                    shown++;
                    worst = Mathf.Max(worst, diff);
                    if (diff > GroundPose) { posed++; first = first ?? o.Def.Id + " posed " + Num4(diff) + " off"; }
                }
                else if (show && !onEdge) { missing++; first = first ?? o.Def.Id + " missing at " + o.Matrix.GetColumn(3); }
                else culled++;
            }
            for (int i = 0; i < drawn.Count; i++)
            {
                if (used[i]) continue;
                extra++;
                first = first ?? drawn[i].def.Id + " drawn with mesh " + drawn[i].mesh.name + " at " + drawn[i].m.GetColumn(3)
                    + ", which the old gather doesn't draw there";
            }

            Check("draws what the old gather would, " + when, missing == 0 && extra == 0 && posed == 0 && reference.Count > 0,
                (gir.ViewReusedLastFrame ? "a frame moving what was in view: " : "a frame gathered afresh: ") + shown + " drawn, "
                + culled + " culled, " + edge + " on the view's edge; " + missing + " missing, " + extra + " extra, "
                + posed + " posed differently (largest difference " + worst.ToString("0.000000", CultureInfo.InvariantCulture) + ")"
                + (first != null ? "; first: " + first : ""));
        }

        private static float MatrixDifference(Matrix4x4 a, Matrix4x4 b)
        {
            float d = 0f;
            for (int i = 0; i < 16; i++) d = Mathf.Max(d, Mathf.Abs(a[i] - b[i]));
            return d;
        }

        // One item as the old gather saw it: its matrix, its mesh, and how far inside the view its bounds reach (below 0, culled).
        private struct OldItem
        {
            public ItemDef Def;
            public Mesh Mesh;
            public Matrix4x4 Matrix;
            public float Margin;
        }

        /// <summary>
        /// The ground items' gather as <see cref="GroundItemRenderer"/> did it before it kept anything between frames, step for
        /// step: every chunk in range, every item in them, its looks and mesh looked up (loading them if need be), then culled,
        /// and its matrix made and filed under its type, look and level of detail. The one change is the bob's phase, worked out
        /// in double precision: in float it ran out of precision for items with large ids, which the renderer no longer does.
        /// </summary>
        private sealed class OldGather
        {
            private readonly List<GroundItemStore.Chunk> _chunks = new List<GroundItemStore.Chunk>(64);
            private readonly Dictionary<int, List<Matrix4x4>> _groups = new Dictionary<int, List<Matrix4x4>>(64);
            private readonly List<int> _liveGroups = new List<int>(64);
            private uint[] _typeHashes = new uint[256];

            public void Run(Vector3 eye, Plane[] frustum, double time, float drawRadius, float nearRadius, bool animate, List<OldItem> record)
            {
                for (int i = 0; i < _liveGroups.Count; i++) _groups[_liveGroups[i]].Clear();
                _liveGroups.Clear();
                _chunks.Clear();

                GroundItemStore.ChunksInRange(eye, drawRadius, _chunks);

                float drawSqr = drawRadius * drawRadius;
                float nearSqr = nearRadius * nearRadius;
                float t = (float)time;

                for (int c = 0; c < _chunks.Count; c++)
                {
                    List<GroundItemStore.Item> items = _chunks[c].Items;

                    for (int i = 0; i < items.Count; i++)
                    {
                        GroundItemStore.Item item = items[i];

                        float dx = item.X - eye.x;
                        float dz = item.Z - eye.z;
                        float d2 = dx * dx + dz * dz;
                        if (d2 > drawSqr) continue;

                        ItemDef def = GroundItemStore.TypeAt(item.TypeIndex);
                        if (def == null) continue;

                        ItemMeshBaker.Lod lod = d2 <= nearSqr ? ItemMeshBaker.Lod.Near : ItemMeshBaker.Lod.Far;
                        int looks = ItemMeshBaker.VariantCount(def);
                        int variant = looks > 1 ? (int)(VariantHash(item, def) % (uint)looks) : 0;
                        ItemMeshBaker.Baked baked = ItemMeshBaker.Get(def, lod, variant);
                        if (baked == null) continue;

                        float height = baked.BaseHeight;
                        float yaw = item.YawDegrees;

                        if (animate)
                        {
                            float phase = item.Id * 0.61803f;
                            yaw += t * 60f;
                            height += (float)Math.Sin((time + phase) * 2.0) * 0.06f;
                        }

                        var centre = new Vector3(item.X, height, item.Z);
                        float radius = baked.Bounds.extents.magnitude;
                        if (record != null)
                        {
                            float margin = float.MaxValue;
                            for (int p = 0; p < 6; p++) margin = Mathf.Min(margin, frustum[p].GetDistanceToPoint(centre) + radius);
                            record.Add(new OldItem
                            {
                                Def = def,
                                Mesh = baked.Mesh,
                                Matrix = Matrix4x4.TRS(centre, Quaternion.Euler(0f, yaw, 0f), Vector3.one),
                                Margin = margin,
                            });
                        }
                        if (!InView(frustum, centre, radius)) continue;

                        var trs = Matrix4x4.TRS(centre, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
                        int key = ((item.TypeIndex * GeneratedItemMeshes.MaxVariants + variant) << 1) | (int)lod;
                        if (!_groups.TryGetValue(key, out List<Matrix4x4> list))
                        {
                            list = new List<Matrix4x4>(64);
                            _groups[key] = list;
                        }
                        if (list.Count == 0) _liveGroups.Add(key);
                        list.Add(trs);
                    }
                }
            }

            private uint VariantHash(in GroundItemStore.Item item, ItemDef def)
            {
                uint h = TypeHash(item.TypeIndex, def);
                h ^= (uint)Mathf.RoundToInt(item.X * 4f) * 0x9E3779B1u;
                h ^= (uint)Mathf.RoundToInt(item.Z * 4f) * 0x85EBCA77u;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return h;
            }

            private uint TypeHash(int typeIndex, ItemDef def)
            {
                if (typeIndex >= _typeHashes.Length) Array.Resize(ref _typeHashes, Mathf.Max(typeIndex + 1, _typeHashes.Length * 2));
                uint h = _typeHashes[typeIndex];
                if (h != 0) return h;
                h = 2166136261u;
                foreach (char c in def.Id) h = (h ^ c) * 16777619u;
                if (h == 0) h = 1;
                _typeHashes[typeIndex] = h;
                return h;
            }

            private static bool InView(Plane[] frustum, Vector3 centre, float radius)
            {
                for (int p = 0; p < 6; p++)
                {
                    if (frustum[p].GetDistanceToPoint(centre) < -radius) return false;
                }
                return true;
            }
        }
    }
}
