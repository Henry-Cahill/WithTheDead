namespace CloneZ
{
    // The raid town's doors, zombie interactions, and windows settings.
    public static partial class CloneZConfig
    {
        // ---- The raid town's doors (RaidDoor): the home's door, plus the dead battering it and a boot to a locked one ----
        public const float RaidDoorHealthInterior = 110f;      // a hollow inside door: eight blows or so from one of the dead
        public const float RaidDoorHealthExterior = 190f;      // a solid outside door on a deadbolt
        public const float RaidDoorHealthGlass = 130f;         // a shop's glazed door in its metal frame
        public const float RaidDoorHitNoise = 10f;             // every blow on a door carries this far
        public const float RaidDoorBreakNoise = 18f;           // .. and the crash when it gives way
        public const float RaidDoorHearRange = 22f;            // the survivor is told (a caption) of a door being battered this near
        public const float RaidDoorCaptionGap = 8f;            // .. at most this often per door
        public const float RaidDoorKickSeconds = 3.2f;         // kicking (shouldering) a locked door in
        public const float RaidDoorKickInterval = 0.8f;        // .. a boot every this many seconds
        public const float RaidDoorKickNoise = 14f;            // .. each one carrying this far
        public const float RaidDoorKickEndurance = 0.22f;      // .. and the breath the whole job costs
        public const float RaidDoorKickMinEndurance = 0.15f;   // too winded to try below this
        public const float RaidInteriorDoorOpenChance = 0.65f; // a fresh raid: inside doors mostly left standing open
        public const float RaidExteriorDoorShutChance = 0.5f;  // .. outside doors shut half the time
        public const float RaidExteriorDoorLockChance = 0.35f; // .. and a shut one bolted from inside this often

        // ---- The dead and the raid's shut doors (ZombieAI) ----
        public const float ZombieDoorRethink = 0.4f;           // seconds between a chasing zombie's looks for a shut door in its way
        public const float ZombieDoorIdleRethink = 2f;         // .. when none offered a way through, it looks again only after this long
        public const float ZombieDoorSearchRange = 15f;        // how far (flat) it will make for one
        public const float ZombieDoorStandOff = 1.0f;          // where it stands to batter one: this far out from the wall line
        public const float ZombieDoorForgetTime = 25f;         // while at a door it keeps at it this long after last seeing its prey

        // ---- The raid town's windows (RaidWindow): the home's window, plus smashed glass and boards ----
        public const float RaidWindowSmashSeconds = 0.7f;      // putting an elbow (or a boot) through the glass
        public const float RaidWindowSmashNoise = 18f;         // breaking glass carries down the street
        public const float RaidWindowClearSeconds = 3f;        // knocking the last shards out of the frame
        public const float RaidWindowClearNoise = 3f;
        public const float RaidWindowCutChance = 0.4f;         // climbing over the shards opens a cut this often
        public const float RaidWindowCutDeepChance = 0.45f;    // .. a deep one (a laceration, not a scratch) this often
        public const float RaidWindowCutDamage = 4f;           // .. and the blood it costs at once
        public const float RaidWindowPrySeconds = 9f;          // tearing the boards off bare-handed
        public const float RaidWindowPryHammerSeconds = 5.5f;  // .. with a claw hammer
        public const float RaidWindowPryCrowbarSeconds = 3.5f; // .. with a crowbar
        public const float RaidWindowPryInterval = 1.4f;       // a nail shrieks out every this many seconds
        public const float RaidWindowPryNoise = 9f;            // .. heard this far
        public const float RaidWindowPryEndurance = 0.08f;     // the breath it costs
        public const float RaidWindowLatchedChance = 0.7f;     // a fresh raid: an intact window's sash lock turned this often
        public const float RaidWindowOpenChance = 0.05f;       // .. its sash left up (a snowbound town shuts its windows)
        public const float RaidWindowDrawnChance = 0.35f;      // .. its curtains drawn (blind down) this often
        public const float RaidWindowBoardSeconds = 7f;        // nailing planks across a window (it takes a hammer)
        public const int RaidWindowBoardPlanks = 3;            // .. a plank to each board
        public const int RaidWindowBoardNails = 6;             // .. and a nail at each end of it
        public const float MapWindowSmashedChance = 0.06f;     // the statewide map's working windows: found smashed this often
        public const float MapWindowBoardedChance = 0.05f;     // .. a ground-floor one boarded over outside this often

        // ---- The statewide map's partitions (PartitionWall): battered through by the dead, knocked through and boarded up ----
        public const float RaidPartitionHealth = 160f;         // studs under plaster: half again an inside door (a dozen blows or so)
        public const float RaidPartitionBreachWidth = 2.0f;    // the hole a wall gives way in: wide enough that the map's agents (0.5 m
                                                               // round, on 1/6 m voxels) always pass it, however the wall's turned
        public const float RaidPartitionBreachHeight = 2.4f;   // .. as tall as the map's outside doorways (the whole wall when that's near it)
        public const float RaidPartitionHitNoise = 10f;        // every blow on one carries as far as a blow on a door
        public const float RaidPartitionBreakNoise = 20f;      // .. and the crash when it gives way a little further
        public const float RaidPartitionPatchSeconds = 9f;     // nailing planks across a hole (it takes a hammer)
        public const int RaidPartitionPatchPlanks = 4;         // .. that many planks
        public const int RaidPartitionPatchNails = 8;          // .. and a nail at each end of each
        public const float RaidPartitionPatchInterval = 0.9f;  // .. a hammer blow every this many seconds while at it
        public const float RaidPartitionPatchHealth = 200f;    // the boards hold as long as a built wall would
        public const float ZombieWallRethink = 0.35f;          // seconds between a stuck zombie's looks for a stretch of wall to batter
        public const float ZombieWallForgetTime = 40f;         // while at a wall it keeps at it this long after last seeing its prey

        // ---- The depot's roll-up doors (RollUpShutter) ----
        public const float RollUpReach = 2.2f;                 // flat reach to the doorway's middle (the doorways are 3 m wide)
        public const float RollUpWorkSeconds = 0.9f;           // hauling the curtain up or pulling it down
        public const float RollUpRollSeconds = 1.8f;           // .. which then rolls fully up or down in this long
        public const float RollUpNoise = 16f;                  // a steel curtain's rattle
        public const float RollUpOpenChance = 0.5f;            // a fresh raid finds it up this often
    }
}
