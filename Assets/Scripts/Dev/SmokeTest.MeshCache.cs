using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;

namespace CloneZ
{
    /// <summary>
    /// <c>meshcache</c>: the generated item meshes' cache (<see cref="GeneratedItemMeshes"/>) on a long walk across the
    /// statewide map (Kentucky, or the zone named with <c>-clonez-smoke-zone</c>). The survivor goes east across the state
    /// <see cref="CacheHop"/> m at a time, then a kilometre south and back west, while the map streams in around them. At
    /// every stop it drops <see cref="CacheCopies"/> each of <see cref="CacheKindsPerStop"/> kinds with generated meshes that
    /// nothing has asked for yet, close by and in view, so the ground-item renderer reads them in. It walks until the cache
    /// has read in three times its budget (at least <see cref="CacheMinStops"/> stops). Then it:
    /// <list type="bullet">
    /// <item>logs, at every stop, what the cache holds against its budget, what it has read in (what it would hold without a
    /// budget) and how many items it has let go of. Beside them go the generated meshes, maps and materials the engine has
    /// in memory, found by name and sized by the profiler. With <c>-clonez-smoke-shots &lt;dir&gt;</c> they go to
    /// meshcache.csv there;</item>
    /// <item>fails if the cache is over its budget at any stop by more than what it holds in use
    /// (<see cref="GeneratedItemMeshes.BytesInUse"/>), or if nothing was let go of. It also fails if the generated meshes,
    /// maps and materials in memory reach higher in the walk's second half than in its first: the memory has to level
    /// off;</item>
    /// <item>fails unless every item dropped at a stop is drawn there, and every kind dropped has a near map and a near
    /// material in memory;</item>
    /// <item>holds a generated item in a character's hand all the way, and fails if its model was let go of;</item>
    /// <item>goes back to the first stop and fails unless its items, let go of on the way, are read in and drawn again;</item>
    /// <item>puts the hand's item down and squeezes the budget to nothing. It fails unless everything idle goes, the hand's
    /// item with it, while what's in view stays drawn with the same meshes.</item>
    /// </list>
    /// <c>-clonez-smoke-budget &lt;MB&gt;</c> sets the cache's budget for the run; one smaller than a stop's own items
    /// leaves the cache holding just what's in use. The renderer needs a graphics device, so leave out <c>-nographics</c>.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private const int CacheKindsPerStop = 8, CacheCopies = 3, CacheMinStops = 16, CacheMaxStops = 72, CacheLeg = 36;
        private const float CacheHop = 250f, CacheSpread = 9f;

        // Frames, and seconds, the renderer gets to read a stop's items in and draw them before they're counted.
        private const int CacheSettleFrames = 10;
        private const float CacheSettleSeconds = 0.5f;

        // How far inside every plane of the view a dropped item's middle is put, so it's drawn whatever its size.
        private const float CacheInView = 0.5f;

        // A generated item mesh's map and material, as they're named: "<item id>@<version>_<map>" (tex0, nrm0 and msk0 for
        // the near level), and "ItemMesh_<item id>@<version>_<level of detail>".
        private static readonly Regex s_GeneratedMap = new Regex(@"^(?<id>[^@]+)@\d+_(?<map>tex\d|nrm0|msk0)$", RegexOptions.CultureInvariant);
        private static readonly Regex s_GeneratedMaterial = new Regex(@"^ItemMesh_(?<id>[^@]+)@\d+_(?<lod>\d)$", RegexOptions.CultureInvariant);

        // The cache at one stop, beside what the engine has in memory.
        private struct CacheSample
        {
            public long Cache, InUse, Read, MeshBytes, MapBytes, MaterialBytes;
            public int Evictions, Meshes, Maps, Materials;
            public int Short;          // kinds dropped at the stop without a near map or a near material in memory
            public string ShortFirst;  // the first of them, and what it lacks
        }

        private IEnumerator MeshCacheRun()
        {
            yield return ToHub();
            yield return Deploy(RaidKind.Operative, ZoneCatalog.FeaturedZone);
            KentuckyStreamer streamer = KentuckyStreamer.Active;
            GroundItemRenderer gir = GroundItemRenderer.Instance;
            Actor me = Actor.LocalPlayer;
            bool ready = streamer != null && gir != null && gir.isActiveAndEnabled && SystemInfo.supportsInstancing && me != null;
            Check("meshcache: a streaming map, and the ground-item renderer drawing", ready,
                streamer == null ? "no statewide map streaming (deploy into bardstown)" : gir == null ? "no renderer"
                : !SystemInfo.supportsInstancing ? "this device can't instance (leave out -nographics)" : me == null ? "no survivor"
                : streamer.Summary);
            if (!ready) yield break;
            bool untargetable = me.Untargetable;
            me.Untargetable = true;   // the dead may come; this walk isn't about them biting

            if (float.TryParse(ArgAfter(Environment.GetCommandLineArgs(), "-clonez-smoke-budget"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float budgetMb) && budgetMb > 0f)
                GeneratedItemMeshes.Budget = (long)(budgetMb * 1024.0 * 1024.0);
            long budget = GeneratedItemMeshes.Budget;

            // Kinds with generated meshes that nothing has read in yet: one for the hand, the rest a few to a stop.
            List<ItemDef> kinds = CacheKinds(CacheMaxStops * CacheKindsPerStop + 1);
            bool enough = kinds.Count > CacheMinStops * CacheKindsPerStop;
            Check("meshcache: kinds with generated meshes nothing has read in yet", enough, kinds.Count + " found");
            if (!enough)
            {
                me.Untargetable = untargetable;
                yield break;
            }

            // A hand holding a generated item all the way. Nothing else draws it, so only the hold keeps it.
            ItemDef handKind = kinds[0];
            kinds.RemoveAt(0);
            HumanoidRig hand = HumanoidRig.Create(transform, HumanoidLook.Survivor(), "Smoke hand");
            hand.transform.position = new Vector3(0f, -50f, 0f);   // out of the way
            hand.SetHeldItem(handKind, handKind.IsFirearm ? HoldStyle.Firearm : handKind.IsWeapon ? HoldStyle.Melee : HoldStyle.Carry);
            Mesh handMesh = HeldMesh(hand);
            Check("meshcache: a hand holds a generated item", handMesh != null && handMesh.name.StartsWith("ItemMesh_", StringComparison.Ordinal),
                handKind.Id + (handMesh != null ? " as " + handMesh.name : " not held"));

            Vector3 home = me.transform.position;
            var rng = new System.Random(20261107);
            var planes = new Plane[6];
            var dropped = new List<(long, uint)>(CacheMaxStops * CacheKindsPerStop * CacheCopies);
            var firstSpots = new List<Vector3>();
            var firstKinds = new HashSet<ItemDef>();
            var samples = new List<CacheSample>(CacheMaxStops);
            var csv = new StringBuilder("stop,east_km,north_km,dropped,drawn,cache_mb,in_use_mb,budget_mb,read_mb,evictions,generation,"
                + "meshes,mesh_mb,maps,map_mb,materials,material_mb,all_textures_mb,graphics_driver_mb,allocated_mb\n");
            int next = 0, undrawnStops = 0, overStops = 0, shortStops = 0;
            string undrawn = null, over = null, shortOf = null;
            float walkStarted = Time.realtimeSinceStartup;

            for (int stop = 0; stop < CacheMaxStops && next + CacheKindsPerStop <= kinds.Count; stop++)
            {
                if (stop >= CacheMinStops && GeneratedItemMeshes.BytesRead >= 3 * budget) break;
                Vector3 at = CacheRoute(home, stop);
                float departed = Time.unscaledTime;   // the last stop's items go undrawn from here
                yield return CacheHopTo(streamer, me, at);

                gir.CopyFrustum(planes);
                List<Vector3> spots = CacheSpots(gir.Eye, planes, CacheKindsPerStop * CacheCopies, rng);
                var here = new HashSet<ItemDef>();
                int placed = 0;
                for (int i = 0; i < spots.Count; i++)
                {
                    ItemDef def = kinds[next + i % CacheKindsPerStop];
                    int before = dropped.Count;
                    SpawnGroundItem(def, spots[i], rng, dropped);
                    if (dropped.Count == before) continue;
                    placed++;
                    here.Add(def);
                    if (stop == 0) firstSpots.Add(spots[i]);
                }
                if (stop == 0) firstKinds.UnionWith(here);
                next += CacheKindsPerStop;

                // Frames for the renderer to read the new items in and draw them, and time enough since the last stop for
                // its items to have gone idle and been let go of, if the cache must.
                yield return CacheSettle(departed);

                int drawn = 0;
                gir.VisitDrawn((m, mesh, def) => { if (here.Contains(def)) drawn++; });
                if (drawn != placed || placed == 0)
                {
                    undrawnStops++;
                    undrawn = undrawn ?? "stop " + stop + ": " + drawn + " of " + placed + " drawn (" + spots.Count + " spots in view)";
                }

                CacheSample s = CacheMeasure(here);
                // Over budget is right only while all the cache holds is in use. Something that went idle a moment ago is
                // given the trim's retry before it counts.
                if (s.Cache > Math.Max(budget, s.InUse))
                {
                    yield return new WaitForSecondsRealtime(GeneratedItemMeshes.RetrySeconds + 0.5f);
                    s = CacheMeasure(here);
                }
                if (s.Cache > Math.Max(budget, s.InUse))
                {
                    overStops++;
                    over = over ?? "stop " + stop + ": " + Mb(s.Cache) + " MB, " + Mb(s.InUse) + " MB of it in use";
                }
                if (s.Short > 0)
                {
                    shortStops++;
                    shortOf = shortOf ?? "stop " + stop + ": " + s.Short + " of " + here.Count + " kinds, first " + s.ShortFirst;
                }
                samples.Add(s);
                Vector3 off = at - home;
                Debug.Log("[Smoke] meshcache stop " + stop + " (" + Num(off.x / 1000f) + " km east, " + Num(off.z / 1000f) + " km north): "
                    + placed + " items of " + here.Count + " new kinds dropped, " + drawn + " drawn; the cache " + Mb(s.Cache) + " of "
                    + Mb(budget) + " MB (" + Mb(s.InUse) + " MB in use), " + Mb(s.Read) + " MB read in, " + s.Evictions
                    + " items let go of; in memory " + s.Meshes + " generated meshes (" + Mb(s.MeshBytes) + " MB), " + s.Maps + " maps ("
                    + Mb(s.MapBytes) + " MB) and " + s.Materials + " materials (" + Mb(s.MaterialBytes) + " MB)");
                csv.Append(stop).Append(',').Append(Num4(off.x / 1000f)).Append(',').Append(Num4(off.z / 1000f)).Append(',')
                    .Append(placed).Append(',').Append(drawn).Append(',').Append(Mb(s.Cache)).Append(',').Append(Mb(s.InUse)).Append(',')
                    .Append(Mb(budget)).Append(',').Append(Mb(s.Read)).Append(',').Append(s.Evictions).Append(',').Append(ItemMeshBaker.Generation).Append(',')
                    .Append(s.Meshes).Append(',').Append(Mb(s.MeshBytes)).Append(',').Append(s.Maps).Append(',').Append(Mb(s.MapBytes)).Append(',')
                    .Append(s.Materials).Append(',').Append(Mb(s.MaterialBytes)).Append(',').Append(Mb((long)Texture.currentTextureMemory)).Append(',')
                    .Append(Mb(Profiler.GetAllocatedMemoryForGraphicsDriver())).Append(',').Append(Mb(Profiler.GetTotalAllocatedMemoryLong())).Append('\n');
            }

            int stops = samples.Count;
            long cacheMost = 0, firstHalf = 0, secondHalf = 0;
            int cacheMostAt = 0, materialsFirst = 0, materialsSecond = 0;
            for (int i = 0; i < stops; i++)
            {
                if (samples[i].Cache > cacheMost)
                {
                    cacheMost = samples[i].Cache;
                    cacheMostAt = i;
                }
                long inMemory = samples[i].MeshBytes + samples[i].MapBytes + samples[i].MaterialBytes;
                if (i < stops / 2)
                {
                    firstHalf = Math.Max(firstHalf, inMemory);
                    materialsFirst = Math.Max(materialsFirst, samples[i].Materials);
                }
                else
                {
                    secondHalf = Math.Max(secondHalf, inMemory);
                    materialsSecond = Math.Max(materialsSecond, samples[i].Materials);
                }
            }
            long read = GeneratedItemMeshes.BytesRead;
            Debug.Log("[Smoke] meshcache walked " + stops + " stops in " + Num((Time.realtimeSinceStartup - walkStarted) / 60f)
                + " min, " + Num(stops * CacheHop / 1000f) + " km: read in " + Mb(read) + " MB of " + next + " kinds against a budget of "
                + Mb(budget) + " MB; the cache held at most " + Mb(cacheMost) + " MB and let go of " + GeneratedItemMeshes.Evictions
                + " items; generated meshes, maps and materials in memory peaked at " + Mb(firstHalf) + " MB in the first half and "
                + Mb(secondHalf) + " MB in the second, the materials numbering " + materialsFirst + " and " + materialsSecond);
            Check("meshcache: every item dropped is drawn where it lies", undrawnStops == 0 && stops > 0,
                undrawnStops + " of " + stops + " stops short" + (undrawn != null ? "; first " + undrawn : ""));
            Check("meshcache: every kind dropped has its near maps and material in memory", shortStops == 0 && stops > 0,
                shortStops + " of " + stops + " stops short" + (shortOf != null ? "; first " + shortOf : ""));
            Check("meshcache: the cache keeps to its budget at every stop, but for what's in use", overStops == 0 && stops > 0,
                over ?? "at most " + Mb(cacheMost) + " MB (stop " + cacheMostAt + ") of " + Mb(budget) + " MB");
            Check("meshcache: the walk read in well over the budget, and the cache let go of what it had to",
                read > 2 * budget && GeneratedItemMeshes.Evictions > 0,
                Mb(read) + " MB read in, " + GeneratedItemMeshes.Evictions + " items let go of");
            Check("meshcache: the generated meshes, maps and materials in memory level off", stops >= CacheMinStops && firstHalf > 0
                && secondHalf <= firstHalf + firstHalf / 10 + 4L * 1024 * 1024 && materialsSecond <= materialsFirst + materialsFirst / 10 + 4,
                "the most over the first " + stops / 2 + " stops " + Mb(firstHalf) + " MB and " + materialsFirst + " materials, over the other "
                + (stops - stops / 2) + " " + Mb(secondHalf) + " MB and " + materialsSecond);

            Mesh heldNow = HeldMesh(hand);
            Check("meshcache: the hand's item stays, held, though nothing else draws it",
                handMesh != null && heldNow == handMesh && Cached(handKind),
                handMesh == null ? "its mesh was destroyed" : heldNow != handMesh ? "the hand holds another mesh now"
                : !Cached(handKind) ? "let go of by the cache" : handKind.Id + " kept");

            // Back to the first stop: its items were the first to go, and are read in and drawn again.
            int firstLetGo = 0;
            foreach (ItemDef def in firstKinds) if (!Cached(def)) firstLetGo++;
            Check("meshcache: the first stop's items were let go of on the way", firstKinds.Count > 0 && firstLetGo == firstKinds.Count,
                firstLetGo + " of " + firstKinds.Count + " kinds");
            float back = Time.unscaledTime;
            yield return CacheHopTo(streamer, me, home);
            yield return CacheSettle(back);
            gir.CopyFrustum(planes);
            int firstInView = 0;
            foreach (Vector3 p in firstSpots) if (CacheSpotInView(p, planes)) firstInView++;
            int firstDrawn = 0, firstCached = 0;
            var firstMeshes = new HashSet<Mesh>();
            gir.VisitDrawn((m, mesh, def) =>
            {
                if (!firstKinds.Contains(def)) return;
                firstDrawn++;
                firstMeshes.Add(mesh);
            });
            foreach (ItemDef def in firstKinds) if (Cached(def)) firstCached++;
            Check("meshcache: back at the first stop, its items are read in and drawn again",
                firstInView > 0 && firstDrawn >= firstInView && firstCached == firstKinds.Count,
                firstDrawn + " drawn of the " + firstInView + " in view (" + firstSpots.Count + " dropped); " + firstCached + " of "
                + firstKinds.Count + " kinds read in again");

            // The hand puts its item down, and the budget goes to nothing: everything idle goes, but not what's in view.
            Destroy(hand.gameObject);
            yield return null;
            int evictedBefore = GeneratedItemMeshes.Evictions;
            GeneratedItemMeshes.Budget = 1;
            yield return new WaitForSecondsRealtime(GeneratedItemMeshes.IdleSeconds + 2f);
            int squeezedDrawn = 0;
            var squeezedMeshes = new HashSet<Mesh>();
            gir.VisitDrawn((m, mesh, def) =>
            {
                if (!firstKinds.Contains(def)) return;
                squeezedDrawn++;
                squeezedMeshes.Add(mesh);
            });
            bool handGone = !Cached(handKind);
            bool viewKept = squeezedDrawn == firstDrawn && squeezedMeshes.SetEquals(firstMeshes);
            int squeezedOut = GeneratedItemMeshes.Evictions - evictedBefore;
            long left = GeneratedItemMeshes.Bytes;
            GeneratedItemMeshes.Budget = budget;
            Check("meshcache: squeezed to nothing, everything idle goes but what's in view stays", handGone && viewKept && squeezedOut > 0,
                squeezedOut + " items let go of, " + Mb(left) + " MB left; the hand's item " + (handGone ? "gone" : "still cached")
                + "; in view " + squeezedDrawn + " of " + firstDrawn + " drawn, " + (squeezedMeshes.SetEquals(firstMeshes)
                    ? "with the same meshes" : "with other meshes (read in again)"));

            if (s_ShotDir != null)
            {
                string file = Path.Combine(s_ShotDir, "meshcache.csv");
                try { File.WriteAllText(file, csv.ToString()); Debug.Log("[Smoke] meshcache stops written to " + file); }
                catch (Exception e) { Debug.LogWarning("[Smoke] meshcache.csv not written: " + e.Message); }
            }

            int removed = 0;
            foreach ((long key, uint id) in dropped) if (GroundItemStore.Remove(new GroundItemStore.Handle(key, -1, id))) removed++;
            Check("meshcache: the dropped items cleared away", removed == dropped.Count, removed + " of " + dropped.Count);
            me.Untargetable = untargetable;
        }

        // The walk: east from the start a hop at a time, then a kilometre south and back west.
        private static Vector3 CacheRoute(Vector3 home, int stop)
        {
            int leg = stop / CacheLeg, i = stop % CacheLeg;
            return home + new Vector3((leg % 2 == 0 ? i : CacheLeg - 1 - i) * CacheHop, 0f, -1000f * leg);
        }

        // Sets the survivor down at the stop once the map under it is built, and lets the camera catch up.
        private static IEnumerator CacheHopTo(KentuckyStreamer streamer, Actor me, Vector3 to)
        {
            to.y = 0.1f;
            Teleport(me, to);
            yield return Until(() => streamer.IsBuiltAt(to), 60f);
            Teleport(me, to);   // again, in case it dropped before the ground under it was built
            yield return new WaitForSeconds(1f);
        }

        // Some frames and time for the renderer to read in and draw what's just been dropped or come into view, and until
        // what was drawn up to `departed` has been idle long enough for the cache to have let it go (a trim that found
        // nothing to let go of looks again RetrySeconds later).
        private static IEnumerator CacheSettle(float departed)
        {
            int frame = Time.frameCount;
            float start = Time.unscaledTime;
            while (Time.frameCount - frame < CacheSettleFrames || Time.unscaledTime - start < CacheSettleSeconds
                || Time.unscaledTime - departed < GeneratedItemMeshes.IdleSeconds + GeneratedItemMeshes.RetrySeconds + 1f)
                yield return null;
        }

        // Points on the ground within CacheSpread of the eye, so drawn at the near level of detail and with every map, whose
        // items are in view.
        private static List<Vector3> CacheSpots(Vector3 eye, Plane[] planes, int count, System.Random rng)
        {
            var spots = new List<Vector3>(count);
            for (int tries = 0; spots.Count < count && tries < count * 100; tries++)
            {
                float r = CacheSpread * Mathf.Sqrt((float)rng.NextDouble());
                float a = (float)(rng.NextDouble() * Math.PI * 2.0);
                var p = new Vector3(eye.x + Mathf.Cos(a) * r, 0f, eye.z + Mathf.Sin(a) * r);
                if (CacheSpotInView(p, planes)) spots.Add(p);
            }
            return spots;
        }

        private static bool CacheSpotInView(Vector3 p, Plane[] planes)
        {
            for (int i = 0; i < 6; i++) if (planes[i].GetDistanceToPoint(p) < CacheInView) return false;
            return true;
        }

        // Up to `count` kinds with generated meshes that nothing has read in yet, in a fixed shuffled order.
        private static List<ItemDef> CacheKinds(int count)
        {
            var ids = new List<string>(ItemDatabase.AllIds);
            ids.Sort(StringComparer.Ordinal);
            var rng = new System.Random(1780);
            for (int i = ids.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (ids[i], ids[j]) = (ids[j], ids[i]);
            }
            IDictionary asked = MeshesAskedFor();
            var kinds = new List<ItemDef>(count);
            foreach (string id in ids)
            {
                if (kinds.Count >= count) break;
                ItemDef def = ItemDatabase.Get(id);
                if (def == null || (asked != null && asked.Contains(def.Id))) continue;
                TextAsset generated = Resources.Load<TextAsset>(GeneratedItemMeshes.ResourceFolder + "/" + def.Id + "@1");
                if (generated == null) continue;
                Resources.UnloadAsset(generated);
                kinds.Add(def);
            }
            return kinds;
        }

        // Whether the cache has the item's meshes (or knows it has none) without asking for them, which would read them in.
        private static bool Cached(ItemDef def)
        {
            IDictionary asked = MeshesAskedFor();
            return asked != null && asked.Contains(def.Id);
        }

        private static Mesh HeldMesh(HumanoidRig rig)
        {
            if (rig == null || rig.Held == null) return null;
            MeshFilter filter = rig.Held.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        // What the cache says it holds, beside the generated meshes, maps and materials the engine has in memory, found by
        // name and sized by the profiler (the editor's and development builds' figures). Also which of the kinds just
        // dropped (`here`) lack a near map or a near material: they're drawn close up, so they must have both.
        private static CacheSample CacheMeasure(HashSet<ItemDef> here)
        {
            var s = new CacheSample
            {
                Cache = GeneratedItemMeshes.Bytes,
                InUse = GeneratedItemMeshes.BytesInUse,
                Read = GeneratedItemMeshes.BytesRead,
                Evictions = GeneratedItemMeshes.Evictions,
            };
            foreach (Mesh m in Resources.FindObjectsOfTypeAll<Mesh>())
            {
                if (m == null || !m.name.StartsWith("ItemMesh_", StringComparison.Ordinal)) continue;
                s.Meshes++;
                s.MeshBytes += Profiler.GetRuntimeMemorySizeLong(m);
            }
            // Resources paths ignore case, and some items' files are named in another case than their ids.
            var nearMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Texture2D t in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                if (t == null) continue;
                Match match = s_GeneratedMap.Match(t.name);
                if (!match.Success) continue;
                s.Maps++;
                s.MapBytes += Profiler.GetRuntimeMemorySizeLong(t);
                if (match.Groups["map"].Value != "tex1") nearMaps.Add(match.Groups["id"].Value);
            }
            var nearMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Material m in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (m == null) continue;
                Match match = s_GeneratedMaterial.Match(m.name);
                if (!match.Success) continue;
                s.Materials++;
                s.MaterialBytes += Profiler.GetRuntimeMemorySizeLong(m);
                if (match.Groups["lod"].Value == "0") nearMaterials.Add(match.Groups["id"].Value);
            }
            foreach (ItemDef def in here)
            {
                bool map = nearMaps.Contains(def.Id), material = nearMaterials.Contains(def.Id);
                if (map && material) continue;
                s.Short++;
                s.ShortFirst = s.ShortFirst ?? def.Id + (map ? " has no near material" : material ? " has no near map" : " has neither");
            }
            return s;
        }

        private static string Mb(long bytes) => (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture);
    }
}
