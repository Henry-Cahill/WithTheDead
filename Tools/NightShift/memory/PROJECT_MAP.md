# CLONE-Z project map

Generated at the start of every night. Each script with its line count, main types and the first
sentence of its summary. Grep the code for details.

## Assets/Editor
- CloneZBatch.cs (57) CloneZBatch: Entry points for running the editor headless (Unity.exe -batchmode -executeMethod ...): regenerating

## Assets/Scripts/AI/Creatures
- CreatureCatalog.cs (165) CreatureLoot, CreatureDef, CreatureCatalog: One line of a carcass's loot: an item id, how many, and the chance it is there at all.
- CreatureContent.cs (83) CreatureContent, PsiShield, ZoneLifeModule: Carcass loot: hides, pelts and the mutant parts the Institute and the Fence pay for.
- CreatureMeshes.cs (350) CreaturePrim, CreaturePartBuilder, UnitMesh: The unit shapes creature bodies are assembled from (each one unit across and one unit tall).
- Creatures.cs (647) Creature, Mode, CreatureBody: The Zone's non-human life.
- PlayerMonster.cs (369) HunterForms, PlayerMonster, HunterNet: The shapes a hunter can take, each with one trick (enemies as puzzles — now with a human brain).

## Assets/Scripts/AI/Humans
- ALife.cs (434) ALifeSquad, ALifeDirector, ZonePeopleModule: One squad of the Zone's people, alive whether or not anyone is watching.
- Companions.cs (225) CompanionService, Order, Candidate: Hired guns.
- HumanLoadouts.cs (353) GunPick, LootPick, HumanLoadout: One weighted gun choice of a loadout; the round defaults to the gun's own.
- HumanNpc.cs (953) HumanNpc, Mode, HumanNpcAvatar: A person in the Zone: a stalker, a soldier, a scavenger, a warden on a tower, a zealot of the Choir,
- HumanUtil.cs (254) HumanUtil, HumanRunner: Small helpers shared by the Zone's people (stalkers, A-Life squads, outposts, wardens, companions): 
- Outposts.cs (351) OutpostSystem, Outpost, PdaStashes: Warfare outposts (Gray Zone Warfare's PvEvP): sandbagged positions held by a faction's garrison.

## Assets/Scripts/Audio
- AudioDirector.cs (291) AudioModule, ListenerRig, FootstepAudio: Installs the synthesiser as the game's sound backend and wires the sounds no single system owns.
- Sfx.cs (162) ISfxBackend, Sfx, SfxIds: Produces the clip for a sound id (the procedural synthesiser implements this).
- SfxSynth.cs (585) SfxSynth, Recipe: Every sound in the game, synthesised in code on first use (there are no audio assets): gunshots with

## Assets/Scripts/Audio/Synth
- SynthCore.cs (932) SynthGen, OnePole, Biquad: The deterministic random source and buffer helpers for one generated sound (one variation of one id)

## Assets/Scripts/Bootstrap
- GameBootstrapper.cs (647) GameBootstrapper: Builds the entire playable world from code — ground, cover, lighting, the player, the isometric came

## Assets/Scripts/Camera
- CinemachineViews.cs (124) CinemachineViews: The camera rig as Cinemachine sees it (the doc's Unity setup: one virtual camera per mode, blended o
- CutawayOccluder.cs (31) CutawayOccluder: Marks a wall (or any tall piece of building) that the isometric camera may cut away when it stands b
- IsoCameraFollow.cs (727) IsoCameraFollow: The player camera.
- IsoCutaway.cs (364) IsoCutaway, Piece, HiddenRenderer: Project Zomboid's wall cutaway for the isometric camera.
- ViewCrosshair.cs (151) ViewCrosshair: The small crosshair at the screen centre in the shoulder and first-person views: a dot and four tick

## Assets/Scripts/Characters
- HumanoidAnimator.Body.cs (349) HumanoidAnimator: Whole-body poses: the gait cycle, crouching, posture, crawling, sitting, lying and death.
- HumanoidAnimator.Hands.cs (440) HumanoidAnimator, HandGoal: Hands and head: two-bone IK for weapon holds, swings, shoves, lunges and jobs (working a fixture, re
- HumanoidAnimator.cs (474) HumanoidGait, HumanoidActivity, HumanoidAnimator: How a humanoid moves: the survivor's walk and run, or one of the dead's gaits.
- HumanoidBodyBuilder.Body.cs (383) HumanoidBodyBuilder: Head, torso, arms and hands.
- HumanoidBodyBuilder.Gear.cs (295) HumanoidBodyBuilder: Legs, feet and footwear, headwear, and the blood on the dead.
- HumanoidBodyBuilder.cs (405) HumanoidBodyBuilder, Ring, Key: Generates the skinned humanoid mesh for one HumanoidShape.
- HumanoidLook.cs (392) BodyBuild, HairStyle, HeadwearStyle: Overall body proportions a humanoid is built with.
- HumanoidMeshFactory.cs (67) HumanoidMesh, HumanoidMeshFactory: A baked humanoid mesh and the colour region drawn by each of its submeshes.
- HumanoidProportions.cs (142) HumanBone, HumanoidProportions: The bones of the humanoid rig, in hierarchy order (a parent always precedes its children).
- HumanoidRig.cs (231) HoldStyle, HumanoidRig: How an item sits in the right hand.
- PlayerAvatar.cs (330) PlayerAvatar: Clone Z's body.
- WeaponHandIK.cs (154) WeaponHandIK, SceneToStreamJob: Hand IK on the weapon with the Animation Rigging package (the doc's "one full-body rig for every vie
- ZombieAvatar.cs (131) ZombieAvatar: A zombie's body: the same humanoid as the survivor, dressed as a random dead survivor (grimy clothes

## Assets/Scripts/Content
- ContentRegistry.cs (151) IContentProvider, ContentRegistry, ContentBuilder: A feature that contributes items, loot-table entries or patches to existing items (ammunition and ar
- ZoneContent.Arms.cs (244) ZoneContent: The Zone's content: ammunition and guns with real ballistics, attachments, body armour and Zone suit
- ZoneContent.Gear.cs (320) ZoneContent: Every artifact id (their effects live in ArtifactCatalog; descriptions never tell).

## Assets/Scripts/Core
- Actor.cs (198) ActorKind, Actor, IZombieAttackable: What sort of creature an Actor is.
- Broadcasts.cs (354) BroadcastStation, Broadcasts, Programme: The stations the home's sets can pick up: the portable TV's three channels, then the kitchen radio's
- Climate.cs (224) Climate: The outdoor weather in real units, for the HUD watch and the body-temperature model.
- CloneZConfig.Bathroom.cs (68) CloneZConfig
- CloneZConfig.Doors.cs (27) CloneZConfig
- CloneZConfig.Fixtures.cs (30) CloneZConfig
- CloneZConfig.Kitchen.cs (54) CloneZConfig
- CloneZConfig.Lighting.cs (33) CloneZConfig
- CloneZConfig.Living.cs (78) CloneZConfig
- CloneZConfig.Raid.cs (13) CloneZConfig
- CloneZConfig.RaidCivic.cs (52) CloneZConfig
- CloneZConfig.RaidHomes.cs (53) CloneZConfig
- CloneZConfig.RaidLighting.cs (14) CloneZConfig
- CloneZConfig.RaidOpenings.cs (55) CloneZConfig
- CloneZConfig.RaidStore.cs (95) CloneZConfig
- CloneZConfig.cs (514) CloneZConfig: Central gameplay tuning.
- CloneZUtil.cs (100) CloneZUtil: Small helpers shared across the bootstrapper and gameplay.
- CombatFX.cs (73) CombatFX, FxFade: Throwaway visual feedback for gunfire: a fading tracer line and a brief muzzle flash.
- CombatResolver.cs (208) Hitbox, ShotRequest, ShotResult: A collider that belongs to one body zone of an actor (heads take more, armour covers some).
- DamageModel.cs (204) DamageKind, BodyZone, BodyZones: What kind of harm a blow does.
- Factions.cs (152) Factions, Def, CordonRules: The groups that live in (and around) the Zone, how they feel about each other, and how they feel abo
- GameEvents.cs (66) RaidOutcome, KillInfo, GameEvents: How a raid ended for the local survivor.
- GameInput.cs (124) GameAction, GameInput: Gameplay actions bound through GameInput (the original controls read the keyboard directly).
- GameManager.cs (119) GameState, GameManager: Owns global game state: score, waves, and win/lose flow.
- GameModules.cs (78) IGameModule, GameModules, WorldHooks: A gameplay feature that wires itself in at boot: registers item actions, subscribes to GameEvents an
- GameSession.cs (91) GameMode, RaidKind, CameraView: The kind of run being played, picked on the title screen or in the hub.
- IDemolishable.cs (28) IDemolishable: Anything the build tool's Demolish mode can tear down — player-built structures (barricades, house p
- NoiseSystem.cs (67) NoiseSystem, Pulse: Lightweight world sound system.
- PlayerModifiers.cs (99) StatMods, IStatModSource, PlayerModifiers: The survivor's combined stat modifiers for this frame — the sum of everything that bends the base ru
- PowerGrid.cs (90) PowerGrid: The electricity supply.
- SandboxSettings.cs (205) InfectionMode, DeathPenalty, SandboxSettings: The sandbox dials (Project Zomboid style) plus the server rules: nearly everything that changes how 
- SaveData.cs (230) SaveData, PlayerSave, StackSave: The complete serialisable snapshot of a run — world clock, weather, utilities, score, the survivor (
- SaveSystem.cs (495) SaveSystem: Reads and writes the game to disk and knows how to capture a live run into a SaveData and pour a Sav
- Shelter.cs (32) Shelter: The interiors of the current world — boxes of space under a roof.
- SqliteSaveStore.cs (827) SqliteSaveStore: The relational persistence layer: decomposes a SaveData snapshot into normalised SQLite tables on Wr
- Subtitles.cs (33) Subtitles, Line: The caption line for things the survivor hears: a radio or TV broadcast, a voice on the street.
- TimeOfDay.cs (192) TimeOfDay: Drives a continuous day/night cycle: advances a 24-hour clock, swings the directional "sun", and ble
- UIState.cs (21) UIState: Global UI/input flags.
- WaterMains.cs (62) WaterMains: The municipal water supply.
- WeatherSystem.cs (310) Weather, WeatherSystem: A lightweight weather simulation.
- WorldMarkers.cs (57) WorldMarkers, ZoneMarkRegistry: Named points the current world publishes for other systems: landmarks ("landmark:crashed_helicopter"
- WorldNavMesh.cs (30) WorldNavMesh: Holds the world's NavMeshSurface so gameplay can rebuild the baked NavMesh when a baked-in obstacle 

## Assets/Scripts/Core/Sqlite
- Sqlite.cs (246) SqliteNative, SqliteException, SqliteConnection: A tiny, self-contained binding to the native SQLite C API — just the handful of entry points the sav

## Assets/Scripts/Dev
- SmokeTest.cs (400) SmokeTest: An automated smoke test, inert unless the game (or a batch-mode editor in play mode) is started with

## Assets/Scripts/Economy
- CentralEconomy.cs (280) EconomyRule, EconomyConfig, CentralEconomy: One loot rule: how many of a thing the Zone holds (nominal), the floor restocks refill to (min), how
- Hideout.cs (362) ModuleLevel, ModuleDef, Recipe: One level of a hideout module: what it costs to build and what it gives.
- Insurance.cs (152) InsuranceDesk, CorpseRecovery: Insurance, Tarkov style: pay a share of an item's value and, if you die carrying it and nobody walks
- Money.cs (170) Money, StarterPack: The money: military-grade 5.45 rounds (ZoneContent.Currency).
- MoralLedger.cs (81) MoralLedger, HubScreens: The hidden morality track (Metro): sparing people who give up, listening to the bar's stories, helpi
- TraderCatalog.cs (146) TraderOffer, LoyaltyLevel, TraderDef: One thing a trader sells: for money, or as a barter for other items.
- Trading.cs (195) Trading: Buying, bartering and selling against the active character's stash, and the loyalty that gates what 

## Assets/Scripts/Extraction
- ExtractionBeacon.cs (391) ExtractionBeacon: The in-world beacon for one RaidExit: a tall glowing light column that reads across the dark town, a
- ExtractionManager.cs (475) ExtractionManager, RaidState: Runs a raid: the spawn-protection countdown, the exits (beacons in the world, extraction progress wh
- RaidLoadout.cs (446) RaidLoadout, RaidKit, RaidKitPiece: The kit a survivor takes into a raid and brings back out.
- RaidSummaryUI.cs (464) RaidSummaryUI, RaidSummary, LineOrder: The end-of-raid report: EXTRACTED / KILLED IN ACTION / MISSING IN ACTION, how long the raid ran, the
- RaidTypes.cs (80) ExitCondition, RaidExit, RaidMapInfo: What an exit demands before it lets you out (Tarkov's conditional extracts turn the map into a puzzl
- RaidWorldRules.cs (166) RaidWorldRules, PowerSwitch: The per-raid rolls on top of a zone's fixed layout: fresh loot in every container, which exits are o

## Assets/Scripts/Flow
- GameFlow.cs (63) GameFlow: The combined core loop's transitions: title → hub → (deploy) raid → summary → hub.

## Assets/Scripts/Gadgets
- Gadgets.cs (683) DroneKind, Drone, DronePickup: Bodycam's drones: a flying FPV drone (fast, loud, can carry a grenade to drop) and a tracked ground 

## Assets/Scripts/Hub
- HubCatalog.cs (33) HubDef, HubCatalog: One of the places runners live between raids.
- HubNpc.cs (209) HubNpc, Role, HubNpcTalk: Someone in the hub: a station keeper, a patron nursing a drink, a guitarist on the stage, an old-tim
- HubWorld.cs (384) HubRules, HubStation, HubStashLocker: Rules that hold in the hub: a safe place.

## Assets/Scripts/Hub/Screens
- HomeScreens.cs (434) Upkeep, UpkeepScreen, HideoutScreen: The pressure behind every run (Roadside Picnic's desperation): rent on the room above the bar, a deb
- HubKit.cs (264) HubItemSource, HubItemRef, HubKit: Where an item the hub's screens work on lives.
- HubScreenBase.cs (159) HubScreenBase: Base for the hub's screens: a large window with the money on hand in the title bar, a hover tooltip,
- HubTooltip.cs (176) HubTooltip, HubTooltipTrigger: A hover tooltip panel for a hub screen: one per screen canvas, drawn above everything, following the
- HubUI.cs (485) HubUI: Layout and row builders shared by the hub's screens (on top of UIKit): anchored areas and columns, c
- ServiceScreens.cs (437) StashScreen, MedicScreen, InsuranceScreen: The stash: move things between the stash shelves and what you carry.
- TradeScreens.cs (246) TraderScreen, FleaScreen: A trader's counter: what they sell at your loyalty level (money or barter), what they'll buy from yo

## Assets/Scripts/Items
- ContentDatabase.cs (547) ContentDatabase: The relational store for the game's static content — the item catalogue, the per-container loot tabl
- Inventory.cs (275) Inventory: A weight-limited bag of ItemStacks, used both for the player's carried items and for world container
- InventoryLayout.cs (483) GridSection, GridSortMode, InventoryLayout: One rectangular grid of cells inside an Inventory: the survivor's pockets, a worn backpack / chest r
- ItemActions.cs (84) ItemMenuAction, ItemActions: One extra verb on an item's menu ("Attach to M9", "Strip mods", "Throw").
- ItemDatabase.cs (514) ItemCategory, ClothingSlot, GearKind: Where a piece of clothing is worn.
- ItemExtra.cs (174) ItemExtra, ItemStackExtras, ItemConditionTiers: Per-instance data an item can carry beyond its count, freshness and wear: whether it was FoundInRaid
- LootContainer.cs (356) ContainerType, LootContainer: A searchable world container.

## Assets/Scripts/Medical
- MedicalItems.cs (139) MedicalItems, Body: What the field medicine does.
- PlayerBlood.cs (195) PlayerBlood: DayZ's separate blood and shock pools.
- PlayerLimbs.cs (597) PlayerLimbs, HudLimbs: The survivor's seven hit zones (Tarkov): head, thorax, stomach, both arms, both legs, each with its 

## Assets/Scripts/Net
- NetAuthority.cs (33) NetAuthority: Who decides what happens.
- NetPlayers.cs (558) NetPlayers, NetTicker, NetTracer: The other runners.
- NetSession.cs (455) NetOut, NetIn, NetClientInfo: A message being written (a thin wrapper over the netcode's buffer writer).
- NetSocial.cs (1283) NetCodec, NetSocial, VoiceCapture: Item lists on the wire (every field a stack carries: wear, age, repairs, extras).
- NetWorld.cs (500) NetWorld, NetAgentProxy: The shared world.

## Assets/Scripts/Player
- PlayerBuilder.cs (846) PlayerBuilder, BuildSelection: The survivor's construction tool.
- PlayerClothing.cs (175) PlayerClothing, WornPiece: The clothing the survivor is wearing — one garment per body slot.
- PlayerCombat.cs (600) PlayerCombat, FlareProjectile, NetClock: Melee and firearms.
- PlayerController.cs (367) PlayerController: Survival movement: move relative to the camera, aim Project Zomboid style, sprint/sneak with stamina
- PlayerFarming.cs (338) PlayerFarming, FarmAction: Lets the survivor work the land.
- PlayerFoodSickness.cs (119) PlayerFoodSickness: Food poisoning — Project Zomboid's Sickness / Nausea moodle.
- PlayerGear.cs (552) PlayerGear: The survivor's powered and protective gear: night-vision goggles worn in the Eyes slot (toggled with
- PlayerGearParts.cs (291) PlayerGearParts, PlayerGearTorso: Building blocks for PlayerGearVisuals: collider-free primitive parts, flat straps (single boxes, or 
- PlayerGearVisuals.cs (622) PlayerGearVisuals, Piece: Draws the survivor's gear on the body: a backpack on the back (sized by how much it holds, in the ba
- PlayerHydration.cs (107) PlayerHydration: Lets the survivor get water.
- PlayerInteractor.cs (733) PlayerInteractor, FocusKind: Everything the survivor reaches out and uses.
- PlayerInventory.cs (865) PlayerInventory: The player's carried items plus the currently-equipped melee weapon.
- PlayerPanic.cs (189) PlayerPanic: The survivor's fear response — Project Zomboid's Panic moodle.
- PlayerSeat.cs (298) PlayerSeat: Sitting down — on a dining chair, the armchair, the edge of the bed, the toilet lid — and lying down
- PlayerSkills.cs (209) PlayerSkills, Skill: The player's Project Zomboid–style skills (perks).
- PlayerSleep.cs (177) PlayerSleep: Lets the player sleep at a bed to pass the night, shedding fatigue and slowly healing (Project Zombo
- PlayerStats.cs (343) Exertion, PlayerStats: Survival stats for the player: health, endurance, hunger, thirst and the zombie infection.
- PlayerTemperature.cs (199) PlayerTemperature: The survivor's thermoregulation — Project Zomboid's body-temperature and wetness model.
- PlayerWounds.cs (560) WoundType, Wound, PlayerWounds: Kinds of injury.
- Stealth.cs (151) Stealth, HudStealth: How easily the survivor is seen and heard right now (Metro's wrist light, Gray Zone Warfare's visibi

## Assets/Scripts/Profile
- ProfessionCatalog.cs (129) ProfessionDef, TraitDef, ProfessionCatalog: A starting profession: skill head-starts, a little kit, and how many trait points it leaves.
- ProfileData.cs (323) AccountProfile, CharacterProfile, HealthCarry: The account: its characters (up to three), the season, and what was learned that no wipe takes away.
- ProfileStore.cs (348) ProfileStore: Loads, saves and edits the account profile (clonez_profile.json under the persistent data path): the
- Progression.cs (50) Progression: The character's level: experience from kills, extractions, found loot, quests and exploration.

## Assets/Scripts/Quests
- QuestBook.cs (477) ObjectiveKind, QuestObjective, QuestDef: Kill: "faction:wardens", "kind:Zombie", "creature:hound", "human".

## Assets/Scripts/Rendering
- ActorFog.cs (76) ActorFog: Fog of war for any actor that isn't a zombie (people, mutants, animals, other players): its body is 
- BreathFX.cs (174) BreathFX: Visible breath in the cold (reference photo 3): while Climate.BreathVisible, a small puff of vapour 
- FlashlightFX.cs (209) FlashlightFX: The survivor's flashlight beam: a warm white, shadow-casting spot light from the torch clipped to th
- IsoVisionShade.cs (114) IsoVisionShade, IsoVisionShadeModule: The isometric fog of war by day (Zomboid): from above you could otherwise see everything nearby, so 
- NightVisionFX.cs (303) NightVisionFX: The night-vision goggles' look (reference photo 2): a runtime global URP volume that turns the pictu
- Presentation.cs (362) PostFx, GasVisorOverlay, IsoAimLine: The look of the game on top of the cameras: a URP post-processing volume carrying the accessibility 
- RoomLight.cs (389) RoomLight: A room's light fitting — a ceiling bulb or a desk lamp: a warm point light with a small glowing bulb
- SnowFX.cs (241) SnowFX: Falling snow: soft white specks drifting down through the dark on a slight wind (reference photo 1),
- VisionAssets.cs (361) VisionAssets: Procedural textures, meshes and materials shared by the vision and lighting effects (the line-of-sig
- VisionController.cs (530) VisionController: The heart of the VisionRig: decides how dark the night is and lights only what the survivor can see,
- VisionRig.cs (88) VisionRig: Installs everything that decides what the survivor can see and how the world is lit: the line-of-sig
- ZombieVisibility.cs (243) ZombieVisibility, Sight: Added to every zombie by the spawner; hides the zombie's body while the survivor can't see it, Proje

## Assets/Scripts/Survival
- Diseases.cs (164) PlayerDiseases, DiseaseModule: DayZ's illnesses, each with its own cause and its own cure — survival needs that interact rather tha
- FieldSurvival.cs (517) PlayerChannel, FieldSurvival, SnareTrap: A timed action the survivor performs in the field (foraging, fishing, reading, cranking a charger): 
- Navigation.cs (258) ZoneMapScreen, Mark, JournalScreen: The Zone map (P), on a clipboard: streets and buildings, the landmarks, the exits — and no dot sayin

## Assets/Scripts/UI
- GridDragHandle.cs (62) GridDragHandle: Moves a floating window when its title bar is dragged, or resizes it from its corner grip.
- GridPointerRelay.cs (44) GridElement, GridPointerRelay: What kind of grid-inventory element a GridPointerRelay sits on.
- GridScrollRect.cs (20) GridScrollRect: A ScrollRect that scrolls with the mouse wheel and its scrollbar but not by dragging its content: in
- GridSprites.cs (118) GridSprites: Procedural sprites for the grid inventory, built once and cached: the tiling cell, a small gear, a h
- HUDController.cs (472) HUDController: The code-built heads-up display, laid out like a Project Zomboid HUD mod: hand slots top-left, gas-m
- HealthPanelController.cs (464) HealthPanelController: A code-built health screen (toggled with H) inspired by Project Zomboid's health panel.
- HudHands.cs (220) HudHands: Top-left hand slots, Project Zomboid HUD-mod style: a circle for the weapon in hand (line-art silhou
- HudHints.cs (117) HudHints: The key-hint chips along the bottom-centre (two compact rows of key caps + actions).
- HudIconArt.cs (408) HudIcon, HudIconSpec, HudIconArt: Every procedural sprite the HUD draws (see HudIconArt for the art).
- HudIcons.cs (53) HudIcons: Bakes the HUD's procedural art (HudIconArt) into sprites on first use and caches them for the rest o
- HudInteraction.cs (178) HudInteraction, Row: The interaction overlay: a small label hung over whatever the survivor has in focus (a fixture's nam
- HudMoodles.cs (219) HudMoodles, Kind, Moodle: Project Zomboid-style moodles: a column of small square tiles down the right edge, one per active co
- HudRaidOverlay.cs (134) HudRaidOverlay: Raid feedback from ExtractionManager: the floating green "Spawn Protection: 40s" over the survivor's
- HudRaster.cs (267) HudRaster: A tiny anti-aliased vector rasteriser for the HUD's procedural line art.
- HudSevenSegment.cs (60) HudSevenSegment: One digit of the digital watch: seven hexagonal segments built from sliced images, with the unlit se
- HudStamina.cs (77) HudStamina: Bottom-left: a running-man pictogram that mirrors how the survivor is moving (standing, walking, spr
- HudStatusRow.cs (412) HudStatusRow, StatusIcon, InfoRow: Bottom-right: the mode box (amber "EXTRACTION MODE", or the wave and kill count in wave survival) wi
- HudSubtitles.cs (69) HudSubtitles: Captions for what the survivor hears — a radio or TV broadcast (Subtitles): the latest two lines at 
- HudToasts.cs (64) HudToasts: Short fading messages at the bottom-centre, above the interaction prompts — e.g.
- HudUI.cs (211) HudContext, HudUI: Everything the HUD widgets read, gathered once by HUDController.Bind.
- HudWatchRow.cs (232) HudWatchRow: The top-right row, right-aligned: the gas-mask "Filter 72%" panel (only with a mask on), a dim activ
- InventoryUIController.Grid.cs (1384) InventoryUIController, GridSide, GridWindow: Whose cells a section view draws.
- InventoryUIController.GridActions.cs (249) InventoryUIController: Switches between the grid windows and the verbose list (remembered across runs).
- InventoryUIController.GridDrag.cs (997) InventoryUIController, DragState, DropKind: The item being dragged, where it came from and how it's held.
- InventoryUIController.List.cs (811) InventoryUIController, SortKey, EntryKind: One listed item: a stack in the bag or a container, a worn garment, or a floor pile.
- InventoryUIController.Picker.cs (300) InventoryUIController: The CONTAINERS column down the right of the inventory: Project Zomboid's strip of container
- InventoryUIController.cs (915) InventoryUIController, MenuOption: The code-built inventory / loot UI, in two interchangeable views (toggled from either; the choice pe
- MainMenuController.cs (229) MainMenuController: The title screen: your runners (up to three per account), making a new one, hosting or joining a ser
- PauseMenuController.cs (282) PauseMenuController: The in-game pause menu (Esc).
- SkillsPanelController.cs (215) SkillsPanelController: A code-built skills screen (toggled with K) listing every perk with its current level and progress t
- TooltipTrigger.cs (78) TooltipTrigger: Attached to each inventory row; reports pointer enter/exit to the InventoryUIController so it can sh
- UIFader.cs (54) UIFader: Fades a panel's CanvasGroup in and out so it opens and closes smoothly instead of popping.
- UIKit.cs (485) UIKit, UIScreen, HudLayer: The shared builder for code-made uGUI: canvases, panels, labels, buttons, scroll lists, toggles, sli
- UITheme.cs (43) UITheme: Shared styling helpers for the code-built UI.

## Assets/Scripts/Weapons
- Ballistics.cs (218) ModSlot, AmmoStats, GunStats: Where an attachment goes on a gun.
- GearEffects.cs (31) GearEffects: What worn armour and Zone suits do to the body besides stopping rounds: plates and exo-frames weigh 
- WeaponItems.cs (236) WeaponItems: The inventory verbs for guns and their parts: attach an attachment (to the gun in hand, else the fir
- WeaponUpgrades.cs (125) WeaponUpgrades, Node: The technician's branching upgrade trees (STALKER-style).

## Assets/Scripts/World
- Barricade.cs (291) Barricade: A player-built wooden barricade (Project Zomboid's planked windows/doorways).
- BuildPiece.cs (491) BuildPieceType, BuildPiece: The kind of structural piece a survivor can raise when building a house.
- Camps.cs (746) ZoneCamps, CampGate, StructureBlast: DayZ's base building and raiding, fitted to raids into a Zone.
- CeilingVisibility.cs (58) CeilingVisibility: Shows interior ceilings (and other overhead pieces such as the tent roof and the gas-station canopy)
- CookingStation.cs (80) CookingStation: A stove the survivor can cook raw ingredients at.
- Cover.cs (53) Cover: A scattered arena cover block — a solid box that breaks line of sight and carves the baked NavMesh.
- DecorFont.cs (112) DecorFont: A tiny 5×7 bitmap font for painting text into procedural textures (the "U.S.
- DecorMaterials.cs (80) DecorMaterials: Cached URP materials for the raid map's textured surfaces (snow, asphalt, floors, walls, ceilings, c
- DecorPalette.cs (120) DecorPalette: One small point-filtered texture holding every flat colour the raid map uses, one texel per colour.
- DecorProps.cs (452) DecorProps, CarKind: Outdoor props and loose debris for the raid map: wrecked cars, bare trees and pines, dumpsters, sand
- DecorTextures.cs (970) WainscotStyle, PaperStyle, ExteriorStyle: The interior wall finishes the raid map paints rooms with: a dark lower wainscot under a pale paper,
- FallbackItemModel.cs (98) FallbackItemModel: A plain stand-in model for items that have no bespoke one yet — a single tinted primitive picked by 
- FarmPlot.cs (349) FarmPlot, PlotState, FarmCrop: A tilled patch of soil the survivor can sow, water and harvest — Project Zomboid's farming plot.
- FurnitureBuilder.Bathroom.cs (590) FurnitureBuilder, BathFacing, BathRing: Glazed white china: the toilet, the basin, the shower tray.
- FurnitureBuilder.Fixtures.cs (208) FurnitureBuilder: A wooden panel door leaf hinged at the local origin and extending along +X (width × height, 4.5 cm t
- FurnitureBuilder.Home.cs (290) FurnitureBuilder: A run of wooden base cabinets with a laminate top (0.62 deep, 0.92 high), optionally with a stainles
- FurnitureBuilder.Kitchen.cs (707) FurnitureBuilder, KitchenTone: The colours of one run of kitchen units, picked once so the carcass and the doors drawn apart from i
- FurnitureBuilder.Lighting.cs (237) FurnitureBuilder: A cream switch plate width wide centred on at (on the wall face).
- FurnitureBuilder.Living.cs (284) FurnitureBuilder: Height of the middle of a DresserBody drawer (row 0 the top one).
- FurnitureBuilder.Office.cs (236) FurnitureBuilder: A beige 1990s CRT monitor (deep tapered housing, dark glass) on a swivel foot, standing at at.
- FurnitureBuilder.RaidCivic.cs (887) FurnitureBuilder, CrateLoad: The middle of filing drawer i (0 = the bottom one).
- FurnitureBuilder.RaidHome.cs (493) FurnitureBuilder: How many two-door bays a run of wall cupboards len long is split into (each about the starter home's
- FurnitureBuilder.RaidOpenings.cs (297) OpeningLine, FurnitureBuilder: A straight wall line in world axes, as the raid town's door and window builders draw on it: a point 
- FurnitureBuilder.RaidStore.cs (469) FurnitureBuilder: The copier's lid hinge (its back edge, under the lid).
- FurnitureBuilder.Utility.cs (341) FurnitureBuilder: A white steel medicine cabinet (0.9 × 0.42 × 1.8): glazed upper doors over shelves of boxes and bott
- FurnitureBuilder.cs (227) FurnitureBuilder: The raid map's furniture, built from boxes, prisms and quads into a RaidMesh.
- GeneratedItemModels.cs (128) GeneratedItemModels, FilePart, FileModel: Ground models produced by the offline LLM asset pipeline (Tools/AssetPipeline), loaded from Resource
- GroundItemRenderer.cs (188) GroundItemRenderer: Draws every item lying on the ground near the player.
- GroundItemStore.cs (352) GroundItemStore, Item, Chunk: Every item lying on the ground in the world, held as data rather than as scene objects.
- HomeFurniture.Bathroom.cs (98) HomeFurniture
- HomeFurniture.Kitchen.cs (89) HomeFurniture: The kitchen's loose furniture (see the per-area hooks in HomeFurniture.cs): the counter along the we
- HomeFurniture.Living.cs (271) HomeFurniture: The living room's, bedroom's and store room's furniture (see HomeFurniture): the dining table (a sur
- HomeFurniture.cs (361) HomeFurnitureKind, HomeFurniture: The loose furniture that dresses the starter home.
- HomeLamp.cs (225) HomeLamp: A lamp of the wave-survival starter home — the bedside lamp, the standard lamp, the kitchen-diner's 
- ItemMeshBaker.cs (253) ItemMeshBaker, Lod, Baked: Turns an ItemModelDesc into a single combined mesh that can be drawn instanced.
- ItemModelDesc.cs (230) PartShape, PartMat, Part: Which primitive mesh a part is cut from.
- ItemModels.cs (12057) ItemModels: Builds bespoke, hand-modelled ground visuals for specific items, assembled out of primitives (the pr
- PrimitiveMesh.cs (188) PrimitiveMesh: The unit primitives item models are assembled from, generated rather than taken from GameObject.Crea
- RaidBuilding.Civic.cs (760) RaidBuilding: The raid town's shared workplace furniture, made usable: filing cabinets whose drawers each slide ou
- RaidBuilding.Doors.cs (212) RaidBuilding, DoorPolicy: The raid town's doors: every doorway declared with a leaf gets the starter home's kind of door — a p
- RaidBuilding.Fixtures.cs (215) RaidBuilding: The raid town's interactive layer, shared by every building: the starter home's fittings — Interacta
- RaidBuilding.Furnish.cs (520) RaidBuilding, Place: A placement in the building's local frame: centre (x, z) and yaw (0 faces +Z).
- RaidBuilding.Home.cs (477) RaidBuilding: The raid town's houses (RaidLayoutsHomes.cs) fitted out the way the starter home is: the home's own 
- RaidBuilding.Lighting.cs (654) RaidBuilding, WallFitting, SwitchSpot: The raid town's lights, switches and wall decor, working as the starter home's do.
- RaidBuilding.Openings.cs (81) RaidBuilding: A roll-up door in the (leafless) doorway at local (x, z): the drum over the inside of the doorway wi
- RaidBuilding.Windows.cs (288) RaidBuilding: The raid town's windows: the starter home's double-hung window — a fixed upper sash over a lower one
- RaidBuilding.cs (656) FloorKind, LightStyle, RaidRoom: Floor finishes for raid-map rooms.
- RaidLayouts.cs (255) RaidLayouts: The raid map's authored buildings.
- RaidLayoutsCivic.cs (244) RaidLayouts: The town clinic: waiting room with reception, an exam room, the doctor's office, a three-bed ward an
- RaidLayoutsHomes.cs (246) RaidLayouts: A family house: the 12 × 10 ranch (living room, kitchen, bedroom, bathroom with a bath, den) or the 
- RaidLayoutsOffice.cs (255) RaidLayouts: The county office: an open-plan floor of desks with CRTs and lamps (photo 3), a lobby with reception
- RaidLayoutsStore.cs (269) RaidLayouts: The gas station's food mart: the checkout counter with its till, gondolas of shelves to browse from 
- RaidMapBuilder.Edge.cs (256) RaidMapBuilder
- RaidMapBuilder.Outdoor.cs (194) RaidMapBuilder
- RaidMapBuilder.Props.cs (227) RaidMapBuilder: The object every interactive outdoor prop hangs under (made on first use).
- RaidMapBuilder.Streets.cs (193) RaidMapBuilder, RoadSpec: An axis-aligned street: asphalt, a faded double centre line, kerbs, sidewalks, banks of plowed snow 
- RaidMapBuilder.Town.cs (295) RaidMapBuilder: Draws a prop into the outdoor mesh at a world placement, with an optional collider (size.y = 0 for n
- RaidMapBuilder.cs (94) RaidMapBuilder: Builds the extraction raid map under world: a snowbound, run-down 1993 Kentucky town at night (140 ×
- RaidMesh.cs (490) Surf, RaidMesh: How a piece of raid-map geometry is skinned: a material plus the way its UVs are generated.
- RaidTown.cs (195) RaidTown, OutdoorBatches: Shared state while one raid map is being built: the seeded random stream, the three roots (the baked
- SleepSpot.cs (97) SleepSpot: A bed the player can sleep in to pass the night and shed fatigue.
- StarterHome.Bathroom.cs (65) StarterHome
- StarterHome.Doors.cs (130) StarterHome
- StarterHome.Grounds.cs (302) StarterHome: The street past the front of the home: sun-faded asphalt with a dashed yellow centre line and white 
- StarterHome.Kitchen.cs (51) StarterHome
- StarterHome.Lighting.cs (217) StarterHome
- StarterHome.Shell.cs (365) StarterHome, Frame, Band: Builds the finished look onto a house piece standing in one of the home's slots — its visible geomet
- StarterHome.Windows.cs (205) StarterHome
- StarterHome.cs (447) StarterHome, HomeRoom, Slot: The wave-survival starter home, finished to the extraction town's standard.
- ToxicZone.cs (293) ToxicZone: A cloud of toxic gas: breathing it without a gas mask (or with a spent filter) sickens and hurts the
- Vehicles.cs (761) Vehicle, VehicleDriver, HudVehicle: A car (DayZ's vehicle assembly, Zomboid's hotwiring): they're found broken at the Zone's parking spo
- WaterSource.cs (278) WaterSourceKind, WaterSource: Where the survivor can get water — a plumbed tap or a rain barrel.
- WorldItem.cs (124) WorldItem: A handle to one item lying on the ground.

## Assets/Scripts/World/Fixtures
- BasinFixture.cs (261) BasinFixture: The bathroom's pedestal basin: a single-lever mixer tap and a plug on a chain.
- BathroomFixtures.cs (79) BathroomFixtures: Small helpers shared by the bathroom's fixtures (ToiletFixture, BasinFixture, ShowerFixture, TowelRa
- BedFixture.cs (174) BedFixture: The bed (on its SleepSpot).
- BreakerPanel.cs (117) BreakerPanel: The house's breaker panel, in the store room: a grey steel box whose tin door opens on the dead-fron
- ClockFixture.cs (137) ClockFixture: The living room's battery wall clock.
- DoormatFixture.cs (75) DoormatFixture: The coir welcome mat on the front stoop: wipe your feet on it, or lift its near edge to see what's u
- HomeDoor.cs (323) HomeDoor, Setup: A door of the starter home, hung in a finished doorway BuildPiece by StarterHome.Doors.cs: a pair of
- HomeWindow.cs (422) HomeWindow, Setup, Lookout: A window of the starter home, glazed and hung by StarterHome.Windows.cs: a fixed upper sash over a l
- HoseSpigot.cs (175) HoseSpigot: The outside tap: a standpipe with a red wheel beside the hose reel, round the west side of the house
- KitchenFridge.cs (206) KitchenFridge: The workings of the kitchen's fridge-freezer (HomeFurniture of kind Fridge): a small freezer over a 
- KitchenParts.cs (157) KitchenFrame, KitchenParts: A kitchen fitting's local frame, as the furniture builders draw it (front toward +Z, origin on the f
- KitchenSink.cs (472) KitchenSink: The kitchen sink under the front window — the fixture fronting the kitchen tap's WaterSource.
- KitchenStove.cs (272) KitchenStove: The gas range's controls — the fixture on the kitchen's CookingStation.
- LampSwitch.cs (70) LampSwitch: The switch of a free-standing lamp — the bedside lamp, the standard lamp, a desk lamp: [E] turns it 
- LightCircuits.cs (97) LightCircuits, Wiring: The starter home's lighting circuits — the living room's, the porch's, the store room's and the bath
- LightSwitch.cs (117) LightSwitch: A wall light switch by a door: a cream plate of one or more toggles (in a tent, a switch box's lever
- LightingFixtures.cs (34) LightingFixtures: Small shared helpers for the home's lights, switches, wall decor and yard fixtures.
- LivingFixtures.cs (39) LivingFixtures: Small shared helpers for the living room's and bedroom's fixtures.
- LivingReading.cs (112) LivingReading, Book: The bookcase's pastimes, offered after browsing its shelves: reading a few chapters of one of the bo
- LivingReceiver.cs (258) LivingReceiver: A set that plays the broadcasts (Broadcasts) — the portable TV and the kitchen radio.
- MailboxFixture.cs (183) MailboxFixture: The rural mailbox at the kerb.
- MirrorCabinet.cs (184) MirrorCabinet, Sign: The mirror cabinet over the bathroom basin: a white wall cabinet with a mirrored door, kept as a Sto
- OutdoorHouseNumber.cs (40) OutdoorHouseNumber: The enamelled house-number plate beside the front door: "Read the house number".
- OutdoorLantern.cs (51) OutdoorLantern: The black carriage lantern beside the front door.
- PictureFixture.cs (142) PictureFixture: A framed print on the wall.
- RadioFixture.cs (82) RadioFixture: The kitchen radio on the worktop (see LivingReceiver): battery powered, so it plays on long after th
- RaidCivicFixtures.cs (362) CivicToggle, CivicLook, CivicRest: A workplace fitting with two states, flipped with [E]: a bench vice wound open or shut, a ward's pri
- RaidCivicFridge.cs (233) CivicFridge, CivicReading, Shelf: The workings of a workplace fridge-freezer — the National Guard post's kitchenette fridge, the clini
- RaidCivicRadio.cs (318) CivicRadio, Channel, Band: A two-way set the survivor can listen in on — the National Guard post's olive field radio, the depot
- RaidCivicSink.cs (277) CivicSink: The deep steel sink let into a workplace counter — the National Guard post's kitchenette, the clinic
- RaidDoor.cs (400) RaidDoor, Raid: A door of the raid town: the starter home's door (HomeDoor) — a pair of leaves drawn shut across the
- RaidHomeBath.cs (375) RaidHomeBath: A raid ranch's bath: a built-in tub with a hot and a cold tap over the drain and a plug on a chain.
- RaidHomeLiving.cs (170) RaidHomeLiving: The small pastimes and the paperwork of the raid town's houses: plumping the sofa's cushions, flicki
- RaidHomeWasher.cs (178) RaidHomeWasher: The controls of a raid cottage's washing machine (its porthole door and the drum behind it are a Sto
- RaidStoreCopier.cs (142) RaidStoreCopier: The county office's photocopier.
- RaidStoreCot.cs (115) RaidStoreCot: The quarantine tent's folding camp cot: "Lie down and rest" stretches out on the canvas, awake (an o
- RaidStoreSafe.cs (78) RaidStoreSafe: The food mart's floor safe in the stock room, locked.
- RaidStoreSwivelSeat.cs (131) RaidStoreSwivelSeat: A swivel office chair in the raid town's county office.
- RaidStoreVending.cs (135) RaidStoreVending: The break room's snack and soda machine.
- RaidStoreWaterCooler.cs (95) RaidStoreWaterCooler: The county office's bottled-water cooler: a water point (IWaterPoint) the survivor can drink from or
- RaidStoreWaterHeater.cs (105) RaidStoreWaterHeater: The county office's gas water heater in the utility room, as a water point (IWaterPoint): its cold i
- RaidTownFieldRadio.cs (224) RaidTownFieldRadio, Net: A National Guard field radio left on the quarantine tent's field desk (and one more on the ledge of 
- RaidTownGasPump.cs (66) RaidTownGasPump: A gas pump on the food mart's forecourt, worked from whichever side the survivor stands.
- RaidTownGenerator.cs (112) RaidTownGenerator: The portable generator at the quarantine site, its tank part full.
- RaidTownHydrant.cs (44) RaidTownHydrant: A fire hydrant on a street corner, frozen up for the winter: nothing to be had from it without a hyd
- RaidTownMailDrop.cs (56) RaidTownMailDrop: A blue public mail drop box on a Main Street sidewalk.
- RaidWindow.cs (385) RaidWindow, Glazing, Dressing: A window of the raid town: the starter home's double-hung window (HomeWindow) — a lower sash that sl
- RainBarrelFixture.cs (70) RainBarrelFixture: The rain barrel by the west wall, as a fixture: fill a bottle from it or drink (the water's tainted 
- RollUpShutter.cs (161) RollUpShutter, Setup: A depot's roll-up door: a steel curtain of slatted bands that rolls up into the drum over the inside
- SeatFixture.cs (135) SeatFixture: Somewhere to sit: a dining chair or the armchair.
- ShowerFixture.cs (304) ShowerFixture: The bathroom's shower stall: a glass door that swings out and a mixer that sets the rose spraying — 
- ToiletFixture.cs (300) ToiletFixture, Step: The bathroom's toilet.
- TowelRail.cs (146) TowelRail: The chrome towel rail on the bathroom wall over the toilet, a towel folded over it.
- TvFixture.cs (230) TvFixture: The portable TV on the bedroom dresser (see LivingReceiver): it runs off the mains, so it needs the 

## Assets/Scripts/World/Interaction
- FixturePart.cs (133) FixturePart, Motion: A moving piece of a fixture — a cupboard door, a drawer, an oven door, a window sash, a toilet seat 
- Interactable.cs (271) InteractAction, InteractContext, Interactable: One thing the survivor can do at a fixture — "Turn on the tap", "Close the curtains", "Sit down".
- StorageFixture.cs (219) StorageOptions, StorageFixture, FixtureContents: How a StorageFixture is filled and worded.
- WaterPoints.cs (162) IWaterPoint, WaterPoints, WaterUse: Anything the survivor can drink from or fill a bottle at: a WaterSource (the kitchen tap, the rain b

## Assets/Scripts/World/Zone
- Helicopter.cs (336) HelicopterModel, HelicopterFlight, HelicopterService: Builds the helicopter: fuselage, cockpit glass, tail boom and fin, skids, two spinning rotors, a sea
- Uncanny.cs (290) UncannyDirector, ReturnedFigure, DistantFigure: Fear without a health bar.
- ZoneAtmosphere.cs (66) ZoneAtmosphere: The air of a zone, layered over the clock and the weather each frame: Mirelock's fog that never lift
- ZoneCordon.cs (240) ZoneCordon, CordonGate, SearchlightSweep: The cordon (Roadside Picnic's illegal entry): an inner fence just inside the restricted belt along t
- ZoneKit.cs (610) ZoneKit, ZoneSmokeTint, ZoneChunks: Shared pieces for the zone overlays: the extra materials they need (chain-link, light beams, steam a
- ZoneLandmarks.cs (331) ZoneLandmarks, LocationVisitTrigger: The handcrafted anchors of each zone (Metro's set pieces, used sparingly): a crashed helicopter, a d
- ZoneOverlays.cs (210) ZoneForest, ZoneIndustry: Mirelock's woods: hundreds of snow-dusted conifers and bare birches crowding every open patch of gro
- ZoneSiteMap.cs (327) SiteCell, ZoneSiteMap: What lies on one metre of a raid map, as far as the zone overlays care.
- ZoneWorld.cs (175) ZoneWorld, ZoneWorldModule, ZoneRuntime: What the zone overlays know about the raid map being built: the roots they build under, the occupanc

## Assets/Scripts/Zombies
- ZombieAI.cs (735) ZombieType, ZombieAI, State: NavMesh-driven zombie brain.
- ZombieHealth.cs (96) ZombieHealth: Zombie hit points.
- ZombieSpawner.cs (282) SpawnerMode, ZombieSpawner: How the spawner fills the map: escalating waves (wave survival) or a standing population of wanderer

## Assets/Scripts/Zone
- Anomalies.cs (514) AnomalyDef, AnomalyCatalog, Anomaly: One kind of anomaly: what it does and the single consistent rule a survivor can learn about it.
- ArtifactCatalog.cs (196) ArtifactCatalog, Property, Def: What each artifact actually does.
- Artifacts.cs (373) ArtifactNode, ArtifactField, ArtifactBelt: An artifact lying in an anomaly, latent: invisible until a detector good enough for its tier is swit
- Bolts.cs (198) Bolt, BoltThrower: A thrown nut with a strip of rag tied on: the oldest tool in the Zone.
- Detectors.cs (241) DetectorDevice, Model, HudDetector: The artifact detector, switched on with O (or "Switch on" in the pack).
- Emissions.cs (211) EmissionDirector, Phase: Emissions: the Zone's periodic convulsion.
- Radiation.cs (273) PlayerRadiation, RadiationSpot, HudRadiation: Radiation as a dose the body keeps (0..1; 1 is lethal) rather than a bar that refills: the Zone's ba
- ZoneCatalog.cs (96) ZoneDef, ZoneCatalog: One of the Zones left behind by the Passing: a map with its own rules.
- ZoneHazards.cs (241) ZoneExposure, Mark, ZoneMarkEffects: The Zone's cost on the body, across runs: radiation taken, emissions lived through and artifacts wor
