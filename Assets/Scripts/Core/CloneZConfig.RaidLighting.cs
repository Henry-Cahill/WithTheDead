namespace CloneZ
{
    public static partial class CloneZConfig
    {
        // ---- The raid town's lights, switches and wall decor ----
        public const float RaidDeadBulbChance = 0.1f;            // a ceiling fitting (or a desk lamp left off) whose bulb has gone; a lit room always keeps one that works
        public const float RaidLampRange = 3.6f, RaidLampIntensity = 1.7f;   // a desk / table / bedside lamp
        public const float RaidLampFlickerChance = 0.12f;        // .. that flickers like a failing bulb
        public const float RaidPorchLightRange = 6f, RaidPorchLightIntensity = 1.8f;   // a house's porch lantern, as the starter home's
        public const bool RaidPorchLightShadows = true;          // like the home's: unshadowed, it would shine through the front wall into the dark room behind
        public const float RaidClockStoppedChance = 0.2f;        // a wall clock whose battery has run down
    }
}
