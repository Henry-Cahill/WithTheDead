# CLONE-Z map harness: Falmouth (build ky-local-f2)

**Exit 0**: ok. Full run, 208.8 s, started 2026-10-24 10:21:01.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Falmouth plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,772 ms |
| the same square again, warm, pass by pass | 5,399 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 42.7, -4.8, facing -67° |
| exits | Chapel Street (north) at -2.5, 197.0; KY 22 east at 197.0, 173.2; KY 22 south at -60.4, -197.0; West Shelby Street at -197.0, 107.3 |
| overhead renderers (hidden in the iso view) | 215 |
| `KentuckyMap.LastStats` | Kentucky map Falmouth (build ky-local-f2): 150 buildings, 162 beyond the edge, 458 street pieces, 851 containers, 1128 lights, 510 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 150 with their middle inside the square (11 of them reach within 1 m of its edge or past it), 162 in the scenery ring; inside: house 69, garage 27, commercial 19, shed 9, apartments 8, retail 6, office 5, civic 4, church 2, canopy 1 |
| map data: streets, lines, areas | 458 street pieces, 7 lines (stream 5, rail 1, river 1), 91 land-use areas (residential 27, parking 15, commercial 8, grass 8, churchyard 7, forest 7, industrial 6, pitch 3, retail 3, school 2, water 2, meadow 1, park 1, plaza 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 32,164 | 18,066 | 18,066 | 16,941 | 8,543,294 | 5,241,360 | 148 | 41,202 | 1,159 (0) | 9,590 | 12,644 | 851 |
| RaidOverhead | 695 | 546 | 546 | 546 | 434,048 | 259,619 | 118 | 2,095 | 0 (0) | 0 | 546 | 0 |
| RaidScenery | 112 | 111 | 111 | 111 | 104,769 | 111,390 | 67 | 487 | 0 (0) | 0 | 62 | 0 |
| **all** | 32,971 | 18,723 | 18,723 | 17,598 | 9,082,111 | 5,612,369 | 232 | 43,784 | 1,159 (0) | 9,590 | 13,252 | 851 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 30,041 | 17,606 | 17,606 | 16,497 | 7,042,917 | 4,479,563 | 80 | 38,293 | 1,110 (0) | 6,451 | 12,342 | 782 |
| World/Building shells | 164 | 163 | 163 | 163 | 1,025,136 | 515,538 | 59 | 1,483 | 0 (0) | 0 | 163 | 0 |
| RaidOverhead/Building shells | 164 | 163 | 163 | 163 | 286,532 | 138,475 | 97 | 1,036 | 0 (0) | 0 | 163 | 0 |
| World/OutdoorGround | 139 | 139 | 139 | 139 | 159,676 | 65,414 | 20 | 754 | 0 (0) | 0 | 0 | 0 |
| World/OutdoorProps | 138 | 138 | 138 | 138 | 314,175 | 178,741 | 17 | 652 | 0 (0) | 0 | 138 | 0 |
| RaidOverhead/Roofs | 132 | 132 | 132 | 132 | 46,314 | 26,707 | 64 | 486 | 0 (0) | 0 | 132 | 0 |
| RaidOverhead/Interior KY# | 294 | 147 | 147 | 147 | 93,570 | 84,604 | 5 | 469 | 0 (0) | 0 | 147 | 0 |
| RaidScenery/SceneryProps | 40 | 40 | 40 | 40 | 52,456 | 31,294 | 53 | 291 | 0 (0) | 0 | 40 | 0 |
| RaidScenery/SceneryGround | 47 | 47 | 47 | 47 | 13,393 | 5,598 | 13 | 172 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 68 | 68 | 68 | 68 | 2,481 | 827 | 1 | 68 | 0 (0) | 0 | 68 | 0 |
| RaidOverhead/Wires | 36 | 36 | 36 | 36 | 5,151 | 9,006 | 1 | 36 | 0 (0) | 0 | 36 | 0 |
| RaidScenery/Wires | 22 | 22 | 22 | 22 | 1,755 | 3,102 | 1 | 22 | 0 (0) | 0 | 22 | 0 |
| World/StreetLamp | 36 | 18 | 18 | 2 | 1,342 | 2,080 | 2 | 18 | 18 (0) | 18 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 37,161 | 71,394 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 22,301 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 210 | 4% | 1 | 210 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 4 | 0% | 1 | 4 |
| water | 11 | 0% | 1 | 11 |
| fields | 0 | 0% | 1 | 0 |
| lots | 2 | 0% | 1 | 2 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 109 | 2% | 1 | 109 |
| road network | 15 | 0% | 1 | 15 |
| junctions | 21 | 0% | 1 | 21 |
| streets | 73 | 1% | 113 | 21 |
| sidewalks | 40 | 1% | 113 | 8 |
| berms and driveways | 24 | 0% | 106 | 3 |
| road markings | 2 | 0% | 6 | 1 |
| paths | 12 | 0% | 1 | 12 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 5 | 0% | 1 | 5 |
| buildings | 249 | 5% | 76 | 19 |
| interior plans | 532 | 10% | 150 | 36 |
| interior shell | 16 | 0% | 147 | 0 |
| interior walls | 166 | 3% | 147 | 20 |
| interior floors | 18 | 0% | 147 | 1 |
| interior doors | 547 | 10% | 275 | 14 |
| interior switches | 91 | 2% | 147 | 9 |
| interior rooms | 2,572 | 48% | 795 | 29 |
| interior wall meshes | 104 | 2% | 147 | 4 |
| interior finish | 237 | 4% | 147 | 10 |
| cutaway | 49 | 1% | 23 | 2 |
| utility lines | 7 | 0% | 1 | 7 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 5 | 0% | 1 | 5 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 7 | 0% | 1 | 7 |
| street furniture | 46 | 1% | 1 | 46 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 3 | 0% | 53 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 5 | 0% | 1 | 5 |
| nature plan | 0 | 0% | 1 | 0 |
| driveway plan | 2 | 0% | 1 | 2 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 0% | 173 | 0 |
| grounds trees | 1 | 0% | 7 | 0 |
| street trees | 2 | 0% | 159 | 0 |
| creek trees | 0 | 0% | 4 | 0 |
| fence rows | 1 | 0% | 33 | 0 |
| woods | 6 | 0% | 9347 | 0 |
| driveway cars | 2 | 0% | 24 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 5 | 0% | 45 | 2 |
| kerbside cars | 6 | 0% | 103 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 1 | 0% | 1 | 1 |
| wall drifts | 37 | 1% | 147 | 1 |
| fence drifts | 7 | 0% | 51 | 4 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 2 | 0% | 1 | 2 |
| points | 1 | 0% | 1 | 1 |
| (after the last pass) | 16 | 0% | 1 | 16 |
| (emit the overhead meshes) | 24 | 0% | 1 | 24 |
| (emit the outdoor meshes) | 86 | 2% | 1 | 86 |
| **total** | **5,399** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Falmouth | 12683_9851 (198_153) | 0 + 0 | **156** | data 40 | 2 | 333 | 113 | 240,463 | 123,174 | 4 | 372 | 25 | 34/25/9 | 0E 0W |
| rural-5km | 5 km from Falmouth (bearing 50°) | 12713_9876 (198_154) | 0 + 0 | **20** | buildings 8 | 0 | 68 | 38 | 34,696 | 25,287 | 0 | 93 | 2 | 2/2/6 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 314 + 10 | **82** | paths 10 | 1 | 203 | 50 | 137,487 | 72,622 | 0 | 161 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 439 + 60 | **50** | streets 7 | 0 | 131 | 57 | 126,569 | 66,273 | 1 | 92 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **60** | wires 40 | 1 | 69 | 44 | 59,526 | 43,840 | 0 | 147 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 40.4 | 1 | 40.4 |
| ground | 0.5 | 1 | 0.5 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 1.4 | 1 | 1.4 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 1.3 | 1 | 1.3 |
| junctions | 0.7 | 1 | 0.7 |
| streets | 3.3 | 1 | 3.3 |
| sidewalks | 2.6 | 2 | 2.5 |
| berms and driveways | 2.0 | 2 | 2.0 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.6 | 1 | 0.6 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 15.1 | 7 | 3.9 |
| interior plans | 41.8 | 13 | 9.9 |
| cutaway | 10.2 | 5 | 2.3 |
| utility lines | 0.6 | 1 | 0.6 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.3 | 1 | 1.3 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.5 | 1 | 0.5 |
| street furniture | 1.0 | 1 | 1.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.1 | 1 | 0.1 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.2 | 1 | 0.2 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.5 | 1 | 0.5 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 4 | 0.0 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 0.9 | 1 | 0.9 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.1 | 1 | 0.1 |
| wall drifts | 4.8 | 3 | 2.2 |
| fence drifts | 0.1 | 1 | 0.1 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 3.7 | 2 | 2.2 |
| objects | 16.0 | 1 | 16.0 |
| meshes | 1.7 | 1 | 1.7 |

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
| snow tone | 1.5 | 1 | 1.5 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 0.2 | 1 | 0.2 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 0.6 | 1 | 0.6 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 8.6 | 2 | 8.5 |
| interior plans | 1.0 | 1 | 1.0 |
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
| nature plan | 0.0 | 1 | 0.0 |
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
| colliders | 0.5 | 1 | 0.5 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 1.1 | 1 | 1.1 |
| water | 1.9 | 1 | 1.9 |
| fields | 0.1 | 1 | 0.1 |
| lots | 0.2 | 1 | 0.2 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.9 | 1 | 1.9 |
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.7 | 1 | 0.7 |
| paths | 10.4 | 1 | 10.4 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 21.9 | 6 | 7.6 |
| interior plans | 20.6 | 6 | 6.0 |
| cutaway | 6.4 | 3 | 4.2 |
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
| nature plan | 0.0 | 1 | 0.0 |
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
| kerbside cars | 1.1 | 1 | 1.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 3.4 | 2 | 2.7 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.9 | 1 | 1.9 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.5 | 1 | 0.5 |
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.8 | 1 | 0.8 |
| junctions | 0.6 | 1 | 0.6 |
| streets | 6.8 | 1 | 6.8 |
| sidewalks | 2.8 | 2 | 2.3 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.9 | 1 | 0.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 7.9 | 4 | 2.4 |
| interior plans | 11.5 | 4 | 5.2 |
| cutaway | 3.0 | 2 | 2.2 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.8 | 1 | 0.8 |
| street furniture | 0.8 | 1 | 0.8 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.4 | 4 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.8 | 1 | 0.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.2 | 2 | 2.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.4 | 1 | 1.4 |
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
| snow tone | 1.9 | 1 | 1.9 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 3.3 | 1 | 3.3 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.2 | 1 | 1.2 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 39.5 | 1 | 39.5 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 2.3 | 2 | 2.1 |
| woods | 4.0 | 4 | 1.4 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.1 | 1 | 0.1 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.8 | 1 | 0.8 |
| objects | 1.3 | 1 | 1.3 |
| meshes | 0.4 | 1 | 0.4 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 198_154 | yes | 33 | 10 |
| 198_153 | yes | 74 | 7 |
| 197_153 | yes | 13 | 3 |
| 197_154 | yes | 36 | 4 |
| 183_148 | yes | 314 | 10 |
| 196_145 | yes | 439 | 60 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 132 | 20 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 493 ms (slowest 89 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **50** | interior plans 6 | 1 | 203 | 50 | 137,487 | 72,622 | 0 | 161 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **63** | interior plans 8 | 1 | 200 | 75 | 148,896 | 80,778 | 0 | 200 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 132 + 20 | **59** | interior plans 9 | 1 | 202 | 55 | 101,449 | 55,857 | 1 | 172 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **51** | interior plans 4 | 1 | 159 | 59 | 121,079 | 62,845 | 1 | 131 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **42** | streets 7 | 0 | 175 | 60 | 90,627 | 51,976 | 1 | 132 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **43** | interior plans 9 | 1 | 160 | 53 | 113,267 | 64,992 | 0 | 120 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **89** | buildings 49 | 1 | 186 | 53 | 115,540 | 62,122 | 0 | 192 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **38** | interior plans 5 | 0 | 163 | 54 | 105,990 | 62,171 | 3 | 100 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **57** | interior plans 8 | 1 | 174 | 41 | 188,492 | 103,086 | 0 | 149 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,624 | 500 | 500 | 497 | 1,122,827 | 616,449 | 132 | 2,141 | 6 (0) | 1,357 | 341 | 82 |

### Determinism

Chunk 12683_9851 (start-centre) built twice: **match**: identical, in the same order (240,463 vertices, 123,174 triangles, 333 objects, 113 meshes; hash 8723735b833c85eb vs 8723735b833c85eb; 384 colliders, lights and loot containers, hash 192c2612ed8d842d vs 192c2612ed8d842d). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 156 | 370 / 185 / 3.5 | 0 / 0.0 | 105 / 237,386 / 7.4 | 23.2 | 129.7 | 59 / 5.6 |
| rural-5km | Full | 20 | 92 / 23 / 0.5 | 0 / 0.0 | 37 / 32,095 / 0.3 | 0.9 | 18.4 | 34 / 0.2 |
| louisville-downtown | Full | 82 | 160 / 114 / 1.9 | 0 / 0.0 | 49 / 134,889 / 5.8 | 6.8 | 73.8 | 37 / 0.5 |
| lexington-downtown | Full | 50 | 90 / 53 / 0.7 | 0 / 0.0 | 54 / 123,902 / 2.4 | 3.4 | 45.6 | 39 / 0.3 |
| hazard | Full | 60 | 146 / 18 / 0.8 | 0 / 0.0 | 39 / 56,030 / 0.2 | 1.2 | 57.9 | 31 / 0.3 |
| louisville 0,0 | Full | 50 | 160 / 114 / 1.3 | 0 / 0.0 | 49 / 134,889 / 2.5 | 3.5 | 45.6 | 37 / 0.3 |
| louisville 1,0 | Full | 63 | 199 / 106 / 1.5 | 0 / 0.0 | 73 / 146,224 / 2.6 | 4.1 | 57.2 | 46 / 0.3 |
| louisville -1,0 | Full | 59 | 170 / 111 / 1.6 | 0 / 0.0 | 49 / 98,818 / 1.5 | 2.3 | 55.4 | 38 / 0.3 |
| louisville 0,1 | Full | 51 | 129 / 74 / 0.9 | 0 / 0.0 | 54 / 118,508 / 2.0 | 3.1 | 47.3 | 37 / 0.3 |
| louisville 0,-1 | Full | 42 | 131 / 79 / 1.0 | 0 / 0.0 | 55 / 87,268 / 1.0 | 1.9 | 38.7 | 37 / 0.3 |
| louisville 1,1 | Full | 43 | 119 / 73 / 0.9 | 0 / 0.0 | 50 / 110,208 / 2.0 | 3.0 | 39.4 | 35 / 0.3 |
| louisville -1,1 | Full | 89 | 191 / 102 / 1.6 | 0 / 0.0 | 50 / 112,480 / 1.8 | 2.8 | 85.1 | 37 / 0.3 |
| louisville 1,-1 | Full | 38 | 96 / 61 / 0.8 | 0 / 0.0 | 46 / 101,579 / 1.1 | 2.2 | 35.0 | 35 / 0.5 |
| louisville -1,-1 | Full | 57 | 148 / 103 / 1.4 | 0 / 0.0 | 39 / 185,856 / 3.5 | 4.2 | 51.5 | 30 / 0.3 |

The Louisville block's NavMesh: 332 sources collected in 11 ms; baked at once (as one 384 m square) in 317 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 69 | 12 | 17% | streets 3.6 | streets 2.2 | 240,463 / 14,628 | 333 / 45 | 113 / 40 | 372 / 1 | 4 / 0 |
| rural-5km | 11 | 5 | 44% | snow tone 1.4 | snow tone 1.5 | 30,145 / 5,149 | 66 / 37 | 37 / 32 | 89 / 1 | 0 / 0 |
| louisville-downtown | 51 | 11 | 21% | interior plans 6.2 | paths 2.4 | 129,881 / 10,497 | 203 / 36 | 50 / 31 | 160 / 1 | 0 / 0 |
| lexington-downtown | 45 | 12 | 27% | streets 6.8 | streets 5.4 | 120,915 / 7,969 | 129 / 32 | 57 / 27 | 95 / 1 | 0 / 0 |
| hazard | 19 | 7 | 36% | streets 3.3 | streets 2.0 | 64,430 / 6,261 | 88 / 32 | 44 / 27 | 152 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 56.77 | 28.38 | make 49.39 + switch on 7.38 ms; 1602 objects; destroy 3.67 ms; 3,000 rays 2.77 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 15.05 | 7.53 | make 10.17 + switch on 4.88 ms; 10 objects; destroy 1.76 ms; 3,000 rays 2.24 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.95 | 5.98 | make 3.91 + switch on 8.05 ms; 2 objects; destroy 0.13 ms; 3,000 rays 2.27 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.17 | 1.08 | make 2.13 + switch on 0.04 ms (+ 9.45 ms baking on the worker); 2 objects; destroy 0.11 ms; 3,000 rays 2.37 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.60 | 215.05 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.64 | 215.97 | objects 1.22, allocate 0.02, pack 7.25, apply 0.15 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.09 | 27.33 | objects 0.95, allocate 0.02, pack 7.62 (worker), apply 0.12 ms |
| primitives | Box, turned, flat colour | 20,000 | 26.99 | 1.35 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 87.21 | 4.36 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 49.10 | 2.46 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 107.33 | 5.37 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 27.62 | 1.38 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 23.92 | 1.20 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.65 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.76 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 48.42 | 9.68 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 24.29 | 4.86 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 30.40 | 10.13 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 41.04 | 13.68 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 14.69 | 7.35 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 12.04 | 12.04 |  |
| objects | BoxCollider added to one object | 2,000 | 39.70 | 19.85 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 23.58 | 117.90 |  |
| objects | RoomLight.Spawn | 100 | 10.48 | 104.77 |  |
| objects | switching all of the above on | 1 | 27.41 | 27412.40 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 10.41 | 10409.80 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 78 | 0.64 | 8.17 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.16 | 216.00 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 386.53 | 386531.00 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 373.05 | 373049.30 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 25.27 | 25274.20 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (16 pieces in 1,046 ms, 2,611,537 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (27) | 0.26 | 0.05 | 1.40 | 4.74 | 11.17 | 4 / 1 / 0 / 0 | junctions 7.0 | 9 (0 / 9) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 25 (9 Lite) / 1,537 / 2,694,768 | 2 | 189 / 888 (pool 40) |
| drive-east | 2400 (192) | 0.38 | 0.04 | 3.04 | 5.14 | 103.10 | 31 / 5 / 2 / 1 | woods 103.1 | 40 (24 / 16) | 0 | 19 | 0.0 | 0.0 | 0 / 0 | 46 (12 Lite) / 2,151 / 3,621,300 | 2 | 191 / 928 (pool 40) |
| teleport-louisville | 720 (186) | 1.12 | 0.04 | 4.79 | 9.32 | 40.46 | 31 / 10 / 4 / 2 | interior plans 14.2 | 25 (9 / 16) | 0 | 30 | 0.2 | 2.4 | 12 / 143 | 41 (16 Lite) / 2,131 / 3,864,255 | 4 | 208 / 935 (pool 40) |
| drive-louisville | 1800 (213) | 0.49 | 0.05 | 3.72 | 5.71 | 20.56 | 28 / 10 / 2 / 0 | interior plans 20.5 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 44 | 46 (12 Lite) / 2,523 / 4,675,230 | 6 | 254 / 973 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: snow tone (1x, worst 1.6 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: woods (2x, worst 103.1 ms), snow tone (1x, worst 16.7 ms), meshes (1x, worst 9.0 ms)
- **teleport-louisville**: set down in downtown Louisville (133 km away), standing; the frames over 8 ms were made by: interior plans (7x, worst 14.2 ms), ground (1x, worst 9.2 ms), wall drifts (1x, worst 2.9 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (4x, worst 20.5 ms), cutaway (1x, worst 12.4 ms), kerbside cars (1x, worst 10.1 ms), fields (1x, worst 8.7 ms), colliders (1x, worst 2.1 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.550 | 0% | 7,292 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.069 | 0% | 328 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.445 | 0% | 2,092 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Shelby Street, at the game's starting zoom | 0.085 | 0% | 3,017 |
| `iso_downtown_day.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.416 | 0% | 72 |
| `iso_downtown_night.png` | Downtown Falmouth: the middle of the square, on West Shelby Street | 0.088 | 0% | 63 |
| `iso_residential_day.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.472 | 0% | 60 |
| `iso_residential_night.png` | The densest residential street: West 4th Street, 5 homes within 30 m | 0.084 | 0% | 57 |
| `iso_intersection_day.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.350 | 0% | 63 |
| `iso_intersection_night.png` | The busiest intersection: Main Street / West Shelby Street / East Shelby Street (4 ways in) | 0.084 | 0% | 75 |
| `iso_green_day.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.590 | 0% | 51 |
| `iso_green_night.png` | Green: park 'Max Goldberg Park', 3,874 m² inside the square | 0.080 | 0% | 58 |
| `iso_parking_day.png` | Parking: a parking, 2,703 m² inside the square | 0.388 | 0% | 62 |
| `iso_parking_night.png` | Parking: a parking, 2,703 m² inside the square | 0.080 | 0% | 53 |
| `iso_edge_day.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.359 | 0% | 2,671 |
| `iso_edge_night.png` | The square's edge: where Main Street leaves it (south), the playable map giving way to the scenery ring | 0.072 | 0% | 4,703 |
| `iso_waterway_day.png` | Waterway: a stream | 0.468 | 0% | 59 |
| `iso_waterway_night.png` | Waterway: a stream | 0.082 | 0% | 57 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.549 | 0% | 66 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, a commercial (1,495 m² footprint) | 0.079 | 0% | 59 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.384 | 0% | 138 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Shelby Street (downtown), looking along the street | 0.181 | 15% | 158 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.464 | 0% | 76 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West 4th Street (the densest residential street), looking along it | 0.152 | 31% | 88 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.539 | 0% | 108 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.467 | 0% | 65 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 64 |

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
| setup | yes | 53 |  |
| square: KentuckyMap.Build | yes | 6,774 |  |
| square: count | yes | 943 |  |
| square: pick spots | yes | 116 |  |
| square: pictures | yes | 23,814 |  |
| square: discard | yes | 296 |  |
| square: passes (warm rebuild) | yes | 5,670 |  |
| chunks: stress test | yes | 1,463 |  |
| chunks: Louisville 3x3 | yes | 2,047 |  |
| chunks: determinism | yes | 259 |  |
| perf: Lite against Full | yes | 1,249 |  |
| perf: determinism across build orders | yes | 449 |  |
| perf: what things cost | yes | 1,678 |  |
| perf: walking and driving through the state | yes | 163,974 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
