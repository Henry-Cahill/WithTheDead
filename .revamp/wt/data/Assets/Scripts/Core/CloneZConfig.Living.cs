namespace CloneZ
{
    // The living room, bedroom and store room settings (SeatFixture, BedFixture, LivingReading, LivingReceiver): sitting and lying down, reading at the bookcase, and the portable TV and the kitchen radio.
    public static partial class CloneZConfig
    {
        // ---- Sitting (dining chairs, the armchair, the edge of the bed, the toilet lid) ----
        public const float SeatEnduranceBonus = 0.5f;        // seated, endurance comes back at ×(1 + this + comfort) the standing rate
        public const float SeatFatigueEasePerSec = 0.0015f;  // fatigue shed per second seated, ×comfort
        public const float SeatPanicEasePerSec = 1.2f;       // extra panic ebb per second seated, ×(0.5 + comfort)
        public const float SeatMaxReach = 3f;                // a seat farther than this (flat m) can't be sat on from where the survivor stands
        public const float SeatStandClearance = 0.38f;       // capsule radius tested for a free spot to stand up into
        public const float ChairSeatHeight = 0.47f;          // a dining chair's seat above the floor..
        public const float ChairSeatBack = -0.04f;           // .. and how far back on it (local z) the hips rest
        public const float ChairComfort = 0.3f;
        public const float ArmchairSeatHeight = 0.48f;       // the armchair's cushion, sat in..
        public const float ArmchairSeatBack = 0.03f;         // .. the hips just clear of its back cushion
        public const float ArmchairComfort = 0.8f;
        public const float ArmchairFeetUpComfort = 0.95f;    // sunk right back into the cushions
        public const float BedEdgeSeatHeight = 0.53f;        // the edge of the bed, quilt and all
        public const float BedEdgeInset = 0.12f;             // the hips rest this far in from the bed's side
        public const float BedEdgeComfort = 0.5f;
        public const float TableChairRange = 1.2f;           // "Sit at the table" takes a chair whose centre is this close to the table's

        // ---- Lying down on the bed to rest (awake: the clock runs on as normal) ----
        public const float RestEnduranceBonus = 2f;          // endurance comes back at ×(1 + this)
        public const float RestFatigueEasePerSec = 0.002f;   // fatigue shed per second (the waking drain still runs)
        public const float RestPanicEasePerSec = 2f;         // extra panic ebb per second
        public const float MakeBedSeconds = 4f;
        public const float BedReach = 1.9f;                  // from the bed's middle line: keeps the survivor within SleepRange of its middle

        // ---- Reading at the bookcase ----
        public const float ReadBookSeconds = 12f;
        public const float ReadBookPanicRelief = 30f;
        public const float ReadMagazineSeconds = 5f;
        public const float ReadMagazinePanicRelief = 10f;
        public const float TidyPanicRelief = 2f;             // straightening the books, plumping the pillows: a small comfort..
        public const float TidyCooldown = 45f;               // .. once in a while
        public const float ReadingLightLevel = 0.3f;         // daylight below which reading needs a lit lamp or a torch

        // ---- The portable TV and the kitchen radio ----
        public const float ReceiverNoiseInterval = 4f;                         // a set that's on makes a noise this often (s)..
        public static readonly float[] ReceiverNoiseRadius = { 3f, 6f, 10f };  // .. this far, by volume: low / medium / loud
        public static readonly float[] ReceiverHearingScale = { 0.7f, 1f, 1.35f };   // captions carry farther the louder it plays
        public const int ReceiverDefaultVolume = 1;
        public const float ReceiverCaptionMin = 5f, ReceiverCaptionMax = 7f;              // seconds between captions..
        public const float ReceiverStaticCaptionMin = 9f, ReceiverStaticCaptionMax = 13f; // .. and between hisses of dead air
        public const float ReceiverFirstCaption = 1.2f;      // the first caption after switching on or retuning
        public const float ReceiverCaptionSeconds = 6f;      // how long a caption stays up
        public const float ReceiverKnobReach = 1.7f;         // a set can be watched or listened to from farther off, but its knobs need a hand on them
        public const float ReceiverKnobNoise = 1.5f;         // a click of a knob
        public const float TvCaptionRange = 6f;              // the survivor follows what's on from this close
        public const float RadioCaptionRange = 7f;
        public const float TvReach = 2.6f;                   // the TV can be watched from the edge of the bed
        public const float RadioReach = 2.2f;                // the radio can be listened to from the dining table
        public const float TvDeathNoticeRange = 8f;          // the survivor notices the set die from this close
        public const float WatchTvPanicEasePerSec = 1.5f;
        public const float ListenRadioPanicEasePerSec = 1f;
        public const float DeadAirReliefFactor = 0.25f;      // static soothes a lot less
        public const float TvThumpNoise = 3f;                // giving the set a thump
        public const float TvLightRange = 3.6f;              // the screen's glow on the room
        public const float TvLightIntensity = 0.9f;
        public const float TvFlickerInterval = 0.2f;         // the picture's brightness shifts this often

        // ---- What the living room's, bedroom's and store room's furniture holds (the wave home stays lean) ----
        public const float DresserDrawerEmptyChance = 0.5f;
        public const int DresserDrawerMaxStacks = 1;
        public const float DresserDrawerTravel = 0.28f;      // how far a drawer pulls out (clear of the standard lamp beside it)
        public const float BedsideDrawerEmptyChance = 0.45f;
        public const int BedsideDrawerMaxStacks = 1;
        public const float BedsideDrawerTravel = 0.22f;
        public const float BookcaseEmptyChance = 0.55f;
        public const int BookcaseMaxStacks = 1;
        public const float ShelvingEmptyChance = 0.4f;
        public const int ShelvingMaxStacks = 2;
        public const float BoxesEmptyChance = 0.4f;
        public const int BoxesMaxStacks = 2;
    }
}
