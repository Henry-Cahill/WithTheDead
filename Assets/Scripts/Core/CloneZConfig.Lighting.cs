namespace CloneZ
{
    public static partial class CloneZConfig
    {
        // ---- Home lighting: wall switches, lamps and the breaker panel ----
        public const float LightSwitchNoise = 1.2f;          // a wall switch or a lamp's switch clicking
        public const float BreakerNoise = 3f;                // the main breaker thrown (a heavy clunk)
        public const float BreakerDoorNoise = 1.5f;          // the breaker panel's tin door
        public const float LampCheckInterval = 0.5f;         // seconds between a lamp's checks of its switch, the power and the daylight
        public const float LampDuskHysteresis = 0.05f;       // a dusk sensor that has come on waits for this much more daylight before going off
        public const float SwitchSyncInterval = 0.25f;       // seconds between a switch's checks that its toggles still match their circuits
        public const float StoreLightRange = 4.5f, StoreLightIntensity = 1.5f;   // the store room's bulkhead light
        public const float BathLightRange = 3.5f, BathLightIntensity = 1.4f;     // the bathroom's vanity light

        // ---- Wall decor: pictures and the clock ----
        public const float PictureCalm = 3f;                 // panic eased by stopping to look at a picture..
        public const float PictureCalmCooldown = 180f;       // .. at most once in this many (real) seconds per picture
        public const float PictureMaxRoll = 10f;             // degrees a picture can be knocked crooked
        public const float PictureKnockChance = 0.6f;        // odds a blow to its wall knocks a picture crooked
        public const float ClockListenSeconds = 4f;          // how long the survivor stands listening to the clock tick
        public const float ClockCalm = 2f;                   // panic eased by listening to it..
        public const float ClockCalmCooldown = 120f;         // .. at most once in this many (real) seconds

        // ---- The yard: mailbox, garden tap and doormat ----
        public const float MailDeliveryHour = 8f;            // the day's post is in the box from this hour
        public const float MailLineSeconds = 2.4f;           // reading time per line of a letter
        public const float GardenWaterRange = 8f;            // the hose reaches garden plots this far from the outside tap
        public const float GardenWaterSeconds = 4f;          // how long watering the garden takes
        public const float HoseCoilSeconds = 2.5f;           // winding the hose back onto its reel
        public const float OutsideTapNoise = 1.5f;           // the outside tap's squeaky handle
    }
}
