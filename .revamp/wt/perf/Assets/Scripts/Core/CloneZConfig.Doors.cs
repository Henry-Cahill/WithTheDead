namespace CloneZ
{
    // The starter home's doors and windows (HomeDoor, HomeWindow): open and close, lock, peephole, curtains, look out, climb through.
    public static partial class CloneZConfig
    {
        // ---- The starter home's doors (HomeDoor): open, close, lock, peephole, knock ----
        public const float DoorReach = 1.8f;                 // how close (m, flat) to a door's middle the survivor must stand
        public const float DoorSwingSeconds = 0.45f;         // a leaf swings fully open or shut in this long
        public const float DoorKnockNoise = 8f;              // a knock carries this far (plus the dead's hearing) — a lure
        public const float DoorRattleNoise = 2f;             // trying the handle of a locked door
        public const float DoorPeepSeconds = 0.8f;           // putting an eye to the peephole
        public const float DoorPeepRange = 14f;              // how far up the street the peephole shows (less after dark)
        public const float DoorDoorstepRange = 2.5f;         // one of the dead this close is "right outside the door"

        // ---- The starter home's windows (HomeWindow): sash, latch, curtains / blind, look out, climb through ----
        public const float WindowReach = 2.0f;               // flat reach to the glazing's middle (the dining table stands under one)
        public const float WindowSashSeconds = 0.6f;         // the lower sash slides fully up or down in this long
        public const float WindowNoise = 2.5f;               // a sash being thrown up or banged down
        public const float WindowLookSeconds = 1.0f;         // peering out
        public const float WindowLookRange = 15f;            // how far out the survivor can count the dead (less after dark)
        public const float WindowUnderRange = 3f;            // one of the dead this close is "right under the window"
        public const float WindowClimbSeconds = 1.6f;        // clambering through an open window
        public const float WindowClimbNoise = 4f;            // .. and the racket it makes
        public const float WindowClimbEndurance = 0.04f;     // .. and the breath it costs
        public const float WindowClimbRecheck = 0.25f;       // seconds a "is the far side clear?" answer is reused for the prompt
    }
}
