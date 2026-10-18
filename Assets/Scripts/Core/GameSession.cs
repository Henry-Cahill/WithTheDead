namespace CloneZ
{
    /// <summary>The kind of run being played, picked on the title screen or in the hub.</summary>
    public enum GameMode
    {
        /// <summary>A raid into a Zone map: spawn, loot, reach an exit (or die).</summary>
        Extraction,
        /// <summary>The original endless-wave arena around the starter house (saved / continued).</summary>
        WaveSurvival,
        /// <summary>The hub: the bar, the traders, the stash and the hideout between raids.</summary>
        Hub,
    }

    /// <summary>What sort of raid an extraction run is.</summary>
    public enum RaidKind
    {
        /// <summary>Your own character with your own kit: everything carried is at risk.</summary>
        Operative,
        /// <summary>A free, cooldown-gated run with random gear among the scavengers; your stash is never at risk.</summary>
        Scav,
        /// <summary>The persistent open zone: no dawn deadline, stay as long as supplies last.</summary>
        OpenZone,
        /// <summary>Play as one of the Zone's horrors, invading someone else's raid.</summary>
        Monster,
    }

    /// <summary>Which camera view is active (cycled with V by <see cref="IsoCameraFollow"/>).</summary>
    public enum CameraView { Isometric, ThirdPerson, FirstPerson, Bodycam }

    /// <summary>
    /// Run-wide settings chosen before the world is built. Static fields survive a scene reload, so
    /// the bootstrapper sets every one of them explicitly whenever it builds a world.
    /// </summary>
    public static class GameSession
    {
        public static GameMode Mode = GameMode.Extraction;
        public static RaidKind Raid = RaidKind.Operative;

        public static bool IsExtraction => Mode == GameMode.Extraction;
        public static bool IsWaveSurvival => Mode == GameMode.WaveSurvival;
        public static bool IsHub => Mode == GameMode.Hub;
        public static bool IsScavRun => IsExtraction && Raid == RaidKind.Scav;
        public static bool IsOpenZone => IsExtraction && Raid == RaidKind.OpenZone;
        public static bool IsMonsterRun => IsExtraction && Raid == RaidKind.Monster;

        /// <summary>The survivor's name, shown on the inventory ("Clone Z's Inventory") and the HUD.</summary>
        public static string SurvivorName = "Clone Z";

        /// <summary>Seed for the current raid's loot, spawns and anomalies (changes every raid).</summary>
        public static int RaidSeed;

        /// <summary>The Zone being raided (see <see cref="ZoneCatalog"/>).</summary>
        public static string ZoneId = ZoneCatalog.DefaultZone;

        /// <summary>The landing zone picked for the helicopter insertion (-1 = a random spawn side).</summary>
        public static int InsertionPoint = -1;

        /// <summary>Set before reloading the scene (e.g. the raid summary's "Next Raid") to skip the
        /// title screen and start a raid straight away. The bootstrapper consumes and clears it.</summary>
        public static bool AutoStartRaid;

        /// <summary>Set before reloading to go straight to the hub (after a raid, or "Return to hub").</summary>
        public static bool AutoStartHub;

        /// <summary>True while the world was built for a networked session (host or client).</summary>
        public static bool Networked;
    }

    /// <summary>
    /// The live camera view, written by <see cref="IsoCameraFollow"/> and read by anything that has
    /// to behave differently per view (aiming, cursor, ceilings that only show from inside, HUD).
    /// </summary>
    public static class ViewState
    {
        public static CameraView Current = CameraView.Isometric;

        /// <summary>True in the shoulder, first-person and bodycam views, where the mouse steers the camera.</summary>
        public static bool MouseLook => Current != CameraView.Isometric;

        /// <summary>True in the views whose camera sits on the head / body (sights, visor, diegetic watch).</summary>
        public static bool FirstPersonLike => Current == CameraView.FirstPerson || Current == CameraView.Bodycam;

        /// <summary>True when interior ceilings should render — only from the shoulder / first-person
        /// views. The isometric camera looks down through roofless rooms, PZ-style.</summary>
        public static bool CeilingsVisible => Current != CameraView.Isometric;
    }
}
