# CLONE-Z map harness: Louisville (build ky-local-f2)

**Exit 2**: 1 error(s) logged or thrown by the map code while building (see the logs below). Quick run, 26.6 s, started 2026-10-24 08:38:33.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Louisville plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 4,238 ms |
| logged while building | 1 errors, 48 warnings, 96 edit-mode artefacts |
| spawn | 76.4, 19.8, facing -172° |
| exits | South 6th Street (north) at -52.3, 197.0; US 31W east at 197.0, 114.4; West Jefferson Street (east) at 197.0, -42.9; South 5th Street (south) at 40.9, -197.0; West Jefferson Street (west) at -197.0, 11.9; US 60 west at -197.0, 169.0 |
| overhead renderers (hidden in the iso view) | 32 |
| `KentuckyMap.LastStats` | Kentucky map Louisville (build ky-local-f2): 51 buildings, 56 beyond the edge, 495 street pieces, 352 containers, 5 lights, 321 trees, 6 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 51 with their middle inside the square (7 of them reach within 1 m of its edge or past it), 56 in the scenery ring; inside: commercial 37, house 8, canopy 2, office 2, warehouse 2 |
| map data: streets, lines, areas | 521 street pieces, 0 lines (), 98 land-use areas (park 38, parking 23, grass 22, water 7, plaza 6, commercial 2) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 8,594 | 3,148 | 3,148 | 2,689 | 2,096,671 | 1,244,816 | 104 | 6,562 | 79 (0) | 3,088 | 2,305 | 859 |
| RaidOverhead | 208 | 158 | 158 | 158 | 844,556 | 422,806 | 80 | 607 | 0 (0) | 0 | 158 | 0 |
| RaidScenery | 100 | 99 | 99 | 99 | 75,673 | 78,014 | 29 | 254 | 0 (0) | 0 | 56 | 0 |
| **all** | 8,902 | 3,405 | 3,405 | 2,946 | 3,016,900 | 1,745,636 | 168 | 7,423 | 79 (0) | 3,088 | 2,519 | 859 |

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.557 | 0% | 9,744 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on South 5th Street, at the game's starting zoom | 0.104 | 0% | 4,573 |
| `iso_downtown_day.png` | Downtown Louisville: the middle of the square, on West Jefferson Street | 0.265 | 0% | 3,518 |
| `iso_downtown_night.png` | Downtown Louisville: the middle of the square, on West Jefferson Street | 0.070 | 0% | 69 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on South 5th Street (downtown), looking along the street | 0.249 | 0% | 3,214 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 76.4, 19.8 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -2.2, -15.2 | the place's own point in places.json (its OSM place node, which the square is built around), moved 15.4 m onto the nearest street |
| residential | -58.7, 151.4 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | 84.3, 130.1 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | -60.9, -53.4 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | 90.4, -156.8 | the biggest parking area inside the square |
| edge | 200.0, 113.9 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | -59.0, 45.1 | no railway or river in reach: the biggest water area inside the square |
| landmark | -157.9, -36.5 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 1 errors, 48 warnings, 96 edit-mode artefacts

- `Warning: [CloneZ] Interiors: furnishing building 598083532 (Bank) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY598083532: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY598083532: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565246 (Police) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565246: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565246: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565414 (Workshop) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565414: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565414: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565450 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565450: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565450: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565466 (Workshop) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565466: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565466: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565482 (Civic) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565482: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565482: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565486 (Civic) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565486: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

### Everything logged during the run: 1 errors, 48 warnings, 96 edit-mode artefacts

- `Warning: [CloneZ] Interiors: furnishing building 598083532 (Bank) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY598083532: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY598083532: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565246 (Police) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565246: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565246: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565414 (Workshop) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565414: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565414: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565450 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565450: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565450: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565466 (Workshop) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565466: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565466: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565482 (Civic) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565482: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY662565482: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 662565486 (Civic) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY662565486: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 73 |  |
| square: KentuckyMap.Build | yes | 4,241 |  |
| square: count | yes | 48 |  |
| square: pick spots | yes | 324 |  |
| square: pictures | yes | 21,813 |  |
| square: discard | yes | 77 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
