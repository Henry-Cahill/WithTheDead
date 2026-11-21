# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 145.7 s, started 2026-10-24 00:22:52.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 1,269 ms |
| the same square again, warm, pass by pass | 588 ms |
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
| (read the area) | 73 | 12% | 1 | 73 |
| (set up the site) | 4 | 1% | 1 | 4 |
| ground | 1 | 0% | 1 | 1 |
| water and lots | 2 | 0% | 1 | 2 |
| waterways | 0 | 0% | 1 | 0 |
| streets | 25 | 4% | 1 | 25 |
| sidewalks | 12 | 2% | 1 | 12 |
| road markings | 1 | 0% | 1 | 1 |
| railways | 0 | 0% | 1 | 0 |
| buildings | 338 | 58% | 41 | 58 |
| roofs | 10 | 2% | 1 | 10 |
| roof snow and fence posts | 2 | 0% | 1 | 2 |
| utility poles | 23 | 4% | 1 | 23 |
| street lights | 4 | 1% | 1 | 4 |
| signposts | 3 | 0% | 1 | 3 |
| hydrants | 1 | 0% | 1 | 1 |
| lot fences | 1 | 0% | 1 | 1 |
| snow piles | 2 | 0% | 1 | 2 |
| parked cars | 4 | 1% | 1 | 4 |
| trees | 4 | 1% | 1 | 4 |
| snow drifts | 21 | 4% | 1 | 21 |
| points | 1 | 0% | 1 | 1 |
| park litter | 0 | 0% | 1 | 0 |
| (emit the overhead meshes) | 10 | 2% | 1 | 10 |
| (emit the outdoor meshes) | 47 | 8% | 1 | 47 |
| **total** | **588** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **48** | snow drifts 9 | 1 | 260 | 117 | 81,032 | 51,975 | 2 | 231 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **20** | buildings 6 | 2 | 387 | 103 | 48,109 | 27,459 | 5 | 335 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 130 + 8 | **105** | buildings 62 | 1 | 236 | 94 | 272,591 | 146,037 | 1 | 202 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 409 + 26 | **45** | buildings 32 | 1 | 184 | 92 | 85,156 | 52,379 | 1 | 172 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 6 + 14 | **11** | outdoor 3 | 0 | 60 | 29 | 31,559 | 21,313 | 1 | 51 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.7 | 1 | 2.7 |
| ground | 0.6 | 1 | 0.6 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.8 | 1 | 1.8 |
| sidewalks | 3.3 | 1 | 3.3 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 8.3 | 1 | 8.3 |
| roofs | 3.2 | 1 | 3.2 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.7 | 1 | 0.7 |
| signposts | 3.8 | 1 | 3.8 |
| hydrants | 0.2 | 1 | 0.2 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 3.7 | 1 | 3.7 |
| parked cars | 0.8 | 1 | 0.8 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 9.0 | 1 | 9.0 |
| points | 0.1 | 1 | 0.1 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 1.0 | 1 | 1.0 |
| outdoor | 8.1 | 1 | 8.1 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.2 | 1 | 0.2 |
| sidewalks | 0.2 | 1 | 0.2 |
| road markings | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 11.8 | 2 | 6.0 |
| roofs | 0.6 | 1 | 0.6 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.5 | 1 | 0.5 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.0 | 1 | 0.0 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.1 | 1 | 0.1 |
| parked cars | 0.3 | 1 | 0.3 |
| trees | 0.0 | 1 | 0.0 |
| snow drifts | 0.6 | 1 | 0.6 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 1.2 | 1 | 1.2 |
| outdoor | 3.4 | 1 | 3.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.5 | 1 | 0.5 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.5 | 1 | 1.5 |
| sidewalks | 11.0 | 1 | 11.0 |
| road markings | 0.3 | 1 | 0.3 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 61.9 | 1 | 61.9 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.3 | 1 | 0.3 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 3.3 | 1 | 3.3 |
| lot fences | 5.1 | 1 | 5.1 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.4 | 1 | 0.4 |
| snow drifts | 2.7 | 1 | 2.7 |
| points | 0.0 | 1 | 0.0 |
| park litter | 7.3 | 1 | 7.3 |
| overhead | 5.0 | 1 | 5.0 |
| outdoor | 3.8 | 1 | 3.8 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.8 | 1 | 1.8 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 32.1 | 1 | 32.1 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.1 | 1 | 0.1 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.6 | 1 | 0.6 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.2 | 1 | 0.2 |
| parked cars | 0.2 | 1 | 0.2 |
| trees | 0.7 | 1 | 0.7 |
| snow drifts | 2.7 | 1 | 2.7 |
| points | 0.1 | 1 | 0.1 |
| park litter | 0.3 | 1 | 0.3 |
| overhead | 1.1 | 1 | 1.1 |
| outdoor | 3.8 | 1 | 3.8 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.3 | 1 | 0.3 |
| water and lots | 0.2 | 1 | 0.2 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.5 | 1 | 0.5 |
| sidewalks | 0.5 | 1 | 0.5 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.7 | 1 | 0.7 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.2 | 1 | 0.2 |
| lot fences | 1.5 | 1 | 1.5 |
| snow piles | 0.3 | 1 | 0.3 |
| parked cars | 0.5 | 1 | 0.5 |
| trees | 1.1 | 1 | 1.1 |
| snow drifts | 1.0 | 1 | 1.0 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.2 | 1 | 0.2 |
| overhead | 0.0 | 1 | 0.0 |
| outdoor | 3.2 | 1 | 3.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 51 | 8 |
| 186_141 | yes | 11 | 2 |
| 185_141 | yes | 10 | 6 |
| 185_142 | yes | 12 | 2 |
| 183_148 | yes | 130 | 8 |
| 196_145 | yes | 409 | 26 |
| 210_134 | yes | 6 | 14 |
| 182_148 | yes | 116 | 7 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 777 ms (slowest 142 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **111** | buildings 86 | 1 | 236 | 94 | 272,591 | 146,037 | 1 | 202 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **142** | buildings 100 | 1 | 435 | 169 | 520,495 | 266,106 | 4 | 402 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 116 + 7 | **43** | roofs 10 | 1 | 195 | 64 | 97,365 | 59,477 | 1 | 171 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **117** | buildings 75 | 1 | 260 | 106 | 477,200 | 247,660 | 3 | 216 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **131** | roofs 95 | 1 | 171 | 82 | 549,275 | 286,134 | 2 | 142 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **101** | roofs 85 | 1 | 169 | 78 | 339,738 | 174,890 | 1 | 138 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **68** | roofs 53 | 1 | 199 | 75 | 197,100 | 107,721 | 1 | 173 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **22** | lot fences 8 | 0 | 81 | 43 | 57,818 | 40,331 | 1 | 55 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **43** | roofs 29 | 1 | 177 | 65 | 164,653 | 92,207 | 1 | 163 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,925 | 776 | 776 | 353 | 2,676,235 | 1,420,563 | 39 | 1,799 | 15 (6) | 1,662 | 628 | 242 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (81,032 vertices, 51,975 triangles, 260 objects, 117 meshes; hash ea0c9d131ea6f9a8 vs ea0c9d131ea6f9a8; 273 colliders, lights and loot containers, hash e64306018aaf4734 vs e64306018aaf4734). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 48 | 160 / 134 / 1.8 | 1 / 0.2 | 47 / 77,443 / 7.2 | 8.8 | 37.5 | 102 / 4.7 |
| rural-5km | Full | 20 | 277 / 269 / 3.0 | 1 / 0.2 | 46 / 45,152 / 2.4 | 4.3 | 12.1 | 86 / 0.5 |
| louisville-downtown | Full | 105 | 150 / 135 / 2.4 | 1 / 0.2 | 43 / 270,034 / 7.4 | 8.7 | 93.3 | 81 / 0.4 |
| lexington-downtown | Full | 45 | 122 / 85 / 1.5 | 1 / 0.4 | 43 / 82,775 / 2.9 | 4.7 | 38.8 | 81 / 0.5 |
| hazard | Full | 11 | 49 / 24 / 0.5 | 1 / 0.3 | 28 / 31,482 / 1.7 | 3.0 | 6.8 | 28 / 0.3 |
| louisville 0,0 | Full | 111 | 150 / 135 / 2.9 | 1 / 0.2 | 43 / 270,034 / 8.9 | 10.5 | 97.1 | 81 / 0.4 |
| louisville 1,0 | Full | 142 | 275 / 253 / 4.5 | 1 / 0.2 | 43 / 514,218 / 14.6 | 15.8 | 121.1 | 152 / 0.8 |
| louisville -1,0 | Full | 43 | 146 / 124 / 1.5 | 1 / 0.2 | 40 / 96,224 / 10.8 | 12.3 | 29.3 | 56 / 0.7 |
| louisville 0,1 | Full | 117 | 146 / 144 / 1.9 | 1 / 0.2 | 37 / 473,507 / 21.7 | 23.2 | 91.4 | 97 / 3.1 |
| louisville 0,-1 | Full | 131 | 100 / 80 / 1.5 | 1 / 0.2 | 41 / 546,958 / 15.6 | 16.9 | 112.5 | 73 / 0.4 |
| louisville 1,1 | Full | 101 | 94 / 84 / 1.3 | 1 / 0.2 | 35 / 337,565 / 10.8 | 12.1 | 87.2 | 69 / 0.4 |
| louisville -1,1 | Full | 68 | 137 / 117 / 1.7 | 1 / 0.3 | 40 / 195,311 / 5.4 | 6.6 | 59.7 | 65 / 0.4 |
| louisville 1,-1 | Full | 22 | 44 / 31 / 0.5 | 1 / 0.2 | 33 / 57,269 / 2.3 | 3.2 | 17.7 | 40 / 0.2 |
| louisville -1,-1 | Full | 43 | 135 / 105 / 1.5 | 1 / 0.2 | 38 / 163,248 / 5.4 | 6.4 | 35.1 | 58 / 0.3 |

The Louisville block's NavMesh: 691 sources collected in 4 ms; baked at once (as one 384 m square) in 153 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, back to back (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre (identical) | 33 | 26 | 79% | roofs 10.4 | buildings 8.7 | 81,032 / 81,032 | 260 / 260 | 117 / 117 | 231 / 231 | 2 / 2 |
| rural-5km (identical) | 26 | 17 | 68% | buildings 13.8 | buildings 6.4 | 48,109 / 48,109 | 387 / 387 | 103 / 103 | 335 / 335 | 5 / 5 |
| louisville-downtown (identical) | 107 | 79 | 74% | buildings 50.3 | buildings 51.5 | 272,591 / 272,591 | 236 / 236 | 94 / 94 | 202 / 202 | 1 / 1 |
| lexington-downtown (identical) | 24 | 24 | 103% | buildings 13.0 | buildings 13.7 | 85,156 / 85,156 | 184 / 184 | 92 / 92 | 172 / 172 | 1 / 1 |
| hazard (identical) | 8 | 18 | 220% | outdoor 2.1 | lot fences 10.4 | 31,559 / 31,559 | 60 / 60 | 29 / 29 | 51 / 51 | 1 / 1 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| colliders | today: an object per turned box | 2,000 | 65.97 | 32.99 | make 57.70 + switch on 8.28 ms; 1602 objects; destroy 4.32 ms; 3,000 rays 2.69 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle | 2,000 | 14.70 | 7.35 | make 10.10 + switch on 4.61 ms; 10 objects; destroy 1.34 ms; 3,000 rays 1.72 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.43 | 5.71 | make 3.86 + switch on 7.56 ms; 2 objects; destroy 0.09 ms; 3,000 rays 2.05 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.14 | 1.07 | make 2.11 + switch on 0.03 ms (+ 8.91 ms baking on the worker); 2 objects; destroy 0.06 ms; 3,000 rays 1.95 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 41.67 | 1041.77 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 20.30 | 507.49 | objects 1.49, allocate 0.51, pack 17.12, apply 1.17 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.48 | 36.88 | objects 1.28, allocate 0.02, pack 13.59 (worker), apply 0.17 ms |
| primitives | Box, turned, flat colour | 20,000 | 59.94 | 3.00 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 97.43 | 4.87 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 55.74 | 2.79 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 121.01 | 6.05 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 8.47 | 0.42 | 80,000 vertices |
| primitives | Tri | 20,000 | 6.59 | 0.33 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 32.67 | 6.53 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 57.04 | 19.01 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 32.24 | 16.12 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 27.51 | 27.51 |  |
| objects | BoxCollider added to one object | 2,000 | 57.17 | 28.59 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 26.99 | 134.97 |  |
| objects | RoomLight.Spawn | 100 | 17.55 | 175.54 |  |
| objects | switching all of the above on | 1 | 39.15 | 39145.60 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 15.11 | 15109.90 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.91 | 14.95 | worst 0.15 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 10.63 | 1062.72 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 928.59 | 928594.50 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 598.64 | 598640.90 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 26.32 | 26320.90 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,164 ms, 1,610,421 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (0) | 0.06 | 0.04 | 0.07 | 0.74 | 4.82 | 0 / 0 / 0 / 0 |  0.0 | 0 (0 / 0) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 / 1,200 / 1,610,421 | 2 | 232 / 766 |
| drive-east | 2400 (101) | 0.39 | 0.04 | 0.31 | 10.32 | 23.40 | 95 / 37 / 8 / 0 | roofs 23.2 | 40 (40 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 2,040 / 2,444,076 | 2 | 139 / 820 |
| teleport-louisville | 720 (100) | 3.17 | 0.04 | 13.92 | 85.44 | 184.70 | 101 / 62 / 30 / 13 | buildings 181.8 | 25 (25 / 0) | 0 | 30 | 0.2 | 0.7 | 10 / 40 | 50 / 2,147 / 7,079,769 | 4 | 179 / 1,000 |
| drive-louisville | 1800 (64) | 0.85 | 0.04 | 0.28 | 19.13 | 127.20 | 62 / 41 / 22 / 11 | roofs 122.9 | 15 (15 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 2,319 / 7,402,700 | 6 | 182 / 1,017 |

- **settle**: standing at the spawn while the country around the start is built
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: roofs (13x, worst 23.2 ms), buildings (15x, worst 17.0 ms), trees (1x, worst 15.2 ms), outdoor (5x, worst 14.6 ms), lot fences (1x, worst 7.6 ms), snow drifts (1x, worst 6.9 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: buildings (15x, worst 181.8 ms), roofs (18x, worst 165.1 ms), lot fences (3x, worst 31.1 ms), overhead (8x, worst 21.2 ms), streets (1x, worst 17.6 ms), parked cars (2x, worst 14.1 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: roofs (11x, worst 122.9 ms), buildings (6x, worst 92.9 ms), lot fences (4x, worst 70.0 ms), outdoor (6x, worst 29.4 ms), (activate) (1x, worst 19.1 ms), park litter (1x, worst 18.2 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 2 edit-mode artefacts

- `(edit mode) Error: Raid_sign_WICKLAND CHURCH130E0939362F: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_WICKLAND CHURCH130E0939362F: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.616 | 0% | 1,055 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.069 | 0% | 185 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.589 | 0% | 100 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.097 | 0% | 60 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.572 | 0% | 59 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.084 | 0% | 70 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.593 | 0% | 50 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.076 | 0% | 59 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.578 | 0% | 74 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.085 | 0% | 52 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.524 | 0% | 62 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.080 | 0% | 54 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.283 | 0% | 55 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.052 | 1% | 50 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.557 | 0% | 54 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.078 | 0% | 63 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.603 | 0% | 52 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.082 | 0% | 57 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.479 | 0% | 60 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.144 | 35% | 57 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.489 | 0% | 49 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.145 | 37% | 74 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.596 | 0% | 211 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.560 | 0% | 53 |
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

### Everything logged during the run: 0 errors, 0 warnings, 2 edit-mode artefacts

- `(edit mode) Error: Raid_sign_WICKLAND CHURCH130E0939362F: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_WICKLAND CHURCH130E0939362F: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 80 |  |
| square: KentuckyMap.Build | yes | 1,271 |  |
| square: count | yes | 12 |  |
| square: pick spots | yes | 95 |  |
| square: pictures | yes | 4,183 |  |
| square: discard | yes | 14 |  |
| square: passes (warm rebuild) | yes | 603 |  |
| chunks: stress test | yes | 948 |  |
| chunks: Louisville 3x3 | yes | 1,773 |  |
| chunks: determinism | yes | 111 |  |
| perf: Lite against Full | yes | 546 |  |
| perf: determinism across build orders | yes | 557 |  |
| perf: what things cost | yes | 2,663 |  |
| perf: walking and driving through the state | yes | 132,821 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
