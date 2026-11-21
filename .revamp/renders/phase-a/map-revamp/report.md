# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 8.0 s, started 2026-10-23 22:46:21.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 1,093 ms |
| the same square again, warm, pass by pass | 406 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 2.6, 14.9, facing -95° |
| exits | North 3rd Street (north) at 59.9, 197.0; East Stephen Foster Avenue (east) at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; West Stephen Foster Avenue (west) at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 92 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 104 buildings, 121 beyond the edge, 152 street pieces, 433 containers, 10 lights, 23 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 3,126 | 1,051 | 1,051 | 277 | 622,673 | 381,811 | 41 | 2,096 | 33 (23) | 3,005 | 908 | 433 |
| RaidOverhead | 221 | 220 | 220 | 220 | 76,428 | 40,404 | 18 | 582 | 0 (0) | 0 | 220 | 0 |
| RaidScenery | 72 | 71 | 71 | 71 | 32,245 | 15,457 | 18 | 342 | 0 (0) | 0 | 39 | 0 |
| **all** | 3,419 | 1,342 | 1,342 | 568 | 731,346 | 437,672 | 42 | 3,020 | 33 (23) | 3,005 | 1,167 | 433 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 59 | 14% | 1 | 59 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 4 | 1% | 1 | 4 |
| water and lots | 0 | 0% | 1 | 0 |
| waterways | 0 | 0% | 1 | 0 |
| streets | 15 | 4% | 1 | 15 |
| sidewalks | 8 | 2% | 1 | 8 |
| road markings | 0 | 0% | 1 | 0 |
| railways | 0 | 0% | 1 | 0 |
| buildings | 191 | 47% | 41 | 23 |
| roofs | 16 | 4% | 1 | 16 |
| roof snow and fence posts | 2 | 1% | 1 | 2 |
| utility poles | 16 | 4% | 1 | 16 |
| street lights | 4 | 1% | 1 | 4 |
| signposts | 4 | 1% | 1 | 4 |
| hydrants | 1 | 0% | 1 | 1 |
| lot fences | 1 | 0% | 1 | 1 |
| snow piles | 3 | 1% | 1 | 3 |
| parked cars | 4 | 1% | 1 | 4 |
| trees | 7 | 2% | 1 | 7 |
| snow drifts | 17 | 4% | 1 | 17 |
| points | 1 | 0% | 1 | 1 |
| park litter | 0 | 0% | 1 | 0 |
| (emit the overhead meshes) | 10 | 3% | 1 | 10 |
| (emit the outdoor meshes) | 45 | 11% | 1 | 45 |
| **total** | **406** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **27** | buildings 8 | 1 | 260 | 117 | 81,032 | 51,975 | 2 | 231 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **33** | buildings 18 | 1 | 387 | 103 | 48,109 | 27,459 | 5 | 335 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 130 + 8 | **96** | buildings 60 | 1 | 236 | 94 | 272,591 | 146,037 | 1 | 202 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 413 + 20 | **43** | buildings 32 | 1 | 184 | 92 | 85,156 | 52,379 | 1 | 172 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 15 + 5 | **7** | outdoor 2 | 0 | 60 | 29 | 31,559 | 21,313 | 1 | 51 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.7 | 1 | 1.7 |
| ground | 0.4 | 1 | 0.4 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.6 | 1 | 1.6 |
| sidewalks | 3.3 | 1 | 3.3 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 8.3 | 1 | 8.3 |
| roofs | 2.9 | 1 | 2.9 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.7 | 1 | 0.7 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.8 | 1 | 0.8 |
| parked cars | 0.1 | 1 | 0.1 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 2.6 | 1 | 2.6 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 3.1 | 1 | 3.1 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.1 | 1 | 0.1 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.2 | 1 | 0.2 |
| sidewalks | 0.2 | 1 | 0.2 |
| road markings | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 27.4 | 2 | 18.2 |
| roofs | 0.5 | 1 | 0.5 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.4 | 1 | 0.4 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.0 | 1 | 0.0 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.1 | 1 | 0.1 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 0.5 | 1 | 0.5 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 0.7 | 1 | 0.7 |
| outdoor | 2.0 | 1 | 2.0 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.6 | 1 | 0.6 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.1 | 1 | 1.1 |
| sidewalks | 13.2 | 1 | 13.2 |
| road markings | 0.3 | 1 | 0.3 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 59.7 | 1 | 59.7 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.3 | 1 | 0.3 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.6 | 1 | 0.6 |
| snow piles | 0.1 | 1 | 0.1 |
| parked cars | 0.1 | 1 | 0.1 |
| trees | 0.3 | 1 | 0.3 |
| snow drifts | 2.5 | 1 | 2.5 |
| points | 0.0 | 1 | 0.0 |
| park litter | 6.9 | 1 | 6.9 |
| overhead | 6.0 | 1 | 6.0 |
| outdoor | 3.4 | 1 | 3.4 |

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
| buildings | 31.7 | 1 | 31.7 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.1 | 1 | 0.1 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.3 | 1 | 0.3 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.4 | 1 | 0.4 |
| snow drifts | 2.3 | 1 | 2.3 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 3.6 | 1 | 3.6 |

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
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.5 | 1 | 0.5 |
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
| outdoor | 1.8 | 1 | 1.8 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 44 | 10 |
| 186_141 | yes | 22 | 2 |
| 185_141 | yes | 14 | 1 |
| 185_142 | yes | 14 | 9 |
| 183_148 | yes | 130 | 8 |
| 196_145 | yes | 413 | 20 |
| 210_134 | yes | 15 | 5 |
| 182_148 | yes | 122 | 7 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 710 ms (slowest 130 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **78** | buildings 58 | 1 | 236 | 94 | 272,591 | 146,037 | 1 | 202 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **129** | buildings 91 | 1 | 435 | 169 | 520,495 | 266,106 | 4 | 402 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 122 + 7 | **43** | roofs 11 | 1 | 195 | 64 | 97,365 | 59,477 | 1 | 171 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **108** | buildings 71 | 1 | 260 | 106 | 477,200 | 247,660 | 3 | 216 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **130** | roofs 92 | 1 | 171 | 82 | 549,275 | 286,134 | 2 | 142 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **98** | roofs 78 | 1 | 169 | 78 | 339,738 | 174,890 | 1 | 138 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **64** | roofs 25 | 1 | 199 | 75 | 197,100 | 107,721 | 1 | 173 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **15** | roofs 3 | 0 | 81 | 43 | 57,818 | 40,331 | 1 | 55 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **44** | roofs 30 | 1 | 177 | 65 | 164,653 | 92,207 | 1 | 163 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,925 | 776 | 776 | 353 | 2,676,235 | 1,420,563 | 39 | 1,799 | 15 (6) | 1,662 | 628 | 242 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (81,032 vertices, 51,975 triangles, 260 objects, 117 meshes; hash d9c3c6d375ee5ffa vs d9c3c6d375ee5ffa).

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.616 | 0% | 835 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.069 | 0% | 107 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.589 | 0% | 100 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.097 | 0% | 63 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.572 | 0% | 60 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.084 | 0% | 76 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.593 | 0% | 54 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.076 | 0% | 60 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.578 | 0% | 54 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.085 | 0% | 49 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.524 | 0% | 56 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.080 | 0% | 55 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.283 | 0% | 53 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.052 | 1% | 64 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.557 | 0% | 63 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.078 | 0% | 49 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.603 | 0% | 49 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.082 | 0% | 47 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.479 | 0% | 70 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.144 | 35% | 57 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.489 | 0% | 57 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.145 | 37% | 77 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.596 | 0% | 251 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.560 | 0% | 54 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.078 | 0% | 50 |

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
| setup | yes | 72 |  |
| square: KentuckyMap.Build | yes | 1,095 |  |
| square: count | yes | 9 |  |
| square: pick spots | yes | 77 |  |
| square: pictures | yes | 3,765 |  |
| square: discard | yes | 15 |  |
| square: passes (warm rebuild) | yes | 418 |  |
| chunks: stress test | yes | 929 |  |
| chunks: Louisville 3x3 | yes | 1,530 |  |
| chunks: determinism | yes | 89 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
