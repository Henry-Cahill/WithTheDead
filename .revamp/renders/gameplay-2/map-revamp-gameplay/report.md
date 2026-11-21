# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 15.9 s, started 2026-10-24 01:50:56.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 1,522 ms |
| the same square again, warm, pass by pass | 903 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 18.3, 53.5, facing -166° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 92 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 104 buildings, 121 beyond the edge, 152 street pieces, 351 containers, 10 lights, 23 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 2,974 | 897 | 897 | 277 | 613,763 | 374,601 | 51 | 1,951 | 33 (23) | 2,853 | 754 | 351 |
| RaidOverhead | 221 | 220 | 220 | 220 | 76,428 | 40,404 | 18 | 582 | 0 (0) | 0 | 220 | 0 |
| RaidScenery | 72 | 71 | 71 | 71 | 32,245 | 15,457 | 18 | 342 | 0 (0) | 0 | 39 | 0 |
| **all** | 3,267 | 1,188 | 1,188 | 568 | 722,436 | 430,462 | 52 | 2,875 | 33 (23) | 2,853 | 1,013 | 351 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 149 | 16% | 1 | 149 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water and lots | 0 | 0% | 1 | 0 |
| waterways | 0 | 0% | 1 | 0 |
| streets | 15 | 2% | 1 | 15 |
| sidewalks | 12 | 1% | 1 | 12 |
| road markings | 1 | 0% | 1 | 1 |
| railways | 0 | 0% | 1 | 0 |
| buildings | 411 | 46% | 41 | 47 |
| roofs | 26 | 3% | 1 | 26 |
| roof snow and fence posts | 3 | 0% | 1 | 3 |
| utility poles | 37 | 4% | 1 | 37 |
| street lights | 7 | 1% | 1 | 7 |
| signposts | 11 | 1% | 1 | 11 |
| hydrants | 1 | 0% | 1 | 1 |
| lot fences | 2 | 0% | 1 | 2 |
| snow piles | 4 | 0% | 1 | 4 |
| parked cars | 20 | 2% | 1 | 20 |
| trees | 24 | 3% | 1 | 24 |
| snow drifts | 64 | 7% | 1 | 64 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 3 | 0% | 1 | 3 |
| (emit the overhead meshes) | 52 | 6% | 1 | 52 |
| (emit the outdoor meshes) | 58 | 6% | 1 | 58 |
| **total** | **903** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **95** | outdoor 39 | 1 | 251 | 108 | 80,752 | 51,799 | 2 | 222 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **161** | buildings 138 | 1 | 378 | 94 | 47,637 | 27,079 | 5 | 326 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 358 + 16 | **210** | buildings 133 | 1 | 204 | 89 | 271,244 | 144,398 | 1 | 169 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 647 + 41 | **67** | buildings 49 | 1 | 177 | 89 | 85,118 | 52,341 | 1 | 164 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 7 + 5 | **17** | utility poles 6 | 0 | 55 | 29 | 31,263 | 20,911 | 1 | 46 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.9 | 1 | 2.9 |
| ground | 0.6 | 1 | 0.6 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 2.2 | 1 | 2.2 |
| sidewalks | 4.9 | 1 | 4.9 |
| road markings | 0.2 | 1 | 0.2 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 15.4 | 1 | 15.4 |
| roofs | 12.5 | 1 | 12.5 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.8 | 1 | 0.8 |
| street lights | 1.0 | 1 | 1.0 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 5.0 | 1 | 5.0 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 7.9 | 1 | 7.9 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| overhead | 1.3 | 1 | 1.3 |
| outdoor | 39.1 | 1 | 39.1 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 1.7 | 1 | 1.7 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 2.5 | 1 | 2.5 |
| sidewalks | 0.8 | 1 | 0.8 |
| road markings | 0.2 | 1 | 0.2 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 147.2 | 2 | 138.1 |
| roofs | 0.8 | 1 | 0.8 |
| roof snow and fence posts | 0.1 | 1 | 0.1 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.8 | 1 | 0.8 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.0 | 1 | 0.0 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.3 | 1 | 0.3 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 0.6 | 1 | 0.6 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| overhead | 1.1 | 1 | 1.1 |
| outdoor | 3.3 | 1 | 3.3 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 0.3 | 1 | 0.3 |
| water and lots | 0.8 | 1 | 0.8 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.6 | 1 | 1.6 |
| sidewalks | 43.5 | 1 | 43.5 |
| road markings | 0.5 | 1 | 0.5 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 133.3 | 1 | 133.3 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.3 | 1 | 0.3 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.5 | 1 | 0.5 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 1.1 | 1 | 1.1 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.4 | 1 | 0.4 |
| snow drifts | 11.7 | 1 | 11.7 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| overhead | 8.2 | 1 | 8.2 |
| outdoor | 6.2 | 1 | 6.2 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 0.3 | 1 | 0.3 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.3 | 1 | 1.3 |
| sidewalks | 2.2 | 1 | 2.2 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 48.8 | 1 | 48.8 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.5 | 1 | 0.5 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.3 | 1 | 0.3 |
| parked cars | 0.3 | 1 | 0.3 |
| trees | 0.5 | 1 | 0.5 |
| snow drifts | 3.6 | 1 | 3.6 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| overhead | 1.4 | 1 | 1.4 |
| outdoor | 6.3 | 1 | 6.3 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.3 | 1 | 0.3 |
| water and lots | 0.4 | 1 | 0.4 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 2.0 | 1 | 2.0 |
| sidewalks | 1.2 | 1 | 1.2 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 6.3 | 1 | 6.3 |
| street lights | 0.7 | 1 | 0.7 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.2 | 1 | 0.2 |
| lot fences | 1.2 | 1 | 1.2 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.4 | 1 | 0.4 |
| trees | 1.0 | 1 | 1.0 |
| snow drifts | 0.9 | 1 | 0.9 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.0 | 1 | 0.0 |
| outdoor | 2.2 | 1 | 2.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 86 | 14 |
| 186_141 | yes | 18 | 4 |
| 185_141 | yes | 10 | 3 |
| 185_142 | yes | 29 | 3 |
| 183_148 | yes | 358 | 16 |
| 196_145 | yes | 647 | 41 |
| 210_134 | yes | 7 | 5 |
| 182_148 | yes | 258 | 13 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 1,735 ms (slowest 554 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **131** | buildings 105 | 1 | 204 | 89 | 271,244 | 144,398 | 1 | 169 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **264** | buildings 169 | 2 | 404 | 143 | 518,914 | 264,720 | 4 | 371 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 258 + 13 | **183** | roofs 59 | 2 | 180 | 62 | 96,694 | 58,602 | 1 | 156 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **554** | buildings 355 | 1 | 245 | 91 | 476,264 | 246,868 | 3 | 201 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **239** | roofs 191 | 1 | 149 | 68 | 547,910 | 284,774 | 2 | 120 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **166** | roofs 144 | 1 | 147 | 64 | 338,367 | 173,536 | 1 | 115 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **110** | roofs 83 | 1 | 187 | 63 | 196,556 | 107,305 | 1 | 161 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **23** | roofs 5 | 0 | 76 | 41 | 57,532 | 40,017 | 1 | 48 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **66** | roofs 41 | 1 | 163 | 59 | 164,122 | 91,748 | 1 | 149 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,757 | 680 | 680 | 353 | 2,667,603 | 1,411,968 | 36 | 1,675 | 15 (6) | 1,490 | 532 | 191 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (80,752 vertices, 51,799 triangles, 251 objects, 108 meshes; hash fd9cdac96b2d0d85 vs fd9cdac96b2d0d85).

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.616 | 0% | 1,990 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.069 | 0% | 176 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on North 3rd Street, at the game's starting zoom | 0.574 | 0% | 204 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on North 3rd Street, at the game's starting zoom | 0.092 | 0% | 155 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.572 | 0% | 93 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.084 | 0% | 69 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.593 | 0% | 152 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.076 | 0% | 50 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.578 | 0% | 70 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.085 | 0% | 64 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.524 | 0% | 76 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.080 | 0% | 92 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.283 | 0% | 79 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.052 | 1% | 60 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.555 | 0% | 69 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.078 | 0% | 100 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.603 | 0% | 54 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.082 | 0% | 58 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on North 3rd Street (downtown), looking along the street | 0.436 | 0% | 219 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on North 3rd Street (downtown), looking along the street | 0.171 | 25% | 76 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.489 | 0% | 86 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.145 | 37% | 139 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.596 | 0% | 247 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.560 | 0% | 61 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.078 | 0% | 63 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 18.3, 53.5 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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
| setup | yes | 55 |  |
| square: KentuckyMap.Build | yes | 1,525 |  |
| square: count | yes | 55 |  |
| square: pick spots | yes | 284 |  |
| square: pictures | yes | 8,003 |  |
| square: discard | yes | 77 |  |
| square: passes (warm rebuild) | yes | 940 |  |
| chunks: stress test | yes | 1,817 |  |
| chunks: Louisville 3x3 | yes | 2,968 |  |
| chunks: determinism | yes | 132 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
