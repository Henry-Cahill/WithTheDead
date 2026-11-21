namespace CloneZ
{
    // The raid store's office fixtures, food mart, quarantine tent, and outdoor items settings.
    public static partial class CloneZConfig
    {
        // ---- The county office's working things (RaidStore fixtures) ----
        public const float RaidSwivelSeatHeight = 0.5f;          // an office chair's cushion, sat on..
        public const float RaidSwivelSeatBack = -0.02f;          // .. the hips this far back on it
        public const float RaidSwivelComfort = 0.45f;            // a padded office chair: a little better than a kitchen chair
        public const float RaidSwivelSpinSeconds = 1.4f;         // "Spin round": one full turn on the chair
        public const float RaidSwivelSpinCalm = 3f;              // .. and the panic it takes off, now and then
        public const float RaidSwivelFreeSpin = 540f;            // an empty chair given a shove spins off at this (deg/s)..
        public const float RaidSwivelSpinDrag = 380f;            // .. and slows by this (deg/s each second)
        public const float RaidStoolSeatHeight = 0.63f;          // a shop stool's padded top
        public const float RaidStoolComfort = 0.15f;

        public const float RaidCopierCopySeconds = 5f;           // "Run off a copy": the copier grinding away..
        public const float RaidCopierNoise = 6f;                 // .. loud enough to carry through the office (m)..
        public const float RaidCopierNoiseInterval = 1f;         // .. every second it runs
        public const float RaidCopierJamChance = 0.8f;           // it nearly always jams halfway through
        public const float RaidCopierClearSeconds = 4f;          // fishing the crumpled sheet out of its innards

        public const int RaidCoolerJugMax = 10;                  // the water cooler's jug: at most this many drinks / bottles..
        public const float RaidCoolerJugEmptyChance = 0.3f;      // .. and the odds it's already been drunk dry
        public const float RaidCoolerGlugNoise = 1.5f;           // the jug's glug as water is drawn

        public const float RaidVendingThumpNoise = 8f;           // a fist on a vending machine's side (m)
        public const float RaidVendingThumpChance = 0.3f;        // .. and the odds something works loose into the tray
        public const float RaidVendingThumpSeconds = 0.8f;
        public const float RaidVendingSmashSeconds = 2.5f;       // putting the front glass in..
        public const float RaidVendingSmashNoise = 22f;          // .. heard right across the block
        public const int RaidVendingStockMin = 2, RaidVendingStockMax = 4;   // snack stacks still on the coils
        public const float RaidVendingSearchSeconds = 2f;

        public const int RaidHeaterWaterMin = 6, RaidHeaterWaterMax = 16;   // drinks / bottles left in the water heater's tank
        public const float RaidHeaterValveNoise = 1.5f;          // the drain valve's squeal
        public const float RaidHeaterPilotChance = 0.6f;         // odds the pilot is still burning
        public const float RaidHeaterRelightSeconds = 3f;

        public const float RaidDrumLidNoise = 3f;                // prising the lid off a drum
        public const float RaidDrumEmptyChance = 0.55f;          // drums seldom hold anything worth having..
        public const int RaidDrumMaxStacks = 2;                  // .. and never much

        // ---- The food mart ----
        public const float RaidTillTravel = 0.28f;               // how far the cash drawer shoots out
        public const float RaidTillBellNoise = 3f;               // the register's bell and the drawer's clatter
        public const float RaidMartCoolerLightRange = 1.9f;      // the cool light inside a drinks cooler
        public const float RaidMartCoolerLightIntensity = 0.5f;
        public const float RaidMartCoolerDoorAngle = 100f;
        public const float RaidSafeDialSeconds = 14f;            // "Work the dial": a long job at the safe..
        public const float RaidSafeDialNoise = 3f;               // .. clicking and thumping as it goes (m)..
        public const float RaidSafeDialNoiseInterval = 2f;       // .. every so often
        public const float RaidSafeDoorAngle = 105f;
        public const float RaidSafeEmptyChance = 0.5f;           // a store safe: a coin flip whether it's been emptied..
        public const int RaidSafeMaxStacks = 2;                  // .. and a couple of things if not

        // ---- The quarantine tent ----
        public const float RaidCotSeatHeight = 0.46f;            // a camp cot's canvas, sat on..
        public const float RaidCotLieHeight = 0.48f;             // .. and lain on (bedroll and all)
        public const float RaidHomeMattressHeight = 0.51f;       // the home bed's mattress, which the lying pose is made for
        public const float RaidCotComfort = 0.35f;
        public static readonly float[] RaidFieldRadioNoise = { 3f, 7f, 12f };   // a field radio's squawk by volume: low / medium / loud
        public const float RaidFieldRadioNoiseInterval = 4f;
        public const float RaidFieldRadioCaptionRange = 7f;      // the survivor follows the net from this close at medium volume
        public const float RaidFieldRadioCaptionMin = 6f, RaidFieldRadioCaptionMax = 9f;   // seconds between transmissions
        public const float RaidFieldRadioKnobReach = 1.7f;
        public const float RaidFieldRadioReach = 2.2f;

        // ---- Outdoors: dumpsters, wrecks, pumps, the generator, street furniture ----
        public const float RaidDumpsterLidNoise = 5f;            // a steel lid banging up or down
        public const float RaidDumpsterEmptyChance = 0.4f;
        public const int RaidDumpsterMaxStacks = 2;
        public const float RaidTrashBagEmptyChance = 0.5f;
        public const int RaidTrashBagMaxStacks = 1;
        public const float RaidTrashBagSearchSeconds = 2.5f;
        public const float RaidGloveboxEmptyChance = 0.5f;       // a wreck's glovebox..
        public const int RaidGloveboxMaxStacks = 1;
        public const float RaidTrunkEmptyChance = 0.55f;         // .. and its trunk or bed
        public const int RaidTrunkMaxStacks = 2;
        public const float RaidTrunkSearchSeconds = 2.6f;
        public const float RaidHornNoise = 30f;                  // leaning on a wreck's horn: a lure heard across town
        public const float RaidHornSeconds = 1.5f;
        public const float RaidHornLiveChance = 0.45f;           // odds a wreck's battery still has the juice for it
        public const float RaidIgnitionNoise = 6f;               // a starter motor grinding
        public const float RaidPumpNoise = 2f;
        public const float RaidGeneratorPullSeconds = 1.2f;      // a pull on the starter cord..
        public const float RaidGeneratorPullNoise = 6f;          // .. its rattle..
        public const float RaidGeneratorStartChance = 0.4f;      // .. and the odds it catches
        public const float RaidGeneratorNoise = 26f;             // a running generator: a lure heard right across the site..
        public const float RaidGeneratorNoiseInterval = 2f;      // .. every couple of seconds
        public const float RaidGeneratorFuelMin = 45f, RaidGeneratorFuelMax = 110f;   // seconds of running left in its tank
        public const float RaidBoothEmptyChance = 0.45f;         // the guard booth's desk
        public const int RaidBoothMaxStacks = 2;
    }
}
