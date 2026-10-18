using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A gameplay feature that wires itself in at boot: registers item actions, subscribes to
    /// <see cref="GameEvents"/> and <see cref="WorldHooks"/>, adds HUD widgets. Every implementation in the game
    /// assembly is found by reflection and initialised once, in <see cref="Order"/>, before the first world is built.
    /// </summary>
    public interface IGameModule
    {
        int Order { get; }
        void Init();
    }

    public static class GameModules
    {
        private static bool s_Init;

        public static void EnsureInit()
        {
            if (s_Init) return;
            s_Init = true;
            ItemDatabase.EnsureInit();
            Type iface = typeof(IGameModule);
            List<IGameModule> modules = typeof(GameModules).Assembly.GetTypes()
                .Where(t => iface.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (IGameModule)Activator.CreateInstance(t))
                .OrderBy(m => m.Order)
                .ToList();
            foreach (IGameModule m in modules)
            {
                try { m.Init(); }
                catch (Exception e) { Debug.LogError("[CloneZ] Module " + m.GetType().Name + " failed to initialise: " + e); }
            }
            Debug.Log("[CloneZ] " + modules.Count + " game modules initialised.");
        }
    }

    /// <summary>
    /// Points in world construction that features hook into, so the bootstrapper never needs to know about them.
    /// All are raised on the main thread in the order listed.
    /// </summary>
    public static class WorldHooks
    {
        /// <summary>The survivor's body exists (components can be added). Raised for every mode.</summary>
        public static event Action<PlayerStats> PlayerBuilt;
        /// <summary>A raid map is built, before the navmesh bake: add obstacles, anomalies, landmarks (the rng is the raid's).</summary>
        public static event Action<Transform, RaidMapInfo, ZoneDef, System.Random> RaidMapBuilt;
        /// <summary>The raid is live (navmesh baked, survivor and HUD built): spawn agents, start events.</summary>
        public static event Action<Transform, RaidMapInfo, ZoneDef> RaidStarted;
        /// <summary>The hub world is built (navmesh baked, survivor and HUD built).</summary>
        public static event Action<Transform> HubBuilt;
        /// <summary>The wave-survival arena is built.</summary>
        public static event Action<Transform> ArenaBuilt;

        public static void RaisePlayerBuilt(PlayerStats p) => Each(PlayerBuilt, h => h(p));
        public static void RaiseRaidMapBuilt(Transform w, RaidMapInfo m, ZoneDef z, System.Random rng) => Each(RaidMapBuilt, h => h(w, m, z, rng));
        public static void RaiseRaidStarted(Transform w, RaidMapInfo m, ZoneDef z) => Each(RaidStarted, h => h(w, m, z));
        public static void RaiseHubBuilt(Transform w) => Each(HubBuilt, h => h(w));
        public static void RaiseArenaBuilt(Transform w) => Each(ArenaBuilt, h => h(w));

        // Each subscriber runs in isolation: one broken feature must not stop the world being built.
        private static void Each<T>(T multicast, Action<T> call) where T : Delegate
        {
            if (multicast == null) return;
            foreach (Delegate d in multicast.GetInvocationList())
            {
                try { call((T)d); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
