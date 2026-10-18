namespace CloneZ
{
    public static partial class CloneZConfig
    {
        // ---- The raid town's interactive storage (every cupboard, drawer, locker and fridge in its buildings) ----
        public const float RaidMainStorageEmptyChance = 0.06f;   // a raid's main containers (wardrobe, locker, fridge…) are seldom bare
        public const float RaidSmallStorageEmptyChance = 0.55f;  // its drawers and cupboards often are
        public const int RaidSmallStorageMaxStacks = 2;          // .. and hold a couple of things at most
        public const float RaidStorageEmptyScale = 0.75f;        // the starter home's own furniture, met in a raid: a little less likely to be empty..
        public const int RaidStorageExtraStacks = 1;             // .. and holding one more stack than the lean wave home allows
    }
}
