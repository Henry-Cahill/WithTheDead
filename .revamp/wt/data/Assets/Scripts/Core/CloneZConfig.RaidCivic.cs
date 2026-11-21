namespace CloneZ
{
    // The raid town's workplace settings (CivicSink, CivicRest, CivicRadio, the filing cabinets, lockers, safes and benches): each fixture's noise, reach, timing and how bare it starts.
    public static partial class CloneZConfig
    {
        // ---- The raid town's workplace furniture kit (filing cabinets, lockers, safes, crates, benches, desks) ----
        public const float CivicSpareEmptyChance = 0.7f;     // a piece's extra places beyond its main one (a desk's lower drawers,
        public const int CivicSpareMaxStacks = 1;            // .. the second cupboard under a counter): mostly bare, a thing at most
        public const float CivicFilingEmptyChance = 0.6f;    // each drawer of a filing cabinet: files, mostly..
        public const int CivicFilingMaxStacks = 2;           // .. and a few desk odds and ends
        public const float CivicRackEmptyChance = 0.8f;      // the armory's weapon rack: chained up, all but empty
        public const float CivicDrawerTravel = 0.4f;         // how far a filing cabinet's or a desk's drawer pulls out
        public const float CivicPencilDrawerTravel = 0.24f;  // .. and a desk's shallow pencil drawer
        public const float CivicBenchDrawerTravel = 0.36f;   // .. and a tool bench's drawers
        public const float CivicLockerDoorAngle = 96f;       // a locker door swings just past square, clear of its neighbour
        public const float CivicCabinetDoorAngle = 100f;     // a medicine cabinet's doors
        public const float CivicSafeDoorAngle = 100f;        // the gun safe's heavy door..
        public const float CivicSafeDoorSeconds = 1.1f;      // .. swings slowly
        public const float CivicSafeNoise = 4f;              // .. and clanks
        public const float CivicCounterDoorAngle = 95f;      // the steel counter's cupboard doors
        public const float CivicLidAngle = 80f;              // a crate's lid stands up against the wall behind it..
        public const float CivicLidSeconds = 0.6f;           // .. lifted at a steady pace
        public const float CivicCarTrunkAngle = 62f;         // the garage car's trunk lid
        public const float CivicCarDoorAngle = 58f;          // .. and its passenger door
        public const float CivicViceTravel = 0.07f;          // how far the bench vice's jaw opens
        public const float CivicAjarChance = 0.3f;           // odds a locker bank has a door left ajar (or a cabinet a drawer half out)

        // ---- The working sink in a steel counter (CivicSink) ----
        public const float CivicSinkWashSeconds = 3f;        // "Wash your hands and face"
        public const float CivicSinkScrubSeconds = 5f;       // "Scrub up" at the clinic's sink
        public const float CivicSinkRinseSeconds = 4f;       // "Rinse your wounds"

        // ---- Lying down on a bunk, a hospital bed or a cot (CivicRest) ----
        public const float CivicBunkComfort = 0.35f;         // sitting on the edge of a hard bed
        public const float CivicBedEdgeHeight = 0.68f;       // the hospital bed's edge, sat on

        // ---- Seats in the town's workplaces ----
        public const float CivicOfficeChairComfort = 0.45f;
        public const float CivicStoolComfort = 0.15f;
        public const float CivicWheelchairComfort = 0.35f;
        public const float CivicSofaComfort = 0.7f;

        // ---- The field radio and the CB set (CivicRadio) ----
        public const float CivicRadioReach = 2.2f;           // listened to from this close..
        public const float CivicRadioCaptionRange = 7f;      // .. followed from this far at medium volume
        public const float CivicRadioListenCalm = 0.4f;      // panic shed per second listening: voices that still sound in charge
        public const float CivicRadioCallNoise = 6f;         // keying the handset and calling in

        // ---- The desk's CRT monitor (CivicToggle) ----
        public const float CivicMonitorNoise = 1.2f;         // the pop and whine of a CRT warming up
    }
}
