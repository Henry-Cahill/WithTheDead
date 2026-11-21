# CLONE-Z map harness: Maysville (build ky-local-f2)

**Exit 2**: 1 error(s) logged or thrown by the map code while building (see the logs below). Quick run, 16.9 s, started 2026-10-24 09:51:44.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Maysville plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 10,795 ms |
| logged while building | 1 errors, 86 warnings, 176 edit-mode artefacts |
| spawn | -22.8, -29.1, facing 29° |
| exits | Sutton Street (north) at -122.1, 197.0; US 62 east at 197.0, -162.8; US 62 south at 180.5, -197.0; KY 8 west at -197.0, -1.0 |
| overhead renderers (hidden in the iso view) | 52 |
| `KentuckyMap.LastStats` | Kentucky map Maysville (build ky-local-f2): 87 buildings, 29 beyond the edge, 75 street pieces, 424 containers, 34 lights, 250 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 87 with their middle inside the square (7 of them reach within 1 m of its edge or past it), 29 in the scenery ring; inside: commercial 48, house 34, church 3, canopy 1, warehouse 1 |
| map data: streets, lines, areas | 76 street pieces, 3 lines (rail 2, river 1), 6 land-use areas (park 3, cemetery 1, school 1, water 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 11,393 | 4,124 | 4,124 | 3,428 | 2,541,833 | 1,493,169 | 118 | 8,999 | 198 (0) | 3,813 | 3,049 | 1,514 |
| RaidOverhead | 315 | 227 | 227 | 227 | 312,461 | 164,802 | 105 | 870 | 0 (0) | 0 | 227 | 0 |
| RaidScenery | 82 | 81 | 81 | 81 | 67,427 | 90,797 | 30 | 186 | 0 (0) | 0 | 47 | 0 |
| **all** | 11,790 | 4,432 | 4,432 | 3,736 | 2,921,721 | 1,748,768 | 197 | 10,055 | 198 (0) | 3,813 | 3,323 | 1,514 |

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.535 | 0% | 2,572 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Market Street, at the game's starting zoom | 0.109 | 0% | 531 |
| `iso_downtown_day.png` | Downtown Maysville: the middle of the square, on Market Street | 0.462 | 0% | 129 |
| `iso_downtown_night.png` | Downtown Maysville: the middle of the square, on Market Street | 0.080 | 0% | 112 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Market Street (downtown), looking along the street | 0.379 | 0% | 286 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -22.8, -29.1 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -11.3, 6.4 | the place's own point in places.json (its OSM place node, which the square is built around), moved 13.0 m onto the nearest street |
| residential | -34.2, -39.3 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | 0.1, 31.8 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | 130.4, 111.3 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | -26.4, 45.3 | no parking area is mapped in the square: the point along a main street with the most shops and offices within 35 m |
| edge | 200.0, -157.6 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | 68.1, 127.2 | the point of a railway, river, canal or stream nearest the square's middle |
| landmark | 74.3, -66.7 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 1 errors, 86 warnings, 176 edit-mode artefacts

- `Warning: [CloneZ] Interiors: furnishing building 1088744818 (Bank) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088744818: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088744818: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1088746268 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088746268: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088746268: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1088839124 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088839124: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088839124: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1088871152 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088871152: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088871152: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1088871154 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088871154: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088871154: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.  @ UnityEngine.Renderer:get_material ()`
- `(edit mode) Error: Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.  @ UnityEngine.Renderer:get_material ()`
- `(edit mode) Error: Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.  @ UnityEngine.Renderer:get_material ()`
- `(edit mode) Error: Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.  @ UnityEngine.Renderer:get_material ()`
- `Warning: [CloneZ] Interiors: furnishing building 1088871156 (Restrooms) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`

### Everything logged during the run: 1 errors, 86 warnings, 176 edit-mode artefacts

- `Warning: [CloneZ] Interiors: furnishing building 1088744818 (Bank) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088744818: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088744818: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1088746268 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088746268: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088746268: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1088839124 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088839124: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088839124: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1088871152 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088871152: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088871152: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1088871154 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1088871154: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1088871154: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.  @ UnityEngine.Renderer:get_material ()`
- `(edit mode) Error: Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.  @ UnityEngine.Renderer:get_material ()`
- `(edit mode) Error: Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.  @ UnityEngine.Renderer:get_material ()`
- `(edit mode) Error: Instantiating material due to calling renderer.material during edit mode. This will leak materials into the scene. You most likely want to use renderer.sharedMaterial instead.  @ UnityEngine.Renderer:get_material ()`
- `Warning: [CloneZ] Interiors: furnishing building 1088871156 (Restrooms) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 544 |  |
| square: KentuckyMap.Build | yes | 10,798 |  |
| square: count | yes | 126 |  |
| square: pick spots | yes | 91 |  |
| square: pictures | yes | 5,075 |  |
| square: discard | yes | 168 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
