namespace CloneZ
{
    // The kitchen's settings (KitchenSink, KitchenStove, KitchenFridge): the sink, the gas range, the fridge-freezer and what its cupboards and drawers start with.
    public static partial class CloneZConfig
    {
        // ---- The kitchen sink (KitchenSink): tap, plug and basin ----
        public const float KitchenBasinCapacity = 2f;        // water the basin holds with the plug in (drinks / bottles' worth)
        public const float KitchenBasinDraw = 1f;            // .. and what one drink or one bottle takes from it
        public const float KitchenBasinFillSeconds = 10f;    // seconds of running tap to fill the basin from empty
        public const float KitchenBasinDrainSeconds = 3f;    // seconds a full basin takes to drain once the plug is pulled
        public const float KitchenTapNoiseRadius = 2.5f;     // a running tap is heard this far off..
        public const float KitchenTapNoiseInterval = 3f;     // .. every this many seconds
        public const float KitchenHearRange = 6f;            // the survivor notices the tap or basin from this close (m, flat)
        public const float KitchenStatusInterval = 0.25f;    // seconds between the kitchen fixtures' checks of the mains, the power and the clock
        public const float KitchenWashSeconds = 3f;          // "Wash your hands and face"
        public const float KitchenWashWater = 0.5f;          // .. and the basin water it takes once the mains are off
        public const float KitchenRinseSeconds = 4f;         // "Rinse your wounds"
        public const float KitchenRinseWater = 0.5f;
        public const float KitchenRinseSepsis = 0.3f;        // sepsis washed out of each open (unbandaged, uninfected) wound
        public const float KitchenDishesSeconds = 6f;        // "Wash the dishes"
        public const float KitchenDishesWater = 1f;
        public const float KitchenDishesCalm = 4f;           // panic eased by the routine of washing up
        public const float KitchenDishesDirtyHours = 12f;    // in-game hours until there's washing-up again
        public const float KitchenWipeSeconds = 2.5f;        // "Wipe down the worktop"

        // ---- The gas range (KitchenStove): works without power ----
        public const float StoveWarmthRadius = 2.2f;         // how far a lit stove's warmth reaches (m)
        public const float StoveBurnerWarmth = 0.04f;        // warmth per lit burner (0..ComfortWarmthMax scale)
        public const float StoveOvenWarmth = 0.06f;          // .. for the oven
        public const float StoveOvenOpenWarmth = 0.03f;      // .. and a little more with the hot oven's door open
        public const float StoveGlowRange = 1.8f;            // the warm light of lit burners
        public const float StoveGlowIntensity = 0.8f;

        // ---- The fridge-freezer (KitchenFridge): keeps food while the house has power ----
        public const float FridgeSpoilFactor = 0.25f;        // food in the powered fridge ages at this fraction of the normal rate
        public const float FreezerSpoilFactor = 0f;          // .. and in the powered freezer
        public const float FridgeCoolInterval = 1f;          // seconds between cooling passes
        public const float FridgeLampRange = 1.1f;           // the cool-white lamp inside, lit while the door is open
        public const float FridgeLampIntensity = 0.7f;

        // ---- What the kitchen's cupboards and drawers start with (the wave home is meant to be lean) ----
        public const float SinkCupboardEmptyChance = 0.5f;   // the cupboard under the sink: odds and ends
        public const int SinkCupboardMaxStacks = 1;
        public const float CounterCupboardEmptyChance = 0.4f;// the counter's cupboard: a few tins and packets
        public const int CounterCupboardMaxStacks = 2;
        public const float CounterDrawerEmptyChance = 0.55f; // the cutlery drawer and the drawer of odds and ends
        public const int CounterDrawerMaxStacks = 1;
        public const float WallCupboardEmptyChance = 0.5f;   // the wall cupboard over the counter
        public const int WallCupboardMaxStacks = 1;
        public const float FreezerEmptyChance = 0.5f;
        public const int FreezerMaxStacks = 1;
        public const float FridgeEmptyChance = 0.35f;
        public const int FridgeMaxStacks = 2;
    }
}