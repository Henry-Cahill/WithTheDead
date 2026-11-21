# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 191.9 s, started 2026-10-24 13:36:58.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 8,819 ms |
| the same square again, warm, pass by pass | 5,568 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 30.1, 47.5, facing -166° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 804 containers, 949 lights, 335 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 28,923 | 14,171 | 14,171 | 13,226 | 7,471,468 | 4,571,044 | 141 | 33,417 | 965 (0) | 8,368 | 9,638 | 804 |
| RaidOverhead | 588 | 470 | 470 | 470 | 471,502 | 277,773 | 67 | 1,697 | 0 (0) | 0 | 470 | 0 |
| RaidScenery | 118 | 117 | 117 | 117 | 80,444 | 104,466 | 48 | 361 | 0 (0) | 0 | 72 | 0 |
| **all** | 29,629 | 14,758 | 14,758 | 13,813 | 8,023,414 | 4,953,283 | 177 | 35,475 | 965 (0) | 8,368 | 10,180 | 804 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,134 | 13,733 | 13,733 | 12,807 | 6,229,307 | 3,927,834 | 82 | 31,003 | 927 (0) | 6,083 | 9,365 | 698 |
| World/Building shells | 131 | 130 | 130 | 130 | 802,262 | 403,344 | 51 | 1,216 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 332,567 | 165,190 | 53 | 788 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 305,847 | 179,536 | 19 | 628 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 132,411 | 57,818 | 14 | 547 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 132 | 132 | 132 | 132 | 46,151 | 27,180 | 50 | 432 | 0 (0) | 0 | 132 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 36,436 | 21,756 | 41 | 257 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,240 | 430 | 5 | 73 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 29 | 29 | 29 | 29 | 2,478 | 4,392 | 1 | 29 | 0 (0) | 0 | 29 | 0 |
| RaidOverhead/Wires | 26 | 26 | 26 | 26 | 4,734 | 8,238 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| World/StreetLamp | 44 | 22 | 22 | 3 | 1,617 | 2,500 | 3 | 22 | 22 (0) | 22 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,286 | 77,886 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 19,146 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 111 | 2% | 1 | 111 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 2 | 0% | 1 | 2 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 4 | 0% | 1 | 4 |
| lots | 0 | 0% | 85 | 0 |
| cemeteries | 6 | 0% | 500 | 0 |
| grounds | 0 | 0% | 2 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 54 | 1% | 1 | 54 |
| road network | 5 | 0% | 1 | 5 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 26 | 0% | 189 | 1 |
| sidewalks | 32 | 1% | 67 | 5 |
| berms and driveways | 16 | 0% | 63 | 1 |
| road markings | 1 | 0% | 10 | 1 |
| paths | 2 | 0% | 72 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 126 | 2% | 51 | 8 |
| interior plans | 209 | 4% | 798 | 7 |
| interior shell | 19 | 0% | 116 | 1 |
| interior walls | 219 | 4% | 116 | 20 |
| interior floors | 19 | 0% | 116 | 1 |
| interior doors | 578 | 10% | 233 | 21 |
| interior switches | 97 | 2% | 116 | 3 |
| interior rooms | 2,996 | 54% | 697 | 26 |
| interior wall meshes | 123 | 2% | 116 | 4 |
| interior finish | 271 | 5% | 232 | 6 |
| interior merge | 368 | 7% | 467 | 10 |
| cutaway | 12 | 0% | 6 | 2 |
| utility lines | 19 | 0% | 26 | 7 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 90 | 3 |
| service drops | 1 | 0% | 251 | 0 |
| street lights | 3 | 0% | 48 | 0 |
| signs | 2 | 0% | 115 | 0 |
| street furniture | 3 | 0% | 255 | 2 |
| landmarks | 0 | 0% | 2 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 9 | 0% | 56 | 7 |
| nature plan | 2 | 0% | 1 | 2 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 12 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 9 | 0% | 9356 | 0 |
| driveway cars | 3 | 0% | 30 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 45 | 0 |
| kerbside cars | 33 | 1% | 232 | 8 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 15 | 0% | 345 | 0 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 14 | 0% | 1 | 14 |
| (emit the overhead meshes) | 11 | 0% | 1 | 11 |
| (emit the outdoor meshes) | 108 | 2% | 1 | 108 |
| **total** | **5,568** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **96** | data 42 | 1 | 137 | 65 | 138,586 | 72,762 | 1 | 121 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **50** | interior plans 9 | 1 | 145 | 64 | 133,076 | 75,702 | 4 | 218 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 153 + 12 | **74** | ground 12 | 1 | 210 | 53 | 127,126 | 69,028 | 0 | 162 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 497 + 33 | **63** | paths 8 | 1 | 175 | 58 | 131,784 | 70,280 | 1 | 109 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 6 + 3 | **22** | streets 3 | 0 | 63 | 42 | 53,923 | 39,907 | 0 | 145 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 41.9 | 1 | 41.9 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.6 | 1 | 0.6 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.7 | 1 | 1.7 |
| junctions | 1.4 | 1 | 1.4 |
| streets | 5.8 | 3 | 2.2 |
| sidewalks | 5.0 | 2 | 2.8 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 1.4 | 1 | 1.4 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.7 | 4 | 2.2 |
| interior plans | 13.2 | 6 | 2.6 |
| cutaway | 0.8 | 1 | 0.8 |
| utility lines | 0.6 | 1 | 0.6 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.4 | 1 | 0.4 |
| signs | 1.6 | 1 | 1.6 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.1 | 1 | 0.1 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.8 | 1 | 0.8 |
| nature plan | 0.3 | 1 | 0.3 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.1 | 1 | 0.1 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.6 | 1 | 0.6 |
| driveway cars | 0.1 | 1 | 0.1 |
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
| colliders | 1.9 | 1 | 1.9 |
| objects | 2.4 | 1 | 2.4 |
| meshes | 1.0 | 1 | 1.0 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.3 | 1 | 0.3 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.4 | 1 | 1.4 |
| sidewalks | 0.8 | 1 | 0.8 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 6.5 | 3 | 3.4 |
| interior plans | 24.1 | 8 | 9.3 |
| cutaway | 1.5 | 1 | 1.5 |
| utility lines | 0.0 | 1 | 0.0 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.8 | 1 | 0.8 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 2.3 | 2 | 2.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.7 | 1 | 0.7 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.4 | 1 | 1.4 |
| driveway cars | 0.4 | 1 | 0.4 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.4 | 1 | 0.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.5 | 1 | 1.5 |
| objects | 2.4 | 1 | 2.4 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 11.8 | 1 | 11.8 |
| water | 3.7 | 2 | 3.7 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 1.0 | 1 | 1.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.2 | 1 | 1.2 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.3 | 1 | 1.3 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.7 | 1 | 0.7 |
| paths | 4.6 | 3 | 2.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.6 | 5 | 4.2 |
| interior plans | 20.7 | 8 | 4.5 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.7 | 1 | 1.7 |
| wires | 0.2 | 1 | 0.2 |
| nature plan | 0.4 | 1 | 0.4 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.7 | 1 | 0.7 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 1 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 1.7 | 1 | 1.7 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.4 | 2 | 2.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.9 | 1 | 1.9 |
| objects | 1.0 | 1 | 1.0 |
| meshes | 0.5 | 1 | 0.5 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.8 | 1 | 0.8 |
| ground | 1.1 | 1 | 1.1 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.8 | 1 | 0.8 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 1.3 | 1 | 1.3 |
| junctions | 1.6 | 1 | 1.6 |
| streets | 6.4 | 2 | 5.9 |
| sidewalks | 3.1 | 2 | 2.5 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.5 | 1 | 0.5 |
| paths | 8.0 | 2 | 7.7 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 8.2 | 4 | 3.1 |
| interior plans | 11.7 | 5 | 2.9 |
| cutaway | 0.9 | 1 | 0.9 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.2 | 1 | 0.2 |
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
| woods | 0.7 | 1 | 0.7 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 3.0 | 2 | 2.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.0 | 2 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.5 | 1 | 1.5 |
| colliders | 1.1 | 1 | 1.1 |
| objects | 2.0 | 1 | 2.0 |
| meshes | 0.7 | 1 | 0.7 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.2 | 1 | 0.2 |
| lots | 1.0 | 1 | 1.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.6 | 1 | 0.6 |
| streets | 3.5 | 2 | 3.4 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.8 | 1 | 0.8 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.4 | 1 | 1.4 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.8 | 1 | 0.8 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 1.1 | 1 | 1.1 |
| woods | 5.5 | 3 | 2.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.1 | 1 | 0.1 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.6 | 1 | 0.6 |
| objects | 1.1 | 1 | 1.1 |
| meshes | 0.4 | 1 | 0.4 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 48 | 11 |
| 186_141 | yes | 89 | 3 |
| 185_141 | yes | 9 | 2 |
| 185_142 | yes | 12 | 3 |
| 183_148 | yes | 153 | 12 |
| 196_145 | yes | 497 | 33 |
| 210_134 | yes | 6 | 3 |
| 182_148 | yes | 156 | 23 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 549 ms (slowest 124 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **54** | interior plans 5 | 1 | 210 | 53 | 127,126 | 69,028 | 0 | 162 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **60** | interior plans 8 | 1 | 201 | 75 | 146,979 | 80,890 | 0 | 202 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 156 + 23 | **68** | interior plans 10 | 1 | 249 | 62 | 130,826 | 72,622 | 3 | 197 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **124** | interior plans 37 | 1 | 159 | 57 | 120,840 | 64,239 | 0 | 152 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **55** | fences and walls 7 | 1 | 180 | 63 | 94,285 | 55,022 | 1 | 132 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **44** | interior plans 8 | 0 | 154 | 54 | 128,299 | 73,505 | 1 | 124 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **47** | buildings 7 | 1 | 209 | 53 | 127,992 | 69,499 | 0 | 210 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **38** | interior plans 5 | 0 | 147 | 56 | 96,693 | 58,262 | 4 | 90 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **59** | interior plans 6 | 1 | 189 | 45 | 209,784 | 116,691 | 0 | 159 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,700 | 518 | 518 | 512 | 1,182,824 | 659,758 | 95 | 2,111 | 9 (0) | 1,428 | 356 | 86 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (138,586 vertices, 72,762 triangles, 137 objects, 65 meshes; hash 37f33d4db9d013fa vs 37f33d4db9d013fa; 122 colliders, lights and loot containers, hash 4a5b6e3702bd270e vs 4a5b6e3702bd270e). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 96 | 119 / 63 / 1.7 | 0 / 0.0 | 61 / 135,720 / 0.5 | 1.6 | 92.6 | 42 / 3.6 |
| rural-5km | Full | 50 | 217 / 69 / 1.5 | 0 / 0.0 | 63 / 130,477 / 0.3 | 3.5 | 45.2 | 43 / 0.3 |
| louisville-downtown | Full | 74 | 161 / 119 / 1.9 | 0 / 0.0 | 51 / 124,515 / 0.5 | 1.3 | 71.3 | 36 / 0.3 |
| lexington-downtown | Full | 63 | 107 / 72 / 1.1 | 0 / 0.0 | 53 / 128,980 / 0.6 | 2.3 | 59.9 | 39 / 0.3 |
| hazard | Full | 22 | 144 / 15 / 0.6 | 0 / 0.0 | 37 / 50,725 / 0.2 | 1.1 | 20.7 | 30 / 0.4 |
| louisville 0,0 | Full | 54 | 161 / 119 / 1.8 | 0 / 0.0 | 51 / 124,515 / 0.3 | 1.8 | 50.0 | 36 / 0.3 |
| louisville 1,0 | Full | 60 | 201 / 108 / 2.0 | 0 / 0.0 | 73 / 144,310 / 0.5 | 2.0 | 56.1 | 46 / 0.3 |
| louisville -1,0 | Full | 68 | 193 / 140 / 2.6 | 0 / 0.0 | 52 / 127,403 / 0.4 | 1.8 | 63.9 | 40 / 0.8 |
| louisville 0,1 | Full | 124 | 151 / 83 / 1.6 | 0 / 0.0 | 54 / 118,339 / 0.4 | 1.6 | 120.3 | 36 / 0.3 |
| louisville 0,-1 | Full | 55 | 131 / 77 / 1.1 | 0 / 0.0 | 58 / 90,063 / 0.5 | 1.6 | 52.4 | 37 / 0.5 |
| louisville 1,1 | Full | 44 | 122 / 76 / 1.2 | 0 / 0.0 | 50 / 125,165 / 0.4 | 1.3 | 41.7 | 36 / 0.3 |
| louisville -1,1 | Full | 47 | 209 / 116 / 1.9 | 0 / 0.0 | 50 / 124,716 / 0.2 | 1.4 | 44.1 | 37 / 0.3 |
| louisville 1,-1 | Full | 38 | 85 / 48 / 0.6 | 0 / 0.0 | 47 / 92,373 / 0.3 | 1.2 | 36.4 | 36 / 0.3 |
| louisville -1,-1 | Full | 59 | 158 / 110 / 1.5 | 0 / 0.0 | 41 / 206,647 / 0.3 | 1.3 | 56.5 | 30 / 0.3 |

The Louisville block's NavMesh: 334 sources collected in 3 ms; baked at once (as one 384 m square) in 211 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 51 | 9 | 17% | sidewalks 3.4 | sidewalks 2.3 | 138,586 / 5,901 | 137 / 36 | 65 / 31 | 121 / 1 | 1 / 0 |
| rural-5km | 33 | 5 | 15% | interior plans 2.7 | objects 1.3 | 132,918 / 4,492 | 142 / 38 | 64 / 33 | 213 / 1 | 4 / 0 |
| louisville-downtown | 53 | 9 | 17% | buildings 4.7 | buildings 2.8 | 119,574 / 6,869 | 212 / 33 | 53 / 28 | 160 / 1 | 0 / 0 |
| lexington-downtown | 50 | 7 | 14% | streets 5.9 | sidewalks 1.2 | 126,971 / 4,206 | 177 / 33 | 59 / 28 | 111 / 1 | 2 / 0 |
| hazard | 20 | 5 | 23% | streets 3.1 | objects 1.0 | 61,870 / 4,435 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 77.03 | 38.52 | make 64.83 + switch on 12.20 ms; 1602 objects; destroy 6.25 ms; 3,000 rays 3.54 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 19.36 | 9.68 | make 13.08 + switch on 6.28 ms; 10 objects; destroy 2.82 ms; 3,000 rays 2.68 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 14.43 | 7.22 | make 3.97 + switch on 10.46 ms; 2 objects; destroy 0.13 ms; 3,000 rays 2.59 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.43 | 1.22 | make 2.40 + switch on 0.03 ms (+ 8.53 ms baking on the worker); 2 objects; destroy 0.10 ms; 3,000 rays 2.64 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 9.73 | 243.22 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.88 | 222.10 | objects 0.97, allocate 0.02, pack 7.74, apply 0.15 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.41 | 35.31 | objects 1.24, allocate 0.02, pack 7.46 (worker), apply 0.15 ms |
| primitives | Box, turned, flat colour | 20,000 | 29.93 | 1.50 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 84.30 | 4.22 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 52.62 | 2.63 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 88.71 | 4.44 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 53.73 | 2.69 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 40.76 | 2.04 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 8.26 | 0.41 | 80,000 vertices |
| primitives | Tri | 20,000 | 5.97 | 0.30 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 23.52 | 4.70 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 38.18 | 7.64 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 23.74 | 7.91 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 75.47 | 25.16 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 18.30 | 9.15 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 15.37 | 15.37 |  |
| objects | BoxCollider added to one object | 2,000 | 49.69 | 24.85 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 33.69 | 168.45 |  |
| objects | RoomLight.Spawn | 100 | 14.69 | 146.88 |  |
| objects | switching all of the above on | 1 | 34.70 | 34701.50 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 17.52 | 17519.10 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.67 | 10.99 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.37 | 237.14 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 449.11 | 449106.30 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 448.10 | 448096.00 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 26.68 | 26676.90 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,371 ms, 3,818,333 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (10) | 0.13 | 0.04 | 0.08 | 3.41 | 12.76 | 1 / 1 / 0 / 0 | landmarks 2.6 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,069 / 3,849,793 | 2 | 200 / 934 (pool 40) |
| drive-east | 2400 (206) | 0.35 | 0.05 | 3.36 | 4.88 | 9.31 | 19 / 1 / 0 / 0 | objects 6.0 | 40 (24 / 16) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,157 / 5,490,690 | 2 | 199 / 1,006 (pool 40) |
| teleport-louisville | 720 (106) | 0.89 | 0.05 | 6.27 | 9.97 | 36.60 | 51 / 15 / 3 / 2 | interior plans 9.9 | 25 (9 / 16) | 0 | 30 | 0.2 | 1.2 | 9 / 73 | 50 (16 Lite) / 2,828 / 5,094,963 | 4 | 215 / 990 (pool 40) |
| drive-louisville | 1800 (159) | 0.40 | 0.05 | 3.11 | 5.98 | 27.03 | 30 / 8 / 4 / 0 | interior plans 27.0 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 22 | 55 (12 Lite) / 3,238 / 5,941,429 | 6 | 260 / 1,025 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: landmarks (1x, worst 2.6 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: objects (1x, worst 6.0 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (10x, worst 9.9 ms), sidewalks (2x, worst 2.8 ms), road network (1x, worst 2.2 ms), wall drifts (1x, worst 2.0 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (6x, worst 27.0 ms), utility lines (1x, worst 7.4 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 8 edit-mode artefacts

- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEFFERSON COUNTY SHERIFF0A0D16373936: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEFFERSON COUNTY SHERIFF0A0D16373936: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.597 | 0% | 2,222 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 403 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on North 3rd Street, at the game's starting zoom | 0.302 | 0% | 139 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on North 3rd Street, at the game's starting zoom | 0.108 | 0% | 100 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.420 | 0% | 60 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 71 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.489 | 0% | 69 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 69 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.353 | 0% | 63 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.074 | 0% | 52 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.583 | 0% | 68 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 88 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 70 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 94 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.396 | 0% | 59 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.073 | 0% | 63 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.361 | 0% | 68 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 59 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on North 3rd Street (downtown), looking along the street | 0.297 | 0% | 92 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on North 3rd Street (downtown), looking along the street | 0.194 | 21% | 96 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.433 | 0% | 163 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.166 | 20% | 134 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.543 | 0% | 222 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.463 | 0% | 60 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.081 | 0% | 59 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | 30.1, 47.5 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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

### Everything logged during the run: 0 errors, 0 warnings, 8 edit-mode artefacts

- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_STARBUCKS353432060708: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_POLICE373736060B1C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEFFERSON COUNTY SHERIFF0A0D16373936: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_JEFFERSON COUNTY SHERIFF0A0D16373936: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 69 |  |
| square: KentuckyMap.Build | yes | 8,821 |  |
| square: count | yes | 1,031 |  |
| square: pick spots | yes | 168 |  |
| square: pictures | yes | 7,017 |  |
| square: discard | yes | 300 |  |
| square: passes (warm rebuild) | yes | 5,868 |  |
| chunks: stress test | yes | 1,283 |  |
| chunks: Louisville 3x3 | yes | 1,953 |  |
| chunks: determinism | yes | 171 |  |
| perf: Lite against Full | yes | 1,291 |  |
| perf: determinism across build orders | yes | 359 |  |
| perf: what things cost | yes | 1,981 |  |
| perf: walking and driving through the state | yes | 161,545 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
