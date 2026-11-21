# CLONE-Z map harness: Louisville (build ky-local-f2)

**Exit 0**: ok. Full run, 187.5 s, started 2026-10-24 10:54:49.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Louisville plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 7,178 ms |
| the same square again, warm, pass by pass | 4,309 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 23.4, -25.0, facing -82° |
| exits | South 6th Street (north) at -52.3, 197.0; US 31W east at 197.0, 114.4; West Jefferson Street (east) at 197.0, -42.9; South 5th Street at 40.9, -197.0; West Jefferson Street at -197.0, 11.9; US 60 west at -197.0, 169.0 |
| overhead renderers (hidden in the iso view) | 80 |
| `KentuckyMap.LastStats` | Kentucky map Louisville (build ky-local-f2): 51 buildings, 56 beyond the edge, 495 street pieces, 631 containers, 403 lights, 325 trees, 6 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 51 with their middle inside the square (7 of them reach within 1 m of its edge or past it), 56 in the scenery ring; inside: commercial 37, house 8, canopy 2, office 2, warehouse 2 |
| map data: streets, lines, areas | 521 street pieces, 0 lines (), 98 land-use areas (park 38, parking 23, grass 22, water 7, plaza 6, commercial 2) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 25,496 | 13,591 | 13,591 | 13,182 | 6,334,387 | 3,705,990 | 138 | 29,712 | 404 (0) | 6,415 | 11,241 | 631 |
| RaidOverhead | 308 | 258 | 258 | 258 | 307,653 | 185,743 | 90 | 1,007 | 0 (0) | 0 | 258 | 0 |
| RaidScenery | 107 | 106 | 106 | 106 | 78,796 | 99,630 | 37 | 279 | 0 (0) | 0 | 63 | 0 |
| **all** | 25,911 | 13,955 | 13,955 | 13,546 | 6,720,836 | 3,991,363 | 214 | 30,998 | 404 (0) | 6,415 | 11,562 | 631 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 23,790 | 13,221 | 13,221 | 12,843 | 5,388,807 | 3,209,777 | 81 | 28,019 | 379 (0) | 4,788 | 11,046 | 515 |
| World/OutdoorGround | 140 | 140 | 140 | 140 | 153,227 | 64,500 | 17 | 605 | 0 (0) | 0 | 0 | 0 |
| World/OutdoorProps | 136 | 136 | 136 | 136 | 258,215 | 154,403 | 31 | 577 | 0 (0) | 0 | 136 | 0 |
| World/Building shells | 60 | 59 | 59 | 59 | 531,628 | 273,458 | 36 | 476 | 0 (0) | 0 | 59 | 0 |
| RaidOverhead/Building shells | 60 | 59 | 59 | 59 | 193,067 | 98,397 | 79 | 466 | 0 (0) | 0 | 59 | 0 |
| RaidOverhead/Roofs | 89 | 89 | 89 | 89 | 43,398 | 26,545 | 29 | 303 | 0 (0) | 0 | 89 | 0 |
| RaidOverhead/Interior KY# | 96 | 48 | 48 | 48 | 62,701 | 47,492 | 5 | 176 | 0 (0) | 0 | 48 | 0 |
| RaidScenery/SceneryGround | 41 | 41 | 41 | 41 | 8,916 | 2,992 | 10 | 126 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SceneryProps | 35 | 35 | 35 | 35 | 28,408 | 17,954 | 26 | 123 | 0 (0) | 0 | 35 | 0 |
| World/StreetLamp | 68 | 34 | 34 | 3 | 2,486 | 3,840 | 3 | 34 | 24 (0) | 34 | 0 | 0 |
| RaidOverhead/Overhead | 32 | 32 | 32 | 32 | 1,263 | 421 | 1 | 32 | 0 (0) | 0 | 32 | 0 |
| RaidOverhead/Wires | 30 | 30 | 30 | 30 | 7,224 | 12,888 | 1 | 30 | 0 (0) | 0 | 30 | 0 |
| RaidScenery/Wires | 28 | 28 | 28 | 28 | 3,930 | 7,092 | 1 | 28 | 0 (0) | 0 | 28 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 37,538 | 71,590 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 17,052 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 449 | 10% | 1 | 449 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 4 | 0% | 1 | 4 |
| water | 4 | 0% | 1 | 4 |
| fields | 0 | 0% | 1 | 0 |
| lots | 4 | 0% | 1 | 4 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 28 | 1% | 1 | 28 |
| road network | 16 | 0% | 1 | 16 |
| junctions | 5 | 0% | 1 | 5 |
| streets | 23 | 1% | 115 | 3 |
| sidewalks | 26 | 1% | 58 | 5 |
| berms and driveways | 13 | 0% | 55 | 2 |
| road markings | 1 | 0% | 14 | 0 |
| paths | 13 | 0% | 217 | 1 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 8 | 0% | 1 | 8 |
| buildings | 110 | 3% | 35 | 14 |
| interior plans | 190 | 4% | 49 | 10 |
| interior shell | 6 | 0% | 48 | 0 |
| interior walls | 136 | 3% | 48 | 25 |
| interior floors | 9 | 0% | 48 | 1 |
| interior doors | 378 | 9% | 136 | 22 |
| interior switches | 56 | 1% | 48 | 10 |
| interior rooms | 2,405 | 56% | 459 | 199 |
| interior wall meshes | 62 | 1% | 48 | 7 |
| interior finish | 133 | 3% | 48 | 11 |
| cutaway | 26 | 1% | 12 | 3 |
| utility lines | 10 | 0% | 1 | 10 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 14 | 0% | 1 | 14 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 3 | 0% | 1 | 3 |
| street furniture | 21 | 0% | 1 | 21 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 3 | 0 |
| park litter | 2 | 0% | 1 | 2 |
| wires | 7 | 0% | 1 | 7 |
| nature plan | 2 | 0% | 1 | 2 |
| driveway plan | 0 | 0% | 1 | 0 |
| mapped trees | 9 | 0% | 172 | 0 |
| tree rows | 0 | 0% | 1 | 0 |
| yard trees | 1 | 0% | 22 | 0 |
| grounds trees | 0 | 0% | 2 | 0 |
| street trees | 0 | 0% | 17 | 0 |
| fence rows | 1 | 0% | 164 | 0 |
| woods | 5 | 0% | 9336 | 0 |
| driveway cars | 0 | 0% | 1 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 4 | 0% | 44 | 1 |
| kerbside cars | 16 | 0% | 286 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 1 | 0% | 1 | 1 |
| wall drifts | 22 | 1% | 50 | 2 |
| fence drifts | 0 | 0% | 3 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 5 | 0% | 1 | 5 |
| (after the last pass) | 10 | 0% | 1 | 10 |
| (emit the overhead meshes) | 6 | 0% | 1 | 6 |
| (emit the outdoor meshes) | 54 | 1% | 1 | 54 |
| **total** | **4,309** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Louisville | 11712_9478 (183_148) | 0 + 0 | **59** | interior plans 6 | 1 | 211 | 49 | 128,478 | 68,562 | 0 | 164 | 6 | 80/6/19 | 0E 0W |
| rural-5km | 5 km from Louisville (bearing 280°) | 11673_9484 (182_148) | 0 + 0 | **203** | interior plans 69 | 1 | 221 | 54 | 275,118 | 146,463 | 3 | 242 | 9 | 4/9/6 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 0 + 0 | **49** | interior plans 6 | 1 | 211 | 49 | 128,478 | 68,562 | 0 | 164 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 353 + 21 | **110** | buildings 40 | 1 | 134 | 57 | 128,299 | 67,999 | 1 | 97 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 12 + 6 | **27** | signs 4 | 0 | 94 | 42 | 52,703 | 38,025 | 0 | 135 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.3 | 1 | 2.3 |
| ground | 0.9 | 1 | 0.9 |
| water | 0.8 | 1 | 0.8 |
| fields | 0.1 | 1 | 0.1 |
| lots | 0.9 | 1 | 0.9 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 1.1 | 1 | 1.1 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.4 | 1 | 1.4 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 4.7 | 3 | 2.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.8 | 3 | 4.2 |
| interior plans | 19.0 | 6 | 6.3 |
| cutaway | 3.0 | 2 | 2.2 |
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
| mapped trees | 0.3 | 1 | 0.3 |
| yard trees | 0.7 | 1 | 0.7 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 1.6 | 1 | 1.6 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.5 | 1 | 1.5 |
| objects | 2.8 | 1 | 2.8 |
| meshes | 1.4 | 1 | 1.4 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.9 | 1 | 1.9 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 0.4 | 1 | 0.4 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 0.7 | 1 | 0.7 |
| sidewalks | 0.2 | 1 | 0.2 |
| berms and driveways | 9.9 | 4 | 5.6 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 31.8 | 9 | 7.8 |
| interior plans | 81.9 | 6 | 69.4 |
| cutaway | 41.1 | 2 | 39.2 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.1 | 1 | 0.1 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.8 | 1 | 0.8 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.7 | 1 | 0.7 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 1.3 | 1 | 1.3 |
| grounds trees | 0.8 | 1 | 0.8 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 4 | 0.2 |
| driveway cars | 0.6 | 1 | 0.6 |
| buses and cruisers | 5.8 | 1 | 5.8 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 18.5 | 3 | 15.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.6 | 1 | 0.6 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 2.0 | 2 | 2.0 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 0.7 | 1 | 0.7 |
| water | 0.7 | 1 | 0.7 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.1 | 1 | 0.1 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 4.4 | 3 | 2.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.3 | 3 | 3.8 |
| interior plans | 18.1 | 6 | 6.3 |
| cutaway | 2.8 | 2 | 2.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.9 | 1 | 0.9 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.3 | 1 | 0.3 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.3 | 1 | 0.3 |
| kerbside cars | 1.4 | 1 | 1.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 2.1 | 2 | 2.0 |
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.5 | 1 | 0.5 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 5.8 | 2 | 5.4 |
| sidewalks | 2.9 | 2 | 2.3 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 52.5 | 5 | 40.2 |
| interior plans | 20.7 | 5 | 8.2 |
| cutaway | 3.2 | 2 | 2.3 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 3.4 | 1 | 3.4 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 1.0 | 1 | 1.0 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 1.7 | 1 | 1.7 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 1.3 | 1 | 1.3 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.2 | 1 | 0.2 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.7 | 4 | 0.6 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 1.4 | 1 | 1.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.2 | 2 | 2.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.3 | 1 | 0.3 |
| points | 2.4 | 1 | 2.4 |
| colliders | 1.2 | 1 | 1.2 |
| objects | 1.2 | 1 | 1.2 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.5 | 1 | 0.5 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 1.1 | 1 | 1.1 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 2.7 | 1 | 2.7 |
| road network | 0.4 | 1 | 0.4 |
| junctions | 1.2 | 1 | 1.2 |
| streets | 3.0 | 2 | 2.8 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.2 | 1 | 0.2 |
| utility lines | 1.2 | 1 | 1.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 3.5 | 1 | 3.5 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.8 | 1 | 0.8 |
| nature plan | 1.2 | 1 | 1.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.1 | 1 | 0.1 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.7 | 1 | 0.7 |
| woods | 4.5 | 4 | 1.6 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 1.1 | 1 | 1.1 |
| kerbside cars | 0.3 | 1 | 0.3 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
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
| 183_148 | yes | 157 | 14 |
| 183_147 | yes | 433 | 30 |
| 182_147 | yes | 468 | 55 |
| 182_148 | yes | 128 | 11 |
| 196_145 | yes | 353 | 21 |
| 210_134 | yes | 12 | 6 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 473 ms (slowest 81 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **57** | data 7 | 0 | 211 | 49 | 128,478 | 68,562 | 0 | 164 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **55** | interior plans 8 | 1 | 196 | 75 | 146,585 | 80,632 | 0 | 202 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 0 + 0 | **50** | interior plans 8 | 1 | 247 | 57 | 111,047 | 62,255 | 2 | 203 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **41** | interior plans 3 | 0 | 149 | 58 | 114,581 | 60,646 | 0 | 119 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **36** | interior plans 4 | 0 | 186 | 60 | 93,398 | 53,570 | 0 | 137 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **39** | interior plans 9 | 1 | 149 | 53 | 102,289 | 59,858 | 0 | 119 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **58** | interior plans 9 | 2 | 201 | 53 | 122,365 | 65,728 | 1 | 186 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **57** | objects 9 | 1 | 158 | 53 | 93,255 | 55,675 | 3 | 94 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **81** | interior plans 12 | 2 | 159 | 41 | 183,286 | 100,869 | 0 | 154 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,658 | 499 | 499 | 496 | 1,095,284 | 607,795 | 128 | 2,164 | 6 (0) | 1,378 | 340 | 82 |

### Determinism

Chunk 11712_9478 (start-centre) built twice: **match**: identical, in the same order (128,478 vertices, 68,562 triangles, 211 objects, 49 meshes; hash 5d612c1d6017d62e vs 5d612c1d6017d62e; 178 colliders, lights and loot containers, hash 63215237939e7a04 vs 63215237939e7a04). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 59 | 163 / 118 / 1.4 | 0 / 0.0 | 48 / 125,885 / 3.1 | 3.8 | 53.9 | 36 / 3.2 |
| rural-5km | Full | 203 | 241 / 156 / 2.0 | 0 / 0.0 | 52 / 272,408 / 40.3 | 41.4 | 159.6 | 36 / 0.3 |
| louisville-downtown | Full | 49 | 163 / 118 / 2.1 | 0 / 0.0 | 48 / 125,885 / 2.4 | 3.4 | 43.2 | 36 / 0.3 |
| lexington-downtown | Full | 110 | 95 / 56 / 1.2 | 0 / 0.0 | 53 / 125,570 / 2.7 | 3.9 | 105.4 | 39 / 1.2 |
| hazard | Full | 27 | 134 / 24 / 0.6 | 0 / 0.0 | 37 / 49,325 / 0.3 | 0.7 | 26.1 | 29 / 0.5 |
| louisville 0,0 | Full | 57 | 163 / 118 / 1.4 | 0 / 0.0 | 48 / 125,885 / 2.5 | 3.4 | 51.9 | 36 / 0.3 |
| louisville 1,0 | Full | 55 | 201 / 107 / 1.5 | 0 / 0.0 | 73 / 143,916 / 3.2 | 5.0 | 48.7 | 46 / 0.3 |
| louisville -1,0 | Full | 50 | 200 / 143 / 1.9 | 0 / 0.0 | 50 / 108,095 / 1.5 | 2.4 | 45.3 | 39 / 0.3 |
| louisville 0,1 | Full | 41 | 118 / 71 / 1.1 | 0 / 0.0 | 54 / 112,107 / 2.2 | 3.9 | 35.9 | 36 / 0.3 |
| louisville 0,-1 | Full | 36 | 136 / 81 / 1.1 | 0 / 0.0 | 55 / 90,121 / 1.2 | 2.2 | 32.9 | 37 / 0.3 |
| louisville 1,1 | Full | 39 | 118 / 72 / 0.9 | 0 / 0.0 | 50 / 99,218 / 2.0 | 2.8 | 35.3 | 35 / 0.2 |
| louisville -1,1 | Full | 58 | 185 / 102 / 2.4 | 0 / 0.0 | 50 / 119,233 / 3.4 | 5.1 | 50.0 | 37 / 0.6 |
| louisville 1,-1 | Full | 57 | 90 / 57 / 1.0 | 0 / 0.0 | 45 / 89,001 / 1.4 | 2.8 | 53.4 | 34 / 0.4 |
| louisville -1,-1 | Full | 81 | 153 / 109 / 2.2 | 0 / 0.0 | 39 / 180,656 / 5.5 | 6.7 | 71.7 | 30 / 0.6 |

The Louisville block's NavMesh: 330 sources collected in 4 ms; baked at once (as one 384 m square) in 214 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 59 | 10 | 17% | interior plans 7.9 | buildings 2.7 | 128,478 / 9,020 | 211 / 33 | 49 / 28 | 164 / 1 | 0 / 0 |
| rural-5km | 116 | 7 | 6% | interior plans 35.0 | buildings 2.1 | 288,743 / 11,921 | 223 / 32 | 53 / 27 | 282 / 1 | 3 / 0 |
| louisville-downtown | 55 | 9 | 17% | interior plans 7.2 | buildings 2.5 | 128,478 / 9,020 | 211 / 33 | 49 / 28 | 164 / 1 | 0 / 0 |
| lexington-downtown | 48 | 7 | 16% | streets 5.7 | snow tone 1.6 | 120,585 / 6,003 | 145 / 33 | 62 / 28 | 97 / 1 | 3 / 0 |
| hazard | 22 | 5 | 22% | streets 3.0 | snow tone 1.3 | 66,793 / 6,054 | 94 / 32 | 44 / 27 | 159 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 82.72 | 41.36 | make 70.02 + switch on 12.70 ms; 1602 objects; destroy 8.29 ms; 3,000 rays 4.08 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 19.97 | 9.99 | make 14.83 + switch on 5.14 ms; 10 objects; destroy 1.66 ms; 3,000 rays 2.27 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 15.10 | 7.55 | make 4.19 + switch on 10.91 ms; 2 objects; destroy 0.14 ms; 3,000 rays 2.71 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.97 | 1.49 | make 2.94 + switch on 0.04 ms (+ 11.40 ms baking on the worker); 2 objects; destroy 0.07 ms; 3,000 rays 2.06 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 9.52 | 238.04 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 10.36 | 259.02 | objects 0.96, allocate 0.02, pack 9.22, apply 0.16 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.66 | 41.55 | objects 1.46, allocate 0.02, pack 10.00 (worker), apply 0.18 ms |
| primitives | Box, turned, flat colour | 20,000 | 22.95 | 1.15 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 130.97 | 6.55 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 49.49 | 2.48 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 94.90 | 4.75 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 85.06 | 4.25 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 28.63 | 1.43 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 8.40 | 0.42 | 80,000 vertices |
| primitives | Tri | 20,000 | 5.98 | 0.30 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 31.85 | 6.37 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 31.66 | 6.33 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 49.73 | 16.58 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 37.36 | 12.45 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 31.68 | 15.84 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 23.61 | 23.61 |  |
| objects | BoxCollider added to one object | 2,000 | 63.27 | 31.64 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 49.26 | 246.31 |  |
| objects | RoomLight.Spawn | 100 | 17.20 | 171.99 |  |
| objects | switching all of the above on | 1 | 49.38 | 49375.20 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 18.45 | 18446.30 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 59 | 0.93 | 15.71 | worst 0.17 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 9.04 | 904.32 |  |
| regions | MapBuild.LoadRegion 182_147 (today) | 1 | 536.68 | 536683.00 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 580.04 | 580044.00 | 0 MB allocated; 19 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 37.29 | 37287.60 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 1,292 ms, 1,703,218 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (41) | 0.40 | 0.05 | 2.19 | 8.70 | 26.53 | 7 / 5 / 2 / 0 | buildings 15.4 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 1,057 / 1,767,623 | 4 | 273 / 833 (pool 33) |
| drive-east | 2400 (470) | 0.80 | 0.05 | 4.42 | 8.09 | 62.02 | 68 / 26 / 6 / 1 | interior plans 62.0 | 43 (27 / 16) | 3 | 19 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 2,080 / 3,479,278 | 4 | 278 / 910 (pool 35) |
| teleport-louisville | 720 (22) | 0.21 | 0.05 | 0.08 | 4.12 | 42.69 | 4 / 1 / 1 / 1 | buildings 2.9 | 9 (0 / 9) | 0 | 30 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 1,055 / 1,776,577 | 4 | 277 / 840 (pool 35) |
| drive-louisville | 1800 (140) | 0.32 | 0.05 | 2.73 | 4.98 | 9.28 | 18 / 4 / 0 / 0 | buildings 9.1 | 15 (9 / 6) | 0 | 6 | 0.0 | 0.0 | 0 / 0 | 34 (9 Lite) / 1,570 / 2,686,283 | 4 | 277 / 887 (pool 35) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: buildings (1x, worst 15.4 ms), snow tone (2x, worst 13.8 ms), junctions (1x, worst 4.1 ms), objects (1x, worst 3.9 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: interior plans (12x, worst 62.0 ms), colliders (2x, worst 15.9 ms), wall drifts (1x, worst 13.9 ms), berms and driveways (1x, worst 12.2 ms), buildings (5x, worst 9.8 ms), road network (1x, worst 7.3 ms)
- **teleport-louisville**: set down in downtown Louisville (0 km away), standing
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: buildings (1x, worst 9.1 ms), interior plans (3x, worst 9.0 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 160 edit-mode artefacts

- `(edit mode) Error: Raid_sign_HYATT REGENCY LOUISVIL1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HYATT REGENCY LOUISVIL1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_KAUFMAN-STRAUSS BUILDI050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_KAUFMAN-STRAUSS BUILDI050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_LANDMARK BUILDING050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_LANDMARK BUILDING050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FIFTH THIRD BANK07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FIFTH THIRD BANK07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_LOUISVILLE MARRIOTT DO1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_LOUISVILLE MARRIOTT DO1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_THE OLD SPAGHETTI FACT050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_THE OLD SPAGHETTI FACT050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BISTRO 30107100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BISTRO 30107100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OMNI LOUISVILLE HOTEL07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OMNI LOUISVILLE HOTEL07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.555 | 0% | 1,927 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 344 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Jefferson Street, at the game's starting zoom | 0.208 | 0% | 146 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Jefferson Street, at the game's starting zoom | 0.066 | 0% | 143 |
| `iso_downtown_day.png` | Downtown Louisville: the middle of the square, on West Jefferson Street | 0.286 | 0% | 68 |
| `iso_downtown_night.png` | Downtown Louisville: the middle of the square, on West Jefferson Street | 0.071 | 0% | 75 |
| `iso_residential_day.png` | The densest residential street: South 6th Street, 1 homes within 30 m | 0.318 | 0% | 60 |
| `iso_residential_night.png` | The densest residential street: South 6th Street, 1 homes within 30 m | 0.075 | 0% | 71 |
| `iso_intersection_day.png` | The busiest intersection: West Market Street / South 5th Street (4 ways in) | 0.233 | 0% | 65 |
| `iso_intersection_night.png` | The busiest intersection: West Market Street / South 5th Street (4 ways in) | 0.073 | 0% | 67 |
| `iso_green_day.png` | Green: park 'Jefferson Square', 2,926 m² inside the square | 0.581 | 0% | 64 |
| `iso_green_night.png` | Green: park 'Jefferson Square', 2,926 m² inside the square | 0.078 | 0% | 82 |
| `iso_parking_day.png` | Parking: a parking, 4,553 m² inside the square | 0.367 | 0% | 62 |
| `iso_parking_night.png` | Parking: a parking, 4,553 m² inside the square | 0.073 | 0% | 78 |
| `iso_edge_day.png` | The square's edge: where West Market Street leaves it (east), the playable map giving way to the scenery ring | 0.217 | 0% | 72 |
| `iso_edge_night.png` | The square's edge: where West Market Street leaves it (east), the playable map giving way to the scenery ring | 0.067 | 0% | 61 |
| `iso_waterway_day.png` | Water: a water, 7 m² inside the square | 0.505 | 0% | 57 |
| `iso_waterway_night.png` | Water: a water, 7 m² inside the square | 0.079 | 0% | 74 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, commercial 'Hall of Justice' (6,244 m² footprint) | 0.160 | 0% | 87 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, commercial 'Hall of Justice' (6,244 m² footprint) | 0.062 | 1% | 72 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Jefferson Street (downtown), looking along the street | 0.321 | 0% | 93 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Jefferson Street (downtown), looking along the street | 0.157 | 25% | 94 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of South 6th Street (the densest residential street), looking along it | 0.303 | 0% | 137 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of South 6th Street (the densest residential street), looking along it | 0.139 | 21% | 155 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.541 | 0% | 183 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.467 | 0% | 65 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 76 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 23.4, -25.0 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -2.2, -15.2 | the place's own point in places.json (its OSM place node, which the square is built around), moved 15.4 m onto the nearest street |
| residential | -58.7, 151.4 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | 84.3, 130.1 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | -60.9, -53.4 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | 90.4, -156.8 | the biggest parking area inside the square |
| edge | 200.0, 113.9 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | -59.0, 45.1 | no railway or river in reach: the biggest water area inside the square |
| landmark | -157.9, -36.5 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

### Everything logged during the run: 0 errors, 0 warnings, 160 edit-mode artefacts

- `(edit mode) Error: Raid_sign_HYATT REGENCY LOUISVIL1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HYATT REGENCY LOUISVIL1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_KAUFMAN-STRAUSS BUILDI050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_KAUFMAN-STRAUSS BUILDI050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_LANDMARK BUILDING050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_LANDMARK BUILDING050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FIFTH THIRD BANK07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_FIFTH THIRD BANK07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_LOUISVILLE MARRIOTT DO1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_LOUISVILLE MARRIOTT DO1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_THE OLD SPAGHETTI FACT050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_THE OLD SPAGHETTI FACT050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BISTRO 30107100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BISTRO 30107100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OMNI LOUISVILLE HOTEL07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_OMNI LOUISVILLE HOTEL07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HARDWARE37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 68 |  |
| square: KentuckyMap.Build | yes | 7,180 |  |
| square: count | yes | 771 |  |
| square: pick spots | yes | 355 |  |
| square: pictures | yes | 6,882 |  |
| square: discard | yes | 281 |  |
| square: passes (warm rebuild) | yes | 4,547 |  |
| chunks: stress test | yes | 2,234 |  |
| chunks: Louisville 3x3 | yes | 1,656 |  |
| chunks: determinism | yes | 187 |  |
| perf: Lite against Full | yes | 1,748 |  |
| perf: determinism across build orders | yes | 373 |  |
| perf: what things cost | yes | 2,375 |  |
| perf: walking and driving through the state | yes | 158,860 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
