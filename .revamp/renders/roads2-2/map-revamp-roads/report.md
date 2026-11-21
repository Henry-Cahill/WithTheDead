# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 0**: ok. Full run, 200.6 s, started 2026-10-24 10:39:44.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 6,558 ms |
| the same square again, warm, pass by pass | 3,959 ms |
| logged while building | 0 errors, 0 warnings, 0 edit-mode artefacts |
| spawn | 102.7, -33.0, facing -70° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 182 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 903 containers, 956 lights, 336 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 29,319 | 16,234 | 16,234 | 15,281 | 7,569,047 | 4,627,591 | 136 | 37,209 | 972 (0) | 8,401 | 11,708 | 903 |
| RaidOverhead | 588 | 470 | 470 | 470 | 465,154 | 273,661 | 134 | 1,905 | 0 (0) | 0 | 470 | 0 |
| RaidScenery | 117 | 116 | 116 | 116 | 81,168 | 104,710 | 43 | 340 | 0 (0) | 0 | 71 | 0 |
| **all** | 30,024 | 16,820 | 16,820 | 15,867 | 8,115,369 | 5,005,962 | 241 | 39,454 | 972 (0) | 8,401 | 12,249 | 903 |

Where the renderers come from: the roots' children grouped by name (digits folded to #), most material slots (about one draw call each) first:

| group | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World/Interior KY# | 27,579 | 15,793 | 15,793 | 14,862 | 6,322,098 | 3,984,553 | 82 | 34,799 | 932 (0) | 6,120 | 11,435 | 811 |
| World/Building shells | 131 | 130 | 130 | 130 | 815,118 | 409,876 | 48 | 1,221 | 0 (0) | 0 | 130 | 0 |
| RaidOverhead/Building shells | 131 | 130 | 130 | 130 | 327,105 | 161,997 | 119 | 982 | 0 (0) | 0 | 130 | 0 |
| World/OutdoorProps | 143 | 143 | 143 | 143 | 302,850 | 175,616 | 18 | 618 | 0 (0) | 0 | 143 | 0 |
| World/OutdoorGround | 142 | 142 | 142 | 142 | 127,120 | 54,694 | 13 | 545 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Roofs | 133 | 133 | 133 | 133 | 45,487 | 26,615 | 46 | 447 | 0 (0) | 0 | 133 | 0 |
| RaidOverhead/Interior KY# | 232 | 116 | 116 | 116 | 86,703 | 76,716 | 5 | 385 | 0 (0) | 0 | 116 | 0 |
| RaidScenery/SceneryProps | 43 | 43 | 43 | 43 | 37,298 | 22,220 | 36 | 238 | 0 (0) | 0 | 43 | 0 |
| RaidScenery/SceneryGround | 43 | 43 | 43 | 43 | 1,228 | 426 | 5 | 72 | 0 (0) | 0 | 0 | 0 |
| RaidOverhead/Overhead | 66 | 66 | 66 | 66 | 1,347 | 449 | 1 | 66 | 0 (0) | 0 | 66 | 0 |
| RaidScenery/Wires | 28 | 28 | 28 | 28 | 2,352 | 4,176 | 1 | 28 | 0 (0) | 0 | 28 | 0 |
| World/StreetLamp | 50 | 25 | 25 | 3 | 1,837 | 2,840 | 3 | 25 | 24 (0) | 25 | 0 | 0 |
| RaidOverhead/Wires | 25 | 25 | 25 | 25 | 4,512 | 7,884 | 1 | 25 | 0 (0) | 0 | 25 | 0 |
| World/Ground | 1 | 1 | 1 | 1 | 24 | 12 | 1 | 1 | 0 (0) | 1 | 0 | 0 |
| RaidScenery/Snowfield | 1 | 1 | 1 | 1 | 4 | 2 | 1 | 1 | 0 (0) | 0 | 0 | 0 |
| RaidScenery/SnowTone | 1 | 1 | 1 | 1 | 40,286 | 77,886 | 1 | 1 | 0 (0) | 0 | 0 | 0 |

### Meshes that aren't finite numbers

None: every one of the 21,114 meshes checked (the square, the test chunks, what stood at the end of the streamed walks) has finite positions, normals and bounds, and RaidMesh baked no NaN or infinite vertex.

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 79 | 2% | 1 | 79 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| lots | 3 | 0% | 1 | 3 |
| cemeteries | 4 | 0% | 4 | 1 |
| grounds | 0 | 0% | 1 | 0 |
| work yards | 1 | 0% | 1 | 1 |
| snow tone | 50 | 1% | 1 | 50 |
| road network | 11 | 0% | 1 | 11 |
| junctions | 5 | 0% | 1 | 5 |
| streets | 28 | 1% | 67 | 19 |
| sidewalks | 24 | 1% | 67 | 4 |
| berms and driveways | 14 | 0% | 62 | 1 |
| road markings | 1 | 0% | 10 | 0 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 108 | 3% | 42 | 6 |
| interior plans | 167 | 4% | 117 | 6 |
| interior shell | 13 | 0% | 116 | 0 |
| interior walls | 172 | 4% | 116 | 31 |
| interior floors | 14 | 0% | 116 | 0 |
| interior doors | 454 | 11% | 232 | 13 |
| interior switches | 64 | 2% | 116 | 1 |
| interior rooms | 2,234 | 56% | 697 | 56 |
| interior wall meshes | 85 | 2% | 116 | 3 |
| interior finish | 184 | 5% | 116 | 6 |
| cutaway | 45 | 1% | 21 | 2 |
| utility lines | 8 | 0% | 1 | 8 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 14 | 0% | 1 | 14 |
| street lights | 4 | 0% | 1 | 4 |
| signs | 4 | 0% | 1 | 4 |
| street furniture | 14 | 0% | 1 | 14 |
| landmarks | 1 | 0% | 1 | 1 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 0 | 0% | 1 | 0 |
| wires | 6 | 0% | 1 | 6 |
| nature plan | 1 | 0% | 1 | 1 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 0% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 4 | 0% | 9370 | 0 |
| driveway cars | 3 | 0% | 30 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 45 | 0 |
| kerbside cars | 13 | 0% | 204 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 25 | 1% | 117 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 1 | 0% | 1 | 1 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (after the last pass) | 12 | 0% | 1 | 12 |
| (emit the overhead meshes) | 9 | 0% | 1 | 9 |
| (emit the outdoor meshes) | 51 | 1% | 1 | 51 |
| **total** | **3,959** | | | |

### Logged during the pass-by-pass build: 0 errors, 0 warnings, 0 edit-mode artefacts

(nothing)

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **121** | data 34 | 0 | 130 | 61 | 131,788 | 68,494 | 0 | 109 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **46** | buildings 9 | 1 | 144 | 64 | 131,989 | 75,290 | 4 | 199 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 148 + 10 | **88** | cutaway 10 | 1 | 213 | 49 | 136,352 | 72,450 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 490 + 54 | **138** | buildings 66 | 0 | 134 | 57 | 128,534 | 67,580 | 1 | 98 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 7 + 4 | **24** | streets 5 | 0 | 85 | 43 | 59,190 | 42,900 | 0 | 155 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 33.5 | 1 | 33.5 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 0.0 | 1 | 0.0 |
| snow tone | 1.4 | 1 | 1.4 |
| road network | 1.5 | 1 | 1.5 |
| junctions | 1.1 | 1 | 1.1 |
| streets | 7.1 | 1 | 7.1 |
| sidewalks | 3.0 | 2 | 2.1 |
| berms and driveways | 1.3 | 1 | 1.3 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 1.3 | 1 | 1.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 25.9 | 8 | 6.3 |
| interior plans | 15.9 | 7 | 3.1 |
| cutaway | 4.8 | 3 | 2.2 |
| utility lines | 1.8 | 1 | 1.8 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.6 | 1 | 0.6 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 5.9 | 1 | 5.9 |
| street furniture | 0.7 | 1 | 0.7 |
| landmarks | 0.3 | 1 | 0.3 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.6 | 1 | 0.6 |
| nature plan | 2.2 | 1 | 2.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.4 | 1 | 0.4 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.2 | 1 | 0.2 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.3 | 4 | 0.2 |
| driveway cars | 0.3 | 1 | 0.3 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.7 | 1 | 0.7 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.8 | 1 | 1.8 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| colliders | 1.6 | 1 | 1.6 |
| objects | 3.2 | 1 | 3.2 |
| meshes | 1.8 | 1 | 1.8 |

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
| snow tone | 1.4 | 1 | 1.4 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 1.9 | 1 | 1.9 |
| sidewalks | 0.8 | 1 | 0.8 |
| berms and driveways | 0.8 | 1 | 0.8 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 12.4 | 3 | 9.5 |
| interior plans | 11.3 | 5 | 2.7 |
| cutaway | 4.3 | 3 | 2.1 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.0 | 1 | 1.0 |
| street furniture | 0.5 | 1 | 0.5 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| wires | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 3.1 | 2 | 2.2 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.8 | 1 | 0.8 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 1.5 | 4 | 0.5 |
| driveway cars | 0.5 | 1 | 0.5 |
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
| colliders | 1.6 | 1 | 1.6 |
| objects | 1.1 | 1 | 1.1 |
| meshes | 0.4 | 1 | 0.4 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 2.4 | 1 | 2.4 |
| water | 2.7 | 1 | 2.7 |
| fields | 0.1 | 1 | 0.1 |
| lots | 0.5 | 1 | 0.5 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.2 | 1 | 1.2 |
| snow tone | 1.5 | 1 | 1.5 |
| road network | 0.7 | 1 | 0.7 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.1 | 1 | 1.1 |
| sidewalks | 1.3 | 1 | 1.3 |
| berms and driveways | 1.2 | 1 | 1.2 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 4.9 | 1 | 4.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 18.4 | 6 | 7.3 |
| interior plans | 23.9 | 6 | 8.3 |
| cutaway | 13.9 | 3 | 9.7 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 1.5 | 1 | 1.5 |
| wires | 0.0 | 1 | 0.0 |
| nature plan | 0.6 | 1 | 0.6 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.5 | 1 | 0.5 |
| yard trees | 0.1 | 1 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 4 | 0.1 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.4 | 1 | 0.4 |
| kerbside cars | 2.8 | 2 | 2.3 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.4 | 2 | 2.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.0 | 1 | 0.0 |
| colliders | 1.5 | 1 | 1.5 |
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.3 | 1 | 0.3 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.6 | 1 | 0.6 |
| ground | 1.1 | 1 | 1.1 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| lots | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| work yards | 1.1 | 1 | 1.1 |
| snow tone | 1.7 | 1 | 1.7 |
| road network | 0.7 | 1 | 0.7 |
| junctions | 1.1 | 1 | 1.1 |
| streets | 8.4 | 2 | 6.3 |
| sidewalks | 4.8 | 3 | 2.1 |
| berms and driveways | 2.0 | 1 | 2.0 |
| road markings | 0.4 | 1 | 0.4 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 77.8 | 6 | 66.2 |
| interior plans | 16.4 | 5 | 5.2 |
| cutaway | 5.6 | 3 | 2.8 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.4 | 1 | 1.4 |
| street lights | 0.4 | 1 | 0.4 |
| signs | 1.7 | 1 | 1.7 |
| street furniture | 1.6 | 1 | 1.6 |
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
| woods | 0.5 | 4 | 0.3 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 1.4 | 1 | 1.4 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 2.3 | 2 | 2.3 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.2 | 1 | 1.2 |
| colliders | 1.0 | 1 | 1.0 |
| objects | 1.0 | 1 | 1.0 |
| meshes | 0.3 | 1 | 0.3 |

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
| snow tone | 1.6 | 1 | 1.6 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.8 | 1 | 0.8 |
| streets | 5.1 | 1 | 5.1 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 1.7 | 1 | 1.7 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.5 | 1 | 0.5 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.5 | 1 | 1.5 |
| street furniture | 0.1 | 1 | 0.1 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.1 | 1 | 0.1 |
| wires | 0.9 | 1 | 0.9 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.1 | 1 | 0.1 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.8 | 1 | 0.8 |
| woods | 4.9 | 5 | 2.0 |
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
| objects | 0.9 | 1 | 0.9 |
| meshes | 0.4 | 1 | 0.4 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 61 | 11 |
| 186_141 | yes | 11 | 2 |
| 185_141 | yes | 8 | 2 |
| 185_142 | yes | 12 | 2 |
| 183_148 | yes | 148 | 10 |
| 196_145 | yes | 490 | 54 |
| 210_134 | yes | 7 | 4 |
| 182_148 | yes | 154 | 14 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 526 ms (slowest 99 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **48** | interior plans 6 | 0 | 213 | 49 | 136,352 | 72,450 | 0 | 166 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **61** | interior plans 8 | 1 | 192 | 75 | 145,177 | 79,574 | 0 | 201 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 154 + 14 | **54** | interior plans 8 | 1 | 238 | 57 | 107,873 | 59,741 | 2 | 192 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **71** | interior plans 30 | 0 | 163 | 58 | 120,738 | 63,559 | 0 | 135 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **53** | buildings 9 | 0 | 171 | 60 | 85,721 | 49,014 | 1 | 122 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **39** | interior plans 9 | 0 | 151 | 53 | 111,651 | 64,504 | 0 | 121 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **99** | buildings 49 | 1 | 183 | 53 | 115,758 | 62,382 | 0 | 196 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **35** | interior plans 5 | 0 | 134 | 54 | 97,155 | 57,113 | 3 | 81 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **65** | interior plans 16 | 0 | 169 | 41 | 187,428 | 103,083 | 0 | 150 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 1,616 | 500 | 500 | 497 | 1,107,853 | 611,420 | 133 | 2,147 | 6 (0) | 1,364 | 341 | 76 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (131,788 vertices, 68,494 triangles, 130 objects, 61 meshes; hash 796a94feb9dbd4e2 vs 796a94feb9dbd4e2; 111 colliders, lights and loot containers, hash 0c6b92ac278e0240 vs 0c6b92ac278e0240). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 121 | 108 / 54 / 1.5 | 0 / 0.0 | 59 / 129,114 / 4.0 | 5.6 | 114.3 | 41 / 4.0 |
| rural-5km | Full | 46 | 198 / 67 / 1.6 | 0 / 0.0 | 63 / 129,390 / 3.0 | 5.0 | 39.8 | 43 / 0.4 |
| louisville-downtown | Full | 88 | 165 / 120 / 1.5 | 0 / 0.0 | 48 / 133,759 / 12.7 | 14.3 | 72.2 | 36 / 1.0 |
| lexington-downtown | Full | 138 | 96 / 61 / 1.0 | 0 / 0.0 | 53 / 125,805 / 4.9 | 6.3 | 130.2 | 39 / 0.4 |
| hazard | Full | 24 | 154 / 24 / 0.6 | 0 / 0.0 | 38 / 55,812 / 0.3 | 0.8 | 22.8 | 30 / 0.2 |
| louisville 0,0 | Full | 48 | 165 / 120 / 1.3 | 0 / 0.0 | 48 / 133,759 / 2.4 | 3.3 | 43.7 | 36 / 0.3 |
| louisville 1,0 | Full | 61 | 200 / 109 / 1.6 | 0 / 0.0 | 73 / 142,508 / 3.1 | 5.0 | 54.1 | 46 / 0.3 |
| louisville -1,0 | Full | 54 | 189 / 134 / 1.6 | 0 / 0.0 | 50 / 104,921 / 1.5 | 2.3 | 50.4 | 39 / 0.3 |
| louisville 0,1 | Full | 71 | 134 / 75 / 0.9 | 0 / 0.0 | 54 / 118,264 / 2.2 | 3.4 | 67.0 | 36 / 0.3 |
| louisville 0,-1 | Full | 53 | 121 / 70 / 1.0 | 0 / 0.0 | 55 / 82,444 / 1.0 | 2.0 | 50.2 | 37 / 0.3 |
| louisville 1,1 | Full | 39 | 120 / 74 / 0.9 | 0 / 0.0 | 50 / 108,580 / 1.7 | 2.7 | 35.8 | 35 / 0.3 |
| louisville -1,1 | Full | 99 | 195 / 104 / 2.0 | 0 / 0.0 | 50 / 112,626 / 2.3 | 3.6 | 93.2 | 37 / 0.3 |
| louisville 1,-1 | Full | 35 | 77 / 42 / 0.5 | 0 / 0.0 | 46 / 92,901 / 1.0 | 1.9 | 32.3 | 35 / 0.3 |
| louisville -1,-1 | Full | 65 | 149 / 103 / 1.2 | 0 / 0.0 | 39 / 184,798 / 3.3 | 4.1 | 59.8 | 30 / 0.3 |

The Louisville block's NavMesh: 331 sources collected in 2 ms; baked at once (as one 384 m square) in 183 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, three times each in turn; the middle run of each is shown (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 52 | 14 | 27% | snow tone 9.6 | streets 5.6 | 131,788 / 10,642 | 130 / 37 | 61 / 32 | 109 / 1 | 0 / 0 |
| rural-5km | 28 | 6 | 22% | interior plans 2.5 | streets 1.4 | 131,831 / 6,898 | 141 / 38 | 64 / 33 | 194 / 1 | 4 / 0 |
| louisville-downtown | 48 | 11 | 23% | interior plans 6.2 | paths 2.5 | 129,202 / 10,503 | 211 / 36 | 49 / 31 | 165 / 1 | 0 / 0 |
| lexington-downtown | 42 | 12 | 28% | streets 6.4 | streets 4.9 | 120,974 / 7,782 | 139 / 32 | 57 / 27 | 94 / 1 | 2 / 0 |
| hazard | 21 | 7 | 34% | streets 4.0 | streets 2.7 | 66,475 / 6,304 | 93 / 32 | 44 / 27 | 157 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 81,034, normals 81,034, UVs 81,034; largest difference 0 m, 0 (normals), 0 (UVs); triangles same, materials same |
| colliders | an object per turned box (the procedural town's way) | 2,000 | 58.63 | 29.31 | make 50.78 + switch on 7.85 ms; 1602 objects; destroy 2.97 ms; 3,000 rays 2.66 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle (the Kentucky map's way) | 2,000 | 14.64 | 7.32 | make 9.86 + switch on 4.78 ms; 10 objects; destroy 1.20 ms; 3,000 rays 1.75 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 11.25 | 5.62 | make 3.72 + switch on 7.52 ms; 2 objects; destroy 0.10 ms; 3,000 rays 1.93 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.15 | 1.08 | make 2.12 + switch on 0.03 ms (+ 8.03 ms baking on the worker); 2 objects; destroy 0.06 ms; 3,000 rays 1.92 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 8.65 | 216.22 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 7.77 | 194.31 | objects 0.77, allocate 0.02, pack 6.89, apply 0.09 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.03 | 25.85 | objects 0.90, allocate 0.02, pack 7.20 (worker), apply 0.12 ms |
| primitives | Box, turned, flat colour | 20,000 | 27.99 | 1.40 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 84.29 | 4.22 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 61.21 | 3.06 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 83.73 | 4.19 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 40.61 | 2.03 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 27.21 | 1.36 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 6.55 | 0.33 | 80,000 vertices |
| primitives | Tri | 20,000 | 5.43 | 0.27 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 15.54 | 3.11 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 33.52 | 6.71 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 39.05 | 13.02 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 38.46 | 12.82 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 13.54 | 6.77 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.16 | 11.16 |  |
| objects | BoxCollider added to one object | 2,000 | 39.40 | 19.70 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 23.56 | 117.82 |  |
| objects | RoomLight.Spawn | 100 | 10.03 | 100.28 |  |
| objects | switching all of the above on | 1 | 27.87 | 27869.60 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 11.32 | 11324.30 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.60 | 9.86 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.03 | 202.84 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 372.00 | 371995.50 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 378.45 | 378449.80 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 23.50 | 23497.60 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,312 ms, 3,871,128 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (13) | 0.14 | 0.05 | 0.07 | 4.43 | 10.39 | 2 / 1 / 0 / 0 | landmarks 2.7 | 5 (0 / 5) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 (5 Lite) / 2,045 / 3,914,860 | 2 | 182 / 934 (pool 40) |
| drive-east | 2400 (249) | 0.44 | 0.05 | 3.71 | 5.40 | 11.28 | 33 / 5 / 0 / 0 | interior plans 10.0 | 38 (24 / 14) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 53 (10 Lite) / 3,066 / 5,561,964 | 2 | 184 / 1,007 (pool 40) |
| teleport-louisville | 720 (135) | 1.10 | 0.05 | 6.44 | 10.27 | 48.28 | 53 / 17 / 4 / 3 | interior plans 10.5 | 25 (9 / 16) | 0 | 29 | 0.2 | 1.5 | 11 / 87 | 50 (16 Lite) / 2,799 / 5,126,512 | 4 | 201 / 989 (pool 40) |
| drive-louisville | 1800 (213) | 0.52 | 0.05 | 3.60 | 7.36 | 35.94 | 37 / 15 / 3 / 1 | interior plans 35.9 | 18 (12 / 6) | 3 | 10 | 0.0 | 0.0 | 0 / 26 | 55 (12 Lite) / 3,194 / 5,900,884 | 6 | 247 / 1,024 (pool 40) |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: snow tone (1x, worst 1.3 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: interior plans (1x, worst 10.0 ms), buildings (2x, worst 8.8 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: interior plans (10x, worst 10.5 ms), snow tone (2x, worst 8.1 ms), utility poles (1x, worst 5.8 ms), cutaway (1x, worst 4.5 ms), streets (1x, worst 4.3 ms), paths (1x, worst 4.3 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: interior plans (3x, worst 35.9 ms), junctions (1x, worst 16.6 ms), snow tone (3x, worst 13.9 ms), colliders (1x, worst 13.1 ms), (swap) (1x, worst 12.4 ms), buildings (1x, worst 8.1 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 38 edit-mode artefacts

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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.594 | 0% | 6,878 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 387 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.488 | 0% | 123 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on East Stephen Foster Avenue, at the game's starting zoom | 0.116 | 0% | 101 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.433 | 0% | 2,323 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.074 | 0% | 2,938 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.472 | 0% | 103 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.086 | 0% | 60 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.370 | 0% | 70 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.072 | 0% | 64 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.586 | 0% | 84 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.078 | 0% | 63 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.350 | 0% | 55 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.075 | 0% | 59 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.411 | 0% | 2,773 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.073 | 0% | 2,742 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 59 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.075 | 0% | 53 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.456 | 0% | 123 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on East Stephen Foster Avenue (downtown), looking along the street | 0.164 | 20% | 147 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.434 | 0% | 123 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.165 | 20% | 122 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.542 | 0% | 105 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.465 | 0% | 57 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.079 | 0% | 53 |

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

### Everything logged during the run: 0 errors, 0 warnings, 38 edit-mode artefacts

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
| setup | yes | 67 |  |
| square: KentuckyMap.Build | yes | 6,561 |  |
| square: count | yes | 890 |  |
| square: pick spots | yes | 110 |  |
| square: pictures | yes | 21,719 |  |
| square: discard | yes | 237 |  |
| square: passes (warm rebuild) | yes | 4,241 |  |
| chunks: stress test | yes | 1,327 |  |
| chunks: Louisville 3x3 | yes | 1,619 |  |
| chunks: determinism | yes | 158 |  |
| perf: Lite against Full | yes | 1,230 |  |
| perf: determinism across build orders | yes | 334 |  |
| perf: what things cost | yes | 1,632 |  |
| perf: walking and driving through the state | yes | 160,450 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
