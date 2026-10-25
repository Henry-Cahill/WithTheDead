using UnityEngine;
using UnityEngine.SceneManagement;

namespace CloneZ
{
    /// <summary>
    /// The combined core loop's transitions: title → hub → (deploy) raid → summary → hub. Every transition saves
    /// the profile and reloads the scene so the bootstrapper rebuilds a clean world for the next stage. In a
    /// networked session the server drives transitions for everyone (<see cref="NetworkTransition"/>).
    /// </summary>
    public static class GameFlow
    {
        /// <summary>Set by the netcode: a stage change goes through the session instead of a local reload.
        /// Returns true when it took over.</summary>
        public static System.Func<GameMode, string, int, RaidKind, bool> NetworkTransition;

        /// <summary>Back to the hub (after a raid, or from a menu).</summary>
        public static void GoToHub()
        {
            if (NetworkTransition != null && NetworkTransition(GameMode.Hub, null, -1, RaidKind.Operative)) return;
            GameSession.AutoStartHub = true;
            GameSession.AutoStartRaid = false;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            HubDef hub = HubCatalog.Get(c != null ? c.Hub : HubCatalog.Kettle);
            LoadingScreen.Show(hub.Name.ToUpperInvariant(), KettleLobby.Where(hub));
            Reload();
        }

        /// <summary>Into a zone: <paramref name="landingZone"/> picks the helicopter's LZ (-1 = random side).</summary>
        public static void Deploy(string zoneId, int landingZone, RaidKind kind)
        {
            if (NetworkTransition != null && NetworkTransition(GameMode.Extraction, zoneId, landingZone, kind)) return;
            PrepareRaid(zoneId, landingZone, kind);
            GameSession.AutoStartRaid = true;
            GameSession.AutoStartHub = false;
            ZoneDef zone = ZoneCatalog.Current;
            LoadingScreen.Show(zone.Name.ToUpperInvariant(),
                (string.IsNullOrEmpty(zone.RealPlace) ? "" : zone.RealPlace + "  \u00b7  ") + LoadingScreen.NightOf());
            Reload();
        }

        /// <summary>Sets the session up for a raid (also used by the netcode on every peer).</summary>
        public static void PrepareRaid(string zoneId, int landingZone, RaidKind kind)
        {
            GameSession.ZoneId = string.IsNullOrEmpty(zoneId) ? ZoneCatalog.DefaultZone : zoneId;
            GameSession.InsertionPoint = landingZone;
            GameSession.Raid = kind;
            GameSession.Mode = GameMode.Extraction;
        }

        public static void ToMainMenu()
        {
            GameSession.AutoStartHub = false;
            GameSession.AutoStartRaid = false;
            Reload();
        }

        private static bool s_ReloadPending;

        /// <summary>Saves and reloads the scene (the bootstrapper reads the session flags on the way back up). With the
        /// loading card up, the reload waits until the card has been drawn (a world is built on the main thread, so
        /// nothing redraws until it's done); the world is held still meanwhile, and a second request is ignored.</summary>
        public static void Reload()
        {
            if (s_ReloadPending) return;
            ProfileStore.Save();
            NoiseSystem.Clear();
            Time.timeScale = 1f;
            int scene = SceneManager.GetActiveScene().buildIndex;
            if (!LoadingScreen.Showing)
            {
                UIState.GameplayBlocked = false;
                SceneManager.LoadScene(scene);
                return;
            }
            s_ReloadPending = true;
            UIState.GameplayBlocked = true;
            LoadingScreen.Run(() =>
            {
                s_ReloadPending = false;
                SceneManager.LoadScene(scene);
            });
        }
    }
}
