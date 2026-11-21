# CLONE-Z map harness: Falmouth (build ky-local-f2)

**Exit 0**: ok. Full run, 18.7 s, started 2026-10-24 01:50:55.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Falmouth plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 2,889 ms |
| the same square again, warm, pass by pass | 1,340 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 84.7, -22.5, facing -65° |
| exits | Chapel Street (north) at -2.5, 197.0; KY 22 (east) at 197.0, 173.2; Main Street (south) at -60.4, -197.0; West Shelby Street (west) at -197.0, 107.3 |
| overhead renderers (hidden in the iso view) | 91 |
| `KentuckyMap.LastStats` | Kentucky map Falmouth (build ky-local-f2): 139 buildings, 149 beyond the edge, 458 street pieces, 449 containers, 18 lights, 105 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 150 with their middle inside the square (11 of them reach within 1 m of its edge or past it), 162 in the scenery ring; inside: house 69, garage 27, commercial 19, shed 9, apartments 8, retail 6, office 5, civic 4, church 2, canopy 1 |
| map data: streets, lines, areas | 458 street pieces, 7 lines (stream 5, rail 1, river 1), 91 land-use areas (residential 27, parking 15, commercial 8, grass 8, churchyard 7, forest 7, industrial 6, pitch 3, retail 3, school 2, water 2, meadow 1, park 1, plaza 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 4,144 | 1,082 | 1,082 | 273 | 735,334 | 467,270 | 52 | 2,282 | 44 (26) | 4,226 | 933 | 449 |
| RaidOverhead | 265 | 264 | 264 | 264 | 244,940 | 139,998 | 26 | 787 | 0 (0) | 0 | 264 | 0 |
| RaidScenery | 89 | 88 | 88 | 88 | 76,755 | 44,947 | 20 | 400 | 0 (0) | 0 | 55 | 0 |
| **all** | 4,498 | 1,434 | 1,434 | 625 | 1,057,029 | 652,215 | 55 | 3,469 | 44 (26) | 4,226 | 1,252 | 449 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 150 | 11% | 1 | 150 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water and lots | 4 | 0% | 1 | 4 |
| waterways | 6 | 0% | 1 | 6 |
| streets | 10 | 1% | 1 | 10 |
| sidewalks | 25 | 2% | 1 | 25 |
| road markings | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| buildings | 482 | 36% | 52 | 51 |
| roofs | 18 | 1% | 1 | 18 |
| roof snow and fence posts | 21 | 2% | 1 | 21 |
| utility lines | 74 | 6% | 1 | 74 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 39 | 3% | 1 | 39 |
| street lights | 27 | 2% | 1 | 27 |
| signs | 44 | 3% | 1 | 44 |
| street furniture | 131 | 10% | 1 | 131 |
| landmarks | 2 | 0% | 1 | 2 |
| fences and walls | 14 | 1% | 1 | 14 |
| park litter | 11 | 1% | 2 | 10 |
| trees | 71 | 5% | 1 | 71 |
| snow drifts | 73 | 5% | 1 | 73 |
| points | 0 | 0% | 1 | 0 |
| (emit the overhead meshes) | 36 | 3% | 1 | 36 |
| (emit the outdoor meshes) | 100 | 7% | 1 | 100 |
| **total** | **1,340** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Falmouth | 12683_9851 (198_153) | 0 + 0 | **137** | buildings 34 | 4 | 682 | 205 | 126,711 | 76,182 | 6 | 693 | 25 | 34/25/9 | 0E 0W |
| rural-5km | 5 km from Falmouth (bearing 50°) | 12713_9876 (198_154) | 0 + 0 | **67** | trees 33 | 0 | 71 | 34 | 33,008 | 21,460 | 0 | 82 | 2 | 2/2/6 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 298 + 14 | **201** | buildings 149 | 1 | 239 | 97 | 265,570 | 142,440 | 0 | 197 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 765 + 91 | **47** | buildings 24 | 1 | 180 | 97 | 86,998 | 53,549 | 0 | 170 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 24 + 2 | **24** | trees 8 | 0 | 56 | 39 | 19,789 | 16,211 | 0 | 41 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 3.4 | 1 | 3.4 |
| ground | 0.6 | 1 | 0.6 |
| water and lots | 0.1 | 1 | 0.1 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.4 | 1 | 1.4 |
| sidewalks | 3.4 | 1 | 3.4 |
| road markings | 0.2 | 1 | 0.2 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 73.7 | 4 | 33.5 |
| roofs | 4.2 | 1 | 4.2 |
| roof snow and fence posts | 0.4 | 1 | 0.4 |
| utility lines | 1.4 | 1 | 1.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 3.3 | 1 | 3.3 |
| street lights | 0.8 | 1 | 0.8 |
| signs | 1.2 | 1 | 1.2 |
| street furniture | 1.5 | 1 | 1.5 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.3 | 1 | 0.3 |
| park litter | 1.0 | 2 | 1.0 |
| trees | 0.2 | 1 | 0.2 |
| snow drifts | 3.5 | 1 | 3.5 |
| points | 0.0 | 1 | 0.0 |
| overhead | 14.6 | 1 | 14.6 |
| outdoor | 21.3 | 1 | 21.3 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 4.2 | 1 | 4.2 |
| ground | 0.7 | 1 | 0.7 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.4 | 1 | 1.4 |
| road markings | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| roofs | 22.0 | 1 | 22.0 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 1.1 | 1 | 1.1 |
| park litter | 0.0 | 2 | 0.0 |
| trees | 33.2 | 1 | 33.2 |
| snow drifts | 0.4 | 1 | 0.4 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.4 | 1 | 0.4 |
| outdoor | 2.5 | 1 | 2.5 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 0.3 | 1 | 0.3 |
| water and lots | 0.1 | 1 | 0.1 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.6 | 1 | 1.6 |
| sidewalks | 6.0 | 1 | 6.0 |
| road markings | 0.2 | 1 | 0.2 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 148.9 | 1 | 148.9 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.3 | 1 | 0.3 |
| utility lines | 1.9 | 1 | 1.9 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.5 | 1 | 0.5 |
| street lights | 0.1 | 1 | 0.1 |
| signs | 0.5 | 1 | 0.5 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 18.6 | 2 | 16.9 |
| trees | 0.5 | 1 | 0.5 |
| snow drifts | 3.9 | 1 | 3.9 |
| points | 0.0 | 1 | 0.0 |
| overhead | 9.8 | 1 | 9.8 |
| outdoor | 7.1 | 1 | 7.1 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 0.3 | 1 | 0.3 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.3 | 1 | 1.3 |
| sidewalks | 2.7 | 1 | 2.7 |
| road markings | 0.2 | 1 | 0.2 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 24.0 | 1 | 24.0 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility lines | 0.8 | 1 | 0.8 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.7 | 1 | 1.7 |
| street lights | 0.1 | 1 | 0.1 |
| signs | 0.7 | 1 | 0.7 |
| street furniture | 1.2 | 1 | 1.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.2 | 2 | 1.1 |
| trees | 1.0 | 1 | 1.0 |
| snow drifts | 3.5 | 1 | 3.5 |
| points | 0.1 | 1 | 0.1 |
| overhead | 1.5 | 1 | 1.5 |
| outdoor | 6.4 | 1 | 6.4 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.3 | 1 | 0.3 |
| water and lots | 0.2 | 1 | 0.2 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.6 | 1 | 0.6 |
| sidewalks | 0.6 | 1 | 0.6 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility lines | 0.6 | 1 | 0.6 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 4.4 | 1 | 4.4 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 2.5 | 1 | 2.5 |
| street furniture | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 2.5 | 2 | 2.2 |
| trees | 8.0 | 1 | 8.0 |
| snow drifts | 1.1 | 1 | 1.1 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 3.0 | 1 | 3.0 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 198_154 | yes | 83 | 44 |
| 198_153 | yes | 274 | 10 |
| 197_153 | yes | 73 | 5 |
| 197_154 | yes | 67 | 12 |
| 183_148 | yes | 298 | 14 |
| 196_145 | yes | 765 | 91 |
| 210_134 | yes | 24 | 2 |
| 182_148 | yes | 247 | 12 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 1,310 ms (slowest 305 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **106** | buildings 68 | 1 | 239 | 97 | 265,570 | 142,440 | 0 | 197 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **295** | buildings 223 | 1 | 437 | 171 | 520,356 | 266,265 | 3 | 397 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 247 + 12 | **58** | roofs 19 | 1 | 198 | 72 | 76,260 | 49,081 | 0 | 159 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **305** | buildings 252 | 1 | 257 | 114 | 441,340 | 229,395 | 4 | 215 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **213** | roofs 172 | 1 | 183 | 95 | 544,874 | 285,807 | 1 | 147 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **126** | roofs 108 | 1 | 170 | 84 | 339,982 | 175,506 | 0 | 128 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **82** | roofs 51 | 2 | 190 | 79 | 178,559 | 98,859 | 0 | 162 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **36** | outdoor 15 | 0 | 84 | 56 | 44,573 | 34,881 | 0 | 53 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **89** | roofs 60 | 1 | 191 | 74 | 164,932 | 93,556 | 1 | 150 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,951 | 842 | 842 | 426 | 2,576,446 | 1,375,790 | 50 | 1,884 | 9 (6) | 1,608 | 700 | 242 |

### Determinism

Chunk 12683_9851 (start-centre) built twice: **match**: identical, in the same order (126,711 vertices, 76,182 triangles, 682 objects, 205 meshes; hash ee62c44539cf94cf vs ee62c44539cf94cf).

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.606 | 0% | 2,555 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 286 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.568 | 0% | 126 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.120 | 0% | 230 |
| `iso_downtown_day.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.527 | 0% | 57 |
| `iso_downtown_night.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.073 | 0% | 78 |
| `iso_residential_day.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.546 | 0% | 132 |
| `iso_residential_night.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.079 | 0% | 144 |
| `iso_intersection_day.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.501 | 0% | 100 |
| `iso_intersection_night.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.081 | 0% | 88 |
| `iso_green_day.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.547 | 0% | 75 |
| `iso_green_night.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.076 | 0% | 69 |
| `iso_parking_day.png` | Parking: a parking, 2,703 m² inside the square | 0.458 | 0% | 90 |
| `iso_parking_night.png` | Parking: a parking, 2,703 m² inside the square | 0.062 | 0% | 84 |
| `iso_edge_day.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.533 | 0% | 86 |
| `iso_edge_night.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.077 | 0% | 55 |
| `iso_waterway_day.png` | Waterway: a stream | 0.490 | 0% | 107 |
| `iso_waterway_night.png` | Waterway: a stream | 0.092 | 0% | 67 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.579 | 0% | 68 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.075 | 0% | 101 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.469 | 0% | 115 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.155 | 28% | 101 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.472 | 0% | 99 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.150 | 33% | 68 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.595 | 0% | 295 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.560 | 0% | 54 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.078 | 0% | 87 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 84.7, -22.5 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | 7.1, 16.2 | the place's own point in places.json (its OSM place node, which the square is built around), moved 17.7 m onto the nearest street |
| residential | -70.4, -100.1 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | 25.1, 8.4 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | 108.4, -123.7 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | 2.4, 106.8 | the biggest parking area inside the square |
| edge | -60.9, -200.0 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | 116.8, 2.4 | the point of a railway, river, canal or stream nearest the square's middle |
| landmark | 74.3, 43.9 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 80 |  |
| square: KentuckyMap.Build | yes | 2,891 |  |
| square: count | yes | 33 |  |
| square: pick spots | yes | 181 |  |
| square: pictures | yes | 9,134 |  |
| square: discard | yes | 38 |  |
| square: passes (warm rebuild) | yes | 1,383 |  |
| chunks: stress test | yes | 2,298 |  |
| chunks: Louisville 3x3 | yes | 2,410 |  |
| chunks: determinism | yes | 198 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
