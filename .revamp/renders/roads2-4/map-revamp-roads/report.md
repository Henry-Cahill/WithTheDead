# CLONE-Z map harness: Lexington (build ky-local-f2)

**Exit 0**: ok. Full run, 189.0 s, started 2026-10-24 11:23:15.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Lexington plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 7,040 ms |
| the same square again, warm, pass by pass | 3,690 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 27.2, 28.2, facing -134° |
| exits | US 25 north at -179.0, 197.0; East Short Street at 197.0, -72.3; US 25 south at 175.6, -197.0; US 60 south at -3.7, -197.0; West High Street at -197.0, -91.2; US 421 west at -197.0, 56.2 |
| overhead renderers (hidden in the iso view) | 130 |
| `KentuckyMap.LastStats` | Kentucky map Lexington (build ky-local-f2): 85 buildings, 70 beyond the edge, 642 street pieces, 808 containers, 640 lights, 47 trees, 6 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 85 with their middle inside the square (12 of them reach within 1 m of its edge or past it), 70 in the scenery ring; inside: commercial 68, house 11, apartments 2, warehouse 2, canopy 1, retail 1 |
| map data: streets, lines, areas | 645 street pieces, 0 lines (), 67 land-use areas (parking 30, grass 25, park 9, commercial 2, plaza 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 26,363 | 13,307 | 13,307 | 12,665 | 6,865,605 | 4,097,102 | 124 | 31,345 | 641 (0) | 7,226 | 9,727 | 808 |
| RaidOverhead | 436 | 352 | 352 | 352 | 380,058 | 225,325 | 138 | 1,472 | 0 (0) | 0 | 352 | 0 |
| RaidScenery | 116 | 115 | 115 | 115 | 73,325 | 91,570 | 40 | 355 | 0 (0) | 0 | 66 | 0 |
| **all** | 26,915 | 13,774 | 13,774 | 13,132 | 7,318,988 | 4,413,997 | 245 | 33,172 | 641 (0) | 7,226 | 10,145 | 808 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 24,503 | 12,898 | 12,898 | 12,283 | 5,862,052 | 3,587,572 | 79 | 29,145 | 616 (0) | 5,433 | 9,491 | 668 |
| World/Building shells | 96 | 95 | 95 | 95 | 562,824 | 285,468 | 36 | 792 | 0 (0) | 0 | 95 | 0 |
| RaidOverhead/Building shells | 96 | 95 | 95 | 95 | 252,066 | 130,247 | 120 | 789 | 0 (0) | 0 | 95 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 176,465 | 73,924 | 15 | 742 | 0 (0) | 0 | 0 | 0 |
| World/OutdoorProps | 141 | 141 | 141 | 141 | 261,952 | 146,566 | 19 | 635 | 0 (0) | 0 | 141 | 0 |
| RaidOverhead/Roofs | 99 | 99 | 99 | 99 | 38,225 | 23,843 | 38 | 321 | 0 (0) | 0 | 99 | 0 |
| RaidOverhead/Interior KY# | 164 | 82 | 82 | 82 | 85,726 | 65,720 | 5 | 286 | 0 (0) | 0 | 82 | 0 |
| RaidScenery/SceneryGround | 47 | 47 | 47 | 47 | 11,646 | 3,902 | 6 | 171 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SceneryProps | 40 | 40 | 40 | 40 | 19,858 | 10,782 | 33 | 156 | 0 (0) | 0 | 40 | 0 |
| RaidOverhead/Overhead | 48 | 48 | 48 | 48 | 1,101 | 367 | 1 | 48 | 0 (0) | 0 | 48 | 0 |
| World/StreetLamp | 60 | 30 | 30 | 3 | 2,288 | 3,560 | 3 | 30 | 24 (0) | 30 | 0 | 0 |
| RaidOverhead/Wires | 28 | 28 | 28 | 28 | 2,940 | 5,148 | 1 | 28 | 0 (0) | 0 | 28 | 0 |
| RaidScenery/Wires | 26 | 26 | 26 | 26 | 2,682 | 4,716 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 39,135 | 72,168 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 17,586 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 557 | 15% | 1 | 557 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 2 | 0% | 1 | 2 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 5 | 0% | 1 | 5 |
| lots | 5 | 0% | 48 | 3 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 0 | 0% | 6 | 0 |
| snow tone | 20 | 1% | 1 | 20 |
| road network | 8 | 0% | 1 | 8 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 38 | 1% | 260 | 3 |
| sidewalks | 32 | 1% | 94 | 4 |
| berms and driveways | 14 | 0% | 85 | 1 |
| road markings | 2 | 0% | 28 | 1 |
| paths | 9 | 0% | 258 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 7 | 0% | 1 | 7 |
| buildings | 71 | 2% | 31 | 5 |
| interior plans | 149 | 4% | 83 | 10 |
| interior shell | 8 | 0% | 82 | 0 |
| interior walls | 149 | 4% | 82 | 37 |
| interior floors | 9 | 0% | 82 | 0 |
| interior doors | 369 | 10% | 186 | 30 |
| interior switches | 42 | 1% | 82 | 2 |
| interior rooms | 1,797 | 49% | 536 | 29 |
| interior wall meshes | 61 | 2% | 82 | 4 |
| interior finish | 117 | 3% | 82 | 8 |
| cutaway | 10 | 0% | 4 | 4 |
| utility lines | 4 | 0% | 1 | 4 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 6 | 0% | 1 | 6 |
| street lights | 11 | 0% | 1 | 11 |
| signs | 7 | 0% | 1 | 7 |
| street furniture | 2 | 0% | 1 | 2 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 4 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 5 | 0% | 1 | 5 |
| nature plan | 2 | 0% | 1 | 2 |
| driveway plan | 0 | 0% | 1 | 0 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 1 | 0% | 31 | 0 |
| grounds trees | 0 | 0% | 2 | 0 |
| street trees | 0 | 0% | 11 | 0 |
| fence rows | 0 | 0% | 111 | 0 |
| woods | 1 | 0% | 9357 | 0 |
| driveway cars | 0 | 0% | 2 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 12 | 0% | 175 | 1 |
| kerbside cars | 14 | 0% | 259 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 1 | 0% | 1 | 1 |
| wall drifts | 32 | 1% | 85 | 4 |
| fence drifts | 0 | 0% | 4 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 5 | 0% | 1 | 5 |
| (after the last pass) | 13 | 0% | 1 | 13 |
| (emit the overhead meshes) | 13 | 0% | 1 | 13 |
| (emit the outdoor meshes) | 69 | 2% | 1 | 69 |
| **total** | **3,690** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Lexington | 12577_9303 (196_145) | 0 + 0 | **56** | objects 14 | 1 | 137 | 57 | 119,517 | 63,040 | 1 | 94 | 6 | 43/6/6 | 0E 0W |
| rural-5km | 5 km from Lexington (bearing 40°) | 12602_9333 (196_145) | 0 + 0 | **41** | interior plans 4 | 1 | 158 | 70 | 124,739 | 70,425 | 3 | 218 | 9 | 7/9/3 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 119 + 8 | **53** | interior plans 6 | 1 | 198 | 53 | 136,728 | 73,131 | 0 | 164 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 0 + 0 | **38** | streets 5 | 0 | 137 | 57 | 119,517 | 63,040 | 1 | 94 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **13** | streets 3 | 0 | 58 | 42 | 39,797 | 28,704 | 0 | 73 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.7 | 1 | 1.7 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.5 | 1 | 0.5 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.9 | 1 | 0.9 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 5.7 | 2 | 5.3 |
| sidewalks | 3.7 | 2 | 2.0 |
| berms and driveways | 1.8 | 1 | 1.8 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 4.5 | 2 | 3.0 |
| interior plans | 11.6 | 4 | 5.6 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.7 | 1 | 0.7 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.4 | 1 | 0.4 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 1.0 | 1 | 1.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.8 | 1 | 1.8 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 14.0 | 1 | 14.0 |
| meshes | 0.7 | 1 | 0.7 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.3 | 1 | 0.3 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.2 | 1 | 1.2 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.3 | 1 | 0.3 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.8 | 1 | 0.8 |
| sidewalks | 1.3 | 1 | 1.3 |
| berms and driveways | 0.8 | 1 | 0.8 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 8.2 | 4 | 2.4 |
| interior plans | 12.5 | 5 | 4.0 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.2 | 1 | 1.2 |
| street lights | 0.5 | 1 | 0.5 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.3 | 1 | 0.3 |
| mapped trees | 0.2 | 1 | 0.2 |
| yard trees | 2.2 | 2 | 2.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.2 | 1 | 0.2 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.6 | 4 | 0.8 |
| driveway cars | 0.9 | 1 | 0.9 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.3 | 1 | 0.3 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.8 | 1 | 0.8 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.1 | 1 | 0.1 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.2 | 1 | 1.2 |
| objects | 1.2 | 1 | 1.2 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 1.5 | 1 | 1.5 |
| water | 2.7 | 2 | 2.7 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.8 | 1 | 0.8 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 4.2 | 3 | 2.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 9.3 | 4 | 3.1 |
| interior plans | 19.1 | 6 | 6.4 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.8 | 1 | 0.8 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.3 | 1 | 0.3 |
| kerbside cars | 1.0 | 1 | 1.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.2 | 2 | 2.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.4 | 1 | 1.4 |
| objects | 1.3 | 1 | 1.3 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.5 | 1 | 0.5 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 5.7 | 2 | 5.3 |
| sidewalks | 2.8 | 2 | 2.2 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 3.5 | 2 | 2.1 |
| interior plans | 11.5 | 5 | 3.5 |
| cutaway | 0.6 | 1 | 0.6 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.8 | 1 | 0.8 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.4 | 1 | 0.4 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 1.1 | 1 | 1.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.8 | 1 | 1.8 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.1 | 1 | 0.1 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.1 | 1 | 1.1 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 2.8 | 2 | 2.7 |
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
| fence rows | 1.0 | 1 | 1.0 |
| woods | 1.7 | 4 | 1.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.1 | 1 | 0.1 |
| kerbside cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.3 | 1 | 0.3 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.1 | 1 | 0.1 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 196_145 | yes | 396 | 27 |
| 197_145 | yes | 83 | 7 |
| 196_144 | yes | 299 | 22 |
| 195_145 | yes | 87 | 6 |
| 183_148 | yes | 119 | 8 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 132 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 482 ms (slowest 99 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **55** | snow tone 8 | 1 | 198 | 53 | 136,728 | 73,131 | 0 | 164 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **99** | buildings 26 | 1 | 195 | 75 | 146,703 | 80,889 | 0 | 203 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 132 + 8 | **50** | interior plans 9 | 1 | 252 | 62 | 112,060 | 63,031 | 3 | 195 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **38** | interior plans 3 | 0 | 153 | 58 | 125,745 | 66,451 | 0 | 149 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **47** | fences and walls 6 | 0 | 174 | 63 | 92,485 | 54,477 | 1 | 127 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **39** | interior plans 9 | 0 | 162 | 53 | 112,623 | 65,192 | 0 | 123 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **62** | buildings 23 | 1 | 202 | 53 | 120,150 | 65,369 | 0 | 206 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **41** | interior plans 7 | 0 | 159 | 56 | 100,285 | 59,401 | 4 | 91 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **51** | interior plans 9 | 1 | 200 | 46 | 191,300 | 106,301 | 0 | 160 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,697 | 519 | 519 | 514 | 1,138,079 | 634,242 | 134 | 2,220 | 8 (0) | 1,418 | 358 | 86 |

### Determinism

Chunk 12577_9303 (start-centre) built twice: **match**: identical, in the same order (119,517 vertices, 63,040 triangles, 137 objects, 57 meshes; hash aebc59dd8f5ef6b8 vs aebc59dd8f5ef6b8; 101 colliders, lights and loot containers, hash 3e73792a647af194 vs 3e73792a647af194). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 56 | 92 / 55 / 0.7 | 0 / 0.0 | 53 / 116,788 / 0.4 | 13.6 | 42.1 | 39 / 3.2 |
| rural-5km | Full | 41 | 216 / 62 / 1.2 | 0 / 0.0 | 66 / 121,684 / 0.2 | 1.6 | 37.9 | 42 / 0.6 |
| louisville-downtown | Full | 53 | 163 / 117 / 1.4 | 0 / 0.0 | 51 / 134,117 / 0.2 | 1.6 | 49.7 | 36 / 0.2 |
| lexington-downtown | Full | 38 | 92 / 55 / 0.7 | 0 / 0.0 | 53 / 116,788 / 0.2 | 1.0 | 36.1 | 39 / 0.2 |
| hazard | Full | 13 | 72 / 10 / 0.3 | 0 / 0.0 | 37 / 36,599 / 0.1 | 0.7 | 12.5 | 30 / 0.2 |
| louisville 0,0 | Full | 55 | 163 / 117 / 3.3 | 0 / 0.0 | 51 / 134,117 / 0.2 | 1.0 | 50.5 | 36 / 0.3 |
| louisville 1,0 | Full | 99 | 202 / 107 / 1.8 | 0 / 0.0 | 73 / 144,034 / 0.3 | 1.6 | 95.8 | 46 / 0.8 |
| louisville -1,0 | Full | 50 | 191 / 143 / 1.6 | 0 / 0.0 | 52 / 108,637 / 0.2 | 1.0 | 47.1 | 40 / 0.3 |
| louisville 0,1 | Full | 38 | 148 / 81 / 1.0 | 0 / 0.0 | 54 / 123,235 / 0.3 | 1.1 | 36.3 | 36 / 0.3 |
| louisville 0,-1 | Full | 47 | 126 / 71 / 0.9 | 0 / 0.0 | 58 / 88,236 / 0.2 | 1.2 | 44.9 | 37 / 0.2 |
| louisville 1,1 | Full | 39 | 122 / 76 / 0.9 | 0 / 0.0 | 50 / 109,552 / 0.2 | 1.0 | 37.3 | 35 / 0.2 |
| louisville -1,1 | Full | 62 | 205 / 114 / 1.3 | 0 / 0.0 | 50 / 116,865 / 0.2 | 1.1 | 59.3 | 37 / 0.3 |
| louisville 1,-1 | Full | 41 | 86 / 55 / 0.7 | 0 / 0.0 | 47 / 95,965 / 0.2 | 1.0 | 39.3 | 36 / 0.3 |
| louisville -1,-1 | Full | 51 | 159 / 110 / 1.4 | 0 / 0.0 | 42 / 188,163 / 0.2 | 0.8 | 48.9 | 30 / 0.3 |

The Louisville block's NavMesh: 333 sources collected in 2 ms; baked at once (as one 384 m square) in 148 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 39 | 6 | 16% | interior plans 5.5 | sidewalks 0.9 | 119,517 / 4,078 | 137 / 33 | 57 / 28 | 94 / 1 | 1 / 0 |
| rural-5km | 25 | 4 | 15% | interior plans 2.7 | buildings 0.8 | 126,411 / 5,171 | 156 / 35 | 70 / 30 | 220 / 1 | 3 / 0 |
| louisville-downtown | 44 | 7 | 15% | interior plans 6.0 | buildings 2.4 | 129,293 / 7,103 | 214 / 33 | 53 / 28 | 164 / 1 | 0 / 0 |
| lexington-downtown | 37 | 5 | 13% | interior plans 5.4 | sidewalks 0.9 | 119,517 / 4,078 | 137 / 33 | 57 / 28 | 94 / 1 | 1 / 0 |
| hazard | 16 | 4 | 22% | streets 2.6 | objects 0.6 | 62,045 / 4,375 | 77 / 32 | 43 / 27 | 149 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 56.15 | 28.07 | make 48.97 + switch on 7.18 ms; 1602 objects; destroy 2.97 ms; 3,000 rays 2.56 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 14.88 | 7.44 | make 10.16 + switch on 4.72 ms; 10 objects; destroy 1.56 ms; 3,000 rays 2.00 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.83 | 5.92 | make 3.91 + switch on 7.92 ms; 2 objects; destroy 0.11 ms; 3,000 rays 1.98 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.11 | 1.06 | make 2.08 + switch on 0.03 ms (+ 8.18 ms baking on the worker); 2 objects; destroy 0.06 ms; 3,000 rays 2.02 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.77 | 219.14 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.93 | 198.25 | objects 0.81, allocate 0.02, pack 6.99, apply 0.11 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.03 | 25.75 | objects 0.89, allocate 0.02, pack 6.84 (worker), apply 0.12 ms |
| primitives | Box, turned, flat colour | 20,000 | 27.98 | 1.40 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 78.40 | 3.92 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 42.79 | 2.14 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 77.60 | 3.88 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 18.06 | 0.90 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 51.69 | 2.58 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.44 | 0.32 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.82 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 10.08 | 2.02 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 21.32 | 4.26 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 46.83 | 15.61 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 33.67 | 11.22 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.72 | 6.86 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.36 | 11.36 |  |
| objects | BoxCollider added to one object | 2,000 | 39.85 | 19.92 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 28.67 | 143.32 |  |
| objects | RoomLight.Spawn | 100 | 16.06 | 160.61 |  |
| objects | switching all of the above on | 1 | 27.50 | 27503.30 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 8.53 | 8527.30 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 59 | 0.59 | 10.03 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 1.98 | 197.64 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 389.70 | 389702.30 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 386.87 | 386870.70 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 19.58 | 19577.40 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 1,296 ms, 2,000,934 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (25) | 0.24 | 0.05 | 0.43 | 4.86 | 10.24 | 4 / 2 / 0 / 0 | ground 9.7 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 1,347 / 2,064,831 | 1 | 237 / 850 (pool 39) |
| drive-east | 2400 (567) | 0.88 | 0.05 | 4.39 | 5.54 | 33.03 | 47 / 9 / 1 / 1 | interior plans 31.4 | 38 (24 / 14) | 3 | 14 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 3,669 / 7,096,784 | 1 | 236 / 1,066 (pool 40) |
| teleport-louisville | 720 (96) | 0.92 | 0.05 | 5.79 | 9.04 | 83.91 | 45 / 16 / 4 / 3 | interior plans 10.3 | 25 (9 / 16) | 0 | 30 | 0.1 | 1.1 | 8 / 67 | 41 (16 Lite) / 1,988 / 3,217,606 | 3 | 251 / 919 (pool 40) |
| drive-louisville | 1800 (162) | 0.40 | 0.05 | 3.18 | 5.39 | 43.49 | 24 / 8 / 2 / 1 | interior plans 18.6 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 21 | 46 (12 Lite) / 2,391 / 4,020,233 | 5 | 294 / 954 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: ground (1x, worst 9.7 ms), lots (1x, worst 0.7 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: interior plans (5x, worst 31.4 ms), utility lines (1x, worst 10.6 ms), colliders (1x, worst 10.3 ms)
- **teleport-louisville**: set down in downtown Louisville (113 km away), standing; the frames over 8 ms were made by: interior plans (12x, worst 10.3 ms), wall drifts (1x, worst 2.2 ms), kerbside cars (2x, worst 2.1 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (3x, worst 18.6 ms), colliders (1x, worst 8.4 ms), sidewalks (1x, worst 8.3 ms), buildings (2x, worst 7.4 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 138 edit-mode artefacts

- `(edit mode) Error: Raid_sign_SIS CORPORATE HEADQUAR1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SIS CORPORATE HEADQUAR1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOBACCO08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOBACCO08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OLD SAYRE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OLD SAYRE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BEAUTY SHOP37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BEAUTY SHOP37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BAKERY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BAKERY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_CHINA STAR37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_CHINA STAR37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FEED & SEED1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FEED & SEED1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_ANTIQUES37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_ANTIQUES37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_MERCANTILE CO1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_MERCANTILE CO1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.534 | 0% | 3,043 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.069 | 0% | 309 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on North Limestone, at the game's starting zoom | 0.393 | 0% | 115 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on North Limestone, at the game's starting zoom | 0.100 | 0% | 86 |
| `iso_downtown_day.png` | Downtown Lexington: the middle of the square, on West Main Street | 0.308 | 0% | 64 |
| `iso_downtown_night.png` | Downtown Lexington: the middle of the square, on West Main Street | 0.076 | 0% | 53 |
| `iso_residential_day.png` | The densest residential street: Wrenn Court, 3 homes within 30 m | 0.209 | 0% | 60 |
| `iso_residential_night.png` | The densest residential street: Wrenn Court, 3 homes within 30 m | 0.058 | 0% | 69 |
| `iso_intersection_day.png` | The busiest intersection: East Vine Street / South Limestone / West Vine Street (4 ways in) | 0.327 | 0% | 51 |
| `iso_intersection_night.png` | The busiest intersection: East Vine Street / South Limestone / West Vine Street (4 ways in) | 0.076 | 0% | 54 |
| `iso_green_day.png` | Green: park 'Courthouse Plaza', 5,476 m² inside the square | 0.497 | 0% | 57 |
| `iso_green_night.png` | Green: park 'Courthouse Plaza', 5,476 m² inside the square | 0.086 | 0% | 60 |
| `iso_parking_day.png` | Parking: a parking, 4,181 m² inside the square | 0.350 | 0% | 50 |
| `iso_parking_night.png` | Parking: a parking, 4,181 m² inside the square | 0.073 | 0% | 59 |
| `iso_edge_day.png` | The square's edge: where East Vine Street leaves it (south), the playable map giving way to the scenery ring | 0.458 | 0% | 61 |
| `iso_edge_night.png` | The square's edge: where East Vine Street leaves it (south), the playable map giving way to the scenery ring | 0.070 | 0% | 52 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, commercial 'Helix Parking Garage' (3,144 m² footprint) | 0.433 | 0% | 59 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, commercial 'Helix Parking Garage' (3,144 m² footprint) | 0.069 | 0% | 65 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on North Limestone (downtown), looking along the street | 0.363 | 0% | 92 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on North Limestone (downtown), looking along the street | 0.160 | 22% | 70 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of Wrenn Court (the densest residential street), looking along it | 0.206 | 0% | 75 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of Wrenn Court (the densest residential street), looking along it | 0.135 | 0% | 61 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.539 | 0% | 105 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.466 | 0% | 51 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.081 | 0% | 54 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 27.2, 28.2 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -1.5, -1.4 | the place's own point in places.json (its OSM place node, which the square is built around), moved 2.1 m onto the nearest street |
| residential | -46.7, 51.5 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | -83.8, -80.7 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | 86.6, 34.0 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | -157.2, -75.7 | the biggest parking area inside the square |
| edge | -0.8, -200.0 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | none | nothing of the kind in the square |
| landmark | 61.7, -160.8 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 138 edit-mode artefacts

- `(edit mode) Error: Raid_sign_SIS CORPORATE HEADQUAR1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SIS CORPORATE HEADQUAR1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOBACCO08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOBACCO08080733322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OLD SAYRE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OLD SAYRE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BEAUTY SHOP37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BEAUTY SHOP37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BAKERY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BAKERY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_CHINA STAR37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_CHINA STAR37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FEED & SEED1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FEED & SEED1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_ANTIQUES37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_ANTIQUES37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_MERCANTILE CO1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_MERCANTILE CO1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 79 |  |
| square: KentuckyMap.Build | yes | 7,042 |  |
| square: count | yes | 789 |  |
| square: pick spots | yes | 597 |  |
| square: pictures | yes | 6,686 |  |
| square: discard | yes | 266 |  |
| square: passes (warm rebuild) | yes | 3,886 |  |
| chunks: stress test | yes | 1,332 |  |
| chunks: Louisville 3x3 | yes | 1,565 |  |
| chunks: determinism | yes | 141 |  |
| perf: Lite against Full | yes | 1,035 |  |
| perf: determinism across build orders | yes | 270 |  |
| perf: what things cost | yes | 1,638 |  |
| perf: walking and driving through the state | yes | 163,693 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
