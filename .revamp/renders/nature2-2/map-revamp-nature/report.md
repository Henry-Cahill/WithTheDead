# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 210.4 s, started 2026-10-24 10:24:33.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 7,962 ms |
| the same square again, warm, pass by pass | 4,066 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 10.8, 142.2, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 911 containers, 954 lights, 337 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,339 | 16,231 | 16,231 | 15,281 | 7,598,198 | 4,638,911 | 134 | 37,229 | 970 (0) | 8,405 | 11,708 | 911 |
| RaidOverhead | 585 | 467 | 467 | 467 | 462,844 | 271,817 | 134 | 1,896 | 0 (0) | 0 | 467 | 0 |
| RaidScenery | 115 | 114 | 114 | 114 | 81,047 | 105,094 | 43 | 341 | 0 (0) | 0 | 69 | 0 |
| **all** | 30,039 | 16,812 | 16,812 | 15,862 | 8,142,089 | 5,015,822 | 239 | 39,466 | 970 (0) | 8,405 | 12,244 | 911 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,587 | 15,793 | 15,793 | 14,862 | 6,321,697 | 3,984,349 | 82 | 34,799 | 932 (0) | 6,120 | 11,435 | 815 |
| World/Building shells | 131 | 130 | 130 | 130 | 815,118 | 409,876 | 48 | 1,221 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 327,105 | 161,997 | 119 | 982 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 326,346 | 185,442 | 16 | 614 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 133,407 | 56,752 | 13 | 572 | 0 (0) | 0 | 0 | 0 |
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
| (read the area) | 61 | 2% | 1 | 61 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| lots | 3 | 0% | 1 | 3 |
| cemeteries | 4 | 0% | 4 | 1 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 60 | 1% | 1 | 60 |
| road network | 8 | 0% | 1 | 8 |
| junctions | 15 | 0% | 1 | 15 |
| streets | 28 | 1% | 67 | 17 |
| sidewalks | 23 | 1% | 67 | 2 |
| berms and driveways | 13 | 0% | 62 | 1 |
| road markings | 1 | 0% | 17 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 128 | 3% | 48 | 11 |
| interior plans | 186 | 5% | 117 | 12 |
| interior shell | 13 | 0% | 116 | 0 |
| interior walls | 133 | 3% | 116 | 7 |
| interior floors | 14 | 0% | 116 | 0 |
| interior doors | 429 | 11% | 232 | 12 |
| interior switches | 67 | 2% | 116 | 2 |
| interior rooms | 2,341 | 58% | 697 | 36 |
| interior wall meshes | 87 | 2% | 116 | 2 |
| interior finish | 189 | 5% | 116 | 9 |
| cutaway | 43 | 1% | 20 | 2 |
| utility lines | 4 | 0% | 1 | 4 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 1 | 4 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 3 | 0% | 1 | 3 |
| street furniture | 13 | 0% | 1 | 13 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 5 | 0% | 1 | 5 |
| nature plan | 0 | 0% | 1 | 0 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 2 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 3 | 0% | 9370 | 0 |
| driveway cars | 3 | 0% | 30 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 40 | 0 |
| kerbside cars | 28 | 1% | 215 | 6 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 32 | 1% | 117 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 11 | 0% | 1 | 11 |
| (emit the overhead meshes) | 9 | 0% | 1 | 9 |
| (emit the outdoor meshes) | 69 | 2% | 1 | 69 |
| **total** | **4,066** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **88** | data 27 | 0 | 131 | 61 | 136,811 | 70,454 | 0 | 109 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **56** | buildings 8 | 1 | 144 | 64 | 135,329 | 76,566 | 4 | 199 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 148 + 9 | **75** | buildings 7 | 0 | 214 | 50 | 138,923 | 73,362 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 418 + 35 | **87** | buildings 38 | 0 | 134 | 57 | 129,233 | 67,855 | 1 | 99 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **18** | streets 3 | 0 | 69 | 43 | 57,402 | 42,173 | 0 | 150 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 27.0 | 1 | 27.0 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.7 | 1 | 1.7 |
| road network | 1.4 | 1 | 1.4 |
| junctions | 0.9 | 1 | 0.9 |
| streets | 6.8 | 1 | 6.8 |
| sidewalks | 2.8 | 2 | 2.0 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 5.4 | 3 | 2.4 |
| interior plans | 14.7 | 6 | 4.5 |
| cutaway | 3.0 | 2 | 2.1 |
| utility lines | 0.9 | 1 | 0.9 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.6 | 1 | 0.6 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 5.9 | 1 | 5.9 |
| street furniture | 1.1 | 1 | 1.1 |
| landmarks | 0.5 | 1 | 0.5 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.8 | 1 | 0.8 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.3 | 1 | 0.3 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.1 | 1 | 0.1 |
| street trees | 0.1 | 1 | 0.1 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.1 |
| driveway cars | 0.3 | 1 | 0.3 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.4 | 1 | 0.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.6 | 2 | 2.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 1.3 | 1 | 1.3 |
| objects | 3.3 | 1 | 3.3 |
| meshes | 1.1 | 1 | 1.1 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.5 | 1 | 0.5 |
| snow tone | 4.7 | 1 | 4.7 |
| road network | 0.3 | 1 | 0.3 |
| junctions | 1.2 | 1 | 1.2 |
| streets | 1.9 | 1 | 1.9 |
| sidewalks | 0.6 | 1 | 0.6 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.7 | 3 | 8.0 |
| interior plans | 15.2 | 6 | 4.3 |
| cutaway | 3.7 | 2 | 2.3 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.7 | 1 | 1.7 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.4 | 1 | 0.4 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 5.1 | 1 | 5.1 |
| yard trees | 2.5 | 2 | 2.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.7 | 1 | 0.7 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.1 | 4 | 0.4 |
| driveway cars | 0.4 | 1 | 0.4 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.4 | 1 | 1.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.1 | 1 | 1.1 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 2.1 | 1 | 2.1 |
| water | 2.5 | 1 | 2.5 |
| fields | 0.1 | 1 | 0.1 |
| lots | 0.8 | 1 | 0.8 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.1 | 1 | 1.1 |
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 1.2 | 1 | 1.2 |
| berms and driveways | 1.1 | 1 | 1.1 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 4.3 | 1 | 4.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 17.8 | 6 | 7.0 |
| interior plans | 19.9 | 6 | 6.4 |
| cutaway | 8.2 | 3 | 5.1 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.5 | 1 | 1.5 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.6 | 1 | 0.6 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.1 | 1 | 0.1 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.1 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 1.7 | 1 | 1.7 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 3.5 | 2 | 2.8 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.6 | 1 | 1.6 |
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.2 | 1 | 0.2 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.2 | 1 | 0.2 |
| ground | 0.6 | 1 | 0.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.5 | 1 | 0.5 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.5 | 1 | 0.5 |
| streets | 6.6 | 1 | 6.6 |
| sidewalks | 2.7 | 2 | 2.3 |
| berms and driveways | 1.4 | 1 | 1.4 |
| road markings | 0.2 | 1 | 0.2 |
| paths | 0.8 | 1 | 0.8 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 46.4 | 5 | 37.9 |
| interior plans | 12.3 | 5 | 4.0 |
| cutaway | 2.8 | 2 | 2.7 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.9 | 1 | 0.9 |
| street lights | 0.2 | 1 | 0.2 |
| signs | 0.7 | 1 | 0.7 |
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
| woods | 0.3 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 1.0 | 1 | 1.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.2 | 2 | 2.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.1 | 1 | 1.1 |
| colliders | 0.8 | 1 | 0.8 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.1 | 1 | 0.1 |

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
| snow tone | 1.8 | 1 | 1.8 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 3.2 | 1 | 3.2 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.5 | 1 | 0.5 |
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
| nature plan | 0.0 | 1 | 0.0 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| woods | 4.0 | 4 | 1.7 |
| driveway cars | 0.0 | 1 | 0.0 |
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
| colliders | 0.5 | 1 | 0.5 |
| objects | 0.8 | 1 | 0.8 |
| meshes | 0.2 | 1 | 0.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 46 | 15 |
| 186_141 | yes | 11 | 2 |
| 185_141 | yes | 7 | 1 |
| 185_142 | yes | 9 | 2 |
| 183_148 | yes | 148 | 9 |
| 196_145 | yes | 418 | 35 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 139 | 26 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 539 ms (slowest 97 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **49** | interior plans 6 | 1 | 214 | 50 | 138,923 | 73,362 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **61** | interior plans 8 | 1 | 192 | 75 | 149,148 | 80,896 | 0 | 201 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 139 + 26 | **54** | interior plans 9 | 1 | 236 | 55 | 108,829 | 59,854 | 1 | 191 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **75** | interior plans 30 | 0 | 157 | 59 | 120,136 | 62,361 | 1 | 129 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **53** | buildings 9 | 0 | 172 | 60 | 87,136 | 49,662 | 1 | 123 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **48** | objects 9 | 1 | 151 | 53 | 113,517 | 65,100 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **97** | buildings 49 | 1 | 179 | 53 | 116,902 | 62,864 | 0 | 195 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **36** | interior plans 5 | 1 | 152 | 54 | 102,095 | 60,113 | 3 | 89 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **66** | interior plans 13 | 1 | 169 | 41 | 189,242 | 103,759 | 0 | 150 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,624 | 500 | 500 | 497 | 1,125,928 | 617,971 | 132 | 2,145 | 6 (0) | 1,365 | 341 | 76 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (136,811 vertices, 70,454 triangles, 131 objects, 61 meshes; hash 3d648b987cf25088 vs 3d648b987cf25088; 111 colliders, lights and loot containers, hash bac77ee04f661af5 vs bac77ee04f661af5). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 88 | 108 / 55 / 1.2 | 0 / 0.0 | 59 / 134,133 / 2.9 | 4.2 | 82.7 | 41 / 3.5 |
| rural-5km | Full | 56 | 198 / 67 / 1.1 | 0 / 0.0 | 63 / 132,728 / 2.9 | 4.2 | 50.9 | 43 / 0.3 |
| louisville-downtown | Full | 75 | 165 / 120 / 1.6 | 0 / 0.0 | 49 / 136,325 / 3.5 | 8.7 | 64.9 | 37 / 0.9 |
| lexington-downtown | Full | 87 | 97 / 61 / 0.8 | 0 / 0.0 | 53 / 126,500 / 2.3 | 3.3 | 83.2 | 39 / 0.3 |
| hazard | Full | 18 | 149 / 19 / 0.5 | 0 / 0.0 | 38 / 53,978 / 0.1 | 0.7 | 16.4 | 30 / 0.2 |
| louisville 0,0 | Full | 49 | 165 / 120 / 1.4 | 0 / 0.0 | 49 / 136,325 / 2.5 | 3.3 | 44.6 | 37 / 0.3 |
| louisville 1,0 | Full | 61 | 200 / 109 / 1.4 | 0 / 0.0 | 73 / 146,476 / 2.7 | 4.4 | 54.9 | 46 / 0.3 |
| louisville -1,0 | Full | 54 | 189 / 135 / 1.6 | 0 / 0.0 | 49 / 105,892 / 1.5 | 2.4 | 50.2 | 38 / 0.4 |
| louisville 0,1 | Full | 75 | 127 / 72 / 0.9 | 0 / 0.0 | 54 / 117,664 / 2.1 | 3.2 | 70.8 | 37 / 0.2 |
| louisville 0,-1 | Full | 53 | 122 / 71 / 0.9 | 0 / 0.0 | 55 / 83,840 / 1.0 | 2.0 | 50.4 | 37 / 0.2 |
| louisville 1,1 | Full | 48 | 120 / 74 / 0.9 | 0 / 0.0 | 50 / 110,458 / 1.8 | 10.7 | 35.9 | 35 / 0.2 |
| louisville -1,1 | Full | 97 | 194 / 105 / 1.3 | 0 / 0.0 | 50 / 113,788 / 1.9 | 5.9 | 90.1 | 37 / 0.3 |
| louisville 1,-1 | Full | 36 | 85 / 50 / 0.6 | 0 / 0.0 | 46 / 97,801 / 1.3 | 2.0 | 33.4 | 35 / 0.3 |
| louisville -1,-1 | Full | 66 | 149 / 103 / 1.2 | 0 / 0.0 | 39 / 186,606 / 8.9 | 9.6 | 55.3 | 30 / 0.3 |

The Louisville block's NavMesh: 332 sources collected in 2 ms; baked at once (as one 384 m square) in 165 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 54 | 14 | 26% | streets 7.1 | streets 5.3 | 136,811 / 11,212 | 131 / 37 | 61 / 32 | 109 / 1 | 0 / 0 |
| rural-5km | 31 | 9 | 29% | snow tone 3.1 | snow tone 2.4 | 135,170 / 6,899 | 141 / 38 | 64 / 33 | 194 / 1 | 4 / 0 |
| louisville-downtown | 50 | 10 | 21% | interior plans 6.1 | buildings 2.4 | 131,780 / 10,503 | 212 / 36 | 50 / 31 | 165 / 1 | 0 / 0 |
| lexington-downtown | 47 | 12 | 26% | streets 6.7 | streets 5.4 | 121,885 / 7,969 | 139 / 32 | 57 / 27 | 95 / 1 | 2 / 0 |
| hazard | 19 | 7 | 36% | streets 3.2 | streets 2.0 | 64,430 / 6,261 | 88 / 32 | 44 / 27 | 152 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 60.94 | 30.47 | make 52.99 + switch on 7.94 ms; 1602 objects; destroy 3.12 ms; 3,000 rays 2.60 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 15.06 | 7.53 | make 10.52 + switch on 4.54 ms; 10 objects; destroy 1.50 ms; 3,000 rays 1.95 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.27 | 5.64 | make 3.76 + switch on 7.52 ms; 2 objects; destroy 0.09 ms; 3,000 rays 2.05 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.15 | 1.08 | make 2.12 + switch on 0.03 ms (+ 8.18 ms baking on the worker); 2 objects; destroy 0.08 ms; 3,000 rays 2.06 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.89 | 222.31 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 8.14 | 203.41 | objects 0.90, allocate 0.02, pack 7.09, apply 0.12 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.27 | 31.67 | objects 1.13, allocate 0.02, pack 7.44 (worker), apply 0.12 ms |
| primitives | Box, turned, flat colour | 20,000 | 54.66 | 2.73 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 64.44 | 3.22 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 58.46 | 2.92 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 90.22 | 4.51 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 40.20 | 2.01 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 28.31 | 1.42 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.43 | 0.32 | 80,000 vertices |
| primitives | Tri | 20,000 | 4.71 | 0.24 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 16.64 | 3.33 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 28.02 | 5.61 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 20.55 | 6.85 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 61.04 | 20.35 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 14.33 | 7.17 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.37 | 11.37 |  |
| objects | BoxCollider added to one object | 2,000 | 39.08 | 19.54 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 23.29 | 116.43 |  |
| objects | RoomLight.Spawn | 100 | 10.08 | 100.84 |  |
| objects | switching all of the above on | 1 | 27.01 | 27005.90 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 8.78 | 8774.80 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.59 | 9.64 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 1.96 | 195.66 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 368.24 | 368236.90 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 367.38 | 367378.60 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 20.14 | 20134.90 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,368 ms, 3,952,079 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (15) | 0.17 | 0.04 | 0.07 | 3.38 | 20.89 | 4 / 1 / 1 / 0 | snow tone 2.8 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,040 / 3,992,945 | 2 | 181 / 938 (pool 40) |
| drive-east | 2400 (305) | 0.51 | 0.05 | 4.02 | 5.28 | 12.04 | 39 / 9 / 0 / 0 | snow tone 11.6 | 40 (26 / 14) | 3 | 15 | 0.0 | 0.0 | 0 / 0 | 52 (10 Lite) / 3,212 / 6,009,193 | 2 | 184 / 1,026 (pool 39) |
| teleport-louisville | 720 (189) | 1.13 | 0.05 | 4.87 | 9.05 | 53.31 | 32 / 12 / 4 / 3 | interior plans 12.0 | 25 (9 / 16) | 0 | 28 | 0.2 | 2.4 | 10 / 145 | 50 (16 Lite) / 2,807 / 5,213,779 | 4 | 199 / 994 (pool 40) |
| drive-louisville | 1800 (200) | 0.46 | 0.05 | 3.34 | 5.51 | 34.90 | 25 / 11 / 3 / 1 | interior plans 34.8 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 37 | 55 (12 Lite) / 3,195 / 6,034,553 | 6 | 245 / 1,030 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: ground (1x, worst 0.2 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: snow tone (1x, worst 11.6 ms), buildings (3x, worst 10.3 ms), junctions (1x, worst 9.2 ms), woods (1x, worst 6.8 ms), signs (1x, worst 6.0 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (7x, worst 12.0 ms), driveway cars (1x, worst 8.7 ms), streets (1x, worst 6.5 ms), cutaway (1x, worst 4.9 ms), utility poles (1x, worst 4.3 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (4x, worst 34.8 ms), colliders (3x, worst 11.5 ms), fields (1x, worst 9.3 ms), landmarks (1x, worst 5.0 ms), utility poles (1x, worst 4.2 ms)

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.584 | 0% | 9,407 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.070 | 0% | 382 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.524 | 0% | 3,366 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Flaget Street, at the game's starting zoom | 0.073 | 0% | 4,009 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.451 | 0% | 66 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.075 | 0% | 63 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.426 | 0% | 64 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.087 | 0% | 63 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.409 | 0% | 64 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.072 | 0% | 81 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.587 | 0% | 63 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 49 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.348 | 0% | 62 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.072 | 0% | 56 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.430 | 0% | 3,309 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.075 | 0% | 4,107 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 87 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 70 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.265 | 0% | 82 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Flaget Street (downtown), looking along the street | 0.165 | 13% | 81 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.402 | 0% | 127 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.172 | 21% | 225 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.540 | 0% | 115 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.465 | 0% | 57 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 51 |

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
| setup | yes | 79 |  |
| square: KentuckyMap.Build | yes | 7,965 |  |
| square: count | yes | 977 |  |
| square: pick spots | yes | 92 |  |
| square: pictures | yes | 28,149 |  |
| square: discard | yes | 258 |  |
| square: passes (warm rebuild) | yes | 4,356 |  |
| chunks: stress test | yes | 1,108 |  |
| chunks: Louisville 3x3 | yes | 1,620 |  |
| chunks: determinism | yes | 197 |  |
| perf: Lite against Full | yes | 1,292 |  |
| perf: determinism across build orders | yes | 346 |  |
| perf: what things cost | yes | 1,628 |  |
| perf: walking and driving through the state | yes | 162,306 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
