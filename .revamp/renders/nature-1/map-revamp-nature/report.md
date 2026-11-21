# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 9.5 s, started 2026-10-24 00:27:15.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\AppData\Local\Temp\clonez-revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 1,384 ms |
| the same square again, warm, pass by pass | 696 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | -6.2, 12.0, facing -117° |
| exits | North 3rd Street (north) at 59.9, 197.0; East Stephen Foster Avenue (east) at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; West Stephen Foster Avenue (west) at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 92 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 104 buildings, 121 beyond the edge, 152 street pieces, 433 containers, 10 lights, 311 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 3,311 | 1,054 | 1,054 | 280 | 748,198 | 458,959 | 40 | 2,086 | 33 (23) | 3,277 | 911 | 433 |
| RaidOverhead | 221 | 220 | 220 | 220 | 76,428 | 40,404 | 18 | 582 | 0 (0) | 0 | 220 | 0 |
| RaidScenery | 77 | 76 | 76 | 76 | 42,785 | 23,633 | 18 | 347 | 0 (0) | 0 | 44 | 0 |
| **all** | 3,609 | 1,350 | 1,350 | 576 | 867,411 | 522,996 | 41 | 3,015 | 33 (23) | 3,277 | 1,175 | 433 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 64 | 9% | 1 | 64 |
| (set up the site) | 4 | 1% | 1 | 4 |
| ground | 0 | 0% | 1 | 0 |
| water and lots | 0 | 0% | 1 | 0 |
| waterways | 0 | 0% | 1 | 0 |
| streets | 16 | 2% | 1 | 16 |
| sidewalks | 9 | 1% | 1 | 9 |
| road markings | 1 | 0% | 1 | 1 |
| railways | 0 | 0% | 1 | 0 |
| buildings | 373 | 54% | 41 | 66 |
| roofs | 11 | 2% | 1 | 11 |
| roof snow and fence posts | 10 | 1% | 1 | 10 |
| utility poles | 17 | 2% | 1 | 17 |
| street lights | 4 | 1% | 1 | 4 |
| signposts | 3 | 0% | 1 | 3 |
| hydrants | 1 | 0% | 1 | 1 |
| lot fences | 1 | 0% | 1 | 1 |
| snow piles | 0 | 0% | 1 | 0 |
| parked cars | 0 | 0% | 1 | 0 |
| nature plan | 1 | 0% | 1 | 1 |
| driveways | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yards | 16 | 2% | 9 | 3 |
| street trees | 8 | 1% | 1 | 8 |
| fence rows | 0 | 0% | 1 | 0 |
| trees | 19 | 3% | 8 | 5 |
| driveway cars | 4 | 1% | 1 | 4 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| kerbside cars | 41 | 6% | 1 | 41 |
| parking lot cars | 5 | 1% | 1 | 5 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 31 | 5% | 1 | 31 |
| fence drifts | 0 | 0% | 1 | 0 |
| yard snow | 1 | 0% | 1 | 1 |
| points | 1 | 0% | 1 | 1 |
| park litter | 0 | 0% | 1 | 0 |
| (emit the overhead meshes) | 12 | 2% | 1 | 12 |
| (emit the outdoor meshes) | 40 | 6% | 1 | 40 |
| **total** | **696** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **36** | buildings 16 | 1 | 245 | 117 | 75,774 | 41,688 | 2 | 214 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **46** | outdoor 8 | 1 | 392 | 104 | 72,257 | 47,764 | 5 | 373 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 139 + 9 | **112** | buildings 64 | 1 | 239 | 93 | 273,210 | 141,702 | 1 | 209 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 440 + 30 | **47** | buildings 35 | 1 | 192 | 92 | 80,556 | 43,472 | 1 | 173 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 5 | **19** | utility poles 4 | 1 | 76 | 30 | 57,444 | 39,330 | 1 | 176 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.0 | 1 | 2.0 |
| ground | 0.5 | 1 | 0.5 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.7 | 1 | 1.7 |
| sidewalks | 3.7 | 1 | 3.7 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 16.3 | 1 | 16.3 |
| roofs | 3.4 | 1 | 3.4 |
| roof snow and fence posts | 0.2 | 1 | 0.2 |
| utility poles | 0.5 | 1 | 0.5 |
| street lights | 0.7 | 1 | 0.7 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.0 | 1 | 0.0 |
| parked cars | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yards | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.2 | 3 | 0.2 |
| driveway cars | 0.2 | 1 | 0.2 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.5 | 1 | 0.5 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.5 | 1 | 1.5 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 0.8 | 1 | 0.8 |
| outdoor | 3.1 | 1 | 3.1 |

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
| buildings | 12.8 | 2 | 6.5 |
| roofs | 0.5 | 1 | 0.5 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.5 | 1 | 0.5 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.0 | 1 | 0.0 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.0 | 1 | 0.0 |
| parked cars | 0.0 | 1 | 0.0 |
| nature plan | 0.0 | 1 | 0.0 |
| driveways | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yards | 2.8 | 1 | 2.8 |
| street trees | 0.7 | 1 | 0.7 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 5.3 | 3 | 4.6 |
| driveway cars | 6.1 | 1 | 6.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.5 | 1 | 0.5 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 4.0 | 1 | 4.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 1.4 | 1 | 1.4 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| overhead | 1.4 | 1 | 1.4 |
| outdoor | 8.4 | 1 | 8.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.6 | 1 | 0.6 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.5 | 1 | 1.5 |
| sidewalks | 9.8 | 1 | 9.8 |
| road markings | 0.3 | 1 | 0.3 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 63.7 | 1 | 63.7 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.3 | 1 | 0.3 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 3.6 | 1 | 3.6 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 4.0 | 1 | 4.0 |
| snow piles | 0.0 | 1 | 0.0 |
| parked cars | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 1.2 | 1 | 1.2 |
| yards | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.2 | 3 | 0.2 |
| driveway cars | 0.7 | 1 | 0.7 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 1.1 | 1 | 1.1 |
| parking lot cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.0 | 1 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| park litter | 9.8 | 1 | 9.8 |
| overhead | 7.3 | 1 | 7.3 |
| outdoor | 4.2 | 1 | 4.2 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.0 | 1 | 0.0 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 2.0 | 1 | 2.0 |
| road markings | 0.2 | 1 | 0.2 |
| railways | 0.0 | 1 | 0.0 |
| buildings | 35.5 | 1 | 35.5 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.1 | 1 | 0.1 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.3 | 1 | 0.3 |
| signposts | 0.0 | 1 | 0.0 |
| hydrants | 0.1 | 1 | 0.1 |
| lot fences | 0.0 | 1 | 0.0 |
| snow piles | 0.0 | 1 | 0.0 |
| parked cars | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yards | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.2 | 3 | 0.1 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 1.2 | 1 | 1.2 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.2 | 1 | 1.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| points | 0.1 | 1 | 0.1 |
| park litter | 0.2 | 1 | 0.2 |
| overhead | 0.9 | 1 | 0.9 |
| outdoor | 3.1 | 1 | 3.1 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water and lots | 0.1 | 1 | 0.1 |
| waterways | 0.0 | 1 | 0.0 |
| streets | 0.5 | 1 | 0.5 |
| sidewalks | 0.8 | 1 | 0.8 |
| road markings | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| roofs | 0.0 | 1 | 0.0 |
| roof snow and fence posts | 0.0 | 1 | 0.0 |
| utility poles | 3.8 | 1 | 3.8 |
| street lights | 0.5 | 1 | 0.5 |
| signposts | 0.1 | 1 | 0.1 |
| hydrants | 0.2 | 1 | 0.2 |
| lot fences | 1.3 | 1 | 1.3 |
| snow piles | 0.0 | 1 | 0.0 |
| parked cars | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yards | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.7 | 1 | 0.7 |
| trees | 5.5 | 3 | 3.6 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| parking lot cars | 2.1 | 1 | 2.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.1 | 1 | 0.1 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| overhead | 0.0 | 1 | 0.0 |
| outdoor | 2.8 | 1 | 2.8 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 63 | 8 |
| 186_141 | yes | 12 | 2 |
| 185_141 | yes | 11 | 6 |
| 185_142 | yes | 11 | 4 |
| 183_148 | yes | 139 | 9 |
| 196_145 | yes | 440 | 30 |
| 210_134 | yes | 5 | 5 |
| 182_148 | yes | 133 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 869 ms (slowest 144 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **107** | buildings 73 | 1 | 239 | 93 | 273,210 | 141,702 | 1 | 209 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **144** | buildings 99 | 2 | 443 | 169 | 526,818 | 270,401 | 4 | 412 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 133 + 8 | **81** | parking lot cars 31 | 1 | 243 | 64 | 108,801 | 61,015 | 1 | 228 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **129** | buildings 81 | 1 | 307 | 106 | 491,352 | 253,599 | 3 | 264 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **141** | roofs 105 | 1 | 193 | 82 | 552,228 | 281,484 | 2 | 168 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **115** | roofs 94 | 1 | 181 | 78 | 356,031 | 185,277 | 1 | 168 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **77** | roofs 58 | 1 | 230 | 76 | 204,260 | 106,321 | 1 | 205 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **26** | outdoor 10 | 0 | 121 | 43 | 69,215 | 40,933 | 1 | 100 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **49** | roofs 25 | 1 | 190 | 65 | 170,368 | 90,908 | 1 | 188 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 2,149 | 776 | 776 | 353 | 2,752,283 | 1,431,640 | 40 | 1,780 | 15 (6) | 1,942 | 628 | 242 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (75,774 vertices, 41,688 triangles, 245 objects, 117 meshes; hash 62e012d1a115f65d vs 62e012d1a115f65d).

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.614 | 0% | 1,064 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.069 | 0% | 168 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.582 | 0% | 98 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Court Square, at the game's starting zoom | 0.100 | 0% | 68 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.572 | 0% | 64 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.084 | 0% | 78 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.567 | 0% | 52 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.075 | 0% | 57 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.576 | 0% | 58 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.085 | 0% | 50 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.577 | 0% | 47 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.080 | 0% | 56 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.329 | 0% | 54 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.058 | 1% | 55 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.554 | 0% | 54 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.078 | 0% | 55 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.597 | 0% | 52 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.082 | 0% | 61 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.482 | 0% | 110 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Court Square (downtown), looking along the street | 0.156 | 36% | 56 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.459 | 0% | 50 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.156 | 28% | 74 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.596 | 0% | 221 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.560 | 0% | 55 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.078 | 0% | 53 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -6.2, 12.0 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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
| setup | yes | 61 |  |
| square: KentuckyMap.Build | yes | 1,386 |  |
| square: count | yes | 14 |  |
| square: pick spots | yes | 96 |  |
| square: pictures | yes | 4,391 |  |
| square: discard | yes | 24 |  |
| square: passes (warm rebuild) | yes | 712 |  |
| chunks: stress test | yes | 1,019 |  |
| chunks: Louisville 3x3 | yes | 1,750 |  |
| chunks: determinism | yes | 76 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
