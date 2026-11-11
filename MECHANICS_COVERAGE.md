# WithTheDead — Mechanics coverage

This maps every row of *Survival Shooter Mechanics Breakdown* (Oct 2026) to where it lives in the game. Paths are relative to the repo root. Systems wire themselves in as `IGameModule`s ([GameModules.cs](Assets/Scripts/Core/GameModules.cs)), so each row names the module or class to start from. The answers to the doc's design tensions are in [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md).

## Roadside Picnic: book to mechanic

| Book element | In WithTheDead | Where |
| --- | --- | --- |
| The Visitation (several Zones, never explained) | Several Zones, each with its own map, depth, loot multiplier, mutants, cordon and special rule. The cause is never explained (in-game it's "the Passing"). | [ZoneCatalog.cs](Assets/Scripts/Zone/ZoneCatalog.cs), [ZoneWorld.cs](Assets/Scripts/World/Zone/ZoneWorld.cs) |
| Litter, not treasure | Artifacts have hidden properties, learned by wearing them on the belt or paying the Institute. Knowledge is kept per account and sold as rumours. | [Artifacts.cs](Assets/Scripts/Zone/Artifacts.cs), [ArtifactCatalog.cs](Assets/Scripts/Zone/ArtifactCatalog.cs), `RumoursScreen` in [ServiceScreens.cs](Assets/Scripts/Hub/Screens/ServiceScreens.cs) |
| Bolts | Throw a bolt (X) with its own feel and sound. Anomalies react to the bolt first. | [Bolts.cs](Assets/Scripts/Zone/Bolts.cs) |
| Anomalies | Five types with consistent but alien rules (sinkhole, embermoss, staticbloom, greysyrup, tollgate). A rule is learned by surviving it. | [Anomalies.cs](Assets/Scripts/Zone/Anomalies.cs) |
| Illegal entry | A cordon with a restricted belt where the Wardens shoot. Pay for a pass, bribe the gate or risk it. Two economies: the Institute (legal, fair) and the fence (rich, risky, raises your Heat). | [ZoneCordon.cs](Assets/Scripts/World/Zone/ZoneCordon.cs), `CordonRules` in [Factions.cs](Assets/Scripts/Core/Factions.cs), [TraderCatalog.cs](Assets/Scripts/Economy/TraderCatalog.cs) |
| The bar and the fences | The hub is a bar full of people with buyers, rumours, music and stories. The starter bar, the Kettle, stands in Bardstown on the Kentucky map, and walking out of its front door starts the raid where you stand. | [HubWorld.cs](Assets/Scripts/Hub/HubWorld.cs), [KettleLobby.cs](Assets/Scripts/Hub/KettleLobby.cs), [HubNpc.cs](Assets/Scripts/Hub/HubNpc.cs), [TradeScreens.cs](Assets/Scripts/Hub/Screens/TradeScreens.cs) |
| The Zone's cost on the body | Exposure builds lasting marks (buffs with drawbacks) that outlive death. Radiation dose carries between raids. | `ZoneExposure` in [ZoneHazards.cs](Assets/Scripts/Zone/ZoneHazards.cs), [Radiation.cs](Assets/Scripts/Zone/Radiation.cs) |
| Uncanny, not hostile | The cemeteries' dead walk home and sit. Whispers, stingers and wrongness, with no health bar. | [Uncanny.cs](Assets/Scripts/World/Zone/Uncanny.cs) |
| The deadly route | The tollgate anomaly lets you through only after it takes someone. A hired companion can be ordered to go first. | [Anomalies.cs](Assets/Scripts/Zone/Anomalies.cs), [Companions.cs](Assets/Scripts/AI/Humans/Companions.cs) |
| The Golden Sphere | The Lodestar: a rumoured wish deep in the Spillway, behind a questline and clues, with a wish that has a price. | `LodestarScreen` in [HomeScreens.cs](Assets/Scripts/Hub/Screens/HomeScreens.cs), [QuestBook.cs](Assets/Scripts/Quests/QuestBook.cs) |
| Desperation | Rent on the room, a debt and a family to feed, due by day. Missed payments have consequences. | `Upkeep` and `UpkeepScreen` in [HomeScreens.cs](Assets/Scripts/Hub/Screens/HomeScreens.cs) |
| Three rules, rights note | See [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md). All names are invented, and no book names remain in code or content. | — |

## 1. Escape From Tarkov

| Mechanic | Where |
| --- | --- |
| Raid + extraction (time limit, spawn-side exits, conditional exits: pay, no backpack, switch, co-op with scavs, flare) | [ExtractionManager.cs](Assets/Scripts/Extraction/ExtractionManager.cs), [RaidWorldRules.cs](Assets/Scripts/Extraction/RaidWorldRules.cs), [RaidTypes.cs](Assets/Scripts/Extraction/RaidTypes.cs) |
| Loss on death | `RaidLoadout.Forfeit` in [RaidLoadout.cs](Assets/Scripts/Extraction/RaidLoadout.cs) |
| Secure container (grows with level) | The pouch slot, `HubKit.SyncPouch` in [HubKit.cs](Assets/Scripts/Hub/Screens/HubKit.cs) |
| Insurance | `InsuranceDesk` in [Insurance.cs](Assets/Scripts/Economy/Insurance.cs), `InsuranceScreen` |
| Scav runs (cooldown, random kit, fence rep) | `ApplyScavKit` in [RaidLoadout.cs](Assets/Scripts/Extraction/RaidLoadout.cs), `RecordOutcome` in [ExtractionManager.cs](Assets/Scripts/Extraction/ExtractionManager.cs) |
| Traders + loyalty + barters | [TraderCatalog.cs](Assets/Scripts/Economy/TraderCatalog.cs), [Trading.cs](Assets/Scripts/Economy/Trading.cs), `TraderScreen` |
| Flea market (level-gated, fees, FIR only, server toggle) | `FleaScreen` in [TradeScreens.cs](Assets/Scripts/Hub/Screens/TradeScreens.cs) |
| Hideout (modules, real-time crafts) | [Hideout.cs](Assets/Scripts/Economy/Hideout.cs), `HideoutScreen` |
| Quests + found-in-raid | [QuestBook.cs](Assets/Scripts/Quests/QuestBook.cs) (Fetch objectives require FIR), [ItemExtra.cs](Assets/Scripts/Items/ItemExtra.cs) |
| Limb health (7 zones, bleeds, fractures, pain, blackouts) | [PlayerLimbs.cs](Assets/Scripts/Medical/PlayerLimbs.cs), [PlayerWounds.cs](Assets/Scripts/Player/PlayerWounds.cs), [MedicalItems.cs](Assets/Scripts/Medical/MedicalItems.cs) |
| Ballistics + armour; special rounds (a tracer's streak, an incendiary's fire) | [Ballistics.cs](Assets/Scripts/Weapons/Ballistics.cs), `CombatResolver` in [CombatResolver.cs](Assets/Scripts/Core/CombatResolver.cs), `CombatFX.Tracer` in [CombatFX.cs](Assets/Scripts/Core/CombatFX.cs) |
| Weapon modding | `ModStats` in [Ballistics.cs](Assets/Scripts/Weapons/Ballistics.cs), [WeaponItems.cs](Assets/Scripts/Weapons/WeaponItems.cs), the tablet's Gun page in [Gadgets.cs](Assets/Scripts/Gadgets/Gadgets.cs) |
| Grid inventory, weight drains stamina | [InventoryLayout.cs](Assets/Scripts/Items/InventoryLayout.cs), [InventoryUIController.Grid.cs](Assets/Scripts/UI/InventoryUIController.Grid.cs) |
| Use-based skills + level | [PlayerSkills.cs](Assets/Scripts/Player/PlayerSkills.cs), [Progression.cs](Assets/Scripts/Profile/Progression.cs) |
| Wipes | `ProfileStore.Wipe` in [ProfileStore.cs](Assets/Scripts/Profile/ProfileStore.cs), `SandboxSettings.SeasonLengthDays` |
| Sound (positional footsteps for every walker, doors, gear; walls muffle, rooms echo; volume buses) | [Sfx.cs](Assets/Scripts/Audio/Sfx.cs), [SfxMixer.cs](Assets/Scripts/Audio/SfxMixer.cs), [SfxAcoustics.cs](Assets/Scripts/Audio/SfxAcoustics.cs), [Footsteps.cs](Assets/Scripts/Audio/Footsteps.cs), recipes in [SfxSynth.cs](Assets/Scripts/Audio/SfxSynth.cs) and [Synth/Recipes](Assets/Scripts/Audio/Synth/Recipes) |

## 2. DayZ

| Mechanic | Where |
| --- | --- |
| Character permadeath (opt-in: Ironman / server rule) | `CharacterProfile.Ironman`, `RecordOutcome` in [ExtractionManager.cs](Assets/Scripts/Extraction/ExtractionManager.cs) |
| Persistent world (hybrid) | Persists per Zone: A-Life squads, looted containers, stashes, outposts, camps, cars (`ZoneWorldState` in [ProfileData.cs](Assets/Scripts/Profile/ProfileData.cs)) |
| Central Economy (nominal, min, lifetime, restock in a file) | [CentralEconomy.cs](Assets/Scripts/Economy/CentralEconomy.cs), with `economy.json` beside the save and a Reload button in the sandbox rules |
| Survival needs (hunger, thirst, temperature, wetness, energy, cooking) | [PlayerStats.cs](Assets/Scripts/Player/PlayerStats.cs), [PlayerTemperature.cs](Assets/Scripts/Player/PlayerTemperature.cs), [PlayerHydration.cs](Assets/Scripts/Player/PlayerHydration.cs), [CookingStation.cs](Assets/Scripts/World/CookingStation.cs) |
| Blood + health + shock, unconsciousness, typed transfusions | [PlayerBlood.cs](Assets/Scripts/Medical/PlayerBlood.cs), `BloodTypes` in [ArtifactCatalog.cs](Assets/Scripts/Zone/ArtifactCatalog.cs) |
| Diseases (cholera, food poisoning, wound infection, colds) | [Diseases.cs](Assets/Scripts/Survival/Diseases.cs), [PlayerFoodSickness.cs](Assets/Scripts/Player/PlayerFoodSickness.cs), [PlayerWounds.cs](Assets/Scripts/Player/PlayerWounds.cs) |
| Item condition: pristine to ruined; hits damage what you wear and what's in the bag | `ItemConditionTiers` in [ItemExtra.cs](Assets/Scripts/Items/ItemExtra.cs), `GearCollateral` in [PlayerLimbs.cs](Assets/Scripts/Medical/PlayerLimbs.cs) |
| Clothing as inventory (each worn container has its own grid) | [PlayerClothing.cs](Assets/Scripts/Player/PlayerClothing.cs), [InventoryLayout.cs](Assets/Scripts/Items/InventoryLayout.cs) |
| Base building + raiding (walls, gates, locks, tools/explosives, offline-raid rule) | [Camps.cs](Assets/Scripts/World/Camps.cs), [BuildPiece.cs](Assets/Scripts/World/BuildPiece.cs), [Barricade.cs](Assets/Scripts/World/Barricade.cs), [PlayerBuilder.cs](Assets/Scripts/Player/PlayerBuilder.cs) |
| Infected + wildlife | [ZombieAI.cs](Assets/Scripts/Zombies/ZombieAI.cs), [Creatures.cs](Assets/Scripts/AI/Creatures/Creatures.cs) (wolves, bears, deer) |
| Vehicle assembly (wheels, battery, spark plug, radiator) | [Vehicles.cs](Assets/Scripts/World/Vehicles.cs) |
| Proximity voice, radios, handcuffs and rope, force-feeding | [NetSocial.cs](Assets/Scripts/Net/NetSocial.cs) (`VoiceCapture`, `VoicePlayback`, `PlayerRestraint`, `RemotePlayerInteract`) |
| No GPS by default (map, compass and GPS are items) | [Navigation.cs](Assets/Scripts/Survival/Navigation.cs) |

## 3. Metro

| Mechanic | Where |
| --- | --- |
| Military-grade ammo as money | `Money` in [Money.cs](Assets/Scripts/Economy/Money.cs). The currency is a real round, `ZoneContent.Currency` in [ZoneContent.Arms.cs](Assets/Scripts/Content/ZoneContent.Arms.cs). |
| Gas mask + filters (watch timer, cracks, wipe the visor) | [PlayerGear.cs](Assets/Scripts/Player/PlayerGear.cs), `GasVisorOverlay` in [Presentation.cs](Assets/Scripts/Rendering/Presentation.cs), wipe on F2 |
| Light and stealth (wrist-light visibility, lighters, crank charger) | [Stealth.cs](Assets/Scripts/Player/Stealth.cs), `DiegeticWatch` in [Presentation.cs](Assets/Scripts/Rendering/Presentation.cs), crank in [FieldSurvival.cs](Assets/Scripts/Survival/FieldSurvival.cs) |
| Diegetic UI (watch, clipboard map, notebook, lighter) | `DiegeticWatch`, `ZoneMapScreen`/`JournalScreen` in [Navigation.cs](Assets/Scripts/Survival/Navigation.cs), HUD modes in [SandboxSettings.cs](Assets/Scripts/Core/SandboxSettings.cs) |
| Workbench + backpack crafting (scrap and chemicals) | `FieldCraftScreen` in [FieldSurvival.cs](Assets/Scripts/Survival/FieldSurvival.cs), [Hideout.cs](Assets/Scripts/Economy/Hideout.cs) |
| Weapon mods from enemies | `WeaponItems.Strip` in [WeaponItems.cs](Assets/Scripts/Weapons/WeaponItems.cs) |
| Weapon dirt + jamming | `ItemExtra.Dirt`, `WeaponItems.Clean`, `JamChance` in [Ballistics.cs](Assets/Scripts/Weapons/Ballistics.cs) |
| Hidden moral points | [MoralLedger.cs](Assets/Scripts/Economy/MoralLedger.cs) |
| Living hub stations | [HubWorld.cs](Assets/Scripts/Hub/HubWorld.cs), [HubNpc.cs](Assets/Scripts/Hub/HubNpc.cs) |
| Day/night picks the threat | `ThreatDirector` in [Creatures.cs](Assets/Scripts/AI/Creatures/Creatures.cs), [TimeOfDay.cs](Assets/Scripts/Core/TimeOfDay.cs) |
| Handcrafted set pieces (sparingly) | [ZoneLandmarks.cs](Assets/Scripts/World/Zone/ZoneLandmarks.cs) |

## 4. S.T.A.L.K.E.R.

| Mechanic | Where |
| --- | --- |
| A-Life (squads travel, fight, loot and die off-screen) | [ALife.cs](Assets/Scripts/AI/Humans/ALife.cs), [HumanNpc.cs](Assets/Scripts/AI/Humans/HumanNpc.cs) |
| Factions + reputation (pick one at creation) | [Factions.cs](Assets/Scripts/Core/Factions.cs), character creation in [MainMenuController.cs](Assets/Scripts/UI/MainMenuController.cs) |
| Anomalies + bolts | [Anomalies.cs](Assets/Scripts/Zone/Anomalies.cs), [Bolts.cs](Assets/Scripts/Zone/Bolts.cs) |
| Artifacts (inside anomalies, belt buffs with drawbacks, reshuffled by emissions) | [Artifacts.cs](Assets/Scripts/Zone/Artifacts.cs) |
| Detector tiers | [Detectors.cs](Assets/Scripts/Zone/Detectors.cs) |
| Emissions + psi-storms | [Emissions.cs](Assets/Scripts/Zone/Emissions.cs), [Shelter.cs](Assets/Scripts/Core/Shelter.cs) |
| Damage types + resistances | `DamageKind` and `Resistances` in [DamageModel.cs](Assets/Scripts/Core/DamageModel.cs) |
| Condition + repair + branching upgrades | [WeaponUpgrades.cs](Assets/Scripts/Weapons/WeaponUpgrades.cs), `TechnicianScreen` in [ServiceScreens.cs](Assets/Scripts/Hub/Screens/ServiceScreens.cs) |
| Weight limit | `Inventory.MaxWeight`, `PlayerInventory.MoveSpeedMultiplier` |
| Survival needs, cooking mutant meat, disassembly | [CreatureContent.cs](Assets/Scripts/AI/Creatures/CreatureContent.cs), [FieldSurvival.cs](Assets/Scripts/Survival/FieldSurvival.cs) |
| Ironman + PDA stashes | `PdaStashes` and `StashCache` in [Outposts.cs](Assets/Scripts/AI/Humans/Outposts.cs) |
| Mutants as puzzles (Shade cloaks, Puppeteer attacks minds, Thumper hits through cover) | [Creatures.cs](Assets/Scripts/AI/Creatures/Creatures.cs), [CreatureCatalog.cs](Assets/Scripts/AI/Creatures/CreatureCatalog.cs) |
| Guides (fast travel between hubs, for a fee) | `GuideScreen` in [ServiceScreens.cs](Assets/Scripts/Hub/Screens/ServiceScreens.cs), [HubCatalog.cs](Assets/Scripts/Hub/HubCatalog.cs) |

## 5. Project Zomboid

| Mechanic | Where |
| --- | --- |
| Profession + point-buy traits | [ProfessionCatalog.cs](Assets/Scripts/Profile/ProfessionCatalog.cs), [MainMenuController.cs](Assets/Scripts/UI/MainMenuController.cs) |
| Skills by use + skill books | [PlayerSkills.cs](Assets/Scripts/Player/PlayerSkills.cs), `SkillBooks` in [FieldSurvival.cs](Assets/Scripts/Survival/FieldSurvival.cs) |
| Moodles | [HudMoodles.cs](Assets/Scripts/UI/HudMoodles.cs), [PlayerPanic.cs](Assets/Scripts/Player/PlayerPanic.cs) |
| Noise + sight; world events pull hordes | [NoiseSystem.cs](Assets/Scripts/Core/NoiseSystem.cs), [Helicopter.cs](Assets/Scripts/World/Zone/Helicopter.cs) |
| Melee with weight: each weapon's reach, arc, wind-up and recovery, and chances to stagger the dead (spoiling a lunge) or knock them down; a stab meets one body, a wider swing two, a full sweep three; shove the dead down and stomp them on the ground (crawlers too), hardest on the head | [MeleeStats.cs](Assets/Scripts/Weapons/MeleeStats.cs), [PlayerCombat.cs](Assets/Scripts/Player/PlayerCombat.cs), `KnockDown` in [ZombieAI.cs](Assets/Scripts/Zombies/ZombieAI.cs), [ZombieHealth.cs](Assets/Scripts/Zombies/ZombieHealth.cs) |
| Throwables: noise lures that pull the dead off a house (firecrackers, an alarm clock, a timer radio), molotov fire that spreads and burns a crowd, a crafted pipe bomb | [Throwables.cs](Assets/Scripts/Weapons/Throwables.cs), [NoiseLure.cs](Assets/Scripts/Weapons/NoiseLure.cs), [Fire.cs](Assets/Scripts/Weapons/Fire.cs) |
| Infection (lethal to off) | `SandboxSettings.Infection`, [PlayerStats.cs](Assets/Scripts/Player/PlayerStats.cs) |
| Wound care (bandage, disinfect, stitch, shards, splint) | [PlayerWounds.cs](Assets/Scripts/Player/PlayerWounds.cs), [MedicalItems.cs](Assets/Scripts/Medical/MedicalItems.cs) |
| Utility shutoff | [WaterMains.cs](Assets/Scripts/Core/WaterMains.cs), [PowerGrid.cs](Assets/Scripts/Core/PowerGrid.cs) |
| Building + barricades | [PlayerBuilder.cs](Assets/Scripts/Player/PlayerBuilder.cs), [Barricade.cs](Assets/Scripts/World/Barricade.cs), [Camps.cs](Assets/Scripts/World/Camps.cs) |
| Food systems (farming, foraging, fishing, trapping, cooking, spoilage) | [FarmPlot.cs](Assets/Scripts/World/FarmPlot.cs), [FieldSurvival.cs](Assets/Scripts/Survival/FieldSurvival.cs), `ItemDef.Perishable` |
| Vehicles (keys or hotwiring, part condition, fuel, Mechanics) | [Vehicles.cs](Assets/Scripts/World/Vehicles.cs) (engine, tyres and battery wear) |
| Sandbox settings | [SandboxSettings.cs](Assets/Scripts/Core/SandboxSettings.cs), `SandboxScreen` |
| Build 42 additions (workstations, animals, darker nights) | [Hideout.cs](Assets/Scripts/Economy/Hideout.cs) (animal pen), [VisionController.cs](Assets/Scripts/Rendering/VisionController.cs), `NightDarkness` |
| A real place for a map: all of Kentucky, its streets and buildings in 256 m cells, streamed around the survivor in 128 m pieces, buildings with typed rooms and loot by use (the structure only; the data is OpenStreetMap's, see [Tools/World](Tools/World/README.md)) | [KentuckyMap.cs](Assets/Scripts/World/Map/KentuckyMap.cs), [KentuckyStreamer.cs](Assets/Scripts/World/Map/KentuckyStreamer.cs), [MapRegionIndex.cs](Assets/Scripts/World/Map/MapRegionIndex.cs), [WorldStore.cs](Assets/Scripts/World/Map/WorldStore.cs), the Kentucky zone in [ZoneCatalog.cs](Assets/Scripts/Zone/ZoneCatalog.cs) |

## 6. Beautiful Light

| Mechanic | Where |
| --- | --- |
| Shared raid objective (one artifact everyone is after) | The prize artifact in `ArtifactField` ([Artifacts.cs](Assets/Scripts/Zone/Artifacts.cs)), the `extractwith` job in [QuestBook.cs](Assets/Scripts/Quests/QuestBook.cs) |
| Player-controlled anomalies (invade a raid as a monster) | [PlayerMonster.cs](Assets/Scripts/AI/Creatures/PlayerMonster.cs) (Shade, Hound, Puppeteer); join model in [NetSession.cs](Assets/Scripts/Net/NetSession.cs) |
| Called extraction (flare, the helicopter comes, hold out) | `ExitCondition.Flare` in [ExtractionManager.cs](Assets/Scripts/Extraction/ExtractionManager.cs), [Helicopter.cs](Assets/Scripts/World/Zone/Helicopter.cs) |
| Artifacts with properties | [ArtifactCatalog.cs](Assets/Scripts/Zone/ArtifactCatalog.cs) |
| Loss on death + progression from quests and artifacts | [RaidLoadout.cs](Assets/Scripts/Extraction/RaidLoadout.cs), [Progression.cs](Assets/Scripts/Profile/Progression.cs) |
| Horror-first atmosphere (with a readability floor) | [ZoneAtmosphere.cs](Assets/Scripts/World/Zone/ZoneAtmosphere.cs), [Uncanny.cs](Assets/Scripts/World/Zone/Uncanny.cs), `PostFx` in [Presentation.cs](Assets/Scripts/Rendering/Presentation.cs), `GameSettings` |

## 7. Gray Zone Warfare

| Mechanic | Where |
| --- | --- |
| Open world, no raid timer | `RaidKind.OpenZone`, `ExtractionManager.NoDeadline` |
| Helicopter insertion (choose the LZ) | `DeployScreen` in [ServiceScreens.cs](Assets/Scripts/Hub/Screens/ServiceScreens.cs), `RaidWorldRules.PickInsertion` |
| Faction choice | Character creation, [Factions.cs](Assets/Scripts/Core/Factions.cs) |
| Death and corpse recovery | `CorpseRecovery` in [Insurance.cs](Assets/Scripts/Economy/Insurance.cs), `SandboxSettings.Death` |
| Detailed medical | [PlayerLimbs.cs](Assets/Scripts/Medical/PlayerLimbs.cs), [PlayerBlood.cs](Assets/Scripts/Medical/PlayerBlood.cs) |
| Replayable tasks + field manual | Daily contracts in [QuestBook.cs](Assets/Scripts/Quests/QuestBook.cs), `ManualScreen` in [HomeScreens.cs](Assets/Scripts/Hub/Screens/HomeScreens.cs) |
| Warfare mode (outposts locked 30 minutes) | [Outposts.cs](Assets/Scripts/AI/Humans/Outposts.cs) (server-validated captures in [NetWorld.cs](Assets/Scripts/Net/NetWorld.cs)) |
| Squad task sharing | `NetSocial.ShareQuest` in [NetSocial.cs](Assets/Scripts/Net/NetSocial.cs) |
| Visibility indicator | `HudStealth` in [Stealth.cs](Assets/Scripts/Player/Stealth.cs), the wristwatch lamp |
| Hideout + crafting, several characters per account | [Hideout.cs](Assets/Scripts/Economy/Hideout.cs), `AccountProfile.MaxCharacters` |

## 8. Bodycam

| Mechanic | Where |
| --- | --- |
| Bodycam view (wide distorted lens, heavy motion) | `CameraView.Bodycam` in [IsoCameraFollow.cs](Assets/Scripts/Camera/IsoCameraFollow.cs), [CinemachineViews.cs](Assets/Scripts/Camera/CinemachineViews.cs), `PostFx` |
| No crosshair, no HUD | `HudMode.Hardcore`/`Diegetic`, [ViewCrosshair.cs](Assets/Scripts/Camera/ViewCrosshair.cs) |
| True-body animation (one rig for every view) | [HumanoidAnimator.cs](Assets/Scripts/Characters/HumanoidAnimator.cs), [PlayerAvatar.cs](Assets/Scripts/Characters/PlayerAvatar.cs), [WeaponHandIK.cs](Assets/Scripts/Characters/WeaponHandIK.cs) |
| In-game tablet (loadouts, weapon tuning) | `TabletScreen` in [Gadgets.cs](Assets/Scripts/Gadgets/Gadgets.cs) |
| Drones (FPV and ground) | `Drone`, `DronePilot`, `DroneNet` in [Gadgets.cs](Assets/Scripts/Gadgets/Gadgets.cs) |
| Zombie content | [Zombies/](Assets/Scripts/Zombies) |

## Cross-game matrix

Every system in the matrix is covered by the rows above. Session structure, insertion, objective, loss, trading, sinks, loot (Central Economy), needs, medical, ballistics, modding, condition, hazards, world events, living AI, asymmetric PvP, hordes, stealth, horror, skills, creation, factions, bases, vehicles, camera, animation, UI, squads and social all appear there.

## The three cameras

| Mechanic | First person / bodycam | Third person | Isometric |
| --- | --- | --- | --- |
| Aiming | ADS through sights; no crosshair in bodycam or hardcore ([PlayerCombat.cs](Assets/Scripts/Player/PlayerCombat.cs)) | Over-shoulder reticle; shots leave the muzzle, so cover still blocks | Cursor on the ground plane at chest height; aim line on Alt (`IsoAimLine`) |
| What you see | What the eyes see | Server culls bodies the head can't see | `ActorFog` hides unseen actors; [IsoVisionShade.cs](Assets/Scripts/Rendering/IsoVisionShade.cs) shades unseen ground; at night [VisionController.cs](Assets/Scripts/Rendering/VisionController.cs) lights only line of sight |
| Stealth + light | The watch lamp | Same, plus your own shadow | Vision shading + `HudStealth` icon |
| UI | Diegetic (watch, clipboard map, notebook, tablet) | HUD fallback | HUD fallback (moodles) |
| Gas mask | Visor overlay, cracks, breathing | Breathing + filter timer | Breathing + filter timer |
| Anomalies | Bolts and detector beeps | Same | Fade in by distance (Anomalies.cs) |
| Audio | Listener on the head (`ListenerRig` in [AudioDirector.cs](Assets/Scripts/Audio/AudioDirector.cs)) in every view | | |

Input is one Input System action map per view (Common, Isometric, Shoulder, FirstPerson), switched as the view changes ([GameInput.cs](Assets/Scripts/Core/GameInput.cs)).

## Unity setup

| Doc | WithTheDead |
| --- | --- |
| URP | URP throughout. `PostFx` volume, URP materials. |
| Cinemachine | Cinemachine 3: one virtual camera per mode with custom blends, plus impulse shake ([CinemachineViews.cs](Assets/Scripts/Camera/CinemachineViews.cs)). Iso roof and wall cutaways in [IsoCutaway.cs](Assets/Scripts/Camera/IsoCutaway.cs). |
| One full-body rig + Animation Rigging | True first person on the head bone; Animation Rigging two-bone IK for the support hand ([WeaponHandIK.cs](Assets/Scripts/Characters/WeaponHandIK.cs)). |
| Visibility on the server | `NetPlayers.Visible` and the AI interest set in `NetWorld.SendAi`, with line of sight from each viewer's eye ([Net/](Assets/Scripts/Net)). |
| Netcode (decide in week one) | Netcode for GameObjects 2.x with Unity Transport. Server-authoritative, with lag-compensated hits. |
| Input System, a map per camera | [GameInput.cs](Assets/Scripts/Core/GameInput.cs) |

## Build order

All eleven steps are in, along with every item on the "push later or cut first" list:

* Player market: flea market.
* Base building: [Camps.cs](Assets/Scripts/World/Camps.cs).
* Vehicles: [Vehicles.cs](Assets/Scripts/World/Vehicles.cs).
* Farming: [FarmPlot.cs](Assets/Scripts/World/FarmPlot.cs).
* Full A-Life: [ALife.cs](Assets/Scripts/AI/Humans/ALife.cs).
* Player-controlled monsters: [PlayerMonster.cs](Assets/Scripts/AI/Creatures/PlayerMonster.cs).
* Handcrafted story: the Lodestar questline and landmarks.

Each of these can be switched off from the sandbox rules where the doc suggests a later mode (PvP, hunters, the flea market, the Central Economy, offline-raid rules).

## How it's checked

[SmokeTest.cs](Assets/Scripts/Dev/SmokeTest.cs) plays the game end to end. It stays inert unless the game or a batch-mode editor is started with `-clonez-smoke solo|hunter|host|client|lobby|nightwalk|grounditems|meshcache|horde|cutaway|attachments|guns|rounds|melee|throwables|wave`.

* **Solo:** makes a throwaway character with its own save file, walks into the bar, and opens every hub screen. Then it deploys into Kestrel Flats, or the zone named with `-clonez-smoke-zone`, and checks:
  * the tablet;
  * a grenade;
  * the FPV and ground drones;
  * a parked car, repaired and driven;
  * a camp gate blown by a breaching charge;
  * cuffs and rope;
  * the Central Economy;
  * a quest;
  * on the statewide Kentucky zone (`-clonez-smoke-zone bardstown`), a trip 1.5 km out, 10 km across a region border and
    back: the pieces under the survivor get built and baked into the NavMesh, the dead come back around them, the stretch
    left behind is let go of, and a container emptied there is still empty when it's built again.
* **Hunter:** plays a raid as a monster.
* **Host and client:** play a networked raid together on port 7791. With `-clonez-smoke-join <address[:port]>`, `client`
  plays against a server already running instead, such as the in-house dedicated server
  ([Tools/Server/README.md](Tools/Server/README.md)), and opens the raid on it itself.
* **Lobby:** goes from the title screen into the Kettle and checks that:
  * the town goes up a step at a time under the loading card;
  * the bar stands on a clear lot in Bardstown;
  * no anomaly or radiation spot reaches into the bar or the 20 m safe zone round it, and no anomaly can be spawned in the bar;
  * no rain or snow falls inside the bar, while it still falls in the street outside;
  * walking out of its front door starts the raid where the survivor stands, with no reload, and the door is barred behind them;
  * extracting brings the survivor back into the bar.

  Add `-clonez-smoke-shots <dir>` to save pictures of the title, the loading card, the bar and the street. This mode needs graphics, so leave out `-nographics`.
* **Night walk** ([SmokeTest.NightWalk.cs](Assets/Scripts/Dev/SmokeTest.NightWalk.cs)): goes into the Kettle from the title screen, sets the clock to 23:00 under a dry sky, and stops in the bar, in the street outside its door, in up to six lit rooms nearby, and in the street again at noon. At each stop it renders the game's camera at 1920×1080 and logs:
  * the frame time and the render on its own, waited out on the GPU;
  * the camera's, the sun's shadows' and the lamps' shadows' CPU and GPU time, with the sun's cascades as the game has them and with the other count (4 or 2);
  * the punctual lights casting shadows in view: lights, shadow maps, and the atlas texels they ask for.

  Any URP message about squeezing the lamps' shadow atlas fails the run (see the shadow budget, [ShadowBudget.cs](Assets/Scripts/Rendering/ShadowBudget.cs)). With `-clonez-smoke-shots <dir>`, it photographs each stop and writes the numbers to `nightwalk.csv` there. It renders, so leave out `-nographics`.
* **Ground items** ([SmokeTest.GroundItems.cs](Assets/Scripts/Dev/SmokeTest.GroundItems.cs)): in the Kettle, scatters 3,000 items of 48 kinds over the draw range round the survivor, and a pile of a kind nothing has looked at yet somewhere in range but out of view. Then it checks the ground-item renderer ([GroundItemRenderer.cs](Assets/Scripts/World/GroundItemRenderer.cs)):
  * standing still for 240 frames, it logs the renderer's CPU time a frame from its profiler samples (`GroundItemRenderer` and its `.Refresh`, `.Gather` and `.Draw`), beside what the old per-frame gather costs over the same items. It fails if any chunk is filed again while nothing on the ground changed, or if the renderer's refresh and gather together aren't at least four times cheaper than the old gather;
  * what it draws is what the old gather would have drawn: the same items, meshes, looks and levels of detail, with the same matrices, spin and bob included. It checks frames that only move what was already in view (standing still), frames gathered afresh, and frames with the animation off;
  * the pile out of view has nothing looked up (meshes load from disk on lookup). Standing on the pile gets it looked up and drawn;
  * dropping an item and picking it up again files just that chunk again each time;
  * every item's model and every generated mesh fits the reach the renderer assumes for an item before looking up its mesh (`GroundItemRenderer.UnknownReach`); the generated meshes are read from their files, so this part runs only in the editor.

  With `-clonez-smoke-shots <dir>`, the standing-still frames' numbers go to `grounditems.csv` there. The renderer needs a graphics device, so leave out `-nographics`.
* **Mesh cache** ([SmokeTest.MeshCache.cs](Assets/Scripts/Dev/SmokeTest.MeshCache.cs)): deploys into the statewide map (Bardstown, or the zone named with `-clonez-smoke-zone`). It walks east across the state 250 m at a time, then a kilometre south and back west. At every stop it drops three each of eight item kinds with generated meshes that nothing has read in yet, close by and in view. It walks until the generated meshes' cache ([GeneratedItemMeshes.cs](Assets/Scripts/World/GeneratedItemMeshes.cs)) has read in three times its budget, then checks that:
  * every item dropped at a stop is drawn there, and every kind dropped has a near map and a near material in memory;
  * the cache never holds more than its budget at a stop, unless all it holds is in use, and it lets go of the items drawn least recently;
  * the generated meshes, maps and materials the engine has in memory, which the profiler finds and sizes, are no higher over the walk's second half than over its first;
  * a generated item held in a character's hand stays all the way, though nothing else draws it;
  * the first stop's items, let go of on the way, are read in and drawn again on the way back;
  * with the budget squeezed to nothing, everything idle goes, the hand's item with it once it's put down, while what's in view stays drawn with the same meshes.

  It logs each stop: what the cache holds, what it has read in (what it would hold without a budget), and what's in memory. With `-clonez-smoke-shots <dir>`, the numbers go to `meshcache.csv` there, with the engine's texture, graphics-driver and total memory too. `-clonez-smoke-budget <MB>` walks with another budget. The renderer needs a graphics device, so leave out `-nographics`.
* **Horde** ([SmokeTest.Horde.cs](Assets/Scripts/Dev/SmokeTest.Horde.cs)): deploys into the procedural town (or the zone named with `-clonez-smoke-zone`), sets the clock to noon and raises 200 zombies on the NavMesh round the survivor, most of them in front (the worst case, since those are all in view), to mill about. It checks that:
  * every solid collider the world was built with is on the Sight Occluder layer ([SightOccluders.cs](Assets/Scripts/World/SightOccluders.cs)), not Default (the streamed map goes on fitting bits to its pieces, so there it only logs what's left for the sweep);
  * at 60, 30 and 20 frames a second (a fixed frame time, so a fast machine can't hide a slow one), with the horde kept at 200 and the survivor stepping back halfway through each, no zombie in view goes more than 0.2 s between visibility checks ([ZombieVisibility.cs](Assets/Scripts/Rendering/ZombieVisibility.cs));
  * rays from the eye all round, at both spots, are blocked by the Sight Occluder layer wherever the whole world (the survivor's own body aside) blocked them;
  * once the dead stand still, each is shown exactly when the sight rule says it can be seen;
  * nothing solid within the survivor's sight is left on Default.

  It logs each frame rate's checks and rays a frame, the ticks' CPU time, and how long the old round-robin would have left each zombie. With `-clonez-smoke-shots <dir>`, the frame-by-frame numbers go to `horde.csv` there. It runs with or without `-nographics`.
* **Cutaway** ([SmokeTest.Cutaway.cs](Assets/Scripts/Dev/SmokeTest.Cutaway.cs)): deploys into the statewide map (Bardstown, or the zone named with `-clonez-smoke-zone`) and finds the densest block in the start town. It tries the survivor at every spot of a 4 m grid over the block and keeps the spot indoors and the spot outdoors where the isometric cutaway hides the most. Cut walls, roofs and partitions stop drawing but still cast shadows ([IsoCutaway.cs](Assets/Scripts/Camera/IsoCutaway.cs), and the shells of [MapCutaway.cs](Assets/Scripts/World/Map/MapCutaway.cs)). At each spot, at 23:00 and at noon, it logs:
  * what the cutaways hide that still casts shadows: the renderers and triangles, how many of them are away from the building the survivor is in, and how often they're drawn into the shadow views round them (the sun's cascades and the shadowed lamps' maps) out of every shadow caster's draws;
  * the frames rendered at 1920×1080 in turn three ways: as the game has them, with nothing the cutaways hide casting shadows, and with only the hidden parts of the building the survivor is in casting them. For each way it logs the frame time, the render on its own waited out on the GPU, and the camera's, the sun's shadows' and the lamps' shadows' CPU and GPU time a render, and what the last two ways save on the first.

  Then it walks east across the state, 160 m at a stop, standing at a few spots at each so the cutaway's rays meet the walls and furniture there. It fails if the cutaway still remembers a collider of a piece, interior or window a frame after that was let go of. It logs what the cutaway remembers at each stop, what it has forgotten, and the longest a frame spent forgetting. With `-clonez-smoke-shots <dir>`, it photographs each spot each way and writes the numbers to `cutaway.csv` and `cutaway-cache.csv` there. With `-clonez-smoke-zone intro`, it profiles the procedural town instead, where the cutaway cuts the houses' walls itself (no walk there: nothing is let go of). It renders, so leave out `-nographics`.
* **Attachments** ([SmokeTest.Attachments.cs](Assets/Scripts/Dev/SmokeTest.Attachments.cs)): deploys into a raid, sets the clock to 23:00 and stands a target board a few metres off down the clearest line. Then it kits out a pistol, an AK-74, the shotgun, the PP-9 and the Mosin in turn, fitting each attachment with the inventory's own verb ([WeaponItems.cs](Assets/Scripts/Weapons/WeaponItems.cs)). Keys and the mouse go through its own keyboard and mouse, as the wave run's do. It checks that:
  * every attachment is built onto the gun in hand ([WeaponAttachments.cs](Assets/Scripts/Weapons/WeaponAttachments.cs)) and touches it; a red dot or a scope rises above the gun where it sits, a suppressor or long barrel lengthens it, and a suppressor goes on the end of a long barrel; a foregrip moves the off hand onto it;
  * every look of a gun's generated model is held muzzle first with the kit seated on it ([GunShape.cs](Assets/Scripts/Weapons/GunShape.cs) turns round one made end for end);
  * first person hides the attachments with the body, and the shoulder view shows them again;
  * L switches the weapon light ([WeaponTactical.cs](Assets/Scripts/Weapons/WeaponTactical.cs)), its beam leaves the lens along the barrel, and the frame is brighter where the beam lands than with it off; aiming down the sights swings the beam onto the shot's line;
  * the light runs its battery flat and goes out, won't come on flat, and comes back with a battery; swapped for a laser, it goes back into the pack with its charge and the gun keeps none;
  * the laser's dot lands where the barrel points, sits where the round will go once aimed, and L puts it out;
  * the floating-geometry report flags nothing of the gun's kit, light or laser.

  It logs how bright the light's spot is, off and on. With `-clonez-smoke-shots <dir>`, it photographs each gun from either side, the scene with the light or laser on, and the view through the bodycam. It measures the light in a rendered frame, so leave out `-nographics`.
* **Guns** ([SmokeTest.Guns.cs](Assets/Scripts/Dev/SmokeTest.Guns.cs)): the period guns ([ZoneContent.Arms.cs](Assets/Scripts/Content/ZoneContent.Arms.cs): Kentucky's .38 revolver, M1911, .22 rifle, police pump, .30-06 hunting rifle, .30-30 lever-action and AR-15; the Zone's SVD, PKM and AKS-74U). First on paper, it checks that:
  * each one has its stats, rounds of its calibre, a hand-built model the right length for a handgun or a long gun ([ItemModels.Guns.cs](Assets/Scripts/World/ItemModels.Guns.cs)), a hold of its own (`GunStats.Hold`), a report and a reload of its own that the synthesiser renders ([SfxSynth.Weapons.cs](Assets/Scripts/Audio/Synth/Recipes/SfxSynth.Weapons.cs)), a trader's price and a place in the loot; the .22 is the quietest;
  * a farmhouse's gun cabinet, a gun shop's counter and a police weapons locker each list guns the other two don't ([ZoneContent.Gear.cs](Assets/Scripts/Content/ZoneContent.Gear.cs)), and turn them up when rolled; the Zone's military gun lockers hold its Soviet guns;
  * the buildings put those containers in ([MapGameplay.Loot.cs](Assets/Scripts/World/Map/MapGameplay.Loot.cs)): a farmhouse's living room starts with its gun cabinet, a gun shop's floor with its counters, a police station's storeroom with its weapons lockers, and no Kentucky building keeps the military gun locker;
  * each of the gunsmith's loyalty levels sells guns of its own ([TraderCatalog.cs](Assets/Scripts/Economy/TraderCatalog.cs)).

  Then it deploys into a raid and takes up each gun in turn, in every look it has (its generated meshes, once the asset pipeline has made them), checking that it's held muzzle first, the right fist closes on its grip and the left hand (the [hand IK](Assets/Scripts/Characters/WeaponHandIK.cs)) reaches its hold on the gun: the forend, pump or handguard, or ahead of the magazine on the SVD and PKM, whose handguards lie beyond the stance's reach (`GunHold`); a sight sits on it; it fires a round with its own report; the trigger held fires a burst from the automatics and one round from the rest; and it reloads with its own sound. With `-clonez-smoke-shots <dir>`, it lays the guns' hand-built models and generated looks out side by side and photographs them from above, and each gun in the hands from either side. It runs with or without `-nographics` (no pictures without).
* **Rounds** ([SmokeTest.Rounds.cs](Assets/Scripts/Dev/SmokeTest.Rounds.cs)): the special rounds ([ZoneContent.Arms.cs](Assets/Scripts/Content/ZoneContent.Arms.cs)). First on paper, it checks that:
  * the 5.45 tracer carries its tracer effect and the 9mm and 12g incendiaries theirs (`AmmoStats.Tracer`, `AmmoStats.Incendiary`), each card names its effect ([WeaponItems.cs](Assets/Scripts/Weapons/WeaponItems.cs)), and each is sold by a trader and found in some container;
  * no other round carries an effect, and the Cold-Snap, Heavy-Velocity and Fuel-Gel rounds are gone.

  Then it deploys into a raid, stands the town's people down and fires as a player does, through its own mouse with the cursor on the target in the isometric view. It checks that a plain 5.45 round draws no trail and a tracer round draws its streak ([CombatFX.cs](Assets/Scripts/Core/CombatFX.cs)). It also checks that a zombie stood before the gun (made tough enough to live through the round) isn't set alight by a plain 9mm round. A 9mm incendiary from the pistol and a 12g incendiary from the shotgun each set their zombie alight ([CombatResolver.cs](Assets/Scripts/Core/CombatResolver.cs), `Aflame` in [Fire.cs](Assets/Scripts/Weapons/Fire.cs)), and it burns and falls, its death counted as the gun's. With `-clonez-smoke-shots <dir>`, it photographs a tracer's streak in flight. It runs with or without `-nographics` (no pictures without).
* **Melee** ([SmokeTest.Melee.cs](Assets/Scripts/Dev/SmokeTest.Melee.cs)): melee with weight ([MeleeStats.cs](Assets/Scripts/Weapons/MeleeStats.cs)) with the kitchen knife, the baseball bat and the fire axe. First on paper, it checks that bare hands, the knife, the axe and the bat reach further in that order; that the knife's stab meets one body, the axe two and the bat's sweep three (by the arc); that the knife winds up and recovers fastest and the axe slowest; that the bat staggers most and the knife least; that the knife never knocks the dead down and the axe does most; and that each item card says how the weapon handles.

  Then it deploys into a raid, stands the town's people down and walls in a corridor 2.2 m wide ahead of the survivor (carved out of the NavMesh, so the dead keep to it). Every swing is a player's: its own mouse, the cursor on the target in the isometric view. For each weapon it checks that:
  * one swing at three of the dead (one in front, two a pace behind it) lands its wind-up after the press, and strikes as many of them as the arc carries through, the front one taking the full blow and each further one less;
  * held, the attack swings again only once the recovery is over;
  * over 16 blows at one of the dead, its blows stagger or knock it down as often as the weapon's chances say, the knife never knocks it down, and the bat and the axe do.

  Then that a crawler at the survivor's feet is stomped on the head, not swung at; that with nothing standing in a swing, it comes down on a crawler beyond a stomp's reach; that a swing passes over one on the ground while one of the dead stands in it; and that bare-handed, a shove puts one of the dead down and the attack stomps it: on its body for half while its head is out of reach, then, stood beside its head, on that in full, until it's dead before it gets up. Last, each weapon fights its way through three of the dead coming down the corridor, with the attack held while the nearest is in reach: it must clear the corridor, with no swing striking more bodies than its arc allows. It logs each weapon's profile, its blows' outcomes, and each fight side by side: the time, the swings, the bodies a blow, staggers, knockdowns, stomps, the lunges the dead got in and the hits that got through. With `-clonez-smoke-shots <dir>`, it photographs the bat's and the axe's opening blows, one of the dead on the ground and a stomp. It runs with or without `-nographics` (no pictures without). To watch it, use **CloneZ ▸ Smoke Test ▸ Watch Melee** in the editor.
* **Throwables** ([SmokeTest.Throwables.cs](Assets/Scripts/Dev/SmokeTest.Throwables.cs)): deploys into the procedural town (or the zone named with `-clonez-smoke-zone`), stands the town's people down (they come to a din and shoot the dead) and picks a house near the survivor: an outside door with the NavMesh on both sides and a clear lob from its step out into the street. It checks that:
  * a molotov is crafted in the field from an empty bottle, a rag and a litre out of a gas can, and a pipe bomb from scrap, chemicals and duct tape ([Hideout.cs](Assets/Scripts/Economy/Hideout.cs)); the pipe bomb, thrown, goes off where it came down;
  * with the survivor shut in the house, ten of the dead set on them from outside and the survivor gone quiet (the horde keeps at the house), an alarm clock tossed out of the door ([Throwables.cs](Assets/Scripts/Weapons/Throwables.cs), [NoiseLure.cs](Assets/Scripts/Weapons/NoiseLure.cs)) comes down where it was aimed and rings, every one of the dead drops the chase within 3 s of the ring, and most of them leave the house for the clock; once it has rung out the clock lies there and is picked up again;
  * firecrackers bang, call the dead from 40 m and are spent; the timer radio comes on and calls the dead;
  * a molotov thrown into the doorway as six fresh dead come through it bursts into a fire there ([Fire.cs](Assets/Scripts/Weapons/Fire.cs)) that spreads, and the dead catch fire and fall to it.

  It logs how far the horde stands from the door and from the clock every 5 s of the ring, and each of the doorway's dead as it falls, and to what. It runs with or without `-nographics`.
* **Wave:** plays Wave Survival through its own controls ([SmokeTest.Wave.cs](Assets/Scripts/Dev/SmokeTest.Wave.cs)). Keys and the mouse go through the Input System on a keyboard and a mouse of its own ([SmokeTest.Drive.cs](Assets/Scripts/Dev/SmokeTest.Drive.cs)), and UI controls get the pointer clicks the EventSystem sends. Every press is a check. From the title's **New run**, it covers:
  * walking (WASD and the arrows), sprinting, sneaking, firing, reloading, shoving, the knife and bare hands;
  * the four camera views (V), with turning, zoom, the aim line, the shoulder swap, aiming down the sights and holding a breath;
  * F1, M and F3; the detector (O), a bolt (X) and a grenade (`), plus the keys that do nothing in a lone wave run;
  * the flashlight (L) and night vision (N);
  * build mode: every piece key, the wheel, building a barricade and demolishing it;
  * farming (F), water (G), sleep (T, then waking with T or Space), a door (E), the supply crate (E and R), and dropping, inspecting (F) and picking up an item;
  * every action of one fixture of each kind in the home and the yard, chosen by number key or by clicking its row in the action menu (Q);
  * every control of the health and skills panels, the inventory (alone and with the crate open), the field menu (C), the map (P), the journal (J), the HUD, the pause menu, and the settings and field-manual screens;
  * two waves cut down, F5, **Save & quit to menu**, **Continue** (the wave and kills restored), and a death: the game-over screen, the save deleted, and R back to the title.

  It plays on its own save file (`smoke_wave_save.db`) and restores the settings its presses change. It runs with or without `-nographics`.

  A batch run has no window. To watch the run, use **CloneZ ▸ Smoke Test ▸ Watch Wave Survival** in the editor: it plays in the Game view with a pause between presses. Its own keyboard and mouse drive the game, and the real ones are switched off until the run ends. When it's done, play mode stops and the `[Smoke]` lines are in the Console. Stopping play mode ends the run early, and the editor gets its own save, profile, settings and input back either way. Scripts changed during the run are reloaded once it ends, not mid-run. **Run Wave Survival (full speed)** skips the pauses. From the command line, `-clonez-smoke-pace <seconds>` adds the same pause, for a visible editor or a built player.

Any error or exception the game logs fails the run.

The lobby and wave runs also fail on floating geometry ([SmokeTest.Floating.cs](Assets/Scripts/Dev/SmokeTest.Floating.cs)). They run the floating-geometry report ([Debug Mode](#debug-mode)) on the world as played: the lobby in the town once the dead roam it and again in the hub on its own; the waves after two waves are cut down and again after **Continue** rebuilds the run. Anything the report flags fails the run unless it's on the list of known false positives (`KnownFloating`, empty for now). The report goes to `Logs/FloatingGeometry-<mode>-<where>.txt`, or to the `-clonez-smoke-shots` folder with a picture of each kind of thing flagged. `-clonez-smoke-floating-prop` (`-FloatingProp` in Smoke.ps1) hangs a test prop in the air beside the survivor at each check, so the run must fail. The report runs in the editor only.

    Unity.exe -batchmode -nographics -projectPath <project> -executeMethod CloneZ.EditorTools.CloneZBatch.SmokePlay -clonez-smoke solo -logFile smoke.log
    Unity.exe -batchmode -projectPath <project> -executeMethod CloneZ.EditorTools.CloneZBatch.SmokePlay -clonez-smoke wave -logFile smoke-wave.log

Results are the `[Smoke]` lines in the log, and the editor exits with 0 when everything passes. If the editor already has the project open, point the batch run at a copy of the project. Delete the copy's `Library/ilpp.pid` and `Library/burst.pid` before you start it. Otherwise the copy's editor stops the processes they name, which are the open editor's own helpers.

[Tools/Smoke/Smoke.ps1](Tools/Smoke/Smoke.ps1) does the same in one step for the single-player modes. It finds the project's editor version through the Unity Hub, refuses to start while the project is open, and prints the summary and anything that failed or was skipped:

    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 [-Mode wave|solo|hunter|lobby|nightwalk|grounditems|meshcache|horde|cutaway|attachments|guns|rounds|melee|throwables] [-Zone <zone>] [-Shots <dir>] [-NoGraphics] [-Threaded] [-FloatingProp]

A batch editor renders on the main thread and, having no Game view, draws no camera to the screen. `-Threaded` passes `-force-gfx-mt -clonez-smoke-view 1920x1080`: the editor gets a render thread, and the screen's cameras are drawn at that size every frame, as the Game view would draw them ([SmokeTest.Ring.cs](Assets/Scripts/Dev/SmokeTest.Ring.cs)). Any mode then also fails if the main thread ever had to wait for room in the render thread's command ring. That's what Unity's "Ran out of Graphics Ring Buffer space" counts: it logs on every 301st wait, and never in a batch editor. In the editor and development builds, [CommandRingWatch.cs](Assets/Scripts/Dev/CommandRingWatch.cs) logs those waits as `[CloneZ] Render command ring full`, with the frame and what it drew.

### Debug Mode

[DebugMode.cs](Assets/Scripts/Dev/DebugMode.cs) names whatever is on screen. It runs in the editor and in development builds. Press **F8** in any mode to turn it on or off. While it's on, gameplay input is blocked, the cursor is free and the camera holds still.

Point at anything and the panel lists what is drawn under the cursor, nearest first. For each thing it shows the object's path, renderer type, material, submesh and the exact world point under the cursor. It finds things whether or not they have a collider or a readable mesh, and it finds the ground items, which are drawn instanced and have no objects of their own. It works by drawing every renderer the camera draws again, flat in an ID colour, into a small patch round the cursor, together with each pixel's world position ([DebugPick.shader](Assets/Resources/DebugPick.shader)). A point that lies outside its renderer's own bounds is flagged. If nothing is listed under something you can see, it isn't a renderer at all: it is UI, a gizmo or post-processing.

* **Click** locks the nearest thing. In the editor it is selected in the Hierarchy, and it's always logged and copied to the clipboard. **Wheel** steps through everything under that spot and **right-click** lets go.
* **Delete** hides the locked thing and **Backspace** shows everything again.
* **1–6** hide a whole category: characters, the world, particles, lines, ground items, everything else.
* **F9** freezes time, **F10** writes the floating-geometry report and **F11** draws the bounds of everything in view.

The floating-geometry report ([FloatingGeometryReport.cs](Assets/Scripts/Dev/FloatingGeometryReport.cs), editor only, also at **CloneZ ▸ Debug ▸ Report Floating Geometry**) writes `Logs/FloatingGeometry.txt`, a capture of the Game view and close-ups of the nearest zombies. It flags:

* every character's body baked as it's posed, with any vertices hanging away from the body;
* bones that have wandered off;
* parts drawn under a character (gear, a held item) that are away from it, or don't touch its posed body, directly or through the parts they touch;
* anything drawn in the world that, with everything it touches, touches nothing that reaches the ground ([FloatingGeometryReport.Adrift.cs](Assets/Scripts/Dev/FloatingGeometryReport.Adrift.cs)). Two pieces touch when their bounds come within 5 cm of each other. When one is over 3 m across and the other isn't, the big one's triangles decide, since its bounds don't show where its surfaces are. A collider within 5 cm holds a piece up if it reaches the ground or touches a piece that does.

For the eye only, it also lists anything drawn wholly above the rooftops, line renderers and every scene's root objects. It ends with the names of everything it flagged, as the smoke test's list of known false positives matches them.
