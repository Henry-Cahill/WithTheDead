using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The insides of the real-world map's enterable buildings: what <see cref="BuildingPass"/> lists in
    /// <see cref="Site.Enterables"/> (footprint, storeys, doors, windows), furnished. Each building's ground floor is laid
    /// out in rooms on its footprint's own axes by what it's used as (<see cref="MakePlan"/>: houses, trailers, flats,
    /// shops, diners, offices, banks, clinics, the jail, the firehouse, churches, schools, warehouses, barns...), its outside
    /// walls lined room by room with paper, skirting and window dressing (into the shell's wall meshes, so they cut away
    /// with them), and its rooms raised and furnished with the raid town's working pieces (<see cref="InteriorBuild"/>).
    /// The fixed square furnishes every building as it's built; a streamed piece of the state leaves its buildings' plans
    /// with the <see cref="InteriorStreamer"/>, which furnishes the ones near a survivor and takes them down again behind
    /// them. A building whose outline holds no room-sized rectangle gets loose loot (<see cref="MapGameplay.Loot"/>).
    /// </summary>
    internal static partial class InteriorPass
    {
        private const int SaltLoot = 0x1A73;

        /// <summary>The last fixed-square build's tally (for the logs and the harness).</summary>
        internal static string LastStats = "";

        internal static IEnumerable<string> Build(Site site)
        {
            if (site.Detail == Detail.Lite || site.Enterables.Count == 0) yield break;
            float started = Time.realtimeSinceStartup;
            var shells = new Dictionary<Footprint, BuildingShell>(site.Shells.Count);
            foreach (BuildingShell s in site.Shells)
                if (s != null && s.Footprint != null && !shells.ContainsKey(s.Footprint)) shells[s.Footprint] = s;
            var loose = new List<LootContainer>();
            if (site.Loot != null) site.Loot.GetComponentsInChildren(true, loose);

            var plans = new List<InteriorPlan>(site.Enterables.Count);
            int fallback = 0, rooms = 0;
            for (int i = 0; i < site.Enterables.Count; i++)
            {
                EnterableBuilding e = site.Enterables[i];
                if (e == null || e.Footprint == null) continue;
                InteriorPlan plan = null;
                try { plan = MakePlan(site, e); }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[CloneZ] Interiors: no plan for building " + e.Footprint.Data.id + ": " + ex.Message);
                    plan = null;
                }
                // The exteriors' loose crates in a building that's furnished make way for its furniture; a building that
                // can't be furnished keeps them, or gets its own.
                int crates = LooseIn(site, loose, e.Footprint, plan != null);
                if (plan == null)
                {
                    if (crates == 0) Fallback(site, e);
                    fallback++;
                    continue;
                }
                if (shells.TryGetValue(e.Footprint, out BuildingShell shell))
                {
                    try { Linings(plan, shell); }
                    catch (System.Exception ex) { Debug.LogWarning("[CloneZ] Interiors: lining building " + plan.Id + " failed: " + ex.Message); }
                }
                plans.Add(plan);
                rooms += plan.Rooms.Count;
                if ((i & 15) == 15) yield return "interior plans";
            }

            if (site.Chunked)
            {
                // A streamed piece: furnished near the survivors, a building at a time (in play only: a piece built for
                // the harness stays unfurnished).
                if (Application.isPlaying && plans.Count > 0) InteriorStreamer.Register(site, plans);
                yield break;
            }

            // The fixed square: every building furnished now.
            int built = 0, containers = 0, lights = 0, failed = 0;
            foreach (InteriorPlan plan in plans)
            {
                var build = new InteriorBuild(plan, site.Town.World, site.Town.Overhead, false);
                IEnumerator<string> steps = build.Steps().GetEnumerator();
                bool ok = true;
                while (true)
                {
                    string step;
                    try
                    {
                        if (!steps.MoveNext()) break;
                        step = steps.Current;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("[CloneZ] Interiors: furnishing building " + plan.Id + " (" + plan.Use + ") failed: " + ex);
                        ok = false;
                        break;
                    }
                    yield return step;
                }
                if (!ok || !build.Done)
                {
                    build.Discard();
                    Fallback(site, plan.Record);
                    failed++;
                    continue;
                }
                built++;
                containers += build.Containers.Count;
                lights += build.Lights;
            }
            site.Containers += containers;
            site.Town.LightCount += lights;
            LastStats = "Interiors: " + built + " buildings furnished (" + rooms + " rooms), " + fallback + " unplannable, " + failed + " failed, "
                + containers + " containers, " + lights + " lights, " + Mathf.RoundToInt((Time.realtimeSinceStartup - started) * 1000f) + " ms";
            Debug.Log("[CloneZ] " + LastStats);
        }

        // Counts the loose containers standing in a footprint, and takes them away when it's furnished.
        private static int LooseIn(Site site, List<LootContainer> loose, Footprint f, bool furnished)
        {
            int n = 0;
            Rect bounds = f.Bounds;
            for (int i = loose.Count - 1; i >= 0; i--)
            {
                LootContainer box = loose[i];
                if (box == null) { loose.RemoveAt(i); continue; }
                Vector3 p = box.transform.position;
                var q = new Vector2(p.x, p.z);
                if (!bounds.Contains(q) || !MapGeometry.Contains(f.Pts, q)) continue;
                n++;
                if (!furnished) continue;
                loose.RemoveAt(i);
                Object.DestroyImmediate(box.gameObject);
                site.Containers--;
            }
            return n;
        }

        // Loose loot for a building nothing could be laid out in.
        private static void Fallback(Site site, EnterableBuilding e)
        {
            Vector2 door = e.Doors.Count > 0 ? e.Doors[0].At : e.Footprint.Middle;
            MapGameplay.Loot(site, e.Footprint, new System.Random(unchecked(e.Seed * 31 + SaltLoot)), door);
        }
    }
}
