# Lessons for the night shift

Every night-shift agent reads this first. When an agent learns something a future night would need, it
appends one short bullet at the end. Keep each bullet to a line or two.

## How CLONE-Z is put together
- Everything is built from code at runtime: `GameBootstrapper` (Assets/Scripts/Bootstrap) creates the world, the player, the camera, the NavMesh and the UI when Play starts. There are almost no prefabs or hand-built scenes; never edit `.unity` scenes.
- New features plug in through `IGameModule` (Core/GameModules.cs): a class with a parameterless constructor, an `Order` and `Init()`, found by reflection at boot. Use it to register item actions, subscribe to `GameEvents` / `WorldHooks` or add HUD widgets, instead of editing `GameBootstrapper`.
- Per-player systems are MonoBehaviours added to the player root in `GameBootstrapper` (the block of `root.AddComponent<Player...>()` lines, each with a short comment). Add a line there only for a new per-player component.
- Anything the survivor can use in the world (tap, door, cupboard, lamp, chair) is an `Interactable` (World/Interaction/Interactable.cs): implement `DisplayName` and `GetActions(ctx, into)`, and `PlayerInteractor` shows "[E] main action  [Q] more". Persist state with `SaveKey` (built with `Interactable.KeyFor`), `CaptureState` and `RestoreState`. Fixtures live in World/Fixtures.
- Interfaces worth knowing: `IDamageable`, `IZombieAttackable`, `IExplosionTarget`, `IDemolishable`, `IWaterPoint`, `IWarmthSource`, `IStatModSource`, `IContentProvider`, `ISfxBackend`.
- Items, loot tables and crops are a catalogue in SQLite (`ItemDatabase`, Core/Sqlite). Item models are code-built descriptions in World/ItemModels.cs (10,000+ lines: grep it, never read it whole) plus generated JSON in Assets/Resources/GeneratedItemModels (don't touch those).
- Multiplayer is Netcode for GameObjects with named messages and no networked prefabs (Assets/Scripts/Net). The host is authoritative.
- The real-world map (Assets/Scripts/World/Map): `KentuckyMap` builds the raid map for a zone with a `RealPlace` (Bardstown) from OpenStreetMap data that Tools/World writes to the NAS. `MapData.cs` mirrors that data's JSON, so never rename or retype its fields, and don't change `WorldStore`'s paths. check.sh can't see the data, so reason from the code: coordinates are metres from the place's centre (x east, z north), footprints run anticlockwise seen from above, and `RaidMesh.Tri` draws a face only when its corners run clockwise as seen. Ground-floor walls and props go in `site.Town.Out` with a `PropCollider`; upper storeys, ceilings and roofs in `site.Overhead`; anything past the playable square in `site.Town.Far`, without colliders. Mark what you place with `site.Take`, and check `site.Free` before placing a prop.

## Code conventions
- Game code is in `namespace CloneZ` (editor tools in `CloneZ.EditorTools`). Big classes are split into partial files such as `FurnitureBuilder.Kitchen.cs`.
- UI is uGUI (`UnityEngine.UI`) built from code; there's no UI Toolkit or OnGUI. Input uses the new Input System (`UnityEngine.InputSystem`); the old `UnityEngine.Input` class is not used.
- Unity 6 APIs: `FindObjectsByType` / `FindAnyObjectByType`, `Rigidbody.linearVelocity`. The language is C# 9: no file-scoped namespaces, global usings or other C# 10+ features.
- Public types and non-trivial members have a `/// <summary>` that explains why, in plain English. Keep that style.
- Files are UTF-8 and comments use typographic dashes and quotes; keep them intact.
- DESIGN_DECISIONS.md and MECHANICS_COVERAGE.md (repo root) explain the design and where each mechanic lives. Update MECHANICS_COVERAGE.md when you add a mechanic.

## Night shift practicalities
- `bash Tools/NightShift/check.sh` compiles Assets/ against Unity 6000.7's assemblies. It can't run the game, so reason carefully about runtime behaviour: null checks, initialisation order, and which GameObject a component lives on.
- `.meta` files for new scripts and folders are written automatically; don't create them.

## Learned on earlier nights
- Partial-class header comments: when a `partial` file has no class-level doc, put a wrapped `//` block what it builds right after the `namespace {` and before the `partial class` line (see HomeFurniture.Kitchen.cs); the primary file keeps the `/// <summary>`. Reference the sibling partials (e.g. "the exit roads are in RaidMapBuilder.Town.cs") rather than restate them.
- `CloneZConfig` partial files (CloneZConfig.Bathroom.cs etc.) carry a one-line `// ...` header above the `public static partial class` line describing what the file's settings tune (see HomeFurniture.Kitchen.cs); the settings themselves are grouped under `// ---- section ----` comments inside.
- When adding random‑based UI elements, remember to include `using System;` for `System.Random`.
- Cache singleton lookups like WeatherSystem.Instance in frequently‑called Update loops to avoid per‑frame FindObjectByType overhead.
- UI fade logic now disables the MonoBehaviour when idle, reducing per‑frame overhead.
- Cache per‑client temporary collections in `NetWorld.SendAi` (static `s_Visible` and `s_Keep`) to avoid allocating a new List and HashSet each tick, reducing GC pressure.
- Added intermediate water‑level feedback to RaidStoreWaterHeater.Knock for richer player cues.
- When extending a string array literal, ensure the closing brace and semicolon are moved after the new entries to keep syntax correct.
- `PostFx.LateUpdate` (Presentation.cs) caches `Camera.main` in `_cachedCam` (lazy, re-resolves when it reads null after destruction); the `cam != _cachedCam` equality check was dropped — only `_cachedData == null` triggers the URP data setup now.
- Tree colliders now vary per tree using a random scale factor (±10 %) for width, height and depth, giving a hand‑painted look to the forest.
- Removed per‑actor NetAgentProxy lookup in NetWorld.SendAi to save CPU (unnecessary on server)
