# CLONE-Z map harness: Bardstown (build ky-local-f2)

**Exit 2**: 8 error(s) logged or thrown by the map code while building (see the logs below). Full run, 191.4 s, started 2026-10-24 10:03:08.  
Unity 6000.7.0a2, Direct3D11 / Intel(R) Arc(TM) A750 Graphics. World store `C:\Users\hlcma\source\Unity\CLONE-Z\.revamp\world-f2`. Map data © OpenStreetMap contributors (ODbL)

## Start square: `KentuckyMap.Build`

The fixed 400 m square around Bardstown plus its scenery ring, built at once in edit mode from a copy of zone `bardstown` with `RealStatewide = false`. (The statewide game builds its start square as streamed chunks instead: see the Louisville pictures for that look.)

| | |
|---|---|
| built | yes |
| build time (cold: the first map built in the process) | 4,494 ms |
| the same square again, warm, pass by pass | 2,033 ms |
| logged while building | 4 errors, 116 warnings, 232 edit-mode artefacts |
| spawn | -13.6, -112.9, facing 14° |
| exits | US 150 north at 59.9, 197.0; US 62 east at 197.0, -62.9; South 3rd Street (south) at -39.3, -197.0; US 31E west at -197.0, 36.7 |
| overhead renderers (hidden in the iso view) | 66 |
| `KentuckyMap.LastStats` | Kentucky map Bardstown (build ky-local-f2): 117 buildings, 134 beyond the edge, 152 street pieces, 516 containers, 20 lights, 334 trees, 4 exits. Map data © OpenStreetMap contributors (ODbL) |
| map data: buildings | 117 with their middle inside the square (13 of them reach within 1 m of its edge or past it), 134 in the scenery ring; inside: house 64, commercial 50, church 2, canopy 1 |
| map data: streets, lines, areas | 153 street pieces, 0 lines (), 3 land-use areas (parking 2, cemetery 1) within reach |

| root | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| World | 15,123 | 5,529 | 5,529 | 4,624 | 3,294,265 | 1,956,098 | 113 | 11,989 | 246 (0) | 5,235 | 4,162 | 1,843 |
| RaidOverhead | 447 | 329 | 329 | 329 | 318,940 | 162,039 | 128 | 1,325 | 0 (0) | 0 | 329 | 0 |
| RaidScenery | 117 | 116 | 116 | 116 | 83,858 | 83,222 | 38 | 336 | 0 (0) | 0 | 71 | 0 |
| **all** | 15,687 | 5,974 | 5,974 | 5,069 | 3,697,063 | 2,201,359 | 210 | 13,650 | 246 (0) | 5,235 | 4,562 | 1,843 |

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
| yards | 10 | 0% | 10 | 2 |
| snow tone | 14 | 1% | 1 | 14 |
| road network | 5 | 0% | 1 | 5 |
| junctions | 5 | 0% | 1 | 5 |
| streets | 39 | 2% | 1 | 39 |
| sidewalks | 23 | 1% | 67 | 3 |
| berms and driveways | 13 | 1% | 62 | 1 |
| road markings | 1 | 0% | 17 | 0 |
| paths | 2 | 0% | 1 | 2 |
| railways | 0 | 0% | 1 | 0 |
| bridges | 0 | 0% | 1 | 0 |
| scenery roads | 1 | 0% | 1 | 1 |
| buildings | 131 | 6% | 21 | 25 |
| interior plans | 170 | 8% | 7 | 41 |
| interior shell | 657 | 32% | 116 | 38 |
| interior walls | 132 | 6% | 116 | 7 |
| interior doors | 504 | 25% | 116 | 22 |
| cutaway | 61 | 3% | 15 | 17 |
| utility lines | 12 | 1% | 1 | 12 |
| power lines | 0 | 0% | 1 | 0 |
| utility poles | 4 | 0% | 1 | 4 |
| street lights | 3 | 0% | 1 | 3 |
| signs | 2 | 0% | 1 | 2 |
| street furniture | 9 | 0% | 1 | 9 |
| landmarks | 0 | 0% | 1 | 0 |
| fences and walls | 0 | 0% | 1 | 0 |
| park litter | 4 | 0% | 1 | 4 |
| nature plan | 1 | 0% | 1 | 1 |
| driveways | 1 | 0% | 1 | 1 |
| mapped trees | 0 | 0% | 1 | 0 |
| street trees | 3 | 0% | 1 | 3 |
| fence rows | 0 | 0% | 1 | 0 |
| trees | 9 | 0% | 15 | 1 |
| driveway cars | 3 | 0% | 3 | 2 |
| buses and cruisers | 0 | 0% | 1 | 0 |
| kerbside cars | 31 | 2% | 22 | 12 |
| parking lot cars | 2 | 0% | 2 | 1 |
| wrecks | 0 | 0% | 1 | 0 |
| plough piles | 0 | 0% | 1 | 0 |
| wall drifts | 29 | 1% | 10 | 5 |
| fence drifts | 0 | 0% | 1 | 0 |
| yard snow | 1 | 0% | 1 | 1 |
| danger and landmarks | 0 | 0% | 1 | 0 |
| points | 2 | 0% | 1 | 2 |
| (emit the overhead meshes) | 9 | 0% | 1 | 9 |
| (emit the outdoor meshes) | 36 | 2% | 1 | 36 |
| **total** | **2,033** | | | |

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
| start-centre | Bardstown | 11913_9092 (186_142) | 0 + 0 | **51** | cutaway 18 | 0 | 92 | 62 | 97,611 | 50,452 | 2 | 68 | 8 | 64/8/0 | 0E 0W |
| rural-5km | 5 km from Bardstown (bearing 60°) | 11947_9112 (186_142) | 0 + 0 | **40** | buildings 10 | 0 | 95 | 64 | 135,052 | 77,486 | 5 | 115 | 12 | 3/12/0 | 0E 0W |
| louisville-downtown | Louisville | 11712_9478 (183_148) | 156 + 10 | **110** | cutaway 29 | 0 | 99 | 54 | 179,642 | 93,596 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| lexington-downtown | Lexington | 12577_9303 (196_145) | 426 + 59 | **70** | buildings 19 | 0 | 100 | 54 | 94,116 | 50,182 | 0 | 40 | 6 | 43/6/6 | 0E 0W |
| hazard | Hazard | 13490_8630 (210_134) | 5 + 1 | **19** | streets 3 | 0 | 68 | 40 | 61,640 | 45,301 | 0 | 2 | 0 | 6/0/3 | 0E 0W |

<details><summary>start-centre: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 2.3 | 1 | 2.3 |
| ground | 2.5 | 1 | 2.5 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 1.4 | 1 | 1.4 |
| junctions | 0.9 | 1 | 0.9 |
| streets | 6.1 | 1 | 6.1 |
| sidewalks | 2.7 | 17 | 0.7 |
| berms and driveways | 1.5 | 15 | 0.3 |
| road markings | 0.4 | 13 | 0.4 |
| paths | 1.1 | 1 | 1.1 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 4.5 | 2 | 4.0 |
| cutaway | 17.9 | 2 | 17.8 |
| utility lines | 0.7 | 1 | 0.7 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 1.3 | 1 | 1.3 |
| street furniture | 0.5 | 1 | 0.5 |
| landmarks | 0.2 | 1 | 0.2 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.4 | 1 | 0.4 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.1 | 1 | 0.1 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.2 | 4 | 0.1 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.6 | 1 | 0.6 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.4 | 1 | 1.4 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 0.1 | 1 | 0.1 |
| overhead | 0.6 | 1 | 0.6 |
| outdoor | 1.6 | 1 | 1.6 |

</details>

<details><summary>rural-5km: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.1 | 1 | 0.1 |
| ground | 0.1 | 1 | 0.1 |
| water | 0.0 | 1 | 0.0 |
| fields | 0.0 | 1 | 0.0 |
| parking | 0.0 | 1 | 0.0 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 3.6 | 2 | 2.1 |
| snow tone | 1.2 | 1 | 1.2 |
| road network | 0.2 | 1 | 0.2 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 1.9 | 1 | 1.9 |
| sidewalks | 0.5 | 4 | 0.3 |
| berms and driveways | 0.7 | 4 | 0.5 |
| road markings | 0.0 | 1 | 0.0 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 10.3 | 2 | 10.1 |
| cutaway | 10.8 | 2 | 9.7 |
| utility lines | 0.1 | 1 | 0.1 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.1 | 1 | 0.1 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 0.0 | 1 | 0.0 |
| street furniture | 0.3 | 1 | 0.3 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.0 | 1 | 0.0 |
| nature plan | 0.0 | 1 | 0.0 |
| driveways | 0.1 | 1 | 0.1 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.6 | 1 | 0.6 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 1.1 | 4 | 0.4 |
| driveway cars | 0.5 | 1 | 0.5 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.2 | 2 | 1.2 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.2 | 1 | 0.2 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 6.0 | 1 | 6.0 |

</details>

<details><summary>louisville-downtown: steps</summary>

| step | ms | runs | slowest |
|---|--:|--:|--:|
| data | 0.5 | 1 | 0.5 |
| ground | 8.2 | 1 | 8.2 |
| water | 13.5 | 1 | 13.5 |
| fields | 14.3 | 1 | 14.3 |
| parking | 0.7 | 1 | 0.7 |
| grounds | 0.0 | 1 | 0.0 |
| yards | 2.4 | 2 | 2.3 |
| snow tone | 1.6 | 1 | 1.6 |
| road network | 0.6 | 1 | 0.6 |
| junctions | 0.3 | 1 | 0.3 |
| streets | 0.8 | 1 | 0.8 |
| sidewalks | 1.1 | 4 | 1.1 |
| berms and driveways | 1.1 | 4 | 0.9 |
| road markings | 0.0 | 2 | 0.0 |
| paths | 4.4 | 1 | 4.4 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 13.6 | 2 | 8.6 |
| cutaway | 28.8 | 1 | 28.8 |
| utility lines | 0.4 | 1 | 0.4 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.4 | 1 | 0.4 |
| street lights | 0.3 | 1 | 0.3 |
| signs | 0.2 | 1 | 0.2 |
| street furniture | 0.2 | 1 | 0.2 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 3.3 | 1 | 3.3 |
| nature plan | 0.7 | 1 | 0.7 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 5.3 | 1 | 5.3 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.1 | 4 | 0.0 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 1.7 | 3 | 0.9 |
| parking lot cars | 0.1 | 1 | 0.1 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 3.0 | 1 | 3.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.2 | 1 | 0.2 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 1.7 | 1 | 1.7 |

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
| yards | 8.8 | 2 | 8.8 |
| snow tone | 2.2 | 1 | 2.2 |
| road network | 0.8 | 1 | 0.8 |
| junctions | 0.7 | 1 | 0.7 |
| streets | 6.2 | 1 | 6.2 |
| sidewalks | 2.6 | 11 | 1.0 |
| berms and driveways | 1.4 | 10 | 0.6 |
| road markings | 0.2 | 6 | 0.1 |
| paths | 0.9 | 1 | 0.9 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 19.1 | 1 | 19.1 |
| cutaway | 12.7 | 1 | 12.7 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 1.0 | 1 | 1.0 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 2.8 | 1 | 2.8 |
| street furniture | 0.5 | 1 | 0.5 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.5 | 1 | 0.5 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.0 | 1 | 0.0 |
| trees | 0.3 | 4 | 0.2 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 1.9 | 3 | 0.7 |
| parking lot cars | 0.0 | 1 | 0.0 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 1.9 | 1 | 1.9 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.1 | 1 | 0.1 |
| points | 1.0 | 1 | 1.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 1.7 | 1 | 1.7 |

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
| yards | 0.0 | 2 | 0.0 |
| snow tone | 1.3 | 1 | 1.3 |
| road network | 0.1 | 1 | 0.1 |
| junctions | 0.4 | 1 | 0.4 |
| streets | 2.7 | 1 | 2.7 |
| sidewalks | 0.0 | 7 | 0.0 |
| berms and driveways | 1.1 | 7 | 0.3 |
| road markings | 0.4 | 4 | 0.3 |
| paths | 0.0 | 1 | 0.0 |
| railways | 0.0 | 1 | 0.0 |
| bridges | 0.0 | 1 | 0.0 |
| buildings | 0.0 | 1 | 0.0 |
| utility lines | 0.3 | 1 | 0.3 |
| power lines | 0.0 | 1 | 0.0 |
| utility poles | 0.3 | 1 | 0.3 |
| street lights | 0.0 | 1 | 0.0 |
| signs | 1.1 | 1 | 1.1 |
| street furniture | 0.0 | 1 | 0.0 |
| landmarks | 0.0 | 1 | 0.0 |
| fences and walls | 0.0 | 1 | 0.0 |
| park litter | 0.6 | 1 | 0.6 |
| nature plan | 0.1 | 1 | 0.1 |
| driveways | 0.0 | 1 | 0.0 |
| mapped trees | 0.0 | 1 | 0.0 |
| street trees | 0.0 | 1 | 0.0 |
| fence rows | 0.6 | 1 | 0.6 |
| trees | 3.8 | 4 | 1.6 |
| driveway cars | 0.0 | 1 | 0.0 |
| buses and cruisers | 0.0 | 1 | 0.0 |
| kerbside cars | 0.1 | 1 | 0.1 |
| parking lot cars | 1.6 | 3 | 0.7 |
| wrecks | 0.0 | 1 | 0.0 |
| plough piles | 0.0 | 1 | 0.0 |
| wall drifts | 0.0 | 1 | 0.0 |
| fence drifts | 0.0 | 1 | 0.0 |
| yard snow | 0.0 | 1 | 0.0 |
| danger and landmarks | 0.0 | 1 | 0.0 |
| points | 0.0 | 1 | 0.0 |
| overhead | 0.3 | 1 | 0.3 |
| outdoor | 2.2 | 1 | 2.2 |

</details>

### Regions read (8 km files; the streamer reads and sorts them off the main thread)

| region | data | read ms | sort into chunks ms |
|---|---|--:|--:|
| 186_142 | yes | 87 | 14 |
| 186_141 | yes | 11 | 2 |
| 185_141 | yes | 7 | 2 |
| 185_142 | yes | 12 | 8 |
| 183_148 | yes | 156 | 10 |
| 196_145 | yes | 426 | 59 |
| 210_134 | yes | 5 | 1 |
| 182_148 | yes | 125 | 23 |

### Louisville 3x3 (centre chunk 11712_9478)

Nine chunks in 781 ms (slowest 129 ms); in chunks_louisville_top.png the seams are the lines x = 693 and 1,355 px, y = the same (from the top left); the iso pictures are centred on the corner where four chunks meet (-40.7, -4.7).

| chunk | place | cx_cz (region) | region read + index ms | total ms | slowest step | activate ms | objects | renderers | vertices | triangles | lights | colliders | buildings | data: streets/owned/areas | logged |
|---|---|---|--:|--:|---|--:|--:|--:|--:|--:|--:|--:|--:|---|---|
| louisville 0,0 | Louisville | 11712_9478 (183_148) | 0 + 0 | **64** | cutaway 24 | 0 | 99 | 54 | 179,642 | 93,596 | 0 | 65 | 6 | 80/6/19 | 0E 0W |
| louisville 1,0 | Louisville | 11713_9478 (183_148) | 0 + 0 | **119** | interior plans 27 | 0 | 100 | 75 | 242,421 | 126,701 | 0 | 129 | 17 | 11/17/14 | 0E 0W |
| louisville -1,0 | Louisville | 11711_9478 (182_148) | 125 + 23 | **88** | cutaway 18 | 0 | 106 | 61 | 114,114 | 64,469 | 1 | 67 | 4 | 45/4/9 | 0E 0W |
| louisville 0,1 | Louisville | 11712_9479 (183_148) | 0 + 0 | **89** | cutaway 37 | 0 | 101 | 53 | 267,514 | 138,316 | 0 | 83 | 8 | 19/8/3 | 0E 0W |
| louisville 0,-1 | Louisville | 11712_9477 (183_148) | 0 + 0 | **129** | fences and walls 35 | 0 | 109 | 63 | 249,704 | 133,131 | 2 | 48 | 4 | 40/4/9 | 0E 0W |
| louisville 1,1 | Louisville | 11713_9479 (183_148) | 0 + 0 | **61** | cutaway 28 | 0 | 87 | 53 | 189,926 | 102,417 | 0 | 55 | 5 | 11/5/5 | 0E 0W |
| louisville -1,1 | Louisville | 11711_9479 (182_148) | 0 + 0 | **64** | buildings 15 | 0 | 100 | 54 | 162,245 | 86,915 | 0 | 49 | 5 | 44/5/5 | 0E 0W |
| louisville 1,-1 | Louisville | 11713_9477 (183_148) | 0 + 0 | **41** | cutaway 7 | 0 | 95 | 53 | 111,928 | 66,746 | 1 | 15 | 1 | 32/1/7 | 0E 0W |
| louisville -1,-1 | Louisville | 11711_9477 (182_148) | 0 + 0 | **127** | cutaway 69 | 0 | 80 | 47 | 212,975 | 116,230 | 0 | 44 | 3 | 49/3/10 | 0E 0W |

| block | objects | renderers | mesh filters | distinct meshes | vertices | triangles | materials | material slots | lights (shadowed) | colliders | shadow casters | loot containers |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| Louisville 3x3 | 879 | 513 | 513 | 512 | 1,730,469 | 928,521 | 127 | 2,043 | 4 (0) | 555 | 356 | 88 |

### Determinism

Chunk 11913_9092 (start-centre) built twice: **match**: identical, in the same order (97,611 vertices, 50,452 triangles, 92 objects, 62 meshes; hash 9b2fc0e0eddaa548 vs 9b2fc0e0eddaa548; 74 colliders, lights and loot containers, hash eac6efd572f190d6 vs eac6efd572f190d6). Compared: every mesh's vertices (mm), normals, UVs, triangles and materials; every object's place, rotation, scale and state; every collider, light and loot container with its contents.

### Where a piece's time goes

From RaidTown's and RaidMesh's running totals over each build: colliders (made / the objects holding them / ms), lights, meshes baked (count / vertices / ms baking the data) and the objects made to show them (ms, baking included); the rest is the passes' own work (geometry, the occupancy grid, loot, props' objects). Nav = the NavMesh sources KentuckyStreamer collects from the built piece (count / ms), on the main thread.

| piece | detail | total ms | colliders | lights | meshes | emit ms | rest ms | nav sources |
|---|---|--:|---|---|---|--:|--:|---|
| start-centre | Full | 51 | 40 / 1 / 0.1 | 0 / 0.0 | 58 / 94,845 / 7.5 | 9.1 | 42.3 | 43 / 3.2 |
| rural-5km | Full | 40 | 84 / 1 / 0.1 | 0 / 0.0 | 63 / 132,452 / 2.6 | 8.4 | 31.8 | 43 / 0.3 |
| louisville-downtown | Full | 110 | 102 / 1 / 0.1 | 0 / 0.0 | 52 / 177,026 / 3.4 | 8.1 | 101.7 | 37 / 0.2 |
| lexington-downtown | Full | 70 | 67 / 1 / 0.1 | 0 / 0.0 | 51 / 91,462 / 2.2 | 3.6 | 66.4 | 38 / 0.2 |
| hazard | Full | 19 | 165 / 1 / 0.1 | 0 / 0.0 | 35 / 58,118 / 1.3 | 2.1 | 16.8 | 28 / 0.2 |
| louisville 0,0 | Full | 64 | 102 / 1 / 0.1 | 0 / 0.0 | 52 / 177,026 / 3.2 | 4.6 | 59.3 | 37 / 0.2 |
| louisville 1,0 | Full | 119 | 71 / 1 / 0.1 | 0 / 0.0 | 73 / 239,751 / 5.7 | 8.3 | 110.8 | 46 / 0.3 |
| louisville -1,0 | Full | 88 | 154 / 1 / 0.1 | 0 / 0.0 | 53 / 110,782 / 2.5 | 3.9 | 83.6 | 38 / 0.2 |
| louisville 0,1 | Full | 89 | 83 / 1 / 0.1 | 0 / 0.0 | 50 / 264,857 / 6.6 | 8.3 | 80.3 | 36 / 0.2 |
| louisville 0,-1 | Full | 129 | 93 / 1 / 0.1 | 0 / 0.0 | 57 / 245,339 / 5.4 | 7.6 | 121.5 | 38 / 0.3 |
| louisville 1,1 | Full | 61 | 69 / 1 / 0.1 | 0 / 0.0 | 50 / 186,854 / 3.7 | 5.3 | 55.7 | 35 / 0.2 |
| louisville -1,1 | Full | 64 | 126 / 1 / 0.1 | 0 / 0.0 | 51 / 159,000 / 3.3 | 4.8 | 59.2 | 37 / 0.2 |
| louisville 1,-1 | Full | 41 | 102 / 1 / 0.1 | 0 / 0.0 | 48 / 107,739 / 2.4 | 3.7 | 36.9 | 33 / 0.2 |
| louisville -1,-1 | Full | 127 | 116 / 1 / 0.1 | 0 / 0.0 | 43 / 209,862 / 4.4 | 5.6 | 121.1 | 30 / 0.2 |

The Louisville block's NavMesh: 330 sources collected in 2 ms; baked at once (as one 384 m square) in 170 ms. In the game the bake runs on worker threads, tile by tile.

## Streaming performance

### Lite against Full

Each test piece built in both details, warm, back to back (Lite should cost at most a quarter of Full).

| piece | Full ms | Lite ms | Lite/Full | slowest step Full | slowest step Lite | vertices Full / Lite | objects Full / Lite | renderers Full / Lite | colliders Full / Lite | lights Full / Lite |
|---|--:|--:|--:|---|---|---|---|---|---|---|
| start-centre | 41 | 14 | 34% | cutaway 12.2 | streets 5.3 | 97,611 / 12,889 | 92 / 37 | 62 / 32 | 68 / 1 | 2 / 0 |
| rural-5km | 28 | 6 | 23% | cutaway 9.4 | streets 1.5 | 134,741 / 8,358 | 95 / 38 | 64 / 33 | 115 / 1 | 5 / 0 |
| louisville-downtown | 103 | 12 | 12% | cutaway 45.3 | buildings 3.6 | 193,082 / 15,376 | 99 / 36 | 54 / 31 | 65 / 1 | 0 / 0 |
| lexington-downtown | 56 | 13 | 23% | cutaway 15.1 | streets 5.6 | 130,237 / 11,875 | 100 / 33 | 57 / 28 | 44 / 1 | 2 / 0 |
| hazard | 21 | 16 | 80% | streets 2.7 | outdoor 5.7 | 68,128 / 8,497 | 73 / 33 | 40 / 28 | 2 / 1 | 0 / 0 |

### Determinism across build orders

- start-centre, Full, with a Louisville piece built in between: **match**: identical, in the same order
- start-centre, Full, then Lite, then Full again (an upgrade): **match**: identical, in the same order
- start-centre, Lite, with a Louisville piece built in between: **match**: identical, in the same order

### What things cost

Micro-benchmarks, warm, in the editor (Mono, edit mode).

| group | what | n | ms | µs each | notes |
|---|---|--:|--:|--:|---|
| equivalence | the fast RaidMesh against the old primitives, call for call | 4,000 | 0.00 | 0.00 | SAME: 81,034 vs 81,034 vertices; bit-exact positions 73,873, normals 69,223, UVs 78,946; largest difference 3.93E-06 m, 1.2E-05 (normals), 0.000614 (UVs); triangles same, materials same |
| colliders | today: an object per turned box | 2,000 | 63.08 | 31.54 | make 54.12 + switch on 8.96 ms; 1602 objects; destroy 5.97 ms; 3,000 rays 2.95 ms, 1075 hits, 3200.7 m |
| colliders | grouped: an object per angle | 2,000 | 17.10 | 8.55 | make 11.23 + switch on 5.87 ms; 10 objects; destroy 2.63 ms; 3,000 rays 2.26 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, cooked on the main thread | 2,000 | 14.96 | 7.48 | make 6.41 + switch on 8.55 ms; 2 objects; destroy 0.10 ms; 3,000 rays 1.97 ms, 1075 hits, 3200.7 m |
| colliders | one MeshCollider, baked on a worker | 2,000 | 4.60 | 2.30 | make 4.56 + switch on 0.04 ms (+ 9.54 ms baking on the worker); 2 objects; destroy 0.09 ms; 3,000 rays 2.10 ms, 1075 hits, 3200.7 m |
| meshes | today: RaidMesh.Emit, one by one | 40 | 7.13 | 178.32 | 288,000 vertices; GameObjects, components and meshes |
| meshes | MeshBake, packed on the main thread | 40 | 11.10 | 277.38 | objects 0.98, allocate 0.42, pack 8.42, apply 1.28 ms; same as today's: yes |
| meshes | MeshBake, packed on a worker | 40 | 1.48 | 36.92 | objects 1.33, allocate 0.02, pack 5.89 (worker), apply 0.13 ms |
| primitives | Box, turned, flat colour | 20,000 | 20.98 | 1.05 | 480,000 vertices |
| primitives | before: Box, turned, flat colour | 20,000 | 87.05 | 4.35 | 480,000 vertices |
| primitives | Box, turned, planar texture | 20,000 | 45.53 | 2.28 | 480,000 vertices |
| primitives | before: Box, turned, planar texture | 20,000 | 127.50 | 6.38 | 480,000 vertices |
| primitives | Box, square, flat colour | 20,000 | 15.43 | 0.77 | 480,000 vertices |
| primitives | Box in a turned frame (furniture) | 20,000 | 27.01 | 1.35 | 480,000 vertices |
| primitives | Quad (Upright) | 20,000 | 7.16 | 0.36 | 80,000 vertices |
| primitives | Tri | 20,000 | 5.60 | 0.28 | 60,000 vertices |
| primitives | Cylinder, 8 sides | 5,000 | 34.97 | 6.99 | 190,000 vertices |
| primitives | before: Cylinder, 8 sides | 5,000 | 63.04 | 12.61 | 190,000 vertices |
| primitives | Ellipsoid, 8 x 5 | 3,000 | 32.47 | 10.82 | 162,000 vertices |
| primitives | before: Ellipsoid, 8 x 5 | 3,000 | 40.54 | 13.51 | 162,000 vertices |
| objects | empty GameObject, parented and placed | 2,000 | 14.97 | 7.49 |  |
| objects | GameObject + MeshFilter + MeshRenderer | 1,000 | 12.30 | 12.30 |  |
| objects | BoxCollider added to one object | 2,000 | 41.11 | 20.56 |  |
| objects | LootContainer.Spawn (a crate) | 200 | 29.53 | 147.63 |  |
| objects | RoomLight.Spawn | 100 | 13.30 | 132.99 |  |
| objects | switching all of the above on | 1 | 37.76 | 37762.90 | edit mode: no Awake or OnEnable runs |
| objects | destroying all of the above | 1 | 17.98 | 17981.60 |  |
| triangulation | MapGeometry.Triangulate, per footprint | 61 | 0.67 | 11.01 | worst 0.12 ms; the biggest has 46 points; 0 over 100 |
| triangulation | KentuckyMap.Area for a piece (the data step) | 10 | 2.89 | 289.24 |  |
| regions | MapBuild.LoadRegion 196_145 (today) | 1 | 522.96 | 522954.90 | 0 MB allocated |
| regions | the same through one buffer and an exactly sized string | 1 | 518.68 | 518676.50 | 0 MB allocated; 18 MB of JSON; same data: yes |
| regions | sorting it into chunks (MapRegionIndex) | 1 | 26.97 | 26972.40 |  |

### Walking and driving through the state

KentuckyStreamer as the statewide game makes it, its start square built at once (25 pieces in 1,510 ms, 3,842,372 vertices), then simulated frame by frame at 60 fps (Update's work without the NavMesh and the agents; frames padded to 16.7 ms so the regions read in the background take their real time). Ms = the streamer's main-thread time per frame. Hole = frames the survivor stood where nothing was built; ring = frames the nine pieces around them weren't all Full.

| route | frames (building) | mean ms | p50 | p95 | p99 | max | >5 / >8 / >16 / >33 ms | longest step | built (Full / Lite) | upgraded | let go | own built after s | ring Full after s | hole / ring frames | pieces / meshes / vertices at the end | regions | managed / total MB |
|---|---|--:|--:|--:|--:|--:|---|---|---|--:|--:|--:|--:|---|---|--:|---|
| settle | 480 (22) | 0.33 | 0.04 | 0.07 | 8.37 | 11.44 | 21 / 5 / 0 / 0 | cutaway 9.7 | 5 (5 / 0) | 0 | 0 | 0.0 | 0.0 | 0 / 0 | 30 / 2,167 / 4,225,768 | 2 | 294 / 896 |
| drive-east | 2400 (237) | 1.09 | 0.05 | 6.61 | 18.17 | 91.02 | 238 / 88 / 31 / 9 | cutaway 77.2 | 39 (39 / 0) | 0 | 15 | 0.0 | 0.0 | 0 / 0 | 54 / 3,451 / 6,703,814 | 2 | 233 / 999 |
| teleport-louisville | 720 (228) | 4.54 | 0.05 | 25.36 | 53.34 | 108.26 | 229 / 103 / 55 / 26 | cutaway 105.6 | 25 (25 / 0) | 0 | 30 | 0.3 | 1.5 | 15 / 90 | 50 / 3,312 / 7,979,263 | 4 | 220 / 1,047 |
| drive-louisville | 1800 (130) | 0.77 | 0.05 | 5.68 | 15.75 | 56.51 | 128 / 48 / 17 / 3 | cutaway 56.2 | 15 (15 / 0) | 0 | 10 | 0.0 | 0.0 | 0 / 0 | 55 / 3,583 / 8,598,389 | 6 | 278 / 1,074 |

- **settle**: standing at the spawn while the country around the start is built; the frames over 8 ms were made by: cutaway (2x, worst 9.7 ms), junctions (1x, worst 6.9 ms), trees (1x, worst 5.7 ms), buildings (1x, worst 1.7 ms)
- **drive-east**: driving east from the spawn at 90 km/h; the frames over 8 ms were made by: cutaway (20x, worst 77.2 ms), driveway cars (1x, worst 56.9 ms), overhead (2x, worst 50.7 ms), wall drifts (2x, worst 44.6 ms), outdoor (10x, worst 35.6 ms), trees (13x, worst 25.4 ms)
- **teleport-louisville**: set down in downtown Louisville (56 km away), standing; the frames over 8 ms were made by: cutaway (23x, worst 105.6 ms), fields (14x, worst 81.6 ms), yards (1x, worst 77.1 ms), water (3x, worst 44.9 ms), buildings (28x, worst 41.6 ms), interior plans (3x, worst 37.6 ms)
- **drive-louisville**: driving south through downtown Louisville at 43 km/h; the frames over 8 ms were made by: cutaway (14x, worst 56.2 ms), buildings (11x, worst 40.0 ms), parking (1x, worst 14.6 ms), fields (6x, worst 14.4 ms), outdoor (3x, worst 12.9 ms), sidewalks (3x, worst 12.4 ms)

### Logged while walking and driving: 0 errors, 0 warnings, 142 edit-mode artefacts

- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_PAWN SHOP07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_BOONE'S BUTCHER SHOP050505372C11: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_TOWN & COUNTRY BANK & TR363431060707: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_INSURANCE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_APPLIANCES1906063B3A37: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_NELSON COUNTY JUSTICE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
- `(edit mode) Error: Raid_sign_NELSON COUNTY JUSTICE07100B3A372C: Destroy may not be called from edit mode! Use DestroyImmediate instead.  @ UnityEngine.Object:Destroy (UnityEngine.Object)`
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
| `00_overview.png` | Top-down overview of the whole start square (400 m, red outline) and its scenery ring, roofs on, north up; cyan = spawn, yellow = exits, magenta = the close-up spots | 0.595 | 0% | 1,328 |
| `00_overview_night.png` | The same overview by night as a raid starts, every map light on and unshadowed (no survivor, so no sight light): where the light is | 0.071 | 0% | 389 |
| `iso_spawn_day.png` | The raid's opening frame: the survivor's spawn on South 3rd Street, at the game's starting zoom | 0.445 | 0% | 168 |
| `iso_spawn_night.png` | The raid's opening frame: the survivor's spawn on South 3rd Street, at the game's starting zoom | 0.082 | 0% | 4,524 |
| `iso_downtown_day.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.462 | 0% | 92 |
| `iso_downtown_night.png` | Downtown Bardstown: the middle of the square, on Court Square | 0.095 | 0% | 84 |
| `iso_residential_day.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.435 | 0% | 69 |
| `iso_residential_night.png` | The densest residential street: West Flaget Street, 5 homes within 30 m | 0.080 | 0% | 135 |
| `iso_intersection_day.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.422 | 0% | 56 |
| `iso_intersection_night.png` | The busiest intersection: Court Square / US 31E;US 150 (3 ways in) | 0.094 | 0% | 68 |
| `iso_green_day.png` | Green: a cemetery, 5,927 m² inside the square | 0.606 | 0% | 81 |
| `iso_green_night.png` | Green: a cemetery, 5,927 m² inside the square | 0.079 | 0% | 62 |
| `iso_parking_day.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.388 | 0% | 68 |
| `iso_parking_night.png` | Parking: parking 'Free Municipal Parking', 2,442 m² inside the square | 0.074 | 0% | 71 |
| `iso_edge_day.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.447 | 0% | 59 |
| `iso_edge_night.png` | The square's edge: where West Stephen Foster Avenue leaves it (west), the playable map giving way to the scenery ring | 0.071 | 0% | 71 |
| `iso_landmark_day.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.360 | 0% | 62 |
| `iso_landmark_night.png` | Landmark: the biggest building in the square, church 'Bardstown United Methodist Church' (1,231 m² footprint) | 0.073 | 0% | 79 |
| `tp_downtown_day.png` | Third-person shoulder view from the raid's spawn on South 3rd Street (downtown), looking along the street | 0.415 | 0% | 95 |
| `tp_downtown_night.png` | Third-person shoulder view from the raid's spawn on South 3rd Street (downtown), looking along the street | 0.137 | 23% | 135 |
| `tp_residential_day.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.398 | 0% | 73 |
| `tp_residential_night.png` | Third-person shoulder view on the sidewalk of West Flaget Street (the densest residential street), looking along it | 0.166 | 21% | 189 |
| `chunks_louisville_top.png` | Streamed look: a 3x3 block of 128 m chunks around downtown Louisville (KentuckyStreamer's way, no scenery ring), top-down, roofs on, north up | 0.548 | 0% | 174 |
| `chunks_louisville_iso_day.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.500 | 0% | 62 |
| `chunks_louisville_iso_night.png` | Streamed look: downtown Louisville in the isometric view, centred where four chunks meet | 0.080 | 0% | 61 |

### Spots (map metres from the place: x east, z north)

| spot | x, z | why |
|---|---|---|
| spawn | -13.6, -112.9 | KentuckyMap.Build's spawn (the sidewalk spot nearest the place's centre), facing along the street |
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
| setup | yes | 74 |  |
| square: KentuckyMap.Build | yes | 4,497 |  |
| square: count | yes | 79 |  |
| square: pick spots | yes | 83 |  |
| square: pictures | yes | 10,226 |  |
| square: discard | yes | 119 |  |
| square: passes (warm rebuild) | yes | 2,185 |  |
| chunks: stress test | yes | 1,108 |  |
| chunks: Louisville 3x3 | yes | 1,831 |  |
| chunks: determinism | yes | 135 |  |
| perf: Lite against Full | yes | 409 |  |
| perf: determinism across build orders | yes | 334 |  |
| perf: what things cost | yes | 2,065 |  |
| perf: walking and driving through the state | yes | 168,246 |  |

## Limits of this harness

- Edit mode: no `Awake`/`Start`/`Update` runs, so no NavMesh bake, flicker, lamp scripts, snowfall, people, zombies, fog-of-war shading or HUD. Lights are switched per picture as the game's budgets would (RoomLight: 45 m and four shadowed; street lamps: 64 m); shadowed room lights keep URP's default High shadow tier (the game asks for Low, which can't be set outside play mode).
- The lighting is the game's own sums (TimeOfDay, WeatherSystem's overcast start, ZoneAtmosphere, VisionController's raid night), replicated rather than run; the pictures add SMAA, which the game doesn't use.
- Timings are single wall-clock runs in the editor (Stopwatch): expect 10-20% noise between runs. Edit mode skips component callbacks, so activation costs are lower than in play mode.
