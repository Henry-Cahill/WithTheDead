# WithTheDead — to-do

A detailed, evidence-based to-do list for the game, in nine categories. Each item says why it matters, where in the code
to start, what to do, and how to tell it's finished. It's a snapshot of **2026-11-05**: `main` at 76e670c plus the
uncommitted work in the tree. Line links drift as the code changes; if one lands a few lines off, search for the named
symbol.

**Key:** **P0** do first (it blocks other work or risks losing work) · **P1** next up · **P2** soon · **P3** nice to
have. Sizes: **S** hours · **M** a day or two · **L** a week or more. The type after the size says what kind of work it
is: Bug, Gap (designed or expected but missing), Content, Polish, Perf, Save, Co-op, Balance, Tech debt, QA, Docs.

## Categories

| Category | Items | P1 | P2 | P3 | Night Shift | Headline |
|---|---:|---:|---:|---:|---:|---|
| [Quests](Quests.md) | 14 | 4 | 4 | 5 | 1 | The Lodestar's wish is never made, and the first quest points away from where raids start |
| [Rendering](Rendering.md) | 12 | 2 | 6 | 4 | 0 | 2,840 items still wait for meshes; the room-light shadows are on a budget now |
| [Survival](Survival.md) | 15 | 3 | 4 | 5 | 3 | Cooking is instant, bags don't lighten loads, and the wave save forgets diseases |
| [UI](UI.md) | 18 | 6 | 4 | 5 | 3 | No display settings, UI scale, gamepad, rebinding or hotbar; the theme rework is half-done |
| [Vehicles](Vehicles.md) | 18 | 3 | 6 | 5 | 4 | The map's cars can't be driven, drivable cars have no trunk, and pumps don't pump |
| [Weapons](Weapons.md) | 15 | 3 | 8 | 3 | 1 | Sixteen guns, with period ones in the right buildings and at every trader tier; molotovs, pipe bombs and noise lures to throw; tracer and incendiary rounds that work; melee weapons that each handle their own way |
| [World](World.md) | 15 | 3 | 8 | 4 | 0 | NaN furniture, a physics workaround, streaming hitches, and fixture state across streaming and co-op |
| [Zombies](Zombies.md) | 14 | 5 | 5 | 4 | 0 | Two sandbox settings do nothing; no real hordes, and the dead ignore windows and fences |
| [Zone](Zone.md) | 16 | 3 | 5 | 4 | 4 | Kentucky is a map but not yet a Zone; upkeep vs income is untested |
| [Cross-cutting](#cross-cutting) (below) | 17 | 4 | 9 | 2 | — | Two P0s: commit the work in progress, and decide on the radio music before the next release |

The "Night Shift" column counts failed or open tasks from [Tools/NightShift/tasks.md](../Tools/NightShift/tasks.md)
filed under that category. Rendering and World fold theirs into regular items; Zombies had none. Some are marked as
safe to close because the work already exists.

## Start here

The items with the most value for the least risk, across categories:

1. **Commit the work in progress**: [below](#cross-cutting). It's P0.
2. **Decide on the radio music before the next release**: [below](#cross-cutting). It's P0.
3. **Make "Sprinter share" and "The dead return" do something**: [Zombies](Zombies.md). P1, S: two inert settings.
4. **Make the first quest work where raids now start**: [Quests](Quests.md). P1, S.
5. **Make cooking take time** and **give worn bags their weight reduction**: [Survival](Survival.md). P1, S each.
6. **Fix the fresh-clone compile failure**: [below](#cross-cutting). P1, S.
7. **Find what makes furniture with NaN vertices**: [World](World.md). P1, M.
8. **Put the room lights' shadows on a budget**: [Rendering](Rendering.md). P1, M. Done 2026-11-06.
9. **Display and graphics settings**, then **finish the theme rework**: [UI](UI.md). P1, M each.
10. **Finish the Lodestar**: [Quests](Quests.md). P1, L. It's the story's payoff.
11. **Make Kentucky a Zone** and **check the upkeep against income**: [Zone](Zone.md). P1, L and M.

## Cross-cutting

These belong to no single category.

### P0 — do first

- [ ] **Commit the work in progress** · P0 · M · Repo
  - **Why:** 24 tracked files are modified (+1,070 / −427 lines), and about 6,650 files are untracked. That includes
    whole new systems: the Java 27 scripting host ([Assets/Scripts/Java](../Assets/Scripts/Java), [Java](../Java/README.md),
    [Tools/Java](../Tools/Java), [JavaBuild.cs](../Assets/Editor/JavaBuild.cs) and [JavaSelfTest.cs](../Assets/Editor/JavaSelfTest.cs)),
    the radio stations ([MusicStations.cs](../Assets/Scripts/Audio/MusicStations.cs),
    [StationPlayer.cs](../Assets/Scripts/Audio/StationPlayer.cs), [MenuRadio.cs](../Assets/Scripts/UI/MenuRadio.cs) and
    [ServerBuildMusic.cs](../Assets/Editor/ServerBuildMusic.cs)), Debug Mode ([DebugMode.cs](../Assets/Scripts/Dev/DebugMode.cs),
    [FloatingGeometryReport.cs](../Assets/Scripts/Dev/FloatingGeometryReport.cs) and [DebugPick.shader](../Assets/Resources/DebugPick.shader)),
    the release console ([Tools/Server/Console](../Tools/Server/Console)), the stream relay
    ([Tools/Streaming/relay](../Tools/Streaming/relay)), the YouTube branding, and 659 new generated item meshes with
    their textures. All of it lives only on this PC's disk. Night Shift helper machines work from GitHub, so they
    compile against a `main` that has none of it.
  - **Do:** Branch, and commit in logical pieces (code and docs together, then the meshes on their own, through LFS,
    after the budget check below). Open a PR, as with #58 and #59.
  - **Done when:** `git status` shows only files that are meant to stay local.

- [ ] **Decide on the radio music before the next release** · P0 · S · Legal / Size
  - **Why:** `Assets/StreamingAssets/Music` holds 4 stations and 188 tracks (175 .ogg, 13 .mp3), about 2.6 GB. It's
    gitignored ([.gitignore](../.gitignore)), but [MusicStations.cs](../Assets/Scripts/Audio/MusicStations.cs) describes
    it as "shipped with the game", and only the dedicated-server build strips it
    ([ServerBuildMusic.cs](../Assets/Editor/ServerBuildMusic.cs)). The last release (20261101.1637) had no music, and
    its client zip was 1.22 GB. The next one would be about 2.6 GB bigger, and friends re-download the whole thing every
    release (no patching, [Tools/Server/README.md](../Tools/Server/README.md), "Limits"). Before builds go to anyone
    (the download page, itch.io), the rights to every track need to be clear.
  - **Do:** For each station, write down the source and licence (for example a `LICENSE.txt` next to `station.json`).
    Ship only cleared tracks. For the rest, rely on the player's own `<persistentDataPath>/Music` folder, which
    `MusicStations` already reads, and say so on the download page.
  - **Done when:** Every shipped track has a recorded licence, and the client build size is known before release.

### P1 — next up

- [ ] **Fix the fresh-clone compile failure** · P1 · S · Build
  - **Why:** Batch runs on project copies with a fresh `Library` (2026-11-01/02) failed to compile
    `com.unity.ai.assistant` 2.21.0-pre.1 on this editor ("MaterialGlobalIlluminationFlags does not contain a definition
    for BakedEmission"), and compiled only once it was removed from the copy's manifest. Every new clone, contributor
    and build machine starts with a fresh `Library`. No script in `Assets` references the package.
  - **Do:** Remove it from [Packages/manifest.json](../Packages/manifest.json) and `packages-lock.json` (or pin a
    version that compiles), then open a clean clone to confirm.
  - **Done when:** A clean clone opens and compiles with no edits.

- [ ] **Plan the move off the Unity alpha** · P1 · M · Build
  - **Why:** The project runs on 6000.7.0a2, an alpha. Alpha-only problems already cost time: the snowfield that drops
    out of the physics scene ([World.md](World.md)) and the package compile failure above.
  - **Do:** Pick a target (the 6000.7 beta or release when it's out, or an LTS) and upgrade on a branch. Run every
    smoke mode (`Tools\Smoke\Smoke.ps1`, plus host and client) and the Java self-test.
  - **Done when:** The project is on a non-alpha editor and every smoke mode passes.

- [x] **Check the Git LFS budget before pushing the meshes** · P1 · S · Repo
  - **Done (2026-11-08):** It fits; the plan is the mesh README's "Committing the meshes". Measured per map size, the
    whole catalogue comes to about 6.1 GiB (1.48 GiB for the first 3,255 versions) against 10 GiB of storage. Downloads
    are 10 GiB a month, but the Night Shift's helpers never fetch LFS files (`GIT_SAFE` in
    [nightshift.py](../Tools/NightShift/nightshift.py)) and `Release.ps1 -FromMain` shares this clone's LFS store, so
    only fresh clones count: about one full clone a month, or any number with
    `git clone -c lfs.fetchexclude=Assets/Resources/GeneratedItemMeshes`. The quota is the whole account's, and the gh
    token can't read its usage (that needs the `user` scope), so check what other repositories use once in GitHub's
    billing settings.
  - **Why:** Generated item meshes go to Git LFS ([.gitattributes](../.gitattributes)). They're 1.29 GB for 798 items
    so far, and the mesh README estimates about 7 GB for all three versions of the catalogue
    ([Tools/AssetPipeline/mesh/README.md](../Tools/AssetPipeline/mesh/README.md)). Every clone and every Night Shift
    helper pulls them.
  - **Do:** Check the GitHub account's LFS storage and bandwidth quota and what data packs cost. If it doesn't fit, keep
    the meshes on the NAS (which already holds the originals) and fetch them in a build step.
  - **Done when:** There's a written plan that fits the quota.

- [ ] **A legal check before anything goes public** · P1 · S · Legal
  - **Why:** [DESIGN_DECISIONS.md](../DESIGN_DECISIONS.md) already says to check with a lawyer before shipping anything
    commercial. Specific points:
    - The radio music (above).
    - Project Zomboid-derived reference material in the repo ([ZOMBOID_CONTAINERS.md](../ZOMBOID_CONTAINERS.md), 319 KB,
      tracked), and the MapScraper's mirror of zomboidmap.com, must stay out of shipped content.
    - OpenStreetMap's licence (ODbL) needs visible attribution. The Kentucky Zone's description has it
      ([ZoneCatalog.cs](../Assets/Scripts/Zone/ZoneCatalog.cs#L84)), but there's no credits screen, and the download page
      doesn't mention it.
    - The models behind the item art (TRELLIS.2, Z-Image-Turbo, the DINOv3 weights, gpt-oss) need licences that allow
      distributing what they make.
    - Temurin's notices already ship with the runtime ([Java/README.md](../Java/README.md)).
  - **Do:** Make the list, add a credits screen (OSM, Temurin, any music and model credits), and get an answer on each
    point.
  - **Done when:** Each point has a recorded answer and the credits screen exists.

### P2 — soon

- [ ] **CI: a compile check on every PR, smoke tests nightly** · P2 · M · QA
  - **Why:** Nothing runs automatically. There's no `.github/workflows`, and no unit tests: `com.unity.test-framework`
    is installed, but nothing in `Assets` uses `[Test]`. The compile check
    ([Tools/NightShift/.unityrefs](../Tools/NightShift/.unityrefs), built with `dotnet build`) and
    [Smoke.ps1](../Tools/Smoke/Smoke.ps1) are run by hand.
  - **Do:** Set up a self-hosted GitHub Actions runner on the Windows PC (it has Unity and a licence). Run the dotnet
    compile check on every PR, and the smoke modes nightly against `main`. Add EditMode tests for pure logic:
    ballistics, quest matching, save round-trips.
  - **Done when:** PRs show a compile result, and a nightly smoke summary is posted somewhere visible.

- [ ] **Split the code into assemblies** · P2 · L · Tech debt
  - **Why:** 534 scripts, about 216,000 lines, compile as one `Assembly-CSharp` (there are no `.asmdef` files), so every
    edit recompiles everything. `World` alone is about 110,000 lines, `Audio` about 19,000 and `Dev` about 16,000.
  - **Do:** Split out the largest, most self-contained parts first (the audio synth, `World/Map`, `Dev`, `Java`), and
    keep the Night Shift compile check's project files ([Tools/NightShift/.unityrefs](../Tools/NightShift/.unityrefs))
    in step.
  - **Done when:** An edit in `UI` no longer recompiles the map code, with times measured before and after.

- [ ] **Remove unused packages** · P2 · S · Tech debt
  - **Why:** `com.unity.visualscripting` and `com.unity.ai.inference` have no references in `Assets`, and
    `com.unity.collab-proxy` (Unity Version Control) isn't used with Git. Each adds import and compile time.
  - **Do:** Check `packages-lock.json` for anything that depends on them (ai.inference may come with ai.assistant),
    remove them, and run the smoke tests.
  - **Done when:** The manifest lists only packages the game uses.

- [ ] **Version profile saves and test migrations** · P2 · M · Save
  - **Why:** The Wave Survival save has a version number but no migration path
    ([SaveSystem.cs](../Assets/Scripts/Core/SaveSystem.cs#L22)). Profiles (runners, stash, account knowledge) are what
    friends care about, and they have a single ad-hoc migration (`MigrateOldKit`,
    [ProfileStore.cs](../Assets/Scripts/Profile/ProfileStore.cs#L187)). Skill XP is saved by position, too
    ([Survival.md](Survival.md)).
  - **Do:** Add a profile schema version and a migration step per version. Keep a copy of each release's saves, and
    test that the next build loads them.
  - **Done when:** A build loads the previous release's profiles and wave saves without loss.

- [ ] **A real company name and version** · P2 · S · Release
  - **Why:** PlayerSettings still says `DefaultCompany`, so saves live in `AppData\LocalLow\DefaultCompany\WithTheDead`.
    `bundleVersion` is `0.1.0` while releases are stamped like `20261101.1637`. Changing the company name moves
    `persistentDataPath`.
  - **Do:** Set a real company name, with the same one-time copy the CLONE-Z → WithTheDead rename got (README, step 5).
    Have `Release.ps1` set `bundleVersion` from the stamp.
  - **Done when:** New saves go to the new folder, old ones are copied across, and the title screen shows the release
    version.

- [ ] **Crash and log reports from friends' builds** · P2 · M · QA
  - **Why:** When a friend's game breaks, its `Player.log` stays on their PC. The game watches its own log only in the
    smoke test.
  - **Do:** After a crash or an error-filled session, offer "send the log" on the next launch, uploading to the co-op
    server, which already runs a web site. Add the same as a button in the pause menu.
  - **Done when:** A friend's crash reaches you with its log, with their consent.

- [ ] **Lift the co-op server's limits** · P2 · M–L · Co-op
  - **Why:** From [Tools/Server/README.md](../Tools/Server/README.md), "Limits": one raid at a time; no game-server
    password (only the download page's login keeps strangers out); every release is a full 1.2 GB download with no
    patching; and everyone must be on the same release.
  - **Do:** Add a server password or an allow-list of runner ids. Patch through itch.io: butler uploads deltas, and
    `Release.ps1` already has `-Itch` and `-Butler` in the uncommitted changes. Then allow one raid per Zone at a time.
  - **Done when:** A stranger with the address can't join, and an update downloads only what changed.

- [ ] **The next steps for Java scripting** · P2 · M · Modding
  - **Why:** The API is ten events and five calls (`toast`, `subtitle`, `log`, `after` and `version`,
    [Game.java](../Java/withthedead.api/withthedead/api/Game.java)): scripts can watch but barely act. Mods dropped into
    `WithTheDead_Data/Java/scripts` run with the game's full trust, and `java.base` includes file and network access.
    Java 27's updates end in March 2027, when Java 28 ships ([Java/README.md](../Java/README.md)). A Linux server run
    outside the Docker image plays without scripts.
  - **Do:** Add read-only queries (the survivor's needs, position, inventory, the time, the Zone), a few safe actions
    (give an item, offer a quest, play a sound, place a map marker) and per-script saved data. State the trust model in
    the README before anyone shares mods. Put the Java 28 move (or the Java 25 LTS) in the calendar for March 2027.
  - **Done when:** A script can give a quest and remember something between sessions, and the README says what a mod
    can do.

- [ ] **Clean up the Night Shift queue and teach it the lessons** · P2 · S · Tooling
  - **Why:** [tasks.md](../Tools/NightShift/tasks.md) is 3.1 MB and 12,654 lines: 1,838 landed, 47 with nothing
    needed, 58 failed and 4 open. The failed and open ones are filed in this folder. Several failed because the work
    already existed (see [already done](#already-done)). The planner still writes "visual detail" tasks
    ([nightshift.py](../Tools/NightShift/nightshift.py#L1627)), and [LESSONS.md](../Tools/NightShift/memory/LESSONS.md)
    doesn't warn against `CreatePrimitive` children, which caused the floating geometry removed on 2026-11-02.
  - **Do:** While the night shift is stopped (the queue is shared under `.state/tasks.lock`), move landed and
    nothing-needed entries to an archive file, and mark the obsolete failed tasks `[~]`. Add a LESSONS.md bullet:
    detail goes into the owning RaidMesh or `HumanoidBodyBuilder` mesh, never `CreatePrimitive` children. Point the
    planner's visual-detail prompt at the same rule.
  - **Done when:** The queue holds only open work, and new visual tasks don't add child primitives.

### P3 — nice to have

- [ ] **Tidy the repository** · P3 · S · Repo
  - 30 local branches are already merged into `main` and can go: the 14 `map-revamp*` branches, the `feature/*` and
    `fix/*` branches, `coop-server-breach-windows`, `intro-town-revamp`, `nightshift/2026-10-19` and
    `reconcile/nightshift-2026-10-21`. Three are unmerged and need a look first:
    `fix/nightshift-2026-10-21-reconcile`, `nightshift/2026-10-18` and `smoke-check-a`. Also check the remote's leftover
    `origin/copilot/*` branches and two old stashes.
  - Template leftovers: [Assets/TutorialInfo](../Assets/TutorialInfo) (tracked), the empty `Assets/Scripts/Presentation`
    and `Assets/Scripts/Vehicles` folders, the Mobile quality level ([Rendering.md](Rendering.md)), and the scene still
    named `SampleScene` (README step 3 names it).
  - Root files: `Untitled.ipynb` is tracked (check whether AI Workbench needs it), and `MapVerify*.csproj` are stale and
    ignored. Keep the AI Workbench files (`apt.txt`, `requirements.txt`, `variables.env`, `preBuild.bash`,
    `postBuild.bash`, `onStart.bash`, `.project/`): the Night Shift needs them.
  - **Done when:** `git branch` shows live work only and the template leftovers are gone.

- [ ] **Fix the docs that disagree with the game** · P3 · S · Docs
  - The README's clone URL is `https://github.com/Henry-Cahill/CLONE-Z.git`, but the repository is now
    `Henry-Cahill/WithTheDead`.
  - The README says the game "is set in Knox County", but the map and the Kettle are in Bardstown, in Nelson County.
    Knox County is also Project Zomboid's setting, which is one more reason to change it.
  - [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md)'s statuses are stale; [Survival.md](Survival.md) has the corrected
    table.
  - GitHub Issues are turned off for the repository, so this folder is the tracker. Turning on Issues, or a Project
    board, would let friends file bugs.
  - **Done when:** The README, ITEMS_ENHANCEMENTS.md and MECHANICS_COVERAGE.md match the code.

## Already done

These looked missing, from the docs or the failed Night Shift tasks, but they exist. Don't plan them again:

- Can openers and tins ([PlayerInventory.cs](../Assets/Scripts/Player/PlayerInventory.cs#L279)), and campfires that
  burn fuel, warm and cook ([FieldSurvival.cs](../Assets/Scripts/Survival/FieldSurvival.cs#L367)).
- Weapon wear on every shot and swing ([PlayerCombat.cs](../Assets/Scripts/Player/PlayerCombat.cs#L499)), per-pellet
  shotguns ([L457](../Assets/Scripts/Player/PlayerCombat.cs#L457)), and two-handed weapons with their own swing.
- Thrown grenades and breach charges ([Camps.cs](../Assets/Scripts/World/Camps.cs#L632)). The solo smoke test already
  breaches a camp gate.
- Kicking in, bashing and forcing doors; breaking, boarding up and climbing through windows. See "Close out the
  doors-and-windows tasks" in [World.md](World.md).
- All five audio sliders apply ([SfxMixer.cs](../Assets/Scripts/Audio/SfxMixer.cs#L404),
  [NetSocial.cs](../Assets/Scripts/Net/NetSocial.cs#L587)).
- Ground items without a generated mesh fall back to their primitive model, so nothing draws invisible
  ([ItemMeshBaker.cs](../Assets/Scripts/World/ItemMeshBaker.cs#L63)).
- Raid characters keep their diseases in the profile ([Diseases.cs](../Assets/Scripts/Survival/Diseases.cs#L40)).
- Wipes that keep account knowledge ([ProfileStore.cs](../Assets/Scripts/Profile/ProfileStore.cs#L279)).
- Zone overlay steps log failures as errors ([ZoneWorld.cs](../Assets/Scripts/World/Zone/ZoneWorld.cs#L145)), so a smoke
  run catches them.
- `VehicleDriver` caches its `PlayerStats` ([Vehicles.cs](../Assets/Scripts/World/Vehicles.cs#L655)).

## How this list was made

- The project's own docs: [README.md](../README.md), [DESIGN_DECISIONS.md](../DESIGN_DECISIONS.md),
  [MECHANICS_COVERAGE.md](../MECHANICS_COVERAGE.md) and [ITEMS_ENHANCEMENTS.md](../ITEMS_ENHANCEMENTS.md).
- The Night Shift queue: all 58 failed and 4 open tasks were filed under a category or marked as safe to close.
- `Logs/Editor.log` from 2026-11-05: URP shadow-atlas warnings, NaN vertices, the snowfield workaround, the ring
  buffer, and the map and Zone summaries.
- `git status`, the branches, the packages and the project settings.
- A read-through of each category's code. Every bug and every high-priority claim was checked against the code before
  going in. About a third of the first-pass findings turned out to be already implemented; they're listed under
  [Already done](#already-done) instead.

## Keeping it current

- Tick an item with `- [x]` and add a short note: the commit, or what changed.
- Small, concrete items can go to the Night Shift queue as they are: one concrete change per task, naming the place
  ([tasks.md](../Tools/NightShift/tasks.md) header). Anything marked M or L, or touching the map passes, needs a person
  or a stronger model.
- Add new items to the right category, in the same shape: why, do, done when.
