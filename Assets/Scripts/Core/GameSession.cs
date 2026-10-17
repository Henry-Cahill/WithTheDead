namespace CloneZ
{
    /// <summary>The kind of run being played, picked on the title screen.</summary>
    public enum GameMode
    {
        /// <summary>A night raid: spawn under protection, loot a run-down town, reach an exit before dawn.</summary>
        Extraction,
        /// <summary>The original endless-wave arena around the starter house (saved / continued).</summary>
        WaveSurvival,
    }

    /// <summary>Which camera view is active (cycled with V by <see cref="IsoCameraFollow"/>).</summary>
    public enum CameraView { Isometric, ThirdPerson, FirstPerson }

    /// <summary>
    /// Run-wide settings chosen before the world is built. Static fields survive a scene reload, so
    /// the bootstrapper sets every one of them explicitly whenever it builds a world.
    /// </summary>
    public static class GameSession
    {
        public static GameMode Mode = GameMode.Extraction;

        public static bool IsExtraction => Mode == GameMode.Extraction;
        public static bool IsWaveSurvival => Mode == GameMode.WaveSurvival;

        /// <summary>The survivor's name, shown on the inventory ("Clone Z's Inventory") and the HUD.</summary>
        public static string SurvivorName = "Clone Z";

        /// <summary>Seed for the current raid's map layout and loot.</summary>
        public static int RaidSeed;

        /// <summary>Set before reloading the scene (e.g. the raid summary's "Next Raid") to skip the
        /// title screen and start a raid straight away. The bootstrapper consumes and clears it.</summary>
        public static bool AutoStartRaid;
    }

    /// <summary>
    /// The live camera view, written by <see cref="IsoCameraFollow"/> and read by anything that has
    /// to behave differently per view (aiming, cursor, ceilings that only show from inside, HUD).
    /// </summary>
    public static class ViewState
    {
        public static CameraView Current = CameraView.Isometric;

        /// <summary>True in the shoulder and first-person views, where the mouse steers the camera.</summary>
        public static bool MouseLook => Current != CameraView.Isometric;

        /// <summary>True when interior ceilings should render — only from the shoulder / first-person
        /// views. The isometric camera looks down through roofless rooms, PZ-style.</summary>
        public static bool CeilingsVisible => Current != CameraView.Isometric;
    }
}
