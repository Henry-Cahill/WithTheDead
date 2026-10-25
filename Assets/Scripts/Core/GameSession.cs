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

        /// <summary>True when this run is a raid into a Zone (as opposed to wave survival or time at the hub).</summary>
        public static bool IsExtraction => Mode == GameMode.Extraction;
        /// <summary>True when this run is the endless-wave arena around the starter house.</summary>
        public static bool IsWaveSurvival => Mode == GameMode.WaveSurvival;
        /// <summary>True when the session is the hub, between raids.</summary>
        public static bool IsHub => Mode == GameMode.Hub;
        /// <summary>Set while the hub stands in the town a raid will start in (the Kettle in Bardstown, until the survivor
        /// walks out of its door, see <see cref="KettleLobby"/>): the world round the bar is already the raid's.</summary>
        public static bool HubInRaidTown;
        /// <summary>True when the world is a raid's: in a raid, or round the hub standing in a raid's town. What's rolled
        /// for it (loot found in raid, the Central Economy's caps, the night's weather) is the raid's.</summary>
        public static bool IsRaidWorld => IsExtraction || HubInRaidTown;
        /// <summary>True when the raid is a free scav run in random gear; the character's stash stays safe.</summary>
        public static bool IsScavRun => IsExtraction && Raid == RaidKind.Scav;
        /// <summary>True when the raid is the persistent open zone, with no dawn deadline.</summary>
        public static bool IsOpenZone => IsExtraction && Raid == RaidKind.OpenZone;
        /// <summary>True when playing as one of the Zone's horrors, in someone else's raid.</summary>
        public static bool IsMonsterRun => IsExtraction && Raid == RaidKind.Monster;
        /// <summary>A body that isn't the character's own (a scav's, a hunter's): nothing it suffers or carries
        /// touches the character's health, kit, marks or diseases.</summary>
        public static bool IsBorrowedBody => IsScavRun || IsMonsterRun;

        /// <summary>The survivor's name, shown on the inventory ("Dale Mattingly's Inventory") and the HUD: the character's,
        /// else a random one (rolled afresh for each new wave-survival run and kept in its save).</summary>
        public static string SurvivorName = SurvivorNames.Roll();

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
