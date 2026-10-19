namespace CloneZ
{
    // The raid town's houses' settings (RaidHomeBath, RaidHomeWasher, the sofa and TV): the bath's taps and water, the washing machine's cycle, and the sofa and den's comfort.
    public static partial class CloneZConfig
    {
        // ---- The raid town's houses: the sofa, the living room's TV, the den's chair ----
        public const float SofaSeatHeight = 0.52f;           // a sofa cushion, sat on..
        public const float SofaSeatBack = 0.06f;             // .. and how far back on it (local z) the hips rest
        public const float SofaSeatSpread = 0.45f;           // the two places on a 2 m sofa sit this far either side of its middle
        public const float SofaComfort = 0.7f;
        public const float SofaCushionsEmptyChance = 0.7f;   // down the back of the cushions: usually nothing at all..
        public const int SofaCushionsMaxStacks = 1;          // .. and never much
        public const float RaidTvReach = 3.6f;               // a living room's TV is watched from the sofa across the room
        public const float OfficeChairSeatHeight = 0.5f;
        public const float OfficeChairComfort = 0.45f;
        public const float MagazineCalmCooldown = 40f;       // seconds before another magazine off the coffee table eases the nerves

        // ---- The ranch's bath (RaidHomeBath): taps, plug and a bathful of water ----
        public const float BathFillSeconds = 36f;            // seconds of running taps to fill the plugged bath
        public const float BathDrainSeconds = 14f;           // seconds a full bath takes to drain once the plug is pulled
        public const float BathCoolSeconds = 420f;           // seconds a hot bath takes to go stone cold
        public const float BathSoakLevel = 0.45f;            // how full (0..1) the bath must be to get in
        public const float BathWashLevel = 0.15f;            // .. and to wash in, once the mains are off
        public const float BathDrinkShare = 0.08f;           // a drink or a bottle drawn from the standing water, once the mains are off
        public const float BathTapNoiseRadius = 3f;          // running taps are heard this far off..
        public const float BathTapNoiseInterval = 3f;        // .. every this many seconds
        public const float BathTapTurnNoise = 1f;            // the taps turned
        public const float BathDrainNoise = 3f;              // the plug pulled on a bathful: a long gurgle
        public const float BathTapAngle = 90f;               // degrees the taps' heads turn to run
        public const float BathSoakCalmPerSec = 2.5f;        // panic soaked away per second in a hot bath (less as it cools)
        public const float BathSoakRestPerSec = 0.002f;      // .. and fatigue
        public const float BathSoakWarmPerSec = 0.02f;       // body warmth (0..1) a hot bath gives back per second..
        public const float BathSoakWarmTarget = 0.58f;       // .. up to this
        public const float BathColdChillPerSec = 0.008f;     // a cold bath takes warmth away instead
        public const float BathWetness = 0.9f;               // how wet the survivor climbs out
        public const float BathWashSeconds = 3f;             // "Wash your hands and face"
        public const float BathSoakComfort = 0.95f;          // lying back in the water
        public const float BathWarmth = 0.1f;                // a hot bath's steam lifts the warmth target this much beside it..
        public const float BathWarmthRadius = 1.6f;          // .. fading out this far away (m)

        // ---- The cottage's washing machines (RaidHomeWasher) ----
        public const float WasherCycleSeconds = 100f;        // a full wash, start to spin
        public const float WasherSpinSeconds = 25f;          // the last stretch of it: the spin, louder still
        public const float WasherNoiseInterval = 3f;         // a running machine is heard every this many seconds..
        public const float WasherNoiseRadius = 9f;           // .. this far off while it washes..
        public const float WasherSpinNoiseRadius = 15f;      // .. and this far off while it spins (it walks on the concrete)
        public const float WasherDoorAngle = 105f;           // degrees the porthole door swings open
        public const float WasherDoorNoise = 1.5f;           // the porthole clunking open or shut
        public const float WasherSetSeconds = 1.5f;          // turning the dial and pulling the knob
        public const float WasherEmptyChance = 0.65f;        // somebody's last load: more often than not, nothing worth taking
        public const int WasherMaxStacks = 2;
    }
}
