# CLONE-Z project map

Generated at the start of every night. Each script with its line count, main types and the first
sentence of its summary. Grep the code for details.

## Assets/Editor
- CloneZBatch.cs (151) CloneZBatch, SmokeGuard: Entry points for running the editor headless (Unity.exe -batchmode -executeMethod ...): regenerating
- CutawayShots.cs (67) CutawayShots: The isometric cutaway's harness (see global::CloneZ.CutawayShots), run headless: Unity.exe -batchmod
- InteriorShots.cs (68) InteriorShots: The interiors' harness (see global::CloneZ.InteriorShots), run headless: Unity.exe -batchmode -proje
- SoundBoard.cs (213) SoundBoard: WithTheDead ▸ Sound Board: every procedural sound by family, any variation played on the spot (no pl
- TownShots.cs (69) TownShots: The Introduction town's harness (see global::CloneZ.TownShots), run headless: Unity.exe -batchmode -

## Assets/Scripts/AI/Creatures
- CreatureCatalog.cs (165) CreatureLoot, CreatureDef, CreatureCatalog: One line of a carcass's loot: an item id, how many, and the chance it is there at all.
- CreatureContent.cs (87) CreatureContent, PsiShield, ZoneLifeModule: Carcass loot: hides, pelts and the mutant parts the Institute and the Fence pay for.
- CreatureMeshes.cs (363) CreaturePrim, CreaturePartBuilder, UnitMesh: The unit shapes creature bodies are assembled from (each one unit across and one unit tall).
- Creatures.cs (664) Creature, Mode, CreatureBody: The Zone's non-human life.
- PlayerMonster.cs (406) HunterForms, PlayerMonster, HunterNet: The shapes a hunter can take, each with one trick (enemies as puzzles — now with a human brain).

## Assets/Scripts/AI/Humans
- ALife.cs (439) ALifeSquad, ALifeDirector, ZonePeopleModule: One squad of the Zone's people, alive whether or not anyone is watching.
- Companions.cs (239) CompanionService, Order, Candidate: Hired guns.
- HumanLoadouts.cs (356) GunPick, LootPick, HumanLoadout: One weighted gun choice of a loadout; the round defaults to the gun's own.
- HumanNpc.cs (1100) HumanNpc, Mode, HumanNpcAvatar: A person in the Zone: a stalker, a soldier, a scavenger, a warden on a tower, a zealot of the Choir,
- HumanUtil.cs (324) HumanUtil, HumanRunner: Small helpers shared by the Zone's people (stalkers, A-Life squads, outposts, wardens, companions): 
- Outposts.cs (433) OutpostSystem, Outpost, PdaStashes: Warfare outposts (Gray Zone Warfare's PvEvP): sandbagged positions held by a faction's garrison.

## Assets/Scripts/Audio
- AmbienceDirector.cs (531) AmbienceDirector, Bed, Caller: The sound of the world around the survivor, all of it synthesised.
- AudioDirector.cs (143) AudioModule, ListenerRig: Installs the synthesiser as the game's sound backend, starts building every sound in the background,
- AudioJobs.cs (163) AudioJobs, Job, Crew: A small job system for audio rendering: work runs on a couple of low-priority background threads and
- BodyAudio.cs (115) BodyAudio: The survivor's own body, heard from inside the head: breathing through a gas mask, heavy breathing w
- Footsteps.cs (897) FootstepAudio, Surface, Ground: Footsteps that sound like what is underfoot, for everything that walks.
- MusicDirector.cs (980) MusicDirector, Context, Phrase: The score: music that follows what the survivor is going through, rendered in code (MusicScore, Musi
- Sfx.cs (128) Sfx: The game's sound API: positional one-shots, flat sounds and attached loops by id (see SfxIds).
- SfxAcoustics.cs (227) SfxAcoustics, Space: The space the survivor stands in, heard.
- SfxIds.cs (93) SfxBus, SfxMeta, ISfxBackend: The volume buses: each has its own slider in the settings and its own share of ducking.
- SfxMixer.cs (592) SfxMixer, Voice, Delayed: The mixer behind Sfx.
- SfxSynth.cs (425) SfxSynth, Recipe, Rendered: Every sound in the game, synthesised in code (there are no audio assets).
- WeaponAudio.cs (230) WeaponAudio: The sounds of the fight that no single weapon owns.
- ZombieVoices.cs (354) ZombieVoices, Mouth: The voices of the dead.

## Assets/Scripts/Audio/Synth
- SynthCore.cs (998) SynthGen, OnePole, Biquad: The deterministic random source and buffer helpers for one generated sound (one variation of one id)
- SynthStereo.cs (296) StereoBuf, Dsp, Tank: A stereo render: two channels of equal length (beds and music; positional sounds stay mono).
- WavFile.cs (42) WavFile: Writes 16-bit PCM WAV files: the sound board's export and the offline sound tools.

## Assets/Scripts/Audio/Synth/Music
- MusicKit.cs (867) StringTone, BowTone, MusicKit: How a plucked string sounds: how long it rings, how fast its overtones die, how bright the pluck is,
- MusicPreview.cs (320) MusicPreview: Offline previews of the score for the sound tools (never called by the game): each layer over a few 
- MusicRecords.cs (559) MusicRecords, Take: The Kettle's jukebox: four old records — a honky-tonk shuffle, a bluegrass breakdown, a country blue
- MusicScore.cs (1018) MusicPart, MusicCue, MusicScore: The layers of the score.
- SfxSynth.Music.cs (233) SfxSynth, MusicStingers, SfxIds: Stepping out into the Zone: a deep drum, a low cello swelling, a banjo climbing D–A–D–E–F and leavin

## Assets/Scripts/Audio/Synth/Recipes
- SfxSynth.Ambience.cs (2069) SfxSynth, AmbienceKit, Scene: The world around the survivor, all of it synthesised: the weather (wind, rain, the roof overhead, th
- SfxSynth.Creatures.cs (1944) SfxSynth, CreatureKit, Throat: One throat.
- SfxSynth.Foley.cs (1367) SfxSynth, FoleyKit, SfxIds: The foley family's building blocks and recipes (pure DSP on the generator; thread-safe).
- SfxSynth.Footsteps.cs (856) SfxSynth, StepKit, SfxIds: The footstep family's toolbox: contacts, thuds, crunch, pings, splashes, scrapes and squeaks, and th
- SfxSynth.Weapons.cs (1425) SfxSynth, WeaponKit, Surround: Where transients start: clear of the 1.5 ms fade-in every one-shot gets.
- SfxSynth.Zone.cs (1882) SfxSynth, ZoneSfx, V8: One cycle of a band-limited wave from harmonic amplitudes (index 0 = the fundamental).

## Assets/Scripts/Bootstrap
- GameBootstrapper.cs (663) GameBootstrapper: Builds the entire playable world from code — ground, cover, lighting, the player, the isometric came

## Assets/Scripts/Camera
- CinemachineViews.cs (127) CinemachineViews: The camera rig as Cinemachine sees it (the doc's Unity setup: one virtual camera per mode, blended o
- CutawayOccluder.cs (31) CutawayOccluder: Marks a wall (or any tall piece of building) that the isometric camera may cut away when it stands b
- IsoCameraFollow.cs (733) IsoCameraFollow: The player camera.
- IsoCutaway.cs (377) IsoCutaway, Piece, HiddenRenderer: Project Zomboid's wall cutaway for the isometric camera.
- ViewCrosshair.cs (160) ViewCrosshair: The small crosshair at the screen centre in the shoulder and first-person views: a dot and four tick

## Assets/Scripts/Characters
- HumanoidAnimator.Body.cs (349) HumanoidAnimator: Whole-body poses: the gait cycle, crouching, posture, crawling, sitting, lying and death.
- HumanoidAnimator.Hands.cs (440) HumanoidAnimator, HandGoal: Hands and head: two-bone IK for weapon holds, swings, shoves, lunges and jobs (working a fixture, re
- HumanoidAnimator.cs (491) HumanoidGait, HumanoidActivity, HumanoidAnimator: How a humanoid moves: the survivor's walk and run, or one of the dead's gaits.
- HumanoidBodyBuilder.Body.cs (383) HumanoidBodyBuilder: Head, torso, arms and hands.
- HumanoidBodyBuilder.Gear.cs (295) HumanoidBodyBuilder: Legs, feet and footwear, headwear, and the blood on the dead.
- HumanoidBodyBuilder.cs (405) HumanoidBodyBuilder, Ring, Key: Generates the skinned humanoid mesh for one HumanoidShape.
- HumanoidLook.cs (443) BodyBuild, HairStyle, HeadwearStyle: Overall body proportions a humanoid is built with.
- HumanoidMeshFactory.cs (67) HumanoidMesh, HumanoidMeshFactory: A baked humanoid mesh and the colour region drawn by each of its submeshes.
- HumanoidProportions.cs (142) HumanBone, HumanoidProportions: The bones of the humanoid rig, in hierarchy order (a parent always precedes its children).
- HumanoidRig.cs (241) HoldStyle, HumanoidRig: How an item sits in the right hand.
- PlayerAvatar.cs (342) PlayerAvatar: Clone Z's body.
- WeaponHandIK.cs (166) WeaponHandIK, SceneToStreamJob: Hand IK on the weapon with the Animation Rigging package (the doc's "one full-body rig for every vie
- ZombieAvatar.cs (133) ZombieAvatar: A zombie's body: the same humanoid as the survivor, dressed as a random dead survivor (grimy clothes

## Assets/Scripts/Content
- ContentRegistry.cs (157) IContentProvider, ContentRegistry, ContentBuilder: A feature that contributes items, loot-table entries or patches to existing items (ammunition and ar
- ZoneContent.Arms.cs (269) ZoneContent: The Zone's content: ammunition and guns with real ballistics, attachments, body armour and Zone suit
- ZoneContent.Gear.cs (320) ZoneContent: Every artifact id (their effects live in ArtifactCatalog; descriptions never tell).

## Assets/Scripts/Core
- Actor.cs (207) ActorKind, Actor, IZombieAttackable: What sort of creature an Actor is.
- Broadcasts.cs (392) BroadcastStation, Broadcasts, Programme: The stations the home's sets can pick up: the portable TV's three channels, then the kitchen radio's
- Climate.cs (224) Climate: The outdoor weather in real units, for the HUD watch and the body-temperature model.
- CloneZConfig.Bathroom.cs (69) CloneZConfig: The bathroom's fittings (ToiletFixture, BasinFixture, ShowerFixture, TowelRail, MirrorCabinet): seat
- CloneZConfig.Doors.cs (28) CloneZConfig: The starter home's doors and windows (HomeDoor, HomeWindow): open and close, lock, peephole, curtain
- CloneZConfig.Fixtures.cs (31) CloneZConfig: The starter home's interactive fixtures: their reach, cursor pick, timing and noise, the power grid'
- CloneZConfig.Kitchen.cs (55) CloneZConfig: The kitchen's settings (KitchenSink, KitchenStove, KitchenFridge): the sink, the gas range, the frid
- CloneZConfig.Lighting.cs (34) CloneZConfig: The home's lighting, wall decor and yard settings (LightSwitch, LampSwitch, BreakerPanel, MailboxFix
- CloneZConfig.Living.cs (79) CloneZConfig: The living room, bedroom and store room settings (SeatFixture, BedFixture, LivingReading, LivingRece
- CloneZConfig.Raid.cs (14) CloneZConfig: The raid town's interactive storage settings (every cupboard, drawer, locker and fridge in its build
- CloneZConfig.RaidCivic.cs (53) CloneZConfig: The raid town's workplace settings (CivicSink, CivicRest, CivicRadio, the filing cabinets, lockers, 
- CloneZConfig.RaidHomes.cs (54) CloneZConfig: The raid town's houses' settings (RaidHomeBath, RaidHomeWasher, the sofa and TV): the bath's taps an
- CloneZConfig.RaidLighting.cs (15) CloneZConfig: The raid town's lighting, switches and wall decor settings.
- CloneZConfig.RaidOpenings.cs (56) CloneZConfig: The raid town's doors, zombie interactions, and windows settings.
- CloneZConfig.RaidStore.cs (96) CloneZConfig: The raid store's office fixtures, food mart, quarantine tent, and outdoor items settings.
- CloneZConfig.cs (568) CloneZConfig: Central gameplay tuning.
- CloneZUtil.cs (100) CloneZUtil: Small helpers shared across the bootstrapper and gameplay.
- CombatFX.cs (73) CombatFX, FxFade: Throwaway visual feedback for gunfire: a fading tracer line and a brief muzzle flash.
- CombatResolver.cs (209) Hitbox, ShotRequest, ShotResult: A collider that belongs to one body zone of an actor (heads take more, armour covers some).
- DamageModel.cs (209) DamageKind, BodyZone, BodyZones: What kind of harm a blow does.
- Factions.cs (214) Factions, Def, CordonRules: The groups that live in (and around) the Zone, how they feel about each other, and how they feel abo
- GameEvents.cs (80) RaidOutcome, KillInfo, GameEvents: How a raid ended for the local survivor.
- GameInput.cs (124) GameAction, GameInput: Gameplay actions bound through GameInput (the original controls read the keyboard directly).
- GameManager.cs (131) GameState, GameManager: Owns global game state: score, waves, and win/lose flow.
- GameModules.cs (99) IGameModule, GameModules, WorldHooks: A gameplay feature that wires itself in at boot: registers item actions, subscribes to GameEvents an
- GameSession.cs (97) GameMode, RaidKind, CameraView: The kind of run being played, picked on the title screen or in the hub.
- IDemolishable.cs (28) IDemolishable: Anything the build tool's Demolish mode can tear down — player-built structures (barricades, house p
- NoiseSystem.cs (67) NoiseSystem, Pulse: Lightweight world sound system.
- PlayerModifiers.cs (103) StatMods, IStatModSource, PlayerModifiers: The survivor's combined stat modifiers for this frame — the sum of everything that bends the base ru
- PowerGrid.cs (90) PowerGrid: The electricity supply.
- SandboxSettings.cs (217) InfectionMode, DeathPenalty, SandboxSettings: The sandbox dials (Project Zomboid style) plus the server rules: nearly everything that changes how 
- SaveData.cs (230) SaveData, PlayerSave, StackSave: The complete serialisable snapshot of a run — world clock, weather, utilities, score, the survivor (
- SaveSystem.cs (521) SaveSystem: Reads and writes the game to disk and knows how to capture a live run into a SaveData and pour a Sav
- Shelter.cs (58) Shelter: The interiors of the current world — boxes of space under a roof.
- SqliteSaveStore.cs (827) SqliteSaveStore: The relational persistence layer: decomposes a SaveData snapshot into normalised SQLite tables on Wr
- Subtitles.cs (33) Subtitles, Line: The caption line for things the survivor hears: a radio or TV broadcast, a voice on the street.
- TimeOfDay.cs (219) TimeOfDay: Drives a continuous day/night cycle: advances a 24-hour clock, swings the directional "sun", and ble
- UIState.cs (21) UIState: Global UI/input flags.
- WaterMains.cs (62) WaterMains: The municipal water supply.
- WeatherSystem.cs (319) Weather, WeatherSystem: A lightweight weather simulation.
- WorldMarkers.cs (63) WorldMarkers, ZoneMarkRegistry: Named points the current world publishes for other systems: landmarks ("landmark:crashed_helicopter"
- WorldNavMesh.cs (65) WorldNavMesh: Holds the world's NavMeshSurface so gameplay can rebuild the baked NavMesh when a baked-in obstacle 

## Assets/Scripts/Core/Sqlite
- Sqlite.cs (286) SqliteNative, SqliteException, SqliteConnection: A tiny, self-contained binding to the native SQLite C API — just the handful of entry points the sav

## Assets/Scripts/Dev
- CutawayShots.Pictures.cs (386) CutawayShots, Harness: The cutaway harness's pictures: the spots the survivor stand-in is put on, and the walk in through a
- CutawayShots.Pieces.cs (415) CutawayShots, Harness, Piece: One streamed piece built the way KentuckyStreamer builds it.
- CutawayShots.Rig.cs (284) CutawayShots, Harness: How far along the ground the cutaway follows the sight line at zoom size: to the camera, as MapCutaw
- CutawayShots.cs (624) CutawayShots, Harness, Counts: The isometric cutaway's harness, behind CloneZ.EditorTools.CutawayShots.Run.
- InteriorShots.cs (1463) InteriorShots, Rec, Harness: The interiors' harness, behind CloneZ.EditorTools.InteriorShots.Run.
- MapShots.Perf.cs (1594) MapShots, PerfReport, DetailPair: A piece built, another one far off, then the first again: they must match.
- MapShots.cs (3191) MapShots, Options, Report: The map harness behind CloneZ.EditorTools.CloneZBatch.MapShots (see there for the command line).
- SmokeTest.cs (540) SmokeTest: An automated smoke test, inert unless the game (or a batch-mode editor in play mode) is started with

## Assets/Scripts/Economy
- CentralEconomy.cs (302) EconomyRule, EconomyConfig, CentralEconomy: One loot rule: how many of a thing the Zone holds (nominal), the floor restocks refill to (min), how
- Hideout.cs (395) ModuleLevel, ModuleDef, Recipe: One level of a hideout module: what it costs to build and what it gives.
- Insurance.cs (157) InsuranceDesk, CorpseRecovery: Insurance, Tarkov style: pay a share of an item's value and, if you die carrying it and nobody walks
- Money.cs (182) Money, StarterPack: The money: military-grade 5.45 rounds (ZoneContent.Currency).
- MoralLedger.cs (85) MoralLedger, HubScreens: The hidden morality track (Metro): sparing people who give up, listening to the bar's stories, helpi
- TraderCatalog.cs (146) TraderOffer, LoyaltyLevel, TraderDef: One thing a trader sells: for money, or as a barter for other items.
- Trading.cs (236) Trading: Buying, bartering and selling against the active character's stash, and the loyalty that gates what 

## Assets/Scripts/Extraction
- ExtractionBeacon.cs (392) ExtractionBeacon: The in-world beacon for one RaidExit: a tall glowing light column that reads across the dark town, a
- ExtractionManager.cs (497) ExtractionManager, RaidState: Runs a raid: the spawn-protection countdown, the exits (beacons in the world, extraction progress wh
- RaidLoadout.cs (446) RaidLoadout, RaidKit, RaidKitPiece: The kit a survivor takes into a raid and brings back out.
- RaidSummaryUI.cs (693) RaidSummaryUI, RaidSummary, LineOrder: The end-of-raid report: EXTRACTED / KILLED IN ACTION / MISSING IN ACTION, how long the raid ran, the
- RaidTypes.cs (80) ExitCondition, RaidExit, RaidMapInfo: What an exit demands before it lets you out (Tarkov's conditional extracts turn the map into a puzzl
- RaidWorldRules.cs (241) RaidWorldRules, PowerSwitch: The per-raid rolls on top of a zone's fixed layout: fresh loot in every container, which exits are o

## Assets/Scripts/Flow
- GameFlow.cs (63) GameFlow: The combined core loop's transitions: title → hub → (deploy) raid → summary → hub.

## Assets/Scripts/Gadgets
- Gadgets.cs (870) DroneKind, Drone, DronePickup: Bodycam's drones: a flying FPV drone (fast, loud, can carry a grenade to drop) and a tracked ground 

## Assets/Scripts/Hub
- Fireplace.cs (130) Fireplace: Simple fireplace that can be lit or extinguished; provides a warm point‑light when lit.
- Heater.cs (140) Heater: Toggleable heater that emits a warm point‑light when on.
- HideoutInitHelper.cs (19) HideoutInitHelper
- HubCatalog.cs (33) HubDef, HubCatalog: One of the places runners live between raids.
- HubNpc.cs (277) HubNpc, Role, HubNpcTalk: Someone in the hub: a station keeper, a patron nursing a drink, a guitarist on the stage, an old-tim
- HubWorld.cs (539) HubRules, HubStation, HubStashLocker: Rules that hold in the hub: a safe place.

## Assets/Scripts/Hub/Screens
- HomeScreens.cs (485) Upkeep, UpkeepScreen, HideoutScreen: The pressure behind every run (Roadside Picnic's desperation): rent on the room above the bar, a deb
- HubKit.cs (289) HubItemSource, HubItemRef, HubKit: Where an item the hub's screens work on lives.
- HubScreenBase.cs (187) HubScreenBase: Base for the hub's screens: a large window with the money on hand in the title bar, a hover tooltip,
- HubTooltip.cs (202) HubTooltip, HubTooltipTrigger: A hover tooltip panel for a hub screen: one per screen canvas, drawn above everything, following the
- HubUI.cs (551) HubUI: Layout and row builders shared by the hub's screens (on top of UIKit): anchored areas and columns, c
- ServiceScreens.cs (440) StashScreen, MedicScreen, InsuranceScreen: The stash: move things between the stash shelves and what you carry.
- TradeScreens.cs (246) TraderScreen, FleaScreen: A trader's counter: what they sell at your loyalty level (money or barter), what they'll buy from yo

## Assets/Scripts/Items
- ContentDatabase.cs (547) ContentDatabase: The relational store for the game's static content — the item catalogue, the per-container loot tabl
- Inventory.cs (275) Inventory: A weight-limited bag of ItemStacks, used both for the player's carried items and for world container
- InventoryLayout.cs (483) GridSection, GridSortMode, InventoryLayout: One rectangular grid of cells inside an Inventory: the survivor's pockets, a worn backpack / chest r
- ItemActions.cs (84) ItemMenuAction, ItemActions: One extra verb on an item's menu ("Attach to M9", "Strip mods", "Throw").
- ItemDatabase.cs (535) ItemCategory, ClothingSlot, GearKind: Where a piece of clothing is worn.
- ItemExtra.cs (184) ItemExtra, ItemStackExtras, ItemConditionTiers: Per-instance data an item can carry beyond its count, freshness and wear: whether it was FoundInRaid
- LootContainer.cs (437) ContainerType, LootContainer: A searchable world container.

## Assets/Scripts/Medical
- MedicalItems.cs (147) MedicalItems, Body: What the field medicine does.
- PlayerBlood.cs (207) PlayerBlood: DayZ's separate blood and shock pools.
- PlayerLimbs.cs (654) PlayerLimbs, HudLimbs: The survivor's seven hit zones (Tarkov): head, thorax, stomach, both arms, both legs, each with its 

## Assets/Scripts/Net
- NetAuthority.cs (39) NetAuthority: Who decides what happens.
- NetPlayers.cs (629) NetPlayers, NetTicker, NetTracer: The other runners.
- NetSession.cs (570) NetOut, NetIn, NetClientInfo: A message being written (a thin wrapper over the netcode's buffer writer).
- NetSocial.cs (1284) NetCodec, NetSocial, VoiceCapture: Item lists on the wire (every field a stack carries: wear, age, repairs, extras).
- NetWorld.cs (522) NetWorld, NetAgentProxy: The shared world.

## Assets/Scripts/Player
- PlayerBuilder.cs (884) PlayerBuilder, BuildSelection: The survivor's construction tool.
- PlayerClothing.cs (181) PlayerClothing, WornPiece: The clothing the survivor is wearing — one garment per body slot.
- PlayerCombat.cs (643) PlayerCombat, FlareProjectile, NetClock: Melee and firearms.
- PlayerController.cs (387) PlayerController: Survival movement: move relative to the camera, aim Project Zomboid style, sprint/sneak with stamina
- PlayerFarming.cs (386) PlayerFarming, FarmAction: Lets the survivor work the land.
- PlayerFoodSickness.cs (134) PlayerFoodSickness: Food poisoning — Project Zomboid's Sickness / Nausea moodle.
- PlayerGear.cs (561) PlayerGear: The survivor's powered and protective gear: night-vision goggles worn in the Eyes slot (toggled with
- PlayerGearParts.cs (295) PlayerGearParts, PlayerGearTorso: Building blocks for PlayerGearVisuals: collider-free primitive parts, flat straps (single boxes, or 
- PlayerGearVisuals.cs (769) PlayerGearVisuals, Piece: Draws the survivor's gear on the body: a backpack on the back (sized by how much it holds, in the ba
- PlayerHydration.cs (137) PlayerHydration: Lets the survivor get water.
- PlayerInteractor.cs (898) PlayerInteractor, FocusKind: Everything the survivor reaches out and uses.
- PlayerInventory.cs (900) PlayerInventory: The player's carried items plus the currently-equipped melee weapon.
- PlayerPanic.cs (213) PlayerPanic: The survivor's fear response — Project Zomboid's Panic moodle.
- PlayerSeat.cs (337) PlayerSeat: Sitting down — on a dining chair, the armchair, the edge of the bed, the toilet lid — and lying down
- PlayerSkills.cs (221) PlayerSkills, Skill: The player's Project Zomboid–style skills (perks).
- PlayerSleep.cs (197) PlayerSleep: Lets the player sleep at a bed to pass the night, shedding fatigue and slowly healing (Project Zombo
- PlayerStats.cs (401) Exertion, PlayerStats: Survival stats for the player: health, endurance, hunger, thirst and the zombie infection.
- PlayerTemperature.cs (227) PlayerTemperature: The survivor's thermoregulation — Project Zomboid's body-temperature and wetness model.
- PlayerWounds.cs (570) WoundType, Wound, PlayerWounds: Kinds of injury.
- Stealth.cs (175) Stealth, HudStealth: How easily the survivor is seen and heard right now (Metro's wrist light, Gray Zone Warfare's visibi

## Assets/Scripts/Profile
- ProfessionCatalog.cs (184) ProfessionDef, TraitDef, ProfessionCatalog: A starting profession: skill head-starts, a little kit, and how many trait points it leaves.
- ProfileData.cs (342) AccountProfile, CharacterProfile, HealthCarry: The account: its characters (up to three), the season, and what was learned that no wipe takes away.
- ProfileStore.cs (374) ProfileStore: Loads, saves and edits the account profile (clonez_profile.json under the persistent data path): the
- Progression.cs (50) Progression: The character's level: experience from kills, extractions, found loot, quests and exploration.

## Assets/Scripts/Quests
- QuestBook.cs (486) ObjectiveKind, QuestObjective, QuestDef: Kill: "faction:wardens", "kind:Zombie", "creature:hound", "human".

## Assets/Scripts/Rendering
- ActorFog.cs (96) ActorFog: Fog of war for any actor that isn't a zombie (people, mutants, animals, other players): its body is 
- BreathFX.cs (174) BreathFX: Visible breath in the cold (reference photo 3): while Climate.BreathVisible, a small puff of vapour 
- FlashlightFX.cs (209) FlashlightFX: The survivor's flashlight beam: a warm white, shadow-casting spot light from the torch clipped to th
- IsoVisionShade.cs (140) IsoVisionShade, IsoVisionShadeModule: The isometric fog of war by day (Zomboid): from above you could otherwise see everything nearby, so 
- NightVisionFX.cs (303) NightVisionFX: The night-vision goggles' look (reference photo 2): a runtime global URP volume that turns the pictu
- Presentation.cs (492) PostFx, GasVisorOverlay, IsoAimLine: The look of the game on top of the cameras: a URP post-processing volume carrying the accessibility 
- RoomLight.cs (402) RoomLight: A room's light fitting — a ceiling bulb or a desk lamp: a warm point light with a small glowing bulb
- SnowFX.cs (241) SnowFX: Falling snow: soft white specks drifting down through the dark on a slight wind (reference photo 1),
- VisionAssets.cs (399) VisionAssets: Procedural textures, meshes and materials shared by the vision and lighting effects (the line-of-sig
- VisionController.cs (547) VisionController: The heart of the VisionRig: decides how dark the night is and lights only what the survivor can see,
- VisionRig.cs (88) VisionRig: Installs everything that decides what the survivor can see and how the world is lit: the line-of-sig
- ZombieVisibility.cs (243) ZombieVisibility, Sight: Added to every zombie by the spawner; hides the zombie's body while the survivor can't see it, Proje

## Assets/Scripts/Survival
- Diseases.cs (184) PlayerDiseases, DiseaseModule: DayZ's illnesses, each with its own cause and its own cure — survival needs that interact rather tha
- FieldSurvival.cs (608) PlayerChannel, FieldSurvival, SnareTrap: A timed action the survivor performs in the field (foraging, fishing, reading, cranking a charger): 
- Navigation.cs (341) ZoneMapScreen, Mark, JournalScreen: The Zone map (P), on a clipboard: streets and buildings, the landmarks, the exits — and no dot sayin

## Assets/Scripts/UI
- GridDragHandle.cs (62) GridDragHandle: Moves a floating window when its title bar is dragged, or resizes it from its corner grip.
- GridPointerRelay.cs (48) GridElement, GridPointerRelay: What kind of grid-inventory element a GridPointerRelay sits on.
- GridScrollRect.cs (20) GridScrollRect: A ScrollRect that scrolls with the mouse wheel and its scrollbar but not by dragging its content: in
- GridSprites.cs (118) GridSprites: Procedural sprites for the grid inventory, built once and cached: the tiling cell, a small gear, a h
- HUDController.cs (665) HUDController: The code-built heads-up display, with its own layout for each camera view (HudViewLayout): Isometric
- HealthPanelController.cs (598) HealthPanelController: A code-built health screen (toggled with H) inspired by Project Zomboid's health panel.
- HudBodycamView.cs (222) HudBodycamView: The bodycam view's HUD: the on-screen display of a 1993 camcorder rather than a game HUD.
- HudCombatFeedback.cs (224) HudCombatFeedback: Combat feedback for the views that look through the survivor's eyes or over their shoulder: a red ar
- HudFirstPersonView.cs (219) HudFirstPersonView: The first-person view's HUD: as little as possible between the eyes and the world.
- HudHands.cs (220) HudHands: Top-left hand slots, Project Zomboid HUD-mod style: a circle for the weapon in hand (line-art silhou
- HudHints.cs (117) HudHints: The key-hint chips along the bottom-centre (two compact rows of key caps + actions).
- HudIconArt.cs (408) HudIcon, HudIconSpec, HudIconArt: Every procedural sprite the HUD draws (see HudIconArt for the art).
- HudIcons.cs (53) HudIcons: Bakes the HUD's procedural art (HudIconArt) into sprites on first use and caches them for the rest o
- HudInteraction.cs (178) HudInteraction, Row: The interaction overlay: a small label hung over whatever the survivor has in focus (a fixture's nam
- HudMoodles.cs (314) MoodleDock, HudMoodles, Kind: Where the moodles sit: down the right edge with labels (isometric, over the shoulder), or as a compa
- HudRaidOverlay.cs (134) HudRaidOverlay: Raid feedback from ExtractionManager: the floating green "Spawn Protection: 40s" over the survivor's
- HudRaster.cs (267) HudRaster: A tiny anti-aliased vector rasteriser for the HUD's procedural line art.
- HudSevenSegment.cs (60) HudSevenSegment: One digit of the digital watch: seven hexagonal segments built from sliced images, with the unlit se
- HudShoulderView.cs (312) HudShoulderView: The over-the-shoulder view's HUD, laid out like a modern third-person action game.
- HudSprites.cs (119) HudSprites: Procedural sprites for the per-view HUDs and the pause menu that aren't line art (see HudIcons for t
- HudStamina.cs (77) HudStamina: Bottom-left: a running-man pictogram that mirrors how the survivor is moving (standing, walking, spr
- HudStatusRow.cs (433) HudStatusRow, StatusIcon, InfoRow: Bottom-right: the mode box (amber "EXTRACTION MODE", or the wave and kill count in wave survival) wi
- HudSubtitles.cs (69) HudSubtitles: Captions for what the survivor hears — a radio or TV broadcast (Subtitles): the latest two lines at 
- HudToasts.cs (64) HudToasts: Short fading messages at the bottom-centre, above the interaction prompts — e.g.
- HudUI.cs (224) HudContext, HudUI: Everything the HUD widgets read, gathered once by HUDController.Bind.
- HudViewKit.cs (340) HudNeed, HudReadouts, HudGauge: The supplies the view HUDs show as small filling icons.
- HudViewLayout.cs (159) HudTier, HudGroup, HudViewLayout: How much a HUD cluster matters, which decides which HUD styles keep it on screen.
- HudWatchRow.cs (235) HudWatchRow: The top-right row, right-aligned: the gas-mask "Filter 72%" panel (only with a mask on), a dim activ
- InventoryUIController.Grid.cs (1384) InventoryUIController, GridSide, GridWindow: Whose cells a section view draws.
- InventoryUIController.GridActions.cs (250) InventoryUIController: Switches between the grid windows and the verbose list (remembered across runs).
- InventoryUIController.GridDrag.cs (997) InventoryUIController, DragState, DropKind: The item being dragged, where it came from and how it's held.
- InventoryUIController.List.cs (838) InventoryUIController, SortKey, EntryKind: One listed item: a stack in the bag or a container, a worn garment, or a floor pile.
- InventoryUIController.Picker.cs (301) InventoryUIController: The CONTAINERS column down the right of the inventory: Project Zomboid's strip of container
- InventoryUIController.cs (998) InventoryUIController, MenuOption: The code-built inventory / loot UI, in two interchangeable views (toggled from either; the choice pe
- MainMenuController.cs (256) MainMenuController: The title screen: your runners (up to three per account), making a new one, hosting or joining a ser
- PauseMenuController.cs (908) PauseMenuController, ItemKind, Item: The in-game pause menu (Esc).
- SkillsPanelController.cs (310) SkillsPanelController, SkillCache: A code-built skills screen (toggled with K) listing every perk with its current level and progress t
- TooltipTrigger.cs (90) TooltipTrigger: Attached to each inventory row; reports pointer enter/exit to the InventoryUIController so it can sh
- UIFader.cs (69) UIFader: Fades a panel's CanvasGroup in and out so it opens and closes smoothly instead of popping.
- UIKit.cs (545) UIKit, UIScreen, HudLayer: The shared builder for code-made uGUI: canvases, panels, labels, buttons, scroll lists, toggles, sli
- UITheme.cs (43) UITheme: Shared styling helpers for the code-built UI.

## Assets/Scripts/Weapons
- Ballistics.cs (228) ModSlot, AmmoStats, GunStats: Where an attachment goes on a gun.
- GearEffects.cs (31) GearEffects: What worn armour and Zone suits do to the body besides stopping rounds: plates and exo-frames weigh 
- WeaponItems.cs (253) WeaponItems: The inventory verbs for guns and their parts: attach an attachment (to the gun in hand, else the fir
- WeaponUpgrades.cs (127) WeaponUpgrades, Node: The technician's branching upgrade trees (STALKER-style).

## Assets/Scripts/World
- Barricade.cs (313) Barricade: A player-built wooden barricade (Project Zomboid's planked windows/doorways).
- BuildPiece.cs (506) BuildPieceType, BuildPiece: The kind of structural piece a survivor can raise when building a house.
- Camps.cs (801) ZoneCamps, CampGate, StructureBlast: DayZ's base building and raiding, fitted to raids into a Zone.
- CeilingVisibility.cs (70) CeilingVisibility: Shows interior ceilings (and other overhead pieces such as the tent roof and the gas-station canopy)
- CookingStation.cs (80) CookingStation: A stove the survivor can cook raw ingredients at.
- Cover.cs (53) Cover: A scattered arena cover block — a solid box that breaks line of sight and carves the baked NavMesh.
- DecorExtensions.cs (44) DecorExtensions: Draws an outdoor water tap at pos.
- DecorFont.cs (112) DecorFont: A tiny 5×7 bitmap font for painting text into procedural textures (the "U.S.
- DecorMaterials.cs (122) DecorMaterials: Cached URP materials for the raid map's textured surfaces (snow, asphalt, floors, walls, ceilings, c
- DecorPalette.cs (136) DecorPalette: One small point-filtered texture holding every flat colour the raid map uses, one texel per colour.
- DecorProps.cs (614) DecorProps, CarKind: Outdoor props and loose debris for the raid map: wrecked cars, bare trees and pines, dumpsters, sand
- DecorTextures.cs (1017) WainscotStyle, PaperStyle, ExteriorStyle: The interior wall finishes the raid map paints rooms with: a dark lower wainscot under a pale paper,
- FallbackItemModel.cs (98) FallbackItemModel: A plain stand-in model for items that have no bespoke one yet — a single tinted primitive picked by 
- FarmPlot.cs (388) FarmPlot, PlotState, FarmCrop: A tilled patch of soil the survivor can sow, water and harvest — Project Zomboid's farming plot.
- FurnitureBuilder.Bathroom.cs (761) FurnitureBuilder, BathFacing, BathRing: Glazed white china: the toilet, the basin, the shower tray.
- FurnitureBuilder.Fixtures.cs (378) FurnitureBuilder: A wooden panel door leaf hinged at the local origin and extending along +X (width × height, 4.5 cm t
- FurnitureBuilder.Home.cs (422) FurnitureBuilder: A run of wooden base cabinets with a laminate top (0.62 deep, 0.92 high), optionally with a stainles
- FurnitureBuilder.Kitchen.cs (860) FurnitureBuilder, KitchenTone: The colours of one run of kitchen units, picked once so the carcass and the doors drawn apart from i
- FurnitureBuilder.Lighting.cs (325) FurnitureBuilder: A cream switch plate width wide centred on at (on the wall face).
- FurnitureBuilder.Living.cs (382) FurnitureBuilder: Height of the middle of a DresserBody drawer (row 0 the top one).
- FurnitureBuilder.Office.cs (403) FurnitureBuilder: A beige 1990s CRT monitor (deep tapered housing, dark glass) on a swivel foot, standing at at.
- FurnitureBuilder.RaidCivic.cs (1019) FurnitureBuilder, CrateLoad: The middle of filing drawer i (0 = the bottom one).
- FurnitureBuilder.RaidHome.cs (625) FurnitureBuilder: How many two-door bays a run of wall cupboards len long is split into (each about the starter home's
- FurnitureBuilder.RaidOpenings.cs (459) OpeningLine, FurnitureBuilder: A straight wall line in world axes, as the raid town's door and window builders draw on it: a point 
- FurnitureBuilder.RaidStore.cs (569) FurnitureBuilder: The copier's lid hinge (its back edge, under the lid).
- FurnitureBuilder.Utility.cs (483) FurnitureBuilder: A white steel medicine cabinet (0.9 × 0.42 × 1.8): glazed upper doors over shelves of boxes and bott
- FurnitureBuilder.cs (374) FurnitureBuilder: The raid map's furniture, built from boxes, prisms and quads into a RaidMesh.
- GeneratedItemModels.cs (128) GeneratedItemModels, FilePart, FileModel: Ground models produced by the offline LLM asset pipeline (Tools/AssetPipeline), loaded from Resource
- GroundItemRenderer.cs (202) GroundItemRenderer: Draws every item lying on the ground near the player.
- GroundItemStore.cs (368) GroundItemStore, Item, Chunk: Every item lying on the ground in the world, held as data rather than as scene objects.
- HomeFurniture.Bathroom.cs (101) HomeFurniture: The bathroom's loose furniture (see the per-area hooks in HomeFurniture.cs): the toilet, the pedesta
- HomeFurniture.Kitchen.cs (89) HomeFurniture: The kitchen's loose furniture (see the per-area hooks in HomeFurniture.cs): the counter along the we
- HomeFurniture.Living.cs (271) HomeFurniture: The living room's, bedroom's and store room's furniture (see HomeFurniture): the dining table (a sur
- HomeFurniture.cs (524) HomeFurnitureKind, HomeFurniture: The loose furniture that dresses the starter home.
- HomeLamp.cs (241) HomeLamp: A lamp of the wave-survival starter home — the bedside lamp, the standard lamp, the kitchen-diner's 
- ItemMeshBaker.cs (257) ItemMeshBaker, Lod, Baked: Turns an ItemModelDesc into a single combined mesh that can be drawn instanced.
- ItemModelDesc.cs (230) PartShape, PartMat, Part: Which primitive mesh a part is cut from.
- ItemModels.cs (12057) ItemModels: Builds bespoke, hand-modelled ground visuals for specific items, assembled out of primitives (the pr
- PrimitiveMesh.cs (188) PrimitiveMesh: The unit primitives item models are assembled from, generated rather than taken from GameObject.Crea
- RaidBuilding.Civic.cs (800) RaidBuilding: The raid town's shared workplace furniture, made usable: filing cabinets whose drawers each slide ou
- RaidBuilding.Doors.cs (212) RaidBuilding, DoorPolicy: The raid town's doors: every doorway declared with a leaf gets the starter home's kind of door — a p
- RaidBuilding.Fixtures.cs (217) RaidBuilding: The raid town's interactive layer, shared by every building: the starter home's fittings — Interacta
- RaidBuilding.Furnish.cs (525) RaidBuilding, Place: A placement in the building's local frame: centre (x, z) and yaw (0 faces +Z).
- RaidBuilding.Home.cs (477) RaidBuilding: The raid town's houses (RaidLayoutsHomes.cs) fitted out the way the starter home is: the home's own 
- RaidBuilding.Lighting.cs (683) RaidBuilding, WallFitting, SwitchSpot: The raid town's lights, switches and wall decor, working as the starter home's do.
- RaidBuilding.Openings.cs (81) RaidBuilding: A roll-up door in the (leafless) doorway at local (x, z): the drum over the inside of the doorway wi
- RaidBuilding.Windows.cs (294) RaidBuilding: The raid town's windows: the starter home's double-hung window — a fixed upper sash over a lower one
- RaidBuilding.cs (962) FloorKind, LightStyle, RaidRoom: Floor finishes for raid-map rooms.
- RaidLayouts.cs (415) RaidLayouts: The raid map's authored buildings.
- RaidLayoutsCivic.cs (244) RaidLayouts: The town clinic: waiting room with reception, an exam room, the doctor's office, a three-bed ward an
- RaidLayoutsHomes.cs (254) RaidLayouts: A family house: the 12 × 10 ranch (living room, kitchen, bedroom, bathroom with a bath, den) or the 
- RaidLayoutsOffice.cs (267) RaidLayouts: The county office: an open-plan floor of desks with CRTs and lamps (photo 3), a lobby with reception
- RaidLayoutsStore.cs (273) RaidLayouts: The gas station's food mart: the checkout counter with its till, gondolas of shelves to browse from 
- RaidMapBuilder.Edge.cs (386) RaidMapBuilder: The map edge and what lies past it (the exit roads it opens onto are defined in RaidMapBuilder.Town.
- RaidMapBuilder.Outdoor.cs (421) RaidMapBuilder: The outdoor fill of the streets and verges (the same props made usable and their interactive pieces 
- RaidMapBuilder.Props.cs (230) RaidMapBuilder: The object every interactive outdoor prop hangs under (made on first use).
- RaidMapBuilder.Streets.cs (406) RaidMapBuilder, RoadSpec: An axis-aligned street: asphalt, a faded double centre line, kerbs, sidewalks, banks of plowed snow 
- RaidMapBuilder.Town.cs (468) RaidMapBuilder: Draws a prop into the outdoor mesh at a world placement, with an optional collider (size.y = 0 for n
- RaidMapBuilder.cs (94) RaidMapBuilder: Builds the extraction raid map under world: a snowbound, run-down 1993 Kentucky town at night (140 ×
- RaidMesh.cs (1203) Surf, RaidMesh, Pool: How a piece of raid-map geometry is skinned: a material plus the way its UVs are generated.
- RaidTown.cs (352) RaidTown, ColliderBox, OutdoorBatches: Shared state while one raid map is being built: the seeded random stream, the three roots (the baked
- SleepSpot.cs (98) SleepSpot: A bed the player can sleep in to pass the night and shed fatigue.
- StarterHome.Bathroom.cs (66) StarterHome: The bathroom's wall fittings: the mirror cabinet over the basin on the back wall and the towel rail 
- StarterHome.Doors.cs (131) StarterHome: The home's doors in their finished doorways: a pair of panelled leaves in the wide gaps (the front d
- StarterHome.Grounds.cs (442) StarterHome: The street past the front of the home: sun-faded asphalt with a dashed yellow centre line and white 
- StarterHome.Kitchen.cs (52) StarterHome: The kitchen's wall cupboards over the worktop on the west wall: the carcass, shelf and crockery draw
- StarterHome.Lighting.cs (428) StarterHome: The starter home's lighting and wall decor.
- StarterHome.Shell.cs (541) StarterHome, Frame, Band: Builds the finished look onto a house piece standing in one of the home's slots — its visible geomet
- StarterHome.Windows.cs (407) StarterHome: The starter home builds window fittings (sashes, curtains, blinds, etc.).
- StarterHome.cs (540) StarterHome, HomeRoom, Slot: The wave-survival starter home, finished to the extraction town's standard.
- ToxicZone.cs (316) ToxicZone: A cloud of toxic gas: breathing it without a gas mask (or with a spent filter) sickens and hurts the
- Vehicles.cs (880) Vehicle, VehicleDriver, HudVehicle: A car (DayZ's vehicle assembly, Zomboid's hotwiring): they're found broken at the Zone's parking spo
- WaterSource.cs (525) WaterSourceKind, WaterSource: Where the survivor can get water — a plumbed tap or a rain barrel.
- WorldItem.cs (132) WorldItem: A handle to one item lying on the ground.

## Assets/Scripts/World/Fixtures
- BasinFixture.cs (416) BasinFixture: The bathroom's pedestal basin: a single-lever mixer tap and a plug on a chain.
- BathroomFixtures.cs (79) BathroomFixtures: Small helpers shared by the bathroom's fixtures (ToiletFixture, BasinFixture, ShowerFixture, TowelRa
- BedFixture.cs (274) BedFixture: The bed (on its SleepSpot).
- BreakerPanel.cs (176) BreakerPanel: The house's breaker panel, in the store room: a grey steel box whose tin door opens on the dead-fron
- CivicRadioNote.cs (34) CivicRadioNote: A crumpled note placed on a Guard‑band radio desk.
- ClockFixture.cs (188) ClockFixture: The living room's battery wall clock.
- DoormatFixture.cs (168) DoormatFixture: The coir welcome mat on the front stoop: wipe your feet on it, or lift its near edge to see what's u
- FixtureColour.cs (42) FixtureColour: Tints one renderer through a MaterialPropertyBlock, so fixtures that change colour (a towel drying, 
- HomeDoor.cs (382) HomeDoor, Setup: A door of the starter home, hung in a finished doorway BuildPiece by StarterHome.Doors.cs: a pair of
- HomeWindow.cs (504) HomeWindow, Setup, Lookout: A window of the starter home, glazed and hung by StarterHome.Windows.cs: a fixed upper sash over a l
- HoseSpigot.cs (247) HoseSpigot: The outside tap: a standpipe with a red wheel beside the hose reel, round the west side of the house
- KitchenFridge.cs (212) KitchenFridge: The workings of the kitchen's fridge-freezer (HomeFurniture of kind Fridge): a small freezer over a 
- KitchenParts.cs (157) KitchenFrame, KitchenParts: A kitchen fitting's local frame, as the furniture builders draw it (front toward +Z, origin on the f
- KitchenSink.cs (562) KitchenSink: The kitchen sink under the front window — the fixture fronting the kitchen tap's WaterSource.
- KitchenSinkWarmth.cs (29) KitchenSinkWarmth: Provides a tiny warmth source when the sink’s tap is running.
- KitchenStove.cs (352) KitchenStove: The gas range's controls — the fixture on the kitchen's CookingStation.
- LampSwitch.cs (132) LampSwitch: The switch of a free-standing lamp — the bedside lamp, the standard lamp, a desk lamp: [E] turns it 
- LightCircuits.cs (97) LightCircuits, Wiring: The starter home's lighting circuits — the living room's, the porch's, the store room's and the bath
- LightSwitch.cs (175) LightSwitch: A wall light switch by a door: a cream plate of one or more toggles (in a tent, a switch box's lever
- LightingFixtures.cs (34) LightingFixtures: Small shared helpers for the home's lights, switches, wall decor and yard fixtures.
- LivingFixtures.cs (46) LivingFixtures: Small shared helpers for the living room's and bedroom's fixtures.
- LivingReading.cs (152) LivingReading, Book: The bookcase's pastimes, offered after browsing its shelves: reading a few chapters of one of the bo
- LivingReceiver.cs (273) LivingReceiver: A set that plays the broadcasts (Broadcasts) — the portable TV and the kitchen radio.
- MailboxFixture.cs (352) MailboxFixture: The rural mailbox at the kerb.
- MirrorCabinet.cs (185) MirrorCabinet, Sign: The mirror cabinet over the bathroom basin: a white wall cabinet with a mirrored door, kept as a Sto
- OutdoorHouseNumber.cs (56) OutdoorHouseNumber: The enamelled house-number plate beside the front door: "Read the house number".
- OutdoorLantern.cs (102) OutdoorLantern: The black carriage lantern beside the front door.
- PictureFixture.cs (183) PictureFixture: A framed print on the wall.
- RadioFixture.cs (116) RadioFixture: The kitchen radio on the worktop (see LivingReceiver): battery powered, so it plays on long after th
- RaidCivicFixtures.cs (405) CivicToggle, CivicLook, CivicRest: A workplace fitting with two states, flipped with [E]: a bench vice wound open or shut, a ward's pri
- RaidCivicFridge.cs (264) CivicFridge, CivicReading, Shelf: The workings of a workplace fridge-freezer — the National Guard post's kitchenette fridge, the clini
- RaidCivicRadio.cs (370) CivicRadio, Channel, Band: A two-way set the survivor can listen in on — the National Guard post's olive field radio, the depot
- RaidCivicSink.cs (349) CivicSink: The deep steel sink let into a workplace counter — the National Guard post's kitchenette, the clinic
- RaidDoor.cs (447) RaidDoor, Raid: A door of the raid town: the starter home's door (HomeDoor) — a pair of leaves drawn shut across the
- RaidHomeBath.cs (421) RaidHomeBath: A raid ranch's bath: a built-in tub with a hot and a cold tap over the drain and a plug on a chain.
- RaidHomeLiving.cs (248) RaidHomeLiving: The small pastimes and the paperwork of the raid town's houses: plumping the sofa's cushions, flicki
- RaidHomeWasher.cs (248) RaidHomeWasher: The controls of a raid cottage's washing machine (its porthole door and the drum behind it are a Sto
- RaidStoreCopier.cs (251) RaidStoreCopier: The county office's photocopier.
- RaidStoreCot.cs (135) RaidStoreCot: The quarantine tent's folding camp cot: "Lie down and rest" stretches out on the canvas, awake (an o
- RaidStoreSafe.cs (125) RaidStoreSafe: The food mart's floor safe in the stock room, locked.
- RaidStoreSwivelSeat.cs (219) RaidStoreSwivelSeat: A swivel office chair in the raid town's county office.
- RaidStoreVending.cs (226) RaidStoreVending: The break room's snack and soda machine.
- RaidStoreWaterCooler.cs (108) RaidStoreWaterCooler: The county office's bottled-water cooler: a water point (IWaterPoint) the survivor can drink from or
- RaidStoreWaterHeater.cs (162) RaidStoreWaterHeater: The county office's gas water heater in the utility room, as a water point (IWaterPoint): its cold i
- RaidTownFieldRadio.cs (312) RaidTownFieldRadio, Net: A National Guard field radio left on the quarantine tent's field desk (and one more on the ledge of 
- RaidTownGasPump.cs (193) RaidTownGasPump: A gas pump on the food mart's forecourt, worked from whichever side the survivor stands.
- RaidTownGenerator.cs (196) RaidTownGenerator: The portable generator at the quarantine site, its tank part full.
- RaidTownHydrant.cs (73) RaidTownHydrant: A fire hydrant on a street corner, frozen up for the winter: nothing to be had from it without a hyd
- RaidTownMailDrop.cs (140) RaidTownMailDrop: A blue public mail drop box on a Main Street sidewalk.
- RaidWindow.cs (409) RaidWindow, Glazing, Dressing: A window of the raid town: the starter home's double-hung window (HomeWindow) — a lower sash that sl
- RainBarrelFixture.cs (186) RainBarrelFixture: The rain barrel by the west wall, as a fixture: fill a bottle from it or drink (the water's tainted 
- RollUpShutter.cs (229) RollUpShutter, Setup: A depot's roll-up door: a steel curtain of slatted bands that rolls up into the drum over the inside
- SeatFixture.cs (178) SeatFixture: Somewhere to sit: a dining chair or the armchair.
- ShowerFixture.cs (345) ShowerFixture: The bathroom's shower stall: a glass door that swings out and a mixer that sets the rose spraying — 
- ToggleLamp.cs (122) ToggleLamp: Wooden lamp that can be switched on/off; emits a point light when on.
- ToiletFixture.cs (429) ToiletFixture, Step: The bathroom's toilet.
- TowelRail.cs (226) TowelRail: The chrome towel rail on the bathroom wall over the toilet, a towel folded over it.
- TvFixture.cs (238) TvFixture: The portable TV on the bedroom dresser (see LivingReceiver): it runs off the mains, so it needs the 

## Assets/Scripts/World/Interaction
- FixturePart.cs (136) FixturePart, Motion: A moving piece of a fixture — a cupboard door, a drawer, an oven door, a window sash, a toilet seat 
- Interactable.cs (298) InteractAction, InteractContext, Interactable: One thing the survivor can do at a fixture — "Turn on the tap", "Close the curtains", "Sit down".
- StorageFixture.cs (281) StorageOptions, StorageFixture, FixtureContents: How a StorageFixture is filled and worded.
- WaterPoints.cs (164) IWaterPoint, WaterPoints, WaterUse: Anything the survivor can drink from or fill a bottle at: a WaterSource (the kitchen tap, the rain b

## Assets/Scripts/World/Map
- BarrierPass.cs (948) BarrierPass, Style, Drives: The mapped barriers (format 2), as Kentucky builds them: plank horse fences, white or creosote-black
- BuildingPass.Context.cs (477) BuildingPass, Context: Towns near the site: site-local x, z and population.
- BuildingPass.Facades.cs (98) BuildingPass, FacadeStyle: From this many storeys up a building's upper floors get painted windows.
- BuildingPass.FlatRoofs.cs (526) BuildingPass, Kit: Flat roofs: most of a downtown's and every big building's, and what has to make one read as a roof f
- BuildingPass.Frontage.cs (632) BuildingPass: The longest text a sign spells out.
- BuildingPass.Materials.cs (987) BuildingPass, WallKind, RoofCover: White and cream trim, dark greens, black, brown and oxblood: window frames, fascias, corner boards.
- BuildingPass.Openings.cs (579) BuildingPass: Doors and windows drawn a stage at a time: a big building's hundreds of windows across a streamed pi
- BuildingPass.Plan.cs (1170) BuildingPass, Arch, HouseStyle: How one building is built.
- BuildingPass.Roofs.cs (787) BuildingPass, RoofPart, Axes: One block of a pitched roof.
- BuildingPass.Shape.cs (301) BuildingPass, Shape: A footprint's frame and blocks.
- BuildingPass.Shots.cs (667) BuildingShots, Harness, BuildingPass: The exteriors' own picture harness: builds the fixed square round each place asked for (as the map h
- BuildingPass.Special.cs (674) BuildingPass: True when this pass built a gas station canopy with pumps within within metres of p (site-local) on 
- BuildingPass.cs (726) BuildingPass, Job, Seg: The real-world map's buildings, from their mapped footprints.
- CutawayPass.cs (139) CutawayPass: Emits the per-building shells (BuildingShell) of a Full site as objects the isometric cutaway can hi
- GroundPass.Fields.cs (199) GroundPass, Crop: What a field grew last summer (from its id): corn half the time, then soybeans or tobacco, then hay.
- GroundPass.Grounds.cs (817) GroundPass: What stands on the snow in the land use with a purpose (Full detail, inside the square, kept clear o
- GroundPass.Parking.cs (340) GroundPass: Parking lots after the plough: the asphalt (the fill), the snow shoved off the lot in a ragged white
- GroundPass.Partition.cs (512) GroundPass, Owner, Trap: A land-use area cut to the square (or to a band of the scenery ring), and the part of it that shows.
- GroundPass.Textures.cs (881) GroundPass, Painting, Lattice: Starts painting every ground texture on worker threads (once a session).
- GroundPass.Tone.cs (171) GroundPass: The snow's tone: one see-through sheet over the ground (a grid of 2.56 m cells on a chunk, 5.12 m on
- GroundPass.Water.cs (569) GroundPass, WaterLook: What a body of water looks like in January.
- GroundPass.cs (506) GroundPass, ParkingStall, Ground: The real-world map's ground, which sets the palette of the whole map: a January snow cover over ever
- InteriorPass.Budget.cs (157) InteriorPass: Lamps a home may have in all; and the ones its ceilings leave for desk, bedside and standard lamps.
- InteriorPass.Builder.cs (1228) InteriorBuild, MergeKey, MergeJob: One real building's interior raised from its plan (InteriorPlan), a step at a time: the partitions (
- InteriorPass.Civic.cs (375) InteriorPass: The rooms of public buildings: the clinic's examination rooms and wards, the jail's cells and the ar
- InteriorPass.Food.cs (215) InteriorPass: Diners, cafés and bars: booths along the windows, tables and chairs on the floor, the counter with i
- InteriorPass.Homes.cs (597) InteriorPass: The rooms of a home, furnished like the starter home and the raid town's houses with their own worki
- InteriorPass.Offices.cs (589) InteriorPass: A bank officer's desk: the officer's chair behind it, two for the customers across it.
- InteriorPass.Plan.cs (756) RoomRole, InteriorUse, InteriorPlan: What a room of a real building is for, finer than RoomKinds (which the loot tables read): it picks t
- InteriorPass.Programs.cs (705) InteriorPass: What the enterable building e is used as, or InteriorUse.None for a building with nothing to furnish
- InteriorPass.Rooms.cs (892) InteriorPass, Run, Piece: Partitions along one wall line, merged: from A to B on the rooms' lines, the wall itself from Start 
- InteriorPass.Shell.cs (280) InteriorPass: The plan's outside walls lined from inside and its windows dressed, into shell's walls: a wall at a 
- InteriorPass.Shops.cs (348) InteriorPass: Shops: the sales floor (the checkout by the door, gondolas down the middle, shelving round the walls
- InteriorPass.Trades.cs (191) InteriorPass: Warehouses, workshops, barns and greenhouses: racking in rows with the aisles between, pallets by th
- InteriorPass.Workplaces.cs (902) InteriorPass, Bay: A bay off a corridor: its rectangle, and the side of it the corridor runs along (0 south, 1 east, 2 
- InteriorPass.cs (221) InteriorPass: The insides of the real-world map's enterable buildings: what BuildingPass lists in Site.Enterables 
- InteriorStreamer.cs (559) InteriorStreamer, Job, Memory: Furnishes the statewide map's buildings near the survivors and takes them down again behind them.
- KentuckyMap.Site.cs (546) KentuckyMap, Street, Line: How far past the playable square a fixed (unstreamed) map draws its surroundings, as scenery.
- KentuckyMap.Statewide.cs (172) KentuckyMap, ChunkJob: The side of a streamed map piece (chunk) in metres; 64 of them make a world-store region.
- KentuckyMap.cs (145) KentuckyMap: Builds a raid map from a real place in Kentucky: the streets, buildings, water and land use around t
- KentuckyStreamer.cs (1492) KentuckyStreamer, Chunk, RegionSlot: The whole of Kentucky as one raid map.
- MapContracts.cs (97) KentuckyMap, Detail, Junction: How much of a site is built: Full near survivors (and the fixed square), Lite for far streamed chunk
- MapCutaway.Doors.cs (143) MapCutaway: A door's point lies on its wall's middle line: this close to the outline it's in that wall.
- MapCutaway.Heights.cs (416) MapCutaway, HeightJob: Metres per unit of a height cell.
- MapCutaway.Overlay.cs (806) MapCutaway, StandInVertex, OverlayJob: What's left of a cut wall, PZ-style: its bottom this many metres, capped.
- MapCutaway.Shells.cs (135) MapCutaway, Building: Footprints smaller than this (m²) share the site's batches: too small to stand in, or to hide anyone
- MapCutaway.Work.cs (257) MapCutaway: Time a frame may spend reading meshes and making stand-ins (ms), and while a cut is waiting on one.
- MapCutaway.cs (555) MapCutaway: The Kentucky map's isometric cutaway, Project Zomboid-style: roofs and upper storeys show from outsi
- MapCutawayDriver.cs (21) MapCutawayDriver: Runs the Kentucky map's isometric cutaway (MapCutaway) once a frame, after the player camera has pla
- MapCutawaySite.cs (56) MapCutawaySite: The buildings of one map site (the fixed square, or one streamed piece) that have shells of their ow
- MapData.cs (181) MapCurrent, MapManifest, MapRegionEntry: world/current.json: the build the game and the servers load.
- MapGameplay.Density.cs (394) MapGameplay, CellStat: Households' worth of people whose buildings stand in the cell.
- MapGameplay.Landmarks.cs (376) MapGameplay, RealLandmark, OpenGround: The prefix of a real landmark's id (published as "landmark:ky.p123": p a point of interest, b a buil
- MapGameplay.Loot.cs (756) MapGameplay, Profile: One purpose's containers, room by room: the plain pools, and (rich) with the rarer finds a well stoc
- MapGameplay.Purpose.cs (435) MapGameplay, Purposes, NameWord: What a building was for (see PurposeOf(MapBuilding)).
- MapGameplay.Tally.cs (135) MapGameplay: The last fixed square's loot tally (one line, also logged).
- MapGameplay.cs (721) MapGameplay, ExitPlan: What the real-world map means for play.
- MapGameplayRuntime.cs (108) MapGameplayRuntime, Entry: The real map's landmarks as a survivor finds them: one component (created with the first landmark wo
- MapGeometry.cs (346) MapGeometry: Plane geometry for the real-world map, in the raid map's ground plane: a Vector2 is (x, z), metres e
- MapJunctions.cs (116) MapJunctions, Touch, SortedArm: Where a site's carriageways meet: every point inside the site's square where two or more street piec
- MapRegionIndex.cs (163) MapRegionIndex, Bucket: One region of the world store's build (MapRegion), sorted by the pieces (chunks) the streamed map is
- MapStyle.cs (54) MapStyle: What the real-world map's passes share about looks.
- PorchLamp.cs (137) PorchLamp, PorchLampDriver: The light of a lamp by a front door: lit after dark, out by day, with a faint flicker; never shadowe
- PowerPass.cs (1024) PowerPass, Chain, Anchor: Overhead power, the way rural and small-town Kentucky strings it: wooden poles along nearly every ro
- RoadPass.Berms.cs (266) RoadPass, DrivewaySpot, Driveway: A driveway the roads laid from a street to a house: At on the kerb line, Dir towards the house, Leng
- RoadPass.Bridges.cs (329) RoadPass, Profile: Heights added along a chain (a raised deck), by arc length; linear between its points.
- RoadPass.Junctions.cs (1372) RoadPass, KerbLine: An arm's kerb line on one side, out from the junction (and 3 m back past it): its points, and the ar
- RoadPass.Markings.cs (319) RoadPass, Along: Where a line runs across a chain: about its centre line, a fraction across its lanes, or by one of i
- RoadPass.Network.cs (742) RoadPass, Cls, Winter: One side of a carriageway, seen along the way it's drawn: offsets in metres from the street's line.
- RoadPass.Paths.cs (74) RoadPass: Footways, paths, steps and pedestrian streets: a trodden band through the snow (paved, for a pedestr
- RoadPass.Rail.cs (329) RoadPass: The railways: a ballast bed dusted with snow, wooden ties (some under snow), two steel rails with po
- RoadPass.Sides.cs (669) RoadPass, CutKind, Cut: A verge wider than this is drawn as the snowy ground between two banks, not a kerb-high strip.
- RoadPass.Streets.cs (456) RoadPass: A strip along the samples between left offsets a[i] and b[i] (negative: right of the line), its edge
- RoadPass.Textures.cs (168) RoadTextures: The road pass's own tileable textures, painted once a session and shared by every chunk: wet ploughe
- RoadPass.Wear.cs (149) RoadPass: A thin flat line along pts, width wide, on the asphalt.
- RoadPass.cs (412) RoadPass, KerbStall, Look: The real-world map's roads in a Kentucky January: carriageways meeting in real junctions (one paved 
- SignPass.cs (615) SignPass: Traffic control and road signs the way a Kentucky town had them in 1993: signals hung from a span wi
- SnowPass.cs (370) SnowPass: The real-world map's lying snow, past what the ground, the roads and the roofs carry: the plough's d
- StreetLamp.cs (529) StreetLamp, Lens, StreetLampDriver: A street light that still burns: the fixture at the foot of its pole (carrying the pole's collider, 
- StreetscapePass.Atlas.cs (689) StreetscapePass, SignArt, Painter: A material for an alpha-tested sign atlas (lit, a little glossy, both faces drawn by separate quads)
- StreetscapePass.Furniture.cs (623) StreetFurniture: The small things along the streets: fire hydrants (mapped, and at the town's corners and along its b
- StreetscapePass.Kit.cs (800) StreetscapePass, Ctx, Crossing: The streetscape's view of one site while it's built.
- StreetscapePass.Landmarks.cs (483) Landmarks: What stands out over a Kentucky town: its water tower on its legs with the town's name round the tan
- StreetscapePass.Lights.cs (398) StreetLights: The street lights, as a Kentucky town had them in 1993: cobra heads on arms off the wooden utility p
- StreetscapePass.cs (111) StreetscapePass: The real-world map's street furniture, what makes a place read as small-town Kentucky in 1993: woode
- SwingInteractable.cs (16) SwingInteractable: A porch swing the survivor can "Sit" on (nothing happens yet: the action only closes the prompt).
- VegetationPass.Models.cs (543) TreeKind, TreeTier, ShrubKind: The trees the map grows: Kentucky's winter woods and yards.
- VegetationPass.Plan.cs (1216) Land, NaturePlan, CellPoint: What a spot of ground is mapped as, for the nature passes (unmapped land is None).
- VegetationPass.Yards.cs (350) VegetationPass, Slot: A yard tree's place round its house, in the house's frame (see NaturePlan.HouseFrame).
- VegetationPass.cs (606) VegetationPass, Mix, Planting: The real-world map's trees and bushes, Kentucky in deep winter: bare oaks, maples, tulip poplars and
- VehiclePass.Models.cs (457) CarBody, CarWear, CarLook: The bodies of the cars and trucks left in Kentucky in January 1993 (mostly late seventies to early n
- VehiclePass.Streets.cs (493) VehiclePass: Cars parked along the kerbs as the roads drew them.
- VehiclePass.cs (638) VehiclePass, Candidate, Parking: The real-world map's cars, as January 1993 left them (CarKit): at the house end of most driveways a 
- WorldStore.cs (160) WorldStore, MapBuild: The world store, where the build machines put the real-world map (Tools/World): \\192.168.1.170\CLON

## Assets/Scripts/World/Zone
- Helicopter.cs (387) HelicopterModel, HelicopterFlight, HelicopterService: Builds the helicopter: fuselage, cockpit glass, tail boom and fin, skids, two spinning rotors, a sea
- Uncanny.cs (302) UncannyDirector, ReturnedFigure, DistantFigure: Fear without a health bar.
- ZoneAtmosphere.cs (120) ZoneAtmosphere: The air of a zone, layered over the clock and the weather each frame: Mirelock's fog that never lift
- ZoneCordon.cs (419) ZoneCordon, CordonGate, SearchlightSweep: The cordon (Roadside Picnic's illegal entry): an inner fence just inside the restricted belt along t
- ZoneKit.cs (703) ZoneKit, ZoneSmokeTint, ZoneChunks: Shared pieces for the zone overlays: the extra materials they need (chain-link, light beams, steam a
- ZoneLandmarks.cs (714) ZoneLandmarks, LocationVisitTrigger: The handcrafted anchors of each zone (Metro's set pieces, used sparingly): a crashed helicopter, a d
- ZoneOverlays.cs (405) ZoneForest, ZoneIndustry: Mirelock's woods: hundreds of snow-dusted conifers and bare birches crowding every open patch of gro
- ZoneSiteMap.cs (334) SiteCell, ZoneSiteMap: What lies on one metre of a raid map, as far as the zone overlays care.
- ZoneWorld.cs (201) ZoneWorld, ZoneWorldModule, ZoneRuntime: What the zone overlays know about the raid map being built: the roots they build under, the occupanc

## Assets/Scripts/Zombies
- ZombieAI.cs (816) ZombieType, ZombieAI, State: NavMesh-driven zombie brain.
- ZombieHealth.cs (101) ZombieHealth: Zombie hit points.
- ZombieSpawner.cs (405) SpawnerMode, ZombieSpawner: How the spawner fills the map: escalating waves (wave survival) or a standing population of wanderer

## Assets/Scripts/Zone
- Anomalies.cs (634) AnomalyDef, AnomalyCatalog, Anomaly: One kind of anomaly: what it does and the single consistent rule a survivor can learn about it.
- ArtifactCatalog.cs (223) ArtifactCatalog, Property, Def: What each artifact actually does.
- Artifacts.cs (446) ArtifactNode, ArtifactField, ArtifactBelt: An artifact lying in an anomaly, latent: invisible until a detector good enough for its tier is swit
- Bolts.cs (206) Bolt, BoltThrower: A thrown nut with a strip of rag tied on: the oldest tool in the Zone.
- Detectors.cs (283) DetectorDevice, Model, HudDetector: The artifact detector, switched on with O (or "Switch on" in the pack).
- Emissions.cs (220) EmissionDirector, Phase: Emissions: the Zone's periodic convulsion.
- Radiation.cs (327) PlayerRadiation, RadiationSpot, HudRadiation: Radiation as a dose the body keeps (0..1; 1 is lethal) rather than a bar that refills: the Zone's ba
- ZoneCatalog.cs (102) ZoneDef, ZoneCatalog: One of the Zones left behind by the Passing: a map with its own rules.
- ZoneHazards.cs (296) ZoneExposure, Mark, ZoneMarkEffects: The Zone's cost on the body, across runs: radiation taken, emissions lived through and artifacts wor
