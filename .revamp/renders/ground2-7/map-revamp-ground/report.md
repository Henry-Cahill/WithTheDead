# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 186.2 s, started 2026-10-24 11:11:23.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,109 ms |
| the same square again, warm, pass by pass | 3,833 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 10.8, 142.2, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 901 containers, 956 lights, 337 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,311 | 16,234 | 16,234 | 15,281 | 7,570,563 | 4,628,385 | 134 | 37,230 | 972 (0) | 8,392 | 11,708 | 901 |
| RaidOverhead | 584 | 466 | 466 | 466 | 462,439 | 271,337 | 134 | 1,895 | 0 (0) | 0 | 466 | 0 |
| RaidScenery | 115 | 114 | 114 | 114 | 80,816 | 104,170 | 43 | 342 | 0 (0) | 0 | 69 | 0 |
| **all** | 30,010 | 16,814 | 16,814 | 15,861 | 8,113,818 | 5,003,892 | 239 | 39,467 | 972 (0) | 8,392 | 12,243 | 901 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,579 | 15,793 | 15,793 | 14,862 | 6,322,098 | 3,984,553 | 82 | 34,799 | 932 (0) | 6,120 | 11,435 | 811 |
| World/Building shells | 131 | 130 | 130 | 130 | 815,118 | 409,876 | 48 | 1,221 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 327,105 | 161,997 | 119 | 982 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 298,083 | 174,354 | 16 | 612 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 133,403 | 56,750 | 13 | 572 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 130 | 130 | 130 | 130 | 43,102 | 24,867 | 46 | 438 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 37,198 | 22,156 | 36 | 238 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,240 | 430 | 5 | 76 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 26 | 26 | 26 | 26 | 2,088 | 3,696 | 1 | 26 | 0 (0) | 0 | 26 | 0 |
| World/StreetLamp | 50 | 25 | 25 | 3 | 1,837 | 2,840 | 3 | 25 | 24 (0) | 25 | 0 | 0 |
| RaidOverhead/Wires | 24 | 24 | 24 | 24 | 4,182 | 7,308 | 1 | 24 | 0 (0) | 0 | 24 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,286 | 77,886 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 21,107 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 80 | 2% | 1 | 80 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| ground mask | 3 | 0% | 1 | 3 |
| lots | 0 | 0% | 4 | 0 |
| cemeteries | 5 | 0% | 500 | 0 |
| grounds | 0 | 0% | 2 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 22 | 1% | 1 | 22 |
| road network | 5 | 0% | 1 | 5 |
| junctions | 4 | 0% | 1 | 4 |
| streets | 29 | 1% | 67 | 19 |
| sidewalks | 28 | 1% | 67 | 2 |
| berms and driveways | 15 | 0% | 62 | 1 |
| road markings | 1 | 0% | 17 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 142 | 4% | 51 | 7 |
| interior plans | 159 | 4% | 117 | 8 |
| interior shell | 12 | 0% | 116 | 1 |
| interior walls | 133 | 3% | 116 | 15 |
| interior floors | 13 | 0% | 116 | 0 |
| interior doors | 451 | 12% | 232 | 16 |
| interior switches | 64 | 2% | 116 | 4 |
| interior rooms | 2,175 | 57% | 697 | 46 |
| interior wall meshes | 90 | 2% | 116 | 6 |
| interior finish | 175 | 5% | 116 | 4 |
| cutaway | 39 | 1% | 18 | 2 |
| utility lines | 4 | 0% | 1 | 4 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 1 | 4 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 3 | 0% | 1 | 3 |
| street furniture | 13 | 0% | 1 | 13 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 4 | 0% | 1 | 4 |
| nature plan | 1 | 0% | 1 | 1 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 3 | 0% | 9370 | 0 |
| driveway cars | 2 | 0% | 29 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 45 | 0 |
| kerbside cars | 12 | 0% | 200 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 28 | 1% | 117 | 3 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 1 | 0% | 1 | 1 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 13 | 0% | 1 | 13 |
| (emit the overhead meshes) | 16 | 0% | 1 | 16 |
| (emit the outdoor meshes) | 50 | 1% | 1 | 50 |
| **total** | **3,833** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **73** | cutaway 23 | 0 | 131 | 61 | 135,163 | 69,858 | 0 | 109 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **37** | buildings 7 | 1 | 144 | 64 | 131,989 | 75,290 | 4 | 199 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 151 + 9 | **69** | buildings 7 | 1 | 213 | 49 | 136,146 | 72,374 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 414 + 29 | **79** | buildings 32 | 0 | 134 | 57 | 128,401 | 67,549 | 1 | 99 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **17** | streets 3 | 0 | 65 | 43 | 55,752 | 41,025 | 0 | 146 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.9 | 1 | 1.9 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.6 | 1 | 0.6 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.4 | 1 | 1.4 |
| junctions | 0.9 | 1 | 0.9 |
| streets | 6.8 | 1 | 6.8 |
| sidewalks | 2.9 | 2 | 2.0 |
| berms and driveways | 1.5 | 1 | 1.5 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.3 | 3 | 2.3 |
| interior plans | 11.4 | 5 | 3.2 |
| cutaway | 25.1 | 2 | 23.2 |
| utility lines | 0.8 | 1 | 0.8 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.7 | 1 | 1.7 |
| street furniture | 0.7 | 1 | 0.7 |
| landmarks | 0.4 | 1 | 0.4 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.5 | 1 | 0.5 |
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
| colliders | 0.8 | 1 | 0.8 |
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
| ground mask | 0.2 | 1 | 0.2 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.9 | 1 | 1.9 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.0 | 3 | 7.4 |
| interior plans | 9.2 | 5 | 2.5 |
| cutaway | 3.1 | 2 | 2.0 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.8 | 1 | 0.8 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.3 | 1 | 0.3 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 2.3 | 2 | 2.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.6 | 1 | 0.6 |
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
| colliders | 1.1 | 1 | 1.1 |
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.2 | 1 | 0.2 |

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
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 4.2 | 1 | 4.2 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 17.3 | 6 | 6.9 |
| interior plans | 18.0 | 6 | 5.9 |
| cutaway | 7.4 | 3 | 5.3 |
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
| kerbside cars | 1.4 | 1 | 1.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.1 |
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
| ground mask | 0.5 | 1 | 0.5 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 6.6 | 1 | 6.6 |
| sidewalks | 2.7 | 2 | 2.3 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 39.5 | 5 | 32.4 |
| interior plans | 10.7 | 4 | 5.2 |
| cutaway | 3.2 | 2 | 2.4 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.9 | 1 | 0.9 |
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
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| ground mask | 0.1 | 1 | 0.1 |
| lots | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.1 | 1 | 1.1 |
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
| woods | 4.1 | 4 | 1.7 |
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
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 40 | 8 |
| 186_141 | yes | 15 | 7 |
| 185_141 | yes | 7 | 2 |
| 185_142 | yes | 9 | 2 |
| 183_148 | yes | 151 | 9 |
| 196_145 | yes | 414 | 29 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 118 | 20 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 534 ms (slowest 94 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **48** | interior plans 6 | 0 | 213 | 49 | 136,146 | 72,374 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **59** | interior plans 8 | 1 | 192 | 75 | 145,017 | 79,514 | 0 | 201 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 118 + 20 | **66** | interior plans 10 | 1 | 238 | 55 | 108,199 | 59,662 | 1 | 193 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **88** | interior plans 37 | 0 | 161 | 59 | 119,576 | 62,623 | 1 | 133 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **52** | buildings 9 | 0 | 170 | 60 | 85,403 | 48,826 | 1 | 121 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **40** | interior plans 9 | 0 | 151 | 53 | 111,549 | 64,468 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **94** | buildings 49 | 1 | 198 | 53 | 119,951 | 65,002 | 0 | 204 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **33** | interior plans 5 | 0 | 142 | 54 | 98,351 | 57,841 | 3 | 84 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **54** | interior plans 8 | 0 | 169 | 41 | 187,256 | 103,065 | 0 | 150 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,636 | 499 | 499 | 496 | 1,111,448 | 613,375 | 132 | 2,142 | 6 (0) | 1,373 | 340 | 78 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (135,163 vertices, 69,858 triangles, 131 objects, 61 meshes; hash 941f1fb9fe1a06af vs 941f1fb9fe1a06af; 111 colliders, lights and loot containers, hash bac77ee04f661af5 vs bac77ee04f661af5). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 73 | 108 / 55 / 0.7 | 0 / 0.0 | 59 / 132,489 / 24.8 | 26.0 | 46.5 | 41 / 4.7 |
| rural-5km | Full | 37 | 198 / 67 / 1.1 | 0 / 0.0 | 63 / 129,390 / 2.2 | 3.6 | 32.2 | 43 / 0.3 |
| louisville-downtown | Full | 69 | 165 / 120 / 1.4 | 0 / 0.0 | 48 / 133,553 / 3.2 | 7.9 | 60.2 | 36 / 0.9 |
| lexington-downtown | Full | 79 | 97 / 61 / 0.7 | 0 / 0.0 | 53 / 125,672 / 2.6 | 3.7 | 74.1 | 39 / 0.3 |
| hazard | Full | 17 | 145 / 15 / 0.5 | 0 / 0.0 | 38 / 52,338 / 0.1 | 0.8 | 15.6 | 30 / 0.2 |
| louisville 0,0 | Full | 48 | 165 / 120 / 1.4 | 0 / 0.0 | 48 / 133,553 / 2.8 | 3.8 | 42.7 | 36 / 0.3 |
| louisville 1,0 | Full | 59 | 200 / 109 / 1.5 | 0 / 0.0 | 73 / 142,348 / 2.6 | 4.2 | 53.3 | 46 / 0.3 |
| louisville -1,0 | Full | 66 | 191 / 137 / 2.1 | 0 / 0.0 | 49 / 105,292 / 1.6 | 2.7 | 60.7 | 38 / 0.6 |
| louisville 0,1 | Full | 88 | 131 / 76 / 0.9 | 0 / 0.0 | 54 / 117,000 / 2.0 | 3.2 | 84.0 | 37 / 0.2 |
| louisville 0,-1 | Full | 52 | 120 / 69 / 1.0 | 0 / 0.0 | 55 / 82,144 / 0.9 | 2.0 | 49.5 | 37 / 0.2 |
| louisville 1,1 | Full | 40 | 120 / 74 / 0.9 | 0 / 0.0 | 50 / 108,478 / 1.8 | 2.9 | 35.9 | 35 / 0.2 |
| louisville -1,1 | Full | 94 | 203 / 114 / 1.4 | 0 / 0.0 | 50 / 116,666 / 1.9 | 2.9 | 89.7 | 37 / 0.3 |
| louisville 1,-1 | Full | 33 | 80 / 45 / 0.5 | 0 / 0.0 | 46 / 94,097 / 1.2 | 2.0 | 30.9 | 35 / 0.2 |
| louisville -1,-1 | Full | 54 | 149 / 103 / 1.2 | 0 / 0.0 | 39 / 184,626 / 3.7 | 4.6 | 48.4 | 30 / 0.3 |

The Louisville block's NavMesh: 331 sources collected in 2 ms; baked at once (as one 384 m square) in 175 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 44 | 12 | 28% | streets 6.9 | streets 5.2 | 135,163 / 9,288 | 131 / 37 | 61 / 32 | 109 / 1 | 0 / 0 |
| rural-5km | 27 | 5 | 18% | interior plans 2.5 | streets 1.4 | 131,831 / 4,974 | 141 / 38 | 64 / 33 | 194 / 1 | 4 / 0 |
| louisville-downtown | 48 | 10 | 21% | interior plans 6.2 | buildings 2.4 | 128,996 / 8,579 | 211 / 36 | 49 / 31 | 165 / 1 | 0 / 0 |
| lexington-downtown | 44 | 11 | 26% | streets 6.7 | streets 5.4 | 120,841 / 6,044 | 139 / 32 | 57 / 27 | 95 / 1 | 2 / 0 |
| hazard | 27 | 6 | 21% | wires 7.8 | streets 2.0 | 62,724 / 4,637 | 79 / 32 | 44 / 27 | 148 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 64.48 | 32.24 | make 56.06 + switch on 8.42 ms; 1602 objects; destroy 4.61 ms; 3,000 rays 2.73 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 14.96 | 7.48 | make 10.23 + switch on 4.73 ms; 10 objects; destroy 1.78 ms; 3,000 rays 2.13 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.55 | 5.78 | make 3.87 + switch on 7.68 ms; 2 objects; destroy 0.12 ms; 3,000 rays 2.05 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.64 | 1.32 | make 2.61 + switch on 0.03 ms (+ 9.92 ms baking on the worker); 2 objects; destroy 0.07 ms; 3,000 rays 2.09 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 9.85 | 246.19 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.03 | 200.76 | objects 0.78, allocate 0.01, pack 7.12, apply 0.12 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.00 | 24.87 | objects 0.87, allocate 0.02, pack 7.23 (worker), apply 0.11 ms |
| primitives | Box, turned, flat colour | 20,000 | 27.08 | 1.35 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 86.22 | 4.31 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 49.28 | 2.46 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 102.16 | 5.11 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 27.45 | 1.37 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 46.53 | 2.33 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.48 | 0.32 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.93 | 0.25 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 22.30 | 4.46 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 32.31 | 6.46 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 22.02 | 7.34 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 41.07 | 13.69 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.33 | 6.67 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.47 | 11.47 |  |
| objects | BoxCollider added to one object | 2,000 | 43.47 | 21.73 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 24.12 | 120.62 |  |
| objects | RoomLight.Spawn | 100 | 10.35 | 103.51 |  |
| objects | switching all of the above on | 1 | 30.73 | 30728.40 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 14.91 | 14908.40 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.59 | 9.71 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.09 | 208.55 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 389.60 | 389602.70 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 364.48 | 364477.40 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 22.15 | 22147.20 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,267 ms, 3,872,801 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (11) | 0.14 | 0.04 | 0.06 | 4.36 | 13.55 | 2 / 1 / 0 / 0 | buildings 3.6 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,040 / 3,904,045 | 2 | 188 / 935 (pool 40) |
| drive-east | 2400 (271) | 0.45 | 0.05 | 3.92 | 5.23 | 10.74 | 38 / 5 / 0 / 0 | interior plans 10.5 | 43 (27 / 16) | 3 | 15 | 0.0 | 0.0 | 0 / 0 | 55 (12 Lite) / 3,270 / 5,907,822 | 2 | 190 / 1,022 (pool 40) |
| teleport-louisville | 720 (114) | 0.96 | 0.05 | 6.10 | 9.56 | 42.19 | 52 / 14 / 3 / 3 | interior plans 9.5 | 25 (9 / 16) | 0 | 30 | 0.1 | 1.3 | 8 / 76 | 50 (16 Lite) / 2,806 / 5,101,671 | 4 | 206 / 990 (pool 40) |
| drive-louisville | 1800 (199) | 0.60 | 0.05 | 3.72 | 8.14 | 65.59 | 46 / 19 / 10 / 4 | interior plans 34.6 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 20 | 55 (12 Lite) / 3,194 / 5,893,484 | 6 | 252 / 1,025 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: woods (1x, worst 0.6 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: interior plans (2x, worst 10.5 ms), buildings (2x, worst 8.6 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (9x, worst 9.5 ms), junctions (1x, worst 9.4 ms), utility poles (1x, worst 4.9 ms), cutaway (1x, worst 3.4 ms), wall drifts (1x, worst 3.0 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (5x, worst 34.6 ms), sidewalks (1x, worst 30.8 ms), utility poles (1x, worst 30.8 ms), street furniture (2x, worst 16.9 ms), berms and driveways (1x, worst 15.9 ms), (swap) (1x, worst 10.9 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.587 | 0% | 1,593 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 313 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.524 | 0% | 113 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.073 | 0% | 86 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.450 | 0% | 56 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 58 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.428 | 0% | 63 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.085 | 0% | 59 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.409 | 0% | 50 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.072 | 0% | 64 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.586 | 0% | 53 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 55 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 54 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 67 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.429 | 0% | 52 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.075 | 0% | 62 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 62 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 57 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.265 | 0% | 83 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.165 | 13% | 76 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.401 | 0% | 125 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.167 | 20% | 127 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.540 | 0% | 134 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.465 | 0% | 56 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 47 |

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
| setup | yes | 63 |  |
| square: KentuckyMap.Build | yes | 6,111 |  |
| square: count | yes | 832 |  |
| square: pick spots | yes | 71 |  |
| square: pictures | yes | 5,423 |  |
| square: discard | yes | 271 |  |
| square: passes (warm rebuild) | yes | 4,053 |  |
| chunks: stress test | yes | 1,050 |  |
| chunks: Louisville 3x3 | yes | 1,621 |  |
| chunks: determinism | yes | 140 |  |
| perf: Lite against Full | yes | 1,162 |  |
| perf: determinism across build orders | yes | 334 |  |
| perf: what things cost | yes | 1,684 |  |
| perf: walking and driving through the state | yes | 163,371 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
