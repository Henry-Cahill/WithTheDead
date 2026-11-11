# Rendering — to-do

[← All categories](README.md) · Snapshot of 2026-11-05 (`main` at 76e670c plus the uncommitted work in the tree). Line
links drift as code changes: if one lands a few lines off, search for the named symbol.

**Covers:** URP settings and lighting, shadows, line-of-sight shading and fog of war, the cutaway, weather visuals,
post-processing, item and ground-item rendering, the generated item meshes, and rendering performance.
**Code:** [Assets/Scripts/Rendering](../Assets/Scripts/Rendering), [Assets/Scripts/Camera](../Assets/Scripts/Camera),
[Assets/Settings](../Assets/Settings) (URP assets), [GroundItemRenderer.cs](../Assets/Scripts/World/GroundItemRenderer.cs),
[ItemMeshBaker.cs](../Assets/Scripts/World/ItemMeshBaker.cs), [GeneratedItemMeshes.cs](../Assets/Scripts/World/GeneratedItemMeshes.cs),
[MapCutaway.cs](../Assets/Scripts/World/Map/MapCutaway.cs), [Tools/AssetPipeline/mesh](../Tools/AssetPipeline/mesh/README.md).

## Where it stands

- URP 17.7 on the 6000.7 alpha, with two quality levels: **PC** (in use) and **Mobile**, a leftover from the template.
- Everything visible is built from code: the isometric cutaway (`IsoCutaway`, `MapCutaway`), line-of-sight darkness
  (`VisionController`, `IsoVisionShade`, `ActorFog`), snow and rain, the `PostFx` volume (brightness, blur, lens
  distortion, vignette), flashlight and night vision.
- Ground items are drawn instanced by `GroundItemRenderer`. Item looks come from `ItemMeshBaker`: a generated TRELLIS
  mesh if the item has one, otherwise its tinted primitive model, so nothing is ever invisible.
- 1,140 of the 3,681 catalogue items have generated meshes (3,320 of 11,043 versions), and 3,244 versions are
  committed (Git LFS, about 1.5 GiB). The Spark makes the rest around the clock, about 24 days more.
- Debug Mode (F8) names whatever is under the cursor, and F10 writes the floating-geometry report
  ([MECHANICS_COVERAGE.md](../MECHANICS_COVERAGE.md), "Debug Mode").

## P1 — next up

- [x] **Put the room lights' shadows on a budget** · P1 · M · Perf
  - **Done (2026-11-06):** The culprits were the Kettle's own lights (four pendants and the porch lantern: point lights
    at URP's default 1024 tier) beside the sight cone; Bardstown's fittings cast no shadows at all. Now one manager,
    [ShadowBudget.cs](../Assets/Scripts/Rendering/ShadowBudget.cs), hands out every punctual light's shadows (each
    carries a `ShadowedLight`): the survivor's sight cone and flashlight first, their atlas space held, then the lamps
    nearest the survivor (unseen ones count as twice as far; only in view) while every map fits the atlas at full size.
    Shaded ceiling fittings and the Kettle's pendants are downlights (150° spots, one map instead of six); bare bulbs
    stay point lights; the town's fittings ask for shadows again. The isometric view draws the sun's shadows in two
    cascades, the first ending 5 m past the survivor (no visible difference, 0.3–0.9 ms less GPU). **Shadows** in
    Settings: Low (2 lamps, hard), Medium (4), High (8, sharper, with a 4096 atlas on GPUs with 4 GB or more). The
    `nightwalk` smoke mode ([SmokeTest.NightWalk.cs](../Assets/Scripts/Dev/SmokeTest.NightWalk.cs)) checks it: before,
    3 squeeze messages (31 maps shrunk 4×); after, none. Median frame on an Arc A750 at 1080p: the bar 17.2 → 14.3 ms,
    the street 24.4 → 20.2 ms, by day 26.9 → 22.7 ms; the lit rooms, now shadowed, 19.9–25.8 → 20.4–27.0 ms.
  - **Why:** The editor log repeats URP's warning, for example "Reduced additional punctual light shadows resolution by
    4 to make 31 shadow maps fit in the 2048x2048 shadow atlas" (7 to 31 shadow maps, `Logs/Editor.log`). The PC asset
    turns on additional-light shadows with a 2048 atlas and 256/512/1024 tiers, 4 cascades over 50 m, and 4 lights per
    object ([PC_RPAsset.asset](../Assets/Settings/PC_RPAsset.asset#L46)). Every furnished interior adds lights, and a
    point light costs six shadow maps. Shadows go blurry exactly where the game is most atmospheric: dark rooms.
  - **Do:** Let only the few lights nearest the survivor and in line of sight cast shadows, and switch the rest to
    `LightShadows.None`, re-evaluating as the survivor moves (one manager, not per light). Prefer spot over point lights
    for ceiling fixtures. Try 2 cascades for the isometric view. Raise the atlas to 4096 only if the GPU budget
    allows. Tie it to a shadow-quality setting (see [UI.md](UI.md), display and graphics settings).
  - **Done when:** A night walk through Bardstown interiors logs no atlas-reduction warnings, with frame times
    profiled before and after.

- [ ] **Finish the item meshes and commit them** · P1 · L · Content
  - **Progress (2026-11-08):** The hand-built items were already exported (2026-10-26; `ItemModels.cs` hasn't changed
    since), and the Spark has all 94. The run goes around the clock: 3,320 of 11,043 versions made, about 24 days left.
    `mesh-redo` makes a version again from new seeds (a plain re-run repeats the same photo and mesh), and the 15 worst
    real versions are queued. The normal-bake check that failed 92 versions of thin things (grass, insects, sheaves) now
    measures how far the game mesh lies from the full mesh, and those are being made again. So is `gasmaskfilter`,
    whose model the Spark couldn't find (a case twin of `GasmaskFilter`). The Git LFS budget fits (the mesh README,
    "Committing the meshes"), and 3,244 versions are committed. Left: the run itself, a commit every so often, the 8
    versions given up on (out of memory, crashed or stuck; `--retry-failed`), and the 8 `Wound_*` versions the critic
    scored below 5.
  - **Why:** About 2,540 catalogue items still draw as primitive models next to generated ones, which reads as two art
    styles. The run also has 1,053 worn garments to do ([mesh README](../Tools/AssetPipeline/mesh/README.md), "What each
    version costs").
  - **Do:** Keep `mesh-start` running on the Spark. Use `mesh-status` and the critic scores to redo the worst versions
    (`mesh-redo`). Commit the meshes in batches, as the mesh README's "Committing the meshes" says.
  - **Done when:** `mesh-status` shows every item made or deliberately skipped, and the meshes are in Git LFS.

## P2 — soon

- [x] **Stop re-gathering every ground item every frame** · P2 · M · Perf
  - **Done (2026-11-07):** [GroundItemRenderer.cs](../Assets/Scripts/World/GroundItemRenderer.cs) keeps each chunk in
    range filed by item type (where each item lies, its resting facing, bob phase and look), filed again only when the
    chunk's new `GroundItemStore.Chunk.Version` changes; crossing into other chunks files just the ones arriving. Whole
    chunks and types are culled before anything in them is touched, and a type's looks and meshes are looked up only once
    one of its items may be in view (an item is taken to reach 2.5 m until then; every model and mesh in the game reaches
    1.9 m at most, the mesh pipeline refuses one that reaches further, and the smoke run checks them all). While the eye,
    the view, the chunks and the settings hold, as when standing still, a frame only moves what was in view: the spin and
    bob are unchanged, and only the turn and height of each matrix are written. The world bounds are
    made once a frame (the failed Night Shift task), and `ItemMeshBaker.Generation` tells the renderer when meshes are
    destroyed. The bakers' caches now start empty each play session: with domain reload off, the editor used to hand the
    second session the first one's destroyed meshes. The new `grounditems` smoke mode ([SmokeTest.GroundItems.cs](../Assets/Scripts/Dev/SmokeTest.GroundItems.cs))
    profiles it with 3,024 items in range. Standing still, the renderer's frame went from 1.23 to 0.34 ms (median; worst
    1.62 → 0.45) and its gather from 0.88 to 0.03 ms, with no chunk filed and no mesh looked up (before: 3,024 lookups and
    17 chunks walked a frame). What's left is the same 221 instanced draws as before. It checks that what's drawn matches
    the old gather exactly (items, meshes, looks, levels of detail and matrices), that a pile in range but out of view is
    never looked up, and that a drop or a pickup files just its own chunk.
  - **Why:** `GroundItemRenderer.LateUpdate` collects every chunk within 60 m and walks every item every frame, even
    when nothing moved ([GroundItemRenderer.cs](../Assets/Scripts/World/GroundItemRenderer.cs#L66)). It also looks up
    (and may load) the item's mesh with `ItemMeshBaker.Get` before the frustum test `InView`
    ([L118](../Assets/Scripts/World/GroundItemRenderer.cs#L118) vs [L137](../Assets/Scripts/World/GroundItemRenderer.cs#L137)),
    so items behind the camera still pull meshes into memory. A failed Night Shift task wanted to cache the per-frame
    bounds struct (tasks.md line 4599).
  - **Do:** Cache each chunk's instance matrices and rebuild them only when the chunk changes or the survivor crosses a
    cell. Test the bounds before the mesh lookup.
  - **Done when:** The profiler shows the renderer's CPU time flat while standing still, with no visible change.

- [x] **Bound the generated-mesh cache** · P2 · M · Tech debt
  - **Done (2026-11-07):** [GeneratedItemMeshes.cs](../Assets/Scripts/World/GeneratedItemMeshes.cs) counts what each
    item takes in video memory (its meshes' vertex and index buffers, its maps' mip chains) and keeps the total to a
    budget: a sixteenth of the graphics card's memory, 64 to 256 MB. Once over it, `GeneratedItemMeshes.Trim` lets go of
    the items drawn least recently until it's down to three quarters of the budget. Their meshes and materials are
    destroyed and their maps unloaded (`Clear` never unloaded the maps either; now it does), and `ItemMeshBaker.Generation`
    goes up. The ground-item renderer calls it at the start of each frame, before it looks anything up, and marks every
    model it draws (`Baked.MarkUsed`). Nothing drawn in the last 2 s or the frame before goes, and nothing held: a
    character's hand holds its item's model (`ItemMeshBaker.Hold`/`Release`, in `HumanoidRig`), since nothing else
    draws it. An item let go of is read from its files again when next wanted. The new `meshcache` smoke mode
    ([SmokeTest.MeshCache.cs](../Assets/Scripts/Dev/SmokeTest.MeshCache.cs),
    `Tools\Smoke\Smoke.ps1 -Mode meshcache -Zone bardstown`) checks it. It walks 8.25 km across the state in 33 stops,
    dropping 8 kinds nothing has read in yet at each one: 774 MB of meshes and maps read in against the 256 MB budget.
    The cache goes from 184–192 MB up to at most 252 MB and back, 183 items let go of, and the engine's own count of
    generated meshes, maps and materials levels off: at most 506 MB in the first half of the walk and 501 MB in the
    second, with texture memory at 174 to 213 MB. (The editor's figures for meshes and maps come to about twice the
    cache's own count.) The same walk with no budget (72 stops, 18 km) ends with 1,752 MB in the cache, 3,466 MB of
    generated meshes and maps, 1,036 MB of textures, and graphics-driver memory up from 668 to 2,359 MB. The run also
    checks that every item dropped is drawn with its near maps and material, the item in a hand stays, the first stop's
    items come back on the way back, and squeezing the budget to nothing keeps what's in view. With a 16 MB budget,
    smaller than a stop's own items, the cache holds just what's in use (24 to 46 MB) at every stop.
  - **Why:** Generated meshes, materials and textures stay cached for the whole session and are only released by
    `GeneratedItemMeshes.Clear` ([GeneratedItemMeshes.cs](../Assets/Scripts/World/GeneratedItemMeshes.cs#L73)). With
    up to three versions per item and 256 to 1024 px maps, a long walk across Kentucky keeps adding to video memory.
  - **Do:** Evict least-recently-drawn entries above a memory budget, or clear between raids. Bump
    `ItemMeshBaker.Generation` when evicting (as `Clear` does): the ground-item renderer holds on to the meshes it has
    looked up and would otherwise go on drawing nothing for those items.
  - **Done when:** Memory stays flat over a long streaming smoke run (`-clonez-smoke-zone bardstown`).

- [x] **Keep the dead's visibility up to date in big hordes** · P2 · M · Perf
  - **Done (2026-11-07):** [ZombieVisibility.cs](../Assets/Scripts/Rendering/ZombieVisibility.cs) gives every zombie the
    cheap part of the sight rule each frame (range, cone, arm's reach), so the ones out of view are settled without a ray.
    One in view is checked when it's due: at once when it comes into view, every 1/20 s within 6 m easing to every 1/8 s
    at 30 m, and sooner as it moves (half a metre walked counts as a whole interval). Up to 32 due checks run a frame, most
    overdue first; on top of those goes any zombie in view that would otherwise pass 0.2 s unchecked (allowing for the
    next frame running a quarter longer). A zombie that was seen is re-checked with one ray, to whichever of its chest and
    head was clear last time. The rays are cast against the new Sight Occluder layer (8) alone:
    [SightOccluders.cs](../Assets/Scripts/World/SightOccluders.cs) moves every solid collider off Default onto it
    (characters stay), claiming the scene once each mode has started, each streamed piece and interior before it wakes
    and each window as it's fitted, and sweeping round the survivor every second (or 8 m) for whatever is spawned later.
    The 18 rays and overlaps that tested Default alone (shots, sound occlusion, foot IK, build placement, door body
    checks, NPC sight, the cutaway's fallback and more) test `CloneZConfig.WorldMask`, Default and the new layer together,
    so they hit what they did before. The new `horde` smoke mode ([SmokeTest.Horde.cs](../Assets/Scripts/Dev/SmokeTest.Horde.cs)) keeps 200
    zombies round the survivor, most of them in front. In the procedural town and in Bardstown, with 75 to 140 in view a
    frame, the longest any went unchecked was 0.117–0.144 s at 60 fps, 0.167 s at 30 and 0.167–0.183 s at 20; the old
    round-robin would have left them 0.14–0.17, 0.28–0.33 and 0.42–0.50 s. The ticks cost 0.07–0.18 ms a frame (median).
    It also checks that the new layer blocks exactly the sight lines the whole world did (18,720 rays) and that, once
    still, every zombie is shown exactly when it can be seen.
  - **Why:** `ZombieVisibility` checks at most 24 zombies per frame
    ([ZombieVisibility.cs](../Assets/Scripts/Rendering/ZombieVisibility.cs#L54)) while aiming at 10 checks per zombie per
    second. Past roughly 140 zombies at 60 fps, each one is checked less often than intended, so the dead pop in late,
    which matters most in a horde.
  - **Do:** Check near and moving zombies first, and cheaply re-check ones that were already visible. Cast the rays
    against a dedicated occluder layer instead of the general world mask.
  - **Done when:** With 200 zombies, no zombie in view goes more than about 0.2 s without a check.

- [x] **Measure what the cutaway's hidden walls cost** · P2 · S · Perf
  - **Done (2026-11-08):** Measured with a new `cutaway` smoke mode
    ([SmokeTest.Cutaway.cs](../Assets/Scripts/Dev/SmokeTest.Cutaway.cs), `Tools\Smoke\Smoke.ps1 -Mode cutaway`, `-Zone intro`
    for the procedural town). In the densest block near the start it keeps the spot indoors and the spot outdoors where the
    cutaways hide the most. At each, by night and at noon, it renders the game's camera (1080p, Arc A750, Medium shadows)
    in rotation three ways: as the game has it, with nothing hidden casting shadows, and with only the survivor's own
    building's hidden parts casting them. A cut piece costs the shadow passes exactly what it cost uncut: `ShadowsOnly`
    only drops its main-pass draw. The ray cutaway's own hidden walls cost nothing measurable. In the procedural town they
    were 11–12 renderers of 4 pieces (about 3,000 triangles, up to 4% of the shadow draws), worth −0.29 to 0.08 ms GPU a
    render, inside the noise (±0.1–0.3 ms). In Bardstown the partitions and props it cuts cast no shadows at all. There
    the hidden casters are `MapCutaway`'s shells. Indoors in the densest block, 3 buildings were cut: 19 renderers, 31,400
    triangles, 5% of the shadow draws and 19% of their triangles. Dropping all their shadows saves 0.5 ms GPU a render at
    noon and 1.05 ms at night (mostly the lamps' maps), plus 0.4 ms CPU. Outdoors, with 1 building cut, it saves at most
    0.1 ms GPU and 0.2 ms CPU. Dropping only the buildings the survivor isn't in saves 0.16 ms GPU indoors and 0.1 ms
    outdoors at noon. It visibly relights those buildings' rooms as if their roofs were gone: 3.3% of the picture indoors
    and 4.9% outdoors changes by more than 24 levels. At night it saves 1.0 ms indoors and nothing shows, but that cost is
    the neighbours' shells in the survivor's lamps' maps, which they cost just the same uncut. So cut pieces keep their
    shadows. Keeping other buildings out of a lamp's shadow maps would be the way to that millisecond, cut or not. The
    collider caches now forget what a streamed piece, an interior or a floor's windows took with them a frame after it's
    let go of (`IsoCutaway.LetGo`, from `KentuckyStreamer`, `InteriorStreamer` and `WindowStreamer`), and the 4,096 clear
    stays as a backstop. On the mode's 1.3 km walk east, the cache never held more than 169 colliders. It dropped 229
    destroyed entries it would have kept, and the longest frame spent forgetting took 0.16 ms.
  - **Why:** Cut walls and roofs switch to `ShadowsOnly` so rooms stay lit correctly
    ([IsoCutaway.cs](../Assets/Scripts/Camera/IsoCutaway.cs#L349)). That's right, but they still cost shadow passes.
    The collider caches are only cleared past 4,096 entries ([L58](../Assets/Scripts/Camera/IsoCutaway.cs#L58),
    [L140](../Assets/Scripts/Camera/IsoCutaway.cs#L140)), so they hold streamed-out colliders until then.
  - **Do:** Profile a dense block. If it matters, turn shadows off on cut pieces of buildings the survivor isn't in, and
    drop cache entries when a streamed piece is let go.
  - **Done when:** The numbers are recorded and acted on.

- [x] **"Ran out of Graphics Ring Buffer space"** · P2 · S · Perf
  - **Done (2026-11-08):** It counts stalls, not uploads. With multithreaded rendering (the editor's), the main thread
    writes each frame's rendering commands into a 16 MB ring that the render thread reads. Whenever the ring is full the
    main thread waits (profiler marker `Gfx.WaitForGfxCommandWriteSpace`), and Unity logs the warning on every 301st
    wait. It counts across the editor session and never logs in a batch editor. A 128 MB mesh, buffer or texture in one
    frame caused no wait at all. Batch smoke runs couldn't show it: they render without a render thread and draw no
    camera. `Tools\Smoke\Smoke.ps1 -Threaded` now gives them one and draws the screen's cameras at 1920×1080 every
    frame, where the Game view does ([SmokeTest.Ring.cs](../Assets/Scripts/Dev/SmokeTest.Ring.cs)), and any wait fails
    the run. The streaming solo run in Bardstown then waited 2,005 times, all in the 4 s the ground drone was out. Its
    camera, a plain `Camera` with Unity's 1 km reach, looked along the street at the whole streamed town: about 600
    set-pass calls and 11 million triangles a frame, against the game camera's 150 and 0.9 million. The weather's linear
    fog ends by 78 m, so nearly all of that drew as fog. Shadows weren't it: without them the drone still drew 8.8
    million triangles and waited as often, and capping its reach at the game camera's 400 m only cut it to 1,635 waits.
    Now the drone's camera stops at the fog's end and clears to the fog's colour (`DronePilot.FrameCamera` in
    [Gadgets.cs](../Assets/Scripts/Gadgets/Gadgets.cs)). Past the fog that looks the same, except that its sky is fog
    too, and the run waits 0 times. The editor log's own warnings (one per play session on 11-08) came while the hub
    loaded, where no drone flies. No run here waited there, not even the lobby in a windowed editor at the same Game view
    size. In the editor and development builds, [CommandRingWatch](../Assets/Scripts/Dev/CommandRingWatch.cs) now logs
    any frame that waits as `[CloneZ] Render command ring full`, with what it drew, so the next one names itself.
  - **Why:** It appears in the editor log during play. It usually means large per-frame dynamic uploads: big instanced
    batches, rebuilt meshes, particles.
  - **Do:** Find the frame and the pass in the profiler and cut the upload. Use `-gfx-ring-buffer-size` only as a last
    resort.
  - **Done when:** A streaming smoke run doesn't log it.

- [x] **Fail the smoke test on new floating geometry** · P2 · M · Tooling
  - **Why:** Floating bits have shipped before: the Night Shift "visual detail" children on zombies and walls. The
    floating-geometry report ([FloatingGeometryReport.cs](../Assets/Scripts/Dev/FloatingGeometryReport.cs)) finds them,
    but only when someone presses F10.
  - **Do:** Run the report at the end of the lobby and wave smoke runs (both run in a batch editor) and fail on new
    findings, with a small allow-list for known false positives.
  - **Done when:** A deliberately floating test prop fails the smoke run.

## P3 — nice to have

- [ ] **Destroy `PostFx`'s volume profile** · P3 · S · Tech debt — `PostFx` creates a `VolumeProfile` at runtime
  ([Presentation.cs](../Assets/Scripts/Rendering/Presentation.cs#L54)) and has no `OnDestroy`. With domain and scene
  reload skipped on Play (EditorSettings), each Play leaves one behind in the editor. **Done when:** it's destroyed with
  its component.
- [ ] **Close the open SnowFX Night Shift task as "nothing needed"** · P3 · S · Triage — the task (tasks.md line 12648)
  says roofs are never cleared and snow doesn't refill after a calm. Snow refills at once on a view change and on
  stepping outdoors ([SnowFX.cs](../Assets/Scripts/Rendering/SnowFX.cs#L127)), and roofs are covered per emission
  ([L229](../Assets/Scripts/Rendering/SnowFX.cs#L229)). After a calm, flakes start from the top of the box, which reads
  naturally. **Done when:** the task is marked `[~]`, or a "pre-fill after a calm" is wanted and added.
- [ ] **Retire the Mobile quality level** · P3 · S · Tech debt — `ProjectSettings/QualitySettings.asset` still has
  **Mobile** (with [Mobile_RPAsset.asset](../Assets/Settings/Mobile_RPAsset.asset): HDR, render scale 0.8) next to
  **PC**. The game ships for Windows and a Linux server only. **Done when:** Mobile is removed, or turned into a "Low"
  PC preset on purpose.
- [ ] **Add `/// <summary>` docs to `Presentation.cs`** · P3 · S · Docs — a failed Night Shift task (tasks.md line 752).

## See also

- Display and graphics settings (resolution, vsync, quality, render scale): [UI.md](UI.md).
- The NaN vertices in a furniture mesh and the snowfield collider: [World.md](World.md).
