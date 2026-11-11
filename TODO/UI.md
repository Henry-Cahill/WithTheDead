# UI — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** the title screen, HUD and moodles, inventory screens, hub screens, settings, input and controls,
accessibility, localization readiness and UI performance.
**Code:** [Assets/Scripts/UI](../Assets/Scripts/UI) (built on [UIKit.cs](../Assets/Scripts/UI/UIKit.cs) and
[UITheme.cs](../Assets/Scripts/UI/UITheme.cs)), [Assets/Scripts/Hub/Screens](../Assets/Scripts/Hub/Screens),
[GameInput.cs](../Assets/Scripts/Core/GameInput.cs), `GameSettings` in [SandboxSettings.cs](../Assets/Scripts/Core/SandboxSettings.cs#L158).

## Where it stands

- uGUI built from code: no prefabs and no UI Toolkit. Every canvas scales from a 1920×1080 reference, matched halfway
  between width and height ([UIKit.cs](../Assets/Scripts/UI/UIKit.cs#L45)).
- A grid and a list inventory, a moodle HUD in four styles, the hub screens, a pause menu, the field manual, F1 key
  hints, subtitles and a loading card.
- The Settings screen ([HomeScreens.cs](../Assets/Scripts/Hub/Screens/HomeScreens.cs#L258)) has picture effects, field
  of view, HUD style, crosshair, subtitles, the five audio buses (each applied by `SfxMixer`), microphone and mouse
  sensitivity, and **Shadows** (the shadow budget's quality, `GameSettings.ShadowQuality`). It has nothing for the
  display: no resolution, window mode, vsync, frame cap or quality level, and no code in the game sets any of them.
- Input is the Input System with one map per camera view, keyboard and mouse only
  ([GameInput.cs](../Assets/Scripts/Core/GameInput.cs#L49)). Only the pause menu reads a gamepad
  ([PauseMenuController.cs](../Assets/Scripts/UI/PauseMenuController.cs#L359)).
- A theme rework is under way and uncommitted: `MainMenuController.cs` (+209 lines), `UIKit.cs` (+105), `UITheme.cs`
  (+66) and the new title-screen radio, `MenuRadio.cs`.

## P1 — next up

- [ ] **Finish the theme rework and commit it** · P1 · M · Polish
  - **Why:** The new theme is half-applied. The title screen still declares its own colours (`TitleRed`, `CardColor`,
    `CardActive`… [MainMenuController.cs](../Assets/Scripts/UI/MainMenuController.cs#L24)), and other screens use
    literal colours: the skills panel ([SkillsPanelController.cs](../Assets/Scripts/UI/SkillsPanelController.cs#L161))
    and the grid inventory ([InventoryUIController.Grid.cs](../Assets/Scripts/UI/InventoryUIController.Grid.cs#L708)).
    The work only exists on this PC's disk.
  - **Do:** Move every screen onto `UITheme` colours and `UIKit` helpers, then check contrast on the darkest HUD style.
    Commit it along with the rest of the in-progress work (see the [index](README.md#cross-cutting)).
  - **Done when:** A search for `new Color(` in `Assets/Scripts/UI` and `Hub/Screens` finds only theme definitions,
    and the work is on `main`.

- [ ] **Display and graphics settings** · P1 · M · Settings
  - **Why:** Players can't pick a resolution, window mode (fullscreen, borderless, windowed), vsync, frame cap or
    quality level: nothing calls `Screen.SetResolution`, `Screen.fullScreenMode`, `QualitySettings.vSyncCount`,
    `QualitySettings.SetQualityLevel` or `Application.targetFrameRate`. On a laptop or a 144 Hz monitor that's the first
    thing people look for.
  - **Do:** Add a **Display** section to Settings: window mode, resolution (from `Screen.resolutions`), vsync, frame
    cap, render scale and a preset, and move **Shadows** there from the picture card (`GameSettings.ShadowQuality`, which
    drives the shadow budget in [Rendering.md](Rendering.md); already applied at once and saved). Store them in
    `GameSettings`, apply them at boot and on change, and have a "keep these settings?" countdown for
    resolution changes.
  - **Done when:** Every option applies at once, survives a restart, and a bad resolution reverts by itself.

- [ ] **A UI scale and text-size setting** · P1 · M · Accessibility
  - **Why:** Every canvas uses the fixed 1920×1080 reference ([UIKit.cs](../Assets/Scripts/UI/UIKit.cs#L45)) and
    font sizes are hard-coded per control. At 1280×720 small text is hard to read, and fixed 1200 to 1500 px title
    widths on the title screen risk clipping
    ([MainMenuController.cs](../Assets/Scripts/UI/MainMenuController.cs#L147)). On ultrawide everything sits in a
    narrow middle.
  - **Do:** Feed a user scale into the shared canvas scaler, and give UIKit a text-size multiplier. Then check every
    screen at 1280×720, 1920×1080, 2560×1440 and 3440×1440.
  - **Done when:** All screens are readable and unclipped at those sizes with scale at 80% to 130%.

- [ ] **A hotbar and favourites** · P1 · M · QoL
  - **Why:** [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md) H: the context menu and **Split**
    ([InventoryUIController.GridDrag.cs](../Assets/Scripts/UI/InventoryUIController.GridDrag.cs#L930)) shipped, but the
    1–5 hotbar and favourite-lock never did (no code mentions either). Under pressure, opening the inventory to swap to
    a bandage or the knife costs lives.
  - **Do:** Add five quick slots on the HUD with drag-from-inventory to assign. Bind them to 1–5 outside menus, but
    check for clashes first: number keys already pick rows in the Q action menu and pieces in build mode. Add a
    favourite flag that Drop, Transfer all and Tidy up skip. Save both with the character.
  - **Done when:** Items can be pinned to 1–5 and used without opening the inventory, and favourites survive Transfer
    all.

- [ ] **Gamepad support** · P1 · L · Input
  - **Why:** Every gameplay action is bound to keys or the mouse only
    ([GameInput.cs](../Assets/Scripts/Core/GameInput.cs#L49)). The hub and inventory are hover-first: tooltips need a
    pointer ([HubTooltip.cs](../Assets/Scripts/Hub/Screens/HubTooltip.cs#L159)). Big-screen and Steam Deck play is out.
  - **Do:** Add gamepad bindings to each view's map (Common, Isometric, Shoulder, FirstPerson), with isometric aiming
    on the right stick. Give screens a focus model (selectables in order, submit, cancel) and show tooltips on focus.
    Key hints should show pad glyphs when a pad was used last.
  - **Done when:** A full raid, from the title screen to extraction and selling, can be played on a pad.

- [ ] **Key rebinding** · P1 · M · Input
  - **Why:** Bindings are fixed in code, and `KeyName` shows the first one
    ([GameInput.cs](../Assets/Scripts/Core/GameInput.cs#L116)). AZERTY players, left-handers, and anyone who needs a
    key moved can't change them.
  - **Do:** Add a Controls page built on `PerformInteractiveRebinding`. Save the overrides as JSON in `GameSettings`
    and load them at boot. Key hints and the field manual should read the live bindings.
  - **Done when:** A rebound key works in every view, shows in the hints, and survives a restart.

## P2 — soon

- [ ] **Don't lose unsaved settings** · P2 · S · Bug
  - **Why:** Settings change live but reach disk only through the **Save** button
    ([HomeScreens.cs](../Assets/Scripts/Hub/Screens/HomeScreens.cs#L294)). Leave the screen or quit, and they're gone
    next launch. The pause menu's HUD switch saves straight away
    ([PauseMenuController.cs](../Assets/Scripts/UI/PauseMenuController.cs#L340)), so the two behave differently.
  - **Do:** Save when the screen closes (or debounce saves on change), and drop the button or keep it as a reassurance.
  - **Done when:** Changing a slider and quitting keeps the change.

- [ ] **One back/cancel path for every screen** · P2 · M · UX
  - **Why:** The pause menu owns Esc and tracks the current screen
    ([PauseMenuController.cs](../Assets/Scripts/UI/PauseMenuController.cs#L134)). Other screens close in their own ways,
    so Esc does different things in the inventory, hub, field manual and settings.
  - **Do:** Add a shared Cancel action (Esc, and B on a pad) that closes the topmost screen. Pause opens only when
    nothing else is open.
  - **Done when:** Esc always closes exactly one thing, the top one.

- [ ] **Meet the accessibility floor the design promises** · P2 · M · Accessibility
  - **Why:** DESIGN_DECISIONS.md tension 12 promises "an accessibility floor". Brightness, blur, distortion, shake,
    FOV, HUD styles, crosshair, subtitles and fog readability are in. Missing: colour-blind-safe moodle and condition
    colours, subtitle size and background, text size (above), and hold-or-toggle for sprint, aim and sneak.
  - **Do:** Add those settings, and run the HUD through a colour-blindness simulator.
  - **Done when:** Moodles and condition bars stay readable under deuteranopia and protanopia, and subtitles have size
    and background options.

- [ ] **Stop rebuilding HUD text every frame** · P2 · M · Perf
  - **Why:** `HUDController.Update` ticks every widget every frame
    ([HUDController.cs](../Assets/Scripts/UI/HUDController.cs#L158)). The inventory and skills panels also rebuild
    strings in `Update` ([InventoryUIController.cs](../Assets/Scripts/UI/InventoryUIController.cs#L576)). Each text
    change dirties its canvas and allocates.
  - **Do:** Update a widget only when its value changes (cache the last value or subscribe to the event), and split
    fast-changing widgets onto their own canvas.
  - **Done when:** The profiler shows no per-frame GC allocations from the HUD during a quiet raid.

## P3 — nice to have

- [ ] **Groundwork for localization** · P3 · L · Tech debt — there is no string layer. Hundreds of English literals sit
  in UI code (for example [InventoryUIController.GridDrag.cs](../Assets/Scripts/UI/InventoryUIController.GridDrag.cs#L984)
  and the Settings screen), plus item names in the catalogue. Only worth it if the game goes beyond English speakers.
  Start by routing new strings through one `Tr("key")` helper. **Done when:** new UI code uses keys and a pseudo-locale
  shows what's left.
- [ ] **A loading card that shows it's alive** · P3 · S · Polish — without a known percentage, the card shows just the
  stage name ([LoadingScreen.cs](../Assets/Scripts/UI/LoadingScreen.cs#L59)). The Kettle's town build can take a
  while. Add a spinner, plus a failure state with "back to the title". **Done when:** a stalled load is visibly alive or
  visibly failed.
- [ ] **Tooltips that work without a mouse and stay on screen** · P3 · S · Polish — tooltips follow pointer events and
  sit next to the cursor ([InventoryUIController.cs](../Assets/Scripts/UI/InventoryUIController.cs#L148),
  [HubTooltip.cs](../Assets/Scripts/Hub/Screens/HubTooltip.cs#L159)). Show them on focus, and clamp them to the screen.
  **Done when:** tooltips are fully visible at every edge and reachable by pad.
- [ ] **Say whose settings are whose in co-op** · P3 · S · Co-op — the sandbox rules belong to the host ("Only the host
  can change these", [HomeScreens.cs](../Assets/Scripts/Hub/Screens/HomeScreens.cs#L360)), while picture and audio are
  per machine. Label each group, and show a joined player the host's rules. **Done when:** a client can see the rules
  of the server they're on.
- [ ] **A microphone picker and voice test** · P3 · S · Gap — `GameSettings` has a `MicrophoneDevice` field
  ([SandboxSettings.cs](../Assets/Scripts/Core/SandboxSettings.cs#L177)) that no screen exposes. Add a device dropdown
  and a level meter, for proximity voice (`VoiceCapture` in [NetSocial.cs](../Assets/Scripts/Net/NetSocial.cs)).
  **Done when:** a player can choose a mic and see it working before a raid.

## Night Shift leftovers

From [tasks.md](../Tools/NightShift/tasks.md) (search for the text):

- [ ] Comments explaining `HudRaidOverlay.cs`: failed (tasks.md line 11867).
- [ ] Comments explaining `SkillsPanelController.cs`: failed (tasks.md line 12439).
- [ ] A "peek inside" action on closed storage (`StorageFixture.cs`): failed (tasks.md line 6520). It's a UI-ish
  convenience, best done as a tooltip on the storage prompt rather than an extra action.
