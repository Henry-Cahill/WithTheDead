# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 190.7 s, started 2026-10-24 12:06:23.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,948 ms |
| the same square again, warm, pass by pass | 4,625 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 102.7, -33.0, facing -70° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 901 containers, 950 lights, 334 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 28,745 | 15,731 | 15,731 | 14,785 | 7,528,290 | 4,602,454 | 136 | 36,652 | 966 (0) | 8,382 | 11,196 | 901 |
| RaidOverhead | 584 | 466 | 466 | 466 | 464,274 | 272,438 | 134 | 1,900 | 0 (0) | 0 | 466 | 0 |
| RaidScenery | 116 | 115 | 115 | 115 | 80,834 | 104,346 | 44 | 339 | 0 (0) | 0 | 70 | 0 |
| **all** | 29,445 | 16,312 | 16,312 | 15,366 | 8,073,398 | 4,979,238 | 241 | 38,891 | 966 (0) | 8,382 | 11,732 | 901 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,012 | 15,293 | 15,293 | 14,366 | 6,277,558 | 3,957,372 | 82 | 34,242 | 928 (0) | 6,101 | 10,923 | 809 |
| World/Building shells | 131 | 130 | 130 | 130 | 815,118 | 409,876 | 48 | 1,221 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 327,635 | 162,474 | 119 | 981 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 302,932 | 175,596 | 18 | 619 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 131,030 | 57,078 | 13 | 547 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 131 | 131 | 131 | 131 | 44,704 | 26,067 | 47 | 445 | 0 (0) | 0 | 131 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 37,066 | 22,068 | 37 | 237 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,240 | 430 | 5 | 73 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 27 | 27 | 27 | 27 | 2,238 | 3,960 | 1 | 27 | 0 (0) | 0 | 27 | 0 |
| RaidOverhead/Wires | 23 | 23 | 23 | 23 | 3,885 | 6,732 | 1 | 23 | 0 (0) | 0 | 23 | 0 |
| World/StreetLamp | 44 | 22 | 22 | 3 | 1,628 | 2,520 | 3 | 22 | 22 (0) | 22 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,286 | 77,886 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 20,679 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 135 | 3% | 1 | 135 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 3 | 0% | 1 | 3 |
| lots | 1 | 0% | 4 | 0 |
| cemeteries | 6 | 0% | 500 | 0 |
| grounds | 0 | 0% | 2 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 36 | 1% | 1 | 36 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 24 | 1% | 189 | 1 |
| sidewalks | 28 | 1% | 67 | 3 |
| berms and driveways | 16 | 0% | 62 | 3 |
| road markings | 1 | 0% | 10 | 0 |
| paths | 2 | 0% | 72 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 4 | 0% | 1 | 4 |
| buildings | 119 | 3% | 46 | 8 |
| interior plans | 182 | 4% | 117 | 12 |
| interior shell | 15 | 0% | 116 | 0 |
| interior walls | 159 | 3% | 116 | 9 |
| interior floors | 17 | 0% | 116 | 0 |
| interior doors | 536 | 12% | 232 | 15 |
| interior switches | 93 | 2% | 116 | 5 |
| interior rooms | 2,634 | 57% | 697 | 28 |
| interior wall meshes | 106 | 2% | 116 | 4 |
| interior finish | 235 | 5% | 116 | 12 |
| cutaway | 12 | 0% | 6 | 2 |
| utility lines | 3 | 0% | 1 | 3 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 1 | 4 |
| street lights | 6 | 0% | 1 | 6 |
| signs | 2 | 0% | 1 | 2 |
| street furniture | 4 | 0% | 1 | 4 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 8 | 0% | 1 | 8 |
| nature plan | 1 | 0% | 1 | 1 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 13 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 1 |
| street trees | 4 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 5 | 0% | 9370 | 0 |
| driveway cars | 3 | 0% | 30 | 0 |
| buses and cruisers | 1 | 0% | 1 | 1 |
| parking lot cars | 4 | 0% | 45 | 1 |
| kerbside cars | 30 | 1% | 204 | 5 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 28 | 1% | 117 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 14 | 0% | 1 | 14 |
| (emit the overhead meshes) | 12 | 0% | 1 | 12 |
| (emit the outdoor meshes) | 101 | 2% | 1 | 101 |
| **total** | **4,625** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **87** | data 33 | 1 | 129 | 61 | 132,051 | 68,816 | 0 | 112 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **35** | interior plans 3 | 1 | 144 | 64 | 130,318 | 74,324 | 4 | 207 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 184 + 11 | **72** | interior plans 7 | 1 | 216 | 53 | 136,836 | 73,015 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 479 + 36 | **98** | buildings 38 | 1 | 133 | 57 | 127,673 | 67,380 | 1 | 98 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 15 + 2 | **20** | streets 3 | 1 | 63 | 42 | 54,063 | 39,951 | 0 | 145 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 33.1 | 1 | 33.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.7 | 1 | 0.7 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.6 | 1 | 1.6 |
| junctions | 2.3 | 1 | 2.3 |
| streets | 6.6 | 4 | 2.3 |
| sidewalks | 3.5 | 2 | 2.1 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 1.3 | 1 | 1.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 7.4 | 4 | 2.4 |
| interior plans | 15.9 | 5 | 6.7 |
| cutaway | 0.9 | 1 | 0.9 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.0 | 1 | 1.0 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.1 | 1 | 0.1 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.1 | 1 | 0.1 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.5 | 1 | 0.5 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 1 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 1.3 | 1 | 1.3 |
| objects | 1.8 | 1 | 1.8 |
| meshes | 1.0 | 1 | 1.0 |

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
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.3 | 1 | 1.3 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 4.5 | 3 | 2.1 |
| interior plans | 11.5 | 5 | 3.1 |
| cutaway | 0.9 | 1 | 0.9 |
| utility lines | 0.0 | 1 | 0.0 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.8 | 1 | 0.8 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.3 | 1 | 0.3 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 2.8 | 2 | 2.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.7 | 1 | 0.7 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.2 | 4 | 0.4 |
| driveway cars | 0.5 | 1 | 0.5 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.2 | 1 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.7 | 1 | 0.7 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.9 | 1 | 1.9 |
| objects | 2.0 | 1 | 2.0 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 2.7 | 1 | 2.7 |
| water | 3.6 | 2 | 3.6 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.8 | 1 | 0.8 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.1 | 1 | 1.1 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.3 | 1 | 1.3 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 5.0 | 3 | 2.2 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 16.7 | 6 | 4.6 |
| interior plans | 21.0 | 6 | 7.0 |
| cutaway | 1.1 | 1 | 1.1 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.6 | 1 | 1.6 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.5 | 1 | 0.5 |
| kerbside cars | 1.8 | 1 | 1.8 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.8 | 2 | 2.3 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.7 | 1 | 1.7 |
| objects | 1.3 | 1 | 1.3 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.8 | 1 | 0.8 |
| ground | 1.1 | 1 | 1.1 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.7 | 1 | 0.7 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 1.6 | 1 | 1.6 |
| junctions | 7.3 | 1 | 7.3 |
| streets | 5.9 | 2 | 5.5 |
| sidewalks | 3.0 | 2 | 2.5 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 45.9 | 5 | 38.0 |
| interior plans | 12.5 | 5 | 3.9 |
| cutaway | 1.5 | 1 | 1.5 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.2 | 1 | 1.2 |
| street lights | 0.4 | 1 | 0.4 |
| signs | 0.6 | 1 | 0.6 |
| street furniture | 0.9 | 1 | 0.9 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.2 | 1 | 0.2 |
| wires | 0.8 | 1 | 0.8 |
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
| kerbside cars | 1.1 | 1 | 1.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.7 | 2 | 2.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.5 | 1 | 1.5 |
| colliders | 0.8 | 1 | 0.8 |
| objects | 1.1 | 1 | 1.1 |
| meshes | 0.6 | 1 | 0.6 |

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
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.3 | 1 | 0.3 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 3.2 | 2 | 3.0 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.3 | 1 | 1.3 |
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
| fence rows | 0.7 | 1 | 0.7 |
| woods | 5.2 | 5 | 2.1 |
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
| colliders | 0.7 | 1 | 0.7 |
| objects | 1.5 | 1 | 1.5 |
| meshes | 0.3 | 1 | 0.3 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 61 | 31 |
| 186_141 | yes | 13 | 3 |
| 185_141 | yes | 18 | 2 |
| 185_142 | yes | 19 | 3 |
| 183_148 | yes | 184 | 11 |
| 196_145 | yes | 479 | 36 |
| 210_134 | yes | 15 | 2 |
| 182_148 | yes | 151 | 23 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 605 ms (slowest 107 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **53** | interior plans 7 | 1 | 216 | 53 | 136,836 | 73,015 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **68** | interior plans 9 | 1 | 191 | 75 | 145,001 | 79,555 | 0 | 201 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 151 + 23 | **73** | interior plans 10 | 1 | 249 | 62 | 112,739 | 63,281 | 3 | 199 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **99** | interior plans 36 | 1 | 163 | 58 | 126,897 | 67,211 | 0 | 149 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **61** | fences and walls 8 | 1 | 182 | 63 | 94,616 | 55,618 | 1 | 130 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **44** | interior plans 8 | 1 | 150 | 53 | 112,971 | 65,304 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **107** | buildings 55 | 1 | 197 | 53 | 119,817 | 65,116 | 0 | 206 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **41** | interior plans 6 | 0 | 147 | 56 | 99,349 | 59,005 | 4 | 88 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **60** | interior plans 9 | 1 | 173 | 46 | 190,189 | 105,480 | 0 | 153 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,670 | 519 | 519 | 514 | 1,138,415 | 633,585 | 134 | 2,215 | 8 (0) | 1,413 | 358 | 80 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (132,051 vertices, 68,816 triangles, 129 objects, 61 meshes; hash 9e54ecb61ce35229 vs 9e54ecb61ce35229; 114 colliders, lights and loot containers, hash c85ae33ac3a5b153 vs c85ae33ac3a5b153). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 87 | 111 / 54 / 1.1 | 0 / 0.0 | 59 / 129,368 / 0.5 | 1.4 | 84.3 | 41 / 3.9 |
| rural-5km | Full | 35 | 206 / 68 / 1.9 | 0 / 0.0 | 63 / 127,719 / 0.2 | 2.5 | 30.4 | 43 / 0.5 |
| louisville-downtown | Full | 72 | 165 / 120 / 1.7 | 0 / 0.0 | 51 / 134,225 / 0.3 | 1.9 | 68.2 | 36 / 0.3 |
| lexington-downtown | Full | 98 | 96 / 61 / 0.8 | 0 / 0.0 | 53 / 124,944 / 0.5 | 1.8 | 95.1 | 39 / 0.3 |
| hazard | Full | 20 | 144 / 15 / 0.7 | 0 / 0.0 | 37 / 50,865 / 0.2 | 1.4 | 18.1 | 30 / 0.3 |
| louisville 0,0 | Full | 53 | 165 / 120 / 1.7 | 0 / 0.0 | 51 / 134,225 / 0.3 | 1.3 | 50.5 | 36 / 0.5 |
| louisville 1,0 | Full | 68 | 200 / 109 / 2.1 | 0 / 0.0 | 73 / 142,332 / 0.5 | 1.9 | 63.7 | 46 / 0.3 |
| louisville -1,0 | Full | 73 | 195 / 140 / 2.4 | 0 / 0.0 | 52 / 109,316 / 0.3 | 1.5 | 68.7 | 40 / 1.3 |
| louisville 0,1 | Full | 99 | 148 / 81 / 1.3 | 0 / 0.0 | 54 / 124,387 / 0.5 | 1.6 | 96.0 | 36 / 2.0 |
| louisville 0,-1 | Full | 61 | 129 / 74 / 1.2 | 0 / 0.0 | 58 / 90,367 / 0.4 | 1.5 | 58.2 | 37 / 0.3 |
| louisville 1,1 | Full | 44 | 120 / 74 / 1.1 | 0 / 0.0 | 50 / 109,900 / 0.3 | 1.3 | 41.5 | 35 / 0.4 |
| louisville -1,1 | Full | 107 | 205 / 114 / 2.6 | 0 / 0.0 | 50 / 116,532 / 0.3 | 1.2 | 103.2 | 37 / 0.3 |
| louisville 1,-1 | Full | 41 | 83 / 48 / 0.9 | 0 / 0.0 | 47 / 95,029 / 0.3 | 1.4 | 38.7 | 36 / 0.3 |
| louisville -1,-1 | Full | 60 | 152 / 103 / 2.3 | 0 / 0.0 | 42 / 187,052 / 0.2 | 1.3 | 56.4 | 30 / 0.3 |

The Louisville block's NavMesh: 333 sources collected in 3 ms; baked at once (as one 384 m square) in 202 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 48 | 10 | 21% | interior plans 3.1 | sidewalks 1.9 | 132,051 / 5,904 | 129 / 37 | 61 / 32 | 112 / 1 | 0 / 0 |
| rural-5km | 34 | 5 | 14% | interior plans 3.2 | objects 1.1 | 130,160 / 4,380 | 141 / 38 | 64 / 33 | 202 / 1 | 4 / 0 |
| louisville-downtown | 59 | 8 | 14% | interior plans 7.5 | buildings 2.8 | 129,586 / 7,103 | 214 / 33 | 53 / 28 | 165 / 1 | 0 / 0 |
| lexington-downtown | 47 | 6 | 14% | streets 5.7 | sidewalks 1.1 | 119,983 / 4,078 | 138 / 33 | 57 / 28 | 94 / 1 | 2 / 0 |
| hazard | 27 | 6 | 24% | streets 3.2 | junctions 1.0 | 61,684 / 4,375 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 60.07 | 30.04 | make 50.98 + switch on 9.09 ms; 1602 objects; destroy 3.64 ms; 3,000 rays 2.74 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 20.49 | 10.24 | make 14.15 + switch on 6.34 ms; 10 objects; destroy 2.17 ms; 3,000 rays 2.08 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 15.24 | 7.62 | make 5.11 + switch on 10.13 ms; 2 objects; destroy 0.13 ms; 3,000 rays 2.21 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.35 | 1.17 | make 2.29 + switch on 0.05 ms (+ 11.00 ms baking on the worker); 2 objects; destroy 0.09 ms; 3,000 rays 3.04 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 28.27 | 706.64 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 11.39 | 284.80 | objects 1.12, allocate 0.02, pack 10.07, apply 0.18 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.82 | 45.42 | objects 1.63, allocate 0.06, pack 8.25 (worker), apply 0.13 ms |
| primitives | Box, turned, flat colour | 20,000 | 32.62 | 1.63 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 99.78 | 4.99 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 70.87 | 3.54 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 89.97 | 4.50 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 31.11 | 1.56 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 54.59 | 2.73 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 7.40 | 0.37 | 80,000 vertices |
| primitives | Tri | 20,000 | 7.14 | 0.36 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 26.69 | 5.34 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 35.30 | 7.06 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 23.58 | 7.86 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 46.82 | 15.61 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 25.40 | 12.70 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 18.98 | 18.98 |  |
| objects | BoxCollider added to one object | 2,000 | 52.87 | 26.44 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 41.04 | 205.20 |  |
| objects | RoomLight.Spawn | 100 | 16.97 | 169.66 |  |
| objects | switching all of the above on | 1 | 37.70 | 37703.00 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 16.24 | 16237.70 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 1.26 | 20.60 | worst 0.58 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 7.89 | 789.09 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 430.81 | 430807.20 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 439.76 | 439758.20 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 30.16 | 30161.50 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,462 ms, 3,881,054 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (8) | 0.12 | 0.04 | 0.07 | 3.94 | 14.66 | 2 / 1 / 0 / 0 | landmarks 3.6 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,067 / 3,912,106 | 2 | 194 / 935 (pool 40) |
| drive-east | 2400 (221) | 0.37 | 0.05 | 3.54 | 4.92 | 9.50 | 17 / 3 / 0 / 0 | colliders 8.9 | 40 (24 / 16) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,155 / 5,563,507 | 2 | 195 / 1,009 (pool 40) |
| teleport-louisville | 720 (103) | 0.88 | 0.05 | 5.71 | 9.14 | 37.33 | 50 / 15 / 3 / 2 | interior plans 10.0 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.3 | 10 / 75 | 50 (16 Lite) / 2,830 / 5,104,112 | 4 | 213 / 990 (pool 40) |
| drive-louisville | 1800 (172) | 0.42 | 0.05 | 3.25 | 5.65 | 34.85 | 26 / 10 / 3 / 1 | interior plans 34.8 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 21 | 55 (12 Lite) / 3,237 / 5,910,027 | 6 | 258 / 1,025 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: landmarks (1x, worst 3.6 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: colliders (1x, worst 8.9 ms), signs (1x, worst 7.9 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (9x, worst 10.0 ms), junctions (2x, worst 9.4 ms), sidewalks (1x, worst 2.5 ms), wall drifts (1x, worst 2.1 ms), parking lot cars (1x, worst 1.7 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (5x, worst 34.8 ms), points (1x, worst 11.1 ms), (swap) (1x, worst 10.4 ms), junctions (1x, worst 10.3 ms), sidewalks (1x, worst 2.8 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 36 edit-mode artefacts

- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.593 | 0% | 1,875 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 395 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.485 | 0% | 118 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.124 | 0% | 200 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.420 | 0% | 81 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 66 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.473 | 0% | 79 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 55 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.352 | 0% | 65 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.073 | 0% | 56 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.586 | 0% | 59 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 61 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 84 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 64 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.397 | 0% | 63 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.073 | 0% | 54 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.362 | 0% | 65 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 58 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.455 | 0% | 140 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.165 | 20% | 153 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.433 | 0% | 146 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.166 | 20% | 127 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.538 | 0% | 117 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.464 | 0% | 54 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.080 | 0% | 55 |

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
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 66 |  |
| square: KentuckyMap.Build | yes | 6,951 |  |
| square: count | yes | 995 |  |
| square: pick spots | yes | 113 |  |
| square: pictures | yes | 6,666 |  |
| square: discard | yes | 333 |  |
| square: passes (warm rebuild) | yes | 4,898 |  |
| chunks: stress test | yes | 1,272 |  |
| chunks: Louisville 3x3 | yes | 1,879 |  |
| chunks: determinism | yes | 165 |  |
| perf: Lite against Full | yes | 1,403 |  |
| perf: determinism across build orders | yes | 357 |  |
| perf: what things cost | yes | 1,983 |  |
| perf: walking and driving through the state | yes | 163,624 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
