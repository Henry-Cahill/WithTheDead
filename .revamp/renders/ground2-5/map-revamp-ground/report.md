# CLONE-Z map harness: Mentor (build ky-local-f2)

**Exit 0**: ok. Full run (streamed chunks in Lite detail), 175.6 s, started 2026-10-24 10:42:55.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Mentor plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 3,967 ms |
| the same square again, warm, pass by pass | 2,143 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | -33.4, 18.7, facing 149° |
| exits | KY 8 north at -143.6, 197.0; Main Street (east) at 197.0, 163.8; KY 8 south at 69.6, -197.0; Snowfield (west) at -197.0, 0.0 |
| overhead renderers (hidden in the iso view) | 86 |
| `KentuckyMap.LastStats` | Kentucky map Mentor (build ky-local-f2): 52 buildings, 21 beyond the edge, 59 street pieces, 273 containers, 434 lights, 2953 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 52 with their middle inside the square (2 of them reach within 1 m of its edge or past it), 21 in the scenery ring; inside: house 41, garage 4, shed 4, barn 1, church 1, commercial 1 |
| map data: streets, lines, areas | 61 street pieces, 3 lines (rail 1, river 1, stream 1), 37 land-use areas (meadow 11, farmyard 7, forest 6, residential 5, farmland 4, industrial 2, churchyard 1, water 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 10,573 | 6,119 | 6,119 | 5,687 | 3,116,199 | 1,984,208 | 115 | 14,102 | 448 (0) | 4,314 | 4,170 | 273 |
| RaidOverhead | 252 | 199 | 199 | 199 | 116,700 | 74,775 | 45 | 606 | 0 (0) | 0 | 199 | 0 |
| RaidScenery | 105 | 104 | 104 | 104 | 133,731 | 149,138 | 37 | 216 | 0 (0) | 0 | 57 | 0 |
| **all** | 10,930 | 6,422 | 6,422 | 5,990 | 3,366,630 | 2,208,121 | 146 | 14,924 | 448 (0) | 4,314 | 4,426 | 273 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 9,759 | 5,824 | 5,824 | 5,394 | 2,277,806 | 1,474,386 | 66 | 12,778 | 431 (0) | 1,987 | 3,988 | 257 |
| World/Building shells | 54 | 53 | 53 | 53 | 380,368 | 190,124 | 39 | 533 | 0 (0) | 0 | 53 | 0 |
| World/OutdoorGround | 109 | 109 | 109 | 109 | 68,718 | 30,224 | 17 | 412 | 0 (0) | 0 | 0 | 0 |
| World/OutdoorProps | 129 | 129 | 129 | 129 | 389,085 | 289,162 | 17 | 375 | 0 (0) | 0 | 129 | 0 |
| RaidOverhead/Building shells | 54 | 53 | 53 | 53 | 78,988 | 36,689 | 36 | 293 | 0 (0) | 0 | 53 | 0 |
| RaidOverhead/Interior KY# | 102 | 51 | 51 | 51 | 21,992 | 24,488 | 5 | 145 | 0 (0) | 0 | 51 | 0 |
| RaidOverhead/Roofs | 45 | 45 | 45 | 45 | 11,631 | 7,227 | 24 | 118 | 0 (0) | 0 | 45 | 0 |
| RaidScenery/SceneryGround | 45 | 45 | 45 | 45 | 4,398 | 1,576 | 13 | 114 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SceneryProps | 45 | 45 | 45 | 45 | 88,264 | 67,050 | 23 | 88 | 0 (0) | 0 | 45 | 0 |
| RaidOverhead/Overhead | 35 | 35 | 35 | 35 | 663 | 221 | 1 | 35 | 0 (0) | 0 | 35 | 0 |
| RaidOverhead/Wires | 15 | 15 | 15 | 15 | 3,426 | 6,150 | 1 | 15 | 0 (0) | 0 | 15 | 0 |
| RaidScenery/Wires | 12 | 12 | 12 | 12 | 1,068 | 1,920 | 1 | 12 | 0 (0) | 0 | 12 | 0 |
| World/StreetLamp | 6 | 3 | 3 | 1 | 198 | 300 | 1 | 3 | 3 (0) | 3 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 39,997 | 78,590 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 9,639 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 119 | 6% | 1 | 119 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 3 | 0% | 1 | 3 |
| water | 0 | 0% | 1 | 0 |
| fields | 2 | 0% | 1 | 2 |
| lots | 0 | 0% | 1 | 0 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 2 | 0% | 1 | 2 |
| snow tone | 24 | 1% | 1 | 24 |
| road network | 3 | 0% | 1 | 3 |
| junctions | 3 | 0% | 1 | 3 |
| streets | 13 | 1% | 47 | 8 |
| sidewalks | 8 | 0% | 47 | 4 |
| berms and driveways | 9 | 0% | 46 | 1 |
| road markings | 1 | 0% | 2 | 1 |
| paths | 0 | 0% | 1 | 0 |
| railways | 5 | 0% | 1 | 5 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 32 | 1% | 14 | 7 |
| interior plans | 79 | 4% | 53 | 7 |
| interior shell | 10 | 0% | 51 | 4 |
| interior walls | 72 | 3% | 51 | 11 |
| interior floors | 9 | 0% | 51 | 2 |
| interior doors | 239 | 11% | 92 | 18 |
| interior switches | 45 | 2% | 51 | 5 |
| interior rooms | 1,072 | 50% | 267 | 28 |
| interior wall meshes | 49 | 2% | 51 | 4 |
| interior finish | 118 | 6% | 51 | 10 |
| cutaway | 20 | 1% | 10 | 2 |
| utility lines | 2 | 0% | 1 | 2 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 1 | 0% | 1 | 1 |
| street lights | 1 | 0% | 1 | 1 |
| signs | 7 | 0% | 1 | 7 |
| street furniture | 8 | 0% | 1 | 8 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 6 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 19 | 1% | 1 | 19 |
| nature plan | 9 | 0% | 1 | 9 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 7 | 0% | 55 | 0 |
| grounds trees | 0 | 0% | 2 | 0 |
| street trees | 3 | 0% | 82 | 0 |
| creek trees | 0 | 0% | 1 | 0 |
| fence rows | 2 | 0% | 234 | 0 |
| woods | 66 | 3% | 9370 | 5 |
| driveway cars | 2 | 0% | 25 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 0 | 0% | 1 | 0 |
| kerbside cars | 1 | 0% | 13 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 7 | 0% | 53 | 0 |
| fence drifts | 0 | 0% | 6 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 2 | 0% | 1 | 2 |
| points | 0 | 0% | 1 | 0 |
| (after the last pass) | 6 | 0% | 1 | 6 |
| (emit the overhead meshes) | 3 | 0% | 1 | 3 |
| (emit the outdoor meshes) | 51 | 2% | 1 | 51 |
| **total** | **2,143** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Mentor | 12738_10035 (199_156) | 0 + 0 | **27** | data 13 | 0 | 44 | 39 | 8,799 | 5,619 | 0 | 1 | 13 | 16/13/13 | 0E 0W |
| rural-5km | 5 km from Mentor (bearing 200°) | 12724_9998 (198_156) | 0 + 0 | **7** | buildings 2 | 0 | 34 | 29 | 6,196 | 6,166 | 0 | 1 | 4 | 3/4/7 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 143 + 8 | **24** | buildings 6 | 0 | 36 | 31 | 8,579 | 5,221 | 0 | 1 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 428 + 32 | **20** | buildings 6 | 0 | 33 | 28 | 6,046 | 3,658 | 0 | 1 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 2 | **5** | streets 2 | 0 | 30 | 25 | 3,062 | 2,890 | 0 | 1 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 12.7 | 1 | 12.7 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| snow tone | 0.4 | 1 | 0.4 |
| road network | 0.7 | 1 | 0.7 |
| junctions | 0.7 | 1 | 0.7 |
| streets | 1.5 | 1 | 1.5 |
| sidewalks | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.3 | 1 | 0.3 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 2.5 | 2 | 2.1 |
| landmarks | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 1.0 | 1 | 1.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.2 | 1 | 0.2 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| objects | 2.9 | 1 | 2.9 |
| meshes | 1.3 | 1 | 1.3 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| snow tone | 0.6 | 1 | 0.6 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.1 | 1 | 0.1 |
| sidewalks | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 2.5 | 2 | 2.3 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.1 | 4 | 0.4 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 1.4 | 1 | 1.4 |
| water | 0.1 | 1 | 0.1 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.5 | 1 | 0.5 |
| snow tone | 0.4 | 1 | 0.4 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 0.4 | 1 | 0.4 |
| sidewalks | 0.7 | 1 | 0.7 |
| paths | 2.4 | 1 | 2.4 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 12.5 | 4 | 5.8 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| mapped trees | 0.4 | 1 | 0.4 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.0 |
| danger and landmarks | 2.2 | 1 | 2.2 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| snow tone | 0.4 | 1 | 0.4 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.6 | 1 | 0.6 |
| streets | 5.4 | 1 | 5.4 |
| sidewalks | 1.1 | 1 | 1.1 |
| paths | 0.4 | 1 | 0.4 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 7.0 | 2 | 5.8 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.2 | 1 | 0.2 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.1 |
| danger and landmarks | 2.1 | 1 | 2.1 |
| objects | 0.6 | 1 | 0.6 |
| meshes | 0.1 | 1 | 0.1 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.1 | 1 | 0.1 |
| snow tone | 0.4 | 1 | 0.4 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 2.1 | 1 | 2.1 |
| sidewalks | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.2 | 1 | 0.2 |
| woods | 0.4 | 4 | 0.1 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| objects | 0.6 | 1 | 0.6 |
| meshes | 0.1 | 1 | 0.1 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 199_157 | yes | 14 | 5 |
| 199_156 | yes | 8 | 3 |
| 198_156 | yes | 122 | 20 |
| 198_157 | yes | 37 | 6 |
| 183_148 | yes | 143 | 8 |
| 196_145 | yes | 428 | 32 |
| 210_134 | yes | 5 | 2 |
| 182_148 | yes | 124 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 110 ms (slowest 20 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **10** | paths 2 | 0 | 36 | 31 | 8,579 | 5,221 | 0 | 1 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **15** | buildings 6 | 0 | 35 | 30 | 7,516 | 5,298 | 0 | 1 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 124 + 8 | **18** | buildings 6 | 0 | 33 | 28 | 10,564 | 5,804 | 0 | 1 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **7** | buildings 1 | 0 | 29 | 24 | 7,479 | 4,008 | 0 | 1 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **20** | buildings 8 | 0 | 34 | 29 | 7,421 | 4,407 | 0 | 1 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **4** | buildings 1 | 0 | 29 | 24 | 5,482 | 4,080 | 0 | 1 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **19** | buildings 6 | 0 | 36 | 31 | 15,010 | 7,884 | 0 | 1 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **9** | streets 3 | 0 | 30 | 25 | 7,232 | 4,349 | 0 | 1 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **9** | buildings 2 | 0 | 34 | 29 | 9,954 | 6,201 | 0 | 1 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 298 | 251 | 251 | 251 | 79,237 | 47,252 | 51 | 695 | 0 (0) | 9 | 115 | 0 |

### Determinism

Chunk 12738_10035 (start-centre) built twice: **match**: identical, in the same order (8,799 vertices, 5,619 triangles, 44 objects, 39 meshes; hash 827757b53568ce29 vs 827757b53568ce29; 1 colliders, lights and loot containers, hash 6558d0193acd88e7 vs 6558d0193acd88e7). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Lite | 27 | 0 / 0 / 0.0 | 0 / 0.0 | 38 / 8,123 / 0.8 | 1.2 | 25.5 | 28 / 3.2 |
| rural-5km | Lite | 7 | 0 / 0 / 0.0 | 0 / 0.0 | 28 / 5,520 / 0.1 | 0.7 | 6.3 | 26 / 0.2 |
| louisville-downtown | Lite | 24 | 0 / 0 / 0.0 | 0 / 0.0 | 30 / 7,903 / 0.1 | 0.7 | 23.1 | 24 / 0.2 |
| lexington-downtown | Lite | 20 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 5,370 / 0.1 | 0.6 | 19.1 | 22 / 0.2 |
| hazard | Lite | 5 | 0 / 0 / 0.0 | 0 / 0.0 | 24 / 2,450 / 0.1 | 0.5 | 4.7 | 24 / 0.2 |
| louisville 0,0 | Lite | 10 | 0 / 0 / 0.0 | 0 / 0.0 | 30 / 7,903 / 0.1 | 0.8 | 8.9 | 24 / 0.2 |
| louisville 1,0 | Lite | 15 | 0 / 0 / 0.0 | 0 / 0.0 | 29 / 6,840 / 0.1 | 1.1 | 13.7 | 21 / 0.1 |
| louisville -1,0 | Lite | 18 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 9,978 / 0.1 | 0.6 | 17.8 | 23 / 0.1 |
| louisville 0,1 | Lite | 7 | 0 / 0 / 0.0 | 0 / 0.0 | 23 / 6,858 / 0.1 | 0.5 | 6.1 | 19 / 0.1 |
| louisville 0,-1 | Lite | 20 | 0 / 0 / 0.0 | 0 / 0.0 | 28 / 6,755 / 0.1 | 0.6 | 19.9 | 24 / 0.2 |
| louisville 1,1 | Lite | 4 | 0 / 0 / 0.0 | 0 / 0.0 | 23 / 4,806 / 0.1 | 0.6 | 3.5 | 19 / 0.1 |
| louisville -1,1 | Lite | 19 | 0 / 0 / 0.0 | 0 / 0.0 | 30 / 14,400 / 0.2 | 0.9 | 18.4 | 25 / 0.3 |
| louisville 1,-1 | Lite | 9 | 0 / 0 / 0.0 | 0 / 0.0 | 24 / 6,587 / 0.1 | 0.6 | 8.0 | 23 / 0.2 |
| louisville -1,-1 | Lite | 9 | 0 / 0 / 0.0 | 0 / 0.0 | 28 / 9,278 / 0.1 | 0.6 | 7.9 | 25 / 0.2 |

The Louisville block's NavMesh: 203 sources collected in 2 ms; baked at once (as one 384 m square) in 124 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 46 | 8 | 18% | interior plans 3.3 | streets 1.6 | 152,025 / 8,799 | 181 / 44 | 71 / 39 | 220 / 1 | 4 / 0 |
| rural-5km | 16 | 4 | 24% | interior plans 2.5 | objects 0.6 | 64,600 / 6,214 | 84 / 36 | 39 / 31 | 263 / 1 | 0 / 0 |
| louisville-downtown | 47 | 10 | 21% | interior plans 6.0 | buildings 2.4 | 128,706 / 8,579 | 211 / 36 | 49 / 31 | 164 / 1 | 0 / 0 |
| lexington-downtown | 46 | 12 | 26% | streets 6.7 | streets 5.4 | 120,635 / 6,044 | 138 / 32 | 57 / 27 | 94 / 1 | 0 / 0 |
| hazard | 23 | 6 | 28% | streets 3.3 | streets 2.1 | 63,415 / 4,578 | 87 / 32 | 45 / 27 | 150 / 1 | 1 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 63.26 | 31.63 | make 54.93 + switch on 8.32 ms; 1602 objects; destroy 3.91 ms; 3,000 rays 3.12 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 15.91 | 7.96 | make 10.90 + switch on 5.01 ms; 10 objects; destroy 1.45 ms; 3,000 rays 1.99 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 12.74 | 6.37 | make 4.24 + switch on 8.50 ms; 2 objects; destroy 0.10 ms; 3,000 rays 1.91 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.15 | 1.08 | make 2.11 + switch on 0.04 ms (+ 8.73 ms baking on the worker); 2 objects; destroy 0.11 ms; 3,000 rays 2.38 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.37 | 209.28 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.81 | 195.27 | objects 0.80, allocate 0.02, pack 6.88, apply 0.12 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.17 | 29.16 | objects 1.01, allocate 0.02, pack 7.11 (worker), apply 0.14 ms |
| primitives | Box, turned, flat colour | 20,000 | 50.43 | 2.52 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 60.79 | 3.04 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 36.58 | 1.83 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 99.36 | 4.97 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 20.51 | 1.03 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 55.36 | 2.77 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.44 | 0.32 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.78 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 10.54 | 2.11 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 27.71 | 5.54 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 27.08 | 9.03 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 45.23 | 15.08 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 14.54 | 7.27 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 12.53 | 12.53 |  |
| objects | BoxCollider added to one object | 2,000 | 40.32 | 20.16 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 26.20 | 131.00 |  |
| objects | RoomLight.Spawn | 100 | 12.07 | 120.71 |  |
| objects | switching all of the above on | 1 | 26.99 | 26990.60 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 10.04 | 10039.10 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 66 | 0.86 | 13.02 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 6.00 | 599.77 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 416.36 | 416361.00 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 373.61 | 373608.80 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 20.87 | 20871.50 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 432 ms, 1,381,756 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (13) | 0.16 | 0.04 | 0.07 | 4.80 | 14.26 | 1 / 1 / 0 / 0 | ground 3.3 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 972 / 1,434,457 | 2 | 175 / 797 (pool 34) |
| drive-east | 2400 (70) | 0.14 | 0.05 | 0.08 | 4.09 | 6.56 | 7 / 0 / 0 / 0 | interior plans 3.0 | 40 (24 / 16) | 0 | 19 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 1,147 / 1,557,341 | 2 | 175 / 804 (pool 34) |
| teleport-louisville | 720 (134) | 1.14 | 0.05 | 6.47 | 9.35 | 57.23 | 61 / 16 / 4 / 4 | buildings 47.1 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.6 | 10 / 98 | 41 (16 Lite) / 1,601 / 2,609,546 | 4 | 198 / 848 (pool 36) |
| drive-louisville | 1800 (178) | 0.44 | 0.05 | 3.29 | 6.44 | 34.39 | 37 / 12 / 3 / 1 | interior plans 34.3 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 25 | 46 (12 Lite) / 1,992 / 3,403,758 | 6 | 245 / 885 (pool 36) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: woods (1x, worst 3.2 ms)
- **drive-east**: driving east from the spawn at 90 km/h
- **teleport-louisville**: set down in downtown Louisville (149 km away), standing; the frames over 8 ms were made by: buildings (1x, worst 47.1 ms), interior plans (7x, worst 32.6 ms), lots (1x, worst 5.6 ms), streets (2x, worst 4.6 ms), nature plan (1x, worst 3.8 ms), utility poles (1x, worst 3.3 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (4x, worst 34.3 ms), parking lot cars (1x, worst 8.6 ms), buildings (1x, worst 7.2 ms), streets (2x, worst 5.8 ms), wall drifts (1x, worst 5.5 ms), (swap) (1x, worst 4.8 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 28 edit-mode artefacts

- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GENERAL MDSE1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GENERAL MDSE1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_VARIETY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_VARIETY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HUMANA BUILDING37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HUMANA BUILDING37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_CAFE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_CAFE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.559 | 0% | 1,177 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.068 | 0% | 226 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Mary Ingles Highway, at the game's starting zoom | 0.395 | 0% | 124 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Mary Ingles Highway, at the game's starting zoom | 0.086 | 0% | 81 |
| `iso_downtown_day.png` | Downtown Mentor: the middle of the square, on Main Street | 0.443 | 0% | 67 |
| `iso_downtown_night.png` | Downtown Mentor: the middle of the square, on Main Street | 0.084 | 0% | 65 |
| `iso_residential_day.png` | The densest residential street: Main Street, 4 homes within 30 m | 0.527 | 0% | 54 |
| `iso_residential_night.png` | The densest residential street: Main Street, 4 homes within 30 m | 0.078 | 0% | 88 |
| `iso_intersection_day.png` | The busiest intersection: Mary Ingles Highway / Main Street (3 ways in) | 0.413 | 0% | 51 |
| `iso_intersection_night.png` | The busiest intersection: Mary Ingles Highway / Main Street (3 ways in) | 0.076 | 0% | 41 |
| `iso_green_day.png` | Green: a meadow, 352 m² inside the square | 0.528 | 0% | 41 |
| `iso_green_night.png` | Green: a meadow, 352 m² inside the square | 0.073 | 0% | 55 |
| `iso_edge_day.png` | The square's edge: where Mary Ingles Highway leaves it (south), the playable map giving way to the scenery ring | 0.420 | 0% | 40 |
| `iso_edge_night.png` | The square's edge: where Mary Ingles Highway leaves it (south), the playable map giving way to the scenery ring | 0.073 | 0% | 49 |
| `iso_waterway_day.png` | Railway: rail 'Cincinnati Subdivision' | 0.537 | 0% | 56 |
| `iso_waterway_night.png` | Railway: rail 'Cincinnati Subdivision' | 0.075 | 0% | 44 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, a commercial (371 m² footprint) | 0.571 | 0% | 52 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, a commercial (371 m² footprint) | 0.074 | 0% | 51 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Mary Ingles Highway (downtown), looking along the street | 0.432 | 0% | 75 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Mary Ingles Highway (downtown), looking along the street | 0.152 | 28% | 61 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of Main Street (the densest residential street), looking along it | 0.441 | 0% | 75 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of Main Street (the densest residential street), looking along it | 0.154 | 19% | 77 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.520 | 0% | 91 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.427 | 0% | 60 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.076 | 0% | 56 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -33.4, 18.7 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -5.5, 8.0 | the place's own point in places.json (its OSM place node, which the square is built around), moved 9.7 m onto the nearest street |
| residential | 41.6, 42.5 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | -16.6, 2.3 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | 183.1, -46.9 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | none | nothing of the kind in the square |
| edge | 70.8, -200.0 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | 100.4, 50.5 | the point of a railway, river, canal or stream nearest the square's middle |
| landmark | -87.6, -117.6 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 28 edit-mode artefacts

- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GENERAL MDSE1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GENERAL MDSE1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_VARIETY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_VARIETY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HUMANA BUILDING37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HUMANA BUILDING37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_CAFE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_CAFE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 64 |  |
| square: KentuckyMap.Build | yes | 3,968 |  |
| square: count | yes | 353 |  |
| square: pick spots | yes | 99 |  |
| square: pictures | yes | 4,822 |  |
| square: discard | yes | 113 |  |
| square: passes (warm rebuild) | yes | 2,260 |  |
| chunks: stress test | yes | 939 |  |
| chunks: Louisville 3x3 | yes | 1,101 |  |
| chunks: determinism | yes | 30 |  |
| perf: Lite against Full | yes | 1,224 |  |
| perf: determinism across build orders | yes | 318 |  |
| perf: what things cost | yes | 1,678 |  |
| perf: walking and driving through the state | yes | 158,662 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
