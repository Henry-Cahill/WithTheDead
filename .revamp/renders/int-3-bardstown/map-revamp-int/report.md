# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 211.4 s, started 2026-10-24 10:14:07.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 8,855 ms |
| the same square again, warm, pass by pass | 5,717 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 10.8, 142.2, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 903 containers, 954 lights, 337 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,291 | 16,231 | 16,231 | 15,281 | 7,588,374 | 4,632,784 | 134 | 37,224 | 970 (0) | 8,377 | 11,708 | 903 |
| RaidOverhead | 585 | 467 | 467 | 467 | 462,844 | 271,817 | 134 | 1,896 | 0 (0) | 0 | 467 | 0 |
| RaidScenery | 115 | 114 | 114 | 114 | 81,047 | 105,094 | 43 | 341 | 0 (0) | 0 | 69 | 0 |
| **all** | 29,991 | 16,812 | 16,812 | 15,862 | 8,132,265 | 5,009,695 | 239 | 39,461 | 970 (0) | 8,377 | 12,244 | 903 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,587 | 15,793 | 15,793 | 14,862 | 6,321,697 | 3,984,349 | 82 | 34,799 | 932 (0) | 6,120 | 11,435 | 815 |
| World/Building shells | 131 | 130 | 130 | 130 | 815,118 | 409,876 | 48 | 1,221 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 327,105 | 161,997 | 119 | 982 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 316,512 | 179,306 | 16 | 608 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 133,417 | 56,761 | 13 | 573 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 130 | 130 | 130 | 130 | 43,477 | 25,299 | 46 | 438 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 37,074 | 22,056 | 36 | 237 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,240 | 430 | 5 | 76 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 26 | 26 | 26 | 26 | 2,088 | 3,696 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| RaidOverhead/Wires | 25 | 25 | 25 | 25 | 4,212 | 7,356 | 1 | 25 | 0 (0) | 0 | 25 | 0 |
| World/StreetLamp | 44 | 22 | 22 | 3 | 1,606 | 2,480 | 3 | 22 | 22 (0) | 22 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,641 | 78,910 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 21,114 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 94 | 2% | 1 | 94 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| lots | 4 | 0% | 1 | 4 |
| cemeteries | 5 | 0% | 4 | 2 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 37 | 1% | 1 | 37 |
| road network | 9 | 0% | 1 | 9 |
| junctions | 9 | 0% | 1 | 9 |
| streets | 31 | 1% | 67 | 19 |
| sidewalks | 27 | 0% | 67 | 3 |
| berms and driveways | 16 | 0% | 62 | 1 |
| road markings | 1 | 0% | 17 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 139 | 2% | 49 | 11 |
| interior plans | 207 | 4% | 117 | 7 |
| interior shell | 21 | 0% | 116 | 3 |
| interior walls | 208 | 4% | 116 | 13 |
| interior floors | 20 | 0% | 116 | 1 |
| interior doors | 659 | 12% | 232 | 22 |
| interior switches | 118 | 2% | 116 | 21 |
| interior rooms | 3,234 | 57% | 697 | 51 |
| interior wall meshes | 127 | 2% | 116 | 3 |
| interior finish | 296 | 5% | 116 | 9 |
| cutaway | 129 | 2% | 36 | 35 |
| utility lines | 10 | 0% | 1 | 10 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 8 | 0% | 1 | 8 |
| street lights | 7 | 0% | 1 | 7 |
| signs | 5 | 0% | 1 | 5 |
| street furniture | 35 | 1% | 1 | 35 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 13 | 0% | 1 | 13 |
| nature plan | 0 | 0% | 1 | 0 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 16 | 0% | 163 | 0 |
| grounds trees | 2 | 0% | 5 | 1 |
| street trees | 4 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 7 | 0% | 9370 | 0 |
| driveway cars | 4 | 0% | 30 | 1 |
| buses and cruisers | 1 | 0% | 1 | 1 |
| parking lot cars | 6 | 0% | 40 | 1 |
| kerbside cars | 19 | 0% | 186 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 40 | 1% | 117 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 16 | 0% | 1 | 16 |
| (emit the overhead meshes) | 15 | 0% | 1 | 15 |
| (emit the outdoor meshes) | 100 | 2% | 1 | 100 |
| **total** | **5,717** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **139** | data 59 | 1 | 131 | 61 | 136,811 | 70,454 | 0 | 109 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **44** | buildings 9 | 1 | 144 | 64 | 135,533 | 76,747 | 4 | 200 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 214 + 12 | **99** | cutaway 10 | 1 | 191 | 50 | 136,339 | 71,798 | 0 | 158 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 524 + 49 | **99** | buildings 43 | 1 | 133 | 57 | 128,811 | 67,613 | 1 | 98 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 7 + 2 | **23** | streets 4 | 0 | 69 | 43 | 57,402 | 42,173 | 0 | 150 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 58.5 | 1 | 58.5 |
| ground | 1.8 | 1 | 1.8 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 3.6 | 1 | 3.6 |
| road network | 3.1 | 1 | 3.1 |
| junctions | 1.8 | 1 | 1.8 |
| streets | 20.3 | 2 | 10.9 |
| sidewalks | 6.1 | 3 | 2.6 |
| berms and driveways | 2.0 | 1 | 2.0 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 1.6 | 1 | 1.6 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 7.5 | 4 | 2.4 |
| interior plans | 11.6 | 5 | 2.8 |
| cutaway | 4.8 | 3 | 2.3 |
| utility lines | 0.8 | 1 | 0.8 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.6 | 1 | 1.6 |
| street furniture | 0.7 | 1 | 0.7 |
| landmarks | 0.5 | 1 | 0.5 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.8 | 1 | 0.8 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.2 | 1 | 0.2 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 4 | 0.1 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.6 | 1 | 0.6 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.2 | 1 | 0.2 |
| colliders | 1.1 | 1 | 1.1 |
| objects | 3.1 | 1 | 3.1 |
| meshes | 1.7 | 1 | 1.7 |

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
| snow tone | 2.5 | 1 | 2.5 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.9 | 1 | 1.9 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.9 | 1 | 0.9 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 11.4 | 3 | 8.5 |
| interior plans | 9.8 | 5 | 2.7 |
| cutaway | 4.3 | 2 | 2.5 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.0 | 1 | 1.0 |
| street furniture | 0.5 | 1 | 0.5 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 2.3 | 2 | 2.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.6 | 1 | 0.6 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.3 | 4 | 0.4 |
| driveway cars | 0.4 | 1 | 0.4 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.6 | 1 | 1.6 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.6 | 1 | 1.6 |
| objects | 1.0 | 1 | 1.0 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.8 | 1 | 0.8 |
| ground | 3.1 | 1 | 3.1 |
| water | 3.3 | 1 | 3.3 |
| fields | 0.1 | 1 | 0.1 |
| lots | 1.0 | 1 | 1.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.7 | 1 | 1.7 |
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.7 | 1 | 0.7 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 1.1 | 1 | 1.1 |
| sidewalks | 1.5 | 1 | 1.5 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 5.0 | 1 | 5.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 22.7 | 7 | 7.9 |
| interior plans | 25.3 | 7 | 7.1 |
| cutaway | 13.7 | 3 | 9.8 |
| utility lines | 0.7 | 1 | 0.7 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.3 | 1 | 0.3 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 2.2 | 1 | 2.2 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.7 | 1 | 0.7 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 4 | 0.3 |
| driveway cars | 0.2 | 1 | 0.2 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.5 | 1 | 0.5 |
| kerbside cars | 1.5 | 1 | 1.5 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 5.3 | 2 | 4.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.8 | 1 | 1.8 |
| objects | 1.0 | 1 | 1.0 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.8 | 1 | 0.8 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.6 | 1 | 0.6 |
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.6 | 1 | 0.6 |
| streets | 6.8 | 1 | 6.8 |
| sidewalks | 2.9 | 2 | 2.5 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.9 | 1 | 0.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 51.9 | 5 | 43.2 |
| interior plans | 12.9 | 5 | 3.9 |
| cutaway | 3.9 | 2 | 2.7 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.1 | 1 | 1.1 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 1.4 | 1 | 1.4 |
| street furniture | 1.1 | 1 | 1.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 1.0 | 1 | 1.0 |
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
| kerbside cars | 1.1 | 1 | 1.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.4 | 2 | 2.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.1 | 1 | 1.1 |
| colliders | 1.1 | 1 | 1.1 |
| objects | 1.1 | 1 | 1.1 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 0.4 | 1 | 0.4 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 1.1 | 1 | 1.1 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 2.5 | 1 | 2.5 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 3.8 | 1 | 3.8 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.7 | 1 | 0.7 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.6 | 1 | 0.6 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.5 | 1 | 0.5 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.5 | 1 | 1.5 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.9 | 1 | 0.9 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.9 | 1 | 0.9 |
| woods | 5.2 | 5 | 2.1 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.3 | 1 | 0.3 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 0.6 | 1 | 0.6 |
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.3 | 1 | 0.3 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 54 | 12 |
| 186_141 | yes | 13 | 16 |
| 185_141 | yes | 9 | 2 |
| 185_142 | yes | 11 | 3 |
| 183_148 | yes | 214 | 12 |
| 196_145 | yes | 524 | 49 |
| 210_134 | yes | 7 | 2 |
| 182_148 | yes | 191 | 37 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 731 ms (slowest 142 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **62** | interior plans 7 | 1 | 191 | 50 | 136,339 | 71,798 | 0 | 158 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **72** | interior plans 8 | 2 | 192 | 75 | 149,148 | 80,896 | 0 | 201 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 191 + 37 | **106** | utility poles 18 | 2 | 234 | 55 | 108,169 | 59,464 | 1 | 189 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **88** | interior plans 34 | 1 | 156 | 59 | 119,818 | 62,173 | 1 | 128 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **90** | buildings 20 | 1 | 164 | 60 | 86,030 | 48,934 | 1 | 120 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **46** | interior plans 10 | 1 | 149 | 53 | 112,909 | 64,740 | 0 | 119 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **142** | buildings 61 | 1 | 179 | 53 | 116,902 | 62,864 | 0 | 195 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **48** | interior plans 6 | 1 | 147 | 54 | 100,365 | 59,101 | 3 | 84 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **76** | interior plans 9 | 1 | 166 | 41 | 188,216 | 103,115 | 0 | 147 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,580 | 500 | 500 | 497 | 1,117,896 | 613,085 | 132 | 2,138 | 6 (0) | 1,341 | 341 | 68 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (136,811 vertices, 70,454 triangles, 131 objects, 61 meshes; hash dbcd524747fe222a vs dbcd524747fe222a; 111 colliders, lights and loot containers, hash bac77ee04f661af5 vs bac77ee04f661af5). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 139 | 108 / 55 / 0.9 | 0 / 0.0 | 59 / 134,133 / 4.6 | 5.8 | 132.3 | 41 / 5.0 |
| rural-5km | Full | 44 | 199 / 67 / 1.6 | 0 / 0.0 | 63 / 132,932 / 3.3 | 4.9 | 37.7 | 43 / 0.6 |
| louisville-downtown | Full | 99 | 157 / 112 / 1.8 | 0 / 0.0 | 49 / 133,741 / 12.8 | 14.2 | 83.4 | 37 / 1.0 |
| lexington-downtown | Full | 99 | 96 / 60 / 1.1 | 0 / 0.0 | 53 / 126,078 / 3.3 | 4.6 | 93.4 | 39 / 0.3 |
| hazard | Full | 23 | 149 / 19 / 0.6 | 0 / 0.0 | 38 / 53,978 / 0.2 | 0.9 | 22.0 | 30 / 0.4 |
| louisville 0,0 | Full | 62 | 157 / 112 / 2.5 | 0 / 0.0 | 49 / 133,741 / 4.3 | 5.9 | 53.6 | 37 / 0.4 |
| louisville 1,0 | Full | 72 | 200 / 109 / 2.0 | 0 / 0.0 | 73 / 146,476 / 3.5 | 5.8 | 64.7 | 46 / 0.4 |
| louisville -1,0 | Full | 106 | 187 / 133 / 2.3 | 0 / 0.0 | 49 / 105,232 / 2.7 | 3.6 | 100.3 | 38 / 0.6 |
| louisville 0,1 | Full | 88 | 126 / 71 / 1.5 | 0 / 0.0 | 54 / 117,346 / 2.4 | 4.3 | 82.1 | 37 / 0.3 |
| louisville 0,-1 | Full | 90 | 119 / 68 / 1.5 | 0 / 0.0 | 55 / 82,734 / 1.8 | 3.5 | 84.8 | 37 / 0.4 |
| louisville 1,1 | Full | 46 | 118 / 72 / 1.1 | 0 / 0.0 | 50 / 109,850 / 2.3 | 3.6 | 41.8 | 35 / 0.4 |
| louisville -1,1 | Full | 142 | 194 / 105 / 2.9 | 0 / 0.0 | 50 / 113,788 / 2.8 | 22.9 | 116.5 | 37 / 0.5 |
| louisville 1,-1 | Full | 48 | 80 / 45 / 0.9 | 0 / 0.0 | 46 / 96,071 / 1.3 | 2.7 | 44.8 | 35 / 0.5 |
| louisville -1,-1 | Full | 76 | 146 / 100 / 1.9 | 0 / 0.0 | 39 / 185,580 / 5.1 | 9.9 | 64.0 | 30 / 0.3 |

The Louisville block's NavMesh: 332 sources collected in 4 ms; baked at once (as one 384 m square) in 191 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 60 | 17 | 28% | streets 5.7 | streets 7.0 | 136,811 / 11,212 | 131 / 37 | 61 / 32 | 109 / 1 | 0 / 0 |
| rural-5km | 47 | 9 | 20% | interior plans 3.5 | snow tone 2.9 | 135,374 / 6,899 | 141 / 38 | 64 / 33 | 195 / 1 | 4 / 0 |
| louisville-downtown | 59 | 13 | 21% | interior plans 7.2 | buildings 2.8 | 130,176 / 10,503 | 203 / 36 | 50 / 31 | 161 / 1 | 0 / 0 |
| lexington-downtown | 56 | 14 | 25% | streets 9.1 | streets 6.2 | 120,871 / 7,969 | 131 / 32 | 57 / 27 | 92 / 1 | 2 / 0 |
| hazard | 23 | 9 | 38% | streets 3.8 | snow tone 2.6 | 64,430 / 6,261 | 88 / 32 | 44 / 27 | 152 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 100.50 | 50.25 | make 88.52 + switch on 11.98 ms; 1602 objects; destroy 9.52 ms; 3,000 rays 4.50 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 23.12 | 11.56 | make 15.72 + switch on 7.40 ms; 10 objects; destroy 4.00 ms; 3,000 rays 2.79 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 15.62 | 7.81 | make 5.28 + switch on 10.33 ms; 2 objects; destroy 0.29 ms; 3,000 rays 3.63 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.74 | 1.37 | make 2.70 + switch on 0.04 ms (+ 10.33 ms baking on the worker); 2 objects; destroy 0.13 ms; 3,000 rays 3.02 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 14.57 | 364.22 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 11.78 | 294.49 | objects 0.89, allocate 0.02, pack 10.67, apply 0.21 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.63 | 40.70 | objects 1.46, allocate 0.02, pack 8.82 (worker), apply 0.16 ms |
| primitives | Box, turned, flat colour | 20,000 | 64.01 | 3.20 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 68.07 | 3.40 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 57.77 | 2.89 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 134.26 | 6.71 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 34.75 | 1.74 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 28.62 | 1.43 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 7.49 | 0.37 | 80,000 vertices |
| primitives | Tri | 20,000 | 7.30 | 0.37 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 110.36 | 22.07 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 36.08 | 7.22 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 59.02 | 19.67 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 55.39 | 18.46 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 23.02 | 11.51 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 17.61 | 17.61 |  |
| objects | BoxCollider added to one object | 2,000 | 56.78 | 28.39 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 40.35 | 201.75 |  |
| objects | RoomLight.Spawn | 100 | 17.12 | 171.19 |  |
| objects | switching all of the above on | 1 | 40.22 | 40221.40 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 20.81 | 20813.50 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.74 | 12.11 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 3.13 | 312.96 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 511.07 | 511072.70 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 467.79 | 467793.60 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 31.76 | 31760.60 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,785 ms, 3,934,525 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (13) | 0.17 | 0.04 | 0.09 | 4.18 | 18.30 | 3 / 1 / 1 / 0 | snow tone 3.0 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,040 / 3,975,391 | 2 | 183 / 942 (pool 40) |
| drive-east | 2400 (338) | 0.54 | 0.05 | 3.68 | 5.76 | 26.11 | 46 / 11 / 2 / 0 | buildings 23.1 | 41 (27 / 14) | 3 | 15 | 0.0 | 0.0 | 0 / 0 | 53 (10 Lite) / 3,248 / 6,018,920 | 2 | 185 / 1,031 (pool 40) |
| teleport-louisville | 720 (169) | 1.02 | 0.05 | 4.74 | 9.17 | 39.32 | 29 / 9 / 3 / 3 | snow tone 10.3 | 25 (9 / 16) | 0 | 29 | 0.1 | 2.1 | 8 / 127 | 50 (16 Lite) / 2,807 / 5,187,261 | 4 | 201 / 998 (pool 40) |
| drive-louisville | 1800 (207) | 0.48 | 0.05 | 3.36 | 5.51 | 39.45 | 30 / 10 / 3 / 1 | interior plans 39.4 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 38 | 55 (12 Lite) / 3,195 / 5,997,895 | 6 | 247 / 1,033 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: snow tone (1x, worst 2.8 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: buildings (4x, worst 23.1 ms), colliders (1x, worst 16.4 ms), meshes (1x, worst 11.3 ms), power lines (1x, worst 11.3 ms), interior plans (2x, worst 9.3 ms), cutaway (1x, worst 8.0 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: snow tone (1x, worst 10.3 ms), interior plans (6x, worst 9.9 ms), cutaway (1x, worst 3.6 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (4x, worst 39.4 ms), objects (1x, worst 22.5 ms), snow tone (1x, worst 8.8 ms), junctions (1x, worst 8.8 ms), kerbside cars (1x, worst 6.4 ms), streets (1x, worst 5.6 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 36 edit-mode artefacts

- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
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
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.584 | 0% | 9,529 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 409 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.522 | 0% | 3,594 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.073 | 0% | 5,260 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.451 | 0% | 113 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 83 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.425 | 0% | 72 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.087 | 0% | 86 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.409 | 0% | 82 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.072 | 0% | 83 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.587 | 0% | 73 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 70 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.348 | 0% | 80 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.072 | 0% | 89 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.430 | 0% | 4,355 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.075 | 0% | 5,873 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 79 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 73 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.265 | 0% | 99 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.165 | 13% | 95 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.403 | 0% | 133 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.172 | 21% | 147 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.540 | 0% | 160 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.464 | 0% | 77 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 154 |

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
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PNC TOWER07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 60 |  |
| square: KentuckyMap.Build | yes | 8,857 |  |
| square: count | yes | 981 |  |
| square: pick spots | yes | 97 |  |
| square: pictures | yes | 33,281 |  |
| square: discard | yes | 303 |  |
| square: passes (warm rebuild) | yes | 6,060 |  |
| chunks: stress test | yes | 1,418 |  |
| chunks: Louisville 3x3 | yes | 2,291 |  |
| chunks: determinism | yes | 218 |  |
| perf: Lite against Full | yes | 1,535 |  |
| perf: determinism across build orders | yes | 438 |  |
| perf: what things cost | yes | 2,289 |  |
| perf: walking and driving through the state | yes | 153,539 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
