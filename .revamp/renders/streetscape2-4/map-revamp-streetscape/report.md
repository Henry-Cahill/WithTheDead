# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 190.1 s, started 2026-10-24 11:18:51.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 7,485 ms |
| the same square again, warm, pass by pass | 4,137 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 102.7, -33.0, facing -70° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 901 containers, 950 lights, 334 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 28,745 | 15,731 | 15,731 | 14,785 | 7,522,144 | 4,599,329 | 136 | 36,651 | 966 (0) | 8,384 | 11,196 | 901 |
| RaidOverhead | 588 | 470 | 470 | 470 | 467,080 | 275,200 | 134 | 1,906 | 0 (0) | 0 | 470 | 0 |
| RaidScenery | 118 | 117 | 117 | 117 | 81,212 | 104,870 | 44 | 341 | 0 (0) | 0 | 72 | 0 |
| **all** | 29,451 | 16,318 | 16,318 | 15,372 | 8,070,436 | 4,979,399 | 241 | 38,898 | 966 (0) | 8,384 | 11,738 | 901 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,012 | 15,293 | 15,293 | 14,366 | 6,277,558 | 3,957,372 | 82 | 34,242 | 928 (0) | 6,101 | 10,923 | 809 |
| World/Building shells | 131 | 130 | 130 | 130 | 815,118 | 409,876 | 48 | 1,221 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 327,635 | 162,474 | 119 | 981 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 302,830 | 175,550 | 18 | 620 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 124,997 | 54,019 | 13 | 545 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 132 | 132 | 132 | 132 | 46,667 | 27,335 | 47 | 448 | 0 (0) | 0 | 132 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 37,216 | 22,164 | 37 | 238 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,228 | 426 | 5 | 72 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 29 | 29 | 29 | 29 | 2,478 | 4,392 | 1 | 29 | 0 (0) | 0 | 29 | 0 |
| RaidOverhead/Wires | 26 | 26 | 26 | 26 | 4,728 | 8,226 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| World/StreetLamp | 44 | 22 | 22 | 3 | 1,617 | 2,500 | 3 | 22 | 22 (0) | 22 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,286 | 77,886 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 20,702 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 92 | 2% | 1 | 92 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 3 | 0% | 1 | 3 |
| lots | 1 | 0% | 4 | 0 |
| cemeteries | 5 | 0% | 500 | 0 |
| grounds | 0 | 0% | 2 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 22 | 1% | 1 | 22 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 24 | 1% | 123 | 1 |
| sidewalks | 27 | 1% | 67 | 5 |
| berms and driveways | 22 | 1% | 62 | 6 |
| road markings | 1 | 0% | 10 | 0 |
| paths | 2 | 0% | 72 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 103 | 2% | 42 | 5 |
| interior plans | 170 | 4% | 117 | 9 |
| interior shell | 14 | 0% | 116 | 0 |
| interior walls | 164 | 4% | 116 | 19 |
| interior floors | 17 | 0% | 116 | 1 |
| interior doors | 610 | 15% | 232 | 42 |
| interior switches | 79 | 2% | 116 | 4 |
| interior rooms | 2,227 | 54% | 697 | 33 |
| interior wall meshes | 95 | 2% | 116 | 5 |
| interior finish | 193 | 5% | 116 | 5 |
| cutaway | 10 | 0% | 5 | 2 |
| utility lines | 4 | 0% | 26 | 1 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 90 | 3 |
| service drops | 1 | 0% | 251 | 0 |
| street lights | 3 | 0% | 48 | 0 |
| signs | 1 | 0% | 115 | 0 |
| street furniture | 3 | 0% | 255 | 1 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 5 | 0% | 56 | 3 |
| nature plan | 1 | 0% | 1 | 1 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 11 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 4 | 0% | 9370 | 0 |
| driveway cars | 7 | 0% | 30 | 4 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 16 | 0% | 45 | 8 |
| kerbside cars | 16 | 0% | 204 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 27 | 1% | 117 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 1 | 0% | 1 | 1 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 14 | 0% | 1 | 14 |
| (emit the overhead meshes) | 12 | 0% | 1 | 12 |
| (emit the outdoor meshes) | 104 | 3% | 1 | 104 |
| **total** | **4,137** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **85** | signs 39 | 1 | 134 | 65 | 132,699 | 69,450 | 1 | 111 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **34** | interior plans 3 | 1 | 144 | 64 | 130,318 | 74,324 | 4 | 207 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 171 + 11 | **64** | interior plans 6 | 1 | 216 | 53 | 137,060 | 73,087 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 415 + 38 | **77** | buildings 31 | 0 | 134 | 58 | 128,992 | 68,128 | 1 | 98 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **17** | streets 3 | 0 | 63 | 42 | 54,459 | 40,083 | 0 | 145 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.5 | 1 | 2.5 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.6 | 1 | 0.6 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.3 | 1 | 1.3 |
| junctions | 0.8 | 1 | 0.8 |
| streets | 5.8 | 3 | 2.4 |
| sidewalks | 3.1 | 2 | 2.2 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 1.2 | 1 | 1.2 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.2 | 3 | 2.3 |
| interior plans | 11.1 | 5 | 3.0 |
| cutaway | 0.9 | 1 | 0.9 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 39.1 | 2 | 38.6 |
| street furniture | 0.4 | 1 | 0.4 |
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
| woods | 0.3 | 4 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.5 | 1 | 0.5 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.7 | 1 | 1.7 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 1.2 | 1 | 1.2 |
| objects | 2.1 | 1 | 2.1 |
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
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.3 | 1 | 1.3 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 3.6 | 2 | 2.1 |
| interior plans | 12.7 | 5 | 3.4 |
| cutaway | 1.2 | 1 | 1.2 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.1 | 1 | 0.1 |
| signs | 0.8 | 1 | 0.8 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 2.8 | 2 | 2.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.9 | 1 | 0.9 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.3 | 4 | 0.4 |
| driveway cars | 0.5 | 1 | 0.5 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.8 | 1 | 0.8 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.5 | 1 | 1.5 |
| objects | 1.2 | 1 | 1.2 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 3.1 | 1 | 3.1 |
| water | 3.5 | 2 | 3.5 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 2.4 | 2 | 2.4 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 4.3 | 3 | 2.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 13.8 | 6 | 3.7 |
| interior plans | 19.3 | 6 | 6.1 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.2 | 1 | 1.2 |
| wires | 0.1 | 1 | 0.1 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.4 | 1 | 0.4 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.1 |
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
| colliders | 1.5 | 1 | 1.5 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.5 | 1 | 0.5 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 5.7 | 2 | 5.3 |
| sidewalks | 2.9 | 2 | 2.4 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 38.5 | 5 | 31.2 |
| interior plans | 11.8 | 4 | 5.3 |
| cutaway | 1.2 | 1 | 1.2 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.8 | 1 | 0.8 |
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
| woods | 0.3 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.9 | 1 | 0.9 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.9 | 1 | 1.9 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.0 | 1 | 1.0 |
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
| lots | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 2.8 | 2 | 2.7 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.5 | 1 | 0.5 |
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
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| woods | 4.2 | 4 | 1.7 |
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
| colliders | 0.6 | 1 | 0.6 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.3 | 1 | 0.3 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 59 | 15 |
| 186_141 | yes | 13 | 3 |
| 185_141 | yes | 9 | 3 |
| 185_142 | yes | 16 | 8 |
| 183_148 | yes | 171 | 11 |
| 196_145 | yes | 415 | 38 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 125 | 24 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 542 ms (slowest 97 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **49** | interior plans 6 | 1 | 216 | 53 | 137,060 | 73,087 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **63** | interior plans 8 | 1 | 191 | 75 | 145,127 | 79,597 | 0 | 201 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 125 + 24 | **71** | interior plans 11 | 1 | 248 | 62 | 111,633 | 62,632 | 2 | 199 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **88** | interior plans 36 | 0 | 162 | 57 | 126,776 | 67,065 | 0 | 149 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **47** | interior plans 4 | 1 | 172 | 63 | 88,474 | 52,207 | 1 | 126 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **43** | interior plans 9 | 0 | 150 | 53 | 111,495 | 64,442 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **97** | buildings 51 | 1 | 197 | 53 | 119,921 | 65,059 | 0 | 206 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **33** | interior plans 6 | 0 | 142 | 54 | 99,066 | 58,507 | 3 | 86 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **51** | interior plans 8 | 1 | 172 | 45 | 189,238 | 104,957 | 0 | 153 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,652 | 515 | 515 | 512 | 1,128,790 | 627,553 | 132 | 2,190 | 6 (0) | 1,407 | 356 | 78 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (132,699 vertices, 69,450 triangles, 134 objects, 65 meshes; hash acae3c73a2001bfc vs acae3c73a2001bfc; 114 colliders, lights and loot containers, hash e3a809dbdfde8cd3 vs e3a809dbdfde8cd3). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 85 | 109 / 54 / 1.0 | 0 / 0.0 | 61 / 129,845 / 0.6 | 1.6 | 82.5 | 42 / 4.8 |
| rural-5km | Full | 34 | 206 / 68 / 1.5 | 0 / 0.0 | 63 / 127,719 / 0.4 | 1.9 | 30.8 | 43 / 0.5 |
| louisville-downtown | Full | 64 | 165 / 120 / 1.5 | 0 / 0.0 | 51 / 134,449 / 0.3 | 1.1 | 61.9 | 36 / 0.3 |
| lexington-downtown | Full | 77 | 96 / 61 / 0.8 | 0 / 0.0 | 53 / 126,188 / 0.3 | 1.4 | 74.6 | 39 / 0.3 |
| hazard | Full | 17 | 144 / 15 / 0.6 | 0 / 0.0 | 37 / 51,261 / 0.2 | 0.7 | 15.3 | 30 / 0.2 |
| louisville 0,0 | Full | 49 | 165 / 120 / 1.9 | 0 / 0.0 | 51 / 134,449 / 0.3 | 1.6 | 45.9 | 36 / 0.3 |
| louisville 1,0 | Full | 63 | 200 / 109 / 2.1 | 0 / 0.0 | 73 / 142,458 / 0.6 | 2.1 | 58.5 | 46 / 0.5 |
| louisville -1,0 | Full | 71 | 196 / 140 / 1.8 | 0 / 0.0 | 51 / 108,249 / 0.3 | 1.3 | 68.4 | 39 / 0.7 |
| louisville 0,1 | Full | 88 | 148 / 81 / 1.1 | 0 / 0.0 | 54 / 124,275 / 0.3 | 1.3 | 85.1 | 36 / 0.3 |
| louisville 0,-1 | Full | 47 | 125 / 69 / 1.1 | 0 / 0.0 | 58 / 84,264 / 0.5 | 1.7 | 44.6 | 37 / 0.2 |
| louisville 1,1 | Full | 43 | 120 / 74 / 1.0 | 0 / 0.0 | 50 / 108,424 / 0.3 | 1.2 | 40.9 | 35 / 0.2 |
| louisville -1,1 | Full | 97 | 205 / 114 / 1.4 | 0 / 0.0 | 50 / 116,645 / 0.2 | 1.1 | 94.0 | 37 / 0.3 |
| louisville 1,-1 | Full | 33 | 82 / 46 / 0.6 | 0 / 0.0 | 46 / 94,776 / 0.2 | 1.0 | 31.2 | 35 / 0.2 |
| louisville -1,-1 | Full | 51 | 152 / 103 / 1.3 | 0 / 0.0 | 41 / 186,101 / 0.2 | 0.9 | 49.0 | 30 / 0.8 |

The Louisville block's NavMesh: 331 sources collected in 2 ms; baked at once (as one 384 m square) in 188 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 44 | 7 | 16% | interior plans 3.0 | sidewalks 1.3 | 132,699 / 5,726 | 134 / 37 | 65 / 32 | 111 / 1 | 1 / 0 |
| rural-5km | 32 | 4 | 12% | interior plans 5.7 | objects 0.9 | 130,160 / 4,380 | 141 / 38 | 64 / 33 | 202 / 1 | 4 / 0 |
| louisville-downtown | 52 | 9 | 18% | interior plans 6.8 | buildings 3.3 | 129,810 / 7,103 | 214 / 33 | 53 / 28 | 165 / 1 | 0 / 0 |
| lexington-downtown | 47 | 6 | 12% | streets 5.5 | sidewalks 0.9 | 121,335 / 4,078 | 140 / 33 | 59 / 28 | 94 / 1 | 2 / 0 |
| hazard | 20 | 4 | 20% | streets 3.0 | objects 0.6 | 62,080 / 4,375 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 72.98 | 36.49 | make 61.18 + switch on 11.80 ms; 1602 objects; destroy 5.72 ms; 3,000 rays 3.68 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 17.96 | 8.98 | make 12.36 + switch on 5.60 ms; 10 objects; destroy 1.81 ms; 3,000 rays 2.25 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 13.02 | 6.51 | make 4.22 + switch on 8.80 ms; 2 objects; destroy 0.10 ms; 3,000 rays 2.00 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.15 | 1.08 | make 2.12 + switch on 0.04 ms (+ 8.37 ms baking on the worker); 2 objects; destroy 0.09 ms; 3,000 rays 2.46 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.52 | 213.09 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.91 | 197.84 | objects 0.77, allocate 0.01, pack 7.03, apply 0.11 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.00 | 25.08 | objects 0.85, allocate 0.02, pack 7.18 (worker), apply 0.14 ms |
| primitives | Box, turned, flat colour | 20,000 | 33.87 | 1.69 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 77.07 | 3.85 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 44.17 | 2.21 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 99.83 | 4.99 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 25.25 | 1.26 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 44.56 | 2.23 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.59 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.98 | 0.25 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 16.54 | 3.31 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 30.74 | 6.15 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 21.34 | 7.11 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 31.08 | 10.36 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 19.43 | 9.71 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 12.35 | 12.35 |  |
| objects | BoxCollider added to one object | 2,000 | 42.66 | 21.33 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 29.69 | 148.43 |  |
| objects | RoomLight.Spawn | 100 | 11.63 | 116.34 |  |
| objects | switching all of the above on | 1 | 28.35 | 28346.10 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 11.39 | 11390.90 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.65 | 10.65 | worst 0.12 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 6.31 | 631.09 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 355.96 | 355963.70 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 364.67 | 364674.00 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 20.55 | 20551.60 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,344 ms, 3,871,194 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (10) | 0.13 | 0.05 | 0.07 | 3.45 | 16.59 | 2 / 1 / 1 / 0 | landmarks 3.8 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,069 / 3,902,246 | 2 | 194 / 937 (pool 40) |
| drive-east | 2400 (209) | 0.35 | 0.05 | 3.17 | 4.92 | 8.41 | 21 / 1 / 0 / 0 | interior plans 7.5 | 40 (24 / 16) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,157 / 5,552,379 | 2 | 193 / 1,011 (pool 40) |
| teleport-louisville | 720 (99) | 0.86 | 0.05 | 6.05 | 9.21 | 36.99 | 48 / 13 / 3 / 3 | interior plans 12.9 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.2 | 9 / 73 | 50 (16 Lite) / 2,824 / 5,085,367 | 4 | 210 / 991 (pool 40) |
| drive-louisville | 1800 (164) | 0.42 | 0.05 | 2.91 | 5.64 | 34.47 | 33 / 11 / 3 / 1 | interior plans 34.4 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 19 | 55 (12 Lite) / 3,234 / 5,888,982 | 6 | 257 / 1,026 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: landmarks (1x, worst 3.8 ms)
- **drive-east**: driving east from the spawn at 90 km/h
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (8x, worst 12.9 ms), sidewalks (1x, worst 2.6 ms), wall drifts (1x, worst 2.0 ms), kerbside cars (1x, worst 1.7 ms), junctions (1x, worst 1.4 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (5x, worst 34.4 ms), points (1x, worst 13.5 ms), wall drifts (1x, worst 8.3 ms), street lights (1x, worst 6.6 ms), streets (1x, worst 4.4 ms), sidewalks (1x, worst 2.5 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.594 | 0% | 2,317 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 543 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.488 | 0% | 222 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.116 | 0% | 97 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.423 | 0% | 63 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.074 | 0% | 101 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.473 | 0% | 60 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 81 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.356 | 0% | 95 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.074 | 0% | 66 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.586 | 0% | 97 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 77 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 83 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 96 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.399 | 0% | 79 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.074 | 0% | 73 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.362 | 0% | 92 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 142 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.456 | 0% | 130 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.164 | 20% | 116 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.435 | 0% | 139 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.168 | 20% | 108 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.540 | 0% | 294 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.465 | 0% | 58 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 52 |

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
| setup | yes | 75 |  |
| square: KentuckyMap.Build | yes | 7,488 |  |
| square: count | yes | 928 |  |
| square: pick spots | yes | 78 |  |
| square: pictures | yes | 7,156 |  |
| square: discard | yes | 248 |  |
| square: passes (warm rebuild) | yes | 4,384 |  |
| chunks: stress test | yes | 1,123 |  |
| chunks: Louisville 3x3 | yes | 1,889 |  |
| chunks: determinism | yes | 169 |  |
| perf: Lite against Full | yes | 1,265 |  |
| perf: determinism across build orders | yes | 359 |  |
| perf: what things cost | yes | 1,632 |  |
| perf: walking and driving through the state | yes | 163,246 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
