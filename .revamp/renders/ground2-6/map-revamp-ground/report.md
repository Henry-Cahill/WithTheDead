# CLONE-Z map harness: Frankfort (build ky-local-f2)

**Exit 0**: ok. Full run, 177.9 s, started 2026-10-24 10:56:32.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Frankfort plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 4,351 ms |
| the same square again, warm, pass by pass | 1,536 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | -34.3, 21.2, facing 118° |
| exits | Snowfield (north) at 0.0, 197.0; KY 2261 east at 197.0, 12.6; KY 420 south (High Street) at -25.3, -197.0; KY 420 south (Ann Street) at -194.1, -197.0; KY 420 west (West Clinton Street) at -197.0, -46.1; KY 420 west (Mero Street) at -197.0, 116.0 |
| overhead renderers (hidden in the iso view) | 62 |
| `KentuckyMap.LastStats` | Kentucky map Frankfort (build ky-local-f2): 38 buildings, 27 beyond the edge, 131 street pieces, 316 containers, 311 lights, 692 trees, 6 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 38 with their middle inside the square (6 of them reach within 1 m of its edge or past it), 27 in the scenery ring; inside: house 20, commercial 15, church 2, civic 1 |
| map data: streets, lines, areas | 134 street pieces, 6 lines (rail 3, abandoned 2, river 1), 16 land-use areas (parking 11, pitch 2, grass 1, lot 1, park 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 10,135 | 5,389 | 5,389 | 5,082 | 2,809,157 | 1,724,906 | 126 | 12,827 | 324 (0) | 3,547 | 3,813 | 316 |
| RaidOverhead | 207 | 169 | 169 | 169 | 173,543 | 102,323 | 70 | 610 | 0 (0) | 0 | 169 | 0 |
| RaidScenery | 91 | 90 | 90 | 90 | 74,328 | 97,020 | 29 | 191 | 0 (0) | 0 | 57 | 0 |
| **all** | 10,433 | 5,648 | 5,648 | 5,341 | 3,057,028 | 1,924,249 | 178 | 13,628 | 324 (0) | 3,547 | 4,039 | 316 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 9,009 | 5,121 | 5,121 | 4,821 | 2,162,647 | 1,374,547 | 80 | 11,566 | 301 (0) | 2,024 | 3,653 | 248 |
| World/OutdoorProps | 114 | 114 | 114 | 114 | 240,786 | 152,538 | 16 | 430 | 0 (0) | 0 | 114 | 0 |
| World/OutdoorGround | 97 | 97 | 97 | 97 | 88,705 | 38,045 | 13 | 423 | 0 (0) | 0 | 0 | 0 |
| World/Building shells | 47 | 46 | 46 | 46 | 316,280 | 158,664 | 42 | 397 | 0 (0) | 0 | 46 | 0 |
| RaidOverhead/Building shells | 47 | 46 | 46 | 46 | 130,249 | 64,535 | 56 | 320 | 0 (0) | 0 | 46 | 0 |
| RaidOverhead/Roofs | 45 | 45 | 45 | 45 | 16,723 | 10,535 | 29 | 135 | 0 (0) | 0 | 45 | 0 |
| RaidOverhead/Interior KY# | 72 | 36 | 36 | 36 | 22,896 | 21,752 | 5 | 113 | 0 (0) | 0 | 36 | 0 |
| RaidScenery/SceneryGround | 31 | 31 | 31 | 31 | 7,168 | 2,406 | 7 | 95 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SceneryProps | 37 | 37 | 37 | 37 | 26,576 | 19,440 | 21 | 74 | 0 (0) | 0 | 37 | 0 |
| RaidOverhead/Overhead | 26 | 26 | 26 | 26 | 699 | 233 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| RaidScenery/Wires | 20 | 20 | 20 | 20 | 2,253 | 3,942 | 1 | 20 | 0 (0) | 0 | 20 | 0 |
| RaidOverhead/Wires | 16 | 16 | 16 | 16 | 2,976 | 5,268 | 1 | 16 | 0 (0) | 0 | 16 | 0 |
| World/StreetLamp | 20 | 10 | 10 | 3 | 715 | 1,100 | 3 | 10 | 10 (0) | 10 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 38,327 | 71,230 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 9,216 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 58 | 4% | 1 | 58 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 1 | 0% | 1 | 1 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 2 | 0% | 1 | 2 |
| lots | 8 | 1% | 39 | 2 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 0 | 0% | 2 | 0 |
| snow tone | 27 | 2% | 1 | 27 |
| road network | 2 | 0% | 1 | 2 |
| junctions | 3 | 0% | 1 | 3 |
| streets | 42 | 3% | 52 | 28 |
| sidewalks | 14 | 1% | 52 | 3 |
| berms and driveways | 9 | 1% | 49 | 1 |
| road markings | 1 | 0% | 12 | 0 |
| paths | 1 | 0% | 1 | 1 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 37 | 2% | 15 | 5 |
| interior plans | 54 | 4% | 37 | 7 |
| interior shell | 4 | 0% | 36 | 0 |
| interior walls | 38 | 2% | 36 | 4 |
| interior floors | 4 | 0% | 36 | 0 |
| interior doors | 151 | 10% | 72 | 10 |
| interior switches | 20 | 1% | 36 | 1 |
| interior rooms | 818 | 53% | 234 | 117 |
| interior wall meshes | 31 | 2% | 36 | 4 |
| interior finish | 67 | 4% | 36 | 9 |
| cutaway | 15 | 1% | 7 | 2 |
| utility lines | 12 | 1% | 1 | 12 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 3 | 0% | 1 | 3 |
| street lights | 2 | 0% | 1 | 2 |
| signs | 2 | 0% | 1 | 2 |
| street furniture | 5 | 0% | 1 | 5 |
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
| fence rows | 2 | 0% | 156 | 0 |
| woods | 16 | 1% | 9362 | 0 |
| driveway cars | 1 | 0% | 7 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 7 | 0% | 115 | 1 |
| kerbside cars | 7 | 0% | 124 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 12 | 1% | 39 | 3 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 0 | 0% | 1 | 0 |
| (after the last pass) | 5 | 0% | 1 | 5 |
| (emit the overhead meshes) | 3 | 0% | 1 | 3 |
| (emit the outdoor meshes) | 34 | 2% | 1 | 34 |
| **total** | **1,536** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Frankfort | 12318_9434 (192_147) | 0 + 0 | **63** | streets 10 | 1 | 160 | 65 | 101,036 | 57,019 | 3 | 156 | 6 | 15/6/3 | 0E 0W |
| rural-5km | 5 km from Frankfort (bearing 70°) | 12355_9447 (193_147) | 0 + 0 | **22** | interior plans 3 | 0 | 110 | 46 | 50,051 | 33,839 | 0 | 91 | 1 | 8/1/4 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 153 + 9 | **55** | interior plans 6 | 0 | 190 | 49 | 135,368 | 71,868 | 0 | 163 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 412 + 28 | **72** | buildings 13 | 0 | 123 | 57 | 126,319 | 66,319 | 1 | 94 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **17** | streets 3 | 0 | 68 | 43 | 55,052 | 40,570 | 0 | 135 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 9.0 | 1 | 9.0 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.3 | 1 | 0.3 |
| lots | 1.1 | 1 | 1.1 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.6 | 1 | 0.6 |
| streets | 9.5 | 1 | 9.5 |
| sidewalks | 1.7 | 1 | 1.7 |
| berms and driveways | 1.8 | 1 | 1.8 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 3.1 | 2 | 2.1 |
| interior plans | 10.1 | 4 | 5.2 |
| cutaway | 1.9 | 1 | 1.9 |
| utility lines | 0.7 | 1 | 0.7 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.5 | 1 | 0.5 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 0.4 | 1 | 0.4 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.1 | 1 | 0.1 |
| yard trees | 0.2 | 1 | 0.2 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.6 | 1 | 0.6 |
| fence rows | 0.4 | 1 | 0.4 |
| woods | 1.1 | 4 | 0.6 |
| driveway cars | 0.2 | 1 | 0.2 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 1.3 | 1 | 1.3 |
| kerbside cars | 0.7 | 1 | 0.7 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.7 | 1 | 0.7 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 4.4 | 2 | 3.6 |
| objects | 2.9 | 1 | 2.9 |
| meshes | 1.4 | 1 | 1.4 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.2 | 1 | 0.2 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.5 | 1 | 1.5 |
| sidewalks | 0.7 | 1 | 0.7 |
| berms and driveways | 1.6 | 1 | 1.6 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 0.1 | 1 | 0.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 3.8 | 2 | 2.3 |
| interior plans | 3.5 | 2 | 3.5 |
| cutaway | 0.4 | 1 | 0.4 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.5 | 1 | 0.5 |
| woods | 2.1 | 4 | 1.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 1.0 | 1 | 1.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.4 | 1 | 0.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 1.5 | 1 | 1.5 |
| water | 2.6 | 2 | 2.6 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.1 | 1 | 0.1 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.1 | 1 | 1.1 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 4.3 | 1 | 4.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.8 | 5 | 2.4 |
| interior plans | 17.7 | 6 | 5.9 |
| cutaway | 3.7 | 2 | 2.3 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.8 | 1 | 0.8 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.3 | 1 | 0.3 |
| kerbside cars | 0.8 | 1 | 0.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.2 | 2 | 2.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.3 | 1 | 1.3 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 5.0 | 1 | 5.0 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.5 | 1 | 0.5 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 6.6 | 1 | 6.6 |
| sidewalks | 2.7 | 2 | 2.3 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 25.2 | 6 | 12.5 |
| interior plans | 11.6 | 4 | 5.2 |
| cutaway | 2.8 | 2 | 2.7 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.7 | 1 | 0.7 |
| street furniture | 1.3 | 1 | 1.3 |
| landmarks | 1.4 | 1 | 1.4 |
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
| woods | 0.3 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.6 | 1 | 0.6 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.9 | 1 | 1.9 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.5 | 1 | 1.5 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.1 | 1 | 0.1 |
| lots | 0.3 | 1 | 0.3 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 3.2 | 1 | 3.2 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.0 | 1 | 0.0 |
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
| fence rows | 1.7 | 1 | 1.7 |
| woods | 3.0 | 4 | 1.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.4 | 1 | 0.4 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 192_148 | yes | 4 | 15 |
| 192_147 | yes | 29 | 5 |
| 193_147 | yes | 32 | 3 |
| 192_146 | yes | 8 | 3 |
| 191_147 | yes | 4 | 1 |
| 183_148 | yes | 153 | 9 |
| 196_145 | yes | 412 | 28 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 136 | 17 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 555 ms (slowest 104 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **48** | interior plans 6 | 1 | 190 | 49 | 135,368 | 71,868 | 0 | 163 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **104** | interior plans 29 | 1 | 197 | 75 | 144,751 | 79,284 | 0 | 200 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 136 + 17 | **78** | interior plans 12 | 1 | 213 | 55 | 110,019 | 61,382 | 1 | 191 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **79** | interior plans 31 | 0 | 182 | 59 | 119,742 | 62,713 | 1 | 134 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **55** | buildings 9 | 0 | 180 | 60 | 87,387 | 50,336 | 1 | 126 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **41** | interior plans 9 | 0 | 150 | 53 | 112,601 | 65,110 | 0 | 125 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **54** | interior plans 9 | 1 | 211 | 53 | 119,297 | 64,680 | 0 | 202 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **39** | interior plans 6 | 1 | 161 | 53 | 101,873 | 59,889 | 3 | 96 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **56** | interior plans 8 | 0 | 185 | 41 | 187,996 | 103,233 | 0 | 154 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,671 | 498 | 498 | 495 | 1,119,034 | 618,495 | 132 | 2,133 | 6 (0) | 1,391 | 339 | 84 |

### Determinism

Chunk 12318_9434 (start-centre) built twice: **match**: identical, in the same order (101,036 vertices, 57,019 triangles, 160 objects, 65 meshes; hash 209232b47f67ce8e vs 209232b47f67ce8e; 165 colliders, lights and loot containers, hash fe57d792ae8dcdbc vs fe57d792ae8dcdbc). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 63 | 153 / 66 / 4.3 | 0 / 0.0 | 58 / 97,327 / 2.1 | 3.0 | 55.8 | 37 / 4.2 |
| rural-5km | Full | 22 | 90 / 40 / 0.7 | 0 / 0.0 | 42 / 46,894 / 0.3 | 1.0 | 20.4 | 33 / 0.2 |
| louisville-downtown | Full | 55 | 162 / 117 / 1.3 | 0 / 0.0 | 48 / 132,775 / 3.3 | 4.1 | 49.8 | 36 / 0.3 |
| lexington-downtown | Full | 72 | 92 / 56 / 0.7 | 0 / 0.0 | 53 / 123,590 / 2.3 | 3.2 | 67.9 | 39 / 0.3 |
| hazard | Full | 17 | 134 / 12 / 0.4 | 0 / 0.0 | 38 / 51,638 / 0.1 | 0.7 | 15.7 | 30 / 0.2 |
| louisville 0,0 | Full | 48 | 162 / 117 / 1.4 | 0 / 0.0 | 48 / 132,775 / 2.5 | 3.5 | 43.5 | 36 / 0.3 |
| louisville 1,0 | Full | 104 | 199 / 108 / 1.8 | 0 / 0.0 | 73 / 142,082 / 2.9 | 4.9 | 96.8 | 46 / 0.4 |
| louisville -1,0 | Full | 78 | 189 / 132 / 1.6 | 0 / 0.0 | 49 / 107,112 / 1.5 | 2.5 | 73.9 | 38 / 0.3 |
| louisville 0,1 | Full | 79 | 132 / 77 / 0.9 | 0 / 0.0 | 54 / 117,166 / 2.1 | 3.3 | 74.8 | 37 / 0.3 |
| louisville 0,-1 | Full | 55 | 125 / 74 / 0.9 | 0 / 0.0 | 55 / 84,128 / 1.0 | 2.0 | 52.2 | 37 / 0.3 |
| louisville 1,1 | Full | 41 | 124 / 78 / 1.0 | 0 / 0.0 | 50 / 109,530 / 2.1 | 3.0 | 37.5 | 35 / 0.3 |
| louisville -1,1 | Full | 54 | 201 / 112 / 1.5 | 0 / 0.0 | 50 / 116,012 / 2.0 | 2.8 | 50.2 | 37 / 0.3 |
| louisville 1,-1 | Full | 39 | 92 / 60 / 0.7 | 0 / 0.0 | 45 / 97,619 / 1.2 | 2.6 | 35.6 | 34 / 0.3 |
| louisville -1,-1 | Full | 56 | 153 / 109 / 1.3 | 0 / 0.0 | 39 / 185,366 / 3.3 | 3.9 | 51.0 | 30 / 0.4 |

The Louisville block's NavMesh: 330 sources collected in 20 ms; baked at once (as one 384 m square) in 191 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 38 | 13 | 35% | streets 9.8 | streets 8.5 | 101,036 / 6,604 | 160 / 35 | 65 / 30 | 156 / 1 | 3 / 0 |
| rural-5km | 19 | 4 | 22% | interior plans 3.3 | streets 0.7 | 52,148 / 4,511 | 112 / 31 | 48 / 26 | 89 / 1 | 1 / 0 |
| louisville-downtown | 51 | 10 | 20% | interior plans 6.0 | buildings 2.4 | 128,996 / 8,579 | 211 / 36 | 49 / 31 | 165 / 1 | 0 / 0 |
| lexington-downtown | 49 | 11 | 22% | streets 6.7 | streets 5.4 | 120,670 / 6,044 | 142 / 32 | 59 / 27 | 96 / 1 | 2 / 0 |
| hazard | 19 | 5 | 29% | streets 3.2 | streets 2.0 | 63,930 / 4,578 | 88 / 32 | 45 / 27 | 151 / 1 | 1 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 56.51 | 28.26 | make 49.24 + switch on 7.28 ms; 1602 objects; destroy 3.06 ms; 3,000 rays 2.69 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 14.70 | 7.35 | make 9.99 + switch on 4.70 ms; 10 objects; destroy 1.63 ms; 3,000 rays 1.98 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.87 | 5.94 | make 3.99 + switch on 7.88 ms; 2 objects; destroy 0.11 ms; 3,000 rays 2.05 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.16 | 1.08 | make 2.12 + switch on 0.04 ms (+ 8.54 ms baking on the worker); 2 objects; destroy 0.12 ms; 3,000 rays 2.43 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.75 | 218.62 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.15 | 203.81 | objects 0.84, allocate 0.02, pack 7.18, apply 0.12 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.08 | 26.94 | objects 0.95, allocate 0.02, pack 6.81 (worker), apply 0.11 ms |
| primitives | Box, turned, flat colour | 20,000 | 38.90 | 1.95 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 56.69 | 2.84 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 41.29 | 2.06 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 97.97 | 4.90 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 24.26 | 1.21 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 17.03 | 0.85 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.57 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.81 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 32.11 | 6.42 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 25.20 | 5.04 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 27.11 | 9.04 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 35.30 | 11.77 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.29 | 6.65 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.26 | 11.26 |  |
| objects | BoxCollider added to one object | 2,000 | 39.16 | 19.58 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 24.24 | 121.19 |  |
| objects | RoomLight.Spawn | 100 | 11.11 | 111.13 |  |
| objects | switching all of the above on | 1 | 26.99 | 26993.50 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 11.92 | 11916.90 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 59 | 0.59 | 9.96 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.01 | 201.23 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 376.74 | 376736.80 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 537.49 | 537494.40 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 22.50 | 22497.10 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 519 ms, 1,054,691 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (25) | 0.20 | 0.04 | 0.65 | 4.34 | 10.71 | 2 / 1 / 0 / 0 | landmarks 3.5 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 866 / 1,120,072 | 1 | 175 / 784 (pool 31) |
| drive-east | 2400 (186) | 0.32 | 0.05 | 2.36 | 5.23 | 20.75 | 31 / 10 / 3 / 0 | signs 19.7 | 40 (24 / 16) | 0 | 19 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 1,471 / 1,931,301 | 1 | 176 / 825 (pool 31) |
| teleport-louisville | 720 (122) | 1.01 | 0.05 | 6.43 | 12.91 | 39.08 | 57 / 17 / 3 / 2 | interior plans 19.9 | 25 (9 / 16) | 0 | 30 | 0.1 | 1.3 | 8 / 80 | 41 (16 Lite) / 1,505 / 2,281,328 | 3 | 195 / 838 (pool 33) |
| drive-louisville | 1800 (191) | 0.50 | 0.05 | 3.50 | 7.23 | 30.31 | 40 / 18 / 3 / 0 | interior plans 19.0 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 24 | 46 (12 Lite) / 1,895 / 3,077,049 | 5 | 242 / 874 (pool 33) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: streets (1x, worst 1.5 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: signs (1x, worst 19.7 ms), woods (2x, worst 18.6 ms), buildings (1x, worst 15.9 ms), interior plans (2x, worst 12.4 ms), lots (1x, worst 10.3 ms), streets (2x, worst 7.1 ms)
- **teleport-louisville**: set down in downtown Louisville (78 km away), standing; the frames over 8 ms were made by: interior plans (9x, worst 19.9 ms), junctions (2x, worst 10.3 ms), streets (1x, worst 4.2 ms), utility poles (2x, worst 3.7 ms), cutaway (1x, worst 3.3 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (5x, worst 19.0 ms), landmarks (1x, worst 11.7 ms), buildings (4x, worst 10.7 ms), mapped trees (1x, worst 10.0 ms), wall drifts (1x, worst 5.4 ms), sidewalks (1x, worst 2.3 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.575 | 0% | 1,173 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 234 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on Mero Street, at the game's starting zoom | 0.435 | 0% | 144 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on Mero Street, at the game's starting zoom | 0.102 | 0% | 89 |
| `iso_downtown_day.png` | Downtown Frankfort: the middle of the square, on Elk Alley | 0.404 | 0% | 58 |
| `iso_downtown_night.png` | Downtown Frankfort: the middle of the square, on Elk Alley | 0.073 | 0% | 65 |
| `iso_residential_day.png` | The densest residential street: Lewis Street, 4 homes within 30 m | 0.484 | 0% | 52 |
| `iso_residential_night.png` | The densest residential street: Lewis Street, 4 homes within 30 m | 0.082 | 0% | 68 |
| `iso_intersection_day.png` | The busiest intersection: Ann Street / West Clinton Street (4 ways in) | 0.341 | 0% | 58 |
| `iso_intersection_night.png` | The busiest intersection: Ann Street / West Clinton Street (4 ways in) | 0.072 | 0% | 69 |
| `iso_green_day.png` | Green: park 'Fort Hill Civil War Park', 5,319 m² inside the square | 0.588 | 0% | 60 |
| `iso_green_night.png` | Green: park 'Fort Hill Civil War Park', 5,319 m² inside the square | 0.078 | 0% | 58 |
| `iso_parking_day.png` | Parking: a parking, 7,259 m² inside the square | 0.386 | 0% | 105 |
| `iso_parking_night.png` | Parking: a parking, 7,259 m² inside the square | 0.075 | 0% | 63 |
| `iso_edge_day.png` | The square's edge: where Holmes Street leaves it (east), the playable map giving way to the scenery ring | 0.466 | 0% | 64 |
| `iso_edge_night.png` | The square's edge: where Holmes Street leaves it (east), the playable map giving way to the scenery ring | 0.076 | 0% | 52 |
| `iso_waterway_day.png` | Railway: a rail | 0.558 | 0% | 49 |
| `iso_waterway_night.png` | Railway: a rail | 0.079 | 0% | 54 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, commercial 'Kentucky Transportation Cabinet' (9,435 m² footprint) | 0.454 | 0% | 58 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, commercial 'Kentucky Transportation Cabinet' (9,435 m² footprint) | 0.090 | 0% | 53 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on Mero Street (downtown), looking along the street | 0.410 | 0% | 62 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on Mero Street (downtown), looking along the street | 0.175 | 22% | 100 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of Lewis Street (the densest residential street), looking along it | 0.388 | 0% | 53 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of Lewis Street (the densest residential street), looking along it | 0.141 | 20% | 51 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.540 | 0% | 122 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.469 | 0% | 63 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 68 |

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
| setup | yes | 65 |  |
| square: KentuckyMap.Build | yes | 4,353 |  |
| square: count | yes | 311 |  |
| square: pick spots | yes | 60 |  |
| square: pictures | yes | 5,189 |  |
| square: discard | yes | 76 |  |
| square: passes (warm rebuild) | yes | 1,617 |  |
| chunks: stress test | yes | 1,007 |  |
| chunks: Louisville 3x3 | yes | 1,782 |  |
| chunks: determinism | yes | 147 |  |
| perf: Lite against Full | yes | 1,108 |  |
| perf: determinism across build orders | yes | 409 |  |
| perf: what things cost | yes | 1,744 |  |
| perf: walking and driving through the state | yes | 160,065 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
