# CLONE-Z map harness: Lancaster (build ky-local-f2)

**Exit 0**: ok. Full run (streamed chunks in Lite detail), 173.8 s, started 2026-10-24 10:41:27.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Lancaster plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 3,348 ms |
| the same square again, warm, pass by pass | 1,223 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 4.5, -44.9, facing 6° |
| exits | US 27 north at -37.7, 197.0; KY 52 east at 197.0, -48.0; KY 39 south at -81.9, -197.0; KY 52 west at -197.0, -4.1 |
| overhead renderers (hidden in the iso view) | 48 |
| `KentuckyMap.LastStats` | Kentucky map Lancaster (build ky-local-f2): 27 buildings, 26 beyond the edge, 53 street pieces, 256 containers, 243 lights, 471 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 27 with their middle inside the square (1 of them reach within 1 m of its edge or past it), 26 in the scenery ring; inside: house 13, commercial 11, church 2, civic 1 |
| map data: streets, lines, areas | 53 street pieces, 1 lines (abandoned 1), 13 land-use areas (residential 6, school 2, cemetery 1, commercial 1, park 1, parking 1, pitch 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 8,169 | 4,318 | 4,318 | 4,079 | 2,205,035 | 1,321,696 | 127 | 10,018 | 249 (0) | 2,594 | 3,148 | 256 |
| RaidOverhead | 191 | 163 | 163 | 163 | 111,326 | 71,754 | 54 | 501 | 0 (0) | 0 | 163 | 0 |
| RaidScenery | 93 | 92 | 92 | 92 | 60,511 | 94,600 | 29 | 146 | 0 (0) | 0 | 56 | 0 |
| **all** | 8,453 | 4,573 | 4,573 | 4,334 | 2,376,872 | 1,488,050 | 161 | 10,665 | 249 (0) | 2,594 | 3,367 | 256 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 7,312 | 4,008 | 4,008 | 3,778 | 1,625,769 | 1,012,714 | 76 | 8,669 | 231 (0) | 1,593 | 2,984 | 194 |
| World/OutdoorProps | 133 | 133 | 133 | 133 | 222,385 | 136,265 | 25 | 577 | 0 (0) | 0 | 133 | 0 |
| World/OutdoorGround | 133 | 133 | 133 | 133 | 112,372 | 48,591 | 14 | 462 | 0 (0) | 0 | 0 | 0 |
| World/Building shells | 32 | 31 | 31 | 31 | 243,660 | 122,854 | 39 | 297 | 0 (0) | 0 | 31 | 0 |
| RaidOverhead/Building shells | 32 | 31 | 31 | 31 | 67,751 | 33,117 | 39 | 194 | 0 (0) | 0 | 31 | 0 |
| RaidOverhead/Roofs | 59 | 59 | 59 | 59 | 17,189 | 11,111 | 30 | 168 | 0 (0) | 0 | 59 | 0 |
| RaidOverhead/Interior KY# | 52 | 26 | 26 | 26 | 21,229 | 18,804 | 5 | 92 | 0 (0) | 0 | 26 | 0 |
| RaidScenery/SceneryProps | 37 | 37 | 37 | 37 | 17,648 | 12,852 | 23 | 75 | 0 (0) | 0 | 37 | 0 |
| RaidScenery/SceneryGround | 34 | 34 | 34 | 34 | 960 | 332 | 4 | 50 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Wires | 25 | 25 | 25 | 25 | 4,821 | 8,610 | 1 | 25 | 0 (0) | 0 | 25 | 0 |
| RaidOverhead/Overhead | 22 | 22 | 22 | 22 | 336 | 112 | 1 | 22 | 0 (0) | 0 | 22 | 0 |
| RaidScenery/Wires | 19 | 19 | 19 | 19 | 2,175 | 3,798 | 1 | 19 | 0 (0) | 0 | 19 | 0 |
| World/StreetLamp | 24 | 12 | 12 | 3 | 825 | 1,260 | 3 | 12 | 12 (0) | 12 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 39,724 | 77,616 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 7,819 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 16 | 1% | 1 | 16 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 1 | 0% | 1 | 1 |
| lots | 0 | 0% | 1 | 0 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 0 | 0% | 1 | 0 |
| snow tone | 26 | 2% | 1 | 26 |
| road network | 1 | 0% | 1 | 1 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 65 | 5% | 56 | 55 |
| sidewalks | 21 | 2% | 56 | 4 |
| berms and driveways | 12 | 1% | 55 | 1 |
| road markings | 2 | 0% | 7 | 1 |
| paths | 0 | 0% | 1 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 16 | 1% | 8 | 3 |
| interior plans | 50 | 4% | 27 | 7 |
| interior shell | 3 | 0% | 26 | 0 |
| interior walls | 31 | 3% | 26 | 4 |
| interior floors | 3 | 0% | 26 | 0 |
| interior doors | 121 | 10% | 57 | 17 |
| interior switches | 15 | 1% | 26 | 1 |
| interior rooms | 660 | 54% | 176 | 64 |
| interior wall meshes | 19 | 2% | 26 | 2 |
| interior finish | 41 | 3% | 26 | 4 |
| cutaway | 10 | 1% | 5 | 2 |
| utility lines | 3 | 0% | 1 | 3 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 2 | 0% | 1 | 2 |
| street lights | 2 | 0% | 1 | 2 |
| signs | 4 | 0% | 1 | 4 |
| street furniture | 1 | 0% | 1 | 1 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 5 | 0% | 1 | 5 |
| nature plan | 1 | 0% | 1 | 1 |
| driveway plan | 0 | 0% | 1 | 0 |
| mapped trees | 11 | 1% | 73 | 4 |
| yard trees | 2 | 0% | 30 | 0 |
| grounds trees | 1 | 0% | 4 | 0 |
| street trees | 2 | 0% | 101 | 0 |
| fence rows | 5 | 0% | 325 | 0 |
| woods | 3 | 0% | 9360 | 0 |
| driveway cars | 2 | 0% | 10 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 0 | 0% | 1 | 0 |
| kerbside cars | 9 | 1% | 167 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 4 | 0% | 28 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 0 | 0% | 1 | 0 |
| (after the last pass) | 4 | 0% | 1 | 4 |
| (emit the overhead meshes) | 5 | 0% | 1 | 5 |
| (emit the outdoor meshes) | 34 | 3% | 1 | 34 |
| **total** | **1,223** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Lancaster | 12527_8932 (195_139) | 0 + 0 | **16** | streets 5 | 0 | 22 | 17 | 4,633 | 6,048 | 0 | 1 | 1 | 7/1/1 | 0E 0W |
| rural-5km | 5 km from Lancaster (bearing 210°) | 12507_8898 (195_139) | 0 + 0 | **4** | snow tone 1 | 0 | 28 | 23 | 4,413 | 6,530 | 0 | 1 | 0 | 3/0/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 150 + 9 | **27** | buildings 6 | 0 | 36 | 31 | 10,496 | 8,849 | 0 | 1 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 396 + 29 | **18** | buildings 6 | 0 | 34 | 29 | 8,075 | 7,464 | 0 | 1 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **6** | streets 2 | 0 | 32 | 27 | 5,165 | 6,506 | 0 | 1 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.6 | 1 | 1.6 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 4.7 | 1 | 4.7 |
| sidewalks | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.4 | 1 | 0.4 |
| landmarks | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.6 | 1 | 0.6 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.1 | 1 | 0.1 |
| woods | 0.1 | 4 | 0.1 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| objects | 2.2 | 1 | 2.2 |
| meshes | 2.4 | 1 | 2.4 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.0 | 1 | 0.0 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.1 | 1 | 0.1 |
| sidewalks | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.2 | 1 | 0.2 |
| woods | 0.5 | 4 | 0.2 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| objects | 0.5 | 1 | 0.5 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 1.8 | 1 | 1.8 |
| water | 1.5 | 1 | 1.5 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.1 | 1 | 0.1 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 0.9 | 1 | 0.9 |
| junctions | 1.1 | 1 | 1.1 |
| streets | 0.4 | 1 | 0.4 |
| sidewalks | 0.3 | 1 | 0.3 |
| paths | 2.8 | 1 | 2.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 14.7 | 4 | 6.3 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| mapped trees | 0.1 | 1 | 0.1 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.7 | 1 | 0.7 |
| streets | 5.4 | 1 | 5.4 |
| sidewalks | 1.0 | 1 | 1.0 |
| paths | 0.3 | 1 | 0.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.2 | 2 | 5.6 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.1 |
| danger and landmarks | 0.1 | 1 | 0.1 |
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
| lots | 0.3 | 1 | 0.3 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 2.0 | 1 | 2.0 |
| sidewalks | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.4 | 1 | 0.4 |
| woods | 0.3 | 4 | 0.1 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| objects | 0.5 | 1 | 0.5 |
| meshes | 0.2 | 1 | 0.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 195_140 | yes | 2 | 4 |
| 196_140 | yes | 2 | 9 |
| 196_139 | yes | 2 | 1 |
| 195_139 | yes | 5 | 2 |
| 195_138 | yes | 2 | 1 |
| 183_148 | yes | 150 | 9 |
| 196_145 | yes | 396 | 29 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 158 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 125 ms (slowest 43 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **10** | paths 2 | 0 | 36 | 31 | 10,496 | 8,849 | 0 | 1 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **13** | buildings 6 | 0 | 34 | 29 | 9,373 | 8,954 | 0 | 1 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 158 + 8 | **16** | buildings 6 | 0 | 33 | 28 | 12,101 | 8,810 | 0 | 1 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **7** | streets 1 | 0 | 29 | 24 | 9,065 | 6,926 | 0 | 1 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **43** | buildings 11 | 0 | 34 | 29 | 9,279 | 7,973 | 0 | 1 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **4** | snow tone 1 | 0 | 29 | 24 | 7,406 | 7,778 | 0 | 1 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **12** | buildings 3 | 0 | 36 | 31 | 16,725 | 11,150 | 0 | 1 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **9** | streets 3 | 0 | 30 | 25 | 9,017 | 7,769 | 0 | 1 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **10** | buildings 2 | 0 | 33 | 28 | 11,790 | 9,753 | 0 | 1 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 296 | 249 | 249 | 249 | 95,252 | 77,962 | 51 | 693 | 0 (0) | 9 | 113 | 0 |

### Determinism

Chunk 12527_8932 (start-centre) built twice: **match**: identical, in the same order (4,633 vertices, 6,048 triangles, 22 objects, 17 meshes; hash 10e3cc02b4281052 vs 10e3cc02b4281052; 1 colliders, lights and loot containers, hash 676433a3b80b5da3 vs 676433a3b80b5da3). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Lite | 16 | 0 / 0 / 0.0 | 0 / 0.0 | 16 / 2,032 / 0.6 | 0.7 | 15.2 | 15 / 3.4 |
| rural-5km | Lite | 4 | 0 / 0 / 0.0 | 0 / 0.0 | 22 / 1,814 / 0.1 | 0.4 | 3.5 | 22 / 0.1 |
| louisville-downtown | Lite | 27 | 0 / 0 / 0.0 | 0 / 0.0 | 30 / 7,903 / 0.2 | 0.6 | 26.6 | 24 / 0.2 |
| lexington-downtown | Lite | 18 | 0 / 0 / 0.0 | 0 / 0.0 | 28 / 5,478 / 0.1 | 0.6 | 17.0 | 23 / 0.2 |
| hazard | Lite | 6 | 0 / 0 / 0.0 | 0 / 0.0 | 26 / 2,870 / 0.1 | 0.5 | 5.6 | 26 / 0.2 |
| louisville 0,0 | Lite | 10 | 0 / 0 / 0.0 | 0 / 0.0 | 30 / 7,903 / 0.1 | 0.6 | 9.7 | 24 / 0.2 |
| louisville 1,0 | Lite | 13 | 0 / 0 / 0.0 | 0 / 0.0 | 28 / 6,776 / 0.1 | 0.6 | 12.4 | 20 / 0.1 |
| louisville -1,0 | Lite | 16 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 9,914 / 0.1 | 0.6 | 15.6 | 23 / 0.1 |
| louisville 0,1 | Lite | 7 | 0 / 0 / 0.0 | 0 / 0.0 | 23 / 6,858 / 0.1 | 0.5 | 6.5 | 19 / 0.1 |
| louisville 0,-1 | Lite | 43 | 0 / 0 / 0.0 | 0 / 0.0 | 28 / 6,755 / 0.1 | 0.6 | 42.3 | 24 / 0.1 |
| louisville 1,1 | Lite | 4 | 0 / 0 / 0.0 | 0 / 0.0 | 23 / 4,806 / 0.1 | 0.5 | 3.9 | 19 / 0.1 |
| louisville -1,1 | Lite | 12 | 0 / 0 / 0.0 | 0 / 0.0 | 30 / 14,400 / 0.1 | 0.8 | 11.0 | 25 / 0.2 |
| louisville 1,-1 | Lite | 9 | 0 / 0 / 0.0 | 0 / 0.0 | 24 / 6,587 / 0.1 | 0.6 | 8.6 | 23 / 0.2 |
| louisville -1,-1 | Lite | 10 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 9,202 / 0.1 | 0.7 | 9.0 | 24 / 0.2 |

The Louisville block's NavMesh: 201 sources collected in 1 ms; baked at once (as one 384 m square) in 116 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 26 | 11 | 44% | streets 5.8 | streets 4.7 | 44,123 / 4,633 | 78 / 22 | 39 / 17 | 58 / 1 | 0 / 0 |
| rural-5km | 10 | 3 | 34% | woods 2.0 | snow tone 1.2 | 45,759 / 5,789 | 41 / 25 | 30 / 20 | 187 / 1 | 0 / 0 |
| louisville-downtown | 47 | 11 | 23% | interior plans 6.2 | paths 2.6 | 129,882 / 10,503 | 215 / 36 | 53 / 31 | 165 / 1 | 0 / 0 |
| lexington-downtown | 40 | 11 | 29% | streets 6.6 | streets 5.4 | 120,737 / 7,969 | 139 / 32 | 58 / 27 | 96 / 1 | 1 / 0 |
| hazard | 19 | 6 | 33% | streets 3.2 | streets 2.0 | 66,835 / 6,255 | 95 / 32 | 44 / 27 | 158 / 1 | 1 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 55.60 | 27.80 | make 47.95 + switch on 7.65 ms; 1602 objects; destroy 3.92 ms; 3,000 rays 3.02 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 15.81 | 7.91 | make 10.37 + switch on 5.44 ms; 10 objects; destroy 1.93 ms; 3,000 rays 2.18 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 12.95 | 6.47 | make 4.21 + switch on 8.73 ms; 2 objects; destroy 0.11 ms; 3,000 rays 2.08 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.17 | 1.08 | make 2.13 + switch on 0.04 ms (+ 8.39 ms baking on the worker); 2 objects; destroy 0.11 ms; 3,000 rays 2.35 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.71 | 217.78 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.26 | 206.54 | objects 0.76, allocate 0.01, pack 7.36, apply 0.13 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.20 | 29.89 | objects 1.06, allocate 0.02, pack 7.39 (worker), apply 0.12 ms |
| primitives | Box, turned, flat colour | 20,000 | 25.75 | 1.29 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 95.10 | 4.76 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 44.18 | 2.21 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 93.16 | 4.66 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 17.13 | 0.86 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 23.14 | 1.16 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.64 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.77 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 9.91 | 1.98 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 48.44 | 9.69 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 27.90 | 9.30 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 43.87 | 14.62 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 14.23 | 7.11 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 12.18 | 12.18 |  |
| objects | BoxCollider added to one object | 2,000 | 39.48 | 19.74 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 25.42 | 127.12 |  |
| objects | RoomLight.Spawn | 100 | 10.44 | 104.37 |  |
| objects | switching all of the above on | 1 | 27.06 | 27058.40 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 12.37 | 12372.70 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 54 | 0.76 | 14.09 | worst 0.18 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 5.53 | 552.74 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 347.61 | 347608.10 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 386.96 | 386956.90 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 19.83 | 19833.00 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 447 ms, 1,051,962 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (21) | 0.20 | 0.04 | 0.11 | 4.58 | 10.22 | 3 / 2 / 0 / 0 | snow tone 8.6 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 909 / 1,091,588 | 1 | 157 / 774 (pool 29) |
| drive-east | 2400 (111) | 0.22 | 0.05 | 0.41 | 4.55 | 23.83 | 12 / 3 / 1 / 0 | fences and walls 8.0 | 38 (24 / 14) | 3 | 14 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 1,483 / 1,726,499 | 1 | 159 / 806 (pool 30) |
| teleport-louisville | 720 (157) | 1.25 | 0.05 | 6.60 | 12.33 | 61.07 | 61 / 24 / 5 / 3 | buildings 49.6 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.8 | 11 / 110 | 41 (16 Lite) / 1,659 / 2,310,378 | 3 | 180 / 831 (pool 32) |
| drive-louisville | 1800 (189) | 0.47 | 0.05 | 3.34 | 7.24 | 45.43 | 36 / 11 / 4 / 2 | objects 44.4 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 25 | 46 (12 Lite) / 2,066 / 3,090,475 | 5 | 226 / 867 (pool 32) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: snow tone (2x, worst 8.6 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: fences and walls (1x, worst 8.0 ms)
- **teleport-louisville**: set down in downtown Louisville (125 km away), standing; the frames over 8 ms were made by: buildings (3x, worst 49.6 ms), interior plans (11x, worst 36.6 ms), utility lines (1x, worst 24.5 ms), utility poles (1x, worst 14.9 ms), streets (2x, worst 6.4 ms), ground (1x, worst 6.3 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: objects (1x, worst 44.4 ms), interior plans (4x, worst 34.5 ms), buildings (2x, worst 7.7 ms), fields (1x, worst 6.3 ms), road network (1x, worst 6.2 ms), colliders (1x, worst 5.0 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.598 | 0% | 1,168 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 262 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on South Adams Street, at the game's starting zoom | 0.526 | 0% | 116 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on South Adams Street, at the game's starting zoom | 0.087 | 0% | 75 |
| `iso_downtown_day.png` | Downtown Lancaster: the middle of the square, on North Adams Street | 0.564 | 0% | 61 |
| `iso_downtown_night.png` | Downtown Lancaster: the middle of the square, on North Adams Street | 0.078 | 0% | 75 |
| `iso_residential_day.png` | The densest residential street: West Maple Avenue, 3 homes within 30 m | 0.472 | 0% | 63 |
| `iso_residential_night.png` | The densest residential street: West Maple Avenue, 3 homes within 30 m | 0.080 | 0% | 61 |
| `iso_intersection_day.png` | The busiest intersection: Lexington Street / Stanford Street / Richmond Street / Danville Street (4 ways in) | 0.342 | 0% | 86 |
| `iso_intersection_night.png` | The busiest intersection: Lexington Street / Stanford Street / Richmond Street / Danville Street (4 ways in) | 0.073 | 0% | 57 |
| `iso_green_day.png` | Green: a park, 761 m² inside the square | 0.478 | 0% | 54 |
| `iso_green_night.png` | Green: a park, 761 m² inside the square | 0.080 | 0% | 61 |
| `iso_parking_day.png` | Parking: a parking, 821 m² inside the square | 0.446 | 0% | 62 |
| `iso_parking_night.png` | Parking: a parking, 821 m² inside the square | 0.078 | 0% | 64 |
| `iso_edge_day.png` | The square's edge: where Lexington Street leaves it (north), the playable map giving way to the scenery ring | 0.398 | 0% | 58 |
| `iso_edge_night.png` | The square's edge: where Lexington Street leaves it (north), the playable map giving way to the scenery ring | 0.073 | 0% | 53 |
| `iso_waterway_day.png` | Railway: abandoned 'L&N Rowland Branch' | 0.614 | 0% | 52 |
| `iso_waterway_night.png` | Railway: abandoned 'L&N Rowland Branch' | 0.080 | 0% | 58 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, commercial 'Garrard County Judicial Center' (1,451 m² footprint) | 0.285 | 0% | 68 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, commercial 'Garrard County Judicial Center' (1,451 m² footprint) | 0.074 | 0% | 81 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on South Adams Street (downtown), looking along the street | 0.471 | 0% | 70 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on South Adams Street (downtown), looking along the street | 0.139 | 38% | 72 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Maple Avenue (the densest residential street), looking along it | 0.469 | 0% | 75 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Maple Avenue (the densest residential street), looking along it | 0.165 | 24% | 84 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.521 | 0% | 172 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.445 | 0% | 51 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.080 | 0% | 51 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 4.5, -44.9 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | 14.3, -1.5 | the place's own point in places.json (its OSM place node, which the square is built around), moved 14.4 m onto the nearest street |
| residential | -149.9, 135.7 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | -61.7, -17.7 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | -70.4, 105.5 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | -146.6, -174.1 | the biggest parking area inside the square |
| edge | -37.4, 200.0 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | 121.4, -263.0 | the point of a railway, river, canal or stream nearest the square's middle (none inside the square: this one is in the scenery ring) |
| landmark | -111.4, -118.8 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

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
| setup | yes | 61 |  |
| square: KentuckyMap.Build | yes | 3,350 |  |
| square: count | yes | 239 |  |
| square: pick spots | yes | 37 |  |
| square: pictures | yes | 5,095 |  |
| square: discard | yes | 63 |  |
| square: passes (warm rebuild) | yes | 1,284 |  |
| chunks: stress test | yes | 717 |  |
| chunks: Louisville 3x3 | yes | 1,109 |  |
| chunks: determinism | yes | 27 |  |
| perf: Lite against Full | yes | 975 |  |
| perf: determinism across build orders | yes | 189 |  |
| perf: what things cost | yes | 1,626 |  |
| perf: walking and driving through the state | yes | 158,979 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
