# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 8.3 s, started 2026-10-24 01:51:52.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 1,395 ms |
| the same square again, warm, pass by pass | 452 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | -2.0, 14.2, facing -98° |
| exits | North 3rd Street (north) at 59.9, 197.0; East Stephen Foster Avenue (east) at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; West Stephen Foster Avenue (west) at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 92 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 104 buildings, 121 beyond the edge, 152 street pieces, 433 containers, 10 lights, 23 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 3,084 | 1,060 | 1,060 | 286 | 751,560 | 427,101 | 44 | 2,060 | 33 (23) | 2,910 | 909 | 433 |
| RaidOverhead | 221 | 220 | 220 | 220 | 76,428 | 40,404 | 18 | 582 | 0 (0) | 0 | 220 | 0 |
| RaidScenery | 84 | 83 | 83 | 83 | 26,297 | 12,579 | 18 | 265 | 0 (0) | 0 | 39 | 0 |
| **all** | 3,389 | 1,363 | 1,363 | 589 | 854,285 | 480,084 | 45 | 2,907 | 33 (23) | 2,910 | 1,168 | 433 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 58 | 13% | 1 | 58 |
| (set up the site) | 4 | 1% | 1 | 4 |
| ground | 0 | 0% | 1 | 0 |
| water and lots | 0 | 0% | 1 | 0 |
| waterways | 0 | 0% | 1 | 0 |
| road network | 7 | 2% | 1 | 7 |
| junctions | 4 | 1% | 1 | 4 |
| streets | 36 | 8% | 1 | 36 |
| sidewalks | 23 | 5% | 1 | 23 |
| berms and driveways | 13 | 3% | 1 | 13 |
| road markings | 1 | 0% | 1 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 181 | 40% | 41 | 13 |
| roofs | 13 | 3% | 1 | 13 |
| roof snow and fence posts | 2 | 0% | 1 | 2 |
| utility poles | 11 | 3% | 1 | 11 |
| street lights | 3 | 1% | 1 | 3 |
| signposts | 2 | 0% | 1 | 2 |
| hydrants | 0 | 0% | 1 | 0 |
| lot fences | 1 | 0% | 1 | 1 |
| snow piles | 1 | 0% | 1 | 1 |
| parked cars | 21 | 5% | 1 | 21 |
| trees | 5 | 1% | 1 | 5 |
| snow drifts | 13 | 3% | 1 | 13 |
| points | 1 | 0% | 1 | 1 |
| park litter | 0 | 0% | 1 | 0 |
| (emit the overhead meshes) | 13 | 3% | 1 | 13 |
| (emit the outdoor meshes) | 36 | 8% | 1 | 36 |
| **total** | **452** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **38** | buildings 8 | 1 | 234 | 117 | 73,452 | 45,215 | 2 | 192 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **19** | buildings 6 | 1 | 385 | 103 | 53,022 | 29,204 | 5 | 333 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 139 + 8 | **90** | buildings 50 | 1 | 230 | 93 | 259,204 | 138,110 | 0 | 198 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 390 + 26 | **69** | buildings 38 | 1 | 176 | 91 | 90,513 | 53,049 | 0 | 162 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 11 + 1 | **12** | streets 4 | 0 | 58 | 29 | 38,371 | 23,353 | 1 | 47 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.1 | 1 | 2.1 |
| ground | 0.4 | 1 | 0.4 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| road network | 1.3 | 1 | 1.3 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 7.7 | 1 | 7.7 |
| sidewalks | 1.8 | 1 | 1.8 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 8.3 | 1 | 8.3 |
| roofs | 6.5 | 1 | 6.5 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.4 | 1 | 0.4 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.1 | 1 | 0.1 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 2.6 | 1 | 2.6 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 2.7 | 1 | 2.7 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.1 | 1 | 0.1 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 1.8 | 1 | 1.8 |
| sidewalks | 0.3 | 1 | 0.3 |
| berms and driveways | 0.6 | 1 | 0.6 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.9 | 2 | 5.5 |
| roofs | 0.5 | 1 | 0.5 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.3 | 1 | 0.3 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.0 | 1 | 0.0 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 0.4 | 1 | 0.4 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 2.5 | 1 | 2.5 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.6 | 1 | 0.6 |
| waterways | 0.0 | 1 | 0.0 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.1 | 1 | 1.1 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 4.3 | 1 | 4.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 50.4 | 1 | 50.4 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.0 | 1 | 0.0 |
| lot fences | 0.6 | 1 | 0.6 |
| snow piles | 0.0 | 1 | 0.0 |
| parked cars | 0.1 | 1 | 0.1 |
| trees | 0.3 | 1 | 0.3 |
| snow drifts | 2.5 | 1 | 2.5 |
| points | 0.0 | 1 | 0.0 |
| park litter | 7.5 | 1 | 7.5 |
| overhead | 9.3 | 1 | 9.3 |
| outdoor | 8.7 | 1 | 8.7 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 13.5 | 1 | 13.5 |
| sidewalks | 2.4 | 1 | 2.4 |
| berms and driveways | 3.3 | 1 | 3.3 |
| road markings | 0.9 | 1 | 0.9 |
| paths | 0.9 | 1 | 0.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 38.2 | 1 | 38.2 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.1 | 1 | 0.1 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.0 | 1 | 0.0 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.0 | 1 | 0.0 |
| parked cars | 0.3 | 1 | 0.3 |
| trees | 0.4 | 1 | 0.4 |
| snow drifts | 2.3 | 1 | 2.3 |
| points | 1.1 | 1 | 1.1 |
| park litter | 0.1 | 1 | 0.1 |
| overhead | 1.1 | 1 | 1.1 |
| outdoor | 3.0 | 1 | 3.0 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.1 | 1 | 0.1 |
| waterways | 0.0 | 1 | 0.0 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 3.6 | 1 | 3.6 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.4 | 1 | 0.4 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 1.2 | 1 | 1.2 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.4 | 1 | 0.4 |
| trees | 0.8 | 1 | 0.8 |
| snow drifts | 0.8 | 1 | 0.8 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| overhead | 0.0 | 1 | 0.0 |
| outdoor | 1.9 | 1 | 1.9 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 43 | 8 |
| 186_141 | yes | 11 | 2 |
| 185_141 | yes | 11 | 9 |
| 185_142 | yes | 15 | 2 |
| 183_148 | yes | 139 | 8 |
| 196_145 | yes | 390 | 26 |
| 210_134 | yes | 11 | 1 |
| 182_148 | yes | 116 | 7 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 758 ms (slowest 128 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **86** | buildings 61 | 1 | 230 | 93 | 259,204 | 138,110 | 0 | 198 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **128** | buildings 94 | 1 | 433 | 169 | 525,354 | 267,810 | 3 | 394 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 116 + 7 | **55** | outdoor 11 | 1 | 189 | 63 | 108,132 | 63,023 | 0 | 162 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **124** | buildings 68 | 1 | 249 | 105 | 484,843 | 249,387 | 2 | 206 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **123** | roofs 79 | 0 | 160 | 82 | 559,730 | 288,966 | 2 | 118 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **92** | roofs 68 | 1 | 169 | 80 | 347,537 | 177,700 | 1 | 129 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **73** | roofs 50 | 0 | 187 | 75 | 202,638 | 108,003 | 0 | 149 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **24** | streets 5 | 0 | 68 | 42 | 71,034 | 44,061 | 0 | 38 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **54** | roofs 22 | 0 | 172 | 63 | 171,656 | 94,506 | 0 | 146 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,859 | 772 | 772 | 356 | 2,730,128 | 1,431,566 | 42 | 1,629 | 8 (6) | 1,540 | 628 | 242 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (73,452 vertices, 45,215 triangles, 234 objects, 117 meshes; hash 4d6a5a576135526a vs 4d6a5a576135526a).

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.613 | 0% | 851 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.069 | 0% | 108 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.484 | 0% | 107 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.093 | 0% | 56 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.493 | 0% | 46 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.079 | 0% | 64 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.585 | 0% | 77 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.076 | 0% | 52 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.491 | 0% | 52 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.077 | 0% | 53 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.524 | 0% | 50 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.080 | 0% | 49 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.280 | 0% | 53 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.052 | 1% | 53 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.495 | 0% | 59 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.078 | 0% | 49 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.618 | 0% | 50 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.082 | 0% | 47 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.457 | 0% | 58 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.148 | 34% | 50 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.483 | 0% | 46 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.147 | 37% | 52 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.547 | 0% | 219 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.472 | 0% | 50 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.078 | 0% | 50 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -2.0, 14.2 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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
| setup | yes | 53 |  |
| square: KentuckyMap.Build | yes | 1,396 |  |
| square: count | yes | 9 |  |
| square: pick spots | yes | 66 |  |
| square: pictures | yes | 3,776 |  |
| square: discard | yes | 13 |  |
| square: passes (warm rebuild) | yes | 464 |  |
| chunks: stress test | yes | 916 |  |
| chunks: Louisville 3x3 | yes | 1,563 |  |
| chunks: determinism | yes | 85 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
