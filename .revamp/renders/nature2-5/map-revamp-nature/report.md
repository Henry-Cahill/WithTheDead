# CLONE-Z map harness: Louisville (build ky-local-f2)

**Exit 0**: ok. Quick run, 9.3 s, started 2026-10-24 11:42:34.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Louisville plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 5,810 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 23.4, -25.0, facing -82° |
| exits | South 6th Street (north) at -52.3, 197.0; US 31W east at 197.0, 114.4; West Jefferson Street (east) at 197.0, -42.9; South 5th Street at 40.9, -197.0; West Jefferson Street at -197.0, 11.9; US 60 west at -197.0, 169.0 |
| overhead renderers (hidden in the iso view) | 80 |
| `KentuckyMap.LastStats` | Kentucky map Louisville (build ky-local-f2): 51 buildings, 56 beyond the edge, 495 street pieces, 625 containers, 405 lights, 328 trees, 6 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 51 with their middle inside the square (7 of them reach within 1 m of its edge or past it), 56 in the scenery ring; inside: commercial 37, house 8, canopy 2, office 2, warehouse 2 |
| map data: streets, lines, areas | 521 street pieces, 0 lines (), 98 land-use areas (park 38, parking 23, grass 22, water 7, plaza 6, commercial 2) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 22,127 | 9,629 | 9,629 | 9,219 | 6,141,323 | 3,600,976 | 135 | 23,712 | 406 (0) | 6,186 | 7,282 | 625 |
| RaidOverhead | 309 | 259 | 259 | 259 | 340,583 | 204,231 | 91 | 970 | 0 (0) | 0 | 259 | 0 |
| RaidScenery | 107 | 106 | 106 | 106 | 79,932 | 100,726 | 41 | 284 | 0 (0) | 0 | 63 | 0 |
| **all** | 22,543 | 9,994 | 9,994 | 9,584 | 6,561,838 | 3,905,933 | 210 | 24,966 | 406 (0) | 6,186 | 7,604 | 625 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 20,348 | 9,262 | 9,262 | 8,882 | 5,133,452 | 3,068,175 | 81 | 21,982 | 381 (0) | 4,494 | 7,089 | 501 |
| World/OutdoorGround | 140 | 140 | 140 | 140 | 150,711 | 64,298 | 16 | 626 | 0 (0) | 0 | 0 | 0 |
| World/OutdoorProps | 134 | 134 | 134 | 134 | 276,937 | 167,283 | 28 | 592 | 0 (0) | 0 | 134 | 0 |
| World/Building shells | 60 | 59 | 59 | 59 | 577,790 | 297,488 | 37 | 478 | 0 (0) | 0 | 59 | 0 |
| RaidOverhead/Building shells | 60 | 59 | 59 | 59 | 225,011 | 115,848 | 78 | 425 | 0 (0) | 0 | 59 | 0 |
| RaidOverhead/Roofs | 89 | 89 | 89 | 89 | 44,117 | 27,108 | 35 | 306 | 0 (0) | 0 | 89 | 0 |
| RaidOverhead/Interior KY# | 96 | 48 | 48 | 48 | 62,701 | 47,492 | 5 | 176 | 0 (0) | 0 | 48 | 0 |
| RaidScenery/SceneryGround | 41 | 41 | 41 | 41 | 9,024 | 3,028 | 10 | 130 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SceneryProps | 35 | 35 | 35 | 35 | 28,230 | 17,802 | 30 | 124 | 0 (0) | 0 | 35 | 0 |
| World/StreetLamp | 66 | 33 | 33 | 3 | 2,409 | 3,720 | 3 | 33 | 24 (0) | 33 | 0 | 0 |
| RaidOverhead/Overhead | 32 | 32 | 32 | 32 | 1,263 | 421 | 1 | 32 | 0 (0) | 0 | 32 | 0 |
| RaidOverhead/Wires | 31 | 31 | 31 | 31 | 7,491 | 13,362 | 1 | 31 | 0 (0) | 0 | 31 | 0 |
| RaidScenery/Wires | 28 | 28 | 28 | 28 | 4,002 | 7,236 | 1 | 28 | 0 (0) | 0 | 28 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 38,672 | 72,658 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 9,584 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.562 | 0% | 1,333 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Jefferson Street, at the game's starting zoom | 0.066 | 0% | 138 |
| `iso_downtown_day.png` | Downtown Louisville: the middle of the square, on West Jefferson Street | 0.288 | 0% | 64 |
| `iso_downtown_night.png` | Downtown Louisville: the middle of the square, on West Jefferson Street | 0.073 | 0% | 55 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Jefferson Street (downtown), looking along the street | 0.314 | 0% | 86 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 23.4, -25.0 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -2.2, -15.2 | the place's own point in places.json (its OSM place node, which the square is built around), moved 15.4 m onto the nearest street |
| residential | -58.7, 151.4 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | 84.3, 130.1 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | -60.9, -53.4 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | 90.4, -156.8 | the biggest parking area inside the square |
| edge | 200.0, 113.9 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | -59.0, 45.1 | no railway or river in reach: the biggest water area inside the square |
| landmark | -157.9, -36.5 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 56 |  |
| square: KentuckyMap.Build | yes | 5,812 |  |
| square: count | yes | 650 |  |
| square: pick spots | yes | 309 |  |
| square: pictures | yes | 2,289 |  |
| square: discard | yes | 188 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
