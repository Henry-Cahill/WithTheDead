using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;

namespace CloneZ
{
    /// <summary>
    /// <c>horde</c>: the dead's visibility (<see cref="ZombieVisibility"/>) in a big horde. It deploys into the procedural
    /// town (or the zone named with <c>-clonez-smoke-zone</c>), fails if any of the world's solids were left on Default
    /// rather than moved onto the Sight Occluder layer (<see cref="SightOccluders"/>; a streamed map, which goes on fitting
    /// bits to its pieces, only logs them), sets the clock to noon (the survivor
    /// sees furthest by day) and raises <see cref="HordeSize"/> zombies on the NavMesh round the survivor, most of them in
    /// front (the worst case: all of those are in view), to mill about. Then it:
    /// <list type="bullet">
    /// <item>plays <see cref="HordeSeconds"/> of game time at each of 60, 30 and 20 frames a second (a fixed frame time,
    /// <c>Time.captureDeltaTime</c>, so a fast machine can't hide a slow frame rate), the horde kept at
    /// <see cref="HordeSize"/> (the dead that fall are replaced) and the survivor stepping back halfway through, and fails
    /// if any zombie in view went more than <see cref="ZombieVisibility.MaxCheckAge"/> between checks. It logs the checks
    /// and rays a frame, the ticks' CPU time from their profiler sample, and how long the old round-robin (at most 24 checks
    /// a frame, 10 a second each) would have left each zombie;</item>
    /// <item>casts rays from the eye at both spots to the chest, head, crawler and corpse heights all round, out to
    /// <see cref="HordeFar"/>, and fails if the Sight Occluder layer alone lets through any that the whole world (the
    /// survivor's own body aside) blocked;</item>
    /// <item>stops the dead, lets the checks settle, and fails unless each zombie is shown exactly when the sight rule says
    /// it can be seen (leaving out those on an edge: the eye's idle bob, or a flickering lamp, could tip them either way);</item>
    /// <item>fails if anything solid within the survivor's sight is still on Default (the sweep missed it).</item>
    /// </list>
    /// With <c>-clonez-smoke-shots &lt;dir&gt;</c> the frames' numbers go to horde.csv there. It runs with or without
    /// <c>-nographics</c>.
    /// </summary>
    public sealed partial class SmokeTest
    {
        private const int HordeSize = 200;
        private const float HordeSeconds = 6f;
        // Most of the horde stands in front, within this of the way the survivor faces and this far out; the rest all
        // round, out to HordeFar. The survivor steps back HordeStepBack halfway through each frame rate.
        private const float HordeFrontShare = 0.85f, HordeFrontHalfAngle = 70f, HordeFrontFar = 26f;
        private const float HordeNear = 3f, HordeFar = 40f, HordeStepBack = 8f;
        // The survivor's spot is picked by how many of this many sight lines across the front run clear.
        private const int HordeViewRays = 13;
        private static readonly int[] HordeRates = { 60, 30, 20 };
        private static readonly float[] HordeSightHeights = { 0.3f, 0.35f, 0.55f, 1.2f, 1.62f };
        // The old round-robin, for comparison: at most this many checks a frame, aiming at this many a zombie a second.
        private const int HordeOldChecksPerFrame = 24;
        private const float HordeOldRateHz = 10f;

        private IEnumerator HordeRun()
        {
            yield return ToHub();
            yield return Deploy(RaidKind.Operative);
            Actor me = Actor.LocalPlayer;
            if (me == null) yield break;
            me.Untargetable = true;   // the horde mills about: this run is about seeing them, not their bites
            // Noon: the survivor sees furthest by the light alone, so every zombie in front is in view and needs its rays.
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.RestoreClock(TimeOfDay.Instance.Day, 12f);
            yield return Until(() => ZombieVisibility.HasSight, 10f);
            Check("horde: the vision rig decides who's seen", ZombieVisibility.HasSight);
            if (!ZombieVisibility.HasSight) yield break;
            int sweptAtStart = SightOccluders.Swept;
            HordeWorldClaimed();

            // Somewhere with a view: the horde in front partly in plain sight, partly behind walls.
            Vector3 facing = ZombieVisibility.CurrentSight.Facing;
            Teleport(me, HordeViewSpot(me.transform.position, facing, out int clearRays));
            for (int i = 0; i < 5; i++) yield return null;
            Vector3 home = me.transform.position;
            Vector3 back = HordeBackSpot(home, facing);
            var rng = new System.Random(20261107);
            var horde = new List<ZombieAI>(HordeSize);
            var deaths = new List<float>();
            RaiseHorde(horde, HordeSize, home, facing, rng, deaths, me);
            Check("horde: raised", horde.Count == HordeSize, horde.Count + " of " + HordeSize + " zombies on the NavMesh, "
                + Mathf.RoundToInt(HordeFrontShare * 100f) + "% in front, where " + clearRays + " of " + HordeViewRays
                + " sight lines run clear; the survivor steps back " + HordeNum((back - home).magnitude, 1) + " m");
            if (horde.Count == 0) yield break;
            for (int i = 0; i < 10; i++) yield return null;

            // At 60, 30 and 20 frames a second in turn, the survivor stepping back (or forward again) halfway through each.
            Recorder tick = Recorder.Get(ZombieVisibility.ProfilerName);
            if (tick.isValid) tick.enabled = true;
            var csv = new StringBuilder("rate,frame,zombies,in_view,checks,forced,rays,oldest_in_view_s,tick_ms\n");
            bool atHome = true;
            foreach (int rate in HordeRates)
            {
                horde.RemoveAll(ai => ai == null || ai.IsDeadState);
                RaiseHorde(horde, HordeSize - horde.Count, atHome ? home : back, facing, rng, deaths, me);
                deaths.Clear();
                Time.captureDeltaTime = 1f / rate;
                yield return null;
                ZombieVisibility.ResetWorstGap();
                int frames = Mathf.RoundToInt(HordeSeconds * rate);
                long inView = 0, checks = 0, forced = 0, rays = 0;
                int mostInView = 0, mostChecks = 0, mostRays = 0, fewest = int.MaxValue;
                float oldest = 0f;
                var ms = new List<double>(frames);
                for (int f = 0; f < frames; f++)
                {
                    if (f == frames / 2)
                    {
                        atHome = !atHome;
                        Teleport(me, atHome ? home : back);
                    }
                    // The dead that fall (the town has its dangers) are replaced at once: never fewer than HordeSize.
                    horde.RemoveAll(ai => ai == null || ai.IsDeadState);
                    if (horde.Count < HordeSize) RaiseHorde(horde, HordeSize - horde.Count, atHome ? home : back, facing, rng, deaths, me);
                    yield return null;
                    ZombieVisibility.TickStats s = ZombieVisibility.LastTick;
                    inView += s.InView;
                    checks += s.Checks;
                    forced += s.Forced;
                    rays += s.Rays;
                    mostInView = Mathf.Max(mostInView, s.InView);
                    mostChecks = Mathf.Max(mostChecks, s.Checks);
                    mostRays = Mathf.Max(mostRays, s.Rays);
                    fewest = Mathf.Min(fewest, s.Zombies);
                    oldest = Mathf.Max(oldest, s.OldestInView);
                    double tickMs = tick.isValid ? tick.elapsedNanoseconds / 1e6 : 0.0;
                    ms.Add(tickMs);
                    csv.Append(rate).Append(',').Append(f).Append(',').Append(s.Zombies).Append(',').Append(s.InView).Append(',')
                        .Append(s.Checks).Append(',').Append(s.Forced).Append(',').Append(s.Rays).Append(',')
                        .Append(HordeNum(s.OldestInView, 4)).Append(',').Append(HordeNum(tickMs, 4)).Append('\n');
                }
                float worst = Mathf.Max(ZombieVisibility.WorstGap, oldest);
                float dt = 1f / rate;
                float oldGap = fewest / Mathf.Min(HordeOldChecksPerFrame, fewest * HordeOldRateHz * dt) * dt;
                ms.Sort();
                Debug.Log("[Smoke] horde " + rate + " fps: " + fewest + " zombies at the least (" + deaths.Count + " died and were replaced"
                    + HordeDeaths(deaths) + "), "
                    + HordeNum((double)inView / frames, 1) + " in view a frame (most " + mostInView + "), " + HordeNum((double)checks / frames, 1)
                    + " checks a frame (most " + mostChecks + "; " + HordeNum((double)forced / frames, 1) + " held to " + ZombieVisibility.MaxCheckAge
                    + " s), " + HordeNum((double)rays / frames, 1) + " rays a frame (most " + mostRays + "); the longest a zombie in view went unchecked "
                    + HordeNum(worst, 3) + " s; the ticks " + HordeNum(HordeQuantile(ms, 0.5), 3) + " ms a frame (95th percentile "
                    + HordeNum(HordeQuantile(ms, 0.95), 3) + ", worst " + HordeNum(HordeQuantile(ms, 1.0), 3) + (tick.isValid ? "" : "; no profiler sample")
                    + "); the old round-robin would have left each zombie " + HordeNum(oldGap, 3) + " s");
                Check("horde at " + rate + " fps: no zombie in view goes " + ZombieVisibility.MaxCheckAge + " s unchecked",
                    worst <= ZombieVisibility.MaxCheckAge + 1e-4f && inView > 0,
                    "longest " + HordeNum(worst, 3) + " s, " + HordeNum((double)inView / frames, 1) + " of " + fewest + " or more in view a frame");
            }
            Time.captureDeltaTime = 0f;
            if (tick.isValid) tick.enabled = false;
            if (s_ShotDir != null)
            {
                string file = Path.Combine(s_ShotDir, "horde.csv");
                try { File.WriteAllText(file, csv.ToString()); Debug.Log("[Smoke] horde frames written to " + file); }
                catch (Exception e) { Debug.LogWarning("[Smoke] horde.csv not written: " + e.Message); }
            }

            // The Sight Occluder layer hides what the whole world hid: rays all round, at both spots, the survivor's own body
            // left out (it's no occluder: characters aren't).
            int differ = 0, cast = 0;
            var differs = new List<string>();
            foreach (Vector3 spot in new[] { home, back })
            {
                Teleport(me, spot);
                for (int i = 0; i < 5; i++) yield return null;
                var body = new List<Collider>();
                foreach (Collider c in me.GetComponentsInChildren<Collider>())
                    if (c.enabled)
                    {
                        c.enabled = false;
                        body.Add(c);
                    }
                cast += HordeCompareRays(spot, ZombieVisibility.CurrentSight.Eye, differs, ref differ);
                foreach (Collider c in body) c.enabled = true;
            }
            Check("horde: the Sight Occluder layer blocks every sight line the whole world did", differ == 0,
                differ + " of " + cast + " rays differ" + (differs.Count > 0 ? ": " + string.Join("; ", differs) : ""));

            // Still, every zombie is shown just when it can be seen.
            foreach (ZombieAI ai in horde)
            {
                if (ai == null || ai.IsDeadState) continue;
                ai.enabled = false;
                NavMeshAgent agent = ai.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
            }
            yield return new WaitForSeconds(ZombieVisibility.MaxCheckAge + ZombieVisibility.HideDelay + 0.3f);
            HordeShownAsSeen(horde);

            // Nothing solid round the survivor is left on Default: the sweep catches what the builders didn't.
            ZombieVisibility.Sight sight = ZombieVisibility.CurrentSight;
            float reach = Mathf.Max(sight.SightDistance, sight.LitSightDistance);
            const int defaultLayerOnly = 1 << 0;
            int left = 0;
            var leftNames = new List<string>();
            foreach (Collider c in Physics.OverlapSphere(sight.Eye, reach, defaultLayerOnly, QueryTriggerInteraction.Ignore))
            {
                if (c is CharacterController || c.GetComponentInParent<Actor>(true) != null) continue;
                left++;
                if (leftNames.Count < 5) leftNames.Add(PathOf(c.transform));
            }
            var swept = new List<Collider>();
            SightOccluders.RecentlySwept(swept);
            var sweptNames = new List<string>();
            foreach (Collider c in swept)
            {
                string name = PathOf(c.transform);
                if (!sweptNames.Contains(name) && sweptNames.Count < 6) sweptNames.Add(name);
            }
            Check("horde: nothing solid within sight is left on Default", left == 0,
                left + " within " + HordeNum(reach, 0) + " m" + (leftNames.Count > 0 ? ": " + string.Join(", ", leftNames) : "")
                + "; the sweep moved " + sweptAtStart + " before the run and " + (SightOccluders.Swept - sweptAtStart) + " during it"
                + (sweptNames.Count > 0 ? ", lately " + string.Join(", ", sweptNames) : ""));
            Debug.Log("[Smoke] horde sight: " + HordeNum(sight.SightDistance, 1) + " m by the light, " + HordeNum(sight.LitSightDistance, 1)
                + " m to a lit zombie, flashlight " + (sight.BeamOn ? "on" : "off"));
        }

        // Right after the build, every solid under the world root is on the Sight Occluder layer (characters aside).
        private void HordeWorldClaimed()
        {
            GameObject root = null;
            foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == "World") root = go;
            if (root == null)
            {
                Skip("horde: the world's solids are on the Sight Occluder layer", "no World root");
                return;
            }
            int occluders = 0, left = 0;
            var names = new List<string>();
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger) continue;
                if (c.gameObject.layer == CloneZConfig.SightOccluderLayer) occluders++;
                if (c.gameObject.layer != 0 || c is CharacterController || c.GetComponentInParent<Actor>(true) != null) continue;
                left++;
                if (names.Count < 5) names.Add(PathOf(c.transform));
            }
            string detail = occluders + " solid colliders on it, " + left + " left on Default" + (names.Count > 0 ? ": " + string.Join(", ", names) : "")
                + "; the sweep has moved " + SightOccluders.Swept;
            // A streamed map goes on fitting bits to its pieces after they're claimed (a wall made breakable, a hole's patch):
            // those are the sweep's to move as the survivor nears, which the checks of what's in sight hold it to.
            if (KentuckyStreamer.Active != null)
            {
                Debug.Log("[Smoke] horde world (streamed): " + detail);
                return;
            }
            Check("horde: the world's solids are on the Sight Occluder layer", left == 0 && occluders > 0, detail);
        }

        // Raises `count` zombies on the NavMesh round `from`: most in front (within HordeFrontHalfAngle of `facing`, out to
        // HordeFrontFar), the rest all round out to HordeFar, evenly over each area. Their deaths' distances go into `deaths`.
        private static void RaiseHorde(List<ZombieAI> horde, int count, Vector3 from, Vector3 facing, System.Random rng, List<float> deaths, Actor me)
        {
            float heading = Mathf.Atan2(facing.z, facing.x);
            for (int raised = 0, tries = 0; raised < count && tries < count * 20; tries++)
            {
                bool front = rng.NextDouble() < HordeFrontShare;
                float far = front ? HordeFrontFar : HordeFar;
                float r = Mathf.Sqrt(HordeNear * HordeNear + (float)rng.NextDouble() * (far * far - HordeNear * HordeNear));
                float a = front ? heading + ((float)rng.NextDouble() * 2f - 1f) * HordeFrontHalfAngle * Mathf.Deg2Rad
                    : (float)(rng.NextDouble() * Math.PI * 2.0);
                Vector3 p = from + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
                GameObject z = ZombieSpawner.BuildZombie(hit.position);
                var ai = z.GetComponent<ZombieAI>();
                double kind = rng.NextDouble();
                ai.Configure(kind < 0.05 ? ZombieType.Sprinter : kind < 0.15 ? ZombieType.Crawler
                    : kind < 0.35 ? ZombieType.FastShambler : ZombieType.Shambler);
                ZombieHealth health = z.GetComponent<ZombieHealth>();
                if (health != null)
                    health.OnDeath += dead => deaths.Add(me != null ? Vector3.Distance(dead.transform.position, me.transform.position) : -1f);
                horde.Add(ai);
                raised++;
            }
        }

        // A NavMesh point HordeStepBack behind the survivor (else to a side), so the horde stays in front from there too.
        private static Vector3 HordeBackSpot(Vector3 home, Vector3 facing)
        {
            Vector3 side = new Vector3(facing.z, 0f, -facing.x);
            foreach (Vector3 dir in new[] { -facing, side, -side, (side - facing).normalized, (-side - facing).normalized })
                if (NavMesh.SamplePosition(home + dir * HordeStepBack, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return hit.position;
            return home;
        }

        // Of the NavMesh spots within 30 m of `from`, the one whose view ahead (HordeViewRays sight lines across the front,
        // out to HordeFrontFar) is nearest two thirds clear, so the horde in front is partly in plain sight and partly
        // behind walls.
        private static Vector3 HordeViewSpot(Vector3 from, Vector3 facing, out int clearRays)
        {
            Vector3 best = from;
            clearRays = -1;
            float bestMiss = float.MaxValue;
            for (int ring = 0; ring <= 3; ring++)
                for (int k = 0; k < (ring == 0 ? 1 : 8); k++)
                {
                    float a = k * 45f * Mathf.Deg2Rad;
                    Vector3 p = from + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ring * 10f;
                    if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
                    Vector3 eye = hit.position + Vector3.up * 1.6f;
                    int clear = 0;
                    for (int j = 0; j < HordeViewRays; j++)
                    {
                        float yaw = Mathf.Lerp(-HordeFrontHalfAngle, HordeFrontHalfAngle, j / (HordeViewRays - 1f));
                        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * facing;
                        if (!Physics.Raycast(eye, dir, HordeFrontFar, CloneZConfig.SightOccluderMask, QueryTriggerInteraction.Ignore)) clear++;
                    }
                    float miss = Mathf.Abs(clear - HordeViewRays * 2f / 3f);
                    if (miss >= bestMiss) continue;
                    bestMiss = miss;
                    best = hit.position;
                    clearRays = clear;
                }
            return best;
        }

        private static string HordeDeaths(List<float> deaths)
        {
            if (deaths.Count == 0) return "";
            var at = new List<string>();
            for (int i = 0; i < deaths.Count && i < 5; i++) at.Add(HordeNum(deaths[i], 1) + " m");
            return ", " + string.Join(", ", at) + (deaths.Count > 5 ? ", ..." : "") + " from the survivor";
        }

        // Rays from the eye all round the spot: blocked by the whole world (all but Ignore Raycast, the old rule) against
        // blocked by the Sight Occluder layer alone. Returns how many points were tried.
        private static int HordeCompareRays(Vector3 spot, Vector3 eye, List<string> differs, ref int differ)
        {
            int oldMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);
            int tried = 0;
            for (float r = 1.5f; r <= HordeFar; r += 1.5f)
                for (int k = 0; k < 72; k++)
                {
                    float a = k * 5f * Mathf.Deg2Rad;
                    Vector3 foot = new Vector3(spot.x + Mathf.Cos(a) * r, spot.y, spot.z + Mathf.Sin(a) * r);
                    foreach (float h in HordeSightHeights)
                    {
                        Vector3 p = foot + Vector3.up * h;
                        tried++;
                        bool before = Physics.Linecast(eye, p, out RaycastHit hit, oldMask, QueryTriggerInteraction.Ignore);
                        bool now = Physics.Linecast(eye, p, CloneZConfig.SightOccluderMask, QueryTriggerInteraction.Ignore);
                        if (before == now) continue;
                        differ++;
                        if (differs.Count < 5 && hit.collider != null)
                            differs.Add(PathOf(hit.collider.transform) + " on layer " + hit.collider.gameObject.layer + " at " + HordeNum(r, 1) + " m");
                    }
                }
            return tried;
        }

        // Each zombie shown just when the rule says it can be seen, but for those on an edge: one the eye's idle bob could
        // tip either way, or one seen only by a lamp's light (lamps flicker).
        private void HordeShownAsSeen(List<ZombieAI> horde)
        {
            ZombieVisibility.Sight sight = ZombieVisibility.CurrentSight;
            ZombieVisibility.Sight up = sight, down = sight;
            up.Eye += Vector3.up * 0.03f;
            down.Eye -= Vector3.up * 0.03f;
            int shownSeen = 0, hiddenUnseen = 0, edge = 0, wrong = 0;
            var wrongs = new List<string>();
            foreach (ZombieAI ai in horde)
            {
                if (ai == null || ai.IsDeadState) continue;
                ZombieVisibility zv = ai.GetComponent<ZombieVisibility>();
                zv.SightPoints(out Vector3 chest, out Vector3 head);
                bool seen = ZombieVisibility.CanSee(in sight, chest, head);
                if (ZombieVisibility.SeenOnlyIfLit(in sight, chest)
                    || ZombieVisibility.CanSee(in up, chest, head) != seen || ZombieVisibility.CanSee(in down, chest, head) != seen)
                {
                    edge++;
                    continue;
                }
                if (zv.IsShown == seen)
                {
                    if (seen) shownSeen++;
                    else hiddenUnseen++;
                    continue;
                }
                wrong++;
                if (wrongs.Count < 5)
                    wrongs.Add((zv.IsShown ? "shown, unseen" : "hidden, seen") + " at " + HordeNum(Vector3.Distance(chest, sight.Eye), 1)
                        + " m (last seen " + HordeNum(Time.time - zv.LastSeenTime, 2) + " s ago)");
            }
            Check("horde: once still, every zombie is shown just when it can be seen", wrong == 0 && shownSeen > 0 && hiddenUnseen > 0,
                shownSeen + " shown and seen, " + hiddenUnseen + " hidden and unseen, " + edge + " on an edge, " + wrong + " wrong"
                + (wrongs.Count > 0 ? ": " + string.Join("; ", wrongs) : ""));
        }

        private static double HordeQuantile(List<double> sorted, double q)
        {
            if (sorted.Count == 0) return 0.0;
            return sorted[Mathf.Clamp((int)Math.Ceiling(q * sorted.Count) - 1, 0, sorted.Count - 1)];
        }

        private static string HordeNum(double v, int decimals) => v.ToString("F" + decimals, CultureInfo.InvariantCulture);
    }
}