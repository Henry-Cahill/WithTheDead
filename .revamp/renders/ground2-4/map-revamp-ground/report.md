# CLONE-Z map harness: Falmouth (build ky-local-f2)

**Exit 0**: ok. Full run, 204.0 s, started 2026-10-24 10:29:56.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Falmouth plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,879 ms |
| the same square again, warm, pass by pass | 4,424 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 42.7, -4.8, facing -67° |
| exits | Chapel Street (north) at -2.5, 197.0; KY 22 east at 197.0, 173.2; KY 22 south at -60.4, -197.0; West Shelby Street at -197.0, 107.3 |
| overhead renderers (hidden in the iso view) | 215 |
| `KentuckyMap.LastStats` | Kentucky map Falmouth (build ky-local-f2): 150 buildings, 162 beyond the edge, 458 street pieces, 829 containers, 1125 lights, 510 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 150 with their middle inside the square (11 of them reach within 1 m of its edge or past it), 162 in the scenery ring; inside: house 69, garage 27, commercial 19, shed 9, apartments 8, retail 6, office 5, civic 4, church 2, canopy 1 |
| map data: streets, lines, areas | 458 street pieces, 7 lines (stream 5, rail 1, river 1), 91 land-use areas (residential 27, parking 15, commercial 8, grass 8, churchyard 7, forest 7, industrial 6, pitch 3, retail 3, school 2, water 2, meadow 1, park 1, plaza 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 32,071 | 18,063 | 18,063 | 16,941 | 8,513,607 | 5,227,392 | 148 | 41,195 | 1,156 (0) | 9,557 | 12,644 | 829 |
| RaidOverhead | 694 | 545 | 545 | 545 | 434,237 | 259,901 | 118 | 2,094 | 0 (0) | 0 | 545 | 0 |
| RaidScenery | 112 | 111 | 111 | 111 | 104,406 | 110,804 | 67 | 487 | 0 (0) | 0 | 62 | 0 |
| **all** | 32,877 | 18,719 | 18,719 | 17,597 | 9,052,250 | 5,598,097 | 232 | 43,776 | 1,156 (0) | 9,557 | 13,251 | 829 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 30,041 | 17,606 | 17,606 | 16,497 | 7,042,917 | 4,479,563 | 80 | 38,293 | 1,110 (0) | 6,451 | 12,342 | 782 |
| World/Building shells | 164 | 163 | 163 | 163 | 1,025,136 | 515,538 | 59 | 1,483 | 0 (0) | 0 | 163 | 0 |
| RaidOverhead/Building shells | 164 | 163 | 163 | 163 | 286,532 | 138,475 | 97 | 1,036 | 0 (0) | 0 | 163 | 0 |
| World/OutdoorGround | 139 | 139 | 139 | 139 | 159,378 | 65,282 | 20 | 754 | 0 (0) | 0 | 0 | 0 |
| World/OutdoorProps | 138 | 138 | 138 | 138 | 285,017 | 165,265 | 17 | 648 | 0 (0) | 0 | 138 | 0 |
| RaidOverhead/Roofs | 132 | 132 | 132 | 132 | 46,545 | 27,067 | 64 | 486 | 0 (0) | 0 | 132 | 0 |
| RaidOverhead/Interior KY# | 294 | 147 | 147 | 147 | 93,570 | 84,604 | 5 | 469 | 0 (0) | 0 | 147 | 0 |
| RaidScenery/SceneryProps | 40 | 40 | 40 | 40 | 52,384 | 31,258 | 53 | 291 | 0 (0) | 0 | 40 | 0 |
| RaidScenery/SceneryGround | 47 | 47 | 47 | 47 | 13,393 | 5,598 | 13 | 172 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 68 | 68 | 68 | 68 | 2,481 | 827 | 1 | 68 | 0 (0) | 0 | 68 | 0 |
| RaidOverhead/Wires | 35 | 35 | 35 | 35 | 5,109 | 8,928 | 1 | 35 | 0 (0) | 0 | 35 | 0 |
| RaidScenery/Wires | 22 | 22 | 22 | 22 | 1,755 | 3,102 | 1 | 22 | 0 (0) | 0 | 22 | 0 |
| World/StreetLamp | 30 | 15 | 15 | 2 | 1,111 | 1,720 | 2 | 15 | 15 (0) | 15 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 36,870 | 70,844 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 22,294 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 97 | 2% | 1 | 97 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 4 | 0% | 1 | 4 |
| water | 11 | 0% | 1 | 11 |
| fields | 0 | 0% | 1 | 0 |
| lots | 2 | 0% | 1 | 2 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 2 | 0% | 1 | 2 |
| snow tone | 54 | 1% | 1 | 54 |
| road network | 20 | 0% | 1 | 20 |
| junctions | 21 | 0% | 1 | 21 |
| streets | 31 | 1% | 113 | 19 |
| sidewalks | 25 | 1% | 113 | 4 |
| berms and driveways | 16 | 0% | 106 | 1 |
| road markings | 1 | 0% | 6 | 1 |
| paths | 13 | 0% | 1 | 13 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 3 | 0% | 1 | 3 |
| buildings | 133 | 3% | 56 | 6 |
| interior plans | 261 | 6% | 150 | 16 |
| interior shell | 15 | 0% | 147 | 0 |
| interior walls | 147 | 3% | 147 | 9 |
| interior floors | 16 | 0% | 147 | 1 |
| interior doors | 478 | 11% | 275 | 18 |
| interior switches | 79 | 2% | 147 | 7 |
| interior rooms | 2,340 | 53% | 795 | 23 |
| interior wall meshes | 95 | 2% | 147 | 4 |
| interior finish | 227 | 5% | 147 | 9 |
| cutaway | 63 | 1% | 28 | 5 |
| utility lines | 16 | 0% | 1 | 16 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 5 | 0% | 1 | 5 |
| street lights | 3 | 0% | 1 | 3 |
| signs | 7 | 0% | 1 | 7 |
| street furniture | 55 | 1% | 1 | 55 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 3 | 0% | 53 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 6 | 0% | 1 | 6 |
| nature plan | 4 | 0% | 1 | 4 |
| driveway plan | 2 | 0% | 1 | 2 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 11 | 0% | 173 | 0 |
| grounds trees | 1 | 0% | 7 | 0 |
| street trees | 2 | 0% | 159 | 0 |
| creek trees | 0 | 0% | 4 | 0 |
| fence rows | 1 | 0% | 33 | 0 |
| woods | 7 | 0% | 9347 | 0 |
| driveway cars | 2 | 0% | 22 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 29 | 2 |
| kerbside cars | 6 | 0% | 98 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 1 | 0% | 1 | 1 |
| wall drifts | 29 | 1% | 147 | 1 |
| fence drifts | 4 | 0% | 51 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 2 | 0% | 1 | 2 |
| points | 1 | 0% | 1 | 1 |
| (after the last pass) | 15 | 0% | 1 | 15 |
| (emit the overhead meshes) | 15 | 0% | 1 | 15 |
| (emit the outdoor meshes) | 65 | 1% | 1 | 65 |
| **total** | **4,424** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Falmouth | 12683_9851 (198_153) | 0 + 0 | **111** | data 43 | 2 | 321 | 113 | 236,103 | 121,476 | 4 | 370 | 25 | 34/25/9 | 0E 0W |
| rural-5km | 5 km from Falmouth (bearing 50°) | 12713_9876 (198_154) | 0 + 0 | **19** | buildings 8 | 0 | 68 | 38 | 34,611 | 25,221 | 0 | 93 | 2 | 2/2/6 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 183 + 10 | **70** | work yards 12 | 1 | 202 | 49 | 134,698 | 71,630 | 0 | 161 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 454 + 28 | **69** | interior plans 11 | 0 | 131 | 57 | 125,737 | 65,967 | 1 | 92 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **54** | wires 34 | 1 | 82 | 44 | 60,372 | 44,210 | 0 | 148 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 43.3 | 1 | 43.3 |
| ground | 0.5 | 1 | 0.5 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 1.4 | 1 | 1.4 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.3 | 1 | 1.3 |
| junctions | 0.7 | 1 | 0.7 |
| streets | 3.5 | 1 | 3.5 |
| sidewalks | 2.7 | 2 | 2.0 |
| berms and driveways | 1.6 | 1 | 1.6 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.4 | 1 | 0.4 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.0 | 5 | 2.1 |
| interior plans | 21.1 | 9 | 2.9 |
| cutaway | 6.1 | 3 | 2.2 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.2 | 1 | 1.2 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.5 | 1 | 0.5 |
| street furniture | 1.0 | 1 | 1.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.1 | 1 | 0.1 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.2 | 1 | 0.2 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.5 | 1 | 0.5 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.0 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.2 | 1 | 0.2 |
| kerbside cars | 0.8 | 1 | 0.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.1 | 1 | 0.1 |
| wall drifts | 2.4 | 2 | 2.1 |
| fence drifts | 0.1 | 1 | 0.1 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 2.5 | 2 | 2.1 |
| objects | 3.0 | 1 | 3.0 |
| meshes | 1.6 | 1 | 1.6 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.0 | 1 | 1.0 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.4 | 1 | 0.4 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 0.8 | 1 | 0.8 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 7.8 | 2 | 7.7 |
| interior plans | 0.9 | 1 | 0.9 |
| cutaway | 0.4 | 1 | 0.4 |
| utility lines | 0.1 | 1 | 0.1 |
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
| mapped trees | 0.4 | 1 | 0.4 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.4 | 1 | 0.4 |
| woods | 1.7 | 4 | 1.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.3 | 1 | 0.3 |
| fence drifts | 0.5 | 1 | 0.5 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.4 | 1 | 0.4 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 0.8 | 1 | 0.8 |
| water | 1.4 | 1 | 1.4 |
| fields | 0.1 | 1 | 0.1 |
| lots | 0.1 | 1 | 0.1 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 12.0 | 1 | 12.0 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 0.7 | 1 | 0.7 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.1 | 1 | 1.1 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 4.3 | 1 | 4.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.8 | 5 | 3.7 |
| interior plans | 20.0 | 6 | 6.6 |
| cutaway | 3.0 | 2 | 2.2 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.1 | 1 | 1.1 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.4 | 1 | 0.4 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.9 | 1 | 0.9 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.7 | 1 | 0.7 |
| kerbside cars | 1.4 | 1 | 1.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.6 | 2 | 2.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.4 | 1 | 1.4 |
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 1.0 | 1 | 1.0 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.8 | 1 | 0.8 |
| snow tone | 3.3 | 1 | 3.3 |
| road network | 1.2 | 1 | 1.2 |
| junctions | 2.0 | 1 | 2.0 |
| streets | 6.7 | 1 | 6.7 |
| sidewalks | 3.2 | 2 | 2.6 |
| berms and driveways | 1.7 | 1 | 1.7 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 1.2 | 1 | 1.2 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 12.1 | 5 | 2.9 |
| interior plans | 18.9 | 4 | 10.8 |
| cutaway | 5.1 | 2 | 4.3 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.7 | 1 | 0.7 |
| street furniture | 1.0 | 1 | 1.0 |
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
| woods | 0.5 | 4 | 0.4 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.8 | 1 | 0.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.9 | 1 | 1.9 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.5 | 1 | 1.5 |
| colliders | 0.7 | 1 | 0.7 |
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
| lots | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 3.5 | 1 | 3.5 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 34.0 | 1 | 34.0 |
| nature plan | 0.4 | 1 | 0.4 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 2.2 | 2 | 2.0 |
| woods | 3.4 | 4 | 1.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.9 | 1 | 0.9 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 1.1 | 1 | 1.1 |
| meshes | 0.3 | 1 | 0.3 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 198_154 | yes | 53 | 13 |
| 198_153 | yes | 82 | 16 |
| 197_153 | yes | 14 | 5 |
| 197_154 | yes | 42 | 5 |
| 183_148 | yes | 183 | 10 |
| 196_145 | yes | 454 | 28 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 156 | 8 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 557 ms (slowest 88 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **68** | interior plans 8 | 1 | 202 | 49 | 134,698 | 71,630 | 0 | 161 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **82** | interior plans 8 | 1 | 200 | 75 | 144,765 | 79,396 | 0 | 200 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 156 + 8 | **72** | interior plans 9 | 1 | 208 | 55 | 105,530 | 58,485 | 1 | 183 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **62** | buses and cruisers 5 | 0 | 164 | 59 | 120,230 | 62,943 | 1 | 136 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **56** | interior plans 11 | 0 | 165 | 60 | 87,691 | 50,352 | 1 | 127 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **39** | interior plans 9 | 0 | 160 | 53 | 110,607 | 63,834 | 0 | 119 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **88** | buildings 51 | 1 | 203 | 53 | 115,324 | 62,124 | 0 | 194 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **34** | interior plans 5 | 0 | 152 | 54 | 99,471 | 58,455 | 3 | 88 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **55** | interior plans 8 | 1 | 174 | 41 | 186,506 | 102,392 | 0 | 149 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,630 | 499 | 499 | 496 | 1,104,822 | 609,611 | 132 | 2,142 | 6 (0) | 1,357 | 340 | 84 |

### Determinism

Chunk 12683_9851 (start-centre) built twice: **match**: identical, in the same order (236,103 vertices, 121,476 triangles, 321 objects, 113 meshes; hash 134de7b39daed330 vs 134de7b39daed330; 378 colliders, lights and loot containers, hash 552f7a606dec6e21 vs 552f7a606dec6e21). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 111 | 368 / 183 / 2.4 | 0 / 0.0 | 105 / 233,004 / 5.3 | 7.1 | 101.9 | 59 / 3.3 |
| rural-5km | Full | 19 | 92 / 23 / 0.4 | 0 / 0.0 | 37 / 32,011 / 0.3 | 1.0 | 18.0 | 34 / 0.2 |
| louisville-downtown | Full | 70 | 160 / 114 / 1.4 | 0 / 0.0 | 48 / 132,105 / 2.5 | 3.6 | 65.5 | 36 / 0.3 |
| lexington-downtown | Full | 69 | 90 / 53 / 0.7 | 0 / 0.0 | 54 / 123,074 / 4.7 | 5.6 | 62.7 | 39 / 0.4 |
| hazard | Full | 54 | 147 / 20 / 0.7 | 0 / 0.0 | 39 / 56,814 / 0.2 | 1.1 | 52.0 | 31 / 0.3 |
| louisville 0,0 | Full | 68 | 160 / 114 / 2.6 | 0 / 0.0 | 48 / 132,105 / 3.7 | 5.1 | 60.5 | 36 / 0.4 |
| louisville 1,0 | Full | 82 | 199 / 106 / 2.8 | 0 / 0.0 | 73 / 142,096 / 4.0 | 5.6 | 73.4 | 46 / 0.9 |
| louisville -1,0 | Full | 72 | 181 / 122 / 1.8 | 0 / 0.0 | 49 / 102,632 / 2.7 | 4.0 | 66.5 | 38 / 0.8 |
| louisville 0,1 | Full | 62 | 134 / 79 / 1.3 | 0 / 0.0 | 54 / 117,690 / 2.0 | 3.9 | 56.3 | 37 / 0.8 |
| louisville 0,-1 | Full | 56 | 126 / 74 / 1.0 | 0 / 0.0 | 55 / 84,414 / 1.2 | 2.3 | 53.1 | 37 / 0.3 |
| louisville 1,1 | Full | 39 | 118 / 73 / 0.9 | 0 / 0.0 | 50 / 107,536 / 1.9 | 2.8 | 35.8 | 35 / 0.2 |
| louisville -1,1 | Full | 88 | 193 / 104 / 1.4 | 0 / 0.0 | 50 / 112,192 / 1.9 | 2.9 | 84.0 | 37 / 0.2 |
| louisville 1,-1 | Full | 34 | 84 / 50 / 0.6 | 0 / 0.0 | 46 / 95,217 / 1.1 | 1.8 | 31.6 | 35 / 0.2 |
| louisville -1,-1 | Full | 55 | 148 / 103 / 1.8 | 0 / 0.0 | 39 / 183,876 / 3.7 | 4.6 | 48.6 | 30 / 0.2 |

The Louisville block's NavMesh: 331 sources collected in 2 ms; baked at once (as one 384 m square) in 191 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 62 | 10 | 16% | streets 3.1 | buildings 2.1 | 236,103 / 14,632 | 321 / 45 | 113 / 40 | 370 / 1 | 4 / 0 |
| rural-5km | 9 | 5 | 49% | snow tone 1.3 | snow tone 1.3 | 30,060 / 5,148 | 66 / 37 | 37 / 32 | 89 / 1 | 0 / 0 |
| louisville-downtown | 47 | 11 | 23% | interior plans 6.2 | buildings 2.4 | 127,100 / 10,500 | 202 / 36 | 49 / 31 | 160 / 1 | 0 / 0 |
| lexington-downtown | 42 | 12 | 27% | streets 6.5 | streets 5.4 | 119,871 / 7,969 | 129 / 32 | 57 / 27 | 95 / 1 | 0 / 0 |
| hazard | 19 | 6 | 32% | streets 3.3 | streets 2.0 | 65,490 / 6,257 | 94 / 32 | 44 / 27 | 155 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 70.97 | 35.49 | make 62.79 + switch on 8.18 ms; 1602 objects; destroy 3.72 ms; 3,000 rays 2.49 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 15.26 | 7.63 | make 10.56 + switch on 4.70 ms; 10 objects; destroy 1.59 ms; 3,000 rays 1.89 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.26 | 5.63 | make 3.91 + switch on 7.35 ms; 2 objects; destroy 0.12 ms; 3,000 rays 1.94 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.17 | 1.09 | make 2.13 + switch on 0.04 ms (+ 9.26 ms baking on the worker); 2 objects; destroy 0.10 ms; 3,000 rays 2.15 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.50 | 212.61 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.17 | 204.27 | objects 0.75, allocate 0.02, pack 7.25, apply 0.15 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.29 | 32.20 | objects 1.12, allocate 0.02, pack 6.99 (worker), apply 0.15 ms |
| primitives | Box, turned, flat colour | 20,000 | 33.43 | 1.67 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 109.11 | 5.46 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 66.27 | 3.31 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 88.23 | 4.41 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 32.52 | 1.63 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 23.72 | 1.19 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.72 | 0.34 | 80,000 vertices |
| primitives | Tri | 20,000 | 5.82 | 0.29 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 43.62 | 8.73 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 24.60 | 4.92 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 27.59 | 9.20 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 43.51 | 14.50 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 17.79 | 8.89 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.59 | 11.59 |  |
| objects | BoxCollider added to one object | 2,000 | 40.72 | 20.36 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 22.58 | 112.88 |  |
| objects | RoomLight.Spawn | 100 | 10.20 | 102.02 |  |
| objects | switching all of the above on | 1 | 29.54 | 29539.10 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 11.25 | 11251.60 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 78 | 0.67 | 8.56 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.39 | 238.95 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 427.51 | 427506.60 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 457.59 | 457594.20 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 32.65 | 32649.90 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 1,257 ms, 2,572,587 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (26) | 0.27 | 0.05 | 1.31 | 4.85 | 11.72 | 4 / 2 / 0 / 0 | junctions 7.9 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 1,537 / 2,655,818 | 2 | 191 / 886 (pool 40) |
| drive-east | 2400 (147) | 0.26 | 0.05 | 2.06 | 4.64 | 9.25 | 12 / 2 / 0 / 0 | nature plan 7.7 | 40 (24 / 16) | 0 | 19 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 2,151 / 3,580,572 | 2 | 193 / 926 (pool 40) |
| teleport-louisville | 720 (162) | 0.97 | 0.05 | 4.59 | 8.91 | 38.15 | 30 / 11 / 2 / 2 | interior plans 14.2 | 25 (9 / 16) | 0 | 30 | 0.2 | 2.0 | 9 / 123 | 41 (16 Lite) / 2,130 / 3,812,358 | 4 | 210 / 934 (pool 40) |
| drive-louisville | 1800 (188) | 0.43 | 0.05 | 3.37 | 5.30 | 25.09 | 25 / 5 / 2 / 0 | snow tone 24.4 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 37 | 46 (12 Lite) / 2,522 / 4,571,176 | 6 | 255 / 968 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: junctions (1x, worst 7.9 ms), snow tone (1x, worst 1.6 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: nature plan (1x, worst 7.7 ms), streets (1x, worst 5.2 ms)
- **teleport-louisville**: set down in downtown Louisville (133 km away), standing; the frames over 8 ms were made by: interior plans (9x, worst 14.2 ms), cutaway (1x, worst 3.8 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: snow tone (1x, worst 24.4 ms), interior plans (3x, worst 18.7 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 28 edit-mode artefacts

- `(edit mode) Error: Raid_sign_BOOKS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
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

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.550 | 0% | 6,617 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.069 | 0% | 328 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.444 | 0% | 2,988 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.082 | 0% | 3,056 |
| `iso_downtown_day.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.416 | 0% | 61 |
| `iso_downtown_night.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.088 | 0% | 66 |
| `iso_residential_day.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.473 | 0% | 61 |
| `iso_residential_night.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.084 | 0% | 67 |
| `iso_intersection_day.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.350 | 0% | 71 |
| `iso_intersection_night.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.084 | 0% | 59 |
| `iso_green_day.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.592 | 0% | 51 |
| `iso_green_night.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.080 | 0% | 58 |
| `iso_parking_day.png` | Parking: a parking, 2,703 m² inside the square | 0.380 | 0% | 54 |
| `iso_parking_night.png` | Parking: a parking, 2,703 m² inside the square | 0.080 | 0% | 61 |
| `iso_edge_day.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.358 | 0% | 2,499 |
| `iso_edge_night.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.072 | 0% | 2,745 |
| `iso_waterway_day.png` | Waterway: a stream | 0.469 | 0% | 62 |
| `iso_waterway_night.png` | Waterway: a stream | 0.082 | 0% | 62 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.549 | 0% | 51 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.079 | 0% | 64 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.384 | 0% | 112 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.181 | 15% | 107 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.465 | 0% | 61 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.152 | 31% | 57 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.540 | 0% | 129 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.467 | 0% | 53 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 53 |

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

### Everything logged during the run: 0 errors, 0 warnings, 28 edit-mode artefacts

- `(edit mode) Error: Raid_sign_BOOKS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
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

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 79 |  |
| square: KentuckyMap.Build | yes | 6,882 |  |
| square: count | yes | 976 |  |
| square: pick spots | yes | 115 |  |
| square: pictures | yes | 21,797 |  |
| square: discard | yes | 259 |  |
| square: passes (warm rebuild) | yes | 4,693 |  |
| chunks: stress test | yes | 1,310 |  |
| chunks: Louisville 3x3 | yes | 1,696 |  |
| chunks: determinism | yes | 253 |  |
| perf: Lite against Full | yes | 1,190 |  |
| perf: determinism across build orders | yes | 413 |  |
| perf: what things cost | yes | 1,845 |  |
| perf: walking and driving through the state | yes | 162,465 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
