# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 183.2 s, started 2026-10-24 11:38:59.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 5,999 ms |
| the same square again, warm, pass by pass | 3,778 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 10.8, 142.2, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 901 containers, 955 lights, 338 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,306 | 16,233 | 16,233 | 15,281 | 7,560,899 | 4,623,798 | 137 | 37,217 | 971 (0) | 8,363 | 11,709 | 901 |
| RaidOverhead | 584 | 466 | 466 | 466 | 455,177 | 268,393 | 140 | 1,844 | 0 (0) | 0 | 466 | 0 |
| RaidScenery | 115 | 114 | 114 | 114 | 80,096 | 103,810 | 48 | 360 | 0 (0) | 0 | 69 | 0 |
| **all** | 30,005 | 16,813 | 16,813 | 15,861 | 8,096,172 | 4,996,001 | 246 | 39,421 | 971 (0) | 8,363 | 12,244 | 901 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,578 | 15,792 | 15,792 | 14,862 | 6,325,035 | 3,986,318 | 82 | 34,794 | 931 (0) | 6,117 | 11,436 | 811 |
| World/Building shells | 131 | 130 | 130 | 130 | 802,262 | 403,344 | 51 | 1,216 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 320,313 | 159,227 | 126 | 947 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 298,338 | 174,534 | 15 | 608 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 133,403 | 56,750 | 14 | 573 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 130 | 130 | 130 | 130 | 42,632 | 24,693 | 50 | 422 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 36,478 | 21,796 | 41 | 256 | 0 (0) | 0 | 43 | 0 |
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
| (read the area) | 67 | 2% | 1 | 67 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| lots | 3 | 0% | 1 | 3 |
| cemeteries | 4 | 0% | 4 | 1 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 22 | 1% | 1 | 22 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 27 | 1% | 67 | 17 |
| sidewalks | 33 | 1% | 67 | 5 |
| berms and driveways | 13 | 0% | 62 | 1 |
| road markings | 1 | 0% | 17 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 112 | 3% | 44 | 7 |
| interior plans | 186 | 5% | 117 | 9 |
| interior shell | 12 | 0% | 116 | 0 |
| interior walls | 138 | 4% | 116 | 15 |
| interior floors | 17 | 0% | 116 | 3 |
| interior doors | 475 | 13% | 233 | 34 |
| interior switches | 67 | 2% | 116 | 7 |
| interior rooms | 2,089 | 55% | 697 | 33 |
| interior wall meshes | 94 | 2% | 116 | 7 |
| interior finish | 176 | 5% | 116 | 5 |
| cutaway | 41 | 1% | 20 | 2 |
| utility lines | 4 | 0% | 1 | 4 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 1 | 4 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 3 | 0% | 1 | 3 |
| street furniture | 14 | 0% | 1 | 14 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 5 | 0% | 1 | 5 |
| nature plan | 1 | 0% | 1 | 1 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 4 | 0% | 9370 | 0 |
| driveway cars | 2 | 0% | 29 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 45 | 0 |
| kerbside cars | 26 | 1% | 200 | 5 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 24 | 1% | 117 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 12 | 0% | 1 | 12 |
| (emit the overhead meshes) | 9 | 0% | 1 | 9 |
| (emit the outdoor meshes) | 51 | 1% | 1 | 51 |
| **total** | **3,778** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **77** | data 28 | 0 | 131 | 61 | 135,373 | 70,023 | 0 | 109 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **43** | interior plans 3 | 1 | 146 | 64 | 133,552 | 75,876 | 4 | 218 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 135 + 9 | **83** | cutaway 16 | 1 | 213 | 49 | 126,936 | 68,665 | 0 | 163 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 420 + 32 | **79** | buildings 31 | 0 | 134 | 57 | 128,675 | 67,738 | 1 | 99 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **18** | streets 3 | 0 | 85 | 43 | 59,902 | 43,511 | 0 | 155 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 28.4 | 1 | 28.4 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.6 | 1 | 1.6 |
| junctions | 1.4 | 1 | 1.4 |
| streets | 7.0 | 2 | 5.0 |
| sidewalks | 2.9 | 2 | 2.0 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.4 | 3 | 2.2 |
| interior plans | 10.5 | 4 | 3.8 |
| cutaway | 2.9 | 2 | 2.1 |
| utility lines | 0.8 | 1 | 0.8 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.6 | 1 | 1.6 |
| street furniture | 0.6 | 1 | 0.6 |
| landmarks | 0.3 | 1 | 0.3 |
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
| kerbside cars | 0.4 | 1 | 0.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.5 | 1 | 1.5 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 0.9 | 1 | 0.9 |
| objects | 2.7 | 1 | 2.7 |
| meshes | 1.0 | 1 | 1.0 |

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
| work yards | 0.9 | 1 | 0.9 |
| snow tone | 3.1 | 1 | 3.1 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 1.1 | 1 | 1.1 |
| streets | 2.0 | 1 | 2.0 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.8 | 1 | 0.8 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.5 | 3 | 2.1 |
| interior plans | 11.9 | 5 | 3.3 |
| cutaway | 4.4 | 3 | 2.0 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 2.3 | 1 | 2.3 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.3 | 1 | 0.3 |
| nature plan | 0.5 | 1 | 0.5 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.1 | 1 | 0.1 |
| yard trees | 2.3 | 2 | 2.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.6 | 1 | 0.6 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.4 | 4 | 0.4 |
| driveway cars | 0.4 | 1 | 0.4 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.6 | 1 | 0.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.2 | 1 | 1.2 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 2.1 | 1 | 2.1 |
| water | 2.5 | 1 | 2.5 |
| fields | 0.1 | 1 | 0.1 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.0 | 1 | 1.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 4.2 | 1 | 4.2 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 14.5 | 6 | 3.5 |
| interior plans | 22.8 | 6 | 10.4 |
| cutaway | 16.7 | 2 | 15.7 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.7 | 1 | 1.7 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.7 | 1 | 0.7 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.8 | 1 | 0.8 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.1 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 1.7 | 1 | 1.7 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.6 | 1 | 1.6 |
| objects | 0.9 | 1 | 0.9 |
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
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 6.6 | 1 | 6.6 |
| sidewalks | 2.8 | 2 | 2.3 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 39.4 | 5 | 31.4 |
| interior plans | 10.8 | 4 | 5.3 |
| cutaway | 2.7 | 2 | 2.6 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.7 | 1 | 0.7 |
| street furniture | 0.8 | 1 | 0.8 |
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
| kerbside cars | 0.9 | 1 | 0.9 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.2 | 2 | 2.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.2 | 1 | 1.2 |
| colliders | 0.8 | 1 | 0.8 |
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
| lots | 0.8 | 1 | 0.8 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.3 | 1 | 0.3 |
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
| wires | 0.6 | 1 | 0.6 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| woods | 4.3 | 4 | 1.8 |
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
| colliders | 0.6 | 1 | 0.6 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 42 | 21 |
| 186_141 | yes | 11 | 2 |
| 185_141 | yes | 7 | 2 |
| 185_142 | yes | 9 | 2 |
| 183_148 | yes | 135 | 9 |
| 196_145 | yes | 420 | 32 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 151 | 20 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 516 ms (slowest 87 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **47** | interior plans 6 | 1 | 213 | 49 | 126,936 | 68,665 | 0 | 163 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **71** | cutaway 10 | 1 | 192 | 75 | 147,467 | 80,819 | 0 | 203 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 151 + 20 | **58** | interior plans 8 | 1 | 232 | 55 | 125,187 | 68,163 | 1 | 185 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **69** | interior plans 29 | 0 | 164 | 59 | 116,246 | 60,764 | 1 | 140 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **46** | streets 6 | 0 | 171 | 60 | 86,007 | 49,162 | 1 | 122 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **40** | interior plans 9 | 0 | 151 | 53 | 126,523 | 72,249 | 0 | 122 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **87** | buildings 50 | 1 | 183 | 53 | 123,304 | 66,353 | 0 | 196 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **33** | interior plans 6 | 0 | 134 | 54 | 95,169 | 56,616 | 3 | 83 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **64** | interior plans 18 | 0 | 169 | 41 | 205,773 | 113,157 | 0 | 149 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,611 | 499 | 499 | 496 | 1,152,612 | 635,948 | 125 | 2,113 | 6 (0) | 1,363 | 340 | 76 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (135,373 vertices, 70,023 triangles, 131 objects, 61 meshes; hash 05d141613274f6ed vs 05d141613274f6ed; 111 colliders, lights and loot containers, hash bac77ee04f661af5 vs bac77ee04f661af5). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 77 | 108 / 55 / 0.8 | 0 / 0.0 | 59 / 132,699 / 2.8 | 3.8 | 72.4 | 41 / 3.5 |
| rural-5km | Full | 43 | 217 / 69 / 1.2 | 0 / 0.0 | 63 / 130,953 / 3.2 | 4.8 | 37.3 | 43 / 0.8 |
| louisville-downtown | Full | 83 | 162 / 120 / 1.6 | 0 / 0.0 | 48 / 124,343 / 15.7 | 17.1 | 64.0 | 36 / 0.5 |
| lexington-downtown | Full | 79 | 97 / 61 / 0.8 | 0 / 0.0 | 53 / 125,946 / 2.2 | 3.2 | 74.8 | 39 / 0.3 |
| hazard | Full | 18 | 154 / 24 / 0.6 | 0 / 0.0 | 38 / 56,344 / 0.1 | 0.7 | 17.0 | 30 / 0.2 |
| louisville 0,0 | Full | 47 | 162 / 120 / 1.4 | 0 / 0.0 | 48 / 124,343 / 2.2 | 3.2 | 42.6 | 36 / 0.3 |
| louisville 1,0 | Full | 71 | 202 / 109 / 1.4 | 0 / 0.0 | 73 / 144,798 / 11.5 | 13.2 | 56.0 | 46 / 0.3 |
| louisville -1,0 | Full | 58 | 183 / 131 / 2.2 | 0 / 0.0 | 49 / 122,289 / 3.1 | 3.0 | 52.7 | 38 / 0.3 |
| louisville 0,1 | Full | 69 | 138 / 74 / 0.9 | 0 / 0.0 | 54 / 113,706 / 2.1 | 3.2 | 65.3 | 37 / 0.3 |
| louisville 0,-1 | Full | 46 | 121 / 70 / 1.0 | 0 / 0.0 | 55 / 82,730 / 1.0 | 2.1 | 42.9 | 37 / 0.3 |
| louisville 1,1 | Full | 40 | 121 / 74 / 0.9 | 0 / 0.0 | 50 / 123,452 / 2.1 | 3.1 | 36.2 | 35 / 0.3 |
| louisville -1,1 | Full | 87 | 195 / 104 / 1.3 | 0 / 0.0 | 50 / 120,172 / 2.0 | 3.0 | 82.9 | 37 / 0.3 |
| louisville 1,-1 | Full | 33 | 79 / 42 / 0.6 | 0 / 0.0 | 46 / 90,915 / 1.1 | 1.9 | 30.7 | 35 / 0.3 |
| louisville -1,-1 | Full | 64 | 148 / 103 / 1.2 | 0 / 0.0 | 39 / 203,143 / 3.6 | 4.3 | 58.6 | 30 / 0.2 |

The Louisville block's NavMesh: 331 sources collected in 4 ms; baked at once (as one 384 m square) in 205 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 43 | 13 | 29% | streets 6.7 | streets 5.2 | 135,373 / 11,208 | 131 / 37 | 61 / 32 | 109 / 1 | 0 / 0 |
| rural-5km | 28 | 6 | 23% | interior plans 2.5 | streets 1.5 | 133,394 / 6,898 | 143 / 38 | 64 / 33 | 213 / 1 | 4 / 0 |
| louisville-downtown | 47 | 10 | 22% | interior plans 6.2 | paths 2.4 | 119,608 / 10,177 | 211 / 36 | 49 / 31 | 162 / 1 | 0 / 0 |
| lexington-downtown | 41 | 12 | 28% | streets 6.7 | streets 5.3 | 121,025 / 7,969 | 139 / 32 | 57 / 27 | 95 / 1 | 2 / 0 |
| hazard | 20 | 6 | 31% | streets 3.2 | streets 2.0 | 66,538 / 6,259 | 93 / 32 | 44 / 27 | 157 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 63.40 | 31.70 | make 56.09 + switch on 7.32 ms; 1602 objects; destroy 3.16 ms; 3,000 rays 2.75 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 16.29 | 8.14 | make 10.44 + switch on 5.85 ms; 10 objects; destroy 1.65 ms; 3,000 rays 2.03 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.96 | 5.98 | make 4.09 + switch on 7.87 ms; 2 objects; destroy 0.10 ms; 3,000 rays 1.94 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.13 | 1.06 | make 2.09 + switch on 0.03 ms (+ 8.24 ms baking on the worker); 2 objects; destroy 0.07 ms; 3,000 rays 1.96 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.67 | 216.66 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.98 | 199.54 | objects 0.85, allocate 0.02, pack 6.98, apply 0.12 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 0.96 | 24.02 | objects 0.85, allocate 0.02, pack 7.21 (worker), apply 0.10 ms |
| primitives | Box, turned, flat colour | 20,000 | 48.79 | 2.44 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 64.06 | 3.20 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 59.70 | 2.99 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 81.99 | 4.10 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 38.66 | 1.93 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 28.48 | 1.42 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.55 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.79 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 16.73 | 3.35 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 29.19 | 5.84 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 20.68 | 6.89 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 60.25 | 20.08 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.20 | 6.60 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.42 | 11.42 |  |
| objects | BoxCollider added to one object | 2,000 | 40.68 | 20.34 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 27.79 | 138.92 |  |
| objects | RoomLight.Spawn | 100 | 16.10 | 161.04 |  |
| objects | switching all of the above on | 1 | 27.33 | 27327.40 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 11.12 | 11120.20 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.60 | 9.82 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.00 | 200.37 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 380.79 | 380792.20 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 378.49 | 378485.30 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 20.85 | 20848.30 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,234 ms, 3,784,892 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (11) | 0.13 | 0.05 | 0.07 | 3.15 | 10.41 | 2 / 1 / 0 / 0 | buildings 1.7 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,039 / 3,825,167 | 2 | 181 / 935 (pool 40) |
| drive-east | 2400 (286) | 0.47 | 0.05 | 3.83 | 5.29 | 13.08 | 37 / 5 / 0 / 0 | colliders 8.6 | 43 (27 / 16) | 3 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,269 / 5,845,893 | 2 | 182 / 1,021 (pool 40) |
| teleport-louisville | 720 (118) | 0.98 | 0.05 | 6.03 | 9.97 | 37.63 | 52 / 18 / 3 / 3 | interior plans 9.9 | 25 (9 / 16) | 0 | 30 | 0.1 | 1.3 | 8 / 75 | 50 (16 Lite) / 2,805 / 5,086,895 | 4 | 198 / 992 (pool 40) |
| drive-louisville | 1800 (184) | 0.47 | 0.05 | 3.29 | 5.63 | 34.67 | 28 / 15 / 5 / 1 | interior plans 34.6 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 20 | 55 (12 Lite) / 3,193 / 5,907,244 | 6 | 247 / 1,027 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: snow tone (1x, worst 1.4 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: colliders (1x, worst 8.6 ms), kerbside cars (1x, worst 8.3 ms), snow tone (1x, worst 7.1 ms), yard trees (1x, worst 5.7 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (8x, worst 9.9 ms), nature plan (1x, worst 6.3 ms), streets (3x, worst 4.7 ms), snow tone (2x, worst 3.2 ms), utility poles (1x, worst 2.7 ms), colliders (1x, worst 2.6 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (5x, worst 34.6 ms), streets (2x, worst 29.6 ms), parking lot cars (1x, worst 8.6 ms), snow tone (1x, worst 8.5 ms), (swap) (1x, worst 7.8 ms), wall drifts (1x, worst 5.5 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 36 edit-mode artefacts

- `(edit mode) Error: Raid_sign_JEWELRY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_RADIO & TV37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_RADIO & TV37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.591 | 0% | 2,101 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 317 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.536 | 0% | 125 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.072 | 0% | 93 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.450 | 0% | 61 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 59 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.440 | 0% | 63 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.085 | 0% | 66 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.409 | 0% | 56 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.072 | 0% | 60 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.586 | 0% | 53 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 57 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 103 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 58 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.429 | 0% | 56 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.075 | 0% | 52 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.362 | 0% | 60 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 60 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.265 | 0% | 69 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.165 | 13% | 76 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.401 | 0% | 125 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.167 | 20% | 128 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.544 | 0% | 128 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.465 | 0% | 58 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 50 |

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

- `(edit mode) Error: Raid_sign_JEWELRY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOOKS080B153B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEWELRY050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_RADIO & TV37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_RADIO & TV37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 61 |  |
| square: KentuckyMap.Build | yes | 6,000 |  |
| square: count | yes | 937 |  |
| square: pick spots | yes | 107 |  |
| square: pictures | yes | 6,201 |  |
| square: discard | yes | 245 |  |
| square: passes (warm rebuild) | yes | 4,007 |  |
| chunks: stress test | yes | 1,081 |  |
| chunks: Louisville 3x3 | yes | 1,675 |  |
| chunks: determinism | yes | 150 |  |
| perf: Lite against Full | yes | 1,153 |  |
| perf: determinism across build orders | yes | 338 |  |
| perf: what things cost | yes | 1,662 |  |
| perf: walking and driving through the state | yes | 159,550 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
