# CLONE-Z map harness: Frankfort (build ky-local-f2)

**Exit 0**: ok. Quick run, 8.7 s, started 2026-10-24 13:37:10.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Frankfort plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 5,009 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | -34.3, 21.2, facing 118° |
| exits | Snowfield (north) at 0.0, 197.0; KY 2261 east at 197.0, 12.6; KY 420 south (High Street) at -25.3, -197.0; KY 420 south (Ann Street) at -194.1, -197.0; KY 420 west (West Clinton Street) at -197.0, -46.1; KY 420 west (Mero Street) at -197.0, 116.0 |
| overhead renderers (hidden in the iso view) | 62 |
| `KentuckyMap.LastStats` | Kentucky map Frankfort (build ky-local-f2): 38 buildings, 27 beyond the edge, 131 street pieces, 277 containers, 312 lights, 688 trees, 6 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 38 with their middle inside the square (6 of them reach within 1 m of its edge or past it), 27 in the scenery ring; inside: house 20, commercial 15, church 2, civic 1 |
| map data: streets, lines, areas | 134 street pieces, 6 lines (rail 3, abandoned 2, river 1), 16 land-use areas (parking 11, pitch 2, grass 1, lot 1, park 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 10,182 | 4,874 | 4,874 | 4,566 | 2,787,473 | 1,713,747 | 127 | 11,776 | 325 (0) | 3,542 | 3,274 | 277 |
| RaidOverhead | 209 | 171 | 171 | 171 | 165,909 | 99,402 | 52 | 562 | 0 (0) | 0 | 171 | 0 |
| RaidScenery | 91 | 90 | 90 | 90 | 79,738 | 99,672 | 31 | 224 | 0 (0) | 0 | 57 | 0 |
| **all** | 10,482 | 5,135 | 5,135 | 4,827 | 3,033,120 | 1,912,821 | 161 | 12,562 | 325 (0) | 3,542 | 3,502 | 277 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 9,062 | 4,606 | 4,606 | 4,305 | 2,153,660 | 1,368,508 | 81 | 10,512 | 302 (0) | 2,022 | 3,114 | 211 |
| World/OutdoorProps | 114 | 114 | 114 | 114 | 238,188 | 151,586 | 16 | 430 | 0 (0) | 0 | 114 | 0 |
| World/OutdoorGround | 97 | 97 | 97 | 97 | 87,355 | 38,331 | 13 | 421 | 0 (0) | 0 | 0 | 0 |
| World/Building shells | 47 | 46 | 46 | 46 | 307,542 | 154,230 | 43 | 402 | 0 (0) | 0 | 46 | 0 |
| RaidOverhead/Building shells | 47 | 46 | 46 | 46 | 122,175 | 60,752 | 39 | 272 | 0 (0) | 0 | 46 | 0 |
| RaidOverhead/Roofs | 46 | 46 | 46 | 46 | 16,641 | 10,449 | 28 | 134 | 0 (0) | 0 | 46 | 0 |
| RaidScenery/SceneryGround | 31 | 31 | 31 | 31 | 12,248 | 4,816 | 10 | 127 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Interior KY# | 72 | 36 | 36 | 36 | 22,896 | 21,752 | 5 | 113 | 0 (0) | 0 | 36 | 0 |
| RaidScenery/SceneryProps | 37 | 37 | 37 | 37 | 26,420 | 19,340 | 21 | 75 | 0 (0) | 0 | 37 | 0 |
| RaidOverhead/Overhead | 26 | 26 | 26 | 26 | 699 | 233 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| RaidScenery/Wires | 20 | 20 | 20 | 20 | 2,112 | 3,660 | 1 | 20 | 0 (0) | 0 | 20 | 0 |
| RaidOverhead/Wires | 17 | 17 | 17 | 17 | 3,498 | 6,216 | 1 | 17 | 0 (0) | 0 | 17 | 0 |
| World/StreetLamp | 20 | 10 | 10 | 3 | 704 | 1,080 | 3 | 10 | 10 (0) | 10 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 38,954 | 71,854 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 4,827 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.579 | 0% | 1,674 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Mero Street, at the game's starting zoom | 0.101 | 0% | 212 |
| `iso_downtown_day.png` | Downtown Frankfort: the middle of the square, on Elk Alley | 0.403 | 0% | 138 |
| `iso_downtown_night.png` | Downtown Frankfort: the middle of the square, on Elk Alley | 0.073 | 0% | 72 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Mero Street (downtown), looking along the street | 0.408 | 0% | 88 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -34.3, 21.2 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -2.5, 1.4 | the place's own point in places.json (its OSM place node, which the square is built around), moved 2.9 m onto the nearest street |
| residential | -145.7, 50.8 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | -132.1, -81.4 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | 161.8, 154.2 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | 161.0, -161.8 | the biggest parking area inside the square |
| edge | 200.0, 13.2 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | 29.8, -258.3 | the point of a railway, river, canal or stream nearest the square's middle (none inside the square: this one is in the scenery ring) |
| landmark | -83.0, 110.4 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 73 |  |
| square: KentuckyMap.Build | yes | 5,012 |  |
| square: count | yes | 411 |  |
| square: pick spots | yes | 75 |  |
| square: pictures | yes | 2,957 |  |
| square: discard | yes | 114 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
