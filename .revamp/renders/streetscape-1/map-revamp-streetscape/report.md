# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Quick run, 3.0 s, started 2026-10-24 00:01:34.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 1,154 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | -13.4, 6.1, facing -135° |
| exits | North 3rd Street (north) at 59.9, 197.0; East Stephen Foster Avenue (east) at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; West Stephen Foster Avenue (west) at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 92 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 104 buildings, 121 beyond the edge, 152 street pieces, 433 containers, 9 lights, 23 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 3,051 | 1,074 | 1,074 | 277 | 566,818 | 347,831 | 43 | 1,939 | 32 (23) | 2,812 | 906 | 433 |
| RaidOverhead | 267 | 266 | 266 | 266 | 114,636 | 75,008 | 20 | 714 | 0 (0) | 0 | 266 | 0 |
| RaidScenery | 102 | 101 | 101 | 101 | 53,001 | 35,099 | 20 | 403 | 0 (0) | 0 | 69 | 0 |
| **all** | 3,420 | 1,441 | 1,441 | 644 | 734,455 | 457,938 | 44 | 3,056 | 32 (23) | 2,812 | 1,241 | 433 |

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.615 | 0% | 856 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.122 | 0% | 128 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.569 | 0% | 61 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.090 | 0% | 52 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.482 | 0% | 53 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -13.4, 6.1 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -7.7, 15.0 | the place's own point in places.json (its OSM place node, which the square is built around), moved 16.8 m onto the nearest street |
| residential | -131.8, 183.1 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | 2.3, 18.2 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | -135.9, -100.2 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | -64.4, 125.6 | the biggest parking area inside the square |
| edge | -200.0, 37.4 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | none | nothing of the kind in the square |
| landmark | 180.9, 150.6 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 80 |  |
| square: KentuckyMap.Build | yes | 1,156 |  |
| square: count | yes | 9 |  |
| square: pick spots | yes | 80 |  |
| square: pictures | yes | 1,645 |  |
| square: discard | yes | 13 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
