// CLONE-Z — a 3D reimagining of Project Zomboid.
// Tuning constants grounded in values decompiled from the reference game
// (ref/_decompiled). Values are re-expressed for a 3D, metre/second world.
namespace CloneZ
{
    /// <summary>
    /// Central gameplay tuning. Where a value maps to a Project Zomboid
    /// constant, the PZ origin is noted in a comment. Partial: each area of the starter home's
    /// interactive fixtures keeps its tuning in its own <c>CloneZConfig.*.cs</c> file.
    /// </summary>
    public static partial class CloneZConfig
    {
        // ---- Player movement (m/s). PZ relative speeds: sneak .25 / walk .5 / run .75 / sprint 1.0 ----
        public const float PlayerSneakSpeed = 1.4f;
        public const float PlayerWalkSpeed = 2.4f;
        public const float PlayerRunSpeed = 4.0f;
        public const float PlayerSprintSpeed = 5.6f;
        public const float PlayerTurnSpeed = 14f;     // how fast the body swings to face the aim point
        public const float Gravity = -20f;

        // ---- Endurance (PZ CharacterStat.Endurance 0..1, warn .5, danger .25) ----
        public const float EnduranceMax = 1f;
        public const float EnduranceWarning = 0.5f;
        public const float EnduranceDanger = 0.25f;
        public const float EnduranceSprintDrain = 0.11f; // per second while sprinting
        public const float EnduranceRunDrain = 0.035f;   // per second while running
        public const float EnduranceRegen = 0.07f;       // per second while not exerting

        // ---- Survival stats (PZ CharacterStat: Hunger/Thirst 0..1, rise over time) ----
        public const float HungerRatePerSec = 1f / 900f; // ~15 min to fully starve
        public const float ThirstRatePerSec = 1f / 600f; // ~10 min to fully dehydrate

        // ---- Health (PZ BodyDamage.overallBodyHealth 0..100) ----
        public const float HealthMax = 100f;
        public const float HealthRegenPerSec = 0.4f;     // PZ standardHealthAddition 0.002/tick * ~30fps
        public const float StarvingHealthDrainPerSec = 0.6f;
        public const float DehydratedHealthDrainPerSec = 0.9f;

        // ---- Infection (PZ: bites are effectively fatal; ZombieInfection 0..100) ----
        public const float InfectionMax = 100f;
        public const float InfectionGrowthPerSec = 1.1f;      // climbs after a bite
        public const float InfectionHealthDrainPerSec = 0.9f; // sickness damages health as it rises

        // ---- Melee combat (PZ CombatConfigKey) ----
        public const float BaseWeaponDamageMultiplier = 0.3f;      // BASE_WEAPON_DAMAGE_MULTIPLIER
        public const float HeadHitDamageSplitModifier = 3.0f;      // HEAD_HIT_DAMAGE_SPLIT_MODIFIER
        public const float PlayerReceivedDamageMultiplier = 0.4f;  // PLAYER_RECEIVED_DAMAGE_MULTIPLIER
        public const float NonPlayerReceivedDamageMultiplier = 1.5f;// NON_PLAYER_RECEIVED_DAMAGE_MULTIPLIER
        public const float CritChanceDefault = 0.05f;              // ADDITIONAL_CRITICAL_HIT_CHANCE_DEFAULT (5%)
        public const float CritChanceFromBehind = 0.30f;           // ..._FROM_BEHIND (30%)
        public const float MeleeEnduranceCost = 0.03f;             // per swing (derived from ENDURANCE_LOSS_*)

        // ---- Firearms (PZ firearm to-hit: min 5, maxStart 95, max 100; move -5, run -15, sprint -25) ----
        // Aim "quality" (0..1) is derived from those PZ percentages, then mapped to a
        // spread cone: perfect aim = a tight cone, poor aim (running) = a wide one.
        public const float FirearmAimBase = 0.95f;          // standing aim (maxStart 95%)
        public const float FirearmAimMin = 0.05f;           // never worse than 5%
        public const float FirearmAimMax = 1.00f;           // absolute best (100%)
        public const float FirearmAimPenaltyWalk = 0.05f;   // moving on foot (-5)
        public const float FirearmAimPenaltyRun = 0.15f;    // running (-15)
        public const float FirearmAimPenaltySprint = 0.25f; // sprinting (-25)
        public const float FirearmAimSneakBonus = 0.03f;    // steadier when crouched
        public const float FirearmSpreadTight = 1.0f;       // cone half-angle (deg) at perfect aim
        public const float FirearmSpreadWide = 14f;         // cone half-angle (deg) at worst aim
        public const float FirearmEnduranceCost = 0.015f;   // per shot

        // ---- Item condition & durability (PZ InventoryItem.Condition / ConditionLowerChanceOneIn) ----
        // Damageable gear wears with use: each qualifying use rolls a 1-in-N chance (the item's
        // ConditionLossChance) to shed a condition point, and at zero the item breaks. Per-item
        // MaxCondition / ConditionLossChance live on ItemDef in ItemDatabase.
        public const float FirearmJamConditionThreshold = 0.5f;  // guns below half condition can jam
        public const float FirearmJamChanceAtBroken = 0.35f;     // jam chance as condition nears zero

        // Bare hands \u2014 the melee profile a survivor falls back to when a weapon breaks (mirrors
        // the serialized defaults on PlayerCombat).
        public const float BareHandDamage = 0.9f;
        public const float BareHandRange = 2.4f;
        public const float BareHandArc = 90f;
        public const float BareHandCooldown = 0.55f;
        public const float BareHandKnockback = 2.5f;

        // ---- Item repair (PZ diminishing repair: each patch restores less and caps lower) ----
        // A repair consumes one material (duct tape for gear, thread + a needle for clothing) and
        // raises the item's condition back toward max. The reachable cap falls with every prior
        // repair, so a heavily-patched item can never be brought fully back to pristine.
        public const float RepairRestoreFraction = 0.30f;    // each repair restores up to 30% of max condition
        public const float RepairCapBase = 0.90f;            // the first repair can reach 90% of max
        public const float RepairCapDecayPerRepair = 0.12f;  // every prior repair lowers the reachable cap
        public const float RepairCapFloor = 0.30f;           // but the cap never drops below 30% of max
        public const float RepairCapCarpentryBonus = 0.02f;  // a handy survivor patches a little closer to pristine (per Carpentry level)
        public const float XpRepairPerFix = 10f;             // Carpentry XP earned for a successful repair

        // ---- Equipped-weapon weight relief (PZ: a weapon in hand weighs less on the carry total) ----
        public const float EquippedWeaponWeightReduction = 0.5f;  // the wielded weapon counts at half weight

        // ---- Zombie health (PZ toughness): tough 3.5 / normal 1.8 / fragile 0.5 (+Rand 0..0.3) ----
        public const float ZombieHealthNormal = 1.8f;
        public const float ZombieHealthTough = 3.5f;
        public const float ZombieHealthFragile = 0.5f;
        public const float ZombieHealthVariance = 0.3f;

        // ---- Zombie speed by type (PZ speedType) expressed in m/s ----
        public const float ZombieShamblerSpeed = 1.15f;     // speedType 3 (0.55 mod)
        public const float ZombieFastShamblerSpeed = 2.4f;  // speedType 2 (0.85 mod)
        public const float ZombieSprinterSpeed = 5.2f;      // speedType 1 (0.85 mod, always running)
        public const float ZombieCrawlerSpeed = 0.6f;

        // ---- Zombie perception (PZ VISION_RADIUS_MIN/MAX 10..20 tiles) ----
        public const float ZombieVisionRadius = 16f;
        public const float ZombieVisionAngle = 160f;   // degrees, full FOV
        public const float ZombieHearingRadius = 6f;   // added to a noise's own radius
        public const float ZombieForgetTime = 8f;      // seconds to give up after losing the target

        // ---- Zombie attack (PZ: lunge when distToTarget >= 0.8; bite 85% / laceration 65%) ----
        public const float ZombieAttackRange = 1.6f;
        public const float ZombieAttackWindup = 0.45f; // seconds from lunge start to damage
        public const float ZombieAttackCooldown = 1.4f;
        public const float ZombieBiteChance = 0.85f;        // baseBiteChance 85
        public const float ZombieScratchChance = 0.65f;     // baseLacerationChance 65
        public const float ZombieBiteDamage = 12f;          // immediate wound damage on a bite
        public const float ZombieScratchDamage = 5f;

        // ---- Noise (loudness in metres of attraction radius) ----
        public const float NoiseFootstepWalk = 4f;
        public const float NoiseFootstepRun = 9f;
        public const float NoiseFootstepSprint = 14f;
        public const float NoiseFootstepSneak = 1.5f;
        public const float NoiseMeleeSwing = 6f;
        public const float NoiseMeleeHit = 10f;
        public const float NoiseGunshotPistol = 32f;   // gunfire draws the whole horde
        public const float NoiseGunshotShotgun = 40f;
        public const float NoiseDryFire = 2f;

        // ---- Spawning / waves ----
        public const int WaveBaseZombies = 6;
        public const int WavePerWaveIncrease = 3;
        public const float WaveBreakSeconds = 6f;
        // Zombies spawn at random points across the whole arena; this keep-away radius around the
        // survivor is the only exclusion, so one never materialises right in their face.
        public const float SpawnMinPlayerDistance = 12f;
        public const float SprinterUnlockWave = 4;   // sprinters start appearing from this wave
        public const float ToughUnlockWave = 2;

        // ---- World ----
        // Playable half-extent (ground is a bit larger). Set per mode by the bootstrapper before the
        // world is built: the wave arena keeps the default; an extraction raid map is larger.
        public const float WaveArenaHalfSize = 34f;
        public static float ArenaHalfSize = WaveArenaHalfSize;
        /// <summary>Centre of the playable square (y = 0). Zero on every fixed map; the streamed Kentucky map
        /// (<see cref="KentuckyStreamer"/>) moves it with the survivor, so spawns, the A-Life and the clamps that
        /// work across "the arena" work across the live square around them.</summary>
        public static UnityEngine.Vector3 ArenaCenter = UnityEngine.Vector3.zero;
        /// <summary>The edge of the world on a streamed map (world XZ), past which nobody walks or drives; null on
        /// fixed maps, whose arena square is their edge.</summary>
        public static UnityEngine.Rect? HardBounds;
        public const int LootContainerCount = 50;    // world containers scattered across the arena each build

        /// <summary>A fixed map's arena: <paramref name="half"/> either side of the origin, no world edge beyond it.</summary>
        public static void ResetArena(float half)
        {
            ArenaHalfSize = half;
            ArenaCenter = UnityEngine.Vector3.zero;
            HardBounds = null;
        }

        /// <summary>True when <paramref name="p"/> lies inside the arena square, <paramref name="margin"/> in from its
        /// edge (a negative margin reaches past it).</summary>
        public static bool InArena(UnityEngine.Vector3 p, float margin = 0f)
        {
            float h = ArenaHalfSize - margin;
            return UnityEngine.Mathf.Abs(p.x - ArenaCenter.x) <= h && UnityEngine.Mathf.Abs(p.z - ArenaCenter.z) <= h;
        }

        /// <summary><paramref name="p"/> moved into the arena square (<paramref name="margin"/> in from its edge) and
        /// inside the world's edge; y is kept.</summary>
        public static UnityEngine.Vector3 ClampToArena(UnityEngine.Vector3 p, float margin = 0f)
        {
            float h = UnityEngine.Mathf.Max(0f, ArenaHalfSize - margin);
            p.x = UnityEngine.Mathf.Clamp(p.x, ArenaCenter.x - h, ArenaCenter.x + h);
            p.z = UnityEngine.Mathf.Clamp(p.z, ArenaCenter.z - h, ArenaCenter.z + h);
            return ClampToWorld(p, margin);
        }

        /// <summary><paramref name="p"/> moved inside the world's edge (<paramref name="margin"/> in from it) when the
        /// map has one; y is kept.</summary>
        public static UnityEngine.Vector3 ClampToWorld(UnityEngine.Vector3 p, float margin = 0f)
        {
            if (!HardBounds.HasValue) return p;
            UnityEngine.Rect r = HardBounds.Value;
            float mx = UnityEngine.Mathf.Min(margin, r.width * 0.5f), mz = UnityEngine.Mathf.Min(margin, r.height * 0.5f);
            p.x = UnityEngine.Mathf.Clamp(p.x, r.xMin + mx, r.xMax - mx);
            p.z = UnityEngine.Mathf.Clamp(p.z, r.yMin + mz, r.yMax - mz);
            return p;
        }

        /// <summary>A random point (y = 0) in the arena square, <paramref name="margin"/> in from its edge.</summary>
        public static UnityEngine.Vector3 RandomArenaPoint(System.Random rng, float margin = 0f)
        {
            float h = UnityEngine.Mathf.Max(1f, ArenaHalfSize - margin);
            return new UnityEngine.Vector3(ArenaCenter.x + (float)(rng.NextDouble() * 2.0 - 1.0) * h, 0f,
                ArenaCenter.z + (float)(rng.NextDouble() * 2.0 - 1.0) * h);
        }

        // ---- Day / night cycle ----
        public const float DayLengthSeconds = 720f;      // real seconds for a full 24h day (12 min)
        public const float StartHour = 8.5f;             // world begins mid-morning

        // ---- Extraction raids (a winter night: 00:40 on December 17th, out before dawn) ----
        public const float RaidStartHour = 0f + 40f / 60f;     // 00:40
        public const float RaidDayLengthSeconds = 8640f;       // 6 real minutes per in-game hour
        public const float RaidDawnHour = 6f;                  // the exits close at dawn (~32 real minutes in)
        public const int RaidStartMonth = 12;
        public const int RaidStartDay = 17;
        public const float RaidSpawnProtectionSeconds = 60f;   // the dead ignore a freshly-arrived survivor
        public const int RaidZombiePopulation = 40;            // wanderers roaming the town at once
        public const float DaySunIntensity = 1.25f;      // directional light at noon
        public const float NightSunIntensity = 0.05f;    // faint moonlight
        public const float NightLightFloor = 0.09f;      // ambient light level (0..1) at deep night

        // ---- Weather (each state blends toward these rain/fog targets 0..1) ----
        public const float WeatherMinDuration = 35f;     // seconds a weather state lasts (min)
        public const float WeatherMaxDuration = 95f;     // .. and max
        public const float WeatherBlendSpeed = 0.12f;    // how fast rain/fog ease toward target per sec

        // ---- Zombie vision environment penalties (PZ: darkness/rain/fog reduce sight) ----
        public const float ZombieNightVisionMultiplier = 0.5f;  // sight radius scale in full dark
        public const float ZombieRainVisionPenalty = 0.30f;     // up to -30% sight in heavy rain
        public const float ZombieFogVisionPenalty = 0.55f;      // up to -55% sight in thick fog

        // ---- Fatigue (PZ CharacterStat.Fatigue 0..1; 1 = exhausted) ----
        public const float FatigueRatePerSec = 1f / 600f;       // ~10 min awake (idle) to fully tire
        public const float FatigueExertionBonus = 1f / 320f;    // extra drain while running/sprinting
        public const float FatigueNightMultiplier = 1.6f;       // tires faster past nightfall
        public const float FatigueExhaustedThreshold = 0.85f;   // can no longer sprint above this
        public const float FatigueMovePenalty = 0.28f;          // up to -28% move speed when exhausted
        public const float FatigueEnduranceRegenPenalty = 0.6f; // stamina recovers up to 60% slower

        // ---- Sleep ----
        public const float SleepRange = 2.6f;            // must be this close to a bed
        public const float SleepDangerRadius = 12f;      // zombies inside this block / interrupt sleep
        public const float SleepTimeScale = 26f;         // clock fast-forwards this much while asleep
        public const float SleepFatigueRecoveryPerSec = 0.16f;  // fatigue shed per real second asleep
        public const float SleepHealthRegenPerSec = 1.2f;       // PZ sleeping regen (0.02/tick) scaled up
        public const float SleepWakeHour = 7f;           // auto-wake around dawn

        // ---- Loot & inventory ----
        public const float PlayerCarryCapacity = 12f;        // kg the player can carry (PZ base ~8 + a bag)
        public const float EncumbranceStart = 0.6f;         // fraction of capacity before movement slows
        public const float EncumbranceMinMultiplier = 0.62f; // slowest move multiplier at a full load
        public const float InteractRange = 2.6f;            // metres to reach and search a container

        // ---- Container search time (seconds to rummage before contents reveal) ----
        public const float SearchTimeSmall = 1.4f;    // desk, medicine cabinet
        public const float SearchTimeMedium = 2.3f;   // crate, locker, fridge
        public const float SearchTimeLarge = 3.2f;    // tool bench
        public const float SearchTimeCorpse = 1.6f;   // frisking a body
        public const float CorpseLifetime = 150f;     // corpse loot despawns after this

        // ---- Skills & XP (PZ PerkFactory thresholds + IsoGameCharacter perk mods) ----
        public const int SkillMaxLevel = 10;
        // Cumulative XP to REACH levels 1..10. PZ combat/agility perk curve (PerkFactory.AddPerk).
        public static readonly int[] SkillXpCurve = { 50, 100, 200, 500, 1000, 2000, 3000, 4000, 5000, 6000 };
        // Fitness/Strength are passive and train far slower (PZ physical perk curve).
        public static readonly int[] PhysicalXpCurve = { 1000, 2000, 4000, 6000, 12000, 20000, 40000, 60000, 80000, 100000 };

        public const float WeaponLevelDamageIncrement = 0.10f; // PZ WEAPON_LEVEL_DMG_INCREMENT: +10% melee dmg / weapon level
        public const float StrengthCarryPerLevel = 0.05f;      // +5% carry capacity per Strength level (added to base)
        public const float StrengthMeleePerLevel = 0.03f;      // +3% melee damage per Strength level
        public const float SprintSkillSpeedPerLevel = 0.02f;   // +2% sprint speed per Sprinting level

        // XP rewards for actions (tuned so early levels arrive within a play session).
        public const float XpMeleeHit = 3f;
        public const float XpMeleeKill = 12f;
        public const float XpStrengthPerHit = 6f;      // heavy swings build Strength
        public const float XpFirearmShot = 1f;
        public const float XpFirearmHit = 6f;
        public const float XpFirearmKill = 14f;
        public const float XpReload = 8f;
        public const float XpSprintPerSec = 3f;
        public const float XpFitnessPerSec = 7f;       // fitness curve is huge, so this trains slowly
        public const float XpNimblePerSec = 3f;        // moving with a gun out (combat stance)
        public const float XpSneakPerSec = 4f;
        public const float XpLightfootPerSec = 2.5f;
        public const float XpFitnessOnHurt = 20f;      // surviving a hit toughens you
        public const float XpCarpentryPerPlank = 25f;  // building/reinforcing a barricade trains Carpentry

        // ---- Crafting: barricades (planks + nails + a hammer -> defensive walls) ----
        // PZ barricades hold up to 4 planks, each nailed on; more planks = more HP.
        public const int BarricadeMaxPlanks = 4;
        public const int BarricadePlankCost = 1;             // planks spent per plank added
        public const int BarricadeNailCost = 1;              // boxes of nails spent per plank added
        public const float BarricadeHealthPerPlank = 90f;    // HP each plank contributes
        public const float BarricadeBuildRange = 2.6f;       // reach to build or reinforce
        public const float BarricadeBuildTime = 1.7f;        // seconds of hammering per plank (before Carpentry)
        public const float BarricadePlaceDistance = 1.7f;    // how far in front a new barricade is placed
        public const float BarricadeWidth = 2.0f;            // footprint width (m)
        public const float BarricadeDepth = 0.35f;           // footprint depth (m)
        public const float BarricadeHeight = 1.7f;           // wall height (m)
        public const float NoiseHammering = 12f;             // metres hammering attracts (loud work draws zombies)
        public const float ZombieBarricadeDamage = 14f;      // damage a zombie deals per bash to a barricade
        public const float CarpentryBuildSpeedPerLevel = 0.06f; // build time shrinks this much per Carpentry level
        public const float CarpentryHealthPerLevel = 0.08f;     // barricade HP grows this much per Carpentry level

        // ---- Construction: houses (planks + nails + a hammer -> floors, walls, doorways, roofs) ----
        // A full building system layered on top of barricades: in build mode the survivor picks a
        // piece from a palette and places it on a 2 m tile grid, so floors tile, walls sit on the
        // tile edges and roofs cap the walls. Walls and doorways block movement and carve the
        // NavMesh (zombies must bash them down like a barricade); floors and roofs are structural.
        public const float HouseTileSize = 2.0f;            // grid cell / wall length (m) — matches BarricadeWidth
        public const float HouseWallHeight = 2.6f;          // wall (and doorway) height (m)
        public const float HouseWallThickness = 0.2f;       // wall depth (m)
        public const float HouseFloorThickness = 0.12f;     // floor slab thickness (m)
        public const float HouseRoofThickness = 0.16f;      // roof slab thickness (m)
        public const float HouseDoorwayGapWidth = 1.4f;     // clear opening in a doorway (m) — roomy enough to walk through (player is ~0.8m wide)
        public const float HouseDoorwayLintelHeight = 0.5f; // the header beam above a doorway opening (m); visual only, no collider
        public const float HousePlaceReach = 6f;            // farthest from the survivor a piece can be placed (m)

        // Per-piece plank / nail cost, hammering time (seconds, before Carpentry) and health.
        public const int HouseFloorPlankCost = 2, HouseFloorNailCost = 1;
        public const int HouseWallPlankCost = 3, HouseWallNailCost = 2;
        public const int HouseDoorwayPlankCost = 3, HouseDoorwayNailCost = 2;
        public const int HouseRoofPlankCost = 2, HouseRoofNailCost = 1;
        public const float HouseFloorBuildTime = 2.0f;
        public const float HouseWallBuildTime = 2.8f;
        public const float HouseDoorwayBuildTime = 3.0f;
        public const float HouseRoofBuildTime = 2.4f;
        public const float HouseFloorHealth = 120f;         // structural HP (dismantled, not usually bashed)
        public const float HouseWallHealth = 200f;          // a wall is tougher than a plank barricade
        public const float HouseDoorwayHealth = 150f;       // the frame around a doorway
        public const float HouseRoofHealth = 120f;

        // A window is a wall with an open glazing band — cheaper, a bit weaker, and a clear line of
        // sight/fire, but it still blocks passage (the sill is too high to step over) and carves the navmesh.
        public const int HouseWindowPlankCost = 2, HouseWindowNailCost = 1;
        public const float HouseWindowBuildTime = 2.6f;
        public const float HouseWindowHealth = 160f;        // a weak point, between wall (200) and doorway (150)
        public const float HouseWindowSillHeight = 0.95f;   // solid sill height (m); above the CC step, so no walking through
        public const float HouseWindowHeadHeight = 0.55f;   // solid header height (m); the band between them is the opening
        public const float HousePieceSnapEpsilon = 0.6f;    // a new piece is refused within this of an existing one (per layer)

        // Disassembly — prying a player-built piece (or barricade) apart recovers part of its
        // materials. The refund is lossy so it can't be exploited for free repositioning or XP.
        public const float HouseSalvageFraction = 0.6f;     // fraction of a piece's build cost returned on dismantle
        public const float HouseSalvageTime = 2.2f;         // seconds of prying to dismantle a piece (before Carpentry)
        public const float XpCarpentrySalvage = 6f;         // Carpentry XP for dismantling a structure

        // Salvage (planks, nails) from dismantling PRE-PLACED world objects. Flat per-type yields —
        // these weren't built from a known cost, so they don't use HouseSalvageFraction.
        public const int SalvageBedPlanks = 3, SalvageBedNails = 1;        // wooden bed frame
        public const int SalvageStovePlanks = 1, SalvageStoveNails = 2;    // mostly metal
        public const int SalvageContainerPlanks = 2, SalvageContainerNails = 1;  // a wooden crate/locker
        public const int SalvageCoverPlanks = 2, SalvageCoverNails = 1;    // a crate/pallet stack
        public const int SalvageBarrelPlanks = 2, SalvageBarrelNails = 1;  // a rain barrel
        public const int SalvageTapPlanks = 0, SalvageTapNails = 1;        // a plumbed tap (mostly pipe)
        // The starter home's loose furniture (HomeFurniture). Lamps and cardboard boxes yield nothing.
        public const int SalvageCounterPlanks = 3, SalvageCounterNails = 1;      // kitchen base cabinets
        public const int SalvageTablePlanks = 2, SalvageTableNails = 1;          // dining table
        public const int SalvageChairPlanks = 1, SalvageChairNails = 0;          // kitchen chair
        public const int SalvageNightstandPlanks = 1, SalvageNightstandNails = 1; // bedside table
        public const int SalvageDresserPlanks = 2, SalvageDresserNails = 1;      // chest of drawers
        public const int SalvageBookshelfPlanks = 2, SalvageBookshelfNails = 1;  // bookcase
        public const int SalvageArmchairPlanks = 1, SalvageArmchairNails = 1;    // the frame under the stuffing
        public const int SalvageShelvingPlanks = 0, SalvageShelvingNails = 2;    // steel shelving: bolts only

        // ---- Wounds / bleeding / pain / medical (PZ BodyDamage) ----
        // A zombie strike now leaves a lasting wound that bleeds and hurts until treated.
        // Bleed rates are HP/second; pain is on the PZ Pain scale (0..100, scratchPain 18 / bitePain 25).
        public const float PainMax = 100f;                       // PZ CharacterStat.Pain 0..100
        public const float PainDecayPerSec = 1.6f;               // pain eases as time passes / wounds close
        public const float PainMoveStart = 25f;                  // pain below this doesn't slow you
        public const float PainMovePenalty = 0.22f;              // up to -22% move speed at max pain
        public const float PainMeleePenalty = 0.30f;             // up to -30% melee damage at max pain
        public const float PainAimPenalty = 0.22f;               // up to -0.22 firearm aim quality at max pain

        // Scratch (light): PZ scratchPain 18, bleeds slowly, closes fastest.
        public const float ScratchBleedPerSec = 0.30f;
        public const float ScratchPain = 18f;
        public const float ScratchHealSeconds = 80f;
        public const float ScratchSepsisChance = 0.10f;          // odds an untreated scratch festers

        // Laceration (medium): a deeper tear — the common non-bite wound (PZ laceration 65%).
        public const float LacerationBleedPerSec = 0.70f;
        public const float LacerationPain = 22f;
        public const float LacerationHealSeconds = 150f;
        public const float LacerationSepsisChance = 0.20f;

        // Bite (heavy): PZ bitePain 25. Also seeds the fatal zombie infection (handled in PlayerStats).
        public const float BiteBleedPerSec = 1.05f;
        public const float BitePain = 25f;
        public const float BiteHealSeconds = 220f;
        public const float BiteSepsisChance = 0.35f;

        // Wound infection (sepsis — distinct from the zombie virus). An open or dirtily-bandaged
        // wound festers over time; disinfectant prevents it, antibiotics cure it.
        public const float SepsisRisePerSec = 1f / 90f;          // untreated wound reaches sepsis in ~90s
        public const float SepsisDirtyBandageMult = 0.6f;        // a dirty (rag) bandage only slows it
        public const float SepsisHealthDrainPerSec = 0.35f;      // an infected wound sickens the body
        public const float SepsisPainBonus = 20f;                // .. and adds this much pain while it festers
        public const float AntibioticDurationSec = 240f;         // one dose keeps working this long
        public const float AntibioticSepsisCurePerSec = 1f / 40f;// clears a wound infection over ~40s
        public const float PainkillerDurationSec = 90f;          // painkillers blunt pain for this long

        // Bandaging & healing.
        public const float WoundHealSleepMultiplier = 5f;        // wounds knit far faster while asleep
        public const float BandageSepsisReduction = 0.25f;       // a fresh clean bandage cleans a wound a little
        public const float FirstAidHealSeconds = 40f;            // a wound dressed with a first-aid kit closes faster

        // ---- Panic / fear (PZ CharacterStat.Panic 0..100; moodle levels at 6 / 30 / 65 / 80) ----
        // A survivor grows fearful when the dead are near or in view: each newly-spotted
        // zombie spikes panic, a looming horde sustains it, and it ebbs once the danger
        // passes. High panic makes the hands shake (worse firearm aim) and, past a point,
        // robs the nerve to sleep. Grounded in PZ BodyDamage panic values + MoodleStat.PANIC.
        public const float PanicMax = 100f;                      // PZ CharacterStat.Panic ceiling
        public const float PanicSightRadius = 14f;               // metres within which a visible zombie frightens
        public const float PanicPerNewZombie = 7f;               // PZ BodyDamage.panicIncreaseValue (per newly-seen zombie)
        public const float PanicHuntingWeight = 1.6f;            // a zombie hunting YOU is scarier than a wandering one
        public const float PanicThreatWeight = 20f;              // proximity-weighted threat -> sustained panic floor
        public const float PanicSustainRisePerSec = 25f;         // how fast panic climbs toward the threat floor
        public const float PanicSafeDecayPerSec = 14f;           // panic shed per sec with NO zombie in sight
        public const float PanicDecayPerSec = 6f;                // slower ebb while the dead linger but none are new
        public const float PanicNightMultiplier = 1.3f;          // fear bites harder in the dark (eases toward 1 by day)
        public const float PanicBleedingFloor = 25f;             // bleeding keeps you at least this uneasy (PZ: bleeding adds panic)
        public const float PanicTickInterval = 0.2f;            // panic is re-evaluated at 5 Hz (mimics PZ's discrete update)
        // Panic moodle thresholds (PZ MoodleStat.PANIC = 0, 6, 30, 65, 80).
        public const float PanicLevel1 = 6f;                     // "Uneasy"
        public const float PanicLevel2 = 30f;                    // "Anxious"
        public const float PanicLevel3 = 65f;                    // "Panicked" — too rattled to sleep
        public const float PanicLevel4 = 80f;                    // "Terrified"
        public const float PanicAimPenalty = 0.35f;              // up to -0.35 firearm aim quality at max panic (PZ PANIC_TO_HIT)
        // Beta blockers (PZ: betaEffect ~6600 frames @30fps; removes ~0.6 panic/frame while active).
        public const float BetaBlockerDurationSec = 200f;        // one dose keeps the nerves steady this long
        public const float BetaBlockerCalmPerSec = 18f;          // panic bled off per sec while a dose is active
        public const float BetaBlockerSpikeDampen = 0.35f;       // fresh sightings frighten far less while dosed

        // ---- Body temperature & wetness (PZ Thermoregulator / getColdStrength/getHotStrength) ----
        // The survivor's core warmth drifts toward an ambient target set by the time of day,
        // the weather and what they're wearing. Straying too far from comfortable brings the
        // cold (or heat) moodles; the extremes sap health. Temperature is 0..1 where 0 is
        // hypothermic, 0.5 is comfortable and 1 is overheating. Grounded in PZ's thermoregulation.
        public const float TempStart = 0.5f;              // body starts thermally neutral
        public const float TempNeutral = 0.5f;            // ambient with no day swing / weather
        public const float TempComfortLow = 0.40f;        // below this the survivor feels cold
        public const float TempComfortHigh = 0.62f;       // above this the survivor feels hot
        public const float TempDaySwing = 0.15f;          // warmest ~14:00, coldest ~02:00
        public const float TempRainChill = 0.16f;         // heavy rain pulls the ambient down this much
        public const float TempCloudChill = 0.05f;        // overcast skies cool things a little
        public const float TempFogChill = 0.04f;          // .. as does thick fog
        public const float TempAdjustPerSec = 0.03f;      // how fast body temp drifts toward the target
        public const float TempWetChill = 0.22f;          // being soaked pulls the warmth target down hard
        public const float TempShelterWarmth = 0.18f;     // sleeping in a bed keeps the cold at bay
        public const float TempHypothermiaThreshold = 0.14f;  // below this the cold saps health
        public const float TempHyperthermiaThreshold = 0.86f; // above this the heat saps health
        public const float TempHypothermiaHealthDrain = 0.6f; // HP/sec at the depths of cold
        public const float TempHyperthermiaHealthDrain = 0.5f;// HP/sec at peak heat
        public const float TempColdFatiguePerSec = 1f / 240f; // shivering wears you out
        public const float TempHotThirstPerSec = 1f / 300f;   // sweating makes you thirsty
        public const float TempColdMovePenalty = 0.15f;   // up to -15% move speed when frozen stiff
        public const float TempColdAimPenalty = 0.12f;    // up to -0.12 firearm aim from shivering hands

        // ---- Wetness (rain soaks you; you dry off over time — wet clothes stop insulating) ----
        public const float WetRainPerSec = 1f / 45f;      // soaked after ~45s in heavy rain
        public const float WetSnowPerSec = 1f / 90f;      // soaked slower in snow than rain
        public const float WetDryPerSec = 1f / 120f;      // dries over ~2 min (faster when warm)
        public const float WetWaterResistFactor = 0.9f;   // a raincoat keeps off up to 90% of the soaking
        public const float WetInsulationLoss = 0.7f;      // soaked clothes lose up to 70% of their warmth
        public const float WetMovePenalty = 0.06f;        // heavy, wet clothes drag you down a touch
        public const float WetnessDampThreshold = 0.3f;   // counts as "damp" past this

        // ---- Clothing (insulation + bite/scratch defence; PZ ClothingItem) ----
        public const float ClothingBlockedDamageMult = 0.35f; // a bite/scratch stopped by fabric still bruises

        // ---- Food: freshness, cooking & food poisoning (PZ food rot + Nausea/Sickness) ----
        // Perishable food rots over in-game hours: fresh for the first stretch of its shelf
        // life, then stale, then rotten. Eating spoiled — or raw-hazardous — food risks food
        // poisoning, which drives the Sickness stat (PZ CharacterStat.Sickness / Nausea moodle).
        public const float FoodStaleFraction = 0.5f;      // past this fraction of shelf life it turns "stale"
        public const float FoodStaleHungerMult = 0.6f;    // stale food fills you less
        public const float FoodRottenHungerMult = 0.25f;  // rotten food barely feeds you at all
        public const float FoodStaleSickChance = 0.20f;   // odds a serving of stale food turns your stomach
        public const float FoodRawSickChance = 0.55f;     // odds raw-hazard food (raw meat) makes you ill
        public const float FoodRottenSickChance = 0.90f;  // rotten food nearly always sickens you

        // Sickness / food poisoning (PZ CharacterStat.Sickness 0..100; Nausea moodle).
        public const float SicknessMax = 100f;
        public const float SicknessStaleGain = 22f;       // a bad serving of stale food
        public const float SicknessRottenGain = 45f;      // rotten food hits hard
        public const float SicknessRawGain = 38f;         // undercooked / raw meat
        public const float SicknessDecayPerSec = 1f / 12f;// fades over time once nothing new is eaten
        public const float SicknessSleepMult = 3f;        // rest speeds the recovery
        public const float SicknessRegenBlock = 15f;      // above this, health/wounds stop knitting
        public const float SicknessDrainThreshold = 60f;  // once sickness climbs past here it saps health
        public const float SicknessHealthDrainPerSec = 0.35f; // HP/sec bled off while violently ill
        public const float SicknessMoveStart = 45f;       // queasy above here: a small move penalty
        public const float SicknessMovePenalty = 0.12f;   // up to -12% move speed when badly ill
        // Sickness moodle thresholds -> Fine / Queasy / Nauseous / Sick / Ill.
        public const float SicknessLevel1 = 10f;
        public const float SicknessLevel2 = 35f;
        public const float SicknessLevel3 = 65f;
        public const float SicknessLevel4 = 85f;

        // Cooking (a stove makes raw food safe and more nourishing; trains the Cooking skill).
        public const float CookStationRange = 2.6f;       // must be this close to a stove to cook
        public const float XpCookingPerItem = 8f;         // Cooking XP awarded per item cooked
        public const float CookingHungerPerLevel = 0.03f; // +3% nutrition from cooked food per Cooking level

        // ---- Farming (till -> sow -> water -> harvest; PZ Farming perk + IsoFarmPlot moisture) ----
        // A patch of tilled soil grows a sown crop over in-game hours. Crops need moisture to
        // grow: plots dry out over time, rain refills them, and a watering can tops them up. A
        // parched plot wilts and eventually dies; a harvested plot returns to bare soil to resow.
        // The Farming perk (PZ PerkFactory: standard 50..6000 curve) raises the harvest yield.
        public const float FarmActionRange = 2.4f;        // reach to tend a plot
        public const float FarmTillDistance = 1.6f;       // how far in front bare ground is tilled
        public const float FarmPlotSpacing = 1.5f;        // plots must be at least this far apart
        public const float FarmPlotSize = 1.4f;           // footprint (m) of a tilled plot
        public const int FarmSeedCost = 1;                // seeds sown per planting

        // Timed farming actions (seconds; like a container search, they cancel if you walk off).
        public const float FarmTillTime = 3.0f;           // breaking new ground with a trowel
        public const float FarmPlantTime = 1.5f;          // sowing seeds
        public const float FarmWaterTime = 1.2f;          // watering a plot
        public const float FarmHarvestTime = 2.0f;        // pulling a mature crop
        public const float FarmClearTime = 1.5f;          // clearing a dead crop

        // Moisture (0..1). Plots dry over time; rain and the watering can add water back.
        public const float FarmWaterMax = 1f;
        public const float FarmWaterStart = 0.55f;        // moisture a freshly-sown plot holds
        public const float FarmWaterDryPerHour = 1f / 20f;  // dries out over ~20 in-game hours
        public const float FarmWaterRainPerHour = 1f / 6f;  // heavy rain refills in ~6 in-game hours
        public const float FarmWaterLowThreshold = 0.12f;   // below this the crop stops growing and wilts
        public const float FarmCanRefill = 0.5f;          // moisture one watering-can pass adds
        public const float FarmWaterTopUpBelow = 0.95f;   // only offer to water a plot below this

        // Crop health (0..1): a parched plot wilts and can die; watering recovers it slowly.
        public const float FarmWiltPerHour = 1f / 16f;    // health lost per hour while bone dry
        public const float FarmRecoverPerHour = 1f / 40f; // health regained per hour when watered

        // Farming XP + yield (Farming uses the standard skill curve; higher level = bigger harvests).
        public const float XpFarmTill = 3f;               // breaking new ground
        public const float XpFarmPlant = 5f;              // sowing seeds
        public const float XpFarmHarvestPerCrop = 4f;     // per crop pulled at harvest
        public const float FarmYieldPerLevel = 0.12f;     // +12% harvest yield per Farming level

        // ---- Water: mains supply, rain collection, purification (PZ water shut-off + tainted water) ----
        // Tap water flows freely until the mains are shut off a few days in (PZ's scheduled water cut).
        // After that the survivor must collect rain in a barrel — but rain water is tainted and has to
        // be boiled at a stove before it's safe, or drunk raw at the risk of a sick stomach.
        public const int WaterShutoffDayMin = 2;          // earliest day the mains can run dry
        public const int WaterShutoffDayMax = 5;          // .. and the latest
        public const float WaterSourceRange = 2.4f;       // reach to drink from or fill at a source
        public const float WaterSourceSpacing = 1.4f;     // sources sit at least this far apart
        public const float WaterDrinkAmount = 0.5f;       // thirst slaked by a drink or a bottle (fraction of Thirst)

        // Rain barrel — fills from the weather; each draw empties it a little; a drought runs it dry.
        public const float WaterBarrelMax = 4f;           // units a barrel can hold
        public const float WaterBarrelStart = 1.5f;       // units a fresh barrel starts with
        public const float WaterBarrelDrawPerUse = 1f;    // units drawn per drink / bottle filled
        public const float WaterBarrelRainPerHour = 1.2f; // units heavy rain adds per in-game hour

        // Tainted water (rain / barrel water) sickens you unless boiled first (PZ tainted water).
        public const float WaterTaintedSickChance = 0.5f; // odds a raw drink of tainted water turns your stomach
        public const float SicknessTaintedGain = 30f;     // sickness a bad drink of tainted water adds

        // Built-in "Ignore Raycast" layer index. Zombies live here so line-of-sight
        // and aim raycasts pass through them, while obstacles (Default) still block.
        public const int IgnoreRaycastLayer = 2;
    }
}
