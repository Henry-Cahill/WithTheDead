# CLONE-Z — Mechanics coverage

This maps every row of *Survival Shooter Mechanics Breakdown* (Oct 2026) to where it lives in the game. Paths are relative to the repo root. Systems wire themselves in as `IGameModule`s ([GameModules.cs](Assets/Scripts/Core/GameModules.cs)), so each row names the module or class to start from. The answers to the doc's design tensions are in [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md).

## Roadside Picnic: book to mechanic

| Book element | In CLONE-Z | Where |
| --- | --- | --- |
| The Visitation (several Zones, never explained) | Several Zones, each with its own map, depth, loot multiplier, mutants, cordon and special rule. The cause is never explained (in-game it's "the Passing"). | [ZoneCatalog.cs](Assets/Scripts/Zone/ZoneCatalog.cs), [ZoneWorld.cs](Assets/Scripts/World/Zone/ZoneWorld.cs) |
| Litter, not treasure | Artifacts have hidden properties, learned by wearing them on the belt or paying the Institute. Knowledge is kept per account and sold as rumours. | [Artifacts.cs](Assets/Scripts/Zone/Artifacts.cs), [ArtifactCatalog.cs](Assets/Scripts/Zone/ArtifactCatalog.cs), `RumoursScreen` in [ServiceScreens.cs](Assets/Scripts/Hub/Screens/ServiceScreens.cs) |
| Bolts | Throw a bolt (X) with its own feel and sound. Anomalies react to the bolt first. | [Bolts.cs](Assets/Scripts/Zone/Bolts.cs) |
| Anomalies | Five types with consistent but alien rules (sinkhole, embermoss, staticbloom, greysyrup, tollgate). A rule is learned by surviving it. | [Anomalies.cs](Assets/Scripts/Zone/Anomalies.cs) |
| Illegal entry | A cordon with a restricted belt where the Wardens shoot. Pay for a pass, bribe the gate or risk it. Two economies: the Institute (legal, fair) and the fence (rich, risky, raises your Heat). | [ZoneCordon.cs](Assets/Scripts/World/Zone/ZoneCordon.cs), `CordonRules` in [Factions.cs](Assets/Scripts/Core/Factions.cs), [TraderCatalog.cs](Assets/Scripts/Economy/TraderCatalog.cs) |
| The bar and the fences | The hub is a bar full of people with buyers, rumours, music and stories. | [HubWorld.cs](Assets/Scripts/Hub/HubWorld.cs), [HubNpc.cs](Assets/Scripts/Hub/HubNpc.cs), [TradeScreens.cs](Assets/Scripts/Hub/Screens/TradeScreens.cs) |
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
| Ballistics + armour | [Ballistics.cs](Assets/Scripts/Weapons/Ballistics.cs), `CombatResolver` in [CombatResolver.cs](Assets/Scripts/Core/CombatResolver.cs) |
| Weapon modding | `ModStats` in [Ballistics.cs](Assets/Scripts/Weapons/Ballistics.cs), [WeaponItems.cs](Assets/Scripts/Weapons/WeaponItems.cs), the tablet's Gun page in [Gadgets.cs](Assets/Scripts/Gadgets/Gadgets.cs) |
| Grid inventory, weight drains stamina | [InventoryLayout.cs](Assets/Scripts/Items/InventoryLayout.cs), [InventoryUIController.Grid.cs](Assets/Scripts/UI/InventoryUIController.Grid.cs) |
| Use-based skills + level | [PlayerSkills.cs](Assets/Scripts/Player/PlayerSkills.cs), [Progression.cs](Assets/Scripts/Profile/Progression.cs) |
| Wipes | `ProfileStore.Wipe` in [ProfileStore.cs](Assets/Scripts/Profile/ProfileStore.cs), `SandboxSettings.SeasonLengthDays` |
| Sound (positional footsteps, doors, gear) | [AudioDirector.cs](Assets/Scripts/Audio/AudioDirector.cs), [SfxSynth.cs](Assets/Scripts/Audio/SfxSynth.cs) |

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
| Infection (lethal to off) | `SandboxSettings.Infection`, [PlayerStats.cs](Assets/Scripts/Player/PlayerStats.cs) |
| Wound care (bandage, disinfect, stitch, shards, splint) | [PlayerWounds.cs](Assets/Scripts/Player/PlayerWounds.cs), [MedicalItems.cs](Assets/Scripts/Medical/MedicalItems.cs) |
| Utility shutoff | [WaterMains.cs](Assets/Scripts/Core/WaterMains.cs), [PowerGrid.cs](Assets/Scripts/Core/PowerGrid.cs) |
| Building + barricades | [PlayerBuilder.cs](Assets/Scripts/Player/PlayerBuilder.cs), [Barricade.cs](Assets/Scripts/World/Barricade.cs), [Camps.cs](Assets/Scripts/World/Camps.cs) |
| Food systems (farming, foraging, fishing, trapping, cooking, spoilage) | [FarmPlot.cs](Assets/Scripts/World/FarmPlot.cs), [FieldSurvival.cs](Assets/Scripts/Survival/FieldSurvival.cs), `ItemDef.Perishable` |
| Vehicles (keys or hotwiring, part condition, fuel, Mechanics) | [Vehicles.cs](Assets/Scripts/World/Vehicles.cs) (engine, tyres and battery wear) |
| Sandbox settings | [SandboxSettings.cs](Assets/Scripts/Core/SandboxSettings.cs), `SandboxScreen` |
| Build 42 additions (workstations, animals, darker nights) | [Hideout.cs](Assets/Scripts/Economy/Hideout.cs) (animal pen), [VisionController.cs](Assets/Scripts/Rendering/VisionController.cs), `NightDarkness` |
| A real place for a map: Kentucky's streets and buildings in 256 m cells, buildings with typed rooms and loot by use (the structure only; the data is OpenStreetMap's, see [Tools/World](Tools/World/README.md)) | [KentuckyMap.cs](Assets/Scripts/World/Map/KentuckyMap.cs), [WorldStore.cs](Assets/Scripts/World/Map/WorldStore.cs), the Bardstown zone in [ZoneCatalog.cs](Assets/Scripts/Zone/ZoneCatalog.cs) |

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

| Doc | CLONE-Z |
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

[SmokeTest.cs](Assets/Scripts/Dev/SmokeTest.cs) plays the game end to end. It stays inert unless the game or a batch-mode editor is started with `-clonez-smoke solo|hunter|host|client`.

* **Solo:** makes a throwaway character with its own save file, walks into the bar, and opens every hub screen. Then it deploys into Kestrel Flats, or the zone named with `-clonez-smoke-zone`, and checks:
  * the tablet;
  * a grenade;
  * the FPV and ground drones;
  * a parked car, repaired and driven;
  * a camp gate blown by a breaching charge;
  * cuffs and rope;
  * the Central Economy;
  * a quest.
* **Hunter:** plays a raid as a monster.
* **Host and client:** play a networked raid together on port 7791.

Any error or exception the game logs fails the run.

    Unity.exe -batchmode -nographics -projectPath <project> -executeMethod CloneZ.EditorTools.CloneZBatch.SmokePlay -clonez-smoke solo -logFile smoke.log

Results are the `[Smoke]` lines in the log, and the editor exits with 0 when everything passes. If the editor already has the project open, point the batch run at a copy of the project.
