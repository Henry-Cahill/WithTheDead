# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 186.8 s, started 2026-10-24 11:16:12.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,587 ms |
| the same square again, warm, pass by pass | 4,001 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 10.8, 142.2, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 901 containers, 956 lights, 337 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,310 | 16,234 | 16,234 | 15,281 | 7,570,463 | 4,628,335 | 133 | 37,229 | 972 (0) | 8,391 | 11,708 | 901 |
| RaidOverhead | 584 | 466 | 466 | 466 | 463,169 | 271,937 | 134 | 1,863 | 0 (0) | 0 | 466 | 0 |
| RaidScenery | 115 | 114 | 114 | 114 | 80,792 | 104,158 | 44 | 341 | 0 (0) | 0 | 69 | 0 |
| **all** | 30,009 | 16,814 | 16,814 | 15,861 | 8,114,424 | 5,004,430 | 238 | 39,433 | 972 (0) | 8,391 | 12,243 | 901 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,579 | 15,793 | 15,793 | 14,862 | 6,322,098 | 3,984,553 | 82 | 34,799 | 932 (0) | 6,120 | 11,435 | 811 |
| World/Building shells | 131 | 130 | 130 | 130 | 815,118 | 409,876 | 48 | 1,221 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 327,655 | 162,505 | 119 | 949 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 297,983 | 174,304 | 15 | 611 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 133,403 | 56,750 | 13 | 572 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 130 | 130 | 130 | 130 | 43,282 | 24,959 | 47 | 439 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 37,174 | 22,144 | 37 | 237 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,240 | 430 | 5 | 76 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 26 | 26 | 26 | 26 | 2,088 | 3,696 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| World/StreetLamp | 50 | 25 | 25 | 3 | 1,837 | 2,840 | 3 | 25 | 24 (0) | 25 | 0 | 0 |
| RaidOverhead/Wires | 24 | 24 | 24 | 24 | 4,182 | 7,308 | 1 | 24 | 0 (0) | 0 | 24 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,286 | 77,886 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 21,106 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 98 | 2% | 1 | 98 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| lots | 3 | 0% | 1 | 3 |
| cemeteries | 4 | 0% | 4 | 1 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 39 | 1% | 1 | 39 |
| road network | 8 | 0% | 1 | 8 |
| junctions | 9 | 0% | 1 | 9 |
| streets | 28 | 1% | 67 | 18 |
| sidewalks | 23 | 1% | 67 | 2 |
| berms and driveways | 15 | 0% | 62 | 1 |
| road markings | 1 | 0% | 17 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 112 | 3% | 44 | 7 |
| interior plans | 250 | 6% | 117 | 16 |
| interior shell | 12 | 0% | 116 | 0 |
| interior walls | 132 | 3% | 116 | 10 |
| interior floors | 13 | 0% | 116 | 0 |
| interior doors | 437 | 11% | 232 | 12 |
| interior switches | 62 | 2% | 116 | 3 |
| interior rooms | 2,213 | 55% | 697 | 35 |
| interior wall meshes | 94 | 2% | 116 | 6 |
| interior finish | 191 | 5% | 116 | 9 |
| cutaway | 42 | 1% | 20 | 2 |
| utility lines | 4 | 0% | 1 | 4 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 1 | 4 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 3 | 0% | 1 | 3 |
| street furniture | 15 | 0% | 1 | 15 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 24 | 1% | 1 | 24 |
| nature plan | 2 | 0% | 1 | 2 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 13 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 3 | 0% | 9370 | 0 |
| driveway cars | 2 | 0% | 29 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 45 | 0 |
| kerbside cars | 13 | 0% | 200 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 35 | 1% | 117 | 5 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 12 | 0% | 1 | 12 |
| (emit the overhead meshes) | 9 | 0% | 1 | 9 |
| (emit the outdoor meshes) | 51 | 1% | 1 | 51 |
| **total** | **4,001** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **97** | data 30 | 1 | 131 | 61 | 135,373 | 70,023 | 0 | 109 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **33** | interior plans 3 | 1 | 145 | 64 | 130,378 | 74,344 | 4 | 207 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 174 + 17 | **68** | interior plans 6 | 1 | 213 | 49 | 136,468 | 72,560 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 409 + 23 | **85** | buildings 37 | 0 | 134 | 57 | 128,675 | 67,738 | 1 | 99 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **18** | streets 3 | 0 | 85 | 43 | 59,902 | 43,511 | 0 | 155 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 29.8 | 1 | 29.8 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.5 | 1 | 1.5 |
| junctions | 1.0 | 1 | 1.0 |
| streets | 6.8 | 1 | 6.8 |
| sidewalks | 3.0 | 2 | 2.1 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 1.5 | 1 | 1.5 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 18.6 | 6 | 6.9 |
| interior plans | 11.4 | 5 | 2.9 |
| cutaway | 3.2 | 2 | 2.3 |
| utility lines | 0.8 | 1 | 0.8 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.8 | 1 | 1.8 |
| street furniture | 0.7 | 1 | 0.7 |
| landmarks | 0.3 | 1 | 0.3 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.6 | 1 | 0.6 |
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
| kerbside cars | 0.4 | 1 | 0.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 1 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 1.1 | 1 | 1.1 |
| objects | 3.0 | 1 | 3.0 |
| meshes | 4.6 | 1 | 4.6 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.2 | 1 | 0.2 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.9 | 1 | 1.9 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.8 | 1 | 0.8 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 3.9 | 2 | 2.1 |
| interior plans | 9.6 | 5 | 2.7 |
| cutaway | 3.0 | 2 | 2.2 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.8 | 1 | 0.8 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 2.4 | 2 | 2.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.7 | 1 | 0.7 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.2 | 4 | 0.4 |
| driveway cars | 0.4 | 1 | 0.4 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.7 | 1 | 0.7 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.3 | 1 | 1.3 |
| objects | 1.7 | 1 | 1.7 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.7 | 1 | 0.7 |
| ground | 2.6 | 1 | 2.6 |
| water | 2.7 | 1 | 2.7 |
| fields | 0.1 | 1 | 0.1 |
| lots | 0.6 | 1 | 0.6 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.2 | 1 | 1.2 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.7 | 1 | 0.7 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 1.3 | 1 | 1.3 |
| sidewalks | 1.6 | 1 | 1.6 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 4.5 | 1 | 4.5 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 16.3 | 6 | 4.3 |
| interior plans | 18.4 | 6 | 6.1 |
| cutaway | 3.3 | 2 | 2.5 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.3 | 1 | 1.3 |
| wires | 0.0 | 1 | 0.0 |
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
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 2.4 | 2 | 2.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.4 | 1 | 1.4 |
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
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.5 | 1 | 0.5 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 6.7 | 1 | 6.7 |
| sidewalks | 2.8 | 2 | 2.3 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 45.3 | 5 | 37.4 |
| interior plans | 11.1 | 4 | 5.3 |
| cutaway | 3.0 | 2 | 2.2 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.7 | 1 | 0.7 |
| street furniture | 0.8 | 1 | 0.8 |
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
| kerbside cars | 0.9 | 1 | 0.9 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.9 | 1 | 1.9 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.0 | 1 | 1.0 |
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
| lots | 0.9 | 1 | 0.9 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.1 | 1 | 0.1 |
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
| signs | 1.0 | 1 | 1.0 |
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
| parking lot cars | 0.8 | 1 | 0.8 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 40 | 8 |
| 186_141 | yes | 31 | 8 |
| 185_141 | yes | 8 | 2 |
| 185_142 | yes | 9 | 2 |
| 183_148 | yes | 174 | 17 |
| 196_145 | yes | 409 | 23 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 154 | 7 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 531 ms (slowest 95 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **48** | interior plans 6 | 1 | 213 | 49 | 136,468 | 72,560 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **70** | colliders 9 | 1 | 192 | 75 | 144,869 | 79,388 | 0 | 201 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 154 + 7 | **56** | interior plans 8 | 1 | 232 | 55 | 106,738 | 58,817 | 1 | 187 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **72** | interior plans 30 | 0 | 164 | 59 | 122,738 | 64,210 | 1 | 139 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **48** | fences and walls 7 | 0 | 171 | 60 | 86,007 | 49,162 | 1 | 122 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **40** | interior plans 9 | 0 | 151 | 53 | 112,093 | 64,740 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **95** | buildings 49 | 1 | 183 | 53 | 115,522 | 62,258 | 0 | 194 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **34** | interior plans 5 | 0 | 134 | 54 | 97,445 | 57,255 | 3 | 81 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **66** | interior plans 17 | 0 | 169 | 41 | 187,404 | 103,139 | 0 | 150 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,611 | 499 | 499 | 496 | 1,109,284 | 611,529 | 131 | 2,123 | 6 (0) | 1,361 | 340 | 76 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (135,373 vertices, 70,023 triangles, 131 objects, 61 meshes; hash 950ff4e4f094a51c vs 950ff4e4f094a51c; 111 colliders, lights and loot containers, hash bac77ee04f661af5 vs bac77ee04f661af5). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 97 | 108 / 55 / 1.0 | 0 / 0.0 | 59 / 132,699 / 3.4 | 4.1 | 92.3 | 41 / 3.8 |
| rural-5km | Full | 33 | 206 / 68 / 1.3 | 0 / 0.0 | 63 / 127,779 / 2.2 | 4.3 | 27.3 | 43 / 0.4 |
| louisville-downtown | Full | 68 | 165 / 120 / 1.4 | 0 / 0.0 | 48 / 133,875 / 2.9 | 3.8 | 62.8 | 36 / 0.4 |
| lexington-downtown | Full | 85 | 97 / 61 / 0.7 | 0 / 0.0 | 53 / 125,946 / 2.4 | 3.4 | 80.5 | 39 / 0.3 |
| hazard | Full | 18 | 154 / 24 / 0.7 | 0 / 0.0 | 38 / 56,344 / 0.1 | 0.8 | 16.5 | 30 / 0.2 |
| louisville 0,0 | Full | 48 | 165 / 120 / 1.4 | 0 / 0.0 | 48 / 133,875 / 2.3 | 3.3 | 43.4 | 36 / 0.3 |
| louisville 1,0 | Full | 70 | 200 / 109 / 9.9 | 0 / 0.0 | 73 / 142,200 / 2.8 | 4.5 | 55.7 | 46 / 0.4 |
| louisville -1,0 | Full | 56 | 185 / 131 / 1.5 | 0 / 0.0 | 49 / 103,840 / 5.6 | 6.6 | 48.4 | 38 / 0.3 |
| louisville 0,1 | Full | 72 | 137 / 74 / 0.9 | 0 / 0.0 | 54 / 120,198 / 2.2 | 3.4 | 68.0 | 37 / 0.3 |
| louisville 0,-1 | Full | 48 | 121 / 70 / 1.1 | 0 / 0.0 | 55 / 82,730 / 1.1 | 2.1 | 44.5 | 37 / 0.3 |
| louisville 1,1 | Full | 40 | 120 / 74 / 1.0 | 0 / 0.0 | 50 / 109,022 / 2.1 | 3.1 | 36.2 | 35 / 0.3 |
| louisville -1,1 | Full | 95 | 193 / 104 / 1.3 | 0 / 0.0 | 50 / 112,390 / 1.8 | 2.8 | 91.2 | 37 / 0.3 |
| louisville 1,-1 | Full | 34 | 77 / 42 / 0.6 | 0 / 0.0 | 46 / 93,191 / 1.2 | 2.1 | 31.8 | 35 / 0.3 |
| louisville -1,-1 | Full | 66 | 149 / 103 / 1.2 | 0 / 0.0 | 39 / 184,774 / 3.6 | 4.3 | 60.7 | 30 / 0.3 |

The Louisville block's NavMesh: 331 sources collected in 4 ms; baked at once (as one 384 m square) in 218 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 44 | 13 | 30% | streets 6.7 | streets 5.2 | 135,373 / 11,208 | 131 / 37 | 61 / 32 | 109 / 1 | 0 / 0 |
| rural-5km | 43 | 10 | 24% | interior plans 3.6 | streets 3.1 | 130,220 / 6,786 | 142 / 38 | 64 / 33 | 202 / 1 | 4 / 0 |
| louisville-downtown | 51 | 11 | 21% | interior plans 6.2 | buildings 2.5 | 129,198 / 10,503 | 211 / 36 | 49 / 31 | 165 / 1 | 0 / 0 |
| lexington-downtown | 42 | 11 | 28% | streets 6.7 | streets 5.4 | 121,025 / 7,969 | 139 / 32 | 57 / 27 | 95 / 1 | 2 / 0 |
| hazard | 19 | 6 | 34% | streets 3.2 | streets 2.0 | 66,538 / 6,259 | 93 / 32 | 44 / 27 | 157 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 66.48 | 33.24 | make 56.96 + switch on 9.52 ms; 1602 objects; destroy 3.15 ms; 3,000 rays 2.79 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 17.48 | 8.74 | make 11.72 + switch on 5.76 ms; 10 objects; destroy 2.08 ms; 3,000 rays 2.02 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 13.02 | 6.51 | make 4.87 + switch on 8.14 ms; 2 objects; destroy 0.12 ms; 3,000 rays 2.04 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.27 | 1.13 | make 2.23 + switch on 0.04 ms (+ 9.27 ms baking on the worker); 2 objects; destroy 0.11 ms; 3,000 rays 2.27 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.57 | 214.34 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.83 | 195.86 | objects 0.77, allocate 0.02, pack 6.93, apply 0.11 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 0.99 | 24.78 | objects 0.86, allocate 0.02, pack 6.94 (worker), apply 0.11 ms |
| primitives | Box, turned, flat colour | 20,000 | 26.68 | 1.33 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 80.34 | 4.02 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 58.31 | 2.92 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 81.96 | 4.10 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 41.33 | 2.07 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 28.41 | 1.42 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.46 | 0.32 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.73 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 15.92 | 3.18 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 29.09 | 5.82 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 20.97 | 6.99 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 61.50 | 20.50 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.91 | 6.96 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.54 | 11.54 |  |
| objects | BoxCollider added to one object | 2,000 | 39.53 | 19.76 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 26.00 | 129.98 |  |
| objects | RoomLight.Spawn | 100 | 10.72 | 107.17 |  |
| objects | switching all of the above on | 1 | 28.16 | 28161.70 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 13.15 | 13149.50 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.61 | 9.91 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.05 | 204.49 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 387.54 | 387535.50 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 377.91 | 377905.70 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 21.45 | 21444.90 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,344 ms, 3,870,754 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (11) | 0.12 | 0.04 | 0.07 | 4.60 | 11.13 | 2 / 1 / 0 / 0 | buildings 1.7 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,039 / 3,911,571 | 2 | 181 / 935 (pool 40) |
| drive-east | 2400 (267) | 0.45 | 0.05 | 3.99 | 5.32 | 13.18 | 31 / 3 / 0 / 0 | snow tone 11.8 | 41 (27 / 14) | 3 | 15 | 0.0 | 0.0 | 0 / 0 | 53 (10 Lite) / 3,213 / 5,918,717 | 2 | 183 / 1,021 (pool 40) |
| teleport-louisville | 720 (148) | 1.32 | 0.05 | 6.31 | 20.68 | 47.61 | 56 / 21 / 9 / 4 | interior plans 40.8 | 25 (9 / 16) | 0 | 29 | 0.2 | 1.9 | 11 / 113 | 50 (16 Lite) / 2,805 / 5,121,826 | 4 | 199 / 990 (pool 40) |
| drive-louisville | 1800 (228) | 0.60 | 0.05 | 3.84 | 8.67 | 37.50 | 48 / 19 / 5 / 2 | interior plans 37.5 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 38 | 55 (12 Lite) / 3,193 / 5,894,334 | 6 | 244 / 1,024 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: snow tone (1x, worst 1.3 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: snow tone (1x, worst 11.8 ms), interior plans (1x, worst 9.1 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (10x, worst 40.8 ms), buildings (2x, worst 35.9 ms), colliders (1x, worst 16.2 ms), snow tone (1x, worst 9.0 ms), nature plan (1x, worst 8.6 ms), utility poles (1x, worst 8.6 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (10x, worst 37.5 ms), landmarks (1x, worst 34.5 ms), points (1x, worst 12.3 ms), utility lines (1x, worst 9.0 ms), nature plan (1x, worst 8.9 ms), berms and driveways (2x, worst 8.2 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 36 edit-mode artefacts

- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.587 | 0% | 1,708 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 328 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.536 | 0% | 154 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.072 | 0% | 109 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.450 | 0% | 68 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 111 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.428 | 0% | 55 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.085 | 0% | 62 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.409 | 0% | 54 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.072 | 0% | 92 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.586 | 0% | 65 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 55 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 53 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 53 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.429 | 0% | 71 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.075 | 0% | 88 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.362 | 0% | 142 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 74 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.265 | 0% | 70 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.165 | 13% | 81 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.401 | 0% | 122 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.167 | 20% | 130 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.538 | 0% | 118 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.465 | 0% | 58 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 60 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 10.8, 142.2 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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
- `(edit mode) Error: Raid_sign_BOOKS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_REPUBLIC BANK050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_HOMEWOOD SUITES BY HIL37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 57 |  |
| square: KentuckyMap.Build | yes | 6,589 |  |
| square: count | yes | 870 |  |
| square: pick spots | yes | 112 |  |
| square: pictures | yes | 5,828 |  |
| square: discard | yes | 253 |  |
| square: passes (warm rebuild) | yes | 4,222 |  |
| chunks: stress test | yes | 1,111 |  |
| chunks: Louisville 3x3 | yes | 1,720 |  |
| chunks: determinism | yes | 165 |  |
| perf: Lite against Full | yes | 1,327 |  |
| perf: determinism across build orders | yes | 331 |  |
| perf: what things cost | yes | 1,660 |  |
| perf: walking and driving through the state | yes | 162,565 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
