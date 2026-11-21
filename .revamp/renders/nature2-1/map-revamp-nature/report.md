# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 2**: 8 error(s) logged or thrown by the map code while building (see the logs below). Full run, 196.4 s, started 2026-10-24 09:53:24.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 5,277 ms |
| the same square again, warm, pass by pass | 1,837 ms |
| logged while building | 4 errors, 116 warnings, 232 edit-mode artefacts |
| spawn | -84.2, 13.8, facing 104° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 66 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 490 containers, 21 lights, 336 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 15,062 | 5,531 | 5,531 | 4,627 | 3,258,788 | 1,934,814 | 121 | 12,006 | 247 (0) | 5,236 | 4,162 | 1,817 |
| RaidOverhead | 457 | 339 | 339 | 339 | 323,687 | 166,411 | 128 | 1,350 | 0 (0) | 0 | 339 | 0 |
| RaidScenery | 119 | 118 | 118 | 118 | 84,505 | 83,920 | 38 | 337 | 0 (0) | 0 | 73 | 0 |
| **all** | 15,638 | 5,988 | 5,988 | 5,084 | 3,666,980 | 2,185,145 | 218 | 13,693 | 247 (0) | 5,236 | 4,574 | 1,817 |

### Passes (a warm rebuild of the same square, each step of `KentuckyMap.Passes` timed)

| step | ms | share | runs | slowest run ms |
|---|--:|--:|--:|--:|
| (read the area) | 62 | 3% | 1 | 62 |
| (set up the site) | 0 | 0% | 1 | 0 |
| ground | 0 | 0% | 1 | 0 |
| water | 0 | 0% | 1 | 0 |
| fields | 0 | 0% | 1 | 0 |
| parking | 36 | 2% | 1 | 36 |
| cemeteries | 4 | 0% | 4 | 1 |
| grounds | 0 | 0% | 1 | 0 |
| yards | 1 | 0% | 1 | 1 |
| snow tone | 11 | 1% | 1 | 11 |
| road network | 4 | 0% | 1 | 4 |
| junctions | 1 | 0% | 1 | 1 |
| streets | 39 | 2% | 1 | 39 |
| sidewalks | 29 | 2% | 1 | 29 |
| berms and driveways | 10 | 1% | 1 | 10 |
| road markings | 1 | 0% | 1 | 1 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 117 | 6% | 21 | 24 |
| interior plans | 147 | 8% | 7 | 37 |
| interior shell | 594 | 32% | 116 | 22 |
| interior walls | 119 | 6% | 116 | 7 |
| interior doors | 451 | 25% | 116 | 17 |
| cutaway | 52 | 3% | 15 | 8 |
| utility lines | 12 | 1% | 1 | 12 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 5 | 0% | 1 | 5 |
| street lights | 3 | 0% | 1 | 3 |
| signs | 2 | 0% | 1 | 2 |
| street furniture | 12 | 1% | 1 | 12 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 7 | 0% | 1 | 7 |
| nature plan | 7 | 0% | 1 | 7 |
| driveway plan | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| yard trees | 10 | 1% | 163 | 0 |
| grounds trees | 1 | 0% | 5 | 0 |
| street trees | 3 | 0% | 118 | 0 |
| fence rows | 0 | 0% | 11 | 0 |
| woods | 3 | 0% | 9370 | 0 |
| driveway cars | 2 | 0% | 30 | 1 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| parking lot cars | 3 | 0% | 46 | 1 |
| kerbside cars | 10 | 1% | 173 | 0 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 23 | 1% | 117 | 1 |
| fence drifts | 0 | 0% | 1 | 0 |
| shovelled snow | 0 | 0% | 1 | 0 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (emit the overhead meshes) | 9 | 1% | 1 | 9 |
| (emit the outdoor meshes) | 33 | 2% | 1 | 33 |
| **total** | **1,837** | | | |

### Logged during the pass-by-pass build: 4 errors, 116 warnings, 232 edit-mode artefacts

- `Warning: [CloneZ] Interiors: furnishing building 124090468 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY124090468: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY124090468: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 124092542 (Convenience) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY124092542: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY124092542: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 281586026 (Pharmacy) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY281586026: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY281586026: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1843815690 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1843815690: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1843815690: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1915619802 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1915619802: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1915619802: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 2239973240 (Workshop) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY2239973240: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY2239973240: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 2239973242 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY2239973242: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Streamed chunks (128 m, built the way `KentuckyStreamer` does: `ChunkJob` + `ChunkSteps`, every step timed, back to back)

Region read/index is what the streamer does off the main thread (0 when an earlier chunk already read it). Total = every step on the main thread; the streamer spreads them over frames at 5 ms a frame (40 ms while catching up).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **52** | cutaway 12 | 0 | 95 | 68 | 92,900 | 49,211 | 3 | 69 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **46** | buildings 11 | 0 | 90 | 64 | 130,196 | 74,490 | 5 | 115 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 128 + 9 | **104** | cutaway 31 | 0 | 89 | 53 | 172,842 | 90,225 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 411 + 35 | **65** | buildings 18 | 0 | 81 | 54 | 87,338 | 46,329 | 0 | 40 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **20** | streets 3 | 0 | 65 | 41 | 55,973 | 41,851 | 0 | 2 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 1.7 | 1 | 1.7 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 1 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.3 | 1 | 1.3 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 7.6 | 1 | 7.6 |
| sidewalks | 1.6 | 1 | 1.6 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.1 | 1 | 0.1 |
| paths | 0.9 | 1 | 0.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 4.4 | 2 | 3.9 |
| cutaway | 12.3 | 2 | 12.2 |
| utility lines | 0.7 | 1 | 0.7 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.5 | 1 | 0.5 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 1.2 | 1 | 1.2 |
| street furniture | 0.4 | 1 | 0.4 |
| landmarks | 0.2 | 1 | 0.2 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 3 | 0.0 |
| grounds trees | 0.1 | 2 | 0.1 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.1 | 461 | 0.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 0.4 | 10 | 0.3 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.5 | 9 | 0.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 12.0 | 1 | 12.0 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.2 | 1 | 0.2 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 1.6 | 1 | 1.6 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 1.8 | 1 | 1.8 |
| sidewalks | 0.3 | 1 | 0.3 |
| berms and driveways | 0.5 | 1 | 0.5 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.9 | 2 | 10.6 |
| cutaway | 10.8 | 2 | 9.8 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.0 | 1 | 0.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| nature plan | 0.1 | 1 | 0.1 |
| driveway plan | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 5.5 | 17 | 3.2 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.7 | 20 | 0.2 |
| fence rows | 0.0 | 9 | 0.0 |
| woods | 1.3 | 458 | 1.0 |
| driveway cars | 2.3 | 9 | 0.9 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 4.4 | 6 | 4.3 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.6 | 13 | 0.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.4 | 1 | 0.4 |
| outdoor | 1.8 | 1 | 1.8 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.4 | 1 | 0.4 |
| ground | 11.0 | 1 | 11.0 |
| water | 13.6 | 1 | 13.6 |
| fields | 0.1 | 1 | 0.1 |
| parking | 0.6 | 1 | 0.6 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 1.1 | 1 | 1.1 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 0.5 | 1 | 0.5 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 1.0 | 1 | 1.0 |
| sidewalks | 0.9 | 1 | 0.9 |
| berms and driveways | 0.7 | 1 | 0.7 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 4.3 | 1 | 4.3 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 12.7 | 2 | 8.5 |
| cutaway | 31.4 | 1 | 31.4 |
| utility lines | 0.5 | 1 | 0.5 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.6 | 1 | 0.6 |
| signs | 0.1 | 1 | 0.1 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 4.5 | 1 | 4.5 |
| nature plan | 1.8 | 1 | 1.8 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 2.4 | 4 | 1.4 |
| yard trees | 0.1 | 3 | 0.1 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.1 | 1 | 0.1 |
| fence rows | 0.0 | 1 | 0.0 |
| woods | 0.2 | 453 | 0.1 |
| driveway cars | 0.1 | 1 | 0.1 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.6 | 1 | 0.6 |
| kerbside cars | 4.0 | 15 | 1.3 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 4.0 | 7 | 3.1 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.7 | 1 | 0.7 |
| outdoor | 2.9 | 1 | 2.9 |

</details>

<details><summary>lexington-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.3 | 1 | 0.3 |
| ground | 1.6 | 1 | 1.6 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 8.7 | 1 | 8.7 |
| snow tone | 1.7 | 1 | 1.7 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.2 | 1 | 0.2 |
| streets | 6.7 | 1 | 6.7 |
| sidewalks | 2.0 | 1 | 2.0 |
| berms and driveways | 1.0 | 1 | 1.0 |
| road markings | 0.6 | 1 | 0.6 |
| paths | 0.7 | 1 | 0.7 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 18.5 | 1 | 18.5 |
| cutaway | 12.6 | 1 | 12.6 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.6 | 1 | 0.6 |
| street furniture | 0.7 | 1 | 0.7 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.6 | 1 | 0.6 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 7 | 0.0 |
| woods | 0.3 | 456 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.0 | 1 | 0.0 |
| kerbside cars | 1.0 | 23 | 0.2 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.8 | 7 | 0.5 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.0 | 1 | 1.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 1.8 | 1 | 1.8 |

</details>

<details><summary>hazard: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.3 | 1 | 0.3 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 1.8 | 1 | 1.8 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 1 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.1 | 1 | 0.1 |
| streets | 3.5 | 1 | 3.5 |
| sidewalks | 0.0 | 1 | 0.0 |
| berms and driveways | 0.9 | 1 | 0.9 |
| road markings | 0.3 | 1 | 0.3 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.1 | 1 | 0.1 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.2 | 1 | 0.2 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.6 | 1 | 0.6 |
| nature plan | 0.2 | 1 | 0.2 |
| driveway plan | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| yard trees | 0.0 | 1 | 0.0 |
| grounds trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 46 | 0.2 |
| woods | 4.0 | 462 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| parking lot cars | 0.9 | 12 | 0.3 |
| kerbside cars | 0.1 | 7 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| shovelled snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.2 | 1 | 0.2 |
| outdoor | 2.1 | 1 | 2.1 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 59 | 8 |
| 186_141 | yes | 16 | 6 |
| 185_141 | yes | 7 | 2 |
| 185_142 | yes | 10 | 2 |
| 183_148 | yes | 128 | 9 |
| 196_145 | yes | 411 | 35 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 145 | 13 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 707 ms (slowest 120 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **62** | cutaway 24 | 0 | 89 | 53 | 172,842 | 90,225 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **104** | interior plans 26 | 0 | 101 | 75 | 238,431 | 125,075 | 0 | 129 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 145 + 13 | **64** | cutaway 18 | 0 | 101 | 56 | 98,214 | 54,559 | 0 | 66 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **79** | cutaway 29 | 0 | 87 | 53 | 248,648 | 126,573 | 0 | 83 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **120** | fences and walls 33 | 0 | 107 | 61 | 237,891 | 123,841 | 1 | 47 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **74** | cutaway 28 | 0 | 82 | 52 | 185,728 | 99,477 | 0 | 55 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **61** | buildings 14 | 0 | 81 | 54 | 146,801 | 77,046 | 0 | 49 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **38** | cutaway 7 | 0 | 86 | 44 | 89,583 | 50,636 | 0 | 14 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **104** | cutaway 59 | 0 | 75 | 46 | 205,239 | 110,821 | 0 | 44 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 811 | 494 | 494 | 494 | 1,623,377 | 858,253 | 125 | 1,929 | 1 (0) | 552 | 340 | 66 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (92,900 vertices, 49,211 triangles, 95 objects, 68 meshes; hash 4cf2bfd3d74c9276 vs 4cf2bfd3d74c9276; 74 colliders, lights and loot containers, hash aedabd6433a8ac31 vs aedabd6433a8ac31). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 52 | 34 / 1 / 10.1 | 0 / 0.0 | 61 / 89,969 / 2.1 | 3.9 | 38.3 | 44 / 3.3 |
| rural-5km | Full | 46 | 85 / 1 / 0.1 | 0 / 0.0 | 63 / 127,596 / 2.5 | 4.3 | 41.9 | 43 / 0.2 |
| louisville-downtown | Full | 104 | 89 / 1 / 0.3 | 0 / 0.0 | 51 / 170,226 / 3.8 | 10.2 | 94.0 | 36 / 0.9 |
| lexington-downtown | Full | 65 | 48 / 1 / 0.1 | 0 / 0.0 | 51 / 84,684 / 2.1 | 3.5 | 61.3 | 38 / 0.2 |
| hazard | Full | 20 | 150 / 1 / 0.1 | 0 / 0.0 | 36 / 52,544 / 1.1 | 2.0 | 17.4 | 30 / 0.2 |
| louisville 0,0 | Full | 62 | 89 / 1 / 0.1 | 0 / 0.0 | 51 / 170,226 / 3.1 | 4.6 | 57.4 | 36 / 0.2 |
| louisville 1,0 | Full | 104 | 69 / 1 / 0.1 | 0 / 0.0 | 73 / 235,761 / 5.7 | 7.8 | 95.8 | 46 / 0.3 |
| louisville -1,0 | Full | 64 | 110 / 1 / 0.1 | 0 / 0.0 | 50 / 95,137 / 2.1 | 3.5 | 60.5 | 37 / 0.2 |
| louisville 0,1 | Full | 79 | 37 / 1 / 0.1 | 0 / 0.0 | 50 / 245,985 / 5.4 | 7.0 | 72.3 | 36 / 0.2 |
| louisville 0,-1 | Full | 120 | 62 / 1 / 0.2 | 0 / 0.0 | 57 / 235,077 / 4.6 | 6.6 | 113.7 | 37 / 0.5 |
| louisville 1,1 | Full | 74 | 59 / 1 / 0.1 | 0 / 0.0 | 49 / 183,085 / 3.7 | 5.2 | 69.2 | 35 / 0.2 |
| louisville -1,1 | Full | 61 | 88 / 1 / 0.1 | 0 / 0.0 | 51 / 144,090 / 3.1 | 4.5 | 56.8 | 37 / 0.2 |
| louisville 1,-1 | Full | 38 | 49 / 1 / 0.1 | 0 / 0.0 | 41 / 86,915 / 2.2 | 3.3 | 34.8 | 32 / 0.2 |
| louisville -1,-1 | Full | 104 | 99 / 1 / 0.1 | 0 / 0.0 | 42 / 202,567 / 4.0 | 5.3 | 98.2 | 30 / 0.2 |

The Louisville block's NavMesh: 326 sources collected in 2 ms; baked at once (as one 384 m square) in 234 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, back to back (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 39 | 17 | 44% | cutaway 12.1 | streets 7.6 | 92,900 / 11,329 | 95 / 37 | 68 / 32 | 69 / 1 | 3 / 0 |
| rural-5km | 33 | 7 | 22% | cutaway 11.7 | streets 1.4 | 129,867 / 7,047 | 90 / 38 | 64 / 33 | 115 / 1 | 5 / 0 |
| louisville-downtown | 82 | 12 | 15% | cutaway 28.2 | buildings 3.6 | 188,820 / 14,729 | 99 / 36 | 53 / 31 | 65 / 1 | 0 / 0 |
| lexington-downtown | 57 | 13 | 23% | cutaway 16.7 | streets 6.2 | 122,979 / 11,636 | 80 / 31 | 53 / 26 | 42 / 1 | 0 / 0 |
| hazard | 21 | 8 | 36% | streets 3.5 | streets 3.0 | 60,755 / 6,459 | 56 / 33 | 42 / 28 | 2 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 73,873, normals 69,223, UVs 78,946; largest difference 3.93E-06 m, 1.2E-05 (normals), 0.000614 (UVs); triangles same, materials same |
| colliders | today: an object per turned box | 2,000 | 63.22 | 31.61 | make 47.37 + switch on 15.85 ms; 1602 objects; destroy 21.98 ms; 3,000 rays 5.51 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle | 2,000 | 90.43 | 45.21 | make 81.30 + switch on 9.13 ms; 10 objects; destroy 2.74 ms; 3,000 rays 2.64 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 29.30 | 14.65 | make 18.47 + switch on 10.83 ms; 2 objects; destroy 0.13 ms; 3,000 rays 3.83 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 2.95 | 1.48 | make 2.91 + switch on 0.04 ms (+ 11.08 ms baking on the worker); 2 objects; destroy 0.08 ms; 3,000 rays 2.74 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 7.02 | 175.60 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 11.54 | 288.46 | objects 0.98, allocate 0.39, pack 8.76, apply 1.40 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.51 | 37.81 | objects 1.32, allocate 0.02, pack 6.32 (worker), apply 0.17 ms |
| primitives | Box, turned, flat colour | 20,000 | 20.30 | 1.02 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 81.45 | 4.07 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 40.33 | 2.02 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 102.51 | 5.13 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 13.97 | 0.70 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 17.52 | 0.88 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 5.19 | 0.26 | 80,000 vertices |
| primitives | Tri | 20,000 | 3.52 | 0.18 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 19.81 | 3.96 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 45.48 | 9.10 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 25.23 | 8.41 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 37.75 | 12.58 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 15.04 | 7.52 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 11.90 | 11.90 |  |
| objects | BoxCollider added to one object | 2,000 | 39.55 | 19.78 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 21.07 | 105.35 |  |
| objects | RoomLight.Spawn | 100 | 10.33 | 103.34 |  |
| objects | switching all of the above on | 1 | 27.88 | 27880.50 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 9.65 | 9653.10 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.59 | 9.74 | worst 0.11 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.01 | 201.20 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 393.40 | 393397.90 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 379.84 | 379844.40 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 20.47 | 20469.90 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,285 ms, 3,765,781 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (24) | 0.48 | 0.04 | 0.87 | 9.40 | 39.77 | 23 / 7 / 1 / 1 | cutaway 39.1 | 5 (5 / 0) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 / 2,223 / 4,219,523 | 2 | 282 / 898 |
| drive-east | 2400 (168) | 0.49 | 0.04 | 5.10 | 8.03 | 27.26 | 161 / 25 / 4 / 0 | woods 23.6 | 35 (35 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,312 / 6,289,749 | 2 | 244 / 984 |
| teleport-louisville | 720 (166) | 2.75 | 0.04 | 15.47 | 32.12 | 66.57 | 167 / 75 / 35 / 7 | yards 65.8 | 25 (25 / 0) | 0 | 30 | 0.1 | 1.0 | 7 / 59 | 50 / 3,292 / 7,656,876 | 4 | 274 / 1,035 |
| drive-louisville | 1800 (99) | 0.64 | 0.04 | 5.06 | 15.87 | 66.95 | 97 / 40 / 18 / 2 | cutaway 66.8 | 15 (15 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,553 / 8,114,722 | 6 | 284 / 1,057 |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: cutaway (3x, worst 39.1 ms), signs (1x, worst 9.2 ms), buses and cruisers (1x, worst 5.2 ms), parking (1x, worst 4.3 ms), yards (1x, worst 3.5 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: woods (4x, worst 23.6 ms), cutaway (12x, worst 15.4 ms), buildings (3x, worst 13.8 ms), interior plans (1x, worst 11.2 ms), driveway cars (1x, worst 9.1 ms), wall drifts (1x, worst 5.5 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: yards (1x, worst 65.8 ms), fields (13x, worst 50.8 ms), cutaway (22x, worst 32.1 ms), fences and walls (1x, worst 29.5 ms), parking (1x, worst 29.3 ms), interior plans (3x, worst 28.3 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: cutaway (13x, worst 66.8 ms), buildings (14x, worst 56.4 ms), fields (7x, worst 13.9 ms), sidewalks (1x, worst 13.9 ms), yards (1x, worst 9.7 ms), road network (1x, worst 8.8 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 142 edit-mode artefacts

- `(edit mode) Error: Raid_sign_SCHOOL BUS3C2B06030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS3C2B06030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS3A2A06030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS3A2A06030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS382806030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_SCHOOL BUS382806030303: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_GIFTS37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_WICKLAND CHURCH130E0A383630: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_WICKLAND CHURCH130E0A383630: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_WATERFRONT PLAZA37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_WATERFRONT PLAZA37342D060606: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Pictures

Night = the game's look as a raid starts (22:00, overcast, the survivor's sight light, the zone's fog and tint, lights budgeted around the survivor as the game does). Day = plain overcast daylight (10:00 by the same sums, no fog) to judge the geometry. Close-ups use the isometric camera at its widest zoom (12.5 m half-height) except the spawn (the game's opening zoom, 9), with a 1.8 m capsule standing in for the survivor; the shoulder views leave the survivor out so the street shows. Roofs and upper storeys are hidden in the isometric view, as in the game.

| file | shows | mean luma | near-black | ms |
|---|---|--:|--:|--:|
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.602 | 0% | 8,584 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.072 | 0% | 255 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.490 | 0% | 5,798 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on West Stephen Foster Avenue, at the game's starting zoom | 0.101 | 0% | 5,838 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.480 | 0% | 78 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.099 | 0% | 70 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.478 | 0% | 109 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.091 | 0% | 99 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.449 | 0% | 53 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.099 | 0% | 88 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.604 | 0% | 72 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.079 | 0% | 72 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.395 | 0% | 74 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.074 | 0% | 73 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.467 | 0% | 70 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.080 | 0% | 65 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 80 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.073 | 0% | 107 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.460 | 0% | 73 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on West Stephen Foster Avenue (downtown), looking along the street | 0.214 | 19% | 102 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.445 | 0% | 124 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.169 | 21% | 57 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.538 | 0% | 154 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.472 | 0% | 69 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.077 | 0% | 64 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -84.2, 13.8 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
| downtown | -7.7, 15.0 | the place's own point in places.json (its OSM place node, which the square is built around), moved 16.8 m onto the nearest street |
| residential | -131.8, 183.1 | of the points every 6 m along the square's residential streets, the one with the most houses, apartments and trailers within 30 m |
| intersection | 2.3, 18.2 | the street node inside the square where the most, and the most important, streets meet (each scores its rank, double when it passes through) |
| green | -135.9, -100.2 | the biggest park, cemetery, playground, pitch or golf course inside the square (grass, meadow and orchard count half) |
| parking | -64.4, 125.6 | the biggest parking area inside the square |
| edge | -200.0, 37.4 | the best-ranked street crossing the square's edge, nearest its side's middle (how KentuckyMap ranks exits) |
| waterway | none | nothing of the kind in the square |
| landmark | 180.9, 150.6 | the largest footprint inside the square among churches, schools, warehouses, industrial, civic, hospital, retail, office, hotel and apartment buildings |

### Logged while `KentuckyMap.Build` ran: 4 errors, 116 warnings, 232 edit-mode artefacts

- `Warning: [CloneZ] Interiors: furnishing building 124090468 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY124090468: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY124090468: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 124092542 (Convenience) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY124092542: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY124092542: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 281586026 (Pharmacy) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY281586026: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY281586026: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1843815690 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1843815690: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1843815690: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1915619802 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1915619802: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1915619802: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 2239973240 (Workshop) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY2239973240: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY2239973240: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 2239973242 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY2239973242: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

### Everything logged during the run: 8 errors, 232 warnings, 606 edit-mode artefacts

- `Warning: [CloneZ] Interiors: furnishing building 124090468 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY124090468: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY124090468: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 124092542 (Convenience) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY124092542: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY124092542: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 281586026 (Pharmacy) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY281586026: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY281586026: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1843815690 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1843815690: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1843815690: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 1915619802 (Warehouse) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY1915619802: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY1915619802: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 2239973240 (Workshop) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY2239973240: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Interior KY2239973240: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `Warning: [CloneZ] Interiors: furnishing building 2239973242 (House) failed: System.NullReferenceException: Object reference not set to an instance of an object  @ UnityEngine.Debug:LogWarning (object)`
- `(edit mode) Error: Interior KY2239973242: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`

## Stages

| stage | ok | ms | error |
|---|---|--:|---|
| setup | yes | 125 |  |
| square: KentuckyMap.Build | yes | 5,279 |  |
| square: count | yes | 78 |  |
| square: pick spots | yes | 96 |  |
| square: pictures | yes | 24,009 |  |
| square: discard | yes | 97 |  |
| square: passes (warm rebuild) | yes | 1,933 |  |
| chunks: stress test | yes | 1,004 |  |
| chunks: Louisville 3x3 | yes | 1,834 |  |
| chunks: determinism | yes | 171 |  |
| perf: Lite against Full | yes | 400 |  |
| perf: determinism across build orders | yes | 362 |  |
| perf: what things cost | yes | 1,802 |  |
| perf: walking and driving through the state | yes | 159,209 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
