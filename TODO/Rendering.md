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
- 798 of the ~3,639 catalogue items have generated meshes (2,335 mesh files, 1.29 GB). 659 of those files are not
  committed yet.
- Debug Mode (F8) names whatever is under the cursor, and F10 writes the floating-geometry report
  ([MECHANICS_COVERAGE.md](../MECHANICS_COVERAGE.md), "Debug Mode").

## P1 — next up

- [ ] **Put the room lights' shadows on a budget** · P1 · M · Perf
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
  - **Why:** About 2,840 catalogue items still draw as primitive models next to generated ones, which reads as two art
    styles. The run also has 1,053 worn garments to do ([mesh README](../Tools/AssetPipeline/mesh/README.md), "What each
    version costs"). The 94 items with a hand-built model in `ItemModels.cs` are skipped until they're exported ("Hand-built
    items"), and 659 finished mesh files sit uncommitted in
    [GeneratedItemMeshes](../Assets/Resources/GeneratedItemMeshes).
  - **Do:** Export the hand-built items (**CloneZ → Asset…**, per the mesh README) and keep `mesh-start` running on the
    Spark. Use `mesh-status` and the critic scores to re-run the worst versions. Commit the meshes in batches, after
    checking the Git LFS budget (see the [index](README.md#cross-cutting)).
  - **Done when:** `mesh-status` shows every item made or deliberately skipped, and the meshes are in Git LFS.

## P2 — soon

- [ ] **Stop re-gathering every ground item every frame** · P2 · M · Perf
  - **Why:** `GroundItemRenderer.LateUpdate` collects every chunk within 60 m and walks every item every frame, even
    when nothing moved ([GroundItemRenderer.cs](../Assets/Scripts/World/GroundItemRenderer.cs#L66)). It also looks up
    (and may load) the item's mesh with `ItemMeshBaker.Get` before the frustum test `InView`
    ([L118](../Assets/Scripts/World/GroundItemRenderer.cs#L118) vs [L137](../Assets/Scripts/World/GroundItemRenderer.cs#L137)),
    so items behind the camera still pull meshes into memory. A failed Night Shift task wanted to cache the per-frame
    bounds struct (tasks.md line 4599).
  - **Do:** Cache each chunk's instance matrices and rebuild them only when the chunk changes or the survivor crosses a
    cell. Test the bounds before the mesh lookup.
  - **Done when:** The profiler shows the renderer's CPU time flat while standing still, with no visible change.

- [ ] **Bound the generated-mesh cache** · P2 · M · Tech debt
  - **Why:** Generated meshes, materials and textures stay cached for the whole session and are only released by
    `GeneratedItemMeshes.Clear` ([GeneratedItemMeshes.cs](../Assets/Scripts/World/GeneratedItemMeshes.cs#L73)). With
    up to three versions per item and 256 to 1024 px maps, a long walk across Kentucky keeps adding to video memory.
  - **Do:** Evict least-recently-drawn entries above a memory budget, or clear between raids.
  - **Done when:** Memory stays flat over a long streaming smoke run (`-clonez-smoke-zone bardstown`).

- [ ] **Keep the dead's visibility up to date in big hordes** · P2 · M · Perf
  - **Why:** `ZombieVisibility` checks at most 24 zombies per frame
    ([ZombieVisibility.cs](../Assets/Scripts/Rendering/ZombieVisibility.cs#L54)) while aiming at 10 checks per zombie per
    second. Past roughly 140 zombies at 60 fps, each one is checked less often than intended, so the dead pop in late,
    which matters most in a horde.
  - **Do:** Check near and moving zombies first, and cheaply re-check ones that were already visible. Cast the rays
    against a dedicated occluder layer instead of the general world mask.
  - **Done when:** With 200 zombies, no zombie in view goes more than about 0.2 s without a check.

- [ ] **Measure what the cutaway's hidden walls cost** · P2 · S · Perf
  - **Why:** Cut walls and roofs switch to `ShadowsOnly` so rooms stay lit correctly
    ([IsoCutaway.cs](../Assets/Scripts/Camera/IsoCutaway.cs#L349)). That's right, but they still cost shadow passes.
    The collider caches are only cleared past 4,096 entries ([L58](../Assets/Scripts/Camera/IsoCutaway.cs#L58),
    [L140](../Assets/Scripts/Camera/IsoCutaway.cs#L140)), so they hold streamed-out colliders until then.
  - **Do:** Profile a dense block. If it matters, turn shadows off on cut pieces of buildings the survivor isn't in, and
    drop cache entries when a streamed piece is let go.
  - **Done when:** The numbers are recorded and acted on.

- [ ] **"Ran out of Graphics Ring Buffer space"** · P2 · S · Perf
  - **Why:** It appears in the editor log during play. It usually means large per-frame dynamic uploads: big instanced
    batches, rebuilt meshes, particles.
  - **Do:** Find the frame and the pass in the profiler and cut the upload. Use `-gfx-ring-buffer-size` only as a last
    resort.
  - **Done when:** A streaming smoke run doesn't log it.

- [ ] **Fail the smoke test on new floating geometry** · P2 · M · Tooling
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
