# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 186.8 s, started 2026-10-24 11:02:28.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,581 ms |
| the same square again, warm, pass by pass | 3,817 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 102.7, -33.0, facing -70° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 901 containers, 950 lights, 334 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 28,745 | 15,731 | 15,731 | 14,785 | 7,521,993 | 4,599,263 | 136 | 36,649 | 966 (0) | 8,379 | 11,196 | 901 |
| RaidOverhead | 584 | 466 | 466 | 466 | 464,268 | 272,426 | 134 | 1,900 | 0 (0) | 0 | 466 | 0 |
| RaidScenery | 116 | 115 | 115 | 115 | 80,822 | 104,342 | 44 | 338 | 0 (0) | 0 | 70 | 0 |
| **all** | 29,445 | 16,312 | 16,312 | 15,366 | 8,067,083 | 4,976,031 | 241 | 38,887 | 966 (0) | 8,379 | 11,732 | 901 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,012 | 15,293 | 15,293 | 14,366 | 6,277,558 | 3,957,372 | 82 | 34,242 | 928 (0) | 6,101 | 10,923 | 809 |
| World/Building shells | 131 | 130 | 130 | 130 | 815,118 | 409,876 | 48 | 1,221 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 327,635 | 162,474 | 119 | 981 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 302,668 | 175,464 | 18 | 618 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 124,997 | 54,019 | 13 | 545 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 131 | 131 | 131 | 131 | 44,704 | 26,067 | 47 | 445 | 0 (0) | 0 | 131 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 37,066 | 22,068 | 37 | 237 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,228 | 426 | 5 | 72 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 27 | 27 | 27 | 27 | 2,238 | 3,960 | 1 | 27 | 0 (0) | 0 | 27 | 0 |
| RaidOverhead/Wires | 23 | 23 | 23 | 23 | 3,879 | 6,720 | 1 | 23 | 0 (0) | 0 | 23 | 0 |
| World/StreetLamp | 44 | 22 | 22 | 3 | 1,628 | 2,520 | 3 | 22 | 22 (0) | 22 | 0 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,286 | 77,886 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 20,674 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 85 | 2% | 1 | 85 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 3 | 0% | 1 | 3 |
| lots | 0 | 0% | 4 | 0 |
| cemeteries | 5 | 0% | 500 | 0 |
| grounds | 0 | 0% | 2 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 37 | 1% | 1 | 37 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 24 | 1% | 123 | 1 |
| sidewalks | 24 | 1% | 67 | 4 |
| berms and driveways | 13 | 0% | 62 | 1 |
| road markings | 1 | 0% | 10 | 0 |
| paths | 2 | 0% | 72 | 0 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 88 | 2% | 38 | 5 |
| interior plans | 175 | 5% | 117 | 9 |
| interior shell | 12 | 0% | 116 | 0 |
| interior walls | 148 | 4% | 116 | 16 |
| interior floors | 14 | 0% | 116 | 0 |
| interior doors | 445 | 12% | 232 | 18 |
| interior switches | 70 | 2% | 116 | 4 |
| interior rooms | 2,158 | 57% | 697 | 28 |
| interior wall meshes | 83 | 2% | 116 | 3 |
| interior finish | 194 | 5% | 116 | 10 |
| cutaway | 10 | 0% | 5 | 2 |
| utility lines | 3 | 0% | 1 | 3 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 1 | 4 |
| street lights | 5 | 0% | 1 | 5 |
| signs | 2 | 0% | 1 | 2 |
| street furniture | 3 | 0% | 1 | 3 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 6 | 0% | 1 | 6 |
| nature plan | 1 | 0% | 1 | 1 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 11 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 4 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 5 | 0% | 9370 | 0 |
| driveway cars | 3 | 0% | 30 | 0 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 45 | 0 |
| kerbside cars | 28 | 1% | 204 | 6 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 24 | 1% | 117 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 12 | 0% | 1 | 12 |
| (emit the overhead meshes) | 10 | 0% | 1 | 10 |
| (emit the outdoor meshes) | 80 | 2% | 1 | 80 |
| **total** | **3,817** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **90** | colliders 35 | 0 | 129 | 61 | 131,567 | 68,538 | 0 | 109 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **29** | interior plans 3 | 1 | 144 | 64 | 130,318 | 74,324 | 4 | 207 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 159 + 8 | **64** | interior plans 6 | 1 | 216 | 53 | 137,060 | 73,087 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 402 + 27 | **85** | buildings 32 | 0 | 133 | 57 | 128,109 | 67,512 | 1 | 98 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **16** | streets 3 | 0 | 63 | 42 | 54,459 | 40,083 | 0 | 145 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.8 | 1 | 1.8 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.6 | 1 | 0.6 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.5 | 1 | 1.5 |
| junctions | 0.9 | 1 | 0.9 |
| streets | 5.8 | 3 | 2.4 |
| sidewalks | 2.9 | 2 | 2.0 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 1.2 | 1 | 1.2 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.3 | 3 | 2.3 |
| interior plans | 14.6 | 5 | 4.6 |
| cutaway | 1.1 | 1 | 1.1 |
| utility lines | 0.8 | 1 | 0.8 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 2.7 | 1 | 2.7 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.1 | 1 | 0.1 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 4.9 | 1 | 4.9 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.2 | 1 | 0.2 |
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
| colliders | 36.0 | 2 | 35.3 |
| objects | 1.8 | 1 | 1.8 |
| meshes | 0.5 | 1 | 0.5 |

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
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.4 | 1 | 1.4 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.8 | 1 | 0.8 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 4.0 | 3 | 2.0 |
| interior plans | 9.6 | 5 | 2.5 |
| cutaway | 0.9 | 1 | 0.9 |
| utility lines | 0.0 | 1 | 0.0 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.7 | 1 | 0.7 |
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
| street trees | 0.6 | 1 | 0.6 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.3 | 4 | 0.4 |
| driveway cars | 0.3 | 1 | 0.3 |
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
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 2.1 | 1 | 2.1 |
| water | 2.7 | 2 | 2.7 |
| fields | 0.1 | 1 | 0.1 |
| ground mask | 0.0 | 1 | 0.0 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.1 | 1 | 1.1 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 0.9 | 1 | 0.9 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 4.3 | 3 | 2.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 12.2 | 6 | 3.0 |
| interior plans | 17.8 | 6 | 5.9 |
| cutaway | 2.4 | 2 | 2.1 |
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
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 1.9 | 1 | 1.9 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.8 | 2 | 2.3 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 4.8 | 2 | 3.9 |
| objects | 1.0 | 1 | 1.0 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.8 | 1 | 0.8 |
| ground | 1.0 | 1 | 1.0 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.8 | 1 | 0.8 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 1.0 | 1 | 1.0 |
| junctions | 1.5 | 1 | 1.5 |
| streets | 5.9 | 2 | 5.5 |
| sidewalks | 3.0 | 2 | 2.4 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 6.6 | 2 | 6.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 39.8 | 5 | 32.3 |
| interior plans | 10.8 | 4 | 5.2 |
| cutaway | 0.7 | 1 | 0.7 |
| utility lines | 0.2 | 1 | 0.2 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.8 | 1 | 0.8 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.5 | 1 | 0.5 |
| street furniture | 0.6 | 1 | 0.6 |
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
| wall drifts | 1.9 | 1 | 1.9 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.0 | 1 | 1.0 |
| colliders | 0.7 | 1 | 0.7 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.5 | 1 | 0.5 |

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
| streets | 2.9 | 2 | 2.7 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
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
| woods | 4.2 | 4 | 1.8 |
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
| colliders | 0.5 | 1 | 0.5 |
| objects | 0.7 | 1 | 0.7 |
| meshes | 0.2 | 1 | 0.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 43 | 23 |
| 186_141 | yes | 26 | 2 |
| 185_141 | yes | 7 | 2 |
| 185_142 | yes | 9 | 2 |
| 183_148 | yes | 159 | 8 |
| 196_145 | yes | 402 | 27 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 119 | 16 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 492 ms (slowest 91 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **45** | interior plans 6 | 1 | 216 | 53 | 137,060 | 73,087 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **56** | interior plans 8 | 1 | 191 | 75 | 145,127 | 79,597 | 0 | 201 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 119 + 16 | **57** | interior plans 9 | 1 | 248 | 62 | 111,666 | 62,656 | 2 | 199 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **84** | interior plans 32 | 1 | 163 | 58 | 126,809 | 67,089 | 0 | 149 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **42** | fences and walls 6 | 0 | 172 | 63 | 88,639 | 52,327 | 1 | 126 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **37** | interior plans 9 | 0 | 150 | 53 | 111,495 | 64,442 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **91** | buildings 48 | 1 | 197 | 53 | 119,954 | 65,083 | 0 | 206 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **31** | interior plans 5 | 0 | 142 | 54 | 99,066 | 58,507 | 3 | 86 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **50** | interior plans 8 | 1 | 173 | 46 | 189,238 | 104,957 | 0 | 153 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,654 | 517 | 517 | 514 | 1,129,054 | 627,745 | 132 | 2,192 | 6 (0) | 1,407 | 358 | 78 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (131,567 vertices, 68,538 triangles, 129 objects, 61 meshes; hash b5bd25210cf4318e vs b5bd25210cf4318e; 111 colliders, lights and loot containers, hash e615e127ed197e3c vs e615e127ed197e3c). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 90 | 108 / 54 / 35.8 | 0 / 0.0 | 59 / 128,896 / 0.2 | 1.5 | 52.5 | 41 / 3.6 |
| rural-5km | Full | 29 | 206 / 68 / 1.3 | 0 / 0.0 | 63 / 127,719 / 0.3 | 1.4 | 26.6 | 43 / 0.3 |
| louisville-downtown | Full | 64 | 165 / 120 / 4.8 | 0 / 0.0 | 51 / 134,449 / 0.2 | 2.9 | 56.2 | 36 / 0.3 |
| lexington-downtown | Full | 85 | 96 / 61 / 0.7 | 0 / 0.0 | 53 / 125,380 / 0.4 | 1.1 | 82.8 | 39 / 0.3 |
| hazard | Full | 16 | 144 / 15 / 0.5 | 0 / 0.0 | 37 / 51,261 / 0.1 | 0.7 | 15.1 | 30 / 0.2 |
| louisville 0,0 | Full | 45 | 165 / 120 / 1.3 | 0 / 0.0 | 51 / 134,449 / 0.2 | 1.0 | 42.8 | 36 / 0.2 |
| louisville 1,0 | Full | 56 | 200 / 109 / 1.4 | 0 / 0.0 | 73 / 142,458 / 0.3 | 1.7 | 52.6 | 46 / 0.3 |
| louisville -1,0 | Full | 57 | 196 / 140 / 1.9 | 0 / 0.0 | 51 / 108,273 / 0.2 | 1.1 | 53.6 | 39 / 0.7 |
| louisville 0,1 | Full | 84 | 148 / 81 / 1.0 | 0 / 0.0 | 54 / 124,299 / 0.2 | 1.2 | 81.9 | 36 / 0.3 |
| louisville 0,-1 | Full | 42 | 125 / 69 / 1.0 | 0 / 0.0 | 58 / 84,384 / 0.2 | 1.2 | 40.1 | 37 / 0.2 |
| louisville 1,1 | Full | 37 | 120 / 74 / 0.9 | 0 / 0.0 | 50 / 108,424 / 0.2 | 1.0 | 35.0 | 35 / 0.2 |
| louisville -1,1 | Full | 91 | 205 / 114 / 1.5 | 0 / 0.0 | 50 / 116,669 / 0.2 | 1.2 | 88.3 | 37 / 0.2 |
| louisville 1,-1 | Full | 31 | 82 / 46 / 0.6 | 0 / 0.0 | 46 / 94,776 / 0.1 | 1.0 | 29.6 | 35 / 0.3 |
| louisville -1,-1 | Full | 50 | 152 / 103 / 1.4 | 0 / 0.0 | 42 / 186,101 / 0.2 | 0.9 | 47.3 | 30 / 0.3 |

The Louisville block's NavMesh: 331 sources collected in 2 ms; baked at once (as one 384 m square) in 195 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 42 | 7 | 17% | interior plans 2.7 | sidewalks 1.2 | 131,567 / 5,726 | 129 / 37 | 61 / 32 | 109 / 1 | 0 / 0 |
| rural-5km | 25 | 4 | 15% | interior plans 2.4 | objects 0.8 | 130,160 / 4,380 | 141 / 38 | 64 / 33 | 202 / 1 | 4 / 0 |
| louisville-downtown | 45 | 7 | 15% | interior plans 6.1 | buildings 2.3 | 129,810 / 7,103 | 214 / 33 | 53 / 28 | 165 / 1 | 0 / 0 |
| lexington-downtown | 40 | 5 | 13% | interior plans 5.6 | sidewalks 1.0 | 120,419 / 4,078 | 138 / 33 | 57 / 28 | 94 / 1 | 2 / 0 |
| hazard | 18 | 4 | 20% | streets 2.7 | objects 0.7 | 62,080 / 4,375 | 77 / 32 | 43 / 27 | 147 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 55.50 | 27.75 | make 47.98 + switch on 7.52 ms; 1602 objects; destroy 3.34 ms; 3,000 rays 2.72 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 14.62 | 7.31 | make 10.00 + switch on 4.61 ms; 10 objects; destroy 1.83 ms; 3,000 rays 2.23 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.90 | 5.95 | make 3.81 + switch on 8.09 ms; 2 objects; destroy 0.11 ms; 3,000 rays 2.10 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.18 | 1.09 | make 2.15 + switch on 0.03 ms (+ 7.75 ms baking on the worker); 2 objects; destroy 0.08 ms; 3,000 rays 2.05 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.94 | 223.59 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.13 | 203.15 | objects 0.89, allocate 0.02, pack 7.11, apply 0.11 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.02 | 25.48 | objects 0.89, allocate 0.02, pack 7.10 (worker), apply 0.11 ms |
| primitives | Box, turned, flat colour | 20,000 | 25.20 | 1.26 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 71.87 | 3.59 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 44.98 | 2.25 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 99.48 | 4.97 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 24.89 | 1.24 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 25.47 | 1.27 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.50 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.82 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 15.01 | 3.00 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 48.48 | 9.70 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 25.78 | 8.59 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 39.41 | 13.14 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 18.22 | 9.11 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.41 | 11.41 |  |
| objects | BoxCollider added to one object | 2,000 | 39.23 | 19.62 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 23.52 | 117.59 |  |
| objects | RoomLight.Spawn | 100 | 10.10 | 100.99 |  |
| objects | switching all of the above on | 1 | 26.99 | 26987.00 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 10.56 | 10556.80 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.62 | 10.09 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.30 | 229.46 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 382.45 | 382449.00 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 364.28 | 364282.50 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 21.60 | 21594.60 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,300 ms, 3,872,662 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (12) | 0.15 | 0.04 | 0.06 | 3.77 | 18.47 | 2 / 1 / 1 / 0 | landmarks 5.9 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,067 / 3,903,714 | 2 | 196 / 935 (pool 40) |
| drive-east | 2400 (223) | 0.38 | 0.05 | 3.45 | 5.14 | 12.33 | 31 / 2 / 0 / 0 | nature plan 7.3 | 40 (24 / 16) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,155 / 5,553,880 | 2 | 195 / 1,009 (pool 40) |
| teleport-louisville | 720 (101) | 0.89 | 0.05 | 6.45 | 9.55 | 40.10 | 50 / 12 / 3 / 3 | interior plans 9.9 | 25 (9 / 16) | 0 | 30 | 0.1 | 1.2 | 8 / 73 | 50 (16 Lite) / 2,824 / 5,087,198 | 4 | 212 / 989 (pool 40) |
| drive-louisville | 1800 (162) | 0.42 | 0.05 | 3.25 | 5.36 | 42.14 | 30 / 9 / 3 / 2 | interior plans 37.3 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 19 | 55 (12 Lite) / 3,233 / 5,891,143 | 6 | 256 / 1,025 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: buildings (1x, worst 0.9 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: nature plan (1x, worst 7.3 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (7x, worst 9.9 ms), road network (2x, worst 8.1 ms), sidewalks (1x, worst 2.8 ms), wall drifts (1x, worst 2.0 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (5x, worst 37.3 ms), (swap) (1x, worst 10.2 ms), wall drifts (1x, worst 5.4 ms), buildings (1x, worst 2.6 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.594 | 0% | 1,928 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 419 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.488 | 0% | 139 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.116 | 0% | 105 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.423 | 0% | 63 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.074 | 0% | 65 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.473 | 0% | 75 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.090 | 0% | 57 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.356 | 0% | 98 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.074 | 0% | 77 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.586 | 0% | 56 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 68 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 63 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 58 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.399 | 0% | 85 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.074 | 0% | 80 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.362 | 0% | 57 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 89 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.456 | 0% | 140 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.164 | 20% | 212 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.435 | 0% | 129 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.168 | 20% | 122 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.540 | 0% | 113 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.465 | 0% | 61 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 55 |

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
| square: KentuckyMap.Build | yes | 6,583 |  |
| square: count | yes | 875 |  |
| square: pick spots | yes | 87 |  |
| square: pictures | yes | 6,593 |  |
| square: discard | yes | 244 |  |
| square: passes (warm rebuild) | yes | 4,038 |  |
| chunks: stress test | yes | 1,077 |  |
| chunks: Louisville 3x3 | yes | 1,618 |  |
| chunks: determinism | yes | 154 |  |
| perf: Lite against Full | yes | 1,080 |  |
| perf: determinism across build orders | yes | 300 |  |
| perf: what things cost | yes | 1,621 |  |
| perf: walking and driving through the state | yes | 162,451 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
