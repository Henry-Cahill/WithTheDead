using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// What the zone overlays know about the raid map being built: the roots they build under, the occupancy grid,
    /// the zone and the points they published. Rebuilt for every raid (see <see cref="ZoneWorldModule"/>).
    /// </summary>
    internal static class ZoneWorld
    {
        /// <summary>The baked world root (navmesh, colliders).</summary>
        public static Transform World;
        /// <summary>A scene-level root for pieces that must stay out of the navmesh bake (canopies, undergrowth,
        /// backdrops, plumes); it dies with the scene like the town's own scenery.</summary>
        public static Transform Unbaked;
        public static ZoneSiteMap Site;
        public static ZoneDef Zone;
        public static RaidMapInfo Map;
        /// <summary>The raid seed this context was built for.</summary>
        public static int Seed;
        /// <summary>The runtime host (atmosphere, events).</summary>
        public static ZoneRuntime Runtime;

        /// <summary>Landing zones published this raid, N, E, S, W.</summary>
        public static readonly List<Vector3> LandingZones = new List<Vector3>(4);

        public static float Half => Map != null ? Map.HalfSize : CloneZConfig.ArenaHalfSize;

        public static void Reset()
        {
            World = null;
            Unbaked = null;
            Site = null;
            Zone = null;
            Map = null;
            Runtime = null;
            LandingZones.Clear();
        }
    }

    /// <summary>
    /// Wires the zone overlays into world construction. On <see cref="WorldHooks.RaidMapBuilt"/> (before the bake)
    /// it reads the town back into a <see cref="ZoneSiteMap"/> and, from the raid's random stream, lays the zone's own
    /// character over the shared town: the cordon (<see cref="ZoneCordon"/>), the landmarks and the published points
    /// (<see cref="ZoneLandmarks"/>), the woods of Mirelock (<see cref="ZoneForest"/>), the Spillway's plant
    /// (<see cref="ZoneIndustry"/>) and the air itself (<see cref="ZoneAtmosphere"/>). On
    /// <see cref="WorldHooks.RaidStarted"/> it starts the raid's world events: the helicopter flyover
    /// (<see cref="HeliFlyoverDirector"/>) and the uncanny (<see cref="UncannyDirector"/>).
    /// </summary>
    public sealed class ZoneWorldModule : IGameModule
    {
        public int Order => 40;

        private static int s_PassSeed;

        public void Init()
        {
            WorldHooks.RaidMapBuilt += OnRaidMapBuilt;
            WorldHooks.RaidStarted += OnRaidStarted;
            GameEvents.RaidEnded += OnRaidEnded;
            CombatResolver.ShotResolved += ReturnedFigure.OnShotResolved;
        }

        /// <summary>Records that this raid's pass was paid at the gate (so a stale pass is never carried into the next raid).</summary>
        internal static void NotePassBought() => s_PassSeed = GameSession.RaidSeed != 0 ? GameSession.RaidSeed : 1;

        private static void OnRaidEnded(RaidOutcome outcome, string exit)
        {
            if (s_PassSeed != 0) CordonRules.PassBought = false;
            s_PassSeed = 0;
        }

        private static void OnRaidMapBuilt(Transform world, RaidMapInfo map, ZoneDef zone, System.Random rng)
        {
            if (world == null || map == null) return;
            if (zone == null) zone = ZoneCatalog.Current;
            float t0 = Time.realtimeSinceStartup;

            ZoneWorld.Reset();
            ZoneForest.Reset();
            LocationVisitTrigger.ResetForRaid();
            ReturnedFigure.ResetForRaid();

            // The cordon's belt and this raid's pass.
            CordonRules.Active = zone.HasCordon;
            CordonRules.BeltWidth = 14f;
            if (CordonRules.PassBought && s_PassSeed != 0 && s_PassSeed != GameSession.RaidSeed)
            {
                CordonRules.PassBought = false;
                s_PassSeed = 0;
            }

            ZoneWorld.World = world;
            ZoneWorld.Map = map;
            ZoneWorld.Zone = zone;
            ZoneWorld.Seed = GameSession.RaidSeed;
            ZoneWorld.Unbaked = new GameObject("ZoneUnbaked").transform;
            ZoneWorld.Site = ZoneSiteMap.Analyse(world, map);

            // One draw from the raid's stream seeds everything here, so the overlays never shift what other
            // features draw from it after us; each part gets its own stream.
            int seed = rng != null ? rng.Next() : GameSession.RaidSeed;
            var cordonRng = new System.Random(seed ^ 0x2C0D0);
            var landmarkRng = new System.Random(seed ^ 0x1A4D3);
            var markerRng = new System.Random(seed ^ 0x3A21E);
            var overlayRng = new System.Random(seed ^ 0x0F0E57);

            Step("cordon", () => { if (zone.HasCordon) ZoneCordon.Build(world, map, zone, ZoneWorld.Site, cordonRng); });
            Step("landmarks", () => ZoneLandmarks.Build(world, map, zone, ZoneWorld.Site, landmarkRng));
            Step("markers", () => ZoneLandmarks.PublishPoints(world, map, zone, ZoneWorld.Site, markerRng));
            Step("overlay", () =>
            {
                if (zone.ForestDensity > 0.3f) ZoneForest.Build(world, map, zone, ZoneWorld.Site, overlayRng);
                if (ZoneIndustry.Wants(zone)) ZoneIndustry.Build(world, map, zone, ZoneWorld.Site, overlayRng);
                if (zone.HasCordon) ZoneCordon.DressBelt(world, zone, ZoneWorld.Site, overlayRng);
            });

            var host = new GameObject("ZoneRuntime");
            ZoneWorld.Runtime = host.AddComponent<ZoneRuntime>();
            ZoneAtmosphere.Attach(host, zone);

            DecorPalette.Apply();
            Debug.Log("[CloneZ] Zone overlay for " + zone.Name + ": " + ZoneLandmarks.LastSummary + ", " + ZoneForest.TreeCount + " trees, "
                + Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms");
        }

        // One broken part must never take the rest of the overlay (or the map) with it.
        private static void Step(string name, System.Action a)
        {
            try { a(); }
            catch (System.Exception e) { Debug.LogError("[CloneZ] Zone overlay step '" + name + "' failed: " + e); }
        }

        private static void OnRaidStarted(Transform world, RaidMapInfo map, ZoneDef zone)
        {
            if (ZoneWorld.Runtime == null)
            {
                var host = new GameObject("ZoneRuntime");
                ZoneWorld.Runtime = host.AddComponent<ZoneRuntime>();
                ZoneAtmosphere.Attach(host, zone ?? ZoneCatalog.Current);
            }
            ZoneWorld.Runtime.StartEvents(zone ?? ZoneCatalog.Current);
        }
    }

    /// <summary>
    /// The per-raid host of the zone's live systems: the atmosphere, the helicopter flyover, the uncanny events and the
    /// woods' stingers. Starts them on <see cref="WorldHooks.RaidStarted"/> — or, should that hook never come, a few
    /// seconds after the local survivor appears.
    /// </summary>
    public sealed class ZoneRuntime : MonoBehaviour
    {
        public bool EventsStarted { get; private set; }
        private float _fallbackAt = -1f;

        internal void StartEvents(ZoneDef zone)
        {
            if (EventsStarted) return;
            EventsStarted = true;
            if (zone == null) zone = ZoneCatalog.Current;
            HeliFlyoverDirector.BeginRaid(gameObject, zone);
            UncannyDirector.BeginRaid(gameObject, zone);
        }

        private void Update()
        {
            if (EventsStarted) return;
            if (Actor.LocalPlayer == null) return;
            if (_fallbackAt < 0f) _fallbackAt = Time.time + 4f;
            if (Time.time >= _fallbackAt) StartEvents(ZoneWorld.Zone);
        }
    }
}
