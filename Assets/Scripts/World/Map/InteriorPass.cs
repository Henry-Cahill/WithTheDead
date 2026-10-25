using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The insides of the real-world map's enterable buildings: what <see cref="BuildingPass"/> lists in
    /// <see cref="Site.Enterables"/> (footprint, storeys, doors, windows), furnished. Each building's ground floor is laid
    /// out in rooms on its footprint's own axes by what it was for (<see cref="MakePlan"/>, from
    /// <see cref="MapGameplay.PurposeOf(Site, Footprint)"/>: houses, trailers, flats, shops, diners, offices, banks, clinics,
    /// the jail, the firehouse, churches, schools, warehouses, barns...), with its rooms' loot pools and counts, lamps and
    /// lit rooms settled there and then (<see cref="Budget"/>); its outside walls lined room by room with paper, skirting
    /// and window dressing (into the shell's wall meshes, so they cut away with them); and its rooms raised and furnished
    /// with the raid town's working pieces (<see cref="InteriorBuild"/>). The fixed square furnishes every building as
    /// it's built; a streamed piece of the state only plans its buildings and leaves the plans with the
    /// <see cref="InteriorStreamer"/>, which furnishes the ones near a survivor a few milliseconds a frame and takes them
    /// down again behind them. A building whose outline holds no room-sized rectangle gets loose loot
    /// (<see cref="MapGameplay.Loot"/>).
    /// </summary>
    internal static partial class InteriorPass
    {
        private const int SaltLoot = 0x1A73;

        /// <summary>The last fixed-square build's tally (for the logs and the harness).</summary>
        internal static string LastStats = "";

        /// <summary>The longest single piece of planning work since <see cref="ResetPlanTiming"/> (ms) and what it was (for
        /// the harness: a streamed piece's plan steps must stay within a frame's budget).</summary>
        internal static double SlowestPlanMs;
        internal static string SlowestPlanWhat = "";

        internal static void ResetPlanTiming()
        {
            SlowestPlanMs = 0;
            SlowestPlanWhat = "";
        }

        private static void NotePlan(long t0, string what, InteriorPlan plan = null)
        {
            double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            if (ms <= SlowestPlanMs) return;
            SlowestPlanMs = ms;
            SlowestPlanWhat = plan == null ? what
                : what + " " + plan.Use + " " + plan.Id + " (" + Mathf.RoundToInt(plan.Footprint.Size) + " m², " + plan.Rooms.Count + " rooms, "
                    + plan.Footprint.Pts.Count + " corners, " + plan.Record.Windows.Count + " windows)";
        }

        /// <summary>For tools (the interiors harness): told of every building the fixed square furnished (or failed to:
        /// the exception), in order.</summary>
        internal static System.Action<InteriorBuild, System.Exception> Furnished;

        /// <summary>For tools (the interiors harness): generates interior plans for each building, furnishes them, and yields progress strings.</summary>
        internal static IEnumerable<string> Build(Site site)
        {
            // Exit early for lightweight map detail or when there are no enterable buildings
            if (site.Detail == Detail.Lite || site.Enterables.Count == 0) yield break;
            // Record start time to later compute total processing duration for logging
            float started = Time.realtimeSinceStartup;
            // What each building was for (its point of interest, its surroundings), as the loot tables will see it.
            // Determine each building's purpose for loot table look‑ups
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            MapGameplay.Prepare(site);
            NotePlan(t0, "the purposes (MapGameplay.Prepare)");
            if (site.StepDue) yield return "interior plans";
            // Populate a dictionary of building shells for quick lookup by footprint
            var shells = new Dictionary<Footprint, BuildingShell>(site.Shells.Count);
            foreach (BuildingShell s in site.Shells)
                if (s != null && s.Footprint != null && !shells.ContainsKey(s.Footprint)) shells[s.Footprint] = s;
            var loose = new List<LootContainer>();
            if (site.Loot != null) site.Loot.GetComponentsInChildren(true, loose);

            var plans = new List<InteriorPlan>(site.Enterables.Count);
            int fallback = 0, rooms = 0;
            // Iterate over each enterable building to generate an interior plan
            for (int i = 0; i < site.Enterables.Count; i++)
            {
                EnterableBuilding e = site.Enterables[i];
                if (e == null || e.Footprint == null) continue;
                InteriorPlan plan;
                t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                try { plan = MakePlan(site, e); }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[CloneZ] Interiors: no plan for building " + e.Footprint.Data.id + ": " + ex);
                    plan = null;
                }
                // If a plan succeeded, record its timing for the slowest‑plan statistics
                if (plan != null) NotePlan(t0, "the plan of", plan);
                // The exteriors' loose crates in a building that's furnished make way for its furniture; a building that
                // can't be furnished keeps them, or gets its own.
                // Count and optionally remove loose containers that sit inside this building's footprint
                int crates = LooseIn(site, loose, e.Footprint, plan != null);
                // If no interior plan could be made…
                if (plan == null)
                {
                    // …and there were no crates to keep (i.e. building is empty), drop a fallback loot container
                    if (crates == 0) Fallback(site, e);
                    fallback++;
                }
                else
                {
                    // With a valid plan, try to retrieve its building shell for wall lining
                    if (shells.TryGetValue(e.Footprint, out BuildingShell shell))
                    {
                        // Lined a wall at a time, so a big building's many windows don't hold up a frame.
                        if (site.StepDue) yield return "interior plans";
                        IEnumerator<bool> lining = Linings(plan, shell).GetEnumerator();
                        // Iterate wall lining steps; each iteration may yield progress for streamed pieces
                        while (true)
                        {
                            // Measure time for making the plan – used for performance diagnostics
                            t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                            // Attempt next wall‑lining step, catching errors to avoid aborting the whole interior pass
                            bool more;
                            try { more = lining.MoveNext(); }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning("[CloneZ] Interiors: lining building " + plan.Id + " failed: " + ex);
                                more = false;
                            }
                            NotePlan(t0, "a wall's lining of", plan);
                            // If wall‑lining finished, exit the loop; otherwise possibly yield a step for streamed builds
                            if (!more) break;
                            if (site.StepDue) yield return "interior plans";
                        }
                    }
                    // Add the successful plan to the list for later furnishing
                    plans.Add(plan);
                    // Accumulate total room count for final statistics
                    rooms += plan.Rooms.Count;
                }
                if (site.StepDue) yield return "interior plans";
            }

            if (site.Chunked)
            {
                // A streamed piece: furnished near the survivors, a building at a time, by the streamer (in play, or for a
                // tool that asks; a piece built for the map harness stays unfurnished).
                // For streamed pieces, register plans with the interior streamer if needed, then exit early – streaming builds later
                if (plans.Count > 0 && InteriorStreamer.Wanted) InteriorStreamer.Register(site, plans);
                yield break;
            }

            // The fixed square: every building furnished now.
            // Initialise counters for the final reporting of fixed‑square furnishing
            int built = 0, containers = 0, lights = 0, failed = 0;
            foreach (InteriorPlan plan in plans)
            {
                // Create a builder for this plan and step through its stages, yielding progress strings for the harness
                var build = new InteriorBuild(plan, site.Town.World, site.Town.Overhead, false);
                System.Exception error = null;
                while (true)
                {
                    // Record timing for each build step for the slowest‑plan diagnostics
                    bool more;
                    try { more = build.Advance(); }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("[CloneZ] Interiors: furnishing building " + plan.Id + " (" + plan.Use + ") failed: " + ex);
                        error = ex;
                        break;
                    }
                    // If the builder reports a step, forward its progress string to the harness
                    if (!more) break;
                    yield return build.Current;
                }
                // Ensure a completed build actually reached its end; if not, treat as an error
                if (error == null && !build.Done) error = new System.InvalidOperationException("the build stopped before it was finished");
                Furnished?.Invoke(build, error);
                if (error != null)
                {
                    build.Discard();
                    Fallback(site, plan.Record);
                    failed++;
                    continue;
                }
                // Record successful build stats for final report
                built++;
                containers += build.Containers.Count;
                lights += build.Lights;
            }
            // After all buildings processed, update site-wide container & light counts and compose a summary string
            site.Containers += containers;
            site.Town.LightCount += lights;
            LastStats = "Interiors: " + built + " buildings furnished (" + rooms + " rooms), " + fallback + " unplannable, " + failed + " failed, "
                + containers + " containers, " + lights + " lights, " + Mathf.RoundToInt((Time.realtimeSinceStartup - started) * 1000f) + " ms";
            Debug.Log("[CloneZ] " + LastStats);
        }

        // Counts the loose containers standing in a footprint, and takes them away when it's furnished.
        /// <summary>Counts the loose containers standing in a footprint, and removes them when the building is furnished.</summary>
        private static int LooseIn(Site site, List<LootContainer> loose, Footprint f, bool furnished)
        {
            int n = 0;
            Rect bounds = f.Bounds;
            // Walk backwards through the loose‑container list so removal doesn’t invalidate the index
            for (int i = loose.Count - 1; i >= 0; i--)
            {
                LootContainer box = loose[i];
                // Skip null entries (shouldn’t happen) and remove them from the list
                if (box == null) { loose.RemoveAt(i); continue; }
                Vector3 p = box.transform.position;
                var q = new Vector2(p.x, p.z);
                // Only consider containers whose XZ position lies within the footprint’s bounds and polygon
                if (!bounds.Contains(q) || !MapGeometry.Contains(f.Pts, q)) continue;
                n++;
                // If the building is being furnished, remove the container from the scene and update counts
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

        /// <summary>Loose loot for a planned building whose furnishing failed after its site was gone (the streamer's): a
        /// container of its signature kind in the middle of each room its plan gives loot (six at most), under
        /// <paramref name="parent"/>.</summary>
        internal static void LooseLoot(InteriorPlan plan, Transform parent, Vector3 offset)
        {
            var rng = new System.Random(unchecked(plan.Seed * 31 + SaltLoot));
            int placed = 0;
            foreach (PlanRoom r in plan.Rooms)
            {
                if (placed >= 6) break;
                if (r.Loot <= 0 || r.Pool == null || r.Pool.Length == 0 || r.R.width < 2f || r.R.height < 2f) continue;
                Vector2 at = plan.ToSite(r.R.center);
                LootContainer.Spawn(parent, r.Pool[0], new Vector3(at.x + offset.x, 0f, at.y + offset.z), rng.Next());
                placed++;
            }
        }

        /// <summary>Destroys <paramref name="o"/>: at the end of the frame in play, at once in edit mode (the harness), where
        /// Destroy isn't allowed.</summary>
        internal static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
