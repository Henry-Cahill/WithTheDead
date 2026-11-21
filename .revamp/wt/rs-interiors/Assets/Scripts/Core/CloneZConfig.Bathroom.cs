namespace CloneZ
{
    // The bathroom's fittings (ToiletFixture, BasinFixture, ShowerFixture, TowelRail, MirrorCabinet): seat and lid, the taps, the water and the heat.
    public static partial class CloneZConfig
    {
        // ---- The bathroom's toilet (ToiletFixture): seat, lid, flush and the cistern's last water ----
        public const float ToiletUseSeconds = 4f;            // "Use the toilet"
        public const float ToiletUseCalm = 2f;               // panic eased by a minute's peace and privacy
        public const float ToiletCisternRefillSeconds = 20f; // seconds for an empty cistern to fill again (only while the mains run)
        public const float ToiletFlushMinLevel = 0.75f;      // cistern level (0..1) a proper flush needs
        public const float ToiletCisternDrinks = 2f;         // drinks (or bottles) a full cistern holds — tainted, PZ-style
        public const float ToiletFlushNoise = 4f;            // a flush is heard this far off
        public const float ToiletLidNoise = 1.5f;            // a seat or lid let down
        public const float ToiletCisternLidNoise = 2f;       // the heavy porcelain cistern lid lifted off or set back
        public const float ToiletSitComfort = 0.2f;          // how restful the closed lid is to sit on
        public const float ToiletLiftNoise = 0.8f;           // a seat or lid lifted
        public const float ToiletSeatOpenAngle = 92f;        // degrees the seat and lid lift, to lean on the cistern
        public const float ToiletCisternLidAngle = 86f;      // degrees the cistern lid is stood up against the wall
        public const float ToiletLeverAngle = 35f;           // degrees the flush lever goes down
        public const float ToiletLeverHold = 0.5f;           // seconds it's held down before it springs back

        // ---- The bathroom basin (BasinFixture): mixer tap, plug and a basinful of water ----
        public const float BasinFillSeconds = 8f;            // seconds of running tap to fill the plugged basin
        public const float BasinDrainSeconds = 4f;           // seconds a full basin takes to drain once the plug is pulled
        public const float BasinTapNoiseRadius = 2f;         // a running tap is heard this far off..
        public const float BasinTapNoiseInterval = 3f;       // .. every this many seconds
        public const float BasinWashSeconds = 3f;            // "Wash your hands and face"
        public const float BasinWashFreshen = 1f;            // how refreshing a splash of basin water is (WaterUse.Freshen)
        public const float BasinBrushSeconds = 3f;           // "Brush your teeth"
        public const float BasinBrushCalm = 2f;
        public const float BasinShaveSeconds = 5f;           // "Have a shave"
        public const float BasinShaveCalm = 4f;
        public const float BasinTapTurnNoise = 1f;           // the tap's lever worked
        public const float BasinLeverAngle = 25f;            // degrees the mixer's lever lifts to run
        public const float BasinDrinkLevel = 0.5f;           // basin level (0..1) that still makes a drink once the mains are off
        public const float BasinWashWater = 0.5f;            // share of a basinful a wash uses once the mains are off..
        public const float BasinBrushWater = 0.1f;           // .. brushing your teeth
        public const float BasinShaveWater = 0.25f;          // .. and a shave

        // ---- The shower stall (ShowerFixture): hot while the house has power for the water heater ----
        public const float ShowerSeconds = 8f;               // "Take a shower"
        public const float ShowerNoiseRadius = 3f;           // a running shower is heard this far off..
        public const float ShowerNoiseInterval = 3f;         // .. every this many seconds
        public const float ShowerHotCalm = 25f;              // panic washed away by a hot shower
        public const float ShowerHotRest = 0.05f;            // .. and fatigue
        public const float ShowerColdChill = 0.08f;          // body warmth (0..1) a cold shower knocks off
        public const float ShowerColdPanic = 5f;             // .. and the shock of it
        public const float ShowerWetness = 0.75f;            // how wet the survivor steps out (at least)
        public const float ShowerWarmth = 0.15f;             // a hot shower's steam lifts the warmth target this much beside it
        public const float ShowerWarmthRadius = 1.8f;        // .. fading out this far away (m)
        public const float ShowerSprayFlicker = 0.08f;       // seconds between the running spray's two looks
        public const float ShowerDoorAngle = 65f;            // degrees the glass door swings out (clear of the bathroom door's sweep)
        public const float ShowerDoorNoise = 1.5f;           // the glass door clicking open or shut
        public const float ShowerMixerNoise = 1f;            // the mixer's lever turned
        public const float ShowerSoakAfter = 1.5f;           // seconds under the water before stepping out early still leaves you wet

        // ---- The towel rail (TowelRail) and the mirror cabinet over the basin ----
        public const float TowelDrySeconds = 3f;             // "Dry yourself off"
        public const float TowelDampHours = 3f;              // in-game hours the towel hangs damp after use
        public const float TowelDampLeaves = 0.15f;          // how wet a still-damp towel leaves you (0..1)
        public const float TowelCrookedRoll = 7f;            // the most degrees a towel slung back on the rail hangs askew
        public const float MirrorCalmCooldown = 60f;         // seconds before another look in the mirror moves the nerves again
        public const float MirrorCabinetEmptyChance = 0.45f; // the cabinet is meant to be lean
        public const int MirrorCabinetMaxStacks = 2;
        public const float MirrorCalm = 3f;                  // panic eased by a reassuring look at yourself..
        public const float MirrorUnsettle = 4f;              // .. or raised by an alarming one
    }
}
