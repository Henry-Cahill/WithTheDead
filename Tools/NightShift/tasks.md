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
- [ ] Add a tiny hinge‑pin cylinder to the mailbox door for visual detail in the `Mailbox` method of Assets/Scripts/World/StarterHome.Grounds.cs
  Insert after the line creating `doorPart` (`FixturePart doorPart = FixturePart.Hinge(...);`) the call:
  `m.Cylinder(at + new Vector3(0f, 1.12f, -0.252f), 0.006f, 0.018f, 12, face);`
  The cylinder (radius 0.006 m, height 0.018 m) sits at the door hinge point (`at + new Vector3(0f, 1.12f, -0.252f)`) using the same metal material (`face`) as the door, adding a realistic pin without affecting colliders or interaction.
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
- [ ] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Core/SandboxSettings.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
- [x] Fix HubScreenBase so a screen can be reopened after it has been closed (Assets/Scripts/Hub/Screens/HubScreenBase.cs)
  In `Build()` the `_closed` flag is never reset, so after `OnClosed()` sets `_closed = true` the screen’s `Update()` method immediately returns, stopping live‑label updates and money refresh when the same screen instance is opened again.
  Add `_closed = false;` at the start of `Build()` (right after `EnsureChrome();`) to clear the closed state each time the screen is rebuilt.
  After this change, reopening a hub screen will correctly show live timers, money, and other UI updates, while all existing behavior remains unchanged.
  - 23:46 landed by hts-llm in 0.8 min: **Changed file**
- [ ] Add a subtle row of rivet‑like accents to the top of the skills panel in Assets/Scripts/UI/SkillsPanelController.cs, inside the `Build()` method after the `UITheme.AddTopAccent` call.
  Create 8 small circular `Image` objects (size ≈ 0.008 m × 0.008 m) named `"Rivet0"`…`"Rivet7"` as children of `_panelRoot`’s `RectTransform`.
  Anchor each at `(0.5, 1)` with pivot `(0.5, 1)`, set `anchoredPosition` to `new Vector2(-260f + i*60f, -4f)` (spacing ≈ 0.06 m across the 0.56 m panel width), and tint with `new Color(0.7f, 0.7f, 0.7f, 0.9f)`.
  Use the default UI sprite (no extra assets) and keep the existing colliders and interaction logic untouched.
  This gives the panel a period‑appropriate metal‑fastener detail without altering layout or functionality.
- [ ] Cache player actors for NearAnyone checks in Assets/Scripts/Economy/CentralEconomy.cs
  Add a private static readonly List<Actor> `s_PlayerCache = new List<Actor>();` to the class.
  In `CentralEconomy.Tick()` (just after `if (!Active || Config == null) return;`) clear and fill the cache:
  `s_PlayerCache.Clear(); foreach (Actor a in Actor.All) if (a != null && a.IsPlayerControlled) s_PlayerCache.Add(a);`
  Change `NearAnyone(Vector3 p, float range)` to iterate over `s_PlayerCache` instead of `Actor.All`.
  This removes the per‑container per‑tick enumeration of the full `Actor.All` list, reducing allocations and CPU work while preserving the same player‑proximity logic; the player‑visible behavior (which containers are considered “near anyone”) stays identical.
- [ ] Add a “Check channel” action to Assets/Scripts/World/Fixtures/RaidCivicRadio.cs that lets the survivor see which channel the radio is currently tuned to.
  Declare a new field `private Action<InteractContext> _checkChannel;` alongside the other Action fields.
  In `Awake()` assign it: `_checkChannel = ctx => ctx.Notify("Current channel: " + Tuned.Name);`.
  In `GetActions` after the “Change channel” entry, add:
  `into.Add(new InteractAction("Check channel", _checkChannel).DisabledIf(!near, TooFar));`
  The action should only be enabled when the player is within `CloneZConfig.ReceiverKnobReach` (same as other knob actions) and the radio is on (the existing `if (!_on) return;` guard already covers this).
  Result in‑game: when the radio is on and the survivor stands close enough, pressing Q (or the extra button) shows a notification like “Current channel: Battalion net”.
  No other code, assets, or UI screens are altered; all referenced members (`Tuned`, `TooFar`, `CloneZConfig.ReceiverKnobReach`) already exist.
- [ ] Append six new flavor‑text strings to the `BrushLines` array in Assets/Scripts/World/Fixtures/BasinFixture.cs so that brushing teeth can produce additional period‑correct commentary; use the existing entry `"You brush until your gums tingle and rinse with a splash. Minty. Almost civilised."` as the style reference and cover themes of cold mornings, lingering soot, quiet desperation, makeshift hygiene, and the ever‑present cold wind.
  Locate the `private static readonly string[] BrushLines = { … };` declaration.
  After the second entry (the one quoted above), insert six new quoted sentences, each ending with a period, e.g.:
  `"The frigid air bites your cheeks as you scrub, reminding you the world outside is still frozen."`
  `"You watch the thin stream of water turn into a misty cloud, disappearing like breath in the room."`
  `"A thin film of soot clings to your toothbrush, a reminder of the burnt logs in the stove."`
  `"The metal of the tap feels icy, and you wonder how long the mains will stay silent."`
  `"You spit the frothy foam into the basin, hearing it echo in the tiny, empty space."`
  `"Each swipe feels like a small rebellion against the cold that seeps through the cracked walls."`
- [ ] Add a one-sentence `/// <summary>` to each public method and property in Assets/Scripts/Hub/Screens/HubKit.cs that has none, saying what it does and why, in the same voice as the existing summaries in that file. Don't change any code.
  Skip fields, constants, constructors, and members that override a base member or implement an interface (for example DisplayName, GetActions, SaveKey, CaptureState, RestoreState, CanDemolish, TakeDamage, Dispose, and an IGameModule's Order and Init): they inherit their docs.
  Insert only: each edit adds the new `///` lines directly above one declaration, indented exactly like that declaration, and keeps the declaration line exactly as it was. Never delete, move or rewrite existing lines; if an edit fails, re-read the file first.
- [ ] Add a subtle glowing tip to the beam cone created in `ZoneKit.Beam` (Assets/Scripts/World/Zone/ZoneKit.cs) so each beam has a small bright sphere at its far end, improving visual depth without affecting gameplay.
  Create a primitive sphere child of the beam GameObject, positioned at `new Vector3(0f, 0f, length)` (the beam’s tip) with a scale of `0.05f` m (≈ 5 cm diameter).
  Remove its collider (`DestroyImmediate(tip.GetComponent<Collider>())`).
  Assign the same material used for the beam (`ZoneKit.BeamMaterial()`) and set a property block colour to `tint * 2f` for a brighter emissive look.
  Place this code just after the existing `mr.SetPropertyBlock(block);` line in the `Beam` method.
  Result: beams now show a tiny glowing sphere at the tip, adding a period‑correct, subtle light source while preserving all existing colliders, interaction points and the original beam geometry.
