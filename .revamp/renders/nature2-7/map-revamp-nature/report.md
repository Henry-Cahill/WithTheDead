# CLONE-Z map harness: Falmouth (build ky-local-f2)

**Exit 0**: ok. Full run, 191.4 s, started 2026-10-24 12:23:20.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Falmouth plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 7,848 ms |
| the same square again, warm, pass by pass | 4,485 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 42.7, -4.8, facing -67° |
| exits | Chapel Street (north) at -2.5, 197.0; KY 22 east at 197.0, 173.2; KY 22 south at -60.4, -197.0; West Shelby Street at -197.0, 107.3 |
| overhead renderers (hidden in the iso view) | 215 |
| `KentuckyMap.LastStats` | Kentucky map Falmouth (build ky-local-f2): 150 buildings, 162 beyond the edge, 458 street pieces, 834 containers, 1133 lights, 510 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 150 with their middle inside the square (11 of them reach within 1 m of its edge or past it), 162 in the scenery ring; inside: house 69, garage 27, commercial 19, shed 9, apartments 8, retail 6, office 5, civic 4, church 2, canopy 1 |
| map data: streets, lines, areas | 458 street pieces, 7 lines (stream 5, rail 1, river 1), 91 land-use areas (residential 27, parking 15, commercial 8, grass 8, churchyard 7, forest 7, industrial 6, pitch 3, retail 3, school 2, water 2, meadow 1, park 1, plaza 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 32,495 | 16,376 | 16,376 | 15,246 | 8,526,068 | 5,232,428 | 154 | 38,269 | 1,162 (0) | 9,597 | 10,840 | 834 |
| RaidOverhead | 707 | 558 | 558 | 558 | 452,067 | 273,248 | 80 | 2,057 | 0 (0) | 0 | 558 | 0 |
| RaidScenery | 114 | 113 | 113 | 113 | 105,450 | 112,040 | 68 | 488 | 0 (0) | 0 | 64 | 0 |
| **all** | 33,316 | 17,047 | 17,047 | 15,917 | 9,083,585 | 5,617,716 | 199 | 40,814 | 1,162 (0) | 9,597 | 11,462 | 834 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 30,400 | 15,909 | 15,909 | 14,801 | 7,056,175 | 4,482,085 | 81 | 35,371 | 1,109 (0) | 6,456 | 10,538 | 779 |
| World/Building shells | 164 | 163 | 163 | 163 | 1,018,618 | 512,188 | 61 | 1,476 | 0 (0) | 0 | 163 | 0 |
| RaidOverhead/Building shells | 164 | 163 | 163 | 163 | 294,220 | 142,375 | 62 | 975 | 0 (0) | 0 | 163 | 0 |
| World/OutdoorGround | 140 | 140 | 140 | 140 | 158,401 | 66,022 | 20 | 739 | 0 (0) | 0 | 0 | 0 |
| World/OutdoorProps | 138 | 138 | 138 | 138 | 291,055 | 169,369 | 19 | 657 | 0 (0) | 0 | 138 | 0 |
| RaidOverhead/Roofs | 141 | 141 | 141 | 141 | 53,771 | 31,780 | 66 | 506 | 0 (0) | 0 | 141 | 0 |
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

None: every one of the 20,715 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 88 | 2% | 1 | 88 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 4 | 0% | 1 | 4 |
| water | 11 | 0% | 10 | 5 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 0 | 0% | 1 | 0 |
| lots | 2 | 0% | 225 | 1 |
| grounds | 0 | 0% | 8 | 0 |
| work yards | 2 | 0% | 1 | 2 |
| snow tone | 23 | 1% | 1 | 23 |
| road network | 20 | 0% | 1 | 20 |
| junctions | 13 | 0% | 1 | 13 |
| streets | 26 | 1% | 340 | 1 |
| sidewalks | 29 | 1% | 113 | 5 |
| berms and driveways | 17 | 0% | 106 | 1 |
| road markings | 1 | 0% | 4 | 1 |
| paths | 5 | 0% | 142 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 3 | 0% | 1 | 3 |
| buildings | 120 | 3% | 50 | 5 |
| interior plans | 196 | 4% | 1267 | 6 |
| interior shell | 16 | 0% | 147 | 0 |
| interior walls | 130 | 3% | 147 | 7 |
| interior floors | 17 | 0% | 147 | 0 |
| interior doors | 460 | 10% | 275 | 28 |
| interior switches | 103 | 2% | 147 | 25 |
| interior rooms | 2,333 | 52% | 797 | 34 |
| interior wall meshes | 103 | 2% | 147 | 6 |
| interior finish | 232 | 5% | 294 | 7 |
| interior merge | 275 | 6% | 523 | 7 |
| cutaway | 10 | 0% | 6 | 2 |
| utility lines | 6 | 0% | 15 | 2 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 98 | 3 |
| service drops | 2 | 0% | 312 | 0 |
| street lights | 3 | 0% | 48 | 0 |
| signs | 2 | 0% | 227 | 0 |
| street furniture | 7 | 0% | 316 | 4 |
| landmarks | 0 | 0% | 8 | 0 |
| fences and walls | 12 | 0% | 80 | 6 |
| park litter | 0 | 0% | 2 | 0 |
| wires | 8 | 0% | 65 | 6 |
| nature plan | 4 | 0% | 1 | 4 |
| driveway plan | 2 | 0% | 1 | 2 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 0% | 173 | 0 |
| grounds trees | 1 | 0% | 7 | 0 |
| street trees | 2 | 0% | 159 | 0 |
| creek trees | 0 | 0% | 96 | 0 |
| fence rows | 1 | 0% | 33 | 0 |
| woods | 10 | 0% | 9333 | 0 |
| driveway cars | 2 | 0% | 21 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 29 | 2 |
| kerbside cars | 7 | 0% | 132 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 1 | 0% | 1 | 1 |
| wall drifts | 17 | 0% | 562 | 0 |
| fence drifts | 3 | 0% | 51 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 2 | 0% | 1 | 2 |
| points | 1 | 0% | 1 | 1 |
| (after the last pass) | 13 | 0% | 1 | 13 |
| (emit the overhead meshes) | 13 | 0% | 1 | 13 |
| (emit the outdoor meshes) | 106 | 2% | 1 | 106 |
| **total** | **4,485** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Falmouth | 12683_9851 (198_153) | 0 + 0 | **103** | colliders 40 | 1 | 336 | 120 | 243,939 | 128,029 | 5 | 364 | 25 | 34/25/9 | 0E 0W |
| rural-5km | 5 km from Falmouth (bearing 50°) | 12713_9876 (198_154) | 0 + 0 | **12** | woods 2 | 0 | 68 | 38 | 33,987 | 25,013 | 0 | 93 | 2 | 2/2/6 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 148 + 9 | **52** | interior plans 4 | 1 | 193 | 53 | 125,744 | 68,219 | 0 | 156 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 399 + 51 | **52** | junctions 7 | 0 | 166 | 58 | 133,384 | 71,106 | 1 | 111 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **19** | streets 3 | 0 | 79 | 43 | 57,745 | 42,675 | 0 | 147 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.9 | 1 | 1.9 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.6 | 1 | 0.6 |
| lots | 0.6 | 1 | 0.6 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.3 | 1 | 1.3 |
| junctions | 0.6 | 1 | 0.6 |
| streets | 2.5 | 2 | 2.0 |
| sidewalks | 2.8 | 2 | 2.2 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.4 | 1 | 0.4 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 9.3 | 5 | 2.1 |
| interior plans | 20.3 | 10 | 2.5 |
| cutaway | 1.8 | 1 | 1.8 |
| utility lines | 0.6 | 1 | 0.6 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.1 | 1 | 1.1 |
| street lights | 0.7 | 1 | 0.7 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.1 | 1 | 0.1 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.2 | 1 | 0.2 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.6 | 1 | 0.6 |
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
| wall drifts | 2.1 | 2 | 2.0 |
| fence drifts | 0.1 | 1 | 0.1 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 44.7 | 3 | 39.8 |
| objects | 2.1 | 1 | 2.1 |
| meshes | 1.0 | 1 | 1.0 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.0 | 1 | 1.0 |
| ground | 0.5 | 1 | 0.5 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.1 | 1 | 0.1 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 0.2 | 1 | 0.2 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 0.6 | 1 | 0.6 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 1.3 | 1 | 1.3 |
| interior plans | 0.9 | 1 | 0.9 |
| cutaway | 0.2 | 1 | 0.2 |
| utility lines | 0.0 | 1 | 0.0 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.4 | 1 | 0.4 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.4 | 1 | 0.4 |
| woods | 1.8 | 1 | 1.8 |
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
| colliders | 0.5 | 1 | 0.5 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 0.8 | 1 | 0.8 |
| water | 1.4 | 1 | 1.4 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.1 | 1 | 0.1 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.0 | 1 | 1.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 4.7 | 3 | 2.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 8.5 | 4 | 4.0 |
| interior plans | 18.1 | 7 | 4.2 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.8 | 1 | 0.8 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 1 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.3 | 1 | 0.3 |
| kerbside cars | 0.8 | 1 | 0.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 2.4 | 2 | 2.1 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 1.1 | 1 | 1.1 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.8 | 1 | 0.8 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.1 | 1 | 1.1 |
| junctions | 7.4 | 1 | 7.4 |
| streets | 5.8 | 2 | 5.3 |
| sidewalks | 2.9 | 2 | 2.4 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.6 | 3 | 2.6 |
| interior plans | 10.9 | 5 | 2.8 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.9 | 1 | 0.9 |
| street lights | 0.3 | 1 | 0.3 |
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
| kerbside cars | 1.8 | 1 | 1.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 1 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.5 | 1 | 1.5 |
| colliders | 1.0 | 1 | 1.0 |
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
| lots | 0.6 | 1 | 0.6 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
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
| signs | 1.0 | 1 | 1.0 |
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
| fence rows | 1.8 | 1 | 1.8 |
| woods | 3.4 | 2 | 2.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 1.7 | 1 | 1.7 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.1 | 1 | 0.1 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.6 | 1 | 0.6 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.3 | 1 | 0.3 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 198_154 | yes | 42 | 10 |
| 198_153 | yes | 71 | 6 |
| 197_153 | yes | 20 | 3 |
| 197_154 | yes | 43 | 4 |
| 183_148 | yes | 148 | 9 |
| 196_145 | yes | 399 | 51 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 132 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 470 ms (slowest 85 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **44** | interior plans 4 | 0 | 193 | 53 | 125,744 | 68,219 | 0 | 156 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **54** | interior plans 8 | 1 | 199 | 75 | 147,393 | 81,099 | 0 | 202 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 132 + 8 | **53** | interior plans 9 | 1 | 248 | 62 | 133,432 | 74,574 | 3 | 200 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **72** | buses and cruisers 33 | 1 | 171 | 57 | 121,734 | 64,573 | 0 | 154 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **37** | interior plans 4 | 0 | 186 | 63 | 95,455 | 55,812 | 1 | 134 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **40** | interior plans 7 | 0 | 168 | 53 | 128,615 | 73,625 | 0 | 125 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **39** | interior plans 5 | 1 | 197 | 53 | 126,980 | 68,763 | 0 | 208 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **47** | colliders 10 | 0 | 162 | 56 | 99,435 | 59,622 | 4 | 97 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **85** | interior plans 11 | 1 | 201 | 45 | 209,906 | 116,261 | 0 | 160 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,727 | 517 | 517 | 512 | 1,188,694 | 662,548 | 96 | 2,149 | 8 (0) | 1,436 | 356 | 96 |

### Determinism

Chunk 12683_9851 (start-centre) built twice: **match**: identical, in the same order (243,939 vertices, 128,029 triangles, 336 objects, 120 meshes; hash 0d40c9b01e2dae90 vs 0d40c9b01e2dae90; 373 colliders, lights and loot containers, hash 258874637e47d27f vs 258874637e47d27f). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 103 | 359 / 191 / 44.5 | 0 / 0.0 | 111 / 239,931 / 0.6 | 2.4 | 56.0 | 62 / 3.5 |
| rural-5km | Full | 12 | 92 / 23 / 0.5 | 0 / 0.0 | 37 / 31,387 / 0.2 | 0.7 | 10.9 | 34 / 0.2 |
| louisville-downtown | Full | 52 | 155 / 112 / 2.4 | 0 / 0.0 | 51 / 123,133 / 0.3 | 1.1 | 48.3 | 36 / 0.5 |
| lexington-downtown | Full | 52 | 109 / 73 / 1.0 | 0 / 0.0 | 53 / 130,580 / 0.2 | 1.1 | 49.8 | 39 / 0.3 |
| hazard | Full | 19 | 146 / 19 / 0.6 | 0 / 0.0 | 38 / 54,547 / 0.2 | 0.7 | 17.3 | 31 / 0.2 |
| louisville 0,0 | Full | 44 | 155 / 112 / 1.3 | 0 / 0.0 | 51 / 123,133 / 0.2 | 1.0 | 41.9 | 36 / 0.3 |
| louisville 1,0 | Full | 54 | 201 / 106 / 1.5 | 0 / 0.0 | 73 / 144,724 / 0.4 | 2.0 | 50.7 | 46 / 0.3 |
| louisville -1,0 | Full | 53 | 196 / 139 / 1.8 | 0 / 0.0 | 52 / 130,009 / 0.3 | 1.1 | 49.9 | 40 / 0.3 |
| louisville 0,1 | Full | 72 | 153 / 85 / 1.1 | 0 / 0.0 | 54 / 119,233 / 0.4 | 1.3 | 69.3 | 36 / 0.2 |
| louisville 0,-1 | Full | 37 | 133 / 78 / 1.3 | 0 / 0.0 | 58 / 91,233 / 0.2 | 1.2 | 34.1 | 37 / 0.3 |
| louisville 1,1 | Full | 40 | 124 / 77 / 1.1 | 0 / 0.0 | 50 / 125,547 / 0.3 | 1.5 | 37.7 | 35 / 0.3 |
| louisville -1,1 | Full | 39 | 207 / 114 / 1.5 | 0 / 0.0 | 50 / 123,704 / 0.4 | 1.1 | 36.3 | 37 / 0.3 |
| louisville 1,-1 | Full | 47 | 92 / 58 / 10.4 | 0 / 0.0 | 47 / 95,115 / 0.2 | 1.2 | 35.6 | 36 / 0.7 |
| louisville -1,-1 | Full | 85 | 159 / 112 / 3.8 | 0 / 0.0 | 41 / 206,769 / 0.3 | 0.9 | 79.9 | 30 / 0.3 |

The Louisville block's NavMesh: 333 sources collected in 3 ms; baked at once (as one 384 m square) in 182 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 58 | 8 | 14% | interior plans 2.5 | buildings 2.1 | 243,939 / 11,176 | 336 / 45 | 120 / 40 | 364 / 1 | 5 / 0 |
| rural-5km | 11 | 4 | 35% | woods 1.7 | data 1.0 | 29,632 / 3,225 | 66 / 37 | 37 / 32 | 89 / 1 | 0 / 0 |
| louisville-downtown | 45 | 7 | 16% | interior plans 4.2 | buildings 2.4 | 119,684 / 6,939 | 212 / 33 | 53 / 28 | 159 / 1 | 0 / 0 |
| lexington-downtown | 41 | 5 | 13% | streets 5.4 | sidewalks 1.0 | 127,762 / 4,186 | 175 / 33 | 59 / 28 | 114 / 1 | 0 / 0 |
| hazard | 17 | 4 | 21% | streets 2.7 | woods 0.8 | 61,811 / 4,436 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 58.11 | 29.05 | make 50.74 + switch on 7.36 ms; 1602 objects; destroy 3.94 ms; 3,000 rays 2.77 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 15.64 | 7.82 | make 10.39 + switch on 5.25 ms; 10 objects; destroy 1.66 ms; 3,000 rays 1.99 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 15.27 | 7.63 | make 6.84 + switch on 8.43 ms; 2 objects; destroy 0.10 ms; 3,000 rays 2.11 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 7.31 | 3.66 | make 7.29 + switch on 0.02 ms (+ 8.19 ms baking on the worker); 2 objects; destroy 0.06 ms; 3,000 rays 1.99 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.20 | 204.89 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.76 | 193.94 | objects 0.81, allocate 0.02, pack 6.81, apply 0.12 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 0.94 | 23.49 | objects 0.81, allocate 0.02, pack 6.80 (worker), apply 0.11 ms |
| primitives | Box, turned, flat colour | 20,000 | 26.33 | 1.32 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 82.58 | 4.13 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 45.51 | 2.28 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 103.39 | 5.17 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 25.66 | 1.28 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 54.19 | 2.71 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.46 | 0.32 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.69 | 0.23 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 13.57 | 2.71 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 20.14 | 4.03 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 50.40 | 16.80 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 34.27 | 11.42 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 21.52 | 10.76 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 12.02 | 12.02 |  |
| objects | BoxCollider added to one object | 2,000 | 41.36 | 20.68 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 23.42 | 117.08 |  |
| objects | RoomLight.Spawn | 100 | 11.38 | 113.78 |  |
| objects | switching all of the above on | 1 | 33.83 | 33828.50 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 17.12 | 17116.80 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 78 | 0.64 | 8.16 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.22 | 221.83 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 401.55 | 401554.40 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 390.68 | 390682.20 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 25.92 | 25922.40 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 967 ms, 2,613,936 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (29) | 0.25 | 0.05 | 1.15 | 4.72 | 11.62 | 4 / 2 / 0 / 0 | objects 7.7 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 1,559 / 2,675,190 | 2 | 212 / 891 (pool 40) |
| drive-east | 2400 (135) | 0.26 | 0.05 | 1.29 | 4.71 | 7.04 | 15 / 0 / 0 / 0 | woods 4.3 | 40 (24 / 16) | 0 | 19 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 2,173 / 3,591,606 | 2 | 206 / 931 (pool 40) |
| teleport-louisville | 720 (109) | 1.01 | 0.05 | 6.57 | 12.09 | 40.51 | 55 / 18 / 6 / 3 | (activate) 39.7 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.2 | 12 / 72 | 41 (16 Lite) / 2,162 / 3,894,010 | 4 | 219 / 941 (pool 40) |
| drive-louisville | 1800 (167) | 0.39 | 0.05 | 3.12 | 5.88 | 18.40 | 27 / 10 / 3 / 0 | interior plans 17.1 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 21 | 46 (12 Lite) / 2,573 / 4,744,183 | 6 | 268 / 976 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: objects (1x, worst 7.7 ms), road network (1x, worst 1.4 ms)
- **drive-east**: driving east from the spawn at 90 km/h
- **teleport-louisville**: set down in downtown Louisville (133 km away), standing; the frames over 8 ms were made by: (activate) (1x, worst 39.7 ms), buildings (3x, worst 20.6 ms), interior plans (8x, worst 8.3 ms), sidewalks (1x, worst 3.6 ms), snow tone (1x, worst 2.8 ms), wires (1x, worst 2.7 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (4x, worst 17.1 ms), points (1x, worst 11.2 ms), (swap) (1x, worst 10.1 ms), cutaway (1x, worst 7.8 ms), buildings (1x, worst 2.8 ms), wall drifts (1x, worst 2.1 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 4 edit-mode artefacts

- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.555 | 0% | 2,451 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 361 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.494 | 0% | 139 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.087 | 0% | 217 |
| `iso_downtown_day.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.405 | 0% | 68 |
| `iso_downtown_night.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.086 | 0% | 59 |
| `iso_residential_day.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.475 | 0% | 55 |
| `iso_residential_night.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.084 | 0% | 82 |
| `iso_intersection_day.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.371 | 0% | 101 |
| `iso_intersection_night.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.084 | 0% | 57 |
| `iso_green_day.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.592 | 0% | 77 |
| `iso_green_night.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.080 | 0% | 65 |
| `iso_parking_day.png` | Parking: a parking, 2,703 m² inside the square | 0.390 | 0% | 71 |
| `iso_parking_night.png` | Parking: a parking, 2,703 m² inside the square | 0.080 | 0% | 91 |
| `iso_edge_day.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.405 | 0% | 72 |
| `iso_edge_night.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.075 | 0% | 89 |
| `iso_waterway_day.png` | Waterway: a stream | 0.472 | 0% | 72 |
| `iso_waterway_night.png` | Waterway: a stream | 0.082 | 0% | 61 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.545 | 0% | 98 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.077 | 0% | 81 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.416 | 0% | 147 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.184 | 14% | 130 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.468 | 0% | 72 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.152 | 30% | 80 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.542 | 0% | 294 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.464 | 0% | 63 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.081 | 0% | 56 |

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

### Everything logged during the run: 0 errors, 0 warnings, 4 edit-mode artefacts

- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 71 |  |
| square: KentuckyMap.Build | yes | 7,850 |  |
| square: count | yes | 946 |  |
| square: pick spots | yes | 102 |  |
| square: pictures | yes | 7,212 |  |
| square: discard | yes | 313 |  |
| square: passes (warm rebuild) | yes | 4,733 |  |
| chunks: stress test | yes | 1,127 |  |
| chunks: Louisville 3x3 | yes | 1,785 |  |
| chunks: determinism | yes | 230 |  |
| perf: Lite against Full | yes | 1,108 |  |
| perf: determinism across build orders | yes | 414 |  |
| perf: what things cost | yes | 1,749 |  |
| perf: walking and driving through the state | yes | 163,746 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
