# CLONE-Z map harness: Falmouth (build ky-local-f2)

**Exit 0**: ok. Full run, 190.5 s, started 2026-10-24 11:50:07.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Falmouth plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 9,108 ms |
| the same square again, warm, pass by pass | 4,663 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 42.7, -4.8, facing -67° |
| exits | Chapel Street (north) at -2.5, 197.0; KY 22 east at 197.0, 173.2; KY 22 south at -60.4, -197.0; West Shelby Street at -197.0, 107.3 |
| overhead renderers (hidden in the iso view) | 215 |
| `KentuckyMap.LastStats` | Kentucky map Falmouth (build ky-local-f2): 150 buildings, 162 beyond the edge, 458 street pieces, 830 containers, 1133 lights, 510 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 150 with their middle inside the square (11 of them reach within 1 m of its edge or past it), 162 in the scenery ring; inside: house 69, garage 27, commercial 19, shed 9, apartments 8, retail 6, office 5, civic 4, church 2, canopy 1 |
| map data: streets, lines, areas | 458 street pieces, 7 lines (stream 5, rail 1, river 1), 91 land-use areas (residential 27, parking 15, commercial 8, grass 8, churchyard 7, forest 7, industrial 6, pitch 3, retail 3, school 2, water 2, meadow 1, park 1, plaza 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 32,465 | 16,370 | 16,370 | 15,240 | 8,515,377 | 5,227,117 | 154 | 38,253 | 1,162 (0) | 9,583 | 10,834 | 830 |
| RaidOverhead | 707 | 558 | 558 | 558 | 443,393 | 268,883 | 121 | 2,099 | 0 (0) | 0 | 558 | 0 |
| RaidScenery | 114 | 113 | 113 | 113 | 105,450 | 112,040 | 68 | 488 | 0 (0) | 0 | 64 | 0 |
| **all** | 33,286 | 17,041 | 17,041 | 15,911 | 9,064,220 | 5,608,040 | 240 | 40,840 | 1,162 (0) | 9,583 | 11,456 | 830 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 30,394 | 15,903 | 15,903 | 14,795 | 7,056,175 | 4,482,085 | 81 | 35,358 | 1,109 (0) | 6,456 | 10,532 | 779 |
| World/Building shells | 164 | 163 | 163 | 163 | 1,018,618 | 512,188 | 61 | 1,476 | 0 (0) | 0 | 163 | 0 |
| RaidOverhead/Building shells | 164 | 163 | 163 | 163 | 285,640 | 138,085 | 103 | 1,018 | 0 (0) | 0 | 163 | 0 |
| World/OutdoorGround | 140 | 140 | 140 | 140 | 152,022 | 63,015 | 20 | 739 | 0 (0) | 0 | 0 | 0 |
| World/OutdoorProps | 138 | 138 | 138 | 138 | 286,743 | 167,065 | 19 | 654 | 0 (0) | 0 | 138 | 0 |
| RaidOverhead/Roofs | 141 | 141 | 141 | 141 | 53,677 | 31,705 | 65 | 505 | 0 (0) | 0 | 141 | 0 |
| RaidOverhead/Interior KY# | 294 | 147 | 147 | 147 | 93,978 | 84,892 | 5 | 469 | 0 (0) | 0 | 147 | 0 |
| RaidScenery/SceneryProps | 39 | 39 | 39 | 39 | 52,756 | 31,450 | 54 | 292 | 0 (0) | 0 | 39 | 0 |
| RaidScenery/SceneryGround | 47 | 47 | 47 | 47 | 13,429 | 5,610 | 13 | 169 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 68 | 68 | 68 | 68 | 2,481 | 827 | 1 | 68 | 0 (0) | 0 | 68 | 0 |
| RaidOverhead/Wires | 39 | 39 | 39 | 39 | 7,617 | 13,374 | 1 | 39 | 0 (0) | 0 | 39 | 0 |
| RaidScenery/Wires | 25 | 25 | 25 | 25 | 2,271 | 4,014 | 1 | 25 | 0 (0) | 0 | 25 | 0 |
| World/StreetLamp | 48 | 24 | 24 | 2 | 1,771 | 2,740 | 3 | 24 | 24 (0) | 24 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 36,990 | 70,964 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 20,709 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 94 | 2% | 1 | 94 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 4 | 0% | 1 | 4 |
| water | 11 | 0% | 10 | 5 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 0 | 0% | 1 | 0 |
| lots | 2 | 0% | 225 | 1 |
| grounds | 0 | 0% | 8 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 56 | 1% | 1 | 56 |
| road network | 13 | 0% | 1 | 13 |
| junctions | 9 | 0% | 1 | 9 |
| streets | 33 | 1% | 340 | 7 |
| sidewalks | 26 | 1% | 113 | 4 |
| berms and driveways | 16 | 0% | 106 | 1 |
| road markings | 1 | 0% | 4 | 1 |
| paths | 5 | 0% | 142 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 3 | 0% | 1 | 3 |
| buildings | 122 | 3% | 49 | 7 |
| interior plans | 195 | 4% | 1266 | 5 |
| interior shell | 24 | 1% | 147 | 7 |
| interior walls | 129 | 3% | 147 | 6 |
| interior floors | 17 | 0% | 147 | 0 |
| interior doors | 480 | 10% | 275 | 11 |
| interior switches | 85 | 2% | 147 | 3 |
| interior rooms | 2,361 | 51% | 797 | 26 |
| interior wall meshes | 105 | 2% | 147 | 7 |
| interior finish | 266 | 6% | 294 | 18 |
| interior merge | 302 | 6% | 474 | 16 |
| cutaway | 13 | 0% | 7 | 2 |
| utility lines | 7 | 0% | 16 | 2 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 5 | 0% | 98 | 3 |
| service drops | 2 | 0% | 312 | 0 |
| street lights | 4 | 0% | 48 | 0 |
| signs | 2 | 0% | 227 | 0 |
| street furniture | 4 | 0% | 316 | 1 |
| landmarks | 0 | 0% | 8 | 0 |
| fences and walls | 4 | 0% | 80 | 0 |
| park litter | 0 | 0% | 2 | 0 |
| wires | 8 | 0% | 65 | 4 |
| nature plan | 4 | 0% | 1 | 4 |
| driveway plan | 2 | 0% | 1 | 2 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 30 | 1% | 173 | 8 |
| grounds trees | 1 | 0% | 7 | 0 |
| street trees | 2 | 0% | 159 | 0 |
| creek trees | 0 | 0% | 96 | 0 |
| fence rows | 1 | 0% | 33 | 0 |
| woods | 12 | 0% | 9333 | 0 |
| driveway cars | 3 | 0% | 21 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 4 | 0% | 29 | 3 |
| kerbside cars | 9 | 0% | 116 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 1 | 0% | 1 | 1 |
| wall drifts | 20 | 0% | 562 | 0 |
| fence drifts | 2 | 0% | 51 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 2 | 0% | 1 | 2 |
| points | 1 | 0% | 1 | 1 |
| (after the last pass) | 21 | 0% | 1 | 21 |
| (emit the overhead meshes) | 15 | 0% | 1 | 15 |
| (emit the outdoor meshes) | 119 | 3% | 1 | 119 |
| **total** | **4,663** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Falmouth | 12683_9851 (198_153) | 0 + 0 | **143** | cutaway 56 | 1 | 333 | 120 | 239,217 | 125,683 | 5 | 361 | 25 | 34/25/9 | 0E 0W |
| rural-5km | 5 km from Falmouth (bearing 50°) | 12713_9876 (198_154) | 0 + 0 | **14** | woods 2 | 1 | 68 | 38 | 33,987 | 25,013 | 0 | 93 | 2 | 2/2/6 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 192 + 13 | **62** | interior plans 4 | 1 | 212 | 53 | 125,834 | 68,421 | 0 | 160 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 565 + 55 | **94** | buildings 35 | 1 | 133 | 58 | 125,770 | 66,544 | 1 | 93 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 6 + 1 | **19** | streets 3 | 1 | 79 | 43 | 57,745 | 42,675 | 0 | 147 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 5.0 | 1 | 5.0 |
| ground | 0.5 | 1 | 0.5 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.7 | 1 | 0.7 |
| lots | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 1.4 | 1 | 1.4 |
| junctions | 0.8 | 1 | 0.8 |
| streets | 3.2 | 2 | 2.2 |
| sidewalks | 4.0 | 2 | 3.2 |
| berms and driveways | 1.9 | 1 | 1.9 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.5 | 1 | 0.5 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 18.9 | 9 | 3.5 |
| interior plans | 26.6 | 12 | 4.1 |
| cutaway | 58.0 | 2 | 56.0 |
| utility lines | 0.6 | 1 | 0.6 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.3 | 1 | 1.3 |
| street lights | 1.1 | 1 | 1.1 |
| signs | 0.3 | 1 | 0.3 |
| street furniture | 0.4 | 1 | 0.4 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.1 | 1 | 0.1 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.5 | 1 | 0.5 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 1 | 0.2 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.2 | 1 | 0.2 |
| kerbside cars | 1.0 | 1 | 1.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.1 | 1 | 0.1 |
| wall drifts | 2.5 | 2 | 2.0 |
| fence drifts | 0.1 | 1 | 0.1 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 3.6 | 2 | 2.2 |
| objects | 3.0 | 1 | 3.0 |
| meshes | 0.8 | 1 | 0.8 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.0 | 1 | 1.0 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.2 | 1 | 0.2 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 0.3 | 1 | 0.3 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 0.6 | 1 | 0.6 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 1.6 | 1 | 1.6 |
| interior plans | 0.9 | 1 | 0.9 |
| cutaway | 0.3 | 1 | 0.3 |
| utility lines | 0.0 | 1 | 0.0 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.5 | 1 | 0.5 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.4 | 1 | 0.4 |
| woods | 2.0 | 1 | 2.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.2 | 1 | 0.2 |
| fence drifts | 0.5 | 1 | 0.5 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.6 | 1 | 0.6 |
| objects | 1.2 | 1 | 1.2 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 1.0 | 1 | 1.0 |
| water | 1.7 | 1 | 1.7 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.1 | 1 | 0.1 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.2 | 1 | 1.2 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.8 | 1 | 0.8 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.7 | 1 | 0.7 |
| paths | 4.4 | 3 | 2.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 14.5 | 6 | 3.5 |
| interior plans | 19.9 | 8 | 4.5 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.9 | 1 | 0.9 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.4 | 1 | 0.4 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.5 | 1 | 0.5 |
| kerbside cars | 1.8 | 1 | 1.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.5 | 1 | 1.5 |
| objects | 1.4 | 1 | 1.4 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 1.4 | 1 | 1.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 1.9 | 1 | 1.9 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.2 | 1 | 1.2 |
| junctions | 1.4 | 1 | 1.4 |
| streets | 6.0 | 2 | 5.3 |
| sidewalks | 3.3 | 2 | 2.6 |
| berms and driveways | 1.6 | 1 | 1.6 |
| road markings | 7.1 | 2 | 6.8 |
| paths | 0.9 | 1 | 0.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 43.3 | 5 | 35.0 |
| interior plans | 11.4 | 4 | 4.8 |
| cutaway | 0.8 | 1 | 0.8 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.5 | 1 | 0.5 |
| street furniture | 0.7 | 1 | 0.7 |
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
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.6 | 1 | 0.6 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.9 | 1 | 0.9 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 1 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.6 | 1 | 1.6 |
| colliders | 1.2 | 1 | 1.2 |
| objects | 1.7 | 1 | 1.7 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.1 | 1 | 0.1 |
| lots | 0.6 | 1 | 0.6 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 3.1 | 2 | 2.9 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.0 | 1 | 0.0 |
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
| fence rows | 1.8 | 1 | 1.8 |
| woods | 3.5 | 2 | 2.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.8 | 1 | 0.8 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.1 | 1 | 0.1 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.5 | 1 | 0.5 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.3 | 1 | 0.3 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 198_154 | yes | 68 | 34 |
| 198_153 | yes | 66 | 8 |
| 197_153 | yes | 28 | 5 |
| 197_154 | yes | 87 | 7 |
| 183_148 | yes | 192 | 13 |
| 196_145 | yes | 565 | 55 |
| 210_134 | yes | 6 | 1 |
| 182_148 | yes | 143 | 10 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 522 ms (slowest 99 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **49** | interior plans 4 | 1 | 212 | 53 | 125,834 | 68,421 | 0 | 160 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **63** | interior plans 8 | 1 | 211 | 75 | 145,993 | 80,461 | 0 | 204 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 143 + 10 | **54** | interior plans 8 | 1 | 247 | 62 | 131,347 | 73,503 | 3 | 199 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **49** | interior plans 4 | 1 | 171 | 57 | 120,158 | 63,789 | 0 | 154 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **40** | buildings 4 | 1 | 177 | 63 | 92,872 | 54,521 | 1 | 130 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **43** | interior plans 7 | 1 | 167 | 53 | 126,933 | 72,757 | 0 | 124 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **99** | buildings 51 | 1 | 208 | 53 | 126,713 | 68,700 | 0 | 209 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **49** | interior plans 7 | 0 | 162 | 56 | 98,497 | 59,156 | 4 | 97 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **77** | interior plans 11 | 1 | 185 | 45 | 206,561 | 114,476 | 0 | 154 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,742 | 517 | 517 | 512 | 1,174,908 | 655,784 | 128 | 2,187 | 8 (0) | 1,431 | 356 | 104 |

### Determinism

Chunk 12683_9851 (start-centre) built twice: **match**: identical, in the same order (239,217 vertices, 125,683 triangles, 333 objects, 120 meshes; hash 4e939da240e66225 vs 4e939da240e66225; 370 colliders, lights and loot containers, hash c80d4f256fb953f1 vs c80d4f256fb953f1). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 143 | 356 / 188 / 3.4 | 0 / 0.0 | 111 / 235,209 / 0.6 | 58.9 | 80.6 | 62 / 4.7 |
| rural-5km | Full | 14 | 92 / 23 / 0.6 | 0 / 0.0 | 37 / 31,387 / 0.1 | 1.3 | 12.3 | 34 / 0.2 |
| louisville-downtown | Full | 62 | 159 / 116 / 1.5 | 0 / 0.0 | 51 / 123,223 / 0.3 | 1.6 | 58.7 | 36 / 1.2 |
| lexington-downtown | Full | 94 | 91 / 55 / 1.2 | 0 / 0.0 | 53 / 122,966 / 0.3 | 1.9 | 90.9 | 39 / 0.3 |
| hazard | Full | 19 | 146 / 19 / 0.5 | 0 / 0.0 | 38 / 54,547 / 0.2 | 0.8 | 17.4 | 31 / 0.2 |
| louisville 0,0 | Full | 49 | 159 / 116 / 1.3 | 0 / 0.0 | 51 / 123,223 / 0.3 | 1.4 | 46.5 | 36 / 0.3 |
| louisville 1,0 | Full | 63 | 203 / 108 / 1.7 | 0 / 0.0 | 73 / 143,324 / 0.5 | 1.7 | 59.5 | 46 / 0.4 |
| louisville -1,0 | Full | 54 | 195 / 138 / 1.7 | 0 / 0.0 | 52 / 127,924 / 0.4 | 1.2 | 50.8 | 40 / 0.3 |
| louisville 0,1 | Full | 49 | 153 / 85 / 1.4 | 0 / 0.0 | 54 / 117,657 / 0.4 | 1.8 | 46.2 | 36 / 0.3 |
| louisville 0,-1 | Full | 40 | 129 / 74 / 1.0 | 0 / 0.0 | 58 / 88,650 / 0.5 | 1.3 | 37.3 | 37 / 0.5 |
| louisville 1,1 | Full | 43 | 123 / 76 / 1.0 | 0 / 0.0 | 50 / 123,865 / 0.4 | 1.1 | 40.5 | 35 / 0.3 |
| louisville -1,1 | Full | 99 | 208 / 115 / 1.5 | 0 / 0.0 | 50 / 123,437 / 0.2 | 1.1 | 95.9 | 37 / 0.3 |
| louisville 1,-1 | Full | 49 | 92 / 58 / 0.8 | 0 / 0.0 | 47 / 94,177 / 0.2 | 1.1 | 46.9 | 36 / 0.7 |
| louisville -1,-1 | Full | 77 | 153 / 106 / 1.5 | 0 / 0.0 | 41 / 203,424 / 0.2 | 3.2 | 72.7 | 30 / 0.3 |

The Louisville block's NavMesh: 333 sources collected in 2 ms; baked at once (as one 384 m square) in 160 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 57 | 8 | 13% | interior plans 2.4 | buildings 2.1 | 239,217 / 11,174 | 333 / 45 | 120 / 40 | 361 / 1 | 5 / 0 |
| rural-5km | 11 | 7 | 65% | woods 1.9 | data 2.7 | 29,632 / 3,225 | 66 / 37 | 37 / 32 | 89 / 1 | 0 / 0 |
| louisville-downtown | 45 | 7 | 15% | interior plans 4.5 | buildings 2.3 | 119,106 / 6,777 | 214 / 33 | 53 / 28 | 161 / 1 | 0 / 0 |
| lexington-downtown | 40 | 5 | 13% | streets 5.3 | sidewalks 0.9 | 120,238 / 4,078 | 138 / 33 | 59 / 28 | 97 / 1 | 0 / 0 |
| hazard | 18 | 4 | 20% | streets 2.7 | woods 0.8 | 61,811 / 4,436 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 73.99 | 36.99 | make 61.56 + switch on 12.43 ms; 1602 objects; destroy 5.62 ms; 3,000 rays 3.36 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 27.67 | 13.83 | make 18.88 + switch on 8.79 ms; 10 objects; destroy 2.60 ms; 3,000 rays 4.07 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 18.13 | 9.06 | make 6.53 + switch on 11.59 ms; 2 objects; destroy 0.15 ms; 3,000 rays 3.30 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.89 | 1.45 | make 2.86 + switch on 0.04 ms (+ 10.42 ms baking on the worker); 2 objects; destroy 0.07 ms; 3,000 rays 2.12 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 48.04 | 1201.02 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 10.65 | 266.22 | objects 1.31, allocate 0.02, pack 9.15, apply 0.17 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.03 | 25.83 | objects 0.89, allocate 0.02, pack 7.41 (worker), apply 0.13 ms |
| primitives | Box, turned, flat colour | 20,000 | 40.97 | 2.05 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 64.21 | 3.21 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 47.15 | 2.36 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 97.46 | 4.87 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 25.76 | 1.29 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 25.93 | 1.30 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.50 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.71 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 16.70 | 3.34 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 50.12 | 10.02 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 26.63 | 8.88 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 40.09 | 13.36 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.40 | 6.70 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.23 | 11.23 |  |
| objects | BoxCollider added to one object | 2,000 | 39.90 | 19.95 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 23.07 | 115.35 |  |
| objects | RoomLight.Spawn | 100 | 11.04 | 110.44 |  |
| objects | switching all of the above on | 1 | 28.00 | 28002.00 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 10.86 | 10858.80 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 78 | 0.69 | 8.85 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.23 | 222.61 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 387.46 | 387456.10 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 379.74 | 379741.10 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 21.01 | 21009.70 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 933 ms, 2,591,198 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (24) | 0.23 | 0.05 | 0.08 | 5.37 | 10.16 | 5 / 3 / 0 / 0 | mapped trees 6.5 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 1,559 / 2,652,557 | 2 | 204 / 892 (pool 40) |
| drive-east | 2400 (134) | 0.25 | 0.05 | 1.01 | 4.63 | 10.52 | 13 / 2 / 0 / 0 | signs 7.2 | 40 (24 / 16) | 0 | 19 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 2,173 / 3,568,868 | 2 | 203 / 932 (pool 40) |
| teleport-louisville | 720 (111) | 0.89 | 0.05 | 6.28 | 10.52 | 36.12 | 54 / 16 / 2 / 2 | danger and landmarks 13.5 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.2 | 9 / 73 | 41 (16 Lite) / 2,162 / 3,851,826 | 4 | 220 / 942 (pool 40) |
| drive-louisville | 1800 (167) | 0.42 | 0.05 | 3.51 | 5.89 | 27.61 | 31 / 10 / 2 / 0 | interior plans 16.9 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 20 | 46 (12 Lite) / 2,573 / 4,692,914 | 6 | 269 / 978 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: mapped trees (1x, worst 6.5 ms), streets (1x, worst 2.0 ms), road network (1x, worst 1.3 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: signs (1x, worst 7.2 ms), driveway cars (1x, worst 5.9 ms)
- **teleport-louisville**: set down in downtown Louisville (133 km away), standing; the frames over 8 ms were made by: danger and landmarks (1x, worst 13.5 ms), interior plans (6x, worst 13.0 ms), objects (1x, worst 7.0 ms), junctions (1x, worst 3.5 ms), buildings (2x, worst 2.6 ms), utility lines (1x, worst 2.5 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (4x, worst 16.9 ms), points (1x, worst 11.0 ms), (swap) (1x, worst 9.3 ms), wall drifts (1x, worst 4.5 ms), nature plan (1x, worst 3.5 ms), buildings (1x, worst 2.8 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 26 edit-mode artefacts

- `(edit mode) Error: Raid_sign_BOOKS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GENERAL MDSE1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GENERAL MDSE1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
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
- `(edit mode) Error: Raid_sign_FLORIST1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FLORIST1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.555 | 0% | 2,466 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 475 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.494 | 0% | 206 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.087 | 0% | 111 |
| `iso_downtown_day.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.409 | 0% | 93 |
| `iso_downtown_night.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.087 | 0% | 95 |
| `iso_residential_day.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.474 | 0% | 118 |
| `iso_residential_night.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.084 | 0% | 125 |
| `iso_intersection_day.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.372 | 0% | 83 |
| `iso_intersection_night.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.084 | 0% | 102 |
| `iso_green_day.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.592 | 0% | 101 |
| `iso_green_night.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.080 | 0% | 135 |
| `iso_parking_day.png` | Parking: a parking, 2,703 m² inside the square | 0.390 | 0% | 118 |
| `iso_parking_night.png` | Parking: a parking, 2,703 m² inside the square | 0.080 | 0% | 95 |
| `iso_edge_day.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.405 | 0% | 72 |
| `iso_edge_night.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.075 | 0% | 66 |
| `iso_waterway_day.png` | Waterway: a stream | 0.471 | 0% | 63 |
| `iso_waterway_night.png` | Waterway: a stream | 0.082 | 0% | 83 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.545 | 0% | 67 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.077 | 0% | 69 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.416 | 0% | 120 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.184 | 14% | 119 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.469 | 0% | 96 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.152 | 31% | 65 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.542 | 0% | 130 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.466 | 0% | 53 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.081 | 0% | 58 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 42.7, -4.8 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | 7.1, 16.2 | the place's own point in places.json (its OSM place node, which the square is built around), moved 17.7 m onto the nearest street |
| residential | -70.4, -100.1 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | 25.1, 8.4 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | 108.4, -123.7 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | 2.4, 106.8 | the biggest parking area inside the square |
| edge | -60.9, -200.0 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | 116.8, 2.4 | the point of a railway, river, canal or stream nearest the square's middle |
| landmark | 74.3, 43.9 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 26 edit-mode artefacts

- `(edit mode) Error: Raid_sign_BOOKS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GENERAL MDSE1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GENERAL MDSE1A0A0833322D: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
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
- `(edit mode) Error: Raid_sign_FLORIST1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FLORIST1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 101 |  |
| square: KentuckyMap.Build | yes | 9,110 |  |
| square: count | yes | 1,016 |  |
| square: pick spots | yes | 116 |  |
| square: pictures | yes | 7,544 |  |
| square: discard | yes | 277 |  |
| square: passes (warm rebuild) | yes | 5,111 |  |
| chunks: stress test | yes | 1,556 |  |
| chunks: Louisville 3x3 | yes | 1,648 |  |
| chunks: determinism | yes | 216 |  |
| perf: Lite against Full | yes | 1,106 |  |
| perf: determinism across build orders | yes | 400 |  |
| perf: what things cost | yes | 1,743 |  |
| perf: walking and driving through the state | yes | 160,553 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
