# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 21.3 s, started 2026-10-24 00:22:08.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 1,608 ms |
| the same square again, warm, pass by pass | 523 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 2.6, 14.9, facing -95° |
| exits | North 3rd Street (north) at 59.9, 197.0; East Stephen Foster Avenue (east) at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; West Stephen Foster Avenue (west) at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 92 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 104 buildings, 121 beyond the edge, 152 street pieces, 433 containers, 35 lights, 23 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 3,100 | 1,074 | 1,074 | 277 | 590,406 | 363,553 | 45 | 2,066 | 58 (23) | 2,959 | 906 | 433 |
| RaidOverhead | 267 | 266 | 266 | 266 | 117,230 | 76,832 | 23 | 721 | 0 (0) | 0 | 266 | 0 |
| RaidScenery | 102 | 101 | 101 | 101 | 53,001 | 35,099 | 20 | 403 | 0 (0) | 0 | 69 | 0 |
| **all** | 3,469 | 1,441 | 1,441 | 644 | 760,637 | 475,484 | 48 | 3,190 | 58 (23) | 2,959 | 1,241 | 433 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 63 | 12% | 1 | 63 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water and lots | 0 | 0% | 1 | 0 |
| waterways | 0 | 0% | 1 | 0 |
| streets | 26 | 5% | 1 | 26 |
| sidewalks | 8 | 2% | 1 | 8 |
| road markings | 0 | 0% | 1 | 0 |
| railways | 0 | 0% | 1 | 0 |
| buildings | 236 | 45% | 41 | 19 |
| roofs | 11 | 2% | 1 | 11 |
| roof snow and fence posts | 5 | 1% | 1 | 5 |
| utility lines | 9 | 2% | 1 | 9 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 18 | 3% | 1 | 18 |
| street lights | 19 | 4% | 1 | 19 |
| signs | 12 | 2% | 1 | 12 |
| street furniture | 17 | 3% | 1 | 17 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 16 | 3% | 2 | 16 |
| trees | 6 | 1% | 1 | 6 |
| snow drifts | 23 | 4% | 1 | 23 |
| points | 1 | 0% | 1 | 1 |
| (emit the overhead meshes) | 18 | 3% | 1 | 18 |
| (emit the outdoor meshes) | 32 | 6% | 1 | 32 |
| **total** | **523** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **59** | snow drifts 11 | 1 | 252 | 130 | 88,968 | 57,892 | 6 | 221 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **22** | buildings 8 | 1 | 373 | 102 | 45,819 | 25,766 | 4 | 322 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 138 + 8 | **110** | buildings 68 | 1 | 239 | 97 | 265,570 | 142,440 | 0 | 197 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 437 + 27 | **49** | buildings 33 | 1 | 180 | 97 | 86,998 | 53,549 | 0 | 170 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 15 + 7 | **9** | outdoor 2 | 0 | 56 | 39 | 19,789 | 16,211 | 0 | 41 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.8 | 1 | 1.8 |
| ground | 0.4 | 1 | 0.4 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.6 | 1 | 1.6 |
| sidewalks | 3.3 | 1 | 3.3 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 10.1 | 1 | 10.1 |
| roofs | 3.1 | 1 | 3.1 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility lines | 1.5 | 1 | 1.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 2.4 | 1 | 2.4 |
| street lights | 0.5 | 1 | 0.5 |
| signs | 5.9 | 1 | 5.9 |
| street furniture | 5.2 | 1 | 5.2 |
| landmarks | 0.5 | 1 | 0.5 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.1 | 2 | 1.1 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 10.7 | 1 | 10.7 |
| points | 0.1 | 1 | 0.1 |
| overhead | 2.3 | 1 | 2.3 |
| outdoor | 7.8 | 1 | 7.8 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.1 | 1 | 0.1 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.3 | 1 | 0.3 |
| sidewalks | 0.2 | 1 | 0.2 |
| road markings | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 14.0 | 2 | 7.7 |
| roofs | 0.5 | 1 | 0.5 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.9 | 1 | 0.9 |
| street furniture | 0.5 | 1 | 0.5 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.7 | 2 | 0.7 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 0.7 | 1 | 0.7 |
| points | 0.0 | 1 | 0.0 |
| overhead | 1.4 | 1 | 1.4 |
| outdoor | 2.5 | 1 | 2.5 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.5 | 1 | 0.5 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.2 | 1 | 1.2 |
| sidewalks | 9.5 | 1 | 9.5 |
| road markings | 0.3 | 1 | 0.3 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 67.8 | 1 | 67.8 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility lines | 1.1 | 1 | 1.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 9.5 | 2 | 7.8 |
| trees | 0.4 | 1 | 0.4 |
| snow drifts | 6.3 | 1 | 6.3 |
| points | 0.0 | 1 | 0.0 |
| overhead | 7.9 | 1 | 7.9 |
| outdoor | 3.7 | 1 | 3.7 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.9 | 1 | 1.9 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 32.5 | 1 | 32.5 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.1 | 1 | 0.1 |
| utility lines | 0.6 | 1 | 0.6 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.8 | 1 | 1.8 |
| street lights | 0.1 | 1 | 0.1 |
| signs | 0.7 | 1 | 0.7 |
| street furniture | 1.1 | 1 | 1.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.2 | 2 | 1.1 |
| trees | 1.2 | 1 | 1.2 |
| snow drifts | 2.3 | 1 | 2.3 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.8 | 1 | 0.8 |
| outdoor | 3.1 | 1 | 3.1 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.1 | 1 | 0.1 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.4 | 1 | 0.4 |
| sidewalks | 0.4 | 1 | 0.4 |
| road markings | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.7 | 1 | 0.7 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.4 | 1 | 1.4 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.3 | 2 | 1.1 |
| trees | 0.9 | 1 | 0.9 |
| snow drifts | 0.9 | 1 | 0.9 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 1.7 | 1 | 1.7 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 51 | 8 |
| 186_141 | yes | 17 | 2 |
| 185_141 | yes | 13 | 7 |
| 185_142 | yes | 19 | 2 |
| 183_148 | yes | 138 | 8 |
| 196_145 | yes | 437 | 27 |
| 210_134 | yes | 15 | 7 |
| 182_148 | yes | 125 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 894 ms (slowest 190 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **93** | buildings 69 | 1 | 239 | 97 | 265,570 | 142,440 | 0 | 197 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **142** | buildings 104 | 1 | 437 | 171 | 520,356 | 266,265 | 3 | 397 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 125 + 8 | **36** | roofs 11 | 1 | 205 | 77 | 78,561 | 51,287 | 1 | 161 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **148** | buildings 95 | 1 | 261 | 116 | 442,202 | 230,083 | 4 | 215 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **190** | roofs 113 | 1 | 185 | 95 | 544,874 | 285,807 | 1 | 147 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **118** | roofs 91 | 1 | 170 | 84 | 339,982 | 175,506 | 0 | 128 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **52** | roofs 25 | 1 | 191 | 79 | 178,559 | 98,859 | 0 | 162 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **18** | roofs 4 | 0 | 84 | 56 | 44,573 | 34,881 | 0 | 53 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **97** | roofs 76 | 1 | 198 | 80 | 167,450 | 95,984 | 1 | 154 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,972 | 855 | 855 | 438 | 2,582,127 | 1,381,112 | 50 | 1,913 | 10 (6) | 1,614 | 712 | 242 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (88,968 vertices, 57,892 triangles, 252 objects, 130 meshes; hash 43a582d362715098 vs 43a582d362715098).

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.615 | 0% | 5,639 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 135 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.593 | 0% | 3,219 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.123 | 0% | 3,952 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.572 | 0% | 58 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.109 | 0% | 67 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.590 | 0% | 64 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.080 | 0% | 62 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.578 | 0% | 63 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.102 | 0% | 73 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.600 | 0% | 58 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.082 | 0% | 95 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.284 | 0% | 54 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.052 | 1% | 59 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.553 | 0% | 65 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.090 | 0% | 49 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.603 | 0% | 49 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.082 | 0% | 69 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.483 | 0% | 57 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.186 | 33% | 53 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.487 | 0% | 60 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.151 | 35% | 83 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.595 | 0% | 336 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.558 | 0% | 65 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.078 | 0% | 59 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 2.6, 14.9 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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
| setup | yes | 78 |  |
| square: KentuckyMap.Build | yes | 1,610 |  |
| square: count | yes | 12 |  |
| square: pick spots | yes | 95 |  |
| square: pictures | yes | 15,932 |  |
| square: discard | yes | 20 |  |
| square: passes (warm rebuild) | yes | 539 |  |
| chunks: stress test | yes | 1,016 |  |
| chunks: Louisville 3x3 | yes | 1,878 |  |
| chunks: determinism | yes | 141 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
