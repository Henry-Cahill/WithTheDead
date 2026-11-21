# CLONE-Z map harness: Frankfort (build ky-local-f2)

**Exit 0**: ok. Full run (streamed chunks in Lite detail), 179.9 s, started 2026-10-24 11:35:37.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Frankfort plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 3,927 ms |
| the same square again, warm, pass by pass | 1,627 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | -34.3, 21.2, facing 118° |
| exits | Snowfield (north) at 0.0, 197.0; KY 2261 east at 197.0, 12.6; KY 420 south (High Street) at -25.3, -197.0; KY 420 south (Ann Street) at -194.1, -197.0; KY 420 west (West Clinton Street) at -197.0, -46.1; KY 420 west (Mero Street) at -197.0, 116.0 |
| overhead renderers (hidden in the iso view) | 62 |
| `KentuckyMap.LastStats` | Kentucky map Frankfort (build ky-local-f2): 38 buildings, 27 beyond the edge, 131 street pieces, 316 containers, 311 lights, 691 trees, 6 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 38 with their middle inside the square (6 of them reach within 1 m of its edge or past it), 27 in the scenery ring; inside: house 20, commercial 15, church 2, civic 1 |
| map data: streets, lines, areas | 134 street pieces, 6 lines (rail 3, abandoned 2, river 1), 16 land-use areas (parking 11, pitch 2, grass 1, lot 1, park 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 10,157 | 5,386 | 5,386 | 5,079 | 2,813,014 | 1,727,646 | 127 | 12,861 | 324 (0) | 3,558 | 3,787 | 316 |
| RaidOverhead | 209 | 171 | 171 | 171 | 174,333 | 103,425 | 69 | 612 | 0 (0) | 0 | 171 | 0 |
| RaidScenery | 91 | 90 | 90 | 90 | 74,183 | 96,738 | 29 | 191 | 0 (0) | 0 | 57 | 0 |
| **all** | 10,457 | 5,647 | 5,647 | 5,340 | 3,061,530 | 1,927,809 | 178 | 13,664 | 324 (0) | 3,558 | 4,015 | 316 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 9,025 | 5,119 | 5,119 | 4,818 | 2,166,016 | 1,376,322 | 81 | 11,600 | 302 (0) | 2,027 | 3,627 | 246 |
| World/OutdoorProps | 114 | 114 | 114 | 114 | 243,082 | 153,846 | 16 | 433 | 0 (0) | 0 | 114 | 0 |
| World/OutdoorGround | 97 | 97 | 97 | 97 | 86,974 | 37,822 | 13 | 421 | 0 (0) | 0 | 0 | 0 |
| World/Building shells | 47 | 46 | 46 | 46 | 316,280 | 158,664 | 42 | 397 | 0 (0) | 0 | 46 | 0 |
| RaidOverhead/Building shells | 47 | 46 | 46 | 46 | 130,429 | 64,697 | 55 | 320 | 0 (0) | 0 | 46 | 0 |
| RaidOverhead/Roofs | 46 | 46 | 46 | 46 | 16,811 | 10,527 | 29 | 136 | 0 (0) | 0 | 46 | 0 |
| RaidOverhead/Interior KY# | 72 | 36 | 36 | 36 | 22,896 | 21,752 | 5 | 113 | 0 (0) | 0 | 36 | 0 |
| RaidScenery/SceneryGround | 31 | 31 | 31 | 31 | 7,168 | 2,406 | 7 | 95 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SceneryProps | 37 | 37 | 37 | 37 | 26,572 | 19,440 | 21 | 74 | 0 (0) | 0 | 37 | 0 |
| RaidOverhead/Overhead | 26 | 26 | 26 | 26 | 699 | 233 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| RaidScenery/Wires | 20 | 20 | 20 | 20 | 2,112 | 3,660 | 1 | 20 | 0 (0) | 0 | 20 | 0 |
| RaidOverhead/Wires | 17 | 17 | 17 | 17 | 3,498 | 6,216 | 1 | 17 | 0 (0) | 0 | 17 | 0 |
| World/StreetLamp | 18 | 9 | 9 | 3 | 638 | 980 | 3 | 9 | 9 (0) | 9 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 38,327 | 71,230 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 8,881 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 158 | 10% | 1 | 158 |
| (set up the site) | 3 | 0% | 1 | 3 |
| ground | 2 | 0% | 1 | 2 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 3 | 0% | 1 | 3 |
| lots | 12 | 1% | 39 | 3 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 0 | 0% | 2 | 0 |
| snow tone | 26 | 2% | 1 | 26 |
| road network | 8 | 0% | 1 | 8 |
| junctions | 3 | 0% | 1 | 3 |
| streets | 28 | 2% | 145 | 7 |
| sidewalks | 15 | 1% | 52 | 3 |
| berms and driveways | 9 | 1% | 49 | 1 |
| road markings | 1 | 0% | 12 | 0 |
| paths | 1 | 0% | 25 | 0 |
| railways | 0 | 0% | 6 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 37 | 2% | 16 | 4 |
| interior plans | 55 | 3% | 37 | 7 |
| interior shell | 4 | 0% | 36 | 0 |
| interior walls | 43 | 3% | 36 | 6 |
| interior floors | 6 | 0% | 36 | 1 |
| interior doors | 134 | 8% | 72 | 7 |
| interior switches | 22 | 1% | 36 | 1 |
| interior rooms | 796 | 49% | 234 | 89 |
| interior wall meshes | 29 | 2% | 36 | 3 |
| interior finish | 79 | 5% | 36 | 8 |
| cutaway | 5 | 0% | 3 | 2 |
| utility lines | 3 | 0% | 1 | 3 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 3 | 0% | 1 | 3 |
| street lights | 2 | 0% | 1 | 2 |
| signs | 1 | 0% | 1 | 1 |
| street furniture | 1 | 0% | 1 | 1 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 4 | 0% | 1 | 4 |
| nature plan | 1 | 0% | 1 | 1 |
| driveway plan | 0 | 0% | 1 | 0 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 2 | 0% | 30 | 0 |
| grounds trees | 2 | 0% | 4 | 1 |
| street trees | 1 | 0% | 85 | 0 |
| fence rows | 6 | 0% | 156 | 3 |
| woods | 23 | 1% | 9362 | 5 |
| driveway cars | 1 | 0% | 8 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 8 | 0% | 115 | 1 |
| kerbside cars | 8 | 1% | 127 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 13 | 1% | 39 | 3 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 0 | 0% | 1 | 0 |
| (after the last pass) | 5 | 0% | 1 | 5 |
| (emit the overhead meshes) | 4 | 0% | 1 | 4 |
| (emit the outdoor meshes) | 53 | 3% | 1 | 53 |
| **total** | **1,627** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Frankfort | 12318_9434 (192_147) | 0 + 0 | **19** | data 11 | 0 | 35 | 30 | 4,732 | 3,138 | 0 | 1 | 6 | 15/6/3 | 0E 0W |
| rural-5km | 5 km from Frankfort (bearing 70°) | 12355_9447 (193_147) | 0 + 0 | **5** | buildings 1 | 0 | 33 | 28 | 3,957 | 3,310 | 0 | 1 | 1 | 8/1/4 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 158 + 12 | **13** | buildings 2 | 0 | 33 | 28 | 7,211 | 4,685 | 0 | 1 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 463 + 58 | **8** | buildings 2 | 0 | 33 | 28 | 4,048 | 2,898 | 0 | 1 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 6 + 2 | **4** | objects 1 | 0 | 31 | 26 | 4,170 | 4,326 | 0 | 1 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 10.6 | 1 | 10.6 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.2 | 1 | 0.2 |
| snow tone | 0.3 | 1 | 0.3 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 0.3 | 1 | 0.3 |
| sidewalks | 0.7 | 1 | 0.7 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.6 | 1 | 0.6 |
| landmarks | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.8 | 1 | 0.8 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.1 | 1 | 0.1 |
| fence rows | 0.1 | 1 | 0.1 |
| woods | 0.3 | 4 | 0.2 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| objects | 1.6 | 1 | 1.6 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.2 | 1 | 0.2 |
| snow tone | 0.4 | 1 | 0.4 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 0.1 | 1 | 0.1 |
| sidewalks | 0.1 | 1 | 0.1 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 1.1 | 1 | 1.1 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.1 | 1 | 0.1 |
| woods | 0.4 | 4 | 0.2 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 2.1 | 1 | 2.1 |
| water | 1.8 | 1 | 1.8 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.1 | 1 | 0.1 |
| snow tone | 0.4 | 1 | 0.4 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.1 | 1 | 0.1 |
| sidewalks | 0.2 | 1 | 0.2 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.0 | 3 | 2.2 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| mapped trees | 0.4 | 1 | 0.4 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.1 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| objects | 0.6 | 1 | 0.6 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.7 | 1 | 0.7 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| snow tone | 0.4 | 1 | 0.4 |
| road network | 0.7 | 1 | 0.7 |
| junctions | 0.7 | 1 | 0.7 |
| streets | 0.3 | 1 | 0.3 |
| sidewalks | 0.9 | 1 | 0.9 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 2.4 | 2 | 2.1 |
| landmarks | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.1 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.1 | 1 | 0.1 |
| snow tone | 0.4 | 1 | 0.4 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 0.3 | 1 | 0.3 |
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
| fence rows | 0.5 | 1 | 0.5 |
| woods | 0.9 | 4 | 0.4 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| objects | 0.6 | 1 | 0.6 |
| meshes | 0.3 | 1 | 0.3 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 192_148 | yes | 10 | 6 |
| 192_147 | yes | 29 | 14 |
| 193_147 | yes | 32 | 4 |
| 192_146 | yes | 10 | 3 |
| 191_147 | yes | 4 | 1 |
| 183_148 | yes | 158 | 12 |
| 196_145 | yes | 463 | 58 |
| 210_134 | yes | 6 | 2 |
| 182_148 | yes | 148 | 10 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 97 ms (slowest 23 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **9** | buildings 3 | 0 | 33 | 28 | 7,211 | 4,685 | 0 | 1 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **9** | buildings 2 | 0 | 33 | 28 | 6,918 | 4,964 | 0 | 1 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 148 + 10 | **13** | buildings 4 | 0 | 32 | 27 | 8,273 | 4,883 | 0 | 1 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **6** | buildings 1 | 0 | 28 | 23 | 6,538 | 3,625 | 0 | 1 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **18** | landmarks 4 | 0 | 34 | 29 | 4,898 | 3,358 | 0 | 1 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **5** | objects 1 | 0 | 29 | 24 | 4,726 | 3,712 | 0 | 1 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **23** | buildings 16 | 0 | 36 | 31 | 13,321 | 7,159 | 0 | 1 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **7** | junctions 1 | 0 | 31 | 26 | 4,274 | 3,055 | 0 | 1 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **8** | buildings 2 | 0 | 30 | 25 | 7,234 | 5,063 | 0 | 1 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 288 | 241 | 241 | 241 | 63,393 | 40,504 | 51 | 588 | 0 (0) | 9 | 116 | 0 |

### Determinism

Chunk 12318_9434 (start-centre) built twice: **match**: identical, in the same order (4,732 vertices, 3,138 triangles, 35 objects, 30 meshes; hash df4fa10819719fda vs df4fa10819719fda; 1 colliders, lights and loot containers, hash 2903e617f5368050 vs 2903e617f5368050). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Lite | 19 | 0 / 0 / 0.0 | 0 / 0.0 | 29 / 4,136 / 0.1 | 0.8 | 17.7 | 24 / 3.4 |
| rural-5km | Lite | 5 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 3,330 / 0.1 | 0.6 | 4.1 | 26 / 0.2 |
| louisville-downtown | Lite | 13 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 6,535 / 0.1 | 0.5 | 12.1 | 21 / 0.3 |
| lexington-downtown | Lite | 8 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 3,372 / 0.2 | 0.7 | 7.4 | 22 / 0.3 |
| hazard | Lite | 4 | 0 / 0 / 0.0 | 0 / 0.0 | 25 / 3,558 / 0.1 | 0.5 | 3.9 | 25 / 0.2 |
| louisville 0,0 | Lite | 9 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 6,535 / 0.1 | 1.0 | 7.6 | 21 / 0.1 |
| louisville 1,0 | Lite | 9 | 0 / 0 / 0.0 | 0 / 0.0 | 27 / 6,242 / 0.1 | 0.7 | 8.7 | 19 / 0.1 |
| louisville -1,0 | Lite | 13 | 0 / 0 / 0.0 | 0 / 0.0 | 26 / 7,687 / 0.2 | 1.1 | 11.7 | 22 / 0.2 |
| louisville 0,1 | Lite | 6 | 0 / 0 / 0.0 | 0 / 0.0 | 22 / 5,917 / 0.1 | 0.6 | 5.6 | 18 / 0.1 |
| louisville 0,-1 | Lite | 18 | 0 / 0 / 0.0 | 0 / 0.0 | 28 / 4,232 / 0.1 | 1.2 | 16.9 | 24 / 0.2 |
| louisville 1,1 | Lite | 5 | 0 / 0 / 0.0 | 0 / 0.0 | 23 / 4,050 / 0.1 | 1.0 | 3.8 | 19 / 0.1 |
| louisville -1,1 | Lite | 23 | 0 / 0 / 0.0 | 0 / 0.0 | 30 / 12,711 / 0.1 | 1.3 | 21.8 | 25 / 0.2 |
| louisville 1,-1 | Lite | 7 | 0 / 0 / 0.0 | 0 / 0.0 | 25 / 3,629 / 0.1 | 0.7 | 5.9 | 24 / 0.2 |
| louisville -1,-1 | Lite | 8 | 0 / 0 / 0.0 | 0 / 0.0 | 24 / 6,558 / 0.1 | 0.6 | 7.4 | 21 / 0.1 |

The Louisville block's NavMesh: 193 sources collected in 1 ms; baked at once (as one 384 m square) in 116 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 43 | 6 | 14% | streets 9.2 | sidewalks 0.8 | 100,626 / 4,732 | 159 / 35 | 65 / 30 | 156 / 1 | 3 / 0 |
| rural-5km | 21 | 4 | 20% | interior plans 3.6 | objects 0.6 | 52,154 / 3,959 | 111 / 31 | 48 / 26 | 89 / 1 | 1 / 0 |
| louisville-downtown | 55 | 8 | 15% | interior plans 7.1 | buildings 2.8 | 129,586 / 7,103 | 214 / 33 | 53 / 28 | 165 / 1 | 0 / 0 |
| lexington-downtown | 47 | 6 | 13% | streets 6.3 | sidewalks 0.9 | 119,812 / 4,078 | 141 / 33 | 59 / 28 | 95 / 1 | 2 / 0 |
| hazard | 22 | 4 | 20% | streets 3.3 | objects 0.8 | 62,683 / 4,376 | 84 / 32 | 43 / 27 | 150 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 71.90 | 35.95 | make 61.73 + switch on 10.17 ms; 1602 objects; destroy 6.86 ms; 3,000 rays 3.28 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 20.39 | 10.20 | make 13.45 + switch on 6.95 ms; 10 objects; destroy 3.96 ms; 3,000 rays 2.71 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 16.84 | 8.42 | make 5.37 + switch on 11.46 ms; 2 objects; destroy 0.15 ms; 3,000 rays 2.50 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.48 | 1.24 | make 2.44 + switch on 0.03 ms (+ 9.92 ms baking on the worker); 2 objects; destroy 0.08 ms; 3,000 rays 2.36 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 11.63 | 290.76 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 10.54 | 263.56 | objects 1.25, allocate 0.06, pack 9.01, apply 0.21 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.48 | 37.03 | objects 1.34, allocate 0.02, pack 8.34 (worker), apply 0.13 ms |
| primitives | Box, turned, flat colour | 20,000 | 57.26 | 2.86 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 65.79 | 3.29 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 41.31 | 2.07 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 127.61 | 6.38 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 26.52 | 1.33 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 48.38 | 2.42 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 8.29 | 0.41 | 80,000 vertices |
| primitives | Tri | 20,000 | 6.70 | 0.34 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 40.85 | 8.17 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 72.64 | 14.53 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 35.08 | 11.69 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 41.32 | 13.77 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 17.69 | 8.84 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 14.91 | 14.91 |  |
| objects | BoxCollider added to one object | 2,000 | 49.40 | 24.70 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 31.32 | 156.60 |  |
| objects | RoomLight.Spawn | 100 | 13.63 | 136.26 |  |
| objects | switching all of the above on | 1 | 35.34 | 35342.40 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 18.75 | 18746.00 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 59 | 0.68 | 11.46 | worst 0.13 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.56 | 255.58 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 432.20 | 432201.40 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 439.17 | 439169.90 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 31.20 | 31194.80 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 660 ms, 1,057,228 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (21) | 0.19 | 0.04 | 0.14 | 3.88 | 11.55 | 3 / 1 / 0 / 0 | landmarks 4.3 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 875 / 1,111,786 | 1 | 184 / 783 (pool 33) |
| drive-east | 2400 (144) | 0.27 | 0.05 | 1.57 | 4.91 | 18.45 | 20 / 4 / 1 / 0 | meshes 18.4 | 40 (24 / 16) | 0 | 19 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 1,484 / 1,927,740 | 1 | 184 / 820 (pool 33) |
| teleport-louisville | 720 (115) | 0.96 | 0.05 | 6.50 | 9.42 | 49.98 | 47 / 15 / 4 / 1 | interior plans 30.7 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.5 | 10 / 89 | 41 (16 Lite) / 1,530 / 2,273,232 | 3 | 206 / 838 (pool 35) |
| drive-louisville | 1800 (158) | 0.40 | 0.05 | 3.45 | 5.60 | 30.29 | 29 / 6 / 1 / 0 | interior plans 30.2 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 20 | 46 (12 Lite) / 1,937 / 3,076,762 | 5 | 251 / 875 (pool 35) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: buildings (1x, worst 0.5 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: meshes (1x, worst 18.4 ms), interior plans (2x, worst 13.8 ms)
- **teleport-louisville**: set down in downtown Louisville (78 km away), standing; the frames over 8 ms were made by: interior plans (9x, worst 30.7 ms), buildings (2x, worst 4.0 ms), fences and walls (1x, worst 3.8 ms), sidewalks (1x, worst 2.2 ms), kerbside cars (1x, worst 1.7 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (3x, worst 30.2 ms), buildings (1x, worst 10.6 ms), sidewalks (1x, worst 5.1 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 46 edit-mode artefacts

- `(edit mode) Error: Raid_sign_HOLMES SAINT SCHOOL0A120D373631: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOLMES SAINT SCHOOL0A120D373631: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_COMMUNITY STORAGE353432070807: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_COMMUNITY STORAGE353432070807: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_DIVISION OF ENGINEERIN050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_DIVISION OF ENGINEERIN050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_EDEN PLANTS & VINTAGE373531080706: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_EDEN PLANTS & VINTAGE373531080706: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OFFICE OF FLEET MANAGE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OFFICE OF FLEET MANAGE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS3B2A06030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS3B2A06030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS392906030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS392906030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_AEES (ART'S ELECTRIC)1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_AEES (ART'S ELECTRIC)1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOTEL0A0C1233322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOTEL0A0C1233322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OFFICE OF FLEET MANAGE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OFFICE OF FLEET MANAGE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.575 | 0% | 1,303 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 249 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Mero Street, at the game's starting zoom | 0.430 | 0% | 145 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Mero Street, at the game's starting zoom | 0.101 | 0% | 99 |
| `iso_downtown_day.png` | Downtown Frankfort: the middle of the square, on Elk Alley | 0.404 | 0% | 61 |
| `iso_downtown_night.png` | Downtown Frankfort: the middle of the square, on Elk Alley | 0.073 | 0% | 65 |
| `iso_residential_day.png` | The densest residential street: Lewis Street, 4 homes within 30 m | 0.481 | 0% | 57 |
| `iso_residential_night.png` | The densest residential street: Lewis Street, 4 homes within 30 m | 0.081 | 0% | 64 |
| `iso_intersection_day.png` | The busiest intersection: Ann Street / West Clinton Street (4 ways in) | 0.355 | 0% | 72 |
| `iso_intersection_night.png` | The busiest intersection: Ann Street / West Clinton Street (4 ways in) | 0.073 | 0% | 73 |
| `iso_green_day.png` | Green: park 'Fort Hill Civil War Park', 5,319 m² inside the square | 0.588 | 0% | 54 |
| `iso_green_night.png` | Green: park 'Fort Hill Civil War Park', 5,319 m² inside the square | 0.078 | 0% | 59 |
| `iso_parking_day.png` | Parking: a parking, 7,259 m² inside the square | 0.386 | 0% | 56 |
| `iso_parking_night.png` | Parking: a parking, 7,259 m² inside the square | 0.075 | 0% | 65 |
| `iso_edge_day.png` | The square's edge: where Holmes Street leaves it (east), the playable map giving way to the scenery ring | 0.463 | 0% | 61 |
| `iso_edge_night.png` | The square's edge: where Holmes Street leaves it (east), the playable map giving way to the scenery ring | 0.076 | 0% | 63 |
| `iso_waterway_day.png` | Railway: a rail | 0.558 | 0% | 52 |
| `iso_waterway_night.png` | Railway: a rail | 0.079 | 0% | 48 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, commercial 'Kentucky Transportation Cabinet' (9,435 m² footprint) | 0.454 | 0% | 57 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, commercial 'Kentucky Transportation Cabinet' (9,435 m² footprint) | 0.090 | 0% | 57 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Mero Street (downtown), looking along the street | 0.408 | 0% | 57 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Mero Street (downtown), looking along the street | 0.174 | 22% | 109 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of Lewis Street (the densest residential street), looking along it | 0.389 | 0% | 61 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of Lewis Street (the densest residential street), looking along it | 0.139 | 20% | 61 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.521 | 0% | 135 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.440 | 0% | 57 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.080 | 0% | 90 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -34.3, 21.2 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -2.5, 1.4 | the place's own point in places.json (its OSM place node, which the square is built around), moved 2.9 m onto the nearest street |
| residential | -145.7, 50.8 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | -132.1, -81.4 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | 161.8, 154.2 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | 161.0, -161.8 | the biggest parking area inside the square |
| edge | 200.0, 13.2 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | 29.8, -258.3 | the point of a railway, river, canal or stream nearest the square's middle (none inside the square: this one is in the scenery ring) |
| landmark | -83.0, 110.4 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 46 edit-mode artefacts

- `(edit mode) Error: Raid_sign_HOLMES SAINT SCHOOL0A120D373631: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOLMES SAINT SCHOOL0A120D373631: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_COMMUNITY STORAGE353432070807: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_COMMUNITY STORAGE353432070807: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_DIVISION OF ENGINEERIN050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_DIVISION OF ENGINEERIN050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_EDEN PLANTS & VINTAGE373531080706: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_EDEN PLANTS & VINTAGE373531080706: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OFFICE OF FLEET MANAGE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OFFICE OF FLEET MANAGE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS3B2A06030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS3B2A06030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS392906030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS392906030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_AEES (ART'S ELECTRIC)1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_AEES (ART'S ELECTRIC)1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOTEL0A0C1233322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOTEL0A0C1233322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OFFICE OF FLEET MANAGE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OFFICE OF FLEET MANAGE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 62 |  |
| square: KentuckyMap.Build | yes | 3,929 |  |
| square: count | yes | 311 |  |
| square: pick spots | yes | 82 |  |
| square: pictures | yes | 5,371 |  |
| square: discard | yes | 193 |  |
| square: passes (warm rebuild) | yes | 1,709 |  |
| chunks: stress test | yes | 878 |  |
| chunks: Louisville 3x3 | yes | 1,222 |  |
| chunks: determinism | yes | 24 |  |
| perf: Lite against Full | yes | 1,225 |  |
| perf: determinism across build orders | yes | 317 |  |
| perf: what things cost | yes | 1,978 |  |
| perf: walking and driving through the state | yes | 162,638 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
