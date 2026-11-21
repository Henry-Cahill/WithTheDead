namespace CloneZ
{
    // The starter home's interactive fixtures: their reach, cursor pick, timing and noise, the power grid's failure and the planks and nails salvaged from them.
    public static partial class CloneZConfig
    {
        // ---- Interactive fixtures (the starter home's everyday things: E = main action, Q = action menu) ----
        public const float FixtureReach = 1.7f;              // how close (m, flat) the survivor must stand to a fixture's interact point
        public const float FixtureReachVertical = 2.4f;      // and within this height of it (a wall cupboard, a lamp on a shelf)
        public const float FixtureCursorPickFraction = 0.12f;// cursor pick radius as a fraction of the screen height (~130 px at 1080p)
        public const float FixtureCursorScoreCap = 1.5f;     // a target far from the cursor scores no worse than this (then nearest wins)
        public const float FixtureDistanceWeight = 0.5f;     // how much nearness to the survivor counts against the cursor
        public const float FixtureMoveCancelSpeed = 0.15f;   // a timed action is abandoned once the survivor moves faster than this
        public const float FixturePartSeconds = 0.35f;       // a door, drawer or lid swings/slides fully in this long
        public const float FixtureNoiseSmall = 2.5f;         // a cupboard, drawer or switch being worked
        public const float FixtureNoiseDoor = 4f;            // a door opening or closing

        // ---- Power grid (mirrors the water mains: the lights go out for good a few days in) ----
        public const int PowerShutoffDayMin = 3;             // earliest day the grid can fail
        public const int PowerShutoffDayMax = 7;             // .. and the latest
        public const int PowerShutoffLateDelayMin = 1;       // a save from before the grid existed fails this many days after loading..
        public const int PowerShutoffLateDelayMax = 4;       // .. up to this many

        // ---- Everyday comfort: warmth from fixtures (a lit burner, a hot shower) ----
        public const float ComfortWarmthMax = 0.25f;         // the most a nearby heat source lifts the body's warmth target (0..1 scale)

        // ---- Salvage from the home's newer furniture (porcelain, the TV and the radio give nothing) ----
        public const int SalvageFridgePlanks = 0, SalvageFridgeNails = 2;   // a steel carcass: bolts and brackets
        public const int SalvageShowerPlanks = 0, SalvageShowerNails = 1;   // the stall's frame
    }
}
