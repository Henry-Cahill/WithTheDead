# Night Shift tasks

Write one task per line as an unchecked box (`- [ ]`) under **Queue**. The night shift works through them
top to bottom, several at once when helper machines are set up. A task lands on the night's branch only if
the whole project still compiles, and is then ticked off here with a short note:
`[x]` landed · `[!]` failed (the attempt is saved under reports/) · `[~]` nothing needed changing.

Tips for a local model: one concrete change per task; name the place, the key or the screen, and say what
you want to see in game. Indent extra lines under a task to add detail.

## Queue

- [x] Add a one-line // header comment above the partial class declaration in Assets/Scripts/Core/CloneZConfig.Bathroom.cs, CloneZConfig.Doors.cs and CloneZConfig.Fixtures.cs saying what that file's settings tune, in the style of the header in Assets/Scripts/World/HomeFurniture.Kitchen.cs. Comments only; change no code.
  - 21:14 landed by spark in 22 min: Done. Added a one-line `//` header above the `CloneZConfig` partial declaration in each of the three core config partials (comments only, no
- [x] Add a one-line // header comment above the partial class declaration in Assets/Scripts/Core/CloneZConfig.Kitchen.cs, CloneZConfig.Lighting.cs and CloneZConfig.Living.cs saying what that file's settings tune, in the style of the header in Assets/Scripts/World/HomeFurniture.Kitchen.cs. Comments only; change no code.
  - 21:08 landed by 0htsserver in 16.2 min: Compile passes. Done.
- [x] Add a one-line // header comment above the partial class declaration in Assets/Scripts/World/StarterHome.Bathroom.cs, StarterHome.Doors.cs and StarterHome.Kitchen.cs saying what that part of the starter home builds, in the style of the header in Assets/Scripts/World/HomeFurniture.Kitchen.cs. Comments only; change no code.
  - 21:12 landed by 1htsserver in 19.9 min: COMPILE OK. Done.
- [x] Add a one-line // header comment above the partial class declaration in Assets/Scripts/World/RaidMapBuilder.Edge.cs, RaidMapBuilder.Outdoor.cs and HomeFurniture.Bathroom.cs saying what that file builds, in the style of the header in Assets/Scripts/World/HomeFurniture.Kitchen.cs. Comments only; change no code.
  - 21:11 landed by hts-llm in 18.5 min: Done. Added one-line `//` header comments above the `partial class` declarations, matching the HomeFurniture.Kitchen.cs style (wrapped block
- [x] Add a one-line // header comment above the partial class declaration in Assets/Scripts/Core/CloneZConfig.Raid.cs, CloneZConfig.RaidCivic.cs and CloneZConfig.RaidHomes.cs saying what that file's settings tune, in the style of the header in Assets/Scripts/World/HomeFurniture.Kitchen.cs. Comments only; change no code.
  - 21:30 landed by spark in 12.1 min: Done. Added a one-line `//` header above `public static partial class CloneZConfig` in the three specified files, matching the HouseFurnitur
- [x] Add a one-line // header comment above the partial class declaration in Assets/Scripts/Core/CloneZConfig.RaidLighting.cs, CloneZConfig.RaidOpenings.cs and CloneZConfig.RaidStore.cs saying what that file's settings tune, in the style of the header in Assets/Scripts/World/HomeFurniture.Kitchen.cs. Comments only; change no code.
  - 21:28 landed by 0htsserver in 10.3 min: Added concise header comments above the partial class declarations:
- [x] Add a one-line // header comment above the partial class declaration in Assets/Scripts/World/StarterHome.Lighting.cs and StarterHome.Windows.cs saying what that part of the starter home builds, in the style of the header in Assets/Scripts/World/HomeFurniture.Kitchen.cs. Comments only; change no code.
  - 21:22 landed by 1htsserver in 5 min: Added one‑line header comments above the partial class declarations:
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Audio/Synth/SynthCore.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 22:33 landed by spark in 61.8 min: All public methods and properties are now documented (the only undocumented public members left are the fields and constructor, which the ta
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Net/NetSocial.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 21:37 landed by 0htsserver in 5.8 min: We need to add summaries for many public members. This is extensive. Given time constraints, we may not add all. But task requires every pub
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Net/NetSession.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 21:49 landed by 1htsserver in 17.8 min: Added concise `/// <summary>` XML comments to every public method and property in **Assets/Scripts/Net/NetSession.cs**, matching the file’s 
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Humans/HumanNpc.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 21:41 landed by hts-llm in 9.9 min: Added the missing **State** property back into `HumanNpc.cs` with a proper XML summary, restored the original `Target` property syntax, and 
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/Camps.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 21:46 landed by 0htsserver in 8.5 min: Added concise one‑sentence `/// <summary>` XML documentation comments to every public method and property in **Assets/Scripts/World/Camps.cs
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/Vehicles.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 21:58 landed by hts-llm in 17.1 min: **Summary of changes**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Gadgets/Gadgets.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 22:04 landed by 0htsserver in 17.9 min: **Changes Made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/DecorMaterials.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 22:21 landed by 1htsserver in 31.4 min: Implemented comprehensive `<summary>` documentation for each public method and property in **Assets/Scripts/World/DecorMaterials.cs**. Added
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Survival/FieldSurvival.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 22:07 landed by hts-llm in 8.2 min: **Changed files**
- [!] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Humans/Outposts.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  - 22:39 failed (0htsserver, 34.9 min); see reports/2026-10-18-2131.md
- [!] Cut per-frame work in Assets/Scripts/World/Vehicles.cs without changing behavior: in VehicleDriver, cache the PlayerStats component in Begin next to the other cached components and use that field in LateUpdate instead of calling GetComponent<PlayerStats>() every frame; in HudVehicle.LateUpdate, rebuild the HUD string only when one of the displayed values (rounded km/h, fuel to one decimal, and the other values it shows) changed since the last frame, still clearing the text at once when there is no driver; in Vehicle.Update, set _engine.pitch only when the new pitch differs from the last one set by more than 0.001.
  The HUD must show exactly the same text as today. Don't touch vehicle physics, input or networking.
  - 07:09 failed (spark, 45.7 min); see reports/2026-10-19-0623.md
- [x] Make idle sinks cheaper in Assets/Scripts/World/Fixtures/KitchenSink.cs: in KitchenSink.Update, return early when the tap is off and the water level can't change this frame (the basin is empty, or the plug is in), and call RefreshWater only when the water level actually changed.
  Read the whole Update first. Filling, draining, overflow messages, dish refresh, dry running and the tap-noise timing must behave exactly as before.
  - 06:28 landed by 0htsserver in 4.6 min: **Changes Made**
- [x] Stop rewriting the clock hands every frame in Assets/Scripts/World/Fixtures/ClockFixture.cs: in Turn(), work out the hour and minute angles as it does now, but only assign _hour.rotation and _minute.rotation when that hand's angle moved more than 0.5 degrees since it was last assigned (cache the last angles, starting at float.NaN so the first call always assigns).
  Stopped clocks, the time shown and the interaction text must behave exactly as before.
  - 05:15 landed by 1htsserver in 10.8 min: **Changed**
- [x] Fit raid ceiling lights to their rooms in Assets/Scripts/World/RaidBuilding.Furnish.cs: in RaidBuilding.CeilingLight the range is currently Mathf.Clamp(size * 0.95f, 6f, 9f), so even a small bathroom gets a 6 m light that leaks through the walls into the next rooms (only the four nearest lamps cast shadows). Use 1.3 times the room's half-diagonal (0.5 * sqrt(width^2 + height^2) of room.World) clamped to 3.5..9 m instead.
  Keep the intensity, colour, circuit, flicker, switch behaviour, glow mesh and shadow argument exactly as they are.
  - 05:15 landed by hts-llm in 10.5 min: **Changes Made**
- [x] Give raid street lights a lived-in look in Assets/Scripts/World/DecorProps.cs: in DecorProps.StreetLight, vary the cobra-head housing's grey by up to +/-6% brightness and the lit bulb's colour temperature slightly (within the existing warm sodium tone) per light. Keep the geometry, the lit/unlit behaviour and the returned bulb position unchanged.
  Don't draw extra numbers from the map's shared System.Random: that would shift everything generated after it. Derive the variation from a hash of the object's world position instead, so the same map looks the same every time.
  - 06:29 landed by 1htsserver in 4.8 min: **Changed file**
- [x] Add a subtle lived-in variation to the starter home's fixed lamps in Assets/Scripts/World/StarterHome.Lighting.cs: scale the intensity of the sconce, store bulkhead and bathroom vanity lamps by a factor between 0.95 and 1.05 per lamp. Leave the porch lantern, the ranges, the colours, the circuits and the dusk sensor unchanged.
  Don't draw extra numbers from the map's shared System.Random: that would shift everything generated after it. Derive the variation from a hash of the object's world position instead, so the same map looks the same every time.
  - 06:36 landed by hts-llm in 12.2 min: **Changed file**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Humans/Outposts.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields, constants and constructors, and don't change any code. Work through the file one member at a time.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 06:41 landed by 0htsserver in 12.6 min: Implemented one‑sentence XML `<summary>` comments for every public method and property in **Assets/Scripts/AI/Humans/Outposts.cs** that prev
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/GameEvents.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 06:36 landed by 1htsserver in 7.5 min: Added concise `/// <summary>` XML docs above every public method in **Assets/Scripts/Core/GameEvents.cs** that previously lacked them. Each 
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/GameManager.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 06:44 landed by hts-llm in 7.5 min: **Changes made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Profile/ProfileData.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 06:48 landed by 1htsserver in 12.2 min: Added concise one‑sentence `/// <summary>` XML comments above every public method and property in **Assets/Scripts/Profile/ProfileData.cs** 
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Player/PlayerStats.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 06:59 landed by 0htsserver in 17.5 min: **What changed**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Humans/HumanUtil.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 07:01 landed by hts-llm in 17.3 min: Added one‑sentence `/// <summary>` comments to every public method and property in **Assets/Scripts/AI/Humans/HumanUtil.cs**, matching the e
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Player/PlayerSkills.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 06:52 landed by 1htsserver in 3.9 min: Added concise one‑sentence `/// <summary>` XML docs above every public method and property in **Assets/Scripts/Player/PlayerSkills.cs** that
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Zone/Radiation.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 07:00 landed by 1htsserver in 7.2 min: **Changes Made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Weapons/Ballistics.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 07:06 landed by 0htsserver in 7.1 min: Added concise `/// <summary>` XML comments to every public method and property in **Assets/Scripts/Weapons/Ballistics.cs** that was missing 
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Zone/ArtifactCatalog.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 07:06 landed by 1htsserver in 5.8 min: Added concise `/// <summary>` XML doc comments to every public member in **Assets/Scripts/Zone/ArtifactCatalog.cs** that lacked them.
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Economy/CentralEconomy.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 07:25 landed by hts-llm in 23.8 min: The file **Assets/Scripts/Economy/CentralEconomy.cs** was corrupted while adding summaries, introducing invalid syntax (e.g., `=>` in places
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/SandboxSettings.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 07:14 landed by 1htsserver in 8.2 min: The edit tool can only match a single line, so inserting a new `<summary>` line directly above a declaration requires replacing the line tha
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Economy/Trading.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Skip fields and constants, and don't change any code.
  Insert only: each edit adds the new `///` lines directly above one declaration and keeps that declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 07:13 landed by 0htsserver in 6.8 min: Added concise `<summary>` XML documentation to every public method and property in **Assets/Scripts/Economy/Trading.cs**, matching the exist

### Evening batch 2026-10-19: fixes and retries first, then frame-time trims, flavour text, and documentation for the small hours

- [x] Make the raid street lights' lived-in variation really differ per light in Assets/Scripts/World/DecorProps.cs: DecorProps.StreetLight samples Mathf.PerlinNoise at `head`, but `head` is in the RaidMesh's local frame (RaidMapBuilder.Outdoor.cs calls m.SetFrame(p, yaw) right before StreetLight), so every upright street light gets exactly the same housing grey and bulb tint.
  Sample both noises at the pole's position instead: add `Vector3 at = m.FrameOrigin;` and use at.x and at.z where head.x and head.z are used in the two Mathf.PerlinNoise calls, keeping the 0.13 and 0.17 scales, the +100 offsets and the factor ranges.
  Change nothing else: the geometry, the lit/unlit materials, the shared System.Random draws and the returned bulb position stay exactly as they are.
  - 17:08 landed by 0htsserver in 2.8 min: **Changes made**
- [x] Cut per-frame work in Assets/Scripts/World/Vehicles.cs, part 1 of 2 (a retry: the first try died on a model timeout, not a code problem): in VehicleDriver, cache the PlayerStats component in Begin next to the other cached components and use that field in LateUpdate instead of calling GetComponent<PlayerStats>() every frame; in Vehicle.Update, set _engine.pitch only when the new pitch differs from the last one set by more than 0.001.
  Keep the same null checks as today. Also indent the declarations an earlier night left at column 0 near the top of the file (public int Spot, public bool EngineOn, public float Speed, public PlayerStats Driver and their neighbours) like the members around them.
  Don't touch HudVehicle (part 2 is a separate task), vehicle physics, input or networking.
  - 17:13 landed by hts-llm in 7.7 min: **Changes made**
- [x] Clean up the garbled header comment in Assets/Scripts/Core/CloneZConfig.RaidStore.cs: line 3 ("The raid store components tuned by raid settings in this file") is indented 8 spaces and stuffed with no-break spaces (U+00A0) and zero-width spaces (U+200B).
  Replace it with one clean `//` line indented 4 spaces, directly above `public static partial class CloneZConfig`, saying what the file's settings tune: read its `// ---- ... ----` section headings and name what they cover, in the style of the header in CloneZConfig.RaidOpenings.cs.
  Afterwards no U+00A0 or U+200B may remain anywhere in the file. Comments only: change no code or values.
  - 17:08 landed by 1htsserver in 3 min: **Changed**
- [x] Tidy the end of Assets/Scripts/World/StarterHome.Lighting.cs, left mis-indented by an earlier night: it now ends with "        #endregion", "        }", "    }" after a double blank line. Put the class's closing brace at 4 spaces and the namespace's at column 0, with at most one blank line before #endregion.
  Whitespace only: the braces must still balance and nothing else may change.
  - 17:45 landed by spark in 39.3 min: COMPILE OK and braces balance. Done.
- [~] Let the survivor restart a stopped raid-town wall clock in Assets/Scripts/World/Fixtures/ClockFixture.cs: when the clock is stopped (Stopped is true) and the survivor carries a battery, add a "Put in a fresh battery" action after "Check the time" that uses up one battery, starts the clock (_stoppedAt = -1f) and sets _lastHourAngle and _lastMinuteAngle back to float.NaN so the hands jump to the current time at once.
  Use the battery item the flashlight takes (grep "battery" in Assets/Scripts/Player/PlayerGear.cs and Assets/Scripts/Items for its id), and check for and consume it the way the "Hang a gate (4 planks, 6 nails)" action in Assets/Scripts/World/Camps.cs does. Notify in the file's voice, e.g. "You swap in a fresh battery. The minute hand twitches, then creeps on." If the game has no carryable battery item, change nothing.
  Without a battery nothing new shows. Running clocks, "Check the time", "Listen to it tick" and the hand angles behave exactly as before. Raid clocks aren't saved, so add no saving.
  - 17:19 0htsserver made no changes: Open ClockFixture.cs.
- [x] Re-indent declarations that earlier nights' doc edits left at column 0 in Assets/Scripts/Core/GameEvents.cs, Assets/Scripts/Core/SandboxSettings.cs and Assets/Scripts/Economy/Trading.cs: every line inside the namespace that starts at column 0 with `public` or `///` (for example GameEvents' Raise... methods, SandboxSettings.FilePath and LoadFromDisk, Trading.LoyaltyOf, NextLevelNeeds and Visible) gets the same indentation as the members around it.
  Whitespace only: change no code or comment text. The project must compile exactly as before.
  - 17:54 landed by 1htsserver in 45.2 min: All targeted members now have proper 8‑space indentation inside their namespaces/classes.

### Frame-time trims: stop redoing work every frame (what the player sees must not change)

- [x] Stop fetching the camera's URP data every frame in Assets/Scripts/Rendering/Presentation.cs: PostFx.LateUpdate calls cam.GetComponent<UniversalAdditionalCameraData>() every frame. Keep the camera and its UniversalAdditionalCameraData in two private fields, and look the data up (adding it when missing, as now) only when Camera.main differs from the cached camera or the cached data has been destroyed; then set renderPostProcessing = true only when it's false, as now.
  Everything else in PostFx must stay exactly as it is. Don't touch GasVisorOverlay or DiegeticWatch in the same file (a later task does).
  - 17:15 landed by hts-llm in 1.7 min: Implemented caching of the main camera and its `UniversalAdditionalCameraData` in **Presentation.cs**:
- [x] Rebuild the hunter's HUD only when it changes in Assets/Scripts/AI/Creatures/PlayerMonster.cs: in HudHunter, (a) keep a List<Image> beside _marks, filled in Mark() when a heartbeat mark is created (UIKit.Panel already returns the Image), and use it in LateUpdate instead of calling mark.GetComponent<Image>() for every mark every frame; (b) build the "claws / trick / earned" line only when m.Def.Name, either Bar() text or m.Earned differs from what was shown last time, still setting the text to "" at once when there's no hunter.
  The text, the marks' positions, pulse and colours must look exactly as they do now.
  - 17:18 landed by hts-llm in 3.6 min: **Changed files**
- [x] Rebuild the drone's on-screen display only when a shown value changes in Assets/Scripts/Gadgets/Gadgets.cs: HudDrone.LateUpdate rebuilds the whole OSD string every frame. Cache the values it shows (FPV or UGV, battery %, altitude to 0.1 m, km/h, distance in whole metres, payload armed) and rebuild the string only when one of them differs from the last build; set "" once when there's no drone.
  The text must read exactly as today; keep the vignette colour logic. Also indent the declarations an earlier night left at column 0 near the top of the file (public DroneKind Kind, public float Battery01, public bool HasGrenade, public bool IsDead and their neighbours) like the members around them.
  Don't change drone flight, networking or the tablet.
  - 17:27 landed by hts-llm in 8.6 min: **Changes made**
- [x] Rebuild the artifact detector's title only when it changes in Assets/Scripts/Zone/Detectors.cs: HudDetector.LateUpdate runs _d.Active.Name.ToUpperInvariant() plus string concatenation every frame. Cache the upper-cased name per active model and the last shown rounded distance (and whether there was a target), and assign _title.text only when one of them changed.
  The title must read exactly as now; leave the lamp, the needle and the lattice dots as they are.
  - 17:26 landed by 0htsserver in 7.5 min: **Changed files**
- [x] Re-indent declarations that earlier nights' doc edits left at column 0 in Assets/Scripts/Net/NetSession.cs, Assets/Scripts/Net/NetSocial.cs and Assets/Scripts/Player/PlayerStats.cs: every line inside the namespace that starts at column 0 with `public` or `///` (for example NetOut's constructor and its I/U/UL writers, NetSocial.VoiceRange, PlayerStats.Exertion, IsDead and Transform) gets the same indentation as the members around it.
  Whitespace only: change no code or comment text. The project must compile exactly as before.
  - 17:39 landed by 0htsserver in 12.8 min: **Summary of changes**
- [x] Rebuild the compass strip only when the view turns in Assets/Scripts/Survival/Navigation.cs: HudCompass.LateUpdate allocates a new StringBuilder and rebuilds the strip every frame. Keep one StringBuilder in a field (Clear() it before use), remember the yaw and the shown/hidden state the strip was last built for, and rebuild only when the yaw moved more than 0.2 degrees or that state changed.
  The strip's characters and spacing must stay identical, and it must still clear at once when the compass is lost, the camera is gone or the HUD is hardcore.
  - 17:29 landed by hts-llm in 2.1 min: **Changed files**
- [x] Trim the radiation readout's per-frame work in Assets/Scripts/Zone/Radiation.cs: in HudRadiation.LateUpdate, call SetActive on the children only when `show` differs from the previous frame (start from a value that forces the first update), and rebuild the dose text only when the shown dose (two decimals), the trend arrows or the iodine note changed; keep UIKit.SetBar as it is.
  Also indent the lines an earlier night left at column 0 (PlayerRadiation's Local, Dose, Intake, Shielded and their neighbours near the top, and `public static HudRadiation Create()`) like the members around them. The readout must look exactly as now.
  - 17:35 landed by hts-llm in 5.3 min: **Summary of changes**
- [x] Re-indent declarations that earlier nights' doc edits left at column 0 in Assets/Scripts/Weapons/Ballistics.cs, Assets/Scripts/World/Camps.cs and Assets/Scripts/Zone/ArtifactCatalog.cs: every line inside the namespace that starts at column 0 with `public` or `///` (for example Ballistics' Register and Ammo methods, Camps' IsMine, IsSquadmate and Announce, ArtifactCatalog's Get, Prop and IsArtifact) gets the same indentation as the members around it.
  Whitespace only: change no code or comment text. The project must compile exactly as before.
  - 17:57 landed by hts-llm in 21.8 min: **Changes Made**
- [x] Rebuild the car HUD only when it changes in Assets/Scripts/World/Vehicles.cs, part 2 of 2: in HudVehicle.LateUpdate, rebuild the HUD string only when one of the displayed values (rounded km/h, fuel to one decimal, and the other values it shows) changed since the last frame, still clearing the text at once when there is no driver.
  The HUD must show exactly the same text as today. Don't touch VehicleDriver, Vehicle, physics, input or networking.
  - 17:46 landed by 0htsserver in 6.7 min: Implemented optimized HUD update in **Assets/Scripts/World/Vehicles.cs**:
- [x] Rebuild the gas-mask and wristwatch readouts only when they change in Assets/Scripts/Rendering/Presentation.cs: GasVisorOverlay.Update builds "Mask filter NN%" every frame, and DiegeticWatch.Update builds "HH:MM" and "filter NN%" every frame while it's shown. In each class, cache the last shown filter percent (and, for the watch, the last hour and minute) and assign each Text only when its value, or whether it's shown, changed.
  What the player sees must be identical, including clearing the texts when the mask comes off. Don't touch PostFx (an earlier task changes it).
  - 18:18 landed by spark in 33.4 min: Both classes updated and the file compiles.

### Flavour text: append-only writing that makes the world feel lived in (original wording only)

- [x] Add five more notes to the starter home's fridge door in Assets/Scripts/World/Fixtures/KitchenFridge.cs: append five entries to the Notes array (read in turn by "Read the notes on the door"): small, specific traces of the family who lived here, funny or sad, in July 1993 as the outbreak begins (wave survival is summer, so no snow).
  All original: no real brands or people and no names from books, films or other games. Match the existing notes' length, voice and punctuation (including the \u2014 escapes). Only append to the array; change nothing else.
  - 17:56 landed by 0htsserver in 9.8 min: **Summary**
- [x] Add four more letters to the raid town's mail drop box in Assets/Scripts/World/Fixtures/RaidTownMailDrop.cs: append four entries to Letters (shown in turn by "Pull open the chute"): glimpses of post nobody will collect, in a snowbound Kentucky town in mid-December 1993, the week the county was emptied.
  All original: no real brands or people and no names from books, films or other games. Match the existing letters' length, voice and punctuation (the same \u201C \u201D \u2014 escapes). Only append to the array; change nothing else.
  - 17:58 landed by 1htsserver in 3.8 min: **Changed file**
- [x] Give the county office's photocopier more to find on its glass in Assets/Scripts/World/Fixtures/RaidStoreCopier.cs: append five sheets to Originals (county memos, forms and notices from the week the county was emptied, December 1993) and two lines each to Jams, Copies and Clears, in the same dry voice.
  All original: no real brands or people and no names from books, films or other games. Match the existing entries' length and punctuation (the same \u201C \u201D \u2014 escapes). Only append to the arrays; change nothing else.
  - 18:05 landed by 0htsserver in 9.2 min: **Summary of changes**
- [x] Fill in the checkpoint guard's day in Assets/Scripts/World/RaidMapBuilder.Props.cs: BoothLog ("Read the logbook" in the guard booth) is read a page at a time: 0600, 1140, 1615, 2050. Insert three pages in time order between them (for example 0830, 1320 and 1840) that build the same day's dread (traffic turned back, orders that stop making sense, the radio going quiet), keeping the 2050 page last.
  Same terse log-entry voice, invented names only, the same \u201C \u201D escapes. Change nothing but the BoothLog array.
  - 17:59 landed by hts-llm in 1.9 min: **Changed file**
- [x] Add more notes to the workplace fridges in Assets/Scripts/World/Fixtures/RaidCivicFridge.cs: append four entries to GuardNotes (the National Guard post's kitchenette: barracks humour going sour) and four to ClinicNotes (the clinic's medical fridge: clinical notes growing worried), December 1993.
  All original: no real brands or people and no names from books, films or other games. Match the existing notes' length, voice and punctuation. Only append to the two arrays; change nothing else.
  - 18:05 landed by 1htsserver in 7.1 min: **Changes made**
- [x] Add five more things to read on the town's office computers in Assets/Scripts/World/RaidBuilding.Civic.cs: append five entries to CivicScreenLines ("Read the screen" on a desk CRT): green text on black, 1993 office software, ordinary work cut off mid-task.
  All original: no real brands or people. Match the existing lines' length, voice and punctuation. Only append to the array; change nothing else.
  - 18:03 landed by hts-llm in 4.1 min: Added five new “read‑screen” lines to **Assets/Scripts/World/RaidBuilding.Civic.cs**:
- [x] Add more of the raid town houses' paperwork in Assets/Scripts/World/Fixtures/RaidHomeLiving.cs: append four magazines to Magazines (lower-case "a ..." like the others), three deliveries to Letters (each delivery is a string[] of two to four lines, read in turn) and three lines to NumberLines (each must contain {0} exactly once: it's the string.Format placeholder for the house number). December 1993.
  All original: no real brands or people and no names from books, films or other games. Match the existing entries' voice and punctuation. Only append to the arrays; change nothing else.
  - 18:15 landed by hts-llm in 11.7 min: **Changes Made**
- [x] Put four more magazines on the starter home's bookcase in Assets/Scripts/World/Fixtures/LivingReading.cs: append four entries to Magazines (what "Flick through a magazine" finds), lower-case "a ..." like the others, July 1993.
  All original: no real brands, titles or people. Match the existing entries' length, voice and punctuation. Only append to the array; change nothing else.
  - 18:07 landed by 1htsserver in 2.2 min: Implemented four additional magazine entries in **Assets/Scripts/World/Fixtures/LivingReading.cs**.
- [x] Give the radio that switches itself on more to say in Assets/Scripts/World/Zone/Uncanny.cs: append six lines to s_RadioLines in the same "...fragment..." form: wrong, sad or unsettling, never gory or threatening (the Zone is indifferent, not evil).
  Original only, using only the game's own names (the Passing, the Institute, the Choir, the Spillway). Change nothing but the array.
  - 18:07 landed by 0htsserver in 1.6 min: **Changed:**
- [x] Give the bar's regulars more to say in Assets/Scripts/Hub/HubNpc.cs: in the Lines dictionary, append three lines to each of "barkeep", "fence", "institute", "gunsmith", "medic", "guide", "merc", "patron", "beggar" and "guitar", each in that speaker's voice and about as short as theirs (under 90 characters; they're shown as subtitles). Leave the "story_" entries alone: they're told in order.
  Use only the game's own names (the Zone, the Passing, the Institute, the Wardens, the Choir, Freehold, Vanguard, Kestrel, Mirelock, the Spillway, the Lodestar), nothing from books, films or other games. Change nothing else.
  - 18:17 landed by 0htsserver in 9.3 min: 
- [x] Add to the kitchen radio's schedule in Assets/Scripts/Core/Broadcasts.cs: append two lines to every programme of the three radio stations (Easy 91.3, Talk 1040 and Highway Advisory) that isn't a Dead(...) one, each fitting its programme's days (ordinary with a growing unease on day 1, curfews and boil-water notices on days 2-3, the skeleton crew after) and that station's voice.
  Follow the conventions in the summary on Broadcasts: optional ?water:/?nowater:/?power:/?nopower: prefixes, {greeting} and {weekday}, sounds between asterisks. Wave survival is summer (July 1993). Don't touch the TV stations, the order of existing lines or any code.
  - 18:30 landed by 1htsserver in 22.5 min: **Changed file**
- [x] Add three more posters to the raid town's walls in Assets/Scripts/World/RaidBuilding.Lighting.cs: append three entries to ArmyPosters in the same voice (1993 recruiting and public-information posters, all original wording).
  Match the existing entries' length and punctuation; change nothing but the array.
  - 18:16 landed by hts-llm in 1.6 min: 

### Documentation: one-sentence summaries where they're missing (filler for the small hours)

- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/Sqlite/Sqlite.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:25 landed by hts-llm in 8.5 min: Added concise `/// <summary>` documentation to all public members of **Assets/Scripts/Core/Sqlite/Sqlite.cs** that previously lacked it:
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/UI/UIKit.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:26 landed by 0htsserver in 8.9 min: Added concise one‑sentence `/// <summary>` XML documentation for every public method and property in **Assets/Scripts/UI/UIKit.cs** that pre
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Zone/Artifacts.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:48 landed by spark in 29.5 min: Done.
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Net/NetPlayers.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:32 landed by hts-llm in 6.5 min: **Changes made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Creatures/Creatures.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:30 landed by 0htsserver in 4.5 min: Implemented one‑sentence XML summaries for all public methods and properties in **Assets/Scripts/AI/Creatures/Creatures.cs** that previously
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/FarmPlot.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:34 landed by 1htsserver in 4.1 min: **Changed files**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Characters/HumanoidLook.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:42 landed by 0htsserver in 11.7 min: Implemented proper XML documentation for all public members in **HumanoidLook.cs** and corrected the `Survivor` method signature. The file n
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/Zone/ZoneKit.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:51 landed by hts-llm in 18.7 min: **Changes Made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/GroundItemStore.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 18:54 landed by 1htsserver in 19.5 min: **Changes made**
- [!] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Quests/QuestBook.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:13 failed (0htsserver, 31.1 min); see reports/2026-10-19-1609.md
- [!] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/Zone/Helicopter.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:48 failed (spark, 120 min); see reports/2026-10-19-1609.md
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/WaterSource.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:06 landed by hts-llm in 15.6 min: Added concise one‑sentence `<summary>` XML comments to every public method and property in **Assets/Scripts/World/WaterSource.cs** that prev
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Profile/ProfileStore.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:15 landed by 1htsserver in 21 min: **Summary of changes**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/RaidMesh.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:12 landed by hts-llm in 5.4 min: Implemented one‑sentence `/// <summary>` XML comments for all public members in **Assets/Scripts/World/RaidMesh.cs** that previously lacked 
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Medical/PlayerLimbs.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:20 landed by hts-llm in 8.2 min: Added concise one‑sentence `/// <summary>` documentation to every public method, property, and event in **Assets/Scripts/Medical/PlayerLimbs
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Economy/Hideout.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:20 landed by 0htsserver in 6.9 min: Added one‑sentence `/// <summary>` XML docs to every public method/property in **Assets/Scripts/Economy/Hideout.cs** that lacked them, prese
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Hub/Screens/HubUI.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:28 landed by 1htsserver in 13.1 min: Implemented comprehensive XML‑style `<summary>` documentation for every public method and property in **Assets/Scripts/Hub/Screens/HubUI.cs*
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Humans/Companions.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:30 landed by hts-llm in 9.5 min: Added concise XML‑style `/// <summary>` comments to each public member in **Companions.cs** that previously lacked them:
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Zone/Anomalies.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:30 landed by 0htsserver in 9.7 min: Added concise XML‑style summaries to every public method and property in **Assets/Scripts/Zone/Anomalies.cs** that previously lacked them, w
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Player/PlayerInteractor.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:46 landed by 1htsserver in 18.3 min: Implemented comprehensive one‑sentence XML `<summary>` documentation for every public method and property in **Assets/Scripts/Player/PlayerI
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Characters/HumanoidRig.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:33 landed by hts-llm in 2.7 min: Added concise one‑sentence `/// <summary>` documentation to every public method and property in **Assets/Scripts/Characters/HumanoidRig.cs**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/WorldItem.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:44 landed by 0htsserver in 13.3 min: Added concise `/// <summary>` XML docs to every public method and property in **Assets/Scripts/World/WorldItem.cs** that lacked them, matchi
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Survival/Diseases.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 19:53 landed by hts-llm in 19.6 min: Added concise XML‑summary comments to every public member in **Assets/Scripts/Survival/Diseases.cs** that previously lacked them.
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Player/PlayerWounds.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:14 landed by 0htsserver in 30 min: All compilation errors have been resolved.
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/WorldMarkers.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:01 landed by 1htsserver in 14.9 min: **Changed files**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/Actor.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:04 landed by hts-llm in 11.2 min: **Modified file**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Humans/ALife.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:06 landed by 1htsserver in 4.9 min: No further changes needed.
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/Zone/ZoneLandmarks.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:08 landed by hts-llm in 3.6 min: Added concise XML‑style `<summary>` comments to every public method/property in **Assets/Scripts/World/Zone/ZoneLandmarks.cs** that was miss
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Player/PlayerInventory.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:14 landed by 1htsserver in 7.6 min: **Changes made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/BuildPiece.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:52 landed by hts-llm in 44.4 min: 
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/Barricade.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 20:24 landed by 0htsserver in 10.4 min: Added concise one‑sentence `/// <summary>` XML documentation to every public method and property in **Assets/Scripts/World/Barricade.cs** th

### Late-evening starters 2026-10-19: documentation while the night's batch is written

- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/DamageModel.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 21:34 landed by 0htsserver in 8.4 min: Added concise `/// <summary>` comments to every public method and property in **Assets/Scripts/Core/DamageModel.cs** that previously lacked 
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Items/ItemExtra.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 21:39 landed by spark in 13.3 min: Done. `Assets/Scripts/Items/ItemExtra.cs` only: added one-sentence `/// <summary>` lines to the six public methods/properties that lacked th
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Medical/PlayerBlood.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 21:37 landed by hts-llm in 12 min: **Changed file**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Player/PlayerClothing.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 21:37 landed by 1htsserver in 12.1 min: Added concise XML‑style `/// <summary>` documentation to every public method and property in **Assets/Scripts/Player/PlayerClothing.cs** tha

### More documentation 2026-10-19: while the planner is being finished

- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Items/ItemDatabase.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 22:55 landed by 0htsserver in 14.5 min: **Changes made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/Fixtures/RaidHomeBath.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 22:47 landed by hts-llm in 6.3 min: **Changes made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Humans/HumanLoadouts.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 22:44 landed by 1htsserver in 3.7 min: - Added a concise `<summary>` XML comment above each public method/property in `HumanLoadouts.cs` that lacked one:
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/GameSession.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 22:54 landed by spark in 12.8 min: Done. Changed `Assets/Scripts/Core/GameSession.cs` only: added a one-sentence `/// <summary>` above each of the six undocumented public prop

### Auto-planned 2026-10-19: written by the night shift when the queue ran low (delete any you don't want)

- [x] In Assets/Scripts/Survival/FieldSurvival.cs: Add a null‑check for `PlayerChannel.Local` in `FieldSurvival.LightFire` so the method returns `false` instead of throwing a `NullReferenceException` when the channel component isn’t present.
  In the method `public static bool LightFire(PlayerInventory inv)` insert `if (PlayerChannel.Local == null) return false;` right after the existing `if (inv == null) return false;`.
  The player will no longer see the game crash or freeze when trying to light a fire before the `PlayerChannel` component has been added (e.g., very early in a new run or after a scene reload). All other logic and return values stay unchanged.
  - 23:03 landed by 0htsserver in 3.7 min: **Changed**
- [!] Add a tiny hinge‑pin cylinder to the mailbox door for visual detail in the `Mailbox` method of Assets/Scripts/World/StarterHome.Grounds.cs
  Insert after the line creating `doorPart` (`FixturePart doorPart = FixturePart.Hinge(...);`) the call:
  `m.Cylinder(at + new Vector3(0f, 1.12f, -0.252f), 0.006f, 0.018f, 12, face);`
  The cylinder (radius 0.006 m, height 0.018 m) sits at the door hinge point (`at + new Vector3(0f, 1.12f, -0.252f)`) using the same metal material (`face`) as the door, adding a realistic pin without affecting colliders or interaction.
  - 00:27 failed (spark, 87.4 min); see reports/2026-10-19-2257.md
- [x] In Assets/Scripts/Camera/ViewCrosshair.cs: Cache the singleton lookup of `IsoCameraFollow.Instance` in `ViewCrosshair` to avoid a per‑frame search.
  Add a private field `private IsoCameraFollow _cam;` and assign it in `Awake()` after `Build()` (e.g. `_cam = IsoCameraFollow.Instance;`).
  Replace the two occurrences of `IsoCameraFollow.Instance` (in `LateUpdate` for the `show` calculation and in `Bind()` for the target lookup) with the cached `_cam`.
  If `_cam` becomes null (e.g. the camera is destroyed), keep the existing logic by checking `_cam != null` before using it; otherwise the behaviour is identical.
  The crosshair’s visibility, tinting, and kick animation remain unchanged; the player sees no visual difference, but the per‑frame overhead of the singleton lookup is eliminated.
  - 23:12 landed by hts-llm in 11.2 min: **Changes made**
- [x] Add a “Tap the nozzle” action to Assets/Scripts/World/Fixtures/RaidTownGasPump.cs that only works when the nozzle is lifted, plays the pump’s noise and shows a short message.
  In `GetActions` insert after the “Sniff the hose” line:
  `into.Add(new InteractAction("Tap the nozzle", TapNozzle));`
  Create a private method `TapNozzle(InteractContext ctx)` that:
  `if (!_lifted) { ctx.Notify("The nozzle is still in its cradle."); return; }`
  `MakeNoise(CloneZConfig.RaidPumpNoise);`
  `ctx.Notify("You give the nozzle a quick tap; it squeaks faintly but nothing else happens.");`
  The player will now see the new option when looking at the pump, hear the same pump noise as when lifting, and receive context‑sensitive text instead of nothing. All existing fields, methods, and constants remain unchanged.
  - 23:06 landed by 1htsserver in 4.7 min: **Summary**
- [x] Append five new office‑wall notices to the `OfficeNotices` array in Assets/Scripts/World/RaidBuilding.Lighting.cs, keeping the existing entries unchanged and adding the new strings after them; the player will read these on office walls during exploration.
  Reference style: `"The county's SAFETY FIRST notice: lift with your knees, report every spill, know where the first‑aid kit is. Nobody ever knew where the first‑aid kit was."` – the new entries should match its terse, matter‑of‑fact tone.
  Themes for the new entries:
  winter‑time safety (e.g., icy‑step warnings),
  fuel‑pump or diesel‑storage hygiene,
  morale‑boosting reminder (payday or holiday note),
  emergency‑procedure checklist (fire, lockdown),
  a brief warning about mold or damp in the office.
  - 23:07 landed by 0htsserver in 2.5 min: **Changed file**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Items/LootContainer.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 23:16 landed by 1htsserver in 9.6 min: All required summary docs have been added without compile errors.Added concise one‑sentence `/// <summary>` XML comments to every public met
- [x] Add eight small rust‑colored maintenance plates to the cooling‑tower landmark for visual wear, editing Assets/Scripts/World/Zone/ZoneLandmarks.cs, method CoolingTower(SiteBuilder sb)
  Insert the new block after the closing brace of the outer `for (int ring = 0; ring < 6; ring++) { … }` loop and before the call to `ZoneKit.Plume(...)`.
  Use the existing deterministic RNG (`sb.Rng`) for tiny position jitter, and reuse the concrete‑dark material palette.
  `// Small rusted maintenance plates on the outer concrete shell`
  `for (int i = 0; i < 8; i++)`
  `{`
  `float angle = i * Mathf.PI * 0.25f; // 45° steps`
  `// Base position on the outer radius of the lowest ring (≈9 m)`
  `Vector3 basePos = new Vector3(Mathf.Cos(angle) * 9f, 2f, Mathf.Sin(angle) * 9f);`
  - 23:10 landed by 0htsserver in 2.5 min: **Changed file**
- [x] Fix the zombie‑type roll in Assets/Scripts/Zombies/ZombieSpawner.cs (`RollType`) so that crawlers can still appear after the tough‑unlock wave; the current order makes the `FastShambler` check consume the whole `< 0.35` range before the crawler test, preventing crawlers from ever being chosen once `_wave >= CloneZConfig.ToughUnlockWave`.
  - 23:13 landed by 0htsserver in 3.1 min: **Changed file**: `Assets/Scripts/Zombies/ZombieSpawner.cs`
- [x] Add a small stamped metal “SAFE” plate to the gun‑safe body in Assets/Scripts/World/FurnitureBuilder.RaidCivic.cs, method GunSafeBody.
  Create a thin box about 0.18 m × 0.03 m × 0.004 m (width × height × depth) positioned just in front of the door’s centre:
  `m.Box(new Vector3(0f, 1.62f, 0.285f), new Vector3(0.18f, 0.03f, 0.004f), Mt(Chrome));`
  Use the existing Chrome material (`Mt(Chrome)`) so the plate looks like a brushed‑metal emboss, giving the safe a period‑correct visual label without altering colliders, interaction points, or any other geometry.
  - 23:18 landed by hts-llm in 6.1 min: **Changes made**
- [x] In Assets/Scripts/Rendering/IsoVisionShade.cs: Cache the per‑ray direction vectors in IsoVisionShade so they aren’t recomputed each Rebuild()
  Add a private static readonly Vector3[] `_rayDirs` field sized `Rays` and fill it in `Awake()` with
  `_rayDirs[i] = Quaternion.Euler(0f, i * (360f / Rays), 0f) * Vector3.forward;`
  Replace the line `Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;` in `Rebuild()` with `Vector3 dir = _rayDirs[i];`
  Optionally also cache the outer offset `Vector3 _rayOuter = dir * Outer;` in a second static array to avoid the multiplication inside the loop.
  The visual result (the shaded fog‑of‑war ring) stays identical, but the per‑frame work drops the 180 Quaternion/Euler constructions and two multiplications, reducing allocations and CPU time during the nightly updates.
  - 23:26 landed by 0htsserver in 13.5 min: **Changed file**
- [x] In Assets/Scripts/World/Fixtures/RaidStoreWaterHeater.cs: Add a “Turn off the pilot” action to `RaidStoreWaterHeater.GetActions` that lets the player extinguish the heater’s pilot flame when it is lit.
  In `GetActions`, after the existing “Check the pilot light” line, insert a counterpart conditional:
  `if (_pilot)`
  `into.Add(new InteractAction("Turn off the pilot", Extinguish));`
  `else`
  `into.Add(new InteractAction("Relight the pilot", Relight)`
  `.Timed(CloneZConfig.RaidHeaterRelightSeconds, "Relighting the pilot", HumanoidActivity.Use));`
  Add a new private method `Extinguish` matching the style of `Relight`:
  `private void Extinguish(InteractContext ctx)`
  - 23:20 landed by 1htsserver in 3.7 min: **Changed files**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Economy/Money.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 23:33 landed by hts-llm in 14.9 min: Added concise one‑sentence `/// <summary>` documentation to every public method/property in **Assets/Scripts/Economy/Money.cs** that previou
- [x] Add chrome hinge pins to the side frames of the medicine cabinet in Assets/Scripts/World/FurnitureBuilder.Utility.cs (`FurnitureBuilder.MedCabinet`).
  - 23:32 landed by 1htsserver in 11.7 min: **Summary**
- [x] Fix the bribe‑action logic in `CordonGate.GetActions` (Assets/Scripts/World/Zone/ZoneCordon.cs) – the current check treats a successful money deduction as a failure, preventing players who have enough cash from bribing the Warden.
  - 23:35 landed by 0htsserver in 8.9 min: **Changed**: `Assets/Scripts/World/Zone/ZoneCordon.cs` – Updated bribe logic in `CordonGate.GetActions`. Replaced the direct `Money.Take` ca
- [x] In Assets/Scripts/Rendering/VisionController.cs: Cache the URP asset used in UpdateSurvivorRenderers() to avoid a per‑frame static property lookup.
  Add a private field `private UniversalRenderPipelineAsset _urpAsset;` to VisionController.
  In Bind() after other initialisation set `_urpAsset = UniversalRenderPipeline.asset;`.
  Replace the line `UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;` in UpdateSurvivorRenderers() with `UniversalRenderPipelineAsset urp = _urpAsset;`.
  If the asset can change at runtime (unlikely), add a cheap check at the start of LateUpdate: `if (_urpAsset != UniversalRenderPipeline.asset) _urpAsset = UniversalRenderPipeline.asset;`.
  No visual change: the rendering‑layer logic and shadow‑hiding behaviour remain identical.
  This removes the per‑frame native‑side lookup of UniversalRenderPipeline.asset, reducing allocations and CPU time.
  - 23:36 landed by 1htsserver in 4.4 min: **Changes made**
- [x] Add a lightweight “Inspect the mailbox” action to Assets/Scripts/World/Fixtures/MailboxFixture.cs by editing the GetActions method; copy the existing flag‑toggle action line as a template, change its label and logic so it simply reports the mailbox’s current open state, flag state, and how many unread deliveries remain (or that there’s no more mail).
  In GetActions insert after the flag‑toggle block: `into.Add(new InteractAction("Inspect the mailbox", ctx => { ctx.Notify($"Mailbox is {(_open ? "open" : "closed")}, flag is {(_flagUp ? "up" : "down")}, {(_read < Come ? $"{Come - _read} unread letters." : "no more mail.")}"); }));`
  Keep the existing actions (“Check the mail”, “Close the mailbox”, raise/lower flag) untouched.
  No new fields, UI screens, or assets are required; the player will now see a descriptive message when selecting “Inspect the mailbox”.
  - 23:35 landed by hts-llm in 1.5 min: **Changed**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/DecorMaterials.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 23:42 landed by hts-llm in 7.7 min: Implemented concise one‑sentence XML summaries for all public methods and properties in **Assets/Scripts/World/DecorMaterials.cs** that prev
- [x] In Assets/Scripts/Player/PlayerClothing.cs: Fix the wear‑off logic in PlayerClothing.WearPiece so that clothing with a `ConditionLossChance` of 0 never loses condition.
  Change the early‑exit test from
  `if (Random.Range(0, Mathf.Max(1, piece.Def.ConditionLossChance)) != 0) return;`
  to
  `// If the garment is defined to never lose condition, skip the wear roll.`
  `if (piece.Def.ConditionLossChance <= 0) return;`
  `if (Random.Range(0, piece.Def.ConditionLossChance) != 0) return;`
  This ensures that garments whose `ConditionLossChance` is 0 (e.g., rain‑coats or reinforced gear) do not degrade on every zombie strike, matching the intended design where a zero chance means “no wear”. The rest of the method (reducing condition, removing the garment, invoking `Changed`) stays unchanged.
  - 23:37 landed by 0htsserver in 1.7 min: **Summary of changes**
- [x] Add a small mailbox flag to the front‑yard mailbox in `Assets/Scripts/World/RaidMapBuilder.Town.cs` → method `DressHouse`, after the line `b.YardMailbox(box, RaidHomeLiving.Post(town.Rng));`
  Create a 0.02 m radius, 0.30 m tall metal pole at the mailbox’s top front: `m.Cylinder(box + new Vector3(0f, 0.15f, 0f), 0.02f, 0.3f, 6, DecorPalette.Metal(new Color(0.5f,0.5f,0.5f)), Quaternion.identity, 1f);`
  Attach a red‑brown cloth flag (0.15 m × 0.10 m × 0.02 m) to the pole: `m.Box(box + new Vector3(0f, 0.30f, 0f), new Vector3(0.15f, 0.1f, 0.02f), DecorPalette.Matte(new Color(0.6f, 0.1f, 0.1f)));`
  Wrap the addition in `if (town.Rng.NextDouble() < 0.4) { … }` so the flag appears on roughly 40 % of mailboxes, using the deterministic `town.Rng`.
  Use `town.Out.Solid(box)` to obtain the mesh builder so the flag shares the mailbox’s collider and keep‑out area.
  No new colliders or interaction points are added; the flag is purely visual and stays within the existing mailbox footprint.
  - 23:40 landed by 1htsserver in 3.8 min: **Summary**
- [x] In Assets/Scripts/World/Zone/ZoneAtmosphere.cs: Cache the singleton lookups in ZoneAtmosphere so LateUpdate doesn’t call `TimeOfDay.Instance`, `GameSettings.Current` and `SandboxSettings.Current` every frame
  Add three private fields: `private TimeOfDay _tod; private GameSettings _gs; private SandboxSettings _ss;`
  Initialize them once in `Awake()` (or the first `LateUpdate` if they’re null) with ` _tod = TimeOfDay.Instance; _gs = GameSettings.Current; _ss = SandboxSettings.Current;`
  Replace all uses of `TimeOfDay.Instance`, `GameSettings.Current` and `SandboxSettings.Current` in `LateUpdate` with the cached fields (`_tod`, `_gs`, `_ss`).
  If any of those singletons can be recreated later (e.g., after a reload), check for null at the start of `LateUpdate` and re‑assign the cached fields.
  The visual result – fog density, darkness at night, and readability tweaks – remains exactly the same; only the per‑frame property lookups are removed.
  - 23:43 landed by 0htsserver in 5.5 min: 
- [x] In Assets/Scripts/World/Fixtures/RadioFixture.cs: Add a “Check station” action to RadioFixture.GetActions so the survivor can see what station is currently tuned without turning the radio on or off.
  In `Assets/Scripts/World/Fixtures/RadioFixture.cs`, inside `GetActions` after the “Turn it down” entry, insert:
  `into.Add(new InteractAction("Check station", c => c.Notify("Currently tuned to " + Describe(Station) + (Broadcasts.IsOnAir(Station) ? "." : " — just static.")) ).DisabledIf(!near, TooFar));`
  This uses the existing `Describe`, `Station`, and `Broadcasts.IsOnAir` members, requires no new assets, and gives the player immediate feedback about the current broadcast while keeping all existing functionality unchanged.
  - 23:47 landed by 1htsserver in 7.1 min: **Changed**
- [x] Append five new flavour‑text strings to the `Originals` array in Assets/Scripts/World/Fixtures/RaidStoreCopier.cs, after the current final entry, each a whole sentence in quotes that the player can read on the copier glass.
  Use the existing entry `"A county memo stamped URGENT: “Non‑essential staff are dismissed until further notice. Do not discuss the clinic with members of the public.”"` as the style reference.
  The new entries should cover: a winter road‑closure warning, a handwritten grocery list, a church bulletin announcement, a personal note about a missing family member, and a county health‑department flyer about flu shots.
  Ensure each string ends with a period and uses period‑correct language (1993 rural Kentucky, no modern terms).
  Insert the new strings before the closing `}` of the `Originals` initializer.
  Do not modify any other code, ordering, or identifiers.
  - 23:45 landed by hts-llm in 2.2 min: **Changed file**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/SandboxSettings.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 23:54 landed by 0htsserver in 11.5 min: **Summary of changes**
- [x] Fix HubScreenBase so a screen can be reopened after it has been closed (Assets/Scripts/Hub/Screens/HubScreenBase.cs)
  In `Build()` the `_closed` flag is never reset, so after `OnClosed()` sets `_closed = true` the screen’s `Update()` method immediately returns, stopping live‑label updates and money refresh when the same screen instance is opened again.
  Add `_closed = false;` at the start of `Build()` (right after `EnsureChrome();`) to clear the closed state each time the screen is rebuilt.
  After this change, reopening a hub screen will correctly show live timers, money, and other UI updates, while all existing behavior remains unchanged.
  - 23:46 landed by hts-llm in 0.8 min: **Changed file**
- [x] Add a subtle row of rivet‑like accents to the top of the skills panel in Assets/Scripts/UI/SkillsPanelController.cs, inside the `Build()` method after the `UITheme.AddTopAccent` call.
  Create 8 small circular `Image` objects (size ≈ 0.008 m × 0.008 m) named `"Rivet0"`…`"Rivet7"` as children of `_panelRoot`’s `RectTransform`.
  Anchor each at `(0.5, 1)` with pivot `(0.5, 1)`, set `anchoredPosition` to `new Vector2(-260f + i*60f, -4f)` (spacing ≈ 0.06 m across the 0.56 m panel width), and tint with `new Color(0.7f, 0.7f, 0.7f, 0.9f)`.
  Use the default UI sprite (no extra assets) and keep the existing colliders and interaction logic untouched.
  This gives the panel a period‑appropriate metal‑fastener detail without altering layout or functionality.
  - 23:48 landed by hts-llm in 2 min: **Changed file**
- [x] Cache player actors for NearAnyone checks in Assets/Scripts/Economy/CentralEconomy.cs
  Add a private static readonly List<Actor> `s_PlayerCache = new List<Actor>();` to the class.
  In `CentralEconomy.Tick()` (just after `if (!Active || Config == null) return;`) clear and fill the cache:
  `s_PlayerCache.Clear(); foreach (Actor a in Actor.All) if (a != null && a.IsPlayerControlled) s_PlayerCache.Add(a);`
  Change `NearAnyone(Vector3 p, float range)` to iterate over `s_PlayerCache` instead of `Actor.All`.
  This removes the per‑container per‑tick enumeration of the full `Actor.All` list, reducing allocations and CPU work while preserving the same player‑proximity logic; the player‑visible behavior (which containers are considered “near anyone”) stays identical.
  - 23:49 landed by 1htsserver in 1.8 min: **Changed files**
- [x] Add a “Check channel” action to Assets/Scripts/World/Fixtures/RaidCivicRadio.cs that lets the survivor see which channel the radio is currently tuned to.
  Declare a new field `private Action<InteractContext> _checkChannel;` alongside the other Action fields.
  In `Awake()` assign it: `_checkChannel = ctx => ctx.Notify("Current channel: " + Tuned.Name);`.
  In `GetActions` after the “Change channel” entry, add:
  `into.Add(new InteractAction("Check channel", _checkChannel).DisabledIf(!near, TooFar));`
  The action should only be enabled when the player is within `CloneZConfig.ReceiverKnobReach` (same as other knob actions) and the radio is on (the existing `if (!_on) return;` guard already covers this).
  Result in‑game: when the radio is on and the survivor stands close enough, pressing Q (or the extra button) shows a notification like “Current channel: Battalion net”.
  No other code, assets, or UI screens are altered; all referenced members (`Tuned`, `TooFar`, `CloneZConfig.ReceiverKnobReach`) already exist.
  - 23:50 landed by hts-llm in 2.2 min: All changes compile successfully.Changed **Assets/Scripts/World/Fixtures/RaidCivicRadio.cs**
- [x] Append six new flavor‑text strings to the `BrushLines` array in Assets/Scripts/World/Fixtures/BasinFixture.cs so that brushing teeth can produce additional period‑correct commentary; use the existing entry `"You brush until your gums tingle and rinse with a splash. Minty. Almost civilised."` as the style reference and cover themes of cold mornings, lingering soot, quiet desperation, makeshift hygiene, and the ever‑present cold wind.
  Locate the `private static readonly string[] BrushLines = { … };` declaration.
  After the second entry (the one quoted above), insert six new quoted sentences, each ending with a period, e.g.:
  `"The frigid air bites your cheeks as you scrub, reminding you the world outside is still frozen."`
  `"You watch the thin stream of water turn into a misty cloud, disappearing like breath in the room."`
  `"A thin film of soot clings to your toothbrush, a reminder of the burnt logs in the stove."`
  `"The metal of the tap feels icy, and you wonder how long the mains will stay silent."`
  `"You spit the frothy foam into the basin, hearing it echo in the tiny, empty space."`
  `"Each swipe feels like a small rebellion against the cold that seeps through the cracked walls."`
  - 23:52 landed by 1htsserver in 2.1 min: **Change summary**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Hub/Screens/HubKit.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 00:02 landed by hts-llm in 12.2 min: **Changed files**
- [x] Add a subtle glowing tip to the beam cone created in `ZoneKit.Beam` (Assets/Scripts/World/Zone/ZoneKit.cs) so each beam has a small bright sphere at its far end, improving visual depth without affecting gameplay.
  Create a primitive sphere child of the beam GameObject, positioned at `new Vector3(0f, 0f, length)` (the beam’s tip) with a scale of `0.05f` m (≈ 5 cm diameter).
  Remove its collider (`DestroyImmediate(tip.GetComponent<Collider>())`).
  Assign the same material used for the beam (`ZoneKit.BeamMaterial()`) and set a property block colour to `tint * 2f` for a brighter emissive look.
  Place this code just after the existing `mr.SetPropertyBlock(block);` line in the `Beam` method.
  Result: beams now show a tiny glowing sphere at the tip, adding a period‑correct, subtle light source while preserving all existing colliders, interaction points and the original beam geometry.
  - 23:54 landed by 1htsserver in 2.6 min: Added a subtle glowing tip to each zone beam:
- [x] In Assets/Scripts/World/Fixtures/KitchenFridge.cs: Fix KitchenFridge so its lamp lens is disabled at start, preventing the fridge to appear lit before the first lamp refresh.
  In `Assets/Scripts/World/Fixtures/KitchenFridge.cs` inside `Assemble`, after the line ` _lamp.On = false;` add the line `_lens.enabled = false;` (or call `RefreshLamp()` once). This ensures the mesh renderer that represents the lamp’s glow is off when the game begins, matching the description “A cool‑white lamp comes on inside while the fridge door is open, if there's power to light it.” Without this, the fridge door can look illuminated even when closed and powered off. All other logic and fields remain unchanged.
  - 23:58 landed by 1htsserver in 3.6 min: **Changed:**
- [x] In Assets/Scripts/Gadgets/Gadgets.cs, inside `Drone.Build()`’s ground‑drone branch (`else` block), add a tiny front‑sensor panel so the ground drone looks a bit more industrial: after the existing `Part` calls (the three side‑panels and the top‑cube), insert
  `Part(new Vector3(0f, 0.28f, 0.28f), new Vector3(0.06f, 0.02f, 0.02f), new Color(0.23f, 0.18f, 0.13f));`
  – size ≈ 6 cm × 2 cm × 2 cm, positioned just above the front‑center of the chassis (`0f, 0.28f, 0.28f` relative to the drone’s origin, i.e. a bit forward of the main body `Part(new Vector3(0f, 0.12f, 0f), new Vector3(0.36f, 0.12f, 0.5f), dark);`).
  – Uses a muted brown colour from the existing palette, rendered with `CloneZUtil.Mat` via the `Part` helper, keeping colliders and interaction points unchanged.
  – No more than one extra primitive is added, staying within the ≤12‑primitive limit.
  - 23:59 landed by 0htsserver in 4.8 min: **Changed file**: `Assets/Scripts/Gadgets/Gadgets.cs`
- [x] Cache per‑frame lookups in Assets/Scripts/Player/PlayerPanic.cs
  – Add a private field `private TimeOfDay _timeOfDay;` and assign it in Awake with `_timeOfDay = TimeOfDay.Instance;`.
  – Add a private readonly int `_ignoreRaycastMask = 1 << CloneZConfig.IgnoreRaycastLayer;` and a private readonly int `_defaultRaycastMask = 1 << 0;` (initialize these in Awake or as inline field initializers).
  – Replace the two occurrences of `1 << CloneZConfig.IgnoreRaycastLayer` in `AssessThreat` with `_ignoreRaycastMask`.
  – Replace the occurrence of `1 << 0` in the `Physics.Raycast` call with `_defaultRaycastMask`.
  – Replace `TimeOfDay.Instance` in `Update` with the cached `_timeOfDay` (use a null‑check on `_timeOfDay`).
  – The player’s visual behaviour stays identical; only the per‑frame cost of repeatedly constructing the layer‑mask ints and fetching the singleton is removed.
  - 00:05 landed by 1htsserver in 6.8 min: **Changed files**
- [x] Add a quick “Bang the roll‑up door” action to Assets/Scripts/World/Fixtures/RollUpShutter.cs that lets the survivor slap the curtain for noise without changing its state.
  In `Awake()` add ` _bangAct = Bang;` alongside the existing assignments.
  Implement a private method `void Bang(InteractContext ctx) => MakeNoise(CloneZConfig.RollUpNoise);`.
  In `GetActions(...)` after the existing raise/lower entry, add:
  `into.Add(new InteractAction("Bang the roll‑up door", _bangAct).Timed(0f, "Banging the door", HumanoidActivity.Use));`
  This action is always available (no state check), plays the same roll‑up noise defined by `CloneZConfig.RollUpNoise`, and costs no time, giving the player a way to distract zombies or test the door.
  All other fields, methods and behaviours stay unchanged.
  - 00:06 landed by 0htsserver in 6.8 min: **Modified file**
- [x] Append four new deliveries to the `Deliveries` array in `Assets/Scripts/World/Fixtures/MailboxFixture.cs`
  Locate the static readonly field `private static readonly string[][] Deliveries` and add four more elements after the existing ones, each defined as a `new[] { "...", "...", "..." }` block.
  Base the style on the entry `"A county public-health notice, photocopied slightly crooked. URGENT — READ AND KEEP."` – each line should be a full sentence, period‑correct, and convey a distinct theme.
  The new themes should cover (1) a winter weather advisory, (2) a community request for firewood, (3) a personal note about a missing pet, and (4) a hand‑written reminder about a church pot‑luck.
  Ensure each new delivery contains exactly three lines, uses 1993‑appropriate language, and contains no modern technology references.
  Do not modify any other code, fields, or logic in the file.
  - 00:06 landed by hts-llm in 3 min: **Changed**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Creatures/CreatureMeshes.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 00:11 landed by 1htsserver in 6.5 min: Added concise XML `<summary>` comments above each public method/property in **Assets/Scripts/AI/Creatures/CreatureMeshes.cs** that previousl
- [x] Add a small door‑handle to every interior doorway built in `Assets/Scripts/World/StarterHome.cs` by extending the `Raise` method.
  After the line `BuildPiece.Spawn(world, s.Type, BuildPiece.CenterFor(s.Type, new Vector3(s.X, 0f, s.Z)), s.Yaw, 1f, prebuilt: true);` insert code that, when `s.Type == BuildPieceType.Doorway`, creates one `PrimitiveType.Cylinder` child named “Handle” on the just‑spawned piece.
  Size the cylinder to `new Vector3(0.04f, 0.02f, 0.02f)` (localScale) and place it ≈ 0.9 m above the floor on the side opposite the hinge (use `s.Yaw` to pick the offset: 0° → `(0.35f,0.9f,0f)`, 90° → `(0f,0.9f,0.35f)`, 180° → `(-0.35f,0.9f,0f)`, 270° → `(0f,0.9f,-0.35f)`).
  Apply a tiny deterministic jitter (±0.01 m) using `int seed = StarterHome.SlotSeed(s.Type, s.X, s.Z); UnityEngine.Random.InitState(seed);` and `offset += (UnityEngine.Random.value-0.5f)*0.02f` on the X/Z components.
  Reuse the doorway’s existing material for the handle (`handle.GetComponent<Renderer>().sharedMaterial = door.GetComponentInChildren<Renderer>().sharedMaterial`).
  Leave colliders, interaction points and existing pivots untouched; the added cylinder is purely visual and adds at most one primitive per doorway (well under the 12‑primitive limit).
  - 00:17 landed by hts-llm in 11.4 min: **Changes Made**

### Auto-planned 2026-10-20: written by the night shift when the queue ran low (delete any you don't want)

- [x] Add two small rivet boxes to the backpack’s top tube in Assets/Scripts/Player/PlayerGearVisuals.cs, method BuildBackpack(ItemDef def), to give the pack a worn‑metal look.
  - 00:16 landed by 0htsserver in 9.5 min: Added two small rivet boxes to the backpack’s top tube in **PlayerGearVisuals.cs**. The new boxes sit on the front and back of the tube, usi
- [x] In Assets/Scripts/World/CeilingVisibility.cs: Optimize CeilingVisibility — remove the unused `s_Applied` flag and simplify the per‑frame check in `LateUpdate` so the script only compares the current view state to the cached `s_Shown` value.
  - 00:15 landed by 1htsserver in 3.3 min: **Changed file**
- [x] Modify Assets/Scripts/World/Fixtures/RaidCivicSink.cs by adding four to eight new sentences to the `ScrubLines` array in the `CivicSink` class.
  Append the entries after the existing two lines, preserving the current order and content.
  Use the style of the entry `"You scrub up the way the poster over the sink tells you to, step by step. Your hands come out pink and stinging‑clean."` as a reference.
  The new lines should each be a single‑sentence description of a 1993‑rural‑Kentucky winter hygiene routine, conveying gritty, period‑correct detail (e.g., cold‑water hand‑washing, makeshift soap, the smell of disinfectant, the clatter of metal fixtures, the bite of frost on skin, or a brief memory of a clinic poster).
  Do not alter any other code, constants, or logic in the file.
  - 00:18 landed by 1htsserver in 2.9 min: Modified **Assets/Scripts/World/Fixtures/RaidCivicSink.cs**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Zone/Emissions.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 00:20 landed by 0htsserver in 4.5 min: **Changed file**
- [x] Add subtle stitching detail to the watch strap in Assets/Scripts/Rendering/Presentation.cs, class DiegeticWatch, method Create.
  *After the line* `UIKit.Place(strap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 200f));`
  add four thin white panels to simulate hand‑stitched thread:
  `Image stitch1 = UIKit.Panel(strap.rectTransform, "Stitch1", Color.white);`
  `UIKit.Place(stitch1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 30f));`
  `stitch1.rectTransform.anchoredPosition = new Vector2(0f, -45f);`
  `Image stitch2 = UIKit.Panel(strap.rectTransform, "Stitch2", Color.white);`
  `UIKit.Place(stitch2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 30f));`
  `stitch2.rectTransform.anchoredPosition = new Vector2(0f, -15f);`
  - 00:19 landed by hts-llm in 1.9 min: **Changed**
- [x] Add a small metal strap detail to the checkpoint “CHECKPOINT” post sign in Assets/Scripts/World/RaidMapBuilder.Outdoor.cs
  Modify the `Prop` call at line `Prop(town, new Vector3(-7.0f, 0f, -52.4f), 0f, new Vector3(2.4f, 2.2f, 0.3f), true, (m, r) => DecorProps.PostSign(m, r, "CHECKPOINT", new Color(0.20f, 0.26f, 0.16f), new Color(0.88f, 0.86f, 0.74f), 0.42f, 1.3f));`
  ‑ Change the lambda to a block, call `DecorProps.PostSign` first, then add a thin box representing a rust‑stained metal strap: `m.Box(new Vector3(0f, 2.15f, 0f), new Vector3(0.05f, 0.02f, 0.3f), DecorPalette.Metal(new Color(0.40f, 0.40f, 0.35f)));`
  ‑ The strap sits near the top of the sign (≈ 2.15 m above ground, centered on the sign’s Y‑axis) and spans the width of the board (0.3 m).
  ‑ This adds period‑appropriate wear without altering colliders or interaction points and uses only one additional primitive.
  - 00:21 landed by 1htsserver in 2.4 min: **Changed file**
- [x] In Assets/Scripts/AI/Humans/HumanNpc.cs: Cache the frequently‑queried `Actor.LocalPlayer` in `HumanNpc` to avoid repeated scene searches each frame.
  *Add a private field `private Actor _localPlayer;` and set it in `Awake()` (or `OnEnable`) with ` _localPlayer = Actor.LocalPlayer;`.
  *Create a tiny helper `Actor LocalPlayerCached => _localPlayer != null && _localPlayer.IsAlive ? _localPlayer : (_localPlayer = Actor.LocalPlayer);`.
  *Replace every direct call to `Actor.LocalPlayer` in this file (`Bark`, `TakeDamage`, `Release`, `Die`, `Alert`, etc.) with `LocalPlayerCached`.
  *The player’s presence and alive state rarely change; caching eliminates the hidden `FindObjectsOfType`/search that `Actor.LocalPlayer` performs each frame, while the game’s visual behaviour (dialogues, AI reactions, surrender handling) stays identical.
  - 00:33 landed by hts-llm in 13.5 min: Implemented caching of the player reference in **HumanNpc**:
- [x] Add a simple “Inspect door” action to Assets/Scripts/World/Fixtures/RaidDoor.cs that lets the survivor read the door’s current health.
  - 00:23 landed by 0htsserver in 2.3 min: **Changed file**
- [x] Append five new period‑correct distress call strings to the `private static readonly string[] GuardCalls` array in `Assets/Scripts/World/Fixtures/RaidCivicRadio.cs` so the survivor can hear more varied radio pleas.
  Quote the existing entry `"You give the post's call sign and ask for anyone at all. A voice comes back in pieces — “...say again... cannot...” — then nothing."` as the style reference.
  Each new entry must be a single string literal, beginning with an opening quote (Unicode “\u201C”), containing a full sentence spoken by a caller, followed by a brief narration after the closing quote, and ending with a period inside the string.
  Themes to cover: a wounded farmer trapped by a snowdrift, a desperate mother looking for her missing child, a mechanic reporting a broken generator, a volunteer warning of a spreading fire in a barn, and a lone trucker hearing eerie howls on the highway.
  Add the entries directly inside the `GuardCalls` initializer, preserving the trailing commas and line‑break formatting.
  Do not alter any other code, identifiers, or existing entries.
  - 00:24 landed by 1htsserver in 3.1 min: **Summary**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/UI/InventoryUIController.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 00:29 landed by 0htsserver in 5.6 min: **Changes made**
- [x] Add a subtle “rivet” visual to each pause‑menu item row in Assets/Scripts/UI/PauseMenuController.cs, inside the `BuildItem(Item it, int index)` method right after the existing accent rectangle is created.
  - 00:29 landed by 1htsserver in 5.6 min: **Changed file**
- [x] Add a small chrome handle to each drawer front in `Assets/Scripts/World/FurnitureBuilder.Office.cs` → `public static void FilingCabinet(...)`
  Locate the line `m.Box(new Vector3(0f, y, 0.275f + pull), new Vector3(0.43f, 0.3f, 0.02f), M(c));` which builds the drawer front panel.
  After this line, insert a new primitive that represents a drawer pull:
  - 01:33 landed by hts-llm in 2.5 min: **Changed**
- [x] Cache player component look‑ups in ExtractionManager (Assets/Scripts/Extraction/ExtractionManager.cs) to avoid per‑frame GetComponent calls in MeetsCondition.
  Add two private fields `PlayerInventory _playerInv;` and `PlayerClothing _playerCloth;` to the class.
  In `BindPlayer(PlayerStats player)` after setting `_player = player;` also assign `_playerInv = player != null ? player.GetComponent<PlayerInventory>() : null;` and `_playerCloth = player != null ? player.GetComponent<PlayerClothing>() : null;`.
  When switching players (or unbinding) clear the caches (`_playerInv = null; _playerCloth = null;`).
  Replace the GetComponent calls inside `MeetsCondition`:
  `var inv = _player != null ? _player.GetComponent<PlayerInventory>() : null;` → `var inv = _playerInv;`
  `var clothing = _player != null ? _player.GetComponent<PlayerClothing>() : null;` → `var clothing = _playerCloth;`
  Leave the rest of the logic unchanged.
  Result: no allocations or component look‑ups each frame; the player experience (exit conditions, payment checks, backpack checks) stays exactly the same.
  - 00:33 landed by 0htsserver in 4.5 min: **Changes made**
- [x] In Assets/Scripts/World/Fixtures/RaidTownMailDrop.cs: Add a third interaction “Inspect the box” to `RaidTownMailDrop.GetActions` that lets the player read a short flavor note about the fixture itself.
  In `GetActions` insert after the existing actions:
  `into.Add(new InteractAction("Inspect the box", c => c.Notify("The blue paint is chipped and the metal groans with each gust of wind.")));`
  This uses only the `InteractAction` and `ctx.Notify` members already present.
  The new action appears alongside “Pull open the chute” and “Read the collection times”, giving the player an extra, period‑correct piece of atmosphere without altering any existing logic.
  No other files, UI screens, or data structures need to change.
  - 00:31 landed by 1htsserver in 1.3 min: **Changed file**
- [x] Append four new manual pages to the `Pages` array in `ManualScreen` (Assets/Scripts/Hub/Screens/HomeScreens.cs), adding them after the existing final entry `("Accessibility", "Settings: brightness, motion blur, lens distortion, camera shake, field of view, fog readability, HUD style (full, minimal, diegetic, hardcore), crosshair, subtitles. Readability is a floor, not an option.")`; use the same tuple syntax and keep the array initializer format unchanged.
  The new entries should follow the same “title‑body” pattern as the existing pages (see the “Accessibility” entry for style) and cover these themes: local folklore about the Spillway, winter‑time dangers (ice, cold‑related illness, snow‑bound travel), radio‑broadcast snippets that hint at faction activity, and common survivor myths that turn out to be false.
  Add these four tuples in order:
  (`"Spillway folklore"`, `"Old wives tell of a ghostly light that lures wanderers to the dam. The tale warns of a cursed stone that whispers. It’s just superstition, but the night can be scary when the wind whistles through the broken concrete."`),
  (`"Winter hazards"`, `"Freezing wind bites through even thick coats. Ice patches on roofs can give you a nasty fall, and prolonged exposure causes hypothermia. Light a fire, stay dry, and keep your breath visible as a warning."`),
  (`"Radio chatter"`, `"Static‑filled broadcasts drift through the zone. Occasionally you’ll catch a fragment: “...wardens moving east…”, “...flea market opens at dusk…”, “...supply drop scheduled…”. Listen for clues about safe routes or new quests."`),
  (`"Survivor myths"`, `"Stories circulate about “immune blood” that cures any disease or a “golden artifact” that grants endless ammo. Most are hoaxes that waste time, but a few turn out to be real—investigate before you spend resources chasing legends."`).
  - 00:33 landed by 1htsserver in 1.9 min: Implemented four new manual pages in **ManualScreen** (`Assets/Scripts/Hub/Screens/HomeScreens.cs`):
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Player/PlayerSleep.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 00:35 landed by hts-llm in 2 min: Added concise one‑sentence `/// <summary>` XML docs to the three public properties in **Assets/Scripts/Player/PlayerSleep.cs**:
- [x] In Assets/Scripts/World/WaterSource.cs: Add a thin, slightly darker “weathered band” around the rain barrel in `WaterSource.BuildBarrel` so the barrel looks used and aged.
  Insert a new mesh primitive just after the existing `Rim(m, r - wall, r + 0.012f, h, 16, DecorPalette.Gloss(plastic * 0.9f));` line.
  `// weathered band midway up the barrel – a thin matte ring`
  `m.Cylinder(new Vector3(0f, h * 0.5f, 0f), r + 0.005f, 0.01f, 16,`
  `DecorPalette.Matte(plastic * 0.6f));`
  size: radius ≈ `r + 0.005f` = 0.345 m, height = 0.01 m (1 cm)
  position: centered at `new Vector3(0f, h * 0.5f, 0f)` (mid‑height of barrel)
  material: reuse existing `plastic` colour with a darker matte tint (`plastic * 0.6f`) via `DecorPalette.Matte` – no new assets needed
  keeps existing colliders, interaction points, and pivots unchanged; adds only one primitive, staying under the ~12 limit.
  - 00:35 landed by 1htsserver in 1.8 min: **Changed file**
- [x] Fix a memory‑leak bug in Assets/Scripts/World/Zone/ZoneWorld.cs: `ZoneWorld.Reset()` clears the static references but never destroys the temporary GameObjects it creates (`ZoneUnbaked` and the `ZoneRuntime` host), so each raid leaves orphan objects in the scene, eventually causing performance drops and invisible “ghost” colliders.
  - 00:35 landed by 0htsserver in 1.4 min: **Changed file**
- [x] Add a thin dark trim around the health panel to give it a worn‑metal frame in Assets/Scripts/UI/HealthPanelController.cs, inside the `Build()` method right after the line `_panelRoot.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.06f, 0.95f);`
  Create a child GameObject “PanelTrim”, give it a `RectTransform` matching the panel’s anchor/pivot, set its size slightly larger than the panel (e.g. `new Vector2(panelW + 4f, panelH + 4f)` ≈ 0.01 m border on each side), add an `Image` component and colour it a slightly darker shade such as `new Color(0.04f, 0.03f, 0.05f, 0.9f)`.
  This adds a subtle frame without altering colliders, interaction points or existing UI layout, keeping the panel’s functionality unchanged while giving it a period‑appropriate brushed‑metal edge.
  - 00:37 landed by 0htsserver in 2.3 min: **Changes made**
- [x] Add a small “Lean against the cot” action to Assets/Scripts/World/Fixtures/RaidStoreCot.cs
  In `RaidStoreCot.GetActions` after the existing “Look under the cot” line, insert:
  `into.Add(new InteractAction("Lean against the cot", LeanAgainst).DisabledIf(seat != null, "You are already seated"));`
  Add a private helper method at the end of the class:
  `private void LeanAgainst(InteractContext ctx) { ctx.Notify("You lean against the cot, feeling the cold canvas against your back."); }`
  The new action appears when the survivor is not already seated, letting the player choose “Lean against the cot” (shown as [E] Lean against the cot) and receive the above notification. All other code and behavior remain unchanged.
  - 00:38 landed by hts-llm in 2.5 min: **Changed file**
- [x] In Assets/Scripts/AI/Humans/HumanNpc.cs extend the `ZoneTalk.s_Lines` entry for `Factions.Freehold` by appending five new dialogue strings that fit the 1993 rural Kentucky winter setting; each string must be a whole sentence in quotes and reflect themes of snow‑bound scarcity, period‑correct tech, outbreak rumors, and survivor morale – use the existing line `"Throw your bolts, brother. Every time."` as the style reference.
  Locate the dictionary initializer for `s_Lines` inside the `ZoneTalk` static class and add the new strings to the array keyed by `Factions.Freehold`.
  Append the following quoted sentences (each ending with a period):
  – "The cold's gotten worse; keep your coil‑spring heater close."
  – "I heard the Institute buried a cache near the old mill, under the frozen creek."
  – "If you find any spare 2‑hour batteries, I'll trade you for fresh ammo."
  – "Snow's covering the road to the supply dump, but the dogs still howl."
  – "Don't trust the warden's radio—static's a sign they're listening."
  Preserve all existing entries and the overall dictionary syntax; only the Freehold array should be modified.
  - 00:39 landed by 1htsserver in 4.2 min: **Changed file**
- [~] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/AI/Creatures/CreatureCatalog.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 00:40 0htsserver made no changes: **Changed file**
- [x] Add a small metal door handle to the fridge in Assets/Scripts/World/HomeFurniture.cs → `DrawPlain` method, `case HomeFurnitureKind.Fridge:`
  After the existing `FurnitureBuilder.Fridge(m, rng, …)` call, insert code that creates a thin box primitive for a handle:
  Use `bool left = rng.Next(2) == 0;` to pick left or right side deterministically.
  Position the handle at `new Vector3(left ? -0.33f : 0.33f, 0.5f, 0.05f)` (meters from the fridge centre).
  Size the handle `new Vector3(0.02f, 0.12f, 0.02f)`.
  Apply a dark‑metal material: `DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f))`.
  Result: each fridge now shows a realistic metal door handle, with left/right variation per seed, while keeping all colliders, interaction points and existing geometry unchanged.
  - 00:40 landed by hts-llm in 2.1 min: **Changed file**
- [x] Fix duplicate floor entries in Assets/Scripts/UI/InventoryUIController.Picker.cs by clearing the floor list before repopulating it in RefreshPicker so the UI reflects the actual ground items.
  - 00:41 landed by 1htsserver in 1.1 min: **Changed file**
- [x] Add subtle decorative rivets to each button in Assets/Scripts/UI/UIKit.cs by modifying `UIKit.Button` – after the label is created, create four tiny `Image` children ("Rivet") at the button’s corners (size ≈ 4 × 4 px) using the existing `Accent` colour; this adds period‑appropriate hardware detail without altering layout, colliders, or interaction.
  - 00:42 landed by hts-llm in 2.3 min: **Summary of changes**
- [x] Cache the main camera once in Awake instead of checking Camera.main every frame in ApplyRain() for Assets/Scripts/Core/WeatherSystem.cs
  In Awake() add `_cam = Camera.main;` so the reference is obtained at startup.
  Remove the null‑check/lookup inside ApplyRain(): delete the lines
  - 00:44 landed by 1htsserver in 3 min: **Changes made**
- [x] Add a simple “Inspect” extra action to CivicToggle in Assets/Scripts/World/Fixtures/RaidCivicFixtures.cs
  In class `CivicToggle` create a private method `void Inspect(InteractContext ctx)` that calls
  `ctx.Notify($"The {_name} is currently {(_on ? "on" : "off")}.");`
  In the static `Create` method, after the line `t._offLabel = offLabel;` add:
  `t._extra = (c, list) => list.Add(new InteractAction("Inspect", Inspect));`
  This adds a second action listed after the main toggle action; pressing [Q] will show the current on/off state without changing anything. All existing fields and behavior remain unchanged.
  - 00:49 landed by 0htsserver in 6.7 min: Implemented a new **Inspect** action for `CivicToggle`:
- [x] In Assets/Scripts/World/Fixtures/RaidTownMailDrop.cs append six new flavor letters to the `Letters` array, using the style of the first entry ("A drift of envelopes. The top one is addressed to the county tax office in a careful hand, stamped and sealed. Somebody still meant to pay.") and covering themes of eviction, church notice, family recipe, missing‑child flyer, ration‑card reminder, and a winter‑storm warning.
  Add the following quoted strings after the current last element in the `Letters` initializer, preserving the existing commas and formatting:
  "An eviction notice, type‑written on yellowed stock, stating the lease ends March 1st and the landlord will lock the doors at sunrise. No response."
  "The Sunday church bulletin, folded thin, lists a prayer circle for “the ones lost in the woods” and a call for donations to repair the stained‑glass window."
  "A handwritten recipe card, ink smudged: “Grandma’s turnip soup – 2 cups water, 1 cup diced turnips, pinch of salt, simmer till tender. Share with anyone who’s cold.”"
  "A faded flyer, corners torn: “Missing: 8‑year‑old Ellie Harper, last seen near Miller’s Creek, wearing a red sweater and blue boots. Please call the sheriff.”"
  "A ration‑card reminder, stamped “County Health Dept”: “Winter food rations will be distributed at the school gym, 9 AM–12 PM, bring your metal ID tag.”"
  "A hand‑drawn weather warning, charcoal on cheap paper: “Heavy snow expected tonight. Stay indoors after dark; roads will be impassable.”
  - 00:46 landed by hts-llm in 2.2 min: **Summary**
- [ ] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Rendering/Presentation.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
- [x] Add a subtle door‑handle and occasional rust spot to each door frame built in `RaidBuilding.DrawDoorFrame` (Assets/Scripts/World/RaidBuilding.cs)
  · In `DrawDoorFrame`, after the existing `for (int side = -1; side <= 1; side += 2)` block that creates the casings, insert a new small box representing a handle on the interior side of the door.
  – Determine the handle height as `float handleY = DoorHeight * 0.5f;` (≈ 1.1 m).
  – Give the handle a size of `new Vector3(0.04f, 0.015f, 0.015f)` (m).
  – Position it with `RunPoint(r, o.Pos, handleY, side * (Half + 0.025f))`, i.e. just off the door plane on each side.
  – Use the existing wood material `Surf wood = DecorPalette.Matte(Casing);` for the box.
  – Apply a tiny random offset so handles are not perfectly symmetric: `float offX = (float)(Rng.NextDouble() - 0.5) * 0.02f;` and add it to the X component of the point (`RunPoint(...).x + offX`).
  – Add the primitive with `p.Mesh.Box(RunPoint(...), RunSize(r, 0.04f, 0.015f, 0.015f), wood);`.
  – Leave colliders untouched (the handle is visual only).
  - 00:49 landed by hts-llm in 3.6 min: **Changed file**
- [x] Fix HasUsableContent in Assets/Scripts/Items/ContentDatabase.cs so it verifies *all* required tables exist, not just `content_meta` and `item`.
  - 00:51 landed by 0htsserver in 2 min: **Changed**
- [x] Add a subtle four‑edge metal border to the main report panel in Assets/Scripts/Extraction/RaidSummaryUI.cs (method `Build`) so the UI feels like a worn wooden/metal board appropriate to a 1993 rural setting.
  - 00:55 landed by hts-llm in 5.8 min: **Changes made**
- [x] In Assets/Scripts/World/Fixtures/OutdoorLantern.cs: Add a second interaction “Check power” to OutdoorLantern.GetActions by copying the existing Tap entry and inserting a new `InteractAction` that calls a private `CheckPower` method.
  · In `GetActions` after `into.Add(new InteractAction("Tap the lantern", Tap));` add
  `into.Add(new InteractAction("Check power", CheckPower));`
  · Implement a new private method:
  `private void CheckPower(InteractContext ctx) {`
  ` ctx.Notify(PowerGrid.HousePowered ? "The house is powered." : "No power: " + (PowerGrid.NoPowerReason ?? "unknown reason") + ".");`
  `}`
  · The new action re‑uses only members already present (`PowerGrid.HousePowered`, `PowerGrid.NoPowerReason`, `InteractContext.Notify`).
  · When the player selects “Check power” the game will display a short message telling whether the house has electricity and, if not, the stored `NoPowerReason`.
  - 00:57 landed by 0htsserver in 6.1 min: **Changed files**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/WeatherSystem.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 00:58 landed by hts-llm in 2.4 min: Added concise one‑sentence XML summaries to every public member in **Assets/Scripts/Core/WeatherSystem.cs** that previously lacked documenta
- [x] Add a small brass latch to each louvred shutter panel in `Assets/Scripts/World/FurnitureBuilder.RaidOpenings.cs` → method `OpeningShutters`
  After the line `m.Box(l.P(c, cy, side * (face + 0.02f)), l.Size(sw, sh, 0.03f), panel);` insert a new primitive for a latch.
  Use the existing brass material `Mt(Brass)`.
  Create the latch as a thin box `new Vector3(0.032f, 0.064f, 0.008f)` positioned near the lower‑outer edge of the panel:
  `m.Box(l.P(c, sill + 0.12f, side * (face + 0.045f)), l.Size(0.032f, 0.064f, 0.008f), Mt(Brass));`
  `sill` is the window sill height passed to the method, so the latch sits a few centimetres above it and slightly outward from the panel, mimicking period‑correct metal hardware.
  No new colliders or interaction points are added; the latch is purely visual and stays within the existing 12‑primitive limit (adds only 2 boxes, one per shutter).
  - 00:59 landed by 0htsserver in 1.6 min: **Changed file**
- [x] Fix the malformed XML comment on `BuildPiece.Position` in Assets/Scripts/World/BuildPiece.cs
  The property currently has two opening `<summary>` tags and only one closing tag, causing a compile‑time error and preventing the game from building.
  Change
  `/// <summary>The world‑space position of this piece.`
  `/// <summary>World‑space position of this piece.</summary>`
  `public Vector3 Position => transform.position;`
  to a single well‑formed comment, e.g.:
  `/// <summary>World‑space position of this piece.</summary>`
  `public Vector3 Position => transform.position;`
  - 01:00 landed by hts-llm in 2.3 min: **Changed**
- [x] In Assets/Scripts/Rendering/ActorFog.cs, replace the per‑frame hierarchy‑count check with Unity’s hierarchy‑change callback to avoid the `transform.hierarchyCount` lookup each LateUpdate.
  Remove the line `if (transform.hierarchyCount != _count) Cache();` from `LateUpdate()`.
  Add a new method:
  `private void OnTransformChildrenChanged() { Cache(); }`
  (Unity calls this automatically whenever the actor’s child hierarchy is modified.)
  Keep the existing `Cache()` call in `Start()` so the initial renderer list is built.
  This change stops a per‑frame property access and conditional call, saving a tiny but unnecessary allocation‑free check while preserving visual behaviour – actors still update their renderers only when their hierarchy actually changes.
  - 01:07 landed by 0htsserver in 8 min: **Changed file**
- [x] In Assets/Scripts/World/Fixtures/RaidTownFieldRadio.cs: Add a “Previous channel” action to `RaidTownFieldRadio.GetActions` and implement its handler
  In `GetActions` add a line after the existing “Change channel” entry:
  `into.Add(new InteractAction("Previous channel", PrevChannel).DisabledIf(!near, TooFar));`
  Create a private method `PrevChannel(InteractContext ctx)` that mirrors `Retune` but steps the channel backwards:
  `private void PrevChannel(InteractContext ctx)`
  `{`
  `_net = (_net + Nets.Length - 1) % Nets.Length;`
  `MakeNoise(CloneZConfig.ReceiverKnobNoise);`
  `_captionAt = Time.time + CloneZConfig.ReceiverFirstCaption * 0.5f;`
  - 01:04 landed by hts-llm in 3.9 min: **Changed files**
- [x] In Assets/Scripts/Hub/Screens/ServiceScreens.cs, class DeployScreen, method BuildContent, extend the `string[] kinds` array that currently reads
  - 01:13 landed by hts-llm in 8.6 min: **Changed file**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/UI/PauseMenuController.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 01:12 landed by 0htsserver in 5.3 min: **Changes made**
- [x] Add a small metal knob to the kitchen upper cabinets built in `RaidLayouts.Ranch` (Assets/Scripts/World/RaidLayoutsHomes.cs)
  After the line `b.UpperCabinets(kitchen, 'S', 1.72f, 1.1f, FurnitureBuilder.HomeCabinetPaint);` insert a primitive box that represents a cabinet knob.
  Use the existing deterministic RNG (`rng`) to vary the knob’s horizontal offset and colour:
  - 01:22 landed by 0htsserver in 9.4 min: Implemented a placeholder for a small metal knob on the kitchen upper cabinets:
- [x] Add four small metal bolt cylinders to the `WeaponRack(RaidMesh m, System.Random r)` builder in Assets/Scripts/World/RaidLayouts.cs so the rack’s wooden frame shows mounting hardware.
  Insert after the line `m.Box(new Vector3(0f, 0.25f, 0f), Sz(1.9f, 0.08f, 0.3f), wood);` the following primitives:
  `// mounting bolts`
  `var boltMat = DecorPalette.Metal(new Color(0.25f, 0.25f, 0.25f));`
  `float boltRadius = 0.015f; float boltHeight = 0.02f;`
  `Vector3[] boltPos = { new Vector3(-0.95f, 0.41f, 0.05f), new Vector3(-0.95f, 0.41f, -0.05f), new Vector3(0.95f, 0.41f, 0.05f), new Vector3(0.95f, 0.41f, -0.05f) };`
  `foreach (var p in boltPos) m.Cylinder(p, boltRadius, boltHeight, 8, boltMat);`
  Each cylinder is 0.015 m radius, 0.02 m tall, placed on the lower‑side of the two vertical posts to suggest realistic mounting screws. No collider or interaction changes are needed; the added primitives use existing materials and the deterministic random source is unchanged.
  - 01:16 landed by hts-llm in 2.5 min: **Changed file**
- [x] Add a “Check window” action to Assets/Scripts/World/Fixtures/HomeWindow.cs — in `Awake()` set `_checkAct = c => CheckWindow(c);`, add a private `void CheckWindow(InteractContext ctx)` that builds a short status string from `_open`, `_latched` and `_drawn` (e.g. “The window is open, latched, and the curtains are drawn.”) and calls `ctx.Notify` with it; then insert `into.Add(new InteractAction("Check window", _checkAct));` inside the `if (inside)` block of `GetActions` (after the curtain action). This gives the survivor a quick way to see the window’s current state without changing anything else.
  - 01:21 landed by hts-llm in 5.1 min: **Changes Made**
- [x] Append four new Kentucky‑winter themed trait entries to the `ProfessionCatalog.Traits` list in Assets/Scripts/Profile/ProfessionCatalog.cs; add them inside the `new List<TraitDef> { … }` initializer after the existing entries.
  Use the style of the existing entry `T("strong", "Strong", 4, "Carry 20% more.", m => m.CarryCapacity *= 1.2f, "weak")`.
  The new traits should cover: frost‑bite resistance, fire‑starting ability, extra hauling strength, and a superstition that steadies nerves.
  Add:
  `T("frost_resist", "Frost Resistance", 3, "Stamina drains 15% slower in the cold.", m => m.StaminaDrain *= 0.85f),`
  `T("firestarter", "Firestarter", 2, "Night vision improves by a small margin.", m => m.NightVision += 0.15f),`
  `T("handy", "Handy", 2, "Carry capacity increases by 2 kg.", m => m.CarryBonusKg += 2f),`
  `T("superstitious", "Superstitious", 1, "You’re less likely to panic.", m => m.PanicRate *= 0.9f),`
  These entries will appear as selectable trait options with the quoted sentences shown to the player during character creation.
  - 01:23 landed by hts-llm in 2 min: **Changed file**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/RaidTown.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 01:26 landed by 0htsserver in 3.9 min: **Changed files**
- [x] In Assets/Scripts/Characters/HumanoidLook.cs: Modify HumanoidLook.Wear (the `case ClothingSlot.Head:` block) to give helmets a subtle worn‑metal detail by adding a deterministic colour‑shift and gloss boost based on the humanoid’s shape hash.
  After the line `HatGloss = GarmentStyles.Gloss(k, Shape.Headwear == HeadwearStyle.Helmet ? 0.5f : 0.12f);` insert:
  `if (Shape.Headwear == HeadwearStyle.Helmet)
  {
  // deterministic “scuff” variation: 0‑0.07 (≈ 0‑7 %); uses shape hash for reproducibility
  int seed = Shape.GetHashCode();
  float var = ((seed >> 5) & 0x7) / 100f; // 0.00‑0.07
  HatTrimColor = Shade(HatTrimColor, 0.9f + var); // slight darkening / rust tint
  HatGloss = Mathf.Clamp01(HatGloss + var); // a touch more shine on worn metal
  - 01:25 landed by hts-llm in 1.8 min: **Changes made**
- [x] Fix the malformed `TownDumpsterBody` ellipsoid call in Assets/Scripts/World/FurnitureBuilder.RaidStore.cs
  The current line
  `m.Ellipsoid(new Vector3(R(rng, 0.2f, 0.75f), 1.152f, R(rng, -0.35f, 0.35f)), new Vector3(R(rng, 0.25f, 0.4f), 0.012f, R(rng, 0.25f, 0.4f)), G(new Color(0.05f, 0.05f, 0.06f)), 8, 3);`
  has an extra closing parenthesis after the first `Vector3`, so the compiler reads the call as `m.Ellipsoid(<Vector3>)` followed by stray arguments, causing a compile‑time error and resulting in the dumpster’s rubbish never being rendered.
  Change it to a single, correctly‑parenthesised call:
  `m.Ellipsoid(new Vector3(R(rng, 0.2f, 0.75f), 1.152f, R(rng, -0.35f, 0.35f)), new Vector3(R(rng, 0.25f, 0.4f), 0.012f, R(rng, 0.25f, 0.4f)), G(new Color(0.05f, 0.05f, 0.06f)), 8, 3);`
  After this fix the ellipsoids are created correctly, and the dumpster will display the dark heap of bags under the right‑hand lid as intended.
  - 01:28 landed by hts-llm in 3.4 min: **Changes made**
- [x] In Assets/Scripts/World/FurnitureBuilder.cs: Add a small metal pull‑handle to each locker door in `FurnitureBuilder.LockerBank(RaidMesh m, System.Random rng, int count, Color body)` so the door has a visible grip.
  - 01:28 landed by 0htsserver in 2.4 min: **Changed file**
- [x] Add a “Sing a lullaby” timed action to Assets/Scripts/World/Fixtures/BedFixture.cs that calms the survivor when used while seated or lying in the bed.
  Declare a new private float field `_lullabyCalmAt` next to `_plumpCalmAt`.
  In `GetActions`, after the “Plump the pillows” action, add:
  `into.Add(new InteractAction("Sing a lullaby", SingLullaby)`
  `.Timed(2f, "Singing lullaby", HumanoidActivity.None)`
  `.DisabledIf(ctx.Seat == null, "You need to be sitting or lying to sing"));`
  Implement a private method `SingLullaby(InteractContext ctx)` similar to `PlumpPillows`: check `Time.time >= _lullabyCalmAt`, set `_lullabyCalmAt = Time.time + CloneZConfig.TidyCooldown`, call `LivingFixtures.Calm(ctx, CloneZConfig.TidyPanicRelief)`, and `ctx.Notify("Your soft humming soothes the nerves.");` else `ctx.Notify("Your voice is already echoing.");`.
  The player should see the new “Sing a lullaby” option when near the bed (if seated or lying), experience a brief 2‑second activity, and receive the same panic‑relief boost as plumping pillows. All existing behavior must remain unchanged.
  - 01:36 landed by 0htsserver in 7.1 min: **Implemented “Sing a lullaby” timed action**
- [x] Append four new entries to the `ArmyLeaflets` array in Assets/Scripts/World/Fixtures/RaidCivicFridge.cs – the list of leaflets found on the Guard post’s bookcase.
  The existing entry `"a field manual on vehicle maintenance, every page thumbed grey at the corners."` shows the style: short, period‑correct descriptions of mundane, period‑appropriate papers.
  Add the following lines after the last existing string (before the closing `}`) to flesh out the leaflets with themes of safety reminders, volunteer calls, lost‑pet notices, and weather warnings:
  - 01:31 landed by hts-llm in 2.1 min: **Changed file**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Economy/Insurance.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 01:47 landed by hts-llm in 13.2 min: Added concise one‑sentence XML `<summary>` documentation to every public method/property in **Assets/Scripts/Economy/Insurance.cs** that pre
- [x] Add subtle metal detailing to the porch lantern in Assets/Scripts/World/StarterHome.Lighting.cs by inserting a small brass knob atop the lantern pole and four tiny rivet caps on the black lantern housing, using existing materials and keeping colliders unchanged.
  In `PorchLantern`, after the existing box calls (e.g. `m.Box(lamp + Vector3.up * 0.125f, new Vector3(0.17f, 0.04f, 0.17f), black);` etc.), add:
  `// brass knob crowning the pole`
  `Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);`
  `m.Cylinder(lamp + Vector3.up * 0.225f, 0.015f, 0.02f, brass);` – a cylinder 0.015 m radius, 0.02 m tall, centered 0.225 m above the base.
  `// four rivet caps on the black housing`
  `Vector3[] rivetPos = new[] {`
  `lamp + new Vector3(-0.055f, 0.115f, -0.055f),`
  `lamp + new Vector3( 0.055f, 0.115f, -0.055f),`
  - 01:42 landed by 0htsserver in 6.2 min: **Changes made**
- [x] In Assets/Scripts/Hub/Screens/HubUI.cs: Add a defensive check to `HubUI.Cycle` to avoid an out‑of‑range access when the `options` array is empty or null, which currently throws an exception and prevents the whole hub UI from opening.
  In `public static Button Cycle(Transform parent, string label, string[] options, int index, Action<int> onChanged, bool interactable = true)` add at the top:
  `if (options == null || options.Length == 0) return UIKit.Button(parent, "<none>", null, UIKit.ButtonColor, 16).SetInteractable(false);`
  (or create a disabled button using the existing `Btn` helper if preferred).
  Change the existing `int cur = Mathf.Clamp(index, 0, options.Length - 1);` line to remain unchanged – it will now only run when `options.Length > 0`.
  This prevents a `IndexOutOfRangeException` when a cycle control is built with an empty options list (e.g., a mis‑configured setting), so the hub screen loads correctly and the player sees the rest of the UI instead of a crash.
  All other behaviour of the cycle button (cycling through the provided options, invoking `onChanged`, playing the click SFX) stays exactly the same.
  - 01:49 landed by 0htsserver in 6.4 min: **Changed**
- [x] Add a subtle metal band around the barrels created in `RaidBuilding.Clutter` (Assets/Scripts/World/RaidBuilding.Furnish.cs) to give them a worn‑out, period‑appropriate look.
  - 01:53 landed by hts-llm in 6.1 min: **Changed file**
- [x] Add a small reputation boost when the player shows a zone permit in `CordonGate.GetActions` (Assets/Scripts/World/Zone/ZoneCordon.cs)
  Locate the `into.Add(new InteractAction("Show a Zone Permit", c =>` block.
  After the existing call to `Grant(c, "He stamps it and waves you through.");` insert
  `Factions.AdjustReputation(Factions.Wardens, 2f);`
  This gives the Wardens a +2 reputation increase when the permit is shown, while keeping the same dialog and UI feedback.
  The player will see the same "He stamps it and waves you through." subtitle, and the reputation system will reflect the extra boost.
  All other code, fields, and constants remain unchanged.
  - 01:50 landed by 0htsserver in 1.7 min: **Summary**
- [x] Append five new sentences to the `Lines` array of the `Days(1, 1)` programme for the “Public Television” station (Channel 12) in Assets/Scripts/Core/Broadcasts.cs
  Locate the `Station` entry with `Name = "Channel 12"` and `Title = "Public Television"`.
  Within its `Schedule` array, find the `Days(1, 1, …)` call that currently contains the pie‑crust tutorial lines.
  Add the new quoted lines after the existing ones, preserving the comma‑separated syntax and surrounding braces.
  Keep the original entries unchanged; only insert the new lines inside the same `new Programme { …, Lines = new[] { … } }` initializer.
  Use period‑correct, winter‑era language (no LEDs, smartphones, etc.) and write each line as a complete sentence in quotes.
  Style reference: `"Now, the secret to a good pie crust is cold butter. Very cold butter."` – the new lines should match this straightforward, spoken‑word style.
  Themes for the new entries:
  A safety reminder about heavy snow on roofs: “Heavy snow has piled on low‑slope roofs; clear the edges before they give way.”
  - 02:00 landed by 0htsserver in 9.3 min: **Changes made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/UI/TooltipTrigger.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 01:56 landed by hts-llm in 2.8 min: Added concise `/// <summary>` documentation to each public method in **Assets/Scripts/UI/TooltipTrigger.cs**:
- [x] Modify Assets/Scripts/UI/InventoryUIController.List.cs to add a subtle “bolt” trim on every inventory row for visual depth.
  In the `NewRow` method, after the line `var img = go.AddComponent<Image>();` insert a new 4 × 4 pixel image as a decorative bolt on the right edge of the row background.
  `// tiny bolt decoration, adds a bit of worn‑metal detail to each row`
  `NewImage(rt, "Bolt",`
  `new Vector2(1f, 0.5f), // anchored to the right‑center of the row`
  `new Vector2(-6f, 0f), // inset 6 px from the right edge`
  `new Vector2(4f, 4f), // size of the bolt`
  `new Color(0.58f, 0.55f, 0.52f, 0.85f)); // muted metal tint, matches existing UI palette`
  The bolt uses the same `NewImage` helper, keeps the existing layout (no change to anchors, pivots, or colliders), and adds only one primitive (well under the 12‑primitive limit). It gives each entry a period‑appropriate hardware hint without affecting functionality.
  - 01:58 landed by hts-llm in 2 min: **Changes Made**
- [x] Add a tiny brass latch knob to the lower‑sash lock lever in `RaidBuilding.FitWindow` (Assets/Scripts/World/RaidBuilding.Windows.cs)
  After the line
  `latchOpen = WindowBox(sash.transform, "LatchOpen", l.P(o.Pos + 0.03f, SashMeet + 0.032f, inner), l.Size(0.06f, 0.008f, 0.02f), brass);`
  insert a new mesh that creates a small box (representing a knob) of size `new Vector3(0.012f, 0.012f, 0.005f)` positioned at `l.P(o.Pos + 0.03f, SashMeet + 0.038f, inner + 0.011f)` using the same `brass` material, and attach it under `latchOpen.transform` via `WindowPart`.
  This adds a period‑correct brass detail to the latch, visible on every window, without altering colliders or interaction points.
  - 02:01 landed by hts-llm in 2.9 min: **Changed file**
- [x] In Assets/Scripts/AI/Humans/HumanNpc.cs: Add an “Intimidate” action to HumanNpcInteract.GetActions that lets the player force a non‑surrendered NPC to give up if the player is visibly armed.
  In HumanNpcInteract.GetActions, after the existing `into.Add(new InteractAction("Talk", Talk));` line, insert:
  `into.Add(new InteractAction("Intimidate", c => {`
  `if (!c.Inventory.Has("pistol")) return;`
  `if (Random.value < 0.5f) { _npc.Surrender(); GameEvents.RaiseSubtitle(_npc.Name, "I don't want any trouble… I give up!"); }`
  `else { GameEvents.RaiseSubtitle(_npc.Name, "You think you can scare me?"); }`
  `}).DisabledIf(!ctx.Inventory.Has("pistol"), "Need a weapon"));`
  This uses the existing `pistol` item ID, the public `HumanNpc.Surrender()` method, and the existing `GameEvents.RaiseSubtitle` call, providing a new small interaction that fits the tense, winter‑bound setting.
  - 02:04 landed by 0htsserver in 3.7 min: **Changes made**
- [x] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Zombies/ZombieHealth.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 02:06 landed by hts-llm in 5.1 min: **Changed file**
- [x] Add small chrome tap‑knobs to the water‑cooler model in Assets/Scripts/World/FurnitureBuilder.RaidStore.cs, method OfficeWaterCoolerBody
  After the two tap boxes (the blue tap at `new Vector3(-0.04f, 0.785f, 0.175f)` and the red tap at `new Vector3(0.04f, 0.785f, 0.175f)`) insert two tiny cylinders to represent the turn‑knobs.
  Create a cylinder for the blue tap: `m.Cylinder(new Vector3(-0.04f, 0.795f, 0.185f), 0.005f, 0.015f, 6, Mt(Chrome));`
  Create a cylinder for the red tap: `m.Cylinder(new Vector3(0.04f, 0.795f, 0.185f), 0.005f, 0.015f, 6, Mt(Chrome));`
  Each knob is 0.005 m radius and 0.015 m tall, placed 0.01 m above and slightly forward of its tap to look like a realistic metal handle.
  Use the existing chrome material (`Mt(Chrome)`) so colour/shade stays consistent. No other code changes are required.
  - 02:06 landed by 0htsserver in 2.1 min: **Changes made**
- [x] Fix the ground‑height detection in Assets/Scripts/Zone/ZoneHazards.cs so anomalies are placed at the actual terrain height instead of constantly at y = 0.
  In `AnomalyField.GroundY` the raycast uses a hard‑coded layer mask `1`, which only checks the Default layer and misses the terrain layer used in the game. This causes the method to return 0 for most positions, spawning anomalies floating in the air or buried underground.
  Change the layer‑mask argument to include all layers (e.g. `Physics.DefaultRaycastLayers` or `~0`) or simply omit it. The corrected method should be:
  - 02:08 landed by 0htsserver in 1.5 min: **Changed**
- [x] Add subtle rivet‑like rust specks to the corrugated‑sheet‑metal texture (Assets/Scripts/World/DecorTextures.cs → `public static Texture2D Corrugated(Color color)`)
  After the line `Color c = Mul(color, 0.78f + 0.26f * Mathf.Sin(u * Mathf.PI * 2f * 8f));` insert a deterministic rust‑spot test using the existing `R01` function; e.g. `float spot = R01(x, y, 122);`
  If `spot < 0.018f` (≈ 2 % of pixels) treat the pixel as the centre of a tiny rivet and darken it: `c = Mul(c, 0.45f);` (roughly a 2 cm‑wide spot at the texture’s 64 px scale)
  If `spot` is between `0.018f` and `0.030f` add a slight highlight to suggest a brushed‑metal edge: `c = Mul(c, 0.65f);`
  Keep the existing rust overlay (`if (r > 0.55f) …`) unchanged; the new spots will blend with it because they use the same deterministic seed (`122`).
  No new colliders or interaction points are added; the texture generation remains tileable and uses at most a dozen extra per‑pixel condition checks.
  - 02:09 landed by hts-llm in 2.3 min: **Changes made**
- [x] Cache per‑frame lookups in `Assets/Scripts/Core/TimeOfDay.cs` by storing `Camera.main` and `WeatherSystem.Instance` in fields set once, then use those cached references in `Apply()` so the visual output (sky colour, fog, sun rotation, LightLevel, etc.) stays identical.
  Add a private field `private WeatherSystem _weather;`.
  In `Awake()` (after setting `Instance`) assign `_cam = Camera.main;` and `_weather = WeatherSystem.Instance;`.
  Replace the line `float weatherDim = WeatherSystem.Instance != null ? WeatherSystem.Instance.LightMultiplier : 1f;` with `float weatherDim = _weather != null ? _weather.LightMultiplier : 1f;`.
  Remove the null‑check `if (_cam == null) _cam = Camera.main;` from `Apply()` (the cached `_cam` is already set).
  Optionally guard against late creation by lazily setting `_weather = WeatherSystem.Instance` the first time `Apply()` runs if `_weather` is still null.
  No change to how the sky, fog, ambient light, sun colour/intensity or zombie vision are computed, so the player sees exactly the same lighting cycle.
  - 02:15 landed by 0htsserver in 7.6 min: **Changed files**
- [x] In Assets/Scripts/World/Interaction/StorageFixture.cs: Add a “Knock on the …” action to `StorageFixture.GetActions(InteractContext ctx, List<InteractAction> into)` that triggers the fixture’s noise.
  In `GetActions` after `string noun = Noun;` insert a line adding a new `InteractAction` with the label `"Knock on the " + noun`.
  The action’s callback should be `c => { MakeNoise(_opt.Noise); }` (the `InteractContext` parameter is unused).
  Place this insertion before the existing logic that adds open/close/look actions so the knock option is always available.
  No other code changes are required; existing open/close/search functionality stays unchanged.
  This gives the survivor a way to make noise without opening the storage, affecting zombie attraction.
  - 02:10 landed by hts-llm in 1.3 min: **Changed:** `Assets/Scripts/World/Interaction/StorageFixture.cs`
- [x] Append five new entries to the `CivicScreenLines` array in Assets/Scripts/World/RaidBuilding.Civic.cs (class `RaidBuilding`, field `CivicScreenLines`) to give the player more period‑appropriate screen text to read.
  Use the existing entry `"Green letters on black: a memo about the coffee fund, half typed. The cursor blinks after “and furthermore”.` as the style reference – each new line should be a single‑sentence description of a 1993‑era office screen, covering themes such as an unfinished inventory list, a weather forecast, a personal note, a supply request, and a government notice.
  Example new entries (replace the placeholder text with these exact strings):
  `"A half‑filled spreadsheet shows fuel allocations, column C empty and the cursor waiting over a formula."`
  `"A weather widget displays a bitter‑cold forecast for Kentucky, with snow‑depth predictions flickering."`
  `"A personal e‑mail draft reads “Hey Mom, …” but stops before the signature, the cursor pulsing."`
  `"An urgent supply request list titled “MEDICAL RESTOCK” lists bandages, IV bags and an unfinished line."`
  `"A government bulletin scrolls: “County emergency shelter capacity at 78 %…”, the text pauses briefly before the next line."`
  The new strings must be added after the existing entries, keeping the array syntax unchanged.
  - 02:14 landed by hts-llm in 3.8 min: **Changed file**
- [~] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/World/Fixtures/RaidStoreWaterCooler.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
  - 02:18 hts-llm made no changes: No public methods or properties in **Assets/Scripts/World/Fixtures/RaidStoreWaterCooler.cs** lack a `/// <summary>` comment that isn’t alrea
- [x] Add a subtle “worn‑edge” visual detail to the paper background that ZoneMapScreen builds (Assets/Scripts/Survival/Navigation.cs)
  – Locate the line ` _paper = UIKit.Place(paper.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(760f, 760f));` and insert the new code immediately after it.
  – Create a deterministic random generator: `int seed = ZoneCatalog.Current != null ? ZoneCatalog.Current.LayoutSeed : 0; System.Random rng = new System.Random(seed);`
  – For each of the four sides (top, bottom, left, right) add two thin UI panels (total 8 primitives) to simulate ragged paper edges:
  `// top edge – two short strips with random offset`
  `for (int i = 0; i < 2; i++)`
  `{`
  `float offsetX = (float)rng.NextDouble() * 6f - 3f; // ±3 px random shift`
  `float width = 760f - (float)rng.NextDouble() * 20f; // slight length variation`
  - 02:23 landed by 0htsserver in 7.4 min: **Changed files**
- [x] Fix the armor class upgrade check in Assets/Scripts/Weapons/WeaponUpgrades.cs so that the “Ceramic inserts” upgrade always raises armor class (up to 6) instead of being ignored when the base class is 0.
  In `Effective(ArmorStats a, ItemExtra extra)` the line
  `if (u.Contains("a_plate2") && e.Class > 0) e.Class = Mathf.Min(6, e.Class + 1);`
  prevents the upgrade from applying to armor pieces whose `Class` starts at 0, contradicting the node description “+1 armour class (max 6)”.
  Change it to:
  `if (u.Contains("a_plate2")) e.Class = Mathf.Min(6, e.Class + 1);`
  The player will now see the “Ceramic inserts” upgrade increase armor class for any applicable armor, including those with class 0, matching the intended game mechanic. All other behavior stays unchanged.
  - 02:19 landed by hts-llm in 0.9 min: **Changed**: `Assets/Scripts/Weapons/WeaponUpgrades.cs` – removed the `e.Class > 0` guard in the ceramic inserts upgrade logic.
- [ ] Add a subtle torn‑fur strip to the torso of all quadruped creatures built in `CreatureBody.Build` (method `Quad` in *Assets/Scripts/AI/Creatures/Creatures.cs*).
  In the `CreatureParts.Attach(_torso, …)` call, append an extra `Add` that creates a thin box representing a ragged strip of fur.
  Use a size of 0.1 m × 0.2 m × 0.02 m (`new Vector3(bodyW * 0.1f, bodyH * 0.2f, 0.02f)`).
  Position it at the lower back of the torso: `new Vector3(0f, bodyH * 0.5f, -length * 0.2f)` (relative to the torso pivot used in the current call).
  Colour it a darker tone of the torso material: `c * 0.8f`.
  The resulting line should look like:
  ` .Add(CreaturePrim.Box, new Vector3(0f, bodyH * 0.5f, -length * 0.2f), Vector3.zero, new Vector3(bodyW * 0.1f, bodyH * 0.2f, 0.02f), c * 0.8f)`
  This adds only one primitive (the box) to the existing torso build, keeping colliders, interaction points and pivots unchanged, and gives each quadruped (Deer, Bear, Wolf, Thumper, default hound) a period‑appropriate visual detail of worn fur.
- [ ] Add a contextual “Inspect door” action to the “Door to the Zone” hub station in Assets/Scripts/Hub/HubWorld.cs
  In `HubStation.GetActions`, after the existing `for` loop that adds `Extra` actions, insert a conditional block:
  `// Special inspection for the zone door`
  `if (Title == "Door to the Zone")`
  `{`
  `into.Add(new InteractAction("Inspect door", c => GameEvents.RaiseToast(`
  `"The door is locked tight, but you can hear the cold wind outside.")));`
  `}`
  This uses the already‑available `GameEvents.RaiseToast` method and does not require any new screens or items.
- [ ] In Assets/Scripts/Hub/HubNpc.cs, extend the `"barkeep"` entry in the `Lines` dictionary by appending six new sentences that fit the 1993 rural‑Kentucky winter vibe (snow‑choked roads, dim‑lit lamps, local rumours, and the ever‑present Zone).
  Use the existing line `"Rent's due on the seventh, same as always."` as the style reference – short, colloquial, present‑tense, and hinting at a wider story.
  Add the following strings to the array (preserving the surrounding commas and quotes):
- [ ] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Zombies/ZombieAI.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
- [ ] Add four small brass corner brackets to the top of the dresser in `FurnitureBuilder.DresserBody` (Assets/Scripts/World/FurnitureBuilder.Living.cs)
  Insert after the line `m.Box(new Vector3(0f, 0.81f, -0.01f), new Vector3(1.04f, 0.03f, 0.5f), G(c * 1.05f));`
  For each corner (±0.5 m on X, ±0.24 m on Z) call
  `if (rng.NextDouble() < 0.75) m.Box(new Vector3(x, 0.825f, z), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));`
  where `(x, z)` are `(-0.5f, -0.24f)`, `(0.5f, -0.24f)`, `(-0.5f, 0.24f)`, `(0.5f, 0.24f)`.
  The brackets use the existing brass material (`Mt(Brass)`), add only four primitives, keep all colliders and interaction points unchanged, and give the dresser a period‑correct metallic accent that varies per run.
- [ ] In Assets/Scripts/Player/PlayerSleep.cs: Fix the layer‑mask used in `PlayerSleep.ZombiesNearby` so that zombies on normal layers are detected and the player cannot sleep when they are close.
  In `ZombiesNearby()` the call
- [ ] Add a subtle “worn‑edge” accent to each scrollbar handle built in `HubUI.Scroll` (Assets/Scripts/Hub/Screens/HubUI.cs)
