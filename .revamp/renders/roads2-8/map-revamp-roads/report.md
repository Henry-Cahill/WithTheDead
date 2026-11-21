# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 182.6 s, started 2026-10-24 12:45:40.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,010 ms |
| the same square again, warm, pass by pass | 4,008 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 102.7, -33.0, facing -70° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 915 containers, 949 lights, 335 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,266 | 14,324 | 14,324 | 13,379 | 7,522,688 | 4,602,371 | 139 | 33,737 | 965 (0) | 8,382 | 9,791 | 915 |
| RaidOverhead | 588 | 470 | 470 | 470 | 475,592 | 279,990 | 68 | 1,771 | 0 (0) | 0 | 470 | 0 |
| RaidScenery | 118 | 117 | 117 | 117 | 80,444 | 104,466 | 48 | 361 | 0 (0) | 0 | 72 | 0 |
| **all** | 29,972 | 14,911 | 14,911 | 13,966 | 8,078,724 | 4,986,827 | 176 | 35,869 | 965 (0) | 8,382 | 10,333 | 915 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,478 | 13,886 | 13,886 | 12,960 | 6,280,631 | 3,959,213 | 82 | 31,325 | 927 (0) | 6,098 | 9,518 | 809 |
| World/Building shells | 131 | 130 | 130 | 130 | 802,262 | 403,344 | 51 | 1,216 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 336,637 | 167,389 | 54 | 857 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 305,743 | 179,484 | 17 | 626 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 132,411 | 57,818 | 14 | 547 | 0 (0) | 0 | 0 | 0 |
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

None: every one of the 19,299 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 63 | 2% | 1 | 63 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 3 | 0% | 1 | 3 |
| lots | 0 | 0% | 85 | 0 |
| cemeteries | 5 | 0% | 500 | 0 |
| grounds | 0 | 0% | 2 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 46 | 1% | 1 | 46 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 22 | 1% | 189 | 1 |
| sidewalks | 25 | 1% | 67 | 4 |
| berms and driveways | 15 | 0% | 63 | 1 |
| road markings | 1 | 0% | 10 | 0 |
| paths | 2 | 0% | 72 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 108 | 3% | 43 | 6 |
| interior plans | 160 | 4% | 798 | 5 |
| interior shell | 12 | 0% | 116 | 0 |
| interior walls | 134 | 3% | 116 | 9 |
| interior floors | 13 | 0% | 116 | 0 |
| interior doors | 480 | 12% | 233 | 29 |
| interior switches | 66 | 2% | 116 | 1 |
| interior rooms | 2,101 | 52% | 697 | 32 |
| interior wall meshes | 86 | 2% | 116 | 3 |
| interior finish | 200 | 5% | 232 | 16 |
| interior merge | 239 | 6% | 467 | 13 |
| cutaway | 9 | 0% | 5 | 2 |
| utility lines | 3 | 0% | 24 | 1 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 90 | 3 |
| service drops | 1 | 0% | 251 | 0 |
| street lights | 3 | 0% | 48 | 0 |
| signs | 1 | 0% | 115 | 0 |
| street furniture | 14 | 0% | 255 | 8 |
| landmarks | 0 | 0% | 2 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 6 | 0% | 56 | 4 |
| nature plan | 2 | 0% | 1 | 2 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 2 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 6 | 0% | 9356 | 0 |
| driveway cars | 2 | 0% | 30 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 45 | 0 |
| kerbside cars | 13 | 0% | 232 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 14 | 0% | 345 | 0 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 12 | 0% | 1 | 12 |
| (emit the overhead meshes) | 10 | 0% | 1 | 10 |
| (emit the outdoor meshes) | 91 | 2% | 1 | 91 |
| **total** | **4,008** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **67** | objects 23 | 1 | 137 | 65 | 139,172 | 73,107 | 1 | 121 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **28** | interior plans 2 | 1 | 145 | 64 | 133,076 | 75,702 | 4 | 218 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 148 + 10 | **103** | colliders 20 | 7 | 210 | 53 | 127,592 | 69,377 | 0 | 162 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 480 + 27 | **55** | paths 7 | 0 | 175 | 58 | 131,904 | 70,388 | 1 | 109 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **17** | streets 3 | 0 | 63 | 42 | 53,923 | 39,907 | 0 | 145 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.7 | 1 | 1.7 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.6 | 1 | 0.6 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.4 | 1 | 1.4 |
| junctions | 0.9 | 1 | 0.9 |
| streets | 5.1 | 3 | 2.2 |
| sidewalks | 3.9 | 2 | 2.1 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 1.0 | 1 | 1.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.1 | 3 | 2.2 |
| interior plans | 11.1 | 5 | 3.2 |
| cutaway | 0.9 | 1 | 0.9 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.1 | 1 | 0.1 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.7 | 1 | 0.7 |
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
| wall drifts | 1.3 | 1 | 1.3 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 1.0 | 1 | 1.0 |
| objects | 23.0 | 1 | 23.0 |
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
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.3 | 1 | 1.3 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 3.7 | 2 | 2.0 |
| interior plans | 9.8 | 5 | 2.4 |
| cutaway | 0.8 | 1 | 0.8 |
| utility lines | 0.0 | 1 | 0.0 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.6 | 1 | 0.6 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 2.2 | 2 | 2.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.6 | 1 | 0.6 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.4 | 1 | 1.4 |
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
| colliders | 1.2 | 1 | 1.2 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 2.1 | 1 | 2.1 |
| water | 2.8 | 2 | 2.8 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.1 | 1 | 1.1 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.8 | 1 | 0.8 |
| sidewalks | 1.3 | 1 | 1.3 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 4.9 | 3 | 2.5 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 9.3 | 4 | 3.4 |
| interior plans | 20.7 | 8 | 4.4 |
| cutaway | 1.0 | 1 | 1.0 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.1 | 1 | 0.1 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.9 | 1 | 1.9 |
| wires | 0.2 | 1 | 0.2 |
| nature plan | 0.5 | 1 | 0.5 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.9 | 1 | 0.9 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 1 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.5 | 1 | 0.5 |
| kerbside cars | 22.1 | 3 | 18.5 |
| wrecks | 0.2 | 1 | 0.2 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.4 | 2 | 2.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 21.1 | 2 | 19.8 |
| objects | 2.0 | 1 | 2.0 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.0 | 1 | 1.0 |
| ground | 1.1 | 1 | 1.1 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.8 | 1 | 0.8 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.2 | 1 | 1.2 |
| junctions | 1.5 | 1 | 1.5 |
| streets | 6.1 | 2 | 5.4 |
| sidewalks | 2.9 | 2 | 2.4 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 7.3 | 1 | 7.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.8 | 3 | 2.9 |
| interior plans | 11.3 | 4 | 4.5 |
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
| kerbside cars | 2.1 | 2 | 2.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.5 | 1 | 1.5 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.0 | 1 | 1.0 |
| colliders | 0.8 | 1 | 0.8 |
| objects | 1.0 | 1 | 1.0 |
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
| streets | 2.9 | 2 | 2.7 |
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
| signs | 1.0 | 1 | 1.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.7 | 1 | 0.7 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| woods | 4.4 | 3 | 2.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.1 | 1 | 0.1 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.6 | 1 | 0.6 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.4 | 1 | 0.4 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 40 | 8 |
| 186_141 | yes | 11 | 9 |
| 185_141 | yes | 26 | 2 |
| 185_142 | yes | 10 | 2 |
| 183_148 | yes | 148 | 10 |
| 196_145 | yes | 480 | 27 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 148 | 19 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 466 ms (slowest 93 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **45** | buildings 4 | 1 | 210 | 53 | 127,592 | 69,377 | 0 | 162 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **52** | interior plans 8 | 1 | 201 | 75 | 147,721 | 81,329 | 0 | 202 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 148 + 19 | **68** | interior plans 9 | 1 | 249 | 62 | 131,032 | 72,745 | 3 | 197 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **93** | buses and cruisers 30 | 0 | 159 | 57 | 120,904 | 64,271 | 0 | 152 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **44** | fences and walls 5 | 1 | 180 | 63 | 94,285 | 55,022 | 1 | 132 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **39** | interior plans 7 | 0 | 154 | 54 | 128,779 | 73,793 | 1 | 124 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **39** | interior plans 5 | 1 | 209 | 53 | 127,992 | 69,499 | 0 | 210 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **33** | interior plans 4 | 0 | 147 | 56 | 96,693 | 58,262 | 4 | 90 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **54** | interior plans 5 | 1 | 189 | 45 | 209,914 | 116,808 | 0 | 159 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,700 | 518 | 518 | 512 | 1,184,912 | 661,106 | 96 | 2,148 | 9 (0) | 1,428 | 356 | 86 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (139,172 vertices, 73,107 triangles, 137 objects, 65 meshes; hash 6b5bb5517749fb44 vs 6b5bb5517749fb44; 122 colliders, lights and loot containers, hash 4a5b6e3702bd270e vs 4a5b6e3702bd270e). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 67 | 119 / 63 / 0.8 | 0 / 0.0 | 61 / 136,306 / 0.2 | 22.5 | 43.2 | 42 / 3.5 |
| rural-5km | Full | 28 | 217 / 69 / 1.2 | 0 / 0.0 | 63 / 130,477 / 0.3 | 1.2 | 25.6 | 43 / 0.3 |
| louisville-downtown | Full | 103 | 161 / 119 / 21.1 | 0 / 0.0 | 51 / 124,981 / 0.4 | 2.4 | 79.5 | 36 / 0.9 |
| lexington-downtown | Full | 55 | 107 / 72 / 0.8 | 0 / 0.0 | 53 / 129,100 / 0.2 | 1.2 | 53.1 | 39 / 0.3 |
| hazard | Full | 17 | 144 / 15 / 0.6 | 0 / 0.0 | 37 / 50,725 / 0.3 | 0.8 | 15.8 | 30 / 0.2 |
| louisville 0,0 | Full | 45 | 161 / 119 / 1.3 | 0 / 0.0 | 51 / 124,981 / 0.2 | 1.1 | 42.7 | 36 / 0.3 |
| louisville 1,0 | Full | 52 | 201 / 108 / 1.4 | 0 / 0.0 | 73 / 145,052 / 0.3 | 1.6 | 49.1 | 46 / 0.4 |
| louisville -1,0 | Full | 68 | 193 / 140 / 1.6 | 0 / 0.0 | 52 / 127,609 / 0.2 | 1.1 | 64.9 | 40 / 0.4 |
| louisville 0,1 | Full | 93 | 151 / 83 / 1.0 | 0 / 0.0 | 54 / 118,403 / 0.2 | 1.2 | 90.7 | 36 / 0.2 |
| louisville 0,-1 | Full | 44 | 131 / 77 / 1.0 | 0 / 0.0 | 58 / 90,063 / 0.2 | 1.2 | 42.2 | 37 / 0.3 |
| louisville 1,1 | Full | 39 | 122 / 76 / 0.9 | 0 / 0.0 | 50 / 125,645 / 0.2 | 1.0 | 36.8 | 36 / 0.2 |
| louisville -1,1 | Full | 39 | 209 / 116 / 1.4 | 0 / 0.0 | 50 / 124,716 / 0.2 | 1.0 | 36.5 | 37 / 0.3 |
| louisville 1,-1 | Full | 33 | 85 / 48 / 0.6 | 0 / 0.0 | 47 / 92,373 / 0.2 | 1.0 | 31.0 | 36 / 0.2 |
| louisville -1,-1 | Full | 54 | 158 / 110 / 1.4 | 0 / 0.0 | 41 / 206,777 / 0.2 | 0.8 | 51.4 | 30 / 0.3 |

The Louisville block's NavMesh: 334 sources collected in 2 ms; baked at once (as one 384 m square) in 190 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 45 | 8 | 18% | interior plans 3.0 | sidewalks 1.7 | 139,172 / 5,891 | 137 / 36 | 65 / 31 | 121 / 1 | 1 / 0 |
| rural-5km | 26 | 4 | 14% | interior plans 2.4 | objects 0.7 | 132,918 / 4,492 | 142 / 38 | 64 / 33 | 213 / 1 | 4 / 0 |
| louisville-downtown | 45 | 7 | 16% | interior plans 4.3 | buildings 2.4 | 119,754 / 6,939 | 212 / 33 | 53 / 28 | 160 / 1 | 0 / 0 |
| lexington-downtown | 40 | 5 | 13% | streets 5.1 | sidewalks 0.9 | 127,131 / 4,186 | 177 / 33 | 59 / 28 | 111 / 1 | 2 / 0 |
| hazard | 23 | 4 | 17% | streets 2.8 | woods 0.8 | 61,870 / 4,435 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 64.85 | 32.42 | make 56.29 + switch on 8.56 ms; 1602 objects; destroy 4.20 ms; 3,000 rays 2.88 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 15.22 | 7.61 | make 10.60 + switch on 4.62 ms; 10 objects; destroy 2.03 ms; 3,000 rays 1.95 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.81 | 5.90 | make 3.75 + switch on 8.06 ms; 2 objects; destroy 0.10 ms; 3,000 rays 2.03 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.48 | 1.24 | make 2.44 + switch on 0.04 ms (+ 8.63 ms baking on the worker); 2 objects; destroy 0.10 ms; 3,000 rays 2.51 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.62 | 215.45 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.44 | 185.98 | objects 0.84, allocate 0.02, pack 6.49, apply 0.09 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.62 | 40.48 | objects 1.49, allocate 0.01, pack 6.71 (worker), apply 0.12 ms |
| primitives | Box, turned, flat colour | 20,000 | 27.23 | 1.36 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 86.64 | 4.33 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 58.50 | 2.93 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 84.66 | 4.23 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 29.03 | 1.45 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 24.24 | 1.21 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.58 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.82 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 39.65 | 7.93 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 30.02 | 6.01 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 21.23 | 7.08 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 38.20 | 12.74 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.26 | 6.63 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.48 | 11.48 |  |
| objects | BoxCollider added to one object | 2,000 | 39.65 | 19.82 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 23.61 | 118.06 |  |
| objects | RoomLight.Spawn | 100 | 9.73 | 97.31 |  |
| objects | switching all of the above on | 1 | 27.51 | 27507.60 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 11.91 | 11909.90 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.60 | 9.86 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.01 | 200.68 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 401.05 | 401050.50 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 368.51 | 368511.30 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 21.24 | 21243.90 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,241 ms, 3,822,687 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (8) | 0.12 | 0.04 | 0.05 | 4.02 | 14.54 | 1 / 1 / 0 / 0 | landmarks 3.4 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,069 / 3,854,147 | 2 | 200 / 932 (pool 40) |
| drive-east | 2400 (207) | 0.34 | 0.05 | 3.19 | 4.84 | 9.48 | 17 / 3 / 0 / 0 | interior plans 9.1 | 40 (24 / 16) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,157 / 5,495,114 | 2 | 196 / 1,005 (pool 40) |
| teleport-louisville | 720 (103) | 0.88 | 0.05 | 6.17 | 8.51 | 37.58 | 50 / 13 / 3 / 3 | interior plans 10.0 | 25 (9 / 16) | 0 | 30 | 0.1 | 1.2 | 8 / 70 | 50 (16 Lite) / 2,828 / 5,101,115 | 4 | 210 / 988 (pool 40) |
| drive-louisville | 1800 (155) | 0.40 | 0.05 | 3.35 | 6.04 | 27.14 | 29 / 8 / 3 / 0 | interior plans 27.1 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 17 | 55 (12 Lite) / 3,238 / 5,948,577 | 6 | 260 / 1,024 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: landmarks (1x, worst 3.4 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: interior plans (2x, worst 9.1 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (8x, worst 10.0 ms), mapped trees (1x, worst 5.1 ms), sidewalks (1x, worst 2.5 ms), wall drifts (1x, worst 2.0 ms), junctions (1x, worst 1.7 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (5x, worst 27.1 ms), buildings (1x, worst 8.2 ms), utility lines (1x, worst 7.4 ms), road network (1x, worst 6.2 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.597 | 0% | 1,683 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 277 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.483 | 0% | 117 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.124 | 0% | 91 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.420 | 0% | 50 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 58 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.489 | 0% | 65 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 60 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.352 | 0% | 55 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.074 | 0% | 54 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.583 | 0% | 53 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 54 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 49 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 62 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.398 | 0% | 57 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.073 | 0% | 50 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.361 | 0% | 57 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 57 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.453 | 0% | 108 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.165 | 20% | 113 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.433 | 0% | 114 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.166 | 20% | 98 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.542 | 0% | 208 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.463 | 0% | 57 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.081 | 0% | 50 |

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
| setup | yes | 53 |  |
| square: KentuckyMap.Build | yes | 6,012 |  |
| square: count | yes | 855 |  |
| square: pick spots | yes | 65 |  |
| square: pictures | yes | 5,551 |  |
| square: discard | yes | 223 |  |
| square: passes (warm rebuild) | yes | 4,218 |  |
| chunks: stress test | yes | 1,154 |  |
| chunks: Louisville 3x3 | yes | 1,714 |  |
| chunks: determinism | yes | 148 |  |
| perf: Lite against Full | yes | 1,134 |  |
| perf: determinism across build orders | yes | 302 |  |
| perf: what things cost | yes | 1,649 |  |
| perf: walking and driving through the state | yes | 159,507 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
