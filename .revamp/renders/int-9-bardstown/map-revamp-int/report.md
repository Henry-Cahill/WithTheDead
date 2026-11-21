# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 188.7 s, started 2026-10-24 12:14:39.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 5,874 ms |
| the same square again, warm, pass by pass | 3,739 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 102.7, -33.0, facing -70° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 915 containers, 949 lights, 335 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,263 | 14,324 | 14,324 | 13,379 | 7,521,033 | 4,601,382 | 139 | 33,737 | 965 (0) | 8,381 | 9,791 | 915 |
| RaidOverhead | 588 | 470 | 470 | 470 | 475,592 | 279,990 | 68 | 1,771 | 0 (0) | 0 | 470 | 0 |
| RaidScenery | 118 | 117 | 117 | 117 | 80,444 | 104,466 | 48 | 361 | 0 (0) | 0 | 72 | 0 |
| **all** | 29,969 | 14,911 | 14,911 | 13,966 | 8,077,069 | 4,985,838 | 176 | 35,869 | 965 (0) | 8,381 | 10,333 | 915 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,478 | 13,886 | 13,886 | 12,960 | 6,280,631 | 3,959,213 | 82 | 31,325 | 927 (0) | 6,098 | 9,518 | 809 |
| World/Building shells | 131 | 130 | 130 | 130 | 802,262 | 403,344 | 51 | 1,216 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 336,637 | 167,389 | 54 | 857 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 305,479 | 179,244 | 17 | 626 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 131,020 | 57,069 | 14 | 547 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 132 | 132 | 132 | 132 | 46,171 | 27,198 | 51 | 437 | 0 (0) | 0 | 132 | 0 |
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

None: every one of the 19,298 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 71 | 2% | 1 | 71 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 3 | 0% | 1 | 3 |
| lots | 0 | 0% | 85 | 0 |
| cemeteries | 5 | 0% | 500 | 0 |
| grounds | 0 | 0% | 2 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 34 | 1% | 1 | 34 |
| road network | 5 | 0% | 1 | 5 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 23 | 1% | 189 | 1 |
| sidewalks | 24 | 1% | 67 | 3 |
| berms and driveways | 13 | 0% | 62 | 1 |
| road markings | 1 | 0% | 10 | 0 |
| paths | 2 | 0% | 72 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 103 | 3% | 43 | 5 |
| interior plans | 159 | 4% | 798 | 6 |
| interior shell | 12 | 0% | 116 | 0 |
| interior walls | 136 | 4% | 116 | 11 |
| interior floors | 13 | 0% | 116 | 0 |
| interior doors | 409 | 11% | 233 | 13 |
| interior switches | 68 | 2% | 116 | 4 |
| interior rooms | 1,918 | 51% | 697 | 33 |
| interior wall meshes | 79 | 2% | 116 | 2 |
| interior finish | 177 | 5% | 232 | 7 |
| interior merge | 234 | 6% | 467 | 9 |
| cutaway | 10 | 0% | 5 | 2 |
| utility lines | 11 | 0% | 26 | 4 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 23 | 1% | 90 | 10 |
| service drops | 3 | 0% | 251 | 0 |
| street lights | 10 | 0% | 48 | 4 |
| signs | 8 | 0% | 115 | 5 |
| street furniture | 3 | 0% | 255 | 1 |
| landmarks | 0 | 0% | 2 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 5 | 0% | 56 | 3 |
| nature plan | 2 | 0% | 1 | 2 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 6 | 0% | 9356 | 0 |
| driveway cars | 2 | 0% | 30 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 2 | 0% | 45 | 0 |
| kerbside cars | 13 | 0% | 231 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 19 | 1% | 345 | 3 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 1 | 0% | 1 | 1 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 18 | 0% | 1 | 18 |
| (emit the overhead meshes) | 9 | 0% | 1 | 9 |
| (emit the outdoor meshes) | 76 | 2% | 1 | 76 |
| **total** | **3,739** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **67** | data 25 | 0 | 136 | 65 | 137,045 | 71,996 | 1 | 121 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **27** | interior plans 2 | 1 | 145 | 64 | 133,076 | 75,702 | 4 | 218 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 164 + 10 | **63** | interior plans 8 | 1 | 210 | 53 | 127,814 | 69,435 | 0 | 162 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 402 + 26 | **52** | berms and driveways 6 | 0 | 175 | 58 | 132,494 | 70,574 | 1 | 109 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **17** | streets 3 | 1 | 63 | 42 | 54,031 | 39,943 | 0 | 145 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 24.9 | 1 | 24.9 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.6 | 1 | 0.6 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.4 | 1 | 1.4 |
| junctions | 0.9 | 1 | 0.9 |
| streets | 5.7 | 3 | 2.4 |
| sidewalks | 3.2 | 2 | 2.2 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 1.0 | 1 | 1.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.3 | 3 | 2.1 |
| interior plans | 10.7 | 5 | 2.9 |
| cutaway | 0.9 | 1 | 0.9 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 1.0 | 1 | 1.0 |
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
| kerbside cars | 0.4 | 1 | 0.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.3 | 1 | 1.3 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 0.9 | 1 | 0.9 |
| objects | 2.1 | 1 | 2.1 |
| meshes | 0.5 | 1 | 0.5 |

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
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.3 | 1 | 1.3 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 3.7 | 2 | 2.1 |
| interior plans | 9.2 | 5 | 2.3 |
| cutaway | 0.8 | 1 | 0.8 |
| utility lines | 0.0 | 1 | 0.0 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.5 | 1 | 0.5 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 2.1 | 2 | 2.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.6 | 1 | 0.6 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.3 | 1 | 1.3 |
| driveway cars | 0.3 | 1 | 0.3 |
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
| colliders | 1.1 | 1 | 1.1 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 2.3 | 1 | 2.3 |
| water | 2.8 | 2 | 2.8 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.1 | 1 | 1.1 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.3 | 1 | 1.3 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 4.3 | 3 | 2.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.5 | 4 | 3.3 |
| interior plans | 21.9 | 7 | 7.7 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.3 | 1 | 1.3 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.4 | 1 | 0.4 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 1 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.3 | 1 | 0.3 |
| kerbside cars | 1.3 | 1 | 1.3 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.1 | 2 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.3 | 1 | 1.3 |
| objects | 1.4 | 1 | 1.4 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.9 | 1 | 0.9 |
| ground | 1.0 | 1 | 1.0 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.8 | 1 | 0.8 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.1 | 1 | 1.1 |
| junctions | 1.4 | 1 | 1.4 |
| streets | 5.9 | 2 | 5.4 |
| sidewalks | 3.1 | 2 | 2.4 |
| berms and driveways | 6.8 | 2 | 5.7 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.5 | 3 | 2.6 |
| interior plans | 10.7 | 4 | 4.4 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.8 | 1 | 0.8 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.6 | 1 | 0.6 |
| street furniture | 0.6 | 1 | 0.6 |
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
| kerbside cars | 2.1 | 2 | 2.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.5 | 1 | 1.5 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.1 | 1 | 1.1 |
| colliders | 0.8 | 1 | 0.8 |
| objects | 0.8 | 1 | 0.8 |
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
| lots | 0.8 | 1 | 0.8 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 2.8 | 2 | 2.6 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.9 | 1 | 0.9 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| woods | 4.3 | 3 | 2.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.1 | 1 | 0.1 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.5 | 1 | 0.5 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.4 | 1 | 0.4 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 40 | 8 |
| 186_141 | yes | 16 | 7 |
| 185_141 | yes | 7 | 2 |
| 185_142 | yes | 9 | 2 |
| 183_148 | yes | 164 | 10 |
| 196_145 | yes | 402 | 26 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 127 | 18 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 478 ms (slowest 83 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **54** | objects 9 | 1 | 210 | 53 | 127,814 | 69,435 | 0 | 162 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **83** | buildings 28 | 1 | 201 | 75 | 147,583 | 81,115 | 0 | 202 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 127 + 18 | **60** | interior plans 10 | 1 | 249 | 62 | 131,478 | 72,891 | 3 | 197 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **71** | interior plans 27 | 1 | 159 | 57 | 121,258 | 64,363 | 0 | 152 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **45** | fences and walls 5 | 0 | 180 | 63 | 94,765 | 55,178 | 1 | 132 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **38** | interior plans 7 | 1 | 152 | 53 | 128,313 | 73,407 | 0 | 124 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **44** | meshes 5 | 1 | 209 | 53 | 128,194 | 69,563 | 0 | 210 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **32** | interior plans 4 | 0 | 147 | 56 | 96,997 | 58,360 | 4 | 90 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **50** | interior plans 7 | 1 | 189 | 45 | 210,364 | 116,952 | 0 | 159 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,698 | 517 | 517 | 512 | 1,186,766 | 661,264 | 96 | 2,148 | 8 (0) | 1,428 | 356 | 86 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (137,045 vertices, 71,996 triangles, 136 objects, 65 meshes; hash c2e77ee7931b8fa6 vs c2e77ee7931b8fa6; 122 colliders, lights and loot containers, hash 41c272f07cf29cc0 vs 41c272f07cf29cc0). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 67 | 119 / 62 / 0.8 | 0 / 0.0 | 61 / 134,179 / 0.2 | 1.3 | 64.9 | 42 / 3.2 |
| rural-5km | Full | 27 | 217 / 69 / 1.1 | 0 / 0.0 | 63 / 130,477 / 0.3 | 1.2 | 24.9 | 43 / 0.3 |
| louisville-downtown | Full | 63 | 161 / 119 / 1.3 | 0 / 0.0 | 51 / 125,203 / 0.2 | 1.6 | 60.3 | 36 / 1.0 |
| lexington-downtown | Full | 52 | 107 / 72 / 0.8 | 0 / 0.0 | 53 / 129,690 / 0.2 | 1.0 | 50.2 | 39 / 0.3 |
| hazard | Full | 17 | 144 / 15 / 0.5 | 0 / 0.0 | 37 / 50,833 / 0.3 | 0.7 | 15.4 | 30 / 0.2 |
| louisville 0,0 | Full | 54 | 161 / 119 / 1.3 | 0 / 0.0 | 51 / 125,203 / 0.2 | 9.6 | 43.6 | 36 / 0.2 |
| louisville 1,0 | Full | 83 | 201 / 108 / 2.6 | 0 / 0.0 | 73 / 144,914 / 0.2 | 2.6 | 77.4 | 46 / 0.3 |
| louisville -1,0 | Full | 60 | 193 / 140 / 1.5 | 0 / 0.0 | 52 / 128,055 / 0.2 | 1.0 | 57.8 | 40 / 0.3 |
| louisville 0,1 | Full | 71 | 151 / 83 / 1.0 | 0 / 0.0 | 54 / 118,757 / 0.4 | 1.1 | 68.7 | 36 / 0.3 |
| louisville 0,-1 | Full | 45 | 131 / 77 / 1.1 | 0 / 0.0 | 58 / 90,543 / 0.2 | 1.3 | 42.6 | 37 / 0.2 |
| louisville 1,1 | Full | 38 | 123 / 76 / 0.9 | 0 / 0.0 | 50 / 125,245 / 0.3 | 1.1 | 36.3 | 35 / 0.3 |
| louisville -1,1 | Full | 44 | 209 / 116 / 1.6 | 0 / 0.0 | 50 / 124,918 / 0.2 | 1.3 | 41.4 | 37 / 0.3 |
| louisville 1,-1 | Full | 32 | 85 / 48 / 0.7 | 0 / 0.0 | 47 / 92,677 / 0.2 | 1.0 | 30.8 | 36 / 0.3 |
| louisville -1,-1 | Full | 50 | 158 / 110 / 1.4 | 0 / 0.0 | 41 / 207,227 / 0.2 | 0.9 | 48.0 | 30 / 3.4 |

The Louisville block's NavMesh: 333 sources collected in 2 ms; baked at once (as one 384 m square) in 181 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 41 | 8 | 19% | interior plans 3.0 | sidewalks 1.3 | 137,045 / 5,902 | 136 / 36 | 65 / 31 | 121 / 1 | 1 / 0 |
| rural-5km | 26 | 3 | 13% | interior plans 2.3 | buildings 0.7 | 132,918 / 4,492 | 142 / 38 | 64 / 33 | 213 / 1 | 4 / 0 |
| louisville-downtown | 46 | 7 | 16% | interior plans 4.2 | buildings 2.4 | 119,976 / 6,939 | 212 / 33 | 53 / 28 | 160 / 1 | 0 / 0 |
| lexington-downtown | 40 | 5 | 13% | streets 5.3 | buildings 0.9 | 127,721 / 4,186 | 177 / 33 | 59 / 28 | 111 / 1 | 2 / 0 |
| hazard | 22 | 4 | 16% | ground mask 4.8 | woods 0.8 | 61,978 / 4,435 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 59.21 | 29.61 | make 51.42 + switch on 7.79 ms; 1602 objects; destroy 3.24 ms; 3,000 rays 2.60 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 15.71 | 7.86 | make 10.27 + switch on 5.44 ms; 10 objects; destroy 1.51 ms; 3,000 rays 2.10 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 12.78 | 6.39 | make 4.02 + switch on 8.76 ms; 2 objects; destroy 0.12 ms; 3,000 rays 2.00 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.17 | 1.08 | make 2.13 + switch on 0.04 ms (+ 7.91 ms baking on the worker); 2 objects; destroy 0.08 ms; 3,000 rays 2.21 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 9.05 | 226.32 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.79 | 194.79 | objects 0.83, allocate 0.02, pack 6.82, apply 0.13 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.00 | 25.10 | objects 0.86, allocate 0.02, pack 6.75 (worker), apply 0.12 ms |
| primitives | Box, turned, flat colour | 20,000 | 25.31 | 1.27 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 83.26 | 4.16 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 59.95 | 3.00 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 82.02 | 4.10 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 40.38 | 2.02 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 26.59 | 1.33 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.59 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.81 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 15.44 | 3.09 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 28.76 | 5.75 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 20.70 | 6.90 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 63.45 | 21.15 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.52 | 6.76 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.06 | 11.06 |  |
| objects | BoxCollider added to one object | 2,000 | 39.51 | 19.76 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 23.58 | 117.90 |  |
| objects | RoomLight.Spawn | 100 | 10.43 | 104.28 |  |
| objects | switching all of the above on | 1 | 28.46 | 28454.70 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 11.70 | 11702.60 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.59 | 9.73 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.02 | 202.08 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 398.51 | 398509.30 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 381.83 | 381828.60 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 24.13 | 24133.90 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,161 ms, 3,822,214 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (8) | 0.12 | 0.05 | 0.06 | 3.80 | 13.29 | 2 / 1 / 0 / 0 | landmarks 3.1 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,069 / 3,853,674 | 2 | 199 / 930 (pool 40) |
| drive-east | 2400 (202) | 0.34 | 0.05 | 3.31 | 4.99 | 8.26 | 24 / 1 / 0 / 0 | buildings 8.2 | 40 (24 / 16) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,157 / 5,495,345 | 2 | 198 / 1,003 (pool 40) |
| teleport-louisville | 720 (107) | 0.89 | 0.05 | 6.24 | 9.62 | 36.60 | 50 / 13 / 3 / 2 | interior plans 10.1 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.3 | 9 / 77 | 50 (16 Lite) / 2,828 / 5,102,538 | 4 | 214 / 987 (pool 40) |
| drive-louisville | 1800 (159) | 0.40 | 0.05 | 3.20 | 6.27 | 27.33 | 32 / 10 / 4 / 0 | interior plans 27.3 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 18 | 55 (12 Lite) / 3,238 / 5,953,998 | 6 | 264 / 1,023 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: landmarks (1x, worst 3.1 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: buildings (1x, worst 8.2 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (9x, worst 10.1 ms), junctions (1x, worst 3.3 ms), sidewalks (1x, worst 3.1 ms), objects (1x, worst 2.5 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (6x, worst 27.3 ms), cutaway (1x, worst 8.8 ms), buildings (1x, worst 8.1 ms), ground (1x, worst 7.8 ms), streets (1x, worst 3.2 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 8 edit-mode artefacts

- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEFFERSON COUNTY SHERIFF0A0D16373936: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEFFERSON COUNTY SHERIFF0A0D16373936: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.597 | 0% | 1,695 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 322 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.484 | 0% | 128 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.124 | 0% | 96 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.420 | 0% | 50 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 61 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.489 | 0% | 57 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 62 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.352 | 0% | 51 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.073 | 0% | 65 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.583 | 0% | 55 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 55 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 55 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 56 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.399 | 0% | 62 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.073 | 0% | 58 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.361 | 0% | 55 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 63 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.454 | 0% | 134 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.164 | 20% | 122 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.433 | 0% | 112 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.166 | 20% | 111 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.542 | 0% | 121 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.464 | 0% | 53 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.080 | 0% | 54 |

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

### Everything logged during the run: 0 errors, 0 warnings, 8 edit-mode artefacts

- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEFFERSON COUNTY SHERIFF0A0D16373936: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEFFERSON COUNTY SHERIFF0A0D16373936: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 66 |  |
| square: KentuckyMap.Build | yes | 5,876 |  |
| square: count | yes | 862 |  |
| square: pick spots | yes | 65 |  |
| square: pictures | yes | 5,692 |  |
| square: discard | yes | 224 |  |
| square: passes (warm rebuild) | yes | 3,965 |  |
| chunks: stress test | yes | 1,012 |  |
| chunks: Louisville 3x3 | yes | 1,604 |  |
| chunks: determinism | yes | 146 |  |
| perf: Lite against Full | yes | 1,130 |  |
| perf: determinism across build orders | yes | 297 |  |
| perf: what things cost | yes | 1,661 |  |
| perf: walking and driving through the state | yes | 166,084 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
