# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 189.6 s, started 2026-10-24 11:49:47.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 7,397 ms |
| the same square again, warm, pass by pass | 5,866 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 102.7, -33.0, facing -70° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 915 containers, 949 lights, 335 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,257 | 14,318 | 14,318 | 13,373 | 7,514,797 | 4,598,376 | 139 | 33,707 | 965 (0) | 8,381 | 9,785 | 915 |
| RaidOverhead | 588 | 470 | 470 | 470 | 459,114 | 271,699 | 140 | 1,855 | 0 (0) | 0 | 470 | 0 |
| RaidScenery | 118 | 117 | 117 | 117 | 80,444 | 104,466 | 48 | 361 | 0 (0) | 0 | 72 | 0 |
| **all** | 29,963 | 14,905 | 14,905 | 13,960 | 8,054,355 | 4,974,541 | 248 | 35,923 | 965 (0) | 8,381 | 10,327 | 915 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,472 | 13,880 | 13,880 | 12,954 | 6,280,631 | 3,959,213 | 82 | 31,295 | 927 (0) | 6,098 | 9,512 | 809 |
| World/Building shells | 131 | 130 | 130 | 130 | 802,262 | 403,344 | 51 | 1,216 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 320,313 | 159,227 | 126 | 947 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 305,479 | 179,244 | 17 | 626 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 124,784 | 54,063 | 14 | 547 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 132 | 132 | 132 | 132 | 46,017 | 27,069 | 50 | 431 | 0 (0) | 0 | 132 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 36,436 | 21,756 | 41 | 257 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,240 | 430 | 5 | 73 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 29 | 29 | 29 | 29 | 2,478 | 4,392 | 1 | 29 | 0 (0) | 0 | 29 | 0 |
| RaidOverhead/Wires | 26 | 26 | 26 | 26 | 4,734 | 8,238 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| World/StreetLamp | 44 | 22 | 22 | 3 | 1,617 | 2,500 | 3 | 22 | 22 (0) | 22 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,286 | 77,886 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 19,292 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 88 | 2% | 1 | 88 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 4 | 0% | 1 | 4 |
| lots | 1 | 0% | 85 | 0 |
| cemeteries | 5 | 0% | 500 | 0 |
| grounds | 0 | 0% | 2 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 30 | 1% | 1 | 30 |
| road network | 8 | 0% | 1 | 8 |
| junctions | 8 | 0% | 1 | 8 |
| streets | 34 | 1% | 189 | 2 |
| sidewalks | 32 | 1% | 67 | 4 |
| berms and driveways | 19 | 0% | 62 | 1 |
| road markings | 1 | 0% | 10 | 0 |
| paths | 2 | 0% | 72 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 161 | 3% | 59 | 12 |
| interior plans | 234 | 4% | 798 | 7 |
| interior shell | 17 | 0% | 116 | 1 |
| interior walls | 163 | 3% | 116 | 10 |
| interior floors | 22 | 0% | 116 | 4 |
| interior doors | 662 | 11% | 233 | 33 |
| interior switches | 107 | 2% | 116 | 9 |
| interior rooms | 3,058 | 52% | 697 | 36 |
| interior wall meshes | 134 | 2% | 116 | 11 |
| interior finish | 268 | 5% | 232 | 14 |
| interior merge | 318 | 5% | 408 | 11 |
| cutaway | 15 | 0% | 8 | 2 |
| utility lines | 4 | 0% | 26 | 2 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 90 | 3 |
| service drops | 1 | 0% | 251 | 0 |
| street lights | 3 | 0% | 48 | 0 |
| signs | 2 | 0% | 115 | 0 |
| street furniture | 5 | 0% | 255 | 2 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 157 | 3% | 56 | 154 |
| nature plan | 2 | 0% | 1 | 2 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 15 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 13 | 0% | 9356 | 1 |
| driveway cars | 3 | 0% | 30 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 4 | 0% | 45 | 0 |
| kerbside cars | 41 | 1% | 231 | 9 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 16 | 0% | 345 | 0 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 18 | 0% | 1 | 18 |
| (emit the overhead meshes) | 14 | 0% | 1 | 14 |
| (emit the outdoor meshes) | 154 | 3% | 1 | 154 |
| **total** | **5,866** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **102** | objects 43 | 1 | 136 | 65 | 135,061 | 71,000 | 1 | 121 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **35** | interior plans 2 | 1 | 145 | 64 | 133,076 | 75,702 | 4 | 218 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 196 + 11 | **69** | interior plans 4 | 1 | 210 | 53 | 126,528 | 68,791 | 0 | 162 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 468 + 35 | **85** | buildings 37 | 1 | 175 | 58 | 131,424 | 70,032 | 1 | 109 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 6 + 2 | **20** | streets 3 | 1 | 63 | 42 | 54,031 | 39,943 | 0 | 145 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.4 | 1 | 2.4 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 3.8 | 1 | 3.8 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 1.8 | 1 | 1.8 |
| junctions | 1.3 | 1 | 1.3 |
| streets | 6.4 | 3 | 2.6 |
| sidewalks | 3.4 | 2 | 2.0 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 1.5 | 1 | 1.5 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 7.9 | 4 | 2.4 |
| interior plans | 17.8 | 6 | 7.2 |
| cutaway | 1.0 | 1 | 1.0 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.1 | 1 | 0.1 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.1 | 1 | 0.1 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.4 | 1 | 0.4 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.5 | 1 | 0.5 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.4 | 1 | 1.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 1.0 | 1 | 1.0 |
| objects | 43.1 | 1 | 43.1 |
| meshes | 0.9 | 1 | 0.9 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.2 | 1 | 0.2 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.5 | 1 | 1.5 |
| sidewalks | 0.9 | 1 | 0.9 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.3 | 3 | 2.2 |
| interior plans | 10.8 | 6 | 2.3 |
| cutaway | 1.1 | 1 | 1.1 |
| utility lines | 0.0 | 1 | 0.0 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.8 | 1 | 0.8 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 3.0 | 2 | 2.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.8 | 1 | 0.8 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.7 | 1 | 1.7 |
| driveway cars | 0.4 | 1 | 0.4 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.4 | 1 | 0.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.3 | 1 | 1.3 |
| objects | 1.1 | 1 | 1.1 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 2.5 | 1 | 2.5 |
| water | 2.7 | 2 | 2.7 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.0 | 1 | 1.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.4 | 1 | 1.4 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.8 | 1 | 0.8 |
| paths | 4.9 | 3 | 2.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 16.3 | 7 | 3.9 |
| interior plans | 20.7 | 8 | 4.5 |
| cutaway | 1.0 | 1 | 1.0 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.6 | 1 | 1.6 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.6 | 1 | 0.6 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 1 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.5 | 1 | 0.5 |
| kerbside cars | 1.8 | 1 | 1.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.6 | 2 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.9 | 1 | 1.9 |
| objects | 1.1 | 1 | 1.1 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.5 | 1 | 0.5 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 6.2 | 2 | 5.7 |
| sidewalks | 2.9 | 2 | 2.4 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.9 | 1 | 0.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 45.6 | 5 | 36.5 |
| interior plans | 11.3 | 5 | 3.0 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.9 | 1 | 0.9 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.5 | 1 | 0.5 |
| street furniture | 0.7 | 1 | 0.7 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.5 | 1 | 0.5 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 2.2 | 2 | 2.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 1 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.1 | 1 | 1.1 |
| colliders | 1.1 | 1 | 1.1 |
| objects | 1.2 | 1 | 1.2 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.1 | 1 | 0.1 |
| lots | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 3.0 | 2 | 2.8 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.9 | 1 | 0.9 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| woods | 5.9 | 3 | 2.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.2 | 1 | 0.2 |
| kerbside cars | 0.2 | 1 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.0 | 1 | 1.0 |
| objects | 1.4 | 1 | 1.4 |
| meshes | 0.4 | 1 | 0.4 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 54 | 28 |
| 186_141 | yes | 20 | 2 |
| 185_141 | yes | 9 | 2 |
| 185_142 | yes | 13 | 3 |
| 183_148 | yes | 196 | 11 |
| 196_145 | yes | 468 | 35 |
| 210_134 | yes | 6 | 2 |
| 182_148 | yes | 184 | 9 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 584 ms (slowest 103 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **47** | buildings 4 | 1 | 210 | 53 | 126,528 | 68,791 | 0 | 162 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **66** | interior plans 8 | 1 | 201 | 75 | 145,407 | 80,027 | 0 | 202 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 184 + 9 | **59** | interior plans 9 | 1 | 249 | 62 | 129,711 | 72,008 | 3 | 197 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **71** | interior plans 28 | 1 | 159 | 57 | 119,730 | 63,605 | 0 | 152 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **49** | interior plans 4 | 0 | 180 | 63 | 93,054 | 54,343 | 1 | 132 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **42** | interior plans 7 | 0 | 152 | 53 | 126,901 | 72,701 | 0 | 124 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **103** | buildings 59 | 1 | 209 | 53 | 127,457 | 69,194 | 0 | 210 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **51** | kerbside cars 6 | 1 | 147 | 56 | 96,059 | 57,894 | 4 | 90 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **96** | interior plans 22 | 2 | 189 | 45 | 209,011 | 116,297 | 0 | 159 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,698 | 517 | 517 | 512 | 1,173,858 | 654,860 | 128 | 2,183 | 8 (0) | 1,428 | 356 | 86 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (135,061 vertices, 71,000 triangles, 136 objects, 65 meshes; hash 45fddc2320d6ad0d vs 45fddc2320d6ad0d; 122 colliders, lights and loot containers, hash 41c272f07cf29cc0 vs 41c272f07cf29cc0). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 102 | 119 / 62 / 0.9 | 0 / 0.0 | 61 / 132,195 / 0.5 | 42.7 | 58.5 | 42 / 3.6 |
| rural-5km | Full | 35 | 217 / 69 / 1.3 | 0 / 0.0 | 63 / 130,477 / 0.4 | 1.8 | 31.8 | 43 / 0.5 |
| louisville-downtown | Full | 69 | 161 / 119 / 1.9 | 0 / 0.0 | 51 / 123,917 / 0.4 | 1.5 | 65.9 | 36 / 0.5 |
| lexington-downtown | Full | 85 | 107 / 72 / 1.1 | 0 / 0.0 | 53 / 128,620 / 0.2 | 1.4 | 82.8 | 39 / 0.5 |
| hazard | Full | 20 | 144 / 15 / 0.9 | 0 / 0.0 | 37 / 50,833 / 0.3 | 1.3 | 18.0 | 30 / 0.2 |
| louisville 0,0 | Full | 47 | 161 / 119 / 1.4 | 0 / 0.0 | 51 / 123,917 / 0.3 | 1.1 | 44.0 | 36 / 0.3 |
| louisville 1,0 | Full | 66 | 201 / 108 / 1.7 | 0 / 0.0 | 73 / 142,738 / 0.3 | 2.3 | 61.6 | 46 / 0.3 |
| louisville -1,0 | Full | 59 | 193 / 140 / 1.7 | 0 / 0.0 | 52 / 126,288 / 0.2 | 1.1 | 55.9 | 40 / 0.5 |
| louisville 0,1 | Full | 71 | 151 / 83 / 1.2 | 0 / 0.0 | 54 / 117,229 / 0.4 | 1.3 | 68.8 | 36 / 0.5 |
| louisville 0,-1 | Full | 49 | 131 / 77 / 1.2 | 0 / 0.0 | 58 / 88,832 / 0.3 | 1.6 | 46.2 | 37 / 0.3 |
| louisville 1,1 | Full | 42 | 123 / 76 / 1.7 | 0 / 0.0 | 50 / 123,833 / 0.2 | 1.2 | 39.1 | 35 / 0.3 |
| louisville -1,1 | Full | 103 | 209 / 116 / 3.8 | 0 / 0.0 | 50 / 124,181 / 0.4 | 1.8 | 97.5 | 37 / 0.6 |
| louisville 1,-1 | Full | 51 | 85 / 48 / 1.2 | 0 / 0.0 | 47 / 91,739 / 0.5 | 1.8 | 48.2 | 36 / 0.8 |
| louisville -1,-1 | Full | 96 | 158 / 110 / 2.7 | 0 / 0.0 | 41 / 205,874 / 0.6 | 2.1 | 91.4 | 30 / 0.5 |

The Louisville block's NavMesh: 333 sources collected in 4 ms; baked at once (as one 384 m square) in 246 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 66 | 17 | 25% | buildings 5.7 | junctions 3.5 | 135,061 / 6,020 | 136 / 36 | 65 / 31 | 121 / 1 | 1 / 0 |
| rural-5km | 34 | 5 | 15% | interior plans 2.6 | objects 1.1 | 132,918 / 4,492 | 142 / 38 | 64 / 33 | 213 / 1 | 4 / 0 |
| louisville-downtown | 60 | 9 | 15% | interior plans 5.5 | buildings 3.0 | 118,690 / 6,777 | 212 / 33 | 53 / 28 | 160 / 1 | 0 / 0 |
| lexington-downtown | 52 | 7 | 13% | streets 6.3 | sidewalks 1.3 | 126,147 / 4,078 | 177 / 33 | 59 / 28 | 111 / 1 | 2 / 0 |
| hazard | 26 | 5 | 19% | streets 3.2 | objects 0.8 | 61,978 / 4,435 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 84.07 | 42.03 | make 70.80 + switch on 13.27 ms; 1602 objects; destroy 3.62 ms; 3,000 rays 2.66 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 20.10 | 10.05 | make 12.88 + switch on 7.22 ms; 10 objects; destroy 2.84 ms; 3,000 rays 2.53 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 15.46 | 7.73 | make 4.94 + switch on 10.52 ms; 2 objects; destroy 0.13 ms; 3,000 rays 2.17 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.33 | 1.17 | make 2.29 + switch on 0.04 ms (+ 11.31 ms baking on the worker); 2 objects; destroy 0.08 ms; 3,000 rays 2.48 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 10.30 | 257.51 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 11.07 | 276.79 | objects 0.95, allocate 0.02, pack 9.92, apply 0.18 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.59 | 39.66 | objects 1.43, allocate 0.02, pack 8.19 (worker), apply 0.14 ms |
| primitives | Box, turned, flat colour | 20,000 | 60.11 | 3.01 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 104.12 | 5.21 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 55.19 | 2.76 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 134.46 | 6.72 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 38.89 | 1.95 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 38.95 | 1.95 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 8.12 | 0.41 | 80,000 vertices |
| primitives | Tri | 20,000 | 6.14 | 0.31 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 65.09 | 13.02 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 42.97 | 8.60 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 25.68 | 8.56 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 53.78 | 17.93 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 16.99 | 8.49 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 16.02 | 16.02 |  |
| objects | BoxCollider added to one object | 2,000 | 60.31 | 30.16 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 33.97 | 169.83 |  |
| objects | RoomLight.Spawn | 100 | 14.99 | 149.92 |  |
| objects | switching all of the above on | 1 | 40.06 | 40057.90 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 14.77 | 14774.20 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.62 | 10.17 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.10 | 210.40 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 485.80 | 485794.80 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 490.40 | 490400.40 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 26.69 | 26688.60 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,415 ms, 3,792,773 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (8) | 0.12 | 0.04 | 0.06 | 4.26 | 13.49 | 3 / 1 / 0 / 0 | landmarks 2.7 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,069 / 3,824,185 | 2 | 197 / 902 (pool 40) |
| drive-east | 2400 (205) | 0.34 | 0.05 | 3.26 | 4.98 | 10.69 | 24 / 1 / 0 / 0 | woods 4.9 | 40 (24 / 16) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,157 / 5,460,510 | 2 | 196 / 975 (pool 40) |
| teleport-louisville | 720 (110) | 0.94 | 0.05 | 5.42 | 9.63 | 57.82 | 46 / 11 / 4 / 3 | interior plans 11.2 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.4 | 11 / 81 | 50 (16 Lite) / 2,828 / 5,058,141 | 4 | 209 / 958 (pool 40) |
| drive-louisville | 1800 (174) | 0.44 | 0.05 | 3.23 | 6.46 | 32.01 | 31 / 9 / 3 / 0 | interior plans 30.3 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 21 | 55 (12 Lite) / 3,238 / 5,898,652 | 6 | 259 / 994 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: landmarks (1x, worst 2.7 ms)
- **drive-east**: driving east from the spawn at 90 km/h
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (7x, worst 11.2 ms), buildings (1x, worst 4.8 ms), streets (1x, worst 4.1 ms), kerbside cars (1x, worst 2.2 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (6x, worst 30.3 ms), (swap) (1x, worst 9.3 ms), nature plan (1x, worst 8.2 ms), buildings (1x, worst 3.0 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 36 edit-mode artefacts

- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_RADIO & TV37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_RADIO & TV37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.597 | 0% | 2,052 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 386 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.478 | 0% | 132 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.123 | 0% | 109 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.419 | 0% | 60 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 93 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.488 | 0% | 63 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 65 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.352 | 0% | 54 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.074 | 0% | 64 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.583 | 0% | 72 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 64 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 57 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 53 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.400 | 0% | 85 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.073 | 0% | 68 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.362 | 0% | 56 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 78 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.448 | 0% | 144 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.165 | 20% | 156 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.435 | 0% | 155 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.168 | 20% | 155 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.543 | 0% | 221 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.466 | 0% | 67 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.081 | 0% | 53 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 102.7, -33.0 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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

### Everything logged during the run: 0 errors, 0 warnings, 36 edit-mode artefacts

- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_RADIO & TV37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_RADIO & TV37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 65 |  |
| square: KentuckyMap.Build | yes | 7,399 |  |
| square: count | yes | 923 |  |
| square: pick spots | yes | 123 |  |
| square: pictures | yes | 6,660 |  |
| square: discard | yes | 295 |  |
| square: passes (warm rebuild) | yes | 6,169 |  |
| chunks: stress test | yes | 1,245 |  |
| chunks: Louisville 3x3 | yes | 2,008 |  |
| chunks: determinism | yes | 163 |  |
| perf: Lite against Full | yes | 1,478 |  |
| perf: determinism across build orders | yes | 375 |  |
| perf: what things cost | yes | 2,195 |  |
| perf: walking and driving through the state | yes | 160,515 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
