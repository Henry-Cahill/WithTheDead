# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 23.4 s, started 2026-10-24 00:25:35.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 2,157 ms |
| the same square again, warm, pass by pass | 833 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 2.6, 14.9, facing -95° |
| exits | North 3rd Street (north) at 59.9, 197.0; East Stephen Foster Avenue (east) at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; West Stephen Foster Avenue (west) at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 92 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 104 buildings, 121 beyond the edge, 152 street pieces, 433 containers, 10 lights, 0 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 3,160 | 1,051 | 1,051 | 277 | 646,959 | 395,599 | 42 | 2,102 | 33 (23) | 3,016 | 908 | 433 |
| RaidOverhead | 221 | 220 | 220 | 220 | 76,428 | 40,404 | 18 | 582 | 0 (0) | 0 | 220 | 0 |
| RaidScenery | 73 | 72 | 72 | 72 | 45,546 | 40,807 | 21 | 344 | 0 (0) | 0 | 39 | 0 |
| **all** | 3,454 | 1,343 | 1,343 | 569 | 768,933 | 476,810 | 44 | 3,028 | 33 (23) | 3,016 | 1,167 | 433 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 88 | 11% | 1 | 88 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 1 | 0% | 1 | 1 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| parking | 57 | 7% | 1 | 57 |
| cemeteries | 31 | 4% | 2 | 30 |
| grounds | 0 | 0% | 1 | 0 |
| yards | 0 | 0% | 1 | 0 |
| snow tone | 9 | 1% | 1 | 9 |
| streets | 14 | 2% | 1 | 14 |
| sidewalks | 21 | 3% | 1 | 21 |
| road markings | 1 | 0% | 1 | 1 |
| railways | 0 | 0% | 1 | 0 |
| buildings | 372 | 45% | 41 | 49 |
| roofs | 21 | 3% | 1 | 21 |
| roof snow and fence posts | 3 | 0% | 1 | 3 |
| utility poles | 22 | 3% | 1 | 22 |
| street lights | 8 | 1% | 1 | 8 |
| signposts | 5 | 1% | 1 | 5 |
| hydrants | 9 | 1% | 1 | 9 |
| lot fences | 2 | 0% | 1 | 2 |
| snow piles | 5 | 1% | 1 | 5 |
| parked cars | 19 | 2% | 1 | 19 |
| trees | 0 | 0% | 1 | 0 |
| snow drifts | 30 | 4% | 1 | 30 |
| points | 1 | 0% | 1 | 1 |
| park litter | 0 | 0% | 1 | 0 |
| (emit the overhead meshes) | 21 | 2% | 1 | 21 |
| (emit the outdoor meshes) | 95 | 11% | 1 | 95 |
| **total** | **833** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **60** | buildings 16 | 1 | 261 | 118 | 82,104 | 53,935 | 2 | 231 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **75** | buildings 34 | 2 | 388 | 104 | 49,179 | 29,423 | 5 | 335 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 193 + 13 | **276** | buildings 106 | 1 | 237 | 95 | 274,218 | 148,173 | 1 | 202 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 614 + 32 | **246** | ground 172 | 1 | 185 | 93 | 86,247 | 54,329 | 1 | 172 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **12** | parking 3 | 0 | 62 | 30 | 34,783 | 24,375 | 1 | 54 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.9 | 1 | 2.9 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 1 | 0.0 |
| snow tone | 0.7 | 1 | 0.7 |
| streets | 10.8 | 1 | 10.8 |
| sidewalks | 5.9 | 1 | 5.9 |
| road markings | 0.2 | 1 | 0.2 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 15.6 | 1 | 15.6 |
| roofs | 4.9 | 1 | 4.9 |
| roof snow and fence posts | 0.3 | 1 | 0.3 |
| utility poles | 0.8 | 1 | 0.8 |
| street lights | 1.2 | 1 | 1.2 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.2 | 1 | 0.2 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 1.2 | 1 | 1.2 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 4.6 | 1 | 4.6 |
| points | 0.1 | 1 | 0.1 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 1.7 | 1 | 1.7 |
| outdoor | 8.1 | 1 | 8.1 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 1 | 0.0 |
| snow tone | 0.7 | 1 | 0.7 |
| streets | 0.3 | 1 | 0.3 |
| sidewalks | 0.3 | 1 | 0.3 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 44.1 | 2 | 34.4 |
| roofs | 3.4 | 1 | 3.4 |
| roof snow and fence posts | 0.1 | 1 | 0.1 |
| utility poles | 0.7 | 1 | 0.7 |
| street lights | 1.8 | 1 | 1.8 |
| signposts | 0.2 | 1 | 0.2 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.5 | 1 | 0.5 |
| parked cars | 2.8 | 1 | 2.8 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 2.6 | 1 | 2.6 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 6.3 | 1 | 6.3 |
| outdoor | 10.7 | 1 | 10.7 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.9 | 1 | 0.9 |
| ground | 3.0 | 1 | 3.0 |
| water | 91.5 | 1 | 91.5 |
| fields | 1.0 | 1 | 1.0 |
| parking | 0.8 | 1 | 0.8 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 1.7 | 1 | 1.7 |
| snow tone | 0.8 | 1 | 0.8 |
| streets | 6.8 | 1 | 6.8 |
| sidewalks | 26.2 | 1 | 26.2 |
| road markings | 0.2 | 1 | 0.2 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 105.7 | 1 | 105.7 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.3 | 1 | 0.3 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.4 | 1 | 0.4 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.9 | 1 | 0.9 |
| snow piles | 0.1 | 1 | 0.1 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 1.9 | 1 | 1.9 |
| snow drifts | 3.5 | 1 | 3.5 |
| points | 0.0 | 1 | 0.0 |
| park litter | 15.2 | 1 | 15.2 |
| overhead | 8.7 | 1 | 8.7 |
| outdoor | 5.7 | 1 | 5.7 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 172.0 | 1 | 172.0 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 10.1 | 1 | 10.1 |
| snow tone | 1.1 | 1 | 1.1 |
| streets | 1.3 | 1 | 1.3 |
| sidewalks | 2.9 | 1 | 2.9 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 41.1 | 1 | 41.1 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.5 | 1 | 0.5 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.2 | 1 | 0.2 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.7 | 1 | 0.7 |
| parked cars | 1.1 | 1 | 1.1 |
| trees | 5.7 | 1 | 5.7 |
| snow drifts | 2.4 | 1 | 2.4 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.2 | 1 | 0.2 |
| overhead | 1.5 | 1 | 1.5 |
| outdoor | 3.3 | 1 | 3.3 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 2.6 | 1 | 2.6 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 1 | 0.0 |
| snow tone | 0.6 | 1 | 0.6 |
| streets | 0.4 | 1 | 0.4 |
| sidewalks | 0.5 | 1 | 0.5 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.7 | 1 | 0.7 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 1.4 | 1 | 1.4 |
| snow piles | 0.5 | 1 | 0.5 |
| parked cars | 0.5 | 1 | 0.5 |
| trees | 1.2 | 1 | 1.2 |
| snow drifts | 0.8 | 1 | 0.8 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.2 | 1 | 0.2 |
| overhead | 0.0 | 1 | 0.0 |
| outdoor | 2.0 | 1 | 2.0 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 101 | 14 |
| 186_141 | yes | 27 | 4 |
| 185_141 | yes | 13 | 3 |
| 185_142 | yes | 14 | 3 |
| 183_148 | yes | 193 | 13 |
| 196_145 | yes | 614 | 32 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 130 | 7 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 900 ms (slowest 161 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **112** | buildings 74 | 1 | 237 | 95 | 274,218 | 148,173 | 1 | 202 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **159** | buildings 109 | 1 | 437 | 171 | 521,708 | 268,091 | 4 | 402 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 130 + 7 | **64** | water 13 | 1 | 196 | 65 | 100,057 | 61,972 | 1 | 171 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **128** | buildings 80 | 1 | 261 | 107 | 479,533 | 249,747 | 3 | 216 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **161** | roofs 119 | 1 | 172 | 83 | 550,902 | 288,338 | 2 | 142 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **115** | roofs 92 | 1 | 172 | 81 | 340,977 | 176,726 | 1 | 138 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **79** | roofs 52 | 1 | 200 | 76 | 198,980 | 109,904 | 1 | 174 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **23** | fields 7 | 0 | 82 | 44 | 59,943 | 42,758 | 1 | 56 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **59** | roofs 29 | 1 | 178 | 66 | 165,925 | 94,342 | 1 | 163 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,937 | 788 | 788 | 365 | 2,692,243 | 1,440,051 | 45 | 1,886 | 15 (6) | 1,664 | 628 | 242 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (82,104 vertices, 53,935 triangles, 261 objects, 118 meshes; hash 1cc5e96fdd44dede vs 1cc5e96fdd44dede).

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.609 | 0% | 3,827 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.068 | 0% | 110 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.586 | 0% | 3,296 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.096 | 0% | 4,177 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.569 | 0% | 62 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.084 | 0% | 72 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.586 | 0% | 64 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.075 | 0% | 88 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.576 | 0% | 91 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.084 | 0% | 112 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.577 | 0% | 126 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 79 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.270 | 0% | 78 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.071 | 0% | 125 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.552 | 0% | 72 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.078 | 0% | 91 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.590 | 0% | 81 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.081 | 0% | 63 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.479 | 0% | 79 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.143 | 35% | 76 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.482 | 0% | 81 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.141 | 37% | 96 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.593 | 0% | 221 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.553 | 0% | 57 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.077 | 0% | 53 |

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
| setup | yes | 60 |  |
| square: KentuckyMap.Build | yes | 2,159 |  |
| square: count | yes | 9 |  |
| square: pick spots | yes | 78 |  |
| square: pictures | yes | 16,700 |  |
| square: discard | yes | 23 |  |
| square: passes (warm rebuild) | yes | 859 |  |
| chunks: stress test | yes | 1,729 |  |
| chunks: Louisville 3x3 | yes | 1,730 |  |
| chunks: determinism | yes | 79 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
