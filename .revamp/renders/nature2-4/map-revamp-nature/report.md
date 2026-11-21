# CLONE-Z map harness: Hazard (build ky-local-f2)

**Exit 0**: ok. Quick run, 7.8 s, started 2026-10-24 11:16:12.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Hazard plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 3,985 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 40.5, -0.1, facing -112° |
| exits | KY 15 Business north at -29.4, 197.0; KY 1762 east at 197.0, 54.5; KY 15 Business south at -26.6, -197.0; Main Street (west) at -197.0, -40.2 |
| overhead renderers (hidden in the iso view) | 61 |
| `KentuckyMap.LastStats` | Kentucky map Hazard (build ky-local-f2): 37 buildings, 40 beyond the edge, 62 street pieces, 299 containers, 277 lights, 2666 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 37 with their middle inside the square (5 of them reach within 1 m of its edge or past it), 40 in the scenery ring; inside: commercial 23, house 6, church 3, apartments 2, retail 2, school 1 |
| map data: streets, lines, areas | 63 street pieces, 3 lines (rail 2, river 1), 12 land-use areas (parking 8, park 3, lot 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 10,150 | 5,413 | 5,413 | 5,139 | 2,900,780 | 1,796,056 | 131 | 12,340 | 280 (0) | 4,092 | 3,916 | 299 |
| RaidOverhead | 229 | 190 | 190 | 190 | 150,655 | 91,960 | 77 | 709 | 0 (0) | 0 | 190 | 0 |
| RaidScenery | 124 | 123 | 123 | 123 | 133,980 | 148,096 | 35 | 240 | 0 (0) | 0 | 79 | 0 |
| **all** | 10,503 | 5,726 | 5,726 | 5,452 | 3,185,415 | 2,036,112 | 190 | 13,289 | 280 (0) | 4,092 | 4,185 | 299 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 9,200 | 5,085 | 5,085 | 4,816 | 2,090,001 | 1,299,584 | 81 | 10,972 | 270 (0) | 2,040 | 3,726 | 241 |
| World/OutdoorProps | 142 | 142 | 142 | 142 | 450,931 | 324,001 | 17 | 488 | 0 (0) | 0 | 142 | 0 |
| World/OutdoorGround | 130 | 130 | 130 | 130 | 99,440 | 40,547 | 15 | 478 | 0 (0) | 0 | 0 | 0 |
| World/Building shells | 49 | 48 | 48 | 48 | 259,878 | 131,132 | 44 | 394 | 0 (0) | 0 | 48 | 0 |
| RaidOverhead/Building shells | 49 | 48 | 48 | 48 | 100,838 | 51,094 | 66 | 360 | 0 (0) | 0 | 48 | 0 |
| RaidOverhead/Roofs | 57 | 57 | 57 | 57 | 15,215 | 9,660 | 28 | 159 | 0 (0) | 0 | 57 | 0 |
| RaidOverhead/Interior KY# | 74 | 37 | 37 | 37 | 30,711 | 25,052 | 5 | 142 | 0 (0) | 0 | 37 | 0 |
| RaidScenery/SceneryProps | 44 | 44 | 44 | 44 | 82,952 | 62,944 | 25 | 102 | 0 (0) | 0 | 44 | 0 |
| RaidScenery/SceneryGround | 42 | 42 | 42 | 42 | 7,394 | 2,474 | 9 | 101 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/Wires | 35 | 35 | 35 | 35 | 4,269 | 7,110 | 1 | 35 | 0 (0) | 0 | 35 | 0 |
| RaidOverhead/Wires | 24 | 24 | 24 | 24 | 3,501 | 6,024 | 1 | 24 | 0 (0) | 0 | 24 | 0 |
| RaidOverhead/Overhead | 24 | 24 | 24 | 24 | 390 | 130 | 1 | 24 | 0 (0) | 0 | 24 | 0 |
| World/StreetLamp | 14 | 7 | 7 | 2 | 506 | 780 | 3 | 7 | 7 (0) | 7 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 39,361 | 75,566 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 5,452 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.565 | 0% | 2,223 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on L O Davis Drive, at the game's starting zoom | 0.079 | 0% | 148 |
| `iso_downtown_day.png` | Downtown Hazard: the middle of the square, on L O Davis Drive | 0.396 | 0% | 58 |
| `iso_downtown_night.png` | Downtown Hazard: the middle of the square, on L O Davis Drive | 0.075 | 0% | 104 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on L O Davis Drive (downtown), looking along the street | 0.425 | 0% | 112 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 40.5, -0.1 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | 1.0, -6.1 | the place's own point in places.json (its OSM place node, which the square is built around), moved 6.2 m onto the nearest street |
| residential | 115.3, -101.0 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | -50.0, -167.3 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | 91.9, -13.5 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | -43.7, 36.7 | the biggest parking area inside the square |
| edge | -24.1, -200.0 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | -143.1, -253.4 | the point of a railway, river, canal or stream nearest the square's middle (none inside the square: this one is in the scenery ring) |
| landmark | -76.0, -68.9 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 56 |  |
| square: KentuckyMap.Build | yes | 3,987 |  |
| square: count | yes | 348 |  |
| square: pick spots | yes | 26 |  |
| square: pictures | yes | 3,301 |  |
| square: discard | yes | 84 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
