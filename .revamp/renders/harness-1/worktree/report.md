# CLONE-Z map harness: Bardstown (build ky-local-f1)

**Exit 1**: 1 harness failure(s): see the stages below and unity.log. Full run, 0.1 s, started 2026-10-23 22:04:05.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | **no**:  |
| build time (cold: the first map built in the process) | 0 ms |
| the same square again, warm, pass by pass | 0 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 0.0, 0.0, facing 0° |
| exits |  |
| overhead renderers (hidden in the iso view) | 0 |
| `KentuckyMap.LastStats` |  |

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9); a 1.8 m capsule stands in for the survivor. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | **no** | 56 | InvalidOperationException: Cannot modify additionalLightsShadowResolutionTier outside of play mode.  @ at UnityEngine.Rendering.Universal.UniversalAdditionalLightData.set_additionalLightsShadowResolutionTier (System.Int32 value) [0x00019] in .\Library\PackageCache\com.unity.render-pipelines.universal@5... |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
